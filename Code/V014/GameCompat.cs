// Stock Day Summary — v0.14 versions of game calls whose signature changed in v0.15.

using Game.Data.Economy;

namespace StockDaySummary {
  /// <summary>Game API differences, v0.14 side.</summary>
  internal static class GameCompat {
    /// <summary>Unsigned money amount in the game's own format ("$227.21K").</summary>
    public static string Money(double value) => Wallet.ToValueString(value, true);
  }
}
