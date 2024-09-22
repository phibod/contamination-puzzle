using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameModel
{
    private const int NB_ROWS = 7;
    private const int NB_COLUMNS = 7;

    private enum BoxSurroundedCriteria
    {
        TheMost = default,
        TheLess = 1,
    }

    public enum BoxValue
    {
        FreeBox = default,
        ComputerCell = 1,
        PlayerCell = 2,
    }


    private BoxValue[,] biologicalCellBox = new BoxValue[NB_COLUMNS, NB_ROWS];
    //private readonly GameView view;

    enum SelectionType
    {
        TheMost = default,
        TheLess = 1,
    }
    
  
    struct BoxInfos {
        public BoxValue boxValueSelected;
        public SelectionType chosenSelectionType;
        public BoxValue adjacentBoxValue;
        public Vector3Int position;
        public int nbAdjacentCells;
    }

    
    

    private BoxInfos IdentifySurroundedBox(BoxInfos boxInfosInputCriterias )
    {
        BoxInfos currentBoxSelectedInfos = new BoxInfos(),
                lastBoxSelectedInfos = new BoxInfos();
        currentBoxSelectedInfos.boxValueSelected = boxInfosInputCriterias.boxValueSelected;
        lastBoxSelectedInfos.boxValueSelected = boxInfosInputCriterias.boxValueSelected;

        int x, y;
        bool firstCellSelected = true;

        for (x = 0; x < NB_COLUMNS; x++)
        {
            for (y = 0; y < NB_ROWS; y++)
            {
                var currentBox = this[x, y];
                if (currentBox.Equals(boxInfosInputCriterias.boxValueSelected))
                {
                    boxInfosInputCriterias.position = new Vector3Int(x,y);
                    currentBoxSelectedInfos = ExploreAdjacentBoxes(boxInfosInputCriterias, x, y);
                    if (firstCellSelected ||    
                        (currentBoxSelectedInfos.nbAdjacentCells < lastBoxSelectedInfos.nbAdjacentCells &&
                         boxInfosInputCriterias.chosenSelectionType == SelectionType.TheLess) ||
                        (currentBoxSelectedInfos.nbAdjacentCells > lastBoxSelectedInfos.nbAdjacentCells &&
                         boxInfosInputCriterias.chosenSelectionType == SelectionType.TheMost))
                    {
                        lastBoxSelectedInfos = currentBoxSelectedInfos;
                        firstCellSelected = false;
                    }
                    
                }
            }
        }

        return lastBoxSelectedInfos;

    }

    private BoxInfos ExploreAdjacentBoxes(BoxInfos boxInfosInputCriterias, int x, int y)
    {
        BoxInfos currentBoxSelectedInfos = boxInfosInputCriterias;
        currentBoxSelectedInfos.nbAdjacentCells = 0;
        for (var xBoxAdjacent = x - 1; xBoxAdjacent <= x + 1; xBoxAdjacent++)
        {
            if (xBoxAdjacent < 0 || xBoxAdjacent >= NB_COLUMNS) continue;
            for (var yBoxAdjacent = y - 1; yBoxAdjacent <= y + 1; yBoxAdjacent++)
            {
                if (yBoxAdjacent < 0 || yBoxAdjacent >= NB_ROWS) continue;
                var adjacentBox = this[xBoxAdjacent, yBoxAdjacent];
                if (adjacentBox.Equals(boxInfosInputCriterias.adjacentBoxValue))
                {
                    currentBoxSelectedInfos.nbAdjacentCells++;
                            
                }
            }
        }

        return currentBoxSelectedInfos;
    }


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

    
    private int countBoxesWithBoxValue(BoxValue cellValue)
    {
        var resultCount = 0;
        for (var x = 0; x < NB_COLUMNS; x++)
        {
            for (var y = 0; y < NB_ROWS; y++)
            {
                var cell = this[x, y];
                if (cell == cellValue) resultCount++;
            }
        }
        return resultCount;
        
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
        BoxInfos freeBoxToSelect;
        
        //select the computer cell which has less adjacent cells
        var boxInfosInputCriteria = new BoxInfos
        {
            boxValueSelected = BoxValue.ComputerCell,
            adjacentBoxValue = BoxValue.ComputerCell,
            chosenSelectionType = SelectionType.TheLess
        };
        var computerCellToSelect = IdentifySurroundedBox(boxInfosInputCriteria);


        //potential attack
        //select the free box which as the most adjacent player cells
        boxInfosInputCriteria = new BoxInfos
        {
            boxValueSelected = BoxValue.FreeBox,
            adjacentBoxValue = BoxValue.PlayerCell,
            chosenSelectionType = SelectionType.TheMost
        };
        var freeboxCandidate1 = IdentifySurroundedBox(boxInfosInputCriteria);

        //last attack
        if (freeboxCandidate1.nbAdjacentCells == countBoxesWithBoxValue(BoxValue.PlayerCell))
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
                chosenSelectionType = SelectionType.TheMost
            };
            var freeBoxCandidate2 = IdentifySurroundedBox(boxInfosInputCriteria);

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
        MoveOrCloneTheCell(computerCellToSelect.position,
            freeBoxToSelect.position,
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