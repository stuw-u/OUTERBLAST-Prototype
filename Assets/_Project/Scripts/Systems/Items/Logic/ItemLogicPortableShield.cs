using Blast.ECS;
using MLAPI.Serialization;
using UnityEngine;
using Unity.Mathematics;

public interface IShatterableItemVisual {
    float GetShatterValue (BaseItem_Data data);
}

public interface IDamagable {
    short GetClearedDamage ();
}

public interface ITogglableState {
    void SetState (bool isEnabled);
}



public class ItemLogicPortableShield_Data : BaseItem_Data {
    
    public short damage;
    public byte usageTimer;
    public byte penalityTimer;

    public override void Serialize (NetworkWriter writer) {
        writer.WriteInt16(damage);
        writer.WriteByte(usageTimer);
        writer.WriteByte(penalityTimer);
    }
    public override void Deserialize (NetworkReader reader) {
        //Debug.Log($"[Pre] Frame: {SimulationManager.inst.currentFrame}, Damage: {damage}");
        damage = reader.ReadInt16();
        usageTimer = (byte)reader.ReadByte();
        penalityTimer = (byte)reader.ReadByte();
        //Debug.Log($"[Post] Frame: {SimulationManager.inst.currentFrame}, Damage: {damage}");
    }
}


public class ItemLogicPortableShield<T> : BaseItem, IShatterableItemVisual where T : struct, IDamagable, ITogglableState {

    // Parameters
    public short maxDamageResistance;
    public byte usageCooldown;
    public byte penalityCooldown;


    public sealed override BaseItem_Data GetEmptyData () {
        return new ItemLogicPortableShield_Data();
    }


    public sealed override void OnUpdate (ref Entity entity, BaseItem_Data data, bool isHeld) {
        ref var inputCtrl = ref entity.Get<InputControlledComponent>();
        OnUpdateUniversal(ref entity, data, isHeld, inputCtrl.inputSnapshot.GetButton((byte)itemMapping), false);
    }

    
    public sealed override void OnUpdateClient (ref Entity entity, BaseItem_Data data, bool isHeld, bool isUsed) {
        // Do not care about OTHER clients
    }

    private void OnUpdateUniversal (ref Entity entity, BaseItem_Data data, bool isHeld, bool isUsed, bool isClient) {
        ItemLogicPortableShield_Data castedData = (ItemLogicPortableShield_Data)data;

        // Has an activation cooldown,
        // Has a damage limit, which increases when the shield is damaged or gradually when used
        // Has a penality for reaching max usage/damage that prevents it from recharging imediately

        ref var inputCtrl = ref entity.Get<InputControlledComponent>();
        ref var player = ref entity.Get<PlayerComponent>();
        ref var position = ref entity.Get<Blast.ECS.DefaultComponents.Position>();
        ref var shieldUser = ref entity.Get<PortableShieldUser>();

        shieldUser.isEnabled = false;
        if(isHeld && isUsed && castedData.damage < maxDamageResistance) {
            if(castedData.usageTimer == usageCooldown) {
                shieldUser.isEnabled = true;
                short clearedDamage = shieldUser.GetClearedDamage();
                castedData.damage = (short)math.min(castedData.damage + clearedDamage + 1, maxDamageResistance);

                if(castedData.damage == maxDamageResistance) {
                    AudioManager.PlayClientEnvironmentSoundAt((int)player.clientId, position.value, EnvironmentSound.ShieldBreak);
                    castedData.penalityTimer = penalityCooldown;
                } else if(clearedDamage > 0) {
                    AudioManager.PlayClientEnvironmentSoundAt((int)player.clientId, position.value, EnvironmentSound.ShieldHit);
                }
            } else {
                castedData.usageTimer++;
            }
        } else {
            castedData.usageTimer = 0;
            if(castedData.penalityTimer > 0) {
                castedData.penalityTimer--;
            } else if(castedData.damage > 0) {
                castedData.damage--;
            }
        }
        /*if(isHeld && isUsed && castedData.damage < maxDamageResistance) {
            castedData.damage++;
        } else {
            if(castedData.damage > 0) {
                castedData.damage--;
            }
        }*/
    }
    

    // The item is concidered as "Used" for as long as the item will be charged
    public sealed override bool IsUsed (ref Entity entity, BaseItem_Data data, bool isHeld) => ((ItemLogicPortableShield_Data)data).usageTimer == usageCooldown;


    // Should return true to pull out the item
    public sealed override bool DoRequestUsage (ref Entity entity, BaseItem_Data data) {
        ItemLogicPortableShield_Data castedData = (ItemLogicPortableShield_Data)data;
        ref var inputCtrl = ref entity.Get<InputControlledComponent>();
        return inputCtrl.inputSnapshot.GetButton((byte)itemMapping) && castedData.damage < maxDamageResistance;
    }


    // The indicator shows how much % of the max allowed usage frame is remaining
    public sealed override float GetIndicatorValue (ref Entity entity, BaseItem_Data data, bool isHeld) =>
        1f - (((ItemLogicPortableShield_Data)data).damage / (float)maxDamageResistance);

    public float GetShatterValue (BaseItem_Data data) =>
        ((ItemLogicPortableShield_Data)data).damage / (float)maxDamageResistance;

}
