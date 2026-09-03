using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BinderDyn.config;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using BinderDyn.model;
using UnityEngine;

namespace BinderDyn.service;

public static class SoundPackService
{
    private static readonly JsonSerializerSettings ProfileJsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    private static readonly Dictionary<string, CreatureProfile> ProfilesByEnemyType = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, IReadOnlyList<string>> ClipsByProfileId = new();
    private static readonly System.Random Random = new();
    private static readonly System.Collections.Generic.HashSet<string> _loggedOnce = new(StringComparer.Ordinal);

    public static string PluginDirectory { get; private set; } = string.Empty;
    public static string AudioRootDirectory { get; private set; } = string.Empty;

    public static IReadOnlyCollection<CreatureProfile> AllProfiles => ProfilesByEnemyType.Values;

    public static void Load(string pluginPath)
    {
        var pluginDirectory = Directory.Exists(pluginPath)
            ? pluginPath
            : Path.GetDirectoryName(pluginPath)!;

        PluginDirectory = pluginDirectory;
        AudioRootDirectory = Path.Combine(pluginDirectory, "audio");
        ProfilesByEnemyType.Clear();
        ClipsByProfileId.Clear();

        var profiles = LoadProfilesFile(pluginDirectory);
        ModConfig.Bind(Plugin.Instance.Config, profiles);

        foreach (var profile in profiles)
        {
            ProfilesByEnemyType[profile.EnemyType] = profile;
            ClipsByProfileId[profile.Id] = ScanPackFolder(profile);
        }

        Plugin.Log.LogInfo($"Loaded {profiles.Count} creature sound profile(s).");
    }

    private static List<CreatureProfile> LoadProfilesFile(string pluginDirectory)
    {
        var configPath = Path.Combine(pluginDirectory, "config", "creature-profiles.json");
        if (!File.Exists(configPath))
        {
            configPath = Path.Combine(pluginDirectory, "creature-profiles.json");
        }

        if (!File.Exists(configPath))
        {
            Plugin.Log.LogWarning($"creature-profiles.json not found at {configPath}");
            return new List<CreatureProfile>();
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var file = JsonConvert.DeserializeObject<CreatureProfilesFile>(json, ProfileJsonSettings);
            return file?.Profiles ?? new List<CreatureProfile>();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Failed to parse creature-profiles.json: {ex.Message}");
            return new List<CreatureProfile>();
        }
    }

    private static IReadOnlyList<string> ScanPackFolder(CreatureProfile profile)
    {
        var folderPath = Path.Combine(AudioRootDirectory, profile.SoundPackFolder.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(folderPath))
        {
            LogOnce($"missing_folder_{profile.Id}", $"Sound pack folder missing for {profile.Id}: {folderPath}");
            return Array.Empty<string>();
        }

        var clips = Directory
            .EnumerateFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(path =>
            {
                var extension = Path.GetExtension(path);
                return extension.Equals(".opus", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".wav", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (clips.Count == 0)
        {
            LogOnce($"empty_pack_{profile.Id}", $"No .opus or .wav clips found for {profile.Id} in {folderPath}");
        }
        else
        {
            Plugin.Log.LogInfo($"Found {clips.Count} clip(s) for {profile.Id} in {folderPath}");
        }

        return clips;
    }

    public static CreatureProfile? GetProfileForEnemy(EnemyAI enemy)
    {
        if (enemy == null)
        {
            return null;
        }

        var enemyType = enemy.GetType().Name;
        return ProfilesByEnemyType.TryGetValue(enemyType, out var profile) ? profile : null;
    }

    public static bool IsProfileActive(CreatureProfile profile) =>
        ModConfig.IsProfileEnabled(profile) &&
        ClipsByProfileId.TryGetValue(profile.Id, out var clips) &&
        clips.Count > 0;

    public static string? PickRandomClip(CreatureProfile profile, string? excludePath = null)
    {
        if (!ClipsByProfileId.TryGetValue(profile.Id, out var clips) || clips.Count == 0)
        {
            return null;
        }

        return PickRandomClipFromList(clips, excludePath, Random);
    }

    public static string? PickRandomClipFromList(
        IReadOnlyList<string> clips,
        string? excludePath,
        System.Random random)
    {
        if (clips.Count == 0)
        {
            return null;
        }

        if (clips.Count == 1)
        {
            return clips[0];
        }

        List<string> candidates;
        if (string.IsNullOrEmpty(excludePath))
        {
            candidates = clips.ToList();
        }
        else
        {
            candidates = clips
                .Where(clip => !clip.Equals(excludePath, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (candidates.Count == 0)
        {
            candidates = clips.ToList();
        }

        return candidates[random.Next(candidates.Count)];
    }

    public static bool ShouldTrigger(CreatureProfile profile, string? clipName) =>
        IsProfileActive(profile) && profile.ShouldTriggerForClip(clipName);

    private static void LogOnce(string key, string message)
    {
        if (_loggedOnce.Add(key))
        {
            Plugin.Log.LogWarning(message);
        }
    }

    /// <summary>
    /// Given a profile and a bare clip filename (e.g. "wallah.wav"), returns the full local
    /// path if the file exists in this client's pack folder, or null if it is not present.
    /// </summary>
    public static string? ResolveClipPath(CreatureProfile profile, string clipName)
    {
        var folderPath = Path.Combine(AudioRootDirectory, profile.SoundPackFolder.Replace('/', Path.DirectorySeparatorChar));
        var fullPath = Path.Combine(folderPath, clipName);
        return File.Exists(fullPath) ? fullPath : null;
    }
}
