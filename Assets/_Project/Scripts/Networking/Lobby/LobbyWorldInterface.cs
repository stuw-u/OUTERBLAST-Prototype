using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyWorldInterface : MonoBehaviour {

    public static LobbyWorldInterface inst;
    private void Awake () {
        inst = this;
    }

    public int PlayerCount {
        get {
            if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) {
                return RecordingManager.inst.actors.Length;
            } else {
                return LobbyManager.inst.localPlayers.Count;
            }
        }
    }

    public LocalLobbyState LocalLobbyState {
        get {
            if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) {
                return LocalLobbyState.InGame;
            } else {
                return LobbyManager.LocalLobbyState;
            }
        }
    }

    public LobbyState LobbyState {
        get {
            if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) {
                return LobbyState.InGame;
            } else {
                return LobbyManager.LobbyState;
            }
        }
    }

    public TerrainTypeAsset terrainType {
        get {
            if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) {
                return RecordingManager.inst.terrainType;
            } else {
                return LobbyManager.terrainType;
            }
        }
    }

    public TerrainStyleAsset terrainStyle {
        get {
            if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) {
                return RecordingManager.inst.terrainStyle;
            } else {
                return LobbyManager.terrainStyle;
            }
        }
    }

    public MatchRulesInfo matchRulesInfo {
        get {
            if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) {
                return RecordingManager.inst.matchRulesInfo;
            } else {
                return LobbyManager.inst.matchRulesInfo;
            }
        }
    }

    public MatchTerrainInfo matchTerrainInfo {
        get {
            if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) {
                return RecordingManager.inst.matchTerrainInfo;
            } else {
                return LobbyManager.inst.matchTerrainInfo;
            }
        }
    }

    public void OnGameSceneLoaded () {
        if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) {

            // Do stuff

        } else {
            LobbyManager.inst.OnGameSceneLoaded();
        }
    }

    public void SendEverythingToAllClients () {
        if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) {

            // Do stuff

        } else {
            LobbyManager.inst.SendEverythingToAllClients();
        }
    }

    public static bool TryGetLocalPlayer (ulong clientId, out ILocalPlayer localPlayer) {
        if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) {
            return RecordingManager.inst.localPlayers.TryGetValue(clientId, out localPlayer);
        } else {
            return LobbyManager.inst.localPlayers.TryGetValue(clientId, out localPlayer);
        }
    }

    public static ILocalPlayer GetLocalPlayer (ulong clientId) {
        if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) {
            return RecordingManager.inst.localPlayers[clientId];
        } else {
            return LobbyManager.inst.localPlayers[clientId];
        }
    }

    public static bool RegisterLocalPlayer (LocalPlayer localPlayer) {
        if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) {
            return true;
        } else {
            return LobbyManager.RegisterLocalPlayer(localPlayer);
        }
    }
}
