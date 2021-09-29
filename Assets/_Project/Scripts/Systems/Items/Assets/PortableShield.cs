using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using MLAPI.Serialization;
using Blast.ECS;
using Blast.ECS.DefaultComponents;

public struct PortableShieldUser : IDamagable, ITogglableState {
    public bool isEnabled;
    public short storedDamage;

    public short GetClearedDamage () {
        short damage = storedDamage;
        storedDamage = 0;
        return damage;
    }

    public void SetState (bool isEnabled) {
        this.isEnabled = isEnabled;
    }
}

[CreateAssetMenu(fileName = "PortableShield", menuName = "Custom/Item/Portable Shield")]
public class PortableShield : ItemLogicPortableShield<PortableShieldUser> {
    public const float angleConstant = 0.3f;
}
