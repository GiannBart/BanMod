//credits and licenses in the resources folder
using AmongUs.Data;
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using AmongUs.QuickChat;
using BanMod;
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Discord;
using Epic.OnlineServices;
using Epic.OnlineServices.Presence;
using Epic.OnlineServices.RTC;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMono.Security.Interface;
using Il2CppSystem;
using Il2CppSystem.Data;
using Il2CppSystem.Linq;
using Il2CppSystem.Security.Cryptography;
using InnerNet;
using JetBrains.Annotations;
using MS.Internal.Xml.XPath;
using Rewired;
using Rewired.Utils.Classes.Data;
using Rewired.Utils.Platforms.Windows;
using Sentry.Internal;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using UnityEngine.UIElements.UIR;
using static BanMod.ChatCommands;
using static BanMod.ExtendedPlayerControl;
using static BanMod.ImmortalManager;
using static BanMod.Translator;
using static BanMod.Utils;
using static InnerNet.InnerNetClient;
using static Rewired.Controller;
using static Rewired.Platforms.Custom.CustomInputSource;
using static UnityEngine.AudioSettings;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ParticleSystem.PlaybackState;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;
using static UnityEngine.UIElements.UIR.Allocator2D;
using static UnityEngine.Windows.WebCam.VideoCapture;
using Action = System.Action;
using Array = System.Array;
using BitConverter = System.BitConverter;
using CollectionExtensions = System.Collections.Generic.CollectionExtensions;
using Cursor = UnityEngine.Cursor;
using DataReceivedEventArgs = Hazel.DataReceivedEventArgs;
using DateTime = System.DateTime;
using Enum = System.Enum;
using Exception = System.Exception;
using Label = System.Reflection.Emit.Label;
using Math = System.Math;
using Object = UnityEngine.Object;
using TimeSpan = System.TimeSpan;
using Type = System.Type;

namespace BanMod;

[HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
public static class PingTracker_Update
{
    public static void Postfix(PingTracker __instance)
    {
        if (AmongUsClient.Instance == null || __instance.text == null)
            return;

        __instance.text.alignment = TextAlignmentOptions.Center;
        __instance.aspectPosition.DistanceFromEdge = new Vector3(0f, 0.50f, 0f);

        string finalText = Utils.getColoredPingText(
            AmongUsClient.Instance.Ping
        );

        int myVotes = 0;

        if (!AmongUsClient.Instance.AmHost)
        {
            myVotes = VoteBanTracker.GetVoteCount(
                AmongUsClient.Instance.ClientId
            );
        }

        if (myVotes > 0)
        {
            string voteColor = "#FFFF00";

            if (myVotes >= 2)
            {
                bool isFlashOn =
                    Mathf.FloorToInt(Time.time * 4f) % 2 == 0;

                voteColor = isFlashOn ? "#FF0000" : "#FFFF00";
            }

            finalText +=
                $" | <color={voteColor}>Kicks: {myVotes}/3</color>";
        }

        if (BanMod.ShowFPS.Value)
        {
            float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            int fps = Mathf.RoundToInt(1f / deltaTime);

            string fpsColor;

            if (fps < 20)
                fpsColor = "#FF0000";
            else if (fps < 40)
                fpsColor = "#FFFF00";
            else
                fpsColor = "#00FF00";

            finalText +=
                $"\n<color=#00FFFF>FPS:</color> " +
                $"<color={fpsColor}>{fps}</color>";
        }

        __instance.text.text = finalText;
    }
}


[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class HudManager_Update
{
    public static void Postfix(HudManager __instance)
    {
        __instance.ShadowQuad.gameObject.SetActive(!Utils.fullBrightActive());

        if (Utils.chatUiActive())
            __instance.Chat.gameObject.SetActive(true);
        else
        {
            Utils.closeChat();
            __instance.Chat.gameObject.SetActive(false);
        }
    }
}


[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Update))]
public static class AmongUsClient_Update
{
    public static void Postfix()
    {
        Spoof.spoofLevel();
    }
}

[HarmonyPatch(typeof(PlayerBanData), nameof(PlayerBanData.BanMinutesLeft), MethodType.Getter)]
public static class RemoveDisconnectPenalty_PlayerBanData_BanMinutesLeft_Postfix
{
    public static void Postfix(PlayerBanData __instance, ref int __result)
    {
        __instance.BanPoints = 0f;
        __result = 0;
    }
}


[HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
class ModManagerLateUpdatePatch
{
    public static void Prefix(ModManager __instance)
    {
        __instance.ShowModStamp();
    }
}

[HarmonyPatch(typeof(EndGameManager), "Start")]
public class EndGameManager_Start_Patch
{
    public static void Postfix(EndGameManager __instance)
    {
        if (Options.AutoRejoin.GetBool())
        {
            __instance.StartCoroutine(AutoReturnToLobby(__instance));
        }
    }

    private static IEnumerator AutoReturnToLobby(EndGameManager endGameManager)
    {
        yield return new WaitForSeconds(4f);

        endGameManager.Navigation.NextGame();
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ProtectPlayer))]
public static class ProtectPlayerPatch
{
    public static bool Prefix(
        PlayerControl __instance,
        PlayerControl target,
        int colorId)
    {
        if (target == null || target.Data == null)
            return true;

        if (!AmongUsClient.Instance.AmHost)
            return true;

        if (__instance != PlayerControl.LocalPlayer)
            return true;

        bool isCurrentlyProtected =
            BanMod.ShieldedPlayers.Contains(target.PlayerId);

        if (!isCurrentlyProtected &&
            !Options.Protection10Sec.GetBool() &&
            !Options.EnableShield.GetBool())
        {
            return true;
        }

        if (target.Data.IsDead)
            return false;

        if (target.protectedByGuardianId != -1)
            return false;

        bool canSeeProtection =
            PlayerControl.LocalPlayer.Data.IsDead ||
            (
                PlayerControl.LocalPlayer.Data.Role.TeamType ==
                RoleTeamTypes.Impostor &&
                GameOptionsManager.Instance.CurrentGameOptions.GetBool(
                    BoolOptionNames.ImpostorsCanSeeProtect
                )
            );

        target.TurnOnProtection(
            canSeeProtection,
            colorId,
            __instance.PlayerId
        );

        target.Data.MarkDirty();

        return false;
    }
}

[HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.GetKillDistance))]
public static class Patch_KillDistanceOverride
{
    static bool Prefix(LogicOptions __instance, ref float __result)
    {
        int index = __instance.currentGameOptions.GetInt(Int32OptionNames.KillDistance);
        if (index == 0 && Options.Veryshort.GetBool()) 
        {
            __result = 0.4f; 
            return false; 
        }
        if (index == 1 && Options.Veryshort.GetBool())
        {
            __result = 0.4f;
            return false;
        }
        if (index == 2 && Options.Veryshort.GetBool())
        {
            __result = 0.4f;
            return false;
        }
        return true; 
    }
}

[HarmonyPatch(typeof(NotificationPopper))]
public static class NotificationPopper_AddInfoMessagePatch
{
    private static MethodInfo addMessageToQueueMethod;
    private static MethodInfo shiftMessagesMethod;
    private static MethodInfo onMessageDestroyMethod;

    [HarmonyPostfix]
    [HarmonyPatch("Awake")]
    public static void OnAwake(NotificationPopper __instance)
    {
        addMessageToQueueMethod = AccessTools.Method(typeof(NotificationPopper), "AddMessageToQueue");
        shiftMessagesMethod = AccessTools.Method(typeof(NotificationPopper), "ShiftMessages");
        onMessageDestroyMethod = AccessTools.Method(typeof(NotificationPopper), "OnMessageDestroy");
    }

    public static void AddInfoMessage(NotificationPopper popper, string item)
    {
        if (popper == null) return;

        LobbyNotificationMessage newMessage = UnityEngine.Object.Instantiate(
            popper.notificationMessageOrigin,
            Vector3.zero,
            Quaternion.identity,
            popper.transform
        );

        newMessage.transform.localPosition = new Vector3(0f, 0f, -2f);

        Color infoColor = new Color(1f, 0.6f, 0f);

        Sprite infoSprite = popper.playerDisconnectSprite;

        string formattedItem = $"<b>{item}</b>";

        Action onDestroy = new Action(() =>
        {
            onMessageDestroyMethod?.Invoke(popper, new object[] { newMessage });
        });

        newMessage.SetUp(formattedItem, infoSprite, infoColor, onDestroy);

        SoundManager.Instance.PlaySound(popper.playerDisconnectSound, false, 1f, null);

        popper.GetType().GetField("lastMessageKey", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(popper, -1);

        shiftMessagesMethod?.Invoke(popper, null);
        addMessageToQueueMethod?.Invoke(popper, new object[] { newMessage });
    }
}
[HarmonyPatch(typeof(PlatformSpecificData), nameof(PlatformSpecificData.Serialize))]
public static class PlatformSpecificData_Serialize
{
    public static void Prefix(PlatformSpecificData __instance)
    {

        Spoof.spoofPlatform(__instance);

    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CanMove), MethodType.Getter)]
public static class BlockMovementWhenUIActive_Patch1
{
    public static void Postfix(ref bool __result)
    {
        if (PlayerUI.Instance != null && PlayerUI.Instance.open && PlayerUI.Instance.editingInput)
        {
            __result = false;
            return;
        }

    }
}


[HarmonyPatch(typeof(DisconnectPopup), "SetText")]
public static class DisconnectPopup_SetText_Patch
{
    static void Prefix(ref string text)
    {
        var client = AmongUsClient.Instance;
        if (client == null) return;

        DisconnectReasons reason = client.LastDisconnectReason;

        string originKey;
        string reasonKey;

        switch (reason)
        {
            case DisconnectReasons.Kicked:
                originKey = "disconnect.origin.host";
                reasonKey = "disconnect.reason.kicked";
                break;

            case DisconnectReasons.Banned:
                originKey = "disconnect.origin.host";
                reasonKey = "disconnect.reason.banned";
                break;

            case DisconnectReasons.Hacking:
                originKey = "disconnect.origin.system";
                reasonKey = "disconnect.reason.rpc_invalid";
                break;

            case DisconnectReasons.Sanctions:
                originKey = "disconnect.origin.system";
                reasonKey = "disconnect.reason.sanction";
                break;

            default:
                originKey = "disconnect.origin.system";
                reasonKey = "disconnect.reason.unknown";
                break;
        }

        if (reason == DisconnectReasons.Hacking &&
            string.IsNullOrWhiteSpace(client.LastCustomDisconnect))
        {
            reasonKey = "disconnect.reason.rpc_invalid_mod";
        }

        string technicalLabel = Translator.GetString("disconnect.label.technical");
        string originLabel = Translator.GetString("disconnect.label.origin");

        string technicalText = Translator.GetString(reasonKey);
        string originText = Translator.GetString(originKey);

        text +=
            "\n\n" + technicalLabel + " " + technicalText +
            "\n" + originLabel + " " + originText;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
static class AntiLobbyCrash
{
    static void Postfix(PlayerControl __instance)
    {
        if (!AmongUsClient.Instance || !AmongUsClient.Instance.AmHost) return;
        if (__instance.Data == null) return;

        string name = __instance.Data.PlayerName;
        string fc = __instance.Data.FriendCode;

        if (!IsSafe(name) || !IsSafe(fc))
        {
            BMLogger.LogWarning(
    $"[AntiCrash] name='{name}' len={name?.Length ?? -1}, fc='{fc}' len={fc?.Length ?? -1}");
            AmongUsClient.Instance.KickPlayer(__instance.PlayerId, true);
        }
    }

    static bool IsSafe(string s)
        => !string.IsNullOrEmpty(s) && s.Length < 32 && !s.Contains("<voffset");
}
[HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.GetAnonymousVotes))]
public static class LogicOptions_GetAnonymousVotes
{
    public static void Postfix(ref bool __result)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (Options.revealVotes.GetBool())
        {
            __result = false;
        }
    }
}

[HarmonyPatch(typeof(LogicOptionsNormal), nameof(LogicOptionsNormal.GetAnonymousVotes))]
public static class LogicOptionsNormal_GetAnonymousVotes
{
    public static void Postfix(ref bool __result)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (Options.revealVotes.GetBool())
        {
            __result = false;
        }
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
public static class LobbyHistoryPatch
{
    public static string LastLobbyCode { get; private set; } = string.Empty;

    public static void Postfix(AmongUsClient __instance)
    {
        if (__instance == null)
            return;

        GameModeType gameMode =
            (GameModeType)Options.GameMode.GetValue();

        string gameCode =
            GameCode.IntToGameName(__instance.GameId);

        if (!string.IsNullOrWhiteSpace(gameCode))
        {
            if (!string.Equals(
                    LastLobbyCode,
                    gameCode,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                LastLobbyCode = gameCode;
                GUIUtility.systemCopyBuffer = gameCode;

                BMLogger.Info(
                    $"[LobbyCode] Nuovo codice salvato e copiato: {gameCode}"
                );
            }
        }

        if (GameManager.Instance == null)
            return;

        if (!Options.IsLoaded)
            return;

        if (GameManager.Instance.IsHideAndSeek() ||
            gameMode == GameModeType.SnS ||
            gameMode == GameModeType.TaskRun ||
            gameMode == GameModeType.FFA)
        {
            BanMod.DisableAllRoles();
        }
    }
}

public static class ReconnectHandler
{
    public static void TryRejoin()
    {
        if (AmongUsClient.Instance == null)
            return;

        string codeStr = LobbyHistoryPatch.LastLobbyCode;

        if (string.IsNullOrWhiteSpace(codeStr))
        {
            BMLogger.LogWarning(
                "[Reconnect] Nessun codice lobby salvato."
            );

            return;
        }

        try
        {
            int gameId = GameCode.GameNameToInt(codeStr);

            BMLogger.Info(
                $"[Reconnect] Tentativo di rientro nella lobby: {codeStr}"
            );

            AmongUsClient.Instance.StartCoroutine(
                AmongUsClient.Instance.CoJoinOnlineGameFromCode(
                    gameId,
                    true
                )
            );
        }
        catch (Exception e)
        {
            BMLogger.LogWarning(
                $"[Reconnect] Codice lobby non valido: {e.Message}"
            );
        }
    }

    public static void TryNewGame()
    {
        ExecuteCreateGame();
    }

    private static void ExecuteCreateGame()
    {
        if (AmongUsClient.Instance == null)
            return;

        AmongUsClient.Instance.StartCoroutine(
            AmongUsClient.Instance.CoCreateOnlineGame()
        );
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckShapeshift))]
internal static class CheckShapeshiftPatch
{
    public static bool Prefix(PlayerControl __instance, ref PlayerControl target, ref bool shouldAnimate)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        GameModeType gameMode = (GameModeType)Options.GameMode.GetValue();
        if (gameMode == GameModeType.SnS && !GameStates.isHideNSeek && __instance.isNew)
        {
            BMLogger.Info($"Blocco Shapeshift per {__instance.Data.PlayerName} (Misfire attivo).");
            __instance.RpcRejectShapeshift();
            return false;
        }
        return true;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckShapeshift))]
internal static class BlockShapeshiftPatch
{
    public static bool Prefix(PlayerControl __instance)
    {
        GameModeType gameMode = (GameModeType)Options.GameMode.GetValue();
        if (gameMode == GameModeType.SnS && MurderPlayerCombinedPatch.isBlocked)
        {
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(AmongUsClient), "Update")]
public static class CursorUpdatePatch
{
    private static Texture2D normalCursor;
    private static Texture2D clickCursor;
    private static bool texturesLoaded = false;

    public static void Postfix()
    {
        if (!BanMod.CustomMouse.Value) return;
        if (!texturesLoaded) LoadTextures();

        if (Input.GetMouseButton(0))
        {
            SetCursor(clickCursor);
        }
        else
        {
            SetCursor(normalCursor);
        }
    }

    private static void LoadTextures()
    {
        string folderPath = Path.Combine(Application.dataPath, "..", "BAN_DATA", "IMAGE", "Cursor");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string normalPath = Path.Combine(folderPath, "cursor.png");
        string clickPath = Path.Combine(folderPath, "click.png");

        normalCursor = LoadOrCreate(normalPath, "BanMod.Resources.image.cursor.png");
        clickCursor = LoadOrCreate(clickPath, "BanMod.Resources.image.click.png");

        texturesLoaded = true;
    }

    private static Texture2D LoadOrCreate(string filePath, string resPath)
    {
        Texture2D tex = LoadExternalTexture(filePath);

        if (tex == null)
        {
            tex = Utils.LoadTextureFromResources(resPath);

            if (tex != null)
            {
                try
                {
                    byte[] bytes = ImageConversion.EncodeToPNG(tex);
                    File.WriteAllBytes(filePath, bytes);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Errore nel salvataggio del cursore di default: {e.Message}");
                }
            }
        }
        return tex;
    }

    private static void SetCursor(Texture2D tex)
    {
        if (tex == null) return;

        Vector2 hotspot = Vector2.zero;
        Cursor.SetCursor(tex, hotspot, CursorMode.Auto);
    }

    private static Texture2D LoadExternalTexture(string path)
    {
        if (!File.Exists(path)) return null;

        byte[] fileData = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (tex.LoadImage(fileData))
        {
            return tex;
        }
        return null;
    }
}
[HarmonyPatch(typeof(DeconSystem))]
public static class DeconSystemPatch
{

    [HarmonyPatch(nameof(DeconSystem.UpdateSystem))]
    [HarmonyPrefix]
    public static void UpdateSystemPrefix(DeconSystem __instance)
    {
        ApplyCustomTimes(__instance);
    }

    [HarmonyPatch(nameof(DeconSystem.Deteriorate))]
    [HarmonyPrefix]
    public static void DeterioratePrefix(DeconSystem __instance)
    {
        ApplyCustomTimes(__instance);
    }

    private static void ApplyCustomTimes(DeconSystem instance)
    {
        if (instance == null)
            return;
        float CustomTime = Options.DecontaminationTime.GetFloat();
        instance.DoorOpenTime = CustomTime / 3f;
        instance.DeconTime = CustomTime / 3f;
    }
}

[HarmonyPatch(typeof(SabotageSystemType), nameof(SabotageSystemType.UpdateSystem))]
public static class SabotageUpdatePatch
{
    public static void Postfix(SabotageSystemType __instance)
    {
        if (__instance == null)
        {
            return;
        }

        __instance.Timer = Options.SabotageCooldown.GetFloat();
    }
}


[HarmonyPatch(typeof(SabotageSystemType), "get_PercentCool")]
public static class SabotagePercentPatch
{
    public static bool Prefix(SabotageSystemType __instance, ref float __result)
    {
        if (__instance == null)
        {
            __result = 0f;
            return false;
        }

        float maxTimer = Options.SabotageCooldown.GetFloat();

        if (maxTimer <= 0f)
        {
            __result = 0f;
            return false;
        }

        __result = __instance.Timer / maxTimer;

        return false;
    }
}
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class ProxyMessageQueueUpdatePatch
{
    public static void Postfix()
    {
        try
        {
            ProxyMessageQueue.TrySendNext();
        }
        catch
        {
        }
    }
}
