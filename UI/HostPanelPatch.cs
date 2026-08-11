//credits and licenses in the resources folder
using BanMod;
using HarmonyLib;
using TMPro;
using UnityEngine;

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
public static class GameStartManagerLastMatchButtonPatch_Start
{
    public static void Postfix(GameStartManager __instance)
    {
        if (BanMod.BanMod.IsBanModDisabled) return;

        GameStartManagerLastMatchButtonPatchHelper.Apply(__instance);
    }
}

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
public static class GameStartManagerLastMatchButtonPatch_Update
{
    public static void Postfix(GameStartManager __instance)
    {
        if (BanMod.BanMod.IsBanModDisabled) return;

        GameStartManagerLastMatchButtonPatchHelper.Apply(__instance);
    }
}

internal static class GameStartManagerLastMatchButtonPatchHelper
{
    private const string ButtonName = "BanMod_LastMatchResultButton";
    private static readonly Color inactiveButtonColor = new(0f, 0.647f, 1f, 1f);
    private static readonly Color activeButtonColor = new(0f, 0.847f, 1f, 1f);
    private static readonly Color TextColor = Color.black;

    private static readonly Vector3 FixedLocalPosition = new(-0.07144928f, 0.49669075f, 6.556511E-07f);
    private static readonly Vector3 FixedLocalScale = new(1.0000005f, 0.42999995f, 1f);
    public static void Apply(GameStartManager gsm)
    {
        if (gsm == null)
            return;

        HideOriginalGameSettingsLabel(gsm);

        PassiveButton sourceButton = GetSourceButton(gsm);
        if (sourceButton == null || sourceButton.gameObject == null || sourceButton.transform.parent == null)
            return;

        Transform parent = sourceButton.transform.parent;
        Transform existing = FindChildByName(parent, ButtonName);

        bool hasData = PreviousMatchPopupTracker.LastSnapshot != null;

        if (!hasData)
        {
            if (existing != null)
                existing.gameObject.SetActive(false);
            return;
        }

        if (existing == null)
            existing = CreateCloneButton(sourceButton);

        if (existing == null)
            return;

        RefreshCloneButton(existing.gameObject);
    }

    private static void HideOriginalGameSettingsLabel(GameStartManager gsm)
    {
        TextMeshPro label = FindOriginalGameSettingsLabel(gsm);
        if (label == null || label.gameObject == null)
            return;

        label.gameObject.SetActive(false);
    }

    private static TextMeshPro FindOriginalGameSettingsLabel(GameStartManager gsm)
    {
        if (gsm == null)
            return null;

        TextMeshPro[] texts = gsm.GetComponentsInChildren<TextMeshPro>(true);
        if (texts == null || texts.Length == 0)
            return null;

        string expected = DestroyableSingleton<TranslationController>.Instance
            .GetString(StringNames.GameSettingsLabel)
            .Trim()
            .ToLowerInvariant();

        foreach (var tmp in texts)
        {
            if (tmp == null || tmp.gameObject == null)
                continue;

            string text = (tmp.text ?? "").Trim().ToLowerInvariant();
            string parsed = (tmp.GetParsedText() ?? "").Trim().ToLowerInvariant();

            if (text == expected || parsed == expected)
                return tmp;
        }

        return null;
    }
    private static PassiveButton GetSourceButton(GameStartManager gsm)
    {
        if (gsm.HostViewButton != null && gsm.HostViewButton.gameObject != null)
            return gsm.HostViewButton;

        if (gsm.EditButton != null && gsm.EditButton.gameObject != null)
            return gsm.EditButton;

        return null;
    }
    private static Transform CreateCloneButton(PassiveButton sourceButton)
    {
        if (sourceButton == null || sourceButton.gameObject == null || sourceButton.transform.parent == null)
            return null;

        GameObject clone = UnityEngine.Object.Instantiate(sourceButton.gameObject, sourceButton.transform.parent);
        clone.name = ButtonName;

        StripChildInteractivity(clone);
        ApplyButtonVisuals(clone);

        PassiveButton rootButton = clone.GetComponent<PassiveButton>();
        if (rootButton == null)
            rootButton = clone.AddComponent<PassiveButton>();

        BoxCollider2D rootCollider = clone.GetComponent<BoxCollider2D>();
        if (rootCollider == null)
            rootCollider = clone.AddComponent<BoxCollider2D>();

        rootButton.enabled = true;

        rootButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        rootButton.OnClick.RemoveAllListeners();
        rootButton.OnClick.AddListener((System.Action)(() =>
        {
            if (PreviousMatchPopupTracker.LastSnapshot == null)
                return;

            if (PreviousMatchSummaryUi.Instance == null || !PreviousMatchSummaryUi.Instance.showMenu)
                PreviousMatchSummaryUi.ShowMenu();
            else
                PreviousMatchSummaryUi.Instance.CloseMenu();
        }));

        rootButton.OnMouseOver = new UnityEngine.Events.UnityEvent();
        rootButton.OnMouseOver.RemoveAllListeners();
        rootButton.OnMouseOver.AddListener((System.Action)(() =>
        {
            TextMeshPro text = FindText(clone);
            if (text != null)
                text.color = TextColor;
        }));

        rootButton.OnMouseOut = new UnityEngine.Events.UnityEvent();
        rootButton.OnMouseOut.RemoveAllListeners();
        rootButton.OnMouseOut.AddListener((System.Action)(() =>
        {
            TextMeshPro text = FindText(clone);
            if (text != null)
                text.color = TextColor;
        }));

        return clone.transform;
    }

    private static void RefreshCloneButton(GameObject clone)
    {
        if (clone == null)
            return;

        clone.SetActive(true);

        Transform cloneTransform = clone.transform;
        cloneTransform.localPosition = FixedLocalPosition;
        cloneTransform.localScale = FixedLocalScale;

        ApplyButtonVisuals(clone);

        PassiveButton button = clone.GetComponent<PassiveButton>();
        if (button != null)
            button.enabled = true;

        TextMeshPro text = FindText(clone);
        if (text != null)
        {
            text.text = "<b>MATCH SUMMARY</b>";
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.color = TextColor;
            text.raycastTarget = false;
        }

        BoxCollider2D collider = clone.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = clone.AddComponent<BoxCollider2D>();

        UpdateRootColliderFromText(clone, text, collider);
    }

    private static void ApplyButtonVisuals(GameObject clone)
    {
        if (clone == null)
            return;

        PassiveButton rootButton = clone.GetComponent<PassiveButton>();
        if (rootButton == null)
            return;

        rootButton.activeTextColor = Color.black;
        rootButton.inactiveTextColor = Color.black;

        if (rootButton.inactiveSprites != null)
        {
            SpriteRenderer inactiveSr = rootButton.inactiveSprites.GetComponent<SpriteRenderer>();
            if (inactiveSr != null)
                inactiveSr.color = new Color(0f, 0.647f, 1f, 1f);

            Transform shine = rootButton.inactiveSprites.transform.Find("Shine");
            if (shine != null)
            {
                SpriteRenderer shineSr = shine.GetComponent<SpriteRenderer>();
                if (shineSr != null)
                    shineSr.color = new Color(0f, 1f, 1f, 0.5f);
            }
        }

        if (rootButton.activeSprites != null)
        {
            SpriteRenderer activeSr = rootButton.activeSprites.GetComponent<SpriteRenderer>();
            if (activeSr != null)
                activeSr.color = new Color(0f, 0.847f, 1f, 1f);
        }
    }

    private static void StripChildInteractivity(GameObject clone)
    {
        if (clone == null)
            return;

        Transform root = clone.transform;

        var allColliders = clone.GetComponentsInChildren<Collider2D>(true);
        foreach (var c in allColliders)
        {
            if (c == null)
                continue;

            if (c.transform != root)
                c.enabled = false;
        }

        var allButtons = clone.GetComponentsInChildren<PassiveButton>(true);
        foreach (var b in allButtons)
        {
            if (b == null)
                continue;

            if (b.transform != root)
                b.enabled = false;
        }
    }

    private static void UpdateRootColliderFromText(GameObject go, TextMeshPro text, BoxCollider2D collider)
    {
        if (go == null || text == null || collider == null)
            return;

        text.ForceMeshUpdate();
        Vector2 size = text.GetRenderedValues(false);

        Vector3 worldPos = text.transform.position;
        Vector3 localPos = go.transform.InverseTransformPoint(worldPos);

        collider.size = new Vector2(
            Mathf.Max(size.x + 0.25f, 2.2f),
            Mathf.Max(size.y + 0.16f, 0.34f)
        );
        collider.offset = new Vector2(localPos.x, localPos.y);
        collider.enabled = true;
        collider.isTrigger = true;
    }

    private static TextMeshPro FindText(GameObject go)
    {
        if (go == null)
            return null;

        var texts = go.GetComponentsInChildren<TextMeshPro>(true);
        if (texts == null || texts.Length == 0)
            return null;

        foreach (var t in texts)
        {
            if (t != null)
                return t;
        }

        return null;
    }

    private static Transform FindChildByName(Transform parent, string name)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.name == name)
                return child;
        }

        return null;
    }
}
