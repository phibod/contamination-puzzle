using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class GameController : MonoBehaviour
{
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

    private GameModel model;
    
    [SerializeField] private Grid grid;

    [SerializeField] private GameView gameView;
    
    [SerializeField] private CursorView cursorView;

    [SerializeField] private GameObject cellPrefab;

    
    public event Action<AnimationData> GameBoardToAnimate;

    public bool isWaitingEndOfAnimation;
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
        gameView.Subscribe(model);
        gameView.Subscribe(this);
        cursorModel = cursorView.GetCursorModel();
 
        computerStrategy = new ComputerStrategy(model);
        gameState = GameStateValues.GameInitialized;
        isWaitingEndOfAnimation = false;
        
        
        Debug.Log($"start GameController");
        Debug.Log("GameStateValues.GameInitialized");
    }

    private void OnDestroy()
    {
        gameView.UnSubscribe(model);
        gameView.UnSubscribe(this);
    }
    
    private void Update()
    {
        Vector2Int clickPosition;

        //A user action is needed in these states
        if (!Input.GetMouseButtonDown(0) &&
            ( (gameState == GameStateValues.WaitCellUserToBeSelected && model.ReturnPlayableCellsPositions(GameModel.BoxValue.IsUserCell).Count > 0)
               ||
             gameState == GameStateValues.WaitFreeBoxToBeSelected 
               ||
             gameState == GameStateValues.EndOfGame)) return;

      
        
        switch (gameState)
        {
            case GameStateValues.GameInitialized :
                isWaitingEndOfAnimation = true;
                model.InitGameModel();
                gameState = GameStateValues.WaitCellUserToBeSelected;
                Debug.Log("GameStateValues.waitCellUserToBeSelected");
                break;
            
            case GameStateValues.WaitCellUserToBeSelected :
                if (isWaitingEndOfAnimation) break;
                if (model.ReturnPlayableCellsPositions(GameModel.BoxValue.IsUserCell).Count == 0)
                {
                    if (model.ReturnPlayableCellsPositions(GameModel.BoxValue.IsComputerCell).Count == 0)
                    {
                        gameState = GameStateValues.EndOfGame;
                        Debug.Log("GameStateValues.endOfGame");

                    }
                    else
                    {
                        gameState = GameStateValues.ComputerReadyToPlay;
                        Debug.Log("GameStateValues.computerReadyToPlay");

                    }
                }
                
                if (gameState == GameStateValues.WaitCellUserToBeSelected)
                {
                    clickPosition = (Vector2Int) GetCursorPositionInGrid(grid);
                    if (model.CandidateCellIsChosen(clickPosition,GameModel.BoxValue.IsUserCell))
                    {

                        // Nouvelle sélection
                        SelectCellUser(clickPosition);

                        gameState = GameStateValues.WaitFreeBoxToBeSelected;
                        Debug.Log("GameStateValues.waitFreeBoxToBeSelected");
                    }
                    
                }
                break;
            
            case GameStateValues.WaitFreeBoxToBeSelected :
                if (isWaitingEndOfAnimation) break;
                
                clickPosition = (Vector2Int) GetCursorPositionInGrid(grid);
                if (model.CandidateCellIsChosen(clickPosition,GameModel.BoxValue.IsFreeBox))
                {
                    var distMove = clickPosition - cellUserSelectedPosition;
                    if (Math.Abs(distMove.x) <= GameModel.MaxDistanceMove && Math.Abs(distMove.y) <= GameModel.MaxDistanceMove)
                    {
                        //animation du move ou du clone : la cellule est désélectionnée avant l'animation
                        //TODO verifier l'enchainement en séquence
                        gameView.DeselectCurrentCell(selectedCellGO);
                        var animationSteps = model.MoveOrCloneTheCell(cellUserSelectedPosition,clickPosition);
                        
                        //update the view
                        isWaitingEndOfAnimation = true;
                        var animationData = new AnimationData(animationSteps);
                        GameBoardToAnimate?.Invoke(animationData);
                        
                        gameState = model.NoMoreBoxesWithCellValue(GameModel.BoxValue.IsFreeBox) ||
                                    model.NoMoreBoxesWithCellValue(GameModel.BoxValue.IsComputerCell)
                            ? GameStateValues.EndOfGame
                            : GameStateValues.ComputerReadyToPlay;

                        Debug.Log(gameState.Equals(GameStateValues.ComputerReadyToPlay)
                            ? "GameStateValues.computerReadyToPlay"
                            : "GameStateValues.endOfGame");
                        
                    }
                    else
                    {
                        Debug.Log("Not authorized");
                    }
                }
                else if (model.CandidateCellIsChosen(clickPosition, GameModel.BoxValue.IsUserCell))
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
                        Debug.Log("Retour à WaitCellUserToBeSelected");
                        break;
                    }

                    // Si l'utilisateur clique une autre cellule user jouable
                    if (selectedCellGO != null && selectedCellGO != cellGO)
                    {
                        gameView.DeselectCurrentCell(selectedCellGO);
                    }

                    // Nouvelle sélection
                    SelectCellUser(clickPosition);

                    Debug.Log("Nouvelle cellule sélectionnée en WaitFreeBoxToBeSelected");
                }
                break;
            
            case GameStateValues.ComputerReadyToPlay :

                if (isWaitingEndOfAnimation) break;                
                if (model.ReturnPlayableCellsPositions(GameModel.BoxValue.IsComputerCell).Count == 0)
                {
                    if (model.ReturnPlayableCellsPositions(GameModel.BoxValue.IsUserCell).Count == 0)
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

                    gameState = model.NoMoreBoxesWithCellValue(GameModel.BoxValue.IsFreeBox) ||
                                model.NoMoreBoxesWithCellValue(GameModel.BoxValue.IsUserCell)
                        ? GameStateValues.EndOfGame
                        : GameStateValues.WaitCellUserToBeSelected;

                    Debug.Log(gameState.Equals(GameStateValues.WaitCellUserToBeSelected)
                        ? "GameStateValues.waitCellUserToBeSelected"
                        : "GameStateValues.endOfGame");
                }
                

                break;
            
            case GameStateValues.EndOfGame:
                if (isWaitingEndOfAnimation) break;  

                //TODO manage a restart game button
                gameState = GameStateValues.GameInitialized;
                Debug.Log("GameStateValues.GameInitialized");
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