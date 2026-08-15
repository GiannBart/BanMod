//credits and licenses in the resources folder
using System;
using System.Collections.Generic;
using HarmonyLib;
using PowerTools;
using UnityEngine;

namespace BanMod.Modules.CustomHats.Patches
{
    [HarmonyPatch(typeof(HatParent))]
    internal static class HatParentPatches
    {
        private static readonly Dictionary<int, ClimbState> ClimbStates = new Dictionary<int, ClimbState>();
        public static readonly Dictionary<int, int> ActiveHatColors = new Dictionary<int, int>();
        private const float CustomHatOffsetX = 0f;
        private const float CustomHatOffsetY = 0f;
        private const float MirroredCustomHatOffsetX = 0f;

        private static readonly Dictionary<int, Vector3> CustomHatBaseLayerPositions = new Dictionary<int, Vector3>();

        private const float MovementThreshold = 0.00008f;
        private const float StopGraceSeconds = 0.18f;

        [HarmonyPatch(nameof(HatParent.SetHat), typeof(HatData), typeof(int))]
        [HarmonyPrefix]
        private static bool SetHatPrefix(HatParent __instance, HatData hat, int color)
        {
            try
            {
                if (hat == null || !CustomHatManager.TryGetViewData(hat, out HatViewData viewData))
                    return true;

                ForceCustomRender(__instance, hat, viewData, color, true);
                return false;
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] HatParent.SetHat custom prefix failed: " + ex);
                return true;
            }
        }

        [HarmonyPatch(nameof(HatParent.SetIdleAnim), typeof(int))]
        [HarmonyPrefix]
        private static bool SetIdleAnimPrefix(HatParent __instance, int colorId)
        {
            try
            {
                HatData hat = __instance.Hat;
                if (hat == null || !CustomHatManager.TryGetViewData(hat, out HatViewData viewData))
                    return true;

                ClearClimb(__instance);
                ForceCustomRender(__instance, hat, viewData, colorId, true);
                __instance.transform.SetLocalZ(0f);
                return false;
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] HatParent.SetIdleAnim custom prefix failed: " + ex);
                return true;
            }
        }

        [HarmonyPatch(nameof(HatParent.SetFloorAnim))]
        [HarmonyPrefix]
        private static bool SetFloorAnimPrefix(HatParent __instance)
        {
            try
            {
                HatData hat = __instance.Hat;
                if (hat == null || !CustomHatManager.TryGetViewData(hat, out HatViewData viewData))
                    return true;

                int color = ActiveHatColors.ContainsKey(__instance.GetInstanceID()) ? ActiveHatColors[__instance.GetInstanceID()] : 0;
                viewData = CustomHatManager.GetViewDataForColor(hat.ProdId, viewData, color);

                ClearClimb(__instance);

                if (__instance.BackLayer != null)
                {
                    __instance.BackLayer.enabled = false;
                    __instance.BackLayer.sprite = null;
                    __instance.BackLayer.flipX = false;
                }

                if (__instance.FrontLayer != null)
                {
                    __instance.FrontLayer.enabled = true;
                    __instance.FrontLayer.flipX = false;
                    __instance.FrontLayer.sprite = viewData.FloorImage != null ? viewData.FloorImage : viewData.MainImage;
                }

                ApplyCustomHatOffset(__instance);
                return false;
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] HatParent.SetFloorAnim custom prefix failed: " + ex);
                return true;
            }
        }

        [HarmonyPatch(nameof(HatParent.SetClimbAnim))]
        [HarmonyPrefix]
        private static bool SetClimbAnimPrefix(HatParent __instance)
        {
            try
            {
                HatData hat = __instance.Hat;
                if (hat == null || !CustomHatManager.TryGetViewData(hat, out HatViewData viewData))
                    return true;

                int color = ActiveHatColors.ContainsKey(__instance.GetInstanceID()) ? ActiveHatColors[__instance.GetInstanceID()] : 0;
                viewData = CustomHatManager.GetViewDataForColor(hat.ProdId, viewData, color);

                MarkClimb(__instance);

                Vector3 pos = __instance.transform.localPosition;
                __instance.transform.localPosition = new Vector3(pos.x, pos.y, -0.02f);

                ApplyClimbSprite(__instance, viewData);
                ApplyCustomHatOffset(__instance);

                SpriteAnimNodeSync sync = __instance.SpriteSyncNode ?? __instance.GetComponent<SpriteAnimNodeSync>();
                if (sync)
                    sync.NodeId = 0;

                return false;
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] HatParent.SetClimbAnim custom prefix failed: " + ex);
                return true;
            }
        }

        [HarmonyPatch(nameof(HatParent.LateUpdate))]
        [HarmonyPostfix]
        private static void LateUpdatePostfix(HatParent __instance)
        {
            try
            {
                if (__instance == null || __instance.Parent == null || !__instance.HasHat())
                    return;

                HatData hat = __instance.Hat;
                if (hat == null || !CustomHatManager.TryGetViewData(hat, out HatViewData viewData))
                    return;

                if (__instance.FrontLayer == null || __instance.BackLayer == null)
                    return;
                if (!CustomHatManager.IsCustomHat(__instance.Hat))
                    return;
                if ((__instance.FrontLayer.sprite == null && __instance.BackLayer.sprite == null))
                {
                    PopulateCustomFromViewData(__instance, hat, viewData);
                }
                int id = __instance.GetInstanceID();

                int colorId = ActiveHatColors.ContainsKey(id) ? ActiveHatColors[id] : 0;
                viewData = CustomHatManager.GetViewDataForColor(hat.ProdId, viewData, colorId);

                if (ClimbStates.TryGetValue(id, out ClimbState climb))
                {
                    Vector3 currentPos = __instance.transform.position;
                    float deltaY = Mathf.Abs(currentPos.y - climb.LastWorldPosition.y);
                    float deltaX = Mathf.Abs(currentPos.x - climb.LastWorldPosition.x);

                    if (deltaY > MovementThreshold || deltaX > MovementThreshold)
                    {
                        climb.LastMovementTime = Time.time;
                        climb.LastWorldPosition = currentPos;
                        ClimbStates[id] = climb;
                    }

                    bool stillMoving = Time.time - climb.LastMovementTime <= StopGraceSeconds;

                    if (stillMoving)
                    {
                        ApplyClimbSprite(__instance, viewData);
                        ApplyCustomHatOffset(__instance);

                        SpriteAnimNodeSync sync = __instance.SpriteSyncNode ?? __instance.GetComponent<SpriteAnimNodeSync>();
                        if (sync)
                            sync.NodeId = 0;
                        return;
                    }

                    ClearClimb(__instance);
                    PopulateCustomFromViewData(__instance, hat, viewData);
                }

                bool left = __instance.Parent.flipX;
                ApplyDirectionalSprites(__instance, hat, viewData, left);
                ApplyCustomHatOffset(__instance);
            }
            catch
            {
            }
        }

        public static void ForceCustomRender(HatParent parent, HatData hat, HatViewData viewData, int color)
        {
            ForceCustomRender(parent, hat, viewData, color, false);
        }

        public static void ForceCustomRender(HatParent parent, HatData hat, HatViewData viewData, int color, bool forcePopulate)
        {
            if (parent == null || hat == null || viewData == null)
                return;

            ActiveHatColors[parent.GetInstanceID()] = color;

            viewData = CustomHatManager.EnsureFullViewData(hat.ProdId, viewData);
            viewData = CustomHatManager.GetViewDataForColor(hat.ProdId, viewData, color);
            viewData = CustomHatManager.GetAdaptiveViewData(hat.ProdId, viewData, color);

            bool changedHat = hat != parent.Hat;

            if (changedHat)
            {
                if (parent.BackLayer != null) parent.BackLayer.sprite = null;
                if (parent.FrontLayer != null) parent.FrontLayer.sprite = null;
            }

            parent.Hat = hat;
            parent.SetMaterialColor(color);

            bool needsPopulate = forcePopulate || changedHat;

            if (parent.FrontLayer == null || parent.BackLayer == null)
                needsPopulate = true;
            else if (parent.FrontLayer.sprite == null && parent.BackLayer.sprite == null)
                needsPopulate = true;

            if (needsPopulate)
                PopulateCustomFromViewData(parent, hat, viewData);

            if (parent.Parent != null)
                ApplyDirectionalSprites(parent, hat, viewData, parent.Parent.flipX);

            ApplyCustomHatOffset(parent);
        }

        public static void ForceSticky(HatParent parent, HatData hat, HatViewData viewData, int color)
        {
            ForceCustomRender(parent, hat, viewData, color, true);
        }

        public static void ClearSticky(HatParent parent)
        {
            ClearCustomRender(parent);
        }

        public static void ClearAllSticky() { }

        public static void ClearCustomRender(HatParent parent)
        {
            try
            {
                if (parent == null)
                    return;

                ClearClimb(parent);
                ActiveHatColors.Remove(parent.GetInstanceID());

                if (parent.Hat != null && CustomHatManager.IsCustomHat(parent.Hat))
                {
                    parent.Hat = null;

                    if (parent.FrontLayer != null)
                    {
                        parent.FrontLayer.enabled = true;
                        parent.FrontLayer.sprite = null;
                        parent.FrontLayer.flipX = false;
                    }

                    if (parent.BackLayer != null)
                    {
                        parent.BackLayer.enabled = true;
                        parent.BackLayer.sprite = null;
                        parent.BackLayer.flipX = false;
                    }

                    int id = parent.GetInstanceID();
                    if (CustomHatBaseLayerPositions.ContainsKey(id))
                    {
                        CustomHatBaseLayerPositions.Remove(id);
                    }
                }
            }
            catch
            {
            }
        }

        public static void ApplyCustomHat(HatParent parent, HatData hat, HatViewData viewData, int color)
        {
            ForceCustomRender(parent, hat, viewData, color, true);
        }

        private static void PopulateCustomFromViewData(HatParent parent, HatData hat, HatViewData viewData)
        {
            if (parent == null || hat == null || viewData == null)
                return;

            SpriteAnimNodeSync sync = parent.SpriteSyncNode ?? parent.GetComponent<SpriteAnimNodeSync>();
            if (sync)
                sync.NodeId = hat.NoBounce ? 1 : 0;

            if (hat.InFront)
            {
                if (parent.BackLayer != null)
                {
                    parent.BackLayer.enabled = false;
                    parent.BackLayer.sprite = null;
                    parent.BackLayer.flipX = false;
                }

                if (parent.FrontLayer != null)
                {
                    parent.FrontLayer.enabled = true;
                    parent.FrontLayer.flipX = false;
                    parent.FrontLayer.sprite = viewData.MainImage;
                }
            }
            else if (viewData.BackImage != null)
            {
                if (parent.BackLayer != null)
                {
                    parent.BackLayer.enabled = true;
                    parent.BackLayer.flipX = false;
                    parent.BackLayer.sprite = viewData.BackImage;
                }

                if (parent.FrontLayer != null)
                {
                    parent.FrontLayer.enabled = true;
                    parent.FrontLayer.flipX = false;
                    parent.FrontLayer.sprite = viewData.MainImage;
                }
            }
            else
            {
                if (parent.BackLayer != null)
                {
                    parent.BackLayer.enabled = true;
                    parent.BackLayer.flipX = false;
                    parent.BackLayer.sprite = viewData.MainImage;
                }

                if (parent.FrontLayer != null)
                {
                    parent.FrontLayer.enabled = false;
                    parent.FrontLayer.sprite = null;
                    parent.FrontLayer.flipX = false;
                }
            }

            try
            {
                if (parent.HideHat())
                {
                    if (parent.FrontLayer != null) parent.FrontLayer.enabled = false;
                    if (parent.BackLayer != null) parent.BackLayer.enabled = false;
                }
            }
            catch { }
        }

        private static void ApplyDirectionalSprites(HatParent parent, HatData hat, HatViewData viewData, bool left)
        {
            if (parent == null || hat == null || viewData == null)
                return;

            if (parent.FrontLayer != null &&
                (parent.FrontLayer.sprite == viewData.ClimbImage ||
                 parent.FrontLayer.sprite == viewData.LeftClimbImage ||
                 parent.FrontLayer.sprite == viewData.FloorImage ||
                 parent.FrontLayer.sprite == viewData.LeftFloorImage))
            {
                if (!ClimbStates.ContainsKey(parent.GetInstanceID()))
                    PopulateCustomFromViewData(parent, hat, viewData);
                else
                    return;
            }

            bool hasLeftMain = viewData.LeftMainImage != null;
            bool hasLeftBack = viewData.LeftBackImage != null;

            Sprite main = left && hasLeftMain ? viewData.LeftMainImage : viewData.MainImage;
            Sprite back = left && hasLeftBack ? viewData.LeftBackImage : viewData.BackImage;

            bool mirrorMain = left && !hasLeftMain;
            bool mirrorBack = left && !hasLeftBack;

            if (hat.InFront)
            {
                if (parent.BackLayer != null)
                {
                    parent.BackLayer.enabled = false;
                    parent.BackLayer.sprite = null;
                    parent.BackLayer.flipX = false;
                }

                if (parent.FrontLayer != null)
                {
                    parent.FrontLayer.enabled = true;
                    parent.FrontLayer.sprite = main;
                    parent.FrontLayer.flipX = mirrorMain;
                }
            }
            else if (viewData.BackImage != null)
            {
                if (parent.BackLayer != null)
                {
                    parent.BackLayer.enabled = true;
                    parent.BackLayer.sprite = back;
                    parent.BackLayer.flipX = mirrorBack;
                }

                if (parent.FrontLayer != null)
                {
                    parent.FrontLayer.enabled = true;
                    parent.FrontLayer.sprite = main;
                    parent.FrontLayer.flipX = mirrorMain;
                }
            }
            else
            {
                if (parent.FrontLayer != null)
                {
                    parent.FrontLayer.enabled = false;
                    parent.FrontLayer.sprite = null;
                    parent.FrontLayer.flipX = false;
                }

                if (parent.BackLayer != null)
                {
                    parent.BackLayer.enabled = true;
                    parent.BackLayer.sprite = main;
                    parent.BackLayer.flipX = mirrorMain;
                }
            }
        }

        private static void ApplyClimbSprite(HatParent parent, HatViewData viewData)
        {
            if (parent == null || viewData == null)
                return;

            if (parent.BackLayer != null)
            {
                parent.BackLayer.enabled = false;
                parent.BackLayer.sprite = null;
                parent.BackLayer.flipX = false;
            }

            if (parent.FrontLayer != null)
            {
                parent.FrontLayer.enabled = true;
                parent.FrontLayer.flipX = false;
                parent.FrontLayer.sprite = viewData.ClimbImage != null ? viewData.ClimbImage : (viewData.BackImage != null ? viewData.BackImage : viewData.MainImage);
            }
        }

        private static void MarkClimb(HatParent parent)
        {
            if (parent == null)
                return;

            int id = parent.GetInstanceID();
            Vector3 pos = parent.transform.position;

            ClimbStates[id] = new ClimbState
            {
                LastWorldPosition = pos,
                LastMovementTime = Time.time
            };
        }

        private static void ClearClimb(HatParent parent)
        {
            if (parent == null)
                return;

            ClimbStates.Remove(parent.GetInstanceID());
        }


        private static void ApplyCustomHatOffset(HatParent parent)
        {
            try
            {
                if (parent == null)
                    return;

                ApplyOffsetToLayer(parent.FrontLayer);
                ApplyOffsetToLayer(parent.BackLayer);
            }
            catch
            {
            }
        }

        private static void ApplyOffsetToLayer(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.transform == null)
                return;

            int id = renderer.GetInstanceID();

            if (!CustomHatBaseLayerPositions.TryGetValue(id, out Vector3 basePosition))
            {
                basePosition = renderer.transform.localPosition;
                CustomHatBaseLayerPositions[id] = basePosition;
            }

            float xOffset = CustomHatOffsetX;

            if (renderer.flipX)
                xOffset += MirroredCustomHatOffsetX;

            renderer.transform.localPosition = new Vector3(
                basePosition.x + xOffset,
                basePosition.y + CustomHatOffsetY,
                basePosition.z
            );
        }

        private struct ClimbState
        {
            public Vector3 LastWorldPosition;
            public float LastMovementTime;
        }
    }
}
