using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TerrainTypeCollection", menuName = "Custom/Terrain/Terrain Type Collection", order = -2)]
public class TerrainTypeCollection : ScriptableObject {
    public TerrainTypeAsset[] terrainTypes;

    public int GetRandom () {
        List<int> weightedTerrainTypes = new List<int>();
        int totalWeight = 0;
        for(int i = 0; i < terrainTypes.Length; i++) {
            int weight = terrainTypes[i].GetRarityWeight();
            totalWeight += weight;
            weightedTerrainTypes.Add(totalWeight);
        }

        int randomWeightValue = Random.Range(0, totalWeight);
        int typeIndex = 0;
        for(int i = 0; i < terrainTypes.Length; i++) {
            typeIndex = i;
            if(randomWeightValue < weightedTerrainTypes[i]) {
                break;
            }
        }
        return typeIndex;
    }
}