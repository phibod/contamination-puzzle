using ContaminationPuzzle.Entities;
using UnityEngine;

namespace ContaminationPuzzle.Gameplay
{
    /// <summary>
    /// Handles the visual representation and behavior of the game cursor.
    /// Updates cursor position based on mouse input and changes its color based on game context.
    /// </summary>
    public class CursorView : MonoBehaviour
    {
        [SerializeField] private Transform cursor;

        [SerializeField] private Grid grid;
        
        [Header("Controllers")]
        [SerializeField] private GameManager gameManager;
        
        [Header("StateSprites")]
        [SerializeField] private Sprite Arrow;
        [SerializeField] private Sprite OpponentGameBoard;
        [SerializeField] private Sprite UserGameBoard;
        
        [Header("Players")]
        [SerializeField]
        private GameObject humanPlayer1;
        
        [SerializeField]
        private GameObject humanPlayer2;

        [SerializeField]
        private GameObject aiPlayer;

        
        private SpriteRenderer spriteRenderer;

        /// <summary>
        /// Defines the possible states of the cursor based on its position and context.
        /// </summary>
        private enum CursorState
        {
            
            /// <summary>Cursor is outside the game board area</summary>
            OutOfGameArea = 1,
            
            /// <summary>Cursor is associated with player </summary>
            IsUsedByPlayer  = 2,
            
            /// <summary>Cursor is associated with player</summary>
            IsUsedByOpponent = 3,

        }

        private CursorState currentState = CursorState.IsUsedByPlayer;
        
        /// <summary>
        /// Determines if the cursor is within the game board area.
        /// </summary>
        private bool CursorInGameArea()
        {
            return (cursor.position.x >= 0 &&
                    cursor.position.x < GameModel.NbColumns &&
                    cursor.position.y >= 0 &&
                    cursor.position.y < GameModel.NbRows);
        }

        // Start is called before the first frame update
        void Start()
        {
            //Fetch the SpriteRenderer from the GameObject
            spriteRenderer = cursor.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = UserGameBoard;

        }

        // Update is called once per frame
        private void Update()
        {
            cursor.position = GameController.GetCursorPositionInGrid(grid) + new Vector3(0.5f, 0.5f, 0);
            if (CursorInGameArea())
            {
                if (gameManager.currentPlayer == humanPlayer1 && currentState != CursorState.IsUsedByPlayer)
                {
                    spriteRenderer.sprite =  UserGameBoard;
                    currentState = CursorState.IsUsedByPlayer;
                }

                if (gameManager.currentPlayer != humanPlayer1 && currentState != CursorState.IsUsedByOpponent)
                {
                    spriteRenderer.sprite =  OpponentGameBoard;
                    currentState = CursorState.IsUsedByOpponent;
                }
            }
            else if (currentState != CursorState.OutOfGameArea)
            {
                spriteRenderer.sprite = Arrow;
                currentState = CursorState.OutOfGameArea;
            }

        }
    }
}