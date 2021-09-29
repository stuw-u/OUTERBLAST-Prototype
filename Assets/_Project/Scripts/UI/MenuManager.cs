using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public enum MenuScenes : int {
    OverlayLogo,
    MainMenu,
    PlayMenu,
    SettingsMenu,
    LobbyMenu,
    FinishScreen,
    PlayMode
}

public class MenuManager : MonoBehaviour {

    public Animator cinematicAnimator;
    public Animator[] scenes;
    public TMP_InputField lobbyCode;

    public TMP_InputField[] usernames;

    public TMP_InputField connectAddress;
    public TMP_InputField connectPort;
    public TMP_InputField hostAddress;
    public TMP_InputField hostPort;

    public TMP_Dropdown regionHost;
    public TMP_Dropdown regionJoin;

    public static MenuManager inst;


    private MenuScenes nextScene = 0;

    public static string[] regionToken = {
        "us",
        "usw",
        "cae",
        "sa",
        "eu",
        "ru",
        "rue",
        "asia",
        "in",
        "jp",
        "kr",
        "au",
        "za"
    };

    private void Start () {
        inst = this;
        Time.timeScale = 1f;

        string username = PlayerPrefs.GetString("Username", "Nameless");
        foreach(TMP_InputField inputField in usernames) {
            inputField.text = username;
        }
        int region = PlayerPrefs.GetInt("RegionId", 0);
        regionHost.value = region;
        regionJoin.value = region;

        if(LobbyManager.inst != null) {
            if(LobbyManager.inst.loadMenuInLobby) {
                LobbyManager.inst.loadMenuInLobby = false;
                if(LobbyManager.LobbyState == LobbyState.InGame) {
                    OpenScene(MenuScenes.LobbyMenu);
                } else {
                    OpenScene(MenuScenes.FinishScreen);
                }
                cinematicAnimator.SetInteger("State", 2);
            } else {
                OpenScene(MenuScenes.OverlayLogo);
                cinematicAnimator.SetInteger("State", 2);
                cinematicAnimator.Play("CinematicOpened", 0);
            }
        } else {
            OpenScene(MenuScenes.OverlayLogo);
            cinematicAnimator.SetInteger("State", 2);
            cinematicAnimator.Play("CinematicOpened", 0);
        }

        if(NetAssist.IsHeadlessServer) {
            OnStartServer();
        }
    }

    private bool debugMatchStarted = false;
    private void Update () {
        if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Debug) && !debugMatchStarted) {
            debugMatchStarted = true;
            string[] s = Application.dataPath.Split('/');
            string projectName = s[s.Length - 2];

            if(!projectName.Contains("Clone")) {
                CloseScene(MenuScenes.OverlayLogo);
                OnStartHost();
                OpenScene(MenuScenes.LobbyMenu);
                LobbyManager.inst.StartMatch();
            } else {
                CloseScene(MenuScenes.OverlayLogo);
                OpenScene(MenuScenes.LobbyMenu);
                OnStartClient();
            }
        }
    }
    
    public void PlayMenuSFX (int id) {
        AudioManager.PlayMenuSound((MenuSound)id);
    }

    public void CinematicClose () {
        cinematicAnimator.SetInteger("State", 0);
    }


    #region Scenes

    public void OpenScene (MenuScenes scene) {
        scenes[(int)scene].gameObject.SetActive(true);
    }
    public void CloseScene (MenuScenes scene) {
        scenes[(int)scene].gameObject.SetActive(false);
    }
    public void PrepareExitScene (MenuScenes scene) {
        scenes[(int)scene].SetTrigger("Exit");
    }

    public void OnSceneExitedCallback (MenuScenes sceneExited) {
        CloseScene(sceneExited);

        switch(sceneExited) {
            case MenuScenes.OverlayLogo:
            nextScene = MenuScenes.MainMenu;
            break;

            case MenuScenes.MainMenu:
            break;

            case MenuScenes.PlayMenu:
            break;

            case MenuScenes.SettingsMenu:
            nextScene = MenuScenes.MainMenu;
            break;
        }

        OpenScene(nextScene);
    }
    
    public void QuitSceneButton (int currentScene) {
        PrepareExitScene((MenuScenes)currentScene);

    }

    public void SetNextSceneButton (int nextScene) {
        this.nextScene = (MenuScenes)nextScene;

    }

    public void ExitGame () {
        Application.Quit();
    }

    public void OpenCredits () {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Credits");
    }
    #endregion


    #region Network Buttons
    public void OnStartServer () {
        if(!ushort.TryParse(hostPort.text, out ushort port)) {
            return;
        }

        try {
            NetAssist.inst.StartServer(hostAddress.text, port);
        } catch {
            return;
        }
    }

    public void OnStartHostPhoton () {
        AudioManager.PlayMenuSound(MenuSound.YE);

        NetAssist.inst.appSettings.AppSettings.FixedRegion = regionToken[regionHost.value];
        PlayerPrefs.SetInt("RegionId", regionHost.value);
        PlayerPrefs.SetString("Username", usernames[0].text);

        LobbyMenu.inst.PrepareDisplays(true);
        NetAssist.inst.selfUserData = new UserData(
            clientId: 0, 
            new UserDisplayInfo(usernames[0].text, Color.HSVToRGB(Random.Range(0f, 1f), 1f, 1f)), 
            new UserSharedSettings(NetAssist.inst.settings.inputBufferMode, RecoveryStyles.Delta));

        try {
            NetAssist.inst.StartHostPhoton();
        } catch(System.Exception ex) {
            Debug.Log(ex.Source);
            Debug.Log(ex.StackTrace);
            ErrorPromptUI.ShowError(0, ex.Message);
            return;
        }

        PrepareExitScene(MenuScenes.PlayMenu);
        SetNextSceneButton((int)MenuScenes.LobbyMenu);
    }

    public void OnStartHost () {
        AudioManager.PlayMenuSound(MenuSound.YE);

        if(!ushort.TryParse(hostPort.text, out ushort port)){
            ErrorPromptUI.ShowError(1, (System.Action)null);
            return;
        }
        LobbyMenu.inst.PrepareDisplays(true);
        NetAssist.inst.selfUserData = new UserData(
            clientId: 0, 
            new UserDisplayInfo(usernames[2].text, Color.HSVToRGB(Random.Range(0f, 1f), 1f, 1f)), 
            new UserSharedSettings(NetAssist.inst.settings.inputBufferMode, RecoveryStyles.Delta));
        PlayerPrefs.SetString("Username", usernames[2].text);

        NetAssist.inst.StartHost(hostAddress.text, port);
        /*try {
            NetworkAssistant.inst.StartHost(hostAddress.text, port);
        } catch(System.Exception ex) {
            ErrorPromptUI.ShowError(1, ex.Message);
            return;
        }*/
        PrepareExitScene(MenuScenes.PlayMenu);
        SetNextSceneButton((int)MenuScenes.LobbyMenu);
    }

    public void OnStartClient () {
        AudioManager.PlayMenuSound(MenuSound.YE);

        if(!ushort.TryParse(connectPort.text, out ushort port)) {
            ErrorPromptUI.ShowError(1, (System.Action)null);
            return;
        }
        LobbyMenu.inst.PrepareDisplays(false);

        PlayerPrefs.SetString("Username", usernames[3].text);
        NetAssist.inst.selfUserData = new UserData(
            clientId: 0,
            new UserDisplayInfo(usernames[3].text, Color.HSVToRGB(Random.Range(0f, 1f), 1f, 1f)),
            new UserSharedSettings(NetAssist.inst.settings.inputBufferMode, RecoveryStyles.Delta));

        try {
            NetAssist.inst.StartClient(connectAddress.text, port);
        } catch(System.Exception ex) {;
            ErrorPromptUI.ShowError(1, ex.Message);
            return;
        }
        PrepareExitScene(MenuScenes.PlayMenu);
        SetNextSceneButton((int)MenuScenes.LobbyMenu);
    }

    public void OnStartClientPhoton () {
        AudioManager.PlayMenuSound(MenuSound.YE);

        NetAssist.inst.appSettings.AppSettings.FixedRegion = regionToken[regionJoin.value];
        PlayerPrefs.SetInt("RegionId", regionJoin.value);

        LobbyMenu.inst.PrepareDisplays(false);

        PlayerPrefs.SetString("Username", usernames[1].text);
        NetAssist.inst.selfUserData = new UserData(
            clientId: 0,
            new UserDisplayInfo(usernames[1].text, Color.HSVToRGB(Random.Range(0f, 1f), 1f, 1f)),
            new UserSharedSettings(NetAssist.inst.settings.inputBufferMode, RecoveryStyles.Delta));

        try {
            NetAssist.inst.StartClientPhoton(lobbyCode.text);
        } catch(System.Exception ex) {
            ErrorPromptUI.ShowError(1, ex.Message); // Needs more error messages :(
            Debug.Log(ex.Source);
            Debug.Log(ex.StackTrace);
            return;
        }
        PrepareExitScene(MenuScenes.PlayMenu);
        SetNextSceneButton((int)MenuScenes.LobbyMenu);
    }

    public void ActionButton () {
        if(NetAssist.IsServer) {
            LobbyManager.inst.StartMatch();
        } else {
            if(!LobbyManager.inst) {
                return;
            }
            if(LobbyManager.inst.IsHeadlessServerLobby) {
                AudioManager.PlayMenuSound(MenuSound.TICK);
                LobbyManager.inst.ReadyClient();
            } else {
                ChatManager.NotifyHurry();
            }
            if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Debug)) {
                //LobbyManager.inst.StartGameAsClient();
            }
        }
    }

    public void ExitLobby () {
        AudioManager.PlayMenuSound(MenuSound.NA);

        QuitSceneButton(4);
        SetNextSceneButton(2);
        NetAssist.inst.StopConnection();
    }

    public void FinishScreenToLobby () {
        AudioManager.PlayMenuSound(MenuSound.TICK);

        QuitSceneButton(5);
        SetNextSceneButton(4);
        LobbyMenu.inst.PrepareDisplays(NetAssist.IsServer);
    }

    public void ExitFinishScreenButton () {
        AudioManager.PlayMenuSound(MenuSound.NA);

        QuitSceneButton(5);
        SetNextSceneButton(2);
        LobbyManager.inst.RemoteCloseLobby();
        NetAssist.inst.StopConnection();
    }

    public void RematchButton () {
        ActionButton();
    }
    #endregion

}
