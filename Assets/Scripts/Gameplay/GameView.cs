using System;
using System.Collections;
using System.Collections.Generic;
using ContaminationPuzzle.Entities;
using DG.Tweening;
using Unity.InferenceEngine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Sequence = DG.Tweening.Sequence;

namespace ContaminationPuzzle.Gameplay
{
    /// <summary>
    /// Handles all visual animations and view updates for the game board.
    /// Manages chained animations and DOTween sequences for cell movements and state changes.
    /// </summary>
    public class GameView : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private float moveDuration;
        [SerializeField] private GameObject winnerCupPrefab;

        private const int LayerMove = -1,
            LayerPosition = 0,
            PositionCupY = 3,
            PositionCupX = 3;

        private enum RegisterType
        {
            Register,
            UnRegister,
        }

        private GameModel gameModel;
        private int currentStepIndex;
        private IReadOnlyList<CellAnimationStep> stepsToAnimate;
        private List<CellAnimationStep> chainedAnimations;
        private int dotweenOffset;
        private AnimationData animationData;
        private GameObject gameObjectCup;
        private bool isWaitingAnimatorExit;
       
        public event Action<ScoreData> OnEndRound;

        // Next chained animation
        public void HandleCellAnimationExitState()
        {
            currentStepIndex++;
            
            Debug.Log("HandleCellAnimationExitState currentStepIndex:" + currentStepIndex);
            Debug.Log("HandleCellAnimationExitState stepsToAnimate.Count :" + stepsToAnimate.Count);

            if (currentStepIndex >= stepsToAnimate.Count || stepsToAnimate[currentStepIndex].animationType != AnimationType.ChainedAnimation)
            {
                //         Debug.Log("End animation chained indexStepAnimation="+currentStepIndex+", chainedAnimations.count="+chainedAnimations.Count);
                PlayNextStep();
            }
            else
            {
                TriggerStepAnimation();
            }
        }

        
        
        /// <summary>
        /// Subscribes to game model initialization events.
        /// </summary>
        public void Subscribe(GameModel paramGameModel)
        {
            paramGameModel.OnInitialize += ClearBoard;
        }

        /// <summary>
        /// Subscribes to game controller animation events.
        /// </summary>
        public void Subscribe(GameController gameController)
        {
            gameController.GameBoardToAnimate += AnimateGameBoard;
        }

        public void ShowTheCup()
        {
            var positionCup = new Vector3Int
            {
                x = PositionCupX,
                y = PositionCupY
            };
            if (gameObjectCup == null) gameObjectCup = (GameObject) Instantiate(winnerCupPrefab, (Vector3Int)positionCup + new Vector3(0.5f, 0.5f, 0),
                Quaternion.identity);

            var animatorCup = gameObjectCup.GetComponent<Animator>();
            var scoreData = gameModel.GetScoreData();

            //Equal scores , no winner
            if (scoreData.playerScore == scoreData.computerScore) return;
            
            //A winner
            gameObjectCup.SetActive(true);
            animatorCup.SetTrigger(scoreData.playerScore > scoreData.computerScore
                ? "BlueCupToShow"
                : "GreenCupToShow");
            
        }

        public void RemoveTheCup()
        {
            if (gameObjectCup != null) gameObjectCup.SetActive(false);
            
       
        }

        private void PlayNextStep()
        {
            // Fin de toutes les animations
            if (currentStepIndex >= stepsToAnimate.Count)
            {
                //reactivate the GameController
                Debug.Log("Fin du step d'animations");
                controller.isWaitingEndOfAnimation = false;

                //request the new scoreData
                var scoreData = gameModel.GetScoreData();

                //update the ui scores
                OnEndRound?.Invoke(scoreData);

                return;
            }

            var step = stepsToAnimate[currentStepIndex];

            if (step.animationType == AnimationType.ChainedAnimation)
            {
                PlayChainedAnimation(currentStepIndex);
                return;
            }

            // Sinon → construire une séquence DOTween jusqu'au prochain ChainedAnimation
            PlayDotweenSequence();
        }

        private void PlayChainedAnimation(int currentStepIndex)
        {
            chainedAnimations = new List<CellAnimationStep>();
            int i = currentStepIndex;
            var step = stepsToAnimate[currentStepIndex];
            while (i < stepsToAnimate.Count && step.animationType == AnimationType.ChainedAnimation)
            {
                step = stepsToAnimate[i];
                chainedAnimations.Add(step);
                i++;
            }
            TriggerStepAnimation();
        }

        private void TriggerStepAnimation()
        {
            var step = stepsToAnimate[currentStepIndex];

            if (step.animationType != AnimationType.ChainedAnimation) return;
            var animator = step.cellGO.GetComponent<Animator>();
            animator.SetTrigger(step.triggerName);
            //  Debug.Log("GO " + step.cellGO.GetEntityId() + " triggered "+step.triggerName);
        }


        private void PlayDotweenSequence()
        {
            var sequence = DOTween.Sequence();
            
            //construire la sequence dotween jusqu'à la rencontre d'une animation chainée
            dotweenOffset = 0;
            while ((currentStepIndex + dotweenOffset) < stepsToAnimate.Count &&
                   stepsToAnimate[currentStepIndex + dotweenOffset].animationType != AnimationType.ChainedAnimation)
            {
                var step = stepsToAnimate[currentStepIndex + dotweenOffset];

                // Construction du tween
                AppendMoveTween(sequence, step);
                dotweenOffset++;
            }

            // Fin de la séquence → reprendre le traitement
            sequence.OnComplete(() =>
            {
                currentStepIndex += dotweenOffset;
                Debug.Log("End of doTween sequence offset =" + dotweenOffset);
                PlayNextStep();
            });

            sequence.Play();
        }

        private void AppendMoveTween(Sequence seq, CellAnimationStep step)
        {
            var cellGO = step.cellGO;
            var dest = step.positionDestination.Value;

            seq.Append(cellGO.transform.DOMoveZ(LayerMove, 0.2f));
            seq.Append(cellGO.transform.DOMove(
                new Vector3(dest.x + 0.5f, dest.y + 0.5f, -1),
                moveDuration
            ));
            seq.Append(cellGO.transform.DOMoveZ(LayerPosition, 0.2f));
        }

        /// <summary>
        /// Triggers the selection animation for a cell.
        /// </summary>
        public void SelectCell(GameObject cellGO)
        {
            // Déclenche l'animation de sélection
            var animator = cellGO.GetComponent<Animator>();
            animator.SetTrigger("selectCell");
        }

        /// <summary>
        /// Triggers the deselection animation for a cell.
        /// </summary>
        public void DeselectCurrentCell(GameObject cellGO)
        {
            var animator = cellGO.GetComponent<Animator>();
            animator.SetTrigger("deselectCell");
        }

        /// <summary>
        /// Sets the game model for this view.
        /// </summary>
        public void SetModel(GameModel gameModel)
        {
            this.gameModel = gameModel;
        }

        /// <summary>
        /// Unsubscribes from game model events.
        /// </summary>
        public void UnSubscribe(GameModel model)
        {
            model.OnInitialize -= AnimateGameBoard;
        }

        /// <summary>
        /// Unsubscribes from game controller events.
        /// </summary>
        public void UnSubscribe(GameController gameController)
        {
            gameController.GameBoardToAnimate -= AnimateGameBoard;
        }

        private void ClearBoard(AnimationData animationClearData)
        {
            AnimateGameBoard(animationClearData);
        }

        private void AnimateGameBoard(AnimationData obj)
        {
            this.animationData = obj;
            stepsToAnimate = obj.animations;
            currentStepIndex = 0;
            PlayNextStep();
        }

    }
}
