# Plan 14 · Battle formats of the later games and the venues that use them

Written 2026-10-06, before any session.

**Goal**: the formats Platinum never had, each a change to the rules first and to the screen second: inverse battles and sky battles (a flag and a filter), hordes (five against one), SOS calls (a second foe joining mid-battle), triple and rotation battles (three places a side, reach by position), and, last and optional, the Battle Royal (four sides); then the venues that put them to use: Emerald's five other Frontier facilities as a second wing of plan 06 · R18's Battle Frontier, and the Pokémon World Tournament as a bracket played at level 50 against Sinnoh's leaders first and every later region's as its plan adds them. Platinum's own battles don't move: Sinnoh's routes and trainers keep Platinum's formats, and every new format is met in the post-game, in a facility, in plan 13's Versus, or in the region it came from.

## Where we are

- **Two places a side, two sides.** `BattleFormat` (`Battle/BattleSetup.cs`) is `Single` and `Double`; `BattleCore`'s constructor makes `PlayerSlots` and `EnemySlots` of the same count from it, and falls back to a single when a side can't field two. `Place.Number` (`Battle/Sim/BattleLog.cs`) counts places as the original does, `Slot * 2 + side`; `FieldState.Places` (`BattleField.cs`) is an array of four read by `FieldState.At` with the same arithmetic, `FieldState.Sides` two `SideState`s, and `BattleCore.ControllerOf` picks a place's chooser by its side (`enemyController`, `playerController`, and since plan 06 · R9 `partnerController` for a partner's place), with `BattleSide` declared in `Battle/BattleAnimator.cs` as `Player, Enemy`. `Battler.Turn.PhysicalFrom` is indexed by `Place.Number` too.
- **Targets by kind, not by distance.** `ResolveTargets` (`BattleCore.Moves.cs`) picks targets from `MoveTarget` and `SlotsOf(Other(side))`; `Reaches` marks `MoveHit.OutOfReach` only for a target that is elsewhere (`Battler.IsElsewhere`). `MoveFlags` (`Data/Enums.cs`) has no flag for reach. `TrainerAi.ChooseAction` scores each usable move against `ActiveFoes(mine)` and the one ally beside it, and `ChooseReplacement` goes in party order.
- **What may be chosen** is `WhyNot`, `WhyNotMove` and `UsableMoves` (`BattleCore.Choices.cs`); `WhyNotItem` (`BattleCore.Items.cs`) already refuses a ball when more than one foe stands ("There are two Pokémon out!").
- **The chart** is `TypeChart.GetEffectiveness(attack, defender1, defender2, rules)` (`Data/TypeChart.cs`), read type by type by `DamageCalculator.Matchups` (for `Effectiveness` and `Calculate`), by Conversion 2 in `BattleCore.Unique.cs` and by Stealth Rock in `BattleCore.Switch.cs`. `BattleConditions` (`Battle/Sim/Formulas.cs`) carries what the rules need of the world: `Terrain`, `Night`, `HasCaught`, `Weather`, `TrickRoom`; `GameEngine.BattleConditionsHere()` builds it.
- **Where a format comes from.** `GameEngine.StartTrainerBattle` sets `Format` from `Trainer.DoubleBattle` (`Models/Trainer.cs`; the overlay's `MapFile.TrainerRecord.DoubleBattle`). `GameEngine.StartWildBattle(WildEncounterEntry)` makes one Pokémon of the entry `Map.RollWildEncounter` returned (a species, a level range, a weight; `Overworld/Tile.cs`) from the map's own `Dice.New()` generator; `BattleSetup.WildPokemon` is a list already. A script's `battle self [canlose]` is `Op.Battle` (`ScriptParser`, `Instruction.Option`) and `IScriptHost.Battle(NPC, bool)`.
- **The screen.** `BattleAnimator.Slots` is one count for both sides and `views[side, slot]` holds two a side; `BattleRenderer.Spot(side, slot, slots)` lays platforms round `BattleStage.EnemySpot` and `PlayerSpot` (radii 2.4 and 1.2), `SlotFit` shrinks a Pokémon to 0.78 in a double; `ModernUi.Battle.DrawBattle` draws a `CompactBox` for `slot < 2`; `BattleEngine.TargetLayout` is four cards two to a row and `TargetStep` walks them; `BattleCamera.Overview` orbits at `OverviewFov` 24° and `WidestFov` is 40. `BattleArena` (`Overworld/Tile.cs`) is Grass, Forest, Cave, Water, Snow, Sand, Indoors, Gym, League; `BattleArenas.Build` switches on it, `SkyPainter.Draw` paints every outdoor stage's sky, `ArtLook.ArenaRig(spec, hour)` lights it.
- **Replays and the fuzz.** `BattleRecord(Seed, Answers)`, `BattleCore.Record` and `Replay`; `BattleCoreTests.ARecordedBattleReplaysTheSame` plays the game's own menus, and `RandomBattlesAlwaysEndAndLeaveNothingOutOfRange` plays 150 battles, singles and doubles, both sides chosen by `TrainerAi`. `CoreScenario` (`PokemonPlatinumTests`) has `Wild`, `Against`, `Doubles`, `Turn`, `DoubleTurn`, `SwitchTo`, `SendIn`, `Calm` and the places `Mine`, `Foe`, `Mine2`, `Foe2`.
- **Venues.** `Data/world/sinnoh/world.json` opens 23 areas, the last the Ironworks; the Battle Zone's buildings (`d31_s01` the Tower, `d32_s01` to `d32_s04`) are plan 01 · M10's stand-ins (`docs/world-models.md`) and their rooms M11's. Plan 06 · R18 owns Platinum's five facilities, Battle Points and the Brains, and none of it exists; plan 07 · O6 names Flat 50 and its clauses and plan 13 · V12 `VersusRules` with the same three, and no level scaling exists; plan 08 · P6 reads the original's trainer table into `Data/trainers.json`, P7 reads trainers from it, and P10's record file carries a battle's format, rules and conditions; plan 13 · V13 has a gauntlet of the leaders and V14 the Factory-style rental draw. `MusicRole` (`Audio/MusicDirector.cs`) has no role for a facility or a tournament; `ScriptScreen` is `Starter, Shop, Pc, Travel`; `GameState` has no state beyond the menus; `CharacterStyle.For(npcType)` has the named cast and a few classes, the rest plan 11 · C4 to C6.
- **Pokémon copies**: `BattleMirror.Copy` clones (`Pokemon.Clone()`); `Pokemon.Level` has a private setter and `RecalculateStats()` is public.

## Design

**One core, more places.** `BattleFormat` gains `Horde`, `Sos`, `Triple` and `Rotation` (`Royal` only in B13), and a `BattleFormat.PlacesOf(side)` says how many places each side has (1/1, 2/2, 1/5, 1/2, 3/3, 3/3). `BattleCore`'s constructor makes each side's slots from it in place of one `slots`, and its fallback (a double a side can't fill) becomes `BattleFormat.FallBack`. `Place.Number` keeps its formula and `Place.MaxNumber` (10) sizes `FieldState.Places` and `Battler.Turn.PhysicalFrom` instead of 4. `BattleAnimator.Slots` becomes `SlotsOf(side)` with `views` of five a side, and `BattleEngine` sets both from the core. Nothing a single or a double does changes: `BattleCoreTests` run unchanged, and the harness's `battle`, `doubles` and `demo` modes diff clean against a run made before.

**Every new kind of choice or request** (a shift, a rotation, a place opened by a call) is a value of `ChoiceKind` or a field of `BattleChoice`, refused by `WhyNot` where it can't be made, played by `ARecordedBattleReplaysTheSame`, and given a turn of the fuzz, which plays every format in rotation. Nothing in a format may draw from anything but the battle's `BattleRandom` (`RollKind.Call` for an SOS, `RollKind.Target` as now).

**Where a format comes from, and Platinum's defaults.** A trainer's format is `TrainerRecord.Format` (`"double"`, `"triple"`, `"rotation"`, `"sky"`; `DoubleBattle` stays and means `"double"`), a wild horde the table's `WildEncounterEntry.Count`, an SOS the area's `callsForHelp`, an inverse battle a word on the script's line or a facility's rule. The imported overlays carry Platinum's trainers and tables, so no Sinnoh route gains a later format unless an overlay is edited by hand; the later formats are met at the Battle Zone's post-game characters, in the facilities, in the new zone plan 03 · D12 opens, in plan 13 · V12's Versus, and in the regions they came from (plan 22's Unova, plan 23's Kalos and Alola). `docs/mechanics/rulings.md` gets a section "Formats of the later games" for each rule taken from a later game and each stand-in.

**Flags on the conditions.** `BattleConditions.Inverse` inverts the chart in one place, `TypeChart.GetEffectiveness` (2 becomes ½, ½ becomes 2, 0 becomes 2, as X and Y have it), passed through by `Matchups`, Conversion 2 and Stealth Rock, so Anticipation, Wonder Guard, the resist berries, an Expert Belt, `HitSounded` and the lines follow with no code of their own. `BattleConditions.InTheAir` is a sky battle: `WhyNotMove` refuses the grounded moves (the list from Showdown's data of X and Y, kept in `Data/sky-battle.json` by the importer), and `BattleCore` refuses a side that fields nobody eligible before it starts. Both are kept by plan 08 · P10's record file.

**Reach.** `BattleCore.InReach(user, target)` is adjacency by slot: on the other side a place faces the one across from it (slot `2 − theirs` in a triple) and its neighbours, on the user's side the neighbours; `MoveFlags.Distance` (Showdown's `distance` flag, read by the importer) reaches anywhere. `ResolveTargets`, the menus' `TargetChoices` and a spread move's list are filtered by it; a move left with no target fails with the original's line. In a single or a double everything is in reach, so nothing there changes. A `Shift` is a `ChoiceKind` of its own: the Pokémon at an end trades places with the centre and that is its turn. A side down to one Pokémon has it moved to the centre, so a battle can't stall out of reach (the fuzz holds it). In a rotation battle the three places exist but only the front one `IsActive` for targets, the end of the turn and entry checks; `BattleChoice.Rotation` (−1, 0, +1) goes with a Fight, rotations happen at the turn's start in speed order, and a Pokémon rotated out keeps its stages and volatiles, as Black and White have it.

**Hordes and calls.** A horde is five wild Pokémon of the entry's species at levels rolled each (`WildEncounterRules.Level`), one of the player's, every foe chosen for by `TrainerAi`; spread moves take their ×0.75 (`Formulas.BaseDamage`), a ball is refused while more than one stands (`WhyNotItem`, generalised from two), EXP comes per faint as now. An SOS battle is a single whose enemy side has a second, empty place: `BattleCore.Turn.cs` gains a step after the end-of-turn effects where a wild foe may call (`RollKind.Call`: the species' call rate, doubled when its HP is low or its ally fell this turn, the Alola games' numbers), answered by `Arrive` into the empty place with an `Entered` event, which the screen shows as a send-out without a ball. `BattleConditions.SosChain` counts the calls answered this battle and `SosChain.Bonus(chain)` (`Overworld/WildEncounterRules.cs`, GPU-free) gives the ally's guaranteed IVs, its hidden-ability chance and its shiny odds, never past the odds plan 13 · V5 sets; the Adrenaline Orb is a hold effect for plan 06 · R28. A caller never calls at a place that is full, so there are never more than two.

**The screen.** `BattleRenderer.Spot` lays n places on an arc round each side's spot (three at 0.6 of a single's size, five at 0.45; style guide, "Stage"); `BattleCamera.Overview` widens its lens by the places in view (up to `WidestFov`), a target shot frames whoever a spread move hits; `ModernUi.Battle` draws a `CompactBox` per place (a horde's five stacked small on the left); `TargetLayout` becomes rows of the format's width and `TargetStep` walks a grid (style guide, "Battle panels": the six-card and the five-and-one target menus, the SHIFT button and the rotation arrows beside FIGHT); the rotation's platforms sit on a turntable that turns as the rotation is shown. `BattleArena.Sky` is the sky battle's stage: platforms of cloud over `SkyPainter`'s sky, a row of the "Arenas (G8)" table, an `ArenaRig` of its own. Everything drawn is a function of `BattleAnimator.Time` and the cue's seed, so two runs draw the same; the frame cost of five Pokémon is measured with `profile` before B4 is called done (the budget is 8 ms on High).

**The venues.** Emerald's facilities are GPU-free rules each (`Models/Frontier/`), their screens on the kit, their buildings our own: the Dome's bracket (`Bracket`: sixteen entrants, four rounds, drawn by `Dice`, each trainer's card from its team), which the World Tournament reuses at eight; the Palace's `PalaceController : IBattleController` on the player's side (`CoreSetup.PlayerController`), choosing by the Pokémon's nature from Emerald's table of attack, defence and support moves, so the player's menus never open; the Arena's judge (`ArenaJudge` reads three turns of the log: Mind from the moves chosen, Skill from the hits landed, Body from the HP left) with `CoreSetup.TurnLimit` ending a bout and a `Judged` event naming the winner; the Pike's rooms of chance as a small hand-made map whose doors a script sends through (`Dice` picks the room); the Pyramid's floors generated on a `BattleRandom` seed from pieces of our own, dark under `Overworld/Darkness.cs` with a circle that grows per foe beaten, items as `Map.HiddenItems`, seven floors. Battle Points, streaks, the rental draw and the Brains' challenges at 21 and 49 are R18's; this plan adds its five to R18's record. The World Tournament is `Models/Tournament.cs` (GPU-free): an event, a bracket of eight, the player's three at level 50 by `LevelScaling.At(pokemon, 50)` (a clone with its level set and `RecalculateStats`, in `Battle/Sim/LevelScaling.cs`, written once for plan 07 · O6 and plan 13 · V12 to use, or taken from whichever lands first), species and item clauses at the pick, the other matches of a round played headless by the core with `TrainerAi` on both sides so the bracket is real, nothing written to the save but the record and the Battle Points; its teams in `Data/tournaments.json` in `TrainerRecord`'s shape, Sinnoh's eight leaders and Cynthia first (the overlays' teams once plan 02's chapters place them, plan 08 · P7's table where it has them), every later region's as its plan adds its table.

## Sessions

### B1 · Inverse battles
- `BattleConditions.Inverse`, the inversion in `TypeChart.GetEffectiveness`, and the word `inverse` on a script's `battle` line (`ScriptParser`, `Instruction`, `IScriptHost.Battle` gains the conditions; both hosts; a row in `docs/scripts.md`; a line in `ScriptTests.EveryCommand`). A character at the Fight Area (`overlays/fight_area.json`, once plan 01 · M10 opens it; until then a person of the harness's own scene) who offers one battle a day on plan 06 · R14's daily events, with lines of our own. The rulings' new section.
- Tests: the three cases on the bare core (a resisted hit doubled, a weakness halved, an immunity hit for double), Anticipation and a resist berry following, a replay with the flag, the script's word.
- Shots: a new harness mode `formats` (not part of `all`), `ft01_inverse_line` with "It's super effective!" on a resisted type.
- **Done when** a Ghost takes a Normal hit for double under the flag and nothing changes without it.

### B2 · Sky battles
- `BattleConditions.InTheAir`, the eligibility (Flying type or Levitate by `Battler.Ability`, the exceptions of X and Y) checked in `BattleCore`'s constructor against the party and in `GameEngine.StartTrainerBattle` before the challenge (a trainer who can't be fought says so), `Data/sky-battle.json` of the grounded moves from Showdown through the importer, `WhyNotMove`'s refusal, `TrainerRecord.Format` with `"sky"`. `BattleArena.Sky` in `BattleArenas`, `ArtLook.ArenaRig`, `BattleStage.PlatformTopOf`; the style guide's row first.
- Tests: the filter, the refusal, Earthquake refused in the air, a replay.
- Shots: `ft02_sky_arena`, `ft03_sky_refused`; `profile` with the new arena.
- **Done when** a sky trainer in the harness is beaten with a Staraptor and refused with a Bidoof.

### B3 · Hordes: the core
- `BattleFormat.Horde`, `PlacesOf`, `Place.MaxNumber`, the widened arrays, `WildEncounterEntry.Count`, `GameEngine.StartWildBattle` making `Count` foes, `WhyNotItem` at more than one, `TrainerAi` per foe, a horde's run check against the fastest foe.
- Tests: `CoreScenario.Horde` (five foes, places `Foe3` to `Foe5`), a spread move hitting five at ×0.75, a ball refused until one is left, EXP per faint, the replay and the fuzz with hordes in rotation; every existing test unchanged.
- **Done when** the bare core plays a horde to its end headless and the fuzz has run hordes a hundred times.

### B4 · Hordes on screen and in the grass
- `BattleAnimator.SlotsOf`, `views` of five, `BattleRenderer.Spot` on an arc, `SlotFit` by count, the camera's wider overview, five `CompactBox`es, `TargetLayout` five and one and `TargetStep` on a grid, the send-out of five with their cries staggered. A horde slot in the new zone's tables (plan 03 · D12) only; the style guide's "Stage" and "Battle panels" first.
- Tests: the layout's walk over five cards, a presentation test of five send-outs.
- Shots: `ft10_horde_intro`, `ft11_horde_targets`, `ft12_horde_spread`; `diff` of `battle` and `doubles` against a run before; `profile`'s horde line under 8 ms on High.
- **Done when** five Zubat come out, Rock Slide hits them all and the ball is refused on screen.

### B5 · SOS calls
- `BattleFormat.Sos`, the empty second place, the call step in `BattleCore.Turn.cs`, `RollKind.Call`, `Arrive` into an empty place, `BattleConditions.SosChain`, `SosChain.Bonus`, `callsForHelp` in an area's overlay (no Sinnoh area sets it; plan 23's Alola does), the engine showing an `Entered` without a ball. A rule of ours for which species answer whom (the species' own family, with the Alola games' pairs where they exist) in `Data/sos-allies.json`, from PokeAPI's data where it has them.
- Tests: a call fills the place, never a full one, a chain of ten gives the IVs the table says, catching refused while two stand, the chain ends on a catch or a faint of the caller with nobody left, a replay with calls.
- Shots: `ft20_sos_call`, `ft21_sos_two_out`.
- **Done when** a Pikipek calls a second in a harness battle and the chain's bonus is seen on the catch.

### B6 · Triple battles
- `BattleFormat.Triple`, `InReach`, `MoveFlags.Distance` through the importer (and `docs/data-files.md`), the filters in `ResolveTargets` and the menus, `ChoiceKind.Shift`, the move to the centre, `TrainerRecord.Format` with `"triple"`; on screen three platforms a side, the six-card target menu, the SHIFT button, the camera's framing of three.
- Tests: `CoreScenario.Triples` and `TripleTurn`; a move from an end can't reach the far foe and Flamethrower with `Distance` can; a shift takes the turn; the last Pokémon moved to the centre; the replay and the fuzz with triples.
- Shots: `ft30_triple_intro`, `ft31_triple_targets`, `ft32_triple_shift`.
- **Done when** a triple is played through the game's menus and the fuzz has run it a hundred times.

### B7 · Rotation battles
- `BattleFormat.Rotation`, the front place, `BattleChoice.Rotation`, rotations at the turn's start, what stays on a Pokémon rotated out, the end of the turn and entry checks on the front only; `TrainerAi` rotating to the Pokémon whose best move scores highest; the turntable on screen, the rotation arrows beside FIGHT, `BattleCamera`'s shot of the turn.
- Tests: a rotation keeps stages and a Substitute, poison ticks on the front only, a rotated-in Pokémon uses the move chosen for it, the replay and the fuzz.
- Shots: `ft40_rotation_menu`, `ft41_rotation_turn`.
- **Done when** a rotation battle in the harness turns three times and replays.

### B8 · The Battle Dome and its bracket
- `Models/Frontier/Bracket.cs` (sixteen, four rounds, `Dice`), `BracketScreen` on `ModernUi.Lists` (a new `ScriptScreen.Bracket` opened by the Dome's desk script), the other matches decided headless, the Dome's rules (one of two of a trainer's Pokémon seen beforehand, its card), its building's rooms and desk, the prize and streak on R18's record; a new harness mode `venues` (not part of `all`).
- Tests: a bracket of sixteen always has one winner, a draw is the same for a seed, a round with the player out ends the challenge.
- Shots: `vn01_dome_bracket`, `vn02_dome_card`; the style guide's "Menu screens (G10)" gains the bracket.
- **Done when** a bracket is won in the harness and its record replays.

### B9 · The Palace and the Arena
- `PalaceController` (Emerald's nature table as data of our own transcribing, `Data/palace-natures.json`), the menus closed for the player's side with the "is thinking" line, `CoreSetup.TurnLimit`, `ArenaJudge` and the `Judged` event, the Arena's three bouts against a trainer's three; both buildings' rooms and desks, prizes and streaks.
- Tests: a Pokémon of each nature picks from the group its table says at the rate it says, a bout judged on each of the three counts, a judged battle ends in three turns and replays.
- Shots: `vn10_palace_thinking`, `vn11_arena_judged`.
- **Done when** a Palace battle plays with no menu opened and an Arena bout is judged on screen.

### B10 · The Pike and the Pyramid
- The Pike's map (`Data/maps/BattlePike.json`) with its rooms of chance and the script that sends through its doors (`Dice`; the attendant's hint), the Pyramid's generated floors (`Models/Frontier/PyramidFloor.cs`, seeded, from pieces of our own; a `Map` built from it through `MapFile.ToMap`), its darkness and the circle that grows, `Map.HiddenItems` placed by the seed, the floor's wild table from habitats of a theme, seven floors to the Brain; the Pyramid's own rule that Flash doesn't lift its dark (`Darkness.Covers` asks the map).
- Tests: the Pike's rooms come in the original's odds over a thousand draws, every Pyramid floor can be walked from its stairs to the next (`WorldWalk`'s way, on the generated map), a seed gives the same floor twice.
- Shots: `vn20_pike_doors`, `vn21_pyramid_dark`, `vn22_pyramid_lit`.
- **Done when** the Pyramid's seventh floor is reached in the harness with the same floors on a second run.

### B11 · The World Tournament: Sinnoh's leaders
- `Models/Tournament.cs`, `Battle/Sim/LevelScaling.cs`, `Data/tournaments.json` (Sinnoh Leaders, the Champion's event; teams as the overlays and plan 08 · P7's table give them, at 50), the clauses at the pick, `TournamentScreen` (the events, the pick, the bracket of eight from B8's `Bracket`), `ScriptScreen.Tournament` and the hall's desk script, the matches through `GameEngine`'s battle start with copies at 50 and nothing paid or gained, the prize in R18's Battle Points, `MusicRole.Tournament`, `BattleTournament` and `VictoryTournament` with songs of our own in `Data/music/common/tournament/`.
- Tests: a bracket plays through headless with `CoreSetup.PlayerController`, a party is untouched by a tournament, every team of the file is legal (moves learnable, one item each), a scaled copy has the stats of level 50.
- Shots: `vn30_pwt_events`, `vn31_pwt_pick`, `vn32_pwt_bracket`, `vn33_pwt_cynthia`.
- **Done when** the Champion's event is won in the harness and the save is byte for byte afterwards but its record.

### B12 · The World Tournament: events and the other regions
- The type and mixed brackets, doubles and (after B6) triples and rotations as events, the rental event on plan 13 · V14's draw, the file's shape per region (`tournaments.json` grows a section as plan 19 · Kanto, 20 · Johto, 21 · Hoenn and 22 · Unova add their tables), looks from plan 11's classes and named cast with `CharacterStyle.For`'s default as the stand-in, the World Leaders and World Champions events once two regions' tables exist.
- Tests: an event's bracket seats only entrants of its kind, a region's section is read when its data exists and skipped when it doesn't.
- Shots: `vn34_pwt_type_bracket`, `vn35_pwt_doubles`.
- **Done when** a type bracket is played and a second region's leaders appear the day its table lands.

### B13 · The Battle Royal (optional)
- `BattleFormat.Royal`: `BattleSide` gains `Third` and `Fourth`, `FieldState.Sides` four, `controllers` four, `Other(side)` becomes `Foes(side)` and `ActiveFoes` every other side, `SideDefeated` any side, the end when the first side falls, the score (knock-outs plus Pokémon left) and `BattleResult.Placed`; `TrainerAi` scoring every other side; four platforms in a ring, the camera's ring shots, the four-way target menu, the Royal in plan 13 · V12's Versus and a desk in the Frontier's annex until plan 23's Alola builds the Dome and the Masked Royal.
- Tests: a four-side battle always ends and replays, the score, the fuzz with four sides.
- Shots: `ft50_royal_ring`, `ft51_royal_targets`.
- **Done when** four trainers fight to the first fall in the harness and the fuzz has run it a hundred times.

## Risks

- **`BattleSide` as two values** is in the log's `Place`, the field's arrays, the animator's views and the renderer's player-or-enemy split. B3 to B7 keep two sides and only widen the slots, so the risk stays in B13, which is optional; everything before it is checked by the unchanged tests and a clean `diff` of the old modes.
- **Reach** touches `ResolveTargets`, the menus and every spread move; a rule that reads "the other side" without asking reach is a bug only a triple shows. B6's fuzz with triples is the net, and `InReach` is the one place reach is decided.
- **Balance and Platinum's defaults**: a horde slot or a sky trainer on a Platinum route would move Platinum's play; this plan places them only where decision 1 says, and a new test in `WorldTests` holds every open area's trainer formats and tables to what the import gave.
- **Frame cost**: five Pokémon, five boxes and a wider lens fill more of 4K; B4 measures with `profile` and `SlotFit` shrinks models before anything is drawn twice.
- **The facilities' rules** are Emerald's, read from pokeemerald's code at a pinned commit as the importers read Platinum's decompilation (numbers and layouts only, never a line of its text); where a number can't be read it is our own and the rulings say so.
- **Two tournaments' data** (the leaders' teams) will exist in the overlays, plan 08's table and `tournaments.json`; B11 reads them in that order and writes nothing twice.

## Needs and gives

- **Needs**: plan 06 · R2 to R8 (done: the core, the log, every move, ability and item); plan 06 · R9's trainer AI for the facilities' opponents to be worth fighting (the simple chooser does until then); plan 06 · R14's daily events (B1's character), R18 (B8 to B11: Battle Points, records, rentals, the Frontier's rooms and the Brains' shape), R19's modern preset (the SOS and horde rules sit under either preset but are met only where decision 1 allows), R28 (the Adrenaline Orb); plan 01 · M10 (the Battle Zone) and M11 (rooms) for every venue; plan 03 · D12 (the new zone's tables) for B4's horde slot; plan 08 · P6/P7 (the trainer table) for the leaders' own teams, P10 (the record file carries the conditions and the format); plan 13 · V12 (Versus plays every format), V14 (the rental draw); plan 07 · O6 or plan 13 · V12 for `LevelScaling` if either lands first; plan 11 for looks beyond the stand-in.
- **Gives**: `BattleFormat.PlacesOf` and the widened places to plan 06 · R9's tag battles and to plan 07 · O8's doubles online; `LevelScaling` and the clauses to plan 07 · O6 and plan 13 · V12; the formats to plan 13 · V12's Versus; `Bracket` to plan 13 · V13's gauntlet; triples, rotations and the World Tournament to plan 22 (Unova); inverse and sky battles, hordes and `BattleArena.Sky` to plan 23 (Kalos); SOS calls, the chain and the Royal to plan 23 (Alola, the totems); the five facilities to plan 21 (Hoenn's Frontier); the format and the flags to plan 08 · P10's file.

## Decisions for the user

1. **Where the later formats are met in Sinnoh.** *Recommended:* only after the Hall of Fame: the Battle Zone's characters, the facilities, the World Tournament, plan 03 · D12's new zone, and plan 13's Versus; Platinum's routes and trainers keep Platinum's formats under both rule presets. Alternative: sky trainers and horde slots on Sinnoh's routes under the modern preset, with the balance that costs.
2. **Where Emerald's five facilities stand.** *Recommended:* an annex of Sinnoh's Battle Frontier, one building of our own beside Platinum's five with the five inside, so they are played before Hoenn exists and plan 21 reuses their rules and rooms. Alternative: wait for Hoenn and build them there only.
3. **Where the World Tournament is held.** *Recommended:* Jubilife's Global Terminal, whose door plan 01 · M11 opens and which plan 07 leaves empty (its counter is in the Pokémon Centers), unlocked by the Hall of Fame. Alternative: a hall of its own in the Fight Area.
4. **The triple's shift.** *Recommended:* Black and White's rule, the shift is the Pokémon's turn, and a side's last Pokémon moves to the centre by itself; the session checks what X and Y changed against Showdown's code and the rulings record the choice. Alternative: X and Y's rule throughout.
5. **The Battle Royal.** *Recommended:* B13 stays optional and last, built only when plan 23's Alola is near, since it alone makes `BattleSide` more than two. Alternative: build it with B7 so Versus has it early.
6. **Who the SOS allies are.** *Recommended:* the species' own evolutionary family plus the Alola games' pairs where PokeAPI has them, as data of our own. Alternative: the family only.

## Status

- [ ] B1 Inverse battles
- [ ] B2 Sky battles
- [ ] B3 Hordes: the core
- [ ] B4 Hordes on screen and in the grass
- [ ] B5 SOS calls
- [ ] B6 Triple battles
- [ ] B7 Rotation battles
- [ ] B8 The Battle Dome and its bracket
- [ ] B9 The Palace and the Arena
- [ ] B10 The Pike and the Pyramid
- [ ] B11 The World Tournament: Sinnoh's leaders
- [ ] B12 The World Tournament: events and the other regions
- [ ] B13 The Battle Royal (optional)
