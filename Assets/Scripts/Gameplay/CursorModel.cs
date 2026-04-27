namespace ContaminationPuzzle.Gameplay
{
    /// <summary>
    /// Model for cursor state management.
    /// </summary>
    public class CursorModel
    {
        /// <summary>
        /// Defines the possible states of the cursor based on its position and context.
        /// </summary>
        public enum CursorState
        {
            /// <summary>Cursor is idle</summary>
            Idle = default,
            
            /// <summary>Cursor is outside the game board area</summary>
            OutOfGameArea = 1,
            
            /// <summary>Cursor points to a valid player cell</summary>
            ValidPlayerCellPointed = 2,
            
            /// <summary>Cursor points to an invalid player cell (cannot be moved)</summary>
            InvalidPlayerCellPointed = 3,
            
            /// <summary>Cursor points to a valid free box</summary>
            ValidFreeBoxPointed = 4,
            
            /// <summary>Cursor points to an invalid free box (out of range)</summary>
            InvalidFreeBoxPointed = 5,
        }
    }
}