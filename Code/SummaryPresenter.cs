// Stock Day Summary — turns a day summary into an event, a log line, a dialog and a pause.

using System;
using System.Linq;
using System.Text;
using Game;
using Game.Constants;
using Game.Data;
using Game.Data.Economy;
using Game.Utils;
using StockDaySummary.Core;
using StockDaySummary.Settings;

namespace StockDaySummary {
  /// <summary>
  /// Shows the summary: an entry in the "Events" list (click opens details),
  /// a line in the event log, optional details dialog and optional pause.
  /// </summary>
  internal sealed class SummaryPresenter {
    private const string EventId = "StockDaySummary";
    private const string IconUp = "Icons/Color/StockMarket";
    private const string IconDown = "Icons/Color/StockMarketDown";
    /// <summary>Event stays in the list for one in-game day (ticks are minutes).</summary>
    private const long EventLifetimeTicks = 1440;
    /// <summary>Minimum real minutes the event stays visible.</summary>
    private const float EventMinMinutes = 15f;
    /// <summary>Max number of assets listed in the details.</summary>
    private const int MaxAssetLines = 10;
    /// <summary>Amounts below this are shown as zero.</summary>
    private const double Epsilon = 0.005;

    private EventNotification lastEvent;

    /// <summary>Presents the summary according to the mod options.</summary>
    /// <param name="s">Current game state.</param>
    /// <param name="summary">Result of the finished day.</param>
    public void Present(GameState s, DaySummary summary) {
      string title = Title(summary);
      string details = Details(summary);
      string icon = summary.Pnl < 0 ? IconDown : IconUp;

      ShowEvent(s, title, details, icon);
      s.Sys.Log?.AddLine(title, false);

      if (ModSettings.PauseOnSummary && !s.Clock.IsStopped) { s.Clock.Pause(); }
      if (ModSettings.OpenDetailsImmediately) { ShowDetails(icon, details); }
    }

    /// <summary>Replaces the previous summary in the "Events" list with the new one.</summary>
    private void ShowEvent(GameState s, string title, string details, string icon) {
      if (lastEvent != null) { s.Sig.RemoveEvent.Send(lastEvent); }

      var data = UDB.Create(EventId, UDBT.IEvent, icon, title)
        .WithTooltip(details)
        .WithIconClickFunction(() => ShowDetails(icon, details));
      lastEvent = EventNotification.Create(s.Clock.Ticks, data, Priority.Normal, false)
        .WithHideAfter(EventLifetimeTicks, EventMinMinutes);
      s.Sig.AddEvent.Send(lastEvent);
    }

    private static void ShowDetails(string icon, string details) {
      The.Dialog?.ShowSimple(icon, details);
    }

    /// <summary>Short one-line result, e.g. "Stocks today: +$1.2K (+3.4%)".</summary>
    private static string Title(DaySummary summary) {
      return "stockdaysummary.event.title".T(Money(summary.Pnl), Percent(summary.Percent));
    }

    /// <summary>Multi-line details for the tooltip and the dialog.</summary>
    private static string Details(DaySummary summary) {
      var sb = new StringBuilder();
      sb.AppendLine(Texts.Bold("stockdaysummary.details.header".T()));
      sb.AppendLine("stockdaysummary.details.period".T(summary.FromDay, summary.ToDay));
      sb.AppendLine("stockdaysummary.details.result".T(Money(summary.Pnl), Percent(summary.Percent)));
      sb.AppendLine("stockdaysummary.details.value".T(Plain(summary.PortfolioValue)));
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
      if (Math.Abs(value) < Epsilon) { return "±" + Plain(0); }
      string text = (value > 0 ? "+" : "−") + Plain(Math.Abs(value));
      return value > 0 ? Texts.Green(text) : Texts.Red(text);
    }

    /// <summary>Unsigned money amount in the game's own format ("$227.21K").</summary>
    private static string Plain(double value) => Wallet.ToValueString(value, true);

    /// <summary>Signed percent with two decimals.</summary>
    private static string Percent(double value) {
      if (Math.Abs(value) < Epsilon) { return "0%"; }
      return (value > 0 ? "+" : "−") + Math.Abs(value).ToString("0.##") + "%";
    }
  }
}
