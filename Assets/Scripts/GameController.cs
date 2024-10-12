using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class GameController : MonoBehaviour
{
    private GameStateValues gameState;

 
    private Vector2Int cellUserSelectedPosition;
    
    private Vector2Int freeBoxSelectedPosition;

    private GameModel model;
    
    private ComputerStrategy computerStrategy;

    [SerializeField] private Grid grid;

    [SerializeField] private GameView view;

    private enum GameStateValues
    {
        gameInitialized = default,
        waitCellUserToBeSelected = 1,
        waitFreeBoxToBeSelected = 2,
        computerReadyToPlay = 3,
        endOfGame = 4,
    }
    
    private void Start()
    {
        model = new GameModel();
        view.Subscribe(model);

        computerStrategy = new ComputerStrategy(model);
        gameState = GameStateValues.gameInitialized;
        
        
        Debug.Log($"start GameController");
        Debug.Log("GameStateValues.gameInitialized");
    }

    private void OnDestroy()
    {
        view.UnSubscribe(model);        
    }


    private Vector2Int GetPositionCellSelected()
    {
        Vector3 worldPoint = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int cellPosition = (Vector2Int)grid.WorldToCell(worldPoint);
        
        return cellPosition;
        

    }

    private void Update()
    {
        Vector2Int clickPosition;
        Vector2Int distMove;

        //handle user action
        if (!Input.GetMouseButtonDown(0) && 
            (gameState == GameStateValues.waitCellUserToBeSelected ||
             gameState == GameStateValues.waitFreeBoxToBeSelected ||
             gameState == GameStateValues.computerReadyToPlay ||
             gameState == GameStateValues.endOfGame)) return;

        switch (gameState)
        {
            case GameStateValues.gameInitialized :
                model.InitGameModel();
                gameState = GameStateValues.waitCellUserToBeSelected;
                Debug.Log("GameStateValues.waitCellUserToBeSelected");
                break;
            
            case GameStateValues.waitCellUserToBeSelected :
                clickPosition = GetPositionCellSelected();
                if (model.ABoxWithCellValueIsChosen(clickPosition,GameModel.BoxValue.PlayerCell))
                {
                    
                    cellUserSelectedPosition = clickPosition;
                    gameState = GameStateValues.waitFreeBoxToBeSelected;
                    Debug.Log("GameStateValues.waitFreeBoxToBeSelected");

                }
                break;
            
            case GameStateValues.waitFreeBoxToBeSelected :
                clickPosition = GetPositionCellSelected();
                if (model.ABoxWithCellValueIsChosen(clickPosition,GameModel.BoxValue.FreeBox))
                {
                    distMove = clickPosition - cellUserSelectedPosition;
                    if (Math.Abs(distMove.x) <= GameModel.MaxDistanceMove && Math.Abs(distMove.y) <= GameModel.MaxDistanceMove)
                    {
                        model.MoveOrCloneTheCell(cellUserSelectedPosition,clickPosition,GameModel.BoxValue.PlayerCell);
                    
                        gameState = model.NoMoreBoxesWithCellValue(GameModel.BoxValue.FreeBox) ||
                                    model.NoMoreBoxesWithCellValue(GameModel.BoxValue.ComputerCell)
                            ? GameStateValues.endOfGame
                            : GameStateValues.computerReadyToPlay;

                        Debug.Log(gameState.Equals(GameStateValues.computerReadyToPlay)
                            ? "GameStateValues.computerReadyToPlay"
                            : "GameStateValues.endOfGame");
                        
                    }
                    else
                    {
                        Debug.Log("Not authorized");
                    }
                }
                break;
            
            case GameStateValues.computerReadyToPlay :
                
                computerStrategy.Play();
                
                //TODO Verify if the player is not stuck or if the computer is not stuck
                
                gameState = model.NoMoreBoxesWithCellValue(GameModel.BoxValue.FreeBox) ||
                            model.NoMoreBoxesWithCellValue(GameModel.BoxValue.PlayerCell)
                    ? GameStateValues.endOfGame
                    : GameStateValues.waitCellUserToBeSelected;

                Debug.Log(gameState.Equals(GameStateValues.waitCellUserToBeSelected)
                    ? "GameStateValues.waitCellUserToBeSelected"
                    : "GameStateValues.endOfGame");
                break;
            
            case GameStateValues.endOfGame:
                
                //initialize the game (for test) 
                gameState = GameStateValues.gameInitialized;
                Debug.Log("GameStateValues.gameInitialized");
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }



    }
   

}