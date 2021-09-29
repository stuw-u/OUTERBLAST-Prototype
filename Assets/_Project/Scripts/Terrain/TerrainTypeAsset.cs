using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TerrainType", menuName = "Custom/Terrain/Terrain Type Asset", order = -1)]
public class TerrainTypeAsset : ScriptableObject {
    new public string name;
    public TerrainGeneratorPassAsset[] generationPasses;
    public Rarity rarity; 
    public GravityFieldType gravityFieldType;
    public GameObject[] effects;
    public Thumbnail thumbnail;
    public Sprite selectionIcon;

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

    public enum Thumbnail {
        Default,
        Planetoid
    }

    public static readonly string[] ThumbnailKeys = {
        "default",
        "planetoid"
    };
}

public enum Rarity {
    Common,
    Uncommon,
    Rare,
    UltraRare,
    Legendary
}
