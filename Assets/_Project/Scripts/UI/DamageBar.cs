using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DamageBar : MonoBehaviour {

    public bool isSelf = true;
    public float barSpeed = 30f;
    public float barSpeedFast = 30f;
    public float smooth = 0.1f;
    public float flashSpeed = 5f;

    public float textFade = 1f;

    public Color defaultTextColor;
    public Color flashTextColor;
    public Image damageBarUnder;
    public Image damageBarOver;
    public TextMeshProUGUI damageText;
    private float targetValue;
    private float value;
    private float valueFast;

    private void Start () {
        damageText.ForceMeshUpdate();
        SetValue(0f);

        gameObject.SetActive(LobbyManager.inst.matchRulesInfo.damageMode);
    }

    public void SetValue (float value) {
        targetValue = value;
    }


    private void Update () {

        float lerpBlend = 1f - math.pow(1f - smooth, Time.deltaTime * barSpeed);
        float lerpBlendFast = 1f - math.pow(1f - smooth, Time.deltaTime * barSpeedFast);
        value = math.lerp(value, targetValue, lerpBlend);
        valueFast = math.lerp(valueFast, targetValue, lerpBlendFast);

        if(valueFast > value) {
            damageBarUnder.fillAmount = valueFast;
            damageBarOver.fillAmount = value;
        } else {
            damageBarUnder.fillAmount = value;
            damageBarOver.fillAmount = valueFast;
        }



        float sizeValue = 1f;
        if(isSelf) {
            sizeValue = math.lerp(20f, 40f, math.saturate(math.unlerp(0.9f, 1.5f, value)));
        } else {
            sizeValue = math.lerp(13f, 25f, math.saturate(math.unlerp(0.9f, 1.5f, value)));
        }
        float flashValue = math.saturate(math.unlerp(0.9f, 1.5f, value));
        Color flashColor = Color.Lerp(defaultTextColor, flashTextColor, 1f-math.frac(Time.time * flashSpeed));
        Color color = Color.Lerp(defaultTextColor, flashColor, flashValue);
        color.a = textFade;

        damageText.color = color;
        damageText.fontSize = sizeValue;
        if((int)math.round(math.clamp(valueFast * 100, 0, 200)) == 200) {
            damageText.SetText("DANGER");
        } else {
            damageText.SetText($"{(int)math.round(math.clamp(valueFast * 100, 0, 200))}%");
        }
        


        float shakeValue = math.saturate(math.unlerp(0.9f, 2f, value)) * 7f;
        Vector3[] newVertexPositions;

        damageText.renderMode = TextRenderFlags.DontRender;
        damageText.ForceMeshUpdate();
        TMP_TextInfo textInfo = damageText.textInfo;
        int characterCount = textInfo.characterCount;
        newVertexPositions = textInfo.meshInfo[0].vertices;
        
        for(int i = 0; i < characterCount; i++) {
            if(!textInfo.characterInfo[i].isVisible)
                continue;

            int vertexIndex = textInfo.characterInfo[i].vertexIndex;

            float offsetX = noise.snoise(new float2(i, Time.time * 10f)) * shakeValue;                    
            float offsetY = noise.snoise(new float2(i+2, Time.time * 10f)) * shakeValue;                    

            newVertexPositions[vertexIndex + 0].x += offsetX;
            newVertexPositions[vertexIndex + 0].y += offsetY;
            newVertexPositions[vertexIndex + 1].x += offsetX;
            newVertexPositions[vertexIndex + 1].y += offsetY;
            newVertexPositions[vertexIndex + 2].x += offsetX;
            newVertexPositions[vertexIndex + 2].y += offsetY;
            newVertexPositions[vertexIndex + 3].x += offsetX;
            newVertexPositions[vertexIndex + 3].y += offsetY;

        }
        
        damageText.mesh.vertices = newVertexPositions;
        damageText.UpdateVertexData();
    }
}
