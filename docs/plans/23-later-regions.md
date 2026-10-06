# Plan 23 · The later regions: Kalos, Alola, Galar, Paldea and Hisui

Written 2026-10-06, before any session.

**Goal**: the regions of the 3DS and Switch games on this game's tile grid, after Unova: Kalos with its free movement reduced to the four-way grid and Mega Evolution's home, Alola with trials instead of gyms and Ride Pokémon instead of HMs, Galar with the Wild Area and the gym tournament, Paldea as an open world with the ride and Terastallization's home, and Hisui as Sinnoh's past, a side region off the chain; each on plan 18's framework and its decision on where a region without a decompilation comes from, each section starting with the decisions its region forces on the engine, each short and honest about what isn't known, because these are the furthest off.

## Where we are

- **The chain** (`Data/RegionDatabase.cs`): `Kalos`, `Alola`, `Galar` and `Paldea` are `Region`s with `Generation` 6 to 9 and no `Start` or `Maps`; the `links` from Sinnoh on are `Transport.Undecided`; there is no Hisui. `RegionTests.TestEachRegionLinksOnlyToTheNextGenerationAndTheLastToNothing` holds every link's `To` to the next generation and `Links.Count` to `All.Count - 1`, so a branch fails it. Plan 18 · W1 gives a region its file (`Data/regions/<id>.json`: professor, starters, badges, the `Gates` its field moves ask, the `Spawns` Fly and Teleport go to, dex), W2 badges per region, W3 the regional Pokédexes from PokeAPI (Hisui's 242 among them), W4 and W5 the crossing (a fresh journey: the party into the boxes, a starter from the new professor), and its decision 5 recommends a reader of the user's own dump, hash pinned, for Generation 5 on, naming Galar and Paldea's missing tile grid as this plan's question; its crossing table leaves the later links' airports to this plan.
- **Walking** is `FieldMovement.Step` over four `Direction`s (`Down, Up, Left, Right`), `TravelMode { OnFoot, Surfing, Cycling }`, `Pace`, `Walker(Mode, Height, Running, FastGear, Moves, Climbing)` and `FieldMoves { Surf, Waterfall, RockClimb, Flash }`. `MovesOf(party)` is the one source of those flags and `GameEngine` sets `player.Moves = FieldMovement.MovesOf(playerParty)`; water is `Obstacle.Water` for anyone not `Surfing`, a step of `StepLimit` (1.25) or more is `Obstacle.Cliff`. `CharacterSprites` bakes four facings (style guide, "Characters"). Plan 02 · S2 added the badge gates (`FieldMoveRules.BadgeFor`, Platinum's badges, asked by the party menu, and by the obstacles' common scripts with `if badge` after `if knows`), the obstacles as things of the map (`NPC.Obstacle`, hidden by local flags), Strength, Flash and Defog as story flags (`FieldMoveRules.StrengthFlag`, `FlashFlag`, `DefogFlag`), Fly (`UI/FlyScreen.cs`) and the Bicycle as an item (`Overworld/Bicycle.cs`, its gears on the run button).
- **Pokémon in the field**: none today. `GameEngine.MeetWildPokemon(Pokemon, BattleKind, cannotFlee)` (private) starts a wild battle from a Pokémon already made; plan 10 · F1 gives species their field sprites and F5 `FieldPokemon`, wandering and touched, behind `OptionRow.WildPokemon`, off by default.
- **Camera and weather**: `FieldCamera { Default, Cave, ZoomedIn, CoronetSouth }` per area (`Map.Camera`, `WorldMapBuilder.CameraOf`, `MapScene.ViewOf`; plan 01 · M6 added the fourth, for the outside of Mt. Coronet's south face), fixed pitches the shader's `SetUpright` depends on; plan 09 · L4's `CameraRig` zooms the lens only. Weather is one `FieldWeather` per area from its header (`MapArea.Weather`, `Map.WeatherAt`); `GameClock` has no date; plan 06 · R14 owns the daily clock.
- **The later mechanics**: plan 06 · R20 Mega Evolution, R21 Z-Moves, R22 Dynamax (Max Raid dens optional), R23 Terastallization (Tera Raids optional), R28 every item (`items.json` already holds the Beast, Feather, Wing, Jet, Leaden, Gigaton, Origin and Strange Balls as data; `Formulas.BallTenths` knows ten of Platinum's), R29 the later forms. Plan 06 · decision 2 brings each into Sinnoh's story if balanced, else after the Hall of Fame.
- **Forms**: `FormKind.Regional`; `PokemonModels.Regional.cs` hand-builds 54 regional forms (all 18 Alolan, all 20 Galarian, 12 of the 16 Hisuian, the 4 Paldean); the four left, Hisuian Sliggoo, Goodra, Avalugg and Decidueye, are forms of generated species and are generated from their own data. `EvolutionContext.Region` keeps an evolution to its region (`species.json` names Alola three times, Galar twice, Hisui seven), and `docs/mechanics/rulings.md` ("Evolution") records that none of those regions is built, so they are the species' own forms until one is.
- **Formats and pastimes already planned elsewhere**: plan 14 · B2 sky battles, B3 and B4 hordes, B5 SOS calls (`callsForHelp` in an area's overlay, "plan 23's Alola"), B8 the Dome's `Bracket`, B13 the Battle Royal (optional, "when plan 23's Alola is near"); plan 10 · F6 to F8 the camp, affection and curry (`Kitchen`, `Data/recipes.json`; its decision 4 names sandwiches as a second kitchen); plan 11 · C9 and C10 the player's `Outfit` and the `WardrobeScreen`; plan 15 · E1 and E2 the research record and the TASKS page, Legends: Arceus's own; plan 12 · Q12 the AUTO button; plan 13 · V3 the level cap by badge, Sinnoh's table only.
- **Headless battles and catching**: `BattleCore` plays with no screen (`CoreSetup.PlayerController` and `EnemyController`, `TrainerAi.Instance`), `CatchCalculator.Shakes(wild, ball, rng, turn, conditions)` and `BattleConditions` (`Terrain`, `Night`, `Weather`, `TrickRoom`) are GPU-free; `MoveData.Modern` (`MoveValues`: power, accuracy, PP, priority, type) is the one second set of a move's values, swapped in by `Ruleset.Use`.
- **World files**: `WorldChunkFile` (`Behaviours`, `Solid`, `Cover`, `Heights` as `HeightPlate`s, `Props`), `WorldAreaFile` (`Land`, `Water`, the three rods' tables since plan 02 · S2, `Warps`, `Objects`, `Signs`, `Triggers`, `Bike`, `Running`, `Fly`), `WorldMapEntry.Setting` (`MapSetting { Outdoors, Cave }`); `Relief` welds steps under `WeldLimit` (0.75) and faces the rest. `tools/MapImporter` reads Platinum alone; plan 18 · W6 and W7 add the GBA reader beside `DecompMaps`, the pattern a dump reader follows.
- **Species**: plan 03's hand-built batches run in National order and stand at the end of Unova (`PokemonModels.Unova4.cs`, Genesect: 642 species hand-built, every one of Kanto, Johto, Hoenn and Unova among them); every species of Kalos onward is generated (`PokemonGenomes.For`, `PokemonGenerator`), which plan 03 · D5's risks call generic.

## Design

**One grid, four ways.** Every later region is a tile grid walked by `FieldMovement.Step`'s four directions. The free movement of X and Y and of the Switch games is not rebuilt: eight directions would leave `player_move.c`'s rules, which `FieldMovement` follows, and every sprite has four facings. What a region's movement adds is a `TravelMode` or a `FieldMoves` flag with a rule in `FieldMovement`, GPU-free and tested by walking a map built in the test; `Player.Advance` plays it out as it plays out surfing. A curved street is an octagon of stepped streets on the grid, and the user judges the first town of each region before the rest is laid (plan 01 · M4's decision 8: no picture of the original is kept).

**Field powers from two sources.** `FieldMoves` grows the later games' powers (`Cut`, `RockSmash` and `Strength`, which plan 02 · S2 asks in the obstacles' common scripts, `CutTree`, `Rock` and `Boulder`, with `if knows` and not through `FieldMoves`, so those scripts ask the region's grant beside the move; `Fly`, which opens S2's `FlyScreen` with no Pokémon of the party knowing it; then `Charge` for breakable rocks, `Climb` for any cliff, `Glide`, `Float` for a bike over water, `Jump`). `StoryState.Grants(region)` is a `FieldMoves` value per region (`SaveData.FieldGrants`, a dictionary by region id, empty in older saves) that scripts set with a new `grant <power>` command (`Op`, parser, runner, both hosts, `ScriptTests.EveryCommand`, `docs/scripts.md`), and `GameEngine` sets `player.Moves = MovesOf(playerParty) | story.Grants(Here)`. The badge gates of plan 02 · S2 (`FieldMoveRules.BadgeFor`, the scripts' `if badge`) apply to the party's moves alone. Sinnoh grants nothing, so `MovesOf` keeps giving what it gives.

**Where a region's layout comes from.** Plan 18 · decision 5 (a): `tools/MapImporter/Dump/`, a reader of the user's own dump found by `--dump` and pinned by `Sources.DumpHash`, which plan 22 · U1 starts for the DS container (`DumpFiles.cs`, `Gen5Maps.cs`); the 3DS and Switch readers are two more files of that folder, sharing `WorldWriter`, and nothing of a dump is in the repository but the world files. Kalos and Alola's maps are 3D models over a collision grid, so the reader samples the walkable and the height at one tile per unit of that grid, writes `HeightPlate`s a tile wide (a step over `WeldLimit` is a face, under it a slope) and leaves `Cover` to a table per zone written by hand in the region's session, as plan 18 · W6's `GbaCover` does. Galar and Paldea have no grid: decision 1 samples their collision at a tile per two metres of the original's world, places the landmarks where they stand, and names what is lost (a slope becomes steps, a bending road a staircase, a cliff path a `Climb`). Nothing of this is known to work: the containers are documented by the community and not by a decompilation, and the fallback at every step is (b), hand-authored layouts in the region's shape with plan 16 · T9's editor, which plan 01 · decision 1 keeps for fixes alone and this plan would widen. Either way the rule holds: layouts and numbers, never a tile's graphics.

**A region's rules live in its file.** Plan 18 · W1's `Region` gains `Challenge` (`badges`, `trials` or `stamps`: what the card shows and what plan 13 · V3's cap counts; not W1's `Gates`, the badge each field move asks for), `Dex` as a list (Kalos has three), `Arrival` (the airport or station of plan 18's plane) and `Powers` (the grants its story hands out, in order). `GameEngine.Here` is who every rule here asks; a game that stays in Sinnoh never meets one, and Sinnoh's shots stay what they are.

**The later mechanics are at home.** Each region's story hands out plan 06's mechanic where the original does (the Key Stone at the Tower of Mastery, the Z-Ring at the first trial, the Dynamax Band from the Champion, the Tera Orb from the academy): a `give` in a script, with the mechanic's rules R20 to R23's, and the one-of-each rule of plan 06 · decision 3 unchanged. Where a region's own venue was optional in plan 06 (the dens, the raid crystals), this plan builds it here as that session's optional part.

**Wild Pokémon seen, by the area's own rule.** `MapArea.WildVisible` (from the area's header through the overlay) makes plan 10 · F5's `FieldPokemon` the area's way whatever `OptionRow.WildPokemon` says, because the Wild Area, Paldea and Hisui are played that way; Sinnoh's grass follows the option as F5 leaves it. A touch meets the Pokémon seen through `MeetWildPokemon`.

**Art and sound are ours.** A town's look is an `Architecture` of its own (`BuildingArt.Styles.cs`), a stadium a `BuildingKind`, a Noble's arena a `BattleArena`, each a row of the style guide's "Buildings", "Arenas (G8)" or "Areas (targets)" before the code; music is `Data/music/<region>/` by `MusicDirector.Resolve`'s roles (plan 05 · A6's way); every line is ours on the original's beats. Field Pokémon, weather and a stadium's crowd cost the frame: each session with one measures it with `profile` on High against 8 ms, and `Dice` and `FrameClock` keep two runs the same.

### Kalos
Decisions: four directions (the skates are `TravelMode.Skating`: `Pace.Fast`, a slide of one tile on stopping like ice's `Carries`, refused on stairs and grass, taken with a key as the Bicycle's gear is); Lumiose's ring as a grid octagon with its plazas where they stand; the Central, Coastal and Mountain dexes as three listings of plan 18 · W3's `Pokedex.Dexes`; Mega Evolution given at Shalour as plan 06 · R20 builds it; sky battles and hordes placed from plan 14 · B2 to B4; the boutiques as plan 11 · C10's `WardrobeScreen` with Lumiose's stock; Pokémon-Amie is the camp (plan 10 · F6, F7; rulings.md folds affection into friendship); the Friend Safari is optional, its species drawn from plan 07 · O7's friend list, or from the player's own trainer id without it. Kalos's 72 species are generated until plan 03's batches reach them.

### Alola
Decisions: four islands as four matrices of one world folder, joined by a ferry that is a warp with a scene (plan 18 · W5's `common.Crossing` within a region); trials replace gyms (`Region.Challenge = "trials"`: seven trials and four grand trials as story flags, the card showing the four stamps, the cap counting stamps); the Ride Pager is `Grants` (Tauros `Charge`, Lapras and Sharpedo `Surf`, Machamp `Strength`, Mudsdale `Climb`, Charizard `Fly`); totems are a trainer-like battle with `BattleConditions.TotemAura` (stat stages on entry) and `callsForHelp` set in their areas (plan 14 · B5); Z-Moves given at the first trial (R21); the three region-bound evolutions come alive; Ultra Beasts and Ultra Space are the post-game; the Battle Royal is plan 14 · B13 placed; the Rotom Dex, Poké Pelago and the Festival Plaza are left out.

### Galar
Decisions: the Wild Area is one area of the overworld with `WildVisible`, `FieldCamera.Wide` (a fifth preset: the lens of `ZoomedIn` the other way, never a rotating camera) and weather by day from a `WeatherCalendar` (`Overworld/WeatherCalendar.cs`, GPU-free: an area's weathers by the day of year, read from plan 06 · R14's date, `Fixed` for the harness); the gym challenge is eight stadiums (`BuildingKind.Stadium`, `BattleArena.Stadium` with a crowd that is a painted ring, its mission before each leader a script) and the Champion Cup is plan 14 · B8's `Bracket` with Galar's entrants; Dynamax given with the band (R22) and working anywhere as plan 06 · decision 4 rules, Power Spots or not, and the dens built here as R22's optional part (a `Raid` is a `BattleFormat` of four places against one, the three allies `TrainerAi`'s); the Rotom Bike's water upgrade is the `Float` grant; the Stone Arch stands in the Wild Area's `EvolutionSites` (rulings.md's Runerigus line); camping and curry are plan 10 · F6 to F8; the Isle of Armor and the Crown Tundra are optional.

### Paldea
Decisions: one continuous overworld on plan 01 · M2's streaming, its heights from the sampled terrain and not from plates the original has; Koraidon or Miraidon is `TravelMode.Ride` (`Pace.Fastest` on a dash; `Jump`, `Glide` from a cliff's edge to the ground below as a `StepKind.Glide` over the face, `Climb` on any `Cliff`, `Surf` as `Float`: each a grant the story gives in the original's order); the three paths are three scripts gated by nothing but each other's flags, the leaders' levels fixed as the original fixes them and the academy saying the order it suggests; Terastallization given with the orb (R23) and the Tera Raids built here as R23's optional part on the same `Raid` format; picnics are plan 10's camp with sandwiches as a second section of `recipes.json` on the same `Kitchen` (decision 7); Let's Go is a battle `BattleCore` plays headless in the field (`Models/LetsGo.cs`: the lead sent at a `FieldPokemon`, both sides `TrainerAi`, the log read for EXP and never shown); Area Zero ends the story; Kitakami and Blueberry are optional.

### Hisui
Decisions: a side region reached from Sinnoh after its Hall of Fame (decision 3: the Celestic Town ruins, a scene of plan 02's post-game), so `RegionLink` gains `Kind { Next, Side }`, `LinkFrom` keeps giving the chain's link, `SideLinksFrom` the branches, and the link test counts chain links alone; Jubilife Village and five open areas as matrices of their own with `WildVisible`; catching from the field without a battle (`Overworld/FieldCatch.cs`, GPU-free: a throw at a `FieldPokemon`, the shakes from `CatchCalculator.Shakes` with the turn at 0 and Hisui's balls given rows in `Formulas.BallTenths`, a back strike's bonus, a Pokémon that flees or turns to fight through `MeetWildPokemon`); strong and agile styles as a choice on a move (`BattleChoice.Style`, decision 4: Strong a quarter more power at priority −1, Agile three quarters at +1, each a PP more, refused by `WhyNot` outside Hisui and before the move is mastered, Platinum's action order kept); the research tasks of plan 15 · E1 and E2 gating the story by a rank summed from `Research.LevelOf`; Nobles as a field fight (`Overworld/NobleFight.cs`: the noble's charges along the arena's lanes as a function of time, balms thrown as `FieldCatch` throws, a stun opening a battle that can't catch); no gyms, no badges (`Challenge = "none"`).

## Sessions

Each session: the rule in a GPU-free class with its tests, the areas opened through the region's `world.json` with their overlays and scripts (`StoryTests` and `WorldWalk` as plan 02 and plan 01 use them), shots in `world`, `story` and `menus` with a prefix per region, and a `profile` line where something new is on screen.

### Kalos

### Z1 · The 3DS reader and Vaniville to Santalune
- `tools/MapImporter/Dump/ThreeDs.cs` beside plan 22 · U1's `DumpFiles.cs`: the collision grid and heights to `WorldChunkFile`, zones to `WorldAreaFile`, the hash pinned as U1 pins Unova's; `Data/world/kalos/world.json`; `regions/kalos.json` (Sycamore, the Kanto starters as its second gift, the eight badges, three dexes); `TravelMode.Skating` in `FieldMovement` and `Player`; Vaniville, Aquacorde, Route 2 and Santalune with its gym.
- Tests: `MapImportTests` builds a sampled grid byte by byte (a cliff's face, a slope, a bridge); the skates' pace, slide and refusals on a test map; `RegionTests` for Kalos's file. Shots: `wk01` to `wk06`, `lab`'s skating shot.
- **Done when** Santalune's gym is reached on skates from Vaniville and the user calls the first town recognisable.

### Z2 · Lumiose and central Kalos
- Lumiose's octagon with its five plazas, the Prism Tower, the boutique (plan 11 · C10's screen with its stock), the cafés as rooms; Routes 4 to 7, Camphrier, Cyllage and its gym; `Architecture.Lumiose` and `Cyllage`; the Kalos music roles.
- Tests: `WorldWalk` through every plaza and gate; the boutique sells and the outfit saves. Shots: `wk10` to `wk18`, `menus` `28e_boutique_lumiose`; `profile`'s Lumiose line.
- **Done when** the city is walked round its ring in both directions and `profile` holds 8 ms on its busiest plaza.

### Z3 · Shalour, the coast and the Tower of Mastery
- Routes 8 to 12, Ambrette, Geosenge, Shalour and the Tower (the Key Stone given, plan 06 · R20's rules), Coumarine; sky trainers and hordes placed from their tables (plan 14 · B2 to B4); the Friend Safari (optional).
- Tests: the Tower's script gives the stone once; a sky trainer refuses a grounded party. Shots: `wk20` to `wk28`, a Mega in a Kalos battle.
- **Done when** a Lucario Mega Evolves in a harness battle after the Tower's scene.

### Z4 · The mountain, Team Flare and the League
- Lumiose's second half, Laverre, Dendemille, Anistar, Couriway, Snowbelle, the Flare labs, the Pokémon Village, Victory Road and the League; `KalosHallOfFame`; the three dexes' diplomas; the link to Alola (plane, Lumiose's airport) confirmed in `RegionDatabase`.
- Tests: every chapter's script to its end headless; the Hall of Fame opens the crossing. Shots: `wk30` to `wk40`, `st40_kalos_crossing`.
- **Done when** a Kalos game is played from Vaniville to the Hall of Fame by the playthrough tests and the attendant takes the player on.

### Alola

### Z5 · The four islands, the Ride Pager and the ferry
- `Data/world/alola/` with four matrices and Aether Paradise; `regions/alola.json` (Kukui, Rowlet, Litten and Popplio, `Challenge = "trials"`, the stamps, the `Powers` list); `StoryState.Grants`, `SaveData.FieldGrants`, the `grant` command, `FieldMoves.Charge` and `Climb`, with `Strength` as plan 02 · S2 leaves it (a story flag, `FieldMoveRules.StrengthFlag`, that `common.Boulder` sets and `KeepFieldMovesInForce` turns into `Player.PushesBoulders`) and the obstacles' scripts asking the grant; the ferry's scene; Melemele's towns and Routes 1 to 3.
- Tests: a grant opens a rock that no party move opens; a save without `FieldGrants` loads empty; the ferry warps between matrices in `WorldWalk`. Shots: `wa01` to `wa08`, `lab` with a Tauros charge.
- **Done when** the player rides Tauros through a rock on Route 1 with no Pokémon that knows Rock Smash.

### Z6 · Melemele and Akala: trials, totems and Z-Moves
- The trial scripts of Melemele and Akala, `BattleConditions.TotemAura`, `callsForHelp` in the totems' areas (plan 14 · B5), the Z-Ring at the first trial (plan 06 · R21), the grand trials and their stamps on the card (plan 18 · W2's region row), the cap by stamps (plan 13 · V3's table for Alola).
- Tests: a totem enters with its stages and calls an ally; a trial's flag sets its stamp; a Z-Move refused before the ring. Shots: `wa10` to `wa20`, `ft22_totem_aura`, `28f_card_alola_stamps`.
- **Done when** the Totem Gumshoos calls for help and the Normalium Z is used in the harness's battle after it.

### Z7 · Ula'ula, Poni and the Aether Foundation
- Ula'ula and Poni with their trials, Aether Paradise, Team Skull's town, the Altar, the League on Mount Lanakila, `AlolaHallOfFame`; the region-bound evolutions in `EvolutionContext.Region` (Raichu, Exeggutor, Marowak) alive; the Ultra Beasts placed as plan 03 · D12 asks; `rulings.md`'s Alolan line amended.
- Tests: a Pikachu evolves into its Alolan form in Alola and its Kanto form in Sinnoh; every chapter headless. Shots: `wa30` to `wa44`.
- **Done when** an Alola game runs from Iki Town to the Hall of Fame and a Kanto Pikachu brought over becomes an Alolan Raichu.

### Z8 · After the League: Ultra Space and the Battle Royal (optional)
- Ultra Space's worlds as cave-set matrices, the Ultra Beast hunt, the Battle Tree's door, the Battle Royal from plan 14 · B13 in Royal Avenue; the link to Galar (plane, Hau'oli's airport).
- Tests: the hunt's script; a Royal in the harness. Shots: `wa50` to `wa56`.
- **Done when** every Ultra Beast can be caught and the Royal plays.

### Galar

### Z9 · Galar's shape and Postwick to Motostoke
- Decision 1 carried out: `tools/MapImporter/Dump/Switch.cs` sampling at a tile per two metres, the report of what each place lost, the first areas (Postwick, Wedgehurst, Route 1 and 2, the first Wild Area strip, Motostoke); `regions/galar.json` (Magnolia and Leon, Grookey, Scorbunny and Sobble, eight badges); the Rotom Bike as the Bicycle item with `Float` as a later grant; `Architecture.Galar` for the stone-and-brick towns.
- Tests: the sampled grid's faces and slopes; the bike floats only with the grant. Shots: `wg01` to `wg08`.
- **Done when** the user judges Motostoke recognisable on the grid and `WorldWalk` reaches it from Postwick.

### Z10 · The Wild Area: field Pokémon, weather by day and the dens
- `MapArea.WildVisible`, `FieldCamera.Wide` (`MapScene.ViewOf`'s fifth row, the style guide's "Overworld" camera line first), `Overworld/WeatherCalendar.cs` on plan 06 · R14's date with `Fixed` for the harness, the Wild Area's two halves open, the dens as plan 06 · R22's optional part (`BattleFormat.Raid`, `Models/Raids.cs`: the den's species by its beam, four places against one Dynamaxed foe, the allies `TrainerAi`'s, the catch at the end), the Stone Arch in `EvolutionSites`.
- Tests: `WildVisible` spawns with the option off and Sinnoh's grass still rolls; the calendar's weather for a day is the same twice; a raid fills four places, ends on the catch, replays; a Galarian Yamask evolves under the arch. Shots: `wg10` to `wg18` by weather, `ft50_raid_four`, `ft51_raid_catch`; `profile` with eight field Pokémon and rain.
- **Done when** the Wild Area shows its Pokémon under the day's weather within 8 ms and a raid is won and caught in the harness.

### Z11 · The gym challenge: stadiums, badges and the bike on water
- `BuildingKind.Stadium` and `BattleArena.Stadium` (the painted crowd ring, `ArtLook.ArenaRig`'s stadium light; "Arenas (G8)" first), the eight missions as scripts, the leaders with Dynamax (R22's AI use), Turffield to Hammerlocke's first visit, the bike's water upgrade (`Float`) on Route 9; `Galar`'s music roles.
- Tests: a mission script to its end; the bike crosses water with the grant and bumps without. Shots: `wg20` to `wg32`, `a_stadium`.
- **Done when** four badges are won in stadiums in the harness and the bike rides over Route 9's water.

### Z12 · The Champion Cup, Eternatus and the expansions (optional)
- Circhester to Wyndon, the Cup on plan 14 · B8's `Bracket`, the Darkest Day, Eternatus (a raid that can't fail), Leon, `GalarHallOfFame`; the link to Paldea (plane, Wyndon's station is the original's; decision left to the session); the Isle of Armor and the Crown Tundra as two more open areas with Dynamax Adventures on `Raid` (optional).
- Tests: the bracket plays headless; every chapter to its end. Shots: `wg40` to `wg52`.
- **Done when** a Galar game runs from Postwick to the Hall of Fame.

### Paldea

### Z13 · Paldea's shape, the academy and the ride
- The Switch reader's terrain sampled to `HeightPlate`s a tile wide with slopes from their neighbours; `Data/world/paldea/` as one matrix, its size reported before it is opened; Cabo Poco, Los Platos, Mesagoza and the academy; `regions/paldea.json` (Sada or Turo's school, Sprigatito, Fuecoco and Quaxly, eight badges); `TravelMode.Ride` with the dash in `PaceOn`; `Architecture.Paldea`.
- Tests: a sampled slope welds under 0.75 and faces over it; the ride's dash pace. Shots: `wp01` to `wp08`, `lab` riding.
- **Done when** Mesagoza is reached on the ride and the user judges it, and `world`'s timed walk across the first province shows no chunk waited for.

### Z14 · The ride's powers and the three paths
- `Jump` (a hop over one tile of anything at the same height), `Glide` (`StepKind.Glide` down a face to the first ground), `Climb` and `Float` as grants in the original's order; the three paths as scripts gated by flags alone (the gyms with their tests, the Titans with the grants they give, the Team Star bases with the Let's Go of Z15 placed after it); the Tera Orb from the academy (R23); the leaders at the original's fixed levels and the academy's suggested order.
- Tests: each power's rule on a test map; the three paths in any order headless; the cap table for Paldea (plan 13 · V3). Shots: `wp10` to `wp30`, `lab`'s glide and climb.
- **Done when** a Titan's grant lets the ride climb a face it bumped against before, and the three paths' first stops are reached in six orders.

### Z15 · Picnics, Let's Go and the Tera Raids
- `recipes.json`'s `sandwiches` section on plan 10 · F8's `Kitchen` (the camp's screen as a picnic), `Models/LetsGo.cs` and its key in the field (a flash, the Pokémon fading, EXP by the log), the raid crystals as plan 06 · R23's optional part on `Raid` (a Tera foe, the shield at half HP as a rule), Team Star's bases as Let's Go against a clock.
- Tests: a sandwich gives the same powers as a curry's table says; a Let's Go battle replays from its record and gives its EXP; a Tera raid ends on the catch. Shots: `27g_picnic`, `l70_lets_go`, `ft60_tera_raid`.
- **Done when** the lead beats a wild Lechonk without the screen and the record replays.

### Z16 · The Way Home and the expansions (optional)
- Area Zero as a cave-set matrix, the four Paradox species placed, the professor's battle, the League, `PaldeaHallOfFame`; Kitakami and Blueberry Academy as two more world folders of the region (optional).
- Tests: every chapter headless; Area Zero's ride rules (no glide). Shots: `wp40` to `wp52`.
- **Done when** a Paldea game runs from Cabo Poco to the Hall of Fame.

### Hisui

### Z17 · A side region: the branch and Jubilife Village
- `RegionLink.Kind { Next, Side }`, `SideLinksFrom`, `Region.Side` (left out of the chain's order), the link test counting chain links alone and a new test for a side link's rules (reached after its root's Hall of Fame, returned from by the same way, the party resting as plan 18 · W4's crossing has it); `regions/hisui.json` (Laventon, Rowlet, Cyndaquil and Oshawott, `Challenge = "none"`, the research rank); the Celestic ruins' scene in Sinnoh's post-game script; Jubilife Village as a town of its own with the Galaxy Hall and the pastures as the boxes.
- Tests: `RegionTests` with the branch; the ruins' scene headless. Shots: `wh01` to `wh06`, `st41_hisui_crossing`.
- **Done when** a Sinnoh Champion reaches the village from Celestic Town and comes back.

### Z18 · The open areas and catching from the field
- The Obsidian Fieldlands and the Crimson Mirelands as matrices with `WildVisible`, the base camps as warps; `Overworld/FieldCatch.cs` (the throw, the shakes, the back strike, a Pokémon that flees or fights), the Hisuian balls' rows in `BallTenths` (the Heavy family's weight rule, the Feather family's reach), the Strange Ball on a Pokémon brought in; the throw's arc and the ball's flashes drawn by `FieldLife` and `BattleBall` with the style guide's "Life" line first.
- Tests: a throw's odds equal a battle's at turn 0 for the same ball; a back strike's bonus; a Pokémon that fails its catch turns to a battle through `MeetWildPokemon`; a Gigaton Ball at a distance. Shots: `wh10` to `wh16`, `l72_field_catch`, `l73_field_catch_flees`.
- **Done when** a Bidoof is caught from the field in the harness and the catch registers as a battle's would.

### Z19 · Styles, research and the Nobles
- `BattleChoice.Style` with its rule in `BattleCore` and its refusals in `WhyNot`, mastery kept on the Pokémon's move (`Move.Mastered`, saved), the move menu's third row; the research rank from plan 15 · E1's levels gating the story's flags; `Overworld/NobleFight.cs` and Kleavor's fight in the Fieldlands.
- Tests: a Strong move's power and order, an Agile's, each a PP more, refused in Sinnoh and before mastery, a replay with styles; the rank's thresholds; the Noble's charges as a function of time and a stun opening the battle. Shots: `23f_battle_styles`, `wh20_noble_charge`, `wh21_noble_stunned`.
- **Done when** Kleavor is calmed in the harness and the style's numbers hold in `BattleUniqueTests`.

### Z20 · Hisui's forms and the story's end
- The four Hisuian forms not yet sculpted (Sliggoo, Goodra, Avalugg, Decidueye; Samurott, Lilligant, Zorua, Zoroark and Braviary came with plan 03's Unova batches, and plan 03's Kalos and Alola batches bring these four if they come first) in `PokemonModels.Regional.cs` with the seven evolutions `species.json` keeps to Hisui alive; the Cobalt Coastlands, the Coronet Highlands and the Alabaster Icelands with their Nobles; the Temple of Sinnoh and the end; Arceus optional.
- Tests: every form hand-built and distinct (`PokemonModelTests`); a Hisuian Zorua in Hisui and a Unovan one elsewhere; every chapter headless. Shots: `dex` of the four, `wh30` to `wh44`.
- **Done when** a Hisui game runs from the village to the temple and every Hisuian form stands in its own sculpt.

## Risks

- **The readers may not be possible.** No decompilation documents the 3DS or Switch containers as Platinum's does; a session that can't read a dump within its first day falls back to hand-authored layouts in the region's shape (decision 1's (b)) and says so in the plan.
- **A grid reading of a 3D city may not be the place.** Lumiose's ring, Hau'oli's beach, Wyndon's bowl: the user judges each region's first town before the rest, as plan 01 · M4 did, and a region whose first town fails stops there.
- **Size.** Paldea at a tile per two metres is larger than Sinnoh's 960×960; streaming holds, but the habitats map and the Town Map are drawn whole, so Z13 reports the size before the matrix is opened and halves the scale if it must.
- **Generated species.** Kalos to Paldea are played with generated models until plan 03's batches reach them; a region's plan may ask for its batch first (plan 03 · decision 3's order), and this is the real cost of the later regions.
- **Field Pokémon and weather on screen** are new per-pixel and per-model costs: Z10 and Z18 measure before they are called done and drop the crowd, the far Pokémon or the second mist layer if the frame goes over.
- **The level curve.** Plan 18 · decision 2 (a) lands the player with none; the fixed levels of each region's leaders then play as the original's, with plan 13 · V3's cap table a row per region.
- **These plans are far off** and will be rewritten when their time comes, after Unova; what is fixed here is the decisions each region forces on the engine, not its sessions' contents.

## Needs and gives

- **Needs** plan 18 (W1 to W5 and decision 5; every session), plan 22 · U1's dump reader as the family's first and its U2 for the events' shape, plan 10 · F1 and F5 (Z10, Z14, Z18), plan 14 · B2 to B5, B8 and B13 (Z3, Z6, Z8, Z12), plan 06 · R20 to R23, R28 and R29 (the homes), R14's date (Z10), plan 11 · C10 (Z2), plan 15 · E1 (Z19), plan 13 · V3's cap table shape (Z6, Z14), plan 02 · S2's obstacles and flags (done; Z5), plan 03's batches for the species and D12's placing of the legendaries, plan 09 · L4 only if a region's scene wants a path (none is planned).
- **Gives** plan 14 · B5 the area that sets `callsForHelp` and B13 its venue; plan 06 · R20 to R23 their homes and the dens and raids built; plan 03 · D12 a region to meet every regional form in and the Stone Arch; plan 18 the later links' arrivals; plan 10 · F8 the second kitchen; plan 02 the Celestic ruins' post-game scene; rulings.md the lines on region-bound evolutions, styles and field catching amended.

## Decisions for the user

1. **Galar and Paldea on the grid.** *Recommended:* (a) sampled from the dump at a tile per two metres, landmarks placed where they stand, the user judging the first town. The alternatives: (b) hand-drawn in the region's shape with plan 16 · T9's editor; (c) the chain ends at Alola.
2. **Eight directions.** *Recommended:* no; four directions everywhere, the skates and the ride as `TravelMode`s with their own pace and rules. The alternative, eight-direction movement as an option, needs four more facings of every sprite and new rules beside `player_move.c`'s.
3. **Hisui's place.** *Recommended:* a side region off Sinnoh, reached from the Celestic ruins after Sinnoh's Hall of Fame, with `RegionLink.Kind`. The alternative is a tenth chain link after Paldea, which puts Sinnoh's past last.
4. **Hisui's battles.** *Recommended:* the game's turn order with the styles as a choice on the move. The alternative rebuilds Legends: Arceus's action order as a second core, which nothing else uses.
5. **Order.** *Recommended:* the chain's (Kalos first), with Hisui whenever the user wants it, since it hangs off Sinnoh and its reader is Paldea's. The alternative starts with Hisui for its hand-built forms.
6. **The Wild Area's Pokémon.** *Recommended:* the area's own rule (`WildVisible`), whatever the option says, because it is how Galar is played; the option keeps governing Sinnoh's grass. The alternative follows the option everywhere.
7. **Sandwiches.** *Recommended:* a second section of `recipes.json` on the one `Kitchen`, amending plan 10 · decision 4. The alternative keeps curry alone and Paldea's picnic cooks it.
8. **Expansions.** *Recommended:* optional, last in each region (Ultra Space, the Isle of Armor and the Crown Tundra, Kitakami and Blueberry), built only if the region's story is played through. The alternative plans them as sessions of their own now.

## Status

- [ ] Z1 The 3DS reader and Vaniville to Santalune
- [ ] Z2 Lumiose and central Kalos
- [ ] Z3 Shalour, the coast and the Tower of Mastery
- [ ] Z4 The mountain, Team Flare and the League
- [ ] Z5 The four islands, the Ride Pager and the ferry
- [ ] Z6 Melemele and Akala: trials, totems and Z-Moves
- [ ] Z7 Ula'ula, Poni and the Aether Foundation
- [ ] Z8 After the League: Ultra Space and the Battle Royal (optional)
- [ ] Z9 Galar's shape and Postwick to Motostoke
- [ ] Z10 The Wild Area: field Pokémon, weather by day and the dens
- [ ] Z11 The gym challenge: stadiums, badges and the bike on water
- [ ] Z12 The Champion Cup, Eternatus and the expansions (optional)
- [ ] Z13 Paldea's shape, the academy and the ride
- [ ] Z14 The ride's powers and the three paths
- [ ] Z15 Picnics, Let's Go and the Tera Raids
- [ ] Z16 The Way Home and the expansions (optional)
- [ ] Z17 A side region: the branch and Jubilife Village
- [ ] Z18 The open areas and catching from the field
- [ ] Z19 Styles, research and the Nobles
- [ ] Z20 Hisui's forms and the story's end
