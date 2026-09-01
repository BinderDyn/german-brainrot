using System.Collections.Generic;

namespace BinderDyn.model;

public class CreatureProfile
{
    public string Id { get; set; } = string.Empty;
    public string EnemyType { get; set; } = string.Empty;
    public string SoundPackFolder { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public List<string> TriggerOnVanillaClips { get; set; } = new();

    public bool TriggersOnAnyClip =>
        TriggerOnVanillaClips.Exists(static clip => clip == "*");

    public bool ShouldTriggerForClip(string? clipName)
    {
        if (string.IsNullOrEmpty(clipName))
        {
            return false;
        }

        if (TriggersOnAnyClip)
        {
            return true;
        }

        return TriggerOnVanillaClips.Contains(clipName);
    }
}

public class CreatureProfilesFile
{
    public List<CreatureProfile> Profiles { get; set; } = new();
}
