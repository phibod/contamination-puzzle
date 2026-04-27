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

        private GameStateValues gameState;

        private Vector2Int cellUserSelectedPosition;

        private Vector2Int freeBoxSelectedPosition;

        private CursorModel cursorModel;

        private ComputerStrategy computerStrategy;

        [SerializeField] private Grid grid;

        [SerializeField] private GameView gameView;

        [SerializeField] private CursorView cursorView;

        [SerializeField] private GameObject cellPrefab;

        [SerializeField] private UIController uiController;

        private GameModel model;
        public GameModel gameModel => model;

        public event Action<AnimationData> GameBoardToAnimate;

        public bool isWaitingEndOfAnimation;

        public bool IsPlayerTurn => isPlayerTurn;

        private bool isPlayerTurn;
        private GameObject selectedCellGO;

        private enum GameStateValues
        {
            GameInitialized = default,
            WaitCellUserToBeSelected = 1,
            WaitFreeBoxToBeSelected = 2,
            ComputerReadyToPlay = 3,
            EndOfGame = 4,
        }

        private void Start()
        {
            model = new GameModel(cellPrefab);
            gameView.SetModel(model);
            gameView.Subscribe(model);
            gameView.Subscribe(this);
            uiController.SetModel(model);

            cursorModel = cursorView.GetCursorModel();

            computerStrategy = new ComputerStrategy(model);
            gameState = GameStateValues.GameInitialized;
            isWaitingEndOfAnimation = false;
        }

        private void OnDestroy()
        {
            gameView.UnSubscribe(model);
            gameView.UnSubscribe(this);
        }

        private void Update()
        {
            Vector2Int clickPosition;

            //Is a modal window displayed
            var isModalMode = uiController.isModalMode;

            //A user action is needed in these states
            if (!Input.GetMouseButtonDown(0) &&
                ( (gameState == GameStateValues.WaitCellUserToBeSelected && model.ReturnPlayableCellsPositions(BoxValue.IsUserCell).Count > 0)
                   ||
                 gameState == GameStateValues.WaitFreeBoxToBeSelected
                   ||
                 gameState == GameStateValues.EndOfGame)
                  ||
                 isModalMode) return;



            switch (gameState)
            {
                case GameStateValues.GameInitialized :
                    isWaitingEndOfAnimation = true;
                    model.InitGameModel();
                    gameState = GameStateValues.WaitCellUserToBeSelected;
                    isPlayerTurn = true;

                    //Debug.Log("GameStateValues.waitCellUserToBeSelected");
                    break;

                case GameStateValues.WaitCellUserToBeSelected :
                    if (isWaitingEndOfAnimation) break;

                    if (model.ReturnPlayableCellsPositions(BoxValue.IsUserCell).Count == 0)
                    {
                        if (model.ReturnPlayableCellsPositions(BoxValue.IsComputerCell).Count == 0)
                        {
                            gameState = GameStateValues.EndOfGame;
                            Debug.Log("GameStateValues.endOfGame");

                        }
                        else
                        {
                            gameState = GameStateValues.ComputerReadyToPlay;
                            //Debug.Log("GameStateValues.computerReadyToPlay");

                        }
                    }

                    if (gameState == GameStateValues.WaitCellUserToBeSelected)
                    {
                        clickPosition = (Vector2Int) GetCursorPositionInGrid(grid);
                        if (model.CandidateCellIsChosen(clickPosition, BoxValue.IsUserCell))
                        {

                            // Nouvelle sélection
                            SelectCellUser(clickPosition);

                            gameState = GameStateValues.WaitFreeBoxToBeSelected;
                            //Debug.Log("GameStateValues.waitFreeBoxToBeSelected");
                        }

                    }
                    break;

                case GameStateValues.WaitFreeBoxToBeSelected :
                    if (isWaitingEndOfAnimation) break;


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
                                ? GameStateValues.EndOfGame
                                : GameStateValues.ComputerReadyToPlay;

                            //Debug.Log(gameState.Equals(GameStateValues.ComputerReadyToPlay)
                             //   ? "GameStateValues.computerReadyToPlay"
                              //  : "GameStateValues.endOfGame");

                            isPlayerTurn = false;

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
                            gameState = GameStateValues.WaitCellUserToBeSelected;
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

                case GameStateValues.ComputerReadyToPlay :

                    if (isWaitingEndOfAnimation) break;


                    if (model.ReturnPlayableCellsPositions(BoxValue.IsComputerCell).Count == 0)
                    {
                        if (model.ReturnPlayableCellsPositions(BoxValue.IsUserCell).Count == 0)
                        {
                            gameState = GameStateValues.EndOfGame;
                            Debug.Log("GameStateValues.endOfGame");
                        }
                        else
                        {
                            gameState = GameStateValues.WaitCellUserToBeSelected;
                            Debug.Log("GameStateValues.waitCellUserToBeSelected");
                        }
                    }

                    if (gameState == GameStateValues.ComputerReadyToPlay)
                    {
                        var animationSteps = computerStrategy.Play();

                        //update the view
                        isWaitingEndOfAnimation = true;
                        var animationData = new AnimationData(animationSteps);
                        GameBoardToAnimate?.Invoke(animationData);

                        gameState = model.NoMoreBoxesWithCellValue(BoxValue.IsFreeBox) ||
                                    model.NoMoreBoxesWithCellValue(BoxValue.IsUserCell)
                            ? GameStateValues.EndOfGame
                            : GameStateValues.WaitCellUserToBeSelected;

                        //Debug.Log(gameState.Equals(GameStateValues.WaitCellUserToBeSelected)
                         //   ? "GameStateValues.waitCellUserToBeSelected"
                         //   : "GameStateValues.endOfGame");

                        isPlayerTurn = true;
                    }


                    break;

                case GameStateValues.EndOfGame:
                    if (isWaitingEndOfAnimation) break;
                    //Debug.Log("GameStateValues.EndOfGame");

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
