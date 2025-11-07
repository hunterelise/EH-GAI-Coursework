using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AgentsManager
{
	public float GetAgentHealth(SteeringAgent agent)
	{
		if (agentToHealth.ContainsKey(agent))
		{
			return agentToHealth[agent];
		}
		return -1.0f;
	}

	#region Private interface

	private Dictionary<SteeringAgent, float> agentToHealth = new Dictionary<SteeringAgent, float>();

	private List<SteeringAgent> enemyAgents = new List<SteeringAgent>();
	private List<SteeringAgent> allyAgents = new List<SteeringAgent>();

	private Sprite agentSprite;

	private GameObject enemiesGO;
	private GameObject alliesGO;

	public void Initialise()
	{
		enemiesGO = new GameObject("Enemies");
		alliesGO = new GameObject("Allies");

		agentSprite = Resources.Load<Sprite>("Unit");

		var gameData = GameData.Instance;
		var map = gameData.Map;

		var enemyType = typeof(EnemyAgent);
		var enemyUnitLocations = map.GetInitialEnemyLocations();
		foreach (var enemyLocation in enemyUnitLocations)
		{
			if (TestingVariables.MaxEnemies >= 0 && enemyAgents.Count >= TestingVariables.MaxEnemies)
			{
				break;
			}
			enemyAgents.Add(CreateAgent(map, map.MapIndexToX(enemyLocation), map.MapIndexToY(enemyLocation), true, enemyType));
		}

		if (allyAgents.Count <= 0)
		{
			var allyIndex = 0;
			var allyLocations = map.GetInitialAllyLocations();

			if(AgentCreator.AllySteeringAgentTypes.Length < allyLocations.Count)
			{
				Debug.LogWarning("AgentCreator.AgentTypes[] has less elements than the " + allyLocations.Count + " agents required. AgentsTypes will therefore be cycled over again from the beginning when run out");
			}
			else if(AgentCreator.AllySteeringAgentTypes.Length > allyLocations.Count)
			{
				Debug.LogWarning("AgentCreator.AgentTypes[] has more elements than the " + allyLocations.Count + " agents required. Therefore some AgentsTypes will never be created");
			}

			foreach (var allyLocation in allyLocations)
			{
				if (TestingVariables.MaxAllies >= 0 && allyAgents.Count >= TestingVariables.MaxAllies)
				{
					break;
				}

				allyAgents.Add(CreateAgent(map, map.MapIndexToX(allyLocation), map.MapIndexToY(allyLocation), false, AgentCreator.AllySteeringAgentTypes[allyIndex]));
				++allyIndex;

				// Cycle over back from the start index if this is reached
				if(allyIndex >= AgentCreator.AllySteeringAgentTypes.Length)
				{
					allyIndex = 0;
				}
			}
		}

		CopyAgentsToLists();
	}

	public void Tick()
	{
		CopyAgentsToLists();
	}

	private void CopyAgentsToLists()
	{
		var gameData = GameData.Instance;
		gameData.allies.Clear();
		foreach (var agent in allyAgents)
		{
			gameData.allies.Add(agent);
		}
		gameData.enemies.Clear();
		foreach (var agent in enemyAgents)
		{
			gameData.enemies.Add(agent);
		}
	}


	private SteeringAgent CreateAgent(Map map, int mapX, int mapY, bool isEnemy, Type type)
	{
		if (!typeof(SteeringAgent).IsAssignableFrom(type))
		{
			throw new TypeAccessException("Type " + type.Name + " does not derive from " + typeof(SteeringAgent).Name);
		}

		var agentGO = new GameObject(isEnemy ? "Enemy " + enemyAgents.Count : "Ally " + allyAgents.Count);
		agentGO.transform.parent = isEnemy ? enemiesGO.transform : alliesGO.transform;
		agentGO.transform.position = new Vector3(mapX, mapY, 0.0f);

		var spriteRenderer = agentGO.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = agentSprite;
		spriteRenderer.color = isEnemy ? TestingVariables.ColourEnemy : TestingVariables.ColourAlly;

		var collider = agentGO.AddComponent<CircleCollider2D>();
		collider.radius = SteeringAgent.CollisionRadius;

		// Ensure this is last as the users entry point into these classes will be called when this happens
		// so everything needs setup before this
		var agent = agentGO.AddComponent(type) as SteeringAgent;
		agentToHealth.Add(agent, 1.0f);
		return agent;
	}

	/// <summary>
	/// NEVER CALL THIS!
	/// Anyone calling this function manually will be deducted marks from their coursework!
	/// Applies damage to the agent
	/// </summary>
	/// <param name="agent">Agent to apply damage to</param>
	/// <param name="damage">Amount of damage to apply</param>
	public void ApplyDamageToAgent(SteeringAgent agent, float damage)
	{
		if (agentToHealth.ContainsKey(agent))
		{
			agentToHealth[agent] -= damage;

			if (agentToHealth[agent] <= 0.0f)
			{
				agentToHealth.Remove(agent);
			}
		}
	}
	#endregion
}
