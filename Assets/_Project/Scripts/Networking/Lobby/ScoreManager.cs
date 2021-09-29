using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Blast.ECS;
using Blast.ECS.DefaultComponents;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Mathematics;
using MLAPI;
using MLAPI.NetworkVariable;
using MLAPI.NetworkVariable.Collections;
using MLAPI.Messaging;
using MLAPI.Serialization.Pooled;
using Eldemarkki.VoxelTerrain.World;



public class ScoreManager : NetworkBehaviour {

    public static ScoreManager inst;
    public FinalScoreData[] lastestFinalScores;                     // This lobby's most recent final scores
    private Dictionary<ulong, PlayerLobbyData> playersLobbyData;    // Holds lobby data (ex: in which team as player is), for each in-game player.
    private Dictionary<int, TeamData> teamsData;                    // Holds team data (ex: score data)
    private int topScore = 0;
    private TeamData topTeam = null;

    public const int startScoreBounty = 100;
    public const int startScoreStocks = 0;

    private void Awake () {
        inst = this;
        playersLobbyData = new Dictionary<ulong, PlayerLobbyData>();
        teamsData = new Dictionary<int, TeamData>();
    }


    // Prepares the match...
    public void PrepareMatch () {

        // Clear off data before more
        topScore = 0;
        topTeam = null;
        playersLobbyData.Clear();
        teamsData.Clear();

        // Place everyone in a team
        int teamId = 0;
        foreach(KeyValuePair<ulong, ILocalPlayer> kvp in LobbyManager.inst.localPlayers) {

            // Setup per player team
            teamsData.Add(teamId, new TeamData() {
                score = math.select(startScoreBounty, startScoreStocks, LobbyManager.inst.matchRulesInfo.scoreMode == ScoreMode.Stocks),
                membersId = new List<ulong>()
            });

            // Setup per player data
            playersLobbyData.Add(kvp.Key, new PlayerLobbyData() {
                teamId = teamId
            });
            teamsData[teamId].membersId.Add(kvp.Key);
            ((LocalPlayer)LobbyManager.inst.localPlayers[kvp.Key]).displayScore.Value = (short)teamsData[teamId].score;
            teamId++;
        }
    }


    // Writes scores at the end of the match
    public void WriteFinalScores (PooledNetworkWriter writer) {
        List<KeyValuePair<int,TeamData>> teamsDataOrdered = teamsData.ToList();
        teamsDataOrdered.Sort((a, b) => b.Value.score.CompareTo(a.Value.score));

        writer.WriteInt32Packed(teamsData.Count);
        lastestFinalScores = new FinalScoreData[teamsData.Count];
        byte rank = 0;
        for(int i = 0; i < teamsDataOrdered.Count; i++) {
            lastestFinalScores[i] = new FinalScoreData();
            lastestFinalScores[i].clientId = teamsDataOrdered[i].Value.membersId[0];
            lastestFinalScores[i].score = teamsDataOrdered[i].Value.score;
            writer.WriteUInt64Packed(teamsDataOrdered[i].Value.membersId[0]);
            writer.WriteInt32Packed(teamsDataOrdered[i].Value.score);
            if(i > 0) {
                if(teamsDataOrdered[i - 1].Value.score > teamsDataOrdered[i].Value.score) {
                    rank++;
                }
            }
            writer.WriteByte(rank);
            lastestFinalScores[i].rank = rank;
        }
    }


    // Transfers score upon death. Updates top score and individual score changes
    public static void TransferScoreFromBounty (ulong claimantId, ulong taggedId, int value) {
        if(inst == null)
            return;

        // Verify if claimant is still there, then search for its team
        if(!inst.playersLobbyData.ContainsKey(claimantId)) {
            return;
        }
        TeamData teamData = inst.teamsData[inst.playersLobbyData[claimantId].teamId];
        teamData.score += value;
        ((LocalPlayer)LobbyManager.inst.localPlayers[claimantId]).displayScore.Value = (short)teamData.score;
        TabMenu.RefreshTabMenuData();

        // Rpc all clients in the team to receive the score update (InvokeClientRpc is broken, sends message to host when it shouldn't)
        foreach(ulong clientId in teamData.membersId) {
            //inst.InvokeClientRpcOnClient(inst.UpdateSelfScoreClientRPC, clientId, teamData.score);
            inst.UpdateSelfScoreClientRPC(teamData.score, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = LobbyManager.inst.clientLists.onlyClient[clientId] } });
        }
        
        // Verify if the tagged player is still here. It is not required for the score transaction to be made. The score will me maxed to 0. Point may appear from thin air.
        if(inst.playersLobbyData.ContainsKey(taggedId)) {
            teamData = inst.teamsData[inst.playersLobbyData[taggedId].teamId];
            teamData.score = math.max(0, teamData.score - value);
            ((LocalPlayer)LobbyManager.inst.localPlayers[taggedId]).displayScore.Value = (short)teamData.score;
            TabMenu.RefreshTabMenuData();

            // Rpc all clients in the team to receive the score update (InvokeClientRpc is broken, sends message to host when it shouldn't)
            foreach(ulong clientId in teamData.membersId) {
                //inst.InvokeClientRpcOnClient(inst.UpdateSelfScoreClientRPC, clientId, teamData.score);
                inst.UpdateSelfScoreClientRPC(teamData.score, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = LobbyManager.inst.clientLists.onlyClient[clientId] } });
            }
        }

        UpdateTopScore();
    }


    // Transfers score upon death. Updates top score and individual score changes
    public static void TransferScoreFromStock (ulong claimantId, ulong taggedId) {
        if(inst == null)
            return;

        // Verify if claimant is still there, then search for its team
        if(!inst.playersLobbyData.ContainsKey(claimantId)) {
            return;
        }
        TeamData teamData = inst.teamsData[inst.playersLobbyData[claimantId].teamId];
        teamData.score++;
        ((LocalPlayer)LobbyManager.inst.localPlayers[claimantId]).displayScore.Value = (short)teamData.score;
        TabMenu.RefreshTabMenuData();

        // Rpc all clients in the team to receive the score update (InvokeClientsRpc is broken, sends message to host when it shouldn't)
        foreach(ulong clientId in teamData.membersId) {
            //inst.InvokeClientRpcOnClient(inst.UpdateSelfScoreClientRPC, clientId, teamData.score);
            inst.UpdateSelfScoreClientRPC(teamData.score, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = LobbyManager.inst.clientLists.onlyClient[clientId] } });
        }

        UpdateTopScore();
    }


    // If everyone has a store score of 0 but one, the game must end
    public static void CheckForStockEnd () {
        if(inst == null)
            return;

        int teamsWithStocksAboveZero = 0;
        for(int i = 0; i < inst.teamsData.Count; i++) {
            for(int m = 0; m < inst.teamsData[i].membersId.Count; m++) {
                if(SimulationManager.inst.playerSystem.playerEntities[inst.teamsData[i].membersId[m]].Get<TaggableBounty>().value > 0) {
                    teamsWithStocksAboveZero++;
                    break;
                }
            }
        }
        if(teamsWithStocksAboveZero <= 1) {
            LobbyManager.inst.StopMatch();
        }
    }


    // Checks if top score has changed it
    private static void UpdateTopScore () {
        if(inst == null)
            return;

        // Check if top score changed
        // RPC clients if top score did change
        int newTopScore = -1;
        TeamData newTopTeam = null;
        for(int i = 0; i < inst.teamsData.Count; i++) {
            if(inst.teamsData[i].score > newTopScore) {
                newTopScore = inst.teamsData[i].score;
                newTopTeam = inst.teamsData[i];
            }
        }

        // Make sure top score is first or higher or by a different person
        // - BEFORE ANYTHING ELSE: If there's no top team, do nothing!
        // - The top team isn't the same anymore
        // - The top team has a new top score
        if(newTopTeam != null && (newTopTeam != inst.topTeam || newTopScore != inst.topScore)) {
            inst.topScore = newTopScore;
            inst.topTeam = newTopTeam;

            //inst.InvokeClientRpcOnEveryone(inst.UpdateTopScoreClientRPC, inst.topTeam.membersId[0], inst.topScore);
            inst.UpdateTopScoreClientRPC(inst.topTeam.membersId[0], inst.topScore);
        }
    }


    // Calls every clients to tell them a player's new bounty worth or stock left
    public static void UpdateIndicatorValue (ulong playerId, int value) {
        if(inst == null)
            return;
        if(NetAssist.IsServer)
            //inst.InvokeClientRpcOnEveryone(inst.UpdateIndicatorValueClientRPC, playerId, value);
            inst.UpdateIndicatorValueClientRPC(playerId, value);
    }


    // Calls every clients to tell them a player's new claimant
    public static void UpdateClaimant (ulong playerId, int claimantId) {
        if(inst == null)
            return;
        if(NetAssist.IsServer)
            //inst.InvokeClientRpcOnEveryoneExcept(inst.UpdateClaimantClientRPC, playerId, playerId, claimantId);
            inst.UpdateClaimantClientRPC(playerId, claimantId, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = LobbyManager.inst.clientLists.exceptClient[playerId] } });
    }


    // Removes scores and teams of players that left
    public void RemovePlayer (ulong playerId) {
        if(playersLobbyData.TryGetValue(playerId, out PlayerLobbyData value)) {
            TeamData team = teamsData[value.teamId];
            team.membersId.Remove(playerId);
            if(team.membersId.Count == 0) {
                teamsData.Remove(value.teamId);
            }
            playersLobbyData.Remove(playerId);
        }
    }


    // Gets the score of a player
    public int GetDisplayScore (ulong playerId) {
        if(LobbyManager.inst.localPlayers.TryGetValue(playerId, out ILocalPlayer localPlayer)) {
            return ((LocalPlayer)localPlayer).displayScore.Value;
        } else {
            return 0;
        }
    }


    #region Client RPCs
    [ClientRpc]
    private void UpdateSelfScoreClientRPC (int scoreValue, ClientRpcParams clientRpcParams = default) {
        if(GameUI.inst != null) {
            GameUI.inst.SetSelfScore(scoreValue);
        }
        TabMenu.RefreshTabMenuData();
    }

    [ClientRpc]
    private void UpdateTopScoreClientRPC (ulong topScoreHolder, int scoreValue, ClientRpcParams clientRpcParams = default) {
        if(GameUI.inst == null) {
            return;
        }
        GameUI.inst.SetTopBountyHolder(topScoreHolder, scoreValue);
    }

    [ClientRpc]
    private void UpdateIndicatorValueClientRPC (ulong playerId, int value, ClientRpcParams clientRpcParams = default) {
        if(GameUI.inst != null) {
            if(LobbyManager.inst.matchRulesInfo.scoreMode == ScoreMode.Stocks) {
                if(playerId == NetAssist.ClientID) {
                    StockUI.inst.SetStockCount(value);
                }
            }
            if(playerId != NetAssist.ClientID) {
                PlayerIndicatorDisplay.inst.UpdateIndicatorValue(playerId, value);
            }
            if(!NetAssist.IsServer) {
                PlayerUtils.SetTaggableValue(playerId, value);
            }
            TabMenu.RefreshTabMenuData();
        }
    }

    [ClientRpc]
    private void UpdateClaimantClientRPC (ulong playerId, int claimantId, ClientRpcParams clientRpcParams = default) {
        if(GameUI.inst != null) {
            PlayerIndicatorDisplay.inst.UpdateIndicatorClaimant(playerId, claimantId);
        }
    }
    #endregion
}

public struct PlayerLobbyData {
    public int teamId;
}

public class TeamData {
    public List<ulong> membersId;
    public int score;
}
