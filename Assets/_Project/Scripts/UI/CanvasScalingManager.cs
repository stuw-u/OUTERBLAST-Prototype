using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Blast.Settings;

public class CanvasScalingManager : MonoBehaviour {

    public float minScale = 0.5f;
    public float maxScale = 2f;
    public float minScaleScreenHeight = 200f;
    public float maxScaleScreenHeight = 800f;
    public CanvasScaler canvasScaler;

    private void Start () {
        if(NetAssist.IsHeadlessServer) {
            Destroy(this);
        }
    }

    void Update () {
        float screenScaleValue = math.saturate(math.unlerp(minScaleScreenHeight, maxScaleScreenHeight, Screen.height));

        canvasScaler.scaleFactor = math.min(
            SettingsManager.settings.uiScale, 
            math.lerp(
                minScale, 
                maxScale,
                screenScaleValue
        ));
    }
}
