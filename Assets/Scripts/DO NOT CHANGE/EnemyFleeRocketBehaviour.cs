using UnityEngine;

public sealed class EnemyFleeRocketBehaviour : SteeringBehaviour
{
	public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
	{
		var enemySteeringAgent = steeringAgent as EnemyAgent;

		Vector3 targetPosition = enemySteeringAgent.rocketPosition;
		desiredVelocity = Vector3.Normalize(transform.position - targetPosition) * SteeringAgent.MaxCurrentSpeed;
		steeringVelocity = desiredVelocity - steeringAgent.CurrentVelocity;
		return steeringVelocity;
	}
}
