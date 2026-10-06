# Plan 13 · Ways to play: difficulty, challenge runs, a randomiser, medals, timers and local versus

Written 2026-10-06, before any session.

**Goal**: choices made once, at NEW GAME, that change how a run is played and never move afterwards: a difficulty (Challenge and Easy, a level cap by badge, trainers scaled to the team), the rules of a challenge run (a Nuzlocke preset, monotype, a party of one, no items in battle, no Pokémon Center, no catching, no running, Set forced, the shiny odds) and a seeded randomiser; and around them what a run leaves behind (counters, a statistics page, medals, a run timer with splits, a New Game+), a seeded daily run, and two people battling at one screen. Normal with nothing on is today's game byte for byte: Platinum's defaults never move, and every choice here is the player's at the start of their own game, as plan 06 · R1's choice of rules is.

## Where we are

- **The pattern to follow** (plan 06 · R1): whose rules a game is played by is asked on the title screen after NEW GAME (`TitlePhase.ChooseRules` in `UI/TitleScreen.cs`, `TitleScreen.Rules`), handed through `GameEngine.StartNewGame(name, look, rules)` (the engine keeps it as `newGameRules` between the title and the introduction), kept in `SaveData.Rules` and never changed. `Ruleset.Use` is called by the engine alone and tests hand rules to what they drive. `TitleScreenTests.TheRulesAreChosenBeforeANewGameAndPlatinumsAreOffered` walks it.
- **The save** (`Core/SaveManager.cs`): `SaveData` has `PlayTimeSeconds`, `Money`, `Badges`, `SeenSpecies`, `CaughtSpecies`, `Diplomas`, `BoxStorage` and the story's fields; no counters, no run state, no graveyard, no splits. `SavedPokemonData` keeps a Pokémon whole. The title's CONTINUE card and the save screen draw `ModernUi.SaveSummary`; the Trainer Card draws a `TrainerCardInfo` (name, id, money, Pokédex, time, badges, the day) on one page, and plan 06 · R12 owes its back, the stars and the signature.
- **Trainers** are made as maps load: `MapFile.BuildNpc` turns each `MapFile.TrainerRecord`'s `PartyMember`s into `new Pokemon(species, level)` with the chosen moves, drawing from the seeded `Dice`, which is why the harness's battles repeat. A trainer of the imported world whose overlay names the same team as plan 06 · R9's `Data/trainers.json`, or none, is then filled from the table (`TrainerDatabase.Fill`: `Trainer.Ai`, `Trainer.Items`, the team built as the original builds it from each Pokémon's personality, and the prize money of its class's formula); the table also holds Platinum's rematch teams (`leader_roark_rematch`). `GameEngine.StartTrainerBattle` hands the NPC's `TrainerData` to the `BattleSetup` as it is, with `Trainer.PrizeMoney`, `TrainerClass` (a string; "Leader" is already matched by name for the transition) and `DoubleBattle`, and a second trainer or a partner when a script names them.
- **What may be chosen in a battle** is one place, `BattleCore.WhyNot` (`Battle/Sim/BattleCore.Choices.cs`): running is refused by a trap and by `CoreSetup.CannotFlee` (plan 06 · R9), an item by `WhyNotItem`, and the menus ask it first. `CoreSetup` carries `Rules`, `Conditions`, `Kind`, `CannotFlee`, `FirstBattle`, `Partner`, `PlayerController`, `EnemyController` and `PartnerController` (`TrainerAi.Instance`, Platinum's AI since R9, which thinks for a place with a `Trainer` behind it and draws a move at random for a wild Pokémon or the player's own; null makes the core ask both sides, which plan 07 · O6 uses for a link battle). `BattleEngine.Menus.cs`'s `BeginChoosing` keeps only the places whose `Side == BattleSide.Player`, and `InputManager` reads gamepad 0 alone. The harness's `versus` mode already shoots a battle between two named species.
- **EXP** is given in `BattleCore.AwardExp` (`Formulas.ExpShares`, `ExpFor`, then `Pokemon.GainExp`) and by a Rare Candy in `BagScreen` (`ExpForNextLevel`, `Pokemon.GetExpForLevel`); nothing caps a level.
- **Shininess**: `Pokemon`'s constructor rolls `rng.Next(8192) == 0` and nothing reads the result (plan 09 · L1 draws it). The Shiny Charm is in `items.json` among the later games' items (plan 06 · R28) and does nothing.
- **Wild Pokémon** come from `Map.RollWildEncounter` through `WildEncounterRules.Meet` (and since plan 02 · S2 from a rod's bite and Sweet Scent), whose `WildEncounterEntry.SpeciesName` `GameEngine.StartWildBattle` turns into a Pokémon; the starters are `StarterSelectScreen.Starters`; an item ball's item is `NPC.Item`, a hidden one `HiddenItem.Item`; the Pokédex's area page reads `Data/Habitats.cs`. Nothing remaps any of them.
- **Counting**: `GameEngine.OnStep` counts steps for friendship, `Evolution.CountStep` and the Pokétch's pedometer (`Poketch.Step`, plan 02 · S2: saved, and reset by the player); `BattleEngine.Show` sees every `Caught`, `ExpGained`, `LevelRose`, `Won` and `Ended`; `StoryState.GiveBadge` gives a badge; `GameEngine.PlayEvolutions` plays what `Evolution.Evolve` does. Beyond the pedometer, none writes a number anyone reads later. `Toast` (`UI/FieldNotices.cs`) shows one line; `AudioManager.PlayFanfare(MusicRole)` plays a fanfare by role; `OptionRow` has twelve rows.
- **Scripts** ask twenty-one questions (`Query` in `Story/Script.cs`: `flag`, `badge`, `item`, `knows`, `money`...), none about how the game is being played; the nurse heals on a yes (`common.Nurse`). `IScriptHost.GivePokemon` is how a script gives one.
- **Chance and time** for tools: `Core/Dice.cs` (`Seed`, `New`, `Shared`) and `Graphics/FrameClock.cs` (`Fixed`, `Now`). `GenomeRandom` (SplitMix32 in `Graphics/PokemonGenome.cs`) is the game's one generator that gives the same numbers on every machine; it is internal to `Graphics`.

## Design

**One record, chosen once.** `Models/RunRules.cs` (GPU-free): a `RunRules` record with `Difficulty` (a new enum `Difficulty`: `Easy`, `Normal`, `Challenge`), `LevelCap`, `ScaleTrainers`, `Nuzlocke` and its clauses (`DupesClause`, `ShinyClause`, `GiftsCount`), `Monotype` (a `PokemonType?`), `PartyLimit` (6; 1 for a solo run), `NoBattleItems`, `NoCenter`, `NoCatching`, `NoRunning`, `SetStyle`, `ShinyDenominator` (8192) and `Random` (a `RandomizerOptions` with the seed and what it touches; null for none). `RunRules.Normal` is `new RunRules()`, and `IsNormal` says nothing is on. The presets (`RunRules.NuzlockePreset`, `Hardcore`) are records with some fields set; the title shows them on the first row and the rows under it follow the preset. It is kept whole in `SaveData.Run` (null in older saves, read as Normal), shown on the CONTINUE card, the save screen's summary and a page of the Trainer Card, and never changed. It is passed wherever it is read: `BattleSetup.Run` to `CoreSetup.Run`, `IScriptHost.Rules` to the runner, `Party.Limit` on the party. One rule has no caller to hand it to, the shiny roll in `Pokemon`'s constructor: `RunRules.Current` is static for it alone, set by the engine (`StartNewGame`, `ApplySaveData`) as `Ruleset.Use` is, and tests never set it.

**What a run remembers** is `RunState` (`Models/RunState.cs`, GPU-free), saved as `SaveData.RunState`: the areas where a wild Pokémon has been met (by `MapArea.Key` on a streamed map, a hand-made map's name otherwise), the graveyard (each fallen Pokémon as `SavedPokemonData` with where and when), whether and why the run ended, the clears (New Game+), the timer and its splits. `SaveData.Counters` (`Dictionary<string, long>`) is everything counted, named in `Models/Statistics.cs`.

**Where each rule is enforced**, once each: a battle's in `BattleCore.WhyNot` and `WhyNotItem` from `CoreSetup.Run` (running, items, balls, revives); the party's size in `Party.Limit` (a property, 6 by default, read where the constant is today, so a catch under a solo run goes to the box through the core's own `Caught.ToBox`); a trainer's strength in `StartTrainerBattle`, on a copy; EXP's ceiling in `AwardExp` and the Rare Candy; the field's in the scripts (`Query.Rule`) and the screens (the PC's withdraw, a gift). `RunRulesTests.EveryRuleHasItsPlace` drives each rule through the bare core (`CoreScenario`) or `HeadlessScriptHost` and holds that Normal refuses nothing.

**Trainers are raised on a copy.** The map's `TrainerData` stays as the import, `TrainerDatabase.Fill` and the seeded `Dice` made it, so the harness's shots and every test that loads a map are untouched. `DifficultyRules.Prepare(trainer, rules, leadLevel)` (`Models/DifficultyRules.cs`, GPU-free) returns the `Trainer` the `BattleSetup` gets: under Normal the same object; otherwise a new one. Challenge raises levels by a table by class (our own numbers: leaders, the Elite Four and the Champion most, the rival next, everyone else a little), gives leaders and the League one more Pokémon from the species the original's rematch team gives them (the rematch teams in plan 06 · R9's `Data/trainers.json`, `leader_roark_rematch` and the rest), gives the important trainers held items (plan 06 · R8's effects run already), sets every trainer routine of R9's `AiFlags` on `Trainer.Ai` (not the roamer's, the Great Marsh's or the lesson's), and pays prize money ×1.5. Easy lowers levels by the same table, gives EXP ×1.5 in `AwardExp` and takes a quarter off `ShopScreen`'s prices. The copy's Pokémon are built at their new level as `TrainerDatabase.Build` builds the table's (plan 06 · R9: personality, IVs, nature, ability, item and the chosen moves kept, nothing left to chance, as the original builds its rematch teams); a team an overlay types by hand is made `new Pokemon` at its level with the chosen moves kept, drawn from `Dice.New()`. `ScaleTrainers` re-levels the copy's party round the lead's level keeping its spread, at any difficulty.

**The level cap** is the next leader's highest level from plan 02's pacing targets (14, 22, 26, 32, 37, 41, 44, 50, then 62 for the League), by `StoryState.BadgeCount`, lifted by the region's `StoryCompleteFlag`. It is Sinnoh's; a region without a table is uncapped until its own plan writes one. `AwardExp` clamps the amount at `Pokemon.GetExpForLevel(cap)` and says once a battle that the Pokémon can't grow further yet; the Rare Candy refuses the same way.

**The randomiser never edits the data.** `Models/Randomizer.cs` (GPU-free) is built from the seed with `GenomeRandom`, moved to `Core/SplitMix.cs` and made public so one seed gives one mapping on every machine (never a string's `GetHashCode`), and asked at the point of use: a species to one of similar base-stat total (within a tenth, widening until one is found) and the same stage of its line, legendaries among legendaries, a line mapped from its first stage so it stays coherent unless "random evolutions" is on. It is asked for a wild Pokémon in `StartWildBattle`, a trainer's team in the copy `Prepare` makes, the starters in `StarterSelectScreen`, a gift in `IScriptHost.GivePokemon`, an item in `find` and the hidden items, and in reverse by `Habitats` for the area page. What belongs to a species (abilities, learnset, behind its own toggle the types) is swapped in memory at the start the way `Ruleset.Use` swaps the modern values (`Randomizer.Use`) and put back on a new game or a load. The harness and `Dice.Seed` are untouched: the randomiser has a seed of its own, typed on the title's keyboard or rolled once from `Dice.Shared`.

**Counters and what stands on them.** Every count is one line, `Statistics.Count(key, n)` on the engine's counters, from `OnStep` (steps by `Player.Mode`), `EndBattle` (battles by kind and `BattleResult`, whiteouts), `BattleEngine.Show` (catches by ball, `ExpGained`, `LevelRose`, the player's `Fainted`), `GiveBadge`, `PlayEvolutions`, money in and out, items found, `EnterArea` (time by area) and `StartWildBattle` (encounters by species). Medals (`Models/Medals.cs`) are a catalogue of predicates over the counters and the save, with our own names, texts and signs (nothing of the games' Medal Rally but the idea), checked when a counter moves; one earned is kept with its day in `SaveData.Medals`, told by the `Toast` with its sign and a fanfare of our own (`MusicRole.Medal`). The statistics and the medals are pages of the Trainer Card (`CardPage`: Front, Run, Statistics, Medals; plan 06 · R12's back takes its place after Front when it comes), flipped with left and right and drawn in `ModernUi.Records.cs` with `ListRow` and `Tabs`.

**The timer** (`Models/RunTimer.cs`, GPU-free): real time (the wall clock, paused on the title, in the options and while the window has no focus) and the game's own time (the sum of `dt`, which the harness's fixed clock makes repeatable) side by side; splits taken by itself at `GiveBadge`, at named flags (each commander beaten, Spear Pillar) and at the region's `StoryCompleteFlag`; a corner of the field and the battle (`ModernUi.Field`, top right, where nothing stands) behind a new `OptionRow.RunTimer`, off by default; `splits.json` beside the save in a plain shape a spreadsheet or an autosplitter reads.

**Two people at one screen.** `BattleSetup` gains `SecondSide`, a `Trainer` whose choices come from outside (`CoreSetup.EnemyController = null`). `BeginChoosing` then asks every place of the `ActionRequest`, the player's first, and the HUD's menus open for the side whose turn it is, from its own side; a `ReplacementRequest` for an enemy place is asked the same way. Between the sides a curtain (`CurtainScreen`: "Pass the controller to {name}", any button) hides the choice just made; with two pads `InputManager` takes a pad index, the second side reads pad 1 and there is no curtain. Against the AI the second side keeps `TrainerAi.Instance`. The battle is fought on copies of both teams (`Pokemon.CopyStateFrom` into fresh ones), so neither save changes, and it is reached from VERSUS on the title (`TitleChoice.Versus`, `GameState.Versus`, `VersusScreen`). This is plan 07 · O3's menu change delivered early, built once for a remote player to use too.

**GPU-free and drawn.** `RunRules`, `RunState`, `DifficultyRules`, `Randomizer`, `Statistics`, `Medals`, `RunTimer`, `DailyChallenge`, the team builder's state and every change to `BattleCore`, `Party`, `Pokemon` and the scripts take no window. What draws: the title's run panel, the card's pages, the medal notice, the timer's corner, the curtain, the versus and team-builder screens, all on the kit, each with harness shots, and two runs must still draw the same pictures.

## Sessions

### V1 · The choice at NEW GAME
- `Models/RunRules.cs` with `Difficulty` and the presets; `TitlePhase.ChooseRun` after `ChooseRules` (`TitleScreen.Run`; rows like the options', the preset first; Confirm on Normal leaves at once, Cancel goes back to the rules); `GameEngine.StartNewGame(name, look, rules, run)` and `newGameRun`; `SaveData.Run` and `RunState`; `RunRules.Current` set in `StartNewGame` and `ApplySaveData`; the run on `SaveSummary` (one line under the badges, nothing when Normal) and on `CardPage.Run` of the Trainer Card; `docs/mechanics/rulings.md` gains "Ways to play": each choice, what it changes, Normal changing nothing.
- Style guide first: "Opening and title screen" gains the step after "which rules?", "Menu screens (G10)" the card's pages.
- Tests: `TitleScreenTests` walk both prompts and hold Normal as the default; `RunRules` round-trips through JSON and an old save reads as Normal; `IsNormal`.
- Shots: `title_*` after `title_11_confirm_new_game`, the panel on its preset and on a flag; `28b_trainer_card_run`.
- **Done when** a game started with the Nuzlocke preset is saved, continued and seen on its card, and plays exactly as before because nothing enforces it yet.

### V2 · Challenge and Easy
- `Models/DifficultyRules.cs`: the table by class, `Prepare`, the extra member from the rematch team in `Data/trainers.json`, the items, the prize money and the EXP; `StartTrainerBattle` on the copy; `ShopScreen`'s prices; the copy's `Trainer.Ai` raised to every trainer routine (plan 06 · R9's flags).
- Tests: `Prepare` under Normal returns the same object; the table's numbers; a leader's copy has four under Challenge; the harness's `all` run diffs clean against a run before.
- **Done when** Roark under Challenge has his levels raised, a fourth Pokémon and a held item, and under Normal the `battle` shots are what they were.

### V3 · The level cap and scaling
- The cap table; `AwardExp`'s clamp and its line; the Rare Candy refusing; `ScaleTrainers` in `Prepare`.
- Tests: EXP stops at the threshold and goes on with the badge; a scaled party keeps its spread; the `Steady()` scenarios' numbers are unchanged without the cap.
- **Done when** a capped Turtwig stops at 14 before the Coal Badge and grows after it, and a scaled Youngster meets a level 30 lead at 30.

### V4 · Nuzlocke
- The areas met, written in `StartWildBattle`; `CoreSetup.Run` and `WhyNotItem` refusing a ball where the area is spent and a revive at any time, with the dupes clause letting an encounter of a line already caught pass; the graveyard filled in `EndBattle` from the fainted, out of the party for good (`FieldItems.WouldHelp` false for them); the nickname prompt after a catch (`GameState.Nickname` on the introduction's keyboard through `NameEntry`); a whiteout with nobody left ends the run (`RunState.Ended`), the save stays and `SaveScreen` refuses; the graveyard on `CardPage.Run`.
- Tests: the second encounter in an area can't be caught and the first can; a fainted member is in the graveyard after the battle and no revive reaches it; the run ends on a whiteout and the save still loads.
- Shots: `98_nuzlocke_ball_refused`, `98_nickname`, `28b_trainer_card_graveyard`.
- **Done when** a Nuzlocke from Twinleaf Town to Jubilife City plays by its rules through the game's own battles and the headless host alike.

### V5 · Restrictions and shiny odds
- `Party.Limit`; `Query.Rule` and `IScriptHost.Rules` with `common.Nurse` declining under `nocenter` in a line of our own; monotype in `WhyNotItem`, `GivePokemon` and the PC's withdraw, the starter exempt; `NoBattleItems` (the BAG command greyed in `BattleHUD`), `NoCatching` and `NoRunning` in `WhyNot`; `SetStyle` locking plan 08 · P1's row (nothing before P1, since the offer doesn't exist); `ShinyDenominator` read by `Pokemon`'s constructor from `RunRules.Current`, its 1,024 and 512 steps beyond both presets and so in the run's record, never the `Ruleset`, so Platinum's 1 in 8,192 stands; the Shiny Charm in the bag re-rolling twice in `StartWildBattle` and `GivePokemon`; encounters by species counted, shown on the statistics page and not on the Pokédex's entry, whose pages are Platinum's.
- Tests: each flag through the core or the host; the odds hold with nothing on; the charm's three rolls.
- Shots: `st13_nurse_declines`, `98_bag_greyed`.
- **Done when** every flag of the record refuses where it should and nowhere else.

### V6 · The randomiser: species and items
- `Core/SplitMix.cs`; `Models/Randomizer.cs` with the species and item mappings; the seed on the title (`NameEntry`'s digits, or rolled); its points of use; `Habitats` in reverse; the seed on the card.
- Tests: one seed, one mapping, held against a table written into the test; every mapped name exists; the three starters map to three first stages; no seed maps everything to itself; plan 03 · D12's completeness test under a seed once D12 exists.
- Shots: `95_versus_*` under a seed, `26e_pokedex_area_*` under it.
- **Done when** a seeded game meets, fights and finds different species and items, the same ones every time, with the data files untouched.

### V7 · The randomiser: abilities, moves and types
- `Randomizer.Use` swapping species' abilities, learnsets and, behind its toggle, types in memory, put back by `StartNewGame` and `ApplySaveData`; trainers' chosen moves mapped by type and power in `Prepare`, so a leader keeps a move of their type.
- Tests: the swap and its undo leave the databases as loaded; Roark's copy keeps a Rock move; `CoverageTests` unchanged.
- **Done when** the toggles work and a game started without them reads the data as before.

### V8 · Counters and the statistics page
- `SaveData.Counters`, `Models/Statistics.cs` naming every key, `Count` at every point named above; `CardPage.Statistics` in groups with `Tabs`; the run card written by `Raylib.ExportImage` of the virtual screen (`run_<day>.png` beside the save) from the page and at the Hall of Fame; the numbers handed to plan 06 · R12's Hall of Fame record.
- Tests: every key the engine increments is named; the counters survive a save round trip (the first test that round-trips a whole `SaveData`); a battle won counts once.
- Shots: `28c_trainer_card_statistics`.
- **Done when** a walk, a battle and a catch show on the page and in the file.

### V9 · Medals
- `Models/Medals.cs` (about 120 at first, in six categories), `SaveData.Medals`, the check, the notice with its sign (style guide "Field menus and notices" first), `MusicRole.Medal` and its fanfare of our own, `CardPage.Medals` with progress.
- Tests: every medal reachable from its counters; none earned twice; the fanfare heard through `AudioManager.Listen`.
- Shots: `l*` of the notice, `28d_trainer_card_medals`.
- **Done when** the first catch earns a medal on screen and in the save.

### V10 · The run timer and splits
- `Models/RunTimer.cs`, `RunState.Timer`, the splits at badges, flags and the Hall of Fame, `OptionRow.RunTimer`, the corner (style guide "Field menus and notices"), `splits.json`, the final time on the Hall of Fame record, category presets choosing which splits count.
- Tests: the timer reads the same for the same `dt`s; a badge takes a split; the file's shape.
- Shots: a `field` and a `battle` shot with the corner.
- **Done when** a timed run writes its splits and the corner costs nothing the frame budget notices.

### V11 · New Game+
- A third answer on `TitlePhase.ConfirmNewGame` when the save's region is complete (`Region.StoryCompleteFlag`): the Pokédex, the boxes (the party emptied into them), half the money, `RunState.Clears`, the difficulty a step up and `ScaleTrainers` on, applied in `StartNewGame` from the old save after `InitializeNewGame`; the region the save finished through `NewGameRegion`; the clears on the card.
- Tests: the carry-over; the raised difficulty; `StoryMigration` untouched (a fresh `StoryState`).
- **Done when** a finished save starts again in Sinnoh with its boxes and a harder road.

### V12 · Local versus
- `BattleSetup.SecondSide`, `BeginChoosing` for both sides and the enemy's replacement, the HUD from the chooser's side, `CurtainScreen`, `InputManager`'s pad index (pad 1 with pad 0's buttons until plan 12 · Q4's bindings widen it), `TitleChoice.Versus`, `GameState.Versus`, `VersusScreen` (hot-seat, two pads, against the AI; teams from this save's party or boxes, or a second save file; `VersusRules`: Flat 50, Flat 100, As is, handed to plan 07 · O6), nothing written to either save.
- Style guide: "Opening and title screen" gains VERSUS, "Battle panels" the curtain and the mirrored menus.
- Tests: a two-sided battle through the menus ends and its `BattleRecord` replays; the enemy's replacement is asked of the second side; both saves are byte for byte afterwards.
- Shots: `98_versus_curtain`, `98_versus_second_side`, `title_*` with VERSUS.
- **Done when** two people finish a battle on one keyboard and on two pads.

### V13 · The team builder and the gauntlet
- `TeamBuilderScreen` on the kit (species from the Pokédex's list, form, level, moves from the learnset, item, ability, nature), teams in `teams.json` in the working directory, the second player's name by `NameEntry`; the gauntlet (the eight leaders, the Elite Four and Cynthia, their teams read by id from plan 06 · R9's `Data/trainers.json`, which matches plan 02's pacing targets, and `Data/gauntlet.json` holding the order) and an endless streak of trainers generated by a `BattleRandom` seeded per streak; best streaks in `settings.json` (per machine, not per save).
- Tests: a built team is legal (moves learnable, one item each); the gauntlet's order; a streak's record replays.
- Shots: `98_team_builder`, `98_gauntlet_cynthia`.
- **Done when** a team built from nothing beats the gauntlet's first leader.

### V14 · The daily run
- `Models/DailyChallenge.cs`: the day's seed from the UTC date and the data version (plan 07 · O4's; a hash of the four data files until then), a `BattleRandom(seed)` drawing the rentals and seven opponents with `RollKind.Pick` (the Battle Factory's format: plan 06 · R18's pool and rules when they exist, the Sinnoh Pokédex at level 50 until then), the result kept in `daily.json` with its date; an entry under VERSUS; the harness hands it a fixed date; sharing waits for plan 07 · O7 and stays friends-only, never a board.
- Tests: a date gives the same run twice and on every machine; a result is kept once a day.
- Shots: `98_daily_rentals`.
- **Done when** two machines on one day face the same seven trainers.

## Risks

- **A rule enforced in one place and forgotten in another** (a script's `wildbattle`, a gift, the PC): each rule's one place is named above and `EveryRuleHasItsPlace` holds it; a new way to get a Pokémon or use an item asks `RunRules` there.
- **Two runs must still draw the same pictures**: Normal leaves the maps as loaded, the copy in `StartTrainerBattle` is made only when a rule asks for it, the randomiser rolls from its own seed, the daily takes a date, and the harness's modes run with nothing on. A rule that reached into map loading would move every shot of `all`.
- **Static state**: `RunRules.Current` is one more static beside `Ruleset.Current` and `PlayerIdentity`; tests run in parallel and must never set it.
- **Overlap**: Set belongs to plan 08 · P1, the second pad to plan 12 · Q4, both sides' menus to plan 07 · O3 and the rule sets to O6, the Factory's pool to plan 06 · R18, the stars to R12. Each session builds on the owner's work or hands its own over, never a second version.
- **Balance**: Challenge's numbers are ours. A table that makes Roark unwinnable at level 12 shows in a story walk under Challenge with the pacing party (plan 02's chapters, when they exist).
- **Scope**: fourteen sessions. V1 to V5 are the value, V12 and V13 the second, V14 the least and last.

## Needs and gives

- **Needs**: plan 06 · R1 (done: the choice, `Ruleset`, `BattleRandom`); plan 08 · P1 for Set forced; plan 06 · R9 (done: the trainer table with its rematch teams, `Trainer.Ai` and `Trainer.Items`) for Challenge's extra members and AI flags; plan 06 · R11 for the bag in battle beyond four items (no-items means more then); plan 06 · R12 for the Hall of Fame record (V8, V10) and the card's back; plan 09 · L1 for a shiny that is seen; plan 02 · S14 for the Hall of Fame flag (V10's last split, V11); plan 03 · D12 for the obtainability test under a seed; plan 07 · O4 for the data version and O7 for the daily's sharing; plan 12 · Q4 for the second pad in full.
- **Gives**: `SaveData.Counters` to plan 06 · R12's stars and Journal; both sides' menus to plan 07 · O3 and `VersusRules` to O6; `Party.Limit`; the rental gauntlet's format to plan 06 · R18; the level cap and `ScaleTrainers` to plan 18's crossing between regions; the run card to plan 16's playthrough bot as its summary.

## Decisions for the user

1. **Where the choice is made.** *Recommended:* on the title after "which rules?", one panel, left by Confirm when nothing is wanted. The alternative is the professor asking in the introduction, which costs a phase of `IntroScreen` and lines for every flag.
2. **What a Nuzlocke's end does.** *Recommended:* the run ends, the save stays, can be loaded to look at the card, the graveyard and the boxes, and can't be saved over. The alternative deletes it, as the strictest players do by hand.
3. **VERSUS on the title.** *Recommended:* yes, since it needs no save; Platinum's menu has no such entry, so the style guide says it is ours. The alternative is the Pokémon Center's upstairs counter, which plan 07 builds for online play.
4. **New Game+ and the chain.** *Recommended:* start the finished region again with the carry-over; the chain's default (Kanto onward) stays what a plain NEW GAME does. The alternative restarts the chain at Kanto with the team, which plan 18's crossing has to answer first.
5. **The daily run.** *Recommended:* after plan 06 · R18, local results only, and dropped if the Factory's own format satisfies. The alternative builds it now on a pool of its own.
6. **How many medals.** *Recommended:* about 120 in six categories, our own names and signs, grown with later plans. The alternative is Platinum's five stars only, from R12.

## Status

- [ ] V1 The choice at NEW GAME
- [ ] V2 Challenge and Easy
- [ ] V3 The level cap and scaling
- [ ] V4 Nuzlocke
- [ ] V5 Restrictions and shiny odds
- [ ] V6 The randomiser: species and items
- [ ] V7 The randomiser: abilities, moves and types
- [ ] V8 Counters and the statistics page
- [ ] V9 Medals
- [ ] V10 The run timer and splits
- [ ] V11 New Game+
- [ ] V12 Local versus
- [ ] V13 The team builder and the gauntlet
- [ ] V14 The daily run
