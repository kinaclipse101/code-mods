using System;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RobItems.Content;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ScarfTweaks
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(RobItems.Plugin.MODUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ChipTier : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "ScarfTweaks";
        private const string PluginVersion = "1.0.0";

        private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");

        private static ChipTier instance;
        private static AssetBundle assetbundle;

        private enum tier 
        {
            common,
            uncommon,
            legendary,
            lunar
        }
        
        public void Awake()
        {
            Log.Init(Logger);
            
            instance = this;
            assetbundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "chipbundle"));
            
            Harmony harmony = new Harmony(Info.Metadata.GUID);
            harmony.CreateClassProcessor(typeof(ChipChanges)).Patch();
            
            ItemCatalog.availability.onAvailable += UpgradeChip;
        }

        private void UpgradeChip()
        {
            ConfigEntry<tier> realtier = instance.Config.Bind("ScarfTweaks", "scarf tier", tier.legendary, "");
            
            string key = realtier.Value switch
            {
                tier.common => RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.Tier1Def_asset,
                tier.uncommon => RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.Tier2Def_asset,
                tier.legendary => RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.Tier3Def_asset,
                tier.lunar => RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.LunarTierDef_asset,
                _ => ""
            };
            string sprite = realtier.Value switch
            {
                tier.common => "tier1",
                tier.uncommon => "tier2",
                tier.legendary => "tier3",
                tier.lunar => "lunar",
                _ => ""
            };
            
            FriendScarf.instance.ItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(key).WaitForCompletion();
            FriendScarf.instance.ItemDef.pickupIconSprite = assetbundle.LoadAsset<Sprite>(sprite); 
        }
        
        [HarmonyPatch]
        public class ChipChanges
        {
            [HarmonyPatch(typeof(FriendHandler), "HandleSize")]
            [HarmonyPostfix]
            public static void HandleSizePostFix(FriendHandler __instance)
            {
                __instance.characterBody.modelLocator.modelTransform.localScale *= Utils.SliderConfig(0, 10, instance.Config.Bind("ScarfTweaks", "chip size multiplier", 2f, "")).Value;
            }

            [HarmonyPatch(typeof(FriendHandler), "Roar")]
            [HarmonyILManipulator]
            public static void RoarIL(ILContext il)
            {
                ILCursor c = new ILCursor(il);
                c.SearchTarget = SearchTarget.Prev;
                
                try
                { 
                    //val.radius = 300f;
                    //IL_0035: ldloc.1
                    //IL_0036: ldc.r4 300
                    //IL_003b: stfld float32 [RoR2]RoR2.BlastAttack::radius

                    if (c.TryGotoNext(x => x.MatchLdloc(1),
                            x => x.MatchLdcR4(300f),
                            x => x.MatchStfld<RoR2.BlastAttack>("radius")))
                    {
                        c.RemoveRange(3);
                        c.Emit(OpCodes.Ldloc_1);
                        c.Emit(OpCodes.Ldc_R4, Utils.SliderConfig(0, 800, instance.Config.Bind("ScarfTweaks", "chip roar far attack radius", 400f, "base 300")).Value);
                        c.Emit(OpCodes.Stfld, typeof(BlastAttack).GetField("radius"));
                    }
                    
                    if (c.TryGotoNext(x => x.MatchLdloc(1),
                            x => x.MatchLdcR4(0.0f),
                            x => x.MatchStfld<RoR2.BlastAttack>("procCoefficient")))
                    {
                        c.RemoveRange(3);
                        c.Emit(OpCodes.Ldloc_1);
                        c.Emit(OpCodes.Ldc_R4, Utils.SliderConfig(0, 10, instance.Config.Bind("ScarfTweaks", "chip roar far attack proc", 1f, "base 0")).Value);
                        c.Emit(OpCodes.Stfld, typeof(BlastAttack).GetField("procCoefficient"));
                    }
                    
                    if (c.TryGotoNext(x => x.MatchLdloc(1),
                            x => x.MatchLdcR4(1f),
                            x => x.MatchStfld<RoR2.BlastAttack>("baseDamage")))
                    {
                        c.RemoveRange(3);
                        c.Emit(OpCodes.Ldloc_1);
                        c.Emit(OpCodes.Ldarg_0);
                        c.EmitDelegate<Func<FriendHandler, float>>((friend) => friend.characterBody.damage * Utils.SliderConfig(0, 40, instance.Config.Bind("ScarfTweaks", "chip roar far attack damage", 15f, "")).Value);
                        c.Emit(OpCodes.Stfld, typeof(BlastAttack).GetField("baseDamage"));
                    }
                    
                    //roar has two blast attacks, this should match for the second one ,,.
                    c.TryGotoNext(x => x.MatchPop());
                    
                    if (c.TryGotoNext(x => x.MatchLdloc(1),
                            x => x.MatchLdcR4(50),
                            x => x.MatchStfld<RoR2.BlastAttack>("radius")))
                    {
                        c.RemoveRange(3);
                        c.Emit(OpCodes.Ldloc_1);
                        c.Emit(OpCodes.Ldc_R4, Utils.SliderConfig(0, 400, instance.Config.Bind("ScarfTweaks", "chip roar close attack radius", 100f, "base 50")).Value);
                        c.Emit(OpCodes.Stfld, typeof(BlastAttack).GetField("radius"));
                    }
                    
                    if (c.TryGotoNext(x => x.MatchLdloc(1),
                            x => x.MatchLdcR4(0.0f),
                            x => x.MatchStfld<RoR2.BlastAttack>("procCoefficient")))
                    {
                        c.RemoveRange(3);
                        c.Emit(OpCodes.Ldloc_1);
                        c.Emit(OpCodes.Ldc_R4, Utils.SliderConfig(0, 10, instance.Config.Bind("ScarfTweaks", "chip roar close attack proc", 1f, "base 0")).Value);
                        c.Emit(OpCodes.Stfld, typeof(BlastAttack).GetField("procCoefficient"));
                    }
                    
                    if (c.TryGotoNext(x => x.MatchLdloc(1),
                            x => x.MatchLdcR4(1f),
                            x => x.MatchStfld<RoR2.BlastAttack>("baseDamage")))
                    {
                        c.RemoveRange(3);
                        c.Emit(OpCodes.Ldloc_1);
                        c.Emit(OpCodes.Ldarg_0);
                        c.EmitDelegate<Func<FriendHandler, float>>((friend) => friend.characterBody.damage * Utils.SliderConfig(0, 40, instance.Config.Bind("ScarfTweaks", "chip roar damage from close attack", 30f, "")).Value);
                        //c.Emit(OpCodes.Ldc_R4, (12 + (TeamManager.instance.GetTeamLevel(TeamIndex.Player) - 1) * 2.4) * 4f);
                        c.Emit(OpCodes.Stfld, typeof(BlastAttack).GetField("baseDamage"));
                    }
                    
                    if (c.TryGotoNext(x => x.MatchLdloc(1),
                            x => x.MatchLdcR4(2000f),
                            x => x.MatchStfld<RoR2.BlastAttack>("baseForce")))
                    {
                        c.RemoveRange(3);
                        c.Emit(OpCodes.Ldloc_1);
                        c.Emit(OpCodes.Ldc_R4, Utils.SliderConfig(0, 8000, instance.Config.Bind("ScarfTweaks", "chip roar close attack force", 4000f, "base 2000")).Value);
                        c.Emit(OpCodes.Stfld, typeof(BlastAttack).GetField("baseForce"));
                    }
                }
                catch (Exception e)
                {
                    Log.Error("error while il patching roar !!!");
                    Log.Error(e);
                }
            }

            private static ConfigEntry<float> cooldown = Utils.SliderConfig(0, 400, instance.Config.Bind("ScarfTweaks", "chip roar cooldown", 30f, ""));
            [HarmonyPatch(typeof(FriendHandler), "FixedUpdate")]
            [HarmonyPostfix]
            public static void FixedUpdatePostfix(FriendHandler __instance)
            {
                if (__instance.roarStopwatch > cooldown.Value)
                {
                    __instance.roarStopwatch = cooldown.Value;
                }
            }
        }

        private void Update()
        {
#if DEBUG
            if (Input.GetKeyUp(KeyCode.F7))
            {
                if (UHRInstalled)
                {
                    Log.Debug(nameof(ChipTier) + ".dll");
                    UHRSupport.hotReload(typeof(ChipTier).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, nameof(ChipTier) + ".dll"));
                }
                else
                {
                    Log.Debug("couldnt finds unity hot reload !!");
                }
            }
#endif  
        }
    }
}
