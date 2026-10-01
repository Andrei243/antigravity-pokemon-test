# Plan 03 · Every Pokémon: the National Pokédex

**Goal**: all 1025 species (Generations 1–9). The Sinnoh Pokédex (Platinum's 210) during the story; the National Pokédex unlocked after the Hall of Fame; every species obtainable in the game, each with its stats, types, abilities, moves, evolutions, Pokédex entry and its own animated 3D model.

## Where we are

- 23 species written as C# in `Data/PokemonDatabase.cs` (with their national numbers), 45 moves in `Data/MoveDatabase.cs`, 18 items in `Data/ItemDatabase.cs`.
- `Data/TypeChart.cs` is Generation 4's chart: the `Fairy` value exists in the enum but has no matchups.
- Pokémon have natures, IVs, six growth rates and level-up evolution. No abilities, EVs, held items, forms or breeding.
- `Models/Pokedex.cs` keeps seen and caught national numbers; `UI/PokedexScreen.cs` shows them.
- Every species has a hand-built 3D model in `Graphics/PokemonModels.cs` (the `PokeBuilder` shape language plus bones for idle, attack and hurt animations). `PokemonSprites.BakeAll` renders them to 128 px pixel-art sprites and 48 px icons at start-up; battles render them live every frame. `PokemonModels.Generic` is the fallback. The test `TestEverySpeciesHasItsOwn3DModel` checks every species has a dedicated model.

## Decisions to confirm

1. **Data sources.**
   - Species 1–493 and Generation 4 moves: Platinum's own data from the decompilation: `res/pokemon/<species>/data.json` (base stats, types, abilities, catch rate, EXP, growth rate, gender ratio, egg groups, EV yields, held items, body colour, learnsets by level, TM, tutor and egg) and `res/moves/<move>/` for moves. Find evolutions and Pokédex text in the same tree.
   - Species 494–1025 and later moves and abilities: PokeAPI's CSV files (<https://github.com/PokeAPI/pokeapi/tree/master/data/v2/csv>, BSD licence): `pokemon_species.csv`, `pokemon.csv`, `pokemon_stats.csv`, `pokemon_types.csv`, `pokemon_abilities.csv`, `pokemon_moves.csv` (learnsets by version group), `pokemon_evolution.csv`, `evolution_chains.csv`, `pokemon_forms.csv`, `pokemon_species_flavor_text.csv`, `pokemon_shapes.csv`, `pokemon_colors.csv`, `pokemon_dex_numbers.csv`, `moves.csv`, `move_meta.csv`, `abilities.csv`.
2. **Type rules.** *Recommended:* keep Platinum's chart and typings for species 1–493 so the story plays exactly like Platinum, and add the Fairy type's matchups for the later species that need it. The alternative is modern data everywhere (Clefairy becomes Fairy, Steel stops resisting Ghost and Dark), which is consistent with later generations but changes Platinum's battles.
3. **Models for 1025 species.** Both built with plan 04's SDF modelling kit: hand-built models for Platinum's 210 Sinnoh species (the ones seen in the story), and a **procedural model generator** for everyone else, driven by body shape (PokeAPI's 14 shapes: ball, squiggle, fish, arms, blob, upright, legs, quadruped, wings, tentacles, heads, humanoid, bug-wings, armor), body colour, height and types, plus a few feature tags (wings, tail, horns, fins, flame, leaves, shell). Popular species can be upgraded to hand-built models over time. All art stays our own; the decomp's sprite files are not used.
4. **Where the non-Sinnoh species live.** Platinum's post-game tables (National Pokédex route additions, swarms, the Poké Radar, the Great Marsh and Trophy Garden dailies) cover many species 1–493. The rest need in-game sources, because trading and GBA migration don't exist here:
   - a Pal Park-style post-game area with habitats per region;
   - a new post-game zone for Generations 5–9 (for example Distortion World rifts or new Battle Zone routes) with biome-based tables;
   - gifts: other regions' starters from Professor Rowan or Oak after the National Pokédex;
   - event legendaries as post-game quests (Darkrai on Newmoon Island, Shaymin in Flower Paradise, Arceus at the Hall of Origin, and the like);
   - trade evolutions by another method (a held item or an NPC who "trades" and returns the Pokémon).
5. **When the National Pokédex unlocks.** Required: after the Hall of Fame, when Professor Rowan upgrades it. Platinum additionally requires seeing all 210 Sinnoh species; that can be an option.

## Architecture

- **Data files**: generated JSON under `PokemonPlatinumEngine/Data/pokemon/`, `moves/`, `abilities/`, `items/` (checked in, so the game never needs a network or the decomp), loaded into `PokemonDatabase`, `MoveDatabase` and friends at start-up. The C# species and move definitions go away.
- **`tools/DataImporter`** (console app): reads a local decomp checkout and the PokeAPI CSVs, normalises names (`SPECIES_PIPLUP` → `Piplup`), writes the JSON. Deterministic and re-runnable; hand corrections live in override files it applies last.
- **Species model** gains: abilities (two plus hidden), gender ratio, egg groups, EV yield, base friendship, hatch cycles, held items, shape, colour, forms, regional dex numbers (Sinnoh 1–210), evolution methods.
- **Pokémon** gain: ability, EVs, held item, friendship, form, Pokérus (optional), met location and level.
- **Battle engine**: moves become table-driven: each move names an effect (like the decomp's battle effects: status, stat stages, multi-hit, recoil, draining, fixed damage, two-turn, trapping, weather, screens, healing, protection and so on) and the engine runs a handler per effect. Abilities and held items hook into fixed points (on entry, on damage, end of turn, on status, speed and damage modifiers).
- **Pokédex**: Sinnoh and National modes; National hidden until the unlock flag (plan 02 sets it after the Hall of Fame); sort and search; "area" view built from the encounter tables; forms viewer (optional).
- **Sprites at scale**: bake lazily on first use and cache the PNGs on disk (keyed by species and model version) instead of baking everything at start-up; icons the same.
- **Save format**: species by national number, with a migration for old saves.

## Sessions

### D1 · Data pipeline
`tools/DataImporter`, the JSON schema, runtime loaders; import species 1–493 (stats, types, abilities as data only, learnsets, evolutions, Pokédex text) and all Generation 4 moves (power, accuracy, PP, type, category, priority, target, flags, effect id). Keep the 23 existing species behaving the same. Change the model test to "every species has a model, hand-built or generated", and keep a list of species that must be hand-built.
**Done when** 493 species load, all tests pass and the game plays as before.
*Progress:* the runtime loaders and file formats are in (`GameDataFiles`, `docs/data-files.md`); the 23 species, 45 moves, 18 items and 10 maps moved from C# into `PokemonPlatinumEngine/Data/*.json` unchanged. Still to do: `tools/DataImporter` and the full import. Learnsets name three moves that don't exist yet (Aqua Jet, Headbutt, Synthesis; they fall back to Tackle); `DataFileTests` pins that list.

### D2 · Move effects
Table-driven effects: first the common ones (status, stat stages, multi-hit, recoil, draining, fixed and level damage, one-hit KOs, priority, two-turn, trapping, weather, screens, healing, protection, switching moves), then the rest of Generation 4's moves. Unit tests per effect family. Probably two sessions.

### D3 · Abilities and held items
Generation 4's abilities (start with the ones on story trainers' Pokémon: Intimidate, Levitate, Sturdy, Static, the starter abilities, weather abilities), held items and berries. Abilities not yet implemented show up but do nothing, marked in the data so a test can list them.

### D4 · Evolutions and forms
All Generation 4 evolution methods: level, stone, friendship (day and night), held item and time, known move, location (Mt. Coronet's magnetic field, the Moss Rock in Eterna Forest, the Ice Rock on Route 217), gender, the party-based ones; trade evolutions replaced (decision 4). Forms: Giratina's Origin Forme, Rotom's appliances, Shaymin's Sky Forme, Deoxys, Burmy and Wormadam cloaks, Shellos and Gastrodon's east and west seas, Unown letters, Castform, Cherrim, Arceus's plates. Optional: the Solaceon Day Care and eggs.

### D5 · Procedural models for everyone (after plan 04 · G7)
The model generator, built on plan 04's SDF modelling kit, from shape, colour, height, types and feature tags; lazy baking with the disk cache; a harness contact sheet showing every species (extend the `sheets` mode with paging). **Done when** all 1025 species (after D11) have a distinct model and start-up time doesn't grow.

### D6–D9 · Hand-built Sinnoh models (with plan 04's SDF modelling kit)
Platinum's 210 Sinnoh species in four batches of about 50, in the order they appear in the story: the Route 201–204 species first, the legendaries last. Check each batch on the harness contact sheet and in battle.

### D10 · Pokédex
Sinnoh and National modes, the unlock after the Hall of Fame, sorting and search, the area view, seen and caught counters, the completion reward (a diploma).

### D11 · Generations 5–9
Import species 494–1025 with their abilities and level-up moves; add the Fairy type (decision 2); implement the later moves those species learn by level, with the closest existing effect as a stand-in where needed. A test lists every move whose effect is only approximated.

### D12 · Every species obtainable
Encounter tables for Platinum's post-game, the Pal Park-style area, the new zone for later generations, gifts, legendary quests, the trade-evolution replacement. A completeness test fails for any species with no way to get it.

### D13 · Balance and polish
Level curves in the post-game areas, start-up time and memory with 1025 species, save migration, a full-dex playthrough check with debug tools (a debug menu to give any Pokémon and set flags).

## Risks

- **Move and ability count**: about 900 moves and 300 abilities across all generations. Generation 4's are the priority; later ones can approximate their effects at first, as long as the approximations are listed.
- **Model quality at scale**: generated models will look generic. Hand-build the species players see most, and let feature tags carry the rest.
- **Start-up time**: baking 1025 × 3 sprites at start-up would take minutes; bake lazily with a disk cache.

## Needs and gives

- **Needs** plan 01's areas for encounter tables and plan 02's Hall of Fame flag for the unlock.
- **Gives** plans 01 and 02 the species, moves and trainers' Pokémon they need. D1 comes before any chapter work.

## Status

- [ ] D1 Data pipeline
- [ ] D2 Move effects
- [ ] D3 Abilities and held items
- [ ] D4 Evolutions and forms
- [ ] D5 Procedural models for everyone
- [ ] D6 Hand-built Sinnoh models, batch 1
- [ ] D7 Hand-built Sinnoh models, batch 2
- [ ] D8 Hand-built Sinnoh models, batch 3
- [ ] D9 Hand-built Sinnoh models, batch 4
- [ ] D10 Pokédex
- [ ] D11 Generations 5–9
- [ ] D12 Every species obtainable
- [ ] D13 Balance and polish
