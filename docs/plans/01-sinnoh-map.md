# Plan 01 · The whole Sinnoh map

**Goal**: all of Platinum's Sinnoh explorable in the 3D Platinum style: every town and city, Routes 201–230, the forests, caves and dungeons, the three lakes, the sea routes (Surf), the eight gyms with their puzzles, Victory Road and the Pokémon League, and the post-game Battle Zone. The overworld is continuous like the original, and each area has its own encounters, music and weather.

## Where we are

- The game's maps are hand-made JSON files in `Data/maps/` (`docs/data-files.md`): Twinleaf Town, Route 201, Lake Verity, Sandgem Town, Route 202, Jubilife City, a stand-in Pallet Town and their rooms. M1 measured them against the original: they are smaller and laid out differently (Twinleaf has two of its four houses, Jubilife seven of its thirty-two buildings).
- `Overworld/Map.cs` is a tile grid: one `TileType` per tile plus a solid flag, an overhead layer, props, NPCs, warps, signs and one wild-encounter table. Outdoor maps connect through **edge warps** (for example Twinleaf's top row warps to Route 201's bottom row).
- `TileType` covers grass, flowers, tall grass, path, water (blocking), one-way ledges (down only), trees, roofs, walls, doors, floor, signs, the PC, and sand, dirt, snow and cave floor (which no map uses yet).
- Rendering: `Graphics/MapScene.cs` turns a map into static meshes with plan 04's kits (pixel ground, water, trees, tall grass, painted buildings, street furniture, furnished rooms); `WorldRenderer` draws them with Platinum's field camera, time of day and lights after dark.
- Since M1: `tools/MapImporter` reads all of Sinnoh from the decompilation and writes it as world files (chunks, matrices, areas) that nothing loads yet.
- Missing: the world files in the game (M2), elevation, caves, surfing, the bike, fishing, weather, Fly, and every area past Jubilife City.

## Decisions (taken in M1, 2026-10-03)

1. **Where the layouts come from: imported from the decompilation.** The spike read every one of the 666 chunks, 289 matrices and 593 areas in twelve seconds, and the result is the real Sinnoh (see M1's outcome). Hand-authoring is kept for fixes only.
   - `res/field/matrices/map_matrix_NNN.json`: the grids. Matrix 0 is the overworld, 30 by 30 chunks, with the area and the altitude of each chunk.
   - `res/field/maps/data/map_data_NNN.bin`: a chunk. Per tile a behaviour and a blocked flag; the props standing on it (model id and position); its 3D model; its heights as plates.
   - `include/data/map_headers.h` and `res/text/location_names.json`: each area's name, music, weather, camera, battle background and what is allowed there.
   - `res/field/events/events_<area>.json`: people, warps, signs and triggers, in tiles of the area's matrix.
   - `res/field/encounters/encounters_<area>.json`: the wild tables (plan 03 imports them).
2. **What a tile looks like comes from the names of the original's textures.** The tile data says where one can walk, not whether open ground is path or lawn, nor whether a blocked tile is a tree, a fence or a rock face. That is only in each chunk's 3D model. The importer reads from it, per polygon, the *name* of its texture (`ngrass`, `nsand`, `tree01`, `criff`) and which tiles it lies over, and sorts the names into our own kinds of ground (`TerrainCover`). No vertex, texture or palette is kept, and the game draws those tiles with its own art, so this stays inside the assets policy. It is the one place the importer looks inside a model, and the one decision here the plan had not foreseen: without it the ground of all 176 overworld chunks would have to be painted by hand.
3. **Buildings come from props.** A prop is a model id and a position. The importer keeps the model's short name and bounding box: `pc` is a Pokémon Center, `fs` a Mart, `gym00` a Gym, `t1_h01` a Twinleaf house. M4 maps names to our building kinds; the box is the footprint.
4. **Seamless overworld: yes, stream chunks.** The overworld's 468 filled cells are made of 176 different chunks (forest and sea fillers repeat), and areas change at chunk borders, so a 3×3 block of chunks round the player is the natural unit. Lakes, forests, caves and rooms are matrices of their own, reached by warps: 1,213 warps, every one of which leads to a warp that exists.
5. **Maps as data: world files.** `chunks/NNN.json`, `matrices/NNN.json` and `areas/<key>.json` (`docs/data-files.md`, "World files"; records in `Data/WorldFiles.cs`). The importer writes them to `tools/MapImporter/out/world/` (8.7 MB for all of Sinnoh, not checked in). M2 moves the output under `PokemonPlatinumEngine/Data/` and decides how much to check in at a time; hand fixes go in override files beside the importer, as the data importer does, so a re-import never loses them.
6. **Tile behaviours keep Platinum's numbers.** `TileBehavior` names the 94 values the maps use, with the original's values, so chunks need no translation; `docs/tile-behaviours.md` is generated from the import.

## Architecture

- **`World`**: the overworld grid; loads chunks lazily, answers queries in global tile coordinates (behaviour, collision, height, which area/map header covers a tile), and lists the events near the player.
- **`Chunk`**: 32×32 tiles of behaviours, collision and heights plus its buildings; its meshes are built when it comes into range and freed when it leaves.
- **Tile behaviours** replace the small `TileType` enum: use the decomp's behaviour values (or a documented mapping) for tall grass, very tall grass, water, waterfalls, puddles, sand, mud slopes, snow, deep snow, ice, ledges in each direction, rock-climb walls, stairs, bridges, doors and warp tiles, counters, and so on. Document them in `docs/tile-behaviours.md`.
- **Elevation**: per-tile heights from the BDHC data (or derived from behaviours where BDHC is too hard), rendered as raised terrain with cliff faces, slopes and stairs; characters follow the height; bridges can be walked over and under.
- **Movement states** in `Player`: walk, run, bike (two gears), surf (riding a Pokémon), sliding on ice, jumping ledges, rock climbing, waterfall climbing, sinking in deep snow, sliding back down mud slopes.
- **Camera presets**: map headers select a camera; support the few variants Platinum uses (caves and some interiors sit closer).
- **Environment**: weather per area (rain on Routes 212, 213 and 215, snow and blizzard on 216 and 217, fog on the north of Route 210, cave darkness until Flash) and day/night lighting from the system clock, using the games' five periods (morning, day, evening, night, late night).
- **Encounters** per area: land (morning, day, night slots), surf, and the Old, Good and Super Rods. Swarms, the Poké Radar, the Great Marsh and the Trophy Garden come with plan 03.

## Sessions

### M1 · Import spike and world format
- Write `tools/MapImporter` (console app): read the matrix and map headers, parse the land-data permissions, building list and heights, and dump a top-down PNG per chunk coloured by behaviour. Confirm the binary layout by reading the decomp's land-data loading code in `src/`.
- Compare Twinleaf, Route 201 and Sandgem with the current hand-made maps and reference maps; list every behaviour value used and what it means in `docs/tile-behaviours.md`.
- Decide: import or hand-author. Record the decision above.
- **Done when** the overworld PNG mosaic is recognisably Sinnoh and the importer runs in under a minute.

**Outcome (2026-10-03).**
- **The importer** (`tools/MapImporter`, in the solution; its README says what it reads and what it leaves): a sparse checkout of the decompilation at the data importer's pinned commit, readers for the matrices, the header table, the events and the chunk files, and a PNG writer of its own. A full run takes twelve seconds and writes 1,251 pictures, 1,548 world files, `docs/tile-behaviours.md` and a report.
- **The binary layout, confirmed against the game's loaders** (`src/overlay005/land_data.c`, `map_prop.c`, `bdhc.c`, `src/terrain_collision_manager.c`, `src/map_matrix.c`): a chunk file is four sizes, then 32×32 16-bit tile attributes (behaviour in the low byte, blocked in the top bit, nothing between), the props (48 bytes each: a model id and a position from the chunk's centre in sixteenths of a tile; at most 32; none is rotated or scaled), the model, and the heights. All 666 chunks parse; one (`map_data_506`) has two stray bytes after its props, which the game also drops.
- **Heights are not the risk the plan feared.** The decompilation documents the format (`docs/maps/bdhc.md`): rectangles ("plates") with a plane each. 8,974 plates in all, 1,004 of them sloping (stairs and ramps, mostly one tile up per tile along), up to 1,033 in one chunk. Every chunk has them. A step of 1.25 tiles or more blocks a walker, so cliffs need no blocked tiles; where a bridge crosses a path two plates overlap and the walker keeps the nearer. The matrix adds an altitude per chunk in half tiles.
- **The mosaic is Sinnoh**: `out/sinnoh.png`, 30×30 chunks at four pixels a tile, with every town and route in its place, Mt. Coronet's rock down the middle, the snow of Routes 216 and 217, the Battle Zone and the three islands. Mt. Coronet's inside, the three lakes, Eterna Forest and every cave and room are matrices of their own (289 in all, 334 indoor areas, 166 cave areas).
- **Compared with the hand-made maps** (boards in `out/compare/`, numbers in `out/report.md`): Twinleaf Town is 20×32 walkable tiles with four houses, a pond and Platinum's snow patches, against 24×20 with two houses; Route 201 is 64 tiles long with 33 ledge tiles and 13 people, against 36 with 9 and one; Sandgem has the lab, a Center, a Mart and two houses on the way down to the beach, against three buildings; Jubilife is 64×64 with 32 buildings and 33 people, against 40×34 with seven and ten. The hand-made maps were sketches from memory; none can be kept.
- **Ground cover**: 836 texture names lie over tiles, 165 of them on the overworld's ground. Nineteen patterns sort them (`Cover.cs`), which leaves 0.5% of the overworld's tiles unknown: Sunyshore's walkways, cave mouths and gates, to be sorted when those places are built. The overworld is 29% trees, 19% rock face, 15% water, 11% lawn, 5% tall grass, 4% paving, 3% buildings.
- **Behaviours**: 94 values in use on 681,984 tiles (`docs/tile-behaviours.md`). Ledges go south, east and west, never north; doors are blocked tiles; water is three behaviours; eight values (80 tiles in all) are not understood, and the decompilation has no name for them either.
- **Props**: 3,476 of 360 models, with meaningful names: `pc` (16 Pokémon Centers), `fs` (12 Marts), `gym00` (8), `gate_a` and `gate_b`, houses per town (`t1_h01`, `c3_h01a`), Jubilife's `c1_school`, fountains (`funsui`). Doors are props of their own (`t1_door1`, `p_door`), which is what G9 animates.
- **Events**: 3,555 people and objects, 1,213 warps, 682 signs, 186 triggers. Scripts and hidden-by flags are numbers or the decompilation's names in different files; both are kept as text for plan 02. Six lifts warp to a destination a script sets (`dynamic`).
- **World format**: `Data/WorldFiles.cs` (`WorldChunkFile`, `WorldMatrixFile`, `WorldAreaFile`, `TerrainCover`) and `Overworld/TileBehavior.cs`; small records are written one to a line so a chunk with a thousand plates stays readable. Nothing loads them yet.
- **Tests**: 65 new cases (`MapImportTests`, `WorldFileTests`, `TileBehaviorTests`). The readers are tested on chunk files, height data and display lists built byte by byte in the tests, so no test needs the decompilation.
- **Found on the way, for later sessions**: Twinleaf's ground is one tile above zero and its pond half a tile (M3's water must sit below its banks); the story changes the overworld's matrix twice (the paths to Sendoff Spring and to Flower Paradise appear: `MapMatrix_RevealSpringPath` and `RevealSeabreakPath` in `src/map_matrix.c`), for M8 and M10; `MountainFloor` is the Underground's floor, not a mountain's.
- **Not done here**: nothing in the game changed. Loading the world files, streaming, and porting the first areas are M2; the movement rule of each behaviour is M3; turning prop names into buildings is M4; encounter tables are plan 03's.

### M2 · Data-driven world and chunk streaming
- Add `World`, `Chunk` and map-header types; load the generated JSON; switch the player, NPCs, warps and encounters to global coordinates; stream a 3×3 block of chunks.
- Port Twinleaf, Route 201, Lake Verity, Sandgem and Route 202 to imported data; keep the existing interiors (they still load through door warps).
- *Ready from M1:* the world files and their records (`Data/WorldFiles.cs`), `TileBehavior`, and a cover code per tile to draw from (`TerrainCover`: lawn, path, flowers, trees, rock faces, buildings). The four towns and routes are on matrix 0, at chunks (3,27) Twinleaf, (3..4,26) Route 201, (5,26) Sandgem and (5,25) Route 202, with Jubilife at (4..5,23..24); Lake Verity is matrix 102, three chunks by two, entered by a warp from Verity Lakefront. Run the importer with `--out` under `PokemonPlatinumEngine/Data/` and check in only the chunks and areas in use. The props still need M4's table of names before they become buildings: for M2 a first handful (`t1_h01`, `t1_s01`, `t1_s02`, `t2_h01`, `t2_s01`, `t2_s02`, `pc`, `fs`) is enough.
- Generalise the tests (every warp reachable, doors lead inside) to all imported areas; teach the harness to jump to an area by name.
- **Done when** you can walk from Twinleaf to Route 202 with no loading screens, the screenshots look at least as good as today, and a frame stays under 8 ms.

### M3 · Terrain features (with plan 04 · G4's materials)
- Elevation, cliffs, slopes, stairs and bridges; ledges in every direction.
- Water: animated surface with shorelines; Surf (start from the shore, ride, land), with surf encounters; waterfalls (rendered, climbed with Waterfall).
- Sand, mud slopes (need the bike's speed), snow and deep snow, ice (slide until blocked), rock-climb walls.
- Caves: cave floor and wall style, ladders, darkness and Flash.
- Obstacle objects rendered and blocking: cuttable trees, cracked rocks, Strength boulders (their behaviour comes from plan 02).
- Unit tests for the movement rules of each behaviour, run on `World` and `Player` without rendering; a test map in the harness that shows every behaviour.
- **Done when** each feature has a test and a screenshot.

### M4 · Buildings and landmarks (with plan 04 · G5's building kit)
- Map building model ids to procedural building kinds, by the short names M1 found (the report lists all 360 with their sizes). Houses vary by town (Twinleaf's green roofs, Jubilife's modern blocks, Eterna's older timber, Snowpoint's snowed roofs, Sunyshore's seaside homes); Pokémon Centers, Marts and a distinct gym per city.
- Landmarks: Jubilife TV, Global Terminal and Trainers' School; Oreburgh's mine works; Floaroma's flower fields; Valley Windworks' turbines; Eterna's statue and Galactic building; Hearthome's contest hall, church and Amity Square; Solaceon ruins entrance; Veilstone's department store, meteorites and Galactic HQ; Pastoria's Great Marsh; Celestic's shrine; Canalave's drawbridge, library and harbour; Snowpoint's temple; Sunyshore's solar walkways and Vista Lighthouse; the Pokémon League.
- Scenery: tree styles per area, rocks, fences, flowers, signs, lamps, benches.
- **Done when** each city is recognisable side by side with reference pictures.

### M5–M8 · Regions
For every area: import and check it, fix unmapped behaviours, add its landmarks and interiors, encounters, music and weather, and a harness shot. Split by the route through the story:

- **M5 · South-west**: Twinleaf, Routes 201–204, Lake Verity, Sandgem, Jubilife, Oreburgh Gate, Oreburgh City and Mine, Ravaged Path, Floaroma Town and Meadow, Valley Windworks, Fuego Ironworks (outside).
- **M6 · Centre**: Route 205, Eterna Forest, Old Chateau, Eterna City, Routes 206–207 (Cycling Road, Wayward Cave), Mt. Coronet (south), Route 208, Hearthome City (Amity Square, Contest Hall), Route 209, Lost Tower, Solaceon Town and Ruins.
- **M7 · East and sea**: Route 210 (south and the foggy north), Route 215, Veilstone City, Route 214, Maniac Tunnel, Valor Lakefront, Lake Valor, Route 213, Pastoria City and the Great Marsh, Route 212 (Pokémon Mansion, Trophy Garden), Celestic Town, Routes 218–221, Canalave City, Iron Island.
- **M8 · North and the end**: Route 211, Mt. Coronet (north and summit), Routes 216–217, Acuity Lakefront, Lake Acuity, Snowpoint City and Temple, Spear Pillar, the Distortion World (floating islands and sideways gravity: its own rendering tricks), Sendoff Spring, Turnback Cave, Route 222, Sunyshore City, Route 223, Victory Road, the Pokémon League.

### M9 · Gyms and the League
Platinum's gym puzzles, built on plan 02's event system:

| Gym | Leader, type | Badge | Puzzle |
|---|---|---|---|
| Oreburgh | Roark, Rock | Coal | Trainers along a rocky maze |
| Eterna | Gardenia, Grass | Forest | Rotating flower-clock hands (Platinum) |
| Hearthome | Fantina, Ghost | Relic | Dark rooms with quiz doors (Platinum) |
| Veilstone | Maylene, Fighting | Cobble | Sliding punching bags (Platinum) |
| Pastoria | Crasher Wake, Water | Fen | Buttons that raise and lower the water |
| Canalave | Byron, Steel | Mine | Lifts between levels |
| Snowpoint | Candice, Ice | Icicle | Sliding on ice and breaking snowballs |
| Sunyshore | Volkner, Electric | Beacon | Rotating gear walkways |

Plus the Elite Four rooms (Aaron, Bertha, Flint, Lucian), Cynthia's room and the Hall of Fame. Check each puzzle's details against the decomp's gym scripts before building it.

### M10 · Post-game areas
Fight Area, Survival Area, Resort Area, Routes 224–230, Stark Mountain, Snowpoint Temple (inside), Fullmoon and Newmoon Islands, Flower Paradise, the Battle Frontier buildings (their facilities are optional).

### M11 · Interiors
Every house and special building from the event files: reuse the house, Pokémon Center, Mart and lab styles, and build the unique ones (Jubilife TV, Global Terminal, Pokétch Company, Trainers' School, department stores, Galactic buildings, Canalave Library, Hotel Grand Lake, Solaceon Day Care, the Pokémon Mansion).

### M12 · Travel and polish
Town Map and Fly destinations, day/night and weather polish, and a performance pass (chunk meshes built off the main thread or cached, draw distance, memory).

## Risks

- **Land-data format**: settled in M1. Permissions, props and heights all read cleanly; the heights are plates, which the decompilation documents.
- **Ground cover by texture name**: a pattern can sort a name into the wrong kind of ground. Look at each area's picture (`out/areas/<key>.png`) when it is built, and keep the patterns few.
- **Performance**: the field renders a whole map as one scene today. Chunks need their own meshes and culling; watch frame time in the harness (`Timing` lines).
- **Scale**: hundreds of chunks. Keep the importer deterministic and re-runnable, and keep every hand fix in override files so re-imports don't lose it.
- **Distortion World**: its geometry is unlike the rest; a simpler interpretation is acceptable.

## Needs and gives

- **Needs** plan 02's event system for gym puzzles, obstacles and story-gated NPCs (M9 and parts of M3) and plan 03's data for encounter tables.
- **Gives** plans 02 and 03 the places: areas, events, encounter tables.

## Status

- [x] M1 Import spike and world format (2026-10-03: `tools/MapImporter` reads all of Sinnoh in twelve seconds; decided to import; ground cover from texture names; world files and `TileBehavior` defined; nothing in the game changed yet)
- [ ] M2 Data-driven world and chunk streaming
- [ ] M3 Terrain features
- [ ] M4 Buildings and landmarks
- [ ] M5 South-west
- [ ] M6 Centre
- [ ] M7 East and sea
- [ ] M8 North and the end
- [ ] M9 Gyms and the League
- [ ] M10 Post-game areas
- [ ] M11 Interiors
- [ ] M12 Travel and polish
