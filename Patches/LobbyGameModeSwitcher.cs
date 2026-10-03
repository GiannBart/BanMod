using AmongUs.GameOptions;
using InnerNet;
using System.Collections;
using UnityEngine;

namespace BanMod;

public static class LobbyGameModeOptionPatch
{
    private static bool switching = false;

    public static IEnumerator SwitchGameMode(GameModes target)
    {
        if (switching)
            yield break;

        switching = true;

        var client = AmongUsClient.Instance;
        var optionsManager = GameOptionsManager.Instance;

        if (client == null ||
            optionsManager == null ||
            !client.AmHost)
        {
            switching = false;
            yield break;
        }

        var oldManager = GameManager.Instance;

        Debug.Log(
            $"[BanMod] Switching lobby mode -> {target} | " +
            $"OldManager={oldManager?.GetType().Name} | " +
            $"OldNetId={oldManager?.NetId}"
        );

        optionsManager.SwitchGameMode(target);
        optionsManager.CurrentGameOptions = optionsManager.GameHostOptions;

        Debug.Log(
            $"[BanMod] Target options -> " +
            $"{optionsManager.CurrentGameOptions?.GameMode}"
        );

        if (oldManager != null)
        {
            Debug.Log(
                $"[BanMod] Despawn old manager -> " +
                $"{oldManager.GetType().Name} | NetId={oldManager.NetId}"
            );

            client.Despawn(oldManager);
            GameManager.DestroyInstance();

            yield return new WaitForSecondsRealtime(0.20f);
        }

        GameManager newManager =
            GameManagerCreator.CreateGameManager(target);

        if (newManager == null)
        {
            Debug.LogError(
                $"[BanMod] Cannot create GameManager: {target}"
            );

            switching = false;
            yield break;
        }

        Debug.Log(
            $"[BanMod] Spawn new manager -> " +
            $"{newManager.GetType().Name} | SpawnId={newManager.SpawnId}"
        );

        client.Spawn(
            newManager,
            -2,
            SpawnFlags.None
        );

        yield return new WaitForSecondsRealtime(0.20f);

        for (int i = 0; i < 60; i++)
        {
            var manager = GameManager.Instance;

            if (manager != null)
            {
                bool correctManager;

                if (target == GameModes.Normal ||
                    target == GameModes.NormalFools)
                {
                    correctManager =
                        manager is NormalGameManager;
                }
                else
                {
                    correctManager =
                        manager is HideAndSeekManager;
                }

                if (correctManager)
                    break;
            }

            yield return null;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError(
                $"[BanMod] GameManager missing after switch -> {target}"
            );

            switching = false;
            yield break;
        }

        if (GameManager.Instance.LogicOptions != null)
        {
            GameManager.Instance.LogicOptions.SyncOptions();
        }

        yield return null;

        OptionItem.SyncAllOptions();
        FfaExternalBridge.SyncGameMode();
        FfaExternalBridge.SyncAll();

        Debug.Log(
            $"[BanMod] Lobby mode switched -> {target} | " +
            $"Manager={GameManager.Instance?.GetType().Name} | " +
            $"NetId={GameManager.Instance?.NetId} | " +
            $"Options={GameOptionsManager.Instance.CurrentGameOptions?.GameMode}"
        );

        switching = false;
    }
}