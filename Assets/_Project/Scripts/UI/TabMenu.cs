using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;

public class TabMenu : MonoBehaviour {

    public CanvasGroup fader;
    public float fadeSpeed = 10f;
    private float fadeValue;
    public PlayerCard[] playerCards;

    public static TabMenu inst;
    private void Awake () {
        inst = this;

        RefreshTabMenuData();
    }

    public static void RefreshTabMenuData () {
        if(inst == null) {
            return;
        }
        if(inst.fadeValue == 0f) {
            return;
        }
        for(int i = 0; i < 8; i++) {
            inst.playerCards[i].UpdateState(-1);
        }
        if(LobbyManager.inst == null) {
            return;
        }

        int cardIndex = 0;
        foreach(KeyValuePair<ulong, ILocalPlayer> kvp in LobbyManager.inst.localPlayers) {
            inst.playerCards[cardIndex].UpdateState((int)kvp.Key);
            cardIndex++;
            if(cardIndex == 8) {
                break;
            }
        }
    }

    private void Update () {
        if(Input.GetKeyDown(KeyCode.Tab) && fadeValue == 0f) {
            fadeValue = 0.001f;
            RefreshTabMenuData();
        }

        fadeValue = math.saturate(fadeValue + math.select(-1f, 1f, Input.GetKey(KeyCode.Tab)) * Time.deltaTime * fadeSpeed);
        fader.alpha = fadeValue;
    }
}
