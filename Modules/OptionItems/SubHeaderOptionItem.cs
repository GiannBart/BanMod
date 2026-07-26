// Sub-header interno per dividere le opzioni dentro lo stesso tab/categoria principale.
// Uso: SubHeaderOptionItem.Create("AutoStart", OptionCategory.Game);
using System;
using TMPro;
using UnityEngine;

namespace BanMod
{
    public sealed class SubHeaderOptionItem : OptionItem
    {
        private static int NextHeaderId = 980000;

        public SubHeaderOptionItem(int id, string name, OptionCategory category)
            : base(id, name, 0, category, true)
        {
            SetHeader(true);
            SetColor(new Color32(255, 204, 0, 255));
        }

        public static SubHeaderOptionItem Create(string name, OptionCategory category)
        {
            return new SubHeaderOptionItem(NextHeaderId++, name, category);
        }

        public static SubHeaderOptionItem Create(int id, string name, OptionCategory category)
        {
            return new SubHeaderOptionItem(id, name, category);
        }

        public override string GetString()
        {
            return string.Empty;
        }

        public override void Refresh()
        {
            base.Refresh();

            if (OptionBehaviour == null)
                return;

            try
            {
                if (OptionBehaviour.TitleText != null)
                {
                    OptionBehaviour.TitleText.gameObject.SetActive(true);
                    OptionBehaviour.TitleText.text = GetName();
                    OptionBehaviour.TitleText.alignment = TextAlignmentOptions.Center;
                    OptionBehaviour.TitleText.fontStyle = FontStyles.Bold;
                    OptionBehaviour.TitleText.color = NameColor;
                    OptionBehaviour.TitleText.fontSize = Math.Max(OptionBehaviour.TitleText.fontSize, 2.6f);
                }

                if (OptionBehaviour.ValueText != null)
                    OptionBehaviour.ValueText.gameObject.SetActive(false);

                HideRightControls();
            }
            catch { }
        }

        private void HideRightControls()
        {
            try
            {
                Transform root = OptionBehaviour.transform;
                Transform title = OptionBehaviour.TitleText != null ? OptionBehaviour.TitleText.transform : null;

                Transform[] children = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform tr in children)
                {
                    if (tr == null || tr == root || tr == title)
                        continue;

                    string n = tr.gameObject.name ?? string.Empty;
                    bool rightControl =
                        n.IndexOf("Plus", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Minus", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Increase", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Decrease", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Value", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (rightControl)
                        tr.gameObject.SetActive(false);
                }

                Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(true);
                foreach (Collider2D c in colliders)
                {
                    if (c != null)
                        c.enabled = false;
                }
            }
            catch { }
        }
    }
}
