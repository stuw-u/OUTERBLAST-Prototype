using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemAssetCollection", menuName = "Custom/Item/Item Asset Collection", order = -1)]
public class ItemAssetCollection : ScriptableObject {

    private static ItemAssetCollection inst;
    public List<BaseItem> items;

    public void Init () {
        inst = this;

        int id = 0;
        foreach(BaseItem item in items) {
            item.id = id;
            id++;
        }
    }

    public static BaseItem GetItemById (int id) {
        if(inst == null) {
            Debug.LogError("Item Assets haven't been initialized.");
            return null;
        }

        if(id >= 0 && id < inst.items.Count) {
            return inst.items[id];
        }
        return null;
    }
}
