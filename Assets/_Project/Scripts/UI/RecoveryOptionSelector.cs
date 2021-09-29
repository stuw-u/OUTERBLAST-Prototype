using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecoveryOptionSelector : MonoBehaviour {

    public Color defaultColor;
    public Color selectedColor;

    public Image[] optionIcons;


    private void Start () {
        SelectOption((int)NetAssist.inst.selfUserData.SharedSettings.recoveryStyle);
    }


    public void SelectOption (int id) {
        optionIcons[(int)NetAssist.inst.selfUserData.SharedSettings.recoveryStyle].color = defaultColor;
        NetAssist.inst.selfUserData.SharedSettings.recoveryStyle = (RecoveryStyles)id;
        if(LobbyManager.inst != null) {
            LobbyManager.inst.localPlayers[NetAssist.ClientID].UpdateSelfRecoveryStyleOnServer();
        }
        optionIcons[(int)NetAssist.inst.selfUserData.SharedSettings.recoveryStyle].color = selectedColor;
    }

    
}
