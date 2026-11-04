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

Making the voices (plan 26 · V3 to V5; no window, no engine needed until `generate`):

```bash
dotnet run --project tools/VoiceTool -- check-samples [folder] [--voice rival]   # the user's recordings held to docs/voice/recording.md: 48 kHz, mono, 24-bit, no clip, peak, the room's noise, quiet at each end, pace against the passage's words, every mood; ends with 1 on an error or a passage not recorded
dotnet run --project tools/VoiceTool -- bake-samples [--out <folder>]            # the samples cleaned (trimmed, rumble and plosives softened) at their own rate into voice/samples-clean/rival: what an engine clones from
dotnet run --project tools/VoiceTool -- bake <folder> --voice <id> [--pack <folder>] [--engine <name>]   # takes made elsewhere (<key>.wav, the colon '+') baked into the pack: trimmed, levelled to the voice bus, limited under -1 dB, at the game's rate, stamped in pack.json
dotnet run --project tools/VoiceTool -- jobs <voice> [--which missing|stale|all] # a voice's lines an engine still has to speak, with directions (guessed where none is written), samples by mood, respellings and three seeds each: voice/jobs/<voice>.json
dotnet run --project tools/VoiceTool -- generate <voice> | --jobs <file>          # runs tools/VoiceGen/engine.json's engine over them, three takes a line, the best baked into the pack (says so and stops while no engine is chosen)
dotnet run --project tools/VoiceTool -- reroll <key>                              # the next round's seeds for one line: voice/jobs/reroll.json
dotnet run --project tools/VoiceTool -- trial <voice>...                          # the twenty lines of a trial (V3: trial rival; V4: the named cast), the same for every engine: voice/trial/<names>.json and .md
dotnet run --project tools/VoiceTool -- reel <key prefix>                         # a place's current takes one after another, for spot listening (reel world:jubilife_city.): voice/reels/<prefix>.wav and its keys
dotnet run --project tools/VoiceTool -- lexicon [--top 60]                        # the names the lines say that Data/voice/lexicon.json has no respelling for, the most said first
```

The rival's sample script is `PokemonPlatinumEngine/Data/voice/samples/rival/samples.json` (the same passages for a person in `docs/voice/samples.md`, how to record them in `docs/voice/recording.md`, a booth page in `tools/VoiceBooth/booth.html`). The engine itself is chosen in plan 26's trials and set up as `tools/VoiceGen/README.md` says.

A pack (`docs/data-files.md`, "Voice packs") is `voice/<language>/` beside the game or at the repository's root, ignored by git. The game plays a take only while its `textHash` is the hash of the line's English now; a rewritten line is stale and silent until its take is made again.

After a merge that brings new scripts, overlays or maps, run `catalogue --add-missing`: new speakers get a provisional voice, new lines get keys and spoken texts, and a line whose English changed is listed as stale. A spoken text written by hand (`"manual": true` in `lines.json`), a line's `direction` and its `notes` are kept across runs. `VoiceTests` runs the same catalogue in memory and fails when the files differ.

The game's files are read from the build output (`dotnet run` builds first), and the cast, the catalogue and the report are written in the repository.
