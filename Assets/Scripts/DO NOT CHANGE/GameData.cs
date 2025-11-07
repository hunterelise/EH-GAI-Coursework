using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    public Map Map { get; private set; }

    public readonly List<SteeringAgent> enemies = new List<SteeringAgent>();
	public readonly List<SteeringAgent> allies = new List<SteeringAgent>();
	public readonly List<Attack> attacks = new List<Attack>();

	public int AllyRocketsAvailable => attackManager.AllyRocketsAvailable;
	public int EnemyRocketsAvailable => attackManager.EnemyRocketsAvailable;

	/// <summary>
	/// Retuns health of agent
	/// </summary>
	/// <param name="agent"></param>
	/// <returns>Health of agent. Value of <= 0 means agent is dead</returns>
	public float GetAgentHealth(SteeringAgent agent)
	{
		return agentsManager.GetAgentHealth(agent);
	}

	#region Private interface
	private AgentsManager agentsManager;
	private AttackManager attackManager;

	/// <summary>
	/// NEVER CALL THIS!
	/// Anyone calling this function manually will be deducted marks from their coursework!
	/// Creates an attack
	/// </summary>
	/// <param name="attack">Type of attack</param>
	/// <param name="agent">Agent that initiated the attack</param>
	/// <returns></returns>
	public bool CreateAttack(Attack.AttackType attack, SteeringAgent agent)
	{
		return attackManager.Create(attack, agent);
	}

	// Start is called before the first frame update
	private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Another GameData instance has been created! This is a Singleton game object there can only be one instance!");
            DestroyImmediate(this);
            return;
        }

        Map = new Map();

		agentsManager = new AgentsManager();
		agentsManager.Initialise();

		attackManager = new AttackManager();
        attackManager.Initialise(agentsManager);

		var mapRenderer = gameObject.AddComponent<MapRenderer>();
		mapRenderer.Initialise(Map.GetMapData(), Map.MapWidth, Map.MapHeight);
	}

	private void Update()
	{
		attackManager.Tick();
		agentsManager.Tick();
	}
	#endregion
}
