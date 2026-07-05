using System;
using ContaminationPuzzle.Entities;
using ContaminationPuzzle.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ContaminationPuzzle.UI
{
    /// <summary>
    /// Main UI controller orchestrating all UI components and panels.
    /// Manages modal dialogs, score displays, and player turn indicators.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        private static readonly int IsPlayerTurn = Animator.StringToHash("isPlayerTurn");
        private static readonly int IsplayerTurn = Animator.StringToHash("IsplayerTurn");
        private GameObject currentPanel = null;

        public bool isModalMode = false;


        [SerializeField]
        private GameManager gameManager;

        [SerializeField]
        private GameView gameView;

        [Header("Panels")]
        [SerializeField]
        private GameObject confirmRestartPanel;

        [SerializeField]
        private GameObject confirmQuitPanel;

        [SerializeField]
        private GameObject gameModePanel;
        
        [Header("ScoreTexts")]
        [SerializeField]
        private TextMeshProUGUI playerScoreText;

        [SerializeField]
        private TextMeshProUGUI computerScoreText;

        [Header("DominationBar")]
        [SerializeField]
        private Image fillPlayerDominance;
        [SerializeField]
        private Image fillComputerDominance;

        [FormerlySerializedAs("NextPlayerIndicator")]
        [Header("NextPlayerIndicator")]
        [SerializeField]
        private Animator nextPlayerIndicator;

        [Header("GameModeIndicator")]
        [SerializeField]
        private TextMeshProUGUI gameModeText;
        
        private GameModel gameModel;

        private GameObject playerScore;
        private GameObject opponentScore;
        
        public GameMode CurrentGameMode { get; } = new();

        /// <summary>
        /// Sets the game model for this UI controller.
        /// </summary>
        public void SetModel(GameModel paramGameModel)
        {
            this.gameModel = paramGameModel;
        }
        
        public void UpdatePanelComponents(ScoreData scoreData)
        {

            playerScoreText.text = scoreData.playerScore.ToString("00");
            computerScoreText.text = scoreData.computerScore.ToString("00");

            float totalCells = scoreData.playerScore + scoreData.computerScore;
            var dominancePlayerRatio = scoreData.playerScore / totalCells;
            fillPlayerDominance.fillAmount = dominancePlayerRatio;
            fillComputerDominance.fillAmount = 1 - dominancePlayerRatio;
            var playerTypeValue = gameManager.PlayerType;
            nextPlayerIndicator.SetInteger("PlayerType", playerTypeValue);
        }
        
        private void Start()
        {
            this.ActivateCurrentPanel(gameModePanel);
        }

        private void DesactivateCurrentPanel()
        {
            isModalMode = false;
            currentPanel.SetActive(false);
        }

        private void ActivateCurrentPanel(GameObject panel)
        {
            isModalMode = true;
            panel.SetActive(true);
            currentPanel = panel;
        }

        private void StartNewGame()
        {
            gameManager.CurrentPlayer = null;
            var animationData = new AnimationData(gameModel.Init());
            gameManager.OnPlayerAnimationRequested(animationData);
        }

        /*
         * Manage the Restart Button of the right panel
         */
        public void OnRestartButtonClicked()
        {
            ActivateCurrentPanel(confirmRestartPanel);
        }

        /*
         * Manage the Restart Button of the right panel
         */
        public void OnQuitButtonClicked()
        {
            ActivateCurrentPanel(confirmQuitPanel);
        }

        /*
         * Manage the GameMode Button of the right panel
         */
        public void OnGameModeButtonClicked()
        {
            ActivateCurrentPanel(gameModePanel);
        }

        
        
        
        /*
         * Yes or No button of the ConfirmRestartPanel
         */
        public void OnConfirmYes()
        {
            DesactivateCurrentPanel();

            if (currentPanel == confirmQuitPanel)
            {
#if UNITY_EDITOR
                // Quitter le Play Mode dans l'éditeur
                UnityEditor.EditorApplication.isPlaying = false;

#elif UNITY_ANDROID 
                // Recommandation Unity : ne pas forcer Application.Quit()
                // mais envoyer l'app en arrière-plan
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    activity.Call<bool>("moveTaskToBack", true);
                }

#elif UNITY_IOS
                // iOS : Apple interdit de quitter une app par code
                // On ne fait rien (ou afficher un message si tu veux)
                // Application.Quit() est ignoré de toute façon.

#else
                // Windows / macOS / Linux
                Application.Quit();
#endif
            }

            if (currentPanel == confirmRestartPanel)
            {
                //init the model of the game
                gameView.RemoveTheCup();
                StartNewGame();
            }

        }

        public void OnConfirmNo()
        {
            DesactivateCurrentPanel();
        }

        /*
         * Confirm one player mode
         */
        public void OnConfirmOnePlayer()
        {
            if (CurrentGameMode.Value == GameMode.Options.Solo) return;
            CurrentGameMode.Value = GameMode.Options.Solo;
            gameModeText.text = "One player";
            DesactivateCurrentPanel();
            StartNewGame();

        }

        /*
         * Confirm two players mode
         */
        public void OnConfirmTwoPlayers()
        {
            if (CurrentGameMode.Value == GameMode.Options.TwoPlayers) return;
            CurrentGameMode.Value = GameMode.Options.TwoPlayers;
            gameModeText.text = "Two players";
            DesactivateCurrentPanel();
            StartNewGame();
        }

        

    }

}
