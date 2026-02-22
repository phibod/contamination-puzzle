using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class GameView : MonoBehaviour
{
    [SerializeField] private GameController controller;
  
    [SerializeField] private float moveDuration;

    private const int LayerMove = -1,
                      LayerPosition = 1;


    private enum RegisterType {
        Register,
        UnRegister,
    }

    private GameModel model;

    private int currentStepIndex;
 
    private IReadOnlyList<CellAnimationStep> stepsToAnimate;
    private List<CellAnimationStep> chainedAnimations;
    private int dotweenOffset;

    public void Subscribe(GameModel paramGameModel)
    {
       paramGameModel.OnInitialize += ClearBoard;
    }
    
    public void Subscribe(GameController gameController)
    {
        gameController.GameBoardToAnimate += AnimateGameBoard;
    }

    //Register or unregister the cells to a chained animation
    private void RegisterOrUnregisterCells(IReadOnlyList<CellAnimationStep> steps,RegisterType registerType )
    {
        foreach (var step in steps)
        {
            var handlers = step.cellGO.GetComponent<Animator>().GetBehaviours<CellStateBehaviour>();
            foreach (var handler in handlers)
            {
                if (registerType == RegisterType.Register) handler.OnCellExitState += HandleCellAnimationExitState;
                else handler.OnCellExitState -= HandleCellAnimationExitState;

            }
        }
    }
    
    private void PlayNextStep()
    {
        
        // Fin de toutes les animations
        if (currentStepIndex >= stepsToAnimate.Count)
        {
            controller.isWaitingEndOfAnimation = false;
            return;
        }

        var step = stepsToAnimate[currentStepIndex];

        if (step.animationType == AnimationType.ChainedAnimation)
        {
            
            PlayChainedAnimation(currentStepIndex);
            return;
        }

        // Sinon → construire une séquence DOTween jusqu’au prochain ChainedAnimation
        PlayDotweenSequence();
    }


    private void PlayChainedAnimation(int currentStepIndex)
    {

        chainedAnimations = new List<CellAnimationStep>();
        int i = currentStepIndex;
        var step= stepsToAnimate[currentStepIndex];
        while (i < stepsToAnimate.Count && step.animationType == AnimationType.ChainedAnimation)
        {
            step= stepsToAnimate[i];
            chainedAnimations.Add(step);
            i++;
        }
        RegisterOrUnregisterCells(chainedAnimations, RegisterType.Register);
        TriggerStepAnimation();
    }
    
    private void TriggerStepAnimation()
    {
        var step = stepsToAnimate[currentStepIndex];

        if (step.animationType != AnimationType.ChainedAnimation) return;
        var animator = step.cellGO.GetComponent<Animator>();
        animator.SetTrigger(step.triggerName);
        Debug.Log("GO " + step.cellGO.GetEntityId() + " triggered "+step.triggerName);
    }

    // Next chained animation
    private void HandleCellAnimationExitState()
    {
        currentStepIndex++;
        Debug.Log("HandleCellAnimationExitState currentStepIndex="+currentStepIndex);
        if (currentStepIndex >= stepsToAnimate.Count || stepsToAnimate[currentStepIndex].animationType != AnimationType.ChainedAnimation )
        {
            Debug.Log("End animation chained indexStepAnimation="+currentStepIndex+", chainedAnimations.count="+chainedAnimations.Count);
            RegisterOrUnregisterCells(chainedAnimations,RegisterType.UnRegister);
            PlayNextStep();
        }
        else
        {
            TriggerStepAnimation();
        }


    }

    private void PlayDotweenSequence()
    {
        var sequence = DOTween.Sequence();
        dotweenOffset = 0;        
        while ((currentStepIndex + dotweenOffset) < stepsToAnimate.Count &&
               stepsToAnimate[currentStepIndex+dotweenOffset].animationType != AnimationType.ChainedAnimation)
        {
            var step = stepsToAnimate[currentStepIndex];

            // Construction du tween
            AppendMoveTween(sequence, step);

            dotweenOffset++;
        }

        // Fin de la séquence → reprendre le traitement
        sequence.OnComplete(() =>
        {
            currentStepIndex+=dotweenOffset;
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
    
    
    public void SelectCell(GameObject cellGO)
    {
 
        // Déclenche l’animation de sélection
        var animator = cellGO.GetComponent<Animator>();
        animator.SetTrigger("selectCell");
    }
    
    public void DeselectCurrentCell(GameObject cellGO)
    {
        var animator = cellGO.GetComponent<Animator>();
        animator.SetTrigger("deselectCell");
    }
    
 
    public void UnSubscribe(GameModel model)
    {
        model.OnInitialize -= AnimateGameBoard;
    }
    
    public void UnSubscribe(GameController gameController)
    {
        gameController.GameBoardToAnimate -= AnimateGameBoard;
    }

    private void ClearBoard(AnimationData animationData)
    {
        AnimateGameBoard(animationData);
    }

    private void AnimateGameBoard(AnimationData obj)
    {
        stepsToAnimate = obj.animations;
        currentStepIndex = 0;
        PlayNextStep();

    }
    
}