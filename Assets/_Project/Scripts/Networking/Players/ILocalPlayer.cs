using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILocalPlayer {
    public ulong ClientID { get; }
    public PermissionLevel PermissionLevel { get; set; }
    public PlayerControlType PlayerControlType { get; }
    public UserData UserData { get; set; }
    public MaterialPropertyBlock ColoredMaterialPropertyBlock { get;}
    public bool IsOwnedByInstance { get; }
    public bool IsInMatch { get; set; }
    public float LastTimePressedHurry { get; set; }
    public float Ping { get; }
    public int[] FixedInventoryIDs { get; set; }
    public GameObject GameObject { get; }
    public PlayerGameObject PlayerGameObject { get; }

    public void OnUserDataCallback ();
    public void SendInputsToServer ();
    public void UpdateSelfRecoveryStyleOnServer ();
    public void SetAnimationTrigger (byte itemId);
    public void SetAnimationTriggerPosition (byte itemId, Vector3 position);

    public void Inputs_UpdateOnServer ();
    public void Inputs_UpdateOnClient ();
    public int Inputs_RecentInputIndex { get; }
    public InputSnapshot Inputs_RecentInput { get; }
    public InputSnapshot Inputs_PreviousInput { get; }
}
