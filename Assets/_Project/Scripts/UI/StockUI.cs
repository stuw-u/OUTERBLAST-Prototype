using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StockUI : MonoBehaviour {

    public GameObject[] stocks;
    public RectTransform[] stocksBody;
    public TextMeshProUGUI stockCount;
    public TextMeshProUGUI stockOutline;
    private int count;

    public static StockUI inst;
    private void Awake () {
        inst = this;
    }

    private void Start () {
        if(LobbyManager.inst.matchRulesInfo.scoreMode != ScoreMode.Stocks) {
            Destroy(gameObject);
            return;
        }
        SetStockCount(LobbyManager.inst.matchRulesInfo.stocks);
    }
    
    public void SetStockCount (int count) {
        this.count = count;
        stockCount.gameObject.SetActive(count > 5);
        stockOutline.gameObject.SetActive(count > 5);
        for(int i = 1; i <= stocks.Length; i++) {
            stocks[i-1].SetActive(i <= count);
        }
        if(count <= 99) {
            stockCount.SetText(count.ToString());
            stockOutline.SetText(count.ToString());
        } else {
            stockCount.SetText("99+");
            stockOutline.SetText("99+");
        }
    }

    private void Update () {
        for(int i = 0; i < stocksBody.Length; i++) {
            float bounceValue = Mathf.Sin(Mathf.Min(1f, Mathf.Repeat((Time.time + i) * 2f, 2f * count)) * Mathf.PI);
            stocksBody[i].anchoredPosition = Vector3.up * 5f * bounceValue;
        }
    }
}
