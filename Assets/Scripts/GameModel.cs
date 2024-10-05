using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class GameModel
{
    private const int NB_ROWS = 7;
    private const int NB_COLUMNS = 7;
    public static readonly int MAX_DISTANCE_MOVE = 2;
    
    public enum BoxValue
    {
        FreeBox = default,
        ComputerCell = 1,
        PlayerCell = 2,
    }


    private BoxValue[,] biologicalCellBox;
    //private readonly GameView view;

    enum SelectionType
    {
        TheMost = default,
        TheLeast = 1,
    }


    struct BoxInfos
    {
        public BoxValue boxValueSelected;
        public SelectionType chosenSelectionType;
        public BoxValue adjacentBoxValue;
        public RectInt rectZone;
        public Vector2Int positionDestination;
        public int nbAdjacentCells;
    }


    private void DoInArea(RectInt area, Action<Vector2Int, BoxValue> action)
    {
        foreach (var currentPosition in area.allPositionsWithin)
        {
            if (currentPosition.x < 0
                || currentPosition.y < 0
                || currentPosition.x >= NB_ROWS
                || currentPosition.y >= NB_ROWS) continue;

            var currentBox = this[currentPosition.x, currentPosition.y];
            action.Invoke(currentPosition, currentBox);
        }
    }

    private BoxInfos IdentifySurroundedBox(BoxInfos boxInfosCriteria)
    {
        //a copy of boxInfosCriteria
        var lastBoxSelectedInfos = boxInfosCriteria;

        bool firstCellSelected = true;
        DoInArea(boxInfosCriteria.rectZone,(pos, value) =>
        {
            if (value.Equals(boxInfosCriteria.boxValueSelected))
            {
                boxInfosCriteria.positionDestination = pos;
                ExploreAdjacentBoxes(boxInfosCriteria,pos);

                if (firstCellSelected ||
                    (boxInfosCriteria.nbAdjacentCells <= lastBoxSelectedInfos.nbAdjacentCells &&
                     boxInfosCriteria.chosenSelectionType == SelectionType.TheLeast) ||
                    (boxInfosCriteria.nbAdjacentCells >= lastBoxSelectedInfos.nbAdjacentCells &&
                     boxInfosCriteria.chosenSelectionType == SelectionType.TheMost))
                {
                    lastBoxSelectedInfos.positionDestination = boxInfosCriteria.positionDestination;
                    lastBoxSelectedInfos.nbAdjacentCells = boxInfosCriteria.nbAdjacentCells;
                    firstCellSelected = false;
                }
            }       
        });

      
        return lastBoxSelectedInfos;
    }


  
    private void ExploreAdjacentBoxes(BoxInfos boxInfosCriterias, Vector2Int centerPosition)
    {
        
        boxInfosCriterias.nbAdjacentCells = 0;
        var recZone =new RectInt(centerPosition - Vector2Int.one, new Vector2Int(3, 3));
        DoInArea(recZone,(pos, valueToIdentify) =>
        {
            //Increment the number of adjacent cells with the valueToIdentify
            var currentBoxValue= this[pos.x, pos.y];
            if (pos != centerPosition && currentBoxValue == valueToIdentify)
                boxInfosCriterias.nbAdjacentCells++;
        });
        
    }

    public event Action OnInitialize;
    public event Action<Vector2Int, Vector2Int, BoxValue> OnTileChanged;

    /// <summary>
    /// Indexer of the biologicalCellBox
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    private BoxValue this[int col, int row] => biologicalCellBox[col, row];

    private void SetTile(Vector2Int posOrigin, Vector2Int posDestination, BoxValue value)
    {
        OnTileChanged?.Invoke(posOrigin, posDestination, value);
        biologicalCellBox[posDestination.x, posDestination.y] = value;
    }

    private bool ClickInGameArea(Vector2Int clickPosition)
    {
        return (clickPosition.x >= 0 && clickPosition.x < NB_COLUMNS && clickPosition.y >= 0 &&
                clickPosition.y < NB_ROWS);
    }


    private void PlaceAndContaminateNearbyCells(Vector2Int posOrigin, Vector2Int posCell, BoxValue boxValue)
    {
        //todo : notify placement to the view.
        SetTile(posOrigin, posCell, boxValue);

        DoInArea(new RectInt(posCell - Vector2Int.one, new Vector2Int(3, 3)), (pos, value) =>
        {
            //contaminate the cell nearby
            if (value != BoxValue.FreeBox && boxValue != value)
                SetTile(posCell, pos, boxValue);
        });
    }


    private void MoveACell(Vector2Int posOrigin, Vector2Int posDestination)
    {
        Debug.Log("MoveACell");
        var previousCell = this[posOrigin.x, posOrigin.y];

        if (previousCell == BoxValue.FreeBox)
            throw new InvalidDataException($"Can't move a {nameof(BoxValue.FreeBox)} cell");

        SetTile(posOrigin, posOrigin, BoxValue.FreeBox);
        SetTile(posOrigin, posDestination, previousCell);
        PlaceAndContaminateNearbyCells(posOrigin, posDestination, previousCell);
    }

    private void CloneACell(Vector2Int posOrigin, Vector2Int posDestination, BoxValue boxValue)
    {
        Debug.Log("CloneACell");
        PlaceAndContaminateNearbyCells(posOrigin, posDestination, boxValue);
    }

    private int CountBoxesWithBoxValue(BoxValue cellValue)
    {
        var resultCount = 0;
        
        DoInArea(new RectInt(new Vector2Int(0, 0), new Vector2Int(NB_ROWS, NB_COLUMNS)), (pos, value) =>
        {
            //the current box has the value
            if (value == cellValue)
                resultCount++;
        });
    
        return resultCount;
    }
    

   
    public void InitGameModel()
    {
        OnInitialize?.Invoke();
        biologicalCellBox = new BoxValue[NB_COLUMNS, NB_ROWS];

        //two computer cells and two player cells
        var posOrigin = new Vector2Int(Mathf.CeilToInt(NB_COLUMNS / 2f), Mathf.CeilToInt(NB_ROWS / 2f));

        SetTile(posOrigin, new Vector2Int(0, 0), BoxValue.ComputerCell);
        SetTile(posOrigin, new Vector2Int(NB_COLUMNS - 1, NB_ROWS - 1), BoxValue.ComputerCell);
        SetTile(posOrigin, new Vector2Int(0, NB_ROWS - 1), BoxValue.PlayerCell);
        SetTile(posOrigin, new Vector2Int(NB_COLUMNS - 1, 0), BoxValue.PlayerCell);
    }


    public bool ABoxWithCellValueIsChosen(Vector2Int cellPosition, BoxValue boxValue)
    {
        return ClickInGameArea(cellPosition) && this[cellPosition.x, cellPosition.y] == boxValue;
    }


    public void ComputerToPlay()
    {
        BoxInfos freeBoxToSelect;

        //select the computer cell which has less adjacent cells
        var boxInfosInputCriteria = new BoxInfos
        {
            boxValueSelected = BoxValue.ComputerCell,
            adjacentBoxValue = BoxValue.ComputerCell,
            chosenSelectionType = SelectionType.TheLeast,
            rectZone = new RectInt(0, 0, NB_COLUMNS, NB_ROWS)
        };
        var computerCellToSelect = IdentifySurroundedBox(boxInfosInputCriteria);

        //select the computer cell which has less adjacent cells ?????


        //try to identify a computer cell that can be cloned 

        //potential attack
        //select the free box which as the most adjacent player cells
        boxInfosInputCriteria = new BoxInfos
        {
            boxValueSelected = BoxValue.FreeBox,
            adjacentBoxValue = BoxValue.PlayerCell,
            chosenSelectionType = SelectionType.TheMost,
            rectZone = new RectInt(computerCellToSelect.positionDestination.x - MAX_DISTANCE_MOVE,
                computerCellToSelect.positionDestination.y - MAX_DISTANCE_MOVE,
                MAX_DISTANCE_MOVE * 2,
                MAX_DISTANCE_MOVE * 2)
        };
        var freeboxCandidate1 = IdentifySurroundedBox(boxInfosInputCriteria);

        Debug.Log("Potential attack");
        Debug.Log("x =" + freeboxCandidate1.positionDestination.x);
        Debug.Log("y=" + freeboxCandidate1.positionDestination.y);
        Debug.Log("nbAdajcentCells = " + freeboxCandidate1.nbAdjacentCells);


        //last attack
        if (freeboxCandidate1.nbAdjacentCells == CountBoxesWithBoxValue(BoxValue.PlayerCell))
        {
            freeBoxToSelect = freeboxCandidate1;
            Debug.Log("last attack");
        }
        else
        {
            //potential consolidation
            //select the free box which as the most adjacent computer cells
            boxInfosInputCriteria = new BoxInfos
            {
                boxValueSelected = BoxValue.FreeBox,
                adjacentBoxValue = BoxValue.ComputerCell,
                chosenSelectionType = SelectionType.TheMost,
                rectZone = new RectInt(computerCellToSelect.positionDestination.x - MAX_DISTANCE_MOVE,
                    computerCellToSelect.positionDestination.y - MAX_DISTANCE_MOVE,
                    MAX_DISTANCE_MOVE * 2,
                    MAX_DISTANCE_MOVE * 2)
            };
            var freeBoxCandidate2 = IdentifySurroundedBox(boxInfosInputCriteria);

            Debug.Log("potential consolidation");
            Debug.Log("x =" + freeBoxCandidate2.positionDestination.x);
            Debug.Log("y=" + freeBoxCandidate2.positionDestination.y);
            Debug.Log("nbAdajcentCells = " + freeBoxCandidate2.nbAdjacentCells);


            //better to consolidate
            if (freeBoxCandidate2.nbAdjacentCells > freeboxCandidate1.nbAdjacentCells)
            {
                freeBoxToSelect = freeBoxCandidate2;
                Debug.Log("consolidation");
            }
            //attack instead 
            else
            {
                freeBoxToSelect = freeboxCandidate1;
                Debug.Log("attack");
            }
        }

        //move or clone the cell
        Debug.Log("Computer MoveOrCloneTheCell");
        MoveOrCloneTheCell(computerCellToSelect.positionDestination, freeBoxToSelect.positionDestination,
            BoxValue.ComputerCell);
    }


    public bool NoMoreBoxesWithCellValue(BoxValue cellValue)
    {
        for (var x = 0; x < NB_COLUMNS; x++)
        {
            for (var y = 0; y < NB_ROWS; y++)
            {
                var cell = this[x, y];
                if (cell == cellValue) return false;
            }
        }

        return true;
    }

    public void MoveOrCloneTheCell(Vector2Int posOrigin, Vector2Int posDestination, BoxValue cellValue)
    {
        var deltaX = Mathf.Abs(posDestination.x - posOrigin.x);
        var deltaY = Mathf.Abs(posDestination.y - posOrigin.y);

        if (deltaX <= 1 && deltaY <= 1)
            CloneACell(posOrigin, posDestination, cellValue);
        else
            MoveACell(posOrigin, posDestination);
    }
}