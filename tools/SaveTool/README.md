# SaveTool

Dumps, validates, migrates and diffs the game's saves without the game. Plan 12 · Q3. It opens no window and reads the same `Data` folder the game does; it never writes over a save.

```bash
dotnet run --project tools/SaveTool -- dump <save> [--all]             # who, where, how far, the team, the boxes, the bag, the story's size (--all: every box place, every flag)
dotnet run --project tools/SaveTool -- validate <save>... [--errors]   # held to the data; ends with 1 on an error (--errors leaves the notes out)
dotnet run --project tools/SaveTool -- migrate <save> [--out <file>]   # what the game's migrations do to it, line by line; --out writes the migrated copy (never over the save)
dotnet run --project tools/SaveTool -- diff <save> <other save>        # every place two saves differ; ends with 1 when they do
```

A `<save>` is any file the game writes: a slot's (`save1.json`), its backup (`save1.bak`), the autosave (`autosave.json`), the crash save. Look for them with `dotnet run --project PokemonPlatinumEngine -- --where`.

## What `validate` finds

A finding is one line, `error` (the game would trip over it or silently change it) or `note` (it repairs it as it loads), with where it is and what:

```text
save1.json: 3 error(s), 1 note(s)
  error  file: the checksum is not the file's: it was changed after the game wrote it (by hand, or cut short)
  error  team #1 (Turtwiggy): there is no species 'Turtwiggy'; number 387 is Turtwig, which the game would make it
  error  team #2 (Bibarel): Speed IV 99 is outside 0 to 31
  note   bag: Potion is listed twice
```

- **The file**: its checksum is the file's. A save edited by hand is found by this when the editor left the checksum alone; a save whose checksum was taken out is found by the rest.
- **Every Pokémon** (team, boxes, Day Care): the species (and its number) and form exist; level 1 to 260; IVs 0 to 31; effort 0 to 255 each (a note above 252, which the game cuts), 510 in all; friendship; every move exists, at most four, none twice, PP within the most its PP Ups allow, PP Ups 0 to 3; the ability and the held item exist; HP within the stats the game would make; ids unique; boxes of 30 places, each used once, as many boxes as the PC has.
- **The trainer**: money, play time, trainer number, name, the badges of each region.
- **The bag**: every item exists, 1 to 999 of it, listed once.
- **The Pokédex**: numbers within the data's; caught without being seen (a note).
- **The place**: the map exists, the tile is inside it and not solid, on the maps of today (`SaveData.PlaceOn`); a note where it wakes elsewhere than it was saved.
- **The versions**: none newer than this game wrote; a note for one older, which the game brings up to date.

## What `migrate` does

The three things the game brings a save up to date by when it loads one: `SaveMigration.Upgrade` (the save's own records: Pokémon ids), `SaveData.PlaceOn` (where its player stands on today's maps) and `StoryMigration.Upgrade` (its story and its bag). The result is printed as `+`/`-` for a flag, `=` for a variable, and the bag before and after. What the game does besides as it loads (the National Pokédex's boxes, a Turtwig to a save from before the opening) it does at load whatever the file says, and is not repeated here.

The classes (`SaveChecks`, `SaveDump`, `SaveMigrate`, `SaveDiff`) are what the test project drives (`SaveToolTests`).
