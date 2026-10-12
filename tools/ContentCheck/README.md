# ContentCheck

Reads the files of the game that are written by hand (the overlays of the imported world, the rooms, the scripts) and names what is wrong with them; and draws a room or an open area from above, so one can be looked at without the game's window. Plan 16 · T8.

```bash
dotnet run --project tools/ContentCheck                                  # check the game's own files; ends with 1 on a finding
dotnet run --project tools/ContentCheck -- --data <dir>                  # check another folder laid out like Data/
dotnet run --project tools/ContentCheck -- --plan <out dir> <name>...    # plans of rooms (PlayerHouse) and open areas (twinleaf_town), as PNGs
dotnet run --project tools/ContentCheck -- --plan <out dir> Sinnoh@96,864,20,12   # a rectangle of a map's tiles alone: x, y, width, height
dotnet run --project tools/ContentCheck -- --plan <out dir> --tile 64 <name>      # a tile drawn 8, 16, 32, 48, 64 or 96 pixels wide (fitted unless said)
dotnet run --project tools/ContentCheck -- --rows "<line>"               # a line as the text box breaks it (\n for a break), row by row with its widths
```

It opens no window and takes about two seconds. It is in the solution, and the test project drives its classes: `ContentTests` holds the game's own files to it and breaks a small world of its own to see each mistake named.

## What it checks

A finding is one line: the file (from the data folder), the name of the thing in it, and what is wrong.

```text
world/sinnoh/overlays/route_227.json: people.crasher_wake: is drawn as 'CrasherWake', which is no look of characters.json and no thing of the map: they would stand there as the plain default ('crasher_wake'?)
maps/Route221House.json: the sign at 4,2: the line wraps to 4 rows and the text box holds 3: "WIN A PRIZE FROM ME! / Black Belt: Fighting-type moves hit ..."
```

What it looks for is, above all, what the game's readers pass over **without a word**. `WorldMapBuilder.PlaceEvents` goes through the area's own objects, warps and signs and asks the overlay about each, so whatever the overlay names that isn't among them is never met; the JSON reader skips a field it doesn't know; a script that isn't there simply doesn't start.

**An overlay** (`world/<region>/overlays/<key>.json`, against `areas/<key>.json`):

- its `area` is its file's name, the area exists and is open in `world.json`; every open area has its file, its map and its overlay
- a field the format doesn't have (`"dialogue"` for `"dialog"`), anywhere in the file, with the nearest one that would be read
- `people`: an id that is no object of the area; someone the game never stands there (a signpost or a mailbox, whose words go under `signs`; a copy of a neighbouring area's object; someone the other half of a route places); an entry for a thing the game lays out by itself (an obstacle, a berry patch, an item's ball); someone with nothing to say; an `npcType` that is no look of `characters.json` and no thing of the map; an `at` outside the area
- a trainer: neither the object's script nor the overlay's `id` names a trainer of `trainers.json` (the game would not load), or the `id` given is not the one used
- `heldBack`, `signs` and `signScripts`: ids that are no object, or no sign that can be read; `read`: a script number nothing read in the area runs; `readAt`, `pits`, a trigger of the overlay's own, `npcs` and `props`: tiles outside the area
- `doors`: a warp number past the area's warps, a warp given twice, a map that is none, a landing outside the map or on a blocked tile; `locked` and `switches`: a number past the warps, a switch that names no flag or whose warp leads nowhere; `exits`: from outside the area, to no map or onto a blocked tile; and every way the world joins by itself lands on open ground
- `triggers`: a number past the area's triggers (the game would not load), one bound twice, one without a script
- every script named (a person's, a sign's, a trigger's, a tile's) is in the area's script file or the common one
- every flag someone is hidden or shown by, a door is opened by and a way is switched by is set or cleared by some script, or by the game itself
- the area's music is a song; its `evolutionSites` are places some species' evolution names

**A room** (`maps/<Name>.json`): it loads; it reads back as it is written (the first line that parts is named), with no field the format lacks; its warps stand in the room and land on open ground of a map that exists; its signs, triggers, props, hidden items and people stand inside it; its scripts, flags, counters, looks, wild Pokémon and typed teams exist; a trainer with no team is one of the table.

**The scripts** (`scripts/<place>.txt`): what `ScriptLibrary.Problems` says of them (a script called that doesn't exist, an item, a species, a trainer, a map, a song or a sound that isn't there), and that each file is named for an open area or a map.

**Every line said**, wherever it is written (a person's `dialog`, a trainer's two lines, a sign, a script's `say`, `text`, `ask` and `choose`):

- it fits the text box. A line is written whole into the box, wrapped to its width, and nothing scrolls or pages: the box holds three rows (`DialogueBox.Rows`), and a fourth runs out under the panel and off the screen. The line is measured as the game measures it (`TextMetrics`, which reads the typeface's own widths with no window and reckons as raylib does), with `{player}` and `{rival}` at the widest name the name keyboard takes (seven of its widest key), `{assistant}` at the wider of the two it can be, and what a script fills in as it runs (`{lead}`, `{item}`, `{money}`...) at the widest of its kind in the data. A line that begins with its speaker's name and a colon is measured without it, as the box shows it
- an answer of a question keeps its panel on the screen
- it can be drawn: the interface's typeface is loaded with Latin-1, the two dashes and the ellipsis, and anything else comes out as a question mark (a curly quote, a gender mark, an arrow: the typeface has none), or as nothing (a non-breaking space)
- it isn't empty

## When it finds something

Fix the file. A finding is a mistake of the data unless the check is wrong, and then the check is what to fix; `ContentTests` has a world to break for it.

The one list of exceptions is `Checks.Flags.NeverSetOnPurpose`: flags the files name on purpose though nothing sets them, each with its reason beside it (today one: the original's own flag of Gardenia inside her Gym). A flag on it that something comes to set is itself a finding, so the list only shrinks.

## Plans

`--plan` writes a PNG to a name, to open with the Read tool:

- the ground as the field bakes it (`PixelGround`, on the CPU) with a tile of margin, a grid, and the tiles' numbers along the top and the left; what blocks is hatched; a room's walls are grey and the nothing round it paper
- over it every prop (orange) and building (brown) framed on its tiles, every warp (magenta), sign (cyan), trigger (yellow, dashed), hidden item and tile read (violet), and everyone who stands there as a disc with a dot on the side they face: people blue, trainers red, Pokémon green, things white. Whoever a flag hides is there too
- each with a tag (`P3`, `W1`, `N2`, `T1`) that the key under the picture explains: the prop's kind and tiles, where a warp leads and the flags it goes by, who someone is, their look, facing, script and flags, a trigger's script and the variable it waits for. The key is printed beside the file's name as well

A room is drawn whole. An open area is drawn from its own chunks, never from the map of Sinnoh whole: one picture of the area, and one of each of its chunks when it has more than one. A map of the world that is one place (`LakeVerity`, `OreburghGate1F`) can be named like a room; a larger one by a rectangle of its tiles.

## Files

| | |
| --- | --- |
| `GameContent.cs` | A data folder's overlays, rooms and scripts with their text, and the maps the game makes of them; what can't be read is a finding |
| `Checks.cs` | Everything in order (`Checks.All`), the game's own findings (`Checks.OfTheGame`), the flags something sets, and what people and rooms share |
| `Overlays.cs`, `Rooms.cs`, `Scripts.cs` | The checks of each |
| `Lines.cs` | A line against the text box, and its characters |
| `JsonShape.cs` | Fields a file's format doesn't have |
| `Plans.cs` | The plans and their lettering |
