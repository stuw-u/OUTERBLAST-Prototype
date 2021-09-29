using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatedArrowFlicker : MonoBehaviour {

    public RectTransform rectTransform;
    public Vector2 offset;
    public float speed;

    Vector2 init;

    private void Start () {
        init = rectTransform.anchoredPosition;
    }

    void Update () {
        if(Mathf.Repeat(Time.time, speed) > 0.5f) {
            rectTransform.anchoredPosition = init + offset;
        } else {
            rectTransform.anchoredPosition = init;
        }
    }
}
