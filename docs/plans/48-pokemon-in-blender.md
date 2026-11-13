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
**Answered by the user on 2026-11-13.** 1: not understood as asked, so Blender stands, as chosen (put again in two sentences in that day's reply: the models are made as Blender files, or the same new look is written for the game's own C# sculptor with no files). 2: the C# sculpts are what the repository holds and what is drawn where nothing is built; the Blender models are ignored by git and are published with the game. 3: Claude fetches the reference pictures into a folder outside the repository, the 2D design and pictures of 3D models beside it. 4: agreed (the Sinnoh Pokédex in the story's order, then the regions). 5: a board a batch. 6: install the three starters now. 7: yes, the C# sculptors stay as the fallback and the source of rigs.

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

- [x] Decisions 1 to 7 answered (2026-11-13, above)
- [ ] BL1 · Parity: begun 2026-11-13 (below)
- [ ] BL2 · The kit for a whole line
- [ ] BL3 · The pilot and its gate
- [ ] BL4 on · Batches (sized after BL3)
- [ ] BL-A · Clips of their own (optional)

Built before the plan, as drafts (2026-11-12): the kit, the three starters' scripts, and what the skill records of them. The user's first correction is in them: Chimchar's and Piplup's eyes set further apart.

### BL1 as built so far (2026-11-13)

- **Storage** (decision 2): `built/models` at the repository's root, ignored by git. `ModelOverrides.LookForBuiltModels` adds it (beside the executable and in the folder the game is started from) after every other folder, and only `Program.Main` calls it, so tests, tools and the harness draw the sculpts whatever a machine has built (`BuiltModelTests`). `dotnet publish` copies the folder beside the published game (the project file's `PublishBuiltModels` target: a copy on publish alone, since as content the files would be copied into every project that refers to the engine).
- **`dotnet run --project tools/BlenderAnim -- sculpt <Species>... | all`** exports the C# sculpt with every model file switched off (`Sculpt.Export`, held by a test: a species already built must not be exported as its own file), builds the species' script twice, installs it only if the two builds are the same bytes, and prints the eye states, surfaces and clips the game's reader finds in the file.
- **Eye states**: a node named `eyes.<State>` is drawn only while the pose shows that state (`ImportedModels.EyesOf`, `ImportedPart.Eyes`, `ImportedRig.HasEyes`; `PokemonRenderer` hands `PokemonAnimation.EyesOf(pose, fierce)` to every pass); a state a file lacks shows the open eyes; a file with no eye nodes draws as before. `ImportedModels.Version` is 2. The kit: `Kit.eye_states(lay)` and `eye_look(state, open, lid, ink)`.
- **Surfaces**: a material named after a `SurfaceMaterial` is that surface (`ImportedModels.SurfaceNamed`). The kit: `Kit(coat="scales")`, `add(..., surface="shell")`, a material a surface on the fused mesh by the part each face is nearest.
- **The three starters** carry both (Turtwig scales, shell and leaf; Chimchar fur and its glowing flame; Piplup scales and shell, as their C# sculptors have them) and are installed in `built/models` on the Windows machine.
- **Releases** (the user's answer of 2026-11-13: Blender in CI): `release.yml` has a `models` job that fetches Blender 4.5.10 from blender.org, runs `sculpt all` and hands the files to every platform's zip as `built/models`, as the mesh cache is handed over (`ReleaseBuildTests` holds the names). **Not run yet**: a workflow can only be tried on GitHub, so the first tagged or hand-run release is its test (the system libraries Blender asks for on the runner are a guess from its documentation).
- **Still owed by BL1**: female looks from a file; a footprint checked for a file; the shiny of a file compared with the species' hand-picked palette; the release workflow seen to run.

### The first batch (2026-11-13)

The user's word of the same day changes decision 4's order: **the order a player meets them in, and Giratina first of all, as the cover's Pokémon**. So the starters' evolutions (BL3 as it was written) wait their turn in that order, and the batches go: the three starters (done), Giratina, then Route 201 and on (Starly, Bidoof, then Shinx, Kricketot and the rest as the routes bring them), each species' line when its first member is met.

- **Giratina** (Altered Forme, the one the title shows), **Starly** and **Bidoof** are sculpted (`sculpts/giratina.py`, `starly.py`, `bidoof.py`), each with five eye states and its surfaces, built and installed with the starters. Giratina is a first pass for the user's eye: its Origin Forme is not sculpted yet.
- **Reference pictures** (decision 3) are fetched into `references/pokemon/` in the folder above the repository, never into it: for each species the design's drawing and a picture of its 3D model, from the PokeAPI sprites repository, named `<number>_<Species>_art.png` and `_3d.png`. They are looked at while sculpting and used for nothing else.
- **Found by this batch**: a patch whose rays miss the surface left its points where the rays began, far off the model, which made the game measure the model too large (the kit now keeps only the points a face uses and says when a patch missed altogether); the turntable drew all five eye states at once, so every face looked fierce (it now shows the open eyes alone).
- **Not yet done for the batch**: the user's board and approval; the rubric filled in a species; female looks (Starly and Bidoof have them in the C# sculpts).

### The starters' lines (2026-11-13)

At the user's word the six evolutions came before the next routes, so that a new Turtwig no longer evolves into an old Grotle: **Grotle, Torterra, Monferno, Infernape, Prinplup, Empoleon**, each sculpted by an agent with the skill, the kit and the references (three agents, a line each, in parallel; the main session built, installed and shot them). All six are installed with five eye states and their surfaces and were seen in the game from the front, from behind and through every clip in battle.

- **Colours follow the designs, not the old sculpts**: Grotle's shell is yellow and Torterra's body brown under a green shell, where the C# sculpts had them the other way.
- **The kit grew by what the agents had to write for themselves**, now in `sculptkit.py`: `blade` (a tube squashed about its own line), `sheet` (a thick sheet from a function), `leaf` (a flat pointed piece along a direction), `lumpy` (bumps on a sphere), `polygon` (a patch's outline with corners). The turntable takes an eye state to show.
- **What the agents asked for and the kit still lacks**: a painted border sharper than a voxel (a patch with a `polygon` outline is the way round it); a patch on a loose piece; a ragged outline for flames; rock that keeps its facets through the remesh.
- **Weakest of the six, for the user's eye**: Infernape's face (too white) and round shoulder pads; Empoleon's soft shirt edge and rounded flippers; Torterra's beak from the front and its one-mass crown; Grotle's too-regular shrubs.
- **Measured**: an agent took 7 to 17 minutes and 33 to 68 tool calls for two species; the main session's build, install and game shots for six took about 12 minutes.
- **Next**: Shinx, Kricketot, Abra, Budew, Zubat and Geodude (begun the same day, three agents).

### Routes 202 to 204 and Oreburgh Gate (2026-11-13)

**Shinx, Kricketot, Abra, Budew, Zubat and Geodude**, by three agents as before, built, installed and seen in the game from the front, from behind and through every clip. Eighteen species are installed now (the nine of the starters' lines, Giratina, Starly, Bidoof and these six). Zubat has no eyes in its design and so no eye pieces: the reader draws it whole.

- **Weakest of the six, for the user's eye**: Zubat's mouth is a flat patch, not a hollow; Geodude's fists are boxy; Shinx's tail star shows behind its head from the front and its blue-to-black edge is soft; Kricketot's hood is a thin sheet whose inside shows from above; Abra's cream snout tip and smudged hip marks; Budew's body drawn small under its tall stalk.
- **Asked of the kit by this batch, not built yet**: a cut into the fused body (a real mouth); a faceting deform (written inside `geodude.py`); a patch that keeps off nearby parts; a flat star; an outlined patch; a closer view on the turntable; and a known quirk left as it is because three tuned sculpts rest on it: a mirrored patch's `outline` is not mirrored, so an outline that is not symmetric is laid as two patches.
- **Not done for any of the eighteen**: the user's approval a species; female looks (Starly, Bidoof, Shinx, Kricketot, Zubat and others have one in the C# sculpts); footprints and shiny palettes checked for a file; the rubric written out a species.
- **Next in the order met**: Psyduck, Machop and Onix (Oreburgh Gate and the mine), Wurmple's line and Ponyta (Routes 204 to 206), then Eterna Forest; each species' line with its first member, and Giratina's Origin Forme.

### Oreburgh Gate, the mine and Routes 204 to 207 (2026-11-13)

**Psyduck, Buizel, Onix, Wurmple, Machop and Ponyta**, by three agents as before (5 to 9 minutes each, 34 to 51 tool calls), built, installed and seen in the game from the front, from behind and through their clips (idle to dodge looked at; tired, cheer and faint shot and not looked at). Twenty-four species are installed now.

- **Weakest of the six, for the user's eye**: Machop's eyes are large and its mouth a flat pink patch, its tail thin; Ponyta's leg flames sit on the upper legs and its flames read leafy, its forelock makes the body small; Onix's facets are soft and its grin painted; Wurmple's fringe is blunt; Buizel stands on all fours as its skeleton does, with a short snout; Psyduck's bill has no curve at its corners.
- **Asked of the kit by this batch, not built yet**: one glow material a colour (Ponyta groups its flames by hand: a material a loose glowing piece otherwise); a flame tongue helper; `facets` in the kit (now copied into `geodude.py` and `onix.py`); flat shading and paint on loose pieces; a ring; a body's centre line with a belly direction (written by hand in `wurmple.py`); `paint(on=...)` by exact name (it goes by prefix: `tail` also takes `tailtip`).
- **Not done for any of the twenty-four**: as above (the user's approval, female looks, footprints, shiny palettes).
- **Next in the order met**: Wurmple's line (Silcoon, Cascoon, Beautifly, Dustox), Shellos, Pachirisu, then Eterna Forest (Buneary, Gastly, Hoothoot, Murkrow or Misdreavus), the evolutions of what is done (Staravia, Bibarel, Luxio, Kricketune...), and Giratina's Origin Forme.

### Corrections before the next batch (2026-11-13)

At the user's word the weak points listed so far were corrected before any new species: **Psyduck, Buizel, Onix, Wurmple, Machop, Ponyta** and then **Giratina, Zubat, Infernape, Empoleon, Torterra, Grotle, Shinx**, by six agents (three at a time, 8 to 13 minutes each), each given its species' list of faults. All thirteen were built again, installed and seen in the game from the front and from behind; the clips of Giratina, Empoleon and Shinx were looked at, the others' shot and not looked at.

- **The kit gained**: `add(..., carve=True)`, a part cut out of the fused body by a Boolean after the remesh, its colour lining the hollow (Zubat's, Machop's and Onix's mouths); `facets` (from `geodude.py`); one glow material a colour of light (Ponyta went from fourteen to three).
- **What changed, in a line each**: Giratina a heavy barrel with a gold helmet, half-rings, banded throat and gold-clawed legs (its third leg pair was on the wrong bones: the export's are `hindR.001` and `hindL.001`); Zubat a carved mouth with four fangs; Infernape a red mask on an orange face and flat swirled plates; Empoleon a lace shirt front of patches and angular loose blades for flippers; Torterra a loose hooked beak, a loose white band and a crown in four lobes; Grotle two unlike shrubs with loose leaves; Shinx's tail swung out to its side and a crisp blue-to-black line; Onix rocks of flat planes (convex hulls, shaded flat) and a carved mouth; Wurmple loose pointed spikes; Machop smaller eyes, a carved mouth, a stub tail; Ponyta ragged tongues of flame low on the legs; Psyduck a shaped bill and clean pupils; Buizel a longer muzzle and a lifted chest.
- **Still weak, for the user's eye**: Shinx's tail is a straight bar out to the side; Torterra's band carries long thin spikes and its crown reads as a bonsai; Grotle's shrubs are lumpy; Giratina stands low as its skeleton does and its wings are round sails; Infernape's cheek fur is large; Machop's carved mouth reads as pursed lips (a carve's colour spreads a voxel onto the rim); Ponyta's forelock is low; Buizel's head is narrow.
- **Asked of the kit, not built yet**: a carve whose colour stays off the rim; `rock` (hull, merged planes, flat shading) and `flat=True` for loose parts (in `onix.py`); `slab` (an extruded outline, in `empoleon.py`); `tongue` (a flame, in `ponyta.py`); `plate` (in `infernape.py`); a turntable close-up.

### Infernape's face, and Routes 204 and 205 (2026-11-13)

The user found Infernape's face odd: it was rebuilt against the references (small slanted eyes with a blue iris under the red mask, a muzzle that stands forward, large ears with blue linings, cheek fur as swept tufts) and seen in the game.

Then **Silcoon, Beautifly, Cascoon, Dustox, Shellos and Pachirisu**, by three agents (7 to 13 minutes each), built, installed and seen in the game from the front and from behind; the clips of Beautifly and Pachirisu were looked at (idle to dodge), the others' shot and not looked at. Thirty species are installed now.

- **Wings are loose flat plates**: a base plate and thicker coloured panels through it, so the pattern shows on both faces (`beautifly.py`'s and `dustox.py`'s `flat`, which triangulates any outline). They flap and fold with the game's clips.
- **The kit**: a skeleton of fewer than four bones (the cocoons) crashed the skinning; fixed.
- **Weakest, for the user's eye**: Beautifly's forepaws and proboscis read as one block from the front; Dustox has a waist and pie-slice upper wings; Shellos's eye ring is pointed by its lash; Pachirisu's back stripe does not join its tail's from behind; the cocoons' thread lines are speckled; Infernape's crest is a plain wedge under the crown of fire.
- **Where the references overruled the brief**: Shellos has no feelers (the ringed things are its eyes); Cascoon's eye rim is purple, not yellow; Dustox's wing edge is a darker band, not pale.
- **Asked of the kit, not built yet**: `flat` (any outline, flat shaded) and a flat ring; a tube's centre line and a stripe painted along it; a point on a part's surface along a direction; which side of a drawn line a point is on (`shellos.py`'s `side`); a mirrored patch with a mirrored outline.
- **Next in the order met**: Eterna Forest (Buneary, Gastly, Hoothoot, Murkrow, Misdreavus), Cherubi and Burmy from the honey trees, then the evolutions of what is done (Staravia, Bibarel, Luxio, Kricketune, Roselia, Golbat, Graveler, Golduck, Machoke, Floatzel, Gastrodon...), and Giratina's Origin Forme.

### Eterna Forest and the honey trees (2026-11-13)

**Buneary, Gastly, Hoothoot, Murkrow, Misdreavus and Cherubi**, by three agents (6 to 10 minutes each), built, installed and seen in the game from the front and from behind; the clips of Hoothoot and Gastly were looked at (idle to dodge), the others' shot and not looked at. Thirty-six species are installed now.

- **Gas with no transparency**: Gastly's cloud is opaque loose puffs in rings behind and round the sphere, the face left clear and the black ball showing through the middle from behind.
- **Weakest, for the user's eye**: Misdreavus is a brighter teal than the design's dark one, its hair lumpy and its pink locks stiff, like hands; Hoothoot's leg is long and pink (the skeleton's joint is high); Murkrow's crown is short and its tail thin from behind; Gastly's cloud is as regular as petals; Buneary's fleece is regular bumps and its standing ear thin; Cherubi's leaves are smooth arches.
- **Where the references overruled the brief**: Hoothoot's belly has no triangle marks (those are Noctowl's); Cherubi's small cherry hangs on its left; Misdreavus has six skirt points, five locks and a large middle pearl; Gastly's fangs hang from the upper lip.
- **Asked of the kit, not built yet**: a loose plate by a normal and a fan of blades (`infernape.py`'s `plate`); a puff (a core and balls on a sphere, in `buneary.py`), with jitter for clouds; a curved broad leaf (`cherubi.py`); colour by vertex on loose pieces; a patch outline with spokes.
- **Next in the order met**: Burmy and Combee (honey trees), Aipom, then the evolutions of what is done (Staravia, Bibarel, Luxio, Kricketune, Roselia, Golbat, Graveler, Golduck, Machoke, Floatzel, Gastrodon, Lopunny, Haunter, Noctowl...), and Giratina's Origin Forme.

### The honey trees and the first evolutions (2026-11-13)

**Burmy (Plant Cloak), Combee, Aipom, Staravia, Bibarel and Luxio**, by three agents (6 to 8 minutes each), built, installed and seen in the game from the front and from behind; the clips of Combee and Luxio were looked at (idle to dodge), the others' shot and not looked at. Forty-two species are installed now. An evolution's agent is given its forebear's script as the example and told to grow it, which kept Staravia, Bibarel and Luxio in their lines' colours and make.

- **Weakest, for the user's eye**: Combee is plain dark orange from behind and its abdomen hangs at one side; Staravia has a pale streak on one wing from behind and a narrow tail; Bibarel's bib and tail band have frayed edges; Aipom's teeth are blocky and its stare wide; Burmy's front collar leaves stand like ears; Luxio's ruff is messy at the chest.
- **Where the references overruled the brief**: Aipom's hands are purple; Bibarel's feet are round paws and its cream is a bib; Staravia's forehead spot is pale; Luxio's ear linings are ovals, its eyes white with a yellow iris, its rings on all four legs.
- **Asked of the kit, not built yet**: `prism` and a hex plate (`combee.py`); `flat` and its ear clipping in the kit (`beautifly.py`); a soft polygon paint in coordinates round an axis for masks and chest marks (`luxio.py`'s `poly_mask`); a paint in a part's own frame; a row of teeth along a carved smile; a thin line as a patch.
- **Not done for any of the forty-two**: the user's approval a species (Infernape's face was corrected at the user's word); female looks; footprints and shiny palettes; Burmy's other cloaks and Shellos's East Sea look, which still show the C# forms.
- **Next in the order met**: Kricketune, Roselia, Golbat, Graveler, Golduck, Machoke, Floatzel, Gastrodon, Haunter, Noctowl, Lopunny, then Routes 206 to 209 (Gligar, Stunky or Glameow, Bronzor, Chingling, Bonsly, Mime Jr.), and Giratina's Origin Forme.
