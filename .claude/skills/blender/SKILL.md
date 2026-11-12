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
  in a shot); and, by `docs/model-files.md`, the shiny look becomes a recolour of the texture.
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
- **Import, export and render are the only `bpy.ops` used so far, and they work with no window.** Everything else
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

## Not tried yet

Record each here as verified or as failing, with what was run: moving or scaling a part by its bone's vertex group;
smoothing or sculpting through modifiers; re-keying a clip (`animate.py` does it for a Tripo rig, not yet for one of
ours); adding geometry and weighting it; baking vertex colours to a texture; anything on a form or a female look.

Known to be wrong for our rigs: `BlenderAnim inspect` on the exported Turtwig found `root` and `head`, took `body` for
the tail and found no legs (`rig.py` looks for a rigger's names and chains), so `animate` would key one of our sculpts
badly without `--map` or a rule for the body plans' bone names.

## Other skills

`~/.claude/skills/3d-modeling` (the user's, Apache-2.0) is general production advice for hand-modelled meshes (quads,
UVs, LODs, FBX): useful for a model made from scratch, mostly beside the point for our SDF sculpts. Third-party
Blender skills found on the web (2026-11-12) were generic `bpy` checklists with nothing on glTF or 4.5; none is
installed, and none should be without the user's word.
