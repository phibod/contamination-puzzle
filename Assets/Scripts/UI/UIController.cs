using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    
    
    public class UIController : MonoBehaviour
    {
        private static readonly int IsPlayerTurn = Animator.StringToHash("isPlayerTurn");
        private static readonly int IsplayerTurn = Animator.StringToHash("IsplayerTurn");
        private GameObject currentPanel = null;
        
        public bool isModalMode = false;

        
        [SerializeField]
        private GameController gameController;

        [SerializeField]
        private  GameView gameView;
        
        [Header("Panels")]
        [SerializeField]
        private GameObject confirmRestartPanel;
        
        [SerializeField]
        private GameObject confirmQuitPanel;
      
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

        [Header("NextPlayerIndicator")]
        [SerializeField]
        private Animator NextPlayerIndicator;
        
        private GameModel gameModel;

        private GameObject playerScore;
        private GameObject opponentScore;

        public void SetModel(GameModel gameModel)
        {
            this.gameModel = gameModel;
        }

        private void Start()
        {
            this.Subscribe(gameView);
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

        private void Subscribe(GameView view)
        {
            view.OnEndRound += UpdateLeftPanelComponents;
        }

        
        private void UpdatePlayerIndicator(bool isPlayerTurnState)
        {
            NextPlayerIndicator.SetBool("IsPlayerTurn",isPlayerTurnState);
        }


        private void UpdateLeftPanelComponents(ScoreData scoreData)
        {
            
            playerScoreText.text = scoreData.playerScore.ToString("00");
            computerScoreText.text = scoreData.computerScore.ToString("00");
            
            float totalCells = scoreData.playerScore + scoreData.computerScore;
            var dominancePlayerRatio = scoreData.playerScore/totalCells;
            fillPlayerDominance.fillAmount = dominancePlayerRatio;
            fillComputerDominance.fillAmount = 1 - dominancePlayerRatio;

             var isPlayerTurnState = gameController.IsPlayerTurn;
             Debug.Log("isPlayerTurnState ="+ isPlayerTurnState);
             NextPlayerIndicator.SetBool("IsPlayerTurn",isPlayerTurnState);
    

        }

        
        /*
         * Manage the Restart Button of the left panel
         */
        public void OnRestartButtonClicked()
        {
            ActivateCurrentPanel(confirmRestartPanel);
        }
        
        /*
         * Manage the Restart Button of the left panel
         */
        public void OnQuitButtonClicked()
        {
            ActivateCurrentPanel(confirmQuitPanel);
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
                // Quitter le Play Mode dans l’éditeur
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
                gameModel.InitGameModel();
            }

        }

        public void OnConfirmNo()
        {
            DesactivateCurrentPanel();
        }
        
        
    }
        
}
