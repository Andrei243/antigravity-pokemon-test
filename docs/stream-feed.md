# The stream feed

Plan 13 · V24. With the options' **STREAM FEED** row on, the game keeps three files in the `stream/` folder beside your saves (`--where` says which folder). Nothing is sent anywhere: all of it is files, and the game opens no connection for it. With the row off, or under `--safe-mode`, none of them is written.

| File | What it is |
|---|---|
| `state.json` | What the run is doing now, as JSON. Written whole (to `state.json.tmp`, then moved over the old file), so a reader never sees half of it. |
| `overlay.html` | An overlay of ours for an OBS Browser source: add a Browser source, tick "Local file" and choose it. It reads `state.json` beside it. Copied by the game when the feed is switched on. |
| `livesplit.lss` | A LiveSplit splits file, only with the row's third value (File + LiveSplit). Open it in LiveSplit. |

`state.json` is written when something in it has changed, at most four times a second of game time, and once a second more while the run timer's corner is shown. The timer's running seconds and the time of writing are not counted as a change. STREAM-SAFE has no effect on it: the feed never holds a trainer ID, a friend or link code, or a path.

## The shape of `state.json` (format 1)

Fields are camelCase; a field that has no value is left out.

```
{
  "format": 1,
  "game": "Pokémon Platinum (a fan remake)",
  "updated": "2026-11-03T12:00:00Z",       // UTC, when it was written
  "place": "Oreburgh City",                // the place's name in the game's language
  "region": "Sinnoh",
  "badges": ["Coal"],                      // the region's badges won, by their English names
  "rules": { "preset": "nuzlocke", "on": ["nuzlocke", "dupes"] },
  "party": [ <mon>, ... ],                 // the team, in order
  "graveyard": [ { "species", "nickname"?, "level", "place" } ],   // a Nuzlocke run's fallen
  "areasMet": 4,                           // places where the run's one wild Pokémon was met
  "timer": {                               // the run timer, always kept; absent in a save from before it
    "realSeconds": 1234.5, "gameSeconds": 1200, "finished": false,
    "last": { "id": "badge.sinnoh.Coal", "name": "Coal Badge", "realSeconds": 1000, "gameSeconds": 980 }
  },
  "battle": {                              // only while a battle is on screen
    "kind": "trainer",                     // or "wild"
    "foe": "Youngster Tristan",            // a trainer's battle only
    "player": [ <mon>, ... ], "enemy": [ <mon>, ... ]   // the Pokémon on the field, as the screen shows them
  }
}
```

A `<mon>` is `{ "species", "form"?, "nickname"?, "level", "hp", "maxHp", "status", "shiny" }`. `species` and `form` are the data's English names; `status` is `None`, `Poison`, `Toxic`, `Burn`, `Paralyze`, `Sleep`, `Freeze`, `Faint` or `egg`.

The timer's `last` is the last split of the category chosen on the RUN TIMER row (the Badges' when that row is off). `timer.realSeconds` is the run's real time as of `updated`; the overlay adds the time since it read the file.

## `livesplit.lss`

A LiveSplit run (format 1.7.0): a segment for every split point the timer's category counts (each region's Badges and Hall of Fame; with Story, Sinnoh's story turns too), and for each one taken, its time as the Personal Best split (Real Time the run's real time, Game Time the game's). It is rewritten whole whenever a split is taken.
