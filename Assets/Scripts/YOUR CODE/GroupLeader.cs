using UnityEngine;
using System.Linq;

public class GroupLeader : SteeringBehaviour
{
    public float enemySearchDelay = 1.0f; // delay between enemy searches
    private float searchTimer = 0f;

    private Vector3 moveTarget; // current move target
    public Vector3 CurrentTarget => moveTarget;

    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        searchTimer -= Time.deltaTime;

        // EnemyDetector takes priority
        var sight = GetComponent<EnemyDetector>();
        if (sight != null && sight.TargetAgent != null)
        {
            moveTarget = sight.TargetAgent.transform.position; // move to visible enemy
        }
        else
        {
            // periodically choose nearest enemy cluster
            if (searchTimer <= 0f)
            {
                searchTimer = enemySearchDelay;
                ChooseEnemyCluster();
            }
        }

        // move toward current target
        desiredVelocity = (moveTarget - transform.position).normalized *
                          SteeringAgent.MaxCurrentSpeed;

        return desiredVelocity - steeringAgent.CurrentVelocity; // steering force
    }

    private void ChooseEnemyCluster()
    {
        var enemies = GameData.Instance.enemies;

        if (enemies == null || enemies.Count == 0)
        {
            moveTarget = transform.position; // no enemies, hold position
            return;
        }

        // find nearest alive enemy
        var nearest = enemies
            .Where(e => e.Health > 0)
            .OrderBy(e => (e.transform.position - transform.position).sqrMagnitude)
            .FirstOrDefault();

        if (nearest != null)
            moveTarget = nearest.transform.position; // set as target
    }
}
