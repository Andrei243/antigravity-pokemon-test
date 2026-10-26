# Packs

A pack is a folder of files laid over the game's own data (plan 16 · T14, T15): new lines for people, a trainer's
team, a script, a song, a species' numbers, a room, a model. The game reads its own data first and each pack after it,
in the order the options give, so a later pack wins where two change the same thing. Nothing of a pack is written into
the game's folder or the repository, and a game without packs is exactly what it was.

**A pack is its maker's own work.** Like the game itself, it holds nothing taken from the Pokémon games: no sprites,
models, textures, music, cries or script text. Lines are written in your own words, songs are your own tunes, and
anything free-licensed you use is named in the pack's credits with its licence.

## Where packs go

Put each pack in a folder of its own inside a folder called `packs`, in either of these places:

- beside the game's executable (`PokemonPlatinumEngine/bin/<configuration>/net10.0/packs/<your pack>`), or
- beside your saves (`dotnet run --project PokemonPlatinumEngine -- --where` says where that is; then `packs/<your pack>` there).

The next time the game starts it finds the pack and loads it. OPTIONS, PACKS lists every pack found, on or off, in the
order they load, with each one's credits: A turns a pack on or off, Left and Right move it earlier or later. A change
there takes hold at the next start, because the data is read once, before anything else.

`--packs none` starts the game with no pack at all, whatever the folders hold, and `--safe-mode` does the same.

## The manifest: `pack.json`

Every pack has a `pack.json` at the top of its folder:

```json
{
  "name": "gate-lanterns",
  "version": "1.0",
  "author": "Your name",
  "credits": "Written for this game by Your name; the tune is my own. Nothing in it is from the games.",
  "gameVersion": "0.1",
  "after": ["another-pack"],
  "description": "One line on what the pack does."
}
```

- `name`: what the options and the saves know the pack by. It must be a valid folder name, and only one pack of a name is loaded.
- `version`: any text. A save remembers the packs it was played with and their versions; the title says so when a game is continued without one, or with another version.
- `credits`: **required**. Whose work the pack is, and the licence of anything in it that isn't yours. The packs screen shows it, and a pack without it is not loaded.
- `author`, `gameVersion`, `description`: optional.
- `after`: packs this one must load after, whatever the options' order.

## What a pack can hold

A pack is laid out like the game's `Data` folder. A file at the same path replaces the game's whole:

| Path in the pack | What it changes |
|---|---|
| `scripts/<place>.txt` | A place's scripts (`docs/scripts.md`). A file of a new name adds scripts a person can name. |
| `world/<region>/overlays/<area>.json` | An area's music, people, their lines and its signs (`docs/data-files.md`, "World files"). Copy the game's file and change it. |
| `world/<region>/areas/…`, `chunks/…` | An area's events and a chunk's tiles. |
| `maps/<Room>.json` | A hand-made room. |
| `music/<folder>/<song>.mml` | A song (`docs/music-format.md`). A new id is a new song an overlay's `bgmTrack` can name. |
| `text/<language>/…` | Words; a pack's `ui.json` replaces the game's whole, so copy it first. |
| `overrides/models/<species>.glb` | A model (`docs/model-files.md`); a later pack's is looked at first. |

### Patches

Six files are changed a field at a time rather than replaced, with a file of the same name ending `.patch.json`:
`species`, `moves`, `abilities`, `items` and `characters` by `name`, and `trainers` by `id`:

```json
[
  { "id": "veteran_grant", "party": [ { "species": "Riolu", "level": 34, "moves": ["Force Palm"] } ] },
  { "name": "Bulbasaur", "category": null }
]
```

Each field given replaces the field whole; `null` takes it away; an entry whose name or id isn't there is added. In
`characters.patch.json` (an object, not a list) objects are merged key by key all the way down.

## What can't change

- `world/<region>/world.json`: what is open of a region is always the game's own. A pack's copy is never read.
- The save's format, the battle rules written in code and anything else that isn't a data file.
- A new region: the regions are the game's.

## Checking a pack

```bash
dotnet run --project PokemonPlatinumEngine -- --validate-packs
```

checks every pack beside the game and the saves without starting it: the manifest, every JSON file, each patch laid
over the game's file and read as the game reads it, the overlays and rooms, every script (with the same checks the
game's own scripts get: a song, an item or a species nobody has heard of) and every song. It says a line for each
finding and exits with 6 when there is one, 0 when all pass. Nothing is put in use.

## The sample

`samples/packs/gate-lanterns` in the repository is a pack of our own: Oreburgh Gate's lower floor with an overlay's new
lines, a script for the cyclist, Veteran Grant's team patched and a short tune of its own. The game never loads it
unless it is copied into a `packs` folder; the tests check it passes `--validate-packs`.
