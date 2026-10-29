// Stock Day Summary — game system that reacts to the end of every stock market day.

using System.Collections.Generic;
using System.Linq;
using Game.Data.Economy;
using Game.Systems;
using KL.Utils;
using StockDaySummary.Core;
using StockDaySummary.Market;
using StockDaySummary.Settings;

namespace StockDaySummary {
  /// <summary>
  /// Listens to <c>GameSig.MarketDayChanged</c>, which the game sends right
  /// after the market rolls over to a new day (new prices are already set),
  /// computes the profit or loss of the finished day and shows it.
  /// </summary>
  public sealed class StockDaySummarySys : GameSystem {
    /// <summary>System id; by convention equals the class name.</summary>
    public const string SysId = "StockDaySummarySys";

    /// <summary>The system of the currently loaded game, used by the sell patch.</summary>
    internal static StockDaySummarySys Active { get; private set; }

    /// <summary>Sales made during the current market day.</summary>
    internal readonly SalesLedger Ledger = new SalesLedger();

    private readonly SummaryPresenter presenter = new SummaryPresenter();
    private readonly PreCloseAutosave autosave = new PreCloseAutosave();
    private long lastSummaryTick = -1;
    private KL.Clock.Clock subscribedClock;

    /// <inheritdoc />
    public override string Id => SysId;

    /// <inheritdoc />
    public override bool SkipInSandbox => false;

    /// <summary>Registers the system factory with the game.</summary>
    internal static void Register() {
      GameSystems.Register(SysId, () => new StockDaySummarySys());
    }

    /// <inheritdoc />
    protected override void OnInitialize() {
      Active = this;
      S.Sig.MarketDayChanged.AddListener(OnMarketDayChanged);
      // Same pattern as the game's EconomySys: the clock is ready after the state loads.
      S.Sig.AfterLoadState.AddListener(OnAfterLoadState);
    }

    /// <inheritdoc />
    public override void Unload() {
      S?.Sig?.MarketDayChanged.RemoveListener(OnMarketDayChanged);
      S?.Sig?.AfterLoadState.RemoveListener(OnAfterLoadState);
      subscribedClock?.OnTick.RemoveListener(OnTick);
      subscribedClock = null;
      if (Active == this) { Active = null; }
    }

    /// <summary>Subscribes to clock ticks once the game state is loaded.</summary>
    private void OnAfterLoadState(Game.Data.GameState state) {
      if (subscribedClock == state.Clock) { return; }
      subscribedClock?.OnTick.RemoveListener(OnTick);
      subscribedClock = state.Clock;
      subscribedClock.OnTick.AddListener(OnTick);
    }

    /// <summary>
    /// True for a game the player is playing. False while a save is still loading
    /// (market catch-up must not produce reports) and for the v0.15 main menu
    /// background, which is a running demo save that can't be saved or paused.
    /// </summary>
    private bool IsPlayerGame => S.IsLoaded && !GameCompat.IsDemoMode;

    /// <summary>Per-tick work: autosave shortly before the market day closes.</summary>
    private void OnTick(long ticks) {
      if (!IsPlayerGame) { return; }
      autosave.OnTick(S);
    }

    /// <summary>Handles the end of a market day.</summary>
    private void OnMarketDayChanged() {
      if (!IsPlayerGame) { return; }
      // One report per tick, even if the signal fires repeatedly.
      long now = S.Clock.Ticks;
      if (now == lastSummaryTick) { return; }
      lastSummaryTick = now;

      var summary = BuildSummary();
      if (summary == null) { return; }
      if (summary.IsEmpty && !ModSettings.ShowWhenEmpty) { return; }

      // Pausing and opening UI from inside a clock tick is deferred to the next frame.
      S.Clock.OnNextUpdate(() => presenter.Present(S, summary));
    }

    /// <summary>Collects game data of the finished day and runs the calculation.</summary>
    /// <returns>Summary, or null when the market is not available yet.</returns>
    private DaySummary BuildSummary() {
      var market = S.Market;
      var portfolio = S.Portfolio;
      if (market == null || portfolio == null) { return null; }

      int finishedDay = market.Day - 1;
      long dayStartTick = market.LastTick - PriceBook.DayLengthTicks(market);
      var lots = KnownLots(market, portfolio);
      var sold = Ledger.TakeUpTo(finishedDay);

      var summary = DayPnlCalculator.Calculate(lots, sold,
        id => PriceBook.PriceOnDay(market.Find(id), finishedDay),
        id => market.Find(id).Price,
        dayStartTick);
      summary.FromDay = finishedDay;
      summary.ToDay = market.Day;
      LogPrices(market, summary);
      return summary;
    }

    /// <summary>
    /// Writes start and end price of every asset to Player.log.
    /// Day numbers match the in-game price chart tooltip ("День: N").
    /// </summary>
    private static void LogPrices(Game.Data.Economy.Market market, DaySummary summary) {
      D.Warn("[StockDaySummary] Day summary, chart day {0} -> {1}: result {2:0.##} ({3:0.##}%)",
        summary.FromDay, summary.ToDay, summary.Pnl, summary.Percent);
      foreach (var asset in summary.Assets) {
        var mod = market.Find(asset.AssetId);
        if (mod == null) { continue; }
        D.Warn("[StockDaySummary]   {0}: price day {1} = {2:0.##}, day {3} = {4:0.##}; held {5}, sold {6}; result {7:0.##}",
          mod.Item?.Name ?? mod.Id, summary.FromDay, PriceBook.PriceOnDay(mod, summary.FromDay),
          summary.ToDay, mod.Price, asset.HeldQty, asset.SoldQty, asset.Pnl);
      }
    }

    /// <summary>
    /// Converts portfolio positions to lots, skipping assets the market no longer knows
    /// (they have no price, and counting them as zero would show a fake loss).
    /// </summary>
    private static List<Lot> KnownLots(Game.Data.Economy.Market market, MarketPortfolio portfolio) {
      var positions = portfolio.Positions ?? new List<MarketPosition>();
      var known = positions.Where(p => market.Find(p.IdH) != null).ToList();
      int unknown = positions.Count - known.Count;
      if (unknown > 0) {
        D.Warn("[StockDaySummary] Skipped {0} position(s) of unknown market items", unknown);
      }
      return known.Select(p => new Lot(p.IdH, p.Position, p.Cost, p.Tick)).ToList();
    }
  }
}
