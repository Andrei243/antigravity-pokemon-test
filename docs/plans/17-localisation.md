# Plan 17 · Localisation

Written 2026-10-06, before any session.

**Goal**: the game in more than one language. One string table for everything the interface and the battle say, a language row in the options, the names of every species, move, ability, item, type and nature in each language PokeAPI's own tables name them in, the scripts and the overlays translated through files beside the English ones with the placeholders kept, fonts beyond Latin-1, a tool that reports what each language still lacks, and the first translation as the worked example. Platinum shipped in seven languages; this game should not be English by construction. The lines in every language are our own, as the English ones are.

## Where we are

- **Every string of the interface is a C# literal.** `OptionsScreen.Describe` gives each `OptionRow` its label, its value and its help in English; `TitleScreen` draws "NEW GAME", "OPTIONS", "QUIT" and `ModernUi.SaveSummary(..., "CONTINUE")`; `StartMenu` its entries ("POKÉMON", "BAG"); `ModernUi.Battle.cs` the battle's "FIGHT", "BAG", "POKÉMON", "RUN" and the "What will … do?" of `ModernUi.MessageBox`; `ModernUi.Hints` and `ModernUi.ScreenTitle` take literals from every screen; `IntroScreen` holds the professor's lines (`IntroScreen.Professor`); `GameEngine.ShowNotification` passes "Game saved." to the `Toast` and `LocationSign` shows a place's name (`UI/FieldNotices.cs`). About 750 quoted strings stand in `UI/`, `Core/` and `Battle/BattleEngine*.cs`, a few hundred of them drawn.
- **The battle's lines are formed inside the rules.** `BattleCore` and its partial files call `Say($"{down.Name} fainted!")` at some 185 places (`JoinNames`, `Trainer.FullTitle` and the Pokémon's `DisplayName` go into them), and `BattleCore.WhyNot`, `WhyNotMove` and `WhyNotItem` give their reasons as English strings the menus show. A `Said` event carries its `Text` and nothing else, so the log is English the moment it is written; `BattleRecord` and `Replay` compare those texts (`BattleCoreTests.ARecordedBattleReplaysTheSame`).
- **The data holds English names only.** `PokemonSpecies.Name`, `Category` and `DexEntry`, `MoveData.Name` and `Description`, `ItemData.Name` and `Description`, `AbilityDatabase.AbilityRecord.Name` and `Description`. The name is also the identifier: `PokemonDatabase.Get(name)`, `MoveDatabase.Get(name)`, `ItemDatabase.Get(name)`, `AbilityDatabase.Get(name)`, the save, the scripts, the overlays and `DataFileTests.TestEveryNameInTheDataRefersToSomethingThatExists` all go by it, and a held item is compared by name where the original names the item (`HeldItem?.Name == "Griseous Orb"`).
- **A Pokémon with no nickname carries its species' English name** in `Pokemon.Nickname` (set in the constructors and again on evolution), so `Pokemon.DisplayName` would show English in any language. `TypePill` draws `type.ToString().ToUpperInvariant()`; natures and statuses are enum names; `PokedexSearch.NameGroups` are "ABC" to "YZ"; `NameEntry` has four rows of Latin keys.
- **The importer reads one language.** `PokeApi.EnglishNames` keeps rows whose `local_language_id` is `PokeApi.English` (9) of `pokemon_species_names` (name and genus), `move_names`, `ability_names`, `item_names` and `location_names`, and `PokeApi.Prose` the English `short_effect`. The same pinned CSV folder (`Sources.PokeApiFolders`, `Sources.PokeApiCommit`) has those tables in every language `languages.csv` lists: Japanese in kana and in kanji, Korean, Chinese traditional and simplified, French, German, Spanish and Italian. `Importer.DexEntry` composes the Pokédex entry as English sentences (plan 03 · decision 1: no game text; the games' `*_flavor_text` tables are not read). `Coverage.Report` writes `docs/mechanics/coverage.md` and `CoverageTests.TheReportIsTheOneTheDataAndTheEngineGive` holds it true.
- **Dialogue is English in three kinds of file.** The scripts (`Data/scripts/*.txt`, 184 lines over `common`, `twinleaf_town` and `jubilife_city`: `say`, `text`, `ask`, `choose` and its answers, `speaker`), the overlays (`OverlayPerson.Name` and `Dialog`, the `signs` of a `WorldOverlayFile`) and the hand-made map files (`MapFile.NpcRecord.Dialog`, `SignRecord.Text`, `TrainerRecord.DialogueBefore` and `DialogueAfter`, `TrainerClass`, `MapFile.DisplayName`). Area names are `WorldAreaFile.Name`, written by the map importer, and `Map.DisplayNameAt` fills `{player}` into them.
- **Placeholders are filled at the moment a line shows**: `PlayerIdentity.Fill` for `{player}` and `{assistant}`, `ScriptRunner.Fill` for `{var:NAME}`, `{lead}`, `{item}`, `{money}` and the rest. `IScriptHost.Say(speaker, lines)` and `Ask(speaker, question, answers, cancel)` are where lines reach the screen or `HeadlessScriptHost`'s `Transcript`; `NPC.Key` is a person's id in their area's or map's file.
- **Fonts**: `UiFonts.Get` loads Nunito's three weights for codepoints 32 to 255 plus seven marks (the dashes, the ellipsis, four arrows) at 160 px, mipmapped; `UiFonts.Display` loads ASCII and "é". Anything else draws as "?". The money mark and the gender marks are drawn (`ModernUi.Money`, `UiIcons.GenderMark`), not typed. `ModernUi.Wrap` breaks lines at spaces. `StyleGuideTests.TheInterfaceFontShipsWithTheBuild` holds the font files to the build; `docs/art/CREDITS.md` has their OFL line. Building signs are painted with `Pix.Text`'s own 5×7 alphabet and are art, not text.
- **Settings**: `GameSettings` has no language; `OptionsScreen` shows twelve rows, `OptionsScreen.VisibleRows` of them at once; `GameEngine.ApplySettings(window)` pushes the settings to the clock, the audio, the dialogue's speed and the window. The harness's `look` mode takes one shot of the options (`look_8_options`); `menus`, `intro`, `story` and `battle` show the rest of the text. The style guide's "Interface" says the interface costs one or two tenths of a millisecond in the field and 1.2 ms at a double battle's menu.
- **Neighbours in other plans**: plan 09 · L3 puts markup in lines (`{red:…}`, `{pause 0.4}`, `{money 300}`) through a GPU-free `RichText`; plan 12 · Q8 adds `TextSize` and Q9 `Captions`, a line per sound; plan 02 · S4 adds `{rival}`; plan 11 · C1 gives trainer classes a table (`Data/characters.json`); plan 07 hashes the data into a data version. None of them names a language.

## Design

**One table, asked by key.** `Core/Text.cs` is GPU-free and static like `Ruleset` and `PlayerIdentity`: `Text.Of("options.textSpeed.label")` gives the string of the chosen language, the English when the language lacks it, and the key in brackets when English lacks it too (a test keeps that from shipping). Arguments are named, never positional, so a translation can reorder them: `Text.Of("battle.fainted", ("pokemon", name))` with `"battle.fainted": "{pokemon} fainted!"`. An entry that depends on a number has forms (`{ "one": "{n} step", "other": "{n} steps" }`) chosen by the language's plural rule, and one that depends on the player's look has `boy` and `girl` forms; `Text.Number` and `Text.Decimal` write numbers with the language's own marks (the Pokédex's metres and kilograms). Keys are sections by screen or by rule (`title.`, `start.`, `options.`, `bag.`, `pokedex.`, `battle.`, `intro.`, `notices.`, `types.`, `natures.`, `status.`, `classes.`, `places.`). The English lives in `Data/text/en/ui.json`, not in the code, so a writer re-words the game without recompiling; the code holds keys only.

**The files.** `Data/text/languages.json` lists the languages: tag, the language's own name for itself, its script, plural rule, decimal mark and line-break rule. Each language has a folder of its own:

| File | Written by | Holds |
|---|---|---|
| `Data/text/<tag>/ui.json` | hand | the interface and the battle, by key and section |
| `Data/text/<tag>/names.json` | `tools/DataImporter` | species, categories, moves, abilities, items, types, natures, forms and places, each keyed by its English name |
| `Data/text/<tag>/scripts/<file>.json` | hand, from `TextTool extract` | the lines of one script file, script by script, in order |
| `Data/text/<tag>/world/<key>.json` | hand, from `TextTool extract` | an overlay's people's names and lines, its signs, by id |
| `Data/text/<tag>/maps/<Name>.json` | hand, from `TextTool extract` | a hand-made map's display name, people, signs and trainers' lines |

English has `ui.json` alone: its names are the data's and its lines are the originals. The engine's `Data\**\*.json` rule already copies them to the build. The tag is an IETF one (`en`, `fr`, `de`, `es`, `it`, `ja`, `ko`, `zh-Hant`, `zh-Hans`); `GameSettings.Language` holds it and defaults to `en`.

**The pseudo-language.** `Text.Use("xx")` builds no file: it wraps every string of the table, every name and every line in brackets as it is asked for (`[Tackle]`, `[Game saved.]`). A screen or a battle run under it shows an unbracketed word wherever a literal slipped past the table, which is how the tests and the harness find them without a translator.

**Names and identifiers stay apart.** `Name` on a species, move, item or ability is the identifier the data, the save, the scripts, the rules and the hold-effect comparisons go by, and is never drawn. Each gains a `DisplayName` (`PokemonSpecies.DisplayName`, `MoveData.DisplayName`, `ItemData.DisplayName`, `Ability.DisplayName`, and `DisplayCategory`), read through `Data/LocalNames.cs`, which loads the language's `names.json` once and answers English where it has nothing. `Pokemon.DisplayName` becomes "the nickname, or the species' display name": a Pokémon without a nickname keeps `Nickname` empty, `Pokemon.CopyStateFrom` and the save carry that, and a save's nickname equal to its species' English name is read as none (the constructors and the rename on evolution set the name today, so every save has them). The battle's lines take display names; the core's `Say` calls become `Say(Text.Of(...))`, which keeps the core GPU-free and a replay the same within one language.

**Descriptions and the Pokédex entry.** Descriptions are PokeAPI's own `short_effect` prose where a language has it and English where not. The Pokédex entry is composed at run time by a GPU-free `Models/DexText.cs` from one template a language (`pokedex.entry` with `{name}`, `{category}`, `{types}`, `{region}`, `{height}`, `{weight}`, `{from}`, `{into}`), with a test holding the English composition equal to today's `dexEntry` for all 1025 species. Nothing reads the games' flavour texts: plan 03 · decision 1 stands, and the names CSVs are data of the same kind and licence as the English names already imported.

**Scripts and overlays keep their English files.** `ScriptLibrary.Load` and `World` read what they read today; a translation is a sibling file, and a line is translated as it is shown:

1. `ScriptParser` numbers each text-bearing instruction of a script (`Instruction.TextIndex`: `say`, `text`, `ask`, `choose` with its answers).
2. `scripts/<file>.json` holds, for each script of that file, the list of its lines in that order, each entry carrying the English it translates (`"en"`) beside the translation (`"text"`).
3. `ScriptRunner` asks `Text.Line(script.FullName, index, k)` for the k-th line of the instruction before `Fill` puts the placeholders in; English comes back where there is no entry.
4. A person's own lines (`sayown`: `NPC.DialogLines`, `NPC.Name`, a sign's text, a trainer's two lines) are looked up by the place and `NPC.Key` or the sign's id from `world/<key>.json` and `maps/<Name>.json`, so the maps themselves hold English and `Map.FindPerson` and the tests change nothing.
5. An entry whose `en` no longer matches the English line is stale and counts as untranslated: a scene can be rewritten without a translation silently saying something else.

Placeholders and plan 09's markup pass through untouched; a translation may use only the placeholders its English line has.

**Fonts by script.** `UiFonts` becomes a set of faces: Nunito for whatever its shipped files cover (Latin for certain; N5 first measures the rest), and a second face for CJK, loaded in batches of at most six hundred codepoints (one `LoadFontEx` atlas each, at 72 px rather than 160, mipmapped as today) gathered from the characters the chosen language's files use, plus Latin-1 and the marks. `Draw` and `Measure` split a string into runs by the face that has each glyph; `ModernUi.Wrap` takes the language's break rule (at spaces, or between any two CJK characters but not before a closing mark). The faces of a language are built when `Text.Use` changes it, on the main thread, behind the options screen. The money and gender marks stay drawn. The title's lettering (`UiFonts.Display`) stays Latin: it is our own logo, and the menu under it is translated.

**What is never translated**: the identifiers above, the flags and variables, the MML and the sound map, the models' names, the building signs painted into the art, the music. The language is not in the save, and nothing in a save changes with it.

**What draws and what doesn't.** `Text`, `LocalNames`, `DexText`, the plural and break rules and the report are GPU-free, so tests drive them without a window; only `UiFonts` touches the GPU. Two runs of the harness still draw the same pictures: the table is deterministic, and a font atlas's packing depends on the codepoint list alone.

## Sessions

| # | Session | Delivers | Needs |
|---|---|---|---|
| N1 | The string table, the language row and the menus | `Text`, `Language`, `en/ui.json`, the option, the pseudo-language, every literal of `UI/` and `Core/` | nothing |
| N2 | The battle's lines and the introduction | the core's lines, the reasons, the HUD, the evolution screen, the intro | N1 |
| N3 | Names in every language, from the importer | `names.json` per language, `LocalNames`, the display names, empty nicknames, `DexText` | N1 |
| N4 | Scripts, overlays and the report | `TextIndex`, `Text.Line`, the sibling files, `tools/TextTool`, `docs/localisation.md` | N1 |
| N5 | Fonts beyond Latin-1 | the faces, the batches, runs, break rules, the CJK face | N1, the user's OK |
| N6 | The first translation | one language complete to Oreburgh City | N2, N3, N4; N5 for a CJK language |

### N1 · The string table, the language row and the menus
- `Core/Text.cs` (`Of`, `Use`, forms, `Number`, `Decimal`), `Core/Language.cs` and `Data/text/languages.json` with English and the pseudo-language; `Data/text/en/ui.json` by section.
- `GameSettings.Language`; `OptionRow.Language` as the first row with the language's own name on its pill, and `ApplySettings` calling `Text.Use`; a language whose script no face covers is not offered until N5.
- Every literal of `UI/` and `Core/` through `Text.Of`: the title, the start menu, the options (labels, values and help), the bag, the shop, the party, the Pokédex (its orders, groups and the area page's words), the Trainer Card, the PC, the starters, saving, the notices, the hints, the type pills, natures and statuses. `docs/data-files.md` gains a "Text" section.
- Tests (`TextTests`): every key the sources ask for exists in `en/ui.json` and every entry is asked for somewhere (the test reads the engine's sources as `CoverageTests` finds the repository); no drawing call in `UI/` or `Core/` passes a quoted literal (`UiFonts.Draw*`, `ScreenTitle`, `Hints`, `ShowNotification`, `ShowDialogue`); the forms and plural rules; the pseudo-language brackets everything; `MenuScreenTests` keeps text speed's row first after the language's.
- Harness: `SHOTS_LANGUAGE=<tag>` sets the language before `ApplySettings`, like `SHOTS_PRESETS`; `look_8_options` shows the new row; a `menus` run under `xx` is read screen by screen for unbracketed words.
- **Done when** the menus run under `xx` show brackets everywhere and `en` draws exactly what it drew (the `menus`, `look` and `title` diffs against a run before are empty).

### N2 · The battle's lines and the introduction
- The core's 185 `Say` sites, `WhyNot`, `WhyNotMove` and `WhyNotItem`, `JoinNames` and `Trainer.FullTitle` (a `classes.` section, which plan 11 · C1's table reads when it lands), the menus of `BattleEngine.Menus.cs`, `ModernUi.Battle.cs` and the HUD, the evolution screen's lines, and `IntroScreen`'s, all through `Text.Of`.
- The two-line `MessageBox` measured: a test holds every English battle line to two wrapped lines at the box's width, as a floor for translations.
- Tests: the battle and scenario tests keep their English assertions (English is the fallback and the tests' language); a wild battle and a trainer battle played by `CoreScenario` under `xx` leave a log whose every line is bracketed; `ARecordedBattleReplaysTheSame` under `fr` with an empty file replays the same; the intro played through under `xx`.
- Harness: `demo`, `conditions` and `intro` under `xx`, read for literals; the `battle` and `intro` diffs under `en` are empty.
- **Done when** nothing the battle or the introduction says comes from a literal.

### N3 · Names in every language, from the importer
- `PokeApi` reads the names tables for every language of `languages.csv` (`SpeciesName`, `Genus`, `MoveName`, `AbilityName`, `ItemName`, `LocationName` take a language; `type_names`, `nature_names` and `pokemon_form_names` are new readers) and the prose tables where a language has a `short_effect`; `tools/DataImporter/Program.cs` writes `Data/text/<tag>/names.json` for each, leaving a file alone when it did not change. Platinum's place names come from `location_names` where PokeAPI has one and are left for N6 otherwise.
- `Data/LocalNames.cs`; the `DisplayName`s and `DisplayCategory`; `Pokemon.Nickname` empty for no nickname, `CopyStateFrom`, the save's reading rule; `Models/DexText.cs` and the entry template.
- `PokedexSearch`'s name groups and `FirstLetter` take a language's own groups (kana rows for Japanese); `ModernUi.Pokedex.cs` sorts the alphabetical order by the language's collation.
- Tests: every species, move, ability and item has a name in every file or falls back to English, and the count of each is pinned; no file names something the data lacks (as `TestEveryNameInTheDataRefersToSomethingThatExists` does for English); the Pokédex entry composed in English equals the file's for all 1025; a save with a Pokémon named after its species loads with no nickname; a battle under `xx` names every Pokémon and move in brackets.
- `docs/mechanics/coverage.md` gains a line per language (names present, descriptions present) and `CoverageTests` holds it; `docs/art/CREDITS.md`'s PokeAPI line names the per-language tables.
- **Done when** the Pokédex, the bag and a battle read in French with no code written for French.

### N4 · Scripts, overlays and the report
- `Instruction.TextIndex`; `Text.Line`; the runner's and the hosts' lookup of a line, a person's name, a sign and a trainer's lines by place and key; `choose`'s answers included; `Map.DisplayNameAt` through the `places.` section.
- A new `tools/TextTool` (in the solution, like the importer): `extract <tag>` writes the skeleton of a language's `scripts/`, `world/` and `maps/` files with every English line and an empty `text`; `report [tag]` lists what is untranslated, stale or unknown per file and writes `docs/localisation.md` with the counts, which `TextTests` holds as `CoverageTests` holds coverage.
- Tests: every language's files parse to the same shape as the English (every script named exists, the line count matches, every `en` matches, the placeholders and markup codes of a translation are a subset of its English line's); `StoryTests` plays every script on every way through under `xx` and the transcript holds only bracketed lines; a stale entry falls back to English and is reported.
- Harness: the `story` mode under `xx`, read for literals; `st10_sign` and the nurse under `en` unchanged.
- **Done when** a script's every line, question and answer can come from a sibling file, and the report says exactly what is missing.

### N5 · Fonts beyond Latin-1
- The style guide's "Typography" first: the second face, its size and weight beside Nunito's scale, the break rules, and that marks are drawn in every language.
- `UiFonts`' faces, the gathered codepoint sets, the batches, runs in `Draw` and `Measure`, `ModernUi.Wrap`'s rule, the faces rebuilt on `Text.Use`.
- The CJK face is an OFL Noto Sans (JP, KR, SC or TC as the first language needs), downloaded with the user's OK and credited in `docs/art/CREDITS.md`, its licence file shipped beside the Nunito ones as `TheInterfaceFontShipsWithTheBuild` requires; one weight only, since its size goes against the build's.
- Tests: every character a shipped language's files use has a glyph in the face it is drawn with (raylib's `GetGlyphIndex` against the gathered set); English still loads one atlas per weight; the break rule on a CJK line, a Latin line and a line of both.
- Harness: the `menus`, `battle` and `story` runs under a CJK pseudo-language (`xx-cjk`: the brackets become fullwidth and a kana or a hanzi is prefixed) read for "?"; `profile`'s interface pass before and after, since runs switch textures: the budget is 8 ms on High and the interface should not gain more than a tenth of a millisecond.
- **Done when** a line of Japanese, Korean and Chinese draws without a "?" at every size the typography scale lists, and English's pictures are unchanged.

### N6 · The first translation
- The language the user chooses (decision 1), as a worked example of all of it: `ui.json` (about five hundred keys), the names file (free, from N3), the scripts' and overlays' files for everything plan 02 has written by then (184 lines today), the places, the trainer classes. The lines are our own, written as the English ones were; a species' name is PokeAPI's official one.
- The language is offered on the options row and its own name drawn in its own script.
- Tests: the report for the language shows nothing untranslated at the end of the session (its number is pinned, and rises as later sessions write scripts until they translate theirs); the longest line of each screen measured against its panel's width.
- Harness: `menus`, `battle`, `intro` and `story` under the language, read page by page for clipped or overflowing text.
- **Done when** a new game can be played from the title to Oreburgh City in the language without an English word on screen.

## Risks

- **Text that is wider than its panel.** German and French run a third longer than English; the battle's box holds two lines and the options' pills a word. N2's floor test and N6's width test catch what a translator writes, but a panel may need to grow: that is a style-guide change ("Battle panels", "Menu screens") before the code, and plan 12 · Q8's `TextSize` wraps to three lines where there is room, which helps here too.
- **Atlas memory and start-up.** Japanese uses two to three thousand distinct characters across the game's text; at 72 px in batches of six hundred that is five atlases of 2048², built in well under a second when the language is chosen, and English pays nothing. A CJK face at 160 px would be four times the memory for a sharpness no CJK text on screen needs.
- **Grammar the table can't say.** Gendered articles before a species' name ("le Tortipouss"), Korean particles that change with the final sound, counters: the forms mechanism covers number and the player's look; a language that needs more gets a rule in `Language.cs` with a test, not a key per case. The first translation shows which rules are real.
- **Scripts move under the translations.** Every chapter of plan 02 adds lines; the report keeps the gap visible and a stale entry falls back to English rather than lying. A translation is finished per session, never a blocker on the story.
- **The battle's tests read English.** Every scenario test asserts on log text; English stays the fallback and the tests' language, so moving a line into the table must not change its English by a character, which the `en` diffs of N1 and N2 hold.
- **Trademarks.** Species' names in other languages are the games' official names, as the English ones are, and no more: no text of the games is read.

## Needs and gives

- **Needs**: nothing first. N3 needs the importer's pinned PokeAPI checkout (plan 03) and reads nothing new of the decompilation. N5 needs the user's OK for one Noto face.
- **Gives**: to plan 02, lines that can be translated as they are written (`extract` gives a chapter's skeleton); to plan 09 · L3, the rule that markup passes through a translation untouched and the subset test that holds it; to plan 12 · Q9, a `captions.` section its lines go in; to plan 11 · C1, the `classes.` section its table's names show through; to plan 07, the rule that the data version hashes identifiers only, so two players in different languages battle on the same data.

## Decisions for the user

1. **Which language first.** *Recommended:* the one the user reads, so the worked example can be judged by eye; French, German, Spanish and Italian need no new face (N6 can precede N5), Japanese, Korean and Chinese need N5 first. Alternative: Japanese, as the original's own language, with N5 before N6.
2. **Descriptions per language.** *Recommended:* PokeAPI's own `short_effect` prose where a language has it, English otherwise, counted in the report; never the games' flavour texts. Alternative: English descriptions in every language until a translator writes them.
3. **Where English lives.** *Recommended:* in `en/ui.json`, keys in the code, so the game can be re-worded without recompiling and the pseudo-language finds every literal. Alternative: English literals left in the code as the fallback and `en/ui.json` as an override, which keeps the diff of N1 and N2 smaller and the literals forever.
4. **The CJK face.** *Recommended:* one Noto Sans of the first CJK language, one weight, OFL, with its licence shipped. Alternative: a smaller OFL face if the download's size matters.
5. **The language in the save.** *Recommended:* not kept; the save holds identifiers only and any save opens in any language. Alternative: the language a game was begun in recorded for the Trainer Card, as Platinum's own game records it, read and never used otherwise.

## Status

- [ ] N1 The string table, the language row and the menus
- [ ] N2 The battle's lines and the introduction
- [ ] N3 Names in every language, from the importer
- [ ] N4 Scripts, overlays and the report
- [ ] N5 Fonts beyond Latin-1
- [ ] N6 The first translation
