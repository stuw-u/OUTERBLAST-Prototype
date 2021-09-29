using Blast.ECS;
using MLAPI.Serialization;
using UnityEngine;
using Unity.Mathematics;


public class ItemLogicLimitedHoldRecovery_Data : BaseItem_Data {

    public bool cancelUsage;
    public short usageFrame;
    public short postUsageFrameRemaining;

    public override void Serialize (NetworkWriter writer) {
        writer.WriteBool(cancelUsage);
        writer.WriteInt16(usageFrame);
        writer.WriteInt16(postUsageFrameRemaining);
    }
    public override void Deserialize (NetworkReader reader) {
        cancelUsage = reader.ReadBool();
        usageFrame = reader.ReadInt16();
        postUsageFrameRemaining = reader.ReadInt16();
    }
}


public class ItemLogicLimitedHoldRecovery : BaseItem {

    // Parameters
    public short usagePerTick;
    public short maxUsageTick;
    public short postUsageCooldown;


    public sealed override BaseItem_Data GetEmptyData () {
        return new ItemLogicLimitedHoldRecovery_Data();
    }


    public sealed override void OnUpdate (ref Entity entity, BaseItem_Data data, bool isHeld) {
        ItemLogicLimitedHoldRecovery_Data castedData = (ItemLogicLimitedHoldRecovery_Data)data;
        
        // If the item is held, and the button is pressed, start charging the item
        ref var inputCtrl = ref entity.Get<InputControlledComponent>();
        ref var player = ref entity.Get<PlayerComponent>();

        if(inputCtrl.inputSnapshot.GetButtonDown((byte)itemMapping, inputCtrl.lastButtonRaw)) {
            castedData.cancelUsage = !PlayerUtils.IsFarFromGround(ref entity);
        }
        if(player.isGrounded || player.inAirByJump) {
            castedData.cancelUsage = true;
        }
        if(inputCtrl.inputSnapshot.GetButton((byte)itemMapping) && castedData.usageFrame < maxUsageTick && !castedData.cancelUsage && isHeld) {
            OnActivated(ref entity);
            castedData.usageFrame = (short)math.min(castedData.usageFrame + usagePerTick, maxUsageTick);
            castedData.postUsageFrameRemaining = postUsageCooldown;
            if(castedData.usageFrame == maxUsageTick) {
                castedData.cancelUsage = true;
            }
            return;
        }

        if(castedData.postUsageFrameRemaining > 0) {
            castedData.postUsageFrameRemaining--;
        } else if(castedData.usageFrame > 0) {
            castedData.usageFrame--;
        }
    }


    // The OnActivated methode is also called on clients for their player simulation to be accurate.
    public sealed override void OnUpdateClient (ref Entity entity, BaseItem_Data data, bool isHeld, bool isUsed) {
        if(isHeld && isUsed)
            OnActivated(ref entity);
    }


    // The item is concidered as "Used" for as long as the item will be charged
    public sealed override bool IsUsed (ref Entity entity, BaseItem_Data data, bool isHeld) {
        return ((ItemLogicLimitedHoldRecovery_Data)data).postUsageFrameRemaining == postUsageCooldown && !((ItemLogicLimitedHoldRecovery_Data)data).cancelUsage;
    }


    // Should return true to pull out the item
    public sealed override bool DoRequestUsage (ref Entity entity, BaseItem_Data data) {
        ItemLogicLimitedHoldRecovery_Data castedData = (ItemLogicLimitedHoldRecovery_Data)data;
        ref var inputCtrl = ref entity.Get<InputControlledComponent>();
        ref var player = ref entity.Get<PlayerComponent>();
        
        return !player.isGrounded && !player.inAirByJump && inputCtrl.inputSnapshot.GetButton((byte)itemMapping) && castedData.usageFrame < maxUsageTick && !castedData.cancelUsage;
    }


    // The indicator shows how much % of the max allowed usage frame is remaining
    public sealed override float GetIndicatorValue (ref Entity entity, BaseItem_Data data, bool isHeld) => 1f-(((ItemLogicLimitedHoldRecovery_Data)data).usageFrame / (float)maxUsageTick);


    // This function will be called when the item is used
    public virtual void OnActivated (ref Entity entity) { }

}
