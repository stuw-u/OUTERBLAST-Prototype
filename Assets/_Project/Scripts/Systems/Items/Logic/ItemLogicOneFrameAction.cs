using Blast.ECS;
using MLAPI.Serialization;
using UnityEngine;

public enum ActivationType {
    OnButtonDown,
    OnCooldownNull,
    OnButtonDownRecovery
}


public class ItemLogicOneFrameAction_Data : BaseItem_Data {

    public short frameRemaining;

    public override void Serialize (NetworkWriter writer) { writer.WriteInt16(frameRemaining); }
    public override void Deserialize (NetworkReader reader) { frameRemaining = reader.ReadInt16(); }
}


public class ItemLogicOneFrameAction : BaseItem {

    // Parameters
    public ActivationType activationType;
    public short cooldown;


    public sealed override BaseItem_Data GetEmptyData () {
        return new ItemLogicOneFrameAction_Data();
    }


    public sealed override void OnUpdate (ref Entity entity, BaseItem_Data data, bool isHeld) {
        ItemLogicOneFrameAction_Data castedData = (ItemLogicOneFrameAction_Data)data;

        // If the cooldown isn't done yet, reduce it and end it here.
        if(castedData.frameRemaining > 0) {
            castedData.frameRemaining--;
            return;
        }


        // If the item is held, and the button is pressed, activate the item
        ref var inputCtrl = ref entity.Get<InputControlledComponent>();
        ref var player = ref entity.Get<PlayerComponent>();
        if(activationType == ActivationType.OnButtonDown) {

            // The item will activate only if the button has just been pressed
            if(isHeld && inputCtrl.inputSnapshot.GetButtonDown((byte)itemMapping, inputCtrl.lastButtonRaw)) {
                OnActivated(ref entity);
                castedData.frameRemaining = cooldown;
            }

        } else if(activationType == ActivationType.OnCooldownNull) {

            // The item will activate
            if(isHeld && inputCtrl.inputSnapshot.GetButton((byte)itemMapping)) {
                OnActivated(ref entity);
                castedData.frameRemaining = cooldown;
            }

        } else if(activationType == ActivationType.OnButtonDownRecovery) {

            // The item will activate only if the button has just been pressed
            if(inputCtrl.inputSnapshot.GetButtonDown((byte)itemMapping, inputCtrl.lastButtonRaw) && !player.inAirByJump && !player.isGrounded && PlayerUtils.IsSlightlyAboveGround(ref entity)) {
                if(OnActivatedRecovery(ref entity))
                    castedData.frameRemaining = cooldown;
            }

        }
    }


    // Unused
    public sealed override void OnUpdateClient (ref Entity entity, BaseItem_Data data, bool isHeld, bool isUsed) {}


    // The action only ever last one frame, let's consider the item as never actually "used"
    public sealed override bool IsUsed (ref Entity entity, BaseItem_Data data, bool isHeld) {
        if(activationType == ActivationType.OnButtonDownRecovery) {

            ref var inputCtrl = ref entity.Get<InputControlledComponent>();
            ref var player = ref entity.Get<PlayerComponent>();

            return (isHeld && inputCtrl.inputSnapshot.GetButton((byte)itemMapping) && !player.inAirByJump && !player.isGrounded);

        }
        return false;
    }


    // Should return true to pull out the item
    public sealed override bool DoRequestUsage (ref Entity entity, BaseItem_Data data) {
        if(activationType == ActivationType.OnButtonDownRecovery) {

            ref var inputCtrl = ref entity.Get<InputControlledComponent>();
            ref var player = ref entity.Get<PlayerComponent>();

            return inputCtrl.inputSnapshot.GetButton((byte)itemMapping) && !player.inAirByJump && !player.isGrounded && PlayerUtils.IsSlightlyAboveGround(ref entity) && IsUsageConditionMet(ref entity, data);

        }
        return false;
    }


    // The indicator shows how much % is remaining from the cooldown
    public sealed override float GetIndicatorValue (ref Entity entity, BaseItem_Data data, bool isHeld) {
        if(activationType == ActivationType.OnButtonDownRecovery) {
            return 1f - (((ItemLogicOneFrameAction_Data)data).frameRemaining / (float)cooldown);
        } else {
            return ((ItemLogicOneFrameAction_Data)data).frameRemaining / (float)cooldown;
        }
    }


    // This function will be called when the cooldown reach zero and the button is pressed
    public virtual void OnActivated (ref Entity entity) { }


    // Must be overriden by things like grappler hook to not apply an unwanted switch cooldown
    public virtual bool IsUsageConditionMet (ref Entity entity, BaseItem_Data data) { return false; }


    // This function will be called when the cooldown reach zero and the button is pressed
    public virtual bool OnActivatedRecovery (ref Entity entity) { return false; }
}
