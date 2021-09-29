using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ChargingUIType {
    Main,
    Recovery,
    Defence
}

public class ChargingUI : MonoBehaviour {

    public ChargingUIType type;
    public Image chargingImage1;
    public Image chargingImage2;
    public CanvasGroup canvasGroup;
    public float apparitionSpeed = 8f;
    private float apparitionValue = 0f;
    private bool apparitionState = false;

    private static ChargingUI instMain;
    private static ChargingUI instRecovery;
    private static ChargingUI instShield;
    private void Awake () {
        switch(type) {
            case ChargingUIType.Main:
            instMain = this;
            break;
            case ChargingUIType.Recovery:
            instRecovery = this;
            break;
            case ChargingUIType.Defence:
            instShield = this;
            break;
        }
    }

    public static void SetIndicatorState (ChargingUIType type, bool state, float value) {
        ChargingUI inst = null;
        switch(type) {
            case ChargingUIType.Main:
            inst = instMain;
            break;
            case ChargingUIType.Recovery:
            inst = instRecovery;
            break;
            case ChargingUIType.Defence:
            inst = instShield;
            break;
        }
        if(inst == null)
            return;
        inst.apparitionState = state;
        inst.chargingImage1.fillAmount = value;
        if(inst.chargingImage2 != null)
            inst.chargingImage2.fillAmount = value + 0.02f;
    }

    void Update () {
        if(apparitionState) {
            apparitionValue = Mathf.Clamp01(apparitionValue + (Time.deltaTime * apparitionSpeed));
        } else {
            apparitionValue = Mathf.Clamp01(apparitionValue - (Time.deltaTime * apparitionSpeed));
        }
        canvasGroup.alpha = apparitionValue;
    }
}
