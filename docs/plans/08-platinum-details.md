# Plan 08 · Platinum's remaining details

Written 2026-10-06, before any session.

**Goal**: the pieces of Platinum itself that no plan names: its battle options and a quicker battle, the six pages of a Pokémon's summary, the level-up panel, the overlays' trainers read from the table plan 06 · R9 imported instead of typed by hand, Jubilife TV's programmes and the furniture that talks, the Vs. Recorder, Spiritomb's tower without the Underground's greetings, and Pokédex entries written in our own words instead of generated data sheets.

## Where we are

- **Options**: `OptionRow` (`UI/OptionsScreen.cs`) has twelve rows (`TextSpeed`, `Quality`, `WindowSize`, `Fullscreen`, `VSync`, `TimeOfDay`, `Sound`, four volumes, `Speakers`), kept by `GameSettings` in `settings.json`; `SpeakerMode` is `Stereo` or `Handheld`. None of Platinum's Battle Scene, Battle Style or Sound Mono rows exist. The pace of a battle is fixed: `BattleAnimator.AttackTime` 0.65 s, `EffectTime` 1.3 s, `ImpactTime` 0.35 s, `BattleEngine.HitDelay` 0.35 s, and every line waits for a key (`waitingForMessageConfirm`); the text speed is the only control. `GameEngine.Update` hands `dt` unchanged to `battle.Update`, `UpdateDialogue`, `evolutionScreen.Update` and `UpdateTransition`.
- **The foe's next Pokémon comes in unasked**: `BattleCore.ReplaceFainted` (`Battle/Sim/BattleCore.Switch.cs`) sends the foes' from their roster first and yields a `ReplacementRequest` only for the player's empty places. `BattleRequest` has two kinds (`ActionRequest`, `ReplacementRequest`); `BattleRecord(Seed, Answers)` is every answer given from outside, and `BattleCore.Replay` plays one back.
- **Summary**: one page, `ModernUi.DrawSummary` (`UI/ModernUi.Party.cs`), reached through `PartyScreen.ShowSummary` (style guide, "Summary"). `Pokemon` carries `Nickname`, `Friendship`, `Beauty`, `Personality`, `Ball` and `EvolutionProgress` but no original trainer, met place, level or date, and no markings; `SavedPokemonData` the same. Plan 07 · O2 owes the original trainer, met data, EVs and an id for trading, and hasn't started. `PCScreen` and the battle's switch panel open no summary.
- **A level**: `BattleCore.cs` emits `ExpGained` and `LevelRose(Pokemon, Level)`; `BattleEngine.Show` levels the shown copy, plays `MusicRole.FanfareLevelUp` and remembers who grew (`LeveledUp`); `BagScreen` levels with a Rare Candy through `GainExp`. Nothing shows the six stats and their gains. Plan 06 · R10 owes the move-learning prompt that follows a level.
- **Trainers**: plan 06 · R9 (done 2026-10-06, as this plan was written) imported Platinum's trainer table. `tools/DataImporter`'s `Importer.Trainers.cs` reads `/res/trainers/data/` of the pinned commit, with the classes' prize multipliers and genders (all in `Sources.DecompFolders`), and writes `Data/trainers.json`: a `TrainerRecord` per trainer (`Data/TrainerDatabase.cs`: class, name, AI flags by name, battle items, the double flag, prize money) with a `TrainerPokemonRecord` per Pokémon (species, form, level, IV scale, personality, item, chosen moves); `docs/data-files.md` describes it. `tools/MapImporter/WorldWriter.cs` writes an object's trainer type, sight and script name (`AreaObject.Trainer`, `Sight`, `Script`: `TRAINER_YOUNGSTER_TRISTAN`), and `WorldMapBuilder.PlaceEvents` (`Data/World.cs`) fills the placed `Trainer` from the table by that script (`TrainerDatabase.Fill`): its `Ai` and `Items` always, and its team built as the original's `TrainerData_BuildParty` builds it (`TrainerDatabase.Build`: IVs from the scale, nature, gender and ability from the personality, the held item) where the overlay names the same team or none. `TrainerAi` thinks with `Trainer.Ai` and uses `Trainer.Items`; `TrainerAiTests` holds every trainer of Platinum buildable and a map's trainer taking its team and mind. What is left: all sixteen overlays with trainers repeat the table's class and name in their `trainer` blocks (`MapFile.TrainerRecord`) beside the lines, and eight of them (the south-west's, written before R9) type a whole team by hand as well (`party`, `prizeMoney`), a second copy of what the table has. Those eight also give their 27 trainers ids of their own (`trainer_tristan`, `trainer_liv_and_liz`) where the table and the later overlays use the original's (`youngster_tristan`); a save keeps a beaten trainer by that id (`StoryState.DefeatedTrainers`; `docs/data-files.md`: never change it for a trainer already in the game). `tools/DataImporter/README.md` doesn't mention the table.
- **Furniture**: `TileBehavior` has `Television`, `TownMap`, `Bookshelf` (three sizes) and `TrashCan`, and the hand-made rooms' TVs are props (`PropType.Television`; `"type": "Television"` in `Data/maps/PlayerHouse.json`). `GameEngine.TryInteract` reaches a person, a counter (`Map.IsCounter`), a signboard, a hidden item, then what the tile ahead is (the original's `Field_TileBehaviorToScript`: a waterfall, a rock face, water, through `FieldScripts.Waterfall`, `RockFace` and `Water`); nothing answers a TV, a shelf or a bin, and the hand-made rooms have TVs and shelves (`PropType.Bookshelf` in `RivalHouse.json`) but no bin. Jubilife TV stands without a room (plan 01 · M4; its room is M11); plan 02 · S4 has the Twinleaf opening's TV scene only. `UI/Kit/NameEntry.cs` is the one GPU-free picker, used for the player's name alone.
- **Vs. Recorder**: `items.json` has it (id 465, `KeyItems`, `fieldUse` `VsRecorder`, `canBeRegistered`); `BattleCore.Record` and `Replay` work and `BattleCoreTests.ARecordedBattleReplaysTheSame` holds them; nothing writes a record to disk or shows one, and `GameEngine.EndBattle` only trims models and fades. Plan 07 keeps replays on its server for bug reports and leaves the wire format (events name Pokémon by reference) to O3. Looker stands in Jubilife with stand-in lines (`overlays/jubilife_city.json`); plan 02 · S5 writes his scene.
- **Spiritomb**: `Data/WorldModels.cs` knows `r209s02` as `PropType.Cairn`, "the Hallowed Tower"; Route 209 is open since plan 01 · M6 (`overlays/route_209.json`), the tower standing on it as a prop; the original's way to it is four tile events of type 0 round the tower in `areas/route_209.json` (script `2`, `AreaSign`), which nothing reads yet: `WorldMapBuilder.PlaceEvents` reads only the hidden items (type 2), and plan 01 · M6 left the type-0 events, the Unown inscriptions' too, to plan 02 · S7; `habitats.json` lists no Spiritomb; the Odd Keystone is in `items.json`. Plan 06 rules the Underground's multiplayer out (its decision 7) and R16 builds the Underground for one player; plan 03 · D12's completeness test isn't written yet.
- **Entries**: `PokemonSpecies.DexEntry` is one sentence `Importer.DexEntry` composes from the data (name, category, types, region, height, weight, the line's evolutions); the Pokédex's INFO page (plan 03 · D10, `UI/ModernUi.Pokedex.cs`) shows it. Plan 03 · decision 1 generated entries so that no game text is used; there is no habitat word (`habitats.json` says where a species is met, for the AREA page, not Platinum's word for the kind of place it lives in).

## Design

**Platinum's defaults don't move.** Battle Scene on and Battle Style Shift, as the original ships; the speed-up, auto-advance, text frames and autorun are our own rows beside them, off unless chosen, and so is P10's offer to record a story battle. Everything here keeps two harness runs drawing the same pictures: nothing new rolls outside `Dice` or the battle's generator, and nothing reads a clock but `FrameClock` and the game's own.

### Options and pace

- **Rows** (`OptionRow`, `GameSettings`): `BattleScene` (on/off), `BattleStyle` (`BattleStyle.Shift`/`Set`, a new enum), `Speakers` gains `SpeakerMode.Mono` (the mixer folds its two channels in `AudioMixer.Render`), `Frame` (the text box's frame, our own six), `SpeedUp` (off, ×2, ×3, used while Run is held), `AutoAdvance` (off, slow, fast), `Autorun` (on/off). All in `settings.json`; `OptionsScreen.Describe` gets a line for each and `GameEngine.ApplySettings` pushes them. Button Mode goes with key rebinding, which plan 12 · Q4 owns as presets of its bindings; the Rumble Pak has no place here.
- **Battle Scene off** is a mode of the face, never of the rules: `BattleEngine.Show` skips `Anim.Attack` on `Lunged` and makes no `EffectCue` on `MoveShown` (the move's sound still plays), and what a line carries in `OnImpact` lands at once instead of `HitDelay` later, through the same `pendingEffects` so damage still lands in the log's order. The ball's throw and the send-outs stay, as in Platinum. The core and the log don't change, so a record replays the same with the scene on or off. The style guide's "Move effects (G8)" gets a line for the scene off before the code.
- **Battle Style Shift** is a question the rules ask: a new `SwitchOfferRequest(Place, Pokemon upcoming)` beside `ActionRequest` and `ReplacementRequest`, yielded by `ReplaceFainted` after the line "<trainer> is about to send in <X>. Will you switch?" (a `Said`, so the harness can show it) and before the foe's `Entered`, only in a single trainer battle, only when the player's Pokémon is standing and `CanSendIn` finds anyone, and only when `CoreSetup.OffersSwitch` is set. It is answered from outside with a new `BattleChoice.Stay(place)` (`BattleChoice.GoOn` is taken: the core's own answer for a Pokémon held to its move) or `BattleChoice.Switch(place, index)`, checked by `WhyNot`, and so lands in `BattleRecord.Answers`: a replay holds. `GameEngine` passes the setting to the battle through `BattleSetup` (a new `OffersSwitch`, off unless given; Set never asks), `BattleEngine` hands it to `CoreSetup`, opens the switch panel on the request (`BattleEngine.Menus.cs`), and a cancel answers Stay. The core's default is not to ask, so `CoreScenario` and every test that builds a `BattleEngine` itself keep the turns they have. The style guide's "Battle panels" says how the offer opens the switch panel before the code.
- **The speed-up** multiplies the `dt` the engine hands to the Battle, Dialogue, Evolution and Transition states while Run is held (`GameEngine.SpeedUp`, a property the key sets and the harness sets directly). Presentation is a function of time (the harness's `Skip` proves it) and cue seeds come from the animator, so no roll changes. Dialogue runs at `CharactersPerSecond` times the same factor.
- **Auto-advance** gives `waitingForMessageConfirm` a timer once the line is whole (`BattleEngine.AutoAdvanceAfter`, null when off), and `UpdateDialogue` the same for plain lines; a question (`DialogueManager.IsQuestion`) always waits.
- **Autorun**: `Player.Update` reads Run as "walk" when `Player.Autorun` is set; `Advance` is unchanged, so tests and the harness walk as before. Once plan 02 · S4 gates running on the Running Shoes, autorun obeys the same gate.
- **Frames**: `ModernUi.DrawDialogue` takes the frame's number; each frame is drawn with `UiShapes` (a double rule, a wooden edge, a stone edge, a dark glass, a ribbon), never pixel art from the games. The style guide's "Field menus and notices" gets a text-box paragraph with the six before the code.

### The summary

- **Pages** (`SummaryPage`: `Info`, `Memo`, `Skills`, `BattleMoves`, `ContestMoves`, `Ribbons`) live in a GPU-free `SummaryView` (`UI/SummaryView.cs`: `Open(party, index)`, `Move`, `Sideways`, `Confirm`, `Cancel`, a cursor over the moves on the move pages) painted by `ModernUi.Party` painters, one per page. `PartyScreen` keeps `ShowSummary` and owns a view; `PCScreen` and the battle's switch panel (`BattleMenuState.SwitchPokemon`) open the same view on the Pokémon under the cursor. Left and right turn pages, as the Pokédex's do (there are no shoulder buttons). The style guide's "Summary" is rewritten for six pages first.
- **The memo's fields** go on `Pokemon` and `SavedPokemonData`: `OriginalTrainer`, `OriginalTrainerId`, `MetLocation` (the area's `DisplayNameAt`), `MetLevel`, `MetDate` (the day, from a new `GameClock.Today` that is the system's day unless a tool fixes it as it fixes `FrameClock`, so the memo's shot is the same every run), `Markings` (Platinum's six, a bit each; the summary shows and sets them, and the box's own marking menu is plan 06 · R12's, whose "Tools" row has PC storage with markings). They are set where a Pokémon becomes the player's (`BattleEngine.Show`'s `Caught`, `GivePokemon`, the starter) and copied by `Pokemon.CopyStateFrom`. A Pokémon from an older save is taken to be the player's own, met at an unknown place at its current level: that is the one migration, in `SavedPokemonData.ToPokemon`. If plan 07 · O2 hasn't run, P3 takes these fields over from it; O2 then adds `Id`, EVs, `SecretId` and `IsTraded` on top, and `ReceiveTradedPokemon` keeps the giver's memo.
- **The characteristic** is a new `Characteristics.Of(pokemon)` (`Models/Characteristics.cs`; plan 06's "Growth" row lists characteristics, which no R session's line names and nothing in battle reads, so they are written here, where they are shown, and R10 takes them as done): the stat of the highest IV, ties broken from the personality value, the line by that IV's remainder by five, as the original decides; the thirty lines are ours. The nature's line and the ability's text come from the data.
- **Contest Moves and Ribbons** are frames with "nothing yet" until plan 06 · R17 fills them.

### The level-up panel

`LevelRose` gains `Before` and `After` (`StatSpread`s the core fills where it emits the event). `BattleEngine.Show` turns it into a message step with a panel (`BattleHUD.LevelUp`, drawn by `ModernUi.Battle`): the six stats with the +n column first, a press, the new values, a press, then the fanfare's line as today; it comes before any move the level teaches, which is R10's prompt. `BagScreen` sets the same panel (a new `LevelUpPanel`, the two spreads and the press it waits for, shared by both) for a Rare Candy and `ModernUi.Items` draws it over the party cards. The style guide's "Battle panels" describes the panel before the code.

### The trainer table

- **The table is R9's** (above): `Data/trainers.json`, keyed by the original's constant without its prefix (`youngster_tristan`), read by `TrainerDatabase` and never carrying a line of what a trainer says. Nothing here reads the decompilation again.
- **The overlays read from it alone**: the overlay's `trainer` block shrinks to our lines (`dialogueBefore`, `dialogueAfter`) and `TrainerDatabase.Fill` gives the rest, as it gives the class, name and prize money today for a block that names no team; it gives the `id` as well (the table's, `youngster_tristan`), which it doesn't yet. The 27 ids the south-west's overlays made up (`trainer_tristan`) become the table's, and a step of `StoryMigration.Upgrade` (with `StoryState.CurrentVersion` raised) renames them in an older save's beaten trainers, so nobody beaten comes back. A block that still carries a `party` keeps winning, as `Fill` already lets it, for Kanto's hand-made maps and for tests (`MapFile.TrainerRecord` keeps its fields). A test holds that no overlay of the imported world carries a `party`, `prizeMoney`, `trainerClass`, `name` or `id`; `docs/data-files.md`'s overlay section says the block is lines and its paragraph on a trainer's `id` says that in the imported world the id is the table's, and `tools/DataImporter/README.md` says what it reads from `res/trainers/data` (the prize money is the class's multiplier times the last Pokémon's level times four, as `SouthWestTests` expects).

### Furniture, broadcasts and phrases

- **Facing a thing**: `TryInteract`'s last step, what the tile ahead is (where the original's `Field_TileBehaviorToScript` reads its TVs, shelves, bins and town maps as well as its water), gains a first check: the prop under the tile ahead (`Map.Props`, `Prop.Covers`) or the tile's behaviour names a common script (a new `FieldScripts.ForFurniture` beside `FieldScripts.For`: `common.Television`, `common.Bookshelf`, `common.TrashCan`, `common.TownMap`), unless the map gives that tile a script of its own (a new `Map.TileScripts` by tile, which a hand-made map's file fills with a `tileScripts` beside its `signScripts` and P12 fills from an area's tile events: the Twinleaf opening's TV, plan 02 · S4). The rooms M11 imports carry the behaviours already.
- **Broadcasts** (`Story/Broadcasts.cs`, GPU-free): a programme is lines composed from the save and the story (the Pokédex's counts, the badges, the time played, the clock's hour, the last interview's phrases, R14's lottery once it exists, "the draw is another day" until then), chosen by `Dice`. A new op `broadcast` asks the host (`IScriptHost.Broadcast()`, `HeadlessScriptHost` writes what it would have said), with its row in `docs/scripts.md`, a case in both hosts and a line in `ScriptTests.EveryCommand`.
- **Phrases** (`UI/Kit/PhraseEntry.cs`, GPU-free like `NameEntry`; `ModernUi.DrawPhrases`): two words from groups of our own (`Data/PhraseGroups.cs`: Pokémon, moves, types, feelings, people, places, time, small words), never the original's lists; the style guide's "Field menus and notices" gets the picker's paragraph before the code. A new op `phrase` asks for one into `StoryState.Phrases` (saved as `SaveData.StoryPhrases`, empty by default, no migration), and a reporter's `interview` script is an ordinary script that asks and then sets the flag the TV reads. Mail (R11) and the Trainer Card's self-introduction (R12) reuse the picker.

### Recorded battles

- **The file** (`Battle/Sim/BattleRecordFile.cs`, GPU-free): the `BattleRecord` with everything the core needs to replay it, as plain data: both parties as `SavedPokemonData`, the trainers as `TrainerRecord`, the format, the rules preset, the `BattleConditions`, the player's name, the day (`GameClock.Today`, P3's), and a data version (a new `GameDataFiles.Version`, a hash of the five data files, which plan 07's rooms will use too). A replay rebuilds its parties from the file, so events name the replay's own objects: this is plan 07 · O3's wire format, once for both.
- **Writing** happens in `GameEngine.EndBattle`: a trainer battle, the Vs. Recorder in the bag, and a yes to "Record this battle?"; the Hall of Fame's battles always, without a word. Platinum records only its link and Frontier battles, so a question after a story battle is our own and is asked only when a new options row, `RecordBattles` (off unless chosen, beside P2's rows), is on. Files go to `replays/` beside `SaveManager.SaveFilePath`, named by the day and the foe; the harness's working directory keeps them out of a real save's folder.
- **Watching** is `BattleEngine` in a watch mode (`BattleEngine(BattleSetup, BattleRecord)`): the core is given `Replay`, the answers come from the record instead of the menus, the menus never open, input does nothing but Run held (the speed-up) and Menu (leave; not Cancel, because Run shares X and the pad's B with Cancel, so holding it would leave at once), the HUD, animator and sounds run as for any battle, and nothing touches the party or the bag. A list screen (`ReplayListScreen`, `ModernUi.Records`: `ListRow`s of foe, day and result, after a paragraph in the style guide's "Menu screens") opens from USE on the Vs. Recorder in the bag, through the bag's own way for a key item used in the field (`BagScreen.UsedInField`, which gains the item's `fieldUse` `VsRecorder` beside the Bicycle's, the rods' and the Escape Rope's, and `TakeFieldUse`, acted on by `GameEngine.UseFieldItem`; R11 generalises it). A file of another data version is refused with a notice.

### Spiritomb

The tower is reached where the original reaches it: its four tile events of type 0 (above). Plan 01 · M6 left those events to plan 02 · S7; if S7 hasn't run, P12 reads them, and S7 then gives the Unown inscriptions theirs the same way. An overlay gives a type-0 event a script of the area's file by the event's script number (a new `tileScripts`, as `triggers` does for the original's triggers), `WorldMapBuilder.PlaceEvents` puts it on its tiles in P8's `Map.TileScripts`, and P8's step in `TryInteract` runs it on facing one; the prop (`PropType.Cairn`) stays scenery, and the overlay's `props` stay our own street furniture. The script: without the keystone a line; with it, "Set the Odd Keystone in the tower?" takes the item and sets a flag; once the greetings are enough, `wildbattle "Spiritomb" 25` once. The count is `StoryState.Greeted` (the keys of people spoken to, saved as `SaveData.GreetedPeople`, empty by default), filled by `TryInteract`, read by scripts as the built-in variable `GREETINGS` beside `PARTY_COUNT`. The stand-in goes in `docs/mechanics/rulings.md` with the table's three columns. Where a Pokémon no table lists is met goes in a hand-written `Data/world/sinnoh/special.json` that `Habitats` reads beside `habitats.json` (the importer's `--data` never touches it), so the AREA page and D12's test count it.

### Written entries

- **The file** is `Data/dex-entries.json`, read by `DexEntries` (`Data/DexEntries.cs`): for each species and form name, two entries and a habitat word from Platinum's nine (grassland, forest, water's edge, sea, cave, mountain, rough terrain, urban, rare). The importer never writes it; `PokemonSpecies.DexEntry` stays as the fallback for a name the file lacks. The INFO page shows a written entry, the two in turn on each opening, and the habitat word beside the height and weight (the style guide's "Menu screens", its Pokédex entry's INFO page, first).
- **Our own words.** Plan 03 · decision 1 keeps its purpose (no game text) and changes its means: entries written by us in the manner of a Pokédex, never a close paraphrase of Platinum's or PokeAPI's flavour text. `DataFileTests` holds the shape: two entries for every species and form, 15 to 45 words each, none alike, none the generated sentence, every habitat word from the list. The words themselves are checked by the importer, whose git-ignored cache holds PokeAPI's flavour texts (`pokemon_species_flavor_text.csv`, in the CSV folder it checks out already, Platinum's own entries among them): `--check-entries` reports any run of six words shared with any of them, and the session rewrites those. It can't be a unit test, because the original's text must never enter the repository.
- **Batches**, in National order like plan 03's models: the Sinnoh Pokédex's 210 first (they are met first), then the rest by region (Kanto and Johto's 169; Hoenn's 107, the seven Generation 4 species the Sinnoh Pokédex leaves out and Unova's 156; the 376 from Kalos to Paldea), then the 541 forms. Each batch after P13 writes its entries and habitat words in National order, runs `--check-entries` and rewrites whatever it finds, raises the `DataFileTests` floor to its last name, and adds a shot of one of its species' INFO page (`26n_pokedex_<species>`).

## Sessions

### P1 · Platinum's battle options
- The style guide's lines first ("Battle panels" for the offer, "Move effects (G8)" for the scene off); `OptionRow.BattleScene`, `BattleStyle` and `SpeakerMode.Mono`; `SwitchOfferRequest` and `CoreSetup.OffersSwitch` in the core, `BattleSetup.OffersSwitch` from the settings, the offer's panel in `BattleEngine.Menus.cs`, the scene-off path in `BattleEngine.Show`; the mixer's fold.
- Tests: a `CoreScenario` where Shift offers and Set doesn't, the offer's answer in the record and its replay, a switch made at the offer entering before the foe's; the log's events land in the same order with the scene off; `OptionsCycleThroughTheirValues` with the new rows.
- Shots: `demo` and `conditions` run with the scene off (`d*_scene_off`), a `battle` shot of the offer (`23b_battle_offer`), the options' new rows in `look_8_options`.
- **Done when** a trainer battle under Shift asks before the foe's next Pokémon, Set and the scene-off change no outcome, and `diff` of `all` before and after lists no shot but those taken after a trainer's Pokémon faints with another to come, which now show the offer.

### P2 · A quicker battle, of our own
- `SpeedUp`, `AutoAdvance`, `Autorun` and `Frame` rows; `GameEngine.SpeedUp`; `BattleEngine.AutoAdvanceAfter` and the field's; `Player.Autorun`; six frames in `ModernUi.DrawDialogue` after the style guide's paragraph.
- Tests: a sped battle reaches the same log as a slow one; auto-advance never answers a question; autorun walks at the running pace with Run up; the frame number round-trips through `settings.json`.
- Shots: the six frames over one line (`look_9_frames_*`), a battle at ×3 against the same at ×1 (`d*_fast`).
- **Done when** holding Run plays a battle at three times the pace with the same result, and the defaults draw what they drew.

### P3 · The memo's fields and the first three pages
- `OriginalTrainer`, `OriginalTrainerId`, `MetLocation`, `MetLevel`, `MetDate`, `Markings` on `Pokemon` and `SavedPokemonData`, set at the catch, the gift and the starter; `GameClock.Today`, which the harness fixes; the migration in `ToPokemon`; `Characteristics`; `SummaryView` with `Info`, `Memo` and `Skills`, painted after the style guide's "Summary" is rewritten.
- Tests: an older save's Pokémon reads as the player's own; a catch records the area's name and the level; `ACopyTakesOnEverythingThatCanChange` passes with the new fields; the characteristic for hand-worked IVs.
- Shots: `20b_summary` (INFO, kept for the compare board), `20d_summary_memo`, `20e_summary_skills`.
- **Done when** a Pokémon caught on Route 202 says so on its memo, with the day and the level, after a save and a load.

### P4 · The move pages and the other doors
- `BattleMoves` with a cursor and the chosen move's description, `ContestMoves` and `Ribbons` as empty frames; the view opened from `PCScreen` and from the battle's switch panel.
- Tests: the cursor over four, three and one move; the PC's and the switch panel's way in and back.
- Shots: `20f_summary_moves`, `20g_summary_contest`, `30d_pc_summary` (`30b` and `30c` are the box's), `23c_battle_switch_summary`.
- **Done when** every page is reached from the party, the PC and a battle, and the harness shows all six.

### P5 · The level-up panel
- The style guide's "Battle panels" paragraph first; `LevelRose.Before`/`After` from the core; `LevelUpPanel`, `BattleHUD.LevelUp` and its painter; the bag's panel for a Rare Candy.
- Tests: the gains add up to the new stats; the panel comes before the level's move (R10's prompt, when it exists); a Rare Candy from the bag shows the same numbers.
- Shots: a level-up added to the `demo` battle (`d2x_level_gains`, `d2x_level_stats`; later demo shots move, which `diff` will list), `22g_bag_rare_candy_panel` (`22c` to `22f` are the bag's already).
- **Done when** a level in battle and from the bag shows two pages of numbers that match the Pokémon.

### P6 · The trainer table imported
Done by plan 06 · R9 on 2026-10-06, as this plan was written: `tools/DataImporter`'s `Importer.Trainers.cs` writes `Data/trainers.json` (927 trainers: class, AI flags, items, the double flag, prize money, and each Pokémon's species, form, level, IV scale, personality, item and chosen moves), `TrainerDatabase` reads it, `TrainerDatabase.Fill` gives a placed trainer its mind, items and team, and `TrainerAiTests` holds every trainer buildable. The session number stays so that the other plans' references keep their meaning: "plan 08 · P6" now means R9's table.
- **Done when** (met) the importer writes the whole table and the open areas' trainers take their teams from it.

### P7 · The overlays read from the table alone
- What R9 left: the sixteen overlays of the imported world with trainers lose every field of their `trainer` blocks but the lines (`party` and `prizeMoney` in the south-west's eight, `trainerClass`, `name` and `id` in all sixteen), so the table is the only copy; `TrainerDatabase.Fill` gives the id; the 27 made-up ids become the table's, with their step in `StoryMigration.Upgrade` and the tests that name them (`SouthWestTests`, `PokemonTests`, `DataFileTests`: `trainer_tristan` becomes `youngster_tristan`) following; `MapFile.TrainerRecord` keeps every field for Kanto's hand-made maps and the fixtures. `docs/data-files.md`'s overlay section and its paragraph on ids, `docs/plans/README.md`'s "Opening an area" and `tools/DataImporter/README.md` say so.
- Tests: no overlay of the imported world carries a `party`, `prizeMoney`, `trainerClass`, `name` or `id`; every placed trainer of every open area matches the table (id, class, name, species, levels, items, moves); a save of the last story version with `trainer_tristan` beaten loads with `youngster_tristan` beaten; a hand-typed party on a hand-made map still wins; a Youngster's prize money is what it was.
- Shots: `world` and `story` unchanged but for the trainers whose overlay team differed from the original's (compare the two trees field mode by field mode, as CLAUDE.md says for a change to the world's people).
- **Done when** no overlay of the imported world carries a team, a class, a name or an id, an older save keeps every trainer it had beaten, and plan 02's next chapter adds a route's trainers by writing their lines alone.

### P8 · Furniture and the programmes
- `FieldScripts.ForFurniture`, the furniture check in `TryInteract`, `Map.TileScripts` and a map file's `tileScripts`, the four common scripts; `Broadcasts`; the `broadcast` op with its row, cases and `EveryCommand` line.
- Tests: facing a TV runs `common.Television`, a shelf `common.Bookshelf`, and a `TrashCan` or `TownMap` tile on a map of the test's own its script; a tile with its own script wins; a programme on a known save gives known lines under a seeded `Dice`.
- Shots: `story` mode faces the TV in the player's house and a shelf in the rival's (`st*_television`, `st*_shelf`); no hand-made room has a bin, so the bin waits for M11's rooms.
- **Done when** every TV in the game says something true about this game, and the opening's TV still plays its scene.

### P9 · Phrases, interviews and reporters
- The style guide's paragraph first; `PhraseEntry` and its groups, `ModernUi.DrawPhrases`, the `phrase` op, `StoryState.Phrases` and `SaveData.StoryPhrases`; Jubilife's reporters as overlay people with an interview script; the programme that reads the answers back.
- Tests: the picker's cursor and groups; a phrase saved and loaded; the interview played headlessly to its end on every way through.
- Shots: the picker open (`st*_phrases`), the interview's question.
- **Done when** an interview given in Jubilife City is heard on the TV at home.

### P10 · The Vs. Recorder's file
- `BattleRecordFile`, `GameDataFiles.Version`, the `RecordBattles` row, the question at `EndBattle`, the `replays/` folder.
- Tests: a file round trip; a battle replayed from its file ends in the same `BattleResult` with the same lines; a file of another version is refused; with the row off no question is asked; `OptionsCycleThroughTheirValues` with the row.
- Shots: the question after the `story` mode's trainer battle with the row on (`st*_record_question`); with the row off that mode's shots are unchanged.
- **Done when** every trainer battle fought with the recorder in the bag and the row on can be written out and replayed headlessly, and with the row off nothing in a battle's end has changed.

### P11 · The replay viewer
- The style guide's "Menu screens" paragraph first; the watch mode of `BattleEngine`, `ReplayListScreen` and its painter, USE on the Vs. Recorder through `BagScreen.UsedInField` and `GameEngine.UseFieldItem`; Looker's gift waits for plan 02 · S5 (the harness puts the item in the bag).
- Tests: watching changes nothing in the party, the bag or the money; Menu leaves at any line and Run held never does; USE on the Vs. Recorder hands it to the field (`TakeFieldUse`).
- Shots: the list (`31c_replays`), a replay at its third turn (`31d_replay_watching`; `31` and `31b` are the save's).
- **Done when** a recorded battle is watched from the bag at ×3 and the save afterwards equals the save before.

### P12 · Spiritomb's tower
- The tower's tile events read (an overlay's `tileScripts` into P8's `Map.TileScripts`) unless plan 02 · S7 has read them already, `route_209.txt`'s scripts, `StoryState.Greeted` and `GREETINGS`, `special.json` and its reading in `Habitats`, the rulings' row. Route 209 is open (plan 01 · M6), so the scripts are tested on the route itself and on a map of their own.
- Tests: the tower's every way through (no keystone, the keystone set, too few greetings, the battle); thirty-two people counted once each; Spiritomb's AREA page names the tower.
- Shots: `story` mode at the tower (`st*_hallowed_tower`).
- **Done when** a game that has spoken to thirty-two people and set the keystone meets Spiritomb, and the species has a place in the Pokédex.

### P13 · The entries file and the Sinnoh Pokédex
- `Data/dex-entries.json`, `DexEntries`, the INFO page's written entry and habitat word (the style guide first), `--check-entries` in the importer, the `DataFileTests` shape (which until P17 holds only the names the file has), and the 210 species of the Sinnoh Pokédex: 420 entries.
- Shots: `26_pokedex_turtwig` (kept), `26m_pokedex_second_entry`.
- **Done when** every species of the Sinnoh Pokédex has two entries and a habitat word, the check reports nothing shared, and the page shows them.

### P14 · Entries: the rest of Kanto and Johto
- The 169 species from Bulbasaur to Celebi that the Sinnoh Pokédex leaves out: 338 entries, written and checked as the design's "Batches" says.
- **Done when** every species to Celebi has two entries and a habitat word, and the check reports nothing shared.

### P15 · Entries: the rest of Hoenn, Sinnoh and Unova
- Hoenn's 107 left out, the seven Generation 4 species the Sinnoh Pokédex leaves out (Heatran, Regigigas, Cresselia, Phione, Darkrai, Shaymin, Arceus) and Unova's 156: 540 entries, written and checked as the design's "Batches" says.
- **Done when** every species to Genesect has two entries and a habitat word, and the check reports nothing shared.

### P16 · Entries: the rest of Kalos, Alola, Galar and Paldea
- The 376 species from Chespin to Pecharunt: 752 entries, written and checked as the design's "Batches" says, in two sittings if one session can't hold them (the floor shows where the first stopped).
- **Done when** every species has two entries and a habitat word, and the check reports nothing shared.

### P17 · Entries: the 541 forms
- Every form by its Showdown name (`Charizard-Mega-X`), in National order of its species, written and checked as the design's "Batches" says; decision 3 may narrow them. The test then holds every species and form.
- **Done when** `DataFileTests` holds the whole file and `DexEntries` gives each form its own entries (a page that shows them is the forms page plan 03 · D10 left for later).

## Risks

- **The Shift prompt changes the shape of every trainer battle**: the game's and the harness's trainer battles gain a question. P1 keeps the core's and `BattleSetup`'s default silent and turns the offer on from the settings alone, so tests that drive `BattleEngine` keep their turns, and runs `all` before and after.
- **Two owners of one save field**: the memo's fields are O2's as well. Whichever session runs first adds them; the other reads them. The migration is written once.
- **Beaten trainers forgotten**: a save keeps a beaten trainer by the overlay's id, and P7 changes 27 of them. The step in `StoryMigration.Upgrade` renames them in an older save, and P7's test loads one.
- **Broadcasts without a calendar**: the lottery and the weekday programmes wait for R14's clock; P8 writes the programmes that need none.
- **A replay goes stale** with every importer run that changes a move or a species: the data version refuses it with a notice rather than playing it wrong.
- **Three thousand entries are slow to write**, and the overlap check needs the importer's cache: each batch is one session, and the check is run before the batch is called done.

## Needs and gives

- **Needs** plan 01 · M6 (done: Route 209 is open, for P12) and M11 (the rooms whose furniture P8 answers, the bins among them); plan 02 · S5 (Looker's gift, for P11) and S7 (whose chapter reads the type-0 tile events; P12 reads them itself if it comes first); plan 06 · R9 (done: the trainer table P7 finishes moving the overlays onto), R10 (the move prompt that follows P5's panel), R11 (which generalises P11's key-item use), R14 (the lottery programme), R17 (the two empty pages).
- **Gives** plan 02 every route's trainers from the table alone (P7) and the TV scene's tile (P8); plan 06 · R11 a phrase picker for mail, R12 one for the Trainer Card; plan 07 · O2 the memo's fields, O3 a replay file that is the wire format, and the rooms a data version (P10); plan 03 · D12 a place for the Pokémon no table lists (P12).

## Decisions for the user

1. **Where recorded battles are opened from.** *Recommended:* USE on the Vs. Recorder in the bag, as in the original. The alternative is a REPLAYS entry on the title screen as well, which costs a `TitleChoice` and its tests.
2. **Spiritomb's thirty-two greetings.** *Recommended:* thirty-two different people spoken to anywhere in Sinnoh, counted once each. The alternative is plan 06 · R16's solo Underground diggers, which makes P12 wait for R16.
3. **Which forms get entries.** *Recommended:* all 541, two each, so the forms page plan 03 · D10 left optional can show them. The alternative is only the forms that are a Pokédex entry of their own (the regional forms, Rotom's appliances, the cloaks, the seas), about 60, with Megas and Gigantamax showing their species' entries.
4. **The speed-up's key.** *Recommended:* held Run, which a battle doesn't read today (it shares X and the pad's B with Cancel, whose press only reads a line on there), with the factor an options row. The alternative is a toggle row that keeps it on, which also speeds the field's text.
5. **Jubilife TV's programmes.** *Recommended:* the five P8 names (the Pokédex, the trainer survey, the clock, the interviews, the lottery's stand-in). The alternative is the interviews alone.

## Status

- [ ] P1 Platinum's battle options
- [ ] P2 A quicker battle, of our own
- [ ] P3 The memo's fields and the first three pages
- [ ] P4 The move pages and the other doors
- [ ] P5 The level-up panel
- [x] P6 The trainer table imported (done by plan 06 · R9, 2026-10-06)
- [ ] P7 The overlays read from the table alone
- [ ] P8 Furniture and the programmes
- [ ] P9 Phrases, interviews and reporters
- [ ] P10 The Vs. Recorder's file
- [ ] P11 The replay viewer
- [ ] P12 Spiritomb's tower
- [ ] P13 The entries file and the Sinnoh Pokédex
- [ ] P14 Entries: the rest of Kanto and Johto
- [ ] P15 Entries: the rest of Hoenn, Sinnoh and Unova
- [ ] P16 Entries: the rest of Kalos, Alola, Galar and Paldea
- [ ] P17 Entries: the 541 forms
