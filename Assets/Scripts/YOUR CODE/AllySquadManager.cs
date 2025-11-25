using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the ally squad: who is leader, what formation they use,
/// how regrouping works, and how leadership changes.
/// Only one of these should exist in the scene.
/// </summary>
public sealed class AllySquadManager : MonoBehaviour
{
    public static AllySquadManager Instance { get; private set; }

    public enum FormationType { V, Line, Circle }

    [Header("Formation Settings")]
    public FormationType currentFormation = FormationType.V;

    // How tightly allies cluster in V formation
    public float formationSideSpacing = 0.8f;
    public float formationForwardSpacing = 0.7f;

    // Radius used for regrouping circle
    public float circleRadius = 2.5f;

    public List<AllyAgent> Allies { get; private set; } = new List<AllyAgent>();
    public Vector3 RegroupPoint { get; private set; }

    private void Awake()
    {
        // Only allow one manager to exist
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Add any allies that were already in the scene
        foreach (var steering in GameData.Instance.allies)
        {
            var ally = steering.GetComponent<AllyAgent>();
            if (ally != null && !Allies.Contains(ally))
            {
                Allies.Add(ally);
            }
        }

        AssignInitialRoles();
    }

    public void RegisterAlly(AllyAgent ally)
    {
        if (ally == null) return;

        if (!Allies.Contains(ally))
            Allies.Add(ally);

        // Whenever the squad changes, reassign roles
        AssignInitialRoles();
    }

    public void UnregisterAlly(AllyAgent ally)
    {
        if (ally == null) return;

        bool wasLeader = false;
        Vector3 lastLeaderPos = ally.transform.position;

        var fsm = ally.GetComponent<AllyFSM>();
        if (fsm != null && fsm.Role == AllyFSM.AllyRole.Leader)
            wasLeader = true;

        Allies.Remove(ally);

        // If the leader died, choose a new one nearby
        if (wasLeader)
            PromoteNearestNewLeader(lastLeaderPos);
    }

    private void AssignInitialRoles()
    {
        // Start by making everyone a Support ally
        foreach (var ally in Allies)
        {
            if (ally == null) continue;
            var fsm = ally.GetComponent<AllyFSM>();
            if (fsm == null) continue;

            fsm.Role = AllyFSM.AllyRole.Support;
            fsm.State = AllyFSM.AllyState.FollowLeader;
        }

        // First healthy ally becomes the Leader
        var leader = Allies.FirstOrDefault(a => a != null && a.Health > 0f);
        if (leader != null)
        {
            var fsm = leader.GetComponent<AllyFSM>();
            if (fsm != null)
                fsm.Role = AllyFSM.AllyRole.Leader;
        }

        // The next two allies become Flankers
        int flankers = 0;
        foreach (var ally in Allies)
        {
            if (ally == null || ally == leader) continue;

            var fsm = ally.GetComponent<AllyFSM>();
            if (fsm == null) continue;

            if (flankers < 2)
            {
                fsm.Role = AllyFSM.AllyRole.Flanker;
                flankers++;
            }
        }
    }

    private void PromoteNearestNewLeader(Vector3 oldLeaderPos)
    {
        var alive = Allies
            .Where(a => a != null && a.Health > 0f)
            .ToList();

        if (alive.Count == 0)
            return;

        // Find the closest ally to where the leader died
        AllyAgent newLeader = null;
        float bestDistSq = float.MaxValue;

        foreach (var a in alive)
        {
            float dsq = (a.transform.position - oldLeaderPos).sqrMagnitude;
            if (dsq < bestDistSq)
            {
                bestDistSq = dsq;
                newLeader = a;
            }
        }

        if (newLeader == null) return;

        // Everyone moves to this point to regroup
        RegroupPoint = newLeader.transform.position;

        foreach (var ally in alive)
        {
            var fsm = ally.GetComponent<AllyFSM>();
            if (fsm == null) continue;

            fsm.Role = AllyFSM.AllyRole.Support;
            fsm.State = AllyFSM.AllyState.Regroup;
        }

        // Promote the chosen ally to leader
        var newLeaderFSM = newLeader.GetComponent<AllyFSM>();
        newLeaderFSM.Role = AllyFSM.AllyRole.Leader;
    }

    public AllyAgent GetLeader()
    {
        // Look for the living ally that has the Leader role
        return Allies.FirstOrDefault(a =>
        {
            var fsm = a.GetComponent<AllyFSM>();
            return fsm != null && fsm.Role == AllyFSM.AllyRole.Leader && a.Health > 0;
        });
    }

    public Vector3 GetFormationPositionFor(AllyAgent ally)
    {
        var leader = GetLeader();
        if (leader == null)
            return ally.transform.position;

        Vector3 leaderPos = leader.transform.position;
        Vector3 forward = leader.transform.up;
        Vector3 right = leader.transform.right;

        int index = Allies.IndexOf(ally);

        switch (currentFormation)
        {
            case FormationType.V:
                return GetVSlot(index, leaderPos, forward, right);
            case FormationType.Line:
                return GetLineSlot(index, leaderPos, right);
            case FormationType.Circle:
                return GetCircleSlot(index, leaderPos);
        }

        return leaderPos;
    }

    private Vector3 GetVSlot(int index, Vector3 leaderPos, Vector3 forward, Vector3 right)
    {
        // Index 0 is the leader; everyone else forms a V shape behind
        if (index == 0) return leaderPos;

        int slot = index;
        int rank = (slot + 1) / 2;
        int side = (slot % 2 == 0) ? 1 : -1;

        Vector3 offset =
            (-forward * rank * formationForwardSpacing) +
            (right * side * formationSideSpacing * rank);

        return leaderPos + offset;
    }

    private Vector3 GetLineSlot(int index, Vector3 leaderPos, Vector3 right)
    {
        // Straight horizontal line next to the leader
        if (index == 0) return leaderPos;

        int slot = index - 1;
        int side = (slot % 2 == 0) ? 1 : -1;
        int rank = (slot / 2) + 1;

        Vector3 offset = right * side * formationSideSpacing * rank;
        return leaderPos + offset;
    }

    private Vector3 GetCircleSlot(int index, Vector3 leaderPos)
    {
        // Evenly place allies in a circle around the leader
        int n = Mathf.Max(Allies.Count, 1);
        float angle = (Mathf.PI * 2f) * index / n;

        return leaderPos + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * circleRadius;
    }

    public SteeringAgent GetAttackTargetFor(AllyAgent ally)
    {
        var leader = GetLeader();

        // If the leader is dead, each ally targets the nearest enemy to themselves
        if (leader == null)
        {
            return SteeringAgent.GetNearestAgent(
                ally.transform.position,
                GameData.Instance.enemies
            );
        }

        // Otherwise everyone targets whatever is closest to the leader
        return SteeringAgent.GetNearestAgent(
            leader.transform.position,
            GameData.Instance.enemies
        );
    }
}
