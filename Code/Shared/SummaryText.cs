// Stock Day Summary — text of the day summary (title, details, money and percent formatting).

using System;
using System.Linq;
using System.Text;
using Game.Data.Economy;
using Game.Utils;
using StockDaySummary.Core;

namespace StockDaySummary {
  /// <summary>
  /// Builds the translated, colored texts of a day summary.
  /// Money is formatted by <see cref="GameCompat.Money"/>, whose game API differs per version.
  /// </summary>
  internal static class SummaryText {
    /// <summary>Max number of assets listed in the details.</summary>
    private const int MaxAssetLines = 10;
    /// <summary>Amounts below this are shown as zero.</summary>
    private const double Epsilon = 0.005;

    /// <summary>Short one-line result, e.g. "Stock market day: +$1.2K (+3.4%)".</summary>
    public static string Title(DaySummary summary) {
      return "stockdaysummary.event.title".T(Money(summary.Pnl), Percent(summary.Percent));
    }

    /// <summary>Multi-line details for the tooltip and the dialog.</summary>
    public static string Details(DaySummary summary) {
      var sb = new StringBuilder();
      sb.AppendLine(Texts.Bold("stockdaysummary.details.header".T()));
      sb.AppendLine("stockdaysummary.details.period".T(summary.FromDay, summary.ToDay));
      sb.AppendLine("stockdaysummary.details.result".T(Money(summary.Pnl), Percent(summary.Percent)));
      sb.AppendLine("stockdaysummary.details.value".T(GameCompat.Money(summary.PortfolioValue)));
      if (summary.Assets.Any(a => a.SoldQty > 0)) {
        sb.AppendLine("stockdaysummary.details.realized".T(Money(summary.Realized)));
      }
      sb.AppendLine("stockdaysummary.details.unrealized".T(Money(summary.Unrealized)));
      AppendAssets(sb, summary);
      return sb.ToString().TrimEnd();
    }

    /// <summary>Per-asset lines, best first, truncated to <see cref="MaxAssetLines"/>.</summary>
    private static void AppendAssets(StringBuilder sb, DaySummary summary) {
      if (summary.IsEmpty) {
        sb.AppendLine("stockdaysummary.details.no.assets".T());
        return;
      }

      sb.AppendLine();
      sb.AppendLine("stockdaysummary.details.assets".T());
      foreach (var asset in summary.Assets.Take(MaxAssetLines)) {
        sb.AppendLine(AssetLine(asset));
      }
      int rest = summary.Assets.Count - MaxAssetLines;
      if (rest > 0) { sb.AppendLine("stockdaysummary.details.more".T(rest)); }
    }

    private static string AssetLine(AssetDayResult asset) {
      string name = MarketItem.Find(asset.AssetId)?.Name ?? asset.AssetId.ToString();
      string line = "• " + name + ": " + Money(asset.Pnl);
      if (asset.SoldQty > 0) { line += " " + "stockdaysummary.details.sold".T(asset.SoldQty); }
      return line;
    }

    /// <summary>Signed, colored money amount: green "+$1.2K", red "−$300".</summary>
    private static string Money(double value) {
      if (Math.Abs(value) < Epsilon) { return "±" + GameCompat.Money(0); }
      string text = (value > 0 ? "+" : "−") + GameCompat.Money(Math.Abs(value));
      return value > 0 ? Texts.Green(text) : Texts.Red(text);
    }

    /// <summary>Signed percent with two decimals.</summary>
    private static string Percent(double value) {
      if (Math.Abs(value) < Epsilon) { return "0%"; }
      return (value > 0 ? "+" : "−") + Math.Abs(value).ToString("0.##") + "%";
    }
  }
}
