// Stock Day Summary — v0.15 settings tab (Settings → Stock Day Summary).

using Game.Systems.UIUX;
using Game.Utils;
using StockDaySummary.Settings;

namespace StockDaySummary {
  /// <summary>
  /// Tab in the game's own settings window, built with the game's
  /// <see cref="BaseSettingsPage"/> helpers like the built-in tabs.
  /// Values are stored by <see cref="ModSettings"/> in the global settings.
  /// </summary>
  internal sealed class SettingsPage : BaseSettingsPage {
    private const string PageId = "StockDaySummary";
    private const string TitleKey = "stockdaysummary.settings.title";

    /// <summary>After the built-in tabs.</summary>
    private const int PageOrder = 1000;

    /// <summary>Registers the tab at the game's CoreDefs init stage.</summary>
    [SelfInit]
    private static void Register() {
      RegisterPage(new SettingsPage());
    }

    /// <inheritdoc />
    public override string Id => PageId;

    /// <inheritdoc />
    public override int Order => PageOrder;

    /// <inheritdoc />
    public override string TabName => TitleKey.T();

    /// <summary>Builds the widgets of the tab.</summary>
    protected override void LoadTab() {
      int idx = 0;
      AddHeader(idx++, TitleKey.T());
      Toggle(idx++, "stockdaysummary.settings.pause", ModSettings.PauseOnSummary,
        v => ModSettings.PauseOnSummary = v);
      Toggle(idx++, "stockdaysummary.settings.open.details", ModSettings.OpenDetailsImmediately,
        v => ModSettings.OpenDetailsImmediately = v);
      Toggle(idx++, "stockdaysummary.settings.show.empty", ModSettings.ShowWhenEmpty,
        v => ModSettings.ShowWhenEmpty = v);
      Toggle(idx++, "stockdaysummary.settings.autosave", ModSettings.AutosaveBeforeClose,
        v => ModSettings.AutosaveBeforeClose = v);
    }

    /// <summary>Adds a toggle labelled "&lt;key&gt;" with the tooltip "&lt;key&gt;.tip".</summary>
    private void Toggle(int idx, string key, bool value, System.Action<bool> onChange) {
      AddToggle(idx, key.T(), value, onChange, (key + ".tip").T());
    }
  }
}
