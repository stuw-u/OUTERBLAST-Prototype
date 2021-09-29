using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CopyUI : MonoBehaviour {
    public RectTransform source;

    private void Update () {
        GetComponent<RectTransform>().position = source.position;
        GetComponent<RectTransform>().sizeDelta = source.rect.size;
    }
}
