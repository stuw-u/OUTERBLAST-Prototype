using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class RecordingLocalPlayer : MonoBehaviour, ILocalPlayer {

    [Header("Control")]
    public bool selfControlMode;

    [Header("Parameters")]
    public UserData userData;
    public int[] fixedInventoryIDs = { 0, 1, 2, 3, 7 };

    public PlayerRecordMode playerRecordMode;
    public RecordedInputAsset recordedInputAsset;

    private PlayerGameObject playerObject;
    private ulong clientID;
    private MaterialPropertyBlock coloredMpb;

    private InputSnapshot inputSnapshot;
    private InputSnapshot lastInputSnapshot;
    private int inputIndex;

    public void Init (ulong clientID) {
        this.clientID = clientID;

        coloredMpb = new MaterialPropertyBlock();
        coloredMpb.SetColor("_MainColor", userData.DisplayInfo.color);

        inputSnapshot = new InputSnapshot() { moveAxis = float2.zero };
        lastInputSnapshot = inputSnapshot;

        SpawnPlayerObject ();
    }

    private void FixedUpdate () {
        if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Record) && !RecordingManager.inst.isPlaying)
            return;

        if(selfControlMode) {
            lastInputSnapshot = inputSnapshot;
            inputSnapshot = InputListener.MakeSnapshot();
            inputIndex++;
        }
        if(playerRecordMode == PlayerRecordMode.Record) {
            recordedInputAsset.inputSnapshots.Add(inputSnapshot);
        }
        if(playerRecordMode == PlayerRecordMode.Replay) {
            lastInputSnapshot = inputSnapshot;
            if(inputIndex < recordedInputAsset.inputSnapshots.Count) {
                inputSnapshot = recordedInputAsset.inputSnapshots[inputIndex];
            } else {
                inputSnapshot = new InputSnapshot() { moveAxis = float2.zero };
            }
            inputIndex++;
        }
    }

    private void OnDestroy () {
        DespawnPlayerObject();
    }


    #region Setup
    /// <summary>
    /// Spawn the actual player object and adds it to the entity store
    /// </summary>
    private void SpawnPlayerObject () {
        SimulationManager.AddPlayer(this);

        playerObject = Instantiate(AssetsManager.inst.playerObjectPrefab);

        playerObject.userData = UserData;
        playerObject.inventory = fixedInventoryIDs;
        playerObject.playerId = ClientID;
    }


    /// <summary>
    /// Remove the actual player object and removes it to the entity store
    /// </summary>
    private void DespawnPlayerObject () {
        SimulationManager.RemovePlayer(ClientID);

        if(playerObject != null) {
            Destroy(playerObject.gameObject);
        }
    }
    #endregion

    #region Interface
    public ulong ClientID {
        get {
            return clientID;
        }
    }
    public float Ping {
        get {
            return 0f;
        }
    }
    public UserData UserData {
        get {
            return userData;
        }
        set {
            userData = value;
        }
    }
    public bool IsInMatch {
        get {
            return true;
        }
        set {
        }
    }
    public bool IsOwnedByInstance {
        get {
            return true;
        }
    }
    public int[] FixedInventoryIDs {
        get {
            return fixedInventoryIDs;
        }
        set {
            fixedInventoryIDs = value;
        }
    }
    public MaterialPropertyBlock ColoredMaterialPropertyBlock {
        get {
            return coloredMpb;
        }
    }
    public PermissionLevel PermissionLevel {
        get {
            return PermissionLevel.Operator;
        }
        set {}
    }
    public PlayerControlType PlayerControlType {
        get {
            return PlayerControlType.Server;
        }
    }
    public GameObject GameObject {
        get {
            return gameObject;
        }
    }
    public PlayerGameObject PlayerGameObject {
        get {
            return playerObject;
        }
    }
    public float LastTimePressedHurry {
        get {
            return 0f;
        }
        set {

        }
    }

    public void OnUserDataCallback () {}
    public void UpdateSelfRecoveryStyleOnServer () {}
    public void SendInputsToServer () {}
    #endregion

    #region Animation
    public void SetAnimationTrigger (byte itemId) {
        if(playerObject != null) {
            playerObject.itemUserVisuals.SetTrigger(itemId);
        }
    }

    public void SetAnimationTriggerPosition (byte itemId, Vector3 position) {
        if(playerObject != null) {
            playerObject.itemUserVisuals.SetTriggerPosition(itemId, position);
        }
    }
    #endregion

    #region Inputs 
    public int Inputs_RecentInputIndex {
        get {
            return inputIndex;
        }
    }

    public InputSnapshot Inputs_RecentInput {
        get {
            return inputSnapshot;
        }
    }

    public InputSnapshot Inputs_PreviousInput {
        get {
            return lastInputSnapshot;
        }
    }

    public void Inputs_UpdateOnServer () {

    }

    public void Inputs_UpdateOnClient () {

    }
    #endregion
}
