using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Blast.Settings;

public class BindingKeyUI : MonoBehaviour {
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

    private KeyboardMouseBind _bind;
    public KeyboardMouseBind bind {
        get {
            return _bind;
        }
        set {
            Init();
            _bind = value;
            if(value.bindType == KeyboardMouseBindType.Keyboard) {
                key.SetText(_bind.keyBind.ToString());
            } else if(value.bindType == KeyboardMouseBindType.Mouse) {
                key.SetText(buttonIndexToString[_bind.mouseBind]);
            }
        }
    }

    private static readonly List<string> buttonIndexToString = new List<string>() {
        "LMB",
        "RMB",
        "MMB",
        "EX0",
        "EX0",
        "EX1",
        "EX2",
        "EX3",
        "EX4",
        "EX5",
        "EX6",
        "EX7",
    }; 

    public void StartBind () {
        isBinding = true;
        key.SetText("?");
    }

    private void OnGUI () {
        Event e = Event.current;

        if(!isBinding) {
            return;
        }
        if(e.isKey) {
            key.SetText(e.keyCode.ToString());
            _bind = KeyboardMouseBind.KeyBind(e.keyCode);
            isBinding = false;
            return;
        }
        if(e.isMouse) {
            key.SetText(buttonIndexToString[e.button]);
            _bind = KeyboardMouseBind.MouseBind(e.button);
            isBinding = false;
            return;
        }
    }
}
