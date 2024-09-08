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
        endOfGame = 3,
    }
    
    private void Start()
    {
        model = new GameModel();
        view.Subscribe(model);
        model.InitGameModel();
        
        gameState = GameStateValues.gameInitialized;
        
        Debug.Log($"start GameController");
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
        //handle user action
        if (!Input.GetMouseButtonDown(0) && gameState != GameStateValues.gameInitialized) return;
        
        var clickPosition = GetPositionCellSelected();
        switch (gameState)
        {
            case GameStateValues.gameInitialized : 
                gameState = GameStateValues.waitCellUserToBeSelected;
                Debug.Log("GameStateValues.waitCellUserToBeSelected");
                break;
            
            case GameStateValues.waitCellUserToBeSelected :
                if (model.ABoxWithCellValueIsChosen(clickPosition,GameModel.BoxValue.PlayerCell))
                {
                    cellUserSelectedPosition = clickPosition;
                    gameState = GameStateValues.waitFreeBoxToBeSelected;
                    Debug.Log("GameStateValues.waitFreeBoxToBeSelected");

                }
                break;
            
            case GameStateValues.waitFreeBoxToBeSelected :
                if (model.ABoxWithCellValueIsChosen(clickPosition,GameModel.BoxValue.FreeBox))
                {
                    model.MoveOrCloneTheCell(cellUserSelectedPosition,clickPosition,GameModel.BoxValue.PlayerCell);

                    model.ComputerToPlay();
                    gameState = model.NoMoreBoxesWithCellValue(GameModel.BoxValue.FreeBox) ||
                               model.NoMoreBoxesWithCellValue(GameModel.BoxValue.ComputerCell)
                                ? GameStateValues.endOfGame
                                : GameStateValues.waitCellUserToBeSelected;
                    
                    if (gameState.Equals(GameStateValues.waitCellUserToBeSelected))
                        Debug.Log("GameStateValues.waitCellUserToBeSelected");
                    else
                        Debug.Log("GameStateValues.endOfGame");
                    

                }
                break;
            
            case GameStateValues.endOfGame:
                
                //todo display the results
                
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }



    }
   

}