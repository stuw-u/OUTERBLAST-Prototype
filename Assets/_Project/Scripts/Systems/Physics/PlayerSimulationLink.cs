using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using KinematicCharacterController;
using Blast.ECS;
using Blast.ECS.DefaultComponents;

public class PlayerSimulationLink : MonoBehaviour, ICharacterController {

    public KinematicCharacterMotor Motor;
    public Entity playerEntity;
    
    private void Awake () {
        Motor.CharacterController = this;
    }

    public void Init (Entity playerEntity) {
        this.playerEntity = playerEntity;
    }


    private float cachedBounciness;
    public void UpdatePhase1 (KinematicCharacterMotorState state, float deltaTime, float bounciness) {
        cachedBounciness = bounciness;
        Motor.ApplyState(state, true);
        Motor.UpdatePhase1(deltaTime, cachedBounciness);
    }

    public KinematicCharacterMotorState UpdatePhase2 (float deltaTime) {
        Motor.UpdatePhase2(deltaTime, cachedBounciness);
        Motor.Transform.SetPositionAndRotation(Motor.TransientPosition, Motor.TransientRotation);
        return Motor.GetState();
    }

    public void UpdateRotation (ref Quaternion currentRotation, float deltaTime) {
        
    }

    public void UpdateVelocity (ref Vector3 currentVelocity, float deltaTime) {
        ref var player = ref playerEntity.Get<PlayerComponent>();
        ref var playerEffects = ref playerEntity.Get<PlayerEffects>();
        float3 position = Motor.TransientPosition;
        quaternion rot = Motor.TransientRotation;
        ref var input = ref playerEntity.Get<InputControlledComponent>();
        ref var _localToWorld = ref playerEntity.Get<LocalToWorld>();
        bool isGhost = playerEntity.Has<Ghost>();
        ref var interpR = ref playerEntity.Get<InterpolatedRotationEntity>();
        float3 globalDown = math.mul(rot, math.down());
        MovementPhysicsParameters movementPhysicsParam = GameManager.inst.playerParametersAsset.physicsParameters;
        MovementParameters movementParam = GameManager.inst.playerParametersAsset.movementParameters;

        #region Jumping
        player.jumpTimer = math.max(0, player.jumpTimer - deltaTime);

        bool canJump =
            (input.inputSnapshot.GetButton(0)) &&
            (player.isGrounded || player.coyoteTimer < 13) &&
            (player.jumpTimer <= 0f) &&
            LobbyWorldInterface.inst.LocalLobbyState == LocalLobbyState.InGame &&
            !isGhost && playerEffects.IsEffectApplied(1) == 0;

        if(input.inputSnapshot.GetButtonDown(0, input.lastButtonRaw) && !(player.isGrounded || player.coyoteTimer < 13)) {
            player.inAirByJump = false;
        } else if(player.isGrounded || player.coyoteTimer < 13) {
            player.inAirByJump = false;
        }
        if(canJump) {
            if(!SimulationManager.inst.IsReplayingFrame) {
                AudioManager.PlayClientEnvironmentSoundAt((int)player.clientId, position, EnvironmentSound.Jumping);
            }

            float3 invVel = math.transform(math.inverse(_localToWorld.rotationValue), currentVelocity);

            player.inAirByJump = true;
            player.jumpTimer = movementParam.JumpCooldown;
            player.isGrounded = false;
            player.coyoteTimer = 255;

            float3 jumpVector = new float3(0f, movementParam.JumpForce, 0f);
            invVel = new Vector3(
                invVel.x + jumpVector.x,
                jumpVector.y,
                invVel.z + jumpVector.z
            );
            Motor.ForceUnground();

            currentVelocity = math.transform(_localToWorld.rotationValue, invVel);
        }
        #endregion

        if(player.sliperyTimer > 0f) {
            Motor.ForceUnground();
        }
    }



    public bool IsColliderValidForCollisions (Collider coll) {
        return true;
    }

    public void AfterCharacterUpdate (float deltaTime) {
    }

    public void BeforeCharacterUpdate (float deltaTime) {
    }

    public void OnDiscreteCollisionDetected (Collider hitCollider) {
    }

    public void OnGroundHit (Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
    }

    public void OnMovementHit (Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
    }

    public void PostGroundingUpdate (float deltaTime) {
    }

    public void ProcessHitStabilityReport (Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) {
    }
}
