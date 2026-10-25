# TripoSculpt

First-draft 3D sculpts of the new species, made by [Tripo](https://platform.tripo3d.ai)'s image-to-model API from the
concept pictures of `tools/GeminiArt` (`docs/art/reference/gemini/<set>`). A draft is cleaned up in Blender, then installed into `assets/models`. The drafts
are not shipped; the output folder `out/` is tracked by git, so a draft is committed if you add it.

Uses Tripo's **V3** API (`openapi.tripo3d.ai/v3`); V2 is retired on 2026-11-01. The key is read from `TRIPO_API_KEY`
only (create one at platform.tripo3d.ai/api-keys).

```bash
dotnet run --project tools/TripoSculpt -- list                         # the pictures found, and which have a sculpt
dotnet run --project tools/TripoSculpt -- generate --set kanto --limit 1   # try one first: every sculpt costs credits
dotnet run --project tools/TripoSculpt -- generate Fakemon             # by species name or slug
dotnet run --project tools/TripoSculpt -- generate --image my.png --name Fakemon   # any picture
dotnet run --project tools/TripoSculpt -- install Fakemon              # copy an approved draft to assets/models/Fakemon.glb (tracked); --private: overrides/models (ignored)
dotnet run --project tools/TripoSculpt -- selftest                     # the whole pipeline against a fake server; free
```

`generate` does nothing until told which pictures (`--set`, `--all`, names or `--image`). Options: `--model` (default
`v3.1-20260211`), `--face-limit` (30,000 triangles), `--seed`, `--no-texture`, `--pbr`, `--autofix`, `--force`,
`--limit`, `--delay`.

- **PBR is off by default**: the game reads the base colour only (`docs/model-files.md`), so the other maps would be
  paid for and thrown away.
- **No credit is spent twice.** The task is noted in `out/<set>/<slug>.json` as soon as it is made; a run that is
  stopped or whose download fails goes on with that task next time. `--force` starts a new one.
- A run stops at once when the key is refused or the credits are gone.
- Every download is checked: a file that is not a binary glTF is refused, and one with Draco or meshopt compression or
  WebP textures is warned about, since the game cannot read them (export again from Blender with compression off).
- The key is not sent to the download link (a signed CDN address).

## After generating

1. Look at the draft (`dex <species>` or `pokemon <species>` in the ShotHarness once installed; the front of a glTF
   faces +Z, and `docs/model-files.md` has the options for a file that faces another way).
2. Clean it up in Blender: scale, orientation, one connected mesh, fewer triangles if it loads slowly.
3. Name clips as `docs/model-files.md` says if it is rigged; without clips the body plan moves it as a whole.
4. Add a line to `docs/art/CREDITS.md` for the generator's terms before any of it is relied on: check Tripo's terms for
   the plan used (free plans are often non-commercial or public).

## Rigging and animating

```bash
dotnet run --project tools/TripoSculpt -- rig Fakemon --limit 1        # check, rig, retarget: one file with the clips
dotnet run --project tools/TripoSculpt -- rig Fakemon --rig-type quadruped --animations walk
dotnet run --project tools/TripoSculpt -- install Fakemon              # takes the rigged file (--static takes the still one)
```

`rig` works on a sculpt that `generate` made (it needs the task noted in the sculpt's `.json`), in Tripo's three steps:
a **rig check** (what kind of body: biped, quadruped, hexapod, octopod, serpentine, aquatic, avian), the **rig** (the
humanoid model for a biped, the creature model for the rest), and a **retarget** of preset animations, played in place
(the game moves the model itself). The result is `out/<set>/<slug>.animated.glb`, one file: Tripo answers one GLB per
animation, and `GlbMerge` copies their clips into the first.

- **Clips are named for the game's roles** (`docs/model-files.md`, "Animations"): `idle`, `hurt` (being hit),
  `faint_fall`, `strike_slash` (physical moves), `special_shoot` (special moves). A biped gets those five by default.
  Tripo has one walk for each other kind of body, which becomes `idle_walk`: a lone clip plays as the idle. Moves, hits
  and fainting then fall back on the body plan's motion. `--animations` names others (at most five; `preset:` is added).
- **Cost:** a rig, plus a retarget (the task's credits are noted in the `.json` as `rig.credits`). Try one first.
- **Every step is noted as soon as it is made**, so a run that stops goes on with its tasks and pays nothing twice;
  `--force` begins again. The note keeps `rig.type`, the task ids, the presets and the clips.
- **Check it in the harness** before relying on it: the skeleton's fit to a creature body is Tripo's guess.
