# Stock Day Summary

**English** | [Русский](README.ru.md)

[![CI](https://github.com/Gesugao-san/stardeus-stock-day-summary/actions/workflows/ci.yml/badge.svg)](https://github.com/Gesugao-san/stardeus-stock-day-summary/actions/workflows/ci.yml)

A [Stardeus](https://store.steampowered.com/app/1380910/Stardeus/) v0.14 mod: at the end of every
stock market day it shows how much your portfolio gained or lost, and can pause the game.

![cover](ModCover.jpg)

## Features

- An entry in the **Events** list, e.g. `Stock market day: +$3.12K (+4.64%)`, green or red.
- Click it for details: day result, portfolio value, profit realized by sales, unrealized profit,
  per-asset breakdown, and which chart days were compared.
- The same line in the event log.
- Optional auto-pause when the summary appears.
- Optional autosave right before the day closes: the game's own autosave, 10 game minutes before the
  boundary, while the old prices are still in place.
- Each sale and each day's prices are written to `Player.log` (lines start with `[StockDaySummary]`),
  so any number can be checked.

## How it counts

Stardeus changes prices once per market day, at the day boundary. The day result compares the price
that held during the finished day with the new price:

```
day result = (value at the new prices + proceeds of same-day sales)
           − (value at the old prices + cost of same-day purchases)
```

Shares bought during the day count from their purchase price, shares sold during the day count at
their sale price. Old prices come from the game's own price history, so the summary is correct right
after loading a save. Only sales made *before* loading are unknown to the mod.

## Settings

Main Menu → **Mod Settings** → Stock Day Summary:

| Option | Default |
|---|---|
| Pause on stock day summary | on |
| Open details window immediately | off |
| Report days without stocks | off |
| Autosave before the day closes | off |

If [Modding Overhauled](https://steamcommunity.com/sharedfiles/filedetails/?id=3566425397) is
installed, the page appears in its *Mod Settings* menu. Otherwise the mod adds its own *Mod Settings*
button at the same place. Options are stored in the game's global `Settings.txt`.

## Install

- **Steam Workshop:** subscribe and enable the mod in Main Menu → Mods.
- **Manually:** put the mod folder into
  `%USERPROFILE%\AppData\LocalLow\Kodo Linija\Stardeus\Mods\StockDaySummary`
  (needs `ModInfo.json`, `ModCover.jpg`, `Libraries\StockDaySummary.dll`, `Translations\`),
  then enable it in Main Menu → Mods.

The mod stores nothing in saves, so it can be added to a running game and removed at any time.

## Build

Requires the [.NET SDK](https://dotnet.microsoft.com/download) and an installed copy of Stardeus
(the build references `Stardeus_Data/Managed/*.dll`).

```cmd
build.cmd
build.cmd "D:\Games\Stardeus"   :: if the game is not in the default Steam folder
```

The DLL is written to `Libraries\StockDaySummary.dll`.

Tests for the profit/loss math run without the game:

```cmd
dotnet run --project Tests
```

On every push, GitHub Actions runs these tests and `.github/scripts/check_mod.py`: strict `ModInfo.json`,
matching keys and `{0}` placeholders in all translations, every key used in code translated, cover size.
The mod DLL itself is built locally only, since it needs the game's own DLLs.

### Layout

| Path | What |
|---|---|
| `Code/Core/DayPnlCalculator.cs` | Pure profit/loss math, no game types |
| `Code/StockDaySummarySys.cs` | Game system: listens to the market day change, builds the summary |
| `Code/SummaryPresenter.cs` | Event, log line, details dialog, pause |
| `Code/PreCloseAutosave.cs` | Autosave before the day closes |
| `Code/Market/` | Price history lookups, sale tracking (Harmony patch on `MarketPortfolio.Sell`) |
| `Code/Settings/` | Options, settings page, *Mod Settings* integration |
| `Translations/` | English and Russian texts |
| `Artwork/make_cover.py` | Renders `ModCover.jpg` |

Before uploading to the Workshop, the game rejects mods that contain dot files or folders
(`.git`, `.gitignore`, `.vscode`), so upload from a clean copy of the mod folder.

## License

[MIT](LICENSE)
