# Plan 09 · Looks: shiny and gendered Pokémon, animated sprites, status on the model, footprints, reflections, cameras and the text box

Written 2026-10-06, before any session.

**Goal**: what the finished graphics plan still leaves out of the picture: a shiny Pokémon drawn as shiny, the hundred species whose females look different, menu sprites that move, a condition seen on the Pokémon that has it, every species' footprint, reflections in water and on polished floors, a camera kit for the story's scenes and the title's attract loop, and a text box with coloured names, pauses and a backlog. None of it changes a rule of play: every session here is a change to what is drawn, held to plan 04's two laws (two harness runs draw the same pictures through `Dice` and `FrameClock`; a frame stays under 8 ms on High, measured with `profile` before and after).

## Where we are

- **Shiny**: `Pokemon.IsShiny` is rolled 1 in 8192 in the constructor (`Models/Pokemon.cs`) and saved (`SavedPokemonData.IsShiny`), and nothing in `Graphics`, `UI` or `Battle` reads it. A model's colours are baked into `SdfMesh.Colors` at sculpt time and its eyes and marks into the decal atlas (`PokemonDecals.Paint(model, EyeState)`); `PokemonModels.Signature(species)` and `PokemonSprites.GetBaked(name, view)` key by name alone. `SkinnedModel.Upload(source, boneCount, uvs, recolor)` already takes a recolour callback, which only the decal patch passes (to whiten it). Plan 06 · R10 lists shininess among the growth rules: the roll's rules are its, the look is nobody's.
- **Gender**: `Pokemon.Gender` (`Gender.Male`, `Female`, `Genderless`) and `PokemonSpecies.GenderRatio` exist; `PokemonModels.Get(string)` is keyed by species or form, and the only female look is Pikachu's `HeartTail` inside its costume forms (`PokemonModels.Pikachu.cs`). Plan 03's batches D6–D9 and Kanto 1 each say "not done here", and the batches since (to Unova 4: every species of Kanto, Johto, Hoenn and Unova, 642 hand-built) don't mention it; D11 covers only females that are a form of their own (`Meowstic-Female`, `Pyroar-Female`, `Frillish-Female` and `Jellicent-Female`, the last two hand-built as forms). `tools/DataImporter` reads `gender_rate` from PokeAPI's `pokemon_species.csv` (`Importer.cs`) and not that table's `has_gender_differences` column.
- **Menu sprites**: `PokemonSprites` bakes one still per `SpriteView` (Front, Back, Icon) into `cache/sprites`, keyed by `Signature` and `BakeVersion`, served by `PixelArtGenerator.GetPokemonSprite` and `GetPokemonIcon`; the Pokédex entry opens with a hop (`ModernUi.Pokedex.cs`) and its cry. `PokemonStudio.Strip(context, species, clip, times, yaws, w, h, …)` already renders a clip at chosen moments for the harness's boards. Plan 04 · G1 decided that menus show 2D sprites, never 3D renders; this plan keeps that.
- **Status in battle**: `CueKind.Status` plays once as the line appears (`EffectCue`, `MoveFx`), and `ModernUi.StatusPill` marks the HP box. `BattleRenderer` builds a `PokePose` (`Time`, `Blink`, `Attack`, `Kind`, `Hurt`, `Faint`, `Entry`) from each `CombatantView` and never reads `Shown.Status`; `PokemonAnimation.EyesOf(pose)` can give `EyeState.Shut`. The log carries `StatusChanged` and `Confused`; `Battler.ConfusionTurns` and `Battler.Volatile.InLoveWith` are the volatiles. Plan 06 defers only the Substitute's doll and the weather over the stage to the battle's art.
- **Footprints**: `species.json` has none, and the original's are art the importers may not keep. A `PokeModel` has its `Skeleton`, `Bones` (`PokeBoneInfo(Role, Side, Phase, Front)` with `PokeRole.Leg`), its `Mesh` (`SdfMesh.Positions`, `BoneIndices`, `BoneWeights`) and `Hovers`. Plan 04 · G9's footprints are the player's own (`FieldLife`, `TileBehaviors.KeepsFootprints`). Route 213 is plan 01 · M7's and plan 02 · S8's; ribbons are plan 06 · R17's.
- **Water**: a flat surface over the ground coloured in depth bands by `FieldShaders.Water` from the shore mask `PixelGround` bakes (style guide, "Water": whole texels, nothing a smooth gradient). `TileBehavior.Puddle`, `StillPuddle` and `ShinyFloor` are described as mirroring the walker, and `FieldLife` notes the mirror is not drawn. The 444 `ShinyFloor` tiles are the League's and the three ruins' (`docs/tile-behaviours.md`), none open. Plan 01 · M7 owes puddles a look of their own and their mirror (Route 212); the style guide's known gaps say the same.
- **Camera**: `WorldRenderer.PanCamera`, `ReleaseCamera` and `ShakeCamera`, reached by scripts through `camera pan`, `release` and `shake` (`CameraMove`, `IScriptHost.Camera(move, x, y, seconds)`, `GameEngine.PanCamera`); the lens is the area's preset (`MapScene.ViewOf` giving `FieldView(PitchDeg, FovYDeg, Distance)` from `FieldCamera`); the title's journey uses `WorldRenderer.RenderCinematic(map, focusX, focusZ, hour)` between its own bars. `TitlePhase.Idle` waits for a button forever.
- **Text**: `DialogueManager` types plain strings at `CharactersPerSecond` (`ShowDialogue`, `ShowQuestion`, `Type`, `Advance`) and draws through `ModernUi.DrawDialogue(sw, sh, speaker, visibleText, complete)`; `PlayerIdentity.Fill` substitutes `{player}` and `{assistant}` as plain text; `CharacterSprites.Portrait(context, npcType)` renders the face the Trainer Card shows; `UiNav.Window` (`UI/Kit/UiMotion.cs`) and `ModernUi.Dim` are there for a list over the field. The money mark inside a sentence is a known gap. Plan 02 · S4 adds `{rival}` the same plain way.
- **Measuring**: `FrameProfiler.Lap(FrameSection)` gives a pass its own line, `QualityProfile(SceneScale, AmbientOcclusion, Fxaa, ShadowTaps, ShadowMapSize, DepthOfField)` is what a preset can switch.

## Design

**One name for a look.** A new `Pokemon.LookName`: `ModelName`, then `+f` when the Pokémon is female and its species or form has a female look, then `+s` when it is shiny (`Gyarados+s`, `Pikachu+f+s`). `PokemonModels.Get`, `Request`, `TryGet`, `Trim` and `Signature` take a look name: they strip the suffixes, build the base model, and for a look upload a second `SkinnedModel` from the same `SdfMesh` through `Upload`'s `recolor`, with a decal atlas painted in the look's colours (`PokemonDecals.Paint` given a palette). The SDF cache is untouched, because the mesh is the same; a female whose shape differs is a model of its own (below). `Signature` hashes the look in, so `cache/sprites` keys by it, and the harness's `dex` and `versus` modes take look names as they take forms. Everything that shows a particular Pokémon (the party, the summary, the PC, the battle, the evolution scene) asks by `LookName`; the Pokédex, which shows a species, asks for the look it wants.

**Shiny palettes** (`Graphics/ShinyPalettes.cs`, GPU-free). A model's shiny palette is a map from each of its own colours (the distinct entries of `SdfMesh.Colors` and the decals' `Color`, `Iris` and `White`): a table of hand-picked palettes for the designs whose shiny is famous, our own colours chosen after each (a black and crimson Charizard, a red Gyarados, the three Sinnoh starters), and a rule for every other species and form: the main and second colours shifted in hue by an angle taken from the species' National number (never a string's hash, which changes from run to run), the belly lightened, the dark and the eye kept. A generated species shifts its `PokeGenome` colours (`Main`, `Second`, `Belly`, `Dark`, `Eye`) the same way; the genome itself is never changed. The sparkle is a `CueKind.Shiny` cue that `BattleEngine.Show` raises on `Entered` when the Pokémon is shiny, made of `FxShape.Star` quads by `MoveFx`, timed to the send-out, seeded by the animator; the star beside the name is a new `UiIcon.Star` drawn by `ModernUi.NameWithGender`. The roll stays 1 in 8192 under every rules preset; a Shiny Charm or a chosen rate is not this plan's.

**A condition on the model.** `CombatantView` gains `Condition` (the `StatusCondition` shown, with `Confused` and `InLove` flags), set by `BattleEngine.Show` as the line that tells of it is read (`StatusChanged`, `Confused`, the infatuation line) and cleared on a cure, a recall or a faint: the screen stays a line behind the rules, as it does for HP. `PokePose` gains `Asleep` and `Frozen`: `EyesOf` returns `Shut` for a sleeper, `Apply` holds a frozen Pokémon's idle at one moment. What hangs about the body is `MoveFx.Condition(condition, age, seed)`: Z's rising, an ice shell of `Shard` quads with the model flashed ice-blue through `SetFlash`, sparks at intervals, small flames, bubbles with a purple pulse, stars circling a confused head, hearts over an infatuated one; a pure function of age and seed, drawn by `FxRenderer` in the 3D pass like any cue, so `BattlePresentationTests` can hold it still.

**Strips** (`PokemonSprites`). Beside the Front still, a strip of eight frames of the idle clip over one breath (`PokeModel.Tempo` sets its length), baked by the same path as the stills, cached as one PNG under the same key with the frame count, requested and served within `Service`'s budget, the still showing meanwhile. Played by `FrameClock` where one Pokémon is looked at: the Pokédex INFO page once its hop has landed, the summary, the starter choice. Lists and icons keep the still, so memory stays (a strip is 512 KB). The evolution scene renders its Pokémon live already and needs no strip.

**Female looks.** `PokemonModels.Females` lists the species and forms with a female sculpt, beside `Species` and `Forms`; a female look is `Paint`, `PaintEll` or `Mark` on the species' own sculpt where the design differs only in colour or a mark (Combee's red mark, Wobbuffet's lips), and a sculpt of its own where the shape differs (Pikachu's tail, Hippopotas's swapped colours). The importer writes `genderDifferences: true` from PokeAPI's flag, the only thing taken: a flag, not a picture. A test holds that every hand-built species with the flag is in `Females`; a generated species with the flag gets a female by a rule of `PokemonGenomes` (the second colour a step lighter, the head's parts a tenth smaller, seeded from the same genome), replaced by the sculptor's own touch when plan 03 hand-builds it.

**Footprint icons** (`Graphics/Footprints.cs`, GPU-free). An icon is made from the model: the vertices that follow a `PokeRole.Leg` bone (weight 0.3 or more) and lie within a cell of the model's lowest point are projected onto the ground, one foot of each pair, into a 16×16 `PixelCanvas` of ink; a fish, a floater or a model that `Hovers` has none, as Platinum shows none for them. Cached in `cache/sprites` under `Signature`; drawn in the Pokédex list row and on the INFO page.

**Mirror pass.** Reflections are pixel art like the water they lie in: before the scene pass `WorldRenderer` draws the upright things (people and item balls as sprites, the trees' and buildings' meshes with their winding reversed) once more through a view mirrored about the water plane into a half-resolution target of `RenderContext`, and `FieldShaders.Water` reads it in the mid and deep bands only, darkened to the band's colour, broken by the wave marks and sampled in whole texels of the field, so nothing is a smooth fade. There is no sky to mirror: the field's background is a flat colour. A puddle (`Puddle`, `StillPuddle`) and a polished floor (`ShinyFloor`) take the same target with a darker mix and no bands. The pass gets its own `FrameSection` lap and a `Reflections` flag in `QualityProfile`, on for High and Medium, off on Low. The style guide's "Water" section changes first.

**Camera rig** (`Overworld/CameraRig.cs`, GPU-free). The field camera's target, lens factor, path and bars become one state that is a function of time, driven by the existing pan, release and shake and by the new commands, and read by `WorldRenderer` each frame. A zoom changes the lens's field of view only, never the pitch or the distance, so `SetUpright`'s straightening of upright things holds at every zoom. A follow keeps the target on someone else's feet; a path eases through tiles at a pace; letterbox bars are the title's own (120 units) over the field and under the text box; slow motion scales the `dt` the engine hands the field while a script holds it, never the text. The title's `Idle` returns to `Journey` after thirty seconds, as Platinum's title returns to its opening.

**Rich text** (`UI/Kit/RichText.cs`, GPU-free). A line is parsed once into runs: `{red:…}`, `{blue:…}`, `{name:…}` (the speaker's colour), `{pause 0.4}`, `{speed 2}`, `{br}`, `{sfx select}`, `{money 300}` (the drawn mark, the known gap). `PlayerIdentity.Fill` writes `{player}` as a `{name:…}` run in the boy's blue or the girl's red, which is Platinum's convention for the player's name; `DialogueManager.Type` honours the pauses and speeds and `DrawDialogue` draws the runs with `UiFonts` in colour. The markup lives in lines only (scripts, overlays, map files' `dialog`): `RichText.Plain` strips it for `HeadlessScriptHost.Transcript`, and `ScriptParser` reports a code it doesn't know with file and line, `ScriptLibrary.Problems` a sound that doesn't exist. Platinum's battle text colours nothing, so the battle's message box takes runs and uses none. The backlog is a ring of fifty lines with their speakers.

## Sessions

### L1 · Shiny Pokémon
- `Pokemon.LookName`; `PokemonModels` and `PokemonSprites` taking look names (`Graphics/PokemonModels.cs`, `PokemonSprites.cs`); `Graphics/ShinyPalettes.cs` with the table and the rule; the second decal atlas; `PixelArtGenerator`'s ten callers passing the look where they show a Pokémon.
- `CueKind.Shiny` and its stars; a `shiny` sound in `SoundBank` with its row in `docs/sound-effects.md`; `UiIcon.Star` beside the name in the battle HUD, the party, the summary and the PC; `Pokedex.CaughtShiny` (saved as a set of numbers, empty for old saves) so an entry can show the shiny of a species caught shiny.
- Style guide: "Pokémon (version 2, G7)" gains the shiny rule, "Pokémon in menus" the star.
- Tests: every hand-built species' shiny differs from its normal in its main colour and no two palettes of a line are the same; `Signature("X+s")` differs from `Signature("X")`; the sparkle is the same list for the same age and seed; the saved set round-trips. Harness: `dex --shiny`, `versus Gyarados+s Milotic`, and `96_shiny` in `conditions` for the sparkle.
- **Done when** a shiny Gyarados is red in battle, in the party and in the Pokédex, sparkles as it comes out, and the normal one is drawn exactly as before (`diff` of `battle` and `menus` runs shows no change).

### L2 · A condition on the model
- `CombatantView.Condition`, `PokePose.Asleep` and `Frozen`, `MoveFx.Condition` with the seven looks (`Battle/BattleAnimator.cs`, `Graphics/BattleRenderer.cs`, `PokemonAnimation.cs`, `MoveFx.cs`).
- Style guide: "Move effects (G8)", "Marks on a Pokémon", gains the lasting looks and their sizes.
- Tests: `EyesOf` shut for a sleeper; a frozen pose equal at two times; `Condition` empty for `None` and stable for a given age and seed; the view's condition set by the line, not the core (a `BattleCoreTests`-style walk through the log). Harness: `96_status_<condition>` for each, confusion and infatuation included, in `conditions`; `profile`'s battle scene before and after.
- **Done when** a Pokémon put to sleep keeps its eyes shut with Z's until it wakes, and the battle at its menu costs no more than before within the run-to-run noise.

### L3 · The text box
- `UI/Kit/RichText.cs`; `DialogueManager` and `ModernUi.DrawDialogue` playing runs; `PlayerIdentity.Fill` writing coloured names; `{money}` for the prize line; the parser's and the library's checks; `HeadlessScriptHost` stripping.
- The backlog: `Overworld/DialogueLog.cs` (a ring of fifty), opened with Up while the box waits, a `UiNav.Window` list over `ModernUi.Dim`; hold-to-skip and auto-advance are plan 08 · P2's rows (`GameEngine.SpeedUp`, `BattleEngine.AutoAdvanceAfter` and the field's timer in `UpdateDialogue`), so the box gains nothing of them here: a line's pauses are part of its typing time, which P2's factor scales like the rest.
- Portraits if decision 1 says so: `CharacterSprites.Portrait` at the left of the box for the named cast.
- Style guide: "Shapes and components", the dialogue line, changes first (the colours of the runs, the backlog panel).
- Tests: every code parses and strips to plain text; `{player}` colours by look; the ring; `StoryTests` holding every line's codes valid; a line's typing time with a pause. Harness: `story` shows a line of each kind and the backlog open.
- **Done when** "{player}" is blue for a boy and red for a girl in every talk, a pause can be written, and a closed line can be read again.

### L4 · The camera kit and the attract loop
- `Overworld/CameraRig.cs`; `CameraMove` gains `Zoom`, `Follow` and `Path`, `IScriptHost` gains `Letterbox` and `SlowMotion`, with `camera zoom 1.5 [0.8]`, `camera follow <who>`, `camera path X Y X Y … [tiles a second]`, `letterbox on|off [0.4]` and `slowmo 0.25 [2]` as Ops: a case in `ScriptParser` and `ScriptRunner`, a method of `GameEngine.FieldHost` and `HeadlessScriptHost`, a line in `ScriptTests.EveryCommand`, a row in `docs/scripts.md`. `camera release` brings everything back.
- `TitleScreen`: `Idle` back to `Journey` after thirty seconds.
- Style guide: "Field menus and notices", "Story scenes", gains the kit's numbers; "Opening and title screen" the loop.
- Tests: the rig as a function of time (a path's ends eased, a zoom's lens, bars' height); a scene using every command played headless; `TitleScreenTests` for the loop. Harness: the `story` scene written in the harness uses every command; `title` takes a shot after the idle time; a `life` shot of a zoom to check the straightening of sprites.
- **Done when** plan 02's chapters can write Spear Pillar's reveal (a slow path, a zoom, bars, slow motion) in `docs/scripts.md`'s language, and the title left alone plays its opening again.

### L5 · Sprites that move
- The idle strip in `PokemonSprites` (eight frames, `BakeVersion` raised), played on the Pokédex INFO page, the summary and the starter choice.
- Style guide: "Pokémon in menus" gains the strip's rule (eight frames over one breath, whole-number scale, where it plays and where the still stays).
- Tests: the strip's frames differ and its last leads back to its first; the key carries the count; lists never ask for a strip. Harness: `sheets` gains `90_pokemon_strip`; `menus` the summary and the entry at two moments.
- **Done when** the Pokédex entry breathes once it has hopped in, and `menus` lists stay the same pictures as before.

### L6 · Female looks: the key and Kanto
- `PokemonModels.Females`, `Pokemon.LookName`'s `+f`, the importer's `genderDifferences` flag (and `docs/data-files.md`'s row), `Pokemon.ModelName` left as it is; the two dozen species of Kanto with a female look (Venusaur to Gyarados, and Eevee's tail, which the newest games added: Pikachu's and Raichu's tails as sculpts of their own, the rest paint and marks).
- Style guide: "Pokémon (version 2, G7)" gains the female rule.
- Tests: every flagged hand-built species of Kanto in `Females`; a female sculpt distinct from the male; `Signature` differs by look. Harness: `dex` draws the female beside the male for a species in `Females`.
- **Done when** a female Pikachu's tail ends in a heart wherever it is shown and a male's is unchanged.

### L7 · Female looks: Johto and Hoenn
- The 22 of Johto (Meganium to Donphan) and the 16 of Hoenn (Combusken to Relicanth), every one hand-built since plan 03's Johto and Hoenn batches, in National order: paint and marks on the species' sculpt where the design differs in colour or a mark, a sculpt of its own where the shape differs (Wobbuffet's lips are paint; Heracross's heart-shaped horn and Scizor's fuller abdomen are sculpts).
- Tests: the flagged species of Johto and Hoenn in `Females`, each female distinct from its male. Harness: `dex` draws the Johto and Hoenn females beside their males.
- **Done when** every flagged species of Johto and Hoenn draws its female and the test of the list passes for both.

### L8 · Female looks: Sinnoh and the Pokédex
- The 31 of Sinnoh (Starly to Mamoswine; Hippopotas's and Hippowdon's swapped colours as sculpts) and Unfezant, the one of Unova whose female isn't a form of its own; the generated rule in `PokemonGenomes` for any flagged species without a sculpt (today none: every flagged species to Unova is hand-built and the later ones are forms, so the rule is the net under the list for the species plan 03 adds and the sculptor forgets); and the Pokédex entry able to show both looks (a toggle on the INFO page, `PokedexScreen` GPU-free).
- Style guide: "Menu screens (G10)", the Pokédex entry, gains the toggle.
- Tests: the whole flagged list covered, by sculpt or by rule; the toggle's state in `PokedexScreen`. Harness: `menus` shows the entry's female; `dex` the Sinnoh females.
- **Done when** the test that every flagged species has a female look passes for all 1025 species.

### L9 · Footprints
- `Graphics/Footprints.cs` and its cache; the icon in the Pokédex list and INFO page; a script question `if friendship 200` on the lead Pokémon (parser, runner, `EveryCommand`, `docs/scripts.md`) for Dr. Footstep, whose person and lines come with plan 02 · S8 (Route 213) and whose ribbon with plan 06 · R17.
- Style guide: "Menu screens (G10)", the Pokédex, gains the icon's place.
- Tests: every hand-built biped, quadruped and bird has a print of at least eight texels, every fish and floater none, the same print twice. Harness: `menus` entry shots; a `94_prints` board in `dex` of every species' icon.
- **Done when** the Pokédex shows a footprint for every species that walks and none for those that don't.

### L10 · Reflections
- The mirror target in `RenderContext`, the pass in `WorldRenderer` with its `FrameSection` lap, the sampling in `FieldShaders.Water`, the puddle's and the polished floor's mix, `QualityProfile.Reflections`; `BuildTerrainLab` gains a puddle and a shiny-floor tile so both are seen before Route 212 and the League open. Plan 01 · M7 keeps the puddle's own look (the cover the importer names from its behaviour) and takes its mirror from this pass, so neither session waits for the other.
- Style guide: "Water" gains a "Reflection" row, "Quality presets" the column; the known gap is struck.
- Tests: the mirrored view is the view reflected about the water plane; the pass draws nothing when no water or mirror tile is in view; Low draws none. Harness: `lab` shots at the pond, the puddle and the floor; `world` at Lake Verity's shore by day and after dark; `profile` on the towns and the lake before and after, on all three presets.
- **Done when** the player and the trees stand mirrored in Lake Verity in whole texels, and the heaviest town scene on High is still under 8 ms.

## Risks

- **The frame.** L10 draws the upright things twice where water is in view, and L2 adds quads to every battle frame. Both are measured first and last; L10 is on Medium and High only, and if a town with a shore crosses 8 ms it goes to High alone.
- **Memory and start-up.** Looks multiply sprites: a shiny or a female doubles a species' cache, a strip is four stills. All are baked on demand like the stills (plan 03 · D5), so nothing is paid for a Pokémon never seen.
- **Taste.** A shiny palette by rule can be ugly; the table grows as the user sees bad ones. A female by rule can be invisible; it is a stand-in until the sculpt.
- **The data's meaning.** Markup in a line is text the headless host must strip and the parser must check, or a typo shows on screen; `StoryTests` holds every line.
- **Determinism.** Everything new is a function of time and seed; a `new Random()` or a string's hash anywhere here breaks `diff`.

## Needs and gives

- **Needs** nothing first: every session builds on what plans 03, 04 and 06 · R2 gave. L9's Dr. Footstep waits for plan 01 · M7 and plan 02 · S8 (Route 213) and plan 06 · R17 (the ribbon); L10's puddles are seen on the terrain lab until plan 01 · M7 opens Route 212, and its polished floors until the League's rooms exist. Hold-to-skip and auto-advance are plan 08 · P2's and wait for nothing here.
- **Gives** plan 02 its scenes' camera (L4) and richer lines (L3) for S12 and S15; plan 01 · M7 the puddle's mirror (L10), leaving Route 212 only the puddle's cover; plan 06 · R10 a drawn shiny for its roll and plan 06 · R17 the footprint for its ribbon; plan 07 · O2 nothing to wait for, since looks are read from fields the save already has; plan 10 the look names its field sprites bake by; plan 11 a portrait beside the box for its named cast.

## Decisions for the user

1. **Portraits beside the text box?** Platinum has none. *Recommended:* yes, for named characters only, drawn from our own field sprites as the Trainer Card does, the name kept in its pill. Alternative: none, and L3 stays coloured names, pauses and the backlog.
2. **Shiny palettes: a table for how many?** *Recommended:* the thirty best-known designs by hand and the rest by rule, growing as bad ones are seen. Alternative: every hand-built species by hand (642 choices and their 223 forms, several sessions more).
3. **Female looks for generated species?** Today every flagged species to Unova is hand-built, so this decides the net for the batches to come. *Recommended:* the rule, replaced as plan 03's batches hand-build each. Alternative: none until hand-built, and the Pokédex shows one look for them.
4. **Reflections on Medium?** *Recommended:* yes, since Medium is the preset for a slower machine at 4K and the pass is cheap where no water is in view. Alternative: High only.

## Status

- [ ] L1 Shiny Pokémon
- [ ] L2 A condition on the model
- [ ] L3 The text box
- [ ] L4 The camera kit and the attract loop
- [ ] L5 Sprites that move
- [ ] L6 Female looks: the key and Kanto
- [ ] L7 Female looks: Johto and Hoenn
- [ ] L8 Female looks: Sinnoh and the Pokédex
- [ ] L9 Footprints
- [ ] L10 Reflections
