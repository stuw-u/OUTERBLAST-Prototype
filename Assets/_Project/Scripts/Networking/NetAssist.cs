using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Mathematics;
using UnityEngine.SceneManagement;

using MLAPI;
using MLAPI.Spawning;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports.PhotonRealtime;
using MLAPI.Transports.UNET;
using Photon.Realtime;

using Blast.Settings;


/// <summary>
/// Serves as a bridge between the simulation and MLAPI's Network manager.
/// Also stores the client's preferences.
/// Provides various utility functions.
/// </summary>
public class NetAssist : MonoBehaviour {

    [Header("Reference")]
    public PhotonAppSettings appSettings;
    public PhotonRealtimeTransport photonRealtimeTransport;
    public UNetTransport uNetTransport;

    [Header("Configuration")]
    [SerializeField] private NetAssistMode _mode;
    public NetAssistMode Mode => _mode;

    [Header("Saved Settings")]
    public UserData selfUserData;
    public Settings settings;

    public string LobbyCode { private set; get; }
    private LobbyManager lobby;


    // Creating singleton
    public static NetAssist inst;
    private void Awake () {


        selfUserData = new UserData(0, new UserDisplayInfo(), new UserSharedSettings());
        if(inst != null) {
            return;
        }

        inst = this;
    }


    private void Start () {

        // Assign connection event if the netAssistant isn't running on a server
        if(!Mode.HasFlag(NetAssistMode.Record)) {
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        // Ensure the server mode is active in case the game was compiled to be a server
        #if UNITY_SERVER
        _mode |= NetAssistMode.Server;
        #endif

        // Caps the framerate at 60 fps for servers to prevent it from simply running as fast as possible.
        if(Mode.HasFlag(NetAssistMode.HeadlessServer)) {
            Application.targetFrameRate = 60;
        }
    }


    #region MLAPI Events
    private void ApprovalCheck (byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback) {
        // Change the approval logic in case verification needs to be done (password check)
        bool approve = true;
        bool createPlayerObject = true;

        //If approve is true, the connection gets added. If it's false. The client gets disconnected
        callback(createPlayerObject, null, approve, Vector3.zero, Quaternion.identity);
    }

    private void OnClientConnect (ulong clientID) {
        if(lobby != null) {
            lobby.OnClientConnects(clientID);
        }
    }

    private void OnClientDisconnected (ulong clientID) {
        if(lobby != null) {
            lobby.OnClientDisconnects(clientID);
        } else {
            ErrorPromptUI.ShowError(0, () => {
                ExitGameAndLobby();
            });
        }
    }
    #endregion


    #region Get Mode/Status
    /// <summary>
    /// Returns true if the instance is purely a server, and not a host.
    /// </summary>
    public static bool IsHeadlessServer {
        get {
            return inst._mode.HasFlag(NetAssistMode.HeadlessServer);
        }
    }

    /// <summary>
    /// Returns true if the instance is running a server, regardless of if it's also running a client.
    /// </summary>
    public static bool IsServer {
        get {
            if(inst._mode.HasFlag(NetAssistMode.Record))
                return true;
            return NetworkManager.Singleton.IsServer;
        }
    }

    /// <summary>
    /// Returns a reliable sync time.
    /// </summary>
    public static float Time {
        get {
            if(inst._mode.HasFlag(NetAssistMode.Record))
                return UnityEngine.Time.time;
            return NetworkManager.Singleton.NetworkTime;
        }
    }

    /// <summary>
    /// Estimates the delay between this client and the server
    /// </summary>
    public static float Ping {
        get {
            var transp = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            return transp.GetCurrentRtt(ClientID);
        }
    }

    /// <summary>
    /// Returns true if this instance is both a client and a server
    /// </summary>
    public static bool IsHost {
        get {
            if(inst._mode.HasFlag(NetAssistMode.Record))
                return true;
            return NetworkManager.Singleton.IsHost;
        }
    }

    /// <summary>
    /// Returns true if this instance is a client, regardless of if it's also running a server.
    /// </summary>
    public static bool IsClient {
        get {
            if(inst._mode.HasFlag(NetAssistMode.Record))
                return true;
            return NetworkManager.Singleton.IsClient;
        }
    }

    /// <summary>
    /// Returns true if this instance is exclusively a client, and not running any server.
    /// </summary>
    public static bool IsClientNotHost {
        get {
            if(inst._mode.HasFlag(NetAssistMode.Record))
                return false;
            return NetworkManager.Singleton.IsClient && !IsHost;
        }
    }

    /// <summary>
    /// Returns the unique identifier of this instance. Always 0 for hosts
    /// </summary>
    public static ulong ClientID {
        get {
            if(inst._mode.HasFlag(NetAssistMode.Record))
                return 0;
            return NetworkManager.Singleton.LocalClientId;
        }
    }

    /// <summary>
    /// The amount of player connected to the lobby, regardless of if they're in the game or not.
    /// </summary>
    public static int PlayerCount {
        get {
            if(inst._mode.HasFlag(NetAssistMode.Record))
                return RecordingManager.inst.actors.Length;
            return NetworkManager.Singleton.ConnectedClients.Count;
        }
    }

    /// <summary>
    /// Returns the LocallPlayer of this specific instance.
    /// </summary>
    public static LocalPlayer LocalPlayer {
        get {
            return NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<LocalPlayer>();
        }
    }

    /// <summary>
    /// Returns the ping of any given client.
    /// </summary>
    public static ulong GetPlayerRTT (ulong clientID) {
        if(inst._mode.HasFlag(NetAssistMode.Record))
            return (ulong)0;
        var transp = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        if(IsHost && clientID == ClientID) {
            return 0;
        }
        return transp.GetCurrentRtt(clientID);
    }
    #endregion


    #region Start Connection/Game

    /// <summary>
    /// Starts a client and connects to the given ip and port. Make sure the correct transport is enabled.
    /// </summary>
    public void StartClient (string ip, ushort port) {

        var transp = ((MLAPI.Transports.UNET.UNetTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport);
        var config = NetworkManager.Singleton.NetworkConfig;

        transp.ConnectAddress = ip;
        transp.ConnectPort = port;
        NetworkManager.Singleton.StartClient();
    }


    /// <summary>
    /// Starts a client and connects to a given lobby
    /// </summary>
    public void StartClientPhoton (string lobbyCode) {

        var transp = photonRealtimeTransport;
        var config = NetworkManager.Singleton.NetworkConfig;

        LobbyCode = lobbyCode;
        transp.RoomName = lobbyCode;
        NetworkManager.Singleton.StartClient();
    }


    /// <summary>
    /// Starts an host on a given ip and port
    /// </summary>
    public void StartHost (string ip, ushort port) {

        var transp = ((UNetTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport);
        var config = NetworkManager.Singleton.NetworkConfig;

        transp.ConnectAddress = ip;
        transp.ConnectPort = port;

        // Create host player
        NetworkManager.Singleton.StartHost(Vector3.zero, Quaternion.identity, true, NetworkSpawnManager.GetPrefabHashFromGenerator("LocalPlayer"));
        DontDestroyOnLoad(NetworkSpawnManager.GetLocalPlayerObject().gameObject);

        // Create lobby object
        lobby = Instantiate(AssetsManager.inst.hostLobbyManagerPrefab);
        DontDestroyOnLoad(lobby);
        lobby.GetComponent<NetworkObject>().Spawn();

        // Register own player
        OnClientConnect(ClientID);
        lobby.RegisterUserData(ClientID, selfUserData);
    }


    /// <summary>
    /// Starts a lobby on photon relay and saves a lobby code for it.
    /// </summary>
    public void StartHostPhoton () {

        var transp = photonRealtimeTransport;
        var config = NetworkManager.Singleton.NetworkConfig;

        // Generate lobby code
        LobbyCode = string.Empty;
        for(int i = 0; i < 5; i++) {
            LobbyCode += NetUtils.lobbyCodeChars[UnityEngine.Random.Range(0, NetUtils.lobbyCodeChars.Length)];
        }
        photonRealtimeTransport.RoomName = LobbyCode;

        // Create host player
        NetworkManager.Singleton.StartHost(Vector3.zero, Quaternion.identity, true, NetworkSpawnManager.GetPrefabHashFromGenerator("LocalPlayer"));
        DontDestroyOnLoad(NetworkSpawnManager.GetLocalPlayerObject().gameObject);

        // Create lobby object
        lobby = Instantiate(AssetsManager.inst.hostLobbyManagerPrefab);
        DontDestroyOnLoad(lobby);
        lobby.GetComponent<NetworkObject>().Spawn();

        // Register own player
        OnClientConnect(ClientID);
        lobby.RegisterUserData(ClientID, selfUserData);
    }


    /// <summary>
    /// Starts a headless server on a given ip and port
    /// </summary>
    public void StartServer (string ip, ushort port) {
        var transp = ((PhotonRealtimeTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport);
        var config = NetworkManager.Singleton.NetworkConfig;

        NetworkManager.Singleton.StartServer();

        // Create lobby object
        lobby = Instantiate(AssetsManager.inst.serverLobbyManagerPrefab);
        DontDestroyOnLoad(lobby);
        lobby.GetComponent<NetworkObject>().Spawn();
    }
    
    /// <summary>
    /// Stop all connections, no matter the instance type.
    /// </summary>
    public void StopConnection () {
        if(IsHost) {
            NetworkManager.Singleton.StopHost();
        } else
        if(IsServer) {
            NetworkManager.Singleton.StopServer();
        } else
        if(IsClient) {
            NetworkManager.Singleton.StopClient();
        }
    }
    #endregion


    #region Exit Game/Lobby
    /// <summary>
    /// Leaves game or lobby regardless of current state. Displays necessary animations.
    /// </summary>
    public static void ExitGameAndLobby () {
        UnityEngine.Time.timeScale = 1f;
        if(SceneManager.GetActiveScene().name == "Main") {
            GameManager.inst.CloseScreen();
            GameUI.inst.openingAnimator.Close();
            inst.StartCoroutine(inst.ExitCoroutine());
        } else if(SceneManager.GetActiveScene().name == "Menu") {
            inst.StopConnection();
            MenuManager.inst.QuitSceneButton(4);
            MenuManager.inst.QuitSceneButton(5);
            MenuManager.inst.SetNextSceneButton(1);
        }
        DiscordController.SetAsInMenu();
    }


    /// <summary>
    /// Starts a coroutine for a delayed exit
    /// </summary>
    private bool exitStarted = false;
    private IEnumerator ExitCoroutine () {
        if(exitStarted) {
            yield break;
        }
        UnityEngine.Time.timeScale = 1f;
        exitStarted = true;
        yield return new WaitForSecondsRealtime(1f);
        inst.StopConnection();
        GameManager.inst.Exit();
        SceneManager.LoadScene("Menu", LoadSceneMode.Single);
        exitStarted = false;

    }
    #endregion
}
