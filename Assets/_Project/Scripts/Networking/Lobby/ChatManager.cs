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



public class ChatManager : NetworkBehaviour {

    private static ChatManager inst;
    private void Awake () {
        inst = this;
    }


    /// <summary>
    /// Sends a system message.
    /// </summary>
    public static void SendServerMessageEveryone (string message) {
        if(inst == null)
            return;
        if(NetAssist.IsServer)
            inst.SendChatMessageClientRPC(255, message);
    }


    /// <summary>
    /// Sends an unprotected private message.
    /// </summary>
    public static void SendServerMessagePrivate (string message, ulong target) {
        if(inst == null)
            return;
        if(NetAssist.IsServer)
            inst.SendChatMessageClientRPC(255, message);
    }


    /// <summary>
    /// Sends a ServerRPC with a message or command to the server.
    /// </summary>
    public static void SendChatMessageToServer (string message) {
        if(inst == null)
            return;
        if(NetAssist.IsClient)
            inst.SendChatMessageServerRPC(message);
    }


    #region RPCs
    /// <summary>
    /// Processes a client message, and either sends it back or processes the command.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SendChatMessageServerRPC (string message, ServerRpcParams serverRpcParams = default) {
        if(message.StartsWith("/")) {
            string[] commandArguments = message.Remove(0, 1).Split(' ');
            ExecuteCommand((byte)serverRpcParams.Receive.SenderClientId, commandArguments);
        } else {
            SendChatMessageClientRPC((byte)serverRpcParams.Receive.SenderClientId, message);
        }
    }


    /// <summary>
    /// Processes a server message. Displays it as server message if client id is 255.
    /// </summary>
    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void SendChatMessageClientRPC (byte messageOwner, string message) {
        if(!ChatUI.inst)
            return;
        if(!LobbyManager.inst.localPlayers.ContainsKey(messageOwner) && messageOwner != 255) {
            return;
        }
        if(messageOwner == 255) {
            ChatUI.inst.DisplayNewMessage("[SERVER]", message);
        } else {
            ChatUI.inst.DisplayNewMessage(LobbyManager.inst.localPlayers[messageOwner].UserData.DisplayInfo.username, message);
        }
    }
    #endregion


    #region Commands
    private void ExecuteCommand (byte executer, string[] arguments) {
        switch(arguments[0]) {

            // Teleports a player from a location to another.
            case "tptome":
            if(arguments.Length == 2 && LobbyManager.inst.localPlayers[executer].PermissionLevel == PermissionLevel.Operator) {
                if(StringToClientId(arguments[1], out byte clientId)) {
                    SimulationManager.inst.playerSystem.playerEntities[clientId].Get<Position>().value = SimulationManager.inst.playerSystem.playerEntities[executer].Get<Position>().value;
                } else {
                    SendServerMessagePrivate("Player not found.", executer);
                }
            } else {
                SendServerMessagePrivate("Invalid argument count or permissions.", executer);
            }
            break;


            // Kicks a player out of the lobby
            case "kick":
            if(arguments.Length == 2 && LobbyManager.inst.localPlayers[executer].PermissionLevel == PermissionLevel.Operator) {
                if(StringToClientId(arguments[1], out byte clientId)) {
                    NetworkManager.Singleton.DisconnectClient(clientId);
                    Destroy(LobbyManager.inst.localPlayers[clientId].GameObject);
                } else {
                    SendServerMessagePrivate("Player not found.", executer);
                }
            } else {
                SendServerMessagePrivate("Invalid argument count or permissions.", executer);
            }
            break;


            // Grants command persmissions to a player
            case "op":
            if(arguments.Length == 2 && LobbyManager.inst.localPlayers[executer].PermissionLevel == PermissionLevel.Operator) {
                if(StringToClientId(arguments[1], out byte clientId)) {
                    LobbyManager.inst.localPlayers[clientId].PermissionLevel = PermissionLevel.Operator;
                } else {
                    SendServerMessagePrivate("Player not found.", executer);
                }
            } else {
                SendServerMessagePrivate("Invalid argument count or permissions.", executer);
            }
            break;


            // Removes command permission from a player
            case "deop":
            if(arguments.Length == 2 && LobbyManager.inst.localPlayers[executer].PermissionLevel == PermissionLevel.Operator) {
                if(StringToClientId(arguments[1], out byte clientId)) {
                    LobbyManager.inst.localPlayers[clientId].PermissionLevel = PermissionLevel.Client;
                } else {
                    SendServerMessagePrivate("Player not found.", executer);
                }
            } else {
                SendServerMessagePrivate("Invalid argument count or permissions.", executer);
            }
            break;


            // Changes the display name of an user
            case "rename":
            if(arguments.Length == 3 && LobbyManager.inst.localPlayers[executer].PermissionLevel == PermissionLevel.Operator) {
                if(StringToClientId(arguments[1], out byte clientId)) {
                    LobbyManager.inst.localPlayers[clientId].UserData.Rename(arguments[2].Replace('%', ' '));
                } else {
                    SendServerMessagePrivate("Player not found.", executer);
                }
            } else {
                SendServerMessagePrivate("Invalid argument count or permissions.", executer);
            }
            break;


            // Changes the display color of an user
            case "color":
            if(arguments.Length == 3 && LobbyManager.inst.localPlayers[executer].PermissionLevel == PermissionLevel.Operator) {
                if(StringToClientId(arguments[1], out byte clientId)) {
                    if(ColorUtility.TryParseHtmlString(arguments[2], out Color color)) {
                        color.a = 1f;
                        LobbyManager.inst.localPlayers[clientId].UserData.Recolor(color);
                    } else {
                        SendServerMessagePrivate("Unable to parse color.", executer);
                    }
                } else {
                    SendServerMessagePrivate("Player not found.", executer);
                }
            } else {
                SendServerMessagePrivate("Invalid argument count or permissions.", executer);
            }
            break;


            // Changes executer's own color
            case "colorme":
            if(arguments.Length == 2 && LobbyManager.inst.localPlayers[executer].PermissionLevel == PermissionLevel.Client) {
                if(ColorUtility.TryParseHtmlString(arguments[1], out Color color)) {
                    color.a = 1f;
                    LobbyManager.inst.localPlayers[executer].UserData.Recolor(color);
                } else {
                    SendServerMessagePrivate("Unable to parse color" +
                        ".", executer);
                }
            } else {
                SendServerMessagePrivate("Invalid argument count or permissions.", executer);
            }
            break;


            // Sets executer as spectator
            case "ghostme":
            if(arguments.Length == 1 && LobbyManager.inst.localPlayers[executer].PermissionLevel == PermissionLevel.Operator) {
                PlayerUtils.SetGhostStatus(executer, true);
            } else {
                SendServerMessagePrivate("Invalid argument count or permissions.", executer);
            }
            break;


            // Removes spectator state from executer
            case "unghostme":
            if(arguments.Length == 1 && LobbyManager.inst.localPlayers[executer].PermissionLevel == PermissionLevel.Operator) {
                PlayerUtils.SetGhostStatus(executer, false);
            } else {
                SendServerMessagePrivate("Invalid argument count or permissions.", executer);
            }
            break;


            // Sets player as spectator
            case "ghost":
            if(arguments.Length == 2 && LobbyManager.inst.localPlayers[executer].PermissionLevel == PermissionLevel.Operator) {
                if(StringToClientId(arguments[1], out byte clientId)) {
                    PlayerUtils.SetGhostStatus(clientId, true);
                } else {
                    SendServerMessagePrivate("Player not found.", executer);
                }
            } else {
                SendServerMessagePrivate("Invalid argument count or permissions.", executer);
            }
            break;


            // Removes spectator state from player
            case "unghost":
            if(arguments.Length == 2 && LobbyManager.inst.localPlayers[executer].PermissionLevel == PermissionLevel.Operator) {
                if(StringToClientId(arguments[1], out byte clientId)) {
                    PlayerUtils.SetGhostStatus(clientId, false);
                } else {
                    SendServerMessagePrivate("Player not found.", executer);
                }
            } else {
                SendServerMessagePrivate("Invalid argument count or permissions.", executer);
            }
            break;


            // Displays all available commands.
            case "help":
            SendServerMessagePrivate(
                "Commands:" +
                "\n/help" +
                "\n/ping" +
                "\n/op [playerID]" +
                "\n/deop [playerID]" +
                "\n/ghostme" +
                "\n/ghost [playerID]" +
                "\n/unghostme" +
                "\n/unghost [playerID]" +
                "\n/tptome [playerID]" +
                "\n/kick [playerID]" +
                "\n/rename [playerID] [new username]" +
                "\n--- Use % signs to represent spaces" +
                "\n/color [playerID] [new color]" +
                "\n/colorme [new color]" +
                "\nPress [Tab] to see the list of player IDs"
                , executer);
            break;


            // Helps the user check if it is still online
            case "ping":
            SendServerMessagePrivate("pong", executer);
            break;


            // No command? Return message.
            default:
            SendServerMessagePrivate("Invalid Command. Type /help for the list of commands.", executer);
            break;
        }
    }

    // Tries parsing player id
    public bool StringToClientId (string value, out byte clientId) {
        clientId = 0;
        if(byte.TryParse(value, out clientId)) {
            return LobbyManager.inst.localPlayers.ContainsKey(clientId);
        }
        return false;
    }
    #endregion


    // Manages everything to do with that annoying button
    #region Notify Hurry
    private float lastHurryTime = 0f;
    public static void NotifyHurry () {
        if(Time.unscaledTime - inst.lastHurryTime > 2f) {
            inst.lastHurryTime = Time.unscaledTime;
        } else {
            return;
        }

        AudioManager.PlayEnvironmentSoundAt(float3.zero, EnvironmentSound.Hurry);
        inst.NotifyHurryServerRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyHurryServerRPC (ServerRpcParams serverRpcParams = default) {
        if(Time.unscaledTime - LobbyManager.inst.localPlayers[serverRpcParams.Receive.SenderClientId].LastTimePressedHurry > 2f) {
            LobbyManager.inst.localPlayers[serverRpcParams.Receive.SenderClientId].LastTimePressedHurry = Time.unscaledTime;
        } else {
            return;
        }

        if(LobbyManager.LobbyState != LobbyState.InLobby) {
            SendServerMessageEveryone($"{LobbyManager.inst.localPlayers[serverRpcParams.Receive.SenderClientId].UserData.DisplayInfo.username} is waiting for the game to end...");
        } else {
            NotifyHurryClientRPC((byte)serverRpcParams.Receive.SenderClientId);
        }
    }

    [ClientRpc]
    private void NotifyHurryClientRPC (byte ignoreId) {
        if(NetAssist.ClientID != ignoreId)
            AudioManager.PlayEnvironmentSoundAt(float3.zero, EnvironmentSound.Hurry);
    }
    #endregion
}
