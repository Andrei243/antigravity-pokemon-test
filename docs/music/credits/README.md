# The regions' credits songs

Each built region has a credits song of its own, in a style that fits it. All were generated on 2026-11-09 with Google's **Lyria 3.5** (`lyria-3.5`, the Gemini API's `generateContent`, WAV asked for), using the user's own key. Every lyric, title, structure and prompt was written for this project. None names the series, a character, a place, an artist or a song, and none quotes the games.

The game plays the chosen take of each region as `PokemonPlatinumEngine/Data/music/<region>/credits.ogg`. Where a region also has an MML `credits.mml`, the recording wins (`MusicLibrary.GetRecording`, `AudioManager.SceneSongOf`), and the roll is timed to the song (`CreditsScreen.PaceFor`). This folder keeps **every take** that was made, so another take can be chosen later by copying it over the game's file.

Each region's folder holds:
- `prompt.txt`: the exact text Lyria was given (style, timed structure, lyrics).
- `take<n>.ogg`: each take, encoded from Lyria's 48 kHz stereo WAV (Vorbis, quality 0.4, about 1.8 MB).
- `take<n>-lyria.txt`: the timed lyrics Lyria sent back with the take.

| Region | Song | Style | Tempo, key | In the game |
|---|---|---|---|---|
| Kanto | "first route" | Retro power-pop with an 8-bit chiptune lead | 150 BPM, E major | take 1 |
| Johto | "bells over the old town" | Japanese folk fusion: koto, shamisen, shakuhachi, taiko, temple bells | 92 BPM, D minor to F major | take 1 |
| Hoenn | "salt and sunlight" | Tropical house: steel drums, marimba, congas | 112 BPM, A major | take 1 |
| Sinnoh | "where time and space began" | Wintry symphonic pop: celesta, orchestra, choir | 128 BPM, C♯ minor to E major | take 1 |
| Unova | "city of a thousand lights" | Hard techno: distorted kick, acid line, rave stabs | 145 BPM, F minor | take 1 |
| Kalos | "la belle route" | French chanson waltz: accordion, harpsichord, manouche guitar | 100 BPM in 6/8, G major | take 1 |
| Alola | "four islands" | Island reggae-pop: ukulele, slack-key and steel guitar | 96 BPM, D major | take 1 |

Two sets were made and not chosen: `sinnoh/hyperpop/` is "new game plus", the first credits song (its sheet is [`../credits-song.md`](../credits-song.md)), and `unova/funk/` is Unova's song in big-city funk before the user asked for hard techno.

The takes are mastered at about −14 dBFS RMS with peaks at full scale and carry Google's SynthID watermark. Lyria gives no stems. The game plays them at `SongPlayer.RecordingGain` (0.7), which puts them in the music bus's window. Galar and Paldea get theirs when those regions are built: the same way, a prompt here and the take chosen copied into the game's `Data/music/<region>/credits.ogg`.
