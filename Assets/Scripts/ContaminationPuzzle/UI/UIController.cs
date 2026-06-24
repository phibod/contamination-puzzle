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
        private readonly GameMode gameMode = new GameMode();

        public bool isModalMode = false;


        [SerializeField]
        private GameController gameController;

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

        /// <summary>
        /// Sets the game model for this UI controller.
        /// </summary>
        public void SetModel(GameModel gameModel)
        {
            this.gameModel = gameModel;
        }
        
        public void UpdatePanelComponents(ScoreData scoreData)
        {

            playerScoreText.text = scoreData.playerScore.ToString("00");
            computerScoreText.text = scoreData.computerScore.ToString("00");

            float totalCells = scoreData.playerScore + scoreData.computerScore;
            var dominancePlayerRatio = scoreData.playerScore / totalCells;
            fillPlayerDominance.fillAmount = dominancePlayerRatio;
            fillComputerDominance.fillAmount = 1 - dominancePlayerRatio;
            var playerTypeValue = (int) gameController.identifyPlayerType();
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
                gameModel.Init();
                gameController.Init();
                
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
            gameMode.value = GameMode.Options.Solo;
            gameModeText.text = "One player";
            DesactivateCurrentPanel();
        }

        /*
         * Confirm two players mode
         */
        public void OnConfirmTwoPlayers()
        {
            gameMode.value = GameMode.Options.TwoPlayers;
            gameModeText.text = "Two players";
            DesactivateCurrentPanel();

        }

        

    }

}
