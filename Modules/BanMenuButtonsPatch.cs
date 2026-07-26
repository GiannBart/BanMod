//credits and licenses in the resources folder
using HarmonyLib;
using System.Linq;
using UnityEngine;
using static BanMod.ChatCommands;
using static BanMod.Translator;
using static BanMod.Utils;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;

namespace BanMod
{
    [HarmonyPatch(typeof(BanMenu), nameof(BanMenu.Show))]
    public static class BanMenu_Show_Patch
    {
        static void Postfix(BanMenu __instance)
        {
            if (BanMod.IsBanModDisabled) return;
            BanMenuButtonsPatch patch = __instance.GetComponent<BanMenuButtonsPatch>();
            if (patch == null)
            {
                patch = __instance.gameObject.AddComponent<BanMenuButtonsPatch>();
                patch.MenuController1 = BanMod.msgMenu;
                patch.MenuController2 = BanMod.hostControl;
                patch.Init(__instance);
            }
        }
    }

    public class BanMenuButtonsPatch : MonoBehaviour
    {
        public SpriteRenderer CustomButton3Prefab;
        public SpriteRenderer CustomButton4Prefab;
        public SpriteRenderer CustomButton5Prefab; 

        private SpriteRenderer customButton3Instance;
        private SpriteRenderer customButton4Instance;
        private SpriteRenderer customButton5Instance;

        private BanMenu banMenu;
        private SpriteRenderer reportButtonForPosition;

        public float desiredOffsetX_Button3 = 0.40f;
        public float desiredOffsetY_Button3 = -2.05f;

        public float desiredOffsetX_Button4 = 1.23f;
        public float desiredOffsetY_Button4 = -2.05f;

        public float desiredOffsetX_Button5 = -0.43f;
        public float desiredOffsetY_Button5 = -2.05f;

        public MsgMenu MenuController1;
        public HostControl MenuController2;

        public void Init(BanMenu targetMenu)
        {
            banMenu = targetMenu;
            CustomButton3Prefab = targetMenu.BanButton;
            CustomButton4Prefab = targetMenu.KickButton;
            CustomButton5Prefab = targetMenu.ReportButton;

            reportButtonForPosition = targetMenu.ReportButton;
        }

        void Update()
        {
            if (banMenu == null) return;

            bool menuIsActive = banMenu.gameObject.activeInHierarchy;
            bool buttonsVisible = Options.buttonvisibile.GetBool();

            if (menuIsActive && buttonsVisible)
            {
                if (customButton3Instance == null || customButton4Instance == null || customButton5Instance == null)
                {
                    AddCustomButtons();
                }

                customButton3Instance?.gameObject.SetActive(true);
                customButton4Instance?.gameObject.SetActive(true);
                customButton5Instance?.gameObject.SetActive(true);

                RecalculateButtonPositions();
            }
            else
            {
                customButton3Instance?.gameObject.SetActive(false);
                customButton4Instance?.gameObject.SetActive(false);
                customButton5Instance?.gameObject.SetActive(false);

                if (!menuIsActive)
                {
                    if (customButton3Instance != null) Destroy(customButton3Instance.gameObject);
                    if (customButton4Instance != null) Destroy(customButton4Instance.gameObject);
                    if (customButton5Instance != null) Destroy(customButton5Instance.gameObject);
                    customButton3Instance = customButton4Instance = customButton5Instance = null;
                }
            }
        }

        private void AddCustomButtons()
        {
            var btn3 = CreateButton(CustomButton3Prefab, "CustomButton3_Settings",
                "BanMod.Resources.image.SettingsIcon.png", Color.blue, 3, false);
            customButton3Instance = btn3;

            var btn4 = CreateButton(CustomButton4Prefab, "CustomButton4_Info",
                "BanMod.Resources.image.InfoIcon.png", Color.magenta, 4, false);
            customButton4Instance = btn4;

            var btn5 = CreateButton(CustomButton3Prefab, "CustomButton5_Options",
                "BanMod.Resources.image.OptionsIcon.png", Color.red, 5, false);
            customButton5Instance = btn5;
        }

        private SpriteRenderer CreateButton(SpriteRenderer prefab, string name, string iconPath, Color fallbackColor, int id, bool affectsSelection)
        {
            if (prefab == null)
            {
                Debug.LogError($"[BanMod] Prefab per {name} è NULL.");
                return null;
            }

            var go = Object.Instantiate(prefab.gameObject, banMenu.transform);
            go.name = name;

            if (go.TryGetComponent<ButtonRolloverHandler>(out var bh)) Object.DestroyImmediate(bh);
            if (go.TryGetComponent<PassiveButton>(out var pb)) Object.DestroyImmediate(pb);
            if (go.TryGetComponent<BanButton>(out var bb)) Object.DestroyImmediate(bb);
            if (go.TryGetComponent<BoxCollider2D>(out var col)) Object.DestroyImmediate(col);

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingOrder = prefab.sortingOrder + 100;
            sr.sortingLayerName = prefab.sortingLayerName;

            go.transform.localScale = new Vector3(0.88f, 2.5f, 1.5f);

            var icon = Utils.LoadSprite(iconPath, 150f);
            sr.sprite = icon ?? sr.sprite;
            sr.color = icon != null ? Color.white : fallbackColor;

            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.5f, 0.5f);
            collider.isTrigger = true;

            var handler = go.AddComponent<CustomButtonHandler>();
            handler.ButtonId = id;
            handler.Mod = this;
            handler.AffectsSelection = affectsSelection;
            handler.normalColor = sr.color;
            handler.hoverColor = sr.color;

            return sr;
        }



        public void RecalculateButtonPositions()
        {
            if (reportButtonForPosition == null) return;

            Vector3 basePos = reportButtonForPosition.transform.position;
            Vector3 offset = basePos - banMenu.transform.position;

            if (customButton3Instance != null)
                customButton3Instance.transform.localPosition = new Vector3(offset.x + desiredOffsetX_Button3, offset.y + desiredOffsetY_Button3, -100f);
            if (customButton4Instance != null)
                customButton4Instance.transform.localPosition = new Vector3(offset.x + desiredOffsetX_Button4, offset.y + desiredOffsetY_Button4, -100f);
            if (customButton5Instance != null)
                customButton5Instance.transform.localPosition = new Vector3(offset.x + desiredOffsetX_Button5, offset.y + desiredOffsetY_Button5, -100f);
        }

        public void OnCustomButtonClicked(int id)
        {
            switch (id)
            {
                case 3: 
                    if (MenuController1 != null)
                    {
                        if (MenuController1.IsOpen()) MenuController1.CloseMenu();
                        else
                        {
                            MenuController1.OpenMenu();
                            MenuController2?.CloseMenu();
                        }
                    }
                    break;

                case 4: 
                    if (MenuController2 != null)
                    {
                        if (MenuController2.IsOpen()) MenuController2.CloseMenu();
                        else
                        {
                            MenuController2.OpenMenu();
                            MenuController1?.CloseMenu();
                        }
                    }
                    break;

                case 5:
                    if (AmongUsClient.Instance != null &&
                              AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.NotJoined)
                    {
                        MenuController1?.CloseMenu();
                        MenuController2?.CloseMenu();

                        PlayerUI.Instance.OpenPPM();
                    }
                    break;
            }
        }

        public class CustomButtonHandler : MonoBehaviour
        {
            public int ButtonId;
            public BanMenuButtonsPatch Mod;
            public Color normalColor = Color.white;
            public Color hoverColor = Color.white;
            public bool AffectsSelection = false;

            private SpriteRenderer sr;
            private Vector3 baseScale;
            private Vector3 hoverScale;

            void Start()
            {
                sr = GetComponent<SpriteRenderer>();
                baseScale = transform.localScale;
                hoverScale = baseScale * 1.1f;
            }

            void Update()
            {
                var wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var hit = Physics2D.OverlapPoint(wp);

                bool hover = (hit != null && hit.gameObject == gameObject);
                transform.localScale = hover ? hoverScale : baseScale;

                if (hover && Input.GetMouseButtonDown(0))
                {
                    Mod?.OnCustomButtonClicked(ButtonId);
                }
            }
        }
    }
}
