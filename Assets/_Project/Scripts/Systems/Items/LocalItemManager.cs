using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;
using MLAPI.Serialization;
using Blast.ECS;
using Blast.ECS.DefaultComponents;
using UnityEngine;

public struct ItemUserComponent {
    public int lastHeldItem;
    public bool lastHeldItemUsed;
}

public struct ItemStateData {
    public int itemID;
    public short cooldown;
    public short value;
    public float chargedForce;
    public float3 pointOfIntrest;

    public bool visualizeAction;
}



public class InventoryItemDescriptor {
    public int assetID;
    public BaseItem_Data data;

    public int frameOfObtention;
    public bool isEnabled;

    public uint itemInstanceId;
}

public class LocalItemManager {
    public ulong owner;
    public List<InventoryItemDescriptor> inventory;
    public InventoryItemDescriptor[] fixedInventory;
    public int[] slotToInventoryIndex;
    private byte heldItemSwitchCooldown;
    private const int heldItemSwitchFrame = 4;

    // Creates inventory and setup data with correct types
    public LocalItemManager (int[] itemIDs, ulong owner) {
        this.owner = owner;
        slotToInventoryIndex = new int[5];
        inventory = new List<InventoryItemDescriptor>();
        fixedInventory = new InventoryItemDescriptor[itemIDs.Length];
        for(int i = 0; i < itemIDs.Length; i++) {
            fixedInventory[i] = new InventoryItemDescriptor();
            fixedInventory[i].assetID = itemIDs[i];
            fixedInventory[i].data = AssetsManager.inst.itemAssets.items[itemIDs[i]].GetEmptyData();
            slotToInventoryIndex[(int)AssetsManager.inst.itemAssets.items[itemIDs[i]].inventorySlot] = i;
        }
        
        /*inventory.Add(new InventoryItemDescriptor() {
            assetID = 10,
            data = AssetsManager.inst.itemAssets.items[10].GetEmptyData(),
            frameOfObtention = 0,
            isEnabled = true,
            itemInstanceId = 1
        });
        inventory.Add(new InventoryItemDescriptor() {
            assetID = 10,
            data = AssetsManager.inst.itemAssets.items[10].GetEmptyData(),
            frameOfObtention = 0,
            isEnabled = true,
            itemInstanceId = 2
        });
        inventory.Add(new InventoryItemDescriptor() {
            assetID = 10,
            data = AssetsManager.inst.itemAssets.items[10].GetEmptyData(),
            frameOfObtention = 0,
            isEnabled = true,
            itemInstanceId = 3
        });
        inventory.Add(new InventoryItemDescriptor() {
            assetID = 10,
            data = AssetsManager.inst.itemAssets.items[10].GetEmptyData(),
            frameOfObtention = 0,
            isEnabled = true,
            itemInstanceId = 4
        });
        inventory.Add(new InventoryItemDescriptor() {
            assetID = 10,
            data = AssetsManager.inst.itemAssets.items[10].GetEmptyData(),
            frameOfObtention = 0,
            isEnabled = true,
            itemInstanceId = 5
        });
        inventory.Add(new InventoryItemDescriptor() {
            assetID = 10,
            data = AssetsManager.inst.itemAssets.items[10].GetEmptyData(),
            frameOfObtention = 0,
            isEnabled = true,
            itemInstanceId = 6
        });
        inventory.Add(new InventoryItemDescriptor() {
            assetID = 10,
            data = AssetsManager.inst.itemAssets.items[10].GetEmptyData(),
            frameOfObtention = 0,
            isEnabled = true,
            itemInstanceId = 7
        });*/
    }

    // Does the usual appropriate updates based on where the function is ran
    public void Update (ref Entity entity) {

        ref var player = ref entity.Get<PlayerComponent>();
        ref var itemUser = ref entity.Get<ItemUserComponent>();
        ref var input = ref entity.Get<InputControlledComponent>();

        if(entity.Has<Ghost>())
            return;

        if(NetAssist.IsClientNotHost && NetAssist.ClientID != owner) {
            for(int i = 0; i < fixedInventory.Length; i++) {
                AssetsManager.inst.itemAssets.items[fixedInventory[i].assetID].OnUpdateClient(ref entity, fixedInventory[i].data, itemUser.lastHeldItem == i, itemUser.lastHeldItemUsed);
            }
            return;
        }

        // Check if any recovery or shields have been requested
        bool recoveryRequested = false;
        bool shieldRequested = false;
        bool consumableRequested = false;
        for(int i = 0; i < fixedInventory.Length; i++) {
            BaseItem baseItem = AssetsManager.inst.itemAssets.items[fixedInventory[i].assetID];
            if(baseItem.inventorySlot == InventorySlot.Recovery) {
                if(baseItem.DoRequestUsage(ref entity, fixedInventory[i].data)) {
                    recoveryRequested = true;
                    break;
                }
            } else if(baseItem.inventorySlot == InventorySlot.Shield) {
                if(baseItem.DoRequestUsage(ref entity, fixedInventory[i].data)) {
                    shieldRequested = true;
                }
            } else if(baseItem.inventorySlot == InventorySlot.Inventory) {
                if(baseItem.DoRequestUsage(ref entity, fixedInventory[i].data)) {
                    shieldRequested = true;
                }
            }
        }
        for(int i = 0; i < inventory.Count; i++) {
            if(inventory[i].itemInstanceId == input.inputSnapshot.selectedInventoryItemUID) {
                BaseItem baseItem = AssetsManager.inst.itemAssets.items[inventory[i].assetID];
                if(baseItem.DoRequestUsage(ref entity, inventory[i].data)) {
                    consumableRequested = true;
                }
            }
        }

        // Find the target held item
        int targetHeldItem = 0;
        if(recoveryRequested) {
            targetHeldItem = slotToInventoryIndex[(int)InventorySlot.Recovery];
        } else if(shieldRequested) {
            targetHeldItem = slotToInventoryIndex[(int)InventorySlot.Shield];
        } else if(consumableRequested) {
            targetHeldItem = -1;
        } else {
            targetHeldItem = slotToInventoryIndex[input.inputSnapshot.selectedFixedInventoryItem];
        }

        // Apply a selection delay
        if(itemUser.lastHeldItem != targetHeldItem) {
            itemUser.lastHeldItem = targetHeldItem;
            heldItemSwitchCooldown = heldItemSwitchFrame;
        }

        // Update all item, check if held item is actually used
        itemUser.lastHeldItemUsed = false;
        for(int i = 0; i < fixedInventory.Length; i++) {
            BaseItem baseItem = AssetsManager.inst.itemAssets.items[fixedInventory[i].assetID];
            bool isItemSelected = slotToInventoryIndex[(int)baseItem.inventorySlot] == targetHeldItem && heldItemSwitchCooldown == 0;
            baseItem.OnUpdate(ref entity, fixedInventory[i].data, isItemSelected);
            if(isItemSelected) {
                itemUser.lastHeldItemUsed = baseItem.IsUsed(ref entity, fixedInventory[i].data, true);
            }

            // Update indicators
            if(!SimulationManager.inst.IsReplayingFrame && NetAssist.IsClient && NetAssist.ClientID == player.clientId) {
                if(baseItem.inventorySlot == InventorySlot.Recovery) {

                    float indicatorValue = baseItem.GetIndicatorValue(ref entity, fixedInventory[i].data, true);
                    ChargingUI.SetIndicatorState(ChargingUIType.Recovery, indicatorValue != 1f, indicatorValue);

                } else if(baseItem.inventorySlot == InventorySlot.Shield) {

                    float indicatorValue = baseItem.GetIndicatorValue(ref entity, fixedInventory[i].data, true);
                    ChargingUI.SetIndicatorState(ChargingUIType.Defence, indicatorValue != 1f, indicatorValue);

                } else if(isItemSelected) {

                    float indicatorValue = baseItem.GetIndicatorValue(ref entity, fixedInventory[i].data, true);
                    ChargingUI.SetIndicatorState(ChargingUIType.Main, indicatorValue != 0f && indicatorValue != 1f, indicatorValue);

                }
            }
        }
        for(int i = 0; i < inventory.Count; i++) {
            BaseItem baseItem = AssetsManager.inst.itemAssets.items[inventory[i].assetID];
            bool isItemSelected = input.inputSnapshot.selectedInventoryItemUID == inventory[i].itemInstanceId && targetHeldItem == -1;
            baseItem.OnUpdate(ref entity, inventory[i].data, isItemSelected);
        }
        for(int i = inventory.Count - 1; i >= 0; i--) {
            if(inventory[i].data is IRemovableItem) {
                if(((IRemovableItem)inventory[i].data).DoRemove()) {
                    inventory.RemoveAt(i);

                    if(NetAssist.IsClient && NetAssist.ClientID == owner) {
                        ItemUIManager.inst.RefreshHeldItemIcon(input.inputSnapshot.selectedInventoryItemUID);
                    }
                }
            }
        }

        // Update the selection delay
        if(heldItemSwitchCooldown > 0) {
            heldItemSwitchCooldown--;
        }
    }

    #region Serialization
    // Serialize all item data that needs to be sync
    public void SerializeNetwork (ref ItemUserComponent itemUser, NetworkWriter writer) {
        writer.WriteByte(heldItemSwitchCooldown);
        writer.WriteByte((byte)itemUser.lastHeldItem);
        writer.WriteBit(itemUser.lastHeldItemUsed);
        for(int i = 0; i < fixedInventory.Length; i++) {
            if(AssetsManager.inst.itemAssets.items[fixedInventory[i].assetID].serializeOnNetwork) {
                fixedInventory[i].data.Serialize(writer);
            }
        }
    }

    // Deserialize all item data that needs to be sync
    public void DeserializeNetwork (bool isSelf, ref ItemUserComponent itemUser, NetworkReader reader) {
        heldItemSwitchCooldown = (byte)reader.ReadByte();
        int itemHeld = reader.ReadByte();
        bool itemUsed = reader.ReadBit();
        if(!isSelf) {
            itemUser.lastHeldItem = itemHeld;
            itemUser.lastHeldItemUsed = itemUsed;
        }
        for(int i = 0; i < fixedInventory.Length; i++) {
            if(AssetsManager.inst.itemAssets.items[fixedInventory[i].assetID].serializeOnNetwork) {
                fixedInventory[i].data.Deserialize(reader);
            }
        }
    }

    // Fakes a read in case of missing player
    public void DeserializeNetworkFake (NetworkReader reader) {
        reader.ReadByte();
        reader.ReadBit();
        for(int i = 0; i < fixedInventory.Length; i++) {
            if(AssetsManager.inst.itemAssets.items[fixedInventory[i].assetID].serializeOnNetwork) {
                fixedInventory[i].data.Deserialize(reader);
            }
        }
    }

    // Serialize all item data that needs to remain when restoring a frame
    public void SerializeFrame (ref ItemUserComponent itemUser, NetworkWriter writer) {
        for(int i = 0; i < fixedInventory.Length; i++) {
            fixedInventory[i].data.Serialize(writer);
        }
    }

    // Deerialize all item data that needs to remain when restoring a frame
    public void DeserializeFrame (ref ItemUserComponent itemUser, NetworkReader reader) {
        for(int i = 0; i < fixedInventory.Length; i++) {
            fixedInventory[i].data.Deserialize(reader);
        }
    }
    #endregion
}
