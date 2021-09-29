using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Mathematics;
using Unity.Collections;

using MLAPI;
using MLAPI.Messaging;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using MLAPI.NetworkVariable;
using MLAPI.NetworkVariable.Collections;

using Blast.ECS;


public enum PermissionLevel {
    Client,
    Operator
}


[DefaultExecutionOrder(1)]
public class LocalPlayer : NetworkBehaviour, ILocalPlayer {

    [Header("References")]
    public InputBufferController inputBufferController;
    public PlayerControlType controlType;
    public PermissionLevel permissionLevel;

    [HideInInspector] public PlayerGameObject playerObject;
    [HideInInspector] public float lastTimePressedHurry;
    [HideInInspector, System.NonSerialized] public int[] fixedInventoryIDs = { 0, 1, 2, 3, 7 };

    public NetworkVariable<UserData> userData = new NetworkVariable<UserData>(new NetworkVariableSettings() {
        SendNetworkChannel = MLAPI.Transports.NetworkChannel.ReliableRpc,
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });
    public NetworkVariable<bool> isInMatch = new NetworkVariable<bool>(new NetworkVariableSettings() {
        SendNetworkChannel = MLAPI.Transports.NetworkChannel.ReliableRpc,
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });
    public NetworkVariable<ushort> rtt = new NetworkVariable<ushort>(new NetworkVariableSettings() {
        SendNetworkChannel = MLAPI.Transports.NetworkChannel.UnreliableRpc,
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });
    public NetworkVariable<short> displayScore = new NetworkVariable<short>(new NetworkVariableSettings() {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });
    public MaterialPropertyBlock coloredMpb;
    private bool playerAddedToLobby;

    private RecoveryStyles userRecoveryStyle;

    int rttTicking = 0;
    string messageName_GetInputsAsServerRpc;

    private void FixedUpdate () {
        if(!playerAddedToLobby) {
            playerAddedToLobby = LobbyWorldInterface.RegisterLocalPlayer(this);
        }
        if(NetAssist.IsServer) {
            if(rttTicking >= 30) {
                rtt.Value = (ushort)NetAssist.GetPlayerRTT(OwnerClientId);
                rttTicking = 0;
            } else {
                rttTicking++;
            }
        }
    }
    private void Update () {
        if(NetAssist.IsHeadlessServer) {
            return;
        }
        if(Blast.Settings.SettingsManager.settings.showRTT && NetAssist.IsClientNotHost && IsOwner) {
            if(GameManager.inst != null) {
                GameManager.DisplayDebugProperty("Ping ", rtt.Value);
            }
        }
    }



    #region Setup
    /// <summary>
    /// Prepares scene loading events, spawn player if it's the time, init buffer controller
    /// </summary>
    public override void NetworkStart () {
        DontDestroyOnLoad(this);

        messageName_GetInputsAsServerRpc = $"GetInputsAsServerRpc_{OwnerClientId}";
        CustomMessagingManager.RegisterNamedMessageHandler(messageName_GetInputsAsServerRpc, GetInputsAsServerRpc_NamedMessage);

        if(NetAssist.IsHost && OwnerClientId == NetAssist.ClientID) {
            permissionLevel = PermissionLevel.Operator;
        }

        // Adds the local player to the lobby (Manages cases where this is the host, and the lobby isn't there yet)
        playerAddedToLobby = LobbyWorldInterface.RegisterLocalPlayer(this);

        // Figure out control type
        if(IsOwner) {
            controlType = PlayerControlType.Self;
        } else {
            if(IsServer) {
                controlType = PlayerControlType.Remote;
            } else {
                controlType = PlayerControlType.Server;
            }
        }

        // Setup user data networked variable
        userData.SetNetworkBehaviour(this);
        userData.OnValueChanged += OnUserDataChanged;
        if(NetAssist.IsHost && NetAssist.ClientID ==  OwnerClientId) {
            userData.Value = NetAssist.inst.selfUserData;
        }
        OnUserDataCallback();
        isInMatch.OnValueChanged += OnJoinMatch;

        // Setup scene loading events
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        inputBufferController.Init(userData.Value.SharedSettings.inputBufferMode);
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    public void OnJoinMatch (bool lastInMatchState, bool inMatchState) {
        if(inMatchState) {
            LobbyManager.inst.CancelWorldState();
            SpawnPlayerObject();
        }
    }

    /// <summary>
    /// Spawns a player when the main scene gets loaded
    /// </summary>
    private void OnSceneLoaded (Scene scene, LoadSceneMode loadSceneMode) {
        if(scene.name == "Main") {
        }
    }


    /// <summary>
    /// Removes the player when the main scene gets unloaded
    /// </summary>
    private void OnSceneUnloaded (Scene scene) {
        if(scene.name == "Main") {
            DespawnPlayerObject();
        }
    }


    private void OnDestroy () {
        DespawnPlayerObject();

        CustomMessagingManager.UnregisterNamedMessageHandler(messageName_GetInputsAsServerRpc);

        userData.OnValueChanged -= OnUserDataChanged;
        isInMatch.OnValueChanged -= OnJoinMatch;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }


    /// <summary>
    /// Spawn the actual player object and adds it to the entity store
    /// </summary>
    private void SpawnPlayerObject () {
        SimulationManager.AddPlayer(this);

        if(controlType == PlayerControlType.Self) {
            playerObject = Instantiate(AssetsManager.inst.selfPlayerObjectPrefab);
            playerObject.isSelfControlled = true;
        } else {
            playerObject = Instantiate(AssetsManager.inst.playerObjectPrefab);
            playerObject.isSelfControlled = false;
        }

        playerObject.userData = userData.Value;
        playerObject.inventory = fixedInventoryIDs;
        playerObject.playerId = OwnerClientId;
    }


    /// <summary>
    /// Remove the actual player object and removes it to the entity store
    /// </summary>
    private void DespawnPlayerObject () {
        SimulationManager.RemovePlayer(OwnerClientId);

        if(playerObject != null) {
            Destroy(playerObject.gameObject);
        }
    }
    #endregion

    #region User Data
    public void UpdateSelfRecoveryStyleOnServer () {
        RegisterRecoveryStyleServerRPC((byte)NetAssist.inst.selfUserData.SharedSettings.recoveryStyle);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RegisterRecoveryStyleServerRPC (byte style) {
        userRecoveryStyle = (RecoveryStyles)style;
        switch(userRecoveryStyle) {
            case RecoveryStyles.Delta:
            fixedInventoryIDs[3] = 3;
            break;

            case RecoveryStyles.ChargeJet:
            fixedInventoryIDs[3] = 4;
            break;

            case RecoveryStyles.ImpulseJet:
            fixedInventoryIDs[3] = 5;
            break;

            case RecoveryStyles.Grappler:
            fixedInventoryIDs[3] = 6;
            break;
        }
    }

    public void OnUserDataChanged (UserData lastUserData, UserData userData) {
        OnUserDataCallback();
    }

    public void OnUserDataCallback () {
        coloredMpb = new MaterialPropertyBlock();
        coloredMpb.SetColor("_MainColor", userData.Value.DisplayInfo.color);

        if(PlayerIndicatorDisplay.inst != null) {
            PlayerIndicatorDisplay.inst.UpdateIndicator(OwnerClientId);
        }
        if(LobbyMenu.inst != null) {
            LobbyMenu.inst.LoadDisplay(this);
        }
    }
    #endregion
    
    #region Inputs (Network and Streams)
    public void Inputs_UpdateOnServer () {
        inputBufferController.UpdateOnServer();
    }

    public void Inputs_UpdateOnClient () {
        inputBufferController.UpdateOnClient();
    }

    public int Inputs_RecentInputIndex {
        get {
            return inputBufferController.recentInputIndex;
        }
    }

    public InputSnapshot Inputs_RecentInput {
        get {
            return inputBufferController.GetRecentInput();
        }
    }

    public InputSnapshot Inputs_PreviousInput {
        get {
            return inputBufferController.previousSnapshot;
        }
    }

    public void WriteCompressInputsToStream (NetworkWriter writer) {

        NativeArray<InputSnapshot> snapshots = new NativeArray<InputSnapshot>(8, Allocator.Temp);

        // Gather inputs
        int frameCount = SimulationManager.inst.frames.Count - 1;
        for(int i = 0; i < 8; i++) {
            if(frameCount - i < 0) {
                snapshots[i] = new InputSnapshot() { moveAxis = float2.zero };
            } else {
                snapshots[i] = SimulationManager.inst.frames[frameCount - i].clientInputs;
            }
        }

        // Write held item
        byte lastSelectedItem = 0;
        for(int i = 0; i < 8; i++) {
            writer.WriteBit(lastSelectedItem != snapshots[i].selectedFixedInventoryItem);
            if(lastSelectedItem != snapshots[i].selectedFixedInventoryItem) {
                writer.WriteBits(snapshots[i].selectedFixedInventoryItem, 4);
            }
        }

        // Write buttons
        for(int i = 0; i < 8; i++) {
            writer.WriteByte(snapshots[i].GetButtonRaw());
        }

        // Compress moveAxis
        byte lastMoveX = 0;
        byte lastMoveY = 0;
        for(int i = 0; i < 8; i++) {
            snapshots[i].GetMoveAxisRaw(out byte moveX, out byte moveY);

            writer.WriteBit(lastMoveX != moveX);
            if(lastMoveX != moveX) {
                writer.WriteByte(moveX);
            }

            writer.WriteBit(lastMoveY != moveY);
            if(lastMoveY != moveY) {
                writer.WriteByte(moveY);
            }

            lastMoveX = moveX;
            lastMoveY = moveY;
        }

        // Compress lookAxis
        float2 lastLook = float2.zero;
        for(int i = 0; i < 8; i++) {
            float2 lookAxis = snapshots[i].lookAxis;

            writer.WriteBit(lastLook.x != lookAxis.x);
            if(lastLook.x != lookAxis.x) {
                writer.WriteSinglePacked(lookAxis.x);
            }

            writer.WriteBit(lastLook.y != lookAxis.y);
            if(lastLook.y != lookAxis.y) {
                writer.WriteSinglePacked(lookAxis.y);
            }

            lastLook = lookAxis;
        }

        snapshots.Dispose();
    }

    public NativeArray<InputSnapshot> ReadDecompressInputsFromStream (NetworkReader reader) {

        NativeArray<InputSnapshot> snapshots = new NativeArray<InputSnapshot>(8, Allocator.Temp);

        // Read held item
        byte lastSelectedItem = 0;
        for(int i = 0; i < 8; i++) {
            InputSnapshot snapshot = snapshots[i];

            if(reader.ReadBit()) {
                lastSelectedItem = (byte)reader.ReadBits(4);
            }

            snapshot.selectedFixedInventoryItem = lastSelectedItem;
            snapshots[i] = snapshot;
        }

        // Read buttons
        for(int i = 0; i < 8; i++) {
            InputSnapshot snapshot = snapshots[i];

            snapshot.SetButtonRaw((byte)reader.ReadByte());
            snapshots[i] = snapshot;
        }

        // Decompress moveAxis
        byte lastMoveX = 0;
        byte lastMoveY = 0;
        for(int i = 0; i < 8; i++) {
            InputSnapshot snapshot = snapshots[i];

            if(reader.ReadBit()) {
                lastMoveX = (byte)reader.ReadByte();
            }
            if(reader.ReadBit()) {
                lastMoveY = (byte)reader.ReadByte();
            }

            snapshot.SetMoveAxisRaw(lastMoveX, lastMoveY);
            snapshots[i] = snapshot;
        }

        // Decompress lookAxis
        float2 lastLook = float2.zero;
        for(int i = 0; i < 8; i++) {
            InputSnapshot snapshot = snapshots[i];

            if(reader.ReadBit()) {
                lastLook.x = reader.ReadSinglePacked();
            }
            if(reader.ReadBit()) {
                lastLook.y = reader.ReadSinglePacked();
            }

            snapshot.lookAxis = lastLook;
            snapshots[i] = snapshot;
        }

        return snapshots;
    }

    public void SendInputsToServer () {
        if(NetAssist.IsHost) {
            return;
        }

        using(PooledNetworkBuffer stream = PooledNetworkBuffer.Get())
        using(PooledNetworkWriter writer = PooledNetworkWriter.Get(stream)) {

            writer.WriteInt32Packed(SimulationManager.inst.currentFrame);
            WriteCompressInputsToStream(writer);

            if(SimulationManager.inst != null) {
                if(SimulationManager.inst.playerSystem.playerEntities.TryGetValue(OwnerClientId, out Entity playerEntity)) {
                    writer.WriteBit(true);
                    ref var itemUser = ref playerEntity.Get<ItemUserComponent>();
                    
                    writer.WriteByte((byte)itemUser.lastHeldItem);
                    writer.WriteBit(itemUser.lastHeldItemUsed);
                } else {
                    writer.WriteBit(false);
                }
            } else {
                writer.WriteBit(false);
            }

            //InvokeServerRpcPerformance(GetInputsAsServerRpc, stream, "Inputs");
            CustomMessagingManager.SendNamedMessage(messageName_GetInputsAsServerRpc, NetworkManager.Singleton.ServerClientId, stream, MLAPI.Transports.NetworkChannel.UnreliableRpc);
        }
    }

    int lastInput;
    private void GetInputsAsServerRpc_NamedMessage (ulong clientId, Stream stream) {
        using(PooledNetworkReader reader = PooledNetworkReader.Get(stream)) {

            int currentClientFrame = reader.ReadInt32Packed();
            NativeArray<InputSnapshot> snapshots = ReadDecompressInputsFromStream(reader);
            inputBufferController.EnqueueInputs(snapshots, currentClientFrame);
            snapshots.Dispose();

            if(SimulationManager.inst.playerSystem.playerEntities.TryGetValue(clientId, out Entity playerEntity) && SimulationManager.inst.simulationWorld.IsAlive()) {
                if(reader.ReadBit()) {
                    ref var itemUser = ref playerEntity.Get<ItemUserComponent>();

                    itemUser.lastHeldItem = reader.ReadByte();
                    itemUser.lastHeldItemUsed = reader.ReadBit();
                }
            } else {
                reader.ReadBit();
                reader.ReadByte();
                reader.ReadBit();
            }

            if(currentClientFrame != lastInput + 1) {
                inputBufferController.OnPacketLossOrMissing();
            }
            lastInput = currentClientFrame;
        }
    }
    #endregion

    #region Animation
    public void SetAnimationTrigger (byte itemId) {
        if(IsOwner) {
            if(playerObject != null) {
                playerObject.itemUserVisuals.SetTrigger(itemId);
            }
        } else if(NetAssist.IsServer) {
            if(playerObject != null) {
                playerObject.itemUserVisuals.SetTrigger(itemId);
            }
        }
        if(NetAssist.IsServer) {
            //InvokeClientRpcOnEveryoneExcept(AnimationTrigger, OwnerClientId, itemId);
            AnimationTriggerClientRPC(itemId, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = LobbyManager.inst.clientLists.exceptClient[OwnerClientId] } });
        }
    }

    public void SetAnimationTriggerPosition (byte itemId, Vector3 position) {
        if(IsOwner) {
            if(playerObject != null) {
                playerObject.itemUserVisuals.SetTriggerPosition(itemId, position);
            }
        } else if(NetAssist.IsServer) {
            if(playerObject != null) {
                playerObject.itemUserVisuals.SetTriggerPosition(itemId, position);
            }
        }
        if(NetAssist.IsServer) {
            AnimationTriggerPositionClientRPC(itemId, position, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = LobbyManager.inst.clientLists.exceptClient[OwnerClientId] } });
        }
    }

    [ClientRpc]
    private void AnimationTriggerClientRPC (byte itemId, ClientRpcParams clientRpcParams = default) {
        if(playerObject != null && !NetAssist.IsServer) {
            playerObject.itemUserVisuals.SetTrigger(itemId);
        }
    }

    [ClientRpc]
    private void AnimationTriggerPositionClientRPC (byte itemId, Vector3 position, ClientRpcParams clientRpcParams = default) {
        if(playerObject != null && !NetAssist.IsServer) {
            playerObject.itemUserVisuals.SetTriggerPosition(itemId, position);
        }
    }
    #endregion

    #region Interface
    public UserData UserData {
        get {
            return userData.Value;
        }
        set {
            userData.Value = value;
        }
    }
    public bool IsInMatch {
        get {
            return isInMatch.Value;
        }
        set {
            isInMatch.Value = value;
        }
    }
    public bool IsOwnedByInstance {
        get {
            return IsOwner;
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
    public ulong ClientID {
        get {
            return OwnerClientId;
        }
    }
    public float Ping {
        get {
            return rtt.Value;
        }
    }
    public PermissionLevel PermissionLevel {
        get {
            return permissionLevel;
        }
        set {
            permissionLevel = value;
        }
    }
    public PlayerControlType PlayerControlType {
        get {
            return controlType;
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
            return lastTimePressedHurry;
        }
        set {
            lastTimePressedHurry = value;
        }
    }

    public MaterialPropertyBlock ColoredMaterialPropertyBlock {
        get {
            return coloredMpb;
        }
    }
    #endregion

}