using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Mathematics;
using Newtonsoft.Json;

namespace Blast.Settings {

    public class SettingsMenu : MonoBehaviour {

        [Header("Menu")]
        public Animator animator;
        public float fadeTime = 0.1f;
        public RectTransform[] containers;
        public Button[] containerButton;
        public CanvasGroup content;
        public bool exitByMenuManager;

        [Header("General")]
        public Toggle showFPS;
        public Toggle showRTT;
        public Toggle lowerFPSWhenUnfocused;
        public TMP_Dropdown maxFPS;
        public Toggle reduceCameraShake;
        public TMP_Dropdown inputBuffer;
        [Header("Visual")]
        public TMP_Dropdown windowMode;
        public TMP_Dropdown resolutionsDropdown;
        public Toggle applyWindowOnStart;
        public TMP_Dropdown grassQuality;
        public TMP_Dropdown ambientOcclusionQuality;
        public TMP_Dropdown shadowQuality;
        public TMP_Dropdown antialiasingQuality;
        public NumberedSlider renderScale;
        public Toggle blur;
        public NumberedSlider fieldOfView;
        [Header("Controls")]
        public TMP_Dropdown moveStickDropdown;
        public TMP_Dropdown lookStickDropdown;
        public BindingKeyUI moveForward;
        public BindingKeyUI moveBackward;
        public BindingKeyUI moveLeft;
        public BindingKeyUI moveRight;
        public BindingKeyUI useHeldKM;
        public BindControllerUI useHeldC;
        public BindControllerUI shieldC;
        public BindControllerUI jumpC;
        public BindingKeyUI shieldKM;
        public BindingKeyUI jumpKM;
        public BindingKeyUI switch0KM;
        public BindingKeyUI switch1KM;
        public BindingKeyUI switch2KM;
        public BindingKeyUI consumablesKM;
        public BindControllerUI consumablesC;
        public BindingKeyUI inventoryKM;
        public BindControllerUI inventoryC;
        public NumberedSlider mouseX;
        public NumberedSlider mouseY;
        public BindControllerUI switchLC;
        public BindControllerUI switchRC;
        public BindControllerUI callibrateC;
        public BindControllerUI alignC;
        public TMP_Dropdown gyroModeDropdown;
        public NumberedSlider gyroSensibilityX;
        public NumberedSlider gyroSensibilityY;
        public NumberedSlider stickSensibilityX;
        public NumberedSlider stickSensibilityY;
        public NumberedSlider alignAngle;
        [Header("Language")]
        public TMP_Dropdown language;
        [Header("Interface")]
        public NumberedSlider chatOpacity;
        public NumberedSlider uiScale;
        public TMP_Dropdown crosshairType;
        [Header("Audio")]
        public NumberedSlider sfxVolume;
        public NumberedSlider musicVolume;
        
        private float fadeValue = 0f;
        private int currentIndex = 0;
        private int targetIndex = 0;
        private Settings s;

        private void Start () {
            for(int i = 0; i < containers.Length; i++) {
                int index = i;
                containerButton[i].onClick.AddListener(() => {
                    OpenSection(index);
                });
            }
            OpenSection(0);
            SwitchContainer(0);
            PrepareResolutions();

            s = SettingsManager.settings;
            Load();
        }
        
        public void PlayMenuSFX (int id) {
            AudioManager.PlayMenuSound((MenuSound)id);
        }

        private void PrepareResolutions () {
            resolutionsDropdown.ClearOptions();
            List<Resolution> resolutions = GetResolutions();
            List<string> options = new List<string>();
            foreach(Resolution res in resolutions) {
                options.Add($"{res.width}x{res.height}, {res.refreshRate} hz");
            }
            resolutionsDropdown.AddOptions(options);
        }

        private void OpenSection (int index) {
            targetIndex = index;
            for(int i = 0; i < containers.Length; i++) {
                containerButton[i].interactable = (i != index);
            }
        }

        private void SwitchContainer (int index) {
            currentIndex = index;
            for(int i = 0; i < containers.Length; i++) {
                containers[i].gameObject.SetActive(i == index);
            }
        }

        private void Update () {
            if(targetIndex != currentIndex) {
                fadeValue += Time.deltaTime;

                if(fadeValue > fadeTime) {
                    SwitchContainer(targetIndex);
                }
            } else if(targetIndex == currentIndex && fadeValue != 0f) {
                fadeValue = math.max(0f, fadeValue - Time.deltaTime);
            }
            content.alpha = math.smoothstep(0f, 1f, 1f - (fadeValue / fadeTime));
        }

        public void Save () {

            // General
            s.showFPS = showFPS.isOn;
            s.showRTT = showRTT.isOn;
            s.lowerFPSWhenUnfocused = lowerFPSWhenUnfocused.isOn;
            s.maxFPS = (MaxFPSOption)maxFPS.value;
            s.reduceCameraShake = reduceCameraShake.isOn;
            s.inputBufferMode = (InputBufferMode)inputBuffer.value;

            // Visual
            s.windowMode = (FullScreenMode)windowMode.value;
            s.resolution = resolutionsDropdown.value;
            s.applyWindowOnStart = applyWindowOnStart.isOn;
            s.grassQuality = (Quality)grassQuality.value;
            s.ambientOcclusionQuality = (Quality)ambientOcclusionQuality.value;
            s.shadowQuality = (Quality)shadowQuality.value;
            s.antialiasingQuality = (Quality)antialiasingQuality.value;
            s.fieldOfView = fieldOfView.value;
            s.enableBlur = blur.isOn;
            s.renderScale = renderScale.value;

            // Controls
            s.moveStick = (StickBinds)moveStickDropdown.value;
            s.lookStick = (StickBinds)lookStickDropdown.value;
            s.useHeldC = useHeldC.bind;
            s.shieldC = shieldC.bind;
            s.jumpC = jumpC.bind;
            s.moveForward = moveForward.bind;
            s.moveLeft = moveLeft.bind;
            s.moveRight = moveRight.bind;
            s.moveBackward = moveBackward.bind;
            s.useHeldKM = useHeldKM.bind;
            s.switch0KM = switch0KM.bind;
            s.switch1KM = switch1KM.bind;
            s.switch2KM = switch2KM.bind;
            s.shieldKM = shieldKM.bind;
            s.jumpKM = jumpKM.bind;
            s.consumablesKM = consumablesKM.bind;
            s.consumablesC = consumablesC.bind;
            s.inventoryKM = inventoryKM.bind;
            s.inventoryC = inventoryC.bind;
            s.mouseSensibilityX = mouseX.value;
            s.mouseSensibilityY = mouseY.value;
            s.switchLC = switchLC.bind;
            s.switchRC = switchRC.bind;
            s.clibrateGyro = callibrateC.bind;
            s.alignView = alignC.bind;
            s.gyroMode = (GyroMode)gyroModeDropdown.value;
            s.gyroSensibilityX = gyroSensibilityX.value;
            s.gyroSensibilityY = gyroSensibilityY.value;
            s.stickSensibilityX = stickSensibilityX.value;
            s.stickSensibilityY = stickSensibilityY.value;
            s.gyroAlignmentAngle = alignAngle.value;

            // Language
            s.lang = Settings.indexToLang[language.value];

            // Interface
            s.chatOpacity = chatOpacity.value;
            s.uiScale = uiScale.value;
            s.crosshairType = (CrosshairType)crosshairType.value;

            // Audio
            s.soundEffectVolume = sfxVolume.value;
            s.musicVolume = musicVolume.value;

            SettingsManager.inst.ApplySaveSettings(s);
        }

        public void ApplyWindowsSettings () {
            SettingsManager.inst.ApplyWindowSettings(resolutionsDropdown.value, (FullScreenMode)windowMode.value);
        }

        public void Load () {

            // General
            showFPS.isOn = s.showFPS;
            showRTT.isOn = s.showRTT;
            lowerFPSWhenUnfocused.isOn = s.lowerFPSWhenUnfocused;
            maxFPS.value = (int)s.maxFPS;
            reduceCameraShake.isOn = s.reduceCameraShake;
            inputBuffer.value = (int)s.inputBufferMode;

            // Visual
            windowMode.value = (int)s.windowMode;
            resolutionsDropdown.value = math.select(resolutionsDropdown.options.Count - 1, s.resolution, s.resolution != -1);
            applyWindowOnStart.isOn = s.applyWindowOnStart;
            grassQuality.value = (int)s.grassQuality;
            ambientOcclusionQuality.value = (int)s.ambientOcclusionQuality;
            shadowQuality.value = (int)s.shadowQuality;
            antialiasingQuality.value = (int)s.antialiasingQuality;
            fieldOfView.value = s.fieldOfView;
            fieldOfView.value = s.fieldOfView;
            blur.isOn = s.enableBlur;
            renderScale.value = s.renderScale;

            // Controls
            moveStickDropdown.value = (int)s.moveStick;
            lookStickDropdown.value = (int)s.lookStick;
            useHeldC.bind = s.useHeldC;
            shieldC.bind = s.shieldC;
            jumpC.bind = s.jumpC;
            moveForward.bind = s.moveForward    ?? KeyboardMouseBind.KeyBind(KeyCode.W);
            moveLeft.bind = s.moveLeft          ?? KeyboardMouseBind.KeyBind(KeyCode.A);
            moveRight.bind = s.moveRight        ?? KeyboardMouseBind.KeyBind(KeyCode.D);
            moveBackward.bind = s.moveBackward  ?? KeyboardMouseBind.KeyBind(KeyCode.S);
            useHeldKM.bind = s.useHeldKM        ?? KeyboardMouseBind.KeyBind(0);
            switch0KM.bind = s.switch0KM        ?? KeyboardMouseBind.KeyBind(KeyCode.Q);
            switch1KM.bind = s.switch1KM        ?? KeyboardMouseBind.KeyBind(KeyCode.E);
            switch2KM.bind = s.switch2KM        ?? KeyboardMouseBind.KeyBind(KeyCode.R);
            shieldKM.bind = s.shieldKM          ?? KeyboardMouseBind.MouseBind(1);
            jumpKM.bind = s.jumpKM              ?? KeyboardMouseBind.KeyBind(KeyCode.Space);
            consumablesKM.bind = s.consumablesKM?? KeyboardMouseBind.KeyBind(KeyCode.Alpha1);
            consumablesC.bind = s.consumablesC;
            inventoryKM.bind = s.inventoryKM ?? KeyboardMouseBind.KeyBind(KeyCode.Alpha2);
            inventoryC.bind = s.inventoryC;
            mouseX.value = s.mouseSensibilityX;
            mouseY.value = s.mouseSensibilityY;
            switchLC.bind = s.switchLC;
            switchRC.bind = s.switchRC;
            callibrateC.bind = s.clibrateGyro;
            alignC.bind = s.alignView;
            gyroModeDropdown.value = (int)s.gyroMode;
            gyroSensibilityX.value = s.gyroSensibilityX;
            gyroSensibilityY.value = s.gyroSensibilityY;
            stickSensibilityX.value = s.stickSensibilityX;
            stickSensibilityY.value = s.stickSensibilityY;
            alignAngle.value = s.gyroAlignmentAngle;

            // Language
            language.value = Settings.langToIndex[s.lang];

            // Interface
            chatOpacity.value = s.chatOpacity;
            uiScale.value = s.uiScale;
            crosshairType.value = (int)s.crosshairType;

            // Music
            sfxVolume.value = s.soundEffectVolume;
            musicVolume.value = s.musicVolume;
        }

        public void Close () {
            hasBeenPranked = false;
            if(exitByMenuManager) {
                MenuManager.inst.QuitSceneButton(3);
                MenuManager.inst.SetNextSceneButton(1);
            } else {
                gameObject.SetActive(false);
            }
        }

        public static List<Resolution> GetResolutions () {
            //Filters out all resolutions with low refresh rate:
            Resolution[] resolutions = Screen.resolutions;
            HashSet<ResolutionPair> uniqResolutions = new HashSet<ResolutionPair>();
            Dictionary<ResolutionPair, int> maxRefreshRates = new Dictionary<ResolutionPair, int>();

            for(int i = 0; i < resolutions.Length; i++) {
                //Add resolutions (if they are not already contained)
                ResolutionPair resolution = new ResolutionPair(resolutions[i].width, resolutions[i].height);
                if(!uniqResolutions.Contains(resolution)) {
                    uniqResolutions.Add(resolution);
                    maxRefreshRates.Add(resolution, resolutions[i].refreshRate);
                } else {
                    maxRefreshRates[resolution] = resolutions[i].refreshRate;
                }
            }
            //Build resolution list:
            List<Resolution> uniqResolutionsList = new List<Resolution>(uniqResolutions.Count);
            foreach(ResolutionPair resolution in uniqResolutions) {
                Resolution newResolution = new Resolution();
                newResolution.width = resolution.x;
                newResolution.height = resolution.y;
                if(maxRefreshRates.TryGetValue(resolution, out int refreshRate)) {
                    newResolution.refreshRate = refreshRate;
                }
                uniqResolutionsList.Add(newResolution);
            }
            return uniqResolutionsList;
        }

        bool hasBeenPranked = false;
        public void OnEpicPrank () {
            if(hasBeenPranked)
                return;
            hasBeenPranked = true;
            AudioManager.PlayMenuSound(MenuSound.EpicPrank);
        }
    }

    public struct ResolutionPair {
        public int x { get; private set; }
        public int y { get; private set; }

        public ResolutionPair (int x, int y) {
            this.x = x;
            this.y = y;
        }

        public override bool Equals (object obj) {
            return obj is ResolutionPair pair &&
                   x == pair.x &&
                   y == pair.y;
        }

        public override int GetHashCode () {
            int hashCode = 1502939027;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            return hashCode;
        }

        public static bool operator == (ResolutionPair left, ResolutionPair right) {
            return left.Equals(right);
        }

        public static bool operator != (ResolutionPair left, ResolutionPair right) {
            return !(left == right);
        }
    }
}
