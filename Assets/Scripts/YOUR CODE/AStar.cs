using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple A* pathfinding system that works on Node objects in the scene.
/// There should be exactly one instance of this in the scene.
/// </summary>
public class AStar : MonoBehaviour
{
    public static AStar Instance { get; private set; }

    private Node[] allNodes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Nodes are created at runtime by NodeGenerator, which then calls RebuildGraph.
    }

    /// <summary>
    /// Finds and caches all Node components in the scene.
    /// Call this after all nodes have been generated.
    /// </summary>
    public void RebuildGraph()
    {
        allNodes = FindObjectsOfType<Node>();
        Debug.Log("AStar: rebuilt graph, nodes = " + (allNodes != null ? allNodes.Length : 0));
    }

    /// <summary>
    /// Returns the closest node to a given world position, or null if none exist.
    /// </summary>
    public Node GetClosestNode(Vector3 position)
    {
        if (allNodes == null || allNodes.Length == 0)
            return null;

        Node best = null;
        float bestDistSqr = float.MaxValue;

        foreach (var n in allNodes)
        {
            if (n == null) continue;

            float d2 = (n.transform.position - position).sqrMagnitude;
            if (d2 < bestDistSqr)
            {
                bestDistSqr = d2;
                best = n;
            }
        }

        return best;
    }

    public List<Vector3> FindPath(Vector3 startPos, Vector3 endPos)
    {
        var result = new List<Vector3>();

        Node startNode = GetClosestNode(startPos);
        Node goalNode = GetClosestNode(endPos);

        // If we can't even get nodes, fall back to straight line.
        if (startNode == null || goalNode == null)
        {
            Debug.LogWarning($"AStar: no start or goal node found. Using straight-line fallback {startPos} -> {endPos}");
            result.Add(startPos);
            result.Add(endPos);
            return result;
        }

        var openList = new List<NodeRecord>();
        var closedSet = new HashSet<Node>();

        NodeRecord startRecord = new NodeRecord
        {
            node = startNode,
            parent = null,
            g = 0f,
            h = Vector3.Distance(startNode.transform.position, goalNode.transform.position)
        };
        startRecord.f = startRecord.g + startRecord.h;
        openList.Add(startRecord);

        NodeRecord current = null;

        Map map = GameData.Instance != null ? GameData.Instance.Map : null;

        while (openList.Count > 0)
        {
            // Select the node with the lowest f cost.
            current = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].f < current.f)
                    current = openList[i];
            }

            if (current.node == goalNode)
                break; // Reached the goal.

            openList.Remove(current);
            closedSet.Add(current.node);

            foreach (var neighbour in current.node.neighbours)
            {
                if (neighbour == null || closedSet.Contains(neighbour))
                    continue;

                // Movement cost between nodes with an optional terrain penalty.
                float baseCost = Vector3.Distance(
                    current.node.transform.position,
                    neighbour.transform.position);

                float terrainPenalty = 1f;

                if (map != null)
                {
                    Map.Terrain terrain = map.GetTerrainAt(neighbour.mapX, neighbour.mapY);

                    switch (terrain)
                    {
                        case Map.Terrain.Grass:
                            terrainPenalty = 1f;   // Preferred terrain.
                            break;
                        case Map.Terrain.Mud:
                            terrainPenalty = 3f;   // High cost.
                            break;
                        case Map.Terrain.Water:
                            terrainPenalty = 2f;   // Medium cost.
                            break;
                        case Map.Terrain.Tree:
                            terrainPenalty = 100f; // Very high cost, but still allowed (if you ever had nodes there).
                            break;
                    }
                }

                float g = current.g + baseCost * terrainPenalty;

                NodeRecord neighbourRecord = openList.Find(r => r.node == neighbour);

                if (neighbourRecord == null)
                {
                    neighbourRecord = new NodeRecord
                    {
                        node = neighbour,
                        parent = current,
                        g = g,
                        h = Vector3.Distance(neighbour.transform.position, goalNode.transform.position)
                    };
                    neighbourRecord.f = neighbourRecord.g + neighbourRecord.h;
                    openList.Add(neighbourRecord);
                }
                else if (g < neighbourRecord.g)
                {
                    neighbourRecord.g = g;
                    neighbourRecord.parent = current;
                    neighbourRecord.f = neighbourRecord.g + neighbourRecord.h;
                }
            }
        }

        // A* gave up without reaching the goal ? straight-line fallback.
        if (current == null || current.node != goalNode)
        {
            Debug.LogWarning($"AStar: no path found from {startPos} to {endPos}. Using straight-line fallback.");
            result.Add(startPos);
            result.Add(endPos);
            return result;
        }

        // Build the path by walking back through the parents.
        var nodePath = new List<Node>();
        while (current != null)
        {
            nodePath.Add(current.node);
            current = current.parent;
        }
        nodePath.Reverse();

        foreach (var n in nodePath)
            result.Add(n.transform.position);

        // Add the exact target position at the end of the path.
        result.Add(endPos);

        return result;
    }

    private class NodeRecord
    {
        public Node node;
        public NodeRecord parent;
        public float g;
        public float h;
        public float f;
    }
}
