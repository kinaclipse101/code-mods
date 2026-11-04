using BepInEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BepInEx.Bootstrap;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace ThunderstoreInterface
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ThunderstoreInterface : BaseUnityPlugin
    {
        private const string PluginGUID = "zzz." + PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "ThunderstoreInterface";
        private const string PluginVersion = "1.0.0";
        
        private static int modCount;
        
        private static List<ThunderstoreInfo> cachedInfos = [];
        public static List<ThunderstoreInfo> thunderstoreInfos = [];
        public static Action finishedAllMods;
        public static Action containsAI;
        public static List<ThunderstoreInfo> aigenMods = [];

        public void Awake()
        {
            Log.Init(Logger);
            GetThunderstoreInfos();
        }
        
        public static async void GetThunderstoreInfos()
        {
            modCount = Chainloader.PluginInfos.Values.Count;
            
            try
            {
                if (File.Exists(Path.Combine(Environment.CurrentDirectory, "ThunderstoreInfos.json")) && cachedInfos.Count == 0)
                {
                    Log.Debug("json exists, loading");
                    cachedInfos = JsonConvert.DeserializeObject<List<ThunderstoreInfo>>(await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, "ThunderstoreInfos.json")));
                }
            }
            catch (Exception e)
            {
                Log.Warning("failed to read cached json !");
                Log.Warning(e);
            }
            
            finishedAllMods += () =>
            {
                foreach (var tsInfo in thunderstoreInfos)
                {
                    if (!tsInfo.packageData.CommumityListings[0].Categories.Contains("AI Generated")) continue; 
                    aigenMods.Add(tsInfo);
                }

                if (aigenMods.Count > 0)
                    containsAI?.Invoke();
            };
            
            try
            {
                foreach (var pluginInfo in Chainloader.PluginInfos.Values)
                {
                    bool skipGUID = false;
                    foreach (var thunderstoreInfo in cachedInfos)
                    {
                        if (thunderstoreInfo.GUID != pluginInfo.Metadata.GUID) continue;
                    
                        //Log.Debug("found guid in json " + thunderstoreInfo.GUID);
                        thunderstoreInfos.Add(thunderstoreInfo);
                        skipGUID = true;
                        LowerModCount();
                        break;
                    }
                    if (skipGUID)
                        continue;
                
                    const string ThunderstoreManifestJsonFileName = "manifest.json";
                    string manifestPath = null;

                    var currentFolder = new DirectoryInfo(Path.GetDirectoryName(pluginInfo.Location)!);

                    var count = 0;
                    while (count < 10)
                    {
                        if (currentFolder.FullName == Paths.PluginPath || currentFolder.Parent == null)
                        {
                            break;
                        }

                        var potentialManifestPath = Path.Combine(currentFolder.FullName, ThunderstoreManifestJsonFileName);
                        if (File.Exists(potentialManifestPath))
                        {
                            manifestPath = potentialManifestPath;
                            break;
                        }

                        currentFolder = currentFolder.Parent;
                        count++;
                    }

                    if (manifestPath != null)
                    {
                        var thunderstoreManifestV1 = JsonConvert.DeserializeObject<Manifest>(await File.ReadAllTextAsync(manifestPath));

                        const string InvalidTeamName = "|";
                        const char separator = '-';
                        var teamNameDelimiter = separator + thunderstoreManifestV1.Name;

                        var team = InvalidTeamName;
                        var directoryName = new DirectoryInfo(Path.GetDirectoryName(manifestPath)!).Name;
                        if (directoryName.Contains(teamNameDelimiter))
                        {
                            team = directoryName.Split([teamNameDelimiter], StringSplitOptions.RemoveEmptyEntries)[0];
                        }

                        if (team == InvalidTeamName)
                        {
                            Log.Warning($"couldnt find team name for {thunderstoreManifestV1.Name} ,., likely a local mod ,..,");
                            LowerModCount();
                            continue;
                        }

                        //Log.Debug($"starting www request for {team}/{thunderstoreManifestV1.Name}");
                        
                        GetThunderstoreInfoAsync($"https://thunderstore.io/api/experimental/package/{team}/{thunderstoreManifestV1.Name}/", new ThunderstoreInfo(team, thunderstoreManifestV1, pluginInfo.Metadata.GUID));
                        await Task.Delay(TimeSpan.FromSeconds(0.3f)); // dont ddos thunderstore .,,. (setting this too low gets you too many requesteds ,,m,
                        continue;
                    }
                    
                    LowerModCount();
                }
            }
            catch (Exception e)
            {
                Log.Warning("exception while getting thunderstore info .,., ");
                Log.Warning(e);
            }
        }

        private static async void GetThunderstoreInfoAsync(string url, ThunderstoreInfo tsInfo)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            using UnityWebRequest uwr = UnityWebRequest.Get(url);
            
            AsyncOperation asyncOp = uwr.SendWebRequest();

            while (asyncOp.isDone == false)
            {
                await Task.Delay(1000 / 60);
            }
            
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Log.Debug($"{uwr.error}: {url}");
            }
            else
            {
                Log.Debug(uwr.downloadHandler.text);
                
                tsInfo.packageData = JsonConvert.DeserializeObject<APIResponse>(uwr.downloadHandler.text);
                thunderstoreInfos.Add(tsInfo);
                cachedInfos.Add(tsInfo);
            }

            LowerModCount();
        }

        private static void LowerModCount()
        {
            modCount--;
            if (modCount == 0)
            {
                string jsonSerialized = JsonConvert.SerializeObject(cachedInfos, Formatting.Indented);
                File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "ThunderstoreInfos.json"),  jsonSerialized);
                finishedAllMods?.Invoke();
            }
        }
        
        public class ThunderstoreInfo(string team, Manifest manifest, string GUID)
        {
            public string team = team;
            public Manifest manifest = manifest;
            public string GUID = GUID;
            public APIResponse packageData;
        }
        
        public class Manifest
        {
            [JsonProperty("name")]
            internal string Name { get; set; }

            [JsonProperty("version_number")]
            internal string VersionNumber { get; set; }

            [JsonProperty("website_url")]
            internal Uri WebsiteUrl { get; set; }

            [JsonProperty("description")]
            internal string Description { get; set; }

            [JsonProperty("dependencies")]
            internal List<string> Dependencies { get; set; }
        }

        public class APIResponse
        {
            [JsonProperty("namespace")]
            internal string Namespace { get; set; }
            
            [JsonProperty("name")]
            internal string Name { get; set; }
            
            [JsonProperty("full_name")]
            internal string FullName { get; set; }
            
            [JsonProperty("owner")]
            internal string Owner { get; set; }
            
            [JsonProperty("package_url")]
            internal string PackageUrl { get; set; }
            
            [JsonProperty("date_created")]
            internal string DateCreated { get; set; }
            
            [JsonProperty("date_updated")]
            internal string DateUpdated { get; set; }
            
            [JsonProperty("is_pinned")]
            internal bool IsPinned { get; set; }
            
            [JsonProperty("is_deprecated")]
            internal bool IsDeprecated { get; set; }
            
            [JsonProperty("total_downloads")]
            internal string TotalDownloads { get; set; }
            
            [JsonProperty("latest")]
            internal PackageVersionExperimental Latest { get; set; }
            
            [JsonProperty("community_listings")]
            internal List<PackageListingExperimental> CommumityListings { get; set; }
        }   
        
        public class PackageVersionExperimental
        {
            [JsonProperty("namespace")]
            internal string Namespace { get; set; }
            
            [JsonProperty("name")]
            internal string Name { get; set; }
            
            [JsonProperty("full_name")]
            internal string FullName { get; set; }
            
            [JsonProperty("version_number")]
            internal string VersionNumber { get; set; }
            
            [JsonProperty("description")]
            internal string Description { get; set; }
            
            [JsonProperty("date_created")]
            internal string DateCreated { get; set; }
            
            [JsonProperty("icon")]
            internal string Icon { get; set; }
            
            [JsonProperty("dependencies")]
            internal List<string> Dependencies { get; set; }
            
            [JsonProperty("download_url")]
            internal string DownloadUrl { get; set; }
            
            [JsonProperty("downloads")]
            internal int Downloads { get; set; }
            
            [JsonProperty("website_url")]
            internal string WebsiteUrl { get; set; }

            [JsonProperty("is_active")]
            internal bool IsActive { get; set; }
        }
        
        public class PackageListingExperimental
        {
            [JsonProperty("has_nsfw_content")]
            internal bool HasNSFWContent { get; set; }
            
            [JsonProperty("categories")]
            internal List<string> Categories { get; set; }
            
            [JsonProperty("community")]
            internal string Community { get; set; }
            
            [JsonProperty("review_status")]
            internal string ReviewStatus { get; set; }
        }
    }
}
