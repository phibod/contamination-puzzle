    using System;
    using Unity.VisualScripting;
    using UnityEngine;

    public class CellStateBehaviour : StateMachineBehaviour
    {
        public event Action OnCellExitState;
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            //Invoke the event
            OnCellExitState?.Invoke(); 
            Debug.Log("OnCellExitState called LayerIndex="+layerIndex);
            
        }
        
    }
