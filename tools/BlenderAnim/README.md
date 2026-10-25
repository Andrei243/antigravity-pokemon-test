# BlenderAnim

Keys the game's battle clips on a rigged `.glb` with Blender, run with no window, and renders contact sheets of a
file's clips to look at. The rig is usually Tripo's (`tools/TripoSculpt rig`), but any armature in a glTF file will do.

```bash
dotnet run --project tools/BlenderAnim -- inspect <file.glb>                 # its bones, the bones found for each role, its clips, what the game plays for each role
dotnet run --project tools/BlenderAnim -- animate <in.glb> [--out <file.glb>] [--only idle,hit,...] [--map role=bone[,bone]]
dotnet run --project tools/BlenderAnim -- render <file.glb> [--out <dir>] [--frames 6] [--size 400] [--only <clip>,...]
```

Every command takes `--blender <exe>`; otherwise the `BLENDER` variable, the PATH and `Program Files\Blender Foundation`
are searched (`winget install BlenderFoundation.Blender.LTS.4.5`). Written against Blender 4.5 LTS.

## animate

Writes `<in>.battle.glb` beside its input (never over it) with ten clips, one for each role `docs/model-files.md`
lists, named so the game's matcher gives each its role: `idle_breath`, `attack01_lunge`, `attack02_cast`,
`status_buff`, `damage`, `faint`, `appear`, `dodge`, `tired` and `victory`. `--only` keys some of them. A clip the file
already has that would play in place of one written (Tripo's lone `idle_walk` is the idle) is dropped, and the tool
says so; the file's other clips stay. It ends by reading the file as the game does (`ImportedModels.MatchClips`) and
printing which clip plays for each role, and ends with 1 if a role written isn't its new clip.

The poses are a table in `blender/animate.py`: a few poses a clip, each a role's turn ("pitch" tips the front down,
"roll" turns about the body's length, "yaw" about the vertical) and the root's "move" forward, up and to the side, in
shares of the model's height, eased by Blender's curves. The times follow the battle's: a move takes 0.65 s and lands at
0.35 s, a dodge is furthest aside at 0.35 s; the game stretches a clip over its own time anyway.

**Bones by role** (`blender/rig.py`): the root, the spine (the "spine" bones that carry the head), the neck and the head
are found by name; legs and the tail by where their chains go, since a rigger's names can't be trusted for them (Tripo
called Ratchisel's tail a limb and put "Tail_0" in a front paw). A chain off the trunk that drops to the floor more
steeply than it runs is a leg, in front of the body's middle or behind it; the one that runs furthest back from the rear
half is the tail; wings are "wing" by name. `inspect` shows what was found and `--map role=bone[,bone]` corrects it.

The front is the game's: glTF's +Z (Blender's -Y). The poses suit a quadruped best; a biped's legs get the same swings.

## render

A sheet a clip (`<dir>/<clip>.png`), each the clip at `--frames` evenly spaced moments side by side, seen from the
model's front right as the battle sees a foe, rendered with Workbench. Open them with the Read tool.

## Seeing it in battle

The harness's `clips` mode shoots a species' clips in battle, the player's lead and the foe both the species:

```bash
mkdir -p <out dir>/overrides/models && cp <file>.battle.glb <out dir>/overrides/models/<Species>.glb
dotnet run --project tools/ShotHarness -- <out dir> clips <Species>
dotnet run --project tools/ShotHarness -- <out dir> contact 99_clips
```

The harness makes its out dir the working directory, so a model file for it goes in `<out dir>/overrides/models`, not
the repository's. `tools/TripoSculpt install` puts an approved file where the game itself reads it.
