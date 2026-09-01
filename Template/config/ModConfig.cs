using System.Collections.Generic;
using BepInEx.Configuration;
using BinderDyn.model;

namespace BinderDyn.config;

public static class ModConfig
{
    private static readonly Dictionary<string, ConfigEntry<bool>> ProfileToggles = new();

    public static ConfigEntry<bool> PlayAlongsideVanilla { get; private set; } = null!;

    public static void Bind(ConfigFile config, IReadOnlyList<CreatureProfile> profiles)
    {
        PlayAlongsideVanilla = config.Bind(
            "General",
            "PlayAlongsideVanilla",
            true,
            "When true, vanilla creature sounds play alongside custom audio. When false, vanilla sounds are suppressed for triggered clips.");

        ProfileToggles.Clear();
        foreach (var profile in profiles)
        {
            ProfileToggles[profile.Id] = config.Bind(
                "Creatures",
                $"Enable_{profile.Id}",
                profile.Enabled,
                $"Enable custom sounds for {profile.EnemyType} ({profile.Id}).");
        }
    }

    public static bool IsProfileEnabled(CreatureProfile profile)
    {
        if (ProfileToggles.TryGetValue(profile.Id, out var entry))
        {
            return entry.Value;
        }

        return profile.Enabled;
    }
}
