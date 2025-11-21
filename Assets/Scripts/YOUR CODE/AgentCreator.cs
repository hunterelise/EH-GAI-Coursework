using System;

public class AgentCreator
{
	/// <summary>
	/// If you want to extend from SteeringAgent and add custom functionality then provide your agent types that inherits from SteeringAgent in the array to do so.
	/// Agents are created in this order and they will correspond to Ally agents order in Map.GetInitialAllyLocations()
	/// </summary>
	static public readonly Type[] AllySteeringAgentTypes = new Type[]
    {
		typeof(AllyAgent),
		typeof(AllyAgent),
		typeof(AllyAgent),
		typeof(AllyAgent),
		typeof(AllyAgent)
	};
}
