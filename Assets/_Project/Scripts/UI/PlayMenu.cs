using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class PlayMenu : MonoBehaviour {

    [Header("Menu")]
    public float fadeTime = 0.1f;
    public RectTransform[] containers;
    public Button[] containerButton;
    public CanvasGroup content;

    private float fadeValue = 0f;
    private int currentIndex = 0;
    private int targetIndex = 0;

    void Start () {
        for(int i = 0; i < containers.Length; i++) {
            int index = i;
            containerButton[i].onClick.AddListener(() => {
                OpenSection(index);
            });
        }

        OpenSection(0);
        SwitchContainer(0);
    }

    private void OpenSection (int index) {
        targetIndex = index;
        SwitchContainer(index);
        /*for(int i = 0; i < containers.Length; i++) {
            containerButton[i].interactable = (i != index);
        }*/
    }

    private void SwitchContainer (int index) {
        currentIndex = index;
        for(int i = 0; i < containers.Length; i++) {
            containers[i].gameObject.SetActive(i == index);
        }
    }

    private void Update () {
       /*if(targetIndex != currentIndex) {

            fadeValue += Time.deltaTime;

            if(fadeValue > fadeTime) {
                SwitchContainer(targetIndex);
            }
        } else if(targetIndex == currentIndex && fadeValue != 0f) {
            fadeValue = math.max(0f, fadeValue - Time.deltaTime);
        }
        content.alpha = math.smoothstep(0f, 1f, 1f - (fadeValue / fadeTime));
        content.interactable = fadeValue != content.alpha;*/
    }
}
