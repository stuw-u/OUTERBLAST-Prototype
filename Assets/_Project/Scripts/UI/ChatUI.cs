using System.Text;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ChatUI : MonoBehaviour {

    [Header("Parameters")]
    public Color pingColor = Color.red;
    public Color idleColor = Color.white;
    public float defaultChatHeight = 270f;
    public float openCloseSpeed = 4f;

    [Header("References")]
    public CanvasGroup allChatCanvasGroup;
    public RectTransform chatRect;
    public CanvasGroup chatRectCanvasGroup;
    public RectTransform chatContent;
    public TextMeshProUGUI chatTemplate;
    public TMP_InputField inputField;
    public TextMeshProUGUI pingText;
    public RectTransform pingBG;

    // Privates
    private StringBuilder sb;
    private float openCloseValue = 0f;
    private bool shouldBeOpened = false;
    private float onOffValue = 0f;
    private int pingCounter = 0;
    public bool wasWritting;
    
    public static bool IsWritting {
        get {
            if(inst == null) {
                return false;
            }
            return inst.inputField.isFocused;
        }
    }

    public static ChatUI inst;
    private void Awake () {
        inst = this;

        sb = new StringBuilder();
        chatRect.sizeDelta = new Vector2(chatRect.sizeDelta.x, math.smoothstep(0f, 1f, openCloseValue) * defaultChatHeight);
        allChatCanvasGroup.alpha = 0f;
        DisplayPingCount();
    }
    
    private void Update () {
        bool shouldChatExist = LobbyManager.inst != null;
        if(shouldChatExist && (LobbyManager.LobbyState == LobbyState.WaitingToStartIntro)) {
            shouldChatExist = false;
        }
        allChatCanvasGroup.interactable = shouldChatExist;

        if(!shouldChatExist) {
            shouldBeOpened = false;
        }
        if(shouldChatExist && onOffValue < 1f) {
            onOffValue = math.saturate(onOffValue + Time.deltaTime * openCloseSpeed);
            allChatCanvasGroup.alpha = onOffValue;

        } else if(!shouldChatExist && onOffValue > 0f) {
            onOffValue = math.saturate(onOffValue - Time.deltaTime * openCloseSpeed);
            allChatCanvasGroup.alpha = onOffValue;
        }


        if(shouldBeOpened && openCloseValue < 1f) {
            openCloseValue = math.saturate(openCloseValue + Time.deltaTime * openCloseSpeed);
            chatRect.sizeDelta = new Vector2(chatRect.sizeDelta.x, math.smoothstep(0f, 1f, openCloseValue) * defaultChatHeight);
            chatRectCanvasGroup.alpha = openCloseValue;

        } else if(!shouldBeOpened && openCloseValue > 0f) {
            openCloseValue = math.saturate(openCloseValue - Time.deltaTime * openCloseSpeed);
            chatRect.sizeDelta = new Vector2(chatRect.sizeDelta.x, math.smoothstep(0f, 1f, openCloseValue) * defaultChatHeight);
            chatRectCanvasGroup.alpha = openCloseValue;
        }

        if(shouldChatExist && Input.GetKeyDown(KeyCode.Return) && !IsWritting && !wasWritting) {
            OpenFocusChat();
        }

        if(IsWritting && Input.GetKeyDown(KeyCode.Tab)) {
            sb.Clear();
            sb.Append("\n");
            foreach(KeyValuePair<ulong, ILocalPlayer> kvp in LobbyManager.inst.localPlayers) {
                sb.Append($"{kvp.Value.UserData.DisplayInfo.username} : {kvp.Key}");
                sb.Append("\n");
            }
            DisplayNewMessage("Client List:", sb.ToString());
        }

        if(wasWritting && Input.GetKeyDown(KeyCode.Escape)) {
            if(LobbyManager.LocalLobbyState != LocalLobbyState.InLobby) {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            CloseUnfocusChat();
        }
    }

    private void LateUpdate () {
        wasWritting = IsWritting;
    }

    public void ToggleChat () {
        shouldBeOpened = !shouldBeOpened;
        if(shouldBeOpened) {
            pingCounter = 0;
            DisplayPingCount();
        }
    }

    public void OpenFocusChat () {
        if(!shouldBeOpened) {
            pingCounter = 0;
            DisplayPingCount();
        }
        shouldBeOpened = true;
        inputField.Select();
        inputField.MoveTextStart(true);
    }

    public void CloseUnfocusChat () {
        if(!shouldBeOpened) {
            pingCounter = 0;
            DisplayPingCount();
        }
        shouldBeOpened = false;
        inputField.DeactivateInputField();
        inputField.interactable = false;
        inputField.interactable = true;
    }

    public void SendNewMessage (string message) {
        inputField.DeactivateInputField();
        inputField.interactable = false;
        inputField.interactable = true;

        if(string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message)) {
            return;
        }

        message = message.Replace("\\n", "\n");
        message = message.Replace('\n', ' ');
        inputField.text = string.Empty;

        ChatManager.SendChatMessageToServer(message);
    }

    public void DisplayNewMessage (string username, string message) {
        if(!shouldBeOpened) {
            pingCounter++;
            DisplayPingCount();
        }

        TextMeshProUGUI newText = Instantiate(chatTemplate, chatContent);
        newText.gameObject.SetActive(true);
        sb.Clear();
        sb.Append("<color=#FFD700>");
        sb.Append(username);
        sb.Append("</color>   ");
        sb.Append(message);
        newText.SetText(sb);
    }

    public void DisplayPingCount () {
        pingBG.gameObject.SetActive(pingCounter > 0);
        if(pingCounter > 0 && pingCounter < 100) {
            pingText.text = pingCounter.ToString();
        } else if(pingCounter > 99) {
            pingText.text = "99+";
        }
    }
}
