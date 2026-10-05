# Style guide: Sinnoh Diorama

The art direction chosen in plan 04 · G1 (2026-10-01). Every graphics session follows these rules; when a rule has to change, change it here first.

## The direction in one paragraph

Three layers, each with one job:

| Layer | Style | Code |
|---|---|---|
| **Overworld** | HD-2D: pixel-art ground, buildings and characters inside a lit 3D diorama, with strong tilt-shift depth of field, bloom and warm grading | `WorldRenderer`, `PixelGround`, `CharacterSprites` |
| **Battles** | Full 3D: smooth cel-shaded Pokémon and trainers on a modelled stage with soft foliage, a painted sky and clean filtered textures | `BattleRenderer`, `SoftFoliage`, `SkyPainter` |
| **Interface** | Crisp vector UI (Nunito, anti-aliased panels) in Platinum's colour language; Pokémon appear as **2D pixel sprites**, as in the main games | `ModernUi`, `UI/Kit` |

The layers are allowed to differ in rendering technique, never in palette, light direction or mood. What ties them together: the same greens, sands and sky blues; light always from the upper left; the same time of day in the field and in battle; the same interface on top of both; and (G9) a battle intro transition that carries the player from the diorama into the 3D stage.

The numbers in this guide live in `ArtLook` as one light rig per layer and time of day.

## Reference frames

`dotnet run --project tools/ShotHarness -- <dir> look [before dir]` renders the reference frames by day (Twinleaf, dialogue, battle, move menu, party, Route 201, Player's house, Lake Verity, options). Given the folder of an earlier run, it also writes `compare_*.png` before/after boards. `times` renders Twinleaf, Sandgem and a battle at each time of day, then each quality preset; `terrain` renders water, grass, trees and ground kinds; `buildings` renders every building from the street by day and after dark, and every room. Graphics sessions start with a `look` run (the "before") and end with the boards.

## Rules for every layer

- **No single-pixel noise.** Texture detail comes from placed shapes and clusters, never from per-texel random values. The field ground is tested: under 1.5 % of its texels may differ from all eight neighbours (`StyleGuideTests`). The old ground was about 30 %.
- **Light from the upper left**, sun slightly toward the camera, so faces and fronts are lit and shadows fall to the upper right.
- **Shade with colour, not black.** Shadows lean violet-blue, highlights lean warm; outlines are a darker shade of the surface they surround.
- **One pixel size per layer.** Pixel art is shown at one texel density and whole-number scales only, with point filtering; smooth art is mipmapped and filtered. Never mix the two inside one layer.
- **Readable at a glance.** Silhouettes before detail: if a detail doesn't read from the game camera, leave it out.

## Overworld (HD-2D)

### Pixel grid

- **32 texels per world unit** (one tile) for everything: ground (`GroundBaker.ArtTile`), buildings and props (each face painted at its exact size into the map's `ArtSheet`), character sprites (`CharacterSprites.TexelsPerUnit`). With the field camera that is about 3 layout units per texel, 6 real pixels at 4K.
- Point filtering everywhere; no mipmaps on pixel art.
- Upright things (walls, sprites, signs) are stretched by `VS = 1 / cos(pitch)` so they read at true proportions from Platinum's steep camera (pitch 59.05°, FOV 16.18°).
- **Upright things stand upright on screen.** Under that camera, perspective would make everything tall lean outward, by up to 20° at the sides of the screen, and shear its pixel art. Outdoors the vertex shader takes the lean out (`ArtLook.FieldUpright`, `FieldShaders.SetUpright`): every vertex is drawn straight above the point on the ground beneath it. A wall is then an undistorted rectangle, a sprite keeps square texels, and the ground keeps its perspective. Rooms keep true perspective, because their side walls are what makes them a doll's house.

### Ground

Baked per map by `PixelGround`: soft tile masks thresholded into hard pixel edges, then placed details.

| Element | Colour | Notes |
|---|---|---|
| Lawn | `104,190,98` | flat base |
| Lawn patches | `120,200,102` | clean-edged blobs a few tiles wide, one step lighter |
| Tuft marks | `70,150,82`, tip `160,222,122` | three-blade "v", 2–3 per tile on a jittered grid |
| Forest floor | `58,126,82` | under the tree margin |
| Tall-grass ground | `48,124,70` | |
| Path | `222,204,160` | rounded corners, never pixel fringes |
| Path rim / inner light | `166,140,104` / `236,222,186` | one texel each; the light line on the north and west sides |
| Pebbles | `168,160,150`, light `208,202,192`, shadow `214,186,132` | 2×2 with a highlight texel, at most one cluster per tile |
| Pond stones / sea sand | `168,160,150` / `238,224,172` | band around water |
| Contact shade by walls | two flat steps, 14 % and 28 % toward `30,70,50` | never a smooth gradient |

Other ground kinds follow the lawn's recipe (flat base, clean-edged patches one step lighter, a one-texel rim where they meet another ground, placed marks of two texels or more):

| Kind | Base / patches / rim | Marks |
|---|---|---|
| Sand | `238,224,172` / `246,236,196` / `206,188,138` | short ripple arcs `214,196,140` |
| Dirt | `176,136,96` / `194,156,112` / `138,102,70` | clods: 2×2 `146,108,74` with a light texel |
| Snow | `186,204,232` / `196,212,238` / `150,172,214` | drift lines `168,188,224`, three texels long (kept well below white, so the patches of it in Twinleaf Town stay snow under the day light instead of blooming to a blank; the snow routes still need a light rig of their own) |
| Cave floor | `112,100,104` / `130,118,120` / `74,66,80` | cracks `82,72,84`, pebbles `150,140,140` |
| Rock | `158,152,150` / `176,170,166` / `112,106,116` | cracks: bent lines `104,100,112` three to five texels long, each with a light chip `208,204,200` beside it |
| Ice | `150,200,236` / `172,214,242` / `112,166,216` | glints: diagonal strokes `232,246,255`, three texels (blue enough to stay ice under the day light, like the snow) |
| Marsh | `124,106,86` / `140,122,98` / `90,76,66` | wet patches: flat ovals `98,112,108`, five texels wide with a light texel |

Three kinds are built things and keep straight edges, tile for tile, instead of the rounded masks:

| Kind | Look |
|---|---|
| Paving (the ground of cities) | stone slabs 16 texels square, `204,206,214`, one in four a shade darker `194,198,208`, with a joint line `168,172,188` along each slab's east and south edge. Where paving meets other ground it ends in a kerb two texels wide, `150,154,172` with a light line `226,228,234` inside it |
| Planks (bridge decks, boardwalks) | boards 8 texels wide laid across the way one walks, `178,134,92`, every third one a shade lighter `192,150,104`; a groove `126,90,62` between boards, a nail texel `96,70,52` at each end, and a dark rail line `104,74,54` along the deck's open sides |
| Stairs | treads 8 texels deep in stone `184,178,170`: a light nose `214,210,204` on the edge that faces downhill and the riser's shade `122,116,124` under it, so a flight reads as steps whichever way it climbs |

### Relief

Height comes from the map (plan 01 · M3) and is drawn true to scale: one tile up is one world unit, which the camera shows about half a tile tall.

- **The lowlands stay flat.** A map's ground level is drawn at zero, exactly as a map without heights is.
- **Small steps are slopes.** Where neighbouring tiles differ by less than three quarters of a tile, the lower one rises to meet the higher across its own width (`Relief`): a kerb or a beach is a gentle bank, never a sliver of wall.
- **Water lies level with its banks.** Sinnoh's water is half a tile under the ground around it; the game says that with the painted bank (see Water) and draws the surface level with the shore (at the whole tile above the water's own height), so a pond is a flat sheet of pixel art and a river runs level under its bridges.
- **A waterfall** is a sheet of falling water down the slope between two levels: streaks of light `150,206,246` and of foam `236,246,255` on mid-blue `76,146,214`, each with a dark line `46,104,186` beside it, and clumps of foam where the water goes over the edge and where it lands. It doesn't move yet.
- **A step of three quarters of a tile or more is a face**: a wall from the lower ground up to the higher, drawn where it faces south, east or west (a face that looks north is never seen). Its art keeps square texels on screen, so it has 32 texel columns and about 16 rows to the tile:
  - under lawn, flowers, tall grass, paths and sand: an **earth bank**, the ledge's face carried down: bright edge `160,222,122`, scalloped grass lip `120,200,102`, then dirt `178,138,90` with a broken strata line `146,108,72` every six rows
  - under rock, snow, ice and cave floors: a **rock face** in the boulder's shades: a light top edge `196,192,190`, then beds of `150,146,150` of uneven thickness, each lit along its top `176,170,166` with a longer glint or two, shaded `104,100,112` where it overhangs the next, and cracked `74,70,86` in one or two places part of the way down. Long beds and few cracks: strata, never a wall of blocks
- **Everything stands on the ground it is on**: trees, grass, props, buildings and people are lifted with their tile, and the camera follows the player's height.
- **Faces cast shadows; flat ground does not**, as before. Ambient occlusion darkens the foot of a face.
- **Ledges** face south, west or east. The side ones are the same ridge turned: the grass lip runs along the top of the drop and the dirt face shows as a narrow strip, since the camera sees it edge on.


### Water

Water lies in the ground plane and is drawn texel by texel (`PixelGround` bakes a shore-distance mask, the water shader colours it), so shores are rounded pixel art and nothing is a smooth gradient.

| Element | Value |
|---|---|
| Depth bands | shallow `116,184,232` to 7 texels from the shore, mid `76,146,214` to 20, deep `58,118,190` beyond; the band edges breathe by a texel |
| Foam | `236,246,255`: a line at the shore 1–3 texels wide that laps in and out, and a broken second line 3–7 texels out |
| Wave marks | light dashes `150,206,246` 4–8 texels long with a dark line `46,104,186` under them and their ends dipped, drifting east two texels a second, never nearer than 9 texels to the shore |
| Sparkles | white plus-signs that flash for a moment, in sunlight only |
| North bank | where land lies to the north the bank shows as a face 5 texels deep, `124,116,116` over `88,84,98`; other shores get a one-texel light rim |
| Motion | everything moves in whole texels; no sub-texel scrolling |

### Grass, flowers, trees, ledges and rocks

- **Tall grass**: two rows of clumps per tile, each clump five blades in four flat shades (`36,110,62`, `62,150,78`, `106,196,98`, tips `170,232,130`), 16 texels tall, the rows half a clump out of step. They sway at the top and lean away from anyone walking through.
- **Lawn tufts and flowers**: single upright cards facing the camera (crossed cards read as scribbles from the steep camera). Tufts are a small clump in the lawn's shades; flowers are a stem, two leaves and a five-texel blossom, mostly white, some red or yellow.
- **Trees in snow country** (an area with more snow than lawn: Snowpoint City, Routes 216 and 217, not Twinleaf Town with its few patches): every tier of a pine is white at its tip and keeps its green toward its rim.
- **Trees**: grey textures in four flat tones (118, 162, 206, 244) tinted per tree. Pine tiers are bands of scalloped needles with a toothed rim; round crowns are overlapping leaf clumps, each with a light crescent on the upper left and a dark one on the lower right. Bark is `116,80,54` with dark grooves `86,58,42` and a light line beside each. No per-texel random variation.
- **Ledges**: a ridge 12 texels high: a bright edge `160,222,122`, a grass lip (`120,200,102`, scalloped) over a dirt face `178,138,90` with one broken strata line `146,108,72` and a dark base `116,84,60`.
- **Rocks**: a boulder is a sprite like the other props: a big stone with a smaller one at its foot, in three flat shades (`196,192,190`, `150,146,150`, `104,100,112`) with a crack line `74,70,86` and an outline; every other one is mirrored. In water it stands a little lower, in a ring of foam.
- **Obstacles** (what Cut, Rock Smash and Strength clear) are sprites that can be told from the scenery and from each other at a glance: a **small tree** with a thin trunk and a round crown in the tall grass's greens; a **cracked rock** in browns (`204,168,126`, `168,128,92`, `122,90,70`) split right across by cracks `78,56,50`; a **round boulder**, smooth and grey (`214,210,204`, `164,160,162`, `112,108,122`) with one glint and one dimple.
- **The swimmer**: whichever Pokémon knows Surf is shown as one round blue swimmer (`86,128,206`, back `138,180,240`, underside `58,88,160`), 48 by 26, seen from the front (two eyes), the back (a tail fin) or the side (the head leading). The rider stands on its back, twelve rows up, and the two bob a texel together.
- **Sinking**: in deep snow and in marsh mud a walker's sprite is lowered by a few rows (4, 7 and 10 for the three depths of snow; 2 and 6 for mud and deep mud) and the ground hides the feet.

### Buildings

A building is a 3D box dressed in pixel art at 32 texels per tile. Every wall face is painted as one piece (a façade, `BuildingArt`): the wall material, a base course, corner posts, then windows, doors, plaques and lanterns placed bay by bay (a bay is one tile, 32 texels). Nothing floats on a wall as a separate decal, so everything sits on the texel grid.

- **Materials**: a flat base colour with one-texel light and dark lines, two or three shades, no per-texel variation.

| Material | Base / light / dark | Drawn as |
|---|---|---|
| Planks | `214,170,118` / `232,194,146` / `150,106,74` | boards 8 texels tall: a light top line, a dark groove, butt joints staggered row by row; one board in five a shade darker |
| Clapboard | `240,236,226` / `252,250,244` / `196,196,210` | boards 6 texels tall with a grey shadow line under each |
| Plaster | `242,232,208` / `250,244,226` / `220,206,180` | flat, with a few short trowel marks three to five texels long |
| Brick | `190,106,84` / `210,130,100` / `156,82,72`, mortar `226,206,184` | courses 4 texels tall, bricks 8 wide, half a brick out of step; whole bricks lighter or darker in a fixed pattern |
| Panel | `214,222,232` / `234,240,246` / `150,162,184` | panels 32×18 with a dark joint and a light line beside it |
| Base course | `158,152,150` / `190,186,182` / `104,100,112` | stone blocks 16×7 with a light top and a dark bottom line |
| Timber posts and beams | `124,84,58` / `156,110,76` / `92,62,48` | four texels wide at the corners of plank walls |
| Boards | `188,144,98` / `210,168,120` / `136,98,70` | the planks' darker boards all over: farmhouses |
| Half-timber | plaster `240,230,206` between beams `110,78,58` / `140,102,74` / `82,58,48` | posts 3 texels wide every 16, a rail at window height, a brace across each end bay |
| Stone | `176,172,170` / `198,194,190` / `124,120,130` | cut blocks 16×8, half a block out of step; one in five a shade darker `160,156,158` |
| Log | `156,110,72` / `188,140,94` / `108,74,56` | logs 8 texels tall, a light line on top and a dark one under each, butt ends every 48 texels staggered row by row |
| Stucco | `244,242,236` / `252,252,248` / `206,208,220` | white and flat, with the plaster's few trowel marks |
| Sheet metal | `150,160,176` / `178,188,200` / `112,122,144` | upright ribs 4 texels wide (light, base, base, dark) and a seam every 32 |
| Dark panel | `104,116,130` / `132,144,156` / `70,80,98` | the panel's joints on a dark ground: Team Galactic's buildings |

- **Roofs**: pitched roofs run their ridge east–west, so the south slope faces the camera almost square-on and its tiles show at full size. Tiles are 8×8 texels in rows half a tile out of step, each with a light upper-left edge, a darker lower right and a dark rounded bottom line; one tile in eleven is a shade lighter. The slope length is a whole number of rows. Eaves overhang 8 texels with a fascia 3 texels deep; gable ends have a pale bargeboard; a ridge cap runs along the top. Centers, Marts and the lab have hip roofs; city blocks have flat roofs behind a parapet, with equipment on top.
- **Walls** under a pitched roof are 56 texels tall; the eave hides the top ten or so, and the 18 texels below the wall's top are painted two flat steps darker (30 % and 15 %) as the eave's shadow. A city block has a ground storey of 44 texels, upper storeys of 32 and a band of 18 along the top that carries its name.
- **Windows**: a white frame 2 texels wide, glass in three flat blues (`170,218,246`, `122,190,236`, `88,156,220`) with one diagonal streak `232,246,255`, mullions 2 texels wide, a sill, and on houses shutters in the roof's colour and a flower box.
- **Doors**: a house door is 22×39 with a pale frame, two rows of sunken panels, a small window and a brass knob, and a wall lantern beside it; public buildings have double glass doors 36×40 in a steel frame, the school a pair of wooden ones. A stone step stands in front of every door. A building the map gives no door (it can't be entered yet) still shows one in a middle bay: shut, its glass dark.
- **Entrance block**: public buildings under a pitched roof (Center, Mart, lab, school) have a block around the door that stands 10 texels proud of the wall and rises to 74, above the eave. Its header, 24 texels tall in the building's accent colour, carries the sign: our own Poké Ball roundel on red for a Center, `MART` on blue, `LAB`, `SCHOOL`, in a 5×7 pixel alphabet of our own drawn at double size with a shadow. City blocks write their name on the band along their top instead.
- **Roof colours** follow the map's roof tile and the building: teal `52,166,138`, red `214,82,66`, blue `70,118,214`; Center `238,104,58`, Mart `66,122,222`, lab `48,178,198`, the school's slate `92,110,156`. Each colour is its own tile texture, so its darks lean violet and its lights warm instead of being one grey texture tinted.
- **Storeys**: a house whose model stands 5.4 tiles tall or more has a second storey: 32 more texels of wall with its own row of windows, under the same roof. A block under a flat roof has one storey for its ground floor and one more for every 1.95 tiles its model stands above 3.8, up to six.
- **Porches**: where the world's data builds the entrance out a tile in front of the wall, so does the building. A *closed* porch is an entrance block as wide as its tiles and 32 texels deep, with the door in its own front and the sign on its header; an *open* one is two side blocks with a canopy between them (its underside 52 texels up), and the door is in the wall behind, so whoever walks in is seen all the way to the door. Under a pitched roof either is 74 texels tall, like the entrance block; under a flat one it is one storey.
- **Wings**: a model whose blocked tiles are more than one rectangle is built as its main block (the one with the way in) and plainer wings of one storey in the same walls, without doors or signs. Pieces one tile thin are a fence where the town builds in wood, a low stone wall elsewhere.
- **Style per town** (the model's own, from `Data/WorldModels.cs`; `Map.Architecture` for a hand-made map):

| Town | Walls | Roof | With |
|---|---|---|---|
| Twinleaf (Timber) | planks with timber posts | gable, teal `52,166,138` | shutters, flower boxes, a stone chimney |
| Sandgem (Plaster) | plaster over a stone base | gable, red `214,82,66` | flower boxes, a chimney |
| Jubilife (City) | brick blocks of two to five storeys | flat, behind a parapet | sash windows, balconies, vents on the roof |
| Pallet (Clapboard) | white clapboard | gable, red | shutters, flower boxes |
| Oreburgh (Brick) | brick | gable, coal grey `92,94,108` | a chimney; its blocks of flats are brick under flat roofs |
| Floaroma (Cottage) | white clapboard | gable, rose `226,112,140` | shutters and a flower box at every window |
| Eterna, Celestic (HalfTimber) | half-timber | gable, moss `86,128,96` | a chimney |
| Hearthome (Townhouse) | plaster | hip, plum `150,98,150` | shutters |
| Solaceon (Farm) | boards | gable, straw `206,170,96` | a chimney |
| Veilstone (Stone) | cut stone | gable, grey-blue `112,124,150` | |
| Pastoria (Marsh) | planks | hip, reed green `116,156,84` | flower boxes |
| Canalave (Harbour) | brick | gable, slate blue `84,104,140` | a chimney |
| Snowpoint (Snow) | logs | gable, deep in snow | a chimney |
| Sunyshore (Seaside) | stucco | hip, orange `240,150,70` | a solar panel on the south slope |
| Lakeside, Battle Zone (Resort) | white clapboard | hip, turquoise `64,170,180` | shutters |

- **Snow on a roof**: instead of tiles, a 64×64 sheet of snow `226,234,246` with drift lines `204,216,238` three to six texels long and a blue shade `188,202,230` along the two rows above the eave; the fascia stays the building's own. Snowpoint's Center and Mart keep their shapes and signs under it.
- **Kinds of building** (public buildings look the same in every town unless said):

| Kind | Walls, roof | Marks it out |
|---|---|---|
| Pokémon Center, Mart, lab | plaster, hip roof (orange, blue, cyan) | the entrance block with its roundel or name |
| Gym | plaster, hip roof in its leader's type: Rock `150,128,100`, Grass `92,170,96`, Ghost `124,98,170`, Fighting `204,96,72`, Water `72,140,214`, Steel `138,150,170`, Ice `126,200,220`, Electric `236,196,64` | an entrance block 84 wide in that colour, signed `GYM` |
| Gate house | stone, hip roof green-grey `104,146,122` | an open porch where the road goes through north and south; a door in each end wall where it goes east and west |
| Shop (flower shop, cycle shop, café, market, Game Corner, Day Care, Fan Club) | its town's house | an entrance block wide enough for its name, in the roof's colour |
| Department store | panel block of five storeys | its name on the band along the top, ribbon windows |
| Museum | cut stone under a flat roof, two storeys | `MUSEUM` on the band, arched windows |
| Library | brick under a flat roof, three storeys | `LIBRARY` on the band |
| Hotel, restaurant | the Resort's white boards, two storeys | an entrance block with its name |
| Hall (Contest Hall, Pal Park, the Great Marsh's gate, the Battle Frontier's facilities) | plaster, hip roof: Contest magenta `214,92,150`, Pal Park green `96,168,104`, the Frontier's gold `214,170,72` | an entrance block 84 wide with its name |
| Foreign Building (Chapel) | cut stone, two storeys, a steep slate roof `92,104,140` | a round rose window of coloured glass over the door, a small spire on the ridge |
| Temple (Snowpoint) | weathered stone `150,150,160`, flat roofs under snow | two tiers: the wings a storey lower; arched openings between pilasters |
| Shrine (Celestic) | timber | a heavy hip roof `120,74,66` with eaves twice as deep as a house's, no windows |
| Tower (Lost Tower, Battle Tower) | cut stone | four tiers, each a little narrower than the one below, a slit window in each, under a pointed slate cap |
| Lighthouse | white stucco with a red band `214,72,62` | three narrowing tiers, a gallery, and a lantern room whose glass burns all night |
| Factory (the mine's works, the Windworks, the Ironworks) | sheet metal under a flat roof | no windows but a strip under the eave; a sliding door; vents, and a smokestack on the Ironworks |
| Warehouse | sheet metal under a low gable in the same metal | a sliding door two bays wide |
| Mansion | plaster, two storeys, hip roof slate `88,98,126` | arched windows, an entrance block |
| Team Galactic's buildings | dark panel under a flat roof | slit windows `120,230,220`, a yellow band `232,204,76` along the top, spikes on the roof corners |
| Pokémon League | pale stone `214,210,204`, flat roofs | three tiers stepping back, a crimson band `196,60,70`, arched windows, a grand open porch |


- **Lights after dark** are marked in the art itself, texel by texel (their alpha), not found by colour. *Public* lights (street lamps, wall lanterns, Centers, Marts, signs) come on at twilight and stay on all night. *Home* lights (house windows) are on in the evening and off late at night, except in about one house in three. Lit glass keeps its pixel detail: each texel becomes lamplight `255,204,117` scaled by its own brightness.

### Props

- Small things standing outdoors (street lamps, mailboxes, planters, benches, signposts) are **sprites on upright cards**, drawn seen from a little above like the characters, with a one-texel outline in a darker shade of their own colour. They take the scene's light and cast real shadows.
- **Fences** are real geometry: a post 4 texels square and 17 tall in the middle of every fenced tile, with two rails toward whichever neighbouring tiles are fenced too, so runs, corners and ends come out by themselves. Wood where the houses are timber, white where they are clapboard.
- **Street lamps** are 78 texels tall: an iron post on a stepped foot with a four-sided lantern. After dark the lantern glows, a soft halo sits around it and a round pool of light lies at its foot.
- Towns are furnished through the map data (`Fence`, `LampPost`, `Mailbox`, `Planter`, and `Bench`, which outdoors is a park bench): a fenced front garden with the mailbox at its gate, lamps beside the roads, planters either side of public doors, a bench where there is something to look at. In the imported world the lamps stand where the original's do, and the rest of what stands about comes from its models:

| Prop | Look |
|---|---|
| Low wall | where a fence would be in a town of brick or stone: a wall 12 texels tall and 8 thick in cut stone with a pale cap, joining up tile to tile like a fence |
| Fountain | a round basin of pale stone, 3 tiles across, its water in the pond's blues with two ripple rings, and a jet on a pedestal in the middle: a sprite of white and pale-blue streaks 40 texels tall |
| Boat | a white hull with a blue band along the waterline, a planked deck, a cabin with a row of windows and a red funnel; it lies the way its model is longer |
| Wind turbine | a white tower tapering from 10 texels to 6, as tall as its model, a nacelle, and three blades 44 texels long (a sprite; they turn in plan 04 · G9) |
| Statue | a stepped plinth of cut stone carrying a weathered figure in green bronze `98,150,138` (ours: a long-necked creature rearing on its hind legs) |
| Honey tree | a broad round crown in a warmer, yellower green than the forest's (`150,190,84`, `196,220,104`, `104,150,70`) on a thick trunk with an amber patch of honey `236,176,64` |
| Crates | two wooden crates and one on top: boards `188,134,84` with dark frames |
| Coal heap | a mound in three flat darks `74,72,84`, `54,52,66`, `104,102,112`, with two-texel glints |
| Hedge | a clipped box of leaves in the planter's greens, as large as its model's tiles; white on top where its town lies under snow |
| Column | a fluted shaft of pale stone on a square base, as tall as its model; broken ones end in a slanted break |
| Topiary | a cone of clipped leaves on a short trunk |
| Cairn | three worked stones one on another: the Hallowed Tower, a stone tablet |
| Billboard | a board 44 texels wide on two posts, pale with three dark lines |
| Outcrop | a mass of rock as large as its tiles, in the boulders' three shades with ledges every 10 texels |
| Mast | a lattice mast as tall as its model, a red light on top that burns all night |

### Rooms

- A room is a doll's house: floor, a back wall 80 texels tall and two side walls, cut away at the front. The wall strip is crown moulding, wallpaper with a small repeating motif, a chair rail, panelled wainscot and a skirting board.
- **Floors**: planks 8 texels wide in three wood tones (whole planks, never single texels) with dark grooves and staggered joints; or tiles 16 texels square in two close tones with a grout line and a light edge. Walls shade the floor beside them in two flat steps.
- **Furniture** is built from boxes whose every visible face is painted: a light line where the top meets the front, a dark line at the foot, panels and drawers as sunken or raised bevels, handles and knobs of two texels or more. Plants and vases are sprites on cards.
- **Rooms follow the clock**: by day the windows show sky and throw a soft patch of light across the floor, slanting the way the shadows fall; at twilight the glass and the patches turn orange; at night the glass is dark blue (`18,26,56`), the patches are gone and the room is lit warm and a little dimmer by its lamps (`ArtLook.IndoorRigFor`).

### Characters

- **Sculpted with the SDF kit** (G6): each character is one smooth body of signed-distance shapes (`SdfModel`: ellipsoids, round cones, round boxes, tori, cylinders) joined with smooth unions, carved with cuts and coloured with paint, meshed by surface nets into a single skinned mesh (`SdfMesher`, cells of 1/112 unit, 34,000 to 42,000 vertices) and cached on disk (`SdfCache`). The same model is the 3D trainer in battle and the source of the field sprites (`CharacterModels`).
- **Proportions**: chibi. Children (the player, the rival, youngsters, lasses) are about 1.2 units tall with a head of radius 0.27, nearly half their height; adults have a longer body and a slightly smaller head (radius 0.25), then `CharacterStyle.Height` sizes each type. The player reads at about 130 px tall on screen.
- **Parts**: mitten hands with a thumb; shoes with a sole in their own colour (white under red or blue shoes); sleeves ending in a cuff, or short sleeves at two thirds of the upper arm; a collar or a scarf; stripes painted across the shirt (light on a coloured shirt, the accent colour on a white one); skirts and coat tails on a bone of their own so they swing; the player's backpack with straps and a buckle. Colour edges are painted crisp (waistline, sleeves, soles), never blended.
- **Hair is sculpted in locks**: tapering tubes laid along the skull from the crown (from the hairline, for swept hair) to pointed tips, over a snug cap, with small blends so grooves show between them; bangs end just above the eyes; long hair falls over a smooth mass behind and swings; spiky hair stands in tufts. Under a beret or cap the locks start below its edge, so no hair shows through the hat, and they lie close enough at the back that the hat's band doesn't show between them. A hat sits on the hair, never in it: the nurse's cap stands on top of the hair, and a cap's peak comes out of the crown's front edge with the bangs below it. White hair is a shaded grey-white (`190,194,210`), never pure white.
- **Faces in battle**: a smooth texture (128 px, anti-aliased, cut out round the features) laid over the front of the head: dark rim, an iris lighter toward the bottom, a pupil, two white highlights (upper left, lower right), a heavier upper lid, lash flicks for long lashes, brows only where the bangs leave the brow bare, a small mouth (under the moustache, if any). Blush is painted on the cheeks of the model itself. Expressions: neutral, happy, surprised, sad, angry, each also with the eyes shut for blinking.
- **Pixel faces on sprites**: a smooth face shrunk to sprite size turns to mush, so each baked frame gets a face stamped where the point between the eyes lands: eyes two texels wide and three tall (lash row, then a white highlight on the upper left beside the iris, then iris), three texels apart; a mouth of one to three texels two rows below; one eye in profile, none from behind. A blink is a two-texel line.
- **Light on faces**: the normals of the front of the head are bent mostly upward (toward `0, 0.75, 0.66`), so a face lights evenly under any light from above and the edge of the shade never splits it, in profile too.
- **Light on hair**: the hair's normals are bent most of the way (70 %) toward those of the smooth mass it fills: a sphere round the head, stretched into a capsule down the back for long hair. The shade's edge and the sheen then run cleanly across the head instead of breaking into a blotch on every lock; the locks still show in the silhouette, the outline and the grooves between them.
- **Animation** (`CharacterAnimation`, 19 bones): idle breathing with the arms lowered from the sculpted A-pose; walk and run cycles in which the hips drop as the legs spread, so a foot stays on the ground, with arms swinging against the legs (the run leans forward with the elbows bent); the ledge hop; emotes (wave, surprised, nod, cheer). Hair, bag and skirt swing a beat behind the body. Raised hands stay beside the big head, never behind it ("\o/", not straight up).
- **Sprites** (`CharacterSprites`): 40×58 texels, orthographic, seen from 24° above, studio light from the upper left, the pixel face stamped on, then a one-texel outline tinted from the neighbouring colour (`PixelCanvas.OutlinePass`). Frames for each of the four facings: 2 idle breaths, 8 walk, 8 run, 3 hop and 6 per emote; blinking and expressions are frames of their own. Drawn as upright cards lit by the scene (sky × 0.62 + sun × 0.78), casting real shadows, with a soft contact blob under the feet. A trainer who spots the player starts (the surprised emote) under the "!".
- **Hand-drawn overrides**: a 40×58 PNG at `overrides/sprites/<TYPE>/<facing>_<strip>_<frame>[_blink][_<expression>].png` next to the game (for example `PLAYER/down_walk_3.png`) replaces that baked frame.

### Life

What moves in the field (G9). All of it is pixel art in motion: whole texels, a few frames, flat shades, and it fades in two or three steps of alpha, never smoothly. The one exception is mist (fog, and the haze of a sandstorm or a blizzard), which is soft like the depth-of-field blur it lies in.

- **Wind**: everything that sways (grass, flowers, leaves) sways more as a gust passes. Gusts cross the map from the west, one about every twelve seconds, swelling the sway from three quarters of its usual size to a third more and leaning tips a little eastward. Weather raises it: a little in hail, half as much again in rain, more in heavy rain, and twice in a thunderstorm, heavy snow, a blizzard or a sandstorm.
- **Footprints**: on the ground that keeps them in Platinum (its sand, and snow of every depth) each step leaves one shoe print, 6 by 11 texels, left and right alternately, in a darker shade of the ground (`196,170,118` on sand, `140,160,206` on snow). They stay three seconds, then fade in two steps over three more.
- **Dust**: a run raises one puff behind each step, a little to the side of the foot that made it, and a hop lands in two: a round cloud in three frames (6, 10 and 12 texels across) over 0.36 s, in the colour of the ground it comes from (path `214,196,150`, sand `232,218,170`, dirt `170,134,98`, snow `214,226,244`, anything else `200,196,190`), rising three texels as it goes.
- **Tall grass**: stepping in throws up four leaf bits (6 by 4 texels, lighter than the grass so they show against it: `168,226,112` and `110,196,92`, with a dark rim), two to each side: the inner pair fly up 18 texels, the outer pair lower and wider, each turning over at the top of its arc, and they are down again in 0.5 s.
- **Water**: a swimmer leaves a ring on the water behind every stroke (three frames, 8 to 14 texels across, `210,236,255`, gone in 0.7 s); a puddle gives a small ring and two drops, water ankle deep a ring alone, and the water someone has just ridden out onto a wide ring and four drops that fly higher.
- **Waterfalls** fall: the sheet's streaks move down four texels eight times a second.
- **Fountains and turbines**: a fountain plays in four frames at six a second (the rings in its basin travel outward, the jet's top bobs a texel, drops fall away from it in an arc); a wind turbine's three blades turn in four frames of thirty degrees at six a second, one full turn in two seconds. Like the waterfall, they are redrawn in place, frame by frame, never slid.
- **Weather** is in the air, on the ground and in the light. What falls is drawn over the scene in the picture's own pixels (3 px blocks at 1080p) and is blown the way the wind goes, eastward. Mist is a soft cloud texture in two layers that drift at different paces. Under any of them part of the sun is lost, and half of what level ground loses by it comes down from the whole sky instead, so shadows grow faint before the picture grows dark:

| Weather | In the air | On the ground | Sun | Fog |
|---|---|---|---|---|
| Cloudy | nothing | | 0.45 | a little |
| Rain | 220 slanting streaks of three blocks, `176,200,236` | 360 drops a second land in view: a fleck `236,242,255` on the ground, and on water one in three leaves a small ring | 0.3, colours a fifth duller | a little |
| Heavy rain | 380 streaks of four blocks | 720 drops a second | 0.22, colours a fifth duller | some |
| Thunderstorm | as heavy rain; lightning whitens the picture twice within a quarter of a second, once in every eight seconds at a moment of its own (three to thirteen seconds apart) | 720 drops a second | 0.2, colours a fifth duller | some |
| Snow | 140 flakes of 3 and 6 px, white, swaying as they fall | | 0.6 | a little |
| Heavy snow | 320 flakes of 3 to 9 px driven hard, each with a short tail; a thin white haze | | 0.35 | near and pale |
| Blizzard | 460 such flakes driven nearly level; two layers of white haze | | 0.25 | near and pale |
| Hail | 180 pellets of 6 px, `224,238,255`, falling fast and steep | 180 pellets a second land with a fleck | 0.4 | a little |
| Fog | two layers of pale mist `232,236,244` | | 0.5 | hides what is ten tiles off |
| Sandstorm | 240 streaks of sand `226,198,140` driven hard; two layers of sand haze | | 0.55 | near and sand-coloured |
| Ash | 110 grey flakes of 3 and 6 px falling slowly | | 0.75 | a little |

These are the kinds Platinum's map headers name. Five places (the south of Route 212, Route 213, Route 216, Acuity Lakefront and Snowpoint City) take theirs from a calendar in Platinum; until that calendar is imported with their areas they have the weather they have most days.

- **Doors** open: as someone steps up to a door that leads somewhere, it opens in two frames over 0.2 s (a wooden door swings in against its frame; glass doors slide apart) on a dark room `40,34,48`, and it shuts again 0.4 s after someone has stepped out of it.
- **Emote bubbles**: a white bubble 20 by 22 texels with a tail, over the head, popping up over three frames: `!` in red, `?` in blue, three dots, a note, a heart, `Zz`, a drop of sweat. It stays 0.9 s unless told otherwise.
- **The camera** follows the player's height smoothly (stairs, bridges, a hop) and can be sent to look at another spot and back, easing in and out over the time it is given (for the story's scenes).
- **Into battle**: the field flashes white twice in a quarter of a second, then closes. Which way depends, as in Platinum, on who is met and on whether their first Pokémon is of a higher level than the player's first: a pinwheel of eight dark blades for a wild Pokémon, and the picture breaking into shards from the middle outward for a stronger one; shutters from both sides for a trainer, crossed with shutters from above and below for a stronger one; diamonds growing from a grid for a Gym Leader. The battle opens through an iris for a wild Pokémon and through the same shape in reverse for the others. Closing takes 0.9 s and opening 0.5 s; the dark is `14,14,22`, never pure black. (Platinum also tells water and caves apart, and gives the Elite Four, Team Galactic and the legendary Pokémon effects of their own: those come with their places.)

### Light, shadow and grading (day)

| Setting | Value |
|---|---|
| Sun direction | normalize(−0.6, 0.8, 0.42) |
| Sun colour | 0.66, 0.55, 0.40 (golden) |
| Sky / ground ambient | 0.50, 0.55, 0.74 / 0.46, 0.44, 0.40 |
| Diffuse | smooth Lambert (the pixel art carries the shading) |
| Shadows | soft and without grain: nine lookups of the shadow map on High, one texel apart, each the graphics card's own filtered comparison of the four texels round it, so that an edge is one even ramp four texels wide (about a dozen pixels at 4K). Four lookups on Medium, one on Low. Lookups further apart than a texel leave steps in the edge. On a machine where the card can't be asked to compare, a Poisson disc 1.8 texels in radius, rotated per pixel, stands in |
| Cloud shade | 20 % dimmer sunlight in broad patches that drift slowly across the map |
| Fog | toward `0.72, 0.82, 0.92`, 18 % at the far edge (starts 44 units from the camera, full at 72) |
| Ambient occlusion | screen-space, quarter resolution, read from a half-resolution copy of the depth buffer, radius 0.45 units, strength 0.4: grounds walls, props and sprites |
| Tilt-shift | a blur growing to 6 texels in radius (a disc of 12 taps; within 2.5 texels five taps are as dense as the texels and do), plus 95 % mix toward a wide blur outside a ±30 % focus band |
| Bloom | threshold 0.86, strength 0.38 |
| Grade | saturation 1.10, contrast 1.08, shadows × (0.88, 0.92, 1.10), highlights × (1.07, 1.00, 0.90) |
| Vignette | 0.26 |
| Rooms | their own warm lamp light at every hour; tilt-shift 4, depth of field 60 %, focus ±36 %, bloom threshold 0.93 at 0.22 |

The camera target and every sprite snap to the texel grid (1/32 unit), so pixel art scrolls in whole texels and doesn't shimmer. The other times of day are under *Time of day* below.

## Battles (3D)

### Models

- Pokémon and trainers are real 3D models on the platforms (no pixelation), lit and shadowed with the stage. Trainers are the SDF characters of the field, skinned on the GPU, with their smooth faces.
- Size rule: a Pokémon covers the part of the screen its 128-px sprite frame would, i.e. the frame spans the platform's width ÷ 158 px (opponent) or ÷ 150 px (player).
- Shading: two-tone cel ramp with a soft terminator (`smoothstep(0, 0.12, N·L)`), hemisphere ambient × 1.08, rim light 0.30.
- Outlines: offset shells of the SDF (the surface pushed out, meshed on its own and skinned with the body, drawn with front faces culled), so creases, brims and gaps between limbs never show spots of ink; colour = surface mixed toward `34,26,40`. A character's shell is 0.009 units out; a Pokémon's is half a pixel of its 128-px sprite frame (`WorldPerPixel × 0.55`).
- Flashes (send-out, hit, recall, the dark silhouette of a wild Pokémon before the camera settles) are colour blends in the shader, not extra sprites.

### Pokémon (version 2, G7)

- **One smooth body** sculpted with the SDF kit (`PokeBuilder` on `SdfModel`): ellipsoids, round cones, flattened spikes, rings and boxes joined with small smooth blends (2.5 cm for body parts, 1–1.5 cm for spikes and claws, sharp for a jaw's lip), meshed at 128 cells across the model's largest dimension (17,000–47,000 vertices) and cached on disk. Colour regions that shouldn't change the shape (masks, bands, a flame's heart) are paint with a soft edge 1.4 cm wide, or they follow the mesh's vertices in steps.
- **Materials**: fur and feathers matt (`Fur`), smooth hides with a small sheen (`Scales`), shells, claws, beaks, teeth and horns hard and glossy (`Shell`), gold and steel `Metal`, leaves `Leaf`, flames `Glow`.
- **Body plans** set the skeleton and how the clips move it: biped, quadruped, bird, serpent, fish, floating. Every plan has a root at the feet and a body bone at the middle of the torso; legs hang from the root so they stay planted while the body breathes above them; head, jaw, arms, wings, tail, ears, leaves, flames and fins have bones of their own; a serpent is a chain of segments.
- **Eyes and markings are decals**, never sculpted: each is a 96-px square of an atlas laid over the triangles under it (projected along its normal, lifted a hair off the surface) and cut out where it is empty. The eye or marking fills the middle half of its square, so coarse triangles never clip it. Eyes come in four states, each painted with anti-aliased shapes: **open** (a dark oval whose lower part is the iris, lighter toward the bottom, with a big glint upper left and a small one lower right; or a white eye ringed in ink with a pupil looking a little inward), **shut** (a gentle curve, for blinks and fainting), **squeezed** (a chevron pointing to the nose, when hit) and **fierce** (a lid pressed toward the inner corner, painted in the colour of the skin round the eye, while attacking). A species whose eyes are always closed (Abra, Snorlax) shows them shut in every state but the squeeze; one that always glares (the Gastly line, Honchkrow) shows them fierce whenever they are open. A white eye's white can be another colour (Hoothoot's red, Spiritomb's lime). Markings are dots, rings, four- and five-pointed stars, bars, wavy lines, zigzags, triangles and diamonds.
- **Hand-built models** (plan 03 · D6–D9, and decision 3 since): Platinum's Sinnoh Pokédex is sculpted species by species, and the popular species from outside it follow batch by batch from Kanto, our own sculpts after each design's shapes, colours and proportions, with the same kit and rules. Membranes and fins are thin: a bat's wing is skin over its arm and finger bones, filled triangle by triangle from the wrist and scalloped between the fingers; an insect's or a fish's fin is a flat oval rimmed in a darker or paler colour. Rock is lumps over a body (Geodude's line) or a chain of separate boulders with a crease between each (Onix); honeycomb is hexagonal cells. A wing whose two faces are coloured differently is two membranes laid back to back, its finger bones in the back one so the front stays clean (Charizard's, teal in front and orange behind). A lightning bolt is a flat zigzag (Pikachu's line), a curl a tube wound flat into a spiral (the Clefairy line's foreheads), whiskers thin pale tubes. A leaf with a jagged edge is a blade with leaflets swept past its edges (Ivysaur's and Venusaur's palm leaves); a snake rests on its coils laid on the ground (the Ekans line); a flame is lit from within, pale at its heart. Fins, leaves and crests that come to a point are blades, flat to the side they face (Remoraid's fins, Leafeon's leaves, Gallade's crest); a leaf or a frond that is widest in the middle is an oval flat to the side it faces (Tropius's wings). Vines are tubes looped over a ball, the face left bare (Tangela's line); fur that hangs is points pointing down round a body (Piloswine's line); a tiger's stripes are short bands painted across the surface, never rings round it (Electabuzz's line); a drill is a cone with grooves round it (Rhydon's line); a body of flat faces is blocks joined without blending (Porygon). Porygon-Z's parts float apart, as the design has them. Fliers and floaters hang 12 % of their height above their platform, as the generated ones do. Eyes follow the design: two as a rule; none for Zubat, for Ralts (under its hair), for Nosepass (shut in a black band), for Yanmega (its eyes are its head) and for Piloswine (under its fur); one for Unown, Kirlia (the other under its hair), Magnemite and the Duskull line; three for Magneton and Magnezone; a pair on each of Combee's three faces.
- **Generated models** (plan 03 · D5): every species without a hand-built model is sculpted from its data with the same kit, the same rules (smooth bodies, paint for patches, decals for eyes and marks, the materials below) and the same body plans. Chibi proportions shrink with evolution: basic species and babies have the biggest heads and shortest legs, final stages and legendaries the longest. Colours: the Pokédex colour as the body, nudged in hue and value by the evolutionary line so families share a shade; an accent from the types (a flame's orange, a leaf's green) where it stands apart from the body; a pale belly and muzzle; dark paws, stripes and masks. Fliers and floaters hang 12 % of their height above their platform.
- **Forms** (plan 03 · D11): a form (a Mega, a regional form, a Gigantamax form, Deoxys's formes) is a model of its own, generated as its species would be from the form's types, stats and size and in the form's own Pokédex colour (black for Mega Charizard X, white for Alolan Vulpix), as one of its species' family, by Platinum's values whatever rules the game is played by. Every form of a hand-built species is hand-built like the species. Platinum's own: Rotom's appliances are the appliance in Rotom's orange with Rotom's eyes, its plasma limbs in the appliance's own colour; Giratina's Origin Forme floats as a banded serpent with six shadowy tendrils; the cloaks and the East Sea keep their species' build in other materials and colours; the Unown's letters are the A's eye in its ring with strokes of the same black in the front plane, the signs (! and ?) with the eye half shut. The later games': a regional form keeps its species' build in its region's colours and materials; a Mega is its species grown into its Mega Evolution's shape (Mega Alakazam's five spoons float round its head, as the design has them); a Gigantamax form carries Gigantamax's red clouds over it, puffs strung on a wisp so they hang together; Pikachu wears its caps and costumes over its own sculpt, a female's heart at the tip of its tail with the costumes. A form that looks just like its species (Mothim's cloaks, the partner Pikachu and Eevee) shows its sculpt, and a model file named after a form (`Charizard-Mega-X.glb`) replaces any of them.
- **Model files** (`overrides/models`, [`docs/model-files.md`](../model-files.md)): a glTF model replaces a species' sculpt as it is, lit by the character shader with material 0 (or glow and metal where its materials say), outlined by a shell half a sprite pixel out along smoothed normals, scaled to the same size rule.
- **Clips** (`PokemonAnimation`), layered over the idle loop: *idle* (breathing, head sway, tail swish, ear flicks every few seconds, wings flapping when airborne, flames flickering); *physical* (drawn back, then a strike forward with the jaw open, arms swung through and a lunge across the field); *special* (rears up gathering power with arms and wings spread, then thrusts toward the target with a cry, without moving from its place); *status* (a hop and a nod); *hit* (recoil, squash, limbs flung out); *faint* (the legs give way, then a biped topples onto its front, a quadruped sinks onto its belly with its legs splayed, a flier drops onto its side; then it sinks into the platform); *entry* (lands with a squash out of the ball, then stands tall with a cry). A physical strike and a special move's release peak when the damage lands, 0.35 s into a 0.65 s move.

### Materials

Character and Pokémon surfaces carry a material (G6, G7), which the character shader lights differently. Highlights are one crisp band where the half vector meets the normal (`smoothstep(0.42, 0.5, …)`), never a smooth gradient.

| Material | Terminator (`smoothstep(0, w, N·L)`) | Highlight band (strength, sharpness) | Own light | Shade |
|---|---|---|---|---|
| Default | 0.12 | none | none | as lit (models without materials) |
| Skin | 0.22 | none | none | warm: sky light × `1.10, 0.96, 0.94` where unlit |
| Cloth | 0.16 | none | none | rim × 0.9 |
| Hair | 0.10 | 0.14, 10 (a soft sheen) | none | |
| Leather (shoes, bags) | 0.08 | 0.40, 36 | none | |
| Plastic (soles, brims, buttons) | 0.10 | 0.35, 48 | none | |
| Metal (buckles, clasps) | 0.06 | 0.60, 64 | none | slightly cool |
| Glow (the Rift, flames) | 0.12 | none | 0.85 | rim × 0.5 |
| Fur and feathers | 0.20 | none | none | a touch warm, rim × 1.15 |
| Scales and smooth hide | 0.12 | 0.12, 26 | none | |
| Shell (shells, claws, beaks, teeth, horns) | 0.08 | 0.36, 30 | none | rim × 0.9 |
| Leaf | 0.22 | 0.06, 8 | none | green bounce `0.94, 1.06, 0.94`, rim × 1.25 |

### Stage

- Textures are smooth, mipmapped and filtered (`SoftTextures`): meadow `114,190,100` with light `138,202,104` and deep `90,168,94` patches a few metres across; platform tops lighter in the middle (`150,220,124` → `100,182,96`) with one soft worn ring.
- Foliage is modelled (`SoftFoliage`): tiered pines with drooping rims and dark undersides; round trees made of overlapping blobs whose normals bend toward the canopy centre so the crown shades as one soft mass; seven-blade tufts, dark at the root, light at the tip. Colour comes from vertex gradients.
- Scenery takes a soft cel band (`BattleRamp` 0.85).
- Sky (`SkyPainter`): zenith `78,146,226`, middle `140,194,244` at 22 % height, hazy horizon `224,238,250` at 46 %; soft cumulus with a white crown and a cool `196,212,236` underside, drifting slowly.

### Arenas (G8)

The stage depends on where the battle starts (`ArenaSpec`, `BattleArenas`): outdoors the ground under the player picks it, and a map can name its own (data-files.md). Every arena keeps the same layout (platforms, camera) and the smooth style: vertex gradients and soft textures, modelled foliage and rock, no texture noise.

| Arena | Ground and platforms | Around the field | Light (`ArtLook.ArenaRig`) | In the air (`ArenaFx`) |
|---|---|---|---|---|
| Grass (routes, towns) | meadow, grass tops on dirt rims | the map's trees, hills; a lake if the map has one | the time of day's battle rig | fireflies at night |
| Forest | darker meadow with leaf litter, mossy tops | five rows of trees, trees closing in at both sides, bushes, ferns, fallen logs, mushrooms | battle rig × 0.8 sun, green shade and haze (fog +15 %), vignette +0.16; after dark only a dark green haze, so the Pokémon stay clear | slanting light shafts and pollen by day, fireflies at night |
| Cave | warm grey-brown rock floor, flat stone tops | rock walls all round (lumpy masses, dark at the foot, lit on top), stalagmites, boulders, blue and violet crystals that glow | fixed: torch-warm key from the upper left `0.58,0.45,0.31`, cool blue shade, dark haze (55 %), vignette 0.42 | dim warm dust |
| Water | open water to the horizon | the platforms are rocky islets with sandy tops and foam rings; islands and sea stacks far off | the time of day's battle rig | sun glints on the water |
| Snow | white-blue snowfield, packed-snow tops | pines laden with snow, drifts, capped rocks, white mountains | the battle rig with a paler overcast sky, light bounced up off the ground (ground ambient × 1.3), saturation × 0.88, contrast 1.0 | falling snow |
| Sand | rippled sand, sandstone tops | dunes, banded mesas, dry shrubs, bleached rocks | the battle rig, hotter and hazier: warm sun, warm fog, more bloom | sand blowing across |
| Indoors | floorboards, round rugs | panelled walls with daylit windows, bookshelves, potted plants | the room's light of the time of day | dust in the light |
| Gym (per type) | polished tiles in the type's tones, the type's emblem between the platforms, raised drums ringed with light | walls with banners in the type's colour, columns with lamps, the type's props (boulders, planters, pools, candles, girders, ice crystals, tesla coils, braziers, training posts) | fixed: neutral key, ambient leaning 16 % toward the type, bloom 0.32 | the type's matter (embers, bubbles, leaves, sparks, snow, wisps, motes) |
| League (Aaron, Bertha, Flint, Lucian; the Champion) | as a gym, darker and grander; the Champion's room white and gold | as a gym | as a gym, ambient × 0.78, bloom 0.45, vignette 0.38 | as a gym, more of it |

Walls, rock faces and rooms take shadows but cast none, so they never darken the platforms. Lamps, crystals and glowing rims are added light (the stage's light pass).

### Camera (G8)

`BattleCamera` directs the shots from the battle's state alone; the overview is the framing above (9° pitch, 24° field of view).

- **Intro**: the sweep in from the side (0.55 rad round and 35 % further out, easing into the overview over 1.8 s).
- **Send-out**: a closer shot of the platform a Pokémon is thrown onto, until it has come out (3.6 × its height in frame).
- **The foe's shots stand in front of the player's platform** (2.5 units or more): a foe too big to frame from there gets a wider lens (up to 40°) rather than a camera among the player's trainer and Pokémon.
- **A move**: the attacker as it winds up (3 × its height in frame; the player's Pokémon over its left shoulder with the foe beyond, the foe from the front), then a **cut** to the target 0.24 s in, just before the impact (3 × its height; two targets are framed together), back to the overview by 0.95 s. A status move on the user stays on the user.
- **Critical hit**: a cut punch-in to 2.1 × the target's height at the impact, for 0.4 s.
- **Thrown ball**: over to the trainer running in; from behind them, turning to where the ball is going and zooming in (to 15° at the narrowest, so the arc stays in frame) as it flies; a cut to the ball opening over the foe; then close on the ball on the platform (2 units in frame) while it wobbles and clicks.
- **Faint**: toward the fainting Pokémon.
- Shots ease into each other (time constant 0.18 s); only attacker-to-target and the critical punch-in cut, and any move that would carry the camera across the player's platform (from behind it to in front of it, or back) or within 3 units of it, where the trainer or the player's Pokémon would loom up in front of the lens. While the player chooses a command the camera is always on the overview. Hits shake it (more for critical and super-effective ones; Earthquake and Hyper Beam add their own).

### Move effects (G8)

- **One kit** (`BattleFx`): streams of particles from the user to the target, beams, forked bolts, bursts thrown from the impact, shockwaves (rings growing on the ground or facing the camera), auras (rings and motes rising round the user), rain from above, a contact flash, slashes, orbs, waves, glints and screen flashes. Every effect is a function of its cue's age and seed: nothing is simulated.
- **Shapes** come from one atlas (`FxTextures`, 128-px cells): glows, sparks, rings and beams are light, added on top so they bloom; flames, drops, leaves, shards, rocks, feathers, petals, hearts, notes and letters are matter, alpha-blended and back to front. All are white or grey and take their type's colour.
- **A template per type and category** (`MoveFx`): a physical move lands with a flash of contact and a burst of its type (flames, drops, sparks, leaves, shards, rocks, feathers, bubbles, wisps, petals, stars); a special move sends something across (Fire a stream of flames, Water a stream of drops, Electric a bolt, Grass spinning leaves, Ice and Dragon and Steel a beam, Fighting and Normal an orb of power, Poison sludge in an arc, Ground an eruption under the target, Flying gusts, Psychic rings, Bug a swarm, Rock thrown rocks, Ghost a shadow orb, Dark rings, Fairy sparkles); a status move wraps the user in an aura of its colour, or sends waves at the foe. 146 moves have effects of their own (Thunder's bolt from the sky, Flamethrower, Surf's wave, Hyper Beam, Swords Dance, the fangs of Bite, claws and slashes, Earthquake's shockwaves, sound waves for Growl, hearts for Charm, powders, weather).
- **Colours by type**: Fire `255,128,36`, Water `64,146,255`, Electric `255,222,50`, Grass `96,196,72`, Ice `140,218,255`, Fighting `236,110,50`, Poison `172,78,210`, Ground `206,164,92`, Flying `176,206,255`, Psychic `255,92,176`, Bug `168,204,40`, Rock `184,158,110`, Ghost `132,92,210`, Dragon `112,96,250`, Dark `110,80,120`, Steel `196,208,228`, Fairy `255,150,214`, Normal `250,246,226`; each with a light colour for cores and sparks and a dark one for matter in shade.
- **Timing**: the impact lands 0.35 s in, with the damage; effects end by 1.3 s. A miss flies wide and past and lands nothing; a move that does nothing ends in a puff of smoke; a critical hit lands bigger with a white flash and a ring; a super-effective hit lands bigger with a second ring and a harder shake.
- **Screen flashes** at most 0.5 strong and under half a second (Thunder, Hyper Beam, Fire Blast, Blizzard, critical hits, Explosion); Sunny Day's warm glow is softer and longer.
- **Marks on a Pokémon**: a stat rising sends warm streaks up round it, a stat falling cool streaks down; a heal is green motes and rings rising; a new status shows as flames (burn), purple bubbles (poison), crackling bolts (paralysis), drifting Z's (sleep) or a burst of ice (freeze).

### Poké Ball (G8)

- **A 3D model** (`BattleBall`): two hollow halves sculpted with the SDF kit, hinged at the back, with a dark band round the seam and a white button in a dark ring at the front, glossy (`Plastic`), outlined like the Pokémon. Kinds by colour: Poké Ball red over white; Great Ball blue with red patches either side; Ultra Ball black with a yellow H; Master Ball purple with two pink domes and a white M; Premier Ball white with a red band; the others in their own colours with a stripe or spots.
- **Thrown by the trainer**, overarm (the `Throw` emote lets go 0.3 s in). At a send-out the trainer throws it onto the platform and steps aside; it opens in a white burst and the Pokémon grows out of the light. Later send-outs come in from the trainer's side of the field.
- **A capture**: the player runs in at the left of their platform and throws; the ball spins end over end along a high arc, pops open over the foe and draws it in with red light, shuts, drops onto the platform with a bounce, wobbles once per successful shake check with a pause after each, and then clicks shut with three stars (it stays on the platform) or bursts open in white light as the Pokémon breaks out.

### Light, shadow and grading (day)

| Setting | Value |
|---|---|
| Sun | normalize(−0.5, 0.9, 0.5), colour 0.47, 0.43, 0.35 |
| Sky / ground ambient | 0.54, 0.60, 0.72 / 0.50, 0.50, 0.40 |
| Shadows and cloud shade | as in the field; cloud shade 16 % |
| Fog | aerial perspective toward the sky's horizon colour, 35 % at the hills (from 40 to 140 units) |
| Ambient occlusion | radius 0.6 units, strength 0.6: contact shade under Pokémon, tufts and tree crowns |
| Stage outlines | drawn where depth steps, on the near side only, 1 px wide, darkening the surface by up to 35 % (inverse depth is flat across the ground, so only silhouettes and creases fire) |
| Tilt-shift | radius 2 outside ±50 %; no wide blur |
| Bloom | threshold 0.86, strength 0.28 |
| Grade | saturation 1.05, contrast 1.05, shadows × (0.94, 0.97, 1.06), highlights × (1.04, 1.01, 0.95), vignette 0.10 |

## Time of day

The world follows the computer's clock, like the DS's real-time clock, with Platinum's five light states (pret/pokeplatinum, `src/rtc.c`). The options screen can fix a time. Each state has a light rig for the field and one for battles (`ArtLook.FieldRigFor`, `BattleRigFor`); within half an hour of a change the two rigs blend.

| Time | Hours | Field light | Field grade and extras | Battle sky (zenith → horizon) |
|---|---|---|---|---|
| Morning | 4–9 | pale gold sun, lower in the sky; soft blue shade | mist (fog 32 %), gentler contrast | `104,150,214` → peach `246,224,206` |
| Day | 10–16 | golden sun, violet-blue shade | as in the tables above | `78,146,226` → `224,238,250` |
| Twilight | 17–19 | low orange sun `0.90,0.56,0.32`, magenta-violet shade, long shadows | more bloom and saturation; lamps come on (60 %) and windows start to glow (45 %), their pools still faint | `54,62,128` → orange `252,168,108`, pink clouds |
| Night | 20–23 | cool moonlight `0.27,0.31,0.44` | saturation 0.80, vignette 0.40; street lamps, windows and glass doors fully lit, warm light pools on the ground under them, bloom from the lit glass | `8,14,38` → `40,58,100`, stars, dark clouds |
| Late night | 0–3 | dimmer moonlight | saturation 0.72, vignette 0.45; the lamps, shops and signs burn on, but most homes have gone dark | `4,8,26` → `28,42,80`, stars |

- Lit glass is marked in the art, texel by texel (see *Buildings*, "Lights after dark"): it turns to warm lamplight `255,204,117`, scaled by the texel's own brightness, by the rig's amounts for public lights and for homes.
- Night battles stay a little brighter than the night field and gain a stronger rim light (0.55), so Pokémon read clearly.
- Light never drops to black: the darkest ambient is about 0.15, and nights lean blue, never grey.

## Resolution (4K)

- The game's screen is **3840×2160**. Everything is still laid out in **1920×1080 layout units** (`GameEngine.VirtualWidth/Height`); the screen is drawn at `RenderScale = 2` real pixels per unit, so every size in this guide is in layout units unless it says otherwise.
- The interface is drawn through a ×2 camera straight into the 4K screen: text comes from large font atlases and shapes from signed distances, so both are sharp at 4K. Edges are anti-aliased over one real pixel, not one layout unit (`UiShapes.PixelScale`).
- Pixel art keeps its whole-number scales: a field texel is about 6 real pixels, a party icon texel is 8.
- The window shows the 4K screen scaled to fit. Full screen on a 4K display shows it 1:1; a smaller window downsamples it, which also anti-aliases it. The first run picks the largest listed window size the monitor can show.
- The full-screen window is one row taller than the display (`WindowSettings.FullscreenSize`). A borderless OpenGL window that matches the monitor exactly is taken over by the graphics driver as exclusive full screen and flickers on NVIDIA cards.

## Quality presets

| Preset | 3D scene resolution | Anti-aliasing | Shadow taps | Shadow map | Ambient occlusion | Wide depth of field |
|---|---|---|---|---|---|---|
| High (default) | 3840×2160 | FXAA in a window wider than 2880; in a smaller one the downscale to the window does it | 9 | 2048 | yes | yes |
| Medium | 2880×1620 | FXAA in a window wider than 2160 | 4 | 2048 | yes | yes |
| Low | 1920×1080 | FXAA in a window wider than 1440 | 1 | 1024 | no | no |

The interface is always drawn at 4K; the presets change only the 3D scenes. FXAA is for a window that shows the scene at more than three quarters of its size: a smaller window smooths the scene by scaling it down, and FXAA would cost half a millisecond for nothing. Options are saved in `settings.json`.

A frame must stay under 8 ms on High. In the harness's 1080p window after G11 (plan 04): towns and routes 5.0 to 6.6 ms, a room 3.6 ms, battles 6.0 to 7.5 ms with a move's effect at its height; Medium takes 0.4 to 1.1 ms less and Low 1 to 2 ms less. One millisecond of each of those is the hidden window's own swap of its buffers (three in a window as large as a 4K display, where FXAA adds 0.4 more), and readings move by two or three tenths between runs. The harness's `profile` mode says where a frame goes; what was learned from it:

- A frame is spent filling 4K, not on geometry: a few hundred thousand triangles and a hundred or two meshes cost a tenth of what shading every pixel does. An effect is weighed by what it adds per pixel.
- A 4K texture is slow to read at scattered places. What needs the picture or its depth small (the wide blur, the glow, ambient occlusion) reads a half-resolution copy made once a frame.
- A scene seen from low down (a battle's stage) lays its depth down before it is shaded, so each pixel is shaded once.

## Interface (vector)

### Typography

- **Nunito** (SIL Open Font License, see `CREDITS.md`) in three weights: Bold for running text, ExtraBold for labels and dialogue, Black for names, numbers and buttons.
- Scale (layout units): 18–20 captions and tags · 24–26 small labels · 30–32 numbers and move names · 36–42 names, prompts and dialogue · 52 screen titles · 60 the FIGHT button.
- Tracking +0.8 up to 20 px, +0.4 up to 39 px, 0 above. Level reads as a small muted "Lv" before a large number.
- No drop shadows on text over panels; white text on coloured buttons gets a 2 px shadow at 20 % black.

### Colour tokens (`ModernUi`)

| Token | Value | Use |
|---|---|---|
| Ink | `36,44,68` | text |
| Muted | `110,120,148` | secondary labels |
| Frame | `52,64,96` | panel borders |
| Panel | white → `234,240,248` | panel fill (top to bottom) |
| Shadow | `14,22,46` at 31 % | drop shadows |
| Red / Gold / Green / Blue | `232,72,76` / `240,176,56` / `56,182,104` / `70,136,232` | FIGHT / BAG / POKÉMON / RUN, and accents |
| Track | `62,72,98` | bar backgrounds |
| HP | `70,214,110` > 50 %, `246,196,50` > 20 %, `240,72,64` | HP fill |
| EXP | `72,176,250` | EXP bar |
| Selection | `240,104,70` | selected card border and glow |
| Type colours | `Palette.GetTypeColor` | type pills, move bands |
| Party backdrop | `44,112,146` → `24,60,98`, 28° stripes of 5 % white | Pokémon menu |

### Shapes and components

- Everything is drawn by `UiShapes` (signed-distance rounded rectangles anti-aliased over one real pixel, vertical gradients, borders, slanted sides and blurred shadows; rings, round-ended lines and triangles for icons and arrows; `Glow`, a soft ellipse of light that fades from its middle to nothing at its rim). No raylib rounded rectangles, triangles or lines in the kit: their edges are jagged.
- The components live in `ModernUi` (`Panel`, `Card`, `Button`, `HpBar`, `Bar`, `ExpBar`, `TypePill`, `StatusPill`, `Tag`, `Portrait`, `Hints`, `Prompt`, `Badge`, `Dim`); a screen composes these and adds no drawing of its own beyond text.
- Panels: radius 22–34, 4 px Frame border, shadow blur 22 at offset (0, 8).
- Cards (anything that can be chosen: party slots, option rows, title entries): a panel whose selected state is a 6 px Selection border and a glow; gold instead while a Pokémon is being moved. An empty slot is a faint white outline.
- Pills: types in the type's colour, statuses as three letters (PSN, BRN, PAR, SLP, FRZ, FNT) in the status colour, short tags ("IN BATTLE") in a token colour. Gender is a vector mark after the name, blue ♂ or pink ♀.
- Icons (`UiIcons`): our own one-colour signs built from the same shapes, white on a coloured chip; on a selected row the chip turns white and the sign takes the colour.
- Key hints: a white key cap and a label on a translucent pill, right-aligned at the top of a screen (on a Frame-coloured pill when they sit on a light panel).
- Prompts: a question over the dimmed screen, a muted line of explanation, and a row of buttons; the safe answer comes first and is the one highlighted.
- Buttons: pills; gradient 18 % lighter at the top to 12 % darker at the bottom, border 35 % darker, a soft shine over the top 38 %. Selected: a 6 px white ring and a glow in the button's colour.
- HP boxes: slanted sides (skew ±0.2), name + gender left, level right, HP bar under; the player's box adds HP numbers and an EXP line.
- HP bar: pill with an amber "HP" tag 2.3× its height.
- Dialogue: wide panel near the bottom, speaker in a red pill tag on its top edge (so the line itself doesn't repeat the name), a bobbing red arrow when the line is complete.
- Layout grid: 1920×1080 layout units (drawn at 4K), 48–64 margins, 24–32 gutters. Battle: opponent box top-left, player box right above the commands, prompt bottom-left, a large FIGHT button with BAG, POKÉMON and RUN stacked beside it.

### Field menus and notices

- **Start menu** (`StartMenu`): a panel 440 wide on the right that slides in. Platinum's order (Pokédex, Pokémon, Bag, the player's name, Save, Options), then CLOSE, then QUIT GAME under a rule. Each row has its icon on a chip; the selected row is a Selection-coloured pill with white text.
- **Leaving the game**: QUIT GAME asks "Quit the game?" with KEEP PLAYING (highlighted), SAVE AND QUIT and QUIT. The title menu has a QUIT entry too, with no question because nothing can be lost there.
- **Location sign** (`LocationSign`): on arriving outdoors (entering a town or route, stepping out of a building) a slanted plate with the place's name drops in at the top left, holds 2.4 s and lifts away. Its colour bar says what kind of place it is: green for routes, blue for lakes, gold for towns and cities. Rooms have no sign.
- **Notices** (`Toast`): one line in white on a dark pill at the top centre ("Game saved."), 2.6 s; a notice that replaces another does not slide in again.

### Battle panels

- **Switching**: the field dims and the team appears as six cards, three to a row, under a one-line question. Each card has the icon, name, level, HP bar and numbers, and a tag: IN BATTLE, a status, or FNT (the card is greyed). The player's HP box is hidden behind the cards.
- **Bag**: the same shape as the move menu: four item cards (pixel icon, name, ×count; greyed at ×0) and a panel describing the chosen one. Item icons are 20×20 pixel art shown at 3×: the ball itself, a spray bottle whose colour says which medicine, a crystal for a Revive.
- **Trainer's team**: a dark tray hangs under the opponent's box with a ball for each Pokémon (grey once fainted) and a dot for each empty place.
- A message that doesn't fit on one line wraps to two.

### Summary

One Pokémon on three panels: on the left its number, level, the 128-px sprite at 3× on a pale disc, name, category, type pills, nature and experience (points, to next level, bar); top right the six stats, each with its value and a bar whose length and colour follow the species' base stat (red under 50, amber under 80, green under 110, teal above; HP shows current / max in the HP colours); bottom right the moves as rows with type pill, name, category, power, accuracy and PP. Up and down step through the team.

### Menu screens (G10)

Every full-screen menu is the same frame: the backdrop, its title at the top left, key hints at the top right, and its content between rows 132 and 1032 with 64 margins and 32 gutters. Its parts rise or slide into place over 0.25–0.45 s when it opens.

Shared parts (`ModernUi`, in `ModernUi.Lists.cs`):

- **List rows** (`ListRow`): 84 tall and 8 apart, radius 26, on a panel. What the row is about sits at its left on a pale disc (an item's icon at 3×, a Pokémon's at 1×), its name in Black 34, and one value right-aligned (a count "×5", a price). The chosen row is a Selection-coloured pill with white text, as in the start menu; the others have no fill. A row that can't be chosen now is Muted.
- **Scrolling** (`UiNav.Window`, `ScrollBar`): a list shows whole rows only. The cursor keeps two rows of context ahead of it where there are any, and wraps from the last row to the first. A thin bar at the panel's right edge (Rule-coloured track, Frame-coloured thumb) shows where the window is, and only when there is more than fits.
- **Tabs** (`Tabs`): pills in a row across the screen under the title, each with its sign on a chip in its own colour and its name in Black 22. The chosen tab is filled with its colour, its chip white. Left and right change tab and wrap round.
- **Detail panel**: the thing chosen, large, on a pale disc (an item's icon at 6×, a Pokémon's sprite at 3×), its name in Black 44–56, tags, a rule, and its text in Bold 30 on 42 leading.
- **Money** (`Money`): our own mark, a P crossed by two bars, before the number in Black. Amounts on panels and in lists use it, never "$" or "¥".
- **Stepper** (`Stepper`): a number on a Frame-coloured pill between two arrows; left and right change it by one, up and down by ten, and it wraps between the least and the most.
- **Keyboard** (`Keyboard`, its cursor and rules in `NameEntry`): keys as small cards, 96 by 84 and 10 apart, in four rows of ten: the alphabet with a full stop, a hyphen, an apostrophe and a space, then the digits. The chosen key is a Selection-coloured pill. A fifth row has three wide keys: upper or lower case, delete, and OK. The keys turn to lower case by themselves after a name's first capital; the B button deletes, the Start button jumps to OK, and the cursor waits on OK once the name is full. Names are entered with the cursor and never typed, because the game's own keys are letters.

The screens:

- **Bag** (`BagScreen`): eight tabs, Platinum's pockets in its order (Items, Medicine, Poké Balls, TMs & HMs, Berries, Mail, Battle Items, Key Items), each in a colour of its own (gold, pink, red, violet, green, sky blue, orange, purple) and each remembering its own cursor. Under them the pocket's list on the left (eight rows) and the chosen item on the right: icon, name, its pocket as a tag, how many are in the bag, and its text; a TM or HM is described by its move, with the move's type, category, power, accuracy and PP under it. The A button opens what can be done with the item on a small panel at the foot of the detail panel (USE, GIVE, CANCEL, one row each, like the start menu's rows). Using or giving goes on to the party cards, which say ABLE or NOT ABLE for something used.
- **Item icons** (`PixelArtGenerator.ItemIcon`): 20 by 20 pixel art, shown at 3× in rows and 6× on the detail disc. A kind has a shape: the ball itself, a spray bottle for medicine (its colour says which), a crystal for a Revive, a wrapped sweet for a Rare Candy, a flat disc in its move's type colour for a TM (a pale rim for an HM), a berry, an envelope, a key, a tonic bottle, a cut gem in its own colour for an evolution stone, a pouch for anything else.
- **Shop** (`ShopScreen`): the stock on the left (nine rows, each with its price; a price the player can't pay is Red), the player's money on a card at the top right, the chosen item under it with how many are already in the bag. Buying asks how many on a panel over the dimmed screen: the item, a stepper from 1 to as many as the money buys (99 at most), and the total.
- **Pokédex** (`PokedexScreen`, plan 03 · D10): the list on the left, the entry on the right.
  - **The list**: the open Pokédex's name on a tag (SINNOH in Blue, NATIONAL in Red) beside its SEEN and CAUGHT counts, which count that Pokédex's species only. A row has the number in that Pokédex, the name (dashes until it has been seen), the menu icon, and a small ball for a species that has been caught. Left and right jump ten.
  - **The entry**: three pages chosen with tabs across the panel's top, INFO (Blue), AREA (Green) and SIZE (Gold). The A button moves into the entry: left and right then change page, up and down go to the previous or next species seen, and B goes back to the list, which shows INFO again.
  - **INFO**: a species that has been caught shows everything: its sprite at 3× on a disc, number, name, category, types, height, weight and its entry. One only seen shows its sprite, name and types, with question marks for the rest. One not yet seen is a question mark on the disc.
  - **AREA**: the map of Sinnoh, one square per chunk of the overworld (water a pale blue, land the Disc colour, towns white), with the places the species lives lit in the Selection colour, breathing gently. Beside the map, its places in the order of the region, each with how it is met under its name: grass at which times of day, surfing, and with which rods. A species found nowhere says "Area unknown." Swarms, the Poké Radar and the species a second game in the console calls up are left out, as Platinum leaves them out.
  - **SIZE**: the species and the player as two dark silhouettes (black at 84 %) standing on one line at the heights the Pokédex gives (the player is 1.4 m), the taller of the two 520 tall. They are the one exception to whole-number scales: flat shapes scaled to whatever their heights ask for. Only for species caught, whose height the Pokédex knows.
  - **Search** (the Start button): a panel over the dimmed Pokédex with rows like the options' (a value on a pill between two arrows): which Pokédex (once the National one is open), the order (number, A to Z, heaviest, lightest, tallest, smallest), the first letter (A B C, D E F … Y Z), two types and a body shape (the Pokédex's fourteen), then SEARCH (Green) and RESET (Blue), and DIPLOMA (Gold) once one has been given. The results replace the list, with how many were found on a tag; B goes back to the whole list. As in Platinum it looks only among the species seen, and the orders by weight and height only among those caught; those orders show the weight or the height at each row's right.
  - **Diploma**: a cream certificate with a gold double rule over the dimmed screen: DIPLOMA, which Pokédex, the player's name, a line on what was done (in our own words), the day, and a gold seal. It is shown the first time the Pokédex is opened once it is complete, and again from the search panel.
- **Trainer Card** (`TrainerCardScreen`): one wide card that rises into place. A Blue band across its top carries TRAINER CARD and the ID number; under it the name, money, Pokédex, time played and the day the adventure began on the left, the player's own field sprite at 6× on a pale plate on the right, and along the bottom the eight badges at radius 44 with their names.
- **PC boxes** (`PCScreen`): three columns. The party as six compact cards on the left; the box in the middle, 6 by 5 slots under its name between two arrows, each Pokémon as its menu icon at 2×; on the right the Pokémon under the cursor (sprite at 2×, name, level, types, HP, moves). The cursor moves from the party into the box and back, and up from the box's top row to its name, where left and right change box.
- **Starter choice** (`StarterSelectScreen`): the three partners as three cards side by side, each with its sprite at 3× on a disc, name, category and type; the chosen one hops. A panel under them has the chosen one's entry. Choosing asks first ("Choose Turtwig?"), with going back as the first answer.
- **Saving** (`SaveScreen`): over the dimmed field, the save as it will be (the same summary the title's CONTINUE card shows: place, player, time played, Pokédex, badges, party) with the question under it. SAVE is the first answer and the one highlighted, since nothing is lost by saving. Afterwards the panel says so for a moment and goes.
- **Options** (`OptionsScreen`): one card per setting, 98 tall on a pitch of 110, with its value on a pill between two arrows; a panel under them, filling what is left of the screen, explains the chosen one. The first row is the text speed (slow, normal, fast: 24, 45 and 120 characters a second, whatever the frame rate).

### The new-game introduction (`IntroScreen`)

The games' opening talk, in our own words and with our own pictures: Professor Rowan in front of a soft backdrop, speaking in the dialogue panel.

1. Black, then the backdrop fades up: deep teal to night blue with slow drifting discs of light, and Rowan (his 3D model, as battles show a trainer, breathing and blinking) in the middle.
2. He welcomes the player and explains the world. Then he steps aside, a Poké Ball (the 3D one of the battles) comes up and opens in white light, and a Pokémon grows out of the light, hops and stands beside him: Buneary, as in Platinum (the 3D model, as in battle).
3. **Who are you?** Two cards, a boy and a girl (the two player characters as 3D models); the chosen one stands a little forward and waves. Choosing asks whether that is right ("So you're a girl?").
4. **Your name?** The name-entry screen: the character's field sprite at 6×, the name so far as large letters on a row of seven slots, and the keyboard. Seven letters at most, as in Platinum; an empty name takes the default (Lucas or Dawn).
5. He repeats the name and asks whether it is right. In both of his questions YES comes first and going back second: nothing is lost either way, and yes is what is nearly always meant.
6. The send-off: the picture closes to black round the player's own field sprite, which shrinks away in whole steps on a small pool of light (`UiShapes.Glow`), and the field fades in.

Any line can be hurried with the A button; nothing can be skipped, because each step sets something.

Whoever the player isn't is the professor's assistant, with the other default name. Written lines and place names never name the player outright: they say `{player}` and `{assistant}`, which `PlayerIdentity.Fill` replaces when they are shown.

### Pokémon in menus

- Always **2D pixel sprites** baked from the models (`PokemonSprites`): 48-px icons and 128-px front/back sprites with a one-texel outline, the eyes and markings baked in with them.
- Shown at whole-number scales only (party icons at 4×), point-filtered.
- Icons hop like the main games: the selected one 3 sprite-pixels every 0.16 s, the others 1 sprite-pixel every 0.4 s; fainted ones stay still.

### Badges

- Eight round medallions in gym order (`ModernUi.Badge`), each in its gym's colour with a simple white mark; our own designs, not the games' badge art.
- Not yet won: a flat grey-blue disc with a darker rim, same size, so the row always shows eight.

### Opening and title screen (`TitleScreen`, `TitleScene`)

The order follows the games' opening: a notice, a short film, the legendary Pokémon with the title, "press start", then the menu. Any button skips to the title.

1. **Notice** (3.4 s): white text on black saying this is a fan-made project with original art.
2. **Journey** (3 × 3.6 s): slow pans over the field itself at different times of day (Twinleaf Town in the morning, Route 201 by day, Sandgem Town at night with its windows lit), between cinema bars 120 units tall, dipping to black between shots. They are rendered by the field renderer, so they always match the game.
3. **Reveal** (3.2 s): fade up from black on the void, a white flash, then Giratina and the lettering.
4. **Title**: Giratina (the 3D model, as in battle) hovers in a dark violet void that deepens to crimson, among drifting stones and rising motes. It stays a **shadow** against the glow behind it, picked out by its ink lines; it is never fully lit. Lettering: "Pokémon" in cream over "PLATINUM" in a white-to-silver gradient (`UiFonts.Display`, Nunito Black) with a band of light sweeping across every 5.5 s, a thin rule and "A FAN REMAKE". It is our own lettering, not the games' logo. A pulsing "PRESS Z OR ENTER" sits under Giratina, and a one-line disclaimer at the bottom edge.
5. **Menu**: the lettering shrinks to the top left, Giratina slides left and cards slide in on the right: CONTINUE (only with a save), NEW GAME, OPTIONS, QUIT. Standard cards.
   - **CONTINUE** shows the save at a glance: where it was saved (right of the heading), PLAYER, TIME PLAYED (hours:minutes), POKÉDEX (caught), BADGES n / 8 with the eight medallions, and the PARTY as 2D icons.
   - **NEW GAME** with a save present asks first ("Start a new game?"), with No selected by default.
6. **Leaving**: 0.8 s fade to black, then the field fades in.

### Motion (`UiMotion`, `UiReveal`)

- Things arrive with an ease-out over 0.2–0.35 s and leave faster with an ease-in (0.1–0.15 s): the start menu and prompts slide, the party's cards rise one after another, the summary's panels slide in from the sides, HP boxes slide in with the Pokémon.
- Bars drain instead of jumping. Selection moves instantly and glows. Nothing bounces except Pokémon icons and the advance arrow.
- Cursors in grids (`UiNav.Grid`) wrap along rows and columns and skip empty slots.

## Areas (targets)

Implemented so far: Twinleaf, Sandgem, Jubilife, Routes 201–202, Lake Verity, Pallet (Kanto's stand-in), and every battle arena (G8: grass and lakeside, forest, cave, water, snow, sand, indoors, a hall per gym type and the League's rooms). Later sessions extend these families; each new area gets a line here.

| Area family | Ground and foliage | Mood |
|---|---|---|
| Towns and routes (Twinleaf, Sandgem, Routes 201–202) | spring greens above, sand paths, teal/red/blue roofs | bright, warm |
| Lakes (Verity, Valor, Acuity) | the same greens, pale stone shores, clear blue water | calm, cooler shade |
| Forests (Eterna) | deeper greens, dark trunks, dappled light | green-tinted shade, stronger vignette |
| Caves and mines | warm browns, cool blue shadows | torch-warm highlights |
| Snow (Route 216–217, Snowpoint) | white-blue ground, dark pines | low sun, cold grading |
| Coast (Pastoria, Sunyshore) | warm sand, turquoise water | high sun, saturated |
| Cities (Jubilife, Veilstone) | pale paving, glass and steel | neutral, crisp |
| Distortion World | desaturated violets and greys | flat, eerie light |

Caves, buildings and the Distortion World will need their own rigs that ignore the clock, as rooms do today.

## Do and don't

**Do**
- Draw pixel art with placed clusters, one-texel rims and two or three flat shades.
- Keep every pixel-art element on the 32-texel grid and at whole-number scales.
- Use the tokens and components above for every new screen.
- Show before/after boards from the harness for every visual change.

**Don't**
- Don't add random per-texel variation, dithering or "grain" to any texture.
- Don't draw 3D renders of Pokémon in menus, or pixel sprites of Pokémon in battle.
- Don't scale pixel art by fractions or filter it bilinearly.
- Don't use near-black outlines, pure black shadows or flat, unshaded UI boxes.
- Don't copy art, models, textures, fonts or UI from the Pokémon games.

## Assets policy (confirmed in G1)

- Procedural generation stays the backbone: it scales to 1025 species and every town.
- Free-licensed assets are welcome where they beat code (OFL fonts, CC0 textures, skies, props), each recorded in `docs/art/CREDITS.md`, and only downloaded with the user's OK.
- An `overrides/models/` folder (ignored by git) lets glTF models replace any species' model (plan 03 · D5, [`docs/model-files.md`](../model-files.md)); `overrides/sprites/` does the same for character sprite frames. What is put there stays on the machine it is put on and is not part of the game's assets.
- Nothing taken from the Pokémon games and no fan rips.

## Known gaps after G11

- The interface is drawn a shape at a time: one or two tenths of a millisecond in the field, 1.2 ms for a double battle's menu. Drawing its shapes in batches would halve that; no scene needs it to stay inside the frame's budget. Battle models are drawn at one level of detail (their triangles are 0.3 ms of a frame), and scenery is batched per chunk rather than instanced, for the same reason: neither is where a frame goes.
- Relief (plan 01 · M3) is drawn from the maps' heights, but no area open so far has any: it is seen on the harness's terrain lab until the hills past Jubilife City open. Raised ground does not shade the ground behind it yet: only its faces cast shadows.
- The field still needs light rigs of its own for snow and caves when their areas are built; their battle arenas are ready (G8).
- Every town of Sinnoh has its buildings and landmarks (plan 01 · M4), each standing where the original's model does; they are seen in the game as plan 01 opens each area, and in the harness's `cities` mode until then. Thirty-three models have plain stand-ins until their area is built (`docs/world-models.md` says which). Jubilife City is still the hand-made map in the game itself until plan 01 · M5.
- A cross gable would give the bigger houses their look.
- A puddle has no look of its own yet (it is drawn as the ground round it), so nothing mirrors a walker; puddles and their reflections come with the first place that has them, Route 212 (plan 01 · M7). Only the player's steps leave prints, dust and leaves: other people don't walk about yet (plan 02).
- The weather has its eleven looks, but the five places whose weather follows Platinum's calendar keep one weather until the calendar is imported with them (plan 01 · M7 and M8), and the moods Platinum gives some forests, caves and halls through the same setting are not built. Weather is silent until plan 05, and a battle doesn't yet begin in the field's weather (plan 06 · R3).
- Rooms are furnished only as far as the maps place furniture; there are no lamps to see, though the light changes at night. Trees are the only field art still built from smooth 3D shapes under a pixel texture.
- The menu screens show what the rules behind them can do so far. The bag uses medicine and evolution items and gives things to hold; tossing, registering and what every other item does come with plan 06 · R11, as do each shop's own stock and selling. The PC shows the stored Pokémon thirty to a box in the order they were stored; slots of their own, moving, box names and wallpapers come with R12. The Pokédex has no cry page until the cries of plan 05 · A4 and no forms page yet, though the forms are in the data (plan 03 · D11); the National Pokédex is opened by Professor Rowan in the post-game (plan 02), and the diploma is shown by the Pokédex itself until Jubilife City's Game Freak building is built (plan 01 · M5). The options have no volume settings until plan 05.
- Money inside a sentence (a battle's prize) is still written with "$": the mark of the panels is drawn, not typed, so the text renderer has to learn to place it.
- The introduction names only the player. Naming the rival, its last question in Platinum, comes with the story (plan 02 · S4), which has the same name entry to ask with.
- Of Platinum's ways into a battle, five are built (a wild Pokémon and a trainer, each also when stronger than the player's first, and a Gym Leader). It also tells water and caves apart and has effects of their own for the Elite Four, the Champion, Team Galactic and the legendary Pokémon: those come with their places.
- 249 species have hand-built version 2 models: the whole of Platinum's Sinnoh Pokédex, the 24 the story showed first and the rest of it (plan 03 · D6–D9), and Kanto's first 39 from outside it, Bulbasaur to Vileplume (plan 03, decision 3). Every other species has a generated one (plan 03 · D5), which reads as its kind of creature but rarely as the species itself. Model files in `overrides/models` replace any of them. The forms of generated species are generated too; the 113 forms of hand-built species are hand-built (Platinum's own 40 and the later games' 73), but for Mothim's cloaks and the partner Pikachu and Eevee, which look just like their species and show its sculpt.
- Characters have mitten hands and no fingers. Of the emotes only the trainer's start and the battle throw are played in the game so far; scripts (plan 02) will call the others.
- The title keeps Giratina in shadow by design; since G7 its model would hold up fully lit if that is ever wanted. The title music is a placeholder melody until plan 05.
- The opening's journey shots are only as good as the maps they fly over; choose new shots as the regions are rebuilt (plan 01, G4–G5).
