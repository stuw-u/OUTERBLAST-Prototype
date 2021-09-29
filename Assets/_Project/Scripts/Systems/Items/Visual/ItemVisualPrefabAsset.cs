using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemRenderingMode {
    Hide,
    ShadowOnly,
    ShowWithShadow,
    ShowWithoutShadow
}

public enum ItemVisualMode {
    ShowOnFirstPerson,
    ShowOnThirdPerson
}

public enum ItemVisualAnchor {
    Eye,
    Body,
    LeftClaw,
    RightClaw
}

[CreateAssetMenu(fileName = "ItemVisualPrefab", menuName = "Custom/Item Visuals/Item Visual Prefab")]
public class ItemVisualPrefabAsset : ScriptableObject {
    public ItemVisualConfig[] firstPersonPrefabs;
    public ItemVisualConfig[] thirdPersonPrefabs;
}
