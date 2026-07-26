//credits and licenses in the resources folder
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BanMod;

public static class SimulatedArrowFollow
{
    private static PlayerControl target;
    private static bool active;

    private const float AliveStopDistance = 2f;

    private const float DeadStopDistance = 1f;

    private const float RecordStep = 0.12f;

    private const float WaypointReachDistance = 0.18f;

    private const int MaxPathPoints = 500;

    private static readonly Queue<Vector2> targetPath = new Queue<Vector2>();

    private static Vector2 lastRecordedTargetPos;
    private static bool hasLastRecordedTargetPos;

    public static bool IsActive
    {
        get { return active && target != null; }
    }

    public static PlayerControl Target
    {
        get { return target; }
    }

    public static void Start(PlayerControl player)
    {
        if (player == null)
        {
            Stop();
            return;
        }

        PlayerControl local = PlayerControl.LocalPlayer;

        if (local == null)
        {
            Stop();
            return;
        }

        if (player == local)
        {
            Stop();
            return;
        }

        if (local.Data == null || player.Data == null)
        {
            Stop();
            return;
        }

        target = player;
        active = true;

        ResetPath();

        Vector2 targetPos = player.transform.position;
        AddPathPoint(targetPos);

        Debug.Log("[SimulatedArrowFollow] Follow avviato verso: " + (player.Data.PlayerName));
    }

    public static void StartById(byte playerId)
    {
        PlayerControl player = GetPlayerById(playerId);

        if (player == null)
        {
            Stop();
            return;
        }

        Start(player);
    }

    public static void Stop()
    {
        active = false;
        target = null;

        ResetPath();

        try
        {
            PlayerControl local = PlayerControl.LocalPlayer;

            if (local != null && local.MyPhysics != null)
            {
                local.MyPhysics.SetNormalizedVelocity(Vector2.zero);
            }
        }
        catch
        {
        }

        Debug.Log("[SimulatedArrowFollow] Follow fermato");
    }

    public static void Toggle(PlayerControl player)
    {
        if (active && target == player)
        {
            Stop();
            return;
        }

        Start(player);
    }

    public static void ToggleById(byte playerId)
    {
        PlayerControl player = GetPlayerById(playerId);

        if (player == null)
        {
            Stop();
            return;
        }

        Toggle(player);
    }

    private static void ResetPath()
    {
        targetPath.Clear();
        hasLastRecordedTargetPos = false;
        lastRecordedTargetPos = Vector2.zero;
    }

    private static void AddPathPoint(Vector2 point)
    {
        if (hasLastRecordedTargetPos)
        {
            if (Vector2.Distance(lastRecordedTargetPos, point) < RecordStep)
            {
                return;
            }
        }

        targetPath.Enqueue(point);
        lastRecordedTargetPos = point;
        hasLastRecordedTargetPos = true;

        while (targetPath.Count > MaxPathPoints)
        {
            targetPath.Dequeue();
        }
    }

    private static PlayerControl GetPlayerById(byte playerId)
    {
        try
        {
            foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.PlayerId == playerId)
                {
                    return pc;
                }
            }
        }
        catch
        {
        }

        return null;
    }


    private static bool IsLocalDead(PlayerControl local)
    {
        try
        {
            return local != null && local.Data != null && local.Data.IsDead;
        }
        catch
        {
            return false;
        }
    }

    public static void UpdateFollow()
    {
        try
        {
            if (!active)
            {
                return;
            }

            PlayerControl local = PlayerControl.LocalPlayer;

            if (local == null || local.Data == null || local.MyPhysics == null)
            {
                Stop();
                return;
            }

            if (target == null || target.Data == null)
            {
                Stop();
                return;
            }

            Vector2 localPos = local.transform.position;
            Vector2 targetPos = target.transform.position;

            bool localIsDead = IsLocalDead(local);

            if (localIsDead)
            {
                UpdateDeadFollow(local, localPos, targetPos);
            }
            else
            {
                UpdateAliveFollow(local, localPos, targetPos);
            }
        }
        catch (Exception ex)
        {
            Debug.Log("[SimulatedArrowFollow] Errore UpdateFollow: " + ex);
            Stop();
        }
    }

    private static void UpdateDeadFollow(PlayerControl local, Vector2 localPos, Vector2 targetPos)
    {
        ResetPath();

        Vector2 direction = targetPos - localPos;
        float distance = direction.magnitude;

        if (distance <= DeadStopDistance)
        {
            local.MyPhysics.SetNormalizedVelocity(Vector2.zero);
            return;
        }

        local.MyPhysics.SetNormalizedVelocity(direction.normalized);
    }

    private static void UpdateAliveFollow(PlayerControl local, Vector2 localPos, Vector2 targetPos)
    {
        AddPathPoint(targetPos);

        float directDistanceToTarget = Vector2.Distance(localPos, targetPos);

        if (directDistanceToTarget <= AliveStopDistance)
        {
            local.MyPhysics.SetNormalizedVelocity(Vector2.zero);
            return;
        }

        while (targetPath.Count > 1)
        {
            Vector2 nextPoint = targetPath.Peek();

            if (Vector2.Distance(localPos, nextPoint) <= WaypointReachDistance)
            {
                targetPath.Dequeue();
            }
            else
            {
                break;
            }
        }

        if (targetPath.Count == 0)
        {
            local.MyPhysics.SetNormalizedVelocity(Vector2.zero);
            return;
        }

        Vector2 waypoint = targetPath.Peek();
        Vector2 direction = waypoint - localPos;

        if (direction.magnitude <= WaypointReachDistance)
        {
            local.MyPhysics.SetNormalizedVelocity(Vector2.zero);
            return;
        }

        local.MyPhysics.SetNormalizedVelocity(direction.normalized);
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
public static class SimulatedArrowFollowPhysicsPatch
{
    public static void Postfix(PlayerPhysics __instance)
    {
        if (BanMod.IsBanModDisabled) return;

        try
        {
            PlayerControl local = PlayerControl.LocalPlayer;

            if (local == null)
            {
                return;
            }

            if (local.MyPhysics == null)
            {
                return;
            }

            if (__instance != local.MyPhysics)
            {
                return;
            }

            SimulatedArrowFollow.UpdateFollow();
        }
        catch
        {
        }
    }
}