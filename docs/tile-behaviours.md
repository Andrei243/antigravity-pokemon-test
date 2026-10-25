# Tile behaviours

Every tile of the imported world carries two things from Platinum's own map data: a **behaviour** (a number from 0 to 255 saying what the tile does) and a **blocked** flag. This page lists every behaviour Sinnoh's maps use, with the name the game gives it (`TileBehavior` in `Overworld/TileBehavior.cs`), what it means, and where it occurs.

`tools/MapImporter` writes this page: the counts come from all 666 chunks of the decompilation, the meanings from `TileBehaviors.Meaning` and what the game does with each from `TileBehaviors.InGame`. To change either, change it there and run the importer.

## How a tile is read

- A chunk is 32 by 32 tiles; each tile is 16 bits. The low byte is the behaviour, the top bit is the blocked flag, and the seven bits between are never set.
- **Blocked** tiles can't be walked onto. Most are plain (`None`): the tile data doesn't say whether a blocked tile is a tree, a fence or a cliff. Doors, ledges and rock-climb walls are blocked tiles whose behaviour lets a walker through in its own way.
- **Height** is separate: each chunk lists rectangles of flat or sloping ground ("plates"). A walker can't step onto a tile whose ground is 1.25 tiles or more above or below where they stand, which is what makes a cliff edge a wall without any blocked tile. Under a bridge two plates overlap, and a walker stays on the one nearest their own height.
- What a tile **looks like** (lawn, path, flowers, tree, rock face) is in neither: the importer reads it from the names of the textures the original drew there. See "World files" in `docs/data-files.md`.

## The values Sinnoh uses

| Value | Name | What it is | In the game | Wild Pokémon | Surfable | Tiles | Blocked | Where |
|---|---|---|---|---|---|---:|---:|---|
| `0x00` | `None` | Plain ground or a plain obstacle; only the blocked flag matters. | Ground like any other: only the blocked flag and the height matter. |  |  | 582,450 | 330,825 | everywhere |
| `0x02` | `TallGrass` | Tall grass. | Walked on; wild Pokémon from the area's land table. | yes |  | 10,141 | 0 | 41 places |
| `0x03` | `VeryTallGrass` | Grass taller than the walker; too thick to cycle through. | **Rule.** Wild Pokémon, more often than in tall grass; no Bicycles. | yes |  | 1,066 | 0 | Route 210, Route 214, Route 229 |
| `0x08` | `CaveFloor` | The floor of a cave. | Walked on; wild Pokémon from the area's land table. | yes |  | 16,141 | 30 | 18 places |
| `0x0B` | `OldChateauFloor` | The floors of the Old Chateau and the Lost Tower, the buildings with wild Pokémon in their rooms. | Walked on; wild Pokémon from the area's land table. | yes |  | 1,260 | 0 | Old Chateau, Route 209 |
| `0x0C` | `MountainFloor` | A rough floor without wild Pokémon of its own: the Underground's tunnels and parts of Eterna Forest and Victory Road. | Ground like any other: only the blocked flag and the height matter. |  |  | 6,781 | 1,172 | underground, Eterna Forest, Victory Road |
| `0x10` | `River` | Fresh water. | **Rule.** Stops a walker; surfed with Surf, meeting the area's water Pokémon. | yes | yes | 1,583 | 8 | 18 places |
| `0x13` | `Waterfall` | A waterfall: climbed with Waterfall; a surfer who enters from above is carried down. | **Rule.** Surfed up with Waterfall and down without; never sideways. |  | yes | 55 | 33 | Pokémon League, Mt. Coronet, Victory Road and 3 more |
| `0x15` | `Sea` | The sea, and the still water of lakes and ponds. | **Rule.** Stops a walker; surfed with Surf, meeting the area's water Pokémon. | yes | yes | 40,385 | 124 | 46 places |
| `0x16` | `Puddle` | A puddle: walked through with a splash, and it mirrors the walker. | Walked through with a splash: a ring and two drops. A puddle's look, and whoever stands in it is mirrored. |  |  | 913 | 0 | Pastoria City, Fullmoon Island, Lake Verity and 8 more |
| `0x17` | `ShallowWater` | Water ankle deep: walked through, leaving ripples. | Walked through, leaving a ring on the water. |  |  | 378 | 0 | Sunyshore City, Route 213, Route 224 and 2 more |
| `0x1D` | `StillPuddle` | A puddle that mirrors the walker without splashing. | Walked through. A puddle's look, and whoever stands in it is mirrored. |  |  | 512 | 0 | Verity Cavern, Valor Cavern, Acuity Cavern |
| `0x20` | `Ice` | Ice: a walker slides on until something stops them. | **Rule.** Whoever steps on it slides on until something stops them; down a slope the slide gathers speed and up one loses it, and a slide with speed breaks a snowball (IceSlide). |  |  | 835 | 12 | Snowpoint City, Snowpoint Temple |
| `0x21` | `Sand` | Sand, which keeps footprints. | Walked on; every step leaves a footprint that fades. |  |  | 2,436 | 4 | Sunyshore City, unknown 250, Pal Park and 8 more |
| `0x2C` | `ShinyFloor` | A polished floor that mirrors the walker. | Walked on; whoever stands on it is mirrored in the floor. |  |  | 444 | 0 | Pokémon League, Iron Ruins, Iceberg Ruins, Rock Peak Ruins |
| `0x2D` | `NoExplorerKit` | Ground where the Explorer Kit can't be used. | Ground like any other: only the blocked flag and the height matter. |  |  | 2 | 0 | Eterna City, Veilstone City |
| `0x30` | `BlockEast` | Open ground whose east side is closed: no stepping across that edge. | **Rule.** No step across its closed side, either way. |  |  | 25 | 20 | Jubilife TV, Global Terminal, Oreburgh City |
| `0x31` | `BlockWest` | Open ground whose west side is closed. | **Rule.** No step across its closed side, either way. |  |  | 17 | 12 | Jubilife TV, Oreburgh City, Global Terminal |
| `0x38` | `LedgeEast` | A ledge hopped over eastward; a wall from every other side. | **Rule.** Hopped the way it faces, two tiles on; a wall from every other side. |  |  | 37 | 37 | Pastoria City, Victory Road, unknown 250 and 7 more |
| `0x39` | `LedgeWest` | A ledge hopped over westward. | **Rule.** Hopped the way it faces, two tiles on; a wall from every other side. |  |  | 65 | 65 | Pastoria City, Wayward Cave, Battle Tower and 5 more |
| `0x3B` | `LedgeSouth` | A ledge hopped over southward, the common kind. | **Rule.** Hopped the way it faces, two tiles on; a wall from every other side. |  |  | 618 | 618 | 29 places |
| `0x3C` | `Unknown3C` | Not understood yet; by its place in the list, a ledge's north-east corner. | *Not yet.* Not understood. |  |  | 2 | 0 | Distortion World |
| `0x3D` | `Unknown3D` | Not understood yet; by its place in the list, a ledge's north-west corner. | *Not yet.* Not understood. |  |  | 2 | 0 | Distortion World |
| `0x3E` | `LedgeCornerSouthEast` | The corner where a ledge hopped eastward ends at its south: a wall from every side. | A blocked tile, drawn as the end of its ledge. |  |  | 8 | 6 | Pastoria City, Victory Road, unknown 250 and 4 more |
| `0x3F` | `LedgeCornerSouthWest` | The corner where a ledge hopped westward ends at its south: a wall from every side. | A blocked tile, drawn as the end of its ledge. |  |  | 9 | 7 | Pastoria City, Wayward Cave, Battle Tower and 3 more |
| `0x40` | `SlideEast` | A floor that carries the walker east. | **Rule.** Carries whoever steps on it along. |  |  | 11 | 0 | Fuego Ironworks |
| `0x41` | `SlideWest` | A floor that carries the walker west. | **Rule.** Carries whoever steps on it along. |  |  | 18 | 0 | Fuego Ironworks |
| `0x42` | `SlideNorth` | A floor that carries the walker north. | **Rule.** Carries whoever steps on it along. |  |  | 20 | 0 | Fuego Ironworks |
| `0x43` | `SlideSouth` | A floor that carries the walker south. | **Rule.** Carries whoever steps on it along. |  |  | 9 | 0 | Fuego Ironworks |
| `0x49` | `BlockNorthAndSouth` | A walkway closed on its north and south sides. | **Rule.** No step across its closed side, either way. |  |  | 6 | 0 | Snowpoint City |
| `0x4A` | `BlockEastAndWest` | A walkway closed on its east and west sides. | **Rule.** No step across its closed side, either way. |  |  | 7 | 0 | Snowpoint City |
| `0x4B` | `RockClimbNorthSouth` | A rock face climbed north or south with Rock Climb. | **Rule.** Climbed to its far end by a party that knows Rock Climb. |  |  | 200 | 200 | 14 places |
| `0x4C` | `RockClimbEastWest` | A rock face climbed east or west with Rock Climb. | **Rule.** Climbed to its far end by a party that knows Rock Climb. |  |  | 47 | 47 | Mt. Coronet, Valor Lakefront, Acuity Lakefront and 5 more |
| `0x56` | `PastoriaGymHigh` | Pastoria Gym: the highest of its three tiers of floor; the water level decides which tiers can be walked. | *Not yet.* Floors whose height the game moves (plan 01 · M9). |  |  | 10 | 0 | Pastoria City |
| `0x57` | `PastoriaGymMiddle` | Pastoria Gym: the middle tier of floor. | *Not yet.* Floors whose height the game moves (plan 01 · M9). |  |  | 6 | 0 | Pastoria City |
| `0x58` | `PastoriaGymLow` | Pastoria Gym: the lowest tier of floor. | *Not yet.* Floors whose height the game moves (plan 01 · M9). |  |  | 10 | 0 | Pastoria City |
| `0x59` | `MovingFloor` | Ground whose height the game moves (lifts, the gyms' platforms and water); it blocks while the floor is elsewhere. | *Not yet.* Floors whose height the game moves (plan 01 · M9). |  |  | 1,149 | 207 | Canalave City, Pastoria City, Sunyshore City |
| `0x5A` | `LongLedgeNorth` | A gap in the Distortion World, jumped northward to the third tile on. | **Rule.** A gap in the Distortion World: jumped the way it faces, over it and the tile past it to the third tile on, on foot; the drop it is from every other side (`FieldMovement.LongJumpDirection`). |  |  | 58 | 12 | Distortion World |
| `0x5B` | `LongLedgeSouth` | A gap in the Distortion World, jumped southward to the third tile on. | **Rule.** A gap in the Distortion World: jumped the way it faces, over it and the tile past it to the third tile on, on foot; the drop it is from every other side (`FieldMovement.LongJumpDirection`). |  |  | 58 | 12 | Distortion World |
| `0x5C` | `LongLedgeWest` | A gap in the Distortion World, jumped westward to the third tile on. | **Rule.** A gap in the Distortion World: jumped the way it faces, over it and the tile past it to the third tile on, on foot; the drop it is from every other side (`FieldMovement.LongJumpDirection`). |  |  | 54 | 45 | Distortion World |
| `0x5D` | `LongLedgeEast` | A gap in the Distortion World, jumped eastward to the third tile on. | **Rule.** A gap in the Distortion World: jumped the way it faces, over it and the tile past it to the third tile on, on foot; the drop it is from every other side (`FieldMovement.LongJumpDirection`). |  |  | 54 | 45 | Distortion World |
| `0x5E` | `StairsEast` | Stairs at the side of a room: walking east onto them takes the warp there. | *Not yet.* Its warp is taken by stepping onto the tile; taking it by walking off the right way comes with the rooms (plan 01 · M11). |  |  | 83 | 0 | 20 places |
| `0x5F` | `StairsWest` | Stairs at the side of a room, taken walking west. | *Not yet.* Its warp is taken by stepping onto the tile; taking it by walking off the right way comes with the rooms (plan 01 · M11). |  |  | 84 | 0 | 20 places |
| `0x60` | `Unknown60` | Not understood yet. | *Not yet.* Not understood. |  |  | 3 | 0 | unknown 324, unknown 325, Pal Park |
| `0x62` | `EntranceEast` | An opening (a cave mouth, a gate) entered walking east: it takes the warp there. | *Not yet.* Its warp is taken by stepping onto the tile; taking it by walking off the right way comes with the rooms (plan 01 · M11). |  |  | 41 | 30 | 24 places |
| `0x63` | `EntranceWest` | An opening entered walking west. | *Not yet.* Its warp is taken by stepping onto the tile; taking it by walking off the right way comes with the rooms (plan 01 · M11). |  |  | 41 | 30 | 21 places |
| `0x64` | `EntranceNorth` | An opening entered walking north. | *Not yet.* Its warp is taken by stepping onto the tile; taking it by walking off the right way comes with the rooms (plan 01 · M11). |  |  | 2 | 1 | Contest Hall, Mt. Coronet |
| `0x65` | `EntranceSouth` | An opening entered walking south. | *Not yet.* Its warp is taken by stepping onto the tile; taking it by walking off the right way comes with the rooms (plan 01 · M11). |  |  | 187 | 43 | 98 places |
| `0x67` | `WarpPanel` | A panel that warps whoever steps on it. | Walked onto; whoever ends a step on it is carried to the panel its warp names and stands there facing as they did, turning on the spot on the way. Arriving is no step, so the far panel carries them back only once they step off it and on again (`WarpPanels`; the floors of Team Galactic's headquarters). |  |  | 86 | 0 | Global Terminal, Hearthome City, Galactic HQ, union room |
| `0x69` | `Door` | A door: blocked, but walking into it opens it and takes the warp there. | A blocked tile until a warp opens it. |  |  | 192 | 192 | 46 places |
| `0x6A` | `EscalatorFacingBack` | An escalator that turns its rider round. | *Not yet.* Comes with the buildings that have them (plan 01 · M11). |  |  | 6 | 0 | 17 places |
| `0x6B` | `Escalator` | An escalator. | *Not yet.* Comes with the buildings that have them (plan 01 · M11). |  |  | 11 | 0 | Veilstone Store, Pal Park |
| `0x6C` | `ExitEast` | The mat at a room's east edge: walking east off it takes the warp there. | *Not yet.* Its warp is taken by stepping onto the tile; taking it by walking off the right way comes with the rooms (plan 01 · M11). |  |  | 56 | 0 | 24 places |
| `0x6D` | `ExitWest` | The mat at a room's west edge. | *Not yet.* Its warp is taken by stepping onto the tile; taking it by walking off the right way comes with the rooms (plan 01 · M11). |  |  | 54 | 0 | 22 places |
| `0x6E` | `ExitNorth` | The mat at a room's north edge. | *Not yet.* Its warp is taken by stepping onto the tile; taking it by walking off the right way comes with the rooms (plan 01 · M11). |  |  | 175 | 1 | 58 places |
| `0x6F` | `ExitSouth` | The mat at a room's south edge, where most rooms are left. | *Not yet.* Its warp is taken by stepping onto the tile; taking it by walking off the right way comes with the rooms (plan 01 · M11). |  |  | 94 | 0 | 39 places |
| `0x70` | `BridgeEnd` | The end of a bridge, where a walker steps onto its deck. | Ground like any other: only the blocked flag and the height matter. |  |  | 200 | 0 | 16 places |
| `0x71` | `Bridge` | The deck of a bridge: the walker is on the upper level. | Ground like any other: only the blocked flag and the height matter. |  |  | 1,405 | 0 | 19 places |
| `0x72` | `BridgeOverCave` | A bridge inside a cave. | Walked on; wild Pokémon from the area's land table. | yes |  | 184 | 0 | Victory Road |
| `0x73` | `BridgeOverWater` | A bridge with water under it that can be surfed. | **Rule.** Crossed on its deck and surfed under, whichever level one is on. |  | yes | 190 | 0 | Sunyshore City, Sendoff Spring, Route 206 and 3 more |
| `0x75` | `BridgeOverSnow` | A bridge over snow. | Ground like any other: only the blocked flag and the height matter. |  |  | 30 | 0 | Route 216 |
| `0x76` | `BikeBridgeNorthSouth` | A plank running north to south, crossed only on the Bicycle. | **Rule.** Ridden along on a Bicycle, never across, and never walked. |  |  | 104 | 0 | Route 210, Route 212, Route 227, Route 228 |
| `0x79` | `BikeBridgeNorthSouthOverSand` | A bicycle plank running north to south over sand. | **Rule.** Ridden along on a Bicycle, never across, and never walked. |  |  | 25 | 0 | Route 228 |
| `0x7A` | `BikeBridgeEastWest` | A plank running east to west, crossed only on the Bicycle. | **Rule.** Ridden along on a Bicycle, never across, and never walked. |  |  | 46 | 0 | Wayward Cave, Route 210, Route 212, Route 228 |
| `0x7B` | `BikeBridgeEastWestOverGrass` | A bicycle plank running east to west over tall grass. | **Rule.** Ridden along on a Bicycle, never across, and never walked. | yes |  | 6 | 0 | Wayward Cave |
| `0x7C` | `BikeBridgeEastWestOverWater` | A bicycle plank running east to west over water. | **Rule.** Crossed on its deck and surfed under, whichever level one is on. |  | yes | 31 | 0 | Route 210, Route 212 |
| `0x7D` | `BikeBridgeEastWestOverSand` | A bicycle plank running east to west over sand. | **Rule.** Ridden along on a Bicycle, never across, and never walked. |  |  | 11 | 0 | Route 228 |
| `0x80` | `Counter` | A table or a counter: talking across it reaches whoever stands behind. | Ground like any other: only the blocked flag and the height matter. |  |  | 635 | 635 | 43 places |
| `0x83` | `Computer` | A PC. | Ground like any other: only the blocked flag and the height matter. |  |  | 30 | 30 | 26 places |
| `0x85` | `TownMap` | A map of the region on a wall. | Ground like any other: only the blocked flag and the height matter. |  |  | 16 | 16 | 30 places |
| `0x86` | `Television` | A television. | Ground like any other: only the blocked flag and the height matter. |  |  | 114 | 114 | 44 places |
| `0x88` | `Unknown88` | Not understood yet. | *Not yet.* Not understood. |  |  | 47 | 47 | 16 places |
| `0x8E` | `Unknown8E` | Not understood yet. | *Not yet.* Not understood. |  |  | 2 | 2 | Snowpoint City, Pokémon Mansion, Route 212 and 5 more |
| `0x8F` | `Unknown8F` | Not understood yet. | *Not yet.* Not understood. |  |  | 7 | 4 | T.G. Eterna Bldg, Eterna City, Sunyshore City and 7 more |
| `0xA0` | `BerrySoil` | Soft soil where a Berry can be planted. | Ground like any other: only the blocked flag and the height matter. |  |  | 121 | 120 | 27 places |
| `0xA1` | `DeepSnow` | Snow deep enough to slow a walker. | **Rule.** No running, no Bicycles; a walker sinks to the ankle and leaves footprints. |  |  | 2,687 | 0 | Snowpoint City, Acuity Lakefront, Route 216, Route 217 |
| `0xA2` | `DeeperSnow` | Deeper snow, slower still. | **Rule.** Half a walk's pace, no Bicycles; a walker sinks to the shin and leaves footprints. |  |  | 703 | 0 | Snowpoint City, Acuity Lakefront, Route 216, Route 217 |
| `0xA3` | `DeepestSnow` | The deepest snow: a walker wades. | **Rule.** A quarter of a walk's pace, no Bicycles; a walker sinks to the knee and leaves footprints. |  |  | 171 | 0 | Snowpoint City, Acuity Lakefront, Route 217 |
| `0xA4` | `Mud` | Marsh mud. | **Rule.** No running, no Bicycles. (Platinum's sinking in the marsh is not built.) |  |  | 661 | 0 | Route 212, Great Marsh |
| `0xA5` | `DeepMud` | Deep marsh mud, where a walker can get stuck. | **Rule.** Half a walk's pace, no Bicycles. (Getting stuck, as in Platinum, is not built.) |  |  | 542 | 0 | Route 212, Great Marsh |
| `0xA6` | `MarshGrass` | Marsh mud with grass growing in it. | **Rule.** No running, no Bicycles. (Platinum's sinking in the marsh is not built.) | yes |  | 2,510 | 0 | Pastoria City, Great Marsh |
| `0xA7` | `DeepMarshGrass` | Deep marsh mud with grass. | **Rule.** Half a walk's pace, no Bicycles. (Getting stuck, as in Platinum, is not built.) | yes |  | 584 | 0 | Great Marsh |
| `0xA8` | `ShallowSnow` | A thin cover of snow that keeps footprints. | **Rule.** No Bicycles. Every step leaves a footprint. |  |  | 1,443 | 7 | Snowpoint City, Mt. Coronet, Lake Acuity and 3 more |
| `0xA9` | `ShadedSnow` | Thin snow in shade. | Walked on; every step leaves a footprint that fades. |  |  | 84 | 0 | Lake Verity, Twinleaf Town |
| `0xD7` | `BikeRampEast` | A ramp jumped eastward on a fast Bicycle. | **Rule.** On a Bicycle going its way, jumped: three tiles on in top gear, one in low. A wall on foot (`FieldMovement.RampDirection`). |  |  | 9 | 9 | Victory Road, Oreburgh Gate, Wayward Cave |
| `0xD8` | `BikeRampWest` | A ramp jumped westward on a fast Bicycle. | **Rule.** On a Bicycle going its way, jumped: three tiles on in top gear, one in low. A wall on foot (`FieldMovement.RampDirection`). |  |  | 15 | 15 | Victory Road, Oreburgh Gate, Wayward Cave, Route 227 |
| `0xD9` | `BikeSlopeTop` | The top of a muddy slope that only a fast Bicycle climbs. | **Rule.** Climbed only at a bicycle's full speed: Platinum's Bicycle in its fast gear, or the Mach Bike (plan 21 · H3). (The slide back down is not played.) |  |  | 17 | 0 | Wayward Cave, Route 207, Route 209 and 2 more |
| `0xDA` | `BikeSlopeBottom` | The foot of a muddy slope. | **Rule.** Climbed only at a bicycle's full speed: Platinum's Bicycle in its fast gear, or the Mach Bike (plan 21 · H3). (The slide back down is not played.) |  |  | 17 | 0 | Wayward Cave, Route 207, Route 209 and 2 more |
| `0xDB` | `BikeRack` | A bicycle rack. | Ground like any other: only the blocked flag and the height matter. |  |  | 33 | 33 | Jubilife City, Oreburgh City, Eterna City, Pastoria City |
| `0xE0` | `SmallBookshelf` | A low bookshelf. | Ground like any other: only the blocked flag and the height matter. |  |  | 36 | 36 | Pokétch Co., Jubilife TV, Trainers’ School and 6 more |
| `0xE1` | `Bookshelf` | A bookshelf. | Ground like any other: only the blocked flag and the height matter. |  |  | 84 | 84 | 22 places |
| `0xE2` | `TallBookshelf` | A second kind of bookshelf. | Ground like any other: only the blocked flag and the height matter. |  |  | 18 | 18 | Pokétch Co., Pastoria City, Old Chateau |
| `0xE4` | `TrashCan` | A trash can. | Ground like any other: only the blocked flag and the height matter. |  |  | 37 | 37 | 18 places |
| `0xE5` | `ShopShelf` | A shelf of goods in a shop. | Ground like any other: only the blocked flag and the height matter. |  |  | 132 | 120 | 23 places |

94 values are in use, on 681,984 tiles; 335,165 of those tiles are blocked.

## What the game does with them

The rules of walking are `Overworld/FieldMovement.cs` (plan 01 · M3), which follows the original's `src/player_move.c`: 41 of the behaviours have a rule there, 31 are ground like any other as far as a step goes, and 22 wait for the place that needs them. `FieldMovementTests.EveryBehaviourIsAccountedFor` fails if this table and the rules disagree.

- **A step** onto a tile is refused if the tile is blocked, if someone stands on it, or if its ground is 1.25 tiles or more above or below; otherwise the behaviours of the tile left and the tile entered decide.
- **Pace** is Platinum's five speeds. A walk is 4.5 tiles a second and a run 8; surfing goes at a run; the Bicycle at a run in its low gear and half as fast again in its high one.
- **Wild Pokémon** are met by Platinum's own odds (`EncounterSteps`): after the first few steps, four attempts in ten get through (seven in grass taller than the walker or on a Bicycle), and then the area's rate decides.
- **Simpler than the original**, on purpose and until the places that need more are built: marsh mud only slows; a muddy slope refuses a climb instead of letting one slide back; Surf, Waterfall and Rock Climb ask only that a party Pokémon knows the move (the badges come with the story, plan 02).

## What the list shows

- **Ledges** exist in three directions only: south (by far the most common), east and west. No map has a ledge jumped northward. `LedgeCornerSouthEast` and `LedgeCornerSouthWest` are the blocked corner pieces where a side ledge ends at its south; the decompilation has no name for them, and they were worked out from Verity Lakefront, where one closes the ledge beside the lake's entrance.
- **Water** is three behaviours: `Sea` (which also covers lakes and ponds), `River` and `Waterfall`. A few water tiles are blocked: rocks standing in the sea.
- **Wild Pokémon** appear on tall grass, very tall grass, cave floors, the Old Chateau's floors, marsh grass and water. `MountainFloor` has none of its own.
- **Doors** are always blocked: a walker bumps into the door and the warp there takes them inside. The mats and openings (`Exit…`, `Entrance…`, `Stairs…`) are open tiles that take their warp when walked off in the right direction.
- **Bridges** tell a walker which level they are on; the ground's height comes from the plates.
- **Not understood yet**: `Unknown3C`, `Unknown3D`, `Unknown60`, `Unknown88`, `Unknown8E` and `Unknown8F`, 63 tiles in all. The decompilation has no name for them either. Work out each from the place it occurs when that place is built.

## Sources

The numbers and their order are Platinum's (`include/constants/field/map_tile_behaviors.h` in pret/pokeplatinum, where the names are the decompilation's); which behaviours have wild Pokémon or can be surfed is the table at the top of `src/map_tile_behavior.c`; the blocked bit and the 1.25-tile step are in `src/terrain_collision_manager.c`. The names and descriptions here are our own.

## The GBA's behaviours

*Written by `tools/MapImporter --game firered` or `--game emerald` (plan 18 · W6), which read both games; the Sinnoh run keeps it as it is.*

The Game Boy Advance's maps give a tile a behaviour (`MB_*`, numbered differently in FireRed and Emerald) and, apart from it, two bits of collision and four of elevation. The reader (`tools/MapImporter/Gba/GbaBehaviours.cs`) turns each behaviour into one of this game's values by its name: Platinum's own value where Platinum has one that means the same, a value of the GBA's own among the ones Platinum leaves unused at the top of its byte (`0xED` to `0xFD`) where it has none, and `None` for a behaviour that only says how a tile is read or drawn (a sign, a cupboard), which is an event's or the art's business. Collision bits set make the tile blocked. Elevation becomes height: a level is 1.4 tiles above elevation 3, elevation 0 (any level) slopes between the levels beside it, the water's elevation 1 takes the nearest land's level and a bridge's 15 is a deck over the ground.

| Behaviour | FireRed | Emerald | Becomes | Value | FireRed tiles | Emerald tiles |
|---|---|---|---|---|---:|---:|
| `MB_ADVERTISING_POSTER` | 0x98 |  | `None` (not in the table) | `0x00` | 12 |  |
| `MB_ANIMATED_DOOR` |  | 0x69 | `Door` | `0x69` |  | 251 |
| `MB_AQUA_HIDEOUT_WARP` |  | 0x67 | `WarpPanel` | `0x67` |  | 58 |
| `MB_ASHGRASS` |  | 0x24 | `TallGrass` | `0x02` |  | 287 |
| `MB_BATTLE_PYRAMID_WARP` |  | 0x0D | `WarpPanel` | `0x67` |  | 16 |
| `MB_BATTLE_RECORDS` | 0x8E |  | `None` (not in the table) | `0x00` | 19 |  |
| `MB_BERRY_TREE_SOIL` |  | 0xA0 | `BerrySoil` | `0xA0` |  | 87 |
| `MB_BIKE_BRIDGE_OVER_BARRIER` |  | 0x7F | `Bridge` | `0x71` |  | 6 |
| `MB_BLINKING_LIGHTS` | 0x9E |  | `None` (not in the table) | `0x00` | 117 |  |
| `MB_BLUEPRINT` |  | 0xE6 | `None` (not in the table) | `0x00` |  | 48 |
| `MB_BLUEPRINTS` | 0x93 |  | `None` (not in the table) | `0x00` | 25 |  |
| `MB_BOOKSHELF` | 0x81 | 0xE1 | `Bookshelf` | `0xE1` | 161 | 34 |
| `MB_BRIDGE_OVER_OCEAN` |  | 0x70 | `BridgeOverWater` | `0x73` |  | 609 |
| `MB_BRIDGE_OVER_POND_HIGH` |  | 0x73 | `BridgeOverWater` | `0x73` |  | 35 |
| `MB_BRIDGE_OVER_POND_HIGH_EDGE_1` |  | 0x7C | `BridgeOverWater` | `0x73` |  | 2 |
| `MB_BRIDGE_OVER_POND_HIGH_EDGE_2` |  | 0x7D | `BridgeOverWater` | `0x73` |  | 2 |
| `MB_BRIDGE_OVER_POND_LOW` |  | 0x71 | `BridgeOverWater` | `0x73` |  |  |
| `MB_BRIDGE_OVER_POND_MED` |  | 0x72 | `BridgeOverWater` | `0x73` |  | 17 |
| `MB_BRIDGE_OVER_POND_MED_EDGE_1` |  | 0x7A | `BridgeOverWater` | `0x73` |  | 2 |
| `MB_BRIDGE_OVER_POND_MED_EDGE_2` |  | 0x7B | `BridgeOverWater` | `0x73` |  | 2 |
| `MB_BUMPY_SLOPE` |  | 0xD1 | `BumpySlope` | `0xFB` |  | 17 |
| `MB_BURGLARY` | 0xA2 |  | `None` (not in the table) | `0x00` | 11 |  |
| `MB_CABINET` | 0x89 |  | `None` (not in the table) | `0x00` | 80 |  |
| `MB_CABLE_BOX_RESULTS_2` |  | 0xE7 | `None` (not in the table) | `0x00` |  | 17 |
| `MB_CABLE_CLUB_WIRELESS_MONITOR` | 0x8D |  | `None` (not in the table) | `0x00` | 19 |  |
| `MB_CAVE` | 0x08 | 0x08 | `CaveFloor` | `0x08` | 32,292 | 24,607 |
| `MB_CAVE_DOOR` | 0x60 |  | `EntranceNorth` | `0x64` | 93 |  |
| `MB_COMPUTER` | 0x97 |  | `Computer` | `0x83` | 52 |  |
| `MB_COUNTER` | 0x80 | 0x80 | `Counter` | `0x80` | 730 | 566 |
| `MB_CRACKED_FLOOR` |  | 0xD2 | `CrackedFloor` | `0xF1` |  | 81 |
| `MB_CRACKED_FLOOR_HOLE` |  | 0x66 | `Hole` | `0xF2` |  |  |
| `MB_CRACKED_ICE` | 0x27 | 0x27 | `ThinIce` | `0xF0` |  |  |
| `MB_CUP` | 0x9B |  | `None` (not in the table) | `0x00` | 22 |  |
| `MB_CYCLING_ROAD_PULL_DOWN` | 0xD0 |  | `CyclingRoadPull` | `0xFC` | 2,041 |  |
| `MB_CYCLING_ROAD_PULL_DOWN_GRASS` | 0xD1 |  | `CyclingRoadPull` | `0xFC` | 66 |  |
| `MB_CYCLING_ROAD_WATER` | 0x1B |  | `Sea` | `0x15` | 751 |  |
| `MB_DEEP_SAND` |  | 0x06 | `Sand` | `0x21` |  | 690 |
| `MB_DEEP_SOUTH_WARP` |  | 0x6E | `ExitSouth` | `0x6F` |  | 2 |
| `MB_DEEP_WATER` | 0x12 | 0x12 | `DiveWater` | `0xED` |  | 3,400 |
| `MB_DOWN_ESCALATOR` | 0x6B | 0x6B | `Escalator` | `0x6B` | 19 | 17 |
| `MB_DOWN_LEFT_STAIR_WARP` | 0x6F |  | `StairsWest` | `0x5F` | 41 |  |
| `MB_DOWN_RIGHT_STAIR_WARP` | 0x6E |  | `StairsEast` | `0x5E` | 82 |  |
| `MB_DRESSER` | 0x8B |  | `None` (not in the table) | `0x00` | 8 |  |
| `MB_EASTWARD_CURRENT` | 0x50 | 0x50 | `CurrentEast` | `0xF3` | 42 | 56 |
| `MB_EAST_ARROW_WARP` | 0x62 | 0x62 | `ExitEast` | `0x6C` | 34 | 9 |
| `MB_FALL_WARP` | 0x66 |  | `Hole` | `0xF2` | 31 |  |
| `MB_FAST_WATER` | 0x11 |  | `River` | `0x10` | 2,831 |  |
| `MB_FOOD` | 0x90 |  | `None` (not in the table) | `0x00` | 22 |  |
| `MB_FOOD_SMELLS_TASTY` | 0x99 |  | `None` (not in the table) | `0x00` | 13 |  |
| `MB_FOOTPRINTS` |  | 0x25 | `Sand` | `0x21` |  |  |
| `MB_FORTREE_BRIDGE` |  | 0x78 | `Bridge` | `0x71` |  | 27 |
| `MB_HOLDS_LARGE_DECORATION` |  | 0xC3 | `None` (not in the table) | `0x00` |  | 8 |
| `MB_HOLDS_SMALL_DECORATION` |  | 0xB5 | `None` (not in the table) | `0x00` |  | 34 |
| `MB_HORIZONTAL_RAIL` |  | 0xD6 | `RailEastWest` | `0xFA` |  | 45 |
| `MB_HOT_SPRINGS` | 0x28 | 0x28 | `ShallowWater` | `0x17` | 37 | 13 |
| `MB_ICE` | 0x23 | 0x20 | `Ice` | `0x20` | 189 | 340 |
| `MB_IMPASSABLE_EAST` | 0x30 | 0x30 | `BlockEast` | `0x30` | 67 | 27 |
| `MB_IMPASSABLE_NORTH` | 0x32 | 0x32 | `BlockNorth` | `0x32` | 2,125 | 2,601 |
| `MB_IMPASSABLE_NORTHEAST` | 0x34 | 0x34 | `BlockNorthAndEast` | `0x34` |  |  |
| `MB_IMPASSABLE_NORTHWEST` | 0x35 | 0x35 | `BlockNorthAndWest` | `0x35` |  |  |
| `MB_IMPASSABLE_SOUTH` | 0x33 | 0x33 | `BlockSouth` | `0x33` | 9 | 3 |
| `MB_IMPASSABLE_SOUTHEAST` | 0x36 | 0x36 | `BlockSouthAndEast` | `0x36` |  |  |
| `MB_IMPASSABLE_SOUTHWEST` | 0x37 | 0x37 | `BlockSouthAndWest` | `0x37` |  |  |
| `MB_IMPASSABLE_SOUTH_AND_NORTH` |  | 0xC0 | `BlockNorthAndSouth` | `0x49` |  | 3 |
| `MB_IMPASSABLE_WEST` | 0x31 | 0x31 | `BlockWest` | `0x31` | 25 | 24 |
| `MB_IMPASSABLE_WEST_AND_EAST` |  | 0xC1 | `BlockEastAndWest` | `0x4A` |  |  |
| `MB_INDIGO_PLATEAU_SIGN_1` | 0x91 |  | `None` (not in the table) | `0x00` | 22 |  |
| `MB_INDIGO_PLATEAU_SIGN_2` | 0x92 |  | `None` (not in the table) | `0x00` | 21 |  |
| `MB_INDOOR_ENCOUNTER` | 0x0B | 0x0B | `OldChateauFloor` | `0x0B` |  | 2,564 |
| `MB_INTERIOR_DEEP_WATER` |  | 0x11 | `DiveWater` | `0xED` |  | 274 |
| `MB_ISOLATED_HORIZONTAL_RAIL` |  | 0xD4 | `RailEastWest` | `0xFA` |  | 6 |
| `MB_ISOLATED_VERTICAL_RAIL` |  | 0xD3 | `RailNorthSouth` | `0xF9` |  | 3 |
| `MB_JUMP_EAST` | 0x38 | 0x38 | `LedgeEast` | `0x38` | 39 | 120 |
| `MB_JUMP_NORTH` | 0x3A | 0x3A | `LedgeNorth` | `0x3A` |  |  |
| `MB_JUMP_SOUTH` | 0x3B | 0x3B | `LedgeSouth` | `0x3B` | 958 | 657 |
| `MB_JUMP_SOUTHEAST` |  | 0x3E | `None` (not in the table) | `0x00` |  | 33 |
| `MB_JUMP_SOUTHWEST` |  | 0x3F | `None` (not in the table) | `0x00` |  | 44 |
| `MB_JUMP_WEST` | 0x39 | 0x39 | `LedgeWest` | `0x39` | 41 | 169 |
| `MB_KITCHEN` | 0x8A |  | `None` (not in the table) | `0x00` | 92 |  |
| `MB_LADDER` | 0x61 | 0x61 | `WarpPanel` | `0x67` | 142 | 102 |
| `MB_LAVARIDGE_1F_WARP` | 0x68 |  | `WarpPanel` | `0x67` |  |  |
| `MB_LAVARIDGE_GYM_1F_WARP` |  | 0x68 | `WarpPanel` | `0x67` |  | 20 |
| `MB_LAVARIDGE_GYM_B1F_WARP` |  | 0x29 | `WarpPanel` | `0x67` |  | 15 |
| `MB_LONG_GRASS` |  | 0x03 | `VeryTallGrass` | `0x03` |  | 1,381 |
| `MB_LONG_GRASS_SOUTH_EDGE` |  | 0x09 | `VeryTallGrass` | `0x03` |  | 335 |
| `MB_MOSSDEEP_GYM_WARP` |  | 0x0E | `WarpPanel` | `0x67` |  | 23 |
| `MB_MOUNTAIN_TOP` | 0x0C | 0x0C | `MountainFloor` | `0x0C` | 1,119 | 19,324 |
| `MB_MT_PYRE_HOLE` |  | 0x0F | `Hole` | `0xF2` |  | 8 |
| `MB_MUDDY_SLOPE` |  | 0xD0 | `BikeSlopeBottom` | `0xDA` |  | 12 |
| `MB_NEATLY_LINED_UP_TOOLS` | 0x9F |  | `None` (not in the table) | `0x00` | 75 |  |
| `MB_NON_ANIMATED_DOOR` |  | 0x60 | `EntranceNorth` | `0x64` |  | 286 |
| `MB_NORMAL` | 0x00 | 0x00 | `None` | `0x00` | 142,329 | 187,477 |
| `MB_NORTHWARD_CURRENT` | 0x52 | 0x52 | `CurrentNorth` | `0xF5` | 142 | 319 |
| `MB_NORTH_ARROW_WARP` | 0x64 | 0x64 | `ExitNorth` | `0x6E` | 16 | 6 |
| `MB_NO_RUNNING` |  | 0x0A | `NoRunning` | `0xFD` |  | 142 |
| `MB_NO_SURFACING` |  | 0x19 | `NoSurfacing` | `0xEE` |  | 1,182 |
| `MB_OCEAN_WATER` | 0x15 | 0x15 | `Sea` | `0x15` | 34,484 | 46,367 |
| `MB_PACIFIDLOG_HORIZONTAL_LOG_LEFT` |  | 0x76 | `BridgeOverWater` | `0x73` |  | 9 |
| `MB_PACIFIDLOG_HORIZONTAL_LOG_RIGHT` |  | 0x77 | `BridgeOverWater` | `0x73` |  | 9 |
| `MB_PACIFIDLOG_VERTICAL_LOG_BOTTOM` |  | 0x75 | `BridgeOverWater` | `0x73` |  | 10 |
| `MB_PACIFIDLOG_VERTICAL_LOG_TOP` |  | 0x74 | `BridgeOverWater` | `0x73` |  | 10 |
| `MB_PAINTING` | 0x94 |  | `None` (not in the table) | `0x00` | 99 |  |
| `MB_PC` | 0x83 | 0x83 | `Computer` | `0x83` | 45 | 51 |
| `MB_PETALBURG_GYM_DOOR` |  | 0x8D | `Door` | `0x69` |  |  |
| `MB_PICTURE_BOOK_SHELF` |  | 0xE0 | `SmallBookshelf` | `0xE0` |  | 21 |
| `MB_PLAYER_ROOM_PC_ON` |  | 0xC5 | `Computer` | `0x83` |  |  |
| `MB_POKEBLOCK_FEEDER` |  | 0x87 | `None` (not in the table) | `0x00` |  | 11 |
| `MB_POKEMART_SHELF` | 0x82 |  | `ShopShelf` | `0xE5` | 240 |  |
| `MB_POKEMART_SIGN` | 0x88 |  | `None` (not in the table) | `0x00` | 26 |  |
| `MB_POKEMON_CENTER_BOOKSHELF` |  | 0xE2 | `Bookshelf` | `0xE1` |  | 30 |
| `MB_POKEMON_CENTER_SIGN` | 0x87 |  | `None` (not in the table) | `0x00` | 36 |  |
| `MB_POND_WATER` | 0x10 | 0x10 | `River` | `0x10` | 635 | 2,031 |
| `MB_PORTHOLE` | 0x9C |  | `None` (not in the table) | `0x00` | 140 |  |
| `MB_POWER_PLANT_MACHINE` | 0x95 |  | `None` (not in the table) | `0x00` | 40 |  |
| `MB_PUDDLE` | 0x16 | 0x16 | `Puddle` | `0x16` | 116 | 646 |
| `MB_QUESTIONNAIRE` | 0x8F | 0x8F | `None` (not in the table) | `0x00` | 12 | 13 |
| `MB_REFLECTION_UNDER_BRIDGE` |  | 0x2B | `River` | `0x10` |  | 38 |
| `MB_REGION_MAP` | 0x85 | 0x85 | `TownMap` | `0x85` | 80 | 36 |
| `MB_REGULAR_WARP` | 0x67 |  | `WarpPanel` | `0x67` | 64 |  |
| `MB_ROCK_STAIRS` | 0x2A |  | `None` (not in the table) | `0x00` | 358 |  |
| `MB_ROULETTE` |  | 0x8A | `None` (not in the table) | `0x00` |  | 12 |
| `MB_RUNNING_DISALLOWED` | 0x0A |  | `NoRunning` | `0xFD` |  |  |
| `MB_SAND` | 0x21 | 0x21 | `Sand` | `0x21` | 1,505 | 2,273 |
| `MB_SAND_CAVE` | 0x2B |  | `CaveFloor` | `0x08` | 516 |  |
| `MB_SEAWEED` | 0x22 | 0x22 | `Seaweed` | `0xEF` |  | 587 |
| `MB_SEAWEED_NO_SURFACING` |  | 0x2A | `SeaweedNoSurfacing` | `0xFE` |  | 454 |
| `MB_SECRET_BASE_HOLE` |  | 0xC2 | `None` (not in the table) | `0x00` |  | 14 |
| `MB_SECRET_BASE_NORTH_WALL` |  | 0xB7 | `None` (not in the table) | `0x00` |  | 260 |
| `MB_SECRET_BASE_PC` |  | 0xB0 | `Computer` | `0x83` |  | 24 |
| `MB_SECRET_BASE_REGISTER_PC` |  | 0xB1 | `Computer` | `0x83` |  |  |
| `MB_SECRET_BASE_SCENERY` |  | 0xB2 | `None` (not in the table) | `0x00` |  | 16 |
| `MB_SECRET_BASE_SPOT_BLUE_CAVE` |  | 0x9A | `SecretBaseSpot` | `0xF8` |  | 10 |
| `MB_SECRET_BASE_SPOT_BLUE_CAVE_OPEN` |  | 0x9B | `SecretBaseSpot` | `0xF8` |  |  |
| `MB_SECRET_BASE_SPOT_BROWN_CAVE` |  | 0x92 | `SecretBaseSpot` | `0xF8` |  | 12 |
| `MB_SECRET_BASE_SPOT_BROWN_CAVE_OPEN` |  | 0x93 | `SecretBaseSpot` | `0xF8` |  |  |
| `MB_SECRET_BASE_SPOT_RED_CAVE` |  | 0x90 | `SecretBaseSpot` | `0xF8` |  | 15 |
| `MB_SECRET_BASE_SPOT_RED_CAVE_OPEN` |  | 0x91 | `SecretBaseSpot` | `0xF8` |  |  |
| `MB_SECRET_BASE_SPOT_SHRUB` |  | 0x98 | `SecretBaseSpot` | `0xF8` |  | 13 |
| `MB_SECRET_BASE_SPOT_SHRUB_OPEN` |  | 0x99 | `SecretBaseSpot` | `0xF8` |  |  |
| `MB_SECRET_BASE_SPOT_TREE_LEFT` |  | 0x96 | `SecretBaseSpot` | `0xF8` |  | 7 |
| `MB_SECRET_BASE_SPOT_TREE_LEFT_OPEN` |  | 0x97 | `SecretBaseSpot` | `0xF8` |  |  |
| `MB_SECRET_BASE_SPOT_TREE_RIGHT` |  | 0x9C | `SecretBaseSpot` | `0xF8` |  | 7 |
| `MB_SECRET_BASE_SPOT_TREE_RIGHT_OPEN` |  | 0x9D | `SecretBaseSpot` | `0xF8` |  |  |
| `MB_SECRET_BASE_SPOT_YELLOW_CAVE` |  | 0x94 | `SecretBaseSpot` | `0xF8` |  | 11 |
| `MB_SECRET_BASE_SPOT_YELLOW_CAVE_OPEN` |  | 0x95 | `SecretBaseSpot` | `0xF8` |  |  |
| `MB_SECRET_BASE_TRAINER_SPOT` |  | 0xB3 | `None` (not in the table) | `0x00` |  | 24 |
| `MB_SECRET_BASE_WALL` |  | 0x01 | `None` (not in the table) | `0x00` |  | 1,663 |
| `MB_SHALLOW_WATER` | 0x17 | 0x17 | `ShallowWater` | `0x17` | 775 | 3,683 |
| `MB_SHOAL_CAVE_ENTRANCE` |  | 0x1C | `None` (not in the table) | `0x00` |  | 2 |
| `MB_SHOP_SHELF` |  | 0xE5 | `ShopShelf` | `0xE5` |  | 269 |
| `MB_SHORT_GRASS` |  | 0x07 | `None` | `0x00` |  | 198 |
| `MB_SIGNPOST` | 0x84 |  | `None` (not in the table) | `0x00` | 189 |  |
| `MB_SKY_PILLAR_CLOSED_DOOR` |  | 0xEA | `None` (not in the table) | `0x00` |  | 1 |
| `MB_SLIDE_EAST` | 0x44 | 0x44 | `SlideEast` | `0x40` |  |  |
| `MB_SLIDE_NORTH` | 0x46 | 0x46 | `SlideNorth` | `0x42` |  |  |
| `MB_SLIDE_SOUTH` | 0x47 | 0x47 | `SlideSouth` | `0x43` |  | 12 |
| `MB_SLIDE_WEST` | 0x45 | 0x45 | `SlideWest` | `0x41` |  |  |
| `MB_SLOT_MACHINE` |  | 0x89 | `None` (not in the table) | `0x00` |  | 20 |
| `MB_SOOTOPOLIS_DEEP_WATER` |  | 0x14 | `DiveWater` | `0xED` |  | 19 |
| `MB_SOUTHWARD_CURRENT` | 0x53 | 0x53 | `CurrentSouth` | `0xF6` | 45 | 165 |
| `MB_SOUTH_ARROW_WARP` | 0x65 | 0x65 | `ExitSouth` | `0x6F` | 282 | 651 |
| `MB_SPIN_DOWN` | 0x57 |  | `SlideSouth` | `0x43` | 52 |  |
| `MB_SPIN_LEFT` | 0x55 |  | `SlideWest` | `0x41` | 45 |  |
| `MB_SPIN_RIGHT` | 0x54 |  | `SlideEast` | `0x40` | 38 |  |
| `MB_SPIN_UP` | 0x56 |  | `SlideNorth` | `0x42` | 51 |  |
| `MB_STAIRS_OUTSIDE_ABANDONED_SHIP` |  | 0x1B | `None` (not in the table) | `0x00` |  | 1 |
| `MB_STOP_SPINNING` | 0x58 |  | `Spinner` | `0xF7` | 49 |  |
| `MB_STRENGTH_BUTTON` | 0x20 |  | `None` (not in the table) | `0x00` | 15 |  |
| `MB_TALL_GRASS` | 0x02 | 0x02 | `TallGrass` | `0x02` | 5,282 | 2,801 |
| `MB_TELEPHONE` | 0x96 |  | `None` (not in the table) | `0x00` | 39 |  |
| `MB_TELEVISION` | 0x86 | 0x86 | `Television` | `0x86` | 27 | 43 |
| `MB_THIN_ICE` | 0x26 | 0x26 | `ThinIce` | `0xF0` | 9 | 64 |
| `MB_TRAINER_HILL_TIMER` |  | 0xE9 | `None` (not in the table) | `0x00` |  | 4 |
| `MB_TRAINER_TOWER_MONITOR` | 0xA3 |  | `None` (not in the table) | `0x00` | 8 |  |
| `MB_TRASH_BIN` | 0x9A |  | `TrashCan` | `0xE4` | 7 |  |
| `MB_TRASH_CAN` |  | 0xE4 | `TrashCan` | `0xE4` |  | 102 |
| `MB_TRICK_HOUSE_PUZZLE_8_FLOOR` | 0x48 | 0x48 | `None` (not in the table) | `0x00` |  | 186 |
| `MB_TRICK_HOUSE_PUZZLE_DOOR` |  | 0x8C | `None` (not in the table) | `0x00` |  | 8 |
| `MB_UNDERWATER_BLOCKED_ABOVE` | 0x19 |  | `NoSurfacing` | `0xEE` |  |  |
| `MB_UNION_ROOM_WARP` | 0x71 |  | `WarpPanel` | `0x67` | 1 |  |
| `MB_UNUSED_BRIDGE` |  | 0x7E | `Bridge` | `0x71` |  |  |
| `MB_UNUSED_SOOTOPOLIS_DEEP_WATER` |  | 0x18 | `DiveWater` | `0xED` |  |  |
| `MB_UNUSED_SOOTOPOLIS_DEEP_WATER_2` |  | 0x1A | `DiveWater` | `0xED` |  |  |
| `MB_UNUSED_WATER` | 0x1A |  | `Sea` | `0x15` |  |  |
| `MB_UP_ESCALATOR` | 0x6A | 0x6A | `Escalator` | `0x6B` | 19 | 17 |
| `MB_UP_LEFT_STAIR_WARP` | 0x6D |  | `StairsWest` | `0x5F` | 26 |  |
| `MB_UP_RIGHT_STAIR_WARP` | 0x6C |  | `StairsEast` | `0x5E` | 37 |  |
| `MB_VASE` |  | 0xE3 | `None` (not in the table) | `0x00` |  | 29 |
| `MB_VERTICAL_RAIL` |  | 0xD5 | `RailNorthSouth` | `0xF9` |  | 14 |
| `MB_WALK_EAST` | 0x40 | 0x40 | `SlideEast` | `0x40` |  |  |
| `MB_WALK_NORTH` | 0x42 | 0x42 | `SlideNorth` | `0x42` |  |  |
| `MB_WALK_SOUTH` | 0x43 | 0x43 | `SlideSouth` | `0x43` |  |  |
| `MB_WALK_WEST` | 0x41 | 0x41 | `SlideWest` | `0x41` |  |  |
| `MB_WARP_DOOR` | 0x69 |  | `Door` | `0x69` | 205 |  |
| `MB_WATERFALL` | 0x13 | 0x13 | `Waterfall` | `0x13` | 80 | 277 |
| `MB_WATER_DOOR` |  | 0x6C | `Door` | `0x69` |  | 6 |
| `MB_WATER_SOUTH_ARROW_WARP` |  | 0x6D | `ExitSouth` | `0x6F` |  | 9 |
| `MB_WESTWARD_CURRENT` | 0x51 | 0x51 | `CurrentWest` | `0xF4` |  | 3,844 |
| `MB_WEST_ARROW_WARP` | 0x63 | 0x63 | `ExitWest` | `0x6D` | 32 | 1 |
| `MB_WINDOW` | 0x9D |  | `None` (not in the table) | `0x00` | 82 |  |
| `MB_WIRELESS_BOX_RESULTS` |  | 0xE8 | `None` (not in the table) | `0x00` |  | 17 |

95 more names are defined by one game or the other and used on neither's maps (`MB_UNUSED_*` among them); they become `None`.

The values of the GBA's own, and what the game does with each today:

| Value | Name | What it is | In the game |
|---|---|---|---|
| `0x32` | `BlockNorth` | Open ground whose north side is closed. | **Rule.** No step across its closed side, either way. |
| `0x33` | `BlockSouth` | Open ground whose south side is closed. | **Rule.** No step across its closed side, either way. |
| `0x34` | `BlockNorthAndEast` | Open ground closed on its north and east sides. | **Rule.** No step across its closed side, either way. |
| `0x35` | `BlockNorthAndWest` | Open ground closed on its north and west sides. | **Rule.** No step across its closed side, either way. |
| `0x36` | `BlockSouthAndEast` | Open ground closed on its south and east sides. | **Rule.** No step across its closed side, either way. |
| `0x37` | `BlockSouthAndWest` | Open ground closed on its south and west sides. | **Rule.** No step across its closed side, either way. |
| `0x3A` | `LedgeNorth` | A ledge hopped over northward (Platinum's own value, which only the GBA's maps use). | **Rule.** Hopped the way it faces, two tiles on; a wall from every other side. |
| `0xED` | `DiveWater` | Deep, dark water: surfed, and dived into where the sea floor below is a map (the GBA's). | **Rule.** Stops a walker; surfed with Surf, meeting the area's water Pokémon; where the area has a sea floor, a surfer who knows Dive goes down to the same tile of it (FieldMovement.CanDive; plan 21 · H5). |
| `0xEE` | `NoSurfacing` | The sea floor under rock or a ledge above: swum along after a dive, and nobody comes up there (the GBA's). | **Rule.** The sea floor under rock or a ledge above: swum along after a dive (the seaweed meeting the area's water Pokémon), and Dive never comes up from it (FieldMovement.CanSurface; plan 21 · H5). |
| `0xEF` | `Seaweed` | Seaweed on the sea floor, where wild Pokémon live (the GBA's). | **Rule.** The sea floor's grass, swum through after a dive at walking pace, meeting the area's water Pokémon; Dive comes up from it to the same tile of the sea above (FieldMovement.CanSurface; plan 21 · H5). |
| `0xF0` | `ThinIce` | Thin ice that cracks under a step and gives way under the next (the GBA's). | **Rule.** In the Sootopolis Gym (its puzzle, SootopolisIce; plan 21 · H8, part c) it cracks under a first step and gives way under a second, dropping the walker to the floor below, and a field crossed tile by tile clears the glazed stairs above it; walked like plain ground anywhere else. |
| `0xF1` | `CrackedFloor` | A cracked floor that gives way after a step, or the hole it leaves (the GBA's). | **Rule.** Gives way under a walker where its area names the floor below, dropping them onto the same tile there (Map.HoleTargetAt, Emerald's setholewarp: Sky Pillar; plan 21 · H8, part c); ridden over on the Bicycle, and walked like plain ground anywhere else. |
| `0xF2` | `Hole` | A hole that drops whoever steps in to the floor below (the GBA's). | **Rule.** A warp down to the floor below, taken by stepping on it; whoever lands on water there rides out on it (Player.ComeDownOn), and a boulder pushed in is a pit's (Map.BoulderPits; plan 19 · K6, part b). |
| `0xF3` | `CurrentEast` | Water that carries a surfer east (the GBA's). | **Rule.** Stops a walker and is surfed; it carries the surfer its own way, step after step, until calm water or something in the way (FieldMovement.Carries; plan 19 · K6, part b). |
| `0xF4` | `CurrentWest` | Water that carries a surfer west (the GBA's). | **Rule.** Stops a walker and is surfed; it carries the surfer its own way, step after step, until calm water or something in the way (FieldMovement.Carries; plan 19 · K6, part b). |
| `0xF5` | `CurrentNorth` | Water that carries a surfer north (the GBA's). | **Rule.** Stops a walker and is surfed; it carries the surfer its own way, step after step, until calm water or something in the way (FieldMovement.Carries; plan 19 · K6, part b). |
| `0xF6` | `CurrentSouth` | Water that carries a surfer south (the GBA's). | **Rule.** Stops a walker and is surfed; it carries the surfer its own way, step after step, until calm water or something in the way (FieldMovement.Carries; plan 19 · K6, part b). |
| `0xF7` | `Spinner` | The tile a GBA spin stops on: a spin begun on an arrow (a slide) goes on over plain floor until one. | **Rule.** Ends a GBA spin: one begun on an arrow goes on over plain floor until a stop (FieldMovement.Spins). |
| `0xF8` | `SecretBaseSpot` | A hollow, a bush or a tree where a secret base can be made (the GBA's). | **Rule.** Stands in the way; faced, where the area's file names its base (Map.SecretBaseSpots), Secret Power makes the player's secret base there and goes in, and the player's own base is gone into (common.SecretBaseSpot; plan 21 · H10). |
| `0xF9` | `RailNorthSouth` | A rail running north to south, hopped along on the Acro Bike (the GBA's). | **Rule.** Ridden only on the Acro Bike, hopping along its length and never across (FieldMovement.RailRunsNorthSouth; plan 21 · H3); never walked. |
| `0xFA` | `RailEastWest` | A rail running east to west, hopped along on the Acro Bike (the GBA's). | **Rule.** Ridden only on the Acro Bike, hopping along its length and never across (FieldMovement.RailRunsNorthSouth; plan 21 · H3); never walked. |
| `0xFB` | `BumpySlope` | A bumpy slope only the Acro Bike's hops climb (the GBA's). | **Rule.** Hopped up only on the Acro Bike (plan 21 · H3); anyone goes down it. |
| `0xFC` | `CyclingRoadPull` | The Cycling Road's slope, which pulls a rider south (the GBA's). | **Rule.** A rider who steers nowhere is pulled south a step at a time (FieldMovement.Pulls; plan 19 · K6); someone on foot stands. |
| `0xFD` | `NoRunning` | Ground where nobody may run (the GBA's). | *Not yet.* A Game Boy Advance behaviour whose rule comes with the region plan that opens a place using it (19 · Kanto, 21 · Hoenn); walked like plain ground until then. |
| `0xFE` | `SeaweedNoSurfacing` | Seaweed on the sea floor under rock above: wild Pokémon live in it, and nobody comes up there (the GBA's). | **Rule.** The sea floor under rock or a ledge above: swum along after a dive (the seaweed meeting the area's water Pokémon), and Dive never comes up from it (FieldMovement.CanSurface; plan 21 · H5). |

## Black 2's behaviours

*Written by `tools/MapImporter --game black2` (plan 22 · U1); the Sinnoh run and the GBA's keep it as it is.*

A tile of a Black 2 chunk carries a 32-bit type: its low sixteen bits are its class, which the field code asks about class by class (`src/ov036/map_attr.c`), and the lowest of its high sixteen blocks the tile. The reader (`tools/MapImporter/Bw2/Bw2Behaviours.cs`) turns each class the code names into Platinum's value where Platinum has one that means the same, the GBA's where the GBA has one, and a value of Black 2's own from `0xB0` where neither has; a class the code asks about only for its battle background, or not at all, becomes `None` and waits for the session that opens a place using it. Height is separate: each tile names a plane (a slope and an offset in the original's two tables, or two planes for a tile cut along a diagonal), and the reader takes its height at the tile's middle.

| Class | What | Becomes | Value | Tiles | Blocked |
|---|---|---|---|---:|---:|
| `0x00` | Plain ground. | `None` | `0x00` | 145,303 | 1,312 |
| `0x01` | A wall: plain, always blocked. | `None` | `0x00` | 823,825 | 822,858 |
| `0x02` | Not understood yet. | `None` (not in the table) | `0x00` | 1,243 | 0 |
| `0x03` | Not understood yet. | `None` (not in the table) | `0x00` | 10,978 | 0 |
| `0x04` | Tall grass. | `TallGrass` | `0x02` | 7,932 | 0 |
| `0x05` | Tall grass of a second look. | `TallGrass` | `0x02` | 113 | 0 |
| `0x06` | Dark grass: two wild Pokémon at a time. | `DarkGrass` | `0xB0` | 3,791 | 0 |
| `0x07` | Dark grass of a second look. | `DarkGrass` | `0xB0` | 81 | 0 |
| `0x08` | Very tall grass. | `VeryTallGrass` | `0x03` | 919 | 0 |
| `0x09` | Very tall dark grass. | `VeryTallDarkGrass` | `0xB1` | 149 | 0 |
| `0x0A` | A cave's floor. | `CaveFloor` | `0x08` | 24,925 | 0 |
| `0x0B` | Sand. | `Sand` | `0x21` | 3,518 | 0 |
| `0x0C` | Deep sand. | `DeepSand` | `0xB3` | 2,239 | 0 |
| `0x0D` | Not understood yet. | `None` (not in the table) | `0x00` | 75 | 0 |
| `0x0E` | Snow. | `ShadedSnow` | `0xA9` | 3,174 | 0 |
| `0x0F` | Snow no Bicycle crosses. | `ShallowSnow` | `0xA8` | 391 | 0 |
| `0x10` | Ice that is slid on. | `Ice` | `0x20` | 111 | 0 |
| `0x11` | Not understood yet. | `None` (not in the table) | `0x00` | 2 | 0 |
| `0x12` | Not understood yet. | `None` (not in the table) | `0x00` | 327 | 327 |
| `0x13` | Not understood yet. | `None` (not in the table) | `0x00` | 53 | 0 |
| `0x14` | Not understood yet. | `None` (not in the table) | `0x00` | 521 | 0 |
| `0x15` | Not understood yet. | `None` (not in the table) | `0x00` | 181 | 2 |
| `0x16` | Not understood yet. | `None` (not in the table) | `0x00` | 10,419 | 0 |
| `0x17` | Not understood yet. | `None` (not in the table) | `0x00` | 1,424 | 0 |
| `0x18` | Ice that is slid on, of a second kind. | `Ice` | `0x20` | 4,268 | 0 |
| `0x19` | A beach's sand. | `Sand` | `0x21` | 3,182 | 0 |
| `0x1B` | Not understood yet. | `None` (not in the table) | `0x00` | 1,056 | 9 |
| `0x1C` | A swamp. | `MarshGrass` | `0xA6` | 1,295 | 0 |
| `0x1D` | A hole for a boulder pushed with Strength. | `StrengthHole` | `0xB5` | 297 | 297 |
| `0x1E` | Not understood yet. | `None` (not in the table) | `0x00` | 242 | 0 |
| `0x1F` | Not understood yet. | `None` (not in the table) | `0x00` | 17,114 | 0 |
| `0x20` | A bridge's deck, with birds' shadows crossing it. | `Bridge` | `0x71` | 2,100 | 0 |
| `0x21` | Tall grass of a third look. | `TallGrass` | `0x02` | 22 | 0 |
| `0x22` | Dark grass of a third look. | `DarkGrass` | `0xB0` | 36 | 0 |
| `0x23` | Sand of a second look. | `Sand` | `0x21` | 2,133 | 0 |
| `0x24` | The grass before a Hidden Grotto. | `GrottoGrass` | `0xB2` | 10 | 0 |
| `0x25` | Not understood yet. | `None` (not in the table) | `0x00` | 616 | 0 |
| `0x30` | The charged floor of Chargestone Cave. | `ChargedFloor` | `0xB6` | 235 | 0 |
| `0x32` | Not understood yet. | `None` (not in the table) | `0x00` | 207 | 207 |
| `0x33` | Not understood yet. | `None` (not in the table) | `0x00` | 121 | 0 |
| `0x3D` | Water that is surfed. | `River` | `0x10` | 4,764 | 0 |
| `0x3F` | The sea. | `Sea` | `0x15` | 18,089 | 0 |
| `0x40` | A waterfall. | `Waterfall` | `0x13` | 125 | 125 |
| `0x41` | The water's edge, where Surf begins. | `SurfEdge` | `0xB7` | 4,368 | 4,368 |
| `0x43` | The sea, of a third kind. | `Sea` | `0x15` | 36 | 0 |
| `0x44` | The water's edge, of a second kind. | `SurfEdge` | `0xB7` | 1,183 | 1,183 |
| `0x51` | No way out east. | `BlockEast` | `0x30` | 53 | 0 |
| `0x52` | No way out west. | `BlockWest` | `0x31` | 50 | 0 |
| `0x53` | No way out north. | `BlockNorth` | `0x32` | 50 | 0 |
| `0x54` | No way out south. | `BlockSouth` | `0x33` | 35 | 0 |
| `0x72` | A ledge jumped east. | `LedgeEast` | `0x38` | 191 | 191 |
| `0x73` | A ledge jumped west. | `LedgeWest` | `0x39` | 109 | 109 |
| `0x74` | A ledge jumped north. | `LedgeNorth` | `0x3A` | 68 | 68 |
| `0x75` | A ledge jumped south. | `LedgeSouth` | `0x3B` | 249 | 249 |
| `0x7C` | Quicksand. | `Quicksand` | `0xB4` | 223 | 0 |
| `0x94` | A current that carries a surfer east. | `CurrentEast` | `0xF3` | 363 | 0 |
| `0x95` | A current that carries a surfer west. | `CurrentWest` | `0xF4` | 161 | 0 |
| `0x96` | A current that carries a surfer north. | `CurrentNorth` | `0xF5` | 60 | 0 |
| `0x97` | A current that carries a surfer south. | `CurrentSouth` | `0xF6` | 169 | 0 |
| `0x9C` | Not understood yet. | `None` (not in the table) | `0x00` | 6,844 | 0 |
| `0xA3` | Not understood yet. | `None` (not in the table) | `0x00` | 83 | 0 |
| `0xBE` | A catwalk. | `Catwalk` | `0xB8` | 261 | 0 |
| `0xBF` | A catwalk's end, where it is stepped onto. | `Catwalk` | `0xB8` | 72 | 0 |
| `0xD4` | A reception counter. | `Counter` | `0x80` | 2,358 | 2,358 |
| `0xD6` | A PC. | `Computer` | `0x83` | 19 | 19 |
| `0xD8` | A television. | `Television` | `0x86` | 77 | 77 |
| `0xD9` | A bookcase. | `Bookshelf` | `0xE1` | 47 | 47 |
| `0xDA` | A bookcase of a second kind. | `Bookshelf` | `0xE1` | 38 | 38 |
| `0xDB` | A bookcase of a third kind. | `Bookshelf` | `0xE1` | 49 | 49 |
| `0xDC` | A bookcase of a fourth kind. | `Bookshelf` | `0xE1` | 15 | 15 |
| `0xDE` | A trash can of a second kind. | `TrashCan` | `0xE4` | 136 | 136 |
| `0xDF` | A shelf of goods. | `ShopShelf` | `0xE5` | 8 | 8 |
| `0xE0` | A shelf of goods of a second kind. | `ShopShelf` | `0xE5` | 14 | 14 |
| `0xE1` | A shelf of goods of a third kind. | `ShopShelf` | `0xE5` | 46 | 46 |
| `0xE2` | A vending machine: an event's business. | `None` | `0x00` | 80 | 80 |
| `0xFE` | Plain, blocked. | `None` | `0x00` | 3,029 | 3,029 |
| `0xFFFF` | No class: blocked. | `None` | `0x00` | 58 | 58 |

The values of Black 2's own, and what the game does with each today:

| Value | Name | What it is | In the game |
|---|---|---|---|
| `0xB0` | `DarkGrass` | Dark grass, where wild Pokémon may come two at a time (Black 2's). | Walked on; wild Pokémon from the area's dark grass table, now and then two at once (plan 22 · U4). |
| `0xB1` | `VeryTallDarkGrass` | Grass taller than the walker where wild Pokémon may come two at a time; no Bicycles (Black 2's). | **Rule.** Wild Pokémon from the area's dark grass table, more often than in dark grass and now and then two at once; no Bicycles (plan 22 · U4). |
| `0xB2` | `GrottoGrass` | The grass before a Hidden Grotto's way in; no Bicycles (Black 2's). | *Not yet.* A Black 2 behaviour whose rule comes with the session of plan 22 that opens a place using it; walked like plain ground until then. |
| `0xB3` | `DeepSand` | Deep sand (Black 2's). | *Not yet.* A Black 2 behaviour whose rule comes with the session of plan 22 that opens a place using it; walked like plain ground until then. |
| `0xB4` | `Quicksand` | Quicksand, which pulls whoever stands in it down (Black 2's). | *Not yet.* A Black 2 behaviour whose rule comes with the session of plan 22 that opens a place using it; walked like plain ground until then. |
| `0xB5` | `StrengthHole` | A hole a boulder pushed with Strength fills (Black 2's). | *Not yet.* A Black 2 behaviour whose rule comes with the session of plan 22 that opens a place using it; walked like plain ground until then. |
| `0xB6` | `ChargedFloor` | The charged floor of an electric cave (Black 2's). | *Not yet.* A Black 2 behaviour whose rule comes with the session of plan 22 that opens a place using it; walked like plain ground until then. |
| `0xB7` | `SurfEdge` | The edge of the water, where Surf may begin though a walker can't step (Black 2's). | *Not yet.* A Black 2 behaviour whose rule comes with the session of plan 22 that opens a place using it; walked like plain ground until then. |
| `0xB8` | `Catwalk` | A narrow catwalk, walked and never cycled (Black 2's). | *Not yet.* A Black 2 behaviour whose rule comes with the session of plan 22 that opens a place using it; walked like plain ground until then. |
