using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Blast.Settings;

public class ItemUIManager : MonoBehaviour {

    [Header("Reference")]
    public Sprite emptySlotIcon;
    public InventorySlotUI[] fixedSlotsUI;
    public InventorySlotUI itemSlotUI;
    public ItemSelection itemSelection;

    private ILocalPlayer ownerPlayer;
    [HideInInspector] public LocalItemManager ownerItemManager;
    private int lastHeldWeapon = 0;

    public static ItemUIManager inst;
    private void Awake () {
        inst = this;
    }

    
    private void Start () {
        ownerPlayer = LobbyManager.inst.localPlayers[NetAssist.ClientID];
        //ownerItemManager = SimulationManager.inst.playerSystem.itemManagers[NetworkAssistant.ClientID];

        lastHeldWeapon = InputListener.GetHeldWeapon();
        for(int i = 0; i < fixedSlotsUI.Length; i++) {
            fixedSlotsUI[i].ChangeState(lastHeldWeapon == i, true);
        }

        fixedSlotsUI[0].iconImage.sprite = ItemAssetCollection.GetItemById(ownerPlayer.FixedInventoryIDs[0]).icon;
        fixedSlotsUI[1].iconImage.sprite = ItemAssetCollection.GetItemById(ownerPlayer.FixedInventoryIDs[1]).icon;
        fixedSlotsUI[2].iconImage.sprite = ItemAssetCollection.GetItemById(ownerPlayer.FixedInventoryIDs[2]).icon;
        fixedSlotsUI[3].iconImage.sprite = ItemAssetCollection.GetItemById(ownerPlayer.FixedInventoryIDs[3]).icon;
        fixedSlotsUI[4].iconImage.sprite = ItemAssetCollection.GetItemById(ownerPlayer.FixedInventoryIDs[4]).icon;
        itemSlotUI.iconImage.sprite = emptySlotIcon;
    }

    public void OnSelectCallback (uint selectedItemInstanceId) {
        PausedMenu.inst.Unpause();

        for(int i = 0; i < ownerItemManager.inventory.Count; i++) {
            if(ownerItemManager.inventory[i].itemInstanceId == selectedItemInstanceId) {
                InputListener.SetSelectedItemUID(selectedItemInstanceId);
                itemSlotUI.iconImage.sprite = AssetsManager.inst.itemAssets.items[ownerItemManager.inventory[i].assetID].icon;
                return;
            }
        }
        return;
    }

    private void Update () {
        int heldWeapon = InputListener.GetHeldWeapon();
        if(heldWeapon != lastHeldWeapon) {
            fixedSlotsUI[lastHeldWeapon].ChangeState(false);
            fixedSlotsUI[heldWeapon].ChangeState(true);
        }
        lastHeldWeapon = heldWeapon;

        bool doOpenSelection = false;
        if(SettingsManager.settings.inventoryKM.bindType == KeyboardMouseBindType.Keyboard) {
            if(Input.GetKeyDown(SettingsManager.settings.inventoryKM.keyBind)) {
                doOpenSelection = true;
            }
        } else if(SettingsManager.settings.inventoryKM.bindType == KeyboardMouseBindType.Mouse) {
            if(Input.GetMouseButtonDown(SettingsManager.settings.inventoryKM.mouseBind)) {
                doOpenSelection = true;
            }
        }

        if(doOpenSelection) {
            if(ownerItemManager.inventory.Count > 1) {
                itemSelection.OpenSelection(ownerItemManager.inventory, this);
                PausedMenu.inst.Pause(true);
            } else if(ownerItemManager.inventory.Count == 1) {
                OnSelectCallback(ownerItemManager.inventory[0].itemInstanceId);
            }
        }
    }

    public void RefreshHeldItemIcon (uint selectedItemInstanceId) {
        itemSlotUI.iconImage.sprite = emptySlotIcon;
        for(int i = 0; i < ownerItemManager.inventory.Count; i++) {
            if(ownerItemManager.inventory[i].itemInstanceId == selectedItemInstanceId) {
                itemSlotUI.iconImage.sprite = AssetsManager.inst.itemAssets.items[ownerItemManager.inventory[i].assetID].icon;
                return;
            }
        }
    }

    public void UpdateDefenceState (bool state) {
        fixedSlotsUI[3].ChangeState(state);
    }

    public void UpdateRecoveryState (bool state) {
        fixedSlotsUI[4].ChangeState(state);
    }
}
