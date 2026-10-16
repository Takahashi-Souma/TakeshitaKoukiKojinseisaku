
using System;
using UnityEngine;

public enum StockTrend
{
    Up,       // 上昇トレンド
    Down,     // 下降トレンド
    Sideways  // 横ばい
}

public class StockPriceSimulator : MonoBehaviour
{
    [Header("株価設定")]
    [SerializeField] private float startPrice = 1000f;
    [SerializeField] private float priceMin = 800f;
    [SerializeField] private float priceMax = 1200f;

    [Header("値動き設定")]
    [SerializeField] private float priceUpdateInterval = 0.15f;
    [SerializeField] private float volatility = 1.5f;
    [SerializeField] private float trendDrift = 0.45f;

    [Header("トレンド設定")]
    [SerializeField] private float minTrendDuration = 5f;
    [SerializeField] private float maxTrendDuration = 15f;

    // 現在の株価
    public float CurrentPrice { get; private set; }

    // 現在のトレンド
    public StockTrend CurrentTrend { get; private set; }

    // 株価が更新されたときに通知する
    public event Action<float, StockTrend> OnPriceUpdated;

    private float updateTimer;
    private float nextTrendChangeTime;


  

    // チャートから参照する価格範囲
    public float PriceMin => priceMin;
    public float PriceMax => priceMax;

   // public event Action<float, StockTrend> OnPriceUpdated;

    private void Awake()
    {
        CurrentPrice = Mathf.Clamp(startPrice, priceMin, priceMax);

        ChangeTrend();
        ScheduleNextTrendChange();

        OnPriceUpdated?.Invoke(CurrentPrice, CurrentTrend);
    }


    private void Update()
    {
        updateTimer += Time.deltaTime;

        if (Time.time >= nextTrendChangeTime)
        {
            ChangeTrend();
            ScheduleNextTrendChange();
        }

        float interval = Mathf.Max(0.01f, priceUpdateInterval);

        while (updateTimer >= interval)
        {
            updateTimer -= interval;
            UpdatePrice();
        }
    }

    private void UpdatePrice()
    {
        // トレンドに応じた値動き
        float drift = 0f;

        switch (CurrentTrend)
        {
            case StockTrend.Up:
                drift = trendDrift;
                break;

            case StockTrend.Down:
                drift = -trendDrift;
                break;

            case StockTrend.Sideways:
                drift = 0f;
                break;
        }

        // ランダムな値動きを加える
        float randomChange = UnityEngine.Random.Range(
            -volatility,
            volatility
        );

        CurrentPrice += drift + randomChange;

        // 株価の下限・上限
        CurrentPrice = Mathf.Clamp(
            CurrentPrice,
            priceMin,
            priceMax
        );

        // チャートや売買システムに更新を通知
        OnPriceUpdated?.Invoke(CurrentPrice, CurrentTrend);
    }

    private void ChangeTrend()
    {
        StockTrend previousTrend = CurrentTrend;

        // 直前と同じトレンドが連続しにくいようにする
        do
        {
            CurrentTrend = (StockTrend)UnityEngine.Random.Range(0, 3);
        }
        while (CurrentTrend == previousTrend);
    }

    private void ScheduleNextTrendChange()
    {
        float duration = UnityEngine.Random.Range(
            minTrendDuration,
            maxTrendDuration
        );

        nextTrendChangeTime = Time.time + duration;
    }
}
