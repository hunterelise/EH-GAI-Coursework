using UnityEngine;

public class Wander : SteeringBehaviour
{
    public float wanderRadius = 3f;    // radius of wandering circle
    public float wanderDistance = 5f;  // distance ahead of agent
    public float wanderJitter = 1f;    // randomness each frame

    private Vector3 wanderTarget;

    private void Start()
    {
        // initialise random wander direction
        wanderTarget = Random.insideUnitSphere;
        wanderTarget.z = 0;
    }

    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        // add small random displacement
        wanderTarget += new Vector3(
            Random.Range(-1f, 1f) * wanderJitter,
            Random.Range(-1f, 1f) * wanderJitter,
            0);

        // project onto wander circle
        wanderTarget = wanderTarget.normalized * wanderRadius;

        // calculate target in world space
        Vector3 targetWorld = transform.position
                            + transform.up * wanderDistance
                            + wanderTarget;

        // desired velocity toward target
        desiredVelocity = (targetWorld - transform.position).normalized
                        * SteeringAgent.MaxCurrentSpeed;

        // steering force
        steeringVelocity = desiredVelocity - steeringAgent.CurrentVelocity;

        return steeringVelocity;
    }
}
