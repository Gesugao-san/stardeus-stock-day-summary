// Stock Day Summary — puts the settings page into the main menu "Mod Settings".

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Game;
using Game.Data;
using Game.UI;
using Game.Utils;
using HarmonyLib;
using KL.Utils;

namespace StockDaySummary.Settings {
  /// <summary>
  /// Shows <see cref="SettingsPanel"/> under the main menu "Mod Settings" button.
  /// <para>
  /// If the Modding Overhauled mod is loaded, the page is registered in its
  /// config list (via reflection, no hard dependency), and Modding Overhauled
  /// builds the button with our entry in it. Otherwise this mod adds its own
  /// "Mod Settings" button at the same place, or joins an existing one with
  /// the same text instead of making a duplicate.
  /// </para>
  /// </summary>
  internal static class ModSettingsMenu {
    private const string ButtonTextKey = "stockdaysummary.menu.modsettings";
    private const string ButtonNamePrefix = "MenuBtn_";
    private const string MoAssemblyName = "ModdingOverhauled";
    private const string MoMainType = "ModdingOverhauled.Main";
    private const string MoConfigsField = "ModConfigsTypes";
    private const string LogPrefix = "[StockDaySummary] ";

    /// <summary>Same slot Modding Overhauled uses: right after "Settings".</summary>
    private const int ButtonIndex = 5;

    /// <summary>
    /// Runs after Modding Overhauled remembers the menu (priority 800) and before
    /// it builds its button from the config list (default priority 400).
    /// </summary>
    private const int PrefixPriority = Priority.HigherThanNormal;

    private static readonly FieldInfo rootButtonsField = AccessTools.Field(typeof(MainMenu), "rootButtons");
    private static readonly MethodInfo createButtonMethod = AccessTools.Method(typeof(MainMenu), "CreateButton");

    private static bool registeredInMo;

    /// <summary>Installs the main menu hook.</summary>
    /// <param name="harmony">Harmony instance of this mod.</param>
    public static void Apply(Harmony harmony) {
      var original = AccessTools.Method(typeof(MainMenu), "ShowMainMenu");
      if (original == null) {
        D.Err(LogPrefix + "MainMenu.ShowMainMenu not found, settings page will not be reachable");
        return;
      }
      var prefix = new HarmonyMethod(typeof(ModSettingsMenu), nameof(Prefix)) { priority = PrefixPriority };
      harmony.Patch(original, prefix: prefix);
    }

    /// <summary>Opens the settings page, the same way Modding Overhauled does.</summary>
    public static void OpenPage() {
      The.SysSig.ShowPanel.Send(new PanelDescriptor(typeof(SettingsPanel), true, true));
    }

    private static void Prefix(MainMenu __instance) {
      try {
        if (TryRegisterInModdingOverhauled()) { return; }
        AddOwnButton(__instance);
      } catch (Exception e) {
        D.Err(LogPrefix + "Could not add the settings page to the main menu: " + e);
      }
    }

    /// <summary>
    /// Adds this mod to Modding Overhauled's config list.
    /// </summary>
    /// <returns>True when Modding Overhauled is loaded and will show our page.</returns>
    private static bool TryRegisterInModdingOverhauled() {
      if (registeredInMo) { return true; }

      var configs = ModdingOverhauledConfigs();
      if (configs == null) { return false; }

      // Modding Overhauled only reads ModInfo.Name (the submenu label), so a stand-in
      // is enough when the real one can't be found; it avoids a second button.
      var self = OwnModInfo() ?? StandInModInfo();
      if (!configs.Contains(self)) { configs.Add(self, typeof(SettingsPanel)); }
      registeredInMo = true;
      D.Warn(LogPrefix + "Settings page registered in Modding Overhauled");
      return true;
    }

    /// <summary>
    /// Finds <c>ModdingOverhauled.Main.ModConfigsTypes</c>
    /// (<c>Dictionary&lt;ModInfo, Type&gt;</c>).
    /// </summary>
    /// <returns>The dictionary, or null when the mod is not loaded or its layout changed.</returns>
    private static IDictionary ModdingOverhauledConfigs() {
      var assembly = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == MoAssemblyName);
      if (assembly == null) { return null; }

      var field = assembly.GetType(MoMainType)?
        .GetField(MoConfigsField, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
      var configs = field?.GetValue(null) as IDictionary;
      if (configs == null) {
        D.Warn(LogPrefix + "Modding Overhauled is loaded but its config list was not found, using own Mod Settings button");
      }
      return configs;
    }

    /// <summary>
    /// ModInfo of this mod: the loaded mod whose folder contains this DLL
    /// (works for local and Workshop copies, whose ids differ).
    /// </summary>
    private static ModInfo OwnModInfo() {
      var loader = The.ModLoader;
      if (loader == null) { return null; }

      var candidates = (loader.LoadedMods ?? Enumerable.Empty<ModInfo>())
        .Concat(loader.ModInfos?.Values ?? Enumerable.Empty<ModInfo>());
      string dll = NormalizePath(Assembly.GetExecutingAssembly().Location);
      return candidates.FirstOrDefault(m => !string.IsNullOrEmpty(m.FullPath)
        && dll.StartsWith(NormalizePath(m.FullPath) + "/", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Minimal ModInfo carrying only what Modding Overhauled shows.</summary>
    private static ModInfo StandInModInfo() {
      D.Warn(LogPrefix + "Own ModInfo not found, registering a stand-in in Modding Overhauled");
      return new ModInfo { Id = "StockDaySummary", Name = "Stock Day Summary" };
    }

    private static string NormalizePath(string path) {
      return Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
    }

    /// <summary>Adds our entry under an existing or new "Mod Settings" button.</summary>
    private static void AddOwnButton(MainMenu menu) {
      var root = rootButtonsField?.GetValue(menu) as List<MainMenuButton>;
      if (root == null || createButtonMethod == null) {
        D.Err(LogPrefix + "Main menu layout changed, settings page will not be reachable");
        return;
      }

      string text = ButtonTextKey.T();
      var parent = root.FirstOrDefault(b => b != null && b.name == ButtonNamePrefix + text)
        ?? InsertButton(menu, root, text);
      parent.AddSubmenuItem(CreateButton(menu, "stockdaysummary.settings.title".T(), _ => OpenPage()));
    }

    /// <summary>Creates the "Mod Settings" button and puts it after "Settings".</summary>
    private static MainMenuButton InsertButton(MainMenu menu, List<MainMenuButton> root, string text) {
      var button = CreateButton(menu, text, null);
      root.Insert(Math.Min(ButtonIndex, root.Count), button);
      return button;
    }

    private static MainMenuButton CreateButton(MainMenu menu, string text, Action<MainMenuButton> onClick) {
      return (MainMenuButton)createButtonMethod.Invoke(menu, new object[] { text, onClick, null });
    }
  }
}
