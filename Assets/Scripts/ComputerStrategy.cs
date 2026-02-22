using System;
using System.Collections.Generic;
using UnityEngine;
using static GameModel;


public class ComputerStrategy
{

        
    struct BoxInputSearchParameters
    {
        public BoxValue adjacentBoxValue;
        public GameModel.SelectionType chosenSelectionType;
        public List<Vector2Int> boxPositionsCandidates;
    }
    
    struct BoxOutputSearchParameters
    {
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

        Boolean firstBoxFound = true;
        BoxOutputSearchParameters boxOutputSearchParameter = new BoxOutputSearchParameters{};
       
        foreach (var currentPosition in boxInputSearchParameters.boxPositionsCandidates)
        {


            var nbAdjacentCells = ExploreAdjacentBoxes(boxInputSearchParameters,currentPosition);

            if (firstBoxFound ||
                (nbAdjacentCells < boxOutputSearchParameter.nbAdjacentCells &&
                 boxInputSearchParameters.chosenSelectionType == GameModel.SelectionType.TheLeast) ||
                (nbAdjacentCells > boxOutputSearchParameter.nbAdjacentCells &&
                 boxInputSearchParameters.chosenSelectionType == GameModel.SelectionType.TheMost))
            {
                boxOutputSearchParameter.positionBoxFound = currentPosition;
                boxOutputSearchParameter.nbAdjacentCells = nbAdjacentCells;
                firstBoxFound = false;
            }
          
            
        }
      
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
        BoxOutputSearchParameters freeBoxToSelect;

        //select the computer cell which has less adjacent cells
        var searchParametersComputerCell = new BoxInputSearchParameters
        {
            adjacentBoxValue = BoxValue.IsComputerCell,
            chosenSelectionType = GameModel.SelectionType.TheLeast,
            boxPositionsCandidates = gameModel.ReturnPlayableCellsPositions(BoxValue.IsComputerCell)
        };
        
        var computerCellToSelect = IdentifySurroundedBox(searchParametersComputerCell);

        //select the freebox which has the most adjacent player cells
        var rectZone = new RectInt(computerCellToSelect.positionBoxFound.x - GameModel.MaxDistanceMove,
            computerCellToSelect.positionBoxFound.y - GameModel.MaxDistanceMove,
            GameModel.MaxDistanceMove * 2 + 1,
            GameModel.MaxDistanceMove * 2 + 1 );
        var searchParametersFreeBox = new BoxInputSearchParameters
        {
            adjacentBoxValue = BoxValue.IsUserCell,
            chosenSelectionType = GameModel.SelectionType.TheMost,
            boxPositionsCandidates = gameModel.ReturnFreeBoxesInArea(rectZone)

        };
        var freeBoxCandidate1 = IdentifySurroundedBox(searchParametersFreeBox);
        
        Debug.Log("Potential attack");
        Debug.Log("x =" + freeBoxCandidate1.positionBoxFound.x);
        Debug.Log("y=" + freeBoxCandidate1.positionBoxFound.y);
        Debug.Log("nbAdajcentCells = " + freeBoxCandidate1.nbAdjacentCells);

        //last attack
        if (freeBoxCandidate1.nbAdjacentCells == CountBoxesWithBoxValue(GameModel.BoxValue.IsUserCell))
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
                adjacentBoxValue = BoxValue.IsComputerCell,
                chosenSelectionType = GameModel.SelectionType.TheMost,
                boxPositionsCandidates =gameModel.ReturnFreeBoxesInArea(rectZone)

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

    public List<CellAnimationStep> Play()
    {

        List<CellAnimationStep> steps = null;
        
        Vector2Int computerCellToSelectPosition = new Vector2Int();
        Vector2Int freeBoxCellToSelectPosition = new Vector2Int();

        
        if (Strategy1(computerCellToSelectPosition: ref computerCellToSelectPosition,ref freeBoxCellToSelectPosition))
        {
            //move or clone the cell
            Debug.Log("Computer MoveOrCloneTheCell");
            steps = gameModel.MoveOrCloneTheCell(computerCellToSelectPosition, freeBoxCellToSelectPosition);

        }
        else
        {
            Debug.Log("Computer stuck. No computer cell can move !!!!");
        }

        return steps;

    }


}