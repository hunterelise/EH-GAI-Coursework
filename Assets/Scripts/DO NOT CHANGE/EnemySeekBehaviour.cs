using UnityEngine;

public sealed class EnemySeekBehaviour : SteeringBehaviour
{
	public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
	{
		var enemySteeringAgent = steeringAgent as EnemyAgent;

		Vector3 targetPosition = enemySteeringAgent.startPosition;

		var nearestAlly = SteeringAgent.GetNearestAgent(transform.position, GameData.Instance.allies);
		if (nearestAlly != null &&
			(targetPosition - transform.position).magnitude < 10.0f &&
			(nearestAlly.transform.position - transform.position).magnitude <= Attack.AllyGunData.range)
		{
			targetPosition = nearestAlly.transform.position;
		}

		desiredVelocity = Vector3.Normalize(targetPosition - transform.position) * SteeringAgent.MaxCurrentSpeed;
		steeringVelocity = desiredVelocity - steeringAgent.CurrentVelocity;
		return steeringVelocity;
	}
}
