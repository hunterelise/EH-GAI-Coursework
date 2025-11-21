using UnityEngine;

public class ObstacleAvoidance : SteeringBehaviour
{
    public float avoidDistance = 1.5f; // distance to keep from obstacles
    public float lookAhead = 1.2f;     // how far ahead to check

    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        Vector3 velocity = steeringAgent.CurrentVelocity;

        // if not moving, no need to avoid
        if (velocity.sqrMagnitude < 0.01f)
            return Vector3.zero;

        Vector3 forward = velocity.normalized;
        Vector3 ahead = transform.position + forward * lookAhead;

        // check center, left, and right for obstacles
        if (!IsWalkable(ahead))
            return SteerAway(forward, steeringAgent);

        Vector3 left = transform.position + Quaternion.Euler(0, 0, 30f) * forward * lookAhead;
        if (!IsWalkable(left))
            return SteerAway(Quaternion.Euler(0, 0, 30f) * forward, steeringAgent);

        Vector3 right = transform.position + Quaternion.Euler(0, 0, -30f) * forward * lookAhead;
        if (!IsWalkable(right))
            return SteerAway(Quaternion.Euler(0, 0, -30f) * forward, steeringAgent);

        return Vector3.zero; // no obstacles detected
    }

    private bool IsWalkable(Vector3 pos)
    {
        return GameData.Instance.Map.IsNavigatable((int)pos.x, (int)pos.y); // check map
    }

    private Vector3 SteerAway(Vector3 dir, SteeringAgent agent)
    {
        desiredVelocity = -dir.normalized * SteeringAgent.MaxCurrentSpeed; // reverse direction
        return desiredVelocity - agent.CurrentVelocity; // steering force
    }
}
