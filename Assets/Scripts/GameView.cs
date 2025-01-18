using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class GameView : MonoBehaviour
{
    [SerializeField] private GameController controller;

    [SerializeField] private Tilemap biologicCellMap;

    [SerializeField] private Tile computerTileCell;

    [SerializeField] private Tile playerTileCell;

    [SerializeField] private GameObject playerCellPrefab;

    [SerializeField] private GameObject computerCellPrefab;

    [SerializeField] private float moveDuration = 1f;

    public void Subscribe(GameModel model)
    {
       
       model.OnInitialize += ClearBoard;
       model.OnTileChanged += DrawTileDependingOnCellValue;
    }

    private void DrawTileDependingOnCellValue(Vector2Int posOrigin, Vector2Int posDestination,
        GameModel.BoxValue boxValue)
    {
        if (boxValue == GameModel.BoxValue.FreeBox)
        {
            biologicCellMap.SetTile((Vector3Int)posDestination, null);
            return;
        }
        
        var tilePrefab = boxValue switch
        {
            GameModel.BoxValue.ComputerCell => computerCellPrefab,
            GameModel.BoxValue.PlayerCell => playerCellPrefab,
            _ => null
        };

        var tileGO = Instantiate(tilePrefab, (Vector3Int)posOrigin + new Vector3(0.5f, 0.5f, 0), Quaternion.identity);
        tileGO.transform.DOMove(new Vector3(posDestination.x + 0.5f, posDestination.y + 0.5f, 0), moveDuration).SetEase(Ease.OutQuart)
            .OnComplete(() =>
            {
                var tile = boxValue switch
                {
                    GameModel.BoxValue.ComputerCell => computerTileCell,
                    GameModel.BoxValue.PlayerCell => playerTileCell,
                    _ => null
                };
                biologicCellMap.SetTile((Vector3Int)posDestination, tile);
               Destroy(tileGO);
            });
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