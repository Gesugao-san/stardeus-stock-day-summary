// Stock Day Summary — remembers sales made during the current market day.

using System.Collections.Generic;
using StockDaySummary.Core;

namespace StockDaySummary.Market {
  /// <summary>
  /// In-memory list of shares sold, grouped by market day.
  /// Sold lots vanish from the portfolio, so this is the only record of them.
  /// Lives as long as the loaded game; sales made before loading a save are unknown.
  /// </summary>
  internal sealed class SalesLedger {
    private readonly Dictionary<int, List<SoldChunk>> byDay = new Dictionary<int, List<SoldChunk>>();

    /// <summary>Records a sale that happened during the given market day.</summary>
    public void Record(int day, SoldChunk chunk) {
      if (!byDay.TryGetValue(day, out var list)) {
        list = new List<SoldChunk>();
        byDay[day] = list;
      }
      list.Add(chunk);
    }

    /// <summary>
    /// Removes and returns every sale recorded up to and including the given day.
    /// Older days are included in case a day boundary was skipped.
    /// </summary>
    public List<SoldChunk> TakeUpTo(int day) {
      var result = new List<SoldChunk>();
      var taken = new List<int>();
      foreach (var pair in byDay) {
        if (pair.Key > day) { continue; }
        result.AddRange(pair.Value);
        taken.Add(pair.Key);
      }
      foreach (int key in taken) { byDay.Remove(key); }
      return result;
    }
  }
}
