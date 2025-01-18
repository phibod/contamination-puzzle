using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class CursorView : MonoBehaviour
{

    [SerializeField] private Transform cursor;
    
    [SerializeField] private Grid grid;
   
    private SpriteRenderer spriteRenderer;

    private CursorModel cursorModel;

    public CursorModel GetCursorModel()
    {
        return cursorModel;
    }

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

        cursorModel = new CursorModel();
        
        //Fetch the SpriteRenderer from the GameObject
        spriteRenderer = cursor.GetComponent<SpriteRenderer>();
        
    }

    // Update is called once per frame
    private void Update()
    {
        cursor.position = GameController.GetCursorPositionInGrid(grid) + new Vector3(0.5f,0.5f,0);
        spriteRenderer.color = CursorInGameArea() ? Color.yellow : Color.red;
    }
   
    
    
}
