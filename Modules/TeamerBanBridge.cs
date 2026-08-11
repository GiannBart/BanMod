//credits and licenses in the resources folder/
using HarmonyLib;
using InnerNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace BanMod
{
    public enum ActionTeamersMode
    {
        OnlyWarm,
        Kick,
        Ban
    }

    public static class ActionTeamersBridge
    {
        private const string ActionConfigPath = "./BAN_DATA/DENIED/ActionTeamers.txt";
        private const ActionTeamersMode DefaultAction = ActionTeamersMode.OnlyWarm;

        private static bool initialized;

        private static readonly HashSet<string> alreadyNotified =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            try
            {
                Directory.CreateDirectory("BAN_DATA/DENIED");

                if (!File.Exists(ActionConfigPath))
                    File.WriteAllText(ActionConfigPath, DefaultAction.ToString());

                TeamerManager.Initialize();

                BMLogger.Info("[ActionTeamersBridge] Inizializzato. ActionTeamers=" + GetCurrentAction(), "AntiCheat");
            }
            catch (Exception ex)
            {
                Debug.LogError("[ActionTeamersBridge] Errore Initialize: " + ex);
            }
        }

        public static bool HandleIfTeamer(ClientData player)
        {
            try
            {
                if (!AmongUsClient.Instance.AmHost)
                    return false;

                if (player == null)
                    return false;

                if (BanMod.IsProtected(player))
                    return false;

                Initialize();

                if (!TeamerManager.CheckList(player))
                    return false;

                ActionTeamersMode action = GetCurrentAction();
                string realName = SafePlayerName(player);
                string notifyKey = GetNotifyKey(player);

                switch (action)
                {
                    case ActionTeamersMode.OnlyWarm:
                        if (!alreadyNotified.Contains(notifyKey))
                        {
                            alreadyNotified.Add(notifyKey);
                            ShowNotify($"{realName} è nella lista Teamer");
                            BMLogger.Info($"[ActionTeamers] Solo notifica per teamer: {realName}", "AntiCheat");
                        }

                        return false;

                    case ActionTeamersMode.Kick:
                        AmongUsClient.Instance.KickPlayer(player.Id, false);
                        ShowNotify($"{realName} espulso: lista Teamer");
                        BMLogger.Info($"[ActionTeamers] Espulso perché in lista Teamer: {realName}", "AntiCheat");
                        return true;

                    case ActionTeamersMode.Ban:
                        AmongUsClient.Instance.KickPlayer(player.Id, true);
                        ShowNotify($"{realName} bannato: lista Teamer");
                        BMLogger.Info($"[ActionTeamers] Bannato perché in lista Teamer: {realName}", "AntiCheat");
                        return true;

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[ActionTeamersBridge] Errore HandleIfTeamer: " + ex);
                return false;
            }
        }

        public static void AddTeamer(ClientData player, string reason = "Teaming")
        {
            try
            {
                if (!AmongUsClient.Instance.AmHost)
                    return;

                if (player == null)
                    return;

                if (BanMod.IsProtected(player))
                    return;

                Initialize();

                if (string.IsNullOrWhiteSpace(reason))
                    reason = "Teaming";

                TeamerManager.AddPlayer(player, reason);

                ShowNotify($"{SafePlayerName(player)} aggiunto alla lista Teamer");
                BMLogger.Info($"[ActionTeamers] Aggiunto alla lista Teamer: {SafePlayerName(player)} ({reason})", "AntiCheat");
            }
            catch (Exception ex)
            {
                Debug.LogError("[ActionTeamersBridge] Errore AddTeamer: " + ex);
            }
        }

        public static void SetActionToOnlyWarm()
        {
            SetAction(ActionTeamersMode.OnlyWarm);
        }

        public static void SetActionToKick()
        {
            SetAction(ActionTeamersMode.Kick);
        }

        public static void SetActionToBan()
        {
            SetAction(ActionTeamersMode.Ban);
        }

        public static void SetAction(ActionTeamersMode action)
        {
            try
            {
                Directory.CreateDirectory("BAN_DATA/DENIED");
                File.WriteAllText(ActionConfigPath, action.ToString());

                ShowNotify("ActionTeamers impostato su: " + ActionToDisplayName(action));
                BMLogger.Info("[ActionTeamers] Impostato su: " + action, "AntiCheat");
            }
            catch (Exception ex)
            {
                Debug.LogError("[ActionTeamersBridge] Errore SetAction: " + ex);
            }
        }

        public static ActionTeamersMode GetCurrentAction()
        {
            string optionValue = TryReadActionTeamersFromOptions();

            if (!string.IsNullOrWhiteSpace(optionValue))
                return ParseAction(optionValue, DefaultAction);

            string fileValue = TryReadActionFromFile();

            if (!string.IsNullOrWhiteSpace(fileValue))
                return ParseAction(fileValue, DefaultAction);

            return DefaultAction;
        }

        private static string TryReadActionTeamersFromOptions()
        {
            try
            {
                Type optionsType = typeof(Options);

                BindingFlags flags =
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static;

                FieldInfo field = optionsType.GetField("ActionTeamers", flags);
                if (field != null)
                {
                    object optionObj = field.GetValue(null);
                    string value = ExtractOptionValue(optionObj);

                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }

                PropertyInfo prop = optionsType.GetProperty("ActionTeamers", flags);
                if (prop != null)
                {
                    object optionObj = prop.GetValue(null, null);
                    string value = ExtractOptionValue(optionObj);

                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
            catch
            {
            }

            return "";
        }

        private static string ExtractOptionValue(object optionObj)
        {
            if (optionObj == null)
                return "";

            try
            {
                MethodInfo getString = optionObj.GetType().GetMethod("GetString", Type.EmptyTypes);
                if (getString != null)
                {
                    object result = getString.Invoke(optionObj, null);
                    return result?.ToString() ?? "";
                }

                MethodInfo getValue = optionObj.GetType().GetMethod("GetValue", Type.EmptyTypes);
                if (getValue != null)
                {
                    object result = getValue.Invoke(optionObj, null);
                    return result?.ToString() ?? "";
                }

                return optionObj.ToString();
            }
            catch
            {
                return "";
            }
        }

        private static string TryReadActionFromFile()
        {
            try
            {
                if (!File.Exists(ActionConfigPath))
                    return "";

                return File.ReadAllText(ActionConfigPath).Trim();
            }
            catch
            {
                return "";
            }
        }

        private static ActionTeamersMode ParseAction(string raw, ActionTeamersMode fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            string value = raw.Trim().ToLowerInvariant();

            value = value.Replace(" ", "")
                         .Replace("_", "")
                         .Replace("-", "");

            if (value == "onlywarm" ||
                value == "onlywarn" ||
                value == "notifyonly" ||
                value == "notify" ||
                value == "notifica" ||
                value == "solonotifica" ||
                value == "soloavviso" ||
                value == "avviso")
            {
                return ActionTeamersMode.OnlyWarm;
            }

            if (value == "kick" ||
                value == "espelli" ||
                value == "espellere" ||
                value == "espulsione")
            {
                return ActionTeamersMode.Kick;
            }

            if (value == "ban" ||
                value == "banna" ||
                value == "bannare")
            {
                return ActionTeamersMode.Ban;
            }

            return fallback;
        }

        private static string ActionToDisplayName(ActionTeamersMode action)
        {
            switch (action)
            {
                case ActionTeamersMode.OnlyWarm:
                    return "Solo notifica";
                case ActionTeamersMode.Kick:
                    return "Espelli";
                case ActionTeamersMode.Ban:
                    return "Banna";
                default:
                    return action.ToString();
            }
        }

        private static string SafePlayerName(ClientData player)
        {
            try
            {
                string name = BanMod.GetRealPlayerName(player);

                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }
            catch
            {
            }

            return player?.PlayerName ?? "Player";
        }

        private static string GetNotifyKey(ClientData player)
        {
            if (player == null)
                return "";

            try
            {
                string puid = player.GetHashedPuid();

                if (!string.IsNullOrWhiteSpace(puid) && puid != "e3b0cb855")
                    return "puid:" + puid;
            }
            catch
            {
            }

            if (!string.IsNullOrWhiteSpace(player.FriendCode))
                return "fc:" + player.FriendCode;

            return "id:" + player.Id;
        }

        private static void ShowNotify(string message)
        {
            try
            {
                if (HudManager.Instance?.Notifier != null)
                {
                    NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
                        HudManager.Instance.Notifier,
                        message
                    );
                }
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(BanManager), nameof(BanManager.Initialize))]
    public static class BanManagerInitializeActionTeamersPatch
    {
        public static void Postfix()
        {
            ActionTeamersBridge.Initialize();
        }
    }

    [HarmonyPatch(typeof(BanManager), nameof(BanManager.CheckBanPlayer))]
    public static class BanManagerCheckActionTeamersPatch
    {
        public static bool Prefix(ClientData player)
        {
            bool handled = ActionTeamersBridge.HandleIfTeamer(player);

            return !handled;
        }
    }
}
