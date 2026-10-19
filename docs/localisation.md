# Localisation

Written by `dotnet run --project tools/TextTool -- report` (plan 17 · N4); `ScriptTranslationTests` fails when this is not what the files give. Do not edit by hand.

A line is one string a language may translate. A line is **translated** when its entry has text and the entry's `en` is the English the game has now; **stale** when the English was rewritten after the entry was written (it counts as untranslated, and the game shows the English); **unknown** when the entry matches nothing the game has any more.

## What there is to translate

| Kind | Files | Lines |
|---|---|---|
| Scripts (`scripts/<file>.json`) | 474 | 7603 |
| Overlays (`world/<key>.json`) | 611 | 7206 |
| Hand-made maps (`maps/<Name>.json`) | 298 | 3107 |
| Data files' lines (`data.json`: the professors, the trainers in battle, the radio, the Town Map's words about each place) | 1 | 430 |

## Languages

Each cell of a kind of file is translated / lines; a language with no file for a kind shows 0. `ui.json` is the number of keys the language's own table has (the interface's keys are not counted here: they change with every screen). Names are `names.json` (PokeAPI's, plan 17 · N3) and the hand-written `names.<kind>.json` beside it (plan 17 · N6): "yes" where the language has either. Data is the lines of its `data.json`, keyed by the English line, so a line rewritten in English counts as untranslated.

| Language | `ui.json` | Names | Scripts | Overlays | Maps | Data | Stale | Unknown |
|---|---|---|---|---|---|---|---|---|
| Română (`ro`) | 2909 | yes | 7305 / 7603 | 6916 / 7206 | 3107 / 3107 | 430 / 430 | 0 | 0 |
| Français (`fr`) | 0 | yes | 0 / 7603 | 0 / 7206 | 0 / 3107 | 0 / 430 | 0 | 0 |
| Deutsch (`de`) | 0 | yes | 0 / 7603 | 0 / 7206 | 0 / 3107 | 0 / 430 | 0 | 0 |
| Español (`es`) | 0 | yes | 0 / 7603 | 0 / 7206 | 0 / 3107 | 0 / 430 | 0 | 0 |
| Italiano (`it`) | 0 | yes | 0 / 7603 | 0 / 7206 | 0 / 3107 | 0 / 430 | 0 | 0 |
| 日本語 (`ja`) | 0 | yes | 0 / 7603 | 0 / 7206 | 0 / 3107 | 0 / 430 | 0 | 0 |
| 한국어 (`ko`) | 0 | yes | 0 / 7603 | 0 / 7206 | 0 / 3107 | 0 / 430 | 0 | 0 |
| 繁體中文 (`zh-Hant`) | 0 | yes | 0 / 7603 | 0 / 7206 | 0 / 3107 | 0 / 430 | 0 | 0 |
| 简体中文 (`zh-Hans`) | 0 | yes | 0 / 7603 | 0 / 7206 | 0 / 3107 | 0 / 430 | 0 | 0 |

The overlays and maps are found by the key or name of the file; a place both an overlay and a map name is looked for as an overlay first.
