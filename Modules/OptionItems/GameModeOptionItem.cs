//credits and licenses in the resources folder
using AmongUs.GameOptions;
using BanMod;
using GameCore;
using HarmonyLib;
using System;
using UnityEngine;

namespace BanMod
{
    public sealed class GameModeOptionItem : StringOptionItem
    {
        private GameModeType[] _order;

        private readonly GameModeType _defaultMode;

        public GameModeOptionItem(
            string name,
            GameModeType[] order,
            GameModeType defaultMode,
            OptionCategory category,
            bool isSingleValue,
            bool shouldTranslate = true,
            Action onValueChange = null)
            : base(
                OptionItem.NextAutoId(),
                name,
                Array.IndexOf(order, defaultMode),
                category,
                isSingleValue,
                Array.ConvertAll(order, mode => mode.ToString()),
                shouldTranslate,
                onValueChange)
        {
            _order = (GameModeType[])order.Clone();
            _defaultMode = defaultMode;
        }

        public GameModeType Selected
        {
            get
            {
                int index = base.GetValue();

                if (index < 0 || index >= _order.Length)
                    return _defaultMode;

                return _order[index];
            }
        }

        public bool GetValue(GameModeType expected)
            => Selected == expected;

        public void SetValue(
            GameModeType mode,
            bool doSync = true)
        {
            int index = Array.IndexOf(_order, mode);

            if (index < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(mode),
                    mode,
                    "GameMode non presente nell'ordine corrente.");

            base.SetValue(index, doSync);
        }

        public GameModeType[] GetOrder()
            => (GameModeType[])_order.Clone();

        public void SetGameModes(
            GameModeType[] order,
            bool doSync = true)
        {
            if (order == null || order.Length == 0)
                throw new ArgumentException(
                    "La lista dei GameMode non può essere vuota.",
                    nameof(order));

            GameModeType previousMode = Selected;

            _order = (GameModeType[])order.Clone();

            Selections = Array.ConvertAll(
                _order,
                mode => mode.ToString()
            );

            Rule = (
                0,
                _order.Length - 1,
                1
            );

            int newIndex =
                Array.IndexOf(_order, previousMode);

            if (newIndex < 0)
            {
                newIndex =
                    Array.IndexOf(_order, _defaultMode);
            }

            if (newIndex < 0)
                newIndex = 0;

            int currentIndex = base.GetValue();

            if (currentIndex != newIndex)
            {
                base.SetValue(
                    newIndex,
                    doSync
                );
            }
        }

        [Obsolete(
            "Non usare GetInt() per GameMode. Usa GetValue(GameModeType) oppure Selected.",
            true)]
        public new int GetInt()
            => base.GetInt();

        [Obsolete(
            "Non usare GetValue() senza parametro. Usa GetValue(GameModeType) oppure Selected.",
            true)]
        public new int GetValue()
            => base.GetValue();

        [Obsolete(
            "Non usare SetValue(int). Usa SetValue(GameModeType).",
            true)]
        public new void SetValue(
            int value,
            bool doSync = true)
            => base.SetValue(value, doSync);

        [Obsolete(
            "Non usare SetValue(int). Usa SetValue(GameModeType).",
            true)]
        public new void SetValue(
            int value,
            bool doSave,
            bool doSync = true)
            => base.SetValue(value, doSave, doSync);
    }
}