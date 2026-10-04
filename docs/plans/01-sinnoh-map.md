# Plan 01 · The whole Sinnoh map

**Goal**: all of Platinum's Sinnoh explorable in the 3D Platinum style: every town and city, Routes 201–230, the forests, caves and dungeons, the three lakes, the sea routes (Surf), the eight gyms with their puzzles, Victory Road and the Pokémon League, and the post-game Battle Zone. The overworld is continuous like the original, and each area has its own encounters, music and weather.

## Where we are

- Since M2 the outdoors of Sinnoh is the imported world: one map of the whole overworld (`Sinnoh`, 960×960 tiles) and one of Lake Verity, made from `Data/world/sinnoh/` and drawn a chunk at a time. Open so far: Twinleaf Town, Route 201, Verity Lakefront, Lake Verity, Sandgem Town, Route 202 and Route 219's beach. Everything else of the region is forest or scenery nobody can enter.
- Still hand-made (`Data/maps/`, `docs/data-files.md`): the rooms, Jubilife City (seven of its thirty-two buildings; reached by a fade from the top of Route 202) and a stand-in Pallet Town.
- `Overworld/Map.cs` is a tile grid: one `TileType` per tile plus a solid flag, an overhead layer, props, NPCs, warps and signs. A small map has one name, one theme and one wild-encounter table; a map of the world has them per area (`MapArea`).
- A tile's `TileType` is what it looks like (lawn, path, water, ledges facing three ways, rock, ice, planks, stairs, marsh, snow, roofs, walls…); what it does is its behaviour, Platinum's own value, and since M3 the rules of walking go by that and by the heights (`Overworld/FieldMovement.cs`): ledges, cliffs, stairs, bridges, Surf, waterfalls, rock faces, ice, deep snow, mud, the Bicycle's limits.
- Rendering: `Graphics/MapScene.cs` turns a map, or one chunk of a streamed map, into static meshes with plan 04's kits (pixel ground, water, trees, tall grass, painted buildings, street furniture, furnished rooms); `WorldRenderer` draws them with Platinum's field camera, time of day and lights after dark, and keeps three chunks by three round the camera. Since M3 the ground is drawn with its relief (`Graphics/Relief.cs`): faces of earth and rock between levels, stairs, bridge decks, waterfalls. The areas open so far are flat; the harness's `lab` mode shows it all.
- `tools/MapImporter` reads all of Sinnoh from the decompilation; its `--data` mode writes the part of it the game has open.
- Missing: caves (their walls, darkness and Flash), the Bicycle as something the player has, fishing, weather, Fly, and every area past Route 202.

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

## Decisions (taken in M2, 2026-10-03)

1. **One map per matrix, in tiles of the whole matrix.** The overworld is a single `Map`, so a tile of Twinleaf Town has the same coordinates today as when all of Sinnoh is open, and saves, warps and overlays never need renumbering. Saves carry a `WorldVersion`; those from the hand-made maps are moved to the same place on the new one.
2. **Only what is open is checked in.** `world.json`, written by hand, lists the maps and the open areas; the importer's `--data` writes their chunks, the chunks in view of them and their area files (31 files, 238 KB today, the same bytes on every run). This replaces "run the importer with `--out` under `Data/`".
3. **Overlays are the override files.** What the import can't or mustn't bring is written by hand in `overlays/<key>.json`, keyed by the import's own ids: the area's music, which map each door leads to, which doors are still locked, who each person is and what they say in our own words, what each sign reads. A person appears only once an overlay names them, so nobody stands about with nothing to say.
4. **`TileType` stays the game's vocabulary for now.** M2 translates cover and behaviour to the tiles the renderer and the player already know, so the first areas are playable at once; M3 adds the behaviours that need rules of their own.
5. **An area is open or it is scenery.** The chunks next to an open area are drawn but solid, so the world never ends in a void and never lets anyone walk into a place that has no people, doors or music yet.
6. **The base land tables are imported with the areas** (twelve slots a route), so the grass has Platinum's Pokémon from the day a route opens. The slots the time of day, swarms and the Poké Radar swap in, and water and fishing, stay with plan 03.

## Decisions (taken in M3, 2026-10-04)

1. **A tile's type is its look; its behaviour is its rule.** `TileType` stays, as the vocabulary of what the field draws, and grows the looks M3 needed. What a tile does is the original's behaviour value on every tile of a world map, and follows from the type on a hand-made one. This replaces "tile behaviours replace `TileType`" in the architecture below.
2. **Walked heights and drawn heights are two things.** The rules use the original's numbers as they are (a step of 1.25 tiles is a cliff; a bridge's tile has a deck and a ground). The drawing (`Relief`) puts the lowlands at zero, draws water level with its banks, turns steps under three quarters of a tile into slopes and fills larger ones with a face. So every place that is flat today is drawn exactly as before, and the painted bank goes on saying that water is lower.
3. **Open water is not blocked.** Its behaviour stops a walker and carries a surfer; only a rock standing in it is a blocked tile of water.
4. **Field moves ask only for the move for now**: Surf, Waterfall and Rock Climb work when a party Pokémon knows them. Badges, the yes-or-no and the party-menu way of using a move are plan 02 · S2.
5. **Platinum's own odds for wild Pokémon** (`EncounterSteps`), with each area's rate, replace the flat 18 in 100.
6. **Caves wait for the first cave.** Their wall style, darkness and Flash moved to M5, where Oreburgh Gate gives real data to build them on; M3 has the rock faces and the cave-floor ground they will use.
7. **Simpler than the original until a place needs more**, each listed in `docs/tile-behaviours.md`: marsh mud only slows, a muddy slope refuses a climb instead of sliding the rider back, the waterfall's sheet doesn't move.

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

**Outcome (2026-10-04).**
- **The world in the game.** Sinnoh's overworld is one `Map` called `Sinnoh` (matrix 0) and Lake Verity a second (`LakeVerity`, matrix 102), made by `WorldMapBuilder` (`Data/World.cs`) from `Data/world/sinnoh/`: `world.json`, the generated `matrices/`, `chunks/` and `areas/`, and the hand-written `overlays/` (decisions above; formats in `docs/data-files.md`). Areas (`MapArea`) give each chunk its name, music, wild Pokémon, trees and architecture, and `GameEngine.EnterArea` shows the name and changes the music as the player walks from one into the next. The five hand-made maps are deleted, their rooms and Jubilife City lead onto the new map, and old saves wake up in the same place.
- **The walk.** From the player's door in Twinleaf Town up Route 201, through Sandgem Town and Route 202 to the edge of Jubilife City is 215 tiles with no fade and no pause; Route 219's beach and Verity Lakefront hang off it, and the lake is one warp away as in Platinum.
- **Streaming.** `WorldRenderer` keeps three chunks by three: each is baked and packed on another thread (`MapScene.Prepare`), uploaded a step at a time within 2.5 ms a frame, and freed 40 tiles behind the camera. On the walk 24 chunks were made ready and none had to be waited for; the most a frame spent uploading was 5.7 ms (one ground texture, which can't be split), and at most 15 chunks were loaded. Standing frames on High: 6.4 ms in Twinleaf, 7.2 on Route 201, 6.9 in Sandgem, 7.0 on Route 202, 6.9 at the lake (the hand-made maps were 6.3, 6.6 and 6.3; the budget is 8).
- **Looks.** Before/after boards of every outdoor shot (`compare_*` from the harness's `field` and `look` modes): the towns have their real plans (Twinleaf's four houses and its snow, Sandgem's lab beside the Center and the Mart), the routes their real length, and nothing is drawn worse than before. The other modes (battles, arenas, title, rooms, menus) are unchanged.
- **Tests.** 617 pass. `WorldTests` (21 cases) covers the index and overlays, the walk there and back, warps, doors, people, wild tables, buildings, old saves, the block of chunks, and that a chunk's ground is the same baked alone or with its neighbour. The art-kit tests keep the five old maps as fixtures (`PokemonPlatinumTests/Fixtures/maps`).
- **Tools.** The harness has a `world` mode (the imported places, then the timed walk with streaming's cost) and `area <key>`; shots written for the old maps are moved to their new places. The importer's report lists, for each open area, how many of the original's people, warps and signs are in the game (18 of 38 people so far: the rest appear as the story reaches them).
- **Stand-ins, for the sessions that replace them.** Rock faces and the ledges that go west are boulders on dirt (M3). Snow patches are flat white (M3). Jubilife City is still the hand-made map, with the imported city in view as scenery whose touching blocks are found as one building (M5). Two houses in Twinleaf, two in Sandgem and the cave on the lake's island are locked (M11, M5). Route 219's sea blocks until Surf (M3).
- **Found on the way.** In the areas open so far the heights are almost flat: every chunk is one plate at height 1, with the pond, the sea's edge and the lake's water half a tile or a tile lower, and the ledges of Routes 201 and 202 are not steps in the height data at all (they are behaviours). 35 of Sinnoh's 326 buildings (route gates mostly) reach across a chunk's edge and the importer marks only their own chunk; `WorldTests.NoBuildingOfAnOpenAreaReachesAcrossTheEdgeOfItsChunk` trips when such an area is opened. `Map.GetNpcAt`, `GetWarpAt` and `IsWalkable` search lists: index them by chunk before the whole region is open. The lake has a second area for its low water (`lake_verity_low_water`), which plan 02's story switches to.

### M3 · Terrain features (with plan 04 · G4's materials)
- Elevation, cliffs, slopes, stairs and bridges; ledges in every direction.
- Water: animated surface with shorelines; Surf (start from the shore, ride, land), with surf encounters; waterfalls (rendered, climbed with Waterfall).
- Sand, mud slopes (need the bike's speed), snow and deep snow, ice (slide until blocked), rock-climb walls.
- Caves: cave floor and wall style, ladders, darkness and Flash.
- Obstacle objects rendered and blocking: cuttable trees, cracked rocks, Strength boulders (their behaviour comes from plan 02).
- Unit tests for the movement rules of each behaviour, run on `World` and `Player` without rendering; a test map in the harness that shows every behaviour.
- **Done when** each feature has a test and a screenshot.

**Outcome (2026-10-04).**
- **The rules** (`Overworld/FieldMovement.cs`, no drawing or input, after the original's `src/player_move.c`): a step is walked, hopped over a ledge (south, west or east), landed from the water, climbed up or down a waterfall or a rock face, or refused with the reason. Cliffs come from the heights (1.25 tiles), bridges from a deck over the ground, closed sides from the railing behaviours; ice and moving floors carry whoever steps on them; deep snow and mud set the pace; the Bicycle has its pace, its planks, its muddy slopes and the ground it can't ride. Of the 94 behaviours Sinnoh uses, 35 have a rule, 30 are ground like any other and 29 wait for the place that needs them: `docs/tile-behaviours.md` now says which, from a table a test keeps true.
- **The player** walks by those rules: travels on foot, surfing or cycling, stands at the height of the ground or the deck, slides, and sets out onto water from the shore with a Pokémon that knows Surf. A save remembers the mode and the level. `Player.Advance` takes its steering as arguments, so tests walk the player.
- **Wild Pokémon** follow Platinum's odds and each area's rate; water has its own table for whoever surfs on it (imported with the land table: Psyduck and Golduck in Twinleaf Town's pond).
- **Relief** (`Graphics/Relief.cs`, `MapScene.AddFaces`; the style guide has a section for it): ground at its heights, faces of earth under grass and of stratified rock under rock, stairs, bridge decks with the river running on beneath, a waterfall's sheet with foam at its lip and foot. Trees, grass, props, buildings and people stand on the ground they are on; the camera follows the player's height.
- **New looks**: rock, ice, marsh, planks and stairs as ground; ledges that face west and east; the three obstacles (a small tree, a cracked rock, a round boulder), placed from the import and blocking; the Pokémon that carries a surfer; walkers sinking into deep snow and mud. Snow and ice were brought down from white: Twinleaf Town's patches of snow no longer bloom to a blank.
- **In the real areas** the change is small, because they are flat: Verity Lakefront's westward ledge is a ledge, Route 219's sea cliffs are bare rock instead of boulders on dirt, rocks stand in the sea rather than on squares of dirt, and the snow reads as snow. Everything else is drawn as before (boards against M2's shots).
- **The terrain lab** (`dotnet run --project tools/ShotHarness -- <dir> lab`): a map built in the harness with all of it in one place, since no open area has relief yet. 5.3 ms a frame; the real areas are unchanged at 5.8 to 6.6 ms.
- **Tests**: 661 pass. `FieldMovementTests` (24) has a small map for each rule and walks the player through ice, stairs, a hop and a ride across a pond; `ReliefTests` (11) covers the drawn heights, the art of the faces, decks, stairs, the swimmer and the obstacles.
- **Found on the way**: the two behaviours the decompilation calls unknown beside the ledges are the corner pieces where a side ledge ends at its south (worked out at Verity Lakefront; named `LedgeCornerSouthEast` and `LedgeCornerSouthWest`). In the lowlands a beach's bed slopes up under the first tiles of water, so "half a tile under the banks" is not true of every water tile: the surface is drawn at the whole tile above. The part of Jubilife City in view from Route 202 has real relief (a part of it lies a tile higher, with steps up to it), which M5 will be the first to show.
- **Not done here**: caves (moved to M5); the Distortion World's double jumps and the Bicycle's ramps (`Waiting` in the table, M8 and M6); warps taken by walking off a mat in the right direction (M11); a moving waterfall, splashes, footprints and reflections (plan 04 · G9); raised ground does not shade the ground behind it, only its faces cast shadows.

### M4 · Buildings and landmarks (with plan 04 · G5's building kit)
- Map building model ids to procedural building kinds, by the short names M1 found (the report lists all 360 with their sizes). Houses vary by town (Twinleaf's green roofs, Jubilife's modern blocks, Eterna's older timber, Snowpoint's snowed roofs, Sunyshore's seaside homes); Pokémon Centers, Marts and a distinct gym per city.
- Landmarks: Jubilife TV, Global Terminal and Trainers' School; Oreburgh's mine works; Floaroma's flower fields; Valley Windworks' turbines; Eterna's statue and Galactic building; Hearthome's contest hall, church and Amity Square; Solaceon ruins entrance; Veilstone's department store, meteorites and Galactic HQ; Pastoria's Great Marsh; Celestic's shrine; Canalave's drawbridge, library and harbour; Snowpoint's temple; Sunyshore's solar walkways and Vista Lighthouse; the Pokémon League.
- Scenery: tree styles per area, rocks, fences, flowers, signs, lamps, benches.
- **Done when** each city is recognisable side by side with reference pictures.

### M5–M8 · Regions
For every area: import and check it, fix unmapped behaviours, add its landmarks and interiors, encounters, music and weather, and a harness shot. Split by the route through the story:

- **M5 · South-west**: Twinleaf, Routes 201–204, Lake Verity, Sandgem, Jubilife, Oreburgh Gate, Oreburgh City and Mine, Ravaged Path, Floaroma Town and Meadow, Valley Windworks, Fuego Ironworks (outside). With the first caves here, what M3 left for them: the look of a cave's walls (the rock faces and cave floor are ready), a light rig that ignores the clock, darkness and Flash. Jubilife's raised part is also the first real relief to check against M3's lab.
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
- [x] M2 Data-driven world and chunk streaming (2026-10-04: the overworld is one streamed map made from the imported world; Twinleaf Town to Route 202 on foot with no loading, Lake Verity, Route 219's beach; overlays hold what is ours; 6.4–7.2 ms a frame)
- [x] M3 Terrain features (2026-10-04: the rules of walking by Platinum's tile behaviours and heights, relief drawn with faces, stairs, bridges and waterfalls, Surf with its water Pokémon, ice, deep snow, marsh, ledges west and east, the three obstacles, a terrain lab in the harness; caves moved to M5)
- [ ] M4 Buildings and landmarks
- [ ] M5 South-west
- [ ] M6 Centre
- [ ] M7 East and sea
- [ ] M8 North and the end
- [ ] M9 Gyms and the League
- [ ] M10 Post-game areas
- [ ] M11 Interiors
- [ ] M12 Travel and polish
