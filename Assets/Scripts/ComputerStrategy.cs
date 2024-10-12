using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static GameModel;
using Random = UnityEngine.Random;


public class ComputerStrategy
{

        
    struct BoxInputSearchParameters
    {
        public BoxValue boxValueSelected;
        public BoxValue adjacentBoxValue;
        public GameModel.SelectionType chosenSelectionType;
        public RectInt rectZone;
        public List<Vector2Int> boxPositionsToExclude;
    }
    
    struct BoxOutputSearchParameters
    {
        public Boolean noBoxWithValueFound;
        public Vector2Int positionBoxFound;
        public int nbAdjacentCells;
    }
    
    
    
   private readonly GameModel gameModel;

   private int CountBoxesWithBoxValue(BoxValue cellValue)
   {
       var resultCount = 0;
        
       gameModel.DoInArea(new RectInt(new Vector2Int(0, 0), new Vector2Int(NbRows, NbColumns)), (pos, value) =>
       {
           //the current box has the value
           if (value == cellValue)
               resultCount++;
       });
    
       return resultCount;
   }

   
 
    private BoxOutputSearchParameters IdentifySurroundedBox(BoxInputSearchParameters boxInputSearchParameters)
    {
     
        BoxOutputSearchParameters boxOutputSearchParameter = new BoxOutputSearchParameters
        {
            noBoxWithValueFound = true
        };
        gameModel.DoInArea(boxInputSearchParameters.rectZone,(pos, value) =>
        {

            if (value.Equals(boxInputSearchParameters.boxValueSelected) && 
                !boxInputSearchParameters.boxPositionsToExclude.Contains(pos))
            {
                var nbAdjacentCells = ExploreAdjacentBoxes(boxInputSearchParameters,pos);

                if (boxOutputSearchParameter.noBoxWithValueFound ||
                    (nbAdjacentCells < boxOutputSearchParameter.nbAdjacentCells &&
                     boxInputSearchParameters.chosenSelectionType == GameModel.SelectionType.TheLeast) ||
                    (nbAdjacentCells > boxOutputSearchParameter.nbAdjacentCells &&
                     boxInputSearchParameters.chosenSelectionType == GameModel.SelectionType.TheMost))
                {
                    boxOutputSearchParameter.positionBoxFound = pos;
                    boxOutputSearchParameter.nbAdjacentCells = nbAdjacentCells;
                    boxOutputSearchParameter.noBoxWithValueFound = false;
                }
            }       
        });

      
        return boxOutputSearchParameter;
    }
  
    private int ExploreAdjacentBoxes(BoxInputSearchParameters boxInputSearchParametersCriteria, Vector2Int centerPosition)
    {
        
        var nbAdjacentCells = 0;
        var recZone =new RectInt(centerPosition - Vector2Int.one, new Vector2Int(3, 3));
        gameModel.DoInArea(recZone,(pos, currentBoxValue) =>
        {
            //Increment the number of adjacent cells with the valueToIdentify
            if (pos != centerPosition && currentBoxValue == boxInputSearchParametersCriteria.adjacentBoxValue)
                nbAdjacentCells++;
        });
        
        return nbAdjacentCells;
        
    }
      
    public ComputerStrategy(GameModel gameModelToUse)
    {
        gameModel = gameModelToUse;
    }
    
    
    /* Strategy1
       - 1)Select the computer cell which has less adjacent cells 
       - 2) Scan the free boxes reachable by the computer cell.
            A) Select the one which as the most adjacent computer cells (Consolidation)
            B) Select the one which as the most adjacent player cells   (Attack)    
        Choose 2.A) If a free box is surrounded by more computer cells than player cells
        Choose 2.B) If a free box is surrounded by more player cells than computer cells
                    If it is the last move to win.  
    */
    private Boolean Strategy1(ref Vector2Int computerCellToSelectPosition,ref Vector2Int freeBoxCellToSelectPosition)
    {
        BoxInputSearchParameters searchParametersFreeBox;
        BoxOutputSearchParameters computerCellToSelect,freeBoxToSelect,freeBoxCandidate1;

        //select the computer cell which has less adjacent cells
        var searchParametersComputerCell = new BoxInputSearchParameters
        {
            boxValueSelected = GameModel.BoxValue.ComputerCell,
            adjacentBoxValue = GameModel.BoxValue.ComputerCell,
            chosenSelectionType = GameModel.SelectionType.TheLeast,
            rectZone = new RectInt(0, 0, GameModel.NbColumns, GameModel.NbRows),
            boxPositionsToExclude = new List<Vector2Int>()

        };

        //a computer cell that can be moved or cloned must be selected
        do
        {
            computerCellToSelect = IdentifySurroundedBox(searchParametersComputerCell);
            searchParametersFreeBox = new BoxInputSearchParameters
            {
                boxValueSelected = GameModel.BoxValue.FreeBox,
                adjacentBoxValue = GameModel.BoxValue.PlayerCell,
                chosenSelectionType = GameModel.SelectionType.TheMost,
                rectZone = new RectInt(computerCellToSelect.positionBoxFound.x - GameModel.MaxDistanceMove,
                    computerCellToSelect.positionBoxFound.y - GameModel.MaxDistanceMove,
                    GameModel.MaxDistanceMove * 2,
                    GameModel.MaxDistanceMove * 2),
                boxPositionsToExclude = new List<Vector2Int>()

            };

        
            freeBoxCandidate1 = IdentifySurroundedBox(searchParametersFreeBox);

            //exclude a computer cell that can not be moved
            if (freeBoxCandidate1.noBoxWithValueFound)
            {
                searchParametersComputerCell.boxPositionsToExclude.Add(computerCellToSelect.positionBoxFound); 
            }
            
        } while (!computerCellToSelect.noBoxWithValueFound && freeBoxCandidate1.noBoxWithValueFound);
        
        //strategy is aborted
        if (computerCellToSelect.noBoxWithValueFound) return false;
        
        
        Debug.Log("Potential attack");
        Debug.Log("x =" + freeBoxCandidate1.positionBoxFound.x);
        Debug.Log("y=" + freeBoxCandidate1.positionBoxFound.y);
        Debug.Log("nbAdajcentCells = " + freeBoxCandidate1.nbAdjacentCells);


        //last attack
        if (freeBoxCandidate1.nbAdjacentCells == CountBoxesWithBoxValue(GameModel.BoxValue.PlayerCell))
        {
            freeBoxToSelect = freeBoxCandidate1;
            Debug.Log("last attack");
        }
        else
        {
            //potential consolidation
            //select the free box which as the most adjacent computer cells
            searchParametersFreeBox = new BoxInputSearchParameters
            {
                boxValueSelected = GameModel.BoxValue.FreeBox,
                adjacentBoxValue = GameModel.BoxValue.ComputerCell,
                chosenSelectionType = GameModel.SelectionType.TheMost,
                rectZone = new RectInt(computerCellToSelect.positionBoxFound.x - GameModel.MaxDistanceMove,
                    computerCellToSelect.positionBoxFound.y - GameModel.MaxDistanceMove,
                    GameModel.MaxDistanceMove * 2,
                    GameModel.MaxDistanceMove * 2),
                boxPositionsToExclude = new List<Vector2Int>()

            };
            var freeBoxCandidate2 = IdentifySurroundedBox(searchParametersFreeBox);

            Debug.Log("potential consolidation");
            Debug.Log("x =" + freeBoxCandidate2.positionBoxFound.x);
            Debug.Log("y=" + freeBoxCandidate2.positionBoxFound.y);
            Debug.Log("nbAdajcentCells = " + freeBoxCandidate2.nbAdjacentCells);


            //better to consolidate
            if (freeBoxCandidate2.nbAdjacentCells > freeBoxCandidate1.nbAdjacentCells)
            {
                freeBoxToSelect = freeBoxCandidate2;
                Debug.Log("consolidation");
            }
            //attack instead 
            else
            {
                freeBoxToSelect = freeBoxCandidate1;
                Debug.Log("attack");
            }
        }

        computerCellToSelectPosition = computerCellToSelect.positionBoxFound;
        freeBoxCellToSelectPosition = freeBoxToSelect.positionBoxFound;

        return true;    
   
    }

    public void Play()
    {
        
        Vector2Int computerCellToSelectPosition = new Vector2Int();
        Vector2Int freeBoxCellToSelectPosition = new Vector2Int();

        if (Strategy1(ref computerCellToSelectPosition,ref freeBoxCellToSelectPosition))
        {
            //move or clone the cell
            Debug.Log("Computer MoveOrCloneTheCell");
            gameModel.MoveOrCloneTheCell(computerCellToSelectPosition, freeBoxCellToSelectPosition,
                GameModel.BoxValue.ComputerCell);
            
        }
        else
        {
            Debug.Log("Computer stuck. No computer cell can move !!!!");
        }

    }


}