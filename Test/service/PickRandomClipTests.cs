using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BinderDyn.service;

[TestClass]
public class PickRandomClipTests
{
    private static readonly List<string> ThreeClips = new()
    {
        @"C:\audio\clip_a.wav",
        @"C:\audio\clip_b.wav",
        @"C:\audio\clip_c.wav"
    };

    [TestMethod]
    public void PickRandomClipFromList_WhenExcludeSet_NeverReturnsExcludedClip()
    {
        var random = new System.Random(0);
        var excludePath = ThreeClips[1];

        for (var i = 0; i < 100; i++)
        {
            var clip = SoundPackService.PickRandomClipFromList(ThreeClips, excludePath, random);
            Assert.IsNotNull(clip);
            Assert.AreNotEqual(excludePath, clip, ignoreCase: true);
        }
    }

    [TestMethod]
    public void PickRandomClipFromList_WhenOnlyOneClip_ReturnsThatClipEvenIfExcluded()
    {
        var clips = new List<string> { @"C:\audio\only.wav" };
        var clip = SoundPackService.PickRandomClipFromList(clips, clips[0], new System.Random(0));

        Assert.AreEqual(clips[0], clip);
    }

    [TestMethod]
    public void PickRandomClipFromList_WhenExcludeMissing_PicksFromAllClips()
    {
        var random = new System.Random(42);
        var seen = new HashSet<string>();

        for (var i = 0; i < 50; i++)
        {
            var clip = SoundPackService.PickRandomClipFromList(ThreeClips, @"C:\audio\not_in_pack.wav", random);
            Assert.IsNotNull(clip);
            seen.Add(clip);
        }

        Assert.IsTrue(seen.Count > 1);
    }

    [TestMethod]
    public void PickRandomClipFromList_WhenExcludeNull_PicksFromAllClips()
    {
        var random = new System.Random(7);
        var seen = new HashSet<string>();

        for (var i = 0; i < 50; i++)
        {
            var clip = SoundPackService.PickRandomClipFromList(ThreeClips, null, random);
            Assert.IsNotNull(clip);
            seen.Add(clip);
        }

        Assert.IsTrue(seen.Count > 1);
    }
}
