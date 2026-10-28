// Stock Day Summary — optional autosave shortly before the stock market day closes.

using System.Collections;
using System.Reflection;
using Game;
using Game.Data;
using Game.Utils;
using HarmonyLib;
using KL.Utils;
using StockDaySummary.Market;
using StockDaySummary.Settings;

namespace StockDaySummary {
  /// <summary>
  /// Starts the game's own autosave a few game minutes before the market day
  /// boundary, so the save still has the old prices.
  /// <para>
  /// Uses <c>SaveListener.Autosave()</c>, the same routine as the periodic
  /// autosave: it waits for the clock, shows the "Autosaving" event, stops the
  /// clock while writing and rotates <c>Autosave_*</c> files. Because the clock
  /// is stopped during the save, the boundary can't pass before it is written.
  /// Saving exactly on the boundary tick is not possible: prices change in the
  /// middle of that tick.
  /// </para>
  /// </summary>
  internal sealed class PreCloseAutosave {
    /// <summary>How many ticks (game minutes) before the boundary to save.</summary>
    public const long LeadTicks = 10;

    private const string LogPrefix = "[StockDaySummary] ";

    private static readonly MethodInfo autosaveMethod = AccessTools.Method(typeof(SaveListener), "Autosave");
    private static readonly FieldInfo savingNowField = AccessTools.Field(typeof(SaveListener), "isAutoSavingNow");

    /// <summary>Market day that already got its save, so each day saves once.</summary>
    private int savedForDay = int.MinValue;

    /// <summary>False until the first tick after load has been looked at.</summary>
    private bool started;

    /// <summary>Checks on every tick whether the day is about to close.</summary>
    /// <param name="s">Current game state.</param>
    public void OnTick(GameState s) {
      if (!ModSettings.AutosaveBeforeClose) { return; }

      var market = s.Market;
      if (market == null || market.LastTick == 0) { return; }
      if (market.Day == savedForDay) { return; }

      long left = market.LastTick + PriceBook.DayLengthTicks(market) - s.Clock.Ticks;
      bool firstTick = !started;
      started = true;
      if (left <= 0 || left > LeadTicks) { return; }

      savedForDay = market.Day;
      // A game loaded inside the window is most likely this very autosave:
      // saving again would add a new file on every reload.
      if (firstTick) { return; }
      TryAutosave(market.Day);
    }

    /// <summary>Starts the game's autosave routine unless saving is not allowed now.</summary>
    private static void TryAutosave(int day) {
      if (!A.GameCanBeAutoSaved || A.IsSaveTemplate) {
        D.Warn(LogPrefix + "Autosave before market day close skipped: the game can't be saved now");
        return;
      }

      var listener = UnityEngine.Object.FindAnyObjectByType<SaveListener>();
      if (listener == null || autosaveMethod == null) {
        D.Err(LogPrefix + "Game autosave routine not found, autosave before market day close is unavailable");
        return;
      }
      if (savingNowField?.GetValue(listener) is true) {
        D.Warn(LogPrefix + "Autosave already in progress, it covers the market day close");
        return;
      }

      var routine = autosaveMethod.Invoke(listener, null) as IEnumerator;
      if (routine == null) { return; }
      listener.StartCoroutine(routine);
      D.Warn(LogPrefix + "Autosave before market day {0} closes", day);
    }
  }
}
