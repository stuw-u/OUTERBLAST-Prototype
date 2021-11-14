using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.NetworkVariable;
using MLAPI.NetworkVariable.Collections;
using MLAPI.Messaging;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using Blast.Settings;


/// <summary>
/// The user displaying information that needs to be shared with
/// all players in the lobby
/// </summary>
[Serializable]
public class UserDisplayInfo : INetworkSerializable {
    public string username = "...";
    public Color color = Color.white;

    public UserDisplayInfo () { }

    public UserDisplayInfo (string username, Color color) {
        this.username = username;
        this.color = color;
    }

    public void NetworkSerialize (NetworkSerializer serializer) {
        serializer.Serialize(ref username);
        serializer.Serialize(ref color);
    }
}


/// <summary>
/// The user-specific settings and options that need to be shared with
/// all players in the lobby
/// </summary>
[Serializable]
public class UserSharedSettings : INetworkSerializable {
    public InputBufferMode inputBufferMode;
    public RecoveryStyles recoveryStyle;

    public UserSharedSettings () { }

    public UserSharedSettings (InputBufferMode inputBufferMode, RecoveryStyles reveryStyle) {
        this.inputBufferMode = inputBufferMode;
        this.recoveryStyle = reveryStyle;
    }

    public void NetworkSerialize (NetworkSerializer serializer) {
        serializer.Serialize(ref inputBufferMode);
        serializer.Serialize(ref recoveryStyle);
    }
}


/// <summary>
/// The collection of all user-specific data that need to be shared with all
/// players in in the lobby
/// </summary>
[Serializable]
public class UserData : INetworkSerializable {

    // User Data
    public ulong ClientID => _clientID;
    public UserDisplayInfo DisplayInfo => _userDisplayInfo;
    public UserSharedSettings SharedSettings => _userSharedSettings;

    private ulong _clientID;
    private UserDisplayInfo _userDisplayInfo = new UserDisplayInfo("...", Color.white);
    private UserSharedSettings _userSharedSettings = new UserSharedSettings();

    public UserData () { }

    public UserData (ulong clientId, UserDisplayInfo userDisplayInfo) {
        _clientID = clientId;
        _userDisplayInfo = userDisplayInfo;
    }

    public UserData (ulong clientId, UserDisplayInfo userDisplayInfo, UserSharedSettings userSharedSettings) {
        _clientID = clientId;
        _userDisplayInfo = userDisplayInfo;
        _userSharedSettings = userSharedSettings;
    }

    public void NetworkSerialize (NetworkSerializer serializer) {
        if(_userDisplayInfo == null)
            _userDisplayInfo = new UserDisplayInfo();

        serializer.Serialize(ref _clientID);
        _userDisplayInfo.NetworkSerialize(serializer);
    }

    public void Rename (string username) => _userDisplayInfo.username = username;
    public void Recolor (Color color) => _userDisplayInfo.color = color;
}



public enum RecoveryStyles {
    Delta,
    ChargeJet,
    ImpulseJet,
    Grappler
}