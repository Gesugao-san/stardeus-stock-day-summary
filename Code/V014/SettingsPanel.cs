// Stock Day Summary — the mod's settings page (opened from main menu "Mod Settings").

using Game.UI;
using Game.Utils;
using UnityEngine;

namespace StockDaySummary.Settings {
  /// <summary>
  /// Settings page, built the same way as the game's own gameplay settings page.
  /// Opened from the main menu by <see cref="ModSettingsMenu"/>.
  /// </summary>
  public sealed class SettingsPanel : MonoBehaviour, IUIPanel {
    /// <summary>Translation key of the page title.</summary>
    public const string TitleKey = "stockdaysummary.settings.title";

    /// <inheritdoc />
    public bool NeedsRebuild => false;

    /// <summary>Called by the menu with the descriptor; nothing to configure.</summary>
    public void LoadArgs(PanelDescriptor desc) { }

    /// <inheritdoc />
    public void SetActive(bool on) => gameObject.SetActive(on);

    /// <inheritdoc />
    T IUIPanel.GetComponent<T>() => GetComponent<T>();

    /// <summary>Builds the widgets.</summary>
    private void Start() {
      UIBuilder.CreateText("UIHeadingWidget", TitleKey.T(), transform);

      AddToggle("stockdaysummary.settings.pause", ModSettings.PauseOnSummary,
        v => ModSettings.PauseOnSummary = v);
      AddToggle("stockdaysummary.settings.open.details", ModSettings.OpenDetailsImmediately,
        v => ModSettings.OpenDetailsImmediately = v);
      AddToggle("stockdaysummary.settings.show.empty", ModSettings.ShowWhenEmpty,
        v => ModSettings.ShowWhenEmpty = v);
      AddToggle("stockdaysummary.settings.autosave", ModSettings.AutosaveBeforeClose,
        v => ModSettings.AutosaveBeforeClose = v);
    }

    /// <summary>Creates a toggle with a tooltip taken from "&lt;key&gt;.tip".</summary>
    private void AddToggle(string key, bool value, UnityEngine.Events.UnityAction<bool> onChange) {
      var toggle = UIBuilder.CreateToggle("UIToggleWidget", transform, key.T(), value, onChange);
      UIBuilder.AddTooltip(toggle.gameObject, (key + ".tip").T());
    }
  }
}
