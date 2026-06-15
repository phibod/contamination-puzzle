namespace ContaminationPuzzle.Entities
{
    public enum GameState
    {
        GameInitialized = default,
        WaitCellUserToBeSelected = 1,
        WaitFreeBoxToBeSelected = 2,
        ComputerReadyToPlay = 3,
        EndOfGame = 4,
        WaitToRestartOrQuit = 5,
    }
}