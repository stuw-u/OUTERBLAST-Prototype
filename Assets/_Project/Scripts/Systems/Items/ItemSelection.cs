using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSelection : MonoBehaviour {
    
    [Header("Parameters")]
    public float itemCircleRadius;
    public float itemCircleSelectedRadius;
    public float itemIconRadius;
    public float itemIconSelectedRadius;
    public float radius;
    public float selectedRadius;
    public float defaultMargin = 0.01233333333f;
    public float selectedMargin = 0.02366666666f;
    public Color defaultItemColor;
    public Color selectedItemColor;
    public float interpSpeed;
    public float fadeSpeed;

    [Header("Reference")]
    public CanvasGroup fader;
    public TextMeshProUGUI displayItemName;
    public Image displayItemIcon;
    public Transform anchorParent;
    public Transform anchorTemplate;
    public Image itemRing;
    public Image selectedItemRing;


    private Transform[] anchors;
    private float[] anchorsValue;
    private int sectionCount;
    private float sectionAngle;

    private const float valToRad = 6.28318530718f;
    private const float radToVal = 0.15915494309f;
    private const float valToDeg = 360f;
    private const float degToVal = 0.00277777778f;
    private const float degToRad = 0.01745329251f;
    private const float radToReg = 57.2957795131f;

    private bool isSelecting;
    private float fadeTime;

    private float interpTime;
    private float interpFromRot;
    private float interpToRot;
    private BaseItem[] items;
    private uint[] itemsInstanceId;
    private ItemUIManager itemManager;

    void RebuildSelection () {
        sectionCount = items.Length;
        sectionAngle = 1f / sectionCount;
        if(anchors != null) {
            foreach(Transform anchor in anchors) {
                Destroy(anchor.gameObject);
            }
        }
        anchors = new Transform[sectionCount];
        anchorsValue = new float[sectionCount];
        for(int i = 0; i < sectionCount; i++) {
            Transform anchor = Instantiate(anchorTemplate, anchorParent);
            anchor.gameObject.SetActive(true);
            anchor.GetChild(0).GetChild(0).GetComponent<Image>().sprite = items[i].icon;
            anchors[i] = anchor;
        }

        itemRing.fillAmount = 1f - sectionAngle - defaultMargin;
        selectedItemRing.fillAmount = sectionAngle - selectedMargin;
        lastSelected = -1;
    }

    private int lastSelected = -1;
    void Update () {

        if(isSelecting) {
            fadeTime = Mathf.Clamp01(fadeTime + fadeSpeed * Time.deltaTime);
        } else {
            fadeTime = Mathf.Clamp01(fadeTime - fadeSpeed * Time.deltaTime);
        }
        fader.alpha = fadeTime;


        if(fadeTime == 0f)
            return;

        Vector2 aimVector = ((Vector2)Input.mousePosition - new Vector2(Screen.width / 2f, Screen.height / 2f)).normalized;
        float aimTheta = Mathf.Repeat(Mathf.Atan2(aimVector.y, aimVector.x) * radToVal, 1f);
        int selectedIndex = (int)Mathf.Repeat(Mathf.RoundToInt(aimTheta * sectionCount), sectionCount);
        float aimRounded = selectedIndex / (float)sectionCount;
        

        if(lastSelected != selectedIndex) {
            interpTime = 1f;

            interpFromRot = interpToRot;
            interpToRot = aimRounded;

            displayItemIcon.sprite = items[selectedIndex].icon;
            displayItemName.SetText(items[selectedIndex].name);

            lastSelected = selectedIndex;
        }
        interpTime = Mathf.Max(0f, interpTime - Time.deltaTime * interpSpeed);


        for(int i = 0; i < sectionCount; i++) {
            anchorsValue[i] = Mathf.Clamp01(anchorsValue[i] + (selectedIndex == i ? 1f : -0.2f) * Time.deltaTime * interpSpeed);

            float anchorAngle = sectionAngle * i * valToRad;
            float t = anchorsValue[i] * anchorsValue[i];
            anchors[i].localPosition = new Vector2(Mathf.Cos(anchorAngle), Mathf.Sin(anchorAngle)) * Mathf.Lerp(radius, selectedRadius, t) * (1f - ((1f - fadeTime) * (1f - fadeTime)));
            anchors[i].GetChild(0).GetComponent<RectTransform>().sizeDelta = Vector2.one * Mathf.Lerp(itemCircleRadius, itemCircleSelectedRadius, t);
            anchors[i].GetChild(0).GetChild(0).GetComponent<RectTransform>().sizeDelta = Vector2.one * Mathf.Lerp(itemIconRadius, itemIconSelectedRadius, t);
            anchors[i].GetChild(0).GetChild(0).GetComponent<Image>().color = Color.Lerp(defaultItemColor, selectedItemColor, t);
        }

        float aimSmoothed = LerpLoop(interpFromRot, interpToRot, Mathf.SmoothStep(0f, 1f, 1f - interpTime));
        itemRing.transform.localEulerAngles = Vector3.forward * (aimSmoothed + sectionAngle * -0.5f - (defaultMargin * 0.5f)) * valToDeg;
        selectedItemRing.transform.localEulerAngles = Vector3.forward * (aimSmoothed + sectionAngle * 0.5f - (selectedMargin * 0.5f)) * valToDeg;

        if(isSelecting && Input.GetMouseButtonUp(0)) {
            itemManager.OnSelectCallback(itemsInstanceId[selectedIndex]);
            isSelecting = false;
        }

        if(isSelecting && ((PausedMenu.isPaused && !PausedMenu.isSoftPause) || LobbyManager.LocalLobbyState != LocalLobbyState.InGame)) {
            isSelecting = false;
        }
    }

    private void LateUpdate () {
        displayItemName.SetAllDirty();
        displayItemName.GetComponent<RectTransform>().ForceUpdateRectTransforms();
    }

    public void OpenSelection (List<InventoryItemDescriptor> inventoryItems, ItemUIManager itemManager) {
        this.itemManager = itemManager;
        items = new BaseItem[inventoryItems.Count];
        itemsInstanceId = new uint[inventoryItems.Count];
        for(int i = 0; i < inventoryItems.Count; i++) {
            items[i] = AssetsManager.inst.itemAssets.items[inventoryItems[i].assetID];
            itemsInstanceId[i] = inventoryItems[i].itemInstanceId;
        }
        RebuildSelection();
        isSelecting = true;
    }

    public float LerpLoop (float valueA, float valueB, float t) {
        float rA = Mathf.Repeat(valueA, 1f);
        float rB = Mathf.Repeat(valueB, 1f);

        if(Mathf.Abs(rB - 1 - rA) < Mathf.Abs(rB - rA)) {
            return Mathf.Repeat(Mathf.Lerp(rA, rB - 1, t), 1f);
        } else if(Mathf.Abs(rA - 1 - rB) < Mathf.Abs(rB - rA)) {
            return Mathf.Repeat(Mathf.Lerp(rA - 1, rB, t), 1f);
        } else {
            return Mathf.Lerp(rA, rB, t);
        }
    }
}
