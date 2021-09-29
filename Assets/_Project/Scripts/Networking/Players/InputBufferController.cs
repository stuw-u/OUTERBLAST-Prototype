using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using Blast.Collections;
using Blast.Settings;

public class InputBufferController : NetworkBehaviour {

    
    #region Constants
    // Input buffer constants
    const int maxInputBufferLength = 6;             // The max amout of inputs tolerated before they start getting deleted. 
    const int maxInputBufferLengthSensitive = 10;   // The max amout of inputs tolerated before they start getting deleted. 
    const int normalInputBufferLength = 1;          // The normal length of the buffer (when no packet loss has been detected)
    const int normalInputBufferLengthSensitive = 3;  // The normal length of the buffer (when no packet loss has been detected)
    const int panicInputBufferLength = 3;           // The extended length of the buffer, delayed to have time to fix packet losses
    const int panicInputBufferLengthSensitive = 6;  // The extended length of the buffer, delayed to have time to fix packet losses
    const float inputBufferTolerance = 1.5f;        // The range of packet count at which the length is considered as being reached
    const int inputAverageCount = 6;

    // Other
    const float panicModeTime = 4f;                 // How long will the delayed buffer length be kept until reverting to normal mode
    public const float inputSendInterval = 0.05f;   // The rate at which input packets are being sent
    #endregion

    public int recentInputIndex { private set; get; }
    public InputSnapshot previousSnapshot { private set; get; }
    private InputSnapshot recentSnapshot;
    private LimitedQueue<InputSnapshot> inputQueue;
    private LimitedQueue<int> inputIndexQueue;
    private InputBufferMode inputBufferMode;
    
    private int targetInputBufferLength = normalInputBufferLength;
    private float delayedModeTimer = 0f;
    private int lastReceivedFrameIndex = -1;

    private float timeRescaleSign = 1f; // +1: Must speed up (ex: get from 2 to 8 (+6), we must pump more frame than the server can process them), -1: Must slow
    private float timeShiftLockTimer = 0f;

    // How many inputs to include, when sending an input packet, to cover packet loss (keep to 8)
    public const int includedInputCount = 8;

    #region Input Average
    private float[] previousInputBufferLength;
    private float averageInputBufferLength;
    private int averageIndex = 0;
    #endregion
    
    public void Init (InputBufferMode inputBufferMode) {
        this.inputBufferMode = inputBufferMode;
        inputQueue = new LimitedQueue<InputSnapshot>(math.select(maxInputBufferLength, maxInputBufferLength, inputBufferMode == InputBufferMode.Sensitive));
        inputIndexQueue = new LimitedQueue<int>(math.select(maxInputBufferLength, maxInputBufferLength, inputBufferMode == InputBufferMode.Sensitive));
        previousInputBufferLength = new float[inputAverageCount];

        previousSnapshot = new InputSnapshot() {
            moveAxis = float2.zero
        };
        recentSnapshot = previousSnapshot;
    }
    
    public void EnqueueInputs (NativeArray<InputSnapshot> inputs, int recentFrameIndex) {
        int maxReadCount = 1;
        if(lastReceivedFrameIndex != -1) {
            maxReadCount = math.min(8, recentFrameIndex - lastReceivedFrameIndex);
        }
        lastReceivedFrameIndex = recentFrameIndex;

        maxReadCount = math.min(maxReadCount, inputs.Length);
        for(int i = maxReadCount - 1; i >= 0; i--) {
            inputQueue.Enqueue(inputs[i]);
            inputIndexQueue.Enqueue(recentFrameIndex - i);
        }
    }
    
    public InputSnapshot GetRecentInput () {

        previousSnapshot = recentSnapshot;

        if(inputIndexQueue.TryDequeue(out int index)) {
            recentInputIndex = index;
        }
        if(inputQueue.TryDequeue(out InputSnapshot inputSnapshot)) {
            recentSnapshot = inputSnapshot;
        } else {
            OnPacketLossOrMissing();
        }

        return recentSnapshot;
    }
    
    public void UpdateOnServer () {

        // Predict client correction time
        if(timeShiftLockTimer > 0) {
            timeShiftLockTimer = math.max(0f, timeShiftLockTimer - Time.unscaledDeltaTime);
        }
        bool allowShift = timeShiftLockTimer == 0f;

        // Ticking the buffer timer
        // This will figure out how long a certain target buffer length should be kept
        if(delayedModeTimer > 0f) {
            delayedModeTimer = Mathf.Max(delayedModeTimer - Time.unscaledDeltaTime, 0f);
            targetInputBufferLength = math.select(panicInputBufferLength, panicInputBufferLengthSensitive, inputBufferMode == InputBufferMode.Sensitive);
        } else {
            targetInputBufferLength = math.select(normalInputBufferLength, normalInputBufferLengthSensitive, inputBufferMode == InputBufferMode.Sensitive);
        }


        // Input buffer bellow target (not enough input) means speeding up the client's timeScale
        // Input buffer after target (too much input) means slowing down the client's timeScale
        // The magnitude of the timeScale augmentation (timeScaleMul) depends on the magniude of the bufferLenghtDiff.
        float bufferAvg = GetAverageInputBuffer(inputQueue.Count);
        //GameManager.DisplayDebugProperty($"InBufferLenght {LobbyManager.inst.localPlayers[OwnerClientId].userData.Value.username}", bufferAvg);
        float bufferLenghtDiff = targetInputBufferLength - bufferAvg;


        // Do not constantly send. Predict when frame will be finished, then resend.
        if(math.abs(math.round(bufferLenghtDiff)) > 1 && allowShift) {
            timeShiftLockTimer = ServerPredicticShiftTime((int)math.round(bufferLenghtDiff)) + (NetAssist.GetPlayerRTT(OwnerClientId) / 1000f) + inputAverageCount * Time.fixedDeltaTime;
            //InvokeClientRpcOnClient(ClientRPCRequestTimeShift, OwnerClientId, (int)math.round(bufferLenghtDiff));
            RequestTimeShiftClientRPC((int)math.round(bufferLenghtDiff), new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = LobbyManager.inst.clientLists.onlyClient[OwnerClientId]} });
        }
    }

    public void UpdateOnClient () {
        if(timeShiftLockTimer > 0) {
            timeShiftLockTimer = math.max(0f, timeShiftLockTimer - Time.fixedDeltaTime);
        }
        if(timeShiftLockTimer == 0f) {
            TimeManager.ChangeTargetTimeScale(0f);
        }
    }
    
    public void OnPacketLossOrMissing () {
        delayedModeTimer = panicModeTime;
    }
    
    private float GetAverageInputBuffer (int bufferLength) {
        previousInputBufferLength[averageIndex] = bufferLength;
        averageIndex++;
        if(averageIndex >= inputAverageCount)
            averageIndex = 0;

        float total = 0f;
        for(int i = 0; i < inputAverageCount; i++) {
            total += previousInputBufferLength[i];
        }
        total /= inputAverageCount;

        return total;
    }
    
    [ClientRpc]
    private void RequestTimeShiftClientRPC (int framesToShift, ClientRpcParams clientRpcParams = default) {
        if(TimeManager.inst == null) {
            return;
        }

        if(framesToShift > 0) { // Must speed up
            timeRescaleSign = 1f;
        } else {                // Must slow down
            timeRescaleSign = -1f;
        }
        timeShiftLockTimer = ServerPredicticShiftTime(framesToShift);
        TimeManager.ChangeTargetTimeScale(timeRescaleSign);
    }

    public float ServerPredicticShiftTime (int framesToShift) {
        if(framesToShift > 0) { // Must speed up
            float timeToGain = Time.fixedDeltaTime * math.min(2, framesToShift);
            return (timeToGain / (TimeManager.inst.maxFastTime - 1f));
        } else {                // Must slow down
            float timeToLoose = Time.fixedDeltaTime * math.min(2, -framesToShift);
            return (timeToLoose / (1f - TimeManager.inst.minSlowTime));
        }
    }
}
