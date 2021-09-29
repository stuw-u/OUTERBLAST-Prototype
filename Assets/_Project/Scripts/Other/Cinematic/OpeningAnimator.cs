using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Cinemachine;
using Blast.ECS;

public class OpeningAnimator : MonoBehaviour {

    public Animator cinematicBarAnimator;
    public Animator countdownAnimator;
    public CanvasGroup playerIndicator;
    public CanvasGroup overlayFading;
    public CanvasGroup uiFading;

    public AnimationCurve panSpeedCurve;
    public float panSpeed = 0.1f;
    public CinemachineDollyCart dolly;
    public CinemachineVirtualCamera panCamera;

    public int debugPlayerCount = 8;
    private float panPosition = 0f;
    private float mergeTime = 0f;
    private bool animationLock = false;
    private bool countdownStarted = false;
    private int state = 0;
    private float totalTimer;
    private float totalTimerTime;

    private void Start () {
        cinematicBarAnimator.SetInteger("State", 0);
        overlayFading.alpha = 0f;
        uiFading.alpha = 0f;
        playerIndicator.alpha = 0f;
    }

    public void Init () {
        if(state != 0) {
            return;
        }

        state = 1;
        totalTimerTime = (1f / panSpeed) + 1f + 1f;
    }

    public void Close () {
        animationLock = false;
        state = 2;
        mergeTime = 0f;
    }

    void Update () {
        bool isGhost = false;
        if(!NetAssist.IsHeadlessServer) {
            if(SimulationManager.inst.playerSystem.playerEntities.TryGetValue(NetAssist.ClientID, out Entity entity)) {
                isGhost = entity.Has<Ghost>();
            }
        }

        if(animationLock) {
            if(state != 2) {
                uiFading.alpha = math.select(1f, 0f, isGhost);
                playerIndicator.alpha = 1f;
            }
            return;
        }

        if(state == 1) {
            if(panPosition < 1f) {
                if(panPosition < 0.1f) {
                    cinematicBarAnimator.SetInteger("State", 1);
                }

                panPosition += math.min(panSpeed * Time.deltaTime * math.select(1, 10, NetAssist.inst.Mode.HasFlag(NetAssistMode.Debug)), 1f);
                dolly.m_Position = panSpeedCurve.Evaluate(panPosition);
                panCamera.Priority = panPosition < 0.8f ? 16 : 0;
            } else {
                cinematicBarAnimator.SetInteger("State", 2);
                mergeTime = math.saturate(mergeTime + Time.deltaTime);

                playerIndicator.alpha = mergeTime;
                overlayFading.alpha = mergeTime;
                uiFading.alpha = mergeTime;

                if(mergeTime == 1f) {
                    LobbyManager.inst.OnIntroDone();
                    animationLock = true;
                }
            }
            if(!countdownStarted) {
                if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Debug)) {
                    countdownStarted = true;
                } else {
                    if((totalTimerTime - totalTimer) < 4f) {
                        countdownAnimator.gameObject.SetActive(true);
                        countdownStarted = true;
                    }
                }
            }
            totalTimer += Time.deltaTime;
        } else if(state == 2) {
            mergeTime = math.saturate(mergeTime + Time.deltaTime * 2f);

            overlayFading.alpha = 1f - mergeTime;
            uiFading.alpha = 1f - mergeTime;
            playerIndicator.alpha = 1f - mergeTime;

            if(mergeTime == 1f) {
                animationLock = true;
            }
        }
    }
}
