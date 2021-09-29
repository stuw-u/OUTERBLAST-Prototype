using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyCodeButton : MonoBehaviour {
    public TextMeshProUGUI lobbyCodeText;
    public Image visibilityImage;
    public Sprite visible;
    public Sprite hidden;

    bool isVisible = false;

    private void OnEnable () {
        SetCode();
    }
    
    public void ToggleVisibility () {
        isVisible = !isVisible;
        visibilityImage.sprite = isVisible ? hidden : visible;

        SetCode();
    }

    private void SetCode () {
        if(!isVisible) {
            lobbyCodeText.text = new string('*', NetAssist.inst.LobbyCode.Length);
        } else {
            lobbyCodeText.text = NetAssist.inst.LobbyCode;
        }
    }
    
    public void OnCopy () {
        ClipboardHelper.clipBoard = NetAssist.inst.LobbyCode;
    }
}
