using UnityEngine;

/// <summary>
/// Provides a soft steering force that moves an ally toward a formation slot
/// or regroup position. This does not override pathfinding or avoidance;
/// it simply nudges the agent toward its assigned place.
/// </summary>
public sealed class AllyFormationBehaviour : SteeringBehaviour
{
    public enum Mode
    {
        Off,        // No formation steering
        Formation,  // Normal formation following
        Regroup     // Return to squad behaviour
    }

    [Tooltip("Current formation mode.")]
    public Mode mode = Mode.Formation;

    [Tooltip("World-space position the ally should move toward.")]
    public Vector3 formationTarget;

    [Tooltip("Distance at which the ally begins slowing down toward its slot.")]
    public float arriveRadius = 0.4f;

    [Tooltip("Inside this distance, formation force fades out so separation can take over.")]
    public float minFormationDistance = 0.8f;

    [Tooltip("Base strength of the formation pull.")]
    public float formationWeight = 1.5f;

    /// <summary>
    /// Returns a steering force that nudges the agent toward its formation slot.
    /// Force fades out when very close, allowing separation to resolve overlaps.
    /// </summary>
    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        // Formation disabled = contribute no steering.
        if (mode == Mode.Off)
        {
            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        Vector3 toTarget = formationTarget - steeringAgent.transform.position;
        float distance = toTarget.magnitude;

        // Already at the slot = no steering needed.
        if (distance < 0.001f)
        {
            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        // Direction we want to move toward the slot.
        Vector3 dir = toTarget.normalized;
        float speed = SteeringAgent.MaxCurrentSpeed;

        // Smooth arrival: when close, reduce speed to avoid jitter and overshoot.
        if (distance < arriveRadius)
        {
            speed *= Mathf.Clamp01(distance / arriveRadius);
        }

        desiredVelocity = dir * speed;

        // Weight determines how strong formation pulling is.
        float weight = formationWeight;

        // Reduce formation strength when very close to the slot.
        // This prevents fighting against separation/avoidance at short range.
        if (distance < minFormationDistance)
        {
            float t = Mathf.Clamp01(distance / minFormationDistance);
            weight *= t;
        }

        // Actual steering force (desired minus current).
        steeringVelocity = (desiredVelocity - steeringAgent.CurrentVelocity) * weight;

        return steeringVelocity;
    }
}
