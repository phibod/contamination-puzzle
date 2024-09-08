using UnityEngine;
using UnityEngine.Tilemaps;

public class GameView : MonoBehaviour
{
    [SerializeField] private GameController controller;

    [SerializeField] private Tilemap biologicCellMap;

    [SerializeField] private Tile computerCell;
    
    [SerializeField] private Tile playerCell;
   
    public void Subscribe(GameModel model)
    {
        model.OnInitialize += ClearBoard;
        model.OnTileChanged += DrawTileDependingOnCellValue;

    }

    private void DrawTileDependingOnCellValue(int posX, int posY, GameModel.BoxValue boxValue)
    {
        var position = new Vector3Int(posX, posY);
        
        var tile = boxValue switch
        {
            GameModel.BoxValue.ComputerCell => computerCell,
            GameModel.BoxValue.PlayerCell => playerCell,
            _ => null
        };
        biologicCellMap.SetTile(position, tile);

    }

    public void UnSubscribe(GameModel model)
    {
        model.OnInitialize -= ClearBoard;
        model.OnTileChanged -= DrawTileDependingOnCellValue;
    }
    
    private void ClearBoard()
    {
        biologicCellMap.ClearAllTiles();
    }
}
