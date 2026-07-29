#if DEBUG
using System.Reflection;

namespace TooLittleSpawns;

internal static class UHRSupport
{
    internal static void hotReload(Assembly assembly, string path)
    {
        UnityHotReloadNS.UnityHotReload.LoadNewAssemblyVersion(assembly, path);
    }
}
#endif