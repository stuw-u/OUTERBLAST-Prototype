using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairManager : MonoBehaviour {

    public Image image;
    public Sprite[] crosshairs;

    private void Start () {
        if(NetAssist.IsHeadlessServer) {
            Destroy(this);
        }
    }

    void Update () {
        image.sprite = crosshairs[(int)Blast.Settings.SettingsManager.settings.crosshairType];
    }
}
