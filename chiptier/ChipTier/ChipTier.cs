using System;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using R2API;
using RobItems.Content;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ChipTier
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(RobItems.Plugin.MODUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ChipTier : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "ChipTier";
        private const string PluginVersion = "1.0.0";

        private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");

        private static AssetBundle assetbundle;
        
        public void Awake()
        {
            Log.Init(Logger);
            
            assetbundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "asseetbundle_chip"));
            
            Harmony harmony = new Harmony(Info.Metadata.GUID);
            harmony.CreateClassProcessor(typeof(ChipChanges)).Patch();
            
            ItemCatalog.availability.onAvailable += UpgradeChip;
        }

        private void UpgradeChip()
        {
            RobItems.Content.FriendScarf.instance.ItemDef.tier = ItemTier.Tier3;
            RobItems.Content.FriendScarf.instance.ItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.Tier3Def_asset).WaitForCompletion();
            RobItems.Content.FriendScarf.instance.ItemDef.pickupIconSprite = assetbundle.LoadAsset<Sprite>("texIconsTemplate"); 
        }
        
        [HarmonyPatch]
        public class ChipChanges
        {
            [HarmonyPatch(typeof(FriendHandler), "HandleSize")]
            [HarmonyPostfix]
            public static void HandleSizePostFix(FriendHandler __instance)
            {
                __instance.characterBody.modelLocator.modelTransform.localScale *= 2;
            }
            
            [HarmonyPatch(typeof(FriendHandler), "Awake")]
            [HarmonyPostfix]
            public static void AwakePostfix(FriendHandler __instance)
            {
                __instance.roarStopwatch = 30f;
            }
            
           /* [HarmonyPatch(typeof(FriendHandler), "Roar")]
            [HarmonyPostfix]
            public static void RoarPostfix(FriendHandler __instance)
            {
                Log.Debug("roar.,,.");
            }

            [HarmonyPatch(typeof(FriendHandler), "Roar")]
            [HarmonyILManipulator]
            public static void RoarIL(ILContext il)
            {
                ILCursor c = new ILCursor(il);
                c.SearchTarget = SearchTarget.Prev;
                
                try
                {
                    
                      // val.radius = 300f;
                       //IL_0035: ldloc.1
                       //IL_0036: ldc.r4 300
                       //IL_003b: stfld float32 [RoR2]RoR2.BlastAttack::radius
                     

                    if (c.TryGotoNext(x => x.MatchLdloc(1),
                            x => x.MatchLdcR4(300f),
                            x => x.MatchStfld<RoR2.BlastAttack>("radius")))
                    {
                        Log.Debug(c);
                        c.RemoveRange(3);
                        c.Emit(OpCodes.Ldarg_0);
                        c.Emit(OpCodes.Ldc_R4, 400f);
                        c.Emit(OpCodes.Stfld, typeof(BlastAttack).GetField("radius"));
                    }
                    else
                    {
                        Log.Error("Couldn't match radius for first blast attack on chip !!!");
                    }
                    
                    if (c.TryGotoNext(x => x.MatchLdloc(1),
                            x => x.MatchLdcR4(0.0f),
                            x => x.MatchStfld<RoR2.BlastAttack>("procCoefficient")))
                    {
                        Log.Debug(c);
                        c.RemoveRange(3);
                        c.Emit(OpCodes.Ldarg_0);
                        c.Emit(OpCodes.Ldc_R4, 1f);
                        c.Emit(OpCodes.Stfld, typeof(BlastAttack).GetField("procCoefficient"));
                    }
                    else
                    {
                        Log.Error("Couldn't match proc coeff for first blast attack on chip !!!");
                    }
                    
                    if (c.TryGotoNext(x => x.MatchLdloc(1),
                            x => x.MatchLdcR4(1f),
                            x => x.MatchStfld<RoR2.BlastAttack>("baseDamage")))
                    {
                        Log.Debug(c);
                        c.RemoveRange(3);
                        c.Emit(OpCodes.Ldarg_0);
                        c.Emit(OpCodes.Ldc_R4, 20f);
                        c.Emit(OpCodes.Stfld, typeof(BlastAttack).GetField("baseDamage"));
                    }
                    else
                    {
                        Log.Error("Couldn't match damage for first blast attack on chip !!!");
                    }
                    
                    //roar has two blast attacks, this should match for the second one ,,.
                    c.TryGotoNext(x => x.MatchPop());
                    
                    if (c.TryGotoNext(x => x.MatchLdloc(1),
                            x => x.MatchLdcR4(50),
                            x => x.MatchStfld<RoR2.BlastAttack>("radius")))
                    {
                        Log.Debug(c);
                        c.RemoveRange(3);
                        c.Emit(OpCodes.Ldarg_0);
                        c.Emit(OpCodes.Ldc_R4, 100f);
                        c.Emit(OpCodes.Stfld, typeof(BlastAttack).GetField("radius"));
                    }
                    else
                    {
                        Log.Error("Couldn't match radius for second blast attack on chip !!!");
                    }
                    
                    if (c.TryGotoNext(x => x.MatchLdloc(1),
                            x => x.MatchLdcR4(0.0f),
                            x => x.MatchStfld<RoR2.BlastAttack>("procCoefficient")))
                    {
                        Log.Debug(c);
                        c.RemoveRange(3);
                        c.Emit(OpCodes.Ldarg_0);
                        c.Emit(OpCodes.Ldc_R4, 1f);
                        c.Emit(OpCodes.Stfld, typeof(BlastAttack).GetField("procCoefficient"));
                    }
                    else
                    {
                        Log.Error("Couldn't match proc coeff for second blast attack on chip !!!");
                    }
                    
                    if (c.TryGotoNext(x => x.MatchLdloc(1),
                            x => x.MatchLdcR4(1f),
                            x => x.MatchStfld<RoR2.BlastAttack>("baseDamage")))
                    {
                        Log.Debug(c);
                        c.RemoveRange(3);
                        c.Emit(OpCodes.Ldarg_0);
                        c.Emit(OpCodes.Ldc_R4, 30f);
                        c.Emit(OpCodes.Stfld, typeof(BlastAttack).GetField("baseDamage"));
                    }
                    else
                    {
                        Log.Error("Couldn't match damage for second blast attack on chip !!!");
                    }
                    
                    if (c.TryGotoNext(x => x.MatchLdloc(1),
                            x => x.MatchLdcR4(2000f),
                            x => x.MatchStfld<RoR2.BlastAttack>("baseForce")))
                    {
                        Log.Debug(c);
                        c.RemoveRange(3);
                        c.Emit(OpCodes.Ldarg_0);
                        c.Emit(OpCodes.Ldc_R4, 4000f);
                        c.Emit(OpCodes.Stfld, typeof(BlastAttack).GetField("baseForce"));
                    }
                    else
                    {
                        Log.Error("Couldn't match force for second blast attack on chip !!!");
                    }
                    
                    Log.Debug(il);
                }
                catch (Exception e)
                {
                    Log.Error("error while il patching roar !!!");
                    Log.Error(e);
                }
            }*/
            
            [HarmonyPatch(typeof(FriendHandler), "FixedUpdate")]
            [HarmonyILManipulator]
            public static void FixedUpdateIL(ILContext il)
            {
                ILCursor c = new ILCursor(il);
                c.SearchTarget = SearchTarget.Prev;
                
                try
                {
                    /*
                     *  // [47 5 - 47 29]
                          IL_0058: ldarg.0      // this
                          IL_0059: ldc.r4       60
                          IL_005e: stfld        float32 RobItems.Content.FriendHandler::roarStopwatch
                     */

                    if (c.TryGotoNext(x => x.MatchLdarg(0),
                            x => x.MatchLdcR4(60),
                            x => x.MatchStfld<FriendHandler>("roarStopwatch")))
                    {
                        c.RemoveRange(3);
                        c.Emit(OpCodes.Ldarg_0);
                        c.Emit(OpCodes.Ldc_R4, 30f);
                        c.Emit(OpCodes.Stfld, typeof(FriendHandler).GetField("roarStopwatch"));
                    }
                    else
                    {
                        Log.Error("Couldn't match fast roar !!!");
                    }
                    
                    Log.Debug(il);
                }
                catch (Exception e)
                {
                    Log.Error("error while il patching roar !!!");
                    Log.Error(e);
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
            
            if (Input.GetKeyUp(KeyCode.I))
            {
                
            }
#endif  
        }
    }
}
