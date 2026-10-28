// Stock Day Summary — mod options stored in the game's global settings (Settings.txt).

using Game;

namespace StockDaySummary.Settings {
  /// <summary>
  /// Mod options. Stored in the global game settings, so they apply to every save.
  /// </summary>
  internal static class ModSettings {
    private const string PauseKey = "stockdaysummary.pause";
    private const string OpenDetailsKey = "stockdaysummary.open.details";
    private const string ShowWhenEmptyKey = "stockdaysummary.show.empty";
    private const string AutosaveKey = "stockdaysummary.autosave.before.close";

    /// <summary>Pause the game when the summary arrives. Default: on.</summary>
    public static bool PauseOnSummary {
      get => Get(PauseKey, true);
      set => Set(PauseKey, value);
    }

    /// <summary>Open the details window immediately, not only on click. Default: off.</summary>
    public static bool OpenDetailsImmediately {
      get => Get(OpenDetailsKey, false);
      set => Set(OpenDetailsKey, value);
    }

    /// <summary>Report the day even when no stocks were held or traded. Default: off.</summary>
    public static bool ShowWhenEmpty {
      get => Get(ShowWhenEmptyKey, false);
      set => Set(ShowWhenEmptyKey, value);
    }

    /// <summary>Autosave a few game minutes before the market day closes. Default: off.</summary>
    public static bool AutosaveBeforeClose {
      get => Get(AutosaveKey, false);
      set => Set(AutosaveKey, value);
    }

    private static bool Get(string key, bool def) {
      if (The.Prefs == null) { return def; }
      return The.Prefs.GetBoolAutoHash(key, def);
    }

    private static void Set(string key, bool value) {
      if (The.Prefs == null) { return; }
      The.Prefs.SetBool(key, value);
      The.SaveGlobalSettings();
    }
  }
}
