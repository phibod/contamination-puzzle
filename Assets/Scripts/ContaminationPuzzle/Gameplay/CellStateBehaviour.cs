using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ContaminationPuzzle.Gameplay
{
    /// <summary>
    /// State machine behavior for handling cell animation state transitions.
    /// Notifies when a cell exits an animation state to orchestrate animation sequences.
    /// </summary>
    public class CellStateBehaviour : StateMachineBehaviour
    {
       
        private GameView gameView;

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (gameView == null) gameView = Object.FindAnyObjectByType<GameView>();
            gameView.HandleCellAnimationExitState();

        }
       
    }

}
