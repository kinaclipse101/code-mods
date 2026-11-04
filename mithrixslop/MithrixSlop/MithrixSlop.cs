using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;

namespace MithrixSlop
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MithrixSlop : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "MithrixSlop";
        private const string PluginVersion = "1.0.1";

        private static ConfigEntry<int> karmaMultiplier;
        private static ConfigEntry<bool> karmaMultiplierUseAICount;
        private static ConfigEntry<bool> useWithoutAI;
        public static GameObject massProjectilePrefab;
        public static GameObject massPilarModified;
        public static GameObject purchaseLockBlue;
        public static GameObject ppEffect;
        public static PostProcessProfile ppp;
        public static BrotherUltPattern pizzaFissure;
        private List<BrotherUltPattern> pizzaPatterns = [];
        public static CharacterBody braindeadmithrix5000unitsunderthemap;
        
        public void Awake()
        {
            Log.Init(Logger);
            
            karmaMultiplier = Config.Bind("MithrixSlop",
                "karma multiplier .,.,",
                1,
                "if aigen mods are installed ,.. ,amount to multiply karma increases by ,.,., wont go under 2 if they are .,.,.");
            karmaMultiplier.SettingChanged += KarmaMultiplierOnSettingChanged;
            void KarmaMultiplierOnSettingChanged(object sender, EventArgs e)
            {
                if (karmaMultiplier.Value < 0)
                    karmaMultiplier.Value = 1;
                if (ThunderstoreInterface.ThunderstoreInterface.aigenMods.Count > 0 && karmaMultiplier.Value <= 1)
                    karmaMultiplier.Value = 2;
            }
            KarmaMultiplierOnSettingChanged(null, null);

            karmaMultiplierUseAICount = Config.Bind("MithrixSlop",
                "increase karma by count of aigen mods  .,.,",
                true,
                "if aigen mods are installed ,.. ,karma count doesnt use multiplier and instead uses amount of aigen mods you have installed ,.,.,");
           
            useWithoutAI = Config.Bind("MithrixSlop",
                "use karma additions without ai .,.,",
                true,
                "applies karma additions without needing ai mods installed ,... will also not change lang stuff ,.,.,");

            Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VendingMachine/VendingMachineProjectile.prefab").Completed += handle =>
            {
                massProjectilePrefab = handle.Result.InstantiateClone("MassProjectilePrefab", true);

                massProjectilePrefab.GetComponent<TeamFilter>().defaultTeam = TeamIndex.Lunar;
                Destroy(massProjectilePrefab.GetComponent<OnDisableEvent>());
                Destroy(massProjectilePrefab.GetComponent<ProjectileInstantiateDeployable>());
                massProjectilePrefab.AddComponent<OnDisableEvent>();
                massProjectilePrefab.AddComponent<MassPillar>();
                massProjectilePrefab.GetComponent<ProjectileImpactExplosion>().lifetime = 1f;

                ContentAddition.AddProjectile(massProjectilePrefab);
            };
            Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_moon2.MoonBatteryMass_prefab).Completed += handle =>
            {
                massPilarModified = handle.Result.InstantiateClone("MassPillarLockTele", true);
                massPilarModified.AddComponent<PillarLockCommunicator>();
            };
            Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Teleporters.PurchaseLock_prefab).Completed += handle =>
            {
                purchaseLockBlue = handle.Result.InstantiateClone("purchaseLockBlue", true);
                var ps = purchaseLockBlue.GetComponent<ParticleSystem>();
                var main = ps.main;
                main.startColor = (Color)(new Color32(3, 220, 245, 255));

                Material newProngMat = null;
                Transform prongs = purchaseLockBlue.transform.Find("Prongs");
                for (int i = 0; i < prongs.childCount; i++)
                {
                    MeshRenderer prong = prongs.GetChild(i).gameObject.GetComponent<MeshRenderer>();
                    if (!newProngMat)
                    {
                        newProngMat = Instantiate(prong.material);
                        newProngMat.SetTexture(RemapTex, Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_ColorRamps.texRampIce_png).WaitForCompletion());
                        newProngMat.SetColor(TintColor, new Color32(0, 109, 148, 255));
                    }
                    prong.material = newProngMat;
                }
            };
            Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/LunarGolem/cscLunarGolem.asset").Completed += handle => { golemSpawnCard = handle.Result; };
            Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/LunarWisp/cscLunarWisp.asset").Completed += handle => { wispSpawnCard = handle.Result; };
            Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Meteor/MeteorStrikeImpact.prefab").Completed += handle => { impactEffectPrefab = handle.Result; };
            Addressables.LoadAssetAsync<BrotherUltPattern>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Brother.Ult_plusSign_asset).Completed += handle => { pizzaPatterns.Add(handle.Result); pizzaFissure = handle.Result; };
            Addressables.LoadAssetAsync<BrotherUltPattern>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Brother.Ult_clock_asset).Completed += handle => { pizzaPatterns.Add(handle.Result); };
            Addressables.LoadAssetAsync<BrotherUltPattern>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Brother.Ult_scissor_asset).Completed += handle => { pizzaPatterns.Add(handle.Result); };
            Addressables.LoadAssetAsync<BrotherUltPattern>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Brother.Ult_passiveAttack_asset).Completed += handle => { pizzaPatterns.Add(handle.Result); };
            Addressables.LoadAssetAsync<BrotherUltPattern>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Brother.Ult_singleBlast_asset).Completed += handle => { pizzaPatterns.Add(handle.Result); };
            Addressables.LoadAssetAsync<BrotherUltPattern>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Brother.Ult_defaultRotatingLeft_asset).Completed += handle => { pizzaPatterns.Add(handle.Result); };
            Addressables.LoadAssetAsync<BrotherUltPattern>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Brother.Ult_defaultRotatingRight_asset).Completed += handle => { pizzaPatterns.Add(handle.Result); };
           
            ppEffect = PrefabAPI.CreateEmptyPrefab("karmaBlue", true); // he h,.., its called my jaroni dont wantto build an asset bundle ,..,
            PostProcessVolume ppv = ppEffect.AddComponent<PostProcessVolume>();
            ppv.priority = 2.2f;
            ppv.isGlobal = true;
            PostProcessDuration ppd = ppEffect.AddComponent<PostProcessDuration>();
            ppd.ppVolume = ppv;
            ppd.destroyOnEnd = true;
            ppd.maxDuration = 2f;
            ppd.ppWeightCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            ppEffect.layer = (int)LayerIndex.postProcess;
            
            ppp = ScriptableObject.CreateInstance<PostProcessProfile>();
            ColorGrading color = ppp.AddSettings<ColorGrading>();
            color.postExposure.value = 0.1f;
            color.postExposure.overrideState = true;
            color.contrast.value = 40f;
            color.contrast.overrideState = true;
            color.gain.value = new Vector4(0.83f, 1.01f, 1.35f);
            color.gain.overrideState = true;
            color.enabled.value = true;
            color.active = true;
            Vignette vig = ppp.AddSettings<Vignette>();
            vig.mode.value = VignetteMode.Classic;
            vig.mode.overrideState = true;
            vig.intensity.value = 0.2f;
            vig.intensity.overrideState = true;
            vig.color.value = new Color32(44, 62, 144, 255);
            vig.color.overrideState = true;
            vig.enabled.value = true;
            vig.active = true;

            ppp.name = "KarmaBlueProfile";
            ppv.sharedProfile = ppp;

            if (useWithoutAI.Value) 
                Hook();
            
            ThunderstoreInterface.ThunderstoreInterface.containsAI += () =>
            {
                Log.Fatal("yuck ,.,,.");
                
                if (!useWithoutAI.Value) 
                    Hook();

                KarmaMultiplierOnSettingChanged(null, null);
                
                LanguageAPI.Add("NOTIFICATION_KARMA_INCREASED", "The Moon is disgusted by your slop."); 
                LanguageAPI.Add("NOTIFICATION_KARMA_INCREASED_MAXKARMA", "The Moon doesn't want to be tarnished by your slop.");
            };
        }

        private void Hook()
        {
            On.RoR2.KarmaManager.GetEffectiveKarmaMultiplier += KarmaManagerOnGetEffectiveKarmaMultiplier;
            On.RoR2.UI.KarmaIcon.OnKarmaChanged += KarmaIconOnOnKarmaChanged;
            On.RoR2.TeleporterInteraction.OnBossDirectorSpawnedMonsterServer += TeleporterInteractionOnOnBossDirectorSpawnedMonsterServer;
            KarmaManager.onKarmaChangedServer += KarmaManagerOnonKarmaChangedServer;
            GlobalEventManager.onCharacterDeathServer += GlobalEventManagerOnonCharacterDeathServer;
            IL.RoR2.KarmaManager.SetKarmaServer += KarmaManagerOnSetKarmaServer;
            On.RoR2.CombatDirector.Spawn_SpawnCard_EliteDef_Transform_MonsterSpawnDistance_bool_float_PlacementMode_bool += CombatDirectorOnSpawnPerfected;
            RoR2.Stage.onStageStartGlobal += SpawnMasterlessMithry;
        }

        private void SpawnMasterlessMithry(Stage obj)
        {
            if (NetworkServer.active)
            {
                if (!braindeadmithrix5000unitsunderthemap)
                {
                    GameObject brotherBody = Instantiate(BodyCatalog.FindBodyPrefab("BrotherBody"));
                    Destroy(brotherBody.GetComponent<KinematicCharacterController.KinematicCharacterMotor>());
                    brotherBody.transform.position = new Vector3(brotherBody.transform.position.x, brotherBody.transform.position.y - 500000, brotherBody.transform.position.z);
                    brotherBody.GetComponent<CharacterMotor>().enabled = false;
                    braindeadmithrix5000unitsunderthemap = brotherBody.GetComponent<CharacterBody>();
                    NetworkServer.Spawn(brotherBody);
                }
            }
        }

        private void GlobalEventManagerOnonCharacterDeathServer(DamageReport damageReport)
        {
            if (Run.instance.lastCachedKarma <= KarmaManager.maxKarma) return;
            if (damageReport.victimBody?.inventory?.currentEquipmentIndex == RoR2Content.Equipment.AffixLunar?.equipmentIndex || Run.instance.spawnRng.RangeFloat(0, 100) <= (Run.instance.lastCachedKarma - KarmaManager.maxKarma) * 2 )
            {
                BrotherUltUtils.EvaluatePattern(pizzaPatterns[UnityEngine.Random.Range(0, pizzaPatterns.Count)], new BrotherUltUtils.BrotherUltContext
                {
                    owner = braindeadmithrix5000unitsunderthemap.gameObject,
                    target = damageReport.attacker,
                    originOverride = damageReport.victimBody.footPosition,
                    useOriginOverride = true
                });    
            }
        }

        private void KarmaManagerOnonKarmaChangedServer(KarmaManager karmaManager, int newKarma)
        {
            if(Run.instance.lastCachedKarma <= KarmaManager.maxKarma) return;
            
            GameObject postProcessing = Instantiate(ppEffect);
            NetworkServer.Spawn(postProcessing);
            Log.Debug("spawned ppv");
            
            if (!(Run.instance.spawnRng.RangeFloat(0, 100) <= 20 + (Run.instance.lastCachedKarma - KarmaManager.maxKarma) * 5)) return;
            
            foreach (var pcmc in PlayerCharacterMasterController._instances)
            {
                if (pcmc.body)
                {
                    FireMassPillar(pcmc.body);
                }
            }
        }

        private void TeleporterInteractionOnOnBossDirectorSpawnedMonsterServer(On.RoR2.TeleporterInteraction.orig_OnBossDirectorSpawnedMonsterServer orig, RoR2.TeleporterInteraction self, GameObject masterobject)
        {
            orig(self, masterobject);

            if (Run.instance.lastCachedKarma <= KarmaManager.maxKarma) return;
            
            masterobject.GetComponent<CharacterMaster>().onBodyStart += body =>
            {
                if (Run.instance.spawnRng.RangeFloat(0, 100) <= 15 + (Run.instance.lastCachedKarma - KarmaManager.maxKarma) * 5)
                {
                    SceneInfo.instance.groundNodes.GetNodePosition(SceneInfo.instance.groundNodes.FindClosestNode(new Vector3(body.corePosition.x + UnityEngine.Random.Range(-100, 100), body.corePosition.y + UnityEngine.Random.Range(-100, 100), body.corePosition.z + UnityEngine.Random.Range(-100, 100)), HullClassification.Golem), out var newPos);
                    if(newPos != Vector3.zero)
                        SpawnChimera(newPos);
                }
            };
        }
        
        private void FireMassPillar(CharacterBody body)
        {
            Ray ray = new Ray(body.inputBank.aimOrigin, Vector3.down);
            if (Util.CharacterRaycast(base.gameObject, ray, out var hitInfo, 1000f, LayerIndex.world.mask, QueryTriggerInteraction.UseGlobal))
            {
                ProjectileManager.instance.FireProjectileWithoutDamageType(massProjectilePrefab, hitInfo.point, Quaternion.identity, null, body.damage, 0f, Util.CheckRoll(body.crit, body.master));
            }
        }

        private bool CombatDirectorOnSpawnPerfected(On.RoR2.CombatDirector.orig_Spawn_SpawnCard_EliteDef_Transform_MonsterSpawnDistance_bool_float_PlacementMode_bool orig, CombatDirector self, SpawnCard spawncard, EliteDef elitedef, Transform spawntarget, DirectorCore.MonsterSpawnDistance spawndistance, bool preventoverhead, float valuemultiplier, DirectorPlacementRule.PlacementMode placementmode, bool singlescaledboss)
        {
            if (Run.instance.lastCachedKarma <= KarmaManager.maxKarma) return orig(self, spawncard, elitedef, spawntarget, spawndistance, preventoverhead, valuemultiplier, placementmode, singlescaledboss);

            if (NetworkServer.active)
            {
                if (Run.instance.spawnRng.RangeFloat(0, 100) <= (Run.instance.lastCachedKarma - KarmaManager.maxKarma) * 2)
                {
                    elitedef = RoR2Content.Elites.Lunar;
                }

                bool origReturn = orig(self, spawncard, elitedef, spawntarget, spawndistance, preventoverhead, valuemultiplier, placementmode, singlescaledboss);
                if (origReturn)
                {
                    if (Run.instance.spawnRng.RangeFloat(0, 100) <= (Run.instance.lastCachedKarma - KarmaManager.maxKarma) * 3)
                    {
                        SceneInfo.instance.groundNodes.GetNodePosition(SceneInfo.instance.groundNodes.FindClosestNode(new Vector3(spawntarget.position.x + UnityEngine.Random.Range(-50, 50), spawntarget.position.y + UnityEngine.Random.Range(-25, 25), spawntarget.position.z + UnityEngine.Random.Range(-50, 50)), HullClassification.Golem), out var newPos);
                        if(newPos != Vector3.zero)
                            SpawnChimera(newPos);
                    }

                    return true;
                }
                
            }
            
            return orig(self, spawncard, elitedef, spawntarget, spawndistance, preventoverhead, valuemultiplier, placementmode, singlescaledboss);
        }
        
        private void KarmaIconOnOnKarmaChanged(On.RoR2.UI.KarmaIcon.orig_OnKarmaChanged orig, RoR2.UI.KarmaIcon self, KarmaManager karmamanager, int karma)
        {
            orig(self, karmamanager, Math.Clamp(karma, 0, KarmaManager.maxKarma));

            if (karma > RoR2.KarmaManager.maxKarma)
            {
                GameObject additional = self.gameObject.transform.Find("Holder")?.Find("Addition")?.gameObject;
                if (!additional)
                {
                    additional = new GameObject("Addition", typeof(HGTextMeshProUGUI));
                    additional.transform.SetParent(self.gameObject.transform.Find("Holder"), false);
                }

                RectTransform rectTransform = additional.GetComponent<RectTransform>();
                rectTransform.localPosition = new Vector3(108, -19.5f, 0);
                
                HGTextMeshProUGUI text = additional.GetComponent<HGTextMeshProUGUI>();
                text.text = "+" + (karma - RoR2.KarmaManager.maxKarma);
                text.fontSize = 10;
                text.color = new Color32(134, 185, 255, 255);
            }
        }

        private void KarmaManagerOnSetKarmaServer(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            
            /*
             * IL_001b: ldsfld int32 RoR2.KarmaManager::maxKarma
             */

            if (c.TryGotoNext(MoveType.Before, x => x.MatchLdsfld(typeof(RoR2.KarmaManager), "maxKarma")))
            {
                c.Remove();
                c.Emit(OpCodes.Ldc_I4, 99999);  // :nikodurr: ,.,
            }
        }

        private int KarmaManagerOnGetEffectiveKarmaMultiplier(On.RoR2.KarmaManager.orig_GetEffectiveKarmaMultiplier orig, RoR2.KarmaManager self)
        {
            return orig(self) * (self.isKarmaEnabled ? (karmaMultiplierUseAICount.Value ? ThunderstoreInterface.ThunderstoreInterface.aigenMods.Count + 1 : karmaMultiplier.Value) : 1);
        }

        /*public void FixedUpdate()
        {
            if (Input.GetKeyUp(KeyCode.U))
            {
                ThunderstoreInterface.cachedInfos.Clear();
                ThunderstoreInterface.thunderstoreInfos.Clear();
                ThunderstoreInterface.TryLogPluginThunderstoreManifest();
            }
        }*/
        
        private CharacterSpawnCard golemSpawnCard;
        private CharacterSpawnCard wispSpawnCard;
        public static GameObject impactEffectPrefab;
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int TintColor = Shader.PropertyToID("_TintColor");
        private static readonly int RemapTex = Shader.PropertyToID("_RemapTex");

        //stealing from primal birthright code hehe .,., 
        public void SpawnChimera(Vector3 position)
        {
            CharacterSpawnCard spawnCard = golemSpawnCard;
            if (Run.instance.spawnRng.RangeFloat(0, 100) < 50)
            {
                spawnCard = wispSpawnCard;
            }
            
            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(
                spawnCard,
                new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Direct,
                    position = position
                },
                RoR2Application.rng
            );
            spawnRequest.teamIndexOverride = TeamIndex.Monster;
            spawnRequest.onSpawnedServer += result =>
            {
                // i had a single NRE on client while testing i dont trust spawnedserver anymore ,.. 
                if (!NetworkServer.active) return;
                
                CharacterMaster golemMaster = result.spawnedInstance?.GetComponent<CharacterMaster>();
                if (!golemMaster) return;
                
                golemMaster.inventory.GiveItemPermanent(RoR2Content.Items.BoostDamage, Run.instance.lastCachedKarma);
                golemMaster.inventory.GiveItemPermanent(RoR2Content.Items.BoostHp, Run.instance.lastCachedKarma);
                golemMaster.inventory.GiveItemPermanent(RoR2Content.Items.BoostAttackSpeed, Run.instance.lastCachedKarma);

                if (Run.instance.spawnRng.RangeFloat(0, 100) <= (Run.instance.lastCachedKarma - KarmaManager.maxKarma) * 2)
                {
                    EquipmentState equipmentState = new EquipmentState(RoR2Content.Equipment.AffixLunar.equipmentIndex, new Run.FixedTimeStamp(RoR2.Run.instance.fixedTime), 1);
                    golemMaster.inventory.SetEquipment(equipmentState, 0);
                }
                
                //basically just copy how combat director rewards stuff .,,. 
                DeathRewards deathRewards = golemMaster.GetBodyObject()?.GetComponent<DeathRewards>();
                if (!deathRewards) return;
                
                float total = 6f; //this is around 12 gold which isnt much but shouldnt be rewarded too much ,.,. 
                deathRewards.spawnValue = (int)Mathf.Max(1f, total);
         
                deathRewards.expReward = (uint)Mathf.Max(1f, total * Run.instance.compensatedDifficultyCoefficient);
                deathRewards.goldReward = (uint)Mathf.Max(1f, total * 2f * Run.instance.compensatedDifficultyCoefficient);
            };
            
            DirectorCore.instance.TrySpawnObject(spawnRequest);
            
            EffectData effectData = new EffectData
            {
                origin = position
            };
            EffectManager.SpawnEffect(impactEffectPrefab, effectData, transmit: true);
        
            //lunar golem base damage + level damage .,., not magic numbers i promise !!
            uint damage = 35 + 7 * TeamManager.instance.GetTeamLevel(TeamIndex.Monster);
            BlastAttack blastAttack = new BlastAttack
            {
                inflictor = base.gameObject,
                baseDamage = damage,
                baseForce = 400,
                attackerFiltering = AttackerFiltering.Default,
                crit = false,
                falloffModel = BlastAttack.FalloffModel.Linear,
                attacker = null,
                bonusForce = Vector3.zero,
                damageColorIndex = DamageColorIndex.Default,
                position = position,
                procChainMask = default,
                procCoefficient = 1f,
                teamIndex = TeamIndex.Monster,
                radius = 40f,
            };

            blastAttack.Fire();
        }
    }
}
