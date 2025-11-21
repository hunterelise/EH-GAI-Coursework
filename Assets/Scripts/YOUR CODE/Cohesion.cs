using UnityEngine;
using System.Linq;

public class Cohesion : SteeringBehaviour
{
    public float cohesionStrength = 3.0f; // how strongly agent moves toward group
    public float groupRadius = 6.0f;      // distance to start cohesion

    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        // get all other allies
        var allies = GameData.Instance.allies
            .Where(a => a != null && a != steeringAgent)
            .ToList();

        if (allies.Count == 0)
            return Vector3.zero;

        // compute center of group
        Vector3 center = Vector3.zero;
        foreach (var ally in allies)
            center += ally.transform.position;
        center /= allies.Count;

        // vector toward center
        Vector3 toCenter = center - transform.position;

        // only move if outside group radius
        float dist = toCenter.magnitude;
        if (dist < groupRadius)
            return Vector3.zero;

        // calculate desired velocity and steering force
        desiredVelocity = toCenter.normalized * SteeringAgent.MaxCurrentSpeed;
        return (desiredVelocity - steeringAgent.CurrentVelocity) * cohesionStrength;
    }
}
