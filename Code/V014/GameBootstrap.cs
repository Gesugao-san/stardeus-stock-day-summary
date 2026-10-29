// Stock Day Summary — v0.14 setup: game system and the main menu "Mod Settings" entry.

using HarmonyLib;
using StockDaySummary.Settings;

namespace StockDaySummary {
  /// <summary>Version-specific setup, v0.14 side.</summary>
  internal static class GameBootstrap {
    /// <summary>Game version this build targets, for the log.</summary>
    public const string TargetGame = "v0.14";

    /// <summary>
    /// v0.14 has no [SelfInit]: the system is registered right away, and the
    /// settings page is hooked into the main menu with Harmony.
    /// </summary>
    public static void Install(Harmony harmony) {
      StockDaySummarySys.Register();
      ModSettingsMenu.Apply(harmony);
    }
  }
}
