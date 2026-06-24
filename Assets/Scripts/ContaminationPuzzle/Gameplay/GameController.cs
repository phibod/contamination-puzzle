using System;
using ContaminationPuzzle.AI;
using ContaminationPuzzle.Entities;
using ContaminationPuzzle.UI;
using UnityEngine;

namespace ContaminationPuzzle.Gameplay
{
    /// <summary>
    /// Main game controller that orchestrates gameplay state, handles user input,
    /// and manages transitions between game states.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        /// <summary>
        /// Gets the cursor position in grid coordinates from the mouse position.
        /// </summary>
        public static Vector3Int GetCursorPositionInGrid(Grid gridOfGame)
        {
            Vector3 worldPoint = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int positionInGrid = gridOfGame.WorldToCell(worldPoint);

            return positionInGrid;
        }

        public GameState gameState;

        private Vector2Int cellUserSelectedPosition;

        private Vector2Int freeBoxSelectedPosition;

        private ComputerStrategy computerStrategy;

        [SerializeField] private Grid grid;

        [SerializeField] private GameView gameView;

        [SerializeField] private CursorView cursorView;

        [SerializeField] private GameObject cellPrefab;

        [SerializeField] private UIController uiController;

        private GameModel model;
        public GameModel gameModel => model;

        public PlayerType currentPlayerType;
        
        public event Action<AnimationData> GameBoardToAnimate;

        public bool isWaitingEndOfAnimation;

        private GameObject selectedCellGO;



        public void Init()
        {
            gameState = GameState.GameInitialized;
        }


        public PlayerType identifyPlayerType()
        {
            currentPlayerType = gameState switch
            {
                GameState.GameInitialized or GameState.WaitCellUserToBeSelected or GameState.WaitFreeBoxToBeSelected =>
                    PlayerType.User,
                GameState.ComputerReadyToPlay => PlayerType.Opponent,
                _ => currentPlayerType
            };

            return currentPlayerType;
        }
        

        private void Start()
        {
            model = new GameModel(cellPrefab);
            gameView.SetModel(model);
            gameView.Subscribe(model);
//            gameView.Subscribe(this);
            uiController.SetModel(model);
            computerStrategy = new ComputerStrategy(model);

            currentPlayerType = PlayerType.User;
            Init();
        }

        private void OnDestroy()
        {
            gameView.UnSubscribe(model);
  //          gameView.UnSubscribe(this);
        }

        private void Update()
        {
            Vector2Int clickPosition;

            //Is a modal window displayed
            var isModalMode = uiController.isModalMode;

            //no animation running 
            if (isWaitingEndOfAnimation || isModalMode || gameState == GameState.WaitToRestartOrQuit) return;
            
            //A user action is needed in these states
            if (!Input.GetMouseButtonDown(0) &&
                ( (gameState == GameState.WaitCellUserToBeSelected && model.ReturnPlayableCellsPositions(BoxValue.IsUserCell).Count > 0)
                   ||
                 gameState == GameState.WaitFreeBoxToBeSelected))
                 return;

            switch (gameState)
            {
                case GameState.GameInitialized :
                    isWaitingEndOfAnimation = true;
                    model.Init();
                    
                    gameState = GameState.WaitCellUserToBeSelected;

                    //Debug.Log("GameStateValues.waitCellUserToBeSelected");
                    break;

                case GameState.WaitCellUserToBeSelected :
                    if (model.ReturnPlayableCellsPositions(BoxValue.IsUserCell).Count == 0)
                    {
                        if (model.ReturnPlayableCellsPositions(BoxValue.IsComputerCell).Count == 0)
                        {
                            gameState = GameState.EndOfGame;
                            Debug.Log("GameStateValues.endOfGame");

                        }
                        else
                        {
                            gameState = GameState.ComputerReadyToPlay;

                        }
                    }

                    if (gameState == GameState.WaitCellUserToBeSelected)
                    {
                        clickPosition = (Vector2Int) GetCursorPositionInGrid(grid);
                        if (model.CandidateCellIsChosen(clickPosition, BoxValue.IsUserCell))
                        {
                            // Nouvelle sélection
                            SelectCellUser(clickPosition);
                            gameState = GameState.WaitFreeBoxToBeSelected;
                            //Debug.Log("GameStateValues.waitFreeBoxToBeSelected");
                        }

                    }
                    break;

                case GameState.WaitFreeBoxToBeSelected :

                    clickPosition = (Vector2Int) GetCursorPositionInGrid(grid);
                    if (model.CandidateCellIsChosen(clickPosition, BoxValue.IsFreeBox))
                    {
                        var distMove = clickPosition - cellUserSelectedPosition;
                        if (Math.Abs(distMove.x) <= GameModel.MaxDistanceMove && Math.Abs(distMove.y) <= GameModel.MaxDistanceMove)
                        {
                            //animation du move ou du clone : la cellule est désélectionnée avant l'animation
                            gameView.DeselectCurrentCell(selectedCellGO);
                            var animationSteps = model.MoveOrCloneTheCell(cellUserSelectedPosition, clickPosition);

                            //update the view
                            isWaitingEndOfAnimation = true;
                            var animationData = new AnimationData(animationSteps);
                            GameBoardToAnimate?.Invoke(animationData);

                            gameState = model.NoMoreBoxesWithCellValue(BoxValue.IsFreeBox) ||
                                        model.NoMoreBoxesWithCellValue(BoxValue.IsComputerCell)
                                ? GameState.EndOfGame
                                : GameState.ComputerReadyToPlay;

                            //Debug.Log(gameState.Equals(GameStateValues.ComputerReadyToPlay)
                             //   ? "GameStateValues.computerReadyToPlay"
                              //  : "GameStateValues.endOfGame");


                        }
                        else
                        {
                            Debug.Log("Not authorized");
                        }
                    }
                    else if (model.CandidateCellIsChosen(clickPosition, BoxValue.IsUserCell))
                    {
                        var cellGO = model.GetCellGameObject(clickPosition.x, clickPosition.y);

                        // Si l'utilisateur clique à nouveau sur la même cellule on la désélectionne
                        if (selectedCellGO == cellGO)
                        {
                            // Désélection
                            gameView.DeselectCurrentCell(selectedCellGO);
                            selectedCellGO = null;

                            // Retour à l'état précédent
                            gameState = GameState.WaitCellUserToBeSelected;
                            //Debug.Log("Retour à WaitCellUserToBeSelected");
                            break;
                        }

                        // Si l'utilisateur clique une autre cellule user jouable
                        if (selectedCellGO != null && selectedCellGO != cellGO)
                        {
                            gameView.DeselectCurrentCell(selectedCellGO);
                        }

                        // Nouvelle sélection
                        SelectCellUser(clickPosition);

                        //Debug.Log("Nouvelle cellule sélectionnée en WaitFreeBoxToBeSelected");
                    }
                    break;

                case GameState.ComputerReadyToPlay :

                    Debug.Log("Computer ready to play");    
                    if (model.ReturnPlayableCellsPositions(BoxValue.IsComputerCell).Count == 0)
                    {
                        if (model.ReturnPlayableCellsPositions(BoxValue.IsUserCell).Count == 0)
                        {
                            gameState = GameState.EndOfGame;
                        //    Debug.Log("GameStateValues.endOfGame");
                        }
                        else
                        {
                            gameState = GameState.WaitCellUserToBeSelected;
                        //    Debug.Log("GameStateValues.waitCellUserToBeSelected");
                        }
                    }

                    if (gameState == GameState.ComputerReadyToPlay)
                    {
                        var animationSteps = computerStrategy.Play();

                        //update the view
                        isWaitingEndOfAnimation = true;
                        var animationData = new AnimationData(animationSteps);
                        GameBoardToAnimate?.Invoke(animationData);

                        gameState = model.NoMoreBoxesWithCellValue(BoxValue.IsFreeBox) ||
                                    model.NoMoreBoxesWithCellValue(BoxValue.IsUserCell)
                            ? GameState.EndOfGame
                            : GameState.WaitCellUserToBeSelected;

                        //Debug.Log(gameState.Equals(GameStateValues.WaitCellUserToBeSelected)
                         //   ? "GameStateValues.waitCellUserToBeSelected"
                         //   : "GameStateValues.endOfGame");
                    }


                    break;

                case GameState.EndOfGame:
                    gameView.ShowTheCup();
                    gameState = GameState.WaitToRestartOrQuit;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }



        }

        private void SelectCellUser(Vector2Int clickPosition)
        {
            var cellGO = model.GetCellGameObject(clickPosition.x, clickPosition.y);
            selectedCellGO = cellGO;
            gameView.SelectCell(cellGO);
            cellUserSelectedPosition = clickPosition;
        }
    }
}
