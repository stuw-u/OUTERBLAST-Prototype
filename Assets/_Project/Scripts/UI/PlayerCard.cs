using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using Blast.ECS;

public class PlayerCard : MonoBehaviour {
    new public TextMeshProUGUI name;
    public TextMeshProUGUI score;
    public TextMeshProUGUI state;
    public TextMeshProUGUI ping;
    public GameObject dataBoard;
    private int clientId;

    public void UpdateState (int clientId) {
        this.clientId = clientId;
        dataBoard.SetActive(clientId > -1);
        if(clientId < 0) {
            return;
        }

        name.SetText(LobbyManager.inst.localPlayers[(ulong)clientId].UserData.DisplayInfo.username);

        if(SimulationManager.inst.playerSystem.playerEntities.TryGetValue((ulong)clientId, out Entity value)) {
            if(value.Has<Ghost>()) {
                state.SetText("Spectating");
            } else {
                state.SetText("In Game");
            }

            int scoreValue = ScoreManager.inst.GetDisplayScore((ulong)clientId);
            ref var bounty = ref value.Get<TaggableBounty>();
            if(LobbyManager.inst.matchRulesInfo.scoreMode == ScoreMode.Bounty) {
                score.SetText($"Score <color=yellow>{scoreValue}</color>   Bounty <color=yellow>{bounty.value}</color>");
            } else if(LobbyManager.inst.matchRulesInfo.scoreMode == ScoreMode.Stocks) {
                score.SetText($"Score <color=yellow>{scoreValue}</color>   Stocks <color=yellow>{bounty.value}</color>");
            }
        } else {
            state.SetText("In Lobby");
            score.SetText("Waiting to start...");
        }
        ping.SetText($"{LobbyManager.inst.localPlayers[(ulong)clientId].Ping} ms");
    }
}
