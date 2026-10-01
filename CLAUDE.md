# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A fan remake of Pokémon Platinum in C# (.NET 9) on Raylib-cs 8.0, built toward the full game over many sessions. The roadmap lives in `docs/plans/` (start with `docs/plans/README.md`); the art rules live in `docs/art/style-guide.md`. Read the relevant plan before continuing roadmap work, and tick its status checklist and record decisions in it when a session ends.

The git repository is this folder. The parent folder has the same name and is not a repository; sessions often start there.

## Commands

Run from this folder.

```bash
dotnet build PokemonPlatinum.sln
dotnet test PokemonPlatinumTests
dotnet test PokemonPlatinumTests --filter "FullyQualifiedName~TestCatchRateFormula"   # one test
dotnet run --project PokemonPlatinumEngine                                             # play
dotnet run --project PokemonPlatinumEngine -- --region Sinnoh                         # new games start in Sinnoh instead of Kanto
dotnet run --project tools/DataImporter                                               # regenerate species/moves/abilities/items JSON
dotnet run --project tools/ShotHarness -- <out dir> [all|field|lineup|battle|flow|menus|look|title|terrain|times|sheets] [before dir]
dotnet run --project tools/MusicRender -- <out dir> [song id or folder ...] [--night] [--stems]                    # songs to WAV, with level and clash checks
```

- `tools/DataImporter` is in the solution; `tools/ShotHarness` and `tools/MusicRender` are not, so `dotnet build PokemonPlatinum.sln` doesn't compile them. Build them too after renaming or removing engine members.
- The game reads and writes `savegame.json` and `settings.json` in the working directory. The harness makes `<out dir>` its working directory, so it never touches a real save.
- No linter or formatter is configured.
- This machine has no Python and no `gh` CLI. Git Bash heredocs containing apostrophes or non-ASCII text tend to fail; write such content with the Write tool.

## Checking visual work

Claude can't see the game window. `tools/ShotHarness` runs the engine in a hidden 1920×1080 window, drives it into known states and saves PNGs of the virtual screen (halved to 1920×1080; set `SHOTS_4K=1` for the full 3840×2160, or use `ShotCrop` for a native-resolution crop). Write its output to a scratch directory, open the PNGs with the Read tool before calling graphics work done, and show the user before/after pairs. It also prints frame timings (the budget is 8 ms per frame).

The harness sets `GameEngine`'s private fields by reflection (`currentMap`, `player`, `currentState`, `battle`, `battleRenderer`, `virtualScreen`, `dialogue`, `titleScreen`, `locationSign`, the menu screens, `playerParty`, `playerInventory`, `playerPokedex`), so renaming those means updating `tools/ShotHarness/Program.cs`. It can't see `internal` types: `InternalsVisibleTo` covers only the test project. The engine starts on the title screen, so the harness calls `StartNewGame()` first; only its `title` mode goes back there.

The hidden window never shows the screen 1:1 or in full screen. For window problems (full screen, resizing, DPI), write a small program in the scratchpad that mirrors `Program.Main` for a few seconds and logs `GetScreenWidth/Height`, focus and window flags each frame; a full-screen window will appear on the user's display while it runs, so say so first and keep the runs few.

## Architecture

### Game loop and states

`Program.cs` opens the window and calls `GameEngine.Update(dt)` then `GameEngine.Draw()` each frame. `GameEngine` (Core) owns everything: the current `Map` and `Player`, the party, bag, Pokédex and PC storage, the active `BattleEngine`, one instance of each menu screen, and a `GameState` enum that decides which of them updates and draws. State changes that swap scenes go through `StartTransition(next, onMidpoint)`: fade out, run the callback (load the map, create the battle), fade in.

The game starts in `GameState.Title`: `TitleScreen` plays the opening and shows the menu, and `StartNewGame()` / `ContinueGame()` load the game and fade into the field. Nothing in `currentMap`, `player` or the party is valid before one of them has run.

Everything draws into the `virtualScreen` render texture, which is 3840×2160 and is letterboxed into the window. UI code lays out in 1920×1080 units (`VirtualWidth`, `VirtualHeight`); `Draw` wraps the 2D pass in a ×2 camera (`RenderScale`), so drawing code never multiplies by the scale itself. Sizes that must be one real pixel (anti-aliased edges) divide by `UiShapes.PixelScale`.

### Logic stays free of the GPU

Tests create `BattleEngine`, `Map`, `Pokemon` and the databases without a window, and drive battles through `SelectMainMenuOption`, `SelectMove`, `ConfirmMessage` and `Update(dt)`. Keep rules (battle flow, movement, map queries, texture baking into `PixelCanvas`) callable without raylib GPU or input calls; put drawing and key handling in separate methods.

### Data

Species, moves, abilities, items and maps are JSON files in `PokemonPlatinumEngine/Data/` (`species.json`, `moves.json`, `abilities.json`, `items.json`, one `maps/<Name>.json` per map), copied to the build output and read through `GameDataFiles` (camelCase names, enums as strings). The first four hold the whole National Pokédex and are generated by `tools/DataImporter` from the Platinum decompilation (Generations 1–4) and PokeAPI (later ones): don't edit them by hand; put corrections in `tools/DataImporter/Overrides/` and re-run it (`dotnet run --project tools/DataImporter`), which also rewrites `docs/mechanics/coverage.md`. Moves the engine can't fully run carry `effect` and `support` (`Partial`: hits but the effect is missing; `None`: does nothing yet). What abilities do is code in `Data/AbilityEffectTable.cs`, held items in `Battle/Effects/HeldItemEffects.cs`. `PokemonDatabase`, `MoveDatabase`, `AbilityDatabase` and `ItemDatabase` load their file in a static constructor; `MapDatabase.Initialize()` reads every map file into fresh `Map` objects (new NPCs, newly rolled trainer Pokémon) through `MapFile.ToMap`. `docs/data-files.md` describes the formats; `MapFile.FromMap` writes a map back out, so a tool can generate map files. `TypeChart` is still code (Platinum's chart plus Fairy for later species). Maps are tile grids (`Overworld/Map`: ground layer, overhead layer, solid grid) plus NPCs, props, warps, signboards and wild encounters. NPC behaviour is flag-driven (`IsTrainer`, `IsHealingNurse`, `IsPokeMartClerk`, `IsPCTerminal`, `IsStarterBriefcase`, `IsTransportAttendant`), dispatched in `GameEngine.TryInteract`. Plan 01 · M1–M2 replaces the hand-made maps with imported ones.

`RegionDatabase` chains the regions in generation order: a new game starts in Kanto, and finishing a region's story (its `<Region>HallOfFame` story flag in `StoryProgress`, saved as `StoryFlags`) lets the attendant at its departure map take the player to the next region (Kanto to Johto by boat; later links are `Transport.Undecided` until their region is built). Every map belongs to exactly one region and warps never cross regions. Sinnoh is the fourth region; `--region Sinnoh` or `GameEngine.NewGameRegion` starts there for testing, and saves made in Sinnoh load there.

### Rendering

The field and battles are 3D scenes; almost every asset is generated in code at start-up (meshes, textures, character rigs, Pokémon models, sound).

- **`RenderContext`** holds the GPU resources shared by both 3D renderers: the shaders (`FieldShaders`, GLSL embedded as strings), the shadow map, the scene target with a depth texture, and the half-resolution blur, bloom and ambient-occlusion buffers. Only one 3D scene renders per frame.
- **Frame order in `GameEngine.Draw`**: the active renderer's `Render(...)` runs first (shadow pass → scene pass → `PreparePost`), then `BeginTextureMode(virtualScreen)` opens and `DrawToScreen` / `DrawField` composites the scene through the post shader, with the 2D interface drawn on top.
- **Field**: `MapStructures.FindBuildings` derives buildings from the tile grid; `MapScene.Build` turns a `Map` into cached meshes (ground baked by `PixelGround`/`GroundBaker`, water, trees, tall grass, boulders, buildings, furnished rooms). Water is a flat surface over the ground whose shader reads a mask `PixelGround` bakes (alpha = water, red = texels from the shore). Plant and rock textures are drawn in `NatureArt`, without GPU calls, so tests can check them; `WorldRenderer` draws it with Platinum's field camera (pitch 59.05°, FOV 16.18°, taken from the decompilation). Characters are 3D rigs (`CharacterModels`) baked to pixel sprites (`CharacterSprites`) and drawn as lit, shadow-casting billboards. One world unit is one tile, +Z is south, and pixel art is 32 texels per unit.
- **Battle**: `BattleEngine` runs the rules and a queue of messages; `BattleAnimator` holds what the battle looks like at this moment (send-out, lunge, hit, faint, capture, displayed HP); `BattleRenderer` draws the stage (`BattleStage`, `SoftFoliage`, `SkyPainter`) and the Pokémon as 3D models from `PokemonModels`; `BattleHUD` and `ModernUi` draw the interface.
- **Pokémon**: `PokemonModels` builds the species seen so far from primitives with `PokeBuilder`; every other species uses its generic stand-in until plan 03 · D5. The same models appear in 3D in battle and are baked by `PokemonSprites` into 2D pixel sprites for menus (only the hand-built ones and the stand-in, at start-up). A test keeps the hand-built list from shrinking.
- **Look**: `ArtLook` holds the numbers of the style guide as light rigs (sun, ambient, fog, post settings, sky) for Platinum's five times of day, blended around each boundary. `GameClock` gives the hour from the system clock unless the options fix it.
- **Interface**: screens are composed from the kit. `UI/Kit` has `UiFonts` (Nunito), `UiShapes` (anti-aliased SDF panels, rings, lines, triangles), `UiIcons`, and the GPU-free `UiMotion`/`UiReveal` (easing, slide-in) and `UiNav` (grid cursors). `ModernUi` (a partial class: shared components, `.Battle`, `.Party`, `.Field`) holds the colour tokens and components; add a component there rather than drawing shapes in a screen. Each screen keeps its logic in methods that take no input (`StartMenu.Move/Confirm/Cancel`, `TitleScreen.PressConfirm`, `PartyScreen.MoveCursor`) so tests and the harness can drive it. Bag, Pokédex, trainer card, shop, PC and starter choice still use the old `RenderHelper`; plan 04 · G10 replaces them.
- **Music**: songs are MML text files in `Data/music/<region>/` and `Data/music/common/`, played by our own synthesiser (`Audio/`) on raylib's audio thread; `docs/music-format.md` describes the format, the instruments and the checks. Maps name their theme in `bgmTrack`; `MusicDirector` picks battle, victory and fanfare themes by role, looking in the region's folder before `common`. Call `AudioManager.PlayMusic` / `PlayFanfare`, never the mixer directly. Claude can't listen: render with `tools/MusicRender`, read its clash and level report, and give the user MP3s to approve.
- **Leaving the game**: the start menu's QUIT GAME and the title's QUIT set `GameEngine.QuitRequested`, which ends the loop in `Program.cs`. Esc is "back", never "exit".

### Rendering rules that have caused bugs

- raylib texture modes can't nest. Every offscreen pass (field, battle stage, sprite baking, post buffers) must finish before `BeginTextureMode(virtualScreen)`.
- Matrices are built with System.Numerics (row vectors: "A then B" is `A * B`) and must be transposed for `Raylib.DrawMesh`. The light view-projection is `Raymath.MatrixMultiply(view, proj)`.
- rlgl immediate-mode batches don't set `matModel`, which the custom shaders use. Draw immediate-mode quads (speech bubbles, blob shadows) with the default shader.
- Extra samplers set with `SetShaderValueTexture` must be bound after `BeginShaderMode`; each batch flush clears them.
- `WorldRenderer` and `BattleRenderer` share `RenderContext`, so each must set its own uniforms every frame (lighting, character style, fog, time).
- Upright things in the field are stretched by `MapScene.VS` (1 / cos(pitch)) to offset the steep camera. Pass true proportions for the shadow pass and in battle.
- Models rendered on their own (sprite bakes) must call `shaders.SetStudio()` first, or fog and cloud shade from the last scene leak into them. It also clears the walkers that grass leans away from, so `WorldRenderer` calls `SetWalkers` after the sprite bakes, and `BattleRenderer` passes none.
- Vertex alpha below 1 makes scenery sway; exactly 0 (`MeshBuilder.Sway(c, 1)`) also makes it part round walkers. Tall grass, lawn tufts and flowers are in `MeshPass.Ground`, which takes shadows but casts none.
- Field art moves in whole texels: place sprites and cards on the 1/32 grid, and step animation in the water shader with `floor`, never by scrolling UVs.
- `GameEngine.ApplySettings` re-syncs the clock, mute and quality from `Settings`. Change `engine.Settings` and call it (the harness does) rather than setting `GameClock.Fixed` directly. A quality change reloads only the render targets; shaders stay loaded because cached materials refer to them.
- Full screen is `WindowSettings`' own borderless window, one row taller than the monitor. Don't use `Raylib.ToggleBorderlessWindowed` or size the window to the monitor exactly: the NVIDIA driver then treats the OpenGL window as exclusive full screen and the picture flickers.
- `RenderContext.OutputIsNative` (window wider than 2880) turns FXAA on for the High preset; in smaller windows the downscale anti-aliases and FXAA would cost about 1 ms for nothing.
- Battle timing: `QueueMessage(text, onComplete, onShow)` — `onShow` starts the animation the message describes; damage lands `HitDelay` after the lunge through pending effects, which `ConfirmMessage` flushes first.

## Project rules

- **Follow Platinum**, not Diamond/Pearl, wherever they differ (gym order, encounter tables, HM locations, music roles).
- **Nothing taken from the games**: no sprites, models, textures, music, cries or script text. Art, sound and dialogue are our own; dialogue follows the original beats in our own words. Free-licensed assets (OFL, CC0, MIT) are allowed with the user's OK before downloading, and each gets a line in `docs/art/CREDITS.md`.
- **Reference data**: the pret/pokeplatinum decompilation has most game data as JSON. `docs/plans/README.md` lists the paths; fetch raw files from `https://raw.githubusercontent.com/pret/pokeplatinum/main/<path>`. Bulbapedia blocks automated fetching; pokemondb.net and Serebii work.
- **Style guide first**: when a visual rule has to change, change `docs/art/style-guide.md` before the code. `StyleGuideTests` checks the rules that can be tested without a GPU.
- **Add tests for new rules** (movement, scripts, battle effects, data completeness) and keep them green.
- **Commits**: commit or push only when the user asks; commit on `main`.
