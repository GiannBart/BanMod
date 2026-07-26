//credits and licenses in the resources folder
using AmongUs.Data;
using AmongUs.QuickChat;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Il2CppObject = Il2CppSystem.Object;

namespace BanMod;

public static class ModdedOriginalChatManager
{
    public static bool Enabled = false;

    private const int MaxInputChars = 114;

    public static void Reset()
    {
        Enabled = false;
        ModdedOriginalChatButton.Reset();
    }

    public static List<PlayerControl> GetModdedTargets(bool includeLocal = false)
    {
        try
        {
            return PlayerControl.AllPlayerControls.ToArray()
                .Where(p =>
                    p != null &&
                    p.Data != null &&
                    !p.Data.Disconnected &&
                    (includeLocal || p != PlayerControl.LocalPlayer) &&
                    UnifiedRPCHandlerPatch.IsClientModded(p.PlayerId))
                .OrderBy(p => p.PlayerId)
                .ToList();
        }
        catch
        {
            return new List<PlayerControl>();
        }
    }

    public static PlayerControl GetPlayer(byte playerId)
    {
        try
        {
            return PlayerControl.AllPlayerControls.ToArray()
                .FirstOrDefault(p => p != null && p.PlayerId == playerId);
        }
        catch
        {
            return null;
        }
    }

    public static void Toggle()
    {
        Enabled = !Enabled;
        ModdedOriginalChatButton.RefreshVisual();

        try
        {
            HudManager.Instance?.Notifier?.AddDisconnectMessage(
                Enabled ? "Modded chat: ON" : "Modded chat: OFF");
        }
        catch
        {
        }
    }

    public static void SendAll(string text)
    {
        PlayerControl local = PlayerControl.LocalPlayer;

        if (local == null || local.Data == null || AmongUsClient.Instance == null)
            return;

        if (string.IsNullOrWhiteSpace(text))
            return;

        text = text.Trim();

        if (text.Length > MaxInputChars)
            text = text.Substring(0, MaxInputChars);

        List<PlayerControl> targets = GetModdedTargets(false);

        foreach (PlayerControl target in targets)
        {
            int targetClientId = target.GetClientId();

            if (targetClientId < 0)
                continue;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                local.NetId,
                (byte)CustomRPC.ModdedAllChat,
                SendOption.Reliable,
                targetClientId);

            writer.Write(local.PlayerId);
            writer.Write(text);

            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        ReceiveAll(local.PlayerId, text);
    }

    public static void ReceiveAll(byte senderId, string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            PlayerControl sender = GetPlayer(senderId);

            if (sender == null)
                return;

            text = text.Trim();

            if (text.Length > MaxInputChars)
                text = text.Substring(0, MaxInputChars);

            ChatController chat = HudManager.Instance != null ? HudManager.Instance.Chat : null;

            if (chat == null)
                return;

            AddModdedBubbleBypassDeadFilter(chat, sender, FormatModdedChatText(text));
            bool outgoing =
                PlayerControl.LocalPlayer != null &&
                senderId == PlayerControl.LocalPlayer.PlayerId;

            if (!outgoing)
            {
                try
                {
                    PlayOriginalChatSound(senderId);
                }
                catch
                {
                }

                try
                {
                    BanMod.FlashColor(new Color(0f, 0.45f, 1f, 0.30f), 1.1f);
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            try
            {
                BMLogger.Warn($"[ModdedOriginalChat] ReceiveAll failed: {ex}", "ModdedChat");
            }
            catch
            {
            }
        }
    }
    private static void AddModdedBubbleBypassDeadFilter(ChatController chat, PlayerControl sourcePlayer, string chatText)
    {
        try
        {
            if (chat == null || sourcePlayer == null || PlayerControl.LocalPlayer == null)
                return;

            if (sourcePlayer.Data == null || PlayerControl.LocalPlayer.Data == null)
                return;

            ChatBubble bubble = null;

            try
            {
                if (chat.chatBubblePool.NotInUse == 0)
                    chat.chatBubblePool.ReclaimOldest();

                bubble = chat.chatBubblePool.Get<ChatBubble>();
            }
            catch
            {
                bubble = null;
            }

            if (bubble == null)
                return;

            try
            {
                bubble.transform.SetParent(chat.scroller.Inner);
                bubble.transform.localScale = Vector3.one;

                bool outgoing = sourcePlayer == PlayerControl.LocalPlayer;

                if (outgoing)
                    bubble.SetRight();
                else
                    bubble.SetLeft();

                bool didVote = false;

                try
                {
                    didVote = MeetingHud.Instance && MeetingHud.Instance.DidVote(sourcePlayer.PlayerId);
                }
                catch
                {
                    didVote = false;
                }

                bubble.SetCosmetics(sourcePlayer.Data);

                Color nameColor = Color.white;

                try
                {
                    nameColor = PlayerNameColor.Get(sourcePlayer.Data);
                }
                catch
                {
                    try
                    {
                        int colorId = sourcePlayer.Data.DefaultOutfit.ColorId;

                        if (colorId >= 0 && colorId < Palette.PlayerColors.Length)
                            nameColor = Palette.PlayerColors[colorId];
                    }
                    catch
                    {
                        nameColor = Color.white;
                    }
                }

                SetBubbleNameDirect(bubble, sourcePlayer.Data, sourcePlayer.Data.IsDead, didVote, nameColor);

                try
                {
                    if (DataManager.Settings.Multiplayer.CensorChat)
                        chatText = BlockedWords.CensorWords(chatText, false);
                }
                catch
                {
                }

                bubble.SetText(chatText);
                bubble.AlignChildren();

                InvokeAlignAllBubbles(chat);

                try
                {
                    if (!chat.IsOpenOrOpening)
                        ChatControllerUpdatePatch.TryFlashChatNotifyDot();
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                try
                {
                    BMLogger.Warn($"[ModdedOriginalChat] AddModdedBubbleBypassDeadFilter inner failed: {ex}", "ModdedChat");
                }
                catch
                {
                }

                try
                {
                    chat.chatBubblePool.Reclaim(bubble);
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            try
            {
                BMLogger.Warn($"[ModdedOriginalChat] AddModdedBubbleBypassDeadFilter failed: {ex}", "ModdedChat");
            }
            catch
            {
            }
        }
    }

    private static void SetBubbleNameDirect(
    ChatBubble bubble,
    NetworkedPlayerInfo playerInfo,
    bool isDead,
    bool didVote,
    Color nameColor)
    {
        try
        {
            string playerName = playerInfo.PlayerName;

            if (string.IsNullOrWhiteSpace(playerName))
                playerName = "...";

            if (isDead)
            {
                playerName = "👻 " + playerName;
            }

            bubble.SetName(playerName, false, didVote, nameColor);
        }
        catch
        {
            try
            {
                bubble.SetName("...", false, didVote, nameColor);
            }
            catch
            {
            }
        }
    }

    private static void InvokeAlignAllBubbles(ChatController chat)
    {
        try
        {
            AccessTools.Method(typeof(ChatController), "AlignAllBubbles")?.Invoke(chat, null);
        }
        catch
        {
        }
    }
    private static string FormatModdedChatText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        text = text.Trim();

        if (text.StartsWith("MC:", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("<color=#66ccff>MC:</color>", StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        return "<color=#66ccff>MC:</color> " + text;
    }

    private static void PlayOriginalChatSound(byte senderId)
    {
        try
        {
            ChatController chat = HudManager.Instance != null ? HudManager.Instance.Chat : null;

            if (chat == null)
                return;

            AudioClip messageSound =
                AccessTools.Field(typeof(ChatController), "messageSound")?.GetValue(chat) as AudioClip;

            if (messageSound == null)
                return;

            SoundManager.Instance.PlaySound(messageSound, false, 1f, null).pitch =
                0.5f + senderId / 15f;
        }
        catch
        {
        }
    }
}

public static class ModdedOriginalChatButton
{
    private static GameObject ButtonObject;
    private static SpriteRenderer ButtonRenderer;
    private static BoxCollider2D ButtonCollider;

    private static Sprite OffSprite;
    private static Sprite OnSprite;

    private static Vector3 ButtonLocalPosition = new Vector3(2.85f, 2.10f, -10f);

    private static readonly Vector3 ButtonScale = new Vector3(0.65f, 0.65f, 1f);
    private static readonly Vector3 ButtonHoverScale = new Vector3(0.74f, 0.74f, 1f);

    private static float lastClickTime = -10f;

    private static bool dragging = false;
    private static bool wasDragged = false;
    private static bool userMovedButton = false;

    private static Vector3 dragStartMouseWorld;
    private static Vector3 dragStartLocalPosition;

    private const float DragThresholdWorld = 0.035f;

    public static void Reset()
    {
        ButtonObject = null;
        ButtonRenderer = null;
        ButtonCollider = null;
        OffSprite = null;
        OnSprite = null;

        lastClickTime = -10f;
        dragging = false;
        wasDragged = false;
        userMovedButton = false;

        ButtonLocalPosition = new Vector3(2.85f, 2.10f, -10f);
    }

    public static void Ensure(ChatController chat)
    {
        try
        {
            if (chat == null || chat.transform == null)
                return;

            Transform parent = GetBestParent(chat, out Vector3 wantedLocalPosition);

            if (parent == null)
                parent = chat.transform;

            if (!userMovedButton)
                ButtonLocalPosition = wantedLocalPosition;

            if (ButtonObject != null &&
                ButtonObject.transform != null &&
                ButtonObject.transform.parent == parent)
            {
                RefreshVisual();
                return;
            }

            DestroyOldButton(parent);

            OffSprite = Utils.LoadSprite("BanMod.Resources.image.ModChat.png", 200f);
            OnSprite = Utils.LoadSprite("BanMod.Resources.image.ModChat1.png", 200f);

            ButtonObject = new GameObject("BANMOD_OriginalChatModdedToggle");
            ButtonObject.transform.SetParent(parent, false);
            ButtonObject.transform.localPosition = ButtonLocalPosition;
            ButtonObject.transform.localScale = ButtonScale;
            ButtonObject.transform.localRotation = Quaternion.identity;

            int uiLayer = LayerMask.NameToLayer("UI");

            if (uiLayer >= 0)
                ButtonObject.layer = uiLayer;

            ButtonRenderer = ButtonObject.AddComponent<SpriteRenderer>();
            ButtonRenderer.sprite = ModdedOriginalChatManager.Enabled ? OnSprite : OffSprite;
            ButtonRenderer.color = Color.white;
            ButtonRenderer.sortingOrder = short.MaxValue;

            try
            {
                ButtonRenderer.sortingLayerName = "UI";
            }
            catch
            {
            }

            ButtonCollider = ButtonObject.AddComponent<BoxCollider2D>();
            ButtonCollider.isTrigger = true;
            ButtonCollider.enabled = true;

            if (ButtonRenderer.sprite != null)
            {
                ButtonCollider.size = ButtonRenderer.sprite.bounds.size;
                ButtonCollider.offset = Vector2.zero;
            }
            else
            {
                ButtonCollider.size = new Vector2(1f, 1f);
                ButtonCollider.offset = Vector2.zero;
            }

            RefreshVisual();
        }
        catch (Exception ex)
        {
            try
            {
                BMLogger.Warn($"[ModdedOriginalChatButton] Ensure failed: {ex}", "ModdedChat");
            }
            catch
            {
            }
        }
    }

    private static Transform GetBestParent(ChatController chat, out Vector3 wantedLocalPosition)
    {
        wantedLocalPosition = new Vector3(2.85f, 2.10f, -10f);

        try
        {
            GameObject keyboardButton = chat.openKeyboardButton;

            if (keyboardButton != null && keyboardButton.transform != null)
            {
                Transform parent = keyboardButton.transform.parent != null
                    ? keyboardButton.transform.parent
                    : chat.transform;

                Vector3 p = keyboardButton.transform.localPosition;

                wantedLocalPosition = new Vector3(
                    p.x,
                    p.y,
                    -10f);

                return parent;
            }
        }
        catch
        {
        }

        try
        {
            if (chat.quickChatButton != null && chat.quickChatButton.transform != null)
            {
                Transform parent = chat.quickChatButton.transform.parent != null
                    ? chat.quickChatButton.transform.parent
                    : chat.transform;

                Vector3 p = chat.quickChatButton.transform.localPosition;

                wantedLocalPosition = new Vector3(
                    p.x,
                    p.y,
                    -10f);

                return parent;
            }
        }
        catch
        {
        }

        return chat.transform;
    }

    public static void ManualLateUpdate(ChatController chat)
    {
        try
        {
            if (chat == null || chat.transform == null)
                return;

            Transform parent = GetBestParent(chat, out Vector3 wantedLocalPosition);

            if (parent == null)
                parent = chat.transform;

            if (ButtonObject == null ||
                ButtonObject.transform == null ||
                ButtonObject.transform.parent != parent)
            {
                Ensure(chat);
                return;
            }

            bool visible = chat.IsOpenOrOpening;

            ButtonObject.SetActive(visible);

            if (!visible)
                return;

            if (!userMovedButton && !dragging)
            {
                ButtonLocalPosition = wantedLocalPosition;
                ButtonObject.transform.localPosition = ButtonLocalPosition;
            }

            RefreshVisual();

            Camera cam = GetCamera();

            if (cam == null)
                return;

            Vector3 mouseWorld = GetMouseWorld(cam);

            bool hover =
                ButtonCollider != null &&
                ButtonCollider.enabled &&
                ButtonCollider.gameObject.activeInHierarchy &&
                ButtonCollider.OverlapPoint(mouseWorld);

            bool ctrl =
                Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);

            if (!dragging)
                ButtonObject.transform.localScale = hover ? ButtonHoverScale : ButtonScale;

            if (ctrl && hover && Input.GetMouseButtonDown(0))
            {
                dragging = true;
                wasDragged = false;
                dragStartMouseWorld = mouseWorld;
                dragStartLocalPosition = ButtonObject.transform.localPosition;
                return;
            }

            if (dragging && Input.GetMouseButton(0))
            {
                Vector3 currentMouseWorld = GetMouseWorld(cam);
                Vector3 worldDelta = currentMouseWorld - dragStartMouseWorld;

                if (worldDelta.magnitude >= DragThresholdWorld)
                    wasDragged = true;

                if (wasDragged)
                {
                    Vector3 newLocal;

                    if (ButtonObject.transform.parent != null)
                    {
                        Vector3 parentStart = ButtonObject.transform.parent.InverseTransformPoint(dragStartMouseWorld);
                        Vector3 parentNow = ButtonObject.transform.parent.InverseTransformPoint(currentMouseWorld);
                        Vector3 localDelta = parentNow - parentStart;

                        newLocal = dragStartLocalPosition + localDelta;
                    }
                    else
                    {
                        newLocal = dragStartLocalPosition + worldDelta;
                    }

                    newLocal.z = -10f;

                    ButtonLocalPosition = newLocal;
                    ButtonObject.transform.localPosition = ButtonLocalPosition;
                    userMovedButton = true;
                }

                return;
            }

            if (dragging && Input.GetMouseButtonUp(0))
            {
                dragging = false;

                Vector3 pos = ButtonObject.transform.localPosition;
                pos.z = -10f;

                ButtonLocalPosition = pos;
                ButtonObject.transform.localPosition = ButtonLocalPosition;

                if (wasDragged)
                    userMovedButton = true;

                return;
            }

            if (!ctrl && hover && Input.GetMouseButtonDown(0))
            {
                if (Time.realtimeSinceStartup - lastClickTime < 0.15f)
                    return;

                lastClickTime = Time.realtimeSinceStartup;
                ModdedOriginalChatManager.Toggle();
            }
        }
        catch
        {
        }
    }

    private static Camera GetCamera()
    {
        try
        {
            if (HudManager.Instance != null && HudManager.Instance.UICamera != null)
                return HudManager.Instance.UICamera;
        }
        catch
        {
        }

        try
        {
            return Camera.main;
        }
        catch
        {
            return null;
        }
    }

    private static Vector3 GetMouseWorld(Camera cam)
    {
        Vector3 mouse = Input.mousePosition;

        try
        {
            float depth = 10f;

            if (ButtonObject != null)
                depth = Mathf.Abs(cam.transform.position.z - ButtonObject.transform.position.z);

            if (depth <= 0.01f)
                depth = 10f;

            mouse.z = depth;
        }
        catch
        {
            mouse.z = 10f;
        }

        return cam.ScreenToWorldPoint(mouse);
    }

    private static void DestroyOldButton(Transform parent)
    {
        try
        {
            if (parent == null)
                return;

            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child == null || child.gameObject == null)
                    continue;

                if (child.gameObject.name == "BANMOD_OriginalChatModdedToggle")
                {
                    try
                    {
                        Object.Destroy(child.gameObject);
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch
        {
        }
    }

    public static void RefreshVisual()
    {
        try
        {
            if (ButtonObject == null)
                return;

            if (ButtonRenderer == null)
                ButtonRenderer = ButtonObject.GetComponent<SpriteRenderer>();

            if (ButtonCollider == null)
                ButtonCollider = ButtonObject.GetComponent<BoxCollider2D>();

            if (OffSprite == null)
                OffSprite = Utils.LoadSprite("BanMod.Resources.image.ModChat.png", 100f);

            if (OnSprite == null)
                OnSprite = Utils.LoadSprite("BanMod.Resources.image.ModChat1.png", 100f);

            if (ButtonRenderer != null)
            {
                ButtonRenderer.sprite = ModdedOriginalChatManager.Enabled ? OnSprite : OffSprite;
                ButtonRenderer.color = Color.white;
                ButtonRenderer.enabled = true;
                ButtonRenderer.sortingOrder = short.MaxValue;

                try
                {
                    ButtonRenderer.sortingLayerName = "UI";
                }
                catch
                {
                }
            }

            if (ButtonCollider != null && ButtonRenderer != null && ButtonRenderer.sprite != null)
            {
                ButtonCollider.size = ButtonRenderer.sprite.bounds.size;
                ButtonCollider.offset = Vector2.zero;
                ButtonCollider.enabled = true;
                ButtonCollider.isTrigger = true;
            }

            ButtonObject.transform.localPosition = ButtonLocalPosition;
            ButtonObject.transform.localRotation = Quaternion.identity;
        }
        catch
        {
        }
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Awake))]
public static class ModdedOriginalChatButtonAwakePatch
{
    public static void Postfix(ChatController __instance)
    {
        try
        {
            ModdedOriginalChatButton.Ensure(__instance);
        }
        catch
        {
        }
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
public static class ModdedOriginalChatButtonHudStartPatch
{
    public static void Postfix(HudManager __instance)
    {
        try
        {
            if (__instance == null || __instance.Chat == null)
                return;

            ModdedOriginalChatButton.Ensure(__instance.Chat);
        }
        catch
        {
        }
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.LateUpdate))]
public static class ModdedOriginalChatButtonLateUpdatePatch
{
    public static void Postfix(ChatController __instance)
    {
        try
        {
            ModdedOriginalChatButton.ManualLateUpdate(__instance);
        }
        catch
        {
        }
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
public static class ModdedOriginalChatSendChatPatch
{
    public static bool Prefix(ChatController __instance)
    {
        try
        {
            if (__instance == null)
                return false;

            if (!ModdedOriginalChatManager.Enabled)
                return true;

            bool quickChatReady = false;

            try
            {
                quickChatReady =
                    __instance.quickChatMenu != null &&
                    __instance.quickChatMenu.CanSend;
            }
            catch
            {
                quickChatReady = false;
            }

            if (quickChatReady)
                return true;

            FreeChatInputField freeChatField = null;

            try
            {
                freeChatField = __instance.freeChatField;
            }
            catch
            {
            }

            if (freeChatField == null)
            {
                try
                {
                    freeChatField = __instance.GetComponentInChildren<FreeChatInputField>(true);
                }
                catch
                {
                }
            }

            if (freeChatField == null)
                return false;

            string text = null;

            try
            {
                text = freeChatField.Text;
            }
            catch
            {
                try
                {
                    text = freeChatField.textArea != null ? freeChatField.textArea.text : null;
                }
                catch
                {
                    text = null;
                }
            }

            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            if (text.Length > 114)
                text = text.Substring(0, 114);

            if (UrlFinder.TryFindUrl(text.ToCharArray(), out _, out _))
            {
                try
                {
                    __instance.AddChatWarning(
                        DestroyableSingleton<TranslationController>.Instance.GetString(
                            StringNames.FreeChatLinkWarning,
                            new Il2CppReferenceArray<Il2CppObject>(0)));
                }
                catch
                {
                }

                ClearChatInput(__instance);
                return false;
            }

            ModdedOriginalChatManager.SendAll(text);

            ClearChatInput(__instance);

            return false;
        }
        catch (Exception ex)
        {
            try
            {
                BMLogger.Warn($"[ModdedOriginalChat] SendChat patch failed: {ex}", "ModdedChat");
            }
            catch
            {
            }

            return false;
        }
    }

    private static void ClearChatInput(ChatController chat)
    {
        try
        {
            chat.freeChatField?.Clear();
        }
        catch
        {
        }

        try
        {
            chat.quickChatMenu?.Clear();
        }
        catch
        {
        }

        try
        {
            chat.quickChatField?.Clear();
        }
        catch
        {
        }

        try
        {
            AccessTools.Method(typeof(ChatController), "UpdateChatMode")?.Invoke(chat, null);
        }
        catch
        {
        }
    }
}