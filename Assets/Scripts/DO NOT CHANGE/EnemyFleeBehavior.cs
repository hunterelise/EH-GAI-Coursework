using UnityEngine;

public sealed class EnemyFleeBehaviour : SteeringBehaviour
{
	public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
	{
		var enemySteeringAgent = steeringAgent as EnemyAgent;

		Vector3 targetPosition = transform.position;

		var nearestUnit = enemySteeringAgent.nearestAlly;
		if (nearestUnit != null && (nearestUnit.transform.position - transform.position).magnitude <= 25.0f)
		{
			targetPosition = nearestUnit.transform.position;
		}

		desiredVelocity = Vector3.Normalize(transform.position - targetPosition) * SteeringAgent.MaxCurrentSpeed;
		steeringVelocity = desiredVelocity - steeringAgent.CurrentVelocity;
		return steeringVelocity;
	}
}
