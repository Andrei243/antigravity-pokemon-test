# Plan 04 · Graphics overhaul

**Goal**: make the game look polished and professionally made: one coherent art style from the overworld to battles and menus, with Platinum's charm and the finish of a modern 3D release. Every other plan builds its content on top of this, so start here.

## Why it looks amateurish today

Findings from the current screenshots:

- **No single style.** Pixel-art pieces (the 128 px Pokémon sprites, grass tufts, flower beds, speckled ground) sit next to smooth cel-shaded 3D and flat UI boxes. The low-resolution Pokémon look pasted onto a high-resolution scene.
- **Noise instead of texture.** Grass, paths and the battle meadow are covered in single-pixel speckles that read as TV static; path edges are jagged pixel fringes; tree canopies are noisy balls.
- **Models built from visible primitives.** Characters and Pokémon are spheres, cylinders and cones pushed into each other: hard seams, tiny painted faces, stick arms, helmet-like hair.
- **Flat light.** No ambient occlusion, a flat ambient term, near-black outlines on everything, no bloom, colour grading or atmospheric depth; the battle sky is a gradient with flat 2D ellipse clouds.
- **Generic UI.** Pale rounded boxes, one plain font, big empty areas (the battle message box takes a quarter of the screen for one line), almost no icons, no motion, nothing of Platinum's identity.
- **No life.** Grass doesn't move; no footsteps, dust, ripples or weather; plain fades between scenes.

## Direction (decided in G1)

**Chosen: "Sinnoh Diorama", a mix of the two candidates** (user's choice on 2026-10-01, after comparing both on the same frames):

- **Overworld: HD-2D.** Pixel-art ground, buildings and characters in the lit 3D diorama, with strong tilt-shift depth of field, bloom and warm grading. Characters are pixel sprites baked from the 3D rigs.
- **Battles: modern 3D.** Real 3D Pokémon and trainers on a modelled stage, soft cel shading with coloured outlines, smooth filtered textures, modelled foliage, painted sky.
- **Interface: modern vector UI** in Platinum's colour language (Nunito, anti-aliased panels), with Pokémon shown as **2D pixel sprites** in menus, like the main games (no 3D renders in menus).

The rules and numbers are in [`docs/art/style-guide.md`](../art/style-guide.md), with the numbers in code in `ArtLook`. The candidates were:

*Modern 3D Sinnoh.* The overworld as a chibi diorama, full 3D Pokémon in battle, soft cel shading, clean low-frequency textures, bloom and grading, and a crisp HD interface. (Its battle and interface were kept.)

*HD-2D.* Pixel-art characters and Pokémon as sprites in a 3D diorama with pixel-textured terrain, depth of field and bloom. (Its overworld was kept; its battles and pixel-framed interface were not.)

## Asset policy (confirmed in G1)

- Procedural generation stays the backbone: it scales to 1025 species and every town.
- Free-licensed assets are welcome where they beat code: OFL fonts (Google Fonts), CC0 textures and skies (Poly Haven, ambientCG), CC0 props and nature pieces (Kenney, Quaternius, KayKit) through a glTF import path. Record each one in `docs/art/CREDITS.md`, and get the user's OK before downloading.
- An override folder, `overrides/models/` next to the game (ignored by git): a glTF model named after a species, character or building replaces the procedural one, so hand-made Blender models can be dropped in at any time.
- No assets taken from the Pokémon games, and no fan rips.

## Architecture

Each item names the layer it serves: **field** (HD-2D), **battle** (3D) or **UI**.

- **Materials as data** (battle): base colour or texture, shading ramp, outline colour and width, rim, specular, emission; one set per material kind (skin, cloth, hair, foliage, wood, stone, metal, glass, water). The field's materials are pixel-art textures on the 32-texel grid.
- **Lighting** (both): hemisphere ambient, tinted soft shadows (more PCF taps, normal-offset bias), per-area light rigs and palettes for Platinum's five times of day; ramp-based cel shading with soft band edges in battle, smooth Lambert light in the field (the pixel art carries the shading).
- **Post chain** (both, settings per layer in `ArtLook`): scene target → half-resolution blur and bloom chains (done in G1) → colour grading → tilt-shift depth of field (done in G1) → FXAA for the battle. Ambient occlusion for the battle. Revisit 2× supersampling once FXAA is in.
- **Outlines** (battle): inverted hulls for characters and Pokémon (tinted, done in G1), screen-space edge detection for the stage. Field sprites carry a one-texel outline in their pixel art.
- **Sky and atmosphere**: a sky dome or painted sky with lit clouds for the battle (painted sky done in G1); distance fog in the sky's colour for both.
- **Wind and instancing** (both): one wind field sways grass, flowers and trees; grass, flowers and trees are GPU-instanced.
- **Character sprites** (field): 3D rigs baked into pixel sprites per facing and walk frame, drawn as upright lit billboards that cast shadows (done in G1, `CharacterSprites`); hand-drawn overrides can replace any frame.
- **SDF modelling kit** (`SdfModel`, battle and sprite sources): spheres, ellipsoids, capsules, rounded boxes, cones and tori combined with smooth unions and cuts, each carrying a colour; meshed with marching cubes into smooth normals and blended vertex colours; decals for eyes, markings and patterns; meshes cached on disk. It replaces the primitive assembly in `Graphics/CharacterModels.cs` and `Graphics/PokemonModels.cs` (the existing `PokeBuilder` shapes translate almost one-to-one) and is the base of plan 03's model generator. The same models feed the 3D battles and the baked sprites (field characters, menu Pokémon).
- **Skeletal animation** (battle): skinned meshes (raylib 5.5's GPU skinning, or CPU skinning), animation clips written in code (keyframes plus procedural motion), blending between clips.
- **UI kit** (UI): fonts (Nunito, done in G1; SDF fonts later), design tokens, components drawn with `UiShapes` (panel, button, list, tab, bar, dialogue box, icon), vector-drawn icons, tweens for motion. Pokémon appear as 2D pixel sprites.
- **Look-dev tools**: harness modes for model turntables, a UI gallery and before/after pairs (`look` mode, done in G1); shader hot-reload while tuning.

## Sessions

Every session starts by capturing "before" screenshots with the harness and ends with before/after pairs for the user to approve.

### G1 · Direction, references and style guide
- Capture the current look; confirm the direction and the asset policy with the user.
- Collect reference pictures (the user can supply some; keep them out of the repo).
- Write the style guide: palettes per area and time of day, shading ramps, outline rules, texture rules (no single-pixel noise, texel density), proportions for characters and Pokémon, typography scale, UI colours and components, dos and don'ts.
- Prototype the target on three frames (Twinleaf, a battle, the party screen): palette, lighting and font changes, enough for the user to judge the direction before the big work starts.
- **Done when** the user approves the style guide and the prototype frames.

**Outcome (2026-10-01).** Both candidate looks were prototyped on the same frames and compared side by side; the user chose the mix described under *Direction* and approved Nunito and the asset policy. What exists now:
- `ArtLook` switched the field, battle and interface layers between the old and new look (the switch and the old look were removed in G2).
- Post chain with half-resolution depth-of-field and bloom targets and per-layer grading (`RenderContext.PreparePost`).
- Field: `PixelGround` (clean pixel-art ground; the old ground was ~30 % single-texel specks, the new one ~0.6 %, checked by `StyleGuideTests`) and `CharacterSprites` (rigs baked to outlined pixel sprites, drawn as shadow-casting billboards).
- Battle: Pokémon drawn as lit 3D models on the platforms with tinted outlines (`BattleRenderer.DrawPokemon3D`), `SoftFoliage` trees and tufts, `SoftTextures` meadow and platforms, `SkyPainter` sky.
- Interface: `UI/Kit` (Nunito loader, `UiShapes` SDF panels) and `ModernUi` (battle HUD main/move/message states, party screen with 2D pixel sprites, dialogue box).
- Harness `look` mode: reference frames in the old and new look, plus before/after boards.
- Not yet covered: see *Known gaps* in the style guide.

### G2 · Rendering foundation
Per the style guide's layers. Field: soft shadow filtering, fog, the five time-of-day light rigs and grades (confirm Platinum's hour boundaries from pret/pokeplatinum), pixel-perfect sprite and texture placement. Battle: material system, ambient occlusion, screen-space outlines for the stage, FXAA, the time-of-day rigs. Both: a graphics settings screen (quality presets, resolution, full screen, vsync). Remove the `Current` paths this session replaces. Tune on Twinleaf, Route 201 and the grass battle.
**Done when** the before/after pairs show a clear step up and a frame stays under 8 ms.

**Outcome (2026-10-01).**
- **Time of day**: `GameClock` follows the computer's clock with Platinum's table from the decompilation (`src/rtc.c`: late night 0–3, morning 4–9, day 10–16, twilight 17–19, night 20–23). `ArtLook` holds a light rig per time for the field and for battles (sun or moon, ambient, fog, grading, sky, window glow) and blends them within half an hour of each change. Rooms keep their own light.
- **Night**: glass in windows, doors and the Mart sign lights up (`MeshPass.Glow`), and warm light pools on the ground in front (`MeshPass.Light`); battles get a night sky with stars.
- **Shadows, fog, clouds**: rotated Poisson-disc soft shadows with an early-out, distance fog in every lit shader, slow cloud shade over the sunlight.
- **Depth-based passes**: the scene target now has a depth texture; quarter-resolution ambient occlusion (field and battle), depth-step outlines for the battle stage, FXAA for the single-sample presets.
- **Pixel-perfect field**: the camera target and sprites snap to the 1/32-unit texel grid.
- **Options screen** (start menu → OPTIONS): quality (Low/Medium/High), window size, full screen (borderless, also F11), V-Sync, time of day, sound; saved in `settings.json` (`GameSettings`, `WindowSettings`). Esc no longer closes the game.
- **Old look removed**: `ArtDirection` and its branches are gone (old ground painter, 2D battle sprites, old HUD, old dialogue and party list). The battle's switch and bag panels and the summary are the only old-style panels left on those screens.
- **Harness**: `look [before dir]` (reference frames plus before/after boards against an earlier run) and `times` (five times of day, three presets).
- **Frame time on High**: fields 5.7–6.9 ms (Route 201 is the heaviest), battles 6.3–7 ms; Medium about 4 ms, Low about 3 ms.
- **Deferred**: the material system (specular, emission per surface kind) needs models that carry material ids, so it moves to G6 and G7 with the SDF kit. Street lamps come with the props in G5.

### Extra · 4K and the title screen (2026-10-01, asked for by the user between G2 and G3)

- **4K**: the screen is now 3840×2160. Layout stays in 1920×1080 units (`VirtualWidth/Height`) and is drawn at `RenderScale = 2` through a ×2 camera, so no screen had to be re-laid out. Fonts use larger atlases, `UiShapes` anti-aliases over one real pixel, and the High preset renders the 3D scenes at 3840×2160 (Medium 2880×1620, Low 1920×1080). FXAA on High runs only when the window shows the screen about 1:1; in a smaller window the downscale already anti-aliases, which keeps the frame in budget. Window sizes go up to 3840×2160, and the first run picks the largest size the monitor can show.
- **Full-screen flicker fixed**: raylib's borderless mode makes the window exactly the monitor's size; on the user's NVIDIA card the driver then treats it as an exclusive full-screen game (its overlay hooked in) and the picture flickered. `WindowSettings` now does its own borderless full screen with a window one row taller than the display, which stays composited. The window state and rendered frames were verified steady; the flicker itself can only be judged on the display.
- **Title screen** (part of G10, done early): `GameState.Title` is the starting state. `TitleScreen` runs the opening (notice → three fly-over shots of the field → Giratina's reveal → title → menu) and `TitleScene` renders Giratina in the void. The menu offers CONTINUE (with the save's location, player, time played, Pokédex count, badges and party), NEW GAME (asks first when a save exists) and OPTIONS. `GameEngine.StartNewGame()` and `ContinueGame()` enter the game; play time only counts once a game is running. See the style guide's "Opening and title screen".
- **Harness**: `title` mode (opening, title, menus with and without a save); `SHOTS_4K=1` saves full 4K shots; `ShotCrop` saves native-resolution crops.
- **Frame time on High at 4K**: field 6.7 ms, battle 7.6 ms, title 3.9 ms.
- **Left for later**: the new-game introduction with Professor Rowan (G10 with plan 02), real title music (plan 05), a fully lit Giratina once the version 2 models exist (G7).

### G3 · UI kit and core screens
Grow `UI/Kit` and `ModernUi` into the kit: tokens, components and motion from the style guide; then rebuild the start menu, the battle's switch and bag panels, the summary (2D sprites), and polish the dialogue box, battle HUD (animated bars), party screen and options screen. Add the location-name sign shown when entering an area.
**Done when** those screens use only kit components and pass review in the harness `menus`, `battle` and `look` modes.

**Outcome (2026-10-01).**
- **Kit**: `UiShapes` gained anti-aliased rings, round-ended lines and triangles (no raylib triangles or lines are left in these screens); `UiIcons` draws the menu icons, gender marks and arrows from them; `UiMotion`/`UiReveal` hold the easing and slide-in timing and `UiNav.Grid` the cursor rules, all free of GPU calls and tested. `ModernUi` is split into parts (shared components, battle, party and summary, field) and now has `Card`, `TypePill`, `StatusPill`, `Tag`, `Portrait`, `Bar`, `ExpBar`, `Hints`, `Prompt` and `Dim`.
- **Start menu** rebuilt: icon chips, sliding panel, the player's name on the Trainer Card entry, CLOSE, and **QUIT GAME** with a prompt offering to save first. The title menu has QUIT as well. `GameEngine.QuitRequested` ends the main loop, so a full-screen game can be closed from inside.
- **Battle**: the switch panel (six cards over the dimmed field, grid cursor that skips empty slots) and the bag panel (pixel item icons, description) replace the last old-style panels; a trainer's team shows as balls on a tray under the foe's box; long messages wrap to two lines; status pills and gender marks are kit components.
- **Summary** rebuilt with the 2D sprite, stat bars, experience and move rows; up and down step through the team. The party cursor now moves over the two columns, and the cards rise into place.
- **Field**: the location sign on arriving outdoors, notices as a dark pill (the old toast box is gone), dialogue lines no longer repeat the speaker's name.
- **Options and title** screens draw with the same cards, hints, arrows and prompt.
- **Also fixed**: `MapDatabase.Initialize` replaced its dictionary in place, which made tests that run in parallel fail now and then; it now swaps in a complete new set.
- **Frame time on High**: field 6.7 ms, battle 7.1–7.8 ms, title 4.0 ms.
- **Left for G10**: bag, Pokédex, trainer card, shop, PC and starter choice still use the old `RenderHelper` panels.

### G4 · Terrain and nature
Field, in pixel art on the 32-texel grid: sand, dirt, snow and cave ground for `PixelGround`; bevelled ledges and cliffs; tall grass and flowers that sway and part around the player; trees with clean pixel-art bark and leaf textures (no noise); rocks; the water (pixel-art depth bands, shoreline foam, sparkles). Battle: pines and round trees per arena, rocks, water edges in the smooth style.
**Done when** Twinleaf, Route 201 and Lake Verity look finished.

**Outcome (2026-10-02).**
- **Water** is now pixel art in the ground plane. `PixelGround` bakes a mask with each water texel's distance to the shore (a chamfer distance transform); the water shader colours it per texel: three depth bands, foam lapping at the shore, drifting wave marks, sparkles by day, darker and greyer after dark (this fixes the night glow). Shores are rounded, north shores show a bank, and the old sunken rectangle with straight walls is gone.
- **Tall grass** is two staggered rows of clean clumps per tile (four flat shades) instead of a noisy blade texture; it sways, and **leans away from anyone walking through** (`FieldShaders.SetWalkers`, up to eight walkers). Lawn tufts and flowers are single camera-facing cards on the texel grid; none of them cast shadows any more.
- **Trees**: needles, leaves and bark redrawn in four flat tones without per-texel noise (`NatureArt`, tested).
- **Ledges** are 12 texels high with a bright edge, a scalloped grass lip and a dirt face.
- **Rocks**: `PropType.Boulder`, solid, placed in the map files of Twinleaf, Route 201 and Lake Verity; in water they stand in a ring of foam.
- **Ground kinds**: `TileType.Sand`, `Dirt`, `Snow` and `CaveFloor` (map codes `,` `;` `^` `c`) with palettes, rims and marks; no map uses them yet.
- **Battle**: boulders round the meadow, and maps with a lake (`Map.HasLake`) get a lake behind the opponent, drawn with a smooth water shader (`BattleRenderer.SetArena`).
- **Harness**: `terrain [before dir]` renders all of this with native crops and before/after boards.
- **Frame time on High**: Route 201 7.0–7.3 ms (was 7.5), Lake Verity 6.9–7.1 ms, the lakeside battle 7.5 ms.
- **Not done here**: cliffs (they need height in the map data, plan 01), and the shapes of ponds and lakes, which are rectangles in the maps.

### G5 · Buildings, props and interiors
A modular building kit in pixel art with a style per town: planks and plaster with one-texel bevels, framed windows with glass, doors with steps, roof tiles with ridges, overhangs and gutters, chimneys, foundations. Props: fences, signs, lamps, mailboxes, benches, flower boxes. Interiors with warm light, wood and tile floors, rugs and furniture. No per-texel noise anywhere.
**Done when** every existing building and interior is rebuilt.

**Outcome (2026-10-02).**
- **The kit**: buildings, props and furniture are boxes and upright cards whose every visible face is painted at its exact size, 32 texels per tile, into one sheet of art per map (`ArtSheet`, `KitBuilder`). The painters are plain code without GPU calls (`BuildingArt`, `OutdoorProps`, `PropModels`, `GroundBaker`, with `Pix` for bevels, outlines and a 5×7 sign alphabet), so tests build every map's kit and check that no face has specks or gradients. The old noisy textures and flat-coloured boxes are gone.
- **Buildings**: each front wall is one painted façade composed bay by bay from the map (door, name plate, windows), in the wall material of its style: planks, clapboard, plaster, brick or panel over a stone base. Pitched roofs now run their ridge east–west, so the tiled south slope faces the camera; Centers, Marts and the lab have hip roofs and an entrance block with their sign (our own ball roundel, `MART`, `LAB`); city blocks have two or three storeys, a flat roof behind a parapet and equipment on top.
- **A style per town**: maps say how their houses are built (`architecture`: Timber for Twinleaf, Plaster for Sandgem, City for Jubilife, Clapboard for Pallet) and can name what a building is where its door doesn't tell (`buildings`). Jubilife's school, Pokétch Company, TV station, Global Terminal and flats were log cabins before; each now has its own look.
- **Lights after dark** are marked texel by texel in the art instead of being guessed from colour. Street lamps, wall lanterns, shops and signs come on at twilight and burn all night; the windows of homes are lit in the evening and dark late at night, except in about one house in three. Lamps throw a pool of light and wear a halo.
- **Street furniture**, placed through the map files: fenced front gardens with a mailbox at the gate, lamp posts along the roads, planters at public doors, benches, and a shore railing in Pallet. Fences are real posts and rails that join up tile by tile; the rest, and now boulders and signposts too, are outlined pixel sprites that cast real shadows.
- **Rooms**: painted walls (crown moulding, wallpaper, chair rail, panelled wainscot, skirting), clean plank and tile floors, and every piece of furniture rebuilt with painted faces. Rooms follow the clock: daylight on the floor by day, orange glass at twilight, dark blue glass and warmer, dimmer light at night.
- **Upright things stand upright** (a change to the field's look, decided here): the steep camera made everything tall lean outward by up to 20° at the sides of the screen, which sheared sprites and made lamp posts look as if they were falling over. Outdoors the vertex shader now takes that lean out; walls and sprites are undistorted rectangles and the ground keeps its perspective. Rooms keep true perspective.
- **Performance**: a forest in view cost over 2 ms, because every layer of leaves was shaded. Trees are now kept in chunks of eight tiles, only those near the view are drawn, and they lay down their depth before they are shaded. Jubilife went from 8.1 ms (over budget before this session) to 6.7 ms.
- **Harness**: `buildings [before dir]` renders every building from the street by day, at twilight and at night, and every room, with close-ups and boards; `SHOTS_FILTER=text` saves only matching shots.
- **Frame time on High**: Twinleaf 6.4 ms, Jubilife 6.7 ms, rooms 4.2 ms, the reference battle 7.5 ms.
- **For plan 01 · M4**: a new kind of building is a `BuildingKind`, a line in `BuildingArt.StyleOf` and, if it needs one, a sign; a new town style is an `Architecture` value and its line there. Gyms, gates and the League have no style yet.
- **Not done here**: second storeys and cross gables for the bigger houses; doors that open (G9); lamps you can see inside rooms.

### G6 · SDF kit and characters
`SdfModel` with meshing and caching, and the material data deferred from G2 (per-surface ramp, specular and emission, carried as material ids on the models). Rebuild the player and every NPC type: better proportions, hands and shoes, sculpted hair, clothing detail, expressive faces (eyes with highlights, blinking, a few mouth shapes and expressions). Skinned skeletons with walk, run, idle and emote animations with follow-through (hair and bag bounce). The models serve both layers: 3D trainers in battle, and the source of the field sprites (re-bake, with more walk frames and legible faces at sprite size; hand-drawn overrides where a bake falls short).
**Done when** the lineup, sprite sheet and walk-cycle shots pass review.

**Outcome (2026-10-02).**
- **The SDF kit**: `SdfModel` sculpts with signed-distance shapes (spheres, ellipsoids, round cones, round boxes, tori, rounded cylinders) that join smoothly, cut, or paint colour and material onto what is already there. Each shape carries a colour, a `SurfaceMaterial` and the bone it moves with, and all three blend across the joins along with the surface. `SdfMesher` meshes the field by surface nets in a narrow band round the surface (on every core), pulls the vertices onto the surface and takes the normals from the field's gradient; characters use cells of 1/112 unit, 34,000 to 42,000 vertices each. `SdfCache` keeps the meshes in `cache/models` next to the game, named by a hash of the model's description, so a model is meshed again only after it changes (a few seconds per character, once) and is read back in about 50 ms. The game starts building every character type the maps use in the background while it loads (`CharacterModels.Preload`).
- **Materials** (deferred from G2): every vertex carries a material id (skin, cloth, hair, leather, plastic, metal, glow), and the character shader takes from a table per material the width of the shade's edge, a crisp highlight band, light of its own and the tint of the shade (style guide, "Materials"). Material 0 is the old look, so the Pokémon are unchanged until G7.
- **Skeletons**: a 19-bone `Skeleton` per person (hips, spine, head, arms, legs, and one each for the hair, the bag and the skirt or coat tails), with weights carried by the shapes, skinned on the GPU (`SkinnedModel`). The raylib the game ships with binds the bone attributes but never uploads them, so `SkinnedModel` does (CLAUDE.md, rendering rules).
- **Every character rebuilt**: the player, the rival, Rowan, the nurse, mom, lady, clerk, youngster, lass, clown, Looker, gentleman and the generic trainer, and the starter briefcase and the rift with the same kit. Chibi proportions in two builds (children and adults), mitten hands with a thumb, shoes with soles, cuffs, collars and scarves, painted stripes, skirts and coat tails that swing, the player's backpack; hair sculpted in locks (short, long, spiky, swept back) under a beret, a cap or the nurse's cap. The normals of face and hair are softened after meshing, so the shade's edge runs cleanly across them instead of splitting the face or breaking up over every lock.
- **Faces, two ways**: in battle a smooth texture over the front of the head (rimmed eyes with two highlights, lids, lashes, brows, a small mouth) in five expressions, each with a blink; on sprites a pixel face stamped onto each frame (two eyes with a highlight, a mouth; one eye in profile), because a smooth face shrunk to 40×58 turns to mush. Decided here: no attempt to bake the smooth face down.
- **Animation** (`CharacterAnimation`): idle breathing with the arms lowered from the sculpted A-pose; walk and run in eight frames (the hips drop as the legs spread, so a foot stays down; arms swing against the legs; the run leans in with bent elbows); the ledge hop; four emotes (wave, surprised, nod, cheer). Hair, bag and skirt follow a beat behind the body. A trainer who spots the player now starts under the "!".
- **Battle**: trainers are the same models in 3D with their smooth faces. Outlines are an offset shell of the SDF (meshed on its own, skinned with the body): the pushed-out hull poked ink through every crease (hat brims, between locks).
- **Field sprites** are re-baked from the models at 40×58 texels when first needed: per facing 2 idle, 8 walk, 8 run, 3 hop and 6 frames per emote, with blinks and expressions as frames of their own. A PNG in `overrides/sprites/<TYPE>/` next to the game replaces any frame (names in the style guide); no frame needed one yet.
- **Harness**: `lineup` adds the lineup in focus (`36*`), a turntable of every character (`37_turntable_*`), sprite sheets of each strip (`38_sheet_*`) and the faces in every expression (`39_faces_*`).
- **Tests**: 50 new cases in `SdfKitTests`, `CharacterKitTests` and `CharacterAnimationTests` (the kit's distances, blends, paint and watertight meshes, the cache; every character one skinned body with sound weights, hats that show, hair lit as one mass; frames, the walk, follow-through, emotes, faces, overrides, materials, skinning). 329 tests pass.
- **Frame time: not measured.** This session ran in a container with software OpenGL, where any frame takes 270–360 ms. Nothing changed per frame in the field (characters are still lit billboards); a battle draws two skinned models of about 40,000 vertices, each with its shell. Run `ShotHarness <dir> times` on the real machine before G7 to check the budget.
- **Not done here**: fingers (hands stay mittens); the ball throw (G8); the game plays only the trainer's start emote so far (plan 02's scripts call the others); the trainer card's portrait (G10).

### G7 · Pokémon models, version 2
Rebuild the existing species with the SDF kit plus eye and marking decals; skeletons per body plan (biped, quadruped, bird, serpent, fish, floating); animations for idle, physical and special attacks, hit, faint and entry. Battles already draw the Pokémon as 3D models (G1); menu icons, summary and Pokédex pictures stay 2D pixel sprites baked from the same models.
**Done when** the contact sheet and battle shots pass review. Plan 03's generator (D5) and hand-built batches (D6–D9) then use this kit.

**Outcome (2026-10-02).**
- **Built with the SDF kit**: `PokeBuilder` keeps the old builder's calls (ellipsoids, limbs, spikes, rings), so the 23 species and the generic stand-in moved over almost line for line, each now one smooth body: shapes join with small blends, spikes can be flattened into blades and rings into ovals (`SdfPrimitive.Stretch`, new in the kit), and colour patches that shouldn't change the shape (bellies, masks, bands, a flame's heart) are paint with a soft edge. Models are meshed at 128 cells across their largest dimension (17,000–47,000 vertices) with an outline shell half a sprite pixel out, and cached on disk; the game meshes them in the background at start-up (`PokemonModels.Preload`) and bakes the menu sprites from them.
- **Materials**: four new kinds for Pokémon (fur and feathers, scales and smooth hide, shell for claws, beaks and horns, leaves); gold and steel are metal, flames glow.
- **Body plans and skeletons**: biped, quadruped, bird, serpent, fish and floating. Every plan has a root at the feet and a body bone; legs hang from the root, so they stay planted while the body breathes; head, jaw, arms, wings, tail, ears, leaves, flames, fins and a serpent's segments have bones of their own, and each bone has a role the clips move it by. The 23 species are bipeds, quadrupeds and birds, with Giratina floating; the serpent, fish and floating plans also have sample models, which tests and the harness use and plan 03 · D5 can build on.
- **Decals**: eyes and markings (nostrils, mouths, chest dots, the stars in Shinx's ears, Garchomp's star) are projected onto the mesh and painted into an atlas once per eye state: open, shut, squeezed and fierce. A blink, a hit and an attack change the eyes.
- **Clips** (`PokemonAnimation`): idle, physical, special, status, hit, faint and entry, layered over the idle loop and moving bones by role, so one clip serves a whole body plan. `BattleAnimator.Attack` now takes the move's category: a physical move lunges across the field, a special one is cast from its place. Timings changed with them: a move takes 0.65 s, so a strike and a release peak when the damage lands (0.35 s in); a send-out 0.8 s; a faint 1.0 s, the collapse and then the sink into the platform.
- **Harness**: the `pokemon` mode renders turntables of every model (`91_*`), a board of every clip for one species of each plan (`92_*`) and the eye states up close (`93_*`); `sheets` shows the menu sprites.
- **Tests**: 56 new cases (`PokemonModelTests` and the attack's category in battle): bodies, weights and feet; every bone carrying surface; decals covering their squares; each plan's skeleton; planted feet; strikes, casts, hits and faints moving the right way; moves starting and ending at rest; the eyes; stretched shapes. 385 tests pass.
- **Frame time: not measured**, as in G6 (software OpenGL here). A battle now draws two skinned models of up to 47,000 vertices with their shells and decals; the first start meshes the 24 models once (about a second each here), later starts read them from the cache.
- **Not done here**: the other species (plan 03 · D5–D9 build them with this kit); move effects, the trainer's throw and camera choreography (G8). The title keeps Giratina in shadow by design; the new model would now hold up lit if that is ever wanted.

### G8 · Battle presentation
Arenas per environment (grass, forest, cave, water, snow, sand, indoors, each gym, the League rooms) and time of day; camera choreography (intro sweep, send-out close-up, following the attacker and cutting to the target, a zoom on critical hits); a 3D effects system (particles, trails, beams, shockwaves, screen flashes) with a template per type and overrides per move; a 3D Poké Ball thrown by the trainer.
**Done when** a scripted demo battle in the harness looks right.

### G9 · Life
Wind, footprints and dust, water ripples and splashes, rustling grass, rain, snow and fog, day and night, doors that open, emote bubbles, eased camera moves, and battle intro transitions (swirls, shutters, shatter effects) that carry the player from the pixel diorama into the 3D battle.

### G10 · Remaining screens
Bag, Pokédex (2D sprites, per the style guide), trainer card, shop, PC boxes, starter choice, the new-game introduction, save and load, options; all in the vector UI kit. (The title screen and the continue panel were built early; see "Extra" after G2.)

### G11 · Performance and final pass
Profiling, instancing, levels of detail, caches, settings presets, and a last before/after review of every harness shot.

## Risks

- **Procedural limits**: some species will still look off; the override folder lets a hand-made model replace any of them.
- **Cost of effects**: 2× supersampling plus ambient occlusion and bloom is heavy; measure each addition and keep presets for slower machines. After G2 on High: fields 5.7–6.9 ms, battles 6.3–7 ms per frame in the harness, against a budget of 8; little headroom is left, so new effects must pay for themselves or go to Medium/Low as well.
- **Two techniques, one game**: the pixel field and the 3D battles could feel like two games. Keep palettes, light direction and the interface shared, and make the battle intro transition (G9) sell the change.
- **Endless polish**: every session has a done-when and a user review; move on once it passes.
- **raylib limits**: OpenGL 3.3 without compute shaders, so everything runs as vertex and fragment passes.

## Needs and gives

- **Needs** nothing first: start with G1.
- **Gives** every other plan its look. Build plan 01's regions (M4 onwards) after G4–G5, plan 03's models (D5–D9) after G7, plan 02's cutscenes after G6.

## Status

- [x] G1 Direction, references and style guide (2026-10-01: Sinnoh Diorama; see the style guide)
- [x] G2 Rendering foundation (2026-10-01: time of day, soft shadows, fog, ambient occlusion, outlines, FXAA, options screen; materials moved to G6/G7)
- [x] Extra: 4K rendering, full-screen fix, opening and title screen (2026-10-01)
- [x] G3 UI kit and core screens (2026-10-01: kit components and motion, start menu with quit, battle switch and bag panels, summary, location sign, notices)
- [x] G4 Terrain and nature (2026-10-02: pixel-art water, tall grass that parts, clean tree art, ledges, boulders, four new ground kinds, lakeside battles)
- [x] G5 Buildings, props and interiors (2026-10-02: painted façades and roofs in a style per town, lights after dark, street furniture, rebuilt rooms that follow the clock, upright things straightened, forests drawn depth-first)
- [x] G6 SDF kit and characters (2026-10-02: SDF kit with meshing and a disk cache, surface materials, skinned skeletons, every character rebuilt with faces and expressions, 8-frame walk and run, emotes with follow-through, re-baked sprites with pixel faces; frame time still to measure on real hardware)
- [x] G7 Pokémon models, version 2 (2026-10-02: the 23 species rebuilt with the SDF kit, body-plan skeletons, eye and marking decals, materials, idle/physical/special/status/hit/faint/entry clips, re-baked menu sprites; frame time still to measure on real hardware)
- [ ] G8 Battle presentation
- [ ] G9 Life
- [ ] G10 Remaining screens (title screen and continue panel done early, 2026-10-01)
- [ ] G11 Performance and final pass
