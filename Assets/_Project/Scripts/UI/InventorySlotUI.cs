using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour {

    public Image iconImage;
    public Image bgImage;
    public RectTransform bgRectTransform;

    public float fadeSpeed = 4f;
    public float unselectWidth = 60f;
    public float selectWidth = 90f;
    public Color selectIconColor;
    public Color unselectIconColor;
    public Color selectBGColor;
    public Color unselectBGColor;

    private bool targetState;
    private float fadeValue;

    private void Update () {
        fadeValue = math.saturate(fadeValue + fadeSpeed * Time.deltaTime * math.select(-1f, 1f, targetState));
        iconImage.color = Color.Lerp(unselectIconColor, selectIconColor, fadeValue);
        bgImage.color = Color.Lerp(unselectBGColor, selectBGColor, fadeValue);
        bgRectTransform.sizeDelta = new Vector2(math.lerp(unselectWidth, selectWidth, fadeValue), bgRectTransform.sizeDelta.y);
    }

    public void ChangeState (bool selected, bool instantaniously = false) {
        targetState = selected;
        if(instantaniously) {
            fadeValue = math.select(0f, 1f, selected);
        }
    }
}
