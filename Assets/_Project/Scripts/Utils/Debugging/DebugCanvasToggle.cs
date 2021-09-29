using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugCanvasToggle : MonoBehaviour {

    public Canvas[] canvases;
    private bool isOn = true;

    void Update () {
        if(Input.GetKeyDown(KeyCode.F1)) {
            isOn = !isOn;

            foreach(Canvas canvas in canvases) {
                canvas.gameObject.SetActive(isOn);
            }
        }
    }
}
