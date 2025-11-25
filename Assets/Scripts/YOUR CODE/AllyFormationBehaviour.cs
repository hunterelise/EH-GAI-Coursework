using UnityEngine;

// This behaviour makes an ally move toward its assigned spot in the formation
// (or regroup/attack spot depending on the mode).
public sealed class AllyFormationBehaviour : SteeringBehaviour
{
    public enum Mode
    {
        Off,
        Formation,
        Attack,
        Regroup
    }

    public Mode mode = Mode.Formation;

    // The position this ally is supposed to move toward (set somewhere else)
    public Vector3 formationTarget;

    [Tooltip("How close we get before we start slowing down")]
    public float arriveRadius = 0.35f;   // Smaller means everyone groups up more tightly

    [Tooltip("How strongly we try to stay in our assigned spot")]
    public float formationWeight = 1.5f; // Higher means this behaviour is more dominant

    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        // If formation behaviour is turned off, just stay still
        if (mode == Mode.Off)
        {
            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        // Figure out how far we are from the target spot
        Vector3 toTarget = formationTarget - steeringAgent.transform.position;
        float distance = toTarget.magnitude;

        // If we're basically already there, stop moving
        if (distance < 0.001f)
        {
            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        Vector3 dir = toTarget.normalized;
        float speed = SteeringAgent.MaxCurrentSpeed;

        // If we're getting close, slow down smoothly so we don't overshoot
        if (distance < arriveRadius)
        {
            speed *= distance / Mathf.Max(arriveRadius, 0.0001f);
        }

        // The speed and direction we ideally want to move at
        desiredVelocity = dir * speed;

        // Apply a strong push toward the formation target so we stick to our spot
        steeringVelocity = (desiredVelocity - steeringAgent.CurrentVelocity) * formationWeight;
        return steeringVelocity;
    }
}
