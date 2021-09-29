using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public class WorldStylist : MonoBehaviour {

    public Camera mainCamera;
    public Skybox skybox;
    public CameraAnchor cloudAnchor;
    public Transform cloudTransform;
    public MeshRenderer cloudRenderer;
    public Light mainLight;

    void Start () {
        TerrainStyleAsset s = LobbyWorldInterface.inst.terrainStyle;

        if(s.skyType == SkyType.Color) {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = s.skyColor;
        } else if(s.skyType == SkyType.Skybox) {
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            skybox.material = s.skybox;
        }
        RenderSettings.fog = s.enableFog;
        RenderSettings.fogColor = s.fogColor;
        RenderSettings.fogDensity = s.fogDensity;

        cloudRenderer.enabled = s.enableCloud;
        cloudRenderer.material = s.cloudMaterial;
        cloudTransform.localScale = new Vector3(2f, s.cloudsScaleY, 2f);
        cloudAnchor.lockedOffset.y = s.cloudsYHeight;

        mainLight.transform.rotation = s.mainLightRotation;
        mainLight.intensity = s.mainLightIntensity;
        mainLight.color = s.mainLightColor;
        if(s.enableShadow) {
            mainLight.shadows = LightShadows.Soft;
        } else {
            mainLight.shadows = LightShadows.None;
        }

        foreach(GameObject effect in s.effects) {
            Instantiate(effect);
        }
        foreach(GameObject effect in LobbyWorldInterface.inst.terrainType.effects) {
            Instantiate(effect);
        }
    }

}
