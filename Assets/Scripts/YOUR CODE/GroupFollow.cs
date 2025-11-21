using UnityEngine;

public class GroupFollow : SteeringBehaviour
{
    public float followDistance = 3f; // distance to stay behind leader

    private GroupLeader leader;

    private void Start()
    {
        leader = FindObjectOfType<GroupLeader>(); // find the leader
    }

    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        if (leader == null)
            return Vector3.zero; // no leader, no movement

        // target position behind the leader
        Vector3 desiredPos = leader.transform.position +
                             (-leader.transform.up * followDistance);

        Vector3 toDest = desiredPos - transform.position;

        // desired velocity toward target position
        desiredVelocity = toDest.normalized * SteeringAgent.MaxCurrentSpeed;

        // steering force to move there
        return desiredVelocity - steeringAgent.CurrentVelocity;
    }
}
