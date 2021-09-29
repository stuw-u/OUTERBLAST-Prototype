using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Blast.Settings;

public class PausedMenu : MonoBehaviour {

    public SettingsMenu settings;
    public CanvasGroup pauseUI;
    public float fadeTime = 0.1f;
    private bool _isPaused = false;
    private bool _isSoftPaused = false;
    private float fade = 0f;

    public static bool isPaused {
        get {
            if(inst == null) {
                return false;
            }
            return inst._isPaused;
        }
    }

    public static bool isSoftPause {
        get {
            if(inst == null) {
                return true;
            }
            return inst._isSoftPaused && inst._isPaused;
        }
    }

    public static PausedMenu inst;
    private void Start () {
        inst = this;

        fade = 0f;
        pauseUI.alpha = fade / fadeTime;
        Unpause();
    }

    public void Resume () {
        Unpause();
    }

    public void OpenSettings () {
        settings.gameObject.SetActive(true);
    }

    public void Exit () {
        if(NetAssist.IsServer) {
            LobbyManager.inst.StopMatch();
        } else {
            NetAssist.ExitGameAndLobby();
        }
    }


    public void Pause (bool isSoftPause = false) {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if(!_isSoftPaused) {
            pauseUI.gameObject.SetActive(true);
        }
        _isPaused = true;
        _isSoftPaused = isSoftPause;
    }

    public void Unpause () {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _isPaused = false;
    }

    void Update () {
        if(!_isPaused || !_isSoftPaused) {
            if(fade > 0f && !_isPaused) {
                fade = math.max(fade - Time.deltaTime, 0f);
                if(fade == 0f) {
                    pauseUI.gameObject.SetActive(false);
                }
            }
            if(fade < fadeTime && _isPaused) {
                fade = math.min(fade + Time.deltaTime, fadeTime);
            }
            pauseUI.alpha = fade / fadeTime;
            pauseUI.interactable = fade >= fadeTime;
            pauseUI.blocksRaycasts = fade > 0f;

            if(Input.GetKeyDown(KeyCode.Escape) && !(ChatUI.IsWritting || ChatUI.inst.wasWritting)) {
                if(_isPaused) {
                    Unpause();
                } else {
                    Pause();
                }
            }
        }
    }
}
