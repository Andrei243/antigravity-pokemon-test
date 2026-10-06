# Plan 19 · Kanto
Written 2026-10-06, before any session.

**Goal**: the region a new game starts in, built in full in place of the stand-in Pallet Town: its map read from FireRed and LeafGreen's layouts, every town, route, cave and room from Pallet Town to the Indigo Plateau and the Sevii Islands beyond, its story from Oak's lab to the Hall of Fame in our own words on the original's beats, its gyms and badges, its trainers with their teams, its wild Pokémon, its music by role, and the ship from Vermilion City that takes a Champion on to Johto. Every species of Kanto is hand-built already, so this plan builds no model.

It follows the shape of plans 01 and 02 together: an import and the first areas, then chapters that open areas and write their scenes, each with its music and its trainers. It needs plan 18 (the regions framework and the GBA reader) before its first session.

## Where we are

- **Kanto is a stand-in.** `Data/RegionDatabase.cs` lists it first, with `Start = new MapSpot("PalletTown", 9, 9)` and two hand-made maps, `Data/maps/PalletTown.json` (20×21 tiles, `Clapboard` houses, the song `kanto/pallet`) and `PalletPlayerHouse.json`; the link to Johto is `Transport.Boat` with `DepartureMap = "PalletTown"`, whose pier stands in for Vermilion's harbour. `RegionTests` holds the chain (thirteen tests: generation order, warps that never leave their region, an attendant at every built region's departure, the way on opening at `KantoHallOfFame`). `BuildingTests`, `DataFileTests` and `StoryTests` use the stand-in as a fixture.
- **Everything a new game does is Sinnoh's.** `GameEngine.InitializeNewGame` gives a Turtwig whatever the region; `StoryState.Starters` and `StarterSelectScreen.Starters` are Turtwig, Chimchar and Piplup; `IntroScreen.Professor` is `"Prof. Rowan"` and `IntroScreen.ShownSpecies` is Buneary; `Badge` is `Coal` to `Beacon` and `ModernUi.BadgeNames` draws them; `PokedexMode` is `Sinnoh` or `National`, `Pokedex.CanUnlockNational` asks for Sinnoh's Hall of Fame, and `Habitats.Sinnoh` reads only `world/sinnoh/habitats.json`; `MapDatabase.StartMap` is `"Sinnoh"` and an unknown map name falls back to it; a whiteout puts the player in `MapDatabase.Get("PlayerHouse")`, Sinnoh's, and Fly and Teleport land on Sinnoh's twenty spawn locations (`Data/SpawnLocations.cs`, plan 02 · S2), whose last Pokémon Center gone into is where plan 06 · R10 will send a whiteout. Plan 18 turns these into a region's record (its professor, starters, badges and their field-move gates, spawn locations, Pokédex, player looks, world folder), and this plan fills Kanto's in.
- **The world loads by folder.** `World.LoadAll()` reads every `Data/world/<region>/` that has a `world.json` (`WorldIndexFile.Region`, its `Maps` and `Areas`), and `MapDatabase.Load` makes each world's maps through `WorldMapBuilder.Build`. An area's people, signs, warps and triggers are its file's (`WorldAreaFile`, `AreaObject`, `AreaSign`, `AreaTrigger`), its music and our lines its overlay's (`WorldOverlayFile`); `WorldMapBuilder.CharacterFor` folds the original's looks onto our characters. A trainer's mind, items and team are Platinum's table (`Data/trainers.json`, plan 06 · R9, which plan 08 · P6 now names: 927 trainers written by `tools/DataImporter`'s `Importer.Trainers.cs` from the decompilation's `res/trainers/data`, read by `TrainerDatabase`), and `TrainerDatabase.Fill` gives a placed trainer its record where the overlay's `trainer` block (`OverlayPerson.Trainer`) names the same team or none; the block is there for the lines, and the eight overlays that still type a team beside them lose it in plan 08 · P7.
- **The importer reads one game.** `tools/MapImporter` reads pret/pokeplatinum's folders (`DecompMaps.Folders`), sorts ground by texture name (`Cover.Rules`) and writes `Data/world/sinnoh` alone (`Program.cs` names the folder). Plan 18 adds a reader of the GBA decompilations' format beside `DecompMaps`, pinned in `Sources.cs` like the others, that writes the same world files; Kanto is its first use.
- **Music and sound** look in a region's folder first: `Data/music/kanto/` holds `pallet.mml` and `battle_wild.mml`, `MusicDirector.Resolve` tries `<region>/<file>` before `common/<file>`, and `AudioManager.Region` is set from `RegionDatabase.RegionOfMap` on every map change. `Data/audio/sound-map.json` gives a trainer class its eye theme and, for a few, its battle theme by the class's name. `MusicTests.TestEveryRoleHasASongInEveryRegion` and `TestEveryMapPlaysASongThatExists` hold the folders to the roles. Every species of Kanto has a cry (plan 05 · A4).
- **Scripts** are one text file per area key in `Data/scripts/` plus `common.txt` (`docs/scripts.md`); `ScriptParser.FlagName` accepts `FLAG_*` names and a region's `<Region>HallOfFame`; the language has `starter`, `givebadge`, `travel`, `warp`, `battle` (with two trainers and a partner since plan 06 · R9: `battle a and b with c`) and `wildbattle` (`nofleeing`), and plan 02 · S2's `usemove`, `surf`, `fly` and the other field moves' commands; `HeadlessScriptHost` plays a chapter to its end and `StoryTests.EveryScriptOfTheGamePlaysToItsEndOnEveryWayThroughIt` plays every script of every file. `StoryState.CurrentVersion` is 1.
- **Looks**: `CharacterStyle.For` has thirteen looks and a default; plan 11 · C1 moves them into `Data/characters.json` and C2 to C4 give every class of Platinum its own. Kanto's dozen classes of its own are not among them.
- **Models**: all 151 species of Kanto are hand-built (`PokemonModels.Kanto1.cs` to `Kanto3.cs`, plan 03's Kanto batches), with their forms; `PokemonModelTests` holds the list.
- **The field** walks by Platinum's tile behaviours (`TileBehavior`, `FieldMovement`). Plan 02 · S2 gave the party menu Platinum's fifteen field moves (`FieldMove`) and their checks (`FieldMoveRules.Check`), each move's badge read from Sinnoh's `Badge` enum (`FieldMoveRules.BadgeFor`: Forest for Cut, Cobble for Fly, Fen for Surf); the movement rules ask only that a move is known (`FieldMoves`: Surf, Waterfall, Rock Climb and Flash). The Bicycle (`Overworld/Bicycle.cs`) and the rods (`Overworld/Fishing.cs`, reading the area files' `OldRod`, `GoodRod` and `SuperRod`) are used from the bag. The harness starts every run in Sinnoh (`engine.NewGameRegion = "Sinnoh"`) and its `cities` mode reads the importer's Sinnoh output.

## Design

### The source: FireRed and LeafGreen

Two decompilations hold a Kanto. pret/pokeheartgold has HeartGold and SoulSilver's, in Platinum's own formats, so `tools/MapImporter` would read it almost as it is; but it is a post-game Kanto with gyms at levels 40 to 60 and no first story, and a new game starts here. pret/pokefirered has the whole Generation 1 story, Oak, the rival, Team Rocket, eight gyms at a beginner's levels, the Elite Four, and the Sevii Islands, in the GBA format plan 18's reader is written for. **FireRed and LeafGreen are the source**; HeartGold and SoulSilver's Kanto is kept in mind for later (decision 4): its Kanto after the Johto story is the natural post-game for plan 20, and its Pokéathlon-era buildings and Viridian Forest are the same places at a later date.

The assets policy holds as it does for Sinnoh (plan 01 · M1's decision, CLAUDE.md's rules): the reader keeps **layouts and numbers** (where each metatile is, what it does, its elevation, where people, warps, signs and triggers stand, which trainer a person is, a map's music and weather by name, the encounter tables, the trainers' tables, the flags' and variables' names), and never a metatile's graphics, a tileset, a palette, a song or a line of text. The import's cache stays git-ignored.

### Kanto's world folder

`Data/world/kanto/` has the layout of `Data/world/sinnoh/`: `world.json` with `"region": "Kanto"`, the generated `matrices/`, `chunks/`, `areas/` and `habitats.json`, and hand-written `overlays/<key>.json`. Plan 18's reader stitches the connected maps of FireRed into one matrix (the overworld, `Kanto`, measured in tiles of the whole region like `Sinnoh`), one matrix per group of maps connected to each other but not to it (each Sevii island with its routes: `SeviiOne` to `SeviiSeven`), and one per map that connects to nothing (every cave floor and every room). `world.json` lists which become maps; a cave takes `"setting": "Cave"` from its `map_type`, and so `FieldCamera.Cave`, standing rock and darkness (plan 01 · M5's `WorldMapBuilder.CaveRock`). An area is a FireRed map: its key is the map's name in lower case (`pallet_town`, `route_1`, `viridian_forest`, `mt_moon_1f`), which collides with none of Sinnoh's.

What the reader fills in `WorldAreaFile` is what `map.json` says: `music`, `weather` (`Weathers.Of` takes the name), `allow_cycling`, `allow_running`, `allow_escaping`, `show_map_name`; the `object_events` as `AreaObject`s (`graphics_id` is `Looks`, `trainer_type` and `trainer_sight_or_berry_tree_id` are `Trainer` and `Sight`, `flag` is `HiddenBy`); `warp_events`, `coord_events` as `AreaTrigger`s with their variable and value, and `bg_events` as `AreaSign`s and hidden items. The encounter tables (`src/data/wild_encounters.json`) have the same twelve land slots at the same odds as Platinum's and the same five on water, so `WorldAreaFile.LandSlotWeights` and `WaterSlotWeights` serve as they are; FireRed has no morning, day and night, so the one table stands for all three in `habitats.json`, and the rods' slots go to the area file's `OldRod`, `GoodRod` and `SuperRod` with FireRed's own weights (plan 18's reader), where plan 02 · S2's fishing finds them, and to `HabitatArea.OldRod`, `GoodRod` and `SuperRod`. Heights are the elevation nibble, whole tiles; a Kanto cliff is a blocked metatile, and `Relief` draws it as a face as it draws Sinnoh's.

**Ground and buildings.** A GBA tile has no texture name to sort by, so the cover table is by tileset and metatile range (plan 18 gives the mechanism; `Cover.Kanto` holds the rows: lawn, tall grass, dirt paths, sand, water, trees, Mt. Moon's rock, the cycling road's slope, paving). A building is metatiles too, not a model, so there is no `WorldModels` catalogue for Kanto: `MapStructures.FindBuildings` finds each block of building tiles as it does on a hand-made map, its kind from the door's destination (`*_PokemonCenter_1F` is a Pokémon Center, `*_Mart`, `*_Gym`, `*_House`) or from a row of the overlay's `buildings` for the rest (the museum, the Game Corner, Silph Co., the Pokémon Tower), and `BuildingArt.StyleOf` dresses it by the town. Kanto's towns need their own ways of building: `Architecture.Clapboard` is Pallet's already; Viridian, Pewter, Cerulean, Vermilion, Lavender, Celadon, Fuchsia, Saffron, Cinnabar and the Sevii Islands get a row each in the style guide's "Buildings" section **before** the enum grows and `BuildingArt.Styles.cs` paints them.

### Rooms from layouts

Sinnoh's rooms are hand-made map files (plan 01 · M11 makes the rest from the decompilation's furniture). In FireRed every room is a map with a layout of its own, so Kanto's rooms are imported: a matrix of one map each, built by `WorldMapBuilder` with `InteriorStyle` set from the map's tileset (a house, a Pokémon Center, a Mart, a lab, a gym) and its furniture from a table of the interior tilesets' metatile ranges onto `PropType` (a bed, a table, a bookcase, the Center's counter, the PC, the Mart's shelves), drawn by `PropModels` and `GroundBaker` as today's rooms are. A room's warps are its file's and join the outdoors by themselves (`WorldMapBuilder.Arrival`), so an overlay's `doors` block is only for a door that must lead somewhere else. A room that is a dungeon (Silph Co.'s eleven floors, the Rocket Hideout, the Pokémon Mansion) is a room with the dungeon's props: lifts, warp tiles, spin tiles, the Mansion's switches, each a `TileBehavior` the reader maps (plan 18's table in `docs/tile-behaviours.md`) and `FieldMovement.Carries` or a script plays.

### The region's record

Plan 18 makes `Region` carry what a region is. Kanto's entry: the professor (Oak, with our own welcome on FireRed's beats, and the Pokémon he lets out of its ball), the starters (Bulbasaur, Charmander, Squirtle; the rival takes the one that beats the player's, `StoryState.RivalStarterFor`'s rule), the badges (Boulder, Cascade, Thunder, Rainbow, Soul, Marsh, Volcano, Earth, with their colours and marks for `ModernUi.Badge`), the regional Pokédex (the National Pokédex's first 151, from plan 18's `regionalNumbers`), the two player looks (our own takes on FireRed's boy and girl; plan 11 · C1's table), the music folder `kanto`, and the start, in front of the player's house in Pallet Town on the `Kanto` map. The rival's name is asked in the introduction, as FireRed asks it and as plan 02 · S4 adds the step for Sinnoh; written lines say `{rival}`. FireRed has no professor's assistant: Kanto's files never say `{assistant}`, and `StoryTests` holds it.

**Saves and the whiteout.** A save made in the stand-in Pallet Town wakes up on the `Kanto` map: two rows of `SaveData.OldMaps` and a `WorldVersion` after `ImportedJubilife` (`ImportedKanto = 3`). A whiteout goes to the last place the party was healed, as FireRed's `heal_locations` have it. Plan 02 · S2 keeps Sinnoh's place to come back to as the original does, a spawn location's number (`VAR_SPAWN_LOCATION`, set on going into a Pokémon Center, `SpawnLocations.Respawn`), and plan 06 · R10 sends the whiteout there; Kanto's heal locations are its region's `Spawns` (plan 18 · W1 and W7), the player's house in Pallet Town first as Twinleaf's is Sinnoh's, so Fly, Teleport and a whiteout in Kanto land in Kanto.

**The gates.** Kanto ties the field moves to its own badges: Flash to Boulder, Cut to Cascade, Fly to Thunder, Strength to Rainbow, Surf to Soul, Rock Smash to Marsh, Waterfall to Volcano, and every level obeys with Earth. Plan 02 · S2 wrote the check once (`FieldMoveRules.Check` asks `BadgeFor`), on Sinnoh's `Badge` enum today; plan 18 · W2 makes it read the region's `Gates`, and Kanto's rows are K3's.

### The story's scripts

Each chapter reads the maps' `scripts.inc` and the common `data/scripts/*.inc` of pokefirered for what happens and in what order, keeps the flags' and variables' names (`FLAG_BADGE01_GET`, `VAR_MAP_SCENE_*`, the hide flags of each map's objects), and writes every line in our own words (`docs/scripts.md`; the README's "Writing a scene"). The scenes go in `Data/scripts/<area key>.txt`; what every Kanto game starts with is set in `common.NewGame` beside Sinnoh's flags, since a flag's name is the original's and the two decompilations' names don't meet; where they do (a test compares the two lists), Kanto's takes a `KANTO_` prefix. People a chapter brings on or takes off are hidden by their objects' own flags, as in Sinnoh. Every chapter adds a story walk with `HeadlessScriptHost`: the chapter played through with battles won, checking the flags, the items, the badges and the party at its end. A chapter that changes what a save already in play must know raises `StoryState.CurrentVersion` and adds its step to `StoryMigration`.

### Trainers, classes and looks

A trainer's team is FireRed's (`src/data/trainers.h` and `trainer_parties.h`: species, levels, held items, the moves where the original chose them), read by plan 18's reader into Kanto's own trainer file, `Data/world/kanto/trainers.json`, in the record shape of plan 06 · R9's `Data/trainers.json` (`TrainerRecord`, `TrainerPokemonRecord`), so a region's trainers travel with its world (plan 18's decision 8); `TrainerDatabase` reads every world's, and `TrainerDatabase.Fill` gives a placed trainer its team as it gives Sinnoh's. An overlay's `trainer` block carries the lines alone, as plan 08 · P7 leaves Sinnoh's. Prize money is the class's rate times the last Pokémon's level, FireRed's own rates. Kanto's classes that Platinum lacks (Super Nerd, Burglar, Engineer, Juggler, Tamer, Channeler, Cue Ball, Biker, Gamer, Rocker, Team Rocket's grunts, Crush Kin, Cool Couple) get rows in `sound-map.json` and, until plan 11 · C4 gives each a look of its own, fold onto the nearest look in `WorldMapBuilder.CharacterFor` (a Biker onto the Roughneck's, a Channeler onto the Lady's), listed in the fallback block plan 11 · C1 counts.

### Music

A song per role of FireRed's music table, in `Data/music/kanto/`, our own: the towns (`pallet` exists; `viridian`, `pewter`, `cerulean`, `vermilion`, `lavender`, `celadon`, `fuchsia`, `cinnabar`, `indigo`), the route families FireRed shares one theme over (`route1`, `route3`, `route11`, `route24`), the dungeons (`viridian_forest`, `mt_moon`, `rocket_hideout`, `pokemon_tower`, `silph`, `cave`), the gym, the Cycling Road, the Sevii routes and islands, and the battle roles by their file names (`MusicDirector.FileName`: `battle_trainer`, `battle_gym`, `battle_rival`, `battle_champion`, `battle_elite_four`, the three victories, `eye_boy`, `eye_girl`). Team Rocket takes the villains' roles under Sinnoh's names, `EyeGalactic`, `BattleGalactic` and `BattleGalacticBoss`: a role is a job, and in Kanto the job is Team Rocket's, so `kanto/eye_galactic.mml` is Rocket's theme and the sound map's "Rocket Grunt" row points at it. Each area's overlay names its song by FireRed's `music` role, and every area theme has its night arrangement as Sinnoh's do, though FireRed had no night. Mewtwo and the three birds have their own battle themes already (`Data/music/common/legendary/`). Checked as plan 05 · A6 checks: `tools/MusicRender`, the levels, loudness and clash report, the user listening.

### Battles by Platinum's rules

Kanto is fought by whichever rules the game was begun under (`Ruleset`, plan 06 · R1): Platinum's by default, with abilities, held items and the physical-special split, on FireRed's teams and levels. FireRed's own Generation 3 rules are not a third preset. This is a ruling to record in `docs/mechanics/rulings.md` in K3, and the Platinum preset keeps giving in Sinnoh what it gave. `GameEngine.TerrainAt` picks the ground as it does in Sinnoh, from the tile first and then `MapArea.BattleBackground`, which the reader fills from the map's `battle_scene`.

### Tests and shots

`KantoTests` is to Kanto what `WorldTests` and `SouthWestTests` are to Sinnoh: the index says what is built, every chunk in view has its file, every overlay entry points at something, every warp lands on open ground and leads back, people stand where they can be spoken to, wild Pokémon are the area's, and `WorldWalk.From` Kanto's start reaches every open area through its warps, doors and item balls. `RegionTests` holds Kanto built, its start and arrival on its own map, its departure at Vermilion with an attendant. The harness gets a `kanto` mode, not part of `all` (its people would move every battle of the modes after it): it starts a new game in Kanto, shoots each open town, route, cave and room by day and after dark (`k01_pallet_day`, `k02_pallet_night`, …), walks from Pallet Town to Pewter City at running pace timing every frame and printing what streaming cost, and shows the chapters' scenes (`ks01_oak_stops_you`, …) through the game's own `TryInteract` and the question box's buttons, as the `story` mode does. Two runs draw the same pictures: whatever is new takes its chance from `Dice` and its time from `FrameClock`, and a change is judged by `diff` of a `kanto` run against a `kanto` run.

## Sessions

### K1 · The import and Pallet Town
- `Sources.cs` pins pret/pokefirered; plan 18's reader writes Kanto's matrices, chunks, areas, habitats and trainers into `tools/MapImporter/out/kanto/` with its mosaic (`kanto.png`) and a picture per area; `--data --region kanto` writes `Data/world/kanto/`. `world.json` with the `Kanto` map and `pallet_town` and `route_1` open; `Cover.Kanto`'s rows for their ground; the overlays (music, Mom, the neighbours, the signs); Oak's lab and the two houses as rooms (the first rows of the interior table).
- `RegionDatabase`: Kanto's `Start` on the `Kanto` map, its `Maps`; the two stand-in files move to `PokemonPlatinumTests/Fixtures/maps` and `Fixtures.Names` grows, as Jubilife City's did; `SaveData.OldMaps` and `ImportedKanto`.
- Tests: `KantoTests` (the index, the chunks, the overlays, the walk from the start, a save from the stand-in waking up in Pallet Town); the reader's cover rows on a layout built in the test; `RegionTests` unchanged but for the start.
- Harness: the `kanto` mode with `k01`–`k04` (Pallet Town and Route 1 by day and after dark) and the timed walk.
- **Done when** a new game begun with `--region Kanto` stands in Pallet Town on the imported map, walks Route 1 among its wild Pokémon, and the frame on High is under 8 ms.

### K2 · Rooms from layouts, and Viridian
- The interior table for every tileset Kanto's rooms use; `InteriorStyle` rows a Kanto room needs (the gym hall, the museum, the Game Corner, an office floor); the Pokémon Center and Mart as imported rooms with the nurse, the PC and the clerk placed by their objects; the lift, the warp tile and the spin tile as behaviours. Viridian City, Route 2 and Route 22 open, Viridian's style in the style guide first.
- Tests: every room of an open area has its walls, a floor and its door back; the furniture table names only `PropType`s that exist; a lift's script moves between floors in `HeadlessScriptHost`.
- Harness: `k05`–`k10` (Viridian, the Center, the Mart, the gym's shut door, Route 22 by night).
- **Done when** every room of Pallet Town and Viridian City is imported and furnished, and `BuildingTests` builds Kanto's kit without a GPU.

### K3 · Pallet to Pewter
- The introduction as Oak (the region's professor and Pokémon, the rival's name), Oak stopping the player in the grass, the starters on the lab's table, the first rival battle, Oak's Parcel, the Pokédex, Viridian Forest, Pewter City and its museum, Brock and the Boulder Badge (`FLAG_BADGE01_GET`), Route 3, the rival on Route 22. Kanto's badges and gates in the region's record; the Bug Catchers, Youngsters and Lasses with FireRed's teams. The ruling on Platinum's rules in Kanto.
- Music: `route1`, `viridian_forest`, `pewter`, `gym`, `battle_gym`, `battle_rival`, `battle_trainer`, `eye_boy`, `eye_girl`, the victories.
- Tests: the chapter's story walk (a starter chosen, the Parcel delivered, the Pokédex had, the Boulder Badge won, Flash's gate open); the gates' table.
- Harness: `ks01`–`ks08` (Oak's stop, the choice, the first battle, Brock's hall, the badge's fanfare); `k11`–`k16`.
- **Done when** the chapter plays from NEW GAME to Route 3 with every scene, and `--region Sinnoh` still begins with Rowan.

### K4 · Mt. Moon to Vermilion
- Mt. Moon (the fossils, the Rocket grunts, the Moon Stone), Route 4, Cerulean City, Nugget Bridge, Bill's house, Misty and the Cascade Badge, the robbed house and the Dig TM, Routes 5 and 6 and the Underground Path, Vermilion City, the S.S. Anne (the rival, the captain, HM01 Cut), Lt. Surge's bin puzzle and the Thunder Badge, Diglett's Cave, Route 11. Team Rocket's classes, looks and roles.
- Music: `mt_moon`, `cerulean`, `route24`, `vermilion`, `ss_anne`, `eye_galactic`, `battle_galactic`.
- Tests: the story walk; the bins' puzzle in `HeadlessScriptHost`; the ship leaves once Cut is had.
- Harness: `ks09`–`ks16`, `k17`–`k26`.
- **Done when** the player can Cut the tree beside Vermilion's gym and the S.S. Anne has sailed.

### K5 · Rock Tunnel to Saffron
- Routes 9 and 10, Rock Tunnel (Flash's gate), Lavender Town, Route 8, Celadon City (the department store, the Game Corner's hidden door, the Rocket Hideout and Giovanni, the Silph Scope, Erika and the Rainbow Badge, HM02 Fly from the house on Route 16), the Pokémon Tower (Marowak's ghost, Mr. Fuji, the Poké Flute), the Snorlax on Route 12 and Route 16, Saffron City (the Fighting Dojo, Silph Co.'s floors and the Card Key, Giovanni again, the Master Ball, Sabrina and the Marsh Badge). Fly is plan 02 · S2's (`UI/FlyScreen.cs` over the spawn locations) and needs Kanto's map on its screen and Kanto's towns among its `Spawns`, both this session's; the Town Map item stays plan 01 · M12's.
- Music: `lavender`, `pokemon_tower`, `celadon`, `rocket_hideout`, `silph`, `saffron`, `route11`, `battle_galactic_boss`.
- Tests: the story walk; the tower is barred without the Scope; the Snorlax wakes to the Flute.
- Harness: `ks17`–`ks26`, `k27`–`k38`.
- **Done when** the Marsh Badge is won and Fly takes the player to any town seen.

### K6 · Fuchsia and the sea
- Routes 12 to 15, the Cycling Road (Routes 16 to 18, the slope as a moving floor), Fuchsia City, the Safari Zone (its battle is plan 06 · R9's `BattleKind.Safari`, done, and its stay the Great Marsh's as a place, with its steps and balls, plan 01 · M7 and plan 06 · R16; the Gold Teeth, HM03 Surf and HM04 Strength), Koga and the Soul Badge, Routes 19 to 21 by sea, the Seafoam Islands with Articuno, the Power Plant with Zapdos (needs Surf: this session), Cinnabar Island's shore.
- Music: `fuchsia`, `route3`, `cycling`, `surf`, `seafoam`, the legendaries' themes already there.
- Tests: the story walk; the Safari Zone's steps and balls; the boulders of the Seafoam Islands dropped in order.
- Harness: `ks27`–`ks32`, `k39`–`k48`.
- **Done when** the Soul Badge is won and the player surfs from Fuchsia to Cinnabar.

### K7 · Cinnabar, the first islands and Viridian's gym
- The Pokémon Mansion and the Secret Key, Blaine's quiz and the Volcano Badge, Bill's ferry to One Island, Two Island, Three Island and Lostelle (Mt. Ember, the Berry Forest), back to Cinnabar, Viridian's gym with Giovanni and the Earth Badge, the rival on Route 22 again, Route 23's badge gates.
- Music: `cinnabar`, `pokemon_mansion`, `sevii_island`, `sevii_route`, `ferry`.
- Tests: the story walk; the gates of Route 23 ask each badge in order.
- Harness: `ks33`–`ks40`, `k49`–`k58`.
- **Done when** all eight badges are on the card and Route 23 lets the player through.

### K8 · Victory Road and the League
- Victory Road's boulders and floors, the Indigo Plateau, the Elite Four (Lorelei, Bruno, Agatha, Lance), the rival as Champion, the Hall of Fame (`KantoHallOfFame` set, the party recorded), the credits, waking up at home; the ship at Vermilion's harbour with its attendant (`IsTransportAttendant`, `RegionLink.DepartureMap = "Kanto"` with Vermilion's pier, `common.Attendant`) and `TravelTo` Johto when plan 20 has built it.
- Music: `indigo`, `victory_road`, `battle_elite_four`, `battle_champion`, `victory_champion`, `hall_of_fame`, `credits`.
- Tests: the story walk to the Hall of Fame; `RegionTests` finds the attendant at Vermilion; the way on opens only after the flag.
- Harness: `ks41`–`ks48`, `k59`–`k66`.
- **Done when** a game begun in Pallet Town reaches the Hall of Fame and the sailor at Vermilion answers as `RegionDatabase.AttendantLines` says.

### K9 · After the League: the Sevii Islands
- Celio's errand (the Ruby from Mt. Ember, the Rocket Warehouse on Five Island, the Sapphire from the Dotted Hole), Four to Seven Islands with their routes and caves (Icefall Cave and HM07 Waterfall, Lost Cave, the Tanoby Ruins and their Unown, Pattern Bush, Altering Cave), Moltres on Mt. Ember, Cerulean Cave and Mewtwo, the Elite Four's second teams, the National Pokédex from Oak by FireRed's rule (decision 3), the Trainer Tower optional. Kanto's habitats complete, so the Pokédex's area page shows all of it.
- Music: `cerulean_cave`, `sevii_dungeon`, the rematch themes if the user wants them.
- Tests: the story walk of the errand; every area of Kanto open; `WorldWalk` from the start reaches every tile of the region with every field move.
- Harness: `ks49`–`ks54`, `k67`–`k80`.
- **Done when** every area of pokefirered's Kanto is open and walked, and the National Pokédex opens by its rule.

## Risks

- **Scope against Sinnoh.** Nine sessions of a second region compete with finishing Platinum, which the README puts first. The order is kept by rule: K1 and K2 wait for plan 18, and no Kanto chapter goes before the Sinnoh chapter of the same number is done (K3 after plan 02 · S4, K4 after S5, and so on), so Kanto never leads the game it is a prelude to.
- **The reader's gaps.** Connections with offsets, elevation as height, a cover table per tileset and rooms from layouts are plan 18's and this plan's first two sessions: if a Kanto feature has no tile behaviour of Platinum's (the Cycling Road's slope, the spin tiles, the Mansion's switches), it needs a value in the reader's table and a rule in `FieldMovement` before its chapter.
- **Flag names.** Two decompilations' `FLAG_*` and `VAR_*` names share a namespace in the save. The test that compares the two lists catches a collision; a prefix settles it.
- **Looks before plan 11.** Kanto's classes fold onto Sinnoh's looks until plan 11 · C4; a Rocket grunt drawn as a Gentleman is wrong but not broken, and the fallback block says so.
- **Nine sessions of our own songs and lines.** The chapters are sized like plan 02's; a session that runs long leaves its last area for the next and says so in the status.

## Needs and gives

- **Needs** plan 18 first: the region record (professor, starters, badges, Pokédex, looks, world folder), the GBA reader and its behaviour table, and the crossing's decision on what carries over to Johto. Needs plan 02 · S1's scripts (done), S2's field moves, gates, obstacles, Fly, Bicycle and rods (done; their badges and spawn locations made the region's by plan 18 · W1 and W2), S4's rival name (K3); plan 06 · R9 (done) for the double battles, the tag battles and the Safari battle, R10 for what follows a battle (K4), R11 for the shops and TMs (K3); the Great Marsh's stay as a place (plan 01 · M7, plan 06 · R16) for the Safari Zone (K6); plan 06 · R9's trainer table (done; plan 08 · P6) for the record shape Kanto's file takes (plan 18's decision 8); plan 01 · M12 for the Town Map item (K5); plan 11 · C1 for the looks' table.
- **Gives** plan 20 (Johto) the departure at Vermilion and a Champion to carry over, plan 18 the first region built on the GBA reader and the first rooms from layouts (Hoenn's, plan 21, are the same shape), plan 03 · D12 the places of Kanto's species, plan 07 · O5 a Kanto player with a party to trade, plan 14 · B12 Kanto's gym leaders for the World Tournament, and plan 09 · L6's female looks their first wild appearances.

## Decisions for the user

1. **The source.** *Recommended:* FireRed and LeafGreen (pret/pokefirered), for the first story. The alternative is HeartGold and SoulSilver's Kanto from pret/pokeheartgold, cheaper to import but a post-game region with no beginning.
2. **FireRed's own extras.** The Fame Checker, the Teachy TV, the Vs. Seeker (Platinum's, plan 06 · R12), Berry Crush and the Trainer Tower. *Recommended:* the Vs. Seeker when R12 lands, the Trainer Tower as an optional part of K9, the rest left out; they are collectables without their hardware.
3. **The National Pokédex in Kanto.** *Recommended:* FireRed's rule, Oak after the Hall of Fame once sixty species are caught, as the region's unlock rule in plan 18's framework. The alternative is Sinnoh's rule (the regional Pokédex seen in full), stricter than the original's.
4. **HeartGold and SoulSilver's Kanto.** *Recommended:* kept for plan 20 as Johto's post-game, the same towns at a later date with the gyms at their later levels, if the user wants a second Kanto at all. The alternative is one Kanto only.
5. **The player characters' names.** *Recommended:* the introduction asks for the player's and the rival's names as FireRed does, with our own default names offered. The alternative keeps Sinnoh's introduction and names the rival for the player.

## Status

- [ ] K1 The import and Pallet Town
- [ ] K2 Rooms from layouts, and Viridian
- [ ] K3 Pallet to Pewter
- [ ] K4 Mt. Moon to Vermilion
- [ ] K5 Rock Tunnel to Saffron
- [ ] K6 Fuchsia and the sea
- [ ] K7 Cinnabar, the first islands and Viridian's gym
- [ ] K8 Victory Road and the League
- [ ] K9 After the League: the Sevii Islands
