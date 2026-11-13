---
name: blender
description: Working on this game's Pokémon models in Blender with no window (bpy scripts run by `blender --background`): exporting a species' sculpt, looking at it on a turntable, changing it, writing it back as the game reads it and checking it in the game. Use when asked to refine, edit, inspect, rig, animate or render a model file (.glb) with Blender, or to learn or try something in Blender for this project.
---

# Blender for this game's models

Everything here was run on this project with Blender 4.5 LTS on the Windows machine (first session: 2026-11-12, the
Sinnoh starters). Add what a later session verifies; don't add what it only read somewhere. The API changes between
versions: when an operator or a property isn't where this says, look in the docs of the installed version
(`https://docs.blender.org/api/4.5/`), don't guess.

Read first, rather than repeated here: `tools/BlenderAnim/README.md` (the tool's verbs and how bones are found by
role) and `docs/model-files.md` (what the game reads from a model file, clip names, the options file).

## The loop

Claude can't see Blender's window and never opens it. Every step is a script and a picture opened with the Read tool.

```bash
B="/c/Program Files/Blender Foundation/Blender 4.5/blender.exe"     # not on the PATH; BLENDER is not set
S=<scratch dir>

# 1. A species' sculpt as a file (the first build is slow; run it alone, in the background)
dotnet run -c Release --project tools/ShotHarness -- "$S/h" export Turtwig Chimchar Piplup    # -> $S/h/models/<Species>.glb

# 2. Look at it: front, three-quarter, side, back on one sheet, and what the file holds (TT: lines)
"$B" --background --factory-startup --python tools/BlenderAnim/blender/turntable.py -- "$S/h/models/Turtwig.glb" "$S/tt/Turtwig.png" 480

# 3. Change it: an edit script run between import and export (none: an unchanged round trip)
mkdir -p "$S/ed/overrides/models"
"$B" --background --factory-startup --python tools/BlenderAnim/blender/roundtrip.py -- "$S/h/models/Turtwig.glb" "$S/ed/overrides/models/Turtwig.glb" "$S/py/my_edit.py"

# 4. See it in the game: the harness reads <out dir>/overrides/models, never the repository's
dotnet run -c Release --no-build --project tools/ShotHarness -- "$S/ed" dex Turtwig            # 94_dex_01.png
dotnet run -c Release --no-build --project tools/ShotHarness -- "$S/ed" dex --back Turtwig     # as the player's own is seen
dotnet run -c Release --no-build --project tools/ShotHarness -- "$S/ed" clips Turtwig          # its clips in battle
dotnet run -c Release --no-build --project tools/ShotHarness -- "$S/base" crop 94_dex_01 0 0 560 190 3 "$S/ed"   # beside a run without the file
```

**The game is the judge, not Blender's render.** The turntable is for seeing shape and colour quickly; whether an
edit is right is decided by `dex`, `dex --back` and `clips` beside a run of the hand-built model (an out dir with no
`overrides`).

Where things go: experiments and edit scripts in the scratchpad; a script worth keeping in
`tools/BlenderAnim/blender/` (the one place Python is written in this repository; a verb of the C# tool if it becomes
one); drafts of models in `<out dir>/overrides/models`; nothing in `assets/models` without the user's approval.

## What an exported sculpt is, in Blender

Seen with `turntable.py` on the three starters:

- One mesh object named after the species, 25 to 41 thousand vertices, all triangles (surface nets of an SDF: no
  quads, no edge loops; topology advice for hand-modelled meshes doesn't apply). Two materials: `body` and `eyes`.
- **The body's colour is its vertex colours**: one colour attribute, `Color`, per corner (`CORNER`), bytes
  (`BYTE_COLOR`). The importer wires it into the `body` material, so EEVEE shows it. There is no body texture.
- **The eyes and markings are a second primitive** (`eyes`): the triangles under each decal copied, with UVs into one
  small texture, alpha-clipped. Only the open eyes are in the file.
- An armature of the body plan's bones (Turtwig: 9), named by role (`root`, `body`, `head`...), and eleven clips named
  for their roles (`idle`, `physical`, `special`, `status`, `hit`, `faint`, `entry`, `dodge`, `tired`, `cheer`, `walk`).
- Axes: the game's front is glTF +Z, which becomes **Blender -Y**; up is Blender +Z; the model stands on z = 0 and is
  about one unit tall. `rig.py` has `FORWARD`, `UP`, `PITCH`, `ROLL` for this.

## What the round trip keeps and loses

Checked by exporting, round-tripping with no edit, and shooting `dex`, `dex --back` and `clips` beside the hand-built
model:

- **Kept**: the shape, the vertex colours, the eyes, the skeleton, all eleven clips with their names and their exact
  lengths, the facing and the size; the game still draws its outline round an imported model.
- **Lost, because the file never carried it** (`GltfWriter` writes none of it): the surface materials (fur, scales,
  shell, leaf, metal, glow), so the highlight bands and glossy spots of the hand-built model are gone and it is shaded
  plainly; the eye states (read from the writer, which puts in one texture with the eyes open; not yet seen failing
  in a shot); the shiny look is still made (seen on the new Turtwig and Piplup: the vertex colours are recoloured).
- So a Blender file replaces a hand-built sculpt at a cost. It suits a model that is new or wholly reworked; a small
  correction to a hand-built species is better made in its sculptor (`PokemonModels.*.cs`), which keeps all of the above.

## Rules that were learned by breaking them

- **Start every script with** `bpy.ops.wm.read_factory_settings(use_empty=True)` and run with `--factory-startup`:
  no default cube, no user add-ons, the same result on any machine.
- **Set `scene.render.fps = 60` before importing** a file whose clips will be exported again. The importer keys on
  whole frames of the scene's rate: at Blender's 24 a 1.2 s cheer (28.8 frames) came back 28 frames long. At 60 every
  clip of the game's is a whole number of frames.
- **Remove the importer's bone shapes** (an icosphere mesh object it adds for drawing bones) before measuring bounds,
  rendering or exporting; `rig.load` and both new scripts do.
- **Vertex colours are linear floats in Python** whatever the attribute stores: `(0.60, 0.05, 0.02)` is a strong red,
  not a dark one. They are per corner: index them by `loops[...].vertex_index` to choose by position.
- **Use `foreach_get` / `foreach_set` with numpy** for anything per vertex or per corner; a Python loop over 245,000
  corners is slow.
- **Import, export, render and `object.mode_set` (to make edit bones) are the only `bpy.ops` used so far, and they
  work with no window.** Everything else
  was done on `bpy.data` (attributes, armature settings, objects made and linked). Read, not run: other operators are
  said to fail with "context is incorrect" with no window and to want `bpy.context.temp_override(...)`.
- **EEVEE (`BLENDER_EEVEE_NEXT`) renders with no window on this machine**, materials and all, and is the only engine
  that shows an exported sculpt right. Seen on Turtwig: Workbench with `color_type = "VERTEX"` shows the body but the
  eyes' patch as a white mask over the face; with `"TEXTURE"` (what `render.py` uses, right for a textured Tripo
  sculpt) the body is grey and the eyes' patch black. `BlenderAnim render` therefore shows our sculpts' motion, not
  their look, until it is given EEVEE.
- **A skinned mesh's bounds and picture are of its pose**: the importer leaves a clip playing, and Turtwig measured
  1.055 tall, then 1.104 after a round trip, while its sculpt is 1.047 both times. Set
  `armature.data.pose_position = "REST"` (and `view_layer.update()`) before measuring or comparing sculpts;
  `turntable.py` does.
- `view_settings.view_transform = "Standard"` is what the turntable renders with, so that colours are as painted (the
  default was not tried beside it).
- **Reading a render back**: `bpy.data.images.load(path)` and `np.array(img.pixels[:])` (rows bottom to top, RGBA
  floats); tiles are joined with `np.concatenate` and saved through a new image, as both render scripts do. There is
  no Pillow in Blender's Python.
- **The exporter's two warnings on our files are harmless**: "multiple materials with different Vertex Color" and
  "More than one shader node tex image used for a texture".
- Export settings that the game reads are in `roundtrip.py` and nowhere else: change them there. Never Draco, never
  morph targets, images PNG or JPEG (`docs/model-files.md`, "What can be read").
- `dotnet run --project tools/BlenderAnim` builds the engine first: the first call in a session takes minutes. The two
  standalone scripts are run by Blender directly and start in two seconds.

## Writing an edit

An edit for `roundtrip.py` is a Python file with `bpy`, `np`, `meshes` and `arm` in its globals. The one that proved
the path (the top of the model, Turtwig's sprout, turned red):

```python
for o in meshes:
    me = o.data
    co = np.empty(len(me.vertices) * 3, np.float32); me.vertices.foreach_get("co", co)
    up = co.reshape(-1, 3)[:, 2]
    cut = up.min() + (up.max() - up.min()) * 0.78
    loops = np.empty(len(me.loops), np.int32); me.loops.foreach_get("vertex_index", loops)
    attr = me.color_attributes[0]
    col = np.empty(len(me.loops) * 4, np.float32); attr.data.foreach_get("color", col)
    col = col.reshape(-1, 4)
    col[up[loops] > cut, 0:3] = (0.60, 0.05, 0.02)
    attr.data.foreach_set("color", col.ravel())
```

To choose a part by bone instead of by place, the mesh's vertex groups should serve (not tried yet: see below).

## Sculpting a species from nothing

`tools/BlenderAnim/blender/sculptkit.py` builds a new mesh on the skeleton and clips of the game's own export, so a
new sculpt has all eleven clips from its first build. A species is a short script in `sculpts/` (the Sinnoh starters
are the examples; a build and its turntable take about twelve seconds):

```bash
"$B" --background --factory-startup --python tools/BlenderAnim/blender/sculpts/piplup.py -- "$S/h/models/Piplup.glb" "$S/new/overrides/models/Piplup.glb"
```

How the kit works, and why:

- **Parts are meshes made in numpy** (`ell`: an ellipsoid with a turn and a deform of the unit sphere, such as
  `boxy(2.5)` for a squarer head or a taper for a beak or a leaf; `tube`: a round tube through points with a radius
  each), each with a colour and a bone. `mirror=True` writes a part on +X and copies it to -X: **the model's left is
  -X**, where the `L` bones are, and `"{s}"` in a bone's name becomes `R` or `L`.
- **Fused parts are joined by a Remesh modifier in `VOXEL` mode and a Smooth modifier**, read back with
  `bpy.data.meshes.new_from_object(ob.evaluated_get(depsgraph))`: modifiers are evaluated with no window and no
  operator. A voxel of 0.007 to 0.008 on a model one unit tall gives about 45 thousand vertices, as the game's own.
- **Colour and weights come from the parts**: each fused vertex takes the colours of the parts nearest it (a
  `BVHTree` a part, blended over 0.012) and their bones (blended over 0.07), its four strongest kept.
- **`paint(mask, colour, on=[parts])`** recolours the fused body by a rule on the points (a collar's scalloped edge,
  a shell's band, a zigzag of toes with `tri`). Its edge is one voxel soft. `on` goes by the share of the point that
  is those parts': by the nearest part alone the edge was ragged wherever two parts met.
- **`decal(centre, normal, half sizes, colour)`** lays a patch of its own vertices over the fused surface (a polar
  grid cast onto it with `BVHTree.ray_cast`, lifted 0.003 to 0.009): its outline is exact, so everything that must be
  crisp is a decal (eyes with their highlight from a colour function of u and v, a face's mask, belly spots, ear
  linings). Decals stack by their lift. In the game they show no seam and no ink of the outline.
- **Pieces with `fuse=False`** (a beak, feet, a twig and leaves, a tail's flame) keep their own crisp colour and
  simply stand in the body.
- A new mesh needs its own material with a `ShaderNodeVertexColor` (`layer_name = "Color"`) linked to the base
  colour, or `export_vertex_color="MATERIAL"` exports no colours.
- **The rest pose must stand on the export's joints**: print them first (each bone's `head_local`) and put each
  part's pivot there. A pose from the art (Chimchar's crouch) belongs in a clip, not in the sculpt.
- The game stands every model at one height: a tall sprout or tuft makes the body smaller on screen.
- Git Bash heredocs fail on a script with apostrophes: write Python with the Write tool.

**People in the game** (2026-11-13; style guide, "Characters from files"; plan 47): the game now reads people from
files, a file a build, and `sculpts/people_chibi.py` and `people_grown.py` make them on `peoplekit.py`. What was
learned making them:

- **The loop for people**: `tools/dev/people.sh "$S/new" Kid` (18 s a chibi build, all 89 pieces), then
  `dotnet run -c Release --no-build --project tools/ShotHarness -- "$S/new" cast player dawn rowan` (boards
  `56_cast_NN`, a large turntable each, a battle each) beside the same in an out dir with no `overrides`;
  `SHOTS_FILTER=player ... lineup` gives the faces in every expression (`39_faces_*`) and the field sprites
  (`38_sheet_*`); `freewalk` with `SHOTS_FILTER=fw40` the grown kit. The harness copies the engine: after a change
  to the reader build `tools/ShotHarness`, not the engine alone.
- **A look falls back to the old sculpt without a word when its file lacks one piece it wears**, so a board can show
  the old kit and look "unchanged": check a distinctive shape, or ask `CharacterFiles.Missing`.
- **Frames come from the engine** (`people_frames.json`, written by `GOLDEN=accept dotnet test PokemonPlatinumTests
  --filter CharacterFilesTests`): never copy a joint into a script. Shapes are written in the game's axes through
  `E`, `T`, `Box`, `lathe`, `torus` (`peoplekit.py`); a `deform` works on the unit sphere in Blender's axes.
- **Never cut a window in a fused shell** (`build(cut=...)`): the rim is stepped, and a cut by angle from the head
  also removed everything below it (long hair's fall). Draw the unwanted part of the ball in under the skin with a
  `deform` before fusing (`cap` in `people_chibi.py`): the hairline is then the surface's own smooth edge.
- **Hats, brims, skirts and hems are lathes** (`lathe(c, [(radius, height)...])`, the outer side going up),
  unfused: crisp, a few hundred vertices, and they tilt with the engine's own `Pitch`.
- **A face's lines are rows of thin patches** (`arc`): a decal is geometry, so it cannot be see-through; a curved
  lid or a smile is five dashes along the curve. A patch lifted less than 0.005 flickers into the skin.
- **Eyes that read at the game's sizes are large and dark**: the first pair, sized like a realistic eye
  (0.034 by 0.05), looked like dots beside the old painted face; 0.046 by 0.066 with a pupil a third of the width
  and the iris showing round it is right for a child.
- **Voxel 0.012 with 7 rounds of Smooth** for hair (0.010 and 3 left it lumpy); 0.011 for the head.
- A thing with a string colour (`"#B0B4BE"`) in a piece of `colours="true"` is its own colour; a zone piece takes
  the arrays `P`, `A`, `W`, `K`, `STRIPE`, `SHADE`, `THIRD`.
- Four Blender processes at once build the four chibi files in under half a minute.
- **Check every look, not a board**: `CHARACTER_FILES=<folder of the .glb files> dotnet test PokemonPlatinumTests
  --filter CharacterFilesTests` writes `sweep.txt` there: each file's read time and memory, every look of the table
  in both kits that the files can't make (a missing piece, or a mesh past 65,535 vertices) and the largest mesh.

**The first draft of a person** (`sculpts/hero.py`, 2026-11-12, before the reader above existed): then the game read no character from a file (only species, through
`overrides/models`), so a person sculpted here can be seen on the turntable and nowhere in the game. The kit makes
its own armature for one (`Kit(name=..., skeleton=[(bone, parent, place)...], sides=("L", "R"))`: a person's `L`
bones are on +X, the other way round from a Pokémon's; edit bones are made with `bpy.ops.object.mode_set`, which
works with no window once the armature is the view layer's active object), on the chibi kit's own joints
(`CharacterModels`' `Kid` frame; the game's (x, y, z) is Blender's (x, -z, y)), with no clips. Getting one into the game needs a reader in the
engine, and what such a reader does with the present wardrobe, the faces' expressions and the speaking mouth is the
user's to decide before any of it is written.

**A model in pieces, and a wardrobe** (`sculpts/hero.py`, `sculpts/hero_outfits.py`): `build(name=..., props=...)`
makes one mesh object of what was added since the last build and forgets it, so a file can hold a body in parts, a
hair cut and garments, all on one armature. The hero's file is the format tried for clothes that can be changed:

- a node a piece, named `<slot>.<Shape>` in the game's own words (`body.torso`, `hair.Short`, `hat.Beret`,
  `top.Tee`, `bottoms.Shorts`, `shoes.Sneakers`, `bag.Backpack`; the slots are `clothes.json`'s);
- custom properties on the object, which `export_extras=True` writes as the node's extras and the importer gives
  back as `ob["key"]`: `colours` (`zones` or `true`), `tint` (whose colours fill the zones: `skin`, `hair`, `eyes`
  or the garment's own) and `hides` (the body's parts the piece covers, left out when it is worn, so nothing pokes
  through in a pose);
- **a piece of zones has no colour of its own**: its vertex colour is how much of each zone a point is (red the
  primary colour, green the accent, blue white, none of them ink), given to `add`, `paint` and `decal` as plain
  arrays, so one mesh serves every colourway and dye. Pure 0 and 1 come back through export and import exactly;
  the soft edge between two zones is a blend, which is what is wanted there.
- `build(cut=fn)` leaves out the fused faces whose middle `fn` picks (the window for the face in a shell of hair;
  its stepped rim is hidden by locks added as tubes), and `build(surface="body.head")` lays a piece's decals on a
  piece built before (the eyes and the face's marks are pieces of their own on the head).
- `hero_outfits.py` reads the file as an engine would and renders outfits side by side, with an arm raised and a
  knee bent: bones are turned about a world axis by setting `pose_bone.matrix` and calling `view_layer.update()`.
- Found on the board: locks of hair that start high poke through a hat (start them inside the shell, thin), and a
  hat that covers the brow hides the fringe, so a head in a hat reads as bare at the front.

- **A piece that gives its own light** is `add(..., fuse=False, glow="#FF7A2A")`: a material of its own with that
  emission at strength 1, set on its faces by `material_index`. The game lights a material as a glow once its
  emission is over 0.4 (`ImportedModels`), and one of 0.7 metallic or more as metal; seen on Chimchar's flame in a
  battle. Nothing else of a material reaches the game's surface materials.

What these files still lack beside a hand-built sculpt is under "What the round trip keeps and loses".

## Not tried yet

Record each here as verified or as failing, with what was run: moving or scaling a part by its bone's vertex group;
re-keying a clip (`animate.py` does it for a Tripo rig, not yet for one of
ours); baking vertex colours to a texture; anything on a form or a female look.

Known to be wrong for our rigs: `BlenderAnim inspect` on the exported Turtwig found `root` and `head`, took `body` for
the tail and found no legs (`rig.py` looks for a rigger's names and chains), so `animate` would key one of our sculpts
badly without `--map` or a rule for the body plans' bone names.

## Other skills

`~/.claude/skills/3d-modeling` (the user's, Apache-2.0) is general production advice for hand-modelled meshes (quads,
UVs, LODs, FBX): useful for a model made from scratch, mostly beside the point for our SDF sculpts. Third-party
Blender skills found on the web (2026-11-12) were generic `bpy` checklists with nothing on glTF or 4.5; none is
installed, and none should be without the user's word.
