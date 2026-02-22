using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Vector2Int = UnityEngine.Vector2Int;


public enum AnimationType
{
    Move,
    ChainedAnimation
}

public record CellAnimationStep
{
    public AnimationType animationType { get; }
    public GameObject cellGO { get; }
    public string? triggerName { get; }
    public Vector2Int? positionOrigin { get; }
    public Vector2Int? positionDestination { get; }

    private CellAnimationStep(
        AnimationType animationType,
        GameObject cellGO,
        string? triggerName,
        Vector2Int? positionOrigin,
        Vector2Int? positionDestination)
    {
        this.animationType = animationType;
        this.cellGO = cellGO;
        this.triggerName = triggerName;
        this.positionOrigin = positionOrigin;
        this.positionDestination = positionDestination;
    }

    // --- Factory pour animation chaînée ---
    public static CellAnimationStep Chained(GameObject cellGO, string triggerName)
    {
        return new CellAnimationStep(AnimationType.ChainedAnimation, cellGO, triggerName, null, null);
    }

    // --- Factory pour un move ---
    public static CellAnimationStep Move(GameObject cellGO, Vector2Int origin, Vector2Int destination)
    {
        return new CellAnimationStep(AnimationType.Move, cellGO, null, origin, destination);
    }
}


public record AnimationData
{
    public IReadOnlyList<CellAnimationStep> animations { get; }

    public AnimationData(IReadOnlyList<CellAnimationStep> animations)
    {
        this.animations = animations;
    }
}


public class GameModel 
{
    public enum BoxValue
    {
        IsUserCell,
        IsComputerCell,
        IsFreeBox
    }

    public enum SelectionType
    {
        TheMost = default,
        TheLeast = 1
    }

    public const int NbRows = 7;
    public const int NbColumns = 7;
    public const int MaxDistanceMove = 2;

 
    public event Action<AnimationData> OnInitialize;

    private readonly GameObject cellPrefab;
    
    private const string TriggerNameUserCellBirth = "giveBirthToUserCell",
        TriggerNameComputerCellBirth = "giveBirthToComputerCell",
        TriggerNameMutate = "mutate";

    private GameObject[,] cellsBoard;

    private BoxValue this[int col, int row]
    {
        get
        {
            //GameObject
            BoxValue boxValue;
            var cellGO = cellsBoard[col, row];
            if (cellGO == null)
                return BoxValue.IsFreeBox;

            //Animator
            var animator = cellGO.GetComponent<Animator>();
            
            if (animator == null)
                return BoxValue.IsFreeBox;

            //Determinate the state of the cell
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("IsComputerCell")) return BoxValue.IsComputerCell;
            return BoxValue.IsUserCell;
        }
    }

    private List<Vector2Int> candidatePlayerCells,
        candidateComputerCells;

    private List<CellAnimationStep> cellAnimationSteps;

    public GameModel(GameObject cellPrefab)
    {
        this.cellPrefab = cellPrefab;
    }
    
    public void DoInArea(RectInt area, Action<Vector2Int, BoxValue> action)
    {
        foreach (var currentPosition in area.allPositionsWithin)
        {
            if (currentPosition.x < 0
                || currentPosition.y < 0
                || currentPosition.x >= NbRows
                || currentPosition.y >= NbColumns) continue;

            var currentBox = this[currentPosition.x, currentPosition.y];
            action.Invoke(currentPosition, currentBox);
        }
    }


    // ReSharper disable Unity.PerformanceAnalysis
    public void InitGameModel()
    {
        //1. Destroy previous game objects
        ClearCellsBoard();
        
        //2. Recreate the board
        cellsBoard = new GameObject[NbColumns, NbRows];

        //3. Instantiate the 4 initial cells
        var cell00 = InstanciateCellPrefab(new Vector2Int(0, 0));
        var cell66 = InstanciateCellPrefab(new Vector2Int(NbColumns - 1, NbRows - 1));
        var cell06 = InstanciateCellPrefab(new Vector2Int(0, NbRows - 1));
        var cell60 = InstanciateCellPrefab(new Vector2Int(NbColumns - 1, 0));
        
        //4. Build the chained animation sequence
        var steps = new List<CellAnimationStep>
        {
            CellAnimationStep.Chained(cell00, TriggerNameComputerCellBirth),
            CellAnimationStep.Chained(cell66, TriggerNameComputerCellBirth),
            CellAnimationStep.Chained(cell06, TriggerNameUserCellBirth),
            CellAnimationStep.Chained(cell60, TriggerNameUserCellBirth)
        };

        //5. Notify the view
        var animationData = new AnimationData(steps);
        OnInitialize?.Invoke(animationData);
    }


    public GameObject GetCellGameObject(int col, int row)
    {
        return this.cellsBoard[col, row];
    }

    public bool CandidateCellIsChosen(Vector2Int cellPosition, BoxValue boxValue)
    {
        bool result;
        switch (boxValue)
        {
            case BoxValue.IsFreeBox:
                if (cellPosition.x >= 0 && cellPosition.x < NbColumns && cellPosition.y >= 0 &&
                    cellPosition.y < NbRows)
                    result = this[cellPosition.x, cellPosition.y] == boxValue;
                else
                    result = false;
                break;
            case BoxValue.IsComputerCell:
                result = candidateComputerCells.Contains(cellPosition);
                break;
            case BoxValue.IsUserCell:
                result = candidatePlayerCells.Contains(cellPosition);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(boxValue), boxValue, null);
        }

        return result;
    }

    public bool NoMoreBoxesWithCellValue(BoxValue cellValue)
    {
        for (var x = 0; x < NbColumns; x++)
        for (var y = 0; y < NbRows; y++)
        {
            var cell = this[x, y];
            if (cell == cellValue) return false;
        }

        return true;
    }

    public List<CellAnimationStep> MoveOrCloneTheCell(Vector2Int posOrigin, Vector2Int posDestination)
    {
        var resultSteps = new List<CellAnimationStep>();

        // 0. Compute delta positions in x and y
        var deltaX = Mathf.Abs(posDestination.x - posOrigin.x);
        var deltaY = Mathf.Abs(posDestination.y - posOrigin.y);

        if (deltaX == 0 && deltaY == 0)
            throw new InvalidOperationException("MoveOrCloneTheCell requires a minimum distance of 1");

        // 1. Destination must be a free box
        if (this[posDestination.x, posDestination.y] != BoxValue.IsFreeBox)
            throw new InvalidOperationException("MoveOrCloneTheCell requires the destination to be a free box");

        // Determine the owner of the origin cell
        var originOwner = this[posOrigin.x, posOrigin.y];
        if (originOwner == BoxValue.IsFreeBox)
            throw new InvalidOperationException("Origin cell cannot be free in MoveOrCloneTheCell");

        // 2. Clone if the freebox is side by side with the origin cell
        if (deltaX <= 1 && deltaY <= 1 )
            // Clone the cell
            resultSteps.AddRange(CloneACell(posOrigin, posDestination));
        else
            // 3. Otherwise, move the cell
            resultSteps.Add(MoveACell(posOrigin, posDestination));

        // 4. Evaluate contamination around the destination 
        var contaminationSteps = ContaminateNearbyCells(posDestination,originOwner);
        resultSteps.AddRange(contaminationSteps);

        return resultSteps;
    }

    public List<Vector2Int> ReturnFreeBoxesInArea(RectInt area)
    {
        var freeBoxesPosition = new List<Vector2Int>();

        DoInArea(area, (pos, _) =>
        {
            // Utilise l’indexeur basé sur cellsBoard + Animator
            if (this[pos.x, pos.y] == BoxValue.IsFreeBox)
                freeBoxesPosition.Add(pos);
        });

        return freeBoxesPosition;
    }

    public List<Vector2Int> ReturnPlayableCellsPositions(BoxValue boxValueToIdentify)
    {
        List<Vector2Int> allCellPositionsWithBoxValueToIdentify = new();
        List<Vector2Int> candidateCells = new();

        // 1. Select all positions containing a cell of the requested type
        var recZone = new RectInt(Vector2Int.zero, new Vector2Int(NbColumns, NbRows));
        DoInArea(recZone, (pos, _) =>
        {
            if (this[pos.x, pos.y] == boxValueToIdentify)
                allCellPositionsWithBoxValueToIdentify.Add(pos);
        });

        // 2. For each cell found, check if it has at least one empty spot
        foreach (var currentPosition in allCellPositionsWithBoxValueToIdentify)
        {
            var area = new RectInt(
                currentPosition - new Vector2Int(MaxDistanceMove, MaxDistanceMove),
                new Vector2Int(MaxDistanceMove * 2 + 1, MaxDistanceMove * 2 + 1)
            );

            var hasFreeBox = false;

            DoInArea(area, (pos, _) =>
            {
                if (this[pos.x, pos.y] == BoxValue.IsFreeBox)
                    hasFreeBox = true;
            });

            if (hasFreeBox)
                candidateCells.Add(currentPosition);
        }

        // 3. Update internal lists
        if (boxValueToIdentify == BoxValue.IsUserCell)
            candidatePlayerCells = candidateCells;
        else
            candidateComputerCells = candidateCells;

        // 4. Return the corresponding list
        return candidateCells;
    }

    private GameObject InstanciateCellPrefab(Vector2Int position)
    {
        var goCell = Object.Instantiate(cellPrefab, (Vector3Int)position + new Vector3(0.5f, 0.5f, 0),
            Quaternion.identity);
        cellsBoard[position.x, position.y] = goCell;
        return goCell;
    }

    private List<CellAnimationStep> ContaminateNearbyCells(
        Vector2Int posCell,BoxValue mutateState)
    {
        var steps = new List<CellAnimationStep>();

        // 1. Contaminate nearby cells (3×3)
        var area = new RectInt(posCell - Vector2Int.one, new Vector2Int(3, 3));

        DoInArea(area, (pos, _) =>
        {
            //exclude the cell responsible for the chained mutation
            if (pos.x == posCell.x && pos.y == posCell.y) return;
            
            // Check if a cell exists
            var cellGO = cellsBoard[pos.x, pos.y];
            if (cellGO == null)
                return;

            // Read the actual type via the indexer
            var currentValue = this[pos.x, pos.y];

            // If the cell belongs to the opposite side → mutation
            if (currentValue != BoxValue.IsFreeBox && currentValue != mutateState)
                // Add a mutation animation
                steps.Add(CellAnimationStep.Chained(cellGO, TriggerNameMutate));
        });

        return steps;
    }

    private CellAnimationStep MoveACell(Vector2Int posOrigin, Vector2Int posDestination)
    {
        Debug.Log("MoveACell");

        // 1. Récupérer la cellule à déplacer
        var cellGO = cellsBoard[posOrigin.x, posOrigin.y];
        if (cellGO == null)
            throw new InvalidOperationException("MoveACell called on an empty origin cell");

        // 2. Libérer la case d'origine
        cellsBoard[posOrigin.x, posOrigin.y] = null;

        // 3. Placer la cellule dans la nouvelle case
        cellsBoard[posDestination.x, posDestination.y] = cellGO;

        // 4. Retourner un step d’animation Move
        return CellAnimationStep.Move(
            cellGO,
            posOrigin,
            posDestination
        );
    }

    private List<CellAnimationStep> CloneACell(Vector2Int posOrigin, Vector2Int posDestination)
    {
        Debug.Log("CloneACell posOrigin=" + posOrigin + "  posDestination=" + posDestination);

        var steps = new List<CellAnimationStep>();

        // 1. Retrieve the original cell GameObject
        var originCellGO = cellsBoard[posOrigin.x, posOrigin.y];
        if (originCellGO == null)
            throw new InvalidOperationException("CloneACell called with a null origin cell");

        // 2. Instantiate a new cell at the destination
        var newCellGO = InstanciateCellPrefab(posDestination);

        // 3. Add the birth / placement animation
        var triggerName = this[posOrigin.x, posOrigin.y] == BoxValue.IsUserCell
            ? TriggerNameUserCellBirth
            : TriggerNameComputerCellBirth;

        // 3 . Add the birth animation of the cell
        steps.Add(CellAnimationStep.Chained(newCellGO, triggerName));

        // 4. Copy the owner type from the original cell (Animator state will be updated by the view)
        // The model does not trigger animations; it only updates logical placement.
        cellsBoard[posDestination.x, posDestination.y] = newCellGO;

        // 5. Add a Move animation step (the view will move clone while appearing at the destination)
        steps.Add(CellAnimationStep.Move(
            newCellGO,
            posOrigin,
            posDestination
        ));

        // 6. Return the steps
        return steps;
    }
    
    
    private void ClearCellsBoard()
    {
        if (cellsBoard == null)
            return;

        for (int x = 0; x < NbColumns; x++)
        {
            for (int y = 0; y < NbRows; y++)
            {
                var cellGO = cellsBoard[x, y];
                if (cellGO != null)
                {
                    Object.Destroy(cellGO);
                    cellsBoard[x, y] = null;
                }
            }
        }
    }

}