namespace ContaminationPuzzle.Entities
{
    public record GameMode
    {
        public enum Options
        {
            Solo,
            TwoPlayers,
        }

        public Options value { get; set; } = Options.Solo;
        
    }

}