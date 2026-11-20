using System.Collections.Generic;
using BNR.patches;
using BepInEx.Configuration;
using On.RoR2.UI;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using RoR2BepInExPack.GameAssetPaths;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
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

    private GameObject cursorPrefab;
    
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
        if (swingyCursor.Value)
        {
            Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_UI_Cursor.CursorIndicator_prefab).Completed +=
                handle =>
                {
                    cursorPrefab = handle.Result;
                    cursorPrefab.transform.GetChild(0).transform.gameObject.AddComponent<OsuPhysics>();
                };
        }
        
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
        ModSettingsManager.AddOption(new ChoiceOption(cursorType));
        
        cursorColor = config.Bind("BNR - cursoredits",
            "cursor color ,..,",
            Color.white,
            "");
        cursorColor.SettingChanged += (_, _) => { applyCursorEdits(); };
        ModSettingsManager.AddOption(new ColorOption(cursorColor));
        
        swingyCursor = config.Bind("BNR - cursoredits",
            "swingy cursor ,..,",
            true,
            "");
        ModSettingsManager.AddOption(new CheckBoxOption(swingyCursor));

        enabled = config.Bind("BNR - cursoredits",
            "enable patches for cursoredits",
            false,
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
    public static ConfigEntry<bool> swingyCursor;
}

class OsuPhysics : CursorPhysicsBase
{
    private Vector3? initialPos;
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && spinDegrees == 0)
        {
            //targetVector = new Vector2(Mathf.Cos(Mathf.Deg2Rad * spinDegrees), Mathf.Sin(Mathf.Deg2Rad * spinDegrees));
            prevVector = targetVector;
            initialPos = transform.position;
        }
        
        if(Input.GetMouseButtonDown(0))
            initialPos = transform.position;
        
        if (Input.GetMouseButtonUp(0))
        {
            if(added180)
                spinDegrees -= 180;
            added180 = false;
            timeHoldingDownCursor = 0;
            lastSpinDegree = 0;

            positions = [];
            for (int i = 0; i < 3; i++)
            {
                positions.Enqueue(new Vector2(1f, 0f));
            }
            
            prevVector = new Vector2(1f, 0f);
            targetVector = new Vector2(1f, 0f);
        }
        
        if (Input.GetMouseButton(0) && timeHoldingDownCursor > 0.5f)
        {
            Vector2 lookVector = positions.Peek() - (Vector2)transform.position;
            
            if (lookVector != Vector2.zero && transform.position != initialPos)
            {
                positions.Enqueue(transform.position);
                positions.Dequeue();
                float angle2 = Mathf.Atan2(lookVector.normalized.y, lookVector.normalized.x) * Mathf.Rad2Deg;
                angle2 -= 135; // offset to make cursor base look right .,,
                
                prevVector = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle2), Mathf.Sin(Mathf.Deg2Rad * angle2));
                initialPos = null;
            }
            
            Log.Debug($"target {targetVector} prev {prevVector} at {timeHoldingDownCursor}");
            targetVector = Vector2.Lerp(targetVector, prevVector, Time.unscaledDeltaTime * 4f);
            angle = Mathf.Atan2(targetVector.y, targetVector.x) * Mathf.Rad2Deg;
            
            HandleSpinAnlge();
        }
        else
        {
            spinDegrees = Mathf.Lerp(spinDegrees, 0f, Time.unscaledDeltaTime * 2f);
            spinDegrees = Mathf.MoveTowards(spinDegrees, 0f, Time.unscaledDeltaTime * 6f);
            angle = spinDegrees;
        }
        
        base.Update();
    }
}

class ConstantSwingy : CursorPhysicsBase
{
    private void Update()
    {
        Vector2 lookVector = (Vector2)transform.position - positions.Dequeue();
        positions.Enqueue(transform.position);
        
        if (lookVector != Vector2.zero)
        {
            prevVector = lookVector.normalized;
            float angle2 = Mathf.Atan2(prevVector.y, prevVector.x) * Mathf.Rad2Deg;
            angle2 -= 135; // offset to make cursor base look right .,,
            
            prevVector = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle2), Mathf.Sin(Mathf.Deg2Rad * angle2));
        }
        else
        {
            prevVector = Vector2.right; //new Vector2(1f, 0f);
        
            spinDegrees = Mathf.Lerp(spinDegrees, 0f, Time.deltaTime * 2f);
            spinDegrees = Mathf.MoveTowards(spinDegrees, 0f, Time.deltaTime * 6f);
            lastSpinDegree = 0;
        }
        
        targetVector = Vector2.Lerp(targetVector, prevVector, Time.deltaTime * 4f);
        angle = Mathf.Atan2(targetVector.y, targetVector.x) * Mathf.Rad2Deg;

        //HandleSpinAnlge();
        
        base.Update();
    }
}

class CursorPhysicsBase : MonoBehaviour
{
    public Vector2 targetVector;
    public RectTransform rectTransform;
    public Vector2 prevVector;

    public float spinDegrees = 0f;
    public float lastSpinDegree = -1f;
    
    public Queue<Vector2> positions = new();

    public float timeHoldingDownCursor;
    

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        for (int i = 0; i < 3; i++)
        {
            positions.Enqueue(new Vector2(1f, 0f));
        }
    }

    public bool added180;
    public float angle;

    protected void Update()
    {
        if (Input.GetMouseButton(0) && spinDegrees == 0)
        {
            timeHoldingDownCursor += Time.unscaledDeltaTime;
        }
        
        rectTransform.eulerAngles = rectTransform.eulerAngles with { z = angle };
    }

    public void HandleSpinAnlge()
    {
        float spinAngle = angle;
        if (spinAngle < 180)
        {
            added180 = true;
            spinAngle += 180;
        }
        
        if (lastSpinDegree > spinAngle && Mathf.Abs(spinAngle - lastSpinDegree) > 200)
        {
            spinDegrees += 360 - Mathf.Abs(lastSpinDegree - spinAngle);
        }
        else if (spinAngle > lastSpinDegree && Mathf.Abs(spinAngle - lastSpinDegree) > 200)
        {
            spinDegrees -= 360 - Mathf.Abs(lastSpinDegree - spinAngle);
        }
        else
        {
            spinDegrees += Mathf.Abs(spinAngle) - lastSpinDegree;
        }
        
        lastSpinDegree = Mathf.Abs(spinAngle);
    }
}