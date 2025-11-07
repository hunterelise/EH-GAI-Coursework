using UnityEngine;

public sealed class EnemyAgent : SteeringAgent
{
	private EnemyFleeBehaviour fleeBehaviour;
	private EnemyFleeRocketBehaviour fleeRocketBehaviour;
	private EnemySeekBehaviour seekBehaviour;

	private bool rocketIncoming = false;

	public SteeringAgent nearestAlly;
	public Vector3 rocketPosition;
	public Vector3 startPosition;

	protected override void InitialiseFromAwake()
	{
		fleeBehaviour = gameObject.AddComponent<EnemyFleeBehaviour>();
		fleeRocketBehaviour = gameObject.AddComponent<EnemyFleeRocketBehaviour>();
		seekBehaviour = gameObject.AddComponent<EnemySeekBehaviour>();

		fleeBehaviour.enabled = false;
		fleeRocketBehaviour.enabled = false;
	}

	protected override void InitialiseFromStart()
	{
		startPosition = transform.position;
	}

	protected override void CooperativeArbitration()
	{
		nearestAlly = GetNearestAgent(transform.position, GameData.Instance.allies);

		// Determine if there is a rocket attack incoming
		bool wasRocketIncoming = rocketIncoming;
		rocketIncoming = false;
		var attacks = GameData.Instance.attacks;
		foreach (var attack in attacks)
		{
			if (!attack.IsEnemy && attack.Type == Attack.AttackType.Rocket)
			{
				if ((transform.position - attack.currentPosition).magnitude < 25.0f)
				{
					rocketIncoming = true;
					rocketPosition = attack.currentPosition;
					break;
				}
			}
		}

		// On the frame update when an incoming rocket has been detected or vanished enable correct behaviours so enemy goes/returns to doing correct actions
		if (wasRocketIncoming != rocketIncoming)
		{
			if (rocketIncoming && Random.value <= 0.5f)
			{
				fleeBehaviour.enabled = false;
				seekBehaviour.enabled = false;
				fleeRocketBehaviour.enabled = true;
			}

			if (rocketIncoming == false && fleeRocketBehaviour.enabled)
			{
				fleeBehaviour.enabled = false;
				seekBehaviour.enabled = true;
				fleeRocketBehaviour.enabled = false;
			}
		}

		// If low health flee (there can be double amount of flee if the rocket one is active)
		if (Health <= 0.25f)
		{
			fleeBehaviour.enabled = true;
		}

		base.CooperativeArbitration();

		// Attack
		if (TimeToNextAttack <= 0 && nearestAlly != null)
		{
			var distanceToAlly = (nearestAlly.transform.position - transform.position).magnitude;

			if (GameData.Instance.EnemyRocketsAvailable > 0 &&
				distanceToAlly < 27.0f &&
				Random.value <= 0.0001f)
			{
				AttackWith(Attack.AttackType.Rocket);
			}
			else
			{
				if (distanceToAlly <= 15.0f)
				{
					AttackWith(Attack.AttackType.EnemyGun);
				}
			}
		}
	}

	protected override void UpdateDirection()
	{
		base.UpdateDirection();

		if(nearestAlly == null)
		{
			return;
		}

		var difference = nearestAlly.transform.position - transform.position;
		if (nearestAlly != null && (difference).magnitude < 30.0f)
		{
			transform.up = Vector3.Normalize(new Vector3(difference.x, difference.y, 0.0f));
		}
	}
}
