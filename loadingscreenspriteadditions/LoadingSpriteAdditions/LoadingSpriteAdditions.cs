using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Newtonsoft.Json.Utilities;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using Console = System.Console;
using Object = System.Object;

namespace LoadingSpriteAdditions
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.TeamMoonstorm.Starstorm2", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(LoadingScreenFix.LoadingScreenFix.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LoadingSpriteAdditions : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "LoadingSpriteAdditions";
        private const string PluginVersion = "1.1.3";
        
        private static bool ROOInstalled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");

        private static LoadingSpriteAdditions instance;
        private AssetBundle loadingSpritesBundle;
        private string[] loadingSpriteNames = 
        [
            "Huntress",
            "Enforcer",
            "Bandit",
            "BanditWhip",
            "HAND",
            "HANDRun",
            "Engi",
            "Miner",
            "Acrid",
            "Merc",
            "CHEF",
            "Pilot",
            "Arti",
            "ArtiFlame",
            "Drifter",
            "DrifterRun",
            "Sniper",
            "Robomando",
            
            "AcolyteTribeScout",
            "AcolyteTribeArcher",
            "AcolyteTribeHunter",
            
            "Nemmando",
            "NemmandoGun",
            "Nemmerc",
            "Technician",
            "MULE",
            
            "Captain",
            "REX"
        ];

        private string[] disabledByDefault =
        [
            "CHEF", // too long !!
            "Acrid", // too wide !!
        ];

        public enum spriteType
        {
            rorr,
            disabled
        }
        
        public enum spriteTypeReplacement
        {
            rorr,
            ror1,
            disabled
        }
        
        public enum spriteTypeUnsupported
        {
            ror1,
            disabled
        }

        private ConfigEntry<bool> onlyCurrentSurv;
        private ConfigEntry<string> customBodyDict;
        private Dictionary<string, string> configBodyMatch = new();
        private ConfigEntry<string> blacklistBodies;
        private List<string> blacklistBodiesList = [];
        private ConfigEntry<float> ponchoOverride;
        private ConfigEntry<bool> ponchoAddition;
        private SimpleSpriteAnimation ponchoSSA;

        public void Awake()
        {
            instance = this;
            Log.Init(Logger);
            
            loadingSpritesBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "loadingspritebundle"));
            try
            {
                var enableReplacement = instance.Config.Bind("LSA - Replacements",
                    "Executioner", 
                    spriteTypeReplacement.rorr, 
                    $"whether to replace Executioner on the loading screen .,,.");
                if(ROOInstalled)
                    ROOSupport.AddConfig(enableReplacement);
                
                if (enableReplacement.Value == spriteTypeReplacement.rorr)
                {
                    for (int i = 0; i < LoadingScreenFix.LoadingScreenFix.instance._spriteAnimations.Count; i++)
                    {
                        if(LoadingScreenFix.LoadingScreenFix.instance._spriteAnimations[i].name != "ssaExecutionerWalk") continue;
                        LoadingScreenFix.LoadingScreenFix.instance._spriteAnimations[i] = loadingSpritesBundle.LoadAsset<SimpleSpriteAnimation>("ExecutionerWalk");
                        break;
                    }
                } 
                else if (enableReplacement.Value == spriteTypeReplacement.disabled)
                {
                    for (int i = 0; i < LoadingScreenFix.LoadingScreenFix.instance._spriteAnimations.Count; i++)
                    {
                        if(LoadingScreenFix.LoadingScreenFix.instance._spriteAnimations[i].name != "ssaExecutionerWalk") continue;
                        LoadingScreenFix.LoadingScreenFix.instance._spriteAnimations.RemoveAt(i);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("error while trying to replace exe loading sprite ,.., ");
                Log.Warning(e);
            }
            
            ponchoAddition = Config.Bind("LSA - Misc", 
                "poncho addition,..,", 
                false, 
                "addition of .,,. poncho ,..,,.,.,..,");
            ponchoOverride  = Config.Bind("LSA - Misc", 
                "poncho override,..,", 
                1f, 
                "chance for poncho to override any sprite .,, .,. 1 = 1% ,..,,.,.,..,");
            ponchoSSA = loadingSpritesBundle.LoadAsset<SimpleSpriteAnimation>($"ssaFalseSonPoncho");
            if (ponchoAddition.Value)
                LoadingScreenFix.LoadingScreenFix.AddSpriteAnimation(ponchoSSA);
            

            onlyCurrentSurv = Config.Bind("LSA - Misc", 
                "only pick currently playing survivor,..,", 
                true, 
                "makes LSA only pick your current survivor sprite .,,. goes random if it cant find it,.,..,");
            
            customBodyDict = Config.Bind("LSA - Misc", 
                "currentply playing survivor overrides,..,", 
                "DroneTechBody,Pilot,RailgunnerBody,Sniper,ToolbotBody,HAND", 
                "overrides for if you want a specific body to have a sprte ,.,.");
            CreateBodyMatchDict();
            customBodyDict.SettingChanged += (sender, args) => CreateBodyMatchDict();
            
            blacklistBodies = Config.Bind("LSA - Misc", 
                "blacklist bodies for currently playing override,..,", 
                "", 
                "if you think like ,. huntress rorr sprite is chuddy or something and want random on her,.,. eg \"HuntressBody,CommandoBody\" ,.,.");
            CreateBlacklist();
            blacklistBodies.SettingChanged += (sender, args) => CreateBlacklist();
            
            foreach (string spriteName in loadingSpriteNames)
            {
                spriteType defaultType = spriteType.rorr;
                if(disabledByDefault.Contains(spriteName))
                    defaultType = spriteType.disabled;
                
                ConfigEntry<spriteType> spriteTypeConfig = Config.Bind($"LSA - Sprites",
                    $"{spriteName}",
                    defaultType,
                    $"whether {spriteName} should show up as a loading screen icon .,,");
                
                if(ROOInstalled)
                    ROOSupport.AddConfig(spriteTypeConfig);

                if (spriteTypeConfig.Value == spriteType.rorr)
                {
                    try
                    {
                        SimpleSpriteAnimation ssa = loadingSpritesBundle.LoadAsset<SimpleSpriteAnimation>($"ssa{spriteName}");
                        LoadingScreenFix.LoadingScreenFix.AddSpriteAnimation(ssa);
                    }
                    catch (Exception e)
                    {
                       Log.Error($"failed while trying to add {spriteName} !!!");
                       Log.Error(e);
                    }
                }
            }
            
            AddSpriteReplacement("CommandoWalk");
            AddSpriteReplacement("DroneHover");
            AddSpriteReplacement("GolemWalk");
            AddSpriteReplacement("LemurianWalk");
            AddSpriteReplacement("LoaderWalk");
            AddSpriteReplacement("ChildWalk");
            
            Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_UI.MiniScene_prefab).Completed += handle =>
            {
                pickRandomInGame = handle.Result.gameObject.GetComponent<PickRandomObjectOnAwake>();
            };
            
            On.RoR2.PickRandomObjectOnAwake.OnEnable += ReplaceNewSillies;
            On.RoR2.UI.MainMenu.MainMenuController.Start += UnhookNewSillies;
            
            On.RoR2.PickRandomObjectOnAwake.OnEnable += PickRandomObjectOnAwakeOnOnEnable;

            if (ROOInstalled)
            {
                ROOSupport.AddConfig(ponchoAddition);
                ROOSupport.AddConfig(ponchoOverride);
                ROOSupport.AddConfig(onlyCurrentSurv);
                ROOSupport.AddConfig(customBodyDict);
                ROOSupport.AddConfig(blacklistBodies);
            }
        }
        
        private Dictionary<string, SimpleSpriteAnimation> spriteAnimations = new();
        private static PickRandomObjectOnAwake pickRandomInGame;
        
        private void AddSpriteReplacement(string ssaName, string customPrefabName = "")
        {
            string prefab = customPrefabName != "" ? customPrefabName : ssaName;
            var enableReplacement = instance.Config.Bind("LSA - Replacements",
                $"{prefab.Replace("Walk", "").Replace("Hover", "")}", 
                spriteTypeReplacement.rorr, 
                $"whether to replace {prefab.Replace("Walk", "").Replace("Hover", "")} on the loading screen .,,.");
            if(ROOInstalled)
                ROOSupport.AddConfig(enableReplacement);

            if (enableReplacement.Value == spriteTypeReplacement.rorr)
            {
                spriteAnimations.Add(ssaName, loadingSpritesBundle.LoadAsset<SimpleSpriteAnimation>(prefab));
            }
            
            addedDisableConfigs.Add($"{prefab.Replace("Walk", "").Replace("Hover", "")}", enableReplacement);
        }
        
        private void UnhookNewSillies(On.RoR2.UI.MainMenu.MainMenuController.orig_Start orig, RoR2.UI.MainMenu.MainMenuController self)
        {
            orig(self);
        
            On.RoR2.PickRandomObjectOnAwake.OnEnable -= ReplaceNewSillies;
            On.RoR2.UI.MainMenu.MainMenuController.Start -= UnhookNewSillies;

            ReplaceSilliesOnRandom(pickRandomInGame);
        }

        private void ReplaceNewSillies(On.RoR2.PickRandomObjectOnAwake.orig_OnEnable orig, PickRandomObjectOnAwake self)
        {
            if (self.gameObject.name != "MiniScene")
            {
                orig(self);
                return;
            }

            ReplaceSilliesOnRandom(self);
       
            orig(self);
        }

        private static Dictionary<string, ConfigEntry<spriteTypeReplacement>> addedDisableConfigs = [];
        private static Dictionary<string, ConfigEntry<spriteTypeUnsupported>> addedUnsupportedConfigs = [];

        private void ReplaceSilliesOnRandom(PickRandomObjectOnAwake pickRandom)
        {
            GameObject[] ssArray = HG.ArrayUtils.Clone(pickRandom.ObjectsToSelect);
            int realArrayIndex = 0;
            foreach (var loadingSprite in ssArray)
            {
                bool skipSprite = false;
                foreach (string loadingSpriteName in loadingSpriteNames)
                {
                    if (!loadingSprite.name.Contains(loadingSpriteName)) continue; // added by us ,. .,
                    
                    realArrayIndex++;
                    skipSprite = true;
                    break;
                }
                if (skipSprite)
                    continue;
                
                //Log.Debug("name = " + loadingSprite.name);

                string configName = $"{loadingSprite.name.Replace("Walk", "").Replace("Hover", "")}";
                if (addedDisableConfigs.TryGetValue(configName, out ConfigEntry<spriteTypeReplacement> config))
                {
                    if (config.Value == spriteTypeReplacement.disabled)
                    {
                        //Log.Debug($"killing {loadingSprite.name}");
                        Destroy(loadingSprite);
                        HG.ArrayUtils.ArrayRemoveAtAndResize(ref pickRandom.ObjectsToSelect, realArrayIndex);
                        continue;
                    }
                }
                else
                {
                    configName = configName.Replace("(Clone)", "");
                    if (!addedUnsupportedConfigs.TryGetValue(configName, out ConfigEntry<spriteTypeUnsupported> configUnsupportedModded))
                    {
                        configUnsupportedModded = instance.Config.Bind("LSA - Disable",
                            configName, 
                            spriteTypeUnsupported.ror1, 
                            $"whether to disable {configName} .,,.");
                    
                        if(ROOInstalled)
                            ROOSupport.AddConfig(configUnsupportedModded);
                    
                        addedUnsupportedConfigs.Add(configName, configUnsupportedModded);
                    }
                
                    if (configUnsupportedModded.Value == spriteTypeUnsupported.disabled)
                    {
                        //Log.Debug($"killing {loadingSprite.name}");
                        Destroy(loadingSprite);
                        HG.ArrayUtils.ArrayRemoveAtAndResize(ref pickRandom.ObjectsToSelect, realArrayIndex);
                        realArrayIndex--;
                    }
                }
                
                realArrayIndex++;
            }
            
            foreach (GameObject randomObj in pickRandom.ObjectsToSelect)
            {
                if (spriteAnimations.TryGetValue(randomObj.name, out SimpleSpriteAnimation spriteAnimation))
                {
                    randomObj.transform.GetChild(0).gameObject.GetComponent<SimpleSpriteAnimator>().animation = spriteAnimation;
                    randomObj.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = spriteAnimation.frames[0].sprite;
                }
            }
        }

        private void PickRandomObjectOnAwakeOnOnEnable(On.RoR2.PickRandomObjectOnAwake.orig_OnEnable orig, RoR2.PickRandomObjectOnAwake self)
        {
            orig(self);
            
            GameObject ponchoObj = self.ObjectsToSelect[0].transform.parent?.Find("Poncho")?.gameObject;
            if (UnityEngine.Random.Range(0, 100) <= ponchoOverride.Value)
            {
                if (!ponchoObj)
                {
                    ponchoObj = Instantiate(self.ObjectsToSelect[0], self.ObjectsToSelect[0].transform.parent);
                    ponchoObj.name = "Poncho";
                
                    ponchoObj.transform.GetChild(0).gameObject.GetComponent<SimpleSpriteAnimator>().animation = ponchoSSA;
                    ponchoObj.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = ponchoSSA.frames[0].sprite;
                }
                ponchoObj.SetActive(true);

                foreach (var obj in self.ObjectsToSelect)
                {
                    obj.SetActive(false);
                }

                return;
            }
            
            if(ponchoObj)
                ponchoObj.SetActive(false);
            
            if (!onlyCurrentSurv.Value || !Run.instance) return;

            CharacterMaster master = LocalUserManager.GetFirstLocalUser()?.cachedMaster;
            if (!master) return;
            
            CharacterBody body = master.GetBody();
            string currentSurv = master.bodyPrefab.name;
            if (body)
                currentSurv = body.name;
            
            currentSurv = NameCorrector(currentSurv);

            List<int> possibleSprites = [];
            for (int i = 0; i < self.ObjectsToSelect.Length; i++)
            {
                string objName = self.ObjectsToSelect[i].name;
                self.ObjectsToSelect[i].SetActive(false);
                if (!objName.Contains(currentSurv, StringComparison.CurrentCultureIgnoreCase)) continue;
                
                if (objName.Contains("nem", StringComparison.CurrentCultureIgnoreCase) && !currentSurv.Contains("nem", StringComparison.CurrentCultureIgnoreCase))
                    continue;
                    
                possibleSprites.Add(i);
            }

            if (possibleSprites.Count == 0)
            {
                self.ObjectsToSelect[UnityEngine.Random.Range(0, self.ObjectsToSelect.Length)].SetActive(true);
                return;
            }
            
            self.ObjectsToSelect[possibleSprites[UnityEngine.Random.Range(0, possibleSprites.Count)]].SetActive(true);
        }

        private void CreateBlacklist()
        {
            blacklistBodiesList = [];
            
            string[] bodies = blacklistBodies.Value.Split(',');
            if (bodies.Length == 0) return;
            
            foreach (string bodyName in bodies)
            {
                blacklistBodiesList.Add(bodyName);
            }
        }
        
        private void CreateBodyMatchDict()
        {
            configBodyMatch = new Dictionary<string, string>();
            
            string[] bodies = customBodyDict.Value.Split(',');
            if (bodies.Length == 0 || bodies.Length % 2 != 0) return;
            
            for (int i = 0; i < bodies.Length; i += 2)
            {
                configBodyMatch.Add(bodies[i], bodies[i + 1]);
            }
        }

        private Dictionary<string, string> hardcoded = new()
        {
            { "Croco", "Acrid" },
            { "DroneTech", "Pilot" },
            { "Mage", "Arti" },
            { "NemCommando", "Nemmando" },
            { "HANDOverclocked", "Hand" },
            { "SniperClassic", "Sniper" },
            { "Treebot", "REX" },
        };

        private string NameCorrector(string survName)
        {
            string tempSurvName = survName.Replace("(Clone)", "");
            if (configBodyMatch.TryGetValue(tempSurvName, out string config))
                return config;

            if (blacklistBodiesList.Contains(survName))
                return "null";
            
            tempSurvName = tempSurvName.Replace("Body", "").Replace(" ", "").Replace("2", "");
            if (tempSurvName.Contains("Drone") && survName != "DroneTechBody")
                return "Drone";
            
            return hardcoded.GetValueOrDefault(tempSurvName, tempSurvName);
        }
    }
}
