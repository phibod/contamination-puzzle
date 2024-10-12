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
        
    public enum BoxValue
    {
        FreeBox = default,
        ComputerCell = 1,
        PlayerCell = 2,
    }

    public enum SelectionType
    {
        TheMost = default,
        TheLeast = 1,
    }

    
    public const int NbRows = 7;
    public const int NbColumns = 7;
    public const int MaxDistanceMove = 2;
    
    private BoxValue[,] biologicalCellBox;
    //private readonly GameView view;
       
    public event Action OnInitialize;
    public event Action<Vector2Int, Vector2Int, BoxValue> OnTileChanged;

    
    private void SetTile(Vector2Int posOrigin, Vector2Int posDestination, BoxValue value)
    {
        OnTileChanged?.Invoke(posOrigin, posDestination, value);
        biologicalCellBox[posDestination.x, posDestination.y] = value;
    }

    private bool ClickInGameArea(Vector2Int clickPosition)
    {
        return (clickPosition.x >= 0 && clickPosition.x < NbColumns && clickPosition.y >= 0 &&
                clickPosition.y < NbRows);
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

    public void DoInArea(RectInt area, Action<Vector2Int, BoxValue> action)
    {
        foreach (var currentPosition in area.allPositionsWithin)
        {
            if (currentPosition.x < 0
                || currentPosition.y < 0
                || currentPosition.x >= NbRows
                || currentPosition.y >= NbRows) continue;

            var currentBox = this[currentPosition.x, currentPosition.y];
            action.Invoke(currentPosition, currentBox);
        }
    }

    /// <summary>
    /// Indexer of the biologicalCellBox
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    public BoxValue this[int col, int row] => biologicalCellBox[col, row];
 
   
    public void InitGameModel()
    {
        OnInitialize?.Invoke();
        biologicalCellBox = new BoxValue[NbColumns, NbRows];

        //two computer cells and two player cells
        var posOrigin = new Vector2Int(Mathf.CeilToInt(NbColumns / 2f), Mathf.CeilToInt(NbRows / 2f));

        SetTile(posOrigin, new Vector2Int(0, 0), BoxValue.ComputerCell);
        SetTile(posOrigin, new Vector2Int(NbColumns - 1, NbRows - 1), BoxValue.ComputerCell);
        SetTile(posOrigin, new Vector2Int(0, NbRows - 1), BoxValue.PlayerCell);
        SetTile(posOrigin, new Vector2Int(NbColumns - 1, 0), BoxValue.PlayerCell);
    }

    public bool ABoxWithCellValueIsChosen(Vector2Int cellPosition, BoxValue boxValue)
    {
        return ClickInGameArea(cellPosition) && this[cellPosition.x, cellPosition.y] == boxValue;
    }

    public bool NoMoreBoxesWithCellValue(BoxValue cellValue)
    {
        for (var x = 0; x < NbColumns; x++)
        {
            for (var y = 0; y < NbRows; y++)
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