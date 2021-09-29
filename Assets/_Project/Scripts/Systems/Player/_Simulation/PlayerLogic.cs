using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Blast.ECS;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using Blast.ECS.DefaultComponents;

public static class PlayerLogic  {
    public static void Run (ref Entity playerEntity, ref CameraParametersData cameraParam, ref MovementParametersData movementParam, 
        ref MovementPhysicsParametersData movementPhysicsParam, float deltaTime) {

        ref var player = ref playerEntity.Get<PlayerComponent>();
        ref var translate = ref playerEntity.Get<Position>();
        ref var rotation = ref playerEntity.Get<Rotation>();
        ref var velocity = ref playerEntity.Get<Velocity>();
        ref var gravity = ref playerEntity.Get<AffectedByGravity>();
        ref var input = ref playerEntity.Get<InputControlledComponent>();
        ref var _localToWorld = ref playerEntity.Get<LocalToWorld>();
        bool isGhost = playerEntity.Has<Ghost>();
        ref var interpR = ref playerEntity.Get<InterpolatedRotationEntity>();
        ref var playerEffect = ref playerEntity.Get<PlayerEffects>();

        float4x4 localToWorld = _localToWorld.rotationValue;
        float4x4 worldToLocal = math.inverse(localToWorld);
        float3 pos = translate.value;
        float3 vel = velocity.value;
        quaternion rot = rotation.value;
        float3 globalDown = math.mul(rot, math.down());
        byte freezeEffectLvl = playerEffect.IsEffectApplied(1);


        #region GroundedDetection
        int mask = 1 << 9;
        float3 castPosition = pos + movementPhysicsParam.groundSphereYOffset * -globalDown;
        float3 raycastPosition = pos + (movementPhysicsParam.groundSphereYOffset + 0.1f) * -globalDown;

        bool sphereCheck = Physics.CheckSphere(castPosition, movementPhysicsParam.groundSphereRadius, mask);
        bool raycastGround = Physics.Raycast(raycastPosition, globalDown, out RaycastHit hit, 0.2f, mask);

        player.isGrounded = sphereCheck;
        if(raycastGround) {
            player.groundNormal = -hit.normal;
        } else {
            player.groundNormal = -globalDown;
        }

        if(player.isGrounded) {
            player.coyoteTimer = 0;
        } else {
            player.coyoteTimer = (byte)math.min(player.coyoteTimer + 1, 255);
        }
        #endregion


        #region GravityControl
        gravity.disable = isGhost || player.isGrounded;
        #endregion


        #region Friction
        float3 invVel = math.transform(worldToLocal, vel);


        if(isGhost) {
            invVel *= (1f - deltaTime * math.lerp(8f, 0f, player.sliperyTimer));
        } else if(player.isGrounded && freezeEffectLvl == 0) {
            invVel *= (1f - deltaTime * math.lerp(movementPhysicsParam.groundFriction, 0f, player.sliperyTimer));
        } else {
            float arealSide = (1f - deltaTime * movementPhysicsParam.arealFriction);
            float arealVertical = (1f - deltaTime * (movementPhysicsParam.arealVerticalFriction));
            invVel = new float3(invVel.x * arealSide, invVel.y * arealVertical, invVel.z * arealSide);
        }


        vel = math.transform(localToWorld, invVel);
        player.sliperyTimer = math.max(player.sliperyTimer - deltaTime * movementPhysicsParam.unslippingSpeed, 0f);
        #endregion


        #region Move
        if(isGhost) {
            float3 inputDirection = new float3(input.inputSnapshot.moveAxis.x, 0f, input.inputSnapshot.moveAxis.y);
            quaternion lookRelativeRot = math.mul(rot, quaternion.Euler(math.radians(input.inputSnapshot.lookAxis.y), math.radians(input.inputSnapshot.lookAxis.x), 0f));
            float3 rawDirection = math.mul(lookRelativeRot, inputDirection);
            vel = mathUtils.accelerateVelocity(
                vel,
                rawDirection,
                math.select(15f, 30f, input.inputSnapshot.GetButton(0)),
                math.select(5f, 10f, input.inputSnapshot.GetButton(0)));
        } else {
            float2 moveAxis = input.inputSnapshot.moveAxis * (math.select(0f, 1f, LobbyWorldInterface.inst.LocalLobbyState == LocalLobbyState.InGame));
            if(player.behaviour == PlayerEntityBehaviour.ReplicaSimulated) {
                moveAxis *= 0.5f;
            }
            moveAxis *= math.lerp(1f, 0.5f, player.sliperyTimer);

            float3 inputDirection = new float3(moveAxis.x, 0f, moveAxis.y);
            quaternion lookRelativeRot = math.mul(rot, quaternion.AxisAngle(math.up(), math.radians(input.inputSnapshot.lookAxis.x)));

            float3 rawDirection = math.normalizesafe(math.mul(lookRelativeRot, inputDirection));
            float3 moveDirection = math.cross(
                math.cross(player.isGrounded ? player.groundNormal : -globalDown, rawDirection),
                player.isGrounded ? player.groundNormal : -globalDown
            );

            float speedMul = 1f;
            byte speedEffectLvl = playerEffect.IsEffectApplied(0);
            if(speedEffectLvl > 0) {
                speedMul = 1f + speedEffectLvl * 0.5f;
            }
            if(freezeEffectLvl > 0) {
                speedMul *= 0.05f;
            }

            if(player.isGrounded) {
                float angleConfrontation = 90 - mathUtils.angle(player.groundNormal, rawDirection);
                float slopeFactor = 1f + angleConfrontation * movementParam.slopeFactor;
                
                vel = mathUtils.accelerateVelocity(
                    vel,
                    moveDirection * math.length(inputDirection),
                    movementParam.moveMaxSpeed * slopeFactor * speedMul,
                    movementParam.moveAccSpeed * slopeFactor * speedMul);
            } else {
                vel = mathUtils.accelerateVelocity(
                    vel,
                    moveDirection * math.length(inputDirection),
                    movementParam.arealDriftMaxSpeed * speedMul,
                    movementParam.arealDriftAccSpeed * speedMul);
            }
        }
        #endregion


        #region Effects

        if(NetAssist.IsServer) {
            byte healEffect = playerEffect.IsEffectApplied(2);
            if(healEffect > 0) {
                player.damage = math.max(0f, player.damage - 1);
            }
        }
        #endregion


        #region Respawning
        if(GravitySystem.GetGravityFieldType() == GravityFieldType.DirectionAligned) {
            #region Directional Gravity
            if(pos.y < -50 && !isGhost) {
                if(NetAssist.IsClient && !SimulationManager.inst.IsReplayingFrame) {
                    if(player.clientId == NetAssist.ClientID) {
                        GameManager.inst.CloseScreen();
                    }
                }
            } else if(LobbyWorldInterface.inst.LocalLobbyState == LocalLobbyState.InGame) {
                if(player.clientId == NetAssist.ClientID) {
                    GameManager.inst.OpenScreen();
                }
            }
            if(pos.y < -100 && !isGhost) {
                int playerCount = LobbyWorldInterface.inst.PlayerCount;
                float respawnAngle = 0f;
                if(LobbyManager.inst != null) {
                    respawnAngle = math.PI * 2 * (1f / playerCount) * LobbyManager.inst.GetClientOrderIndex(player.clientId);
                }
                pos = new float3(
                    math.cos(respawnAngle) * GameManager.inst.respawnRadius,
                    GameManager.inst.respawnHeight,
                    math.sin(respawnAngle) * GameManager.inst.respawnRadius);
                vel = math.up() * 40f;

                if(NetAssist.IsClient && !SimulationManager.inst.IsReplayingFrame) {
                    if(player.clientId == NetAssist.ClientID) {
                        AudioManager.PlayMenuSound(MenuSound.Falloff);
                    }
                }

                if(NetAssist.IsServer) {
                    TaggingSystem.ClaimEntity(playerEntity);
                }

                player.damage = 0f;
            }
            #endregion
        } else if(GravitySystem.GetGravityFieldType() == GravityFieldType.Spherical) {
            #region Spherical Gravity
            float distanceToCenter = math.distance(float3.zero, pos);
            float3 vectorToCenter = math.normalizesafe(float3.zero - pos);
            if(distanceToCenter < 14f && !isGhost) {
                player.sliperyTimer = math.max(1f, player.sliperyTimer + Time.deltaTime * 1f);
                vel *= 0.9f;
                vel += GravitySystem.GetGravityVector(pos) * deltaTime * 9f;
                if(NetAssist.IsClient && !SimulationManager.inst.IsReplayingFrame) {
                    if(player.clientId == NetAssist.ClientID) {
                        GameManager.inst.CloseScreen();
                    }
                }
            } else if(LobbyWorldInterface.inst.LocalLobbyState == LocalLobbyState.InGame) {
                if(player.clientId == NetAssist.ClientID) {
                    GameManager.inst.OpenScreen();
                }
            }
            if(distanceToCenter < 5f && !isGhost && player.sliperyTimer > 1.4f) {
                pos = vectorToCenter * 45f;
                vel = vectorToCenter * 35f;
                player.sliperyTimer = 0f;

                if(NetAssist.IsClient && !SimulationManager.inst.IsReplayingFrame) {
                    if(player.clientId == NetAssist.ClientID) {
                        AudioManager.PlayMenuSound(MenuSound.Falloff);
                    }
                }

                if(NetAssist.IsServer) {
                    TaggingSystem.ClaimEntity(playerEntity);
                }

                player.damage = 0f;
            }
            #endregion
        }
        #endregion


        #region Collision
        bool hasAnchored = false;
        if(!isGhost) {
            ulong clientId = player.clientId;
            bool isJumpDown = input.inputSnapshot.GetButtonDown(0, input.lastButtonRaw);
            int doAnchor = -1;
            if(player.anchoredClient != 255)
                doAnchor = -2;

            SimulationManager.inst.spatialHasherSystem.GetNearEntity(pos, (e) => {
                if(!e.Has<PlayerComponent>())
                    return;
                if(e.Has<Ghost>())
                    return;
                    
                ref var tplayer = ref e.Get<PlayerComponent>();
                if(tplayer.clientId == clientId)
                    return;

                float3 targetPlayerPos = e.Get<Position>().value;

                float3 posA = targetPlayerPos + -globalDown * 0.5f;
                float3 posB = pos + -globalDown * 0.5f;
                float3 diffVector = posB - posA;
                if(math.lengthsq(diffVector) < 1f) {
                    float diffLen = math.max(0.01f, math.length(diffVector));
                    float3 diffVectorNormal = diffVector / diffLen;
                    float pushValue = math.max(0f, 1f - diffLen); // 1 = 2*radius
                    vel += pushValue * diffVectorNormal * 8f;
                }

                float3 posAStacking = targetPlayerPos + -globalDown * 2.5f;
                float3 diffVectorStacking = posB - posAStacking;
                if(math.lengthsq(diffVectorStacking) < (1.5f * 1.5f) && isJumpDown && doAnchor == -1) {
                    bool alreadyAnchoredTo = false;
                    foreach(KeyValuePair<ulong, Entity> p in SimulationManager.inst.playerSystem.playerEntities) {
                        alreadyAnchoredTo |= (p.Value.Get<PlayerComponent>().anchoredClient == tplayer.clientId);
                    }
                    if(!alreadyAnchoredTo) {
                        AudioManager.PlayClientEnvironmentSoundAt((int)clientId, posA, EnvironmentSound.Stacking);
                        hasAnchored = true;
                        doAnchor = (int)tplayer.clientId;
                    }
                }
            });
            if(doAnchor >= 0) {
                player.anchoredClient = (byte)doAnchor;
            }
        }
        #endregion


        #region Player Stacking
        if(player.anchoredClient != 255 && !isGhost) {
            bool isJumpDown = input.inputSnapshot.GetButtonDown(0, input.lastButtonRaw);
            if(isJumpDown && !hasAnchored) {
                AudioManager.PlayClientEnvironmentSoundAt((int)player.clientId, pos, EnvironmentSound.Unstacking);
                player.isGrounded = true;
                player.groundNormal = -globalDown;
                player.anchoredClient = 255;
            }
            if(SimulationManager.inst.playerSystem.playerEntities.TryGetValue(player.anchoredClient, out Entity e)) {
                float3 targetPlayerPos = e.Get<Position>().value;
                
                float3 posB = pos + -globalDown * 0.5f;
                float3 posAStacking = targetPlayerPos + -globalDown * 2f;
                float3 diffVectorStacking = posB - posAStacking;
                if(math.lengthsq(diffVectorStacking) < (1.5f * 1.5f) && !(isJumpDown && !hasAnchored)) {
                    float diffLen = math.max(0.01f, math.length(diffVectorStacking));
                    float3 diffVectorNormal = diffVectorStacking / diffLen;
                    vel = -diffVectorNormal * (diffLen / 1.5f) * 60f;
                } else {
                    AudioManager.PlayClientEnvironmentSoundAt((int)player.clientId, posAStacking, EnvironmentSound.Unstacking);
                    vel = e.Get<Velocity>().value;
                    player.anchoredClient = 255;
                }
            }
        }
        #endregion


        if(!SimulationManager.inst.IsReplayingFrame && LobbyWorldInterface.TryGetLocalPlayer(player.clientId, out ILocalPlayer localPlayer)) {
            localPlayer.PlayerGameObject.lastestSimPosition = pos;
            localPlayer.PlayerGameObject.lastAnchoredState = player.anchoredClient != 255;
        }

        translate.value = pos;
        velocity.value = vel;
    }

    public static void RunEffects (ref Entity playerEntity) {
        if(NetAssist.IsServer) {
            ref var player = ref playerEntity.Get<PlayerComponent>();
            ref var playerEffects = ref playerEntity.Get<PlayerEffects>();
            playerEffects.UpdateTimers((byte)player.clientId);
        }
    }
}
