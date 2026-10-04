# DataImporter

Rebuilds the game's data files from public sources:

- `PokemonPlatinumEngine/Data/species.json`, `moves.json`, `abilities.json` and `items.json`
- `docs/mechanics/coverage.md`, the report of what the battle engine runs and what is still to write

```bash
dotnet run --project tools/DataImporter                        # fetch the pinned sources once, then import
dotnet run --project tools/DataImporter -- --decomp <pokeplatinum checkout> --pokeapi <pokeapi>/data/v2/csv
dotnet run --project tools/DataImporter -- --last-species 493  # stop after Platinum's National Pokédex
```

The run is deterministic: the same sources and overrides give the same files, so review a re-import as a diff. A file whose content didn't change is left untouched (the run says `unchanged`), so a session working on moves never rewrites `species.json`. It needs `git` on the path the first time, to fetch the sources into `tools/DataImporter/.cache` (ignored by git, about 130 MB). `--showdown <folder>` reads Pokémon Showdown's two tables from a folder of your own instead of fetching them.

## Sources

| What | From |
| --- | --- |
| Species 1–493: types, base stats, EV yields, catch rate, EXP yield, growth rate, gender ratio, hatch cycles, friendship, abilities, level-up learnsets, evolutions | Platinum's own data in [pret/pokeplatinum](https://github.com/pret/pokeplatinum) (`res/pokemon/<species>/data.json`) |
| Moves 1–467: type, category, power, accuracy, PP, priority, target, contact and the other Generation 4 flags, battle effect and its chance | the decompilation (`res/moves/<move>/data.json`) |
| Platinum's items: pocket, price, what using them does, hold effect, the move a TM teaches | the decompilation (`res/items/data/<item>.json`) |
| Names (in their modern spelling), categories, generations, heights, weights, egg groups, colours, shapes, hidden abilities, punch/sound/bite/pulse/ball/powder/dance flags | [PokeAPI](https://github.com/PokeAPI/pokeapi)'s CSV files (`data/v2/csv`) |
| Species 494–1025, moves 468 on, abilities 124 on, items after Generation 4 | PokeAPI: each species' default form with its newest learnset (Scarlet and Violet first) and current values |
| Descriptions of moves, abilities and items | PokeAPI's short effect texts (written by PokeAPI's contributors, not taken from the games) |
| Descriptions of TMs and HMs | written here from the move Platinum's machine teaches: "Teaches Focus Punch to a compatible Pokémon." and that move's own description. PokeAPI's text for a machine names the move a later generation gave it (Hone Claws for TM01) |
| Pokédex entries | generated from the data in our own words |
| The rest of Platinum's item table: the hold effect's number, Fling's power and effect, Natural Gift's power and type, what Pluck gets, what can be tossed or registered, how the bag and the battle use each item and with what parameters | the decompilation (`res/items/data/<item>.json`); never its descriptions or icons |
| The newest games' power, accuracy, PP, priority, type and category for Platinum's moves, where they differ (`modern`) | PokeAPI's `moves.csv`, which holds the current values |
| Each damaging move's power as a Z-Move and as a Max Move, each status move's Z-Power bonus, the Z-Moves, Max Moves and G-Max Moves themselves, and whose each Mega Stone and Z-Crystal is | [Pokémon Showdown](https://github.com/smogon/pokemon-showdown)'s `data/moves.ts` and `data/items.ts`, read by `Showdown.cs` for those numbers and names only; a move without a Z or Max power of its own gets one by Showdown's rule (`Importer.ZPowerOf`, `MaxPowerOf`) |

Both repositories are pinned to a commit in `Sources.cs`. To update, move a pin, run the importer and read the diff. `tools/MapImporter` reads the same commit of the decompilation, so move the pin for both and run both.

All three are pinned to a commit in `Sources.cs` (Showdown's two files and its licence are downloaded one by one rather than checked out).

PokeAPI's data is © Paul Hallett and PokéAPI contributors, used under its BSD 3-Clause licence (<https://github.com/PokeAPI/pokeapi/blob/master/LICENSE.md>). Pokémon Showdown is © Guangcong Luo and other contributors, used under the MIT licence; its notice is in `THIRD-PARTY-NOTICES.md` at the root of the repository. Pokémon and Pokémon character names are trademarks of Nintendo.

## What it decides

- **Platinum first.** Species 1–493 keep Platinum's typings, stats and abilities (Clefairy is Normal, Gengar has Levitate), and moves 1–467 keep Platinum's values (Tackle is 35 power, 95% accurate). Curse's `???` type becomes Ghost. The Fairy type belongs only to later species and moves (plan 03, decision 2).
- **Move effects.** `MoveEffects.cs` turns each move's battle effect into the fields the engine runs (status, stat changes, recoil, drain, flinch, confusion, critical-hit stage, healing). What the fields can't express is kept as `effect` with `support`:
  - `Partial`: the move still hits, or its stat change, status or healing still happens, and the rest waits for its code (multi-hit, two-turn moves, weather).
  - `None`: the move does nothing until its effect is written ("But nothing happened!"), because hitting without the rule would be wrong (fixed damage, Fake Out, OHKO moves, Protect).
  Moves from Generation 5 on share a Platinum move's verdict when PokeAPI gives them the same effect; otherwise only the effects listed in `MoveEffects.FullLaterEffects` (checked by hand) run in full. PokeAPI has no effect data yet for most Generation 9 moves, so they hit but their extras wait.
- **Evolutions.** Platinum's species keep Platinum's methods; later ones come from PokeAPI's `pokemon_evolution.csv`, each trigger mapped onto an `EvolutionMethod` the engine runs (`LaterEvolution` in `Importer.cs`). Where the original needs something this game lacks, the method is the stand-in `docs/mechanics/rulings.md` settles on. Rows that differ only in the form they lead to become one evolution, and an evolution that asks for friendship and something more is listed before the ones that ask for friendship alone, because the first that holds is the one that happens.
- **The later mechanics' data** (plan 06 · R1) is in as data only: the Z-Moves, Max Moves and G-Max Moves are moves of a `kind` of their own that do nothing yet and that nothing learns, and a Mega Stone names its species and the form it brings out without that form existing. Type Z-Moves are one move each (the games give each a physical and a special id), and G-Max Moves, which have no number in the games' move list, get ids from 2001.
- **Left out for now**: regional and other forms, the Mega and Gigantamax forms among them (and the evolutions that start from a regional form), TRs and items from later games that don't matter in battle; TM, tutor and egg learnsets; berries' growing data; the lists of moves Gravity and Heal Block stop. Plan 03 · D11 and plan 06 bring them in.

## Overrides

`Overrides/<file>.json` holds hand corrections, applied last: a name maps to the fields to change (`null` removes one), and a name the import doesn't produce adds a whole entry (the game's own Running Shoes and Distortion Orb). Comments are allowed. `species.json` there gives Cosmoem its two evolutions by time of day, since this game has no versions to choose between Solgaleo and Lunala.

## When an effect gets its code

When plan 06 writes a move effect, remove it from the not-yet-supported cases in `MoveEffects.cs` (or add the later moves' effect id to `FullLaterEffects`), run the importer and check the coverage report. Abilities and held items need no re-import: the report reads `AbilityEffectTable` and `HeldItemEffects` directly.
