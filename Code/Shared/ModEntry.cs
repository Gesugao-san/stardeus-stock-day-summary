// Stock Day Summary — Stardeus mod (v0.14 and v0.15 builds from one source tree).
// At the end of every stock market day shows how much the stock portfolio
// gained or lost; optionally pauses the game. Options live in
//   v0.15: Settings → Stock Day Summary (a tab of the game's own settings);
//   v0.14: Main Menu → Mod Settings → Stock Day Summary (Modding Overhauled's
//          menu when that mod is installed, otherwise this mod's own button).

using System;
using HarmonyLib;
using KL.Utils;
using StockDaySummary.Market;
using UnityEngine;

namespace StockDaySummary {
  /// <summary>
  /// Mod entry point. The mod loader (v0.14 and v0.15) invokes non-public static
  /// methods marked with <see cref="RuntimeInitializeOnLoadMethodAttribute"/>
  /// right after the DLL is loaded. Version-specific setup is in
  /// <see cref="GameBootstrap"/> (Code/V014, Code/V015).
  /// </summary>
  internal static class ModEntry {
    private const string HarmonyId = "gesu.stockdaysummary";

    [RuntimeInitializeOnLoadMethod]
    private static void Init() {
      var harmony = new Harmony(HarmonyId);
      ApplyPatch("sale tracking", () => SellPatch.Apply(harmony));
      ApplyPatch("game version setup", () => GameBootstrap.Install(harmony));
      D.Warn("[StockDaySummary] Loaded for game {0}", GameBootstrap.TargetGame);
    }

    /// <summary>
    /// Runs one setup step. A failure is logged and disables only
    /// that feature, the rest of the mod keeps working.
    /// </summary>
    private static void ApplyPatch(string feature, Action apply) {
      try {
        apply();
      } catch (Exception e) {
        D.Err("[StockDaySummary] Setup of " + feature + " failed: " + e);
      }
    }
  }
}
