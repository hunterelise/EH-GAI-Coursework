using UnityEngine;
public class EnemyDetector : SteeringBehaviour // detects the nearest enemy within sight range
{
    private float sightRadius = 15f; // default sight range
    private SteeringAgent targetAgent;

    public SteeringAgent TargetAgent => targetAgent; // nearest enemy

    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        // allies can see farther
        if (GetComponent<AllyAgent>() != null)
            sightRadius = 20f;

        UpdateTarget(); // find nearest enemy

        // this behaviour doesn't move the agent
        desiredVelocity = Vector3.zero;
        steeringVelocity = Vector3.zero;
        return Vector3.zero;
    }

    private void UpdateTarget()
    {
        SteeringAgent best = null;
        float bestDist = float.MaxValue;

        // find closest alive enemy within sight
        foreach (var enemy in GameData.Instance.enemies)
        {
            if (enemy == null || enemy.Health <= 0)
                continue;

            float d = Vector3.Distance(transform.position, enemy.transform.position);

            if (d < sightRadius && d < bestDist)
            {
                bestDist = d;
                best = enemy;
            }
        }

        targetAgent = best; // update current target
    }
}
