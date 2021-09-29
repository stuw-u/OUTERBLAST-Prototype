using System.Text;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ErrorPromptUI : MonoBehaviour {

    public CanvasGroup canvasGroup;
    public TextMeshProUGUI message;
    public TextMeshProUGUI continueMessage;
    public TextAsset[] errorMessages;

    public static ErrorPromptUI inst;
    private void Awake () {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        inst = this;
    }


    public static void ShowError (int id) {
        inst.StartCoroutine(inst.WriteText(id, string.Empty, null));
    }

    public static void ShowError (int id, Action callback) {
        inst.StartCoroutine(inst.WriteText(id, string.Empty, callback));
    }

    public static void ShowError (int id, string error) {
        inst.StartCoroutine(inst.WriteText(id, error, null));
    }

    public static void ShowError (int id, string error, Action callback) {
        inst.StartCoroutine(inst.WriteText(id, error, callback));
    }

    

    IEnumerator WriteText (int id, string error, Action callback) {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        continueMessage.alpha = 0f;
        StringBuilder sb = new StringBuilder();
        sb.Append('_');

        int skips = 0;
        bool skipUntilEndOfTag = false;

        foreach(char c in errorMessages[id].text) {
            message.SetText(sb);
            message.parseCtrlCharacters = false;
            sb.Insert(sb.Length - 1, c);
            if(c == '<') {
                skipUntilEndOfTag = true;
            }
            if(c == '>') {
                skipUntilEndOfTag = false;
                skips = 1;
            }
            if(c == '\\') {
                yield return new WaitForSeconds(0.1f);
                skips = 2;
            }
            if(c == '\n') {
                yield return new WaitForSeconds(0.1f);
                skips = 1;
            }
            if(skips > 0) {
                skips--;
            } else if(!skipUntilEndOfTag) {
                yield return new WaitForFixedUpdate();
            }
        }
        sb.Insert(sb.Length - 1, '\n');
        message.SetText(sb);
        foreach(char c in error) {
            message.SetText(sb);
            sb.Insert(sb.Length - 1, c);
            yield return new WaitForFixedUpdate();
        }
        message.SetText(sb);
        continueMessage.alpha = 1f;

        yield return new WaitUntil(() => Input.anyKeyDown);
        yield return new WaitForEndOfFrame();
        yield return new WaitUntil(() => Input.anyKeyDown);

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        callback?.Invoke();
    }
}
