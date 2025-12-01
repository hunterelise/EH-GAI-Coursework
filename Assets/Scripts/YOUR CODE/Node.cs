using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple graph node used by A*.
/// Neighbours should be wired up by a generator or editor script.
/// </summary>
public class Node : MonoBehaviour
{
    public int mapX;
    public int mapY;

    public List<Node> neighbours = new List<Node>();

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.2f);

        Gizmos.color = Color.yellow;
        foreach (var n in neighbours)
        {
            if (n != null)
                Gizmos.DrawLine(transform.position, n.transform.position);
        }
    }
}
