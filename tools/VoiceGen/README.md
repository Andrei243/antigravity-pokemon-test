# VoiceGen (plan 26): where the voice engine runs

The engine is **Kokoro-82M** (Apache-2.0, CPU; approved by the user on 2026-11-07,
`docs/plans/decisions/USER-DECISIONS-2026-11-07.md`). Every character's voice is **designed**, the rival's too: a blend
of Kokoro's built-in English voices with a speed and a pitch, written as the voice's `design` in
`PokemonPlatinumEngine/Data/voice/cast.json` beside its `brief`. Nobody's voice is cloned and nothing is recorded.

The game itself never depends on Python or on any of this: it plays the takes the engine made, from a voice pack.

## Setting it up

```bash
bash tools/VoiceGen/setup-kokoro.sh        # PYTHON="py -3.9" (or any 3.9 to 3.12) to choose the interpreter
```

It makes, in `VOICEGEN_HOME` (default `~/.cache/pokemon-voicegen`; never `POKEMON_CACHE`, which the dev scripts may
remove whole):

- `env/`, a Python environment of its own with `requirements-kokoro.txt` installed (onnxruntime, NumPy, eSpeak NG
  through `espeakng-loader`, `phonemizer-fork` and what they need, pinned);
- `models/`, the model's ONNX export (`model.onnx`, fp32, 325 MB), its `config.json` (the phoneme vocabulary) and the
  28 English voices (`voices/*.bin`), each held to the checksum it had when it was approved;

and writes `tools/VoiceGen/engine.json` (git-ignored) from `engine.kokoro.json`. Nothing of it goes into the repository.

`kokoro_speak.py` speaks a take: the text, the voice's design, the line's direction (a share of the speed and the
pitch: Kokoro has no emotion control) and the seed (three takes a line: as written, 4% slower, 4% faster). Names of
`Data/voice/lexicon.json` are said by their respellings, syllable by syllable, the stress where the capitals are. A
pitch is the take made slower by a share and its WAV stamped at a rate higher by the same share, so the bake's
resampling plays it faster and higher. With `--serve` (as `engine.json` runs it) it loads the model once for a whole run.
It is always run with the environment's own Python and `-I`.

## Making voices

```bash
dotnet run --project tools/VoiceTool -- generate rowan mom --region sinnoh            # their lines with no current take, three takes each, the best baked into voice/en
dotnet run --project tools/VoiceTool -- generate rival --region sinnoh --pack <folder> # into another pack folder
dotnet run --project tools/VoiceTool -- reel --voice rowan --count 12                  # a voice's sample reel, voice/reels/rowan.wav
dotnet run --project tools/VoiceTool -- trial rowan mom rival                          # the same twenty lines for any engine or design to be compared on
dotnet run --project tools/VoiceTool -- reroll <key>                                   # new seeds for a line; then generate --jobs voice/jobs/reroll.json
```

`generate` bakes every take (`VoiceTool`'s bake: trimmed, the rumble and plosives taken out, brought to the voice bus's
level, limited under -1 dB, at the game's rate), keeps the one with the fewest findings and the easiest pace, and
writes it into the pack with the engine's name and its seed. The same seed and design make the same line again. On the
Windows machine's Ryzen 7 9800X3D a line takes about 4.6 s (three takes) on 8 threads; three runs side by side, each
into a pack folder of its own (`KOKORO_THREADS=5`), make about two lines a second together.

A voice is tuned by changing its `design` and making its lines again (`generate <voice> --which all` is not needed: a
take made under another design is not stale, so delete the voice's folder in the pack, or reroll its lines). The user
approves each voice by ear from its reel.

A speech-to-text check (whisper.cpp) is not wanted (the user's ruling of 2026-11-07); the bake's checks (clipping,
loudness, a length that fits the words) are what holds a take.
