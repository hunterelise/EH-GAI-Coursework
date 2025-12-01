using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Steering behaviour that follows an A* path (AStar + Node).
/// This component only produces a steering velocity; it does not move the transform directly.
/// </summary>
public class AllyPathfinder : SteeringBehaviour
{
    [Tooltip("World-space destination this agent should pathfind toward.")] //change2 comments
    public Vector3 Destination;

    [Tooltip("Distance to a waypoint before advancing to the next one.")]
    public float waypointTolerance = 0.4f;

    [Tooltip("How often, in seconds, to recompute the A* path.")]
    public float repathInterval = 0.1f;

    [Header("Smoothing")]
    [Tooltip("How many waypoints ahead to look when smoothing corners.")]
    public int lookAheadSteps = 1;

    [Tooltip("Multiplier for waypointTolerance to define the slow-down radius near the final point.")]
    public float arriveRadiusMultiplier = 3f;

    private List<Vector3> pathPoints = new List<Vector3>();
    private int pathIndex;
    private float repathTimer;

    private AllyFSM cachedFsm;

    /// <summary>
    /// Sets a new destination and immediately recalculates the path.
    /// If this agent is the leader, the resulting path is shared with the squad manager.
    /// </summary>
    public void SetDestination(Vector3 dest)
    {
        Destination = dest;
        RecalculatePath();

        var squad = AllySquadManager.Instance;
        var fsm = GetOrCacheFsm();

        // Only the leader publishes the path to the squad, and only if it's non-empty.
        if (squad != null && fsm != null && fsm.Role == AllyFSM.AllyRole.Leader &&
            pathPoints != null && pathPoints.Count > 0)
        {
            squad.SetLeaderPath(pathPoints);
        }
    }

    /// <summary>
    /// Requests a new path from the global AStar instance from the current position to Destination.
    /// Resets the path index to start following from the beginning.
    /// If A* fails, the previous path is kept so followers don't freeze.
    /// </summary>
    private void RecalculatePath()
    {
        repathTimer = 0f;

        if (AStar.Instance == null)
        {
            return;
        }

        var newPath = AStar.Instance.FindPath(transform.position, Destination);
        if (newPath == null || newPath.Count == 0)
        {
            Debug.LogWarning($"{name}: A* failed to find path to {Destination}, keeping previous path.");
            return;
        }

        pathPoints = newPath;
        pathIndex = 0;
    }

    /// <summary>
    /// Computes the steering velocity needed to follow the current A* path.
    /// Also periodically refreshes the path and updates the squad leader path if this is the leader.
    /// </summary>
    public override Vector3 UpdateBehaviour(SteeringAgent agent)
    {
        if (!enabled || AStar.Instance == null)
        {
            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        // Recalculate the path at a fixed time interval.
        repathTimer += Time.deltaTime;
        if (repathTimer >= repathInterval)
        {
            RecalculatePath();

            var squad = AllySquadManager.Instance;
            var fsm = GetOrCacheFsm();

            // Keep the shared leader path up to date if we have one.
            if (squad != null && fsm != null && fsm.Role == AllyFSM.AllyRole.Leader &&
                pathPoints != null && pathPoints.Count > 0)
            {
                squad.SetLeaderPath(pathPoints);
            }
        }

        // No valid path.
        if (pathPoints == null || pathPoints.Count == 0 || pathIndex >= pathPoints.Count)
        {
            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        Vector3 currentPos = agent.transform.position;

        // Handle waypoint advancement using the raw waypoint list.
        Vector3 waypoint = pathPoints[pathIndex];
        waypoint.z = currentPos.z;

        Vector3 toWaypoint = waypoint - currentPos;
        toWaypoint.z = 0f;

        if (toWaypoint.magnitude <= waypointTolerance)
        {
            pathIndex++;
            if (pathIndex >= pathPoints.Count)
            {
                desiredVelocity = Vector3.zero;
                steeringVelocity = Vector3.zero;
                return steeringVelocity;
            }

            waypoint = pathPoints[pathIndex];
            waypoint.z = currentPos.z;
            toWaypoint = waypoint - currentPos;
            toWaypoint.z = 0f;
        }

        if (toWaypoint.sqrMagnitude < 0.0001f)
        {
            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        // Smoothing: aim toward a point slightly ahead on the path to reduce sharp turns.
        int lookAheadIndex = Mathf.Min(pathIndex + Mathf.Max(lookAheadSteps, 0), pathPoints.Count - 1);

        Vector3 target = pathPoints[pathIndex];
        if (lookAheadIndex != pathIndex)
        {
            // Blend between the current waypoint and a future one.
            target = Vector3.Lerp(pathPoints[pathIndex], pathPoints[lookAheadIndex], 0.5f);
        }
        target.z = currentPos.z;

        Vector3 diff = target - currentPos;
        diff.z = 0f;

        if (diff.sqrMagnitude < 0.0001f)
        {
            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        // Compute desired velocity in the direction of the smoothed target.
        Vector3 dir = diff.normalized;
        float speed = SteeringAgent.MaxCurrentSpeed;

        // On the final segment, gradually slow down as we approach the end.
        bool isLastPoint = (pathIndex >= pathPoints.Count - 1);
        if (isLastPoint)
        {
            float dist = diff.magnitude;
            float arriveRadius = waypointTolerance * Mathf.Max(arriveRadiusMultiplier, 1f);

            if (dist < arriveRadius)
            {
                float t = dist / Mathf.Max(arriveRadius, 0.001f);
                speed *= Mathf.Clamp01(t);
            }
        }

        desiredVelocity = dir * speed;
        steeringVelocity = desiredVelocity - agent.CurrentVelocity;

        return steeringVelocity;
    }

    /// <summary>
    /// Returns a cached reference to the AllyFSM on this GameObject.
    /// Avoids repeated GetComponent calls.
    /// </summary>
    private AllyFSM GetOrCacheFsm()
    {
        if (cachedFsm == null)
            cachedFsm = GetComponent<AllyFSM>();

        return cachedFsm;
    }
}
