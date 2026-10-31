# Contributing

Thank you for wanting to help. This remake is built over many sessions toward a long roadmap, so the first thing to read is where it stands.

## Where to start

- **The roadmap** is [`docs/plans/`](docs/plans/README.md). [`00-overview.md`](docs/plans/00-overview.md) is the order of what is left, in waves of sessions that run side by side. Each plan lists its sessions, what each must deliver and when it is done, and its status at the end; pick a session that isn't ticked and read its plan first. When a session ends, tick its status and write down what was decided in it.
- **How the code is laid out**, every command of every tool, and the rules that have caused bugs are in [`CLAUDE.md`](CLAUDE.md). It is long because it is the project's memory: read the part about what you are changing.
- **The art rules** are [`docs/art/style-guide.md`](docs/art/style-guide.md). When a visual rule has to change, change the guide before the code.

## The asset rule

**Nothing is taken from the Pokémon games**: no sprites, models, textures, music, cries or script text. Art, sound and dialogue are our own; a scene follows the original's beats in our own words.

- The importers (`tools/DataImporter`, `tools/MapImporter`) may read **layouts and numbers** from the decompilations and PokeAPI: tile behaviours, heights, positions, short names, a move's power. They never write a vertex, a texture, a palette, a sound or a line of dialogue into the repository, and their caches stay git-ignored. Their READMEs say exactly what they read.
- Free-licensed assets (OFL, CC0, MIT) can be used once the maintainer agrees, each with a line in [`docs/art/CREDITS.md`](docs/art/CREDITS.md); the title's CREDITS screen reads that file, so a new line shows in the game.
- Follow **Platinum**, not Diamond and Pearl, wherever they differ.

## Checking your work

- `dotnet build PokemonPlatinum.sln` builds the game and every tool.
- `dotnet test PokemonPlatinumTests --filter "FullyQualifiedName~<Class>"` runs the tests of what you touched; add tests for new rules (movement, scripts, battle effects, data). Golden files under `PokemonPlatinumTests/Golden/` hold what must not move: accept a change on purpose with `GOLDEN=accept` and say why in your description.
- `dotnet run --project tools/ContentCheck` holds every hand-written overlay, room and script to its rules (a line too wide for the text box, a door to nowhere, a flag nothing sets). Run it after changing any of them.
- **Anything drawn** is looked at through `tools/ShotHarness`, which runs the game in a hidden window and saves pictures: run the mode before and after your change and `diff` the two folders (CLAUDE.md, "Checking visual work"). Show the before and after.

## Pull requests

CI builds and runs the tests on every push (`.github/workflows/ci.yml`). Keep a pull request to one session or one fix, say which plan it belongs to, and include the harness's pictures for anything that changes what is on screen.
