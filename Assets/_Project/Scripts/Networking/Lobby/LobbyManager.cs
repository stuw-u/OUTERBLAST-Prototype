using Blast.ECS;
using Blast.NetworkedEntities;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;


public enum LocalLobbyState {
    InLobby,
    WaitingToStartIntro,
    InIntro,
    InGame
}

public enum LobbyState {
    InLobby,
    WaitingToStartIntro,
    InGame
}

public enum GameScene {
    Lobby,
    Game
}


[System.Serializable]
public class MatchTerrainInfo : AutoNetworkSerializable {
    public int seed;
    public int terrainType;
    public int terrainStyle;
    public float mapSize;
}

[System.Serializable]
public class MatchRulesInfo : AutoNetworkSerializable {
    public int timer = 7;
    public ScoreMode scoreMode;
    public int stocks = 5;
    public bool damageMode = true;
    public bool randomizeMap = true;
    public int selectedMap = 0;
    public bool automaticMapSize = true;
    public float mapSize = 1f;
}

public class LobbyManager : NetworkBehaviour {

    [Header("Reference")]
    public ScoreManager scoreManager;
    public ChatManager chatManager;

    [Header("Parameters")]
    public bool IsHeadlessServerLobby;
    public TerrainStyleAsset fallbackStyle;
    

    // Lobby Vars
    public static LobbyManager inst;
    public ClientLists clientLists;
    public Dictionary<ulong, ILocalPlayer> localPlayers;
    public NetworkVariable<LobbyState> _lobbyStateNetVar = new NetworkVariable<LobbyState>(new NetworkVariableSettings() {
            WritePermission = NetworkVariablePermission.ServerOnly,
            SendNetworkChannel = MLAPI.Transports.NetworkChannel.ReliableRpc
    }, LobbyState.InLobby);
    public static LobbyState LobbyState {
        get {
            if(inst == null)
                return LobbyState.InLobby;
            else
                return inst._lobbyStateNetVar.Value;
        }
    }
    public static LocalLobbyState LocalLobbyState {
        get {
            if(inst == null)
                return LocalLobbyState.InLobby;
            else
                return inst._localGameState;
        }
    }
    private List<ulong> localPlayersClientId;
    private LocalLobbyState _localGameState;

    // Match vars
    public MatchRulesInfo matchRulesInfo;
    public MatchTerrainInfo matchTerrainInfo { get; private set; }  // Contains all the info needed for the client to prepare the match
    [HideInInspector] public bool loadMenuInLobby;                  // A flag to display the lobby when loading the menu scene
    public HashSet<ulong> readyPlayers;                             // A hashset to remember which player are already ready in a server lobby
    private int readyPlayerCount;                                   // How many players are ready to start the match now?
    private float syncedStartTimer;                                 // A timer of how long to wait for all players to be ready before starting anyway

    // Consts
    public const float syncedStartLimit = 10f;                      // How long should the server wait for clients to be done loading the map before starting anyway


    public static TerrainStyleAsset terrainStyle {
        get {
            if(inst == null || AssetsManager.inst == null) {
                return null;
            } else if(inst.matchTerrainInfo == null) {
                return null;
            }
            return AssetsManager.inst.terrainStyleCollection.terrainStyles[inst.matchTerrainInfo.terrainStyle];
        }
    }

    public static TerrainTypeAsset terrainType {
        get {
            if(inst == null || AssetsManager.inst == null) {
                return null;
            } else if(inst.matchTerrainInfo == null) {
                return null;
            }
            return AssetsManager.inst.terrainTypeCollection.terrainTypes[inst.matchTerrainInfo.terrainType];
        }
    }

    private void FixedUpdate () {
        UpdateMatchSyncStartTimer();
    }


    #region Init Lobby
    private void Awake () {
        inst = this;
        localPlayers = new Dictionary<ulong, ILocalPlayer>();
        localPlayersClientId = new List<ulong>();

        // Add already connected clients (There should only be a host)
        foreach(MLAPI.Connection.NetworkClient client in NetworkManager.Singleton.ConnectedClientsList) {
            LocalPlayer localPlayer = client.PlayerObject.GetComponent<LocalPlayer>();
            localPlayers.Add(localPlayer.OwnerClientId, localPlayer);
            localPlayersClientId.Add(localPlayer.OwnerClientId);
        }

        if(PlayerIndicatorDisplay.inst != null) {
            PlayerIndicatorDisplay.inst.UpdateAllIndicators();
        }

        CustomMessagingManager.RegisterNamedMessageHandler("GetMatchInfoClientRPC", GetMatchInfoClientRPC_UnnamedMessage);
        CustomMessagingManager.RegisterNamedMessageHandler("GetFinalScoresClientRPC", GetFinalScoresClientRPC_UnnamedMessage);
        CustomMessagingManager.RegisterNamedMessageHandler("GetPlayerEffectUpdatesClientRPC", GetPlayerEffectUpdatesClientRPC_UnnamedMessage);
        CustomMessagingManager.RegisterNamedMessageHandler("GetStaticEntityEventsClientRPC", GetStaticEntityEventsClientRPC_UnnamedMessage);
        CustomMessagingManager.RegisterNamedMessageHandler("GetWorldAsClientClientRPC", GetWorldAsClientClientRPC_UnnamedMessage);

        DontDestroyOnLoad(this);
    }
    
    public override void NetworkStart () {
        playerEffectNetworkEvents = new Queue<PlayerEffectEvent>();
        //recentWorldStates = new List<PooledBitStream>();
        recentWorldState = PooledNetworkBuffer.Get();
        recentWorldStatesIndex = new List<int>();

        matchRulesInfo = new MatchRulesInfo();
        if(LobbyMenu.inst != null) {
            LobbyMenu.inst.OnGetLobbyType();
        }

        if(NetAssist.IsClientNotHost) {
            SendUserData(NetAssist.inst.selfUserData);
        }

        if(NetAssist.IsHeadlessServer) {
            readyPlayers = new HashSet<ulong>();
        }
        
        _lobbyStateNetVar.OnValueChanged += OnLobbyStateValueChanged;

        DiscordController.SetAsInLobby(1);
    }
    #endregion

    #region Close Lobby
    private void OnDestroy () {
        recentWorldState.Dispose();
        /*foreach(var stream in recentWorldStates) {
            stream.Dispose();
        }*/

        _lobbyStateNetVar.OnValueChanged -= OnLobbyStateValueChanged;

        if(LobbyMenu.inst != null)
            MenuManager.inst.ExitLobby();
    }

    public void RemoteCloseLobby () {
        //InvokeClientRpcOnEveryone(CloseLobbyRPC);
        CloseLobbyClientRPC();
    }

    [ClientRpc]
    private void CloseLobbyClientRPC () {
        if(NetAssist.IsClient) {
            NetAssist.ExitGameAndLobby();
        }
    }
    #endregion

    #region Clients
    // Actions upon client connection to the lobby
    public void OnClientConnects (ulong clientID) {
        if(LobbyState == LobbyState.InLobby && IsHeadlessServerLobby) {
            UpdateReadyPlayerCount();
        }
        if(LobbyState == LobbyState.InLobby && !NetAssist.IsHeadlessServer) {
            DiscordController.SetAsInLobby(localPlayers.Count);
        }
    }


    // Actions upon client disconnection to the lobby
    public void OnClientDisconnects (ulong clientID, bool clearLocalPlayer = false) {
        if(LobbyState == LobbyState.InLobby && IsHeadlessServerLobby) {
            UpdateReadyPlayerCount();
        }
        if(LobbyState == LobbyState.InLobby && !NetAssist.IsHeadlessServer) {
            DiscordController.SetAsInLobby(localPlayers.Count);
        }
        if(LobbyState == LobbyState.InGame && NetAssist.IsServer) {
            scoreManager.RemovePlayer(clientID);
        }
        TabMenu.RefreshTabMenuData();
        if(localPlayers.ContainsKey(clientID)) {
            ChatManager.SendServerMessageEveryone($"{localPlayers[clientID].UserData.DisplayInfo.username} left the lobby");
        }

        if(LobbyMenu.inst != null) {
            LobbyMenu.inst.RemoveDisplay(clientID);
        }
        RemoveLocalPlayer(clientID);

        if(clientID == NetAssist.ClientID) {
            ErrorPromptUI.ShowError(0, () => {
                NetAssist.ExitGameAndLobby();
            });
        }
    }


    // Registering a local player from a client that just joined
    public static bool RegisterLocalPlayer (LocalPlayer localPlayer) {
        if(inst == null)
            return false;
        if(inst.localPlayers.ContainsKey(localPlayer.OwnerClientId))
            return true;

        inst.localPlayers.Add(localPlayer.OwnerClientId, localPlayer);
        inst.localPlayersClientId.Add(localPlayer.OwnerClientId);
        return true;
    }


    // Unregistering a local player that was connected to the lobby
    private void RemoveLocalPlayer (ulong playerId) {
        localPlayersClientId.Remove(playerId);
        localPlayers.Remove(playerId);
    }


    // Provides a client order index for thing such as finding a spawning position
    public int GetClientOrderIndex (ulong playerId) {
        int index = 0;
        localPlayersClientId.Sort();
        for(int i = 0; i < localPlayersClientId.Count; i++) {
            if(localPlayersClientId[i] == playerId) {
                return index;
            }
            index++;
        }
        return 0;
    }

    public void SendRecoveryStyle () {

    }

    // A method to be called for clients to request their wanted username and collect
    public void SendUserData (UserData userData) {
        RegisterUserDataServerRPC(userData.DisplayInfo, userData.SharedSettings);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RegisterUserDataServerRPC (UserDisplayInfo userDisplayInfo, UserSharedSettings userSharedSettings, ServerRpcParams serverRpcParams = default) {
        if(localPlayers.TryGetValue(serverRpcParams.Receive.SenderClientId, out ILocalPlayer localPlayer)) {
            localPlayer.UserData = new UserData(
                serverRpcParams.Receive.SenderClientId,
                userDisplayInfo,
                userSharedSettings
            );
            if(NetAssist.IsHost) {
                localPlayer.OnUserDataCallback();
            }

            ChatManager.SendServerMessageEveryone($"{localPlayers[serverRpcParams.Receive.SenderClientId].UserData.DisplayInfo.username} joined the lobby");
        }
    }


    // Applies a given userData to a given client
    public void RegisterUserData (ulong clientId, UserData userData) {
        if(localPlayers.TryGetValue(clientId, out ILocalPlayer localPlayer)) {
            localPlayer.UserData = new UserData(
                clientId,
                userData.DisplayInfo,
                userData.SharedSettings
            );
            TabMenu.RefreshTabMenuData();
        }
    }
    #endregion


    #region Lobby Readying
    public void ReadyClient () {
        ReadyClientServerRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReadyClientServerRPC (ServerRpcParams serverRpcParams = default) {
        if(LobbyState == LobbyState.InLobby || !IsHeadlessServerLobby || readyPlayers.Contains(serverRpcParams.Receive.SenderClientId))
            return;
        readyPlayers.Add(serverRpcParams.Receive.SenderClientId);
        UpdateReadyPlayerCount();
    }
    
    [ClientRpc]
    private void ReadyPlayerCallbackClientRPC (int ready, int total) {
        if(LobbyMenu.inst != null) {
            LobbyMenu.inst.DisplayReadyPlayerCount(ready, total);
        }
    }

    private void UpdateReadyPlayerCount () {
        if(localPlayers.Count == 0) {
            return;
        }
        //InvokeClientRpcOnEveryone(ReadyPlayerCallbackClientRPC, readyPlayers.Count, localPlayers.Count);
        ReadyPlayerCallbackClientRPC(readyPlayers.Count, localPlayers.Count);
        if(readyPlayers.Count == localPlayers.Count) {
            readyPlayers.Clear();
            StartMatch();
        }
    }
    #endregion

    #region Match Intro Readying
    private void UpdateMatchSyncStartTimer () {
        if(NetAssist.IsServer && LobbyState == LobbyState.WaitingToStartIntro) {
            if(syncedStartTimer > 0f) {
                syncedStartTimer = math.max(0f, syncedStartTimer - Time.fixedDeltaTime);

                if(syncedStartTimer == 0f) {
                    _lobbyStateNetVar.Value = LobbyState.InGame;
                    Debug.Log(_lobbyStateNetVar.Value);
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReadyForSyncStartServerRPC (ServerRpcParams serverRpcParams = default) {
        if(LobbyState == LobbyState.InGame) {
            return;
        }
        if(localPlayers.ContainsKey(serverRpcParams.Receive.SenderClientId)) {
            if(!localPlayers[serverRpcParams.Receive.SenderClientId].IsInMatch) {
                localPlayers[serverRpcParams.Receive.SenderClientId].IsInMatch = true;
                readyPlayerCount++;
                if(readyPlayerCount == localPlayers.Count) {
                    StartIntro();
                }
            }
        }
    }
    #endregion

    #region Switch Scene
    public void PrepareToLoadScene (GameScene gameScene) {
        Time.timeScale = 1f;

        if(gameScene == GameScene.Lobby) {

            // Check to be sure we aren't already in the scene we want to load
            if(SceneManager.GetActiveScene().name == "Menu")
                return;

            // Prepares scene switch animation and coroutines
            loadMenuInLobby = true;
            GameManager.inst.CloseScreen();
            GameUI.inst.openingAnimator.Close();
            inst.StartCoroutine(inst.SceneSwitchCoroutine(gameScene));
            
        } else if(gameScene == GameScene.Game) {

            // Check to be sure we aren't already in the scene we want to load
            if(SceneManager.GetActiveScene().name == "Main")
                return;

            // Prepares scene switch animation and coroutines
            MenuManager.inst.CinematicClose();
            inst.StartCoroutine(inst.SceneSwitchCoroutine(gameScene));

        }
    }

    private bool isSwitchingScene;
    private IEnumerator SceneSwitchCoroutine (GameScene gameScene) {
        if(isSwitchingScene) {
            yield break;
        }
        isSwitchingScene = true;
        yield return new WaitForSecondsRealtime(NetAssist.inst.Mode.HasFlag(NetAssistMode.Debug) ? 0f : 1f);
        
        if(gameScene == GameScene.Lobby) {
            GameManager.inst.Exit();
            SceneManager.LoadScene("Menu", LoadSceneMode.Single);
        } else if(gameScene == GameScene.Game) {
            if(matchTerrainInfo != null) {
                SceneManager.LoadScene("Main");
            } else {
                ErrorPromptUI.ShowError(2);
            }
        }

        isSwitchingScene = false;

    }
    #endregion

    #region Match Info Sharing
    public void SendMatchInfo () {

        // Prepare map type, map timers, game mode, etc.
        MatchTerrainInfo matchTerrainInfo = new MatchTerrainInfo() {
            seed = UnityEngine.Random.Range(-65536, 65536),
            mapSize = math.select(matchRulesInfo.mapSize, localPlayers.Count / 8f, matchRulesInfo.automaticMapSize),
            terrainType = AssetsManager.inst.terrainTypeCollection.GetRandom(),
            terrainStyle = AssetsManager.inst.terrainStyleCollection.GetRandom()
        };
        if(!matchRulesInfo.randomizeMap) {
            matchTerrainInfo.terrainType = matchRulesInfo.selectedMap;
        }

        using(PooledNetworkBuffer stream = PooledNetworkBuffer.Get())
        using(PooledNetworkWriter writer = PooledNetworkWriter.Get(stream)) {
            NetworkSerializer networkSerializer = new NetworkSerializer(writer);
            matchTerrainInfo.NetworkSerialize(networkSerializer);
            matchRulesInfo.NetworkSerialize(networkSerializer);
            writer.WriteByte((byte)localPlayers.Count);

            foreach(KeyValuePair<ulong, ILocalPlayer> kvp in localPlayers) {
                writer.WriteByte((byte)kvp.Value.FixedInventoryIDs.Length);
                for(int l = 0; l < kvp.Value.FixedInventoryIDs.Length; l++) {
                    writer.WriteByte((byte)kvp.Value.FixedInventoryIDs[l]);
                }
                writer.WriteUInt64(kvp.Key);
            }
            //InvokeClientRpcOnEveryonePerformance(GetMatchInfoClientRPC, stream, "UserData");
            CustomMessagingManager.SendNamedMessage("GetMatchInfoClientRPC", null, stream, MLAPI.Transports.NetworkChannel.ReliableRpc);
        }
        this.matchTerrainInfo = matchTerrainInfo;
    }

    private void GetMatchInfoClientRPC_UnnamedMessage (ulong clientId, Stream stream) {
        if(NetAssist.IsServer)
            return;
        using(PooledNetworkReader reader = PooledNetworkReader.Get(stream)) {
            NetworkSerializer networkSerializer = new NetworkSerializer(reader);
            MatchTerrainInfo matchTerrainInfo = new MatchTerrainInfo();
            MatchRulesInfo matchRulesInfo = new MatchRulesInfo();

            matchTerrainInfo.NetworkSerialize(networkSerializer);
            matchRulesInfo.NetworkSerialize(networkSerializer);
            this.matchTerrainInfo = matchTerrainInfo;
            this.matchRulesInfo = matchRulesInfo;

            int readCount = reader.ReadByte();
            for(int i = 0; i < readCount; i++) {
                int[] inventory = new int[reader.ReadByte()];
                for(int l = 0; l < inventory.Length; l++) {
                    inventory[l] = reader.ReadByte();
                }
                localPlayers[reader.ReadUInt64()].FixedInventoryIDs = inventory;
            }
        }
    }
    #endregion


    
    // [Called from client and server] Called when the lobby state value gets changed by the server
    private void OnLobbyStateValueChanged (LobbyState previousValue, LobbyState newValue) {
        if(newValue == LobbyState.InLobby) {
            #region InLobby
            _localGameState = LocalLobbyState.InLobby;
            PrepareToLoadScene(GameScene.Lobby); // Load lobby scene
            matchTerrainInfo = null; // Clear match info so we know if we accidently got throw in a match we weren't prepared to state

            // Update discord status
            if(!NetAssist.IsHeadlessServer)
                DiscordController.SetAsInLobby(localPlayers.Count);
            #endregion
        } else if(newValue == LobbyState.WaitingToStartIntro) {
            #region WaitingToStartIntro
            scoreManager.lastestFinalScores = null;
            _localGameState = LocalLobbyState.WaitingToStartIntro;
            PrepareToLoadScene(GameScene.Game); // Load game scene
            #endregion
        } else if(newValue == LobbyState.InGame) {
            #region InGame
            /*if(NetworkAssistant.IsClientNotHost) {
                if(!localPlayers[NetworkAssistant.ClientID].isInMatch.Value && SceneManager.GetActiveScene().name == "Main") {
                    Debug.Log("Goto lobby?");
                    _localGameState = LocalLobbyState.InLobby;
                    PrepareToLoadScene(GameScene.Lobby); // Load lobby scene
                    return;
                }
            }*/
            if(GameUI.inst != null) {
                _localGameState = LocalLobbyState.InIntro;
                GameUI.inst.openingAnimator.Init();
            }
            #endregion
        }
    }



    #region Start Match
    // [Call from server] The main method that will start the match starting process
    public void StartMatch () {
        clientLists = new ClientLists(localPlayers);

        _lobbyStateNetVar.Value = LobbyState.WaitingToStartIntro;

        SendMatchInfo();
        scoreManager.PrepareMatch();
        foreach(KeyValuePair<ulong, ILocalPlayer> kvp in localPlayers) {
            kvp.Value.IsInMatch = false;
        }

        // Cleanup server data if we are running on a headless server
        if(IsHeadlessServerLobby) {
            readyPlayerCount = 0;
            readyPlayers.Clear();
        }
    }
    

    // [Called from client and server]
    public void OnGameSceneLoaded () {
        if(NetAssist.IsServer) {
            if(localPlayers.Count == 1 && NetAssist.IsHost) {
                localPlayers[NetAssist.ClientID].IsInMatch = true;
                StartIntro();
            } else {
                if(NetAssist.IsClient) {
                    readyPlayerCount++;
                    localPlayers[NetAssist.ClientID].IsInMatch = true;
                }
                syncedStartTimer = syncedStartLimit;
            }
        } else {
            //InvokeServerRpc(ReadyForSyncStartServerRPC);
            ReadyForSyncStartServerRPC();
        }
    }
    #endregion

    #region Intro Control
    // [Call from server]
    public void StartIntro () {
        syncedStartTimer = 0f;
        _lobbyStateNetVar.Value = LobbyState.InGame;
    }


    // [Called from client and server]
    public void OnIntroDone () {
        _localGameState = LocalLobbyState.InGame;
        if(!NetAssist.IsHeadlessServer) {
            TerrainTypeAsset terrainTypeAsset = LobbyManager.terrainType;

            DiscordController.SetAsGameStarted(
                localPlayers.Count, matchRulesInfo.timer,
                terrainTypeAsset.name,
                TerrainTypeAsset.ThumbnailKeys[(int)terrainTypeAsset.thumbnail]
            );
        }
        GameUI.inst.StartTimer(matchRulesInfo.timer, 0);
    }
    #endregion

    #region Stop Match
    public void StopMatch () {
        _lobbyStateNetVar.Value = LobbyState.InLobby;

        using(PooledNetworkBuffer stream = PooledNetworkBuffer.Get())
        using(PooledNetworkWriter writer = PooledNetworkWriter.Get(stream)) {
            scoreManager.WriteFinalScores(writer);
            //InvokeClientRpcOnEveryonePerformance(GetFinalScoresClientRPC, stream, "UserData");
            CustomMessagingManager.SendNamedMessage("GetFinalScoresClientRPC", null, stream, MLAPI.Transports.NetworkChannel.ReliableRpc);
        }
    }

    public void GetFinalScoresClientRPC_UnnamedMessage (ulong clientId, Stream stream) {
        if(NetAssist.IsServer)
            return;
        using(PooledNetworkReader reader = PooledNetworkReader.Get(stream)) {
            int count = reader.ReadInt32Packed();
            scoreManager.lastestFinalScores = new FinalScoreData[count];

            for(int i = 0; i < count; i++) {
                scoreManager.lastestFinalScores[i] = new FinalScoreData() {
                    clientId = reader.ReadUInt64Packed(),
                    score = reader.ReadInt32Packed(),
                    rank = reader.ReadByte()
                };
            }
        }
    }

    // [Called from client and server] Only the server can stop the match.
    public void OnTimerDone () {
        if(NetAssist.IsServer)
            StopMatch();
    }
    #endregion


    #region World Network
    private Queue<PlayerEffectEvent> playerEffectNetworkEvents;
    private int oldestRewindIndex = 0;
    private int recentRewindIndex = 0;
    private List<int> recentWorldStatesIndex;
    //private List<PooledBitStream> recentWorldStates;
    private PooledNetworkBuffer recentWorldState;
    private bool doLoadNewWorldState = true;
    private int lastServerFrameIndexWorld = -1;
    private float localReceivingTime;

    public static void EnqueueEventApplyPlayerEffect (byte playerId, byte id, byte level) {
        if(inst == null)
            return;
        inst.playerEffectNetworkEvents.Enqueue(new PlayerEffectEvent() {
            applyOrRevoke = true,
            playerId = playerId,
            id = id,
            level = level
        });
    }

    public static void EnqueueEventRevokePlayerEffect (byte playerId, byte id) {
        if(inst == null)
            return;
        inst.playerEffectNetworkEvents.Enqueue(new PlayerEffectEvent() {
            applyOrRevoke = false,
            playerId = playerId,
            id = id,
            level = 0
        });
    }

    public void SendEverythingToAllClients () {
        // Prepared shared world
        PooledNetworkBuffer sharedWorldStream = PooledNetworkBuffer.Get();
        using(PooledNetworkWriter writer = PooledNetworkWriter.Get(sharedWorldStream)) {
            SimulationManager.inst.SerializeWorldToStream(writer);
        }

        // Send custom
        foreach(KeyValuePair<ulong, ILocalPlayer> kvp in SimulationManager.inst.localPlayers) {
            SendCustomWorldToClient(kvp.Value, sharedWorldStream);
        }
        sharedWorldStream.Dispose();

        if(playerEffectNetworkEvents.Count > 0) {
            using(PooledNetworkBuffer stream = PooledNetworkBuffer.Get())
            using(PooledNetworkWriter writer = PooledNetworkWriter.Get(stream)) {
                writer.WriteByte((byte)playerEffectNetworkEvents.Count);
                while(playerEffectNetworkEvents.Count > 0) {
                    PlayerEffectEvent playerEffectEvent = playerEffectNetworkEvents.Dequeue();
                    writer.WriteByte(playerEffectEvent.playerId);
                    writer.WriteByte(playerEffectEvent.id);
                    writer.WriteBit(playerEffectEvent.applyOrRevoke);
                    if(playerEffectEvent.applyOrRevoke) {
                        writer.WriteByte(playerEffectEvent.level);
                    }
                }
                //InvokeClientRpcOnEveryonePerformance(GetPlayerEffectUpdatesClientRPC, stream, "Terrain");
                CustomMessagingManager.SendNamedMessage("GetPlayerEffectUpdatesClientRPC", null, stream, MLAPI.Transports.NetworkChannel.ReliableRpc);
            }
        }

        // Static entity event
        if(NetworkedEntitySystems.DoSerializeStaticEntities()) {
            using(PooledNetworkBuffer stream = PooledNetworkBuffer.Get())
            using(PooledNetworkWriter writer = PooledNetworkWriter.Get(stream)) {
                NetworkedEntitySystems.SerializeAllStaticEntities(writer);
                //InvokeClientRpcOnEveryonePerformance(GetStaticEntityEventsClientRPC, stream, "Terrain");
                CustomMessagingManager.SendNamedMessage("GetStaticEntityEventsClientRPC", null, stream, MLAPI.Transports.NetworkChannel.ReliableRpc);
            }
        }
    }
    
    public void SendCustomWorldToClient (ILocalPlayer localPlayer, MLAPI.Serialization.NetworkBuffer sharedWorldStream) {
        using(PooledNetworkBuffer stream = PooledNetworkBuffer.Get())
        using(PooledNetworkWriter writer = PooledNetworkWriter.Get(stream)) {

            writer.WriteInt32Packed(SimulationManager.inst.currentFrame);
            writer.WriteInt32Packed(localPlayer.Inputs_RecentInputIndex);
            SimulationManager.inst.SerializeCustomWorldToStream(localPlayer.ClientID, writer);
            stream.PadBuffer();

            sharedWorldStream.Position = 0;
            sharedWorldStream.BitPosition = 0;
            stream.CopyFrom(sharedWorldStream);
            //InvokeClientRpcOnClientPerformance(GetWorldAsClientClientRPC, localPlayer.OwnerClientId, stream, "World");
            CustomMessagingManager.SendNamedMessage("GetWorldAsClientClientRPC", localPlayer.ClientID, stream, MLAPI.Transports.NetworkChannel.UnreliableRpc); // STATE UPDATE
        }
    }


    private void GetWorldAsClientClientRPC_UnnamedMessage (ulong clientId, Stream stream) {
        if(NetAssist.IsHost || LobbyState != LobbyState.InGame || SimulationManager.inst == null)
            return;
        localReceivingTime = Time.time;
        using(PooledNetworkReader reader = PooledNetworkReader.Get(stream)) {

            int serverIndex = reader.ReadInt32Packed();
            if(serverIndex <= lastServerFrameIndexWorld)
                return;
            lastServerFrameIndexWorld = serverIndex;

            int inputIndex = reader.ReadInt32Packed();
            if(inputIndex <= recentRewindIndex)
                return;
            /*if(recentWorldStates.Count == 0)
                oldestRewindIndex = inputIndex;
            else
                return;*/

            doLoadNewWorldState = true;
            recentRewindIndex = inputIndex;
            oldestRewindIndex = inputIndex;

            /*recentWorldStates.Add(PooledBitStream.Get());
            recentWorldStatesIndex.Add(inputIndex);
            recentWorldStates[recentWorldStates.Count - 1].Position = 0;
            recentWorldStates[recentWorldStates.Count - 1].BitPosition = 0;
            recentWorldStates[recentWorldStates.Count - 1].CopyFrom(stream, -1);
            recentWorldStates[recentWorldStates.Count - 1].Position = 0;
            recentWorldStates[recentWorldStates.Count - 1].BitPosition = 0;*/

            recentWorldState.Position = 0;
            recentWorldState.BitPosition = 0;
            stream.CopyTo(recentWorldState, 4096);
            recentWorldState.Position = 0;
            recentWorldState.BitPosition = 0;
        }
    }


    private void GetPlayerEffectUpdatesClientRPC_UnnamedMessage (ulong clientId, Stream stream) {
        if(NetAssist.IsHost)
            return;
        if(LobbyState != LobbyState.InGame || SimulationManager.inst == null)
            return;
        using(PooledNetworkReader reader = PooledNetworkReader.Get(stream)) {
            int eventsCount = reader.ReadByte();
            for(int i = 0; i < eventsCount; i++) {
                byte playerId = (byte)reader.ReadByte();
                byte id = (byte)reader.ReadByte();
                if(reader.ReadBit()) {
                    byte level = (byte)reader.ReadByte();
                    if(SimulationManager.inst.playerSystem.playerEntities.TryGetValue(playerId, out Entity playerEntity)) {
                        playerEntity.Get<PlayerEffects>().SetEffect(playerId, id, level, 1);
                    }
                } else {
                    if(SimulationManager.inst.playerSystem.playerEntities.TryGetValue(playerId, out Entity playerEntity)) {
                        playerEntity.Get<PlayerEffects>().SetEffect(playerId, id, 0, 0);
                    }
                }
            }
        }
    }


    private void GetStaticEntityEventsClientRPC_UnnamedMessage (ulong clientId, Stream stream) {
        if(NetAssist.IsHost)
            return;
        if(LobbyState != LobbyState.InGame || SimulationManager.inst == null)
            return;
        using(PooledNetworkReader reader = PooledNetworkReader.Get(stream)) {
            NetworkedEntitySystems.DeserializeAllStaticEntities(reader);
        }
    }


    public bool DeserializeLastestWorldState () {
        if(doLoadNewWorldState) {
            SimulationManager.inst.DeserializeFrameFromStream(NetAssist.ClientID, oldestRewindIndex, localReceivingTime, recentWorldState/*s*/, /*recentWorldStatesIndex*/recentRewindIndex);
            /*for(int i = 0; i < recentWorldStates.Count; i++) {
                recentWorldStates[i].Dispose();
            }
            recentWorldStates.Clear();
            recentWorldStatesIndex.Clear();*/
            doLoadNewWorldState = false;
            return true;
        } else {
            return false;
        }
    }

    public void CancelWorldState () {
        recentRewindIndex = 0;
        lastServerFrameIndexWorld = 0;
        doLoadNewWorldState = false;
    }
    #endregion

    #region Audio Rpc
    public void PlayEnvironnementSoundOnEveryoneExcept (int clientId, float3 position, EnvironmentSound sound) {
        PlayEnvironnementSoundIgnoreClientRPC(
            x: NetUtils.RangedFloatToUint16Pos(position.x),
            y: NetUtils.RangedFloatToUint16Pos(position.y),
            z: NetUtils.RangedFloatToUint16Pos(position.z),
            sound: (ushort)sound,
            ignoreId: (byte)clientId);
    }

    public void PlayEnvironnementSoundOnEveryone (float3 position, EnvironmentSound sound) {
        PlayEnvironnementSoundClientRPC(
            x: NetUtils.RangedFloatToUint16Pos(position.x),
            y: NetUtils.RangedFloatToUint16Pos(position.y),
            z: NetUtils.RangedFloatToUint16Pos(position.z),
            sound: (ushort)sound);
    }

    [ClientRpc]
    private void PlayEnvironnementSoundClientRPC (ushort x, ushort y, ushort z, ushort sound) {
        if(NetAssist.IsHost || LocalLobbyState == LocalLobbyState.InLobby) {
            return;
        }
        AudioManager.PlayEnvironmentSoundAt(new float3(NetUtils.Uint16ToRangedFloatPos(x), NetUtils.Uint16ToRangedFloatPos(y), NetUtils.Uint16ToRangedFloatPos(z)), 
            (EnvironmentSound)sound);
    }

    [ClientRpc]
    private void PlayEnvironnementSoundIgnoreClientRPC (ushort x, ushort y, ushort z, ushort sound, byte ignoreId) {
        if(NetAssist.ClientID == ignoreId) {
            return;
        }
        if(NetAssist.IsHost || LocalLobbyState == LocalLobbyState.InLobby) {
            return;
        }
        AudioManager.PlayEnvironmentSoundAt(new float3(NetUtils.Uint16ToRangedFloatPos(x), NetUtils.Uint16ToRangedFloatPos(y), NetUtils.Uint16ToRangedFloatPos(z)),
            (EnvironmentSound)sound);
    }
    #endregion

}