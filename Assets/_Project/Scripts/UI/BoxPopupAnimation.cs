using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoxPopupAnimation : MonoBehaviour {

    [Range(0f, 1f)]
    public float value;
    public AnimationCurve sizeCurve;
    public AnimationCurve posCurve;
    public CanvasGroup windowFading;
    public CanvasGroup contentFading;
    public RectTransform box;
    public RectTransform targetWindowRect;
    public RectTransform windowRect;

    private void Start () {
        Animate();
    }

    private void Update () {
        Animate();
    }

    private void Animate () {
        windowRect.position = Vector2.Lerp(box.position, targetWindowRect.position, posCurve.Evaluate(sInv(0.1f, 0.9f, value)));
        windowRect.sizeDelta = Vector2.Lerp(box.rect.size, targetWindowRect.rect.size, sizeCurve.Evaluate(sInv(0.1f, 0.9f, value)));
        windowRect.pivot = Vector2.Lerp(box.pivot, targetWindowRect.pivot, sInv(0.1f, 0.9f, value));
        windowFading.alpha = sInv(0f, 0.1f, value);
        contentFading.alpha = sInv(0.5f, 1f, value);
        contentFading.interactable = value != 0f;
        contentFading.blocksRaycasts = value != 0f;
    }

    private float sInv (float min, float max, float x) {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(min, max, value));
    }
}
