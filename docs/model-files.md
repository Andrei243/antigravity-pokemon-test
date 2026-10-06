# Pokémon model files

Any species' 3D model can be replaced by a model file you supply (plan 03 · D5). The game uses it everywhere the
species appears: in battle, in the evolution scene, on the title screen and in the menu sprites, which are baked from
it. Species without a file keep their hand-built model (379 species: the whole Sinnoh Pokédex and the rest of Kanto and Johto; and 157 forms of them) or their generated one (everyone else).

## Where the files go

Put the files in a folder called `overrides/models`, in either of these places:

- the repository folder you start the game from (`dotnet run --project PokemonPlatinumEngine` from the repository
  root reads `overrides/models` there), or
- next to the game's executable (`PokemonPlatinumEngine/bin/<configuration>/net9.0/overrides/models`).

Git ignores every folder called `overrides`, so nothing you put there goes into the repository. Check `git status`
after adding files if in doubt.

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

A file with a single clip plays it as its idle. Actions are stretched over their time in battle (a move takes 0.65 s
and lands its blow 0.35 s in). For whatever the file has no clip for, the species' body plan moves the model as a
whole: it breathes and sways, leans into a strike, recoils from a hit and topples when it faints.

## Options

A `.json` file with the same name beside a model file can set:

```json
{
  "yaw": 90,
  "fill": 0.8,
  "hovers": true,
  "clips": { "idle": "Wait_Loop", "physical": "Kick", "faint": "Down" }
}
```

- `yaw`: degrees to turn the model round the vertical, for a file whose front isn't +Z.
- `fill`: how much of its sprite frame (and so its platform) the species fills, 0.45 to 1.
- `hovers`: whether it floats above its platform.
- `clips`: clip names by role (`idle`, `physical`, `special`, `status`, `hit`, `faint`, `entry`), where the names
  don't say.

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
