# Plan 21 · Hoenn

Written 2026-10-06, before any session.

**Goal**: Hoenn as the third region of the chain, read from Emerald through plan 18's GBA reader: its map from Littleroot Town to Ever Grande and the sea floor under its eastern routes, its story with both teams, the three titans and Wallace, its eight badges and its trainers, Dive as a way of travelling, Secret Bases, the Contest Hall of the Generation 3 kind, the Battle Frontier island whose seven facilities already have their rules elsewhere, and a song for every role of Emerald's music table, all in our own art, sound and words. The player comes in by ferry from Johto and leaves by ship for Sinnoh.

## Where we are

- **The chain**: `Data/RegionDatabase.cs` has Hoenn as a `Region` with no `Start`, so `IsBuilt` is false, and its two `RegionLink`s (`Johto → Hoenn`, `Hoenn → Sinnoh`) are `Transport.Undecided`; `CheckTravel` answers `TravelCheck.DestinationNotBuilt`. `Transport` is `Undecided` or `Boat`. Plan 18 · W4 proposes the table every region confirms: Johto to Hoenn by ferry from Olivine to Slateport, Hoenn to Sinnoh by ship from Slateport to Canalave, and `RegionLink.Vessel`; W5 the journey on screen (`common.Crossing`); W1 the region record (`Data/regions/<id>.json`: the professor, starters, badges, dex, heroes), W2 badges per region, W3 the regional Pokédexes from PokeAPI (`PokemonSpecies.DexNumbers`; "hoenn" is Emerald's 202), W4 what crosses (a fresh journey: the party into the boxes, a starter from the new professor).
- **The reader**: plan 18 · W6 and W7 read pret/pokeemerald's `data/maps/<Map>/map.json`, `layouts.json`, `map.bin` and `metatile_attributes.bin` into the world files Sinnoh's maps are made of (`Data/WorldFiles.cs`: `WorldChunkFile`, `WorldMatrixFile`, `WorldAreaFile` with its `Land` and `Water` tables, `AreaObject`, `AreaWarp`, `AreaSign`, `AreaTrigger`), give every `MB_*` behaviour a `TileBehavior` and the ones Platinum lacks (underwater, the secret bases, the rails) a value of its own in `0xED` to `0xFE` with a row in `TileBehaviors.InGame`, stitch connections into matrices with a per-tile `WorldChunkFile.Areas`, write `dive` connections as warps, and build rooms from layouts (a new `MapSetting.Indoors`). Plan 19 · K1 and K2 build the first region on it (Kanto's rows of the range table in plan 18's new `GbaCover.cs`, the interior table, `FindBuildings` by the doors' destinations); this plan repeats their shape for Hoenn and adds what Hoenn alone has.
- **The field**: `TravelMode` is `OnFoot`, `Surfing` or `Cycling`; `FieldMoves` is Surf, Waterfall, RockClimb and Flash (`FieldMovement.MovesOf`); `FieldMovement.Step` ends in `StepKind` Blocked, Walk, Hop, Land, Climb or Jump (a Bicycle's ramp); `FieldMovement.CanStartSurf` and `Player.StartSurf` begin a surf, which `GameEngine.TryInteract` offers by running `common.Water` (the question, then the script command `surf`), and `SurfMount` draws the one swimmer. Plan 02 · S2 puts the field moves in the party menu (`PartyScreen.ActionsFor`, `common.Use<Move>`) and checks each where it is used (`Overworld/FieldMoveRules.cs`: `FieldMove`, Platinum's fifteen, neither Dive nor Secret Power among them; `Check` on a `FieldSpot`; `BadgeFor`, Platinum's badges, which the obstacles' common scripts ask again with `if badge`); Fly goes to Platinum's twenty spots (`UI/FlyScreen.cs`, `Data/SpawnLocations.cs`) on the map of Sinnoh. Nothing dives: `TileBehavior` is Platinum's (`Sea`, `River`, `Waterfall`, `Bridge`, `ShopShelf` at `0xE5`), `MapSetting` is `Outdoors` or `Cave`, `ArtLook.FieldRig(hour, map)` gives a cave `CaveFieldRig`, a room its indoor rig and everything else the sky's, and `BattleArena` has `Water` but nothing under it. `FieldWeather` already has `Sandstorm` and `Ash`, `Weathers.Of` reads Platinum's `"Sandstorm"` and `"SlowAshfall"`, `WeatherFx` draws both and `AmbienceBed.Sandstorm` sounds: Route 111's desert and Route 113's ash need no new weather, only a place of their own (Emerald sets both by a map script over part of the route, `setweather`, while the map's header says sunny).
- **Secret Power** is a battle move (`BattleCore.Unique.cs`, by `BattleConditions.Terrain`); the field knows nothing of it, and no tile is a hollow to open.
- **Condition and contests**: `Pokemon.Beauty` is the one condition a Pokémon keeps (`SavedPokemonData.Beauty`); plan 06 · R14 brings the five conditions, sheen and Poffins, R17 Platinum's Super Contests (Visual, Dance, Acting), Ball Capsules and ribbons; `moves.json` carries no contest data. Hoenn's contest is a different game on the same condition.
- **The Frontier**: plan 06 · R18 builds the Battle Tower, Factory, Hall, Castle and Arcade with Battle Points and records; plan 14 · B8 to B10 build Emerald's Dome, Palace, Arena, Pike and Pyramid as an annex of Sinnoh's Frontier (its decision 2), with their rules and rooms. Hoenn's island needs only its map and its desks.
- **Species**: every species of Hoenn is hand-built (plan 03's Hoenn batches, 2026-10-06: `Graphics/PokemonModels.Hoenn1.cs`, Treecko to Delcatty, `Hoenn2.cs`, Sableye to Armaldo, `Hoenn3.cs`, Castform to Deoxys), with their forms (Castform's weathers and Deoxys's formes in `PokemonModels.Forms.cs`, the Megas and the Primal Kyogre and Groudon in `PokemonModels.Megas.cs`), and `CheckShapes` already gives Castform its shape in battle (plan 06 · R7).
- **Music**: `Data/music/` has `common`, `kanto` and `sinnoh`; `MusicDirector.Resolve(role, region, exists)` looks in the region's folder first; `MusicRole` has one villain family (`EyeGalactic`, `BattleGalactic`, `BattleGalacticBoss`, `VictoryGalactic`, `VictoryGalacticBoss`); `Data/audio/sound-map.json` gives each trainer class its eye theme by short name (`SoundMap`). Emerald gives its two teams two eye themes, one battle theme for the grunts and admins and one for the leaders (`MUS_ENCOUNTER_AQUA`, `MUS_ENCOUNTER_MAGMA`, `MUS_VS_AQUA_MAGMA`, `MUS_VS_AQUA_MAGMA_LEADER`).
- **Trainers**: Platinum's table is plan 06 · R9's `Data/trainers.json` (927 trainers, written by `tools/DataImporter` from the decompilation's `res/trainers/data` and read by `TrainerDatabase`: the AI flags, the items, the double flag, the team built as the original builds it, the prize money of the class's formula). `TrainerDatabase.Fill` gives a placed trainer its own when its overlay names the same team or none, and plan 08 · P7 (to come) leaves the overlays only the lines. The table holds Sinnoh's alone: a region's trainers are added by a reader (plan 18 · W7) writing the same record shape (`TrainerRecord`, `TrainerPokemonRecord` in `Data/TrainerDatabase.cs`), into a file per region in its world folder as plan 18's decision 8 recommends.
- **The roadmap's order**: the README builds the chain in generation order, Sinnoh first, and plan 19 (Kanto) and 20 (Johto) stand before this one; nothing in Sinnoh waits for anything here. The five systems Emerald alone has (Dive, Secret Bases, the Generation 3 contest, the two bikes, the PokéNav) are built only behind Hoenn's maps, so Platinum's play doesn't move.

## Design

### The source: Emerald

pret/pokeemerald at a pinned commit, the fourth repository of `Sources.cs` (plan 18 · W6 adds the pin and its folder list). Emerald over Ruby and Sapphire because it has both teams, Rayquaza's part, Juan, Wallace as Champion, Steven at Meteor Falls, the Battle Frontier and the Battle Tents. The assets policy holds as for Sinnoh (CLAUDE.md's "Layouts and numbers are imported; art is not", the style guide's "Assets policy"): the reader keeps layouts and numbers (metatiles, behaviours, elevations, where people and warps stand, who a trainer is and what they carry, a map's music and weather by name, the encounter tables, the flags' and variables' names) and never a tile's graphics, a palette, a song or a line of text; the checkout stays in the git-ignored cache.

### Hoenn's world folder

`Data/world/hoenn/` with the layout of `Data/world/sinnoh/`: `world.json` with `"region": "Hoenn"`, the generated `matrices/`, `chunks/`, `areas/` and `habitats.json`, hand-written `overlays/<key>.json`. The reader stitches Emerald's connected maps into one matrix, `Hoenn` (measured in tiles of the whole region, as `Sinnoh` is), one per cave floor and room, and **one for the sea floor**: Emerald's underwater maps under routes (`Underwater_Route105`, `Underwater_Route124` to `Underwater_Route129`, `Underwater_Route134`) reach their routes by `dive` and `emerge` connections and each other by ordinary ones, never a map of the land, so they are stitched into `HoennUnderwater` at the offsets of the routes they lie under, and a tile of the sea floor has the coordinates of the tile of sea above it. The four with no connection at all (`Underwater_SootopolisCity`, `Underwater_SeafloorCavern`, `Underwater_SealedChamber`, `Underwater_MarineCave`) are reached by warps from the route floors and come up where their map script's `setdivewarp` sends them (a map and a tile: numbers the reader keeps), so each is a matrix of its own. The entry says `"setting": "Underwater"`, a new `MapSetting` beside plan 18 · W7's `Indoors`. Area keys are Emerald's map names in lower case (`littleroot_town`, `route_101`, `granite_cave_1f`, `underwater_route_124`), which collide with none of Sinnoh's or Kanto's.

Hoenn's rows of `GbaCover.cs`'s range table (plan 18 · W6's mechanism; Hoenn's two dozen tilesets, written per tileset as each area opens) name its ground: lawn, tall grass, the red earth of Route 111 and its sand, Route 113's ash, the sea, Fortree's canopy, Lavaridge's hot springs, Sootopolis's white stone, Pacifidlog's logs, the sea floor's sand and rock. Buildings are metatiles, so there is no `WorldModels` catalogue: `MapStructures.FindBuildings` finds each block, its kind from the door's destination map or an overlay row, and `BuildingArt.StyleOf` dresses it by town. Hoenn's towns need ways of building of their own, each a row of the style guide's "Buildings" section **before** `Architecture` grows and `BuildingArt.Styles.cs` paints it: Littleroot's and Oldale's cottages, Petalburg, Rustboro's stone offices, Dewford's and Slateport's seaside boards (`Harbour` and `Seaside` may serve), Mauville's yellow stucco, Verdanturf, Fallarbor's ash-roofed huts, Lavaridge's bathhouses, Fortree's tree houses (`BuildingModels` builds a house on a trunk, and Emerald's `MB_FORTREE_BRIDGE` becomes a deck `DeckAt` reads, over the forest floor), Lilycove, Mossdeep, Sootopolis's crater terraces, Pacifidlog's houses on logs (decks over `Sea`), Ever Grande's flowered stone. Each chapter below adds the rows of the towns it opens. Rooms are imported layouts (plan 18 · W7) with an interior table for Hoenn's tilesets, so no room is hand-made. Heights are the elevation nibble, as plan 18 · W6 plates them.

### The region's record

Hoenn's `Data/regions/hoenn.json` (plan 18 · W1): Professor Birch, with our own welcome on Emerald's beats; Treecko, Torchic and Mudkip, the rival taking the one that beats the player's (`StoryState.RivalStarterFor`); the badges Stone, Knuckle, Dynamo, Heat, Balance, Feather, Mind and Rain; the dex "hoenn" (202, from plan 18 · W3's `DexNumbers`), the National Pokédex opened by the Hall of Fame alone (Birch's lab after the credits: a `NationalUnlockRule` of W3's); the music folder `hoenn`; the start in the moving van, then the player's room in Littleroot Town. The two heroes are Brendan and May: the one the player isn't is **the rival**, named by the look, so written lines say `{rival}` (plan 02 · S4's word, which `PlayerIdentity.Fill` fills from `PlayerIdentity.RivalName`, here set from the region's `Heroes`; plan 18's decision 7 keeps the player's own figure) and the introduction asks no rival's name, as Emerald asks none; Wally is a person like any other. Emerald's heal locations are the record's `Spawns` (plan 18 · W1 and W7), so Fly, Teleport and a whiteout in Hoenn land in Hoenn, as plan 19 has Kanto's (plan 02 · S2 keeps the place to come back to, `VAR_SPAWN_LOCATION`; plan 06 · R10 sends the whiteout there). The gates tie the field moves to Hoenn's badges (the record's `Gates`, which plan 02 · S2's check, `FieldMoveRules.BadgeFor`, reads once plan 18 · W2 has made it the region's): Cut to Stone, Flash to Knuckle, Rock Smash to Dynamo, Strength to Heat, Surf to Balance, Fly to Feather, Dive to Mind, Waterfall to Rain.

Hoenn is fought by the rules the game began under (`Ruleset`, plan 06 · R1), Platinum's by default, on Emerald's teams and levels; Emerald's own Generation 3 rules are not a third preset, which H2 records in `docs/mechanics/rulings.md` beside plan 19's ruling. `GameEngine.TerrainAt` reads the tile and then `MapArea.BattleBackground`, which the reader fills from the map's `battle_scene`.

### Dive and the sea floor

- **Rules** (`Overworld/FieldMovement.cs`, GPU-free): a new `TravelMode.Diving` and a new flag `FieldMoves.Dive` (the next free flag after plan 20 · J8's `Whirlpool`), set by `MovesOf` when a party Pokémon knows Dive. Plan 18 · W6's table gives the tiles values of their own, which this plan names: `DiveWater` (Emerald's `MB_DEEP_WATER`, the dark water one dives into; surfable; not to be confused with `Map.IsDeepWater`, which is any water a surfer crosses), `Seaweed` (`MB_SEAWEED`, the sea floor's grass, with encounters), `NoSurfacing` and `SeaweedNoSurfacing` (`MB_NO_SURFACING`, `MB_SEAWEED_NO_SURFACING`). A new `FieldMovement.CanDive(map, x, y, walker)` says yes on `DiveWater` while surfing with the move known, and a new `CanSurface(map, x, y)` says yes under any tile but the two that forbid it; the Mind Badge is the field move's check (`FieldMoveRules.Check` through the region's `Gates`), as the Fen Badge is Surf's today, because the movement rules ask only that a move is known. A step under the sea is a surfing step at surfing's pace, with no running, no bike and no ledges; `TileBehaviors.HasEncounters` counts `Seaweed`.
- **Where it goes**: a dive is from any `DiveWater` tile to the same tile of `HoennUnderwater`, and back, so `WorldAreaFile` gains `Dive` (the key of the area below) and `Emerge` (the area above, or the `setdivewarp` spot of a floor with no connection), which the reader fills from the same `dive` and `emerge` connections plan 18 · W6 writes as warps, leaving its warp list for doors. `GameEngine.TryInteract` reads the tile under a surfer as it reads the water in front today (`FieldMoveRules.SpotOf` gains whether one may dive or come up there) and runs a new `common.Dive` on `DiveWater` or `common.Surface` under the sea: the question, then a new script command `dive` (an `Op`, the parser's and the runner's cases, a method of both hosts, a line in `ScriptTests.EveryCommand`, its row in `docs/scripts.md`), whose `FieldHost` method starts a `TransitionKind.Fade` with the bubbles' sound and lands the player on the other map at the same coordinates in `Diving` or `Surfing`; new `Player.StartDive` and `Surface` set the mode. Dive joins plan 02 · S2's party menu as a `FieldMove` of its own (`FieldMove.Dive`, `PartyScreen.ActionsFor`, a case of `FieldMoveRules.Check`, `common.UseDive`) and asks the same rule; the menu lists it only where the region's `Gates` name it, so a Pokémon that knows Dive in Sinnoh shows the menu it shows today.
- **Wild Pokémon** of the sea floor are the area's `Water` table, which the reader fills from Emerald's underwater `water_mons`: `Map.RollWildEncounter(x, y, steps, water: true)` on a `Seaweed` tile, through `WildEncounterRules` unchanged. Battles under the sea are on `BattleTerrain.Water` with a stage of their own, a new `BattleArena.Underwater`, built by `BattleArenas` and lit by `ArtLook.ArenaRig` (the style guide's "Arenas (G8)" first).
- **The look** (the style guide's "Water" gains "The sea floor" before the code): a new `ArtLook.UnderwaterFieldRig`, one rig whatever the hour as the cave's is (a blue-green light from above, fog close and blue, the post's saturation down); the sea floor is sand and rock cover with kelp (a new `NatureArt.Kelp`, swaying like tall grass through `MeshBuilder.Sway`, in `MeshPass.Ground`); caustics are one more haze layer of `WeatherFx.Haze`'s kind drawn after the composite, timed by `FieldLife.Now`; bubbles rise from the swimmer (a new `FieldLife.Bubbles`, cells of `LifeArt`). The camera stays `FieldCamera.Default`. The surface is never seen from the steep camera, so nothing is drawn for it. One layer and the kelp are weighed with `profile` before and after, under the 8 ms budget on High.
- **Sound**: a new `AmbienceBed.Underwater`, a muffled bed in `Ambience.Entries` with its row in `docs/sound-effects.md` ("Ambience") and `SoundTests`, chosen by `FieldAmbience.Around` on an underwater map; `hoenn/underwater.mml` for Emerald's `MUS_UNDERWATER` role, named by the underwater areas' overlays.

### Secret Bases

- **Finding one**: Secret Power, a `FieldMove` of its own in plan 02 · S2's party menu (listed only in a region whose record names it, as Dive is, so Sinnoh's menu doesn't change), used facing a tile whose behaviour is one of Emerald's `MB_SECRET_BASE_SPOT_*` (plan 18's table gives each kind a value: a cave wall, a tree, a shrub) runs a new `common.SecretBaseSpot`: `if knows "Secret Power"`, the question, then a new command `openbase`, which makes the hollow the player's. Emerald's base layouts (one per spot kind and place, about two dozen) are rooms the reader writes as `Indoors` matrices, so the base is a map like any room, reached by a warp the engine points at the spot.
- **The base** is a new `Models/SecretBase.cs` (GPU-free): the spot (area key and tile), the layout, the decorations placed (`Placed`: a decoration's name, x and y). The save keeps one base, a new `SaveData.SecretBase`, and a store of decorations, a new `SaveData.Decorations` (names and counts; they are not bag items, as Emerald keeps them in the PC): moving to another spot empties the old one, Emerald's rule. The catalogue is a new `Data/decorations.json`, our own file transcribed from Emerald's decoration table (`src/data/decoration/header.h`: name, size in tiles, kind, what it blocks, price, where it is sold: numbers and names, with every picture ours, painted by `PropModels` by kind).
- **Placing**: a new `DecorationScreen` (`UI/`, on the kit, the style guide's "Menu screens (G10)" first: a grid cursor over the room, the store's list, place and take back, in methods without input) is shared with plan 06 · R16's Underground base: whichever lands first builds it, taking a catalogue and a room, and the other uses it. In the room a decoration is a `Prop` of a new `PropType.Decoration` with its `Model` the catalogue's name; `Map.Props` is rebuilt when the base is entered and the scene with it. Other people's bases come only by Emerald's record mixing, which has no place here (decision 4).

### The Contest Hall

Emerald's contest is one hall, Lilycove's, with four ranks (Normal, Super, Hyper, Master), and a round of five appeals: the entrants are first ranked by the condition of the contest's type, then each turn plays a move whose contest effect gives hearts, jams the others, chains a combo, or stirs the crowd. No Visual round and no Dance round. What is shared with plan 06 · R17's Super Contests: the five conditions and sheen (R14), ribbons (R17's), a new `MoveData.Contest` (a move's contest type, shared, written by whichever session is first; R17 adds Platinum's own effect for its Acting round from pokeplatinum, and this plan Emerald's effect, appeal, jam and combos from pokeemerald's `src/data/contest_moves.h` (`gContestMoves` and `gContestEffects`), side by side, because the two games' effects differ), the results board and the entrants' row on the kit. What is Hoenn's own: a new `Models/Contest/AppealRound.cs` (GPU-free: four entrants, their order, each turn's effect, the crowd, the score, every roll from the round's own generator, seeded from `Dice` as a battle's is), the ranks and what each asks (the ribbon of the rank below), the opponents from Emerald's contest tables (`src/data/contest_opponents.h`: names, species, condition, as numbers and names), Pokéblocks (a new `Models/Pokeblocks.cs`: the blender with up to four berries, flavours from the berries' data R14 reads for Poffins, raising the same conditions as Poffins do, and feeding `Pokemon.Beauty` for Feebas), and the hall's rooms. A new `AppealScreen` draws the round (`ModernUi.Appeal.cs`; the style guide's "Menu screens (G10)" first), named apart from plan 15 · E5's `ContestScreen`, which shows the Bug-Catching Contest's results. Winning Master rank with a high score hangs a painting in the Lilycove Museum, a flag and a line.

### The story

Chapters on Emerald's `data/maps/<Map>/scripts.inc` and `data/scripts/*.inc` for what happens and in what order, keeping its flags' and variables' names (`FLAG_BADGE01_GET`, the hide flags of each map's objects, `VAR_LITTLEROOT_INTRO_STATE`) with a `HOENN_` prefix where a name meets Platinum's or Kanto's (plan 19's `KANTO_` rule, and its test that compares the lists), every line in our own words (`docs/scripts.md`; the README's "Writing a scene"). Scripts go in `Data/scripts/<area key>.txt`; what every Hoenn game starts with joins `common.NewGame`; each chapter adds a story walk with `HeadlessScriptHost` (battles won, the flags, items, badges and party checked at the end), and one that changes what a save in play must know raises `StoryState.CurrentVersion` with its step in `StoryMigration`.

The two teams are two sets of people with the same `Script`s where Emerald's grunts share one; Maxie, Archie and their admins are the named cast. The titans are never battled before the post-game, as in Emerald: Groudon and Kyogre wake and leave (the hideout's and the cavern's scenes take them off the map with `hide`), the two clash over Sootopolis and Rayquaza parts them; only the post-game's Marine and Terra Caves and Sky Pillar's top hold them for a `wildbattle ... nofleeing` that may catch them, as Giratina's does in Sinnoh. The field scenes that show them (Groudon waking under Mt. Chimney, Kyogre's submarine, the two in Sootopolis's harbour, Rayquaza's descent) use plan 10 · F1's field sprites where it has landed, and until then the camera, the weather and the text. Tate and Liza are one trainer of two people with the double flag, as Route 204's twins are (`doubleBattle`), and the Space Center a multi battle beside Steven, R9's `battle a and b with c`. Latias or Latios roams after the television at home (Emerald's choice between red and blue, as a question), a roamer of plan 06 · R9's `BattleKind.Roamer` that R13 moves over the map; the Regis come by the Sealed Chamber's riddles, written as our own.

### Trainers, classes and looks

A trainer's team is Emerald's (`src/data/trainers.h` and `trainer_parties.h`, or the text form the pin keeps; plan 18 · W7 reads it), written into Hoenn's own trainer file, `Data/world/hoenn/trainers.json` (plan 18's decision 8), in the record shape of plan 06 · R9's `Data/trainers.json` (`TrainerRecord`: the class, the AI flags, the items, the double flag, the prize money, the team), so `TrainerDatabase.Fill` gives Hoenn's placed trainers their teams as it gives Sinnoh's and an overlay carries their lines alone (plan 08 · P7's rule). Prize money is Emerald's formula: the last Pokémon's level, four times over, by the class's rate (`gTrainerMoneyTable`), twice that in a double battle. Classes Platinum lacks (Kindler, Hex Maniac, Triathlete, Bug Maniac, Expert, Old Couple, Pokémaniac, Sr. and Jr., the Aqua and Magma grunts, admins and leaders) get rows in `sound-map.json` and looks of their own. Plan 11 gives Platinum's classes and cast their looks (C2 to C4 the classes, C5 and C6 the cast) and none of Hoenn's, so each chapter here makes the looks of the classes and the named cast it meets first (Birch, Brendan and May, Wally, Norman, the leaders, Steven, Wallace, the Elite Four, Maxie, Archie, the admins, the Brains), as rows of plan 11 · C1's table built from C2 to C4's parts (the style guide's "Characters" first, the boards of plan 11 · C2's `cast` mode for the user), as plan 19 does Kanto's and plan 20 · J14 Johto's. Until its chapter has made one, a class folds onto the nearest look in `WorldMapBuilder.CharacterFor`.

### Music

A song per role of Emerald's `include/constants/songs.h` (named by each `map.json`'s `music`) in `Data/music/hoenn/`, our own, each file named for its constant as plan 19 names Kanto's: the towns (`littleroot`; `oldale`, which Emerald also plays in Lavaridge; `petalburg`; `rustboro`, also Mauville's and Mossdeep's; `dewford`, `slateport`, `verdanturf`, `fallarbor`, `fortree`; `lilycove`, also Pacifidlog's; `sootopolis`, `ever_grande`), the route families Emerald shares one theme over (`route101` for Routes 101 to 103, `route104`, `route110` with the desert's Route 111, `route113`, `route118`, `route119`, `route120` with Route 121 and the eastern sea, `route122`), the places (`birch_lab`, `gym`, `petalburg_woods`, which Granite Cave, Rusturf Tunnel, Fiery Path and Jagged Pass share; `mt_chimney`, which Mirage Tower, the Seafloor Cavern and Sky Pillar share; `cave_of_origin` with Meteor Falls; `mt_pyre`, `mt_pyre_exterior`, `aqua_magma_hideout`, `abandoned_ship`, `sealed_chamber`, `trick_house`, `victory_road`), `underwater`, `cable_car`, `safari_zone`, the hall (`contest`, `contest_lobby`, `contest_results`), the Frontier (`b_frontier` and its halls'), the scenes (`awaken_legend`, `abnormal_weather`, `rayquaza_appears`, `hall_of_fame`, `credits`), and the roles of `MusicDirector.FileName` Emerald has a song for, by their file names (`battle_trainer`, `battle_gym`, `battle_rival`, `battle_elite_four`, `battle_champion`, the victories, `eye_boy`, `eye_girl`, `bicycle` for `MUS_CYCLING`, `surf`). The two teams take the villains' roles under Sinnoh's names, as plan 19 gives them to Team Rocket: `BattleGalactic` is Emerald's one battle theme for grunts and admins of both teams, `BattleGalacticBoss` Maxie's and Archie's. Emerald's two eye themes need one small change: a class's row in `sound-map.json` may name a song file of the region's folder (`"Team Aqua Grunt": "eye_aqua"`) in place of a role's short name, resolved before the role's file, so `hoenn/eye_aqua.mml` and `hoenn/eye_magma.mml` both exist and `MusicRole` grows nothing. Kyogre, Groudon, Rayquaza, the Regis and the eon pair already have battle themes of their own (`Data/music/common/legendary/`, plan 05 · A6). Every area theme gets a night arrangement though Emerald had no night, as Kanto's do. Checked as A6 checks: `tools/MusicRender`, the levels, loudness and clash report, the user listening.

### The crossings

In by ferry from Olivine (plan 20 · J12's departure) to Slateport's harbour, confirming plan 18 · W4's row; out by the S.S. Tidal from Lilycove's harbour, Emerald's own berth, where the Frontier's ferry also sails (decision 2 amends W4's row from Slateport): one harbour room, one attendant, two destinations, `common.Attendant` and `common.Crossing` as they are. `Region.Arrival` is a `MapSpot` on Slateport's pier; `RegionLink.DepartureMap` the Lilycove harbour room; `Transport.Boat` with `Vessel` "ferry" and "ship". `RegionTests` holds both links once plan 20 names Olivine.

### The Battle Frontier

The island off Lilycove is a map of its own (Emerald's `BattleFrontier_OutsideWest` and `East` stitched), reached by the ferry, with its seven buildings' rooms from Emerald's layouts. The Tower and Factory play plan 06 · R18's rules (Platinum's numbers, decision 3); the Dome, Palace, Arena, Pike and Pyramid plan 14 · B8 to B10's, which this plan does not repeat: each desk's script opens the same screen the annex's does, and the Battle Points, records and symbols are R18's. The Brains (Anabel, Tucker, Spenser, Greta, Noland, Lucy, Brandon) take Emerald's teams from its Frontier tables (numbers) and looks of their own, made in H12 as rows of plan 11's table. The three Battle Tents of the story (Verdanturf's the Palace's rules, Fallarbor's the Arena's, Slateport's the Factory's, three bouts each) point at the same rules.

### Tests and shots

`HoennTests` is to Hoenn what `SouthWestTests` and `WorldTests` are to Sinnoh and plan 19's `KantoTests` to Kanto: the index says what is built, every chunk in view has its file, every overlay entry points at something, every warp lands on open ground and leads back, people stand where they can be spoken to, wild Pokémon are the area's, and `WorldWalk.From` Hoenn's start reaches every open area through warps, doors, item balls and dive spots (`WorldWalk` learns that a `DiveWater` tile reaches the tile below). The harness gets a `hoenn` mode, not part of `all` (its people would move every battle of the modes after it), with its line in CLAUDE.md's commands: it starts a new game in Hoenn, shoots each open town, route, cave, room and stretch of sea floor by day and after dark (`h01_littleroot_day`, …), walks from Littleroot Town to Rustboro City at running pace timing every frame and printing what streaming cost, and shows the chapters' scenes (`hs01_the_van`, …) through the game's own `TryInteract`, `Steering` and the question box's buttons. Two runs draw the same pictures: whatever is new takes its chance from `Dice` and its time from `FrameClock`, a contest round rolls on its own generator seeded from `Dice`, and a change is judged by `diff` of a `hoenn` run against a `hoenn` run.

## Sessions

### H1 · The import and Littleroot Town
- Emerald's pin in `Sources.cs` (if plan 18 · W6 hasn't set it); `--game emerald` into `tools/MapImporter/out/hoenn/` with the mosaic and a picture per area, and `--game emerald --data` writing `Data/world/hoenn/` by its `world.json`; `world.json` with `Hoenn`, `HoennUnderwater` (empty of areas for now) and `littleroot_town`, `route_101` and `oldale_town` open; Hoenn's first rows of `GbaCover.cs`'s range table, for their ground; `Architecture`'s first Hoenn rows, Littleroot's and Oldale's cottages (the style guide's "Buildings" first); the overlays; Birch's lab and the houses as imported rooms, with the first rows of Hoenn's interior table; `Data/regions/hoenn.json`; Hoenn's `Start` and `Maps` in `RegionDatabase`; `--region Hoenn` begins in the van (the introduction's last beat) and the player's room; `MapSetting.Underwater` as a value only.
- Music: `littleroot`, `route101`, `oldale`, `birch_lab`.
- Tests: `HoennTests` (the index, the chunks, the overlays, the walk from the start); the cover rows on a layout built in the test; `RegionTests` with Hoenn built.
- Harness: the `hoenn` mode (its line in CLAUDE.md's commands) with `h01`–`h06` (Littleroot, Route 101 and Oldale by day and after dark) and the timed walk.
- **Done when** a new game begun with `--region Hoenn` stands in Littleroot Town on the imported map, walks Route 101 among its wild Pokémon, and the frame on High is under 8 ms.

### H2 · The van to Rustboro
- The introduction as Birch; a new game as plan 02 · S4 left Sinnoh's (no free Pokémon or items: `GameEngine.InitializeNewGame` gives them only to a region whose story isn't written); the van, Mom and the clock, Birch on Route 101 and the starter, the rival on Route 103, the Pokédex and the Running Shoes on Emerald's beats (setting the flags the start menu and running read since S4, `FLAG_HAS_POKEDEX` and `FLAG_HAS_RUNNING_SHOES`), Petalburg and Norman, Wally's catch (plan 06 · R9's catching lesson, `catchinglesson`, given the catcher's Pokémon: Wally with Norman's Zigzagoon in place of the assistant's starter), Route 104, Petalburg Woods and the first grunt, Rustboro, Roxanne and the Stone Badge (`FLAG_BADGE01_GET`), Rusturf Tunnel and the Devon goods, the PokéNav from Mr. Stone (a new `PokeNavScreen` on the kit, the style guide's "Menu screens (G10)" first: the map page on plan 20 · J3's `ModernUi.RegionMap`, Match Call on J3's contacts and rematch rule, `PhoneContact` and `Op.AddContact`, written here if J3 hasn't landed; Condition and Ribbons as R14 and R17 land). Hoenn's badges and gates in the record; the ruling on Platinum's rules in Hoenn; `Architecture` rows for Petalburg and Rustboro (the style guide's "Buildings" first); the looks of Birch, the rival's two figures, Wally, Norman, Roxanne and the chapter's classes ("Trainers, classes and looks").
- Music: `petalburg`, `route104`, `petalburg_woods`, `rustboro`, `gym`, `battle_gym`, `battle_rival`, `battle_trainer`, `battle_galactic` (the first grunt's), `eye_boy`, `eye_girl`, `eye_aqua`, the victories; the sound map's file names.
- Tests: the chapter's story walk (a starter chosen, Wally's Ralts caught in the headless host, the Stone Badge won, Cut's gate open); the sound map's file lookup; `StoryTests` on Hoenn's files.
- Harness: `hs01`–`hs08` (the van, Birch's bag, the choice, the first battle, Wally's catch, Roxanne's hall, the PokéNav's map page); `h07`–`h14`.
- **Done when** the chapter plays from NEW GAME to Rusturf Tunnel with every scene, and a plain NEW GAME still begins with Rowan in Sinnoh (plan 18 · W10).

### H3 · Dewford, Slateport and Mauville
- Mr. Briney's boat as a scene (a walk by sea, the camera on the boat), Dewford, Brawly and the Knuckle Badge, Granite Cave and Steven's letter, Slateport (the market, the museum, Captain Stern and Team Aqua, the Battle Tent on plan 06 · R18's Factory rules, three bouts), Route 117, Verdanturf and its Battle Tent (the Palace's rules, plan 14 · B9) with Rusturf Tunnel opened from its far side, Route 110 and the Trick House's first puzzles (each a script with its own `StepTrigger`s), the Cycling Road, Mauville, Wattson and the Dynamo Badge, Rydel's two bikes (a new `BikeKind { Mach, Acro }` on `Walker`: the Mach Bike's pace and the muddy slopes it alone climbs, the Acro Bike's hop that crosses a rail's gap and the ledges it alone takes, the rails as plan 18's values; one item swapped at the shop; each rider drawn as the Bicycle's is, the Acro Bike's hop as a ledge's; Platinum's Bicycle unchanged in Sinnoh), New Mauville's door shut, the Game Corner (plan 06 · R16's slots; roulette optional); `Architecture` rows for Dewford, Slateport (`Harbour` and `Seaside` may serve), Mauville and Verdanturf (the style guide's "Buildings" first); the chapter's looks.
- Music: `dewford`, `slateport`, `route110`, `verdanturf`, `trick_house`, `bicycle`.
- Tests: the two bikes' rules in `FieldMovementTests` (a slope refused on foot and on the Acro, a rail refused on the Mach, Platinum's Bicycle on a Sinnoh map stepping as before); the chapter's walk (Knuckle, Dynamo, the goods delivered).
- Harness: `h15`–`h26`; `hs09`–`hs14` (the boat, Brawly, the museum, the Trick House, Rydel's).
- **Done when** the chapter plays to Mauville and both bikes are ridden in the harness on their own ground.

### H4 · The desert, Mt. Chimney and Lavaridge
- Route 111's desert in its sandstorm (the desert's tiles an area of their own in the overlay, through the reader's per-tile `Areas`, with `FieldWeather.Sandstorm`, where Emerald's `setweather` lines set it by the player's column; Go-Goggles as the gate), Mirage Tower and its fossils, Route 112 and the cable car (a scene), Fiery Path, Route 113's ash (`Ash`, its stretch made an area the same way; the glass workshop), Fallarbor and its Battle Tent (the Arena's rules, plan 14 · B9), Meteor Falls and Team Magma, Mt. Chimney's Maxie and the meteorite, Jagged Pass, Lavaridge, Flannery and the Heat Badge, Petalburg again, Norman and the Balance Badge. The roll for a Pokémon behind a smashed rock (Emerald's `rock_smash_mons`, first on Route 111, which plan 18 · W7 writes as the area file's `RockSmash` and leaves to the first region that opens such a rock), unless an earlier region's plan or plan 06 · R13 has written it. `Architecture` rows for Fallarbor and Lavaridge (the style guide's "Buildings" first); the chapter's looks.
- Music: `route113`, `fallarbor`, `mt_chimney`, `cave_of_origin`, `cable_car`, `eye_magma`, `battle_galactic_boss` and `victory_galactic_boss` in their Hoenn versions.
- Tests: the chapter's walk (Heat, Balance, Surf's gate open); the goggles' gate; the sandstorm's weather on the desert and clear beside it; a smashed rock on Route 111 rolls from its table, and Platinum's rocks still hide nothing.
- Harness: `h27`–`h38` (the desert in its storm, the ash by day and night); `hs15`–`hs20`.
- **Done when** Norman is beaten in the harness and the desert's storm renders within budget.

### H5 · Dive and the sea floor
- `TravelMode.Diving`, `FieldMoves.Dive`, the party menu's Dive (`FieldMove.Dive`, `common.UseDive`), the four tile values, `CanDive` and `CanSurface`, `WorldAreaFile.Dive` and `Emerge`, `common.Dive` and `common.Surface` from `TryInteract` with the `dive` command (an `Op`, both hosts, `ScriptTests.EveryCommand`, `docs/scripts.md`), `Player.StartDive` and `Surface`, the encounters on `Seaweed`; the look (the style guide's "Water" gains "The sea floor" first: `UnderwaterFieldRig`, `NatureArt.Kelp`, the caustic layer, `FieldLife.Bubbles`), `BattleArena.Underwater` and its stage (the style guide's "Arenas (G8)" first), `AmbienceBed.Underwater`, the bubbles' sound; `underwater_route_124` open with Route 124's sea above it as the first pair; `docs/mechanics/rulings.md` says Dive follows Emerald where Platinum has nothing.
- Tests: `FieldMovementTests` (a dive refused on `Sea` and without the move; a surfacing refused under `NoSurfacing`); `FieldMoveTests` (Dive refused without the Mind Badge, and Dive and Secret Power absent from a Sinnoh party's menu); a dive lands on the same coordinates; `TileBehaviors.InGame` has the rows; `WorldWalk` reaches the sea floor; `FieldLifeTests` for the bubbles; `AmbienceTests` for the bed.
- Harness: `h40_route124_surface`, `h41_dive_down`, `h42_sea_floor_day`, `h43_sea_floor_night`, `h44_seaweed_battle`; `profile` before and after.
- **Done when** the player dives from Route 124, walks the sea floor among Clamperl and comes up where the rule allows, and the frame on High is under 8 ms.

### H6 · Route 118 to Fortree
- Route 118 and Steven, Route 119's rain (Emerald's `SetRoute119Weather`, a script's weather and not the header's, given the area by its overlay) and the Weather Institute (Team Aqua, Castform given, its weathers sculpted already), Fortree in its trees (its `Architecture` row for the tree houses, the style guide's "Buildings" first; `MB_FORTREE_BRIDGE` as decks), Winona and the Feather Badge (Fly from the overworld through plan 02 · S2's Fly, over the record's `Spawns` and with Hoenn's map in `FlyScreen`, drawn on plan 20 · J3's `ModernUi.RegionMap`), Routes 120 and 121, Kecleon and the Devon Scope, the Safari Zone on plan 06 · R9's Safari battle (`BattleKind.Safari`) with Hoenn's tables, its balls and steps counted by plan 01 · M7's Great Marsh stay (`SafariGame`, `safari start`, `safari end`, `if safari`); the chapter's looks.
- Music: `route118`, `route119`, `fortree`, `route120`, `safari_zone`.
- Tests: the chapter's walk (Feather, Castform in the party); a Fortree bridge is a deck over the floor; the Safari's tables are the area's.
- Harness: `h45`–`h54` (Route 119 in the rain by day and night, Fortree, the Institute); `hs21`–`hs25`.
- **Done when** Winona is beaten and Fortree's bridges are walked and drawn over the forest floor.

### H7 · Lilycove, Mt. Pyre and the hideouts
- Lilycove (the department store, the harbour's shut door, the museum; its `Architecture` row, the style guide's "Buildings" first), Team Aqua at the Cove, Mt. Pyre and the orbs, the Magma hideout under Jagged Pass and Groudon waking (a scene with no battle, as Emerald has it: the orb, the camera shaken, Groudon taken off the map), the Aqua hideout and the submarine, Route 122 and 123; the chapter's looks.
- Music: `lilycove`, `route122`, `mt_pyre`, `mt_pyre_exterior`, `aqua_magma_hideout`, `awaken_legend`.
- Tests: the chapter's walk (both orbs' flags, `FLAG_GROUDON_AWAKENED_MAGMA_HIDEOUT` set, Groudon's person hidden afterwards).
- Harness: `h55`–`h64`; `hs26`–`hs31` (Mt. Pyre's summit, Groudon's waking).
- **Done when** the Aqua hideout's last scene plays and the way to Mossdeep opens.

### H8 · Mossdeep, the Seafloor Cavern and Sootopolis
- Routes 125 to 128 by sea with their sea floors open (124 is H5's), Mossdeep, Tate and Liza as one trainer of two (R9's `doubleBattle`) and the Mind Badge, the Space Center and the multi battle beside Steven (R9's `battle a and b with c`), Shoal Cave's tide (two layouts of one room, chosen by `GameClock.Hour` as Emerald's `setmaplayoutindex` chooses them, through a new script command run behind the warp's fade: an `Op`, both hosts, `ScriptTests.EveryCommand`, `docs/scripts.md`), the Seafloor Cavern by Dive and Kyogre's waking (a scene, as Groudon's), Sootopolis reached by surfacing in its crater, the two titans in the harbour under Emerald's alternating downpour and drought (`HeavyRain`, and a new `FieldWeather.Drought` with its light in `ArtLook.Weathered`, the style guide's "Life" first), Wallace, the Cave of Origin, Routes 129 to 131 and Pacifidlog (houses on logs, decks over `Sea`), Sky Pillar and Rayquaza's descent, Juan and the Rain Badge; `Architecture` rows for Mossdeep, Sootopolis's crater terraces and Pacifidlog (the style guide's "Buildings" first); the chapter's looks.
- Music: `sootopolis`, `abnormal_weather`, `rayquaza_appears`.
- Tests: the chapter's walk (Mind, Rain, the titans' flags, Rayquaza's); the tide's layout by the hour; the clash's weather alternating and clearing once Rayquaza has come; the double and the multi battle through the headless host.
- Harness: `h65`–`h78` (the sea by day and night, Sootopolis from the water, Sky Pillar); `hs32`–`hs40`.
- **Done when** Juan is beaten and Sootopolis's scene plays with the weather it calls for.

### H9 · Ever Grande and the League
- Route 128 to Ever Grande by Waterfall, Victory Road (Strength and Rock Smash), Wally's last battle, Sidney, Phoebe, Glacia and Drake, Wallace, the Hall of Fame and the credits, home again, the National Pokédex from Birch, the television and the eon choice (a roamer, R13), `HoennHallOfFame` set, the harbour's ship to Sinnoh as plan 18 · W4 and W5 play it; Ever Grande's `Architecture` row (the style guide's "Buildings" first); the League's looks; Hoenn's leaders and Champion as a section of plan 14 · B11's `Data/tournaments.json` if B11 has made it, for B12 to read.
- Music: `ever_grande`, `victory_road`, `battle_elite_four`, `battle_champion`, the League's victories, `hall_of_fame`, `credits`.
- Tests: the chapter's walk ends with the flag set, the National Pokédex open and `CheckTravel` ready for Sinnoh.
- Harness: `h79`–`h86`; `hs41`–`hs46` (the four rooms, Wallace's hall, the Hall of Fame).
- **Done when** a game played from `--region Hoenn` reaches the Hall of Fame in the headless walk and the attendant at Lilycove offers Sinnoh.

### H10 · Secret Bases
- The spot values, Secret Power's `FieldMove` in the party menu (listed only in Hoenn), `common.SecretBaseSpot` and `openbase` (an `Op`, a case in both hosts, a line in `ScriptTests.EveryCommand`, its row in `docs/scripts.md`), the base layouts as rooms, `Models/SecretBase.cs`, `SaveData.SecretBase` and `Decorations`, `Data/decorations.json`, `PropType.Decoration` and `PropModels`' painters (the style guide's "Props" first), `DecorationScreen` (or R16's; the style guide's "Menu screens (G10)" first), the sellers at Slateport's market and Lilycove's store, the base's PC and the registering desk.
- Tests: a base is found only at a spot tile with the move; the layout matches the spot's kind; a decoration can't go on a blocked tile or over another; a second spot empties the first; the save round-trips base and store; `WorldWalk` leaves a base through its door.
- Harness: `h90_secret_power_tree`, `h91_base_empty`, `h92_base_decorated`, `h93_decoration_screen`.
- **Done when** a base is opened, furnished and found again from a loaded save in the harness.

### H11 · The Contest Hall and Pokéblocks
- `MoveData.Contest` with Emerald's effects (its type shared with R17's), `Models/Contest/AppealRound.cs`, the ranks and the opponents' table, `Models/Pokeblocks.cs` and the blender's room, `AppealScreen` and `ModernUi.Appeal.cs` (the style guide's "Menu screens (G10)" first), Lilycove's hall rooms and desks, the ribbons through R17's record, the museum's painting; `docs/mechanics/rulings.md` says which of the two contests each region plays.
- Tests: a seeded round gives the same scores twice; the order by condition; a combo, a jam and a crowd's burst each worth what Emerald's numbers say; a rank refused without the ribbon below; a Pokéblock raises the condition its flavours name and Feebas's beauty with it.
- Harness: `h94_contest_lobby`, `h95_appeal_round`, `h96_results`, `h97_blender`.
- **Done when** a Master rank is won in the harness by the same round on a second run.

### H12 · The Frontier island and the post-game
- The island's map and its seven halls with their desks (plan 06 · R18's Tower and Factory, plan 14 · B8 to B10's Dome, Palace, Arena, Pike and Pyramid), its buildings' style (the style guide's "Buildings" first), the Brains and their looks, the Battle Points shop; the S.S. Tidal's cabins as a scene; the Weather Institute's reports and the Marine and Terra Caves (Kyogre and Groudon catchable), Sky Pillar's top (Rayquaza; its floors cracked since the descent, crossed only at the Mach Bike's pace, `MB_CRACKED_FLOOR`), the Sealed Chamber and the three Regis, Steven at Meteor Falls, the Desert Underpass, New Mauville, Altering Cave, Trainer Hill (optional), the event islands under decision 5.
- Tests: every desk opens a facility that exists; the post-game's walk (the Regis' doors, the caves' Pokémon, Steven's rematch); `RegionTests` with the island in Hoenn's `Maps`.
- Harness: `h98`–`h110` (the island, each hall's desk, Sky Pillar's top, the two caves); `hs47`–`hs52`.
- **Done when** every facility is entered from the island and the Regis are caught in the headless walk.

## Risks

- **Order.** Plan 18 · W6 and W7 and plan 19 · K1 and K2 must land first: the reader, the cover mechanism, the interior table and `FindBuildings`' kinds are theirs, and repeating them here would fork the importer. H1 waits; nothing of Sinnoh's waits for H1.
- **The sea floor's offsets.** A dive lands on the same coordinates, so the reader must place each underwater map exactly under its route; an offset wrong by a tile puts the player in rock. `HoennTests` holds every `DiveWater` tile of an open area over an open tile of the floor, every floor tile's emerge under water, and each of the four floors with no connection coming up on open ground at its `setdivewarp` spot.
- **What plan 06 · R9 left**: R9 is done, and Tate and Liza's double, the Space Center's multi battle, a Pokémon that flees and the Safari battle are in, and since plan 01 · M7 the Safari Zone's stay is too (the Great Marsh's `SafariGame`, its balls and steps). Still to come: roamers moving over a map (R13), which Latias or Latios needs in H9. H9 can be written before R13 lands with the beat marked, as plan 02's chapters do for their mechanics, but not finished.
- **Two sessions on one field.** `MoveData.Contest` and `DecorationScreen` are shared with plan 06 · R17 and R16: whichever is first builds it, and the second reads it. A session that starts without checking the other's status writes it twice.
- **Frame cost under the sea and in Fortree**: one haze layer over 4K, kelp as sway geometry, and a forest with houses in it; H5 and H6 run `profile` before and after and keep the preset's budget.
- **Platinum's defaults.** Nothing here runs on a Sinnoh map: `Diving` is never entered without a `DiveWater` tile, the new behaviour values sit above Platinum's, the party menu lists Dive and Secret Power only in Hoenn, Platinum's Bicycle keeps its rules, the rules are the game's `Ruleset`, and `all` diffs empty against the run before each session.
- **The named cast's looks** are this plan's to make, in plan 11's way, chapter by chapter; until a chapter makes one, a class or a person wears a stand-in, which the shots will show.

## Needs and gives

- **Needs** plan 18 (W1 to W5 for the record, badges, dex, crossing; W6 and W7 for the reader); plan 19 · K1 and K2 (the first GBA region's patterns) and plan 20 · J12 for Olivine's departure (H1 to H12 run with `--region Hoenn` without it), J3 for `ModernUi.RegionMap` and the phone's contacts the PokéNav's pages stand on (H2 writes them if J3 hasn't landed); plan 02 · S2 (done: the party menu's field moves, the badge gates, Fly and the spawn locations, the bike as an item, the rods; Hoenn adds its `Gates` and `Spawns`, Dive and Secret Power); plan 01 · M7 (done: the Great Marsh's `SafariGame`, H6's Safari Zone's stay); plan 06 · R9 (done: doubles, multi, scripted wild battles, the Safari battle, the trainer table's record shape), R10 (done: obedience, read by the region's badges once plan 18 · W2 is in), R11 (TMs and the bag), R12 (rematches for Match Call), R13 (roamers moving over the map; the rods fish since S2; the Rock Smash roll is H4's unless R13 or an earlier region has written it), R14 (condition, berries, the clock), R16 and R17 (the shared screen and field above, ribbons), R18 (Battle Points, the Tower and Factory); plan 14 · B8 to B10 (the five facilities) and B11 (the tournament file H9 adds to); plan 08 · P7 (overlays that carry a trainer's lines alone; P6's table is R9's); plan 11 · C1 to C4 (the looks' table and parts, C2's `cast` mode), C5 and C6 for the way a named look is made; plan 10 · F1 (the titans in the field); plan 03's Hoenn batches (done: every species of Hoenn hand-built, with its forms); plan 05 · A6's way for the songs.
- **Gives** plan 20 Hoenn's arrival for Olivine's ferry and the harbour's shape; plan 14 · B12 Hoenn's leaders' table; plan 06 · R16 and R17 the shared screen and field if H10 or H11 is first; plan 13 · V11 a fourth built region to play again; plan 22 the shape of a region's tests and harness mode (`HoennTests`, the `hoenn` mode), which its own follow; plan 23 · Z5 the first `TravelMode` added beside Platinum's three, a precedent for the Ride Pokémon; plan 07 · O8 a base worth carrying to a friend if decision 4's alternative is taken.

## Decisions for the user

1. **The source.** *Recommended:* Emerald (both teams, Rayquaza, the Frontier, Juan and Wallace as Champion). The alternative is Ruby and Sapphire, one team at a time from two games, which the reader can take but the plan would write twice.
2. **The harbours.** *Recommended:* in at Slateport (plan 18 · W4's row), out from Lilycove, where the S.S. Tidal and the Frontier's ferry both sail from one room. The alternative keeps W4's table as written, Slateport both ways, with the Frontier's ferry a second attendant there.
3. **The Tower and Factory's numbers.** *Recommended:* plan 06 · R18's rules, Platinum's numbers, in Hoenn as in Sinnoh, one implementation. The alternative transcribes Emerald's Generation 3 streaks and rentals as a second set behind the region.
4. **Secret Bases.** *Recommended:* one base of the player's own, furnished and visited, no visitors. The alternative carries a friend's base over plan 07's server, which is O8's kind of work and not this plan's.
5. **The event Pokémon** (Mew on Faraway Island, Deoxys on Birth Island, Lugia and Ho-Oh at Navel Rock). *Recommended:* the three tickets given by people of our own after the Frontier's first symbols, so the islands exist in the game. The alternative leaves the islands out, as a cartridge without the events had them.
6. **The two bikes.** *Recommended:* both, swapped at Rydel's as Emerald has it, each with its own ground. The alternative is one bike with both abilities, which removes the swap and the two routes' puzzles with it.

## Status

- [x] H1 The import and Littleroot Town (done 2026-10-18, wave 29)
  - **Open**: `Data/world/hoenn` (`--game emerald --data`) opens the overworld `Hoenn` with Littleroot Town, Route 101, Oldale Town, Routes 102 and 103 and Petalburg City, and sixteen rooms built from their layouts (the two families' houses on both floors, Birch's lab, Oldale's two houses, Center on both floors and Mart, Petalburg's three houses, Center on both floors and Mart), 22 areas walked from the start. Petalburg's Gym (a row of rooms joined by the doors its Leader's scenes open) stays shut (`locked`) for H2; Route 103's way into Altering Cave leads nowhere; the trainers across Route 103's water are reached by Surf. More than the plan's three areas were opened because the caller asked for Routes 102 and 103 and Petalburg too.
  - **The record**: `Data/regions/hoenn.json` (Birch with his welcome in our words and a Lotad, Treecko, Torchic and Mudkip, the eight badges, Emerald's gates but Dive's, the dex `hoenn` of 202, the National Pokédex by the Hall of Fame, `rival` left out since the rival is the other hero, three spawns: home, Oldale's and Petalburg's Centers); `RegionDatabase`'s Hoenn has `Start` and `Maps`, so Johto's way on to Hoenn reads Ready once Johto's Hall of Fame is entered. `hasStory` is false: a new game with `--region Hoenn` stands before the west family house in Littleroot Town (Emerald's white-out spot, 125, 291) with a Treecko and a few items, as Kanto's K1 did. The moving van (whose warps Emerald points with `MAP_DYNAMIC`) and the player's room as the start are H2's, with the story. Mom heals in the west house for either look (moved a tile west of Emerald's place so a white-out wakes beside her on the rug, not on the table).
  - **The importer**: Emerald's trainers carry `hoenn_` and Emerald's own prize money (`GbaTables.EmeraldMoneyRates`, transcribed from `src/battle_main.c`'s `gTrainerMoneyTable` at the pinned commit; personalities are made as FireRed's, `CreateNPCTrainerParty` being the same), through `GbaImport.Tables`. FireRed and Emerald share tilesets' names (`gTileset_General`, `gTileset_Building`, `gTileset_PokemonCenter`) and not their metatiles, so Emerald has its own rows: `GbaCover.HoennRanges` (asked before a label, since Emerald's labels call a lawn under a tree `Grass_TreeLeft` and a mountain's foot `RockWall_…`) and `GbaFurniture.HoennRanges`, with `MB_SHOP_SHELF` and `MB_QUESTIONNAIRE` in the behaviour table; until now FireRed's rows had been read on Emerald's maps. Emerald's obstacles' looks (`cuttable_tree`, `breakable_rock`, `pushable_boulder`) are `ObstacleFor`'s. `MapSetting.Underwater` is a value only; the reader keeps Emerald's underwater maps as matrices of their own (not yet `HoennUnderwater` at the routes' offsets), H5's to stitch.
  - **The look**: `Architecture.Sapling` (Littleroot), `Meadow` (Oldale) and `Parkside` (Petalburg), style guide rows first; Emerald's looks fold onto ours in `characters.json`'s fallbacks until H2 makes Birch, the rival's two figures and the cast.
  - **Music**: `hoenn/littleroot`, `route101` (Routes 101 to 103), `oldale`, `birch_lab` and `petalburg`, our own, through `tools/MusicRender` by day and night: no clashes, loudness −18.0 to −20.5 LUFS; not yet heard by the user.
  - **Tests**: `HoennTests` (11: the index, keys apart from other regions, the record, the start, the walk to every area and room with a floor of tiles, ways out and back, rooms of their kind and furnished, Mom and the nurses, Route 101's Pokémon, the trainers' prefix and prize, Emerald's rows apart from FireRed's); `RegionTests`, `RegionRecordTests`, `RegionBadgeTests` and `PokedexTests` take Unova as the region with no file.
  - **Shots**: the `hoenn` mode's `h00` to `h18` (with `h09b` and `h09c` round Petalburg's Gym), looked at; the walk from the family's door to Petalburg's Center, 145 tiles, no chunk waited for (one frame of 50.8 ms at the Center's door as a chunk was made ready); the four towns and routes timed at 5.6 to 6.9 ms a frame (an earlier run's 10.3 ms for Oldale was the machine). Fixed from the look: a door kept shut (`locked`) is still its building's door where houses are found at their doors (`Map.BuildingKinds` notes its kind by the area it would lead into, `WorldMapBuilder.KindOfArea`), or Petalburg's Gym stood as grey rock; the house's three televisions were one.
  - **Left for H2 and after**: the story (the van, the bedroom start, Birch on Route 101, the starters, the rival, Wally, Norman), the triggers north of Littleroot and Oldale's west road left without scripts, Petalburg's Gym, Birch's and the cast's looks, the cover rows of the rest of Emerald's tilesets (`report.md`), the berry trees of Routes 102 and 103 (they need Hoenn's berry patches, which must not share Sinnoh's numbers), `HoennUnderwater`; the user's level-curve choice is pending, so Emerald's levels stand.
- [x] H2 The van to Rustboro (part a done 2026-10-18, wave 30: the van to Wally's catch; part b done 2026-10-18, wave 31: Route 104 to Rusturf Tunnel and the PokéNav)
  - **Played**: a new game with `--region Hoenn` steps out of the moving van beside the west house with nothing
    (`hasStory`, `common.HoennNewGame`), the ride told over the dark as the town's arrival; Mom and the movers'
    Vigoroth, the clock (a line, the computer's own), Dad's Gym on the television, the professor's wife next door and
    the rival at the Poké Ball upstairs, the little girl at the top of the road, Birch chased by a Zigzagoon on Route 101
    and his bag (`starter`, the first battle), his thanks in the lab, the rival's battle on Route 103 (Emerald's team for
    the player's starter), the rival running on through Oldale, the Pokédex and five Poké Balls, Mom's Running Shoes,
    Oldale's footprints man and the clerk's Potion, Petalburg's boy, Norman in his Gym's hall, Wally's catch
    (`catchinglesson ... by "Wally" "Zigzagoon" 7`, new) and Norman's rule of four Badges; every scene a `scene`. Story
    version 23 (`HoennChapterOne`: an H1 save is taken past the opening by `common.HoennOpeningDone`).
  - **The rival** is the hero the player isn't, May for the boy and Brendan for the girl (`hoenn.json`'s `rivals`,
    `Region.RivalFor`, the introduction's offer), drawn from an overlay's `Rival:MAY:BRENDAN`
    (`PlayerIdentity.CharacterFor`); the family lives in the west house for both looks (rulings, "Hoenn's first
    chapter").
  - **Opened**: Petalburg's Gym (`PetalburgCity_Gym`), its hall alone, every way on `locked` until H4 (its rooms opened
    by H4's part b, their doors read).
  - **Looks**: `prof_birch` (the introduction's professor too), `may`, `brendan`, `wally`, `norman`; the `Rival` class
    in battle picks May's or Brendan's.
  - **Music** (ours, rendered clash-free, −18.4 to −20.3 LUFS, not yet heard): `gym`, `rival`, `help`, `battle_wild`,
    `battle_trainer`, `battle_rival`, `victory_wild`, `victory_trainer`, `eye_boy`, `eye_girl`.
  - **Tests**: `HoennStoryTests` (the chapter four ways, by look and starter; the rival's lost battle; Oldale's road;
    the new game's flags and the rival by look; a Champion never put in the van; the songs; an H1 save upgraded);
    `ScriptTests` for the lesson's catcher. Harness: the `hoenn` mode ends with `hs01`–`hs23`.
  - **Left for H2, part b**: Route 104, Petalburg Woods and its grunt, Rustboro City (its `Architecture` row), Roxanne
    and the Stone Badge, Rusturf Tunnel and the Devon Goods, the PokéNav, Roxanne's and the classes' looks, `eye_aqua`
    and `battle_gym`, `petalburg_woods`, `route104`, `rustboro`; still from H1: the cover rows of the rest of Emerald's
    tilesets, Hoenn's berry trees (patch numbers of their own above Sinnoh's 128), the triggers north of Oldale.
    Seen in the shots and left: the Gym's hall is drawn small at the corner of a camera that frames the whole long
    building over the dark (a room 112 tiles deep; its side walls had to be laid in lengths for it to draw at all,
    `MapScene.AddInterior`), and in Wally's lesson the player's own figure stands on the platform, not Wally's.
  - **Part b, played** (wave 31): Route 104's two halves, Petalburg Woods, Rustboro City (`Architecture.Quarry`, style
    guide row first) with its seventeen rooms, Route 116 with the Tunnelers' Rest House, Rusturf Tunnel (a cave) and Route
    104's cottage and flower shop open, 48 areas. The Devon researcher and the grunt in the woods (a Great Ball), Roxanne
    and the Stone Badge with TM39 (her trainers counted beaten, Petalburg's count of Badges raised, Cut's gate open), the
    Cutter's HM01, the Devon Goods snatched outside the Gym and the employee's plea, Mr. Briney at the tunnel's mouth, the
    grunt cornered with Peeko, his battle, the goods back and Briney's thanks, the employee's Great Ball and Mr. Stone's
    PokéNav and Letter in Devon's office (the stairs guarded until then), Match Call added at Devon's door with Mom's and
    Mr. Stone's numbers, the rival registered and battled (or put off) at Rustboro's south edge with Emerald's team for the
    starter, Wanda's young man digging and Wanda beyond the rocks (her cousin Wally coming to stay), the school's Quick
    Claw, the flower shop's Wailmer Pail and berry of the day, Briney and Peeko home; every scene a `scene`. Story version
    24 (`common.HoennChapterTwo` hides the stretch's people; `HoennNewGame` calls it). Rulings: "The road to Rustboro".
  - **The PokéNav** (`Models/PokeNav.cs`, `UI/PokeNavScreen.cs`, `ModernUi.PokeNav.cs`, `GameState.PokeNav`; style guide
    row first): the start menu's POKéNAV once `FLAG_SYS_POKENAV_GET` is set, Hoenn's alone; pages HOENN MAP (the region's
    picture with the player's cell, the A button opening the map whole; `world/hoenn/townmap.json` from the overworld's
    matrix, our words for every place), CONDITION (the five qualities round a pentagon and the sheen), MATCH CALL
    (`world/hoenn/phone.json`, written by hand, kept by the Pokégear's flags, calls in `phone.txt`; `addcontact` reads both
    books) and RIBBONS (once a Pokémon wears one). Hoenn's trainers who swap numbers and their rematches are left.
  - **Looks and music**: `roxanne`, `aqua_grunt` (the `Team Aqua` class in battle), `mr_stone`, `devon_employee`, `briney`;
    Peeko, Skitty and Walda's dolls as field Pokémon. Songs (ours, rendered clash-free, −18.0 to −20.2 LUFS, not yet
    heard): `route104` (Routes 104 and 116), `petalburg_woods`, `rustboro` (and its rooms), `rusturf`, `battle_gym`,
    `victory_gym`, `eye_galactic` and `battle_galactic` (Team Aqua's, the sound map's `Team Aqua` row).
  - **H1's and part a's leftovers done**: Petalburg's Gym is as wide as its hall and followed down its length (a GBA room
    longer than a chunk sized by its floor, `World.cs`); Wally stands on the lesson's platform (`BattleSetup.PlayerLook`);
    Hoenn's 88 berry trees are patches 128 to 215 (`BerryPatches.HoennFirst`, in the order of Emerald's tree names, each
    with its named berry; older saves are given them); cover rows for Rustboro (paving, lamps, its walls), Rusturf's floor,
    the woods' shaded lawn, the beach's path and the sea's rocks and shore; the triggers north of Oldale were already
    scripted in part a. Key items `Letter`, `Devon Goods`, `Wailmer Pail` (ids 9100 to 9102, the importer's overrides).
  - **Tests**: `HoennStoryTests` grew (the stretch four ways by look and starter, the rival put off, the woods' battle lost,
    the Devon stairs, an older save, the berry trees, the PokéNav's screen, the songs). Harness: the `hoenn` mode's
    `h19`–`h32` and `hs24`–`hs48`.
  - **Left for H3 and after**: Mr. Briney's boat (and the rival by his cottage for a player not met in Rustboro), Rock
    Smash's rocks in Rusturf Tunnel and the young man's HM04, Verdanturf; Hoenn's trainers in Match Call and their
    rematches; Emerald's own berry rules (the Wailmer Pail waters nothing yet); cover rows of the tilesets not yet walked
    (`out/hoenn/report.md`); Walda's phrase; the Devon employee of Route 116 (shown once Steven has his letter).
- [ ] H3 Dewford, Slateport and Mauville (part a done 2026-10-18, wave 32: Mr. Briney's boat to Slateport's museum; part b
  done 2026-10-19, wave 33: Route 110 to Verdanturf Town, both bikes; part c, Mauville's Game Corner, done 2026-10-20,
  wave 34; left: the Battle Tents' bouts)
  - **Played**: Mr. Briney sails from his cottage to Dewford Town (a scene: the jetty, the sailing song, the camera over
    the sea, Dewford's jetty), back again, and on to Route 109's beach once Steven has his letter, and back to Dewford
    (`VAR_BRINEY_LOCATION`, Emerald's numbers); Dewford's fisherman and the Old Rod, the hall's word of the week and
    TM36, the boy's Silk Scarf; Granite Cave's hiker and Flash (TM70, Platinum's number), Steven and the Letter, TM47 and
    his number; Brawly's dark Gym, lighter for every trainer beaten (`Overworld/DewfordLights.cs`, Emerald's flash
    levels), Brawly, the Knuckle Badge, TM08 and his number; Route 109's beach (Soft Sand, the Seashore House's six Soda
    Pops), Slateport City (`Architecture.Wharf`; Dewford's `Islet`, style guide rows first), Team Aqua queuing at the
    museum until Dock at the shipyard sends the player there, the museum's fee and the grunt's TM46, Captain Stern, the
    two grunts' battles, Archie, the Devon Goods delivered (the Route 116 employee's Repeat Ball follows), Scott at the
    door, the Energy Guru's vitamins. Every scene a `scene`. Story version 25 (`common.HoennChapterThree` hides the
    stretch's people; `HoennNewGame` calls it). Rulings: "Mr. Briney's waters, Dewford and Slateport".
  - **Opened**: Routes 105 to 109 (the sea routes' swimmers wait for Surf; the Island Cave and the Abandoned Ship stay
    `locked`), Dewford Town and its six rooms, Granite Cave's four floors, the Seashore House, Slateport City and its
    twelve rooms (the Battle Tent's lobby alone, the harbour's way out to the open water `locked`); 78 areas. Spawns for
    Rustboro, Dewford and Slateport in `hoenn.json`; Dewford on the PokéNav's map.
  - **Match Call**: Steven, Brawly and Scott by their scenes, and Route 109's tubers Ricky and Lola once beaten (their
    calls in `phone.txt`); rematches still wait for Emerald's table of them, which the reader doesn't read.
  - **Looks and music**: `brawly`, `steven`, `capt_stern`, `archie`, `scott` (rows of `characters.json`); songs
    `dewford` (also the Seashore House), `slateport` (and its rooms), `sailing` (Mr. Briney's boat), ours, rendered
    clash-free, −20.0 to −20.3 LUFS, not yet heard. Eye themes for Tuber M and F, Sis and Bro and the Triathlete.
  - **The reader**: cover rows for Slateport's roads, squares and walls and Dewford's jetty and palms; furniture rows for
    Devon's offices (the reception counter, the president's desk, the researchers' PCs and machines: no more dark blocks),
    Stern's shipyard and the Oceanic Museum's cases and ship models (a case stands on the layout's blocked tiles); the shipyard's stairs up, whose label made a building of them, are a path. A few of the museum's upper exhibits still stand as plain blocks. The
    content check reads a tile's own area where a GBA chunk is shared (Dewford and Route 107).
  - **Tests**: `HoennStoryTests` (the stretch three ways by look and starter, the Seashore House, an older save, the
    songs); `HoennTests` (the region's maps, a museum door its queue stands on). Harness: the `hoenn` mode's `h33`–`h52`
    and `hs49`–`hs72`.
  - **Left for H3's second part**: Route 110 and the Trick House, the Cycling Road, Mauville City, Wattson and the Dynamo
    Badge, Rydel's two bikes (`BikeKind`), Route 117, Verdanturf, the three Battle Tents' bouts, Rock Smash for Rusturf
    Tunnel and the young man's HM04, the rival by Briney's cottage and on Route 110, New Mauville's door, the Game Corner;
    `route110`, `verdanturf`, `trick_house`, `bicycle`. Still from H2: Emerald's berry rules (the Wailmer Pail waters
    nothing yet), Walda's phrase, Hoenn's rematches.
  - **Part b, played** (wave 33): the rival at Route 110's north end and the Itemfinder (`Overworld/Itemfinder.cs`,
    Emerald's search over the GBA's screen), the Cycling Road's gates turning walkers back, the Trick House's first puzzle
    (the Trick Master under the desk, the hedge maze cut through, the scroll's code on the door, his Rare Candy), Wally's
    battle at Mauville's Gym door and Scott, Rydel's Mach and Acro Bikes swapped at will, HM06 in Mauville, Wattson's Gym
    with Emerald's barriers flipped by its four switches (electric gates of the map shown by two flags of ours kept to
    Emerald's barrier flag), the Dynamo Badge, TM34 and his number, the Rusturf rocks broken and the young man's HM04,
    Wanda and Wally in Verdanturf, the friendship rater, Route 117's Day Care (Sinnoh's rules), eight trainers of Routes
    110 and 117 in Match Call. Every scene a `scene`. Story version 27 (`common.HoennChapterFour`, called by
    `HoennNewGame`; another session of wave 33 may also raise the version: renumber at the merge). Rulings: "Route 110 to
    Verdanturf".
  - **Opened**: Route 110 with the Cycling Road's two gates, the Trick House's front room, first puzzle and last room,
    Mauville City and seven rooms (its Game Corner `locked`), Route 117 and its Day Care, Verdanturf Town and seven rooms
    (the Battle Tent's lobby alone), and Rusturf Tunnel's far end; 102 areas. Spawns for Mauville and Verdanturf;
    Verdanturf on the PokéNav's map. New Mauville's door stays `locked`.
  - **The bikes** (`BikeKind`, `Walker.Bike`, `Player.Bike`; plan decision 6's recommendation): the Mach Bike at full
    speed, up muddy slopes; the Acro Bike at a bicycle's pace, along rails and up bumpy slopes; Platinum's Bicycle as it
    was (`FieldMovementTests`). `TileBehaviors.InGame` rules the rails and bumpy slopes. Items Mach Bike, Acro Bike,
    Itemfinder (9103 to 9105) and Orange Mail (9106) in the importer's overrides, with Romanian names.
  - **Engine fixes**: the GBA's `FLAG_TEMP_*` are cleared on leaving a place and an obstacle's is made its area's
    (`World.LocalFlag`, `StoryState.ClearLocal`): one cut tree had hidden every tree sharing its number across Hoenn and
    Kanto (a Kanto test that talked to the wrong tree by key had passed because of it). A GBA door the story opens is
    open ground under its warp (`World.cs`), shut by `Map.ApplyDoors`. An obstacle may take a script of its area's own.
  - **Looks and music**: `wattson`, `trick_master`, `rydel`; Wally battles as himself (the `Rival` class lists him); Bug
    Maniac, Sr. & Jr. and Pokémon Breeder fold onto existing looks; `Architecture.Dynamo` (Mauville) and `Glade`
    (Verdanturf), style guide rows first. Songs (ours, rendered clash-free, −19.4 to −20.7 LUFS, not yet heard):
    `route110` (also Route 117), `verdanturf`, `trick_house`, `bicycle` (both bikes in Hoenn).
  - **Tests**: `HoennStoryTests` (the stretch three ways by look and starter, an older save, the songs), `FieldMovementTests`
    (the two bikes, the Itemfinder), `HoennTests` (the new rooms). Harness: the `hoenn` mode's `h53`–`h69`, `hs73`–`hs98`.
  - **Left for H3's part c and after**: the three Battle Tents' bouts (Slateport's on the Factory's rules; Verdanturf's and
    Fallarbor's need plan 14 · B9's Palace and Arena), Mauville's Game Corner (R16b's slots), the Cycling Road's timed
    challenge, the move tutors of Mauville and Verdanturf, Birch's number on Route 110, the cracked floors' fall (H12);
    still from H2 and H3a: Hoenn's rematches (Emerald's rematch table is not read: a call's chance and steps, more than
    small), Norman's call on the first voyage, the museum's plain exhibits, Walda's phrase, Emerald's berry rules, the
    boat prop's voyage (the camera pan and warp stand-in kept, the user's answer pending).
  - **Part c, done** (wave 34, with H4's first part): Mauville's Game Corner is open: its twelve machines are Platinum's
    slot machine (plan 06 · R16b), its coin counter and its TM exchange Emerald's (`gamecorner prize mauville`), the
    Coin Case traded for a Harbor Mail (9109) that Slateport's Mart sells on a counter of its own; the doll counter's
    prizes and the girl's starter doll are decorations (H10), the roulette tables are covered over. Song `game_corner`.
    **Still left of H3**: the three Battle Tents' bouts (Slateport's on R18b's Factory rules with Emerald's level-30
    rentals; Verdanturf's and Fallarbor's wait for plan 14 · B9's Palace and Arena), Emerald's own slot machine and
    roulette, the Cycling Road's timed challenge, the move tutors, Birch's number on Route 110, the cracked floors (H12).
- [x] H4 The desert, Mt. Chimney and Lavaridge (part a done 2026-10-20, wave 34: Route 111 to Meteor Falls; part b
  done 2026-10-20, wave 35: the cable car to Norman, its record below part a's)
  - **Played**: the Winstrates one after another at their door (Victor, Victoria, Vivi, Vicky; a family lost to part
    way is put back as Route 111 is come to again) and Victoria's Macho Brace; the desert's storm turning back whoever
    has no Go-Goggles at its south and west edges (Emerald's `ViciousSandstormTrigger`); the old lady's rest stop; the
    girl's daily Razz Berry; Team Magma's two grunts before Route 112's cable car; the glass workshop's Soot Sack, the ash
    gathered on Route 113's grass and Emerald's five flutes made of it; Lanette in Fallarbor's Pokémon Center and at home
    (`FLAG_SYS_PC_LANETTE`); Cozmo's wife; Route 114's TM05 and daily berry, the Fossil Maniac's brother's TM28; Team
    Magma taking the meteorite at Meteor Falls, Archie and his grunts after them, Route 112's grunts gone. Every scene a
    `scene`. Story version 28 (`common.HoennChapterFive`, called by `HoennNewGame`; another session of wave 34 may also
    raise the version: renumber at the merge). Rulings: "The desert, Fallarbor and Mauville's Game Corner".
  - **Opened**: Routes 111 to 114, Fiery Path, Fallarbor Town (`Architecture.Hayloft`, style guide row first) and seven
    rooms (the Battle Tent's lobby alone), Meteor Falls' first room, the Winstrates' house and the rest stop, the cable
    car's station (the ride is part b's), the glass workshop, the Fossil Maniac's house and tunnel, Lanette's house and
    Mauville's Game Corner; 123 areas. Shut: the desert's doors (Mirage Tower, the Desert Ruins), the Trainer Hill,
    Jagged Pass, Route 115 and Meteor Falls' rooms behind the falls, Terra Cave's hollows, the Desert Underpass. A spawn
    for Fallarbor; Fallarbor on the PokéNav's map.
  - **Engine**: an overlay's `zones` (weather and a song of a stretch of an area: the desert's sandstorm and Route 113's
    ash, Emerald's script-set weather by column and row; `MapArea.ZoneAt`, the music changing at a zone's edge); Rock
    Smash rolls Emerald's `rock_smash_mons` (`Map.SmashOut`, `WildEncounterRules.RockSmashMeets`, the script command
    `smashencounter`; Platinum's rocks hide nothing); ash gathered with the Soot Sack (`GameEngine.GatherAsh`); on a GBA
    map a step along a stair's slope is never a cliff (Emerald's elevation-0 stairs join any two levels: Route 114's ways
    down to Meteor Falls climb two levels in a tile). Items Go-Goggles, Soot Sack, Harbor Mail, Meteorite (9107 to 9110)
    in the importer's overrides, with Romanian names.
  - **Looks and music**: `magma_grunt` (the `Team Magma` class); the old woman, the cook, a girl, the reporter and her
    cameraman fold onto existing looks (the old woman of Route 110 and elsewhere had stood as the boy). Songs (ours,
    rendered clash-free, −19.6 to −20.5 LUFS, not yet heard): `desert`, `route113`, `fallarbor`, `cave_of_origin` (Meteor
    Falls), `game_corner`. Team Magma's eye theme is Team Aqua's (a Hoenn `eye_magma` is left).
  - **Tests**: `HoennStoryTests` (the stretch three ways by look and starter, the Game Corner and its Coin Case, a rock
    smashed in Hoenn and none of Platinum's, an older save, the songs); `HoennTests` (the region's maps, the walk to every
    room). Harness: the `hoenn` mode's `h70`–`h91` and `hs99`–`hs113`; `hs97` re-shot on the longest run of Route 110's decks (a bridge over its canal: the Cycling Road itself is imported as raised ground, not a deck, so the shot still misses it). Rooms furnished: Mauville's Game Corner (its slot machines, roulette tables and prize counter), the Battle Tents' lobbies (their two counters, Slateport's and Verdanturf's too) and the generic houses' second table, case and bookshelves (`GbaFurniture.HoennRanges`); the cable car's station is still dark blocks, and so is Verdanturf's Battle Tent outside (its tiles have no label and lie away from trees, so they are taken for bare rock, not a building at its door).
  - **Left for H4's part b**: the cable car and Mt. Chimney (Maxie, Tabitha, the Meteorite back for Cozmo, the Lava Cookie
    lady), Jagged Pass, Lavaridge Town (its `Architecture` row) and its rooms, Flannery and the Heat Badge, the rival's
    Go-Goggles, the desert and its trainers, Mirage Tower and its fossils, the Desert Ruins' door (shut until the Regis),
    Route 115 and Meteor Falls' inner rooms, Petalburg's Gym with Norman and the Balance Badge, `route113` is written but
    `mt_chimney`, `cable_car`, `eye_magma` and the bosses' themes are not; Gabby and Ty; the Secret Power man.
    Still from H2 and H3: Hoenn's rematches (Emerald's table), Emerald's berry rules (the Wailmer Pail waters nothing),
    Walda's phrase, the museum's plain exhibits, the Trick House's furniture, Verdanturf's Battle Tent outside. `PokemonTests` stalls on this machine before its first test (a testhost at 3 s of CPU for twenty minutes, as other sessions' did the same hour): not run to its end this session.
  - **Part b, done** (wave 35): the chapter from the cable car to Norman.
  - **Played**: the cable car's ride up and down (Route 112's attendant once Team Magma's grunts have gone, the summit's
    attendant back; `VAR_CABLE_CAR_STATION_STATE`); Mt. Chimney's summit: Tabitha and two grunts in the way, Team Aqua
    and Team Magma scuffling with their Poochyena, Maxie's battle at the meteorite machine, Team Magma gone, Archie's
    thanks, the Meteorite taken from the machine and handed back to Cozmo in Fallarbor for TM27, the mountain's trainers
    out once Lavaridge is come to, the Lava Cookie lady (200 money a cookie); Jagged Pass and its five trainers; Lavaridge
    Town, the old woman's Wynaut Egg (`FLAG_RECEIVED_LAVARIDGE_EGG`), the herb shop's counter (Emerald's four herbs) and
    its Charcoal; Flannery's Gym on its two floors of sand and steam, her battle, the Heat Badge and TM50; the rival with
    the Go-Goggles as the player next comes out of the Gym or the Pokémon Center; the desert with its trainers past the
    goggles' gate; Mirage Tower's four floors and its two fossils, one taken and the tower fallen; Route 115 and Meteor
    Falls' inner rooms with their trainers; Petalburg's Gym room by room, each door read and asking, Norman's battle,
    the Balance Badge and TM42, Wally's father coming in and HM03 in Wally's house. Every scene a `scene`. **Story
    version 29** (`common.HoennChapterSix`, called by `HoennNewGame`: the summit's trainers and the Lava Cookie lady, the
    rival in Lavaridge and Wally's father in the Gym hidden until their scenes). Rulings: "Mt. Chimney, Lavaridge and
    Petalburg's Gym".
  - **Opened**: Mt. Chimney's summit and its cable car station, Jagged Pass (its way into Team Magma's hideout `locked`
    until H7), Lavaridge Town (`Architecture.Spa`, the style guide's row first) and its six rooms with the Gym's two
    floors, Mirage Tower's four floors (Route 111's way in a `switch` until `FLAG_HOENN_MIRAGE_TOWER_FALLEN`), Route 115
    (Terra Cave's two ways in `locked` until H12), Meteor Falls' upper room and its two lower ones, and every room of
    Petalburg's Gym; 19 areas. A spawn for Lavaridge. Still shut: the Desert Ruins (until the Regis), the Desert
    Underpass, the Trainer Hill, Meteor Falls' Steven room behind its falls.
  - **Engine**: the GBA reader counts the tile a warp panel throws its rider onto as a landing (`GbaWorld`), so
    Lavaridge's vents put their riders on plain sand that leads nowhere; `Architecture.Spa` and its `BuildingArt` style;
    `MusicDirector.EyeSong` and the sound map's `eyeSongs` (a region's own eye song for a class: Team Magma's
    `eye_magma`); the herb shop's counter an override of the importer's `marts.json`.
  - **Looks and music**: `maxie`, `tabitha` and `flannery` (our own takes); the teams' admins and leaders drawn as who
    they are in battle, Emerald's Experts and old couple as `gentleman` and `lady`. Songs (ours, rendered clash-free with MusicRender, not yet heard):
    `mt_chimney`, `cable_car`, `eye_magma`, `battle_galactic_boss` and `victory_galactic_boss` in their Hoenn versions
    (the team leaders' battle and victory through the sound map's classes `Magma Leader` and `Aqua Leader`).
  - **Shots**: not taken (no harness this wave). The `hoenn` mode has no shots of part b yet: `h92` on and `hs114` on are
    owed (the cable car, the summit and Maxie, Jagged Pass, Lavaridge and its Gym's two floors, the Go-Goggles, the desert
    in its storm with the goggles on, Mirage Tower falling, Petalburg's rooms and Norman, Wally's house), and with them the
    budget of the desert's storm the plan's "Done when" asks for.
  - **Tests**: written but not run. `HoennStoryTests.TheSixthStretchPlaysFromMtChimneyToNorman` (three ways by starter:
    Maxie, the Meteorite, Flannery and the Heat Badge, the Go-Goggles and the desert let through, a fossil taken,
    Petalburg's doors and Norman, HM03), `PetalburgsGymDoorsAreShutUntilFourBadges`,
    `ASaveFromBeforeTheSixthStretchHasItsPeopleHidden`, `TheSongsOfMtChimneyAndTheCableCarAreThere`; the two tests that
    name the story version say 29; `StoryTests.AskedAheadOfTheirScenes` is empty again.
  - **Left**: the Desert Ruins' door (shut until the Regis, plan 21 · H8 or later); Gabby and Ty; the Secret Power man on
    Route 111; Norman's Match Call (Emerald registers it on Route 104's boat, H2's to write) and his rematch; Emerald's
    walk with Wally's father across Petalburg to his house (here the dark that closes in the Gym opens in the house);
    Mirage Tower's coming and going by the day (it stands until it falls); the buried trainers springing out of
    Lavaridge's sand; the rival's bicycle; the harness's shots above. Still from H2 and H3 as part a lists them.
- [ ] H5 Dive and the sea floor (part a done 2026-10-20, wave 36: Dive, Route 124 and the sea floor under it; part b
  done 2026-10-20, wave 37: the sea floor's light, bubbles, stage and sound, its record below part a's; only the frame's
  timing on the user's machine is left before the box is ticked)
  - **Opened**: Route 124 (its sea, islets and nine swimmers; the treasure hunter's house `locked` until H8) and
    `underwater_route_124` beneath it, the first pair. The reader lays each of Emerald's underwater maps under the route
    it lies under (`GbaWorld.LayUnderItsRoutes`): the floors of Routes 124 to 128 are matrix 2, `HoennUnderwater`
    (`MapSetting.Underwater`, in `RegionDatabase`'s Hoenn maps), at the overworld's own coordinates, 188 dark-water tiles
    over 188 open floor tiles with none astray; the floors of the Seafloor Cavern, Sootopolis and the Marine Cave are
    matrices 3 to 5, for H8 and H12 (the plan had one map). Route 124 is come to by sea from Lilycove (H7), so the walk
    of the game reaches it through `WorldWalk.OpenedAhead` (630, 109, the sand before the house), which H7 takes off.
  - **Engine**: `TravelMode.Diving` and `FieldMoves.Dive` at their enums' ends; the four tiles (`DiveWater`,
    `NoSurfacing`, `Seaweed`, `SeaweedNoSurfacing`, in `TileBehaviors.InGame` and `docs/tile-behaviours.md`) and
    `TileType.Kelp` (map code `k`); `FieldMovement.CanDive`, `CanSurface` and `SurfacesOnto`, a diver's pace `Walk`
    with no running, bike or ledges (a ruling: Emerald's diver walks); `MapArea.Dive` and `Emerge` from the area files,
    `Map.IsUnderwater`, `DiveAt`, `EmergeAt`; `Player.StartDive` and `Surface`, a player on an underwater map settling
    diving, encounters only in seaweed from the floor's water table; `FieldMove.Dive` on the team's menu only where the
    region's `gates` name it (`PartyScreen.Region`, `ActionsFor(pokemon, region)`), Hoenn's gate the Mind Badge;
    `FieldSpot.Dive` and `Surface`; `common.Dive`, `Surface` and `UseDive`, started from `TryInteract` by the tile
    underfoot with A (B is Run); the script command `dive` (`Op.Dive`, both hosts, `StopsAHurry`, `RESULT` 1 or 0;
    `GameEngine.Diving.cs`: a fade, the sound `dive`, the same tile of the other map). Battles under the sea are on
    `BattleTerrain.Water` and the water's stage until part b. Saving under the sea keeps `Diving` as surfing keeps
    surfing; no version raised, so no older save is touched.
  - **Looks and music**: the style guide's "The sea floor" first, then `ArtLook.UnderwaterFieldRig` (one rig at every
    hour), the floor's sand and rock with kelp beds (`PixelGround`'s kelp ground, `NatureArt.Kelp` swaying through
    `MapScene.AddKelp` in `MeshPass.Ground`). `hoenn/underwater.mml` (ours; clash-free, −19.4 LUFS, not yet heard) and
    the sound `dive` (−30.2 dB, not yet heard). Route 124 plays `hoenn/route104` until Emerald's Route 120 song is
    written. Rulings: "Dive and the sea floor".
  - **Tests**: written but not run. `FieldMovementTests` (`ADiveGoesDownFromDarkWaterAndComesUpWhereTheFloorLetsIt`:
    refused on plain sea, without the move, under `NoSurfacing`; `UnderTheSeaNobodyRunsHopsOrRides`;
    `WildPokemonOfTheSeaFloorLiveInItsSeaweed`), `FieldMoveTests` (`DiveGoesDownAndComesUpByTheQuestionWithHoennsMindBadge`,
    `DiveIsOnATeamsMenuOnlyInARegionWhoseGatesNameIt`), `HoennTests` (`TheSeaFloorLiesTileForTileUnderTheSeaItIsDivedTo`,
    `TheWalkDivesToTheSeaFloorAndComesBackUp`), `ScriptTests.EveryCommand` (`dive`); `WorldWalk` dives and surfaces.
  - **Shots**: not taken (no harness this wave). The `hoenn` mode ends with `HoennSeaShots` (`Modes/HoennSea.cs`):
    `h92_route124_surface`, `h93_dive_down`, `h94_sea_floor_day` and `h95_sea_floor_night` (each with `_native`),
    `h96_seaweed_battle`, and the way back up; the plan's `h40` to `h44` were already taken.
  - **Left for part b**: the caustic haze layer and the diver's bubbles (`FieldLife.Bubbles`, `LifeArt` cells; the style
    guide's "The sea floor" lists both under "Still to come"), `BattleArena.Underwater` and its stage (the style guide's
    "Arenas (G8)" first), `AmbienceBed.Underwater` with `SoundTests`, `FieldLifeTests` and `AmbienceTests`, and the
    `profile` before and after on `HoennUnderwater` that the plan's "Done when" asks for (the kelp is sway geometry over
    every floor tile of kelp). The floors of Routes 125 to 128 open with their routes in H8.
  - **Part b, the looks** (the style guide's "The sea floor", "Life" and "Arenas (G8)" first): **the waves' light** is a
    64-texel net that repeats at its edges (`LifeArt.Caustics`: the edges between the cells of a 4×4 Worley pattern,
    alpha 255 in their core and 110 at their fringe, nothing between, no lone speck), laid in two layers over the
    composite by `WorldRenderer.DrawCaustics` in additive blend (`WeatherFx.Caustics`, `CausticLayers`: texels of 6 and
    9 layout units drifting east and west, both down, tinted `196,236,228` at alpha 34 and 26), each wrapped by its repeat,
    held to the 3-unit grid and pinned to the floor through the camera's target (`WorldRenderer.seaPin`), so the net goes
    with the ground as the camera moves and every edge falls on a texel of the field; point-filtered
    (`SceneTextures.Caustics`), timed by `FieldLife.Now`, drawn under the weather's layers and dimmed with them, and only
    where `Map.IsUnderwater`. **The diver's bubbles** are `FieldLife.Bubbles(map, x, z)`, which the engine asks every
    frame of the field while the player is `Diving`: a breath on every whole multiple of `BreathEvery` (2.8 s) of the
    field's clock, three bubbles a tenth of a second apart from `HeadHeight` (1.25) over the floor, a few texels to one
    side by the breath's number, rising 24 texels a second and swaying a texel every fifth of a second, small (cell 13)
    for a third of a second, grown (14), bursting (15) for the last 0.15 s of 1.3 (`BubbleAt`); the same however the
    frames fall, none made up after a pause (`BreathCatchUp`), none in a room. Each breath plays the sound `bubbles`.
  - **Part b, the battle**: `BattleArena.Underwater` at the enum's end, which `Map.ArenaAt` gives on an underwater map
    whatever the seabed (a stage the area's overlay names still wins), its ground still `BattleTerrain.Water`
    (`GameEngine.TerrainAt`, part a's, unchanged, so the Dive Ball and Nature Power read as they did). The stage
    (`BattleArenas.Underwater`): rippled sand, a sand platform on a rock skirt, kelp in rows behind and to the sides
    (`BattleArenas.Kelp`, a lathe of four greens with double-sided blades, swaying in `MeshPass.Ground`), far rock masses
    and boulders; lit by `ArtLook.UnderwaterArenaRig` at every hour (a blue-green key from nearly overhead, a blue fog
    that closes in at 16 to 64, the sea for a sky with no cloud or star; not `ArenaSpec.Outdoors`); in its water
    `ArenaFx.SeaBubbles` (thirty streams rising, every third a trio), pale shafts of light and motes, all depth-tested.
  - **Part b, the sound**: `AmbienceBed.Underwater` at the enum's end, a 12-second bed (a muffled hum, the sea swelling
    slowly overhead every 6 s, now and then a few low bubbles, a short echo), which `FieldAmbience.Around` gives alone
    on an underwater map at `UnderwaterGain` (0.9), centred, whatever the weather above or the tiles' behaviours.
    MusicRender: −28.6 dB in the bed window (−30 to −26), peak −13 dB, seam 0.007 against a largest step of 0.084. The
    sound `bubbles` (Field, 420 ms): −27.9 dB, peak at 0.36 before the bus, clean ends. Neither heard yet.
    `docs/sound-effects.md` has both rows.
  - **Part b, tests** (written, not run): `FieldLifeTests` (`TheSeaFloorsNumbersAreTheStyleGuides`,
    `ADiverBreathesOutThreeBubblesThatRiseInWholeTexelsGrowAndBurst`, `ADiversBubblesAreTheSameHoweverTheFramesFall`,
    `AfterAPauseNoBreathIsMadeUpAndARoomHasNone`, `TheWavesLightIsHeldToTheFieldsTexelsAndPinnedToTheFloor`,
    `TheWavesNetIsLinesInTwoStepsOfLightThatJoinAcrossItsEdges`, and the atlas test's cells 13 to 15), `AmbienceTests`
    (`TheSeaFloorIsHeardAsItsOwnBedAloneWhateverIsAbove`; the beds' own test takes the new bed by itself), `SoundTests`
    (`ADiverIsHeardOnceABreath`; the bank's tests take `bubbles` by themselves), `BattlePresentationTests`
    (`TheSeaFloorIsFoughtOnItsOwnStage`, the sea floor among the arenas that ignore the clock and have something in
    the air).
  - **Part b, shots** (written, not taken): the `hoenn` mode's `HoennSeaShots` goes on from `h96` (now on the sea
    floor's own stage) with `h97_bubbles_rising`, `h98_bubbles_bursting_native` and `h99_caustics_later` (each with a
    `_native` crop), timed on the field's clock from the next breath, before the way back up; `arenas` ends with
    `a40_underwater` and `a41_underwater_night` (the same light: the rig ignores the clock); `profile` times
    "hoenn's sea floor", "hoenn's sea floor, in the kelp" (by `AreaSpot("underwater_route_124")`, diving) and "battle
    on the sea floor, at the menu". No golden file should move (the arena is the face's alone, the terrain unchanged),
    and no story or save version was raised.
  - **Left**: the `profile` timing on the user's machine (the GTX 970) against the 8 ms budget on High, before and after
    (`tools/dev/ab.sh` on the commit before part b with `sea floor`), which is what "Done when" still waits for: the
    caustics are two full-screen draws of a 64-texel texture and the kelp sway geometry, so the floor is the scene to
    watch; looking at `h94` to `h99`, `a40`, `a41` and listening to the bed and `bubbles` (MP3s from MusicRender's WAVs).
- [ ] H6 Route 118 to Fortree (part a done 2026-10-20, wave 38: Route 118 to Winona and Route 120; part b done
  2026-10-20, wave 39: Route 121, the Safari Zone, the Ancient Tomb and the Scorched Slab, Fortree's trade, Hidden
  Power quiz and Sleep Talk tutor; part c done 2026-10-20, wave 40: Gabby and Ty, Fortree's half of the Wingull
  errand and the plank bridges' bounce; what is left of the chapter is listed under "Left after part c")
  - **Played**: Steven on Route 118, coming to the ledge's edge above whichever of the three tiles the player steps on,
    hopping down and walking off east (`VAR_ROUTE118_STATE`); the fisherman's Good Rod; the Weather Institute held by
    Team Aqua, Shelly beaten upstairs, a grunt's news of Mt. Pyre, Team Aqua gone behind a fade, Castform at 25 holding a
    Mystic Water, the workers back downstairs as the player comes out (`VAR_WEATHER_INSTITUTE_STATE` 0 to 2), the ground
    floor's beds that heal; the rival on Route 119's bridge by the starter's team, HM02, the rival running off and Scott
    coming down the path (`VAR_ROUTE119_STATE`, `VAR_SCOTT_STATE`); Kecleon nobody can see, two on Route 119 and five on
    Route 120 battled at 30 once the Devon Scope is had, Fortree's on the bridge to the Gym fleeing; Steven on Route 120,
    asked and told "not yet" or shown the bridge's Kecleon, the Devon Scope given; the beauty's berry a day; Winona, the
    Feather Badge with her six trainers counted beaten, TM40 and her number; six route trainers registered in Match Call
    once beaten (Rose, Dalton, Jackson, Catherine, Robert, Jeffrey). Every scene a `scene`. **No story version raised**:
    `common.HoennChapterSeven` (called by `HoennNewGame`, and by Route 118's and Route 119's arrival scripts for a save
    that never ran it, `FLAG_HOENN_CHAPTER_SEVEN`) hides the stretch's people. Rulings: "Route 118 to Fortree City".
  - **Opened**: Routes 118, 119 and 120, Fortree City (`Architecture.Treetop`, the style guide's row first: weathered
    boards, leaf-green gables, shutters) and twelve rooms (Route 119's house, the Weather Institute's two floors,
    Fortree's five houses, its Mart, its Pokémon Center on both floors and Winona's Gym); 160 areas. Fortree's tileset
    has its rows in `GbaCover.HoennRanges` (the plank walks and platforms as bridges, the ladders as steps, the forest
    floor's earth, the trunks and canopies as trees). Shut: the decoration shop (H10), the Ancient Tomb and the Scorched
    Slab (`locked`), Route 121 and on. A spawn for Fortree (7) and **Fly over Hoenn**: the Town Map's `fly` places for
    every spawn on the overworld, each on its town's cells, so the Feather Badge's Fly lands where plan 02 · S2's Fly
    lands in Sinnoh.
  - **Engine**: `FortreeGates` (`Overworld/FortreeGates.cs`, a `GymPuzzle`, Emerald's `rotating_gate.c` with the
    Fortree layout: eight gates of rails turning on a corner's pivot, a step into an arm turning it a quarter round
    unless an arm would sweep into a wall, laid out afresh on every arrival; `GymPuzzle.Bars` and `Crossed`, asked by
    `FieldMovement.Step` and `Player`; drawn by `GymPieces` with `GymArt`'s rails), named by a room's entry in Hoenn's
    `world.json` (`WorldMapEntry.Puzzle`, which `WorldMapBuilder` hands `GymPuzzle.Create`); `NPC.UnseenType`
    ("Unseen"): a thing of the map in the way and spoken to, drawn as nothing and casting no shadow (Emerald's
    `invisible` Kecleon); `givepokemon "<species>" <level> holding "<item>"` (the parser, the runner,
    `ScriptLibrary.Problems`, `docs/scripts.md`, `ScriptTests`). The Devon Scope (item 9111) in the importer's overrides
    and `items.json`, with its Romanian name.
  - **Looks and music**: Winona and Shelly in `characters.json`; the Kecleon have no look (they are never drawn). Songs
    (ours, rendered clash-free, −19.0 to −20.2 LUFS, not yet heard): `route118`, `route119` (Route 119 and its
    institute's road), `fortree` (the city and its rooms), `route120`. The institute plays Rustboro's song, as Emerald's
    does. `safari_zone` is part b's.
  - **Tests** (written, not run): `HoennStoryTests` (`TheSeventhStretchPlaysFromRouteOneEighteenToWinona` three ways by
    look and starter, `StevenOnRouteOneTwentyWaitsWhenThePlayerIsNotReady`,
    `ANewGameOfHoennHidesTheSeventhStretchsPeopleAndAnOlderSaveHasThemHiddenOnTheWay`,
    `TheSongsOfRouteOneEighteenToFortreeAreThere`); `HoennTests` (`FortreeCityIsOpenWithItsFlySpotItsTreetopsAndItsRooms`
    with the Fly places and Fortree's cover rows, `RoutesOneNineteenAndOneTwentyHaveTheirWeatherAndTheirSongs`,
    `AKecleonNobodyCanSeeStandsInTheWayOnFortreesBridge`, `TheFortreeGymsGatesTurnWhenPushedAndHoldAgainstAWall`);
    `ScriptTests` (`APokemonGivenHoldingAnItemComesWithIt`, a parse error). The content check finds nothing.
  - **Shots** (written, not taken): the `hoenn` mode's `h100`–`h117` (Route 118, Steven's ledge, Route 119 south and in
    its rain, the institute outside and on both floors, Route 119 north, Fortree from its middle and west with a
    `_native` crop, the Gym with a `_native` crop, the Center, the Mart, a house, Route 119's house, Route 120's bridge,
    rain and cloud) and after dark `h118_fortree_night`, `h119_route119_rain_night`; the scenes `hs114`–`hs139` at the
    end of `HoennScenes` (Steven on Route 118, the institute held, Shelly and her battle, the grunt's news, Castform, the
    rival and his battle, HM02, Scott, Route 119 in the rain, the unseeable Kecleon, Steven on Route 120, the Scope, the
    Kecleon showing itself and its battle, the Scope given, the Kecleon fleeing, Winona, her battle and the Badge);
    `profile` times "fortree city in the treetops", "route 119 in the rain" and "fortree gym, the rotating gates".
  - **Left for part b**: Route 121, the Safari Zone (Hoenn's tables on the Great Marsh's `SafariGame`, `safari_zone`),
    Gabby and Ty's interviews (hidden by `HoennChapterSeven`), the Ancient Tomb and the Scorched Slab, Fortree's house
    trade, house 2's Hidden Power quiz and the Sleep Talk tutor, house 4's Wingull errand (its Wingull hidden), the plank
    bridges' bounce underfoot (Emerald's `MB_FORTREE_BRIDGE` step effect) and the bridge's metatiles swapped once
    Fortree's Kecleon has fled; then the "Done when": Winona beaten in the harness, and Fortree's bridges looked at as
    decks over the forest floor in `h108` and `h109`. The Lavaridge Gym's warp at 2, 15 (Gerald, a buried trainer
    standing on a warp) was not touched.
  - **Part b, done 2026-10-20 (wave 39)**. Rulings: "Route 121 and the Safari Zone".
  - **Open**: Route 121 (west of Lilycove, which stays shut, with the pier for Mt. Pyre), the Safari Zone's gate
    (`Route121_SafariZoneEntrance`), the Safari Zone (`HoennSafariZone`, Emerald's six parts laid side by side on one map
    as the importer's matrix 356, each part its own area with Emerald's own tables: `hoenn_safari_zone_south`,
    `_southwest`, `_north`, `_northwest`, `_northeast`, `_southeast`) and its rest house, the Ancient Tomb and the
    Scorched Slab (Route 120's two `locked` ways open). 171 areas. **Emerald's Safari Zone keys take the region's
    prefix** (`GbaMaps`: `hoenn_safari_zone_*`, `HoennSafariZone`), since FireRed's `safari_zone_*` are Kanto's.
  - **The story**: Team Aqua's three grunts at Route 121's west end, met at any of the area's four triggers, ordered out
    to Mt. Pyre and walking off down the pier (`VAR_ROUTE121_STATE`, `FLAG_HIDE_ROUTE_121_TEAM_AQUA_GRUNTS`, a
    `scene`); Walter, Jessica and Cristin in Match Call once beaten, Kate and Joy's double battle. **The Safari Game is
    the Great Marsh's**, as plan 01 · M7 built it: the counter's coordinate event asks, takes $500, `safari start 500`
    (thirty balls, Emerald's five hundred steps) and walks the player in through the zone's door
    (`VAR_SAFARI_ZONE_STATE` 2, cleared as the zone is come to); the attendant inside the door wishes luck once
    (`FLAG_GOOD_LUCK_SAFARI_ZONE`) and after that offers to end the game; `common.SafariTimeUp` and `SafariOutOfBalls`
    bring a player of Hoenn back to the gate (`VAR_SAFARI_ZONE_STATE` 1), whose arrival walks them out past the counter.
    No Pokéblock Case is asked for (H11 owns Pokéblocks), and no box is checked for room (no gate of this game does).
    The two builders before the expansion stand until Hoenn's Hall of Fame sets their flag (H9), and the expansion's
    attendant is away (`common.HoennChapterSevenB`, `FLAG_HIDE_SAFARI_ZONE_SOUTH_EAST_EXPANSION`, as Emerald's new game
    sets it). Fortree's first house: the boy's Plusle, Pluses, for a Volbeat, once (`NpcTrades` "pluses", holding Grass
    Mail, with Emerald's five conditions and sheen: the trade record gained `Conditions` and `Sheen`). Fortree's second
    house: the old woman's coin, right, right, then left, for TM10 (`FLAG_MET_HIDDEN_POWER_GIVER`,
    `FLAG_RECEIVED_TM_HIDDEN_POWER`); the old man's Sleep Talk, once (`tutor once "Sleep Talk"`, the move's row in
    `MoveTutors.Kanto`, which serves every region's one-move tutors; `FLAG_MOVE_TUTOR_TAUGHT_SLEEP_TALK`). The Ancient
    Tomb's raised dots are read as words of ours; Registeel is not listed and stays away. **No story version raised**:
    `common.HoennChapterSevenB` is called by `HoennNewGame` and by Route 121's arrival for a save that never ran it
    (`FLAG_HOENN_CHAPTER_SEVEN_B`). Emerald's Wood Mail is not in the item table (a new item wants its Romanian name,
    which this session could not write), so Pluses holds Grass Mail.
  - **Lavaridge's Gym**: the GBA reader writes a warp under a buried trainer (`TRAINER_TYPE_BURIED`) as a landing
    (`GbaEvents.BuriedOn`): the trainer springs out at whoever comes near and stays on the patch, so Emerald's player
    never steps on it. Gerald's warp at 2, 15 and seven more of the two floors (3, 14, 3, 10 and 5, 2 above; 4, 18,
    3, 6, 4, 16 and 13, 17 below) lead nowhere now; the warps they were paired with on the other floor stay.
  - **Look and music**: the zone's parts with round trees; the gate plays Fortree's song, Route 121 Route 120's.
    Songs (ours): `safari_zone` (G major, 112 bpm, ocarina, marimba, strings; clash-free, −19.3 LUFS) and
    `sealed_chamber` (the Ancient Tomb's, 60 bpm, choir, bell, contrabass and timpani; rendered once, quiet by 1.4 dB,
    its volumes raised and not rendered again). The Scorched Slab plays Petalburg Woods' song, as Emerald's does.
    Route 121's Hex Maniac brought her class's eye theme to the sound map (`mystery`, Emerald's suspicious encounter).
  - **Tests** (written, not run): `HoennTests` (`RouteOneTwentyOneAndTheSafariZoneAreOpenWithTheirTables`,
    `TheAncientTombAndTheScorchedSlabOpenOffRouteOneTwenty`, `LavaridgesBuriedTrainersLieOnPatchesThatLeadNowhere`);
    `HoennStoryTests` (`TeamAquasGruntsMoveOutOfRouteOneTwentyOneOnce`,
    `TheSafariZoneIsPaidForAtItsCounterAndLeftPastItsAttendant`,
    `ANewGameOfHoennKeepsTheSafariZonesExpansionShutAndAnOlderSaveIsToldOnRouteOneTwentyOne`,
    `FortreesBoyTradesHisPlusleForAVolbeatOnce`, `FortreesOldWomanGivesHiddenPowerForThreeRightGuesses` two ways,
    `FortreesOldManTeachesSleepTalkOnce`). The content check finds nothing.
  - **Shots** (written, not taken): the `hoenn` mode's `h120`–`h131` (Route 121 and its pier, the gate, the zone's
    south with a `_native` crop, south-west, north and north-west, the rest house, the Ancient Tomb's hall and chamber,
    the Scorched Slab, Fortree's second house) and `h132_safari_night`; the scenes `hs140`–`hs154` at the end of
    `HoennScenes` (the grunts and the road clear, the counter's question, the balls, the zone, the attendant's luck and
    his offer to leave, out past the counter, the trade offer and Pluses, the coin game, which hand, three in a row, the
    Sleep Talk tutor and the move learned).
  - **Part c, played** (wave 40): *Gabby and Ty*, Emerald's reporter and cameraman, stand on Routes 111, 118 and 120 in
    turn by the count of their battles (`VAR_GABBY_AND_TY_BATTLE_NUM`, 0 to 8: the nine places the area files give,
    which goes round from 9 to 6 as Emerald's remainder does). `GabbyAndTyUpdateLocation` (`common.txt`), run by the
    arrival scripts of the three routes, hides them where they were and shows them where the count puts them; each
    person of the three areas' overlays runs `GabbyAndTyGabby` or `GabbyAndTyTy`, the same script on every route, with
    two Pokémon on the team asked for (one is told to come back with two). The battle is a double against a party of
    their own (`hoenn_gabby_and_ty_1` to `6`, class Interviewer: the first five by the count, the sixth after), and the
    interview follows: the first time Emerald's one easy-chat word, here the phrase picker's two words (plan 08 · P9,
    `phrase GABBY_AND_TY`), kept until the next battle, when the pair quote it back and ask the question of the battle
    they remember. Two things of a battle are remembered for it, the two this game can tell: it was over at once
    (`TURNS == 0`) or a Pokémon of the team fell in it (`FAINTED` rose). An interview given puts their segment on the
    air (`FLAG_GABBY_AND_TY_ON_AIR`, a new `broadcast gabbyandty` programme, Hoenn's alone, on the first television looked
    at after it by `FLAG_GABBY_AND_TY_SHOW_NEW`, then one among those a die draws until the next battle takes it off);
    the segment says the route, the two Pokémon fought beside, what the battle is remembered for and the answer. Three
    built-in variables came with it (`FAINTED`, `LEAD_SPECIES`, `SECOND_SPECIES`; `docs/scripts.md`). A battle already
    fought on this visit leaves a word that the pair are watching (`FLAG_MAP_LOCAL_GABBY_AND_TY_BEATEN`, a local flag
    standing in for Emerald's trainer flag and its temporary skip). *Fortree's fourth house*: the boy's first talk
    sends the Wingull out of the door (`FLAG_WINGULL_SENT_ON_ERRAND`, hidden afterwards by
    `FLAG_HIDE_FORTREE_CITY_HOUSE_4_WINGULL`, which part b had kept hidden for want of anything to do with it), clears
    Mossdeep's Wingull's hide for H8, and from then on he says the bird is away on an errand. *The plank bridges*
    (`FieldLife.PlankRows`, `Graphics/FieldLife.Planks.cs`; style guide, "Life", "Planks that give"): on the tiles
    Emerald calls `MB_FORTREE_BRIDGE` (Hoenn's maps only, since Sinnoh's decks share `TileBehavior.Bridge` and must stay
    still) the walker's sprite is lowered two rows for 0.07 s after their feet come onto a plank, one row until 0.13 s,
    two again until 0.20 s, then one for as long as they stand, and the step is heard as the new sound `bridge_walk`.
  - **Part c, decisions**: no story or save version is raised. The new people are hidden by `HoennChapterSevenC`
    (`FLAG_HOENN_CHAPTER_SEVEN_C`), which `HoennNewGame` calls and which Routes 111, 118, 119 and 120's and Fortree
    City's arrival scripts run for a save from before (`rulings.md`, "Gabby and Ty, the Wingull and the planks"). The
    plank's picture is baked into its chunk and cannot move, so the walker carries the dip alone.
  - **Part c, tests** (written, not run): `FieldLifeTests` (`ThePlankNumbersAreTheStyleGuides`,
    `OnlyHoennsBridgesGiveUnderFoot`, `AWalkerOnAPlankDropsSpringsAndSettlesAndDropsAgainOnTheNext`), `SoundTests`
    (`APlankBridgeKnocksUnderTheFeet`), `HoennStoryTests` (`GabbyAndTyStandWhereTheCountOfTheirBattlesPutsThemAndFightWithTheNthParty`,
    `GabbyAndTyOnlySayTheyAreWatchingOnceFoughtAndWantTwoPokemonToBegin`,
    `AnInterviewIsKeptQuotedBackAndPutOnTheAirAndTheBattleIsRememberedFor`, `ALostPokemonIsWhatTheNextBattleIsAskedAbout`,
    `AnInterviewRefusedLeavesNothingOnTheAirButTheBattleIsCounted`, `GabbyAndTysSegmentIsHoennsAloneAndNeedsAnInterview`,
    `FortreesBoySendsHisWingullOffOnItsErrandOnce`, `ANewGameOfHoennHidesGabbyAndTyAndAnOlderSaveIsGivenTheThirdPartOnTheWay`).
    The content check finds nothing. `scripts.txt` of the golden files moves (common.txt, Routes 111, 118, 119, 120,
    Fortree City and its fourth house).
  - **Part c, shots** (written, not taken): the `hoenn` mode's `hs155`–`hs167` at the end of `HoennScenes` (the pair on
    Route 111, spotting the player, the offer, the double battle, the pair beaten, the interview asked, the phrase
    picker and its whole phrase, the viewers' line, Route 118's pair and the answer quoted, Route 120's pair, the boy and
    his Wingull running out and gone, the plank before, giving, springing back and at rest). It prints the pair's
    variables and flags after each stage and whether a plank tile was found near Fortree.
  - **Left after part c**: the bridge's metatiles swapped once Fortree's Kecleon has fled (Emerald's bridge changes
    picture; it wants an alternate chunk from the importer, which needs the decompilation's cache and could not be
    fetched this session); the pair's thrown-ball and healing-item trivia (the engine does not count them); the
    Wingull's return, the delivered letter and the boy's Mental Herb (Mossdeep's half, H8); the Safari Zone's
    Pokéblock feeders and the Case the counter asks for (H11); the expansion's opening (H9); the Ancient Tomb's Flash in
    the chamber's middle and Registeel (with the Sealed Chamber, H8 and after); then the "Done when": Winona beaten in
    the harness, and Fortree's bridges looked at as decks over the forest floor in `h108` and `h109`.
- [x] H7 Lilycove, Mt. Pyre and the hideouts (part a done 2026-10-24, wave 41: Lilycove City and its rooms, Routes 122
  and 123 and Mt. Pyre to the Magma Emblem; part b done 2026-10-24, wave 42: the two hideouts, its record below part
  a's; tests and shots written, not run)
  - **Opened** (30 areas, 201 in all): Lilycove City (`gTileset_Lilycove`'s open metatiles paving in
    `GbaCover.HoennRanges`) with its Pokémon Center on both floors, the Department Store's five floors and roof, the Cove
    Lily Motel's two floors, the museum's two, the Pokémon Trainer Fan Club, the Move Deleter's house and four houses;
    Routes 122 and 123 with the Berry Master's house; Mt. Pyre's six floors (`Indoors`), its mountainside (fog above its
    middle as an overlay zone) and its summit. A spawn for Lilycove (8, whose Fly place the Town Map had). Shut: the
    Contest Lobby and Hall (H11), the harbour (H9), Team Aqua's hideout (part b) and the store's lift on every floor.
    **Route 124 is reached by sea from Lilycove**, so `WorldWalk.OpenedAhead` is empty (rulings: the Wailmer stand only
    on their own tiles).
  - **Played**: the rival before the Department Store, asked and refused and asked again (`FLAG_DECLINED_RIVAL_BATTLE_
    LILYCOVE`), battled with the team of the starter taken (`hoenn_may_lilycove_*`, `hoenn_brendan_lilycove_*`) and gone
    (`FLAG_MET_RIVAL_LILYCOVE`); the berry gentleman's berry a day; the Wailmer grunt; the Lottery Corner once a day
    (Jubilife's draw, Emerald's prizes); the store's six counters (marts.json's `lilycove_2f_*` to `lilycove_4f_*`); the
    roof's drinks machine and Substitute tutor once; the Move Deleter; TM44 Rest once; Scott napping in the motel
    (`FLAG_MET_SCOTT_IN_LILYCOVE`, `VAR_SCOTT_STATE`); the Cleanse Tag on Mt. Pyre's ground floor; the summit: Team Aqua's
    four grunts, Archie's words at the first step below the shrine from any of its three tiles, Team Aqua gone behind the
    dark, the old woman's tale of the two orbs and the **Magma Emblem** (9112, `FLAG_RECEIVED_RED_OR_BLUE_ORB`,
    `FLAG_HIDE_JAGGED_PASS_MAGMA_GUARD`, `VAR_MT_PYRE_STATE` 1), and the old man's tale; Route 123's TM19 for a trainer
    with a Grass Pokémon; the Berry Master's two berries a day and his wife's for a phrase. Every scene a `scene`.
    **No story version raised**: `common.HoennChapterEight` (called by `HoennNewGame`, and by the arrivals of Lilycove City
    and Routes 122 and 123, `FLAG_HOENN_CHAPTER_EIGHT`). Rulings: "Lilycove City and Mt. Pyre".
  - **Music** (ours, rendered clash-free, −18.2 to −20.1 LUFS, not yet heard): `lilycove` (the city and its rooms),
    `route122` (Routes 122 and 123), `mt_pyre` (the tower's floors), `mt_pyre_exterior` (the mountainside and the
    summit). The Berry Master's house plays Rustboro's song, as Emerald's does.
  - **Tests** (written, not run): `HoennTests` (`LilycoveCityIsOpenWithItsFlySpotItsStoreAndItsShutDoors`,
    `MtPyreIsOpenFromRouteOneTwentyTwoToItsSummit`, and `TheWalkDivesToTheSeaFloorAndComesBackUp` with Route 124 come to
    by sea); `HoennStoryTests` (`TheRivalBattlesOnceBeforeLilycovesDepartmentStore` three ways,
    `TeamAquaLeavesMtPyresSummitAndTheOldWomanGivesTheMagmaEmblem` from each of the three tiles,
    `MtPyreIsClimbedFromRouteOneTwentyTwoToItsSummit`, `LilycovesStoreSellsEmeraldsStockAndItsLotteryDrawsOnceADay`,
    `LilycovesRoofTeachesSubstituteOnceAndRestIsGivenOnce`,
    `TheBerryMasterGivesTwoBerriesADayAndTheGirlOfRouteOneTwentyThreeWantsAGrassPokemon`,
    `ANewGameOfHoennHidesTheEighthStretchsPeopleAndAnOlderSaveIsGivenThemOnTheWay`, `TheSongsOfLilycoveAndMtPyreAreThere`).
    The content check finds nothing. `scripts.txt` of the golden files moves (common.txt and the new places' scripts).
  - **Shots** (written, not taken): the `hoenn` mode's `h133`–`h152` (Lilycove, its cove and beach, the Center, the
    store's ground and fourth floors and roof, the motel, the museum's two floors, the Fan Club, Routes 122 and 123, the
    Berry Master's house, Mt. Pyre's ground, third and top floors, its mountainside in sun and in mist and its summit,
    with `_native` crops of Lilycove and the summit) and after dark `h153_lilycove_night`, `h154_mt_pyre_summit_night`;
    the scenes `hs168`–`hs177` at the end of `HoennScenes` (the rival in Lilycove, the challenge, the battle, beaten and
    gone, the Lottery Corner, the Cleanse Tag, Team Aqua on the summit, Archie turning, the Red Orb, Team Aqua gone, the
    old woman, the Magma Emblem, the summit after and the old man's tale).
  - **Left for part b**: Team Magma's hideout (Jagged Pass's way in, `locked` in its overlay, opened by the Magma
    Emblem and its guard's flag; the hideout's eight rooms, Tabitha, Maxie and Groudon waking as a scene with no battle,
    `FLAG_GROUDON_AWAKENED_MAGMA_HIDEOUT`, `FLAG_HIDE_MAGMA_HIDEOUT_4F_GROUDON`; `aqua_magma_hideout` and `awaken_legend`),
    Team Aqua's hideout in Lilycove's cove (warp 6, its three floors, the Electrode, Matt, the submarine,
    `FLAG_TEAM_AQUA_ESCAPED_IN_SUBMARINE`, which then hides Lilycove's grunts and Wailmer and turns the town's lines), and
    whether the sea east of Lilycove needs a gate of ours until then; the rival back in their bedroom at home after
    Lilycove; the Berry Master's wife's five rare berries for Emerald's sayings; the museum's curator and gallery and the
    Lilycove Lady (H11); the Fan Club's fans by the player's battles; the motel owner's television trigger; Match Call for
    the rematch trainers of Mt. Pyre and Route 123 (Gabrielle, Valerie, Cameron, Jacki, Fernando); the looks of Archie's
    and Maxie's grunts as Emerald's (the Aqua grunt's stand-in); then the "Done when": the Aqua hideout's last scene, and
    the way to Mossdeep.
  - **Part b, opened** (wave 42; 11 areas, 212 in all): Team Magma's hideout, its eight halls (`MagmaHideout_1F` to
    `_4F`, matrices 273 to 280, `Cave`, `climate: Volcanic`), come to from Jagged Pass by a way that is a wall until the
    rock opens (`switches` from `FLAG_HOENN_MAGMA_HIDEOUT_OPEN`, a flag of ours); Team Aqua's hideout, its three floors
    (`AquaHideout_1F`, `_B1F`, `_B2F`, matrices 210 to 212, `Indoors`, the basements' warp panels the reader's), from
    Lilycove's cove (its warp 6 no longer `locked`).
  - **Part b, played**: Jagged Pass's guard battles once before the emblem (`FLAG_BEAT_MAGMA_GRUNT_JAGGED_PASS`); the
    emblem readies the rock as the pass is come to (`VAR_JAGGED_PASS_STATE` 1) and any of its five triggers shakes it
    open (2); sixteen grunts and Tabitha in Team Magma's halls; Maxie before the sleeping Groudon, the Blue Orb's light,
    Groudon woken, the hall shaken and Groudon gone with no battle, Maxie's battle and Team Magma gone
    (`FLAG_GROUDON_AWAKENED_MAGMA_HIDEOUT`, `FLAG_HIDE_MAGMA_HIDEOUT_GRUNTS`); Captain Stern interviewed by Gabby and Ty
    before Slateport's harbour, the shout from inside and the warp in (`VAR_SLATEPORT_CITY_STATE` 1 to 2); Archie and his
    grunt taking the submarine from its berth from any of the harbour's four triggers, Stern beside himself, the
    hideout's two guards gone from its door (`VAR_SLATEPORT_HARBOR_STATE` 2, `FLAG_MET_TEAM_AQUA_HARBOR`); the guards'
    hints by how far the story has gone; eight grunts, the two Electrode that look like Poké Balls; Matt noticing the
    player, his battle and the submarine sailing (`FLAG_TEAM_AQUA_ESCAPED_IN_SUBMARINE`), which takes Lilycove's grunts
    and its Wailmer away and turns four of its people's lines; the rival home in their bedroom after Lilycove. Every
    scene a `scene`. **No story version raised**: `common.HoennChapterEightB` (called by `HoennNewGame`, and by the
    arrivals of Jagged Pass, Slateport City and Lilycove City, `FLAG_HOENN_CHAPTER_EIGHT_B`). Rulings: "Team Magma's and
    Team Aqua's hideouts".
  - **Part b, decisions**: the channel east of Lilycove needs no gate of ours beyond Emerald's own twelve tiles, which
    the three Wailmer and nine things in the way that are not drawn (`Unseen`) now block, all going with the submarine;
    the submarine is not drawn (Emerald draws only its shadow); the street's crowd keeps its places through the
    interview; `awaken_legend` plays through `music`.
  - **Part b, music** (ours, rendered clash-free with MusicRender, not yet heard): `aqua_magma_hideout` (both hideouts,
    33.1 s loop, −19.9 LUFS), `awaken_legend` (once, 13.3 s, −17.6 LUFS).
  - **Part b, tests** (written, not run): `HoennTests.TheTwoTeamsHideoutsAreOpenWithTheirTrainersAndTheirSong`;
    `HoennStoryTests` (`TheGuardOnJaggedPassBattlesOnceBeforeTheEmblem`, `TheMagmaEmblemShakesTheRockOpenOnJaggedPass`
    from three triggers, `GroudonWakesAndFleesAndMaxieGoesAfterItWithTeamMagma`,
    `TeamAquaTakesCaptainSternsSubmarineFromSlateportsHarbour` from each of the four triggers,
    `TheHideoutsGuardsHintWhereArchieIsByHowFarTheStoryHasGone`, `TwoOfTheBallsInTeamAquasHideoutAreElectrode`,
    `MattKeepsThePlayerBusyWhileTheSubmarineSailsAndLilycovesChannelOpens`, `LilycovesWailmerShutTheChannelOnEmeraldsTwelveTiles`,
    `TheRivalIsHomeInTheirBedroomAfterLilycove`, `ANewGameOfHoennHidesTheHideoutsPeopleAndAnOlderSaveIsGivenThemOnTheWay`,
    `TheSongsOfTheHideoutsAreThere`). The content check finds nothing. `scripts.txt` of the golden files moves (common.txt,
    Jagged Pass, the hideouts' scripts, Slateport City and its harbour, Lilycove City, the rival's bedroom): accept it.
  - **Part b, shots** (written, not taken): the `hoenn` mode's `h155`–`h164` (the hideout's mouth on Jagged Pass, Team
    Magma's halls and Groudon's, with a `_native` crop, Team Aqua's three floors and the panels' maze, with a crop, the
    cove) and the scenes `hs178`–`hs19d` at the end of `HoennScenes` (the rock shaken open, the hideout, Tabitha, Maxie
    and Groudon, the orb's light, Groudon waking and gone, Maxie's battle and Team Magma gone, Stern's interview, the
    shout, the harbour, Archie at the berth, the submarine gone, Stern, Team Aqua's hideout and its panels, an Electrode,
    Matt and his battle, the submarine sailing, the channel open, the rival at home), printing the stretch's flags and
    variables after each stage.
  - **Left after part b**: the Berry Master's wife's five rare berries; the museum's curator and gallery and the Lilycove
    Lady (H11); the Fan Club's fans by the player's battles; the motel owner's television trigger; Match Call for Mt.
    Pyre's and Route 123's rematch trainers; Emerald's own looks for Matt, Tabitha's grunts and the female grunts (stand-ins
    of the teams' grunts); the submarine's shadow as a picture; Emerald's Jagged Pass ash after the hideout
    (`VAR_JAGGED_PASS_ASH_WEATHER`); the crowd round Stern's interview; the Pichu doll in the rival's room; the harbour's
    patrons back after Mossdeep's Gym (H8).
- [ ] H8 Mossdeep, the Seafloor Cavern and Sootopolis (part a done 2026-10-24, wave 43: Routes 125 to 127, Mossdeep City
  to Dive; part b done 2026-10-24, wave 44: Route 128, the Seafloor Cavern, Kyogre, Sootopolis and its clash, Wallace;
  tests and shots written, not run; part c done 2026-10-24, wave 45: Routes 129 to 131, Pacifidlog, Sky Pillar and
  Rayquaza, Sootopolis calmed, Waterfall and Juan's Rain Badge; what is left is listed under "Left for part d")
  - **Opened** (18 areas, 230 in all): Routes 125, 126 and 127 above water and the sea floors under them
    (`underwater_route_126` and `_127` on `HoennUnderwater`; Route 125's floor, matrix 5, is the post-game's: Route 125 has no dark
    water until Emerald's abnormal weather lays it for the Marine Cave, plan 21 · H12), Mossdeep City (`Architecture.Starward`, the style guide's row first; its open
    metatiles sandy paths in `GbaCover.HoennRanges`) with its Gym, Pokémon Center on both floors, Mart (Emerald's stock,
    marts.json's `mossdeep`), Steven's house, the Space Center's two floors and four houses, and the treasure hunter's
    house on Route 124 (its `locked` taken off). A spawn for Mossdeep (9). Shut: Mossdeep's Game Corner (Emerald's room
    of link games), Shoal Cave (Route 125's warp `locked`, part b), Route 128 and on, Sootopolis, the Marine Cave's ways
    from the floors. Every map is in `RegionDatabase`'s Hoenn list. Emerald's shallows are now water to look at over
    all of Hoenn (`GbaCover.Of`; twenty chunks of the earlier areas moved, the shallows of Routes 109 and Lavaridge's
    springs among them); Route 124 plays `hoenn/route120`, as the whole eastern sea does.
  - **Played**: Scott wondering about the town and leaving (`VAR_SCOTT_STATE`); the King's Rock, the Super Rod
    (`FLAG_HOENN_RECEIVED_SUPER_ROD`), the Sun Stone and Dynamic Punch once each; Tate and Liza as one trainer of two
    (`hoenn_tate_and_liza_1`, now "Tate & Liza"), the Mind Badge, TM04, their Match Call, the Gym's twelve trainers
    beaten, the harbour's patrons back in Slateport and Mr. Briney at Stern's shipyard; the Gym's floor of arrows
    (`MossdeepTiles`, the new command `rotatetiles`, the statues things of the map, `GymStatue`); Team Magma filing into
    the Space Center (`VAR_MOSSDEEP_CITY_STATE` 1 to 2), the visitors pressed back, the grunt on the stairs stepping
    aside, the three grunts upstairs one after another (or the player sent back down), Steven and Maxie, the battle
    beside Steven against Maxie and Tabitha (`hoenn_steven_partner`, Emerald's `sStevenMons`), Maxie's doubt and Team
    Magma gone (state 3), Steven at home and **HM09, Dive** (Platinum's eight HMs keep their numbers; `Importer.
    AddHoennMachines`), the Wingull's errand finished and the Mental Herb, the treasure hunter's four trades, Ernest,
    Pablo and Koji in Match Call. Every scene a `scene`. **No story version raised**: `common.HoennChapterNine`
    (`FLAG_HOENN_CHAPTER_NINE`), called by `HoennNewGame` and by Mossdeep City's arrival. Rulings: "Mossdeep City".
  - **Engine**: `Overworld/MossdeepTiles.cs` (a `GymPuzzle`, its arrows and switches; `Turns`, `Anticlockwise`), the
    script command `rotatetiles <colour>` (`Op.RotateTiles` at the enum's end, both hosts' `RotateTiles`, `StopsAHurry`,
    `ScriptTests.EveryCommand`, `docs/scripts.md`), `NPC.GymStatueType` and `PropType.GymStatue` (at the enum's end)
    with `GymArt.PaintGymStatue`, the arrows and switches drawn by `GymPieces` (`GymArt.PaintFloorArrow`,
    `PaintFloorSwitch`; the style guide's "The Mossdeep Gym's arrows"), `Architecture.Starward`, `MoveTutors.Kanto`'s
    Dynamic Punch, `TrainerDatabase.OthersOfTheRivalsClass` (Steven and Wally keep their names), the reader's
    `GbaTables.EmeraldPartners` and a pair's names kept apart at an ampersand (`Titled`). Items: HM09 (id 9113, its
    Romanian name and description written).
  - **Looks**: `tate` and `liza` in `characters.json` (our own takes). No new music: Mossdeep and its rooms play
    `hoenn/rustboro` and the eastern sea `hoenn/route120`, as Emerald's do.
  - **Tests** (written, not run): `HoennTests` (`MossdeepCityIsOpenWithItsFlySpotItsGymAndItsRooms`,
    `TheTwinsAndStevensTeamOfTheSpaceCenterAreEmeralds`, `TheMossdeepGymsSwitchesTurnWhoeverStandsOnTheirArrows`, and
    `TheSeaFloorLiesTileForTileUnderTheSeaItIsDivedTo`); `HoennStoryTests`
    (`TheTwinsBattleTogetherForTheMindBadgeAndTeamMagmaComesForTheFuel`,
    `TheGymsSwitchesMoveThePeopleOnTheirArrowsUntilTheRoomIsComeToAgain`,
    `StevenAndThePlayerBeatMaxieAndTabithaAndStevenGivesDiveAtHome`, `TheThreeGruntsSendAPlayerWhoSaysNoBackDownstairs`,
    `FortreesWingullDeliversItsLetterInMossdeepAndTheBoyGivesTheMentalHerb`,
    `MossdeepsPeopleGiveTheirGiftsOnceAndScottGoesOnHisWay`, `TheTreasureHunterTradesAStoneForEachShard`,
    `ANewGameOfHoennHidesTheNinthStretchsPeopleAndAnOlderSaveIsGivenThemInMossdeep`); `ScriptTests.EveryCommand`
    (`rotatetiles`). The content check finds nothing. `scripts.txt` of the golden files moves (common.txt, the new
    places' scripts, Fortree's fourth house, Stern's shipyard, phone.txt): accept it. `species.json` moved (HM09 on
    every species that learns Dive by a machine), `items.json`, `marts.json` and `coverage.md` with it.
  - **Shots** (written, not taken): the `hoenn` mode's `h165`–`h178` (Mossdeep's streets, the Space Center from
    outside and the east shore, the Gym from its door, its arrows with a `_native` crop and the twins' corner, the
    Space Center's two floors, Steven's house, the Center, the Mart, a house, the treasure hunter's house, Route 127's
    sandbar) and `h179_mossdeep_night`; the scenes `hs200`–`hs226` at the end of `HoennScenes` (Scott, the yellow
    switch before and as it turns, Tate and Liza and their double battle, the Mind Badge, Team Magma filing in, the held
    ground floor and its stairs, the three grunts, Maxie's plan, the battle beside Steven, Maxie's doubt, Steven's thanks,
    HM09 given at home, the Wingull flying home and the Mental Herb, the treasure hunter). `HoennSeaShots` keeps its
    diver on Route 124's own dark water.
  - **Fixed after the merge** (wave 43): the Gym's script had left out clearing Mr. Briney's shipyard flag (Emerald clears
    it); the people a switch moves stand still and are not laid out again by the field, so `MossdeepTiles` notes where
    each stood and puts them back as the room is come into (`Remember`, `Arrive`; the test game's arrival now lays out a
    room's puzzle as `GameEngine.ArrivePuzzle` does); and Route 125's sea floor was taken off again, since Route 125 has
    no dark water before the post-game and nobody could reach it.
  - **Left for part b**: Shoal Cave and its tide (two layouts by the hour through a new command behind the warp's fade,
    as the plan says; its Shoal Salt and Shells and the Shell Bell), Route 128 and its sea floor, the Seafloor Cavern by
    Dive (its guard, `FLAG_HIDE_SEAFLOOR_CAVERN_ENTRANCE_AQUA_GRUNT`, already set by Steven's gift) and Kyogre's waking,
    Sootopolis reached by surfacing in its crater (matrix 1, `Underwater_SootopolisCity`), the titans' clash under
    `HeavyRain` and a new `FieldWeather.Drought` (the style guide's "Life" first), Wallace, the Cave of Origin, Routes
    129 to 131 and Pacifidlog, Sky Pillar and Rayquaza's descent, Juan and the Rain Badge, the songs `sootopolis`,
    `abnormal_weather` and `rayquaza_appears`, and Sootopolis's and Pacifidlog's `Architecture` rows; Rayquaza's Match
    Call warning from the rival that Emerald plays on Mossdeep's Space Center's second floor
    (`MossdeepCity_SpaceCenter_2F_EventScript_RivalRayquazaCall`); the Space Center's launch count by the game's weeks;
    Emerald's choice of three Pokémon for the battle beside Steven; the Beldum and Steven's letter (written, shown by the
    Hall of Fame, H9); the Ancient Tomb's Registeel and the Sealed Chamber (H12 or later).
  - **Part b** (done 2026-10-24, wave 44: Route 128 to the Cave of Origin; tests and shots written, not run; what is left
    is listed under "Left for part c")
  - **Opened** (29 areas, 259 in all): Route 128 and its sea floor (`underwater_route_128` on `HoennUnderwater`), the
    Seafloor Cavern (the pool under the sea before it, `Underwater_SeafloorCavern`, matrix 213, a floor of its own come to
    by the passage of Route 128's floor; its mouth and nine rooms, matrices 214 to 223), Sootopolis City (matrix 1, a map
    of its own, `Architecture.Crater`, the style guide's row first; its white stone floor paving in `GbaCover.HoennRanges`)
    with the pool under its harbour (`Underwater_SootopolisCity`, matrix 192, joined to Route 126's floor), its Pokémon
    Center on both floors, Mart (Emerald's stock, marts.json's `sootopolis`) and eight houses, and the Cave of Origin's
    three rooms (the Ruby and Sapphire rooms between them left out, as Emerald's warps leave them). Every map is in
    `RegionDatabase`'s Hoenn list. Shut: Juan's Gym and the Mystery Events house (`locked`), Sootopolis's houses while the
    titans are in the city (`switches` until `FLAG_LEGENDARIES_IN_SOOTOPOLIS`), Shoal Cave, Routes 129 to 131 and on.
  - **Played**: Archie in Kyogre's hall, his battle (`hoenn_archie`), the Red Orb, Kyogre waking and gone (no battle, as
    Groudon's), Archie's call from outside and Maxie coming in (`VAR_SEAFLOOR_CAVERN_STATE` 1,
    `FLAG_KYOGRE_ESCAPED_SEAFLOOR_CAVERN`, `FLAG_LEGENDARIES_IN_SOOTOPOLIS`, `FLAG_SYS_WEATHER_CTRL`, Sootopolis's people
    shown, the cavern's grunts gone, Steven gone from his house); Route 128's islet: Archie and Maxie leaving to stop it,
    Steven flying in and on (`VAR_ROUTE128_STATE` 2); Shelly and the cavern's five grunts with lines of our own; the
    titans' clash over Sootopolis as the city is first come to (`VAR_SOOTOPOLIS_CITY_STATE` 2, the camera to the harbour,
    three strikes, the weather breaking: `FLAG_HOENN_SOOTOPOLIS_CLASH_SEEN`, ours); the townsfolk watching from the
    harbour's edge; Steven leading the player into the Cave of Origin (`FLAG_STEVEN_GUIDES_TO_CAVE_OF_ORIGIN`) and the old
    man stepping aside afterwards for a player from the south; Wallace's tale and question, and his going to Sky Pillar
    (`FLAG_WALLACE_GOES_TO_SKY_PILLAR`, state 3, `FLAG_HIDE_SKY_PILLAR_WALLACE` cleared); Kiri's two berries a day, TM31
    from the black belt, Double-Edge from the Pokémon Center's woman, the Seedot and Lotad brothers' records and Elixirs,
    Route 128's two triathletes in Match Call (Isaiah, Katelyn), the stolen submarine's hull read in the pool. Every scene a
    `scene`. **No story version raised**: `common.HoennChapterTen` (`FLAG_HOENN_CHAPTER_TEN`), called by `HoennNewGame`
    and by the arrivals of Route 128, Kyogre's hall and Sootopolis. Rulings: "The Seafloor Cavern and Sootopolis".
  - **Engine**: a floor that is no map of the sea's is dived to and come up from at one place, Emerald's `setdivewarp`
    (the overlay's `diveTo` and `emergeTo`, `OverlaySpot`; `MapArea.DiveSpot`, `EmergeSpot`; `Map.DiveTargetAt`,
    `EmergeTargetAt`, which `GameEngine.Dive`, the headless host and `WorldWalk` use; a warp along the sea floor keeps the
    walk's diver diving); overlay zones that hold only `while` a story flag (`OverlayZone.While`, `Follow`, kept by
    `Map.ApplyPresence`); `FieldWeather.Drought` and `Abnormal` at the enum's end (`Weathers.Alternating`, ten seconds of
    the field's clock a turn, `Map.WeatherClock` set by the game each frame from `FieldLife.Now`; `ArtLook.Drought`, the
    harsh sun laid over the hour at seven tenths; a drought's battle opens in the sun; the style guide's "Life" rows
    first); the abnormal weather over Routes 124 to 128, Lilycove and Mossdeep while `FLAG_SYS_WEATHER_CTRL`, and over
    Sootopolis the downpour from the titans' coming and the two by turns from the clash. `Architecture.Crater`.
  - **Looks and music**: `wallace` in `characters.json` (our own take). `hoenn/sootopolis` (43.6 s loop, −19.9 LUFS) and
    `hoenn/abnormal_weather` (27.8 s loop, −20.0 LUFS), ours, rendered clash-free with MusicRender, not yet heard. The
    Seafloor Cavern plays `hoenn/mt_chimney` and the Cave of Origin `hoenn/cave_of_origin`, as Emerald's do.
  - **Tests** (written, not run): `HoennTests` (`TheSeafloorCavernAndSootopolisCityAreComeToByDive`,
    `TheTitansWeatherTakesTurnsOverTheEasternSeaWhileTheStoryKeepsIt`); `HoennStoryTests`
    (`ArchieWakesKyogreInTheSeafloorCavernAndStevenFliesToSootopolis`,
    `GroudonAndKyogreClashOverSootopolisAndStevenTakesThePlayerToWallace`,
    `SootopolissPeopleGiveTheirGiftsOnceAndItsBrothersMeasureTheirFavourites`,
    `ANewGameOfHoennHidesTheTenthStretchsPeopleAndAnOlderSaveIsGivenThemOnTheWay`); `WorldWalk` follows the one-place dives
    and keeps a diver diving through a warp along the floor, so `TheWalkFromTheStartReachesEveryOpenAreaAndEveryRoom` now
    reaches Sootopolis and the cavern. The content check finds nothing. `scripts.txt` of the golden files moves (common.txt,
    the new places' scripts, phone.txt; the headless host's dive line now names the tile it lands on): accept it.
    `marts.json` moved; `lines.json`, `cast.json`, `coverage.md` and `localisation.md` regenerated (the voice catalogue
    recast ten lines of five trainers of other regions who share a name with Route 128's new people). The
    Romanian file of Route 125's sea floor, which part a's fix left with no overlay, is removed.
  - **Shots** (written, not taken): the `hoenn` mode's `h180`–`h194` (Route 128's islet, the cavern's pool, mouth,
    boulders, Shelly's room and Kyogre's hall, Sootopolis with `_native` crops of the city and the harbour, the cave's
    mouth, the pool under the harbour, the Center, the Mart, a house, the Cave of Origin's mouth and its lowest floor) and
    `h195_sootopolis_night`; the scenes `hs230`–`hs24b` at the end of `HoennScenes` (the pool and the mouth, Shelly,
    Kyogre asleep, Archie and his battle, the Red Orb, Kyogre waking and gone, Maxie, Route 128's storm, Archie and Maxie,
    Steven flying in, Sootopolis's titans and their clash, the downpour and the drought with a `_native` crop, Steven's
    guide, the Cave of Origin, Wallace and his question, the old man aside), printing the stretch's state and the weather
    after each stage.
  - **Left for part c**: Routes 129 to 131 and their sea floors, Pacifidlog Town (its houses on logs, decks over `Sea`,
    and its `Architecture` row, the style guide first), Sky Pillar (Route 131's outside, Wallace at the door: state 4,
    `FLAG_HIDE_SKY_PILLAR_WALLACE` already cleared), its floors and top, Rayquaza woken and its descent over Sootopolis
    (`VAR_SKY_PILLAR_STATE`, state 5; the titans gone; **clear `FLAG_HOENN_SOOTOPOLIS_CLASH_SEEN` with
    `FLAG_SYS_WEATHER_CTRL` and `FLAG_LEGENDARIES_IN_SOOTOPOLIS`**, or the city keeps its weather), the song
    `rayquaza_appears`, Maxie and Archie leaving (`FLAG_SOOTOPOLIS_ARCHIE_MAXIE_LEAVE`, state 6, the houses open again),
    Wallace's HM07 Waterfall and Juan's Gym (its ice floor) with the Rain Badge; the abnormal weather's other places
    (Routes 129 to 131, Ever Grande City, Sky Pillar's outside: zones like Route 128's); Shoal Cave and its tide (two
    layouts by the hour through a new command behind the warp's fade; the Shoal Salt and Shells and the Shell Bell);
    Sootopolis's spawn for Fly and a white-out (Emerald's heal location 10, on a map of its own, which `RegionSpawn` can't
    yet name: with part c or plan 18 · W9); Emerald's clash layout (`LAYOUT_SOOTOPOLIS_CITY_LEGENDS_BATTLE`, rough water
    under the titans) and the rough water Rayquaza's scene lays; Steven's walk across the terraces in his guide (a fade
    here); the Wailmer Doll (H10); and part a's leftovers (the rival's Rayquaza call on the Space Center's second floor,
    the launch count by weeks, Emerald's choice of three beside Steven, the Beldum and the letter, the Ancient Tomb).
  - **Part c** (done 2026-10-24, wave 45: Sky Pillar to the Rain Badge; tests written and run with `--filter`, the
    `hoenn` mode shot once; what is left is listed under "Left for part d")
  - **Opened** (21 areas, 280 in all): Routes 129, 130 and 131 (Route 131 laid with Sky Pillar's island, as its own script
    always lays it), Pacifidlog Town (`Architecture.Raft`, the style guide's row first; its log walkways a deck of logs in
    `GbaCover.HoennRanges`) with its Pokémon Center on both floors and five houses, Sky Pillar's cave, island, five floors
    and top (laid clean, `GbaMaps.FirstLayouts`; its stone floors and rock walls in `GbaCover.HoennRanges`), and Juan's Gym
    with its basement. Every map is in `RegionDatabase`'s Hoenn list. Shut: Route 132 and on, Shoal Cave, Mirage Island,
    the Mystery Events house, the sea floor under Route 129 (the Marine Cave's, H12).
  - **Played**: Wallace at Sky Pillar's door, the quake, the weather spreading and Wallace gone back to Sootopolis (state
    4); the door into the tower (the overlay's `doors`, opened by `FLAG_WALLACE_GOES_TO_SKY_PILLAR`); the fourth floor's
    cracked floor dropping into the third's middle; Rayquaza woken on the top (state 5, `VAR_SKY_PILLAR_STATE` 1) and flown
    off; Rayquaza over Sootopolis's harbour, the titans gone and the weather cleared (`FLAG_SYS_WEATHER_CTRL`,
    `FLAG_HOENN_SOOTOPOLIS_CLASH_SEEN`, `FLAG_LEGENDARIES_IN_SOOTOPOLIS` cleared, `VAR_SKY_PILLAR_STATE` 2) to the new
    `hoenn/rayquaza_appears`; Steven, Wallace, Archie and Maxie by the Gym, the townsfolk's lines of Rayquaza, Archie and
    Maxie leaving (`FLAG_SOOTOPOLIS_ARCHIE_MAXIE_LEAVE`, which opens the Gym), Wallace's HM07 (`FLAG_RECEIVED_HM_WATERFALL`)
    and his step aside (`VAR_SOOTOPOLIS_WALLACE_STATE`); Juan's ice, his battle (`hoenn_juan_1`), the Rain Badge, TM03,
    the basement's ten trainers beaten, his Match Call, and the city calm for good (state 6). Every scene a `scene`. **No
    story version raised**: `common.HoennChapterEleven` (`FLAG_HOENN_CHAPTER_ELEVEN`). Rulings: "Sky Pillar, Rayquaza and
    the Rain Badge".
  - **Engine**: `Overworld/SootopolisIce.cs` (a `GymPuzzle`: `StepOn`, `Count`, `FlightsOpen`, `Cracked`, `Glazed`; the
    engine tells it each tile the player comes onto, `GameEngine.WalkTheIce`, and drops a faller to the basement), drawn by
    `GymPieces.BuildSootopolis` (`GymArt.PaintIceCrack`, `PaintIceGlaze`; the style guide's "The Sootopolis Gym's ice");
    cracked floors that drop to the floor below (the overlay's `holeTo`, `MapArea.HoleTo`, `Map.HoleTargetAt`,
    `GameEngine.FallThroughCrackedFloor`, followed by `WorldWalk`); `TileBehavior.ThinIce` and `CrackedFloor` ruled;
    `RegionSpawn.Map` (a spawn on a map of its own: Sootopolis, which Fly reaches from the town map's middle of Route 126,
    and the importer writes it for every heal location off the overworld) and Fly's places reachable on their own maps
    (`GameEngine.Reachable`); `Architecture.Raft`. The GBA import: water a bridge crosses at the lowest level of its
    shores and a bridge's run read to its ends (Route 120's, 119's and 104's decks), an object of a bridge's elevation on
    its deck (Route 120's Steven, Route 119's rival on his bike), a plank of no behaviour on a deck drawn as the deck, and
    the shallows out at sea a sandbar to look at (Route 128's islet); 16 chunks of the earlier areas moved for it, and
    the harness's `TalkTo` speaks to Steven from the deck again.
  - **Looks and music**: `juan` in `characters.json` (our own take); `hoenn/rayquaza_appears` (17.1 s, played once,
    −18.8 LUFS, clash-free, not yet heard).
  - **Tests** (run with `--filter`): `HoennStoryTests` (`RayquazaWakesOnSkyPillarAndComesDownOverSootopolisEndingTheClash`,
    `TheSootopolisGymsIceOpensItsStairsFieldByFieldAndDropsWhoeverStepsTwice`, `JuanBattlesForTheRainBadgeAndCalmsSootopolisForGood`),
    `HoennTests` (`PacifidlogSkyPillarAndJuansGymAreOpen`, `RouteOneTwentysBridgeIsADeckOverItsPondAndStevenStandsOnIt`; the
    walk of the whole game reaches every new room, down through the fourth floor's crack), `FieldMovementTests` (thin ice
    and a cracked floor ruled). Golden `scripts.txt` moves (common.txt, the new places' scripts, Sootopolis's, phone.txt);
    `RomanianTests.TheTownMapSaysWhatEachPlaceIsInRomanian` wants Sootopolis's new town map line in Romanian (the merge's).
  - **Shots**: the `hoenn` mode's `h196`–`h209` (Route 129, Pacifidlog and two rooms, Route 131's beach before the cave,
    Sky Pillar's cave, island, first and fourth floors and top, Juan's Gym and its basement, Route 120's bridge from its
    deck, Route 128's sandbar) and `h210_pacifidlog_night`; the scenes `hs250`–`hs276` at the end of `HoennScenes`
    (Wallace at Sky Pillar and the quake, the open door, the first floor, the fall through the fourth, Rayquaza asleep,
    stirring and gone, Rayquaza over Sootopolis with a `_native` crop, the calm, Maxie, Archie, Wallace's HM07, the Gym of
    ice, the first field crossed and the fall, Juan, his battle and the Rain Badge).
  - **Left for part d**: Shoal Cave and its tide (two layouts by the hour through a new command behind the warp's fade;
    the Shoal Salt and Shells and the Shell Bell); Route 132 to 134 back to Slateport; Emerald's clash layout and the rough
    water Rayquaza's scene lays, and its white flash; Archie and Maxie at Mt. Pyre's summit giving back the orbs
    (`VAR_MT_PYRE_STATE` 2); Pacifidlog's daily Return or Frustration, its trade and the Explosion tutor; Mirage Island;
    the abnormal weather over Ever Grande City (H9 opens it); the Mach Bike's climb of the cracked floors and Rayquaza's
    battle (H12); and parts a's and b's leftovers above but Sootopolis's spawn, which is done.
- [ ] H9 Ever Grande and the League
- [ ] H10 Secret Bases
- [ ] H11 The Contest Hall and Pokéblocks
- [ ] H12 The Frontier island and the post-game
