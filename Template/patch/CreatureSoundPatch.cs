using BinderDyn.audio;
using BinderDyn.config;
using BinderDyn.service;
using HarmonyLib;
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
        if (clip == null)
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

        var player = enemy.GetComponent<LocalAudioPlayer>()
                     ?? enemy.gameObject.AddComponent<LocalAudioPlayer>();

        if (player.IsPlaying)
        {
            return ModConfig.PlayAlongsideVanilla.Value;
        }

        var clipPath = SoundPackService.PickRandomClip(profile, player.LastPlayedClipPath);
        if (clipPath == null)
        {
            return true;
        }

        player.LastPlayedClipPath = clipPath;
        player.PlayClip(clipPath);
        return ModConfig.PlayAlongsideVanilla.Value;
    }
}
