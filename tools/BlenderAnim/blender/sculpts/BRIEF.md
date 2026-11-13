# The brief every sculpting agent is given (plan 48)

The scratch paths are a session's own: replace them. A species' own lines (its number, forebear, examples and what to expect in the pictures) go with it.

Work in the repository worktree `F:\projects\antigravity-pokemon-test\antigravity-pokemon-test\.claude\worktrees\blender-learning-9e5d0e` (run every command from there with paths relative to it; never `cd`; do not commit or touch git; never run a bare `python -`).

Read first, in this order:
1. `.claude/skills/blender/SKILL.md`, the sections "Sculpting a species from nothing", "Building and installing a species" and "References, and what the first batches taught" (read ALL of the last: it is the lessons of forty-two species, among them "A face that reads", which the user has twice had to ask for).
2. `tools/BlenderAnim/blender/sculptkit.py` (the kit: `Kit`, `ell`, `tube`, `blade`, `sheet`, `leaf`, `lumpy`, `polygon`, `facets`, `boxy`, `tri`, `unit`, `inside`, `smoothstep`, `srgb`, `eye_look`, `eye_states`, `paint`, `decal`, `add(..., carve=True)`, `build`, `export`).
3. The worked examples named below.
4. The references, with the Read tool (look only; never copy them into the repository or use them as textures), in `F:\projects\antigravity-pokemon-test\references\pokemon\`: `<number>_<Species>_art.png` and `_3d.png` for each of your species (and its forebear's). Write each species' features down as a list from the PICTURES (they overrule this brief wherever they differ; say so in your report) and tick them against your turntables.
5. The skeletons: the sections `== <Species>` of `G:/Temp/claude/bb3a2cf7ebc9ae26/9d7f979c-f0c3-43b2-9ca3-5ca11351caca/scratchpad/bones.txt` (rest positions in Blender's axes: front is -Y, up +Z, the model's left is -X where the `L` bones are). The skeleton decides the pose. Put every part's pivot on these joints; give parts only bone names that exist, exactly as printed (`{s}` for R/L on mirrored parts).
6. The C# sculptors for the surfaces (`Coat = ...`, `mat:`): search `PokemonPlatinumEngine/Graphics/PokemonModels*.cs` for `PokeBuilder <Species>(`. Follow the references where the old sculpt differs. Female looks and other forms are not needed.

Write exactly one file a species and nothing else in the repository: `tools/BlenderAnim/blender/sculpts/<species in lower case>.py` (use the Write tool). Each must have: a `Kit(src, coat=...)`, surfaces on its parts, eyes in five states through `k.eye_states(lay)` with `eye_look` (`rings=20, segs=56`), and `k.export(out)`.

An evolution must read as the grown form of its forebear's sculpt (same colours and way of making eyes, teeth, fur and marks), not as a copy. Anything that must keep its edges is a loose piece; eyes and marks are patches on the FUSED surface, added BEFORE the body's `build()`; a mouth is carved (`carve=True`) where the design's is open; a crisp colour line is paint with a near-voxel edge and no `on`; a wide mask is paint in coordinates round the head's axis.

Build and look in your own scratch folder (create it), here called A:
```bash
B="/c/Program Files/Blender Foundation/Blender 4.5/blender.exe"; S=G:/Temp/claude/bb3a2cf7ebc9ae26/9d7f979c-f0c3-43b2-9ca3-5ca11351caca/scratchpad; T=tools/BlenderAnim/blender
"$B" --background --factory-startup --python $T/sculpts/<species>.py -- $S/h/models/<Species>.glb $A/<Species>.glb 2>&1 | grep -E "KIT:|Traceback|Error|line [0-9]+"
"$B" --background --factory-startup --python $T/turntable.py -- $A/<Species>.glb $A/<Species>.png 1100 2>&1 | grep -E "TT: bounds|Traceback|Error"
```
Open the PNG with Read (front, three-quarter, side, back) and compare with the references feature by feature. THE FACE FIRST: render the sheet at 1400 and crop the head (`"$B" --background --factory-startup --python $S/a2/crop.py -- in.png out.png x0 y0 width height`), and check the eyes' size, shape, slant and place, what cuts their top edge (a brow or a mask of fur makes the cut in many designs: no ring round the eye unless the design has one), the nose and mouth, the ears' size against the head. Iterate until each species reads at once and no `KIT: !!` warning prints. Watch the `KIT:` line (`height nan` is a bad deform or a `sheet` of no width; a tall crest, tail or ear makes the body small in the game; keep the body mesh under about 60 thousand vertices). Check one other eye state at the end (`... 1100 BLENDER_EEVEE_NEXT Shut`). Other agents run Blender too, which is fine, but do not run two yourself at once; do not run dotnet, the game or the harness.

Report at the end, briefly: what each script builds (vertex count, bounds), the features ticked and those you could not get right, where the pictures overruled this brief, what you wished the kit had, and anything in the skill that was wrong or missing.
