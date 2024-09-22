using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class GameController : MonoBehaviour
{
    public static event Action OnDrawInitGame;

    private GameStateValues gameState;

 
    private Vector3Int cellUserSelectedPosition;
    
    private Vector3Int freeBoxSelectedPosition;
    
    private GameModel model;

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
        gameState = GameStateValues.gameInitialized;
        
        Debug.Log($"start GameController");
        Debug.Log("GameStateValues.gameInitialized");
    }

    private void OnDestroy()
    {
        view.UnSubscribe(model);        
    }


    private Vector3Int GetPositionCellSelected()
    {
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = grid.WorldToCell(worldPoint);
        
        return cellPosition;
        

    }

    private void Update()
    {
        Vector3Int clickPosition;

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
                    model.MoveOrCloneTheCell(cellUserSelectedPosition,clickPosition,GameModel.BoxValue.PlayerCell);
                    
                    gameState = model.NoMoreBoxesWithCellValue(GameModel.BoxValue.FreeBox) ||
                                model.NoMoreBoxesWithCellValue(GameModel.BoxValue.ComputerCell)
                        ? GameStateValues.endOfGame
                        : GameStateValues.computerReadyToPlay;
                    
                    if (gameState.Equals(GameStateValues.computerReadyToPlay))
                        Debug.Log("GameStateValues.computerReadyToPlay");
                    else
                        Debug.Log("GameStateValues.endOfGame");

                }
                break;
            
            case GameStateValues.computerReadyToPlay :
                
                model.ComputerToPlay();
                gameState = model.NoMoreBoxesWithCellValue(GameModel.BoxValue.FreeBox) ||
                            model.NoMoreBoxesWithCellValue(GameModel.BoxValue.PlayerCell)
                    ? GameStateValues.endOfGame
                    : GameStateValues.waitCellUserToBeSelected;

                if (gameState.Equals(GameStateValues.waitCellUserToBeSelected))
                    Debug.Log("GameStateValues.waitCellUserToBeSelected");
                else
                    Debug.Log("GameStateValues.endOfGame");
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