using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameModel
{
    private const int NB_ROWS = 7;
    private const int NB_COLUMNS = 7;

    public enum BoxValue
    {
        FreeBox = default,
        ComputerCell = 1,
        PlayerCell = 2,
    }

    private BoxValue[,] biologicalCellBox = new BoxValue[NB_COLUMNS, NB_ROWS];
    //private readonly GameView view;


    public event Action OnInitialize;
    public event Action<int,int,BoxValue> OnTileChanged;

    /// <summary>
    /// Indexer of the biologicalCellBox
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    private BoxValue this[int col, int row]
    {
        get => biologicalCellBox[col, row];
        set
        {
            biologicalCellBox[col, row] = value;
            OnTileChanged?.Invoke(col, row, value);
        }
    }

    private bool ClickInGameArea(Vector3Int clickPosition)
    {
        return (clickPosition.x >= 0 && clickPosition.x < NB_COLUMNS && clickPosition.y >= 0 &&
                clickPosition.y < NB_ROWS);
    }
    
    
    private void PlaceAndContaminateNearbyCells(Vector3Int posCell, BoxValue boxValue)
    {
        //todo : notify placement to the view.
        this[posCell.x, posCell.y] = boxValue;
        
        for (int x = posCell.x - 1; x <= posCell.x + 1; x++)
        {
            if (x < 0 || x >= NB_COLUMNS) 
                continue;
            for (int y = posCell.y - 1; y <= posCell.y + 1; y++)
            {
                if (y < 0 || y >= NB_ROWS)
                    continue;
                
                var cell = this[x, y];
                
                //contaminate the cell nearby
                if (cell != BoxValue.FreeBox && cell != boxValue)
                {
                    this[x, y] = boxValue;
                }
            }
        }
    }

    private void MoveACell(Vector3Int posOrigin, Vector3Int posDestination)
    {
        
        Debug.Log("MoveACell");
        var previousCell = this[posOrigin.x, posOrigin.y];

        if (previousCell == BoxValue.FreeBox)
            throw new InvalidDataException($"Can't move a {nameof(BoxValue.FreeBox)} cell");
        
        this[posOrigin.x, posOrigin.y] = BoxValue.FreeBox;
        PlaceAndContaminateNearbyCells(posDestination, previousCell);
    }
    
    private void CloneACell(Vector3Int posDestination, BoxValue boxValue)
    {
        Debug.Log("CloneACell");
        PlaceAndContaminateNearbyCells(posDestination, boxValue);
    }

    public void InitGameModel()
    {
        OnInitialize?.Invoke();
        
        //two computer cells and two player cells
        this[0, 0] = BoxValue.ComputerCell;
        this[NB_COLUMNS - 1, NB_ROWS - 1] = BoxValue.ComputerCell;
        this[0, NB_ROWS - 1] = BoxValue.PlayerCell;
        this[NB_COLUMNS - 1, 0] = BoxValue.PlayerCell;

    }
 
    
    public bool ABoxWithCellValueIsChosen(Vector3Int cellPosition, BoxValue boxValue)
    {
        return  ClickInGameArea(cellPosition) && this[cellPosition.x, cellPosition.y] == boxValue;
    }   

 
    public void ComputerToPlay()
    {
        
        if ( NoMoreBoxesWithCellValue(GameModel.BoxValue.FreeBox) 
            || NoMoreBoxesWithCellValue(GameModel.BoxValue.ComputerCell) ) return;
        
        var time = DateTime.Now;
        
        var rndX = new Unity.Mathematics.Random((uint)time.Ticks);
        var rndY = new Unity.Mathematics.Random((uint)time.Ticks);

        var computerCellSelection = new Vector3Int();
        var freeBoxSelection = new Vector3Int();
       
        do
        {
            computerCellSelection.x = rndX.NextInt(NB_COLUMNS);
            computerCellSelection.y = rndY.NextInt(NB_ROWS);
        } while (!ABoxWithCellValueIsChosen(computerCellSelection,BoxValue.ComputerCell));
        
        do
        {
            freeBoxSelection.x = rndX.NextInt(NB_COLUMNS);;
            freeBoxSelection.y = rndY.NextInt(NB_ROWS);
        } while (!ABoxWithCellValueIsChosen(freeBoxSelection,BoxValue.FreeBox));
        
        MoveOrCloneTheCell(computerCellSelection,freeBoxSelection,BoxValue.ComputerCell);
        
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

    public void MoveOrCloneTheCell(Vector3Int posOrigin,Vector3Int posDestination, BoxValue cellValue)
    {
        var deltaX = Mathf.Abs(posDestination.x- posOrigin.x);
        var deltaY = Mathf.Abs(posDestination.y- posOrigin.y);

        if ( deltaX <= 1 &&
             deltaY <= 1)
        {
            CloneACell(posDestination,
                cellValue);
        }
        else
        {
            MoveACell(posOrigin,
                posDestination);

        }
    }
}