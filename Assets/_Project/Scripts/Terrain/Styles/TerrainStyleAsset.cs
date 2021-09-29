using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TerrainStyle", menuName = "Custom/Terrain/Style", order = -2)]
public class TerrainStyleAsset : ScriptableObject {
    new public string name;
    public Rarity rarity;

    [Header("Terrain")]
    public Material terrainMaterial;
    public bool enableDeco;
    public Material decoMaterial;
    public Mesh decoMesh;

    [Header("Sky")]
    public SkyType skyType;
    public Color skyColor;
    public Material skybox;
    public bool enableFog;
    public Color fogColor;
    public float fogDensity;

    [Header("Clouds")]
    public bool enableCloud;
    public float cloudsScaleY = -1f;
    public float cloudsYHeight = 80f;
    public Material cloudMaterial;

    [Header("Lighting")]
    public Quaternion mainLightRotation = Quaternion.Euler(50f, -30f, 0f);
    public Color mainLightColor = Color.white;
    public float mainLightIntensity = 1.1f;
    public bool enableShadow = true;

    [Header("Effets")]
    public GameObject[] effects;

    public int GetRarityWeight () {
        switch(rarity) {
            case Rarity.Common:
            return 20;
            case Rarity.Uncommon:
            return 12;
            case Rarity.Rare:
            return 7;
            case Rarity.UltraRare:
            return 4;
            case Rarity.Legendary:
            return 1;
        }
        return 1;
    }
}

public enum SkyType {
    Color,
    Skybox
}
