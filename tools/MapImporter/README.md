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

All of Sinnoh is 8.7 MB of world files; the game has only what is built. `Data/world/sinnoh/world.json`, written by hand, lists the maps and the areas that are open. `--data` reads it and writes, beside it, the matrices of those maps, the chunks of the open areas and of every chunk next to one (they are in view from the edge), and the open areas' own files, each with its land encounters, and `habitats.json`, where every wild Pokémon of the region lives (every area's grass by time of day, water and rods, and where each area is on the overworld) for the Pokédex's area page. It deletes generated files that are no longer asked for and never touches `world.json` or `overlays/`. It takes a third of a second, writes the same bytes every time, and ends with an error if an open area's wild Pokémon name a species the game doesn't have. To open an area: add its key to `world.json`, run `--data`, write `overlays/<key>.json`, run the tests.

## What it writes

| In `out/` | What |
| --- | --- |
| `sinnoh.png` | The overworld, four pixels per tile, coloured by what each tile looks like, with area names, people, warps and a key |
| `sinnoh_behaviours.png` | The same, coloured by what each tile does |
| `sinnoh_heights.png` | The overworld shaded by height |
| `compare/<area>.png` | An imported area next to the hand-made map that still stands in for it (Jubilife City, until it opens) |
| `areas/<key>.png` | Every area close up, eight pixels per tile, with its events |
| `chunks/NNN.png` | Every chunk by behaviour, with the footprints of its props |
| `world/matrices`, `world/chunks`, `world/areas`, `world/habitats.json` | The world files |
| `report.md` | Counts; for each open area, how many of the original's people, warps and signs are in the game; the comparison in numbers; every texture name with the ground it was sorted into; every prop model; problems |

It also rewrites `docs/tile-behaviours.md` in the repository.

## What is read, and what is not

| What | From | Kept |
| --- | --- | --- |
| The grids of chunks, the area of each chunk, altitudes | `res/field/matrices/map_matrix_NNN.json` | all of it |
| Areas: name, music, weather, camera, flags | `include/data/map_headers.h`, `res/text/location_names.json` | all of it, under our own names |
| People, warps, signs, triggers | `res/field/events/events_<area>.json` | positions, looks, movement, script numbers; no script text |
| What a model is called and how large it is | `res/field/props/models/*.nsbmd` (the name and the bounding box only) | `docs/world-models.md`: every model that stands outdoors, with its size, where it stands and what the game puts in its place (`Data/WorldModels.cs`). A model the catalogue doesn't know is listed under Problems |
| Wild Pokémon on land and on water | `res/field/encounters/encounters_<area>.json` | the twelve base land slots (species and level), the five water slots (species and a range of levels) and each table's rate |
| Tile behaviours and the blocked flag | `res/field/maps/data/map_data_NNN.bin`, first section | all of it |
| Props (buildings, signboards, furniture) | the same file, second section, and `res/field/props/models/*.nsbmd` | the model's id, its short name and its bounding box |
| The chunk's 3D model | the same file, third section | for each tile, the *name* of the texture drawn there, turned into a kind of ground |
| Heights | the same file, fourth section ("BDHC") | every plate, in tiles |

No vertex, texture, palette or animation leaves the cache. From a model the importer keeps a name and a box (props), or the names of textures and where their polygons lie (terrain); the vertices are read only to find which tiles a polygon covers. What the game draws on those tiles is our own art.

The layouts of the binary files were confirmed against the game's own loaders in the decompilation: `src/overlay005/land_data.c` (the four sections), `map_prop.c` (props, 48 bytes each), `bdhc.c` and `docs/maps/bdhc.md` (heights), `src/terrain_collision_manager.c` (behaviour in the low byte, the blocked flag in the top bit, and the 1.25-tile step limit), `src/map_matrix.c` (matrices and altitudes).

## How a tile gets its look

`Cover.cs` decides, in this order:

1. A blocked tile under the box of a prop as large as a building is `Building`.
2. A behaviour that shows (water, tall grass, sand, snow, ice, marsh, cave floor, bridges) decides.
3. Otherwise the texture drawn there does. `TerrainModel` sorts each chunk's polygons into those lying on the walking surface (the ground) and those above it (a tree's crown, a rock), and a short list of name patterns turns a texture name into a `TerrainCover`: `ngrass` is lawn, `nsand` a path, `tree01` and `conttree_b` trees, `criff` a rock face. A blocked tile shows what stands on it, an open tile its ground.
4. A name no pattern knows leaves the tile `Unknown`. `report.md` lists those names with their tile counts, the overworld's first.

On the overworld 99.5% of tiles are sorted. When a session builds an area whose textures are still in the list, add their patterns to `Cover.Rules`, run the importer and look at the area's picture.

## Tests

`MapImportTests` builds small chunk files, height data and display lists byte by byte and checks each reader on them, so the tests never need the decompilation. `WorldFileTests` checks the world files' format.
