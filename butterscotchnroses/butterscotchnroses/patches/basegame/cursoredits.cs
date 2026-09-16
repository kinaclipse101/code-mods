using BNR.patches;
using BepInEx.Configuration;
using On.RoR2.UI;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using MPEventSystem = RoR2.UI.MPEventSystem;

namespace BNR;

public class cursoredits : PatchBase<cursoredits>
{
    private Texture cursorDefault;
    private Texture cursorDefaultActive;
    private Texture cursor1;
    private Texture cursor1Active;
    private Texture cursor2;
    private Texture cursor2Active;
    private Texture cursor3;
    private Texture cursor3Active;
    
    public override void Init()
    {
        applyHooks();
        
        cursorDefault = Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Core.texCursor_Active_png).WaitForCompletion();
        cursorDefaultActive = Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Core.texCursor_Inactive_png).WaitForCompletion();
        cursor1 = Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_MiscIcons.texCursor_png).WaitForCompletion();
        cursor1Active = Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_MiscIcons.texCursor_png).WaitForCompletion();
        cursor2 = Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_MiscIcons.texCursor2_png).WaitForCompletion();
        cursor2Active = Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_MiscIcons.texCursor2_png).WaitForCompletion();
        cursor3 = Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_MiscIcons.texCursor3_png).WaitForCompletion();
        cursor3Active = Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_MiscIcons.texCursor3_png).WaitForCompletion();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            RoR2Application.onLoadFinished += applyCursorEdits;
            On.RoR2.UI.CursorIndicatorController.SetCursor += CursorIndicatorControllerOnSetCursor;
        }
        else
        {
            RoR2Application.onLoadFinished -= applyCursorEdits;
            On.RoR2.UI.CursorIndicatorController.SetCursor -= CursorIndicatorControllerOnSetCursor;
        }
    }

    private void CursorIndicatorControllerOnSetCursor(CursorIndicatorController.orig_SetCursor orig, RoR2.UI.CursorIndicatorController self, RoR2.UI.CursorIndicatorController.CursorSet cursorset, RoR2.UI.CursorIndicatorController.CursorImage cursorimage, Color color)
    {
        orig(self, cursorset, cursorimage, cursorColor.Value);
    }
    
    private void applyCursorEdits()
    {
        foreach (MPEventSystem mpEventSystem in RoR2.UI.MPEventSystem.instancesList)
        {
            Texture cursorActive = cursorType.Value switch
            {
                cursorTypes.cursor1 => cursor1Active,
                cursorTypes.cursor2 => cursor2Active,
                cursorTypes.cursor3 => cursor3Active,
                cursorTypes.defaultCursor => cursorDefaultActive,
            };
            
            Texture cursorInactive = cursorType.Value switch
            {
                cursorTypes.cursor1 => cursor1,
                cursorTypes.cursor2 => cursor2,
                cursorTypes.cursor3 => cursor3,
                cursorTypes.defaultCursor => cursorDefault,
            };
            
            mpEventSystem.cursorIndicatorController.mouseCursorSet.hoverObject.GetComponent<Image>().sprite = Sprite.Create(cursorInactive as Texture2D, new Rect(0, 0, cursor1.width, cursor1.height), new Vector2(0f, 0f));
            mpEventSystem.cursorIndicatorController.mouseCursorSet.pointerObject.GetComponent<Image>().sprite = Sprite.Create(cursorActive as Texture2D, new Rect(0, 0, cursor1.width, cursor1.height), new Vector2(0f, 0f));
            mpEventSystem.cursorIndicatorController.cachedIndicatorColor = cursorColor.Value;
            mpEventSystem.cursorIndicatorController.ApplyIndicatorColor();
        }
    }
    
    public override void Config(ConfigFile config)
    {
        cursorType = config.Bind("BNR - cursoredits",
            "cursor type ,..,",
            cursorTypes.defaultCursor,
            "");
        cursorType.SettingChanged += (_, _) => { applyCursorEdits(); };
        
        cursorColor = config.Bind("BNR - cursoredits",
            "cursor color ,..,",
            Color.white,
            "");
        ModSettingsManager.AddOption(new ColorOption(cursorColor));
        cursorColor.SettingChanged += (_, _) => { applyCursorEdits(); };

        enabled = config.Bind("BNR - cursoredits",
            "enable patches for cursoredits",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
    }

    public enum cursorTypes
    {
        defaultCursor,
        cursor1,
        cursor2,
        cursor3
    }

    private ConfigEntry<cursorTypes> cursorType;
    private ConfigEntry<Color> cursorColor;
    private ConfigEntry<bool> enabled;
}