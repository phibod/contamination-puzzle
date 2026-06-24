using ContaminationPuzzle.Entities;
using ContaminationPuzzle.UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace ContaminationPuzzle.Gameplay
{
    public class GameManager:MonoBehaviour
    
    {
        public GameObject currentPlayer;
        
        [SerializeField]
        private GameObject humanPlayer1;
        
        [SerializeField]
        private GameObject humanPlayer2;

        [SerializeField]
        private GameObject aiPlayer;
      
        [SerializeField]
        private GameView gameView;

        [SerializeField]
        private UIController uiController;

        [SerializeField]

        private GameObject cellPrefab;

        private GameMode gameMode;
        private GameModel gameModel;


        private void Awake()
        {
            gameModel = new GameModel(cellPrefab);
            gameView.SetModel(gameModel);
            gameView.Subscribe(gameModel);
            uiController.SetModel(gameModel);
            //Init();

            this.Subscribe(gameView);
        }

        public void StartGame(GameMode mode)
        {
            gameMode = mode;
            currentPlayer = humanPlayer1;
            ActivateCurrentPlayer();
        }

        public void Subscribe(GameView gameView)
        {
            gameView.OnEndRound += SwitchPlayer;
        }

        private void SwitchPlayer()
        {

            //Update the panels with scoreData
            var scoreData = gameModel.GetScoreData();
            uiController.UpdatePanelComponents((scoreData));

            
            //Switch player
            if (gameMode.value == GameMode.Options.TwoPlayers)
            {
                currentPlayer = (currentPlayer == humanPlayer1) ?  humanPlayer2 : humanPlayer1;

            }
            else
            {
                currentPlayer = (currentPlayer == humanPlayer1) ?  aiPlayer : humanPlayer1;
                
            }
            ActivateCurrentPlayer();
        }


        private void ActivateCurrentPlayer()
        {
            humanPlayer1.SetActive(currentPlayer==humanPlayer1);
            if (gameMode.value == GameMode.Options.TwoPlayers)
            {
                humanPlayer2.SetActive(currentPlayer==humanPlayer2);

            }
            
        }
        
        

        
    }
}