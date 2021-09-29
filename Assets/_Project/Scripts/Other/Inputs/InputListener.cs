using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using Blast.Settings;

public enum GyroMode {
    Off,        // Super Lame
    TurnStickXY,  // Odd...
    TurnStickX,  // Cool!
    FlickStick  // Now we're talking!
}

public enum Buttons {
    Jump,
    MainWeapon,
    MiningTool,
    MeleeWeapon,
    MainDefence,
    Consumables
}

public class InputListener : MonoBehaviour {
    
    [Header("Parameters")]
    public float stickSensibility = 120f;
    public float gyroScale = 1f;
    public float2 mouseSensibility;
    public float clibrationAngle = 25f;
    public GyroMode gyroMode = GyroMode.FlickStick;
    public float flickThreshold = 0.9f;
    public float flickTime = 0.1f;


    private static InputListener inst;

    private BaseControls controls;
    private int connectedJoys = 0;
    private int[] joysHandle;
    private uint selectedItemUID;

    private void Awake () {
        if(inst != null) {
            DestroyImmediate(gameObject);
            return;
        }

        inst = this;

        // Gyro Controller Support
        connectedJoys = JSL.JslConnectDevices();
        joysHandle = new int[connectedJoys];
        JSL.JslGetConnectedDeviceHandles(joysHandle, connectedJoys);

        // Classic Controls
        controls = new BaseControls();
        controls.Enable();
    }

    private void Start () {
        if(NetAssist.IsHeadlessServer) {
            Destroy(gameObject);
        }
    }

    private void OnDestroy () {
        JSL.JslDisconnectAndDisposeAll();
    }

    private InputSnapshot recentSnapshot;
    private JSL.JOY_SHOCK_STATE recentSS;
    private JSL.IMU_STATE recentIMU;

    float targetX, targetY;
    bool wasCalibrating = false;
    int heldWeapon;
    float flickProgress = 0.0f;
    float flickSize = 0.0f;
    float2 lastRStick;
    int lastButtons;
    bool zlHeld, zrHeld;

    public static void RecordInputs () {
        if(inst == null) {
            Debug.LogError("InputListener not present in the scene or not initialized yet.");
            return;
        }

        inst.Internal_RecordInputs();
    }

    private void Internal_RecordInputs () {
        InputSnapshot lastSnapshot = recentSnapshot;
        recentSnapshot = new InputSnapshot();

        if(PausedMenu.isPaused || ChatUI.IsWritting) {
            recentSnapshot.moveAxis = float2.zero;
            recentSnapshot.lookAxis = lastSnapshot.lookAxis;
            return;
        }

        // Remove me in console ports lolz
        targetX += Input.GetAxis("Mouse X") * SettingsManager.settings.mouseSensibilityX;
        targetY -= Input.GetAxis("Mouse Y") * SettingsManager.settings.mouseSensibilityY;
        if(connectedJoys == 0) {
            targetX = Mathf.Repeat(targetX, 360f);
            targetY = math.clamp(targetY, -90f, 90f);
        }

        gyroMode = SettingsManager.settings.gyroMode;

        JSL.JOY_SHOCK_STATE ss = new JSL.JOY_SHOCK_STATE();
        recentSnapshot.moveAxis = float2.zero;
        if(connectedJoys > 0) {

            // Sample data from joy controllers
            ss = JSL.JslGetSimpleState(joysHandle[0]);
            JSL.IMU_STATE imu = JSL.JslGetIMUState(joysHandle[0]);
            recentSS = ss;
            recentIMU = imu;

            // Processed stick input. Todo; improve dead zone linearity
            float2 rawLookJoy;
            if(SettingsManager.settings.lookStick == StickBinds.Left) {
                rawLookJoy = new float2(inst.recentSS.stickLX, inst.recentSS.stickLY);
            } else {
                rawLookJoy = new float2(inst.recentSS.stickRX, inst.recentSS.stickRY);
            }

            float rstickX = math.select(0f, rawLookJoy.x, math.abs(rawLookJoy.x) > 0.2f);
            float rstickY = math.select(0f, rawLookJoy.y, math.abs(rawLookJoy.y) > 0.2f);
            float2 rstick = new float2(rstickX, rstickY);

            // Calibrate Gyro and View In-Game
            if((ss.buttons >> Settings.BindToMask[(int)SettingsManager.settings.alignView] & 1) == 1) {
                targetY = -SettingsManager.settings.gyroAlignmentAngle;
            }
            bool isCalibrating = (ss.buttons >> Settings.BindToMask[(int)SettingsManager.settings.clibrateGyro] & 1) == 1;
            if(!wasCalibrating && isCalibrating) {
                JSL.JslResetContinuousCalibration(joysHandle[0]);
                JSL.JslStartContinuousCalibration(joysHandle[0]);
            }
            if(wasCalibrating && !isCalibrating) {
                JSL.JslPauseContinuousCalibration(joysHandle[0]);
            }
            wasCalibrating = isCalibrating;


            // Look direction!
            if(gyroMode != GyroMode.FlickStick) {
                targetX += rstickX * SettingsManager.settings.stickSensibilityX * Time.deltaTime;
                if(gyroMode == GyroMode.TurnStickXY || gyroMode == GyroMode.Off) {
                    float yOffset = -rstickY * SettingsManager.settings.stickSensibilityY * Time.deltaTime;
                    if(yOffset > 0) {
                        targetY = math.select(targetY, math.min(90f, targetY + yOffset), targetY < 90f);
                    } else {
                        targetY = math.select(targetY, math.max(-90f, targetY + yOffset), targetY > -90f);
                    }
                }
            } else {
                targetX += HandleFlickStick(lastRStick, rstick, Time.deltaTime);
            }
            lastRStick = rstick;


            // Gyro Support!
            if(gyroMode != GyroMode.Off) {
                targetX -= imu.gyroY * SettingsManager.settings.gyroSensibilityX * Time.deltaTime;
                targetY -= imu.gyroX * SettingsManager.settings.gyroSensibilityY * Time.deltaTime;
            }


            // Joy input report
            zlHeld = ss.lTrigger > 0.5f;
            zrHeld = ss.rTrigger > 0.5f;

            if((ss.buttons >> Settings.BindToMask[(int)SettingsManager.settings.switchLC] & 1) == 1 && (lastButtons >> Settings.BindToMask[(int)SettingsManager.settings.switchLC] & 1) == 0) {
                heldWeapon = (int)mathUtils.mod(heldWeapon - 1, 3);
            } else if((ss.buttons >> Settings.BindToMask[(int)SettingsManager.settings.switchRC] & 1) == 1 && (lastButtons >> Settings.BindToMask[(int)SettingsManager.settings.switchRC] & 1) == 0) {
                heldWeapon = (int)mathUtils.mod(heldWeapon + 1, 3);
            }
            lastButtons = ss.buttons;

            recentSnapshot.SetButton((ss.buttons >> Settings.BindToMask[(int)SettingsManager.settings.jumpC] & 1) == 1, 0);
            recentSnapshot.SetButton((ss.buttons >> JSL.ButtonMaskLClick & 1) == 1, 1);
            if(SettingsManager.settings.moveStick == StickBinds.Left) {
                recentSnapshot.moveAxis = new float2(ss.stickLX, ss.stickLY);
            } else {
                recentSnapshot.moveAxis = new float2(ss.stickRX, ss.stickRY);
            }
            
        }

        #region Keyboard Movement 
        // Default Input Snapshot
        float2 moveAxis = float2.zero;
        moveAxis.y += SettingsManager.settings.moveForward.IsHeld() ? 1f : 0;
        moveAxis.y += SettingsManager.settings.moveBackward.IsHeld() ? -1f : 0;
        moveAxis.x += SettingsManager.settings.moveRight.IsHeld() ? 1f : 0;
        moveAxis.x += SettingsManager.settings.moveLeft.IsHeld() ? -1f : 0;
        recentSnapshot.moveAxis += moveAxis;
        #endregion

        float2 moveAxisDir = math.normalizesafe(recentSnapshot.moveAxis);
        float2 moveAxisLen = math.length(recentSnapshot.moveAxis);
        moveAxisLen = math.saturate(math.unlerp(0.3f, 1f, moveAxisLen));
        recentSnapshot.moveAxis = moveAxisDir * moveAxisLen;

        // Buttons:
        // - Jump
        // - Use main
        // - Use melee
        // - Use mining gear
        // - Use shield
        // - Use consumables

        #region Button Setup
        recentSnapshot.SetButton(SettingsManager.settings.jumpKM.IsHeld() || isBindButtonPressed(SettingsManager.settings.jumpC, ss), 0);
        if(SettingsManager.settings.switch0KM.IsDown()) {
            heldWeapon = 0;
        }
        if(SettingsManager.settings.switch1KM.IsDown()) {
            heldWeapon = 1;
        }
        if(SettingsManager.settings.switch2KM.IsDown()) {
            heldWeapon = 2;
        }

        heldWeapon = (int)Mathf.Repeat(heldWeapon + -Mathf.Round(Input.mouseScrollDelta.y), 3);

        switch(heldWeapon) {
            case 0:
            recentSnapshot.SetButton(SettingsManager.settings.useHeldKM.IsHeld() || isBindButtonPressed(SettingsManager.settings.useHeldC, ss), 1);
            recentSnapshot.SetButton(false, 2);
            recentSnapshot.SetButton(false, 3);
            break;
            case 1:
            recentSnapshot.SetButton(false, 1);
            recentSnapshot.SetButton(SettingsManager.settings.useHeldKM.IsHeld() || isBindButtonPressed(SettingsManager.settings.useHeldC, ss), 2);
            recentSnapshot.SetButton(false, 3);
            break;
            case 2:
            recentSnapshot.SetButton(false, 1);
            recentSnapshot.SetButton(false, 2);
            recentSnapshot.SetButton(SettingsManager.settings.useHeldKM.IsHeld() || isBindButtonPressed(SettingsManager.settings.useHeldC, ss), 3);
            break;
        }
        recentSnapshot.SetButton(SettingsManager.settings.shieldKM.IsHeld() || isBindButtonPressed(SettingsManager.settings.shieldC, ss), 4);
        recentSnapshot.SetButton(SettingsManager.settings.consumablesKM.IsHeld() || isBindButtonPressed(SettingsManager.settings.consumablesC, ss), 5);
        #endregion

        recentSnapshot.lookAxis = new float2(Mathf.Repeat(targetX, 360f), math.clamp(targetY, -90f, 90f));
        recentSnapshot.selectedFixedInventoryItem = (byte)heldWeapon;
        recentSnapshot.selectedInventoryItemUID = selectedItemUID;
    }

    #region Utils
    private float HandleFlickStick (float2 lastStick, float2 stick, float deltaTime) {
        float result = 0.0f;

        float lastLength = math.length(lastStick);
        float length = math.length(stick);

        // By comparing the last frame to this one we can decide whether a flick is starting
        if(length >= flickThreshold) {
            if(lastLength < flickThreshold) {
                // Flick start!
                flickProgress = 0.0f; // Festart flick timer
                flickSize = math.degrees(math.atan2(stick.x, stick.y)); // Stick angle from up/forward
            } else {
                // Turn!
                // ...
            }

        } else {
            // Turn cleanup
            // ...
        }

        // Continue flick
        float lastFlickProgress = flickProgress;
        if(lastFlickProgress < flickTime) {
            flickProgress = math.min(flickProgress + deltaTime, flickTime);

            // Get last time and this time in 0-1 completion range
            float lastPerOne = lastFlickProgress / flickTime;
            float thisPerOne = flickProgress / flickTime;

            // Our WarpEaseOut function stays within the 0-1 range but pushes it all closer to 1
            float warpedLastPerOne = WarpEaseOut(lastPerOne);
            float warpedThisPerOne = WarpEaseOut(thisPerOne);

            // Now use the difference between last frame/sample and this frame/sample
            result += (warpedThisPerOne - warpedLastPerOne) * flickSize;
        }

        return result;
    }

    private float WarpEaseOut (float input) {
        float flipped = 1.0f - input;
        return 1.0f - flipped * flipped;
    }
    #endregion

    public static InputSnapshot MakeSnapshot () {
        return inst.recentSnapshot;
    }

    public static void SetSelectedItemUID (uint selectedItemUID) {
        inst.selectedItemUID = selectedItemUID;
    }

    public static float GetMouseAxisX () {
        return inst.targetX;
    }

    public static int GetHeldWeapon () {
        return inst.heldWeapon;
    }

    public static float GetMouseAxisY () {
        return math.clamp(inst.targetY, -90, 90);
    }

    public static bool GetControllerBind (out ControllerButtonBinds controllerBind) {
        return inst._GetControllerBind(out controllerBind);
    }

    private bool _GetControllerBind (out ControllerButtonBinds controllerBind) {
        if(connectedJoys == 0) {
            controllerBind = ControllerButtonBinds.ZR;
            return false;
        }

        // Sample data from joy controllers
        JSL.JOY_SHOCK_STATE ss = JSL.JslGetSimpleState(joysHandle[0]);

        if(ss.lTrigger > 0.5f) {
            controllerBind = ControllerButtonBinds.ZL;
            return true;
        } else if(ss.rTrigger > 0.5f) {
            controllerBind = ControllerButtonBinds.ZR;
            return true;
        } else if(ss.buttons > 0) {
            if((ss.buttons >> JSL.ButtonMaskN & 1) == 1) {
                controllerBind = ControllerButtonBinds.MainTop;
            } else if((ss.buttons >> JSL.ButtonMaskE & 1) == 1) {
                controllerBind = ControllerButtonBinds.MainRight;
            } else if((ss.buttons >> JSL.ButtonMaskW & 1) == 1) {
                controllerBind = ControllerButtonBinds.MainLeft;
            } else if((ss.buttons >> JSL.ButtonMaskS & 1) == 1) {
                controllerBind = ControllerButtonBinds.MainDown;
            } else if((ss.buttons >> JSL.ButtonMaskDown & 1) == 1) {
                controllerBind = ControllerButtonBinds.ArrowDown;
            } else if((ss.buttons >> JSL.ButtonMaskLeft & 1) == 1) {
                controllerBind = ControllerButtonBinds.ArrowLeft;
            } else if((ss.buttons >> JSL.ButtonMaskRight & 1) == 1) {
                controllerBind = ControllerButtonBinds.ArrowRight;
            } else if((ss.buttons >> JSL.ButtonMaskUp & 1) == 1) {
                controllerBind = ControllerButtonBinds.ArrowTop;
            } else if((ss.buttons >> JSL.ButtonMaskL & 1) == 1) {
                controllerBind = ControllerButtonBinds.L;
            } else if((ss.buttons >> JSL.ButtonMaskR & 1) == 1) {
                controllerBind = ControllerButtonBinds.R;
            } else if((ss.buttons >> JSL.ButtonMaskLClick & 1) == 1) {
                controllerBind = ControllerButtonBinds.PressLeft;
            } else if((ss.buttons >> JSL.ButtonMaskRClick & 1) == 1) {
                controllerBind = ControllerButtonBinds.PressRight;
            } else if((ss.buttons >> JSL.ButtonMaskMinus & 1) == 1) {
                controllerBind = ControllerButtonBinds.MenuLeft;
            } else if((ss.buttons >> JSL.ButtonMaskPlus & 1) == 1) {
                controllerBind = ControllerButtonBinds.MenuRight;
            } else {
                controllerBind = ControllerButtonBinds.ZL;
            }
            return true;
        } else {
            controllerBind = ControllerButtonBinds.ZR;
            return false;
        }
    }

    private bool isBindButtonPressed (ControllerButtonBinds bind, JSL.JOY_SHOCK_STATE ss) {
        if(bind == ControllerButtonBinds.ZL) {
            return ss.lTrigger > 0.5f;
        } else if(bind == ControllerButtonBinds.ZR) {
            return ss.rTrigger > 0.5f;
        } else {
            return (ss.buttons >> Settings.BindToMask[(int)bind] & 1) == 1;
        }
    }

    public static Vector2 GetCursorDelta () {
        if(inst.connectedJoys > 0) {
            JSL.JOY_SHOCK_STATE ss = JSL.JslGetSimpleState(inst.joysHandle[0]);
            Vector2 stickDelta = new Vector2(ss.stickLX + ss.stickRX, ss.stickLY + ss.stickRY);
            float stickRawMagnitude = Mathf.Clamp01(stickDelta.magnitude);

            return stickDelta.normalized * Mathf.Clamp01(Mathf.InverseLerp(0.2f, 1f, stickRawMagnitude));
        }
        return Vector2.zero;
    }

    private bool wasEastDown;
    public static void GetCursorEvents (out bool isDown, out bool isPressed, out bool isUp) {
        bool isEastDown = false;
        isDown = false;
        isPressed = false;
        isUp = false;
        if(inst.connectedJoys > 0) {
            JSL.JOY_SHOCK_STATE ss = JSL.JslGetSimpleState(inst.joysHandle[0]);
            isEastDown = (ss.buttons >> JSL.ButtonMaskE & 1) == 1;

            isDown = !inst.wasEastDown && isEastDown;
            isPressed = isEastDown;
            isUp = inst.wasEastDown && !isEastDown;
        }
        inst.wasEastDown = isEastDown;
    }
}
