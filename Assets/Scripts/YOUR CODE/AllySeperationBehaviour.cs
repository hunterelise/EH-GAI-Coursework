using UnityEngine;

/// <summary>
/// Steering behaviour that prevents allies from overlapping.
/// Produces a repelling force when other allies get too close.
/// </summary>
public sealed class AllySeparationBehaviour : SteeringBehaviour
{
    [Tooltip("Distance within which other allies start pushing us away.")]
    public float separationRadius = 1.2f;

    [Tooltip("Strength multiplier applied to the separation force.")]
    public float separationWeight = 1.0f;

    /// <summary>
    /// Computes the steering force needed to push this ally away
    /// from nearby allies inside the separation radius.
    ///
    /// This does not move the agent directly; it only returns a steering
    /// velocity that is blended with other steering behaviours inside the agent.
    /// </summary>
    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        var allies = GameData.Instance.allies;

        Vector3 force = Vector3.zero;
        int count = 0;
        float radiusSq = separationRadius * separationRadius;

        // Check all allies and find which ones are too close.
        foreach (var other in allies)
        {
            // Skip invalid or dead allies, and skip ourselves.
            if (other == null || other == steeringAgent || other.Health <= 0.0f)
                continue;

            Vector3 diff = steeringAgent.transform.position - other.transform.position;
            float sqrDist = diff.sqrMagnitude;

            // Only react to allies inside the separation radius.
            if (sqrDist > 0.0001f && sqrDist < radiusSq)
            {
                float dist = Mathf.Sqrt(sqrDist);

                // Strength becomes stronger as the other ally is closer.
                float strength = 1f - (dist / separationRadius);

                // Add a repelling direction away from the other ally.
                force += diff.normalized * strength;
                count++;
            }
        }

        // No allies close enough to require separation.
        if (count == 0)
        {
            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        // Average the repelling force from all nearby allies.
        force /= count;

        if (force.sqrMagnitude < 0.0001f)
        {
            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        // Desired movement is away from nearby allies at full speed.
        desiredVelocity = force.normalized * SteeringAgent.MaxCurrentSpeed;

        // Steering velocity is scaled by separationWeight.
        steeringVelocity = (desiredVelocity - steeringAgent.CurrentVelocity) * separationWeight;

        return steeringVelocity;
    }
}
