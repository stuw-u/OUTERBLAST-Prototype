using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ClawOpenState {
    Grip,
    Closed,
    Opened
}

public class ClawAnimator : MonoBehaviour {
    public ClawOpenState clawOpenState = ClawOpenState.Opened;
    public Transform[] segmentAnchors;
    public float gripAngle = 44f;
    public float closedAngle = 10f;
    public float openedAngle = -64f;
    public float clawSpeed = 5f;

    private float openedValue;


    private void Update () {
        float target = 0f;
        switch(clawOpenState) {
            case ClawOpenState.Grip:
            target = gripAngle;
            break;
            case ClawOpenState.Closed:
            target = closedAngle;
            break;
            case ClawOpenState.Opened:
            target = openedAngle;
            break;
        }
        openedValue = Mathf.MoveTowards(openedValue, target, Time.deltaTime * clawSpeed);
        segmentAnchors[0].localEulerAngles = Vector3.right * openedValue;
        segmentAnchors[1].localEulerAngles = Vector3.right * openedValue;
        segmentAnchors[2].localEulerAngles = Vector3.right * openedValue;
    }
}
