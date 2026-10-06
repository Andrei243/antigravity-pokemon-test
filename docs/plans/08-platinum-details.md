# Plan 08 · Platinum's remaining details

Written 2026-10-06, before any session.

**Goal**: the pieces of Platinum itself that no plan names: its battle options and a quicker battle, the six pages of a Pokémon's summary, the level-up panel, the trainer table read from the original instead of typed by hand, Jubilife TV's programmes and the furniture that talks, the Vs. Recorder, Spiritomb's tower without the Underground's greetings, and Pokédex entries written in our own words instead of generated data sheets.

## Where we are

- **Options**: `OptionRow` (`UI/OptionsScreen.cs`) has twelve rows (`TextSpeed`, `Quality`, `WindowSize`, `Fullscreen`, `VSync`, `TimeOfDay`, `Sound`, four volumes, `Speakers`), kept by `GameSettings` in `settings.json`; `SpeakerMode` is `Stereo` or `Handheld`. None of Platinum's Battle Scene, Battle Style or Sound Mono rows exist. The pace of a battle is fixed: `BattleAnimator.AttackTime` 0.65 s, `EffectTime` 1.3 s, `ImpactTime` 0.35 s, `BattleEngine.HitDelay` 0.35 s, and every line waits for a key (`waitingForMessageConfirm`); the text speed is the only control. `GameEngine.Update` hands `dt` unchanged to `battle.Update`, `UpdateDialogue`, `evolutionScreen.Update` and `UpdateTransition`.
- **The foe's next Pokémon comes in unasked**: `BattleCore.ReplaceFainted` (`Battle/Sim/BattleCore.Switch.cs`) sends the foes' from their roster first and yields a `ReplacementRequest` only for the player's empty places. `BattleRequest` has two kinds (`ActionRequest`, `ReplacementRequest`); `BattleRecord(Seed, Answers)` is every answer given from outside, and `BattleCore.Replay` plays one back.
- **Summary**: one page, `ModernUi.DrawSummary` (`UI/ModernUi.Party.cs`), reached through `PartyScreen.ShowSummary` (style guide, "Summary"). `Pokemon` carries `Nickname`, `Friendship`, `Beauty`, `Personality`, `Ball` and `EvolutionProgress` but no original trainer, met place, level or date, and no markings; `SavedPokemonData` the same. Plan 07 · O2 owes the original trainer, met data, EVs and an id for trading, and hasn't started. `PCScreen` and the battle's switch panel open no summary.
- **A level**: `BattleCore.cs` emits `ExpGained` and `LevelRose(Pokemon, Level)`; `BattleEngine.Show` levels the shown copy, plays `MusicRole.FanfareLevelUp` and remembers who grew (`LeveledUp`); `BagScreen` levels with a Rare Candy through `GainExp`. Nothing shows the six stats and their gains. Plan 06 · R10 owes the move-learning prompt that follows a level.
- **Trainers**: `tools/MapImporter/WorldWriter.cs` writes an object's trainer type, sight and script name (`AreaObject.Trainer`, `Sight`, `Script`: `TRAINER_YOUNGSTER_TRISTAN`), and every team is typed by hand into the overlay as `MapFile.TrainerRecord` (`Party` of `PartyMember`: species, level, `Moves`; `PrizeMoney` worked out by hand from the class's rate; `SightRange`; `DoubleBattle`), which `WorldMapBuilder.PlaceEvents` (`Data/World.cs`) turns into a `Trainer`. A `Trainer` has no items and no AI flags; a trainer's Pokémon has no held item, ability, gender or IVs of the original's. `tools/DataImporter`'s `Sources.DecompFolders` fetches `/res/pokemon/`, `/res/moves/`, `/res/items/data/` and `/generated/`, nothing of `/res/trainers/`. Plan 06 · R9 wants Platinum's AI flags per trainer and names no source.
- **Furniture**: `TileBehavior` has `Television`, `TownMap`, `Bookshelf` (three sizes) and `TrashCan`, and the hand-made rooms' TVs are props (`PropType.Television`; `"type": "Television"` in `Data/maps/PlayerHouse.json`). `GameEngine.TryInteract` reaches a person, a counter (`Map.IsCounter`), a signboard, a hidden item, then water (`TryStartSurf`); nothing answers a TV, a shelf or a bin. Jubilife TV stands without a room (plan 01 · M4; its room is M11); plan 02 · S4 has the Twinleaf opening's TV scene only. `UI/Kit/NameEntry.cs` is the one GPU-free picker, used for the player's name alone.
- **Vs. Recorder**: `items.json` has it (id 465, `KeyItems`, `fieldUse` `VsRecorder`, `canBeRegistered`); `BattleCore.Record` and `Replay` work and `BattleCoreTests.ARecordedBattleReplaysTheSame` holds them; nothing writes a record to disk or shows one, and `GameEngine.EndBattle` only trims models and fades. Plan 07 keeps replays on its server for bug reports and leaves the wire format (events name Pokémon by reference) to O3. Looker stands in Jubilife with stand-in lines (`overlays/jubilife_city.json`); plan 02 · S5 writes his scene.
- **Spiritomb**: `Data/WorldModels.cs` knows `r209s02` as `PropType.Cairn`, "the Hallowed Tower"; Route 209 opens with plan 01 · M6; `habitats.json` lists no Spiritomb; the Odd Keystone is in `items.json`. Plan 06 · R16 leaves the Underground's multiplayer out, and plan 03 · D12's completeness test isn't written yet.
- **Entries**: `PokemonSpecies.DexEntry` is one sentence `Importer.DexEntry` composes from the data (name, category, types, region, height, weight, the line's evolutions); the Pokédex's INFO page (`UI/ModernUi.Pokedex.cs`) shows it. Plan 03 · decision 1 generated entries so that no game text is used; there is no habitat word.

## Design

**Platinum's defaults don't move.** Battle Scene on and Battle Style Shift, as the original ships; the speed-up, auto-advance, text frames and autorun are our own rows beside them, off unless chosen. Everything here keeps two harness runs drawing the same pictures: nothing new rolls outside `Dice` or the battle's generator, and nothing reads a clock but `FrameClock` and the game's own.

### Options and pace

- **Rows** (`OptionRow`, `GameSettings`): `BattleScene` (on/off), `BattleStyle` (`BattleStyle.Shift`/`Set`, a new enum), `Speakers` gains `SpeakerMode.Mono` (the mixer folds its two channels in `AudioMixer.Render`), `Frame` (the text box's frame, our own six), `SpeedUp` (off, ×2, ×3, used while Run is held), `AutoAdvance` (off, slow, fast), `Autorun` (on/off). All in `settings.json`; `OptionsScreen.Describe` gets a line for each and `GameEngine.ApplySettings` pushes them. Button Mode goes with key rebinding, which no plan owns; the Rumble Pak has no place here.
- **Battle Scene off** is a mode of the face, never of the rules: `BattleEngine.Show` skips `Anim.Attack` on `Lunged` and makes no `EffectCue` on `MoveShown` (the move's sound still plays), and what a line carries in `OnImpact` lands at once instead of `HitDelay` later, through the same `pendingEffects` so damage still lands in the log's order. The ball's throw and the send-outs stay, as in Platinum. The core and the log don't change, so a record replays the same with the scene on or off.
- **Battle Style Shift** is a question the rules ask: a new `SwitchOfferRequest(Place, Pokemon upcoming)` beside `ActionRequest` and `ReplacementRequest`, yielded by `ReplaceFainted` after the line "<trainer> is about to send in <X>. Will you switch?" (a `Said`, so the harness can show it) and before the foe's `Entered`, only in a single trainer battle, only when the player's Pokémon is standing and `CanSendIn` finds anyone, and only when `CoreSetup.OffersSwitch` is set. It is answered from outside with `BattleChoice.GoOn` or `BattleChoice.Switch(index)`, checked by `WhyNot`, and so lands in `BattleRecord.Answers`: a replay holds. `BattleEngine` sets `OffersSwitch` from the settings (Set never asks), opens the switch panel on the request (`BattleEngine.Menus.cs`), and a cancel answers GoOn. The core's default is not to ask, so `CoreScenario` and every test keep the turns they have.
- **The speed-up** multiplies the `dt` the engine hands to the Battle, Dialogue, Evolution and Transition states while Run is held (`GameEngine.SpeedUp`, a property the key sets and the harness sets directly). Presentation is a function of time (the harness's `Skip` proves it) and cue seeds come from the animator, so no roll changes. Dialogue runs at `CharactersPerSecond` times the same factor.
- **Auto-advance** gives `waitingForMessageConfirm` a timer once the line is whole (`BattleEngine.AutoAdvanceAfter`, null when off), and `UpdateDialogue` the same for plain lines; a question (`DialogueManager.IsQuestion`) always waits.
- **Autorun**: `Player.Update` reads Run as "walk" when `Player.Autorun` is set; `Advance` is unchanged, so tests and the harness walk as before. Once plan 02 · S4 gates running on the Running Shoes, autorun obeys the same gate.
- **Frames**: `ModernUi.DrawDialogue` takes the frame's number; each frame is drawn with `UiShapes` (a double rule, a wooden edge, a stone edge, a dark glass, a ribbon), never pixel art from the games. The style guide's "Field menus and notices" gets a text-box paragraph with the six before the code.

### The summary

- **Pages** (`SummaryPage`: `Info`, `Memo`, `Skills`, `BattleMoves`, `ContestMoves`, `Ribbons`) live in a GPU-free `SummaryView` (`UI/SummaryView.cs`: `Open(party, index)`, `Move`, `Sideways`, `Confirm`, `Cancel`, a cursor over the moves on the move pages) painted by `ModernUi.Party` painters, one per page. `PartyScreen` keeps `ShowSummary` and owns a view; `PCScreen` and the battle's switch panel (`BattleMenuState.SwitchPokemon`) open the same view on the Pokémon under the cursor. Left and right turn pages, as the Pokédex's do (there are no shoulder buttons). The style guide's "Summary" is rewritten for six pages first.
- **The memo's fields** go on `Pokemon` and `SavedPokemonData`: `OriginalTrainer`, `OriginalTrainerId`, `MetLocation` (the area's `DisplayNameAt`), `MetLevel`, `MetDate` (the system's day, like `SaveData.Started`), `Markings` (Platinum's six, a bit each). They are set where a Pokémon becomes the player's (`BattleEngine.Show`'s `Caught`, `GivePokemon`, the starter) and copied by `Pokemon.CopyStateFrom`. A Pokémon from an older save is taken to be the player's own, met at an unknown place at its current level: that is the one migration, in `SavedPokemonData.ToPokemon`. If plan 07 · O2 hasn't run, P3 takes these fields over from it; O2 then adds `Id`, EVs, `SecretId` and `IsTraded` on top, and `ReceiveTradedPokemon` keeps the giver's memo.
- **The characteristic** is `Characteristics.Of(pokemon)` (`Models/Characteristics.cs`): the stat of the highest IV, ties broken from the personality value, the line by that IV's remainder by five, as the original decides; the thirty lines are ours. The nature's line and the ability's text come from the data.
- **Contest Moves and Ribbons** are frames with "nothing yet" until plan 06 · R17 fills them.

### The level-up panel

`LevelRose` gains `Before` and `After` (`StatSpread`s the core fills where it emits the event). `BattleEngine.Show` turns it into a message step with a panel (`BattleHUD.LevelUp`, drawn by `ModernUi.Battle`): the six stats with the +n column first, a press, the new values, a press, then the fanfare's line as today; it comes before any move the level teaches, which is R10's prompt. `BagScreen` sets the same `LevelUpPanel` for a Rare Candy and `ModernUi.Items` draws it over the party cards.

### The trainer table

- **The reader** is `tools/DataImporter`'s: `Sources.DecompFolders` gains `/res/trainers/`, and a `Trainers` step reads each trainer's file of the pinned commit (its class, name, AI flags, bag items, whether it battles two at a time, and for each Pokémon the species, form, level, held item, IV score, ability slot, gender and chosen moves), keeping the original's names for the flags and classes. It writes `Data/trainers.json` (`TrainerFile`, `TrainerEntry`, `TrainerPokemonEntry`; `docs/data-files.md` gets a section), keyed by the original's constant without its prefix (`youngster_tristan`), and never a line of what a trainer says. The class table (the prize rate, the gendered pairs) goes with it. The importer's README says what it reads.
- **The game reads it** through `TrainerDatabase` (static constructor like `ItemDatabase`): `WorldMapBuilder.PlaceEvents` takes a trainer's team from the table by the object's `Script`, and the overlay's `trainer` block shrinks to our lines (`dialogueBefore`, `dialogueAfter`); a block that still carries a `party` wins, for Kanto's hand-made maps and tests. `Trainer` gains `Items`, `AiFlags` and `ClassId`; a trainer's Pokémon takes its held item, its IVs from the score (the original's score × 31 / 255, rounded down), its ability by slot and its gender as the file says, and `PrizeMoney` is the class rate times the last Pokémon's level times four, as the tests already expect. `TrainerAi` reads `AiFlags` in R9; until then they are carried, not used.

### Furniture, broadcasts and phrases

- **Facing a thing**: `TryInteract` gains a step between the signboard and the hidden item: the prop under the tile ahead (`Map.Props`, `Prop.Covers`) or the tile's behaviour names a common script (`FieldScripts.ForFurniture`: `common.Television`, `common.Bookshelf`, `common.TrashCan`, `common.TownMap`), unless the map's `SignScripts` or the overlay gives that tile a script of its own (the Twinleaf opening's TV, plan 02 · S4). The rooms M11 imports carry the behaviours already.
- **Broadcasts** (`Story/Broadcasts.cs`, GPU-free): a programme is lines composed from the save and the story (the Pokédex's counts, the badges, the time played, the clock's hour, the last interview's phrases, R14's lottery once it exists, "the draw is another day" until then), chosen by `Dice`. A new op `broadcast` asks the host (`IScriptHost.Broadcast()`, `HeadlessScriptHost` writes what it would have said), with its row in `docs/scripts.md`, a case in both hosts and a line in `ScriptTests.EveryCommand`.
- **Phrases** (`UI/Kit/PhraseEntry.cs`, GPU-free like `NameEntry`; `ModernUi.DrawPhrases`): two words from groups of our own (`Data/PhraseGroups.cs`: Pokémon, moves, types, feelings, people, places, time, small words), never the original's lists. A new op `phrase` asks for one into `StoryState.Phrases` (saved as `SaveData.StoryPhrases`, empty by default, no migration), and a reporter's `interview` script is an ordinary script that asks and then sets the flag the TV reads. Mail (R11) and the Trainer Card's self-introduction (R12) reuse the picker.

### Recorded battles

- **The file** (`Battle/Sim/BattleRecordFile.cs`, GPU-free): the `BattleRecord` with everything the core needs to replay it, as plain data: both parties as `SavedPokemonData`, the trainers as `TrainerRecord`, the format, the rules preset, the `BattleConditions`, the player's name, the day, and a data version (a hash of the four data files, `GameDataFiles.Version`, which plan 07's rooms will use too). A replay rebuilds its parties from the file, so events name the replay's own objects: this is plan 07 · O3's wire format, once for both.
- **Writing** happens in `GameEngine.EndBattle`: a trainer battle, the Vs. Recorder in the bag, and a yes to "Record this battle?"; the Hall of Fame's battles always. Files go to `replays/` beside `SaveManager.SaveFilePath`, named by the day and the foe; the harness's working directory keeps them out of a real save's folder.
- **Watching** is `BattleEngine` in a watch mode (`BattleEngine(BattleSetup, BattleRecord)`): the core is given `Replay`, the answers come from the record instead of the menus, the menus never open, input does nothing but Cancel (leave) and Run (the speed-up), the HUD, animator and sounds run as for any battle, and nothing touches the party or the bag. A list screen (`ReplayListScreen`, `ModernUi.Records`: `ListRow`s of foe, day and result) opens from USE on the Vs. Recorder in the bag, the first key item with a use path (`FieldItems.UseKeyItem`; R11 generalises it). A file of another data version is refused with a notice.

### Spiritomb

The tower is a prop with a script of its own in the overlay (`OverlayProp.Script`, new): without the keystone a line; with it, "Set the Odd Keystone in the tower?" takes the item and sets a flag; once the greetings are enough, `wildbattle Spiritomb 25` once. The count is `StoryState.Greeted` (the keys of people spoken to, saved as `SaveData.GreetedPeople`, empty by default), filled by `TryInteract`, read by scripts as the built-in variable `GREETINGS` beside `PARTY_COUNT`. The stand-in goes in `docs/mechanics/rulings.md` with the table's three columns. Where a Pokémon no table lists is met goes in a hand-written `Data/world/sinnoh/special.json` that `Habitats` reads beside `habitats.json` (the importer's `--data` never touches it), so the AREA page and D12's test count it.

### Written entries

- **The file** is `Data/dex-entries.json`, read by `DexEntries` (`Data/DexEntries.cs`): for each species and form name, two entries and a habitat word from Platinum's nine (grassland, forest, water's edge, sea, cave, mountain, rough terrain, urban, rare). The importer never writes it; `PokemonSpecies.DexEntry` stays as the fallback for a name the file lacks. The INFO page shows a written entry, the two in turn on each opening, and the habitat word beside the height and weight (the style guide's "Pokédex · INFO" first).
- **Our own words.** Plan 03 · decision 1 keeps its purpose (no game text) and changes its means: entries written by us in the manner of a Pokédex, never a close paraphrase of Platinum's or PokeAPI's flavour text. `DataFileTests` holds the shape: two entries for every species and form, 15 to 45 words each, none alike, none the generated sentence, every habitat word from the list. The words themselves are checked by the importer, which has PokeAPI's flavour texts in its git-ignored cache: `--check-entries` reports any run of six words shared with any of them, and the session rewrites those. It can't be a unit test, because the original's text must never enter the repository.
- **Batches**, in National order like plan 03's models: the Sinnoh Pokédex's 210 first (they are met first), then the rest by region, then the 541 forms.

## Sessions

### P1 · Platinum's battle options
- `OptionRow.BattleScene`, `BattleStyle` and `SpeakerMode.Mono`; `SwitchOfferRequest` and `CoreSetup.OffersSwitch` in the core, the offer's panel in `BattleEngine.Menus.cs`, the scene-off path in `BattleEngine.Show`; the mixer's fold.
- Tests: a `CoreScenario` where Shift offers and Set doesn't, the offer's answer in the record and its replay, a switch made at the offer entering before the foe's; the log's events land in the same order with the scene off; `OptionsCycleThroughTheirValues` with the new rows.
- Shots: `demo` and `conditions` run with the scene off (`d*_scene_off`), a `battle` shot of the offer (`23b_battle_offer`), the options' new rows in `look_8_options`.
- **Done when** a trainer battle under Shift asks before the foe's next Pokémon, Set and the scene-off change no outcome, and every existing shot taken with the defaults is unchanged by `diff`.

### P2 · A quicker battle, of our own
- `SpeedUp`, `AutoAdvance`, `Autorun` and `Frame` rows; `GameEngine.SpeedUp`; `BattleEngine.AutoAdvanceAfter` and the field's; `Player.Autorun`; six frames in `ModernUi.DrawDialogue` after the style guide's paragraph.
- Tests: a sped battle reaches the same log as a slow one; auto-advance never answers a question; autorun walks at the running pace with Run up; the frame number round-trips through `settings.json`.
- Shots: the six frames over one line (`look_9_frames_*`), a battle at ×3 against the same at ×1 (`d*_fast`).
- **Done when** holding Run plays a battle at three times the pace with the same result, and the defaults draw what they drew.

### P3 · The memo's fields and the first three pages
- `OriginalTrainer`, `OriginalTrainerId`, `MetLocation`, `MetLevel`, `MetDate`, `Markings` on `Pokemon` and `SavedPokemonData`, set at the catch, the gift and the starter; the migration in `ToPokemon`; `Characteristics`; `SummaryView` with `Info`, `Memo` and `Skills`, painted after the style guide's "Summary" is rewritten.
- Tests: an older save's Pokémon reads as the player's own; a catch records the area's name and the level; `ACopyTakesOnEverythingThatCanChange` passes with the new fields; the characteristic for hand-worked IVs.
- Shots: `20b_summary` (INFO, kept for the compare board), `20d_summary_memo`, `20e_summary_skills`.
- **Done when** a Pokémon caught on Route 202 says so on its memo, with the day and the level, after a save and a load.

### P4 · The move pages and the other doors
- `BattleMoves` with a cursor and the chosen move's description, `ContestMoves` and `Ribbons` as empty frames; the view opened from `PCScreen` and from the battle's switch panel.
- Tests: the cursor over four, three and one move; the PC's and the switch panel's way in and back.
- Shots: `20f_summary_moves`, `20g_summary_contest`, `30b_pc_summary`, `23c_battle_switch_summary`.
- **Done when** every page is reached from the party, the PC and a battle, and the harness shows all six.

### P5 · The level-up panel
- `LevelRose.Before`/`After` from the core; `BattleHUD.LevelUp` and its painter; the bag's panel for a Rare Candy.
- Tests: the gains add up to the new stats; the panel comes before the level's move (R10's prompt, when it exists); a Rare Candy from the bag shows the same numbers.
- Shots: a level-up added to the `demo` battle (`d2x_level_gains`, `d2x_level_stats`; later demo shots move, which `diff` will list), `22c_bag_rare_candy_panel`.
- **Done when** a level in battle and from the bag shows two pages of numbers that match the Pokémon.

### P6 · The trainer table imported
- The `/res/trainers/` folder in `Sources.DecompFolders`, the `Trainers` reader, `Data/trainers.json`, the class table, `docs/data-files.md` and the importer's README.
- Tests: the reader on a sample file in the test project (as `MapImportTests` does for maps, so the decompilation isn't needed to test it); every trainer the open overlays name is in the table with the same species and levels.
- **Done when** the importer writes the whole table and `SouthWestTests`' teams agree with it.

### P7 · Trainers read from the table
- `TrainerDatabase`; `PlaceEvents` by the object's `Script`; `Trainer.Items`, `AiFlags`, `ClassId`; held items, IVs, abilities and genders on trainers' Pokémon; the overlays' `trainer` blocks slimmed to lines; README's "Opening an area" line.
- Tests: every placed trainer of every open area matches the table (species, levels, items, moves); a hand-typed party still wins; a Youngster's prize money is what it was.
- Shots: `versus` against a leader's Pokémon holding its item (`95_versus_*`), unchanged `battle` shots otherwise.
- **Done when** no overlay carries a team, and plan 02's next chapter adds a route by writing its lines alone.

### P8 · Furniture and the programmes
- `FieldScripts.ForFurniture`, the prop step in `TryInteract`, the four common scripts; `Broadcasts`; the `broadcast` op with its row, cases and `EveryCommand` line.
- Tests: facing a TV runs `common.Television` and a shelf `common.Bookshelf`; a tile with its own script wins; a programme on a known save gives known lines under a seeded `Dice`.
- Shots: `story` mode faces the TV, a shelf and a bin in the player's house (`st*_television`, `st*_shelf`).
- **Done when** every TV in the game says something true about this game, and the opening's TV still plays its scene.

### P9 · Phrases, interviews and reporters
- `PhraseEntry` and its groups, `ModernUi.DrawPhrases`, the `phrase` op, `StoryState.Phrases` and `SaveData.StoryPhrases`; Jubilife's reporters as overlay people with an interview script; the programme that reads the answers back.
- Tests: the picker's cursor and groups; a phrase saved and loaded; the interview played headlessly to its end on every way through.
- Shots: the picker open (`st*_phrases`), the interview's question.
- **Done when** an interview given on Route 202 is heard on the TV at home.

### P10 · The Vs. Recorder's file
- `BattleRecordFile`, `GameDataFiles.Version`, the question at `EndBattle`, the `replays/` folder.
- Tests: a file round trip; a battle replayed from its file ends in the same `BattleResult` with the same lines; a file of another version is refused.
- **Done when** every trainer battle fought with the recorder in the bag can be written out and replayed headlessly.

### P11 · The replay viewer
- The watch mode of `BattleEngine`, `ReplayListScreen` and its painter, USE on the Vs. Recorder through `FieldItems.UseKeyItem`; Looker's gift waits for plan 02 · S5 (the harness puts the item in the bag).
- Tests: watching changes nothing in the party, the bag or the money; Cancel leaves at any line.
- Shots: the list (`31_replays`), a replay at its third turn (`31b_replay_watching`).
- **Done when** a recorded battle is watched from the bag at ×3 and the save afterwards equals the save before.

### P12 · Spiritomb's tower
- `OverlayProp.Script`, `route_209.txt`'s scripts, `StoryState.Greeted` and `GREETINGS`, `special.json` and its reading in `Habitats`, the rulings' row. Needs Route 209 open (plan 01 · M6); until then the scripts are tested on a map of their own.
- Tests: the tower's every way through (no keystone, the keystone set, too few greetings, the battle); thirty-two people counted once each; Spiritomb's AREA page names the tower.
- Shots: `story` mode at the tower (`st*_hallowed_tower`) once M6 is in.
- **Done when** a game that has spoken to thirty-two people and set the keystone meets Spiritomb, and the species has a place in the Pokédex.

### P13 · The entries file and the Sinnoh Pokédex
- `Data/dex-entries.json`, `DexEntries`, the INFO page's written entry and habitat word (the style guide first), `--check-entries` in the importer, the `DataFileTests` shape (which until P17 holds only the names the file has), and the 210 species of the Sinnoh Pokédex: 420 entries.
- Shots: `26_pokedex_turtwig` (kept), `26m_pokedex_second_entry`.
- **Done when** every species of the Sinnoh Pokédex has two entries and a habitat word, the check reports nothing shared, and the page shows them.

### P14 · Entries: the rest of Kanto and Johto
### P15 · Entries: the rest of Hoenn and Unova
### P16 · Entries: the rest of Kalos, Alola, Galar and Paldea
### P17 · Entries: the 541 forms
Each batch writes its entries in National order, runs the check, and raises the test's floor to its last name; P17 makes the test hold every species and form. **Done when** `DataFileTests` holds the whole file.

## Risks

- **The Shift prompt changes the shape of every trainer battle**: tests that drive `BattleEngine` and the harness's trainer-battle shots gain a question. P1 keeps the core's default silent and turns the offer on from the settings alone, so only the game and the harness see it, and runs `all` before and after.
- **Two owners of one save field**: the memo's fields are O2's as well. Whichever session runs first adds them; the other reads them. The migration is written once.
- **AI flags carried unused** until R9 reads them; the table is still worth having first, because every chapter of plan 02 places trainers.
- **Broadcasts without a calendar**: the lottery and the weekday programmes wait for R14's clock; P8 writes the programmes that need none.
- **A replay goes stale** with every importer run that changes a move or a species: the data version refuses it with a notice rather than playing it wrong.
- **Three thousand entries are slow to write**, and the overlap check needs the importer's cache: each batch is one session, and the check is run before the batch is called done.
- **The decompilation's trainer files** haven't been opened from this plan; P6 reads them as they are and the plan's field list is corrected if they differ.

## Needs and gives

- **Needs** plan 01 · M6 (Route 209, for P12) and M11 (the rooms whose furniture P8 answers); plan 02 · S5 (Looker's gift, for P11) and its chapters (the trainers P7 places); plan 06 · R9 (which takes P7's AI flags), R10 (the move prompt that follows P5's panel), R11 (which generalises P11's key-item use), R14 (the lottery programme), R17 (the two empty pages).
- **Gives** plan 02 every route's trainers from the table (P7) and the TV scene's tile (P8); plan 06 · R9 its AI flags and bag items per trainer, R11 a phrase picker for mail, R12 one for the Trainer Card; plan 07 · O2 the memo's fields, O3 a replay file that is the wire format, and the rooms a data version (P10); plan 03 · D12 a place for the Pokémon no table lists (P12).

## Decisions for the user

1. **Where recorded battles are opened from.** *Recommended:* USE on the Vs. Recorder in the bag, as in the original. The alternative is a REPLAYS entry on the title screen as well, which costs a `TitleChoice` and its tests.
2. **Spiritomb's thirty-two greetings.** *Recommended:* thirty-two different people spoken to anywhere in Sinnoh, counted once each. The alternative is plan 06 · R16's solo Underground diggers, which makes P12 wait for R16.
3. **Which forms get entries.** *Recommended:* all 541, two each, so the forms page plan 03 · D10 left optional can show them. The alternative is only the forms that are a Pokédex entry of their own (the regional forms, Rotom's appliances, the cloaks, the seas), about 60, with Megas and Gigantamax showing their species' entries.
4. **The speed-up's key.** *Recommended:* held Run, which does nothing in a battle today, with the factor an options row. The alternative is a toggle row that keeps it on, which also speeds the field's text.
5. **Jubilife TV's programmes.** *Recommended:* the five P8 names (the Pokédex, the trainer survey, the clock, the interviews, the lottery's stand-in). The alternative is the interviews alone.

## Status

- [ ] P1 Platinum's battle options
- [ ] P2 A quicker battle, of our own
- [ ] P3 The memo's fields and the first three pages
- [ ] P4 The move pages and the other doors
- [ ] P5 The level-up panel
- [ ] P6 The trainer table imported
- [ ] P7 Trainers read from the table
- [ ] P8 Furniture and the programmes
- [ ] P9 Phrases, interviews and reporters
- [ ] P10 The Vs. Recorder's file
- [ ] P11 The replay viewer
- [ ] P12 Spiritomb's tower
- [ ] P13 The entries file and the Sinnoh Pokédex
- [ ] P14 Entries: the rest of Kanto and Johto
- [ ] P15 Entries: the rest of Hoenn and Unova
- [ ] P16 Entries: the rest of Kalos, Alola, Galar and Paldea
- [ ] P17 Entries: the 541 forms
