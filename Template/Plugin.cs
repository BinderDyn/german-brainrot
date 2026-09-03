using System.IO;
using BepInEx;
using BepInEx.Logging;
using BinderDyn.patch;
using BinderDyn.service;
using HarmonyLib;

namespace BinderDyn;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; } = null!;

    public static ManualLogSource Log => Instance.Logger;

    private readonly Harmony _harmony = new(PluginInfo.PLUGIN_GUID);

    public Plugin()
    {
        Instance = this;
    }

    private void Awake()
    {
        SoundPackService.Load(Path.GetDirectoryName(Info.Location)!);
        Log.LogInfo("Applying patches...");
        _harmony.PatchAll(typeof(CreatureSoundPatch));
        Log.LogInfo("GermanBrainrot loaded.");
    }
}
