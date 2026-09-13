using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using InnerNet;
using System.Collections;
using UnityEngine;

namespace BanMod;

public static class LobbyGameModeOptionPatch
{
    private static bool switching = false;
    public static IEnumerator SwitchGameMode(GameModes target)
    {
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

        Debug.Log($"[BanMod] Switching lobby mode -> {target}");

        // 1. Cambia il set di opzioni vanilla
        optionsManager.SwitchGameMode(target);

        // 2. Usa le Host Options della nuova modalità
        optionsManager.CurrentGameOptions =
            optionsManager.GameHostOptions;

        // 3. Rimuove il vecchio GameManager
        GameManager oldManager = GameManager.Instance;

        if (oldManager != null)
        {
            client.Despawn(oldManager);

            GameManager.DestroyInstance();

            yield return null;
        }

        // 4. Crea il GameManager corretto
        GameManager newManager =
            GameManagerCreator.CreateGameManager(target);

        if (newManager == null)
        {
            Debug.LogError(
                $"[BanMod] Impossibile creare GameManager: {target}"
            );

            switching = false;
            yield break;
        }

        // 5. Lo spawna nella STESSA lobby
        client.Spawn(
            newManager,
            -2,
            SpawnFlags.None
        );

        /*
         * Aspetta che il nuovo GameManager sia realmente diventato
         * GameManager.Instance.
         *
         * Non basta sempre un singolo yield return null con IL2CPP/network spawn.
         */
        for (int i = 0; i < 30; i++)
        {
            if (GameManager.Instance != null)
            {
                bool correctManager =
                    target == GameModes.Normal
                        ? GameManager.Instance is NormalGameManager
                        : GameManager.Instance is HideAndSeekManager;

                if (correctManager)
                    break;
            }

            yield return null;
        }

        // Sync vanilla
        if (GameManager.Instance != null &&
            GameManager.Instance.LogicOptions != null)
        {
            GameManager.Instance.LogicOptions.SyncOptions();
        }

        // Aspetta ancora qualche frame perché CurrentGameOptions / UI si assestino
        yield return null;
        yield return null;

        // Sync BanMod
        OptionItem.SyncAllOptions();
        FfaExternalBridge.SyncGameMode();
        FfaExternalBridge.SyncAll();

        Debug.Log(
            $"[BanMod] Lobby mode switched -> {target} | " +
            $"Manager={GameManager.Instance?.GetType().Name} | " +
            $"Options={GameOptionsManager.Instance.CurrentGameOptions?.GameMode}"
        );

        switching = false;
    }
}