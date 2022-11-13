using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static GeneralUtil;

public class MasterMapEditor : MonoBehaviour{
    #region Resources
    /* ===== Resources ===== */

    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject groundPrefab;
    public GameObject edgePrefab;
    public GameObject ceilingPrefab;
    public GameObject slopeWallPrefab;

    /* ===== Alt Floor Objectss ===== */
    public Dictionary<string, GameObject> altFloorObjects = new Dictionary<string, GameObject>() {

    };

    #endregion Resources

    #region Enums, States

    /* ===== Editing Modes ===== */
    public enum EditingMode {
        Tiling,
        Individual,
    }
    public enum AutoWallMode {
        None,
        BuildToGround,
        Static,
        Dynamic,
    }
    public enum GroundVoid {
        None,
        ColourFog,
        Branches,
        Liquid,
        SolidGround,
        HeightGround,
    }
    public enum TileBuildMode {
        BuildOnGround,
        BuildAtop,
    }
    
    /* ===== Other enums ===== */    
    public readonly Direction[] CardinalDirections = new Direction[] {
        Direction.West,
        Direction.North,
        Direction.East,
        Direction.South,
    };
    public readonly Direction[] DiagonalDirections = new Direction[] {
        Direction.NorthEast,
        Direction.SouthEast,
        Direction.SouthWest,
        Direction.NorthWest,
    };

    public int DirectionToDiagonal(Direction dir) {
        switch (dir) {
            case Direction.NorthEast:
                return 0;
            case Direction.SouthEast:
                return 1;
            case Direction.SouthWest:
                return 2;
            case Direction.NorthWest:
                return 3;
            default:
                return -1;
        }
    }
    
    public int DirectionToCardinal(Direction dir) {
        switch (dir) {
            case Direction.North:
                return 0;
            case Direction.East: // Clockwise
                return 1;
            case Direction.South:
                return 2;
            case Direction.West:
                return 3;
            default:
                return -1;
        }
    }

    /* ===== Actions ===== */
    public enum MapEditorInputAction {
        CreateTile,
        DeleteTile,

    }

    #endregion Enums

    #region Settings

    /* ===== Space Options ===== */

    public static int x_width = 1;
    public static int z_width = 1;
    public static int y_width = 1;
    public static float half_x_width = (float) x_width / 2;
    public static float half_z_width = (float) z_width / 2;
    public static float half_y_width = (float) y_width / 2;
    public int height = 1; // y

    // Plane
    public static int plane_x_width = 10; 
    public static int plane_z_width = 10;

    // Map Size
    public static int map_x_width = 16;
    public static int map_z_width = 16;
    // Offset
    public static float xz_offset = 0.001f; // Differentiating walls
    public static float y_offset = 0.001f; // Differentiating floors from ceiings.

    public readonly Dictionary<Direction, Vector3> WallPosition = new Dictionary<Direction, Vector3>() {
        [Direction.North] = new Vector3(x_width, 0, z_width + xz_offset),
        [Direction.South] = new Vector3(0, 0, 0 - xz_offset),
        [Direction.East] = new Vector3(x_width + xz_offset, 0, 0),
        [Direction.West] = new Vector3(0 - xz_offset, 0, z_width),
    };
    // new Vector3(0, 0, 0);

    /* ===== Tile Editor ===== */

    // Grid Options
    private int grid_x = 0, grid_y = 0, grid_z = 0;

    private Vector3 mouseRealPosition; // Mouse position in 3d space

    /* ===== Cosmetic Numerical ===== */

    private int cosmeticGroundHeight = 0; /* Displayed as 0f */


    #endregion Settings

    #region Active Data

    public Camera camera;
    private List<Tile> tiles = new List<Tile>();
    private List<Wall> walls = new List<Wall>();
    private List<Ceiling> ceilings = new List<Ceiling>();

    /**/

    private List<string> ActionHistory = new List<string>();

    #endregion

    #region Active Settings

    // Tile Editor Mode
    private int curr_x = 0, curr_y = 0, curr_z = 0;    
    private int curr_int_id = 0;
    private int chosenGroundLevel = 0;
    // Curr state
    private TileShape curr_tilingShape = TileShape.Flat;
    private Direction curr_direction = Direction.North; private int curr_int_direction = 0;
    private bool curr_tilingMode = false;
    public LiquidType baseLiquid = LiquidType.Void; // liquid at floor -1
    public AutoWallMode wallMode = AutoWallMode.BuildToGround;
    public TileBuildMode buildMode = TileBuildMode.BuildOnGround;

    #endregion Active Settings

    #region Active Objects
    // Master Objects
    public GameObject tileMaster;

    // Projection
    private GameObject projectionFloor;
    private GameObject northProjectionWall;
    private GameObject westProjectionWall;
    private GameObject eastProjectionWall;
    private GameObject southProjectionWall;
    private List<GameObject> projectionWalls = new List<GameObject>();

    // Grid
    private GameObject groundPlane;
    
    // Active tile (under cursor)
    private GameObject activeTileObject = null;
    private GameObject lastActiveTileObject = null;
    private Tile activeTile = null; // Once the pointer loses LOS with a tile, this is set null
    private Tile lastActiveTile = null; // Once this has a tile, it will not lose the tile

    // Main Objects
    private List<GameObject> tileObjects = new List<GameObject>();
    private List<GameObject> wallObjects = new List<GameObject>();
    private List<GameObject> ceilingObjects = new List<GameObject>();
    private List<GameObject> portalObjects = new List<GameObject>();
    private List<GameObject> ladderObjects = new List<GameObject>();
    private List<GameObject> destructibleObjects = new List<GameObject>();
    private List<GameObject> interactableObjects = new List<GameObject>();

    #endregion Active Objects

    #region Movement Settings

    /*
        Any slope with a height difference beyond this number is considered to have infinite weight (to walk across only)
    */
    const int MAX_TRAVERSABLE_SLOPE_HEIGHT = 5;
    readonly List<int> SLOPE_WEIGHTS = new List<int>() { 0, 1, 2, 3, 5, 8, 11, 14, 18, 23};
    const float HEIGHT_RANGE_BONUS = 0.5f;

    #endregion Movement Settings

    #region Core

    // Start is called before the first frame update
    void Start()    {
        Init();
    }

    // Update is called once per frame
    void Update()    {

    }

    void FixedUpdate() {
        if (curr_tilingMode) {
            UpdateTilePointer();
        }
    }

    void LateUpdate() {

    }

    void Init() {
        CreateProjection();
        groundPlane = CreateGroundPlaneObject();
    }

    void CreateAssetAsPrefab() {

    }

    #endregion Core

    #region UI

    public bool InitUI() {
        return false;
    }

    public bool CreateTileshapeSwab(){
        return false;
    }

    #endregion UI

    #region Input Actions

    public void ActionToggleTilingMode(InputAction.CallbackContext context){
        if (context.started) {
            curr_tilingMode = !curr_tilingMode;
            projectionFloor.SetActive(curr_tilingMode);
        }
    }

    public void ActionIncreaseActiveGroundLevel(InputAction.CallbackContext context){
            // Debug.Log(context);
        if (curr_tilingMode && context.canceled) {
            ++curr_y;
            projectionFloor.transform.position = SpaceToTilePosition(projectionFloor.transform.position);
        }
    }

    public void ActionDecreaseActiveGroundLevel(InputAction.CallbackContext context){
            // Debug.Log(context);
        if (curr_tilingMode && context.canceled) {
            if (curr_y > 0)
                --curr_y;
            projectionFloor.transform.position = SpaceToTilePosition(projectionFloor.transform.position);
        }
    } 

    public void ActionLowerTile(InputAction.CallbackContext context) {
        if (context.canceled) {
            // Get active tile
        }
    }

    public void ActionHeightenTile(InputAction.CallbackContext context) {
        if (context.canceled) {
            // Get active tile
        }
    }

    public void ActionRemoveTile(InputAction.CallbackContext context) {

    }

    public void ActionCreateWall(InputAction.CallbackContext context) {

    }
    
    public void ActionCreateTile(InputAction.CallbackContext context) {
        // Create tile
        if (curr_tilingMode && context.started) {
            Tile t = CreateTile(curr_x, curr_y, curr_z, curr_tilingShape, curr_direction);
        }
    }
    
    public void ActionScrollShapeType(InputAction.CallbackContext context) {
        // Move right
        if (context.started) {
            Debug.Log(context);
            int scroll_y = 1;
            curr_tilingShape += 1;
            if ((int) curr_tilingShape >= System.Enum.GetValues(typeof(TileShape)).Length) {
                curr_tilingShape = (TileShape) 0;
            }
            // Change mesh of projection
            UpdateProjection(projectionFloor);
        }
    }

    public void ActionMoveShapeType(InputAction.CallbackContext context) {
        /* Keyboard alternative to mouse */
        // Move right
        if (context.started) {
            curr_tilingShape += 1; curr_int_direction = 0;
            // Use different directionals now
            switch(curr_tilingShape) {
                case TileShape.Flat:
                    curr_direction = Direction.None;
                    break;
                case TileShape.Slope:
                    curr_direction = CardinalDirections[curr_int_direction];
                    break;
                case TileShape.Sharp:
                    curr_direction = DiagonalDirections[curr_int_direction];
                    break;
                case TileShape.Corner:
                    curr_direction = DiagonalDirections[curr_int_direction];
                    break;
            }
            if ((int) curr_tilingShape >= System.Enum.GetValues(typeof(TileShape)).Length) {
                curr_tilingShape = (TileShape) 0;
            }
            // Change mesh of projection
            UpdateProjection(projectionFloor);
        }
    }

    public void ActionScrollRotate(InputAction.CallbackContext context) {
        if (context.started) {
            Debug.Log(context);
            int scroll_y = 1;
            curr_direction += 1;
            if ((int) curr_direction >= System.Enum.GetValues(typeof(Direction)).Length) {
                curr_direction = (Direction) 0;
            }
            // Change mesh of projection
            UpdateProjection(projectionFloor);
        }
    }

    public void ActionMoveRotate(InputAction.CallbackContext context) {
        /* Keyboard alternative to mouse */
        if (context.started) {
            switch(curr_tilingShape) {
                case TileShape.Flat:
                    curr_direction = Direction.None;
                    break;
                case TileShape.Slope:
                    curr_int_direction = (curr_int_direction + 1) % CardinalDirections.Length;
                    curr_direction = CardinalDirections[curr_int_direction];
                    break;
                case TileShape.Sharp:
                    curr_int_direction = (curr_int_direction + 1) % DiagonalDirections.Length;
                    curr_direction = DiagonalDirections[curr_int_direction];
                    break;
                case TileShape.Corner:
                    curr_int_direction = (curr_int_direction + 1) % DiagonalDirections.Length;
                    curr_direction = DiagonalDirections[curr_int_direction];
                    break;
            }
            // Change mesh of projection
            UpdateProjection(projectionFloor);
        }
    }

    public void ActionDeleteTile(InputAction.CallbackContext context){
        if (context.started){

        }
    }

    /* Actions depending on mouse */
    public GameObject MouseAutoGetActiveTile() {
        return null;
    }

    public void UpdateTilePointer() {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity)){
            // Debug.Log(SpaceToTilePosition(hit.point));
            // projectionFloor.transform.position = SpaceToTilePosition(hit.point);
            Vector3 pos = SpaceToTilePosition(hit.point);
            curr_x = (int) pos.x;
            curr_z = (int) pos.z;
            // curr_x = (int) Mathf.Round(pos.x - half_x_width);
            // curr_z = (int) Mathf.Round(pos.z - half_z_width);
            UpdateProjectionPosition(new Vector3(curr_x, curr_y, curr_z));
        }
    }

    #endregion Input Actions

    #region TilePlacement

    public void CreateProjection() {
        
        projectionFloor = CreateQuadObject(Vector3.zero, false);
        bool active = (curr_tilingShape != TileShape.Flat);
        // northProjectionWall = Instantiate(wallPrefab, Vector3.zero, GetQuaternion(Direction.North));
        // southProjectionWall = Instantiate(wallPrefab, Vector3.zero, GetQuaternion(Direction.South));
        // eastProjectionWall = Instantiate(wallPrefab, Vector3.zero, GetQuaternion(Direction.East));
        // westProjectionWall = Instantiate(wallPrefab, Vector3.zero, GetQuaternion(Direction.West));

        // The rule is: Even if there isn't a wall there, there should be a wall object reference. Just with 0 mesh.
        northProjectionWall = CreateWallObject((int) projectionFloor.transform.position.x, (int) projectionFloor.transform.position.y, (int) projectionFloor.transform.position.z, curr_tilingShape, curr_direction, Direction.North, "North Projection");
        southProjectionWall = CreateWallObject((int) projectionFloor.transform.position.x, (int) projectionFloor.transform.position.y, (int) projectionFloor.transform.position.z, curr_tilingShape, curr_direction, Direction.South, "South Projection");
        eastProjectionWall = CreateWallObject((int) projectionFloor.transform.position.x, (int) projectionFloor.transform.position.y, (int) projectionFloor.transform.position.z, curr_tilingShape, curr_direction, Direction.East, "East Projection");
        westProjectionWall = CreateWallObject((int) projectionFloor.transform.position.x, (int) projectionFloor.transform.position.y, (int) projectionFloor.transform.position.z, curr_tilingShape, curr_direction, Direction.West, "West Projection");
        projectionWalls.Add(northProjectionWall); // Following CardinalDirections
        projectionWalls.Add(eastProjectionWall);
        projectionWalls.Add(southProjectionWall);
        projectionWalls.Add(westProjectionWall);

        // Change material

        // Change translucency

    }

    public void UpdateProjection(GameObject quad) {
        /*
            Update projection object mesh based on curr_tilingShape
        */
        
        // Update tile projection
        projectionFloor.GetComponent<MeshFilter>().mesh = GetTileMesh(curr_tilingShape, curr_direction);
        // Update mesh projection
        for (int i = 0; i < projectionWalls.Count; i++){
            northProjectionWall.GetComponent<MeshFilter>().mesh = GetWallMesh(curr_tilingShape, curr_direction, CardinalDirections[i]);
        }
    }

    public GameObject CreateQuadObject(Vector3 v, bool active = true){
        return CreateQuadObject(v, active, 0, 0, 0, 0);
    }

    public GameObject CreateQuadObject(Vector3 v, TileShape shape, Direction direction, bool active = true) {
        (int y1, int y2, int y3, int y4) = GetQuadVerticeHeights(shape, direction);
        return CreateQuadObject(v, active, y1, y2, y3, y4);
    }

    public GameObject CreateQuadObject(Vector3 v,  bool active = true, int y1 = 0, int y2 = 0, int y3 = 0, int y4 = 0, int tri11 = 0, int tri12 = 2, int tri13 = 1, int tri21 = 0, int tri22 = 3, int tri23 = 2) {

        // Create Object
        GameObject floor = Instantiate(floorPrefab, v, Quaternion.identity, tileMaster.transform);
        floor.transform.name = curr_tilingShape + "@" + v.x + ", " + v.y + ", " + v.z;
        floor.SetActive(active);
        MeshFilter meshFilter = floor.GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        /* Mesh Vertices (Height) */
        Vector3[] vertices = new Vector3[4] {
            // new Vector3(-x_width/2, -z_width/2, 0),
            // new Vector3(x_width/2, z_width/2, 0),
            // new Vector3(x_width/2, -z_width/2, 0),
            // new Vector3(-x_width/2, z_width/2, 0),

            new Vector3(0, y1, 0),
            new Vector3(x_width, y2, 0),
            new Vector3(x_width, y3, z_width),
            new Vector3(0, y4, z_width),
        };

        /* Mesh Triangulation */
        int[] tris = new int[6] {
            // Btm-Left Triangle
            tri11, tri12, tri13,
            // Top-Right Triangle
            tri21, tri22, tri23,
        };

        /* Mesh Normals */
        // Vector3[] normals = new Vector[4] {
        //     -Vector3.forward;
        //     -Vector3.forward;
        //     -Vector3.forward;
        //     -Vector3.forward;
        // }
        // mesh.normals = normals;

        /* Texture Coords */
        Vector2[] uv = new Vector2[4] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };

        /* Mesh Calculations */
        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        // floor.transform.rotation = floorPrefab.transform.rotation;

        /* Accompanying Walls */

        return floor;
    }

    public GameObject CreateTileObject(Vector3 v, TileShape shape, Direction direction, bool active = true) {

        GameObject tileObject = CreateQuadObject(v, shape, direction, active);
        // No difference between the functions as of yet.

        return tileObject;
    }
    
    public GameObject CreateWallObject(int x, int y, int z, Direction d, string name = "") {

        // adjust mesh here instead. just summon default wall object! todo
        Vector3 v;
        switch (d) {
            case Direction.North:
                v = new Vector3(x + half_x_width, y + half_y_width, z + xz_offset + z_width);
                break;
            case Direction.South:
                v = new Vector3(x + half_x_width, y + half_y_width, z - xz_offset);
                break;
            case Direction.East:
                v = new Vector3(x + x_width + xz_offset , y + half_y_width, z + half_z_width);
                break;
            case Direction.West:
                v = new Vector3(x - xz_offset, y + half_y_width, z + half_z_width);
                break;
            default:
                // North
                v = new Vector3(x + xz_offset, y, z);
                break;
        }
        GameObject wallObject = Instantiate(wallPrefab, v, GetQuaternion(d), tileMaster.transform);
        wallObject.transform.name = d + "-Wall@" + x + ", " + y + ", " + z;
        if (name != "")
            wallObject.transform.name = name;

        return wallObject;
    }

    public GameObject CreateWallObject(int x, int y, int z, TileShape shape, Direction shapeDir, Direction wallDir, string name = "") {
        Vector3 v;
        // Offset
        switch (wallDir) {
            case Direction.North:
                v = new Vector3(x + half_x_width, y + half_y_width, z + xz_offset + z_width);
                break;
            case Direction.South:
                v = new Vector3(x + half_x_width, y + half_y_width, z - xz_offset);
                break;
            case Direction.East:
                v = new Vector3(x + x_width + xz_offset , y + half_y_width, z + half_z_width);
                break;
            case Direction.West:
                v = new Vector3(x - xz_offset, y + half_y_width, z + half_z_width);
                break;
            default:
                // North
                v = new Vector3(x + xz_offset, y, z);
                break;
        }
        GameObject wallObject = Instantiate(wallPrefab, v, GetQuaternion(wallDir), tileMaster.transform);
        wallObject.GetComponent<MeshFilter>().mesh = GetWallMesh(shape, shapeDir, wallDir);

        wallObject.transform.name = wallDir + "-Wall@" + x + ", " + y + ", " + z;
        if (name != "")
            wallObject.transform.name = name;

        return wallObject;
    }
    
    public GameObject CreateCeilingObject(Vector3 v, bool active = true, int y1 = 0, int y2 = 0, int y3 = 0, int y4 = 0) {
        return null;
    }
    
    public GameObject CreateGroundPlaneObject(){
        float x_scale = (float) map_x_width / plane_x_width;
        float z_scale = (float) map_z_width / plane_z_width;
        GameObject plane = Instantiate(groundPrefab, new Vector3(plane_x_width * x_scale / 2, -0.01f, plane_z_width * z_scale / 2), Quaternion.identity, tileMaster.transform);
        plane.transform.localScale = new Vector3(x_scale, 1, z_scale);
        plane.transform.name = "Ground Plane";
        return plane;
    }

    public GameObject CreateCeilingObject(int x, int y, int z, bool active = true, TileShape shape = TileShape.Flat) {
        (int y1, int y2, int y3, int y4) = GetQuadVerticeHeights(shape);
        Vector3 v = new Vector3(x, y, z);
        GameObject c = CreateQuadObject(v, active, y1, y2, y3, y4);
        // Reflect ground to face floor
        c.transform.rotation = new Quaternion(c.transform.rotation.x, c.transform.rotation.y, c.transform.rotation.z, -c.transform.rotation.w);
        return c;
    }

    public void CreateEdgeObject(Edge e) {
        GameObject edgeObject = Instantiate(edgePrefab, Vector3.zero, edgePrefab.transform.rotation, tileMaster.transform);
        edgeObject.transform.name = "Edge";
    }

    public void UpdateProjectionPosition(Vector3 position){
        projectionFloor.transform.position = position;
        northProjectionWall.transform.position = position + WallPosition[Direction.North];
        southProjectionWall.transform.position = position + WallPosition[Direction.South];
        eastProjectionWall.transform.position = position + WallPosition[Direction.East];
        westProjectionWall.transform.position = position + WallPosition[Direction.West];
    }

    public Vector3 SpaceToTilePosition(Vector3 pos){
        return new Vector3(Mathf.Round(pos.x - half_x_width), curr_y, Mathf.Round(pos.z - half_z_width));
    }
    
    #endregion TilePlacement

    #region TileData

    public Tile CreateTile(int x, int y, int z, TileShape shape = TileShape.Flat, Direction slope = Direction.None){
        /*
            Default Tile constructor 
        */

        if (slope != Direction.None)
            ; // No difference in y if this is default Slope constructor
        Tile t = new Tile(x, y, z, shape);
        t.tileId = curr_int_id++;

        // Find if nearby tiles exist
        foreach (Tile _t in tiles){
            // If there is a tile in the same position. dy > 1 recommended otherwise
            if (DistEqual(t, _t))
                return null;
            // Add a weight if beside each other
            if (Mathf.Abs(_t.x - t.x) <= 1 && Mathf.Abs(_t.z - t.z) <= 1) {
                /*  Slope Direction points from the lower tile to the higher tile 
                    Tile Direction points from source tile to destination tile 
                    If non-cardinal, angular ties decided in NSEW order.
                */  
                (int weight, bool isInf, bool isSlp) = CalculateTravelCost(t, _t, slope);
                t.edges.Add(CreateEdge(weight, _t, isInf, isSlp));
            }
        }

        // Note: Links must exist bothways, infinite if inaccessible
        // tiles.Add(t);
            if (buildMode == TileBuildMode.BuildAtop && activeTile is not null) {
                t.y = curr_y + activeTile.y;
                t.ceil_y = activeTile.ceil_y;
            }

            GameObject tileObject = CreateTileObject(projectionFloor.transform.position, curr_tilingShape, curr_direction, true);
            if (t is null) {
                return null;
            }
            Debug.Log("Creating tile at " + t.x + ", " + t.y + ", " + t.z);

            /*
                Procedure for wall building:
                
                Anytime you would build a wall, if a wall already exists, overwrite

                Intuitions:

                Calculate ground floor somehow 
                    Or maybe track height:
                        Tile.height = exact to ground
                        We can then just use this as gospel.
                        What is a slope's height!? Since it goes from 'lower' to 'higher',
                        It's height is the (int) x + y / 2.

                    if static mode
                        just use current max_floor_height setting
                    else if dynamic mode
                        for each surrounding tile, do the following and pick the highest
                            Where the floor begins then:                
                                Trace the walls down till it stops. That defines the ground floor
                                If no walls, height is 0.

                                The next floor (joined by walls) below  X
                                If no such floor, assume it goes to 0   X
                                If error such that walls end without a floor at the end, X

                        In the case that there are MULTIPLE floors, 
                            Follow the highest floor
                                
                    Only ever search floors till the ground level. (default, 0)

                When building a floor beside another floor,
                    - If this new floor is higher, build walls all the way down
                        Build walls UNTIL the lower floor
                        Delete walls between the floors
                    - If this floor is of the same heigher, don't build
                        Delete walls between the floors
                    - If this floor is lower, don;t build
                        Delete walls between the floors

            */

            // Find adjacecnt tiles
            List<Tile> /*North*/ n = GetTiles(curr_x, curr_z + 1);
            List<Tile> /*South*/ s = GetTiles(curr_x, curr_z - 1);
            List<Tile> /*East */ e = GetTiles(curr_x + 1, curr_z);
            List<Tile> /*West */ w = GetTiles(curr_x - 1, curr_z);
            List<Tile> /*Here */ h = GetTiles(curr_x, curr_z);
            List<Tile> /*Adjacent*/ a = new List<Tile> (n); // Convenience Variable
            List<List<Tile>> /*Directional*/ l = new List<List<Tile>> ();
            List<Direction> dirs = new List<Direction>() { Direction.North, Direction.South, Direction.East, Direction.West};
            List<Direction> iDirs = new List<Direction>() { Direction.South, Direction.North, Direction.West, Direction.East};
            a.AddRange(s); a.AddRange(e); a.AddRange(w);
            l.Add(n); l.Add(s); l.Add(e); l.Add(w);


            // Determine ground level (Where to build walls to)
            int groundLevel = 0;
            switch (wallMode) {
                case AutoWallMode.BuildToGround:
                    groundLevel = 0;
                    break;
                case AutoWallMode.Static:
                    groundLevel = chosenGroundLevel;
                    break;
                case AutoWallMode.Dynamic:
                    if (a.Count !> 0) {
                        break;
                    }
                    Tile _t = a[0];
                    int d = Mathf.Abs(_t.y - t.y);
                    int _y = _t.y;

                    for (int i = 1; i < a.Count; i++) {
                        Tile __t = a[i];
                        // If tie, choose the higher || If closer, choose this!
                        if (((Mathf.Abs(__t.y - t.y) == d) && (__t.y > _t.y)) || (Mathf.Abs(__t.y - t.y) < d)) {
                            _t = __t;
                        }
                    }

                    // Get Floor
                    groundLevel = _t.ceil_y;
                    break;
                case AutoWallMode.None:
                    groundLevel = t.y;
                    break;
            }
            // Ground level now decides height volume
            t.ceil_y = groundLevel;

            // Determine new groundLevel based on tiles on this current area.
            foreach (Tile _h in h) {
                if (Covered(_h.y, t.ceil_y, t.y)) { // Inclusive on both ends, top inclusive = replace, btm inclusive = btm tile removed
                    t.ceil_y = Mathf.Min(t.ceil_y, _h.ceil_y);
                    // Note*: groundLevel remains the same - Do not need to update groundLevel beyond to _h.ceil_y since the walls are presumably already built.
                }
            }

            Tile _del = null;
            // Check if tile exists within the volume of another tile or vice versa
            foreach (Tile _h in h) {
                if (Covered(_h.y, t.ceil_y, t.y) /* Tile contains a lower tile OR same position */) {
                    // Remove ceiling
                    if (t.ceil_y != _h.ceil_y) {
                        DeleteCeiling(t.x, _h.ceil_y, t.z);
                        if (t.ceil_y >= 0) // No ceilings below ground level
                            CreateCeiling(t.x, t.ceil_y, t.z);
                    }
                    // Remove _h
                    _del = _h;
                    DeleteTile(_h);
                } else if ((_h.ceil_y <= t.y) && (t.y <= _h.y) /* Tile is contained in another higher tile */) { 
                    // Remove walls one space high, for tile t to breathe
                    DeleteWalls(curr_x, curr_y, curr_z);
                    CreateCeiling(curr_x, curr_y+1, curr_z);
                    // This may have exposed walls on neighbouring tiles
                    foreach (Tile _a in a) {
                        if (Covered(curr_y, _a.ceil_y, _a.y - 1)) { /* Walls at ceiling level or 1 below floor level */
                            Direction _dir = GetDirection(_a, t);
                            CreateWall(_a.x, curr_y, _a.z, _dir);
                        }
                    }
                }
            }

            // Delete Walls
            for (int i = 0; i < l.Count; i++) {
                List<Tile> _l = l[i];
                foreach (Tile _a in _l) { // adjacent
                    for (int u = Mathf.Min(_a.y, t.y) - 1; (u >= _a.ceil_y) && (u >= t.ceil_y); u--) {
                        DeleteWall(_a.x, u, _a.z, iDirs[i]);
                    }
                }
            }
            
            // Build Walls
            for (int i = t.y - 1; i >= t.ceil_y; i--) { // spanning the volume of the tile...
                // Check each direction
                for (int u = 0; u < l.Count; u++) {
                    List<Tile> _l = l[u];
                    bool covered = false;
                    Direction _dir = dirs[u];
                    // Build walls if no surrounding tile covers this location
                    foreach (Tile _t in _l) {
                        if (Covered(i, _t.ceil_y, _t.y - 1)) { /* Walls covered if at some tile's ceiling level or 1 below floor level */
                            covered = true;
                            break; // Break Tiles _l loop
                        }
                    }
                    // If no previous deleted tile covered this location
                    if (_del is not null && Covered(i, _del.ceil_y, _del.y - 1))
                        covered = true;
                    // If not covered by anything, build a wall here!
                    if (!covered) {
                        Debug.Log(_dir);
                        CreateWall(t.x, i, t.z, _dir);
                    }
                }
            }

            // Set Tile Object
            tileObject.GetComponent<TileMono>().tile = t;

            /*
                Print Tiles map
            */
            tiles.Add(t);
            tileObjects.Add(tileObject);
        return t;
    }

    public Tile CreateTileData() {
        /* Does not create physical tiles, neither does it care about wall fusings etc. */
        return null;
    }

    public Wall CreateWall(int x, int y, int z, /* WallShape shape */ Direction d ) {
        Wall wall = new Wall(x, y, z, d);
        GameObject wallObject = CreateWallObject(x, y, z, d);
        wallObject.GetComponent<WallMono>().wall = wall;
        // Add to collection
        walls.Add(wall);
        wallObjects.Add(wallObject);

        return wall;
    }

    public Ceiling CreateCeiling(int x, int y, int z) {
        // Create ceiling
        Ceiling ceiling = new Ceiling(x, y, z);
        // Create ceiling object
        GameObject ceilingObject = Instantiate(ceilingPrefab, new Vector3(x, y, z), ceilingPrefab.transform.rotation, tileMaster.transform);
        ceilingObject.transform.name = "Ceiling@" + x + ", " + y + ", " + z;
        // ceilingObject.GetComponent<CeilingMono>().ceiling = ceiling;
        // Add to list
        ceilings.Add(ceiling);
        ceilingObjects.Add(ceilingObject);

        return ceiling;
    }

    public void CreateSlopeTile(int x, int y, int z, Tile t, Tile _t) {
        y = (int) ((t.y + _t.y) / 2);
    }

    public void CreateTeleporterTile(int x, int y, int z, Tile t, Tile _t) {

    }

    public void CreateLadder(){

    }

    public void CreatePortal(){
        
    }

    public void CreateTraversableDestructible(){

    }

    public void CreateTraversableObject(){

    }

    public void CreateInteractibleTile(){

    }

    public void DeleteTile(Tile tile){
        // Delete all links in other Tiles
        foreach (Tile _tile in tiles) {
            if (tile == _tile) {
                foreach (GameObject tileObject in tileObjects) {
                    if (tile == tileObject.GetComponent<TileMono>().tile) {
                        tileObjects.Remove(tileObject);
                        Destroy(tileObject);
                        break;
                    }
                }
                tiles.Remove(tile);
                break;
            }
        }

    }

    public void DeleteWall(int x, int y, int z, Direction dir) {
        foreach (Wall wall in walls) {
            if (wall.x == x && wall.y == y && wall.z == z && wall.direction == dir) {
                foreach (GameObject wallObject in wallObjects) {
                    if (wallObject.GetComponent<WallMono>().wall == wall) {
                        wallObjects.Remove(wallObject);
                        Destroy(wallObject);
                        break;
                    }
                }
                walls.Remove(wall);
                break;
            }
        }
    }

    public void DeleteWalls(int x, int y, int z) {
        foreach (Wall wall in walls) { // TODO there is an error here!
            if (wall.x == x && wall.y == y && wall.z == z) {
                foreach (GameObject wallObject in wallObjects) {
                    if (wallObject.GetComponent<WallMono>().wall == wall) {
                        wallObjects.Remove(wallObject);
                        Destroy(wallObject);
                        break;
                    }
                }
                walls.Remove(wall);
                break;
            }
        }
    }

    public void DeleteCeiling(int x, int y, int z) {
        foreach (Ceiling ceiling in ceilings) {
            if (ceiling.x == x && ceiling.y == y && ceiling.z == z) {
                foreach (GameObject ceilingObject in ceilingObjects) {
                    if (ceilingObject.GetComponent<CeilingMono>().ceiling == ceiling) {
                        ceilingObjects.Remove(ceilingObject);
                        Destroy(ceilingObject);
                        break;
                    }
                }
                ceilings.Remove(ceiling);
                break;
            }
        }
    }

    public Tile GetTile() {

        return null;
    }

    public Tile GetTile(int x, int y, int z) {
        return null;
    }

    public List<Tile> GetTiles(int x, int z) {
        /* 

        */

        List<Tile> r = new List<Tile>();

        foreach (Tile t in tiles) {
            if ((t.x == x) && (t.z == z)) {
                r.Add(t);
            }
        }

        return r;
    }

    public Tile GetNearestLinkedTile(int x, int z, Tile t) {

        Tile _t = null;
        List<Tile> candidateTiles = new List<Tile>();

        foreach (Tile __t in tiles) {
            if ((__t.x == x) && (__t.z == z)) {
                candidateTiles.Add(__t);
            }
        }

        if (candidateTiles.Count !> 0)
            return _t;

        _t = candidateTiles[0];
        int d = Mathf.Abs(_t.y - t.y);
        int _y = _t.y;
        for (int i = 1; i < candidateTiles.Count; i++) {
            Tile __t = candidateTiles[i];
            int _d = Mathf.Abs(__t.y - t.y);
            // If closer || Same distance but higher
            if ((d > _d) || ((d == _d) && (__t.y > _y)))
                _t = __t;
        }

        return _t;
    }

    public Edge CreateEdge(int weight, Tile toTile, bool isInf, bool isSlp) {
        return new Edge(weight, toTile, isInf, isSlp);
    }

    #endregion TileData

    #region Calculation
    public Quaternion GetQuaternion(Direction rotation) {
        switch (rotation) {
            case Direction.North:
                return new Quaternion(0, 1, 0, 0);
                break;
            case Direction.South:
                return new Quaternion(0, 0, 0, 1);
                break;
            case Direction.East:
                return new Quaternion(0, 0.7071068f, 0, -0.7071068f);
                break;
            case Direction.West:
                return new Quaternion(0, 0.7071068f, 0, 0.7071068f);
                break;
            case Direction.NorthEast:
                return new Quaternion(0, 0.3826834f, 0, 0.9238796f);
                break;
            case Direction.SouthEast:
                return new Quaternion(0, 0.3826836f, 0, -0.9238794f);
                break;
            case Direction.SouthWest:
                return new Quaternion(0, 0.9238796f, 0, -0.3826832f);
                break;
            case Direction.NorthWest:
                return new Quaternion(0, 0.9238796f, 0, 0.3826834f);
                break;
            case Direction.Up:
                return new Quaternion(0.7071068f, 0, 0, 0.7071068f);
                break;
            case Direction.Down:
                return new Quaternion(-0.7071068f, 0, 0, 0.7071068f);
                break;
        }
        return Quaternion.identity;
    }

    public (int, bool, bool) CalculateTravelCost(Tile t, Tile _t, Direction slope){
        /*
            Returns (weight, isInf, isSlope)
        */
        // Going from t to _t
        int w = 1;
        /* If slp:
            y = Mathf.Floor(t.y + _t.y / 2) therefore, it is still valid to use this tile's height to calculate slope traversability.
            a.k.a   Tile 0 to Tile Slope -> w = 1 + W(0, 0) // Weight function that calculated difficulty in traversing a slope between heights y1, y2.
                    Tile Slope to Tile 1 -> w = 1 + W(0, 1) = 1 + 0.5 ~= 1
                But
                    Tile 0 to Tile Slope -> w = 1 + W(0, 5/2~=2) = 1 + W(2)
                    Tile Slope to Tile 5 -> w = 1 + W(5/2, 5) = 1 + W(3)
        */
        bool slp = slope == GetDirection(t, _t);
        int d = - t.y + _t.y; // Positive: Ascending, Negative: Descending
        if (Mathf.Abs(d) > MAX_TRAVERSABLE_SLOPE_HEIGHT)
            return (0, true, slp);      // Beyond this level of attack, slope is untraversable through walking.
        /* Climbing */
        if (!slp && d == -1)            // Minor climbing down
            w += 1;
        if (!slp && d == 1)             // Minor climbing up
            w += 2;
        if (!slp && Mathf.Abs(d) > 1)   // Major climbing (up or down) - impossible through default movement
            return (w + 1, true, slp);

        /* Ascending/Descending */
        if (d < 0) {                    // Descending
            // No additional weight for going down
        } 
        if (d > 0) { // Ascending
            if (d > SLOPE_WEIGHTS.Count)// Outside SLOPE_WEIGHTS, untraversable
                return (0, true, slp);       
            w += SLOPE_WEIGHTS[d];      // SLOPE_WEIGHTS traversable
        }

        /* Tile Effects */
        // do this do that

        
        return (w, false, slp);
    }

    public Direction GetDirection(Tile t, Tile _t) {
        /*
            Origin Tile t, Destination Tile _ts
        */
        if (t.x > _t.x) {
            if (t.z > _t.z) {
                return Direction.SouthWest;
            } else if (t.z < _t.z) {
                return Direction.NorthWest;
            } else {
                return Direction.West;
            }
        } else if (t.x < _t.x) {
            if (t.z > _t.z) {
                return Direction.SouthEast;
            } else if (t.z < _t.z) {
                return Direction.NorthEast;
            } else {
                return Direction.East;
            }
        } else {
            if (t.z > _t.z) {
                return Direction.South;
            } else if (t.z < _t.z) {
                return Direction.North;
            } else {
                if (t.y > _t.y) {           // Comparing y has last priority
                    return Direction.Down;
                } else if (t.y < _t.y) {
                    return Direction.Up;
                } else {
                    return Direction.None;  // Literally in the same spot.
                }
            }
        }
    }

    public bool DistEqual(Tile t, Tile _t) {
        return (t.x == _t.x && t.y == _t.y && t.z == _t.z);
    }

    public Mesh GetTileMesh(TileShape shape, Direction direction, int height = 1) {
        (int y1, int y2, int y3, int y4) = GetQuadVerticeHeights(shape, direction, height);
        Mesh mesh = new Mesh();

        /* Mesh Vertices (Height) */
        Vector3[] vertices = new Vector3[4] {
            new Vector3(0, y1, 0), // Floor is rotated, so y is on z position
            new Vector3(x_width, y2, 0),
            new Vector3(x_width, y3, z_width),
            new Vector3(0, y4, z_width),
        };

        // TODO not so simple! need to rotate based on ne sw or nw se
        /* Mesh Triangulation */
        int[] tris = new int[6] {
            // Btm-Left Triangle
            0, 2, 1,
            // Top-Right Triangle
            0, 3, 2,
        };

        /* Mesh Normals */
        // Vector3[] normals = new Vector[4] {
        //     -Vector3.forward;
        //     -Vector3.forward;
        //     -Vector3.forward;
        //     -Vector3.forward;
        // }
        // mesh.normals = normals;

        /* Mesh Calculations */
        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        
        return mesh;
    }

    public (int, int, int, int) GetQuadVerticeHeights(TileShape shape, Direction direction = Direction.North, int height = 1) {
        int y1 = 0, y2 = 0, y3 = 0, y4 = 0;

        switch (shape) {
            case TileShape.Flat:
                return (y1, y2, y3, y4);
            case TileShape.Slope:
                switch (direction) {
                    case Direction.North:
                        return (y1, y2, height, height);
                    case Direction.South:
                        return (height, height, y3, y4);
                    case Direction.East:
                        return (y1, height, height, y4);
                    case Direction.West:
                        return (height, y2, y3, height);
                    default:
                        return (y1, y2, y3, y4);
                }
            case TileShape.Sharp:
                switch (direction) {
                    case Direction.NorthEast:
                        return (y1, y2, height, y4);
                    case Direction.SouthEast:
                        return (y1, height, y3, y4);
                    case Direction.SouthWest:
                        return (height, y2, y3, y4);
                    case Direction.NorthWest:
                        return (y1, y2, y3, height);
                    default:
                        return (y1, y2, y3, y4);
                }
            case TileShape.Corner:
                switch (direction) {
                    case Direction.NorthEast:
                        return (y1, height, height, height);
                    case Direction.SouthEast:
                        return (height, height, height, y4);
                    case Direction.SouthWest:
                        return (height, height, y3, height);
                    case Direction.NorthWest:
                        return (height, y2, height, height);
                    default:
                        return (y1, y2, y3, y4);
                }
            default:
                return (y1, y2, y3, y4);
        }
    }

    public ((int, int, int), (int, int, int)) GetQuadMeshTriangles(TileShape shape, Direction direction = Direction.North, int height = 1) {
        return ((2, 1, 3), (1, 2, 3));
    }

    public (int, int, int, int) GetQuadVerticeHeightsCardinal(Tile northTile, Tile southTile, Tile eastTile, Tile westTile) {        
        return ((northTile.y + westTile.y) / 2, (eastTile.y + northTile.y) / 2, (southTile.y + eastTile.y) / 2, (westTile.y + southTile.y) / 2);
    }

    public (int, int, int, int) GetQuadVerticeHeightsDiagonal(Tile northEast, Tile southEast, Tile southWest, Tile northWest) {
        return (northWest.y, northEast.y, southEast.y, northWest.y);
    }

    public Mesh GetWallMesh(WallShape shape) {
        Mesh mesh = new Mesh();
        return mesh;
    }

    // Note: (May Change) Meshes start at origin 0, 0, no matter what and need to be
    // translated. The translation coords are consistent.
    public Mesh GetWallMesh(TileShape shape, Direction tileDir, Direction wallDir) {
        Mesh mesh = new Mesh();

        /* Mesh Vertices (Height) */
        Vector3[] vertices = new Vector3[4] {
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 1, 0),
            new Vector3(1, 0, 0),
        };

        /* Mesh Triangulation */
        int[] tris = new int[6] {
            // Btm-Left Triangle
            0, 1, 2,
            // Top-Right Triangle
            0, 2, 3,
        };

        int intTileDir = -1;
        int intWallDir = DirectionToCardinal(wallDir);
        if (intWallDir == -1)
            return null; // Only cardinal walls allowed!

        switch (shape) {
            case TileShape.Flat:
                vertices = new Vector3[4] {
                    new Vector3(0, 0, 0),
                    new Vector3(0, 0, 0),
                    new Vector3(0, 0, 0),
                    new Vector3(0, 0, 0),
                };
                break;
            case TileShape.Slope:
                intTileDir = DirectionToCardinal(tileDir);
                if (intTileDir == -1)
                    return null; // No diagonal slopes!
                Debug.Log(wallDir);
                Debug.Log(intTileDir + ":" + intWallDir);
                if (intTileDir == intWallDir) { // 
                    // Do nothing - use wall vertices
                } else if ((intTileDir-intWallDir == 1) || (intTileDir-intWallDir == -3)) {
                    // Left wall
                    vertices = new Vector3[3] {
                        new Vector3(0, 0, 0),
                        new Vector3(0, 1, 0),
                        new Vector3(1, 0, 0),
                    };
                    tris = new int[3] {
                        0, 1, 2
                    };
                } else if ((intTileDir-intWallDir == -1) || (intTileDir-intWallDir == 3)) {
                    // Right wall
                    vertices = new Vector3[3] {
                        new Vector3(0, 0, 0),
                        new Vector3(0, 1, 0),
                        new Vector3(1, 0, 0),
                    };
                    tris = new int[3] {  // TODO I THINK THE ROTATION FUCKS THIS, SO GET IT RIGHT FOR ONE DIRECTION FIRST!
                        0, 2, 1 // WHY NOT NO DIRECTION. JUST MESH ONCE. NO ROTATION! SO CONFUSING WITH ROTATION
                    };
                } else {
                    vertices = new Vector3[4] {
                        // No surface
                        new Vector3(0, 0, 0),
                        new Vector3(0, 0, 0),
                        new Vector3(0, 0, 0),
                        new Vector3(0, 0, 0),
                    };
                }
                break;
            case TileShape.Sharp:
                intTileDir = DirectionToDiagonal(tileDir);
                if (intTileDir == -1)
                    return null; // No cardinal sharps!

                if ((intWallDir - intTileDir == 1) && (intWallDir - intTileDir == 0)) {
                    vertices = new Vector3[3] {
                        new Vector3(0, 0, 0),
                        new Vector3(0, 1, 0),
                        new Vector3(1, 0, 0),
                    };
                    tris = new int[3] {
                        0, 1, 2
                    };
                } else {
                    vertices = new Vector3[4] {
                    // No surface
                        new Vector3(0, 0, 0),
                        new Vector3(0, 0, 0),
                        new Vector3(0, 0, 0),
                        new Vector3(0, 0, 0),
                    };
                }
                break;
            case TileShape.Corner:
                intTileDir = DirectionToDiagonal(tileDir);
                if (intTileDir == -1)
                    return null; // No cardinal corners!

                if ((intWallDir - intTileDir == 1) && (intWallDir - intTileDir == 0)) {
                    // Do nothing - use wall vertices
                } else {
                    vertices = new Vector3[3] {
                        new Vector3(0, 0, 0),
                        new Vector3(0, 1, 0),
                        new Vector3(1, 0, 0),
                    };
                    tris = new int[3] {
                        0, 1, 2
                    };
                }
                break;
        }
        
        /* Mesh Calculations */
        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        return mesh;
    }


    #endregion Calculation
}

#region Enums, Classes and Interfacess
public enum Direction {
        North,      //  |   0
        NorthEast,  //  /   45
        East,       //  -   90
        SouthEast,  //  \   135  // Potentially use this to use enum Direction mathematically
        South,      //  |
        SouthWest,  //  /
        West,       //  -
        NorthWest,  //  \
        Up,         //  X
        Down,       //  O
        None,       //  0
    }

public enum DiagonalDirection {
    NorthEast,
    SouthEast,
    SouthWest,
    NorthWest,
}

public enum CardinalDirection {
    North,
    South,
    East,
    West,
}

public enum TileShape { 
        Flat, // Might* have directional difference depending on tile texture
        Slope, // Requires Direction
        Sharp, // Requires Direction
        Corner,
    }

public enum WallShape {

}

public enum LiquidType{
    Lava,
    Water,
    Saltwater,
    AcidWater,
    BlightWater,
    PoisonWater,
    DirtyWater,
    ColdWater,
    Permafrost,
    Quicksand,
    Void,
}

public class Traversable {

}

public interface Traversable2 {

}

public class Tile: Traversable {

    public int tileId;
    public System.Guid tileUid;
    // public TerrainType terrainType;
    public TileShape shape;
    public Direction dir;

    // Position
    public int x;
    public int y;
    public int z;

    // Path weights
    public List<Edge> edges = new List<Edge>();

    // Slope options
    public Direction slopeDir;
    public int low_y;
    public int high_y;

    // Physical options
    // public int height; // How long walls go down for (y - ceil_y)
    public bool hasCeiling; // If there is a ground floor below (none at -1, 0 or if no space)
    /// volume === h, volumed enveloped by tile
    public int ceil_y; // Where the walls stops
    
    public override string ToString()
    {
        string base_str = $"Tile-{tileId}@({x}, {y}, {z})";
        foreach (Edge e in edges){
            base_str += $" {e}";
        }
        return base_str;
    }

    private void SetupTile(int x, int y, int z, TileShape shape, int height){

    }
    
    public Tile(int x, int y, int z, TileShape shape = TileShape.Flat, int height = -1){
        this.x = x;
        this.y = y;
        this.z = z;
        tileUid = System.Guid.NewGuid();
        this.shape = shape;

        if (height < 0) {
            this.ceil_y = 0;
        } else 
            this.ceil_y = y - height;
    }

    public Tile(int x, int y, int z, List<Tile> tiles, TileShape shape = TileShape.Flat, int height = -1){
        this.x = x;
        this.y = y;
        this.z = z;
        tileUid = System.Guid.NewGuid();
        this.shape = shape;

        foreach (Tile tile in tiles) {
            edges.Add(new Edge(1, tile, false, false));
        }

        if (height < 0) {
            this.ceil_y = 0;
        } else 
            this.ceil_y = y - height;
    }

    public Tile(int x, int y, int z, Direction slopeDir, int low_y, int high_y, int height = -1) {
        this.x = x;
        this.y = y;
        this.z = z;
        tileUid = System.Guid.NewGuid();
        this.shape = TileShape.Slope;
        this.dir = slopeDir;

        this.slopeDir = slopeDir;
        this.low_y = low_y;
        this.high_y = high_y;

        if (height < 0) {
            this.ceil_y = 0;
        } else 
            this.ceil_y = y - height;
    }

    public static Tile Empty = new Tile(0, 0, 0);
    public static Tile NewEmpty() {
        return new Tile(0, 0, 0);
    }
}

public class Portal: Traversable {
    public int x {get; private set;}
    public int y {get; private set;}
    public int z {get; private set;}

    public Portal(int x, int y, int z){
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

public class Edge {
    int weight;
    Tile tile;
    bool isInf;
    bool slope; // Slope = tile.y to _tile.y, False = Steep wall
    public override string ToString()
    {
        string base_str = $"Edge@{tile.tileId} of {weight}, inf: {isInf}, sloped: {slope}";
        return base_str;
    }
    public string ToShortString() {
        string short_str = $"{weight}:{tile.tileId}";
        return short_str;
    }
    public Edge(int w, Tile t, bool inf, bool slp) {
        weight = w;
        tile = t;
        isInf = inf;
        slope = slp;
    }
}

public class Wall: Traversable {

    public Direction direction;
    public WallShape shape;

    // Position
    public int x;
    public int y;
    public int z;

    public Wall(int x, int y, int z, Direction direction) {
        this.direction = direction;
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

public class Ceiling: Traversable {
    public TileShape shape;

    // Position
    public int x;
    public int y;
    public int z;
    public Ceiling(int x, int y, int z, TileShape shape = TileShape.Flat){
        this.x = x;
        this.y = y;
        this.z = z;
        this.shape = shape;
    }
}

public class Ladder: Traversable {

}

#endregion Enums, Classes


/* TODO LIST */
//
// Create Walls
// TileShape UI scroll on the side
//      Programmatically create options on the left, which TileShapes are available
// Different tile textures

// Make walls HALF as tall

// Delete Tile
// Adjust a tile that has already been placed
//  Shift tileShape, shift position -> maybe not. maybe only shift
//

// Languages
// Injection
// Object Pooling
// Fx pooling, loading ,resourcec management,
// Keyboard controls configuration

// Sloped Tiles
// Create ground 

// Move create tile from ActionCT to CreateTile()
// Change the way sloped walls are made!
// Shift+Scroll -> Tile type vs Tile Shape
// Build mountain with one click
// UI!

// convert all Object x Direction x Shape to be centered on 0, 0 and 'mesh-out' from there
// so that, you may place ANY object at x,y,z for example, and e.g. if it was a right wall
// it would be the 'right wall' of x,y,z.

/* Scroll Wheel */

// Roll to rotate
// Shift Roll to change attach points
// Shift move so that tile does not auto change shape


// once tile building done, before going into map saving or map loading
// or loading assets onto the tiles randomly/pseudorandomly/preset/load-statically
// FIRST, try unity dependency injection