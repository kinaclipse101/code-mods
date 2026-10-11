using BepInEx;
using RoR2;

namespace EnsureKeyItemSlot
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class EnsureKeyItemSlot : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "EnsureKeyItemSlot";
        private const string PluginVersion = "1.0.1";

        private void Awake()
        {
            Log.Init(Logger);
        }

        [SystemInitializer(typeof(SurvivorCatalog))]
        public static void Ensure()
        {
            foreach (SurvivorDef surv in SurvivorCatalog.survivorDefs)
            {
                if (surv.bodyPrefab?.TryGetComponent(out KeyItemSlot keyItemSlot) == true) continue;
                surv.bodyPrefab.AddComponent<KeyItemSlot>();
                Log.Warning($"survivor {surv.cachedName} had no keyitemslot !! adding ,.,.");
            }
        }
    }
}
