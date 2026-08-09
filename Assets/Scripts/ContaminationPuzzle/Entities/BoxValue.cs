namespace ContaminationPuzzle.Entities
{
    /// <summary>
    /// Represents the state of a cell on the game board.
    /// </summary>
    public enum BoxValue
    {
        /// <summary>User-controlled cell</summary>
        IsUserCell,
        
        /// <summary>Computer-controlled cell</summary>
        IsComputerCell,
        
        /// <summary>Empty cell available for colonization</summary>
        IsFreeBox
    }
}