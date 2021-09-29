using System;
using System.Text;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Blast.ECS;
using Blast.ECS.DefaultComponents;
using Blast.Settings;
using TMPro;

#region Legacy Snapshot
public struct PhysicsSnapshot {
    public float3 velocity;
    public float3 angularVelocity;
    public bool isKinematic;
    public CollisionDetectionMode detectionMode;

    public PhysicsSnapshot (float3 velocity, float3 angularVelocity, bool isKinematic, CollisionDetectionMode detectionMode) {
        this.velocity = velocity;
        this.angularVelocity = angularVelocity;
        this.isKinematic = isKinematic;
        this.detectionMode = detectionMode;
    }
}

public struct ExplosionEvent {
    public int frameIndex;
    public float3 position;
    public float radius;
}
#endregion

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour {

    public static GameManager inst;
    private SimulationManager simulationStore;
    private StringBuilder sb;

    [Header("References")]
    public Camera mainCamera;
    public Transform objectParent;
    public PlayerParametersAsset playerParametersAsset;
    public TextMeshProUGUI debugText;
    public Eldemarkki.VoxelTerrain.World.VoxelWorld voxelWorld;
    public Animator cinematicAnimator;
    public ReflectionProbe reflectionProbe;

    [Header("Game Settings")]
    public float spawnRadius = 10f;
    public float spawnHeight = 0f;
    public float respawnRadius = 10f;
    public float respawnHeight = 100f;
    
    // Private part :3
    private InputSnapshot lastInputs;


    #region Debugging
    public static void DisplayDebugProperty (string name, object value) {
        if(inst.debugText == null)
            return;

        inst.sb.Append(name);
        inst.sb.Append(": ");
        inst.sb.Append(value);
        inst.sb.Append('\n');
        inst.debugText.SetText(inst.sb);
    }

    public static void ClearDebugProperty () {
        inst.sb.Clear();
    }
    #endregion


    private void Awake () {
        inst = this;
        
        sb = new StringBuilder();
        voxelWorld.Init(LobbyWorldInterface.inst.terrainType, LobbyWorldInterface.inst.matchTerrainInfo.seed, LobbyWorldInterface.inst.matchTerrainInfo.mapSize);
        simulationStore = new SimulationManager();
        reflectionProbe.RenderProbe();

        Application.lowMemory += OnLowMemory;
    }


    private void Start () {
        if(GameUI.inst != null) {
            if(LobbyWorldInterface.inst.matchRulesInfo.scoreMode == ScoreMode.Bounty) {
                GameUI.inst.Cleanup(LobbyWorldInterface.inst.matchRulesInfo.timer, 0, ScoreManager.startScoreBounty);
            } else if(LobbyWorldInterface.inst.matchRulesInfo.scoreMode == ScoreMode.Stocks) {
                GameUI.inst.Cleanup(LobbyWorldInterface.inst.matchRulesInfo.timer, 0, 0);
            }
        }

        LobbyWorldInterface.inst.OnGameSceneLoaded();
    }


    private void FixedUpdate () {
        InputSnapshot inputs = InputListener.MakeSnapshot();

        if(!simulationStore.simulationWorld.IsAlive() || (NetAssist.inst.Mode.HasFlag(NetAssistMode.Record) && !RecordingManager.inst.isPlaying)) {
            return;
        }

        // Deserialize world
        if(NetAssist.IsClientNotHost && simulationStore.localPlayers.ContainsKey(NetAssist.ClientID)) {
            if(LobbyManager.inst.DeserializeLastestWorldState()) {
                simulationStore.ApplyAllReceivedTerrainUpdate();
            }
        }

        // Step player simulation -> projectile -> physics -> replica
        simulationStore.SimulateFixedInterpolationUpdate();
        if(NetAssist.IsClientNotHost && simulationStore.localPlayers.ContainsKey(NetAssist.ClientID)) {
            simulationStore.localPlayers[NetAssist.ClientID].Inputs_UpdateOnClient();
        }
        simulationStore.Simulate(Time.fixedDeltaTime, inputs, lastInputs.GetButtonRaw());

        // Send input
        if(NetAssist.IsClient && simulationStore.localPlayers.ContainsKey(NetAssist.ClientID)) {
            simulationStore.localPlayers[NetAssist.ClientID].SendInputsToServer();
        }

        // Send world and terrain update
        if(NetAssist.IsServer) {
            LobbyWorldInterface.inst.SendEverythingToAllClients();
            TerrainSync.SendTerrainUpdateToClient();
        }

        lastInputs = inputs;
    }


    private void Update () {
        if(lowMemory) {
            DisplayDebugProperty("Low Memory", lowMemory);
        }
#if DEVELOPMENT_BUILD
        DisplayDebugProperty("GRAPH D ALLOC", UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver());
        DisplayDebugProperty("RESERV MEM", UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong());
        DisplayDebugProperty("TOT ALLOC MEM", UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
        DisplayDebugProperty("HEAP SIZE", UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong());
#endif

        // Set max frame rate
        #region Manage Max Frame Rate
        if(!NetAssist.IsHeadlessServer) {
            if(SettingsManager.settings.lowerFPSWhenUnfocused && !Application.isFocused) {
                Application.targetFrameRate = 24;
            } else {
                switch(SettingsManager.settings.maxFPS) {
                    case MaxFPSOption.Max:
                    Application.targetFrameRate = 0;
                    break;
                    case MaxFPSOption.f240:
                    Application.targetFrameRate = 240;
                    break;
                    case MaxFPSOption.f120:
                    Application.targetFrameRate = 120;
                    break;
                    case MaxFPSOption.f60:
                    Application.targetFrameRate = 60;
                    break;
                    case MaxFPSOption.f45:
                    Application.targetFrameRate = 45;
                    break;
                    case MaxFPSOption.f30:
                    Application.targetFrameRate = 30;
                    break;
                }
            }
        }
        #endregion

        // Record Inputs
        if(!NetAssist.IsHeadlessServer) {
            InputListener.RecordInputs();
        }

        if(simulationStore.simulationWorld.IsAlive()) {
            // Apply Interpolation
            simulationStore.SimulateUpdate();

            // Setting damage indicators
            if(GameUI.inst == null)
                goto exit;
            if(NetAssist.IsHeadlessServer)
                goto exit;
            foreach(KeyValuePair<ulong, Entity> entity in SimulationManager.inst.playerSystem.playerEntities) {

                ref PlayerComponent player = ref entity.Value.Get<PlayerComponent>();
                if(entity.Key == NetAssist.ClientID)
                    GameUI.inst.SetSelfDamage(player.damage);
                else {
                    if(PlayerIndicatorDisplay.inst != null)
                        PlayerIndicatorDisplay.inst.UpdateDamageValue(entity.Key, player.damage);
                }

            }
            exit:;
        }
    }


    bool lowMemory = false;
    private void OnLowMemory () {
        lowMemory = true;
    }

    bool alreadyExited = false;
    public void Exit () {
        if(alreadyExited) {
            return;
        }
        alreadyExited = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Application.lowMemory -= OnLowMemory;

        simulationStore?.Dispose();
        voxelWorld?.Dispose();
    }


    private void OnDestroy () {
        Exit();
    }


    #region Visuals
    public void CloseScreen () {
        if(cinematicAnimator != null)
            cinematicAnimator.SetInteger("State", 0);
    }

    public void OpenScreen () {
        if(cinematicAnimator != null)
            cinematicAnimator.SetInteger("State", 2);
    }
#endregion
}