using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AssetsManager : MonoBehaviour {

    [Header("Assets")]
    public ItemAssetCollection itemAssets;
    public ProjectileAsset[] projectileAssets;
    public TerrainTypeCollection terrainTypeCollection;
    public ForwardRendererData pipelineAsset;
    public TerrainStyleCollection terrainStyleCollection;

    [Header("Mine")]
    public Blast.ObjectPooling.PoolableObject minePrefab;
    public float mineThrowSpeed;
    public int mineMaxFrames;

    [Header("Prefabs")]
    public PlayerGameObject selfPlayerObjectPrefab;
    public PlayerGameObject playerObjectPrefab;
    public LobbyManager hostLobbyManagerPrefab;
    public LobbyManager serverLobbyManagerPrefab;
    public PlayerSimulationLink playerSimulationLinkPrefab;
    public Blast.ObjectPooling.PoolableObject itemCrate;

    public static AssetsManager inst;
    private void Awake () {
        inst = this;

        itemAssets.Init();
    }
}
