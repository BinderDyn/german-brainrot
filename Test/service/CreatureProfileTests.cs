using BinderDyn.model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BinderDyn.service;

[TestClass]
public class CreatureProfileTests
{
    [TestMethod]
    public void ShouldTriggerForClip_WhenWildcardConfigured()
    {
        var profile = new CreatureProfile
        {
            TriggerOnVanillaClips = { "*" }
        };

        Assert.IsTrue(CreatureProfileTestHelper.ShouldTriggerForClip(profile, "AnyClip"));
    }

    [TestMethod]
    public void ShouldTriggerForClip_WhenSpecificClipConfigured()
    {
        var profile = new CreatureProfile
        {
            TriggerOnVanillaClips = { "HoarderBugAngry" }
        };

        Assert.IsTrue(CreatureProfileTestHelper.ShouldTriggerForClip(profile, "HoarderBugAngry"));
        Assert.IsFalse(CreatureProfileTestHelper.ShouldTriggerForClip(profile, "OtherClip"));
    }

    [TestMethod]
    public void ShouldNotTriggerForClip_WhenClipNameMissing()
    {
        var profile = new CreatureProfile
        {
            TriggerOnVanillaClips = { "HoarderBugAngry" }
        };

        Assert.IsFalse(CreatureProfileTestHelper.ShouldTriggerForClip(profile, null));
        Assert.IsFalse(CreatureProfileTestHelper.ShouldTriggerForClip(profile, ""));
    }
}
