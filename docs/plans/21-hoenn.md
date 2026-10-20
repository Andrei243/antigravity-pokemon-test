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
- [ ] H6 Route 118 to Fortree (part a done 2026-10-20, wave 38: Route 118 to Winona and Route 120; part b, Route 121
  and the Safari Zone, left)
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
- [ ] H7 Lilycove, Mt. Pyre and the hideouts
- [ ] H8 Mossdeep, the Seafloor Cavern and Sootopolis
- [ ] H9 Ever Grande and the League
- [ ] H10 Secret Bases
- [ ] H11 The Contest Hall and Pokéblocks
- [ ] H12 The Frontier island and the post-game
