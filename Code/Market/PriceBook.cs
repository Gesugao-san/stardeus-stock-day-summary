// Stock Day Summary — reads asset prices and market day timing from game data.

using System.Reflection;
using Game.Data.Economy;
using KL.Utils;

namespace StockDaySummary.Market {
  /// <summary>
  /// Price and day-length lookups on top of the game's own market data.
  /// The game writes one price multiplier per market day into
  /// <see cref="MarketItemModifier.History"/>, keyed by <c>Market.Day</c>;
  /// price = <c>MarketItem.Value * multiplier</c>.
  /// </summary>
  internal static class PriceBook {
    /// <summary>How many days back to look when a day is missing from history.</summary>
    private const int MaxLookbackDays = 30;

    /// <summary>Default market day length (one in-game day) when the tunable can't be read.</summary>
    private const int DefaultDayLengthTicks = 1440;

    private static readonly FieldInfo dayLengthField = typeof(Game.Data.Economy.Market)
      .GetField("marketDayLengthTicks", BindingFlags.Instance | BindingFlags.NonPublic);

    private static bool warnedDayLength;

    /// <summary>
    /// Price per share of an asset during the given market day.
    /// Falls back to an earlier recorded day, then to the current price.
    /// </summary>
    /// <param name="mod">Market state of the asset.</param>
    /// <param name="day">Market day index (<c>Market.Day</c>).</param>
    /// <returns>Price per share.</returns>
    public static double PriceOnDay(MarketItemModifier mod, int day) {
      var item = mod.Item;
      var history = mod.History;
      if (item == null || history == null) { return mod.Price; }

      for (int d = day; d >= day - MaxLookbackDays; d--) {
        if (history.TryGetValue(d, out float mul)) { return item.Value * mul; }
      }
      return mod.Price;
    }

    /// <summary>
    /// Length of one market day in ticks (tunable <c>MarketDayLengthHours</c>).
    /// </summary>
    /// <param name="market">Current market.</param>
    /// <returns>Day length in ticks, never zero.</returns>
    public static long DayLengthTicks(Game.Data.Economy.Market market) {
      int value = dayLengthField == null ? 0 : (int)dayLengthField.GetValue(market);
      if (value > 0) { return value; }

      WarnDayLengthOnce();
      return DefaultDayLengthTicks;
    }

    /// <summary>Logs a single warning when the private tunable field is not readable.</summary>
    private static void WarnDayLengthOnce() {
      if (warnedDayLength) { return; }
      warnedDayLength = true;
      D.Warn("[StockDaySummary] Could not read market day length, assuming {0} ticks", DefaultDayLengthTicks);
    }
  }
}
