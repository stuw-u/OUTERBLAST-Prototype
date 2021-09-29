using MLAPI.Serialization;
using UnityEngine;
using Blast.ECS;


public enum InventorySlot {
    Bar0,
    Bar1,
    Bar2,
    Shield,
    Recovery,
    Inventory
}


/// <summary>
/// The base class all item data should be inherting from.
/// This stores all the data needed for a certain item type to process the item logic.
/// It must be serialiazable for allow the simulation to restore frame and to re-sync up with the server.
/// </summary>
public abstract class BaseItem_Data {

    // Don't serialize a value if you don't need it for any logic, e.g.: indicator values, etc.
    public virtual void Serialize (NetworkWriter writer) { }
    public virtual void Deserialize (NetworkReader reader) { }
}


/// <summary>
/// The base class all item logic should be inherting from. 
/// This class never stores any data itself, it only stores parameters and systems.
/// </summary>
public abstract class BaseItem : ScriptableObject {

    [Header("Parameters")]
    new public string name;
    public Sprite icon;
    public Buttons itemMapping;
    public InventorySlot inventorySlot;
    public bool serializeOnNetwork;
    public ItemVisualPrefabAsset visuals;
    [HideInInspector] public int id;
    

    // The data this item class uses
    public virtual BaseItem_Data GetEmptyData () { return null; }


    // The update function called by the local item manager, every frame, for the local player and on the server
    public virtual void OnUpdate (ref Entity entity, BaseItem_Data data, bool isHeld) { }


    // The update function called by the local item manager, every frame, on clients for other clients that aren't themselves
    public virtual void OnUpdateClient (ref Entity entity, BaseItem_Data data, bool isHeld, bool isUsed) { }


    // Returns true if the item is actively being used
    public virtual bool IsUsed (ref Entity entity, BaseItem_Data data, bool isHeld) { return false; }
    

    // Returns true if the item should be used (needed for recovery selection, etc.)
    public virtual bool DoRequestUsage (ref Entity entity, BaseItem_Data data) { return false; }


    // The value that should be display on the game UI indicators
    public virtual float GetIndicatorValue (ref Entity entity, BaseItem_Data data, bool isHeld) { return 0f; }
}
