// Stock Day Summary — turns a day summary into an event, a log line, a dialog and a pause.

using Game;
using Game.Data;
using StockDaySummary.Core;
using StockDaySummary.Settings;

namespace StockDaySummary {
  /// <summary>
  /// Shows the summary: an entry in the game's event list (click opens details),
  /// a line in the event log, optional details dialog and optional pause.
  /// The event itself is drawn by <see cref="SummaryEvent"/>, which has one
  /// implementation per game version (Code/V014, Code/V015).
  /// </summary>
  internal sealed class SummaryPresenter {
    private const string IconUp = "Icons/Color/StockMarket";
    private const string IconDown = "Icons/Color/StockMarketDown";

    private readonly SummaryEvent summaryEvent = new SummaryEvent();

    /// <summary>Presents the summary according to the mod options.</summary>
    /// <param name="s">Current game state.</param>
    /// <param name="summary">Result of the finished day.</param>
    public void Present(GameState s, DaySummary summary) {
      string title = SummaryText.Title(summary);
      string details = SummaryText.Details(summary);
      string icon = summary.Pnl < 0 ? IconDown : IconUp;

      summaryEvent.Show(s, icon, title, details, () => ShowDetails(icon, details));
      s.Sys.Log?.AddLine(title, false);

      if (ModSettings.PauseOnSummary && !s.Clock.IsStopped) { s.Clock.Pause(); }
      if (ModSettings.OpenDetailsImmediately) { ShowDetails(icon, details); }
    }

    private static void ShowDetails(string icon, string details) {
      The.Dialog?.ShowSimple(icon, details);
    }
  }
}
