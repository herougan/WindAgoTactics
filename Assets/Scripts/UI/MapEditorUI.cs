using UnityEngine;

public class MapEditorUI : MonoBehaviour{

    // Tile Shape sprites
    public Sprite tileFlat;
    public Sprite tileSlopeNorth;
    public Sprite tileSlopeSouth;
    public Sprite tileSlopeEast;
    public Sprite tileSlopeWest;
    public Sprite tileSharpNorthEast;
    public Sprite tileSharpSouthEast;
    public Sprite tileSharpSouthWest;
    public Sprite tileSharpNorthWest;
    
    Sprite GetTileShapeSprite(TileShape shape, Direction direction = Direction.None){
        switch (shape) {
            case TileShape.Flat:
                return tileFlat;
                break;
            case TileShape.Slope:
                return tileSlopeNorth;
                break;
            case TileShape.Sharp:
                return tileSharpNorthEast;
                break;
            default:
                return tileFlat;
                break;
        }
    }

    void Start() {

    }

    void Update() {

    }



}