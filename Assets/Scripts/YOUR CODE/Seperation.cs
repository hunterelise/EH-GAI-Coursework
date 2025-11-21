using UnityEngine;
using System.Linq;

public class Separation : SteeringBehaviour
{
    public float desiredSeparation = 1.5f;  // minimum distance to other allies
    public float separationStrength = 5f;   // how strongly agent moves away

    public override Vector3 UpdateBehaviour(SteeringAgent agent)
    {
        Vector3 force = Vector3.zero;
        int count = 0;

        // loop through all other allies
        foreach (var ally in GameData.Instance.allies)
        {
            if (ally == null || ally == agent)
                continue;

            float dist = Vector3.Distance(agent.transform.position, ally.transform.position);

            if (dist < desiredSeparation)
            {
                // push away from close ally
                Vector3 push = (agent.transform.position - ally.transform.position).normalized;
                push /= Mathf.Max(dist, 0.01f); // stronger push if closer
                force += push;
                count++;
            }
        }

        if (count > 0)
            force /= count; // average the forces

        return force * separationStrength; // apply strength
    }
}
