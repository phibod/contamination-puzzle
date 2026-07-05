namespace ContaminationPuzzle.Entities
{
    public record GameMode
    {
        public enum Options
        {
            Solo,
            TwoPlayers,
            NoSelection
        }

        public Options Value { get; set; } = Options.NoSelection;
    }

}