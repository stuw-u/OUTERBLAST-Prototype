using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Blast.Settings;

public class BindControllerUI : MonoBehaviour {
    private TextMeshProUGUI key;
    private bool isBinding = false;
    private bool hasBeenInit = false;

    private void Awake () {
        Init();
    }

    private void Init () {
        if(hasBeenInit) {
            return;
        }
        hasBeenInit = true;
        GetComponent<UnityEngine.UI.Button>().onClick.AddListener(StartBind);
        key = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
    }

    private ControllerButtonBinds _bind;
    public ControllerButtonBinds bind {
        get {
            return _bind;
        }
        set {
            Init();
            _bind = value;
            key.SetText(switchButtonIndexToString[(int)_bind]);
        }
    }
    
    private static readonly List<string> switchButtonIndexToString = new List<string>() {
        "ZR",
        "R",
        "ZL",
        "L",
        "LStick",
        "RStick",
        "X",
        "Y",
        "A",
        "B",
        "Top",
        "Left",
        "Right",
        "Down",
        "-",
        "+"
    };

    public void StartBind () {
        check = 15;
        isBinding = true;
        key.SetText("?");
    }

    int check;
    private void Update () {
        if(!isBinding) {
            return;
        }
        if(InputListener.GetControllerBind(out ControllerButtonBinds binds)) {
            key.SetText(switchButtonIndexToString[(int)_bind]);
            _bind = binds;
            if(check == 0) {
                isBinding = false;
            } else {
                check--;
            }
            return;
        }
    }
}
