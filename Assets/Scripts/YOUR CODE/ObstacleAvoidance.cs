using UnityEngine;

public sealed class ObstacleAvoidance : SteeringBehaviour
{
    public float lookAheadDistance = 1.2f;
    public float sideStepDistance = 1.0f;
    public float avoidanceWeight = 1.0f;

    public override Vector3 UpdateBehaviour(SteeringAgent agent)
    {
        var map = GameData.Instance.Map;

        Vector3 pos = agent.transform.position;

        // Figure out which way the agent is currently moving.
        // If it's barely moving, just use its facing direction.
        Vector3 forward = agent.CurrentVelocity.sqrMagnitude > 0.001f
            ? agent.CurrentVelocity.normalized
            : agent.transform.up;

        // Look ahead to see if we're about to hit something
        Vector3 ahead = pos + forward * lookAheadDistance;

        int x = Mathf.FloorToInt(ahead.x);
        int y = Mathf.FloorToInt(ahead.y);

        // If the cell ahead is safe, we don't need to avoid anything
        if (map.IsNavigatable(x, y))
        {
            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        // Figure out right and left directions relative to our forward direction
        Vector3 right = new Vector3(forward.y, -forward.x, 0f);
        Vector3 left = -right;

        // Check slightly to the right and left to see which one is open
        Vector3 rightCheck = pos + forward * (lookAheadDistance * 0.5f) + right * sideStepDistance;
        Vector3 leftCheck = pos + forward * (lookAheadDistance * 0.5f) + left * sideStepDistance;

        bool rightBlocked = !map.IsNavigatable(Mathf.FloorToInt(rightCheck.x), Mathf.FloorToInt(rightCheck.y));
        bool leftBlocked = !map.IsNavigatable(Mathf.FloorToInt(leftCheck.x), Mathf.FloorToInt(leftCheck.y));

        // Pick the best side to dodge toward.
        // If only one side is open, go that way.
        // If both are open or both blocked, default to right.
        Vector3 avoidance =
            (!rightBlocked && leftBlocked) ? right :
            (rightBlocked && !leftBlocked) ? left :
            right; // fallback

        // Move quickly in the chosen avoidance direction
        desiredVelocity = avoidance.normalized * SteeringAgent.MaxCurrentSpeed;

        // Apply avoidance force with a multiplier to control how strong it is
        steeringVelocity = (desiredVelocity - agent.CurrentVelocity) * avoidanceWeight;
        return steeringVelocity;
    }
}
