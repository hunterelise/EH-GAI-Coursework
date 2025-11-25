using UnityEngine;

/// <summary>
/// This is the ally character brain.
/// It connects the movement system (SteeringAgent/SteeringBehaviours),
/// the state machine (AllyFSM), and the squad manager (AllySquadManager)
/// so the ally knows how to move and what to do.
/// </summary>
[RequireComponent(typeof(AllyFSM))]
public class AllyAgent : SteeringAgent
{
    private AllyFSM fsm;
    private AllyFormationBehaviour formationBehaviour;
    private AllySeparationBehaviour separationBehaviour;
    private SeekToMouse seekToMouseBehaviour;

    protected override void InitialiseFromAwake()
    {
        fsm = GetComponent<AllyFSM>();

        // Add all the movement behaviours this ally needs
        formationBehaviour = gameObject.AddComponent<AllyFormationBehaviour>();
        separationBehaviour = gameObject.AddComponent<AllySeparationBehaviour>();
        seekToMouseBehaviour = gameObject.AddComponent<SeekToMouse>();

        // Formation and separation are always active
        formationBehaviour.enabled = true;
        separationBehaviour.enabled = true;

        // Only the current squad leader should follow the mouse
        seekToMouseBehaviour.enabled = false;

        // Set how often the AI is allowed to update
        MaxUpdateTimeInSecondsForAI = DefaultUpdateTimeInSecondsForAI;
    }

    protected override void InitialiseFromStart()
    {
        // If there is a squad manager, let it know this ally exists
        if (AllySquadManager.Instance != null)
        {
            AllySquadManager.Instance.RegisterAlly(this);
        }

        // By default, allies start in the "FollowLeader" state
        if (fsm != null)
        {
            fsm.State = AllyFSM.AllyState.FollowLeader;
        }
    }

    protected override void Died()
    {
        base.Died();

        // When this ally dies, remove it from the squad manager
        if (AllySquadManager.Instance != null)
        {
            AllySquadManager.Instance.UnregisterAlly(this);
        }
    }

    protected override void CooperativeArbitration()
    {
        // Let the FSM decide what this ally should be doing right now
        // (state, target, attack type, etc.)
        if (fsm != null)
        {
            fsm.Tick(this);
        }

        // Turn movement behaviours on/off based on the FSM and squad settings
        ConfigureBehavioursFromFSM();

        // Let the base class combine all active steering behaviours
        base.CooperativeArbitration();

        // Try to attack if the FSM says we should and it's allowed
        TryAttackFromFSM();
    }

    private void ConfigureBehavioursFromFSM()
    {
        if (fsm == null) return;

        var squad = AllySquadManager.Instance;
        bool isLeader = (fsm.Role == AllyFSM.AllyRole.Leader);

        // Separation is always used so allies don't bunch up too much
        if (separationBehaviour != null)
        {
            separationBehaviour.enabled = true;
        }

        // The squad leader can follow the mouse cursor using SeekToMouse
        if (seekToMouseBehaviour != null)
        {
            bool leaderShouldSeekMouse =
                isLeader &&
                (fsm.State == AllyFSM.AllyState.FollowLeader ||
                 fsm.State == AllyFSM.AllyState.Attack);

            seekToMouseBehaviour.enabled = leaderShouldSeekMouse;
        }

        if (formationBehaviour == null || squad == null)
            return;

        // The leader's state decides the squad-wide formation type
        if (isLeader)
        {
            switch (fsm.State)
            {
                case AllyFSM.AllyState.FollowLeader:
                    squad.currentFormation = AllySquadManager.FormationType.V;
                    break;
                case AllyFSM.AllyState.Attack:
                    squad.currentFormation = AllySquadManager.FormationType.Line;
                    break;
                case AllyFSM.AllyState.Regroup:
                    squad.currentFormation = AllySquadManager.FormationType.Circle;
                    break;
            }
        }

        // Every ally uses the formation behaviour, but in different modes
        formationBehaviour.enabled = true;

        if (fsm.State == AllyFSM.AllyState.FollowLeader)
        {
            if (isLeader)
            {
                // Leader just moves freely (no formation offset)
                formationBehaviour.mode = AllyFormationBehaviour.Mode.Off;
            }
            else
            {
                // Non-leaders move to their formation slot behind/around the leader
                formationBehaviour.mode = AllyFormationBehaviour.Mode.Formation;
                formationBehaviour.formationTarget = squad.GetFormationPositionFor(this);
            }
        }
        else if (fsm.State == AllyFSM.AllyState.Attack)
        {
            // Allies move in an "attack" style formation
            formationBehaviour.mode = AllyFormationBehaviour.Mode.Attack;

            if (!isLeader)
            {
                // Non-leaders still try to stick to their formation positions
                // (you could add flanking logic here if you want)
                formationBehaviour.formationTarget = squad.GetFormationPositionFor(this);
            }
            else
            {
                // Leader mostly moves based on the mouse input instead of formation
                formationBehaviour.mode = AllyFormationBehaviour.Mode.Off;
            }
        }
        else if (fsm.State == AllyFSM.AllyState.Regroup)
        {
            // Everyone tries to move to one common regroup point
            formationBehaviour.mode = AllyFormationBehaviour.Mode.Regroup;
            formationBehaviour.formationTarget = squad.RegroupPoint;
        }
        else // Idle or any other state
        {
            // No formation control when idle
            formationBehaviour.mode = AllyFormationBehaviour.Mode.Off;
        }
    }

    private void TryAttackFromFSM()
    {
        if (fsm == null) return;

        var target = fsm.CurrentTarget;
        var attackType = fsm.CurrentAttackType;

        // If we have no target or the target is already dead, don't attack
        if (target == null || target.Health <= 0.0f)
            return;

        // If the FSM says "no attack", then do nothing
        if (attackType == Attack.AttackType.None)
            return;

        // If the FSM chose a rocket but we've run out, switch to the ally gun instead
        if (attackType == Attack.AttackType.Rocket &&
            GameData.Instance.AllyRocketsAvailable <= 0)
        {
            attackType = Attack.AttackType.AllyGun;
        }

        // Work out how far we can be from the target for this attack type to work
        float effectiveRange;
        switch (attackType)
        {
            case Attack.AttackType.Melee:
                effectiveRange = Attack.MeleeData.range;
                break;
            case Attack.AttackType.Rocket:
                effectiveRange = Attack.RocketData.range;
                break;
            case Attack.AttackType.AllyGun:
                effectiveRange = Attack.AllyGunData.range;
                break;
            default:
                // If something unexpected happens, treat it like a gun shot
                effectiveRange = Attack.AllyGunData.range;
                break;
        }

        Vector3 diff = target.transform.position - transform.position;
        float distance = diff.magnitude;

        // Too far away or still waiting on attack cooldown? Then don't fire yet.
        if (distance > effectiveRange || TimeToNextAttack > 0.0f)
            return;

        // Turn to face the target and then fire the chosen attack
        transform.up = diff.normalized;
        AttackWith(attackType);
    }

    protected override void UpdateDirection()
    {
        // Normally face the direction we're moving in,
        // but when attacking we also snap to face the enemy
        base.UpdateDirection();
    }
}
