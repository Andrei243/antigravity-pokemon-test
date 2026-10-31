# Changelog

What changes from one release to the next, for players. Every session's close-out adds its lines under **Unreleased**, in plain words (what a player sees or can do now, not how it was built); a release turns Unreleased into its version's section, which `.github/workflows/release.yml` puts on the release's page. Versions are `v0.y.z` until the story is complete.

## [Unreleased]

### Added
- The Trainer Card gains a statistics page once you have played a little: steps (on foot, surfing, cycling, diving), battles won, lost and run from, blackouts, Pokémon met and caught, levels gained and evolutions, in four tabs (up and down change the tab).
- A new way to play at NEW GAME, "No later mechanics": nobody, you or the trainers you meet, can Mega Evolve, use a Z-Move, Dynamax or Terastallize in that run.
- Trainers use Mega Evolution, Z-Moves, Dynamax and Terastallization with more sense: a Z-Move only when it will hit well or its bonus helps, Dynamax kept for their last Pokémon or their ace, and Terastallization when it turns a weakness to your last move around or on their last Pokémon.
- Release builds for Windows, Linux and macOS (Intel and Apple silicon): one executable each with everything it needs inside, and the models the first screens show already made, so the first start takes seconds (plan 16 · T2).
- The title's bottom right corner, and the options' HELP line, say which version of the game this is; a save remembers the version that wrote it.
- The game's own icon.
- A 1280×800 window size, the Steam Deck's screen, which the first start picks on a screen of that size.
- `docs/playing.md`: running a release without installing anything, macOS's and Windows' first-start questions, the Steam Deck, the controls, where saves are kept and `overrides/`.
- `--verify` now has something to check: every release carries the list of its files (`manifest.sha256`).
