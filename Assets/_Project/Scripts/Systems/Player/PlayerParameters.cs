using UnityEngine;
using System;
using Unity.Mathematics;
using MLAPI.Serialization;

[Serializable]
public class CameraParameters {
    public float SensibilityX = 0f;
    public float SensibilityY = 0f;
    public float SmoothingSpeed = 0.1f;
    public float CameraDirectionalSnapSpeed = 0.1f;
    public float CameraCrouchSnapSpeed = 1f;
}

public struct CameraParametersData {
    public float sensibilityX;
    public float sensibilityY;
    public float smoothingSpeed;
    public float cameraDirSnapSpeed;

    public CameraParametersData DataFromParameters (CameraParameters param) {
        return new CameraParametersData() {
            sensibilityX =          param.SensibilityX,
            sensibilityY =          param.SensibilityY,
            smoothingSpeed =        param.SmoothingSpeed,
            cameraDirSnapSpeed =    param.CameraDirectionalSnapSpeed
        };
    }
}


[Serializable]
public class MovementParameters {
    public float MoveAccelerationSpeed = 5f;
    public float MoveMaxSpeed = 15f;
    public float MoveSlideAcceleration = 4f;
    public float MoveSlideMaxSpeed = 20f;

    public float SlopeFactor = 0.0222f;

    public float ArealDriftAccelerationSpeed = 1f;
    public float ArealDriftMaxSpeed = 10f;

    public float JumpForce = 15f;
    public float JumpCooldown = 0.1f;
    public float MaxAirJumpDelay = 0.1f;
}

public struct MovementParametersData {
    public float moveAccSpeed;
    public float moveMaxSpeed;
    public float moveSlideAcc;
    public float moveSlideMaxSpeed;

    public float slopeFactor;

    public float arealDriftAccSpeed;
    public float arealDriftMaxSpeed;

    public float jumpForce;
    public float jumpCooldown;
    public float maxAirJumpDelay;

    public MovementParametersData DataFromParameters (MovementParameters param) {
        return new MovementParametersData {
            moveAccSpeed =          param.MoveAccelerationSpeed,
            moveMaxSpeed =          param.MoveMaxSpeed,
            moveSlideAcc =          param.MoveSlideAcceleration,
            moveSlideMaxSpeed =     param.MoveSlideMaxSpeed,
            slopeFactor =           param.SlopeFactor,
            arealDriftAccSpeed =    param.ArealDriftAccelerationSpeed,
            arealDriftMaxSpeed =    param.ArealDriftMaxSpeed,
            jumpForce =             param.JumpForce,
            jumpCooldown =          param.JumpCooldown,
            maxAirJumpDelay =       param.MaxAirJumpDelay
        };
    }
}


[Serializable]
public class MovementPhysicsParameters {
    public Vector3 GravityForce;
    public float gravityForce;
    public float unslippingSpeed = 2f;
    [Range(0.0f, 10.0f)]
    public float GroundFriction = 0.75f;
    [Range(0.0f, 10.0f)]
    public float ArealFriction = 0.985f;
    [Range(0.0f, 10.0f)]
    public float ArealVerticalFriction = 0.985f;
    [Range(0.0f, 10.0f)]
    public float SlideFriction = 0.75f;
    public float FallMultiplier = 2f;
    public float LowFallMultiplier = 1.5f;

    public float sneakHeightShiftSpeed = 0.2f;
    public float colliderHeight;
    public float colliderRadius;
    public float groundSphereRadius;
    public float groundSphereYOffset;
}

public struct MovementPhysicsParametersData {
    public float unslippingSpeed;
    public float groundFriction;
    public float arealFriction;
    public float arealVerticalFriction;
    public float slideFriction;
    public float fallMultiplier;
    public float lowFallMultiplier;
    
    public float colliderHeight;
    public float colliderRadius;
    public float groundSphereRadius;
    public float groundSphereYOffset;

    public MovementPhysicsParametersData DataFromParameters (MovementPhysicsParameters param) {
        return new MovementPhysicsParametersData {
            unslippingSpeed =       param.unslippingSpeed,
            groundFriction =        param.GroundFriction,
            arealFriction =         param.ArealFriction,
            arealVerticalFriction = param.ArealVerticalFriction,
            slideFriction =         param.SlideFriction,
            fallMultiplier =        param.FallMultiplier,
            lowFallMultiplier =     param.LowFallMultiplier,
            colliderHeight =        param.colliderHeight,
            colliderRadius =        param.colliderRadius,
            groundSphereRadius =    param.groundSphereRadius,
            groundSphereYOffset =   param.groundSphereYOffset
        };
    }
}



#region Legacy System
public struct PlayerStateOld {
    public float targetPlayerHeight;
    public float playerHeight;
    public float groundedCounter;
    public float jumpTimer;
    public float gliderMagnitude;

    public float groundDistance;
    public Vector3 groundNormal;
    public bool isGrounded;

    public Vector3 position;
    public Vector3 velocity;
    public Quaternion rotation;

    public int index;
}
#endregion