// Stock Day Summary — Harmony patch that captures stock sales.

using System;
using System.Collections.Generic;
using Game.Data.Economy;
using HarmonyLib;
using KL.Utils;
using StockDaySummary.Core;

namespace StockDaySummary.Market {
  /// <summary>
  /// Wraps <see cref="MarketPortfolio.Sell"/>: snapshots the lots of the asset
  /// and the wallet before the sale and compares them afterwards to learn exactly
  /// which shares were sold, at what price, from which lots, and how much money
  /// the wallet actually received.
  /// </summary>
  internal static class SellPatch {
    /// <summary>Lot reference with its share count before the sale.</summary>
    internal readonly struct LotSnapshot {
      public readonly MarketPosition Lot;
      public readonly int QtyBefore;

      public LotSnapshot(MarketPosition lot, int qtyBefore) {
        Lot = lot;
        QtyBefore = qtyBefore;
      }
    }

    /// <summary>State passed from prefix to postfix.</summary>
    internal sealed class SellState {
      public LotSnapshot[] Lots;
      public double WalletBefore;
    }

    private static bool reportedError;

    /// <summary>Installs the patch. Logs and skips if the game method changed.</summary>
    /// <param name="harmony">Harmony instance of this mod.</param>
    /// <returns>True when the patch was applied.</returns>
    public static bool Apply(Harmony harmony) {
      var original = AccessTools.Method(typeof(MarketPortfolio), nameof(MarketPortfolio.Sell),
        new[] { typeof(MarketItemModifier), typeof(int) });
      if (original == null) {
        D.Err("[StockDaySummary] MarketPortfolio.Sell not found, same-day sales will not be counted");
        return false;
      }

      harmony.Patch(original,
        prefix: new HarmonyMethod(typeof(SellPatch), nameof(Prefix)),
        postfix: new HarmonyMethod(typeof(SellPatch), nameof(Postfix)));
      return true;
    }

    /// <summary>Remembers lots of the sold asset, their share counts and the wallet.</summary>
    private static void Prefix(MarketPortfolio __instance, MarketItemModifier mod, out SellState __state) {
      __state = null;
      try {
        var lots = Snapshot(__instance, mod);
        if (lots == null) { return; }
        __state = new SellState { Lots = lots, WalletBefore = WalletAmount(mod) };
      } catch (Exception e) {
        ReportOnce(e);
      }
    }

    /// <summary>Turns the difference between snapshot and current lots into recorded sales.</summary>
    private static void Postfix(MarketPortfolio __instance, MarketItemModifier mod, SellState __state) {
      if (__state == null || __state.Lots.Length == 0) { return; }
      try {
        int sold = RecordSales(__instance, mod, __state.Lots);
        LogSale(mod, sold, __state.WalletBefore);
      } catch (Exception e) {
        ReportOnce(e);
      }
    }

    /// <summary>Copies references and share counts of the asset's lots.</summary>
    private static LotSnapshot[] Snapshot(MarketPortfolio portfolio, MarketItemModifier mod) {
      if (mod == null || portfolio?.Positions == null) { return null; }

      var list = new List<LotSnapshot>();
      foreach (var lot in portfolio.Positions) {
        if (lot.IdH != mod.IdH) { continue; }
        list.Add(new LotSnapshot(lot, lot.Position));
      }
      return list.ToArray();
    }

    /// <summary>Records every share that left a lot during this sale.</summary>
    /// <returns>Total number of shares sold.</returns>
    private static int RecordSales(MarketPortfolio portfolio, MarketItemModifier mod, LotSnapshot[] before) {
      var sys = StockDaySummarySys.Active;
      var market = mod.S?.Market;
      if (sys == null || market == null) { return 0; }

      var stillHeld = new HashSet<MarketPosition>(portfolio.Positions ?? new List<MarketPosition>());
      int day = market.Day;
      long dayStart = market.LastTick;
      double sellPrice = mod.Price;
      double dayOpenPrice = PriceBook.PriceOnDay(mod, day);
      int total = 0;

      foreach (var snap in before) {
        int qtyAfter = stillHeld.Contains(snap.Lot) ? snap.Lot.Position : 0;
        int sold = snap.QtyBefore - qtyAfter;
        if (sold <= 0) { continue; }

        bool boughtToday = snap.Lot.Tick >= dayStart;
        double startPrice = boughtToday ? snap.Lot.Cost : dayOpenPrice;
        sys.Ledger.Record(day, new SoldChunk(mod.IdH, sold, sellPrice, startPrice, snap.Lot.Cost));
        total += sold;
      }
      return total;
    }

    /// <summary>
    /// Writes the sale to Player.log together with the real wallet change,
    /// so the price the mod uses can be checked against the money received.
    /// </summary>
    private static void LogSale(MarketItemModifier mod, int sold, double walletBefore) {
      if (sold <= 0) { return; }
      double walletAfter = WalletAmount(mod);
      D.Warn("[StockDaySummary] Sold {0} x {1} on market day {2} at {3:0.##} each = {4:0.##}; wallet {5:0.##} -> {6:0.##} (received {7:0.##})",
        sold, mod.Item?.Name ?? mod.Id, mod.S.Market.Day, mod.Price, sold * (double)mod.Price,
        walletBefore, walletAfter, walletAfter - walletBefore);
    }

    private static double WalletAmount(MarketItemModifier mod) => mod?.S?.Wallet?.Amount ?? 0;

    /// <summary>Logs the first failure only, so a broken patch can't spam the log.</summary>
    private static void ReportOnce(Exception e) {
      if (reportedError) { return; }
      reportedError = true;
      D.Err("[StockDaySummary] Sell tracking failed: " + e);
    }
  }
}
