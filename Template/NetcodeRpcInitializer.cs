using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using Unity.Netcode;
using UnityEngine;

namespace BinderDyn;

internal static class NetcodeRpcInitializer
{
    private static bool _initialized;

    public static void InitializeAssembly()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        var log = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);
        var invoked = 0;

        foreach (var type in GetLoadableTypes(Assembly.GetExecutingAssembly()))
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!ShouldInvokeInitializer(method))
                {
                    continue;
                }

                if (!method.IsStatic)
                {
                    continue;
                }

                try
                {
                    method.Invoke(null, null);
                    invoked++;
                    log.LogInfo($"Registered netcode via {type.Name}.{method.Name}");
                }
                catch (Exception ex)
                {
                    log.LogError($"Failed netcode init {type.Name}.{method.Name}: {ex}");
                }
            }
        }

        if (invoked == 0)
        {
            log.LogWarning(
                "No netcode RPC initializer methods ran. " +
                "Build with netcode-patch enabled (do not use SkipNetcodePatch=true for release builds).");
        }
    }

    public static void InitializeInstance(NetworkBehaviour behaviour)
    {
        var method = behaviour.GetType().GetMethod(
            "__initializeVariables",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        method?.Invoke(behaviour, null);
    }

    private static bool ShouldInvokeInitializer(MethodInfo method)
    {
        if (method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false).Length > 0)
        {
            return true;
        }

        return method.Name.StartsWith("InitializeRPCS_", StringComparison.Ordinal);
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type != null).ToArray()!;
        }
    }
}
