using System;
using ContaminationPuzzle.Entities;
using UnityEngine;

namespace ContaminationPuzzle.Gameplay
{
    public abstract class Player : MonoBehaviour
    {
        protected abstract bool SelectCell();

        protected abstract bool SelectFreeBox();

    }
}