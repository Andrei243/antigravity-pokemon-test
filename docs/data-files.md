# Game data files

The game's species, moves, items and maps live as JSON in `PokemonPlatinumEngine/Data/` and are copied next to the executable when it builds. `GameDataFiles` reads them: property names are camelCase, enum values are written by name (`"Grass"`, `"PokeBalls"`), comments and trailing commas are allowed. A missing or malformed file stops start-up with an error naming the file.

| File | Read by | Contents |
| --- | --- | --- |
| `species.json` | `PokemonDatabase` | A list of `PokemonSpecies`: dex number, name, category, types, base stats, catch rate, experience yield, growth rate, height, weight, Pokédex text, learnset (`level`, `moveName`) and evolution (`level`, `targetSpecies`). `secondaryType` and `evolution` are left out when there is none. |
| `moves.json` | `MoveDatabase` | A list of `MoveData`. `priority` and the secondary-effect fields (`inflictStatus`, `statusChancePercent`, `targetStatChange`, `statStageAmount`, `statChangeTargetSelf`, `statChangeChancePercent`, `recoilPercent`, `drainPercent`, `critStage`) are left out when they are zero or none. An unknown move name gives Tackle. |
| `items.json` | `ItemDatabase` | A list of `ItemData`. `effectValue` is HP restored, or a ball's catch multiplier × 10. |
| `maps/<Name>.json` | `MapDatabase` | One `MapFile` per map; the file name must match its `name`. |

## Maps

```jsonc
{
  "name": "Route201",            // what warps and saves refer to
  "displayName": "Route 201",
  "bgmTrack": "Route201",
  "interior": "None",            // None (outdoors), House, PokemonCenter, PokeMart, Lab
  "trees": "Round",              // Round or Pine
  "width": 36, "height": 22,
  "ground": [ "TTTT…", … ],      // one string per row, one character per tile (below)
  "solid":  [ "####…", … ],      // '#' blocks movement, '.' is open; furniture is already marked
  "overhead": [ … ],             // optional, same codes, '-' for none
  "props": [ { "type": "Table", "x": 4, "y": 4, "width": 2, "depth": 1 } ],
  "warps": [ { "sourceX": 14, "sourceY": 21, "targetMap": "TwinleafTown", "targetX": 11, "targetY": 1, "targetFacing": "Down" } ],
  "signboards": [ { "x": 13, "y": 11, "text": "Route 201\n…" } ],
  "npcs": [ … ],
  "wildEncounters": [ { "speciesName": "Bidoof", "minLevel": 2, "maxLevel": 4, "weight": 45 } ]
}
```

Tile codes (`TileCodes`):

| Code | Tile | Code | Tile |
| --- | --- | --- | --- |
| `.` | Grass | `r` | RoofRed |
| `*` | FlowerGrass | `b` | RoofBlue |
| `w` | TallGrass | `g` | RoofGreen |
| `:` | Path | `#` | Wall |
| `~` | Water | `D` | Door |
| `v` | LedgeDown | `_` | Floor |
| `T` | Tree | `S` | Signpost |
| `t` | TreeTrunk | `P` | PC |

The ground layer and the solid grid are separate because they don't always agree: doors sit in solid walls but are open, signposts are solid, and furniture props make the floor under them solid.

An NPC has `name`, `npcType` (picks the character model), `x`, `y`, `facing` and optional `dialog` lines, plus whichever of `isHealingNurse`, `isPokeMartClerk`, `isPCTerminal` and `isStarterBriefcase` apply. Give it an `id` only when something else refers to it (trainers, the starter briefcase); the others get a fresh one each load. A trainer carries a `trainer` block:

```json
"trainer": {
  "id": "trainer_tristan",
  "name": "Tristan",
  "trainerClass": "Youngster",
  "party": [ { "species": "Starly", "level": 4 } ],
  "prizeMoney": 160,
  "dialogueBefore": "…",
  "dialogueAfter": "…",
  "sightRange": 3
}
```

The trainer's `id` is what the save file records once they are beaten, so don't change it for a trainer who is already in the game. Party Pokémon are rolled fresh (gender, nature, moves for their level) every time the maps load.

`MapFile.FromMap` turns a `Map` back into a file, and `DataFileTests` checks that every map file loads and writes back out unchanged, so a generated file must use the same layout `GameDataFiles.Serialize` writes.
