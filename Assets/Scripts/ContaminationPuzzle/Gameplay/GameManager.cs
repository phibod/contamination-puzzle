using System;
using ContaminationPuzzle.Entities;
using ContaminationPuzzle.UI;
using UnityEngine;

namespace ContaminationPuzzle.Gameplay
{
    public class GameManager : MonoBehaviour
    {

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

        [SerializeField]
        private UnityEngine.Grid grid;

        private GameObject currentPlayer;
        
        private GameMode gameMode;
        private GameModel gameModel;

        // Routing of animation requests: players invoke their GameBoardToAnimate,
        // GameManager forwards to subscribed GameView via this event.
        public event Action<AnimationData> GameBoardToAnimate;

        // Flag to prevent input during animation
        private bool isWaitingEndOfAnimation;
        
        public GameObject CurrentPlayer
        {
            get => currentPlayer;
            set => currentPlayer = value;
        }

        public int PlayerType => (currentPlayer == humanPlayer1) ? 1 : 2;

        public bool IsWaitingEndOfAnimation
        {
            get => isWaitingEndOfAnimation;
            set => isWaitingEndOfAnimation = value;
        }
       
        // Expose model/view/grid/ui to players
        public GameModel GameModel => gameModel;
        public GameView GameView => gameView;
        public UnityEngine.Grid Grid => grid;
        public UIController UIController => uiController;

        // Keep track of currently subscribed player to forward events and avoid duplicate subscriptions
        private Player subscribedPlayer;
        private bool humanPlayer1ReadyToPlay;
        private bool opponentPlayerReadyToPlay;
        private GameObject previousPlayer;

        private void Awake()
        {
            //deactivate all players
            humanPlayer1.SetActive(false);
            humanPlayer2.SetActive(false);
            aiPlayer.SetActive(false);
            
            //instantiate gameModel 
            gameModel = new GameModel(cellPrefab);
            
            //associate gameModel to gameView
            gameView.SetModel(gameModel);
            gameView.Subscribe(gameModel);

            // Subscribe GameView to this GameManager so it can listen to animations (same pattern as previous GameController)
            gameView.Subscribe(this);

            //associate gameModel to uiController
            uiController.SetModel(gameModel);

            // GameView.OnEndRound triggers SwitchPlayer (animation completed or round finished)
            Subscribe(gameView);
        }

        // private void Start()
        // {
        //     var steps = gameModel.Init();
        //     var animationData = new AnimationData(steps);
        //     OnPlayerAnimationRequested(animationData);
        // }

        private void Subscribe(GameView gv)
        {
            gv.OnEndRound += SwitchPlayer;
        }

        private void SwitchPlayer()
        {
            bool endOfGame = false;
            
            var scoreData = gameModel.GetScoreData();

            previousPlayer = currentPlayer;
            gameMode = uiController.CurrentGameMode;
            
            

            // Switch player depending on mode
            if (gameMode.Value == GameMode.Options.TwoPlayers)
            {
                currentPlayer = (currentPlayer == humanPlayer1) ? humanPlayer2 : humanPlayer1;
            }
            else
            {
                currentPlayer = (currentPlayer == humanPlayer1) ? aiPlayer : humanPlayer1;
            }

            //current player can't play
            if (!DetectPlayerReadyToPlay(currentPlayer))
            {
                //does the previous player can play ? 
                currentPlayer = previousPlayer;
         
                //both players can not play : end of game
                if (!DetectPlayerReadyToPlay(currentPlayer)) 
                    endOfGame = true;
                else if (currentPlayer == humanPlayer1 && scoreData.playerScore > scoreData.computerScore ||
                         currentPlayer != humanPlayer1 && scoreData.computerScore > scoreData.playerScore)
                    endOfGame = true;
            }
            // Update UI panels with score data
            uiController.UpdatePanelComponents((scoreData));
            if (endOfGame) 
                gameView.ShowTheCup();
            else 
                ActivateCurrentPlayer();
        }

        private bool DetectPlayerReadyToPlay(GameObject paramPlayer)
        {
            if (paramPlayer == humanPlayer1)
            {
                humanPlayer1ReadyToPlay = gameModel.ReturnPlayableCellsPositions(BoxValue.IsUserCell).Count > 0;
                return humanPlayer1ReadyToPlay;
            }

            opponentPlayerReadyToPlay = gameModel.ReturnPlayableCellsPositions(BoxValue.IsComputerCell).Count > 0;
            return opponentPlayerReadyToPlay;


        }


        private void ActivateCurrentPlayer()
        {
            // Set active states
            humanPlayer1.SetActive(currentPlayer == humanPlayer1);

            if (gameMode.Value == GameMode.Options.TwoPlayers)
            {
                humanPlayer2.SetActive(currentPlayer == humanPlayer2);
                aiPlayer.SetActive(false);
            }
            else
            {
                // Solo mode: only one human and AI exist, activate AI if it's its turn
                humanPlayer2.SetActive(false);
                aiPlayer.SetActive(currentPlayer == aiPlayer);
            }

            // Unsubscribe previous player's event if any
            if (subscribedPlayer != null)
            {
                // If HumanPlayer, reset selection
                if (subscribedPlayer is HumanPlayer hp) hp.ResetSelection();
                subscribedPlayer = null;
            }

            // Subscribe new player's GameBoardToAnimate so we can forward to GameView and set waiting flag
            var playerComp = currentPlayer.GetComponent<Player>();
            if (playerComp != null)
            {
                subscribedPlayer = playerComp;
            }

            // If we just activated the AI in solo mode, trigger its play
            if (gameMode.Value != GameMode.Options.TwoPlayers && currentPlayer == aiPlayer)
            {
                var aiComp = aiPlayer.GetComponent<AIPlayer>();
                if (aiComp != null)
                {
                    // Start AI move
                    aiComp.DoPlay();
                }
            }
        }

        public void OnPlayerAnimationRequested(AnimationData animationData)
        {
            // When a player requests an animation, block further input and forward the animation to GameView
            isWaitingEndOfAnimation = true;

            // Forward to any subscribers (GameView is expected to have subscribed via gameView.Subscribe(this))
            GameBoardToAnimate?.Invoke(animationData);
        }
    }
}