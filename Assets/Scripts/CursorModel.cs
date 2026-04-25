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
