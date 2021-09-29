using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIndicatorDisplay : MonoBehaviour {

    public Transform parent;
    public PlayerIndicator prefab;
    public Dictionary<ulong, PlayerIndicator> playerIndicators;
    public static PlayerIndicatorDisplay inst;
    private Camera mainCamera;

    void Awake () {
        inst = this;
        playerIndicators = new Dictionary<ulong, PlayerIndicator>();

        UpdateAllIndicators();
    }

    public static void SetMainCamera (Camera mainCamera) {
        inst.mainCamera = mainCamera;
        foreach(KeyValuePair<ulong, PlayerIndicator> kvp in inst.playerIndicators) {
            kvp.Value.SetMainCamera(mainCamera);
        }
    }

    public void AddIndicator (ulong playerId) {
        if(NetAssist.IsClient && NetAssist.ClientID == playerId) {
            return;
        }

        PlayerIndicator indicator = Instantiate(prefab, parent);
        indicator.transform.SetAsFirstSibling();
        indicator.gameObject.SetActive(true);
        indicator.SetMainCamera(mainCamera);
        indicator.playerId = playerId;
        indicator.SetHidePosition();
        playerIndicators.Add(playerId, indicator);
        UpdateIndicator(playerId);
    }

    public void RemoveIndicator (ulong playerId) {
        if(!playerIndicators.ContainsKey(playerId)) {
            return;
        }
        if(playerIndicators[playerId] == null) {
            return;
        }

        Destroy(playerIndicators[playerId].gameObject);
        playerIndicators.Remove(playerId);
    }

    public void UpdateIndicator (ulong playerId) {
        if(SimulationManager.TryGetLocalPlayer(playerId, out ILocalPlayer localPlayer)) {
            if(!playerIndicators.ContainsKey(playerId)) {
                return;
            }
            playerIndicators[playerId].SetName(localPlayer.UserData);
        }
    }

    public void SetHideIndicator (ulong playerId, bool hide) {
        if(SimulationManager.TryGetLocalPlayer(playerId, out ILocalPlayer localPlayer)) {
            if(!playerIndicators.ContainsKey(playerId)) {
                return;
            }
            playerIndicators[playerId].hide = hide;
        }
    }

    public void UpdateIndicatorValue (ulong playerId, int value) {
        if(SimulationManager.TryGetLocalPlayer(playerId, out ILocalPlayer localPlayer)) {
            if(!playerIndicators.ContainsKey(playerId)) {
                return;
            }
            playerIndicators[playerId].SetValue(value);
        }
    }

    public void UpdateDamageValue (ulong playerId, float value) {
        if(SimulationManager.TryGetLocalPlayer(playerId, out ILocalPlayer localPlayer)) {
            if(!playerIndicators.ContainsKey(playerId)) {
                return;
            }
            playerIndicators[playerId].damageBar.SetValue(value / 100f);
        }
    }

    public void UpdateIndicatorClaimant (ulong playerId, int claimantId) {
        if(SimulationManager.TryGetLocalPlayer(playerId, out ILocalPlayer localPlayer)) {
            if(!playerIndicators.ContainsKey(playerId)) {
                return;
            }
            playerIndicators[playerId].SetClaimant(claimantId);
        }
    }

    public void UpdateAllIndicators () {
        foreach(KeyValuePair<ulong, ILocalPlayer> kvp in SimulationManager.inst.localPlayers) {
            if(!playerIndicators.ContainsKey(kvp.Key)) {
                AddIndicator(kvp.Key);
            }
        }
    }
}
