using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Blast.ECS;
using Blast.ECS.DefaultComponents;
using KinematicCharacterController;


//RigidbodyInteractionType : Always "SimulatedDynamic"
//StepHandlingMethod : Always "Extra"
//InteractiveRigidbodyHandling : Always "false"


/// <summary>
/// Component that manages character collisions and movement solving
/// </summary>
[RequireComponent(typeof(CapsuleCollider))]
public class KinematicCharacterSystem : IECSRunSystem {


    private readonly EcsFilter<PlayerComponent> players = null;
    public Dictionary<ulong, PlayerSimulationLink> playerSimulationLinks;

    public void AddPlayer (ulong key, Entity playerEntity) {
        PlayerSimulationLink player = Object.Instantiate(AssetsManager.inst.playerSimulationLinkPrefab);
        player.Init(playerEntity);
        playerSimulationLinks.Add(key, player);
    }

    public void RemovePlayer (ulong key) {
        if(!playerSimulationLinks.ContainsKey(key)) {
            return;
        }
        if(playerSimulationLinks[key] != null) {
            Object.Destroy(playerSimulationLinks[key]);
        }
        playerSimulationLinks.Remove(key);
    }

    public KinematicCharacterSystem () {
        playerSimulationLinks = new Dictionary<ulong, PlayerSimulationLink>();
    }
    

    public void Run () {

        float deltaTime = Time.fixedDeltaTime;
        foreach(var entityIndex in players) {

            // Get actual entity reference
            ref var playerEntity = ref players.GetEntity(entityIndex);
            ref var playerComponent = ref playerEntity.Get<PlayerComponent>();
            if(!playerSimulationLinks.TryGetValue(playerComponent.clientId, out PlayerSimulationLink simLink))
                return;

            ref var position = ref playerEntity.Get<Position>();
            ref var rotation = ref playerEntity.Get<Rotation>();
            ref var velocity = ref playerEntity.Get<Velocity>();
            ref var localToWorld = ref playerEntity.Get<LocalToWorld>();

            KinematicCharacterMotorState state = playerComponent.motorState;
            state.Position = position.value;
            state.BaseVelocity = velocity.value;
            state.Rotation = rotation.value;
            float bounciness = math.lerp(0f, 0.7f, playerComponent.sliperyTimer);
            simLink.UpdatePhase1(state, deltaTime, bounciness);
            state = simLink.UpdatePhase2(deltaTime);
            playerComponent.motorState = state;

            float3 velDifference = math.transform(math.inverse(localToWorld.rotationValue), (float3)state.BaseVelocity - velocity.value);
            velDifference.y = math.min(0f, velDifference.y);
            if(!SimulationManager.inst.IsReplayingFrame) {
                float velChangeMagnitude = math.length(velDifference);
                if(velChangeMagnitude > 15f) {
                    AudioManager.PlayEnvironmentSoundAt(state.Position, EnvironmentSound.PlayerBounce, math.saturate(math.unlerp(15f, 40f, velChangeMagnitude)));
                }
            }

            position.value = state.Position;
            velocity.value = state.BaseVelocity;
            rotation.value = state.Rotation;


        }
    }
}