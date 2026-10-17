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
```

`tools/MapImporter/Gba/` reads the GBA decompilations beside `DecompMaps`, sharing nothing with it but the world files' format. Each game's commit is pinned in `tools/DataImporter/Sources.cs` (`FireRedCommit`, `EmeraldCommit`) and fetched once, sparsely (`Sources.GbaFolders`), into `.cache/pokefirered` and `.cache/pokeemerald` (13 and 23 MB). A run takes a few seconds and writes:

| In `out/<region>/` | What |
| --- | --- |
| `<region>.png`, `<region>_behaviours.png`, `<region>_heights.png` | The overworld (matrix 0), four pixels a tile (two for heights), by cover, by behaviour and by height, each map outlined and named |
| `maps/<Map>.png` | Every map by behaviour and by cover, side by side (not with `--quick`) |
| `world/` | The matrices, chunks and areas, and a `world.json` that makes a map of every matrix and opens every area, so a tool can build it with `World.Open` (not with `--quick`). `--data` for a GBA region is W7's |
| `report.md` | Problems (connections that disagree, maps that overlap, layouts left out), every behaviour used with what it becomes, the metatiles with no look per tileset, the overworld's elevations, the matrices |

It also rewrites the section "The GBA's behaviours" of `docs/tile-behaviours.md`, from both games (the other is fetched if it isn't there); the Sinnoh run keeps that section as it stands.

| What | From | Kept |
| --- | --- | --- |
| The maps and their order | `data/maps/map_groups.json` | the order of the groups and of each group's maps |
| A map | `data/maps/<Map>/map.json` | its id, name, layout, music, region map section, weather, map type, the cycling, running and escape flags, and its `connections` (map, offset, direction). The four event lists are W7's |
| Layouts | `data/layouts/layouts.json`, each layout's `map.bin` | its size, its two tilesets, and every block: ten bits of metatile, two of collision, four of elevation |
| A metatile's behaviour | each tileset's `metatile_attributes.bin`, found through `src/data/tilesets/headers.h` and `metatiles.h` | the behaviour's bits alone (two bytes a metatile in Emerald, four in FireRed); nothing about how it is drawn |
| The behaviours' names | `include/constants/metatile_behaviors.h` | FireRed's `#define`s and Emerald's `enum` |
| The metatiles' labels | `include/constants/metatile_labels.h` | a label's words, which sort a metatile into a kind of ground |
| How many metatiles a primary tileset holds | `include/fieldmap.h` | `NUM_METATILES_IN_PRIMARY` (640 in FireRed, 512 in Emerald) |

Never `tiles.png`, a palette, `metatiles.bin` or anything else a tileset draws with: the folder patterns can't reach them, and no vertex, texture or palette reaches the repository.

- **Behaviours** (`GbaBehaviours.cs`): each `MB_*` by its name onto a `TileBehavior`: Platinum's own value where it has one that means the same (`MB_JUMP_NORTH` is Platinum's jump north, `0x3A`, and the four sides and corners of `MB_IMPASSABLE_*` Platinum's blocks), one of the GBA's own at `0xED` to `0xFD` where it has none (deep water, the sea floor, seaweed, thin ice, a cracked floor, a hole, the four currents, spinners, a secret base's spot, the Acro Bike's rails, a bumpy slope, the Cycling Road's pull, no running), and `None` for what only says how a tile is read or drawn. Collision bits set make the tile blocked.
- **Heights** (`GbaWorld.Heights`): a level is 1.4 tiles above elevation 3 (`WorldMapBuilder.GroundLevel`), so a straight step over a level's edge is a cliff; elevation 0 ("any level") ramps from the level on one side of its run to the level on the other, its middle 0.7 from either; the water's elevation 1 takes the nearest land's level; elevation 15 is a deck at its ends' level over the lowest ground beside it.
- **Cover** (`GbaCover.cs`): the behaviour where it shows, then the metatile's label, then `GbaCover.Ranges`, the tilesets' ranges a region's own session writes by hand (empty until plans 19 and 21); otherwise `Unknown`, which the report lists per tileset.
- **Matrices** (`GbaWorld`): the region's first town (Pallet Town, Littleroot Town) is the origin of matrix 0, and every map its `connections` reach up, down, left and right is placed at its offset; every set no connection reaches (an island group, a cave, a room) is a matrix of its own, in the maps' order. Dive and emerge joins are warps (W7). The grid is padded to whole chunks, and a chunk that holds tiles of more than one map lists them (`areas`). FireRed's routes join a stand-in for Saffron City (`SaffronCity_Connection`), so the city itself is a matrix of its own and its four connections are reported.

## Tests

`MapImportTests` builds small chunk files, height data and display lists byte by byte and checks each reader on them, so the tests never need the decompilation. `GbaImportTests` does the same for the GBA reader: a small decompilation of two maps joined by a connection, with a `map.bin`, a tileset's attributes and the two headers, read, stitched, given heights, written as world files and built by `WorldMapBuilder`. `WorldFileTests` checks the world files' format.
