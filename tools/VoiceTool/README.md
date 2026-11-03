# VoiceTool (plan 26)

The catalogue of every line a character says, the cast that speaks them and the report of what is voiced. No window, no audio.

```bash
dotnet run --project tools/VoiceTool -- catalogue                 # rewrite Data/voice/lines.json and docs/voice/coverage.md; says which lines are new, stale (their English changed), recast or gone
dotnet run --project tools/VoiceTool -- catalogue --add-missing   # first give every speaker with no cast entry a provisional voice of its own in Data/voice/cast.json
dotnet run --project tools/VoiceTool -- catalogue --check         # write nothing; ends with 1 if the files are not what the game gives, or a speaker has no cast entry
dotnet run --project tools/VoiceTool -- export <voice> [file]     # a voice's lines in the catalogue's order, for reading through (or for a human actor)
dotnet run --project tools/VoiceTool -- manifest [folder]         # (plan 26 · V2) write a pack's pack.json from the files it holds: <voice>/<key>.wav, the colons written '+'; default folder voice/en at the repository's root
dotnet run --project tools/VoiceTool -- check [folder]            # hold a pack to the game: keys, files, decoding, loudness (-24 to -18 dB), peak under -1 dB, silent ends; stale takes listed; ends with 1 on a fault
dotnet run --project tools/VoiceTool -- vowels [folder] --voice rival   # a pack of stand-in takes (synthesised vowels) for a voice's lines, or --all; turn VOICES on in the options to hear them
```

A pack (`docs/data-files.md`, "Voice packs") is `voice/<language>/` beside the game or at the repository's root, ignored by git. The game plays a take only while its `textHash` is the hash of the line's English now; a rewritten line is stale and silent until its take is made again.

After a merge that brings new scripts, overlays or maps, run `catalogue --add-missing`: new speakers get a provisional voice, new lines get keys and spoken texts, and a line whose English changed is listed as stale. A spoken text written by hand (`"manual": true` in `lines.json`), a line's `direction` and its `notes` are kept across runs. `VoiceTests` runs the same catalogue in memory and fails when the files differ.

The game's files are read from the build output (`dotnet run` builds first), and the cast, the catalogue and the report are written in the repository.
