# MapImporter

Reads the layout of Platinum's Sinnoh from the [pret/pokeplatinum](https://github.com/pret/pokeplatinum) decompilation and writes it out three ways:

- **pictures** for checking the import by eye: the whole overworld, every chunk, every area, and any area that still exists as a hand-made map side by side with it
- **world files**: the game's own format for chunks, matrices and areas (`docs/data-files.md`, "World files")
- **reports**: `docs/tile-behaviours.md` (every tile behaviour the maps use) and `report.md` (what is still unsorted, and how much of each open area is in the game)

```bash
dotnet run --project tools/MapImporter                      # fetch the pinned decompilation once, then import everything
dotnet run --project tools/MapImporter -- --data            # only rewrite the world files the game loads (PokemonPlatinumEngine/Data/world/sinnoh)
dotnet run --project tools/MapImporter -- --quick           # the three mosaics, the comparisons and the reports only
dotnet run --project tools/MapImporter -- --out <dir>       # write somewhere other than tools/MapImporter/out
dotnet run --project tools/MapImporter -- --decomp <dir>    # read an existing pokeplatinum checkout
```

A full run takes about twelve seconds. It needs `git` on the path the first time, to fetch the map folders of the decompilation into `tools/MapImporter/.cache` (about 50 MB). Both that folder and `out/` are ignored by git. The commit it reads is the one pinned in `tools/DataImporter/Sources.cs`, shared with the data importer.

## The game's own world files

All of Sinnoh is 8.7 MB of world files; the game has only what is built. `Data/world/sinnoh/world.json`, written by hand, lists the maps and the areas that are open. `--data` reads it and writes, beside it, the matrices of those maps, the chunks of the open areas and of every chunk next to one (they are in view from the edge), and the open areas' own files, each with its land encounters, `habitats.json`, where every wild Pokémon of the region lives (every area's grass by time of day, water and rods, and where each area is on the overworld) for the Pokédex's area page, and `calendar.json`, Platinum's weather for each day of the year in the five places that follow a calendar. It deletes generated files that are no longer asked for and never touches `world.json` or `overlays/`. It takes a third of a second, writes the same bytes every time, and ends with an error if an open area's wild Pokémon name a species the game doesn't have. To open an area: add its key to `world.json`, run `--data`, write `overlays/<key>.json`, run the tests.

## What it writes

| In `out/` | What |
| --- | --- |
| `sinnoh.png` | The overworld, four pixels per tile, coloured by what each tile looks like, with area names, people, warps and a key |
| `sinnoh_behaviours.png` | The same, coloured by what each tile does |
| `sinnoh_heights.png` | The overworld shaded by height |
| `compare/<area>.png` | An imported area next to the hand-made map that still stands in for it (none today: Jubilife City, the last, opened in plan 01 · M5) |
| `areas/<key>.png` | Every area close up, eight pixels per tile, with its events |
| `chunks/NNN.png` | Every chunk by behaviour, with the footprints of its props |
| `world/matrices`, `world/chunks`, `world/areas`, `world/habitats.json`, `world/calendar.json` | The world files |
| `report.md` | Counts; for each open area, how many of the original's people, warps and signs are in the game; the comparison in numbers; every texture name with the ground it was sorted into; every prop model; problems |

It also rewrites `docs/tile-behaviours.md` in the repository.

## What is read, and what is not

| What | From | Kept |
| --- | --- | --- |
| The grids of chunks, the area of each chunk, altitudes | `res/field/matrices/map_matrix_NNN.json` | all of it |
| Areas: name, music, weather, camera, flags | `include/data/map_headers.h`, `res/text/location_names.json` | all of it, under our own names |
| People, warps, signs, triggers | `res/field/events/events_<area>.json` | positions, looks, movement, script numbers, how far a trainer sees; no script text |
| What a model is called and how large it is | `res/field/props/models/*.nsbmd` (the name and the bounding box only) | `docs/world-models.md`: every model that stands outdoors, with its size, where it stands and what the game puts in its place (`Data/WorldModels.cs`). A model the catalogue doesn't know is listed under Problems |
| What lies on the ground and what is hidden in it | `res/field/scripts/scripts_visible_items.s` (the item and the number each item ball's script sets), `include/data/field/hidden_items.h` (the table of hidden items), `generated/items.txt` and `generated/vars_flags.txt` (the numbers of items and of the hidden items' flags) | for each ball and each hidden place: the item, by the game's own name for it, and how many; for a hidden item also its flag and the Dowsing Machine's range. No line of the scripts' text |
| The weather calendar | `src/field_overworld_weather.c` (`sYearlyWeather`) | the weather of each of its five places on each of the 366 days |
| Wild Pokémon on land and on water | `res/field/encounters/encounters_<area>.json` | the twelve base land slots (species and level), the five water slots (species and a range of levels) and each table's rate |
| Tile behaviours and the blocked flag | `res/field/maps/data/map_data_NNN.bin`, first section | all of it |
| Props (buildings, signboards, furniture) | the same file, second section, and `res/field/props/models/*.nsbmd` | the model's id, its short name and its bounding box |
| The chunk's 3D model | the same file, third section | for each tile, the *name* of the texture drawn there, turned into a kind of ground |
| Heights | the same file, fourth section ("BDHC") | every plate, in tiles |
| The Distortion World's own space and its floating floors | `res/prebuilt/fielddata/tornworld/tw_arc.narc` (member 0: where each floor lies; each floor's file: its floating platforms) and `tw_arc_attr.narc` (their tile attributes) | each floor's place, subtracted from its events; the platforms of the floor kind, as open tiles with their behaviours and a plate at their height. Not the walls and ceilings, nor anything drawn |

No vertex, texture, palette or animation leaves the cache. From a model the importer keeps a name and a box (props), or the names of textures and where their polygons lie (terrain); the vertices are read only to find which tiles a polygon covers. What the game draws on those tiles is our own art.

The layouts of the binary files were confirmed against the game's own loaders in the decompilation: `src/overlay005/land_data.c` (the four sections), `map_prop.c` (props, 48 bytes each), `bdhc.c` and `docs/maps/bdhc.md` (heights), `src/terrain_collision_manager.c` (behaviour in the low byte, the blocked flag in the top bit, and the 1.25-tile step limit), `src/map_matrix.c` (matrices and altitudes), `src/overlay009/ov9_02249960.c` (the Distortion World's archives: its map list of 12-byte entries, its floating platforms of 20 bytes each, and their attributes read a row of `tileCountVertical` to each step south).

## How a tile gets its look

`Cover.cs` decides, in this order:

1. A blocked tile under the box of a prop as large as a building is `Building`.
2. A behaviour that shows (water, tall grass, sand, snow, ice, marsh, cave floor, bridges) decides.
3. Flowers that stand over lawn are flowers: Floaroma Meadow is a lawn with a sheet of flowers over every tile of it, and the lawn is only the ground they grow in.
4. Otherwise the texture drawn there does. `TerrainModel` sorts each chunk's polygons into those lying on the walking surface (the ground) and those above it (a tree's crown, a rock), and a short list of name patterns turns a texture name into a `TerrainCover`: `ngrass` is lawn, `nsand` a path, `tree01` and `conttree_b` trees, `criff` a rock face, `dhole` the dark in the mouth of a cave. A blocked tile shows what stands on it, an open tile its ground.
5. A name no pattern knows leaves the tile `Unknown`. `report.md` lists those names with their tile counts, the overworld's first.

On the overworld 99.5% of tiles are sorted. When a session builds an area whose textures are still in the list, add their patterns to `Cover.Rules`, run the importer and look at the area's picture.

## The Game Boy Advance's maps (plan 18 · W6)

```bash
dotnet run --project tools/MapImporter -- --game firered [--quick]   # Kanto, from pret/pokefirered, into out/kanto/
dotnet run --project tools/MapImporter -- --game emerald [--quick]   # Hoenn, from pret/pokeemerald, into out/hoenn/
dotnet run --project tools/MapImporter -- --game firered --data      # what Data/world/kanto/world.json opens, into the game's data (plan 18 · W7)
```

`tools/MapImporter/Gba/` reads the GBA decompilations beside `DecompMaps`, sharing nothing with it but the world files' format. Each game's commit is pinned in `tools/DataImporter/Sources.cs` (`FireRedCommit`, `EmeraldCommit`) and fetched once, sparsely (`Sources.GbaFolders`), into `.cache/pokefirered` and `.cache/pokeemerald` (13 and 23 MB). A run takes a few seconds and writes:

| In `out/<region>/` | What |
| --- | --- |
| `<region>.png`, `<region>_behaviours.png`, `<region>_heights.png` | The overworld (matrix 0), four pixels a tile (two for heights), by cover, by behaviour and by height, each map outlined and named |
| `maps/<Map>.png` | Every map by behaviour and by cover, side by side (not with `--quick`) |
| `world/` | The matrices, chunks and areas with their events and wild Pokémon, the region's `trainers.json`, `spawns.json` and `habitats.json`, and a `world.json` that makes a map of every matrix (a house's room `Indoors`) and opens every area, so a tool can build it with `World.Open` (not with `--quick`). The harness's `area kanto:pallet_town` builds and shoots an area of it and the rooms its doors lead into. `--data` writes the same into `PokemonPlatinumEngine/Data/world/<region>/` for the areas that folder's own `world.json` opens, and nothing while it has none |
| `report.md` | Problems (connections that disagree, maps that overlap, layouts left out), every behaviour used with what it becomes, the metatiles with no look per tileset, the overworld's elevations, the matrices |

It also rewrites the section "The GBA's behaviours" of `docs/tile-behaviours.md`, from both games (the other is fetched if it isn't there); the Sinnoh run keeps that section as it stands.

| What | From | Kept |
| --- | --- | --- |
| The maps and their order | `data/maps/map_groups.json` | the order of the groups and of each group's maps |
| A map | `data/maps/<Map>/map.json` | its id, name, layout, music, region map section, weather, map type, the cycling, running and escape flags, its `connections` (map, offset, direction), and its four event lists (W7): each object's looks, tile, movement, range, trainer type and sight, script label and flag (a `clone` of a neighbour's object is left out); each warp's tile and destination; each trigger's tile, variable, value and script label; each sign's tile and script label, each hidden item's item, count and flag. Secret bases' spots are left for plan 21 |
| What an item's ball holds | `data/scripts/item_ball_scripts.inc` | each label's `finditem` item and count, nothing else of the file |
| Which trainer a trainer's script battles | each map's `scripts.inc` (Emerald) and `data/scripts/trainers.inc` (FireRed) | each label's first `trainerbattle…` line's trainer constant; the text it names is never read |
| The wild Pokémon | `src/data/wild_encounters.json` | each map's land, water, Rock Smash and fishing tables (the rods' slots by the header's groups, every slot's weight from the header's rates), FireRed's own and not LeafGreen's, the first of a map's several (Altering Cave's) |
| The trainers | `src/data/trainers.h`, `src/data/trainer_parties.h` | each trainer's constant, name, class, AI flags, items, double battle and party (species, level, IV, held item, moves); a placeholder party (`DUMMY_TRAINER_MON`) leaves its trainer out. Prize money and personalities are not worked out (their tables aren't fetched) |
| The heal locations | `src/data/heal_locations.json` | each one's map, tile and the room a white-out wakes in |
| Layouts | `data/layouts/layouts.json`, each layout's `map.bin` | its size, its two tilesets, and every block: ten bits of metatile, two of collision, four of elevation |
| A metatile's behaviour | each tileset's `metatile_attributes.bin`, found through `src/data/tilesets/headers.h` and `metatiles.h` | the behaviour's bits alone (two bytes a metatile in Emerald, four in FireRed); nothing about how it is drawn |
| The behaviours' names | `include/constants/metatile_behaviors.h` | FireRed's `#define`s and Emerald's `enum` |
| The metatiles' labels | `include/constants/metatile_labels.h` | a label's words, which sort a metatile into a kind of ground |
| How many metatiles a primary tileset holds | `include/fieldmap.h` | `NUM_METATILES_IN_PRIMARY` (640 in FireRed, 512 in Emerald) |

Never `tiles.png`, a palette, `metatiles.bin` or anything else a tileset draws with: the folder patterns can't reach them, and no vertex, texture or palette reaches the repository. Emerald's `scripts.inc` hold the maps' lines too; they stay in the git-ignored cache, and only a label and a trainer's constant are read out of them.

- **Behaviours** (`GbaBehaviours.cs`): each `MB_*` by its name onto a `TileBehavior`: Platinum's own value where it has one that means the same (`MB_JUMP_NORTH` is Platinum's jump north, `0x3A`, and the four sides and corners of `MB_IMPASSABLE_*` Platinum's blocks), one of the GBA's own at `0xED` to `0xFD` where it has none (deep water, the sea floor, seaweed, thin ice, a cracked floor, a hole, the four currents, spinners, a secret base's spot, the Acro Bike's rails, a bumpy slope, the Cycling Road's pull, no running), and `None` for what only says how a tile is read or drawn. Collision bits set make the tile blocked.
- **Heights** (`GbaWorld.Heights`): a level is 1.4 tiles above elevation 3 (`WorldMapBuilder.GroundLevel`), so a straight step over a level's edge is a cliff; elevation 0 ("any level") ramps from the level on one side of its run to the level on the other, its middle 0.7 from either; the water's elevation 1 takes the nearest land's level; elevation 15 is a deck at its ends' level over the lowest ground beside it.
- **Cover** (`GbaCover.cs`): the behaviour where it shows, then the metatile's label, then `GbaCover.Ranges`, the tilesets' ranges a region's own session writes by hand (empty until plans 19 and 21); otherwise `Unknown`, which the report lists per tileset.
- **Matrices** (`GbaWorld`): the region's first town (Pallet Town, Littleroot Town) is the origin of matrix 0, and every map its `connections` reach up, down, left and right is placed at its offset; every set no connection reaches (an island group, a cave, a room) is a matrix of its own, in the maps' order. Dive and emerge joins are no warps but the area's `dive` and `emerge` (W7): the original joins every tile of the sea to the tile under it, which plan 21's Dive reads.
- **Events** (`GbaEvents.cs`, W7): every position becomes a tile of the map's matrix; every constant the data's name (`GbaNames`: `SPECIES_NIDORAN_F` is "Nidoran♀", `ITEM_TM_SHADOW_BALL` "TM30", the fourth generation having kept the third's first fifty machines). What has no name in the data (the Gen 3 mails, the Card Key) is kept as the constant and reported. The grid is padded to whole chunks, and a chunk that holds tiles of more than one map lists them (`areas`). FireRed's routes join a stand-in for Saffron City (`SaffronCity_Connection`), so the city itself is a matrix of its own and its four connections are reported.

## HeartGold's maps (plan 20 · J1)

```bash
dotnet run --project tools/MapImporter -- --game heartgold [--quick]   # Johto and Kanto, from pret/pokeheartgold, into out/johto/
dotnet run --project tools/MapImporter -- --game heartgold --data      # what Data/world/johto/world.json opens, and Johto's trainers, phone book and habitats
dotnet run --project tools/MapImporter -- --game platinum              # the same as no --game: Sinnoh
```

`tools/MapImporter/Hg/` reads the second DS game with Platinum's readers where the formats are the same engine's: `LandData` (a chunk, with HeartGold's one difference), `TerrainModel`, `HeightPlates`, `ModelInfo`, `Cover`'s rules and `Renders`' colours. The commit is pinned in `tools/DataImporter/Sources.cs` (`HeartGoldCommit`, `HeartGoldFolders`) and fetched once, sparsely, into `.cache/pokeheartgold` (about 50 MB, the three archives whole). A run takes about seven seconds (four with `--quick`) and writes:

| In `out/johto/` | What |
| --- | --- |
| `johto.png`, `johto_behaviours.png`, `johto_heights.png` | The overworld, matrix 0 (`EVERYWHERE`, 47 × 17 chunks: Johto and Kanto on one grid), four pixels a tile (two for heights), by cover and by behaviour, every area outlined and named (Kanto's in red) |
| `areas/<key>.png` | Every area close up by cover, eight pixels a tile, with its warps (blue), people (red; trainers purple), signs (yellow) and triggers (orange) (not with `--quick`) |
| `world/` | Every matrix, chunk and area, `trainers.json`, `phone.json`, `habitats.json` and a `world.json` that makes a map of every matrix and opens every area (not with `--quick`) |
| `report.md` | Counts, problems, the textures no rule sorts (the overworld's first), every behaviour with what it becomes and its count, the models on the overworld, the chunks with HeartGold's own section, the matrices |

| What | From | Kept |
| --- | --- | --- |
| The chunks | `files/a/0/6/5`, a NARC of 676 members (`Narc.cs`) | Platinum's land data with four bytes more after the four sizes: the word `0x1234` and the size of a section of eight-byte records before the attributes (`LandData.HgssMark`, `HgssRecords`; 230 chunks have records). The loader is still assembly in the decompilation; every member's sizes add up so, with the model's `BMD0` and the heights' `BDHC` where the sizes put them. The section is counted, not read. Then as Platinum's: behaviours and the blocked flag, props, the names of the model's textures and where their polygons lie, the height plates (a plate standing on its edge is left out) |
| The building models | `files/fielddata/build_model/bm_field.narc` (340) and `bm_room.narc` | each model's name and box (`ModelInfo.Read`); a chunk of a matrix whose areas are interiors takes its props from the rooms' set, every other from the field's (the area data that picks the set is assembly; the names agree) |
| The matrices | `files/fielddata/mapmatrix/map_matrix/map_matrix_NNNN_<name>.bin` (288) | `Matrix.ParseBinary`, `src/map_matrix.c`'s layout: width, height, whether the headers' and the altitudes' sections are there, the name, a header id a cell, an altitude a cell, a chunk a cell (`0xFFFF` none) |
| The areas | `src/data/map_headers.h` keyed by `include/constants/maps.h` | the encounter bank, the matrix, the music, the events' member, the map section (the area's name comes from its constant, `MAPSEC_NEW_BARK_TOWN`: no text is read), `worldMapX` and `worldMapY`, `regionNo` (Johto 0, Kanto 1), weather, map type, camera, follow mode, battle background, the bike, escape rope and Fly flags, the phone calls in and out, the radio signal. The key is the constant without `MAP_`, lower case (`new_bark`, `route_29`) |
| Events | `files/fielddata/eventdata/zone_event/NNN_<short>.json` (`HgEvents.Parse`, into Platinum's `AreaEvents`) | signs, people (looks, movement, object type, flag, script label, range, the trainer's sight in `param0`), warps, triggers; a script label shortened (`T20_000+1`, a trainer's `std_trainer(...)` its constant); an item ball's item from its label (`std_itemball_r29_potion`). No script is read |
| Wild Pokémon | `files/fielddata/encountdata/gs_enc_data.json`, `files/arc/headbutt.json`, `files/arc/safari_enc.json` (`HgssEncounters.cs`) | each of the 142 tables: land's twelve slots with a species for the morning, the day and the night, surf, Rock Smash, the three rods, each with its rate; the Hoenn and Sinnoh sounds' slots, the swarms and the Super Rod's night fish; both versions where they differ (`Versioned`); the 60 maps' Headbutt trees with their tiles and their common, rare and secret slots; the Safari Zone's twelve areas by time of day. The world files take HeartGold's |
| Trainers | `files/poketool/trainer/trainers.json` with `include/constants/trainers.h` (`HgssTrainers.cs`) | class, name, AI flags, items, double, the party (species, level, difficulty as the IVs' share, held item, moves); the `messages` are never read |
| The phone book | `files/tel/pmtel_book.json` (`Phonebook`) | each contact's class, trainer, map, gift, rematch day and time |
| Behaviours | `include/constants/metatile_behavior.h` | HeartGold's names, mapped onto Platinum's values by name (`HgBehaviours`); a bare number stays Platinum's |

`Hg/HgCover.cs` is Cover's second list, looked at before Platinum's: it holds what the overworld's picture needed (98.6% of the overworld sorted). The importer's report lists what is left.

## Black 2's maps (plan 22 · U1, U2)

```bash
dotnet run --project tools/MapImporter -- --game black2 [--quick]     # Unova, from fuddlesworth/pokebw2, into out/unova/
dotnet run --project tools/MapImporter -- --game black2 --data        # what Data/world/unova/world.json opens, written beside it
```

`tools/MapImporter/Bw2/` reads the third DS game. Its decompilation is [fuddlesworth/pokebw2](https://github.com/fuddlesworth/pokebw2), **licensed GPL v3.0**: as with the other decompilations, nothing of it enters the repository, only the numbers and names this game builds from (the rules under "What is read, and what is not" hold here too). The commit is pinned in `tools/DataImporter/Sources.cs` (`Bw2Repo`, `Bw2Commit`, `Bw2Folders`) and fetched once, sparsely, into `.cache/pokebw2` (about 230 MB). Unlike pret's, this decompilation keeps its maps as text and glTF, not as the cartridge's archives, so the reader shares no format with Platinum's: `Bw2Maps.cs` reads it, `Bw2Behaviours.cs` turns its tile classes into this game's behaviours and `Bw2Cover.cs` is Cover's third list. A run takes about seven seconds and writes:

| In `out/unova/` | What |
| --- | --- |
| `unova.png`, `unova_behaviours.png`, `unova_heights.png` | The overworld (`MAP_MATRIX_OVERWORLD`, 29 × 27 chunks), four pixels a tile (two for heights), by cover and by behaviour, each zone outlined and named |
| `areas/<zone>.png` | Every zone with ground of its own close up, eight pixels a tile, its props marked (not with `--quick`) |
| `report.md` | Counts, problems, every tile class with what it becomes and its counts, the textures no rule sorts (the overworld's first) with the chunks that name them, the props by resource, the matrices |

| What | From | Kept |
| --- | --- | --- |
| The zones | `res/field/zones/<zone>.json`, numbered by `generated/zones.txt` (`Bw2Zone`) | the matrix, area, place's constant (its words are never read, the name is the constant's), parent, music (a season's song is spring's), encounter table's name, weather, camera, battle background, the bike, Escape Rope and Fly flags, Fly's spot |
| The matrices | `res/field/matrices/<name>.json`, numbered by `generated/map_matrices.txt` (`Bw2Matrix`) | a chunk's model name a cell, and for the overworld a zone a cell; a cell the original leaves empty under a chunk (a join between places) takes the nearest zone (`FillZones`) |
| The terrain | `res/field/maps/models/<chunk>/terrain_N.csv` and `terrain_N_splits.csv` (`Bw2Terrain`) | each tile's class, flags and the height at its middle, from the plane it names in `src/ov036/field_map_chunk_height.c`'s two tables (`Bw2Heights`, read from the C source as numbers), or the mean of two planes for a tile cut in two, or the planes a few grids write out in full; a second layer where a chunk has one |
| The chunk models | `res/field/maps/models/<chunk>/model.glb` (`Bw2Model`, `Bw2Geometry`) | from the JSON chunk, the names of the images the materials use (the file name, without folder or extension) and the props, empty nodes named `bw2_prop_NNNN_id_XXXXX`, with their place and their turn about the up axis. Since plan 22 · U2 (the user's decision of 2026-10-20) the binary chunk is read too, **as plain numbers and only to find which texture covers each tile**: each level triangle's corners, placed by the skin's joints, and its material's texture name; of it a tile keeps one `TerrainCover` code (`Bw2Cover.OfChunk`), and no vertex, texture coordinate, colour, normal or image is written anywhere. The images themselves are never fetched |
| The events | `res/field/events/<zone>.json` (`Bw2Events`) | each person's or thing's id, model's name, movement, flag, script's number, facing, range and tile (one on a rail, with no tile, is left out); each warp's zone, number and tile; each sign's script, condition and tile; each trigger's script, variable, value and box. The scripts themselves are not read, but for the next row |
| The global scripts | `res/field/scripts/global_7000.s` and `global_8000.s` (`Bw2Events.GlobalScripts`) | what each item ball's and hidden item's script sets before it calls the common one: the item, the count and the flag, as numbers |
| The wild Pokémon | `res/field/encounters/<name>.json` (`Bw2Encounters`) | each season's rate settings and tables (grass, dark grass, shaking grass, surfing, rippling water, the rods), species and levels; where Black 2 and White 2 differ, Black 2's |
| The trainers | `res/trainers/data/<trainer>.json`, numbered by `generated/trainers.txt`, their classes by `generated/trainer_classes.txt` | the name, class, battle style, routines, items, money rate, and each Pokémon's species, level, difficulty, item and moves. **The `messages` are never read**: the trainers' lines are their overlays', in our own words |
| The names | `generated/items.txt`, `map_models.txt` | the constants' names by number, matched to the game's own data by name |
| The props' names | `res/field/props/*/models/*/*/data.json` | each resource's id (`res_id`) and the name of the folder it is in; no model is read |
| The areas' settings | `res/field/areas/<n>.json`, numbered by `generated/areas.txt` | whether each area is outdoors (`exterior`) |
| Behaviours | `src/ov036/map_attr.c` and `field_water_current.c`, read by hand | the tile classes the field code asks about, each written as a row of `Bw2Behaviours.Table` in our own words: Platinum's value where one means the same, the GBA's where it has one, a value of Black 2's own from `0xB0` (dark grass, deep sand, quicksand, the charged floor, the water's edge, catwalks…) otherwise |

**Where a texture lies.** At U1 a tile's look was what its behaviour said (8.8% of the overworld's tiles). Since U2 the texture over each tile's middle is found from the binary chunk's triangles (`Bw2Geometry.LayersOver`: those within four units of the walking surface are its ground, those above it a crown, a roof or a rock), sorted by `Bw2Cover`'s rules of names, and a building takes the tiles under its prop's box: 82.3% of the overworld's tiles are sorted, and `report.md` lists the textures no rule sorts by the tiles they lie over. The run also writes Black 2's section of `docs/tile-behaviours.md`; the Sinnoh run and the GBA's keep it as it stands (`DocSections`).

`--data` writes, beside the hand-written `Data/world/unova/world.json`, the chunks and matrices of the maps it lists, an area file for each area it opens (its header, events, item balls and hidden items with their flags, wild Pokémon with each other season's in `seasons`), `trainers.json` with every trainer of Black 2's table (`unova_<constant>`, 813) and `habitats.json`. Unova's keys, flags and variables carry the region's name (`unova_route_19`, `FLAG_UNOVA_0x02e9`, `VAR_UNOVA_0x40a1`); `docs/mechanics/rulings.md`, "Unova as Black 2's maps make it", has why, and what this game decides where the source leaves a choice. No file it writes holds a message id (`_Text_`), since those are made of the original's words.

## Tests

`MapImportTests` builds small chunk files, height data and display lists byte by byte and checks each reader on them, so the tests never need the decompilation. `GbaImportTests` does the same for the GBA reader: a small decompilation of two maps joined by a connection, with a `map.bin`, a tileset's attributes and the two headers, read, stitched, given heights, written as world files and built by `WorldMapBuilder`; `GbaEventsTests` a town with a house and its room, the four event lists, an item's ball, a trainer's script, the tables of wild Pokémon, a trainer's party and a heal location, read, written as world files, built (the house found at its door) and walked into. `HgImportTests` does the same for HeartGold's reader: an archive of three members, a binary matrix with and without its sections, a chunk with HeartGold's own section, two headers, an area's events, an encounter entry with a slot split by version, its sounds and swarms, a Headbutt table with its trees, a trainer read without its messages, a phone contact, the behaviours by name and the textures' second list. `Bw2ImportTests` (in `MapImportTests.cs`) does the same for Black 2's: the height tables read from a C source, a terrain grid with a split tile and one with its planes written out, a matrix whose empty zones are filled, a zone's header, a binary glTF whose binary chunk claims more than the file holds (read for its JSON alone), the table of classes with Black 2's own values described, and since U2 a glTF of two textured squares whose binary chunk says which texture lies over each tile and what rises above it, a zone's events with one on a rail, the flags' and variables' names, the global scripts, the wild Pokémon by season with a table split by version, an area's seasons written and read back, and no message id in any file of Unova's. `WorldFileTests` checks the world files' format.
