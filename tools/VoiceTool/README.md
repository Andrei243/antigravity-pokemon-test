# VoiceTool (plan 26)

The catalogue of every line a character says, the cast that speaks them and the report of what is voiced. No window, no audio.

```bash
dotnet run --project tools/VoiceTool -- catalogue                 # rewrite Data/voice/lines.json and docs/voice/coverage.md; says which lines are new, stale (their English changed), recast or gone
dotnet run --project tools/VoiceTool -- catalogue --add-missing   # first give every speaker with no cast entry a provisional voice of its own in Data/voice/cast.json
dotnet run --project tools/VoiceTool -- catalogue --check         # write nothing; ends with 1 if the files are not what the game gives, or a speaker has no cast entry
dotnet run --project tools/VoiceTool -- export <voice> [file]     # a voice's lines in the catalogue's order, for reading through (or for a human actor)
```

After a merge that brings new scripts, overlays or maps, run `catalogue --add-missing`: new speakers get a provisional voice, new lines get keys and spoken texts, and a line whose English changed is listed as stale. A spoken text written by hand (`"manual": true` in `lines.json`), a line's `direction` and its `notes` are kept across runs. `VoiceTests` runs the same catalogue in memory and fails when the files differ.

The game's files are read from the build output (`dotnet run` builds first), and the cast, the catalogue and the report are written in the repository.
