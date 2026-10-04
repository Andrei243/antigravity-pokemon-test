# Roadmap: from demo to the full Platinum

Seven plans, each worked through over several sessions:

| Plan | Delivers | First session | Rough size |
|---|---|---|---|
| [01 · Sinnoh map](01-sinnoh-map.md) | Every town, route, forest, cave, lake, sea route, gym and the Pokémon League, with surfing, elevation, weather and seamless travel | M1 | 12–16 sessions |
| [02 · Story](02-story.md) | The whole story from Twinleaf Town to the Hall of Fame and the post-game, paced like the original: HM obstacles, story blockers, key items, badges, rival and Team Galactic | S1 | 13–17 sessions |
| [03 · National Pokédex](03-national-dex.md) | All 1025 species: the Sinnoh Pokédex during the story, the National Pokédex after the Hall of Fame, with moves, abilities, evolutions, models and a way to get each one | D1 | 12–16 sessions |
| [04 · Graphics overhaul](04-graphics-overhaul.md) | One polished art style everywhere: lighting, materials, terrain, buildings, characters, Pokémon, battles and a new interface | G1 | 11+ sessions |
| [05 · Sound and music](05-sound-and-music.md) | Music and sounds that behave like Platinum's: day and night themes, trainer eye music, battle and victory themes, fanfares, a sound for every action, a cry for every species | A1 | 7+ sessions |
| [06 · Game mechanics](06-game-mechanics.md) | Every rule of Platinum (battles, abilities, items, breeding, encounters, the Pokétch, contests, the Battle Frontier), plus Mega Evolution, Z-Moves, Dynamax and Terastallization, and every move and item of all the main games | R1 | about 30 sessions |
| [07 · Online trading and battles](07-online.md) | Trading and battling with friends over the internet through a small .NET server: link codes and friend codes, server-checked trades with crash-safe receipts, server-run battles with Flat 50 rules, working across every chained region | O1 | 7–8 sessions |

## Order

The plans depend on each other, so interleave them rather than finishing one before starting the next.

1. **Look and sound first.** Everything built later inherits them.
   - 04 · G1–G3: style guide, rendering foundation, interface kit.
   - 05 · A1–A3: audio engine, music director, sound effects. Independent of the rest; fit them in whenever convenient.
2. **Foundations for content.**
   - 03 · D1: species, moves and learnsets from data files.
   - 06 · R1–R3: the mechanics catalogue, the new battle core, status conditions and field effects.
   - 01 · M1–M3: world import, chunk streaming, terrain; then 04 · G4–G5: the nature and building kits.
   - 02 · S1–S2: scripting and story flags, field moves and obstacles.
   - 06 · R4–R10: Platinum's moves, abilities, held items, double battles, trainer AI and what happens after a battle. The story needs these before its first gym.
   - 04 · G6–G8: characters, Pokémon models, battle presentation; 05 · A4–A5: cries and the core music.
3. **Chapters.** Region by region: the map (01), its story (02), its species (03), its music (05 · A6). Fit in Platinum's systems (06 · R11–R18: the bag and shops, storage, encounters, daily events, breeding, side activities, the Battle Frontier) as the story reaches the places that use them. End every session with something playable.
4. **Endgame.** Pokémon League, National Pokédex, post-game areas, remaining species; then the later generations' mechanics (06 · R19–R30: Mega Evolution, Z-Moves, Dynamax, Terastallization, every remaining move, ability and item); polish with 04 · G10–G11 and 05 · A7 (04 · G9 was done early, on 2026-10-04).

Each plan's "Needs and gives" section lists the exact dependencies.

## Starting a session

One session per conversation works best. Any of these prompts starts one:

```text
Do session G1 of the roadmap.
```

```text
Do the next unchecked session of plan 01.
```

```text
Read docs/plans/README.md and docs/plans/04-graphics-overhaul.md, then do session G1. Verify with the build, the tests and the screenshot harness, show me before/after screenshots, and tick the session off in the plan.
```

The short forms work because Claude's memory points to these plans; the long form spells everything out. At the end of a session, the plan's status checklist is updated and any decision taken is written into the plan, so the next session starts from the truth.

## Shared conventions

- **Build and test**: `dotnet build PokemonPlatinum.sln`, `dotnet test PokemonPlatinumTests`. Keep tests green; add tests for new rules (movement, scripts, battle effects, data completeness, music switching).
- **Visual checks**: `dotnet run --project tools/ShotHarness -- <out dir> [all|field|lineup|battle|flow|menus|intro|look|title|terrain|buildings|lab|life|world|times|sheets] [before dir]` renders the game in a hidden window and saves PNGs (`menus` shows every menu screen in its states and `intro` plays the new-game introduction step by step; `life` shows what moves in the field: prints, dust, leaves, splashes, doors, bubbles, every weather, camera moves, the ways into a battle, waterfalls, fountains and turbines;`look` renders the style guide's reference frames and, given the folder of an earlier run, before/after boards; `times` renders the five times of day and the quality presets; `terrain` and `buildings` render the field's nature, and its buildings, props and rooms by day and after dark; `lab` shows every terrain feature on a map made for it: a mountain with stairs and a waterfall, bridges, ice, snow, marsh, ledges, surfing, obstacles; `world` shows the imported towns and routes and times a walk across them, which is where streaming shows if it can't keep up; `area <key>` takes one shot of an area by its key; `cities [key ...]`, which is not part of `all`, builds the whole of Sinnoh from the importer's last full run and takes a shot before each different building and prop of every town, with a sheet of them per town: how a town is looked at before its area is open). Add shots for every new area or feature and look at them before calling it done; graphics sessions show before/after pairs. The harness reaches into `GameEngine`'s private fields by reflection, so update it when those change.
- **Map checks**: `dotnet run --project tools/MapImporter` reads all of Sinnoh from the decompilation in about twelve seconds and writes, into `tools/MapImporter/out/`, a mosaic of the overworld, a picture of every area and chunk, the world files and a report of what it could not sort and of how much of each open area is in the game. It also rewrites `docs/tile-behaviours.md` and `docs/world-models.md` (the models that stand outdoors and what the game builds for each). Look at an area's picture (`out/areas/<key>.png`) before building it and compare the game's screenshot with it afterwards.
- **Opening an area**: add its key to `PokemonPlatinumEngine/Data/world/sinnoh/world.json`, run `dotnet run --project tools/MapImporter -- --data` (it writes the chunks and area files the game needs and nothing else), and write `overlays/<key>.json` by hand: music, where the doors lead, who the people are and our own lines for them (`docs/data-files.md`, "World files"). `WorldTests` checks the result.
- **Sound checks**: Claude can't listen. Render sounds to WAV files and spectrogram images for checks (levels, clipping, timing, loops), and have the user listen to approve.
- **Art direction**: follow [`docs/art/style-guide.md`](../art/style-guide.md) ("Sinnoh Diorama", chosen in 04 · G1): an HD-2D pixel-art overworld, full 3D battles, and a vector interface that shows Pokémon as 2D sprites.
- **Faithfulness**: follow Platinum, not Diamond/Pearl, wherever they differ (gym order, HM locations, badge effects, the Distortion World, encounter tables, music roles).
- **Rules**: Platinum's rules come from the decompilation's battle code (`src/battle/`, `res/battle/scripts/`), later generations' from Pokémon Showdown (<https://github.com/smogon/pokemon-showdown>, MIT). Rulings where generations disagree go in `docs/mechanics/rulings.md`; `docs/mechanics/coverage.md` lists what is implemented (both start in 06 · R1).
- **Reference data**: the pret/pokeplatinum decompilation (<https://github.com/pret/pokeplatinum>) has most of the game's data as JSON: `res/field/events/events_<map>.json` (NPCs, warps, triggers, signs), `res/field/encounters/encounters_<map>.json`, `res/trainers/data/<trainer>.json`, `res/pokemon/<species>/data.json`, map scripts in `res/field/scripts/scripts_<map>.s`, the world grid in `res/field/matrices/`, per-chunk land data in `res/field/maps/data/`, and the names of every music track and sound effect in `res/sound/pl_sound_data.json`. Raw files download from `https://raw.githubusercontent.com/pret/pokeplatinum/main/<path>`. Bulbapedia blocks automated fetching; pokemondb.net and Serebii work.
- **Assets**: our own art, models, music, sound effects and dialogue (written in our own words, following the original beats), plus free-licensed assets where they help (OFL fonts, CC0 textures, models and sounds, MIT-licensed libraries), each credited in `docs/art/CREDITS.md`. Nothing taken from the games themselves: no sprites, models, textures, music, sound effects, cries or script text. Files the user supplies can go in the `overrides/` folder.
- **Commits**: the user commits and pushes; don't commit unless asked.

## Regions

Decided 2026-10-01: the game grows into every generation as a chain of separate regions, in generation order. A new game starts in Kanto; finishing a region's story (its Hall of Fame) opens the way to the next one, like HeartGold and SoulSilver's trip to Kanto but in the other direction. Kanto to Johto is by boat; how the player reaches Hoenn and each later region is decided when that region is built. Sinnoh, which these plans build, is the fourth region; `--region Sinnoh` starts a new game there for testing. The chain lives in `Data/RegionDatabase.cs`, and `RegionTests` checks it. Kanto is only a stand-in Pallet Town for now.
