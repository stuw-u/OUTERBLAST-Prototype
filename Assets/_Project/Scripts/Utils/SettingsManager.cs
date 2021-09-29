using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using Unity.Mathematics;
using Newtonsoft.Json;

namespace Blast.Settings {

    [DefaultExecutionOrder(1)]
    public class SettingsManager : MonoBehaviour {


        public AudioMixer audioMixer;


        public Settings _settings { private set; get; }
        public static Settings settings {
            get {
                if(inst != null) {
                    return inst._settings;
                } else {
                    return null;
                }
            }
        }
        private string settingsPath;

        public static SettingsManager inst;
        void Awake () {
            inst = this;
        }
        private void Start () {
            if(NetAssist.IsHeadlessServer) {
                return;
            }
            if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) {
                _settings = NetAssist.inst.settings;
                return;
            }

            // Loads settings from a file named "settings.json" in this game's app data folder
            settingsPath = Path.Combine(Application.persistentDataPath, "settings.json");
            if(!File.Exists(settingsPath)) {
                _settings = new Settings();
            } else {
                _settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsPath));
            }
            ApplySettingsInGame();
        }

        public void ApplySaveSettings (Settings settings) {
            this._settings = settings;
            File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
            ApplySettingsInGame();
        }

        private void Update () {
            if(GameManager.inst == null) {
                return;
            }
            if(NetAssist.IsHeadlessServer) {
                return;
            }

            if(settings.showFPS) {
                GameManager.DisplayDebugProperty("FPS ", math.round(1f / (Time.smoothDeltaTime * Time.timeScale)));
            }
        }

        private void ApplySettingsInGame () {
            if(_settings.applyWindowOnStart) {
                ApplyWindowSettings(_settings.resolution, _settings.windowMode);
            }

            #region URP Settings
            switch(settings.shadowQuality) {
                case Quality.Ultra:
                UnityGraphicsBullshit.MainLightCastShadows = true;
                UnityGraphicsBullshit.ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._2048;
                break;
                case Quality.High:
                UnityGraphicsBullshit.MainLightCastShadows = true;
                UnityGraphicsBullshit.ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._1024;
                break;
                case Quality.Medium:
                UnityGraphicsBullshit.MainLightCastShadows = true;
                UnityGraphicsBullshit.ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._512;
                break;
                case Quality.Low:
                UnityGraphicsBullshit.MainLightCastShadows = false;
                break;
            }
            switch(settings.antialiasingQuality) {
                case Quality.Ultra:
                UnityGraphicsBullshit.MsaaQuality = UnityEngine.Rendering.Universal.MsaaQuality._8x;
                break;
                case Quality.High:
                UnityGraphicsBullshit.MsaaQuality = UnityEngine.Rendering.Universal.MsaaQuality._4x;
                break;
                case Quality.Medium:
                UnityGraphicsBullshit.MsaaQuality = UnityEngine.Rendering.Universal.MsaaQuality._2x;
                break;
                case Quality.Low:
                UnityGraphicsBullshit.MsaaQuality = UnityEngine.Rendering.Universal.MsaaQuality.Disabled;
                break;
            }
            UnityGraphicsBullshit.RenderScale = settings.renderScale;

            UnityGraphicsBullshit.SetSSAOSettings(settings.ambientOcclusionQuality);
            #endregion

            #region Audio
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(math.max(0.001f, settings.musicVolume * 0.01f)) * 20);
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(math.max(0.001f, settings.soundEffectVolume * 0.01f)) * 20);
            #endregion
        }

        public void ApplyWindowSettings (int resolutionId, FullScreenMode fullScreenMode) {
            List<Resolution> resolutions = SettingsMenu.GetResolutions();
            if(resolutionId >= 0 && resolutionId < resolutions.Count) {
                Resolution resolution = resolutions[resolutionId];

                Screen.SetResolution(resolution.width, resolution.height, fullScreenMode);
            } else {
                Screen.fullScreenMode = fullScreenMode;
            }
        }
    }
}
