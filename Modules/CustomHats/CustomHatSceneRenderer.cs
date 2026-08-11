//credits and licenses in the resources folder
using System;
using System.Collections.Generic;
using AmongUs.Data;
using UnityEngine;
using BanMod.Modules.CustomHats.Patches;

namespace BanMod.Modules.CustomHats
{
    public class CustomHatSceneRenderer : MonoBehaviour
    {
        private float timer;

        private readonly Dictionary<int, string> appliedParentHatIds = new Dictionary<int, string>();
        private readonly Dictionary<int, int> appliedParentColors = new Dictionary<int, int>();

        public CustomHatSceneRenderer(IntPtr ptr) : base(ptr)
        {
        }

        public void LateUpdate()
        {
            try
            {
                timer += Time.deltaTime;

                if (timer < 0.10f)
                    return;

                timer = 0f;

                CustomHatSync.UpdateLocalFromCustomization();
                CustomHatSync.RefreshKnownPlayers();
                ApplyToAllPlayersIfNeeded();
                ApplyToInventoryTabPreviewIfNeeded();
                ApplyToEndGameScreensIfNeeded();
            }
            catch
            {
            }
        }

        private void ApplyToAllPlayersIfNeeded()
        {
            try
            {
                if (PlayerControl.AllPlayerControls == null)
                    return;

                for (int i = 0; i < PlayerControl.AllPlayerControls.Count; i++)
                {
                    PlayerControl player = PlayerControl.AllPlayerControls[i];
                    ApplyToPlayerIfNeeded(player);
                }
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] ApplyToAllPlayersIfNeeded failed: " + ex);
            }
        }

        private void ApplyToPlayerIfNeeded(PlayerControl player)
        {
            if (player == null)
                return;

            string hatId = GetCurrentPlayerHatId(player);

            if (string.IsNullOrEmpty(hatId) || !CustomHatManager.TryGetViewData(hatId, out HatViewData viewData))
            {
                ClearCustomRenderForPlayer(player);
                return;
            }

            HatData hat = FindCustomHat(hatId);
            if (hat == null)
            {
                ClearCustomRenderForPlayer(player);
                return;
            }

            int color = GetCurrentPlayerColorId(player);
            HatParent[] parents = player.GetComponentsInChildren<HatParent>(true);

            if (parents == null)
                return;

            for (int i = 0; i < parents.Length; i++)
            {
                HatParent parent = parents[i];
                if (parent == null)
                    continue;

                if (!NeedsApply(parent, hatId, color))
                    continue;

                viewData = CustomHatManager.GetAdaptiveViewData(
    hat.ProdId,
    viewData,
    color);

                HatParentPatches.ForceCustomRender(
                    parent,
                    hat,
                    viewData,
                    color);
                appliedParentHatIds[parent.GetInstanceID()] = hatId;
                appliedParentColors[parent.GetInstanceID()] = color;
            }
        }

        private void ClearCustomRenderForPlayer(PlayerControl player)
        {
            try
            {
                if (player == null)
                    return;

                HatParent[] parents = player.GetComponentsInChildren<HatParent>(true);
                if (parents == null)
                    return;

                for (int i = 0; i < parents.Length; i++)
                {
                    HatParent parent = parents[i];
                    if (parent == null)
                        continue;

                    int id = parent.GetInstanceID();
                    bool wasAppliedByCustomRenderer = appliedParentHatIds.ContainsKey(id);
                    bool currentlyCustom = parent.Hat != null && CustomHatManager.IsCustomHat(parent.Hat);

                    if (!wasAppliedByCustomRenderer && !currentlyCustom)
                        continue;

                    HatParentPatches.ClearCustomRender(parent);
                    appliedParentHatIds.Remove(id);
                    appliedParentColors.Remove(id);
                }
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] ClearCustomRenderForPlayer failed: " + ex);
            }
        }

        private void ApplyToInventoryTabPreviewIfNeeded()
        {
            string hatId = GetCurrentCustomizationHatId();
            if (string.IsNullOrEmpty(hatId))
                return;

            if (!CustomHatManager.TryGetViewData(hatId, out HatViewData viewData))
                return;

            HatData hat = FindCustomHat(hatId);
            if (hat == null)
                return;

            int color = GetCurrentCustomizationColorId();

            InventoryTab[] tabs = UnityEngine.Object.FindObjectsOfType<InventoryTab>(true);
            if (tabs == null)
                return;

            for (int i = 0; i < tabs.Length; i++)
            {
                InventoryTab tab = tabs[i];

                if (tab == null || tab.PlayerPreview == null)
                    continue;

                if (!tab.gameObject.activeInHierarchy)
                    continue;

                string path = GetPath(tab.transform);

                if (!path.Contains("PlayerCustomizationMenu") &&
                    !path.Contains("LobbyPlayerCustomizationMenu"))
                    continue;

                HatParent[] parents = tab.PlayerPreview.GetComponentsInChildren<HatParent>(true);
                for (int j = 0; j < parents.Length; j++)
                {
                    HatParent parent = parents[j];
                    if (parent == null)
                        continue;

                    if (!NeedsApply(parent, hatId, color))
                        continue;

                    HatViewData coloredView = CustomHatManager.GetViewDataForColor(hat.ProdId, viewData, color);
                    coloredView = CustomHatManager.GetAdaptiveViewData(hat.ProdId, coloredView, color);

                    HatParentPatches.ForceCustomRender(parent, hat, coloredView, color);
                    appliedParentHatIds[parent.GetInstanceID()] = hatId;
                    appliedParentColors[parent.GetInstanceID()] = color;
                }
            }
        }


        private void ApplyToEndGameScreensIfNeeded()
        {
            try
            {
                EndGameManager[] managers = UnityEngine.Object.FindObjectsOfType<EndGameManager>(true);
                if (managers == null || managers.Length == 0)
                    return;

                if (EndGameResult.CachedWinners == null)
                    return;

                List<object> winners = new List<object>();
                foreach (object winner in EndGameResult.CachedWinners)
                    winners.Add(winner);

                winners.Sort(delegate (object a, object b)
                {
                    int ai = IsCachedPlayerYouStatic(a) ? -1 : 0;
                    int bi = IsCachedPlayerYouStatic(b) ? -1 : 0;
                    return ai.CompareTo(bi);
                });

                for (int m = 0; m < managers.Length; m++)
                {
                    EndGameManager manager = managers[m];
                    if (manager == null || !manager.gameObject.activeInHierarchy)
                        continue;

                    PoolablePlayer[] poolables = manager.GetComponentsInChildren<PoolablePlayer>(true);
                    if (poolables == null || poolables.Length == 0)
                        continue;

                    int count = Math.Min(poolables.Length, winners.Count);
                    for (int i = 0; i < count; i++)
                        ApplyToEndGamePoolablePlayerIfNeeded(poolables[i], winners[i]);
                }
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] ApplyToEndGameScreensIfNeeded failed: " + ex);
            }
        }

        public static void ApplyToEndGamePoolablePlayerIfNeeded(PoolablePlayer poolable, object cachedPlayerData)
        {
            try
            {
                if (poolable == null)
                    return;

                int colorId = GetCachedPlayerColorId(cachedPlayerData, poolable.ColorId);

                if (!TryResolveEndGameRealHatId(cachedPlayerData, colorId, out string hatId) ||
                    string.IsNullOrEmpty(hatId) ||
                    !CustomHatManager.TryGetViewData(hatId, out HatViewData viewData))
                {
                    PoolablePlayerPatches.ClearAllHatParents(poolable);
                    return;
                }

                HatData hat = FindCustomHatStatic(hatId);
                if (hat == null)
                {
                    PoolablePlayerPatches.ClearAllHatParents(poolable);
                    return;
                }
                viewData = CustomHatManager.GetAdaptiveViewData(
    hat.ProdId,
    viewData,
    colorId);
                PoolablePlayerPatches.ApplyToAllHatParents(poolable, hat, viewData, colorId);
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] ApplyToEndGamePoolablePlayerIfNeeded failed: " + ex);
            }
        }

        private static bool TryResolveEndGameRealHatId(object cachedPlayerData, int colorId, out string hatId)
        {
            hatId = "";

            try
            {
                string playerName = GetCachedPlayerName(cachedPlayerData);
                bool isYou = IsCachedPlayerYou(cachedPlayerData);

                if (CustomHatSync.TryResolveCachedRealHatId(playerName, colorId, isYou, out hatId))
                    return true;

                if (GameData.Instance != null && GameData.Instance.AllPlayers != null)
                {
                    NetworkedPlayerInfo fallbackByColor = null;

                    foreach (NetworkedPlayerInfo info in GameData.Instance.AllPlayers)
                    {
                        if (info == null || info.Disconnected || info.DefaultOutfit == null)
                            continue;

                        bool colorMatches = info.DefaultOutfit.ColorId == colorId;
                        bool nameMatches = !string.IsNullOrEmpty(playerName) &&
                                           string.Equals(info.PlayerName, playerName, StringComparison.Ordinal);

                        if (nameMatches && colorMatches)
                            return CustomHatSync.TryResolveRealHatId(info, out hatId);

                        if (fallbackByColor == null && colorMatches)
                            fallbackByColor = info;
                    }

                    if (fallbackByColor != null)
                        return CustomHatSync.TryResolveRealHatId(fallbackByColor, out hatId);
                }

                if (IsCachedPlayerYou(cachedPlayerData) &&
                    PlayerControl.LocalPlayer != null &&
                    PlayerControl.LocalPlayer.Data != null)
                {
                    return CustomHatSync.TryResolveRealHatId(PlayerControl.LocalPlayer.Data, out hatId);
                }
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] TryResolveEndGameRealHatId failed: " + ex);
            }

            return false;
        }

        private static string GetCachedPlayerName(object cachedPlayerData)
        {
            try
            {
                if (cachedPlayerData == null)
                    return "";

                object value = GetCachedMemberValue(cachedPlayerData, "PlayerName");
                return value as string ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static int GetCachedPlayerColorId(object cachedPlayerData, int fallback)
        {
            try
            {
                if (cachedPlayerData == null)
                    return fallback;

                object value = GetCachedMemberValue(cachedPlayerData, "ColorId");
                if (value is int i)
                    return i;
                if (value is byte b)
                    return b;
            }
            catch
            {
            }

            return fallback;
        }

        private static bool IsCachedPlayerYouStatic(object cachedPlayerData)
        {
            return IsCachedPlayerYou(cachedPlayerData);
        }

        private static bool IsCachedPlayerYou(object cachedPlayerData)
        {
            try
            {
                if (cachedPlayerData == null)
                    return false;

                object value = GetCachedMemberValue(cachedPlayerData, "IsYou");
                return value is bool b && b;
            }
            catch
            {
                return false;
            }
        }


        private static object GetCachedMemberValue(object cachedPlayerData, string memberName)
        {
            try
            {
                if (cachedPlayerData == null || string.IsNullOrEmpty(memberName))
                    return null;

                Type type = cachedPlayerData.GetType();

                System.Reflection.PropertyInfo prop = type.GetProperty(memberName);
                if (prop != null)
                    return prop.GetValue(cachedPlayerData, null);

                System.Reflection.FieldInfo field = type.GetField(memberName);
                if (field != null)
                    return field.GetValue(cachedPlayerData);
            }
            catch
            {
            }

            return null;
        }

        private static HatData FindCustomHatStatic(string hatId)
        {
            for (int i = 0; i < CustomHatManager.RegisteredHats.Count; i++)
            {
                HatData candidate = CustomHatManager.RegisteredHats[i];
                if (candidate != null && candidate.ProdId == hatId)
                    return candidate;
            }

            return null;
        }

        private bool NeedsApply(HatParent parent, string hatId, int color)
        {
            try
            {
                if (parent == null)
                    return false;

                int id = parent.GetInstanceID();

                if (!appliedParentHatIds.TryGetValue(id, out string lastHatId))
                    return true;

                if (lastHatId != hatId)
                    return true;

                if (appliedParentColors.TryGetValue(id, out int lastColor) && lastColor != color)
                    return true;

                if (parent.Hat == null || parent.Hat.ProdId != hatId)
                    return true;

                if (parent.FrontLayer != null && parent.FrontLayer.sprite != null)
                    return false;

                if (parent.BackLayer != null && parent.BackLayer.sprite != null)
                    return false;

                return true;
            }
            catch
            {
                return true;
            }
        }

        private string GetCurrentCustomizationHatId()
        {
            try
            {
                if (DataManager.Player != null &&
                    DataManager.Player.Customization != null &&
                    !string.IsNullOrEmpty(DataManager.Player.Customization.Hat))
                {
                    return DataManager.Player.Customization.Hat;
                }
            }
            catch { }

            return "";
        }

        private int GetCurrentCustomizationColorId()
        {
            try
            {
                return (int)DataManager.Player.Customization.Color;
            }
            catch { }

            return 0;
        }

        private string GetCurrentPlayerHatId(PlayerControl player)
        {
            try
            {
                if (player != null && player.Data != null)
                {
                    if (CustomHatSync.TryResolveHatId(player.Data, out string syncedHatId))
                        return syncedHatId;

                    if (CustomHatSync.IsRenderingShapeshiftTarget(player.Data))
                        return "";
                }
            }
            catch { }

            if (IsLocalPlayer(player))
                return GetCurrentCustomizationHatId();

            return "";
        }

        private int GetCurrentPlayerColorId(PlayerControl player)
        {
            try
            {
                if (player != null &&
                    player.Data != null &&
                    CustomHatSync.TryResolveDisplayedColorId(player.Data, out int displayedColorId))
                {
                    return displayedColorId;
                }
            }
            catch { }

            if (IsLocalPlayer(player))
                return GetCurrentCustomizationColorId();

            return 0;
        }

        private bool IsLocalPlayer(PlayerControl player)
        {
            try
            {
                return player != null &&
                       PlayerControl.LocalPlayer != null &&
                       player.PlayerId == PlayerControl.LocalPlayer.PlayerId;
            }
            catch
            {
                return false;
            }
        }

        private HatData FindCustomHat(string hatId)
        {
            for (int i = 0; i < CustomHatManager.RegisteredHats.Count; i++)
            {
                HatData candidate = CustomHatManager.RegisteredHats[i];
                if (candidate != null && candidate.ProdId == hatId)
                    return candidate;
            }

            return null;
        }

        private string GetPath(Transform t)
        {
            try
            {
                if (t == null)
                    return "";

                string path = t.name;
                Transform current = t.parent;

                int guard = 0;
                while (current != null && guard < 40)
                {
                    path = current.name + "/" + path;
                    current = current.parent;
                    guard++;
                }

                return path;
            }
            catch
            {
                return "";
            }
        }
    }
}
