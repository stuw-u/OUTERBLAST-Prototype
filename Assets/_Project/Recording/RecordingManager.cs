using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.Mathematics;
using Blast.ECS;
using Blast.ECS.DefaultComponents;

public class RecordingManager : MonoBehaviour {

    [Header("Prefabs")]
    public RecordingLocalPlayer recordingLocalPlayerPrefab;

    [Header("Parameters")]
    public TerrainTypeAsset terrainType;
    public TerrainStyleAsset terrainStyle;
    public MatchTerrainInfo matchTerrainInfo;
    public MatchRulesInfo matchRulesInfo;
    public PlayerRecordConfig[] actors;

    [Header("References")]
    public CinemachineVirtualCamera cinemachineVirtualCamera;
    public CinemachineVirtualCamera[] cameras;
    public GameObject world;


    public Dictionary<ulong, ILocalPlayer> localPlayers;
    private ulong playerCamFocus = 0;
    int selectedCamera;

    public bool isPlaying = false;

    public static RecordingManager inst;
    private void Awake () {
        inst = this;
    }

    private void Start () {
        Screen.SetResolution(Screen.width, Screen.height, true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.lockState = CursorLockMode.Locked;
        world.SetActive(true);
        localPlayers = new Dictionary<ulong, ILocalPlayer>();

        for(int i = 0; i < cameras.Length; i++) {
            cameras[i].gameObject.SetActive(i == selectedCamera);
        }

        int whoRecords = -1;
        for(int i = 0; i < actors.Length; i++) {
            if(actors[i].recordMode == PlayerRecordMode.Record) {
                whoRecords = i;
                break;
            }
        }
        cinemachineVirtualCamera.gameObject.SetActive(whoRecords != -1);
        playerCamFocus = (ulong)whoRecords;
        Cursor.visible = whoRecords != -1;

        for(int i = 0; i < actors.Length; i++) {
            RecordingLocalPlayer localPlayer = Instantiate(recordingLocalPlayerPrefab);
            localPlayer.userData.Recolor(actors[i].color);
            localPlayer.playerRecordMode = actors[i].recordMode;
            localPlayer.recordedInputAsset = actors[i].inputAsset;
            localPlayers.Add((ulong)i, localPlayer);
            localPlayer.Init((ulong)i);

            localPlayer.selfControlMode = actors[i].recordMode == PlayerRecordMode.Record;
            if(actors[i].recordMode == PlayerRecordMode.Record) {
                actors[i].inputAsset.inputSnapshots = new List<InputSnapshot>();
            }

            if(SimulationManager.inst.playerSystem.playerEntities.TryGetValue((ulong)i, out Entity player)) {
                player.Get<Position>().value = actors[i].startPos;
            }
        }
    }

    private void Update () {
        if(localPlayers.ContainsKey(playerCamFocus)) {
            cinemachineVirtualCamera.transform.position = localPlayers[playerCamFocus].PlayerGameObject.headX.GetChild(0).position;
            cinemachineVirtualCamera.transform.eulerAngles = localPlayers[playerCamFocus].PlayerGameObject.headX.GetChild(0).eulerAngles;
        }

        if(Input.GetKeyDown(KeyCode.Alpha0)) {
            selectedCamera++;
            if(selectedCamera >= cameras.Length) {
                selectedCamera = 0;
            }

            for(int i = 0; i < cameras.Length; i++) {
                cameras[i].gameObject.SetActive(i == selectedCamera);
            }
        }

        if(Input.GetKeyDown(KeyCode.P)) {
            for(int i = 0; i < cameras.Length; i++) {
                if(cameras[i].TryGetComponent<Cinemachine.CinemachineDollyCart>(out CinemachineDollyCart dc)) {
                    dc.m_Position = 0;
                }
            }
            isPlaying = true;
        }
    }

    private void FixedUpdate () {
        
    }

    private void OnDestroy () {
#if UNITY_EDITOR
        for(int i = 0; i < actors.Length; i++) {
            if(actors[i].inputAsset != null) {
                UnityEditor.EditorUtility.SetDirty(actors[i].inputAsset);
            }
        }
#endif
    }
}

[System.Serializable]
public class PlayerRecordConfig {
    public float3 startPos;
    public Color color;
    public PlayerRecordMode recordMode;
    public RecordedInputAsset inputAsset;
}

public enum PlayerRecordMode {
    Idle,
    Record,
    Replay
}
