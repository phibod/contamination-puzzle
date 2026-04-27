using System;
using UnityEngine;

namespace ContaminationPuzzle.Gameplay
{
    /// <summary>
    /// State machine behavior for handling cell animation state transitions.
    /// Notifies when a cell exits an animation state to orchestrate animation sequences.
    /// </summary>
    public class CellStateBehaviour : StateMachineBehaviour
    {
        public event Action OnCellExitState;

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            //Invoke the event
            OnCellExitState?.Invoke();
            //Debug.Log("OnCellExitState called LayerIndex="+layerIndex);

        }

    }
}