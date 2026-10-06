# Plan 10 · Companions: Pokémon in the field

Written 2026-10-06, before any session.

**Goal**: Pokémon seen outside battle. Species sprites for the field, so Platinum's own stationary Pokémon stand where the original puts them (the mine's Machop, the Windworks' Drifloon, later the Psyduck, the lake guardians, Rotom's TV, Giratina) and the surfer rides their own Pokémon; the party's lead walking behind the player as in HeartGold and SoulSilver, behind an option; wild Pokémon visible in the grass, behind an option, so Platinum's grass stays as it was; a camp where the party is petted and fed, with cooking; and the small things later games remember about a catch: marks, alphas, the DexNav's search.

## Where we are

- **The only Pokémon drawn in the field is the surfer's mount**: `Graphics/SurfMount.cs` paints one dark-blue swimmer (48×26 texels, `Seat` 12 rows) for whatever knows Surf, and `WorldRenderer.GatherActors` draws it under the player when `Player.Mount` is set. Nothing else: no species sprite for the field, no Pokémon as a map object.
- **Menu sprites**: `PokemonSprites` (`Graphics/PokemonSprites.cs`) bakes `SpriteView.Front`, `Back` and `Icon` (128 and 48 px) from the 3D model at `PokePose { Time = 0.35f }`, outlines them, keeps them in `cache/sprites` keyed by `PokemonModels.Signature` and `BakeVersion`, and `Service` bakes the queue once a frame; `GetBaked` returns null while one isn't ready. `PokemonStudio.Strip` already renders a clip at several yaws into one image for the harness. `CharacterSprites` bakes people at `TexelsPerUnit` 32 (40×58 texels, `WalkFrames` 8, `IdleFrames` 2) and draws them with `DrawBillboard`; an item ball is a `CharacterSprites.Card` drawn with `DrawCard`, lit and casting like the people, and so since plan 02 · S2 is an obstacle (both are things of the map, `NPC.IsThing`, painted by `ThingCards` and drawn by `WorldRenderer.DrawThings`).
- **People**: `NPC.NpcType` is a string (`ItemBallType` is `"ItemBall"`); `WorldMapBuilder.PlaceEvents` (`Data/World.cs`) makes an `NPC` of every area object the overlay's `people` names, with `WorldMapBuilder.CharacterFor(looks)` picking the character, and skips the rest. The area files already carry Pokémon objects by species: `oreburgh_mine_b2f.json` has `machop_1` to `machop_3` (scripts 2, 4, 3), `oreburgh_city.json` three more, `valley_windworks_outside.json` a `drifloon` hidden by `FLAG_HIDE_VALLEY_WINDWORKS_OUTSIDE_DRIFLOON`, and the centre plan 01 · M6 opened more: Hearthome City a Pachirisu and a Buneary (hidden by `FLAG_HIDE_HEARTHOME_CITY_BUNEARY`), Solaceon Town two Buneary and a Pachirisu, Amity Square the Drifloon, Happiny, Pikachu and Clefairy of the people who walk them there. None is placed: plan 01 · M5 recorded "no field sprites for Pokémon but the surfer's" and assigned it to no session, and M6 left its Pokémon for plan 02; plan 02 · S5 and S6 need them. Route 209's Poké Kid Danielle wears the `pikachu` looks and is a trainer, placed as a person (`npcType` Lass). Amity Square's own walk is imported and unread: the walking Pokémon's object (`follower_mon`, looks `var_0`, hidden by `FLAG_HIDE_AMITY_SQUARE_FOLLOWER_MON`) and the gates' triggers on `VAR_FOLLOWER_MON_ACTIVE`; the receptionists say nothing walks beside the player yet, and the walk is plan 06 · R16's. `AreaObject.Movement`, `RangeX` and `RangeZ` are imported and unread (plan 02 gives people their movement in S4).
- **Animation**: `PokemonAnimation.Apply` layers idle, entry, physical, special, status, hit and faint over a `PokePose { Time, Blink, Attack, Kind, Hurt, Faint, Entry }`; there is no walk. Bones carry `PokeRole` (`Leg`, `Wing`, `Fin`, `Tail`, `Segment`…) and the model its `BodyPlan` and `Hovers`. An imported model's clips are matched by `ImportedModels.ClipRole`; no name means walking.
- **Scripts**: `cry "Shinx"` plays a species' cry with `CryMode.FieldEvent`; `wildbattle`, `place`, `face`, `walk`, `emote` exist (`Op` in `Story/Script.cs`). `FieldScripts.For(npc)` gives the common script of what someone is.
- **Wild Pokémon**: `Map.RollWildEncounter(x, y, steps, water, thick, lead)` rolls when a step lands, through `WildEncounterRules` (`Rate`, `Meet`, `Slot`, `Level`, `ScaredOff`), on the map's own `Dice` generator; `GameEngine.StartWildBattle(entry)` makes the Pokémon and `MeetWildPokemon(wild)` fights it (the harness finds `StartWildBattle` by name, so it may not gain an overload). Plan 02 · S2 added the rods (`Map.Fish` on each area's three rod tables, a cast played as `FishingAttempt`) and Sweet Scent (`Map.DrawOutWild`); plan 06 · R13 owes the time-of-day slots, Repels, swarms and the Poké Radar. `Habitats.Sinnoh` (`Data/Habitats.cs`) knows every area's morning, day, night, surf and rod species for the Pokédex's AREA page (`Habitats.Of(species)`), but not what lives at a tile.
- **Friendship and the party**: `FriendshipRules` (`Models/Friendship.cs`) is Platinum's table; `GameEngine.OnStep` applies `WalkCycle` every 128 steps and counts steps for `Evolution.StepsKey` on the lead. `docs/mechanics/rulings.md` gives Pawmo, Bramblin and Rellor "1000 steps at the head of the party" because "No Pokémon walk beside the player", and folds affection into friendship 220. `PartyScreen` opens a one-page summary (`ModernUi.DrawSummary`); the party's 3D models are seen only in battle, the title and the evolution scene (`EvolutionScreen.Render` draws them before `BeginTextureMode(virtualScreen)`).
- **A catch**: `BattleEngine.Show` on `Caught` sets `Ball`, registers the Pokédex and adds to the party; `Pokemon` carries `Personality`, `Friendship`, `Ball`, `IsShiny` (rolled, never shown) and `EvolutionProgress`, and `SavedPokemonData` the same. Plan 07 · O2 owes the original trainer and met data; plan 08 · P3 the six-page summary with `Markings` (Platinum's box markings, not marks).
- **Size**: a species has one size. `BattleCamera` frames a big foe with a wider lens, `PokemonRenderer.Draw` takes a root matrix, `CryVoice.Of` pitches a cry from height and weight.
- **Items**: `items.json` has 773 items and none of the curry or sandwich ingredients; plan 06 · decision 5 keeps those as collectables without a function, to be imported by R28.
- **Settings and rules**: `GameSettings` and `OptionRow` have twelve rows, none for the field; `Ruleset` (`Data/Ruleset.cs`) is `Platinum` or `Modern`, chosen at NEW GAME, handed to a battle through `BattleSetup.Rules`.

## Design

**Platinum's defaults don't move.** The stationary Pokémon and the species rider are Platinum's own places and sights. Everything else is off until chosen: the follower and visible wild Pokémon are rows of the options (`Follower`, `WildPokemon`), the camp's perks and the DexNav's hidden ability are lines behind `Ruleset.Modern`, marks give a title only when the player picks one, alphas live only in the post-game zone's tables, and cooking waits for the Hall of Fame. Nothing new rolls outside `Dice` or the battle's generator, nothing reads a clock but `FrameClock`, `FieldLife.Now` and `GameClock`, so two harness runs keep drawing the same pictures.

### Field sprites

- **`FieldSprites`** (`Graphics/FieldSprites.cs`, beside `PokemonSprites`): a species' or a form's sprites for the field, baked from its model like the menu sprites but at the characters' scale, `CharacterSprites.TexelsPerUnit`, so Machop stands shorter than the player and Steelix taller. Four facings (toward the camera, right, away, left), each with `IdleFrames` 2 and, from F2, `WalkFrames` 8. The card is square, 32, 48 or 64 texels by the model's height (`FieldSprites.ClassOf`, GPU-free: small under 0.7 units, big over 1.6), with the feet on the card's bottom row less the outline, so `Relief.At` places it as it places a person. Baked through `PokemonSprites.Render` with the field's yaws, outlined by `PixelCanvas.OutlinePass`, cached as `cache/sprites/<name>-field-<facing>-<signature>.png` with the same signature and `BakeVersion`, requested and serviced through the same queue (`PokemonSprites.Request`, `Service`), and replaced by a hand-drawn `overrides/sprites/pokemon/<NAME>/<facing>_<strip>_<frame>.png` as the characters' frames are. A sprite not ready yet is a Poké Ball card, as the menus show the stand-in.
- **The style guide first**: "Characters" gains a "Pokémon in the field" paragraph (the three card sizes, the two idle frames, the walk, that a follower's shadow and prints are the player's) before any of it is drawn.

### A Pokémon as a map object

- **`NPC.PokemonType`** (`"Pokemon"`, beside `ItemBallType`) with `NPC.Species` (a species or a form, as `Pokemon.ModelName` names them) and `IsPokemon`. A map file writes `"npcType": "Pokemon", "species": "Machop"` (`MapFile.ToMap` and `FromMap`, `docs/data-files.md`). `WorldMapBuilder.PlaceEvents` makes one of every area object whose looks name a species (`WorldMapBuilder.SpeciesFor(looks)`: `machop`, `drifloon`, the centre's `pachirisu`, `buneary`, `happiny`, `pikachu` and `clefairy`, and the later areas' `psyduck`, `uxie`, `giratina`, as the original names them; never a trainer, since Route 209's Poké Kid wears the `pikachu` looks), when the overlay's `people` names it, as for anyone else; `OverlayPerson` needs no new field. Hidden by the original's flag like everyone (`HiddenBy`, `Map.ApplyPresence`).
- **Drawn** by `WorldRenderer.GatherActors` beside the things (item balls and obstacles): a `CharacterSprites.Card` per facing and frame, `DrawCard` in the depth and colour passes, sunk by `SinkRows`, the idle frame by `FieldLife.Now`. Later than F1 a Pokémon of the map takes the movement plan 02 · S4 gives people: nothing of its own.
- **Spoken to**, it runs its own script or the common `Pokemon` (`common.txt`): `cry own` (the `cry` command gains `own`, the Pokémon's own species, as `setflag own` has), then `sayown`. The mine's Machop say their lines in our own words; the Drifloon's script (a wild battle with `wildbattle`) is plan 02 · S6's.
- **The rider**: the `surf` command (`GameEngine.FieldHost.Surf`, run by `common.UseSurf` and `common.Water` after `usemove` has named whose Surf it is, plan 02 · S2) notes that party Pokémon (`Player.Carrier`, its `ModelName`), and `GatherActors` draws that species' field sprite in place of `SurfMount`'s card, from behind or the side as the facing says, with its own `Seat` (the card's size class gives it). `SurfMount` stays as the stand-in while the sprite bakes, and as Platinum's look if decision 1 keeps it.

### The follower

- **Rules** (`Overworld/Follower.cs`, no drawing or input): the lead's last tiles are a trail the follower retraces one step behind, as `NpcWalk` replays a path. `Follower.Advance(dt, map, player)` takes a step when the player has moved on, at the player's pace, through `FieldMovement.Step` with a `Walker` of its own (so it hops a ledge after the player and stops where the player did), waits where the player turned without moving, and when its trail is cut (a warp, a door, a tile it can't take) it is put down on the player's tile and steps out behind as HeartGold's does. On a warp it arrives with the player; it is recalled (`Follower.Inside`) while surfing, cycling and in a dark cave, and comes back out with a puff. It is the party's first member that can stand (`Party.FirstUsable`), changed by a swap in the party screen or a faint, and the option off means no follower object at all.
- **Walk cycles** in `PokemonAnimation`: `PokePose.Walk` (0..1 phase) and `Moving` (blend) swing `Leg`, `Wing`, `Fin`, `Tail` and `Segment` bones by `BodyPlan` (a biped's stride, a quadruped's trot, a bird's hop, a serpent's and a fish's undulation, a slide for a `Hovers` model), the same clip in battle for a Pokémon that walks up to strike later. An imported model's `walk` or `run` clip is matched by name (`ClipRole.Walk`).
- **Drawn** as a Pokémon of the map is, with the walk strip and `WalkCycle`; its steps go through `FieldLife.Footstep` and `Landing` (prints in sand and snow, dust, leaves), by `TileBehaviors.KeepsFootprints`. Only one door moves at a time; the follower passes through the player's.
- **Talking to it**: `GameEngine.TryInteract` reaches the follower when it is on the tile ahead: it turns, shows an emote and says a line by its friendship (five bands of `FriendshipRules`, the lines our own), with its cry; now and then (a roll on `fieldRandom`, as Platinum rolls Pickup) it holds an item from a short table of our own. The option: `OptionRow.Follower` (on, off), `GameSettings.Follower`, off, as Platinum's feel is the default.
- **What it gives the rules**: nothing. Pawmo, Bramblin and Rellor keep counting steps at the head of the party (rulings.md's row), which the follower makes literal; its "Why" column changes to say so.

### Wild Pokémon in sight

- **`FieldPokemon`** per open `MapArea` (`Overworld/FieldPokemon.cs`, GPU-free): up to six in the chunks in view, spawned on tall grass and water from `MapArea.WildEncounters` and `WaterEncounters` through `WildEncounterRules.Slot` and `Level` with the lead's `WildLead`, on the map's own `Dice` generator; each wanders the tile grid at a walking pace on the behaviours its table allows, with `StepOffsetX`, `StepOffsetY` and `WalkCycle` as an `NPC` has them, and despawns out of sight as chunks are freed (`WorldRenderer.VisibleRects`). Touching one (a step onto it, or Confirm facing it) goes through `GameEngine.MeetWildPokemon` with the Pokémon already made, so the battle is the one the grass would have given: same slot, same level, same nature and gender rules.
- **The option** `OptionRow.WildPokemon`: `Hidden` (Platinum's, the default: the grass rolls as it always has) or `Visible` (the grass stops rolling, `EncounterSteps` is bypassed, and what is seen is what is met). `Hidden` draws nothing and changes no roll, so every existing shot stays.
- **A back strike**: under `Ruleset.Modern` only, a wild Pokémon met from behind loses its first turn (Legends: Arceus), a line in `BattleCore` where the first turn's order is decided, with `BattleConditions.Surprised`.

### The camp

- **`GameState.Camp`** and `UI/CampScreen.cs`: the party's six stand on a small stage (`BattleArenas.Build` with the arena under the player, `ArenaSpec.For`), drawn by `PokemonRenderer.Draw` in `CampScreen.Render` before `BeginTextureMode(virtualScreen)`, requested with `PokemonModels.Request` before the fade and awaited at its midpoint, trimmed after. The screen's state is methods that take no input (`MoveCursor`, `Confirm`, `Cancel`, `Pet`, `Feed`), its motion a function of time (`UiMotion`), so tests drive it. Three new `PokePose` fields (`Happy`, `Sleepy`, `LookAt`) with `EyeState` reactions give the models their faces; `ModernUi.Camp.cs` draws the hearts and fullness.
- **What it does to the rules**: petting and feeding raise friendship through `FriendshipRules.Apply` with two new `FriendshipEvent`s (`Petted`, `Fed`), and cure a status after a battle as Refresh does, both in `Models/Camp.cs` (GPU-free). Under `Ruleset.Modern`, affection's perks are lines of `BattleCore` where Generation 6 reads affection: a crit stage in `DamageCalculator.RollsCritical`, a dodge in `Hits`, a status shaken off in `EndOfTurn`, holding on at 1 HP where `EnduresHit` is asked, 1.2× EXP; `Ruleset.AffectionPerks` is false for Platinum, so no Platinum battle changes.
- **Cooking** (F8, if decision 4 keeps it): a `Kitchen` (`Models/Kitchen.cs`, GPU-free) whose stirring and timing are functions of time and three buttons; a recipe table `Data/recipes.json` written by the importer from a hand list in `tools/DataImporter/Overrides/recipes.json` (ingredients that plan 06 · R28 imports, plus Sinnoh's berries); a dish feeds the party (HP, status, friendship) and gives one timed **Meal Power** (`MealPowers` on `SaveData`, with an expiry on `GameClock`): Encounter Power for a type, read by `WildEncounterRules.Slot`; Catching Power, by `CatchCalculator.Shakes`; Sparkling Power, by the shiny roll in `Pokemon`'s constructor; Egg Power when plan 06 · R15 exists. None is in force unless the player cooked.

### Marks, alphas and the DexNav

- **Marks**: `Mark` (`Models/Marks.cs`, GPU-free) and `Pokemon.Marks`, `SavedPokemonData.Marks` (a migration step: older Pokémon have none), set where a catch becomes the player's (`BattleEngine.Show`'s `Caught`), copied by `CopyStateFrom`. A mark is a function of the catch's `Personality`, `GameClock.Now` and `Map.WeatherAt`, never a new draw: the time marks and the weather marks by their rules, a rare or a personality mark from the personality's bits at Sword and Shield's odds, a fishing mark for a Pokémon reeled in on a rod (plan 02 · S2's `FishingAttempt`). The summary shows them (a row of plan 08 · P3's memo page, or of the one page until then) with a `UiIcon.Mark` glyph on the party and PC cards; the player picks a title on the summary (`Pokemon.Title`), which the core puts in the send-out line ("Go! Starly the Sleepy!") through `Battler`'s name, so the line changes only when a title was chosen. Plan 06 · R17's ribbons share the title slot.
- **Alphas**: `WildEncounterEntry.Alpha` on a table row, never a roll of `Map.RollWildEncounter`: Platinum's tables have none, and the post-game zone's (plan 03 · D12) have them where that session puts them. An alpha's Pokémon has `Pokemon.Scale` (saved; 1 for everyone else), `WildEncounterRules.Level` plus ten, IVs no lower than twenty, an extra move (its next level-up move until tutor learnsets exist), and `Mark.Alpha`. `PokemonRenderer.Draw` scales the root, `BattleCamera` frames by the scaled height, the field sprite bakes at scale (its signature carries it), `CryVoice.Of` takes the scaled size so the cry drops by itself, and `PokePose.Fierce` holds the fierce eyes. With `WildPokemon` visible it looms in the grass; hidden, it is a rare slot.
- **The DexNav**: a Key Item (`DexNav` in `items.json`, through `Overrides/items.json`; USE switches it on) and a page of the Pokédex, `PokedexPage.Here`, listing the species of the area the player stands in by the hour (`Habitats.At(x, y)`, a new index of `Habitats` by cell, with `Habitats.GrassAt(GameClock.Now)`), seen and caught state, and a search level per species (`SaveData.DexNavLevels`). Switched on, a step now and then places a rustle (a `FieldLife` cell, `LifeArt.Rustle`, on a grass tile within sight) whose species the area's table gives; sneaking up (Run held walks at `Pace.Sneak`, a new pace of `FieldMovement`) keeps it; a leap or a run scares it off; reaching it starts the battle through `MeetWildPokemon` with the chain's bonus (`DexNavRules`, GPU-free: IVs by the search level, under `Ruleset.Modern` the hidden ability, egg moves when plan 03's egg learnsets exist). Switched off, nothing of it runs.

## Sessions

### F1 · Field sprites and the stationary Pokémon
- `FieldSprites` (bake, cache, override path, `ClassOf`, the Poké Ball stand-in), `NPC.PokemonType` and `Species`, `MapFile` and `docs/data-files.md`, `WorldMapBuilder.SpeciesFor` and the placing, the overlays of Oreburgh City, the mine's B2F, the Windworks, Hearthome City, Solaceon Town and Amity Square naming their Pokémon with our own lines, `common.Pokemon` with `cry own` (parser, runner, `EveryCommand`, `docs/scripts.md`), `Player.Carrier` and the species rider, the style guide's paragraph.
- Tests: `ClassOf` for Joltik, Machop and Steelix; every Pokémon looks of every open area names a species that exists; the Machop stand on open ground where they can be talked to (`WorldTests.PeopleStandOnOpenGroundWhereTheyCanBeTalkedTo` extended); `cry own` plays the speaker's species (`SoundTests` through `AudioManager.Listen`); a map file with a Pokémon round-trips.
- Shots: `story` mode talks to a Machop (`st35_machop_in_the_mine`, `st36_machop_speaks`) and looks at the Drifloon with its flag unset (`st37_drifloon`); `world`'s `w48_mine_b2f`, `w60_windworks` and the centre's shots with Pokémon in view (`wa4` to `wa9`) gain them; `life`'s `l06e_surfer_on_its_own_pokemon`; a `pokemon` board of the four facings for the species named (`97_field_<species>`).
- **Done when** the mine's three Machop stand and speak where Platinum's do, the surfer rides the Pokémon that knows Surf, and `diff` lists no shot but those.

### F2 · Walk cycles
- `PokePose.Walk` and `Moving`, the six plans' cycles in `PokemonAnimation`, `ClipRole.Walk` for imported models, the walk strip in `FieldSprites` (8 frames × 4 facings, baked on request), the style guide's "Clips" bullet.
- Tests: a walking biped's legs alternate and its root stays on the ground; a quadruped's front and hind legs oppose; a `Hovers` model keeps its height; a `Fish` undulates its segments in phase order; the clip is a pure function of its phase; every hand-built model's walk keeps every bone within its idle's reach.
- Shots: `92_clips_walk_<plan>` boards beside the other clips, `97_field_walk_<species>` strips.
- **Done when** every body plan walks in a board and the strip of Turtwig reads as a walk at 32 texels.

### F3 · The follower
- `Follower`, `OptionRow.Follower` and `GameSettings.Follower`, the trail, warps, doors, recalls, `GatherActors` drawing it with its prints and dust, `GameEngine.ApplySettings` and the harness's `Settings`.
- Tests (`FollowerTests`): the follower retraces the player's tiles one behind; hops the ledge the player hopped; never stands on a tile `FieldMovement.Step` refuses; is put down behind the player after a warp and a door; is inside while surfing and cycling; is the party's first standing member after a swap and a faint; the option off makes none.
- Shots: `life` mode with the option on walks Turtwig behind the player through sand, a door and a ledge (`l50_follower_walking`, `l51_follower_prints`, `l52_follower_door`, `l53_follower_ledge`, `l54_follower_surf_recalled`); `profile` with the follower on against off.
- **Done when** the lead follows through Twinleaf Town, the house and Route 201 without ever standing in a wall, and the option off leaves every existing shot unchanged.

### F4 · Talking to the follower
- The step in `TryInteract`, the five friendship lines, the emote and cry, the found items and their table, the rulings row's wording, `docs/mechanics/rulings.md`'s "Friendship".
- Tests: each friendship band gives its line; an item found goes to the bag and the roll comes from `fieldRandom`; nothing is said while a script runs.
- Shots: `st38_follower_turns`, `st39_follower_found_item`.
- **Done when** the follower answers in every band and plan 06 · R16 can build Amity Square's walk on it.

### F5 · Wild Pokémon in sight
- `FieldPokemon`, `OptionRow.WildPokemon`, the spawn and despawn with the chunks, the touch through `MeetWildPokemon`, `BattleConditions.Surprised` and the Modern back strike, the style guide's "Life" line for them.
- Tests: with `Hidden` no `FieldPokemon` exists and `RollWildEncounter` is called as before; with `Visible` the grass never rolls; a spawned Pokémon's species, level, nature and gender are the table's through `WildEncounterRules`; six at most in view; a touch fights the Pokémon seen; the back strike under Modern alone (`CoreScenario`).
- Shots: `l60_wild_in_the_grass`, `l61_wild_on_the_water`, `l62_wild_touched` (the flash), by day and at night; `profile` with eight in view.
- **Done when** Route 201's Starly and Bidoof are seen and met under `Visible`, and `Hidden` draws what it drew.

### F6 · The camp
- `GameState.Camp`, `CampScreen` and its painter, the stage, the three poses and faces, `Models/Camp.cs`, the two friendship events, the status cure, the START menu's entry (`StartMenuChoice.Camp`, after Pokémon), the style guide's "Menu screens" paragraph.
- Tests: petting raises friendship by Platinum's table and no more than once a visit; a status is cured once per battle; the cursor over six, three and one; `MenuScreenTests` with the new state.
- Shots: `menus` mode `27_camp`, `27b_camp_petting`, `27c_camp_feeding`, `27d_camp_six`; `profile`'s camp line against the double battle's (six models and a stage must fit the 8 ms).
- **Done when** the party stands in camp with faces that answer the cursor, and friendship rises as the table says.

### F7 · Affection's perks
- `Ruleset.AffectionPerks`, the five lines in `BattleCore` and `DamageCalculator`, `docs/mechanics/rulings.md`'s affection row rewritten, `coverage.md` by the importer.
- Tests (`CoreScenario`): each perk under Modern with the rolls fixed, and none under Platinum; `ARecordedBattleReplaysTheSame` with a perk; `RulesetTests` for the new field.
- **Done when** a Modern Pokémon at full affection shakes off a sleep and a Platinum one never does.

### F8 · Cooking
- `Kitchen`, `Overrides/recipes.json` and `Data/recipes.json`, `DexNav`-style USE for the pot at the camp, `MealPowers` in `SaveData`, the four readers, the Hall of Fame gate, `docs/data-files.md`, rulings.md's ruling 5 amended.
- Tests: a recipe's dish from its ingredients; a power expires on the clock; Encounter Power moves a type's slot odds and nothing else; no power in force means every roll as before; the gate holds before the Hall of Fame.
- Shots: `27e_camp_cooking`, `27f_camp_dish`.
- **Done when** a curry cooked after the Hall of Fame feeds the party and raises one type's odds for its half hour.

### F9 · Marks and titles
- `Mark`, `Pokemon.Marks`, `Title`, the save fields and migration, the setting at the catch, the summary row and the card glyph, the send-out line through the core, `ACopyTakesOnEverythingThatCanChange` with the new fields.
- Tests: a catch at 23:00 in rain carries Sleepy-Time and Rainy; the same personality gives the same rare mark on every run; a title chosen changes the send-out line and none chosen changes nothing; an older save's Pokémon has no marks.
- Shots: `20h_summary_marks`, `23d_battle_titled_send_out`.
- **Done when** a Pokémon caught in Route 202's rain at night shows both marks after a save and a load.

### F10 · Alphas
- `WildEncounterEntry.Alpha`, `Pokemon.Scale` saved, the level, IVs and move, the scaled draw, camera, sprite and cry, `PokePose.Fierce`, `Mark.Alpha`; the harness's `versus` takes `-alpha` after a species.
- Tests: an alpha row gives the scale, level and IVs; Platinum's tables have no alpha row (`DataFileTests`); the sprite signature carries the scale; `CryTests` for the lower pitch.
- Shots: `95_versus_turtwig_bidoof-alpha`, `97_field_bidoof-alpha` beside `97_field_bidoof`.
- **Done when** an alpha Bidoof towers over the player's Turtwig in battle and in the grass, and no Platinum area can meet one.

### F11 · What lives here
- The `DexNav` item, `PokedexPage.Here`, `Habitats.At`, `SaveData.DexNavLevels`, the page's painter in `ModernUi.Pokedex.cs`.
- Tests: `Habitats.At` for a tile of Route 201 by hour agrees with `Habitats.Of` the other way round; seen and caught marks; a search level saved.
- Shots: `26n_pokedex_here_morning`, `26o_pokedex_here_night`.
- **Done when** the page on Route 201 lists Starly, Bidoof and Kricketot with their state.

### F12 · Rustles and chains
- The rustle through `FieldLife` and `LifeArt`, `Pace.Sneak`, the scare rules, `DexNavRules` and the bonus in `MeetWildPokemon`, the chain in `SaveData`.
- Tests: a rustle lands only on the area's grass in sight; a run scares it; a sneak reaches it; the bonus IVs by level, the hidden ability under Modern alone; switched off, no rustle and no sneak pace.
- Shots: `l70_rustle`, `l71_sneaking_up`, `l72_rustle_scared`.
- **Done when** a chain of five on Route 202 meets a Shinx with the chain's IVs, and the DexNav off plays Platinum's grass.

## Risks

- **A thousand species bake on demand**: a follower changed in the party asks for forty frames that take a second on the main thread. F1 bakes a facing a frame inside `Service`'s budget and shows the Poké Ball card meanwhile, as the menus show the stand-in; the bake is cached for good.
- **Generated models walk oddly**: the generator's sculpts rarely read as the species (plan 03 · D5), and a walk shows it more than a stand. Hand-drawn overrides and the hand-built batches are the answer, as for the menus.
- **Shiny and gender are not in the sprite's key**: `PokemonModels.Signature` has no shiny part, so a shiny follower looks like its kind until shiny palettes exist, which no plan owns yet.
- **The follower touches every field system**: doors, warps, ledges, surfing, scripts that walk the player (`PlayerWalk`), the camera. F3 keeps every rule in `Follower.Advance` and the option off by default, so a fault can be switched away.
- **Visible wild Pokémon change the harness's rolls** when on: the field modes are shot with the option off, and F5's shots carry their own prefix.
- **Six models in camp** are more than any battle draws at once: F6 measures with `profile` before it is called done and drops the stage's lights if the frame goes over.
- **Cooking reverses a recorded decision** (plan 06 · decision 5, rulings.md's ruling 5): F8 is not started without decision 4 below, and R28 must have imported the ingredients first.
- **The DexNav's egg moves and hidden abilities** wait for plan 03's egg learnsets and the Modern preset; F12 ships IVs alone under Platinum.

## Needs and gives

- **Needs** plan 02 · S4 (people's movement, which the Pokémon of the map take), S5 and S6 (the Machop's and the Drifloon's scenes), S11 to S13 and S15 (the legendaries' places, for their objects); plan 01 · M7 and M8 (the areas those objects stand in; M6 is done, and with it the centre's Pokémon objects and Amity Square); plan 02 · S2 (done: the rods, for the fishing mark); plan 06 · R13 (the time-of-day slots F5 and F11 read), R15 (Egg Power, egg moves), R28 (the ingredients F8 cooks); plan 07 · O2 or plan 08 · P3 (the memo page F9's row goes on; F9 adds `Marks` beside whatever ran first); plan 03 · D12 (the post-game zone whose tables carry alphas); plan 05 · A4 (the cries, done).
- **Gives** plan 02 every stationary Pokémon as a map object with a cry and a script (F1); plan 06 · R16 the walker Amity Square's walk is built on (F3, F4; the square is open, its gates' triggers and the walking Pokémon's object imported and waiting), R17 the title slot (F9), R13 a visible form for swarms and the Poké Radar's shaking grass if it wants one (F5, F12); plan 03 · D12 alphas as a post-game draw (F10); plan 08 · P3 a marks row (F9); plan 07 the save fields of F9 and F10 to validate on its server.

## Decisions for the user

1. **Whose back the surfer rides.** *Recommended:* the party Pokémon whose Surf was used, as HeartGold does; `SurfMount` stays as the stand-in. The alternative keeps Platinum's one swimmer for all, which costs nothing and changes no picture.
2. **Which species get field sprites.** *Recommended:* all of them, baked on demand from whatever model there is, as the menus do. The alternative is the hand-built species only, with a Poké Ball card for the rest, which spares the generated walks.
3. **Where the follower and visible wild Pokémon are switched on.** *Recommended:* two rows of the options, off, changeable at any time like Battle Style. The alternative ties both to the Modern preset at NEW GAME, which keeps the options short but mixes the field's feel with the battles' rules.
4. **Cooking.** *Recommended:* in, after the Hall of Fame, as the camp's one activity, with curry alone (sandwiches add a second kitchen for the same powers), which amends plan 06 · decision 5 for the ingredients used. The alternative leaves cooking out and the camp feeds berries and R14's Poffins.
5. **Alphas in Platinum's areas.** *Recommended:* none; the post-game zone's tables only. The alternative is a rare slot in every area under the Modern preset, which moves Modern's encounter odds a little.
6. **The DexNav's home.** *Recommended:* a Key Item that switches it on and a page of the Pokédex for the list. The alternative is a Pokétch app, beside the three plan 02 · S2's Pokétch runs (`Poketch.Runs`), under plan 06 · R14 with the other apps.

## Status

- [ ] F1 Field sprites and the stationary Pokémon
- [ ] F2 Walk cycles
- [ ] F3 The follower
- [ ] F4 Talking to the follower
- [ ] F5 Wild Pokémon in sight
- [ ] F6 The camp
- [ ] F7 Affection's perks
- [ ] F8 Cooking (decision 4)
- [ ] F9 Marks and titles
- [ ] F10 Alphas
- [ ] F11 What lives here
- [ ] F12 Rustles and chains
