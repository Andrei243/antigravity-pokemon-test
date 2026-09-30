# Plan 01 · The whole Sinnoh map

**Goal**: all of Platinum's Sinnoh explorable in the 3D Platinum style: every town and city, Routes 201–230, the forests, caves and dungeons, the three lakes, the sea routes (Surf), the eight gyms with their puzzles, Victory Road and the Pokémon League, and the post-game Battle Zone. The overworld is continuous like the original, and each area has its own encounters, music and weather.

## Where we are

- Maps are C# code in `Data/MapDatabase.cs` and `Data/MapDatabase.Interiors.cs`: Twinleaf Town, Route 201, Lake Verity, Sandgem Town, Route 202, the two Twinleaf houses, Pokémon Center, Poké Mart and Rowan's lab.
- `Overworld/Map.cs` is a tile grid: one `TileType` per tile plus a solid flag, an overhead layer, props (furniture), NPCs, warps, signs and one wild-encounter table. Outdoor maps connect through **edge warps** (for example Twinleaf's top row warps to Route 201's bottom row).
- `TileType` covers grass, flowers, tall grass, path, water (blocking), one-way ledges (down only), trees, roofs, walls, doors, floor, signs and the PC.
- Rendering: `Graphics/MapScene.cs` turns a map into static meshes (ground baked by `GroundBaker`, buildings found by `MapStructures`, trees from `TreeModels`, furniture from `PropModels`); `WorldRenderer` draws them with a shadow map, toon-shaded 3D characters and the Platinum field camera (constants from pret/pokeplatinum `src/overlay005/field_camera.c`).
- Missing: elevation, caves, surfing, the bike, fishing, weather, day/night, Fly, and every area past Route 202.

## Decisions (confirm in M1)

1. **Where the layouts come from.** *Recommended: import them from the decompilation.*
   - `res/field/matrices/map_matrix_000.json` is the overworld grid: a map header (area id → name, music, weather, encounters) and a land-data id per 32×32-tile chunk, plus altitudes. Other matrices hold caves and buildings.
   - `res/field/maps/data/map_data_NNN.bin` is the land data of a chunk: per-tile permissions (a behaviour value and a collision bit), building placements (model id, position, rotation), the original 3D model (ignored) and height data (BDHC: slopes and elevated floors).
   - `res/field/events/events_<map>.json` lists NPCs (graphics id, movement type, visibility flag, script id), warps, coordinate triggers and background events (signs, hidden items) in global tile coordinates.
   - `res/field/encounters/encounters_<map>.json` has the wild tables.

   Everything is still rendered by our own procedural code from tile behaviours and building ids; no original models or textures. The alternative, hand-authoring each area from reference maps, costs about a session per town and drifts from the original.
2. **Seamless overworld.** *Recommended:* stream 32×32 chunks around the player from the world grid, like the original. Doors, caves and buildings keep using warps into their own maps. Edge warps stay as the fallback if streaming is too costly.
3. **Maps as data.** Generated JSON under `PokemonPlatinumEngine/Data/maps/` (checked in, so the game never needs the decomp), loaded at start-up, with small hand-written override files for fixes. The C# map definitions go away once their areas are imported.

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

### M2 · Data-driven world and chunk streaming
- Add `World`, `Chunk` and map-header types; load the generated JSON; switch the player, NPCs, warps and encounters to global coordinates; stream a 3×3 block of chunks.
- Port Twinleaf, Route 201, Lake Verity, Sandgem and Route 202 to imported data; keep the existing interiors (they still load through door warps).
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
- Map building model ids to procedural building kinds. Houses vary by town (Twinleaf's green roofs, Jubilife's modern blocks, Eterna's older timber, Snowpoint's snowed roofs, Sunyshore's seaside homes); Pokémon Centers, Marts and a distinct gym per city.
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

- **Land-data format**: the permissions are simple; the height data (BDHC) is not. Fall back to heights derived from behaviours and hand overrides if needed.
- **Performance**: the field renders a whole map as one scene today. Chunks need their own meshes and culling; watch frame time in the harness (`Timing` lines).
- **Scale**: hundreds of chunks. Keep the importer deterministic and re-runnable, and keep every hand fix in override files so re-imports don't lose it.
- **Distortion World**: its geometry is unlike the rest; a simpler interpretation is acceptable.

## Needs and gives

- **Needs** plan 02's event system for gym puzzles, obstacles and story-gated NPCs (M9 and parts of M3) and plan 03's data for encounter tables.
- **Gives** plans 02 and 03 the places: areas, events, encounter tables.

## Status

- [ ] M1 Import spike and world format
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
