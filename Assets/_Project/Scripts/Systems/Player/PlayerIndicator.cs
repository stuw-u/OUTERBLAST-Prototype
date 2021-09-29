using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerIndicator : MonoBehaviour {

    public GameObject bountyDisplay;
    public GameObject stockDisplay;

    public DamageBar damageBar;
    public Image bountyBackground;
    public Color bountyDefaultColor;
    public Color bountyFlashColor;
    public Image stockBackground;
    public Color stockDefaultColor;
    public Color stockFlashColor;
    public float flashSpeed = 10f;

    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI bountyValueText;
    public TextMeshProUGUI stockValueText;
    private Camera mainCamera;
    public ulong playerId;
    public int claimantId = -1;
    public int forceUpdateUI;
    public bool hide;
    public const string pointSuffixBounty = " pts";
    public const string pointSuffixStock = " left";

    private void Awake () {
        if(LobbyManager.inst.matchRulesInfo.scoreMode == ScoreMode.Bounty) {
            SetValue(TaggingSystem.defaultBountyValue);
            bountyDisplay.SetActive(true);
        } else if(LobbyManager.inst.matchRulesInfo.scoreMode == ScoreMode.Stocks) {
            SetValue(LobbyManager.inst.matchRulesInfo.stocks);
            stockDisplay.SetActive(true);
        }
        SetName(null);
    }

    public void SetMainCamera (Camera mainCamera) {
        this.mainCamera = mainCamera;
    }

    public void SetName (UserData userData) {
        if(userData == null) {
            playerNameText.SetText("...");
        } else if(string.IsNullOrEmpty(userData.DisplayInfo.username)) {
            playerNameText.SetText("...");
        } else {
            playerNameText.SetText(userData.DisplayInfo.username);
        }
        forceUpdateUI = 2;
    }

    public void SetHidePosition () {
        transform.position = new Vector3(-100000f, 0f);
    }

    public void SetValue (int value) {
        if(LobbyManager.inst.matchRulesInfo.scoreMode == ScoreMode.Bounty) {
            bountyValueText.SetText(value.ToString() + pointSuffixBounty);
        } else if(LobbyManager.inst.matchRulesInfo.scoreMode == ScoreMode.Stocks) {
            stockValueText.SetText(value.ToString() + pointSuffixStock);
        }
    }

    public void SetClaimant (int claimantId) {
        this.claimantId = claimantId;
    }

    void LateUpdate () {
        if(mainCamera == null || hide) {
            SetHidePosition();
            return;
        }

        if(SimulationManager.inst.localPlayers.TryGetValue(playerId, out ILocalPlayer localPlayer)) {
            Vector3 targetPosition = localPlayer.PlayerGameObject.transform.position + localPlayer.PlayerGameObject.transform.up;
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(targetPosition);
            if(screenPoint.z > 0) {
                transform.position = new Vector3(screenPoint.x, screenPoint.y);
            } else {
                SetHidePosition();
            }
            transform.localScale = Vector3.one * Mathf.Clamp(10f / Vector3.Distance(targetPosition, mainCamera.transform.position), 0.4f, 1.5f);
            damageBar.textFade = Mathf.InverseLerp(0.4f, 0.7f, 10f / Vector3.Distance(targetPosition, mainCamera.transform.position));
        }
        if(forceUpdateUI > 0) {
            playerNameText.SetAllDirty();
            playerNameText.GetComponent<RectTransform>().ForceUpdateRectTransforms();
            forceUpdateUI--;
        }

        if(claimantId == (int)NetAssist.ClientID) {
            bountyBackground.color = Color.Lerp(bountyDefaultColor, bountyFlashColor, Mathf.Repeat(Time.time * flashSpeed, 1.0f));
            stockBackground.color = Color.Lerp(stockDefaultColor, stockFlashColor, Mathf.Repeat(Time.time * flashSpeed, 1.0f));
        } else {
            bountyBackground.color = bountyDefaultColor;
            stockBackground.color = stockDefaultColor;
        }
    }
}
