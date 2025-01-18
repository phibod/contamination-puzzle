using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class CursorModel
{
        
    public enum CursorState
    {
        Idle = default,
        OutOfGameArea = 1,
        ValidPlayerCellPointed = 2,
        InvalidPlayerCellPointed = 3,
        ValidFreeBoxPointed = 4,
        InvalidFreeBoxPointed = 5,
    }
    
}
