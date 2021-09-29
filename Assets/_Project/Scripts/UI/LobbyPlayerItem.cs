using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyPlayerItem : MonoBehaviour {
    public TextMeshProUGUI username;
    public Image playerColorDisplay;

    public void SetUserData (UserDisplayInfo displayInfo) {
        username.SetText(displayInfo.username);
        playerColorDisplay.color = displayInfo.color;
    }
}
