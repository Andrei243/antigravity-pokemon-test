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

`dotnet run --project tools/ShotHarness -- <dir> look [before dir]` renders the reference frames by day (Twinleaf, dialogue, battle, move menu, party, Route 201, Player's house, Lake Verity, options). Given the folder of an earlier run, it also writes `compare_*.png` before/after boards. `times` renders Twinleaf, Sandgem and a battle at each time of day, then each quality preset. Graphics sessions start with a `look` run (the "before") and end with the boards.

## Rules for every layer

- **No single-pixel noise.** Texture detail comes from placed shapes and clusters, never from per-texel random values. The field ground is tested: under 1.5 % of its texels may differ from all eight neighbours (`StyleGuideTests`). The old ground was about 30 %.
- **Light from the upper left**, sun slightly toward the camera, so faces and fronts are lit and shadows fall to the upper right.
- **Shade with colour, not black.** Shadows lean violet-blue, highlights lean warm; outlines are a darker shade of the surface they surround.
- **One pixel size per layer.** Pixel art is shown at one texel density and whole-number scales only, with point filtering; smooth art is mipmapped and filtered. Never mix the two inside one layer.
- **Readable at a glance.** Silhouettes before detail: if a detail doesn't read from the game camera, leave it out.

## Overworld (HD-2D)

### Pixel grid

- **32 texels per world unit** (one tile) for everything: ground (`GroundBaker.ArtTile`), buildings and props (`SceneTextures`), character sprites (`CharacterSprites.TexelsPerUnit`). With the field camera that is about 3 layout units per texel, 6 real pixels at 4K.
- Point filtering everywhere; no mipmaps on pixel art.
- Upright things (walls, sprites, signs) are stretched by `VS = 1 / cos(pitch)` so they read at true proportions from Platinum's steep camera (pitch 59.05°, FOV 16.18°).

### Ground

Baked per map by `PixelGround`: soft tile masks thresholded into hard pixel edges, then placed details.

| Element | Colour | Notes |
|---|---|---|
| Lawn | `104,190,98` | flat base |
| Lawn patches | `120,200,102` | clean-edged blobs a few tiles wide, one step lighter |
| Tuft marks | `70,150,82`, tip `160,222,122` | three-blade "v", 2–3 per tile on a jittered grid |
| Forest floor | `58,126,82` | under the tree margin |
| Tall-grass ground | `40,112,66` | |
| Path | `222,204,160` | rounded corners, never pixel fringes |
| Path rim / inner light | `166,140,104` / `236,222,186` | one texel each; the light line on the north and west sides |
| Pebbles | `168,160,150`, light `208,202,192`, shadow `214,186,132` | 2×2 with a highlight texel, at most one cluster per tile |
| Pond stones / sea sand | `168,160,150` / `238,224,172` | band around water |
| Contact shade by walls | two flat steps, 14 % and 28 % toward `30,70,50` | never a smooth gradient |

### Buildings and props

- Existing pixel-art textures at 32 texels per unit. G5 redraws them in the same rules: bevels and trims drawn as one-texel light and dark lines, two or three shades per material, no random per-texel variation.
- Roofs keep Platinum's colours per town (Twinleaf: teal `52,166,138`).

### Characters

- Built as 3D rigs (`CharacterModels`) and **baked to sprites** (`CharacterSprites`): 40×58 texels, orthographic, seen from 24° above, studio light from the upper left, then a one-texel outline tinted from the neighbouring colour (`PixelCanvas.OutlinePass`).
- Frames: 4 facings × idle + 4 walk frames (+4 run frames). Drawn as upright cards lit by the scene (sky × 0.62 + sun × 0.78), casting real shadows, with a soft contact blob under the feet.
- Proportions: chibi, about 1.2 tiles tall, head about 40 % of the height, big eyes; the player reads at about 130 px tall on screen.
- G6 improves the source rigs (SDF kit); the sprites improve with them. Hand-drawn sprite overrides may replace any baked frame.

### Light, shadow and grading (day)

| Setting | Value |
|---|---|
| Sun direction | normalize(−0.6, 0.8, 0.42) |
| Sun colour | 0.66, 0.55, 0.40 (golden) |
| Sky / ground ambient | 0.50, 0.55, 0.74 / 0.46, 0.44, 0.40 |
| Diffuse | smooth Lambert (the pixel art carries the shading) |
| Shadows | soft: Poisson-disc filter 1.8 shadow-map texels wide, rotated per pixel; 9 taps on High, with a 5-tap early-out where a pixel is fully lit or fully shaded |
| Cloud shade | 20 % dimmer sunlight in broad patches that drift slowly across the map |
| Fog | toward `0.72, 0.82, 0.92`, 18 % at the far edge (starts 44 units from the camera, full at 72) |
| Ambient occlusion | screen-space, quarter resolution, radius 0.45 units, strength 0.4: grounds walls, props and sprites |
| Tilt-shift | 12-tap blur radius 6 texels, plus 95 % mix toward a wide blur outside a ±30 % focus band |
| Bloom | threshold 0.86, strength 0.38 |
| Grade | saturation 1.10, contrast 1.08, shadows × (0.88, 0.92, 1.10), highlights × (1.07, 1.00, 0.90) |
| Vignette | 0.26 |
| Rooms | their own warm lamp light at every hour; tilt-shift 4, depth of field 60 %, focus ±36 %, bloom threshold 0.93 at 0.22 |

The camera target and every sprite snap to the texel grid (1/32 unit), so pixel art scrolls in whole texels and doesn't shimmer. The other times of day are under *Time of day* below.

## Battles (3D)

### Models

- Pokémon and trainers are real 3D models on the platforms (no pixelation), lit and shadowed with the stage.
- Size rule: a Pokémon covers the part of the screen its 128-px sprite frame would, i.e. the frame spans the platform's width ÷ 158 px (opponent) or ÷ 150 px (player).
- Shading: two-tone cel ramp with a soft terminator (`smoothstep(0, 0.12, N·L)`), hemisphere ambient × 1.08, rim light 0.30.
- Outlines: inverted hulls about half a sprite pixel thick (`WorldPerPixel × 0.55`), colour = surface mixed 50 % toward `40,30,56`.
- Flashes (send-out, hit, recall, the dark silhouette of a wild Pokémon before the camera settles) are colour blends in the shader, not extra sprites.
- G7 replaces the primitive-built models with the SDF kit; the menu sprites are re-baked from those models.

### Stage

- Textures are smooth, mipmapped and filtered (`SoftTextures`): meadow `114,190,100` with light `138,202,104` and deep `90,168,94` patches a few metres across; platform tops lighter in the middle (`150,220,124` → `100,182,96`) with one soft worn ring.
- Foliage is modelled (`SoftFoliage`): tiered pines with drooping rims and dark undersides; round trees made of overlapping blobs whose normals bend toward the canopy centre so the crown shades as one soft mass; seven-blade tufts, dark at the root, light at the tip. Colour comes from vertex gradients.
- Scenery takes a soft cel band (`BattleRamp` 0.85).
- Sky (`SkyPainter`): zenith `78,146,226`, middle `140,194,244` at 22 % height, hazy horizon `224,238,250` at 46 %; soft cumulus with a white crown and a cool `196,212,236` underside, drifting slowly.

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
| Twilight | 17–19 | low orange sun `0.90,0.56,0.32`, magenta-violet shade, long shadows | more bloom and saturation; windows start to glow (45 %) | `54,62,128` → orange `252,168,108`, pink clouds |
| Night | 20–23 | cool moonlight `0.27,0.31,0.44` | saturation 0.80, vignette 0.40; windows and glass doors fully lit, warm light pools on the ground in front of them, bloom from the lit glass | `8,14,38` → `40,58,100`, stars, dark clouds |
| Late night | 0–3 | dimmer moonlight | saturation 0.72, vignette 0.45; fewer lights (60 %) | `4,8,26` → `28,42,80`, stars |

- Lit glass is found by colour: bluish texels of glazed textures (windows, glass doors, the Mart sign) turn to warm lamplight `255,204,117` by the rig's glow amount.
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
| High (default) | 3840×2160 | FXAA when the window shows the screen about 1:1 (wider than 2880); otherwise the downscale to the window does it | 9 | 2048 | yes | yes |
| Medium | 2880×1620 | FXAA | 7 | 2048 | yes | yes |
| Low | 1920×1080 | FXAA | 5 | 1024 | no | no |

The interface is always drawn at 4K; the presets change only the 3D scenes. A frame must stay under 8 ms on High (in the harness after the move to 4K: field 6.7 ms, battle 7.6 ms, title 3.9 ms). Options are saved in `settings.json`.

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

- Everything is drawn by `UiShapes` (signed-distance rounded rectangles anti-aliased over one real pixel, vertical gradients, borders, slanted sides and blurred shadows). No raylib rounded rectangles in the new UI.
- Panels: radius 22–34, 4 px Frame border, shadow blur 22 at offset (0, 8).
- Buttons: pills; gradient 18 % lighter at the top to 12 % darker at the bottom, border 35 % darker, a soft shine over the top 38 %. Selected: a 6 px white ring and a glow in the button's colour.
- HP boxes: slanted sides (skew ±0.2), name + gender left, level right, HP bar under; the player's box adds HP numbers and an EXP line.
- HP bar: pill with an amber "HP" tag 2.3× its height.
- Dialogue: wide panel near the bottom, speaker in a red pill tag on its top edge, a bobbing red arrow when the line is complete.
- Layout grid: 1920×1080 layout units (drawn at 4K), 48–64 margins, 24–32 gutters. Battle: opponent box top-left, player box right above the commands, prompt bottom-left, a large FIGHT button with BAG, POKÉMON and RUN stacked beside it.

### Pokémon in menus

- Always **2D pixel sprites** baked from the models (`PokemonSprites`): 48-px icons and 128-px front/back sprites with a one-texel outline.
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
5. **Menu**: the lettering shrinks to the top left, Giratina slides left and cards slide in on the right: CONTINUE (only with a save), NEW GAME, OPTIONS. Standard panels; the selected card has the Selection border and glow.
   - **CONTINUE** shows the save at a glance: where it was saved (right of the heading), PLAYER, TIME PLAYED (hours:minutes), POKÉDEX (caught), BADGES n / 8 with the eight medallions, and the PARTY as 2D icons.
   - **NEW GAME** with a save present asks first ("Start a new game?"), with No selected by default.
6. **Leaving**: 0.8 s fade to black, then the field fades in.

### Motion (targets for G3)

Boxes slide in with an ease-out over 0.2–0.35 s; bars drain instead of jumping; selection moves instantly and glows; nothing bounces except Pokémon icons and the advance arrow.

## Areas (targets)

Implemented so far: Twinleaf, Sandgem, Routes 201–202, Lake Verity and the grass battle stage. Later sessions extend these families; each new area gets a line here.

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
- An `overrides/models/` folder (ignored by git) will let hand-made glTF models, and later hand-drawn sprites, replace procedural ones.
- Nothing taken from the Pokémon games and no fan rips.

## Known gaps after G2

- Building, prop and interior textures are still the older pixel art with light per-texel variation (G5).
- Field water still uses the older water texture and shader, and reads too bright at night (G4).
- Start menu, bag, Pokédex, summary, shop, PC and the battle's switch/bag panels keep their old layouts with the new font (G3, G10).
- The jump from the pixel field to the 3D battle needs its intro transition (G9).
- Materials (specular, emission per surface kind) wait for models that carry material ids: the SDF kit in G6 and G7.
- Street lamps and other light sources besides windows come with the props in G5; rooms don't change with the hour yet.
- The title keeps Giratina in shadow partly because the version 1 model does not hold up fully lit; revisit the lighting with the version 2 models (G7). The title music is a placeholder melody until plan 05.
- The opening's journey shots are only as good as the maps they fly over; choose new shots as the regions are rebuilt (plan 01, G4–G5). The new-game introduction (Professor Rowan) is still to come (G10, plan 02).
