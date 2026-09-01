using BinderDyn.model;

namespace BinderDyn.service;

public static class CreatureProfileTestHelper
{
    public static bool ShouldTriggerForClip(CreatureProfile profile, string? clipName) =>
        profile.ShouldTriggerForClip(clipName);
}
