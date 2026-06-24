using System;
using ContaminationPuzzle.Entities;
using UnityEngine;

namespace ContaminationPuzzle.Gameplay
{
    public abstract class Player : MonoBehaviour
    {
        public event Action<AnimationData> GameBoardToAnimate;

        protected abstract Boolean SelectCell();

        protected abstract Boolean SelectFreeBox();

    }
}