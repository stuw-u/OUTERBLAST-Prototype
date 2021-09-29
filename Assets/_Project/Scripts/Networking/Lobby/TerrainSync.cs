using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Blast.ECS;
using Blast.ECS.DefaultComponents;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Mathematics;
using MLAPI;
using MLAPI.NetworkVariable;
using MLAPI.NetworkVariable.Collections;
using MLAPI.Messaging;
using MLAPI.Serialization.Pooled;
using Eldemarkki.VoxelTerrain.World;


public class TerrainSync : NetworkBehaviour {

    private static TerrainSync inst;
    void Awake () {
        inst = this;

        CustomMessagingManager.RegisterNamedMessageHandler("GetTerrainUpdateClientRPC", GetTerrainUpdateClientRPC_NamedMessage);
    }


    public static bool IsTerrainAtPoint (Vector3 position) {
        int chunkSize = VoxelWorld.inst.WorldSettings.ChunkSize;
        int3 voxelPoint = (int3)math.floor(position);
        if(VoxelWorld.inst.VoxelDataStore.TryGetVoxelData(voxelPoint, 0, out float voxelData)) {
            if(voxelData >= 0.525f) {
                return true;
            }
        }
        return false;
    }

    public static void SendTerrainUpdateToClient () {
        if(!NetAssist.IsServer || !SimulationManager.inst.DoSendTerrainUpdateMessage() || NetAssist.inst.Mode.HasFlag(NetAssistMode.Record))
            return;

        using(PooledNetworkBuffer stream = PooledNetworkBuffer.Get())
        using(PooledNetworkWriter writer = PooledNetworkWriter.Get(stream)) {
            SimulationManager.inst.SerializeTerrainUpdateToStream(writer);
            //inst.InvokeClientRpcOnEveryonePerformance(inst.GetTerrainUpdateClientRPC, stream, "Terrain");
            CustomMessagingManager.SendNamedMessage("GetTerrainUpdateClientRPC", null, stream, MLAPI.Transports.NetworkChannel.ReliableRpc);
        }
    }

    private void GetTerrainUpdateClientRPC_NamedMessage (ulong clientId, Stream stream) {
        if(NetAssist.IsServer || SimulationManager.inst == null)
            return;

        using(PooledNetworkReader reader = PooledNetworkReader.Get(stream))
            SimulationManager.inst.DeserializeTerrainUpdateFromStream(reader);
    }
}
