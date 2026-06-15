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
        
        public bool hasExited = false;

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            
            if (hasExited) return; // no duplicates
            hasExited = true;
            
            //Invoke the event
            OnCellExitState?.Invoke();
            //Debug.Log("OnCellExitState called LayerIndex="+layerIndex);

        }
        
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            hasExited = false; // reset
        }
    }

}
