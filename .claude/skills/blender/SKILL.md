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

# 2. Look at it: front, three-quarter, side, back and then the game's own two views (dex, dex --back) on one sheet,
#    a face seen from its inner side in ink (a piece wound inside out is black), and what the file holds (TT: lines)
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

## Building and installing a species (plan 48 · BL1)

```bash
dotnet run -c Release --project tools/BlenderAnim -- sculpt Turtwig Chimchar Piplup     # or: sculpt all
```

It exports the C# sculpt with every model file switched off, builds `sculpts/<species>.py` twice, installs the file
into `built/models` (ignored by git, read by the game alone, copied by `dotnet publish`) only if the two builds are the
same bytes, and prints what the game's reader finds in it: eye states, surfaces, clips. To see it in a shot, copy it
into `<out dir>/overrides/models` as before: the harness and the tests never look in `built/models`.

Run in a git worktree, `sculpt` also installs each file into the main checkout's `built/models`, where the game is
played from (the folder is ignored by git, so a merge carries no model). A script written since the tool was last
built is not found with `--no-build`: the scripts are copied beside the tool as it builds. A form is a script of its
own named without the hyphen (`gastrodoneast.py` for `Gastrodon-East`, `mimejr.py` for `Mime Jr.`), built on the
form's own export and grown from its species' script; shiny looks in the games' own colours are owed as a second
palette a script and a second file a species (plan 48, "Shiny looks in the games' own colours, and forms").

A species is at parity with a hand-built sculpt only with both of these, so every new script has them:

- **Eyes in five states.** Build the body, then `k.eye_states(lay)`: `lay(state)` adds the eyes' decals for a state
  and each becomes a piece `eyes.<State>` on the body. `eye_look(state, open_look, lid, ink)` makes the colour function
  of each state from the open eye's (shut a line curving down on the lid's colour, a smile curving up, a squeeze two
  lines meeting at the inner corner, fierce the lid slanting over the open eye); `+u` is outward on both eyes. The lid
  is the colour round the eye (Piplup's white disc, Chimchar's cream face); what stays whatever the eye does (Turtwig's
  yellow ring) is painted back after it. Use `rings=20, segs=56` for an eye: a state's line is a seventh of the eye.
- **Surfaces.** `Kit(src, coat="scales")` and `add(..., surface="shell")`: a material a surface, named as the game
  names them (`skin`, `cloth`, `hair`, `leather`, `plastic`, `metal`, `fur`, `scales`, `shell`, `leaf`), each face of
  the fused mesh taking the surface of the part it is nearest. Take them from the species' C# sculptor (`Coat = ...`,
  `mat: Shell`), which is what parity is against.
- The kit removes the export's old materials as it starts and reuses a material by name, or Blender numbers the
  second one (`body.001`); the game's reader ignores such a number all the same.

## References, and what the first batches taught

- **Reference pictures live outside the repository**, in `references/pokemon/` in the folder above it
  (`<number>_<Species>_art.png`, the design's drawing, and `_3d.png`, its 3D model), fetched with the user's leave
  from the PokeAPI sprites repository (`sprites/pokemon/other/official-artwork/<n>.png` and `other/home/<n>.png`; a
  form by its own number, Giratina's Origin Forme 10007). Open both with Read before writing a species, write down
  its features as a list, and tick them against the turntable. Never copy one into the repository or use it as a
  texture.
- **Order**: the order a player meets them in (the user, 2026-11-13); a species' skeleton is printed first
  (each bone's `head_local`) and every part's pivot put on it.
- **A patch that misses the surface** (a decal over thin or loose geometry) used to leave its points where its rays
  began, 0.35 off the model, and the game then measured the model too large. The kit drops them and prints
  `KIT: !! a decal ... missed`; lay a patch only on the fused surface, and watch the `KIT:` line's width and length.
- **The turntable shows the open eyes only.** With all five pieces drawn, every face looked fierce.
- **The user's corrections so far, as rules**: eyes on a round head sit further apart than feels right (Chimchar,
  Piplup: aim about 0.45 of the way from the middle of the face to its side).
- **Shapes in the kit beyond `ell` and `tube`** (each first written by an agent inside a species' script): `blade` for a horn, prong or fin (a tube squashed about its own line; squashing about the part's middle pushes its far end off), `sheet` for a collar or membrane, `leaf` for a flat pointed piece along a direction, `lumpy` for a crown or a cloud of fur, `polygon` for a patch with corners (chest points, a mask). `turntable.py` takes an eye state after the engine (`... 480 BLENDER_EEVEE_NEXT Shut`).
- **Agents sculpt well from this skill** (2026-11-13: six evolutions and then six wild Pokémon, three agents at once, two species each): give each the skill, the kit, two worked examples of a like body, the references, the skeletons' file and its own scratch folder; let it touch only its two scripts; build, install and shoot in the game yourself. Colours and shapes follow the references even where the old C# sculpt differs.
- **A mirrored patch's `outline` is not mirrored** (only its colour function's `u` is): an outline that is not symmetric comes out wrong on the other side, so lay it as two patches. Left as it is because tuned sculpts rest on it.
- **A patch catches whatever its rays meet first**: laid behind a limb it lands on the limb. Make such a mark a small fused part instead.
- **A mouth is carved**: `add(name, shape, colour, bone, carve=True)` cuts the part out of the fused body (a Boolean after the remesh) and lines the hollow with its colour. The cutter must break the surface (centre it on the surface, keep it thin) and stay inside the head's width, or it notches the cheeks; its colour spreads a voxel onto the rim, so on a convex snout it reads as lips: aim it into an overhang. Fangs are loose pieces seated just inside the rim (`zubat.py`, `onix.py`, `machop.py`).
- **Anything that must keep its edges is a loose piece**: a beak, a band (a loose `sheet` along the shell), a peak, spikes of a fringe, a flipper of two extruded slabs (`empoleon.py`'s `slab`). Hard rock is a convex hull with its coplanar faces merged and shaded flat after `build()` (`onix.py`'s `stone` and `rock`): the game reads the file's normals. The kit's `facets` alone stays soft.
- **A crisp line between two colours of the body** is two paints with no `on`, kept to the body by a mask of place, with an edge under a voxel (`inside(d, 0.0025)`); `on=` blends where parts of two colours meet (`shinx.py`).
- **Patches**: a tiny dot (a pupil) is a patch of its own, or it comes out a star; a patch wider than what it is cast on wraps round as a streak; `polygon` is star-shaped about the patch's own middle, so a long band is cut into near-convex patches; a long throat marking is painted along the neck's centre line, not laid as one oval patch.
- **Watch the `KIT:` line**: `height nan` is a fractional power of a negative number in a deform (clip first); a raised tail, crest or forelock raises the height and the game draws the body smaller; a script's `out` is its export path (never a loop variable). A bone the import numbered (`hindR.001`) is named so on its parts: print the joints before borrowing names.
- **Creases between fused parts remesh rough** and speckle under the coat: fill the crease with a set-back part and keep one part's surface from running along another's. A flame's yellow heart is two thin copies on its two faces. Leaves fan out along each lobe's outward direction. A tail that rises behind a round head shows over it from the front.
- **A wing is loose flat plates**: a base plate and coloured panels thicker than it, so each panel shows on both faces, front and back (`beautifly.py`'s `flat`, which triangulates any outline; `slab` fans from the outline's mean and folds a notched one).
- **A face that reads** (Infernape, after the user found it odd): look at the head alone at 1400 and tick it against the references feature by feature: the size and slant of the eyes first (small almond eyes under a brow are an `outline=polygon(...)` patch a side, its top edge overlapping the brow; the iris a patch of its own in the same frame, in the Open and Fierce states only), then whether the muzzle stands forward, the ears' size, the fur's silhouette. Two patches laid by hand have `u` toward +X on both sides: flip the left one's colour function and outline. A face patch misses where an ear stands in front of the head.
- **The references overrule the brief**: an agent told of feelers or a yellow rim that the pictures do not show follows the pictures and says so.
- **Mouths and marks go in before the body's `build()`**: a patch added after it and before `eye_states` falls into `eyes.Open` and shows with open eyes alone. The lid colour given to `eye_look` is whatever is painted under the eye, or closed eyes wear a ring; on a body dark all round give the closed states a light line.
- **A big eye on a round head** wants a flattened face part fused in, or the patch wraps. A skeleton with one leg (`legR` at x 0, Hoothoot) takes the leg on `legR` unmirrored.
- **`sheet` gives NaN normals where its width is nought** (a leaf's point at u 0 or 1): keep u off the ends (`0.015 + 0.97 u`). A thin painted stripe wants an edge near the voxel (0.004 at 0.005), not far under it.
- **Gas, smoke or fluff with no transparency** is opaque loose puffs in rings behind and round the body, the face left clear and the body showing through the middle from behind (`gastly.py`); jitter their places, or they read as petals.
- **An evolution is grown from its forebear's script**: give the agent that script as its example and say so; it keeps the line's colours and its way of making eyes, teeth and fur (Staravia, Bibarel, Luxio).
- **A wide mask or chest mark on a round head** is paint in coordinates round the head's upright axis (angle × radius, z) against a polygon with a soft edge (`luxio.py`'s `poly_mask`), not a wide patch, which streaks. A brow that must stay over the eye in every state is a wrapper round `eye_look`. A mask is painted, not patched, where a mouth is carved: a patch would lie in the hollow, while the carve recolours after the paints.
- **A hard-edged cell with a face** (Combee) is a loose prism with a thin fused plate standing just proud of its front for the patches; paint under eye patches is flat, or closed eyes show a ring.
- **An eye cut by a mask or brow** (Luxio, the user's second face): no ring round the eye unless the design has one. Clip each piece's outline (white, iris, pupil) by the cut line and paint the mask's edge a little under that line, so the patch's edge is the crisp line; work the mask's polygon out from the eye's own top line. A Fierce lid is a convex patch of its own in the mask's colour. A top line sloping to the nose scowls; an arched lid is calm. Thin strokes in an eye patch (lashes) are about 0.6 of the patch long and stay inside its unit circle.
- **Polygons for a mask round a head are in radians times the radius**, not metres. A fused part of another colour bleeds onto a painted face: paint the face's own colour back over that place. Paint over a carved rim is ragged. Teeth along a carved grin follow the opening's real rim.
- **Look at every model from the side and behind before it is called done**, and at its silhouette against the reference: a loose plate can stick out as a box (Lopunny's ear lining; paint a lining on a thin ear instead), a moved part can float, a neck can be a pillar on a hump where the design is one mass (Gastrodon).
- A tube's end cap reaches under the ground by its radius. Mirrored parts are renamed `nameR` and `nameL`: match them by `startswith`. The `KIT:` count includes loose pieces. A mane that streams back takes `tongue` with a small upward `side` and a flick near 0.25.
- **Six agents at once** worked (2026-11-13, twelve species), each given the shared brief (`tools/BlenderAnim/blender/sculpts/BRIEF.md`) and a few lines of its own.
- **A crest, fin or plate in the midline plane is a bar from the front** (Staraptor): build it as a volume with width and look at the front view first. Held things go on the side the art shows them; on the turntable's front view the model's -X is on the viewer's left... check with a marked part before trusting it. A mouth's teeth sit about 0.02 behind the surface the lips were carved from; a jaw's parts go on the `jaw` bone where the skeleton has one. `polygon` answers a far point where a ray slips between two outline points: cap it. The turntable frames by height, so a crop's place moves when the height does.
- Never run a bare `python -` or `blender --python -` here: the first hangs, the second looks for a file named `-`.
- Judge a face on a turntable of 1100 or more; a part off its joint cannot be seen at rest, so check its first point against the skeleton.
- **`inside(d)` is 1 where `d` is below nought**: a region within a radius is `inside(distance - r)`.
- **`paint(on=[...])` goes by a name's beginning**: `tail` also takes `tailtip`; name parts with that in mind.
- **Give a fused part its majority colour and paint the minority**, with the same-coloured neighbours in `on`: a part painted whole to match its neighbour leaves a pale ring where it meets a third (Wurmple's skull).
- **On a patch that faces sideways `+u` runs toward the back** on both sides of a mirrored pair.
- **The skeleton decides the pose**, even where the art stands the species upright (Buizel is a quadruped here).
- **Patches are cast on the fused surface alone**: a loose piece in front never catches one (an ear's lining under a loose forelock lands on the ear).
- **Every loose piece with `glow=` is a material and a primitive of its own**: join a flame's tongues into one piece a bone and colour before adding them (`ponyta.py`). A long body is a loose boulder a joint, each on its own bone, with the head fused for its eyes (`onix.py`); a belly line on a curved body goes by the body's centre line (`wurmple.py`).
- **Ways of making a feature that worked**: a ragged sail is a flat ellipsoid whose outline is scaled by a sine of
  the angle (Giratina's wings); bars down a curved front are one patch with a colour function banded in `v`; a
  zigzag edge is `tri` of the angle round the part (a ruff, a crown, toes); puffs of fur are fused balls; a tooth is
  a `boxy(5)` ellipsoid left loose; a spike that hangs from a sail starts thin inside it, or it pokes out the back.

- **A face is judged small.** The game draws a head some 60 pixels across: render the turntable at 480 too. An eye
  is a solid pupil in a white with a rim a sixth of its half-size or none; a thin ring, a row of tiny teeth, small
  pads on fingertips and the like turn into noise (Ambipom, Gliscor, Mr. Mime). A mouth is one clean shape; a patch
  on the fused face often reads better than a carved hollow full of loose teeth.
- **Whiskers, antennae and other thin loose tubes** widen or heighten the model, so the game draws its body smaller,
  and in pixel art they come out heavier than they are: short and few (Purugly).
- **In the turntable's front view +X is on the viewer's right**; a feature on one side of the art goes by that.
- **A `sheet` closed round in a ring shows a seam where its ends meet**, even overlapped: put the seam at the back or
  under another piece. Two loose pieces sharing an edge plane flicker: offset one by a few percent. A parameter of
  a sheet that jumps with the angle leaves a step along the edge: keep every one continuous.
- **Tiles on a dome** (a honeycomb) laid as rings only work near the equator; lower down the columns converge. Loose
  faceted pieces need their faces wound outward from a point.
- **`sculpt --no-build` builds the scripts as they stood at the last build of the tool**: after a script is written
  or changed, run `sculpt` without it (a model that "did not change" was this).
- **`render.py`** takes a JSON job (`input`, `out`, `frames`, `size`) and wants a `report` key, or it ends with a
  `KeyError` after writing its sheets.
- **A fish's midline fins are edge-on in the game**: the Pokédex and a battle look at a model from the front
  three-quarter and from behind, never side-on, so a dorsal or tail fin in the body's own plane is a pole over it.
  Keep it lower than the body is tall and bend it to one side along its length (some 40 degrees), with the thickest
  plate the one whose colour should show at the edge; eyes on flat sides come forward onto the cheeks and turn about
  40 degrees off straight ahead.
- **A shell** is loose sheets with a darker lining at 0.9 of its radius; knobs and spike roots on it stay shallower
  than the gap, or they poke through the lining. Knobs want `sub=3` (a `sub=4` ball is some 2,500 vertices).
- **A fan with ribs** is edge-to-edge wedges of alternating colour: ribs laid over a bent plate stayed hidden.
- **A thin mark that is not convex** (a W, a zigzag) is paint in a local frame; as a decal its edge is ragged even
  at `rings=40`. An eye's white edge goes between two rings of its decal, never on one.
- **A fused limb tip of another colour bleeds up the limb**: paint the limb's colour back on its side of the line and
  the tip's on the other, both with sharp edges.
- **A part on the head bone that sits inside the body's outline** (a crown on an egg) must stay inside it, or it
  leaves a shelf.
- **A full disk cuts a copy short without a word**: after `No space left on device`, compare what was installed
  byte for byte and remove what differs.
- **Mirroring by hand**: multiply only the offsets by the side, never an anchor point that is off the midline.
- **Thin black paint lines on a fused body hatch** at voxel 0.006 whatever the edge and with or without `on`: they
  read clean at the game's size, and a loose thin strip is the way where they must be crisp up close. A band as wide
  as 0.03 (a stripe on a back) hatches the same way: a lens-shaped decal narrower than the back, cast from behind,
  is crisp, and so is a collar under a chin cast from a little below.
- **A flat plate triangulated from its outline alone** (the `flat()` the fish scripts copy from one another) is long
  triangles from edge to edge: once it is bent they cut straight across the curve and bury whatever is laid on the
  true surface (a fin's flecks were not seen at all). Split its faces (`sub=3`) before bending.
- **A hard cap with a crisp edge** (a dome on a skull, a helmet) is a loose piece meshed from its top down to a rim
  on a plane, with a lip turned in under the rim, over a smaller fused skull; the eyes stay patches on the fused
  skull under its rim. Sinking the lower half of a ball into the head leaves a scalloped rim.
- **A hard-edged slab** (a wall, a window's frame and pane) is an extruded polygon whose faces each keep their own
  points. A loose piece with a rounded base wider than a thin slab (a horn on a wall) pokes through its other face.
- **A pupil with a round edge** inside an eye whose rim is ragged is a patch of its own over the eye's patch.
- **A plus or an X with corners** is a decal over a `polygon` outline; rounded ends want `segs` of about 120.
- **`paint` without `on` recolours fused lips and whatever else lies under it**; and `inside(a) * inside(b)` with
  limits that exclude one another is a mask of nothing, which prints no warning.
- **A helmet, a hat, a mushroom**: a signature part that should BE the head (Cranidos's dome, Shieldon's shield) reads
  as something worn when it sits on top of a head that is whole without it, overhangs it all round or shows a neck
  under it from behind. The head fills it from behind and the face runs up into it. And a biped reptile stood on
  the skeleton's joints with straight columns for legs reads as a person: thighs fused into a leaning body, short
  shins.
- **A flat tail behind the body** (Pikachu's bolt) runs back and some 35 degrees out to the model's +X side
  (`unit(0.6, 0.8, 0)`): nearly straight back it was hidden behind the body from the front three-quarter and
  edge-on from behind. An agent cannot see the game's cameras, so the main session checks such a part in the shot.
- **A patch wider than the thin part it is meant for** (an ear's lining) lands on whatever lies behind it (the tail,
  a metre away) and prints no warning, since its rays did hit something.
- **A family is sculpted in two waves** (Eevee and its seven): the base form alone first, written to be grown from
  (its frame in named constants and small functions, the calls at the end), seen in the game and corrected; only
  then its evolutions, each agent told to keep that frame and change what the pictures change. The eight then read
  as kin: the same paw, the same way of making a face.
- **A long-legged skeleton is not a thin animal** (Umbreon and Leafeon came out as fawns on four sticks, with a
  small head on a pole of a neck, and the other five would have): the joints are high, but shoulder and thigh are
  broad masses fused over the upper half of each leg, the trunk is as deep as the leg showing under it is long, the
  lower leg tapers and is never a stick, the neck is short and thick, the head some 15% larger than first measured.
- **Eyes that are always shut** (Snorlax): the open state is the slit itself, Shut, Squeeze and Smile come from
  `eye_look` on it, and Fierce is written by hand (the same slit tilted): `eye_look`'s Fierce draws a lid line
  across the slit.
- **A large fused mass of another colour** (a mane, a crown) washes its colour over the coat at `build()`'s default
  `band=0.012`: `band=0.004` keeps the edge.
- **A line drawn inside a patch's colour function** (a lid cutting an eye) steps at the patch's rings: make the cut
  the patch's own outline (`polygon`), or use the outer rings as the lid.
- **A paint mask limited by a ball round the head's bone** misses the face standing forward of it and says nothing;
  a decal cast from above whose plane starts over the neck streaks a line down the neck (start it just above the
  back); a lining painted with `on=[ear]` has a ragged edge where one without `on` is crisp; a fused blade thinner
  than two voxels frays (thin plates are loose pieces); a three-point `blade` is a rhombus (a lobe wants four or
  five points).
- **A loose piece built on a hand-made frame keeps that frame right-handed** (side = face × direction), the
  mirrored twin too, or its faces are wound inside out. Blender's turntable draws both faces and shows nothing
  wrong; the game culls the outer faces and draws the inside: Togetic's wings came out thin, grey and see-through,
  and only the game's shot showed it. A wing that looks grey or glassy in the game is first a question of winding.
- **A flyer's level wings are edge-on from the game's cameras**: each pair is raised in a V of 25 to 35 degrees
  and the pairs swept apart; a plate is lit on the face turned to the light, so the pair meant to be seen from
  behind tilts its lit face that way (Yanma's hind pair). Hanging legs half a body long make a dragonfly a spider:
  tucked, a third of the head's height.
- **Eyes that are a visor or a mark of the design** (Yanmega) keep the eye's own colour as the lid in the shut
  states, or the face loses its signature when it blinks. **One eye in the middle** (Magnemite) is laid without
  mirroring; `eye_look`'s Fierce slants one way on it.
- **A hollow mark with two crisp edges** (Togepi's rings and triangles) is two stacked `polygon` patches, the
  colour and a smaller one of the ground's colour at a higher lift; its band a third of the mark's width, or it
  reads as wire at the game's size. An eye's dark outline over a sixth of its radius is a heavy ring there
  (Munchlax, Magnemite: 0.09 reads as a line).
- **A decal is cast from its centre pushed out along its normal**: with a tilted normal the centre goes near the
  surface, or the patch lands off its mark. A loose disc on a dome is lifted, or its rim sinks and shows a
  crescent. A check that keeps vines out of a region samples each arc more finely than its control points.
- **Crisp stripes round a limb or a body** (Elekid's line): a loose ring, a `sheet` wound round the part standing
  0.004 off its surface with its seam behind, has no hatching at any size, where paint steps up close. A band mask
  in paint is `inside(z - top) * inside(bottom - z)` (`inside(d)` is 1 where `d` is below nought): turned round
  it paints nothing and says nothing.
- **An eye on a loose faceted piece** (Porygon) lies on a thin fused plate just proud of the facet, shaded flat
  after `build()` so it takes the facet's light. Loose pieces sit between the fused faces and the decals in the
  mesh: a script that finds them by counting from the end breaks once a decal is added before `build()`.
- **A curved blade at a limb's end** (Scyther's scythe) cannot show its face to both of the game's cameras: turn
  its plane some 55 degrees out from straight ahead and accept the far one edge-on in the three-quarter view.
  A flared rim of petals (Magmortar's muzzle) reads as floating discs unless the petals are thick, nearly close
  the ring and start inside the barrel's wall.
- **The main session holds the game's shot beside the reference BEFORE it sends a correction**: Lickitung's broad
  flat tongue and great tail were the art's own, and a correction written from the game's shot alone had them
  made thin and small; a second round put them back.
- **Thin plates in close pairs alias into stripes at the game's size** (Dialga's crest and fan first came out as
  brushes of hatched lines, its claws as wisps): a model is some 150 pixels tall there, so every blade, claw and
  spike is ONE solid piece, a flat-faced hull with a kite section at least 0.03 thick at its root, showing a lit
  face and a shaded one. A crest standing up sets the model's height and shrinks the body: laid back it does not.
- **A body made wholly of loose hulls** (Nosepass, Porygon) carries its eyes on one thin fused plate inside the
  front; a patch's outer edge stays inside the plate's flat part, or it loses faces with no warning but a lower
  vertex count. Hulls are wound outward by a check on each face's normal against the hull's middle.
- **A glyph of strokes** (Unown): `unown.py` builds a letter from a description (an eye and a list of lines,
  arcs and dots swept as round tubes), and Fierce on its one middle eye is a V cut into the white's own outline.
  A stroke's root stays off the eye's white, or the eye's patch lands on it.
- **An aura round a core** (Rotom): an opaque shell of its own colour set a little BEHIND the core, so it shows as
  a rim from the front and leaves the face free. A pattern that radiates from a point on a ball (Palkia's pearl
  and ribs) is one patch cast down that point's axis.
- **A colour edge that crosses a crease** (a cap's edge over a cheek) is ragged as paint: a patch over the front
  whose outline is the edge, with paint only filling below it. A frame built as `stack([a, cross(a, up), up])`
  is left-handed: use the negative of the cross product.

## The kit's hard-edged shapes and its winding check (the kit session, 2026-11-14)

What eighteen batches of scripts had each written again for themselves is in `sculptkit.py` now. **A new script uses
these and writes none of its own**; the scripts written before keep theirs, since a built model's bytes must not
move (the session held it: every model built by the new kit is the file the old kit built).

| Shape | What it is for | Was |
|---|---|---|
| `hull(points, merge=3.0)` | A solid with flat faces and hard edges from its corner points: rock, armour, a blade, a claw, a gem, a box. Each face has points of its own, so it is shaded flat with nothing more said | `rhyperior.py`'s `hull` with its `finish()`, `onix.py`'s `stone`, Porygon's faces |
| `slab(outline, origin, ex, ez, thick)` | A flat plate with a hard rim from any outline that doesn't cross itself (concave too), in the plane of `ex` and `ez`: a wing, a fin, a bolt, a star | `yanmega.py`'s `flat`, `uxie.py`'s `slab`, `beautifly.py`'s `flat` |
| `ring_band(c, axis, radius, length, thick=0.006)` | A loose band round a limb, a tail or a body, its seam behind; `radius` may be a function of v for a part that tapers. Some 0.004 over the part's own radius | `electivire.py`'s `ring_band` |
| `lathe(c, d, profile, seg=40)` | A closed solid of turning with the edges its profile has, `(radius, distance along d)` from the axis round to the axis: a barrel with its bore, a drum, a screw's head | `magmortar.py`'s `lathe` |
| `outward(shape)`, `volume(V, F)`, `winding(V, F)`, `axes(d)` | A closed shape turned outward whichever way it came; its signed volume; what is wrong with its winding, or None; a right-handed frame round an axis | `magmortar.py`'s `outward`, `volume`, `axes` |
| `sibling(name)` | Another sculpt's script as a module, for a form built by the same functions: `u = sibling("unown"); u.unown(src, out, DESC)`. The script asked for builds under `if __name__ == "__main__":` | new (Unown's letter A built by import is the built file, byte for byte) |

- **Each of these that is a closed piece ends by asking its own signed volume and turning its faces if they came
  out inside out**, so it is right under any frame, left-handed ones and `mirror=True` included. A shape made by
  hand is passed through `outward(...)` before `add`.
- **`Kit.add` checks every loose piece** (`fuse=False`): points welded by place, each closed shell's signed volume,
  and faces wound against each other. The piece being built is told of in one line, `KIT: !! N loose pieces wound
  wrongly (inside out): names...`, which `BlenderAnim sculpt` prints with the kit's other lines. It only tells:
  nothing is turned for a script, so no built model changes. A piece that is open somewhere (one sheet of faces)
  can't be told and is passed.
- **`leaf` is wound inside out and stays so**: its frame `(side, face, d)` is left-handed. Turned over in the kit,
  a model that only fuses a leaf was built to other bytes (Infernape), so the kit keeps it and a loose leaf is
  `outward(leaf(...))`.
- **What an inside-out loose piece looks like in the game**: its own colour gone dull and flat with no outline
  round it (the session's test star), or grey and see-through (Togetic's wings). In Blender's own render nothing shows, since it draws both sides of a face alike; **culling back faces
  shows nothing either on a thin plate** (the far face is then drawn in the near one's place, 0.014 further off).
  So `turntable.py` draws a face seen from its inner side in ink: an inside-out piece is black on the sheet.
- **`turntable.py`'s sheet has six views**: front, three-quarter, side, back as before (so a crop by tile still
  lands), then the game's own two, as `dex` and `dex --back` take them (10 degrees up; 31.5 degrees round from the
  front, the model's face turned to the viewer's left; from behind over the other shoulder). Held against the
  harness's shots of Eevee and of a test model: the same sides, the same parts hidden. They are for seeing what
  the game's cameras hide before the game is asked; the game is still the judge.
- **`tools/BlenderAnim/blender/kitcheck.py` is the kit's own check** (Blender runs it in a few seconds, no model
  built, exit 1 on a finding): every closed shape of the kit wound outward under right-handed, left-handed and
  slanted frames, and the winding check's answers. A shape added to the kit gets its lines there.
- **A change to the kit is proved on every built model**: each script run once with the changed kit on the
  harness's export of its species (`$S/h/models`, which is the export `sculpt` makes, byte for byte), six Blenders
  at a time, some twenty-five minutes for 215, and `cmp` with `built/models`. `sculpt all` builds each twice one
  after another and takes three hours.
- **What that proof found the first time it was run**: the kit moved nothing, but six installed files were older
  than their own scripts (corrected after the last `sculpt` of their batch and never built again), so the game
  here showed another build than a release would make; and sixteen models carried loose pieces wound inside out
  since their batches, each drawn dull and unoutlined and none of them noticed (Burmy's and Wormadam's leaves,
  Roselia's petals, Hippowdon's teeth, Combee's cells). **After an agent's last correction the main session runs
  `sculpt` again on that species before the batch is committed**, and reads the kit's lines for `!!`.
- **`outward` is a common name**: a script with a parameter or a function of that name (the Magnemite line's
  `screw(..., outward, ...)`, `magmortar.py`'s own `outward(name, shape)`) calls the kit's as `sculptkit.outward`.
- **A face's orientation is never decided against a point in the face's own plane** (Vespiquen's cells: the back
  cap was turned by the prism's centre with the cap at depth nought, and rounding turned its triangles each its
  own way): take a point inside the solid.

## What the first batch with the new kit taught (Heatran to Arceus, 2026-11-14)

- **Why a wide, low model is cut by the game's board, exactly** (`PokemonStudio.Strip`): the board frames a square
  of 1.3 × max(height, 0.85 × max(width, length)) and sees the model turned 31.5 degrees, so a model whose four
  corners are feet shows 0.853 × width + 0.522 × length across. A model longer than it is tall fits when its width
  is at most 0.68 of its length. Heatran was cut at 0.90 × 0.87 and still at 0.75 × 0.98 (the main session had
  asked for the feet in sideways and apart lengthwise, which lengthens the square and the showing width alike),
  and stands whole in its cell at 0.64 × 0.98 (seen at four times its size, from both sides). Give an agent the
  sum, not "narrower". **The sum is an upper bound for a footprint whose corners are filled**: a round or oval
  one shows far less (Shaymin, 0.44 × 0.48 and 0.35 tall, would be 18% over by the sum and fills its cell with
  room to spare), so it is asked only of crabs, toads and tables.
- **A bowl, a collar or a cup made of a `sheet` shows its far wall through its own opening** (Darkrai's collar:
  the far teeth as black-notched triangles): close it with a floor of its inner colour just under the rim.
- **A reference drawn from the three-quarter shows a symmetric pair as two unlike parts** (Cresselia's lower ring
  wings read as one wing behind and one below): count pairs before the brief says a number.
- **A small head on a long neck has no face in the game** (Arceus at 0.17 of the height: two specks): a head a
  third larger than the reference's ratio reads, and the game's shot, not the ratio, decides.
- **Fingers, feelers and ribbons that hang to the ground** take the eye from everything else (Regigigas): as long
  as the picture's, never as long as the limb. A ribbon drawn broad (Phione's feeler) is a cord at a third of the
  width.
- **Loose pieces added after `build()` fall into the first eye piece** as patches do: every loose piece goes in
  before the body's `build()`.
- **`glow=` on a pale loose plate renders it white on the turntable** and hides its colour: judge the colour with
  the glow off.
- **The new shapes held**: seven scripts used `hull`, `slab`, `ring_band`, `lathe` and `outward` and none wrote
  its own or printed `KIT: !!`. Asked next: a torus, a seamless closed band, a gem, a swept ellipse along a curve,
  a chain of hulls along a path, an ellipsoid along a direction, a rosette of petals, a spline.

## What the wave of forms and Kanto taught (2026-11-15)

Forty forms of Sinnoh and Kanto's 103 remaining species, by some forty agents, up to twenty at once (a line of
evolution, or two short ones, to an agent).

**How a wave of many agents is run.** The base of a line builds under `if __name__ == "__main__":` and its
evolutions import it with `sibling`. An agent's own instructions are three or four lines that point at two files in the
scratch folder (the standing instructions and the line's own task), so a launch costs the main session nothing to
repeat when the limit of agents is reached. The main session's first look takes **no build**: the agent's own last
`.glb` is copied into a scratch folder's `overrides/models`, the harness's `dex` and `dex --back` shoot it, and the two
cells are cropped at twice their size and laid beside the two references (art | 3d | game front | game back), four
models to a sheet. The tool's own build (twice) and install are for a script's last version only, one tool at a time
(a lock folder), queued while the agents work. A session that is restarted stops its agents and keeps their
transcripts: a message to each resumes it; a background build an agent was waiting for is gone and is started again.

**What the game's shots sent back**, each several times, and now in the brief:

- **Stilts**: a quadruped or a squat biped held high on four straight cylinders with daylight under the belly
  (Bulbasaur's line, the female Nidoran's, Krabby and Kingler, Tauros, Ninetales). The belly within 0.05 to 0.08 of the
  height of the ground, upper legs as fat haunches fused into the flanks, a stub of shin and a wide foot, the rump one
  mass from behind. `bulbasaur.py`'s `underside()` and `legs()` are the worked example.
- **A slender figure with its arms in a T** where the design is stocky (Squirtle's line, Meowth, Hypno, Jynx,
  Hitmonchan, Mewtwo, Kabutops): measure head width / body width and limb thickness / body width as well as the
  heights.
- **A head too small**: when in doubt a tenth larger than measured.
- **Thin strokes**: a tube loses some 0.004 of radius to the remesh and the smooth, so a stroke, whisker, tail or leg
  thinner than 0.03 across is wire in the game (Unown's letters read thinner than their references at every width).
- **A tail or crest of stacked boxes** (Pidgey's line): a fan of a few thick tapering plates whose faces the back
  camera sees.
- **Tails that spring from the wrong place** (Ninetales, three rounds): say where a part's root is (the tail bone's
  joint) and which way it goes, in numbers, when a round comes back with the part elsewhere.

**Smaller things the agents found:**

- A carved hollow wider than a few voxels does not take its cutter's colour (the smooth moves its walls out of the
  colouring band): paint its inside by distance from the mouth's axis (`lining` in `bellsprout.py`). A second cutter of
  another colour kept inside the first gives a mouth a floor (`grimer.py`). A deep tilted bore can come out of the far
  side with no warning.
- A `decal`'s winding is decided by its first, tiny triangle: with `eps=0.005` a hand-laid eye came out inside out
  (drawn in ink on the turntable); a lower `eps` cured it. A decal laid near a dome's top with the default `up`, on a
  steep crease (where a jaw meets a skull) or grazing a neighbouring part breaks, turns black or becomes a long streak,
  with nothing printed: move its centre onto plain surface, give it another `up`, turn a face away from its neighbours.
  A patch under an overhang (a trunk, a nose) names its centre and a normal that looks forward. A `polygon` outline
  must be star-shaped about the patch's middle, an inset one too.
- A sideways patch with `mirror=True` mirrors its colour function only: an outline that is not symmetric is laid as
  two patches by hand, the left one's outline negated.
- A line drawn inside a patch's colour function steps at the patch's rings: ease the colour over about one ring
  (`soft` in `poliwag.py`, whose spiral is crisp at 64 rings by 224 segments). A rim's threshold sits between rings,
  never on one.
- A loose band round a fused tube sits flush at the tube's radius minus 0.003. A band over a squarish section is
  lifted 0.0065. A `sheet` wound into a closed tube has its seam at v = 0: start the angle on the side nobody sees.
- A `tube` bent sharply folds along the inside of the bend: a reared chest or neck is fused ellipsoids.
- Studs, teeth and claws on a loose piece are loose hulls buried in it; fused, they remesh into pimples.
- A cream ball fused to brown limbs takes brown onto itself: paint the body's own colour back with an ellipsoid mask.
- A function's default argument that names a module constant is fixed when the function is defined: an evolution that
  changes the constant through `sibling` reaches only code that reads it at run time.
- Several like units (heads, eggs, pegs) are one function or class placed by a frame, each on its own bone
  (`diglett.py`, `doduo.py`, `koffing.py`); a script that builds as it is read cannot be imported, so every base builds
  under `__main__`.
- The two Nidoran are `nidoranf.py` and `nidoranm.py` (`Sculpt.KeyOf`: a gender sign is f or m); reference files carry
  no leading zeros and keep an apostrophe (`83_Farfetch'd_art.png`), and a build named without it is found all the
  same by the review's script.

**Asked of the kit** (for its next session, most of them asked by three agents or more): a torus; a spiral or coil
(a tube along a shrinking spiral in a turned plane) and a spiral-shell; a spline with a belly frame (`Line` in
`ekans.py` and `dratini.py`); a fur ball with tufts and a scatter of spikes; a feather fan; a cone; a swept solid and
a chain of hulls along a curve; a curved tapering plate; a crisp band along a curved tube and a banded cone; an
ellipsoid laid along a frame and a placement "on an ellipsoid at azimuth and elevation" (`on_ell`, copied into six
scripts); a polygon eye helper that mirrors its outline; a decal turned about its normal and a patch with a rim; a
soft-edged colour helper; a whole-sculpt offset; **a decal oriented by the mean of its faces** (the inside-out eye
above is a fault of the kit's, not a wish).

## What Johto taught (2026-11-15)

Sixty-four species by twenty agents, with the wave before's lines on stilts, arms in a T, small heads and thin strokes in
the brief from the start: five were sent back, against some forty of Kanto's hundred and three. What was new:

- **A script is judged from a build made after its last edit**, whoever makes it. An agent can hand back with edits it
  never built; build the script before looking.
- **The review sheet can show the game's own sculpt by mistake**: a name with no build in the folder given is drawn from
  the C# sculpt. The review script prints `!! no build of <Name>` then, and that line must be read (Skarmory "stood on
  stilts" a second time that way).
- **Scratch folders are case-insensitive on Windows**: `A` and `a` are one folder, and two agents overwrote each other's
  run script. A folder is named after its task (`h17`).
- **A task's dex numbers can be wrong**; the reference files go by the national number and the name
  (`270_Lotad_art.png`), and the name decides.
- **A big flat part must face one of the game's two cameras**: Phanpy's great ears, laid along the head, showed to
  neither; turned 35 degrees out they read from both.
- **A decal's centre must lie on the surface**: placed past a tapered snout's tip, its ray misses the head and lands on
  a shoulder, a thigh or the chest, with nothing printed. A decal beside a raised limb picks the limb up as a streak;
  the only sign is the eyes' `KIT:` line showing a wider width and length.
- **What is in every eye state goes before `build()`** (cheek marks); anything added inside `lay(state)` is copied into
  that state's eye piece alone, which is also how loose eyes that blink are made (Sableye's gems).
- **A fused tube's rounded cap reaches past its last point by its radius**: below the ground under a foot, or through
  the face of a loose piece set at that point. End the tube 0.85 of its radius short.
- **A `ring_band` round a remeshed body stands off it** and reads as a can's rim from behind; segment lines on a smooth
  body are painted.
- **A paint mask from two coordinates catches the far side of a round part**: limit all three.
- **Paint cannot give a crisp edge along the crease between two fused parts of different colours**, even at a band of
  0.004; a loose piece, or one mass painted by a mask, does.
- **A species the game draws hovering need not reach the ground**: legs as long as the skeleton's height make stilts
  (Vibrava); make them the design's length and let them hang.

## What Hoenn taught (2026-11-15)

A hundred and seven species by thirty-two agents; fourteen sent back. What the game's shots sent back is the last line of
the brief (`BRIEF.md`): stilts and trousers still, small heads on tall figures, legs longer than the design's, a
crest laid at the wrong angle, wings as rods, limbs that dwarf the body, a shape misread. What the agents found:

- **Say what each shape in the picture is before building it.** Slakoth's raised arm, curled over its head, was built
  as a ring-shaped tail; nothing in a build can catch that, only the game's shot beside the reference.
- **A wing or a great ear is judged from the game's two cameras**, not the turntable's front: a level plate is an edge
  to both. Make it broad at the root and bank or sweep the pair, as a mirror pair, until neither camera sees it
  edge-on; the far wing foreshortened is fine. Never turn the two wings of a pair differently to show a face to each
  camera (Latias and Latios were first accepted so, and put back): the field's four facings and the battle's other
  shots see the lopsided pair.
- **A patch laid on the side of a coiled tube streaks across to the next turn of the coil** with nothing printed
  (Rayquaza's rings were long bars in mid-air); paint the marks in the coil's own frame. The sign is a body mesh
  with more vertices and long faces.
- **A wide round hollow cut with `carve` leaves a ragged, striped wall** (Lunatone's crescent): sweep a tube along
  the middle line of the crescent instead.
- **An open mouth without a carve**: a jaw dropped as its own fused ellipsoid with a coloured mass set back inside
  gives a big open mouth and a stable vertex count (Huntail, Walrein).
- **A body of loose hulls carries its eyes on a fused plate that is a dark part of the design** (a black frame, a
  socket), so its edge needs no hiding (Beldum's line, the Regis). On a flat front such a plate shows its rounded rim
  once it stands more than about 0.002 proud; 0.001 to 0.0015 hides it.
- **A wide band of colour on a ball is one patch cast from the front with the band's outline**, crisp where paint
  hatches as the ball turns away (Registeel); lift the dots above it.
- **A black sliver past the outline** can be a patch that overhangs the silhouette, not a piece wound inside out:
  move it in or lower its `eps`.
- **Small skeletons** (a body joint at 0.1) want a voxel of 0.004 to 0.0055 and `band=0.006`.
- **A board check on the built mesh** (the span seen at the board's angle against its square) is six lines at the end
  of `metagross.py`; a four-cornered model that fails it is scaled whole, never by drawing its feet in.
- **The kit's wish list grew** (each written again in several scripts): a spike or claw hull along a direction, a
  torus, a helix or spiral tube, a ribbon or streamer along a curve, a partial or toothed lathe (`pad_sweep` in
  `lotad.py`), a leaf or feather plate with a roll toward a camera, a lock of hair, a cut gem, a crystal prism, a
  cone, an eye outline that mirrors itself, a decal that ignores parts in front of it, a crisp colour edge along a
  crease between fused parts, a group of parts scaled about a joint.

## What Unova taught (2026-11-21)

A hundred and fifty-six species by forty-seven agents; nineteen sent back. What the game's shots sent back is the last
line of the brief (`BRIEF.md`): nine sets of arms in a T that their reports called "forward and down", small heads,
a feature built too small to be the design's largest mass, tusks and blades grown from the wrong place. What the agents
found:

- **Exports are 0.1 to 1 unit tall, and every absolute number scales with the height**: the voxel (about a hundredth
  of it), the thickness floor (0.03 of it), decal lifts, `band` and `wband` (the default `wband` of 0.07 is wider
  than Joltik's whole leg offset: 0.02 there). Tynamo is 0.11 tall and builds at a voxel of 0.0019.
- **"Forward and down" is checked in numbers**: the hands' x inside the body's half-width and their y in front of the
  belly. An arm that only points 45 degrees down from the shoulder is still a T from the game's front camera.
- **A `polygon` outline must contain the patch's own middle**, or it answers 9 with nothing printed and the patch
  spreads over the whole face (an L-shaped glyph, a scowl bar, an offset drip): centre the patch on a point inside the
  outline. `min(1, polygon(...))` keeps an oval and lets a straight line cut its top, which is how an eye is cut
  crisply by a mask or a brow (`cut_outline` in `tirtouga.py`, `eyes` in `larvesta.py`).
- **Thin lines inside one patch's colour function come out as a barcode**: lay each line as its own thin patch over
  the first (teeth as triangles over a dark mouth in `trubbish.py`, Scraggy's grin).
- **A crisp colour edge between two fused parts** is drawn a few thousandths beyond the fillet, by distance from one
  part's own ellipsoid, not at the crease (`crisp_edge` in `solosis.py`).
- **A decal goes to whatever its ray meets first.** Marks on a ball's sides land on arms held in front of it
  (Darmanitan's ovals on its fists), and a patch on the side of a lone tube, tilted toward a camera, streaks onto the
  body behind (Tornadus's swirls): keep such a patch smaller than the tube is wide, or limit marks to the angles no
  limb covers. Nothing is printed; only the picture shows it.
- **A band painted round a pear** needs an elliptical reach, or it goes patchy at the back (Krokorok); a band with
  plain limits on a head is a hard rectangle, and a superellipse in angle and height reads right (Larvesta's face).
- **A puff or ruff of fused balls shows ridges** unless the balls overlap heavily (28 round a ring) with `smooth` 6
  (Jellicent); pointed tufts smooth into bumps after the remesh, so points that must stay sharp are loose hulls.
- **A flat blade along a long curve renders as a round tube at these sizes**: flatten to about 0.4 and widen it.
  Overlapping slabs of different colours flicker unless one stands clear of the other by less than its thickness.
- **Colour that must show on both faces of a thin wing** is stacked slabs, the marks thicker than the wing so they
  come through both sides (Volcarona's spots, Meloetta's notes).
- **A pair of fins or ears that runs straight back is an edge to the back camera whatever its bank**: turn each some
  45 degrees outward in plan (Eelektross's head fins, Haxorus's tusks at 58 degrees).
- **A model several times longer than tall is seen nearly end-on by both game cameras** (Tynamo): a midline fin there
  reads as a plate across the body. It is the design, not a fault of the build; say so in the report.
- **One frame for species that are one design** (`genie` in `tornadus.py`, `Head` in `deino.py`, the
  gears of `klink.py`): the siblings pass only colours and an extras function.
- **Hidden eyes** (under hair, under a dome) are still five states: patches of the covering's own colour.
- **Traps that print nothing useful**: a local variable named `out` in a species function shadows the export path
  and fails inside the glTF exporter; a part named with `{s}` but without `mirror=True` fails at `build()` as
  `KeyError: 'wing{s}'`; the `KIT:` line's "height" is the highest point, not the span; `turntable.py` takes the
  eye state after the engine argument; a sheet written again under the same file name can be shown from a cache, so
  give each look a new name.
- **The kit's wish list, again** (each written in several scripts once more): a spike or claw hull along a direction,
  `on_ell`, an eye outline that mirrors itself, a ribbon along a curve with a rim, a flame, a fan of feathers, a
  torus, a gear or toothed lathe, a mouth with teeth, a group of parts scaled about a joint, the board check as a line
  of `build()`, a warning when a decal's ray lands on another part than the one aimed at.

## What Kalos taught (2026-11-21)

Seventy-two species by twenty-four agents; three sent back (an upright dinosaur, a tongue as a level bar, a head lost
under its hair). What the agents found:

- **Which mode or form is the species** is what the game's own sculptor and the 3D picture show, not always the
  drawing (Xerneas's drawing is its Active Mode): build that, and keep the other look as an argument of the species'
  function for the form's script later.
- **A part standing in front of an eye catches the eye's decal** when it is within the cast's start along the eye's
  normal (0.35 out): Clauncher's claw took a dark streak, and hands held before Gourgeist's lantern face took its
  triangles. Make that part a loose piece, or keep it out of the ray. Nothing is printed.
- **An eye under an overhang** (a dish, a brim, a brow) can look right head-on and be hidden in both game views
  (Noivern): judge its place on the crop of the two game views.
- **A pupil drawn inside the eye's own colour function steps at the rings**; lay it as a patch of its own with a
  `polygon` outline (Scatterbug's upright pupil, Honedge's slit).
- **A patch laid across the crease where two fused parts meet is stepped** (Pumpkaboo's eyes on its head and collar):
  move it off the crease.
- **A small decal on lumpy fused ground that comes out black** is cured by a lower `eps` (0.002 to 0.0012), as for
  hand-laid eyes (Florges's flowers).
- **A decal cast straight down on a low coil's top misses the surface** (four warnings on Zygarde): marks on a coil
  are paint in the coil's own frame, on its top as on its sides.
- **A paint mask that is a half-space reaches every part it is allowed on**: a collar's paint turned the shell behind
  it green (Chesnaught). And a mask written with its sign turned round (`inside(0.12 - |x|)`, a throat painted in
  front of a limit instead of behind) paints the wrong half or nothing, with nothing printed: only the sheet shows a
  missing sock or a dark mouth.
- **An ellipsoid flattened by clamping is narrow at its flat end**, so it is no bell or hood: use a `lathe`
  (Heliolisk's folded frill). A `slab` has straight rims, so stacked plates read as boxes (Avalugg).
- **A helper that takes a share of a length must not be handed a length** (Fletchling's chevron became a rod 0.1 too
  tall); name the argument for what it is.
- **A dish, an ear or a ring of marks that must mirror** wants a frame whose `u` runs outward on both sides
  (up = cross(n, d) in `noibat.py`'s `ear_dish`), so one outline serves left and right unflipped.
- **A skeleton may lack the bone the design wants** (the shrimps have one arm bone, Skrelp no tail or fin bone): put
  the part on the nearest bone that moves as it should and say so; the clips show whether it holds.
- **An export's bounds read about a unit** when its bone-shape meshes are counted: take the height from the bones
  file or skip those meshes.
- **The tool builds from a copy of the scripts made as it starts** (`dotnet run` copies `blender/**/*.py` beside
  the tool): a script edited while a long `sculpt` run is going is built from its old self, and the built file's date
  is then newer than the script, so a check by dates passes. Do not correct a script a running install has yet to
  reach; or build it again afterwards and look at that build.
- **A name with an accent keeps it**: `flabébé.py`, UTF-8, and `sibling("flabébé")`; any helper that makes a
  script's name from a species' must keep letters as `Sculpt.cs` does (`char.IsLetterOrDigit`).
- **The kit's wish list, once more**: `on_ell`, a spike or claw hull along a direction, an eye outline that mirrors
  itself and cuts with a lid, a ribbon along a curve, a torus, a rock hull and a crystal, a bevelled or tapering slab,
  a group of parts scaled about a point, a warning when a decal lands on another part or a `polygon` misses its
  middle.

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
