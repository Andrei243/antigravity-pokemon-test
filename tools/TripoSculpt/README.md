# TripoSculpt

First-draft 3D sculpts of the new species, made by [Tripo](https://platform.tripo3d.ai)'s image-to-model API from the
concept pictures of `tools/GeminiArt`. A draft is cleaned up in Blender, then dropped into `overrides/models`. Nothing
here is shipped, and the output folder is git-ignored.

Uses Tripo's **V3** API (`openapi.tripo3d.ai/v3`); V2 is retired on 2026-11-01. The key is read from `TRIPO_API_KEY`
only (create one at platform.tripo3d.ai/api-keys).

```bash
dotnet run --project tools/TripoSculpt -- list                         # the pictures found, and which have a sculpt
dotnet run --project tools/TripoSculpt -- generate --set kanto --limit 1   # try one first: every sculpt costs credits
dotnet run --project tools/TripoSculpt -- generate Fakemon             # by species name or slug
dotnet run --project tools/TripoSculpt -- generate --image my.png --name Fakemon   # any picture
dotnet run --project tools/TripoSculpt -- install Fakemon              # copy an approved draft to overrides/models/Fakemon.glb
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
