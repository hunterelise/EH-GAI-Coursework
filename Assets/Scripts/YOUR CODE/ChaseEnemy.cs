using UnityEngine;
public class ChaseEnemy : SteeringBehaviour // moves agent toward the detected enemy
{
    private EnemyDetector enemyDetector;

    private void Awake()
    {
        enemyDetector = GetComponent<EnemyDetector>(); // get reference to enemy detection
    }

    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        if (enemyDetector == null)
            return Vector3.zero;

        var target = enemyDetector.TargetAgent;
        if (target == null)
            return Vector3.zero; // no enemy, do nothing

        // direction to target
        Vector3 toTarget = target.transform.position - transform.position;

        // calculate velocity toward enemy
        desiredVelocity = toTarget.normalized * SteeringAgent.MaxCurrentSpeed;
        steeringVelocity = desiredVelocity - steeringAgent.CurrentVelocity;

        return steeringVelocity; // steering force
    }
}
