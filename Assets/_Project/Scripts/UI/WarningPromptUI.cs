using System.Text;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI;
using TMPro;


public class WarningPromptUI : MonoBehaviour {

    public CanvasGroup canvasGroup;
    public Image blocker;
    public RectTransform panelTransform;
    public TextMeshProUGUI title;
    public TextMeshProUGUI text;
    public TextAsset[] warningMessages;
    public float fadeSpeed = 3f;

    private bool needClick = false;
    private bool targetState;
    private float fadeValue;

    public static WarningPromptUI inst;
    private void Awake () {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        inst = this;
    }

    public void OnClickOk () {
        needClick = false;
    }

    public static void ShowWarning (int id) {
        inst.StartCoroutine(inst.WriteText(id));
    }
    
    private void Update () {
        fadeValue = math.saturate(fadeValue + math.select(-fadeSpeed * Time.deltaTime, fadeSpeed * Time.deltaTime, targetState));
        panelTransform.anchoredPosition = Vector3.Lerp(Vector3.up*100f, Vector3.zero, math.smoothstep(0f, 1f, fadeValue));
        canvasGroup.alpha = fadeValue;
        canvasGroup.interactable = fadeValue == 1f;
        canvasGroup.blocksRaycasts = fadeValue == 1f;
        blocker.enabled = fadeValue > 0f;
    }

    IEnumerator WriteText (int id) {
        targetState = true;
        needClick = true;

        title.SetText(warningMessages[id].text.Split('@')[0]);
        text.SetText(warningMessages[id].text.Split('@')[1].Remove(1,1));

        yield return new WaitUntil(() => !needClick);
        targetState = false;
    }
}
