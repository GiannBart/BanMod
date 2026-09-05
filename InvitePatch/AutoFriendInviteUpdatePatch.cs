using BanMod;
using HarmonyLib;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BanMod
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class AutoFriendInviteUpdatePatch
    {
        public static void Postfix()
        {

            try
            {
                if (GameStates.isLobby)
                {
                    AutoFriendInviteManager.Update();
                }
            }
            catch (Exception)
            {
            }
        }
    }
}