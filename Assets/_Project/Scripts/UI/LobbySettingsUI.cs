using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ScoreMode {
    Bounty,
    Stocks
}

public class LobbySettingsUI : MonoBehaviour {

    public CanvasGroup blockGroup;
    public float animationSpeed;
    public BoxPopupAnimation boxPopupAnimation;
    private float value;
    private bool targetState;

    public Sprite defaultMapIcon;

    public NumberedSlider timer;
    public TMPro.TMP_Dropdown scoreMode;
    public TMPro.TMP_InputField stocks;
    public Toggle damageMode;
    public Toggle randomizeMap;
    public TMPro.TMP_Dropdown terrainType;
    public Image mapIcon;
    public Toggle isMapSizeAuto;
    public NumberedSlider mapSize;

    private void Start () {
        terrainType.ClearOptions();
        List<string> options = new List<string>();
        foreach(TerrainTypeAsset typeAsset in AssetsManager.inst.terrainTypeCollection.terrainTypes) {
            options.Add(typeAsset.name);
        }
        terrainType.AddOptions(options);
        mapIcon.sprite = defaultMapIcon;
    }

    public void Open () {
        targetState = true;

        timer.value = LobbyManager.inst.matchRulesInfo.timer;
        scoreMode.value = (int)LobbyManager.inst.matchRulesInfo.scoreMode;
        stocks.text = LobbyManager.inst.matchRulesInfo.stocks.ToString();
        damageMode.isOn = LobbyManager.inst.matchRulesInfo.damageMode;
        terrainType.value = LobbyManager.inst.matchRulesInfo.selectedMap;
        mapSize.value = LobbyManager.inst.matchRulesInfo.mapSize * 100f;
        isMapSizeAuto.isOn = LobbyManager.inst.matchRulesInfo.automaticMapSize;
        OnSelectMap();
        randomizeMap.isOn = LobbyManager.inst.matchRulesInfo.randomizeMap;
    }

    public void CloseAndApply () {
        targetState = false;

        LobbyManager.inst.matchRulesInfo.timer = (int)timer.value;
        LobbyManager.inst.matchRulesInfo.scoreMode = (ScoreMode)scoreMode.value;
        if(int.TryParse(stocks.text, out int result)) {
            LobbyManager.inst.matchRulesInfo.stocks = Mathf.Clamp(result, 1, 999);
        } else {
            LobbyManager.inst.matchRulesInfo.stocks = 5;
        }
        LobbyManager.inst.matchRulesInfo.damageMode = damageMode.isOn;
        LobbyManager.inst.matchRulesInfo.randomizeMap = randomizeMap.isOn;
        LobbyManager.inst.matchRulesInfo.selectedMap = terrainType.value;
        LobbyManager.inst.matchRulesInfo.mapSize = mapSize.value * 0.01f;
        LobbyManager.inst.matchRulesInfo.automaticMapSize = isMapSizeAuto.isOn;
    }

    void Update () {
        boxPopupAnimation.value = value;
        if(targetState) {
            value = Mathf.Clamp01(value + animationSpeed * Time.deltaTime);
        } else {
            value = Mathf.Clamp01(value - animationSpeed * Time.deltaTime);
        }
        blockGroup.blocksRaycasts = value != 0f;
    }

    public void OnSelectMap () {
        randomizeMap.isOn = false;
        if(AssetsManager.inst.terrainTypeCollection.terrainTypes[terrainType.value].selectionIcon == null) {
            mapIcon.sprite = defaultMapIcon;
        } else {
            mapIcon.sprite = AssetsManager.inst.terrainTypeCollection.terrainTypes[terrainType.value].selectionIcon;
        }
    }
}
