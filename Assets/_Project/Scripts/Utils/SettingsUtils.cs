using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Mathematics;
using Newtonsoft.Json;

namespace Blast.Settings {

    [System.Serializable]
    public class Settings {

        // General
        public bool showFPS = false;
        public bool showRTT = false;
        public bool lowerFPSWhenUnfocused = true;
        public MaxFPSOption maxFPS = MaxFPSOption.Max;
        public bool reduceCameraShake = false;
        public InputBufferMode inputBufferMode;

        // Visual
        public Quality grassQuality = Quality.Low;
        public Quality ambientOcclusionQuality = Quality.High;
        public Quality shadowQuality = Quality.Ultra;
        public Quality antialiasingQuality = Quality.Medium;
        public bool applyWindowOnStart = true;
        public bool enableBlur = true;
        public float renderScale = 1f;
        public int resolution = -1;
        public FullScreenMode windowMode = FullScreenMode.FullScreenWindow;
        public float fieldOfView = 90f;

        // Controls
        public StickBinds moveStick = StickBinds.Left;
        public KeyboardMouseBind moveForward = KeyboardMouseBind.KeyBind(KeyCode.W);
        public KeyboardMouseBind moveLeft = KeyboardMouseBind.KeyBind(KeyCode.A);
        public KeyboardMouseBind moveRight = KeyboardMouseBind.KeyBind(KeyCode.D);
        public KeyboardMouseBind moveBackward = KeyboardMouseBind.KeyBind(KeyCode.S);
        public StickBinds lookStick = StickBinds.Right;
        public KeyboardMouseBind useHeldKM = KeyboardMouseBind.MouseBind(0);
        public ControllerButtonBinds useHeldC;
        public KeyboardMouseBind switch0KM = KeyboardMouseBind.KeyBind(KeyCode.Q);
        public KeyboardMouseBind switch1KM = KeyboardMouseBind.KeyBind(KeyCode.E);
        public KeyboardMouseBind switch2KM = KeyboardMouseBind.KeyBind(KeyCode.R);
        public ControllerButtonBinds switchLC = ControllerButtonBinds.L;
        public ControllerButtonBinds switchRC = ControllerButtonBinds.R;
        public KeyboardMouseBind shieldKM = KeyboardMouseBind.MouseBind(1);
        public ControllerButtonBinds shieldC = ControllerButtonBinds.L;
        public KeyboardMouseBind jumpKM = KeyboardMouseBind.KeyBind(KeyCode.Space);
        public ControllerButtonBinds jumpC = ControllerButtonBinds.MainDown;
        public ControllerButtonBinds jumpC1 = ControllerButtonBinds.ZL;
        public KeyboardMouseBind consumablesKM = KeyboardMouseBind.KeyBind(KeyCode.Alpha1);
        public ControllerButtonBinds consumablesC = ControllerButtonBinds.R;
        public KeyboardMouseBind inventoryKM = KeyboardMouseBind.KeyBind(KeyCode.Alpha2);
        public ControllerButtonBinds inventoryC = ControllerButtonBinds.MainLeft;
        public ControllerButtonBinds clibrateGyro = ControllerButtonBinds.ArrowTop;
        public ControllerButtonBinds alignView = ControllerButtonBinds.ArrowDown;
        public GyroMode gyroMode = GyroMode.TurnStickX;
        public float gyroSensibilityX = 3f;
        public float gyroSensibilityY = 3f;
        public float gyroAlignmentAngle = -20f;
        public float stickSensibilityX = 420f;
        public float stickSensibilityY = 420f;
        public float mouseSensibilityX = 1.6f;
        public float mouseSensibilityY = 1.6f;

        // Language
        public string lang = "en";

        // UI
        public float chatOpacity = 100f;
        public float uiScale = 1f;
        public CrosshairType crosshairType;

        // Audio
        public float soundEffectVolume = 30f;
        public float musicVolume = 70f;


        public static readonly List<string> indexToLang = new List<string>() {
            "en",
            "fr"
        };
        public static readonly Dictionary<string, int> langToIndex = new Dictionary<string, int>() {
            {"en", 0},
            {"fr", 1}
        };
        public static readonly int[] BindToMask = new int[] {
            JSL.ButtonMaskZR,
            JSL.ButtonMaskR,
            JSL.ButtonMaskZR,
            JSL.ButtonMaskL,
            JSL.ButtonMaskLClick,
            JSL.ButtonMaskRClick,
            JSL.ButtonMaskN,
            JSL.ButtonMaskW,
            JSL.ButtonMaskE,
            JSL.ButtonMaskS,
            JSL.ButtonMaskUp,
            JSL.ButtonMaskLeft,
            JSL.ButtonMaskRight,
            JSL.ButtonMaskDown,
            JSL.ButtonMaskMinus,
            JSL.ButtonMaskPlus
        };
    }

    public enum InputBufferMode {
        Normal,
        Sensitive
    }

    public enum Quality {
        Low,
        Medium,
        High,
        Ultra
    }

    public enum MaxFPSOption {
        Max,
        f240,
        f120,
        f60,
        f45,
        f30
    }

    public enum CrosshairType {
        Default,
        Point
    }

    public enum KeyboardMouseBindType {
        Keyboard,
        Mouse
    }

    public enum ControllerBindType {
        Buttons,
        Stick
    }

    public enum ControllerButtonBinds {
        ZR,
        R,
        ZL,
        L,
        PressLeft,
        PressRight,
        MainTop,
        MainLeft,
        MainRight,
        MainDown,
        ArrowTop,
        ArrowLeft,
        ArrowRight,
        ArrowDown,
        MenuLeft,
        MenuRight
    }

    public enum StickBinds {
        Left,
        Right
    }

    [System.Serializable]
    public class KeyboardMouseBind {
        public KeyboardMouseBindType bindType;
        public KeyCode keyBind;
        public int mouseBind;

        public static KeyboardMouseBind KeyBind (KeyCode keyBind) {
            return new KeyboardMouseBind() {
                bindType = KeyboardMouseBindType.Keyboard,
                keyBind = keyBind
            };
        }

        public static KeyboardMouseBind MouseBind (int mouseBind) {
            return new KeyboardMouseBind() {
                bindType = KeyboardMouseBindType.Mouse,
                mouseBind = mouseBind
            };
        }

        public bool IsDown () {
            if(bindType == KeyboardMouseBindType.Keyboard) {
                return Input.GetKeyDown(keyBind);
            } else {
                return Input.GetMouseButtonDown(mouseBind);
            }
        }

        public bool IsHeld () {
            if(bindType == KeyboardMouseBindType.Keyboard) {
                return Input.GetKey(keyBind);
            } else {
                return Input.GetMouseButton(mouseBind);
            }
        }

        public bool IsUp () {
            if(bindType == KeyboardMouseBindType.Keyboard) {
                return Input.GetKeyUp(keyBind);
            } else {
                return Input.GetMouseButtonUp(mouseBind);
            }
        }
    }

    public class JSLControllerUtils {
        public static readonly List<string> proButtonToName = new List<string>() {
            "ZR",
            "R",
            "ZL",
            "L",
            "LSB",
            "RSB",
            "X",
            "Y",
            "A",
            "B",
            "Up",
            "Left",
            "Right",
            "Down",
            "-",
            "+"
        };

        public static readonly List<string> proStickToName = new List<string>() {
            "Left Stick",
            "Right Stick"
        };
    }
}