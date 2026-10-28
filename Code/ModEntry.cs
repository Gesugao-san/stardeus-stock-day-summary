// Stock Day Summary — Stardeus v0.14 mod.
// At the end of every stock market day shows how much the stock portfolio
// gained or lost; optionally pauses the game. Options live in
// Main Menu → Mod Settings → Stock Day Summary (Modding Overhauled's menu
// when that mod is installed, otherwise this mod's own button).

using System;
using HarmonyLib;
using KL.Utils;
using StockDaySummary.Market;
using StockDaySummary.Settings;
using UnityEngine;

namespace StockDaySummary {
  /// <summary>
  /// Mod entry point. In v0.14 the mod loader invokes non-public static
  /// methods marked with <see cref="RuntimeInitializeOnLoadMethodAttribute"/>
  /// right after the DLL is loaded.
  /// </summary>
  internal static class ModEntry {
    private const string HarmonyId = "gesu.stockdaysummary";

    [RuntimeInitializeOnLoadMethod]
    private static void Init() {
      StockDaySummarySys.Register();
      var harmony = new Harmony(HarmonyId);
      ApplyPatch("sale tracking", () => SellPatch.Apply(harmony));
      ApplyPatch("settings menu", () => ModSettingsMenu.Apply(harmony));
      D.Warn("[StockDaySummary] Loaded");
    }

    /// <summary>
    /// Installs one Harmony patch. A failure is logged and disables only
    /// that feature, the day summary itself keeps working.
    /// </summary>
    private static void ApplyPatch(string feature, Action apply) {
      try {
        apply();
      } catch (Exception e) {
        D.Err("[StockDaySummary] Patching " + feature + " failed: " + e);
      }
    }
  }
}
