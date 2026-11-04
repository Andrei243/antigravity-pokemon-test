# Recording the rival's samples

How to record the 21 passages of [`samples.md`](samples.md) for plan 26 · V3. Twenty to thirty minutes at the
microphone, once. The clone copies the room and the microphone along with the voice, so a few minutes spent on the
setting below pays for itself in every one of Barry's lines.

## What you need

- **A microphone.** Any decent USB microphone. A dynamic one (the kind podcasters use) forgives an untreated room
  better than a condenser. A phone's voice recorder in a quiet room is enough to start with; you can always record
  again with better gear and generate the lines again.
- **A pop filter**, or a sock over the microphone, or simply speaking a little past it rather than straight into it.
- **Headphones**, to listen back without the speakers' sound reaching the microphone.
- **A recorder**: the booth page (below), or Audacity, or anything that writes WAV files.

## The room

- A small room with soft things in it (curtains, a sofa, a bed, clothes in a wardrobe). A bare room echoes, and the
  echo is cloned.
- Nothing running: no fan, no washing machine, no music, nobody talking. Close the window. Turn notifications off.
- The same place, the same chair and the same distance for every passage: about a hand's span (15 to 20 cm) from the
  microphone. For the two shouting passages, step back a little or turn aside, and **don't touch the input level**.

## The settings

| Setting | Value |
| --- | --- |
| Format | WAV (PCM) |
| Rate | 48,000 Hz (48 kHz) |
| Depth | 24-bit (16-bit is accepted, with a warning) |
| Channels | mono (one); stereo is averaged into one, with a warning |
| Level | the loudest moments round −12 dB on the recorder's meter; never touching the top |
| Effects | none: no noise reduction, no compressor, no equaliser, no "voice enhancement" |

The bake cleans every sample the same way afterwards (`VoiceTool bake-samples`: the silence trimmed, the rumble under
80 Hz taken out, the level evened), so leave the recordings as they came from the microphone. A phone that offers a
"voice memo" mode with processing: turn it off if you can, or use a recorder app that writes plain WAV.

Set the rate on the recorder itself. A file recorded at 44.1 kHz and converted to 48 kHz afterwards is refused, as it
has lost what the conversion can't put back.

## Each take

1. Warm up for a minute first: read a couple of passages aloud without recording, hum, yawn. Have water nearby.
2. Read the passage and its direction to yourself once. Decide how Barry says it.
3. Start recording, **wait half a second in silence**, read the passage, **wait half a second again**, stop. The quiet
   at each end tells the bake where the room's noise is.
4. Listen back. Keep it if it sounds like Barry and every word is there; otherwise read it again. Keep the best take of
   each passage; three tries is plenty.
5. Read the words exactly as written. If you stumble, start the passage again rather than going on.

Take a break whenever your voice tires: a tired voice is cloned as a tired voice. Shouting passages last, if you like.

## Naming and where the files go

Each file is named after its passage, exactly: `01_calm_morning.wav`, `02_calm_professor.wav`, … `21_calm_towns.wav`
(the names are the headings of `samples.md`). They go into

```
PokemonPlatinumEngine/Data/voice/samples/rival/
```

beside `samples.json`, in the repository. Nothing else goes in that folder.

## The booth page

`tools/VoiceBooth/booth.html` is a page that does all of the above in a browser, with nothing to install and no server:
open it in Chrome or Edge (from the file itself), press **Choose the samples folder** and pick
`PokemonPlatinumEngine/Data/voice/samples/rival/`. It reads the passages from `samples.json` there, shows them one at a
time with their directions, records from the microphone at 48 kHz, plays a take back, keeps up to three takes of a
passage, and writes the one you mark as best into the folder under its passage's name. Its meter shows the level as you
read. It sends nothing anywhere: the microphone's sound stays in the browser and the files you save.

(Firefox and Safari can't write into a folder: there the page offers each chosen take as a download, named as it should
be, for you to move into the folder.)

## Checking them

When the files are in the folder, run from the repository:

```bash
dotnet run --project tools/VoiceTool -- check-samples
```

It lists every passage with its length, peak, the room's noise and the quiet at its ends, and then what is wrong:

- **errors** (a clone would copy them, so record the passage again): a missing file; not a WAV file; a rate other than
  48 kHz; clipping; a peak under −24 dB (too quiet); the room's noise over −40 dB; a take under 3 or over 35 seconds;
  no voice to be told from the noise;
- **warnings** (look, and decide): stereo; 16-bit; a peak over −3 dB; noise over −50 dB; less than about a third of a
  second of quiet at an end, or more than three seconds; a pace that doesn't fit the passage's words (a part left out,
  or a long gap); a DC offset; a file no passage names; fewer than 90 or more than 300 seconds in all; a mood with no
  take yet.

It ends with 1 on an error, so it can be run again until it says nothing is wrong. `check-samples <folder>` checks
another folder (a trial set you made elsewhere first).

## Sending them

The samples are committed to the repository like any other file (decision 5 of the plan: the user's choice, knowing
the repository is public):

```bash
git add PokemonPlatinumEngine/Data/voice/samples/rival/*.wav
git commit -m "Plan 26 · V3: the rival's samples"
```

If you'd rather a session commits them, put them in the folder and say so. Either way, nothing is uploaded to any
service you haven't chosen: the cloning engine runs on your own machine or in a session's own sandbox, and plan 26 · V3
asks you to approve the engine and its licence before anything is downloaded.

## If you want them gone later

Removing the samples from the repository's history needs a history rewrite and a force-push, and copies already taken
can't be recalled. Deleting the files from the folder stops new lines being generated from them.
