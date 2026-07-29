using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HG;
using RoR2;
using RoR2.Navigation;
using UnityEngine;
using SceneDirector = On.RoR2.SceneDirector;

namespace TooLittleSpawns
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class TooLittleSpawns : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "TooLittleSpawns";
        private const string PluginVersion = "1.0.0";

        private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");
        private static bool ROOInstalled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
        
        private static ConfigEntry<int> playerBehavior;
        public static ConfigEntry<bool> debug;
        public void Awake()
        {
            Log.Init(Logger);

            playerBehavior = Config.Bind("TooLittleSpawns",
                "Max players needed to use new behavior",
                4,
                "Max players limit to determine whether or not to use new behavior. Note that this is the limit, not the amount of players.");
            if (ROOInstalled)
            {
                ROOSupport.SliderConfig(1, 16, playerBehavior);
            }
            
            debug = Config.Bind("TooLittleSpawns",
                "Log debug",
                false,
                "Whether to use debug logging or not. Logs whether a spawn is using a spawnpoint, creating it from the nodegraph and other stuff if you're paranoid about it working or something");
            if (ROOInstalled)
            {
                ROOSupport.CheckboxConfig(debug);
            }

            On.RoR2.SceneDirector.PlaceTeleporter += SceneDirectorOnPlaceTeleporter;
            On.RoR2.SpawnPoint.ConsumeSpawnPoint += SpawnPointOnConsumeSpawnPoint;
        }

        private void SceneDirectorOnPlaceTeleporter(SceneDirector.orig_PlaceTeleporter orig, RoR2.SceneDirector self)
        {
            orig(self);
            teleporterObject = self.teleporterInstance;
        }

        private GameObject teleporterObject;
        private SpawnPoint initialSpawn;
        private List<SpawnPoint> newSpawns = [];

        private SpawnPoint SpawnPointOnConsumeSpawnPoint(On.RoR2.SpawnPoint.orig_ConsumeSpawnPoint orig)
        {
            if (RoR2Application.maxPlayers <= 4)
            {
                return orig();
            }
            
            Log.Debug("entering !");
            
            if (!initialSpawn)
            {
                Log.Debug("creating initial spawn .,,.");
                List<SpawnPoint> spawnPoints = new List<SpawnPoint>(SpawnPoint.readOnlyInstancesList);
                spawnPoints.Sort((SpawnPoint a, SpawnPoint b) => (teleporterObject.transform.position).sqrMagnitude.CompareTo((teleporterObject.transform.position - b.transform.position).sqrMagnitude));
                initialSpawn = spawnPoints[^1];
                initialSpawn.consumed = true;
                newSpawns.Add(initialSpawn);
                return initialSpawn;
            }

            SpawnPoint spawnPoint = null;
            float minDist = float.MaxValue;
            foreach (SpawnPoint spawn in SpawnPoint.readOnlyInstancesList)
            {
                if (spawn.consumed) continue;
                    
                float dist = Vector3.Distance(spawn.gameObject.transform.position, initialSpawn.gameObject.transform.position);
                if (dist < minDist && dist < 40)
                {
                    spawnPoint = spawn;
                    minDist = dist;
                }
            }
            Log.Debug("bwa");

            if (!spawnPoint)
            {
                NodeGraph groundNodes = SceneInfo.instance.groundNodes;
                NodeFlags requiredFlags = NodeFlags.None;
                NodeFlags nodeFlags = NodeFlags.None;
                nodeFlags |= NodeFlags.NoCharacterSpawn;
                List<NodeGraph.NodeIndex> list = groundNodes.GetActiveNodesForHullMaskWithFlagConditions(HullMask.Golem, requiredFlags, nodeFlags);

                for (int i = 0; i < list.Count; i++)
                {
                    if (!RoR2.SceneDirector.IsNodeSuitableForPod(groundNodes, list[i]))
                    {
                        groundNodes.GetNodePosition(list[i], out var position2);
                        list.RemoveAt(i);
                    }
                }
                    
                if (PlayerSpawnInhibitor.readOnlyInstancesList.Count > 0)
                {
                    List<NodeGraph.NodeIndex> list2 = new List<NodeGraph.NodeIndex>();
                    for (int i = 0; i < list.Count; i++)
                    {
                        bool flag = false;
                        foreach (PlayerSpawnInhibitor readOnlyInstances in PlayerSpawnInhibitor.readOnlyInstancesList)
                        {
                            if (readOnlyInstances.IsInhibiting(groundNodes, list[i]))
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            list2.Add(list[i]);
                        }
                    }
                    if (list2.Count > 0)
                    {
                        list = list2;
                    }
                }

                minDist = float.MaxValue;
                NodeGraph.NodeIndex? spawnNode = null;
                foreach (NodeGraph.NodeIndex nodeIndex in list)
                {
                    groundNodes.GetNodePosition(nodeIndex, out var nodePos);

                    bool consumed = false;
                    foreach (SpawnPoint spawn in newSpawns)
                    {
                        if (spawn && spawn.gameObject.transform.position == nodePos)
                        {
                            Log.Debug($"already spawned point at {nodePos} ! skipping ,.,.");
                            consumed = true;
                        }

                        if (spawn && Vector3.Distance(spawn.gameObject.transform.position, nodePos) < 5)
                        {
                            Log.Debug($"spawnpoint at {nodePos} to close to existing ! skipping ,.,.");
                            consumed = true;
                        }
                    }
                    if (consumed) continue;
                        
                    float dist = Vector3.Distance(nodePos, initialSpawn.gameObject.transform.position);
                    if (dist < minDist)
                    {
                        spawnNode = nodeIndex;
                        minDist = dist;
                    }
                }
                    
                Log.Debug($"list length {list.Count} ");

                if (!spawnNode.HasValue)
                {
                    Log.Error("new spawn node is null ! gorp ,.., ");
                    return SpawnPoint.readOnlyInstancesList[0];
                }
                    
                groundNodes.GetNodePosition(spawnNode.Value, out var position);
                List<NodeGraph.LinkIndex> linkedNodes = CollectionPool<NodeGraph.LinkIndex, List<NodeGraph.LinkIndex>>.RentCollection();
                groundNodes.GetActiveNodeLinks(spawnNode.Value, linkedNodes);
                Quaternion rotation;
                if (linkedNodes.Count > 0)
                {
                    NodeGraph.LinkIndex linkIndex = Run.instance.spawnRng.NextElementUniform(linkedNodes);
                    groundNodes.GetNodePosition(groundNodes.GetLinkEndNode(linkIndex), out var position2);
                    rotation = Util.QuaternionSafeLookRotation(position2 - position);
                }
                else
                {
                    rotation = Quaternion.Euler(0f, Run.instance.spawnRng.nextNormalizedFloat * 360f, 0f);
                }
                    
                GameObject objectSpawn = Object.Instantiate(SpawnPoint.prefab, position, rotation);
                spawnPoint = objectSpawn.GetComponent<SpawnPoint>();
                newSpawns.Add(spawnPoint);
                    
                Log.Warning($"finalized spawn from nodegraph {objectSpawn.transform.position} !");
            }
            else
            {
                Log.Warning($"got spawn from spawnlist {spawnPoint.gameObject.transform.position}");
            }
            
            spawnPoint.consumed = true;
            return spawnPoint;
        }
        

        private void Update()
        {
#if DEBUG
            if (Input.GetKeyUp(KeyCode.F7))
            {
                if (UHRInstalled)
                {
                    Log.Debug(nameof(TooLittleSpawns) + ".dll");
                    UHRSupport.hotReload(typeof(TooLittleSpawns).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, nameof(TooLittleSpawns) + ".dll"));
                }
                else
                {
                    Log.Debug("couldnt finds unity hot reload !!");
                }
            }
            
            if (Input.GetKeyUp(KeyCode.I))
            {
                RoR2.Stage.instance.RespawnCharacter(PlayerCharacterMasterController.instances[0].master);
            }
#endif  
        }
    }
}
