using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Blast.ObjectPooling;

[CreateAssetMenu(fileName = "Projectile", menuName = "Custom/Entity/Projectile")]
public class ProjectileAsset : ScriptableObject {
    public short initTime = 1600;
    public float initialSpeed;
    public PoolableObject prefab; 
}
