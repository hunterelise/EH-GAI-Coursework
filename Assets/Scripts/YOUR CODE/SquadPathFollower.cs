using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Follows the shared leader path stored in AllySquadManager.
/// Used by followers instead of running individual A*.
/// This keeps the squad in the same corridor, with a fallback to directly
/// seek the leader when no valid path is available.
/// </summary>
public class SquadPathFollower : SteeringBehaviour
{
    [Tooltip("How close to a path point before moving to the next one.")]
    public float waypointTolerance = 0.4f;

    [Tooltip("How many points ahead to look for smoothing.")]
    public int lookAheadSteps = 1;

    private int pathIndex = 0;
    private int lastPathVersion = -1;

    public override Vector3 UpdateBehaviour(SteeringAgent agent)
    {
        var squad = AllySquadManager.Instance;

        // If there is no shared path, fall back to simply following the leader.
        if (squad == null || squad.CurrentLeaderPath == null || squad.CurrentLeaderPath.Count == 0)
        {
            var leader = squad != null ? squad.GetLeader() : null;
            if (leader != null && leader.Health > 0f)
            {
                Vector3 diff = leader.transform.position - agent.transform.position;
                diff.z = 0f;

                if (diff.sqrMagnitude > 0.0001f)
                {
                    desiredVelocity = diff.normalized * SteeringAgent.MaxCurrentSpeed;
                    steeringVelocity = desiredVelocity - agent.CurrentVelocity;
                    return steeringVelocity;
                }
            }

            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        List<Vector3> path = squad.CurrentLeaderPath;

        // If the leader path changed, re-sync to the new path.
        if (lastPathVersion != squad.CurrentPathVersion)
        {
            lastPathVersion = squad.CurrentPathVersion;
            pathIndex = FindClosestPathIndex(agent.transform.position, path);
        }

        if (pathIndex < 0 || pathIndex >= path.Count)
        {
            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        Vector3 currentPos = agent.transform.position;

        // Move forward along the path when close enough to the current waypoint.
        Vector3 waypoint = path[pathIndex];
        waypoint.z = currentPos.z;

        Vector3 toWaypoint = waypoint - currentPos;
        toWaypoint.z = 0f;

        if (toWaypoint.magnitude <= waypointTolerance && pathIndex < path.Count - 1)
        {
            pathIndex++;
            waypoint = path[pathIndex];
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

        // Look ahead for smoother motion
        int lookAheadIndex = Mathf.Min(pathIndex + Mathf.Max(lookAheadSteps, 0), path.Count - 1);
        Vector3 target = path[lookAheadIndex];
        target.z = currentPos.z;

        Vector3 diff2 = target - currentPos;
        diff2.z = 0f;

        if (diff2.sqrMagnitude < 0.0001f)
        {
            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        Vector3 dir = diff2.normalized;
        float speed = SteeringAgent.MaxCurrentSpeed;

        desiredVelocity = dir * speed;
        steeringVelocity = desiredVelocity - agent.CurrentVelocity;
        return steeringVelocity;
    }

    private int FindClosestPathIndex(Vector3 position, List<Vector3> path)
    {
        int bestIndex = 0;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 p = path[i];
            p.z = position.z;
            float dSq = (p - position).sqrMagnitude;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}
