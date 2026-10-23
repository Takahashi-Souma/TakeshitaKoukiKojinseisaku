
using UnityEngine;

public class TradingManager : MonoBehaviour
{
    [Header("株価シミュレーター")]
    [SerializeField] private StockPriceSimulator stockSimulator;

    [Header("資金設定")]
    [SerializeField] private double initialCapital = 1000000;

    [Header("取引設定")]
    [SerializeField] private int sharesPerOrder = 100;

    // 現金残高
    public double Cash { get; private set; }

    // 保有株数
    public int Holdings { get; private set; }

    // 平均取得単価
    public double AveragePurchasePrice { get; private set; }

    // 売却によって確定した損益
    public double RealizedProfit { get; private set; }

    // 現在の株価
    public double CurrentPrice =>
        stockSimulator != null ? stockSimulator.CurrentPrice : 0;

    // 保有株の評価額
    public double MarketValue => CurrentPrice * Holdings;

    // 保有株の評価損益
    public double UnrealizedProfit =>
        MarketValue - AveragePurchasePrice * Holdings;

    // 現金と保有株を合わせた総資産
    public double TotalAssets => Cash + MarketValue;

    // 初期資金からの総損益
    public double TotalProfit => TotalAssets - initialCapital;

    private void Start()
    {
        if (stockSimulator == null)
        {
            Debug.LogError(
                "TradingManagerにStockPriceSimulatorを設定してください。",
                this
            );
            enabled = false;
            return;
        }

        if (initialCapital <= 0 || sharesPerOrder <= 0)
        {
            Debug.LogError("資金設定・取引株数を確認してください。", this);
            enabled = false;
            return;
        }

        Cash = initialCapital;
        Holdings = 0;
        AveragePurchasePrice = 0;
        RealizedProfit = 0;

        Debug.Log($"取引開始！ 初期資金：{Cash:N0}円");
    }

    // 100株購入
    public void Buy100Shares()
    {
        BuyShares(sharesPerOrder);
    }

    // 100株売却
    public void Sell100Shares()
    {
        SellShares(sharesPerOrder);
    }

    private void BuyShares(int shares)
    {
        if (!CanTrade())
            return;

        double price = CurrentPrice;
        double cost = price * shares;

        if (Cash < cost)
        {
            Debug.Log(
                $"購入できません。必要資金：{cost:N0}円 / " +
                $"現金残高：{Cash:N0}円"
            );
            return;
        }

        // 複数回購入した場合の平均取得単価を計算
        double totalCost =
            AveragePurchasePrice * Holdings + cost;

        Holdings += shares;
        Cash -= cost;

        AveragePurchasePrice = totalCost / Holdings;

        Debug.Log(
            $"購入：{shares}株 / 約定価格：{price:N2}円 / " +
            $"現金残高：{Cash:N0}円 / 保有株数：{Holdings}株"
        );
    }

    private void SellShares(int shares)
    {
        if (!CanTrade())
            return;

        if (Holdings < shares)
        {
            Debug.Log(
                $"売却できません。保有株数：{Holdings}株 / " +
                $"売却必要株数：{shares}株"
            );
            return;
        }

        double price = CurrentPrice;
        double proceeds = price * shares;

        // 平均取得単価を使って確定損益を計算
        double profit =
            (price - AveragePurchasePrice) * shares;

        Cash += proceeds;
        Holdings -= shares;
        RealizedProfit += profit;

        if (Holdings == 0)
        {
            AveragePurchasePrice = 0;
        }

        Debug.Log(
            $"売却：{shares}株 / 約定価格：{price:N2}円 / " +
            $"今回の損益：{profit:+#,##0.##;-#,##0.##;0}円 / " +
            $"確定損益：{RealizedProfit:+#,##0.##;-#,##0.##;0}円"
        );
    }

    private bool CanTrade()
    {
        if (!enabled || stockSimulator == null)
            return false;

        return true;
    }
}

