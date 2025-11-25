using UnityEngine;

/// <summary>
/// This is the ally’s “brain.”
/// It chooses what state the ally should be in,
/// Who the ally should target,
/// And which attack type the ally prefers.
/// </summary>
[RequireComponent(typeof(AllyAgent))]
public class AllyFSM : MonoBehaviour
{
    public enum AllyRole { Leader, Flanker, Support }
    public enum AllyState { Idle, FollowLeader, Attack, Regroup }

    [Header("Debug (FSM state)")]
    public AllyRole Role = AllyRole.Support;
    public AllyState State = AllyState.FollowLeader;

    // These are the decisions this brain gives to AllyAgent
    public SteeringAgent CurrentTarget { get; private set; }
    public Attack.AttackType CurrentAttackType { get; private set; } = Attack.AttackType.None;

    /// <summary>
    /// Main decision-making function.
    /// Called every update by AllyAgent.
    /// </summary>
    public void Tick(AllyAgent agent)
    {
        // If we're regrouping, stay in this state until we reach the regroup point
        if (State == AllyState.Regroup)
        {
            var squad = AllySquadManager.Instance;
            if (squad != null)
            {
                float dist = (agent.transform.position - squad.RegroupPoint).magnitude;

                // Close enough — go back to following the leader
                if (dist < 2.0f)
                {
                    State = AllyState.FollowLeader;
                }
            }

            // No target while regrouping
            CurrentTarget = null;
            CurrentAttackType = Attack.AttackType.None;
            return;
        }

        // Try to pick the closest living enemy
        var enemies = GameData.Instance.enemies;
        CurrentTarget = SteeringAgent.GetNearestAgent(agent.transform.position, enemies);

        // If no target is found, just follow the leader
        if (CurrentTarget == null || CurrentTarget.Health <= 0.0f)
        {
            State = AllyState.FollowLeader;
            CurrentAttackType = Attack.AttackType.None;
            return;
        }

        Vector3 diff = CurrentTarget.transform.position - agent.transform.position;
        float distance = diff.magnitude;

        // Count how many enemies are crowding around this ally
        int nearbyEnemies = CountEnemiesNear(agent.transform.position, 10.0f);

        // Pick which attack we ideally want to use
        CurrentAttackType = ChooseAttackType(distance, nearbyEnemies);

        // If we're close enough for the weapon, attack.
        // Otherwise, move closer / follow the leader.
        float effectiveRange = GetRangeFor(CurrentAttackType);
        if (distance <= effectiveRange * 0.9f)
        {
            State = AllyState.Attack;
        }
        else
        {
            State = AllyState.FollowLeader;
        }
    }

    /// <summary>
    /// Counts how many enemies are within a certain radius.
    /// More enemies = ally acts more aggressively.
    /// </summary>
    private int CountEnemiesNear(Vector3 position, float radius)
    {
        float radiusSq = radius * radius;
        int count = 0;

        foreach (var enemy in GameData.Instance.enemies)
        {
            if (enemy == null || enemy.Health <= 0.0f) continue;

            float dSq = (enemy.transform.position - position).sqrMagnitude;
            if (dSq <= radiusSq)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Decides which type of attack the ally prefers right now,
    /// based on how far the enemy is and how many enemies are nearby.
    /// </summary>
    private Attack.AttackType ChooseAttackType(float distance, int nearbyEnemies)
    {
        // Base scores
        float meleeScore = 0f;
        float rocketScore = 0f;
        float gunScore = 0.1f; // gun is always "okay"

        // Melee
        float meleeRange = Attack.MeleeData.range;
        if (distance <= meleeRange * 1.5f)    // a bit more generous than before
        {
            meleeScore = 1.0f;                // strong baseline for close distance

            // Extra reward if we are really close
            if (distance <= meleeRange * 0.8f)
                meleeScore += 0.5f;

            // Extra reward if there are many enemies nearby
            if (nearbyEnemies >= 3)
                meleeScore += 0.5f;
        }

        // Rocket
        float rocketRange = Attack.RocketData.range;

        // Allow rockets from a bit closer and up to full range
        if (GameData.Instance.AllyRocketsAvailable > 0 &&
            distance >= 4.0f &&
            distance <= rocketRange)
        {
            rocketScore = 0.8f; // good baseline

            // More enemies around -> more reason to use rockets
            if (nearbyEnemies > 1)
            {
                rocketScore += Mathf.Clamp01((nearbyEnemies - 1) * 0.25f); // up to +0.75
            }

            // Slightly prefer rockets at mid-range
            float mid = rocketRange * 0.5f;
            float t = 1f - Mathf.Clamp01(Mathf.Abs(distance - mid) / mid);
            rocketScore += t * 0.5f; // up to +0.5 for "perfect" rocket distance
        }

        // Gun
        float gunRange = Attack.AllyGunData.range;
        if (distance <= gunRange)
        {
            gunScore += 0.7f; // fairly strong, but can be beaten by melee/rocket in ideal spots
        }

        // If everything somehow ended up with zero, just use the gun
        if (meleeScore <= 0f && rocketScore <= 0f && gunScore <= 0f)
            return Attack.AttackType.AllyGun;

        // Weighted random choice
        float total = meleeScore + rocketScore + gunScore;
        float r = Random.value * total;

        if (r < meleeScore)
            return Attack.AttackType.Melee;

        r -= meleeScore;
        if (r < rocketScore)
            return Attack.AttackType.Rocket;

        return Attack.AttackType.AllyGun;
    }


    /// <summary>
    /// Returns how far each attack type can reach.
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
                return Attack.AllyGunData.range;
            default:
                return Attack.AllyGunData.range;
        }
    }
}
