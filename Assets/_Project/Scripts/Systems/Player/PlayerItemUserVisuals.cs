using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerItemUserVisuals : MonoBehaviour {
    [Header("References")]
    public PlayerAnimator playerAnimator;

    [Header("Anchors References")]
    public Transform eyeAnchor;
    public Transform bodyAnchor;
    public Transform clawAnchorLeft;
    public Transform clawAnchorRight;

    private int[] inventory;
    private UserData userData;
    private List<ItemVisualConfig> items;

    public void Setup (int[] inventory, UserData userData, bool isSelf) {
        items = new List<ItemVisualConfig>();
        this.inventory = inventory;
        this.userData = userData;

        for(int i = 0; i < inventory.Length; i++) {
            BaseItem item = AssetsManager.inst.itemAssets.items[inventory[i]];
            if(item.visuals == null)
                continue;

            if(isSelf) {
                for(int x = 0; x < item.visuals.firstPersonPrefabs.Length; x++) {
                    ItemVisualConfig prefab = item.visuals.firstPersonPrefabs[x];
                    ItemVisualConfig itemVisual = Instantiate(prefab, GetAnchor(prefab.anchor));
                    itemVisual.inventoryId = i;
                    itemVisual.Setup(userData);

                    items.Add(itemVisual);
                }
            }

            for(int y = 0; y < item.visuals.thirdPersonPrefabs.Length; y++) {
                ItemVisualConfig prefab = item.visuals.thirdPersonPrefabs[y];
                ItemVisualConfig itemVisual = Instantiate(prefab, GetAnchor(prefab.anchor));
                itemVisual.inventoryId = i;
                itemVisual.Setup(userData);

                items.Add(itemVisual);
            }
        }
    }

    public void SetRendering (bool isFirstPerson) {
        if(items == null)
            return;
        foreach(ItemVisualConfig itemVisual in items) {
            if(isFirstPerson) {
                itemVisual.SetRenderingMode((itemVisual.visualMode == ItemVisualMode.ShowOnFirstPerson) ? ItemRenderingMode.ShowWithoutShadow : ItemRenderingMode.ShadowOnly);
            } else {
                itemVisual.SetRenderingMode((itemVisual.visualMode == ItemVisualMode.ShowOnThirdPerson) ? ItemRenderingMode.ShowWithShadow : ItemRenderingMode.Hide);
            }
        }
    }

    public void SetState (int heldItemId, bool isUsed) {
        if(items == null)
            return;
        bool heldAnyVisualItem = false;
        foreach(ItemVisualConfig itemVisual in items) {
            itemVisual.SetState(heldItemId == itemVisual.inventoryId, isUsed && heldItemId == itemVisual.inventoryId);
            if(heldItemId == itemVisual.inventoryId) {
                heldAnyVisualItem = true;
                if(itemVisual.holdWhenUsed && !isUsed) {
                    playerAnimator.holdPointLeft = null;
                    playerAnimator.holdPointRight = null;
                } else {
                    playerAnimator.holdPointLeft = itemVisual.holdPointLeft;
                    playerAnimator.holdPointRight = itemVisual.holdPointRight;
                }
            }
        }
        if(!heldAnyVisualItem) {
            playerAnimator.holdPointLeft = null;
            playerAnimator.holdPointRight = null;
        }
    }

    public void SetShatterState (int heldItemId, float value) {
        foreach(ItemVisualConfig itemVisual in items) {
            if(heldItemId == itemVisual.inventoryId) {
                itemVisual.OnSetShatterValue(value);
            }
        }
    }

    public void SetTrigger (byte itemId) {
        if(items == null)
            return;
        foreach(ItemVisualConfig itemVisual in items) {
            if(itemId == inventory[itemVisual.inventoryId]) {
                itemVisual.OnTrigger();
            }
        }
    }

    public void SetTriggerPosition (byte itemId, Vector3 position) {
        if(items == null)
            return;
        foreach(ItemVisualConfig itemVisual in items) {
            if(itemId == inventory[itemVisual.inventoryId]) {
                itemVisual.OnTriggerPosition(position);
            }
        }
    }

    public Transform GetAnchor (ItemVisualAnchor anchor) {
        switch(anchor) {
            case ItemVisualAnchor.Body:
            return bodyAnchor;
            case ItemVisualAnchor.Eye:
            return eyeAnchor;
            case ItemVisualAnchor.LeftClaw:
            return clawAnchorLeft;
            case ItemVisualAnchor.RightClaw:
            return clawAnchorRight;
        }
        return null;
    }
}
