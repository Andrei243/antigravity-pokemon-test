# Playing a release

This is for a player who has downloaded a release's zip (plan 16 · T2). Someone building the game from the source wants the README instead.

This is a fan-made project, not affiliated with Nintendo, Creatures or GAME FREAK. Its art, music and words are its own.

## Running it

Unpack the zip anywhere you can write to and run the game in it. Nothing else needs installing: the .NET runtime and raylib are inside the executable.

| System | The zip | What to run |
|---|---|---|
| Windows 10 or 11, 64-bit | `…-win-x64.zip` | `PokemonPlatinumEngine.exe` |
| Linux, 64-bit (and the Steam Deck's desktop mode) | `…-linux-x64.zip` | `PokemonPlatinumEngine` (`chmod +x` it first if your unzipper lost the mark) |
| macOS on an Intel Mac | `…-osx-x64.zip` | `PokemonPlatinumEngine` |
| macOS on Apple silicon (M1 and later) | `…-osx-arm64.zip` | `PokemonPlatinumEngine` |

Keep the folder together: the game reads its `Data` and `Assets` from beside the executable, and `cache/models` holds the models the first screens show, so that the first start takes seconds rather than minutes. The game needs a graphics card that runs OpenGL 3.3.

The title's bottom right corner says which version you have (`v0.1.0`), and so does the help line of the options' HELP row. `PokemonPlatinumEngine --verify` checks the game's files against the list the release was made with (`manifest.sha256`) and says which, if any, are missing or changed.

### macOS: the first start of an app nobody signed

The game is not signed with an Apple developer's certificate, so macOS stops it the first time. Either:

- in Finder, right-click (or Control-click) `PokemonPlatinumEngine`, choose **Open**, then **Open** again in the question; or
- in a terminal, in the game's folder: `xattr -dr com.apple.quarantine .` and then `./PokemonPlatinumEngine`.

macOS asks once; later starts open straight away.

### Windows: SmartScreen

Windows may say it "protected your PC" from an unrecognised app, for the same reason. **More info**, then **Run anyway**.

### The Steam Deck

Add `PokemonPlatinumEngine` (the Linux build) to Steam as a non-Steam game and start it from Game Mode. The first start picks a 1280×800 window, the Deck's whole screen, with the game's 16:9 picture letterboxed in it; full screen does the same. The pad works as it is (see below).

## Controls

| | Keyboard | Pad |
|---|---|---|
| Move | arrow keys or WASD | left stick or d-pad |
| Confirm, talk | Z or Space | A (bottom face button) |
| Back | X or Esc | B (right face button) |
| Run (with the Running Shoes) | Left Shift or X | B held |
| Menu | Enter or Tab | Start |
| The registered item | C | left face button |
| The Pokétch | P, O for its next app, I to touch it | top face button, R, L |
| Full screen | F11 or Alt+Enter | |
| Mute | M | |
| A screenshot | F12 | |
| A report of what just happened, to send with a bug | F8 | |

The options' CONTROLS page changes any of them, and BUTTON MODE swaps a few of the pad's. The options' CONTROLLER row picks which pad is read when there are several.

## Where the game keeps your files

Your saves (three slots), the options, the log and anything a crash leaves are kept together, in your own folder:

| System | Folder |
|---|---|
| Windows | `%APPDATA%\PokemonPlatinum` |
| Linux | `$XDG_DATA_HOME/pokemon-platinum`, or `~/.local/share/pokemon-platinum` |
| macOS | `~/Library/Application Support/PokemonPlatinum` |

The models and menu pictures the game makes as you play are kept there too, under `cache/`; it can be deleted at any time and is made again.

- `PokemonPlatinumEngine --where` says which folder this copy uses and why, and changes nothing.
- `--portable` keeps everything in the game's own folder instead (so does an empty file called `portable.txt` beside the executable): for a copy on a USB stick.
- `--save <folder>` keeps everything in a folder you name.
- `--help` lists every switch: `--windowed`, `--fullscreen`, `--safe-mode` (the lowest settings in a small window, with no cache and nothing of yours read: try it if the game won't start), `--log <file>`.

Each save is written beside a copy of the one before it (`save1.bak`), so a save cut short by a crash or a power cut still leaves the last one whole. After a crash the next start says what happened and whether your save is intact; the report it names is in `logs/` in the same folder.

## Your own files over the game's: `overrides/`

A folder called `overrides` beside the executable, or in the folder you start the game from, is read before the game's own:

- `overrides/models/<Species>.glb` replaces a Pokémon's model with one of your own (`docs/model-files.md` in the source says how they are made).
- `overrides/sprites/` replaces the people's sprites in the field.

`--safe-mode` reads none of them. Packs, which change the game's data, are another matter: the options' PACKS row lists those installed (`docs/packs.md` in the source).

## When something goes wrong

Press F8 just after it happens: the game writes a folder under `repros/` beside your saves that holds what led up to it, which someone working on the game can play back exactly. With the log (`log.txt` in the same folder) it is the best thing to send with a report.
