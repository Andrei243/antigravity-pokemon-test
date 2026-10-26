# Gemini concept art

Concept pictures of the species and Megas of our own, drawn by Google's Gemini image model from the written "Look" briefs of `docs/plans/D14-proposals-*.md` and `R20b-mega-proposals.md`, for the sessions that sculpt their 3D models. **References only: not part of the game, not read by any tool, not shipped.** Nothing in the engine's project, the data importer or the harness copies `docs/art/reference/`.

- One JPEG per species, `<set>/<No>-<Name>.jpg` (Megas `megas/<Name>.jpg`), square, one creature on white. Sets: `kanto`, `johto`, `hoenn`, `sinnoh`, `unova`, `kalos`, `alola`, `galar`, `paldea`, `megas` (370 pictures).
- Made by `tools/GeminiArt` (`dotnet run --project tools/GeminiArt -- generate --set kanto`; the key is read from `GEMINI_API_KEY`). The prompt is the same for every creature (`prompt <name>` prints it), so a picture can be made again; a picture already there is kept unless `--force` is given.
- They sit beside the hand-drawn SVGs of `../d14-*` and `../r20b-megas`. Where a picture and the written brief differ, **the brief wins**: Gemini has never seen our sculpts, so a brief that starts "from our Raticate" is read as a new design and may drift from the forebear.
- What a sculptor can take: silhouette, proportions, colour blocking, where the new feature sits. What not: anatomy and poses are the sculpt's to decide, and no picture is a model sheet (one three-quarter view only).
- Credits: `docs/art/CREDITS.md`, "Generated reference art".
- `megadynamax/` (78 pictures, `R20c-mega-dynamax-proposals.md`): the Dynamax of a Mega-Evolved Pokémon. Its prompt names the base Mega and says it has Dynamaxed, and sends the base Mega's picture along: our own Megas' from `megas/`, the 63 official ones' official artwork, fetched by `fetch-official` into the git-ignored `tools/GeminiArt/.cache/` and never committed. These are derived from official art: **reference only; whether any may ship is a licensing question nobody has settled.**
