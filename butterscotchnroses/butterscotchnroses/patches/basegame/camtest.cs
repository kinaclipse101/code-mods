using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using BepInEx;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using HealthComponent = On.RoR2.HealthComponent;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Object = UnityEngine.Object;
using Vector3 = UnityEngine.Vector3;

namespace BNR;

public class camtest : PatchBase<camtest>
{
    private static readonly int ShadowTex = Shader.PropertyToID("_ShadowTex");

    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            //RoR2Application.onUpdate += RoR2ApplicationOnonUpdate;
            //On.RoR2.HealthComponent.TakeDamageProcess += HealthComponentOnTakeDamageProcess;
            //Stage.onStageStartGlobal += StageOnonStageStartGlobal;
        }
        else
        {
            
        }
    }

    List<Renderer> gameObjects = new();
    List<ShadowCastingMode> castingModes = new();
    private void StageOnonStageStartGlobal(Stage stage)
    {
        gameObjects = [];

        foreach (var renderer in GameObject.FindObjectsOfType<Renderer>(false))
        {
            if (renderer.gameObject.layer != 11) continue;
            gameObjects.Add(renderer);
            castingModes.Add(renderer.shadowCastingMode);
        }

    }

    private void HealthComponentOnTakeDamageProcess(HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, DamageInfo damageinfo)
    {
        var enemyModel = self.body.modelLocator.modelTransform.gameObject;
        var renderers = enemyModel.GetComponent<CharacterModel>()?.baseRendererInfos;
        List<int> oldLayers = [];
        foreach (var renderer in renderers)
        {
            oldLayers.Add(renderer.renderer.gameObject.layer);
            renderer.renderer.gameObject.layer = 28;
        }

        for (int i = 0; i < gameObjects.Count; i++)
        {
            gameObjects[i].gameObject.layer = 28;
            gameObjects[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
        
        ApplyAnamorphic(28, Vector3.Distance(SceneCamera._camera.transform.position, enemyModel.transform.position) + 999);
        
        for (int i = 0; i < gameObjects.Count; i++)
        {
            gameObjects[i].gameObject.layer = 11;
            gameObjects[i].shadowCastingMode = ShadowCastingMode.On;
        }
    }

    private void RoR2ApplicationOnonUpdate()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ApplyAnamorphic(1);
        }
    }

    private void ApplyAnamorphic(int layer, float maxDistance = -1)
    {
        Camera cam = SceneCamera._camera;
        GameObject hud = null;
        if(HUD.instancesList.Count > 0) hud = HUD.instancesList[0]?.gameObject;

        var oldFlags = cam.clearFlags;
        var oldMask = cam.cullingMask;
        var oldBackground = cam.backgroundColor;
            
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.cullingMask = 1 << layer; //LayerMask.GetMask(LayerMask.LayerToName(layer));
        cam.backgroundColor = Color.clear;
        Log.Debug("mask = " + cam.cullingMask);
            
        hud?.SetActive(false);
        SceneCamera._camera.transform.Find("GlobalPostProcessVolume").gameObject.SetActive(false);
        var pp  = GameObject.Find("PP + Amb");
        var ppv = pp?.GetComponent<PostProcessVolume>();
        var fog = ppv?.profile?.GetSetting<RampFog>();
        Color oldFogEnd = new Color();
        if (fog)
        {
            oldFogEnd = fog.fogColorEnd.value;
            fog.fogColorEnd.value = new Color(0, 0, 0, 0);
        }
            
        //pp?.SetActive(false);
            
        var sw = new Stopwatch();
        sw.Start();
        Texture2D snapshot = new Texture2D(cam.pixelWidth, cam.pixelHeight, TextureFormat.RGBA32, false);
        //var pixels = new Color[(cam.pixelWidth + 2) * (cam.pixelHeight + 2)];
        //Array.Fill(pixels, Color.clear);
        //snapshot.SetPixels(pixels);
        //snapshot.Apply();
            
        //take screenie ,.
        var sw2 = new Stopwatch();
        sw2.Start();
        RenderTexture.active = cam.targetTexture;
        cam.Render();
        Log.Debug(sw2.Elapsed.Milliseconds + " ms");

        snapshot.ReadPixels(new Rect(0, 0, cam.pixelWidth, cam.pixelHeight), 0, 0);
        snapshot.Apply();
        snapshot.name = "newSnapshot";
        snapshot.wrapMode = TextureWrapMode.Clamp;
            
        RenderTexture.active = null;
        cam.targetTexture = null;
            
        GameObject testProjector = Object.Instantiate(butterscotchnroses.bnrBundle.LoadAsset<GameObject>("BlobShadowProjector"));
        testProjector.name = "TestProjector";
        testProjector.transform.position = cam.transform.position;
        testProjector.transform.rotation = cam.transform.rotation;
        
        Projector projector = testProjector.GetComponent<Projector>();
        projector.material = new Material(projector.material);
        projector.fieldOfView = cam.fieldOfView;
        projector.farClipPlane = Mathf.Approximately(maxDistance, -1) ? cam.farClipPlane : maxDistance;
        projector.nearClipPlane = cam.nearClipPlane;
        projector.orthographicSize = cam.orthographicSize;
        projector.aspectRatio = cam.aspect;
        projector.material.SetTexture(ShadowTex, snapshot);
        Log.Debug(sw.Elapsed.Milliseconds + " ms");

        var st = new Stopwatch();
        st.Start();
        var planes = GeometryUtility.CalculateFrustumPlanes(cam);
        foreach (var renderer in gameObjects)
        {
            var check = GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
            if (check)
            {
                Log.Debug("renderer was rendered " + renderer.name);
            }
        }
        Log.Debug(st.Elapsed.Milliseconds + " ms");
        //GameObject primative = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //primative.transform.SetParent(testProjector.transform);
        //primative.transform.localPosition = Vector3.zero;
            
        hud?.SetActive(true);
        SceneCamera._camera.transform.Find("GlobalPostProcessVolume").gameObject.SetActive(true);
        pp?.SetActive(true);
            
        cam.clearFlags = oldFlags;
        cam.cullingMask = oldMask;
        cam.backgroundColor = oldBackground;
        if (fog)
        {
            fog.fogColorEnd.value = oldFogEnd;
        }
            
        var textureDirs = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Paths.BepInExConfigPath)!, "test", "evil.png");
        File.WriteAllBytes(textureDirs, snapshot.EncodeToPNG());
    }

    public override void Config(ConfigFile config)
    {
        // enabled = config.Bind("BNR - camtest",
        //     "enable patches for camtest",
        //     true,
        //     "");
        // Utils.CheckboxConfig(enabled);
        // enabled.SettingChanged += (_, _) => { applyHooks(); };
    }
    
    private ConfigEntry<bool> enabled;
}