using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TerrainStyleCollection", menuName = "Custom/Terrain/Terrain Style Collection", order = -2)]
public class TerrainStyleCollection : ScriptableObject {
    public TerrainStyleAsset[] terrainStyles;

    public int GetRandom () {
        List<int> weightedTerrainStyles = new List<int>();
        int totalWeight = 0;
        for(int i = 0; i < terrainStyles.Length; i++) {
            int weight = terrainStyles[i].GetRarityWeight();
            totalWeight += weight;
            weightedTerrainStyles.Add(totalWeight);
        }

        int randomWeightValue = Random.Range(0, totalWeight);
        int typeIndex = 0;
        for(int i = 0; i < terrainStyles.Length; i++) {
            typeIndex = i;
            if(randomWeightValue < weightedTerrainStyles[i]) {
                break;
            }
        }
        return typeIndex;
    }
}