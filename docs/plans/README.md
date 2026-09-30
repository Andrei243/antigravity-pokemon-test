# Roadmap: from demo to the full Platinum

Three plans, each worked through over several sessions:

| Plan | Delivers | Rough size |
|---|---|---|
| [01 · Sinnoh map](01-sinnoh-map.md) | Every town, route, forest, cave, lake, sea route, gym and the Pokémon League, in 3D, with surfing, elevation, weather and seamless travel | 12–16 sessions |
| [02 · Story](02-story.md) | The whole story from Twinleaf Town to the Hall of Fame and the post-game, paced like the original: HM obstacles, story blockers, key items, badges, rival and Team Galactic events | 14–18 sessions |
| [03 · National Pokédex](03-national-dex.md) | All 1025 species: the Sinnoh Pokédex (210) during the story, the National Pokédex after the Hall of Fame, with moves, abilities, evolutions, 3D models and a way to obtain each one | 15–20 sessions |

## Order

The plans depend on each other, so interleave them instead of finishing one before starting the next.

1. **Foundations**, in this order:
   1. 03 · D1: species, moves and learnsets loaded from data files (every trainer and encounter table needs this).
   2. 01 · M1–M3: world data format and import, chunk streaming, terrain features (water, elevation, caves).
   3. 02 · S1–S3: event scripts and story flags, field moves and obstacles, the battle features the story needs.
2. **Chapters**: then advance region by region. For each region: its map (01), then its story beats (02), then the species found there (03). Every session should end with something playable.
3. **Endgame**: Pokémon League, National Pokédex unlock, post-game areas, the remaining species.

The dependency points are called out in each plan ("Needs: …").

## Starting a session

Paste something like:

> Read docs/plans/README.md and docs/plans/02-story.md, then do session S4. Verify with the build, the tests and the screenshot harness, and tick off what's done in the plan.

At the end of a session, update the plan's status checklist and note any decision that was made, so the next session starts from the truth.

## Shared conventions

- **Build and test**: `dotnet build PokemonPlatinum.sln`, `dotnet test PokemonPlatinumTests`. Keep tests green; add tests for new rules (movement, scripts, battle effects, data completeness).
- **Visual checks**: `dotnet run --project tools/ShotHarness -- <out dir> [all|field|lineup|battle|flow|menus|sheets]` renders the game in a hidden window and saves PNGs. Add shots for every new area or feature and look at them before calling it done. The harness reaches into `GameEngine`'s private fields by reflection, so update it when those change.
- **Faithfulness**: follow Platinum, not Diamond/Pearl, wherever they differ (gym order, HM locations, badge effects, Distortion World, encounter tables).
- **Reference data**: the pret/pokeplatinum decompilation (<https://github.com/pret/pokeplatinum>) has most of the game's data as JSON: `res/field/events/events_<map>.json` (NPCs, warps, triggers, signs), `res/field/encounters/encounters_<map>.json`, `res/trainers/data/<trainer>.json`, `res/pokemon/<species>/data.json`, map scripts in `res/field/scripts/scripts_<map>.s`, the world grid in `res/field/matrices/`, per-chunk land data in `res/field/maps/data/`. Raw files download from `https://raw.githubusercontent.com/pret/pokeplatinum/main/<path>`. Bulbapedia blocks automated fetching; pokemondb.net and Serebii work.
- **Our own assets**: keep generating art procedurally (3D models, textures, sprites baked from models) and write dialogue in our own words that follows the original beats. Don't copy the games' sprites, models, music or script text; the decomp is a reference for layouts, numbers and story structure.
- **Commits**: the user commits and pushes; don't commit unless asked.
