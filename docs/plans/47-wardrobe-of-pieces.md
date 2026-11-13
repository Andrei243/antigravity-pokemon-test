# Plan 47 · A wardrobe of pieces

Written 2026-11-12, before any session, at the user's request, from the Blender work of the same day (`.claude/skills/blender/SKILL.md`; `tools/BlenderAnim/blender/sculpts/hero.py` and `hero_outfits.py`).

**Goal**: the player dressed from pieces that are modelled once and coloured at the moment they are worn. A character is a body in parts, a hair cut and a garment in each slot, each a mesh of its own on one skeleton; a garment carries zones and no colour, so every colourway and every dye is the same mesh; a garment says which parts of the body it covers, and those are not drawn. The wardrobe, the boutique, the salon, the dye house and the introduction keep working as they do, on the same garments by the same ids, and saves keep loading with no version raised.

What a player notices: clothes with their own shapes (a cap that is a cap, a tee whose sleeves end on the arm), many more garments for the same work, and any garment in any colour.

## Where we are

- **The wardrobe today** (plan 11 · C9, C10; plan 43 · LF3): an `Outfit` (`Core/Outfit.cs`) is a garment id in each of five slots (`Hat`, `Top`, `Bottoms`, `Shoes`, `Bag`) and the salon's four parts (hair cut, hair colour, eyes, skin). `Data/clothes.json` has 39 garments, each a shape by flags (`hat: Beret|Cap`, `shortSleeves`, `coat`, `stripes`, `scarf`, `shorts`, `skirt`) with a `color` and an `accent`; a dyed garment is a garment of its own beside the plain one (`Wardrobe.Dye`). Only drawing reads an outfit: the look's name dressed (`PLAYER@red_cap.-.-.-.-`) keys the rig, its sprites and its mesh cache.
- **How a person is made today**: `CharacterModels` sculpts every look whole with the SDF kit, chibi (19 bones, four builds) or grown (26 bones, five builds, plan 30 · FM4), meshes it once and caches the mesh. A rig may already hold its clothes apart from its body (`CharacterRig.Clothes`, `ClothesShell`) and what it holds (`Held`). The face is not in the mesh: a patch of the head (`FacePatch`, `FaceUVs`, `FaceLayout`) takes textures the engine paints (`CharacterFaces`), which is where expressions, blinking and the speaking mouth (plan 26 · V6) come from. Field sprites are baked from the rig (`CharacterSprites`).
- **A third kit exists as a prototype on `main`** (not in the tree this plan was written in): `CharacterModels.Modelled.cs`, lofted surfaces by `PolyBuilder` on the grown frame and skeleton, the grown Teen hero only, behind `CharacterModels.Modelled` (`SHOTS_MODELLED=1`), with its own painted face (`ModelledFaces`). Style guide, "Modelled characters (prototype)".
- **No character is read from a file.** Model files are read for species only (`ModelOverrides` in `Graphics/ImportedModels.cs`).
- **What was tried in Blender on 2026-11-12** (drafts, nothing in the game): the boy hero on the chibi `Kid` frame's joints as 18 pieces in one `.glb` (7.8 MB): a body in six parts, eyes and face marks as pieces on the head, one hair cut, two hats, two tops, two bottoms, shoes, a bag, a scarf. Each piece is a node named `<slot>.<Shape>`, skinned to one armature, with extras `colours` (`zones` or `true`), `tint` (`skin`, `hair`, `eyes` or the garment's own) and `hides` (the body's parts it covers). A piece of zones has vertex colours that say how much of each zone a point is (red primary, green accent, blue white, none ink). `hero_outfits.py` read the file back as an engine would and rendered four outfits on one board with an arm raised and a knee bent. Checked: names, extras, one armature and exact zones survive the export and the import; garments follow the body in that pose with the covered parts left out.
- **What the draft is not**: it has no clips; its head alone is 25,010 vertices; it is one frame of nine (chibi Kid; not the girl, the other builds or the grown kit); a hat that covers the brow hides the fringe; its eyes are baked pieces, so as it stands it could not blink, change expression or speak.

## Design

### Pieces, zones and what is hidden

The three ideas are independent of how a piece's mesh is made, and are the plan's core:

1. **A piece a slot.** Body parts (`head`, `torso`, `arms`, `hands`, `legs`, `feet`), `hair.<HairCut>`, `hat.<Headwear>`, `top.<shape>`, `bottoms.<shape>`, `shoes.<shape>`, `bag.<shape>`, and `extra.<what>` for what a garment brings with it (a scarf). The names are the game's own words, so a row of `clothes.json` names its piece with one new field, `piece`, beside its `color` and `accent`.
2. **Zones, not colours.** A piece holds up to four zones (primary, accent, white, ink); the garment's `color` and `accent`, or the salon's skin, hair and eye colours, fill them when the rig is built. A dye stops being a second garment's mesh: it is the same piece with other colours. Stripes are the accent zone: a plain tee is a striped tee whose accent is its colour.
3. **A garment hides what it covers.** `hides` lists body parts; a part hidden by anything worn is not drawn, so nothing pokes through in any clip, and a garment needs no exact fit to the skin.

The rig keys stay as they are: an outfit's key already names every garment, so a rig assembled from pieces is cached and baked to sprites under the same name.

### The face stays the engine's

A head built from pieces keeps `FaceLayout`, `FacePatch` and `FaceUVs` exactly as a sculpted head has them, so `CharacterFaces` goes on painting expressions, blinks and the speaking mouth. The draft's baked `face.eyes` and `face.marks` pieces are dropped; the salon's eye colour stays a parameter of the painted face. WD1 verifies that a patch can be cut from a head that came from a piece (the modelled prototype on `main` already does it for its own head, by the skin's colour).

### Where a piece's mesh comes from (decision 1)

Two routes give the same pieces, zones and hiding:

- **(a) Pieces built in C#**, by the modelled kit on `main` (or the SDF kit) split into pieces. Everything the engine has today carries over (the frames of every build, the face, layered clothes, the cache); nothing new is shipped; Blender is where a shape is tried and looked at before it is written in C#.
- **(b) Pieces read from files** made in Blender (`sculpts/*.py`): a `.glb` a build, read by a new reader for people beside the species' one. Shapes are free of the kits' vocabularies, but each build needs its own file, the files are megabytes each, Blender becomes a tool every session that touches clothes needs, and the reader must rebuild what the kits give for nothing (a face patch, outline shells, eight-way sprites at 32 texels from a mesh of 100,000 vertices).

**Recommended: (a)**, with the hero's Blender file kept as the reference the C# pieces are judged against. It gets the new wardrobe into the game soonest and loses nothing a player has now. (b) remains open for single showpiece garments later; the format above is ready for it.

### Builds and fit

A garment must fit every build that may wear it: the chibi boy and girl (`Kid`), and the grown hero of the free-movement regions (plan 30). Under route (a) a piece is a function of the frame, as every part of the kits is, so one definition fits all builds. Under (b) it is a file a build. The player is the plan's scope; other people keep their whole sculpts until the user asks otherwise (decision 2).

### What must not move

Saved outfits and wardrobes, garment ids and prices, the boutique's stock, the salon's lists, the dye house, the introduction's sets, the Trainer Card's art, the bands' portraits and the field sprites of everyone who is not the player. An outfit with nothing chosen must draw the plain look (decision 3 says whether "as before, to the texel" or "in the new pieces").

## Decisions for the user

1. **Where a piece's mesh comes from**: (a) built in C# by the modelled kit, Blender as the sketchbook (recommended), or (b) read from Blender's files.
2. **Who is dressed from pieces**: the player only (recommended), or also the named cast, or everyone.
3. **The plain look**: the boy and the girl with nothing chosen stay exactly as they are drawn today (recommended: yes until WD3's gate, so no shot moves), or are redrawn in pieces at once.
4. **Which kit first**: the chibi player of Sinnoh to Unova (recommended, where most of the game is played), or the grown player of Kalos on.
5. **New garments**: how many shapes are wanted in the first set beyond today's (a hoodie with its hood, a skirt, a coat, boots, a second and third bag), and whether new slots are wanted (gloves, glasses, a neck slot for the scarf). A new slot is a field of `Outfit` and of the save, so it is asked here and not assumed.
**Said by the user on 2026-11-13**, before the five were answered: the grown characters are to be remade too, not only the chibi ones. So decision 4 is no longer either-or: both kits are in scope, WD5 is a session of the plan and not an afterthought, and WD1's prototype shows a grown hero beside the chibi one. Decisions 1, 2, 3 and 5 are still open.

## Sessions

Prefix `WD`. Sizes are sessions; `H` needs the harness, `U` waits for the user.

### WD1 · The prototype and its gate (1 session; H, U)
Style guide first ("A wardrobe of pieces": pieces, zones, hiding, the face). Then, behind a switch that is off (`CharacterModels.Pieces`), the chibi boy assembled from pieces by decision 1's route: body parts, one hair cut, today's Beret and Cap, jacket and tee, trousers and shorts, shoes, a bag; zones filled from `clothes.json`'s colours; `hides` obeyed; the face patch kept. A harness mode (`wardrobe`, not part of `all`) shoots the turntable of six outfits beside today's, the field sprites, a walk and a run, a talk with the mouth open and every expression. It prints vertices a rig and the time to build one. **Gate**: the user looks at the boards and says go, change or stop.

### WD2 · Every garment of today as a piece (1 to 2 sessions; H)
Each of the 39 rows of `clothes.json` names its piece; the girl's frame and her own clothes; dyes fill zones (a dyed id stays a valid id that resolves to the same piece and other colours, so saves load). `WardrobeScreen`, the boutique, the salon, the dye house and the introduction are driven through their own steps in the harness with the switch on. Tests: every garment has a piece, every piece's `hides` names body parts that exist, an outfit's rig is the same every build (cache key), no save version raised (`SaveCorpusTests`).

### WD3 · The switch thrown (1 session; H, U)
The player is drawn from pieces everywhere a rig is drawn: the field's sprites, battle, the Trainer Card, the bands, the photo modes. `diff` of the modes that show the player against the commit before, every moved shot looked at. **Gate**: the user's eye on the boards; a no leaves the switch off.

### WD4 · New shapes (1 to 2 sessions; H)
Decision 5's first set, each tried in Blender on the hero's file, approved from a board, then made a piece and a row of `clothes.json` with a price and a description (`ContentCheck`, `TextTool report`).

### WD5 · The grown player (1 session; H)
The same pieces on the grown frames of the free-movement regions, with plan 30's kit.

### WD6 · Optional, on the user's word
The cast or everyone from pieces (decision 2); new slots (decision 5); pieces read from files for showpieces (route b).

## Needs and gives

- **Needs**: nothing built later. Under route (a) it builds on the modelled prototype on `main`, so it starts from `main` and after whoever owns that prototype says it is steady. It touches `CharacterModels`, which plan 30 and plan 43 also touch: one of them at a time.
- **Gives**: plan 43 (the player's life) a salon and a dye house whose work is colours only; plan 46 a wardrobe a new region can add shapes to; the Blender skill a second kind of model to practise on.

## The user's answers of 2026-11-13, and what that session built

Asked in a session whose request was "remake the game's characters, chibi and grown, with the Blender skill", the user answered three questions, which settle decisions 1 and 2 otherwise than this plan recommended and overrule "The face stays the engine's":

1. **Route (b)**: the game reads the files made in Blender.
2. **Everyone, the whole vocabulary at once**: every build of both kits, every hair cut, hat, garment and thing held; not the player alone, and no gate between the pilot and the rest.
3. **The face is modelled in Blender**, a piece to a state, in place of the painted patch.

Decisions 3 to 5 were not asked. As built, decision 3 answers itself: with no file nothing moves, and with a file every look of that build is drawn from it.

**Built** (style guide, "Characters from files", is the format's reference):

- `Graphics/CharacterFiles.cs` (GPU-free): the reader (a file a build, pieces by node name, bones found on the engine's skeleton by name, extras `colours`, `tint`, `hides`), `Wanted` (the pieces a look wears), `FacePieces`, `Palette` and `Fill` (zones), `Assemble`, `HiddenBy`. `CharacterModels.Files.cs`: `BuildFromFile`, which `CharacterModels.Build` asks first and which answers only when the build's file holds every piece the look wears, and `FramesJson`, the engine's frames and skeletons for Blender's scripts. `CharacterRig.File` and `FaceState`: the modelled face's meshes, made the first time each is drawn. `GltfNode.Extras`.
- `tools/BlenderAnim/blender/peoplekit.py` (shapes in the game's axes, zones, lathes and rings), `people_frames.json` (held to the engine by `CharacterFilesTests`), `sculpts/people_chibi.py` (89 pieces a build: all four chibi builds) and `sculpts/people_grown.py` (the nine grown files), `tools/dev/people.sh`.
- The wardrobe needed nothing: an outfit already becomes a `CharacterStyle` (`CharacterStyle.Dressed`), and the pieces are chosen from the style. Garment ids, the boutique, the salon, the dye house, the introduction and saves are untouched. So WD2's "each row of `clothes.json` names its piece" is not needed while garments are shapes by flags; it comes back with WD4's new shapes.

**Not done, and the user's to decide**:

- **Where the files live: decided by the user on 2026-11-13, built at release.** No file is ever committed (`/assets/characters/` is git-ignored). The release workflow's `characters` job downloads Blender 4.5.10 LTS, runs `tools/dev/people.sh --strict` (which fails unless all thirteen files are made) and every zip ships them as `Assets/characters`, under the manifest `--verify` checks; the game reads that folder beside its executable after `overrides/characters` and `assets/characters`. **The job has never run**: it was written on the Windows machine and wants a first run by hand (`workflow_dispatch`, which makes a draft) before a tag relies on it. A developer builds them for a look with `tools/dev/people.sh <out dir>`, or into `assets/characters` at the repository's root to play with them (`tools/dev/people.sh --here`). The zips grow by the files' size, about 330 MB before compression: shrinking them (coarser hair, 16-bit positions) is still worth a session.
- **The look itself wants the user's eye**: boards of the chibi cast and the grown turntables beside today's were shown at the session's end; nothing is approved yet.
- WD4 (new shapes, new slots) and the pieces' own `clothes.json` field.
- **Measured** (`CHARACTER_FILES=<folder> dotnet test PokemonPlatinumTests --filter CharacterFilesTests` writes `sweep.txt` there): all 830 looks of the table in both kits are made from the twelve files their builds use, none falls back, the largest mesh is 40,798 vertices; a file is read in 50 to 150 ms and holds 25 to 33 MB, and nothing lets a file go once read (about 340 MB with every build in memory, which a crowd of every build would reach; freeing a build nobody stands in is owed).
- **Where the first pass is worse than the sculpts** (seen on the boards): the grown coats are closed, where the sculpt's stand open over the shirt; a grown coat over a skirt shows two tiers; the broad build's shoulders are boxy; hair is lumpier in both kits, Swept and Bun most; the swim bottoms' leg edge fights the thigh; the grown neck openings and the vest's V are a little stepped; the grown eyes are small.
- **Owed in the scripts**: `people_grown.py` calls `pp.k.build` itself to pass `band` and `wband`, which `People.piece` should take, and its `garment_body` reaches into the kit's parts to flatten a hem; hair's vertex counts (up to 17,000 for a long cut) are where a file's size would come down first.
- `--safe-mode` and `CharacterFiles.Enabled = false` draw everyone from the sculpting kits, which therefore stay.

## Status

- [~] Decisions 1 and 2 answered and the face decided (2026-11-13: route b, everyone, a modelled face); 3 to 5 open
- [~] WD1 · The reader, the format and both kits' files built behind "no file, no change" (2026-11-13); the user's look at the boards and where the files live are open
- [ ] WD2 · Every garment of today as a piece
- [ ] WD3 · The switch thrown
- [ ] WD4 · New shapes
- [ ] WD5 · The grown player
- [ ] WD6 · Optional

Built before the plan, as drafts (2026-11-12, uncommitted when this was written): `tools/BlenderAnim/blender/sculptkit.py` (a model built a piece at a time, zones, `cut`, decals on another piece), `sculpts/hero.py` (the 18 pieces and the file's format in its header) and `sculpts/hero_outfits.py` (the board).
