
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleCandlestickChart : MonoBehaviour
{
    [Header("株価シミュレーター")]
    [SerializeField] private StockPriceSimulator stockSimulator;

    [Header("チャート設定")]
    [SerializeField] private int maxCandles = 30;
    [SerializeField] private float candleInterval = 3f;
    [SerializeField] private float candleWidth = 20f;
    [SerializeField] private float candleGap = 5f;

    [Header("過去のローソク足生成設定")]
    [SerializeField] private float historicalVolatility = 1.5f;

    [Header("ローソク足の色")]
    [SerializeField]
    private Color bullishColor = new Color(0.1f, 0.85f, 0.4f);

    [SerializeField]
    private Color bearishColor = new Color(0.95f, 0.2f, 0.25f);

    private class Candle
    {
        public float open;
        public float high;
        public float low;
        public float close;
    }

    private readonly List<Candle> candles = new();
    private readonly List<GameObject> drawings = new();

    private RectTransform chartRect;
    private float candleTimer;
    private bool initialized;

    private float CurrentPrice => stockSimulator.CurrentPrice;
    private float PriceMin => stockSimulator.PriceMin;
    private float PriceMax => stockSimulator.PriceMax;

    private void Start()
    {
        chartRect = GetComponent<RectTransform>();

        if (chartRect == null)
        {
            Debug.LogError(
                "RectTransformが必要です。Canvas内のPanelに設定してください。",
                this
            );
            enabled = false;
            return;
        }

        if (stockSimulator == null)
        {
            Debug.LogError(
                "StockPriceSimulatorをInspectorで設定してください。",
                this
            );
            enabled = false;
            return;
        }

        if (maxCandles < 1 || candleInterval <= 0f
            || candleWidth <= 0f || candleGap < 0f
            || PriceMax <= PriceMin)
        {
            Debug.LogError("チャート設定の数値を確認してください。", this);
            enabled = false;
            return;
        }

        // 過去のローソク足を仮生成
        float savedPrice = CurrentPrice;

        for (int i = 0; i < maxCandles - 1; i++)
        {
            CreateHistoricalCandle();
        }

        // 過去足の生成による価格変更を戻す
        // 実際の株価はStockPriceSimulatorが管理する
        StartNewCandleAt(savedPrice);

        stockSimulator.OnPriceUpdated += OnPriceUpdated;

        initialized = true;
        DrawChart();
    }

    private void Update()
    {
        if (!initialized)
            return;

        candleTimer += Time.deltaTime;

        // 一定時間ごとにローソク足を確定
        while (candleTimer >= candleInterval)
        {
            candleTimer -= candleInterval;

            // 確定した終値を次の足の始値にする
            StartNewCandleAt(CurrentPrice);
        }
    }

    private void OnPriceUpdated(float price, StockTrend trend)
    {
        if (!initialized || candles.Count == 0)
            return;

        Candle candle = candles[candles.Count - 1];

        // シミュレーターの株価を現在のローソク足へ反映
        candle.close = price;
        candle.high = Mathf.Max(candle.high, price);
        candle.low = Mathf.Min(candle.low, price);

        DrawChart();
    }

    private void CreateHistoricalCandle()
    {
        float min = PriceMin;
        float max = PriceMax;
        float open = candles.Count > 0
            ? candles[candles.Count - 1].close
            : CurrentPrice;

        float close = Mathf.Clamp(
            open + Random.Range(
                -historicalVolatility,
                historicalVolatility
            ),
            min,
            max
        );

        float high = Mathf.Min(
            max,
            Mathf.Max(open, close)
                + Random.Range(0f, historicalVolatility)
        );

        float low = Mathf.Max(
            min,
            Mathf.Min(open, close)
                - Random.Range(0f, historicalVolatility)
        );

        candles.Add(new Candle
        {
            open = open,
            high = high,
            low = low,
            close = close
        });
    }

    private void StartNewCandleAt(float price)
    {
        candles.Add(new Candle
        {
            open = price,
            high = price,
            low = price,
            close = price
        });

        while (candles.Count > maxCandles)
        {
            candles.RemoveAt(0);
        }

        DrawChart();
    }

    private void DrawChart()
    {
        if (chartRect == null)
            return;

        foreach (GameObject obj in drawings)
        {
            if (obj != null)
                Destroy(obj);
        }

        drawings.Clear();

        float height = chartRect.rect.height;
        float width = chartRect.rect.width;
        float range = PriceMax - PriceMin;

        if (range <= 0f || height <= 0f || width <= 0f)
            return;

        float step = candleWidth + candleGap;

        // 最新の足を右端側に配置
        float startX = width / 3f
            - candleWidth / 3f
            - (candles.Count - 1) * step;

        for (int i = 0; i < candles.Count; i++)
        {
            Candle c = candles[i];
            float x = startX + i * step;

            float openY = PriceToY(c.open, height, range);
            float closeY = PriceToY(c.close, height, range);
            float highY = PriceToY(c.high, height, range);
            float lowY = PriceToY(c.low, height, range);

            Color color = c.close >= c.open
                ? bullishColor
                : bearishColor;

            // ヒゲ
            CreateBar(
                x,
                (highY + lowY) / 2f,
                2f,
                Mathf.Max(2f, highY - lowY),
                color
            );

            // 実体
            CreateBar(
                x,
                (openY + closeY) / 2f,
                candleWidth,
                Mathf.Max(3f, Mathf.Abs(closeY - openY)),
                color
            );
        }
    }

    private float PriceToY(float price, float height, float range)
    {
        return (price - PriceMin) / range * height - height / 2f;
    }

    private void CreateBar(
        float x, float y, float w, float h, Color color)
    {
        GameObject obj = new GameObject(
            "CandlePart",
            typeof(RectTransform),
            typeof(Image)
        );

        obj.transform.SetParent(transform, false);

        RectTransform rect = obj.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);

        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        drawings.Add(obj);
    }

    private void OnDestroy()
    {
        if (stockSimulator != null)
        {
            stockSimulator.OnPriceUpdated -= OnPriceUpdated;
        }
    }
}
