using Blast.ECS;
using MLAPI.Serialization;
using UnityEngine;

public interface IRemovableItem {
    bool DoRemove ();
}

public class ItemLogicOneUse_Data : BaseItem_Data, IRemovableItem {

    public bool cancelled;

    public override void Serialize (NetworkWriter writer) { writer.WriteBool(cancelled); }
    public override void Deserialize (NetworkReader reader) { cancelled = reader.ReadBool(); }

    public bool DoRemove () {
        return cancelled;
    }
}


public class ItemLogicOneUse : BaseItem {
    
    public sealed override BaseItem_Data GetEmptyData () {
        return new ItemLogicOneUse_Data();
    }


    public sealed override void OnUpdate (ref Entity entity, BaseItem_Data data, bool isHeld) {
        ItemLogicOneUse_Data castedData = (ItemLogicOneUse_Data)data;

        // If the item is cancelled end it here.
        if(castedData.cancelled) {
            return;
        }

        
        ref var inputCtrl = ref entity.Get<InputControlledComponent>();
        ref var player = ref entity.Get<PlayerComponent>();

        // The item will activate only if the button has just been pressed
        if(isHeld && inputCtrl.inputSnapshot.GetButtonDown((byte)itemMapping, inputCtrl.lastButtonRaw)) {
            OnActivated(ref entity);
            castedData.cancelled = true;
        }
    }


    // Unused
    public sealed override void OnUpdateClient (ref Entity entity, BaseItem_Data data, bool isHeld, bool isUsed) { }


    // The action only ever last one frame, let's consider the item as never actually "used"
    public sealed override bool IsUsed (ref Entity entity, BaseItem_Data data, bool isHeld) {
        return false;
    }


    // Should return true to pull out the item
    public sealed override bool DoRequestUsage (ref Entity entity, BaseItem_Data data) {
        ItemLogicOneUse_Data castedData = (ItemLogicOneUse_Data)data;

        ref var inputCtrl = ref entity.Get<InputControlledComponent>();
        return inputCtrl.inputSnapshot.GetButton((byte)itemMapping) && !castedData.cancelled;
    }


    // The indicator shows how much % is remaining from the cooldown
    public sealed override float GetIndicatorValue (ref Entity entity, BaseItem_Data data, bool isHeld) {
        return 0f;
    }


    // This function will be called when the cooldown reach zero and the button is pressed
    public virtual void OnActivated (ref Entity entity) { }


    // Must be overriden by things like grappler hook to not apply an unwanted switch cooldown
    public virtual bool IsUsageConditionMet (ref Entity entity, BaseItem_Data data) { return false; }
}
