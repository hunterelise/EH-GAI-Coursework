using System.Collections.Generic;
using UnityEngine;

public sealed class AttackManager
{
	public int AllyRocketsAvailable { get; private set; }
	public int EnemyRocketsAvailable { get; private set; }

	#region Private interface

	private static readonly Dictionary<string, Sprite> spriteNameToSprite = new Dictionary<string, Sprite>();

	private int attackNumber = 0;
	private GameObject attacksGO;

	public readonly List<Attack> attacks = new List<Attack>();

	private AgentsManager agentManager;


	public void Initialise(AgentsManager agentManager)
	{
		this.agentManager = agentManager;
		AllyRocketsAvailable = GameData.Instance.Map.GetInitialEnemyHouseLocations().Count * 2;
		EnemyRocketsAvailable = AllyRocketsAvailable;

		attacksGO = new GameObject("Attacks");

		foreach (Attack.Data attackData in Attack.AttackDatas)
		{
			if(string.IsNullOrEmpty(attackData.spriteName))
			{
				continue;
			}

			if (!spriteNameToSprite.ContainsKey(attackData.spriteName))
			{
				spriteNameToSprite.Add(attackData.spriteName, Resources.Load<Sprite>(attackData.spriteName));
			}
		}
	}

	// Called once per frame
	public void Tick()
	{
		var gameData = GameData.Instance;
		var map = gameData.Map;

		var allies = gameData.allies;
		var enemies = gameData.enemies;

		for (int attackIndex = attacks.Count - 1; attackIndex >= 0; -- attackIndex)
		{
			bool attackHit = false;

			var attack = attacks[attackIndex];
			attack.currentPosition += attack.Direction * attack.Speed * Time.deltaTime;
			attack.AttackGO.transform.position = attack.currentPosition;

			var agentsToCheck = new List<List<SteeringAgent>>() { attack.IsEnemy ? allies : enemies };
			if (attack.FriendlyFire)
			{
				agentsToCheck.Add((!attack.IsEnemy) ? allies : enemies);
			}
			
			foreach (var agentList in agentsToCheck)
			{
				foreach (var agent in agentList)
				{
					// Skip agents that are dead or attacks that came from the agent that initiated it unless its an explosion
					if (agent.Health <= 0.0f || (agent == attack.AttackerAgent && attack.Type != Attack.AttackType.Explosion))
					{
						continue;
					}

					var radii = attack.Radius + SteeringAgent.CollisionRadius;
					if ((attack.currentPosition - agent.transform.position).sqrMagnitude <= (radii * radii))
					{
						agentManager.ApplyDamageToAgent(agent, attack.Damage);

						if(attack.OneHit)
						{
							attackHit = true;
							break;
						}
					}
				}
			}
			
			if(attackHit)
			{
				if(attack.Type == Attack.AttackType.Rocket)
				{
					CreateExplosion(attack);
				}

				GameObject.Destroy(attack.AttackGO);
				attacks.RemoveAt(attackIndex);
				continue;
			}

			var x = (int)attack.currentPosition.x;
			var y = (int)attack.currentPosition.y;
			if (attackHit == false)
			{
				bool exceededRange = (attack.currentPosition - attack.StartPosition).magnitude >= attack.Range;
				if (exceededRange ||
					(attack.Type != Attack.AttackType.Explosion && (x < 0 || x >= Map.MapWidth || y < 0 || y >= Map.MapHeight || map.IsNavigatable(x, y) == false)))
				{
					// NOTE: Speacial case for explosion so that it appears on non navigatable areas
					attackHit = true;
				}
			}

			if (attackHit)
			{
				if (attack.Type == Attack.AttackType.Rocket && !(x < 0 || x >= Map.MapWidth || y < 0 || y >= Map.MapHeight))
				{
					CreateExplosion(attack);
				}

				GameObject.Destroy(attack.AttackGO);
				attacks.RemoveAt(attackIndex);
				continue;
			}

			// Need to assign Attack struct back to list to update values
			attacks[attackIndex] = attack;
		}

		gameData.attacks.Clear();
		foreach (var attack in attacks)
		{
			gameData.attacks.Add(attack);
		}
	}



	/// <summary>
	/// NEVER CALL THIS!
	/// Anyone calling this function manually will be deducted marks from their coursework!
	/// Creates an atatck from an agent
	/// <param name="attackType">The type of attavk to do</param>
	/// <param name="agent">The agent that initiated the attack</param>
	/// <returns></returns>
	public bool Create(Attack.AttackType attackType, SteeringAgent agent)
	{
		if(agent == null || agent.CanAttack(attackType) == false)
		{
			return false;
		}

		if(!agent.IsAttackInProgress)
		{
			Debug.LogError("DO NOT CALL THIS FUNCTION DIRECTLY - use SteeringAgent.AttackWith() instead");
			return false;
		}

		var attackGO = new GameObject("Attack " + attackNumber.ToString() + " (" + attackType + ") from " + agent.name);
		attackGO.transform.parent = attacksGO.transform;
		attackGO.transform.position = agent.transform.position;
		attackGO.transform.up = agent.transform.up;

		if (attackType == Attack.AttackType.Melee)
		{
			attackGO.transform.position += attackGO.transform.up * (SteeringAgent.CollisionRadius * 2.0f);
		}

		var attack = new Attack(attackType, attackGO, agent);
		attacks.Add(attack);

		var spriteRenderer = attackGO.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = spriteNameToSprite[attack.SpriteName];
		spriteRenderer.color = agent.gameObject.GetComponent<SpriteRenderer>().color;
		spriteRenderer.sortingOrder = 1;

		var collider = attackGO.AddComponent<CircleCollider2D>();
		collider.radius = attack.Radius;

		if (attackType == Attack.AttackType.Rocket)
		{
			if(attack.IsEnemy)
			{
				--EnemyRocketsAvailable;
			}
			else
			{
				--AllyRocketsAvailable;
			}
		}

		++attackNumber;
		return true;
	}

	private bool CreateExplosion(Attack rocket)
	{
		var agent = rocket.AttackerAgent.GetComponent<SteeringAgent>();

		var attackGO = new GameObject("Attack " + attackNumber.ToString() + " (" + rocket.Type + ") from " + agent.name);
		attackGO.transform.parent = attacksGO.transform;
		attackGO.transform.position = rocket.currentPosition;
		attackGO.transform.up = rocket.Direction;

		var attack = new Attack(Attack.AttackType.Explosion, attackGO, agent);
		attacks.Add(attack);

		var spriteRenderer = attackGO.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = spriteNameToSprite[attack.SpriteName];
		spriteRenderer.color = agent.gameObject.GetComponent<SpriteRenderer>().color;

		var collider = attackGO.AddComponent<CircleCollider2D>();
		collider.radius = attack.Radius;

		++attackNumber;
		return true;
	}
	#endregion
}