using UnityEngine;

/// <summary>
/// Controls ally movement and combat.
/// - Leader computes the shared A* path (AllyPathfinder + AStar).
/// - Followers move along the shared squad path (SquadPathFollower).
/// - FSM chooses state, target, and weapon.
/// - This class wires those decisions into steering, movement and attacking,
///   and keeps the squad from ramming directly into enemies.
/// </summary>
[RequireComponent(typeof(AllyFSM))]
public class AllyAgent : SteeringAgent
{
    private AllyFSM fsm;
    private AllySeparationBehaviour separationBehaviour;
    private AllyPathfinder pathfinderBehaviour;           // Used by leader to generate A* path
    private SquadPathFollower squadPathFollower;          // Followers move along leader's path

    // Called once when the object is created
    protected override void InitialiseFromAwake()
    {
        fsm = GetComponent<AllyFSM>();

        // Ensure needed components exist
        separationBehaviour = GetComponent<AllySeparationBehaviour>() ??
                              gameObject.AddComponent<AllySeparationBehaviour>();

        pathfinderBehaviour = GetComponent<AllyPathfinder>() ??
                              gameObject.AddComponent<AllyPathfinder>();

        squadPathFollower = GetComponent<SquadPathFollower>() ??
                            gameObject.AddComponent<SquadPathFollower>();

        // Initial enable/disable states
        separationBehaviour.enabled = true;
        pathfinderBehaviour.enabled = false;
        squadPathFollower.enabled = false;

        MaxUpdateTimeInSecondsForAI = DefaultUpdateTimeInSecondsForAI;
    }

    // Called once at game start (after Awake)
    protected override void InitialiseFromStart()
    {
        // Add this ally to the squad manager
        var squad = AllySquadManager.Instance;
        if (squad != null)
            squad.RegisterAlly(this);

        // Default behaviour: follow the leader
        if (fsm != null)
            fsm.State = AllyFSM.AllyState.FollowLeader;
    }

    // Called when this ally dies
    protected override void Died()
    {
        base.Died();

        var squad = AllySquadManager.Instance;
        if (squad != null)
            squad.UnregisterAlly(this);
    }

    /// <summary>
    /// Main per-frame logic.
    /// 1. FSM decides what we want to do.
    /// 2. Configure movement behaviours (leader or follower).
    /// 3. Combine all movement forces.
    /// 4. Attempt an attack if valid.
    /// </summary>
    protected override void CooperativeArbitration()
    {
        if (fsm != null)
            fsm.Tick(this);

        ConfigureBehavioursFromFSM();

        base.CooperativeArbitration();

        TryAttackFromFSM();
    }

    /// <summary>
    /// Enables/disables steering behaviours based on:
    /// - Current FSM state (Follow, Attack, Regroup)
    /// - Ally role (Leader vs Follower)
    /// Also enforces "keep your distance" behaviour for the leader,
    /// and prevents followers from overrunning enemies.
    /// </summary>
    private void ConfigureBehavioursFromFSM()
    {
        if (fsm == null)
            return;

        var squad = AllySquadManager.Instance;
        bool isLeader = fsm.Role == AllyFSM.AllyRole.Leader;
        bool hasTarget = fsm.CurrentTarget != null && fsm.CurrentTarget.Health > 0.0f;

        // Separation is always active
        separationBehaviour.enabled = true;

        // Reset both behaviours, then activate the correct one
        if (pathfinderBehaviour != null)
            pathfinderBehaviour.enabled = false;
        if (squadPathFollower != null)
            squadPathFollower.enabled = false;

        Vector3? dest = null;

        // Decide what the leader's next destination should be
        switch (fsm.State)
        {
            case AllyFSM.AllyState.FollowLeader:
                if (isLeader && hasTarget)
                {
                    // Leader moves toward target until weapon range logic says otherwise.
                    dest = fsm.CurrentTarget.transform.position;
                }
                break;

            case AllyFSM.AllyState.Attack:
                if (isLeader && hasTarget)
                {
                    // Keep a comfortable distance: move closer if too far,
                    // stop if in a good band, back away if too close.
                    float range = GetEffectiveRange(fsm.CurrentAttackType);
                    float dist = (fsm.CurrentTarget.transform.position - transform.position).magnitude;

                    float desiredDist = range * 0.8f;       // where we want to hover
                    float tooCloseDist = desiredDist * 0.6f; // back off if closer than this
                    float tooFarDist = desiredDist + 0.4f; // move in if beyond this

                    if (dist > tooFarDist)
                    {
                        // Too far: move toward the target
                        dest = fsm.CurrentTarget.transform.position;
                    }
                    else if (dist < tooCloseDist)
                    {
                        // Too close: back away from the target a bit
                        Vector3 away = (transform.position - fsm.CurrentTarget.transform.position).normalized;
                        dest = transform.position + away * 2.0f;
                    }
                    else
                    {
                        // In a good band: no pathfinding, just hold position
                        dest = null;
                    }
                }
                break;

            case AllyFSM.AllyState.Regroup:
                if (isLeader && squad != null)
                    dest = squad.RegroupPoint;
                break;

                // Idle: dest stays null
        }

        // Leader performs A* pathfinding when we have a destination
        if (isLeader)
        {
            if (pathfinderBehaviour != null && dest.HasValue)
            {
                pathfinderBehaviour.enabled = true;
                pathfinderBehaviour.SetDestination(dest.Value);

                Debug.DrawLine(transform.position, dest.Value, Color.cyan);
            }
        }
        else
        {
            // Followers: move along leader’s path, but don’t overrun the enemy.

            if (squadPathFollower != null)
            {
                bool enableFollower = true;

                if (fsm.State == AllyFSM.AllyState.Attack && hasTarget)
                {
                    float range = GetEffectiveRange(fsm.CurrentAttackType);
                    float dist = (fsm.CurrentTarget.transform.position - transform.position).magnitude;

                    // Once a follower is in "good" range, stop advancing along the path.
                    enableFollower = dist > range * 0.8f;
                }

                squadPathFollower.enabled = enableFollower;
            }
        }
    }

    /// <summary>
    /// Executes an attack when:
    /// - Target exists
    /// - Chosen attack type is valid
    /// - Range is correct
    /// - No friendly fire risk
    /// </summary>
    private void TryAttackFromFSM()
    {
        if (fsm == null)
            return;

        var target = fsm.CurrentTarget;
        var attackType = fsm.CurrentAttackType;

        if (target == null || target.Health <= 0.0f)
            return;

        if (attackType == Attack.AttackType.None)
            return;

        // Fall back to gun if out of rockets
        if (attackType == Attack.AttackType.Rocket &&
            GameData.Instance.AllyRocketsAvailable <= 0)
        {
            attackType = Attack.AttackType.AllyGun;
        }

        // Prevent hitting allies with rockets
        if (attackType == Attack.AttackType.Rocket &&
            IsFriendlyNearTarget(target, 3.0f))
        {
            attackType = Attack.AttackType.AllyGun;
        }

        float range = GetEffectiveRange(attackType);

        Vector3 diff = target.transform.position - transform.position;
        float distance = diff.magnitude;

        // Outside range or still cooling down
        if (distance > range || TimeToNextAttack > 0.0f)
            return;

        // Avoid shooting through allies
        if (IsFriendlyInLineOfFire(target, 0.4f))
            return;

        // Rotate toward target and fire
        transform.up = diff.normalized;
        AttackWith(attackType);
    }

    /// <summary>
    /// Returns the usable range of a weapon type.
    /// </summary>
    private float GetEffectiveRange(Attack.AttackType attackType)
    {
        return attackType switch
        {
            Attack.AttackType.Melee => Attack.MeleeData.range,
            Attack.AttackType.Rocket => Attack.RocketData.range,
            Attack.AttackType.AllyGun => Attack.AllyGunData.range,
            _ => Attack.AllyGunData.range
        };
    }

    /// <summary>
    /// Checks whether any ally is too close to the target (rocket safety).
    /// </summary>
    private bool IsFriendlyNearTarget(SteeringAgent target, float radius)
    {
        float radiusSq = radius * radius;

        foreach (var ally in GameData.Instance.allies)
        {
            if (ally == null || ally == this)
                continue;

            if ((ally.transform.position - target.transform.position).sqrMagnitude <= radiusSq)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if an ally stands between this unit and the target.
    /// Prevents friendly fire when shooting.
    /// </summary>
    private bool IsFriendlyInLineOfFire(SteeringAgent target, float allyRadius)
    {
        Vector3 start = transform.position;
        Vector3 end = target.transform.position;

        Vector3 toTarget = end - start;
        float targetDist = toTarget.magnitude;

        if (targetDist <= 0.001f)
            return false;

        Vector3 dir = toTarget / targetDist;
        float allyRadiusSq = allyRadius * allyRadius;

        foreach (var ally in GameData.Instance.allies)
        {
            if (ally == null || ally == this)
                continue;

            Vector3 toAlly = ally.transform.position - start;
            float proj = Vector3.Dot(toAlly, dir);

            // Ally is behind us or beyond target = safe
            if (proj <= 0f || proj >= targetDist)
                continue;

            Vector3 closest = start + dir * proj;
            float sideDistSq = (ally.transform.position - closest).sqrMagnitude;

            if (sideDistSq <= allyRadiusSq)
                return true;
        }

        return false;
    }

    protected override void UpdateDirection()
    {
        // Base handles facing direction based on movement
        base.UpdateDirection();
    }
}
