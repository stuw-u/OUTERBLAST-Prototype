
// UnityGraphicsBullshit.cs (renamed to UnityGraphicReflection) modified version of JimmyCushnie's code
// https://gist.github.com/JimmyCushnie/e998cdec15394d6b68a4dbbf700f66ce 
// Exposes some Unity URP graphics settings that are (for some stupid fucking bullshit reason) private.

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution;



/// <summary>
/// Enables getting/setting URP graphics settings properties that don't have built-in getters and setters.
/// </summary>
public static class UnityGraphicsBullshit
{
    private static FieldInfo MainLightCastShadows_FieldInfo;
    private static FieldInfo AdditionalLightCastShadows_FieldInfo;
    private static FieldInfo MainLightShadowmapResolution_FieldInfo;
    private static FieldInfo AdditionalLightShadowmapResolution_FieldInfo;
    private static FieldInfo Cascade2Split_FieldInfo;
    private static FieldInfo Cascade4Split_FieldInfo;
    private static FieldInfo SoftShadowsEnabled_FieldInfo;
    private static FieldInfo MSAAQuality_FieldInfo;
    private static FieldInfo ShadowAtlasResolution_FieldInfo;
    private static FieldInfo RenderScale_FieldInfo;
    private static FieldInfo RequireDepthTexture_FieldInfo;
    private static FieldInfo Renderers_FieldInfo;

    static UnityGraphicsBullshit()
    {
        var pipelineAssetType = typeof(UniversalRenderPipelineAsset);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        /*FieldInfo[] fields = pipelineAssetType.GetFields(flags);
        foreach(FieldInfo field in fields) {
            Debug.Log($"{field.Name}, {field.FieldType}");
        }*/

        MainLightCastShadows_FieldInfo = pipelineAssetType.GetField("m_MainLightShadowsSupported", flags);
        AdditionalLightCastShadows_FieldInfo = pipelineAssetType.GetField("m_AdditionalLightShadowsSupported", flags);
        MainLightShadowmapResolution_FieldInfo = pipelineAssetType.GetField("m_MainLightShadowmapResolution", flags);
        AdditionalLightShadowmapResolution_FieldInfo = pipelineAssetType.GetField("m_AdditionalLightsShadowmapResolution", flags);
        Cascade2Split_FieldInfo = pipelineAssetType.GetField("m_Cascade2Split", flags);
        Cascade4Split_FieldInfo = pipelineAssetType.GetField("m_Cascade4Split", flags);
        SoftShadowsEnabled_FieldInfo = pipelineAssetType.GetField("m_SoftShadowsSupported", flags);
        MSAAQuality_FieldInfo = pipelineAssetType.GetField("m_MSAA", flags);
        ShadowAtlasResolution_FieldInfo = pipelineAssetType.GetField("m_ShadowAtlasResolution", flags);
        RenderScale_FieldInfo = pipelineAssetType.GetField("m_RenderScale", flags);
        RequireDepthTexture_FieldInfo = pipelineAssetType.GetField("m_RequireDepthTexture", flags);

        Renderers_FieldInfo = pipelineAssetType.GetField("m_Renderers", flags);
        var renderers = (ScriptableRenderer[])Renderers_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        var mainRenderer = renderers[0];
    }


    public static bool MainLightCastShadows
    {
        get => (bool)MainLightCastShadows_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => MainLightCastShadows_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static bool AdditionalLightCastShadows
    {
        get => (bool)AdditionalLightCastShadows_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => AdditionalLightCastShadows_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static ShadowResolution MainLightShadowResolution
    {
        get => (ShadowResolution)MainLightShadowmapResolution_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => MainLightShadowmapResolution_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static ShadowResolution AdditionalLightShadowResolution
    {
        get => (ShadowResolution)AdditionalLightShadowmapResolution_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => AdditionalLightShadowmapResolution_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static float Cascade2Split
    {
        get => (float)Cascade2Split_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => Cascade2Split_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static Vector3 Cascade4Split
    {
        get => (Vector3)Cascade4Split_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => Cascade4Split_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static bool SoftShadowsEnabled
    {
        get => (bool)SoftShadowsEnabled_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => SoftShadowsEnabled_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static ShadowResolution ShadowResolution {
        get => (ShadowResolution)ShadowAtlasResolution_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => ShadowAtlasResolution_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static MsaaQuality MsaaQuality {
        get => (MsaaQuality)MSAAQuality_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => MSAAQuality_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static float RenderScale {
        get => (float)RenderScale_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => RenderScale_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static bool DepthTextureEnabled {
        get => (bool)RequireDepthTexture_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => RequireDepthTexture_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static void SetSSAOSettings (Blast.Settings.Quality quality) {
        //var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        List<ScriptableRendererFeature> srfs = AssetsManager.inst.pipelineAsset.rendererFeatures;
        
        var ssaoType = srfs[1].GetType();

        /*FieldInfo[] fields = PermanentManagers.inst.pipelineAsset.GetType().GetFields(flags);
        foreach(FieldInfo field in fields) {
            Debug.Log($"{field.Name}, {field.FieldType}");
        }*/
        
        //srfs[0].isActive = quality != Blast.Settings.Quality.Low;
    }

    public static void SetBlurSettings (bool enabled) {

    }
}