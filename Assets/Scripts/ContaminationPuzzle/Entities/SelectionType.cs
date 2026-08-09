namespace ContaminationPuzzle.Entities
{
    /// <summary>
    /// Defines the selection criteria for identifying cells.
    /// </summary>
    public enum SelectionType
    {
        /// <summary>Select the cell with the most adjacent cells</summary>
        TheMost = default,
        
        /// <summary>Select the cell with the least adjacent cells</summary>
        TheLeast = 1
    }
}