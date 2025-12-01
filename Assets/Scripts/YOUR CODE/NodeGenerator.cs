using UnityEngine;

/// <summary>
/// Creates pathfinding nodes at runtime from the map data.
/// Nodes are placed on tiles that are considered navigable for pathfinding.
/// Optionally: every Grass tile will get a node to match the movement space.
/// Movement cost between tiles is handled by A* based on terrain type.
/// </summary>
public class NodeGenerator : MonoBehaviour
{
    [Tooltip("Prefab that contains the Node component.")]
    public GameObject nodePrefab;

    [Header("Formation Clearance")]
    [Tooltip("Radius in world units that should be free around each node for the formation to fit. " +
             "Set to 0 or disable 'Apply Formation Clearance' if you want nodes on every grass tile.")]
    public float formationClearanceRadiusWorld = 1.5f;

    [Tooltip("If true, the clearance radius is used to prune nodes near obstacles. " +
             "If false, every candidate tile gets a node.")]
    public bool applyFormationClearance = false;

    [Header("Node Placement")]
    [Tooltip("If true, put a node on every Grass tile. " +
             "If false, use Map.IsNavigatable instead.")]
    public bool generateNodesOnAllGrass = true;

    private Node[,] nodeGrid;

    private void Start()
    {
        if (GameData.Instance == null)
        {
            Debug.LogError("NodeGenerator: GameData.Instance is null.");
            return;
        }

        Map map = GameData.Instance.Map;

        if (map == null)
        {
            Debug.LogError("NodeGenerator: GameData.Instance.Map is null.");
            return;
        }

        if (nodePrefab == null)
        {
            Debug.LogError("NodeGenerator: nodePrefab is not assigned.");
            return;
        }

        GenerateNodes(map);
        ConnectNeighbours();

        if (AStar.Instance != null)
        {
            AStar.Instance.RebuildGraph();
        }
        else
        {
            Debug.LogWarning("NodeGenerator: No AStar.Instance found in scene.");
        }
    }

    /// <summary>
    /// Generates a grid of Node objects for tiles that are navigable
    /// and also (optionally) have enough free space around them for the formation.
    /// </summary>
    private void GenerateNodes(Map map)
    {
        int width = Map.MapWidth;
        int height = Map.MapHeight;

        nodeGrid = new Node[width, height];

        float worldWidth = MapRenderer.MapWidthInWorld;
        float worldHeight = MapRenderer.MapHeightInWorld;

        // Estimate tile size in world units to convert clearance radius to tile units.
        float tileWidth = worldWidth / Mathf.Max(width, 1);
        float tileHeight = worldHeight / Mathf.Max(height, 1);
        float avgTileSize = (tileWidth + tileHeight) * 0.5f;

        int clearanceTiles = 0;

        if (formationClearanceRadiusWorld > 0f && avgTileSize > 0f)
        {
            clearanceTiles = Mathf.CeilToInt(formationClearanceRadiusWorld / avgTileSize);
        }

        bool useClearance = applyFormationClearance && clearanceTiles > 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool placeNode;

                if (generateNodesOnAllGrass)
                {
                    // Node on every Grass tile (bright green).
                    Map.Terrain t = map.GetTerrainAt(x, y);
                    placeNode = (t == Map.Terrain.Grass);
                }
                else
                {
                    // Legacy behaviour: Map.IsNavigatable decides.
                    placeNode = map.IsNavigatable(x, y);
                }

                if (!placeNode)
                    continue;

                // Optional formation-clearance filter.
                if (useClearance &&
                    !HasFormationClearance(map, x, y, width, height, clearanceTiles))
                    continue;

                float worldX = ((float)x + 0.5f) / width * worldWidth;
                float worldY = ((float)y + 0.5f) / height * worldHeight;

                Vector3 pos = new Vector3(worldX, worldY, 0f);
                GameObject nodeObj = Instantiate(nodePrefab, pos, Quaternion.identity, transform);
                nodeObj.name = $"Node_{x}_{y}";

                Node node = nodeObj.GetComponent<Node>();
                node.mapX = x;
                node.mapY = y;

                nodeGrid[x, y] = node;
            }
        }

        Debug.Log("NodeGenerator: generated nodes (Grass=" + generateNodesOnAllGrass +
                  ", Clearance=" + useClearance + ", clearanceTiles=" + clearanceTiles + ")");
    }

    /// <summary>
    /// Checks whether a circle of tiles around (x, y) is fully navigable.
    /// The radius is expressed in tiles and is used to guarantee that the
    /// whole formation can fit around this node without colliding with obstacles.
    /// Out-of-bounds tiles are ignored instead of blocking the node, so
    /// we can still generate nodes near the map border.
    /// </summary>
    private bool HasFormationClearance(Map map, int x, int y, int width, int height, int radiusTiles)
    {
        int radiusSq = radiusTiles * radiusTiles;

        for (int oy = -radiusTiles; oy <= radiusTiles; oy++)
        {
            for (int ox = -radiusTiles; ox <= radiusTiles; ox++)
            {
                // Skip positions outside the circle radius.
                if (ox * ox + oy * oy > radiusSq)
                    continue;

                int cx = x + ox;
                int cy = y + oy;

                // Ignore out-of-bounds samples instead of treating them as blocked.
                if (cx < 0 || cx >= width || cy < 0 || cy >= height)
                    continue;

                if (!map.IsNavigatable(cx, cy))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Connects neighbouring nodes in the four cardinal directions (up, down, left, right).
    /// This defines the edges of the graph that A* will search over.
    /// </summary>
    private void ConnectNeighbours()
    {
        if (nodeGrid == null)
            return;

        int width = nodeGrid.GetLength(0);
        int height = nodeGrid.GetLength(1);

        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Node node = nodeGrid[x, y];
                if (node == null)
                    continue;

                node.neighbours.Clear();

                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];

                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        continue;

                    Node neighbour = nodeGrid[nx, ny];

                    if (neighbour != null)
                    {
                        node.neighbours.Add(neighbour);
                    }
                }
            }
        }

        Debug.Log("NodeGenerator: neighbours linked.");
    }
}
