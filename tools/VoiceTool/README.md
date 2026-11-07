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

Making the voices (plan 26 · V3 to V5; no window, no engine needed until `generate`). The engine is Kokoro-82M (approved 2026-11-07), set up by `bash tools/VoiceGen/setup-kokoro.sh` as `tools/VoiceGen/README.md` says; every voice, the rival's too, is a `design` in `cast.json`:

```bash
dotnet run --project tools/VoiceTool -- generate <voice>... [--region <id>] | --jobs <file>   # runs tools/VoiceGen/engine.json's engine over the voices' lines with no current take (with --region sinnoh, only that region's places), three takes a line, the best baked into the pack; one engine process for the whole run (says so and stops while no engine is set up)
dotnet run --project tools/VoiceTool -- reel --voice <id> [--count 12] [--region <id>]          # a voice's sample reel: its first lines with a take, spread over their directions, one after another: voice/reels/<voice>.wav and its keys
dotnet run --project tools/VoiceTool -- reel <key prefix>                         # a place's current takes one after another, for spot listening (reel world:jubilife_city.): voice/reels/<prefix>.wav and its keys
dotnet run --project tools/VoiceTool -- jobs <voice> [--which missing|stale|all] [--region <id>]  # a voice's lines an engine still has to speak, with directions (guessed where none is written), respellings and three seeds each: voice/jobs/<voice>.json
dotnet run --project tools/VoiceTool -- reroll <key>                              # the next round's seeds for one line: voice/jobs/reroll.json
dotnet run --project tools/VoiceTool -- trial <voice>...                          # the twenty lines of a trial, the same for every design: voice/trial/<names>.json and .md
dotnet run --project tools/VoiceTool -- bake <folder> --voice <id> [--pack <folder>] [--engine <name>]   # takes made elsewhere (<key>.wav, the colon '+') baked into the pack: trimmed, levelled to the voice bus, limited under -1 dB, at the game's rate, stamped in pack.json
dotnet run --project tools/VoiceTool -- lexicon [--top 60]                        # the names the lines say that Data/voice/lexicon.json has no respelling for, the most said first
dotnet run --project tools/VoiceTool -- check-samples | bake-samples              # the recording kit of the first draft (a cloned rival), kept but unused since the user's ruling of 2026-11-07
```

A line whose voice chooses at run time (`assistant`) is made in each voice it chooses from (`assistant_boy`, `assistant_girl`).

A pack (`docs/data-files.md`, "Voice packs") is `voice/<language>/` beside the game or at the repository's root, ignored by git. The game plays a take only while its `textHash` is the hash of the line's English now; a rewritten line is stale and silent until its take is made again.

After a merge that brings new scripts, overlays or maps, run `catalogue --add-missing`: new speakers get a provisional voice, new lines get keys and spoken texts, and a line whose English changed is listed as stale. A spoken text written by hand (`"manual": true` in `lines.json`), a line's `direction` and its `notes` are kept across runs. `VoiceTests` runs the same catalogue in memory and fails when the files differ.

The game's files are read from the build output (`dotnet run` builds first), and the cast, the catalogue and the report are written in the repository.
