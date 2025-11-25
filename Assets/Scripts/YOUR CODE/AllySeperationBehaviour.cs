using UnityEngine;

// This behaviour gently pushes allies apart so they don’t stand on top of each other.
// It’s kept weak so it won’t ruin the overall formation.
public sealed class AllySeparationBehaviour : SteeringBehaviour
{
    [Tooltip("How close another ally can be before we start pushing away")]
    public float separationRadius = 0.9f;   // Slightly smaller than before

    [Tooltip("How strong the push-away force is")]
    public float separationWeight = 0.4f;   // Lower so it doesn't break formations

    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        var allies = GameData.Instance.allies;

        Vector3 force = Vector3.zero;
        int count = 0;
        float radiusSq = separationRadius * separationRadius;

        foreach (var other in allies)
        {
            // Skip invalid allies and ignore ourselves
            if (other == null || other == steeringAgent || other.Health <= 0.0f)
                continue;

            // Direction from the other ally to us
            Vector3 diff = steeringAgent.transform.position - other.transform.position;
            float sqrDist = diff.sqrMagnitude;

            // If they’re close enough, calculate how much we should push away
            if (sqrDist > 0.0001f && sqrDist < radiusSq)
            {
                // The closer we are, the stronger the push
                float strength = (radiusSq - sqrDist) / radiusSq;
                force += diff.normalized * strength;
                count++;
            }
        }

        // If nobody is close, do nothing
        if (count == 0)
        {
            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        // Average out the push force
        force /= count;

        // If the force is basically nothing, stop here
        if (force.sqrMagnitude < 0.0001f)
        {
            desiredVelocity = Vector3.zero;
            steeringVelocity = Vector3.zero;
            return steeringVelocity;
        }

        // Aim to move in the separation direction at max speed
        force = force.normalized * SteeringAgent.MaxCurrentSpeed;
        desiredVelocity = force;

        // Apply a small weight so this only prevents overlap,
        // and doesn’t overpower the main formation movement
        steeringVelocity = (desiredVelocity - steeringAgent.CurrentVelocity) * separationWeight;
        return steeringVelocity;
    }
}
