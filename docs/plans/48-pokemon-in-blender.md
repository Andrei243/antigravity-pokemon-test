# Plan 48 · Every Pokémon sculpted again in Blender

Written 2026-11-12, before any session, at the user's request, after the three Sinnoh starters were sculpted from nothing in Blender that day and the user judged them much better than the sculpts they replace (`.claude/skills/blender/SKILL.md`; `tools/BlenderAnim/blender/sculptkit.py`, `sculpts/turtwig.py`, `chimchar.py`, `piplup.py`).

**Goal**: every species and form the game draws, sculpted again with the Blender kit, each one looked at beside its design in the games and approved by the user, with nothing lost that a hand-built sculpt has today (blinking and shut eyes, surface materials, female looks, shiny looks, footprints, every clip). And, as the user asked, **a skill that gets better as it goes**: each batch judged by a rubric, each technique tried written down as it worked or failed, so that the hundredth species is made better and faster than the third.

It is the largest single piece of art work in the roadmap, so it starts with a pilot whose measured rate sizes the rest, and it replaces sculpts one species at a time: a species with no approved file keeps the sculpt it has.

## Where we are

- **What the game draws today**: `PokemonModels.Species` names 1,425 sculpts (the games' 1,025 and the 400 species of ours) and `PokemonModels.Forms` 543 more; 95 species have a female look by a `female` parameter of their sculptor; a shiny look is the same mesh in a `ShinyPalette` (a table of hand-picked palettes, and a rule by the National number). Each is C# on the SDF kit (`PokeBuilder`), with eyes and marks as decals painted in five eye states (`EyeState`: Open, Shut, Squeeze, Fierce, Smile), a surface material a shape (`SurfaceMaterial`: fur, scales, shell, leaf, metal, glow and the rest), a skeleton laid out by its body plan, and clips by role from `PokemonAnimation`.
- **A model file replaces a sculpt** (plan 03 · D5; `docs/model-files.md`): a `.glb` named after the species or form, found in `overrides/models` beside the game or in the folder it is started from, or in `assets/models` of the folder it is started from (tracked by git; a file there is not copied into a build by the project file as it stands). The harness reads only its out dir's `overrides/models`.
- **What was built on 2026-11-12** (drafts in a scratch folder; kit and scripts uncommitted when this was written): a kit that builds a new mesh on the skeleton and the eleven clips of the game's own export of the species (`ShotHarness export`): parts made in numpy (ellipsoids with deforms, tubes), fused by a voxel Remesh modifier and smoothed, coloured and weighted by part, painted by rules, with patches cast onto the surface for whatever must be crisp (eyes, masks, spots) and loose pieces for beaks, leaves and flames; a glowing material where a part gives light. A species is a script of about 3 KB; a build and its turntable take about twelve seconds; the three files are 3.2 to 3.5 MB each, 43 to 48 thousand vertices.
- **Seen in the game for the three starters**: `dex` front and back, the `pokemon` board and field sprites, all eleven clips in battle (Turtwig's before its sprout was shortened), a battle each, the shiny look (recoloured; whether it matches the hand-picked palette of the species was not compared), the flame's glow, no seam or ink where pieces and patches meet.
- **What a file loses today beside a hand-built sculpt**: the surface materials other than glow and metal (seen: the highlight bands are gone); the eye states (read from `GltfWriter` and the reader, not yet seen failing: the file carries open eyes only); a female look (not tried). Footprints, which are made from a model's sole, were not checked for a file.
- **What Blender itself gave, said once**: the gain the user saw came mostly from sculpting again from the designs with crisp patches and a part-by-part method; Blender gave the remesh that fuses parts, a renderer to look at a model in seconds, and room for techniques the C# kit has no words for (below). The user chose Blender, and this plan is written for it; decision 1 names the alternative.
- **Other work that sculpts**: plan 46 · CP4 to CP8 (Carpathia's species, in C#) and the tidy-up of plan 03 · D14's weakest sculpts. They go on as they are until this plan's pilot has passed its gate.

## Design

### A species is a script; a file is built from it

The script (`tools/BlenderAnim/blender/sculpts/<species>.py`) is the source, reviewed and versioned like code. The `.glb` is built from it, the same bytes every time on the same Blender (checked: the three starters rebuild byte for byte). Where the built files live is decision 2: two thousand files of 3.3 MB are about 6.5 GB, which does not belong in git as they are.

A sculpt keeps standing on the C# sculptor's skeleton and clips, taken from the export. So the C# sculptors stay: as the source of rigs and clips, and as what is drawn for every species not yet approved. A species with no C# sculptor (none today; plan 46's would have one) can have its armature made by the kit, as the hero's was, and is then moved by its body plan.

### Parity first

Before any batch, the reader of model files learns what a hand-built sculpt has, so that an approved file is never a step back:

- **Eye states**: a file carries its eyes as pieces named for the state (`eyes.Open`, `eyes.Shut`, `eyes.Squeeze`, `eyes.Fierce`, `eyes.Smile`); the reader draws the one the pose asks for. A file with only open eyes behaves as today.
- **Surface materials**: a material's name says which of the game's it is (`fur`, `scales`, `shell`, `leaf`, `skin`, `cloth`, `plastic`, beside today's glow by emission and metal by metallic), so highlight bands and shade tints come back.
- **Female looks**: a file `<Species>+f.glb`, or pieces of one file shown for a female, built by the same script with a flag.
- **Shiny**: kept as the recolour it is; a test compares a file's shiny against the species' own palette for the starters, and the kit learns to name its colours by role if the rule alone gives the wrong picture.
- **Footprints, outline, size and `fill`**: checked for a file and fixed where they differ.

### The kit grows with the species

The three starters needed ellipsoids, tubes, paint and patches. The Pokédex needs more, each added when a species first asks for it and written into the skill with what was seen: wings and fins as lofted membranes, spikes and horns, shells and plates, tails in segments, long bodies along a curve (serpents), petals and leaves in rings, fur tufts, mouths that open, hands with fingers, symmetric and repeated parts, parts shared along an evolution line, and forms as a script that starts from its species' and changes it (a regional recolour, a Mega's additions), as the C# does. Each family of body gets a worked example in `sculpts/`.

### How a species is judged

A rubric, filled in for every species in its batch's notes, each line yes or no with what was seen:

1. The silhouette reads as the species from the front, the side and behind (the player's own Pokémon is seen from behind).
2. Proportions: head to body, limbs, the feature that makes it itself (a sprout, a flame, a crest).
3. Every marking of the design is there, crisp, on the right part.
4. Colours are the design's, and the shiny is recognisable.
5. The face: eyes the right shape and spacing, all five eye states.
6. It reads at the menu sprite's 128 pixels and the field sprite's 32 texels to the tile.
7. Every clip plays without tearing or a part left behind; nothing floats when it faints.
8. It stands at the right size beside its line (a tall crest must not shrink the body).
9. Vertices within the budget (about 50 thousand), and it builds the same twice.

The user approves from a board a batch (the turntables beside the old sculpts, a battle each); a species sent back is redone in the next session. Reference pictures of the designs are looked at while sculpting and are never put in the repository or used as textures (decision 3 says where they come from).

### Learning, measured

- **The skill is the notebook** (`.claude/skills/blender/SKILL.md`): only what was run, each technique with its result, each of the user's corrections as a rule ("eyes further apart on round heads" came from the first review).
- **A list of techniques to try**, each to end as "works, here is how" or "fails, here is why": automatic weights against the kit's own; a texture baked from vertex colours with UVs; edit-mode and sculpt operators through a context override; Geometry Nodes for repeated parts; shape keys baked to clips; clips keyed for a species' own character (`animate.py`, once `rig.py` finds our rigs' bones, which it does not today); Workbench or EEVEE boards that match the game's look more closely.
- **Numbers kept a batch**: species approved first time against sent back, minutes a species, rubric lines failed most often. The plan's sizes are rewritten from them.

### Order

The pilot is the three starter lines of Sinnoh, which also removes the jump a player would see when a new Turtwig evolves into an old Grotle. After it, by decision 4; within any order a line is done together and a species brings its forms and its female.

## Decisions for the user

1. **Blender's files, or the same method in the game's own kit**: this plan as written (Blender; the user's choice of 2026-11-12, and the default), or the same reference-driven redesign with crisp patches written for the C# kit, which would need no files, no Blender in the build and no parity work, and gives up Blender's techniques and its quick look.
2. **Where the built files live**: (a) in git under `assets/models` (about 6.5 GB when done: not recommended); (b) scripts in git, files built by a tool command into a cache and published with each release, the game drawing the C# sculpt where a file is missing (recommended; it makes Blender 4.5 a tool the Windows machine has and the Linux cloud sessions do not install today, so shots there would show the old sculpts until they do); (c) as (b), with the files made smaller first (the reader already takes quantized attributes; a tool to write them would be a download to approve).
3. **Reference pictures**: the user supplies them a batch, as on 2026-11-12, or Claude fetches them into a folder outside the repository with the user's leave a batch.
4. **Order after the pilot**: the Sinnoh Pokédex in the order the story shows it (recommended: what a player meets first is redone first), then the regions in the chain's order; or the National order.
5. **Approval**: a board a batch with the user naming the ones to redo (recommended), or each species one by one.
6. **The starters' three drafts**: installed now as they are (open eyes only, less gloss), or after the parity session (recommended).
7. **The C# sculptors afterwards**: kept as the source of rigs and the fallback (recommended), or retired a species once its file is approved.

## Sessions

Prefix `BL`. `H` needs the harness and Blender, `U` waits for the user. No size is given past the pilot: the pilot measures it.

### BL1 · Parity (1 to 2 sessions; H)
The reader of model files learns eye states by piece, surface materials by name and female looks; footprints, outline and size checked for a file; decision 2's storage made (a `BlenderAnim sculpt <species>...` command that builds, checks a build is the same twice and installs; the game's fallback; where a build and a release find the files). `docs/model-files.md` and the style guide first. Tests: a file with the five eye pieces shows each; a material's name gives its surface; a species with no file draws its sculpt.

### BL2 · The kit for a whole line (1 session; H)
The kit's eye states (one description of an eye, five pieces), materials on parts, a part shared between a line's scripts, forms and females as a flag, a board command (old beside new, front, back, the sprite sizes, a battle). The three starters rebuilt with all of it and compared with their drafts.

### BL3 · The pilot: the starters' lines (1 to 2 sessions; H, U)
Grotle, Torterra, Monferno, Infernape, Prinplup, Empoleon, and the three starters brought to the rubric. Each scored; minutes a species written down. **Gate**: the user's board. A yes installs the nine, fixes this plan's sizes from the measured rate, and tells the other sculpting lanes whether they change over.

### BL4 on · Batches
A line or a family a batch, as many as the measured rate allows in a session, in decision 4's order, each with its board and its rubric notes; the kit's new shapes and the skill's new lines land with the batch that needed them. Agents may take a batch each once BL3 has passed (at most three with the harness at once), each given the skill, the kit and a worked example of its body family.

### BL-A · Clips of their own (optional, beside the batches)
`rig.py` taught the body plans' bone names; a species given clips keyed for its own character where the body plan's are plain.

### Last
Every name of `PokemonModels.Species` and `Forms` has an approved file or a line saying why it keeps its sculpt; decision 7 carried out.

## Needs and gives

- **Needs**: Blender 4.5 on any machine that builds a file; `tools/ShotHarness export`; the user's eye a batch. It reads plan 03's sculptors and changes none of them.
- **Gives**: plan 46 a way to sculpt species that have no forebear; plan 47 the same kit for a person's pieces; plan 16 a release that carries built models.
- **Touches**: `Graphics/ImportedModels.cs` and `GltfReader.cs` in BL1 only; `tools/BlenderAnim`; nothing of the battle's or the field's rules.

## Status

- [ ] Decisions 1 to 7 answered
- [ ] BL1 · Parity
- [ ] BL2 · The kit for a whole line
- [ ] BL3 · The pilot and its gate
- [ ] BL4 on · Batches (sized after BL3)
- [ ] BL-A · Clips of their own (optional)

Built before the plan, as drafts (2026-11-12): the kit, the three starters' scripts, and what the skill records of them. The user's first correction is in them: Chimchar's and Piplup's eyes set further apart.
