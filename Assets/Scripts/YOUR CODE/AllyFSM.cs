using UnityEngine;

/// <summary>
/// Finite state machine for an ally.
/// - Chooses state (Idle, FollowLeader, Attack, Regroup).
/// - Picks a target.
/// - Selects an attack type, with rockets reserved for large nearby groups.
/// </summary>
[RequireComponent(typeof(AllyAgent))]
public class AllyFSM : MonoBehaviour
{
    public enum AllyRole { Leader, Flanker, Support }
    public enum AllyState { Idle, FollowLeader, Attack, Regroup }

    [Header("Debug (FSM state)")]
    public AllyRole Role = AllyRole.Support;
    public AllyState State = AllyState.FollowLeader;

    /// <summary>
    /// Target this ally will act against (movement + attacking).
    /// </summary>
    public SteeringAgent CurrentTarget { get; private set; }

    /// <summary>
    /// Attack type this ally prefers right now (chosen by the FSM).
    /// </summary>
    public Attack.AttackType CurrentAttackType { get; private set; } = Attack.AttackType.None;

    // Rocket / threat tuning
    private const float ROCKET_CLUSTER_RADIUS = 4.5f; // Radius around target to count a cluster for rockets.
    private const float LOCAL_THREAT_RADIUS = 6.0f;   // Radius around self to measure how surrounded we are.
    private const int ROCKET_MIN_CLUSTER = 4;         // Minimum enemies near target before rockets are considered.
    private const int ROCKET_LOW_AMMO = 2;            // Rockets considered "low" at or below this value.

    /// <summary>
    /// Main decision function called once per update by AllyAgent.
    /// Decides:
    /// - State (Attack vs FollowLeader vs Regroup).
    /// - Target.
    /// - Preferred attack type.
    /// </summary>
    public void Tick(AllyAgent agent)
    {
        // Regroup state: move back into formation first, no active targeting.
        if (State == AllyState.Regroup)
        {
            var squad = AllySquadManager.Instance;
            if (squad != null)
            {
                Vector3 slot = squad.GetFormationPositionFor(agent);
                float dist = (agent.transform.position - slot).magnitude;

                // Once close to our formation slot, return to normal following behaviour.
                if (dist < 0.6f)
                {
                    State = AllyState.FollowLeader;
                }
            }

            CurrentTarget = null;
            CurrentAttackType = Attack.AttackType.None;
            return;
        }

        // Find the closest living enemy to this agent.
        var enemies = GameData.Instance.enemies;
        CurrentTarget = SteeringAgent.GetNearestAgent(agent.transform.position, enemies);

        // No valid target then just follow the leader.
        if (CurrentTarget == null || CurrentTarget.Health <= 0.0f)
        {
            State = AllyState.FollowLeader;
            CurrentAttackType = Attack.AttackType.None;
            return;
        }

        Vector3 toTarget = CurrentTarget.transform.position - agent.transform.position;
        float distanceToTarget = toTarget.magnitude;

        // Enemies around the target (used for rocket "cluster" decisions).
        int enemiesNearTarget = CountEnemiesNear(CurrentTarget.transform.position, ROCKET_CLUSTER_RADIUS);

        // Enemies around this ally (used for deciding how safe we are).
        int enemiesNearSelf = CountEnemiesNear(agent.transform.position, LOCAL_THREAT_RADIUS);

        // Choose an attack type based on distance, enemy counts, and role.
        CurrentAttackType = ChooseAttackType(
            distanceToTarget,
            enemiesNearTarget,
            enemiesNearSelf,
            Role
        );

        float effectiveRange = GetRangeFor(CurrentAttackType);

        // If we are in range for the chosen weapon, switch to Attack.
        // Otherwise keep moving / following.
        if (distanceToTarget <= effectiveRange * 0.9f)
        {
            State = AllyState.Attack;
        }
        else
        {
            State = AllyState.FollowLeader;
        }
    }

    /// <summary>
    /// Counts how many enemies are within a given radius of a position.
    /// Used for both rocket cluster checks and local threat checks.
    /// </summary>
    private int CountEnemiesNear(Vector3 position, float radius)
    {
        float radiusSq = radius * radius;
        int count = 0;

        foreach (var enemy in GameData.Instance.enemies)
        {
            if (enemy == null || enemy.Health <= 0.0f)
                continue;

            float dSq = (enemy.transform.position - position).sqrMagnitude;
            if (dSq <= radiusSq)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Chooses an attack type based on:
    /// - Distance to the target.
    /// - How many enemies are near the target (cluster size).
    /// - How many enemies are near this ally (local danger).
    /// - This ally's role (Leader / Flanker / Support).
    /// Rockets are only allowed for decent-sized clusters and sensible ranges.
    /// </summary>
    private Attack.AttackType ChooseAttackType(
        float distToTarget,
        int enemiesNearTarget,
        int enemiesNearSelf,
        AllyRole role)
    {
        float meleeRange = Attack.MeleeData.range;
        float rocketRange = Attack.RocketData.range;
        float gunRange = Attack.AllyGunData.range;

        int rocketsLeft = GameData.Instance.AllyRocketsAvailable;
        bool hasRockets = rocketsLeft > 0;
        bool lowAmmo = rocketsLeft <= ROCKET_LOW_AMMO;

        // Conditions under which rockets are allowed.
        bool bigCluster = enemiesNearTarget >= ROCKET_MIN_CLUSTER;
        bool veryBigCluster = enemiesNearTarget >= ROCKET_MIN_CLUSTER + 2; // Extra large group.
        bool notPointBlank = distToTarget >= meleeRange * 1.8f;           // Avoid rockets at melee range.
        bool notAcrossMap = distToTarget <= rocketRange * 0.75f;          // Avoid long-range rocket sniping.
        bool goodRocketBand = notPointBlank && notAcrossMap;

        bool shouldConsiderRockets =
            hasRockets &&
            bigCluster &&
            goodRocketBand &&
            (!lowAmmo || veryBigCluster); // If ammo is low, only spend it on very large clusters.

        // 1) Rockets: only if strict conditions are met, and behaviour depends on role.
        if (shouldConsiderRockets)
        {
            switch (role)
            {
                case AllyRole.Support:
                    // Supports are primary rocket users.
                    return Attack.AttackType.Rocket;

                case AllyRole.Leader:
                    // Leader avoids rockets when personally surrounded.
                    if (enemiesNearSelf <= 2)
                        return Attack.AttackType.Rocket;
                    break;

                case AllyRole.Flanker:
                    // Flankers are more melee / gun focused. Only rocket if far enough and not threatened.
                    if (distToTarget > meleeRange * 2.5f && enemiesNearSelf == 0)
                        return Attack.AttackType.Rocket;
                    break;
            }
        }

        // 2) Melee: used at close range, but not into huge crowds (those are better for guns/rockets).
        if (distToTarget <= meleeRange * 1.1f)
        {
            // Flankers prefer melee when the fight is small/medium.
            if (role == AllyRole.Flanker && enemiesNearTarget <= 3)
                return Attack.AttackType.Melee;

            // Leader and Support melee only when the group is small.
            if (enemiesNearTarget <= 2)
                return Attack.AttackType.Melee;
        }

        // 3) Gun: default choice inside gun range.
        if (distToTarget <= gunRange)
            return Attack.AttackType.AllyGun;

        // 4) Out of gun range:
        // We do not fire rockets here anymore; instead we move closer and use gun.
        return Attack.AttackType.AllyGun;
    }

    /// <summary>
    /// Returns the effective attack range for each attack type.
    /// Used for determining whether we should be in Attack or FollowLeader.
    /// </summary>
    private float GetRangeFor(Attack.AttackType type)
    {
        switch (type)
        {
            case Attack.AttackType.Melee:
                return Attack.MeleeData.range;
            case Attack.AttackType.Rocket:
                return Attack.RocketData.range;
            case Attack.AttackType.AllyGun:
            default:
                return Attack.AllyGunData.range;
        }
    }
}
