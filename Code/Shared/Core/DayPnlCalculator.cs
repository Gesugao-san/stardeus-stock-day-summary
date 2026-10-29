// Stock Day Summary — pure profit/loss math for one stock market day.
// Has no dependency on game types, so it can be unit-tested outside Stardeus.

using System;
using System.Collections.Generic;

namespace StockDaySummary.Core {
  /// <summary>
  /// One open lot (purchase) in the portfolio, as it is at the end of the day.
  /// </summary>
  public readonly struct Lot {
    /// <summary>Hashed market item id (MarketPosition.IdH).</summary>
    public readonly int AssetId;
    /// <summary>Number of shares still held in this lot.</summary>
    public readonly int Qty;
    /// <summary>Price per share paid when the lot was bought.</summary>
    public readonly double Cost;
    /// <summary>Game tick of the purchase.</summary>
    public readonly long Tick;

    public Lot(int assetId, int qty, double cost, long tick) {
      AssetId = assetId;
      Qty = qty;
      Cost = cost;
      Tick = tick;
    }
  }

  /// <summary>
  /// Shares sold during the day. Captured at the moment of the sale,
  /// because sold lots disappear from the portfolio.
  /// </summary>
  public readonly struct SoldChunk {
    public readonly int AssetId;
    public readonly int Qty;
    /// <summary>Price per share received.</summary>
    public readonly double SellPrice;
    /// <summary>
    /// Price per share at the start of the day, or the purchase price if the
    /// shares were bought during the same day.
    /// </summary>
    public readonly double StartPrice;
    /// <summary>Original purchase price per share (for realized profit).</summary>
    public readonly double Cost;

    public SoldChunk(int assetId, int qty, double sellPrice, double startPrice, double cost) {
      AssetId = assetId;
      Qty = qty;
      SellPrice = sellPrice;
      StartPrice = startPrice;
      Cost = cost;
    }
  }

  /// <summary>Day result for a single asset.</summary>
  public sealed class AssetDayResult {
    public int AssetId;
    /// <summary>Value at the start of the day (or purchase cost for same-day buys).</summary>
    public double StartValue;
    /// <summary>Value at the end of the day plus proceeds of same-day sales.</summary>
    public double EndValue;
    /// <summary>Shares held at the end of the day.</summary>
    public int HeldQty;
    /// <summary>Shares sold during the day.</summary>
    public int SoldQty;

    /// <summary>Profit (positive) or loss (negative) of the day.</summary>
    public double Pnl => EndValue - StartValue;
  }

  /// <summary>Whole-portfolio result of one market day.</summary>
  public sealed class DaySummary {
    public readonly List<AssetDayResult> Assets = new List<AssetDayResult>();
    /// <summary>Market day whose price the result starts from (as on the in-game chart).</summary>
    public int FromDay;
    /// <summary>Market day whose new price the result ends at (as on the in-game chart).</summary>
    public int ToDay;
    /// <summary>Sum of start values of everything that took part in the day.</summary>
    public double StartValue;
    /// <summary>Sum of end values, including proceeds of same-day sales.</summary>
    public double EndValue;
    /// <summary>Market value of the shares still held.</summary>
    public double PortfolioValue;
    /// <summary>Profit locked in by sales during the day (sell price minus purchase price).</summary>
    public double Realized;
    /// <summary>Paper profit of the shares still held (current price minus purchase price).</summary>
    public double Unrealized;

    /// <summary>Profit (positive) or loss (negative) of the day.</summary>
    public double Pnl => EndValue - StartValue;

    /// <summary>Day result in percent of the start value; 0 when nothing was invested.</summary>
    public double Percent => StartValue > 0 ? Pnl / StartValue * 100.0 : 0;

    /// <summary>True when there was nothing to report for this day.</summary>
    public bool IsEmpty => Assets.Count == 0;
  }

  /// <summary>
  /// Computes the profit or loss of one market day.
  /// Day result = (value at end + proceeds of sales) − (value at start + cost of same-day buys).
  /// </summary>
  public static class DayPnlCalculator {
    /// <summary>
    /// Calculates the summary of a finished market day.
    /// </summary>
    /// <param name="lots">Lots held at the end of the day.</param>
    /// <param name="sold">Shares sold during the day.</param>
    /// <param name="startPrice">Price per share of an asset at the start of the day.</param>
    /// <param name="endPrice">Price per share of an asset at the end of the day.</param>
    /// <param name="dayStartTick">First tick of the day; lots bought at or after it are same-day buys.</param>
    /// <returns>Summary with per-asset results.</returns>
    public static DaySummary Calculate(
        IEnumerable<Lot> lots,
        IEnumerable<SoldChunk> sold,
        Func<int, double> startPrice,
        Func<int, double> endPrice,
        long dayStartTick) {
      var summary = new DaySummary();
      var byAsset = new Dictionary<int, AssetDayResult>();

      foreach (var lot in lots) {
        AddLot(summary, ResultFor(byAsset, lot.AssetId), lot, startPrice, endPrice, dayStartTick);
      }
      foreach (var chunk in sold) {
        AddSold(summary, ResultFor(byAsset, chunk.AssetId), chunk);
      }

      summary.Assets.AddRange(byAsset.Values);
      summary.Assets.Sort((a, b) => b.Pnl.CompareTo(a.Pnl));
      return summary;
    }

    /// <summary>Gets or creates the per-asset accumulator.</summary>
    private static AssetDayResult ResultFor(Dictionary<int, AssetDayResult> byAsset, int assetId) {
      if (byAsset.TryGetValue(assetId, out var existing)) { return existing; }
      var created = new AssetDayResult { AssetId = assetId };
      byAsset[assetId] = created;
      return created;
    }

    /// <summary>Accounts a lot that is still held at the end of the day.</summary>
    private static void AddLot(DaySummary summary, AssetDayResult asset, Lot lot,
        Func<int, double> startPrice, Func<int, double> endPrice, long dayStartTick) {
      if (lot.Qty <= 0) { return; }

      bool boughtToday = lot.Tick >= dayStartTick;
      double start = lot.Qty * (boughtToday ? lot.Cost : startPrice(lot.AssetId));
      double end = lot.Qty * endPrice(lot.AssetId);

      asset.StartValue += start;
      asset.EndValue += end;
      asset.HeldQty += lot.Qty;

      summary.StartValue += start;
      summary.EndValue += end;
      summary.PortfolioValue += end;
      summary.Unrealized += end - lot.Qty * lot.Cost;
    }

    /// <summary>Accounts shares sold during the day.</summary>
    private static void AddSold(DaySummary summary, AssetDayResult asset, SoldChunk chunk) {
      if (chunk.Qty <= 0) { return; }

      double start = chunk.Qty * chunk.StartPrice;
      double proceeds = chunk.Qty * chunk.SellPrice;

      asset.StartValue += start;
      asset.EndValue += proceeds;
      asset.SoldQty += chunk.Qty;

      summary.StartValue += start;
      summary.EndValue += proceeds;
      summary.Realized += proceeds - chunk.Qty * chunk.Cost;
    }
  }
}
