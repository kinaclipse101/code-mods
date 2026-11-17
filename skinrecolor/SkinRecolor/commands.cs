using System;
using System.IO;
using Newtonsoft.Json;
using RoR2;
using RoR2.ContentManagement;
using SkinRecolor.RecoloredSkins;
using Console = System.Console;

namespace SkinRecolor;

public class commands
{
    public static int originalSkinIndex = -1;
    
    [ConCommand(commandName = "recolor_skin_new", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void recolorSkin(ConCommandArgs args)
    {
        ModelSkinController skinController = Utils.GetModelLocator(args.GetSenderBody().gameObject);

        int currentIndex = skinController.currentSkinIndex;
        if(originalSkinIndex == -1)
            originalSkinIndex = currentIndex;
        SkinDef baseSkin = skinController.skins[originalSkinIndex];

        float HSVsat = 0;
        float HSVvalue = 0;
        bool multiplySaturation = false;
        if (float.TryParse(args[0], out float HSVhue))
        {
            if (args.Count >= 2)
            {
                HSVsat = float.Parse(args[1]);
            }
        
            if (args.Count >= 3)
            {
                HSVvalue = float.Parse(args[2]);
            }

            if (args.Count >= 4)
            {
                multiplySaturation = bool.Parse(args[3]);
            }
        }
        else
        {
            if (args.Count >= 2)
            {
                HSVhue = float.Parse(args[1]);
            }
        
            if (args.Count >= 3)
            {
                HSVsat = float.Parse(args[2]);
            }
        
            if (args.Count >= 4)
            {
                HSVvalue = float.Parse(args[3]);
            }
            
            if (args.Count >= 5)
            {
                multiplySaturation = bool.Parse(args[4]);
            }
        }
        Log.Debug($"multipl sat ? {multiplySaturation}");
        Log.Debug($"master prefab {args.GetSenderMaster().bodyPrefab.name}");
        RecoloredSkinJson skinJson = new RecoloredSkinJson
        {
            baseSkinName = baseSkin.name,
            baseSkinBody = args.GetSenderMaster().bodyPrefab.name,
            skinName = "test" + DateTime.Now,
            prefix = "",
            lightReplacements = null,
            hsv = new ReplacementHSV()
            {
                hue = HSVhue,
                saturation = HSVsat,
                value = HSVvalue,
                multiplySat = multiplySaturation
            }
        };

        ApplySkin(skinController, skinJson, args.senderBody);
    }
    
    private static async void ApplySkin(ModelSkinController skinController, RecoloredSkinJson skinJson, CharacterBody characterBody)
    {
        try
        {
            SkinDef newSkin = await RecoloredSkinManager.skinRecolor(skinJson, true);
        
            Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
            skinController.skins[^1] = newSkin;
            skinController.currentSkinIndex = skinController.skins.Length - 1;
            characterBody.skinIndex = (uint)(skinController.skins.Length - 1);
        
#pragma warning disable CS0618 // Type or member is obsolete
            skinController.ApplySkin(skinController.currentSkinIndex);
#pragma warning restore CS0618 // Type or member is obsolete
            
            RoR2.Console.instance.RunCmd(LocalUserManager.GetFirstLocalUser(), "apply_skin", [characterBody.skinIndex.ToString()]);
        }
        catch (Exception e)
        {
            Log.Error($"error while setting skin {e}");
        }
    }

    [ConCommand(commandName = "recolor_clear_new", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void ClearSkin(ConCommandArgs args)
    {
        originalSkinIndex = -1;
    }
    
    [ConCommand(commandName = "recolor_create_new", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void createRecolorSkin(ConCommandArgs args)
    {
        ModelSkinController skinController = Utils.GetModelLocator(args.GetSenderBody().gameObject);

        int currentIndex = skinController.currentSkinIndex;
        SkinDef baseSkin = skinController.skins[originalSkinIndex];

        float HSVsat = 0;
        float HSVvalue = 0;
        bool multiplySaturation = false;
        if (float.TryParse(args[0], out float HSVhue))
        {
            if (args.Count >= 2)
            {
                HSVsat = float.Parse(args[1]);
            }
        
            if (args.Count >= 3)
            {
                HSVvalue = float.Parse(args[2]);
            }

            if (args.Count >= 4)
            {
                multiplySaturation = bool.Parse(args[3]);
            }
        }
        else
        {
            if (args.Count >= 2)
            {
                HSVhue = float.Parse(args[1]);
            }
        
            if (args.Count >= 3)
            {
                HSVsat = float.Parse(args[2]);
            }
        
            if (args.Count >= 4)
            {
                HSVvalue = float.Parse(args[3]);
            }
            
            if (args.Count >= 5)
            {
                multiplySaturation = bool.Parse(args[4]);
            }
        }
        Log.Debug($"multipl sat ? {multiplySaturation}");
        Log.Debug($"master prefab {args.GetSenderMaster().bodyPrefab.name}");
        RecoloredSkinJson skinJson = new RecoloredSkinJson
        {
            baseSkinName = baseSkin.name,
            baseSkinBody = args.GetSenderMaster().bodyPrefab.name,
            skinName = "test",
            prefix = "",
            lightReplacements = null,
            hsv = new ReplacementHSV()
            {
                hue = HSVhue,
                saturation = HSVsat,
                value = HSVvalue,
                multiplySat = multiplySaturation
            }
        };
        
        RecoloredSkinManager.skinJsons.Add(skinJson);
        File.WriteAllText(RecoloredSkinManager.jsonPath, JsonConvert.SerializeObject(RecoloredSkinManager.skinJsons, Formatting.Indented));
    }
}