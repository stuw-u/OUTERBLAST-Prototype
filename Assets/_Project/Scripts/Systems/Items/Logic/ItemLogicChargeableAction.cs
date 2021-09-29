using Blast.ECS;
using MLAPI.Serialization;
using UnityEngine;


public class ItemLogicChargeableAction_Data : BaseItem_Data {

    public short chargedFrame;
    public short postUsageFrameRemaining;

    public override void Serialize (NetworkWriter writer) {
        writer.WriteInt16(chargedFrame);
        writer.WriteInt16(postUsageFrameRemaining);
    }
    public override void Deserialize (NetworkReader reader) {
        chargedFrame = reader.ReadInt16();
        postUsageFrameRemaining = reader.ReadInt16();
    }
}


public class ItemLogicChargeableAction : BaseItem {

    // Parameters
    public short maxChargingFrames;
    public short cooldown;


    public sealed override BaseItem_Data GetEmptyData () {
        return new ItemLogicChargeableAction_Data();
    }

    
    public sealed override void OnUpdate (ref Entity entity, BaseItem_Data data, bool isHeld) {
        ItemLogicChargeableAction_Data castedData = (ItemLogicChargeableAction_Data)data;

        // If the cooldown isn't done yet, reduce it and end it here.
        if(castedData.postUsageFrameRemaining > 0) {
            castedData.postUsageFrameRemaining--;
            return;
        }

        // If the item is held, and the button is pressed, start charging the item
        ref var inputCtrl = ref entity.Get<InputControlledComponent>();
        if(isHeld && inputCtrl.inputSnapshot.GetButton((byte)itemMapping)) {
            if(castedData.chargedFrame < maxChargingFrames) {
                castedData.chargedFrame++;
            }

        // If the item was being charged, and it no longer is, unleash the attack (or omly reset the cooldown if the item is no longer held)
        } else if(castedData.chargedFrame > 0) {
            if(isHeld)
                OnActivated(ref entity, castedData.chargedFrame / (float)maxChargingFrames);
            castedData.chargedFrame = 0;
            castedData.postUsageFrameRemaining = cooldown;
        }
    }


    // Unused
    public sealed override void OnUpdateClient (ref Entity entity, BaseItem_Data data, bool isHeld, bool isUsed) {}


    // The item is concidered as "Used" for as long as the item will be charged
    public sealed override bool IsUsed (ref Entity entity, BaseItem_Data data, bool isHeld) => ((ItemLogicChargeableAction_Data)data).chargedFrame > 0;


    // The indicator shows how much % is remaining from the cooldown -OR- how much it is actually charged
    public sealed override float GetIndicatorValue (ref Entity entity, BaseItem_Data data, bool isHeld) {
        ItemLogicChargeableAction_Data castedData = (ItemLogicChargeableAction_Data)data;

        if(castedData.postUsageFrameRemaining > 0) {
            return castedData.postUsageFrameRemaining / (float)cooldown;
        } else {
            return castedData.chargedFrame / (float)maxChargingFrames;
        }      
    }


    // This function will be called when the item is released
    public virtual void OnActivated (ref Entity entity, float chargeValue) { }
}
