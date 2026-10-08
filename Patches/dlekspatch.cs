using HarmonyLib;
using UnityEngine;
using UnityEngine.AddressableAssets;
//thanks to Ehr for Image.png
namespace BanMod
{
    [HarmonyPatch]
    public static class DleksPickerPatch
    {
        public static bool DleksSelected;

        private static MapSelectButton _dleksButton;

        [HarmonyPatch(
            typeof(GameOptionsMapPicker),
            nameof(GameOptionsMapPicker.Initialize)
        )]
        [HarmonyPrefix]
        public static void InitializePrefix()
        {
            try
            {
                if (GameOptionsManager.Instance == null)
                    return;

                if (GameOptionsManager.Instance.CurrentGameOptions == null)
                    return;

                GameOptionsManager.Instance.currentNormalGameOptions.MapId = 0;
            }
            catch (System.Exception e)
            {
                BanMod.PluginLogger?.LogError(
                    $"[Dleks] InitializePrefix error: {e}"
                );
            }
        }

        [HarmonyPatch(
            typeof(GameOptionsMapPicker),
            nameof(GameOptionsMapPicker.Initialize)
        )]
        [HarmonyPostfix]
        public static void InitializePostfix(
            GameOptionsMapPicker __instance)
        {
            try
            {
                if (__instance == null ||
                    __instance.MapButtons == null)
                    return;

                MapSelectButton skeld = null;
                MapSelectButton mira = null;
                MapSelectButton polus = null;

                foreach (var button in __instance.MapButtons)
                {
                    if (button == null)
                        continue;

                    if (button.MapID == 0)
                        skeld = button;
                    else if (button.MapID == 1)
                        mira = button;
                    else if (button.MapID == 2)
                        polus = button;
                }

                if (skeld == null)
                    return;

                if (_dleksButton == null ||
                    _dleksButton.gameObject == null)
                {
                    _dleksButton = Object.Instantiate(
                        skeld,
                        skeld.transform.parent
                    );

                    _dleksButton.name = "DleksMapButton";
                    _dleksButton.MapID = 3;

                    Sprite dleksIcon = Utils.LoadSprite(
                        "BanMod.Resources.image.DleksBanner-Icon.png",
                        95f
                    );

                    if (dleksIcon != null)
                    {
                        var renderers =
                            _dleksButton.GetComponentsInChildren<SpriteRenderer>(
                                true
                            );

                        foreach (var renderer in renderers)
                        {
                            if (renderer == null)
                                continue;

                            if (renderer.sprite == null)
                                continue;

                            renderer.sprite = dleksIcon;
                            break;
                        }
                    }

                    if (mira != null && polus != null)
                    {
                        Vector3 miraPos =
                            mira.transform.localPosition;

                        Vector3 polusPos =
                            polus.transform.localPosition;

                        Vector3 step =
                            polusPos - miraPos;

                        _dleksButton.transform.localPosition =
                            polusPos;

                        foreach (var button in __instance.MapButtons)
                        {
                            if (button == null)
                                continue;

                            if (button.MapID == 2 ||
                                button.MapID == 4 ||
                                button.MapID == 5)
                            {
                                button.transform.localPosition += step;
                            }
                        }
                    }

                    __instance.MapButtons.Add(
                        _dleksButton
                    );
                }

                foreach (var button in __instance.MapButtons)
                {
                    if (button == null)
                        continue;

                    if (button == _dleksButton)
                        continue;

                    MapSelectButton vanillaButton = button;

                    vanillaButton.Button.OnClick.AddListener(
                        (System.Action)(() =>
                        {
                            DleksSelected = false;

                            if (_dleksButton != null &&
                                _dleksButton.gameObject != null)
                            {
                                _dleksButton.Button.SelectButton(false);
                            }

                            OptionSaver.Save();

                            BanMod.PluginLogger?.LogInfo(
                                $"[Dleks] Selected=False vanilla={vanillaButton.MapID}"
                            );
                        })
                    );
                }

                _dleksButton.Button.OnClick.RemoveAllListeners();

                _dleksButton.Button.OnClick.AddListener(
                    (System.Action)(() =>
                    {
                        try
                        {
                            __instance.SelectMap(0);

                            DleksSelected = true;

                            foreach (var button in __instance.MapButtons)
                            {
                                if (button == null)
                                    continue;

                                button.Button.SelectButton(false);
                            }

                            _dleksButton.Button.SelectButton(true);

                            ApplyDleksVisuals(__instance);

                            OptionSaver.Save();

                            BanMod.PluginLogger?.LogInfo(
                                "[Dleks] Selected=True MapId=0"
                            );
                        }
                        catch (System.Exception e)
                        {
                            BanMod.PluginLogger?.LogError(
                                $"[Dleks] Click error: {e}"
                            );
                        }
                    })
                );

                if (DleksSelected)
                {
                    foreach (var button in __instance.MapButtons)
                    {
                        if (button == null)
                            continue;

                        button.Button.SelectButton(false);
                    }

                    _dleksButton.Button.SelectButton(true);

                    ApplyDleksVisuals(__instance);

                    BanMod.PluginLogger?.LogInfo(
                        "[Dleks] Restored=True MapId=0"
                    );
                }
                else
                {
                    foreach (var button in __instance.MapButtons)
                    {
                        if (button == null)
                            continue;

                        button.Button.SelectButton(false);
                    }

                    skeld.Button.SelectButton(true);

                    BanMod.PluginLogger?.LogInfo(
                        "[Dleks] Restored=False Skeld MapId=0"
                    );
                }
            }
            catch (System.Exception e)
            {
                BanMod.PluginLogger?.LogError(
                    $"[Dleks] Picker error: {e}"
                );
            }
        }

        private static void ApplyDleksVisuals(
            GameOptionsMapPicker picker)
        {
            Sprite banner = Utils.LoadSprite(
                "BanMod.Resources.image.DleksBanner.png",
                100f
            );

            Sprite wordart = Utils.LoadSprite(
                "BanMod.Resources.image.DleksBanner-Wordart.png",
                160f
            );

            if (picker.MapImage != null &&
                banner != null)
            {
                picker.MapImage.sprite =
                    banner;
            }

            if (picker.MapName != null &&
                wordart != null)
            {
                picker.MapName.sprite =
                    wordart;
            }
        }
    }

    public static class DleksEhtController
    {
        private static AssetReference _skeld;
        private static AssetReference _dleks;
        private static bool _initialized;
        private static bool _swapped;

        private static bool Initialize()
        {
            if (_initialized)
                return true;

            if (AmongUsClient.Instance == null)
                return false;

            if (AmongUsClient.Instance.ShipPrefabs == null)
                return false;

            if (AmongUsClient.Instance.ShipPrefabs.Count <= 3)
                return false;

            _skeld =
                AmongUsClient.Instance.ShipPrefabs[0];

            _dleks =
                AmongUsClient.Instance.ShipPrefabs[3];

            _initialized = true;

            return true;
        }

        public static void Apply()
        {
            if (!Initialize())
                return;

            if (DleksPickerPatch.DleksSelected)
            {
                if (_swapped)
                    return;

                AmongUsClient.Instance.ShipPrefabs[0] =
                    _dleks;

                AmongUsClient.Instance.ShipPrefabs[3] =
                    _skeld;

                _swapped = true;

                BanMod.PluginLogger?.LogInfo(
                    "[Dleks eht] ShipPrefabs swapped"
                );
            }
            else
            {
                Restore();
            }
        }

        public static void Restore()
        {
            if (!_initialized ||
                !_swapped)
                return;

            if (AmongUsClient.Instance == null)
                return;

            AmongUsClient.Instance.ShipPrefabs[0] =
                _skeld;

            AmongUsClient.Instance.ShipPrefabs[3] =
                _dleks;

            _swapped = false;

            BanMod.PluginLogger?.LogInfo(
                "[Dleks eht] ShipPrefabs restored"
            );
        }
    }
}