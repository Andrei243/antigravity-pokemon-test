# RoomEditor

A window for drawing, furnishing and peopling a room's map file (`Data/maps/<Name>.json`) and for editing an area's overlay (`Data/world/<region>/overlays/<key>.json`). Plan 16 · T9.

```bash
dotnet run --project tools/RoomEditor -- <Room>                          # edit a room of the repository (PokemonCenter, OreburghGym)
dotnet run --project tools/RoomEditor -- --new <Room> <width> <height>   # a new room: floor in a ring of wall, the back wall two rows deep
dotnet run --project tools/RoomEditor -- <Room> --area <key>             # and an area's overlay on the second tab (sandgem_town)
dotnet run --project tools/RoomEditor -- <Room> --data <dir>             # another folder laid out like Data/
dotnet run --project tools/RoomEditor -- <Room> --shot <out dir>         # each tab drawn once in a hidden window (editor_room.png, editor_overlay.png, editor_preview.png)
```

It writes to the repository's `PokemonPlatinumEngine/Data` (the first folder above the program that has one, as `--dev` finds it), never to the copy beside the program.

## The window

- **F1, the room.** The palette on the left is the tile codes' looks (`TileCodes`), or `PropType`'s furniture with the prop tool. Tools on the number keys: `1` paint the ground, `2` solid (Shift opens), `3` the overhead layer (Shift clears), `4` furniture, `5` a warp, `6` a sign, `7` a person, `8` a trigger, `9` select. Left paints or places; a drag of paint is one step. Right takes away the topmost thing on the tile. The wheel zooms and the middle button pans. A tile's behaviour follows from its look on a hand-made map, so only looks are painted.
- **F2, the overlay** (with `--area`). The area file's objects, warps and triggers, each with what the overlay says of it beside it: who a person is and what they say, their script and trainer's lines; where a door leads, or that it is locked; a trigger's script.
- **F3, the preview.** The content check's plan of the map the game makes of the room (`Plans.OfRoom`, the ground baked by `PixelGround` on the CPU, everything on it tagged).
- **The fields panel**, on the right: the room's own fields, or those of the thing selected or placed, or of the overlay's row picked. Click a field, type, Enter sets it (Esc gives up). A list (a person's lines) is written with `|` between its items; a field left empty is left out of the file.
- `Ctrl+Z` undoes, `Ctrl+Y` redoes, `Ctrl+S` saves the tab's file and runs the content check on it; the findings are listed at the foot of the window.

## What it is made of

- `EditorDocument` (no window): a `MapFile` changed a step at a time (paint, fill, solid, overhead, resize, place and take away furniture, warps, signs, people, triggers and tiles read, set a field), each step undone back to the file's text. A room is saved as the game writes the map it makes of it (`MapFile.FromMap` of `MapFile.ToMap`), so what would stop the game loading it is refused before it is written, and every room it saves reads back as it is written, which the content check holds rooms to.
- `OverlayDocument` (no window): an area's overlay listed against its area file and changed a value at a time by `JsonSplice`, which replaces, adds or takes out one value of a hand-written JSON file and leaves everything else as it was written: the comments, the one-line records, the order. A person given nothing leaves `people`, a door given no map leaves `doors`, a trigger given no script leaves `triggers`.
- `EditorChecks`: the content check's own rules (`ContentCheck.Rooms`, `ContentCheck.Overlays`) asked of the one file being edited, against the rest of the data folder. Never a check of its own.
- `EditorWindow`: draws them and turns the mouse and the keys into their methods.

`RoomEditorTests` drives the documents with no window: every room of the game opens and saves as it is written; a series of edits undoes back to the file; a Pokémon Center is drawn, furnished, previewed, checked and saved from nothing and loaded as the game loads a room; an overlay is changed with every comment of it kept, and undone.

Not built yet: the preview through the field's own renderer (`MapScene.Build` with the room's `InteriorStyle` and `MapScene.ViewOf`), which wants the GPU; the plan's top-down picture stands in for it. Placing an overlay's own people, props and exits (`npcs`, `props`, `exits`) and its signs is done by hand in the file.
