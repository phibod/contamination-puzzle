using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ContaminationPuzzle.Entities;
using ContaminationPuzzle.AI;
using UnityEngine.Serialization;

namespace ContaminationPuzzle.Gameplay
{
    /// <summary>
    /// AIPlayer uses ComputerStrategy's decision helpers to pick source & target cells,
    /// then asks the GameModel to apply the move and emits AnimationData for the GameManager/GameView.
    /// </summary>
    public class AIPlayer : Player
    {
        [SerializeField]
        private GameManager gameManager;

        // Delay to simulate thinking and to make selection visible in UI
        [SerializeField] 
        public float thinkTimeSeconds = 0.6f;
        
        [SerializeField] 
        public float betweenStepsDelay = 0.25f;

        private ComputerStrategy strategy;

        // Local selection state for UX highlighting
        private Vector2Int selectedCellPosition;
        private GameObject selectedCellGO;

        private void EnsureStrategy()
        {
            if (strategy == null)
            {
                if (gameManager == null || gameManager.GameModel == null)
                {
                    Debug.LogError("AIPlayer: cannot initialize strategy - owner or GameModel is null");
                    return;
                }
                strategy = new ComputerStrategy(gameManager.GameModel);
            }
        }

        /// <summary>
        /// Public entry point: ask the AI to play its turn.
        /// This will start a coroutine that:
        ///  - asks the strategy for a source cell,
        ///  - optionally highlights it,
        ///  - asks the strategy for a target free box,
        ///  - applies the move through GameModel and emits AnimationData.
        /// </summary>
        public void DoPlay()
        {
            EnsureStrategy();
            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            if (gameManager == null)
            {
                Debug.LogError("AIPlayer.DoPlay: owner not set");
                yield break;
            }

            EnsureStrategy();
            if (strategy == null)
            {
                Debug.LogError("AIPlayer.DoPlay: strategy not initialized");
                yield break;
            }

            // Simulate thinking time
            yield return new WaitForSeconds(thinkTimeSeconds);

            // 1) Identify the cell to select via strategy helper
            Debug.LogWarning("1) Identify the cell to select via strategy helper");
            if (!strategy.IdentifyCellToSelect(out var computerCell))
            {
                Debug.LogWarning("AIPlayer: IdentifyCellToSelect found nothing");
                yield break;
            }
            Debug.LogWarning("1) computerCell.x = "+ computerCell.x);
            Debug.LogWarning("1) computerCell.y = "+ computerCell.y);
            
            // Highlight for UX
            Debug.LogWarning("Highlight cell for UX");
            selectedCellPosition = computerCell;
            selectedCellGO = gameManager.GameModel.GetCellGameObject(selectedCellPosition.x, selectedCellPosition.y);
            if (selectedCellGO != null)
            {
                gameManager.GameView.SelectCell(selectedCellGO);
            }

            yield return new WaitForSeconds(betweenStepsDelay);

            // Cleanup highlight
            Debug.LogWarning("Cleanup highlight");
            if (selectedCellGO != null)
            {
                gameManager.GameView.DeselectCurrentCell(selectedCellGO);
                selectedCellGO = null;
            }
            
            // 2) Identify the free box to select via strategy helper
            Debug.LogWarning("2) Identify the free box to select via strategy helper");
            if (!strategy.IdentifyFreeBoxToSelect(computerCell, out var freeBox))
            {
                Debug.LogWarning("AIPlayer: IdentifyFreeBoxToSelect found nothing");
                
                // Notify with empty animation to avoid deadlocks
                var steps = new List<CellAnimationStep>();
                gameManager.OnPlayerAnimationRequested(new AnimationData(steps));
                yield break;
            }

            // 3) Apply the move via GameModel (strategy only decided the positions)
            Debug.LogWarning("3) Apply the move via GameModel (strategy only decided the positions)");
            Debug.LogWarning("3) computerCell.x = "+ computerCell.x);
            Debug.LogWarning("3) computerCell.y = "+ computerCell.y);
            
            var animationSteps = gameManager.GameModel.MoveOrCloneTheCell(computerCell, freeBox);

            // 4) Emit animation data so GameManager/GameView animate and continue the turn flow
            Debug.LogWarning("4) Emit animation data so GameManager/GameView animate and continue the turn flow");
            var animationData = new AnimationData(animationSteps);
            gameManager.OnPlayerAnimationRequested(animationData);
        }

        // The abstract methods can remain minimal; AI selects via strategy, not by input.
        protected override bool SelectCell()
        {
            // Not used externally in this flow; return false.
            return false;
        }

        protected override bool SelectFreeBox()
        {
            // Not used externally in this flow; return false.
            return false;
        }
    }
}