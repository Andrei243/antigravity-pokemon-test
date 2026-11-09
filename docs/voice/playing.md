# Playing Sinnoh with voices

Plan 26. The game speaks its characters' lines from a **voice pack**: a folder of audio files made by the voice tool and kept
outside git (it is hundreds of megabytes). With no pack the game is silent as it always was; with one, and the options' VOICES
row on, the characters of Sinnoh speak from the first line of a new game to the Hall of Fame and the post-game.

## What you run

The pack on the Windows machine is `C:\Users\andre\.cache\pokemon-voicegen\voice\en` (the folder with `pack.json` in it). From
the repository's inner folder (`antigravity-pokemon-test/antigravity-pokemon-test`):

```bash
# 1. where would the game keep its saves? (nothing is opened or made); use a scratch folder for a trial
dotnet run -c Release --project PokemonPlatinumEngine -- --save C:/scratch/voices --where

# 2. play: --voices names the pack and sets the options' VOICES row to ALL for this run alone
dotnet run -c Release --project PokemonPlatinumEngine -- --save C:/scratch/voices --voices "C:/Users/andre/.cache/pokemon-voicegen/voice/en"
```

Start a new game as usual (NEW GAME, the introduction, Professor Rowan's welcome is spoken). Add `--windowed` for a window. A
game that is to stay the player's own leaves out `--save`; the game then keeps its saves where `--where` says.

`--voices` takes either the pack's own folder (the one with `pack.json`, as above) or a folder that holds a folder for each
language (`voice/` with `en/` in it). It is a mistake, and the game says so and does not start, if the folder is not there.

### Without the switch

The game also finds a pack by itself, in `voice/<language>` beside the executable and in the folder it is started in (the
repository's root, while it is developed). Put or link the pack there (`mklink /J voice\en <the pack>` on Windows), start the
game as usual and set VOICES in the options (the SOUND tab) to STORY ONLY or ALL.

## The options (SOUND tab)

| Row | What it does |
|---|---|
| VOICES | OFF: nobody speaks, as in the original. STORY ONLY: the named characters (the professor, the rival, Cynthia, the Gym Leaders, the Galactic commanders...). ALL: everyone who has a voice, down to the trainers on the routes. |
| VOICE VOLUME | How loud they are. The music and the ambience dip a little while someone speaks. |
| LET VOICES FINISH | Off: A at a finished line moves on and cuts the voice. On: the box waits until the voice has ended. |
| ENGLISH VOICES | In a language with no pack of its own, the characters speak English under the translated text. |

The default is OFF (the voices wait for the player's ear: plan 26, decision 4). **To turn them off**: set VOICES to OFF in the
options, or start the game without `--voices`. `--voices` changes the row for that run only; `settings.json` keeps what the
player had unless the player changes the row during the run.

## What is voiced

- Sinnoh's lines: 6,580 takes in 408 voices (2026-11-09), 95 in a hundred of the lines a person can say there (the rest are the 271 that name a Pokémon, a move, a number, a price or a ribbon, and 50 said by nobody in particular). That is the scripts, the people of the towns, routes, caves and rooms, trainers before and
  after their battles, and what trainers say in battle (the last Pokémon, the first fall, the red bar).
- The introduction (the professor's welcome and his questions).
- A question's text is voiced as it shows; its answers are not.
- Each speaker's mouth moves with their voice in the field, in battle and in the introduction.

What is **not** voiced, by decision: the game's own voice (`speaker none`: "Received the Potion!", signs, the battle's
messages), a Pokémon's cry, a question's answers, and the lines whose text names something the game fills in that cannot be left out of speech (a Pokémon's name, a move, a number: those show their text alone; `docs/voice/coverage.md` counts them as "shown only"). Where a line named the player or the rival, its spoken text says it without the name ("your friend", "that boy").

A line whose English has changed since its take was made is **stale** and silent until its take is made again; the text shows
as it always did. A game with a pack that lags the script is never broken.

## Making and mending the pack

```bash
dotnet run --project tools/VoiceTool -- catalogue --add-missing                  # after a merge that brings scripts, people or maps
dotnet run --project tools/VoiceTool -- design --region sinnoh                    # a brief and a Kokoro design for every voice that speaks Sinnoh and has none
dotnet run --project tools/VoiceTool -- status --region sinnoh --pack <pack>       # what the pack has: current, stale, missing, per voice (--voices, --missing, --shown)
dotnet run --project tools/VoiceTool -- generate <voice>... --region sinnoh --pack <pack>   # the missing and stale takes (tools/VoiceGen/README.md: set Kokoro up first)
dotnet run --project tools/VoiceTool -- check <pack>                               # the pack held to the game: keys, files, loudness, peaks; ends with 1 on a fault
dotnet run --project tools/VoiceTool -- reel --voice rowan --pack <pack>           # a voice's sample reel to listen to: voice/reels/rowan.wav
```

`status` counts a line said by "the person spoken to" in each voice of the people who run its script, so every voice that can
speak a line has a take of it. The test `VoicePackSwitchTests.TheFirstChapterOfANewGameResolvesEveryLineToATake` plays the
first chapter from the bedroom with no window and holds every line the game asks a voice for to the pack (it passes quietly
when there is no pack here; `VOICE_PACK=<folder>` points it at one).

## Listening before you play

The voices are Kokoro-82M's, designed in `cast.json` (a blend, a pace and a pitch; none is a clone of anyone), and wait for the player's ear. A reel is a voice's sample lines one after another:

```bash
dotnet run --project tools/VoiceTool -- reel --voice rowan --pack <pack>      # also barry (the rival is `rival`), cynthia, mom, rival_mom, looker, cheryl, roark, mars, assistant_boy, assistant_girl
dotnet run --project tools/VoiceTool -- reel --voice "look:lass#1" --pack <pack>   # an archetype's voice; look:youngster#1, class:galactic#1, look:nurse#1, look:clerk#1, look:gentleman#1...
```

The reels are `voice/reels/<voice>.wav` at the repository root (add `--out <file.wav>` to put one elsewhere), each with a `.txt` of the words beside it. Eighteen are already made in `C:SERSNDRE.CACHEPOKEMON-VOICEGENOICEEELS` (ROWAN, RIVAL, CYNTHIA, MOM, RIVAL_MOM, LOOKER, CHERYL, ROARK, MARS, BOTH ASSISTANTS AND SIX ARCHETYPES: LOOK_LASS_1, LOOK_YOUNGSTER_1, LOOK_NURSE_1, LOOK_CLERK_1, LOOK_GENTLEMAN_1, CLASS_GALACTIC_1, LOOK_OLD_MAN_1). A voice turned down gets another design (edit its entry in `Data/voice/cast.json`, or its profile in `tools/VoiceTool/Designs.cs`), its folder in the pack removed, and `generate` again.
