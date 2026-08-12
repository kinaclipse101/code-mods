using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using RoR2;
using UnityEngine;

namespace BNR.patches;

public class commands
{
    #region debugcommands
    [ConCommand(commandName = "skin_create", flags = ConVarFlags.None, helpText = "list internal skins.,,.")]
    public static void CreateSkin(ConCommandArgs args)
    {
        if (skinrecolors.baseSkinName == null)
        {
            Log.Warning("base skin null !!");
            return;
        }

        if (args.Count >= 1)
        {
            skinrecolors.newSkinName = args[0];
        }
        
        string skinName = !skinrecolors.newSkinName.IsNullOrWhiteSpace() ? skinrecolors.newSkinName : "Generated Skin";
        skinrecolors.skinRecolors.Value += $";;{skinrecolors.baseSkinName.name},{skinrecolors.currentBody.name[..^7]},{skinrecolors.hsv[0]},{skinrecolors.hsv[1]},{skinrecolors.hsv[2]},{skinName},{skinrecolors.multiplySat}";
        Debug.Log($"added ;;{skinrecolors.baseSkinName.name},{skinrecolors.currentBody.name[..^7]},{skinrecolors.hsv[0]},{skinrecolors.hsv[1]},{skinrecolors.hsv[2]},{skinName},{skinrecolors.multiplySat} to the config !!! restart your game to see it in lobby .,,.");
    }
    
    [ConCommand(commandName = "skin_list", flags = ConVarFlags.None, helpText = "list internal skins.,,.")]
    public static void ListSkins(ConCommandArgs args)
    {
        Debug.Log("args = " + args[0] + " " );
        var bodyPrefab = BodyCatalog.FindBodyPrefab(args[0]);
        if (!bodyPrefab)
        {
            Log.Warning("body no existey ,,.");
            return;
        }

        var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
        if (!modelLocator)
        {
            Log.Warning("model locator no existey .,.,");
            return;
        }

        var mdl = modelLocator.modelTransform.gameObject;
        var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
        if (!skinController)
        {
            Log.Warning("model skin controller no existey .,,.");
            return;
        }
        
        foreach (SkinDef skinControllerSkinDef in skinController.skins)
        {
            Debug.Log(skinControllerSkinDef.name);
        }
    }

    [ConCommand(commandName = "skin_clear", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void clearSkin(ConCommandArgs args)
    {
        bodyNameToPrev.Clear();
    }

    public static Dictionary<string, int> bodyNameToPrev = [];
    [ConCommand(commandName = "skin_recolor", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void recolorSkin(ConCommandArgs args)
    {
        ModelSkinController skinController = Utils.GetModelLocator(args.GetSenderBody().gameObject);

        int currentIndex = skinController.currentSkinIndex;
        if (bodyNameToPrev.TryGetValue(args.senderBody.name, out int value))
        {
            currentIndex = value;
        }
        else
        {
            bodyNameToPrev.Add(args.senderBody.name, skinController.currentSkinIndex);
        }
        SkinDef baseSkin = skinController.skins[currentIndex];

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
            skinrecolors.newSkinName = args[0];
            
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
        
        SkinDef replacementSkin = skinrecolors.skinRecolor(baseSkin.name, args.senderBody.name, HSVhue, HSVsat, HSVvalue, "temp", "", true, multiplySaturation);

        Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
        skinController.skins[^1] = replacementSkin;
        skinController.currentSkinIndex = skinController.skins.Length - 1;
        args.senderBody.skinIndex = (uint)(skinController.skins.Length - 1);
        
#pragma warning disable CS0618 // Type or member is obsolete
        skinController.ApplySkin(skinController.currentSkinIndex);
#pragma warning restore CS0618 // Type or member is obsolete
        
        Array.Resize(ref skinController.skins, skinController.skins.Length - 1);
        //skinController.skins[^1] = replacementSkin;
        
        skinrecolors.baseSkinName = baseSkin;
        skinrecolors.hsv[0] = HSVhue;
        skinrecolors.hsv[1] = HSVsat;
        skinrecolors.hsv[2] = HSVvalue;
        skinrecolors.currentBody = args.senderBody;
        skinrecolors.multiplySat = multiplySaturation;

        //Log.Debug("bwaa");
    }
    #endregion
}