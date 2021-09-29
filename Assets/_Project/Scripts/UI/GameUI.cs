using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour {

    public static GameUI inst;

    [Header("Animation Parameters")]
    public AnimationCurve curvePopCurve;
    public float maxSize = 1.25f;
    public float boostAnimationSpeed = 4f;
    public float retreatAnimationSpeed = 2f;

    [Header("References")]
    public DamageBar damageBar;
    public OpeningAnimator openingAnimator;
    public TextMeshProUGUI timerText;
    public float timerRemaining = 0f;

    public Transform scoreTextTransform;
    public TextMeshProUGUI scoreText;
    private int targetSelfScore = 0;
    private float selfScoreTimer = 0f;
    private bool selfScoreUpdated = false;

    public Transform bountyTextTransform;
    public TextMeshProUGUI bountyText;
    private int targetTopBountyScore = 0;
    private ulong targetTopBountyHolderId = 0;
    private float topBountyTimer = 0f;
    private bool topBountyUpdated = false;


    #region Event
    public void SetSelfScore (int bountyScore) {
        if(bountyScore == targetSelfScore) {
            return;
        }
        targetSelfScore = bountyScore;

        selfScoreTimer = 1f;
        selfScoreUpdated = false;

    }

    public void SetTopBountyHolder (ulong holderId, int bountyScore) {
        targetTopBountyScore = bountyScore;
        targetTopBountyHolderId = holderId;

        topBountyTimer = 1f;
        topBountyUpdated = false;

    }

    public void StartTimer (int min, int sec) {
        timerRemaining = min * 60 + sec + 1;
    }

    public void Cleanup (int timerMin, int timerSec, int startScore) {
        timerText.SetText($"{timerMin}:{timerSec.ToString("D2")}");
        scoreText.SetText(startScore.ToString());
        bountyText.SetText("");
    }

    public void SetSelfDamage (float damage) {
        damageBar.SetValue(damage / 100f);
    }
    #endregion

    
    private void Awake () {
        inst = this;
    }

    private void Update () {
        if(timerRemaining > 0f) {
            timerRemaining = math.max(0f, timerRemaining - Time.unscaledDeltaTime);

            float totalTimerRemaining = timerRemaining;
            int minutes = (int)math.floor(totalTimerRemaining / 60f);
            totalTimerRemaining -= minutes * 60f;
            int seconds = (int)math.floor(totalTimerRemaining);
            totalTimerRemaining -= seconds;

            timerText.SetText($"{minutes}:{seconds.ToString("D2")}");

            if(timerRemaining == 0f && !NetAssist.inst.Mode.HasFlag(NetAssistMode.Debug)) {
                LobbyManager.inst.OnTimerDone();
            }
        }


        if(selfScoreTimer > 0f) {
            selfScoreTimer = Mathf.Max(0f, selfScoreTimer - Time.deltaTime * math.select(boostAnimationSpeed, retreatAnimationSpeed, selfScoreTimer > 0.5f));
            scoreTextTransform.localScale = Vector3.one * math.lerp(1f, maxSize, curvePopCurve.Evaluate(1f - selfScoreTimer));            
            if(selfScoreTimer < 0.5f && !selfScoreUpdated) {
                scoreText.SetText(targetSelfScore.ToString());
                selfScoreUpdated = true;
            }
        }


        if(topBountyTimer > 0f) {
            topBountyTimer = Mathf.Max(0f, topBountyTimer - Time.deltaTime * math.select(boostAnimationSpeed, retreatAnimationSpeed, topBountyTimer > 0.5f));
            bountyTextTransform.localScale = Vector3.one * math.lerp(1f, maxSize, curvePopCurve.Evaluate(1f - topBountyTimer));
            if(topBountyTimer < 0.5f && !topBountyUpdated) {
                if(!LobbyManager.inst.localPlayers.ContainsKey(targetTopBountyHolderId)) {
                    bountyText.SetText("Missing Player <color=#FFD700>" + targetTopBountyScore);
                } else {
                    bountyText.SetText(LobbyManager.inst.localPlayers[targetTopBountyHolderId].UserData.DisplayInfo.username + " <color=#FFD700>" + targetTopBountyScore);
                }
                topBountyUpdated = true;
            }
        }
    }
}
