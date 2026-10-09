using BepInEx;
using HarmonyLib;
using RoR2;
using RoR2.UI;

namespace HideKeyItemSlot
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class HideKeyItemSlot : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "HideKeyItemSlot";
        private const string PluginVersion = "1.0.0";
        
        public void Awake()
        {
            Log.Init(Logger);

            Harmony harmony = new Harmony(PluginGUID);
            harmony.CreateClassProcessor(typeof(DisableKeyItemSlotObject)).Patch();
        }

        [HarmonyPatch]
        public class DisableKeyItemSlotObject
        {
            [HarmonyPatch(typeof(HUD), "OnEnable")]
            [HarmonyPostfix]
            public static void DisableGameObject(HUD __instance)
            {
                __instance.keyItemIcon.gameObject.SetActive(false);
                
                __instance.onTargetMasterDiscovered += master =>
                {
                    if (!master.inventory) return;

                    if (master.inventory.keyItemIndex != KeyItemIndex.None)
                    {
                        __instance.keyItemIcon.gameObject.SetActive(true);
                        return;
                    }

                    master.inventory.onKeyItemAddedClient += EnableKeySlotObject;
                };
                
                __instance.onTargetMasterLost += master =>
                {
                    if(master.inventory)
                        master.inventory.onKeyItemAddedClient -= EnableKeySlotObject;
                };
                
                return;
                
                void EnableKeySlotObject(KeyItemIndex obj)
                {
                    if(!__instance || !__instance.keyItemIcon)
                        Log.Debug("hud/key slot was null ,.");
                    
                    __instance?.keyItemIcon?.gameObject.SetActive(true);
                }
            }
        }
    }
}
