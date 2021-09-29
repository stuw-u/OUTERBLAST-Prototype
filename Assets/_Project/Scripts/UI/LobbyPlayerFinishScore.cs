using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using TMPro;

public class LobbyPlayerFinishScore : MonoBehaviour {

    public Color firstColorTop;
    public Color firstColorBottom;
    public Color secondColor;
    public Color thirdColor;

    public Image playerColorImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI rankText;
    public Transform rankTransform;
    public TextMeshProUGUI scoreText;


    public void Setup (int rank, UserDisplayInfo displayInfo, int score) {

        nameText.SetText(displayInfo.username);
        playerColorImage.color = displayInfo.color;

        if(rank == 0) {
            rankText.enableVertexGradient = true;
            rankText.color = Color.white;
            rankText.colorGradient = new VertexGradient(firstColorTop, firstColorTop, firstColorBottom, firstColorBottom);
        } else if(rank == 1) {
            rankText.enableVertexGradient = false;
            rankText.color = secondColor;
        } else {
            rankText.enableVertexGradient = false;
            rankText.color = thirdColor;
        }
        rankText.SetText($"#{rank+1}");
        rankTransform.localScale = Vector3.one * math.select(1f, 1.5f, rank == 0);

        scoreText.SetText($"<b>Score</b>\n{score} pts");
    }
}
