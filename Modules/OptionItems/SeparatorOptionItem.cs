// Separatore nero interno opzionale, da usare come linea tra sottocategorie.
// Uso: SeparatorOptionItem.Create(OptionCategory.Game);
using System;
using UnityEngine;

namespace BanMod
{
    public sealed class SeparatorOptionItem : OptionItem
    {
        private static int NextSeparatorId = 990000;
        private static Sprite LineSprite;
        private const string LineObjectName = "BanModBlackSeparatorLine";

        public SeparatorOptionItem(int id, OptionCategory category)
            : base(id, string.Empty, 0, category, true)
        {
            SetHeader(true);
            SetColor(Color.black);
        }

        public static SeparatorOptionItem Create(OptionCategory category)
        {
            return new SeparatorOptionItem(NextSeparatorId++, category);
        }

        public static SeparatorOptionItem Create(int id, OptionCategory category)
        {
            return new SeparatorOptionItem(id, category);
        }

        public override string GetName(bool disableColor = false) => string.Empty;
        public override string GetString() => string.Empty;

        public override void Refresh()
        {
            base.Refresh();
            if (OptionBehaviour == null)
                return;

            try
            {
                if (OptionBehaviour.TitleText != null)
                    OptionBehaviour.TitleText.text = string.Empty;

                if (OptionBehaviour.ValueText != null)
                    OptionBehaviour.ValueText.gameObject.SetActive(false);

                HideRightControls();
                DrawLine();
            }
            catch { }
        }

        private void HideRightControls()
        {
            try
            {
                Transform root = OptionBehaviour.transform;
                Transform[] children = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform tr in children)
                {
                    if (tr == null || tr == root)
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

        private void DrawLine()
        {
            try
            {
                Transform root = OptionBehaviour.transform;
                Transform existing = root.Find(LineObjectName);
                GameObject line = existing != null ? existing.gameObject : new GameObject(LineObjectName);
                line.transform.SetParent(root, false);
                line.transform.localPosition = new Vector3(0f, 0f, -10f);
                line.transform.localScale = new Vector3(5.1f, 0.035f, 1f);

                SpriteRenderer sr = line.GetComponent<SpriteRenderer>();
                if (sr == null)
                    sr = line.AddComponent<SpriteRenderer>();

                sr.sprite = GetLineSprite();
                sr.color = Color.black;
                sr.sortingOrder = 1000;
                line.SetActive(true);
            }
            catch { }
        }

        private static Sprite GetLineSprite()
        {
            if (LineSprite != null)
                return LineSprite;

            Texture2D tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            LineSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            return LineSprite;
        }
    }
}
