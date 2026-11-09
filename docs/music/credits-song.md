# "new game plus": the credits song

Written 2026-11-09 at the user's request: a whole song for the credits, generated from these lyrics and this prompt by an AI music generator (Suno v6 first, ElevenLabs Music v2.5 to compare), in a hyperpop style. Everything here is our own: the lyrics, the title, the structure and the prompts. No game, artist, song or company is named in them. Plan 31's decision 4 records the choice, and the overview's "Waiting on the user" (item 11) holds the session that puts the song in the game.

## The song at a glance

| | |
|---|---|
| Title | **new game plus** (lower case, as hyperpop titles usually are) |
| Style | Hyperpop: bright, loud and glitchy, with chiptune leads as a nod to the handheld |
| Tempo | **160 BPM**, half-time feel (80) in the bridge |
| Key | **B major**, the bridge in G♯ minor |
| Length | **2:00**: 80 bars of 1.5 s each |
| Voice | A high, pitched-up, autotuned voice, sweet rather than shouted |
| Mood | Euphoric and a little nostalgic: the end of a long journey, ready to start again |

### Why two minutes

The credits roll at 64 units a second (`CreditsScreen.Pace`). With today's sixteen entries it scrolls for about 65 seconds, after a 5-second title card, and then waits on its last card for up to 30 seconds. A hyperpop song is usually about two minutes, so the roll should be timed to the song rather than the song cut to the roll: the session that puts it in sets the pace so that the last card arrives as the final chorus ends.

## Structure

| Bars | Time | Section | What happens |
|---|---|---|---|
| 1–4 | 0:00–0:06 | Intro | A chopped, pitched-up "new game" from the chorus, stuttering. A filtered supersaw swells. No drums. |
| 5–20 | 0:06–0:30 | Verse 1 | Bouncy 808s, a crisp kit with fast hats, a square-wave arpeggio under the voice. |
| 21–28 | 0:30–0:42 | Pre-chorus | The kick drops out, a riser climbs, and the voice doubles an octave up. |
| 29–44 | 0:42–1:06 | Chorus | The drop: wide supersaws, sidechained to the kick, distorted 808s, glitch fills at every fourth bar. |
| 45–52 | 1:06–1:18 | Verse 2 | Faster and chopped up, the beat half-stuttered, bitcrushed. |
| 53–60 | 1:18–1:30 | Bridge | Half-time, nearly bare: a soft piano and a clean, less processed voice. The quiet moment. |
| 61–76 | 1:30–1:54 | Final chorus | The biggest drop, the chorus up a half step with a counter-melody on the square lead. |
| 77–80 | 1:54–2:00 | Outro | Everything cuts but the last line and a chiptune chime; it ends cleanly, not on a fade. |

## The lyrics

```text
[Intro]
new g-g-game
new game, new g-

[Verse 1]
woke up in a tiny room with the tv on
mom said take your shoes, the tall grass hums a song
one friend in a ball and a map I couldn't read
lost in the forest, but the forest followed me
rival on the bridge with a grin and a bet
lost my first fight, but I didn't lose it yet
potions in my pocket like a glitter grenade
every badge a heartbeat, every town a parade

[Pre-Chorus]
and the screen goes white (white)
and my hands hold tight (tight)
hold on, hold on, it's alright, alright

[Chorus]
we were level one, we were barely anyone
now we're running every route till the morning comes
hit save, don't fade, keep the team up in the sun
new game, new game, new game plus
we were level one, we were barely anyone
now the whole map's lit and we're never really done
hit save, don't fade, every ending's a beginning, so
new game, new game, new game plus

[Verse 2]
snow cap, lava lake, a ferry to a brand-new map
fly back home and I remember every gap
cave went dark, so I lit it with a friend
every goodbye was a road around the bend

[Bridge]
if the credits roll and the lights go low
I'll still know the way, I'll still know the road
every name on the screen is a name I keep
you were there at the start, you're here when I sleep

[Final Chorus]
we were level one, we were barely anyone
now we're running every route till the morning comes
hit save, don't fade, keep the team up in the sun
new game, new game, new game plus
we were level one, now we're everything we've won
and the whole map sings every time we're back for more
hit save, don't fade, every ending's a beginning, so
new game, new game, new game plus

[Outro]
saving... don't let go
new game plus
```

The lines tell this game's own journey: the bedroom and the television, Mom and the shoes, the first friend, the forest, the rival on the bridge, the Badges, the snow and the lava, the ferry to a new region, Fly back home, a dark cave lit, the credits, and starting again. None names the series, a character or a place.

## The prompts

### Suno v6 (Custom mode)

**Title**: `new game plus`

**Style of Music** (paste as is):

```text
hyperpop, bubblegum bass, 160 bpm, B major, euphoric and nostalgic, high pitched-up autotuned vocals, sweet not shouted, bright supersaw chords, chiptune square-wave arpeggios and lead, distorted bouncy 808s, crisp fast hi-hats, glitch stutters and vocal chops, bitcrush, heavy sidechain, riser into each drop, half-time piano bridge with clean vocal, final chorus key change up a half step, clean hard ending
```

**Exclude Styles**: `lo-fi, acoustic, country, rock, metal, orchestral, slow ballad, fade out`

**Lyrics**: the block above, with its section tags.

Settings: v6 (not v6-wild for the first takes), Weirdness about 40%, Style Influence about 70%. Generate a few takes, keep the best two, and use section editing to fix any line the voice slurs before downloading the WAV and its stems.

### ElevenLabs Music v2.5 (to compare)

```text
A two-minute hyperpop song at 160 BPM in B major, euphoric and a little nostalgic. A high, pitched-up, autotuned vocal, sweet rather than shouted. Bright supersaw chords, chiptune square-wave arpeggios, distorted bouncy 808s, fast crisp hi-hats, glitchy vocal chops and stutters, bitcrushed fills, heavy sidechain. Structure: a 6-second stuttered intro, a bouncy verse, a pre-chorus that drops the kick and rises, a huge chorus drop, a chopped faster second verse, a half-time piano bridge with a clean vocal, a final chorus a half step higher, and a hard stop on the last word. Use these lyrics exactly:
```

followed by the lyrics block. Set the length to 2:00.

## Choosing a take

- It runs 1:50 to 2:10, and each section lands near its time above (the bridge's quiet is what the roll's last section should fall on).
- Every word is clear in the chorus. A slurred "new game plus" is a reason to regenerate, not to keep.
- It doesn't sound like a song you already know. If it does, generate again.
- The ending is a hard stop, not a fade, so the last card can land on it.

## What happens next

The take that's chosen comes back as a WAV with its stems. Putting it in the game is one session, in the first wave after the file arrives (the overview's "Waiting on the user", item 11): the first audio file in the repository (with its line in `docs/art/CREDITS.md`, naming the tool and its terms), a way for the credits to play a file instead of an MML song, and the roll's pace set from the song's length.
