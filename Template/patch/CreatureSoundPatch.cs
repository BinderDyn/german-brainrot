using BinderDyn.audio;
using BinderDyn.config;
using BinderDyn.service;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace BinderDyn.patch;

[HarmonyPatch(typeof(AudioSource))]
public static class CreatureSoundPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(AudioSource.PlayOneShot), typeof(AudioClip))]
    private static bool OnPlayOneShotClip(AudioSource __instance, AudioClip clip) =>
        HandleCreatureSound(__instance, clip);

    [HarmonyPrefix]
    [HarmonyPatch(nameof(AudioSource.PlayOneShot), typeof(AudioClip), typeof(float))]
    private static bool OnPlayOneShotClipVolume(AudioSource __instance, AudioClip clip, float volumeScale) =>
        HandleCreatureSound(__instance, clip);

    private static bool HandleCreatureSound(AudioSource audioSource, AudioClip? clip)
    {
        if (clip == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
        {
            return true;
        }

        var enemy = audioSource.GetComponentInParent<EnemyAI>();
        if (enemy == null)
        {
            return true;
        }

        var profile = SoundPackService.GetProfileForEnemy(enemy);
        if (profile == null || !SoundPackService.ShouldTrigger(profile, clip.name))
        {
            return true;
        }

        var clipPath = SoundPackService.PickRandomClip(profile);
        if (clipPath == null)
        {
            return true;
        }

        var stream = enemy.GetComponent<CreatureAudioStream>();
        if (stream == null)
        {
            Plugin.Log.LogWarning($"CreatureAudioStream missing on {enemy.GetType().Name}");
            return ModConfig.PlayAlongsideVanilla.Value;
        }

        if (stream.IsStreaming)
        {
            return ModConfig.PlayAlongsideVanilla.Value;
        }

        stream.StreamOpusFromFile(clipPath);
        return ModConfig.PlayAlongsideVanilla.Value;
    }
}
