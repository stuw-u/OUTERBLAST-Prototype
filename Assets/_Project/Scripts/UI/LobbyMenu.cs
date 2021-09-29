using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MLAPI.Serialization.Pooled;

public class LobbyMenu : MonoBehaviour {

    public Sprite actionHostButton;
    public Sprite actionClientButton;
    public Image actionButtonImage;
    public TextMeshProUGUI actionButtonText;
    public TextMeshProUGUI gameInProgress;

    public GameObject matchSettings;
    public RectTransform readyPlayerCounter;
    public TextMeshProUGUI readyPlayerCounterText;
    public TextMeshProUGUI connectionMessage;
    public RectTransform lobbyLayoutParent;
    public LobbyPlayerItem playerDisplayTemplate;
    private Dictionary<ulong, LobbyPlayerItem> displays = new Dictionary<ulong, LobbyPlayerItem>();
    public static LobbyMenu inst;

    private void Awake () {
        inst = this;
    }

    private void Start () {
        OnGetLobbyType();
    }

    private void Update () {
        if(LobbyManager.inst) {
            gameInProgress.gameObject.SetActive(LobbyManager.LobbyState == LobbyState.InGame);
        }
    }

    public void PrepareDisplays (bool prepareServer) {
        foreach(KeyValuePair<ulong, LobbyPlayerItem> kvp in displays) {
            Destroy(kvp.Value.gameObject);
        }
        displays.Clear();
        if(LobbyManager.inst == null) {
            connectionMessage.alpha = 1f;
        }

        matchSettings.SetActive(prepareServer);
        readyPlayerCounter.gameObject.SetActive(false);

        if(LobbyManager.inst == null) {
            if(prepareServer) {
                actionButtonImage.sprite = actionHostButton;
                actionButtonText.SetText("Start");
            } else {
                actionButtonImage.sprite = actionClientButton;
                actionButtonText.SetText("");
            }
        } else {
            OnGetLobbyType();
        }

        if(LobbyManager.inst != null) {
            foreach(KeyValuePair<ulong, ILocalPlayer> kvp in LobbyManager.inst.localPlayers) {
                LoadDisplay(kvp.Value);
            }
        }
    }

    public void OnGetLobbyType () {
        if(NetAssist.inst == null)
            return;
        if(LobbyManager.inst == null)
            return;
        if(NetAssist.IsHost) {
            actionButtonImage.sprite = actionHostButton;
            actionButtonText.SetText("Start");
        } else {
            actionButtonImage.sprite = actionClientButton;
            if(LobbyManager.inst.IsHeadlessServerLobby) {
                actionButtonText.SetText("Ready");
            } else {
                actionButtonText.SetText("Hurry");
            }
        }
    }

    public void DisplayReadyPlayerCount (int ready, int total) {
        readyPlayerCounter.gameObject.SetActive(true);
        readyPlayerCounterText.SetText($"Ready: {ready} / {total}");
    }

    public void LoadDisplay (ILocalPlayer localPlayer) {
        connectionMessage.alpha = 0f;
        if(!displays.TryGetValue(localPlayer.ClientID, out LobbyPlayerItem value)) {
            LobbyPlayerItem newItem = Instantiate(playerDisplayTemplate, lobbyLayoutParent);
            newItem.gameObject.SetActive(true);
            newItem.SetUserData(localPlayer.UserData.DisplayInfo);
            displays.Add(localPlayer.ClientID, newItem);
        } else {
            value.SetUserData(localPlayer.UserData.DisplayInfo);
        }
    }

    public void RemoveDisplay (ulong clientId) {
        if(displays.TryGetValue(clientId, out LobbyPlayerItem value)) {
            Destroy(value.gameObject);
        }
        displays.Remove(clientId);
    }
}
