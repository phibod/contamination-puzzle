using System;
using System.Collections.Generic;
using ContaminationPuzzle.Entities;
using ContaminationPuzzle.Gameplay;
using UnityEngine;

namespace ContaminationPuzzle.AI
{
    /// <summary>
    /// Implements the computer player's strategy for the contamination puzzle game.
    /// Uses a defensive/offensive algorithm to select cells and target positions.
    /// </summary>
    public class ComputerStrategy
    {
        struct BoxInputSearchParameters
        {
            public BoxValue adjacentBoxValue;
            public SelectionType chosenSelectionType;
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

            gameModel.DoInArea(new RectInt(new Vector2Int(0, 0), new Vector2Int(GameModel.NbRows, GameModel.NbColumns)), (pos, value) =>
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
            BoxOutputSearchParameters boxOutputSearchParameter = new BoxOutputSearchParameters { };

            foreach (var currentPosition in boxInputSearchParameters.boxPositionsCandidates)
            {


                var nbAdjacentCells = ExploreAdjacentBoxes(boxInputSearchParameters, currentPosition);

                if (firstBoxFound ||
                    (nbAdjacentCells < boxOutputSearchParameter.nbAdjacentCells &&
                     boxInputSearchParameters.chosenSelectionType == SelectionType.TheLeast) ||
                    (nbAdjacentCells > boxOutputSearchParameter.nbAdjacentCells &&
                     boxInputSearchParameters.chosenSelectionType == SelectionType.TheMost))
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
            var recZone = new RectInt(centerPosition - Vector2Int.one, new Vector2Int(3, 3));
            gameModel.DoInArea(recZone, (pos, currentBoxValue) =>
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

        // PUBLIC API: identify the computer cell to select (step 1).
        // Returns true and sets computerCellToSelectPosition when a candidate was found.
        //TODO loop situation to avoid
        public bool IdentifyCellToSelect(out Vector2Int computerCellToSelectPosition)
        {
            computerCellToSelectPosition = new Vector2Int();

            var searchParametersComputerCell = new BoxInputSearchParameters
            {
                adjacentBoxValue = BoxValue.IsComputerCell,
                chosenSelectionType = SelectionType.TheLeast,
                boxPositionsCandidates = gameModel.ReturnPlayableCellsPositions(BoxValue.IsComputerCell)
            };

            if (searchParametersComputerCell.boxPositionsCandidates == null || searchParametersComputerCell.boxPositionsCandidates.Count == 0)
                return false;

            var computerCellToSelect = IdentifySurroundedBox(searchParametersComputerCell);
            computerCellToSelectPosition = computerCellToSelect.positionBoxFound;
            return true;
        }

        // PUBLIC API: identify the free box to select (step 2), given the previously identified computer cell.
        // Returns true and sets freeBoxCellToSelectPosition when a candidate was found.
        public bool IdentifyFreeBoxToSelect(Vector2Int computerCellToSelectPosition, out Vector2Int freeBoxCellToSelectPosition)
        {
            freeBoxCellToSelectPosition = new Vector2Int();

            // Define search zone centered on the chosen computer cell
            var rectZone = new RectInt(computerCellToSelectPosition.x - GameModel.MaxDistanceMove,
                computerCellToSelectPosition.y - GameModel.MaxDistanceMove,
                GameModel.MaxDistanceMove * 2 + 1,
                GameModel.MaxDistanceMove * 2 + 1);

            // First candidate: free box which has the most adjacent player cells (attack)
            var searchParametersFreeBox = new BoxInputSearchParameters
            {
                adjacentBoxValue = BoxValue.IsUserCell,
                chosenSelectionType = SelectionType.TheMost,
                boxPositionsCandidates = gameModel.ReturnFreeBoxesInArea(rectZone)
            };

            if (searchParametersFreeBox.boxPositionsCandidates == null || searchParametersFreeBox.boxPositionsCandidates.Count == 0)
            {
                // no free boxes in area
                return false;
            }

            var freeBoxCandidate1 = IdentifySurroundedBox(searchParametersFreeBox);

            // Last attack check
            if (freeBoxCandidate1.nbAdjacentCells == CountBoxesWithBoxValue(BoxValue.IsUserCell))
            {
                freeBoxCellToSelectPosition = freeBoxCandidate1.positionBoxFound;
                return true;
            }
            else
            {
                // Potential consolidation: free box with the most adjacent computer cells
                searchParametersFreeBox = new BoxInputSearchParameters
                {
                    adjacentBoxValue = BoxValue.IsComputerCell,
                    chosenSelectionType = SelectionType.TheMost,
                    boxPositionsCandidates = gameModel.ReturnFreeBoxesInArea(rectZone)
                };

                var freeBoxCandidate2 = IdentifySurroundedBox(searchParametersFreeBox);

                // Choose the better candidate: consolidation vs attack
                if (freeBoxCandidate2.nbAdjacentCells > freeBoxCandidate1.nbAdjacentCells)
                {
                    freeBoxCellToSelectPosition = freeBoxCandidate2.positionBoxFound;
                }
                else
                {
                    freeBoxCellToSelectPosition = freeBoxCandidate1.positionBoxFound;
                }

                return true;
            }
        }

        // Keep Play for backward-compatibility: it uses the new public helpers internally.
        public List<CellAnimationStep> Play()
        {
            List<CellAnimationStep> steps = null;

            Vector2Int computerCellToSelectPosition = new Vector2Int();
            Vector2Int freeBoxCellToSelectPosition = new Vector2Int();

            if (IdentifyCellToSelect(out computerCellToSelectPosition))
            {
                if (IdentifyFreeBoxToSelect(computerCellToSelectPosition, out freeBoxCellToSelectPosition))
                {
                    steps = gameModel.MoveOrCloneTheCell(computerCellToSelectPosition, freeBoxCellToSelectPosition);
                }
                else
                {
                    Debug.Log("ComputerStrategy.Play: no free box candidate found.");
                }
            }
            else
            {
                Debug.Log("ComputerStrategy.Play: no computer cell candidate found.");
            }

            return steps;
        }

        // (other private helpers remain unchanged)
    }
}