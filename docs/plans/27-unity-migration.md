# Plan 27 · Moving the game to Unity

**Goal**: the game runs in the Unity editor and builds as a Unity player, with every rule, every piece of data and every generated asset it has today, and the roadmap in `docs/plans/` carries on from there. Unreal is not considered: it is C++ and Blueprints, so nothing of the 138,000 lines of C# would carry over (the C# bridges for Unreal are community projects, not supported by Epic).

Written 2026-10-08, before any session, at the user's request. It uses the same shape as the other plans: where we are, design, sessions, decisions to take. Numbers below were measured on `main` at `aa872be` (8 October 2026).

## Where we are

### How tied the code is to Raylib

The engine (`PokemonPlatinumEngine`) is about 138,000 lines of C# in 300-odd files; the tests are about 33,000 lines (1,266 tests) and the tools 11,500. CLAUDE.md's rule "logic stays free of the GPU" has held, which is what makes a move possible at all.

| Part | Lines | Uses Raylib | What happens to it in Unity |
|---|---|---|---|
| Battle rules (`Battle/Sim`, effects, AI, formulas) | ~19,800 with the battle face | 2 files: `BattleHUD` (drawing) and one Enter key in `BattleEngine.Menus` | Moves over unchanged |
| Data (`Data/`, the JSON, the world files, scripts) | ~5,200 + 11 MB of data | none | Moves over unchanged |
| Models, story, script runner | ~7,000 | none | Moves over unchanged |
| Field rules (`Overworld/`: movement, encounters, field moves, puzzles) | ~6,800 | 1 file (`DialogueManager`) | Moves over; dialogue's drawing is split off |
| Audio synthesiser (`Audio/`: MML, mixer, cries, ambience) | ~3,900 | none | Moves over unchanged; only its output changes (below) |
| Generators in `Graphics/` (SDF sculpting and meshing, all 1,025 Pokémon models, characters, pixel art, building kit, ground baking, move effects as data) | ~68,400 in 71 files | Raylib's **types** only (`Color` 928 times, a few `Mesh`, `Material`, `Texture2D`, `Matrix4x4`), no Raylib calls | Moves over once `Raylib_cs.Color` is replaced by a struct of our own (mechanical); what they output (vertex arrays, pixel buffers) is handed to Unity's `Mesh` and `Texture2D` |
| Renderers in `Graphics/` (`RenderContext`, `WorldRenderer`, `BattleRenderer`, `MapScene` uploads, `SkinnedModel`, post passes) | ~10,000 in 29 files | Raylib and rlgl calls throughout | **Rebuilt** |
| Shaders (`FieldShaders`: GLSL in strings, shadows with hardware PCF, bloom, blur, AO, FXAA, the upright straightening, water, sway) | ~1,100 | GLSL for OpenGL | **Rebuilt** as Unity shaders (HLSL) in URP |
| Interface (`UI/`, `ModernUi.*`, `UiShapes`, `UiFonts`) | ~10,200 in 25 files | immediate-mode drawing | **Rebuilt underneath**: the layout code can stay if `UiShapes`/`UiFonts` get a Unity back end (see Design) |
| Game loop, window, input, audio output (`Program`, `GameEngine` draw paths, `WindowSettings`, `InputManager`, `AudioManager`) | ~3,500 | yes | **Rebuilt** as MonoBehaviours, Unity's Input System and an audio callback |
| Screenshot harness (`tools/ShotHarness`) | ~5,000 | hidden Raylib window, reflection into `GameEngine` | **Rebuilt** as Unity play-mode tests that capture frames |

So the coordinator's first answer holds, with numbers: about 110,000 lines (80%) carry over with little or no change, and about 25,000 lines plus every shader are rewritten. What it left out is the C# version problem below, and the effect on how Claude works in this project.

### The C# version problem

The code is written for .NET 10 and recent C#: 306 files use file-scoped namespaces (C# 10), 65 types use primary constructors (C# 12), 18 members are `required` (C# 11), plus collection expressions, `System.Text.Json` (13 files), `Parallel.For` and `Span`.

Unity 6 as shipped today runs scripts on **Mono with C# 9 and the .NET Standard 2.1 API**, so none of the above compiles inside Unity as it is. Unity has announced that **Unity 6.8 replaces Mono with CoreCLR, on .NET 10 and C# 14**, with changes leading up to it from 6.6 and an alpha of 6.8 due later in 2026 (Unity's "Path to CoreCLR, 2026" and March 2026 status posts on discussions.unity.com; I read their summaries, not the release notes, so dates should be checked when we start). Two ways round it:

1. **Wait for, or start on, Unity 6.8.** The game's code compiles as it is. Risk: alpha and beta software at first.
2. **Compile the rules as a separate library** (`netstandard2.1`, `LangVersion` latest, a polyfill package for `required` and the like, `System.Text.Json` from NuGet) and drop the DLL into Unity. Most of C# 12's syntax works this way because it is compiled by our own compiler, not Unity's; .NET 5+ APIs the code calls (for example `Random.Shared`) need small replacements. A first build of the library against `netstandard2.1` lists exactly which.

Recommended: route 2 now, which also gives the split in Design below, and move to route 1 once 6.8 is stable.

### What changes in how the project is worked on

- **The Unity editor runs on your computer, not in the cloud.** The official Unity plugin you mentioned works by letting Claude drive the editor on the machine it runs on, so every Unity step of the port (scenes, shaders, play mode, screenshots) happens in a Claude session on your device, with Unity installed and signed in. Cloud threads keep working on the rules, data, scripts, tests and tools, which is most of the roadmap, as long as those stay a plain .NET library (Design).
- **The way Claude checks visual work changes.** Today a cloud thread runs `tools/ShotHarness` in a hidden window and looks at the PNGs. Unity cannot render without a GPU and a licence, so visual checks move to sessions on your device, or to a CI runner with a licence (below).
- **CI needs a Unity licence.** Building or testing the Unity project in GitHub Actions (for example with the GameCI actions) needs a Unity licence stored as a repository secret: a Personal licence works for a free project under Unity's revenue limit, but activating it for CI is a manual step you do once. The existing .NET build and tests keep running in CI as they do now, with no licence.
- **The roadmap pauses** while the renderer, shaders and interface are rebuilt. Nothing on the field or battle screen looks better until stage 4; the gain is a mature renderer, Unity's profiler and editor, and builds for other platforms later.

## Design

### Three projects instead of one

```
PokemonPlatinum.Core        (.NET library, netstandard2.1 + net10.0; no Raylib, no Unity)
├─ Battle/Sim, Models, Data, Story, Overworld rules, Audio synthesiser
├─ the generators: SDF kit, Pokémon and character models, pixel art, building kit, ground bake, FX as data
└─ its own small types: Rgba (for Raylib's Color), Matrix4x4 from System.Numerics (already used)

PokemonPlatinumTests        (xUnit on net10.0, references Core only; runs in the cloud and in CI as today)

PokemonPlatinumUnity/       (Unity project, Unity 6.x, URP)
├─ Assets/Plugins/PokemonPlatinum.Core.dll   (built from the library, copied by a build step)
├─ Assets/StreamingAssets/Data/              (the JSON, world files, scripts, MML: read at run time as now)
├─ Game/      GameDriver MonoBehaviour: the GameEngine's update and state machine, input, audio callback
├─ Field/     chunk streaming into Unity Meshes, the field camera, characters as sprites, life, weather
├─ Battle/    stage, Pokémon models, camera director, move effects drawn from MoveFx's quads
├─ Shaders/   the field, water, character, sky and post shaders in HLSL; URP renderer features
├─ UI/        a Unity back end for UiShapes and UiFonts, so ModernUi's layout code stays
└─ Tests/     play-mode tests that drive states and save screenshots (the harness's successor)
```

- **Keep the Raylib build alive until Unity reaches it.** Both front ends reference the same `PokemonPlatinum.Core`, so the game stays playable and the harness keeps working while the Unity side is built. The Raylib front end is deleted only at the end (stage 6).
- **`GameEngine` is split in two.** Its state machine, saves, scripts and story move into Core as `Game` (no drawing, no keys); the front end feeds it input and draws what it says. Most of `GameEngine` is already that; the drawing paths and the reflection-based harness hooks are what move out.
- **Generated assets stay generated.** The SDF meshes, pixel textures and building kits are pure C# producing arrays, so they feed `Mesh.SetVertices`/`SetIndices` and `Texture2D.SetPixelData` directly. The mesh cache (`cache/models`) and sprite cache keep working. Nothing becomes a hand-made Unity asset, which keeps the "nothing taken from the games, everything generated" rule intact.
- **Shaders**: URP gives shadows, bloom, SSAO, FXAA and tone mapping, which replace most of `RenderContext`'s post chain. What is ours stays ours as HLSL: the field's upright straightening and `1/cos(pitch)` stretch, the toon terminator and material table (`SurfaceMaterials`), water from the baked mask, sway and parting round walkers, lit window glass by alpha, the weather layers. Each is checked against a screenshot of the Raylib build of the same scene.
- **Interface**: `ModernUi` is immediate-mode drawing through `UiShapes` (SDF panels, rings, lines) and `UiFonts` (Nunito). The cheapest path is a Unity implementation of those two classes that batches into one mesh per frame on a screen-space canvas, leaving the 10,000 lines of screens as they are. Moving to UI Toolkit is possible later, screen by screen.
- **Audio**: the synthesiser fills a buffer today from Raylib's audio-thread callback; in Unity the same `FillStream` is called from `OnAudioFilterRead` on an `AudioSource`. Sample rate and buffer size come from Unity's settings.
- **Input**: `InputManager`'s `GameAction`s map onto an Input System action asset; the keyboard and gamepad bindings stay the same.
- **Determinism**: `Dice`, `FrameClock` and the battle's seeded generator are in Core and unchanged, so play-mode screenshot tests repeat run to run as the harness's do.

## Sessions

Each stage ends with something that runs. Stages 0–1 are cloud work; from stage 2 on, the Unity side is a session on your device.

1. **S0 · A spike before committing** (device, short). Install Unity 6 LTS, make an empty URP project, build `PokemonPlatinum.Core` as a DLL with only `Battle/Sim` and `Data`, and play one battle to its end in the editor's console. Answers: does route 2 compile, how the Unity plugin works with Claude on your machine, how CI licensing goes. **Decision point: go on or stay on Raylib.**
2. **S1 · Split out `PokemonPlatinum.Core`** (cloud). Move every GPU-free folder into the library; replace `Raylib_cs.Color` and the few other Raylib types in the generators with our own; move `GameEngine`'s non-drawing half into `Game`. The Raylib game, the tests and the harness keep working, all on the new library. CI builds the library for `netstandard2.1` too, so nothing Unity can't compile slips in afterwards. Mergeable on its own and useful even if the port stops.
3. **S2 · Unity shell** (device). The Unity project, the DLL build step, `StreamingAssets`, `GameDriver`, input, audio output: the title music plays and the title menu answers keys, drawn with plain placeholders.
4. **S3 · Interface back end** (device). `UiShapes`/`UiFonts` on Unity; every menu screen (bag, party, Pokédex, PC, options, intro, saving) drawn by the existing `ModernUi` code. Screenshot tests for the `menus` and `intro` states.
5. **S4 · Battle** (device). Stage, Pokémon models from the generators, camera director, move effects, HUD, ball, evolution scene. Screenshot tests matching the harness's `demo`, `arenas`, `evolution`.
6. **S5 · Field** (device, the largest). Chunk streaming, ground and water, buildings and props, characters, the field camera and its straightening, relief, life, weather, darkness, night. Screenshot tests matching `field`, `world`, `life`, `lab`; frame timings against the 8 ms budget.
7. **S6 · Switch over** (device + cloud). Unity CI with the licence secret; the harness's remaining modes as play-mode tests; CLAUDE.md rewritten for the new layout and workflow; the Raylib front end and `tools/ShotHarness` removed.

Roadmap work on rules, data, story scripts and Pokémon sculpts can carry on in cloud threads throughout, against `PokemonPlatinum.Core`.

## Decisions to take

1. **Go or no go after the spike (S0).** My recommendation is to do S0 and S1 before deciding the rest: S1 is worth having anyway (a clean rules library also serves plan 06's online server), and S0 shows whether working through the editor on your machine suits you better than the cloud-only loop this project has used so far.
2. **Unity version**: Unity 6 LTS with the library route now (recommended), or wait for Unity 6.8's CoreCLR and compile the code as it is.
3. **Render pipeline**: URP (recommended: light, shader-friendly, runs everywhere) or HDRP (heavier, aimed at high-end PCs, more work to reproduce the pixel-art look).
4. **Interface**: keep `ModernUi` on a Unity back end (recommended, cheapest) or rebuild in UI Toolkit.
5. **CI licence**: a Unity Personal licence activated for CI (you would do this once), or Unity builds and screenshot tests only on your machine with CI covering the .NET library alone.
6. **Platforms**: Windows only at first (recommended), or also macOS, Linux, Android or web, which is one of Unity's main gains and changes the 4K-first quality presets.

## Risks

- **The look drifts.** The style guide's rules live partly in custom GLSL; URP's own shadows and post effects will not match them exactly. Mitigation: every rebuilt scene is compared against a Raylib screenshot of the same state before the Raylib build is removed.
- **Two front ends at once** for several stages: every rule change must keep both building. S1's library makes that a compile check, not a duplication.
- **Less work possible in the cloud**: anything visual waits for a session on your device.
- **Unity's licence and pricing terms** apply to the project from then on.
