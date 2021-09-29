using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ItemVisualConfig : MonoBehaviour {

    [Header("Prefab Parameters")]
    public ItemVisualAnchor anchor;
    public ItemVisualMode visualMode;
    public bool doCastAnyShadow;

    [Header("References")]
    public Renderer[] renderers;
    public Renderer[] colorableRenderers;
    public Renderer[] shatterableColorableRenderers;

    [Header("Holding")]
    public bool holdWhenUsed;
    public Transform holdPointLeft;
    public Transform holdPointRight;

    [Header("Animation")]
    public Animator animator;

    public bool doSetHoldingVariable;
    public string holdingVariableName = "IsHeld";
    private int holdingVariableId;

    public bool doSetUsageVariable;
    public string usageVariableName = "IsUsed";
    private int usageVariableId;
    
    public bool doUseCustomTrigger;
    public string customTriggerName = "OnTrigger";
    private int customTriggerId;

    [Header("Particles")]
    public ParticleSystem[] onUseParticle;
    public ParticleSystem[] onHoldParticle;

    [Header("Other")]
    public ItemVisualConfigListener visualListener;


    [HideInInspector] public int inventoryId;
    private MaterialPropertyBlock mpbShatterColor;

    public void Setup (UserData userData) {
        MaterialPropertyBlock mpbColor = new MaterialPropertyBlock();
        mpbShatterColor = new MaterialPropertyBlock();
        mpbColor.SetColor("_MainColor", userData.DisplayInfo.color);
        mpbShatterColor.SetColor("_MainColor", userData.DisplayInfo.color);
        mpbShatterColor.SetFloat("_ShatteredValue", 0f);
        foreach(Renderer r in colorableRenderers) {
            r.SetPropertyBlock(mpbColor);
        }
        foreach(Renderer r in shatterableColorableRenderers) {
            r.SetPropertyBlock(mpbShatterColor);
        }

        holdingVariableId = Animator.StringToHash(holdingVariableName);
        usageVariableId = Animator.StringToHash(usageVariableName);
        customTriggerId = Animator.StringToHash(customTriggerName);
    }
    
    public void SetRenderingMode (ItemRenderingMode state) {
        for(int i = 0; i < renderers.Length; i++) {
            if(state == ItemRenderingMode.ShowWithoutShadow || (state == ItemRenderingMode.ShowWithShadow && !doCastAnyShadow)) {
                renderers[i].shadowCastingMode = ShadowCastingMode.Off;
                renderers[i].enabled = true;
            } else if(state == ItemRenderingMode.ShowWithShadow && doCastAnyShadow) {
                renderers[i].shadowCastingMode = ShadowCastingMode.On;
                renderers[i].enabled = true;
            } else if(state == ItemRenderingMode.ShadowOnly && doCastAnyShadow) {
                renderers[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                renderers[i].enabled = true;
            } else if(state == ItemRenderingMode.Hide || !doCastAnyShadow) {
                renderers[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                renderers[i].enabled = false;
            }
        }
    }

    private bool wasHeld, wasUsed;
    public void SetState (bool isHeld, bool isUsed) {

        if(isHeld != wasHeld || isUsed != wasUsed) {
            if(doSetHoldingVariable) {
                animator.SetBool(holdingVariableId, isHeld);
            }

            if(doSetUsageVariable) {
                animator.SetBool(usageVariableId, isUsed);
            }

            for(int i = 0; i < onUseParticle.Length; i++) {
                var emission = onUseParticle[i].emission;
                emission.enabled = isUsed;
            }

            for(int i = 0; i < onHoldParticle.Length; i++) {
                var emission = onHoldParticle[i].emission;
                emission.enabled = isHeld;
            }
        }

        visualListener?.OnUpdate(isHeld, isUsed);

        wasHeld = isHeld;
        wasUsed = isUsed;
    }

    public void OnTrigger () {
        animator.SetTrigger(customTriggerId);
        visualListener?.OnTrigger();
    }

    public void OnTriggerPosition (Vector3 position) {
        visualListener?.OnTriggerPosition(position);
    }

    private float lastShatterValue = 0f;
    public void OnSetShatterValue (float value) {
        if(value == lastShatterValue) {
            return;
        }
        mpbShatterColor.SetFloat("_ShatteredValue", value);
        foreach(Renderer r in shatterableColorableRenderers) {
            r.SetPropertyBlock(mpbShatterColor);
        }
        lastShatterValue = value;
    }
}
