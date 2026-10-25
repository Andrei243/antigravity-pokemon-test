# Pokémon model files

Any species' 3D model can be replaced by a model file you supply (plan 03 · D5). The game uses it everywhere the
species appears: in battle, in the evolution scene, on the title screen and in the menu sprites, which are baked from
it. Species without a file keep their hand-built model (all 1,025 species, and 483 forms of them; a form that looks just like its species or another form shows that one's).

## Where the files go

Put the files in a folder called `overrides/models`, in either of these places:

- the repository folder you start the game from (`dotnet run --project PokemonPlatinumEngine` from the repository
  root reads `overrides/models` there), or
- next to the game's executable (`PokemonPlatinumEngine/bin/<configuration>/net10.0/overrides/models`).

Git ignores every folder called `overrides`, so nothing you put there goes into the repository. Check `git status`
after adding files if in doubt.

`assets/models` in the repository folder is read the same way, and **git tracks it**: it holds the approved models of
our own (the new species' sculpts), so they travel with the repository. A file of the same name in `overrides/models`
wins, which lets you try a draft without touching what is committed. Only models made for this game belong there, each
with a line in `docs/art/CREDITS.md`.

## Names

A file is found by the species' name or its National Pokédex number:

| Species | Any of these work |
|---|---|
| Pikachu | `Pikachu.glb`, `pikachu.gltf`, `0025.glb`, `25.glb` |
| Mr. Mime | `Mr. Mime.glb`, `mr-mime.glb`, `MrMime.glb`, `0122.glb` |
| Nidoran♀ | `Nidoran♀.glb`, `nidoranf.glb`, `0029.glb` |

Case, spaces and punctuation don't matter; ♀ and ♂ can be written f and m. A file named after a species replaces
its model whether that model is generated or hand-built.

A form (plan 03 · D11) is found by its own name, the species' and the form's as `species.json` spells them:
`Charizard-Mega-X.glb`, `meowth-galar.glb`, `Rotom-Wash.glb`. A form without a file of its own keeps its hand-built model if it has one, is generated from
its own data, or shows its species' hand-built sculpt if the species has one; it never takes its species' file.

## What can be read

glTF 2.0 files, binary (`.glb`) or text (`.gltf` with its `.bin` and textures in files beside it, or embedded):

- Meshes in triangles, strips or fans, with or without indices; normals (worked out if missing); one set of texture
  coordinates; vertex colours; quantized attributes (`KHR_mesh_quantization`); sparse accessors.
- Materials: the base colour (factor and texture, with `KHR_texture_transform`), alpha masks, emission (lit as a glow)
  and metal. Textures may be PNG or JPEG (baseline or progressive); those larger than 1024 pixels are halved until
  they fit, which is more than a model in battle or a 128-pixel sprite shows.
- The node tree with its transforms, skins with any number of joints (a rig with more joints than the shaders take
  at once is drawn in pieces), and animations (translation, rotation and scale; linear, step or cubic-spline keys).

Not read: Draco or meshopt mesh compression, WebP or KTX2 textures, morph targets, and textures other than the base
colour (normal, roughness and other maps). A file that can't be used is reported in the log (`MODELS:` lines) and
the species keeps its own model. To use a compressed file, open it in Blender (File → Import → glTF 2.0) and export it
again (File → Export → glTF 2.0) with compression off and images as PNG or JPEG.

## Size, place and facing

The model is scaled to a standard height and stood on the ground, so files in any unit work. How big the species
looks on its platform comes from its height in the Pokédex, like every other model. The front of a glTF asset faces
+Z; for a file that faces another way, see the options below.

## Animations

A file's clips are played for what their names say. The first matching word wins, in this order:

| Played for | Words looked for in clip names |
|---|---|
| Standing in battle | `battlewait`, `idle`, `wait`, `stand`, `breath`, `loop` |
| Physical moves | `attack01`, `physical`, `tackle`, `bite`, `strike`, `punch`, `attack` |
| Special moves | `attack02`, `special`, `cast`, `shoot`, `beam`, `attack` |
| Status moves | `status`, `buff`, `attack03`, `happy`, `attack` |
| Being hit | `damage`, `hurt`, `flinch`, `hit` |
| Fainting | `down`, `faint`, `death`, `die`, `dead`, `lose` |
| Coming out of the ball | `appear`, `entry`, `intro`, `landing`, `roar`, `cry` |
| Dodging a move that misses | `dodge`, `evade` |
| Standing in battle with its HP in the red, in place of the idle | `lowhp`, `low_hp`, `tired` |
| Cheering for a win, a catch or the other side's loss | `victory`, `cheer`, `win` |

A file with a single clip plays it as its idle, unless its options say `"onlyClipIsIdle": false` (below). Actions are stretched over their time in battle (a move takes 0.65 s
and lands its blow 0.35 s in, a dodge takes 0.8 s and is furthest aside 0.35 s in, a cheer takes 1.2 s). For
whatever the file has no clip for, the species' body plan moves the model as a whole: it breathes and sways, leans
into a strike, recoils from a hit, hops or dips out of a miss's way, droops when tired, hops for joy and topples when
it faints. A dodge clip plays the body's part only: the step aside, away from the side the miss passes on, is the
battle's and is added whatever clip plays.

## Shiny and animations

A shiny look of a model file is the same file with its texture recoloured by the species' shiny palette (`docs/art/style-guide.md`, "Shiny Pokémon"): nothing to make for it. A file without clips is moved as a whole by the body plan; `tools/TripoSculpt` can rig and animate a Tripo sculpt and names its clips for the roles above, and `tools/BlenderAnim` keys a clip for every role on any rigged file with Blender and renders sheets of them (its README); the harness's `clips` mode shoots a model file's clips in battle.

## Options

A `.json` file with the same name beside a model file can set:

```json
{
  "yaw": 90,
  "fill": 0.8,
  "hovers": true,
  "clips": { "idle": "Wait_Loop", "physical": "Kick", "faint": "Down" },
  "hide": ["Vine", "Effect"],
  "onlyClipIsIdle": false
}
```

- `yaw`: degrees to turn the model round the vertical, for a file whose front isn't +Z.
- `fill`: how much of its sprite frame (and so its platform) the species fills, 0.45 to 1.
- `hovers`: whether it floats above its platform.
- `clips`: clip names by role (`idle`, `physical`, `special`, `status`, `hit`, `faint`, `entry`, `dodge`, `tired`,
  `cheer`), where the names
  don't say.
- `hide`: parts of the file to leave out, by name: a mesh, or the node that holds it, whose name contains one of
  these words (any case) is not drawn and doesn't count in the model's size. For files that carry what shouldn't
  stand on the model: vines held ready, an effect, a second set of wings, lower-detail copies.
- `onlyClipIsIdle`: `false` keeps a file's only clip from playing as its idle, for a file whose one clip is an
  attack or a dizzy spell; it can still be named for another role in `clips`.

The harness makes its out dir its working folder, so it reads `<out dir>/overrides/models` and never the
repository's: to see your files in its shots, copy them (with their `.json`) there.

## Exporting a model

`dotnet run --project tools/ShotHarness -- <out dir> export [species ...]` writes species' current models as `.glb`
files into `<out dir>/models` (the hand-built ones when none are named): the mesh with its colours, the eyes and
markings as a texture, the skeleton and every clip of the body plan. Refine one in Blender and put it in
`overrides/models` to use it.

## Checking a model

`dotnet run --project tools/ShotHarness -- <out dir> dex <species ...>` renders the species' models as the game sees
them (add `--back` to see them from behind, as your own Pokémon is seen in battle), and `sheets` the menu sprites of
every species. Menu sprites are cached in `cache/sprites` next to the game,
named after a signature of the model (its file's size and date), so a changed file is baked again.
