# VoiceGen (plan 26 · V3, V4): where the voice engine runs

**Nothing is installed here yet.** The engine is chosen by ear in plan 26's trials (V3 for the rival, cloned from the
user's samples; V4 for everyone else), and each engine, its packages and its voices are downloaded only once the user
has approved their licences, as the project's rules ask. The candidates are in `docs/plans/26-voice.md` ("Where the
voices come from"): Chatterbox, F5-TTS and OpenVoice V2 for cloning; Kokoro, Piper and Chatterbox for designed voices.

Every one of them is a Python program, and the game itself never depends on Python. Once one is chosen:

1. Install the engine in its own folder here, `tools/VoiceGen/env/` (git-ignored), with its version and its packages
   pinned in one requirements file committed beside this README, so the same engine is installed the same way again.
2. Write a small script beside it that speaks one line: it reads the text from a file, writes a WAV, and takes a seed,
   a reference sample and its transcript (for cloning) and a direction (calm, excited, shouting, frustrated, quiet,
   laughing, whispering, sad, smug).
3. Write `tools/VoiceGen/engine.json` (git-ignored, as it holds paths of this machine), from `engine.example.json`:

```json
{
  "name": "the engine and its version",
  "command": "tools/VoiceGen/env/Scripts/python.exe",
  "args": ["{repo}/tools/VoiceGen/speak.py", "--text-file", "{textFile}", "--out", "{out}", "--seed", "{seed}",
           "--sample", "{sample}", "--sample-text", "{sampleText}", "--direction", "{direction}"],
  "useRespelling": false,
  "timeoutSeconds": 600
}
```

Then, from the repository:

```bash
dotnet run --project tools/VoiceTool -- bake-samples                  # the rival's samples cleaned into voice/samples-clean/rival (what the engine clones from)
dotnet run --project tools/VoiceTool -- trial rival                   # the twenty trial lines, voice/trial/rival.json and .md
dotnet run --project tools/VoiceTool -- generate --jobs voice/trial/rival.json --pack voice/trial/<engine>   # one engine's takes of them, to listen to
dotnet run --project tools/VoiceTool -- generate rival                # every line of the rival's that has no current take: three takes each, the best baked into voice/en
dotnet run --project tools/VoiceTool -- reroll <key>                  # new seeds for a line you didn't like; then generate --jobs voice/jobs/reroll.json
```

`generate` runs the command once a take (three takes a line, each under its own seed), bakes every take
(`VoiceTool`'s bake: trimmed, the rumble and plosives taken out, brought to the voice bus's level, at the game's rate),
keeps the one with the fewest findings and the easiest pace, and writes it into the pack with the engine's name, its
seed and the sample it cloned from. The same seed and sample make the same line again.

A speech-to-text check (whisper.cpp, MIT, local) is the plan's next safeguard: it would compare what each take says
with its text and count a mismatch against it. It needs its own download and the user's OK too.
