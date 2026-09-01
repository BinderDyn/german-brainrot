using System.Linq;
using BinderDyn.audio;
using BinderDyn.service;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace BinderDyn.patch;

[HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Start))]
public static class EnemyPrefabPatch
{
    [HarmonyPostfix]
    private static void InjectCreatureAudioStream()
    {
        if (NetworkManager.Singleton == null)
        {
            Plugin.Log.LogWarning("NetworkManager not available during MenuManager.Start");
            return;
        }

        var registeredTypes = SoundPackService.AllProfiles
            .Select(profile => profile.EnemyType)
            .ToHashSet();

        var injected = 0;
        foreach (var networkPrefab in NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs)
        {
            if (networkPrefab?.Prefab == null)
            {
                continue;
            }

            var enemy = networkPrefab.Prefab.GetComponent<EnemyAI>();
            if (enemy == null)
            {
                continue;
            }

            if (!registeredTypes.Contains(enemy.GetType().Name))
            {
                continue;
            }

            if (networkPrefab.Prefab.GetComponent<CreatureAudioStream>() != null)
            {
                continue;
            }

            networkPrefab.Prefab.AddComponent<CreatureAudioStream>();
            injected++;
            Plugin.Log.LogInfo($"Injected CreatureAudioStream on {enemy.GetType().Name}");
        }

        Plugin.Log.LogInfo($"CreatureAudioStream injection complete ({injected} prefab(s)).");
    }
}
