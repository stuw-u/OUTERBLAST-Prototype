using UnityEngine;

public class FinishScreenUI : MonoBehaviour {

    public CanvasGroup playButton;
    public LobbyPlayerFinishScore prefab;

    private void OnEnable () {
        if(LobbyManager.inst != null) {
            Setup(ScoreManager.inst.lastestFinalScores);
        }
    }

    public void Setup (FinalScoreData[] scores) {
        playButton.alpha = (NetAssist.IsHost) ? 1f : 0f;
        playButton.interactable = NetAssist.IsHost;
        foreach(FinalScoreData scoreData in scores) {
            LobbyPlayerFinishScore display = Instantiate(prefab, prefab.transform.parent);
            display.gameObject.SetActive(true);
            if(LobbyManager.inst.localPlayers.ContainsKey(scoreData.clientId)) {
                display.Setup(scoreData.rank, LobbyManager.inst.localPlayers[scoreData.clientId].UserData.DisplayInfo, scoreData.score);
            } else {
                display.Setup(scoreData.rank, new UserDisplayInfo("Missing Player", Color.white), scoreData.score);
            }
        }
    }

}

public struct FinalScoreData {
    public ulong clientId;
    public int rank;
    public int score;
}
