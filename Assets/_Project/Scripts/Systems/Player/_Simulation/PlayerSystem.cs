using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;
using MLAPI.Serialization;
using Blast.ECS;
using Blast.ECS.DefaultComponents;
using UnityEngine;

public class PlayerSystem : IECSRunSystem {
    
    private readonly EcsFilter<PlayerComponent> players = null;
    private PlayerParametersAsset parametersAsset;
    public Dictionary<ulong, Entity> playerEntities { get; private set; }
    public Dictionary<ulong, LocalItemManager> itemManagers { get; private set; }

    public PlayerSystem (PlayerParametersAsset parametersAsset) {
        this.parametersAsset = parametersAsset;

        playerEntities = new Dictionary<ulong, Entity>();
        itemManagers = new Dictionary<ulong, LocalItemManager>();
    }
    
    public void Run () {
        
        CameraParametersData cameraParam = new CameraParametersData().DataFromParameters(parametersAsset.cameraParameters); // Create with data TODO
        MovementParametersData movementParam = new MovementParametersData().DataFromParameters(parametersAsset.movementParameters);
        MovementPhysicsParametersData movementPhysicsParam = new MovementPhysicsParametersData().DataFromParameters(parametersAsset.physicsParameters);
        float deltaTime = Time.fixedDeltaTime;

        RunOnAllPlayers((playerEntity) => {
            PlayerLogic.Run(ref playerEntity, ref cameraParam, ref movementParam, ref movementPhysicsParam, deltaTime);
            PlayerLogic.RunEffects(ref playerEntity);
        });
    }
    
    public void RunItems () {
        if(LobbyWorldInterface.inst.LocalLobbyState != LocalLobbyState.InGame) {
            return;
        }
        foreach(KeyValuePair<ulong, LocalItemManager> kvp in itemManagers) {
            if(playerEntities.TryGetValue(kvp.Key, out Entity value)) {
                kvp.Value.Update(ref value);
            }
        }
    }

    public delegate void PlayerGetter (Entity entity);
    public void RunOnAllPlayers (PlayerGetter getter) {
        foreach(var entityIndex in players) {
            ref var playerEntity = ref players.GetEntity(entityIndex);

            getter(playerEntity);
        }
    }

    public int PlayerCount { get { return players.GetEntitiesCount(); } }

    #region Spawning
    public void SpawnPlayer (ulong playerId) {

        bool isClientReplica = !NetAssist.IsServer && NetAssist.ClientID != playerId;

        // Find spawn position (split a circle for n players, gives a point corresponding to your order index)
        float3 spawnPos = float3.zero;
        float3 up = math.up();
        quaternion spawnRot = quaternion.AxisAngle(up, 0f);

        if(LobbyManager.inst != null) {
            int playerIndex = LobbyManager.inst.GetClientOrderIndex(playerId);
            int playerCount = LobbyManager.inst.localPlayers.Count;
            float radianSplit = math.PI * 2f * (1f / playerCount);
            spawnPos = new float3(
                math.cos(radianSplit * playerIndex) * GameManager.inst.spawnRadius, 
                0f,
                math.sin(radianSplit * playerIndex) * GameManager.inst.spawnRadius
            );
            
            if(LobbyWorldInterface.inst.terrainType.gravityFieldType == GravityFieldType.DirectionAligned)
                spawnPos += math.up() * 20f;

            up = -GravitySystem.GetGravityDirection(spawnPos);
            spawnRot = quaternion.AxisAngle(up, 0f);

            // Carve spawn platform
            SimulationManager.inst.SummonTerrainEffect(2, spawnPos - up * 2f);
            SimulationManager.inst.SummonTerrainEffect(0, spawnPos + up * 0.5f);
            SimulationManager.inst.SummonTerrainEffect(0, spawnPos + up * 0.5f);
        }


        // Create the entity and add all required components
        Entity playerEntity = SimulationManager.inst.simulationWorld.NewEntity();
        playerEntity.Get<PlayerComponent>() = new PlayerComponent() {
            clientId = (byte)playerId,
            behaviour = (PlayerEntityBehaviour)math.select(
                (int)PlayerEntityBehaviour.Simulated,
                (int)PlayerEntityBehaviour.ReplicaSimulated,
                isClientReplica),
            anchoredClient = 255
        };
        playerEntity.Get<Position>() = new Position() { value = spawnPos };
        playerEntity.Get<Rotation>() = new Rotation() { value = spawnRot };
        playerEntity.Get<Velocity>();
        playerEntity.Get<LocalToWorld>() = new LocalToWorld() { rotationValue = float4x4.TRS(float3.zero, spawnRot, math.float3(1f, 1f, 1f)) };
        playerEntity.Get<AffectedByGravity>();
        playerEntity.Get<AlignToGravity>();
        playerEntity.Get<InterpolatedPositionEntity>() = new InterpolatedPositionEntity() {
            lerpPosition = spawnPos,
            oldPosition = spawnPos,
            offsetLerpSmooth = 0.2f,
            offsetLerpSpeed = 50f
        };
        playerEntity.Get<InterpolatedRotationEntity>() = new InterpolatedRotationEntity() { offsetSlerpSmooth = 0.2f, offsetSlerpSpeed = 50f, slerpRotation = spawnRot, oldRotation = spawnRot};
        playerEntity.Get<InputControlledComponent>();
        playerEntity.Get<ItemUserComponent>();
        if(LobbyManager.inst != null) {
            if(LobbyManager.inst.matchRulesInfo.scoreMode == ScoreMode.Bounty) {
                playerEntity.Get<TaggableBounty>() = new TaggableBounty() { value = TaggingSystem.defaultBountyValue };
            } else if(LobbyManager.inst.matchRulesInfo.scoreMode == ScoreMode.Stocks) {
                playerEntity.Get<TaggableBounty>() = new TaggableBounty() { value = LobbyManager.inst.matchRulesInfo.stocks };
            }
        }
        playerEntities.Add(playerId, playerEntity);
        itemManagers.Add(playerId, new LocalItemManager(LobbyWorldInterface.GetLocalPlayer(playerId).FixedInventoryIDs, playerId));

        if(playerId == NetAssist.ClientID && !NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) {
            ItemUIManager.inst.ownerItemManager = itemManagers[playerId];
        }
    }

    public void DespawnPlayer (ulong playerId) {
        if(!SimulationManager.inst.simulationWorld.IsAlive()) {
            return;
        }

        if(playerEntities.TryGetValue(playerId, out Entity playerEntity)) {
            playerEntity.Destroy();
            playerEntities.Remove(playerId);
        }
    }
    #endregion
}

