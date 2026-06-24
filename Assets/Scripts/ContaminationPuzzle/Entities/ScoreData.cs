namespace ContaminationPuzzle.Entities
{
    /// <summary>
    /// Represents the current game score for both players.
    /// </summary>
    public record ScoreData(int playerScore, int computerScore)
    {
        /// <summary>Number of cells controlled by the player</summary>
        public int playerScore { get; } = playerScore;
        
        /// <summary>Number of cells controlled by the computer</summary>
        public int computerScore { get; } = computerScore;
    }
}