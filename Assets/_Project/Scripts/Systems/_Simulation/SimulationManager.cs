using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Blast.Collections;
using Blast.ECS;
using Blast.ECS.DefaultComponents;
using System.Collections.Generic;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using Blast.NetworkedEntities;

public class SimulationManager : System.IDisposable {

    public static SimulationManager inst;

    public int currentFrame { private set; get; }
    private int framesStartIndex;
    public List<FrameData> frames;
    public const int MaxFrames = 300;
    public Dictionary<ulong, ILocalPlayer> localPlayers { private set; get; }
    public bool IsReplayingFrame { private set; get; } = false;

    public LimitedQueue<TerrainEffectData> recentTerrainEffect;
    public LimitedQueue<TerrainEffectData> receivedTerrainUpdateCallback;

    public static float InterpolationFactor {
        get {
            if(inst != null) {
                return inst.interpolationFactor;
            } else {
                return 0f;
            }
        }
    }
    private float interpolationFactor;
    private float oldInterpolationTime;
    private float newInterpolationTime;

    #region ECS World and Systems
    public World simulationWorld;
    public SystemGroup systems;

    public GravitySystem gravitySystem;
    public SpatialHasherSystem spatialHasherSystem;
    public PlayerSystem playerSystem;
    public KinematicCharacterSystem kinematicCharacterSystem;
   //public ProjectileSystem projectileSystem;
    public ProjectileReplica projectileReplica;
    public ItemCrateEntities itemCrateEntities;
    public MineEntities mineEntities;
    public LocalToWorldSystem localToWorldSystem;
    public InterpolationSystem interpolationSystem;
    public BlastSystem explosionSystem;
    public TaggingSystem taggingSystem;
    #endregion
    
    public SimulationManager () {

        NetworkedEntitySystems.ClearSystems();

        inst = this;
        frames = new List<FrameData>(MaxFrames);
        localPlayers = new Dictionary<ulong, ILocalPlayer>();
        recentTerrainEffect = new LimitedQueue<TerrainEffectData>(64);
        receivedTerrainUpdateCallback = new LimitedQueue<TerrainEffectData>(64);

        #region Setup Simulation
        localToWorldSystem = new LocalToWorldSystem();
        gravitySystem = new GravitySystem();
        spatialHasherSystem = new SpatialHasherSystem();
        playerSystem = new PlayerSystem(GameManager.inst.playerParametersAsset);
        kinematicCharacterSystem = new KinematicCharacterSystem();
        //projectileSystem = new ProjectileSystem(64);
        projectileReplica = new ProjectileReplica();
        itemCrateEntities = new ItemCrateEntities();
        mineEntities = new MineEntities();

        interpolationSystem = new InterpolationSystem();
        explosionSystem = new BlastSystem();
        taggingSystem = new TaggingSystem();

        simulationWorld = new World();
        systems = new SystemGroup(simulationWorld)
            .Add(interpolationSystem)
            .Add(localToWorldSystem)
            .Add(spatialHasherSystem)
            .Add(gravitySystem)
            .Add(kinematicCharacterSystem)
            .Add(playerSystem)
            //.Add(projectileSystem)
            .Add(explosionSystem)
            .Add(taggingSystem);
        systems.Init();
        NetworkedEntitySystems.AddSystem(projectileReplica);
        //NetworkedEntitySystems.AddSystem(itemCrateEntities);
        //NetworkedEntitySystems.AddSystem(mineEntities);
        #endregion
    }




    #region Simulation (Fixed, Update)
    /// <summary>
    /// Simulates the whole world by a frame.
    /// </summary>
    public void Simulate (float deltaTime, InputSnapshot clientInputs, byte previousRawInputs) {

        // Apply inputs
        foreach(KeyValuePair<ulong, ILocalPlayer> kvp in localPlayers) {
            Entity playerEntity = playerSystem.playerEntities[kvp.Key];

            if(kvp.Value.PlayerControlType == PlayerControlType.Self) {
                playerEntity.Get<InputControlledComponent>().inputFrame = currentFrame;
                playerEntity.Get<InputControlledComponent>().lastButtonRaw = previousRawInputs;
                playerEntity.Get<InputControlledComponent>().inputSnapshot = clientInputs;

            } else if(NetAssist.IsServer) {
                localPlayers[kvp.Key].Inputs_UpdateOnServer();
                int recentInputIndex = localPlayers[kvp.Key].Inputs_RecentInputIndex;
                InputSnapshot noneLocalClientInputs = localPlayers[kvp.Key].Inputs_RecentInput;
                InputSnapshot lastNoneLocalClientInputs = localPlayers[kvp.Key].Inputs_PreviousInput;
                localPlayers[kvp.Key].PlayerGameObject.SetInterpolatedAxis(noneLocalClientInputs.lookAxis);

                playerEntity.Get<InputControlledComponent>().inputFrame = recentInputIndex;
                playerEntity.Get<InputControlledComponent>().lastButtonRaw = lastNoneLocalClientInputs.GetButtonRaw();
                playerEntity.Get<InputControlledComponent>().inputSnapshot = noneLocalClientInputs;
            }
        }

        // Run and record simulation
        systems.Run();
        NetworkedEntitySystems.UpdateSimulation(deltaTime);
        spatialHasherSystem.Clear();
        playerSystem.RunItems();
        Physics.SyncTransforms();
        RecordNewFrameData(clientInputs);
    }

    /// <summary>
    /// Applies interpolation and applies values to gameObjects
    /// </summary>
    public void SimulateUpdate () {
        interpolationFactor = (Time.time - newInterpolationTime) / (newInterpolationTime - oldInterpolationTime);

        interpolationSystem.Apply();
        //projectileSystem.ApplyComponentsOnGameObject();
        NetworkedEntitySystems.UpdateAllVisuals();

        foreach(KeyValuePair<ulong, ILocalPlayer> kvp in localPlayers) {
            Entity playerEntity = playerSystem.playerEntities[kvp.Key];
            ref var input = ref playerEntity.Get<InputControlledComponent>();
            
            kvp.Value.PlayerGameObject.ApplyComponents(
                    playerEntity.Get<InterpolatedPositionEntity>().lerpPosition,
                    playerEntity.Get<InterpolatedRotationEntity>().slerpRotation);
            kvp.Value.PlayerGameObject.ApplyItemUser(ref playerEntity.Get<ItemUserComponent>());
        }
    }
    
    /// <summary>
    /// Compares the time between the last fixed update and saves all current/old position needed for the interpolation loop
    /// </summary>
    public void SimulateFixedInterpolationUpdate () {

        // Run system
        interpolationSystem.Run();

        // Prepare interpolation time
        oldInterpolationTime = newInterpolationTime;
        newInterpolationTime = Time.fixedTime;
    }
    #endregion




    #region Frame Management (Compensation, Record, Apply, Replay, Network Serialization)
    public bool GetCompensatedFrame (ulong playerId, float rttValue, out FrameData frame) {
        int compensationFrame = (int)math.floor((NetAssist.GetPlayerRTT(playerId) / 1000f) * rttValue / Time.fixedDeltaTime);
        compensationFrame = math.min(frames.Count - 1, compensationFrame);

        if(frames.Count > 0) {
            frame = frames[frames.Count - 1 - compensationFrame];
            return true;
        } else {
            frame = null;
            return false;
        }
    }

    /// <summary>
    /// Saves the world as it is, NOW, and returns the index of the frame
    /// </summary>
    public void RecordNewFrameData (InputSnapshot clientInputs) {
        
        // Create a next frame if we're advancing forward in time
        if(currentFrame >= framesStartIndex + frames.Count) {

            // Record frame and add to the list
            FrameData frameData = new FrameData(currentFrame);
            frameData.Record(this, clientInputs);

            // Discard old frames
            if(frames.Count >= MaxFrames) {
                frames[0].Dispose();
                frames.RemoveAt(0);
                framesStartIndex++;
            }
            frames.Add(frameData);
        }

        // Else Re-record old frame
        else {
            FrameData frameData = frames[currentFrame - framesStartIndex];

            frameData.Dispose();
            frameData.Record(this, frameData.clientInputs);
        }

        currentFrame++;
    }


    /// <summary>
    /// Changes the world to a previous authoritative state. Frames aren't being deleted, the currentFrame index isn't being updated either
    /// </summary>
    public void ApplyAuthoritativeFrame (int frameIndex) {

        // The index is forward in time. That's stupid
        if(frameIndex > currentFrame) {
            Debug.LogError("Attempting to load future frames.");
            return;
        }

        // The index refers to a discared frame. That's gonna cause some issues
        if(currentFrame - frames.Count > frameIndex) {
            Debug.LogError("Attempting to load disposed frames.");
            return;
        }

        // Apply the frame data
        int rewindFrames = currentFrame - frameIndex;
        FrameData frameData = frames[(int)math.clamp(frames.Count - rewindFrames, 0, frames.Count - 1)];
        PlayerSerializer.DeserializeFrame(playerSystem, frameData.playersData);
        using(PooledNetworkReader reader = PooledNetworkReader.Get(frameData.itemEntityData)) {
            frameData.itemEntityData.Position = 0;
            frameData.itemEntityData.BitPosition = 0;
            PlayerSerializer.DeserializeItem(playerSystem, reader);
            NetworkedEntitySystems.DeserializeAllEntitiesFrame(reader, frameIndex);
        }
        //projectileSystem.DeserializeFrame(frameIndex, frameData.projectilesData);
    }


    /// <summary>
    /// Replays the simulation from a saved frame
    /// </summary>
    private int lastProcessedRewindFrame = 0;
    public delegate void SetState ();
    public void ReplayFromFrame (int rewindFromFrameIndex, SetState setState, float deltaTime) {

        float3 preSelfPos = playerSystem.playerEntities[NetAssist.ClientID].Get<Position>().value;
        bool doApplyFrame = true;

        // The index is forward in time. That's stupid
        if(rewindFromFrameIndex - 1 > currentFrame) {
            doApplyFrame = false;
        }

        // The index refers to a discared frame. That's gonna cause some issues
        if(currentFrame - frames.Count > rewindFromFrameIndex - 1) {
            doApplyFrame = false;
        }
        
        // Set world back in time
        if(doApplyFrame) {
            ApplyAuthoritativeFrame(rewindFromFrameIndex - 1);
        }

        // Overrides currentFrame, finds new frame index, simulates forward.
        IsReplayingFrame = true;
        int currentFrameTarget = currentFrame;
        int frameIndex = frames.Count + (rewindFromFrameIndex - currentFrame);
        int iterations = 0;
        currentFrame = rewindFromFrameIndex;
        for(; currentFrame < currentFrameTarget;) {
            byte previousRawInputs = frames[frameIndex].clientInputs.GetButtonRaw();
            if(frameIndex - 1 >= 0) {
                previousRawInputs = frames[frameIndex - 1].clientInputs.GetButtonRaw();
            }

            setState();
            Simulate(deltaTime, frames[frameIndex].clientInputs, previousRawInputs);

            frameIndex++;
            iterations++;
        }
        IsReplayingFrame = false;

        float3 currentSelfPos = playerSystem.playerEntities[NetAssist.ClientID].Get<Position>().value;

        if(math.any(preSelfPos != currentSelfPos)) {
            //Debug.Log($"#{currentFrame}, globalDiff: {currentSelfPos - preSelfPos}, authDiff: {postReceivedAuthSelfPos - postAuthSelfPos}, auth1 {postAuthSelfPos}, auth2 {postReceivedAuthSelfPos}");
        }
    }
    #endregion




    #region Frame Network Serialization
    public void SerializeCustomWorldToStream (ulong playerId, NetworkWriter writer) {
        PlayerSerializer.SerializeSelfPlayer(playerSystem, writer, playerId); // Self Player Data
    }

    public void SerializeWorldToStream (NetworkWriter writer) {
        PlayerSerializer.SerializeNetwork(playerSystem, writer); // Ennemy Players
        //projectileSystem.SerializeNetwork(writer); // Projectiles
        NetworkedEntitySystems.SerializeAllEntitiesNetwork(writer);
    }
    
    public void DeserializeFrameFromStream (ulong playerId, int oldestRewindFrame, float localReceivingTime, /*List<PooledBitStream> worldStates*/PooledNetworkBuffer worldState, /*List<int>*/int worldStateIndex) {

        // Prepare interpolation stuff
        interpolationSystem.CopyPreOffset();

        int streamIndex = 0;
        lastProcessedRewindFrame = oldestRewindFrame;
        ReplayFromFrame(oldestRewindFrame, () => {
            /*if(streamIndex >= worldStateIndex.Count)
                return;*/
            if(/*worldStateIndex[streamIndex]*/worldStateIndex == currentFrame) {
                using(PooledNetworkReader worldReader = PooledNetworkReader.Get(/*worldStates[streamIndex]*/worldState)) {

                    // Self Player Data
                    PlayerSerializer.DeserializeSelfPlayer(playerSystem, worldReader, playerId);
                    worldReader.SkipPadBits();

                    // Ennemy Players
                    PlayerSerializer.DeserializeNetwork(playerSystem, localReceivingTime, worldReader);

                    // Projectiles
                    //projectileSystem.DeserializeNetwork(worldReader);
                    NetworkedEntitySystems.DeserializeAllEntitiesNetwork(worldReader);
                }
                streamIndex++;
            }


        }, Time.fixedDeltaTime);

        // Calculate offsets
        interpolationSystem.ApplyCalculatedOffset();
    }
    
    public bool ValidateWorldState (int receivedRewindFrame) {
        return (receivedRewindFrame > lastProcessedRewindFrame);
    }
    #endregion




    #region Player Management (Player Add, Player Remove)
    /// <summary>
    /// Adds a local player to the player bank
    /// </summary>
    public static void AddPlayer (ILocalPlayer player) {
        if(inst == null) {
            Debug.Log("No entity store has been created");
            return;
        }
        inst.localPlayers.Add(player.ClientID, player);
        inst.playerSystem.SpawnPlayer(player.ClientID);
        inst.kinematicCharacterSystem.AddPlayer(player.ClientID, inst.playerSystem.playerEntities[player.ClientID]);
        if(PlayerIndicatorDisplay.inst != null)
            PlayerIndicatorDisplay.inst.AddIndicator(player.ClientID);
    }


    /// <summary>
    /// Removes a local player from the player bank
    /// </summary>
    public static void RemovePlayer (ulong playerId) {
        if(inst == null) {
            return;
        }

        inst.localPlayers.Remove(playerId);
        inst.playerSystem.DespawnPlayer(playerId);
        inst.kinematicCharacterSystem.RemovePlayer(playerId);
        if(PlayerIndicatorDisplay.inst != null)
            PlayerIndicatorDisplay.inst.RemoveIndicator(playerId);
    }


    public static void FindSpawnPositionPlayer (ulong playerId) {

    }
    #endregion




    #region Terrain Effect Management
    public void SummonTerrainEffect (byte type, float3 position, int claimant = -1) {

        if(NetAssist.IsServer) {

            // If we want to summon an explosion on the server, we must assure the other clients will recieve this
            // terrain effect reliably, else they won't fall were they should be.
            TerrainEffectData newTerrainEffect = TerrainEffectData.ConvertValueToState(type, position);
            recentTerrainEffect.Enqueue(newTerrainEffect);

            switch(type) {
                case 0:
                ExplosionManager.TriggerExplosionAt(newTerrainEffect.position, ExplosionType.Normal, claimant);
                break;
                case 1:
                ExplosionManager.TriggerRockBreakAt(newTerrainEffect.position);
                break;
                case 2:
                ExplosionManager.TriggerSpawnCarve(newTerrainEffect.position);
                break;
            }

        } else {

            // We want predicted explosion visuals to be executed only when playing forward, not when replaying frames.
            // This is already being taken care of in the explosion manager
            TerrainEffectData newTerrainEffect = TerrainEffectData.ConvertValueToState(type, position);
            switch(type) {
                case 0:
                ExplosionManager.TriggerExplosionAt(newTerrainEffect.position, ExplosionType.Predicted, claimant);
                break;
                case 1:
                break;
                case 2:
                break;
            }

        }
    }

    public void ApplyCallbackTerrainEffect (TerrainEffectData terrainEffect) {
        switch(terrainEffect.type) {
            case 0:
            ExplosionManager.TriggerExplosionAt(terrainEffect.position, ExplosionType.ServerCallback);
            break;
            case 1:
            ExplosionManager.TriggerRockBreakAt(terrainEffect.position);
            break;
            case 2:
            ExplosionManager.TriggerSpawnCarve(terrainEffect.position);
            break;
        }
    }

    public bool DoSendTerrainUpdateMessage () {
        return recentTerrainEffect.Count > 0;
    }

    public void SerializeTerrainUpdateToStream (NetworkWriter writer) {
        writer.WriteByte((byte)recentTerrainEffect.Count);
        while(recentTerrainEffect.TryDequeue(out TerrainEffectData item)) {
            item.WriteToStream(writer);
        }
    }

    public void DeserializeTerrainUpdateFromStream (NetworkReader reader) {
        int effectCount = reader.ReadByte();

        for(int i = 0; i < effectCount; i++) {
            TerrainEffectData terrainEffect = TerrainEffectData.ReadFromStream(reader);
            receivedTerrainUpdateCallback.Enqueue(terrainEffect);
        }
    }

    public void ApplyAllReceivedTerrainUpdate () {
        while(receivedTerrainUpdateCallback.TryDequeue(out TerrainEffectData terrainEffect)) {
            ApplyCallbackTerrainEffect(terrainEffect);
        }
    }
    #endregion




    #region Utils
    public static bool TryGetLocalPlayer (ulong key, out ILocalPlayer localPlayer) {
        localPlayer = null;
        if(inst == null) {
            return false;
        }
        if(inst.localPlayers == null) {
            return false;
        }
        return inst.localPlayers.TryGetValue(key, out localPlayer);
    }
    #endregion

    /// <summary>
    /// Disposes all frames
    /// </summary>
    public void Dispose () {
        if(!simulationWorld.IsAlive()) {
            return;
        }

        foreach(FrameData frame in frames) {
            frame.Dispose();
        }
        frames.Clear();

        systems.Dispose();
        simulationWorld.Dispose();
    }
}
