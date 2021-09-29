using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainLightManager : MonoBehaviour {

    new private Light light;
    private static MainLightManager inst;

    private void Awake () {
        light = GetComponent<Light>();
        inst = this;
    }
    private float flashTime = 0f;

    public static void FlashLight () {
        inst.flashTime = 1f;
    }

    private void Update () {
        if(LobbyWorldInterface.inst.terrainStyle == null) {
            return;
        }

        flashTime = Mathf.Clamp01(flashTime - Time.deltaTime * 4f);
        light.intensity = Mathf.Lerp(LobbyWorldInterface.inst.terrainStyle.mainLightIntensity, LobbyWorldInterface.inst.terrainStyle.mainLightIntensity + 0.5f, 1f - Mathf.Abs(1f-(2f*flashTime)));
    }
}
