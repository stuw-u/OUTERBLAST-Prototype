using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Player Parameters", menuName = "Custom/Player/Player Parameters", order = -1)]
public class PlayerParametersAsset : ScriptableObject {
    public MovementParameters movementParameters;
    public MovementPhysicsParameters physicsParameters;
    public CameraParameters cameraParameters;

    public float clientCorrectLerpSmooth = 0.1f;
    public float clientCorrectSlerpSmooth = 0.1f;
    public float clientCorrectLerpSpeedPerUnits = 20f;
    public float clientCorrectSlerpSpeed = 0.1f;
}
