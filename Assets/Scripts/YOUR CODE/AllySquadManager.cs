using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the ally squad as a whole.
/// - Tracks all allies and assigns roles (leader, flanker, support).
/// - Provides positions for a triangle formation based on the current leader.
/// - Handles leader death and regrouping around a new leader.
/// - Stores a shared path computed by the leader that followers can reuse.
/// Only one instance should exist in the scene.
/// </summary>
public sealed class AllySquadManager : MonoBehaviour
{
    public static AllySquadManager Instance { get; private set; }

    [Header("Formation Settings")]
    [Tooltip("Horizontal spacing between allies in the triangle.")]
    public float formationSideSpacing = 0.7f;

    [Tooltip("Forward spacing between rows in the triangle.")]
    public float formationForwardSpacing = 0.7f;

    [Tooltip("Draw formation slots as gizmos when selected.")]
    public bool debugDrawSlots = false;

    /// <summary>
    /// All allies currently registered in the squad.
    /// </summary>
    public List<AllyAgent> Allies { get; private set; } = new List<AllyAgent>();

    /// <summary>
    /// Regroup reference point, usually set to the new leader's position when the old leader dies.
    /// </summary>
    public Vector3 RegroupPoint { get; private set; }

    // Shared path from the leader's A* computations.
    [HideInInspector] public List<Vector3> CurrentLeaderPath = new List<Vector3>();

    // Incremented whenever the leader path is updated so followers can detect changes.
    [HideInInspector] public int CurrentPathVersion = 0;

    private void Awake()
    {
        // Enforce singleton pattern.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Register allies already present in the scene (for example, placed in the editor).
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

    /// <summary>
    /// Adds a new ally to the squad and reassigns roles if needed.
    /// </summary>
    public void RegisterAlly(AllyAgent ally)
    {
        if (ally == null) return;
        if (!Allies.Contains(ally))
            Allies.Add(ally);

        AssignInitialRoles();
    }

    /// <summary>
    /// Removes an ally from the squad.
    /// If it was the leader, promotes a new leader and triggers regrouping.
    /// </summary>
    public void UnregisterAlly(AllyAgent ally)
    {
        if (ally == null) return;

        bool wasLeader = false;
        Vector3 lastLeaderPos = ally.transform.position;

        var fsm = ally.GetComponent<AllyFSM>();
        if (fsm != null && fsm.Role == AllyFSM.AllyRole.Leader)
            wasLeader = true;

        Allies.Remove(ally);

        if (wasLeader)
            PromoteNearestNewLeader(lastLeaderPos);
    }

    /// <summary>
    /// Assigns roles based on the current ally list:
    /// - First healthy ally becomes Leader.
    /// - Next two become Flankers.
    /// - Any remaining allies become Support.
    /// Also resets states to FollowLeader.
    /// </summary>
    private void AssignInitialRoles()
    {
        // Start by setting everyone to Support and FollowLeader.
        foreach (var ally in Allies)
        {
            if (ally == null) continue;
            var fsm = ally.GetComponent<AllyFSM>();
            if (fsm == null) continue;

            fsm.Role = AllyFSM.AllyRole.Support;
            fsm.State = AllyFSM.AllyState.FollowLeader;
        }

        // First healthy ally on the nav graph becomes the Leader.
        var leader = Allies.FirstOrDefault(a => a != null && a.Health > 0f && IsOnNavGraph(a));
        if (leader == null)
        {
            // Fallback: just take the first healthy ally.
            leader = Allies.FirstOrDefault(a => a != null && a.Health > 0f);
        }

        if (leader != null)
        {
            var fsm = leader.GetComponent<AllyFSM>();
            if (fsm != null) fsm.Role = AllyFSM.AllyRole.Leader;
        }

        // Next two healthy allies become Flankers.
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

    /// <summary>
    /// When the leader dies, chooses a new leader close to the old leader's position,
    /// preferring allies that are actually on the navigation graph.
    /// Sets all allies to Regroup around the new leader, then marks that ally as Leader.
    /// </summary>
    private void PromoteNearestNewLeader(Vector3 oldLeaderPos)
    {
        var alive = Allies
            .Where(a => a != null && a.Health > 0f)
            .ToList();

        if (alive.Count == 0) return;

        // Prefer allies that are on the nav graph.
        var onGraph = alive.Where(IsOnNavGraph).ToList();
        var candidates = onGraph.Count > 0 ? onGraph : alive;

        AllyAgent newLeader = null;
        float bestDistSq = float.MaxValue;

        // Find the living ally closest to the old leader's last position.
        foreach (var a in candidates)
        {
            float dsq = (a.transform.position - oldLeaderPos).sqrMagnitude;
            if (dsq < bestDistSq)
            {
                bestDistSq = dsq;
                newLeader = a;
            }
        }

        if (newLeader == null) return;

        // Regroup around the new leader's position.
        RegroupPoint = newLeader.transform.position;

        foreach (var ally in alive)
        {
            var fsm = ally.GetComponent<AllyFSM>();
            if (fsm == null) continue;

            fsm.Role = AllyFSM.AllyRole.Support;
            fsm.State = AllyFSM.AllyState.Regroup;
        }

        // Promote the chosen ally to Leader.
        var newLeaderFSM = newLeader.GetComponent<AllyFSM>();
        if (newLeaderFSM != null)
            newLeaderFSM.Role = AllyFSM.AllyRole.Leader;
    }

    /// <summary>
    /// Returns true if this ally is reasonably close to some navigation node.
    /// Used to avoid making off-graph units into leaders.
    /// </summary>
    private bool IsOnNavGraph(AllyAgent agent)
    {
        if (agent == null) return false;
        if (AStar.Instance == null) return true; // best-effort fallback

        var node = AStar.Instance.GetClosestNode(agent.transform.position);
        if (node == null) return false;

        float dSq = (node.transform.position - agent.transform.position).sqrMagnitude;
        // Within ~2 units of some node counts as "on graph".
        return dSq <= 4f;
    }

    /// <summary>
    /// Returns the current living leader, or null if none exists.
    /// </summary>
    public AllyAgent GetLeader()
    {
        return Allies.FirstOrDefault(a =>
        {
            if (a == null) return false;
            var fsm = a.GetComponent<AllyFSM>();
            return fsm != null && fsm.Role == AllyFSM.AllyRole.Leader && a.Health > 0f;
        });
    }

    /// <summary>
    /// Returns the world position this ally should occupy in the triangle formation.
    /// </summary>
    public Vector3 GetFormationPositionFor(AllyAgent ally)
    {
        var leader = GetLeader();
        if (leader == null || ally == null)
            return ally != null ? ally.transform.position : Vector3.zero;

        Vector3 leaderPos = leader.transform.position;
        Vector3 forward = leader.transform.up;
        Vector3 right = leader.transform.right;

        var aliveAllies = Allies
            .Where(a => a != null && a.Health > 0f)
            .ToList();

        int index = aliveAllies.IndexOf(ally);
        if (index < 0) return ally.transform.position;

        return GetTriangleSlot(index, leaderPos, forward, right);
    }

    /// <summary>
    /// Computes the position of a given index in a triangular formation.
    /// Index 0 is the leader at the tip.
    /// Each row behind the leader holds one more ally than the previous row.
    /// </summary>
    private Vector3 GetTriangleSlot(int index, Vector3 leaderPos, Vector3 forward, Vector3 right)
    {
        if (index == 0) return leaderPos;

        int remaining = index;
        int row = 0;

        // Row sizes: 1, 2, 3, 4, ...
        while (remaining > row)
        {
            remaining -= (row + 1);
            row++;
        }

        int agentsInRow = row + 1;

        float offsetFromCenter = remaining - (agentsInRow - 1) * 0.5f;

        Vector3 rowOffset = -forward * (row * formationForwardSpacing);
        Vector3 sideOffset = right * (offsetFromCenter * formationSideSpacing);

        return leaderPos + rowOffset + sideOffset;
    }

    /// <summary>
    /// Returns a shared attack target for an ally:
    /// - If a leader exists, returns the enemy nearest to the leader.
    /// - If no leader, returns the enemy nearest to this ally.
    /// </summary>
    public SteeringAgent GetAttackTargetFor(AllyAgent ally)
    {
        var leader = GetLeader();
        if (leader == null)
        {
            return SteeringAgent.GetNearestAgent(
                ally.transform.position,
                GameData.Instance.enemies
            );
        }

        return SteeringAgent.GetNearestAgent(
            leader.transform.position,
            GameData.Instance.enemies
        );
    }

    /// <summary>
    /// Called by the leader's pathfinding component when it computes a new A* path.
    /// Stores a copy of the path and bumps the version so followers can update.
    /// </summary>
    public void SetLeaderPath(List<Vector3> path)
    {
        if (path == null || path.Count == 0)
        {
            // Keep the old path if the new one is invalid, to avoid freezing followers.
            Debug.LogWarning("AllySquadManager: attempted to set empty leader path, keeping previous path.");
            return;
        }

        CurrentLeaderPath = new List<Vector3>(path);
        CurrentPathVersion++;
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDrawSlots) return;
        if (!Application.isPlaying) return;

        var leader = GetLeader();
        if (leader == null) return;

        // Visualize the formation slots in the editor.
        foreach (var ally in Allies)
        {
            if (ally == null || ally.Health <= 0f) continue;

            Vector3 slot = GetFormationPositionFor(ally);

            Gizmos.color = (ally == leader) ? Color.cyan : Color.magenta;
            Gizmos.DrawSphere(slot, 0.12f);
        }
    }
}
