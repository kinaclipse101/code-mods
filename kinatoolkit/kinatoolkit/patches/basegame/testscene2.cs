using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine.AddressableAssets;

namespace kinatoolkit.patches.basegame;

public class testscene2 : PatchBase<testscene2>
{
    private SceneDef testsceneDef;
    
    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            Log.Debug("adding test scene def !!");
            AssetReferenceT<SceneDef> sceneDef = new AssetReferenceT<SceneDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Junk_testscene.testscene_asset);
            AssetAsyncReferenceManager<SceneDef>.LoadAsset(sceneDef).Completed += (x) =>
            {
                testsceneDef = x.Result;
                StageRegistration.AddSceneDef(testsceneDef, kinatoolkit.instance.Info);
            };
        }
        else
        {
        }
    }
    
    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("kinaToolkit - testscene",
            "Bring back testscene.",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
    }

    private ConfigEntry<bool> enabled;
}