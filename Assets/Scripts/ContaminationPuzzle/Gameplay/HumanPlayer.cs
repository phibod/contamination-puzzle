using System;
using UnityEngine;
using ContaminationPuzzle.Entities;
using ContaminationPuzzle.UI;
using UnityEngine.Serialization;

namespace ContaminationPuzzle.Gameplay
{
    /// <summary>
    /// HumanPlayer handles user input to select a player cell and a free box to move/clone into.
    /// When a valid move is performed, it invokes GameBoardToAnimate with the AnimationData.
    /// </summary>
    public class HumanPlayer : Player
    {
        [SerializeField]
        private GameManager gameManager;

        [SerializeField]
        public UnityEngine.Grid grid;

        public BoxValue candidateCell;

        // Local selection state
        private Vector2Int selectedCellPosition;
        private GameObject selectedCellGO;

 
        // --- Implementations of abstract methods from Player ---
        protected override bool SelectCell()
        {
            // Called internally (not used directly from outside in this implementation),
            // keep implementation consistent with TryHandleSelectCell for potential reuse.
            if (gameManager == null || gameManager.GameModel == null || grid == null) return false;

            Vector2Int clickPos = (Vector2Int)gameManager.GetCursorPositionInGrid(grid);
            return TryHandleSelectCell(clickPos);
        }

        protected override bool SelectFreeBox()
        {
            if (gameManager == null || gameManager.GameModel == null || grid == null) return false;

            Vector2Int clickPos = (Vector2Int)gameManager.GetCursorPositionInGrid(grid);
            return TryHandleSelectFreeBox(clickPos);
        }

        private Vector3Int GetCursorGridPosition()
        {
            // Convert mouse position to grid coordinates using the assigned grid.
            Vector3 worldPoint = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
            return grid.WorldToCell(worldPoint);
        }

        //TODO Second human player is stuck and cannot select a cell
        private void Update()
        {
            // Guard clauses
            if (!enabled) return;
            if (gameManager == null) return;

            // Prevent input while an animation is running or a modal UI is active.
            if (gameManager.IsWaitingEndOfAnimation || gameManager.UIController.isModalMode) return;

            // Process only on click
            if (!Input.GetMouseButtonDown(0)) return;
            Vector2Int clickPosition = (Vector2Int)gameManager.GetCursorPositionInGrid(grid);
            Debug.Log("GetMouseButtonDown x= " + clickPosition.x + " y = " + clickPosition.y);
            var model = gameManager.GameModel;
            if (model == null) return;
            
           
 
            // If clicked a player-owned playable cell -> select/deselect
            if (model.CandidateCellIsChosen(clickPosition, candidateCell))
            {
                TryHandleSelectCell(clickPosition);
                return;
            }

            // If clicked a free box and we have a selected cell -> try move/clone
            if (model.CandidateCellIsChosen(clickPosition, BoxValue.IsFreeBox) && selectedCellGO != null)
            {
                // Attempt the move (will invoke GameBoardToAnimate on success)
                bool applied = TryHandleSelectFreeBox(clickPosition);
                if (!applied)
                {
                    // invalid move -> nothing else (user keeps selection)
                }
            }
        }

        /// <summary>
        /// Try to select a cell owned by this player.
        /// Returns true if a selection/deselection occurred.
        /// </summary>
        private bool TryHandleSelectCell(Vector2Int clickPosition)
        {
            var model = gameManager.GameModel;
            if (model == null) return false;

            if (!model.CandidateCellIsChosen(clickPosition, candidateCell)) return false;

            var cellGO = model.GetCellGameObject(clickPosition.x, clickPosition.y);

            // If clicked same selected cell -> deselect
            if (selectedCellGO == cellGO)
            {
                gameManager.GameView.DeselectCurrentCell(selectedCellGO);
                selectedCellGO = null;
                return true;
            }

            // Deselect previous selection if any
            if (selectedCellGO != null && selectedCellGO != cellGO)
            {
                gameManager.GameView.DeselectCurrentCell(selectedCellGO);
            }

            // New selection
            selectedCellGO = cellGO;
            gameManager.GameView.SelectCell(cellGO);
            selectedCellPosition = clickPosition;
            return true;
        }

        /// <summary>
        /// Try to apply a move to a free box. If valid, invoke GameBoardToAnimate and return true.
        /// </summary>
        private bool TryHandleSelectFreeBox(Vector2Int clickPosition)
        {
            var model = gameManager.GameModel;
            if (model == null) return false;

            if (!model.CandidateCellIsChosen(clickPosition, BoxValue.IsFreeBox)) return false;

            var distMove = clickPosition - selectedCellPosition;
            if (Math.Abs(distMove.x) <= GameModel.MaxDistanceMove && Math.Abs(distMove.y) <= GameModel.MaxDistanceMove)
            {
                // Deselect before animation (same behavior as original GameController)
                gameManager.GameView.DeselectCurrentCell(selectedCellGO);
                selectedCellGO = null;

                var animationSteps = model.MoveOrCloneTheCell(selectedCellPosition, clickPosition);

                // Notify listeners (GameManager forwards this to GameView)
                var animationData = new AnimationData(animationSteps);
                gameManager.OnPlayerAnimationRequested(animationData);

                return true;
            }
            else
            {
                Debug.Log("Not authorized (distance too large)");
                return false;
            }
        }

        /// <summary>
        /// Reset the selection state (useful when switching players).
        /// </summary>
        public void ResetSelection()
        {
            if (selectedCellGO != null && gameManager != null)
            {
                gameManager.GameView.DeselectCurrentCell(selectedCellGO);
            }

            selectedCellGO = null;
        }
    }
}