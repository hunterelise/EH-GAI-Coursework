using System;
using UnityEngine;

public class AllyAgent : SteeringAgent
{
    private Attack.AttackType attackType;
    private EnemyDetector enemyDetector;
    private ChaseEnemy chaseEnemy;
    public static AllyAgent Leader;
    public bool IsLeader => this == Leader;


    protected override void InitialiseFromAwake()
    {
        enemyDetector = gameObject.AddComponent<EnemyDetector>(); // detect enemies
        chaseEnemy = gameObject.AddComponent<ChaseEnemy>();       // move towards enemies

        int index = Array.IndexOf(AgentCreator.AllySteeringAgentTypes, typeof(AllyAgent));

        // first ally becomes leader, others follow
        if (index == 0)
        {
            gameObject.AddComponent<GroupLeader>();
        }
        else
        {
            gameObject.AddComponent<GroupFollow>();
        }

        gameObject.AddComponent<Cohesion>();   // stay close to group
        gameObject.AddComponent<Separation>(); // avoid crowding

        enemyDetector.enabled = true;
        chaseEnemy.enabled = true;
    }

    protected override void CooperativeArbitration()
    {
        base.CooperativeArbitration();
        AutoPickWeaponAndAttack(); // attack if enemy in sight
    }

    private void AutoPickWeaponAndAttack()
    {
        var target = enemyDetector.TargetAgent;
        if (target == null) return;

        float dist = Vector3.Distance(transform.position, target.transform.position);

        // choose weapon based on distance
        if (dist < 2f)
            attackType = Attack.AttackType.Melee;
        else if (dist > 10f && GameData.Instance.AllyRocketsAvailable > 0)
            attackType = Attack.AttackType.Rocket;
        else
            attackType = Attack.AttackType.AllyGun;

        AttackWith(attackType); // perform attack
    }
}
