// Stock Day Summary — v0.15 setup: everything registers through the game's [SelfInit].

using Game.Utils;
using HarmonyLib;

namespace StockDaySummary {
  /// <summary>Version-specific setup, v0.15 side.</summary>
  internal static class GameBootstrap {
    /// <summary>Game version this build targets, for the log.</summary>
    public const string TargetGame = "v0.15";

    /// <summary>
    /// Nothing to patch in v0.15: the game system and the settings tab register
    /// themselves through [SelfInit], the same way the game's own ones do.
    /// </summary>
    public static void Install(Harmony harmony) { }

    /// <summary>Registers the game system at the game's CoreDefs init stage.</summary>
    [SelfInit]
    private static void RegisterSystem() {
      StockDaySummarySys.Register();
    }
  }
}
