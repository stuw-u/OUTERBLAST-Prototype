using UnityEngine;

[DefaultExecutionOrder(20)]
public class TimeManager : MonoBehaviour {

    [Header("Time Scale Interpolation")]
    [Tooltip("In second percent per second")]
    public float timeScaleChangeSpeed = 0.1f;

    [Header("Time Scales")]
    [Tooltip("The time scale at which the server will advance faster that the client (should be < 1)")]
    [Range(0.75f, 1f)] public float minSlowTime = 0.5f;
    [Tooltip("The time scale at which the server will advance slower that the client (should be > 1)")]
    [Range(1f, 1.25f)]  public float maxFastTime = 1.5f;
    public float timeScaleMultiplier = 1f;

    public static TimeManager inst;
    private float targetTimeScaleRange = 0f;
    private float currentTimeScaleRange = 0f;

    private void Awake () {
        inst = this;
    }

    // For slow time, impact must be -1 to 0, fast must be 0 to 1
    public static void ChangeTargetTimeScale (float targetTimeScaleRange) {
        if(inst == null) {
            return;
        }

        inst.targetTimeScaleRange = targetTimeScaleRange;
    }
    
    void LateUpdate () {
        //currentTimeScaleRange = Mathf.MoveTowards(currentTimeScaleRange, targetTimeScaleRange, timeScaleChangeSpeed);
        currentTimeScaleRange = targetTimeScaleRange;

        Time.timeScale = Mathf.Lerp(
            minSlowTime,
            maxFastTime,
            currentTimeScaleRange * 0.5f + 0.5f
        ) * timeScaleMultiplier;
        
        GameManager.DisplayDebugProperty("TimeScale", Time.timeScale);
        GameManager.ClearDebugProperty();
    }
}
