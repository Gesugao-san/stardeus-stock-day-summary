# Changelog

The release workflow uses the section of the released version as the release notes,
so keep one `## <version>` heading per `ModVersion` from `ModInfo.json`.

## 0.4.0

- Stardeus **v0.15** support. One mod folder serves both game versions: the game loads
  `0.15/Libraries` or `0.14/Libraries`, whichever matches it.
- v0.15: the summary is a notification in the game's notification panel; the settings are a
  **Stock Day Summary** tab of the game's own settings window (main menu and pause menu).
- v0.15: the mod no longer runs in the demo game behind the main menu, where the autosave option
  made the game report "Saving in demo mode!".
- Details: "Realized by sales" and "Unrealized profit" are now under a **Since purchase** header.
  They are counted from the purchase price, so they don't add up to the day result, which is
  counted from the previous day's price.

## 0.3.0

- First release, for Stardeus v0.14.
- At the end of every stock market day: the day result as an Events list entry, details on click,
  the same line in the event log.
- Options: pause on summary, open details immediately, report days without stocks, autosave before
  the day closes.
- Settings in Modding Overhauled's *Mod Settings* menu, or in the mod's own *Mod Settings* button
  when Modding Overhauled is absent.
