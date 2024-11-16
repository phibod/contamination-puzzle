using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class CursorView : MonoBehaviour
{

    [SerializeField] private Transform cursor;
    
    [SerializeField] private Grid grid;
   
    

    // Start is called before the first frame update
    void Start()
    {
       
        
        
    }

    // Update is called once per frame
    private void Update()
    {
        
        transform.position = GameController.GetCursorPositionInGrid(grid) + new Vector3(0.5f,0.5f,0);
         
    }
    
    
    
}
