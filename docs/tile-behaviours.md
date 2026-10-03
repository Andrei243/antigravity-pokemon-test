# Tile behaviours

Every tile of the imported world carries two things from Platinum's own map data: a **behaviour** (a number from 0 to 255 saying what the tile does) and a **blocked** flag. This page lists every behaviour Sinnoh's maps use, with the name the game gives it (`TileBehavior` in `Overworld/TileBehavior.cs`), what it means, and where it occurs.

`tools/MapImporter` writes this page: the counts come from all 666 chunks of the decompilation, the meanings from `TileBehaviors.Meaning`. To change a meaning, change it there and run the importer.

## How a tile is read

- A chunk is 32 by 32 tiles; each tile is 16 bits. The low byte is the behaviour, the top bit is the blocked flag, and the seven bits between are never set.
- **Blocked** tiles can't be walked onto. Most are plain (`None`): the tile data doesn't say whether a blocked tile is a tree, a fence or a cliff. Doors, ledges and rock-climb walls are blocked tiles whose behaviour lets a walker through in its own way.
- **Height** is separate: each chunk lists rectangles of flat or sloping ground ("plates"). A walker can't step onto a tile whose ground is 1.25 tiles or more above or below where they stand, which is what makes a cliff edge a wall without any blocked tile. Under a bridge two plates overlap, and a walker stays on the one nearest their own height.
- What a tile **looks like** (lawn, path, flowers, tree, rock face) is in neither: the importer reads it from the names of the textures the original drew there. See "World files" in `docs/data-files.md`.

## The values Sinnoh uses

| Value | Name | What it is | Wild Pokémon | Surfable | Tiles | Blocked | Where |
|---|---|---|---|---|---:|---:|---|
| `0x00` | `None` | Plain ground or a plain obstacle; only the blocked flag matters. |  |  | 582,450 | 330,825 | everywhere |
| `0x02` | `TallGrass` | Tall grass. | yes |  | 10,141 | 0 | 41 places |
| `0x03` | `VeryTallGrass` | Grass taller than the walker; too thick to cycle through. | yes |  | 1,066 | 0 | Route 210, Route 214, Route 229 |
| `0x08` | `CaveFloor` | The floor of a cave. | yes |  | 16,141 | 30 | 18 places |
| `0x0B` | `OldChateauFloor` | The floors of the Old Chateau, the only building with wild Pokémon in its rooms. | yes |  | 1,260 | 0 | Old Chateau, Route 209 |
| `0x0C` | `MountainFloor` | A rough floor without wild Pokémon of its own: the Underground's tunnels and parts of Eterna Forest and Victory Road. |  |  | 6,781 | 1,172 | underground, Eterna Forest, Victory Road |
| `0x10` | `River` | Fresh water. | yes | yes | 1,583 | 8 | 18 places |
| `0x13` | `Waterfall` | A waterfall: climbed with Waterfall; a surfer who enters from above is carried down. |  | yes | 55 | 33 | Pokémon League, Mt. Coronet, Victory Road and 3 more |
| `0x15` | `Sea` | The sea, and the still water of lakes and ponds. | yes | yes | 40,385 | 124 | 46 places |
| `0x16` | `Puddle` | A puddle: walked through with a splash, and it mirrors the walker. |  |  | 913 | 0 | Pastoria City, Fullmoon Island, Lake Verity and 8 more |
| `0x17` | `ShallowWater` | Water ankle deep: walked through, leaving ripples. |  |  | 378 | 0 | Sunyshore City, Route 213, Route 224 and 2 more |
| `0x1D` | `StillPuddle` | A puddle that mirrors the walker without splashing. |  |  | 512 | 0 | Verity Cavern, Valor Cavern, Acuity Cavern |
| `0x20` | `Ice` | Ice: a walker slides on until something stops them. |  |  | 835 | 12 | Snowpoint City, Snowpoint Temple |
| `0x21` | `Sand` | Sand, which keeps footprints. |  |  | 2,436 | 4 | Sunyshore City, unknown 250, Pal Park and 8 more |
| `0x2C` | `ShinyFloor` | A polished floor that mirrors the walker. |  |  | 444 | 0 | Pokémon League, Iron Ruins, Iceberg Ruins, Rock Peak Ruins |
| `0x2D` | `NoExplorerKit` | Ground where the Explorer Kit can't be used. |  |  | 2 | 0 | Eterna City, Veilstone City |
| `0x30` | `BlockEast` | Open ground whose east side is closed: no stepping across that edge. |  |  | 25 | 20 | Jubilife TV, Global Terminal, Oreburgh City |
| `0x31` | `BlockWest` | Open ground whose west side is closed. |  |  | 17 | 12 | Jubilife TV, Oreburgh City, Global Terminal |
| `0x38` | `LedgeEast` | A ledge hopped over eastward; a wall from every other side. |  |  | 37 | 37 | Pastoria City, Victory Road, unknown 250 and 7 more |
| `0x39` | `LedgeWest` | A ledge hopped over westward. |  |  | 65 | 65 | Pastoria City, Wayward Cave, Battle Tower and 5 more |
| `0x3B` | `LedgeSouth` | A ledge hopped over southward, the common kind. |  |  | 618 | 618 | 29 places |
| `0x3C` | `Unknown3C` | Not understood yet. |  |  | 2 | 0 | Distortion World |
| `0x3D` | `Unknown3D` | Not understood yet. |  |  | 2 | 0 | Distortion World |
| `0x3E` | `Unknown3E` | Not understood yet. |  |  | 8 | 6 | Pastoria City, Victory Road, unknown 250 and 4 more |
| `0x3F` | `Unknown3F` | Not understood yet. |  |  | 9 | 7 | Pastoria City, Wayward Cave, Battle Tower and 3 more |
| `0x40` | `SlideEast` | A floor that carries the walker east. |  |  | 11 | 0 | Fuego Ironworks |
| `0x41` | `SlideWest` | A floor that carries the walker west. |  |  | 18 | 0 | Fuego Ironworks |
| `0x42` | `SlideNorth` | A floor that carries the walker north. |  |  | 20 | 0 | Fuego Ironworks |
| `0x43` | `SlideSouth` | A floor that carries the walker south. |  |  | 9 | 0 | Fuego Ironworks |
| `0x49` | `BlockNorthAndSouth` | A walkway closed on its north and south sides. |  |  | 6 | 0 | Snowpoint City |
| `0x4A` | `BlockEastAndWest` | A walkway closed on its east and west sides. |  |  | 7 | 0 | Snowpoint City |
| `0x4B` | `RockClimbNorthSouth` | A rock face climbed north or south with Rock Climb. |  |  | 200 | 200 | 14 places |
| `0x4C` | `RockClimbEastWest` | A rock face climbed east or west with Rock Climb. |  |  | 47 | 47 | Mt. Coronet, Valor Lakefront, Acuity Lakefront and 5 more |
| `0x56` | `PastoriaGymHigh` | Pastoria Gym: the highest of its three tiers of floor; the water level decides which tiers can be walked. |  |  | 10 | 0 | Pastoria City |
| `0x57` | `PastoriaGymMiddle` | Pastoria Gym: the middle tier of floor. |  |  | 6 | 0 | Pastoria City |
| `0x58` | `PastoriaGymLow` | Pastoria Gym: the lowest tier of floor. |  |  | 10 | 0 | Pastoria City |
| `0x59` | `MovingFloor` | Ground whose height the game moves (lifts, the gyms' platforms and water); it blocks while the floor is elsewhere. |  |  | 1,149 | 207 | Canalave City, Pastoria City, Sunyshore City |
| `0x5A` | `LongLedgeNorth` | A drop two tiles deep, jumped northward. |  |  | 58 | 12 | Distortion World |
| `0x5B` | `LongLedgeSouth` | A drop two tiles deep, jumped southward. |  |  | 58 | 12 | Distortion World |
| `0x5C` | `LongLedgeWest` | A drop two tiles deep, jumped westward. |  |  | 54 | 45 | Distortion World |
| `0x5D` | `LongLedgeEast` | A drop two tiles deep, jumped eastward. |  |  | 54 | 45 | Distortion World |
| `0x5E` | `StairsEast` | Stairs at the side of a room: walking east onto them takes the warp there. |  |  | 83 | 0 | 20 places |
| `0x5F` | `StairsWest` | Stairs at the side of a room, taken walking west. |  |  | 84 | 0 | 20 places |
| `0x60` | `Unknown60` | Not understood yet. |  |  | 3 | 0 | unknown 324, unknown 325, Pal Park |
| `0x62` | `EntranceEast` | An opening (a cave mouth, a gate) entered walking east: it takes the warp there. |  |  | 41 | 30 | 24 places |
| `0x63` | `EntranceWest` | An opening entered walking west. |  |  | 41 | 30 | 21 places |
| `0x64` | `EntranceNorth` | An opening entered walking north. |  |  | 2 | 1 | Contest Hall, Mt. Coronet |
| `0x65` | `EntranceSouth` | An opening entered walking south. |  |  | 187 | 43 | 98 places |
| `0x67` | `WarpPanel` | A panel that warps whoever steps on it. |  |  | 86 | 0 | Global Terminal, Hearthome City, Galactic HQ, union room |
| `0x69` | `Door` | A door: blocked, but walking into it opens it and takes the warp there. |  |  | 192 | 192 | 46 places |
| `0x6A` | `EscalatorFacingBack` | An escalator that turns its rider round. |  |  | 6 | 0 | 17 places |
| `0x6B` | `Escalator` | An escalator. |  |  | 11 | 0 | Veilstone Store, Pal Park |
| `0x6C` | `ExitEast` | The mat at a room's east edge: walking east off it takes the warp there. |  |  | 56 | 0 | 24 places |
| `0x6D` | `ExitWest` | The mat at a room's west edge. |  |  | 54 | 0 | 22 places |
| `0x6E` | `ExitNorth` | The mat at a room's north edge. |  |  | 175 | 1 | 58 places |
| `0x6F` | `ExitSouth` | The mat at a room's south edge, where most rooms are left. |  |  | 94 | 0 | 39 places |
| `0x70` | `BridgeEnd` | The end of a bridge, where a walker steps onto its deck. |  |  | 200 | 0 | 16 places |
| `0x71` | `Bridge` | The deck of a bridge: the walker is on the upper level. |  |  | 1,405 | 0 | 19 places |
| `0x72` | `BridgeOverCave` | A bridge inside a cave. | yes |  | 184 | 0 | Victory Road |
| `0x73` | `BridgeOverWater` | A bridge with water under it that can be surfed. |  | yes | 190 | 0 | Sunyshore City, Sendoff Spring, Route 206 and 3 more |
| `0x75` | `BridgeOverSnow` | A bridge over snow. |  |  | 30 | 0 | Route 216 |
| `0x76` | `BikeBridgeNorthSouth` | A plank running north to south, crossed only on the Bicycle. |  |  | 104 | 0 | Route 210, Route 212, Route 227, Route 228 |
| `0x79` | `BikeBridgeNorthSouthOverSand` | A bicycle plank running north to south over sand. |  |  | 25 | 0 | Route 228 |
| `0x7A` | `BikeBridgeEastWest` | A plank running east to west, crossed only on the Bicycle. |  |  | 46 | 0 | Wayward Cave, Route 210, Route 212, Route 228 |
| `0x7B` | `BikeBridgeEastWestOverGrass` | A bicycle plank running east to west over tall grass. | yes |  | 6 | 0 | Wayward Cave |
| `0x7C` | `BikeBridgeEastWestOverWater` | A bicycle plank running east to west over water. |  | yes | 31 | 0 | Route 210, Route 212 |
| `0x7D` | `BikeBridgeEastWestOverSand` | A bicycle plank running east to west over sand. |  |  | 11 | 0 | Route 228 |
| `0x80` | `Counter` | A table or a counter: talking across it reaches whoever stands behind. |  |  | 635 | 635 | 43 places |
| `0x83` | `Computer` | A PC. |  |  | 30 | 30 | 26 places |
| `0x85` | `TownMap` | A map of the region on a wall. |  |  | 16 | 16 | 30 places |
| `0x86` | `Television` | A television. |  |  | 114 | 114 | 44 places |
| `0x88` | `Unknown88` | Not understood yet. |  |  | 47 | 47 | 16 places |
| `0x8E` | `Unknown8E` | Not understood yet. |  |  | 2 | 2 | Snowpoint City, Pokémon Mansion, Route 212 and 5 more |
| `0x8F` | `Unknown8F` | Not understood yet. |  |  | 7 | 4 | T.G. Eterna Bldg, Eterna City, Sunyshore City and 7 more |
| `0xA0` | `BerrySoil` | Soft soil where a Berry can be planted. |  |  | 121 | 120 | 27 places |
| `0xA1` | `DeepSnow` | Snow deep enough to slow a walker. |  |  | 2,687 | 0 | Snowpoint City, Acuity Lakefront, Route 216, Route 217 |
| `0xA2` | `DeeperSnow` | Deeper snow, slower still. |  |  | 703 | 0 | Snowpoint City, Acuity Lakefront, Route 216, Route 217 |
| `0xA3` | `DeepestSnow` | The deepest snow: a walker wades. |  |  | 171 | 0 | Snowpoint City, Acuity Lakefront, Route 217 |
| `0xA4` | `Mud` | Marsh mud. |  |  | 661 | 0 | Route 212, Great Marsh |
| `0xA5` | `DeepMud` | Deep marsh mud, where a walker can get stuck. |  |  | 542 | 0 | Route 212, Great Marsh |
| `0xA6` | `MarshGrass` | Marsh mud with grass growing in it. | yes |  | 2,510 | 0 | Pastoria City, Great Marsh |
| `0xA7` | `DeepMarshGrass` | Deep marsh mud with grass. | yes |  | 584 | 0 | Great Marsh |
| `0xA8` | `ShallowSnow` | A thin cover of snow that keeps footprints. |  |  | 1,443 | 7 | Snowpoint City, Mt. Coronet, Lake Acuity and 3 more |
| `0xA9` | `ShadedSnow` | Thin snow in shade. |  |  | 84 | 0 | Lake Verity, Twinleaf Town |
| `0xD7` | `BikeRampEast` | A ramp jumped eastward on a fast Bicycle. |  |  | 9 | 9 | Victory Road, Oreburgh Gate, Wayward Cave |
| `0xD8` | `BikeRampWest` | A ramp jumped westward on a fast Bicycle. |  |  | 15 | 15 | Victory Road, Oreburgh Gate, Wayward Cave, Route 227 |
| `0xD9` | `BikeSlopeTop` | The top of a muddy slope that only a fast Bicycle climbs. |  |  | 17 | 0 | Wayward Cave, Route 207, Route 209 and 2 more |
| `0xDA` | `BikeSlopeBottom` | The foot of a muddy slope. |  |  | 17 | 0 | Wayward Cave, Route 207, Route 209 and 2 more |
| `0xDB` | `BikeRack` | A bicycle rack. |  |  | 33 | 33 | Jubilife City, Oreburgh City, Eterna City, Pastoria City |
| `0xE0` | `SmallBookshelf` | A low bookshelf. |  |  | 36 | 36 | Pokétch Co., Jubilife TV, Trainers’ School and 6 more |
| `0xE1` | `Bookshelf` | A bookshelf. |  |  | 84 | 84 | 22 places |
| `0xE2` | `TallBookshelf` | A second kind of bookshelf. |  |  | 18 | 18 | Pokétch Co., Pastoria City, Old Chateau |
| `0xE4` | `TrashCan` | A trash can. |  |  | 37 | 37 | 18 places |
| `0xE5` | `ShopShelf` | A shelf of goods in a shop. |  |  | 132 | 120 | 23 places |

94 values are in use, on 681,984 tiles; 335,165 of those tiles are blocked.

## What the list shows

- **Ledges** exist in three directions only: south (by far the most common), east and west. No map has a ledge jumped northward.
- **Water** is three behaviours: `Sea` (which also covers lakes and ponds), `River` and `Waterfall`. A few water tiles are blocked: rocks standing in the sea.
- **Wild Pokémon** appear on tall grass, very tall grass, cave floors, the Old Chateau's floors, marsh grass and water. `MountainFloor` has none of its own.
- **Doors** are always blocked: a walker bumps into the door and the warp there takes them inside. The mats and openings (`Exit…`, `Entrance…`, `Stairs…`) are open tiles that take their warp when walked off in the right direction.
- **Bridges** tell a walker which level they are on; the ground's height comes from the plates.
- **Not understood yet**: `Unknown3C` to `Unknown3F`, `Unknown60`, `Unknown88`, `Unknown8E` and `Unknown8F`, 80 tiles in all. The decompilation has no name for them either. Work out each from the place it occurs when that place is built.

## Sources

The numbers and their order are Platinum's (`include/constants/field/map_tile_behaviors.h` in pret/pokeplatinum, where the names are the decompilation's); which behaviours have wild Pokémon or can be surfed is the table at the top of `src/map_tile_behavior.c`; the blocked bit and the 1.25-tile step are in `src/terrain_collision_manager.c`. The names and descriptions here are our own.
