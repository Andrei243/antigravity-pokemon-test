# Plan 29 · Legendary scenes

**Goal**: every legendary and mythical Pokémon the game lets a player meet in a place of its own is met through a short staged scene before its battle, built the way Spear Pillar's altar, the Distortion World's last room and Sky Pillar's awakening are: the music drops, the camera kit takes the picture to the Pokémon, a scene effect and a set piece say what it is, it cries, a line of our own says what the player sees, and the battle's own way in (plan 09 · L14's `Legendary` or `Mythical`) takes over. Today about half of them are a cry, one line and `wildbattle`.

Written 2026-10-31 at the user's request, before any session, from an audit of `main` at `8586d3e7` (every `wildbattle`, `roamer start` and scene of a legendary or mythical species in `PokemonPlatinumEngine/Data/scripts`, the overlays of `Data/world/*/overlays`, and plans 02, 19 to 22). Same shape as the other plans: where we are, design, sessions, decisions.

**Nothing outside the engine.** Every scene is a script of `Data/scripts` played by the engine itself, with the camera kit (plan 09 · L4), the scene effects (L11), the set pieces (L12), the field's sounds and cries and our own lines. No video, no picture or sound made by another program or an outside AI service, nothing of the games' (lines on the original's beats, in our own words, as every scene so far). Every game is played by the modern rules (plan 06 · R19); nothing here changes a battle, a level, a flag's meaning or what a Pokémon is.

## Where we are

Four kinds, by what stands before the battle:

- **Staged**: a scene (`scene` … `endscene`) that moves the camera (`camera pan|path|zoom|follow`, `letterbox`, `slowmo`) and lights a scene effect or plays a set piece, with people or the Pokémon walked.
- **Light**: a scene with lines, a fade, an emote, a cry and at most one effect or `camera shake`, but the camera never leaves the player.
- **Bare**: `cry`, one line, `wildbattle` (some inside an empty `scene` marker).
- **Missing**: the encounter is not in the game. *Event* marks the ones the games gave only by a distribution; *owed* the ones a region plan has still to build.

A Pokémon that **roams** (`roamer start`) is met in the grass, where the original has no scene either: plan 09 · L14's way in is its whole meeting, and only the scene that sets it roaming is judged here.

### The audit

| Region | Pokémon | Where (script) | Kind | What it has | The gap |
|---|---|---|---|---|---|
| Sinnoh | Dialga, Palkia (story) | `spear_pillar.txt` | Staged | bars, ten camera moves, six effects (rifts, bursts, a ripple), slow motion, walks | none: the model the rest follow |
| Sinnoh | Giratina (Origin) | `distortion_world_giratina_room.txt` | Light (before its battle) | three effects (shadow, ripple) and shakes before the battle; its five camera moves are Cynthia's, after it | LS1's test found it: no look before the battle; LS2 owes it |
| Sinnoh | Giratina (again) | `turnback_cave_giratina_room.txt` | Bare | cry, "a roar shakes the cave", battle | the roar shakes nothing; no shadow, no camera |
| Sinnoh | Dialga | `spear_pillar_dialga.txt` | Bare | text, question, cry, `camera shake`, battle | its header says "stood in for until L14", which is done: the rift is read and nothing comes through it |
| Sinnoh | Palkia | `spear_pillar_palkia.txt` | Bare | the same as Dialga's | the same |
| Sinnoh | Mesprit | `verity_cavern.txt` | Light (roams) | scene, the professor's walk, lines, cry | no camera to the Pokémon, nothing as it vanishes |
| Sinnoh | Azelf | `valor_cavern.txt` | Light | scene, lines, fade, emote, cry | no camera, no effect |
| Sinnoh | Uxie | `acuity_cavern.txt` | Bare | cry, two lines, battle | behind its two siblings |
| Sinnoh | Heatran | `stark_mountain_room_3.txt` (`Heatran`) | Bare | cry, line, battle | Charon's scene in the same room is staged; the battle itself is not |
| Sinnoh | Regigigas | `snowpoint_temple_b5f.txt` | Light | lines, the statue's `shake regigigas 8` (L12), cry | no camera, no light in the eyes |
| Sinnoh | Cresselia | `fullmoon_island_forest.txt` | Light (roams) | `flicker cresselia 6` (L12), cry, lines, a walk | no camera, nothing as it leaves |
| Sinnoh | Rotom | `OldChateauBackMiddleWestRoom.txt` | Bare | cry, battle | the television does nothing |
| Sinnoh | Articuno, Zapdos, Moltres | `EternaCitySouthHouse.txt` | Roams | Oak's lines | none: roamers |
| Sinnoh | Darkrai, Shaymin, Arceus, Manaphy, Phione | `newmoon_island.txt`, `flower_paradise.txt`, Hall of Origin | Missing (event) | the places stand empty; plan 03 · D12c left the Hall of Origin dead | decision 1 |
| Kanto | Articuno | `seafoam_islands_b4f.txt` | Bare | empty scene marker, cry, line, battle | everything |
| Kanto | Zapdos | `power_plant.txt` | Bare | the same | everything |
| Kanto | Moltres | `mt_ember_summit.txt` | Bare | the same | everything |
| Kanto | Mewtwo | `cerulean_cave_b1f.txt` | Bare | the same | everything |
| Kanto | Raikou, Entei or Suicune (FireRed's roaming beast) | — | Missing | not built | session LS9 builds it |
| Johto | Raikou, Entei, Suicune (they wake) | `burned_tower_b1f.txt` | Light | three cries, `flicker` on each, lines, emote | no camera to the three, nothing as they leap away |
| Johto | Suicune (the chase) | `cianwood.txt`, `burned_tower_1f.txt` | Light | walks, lines, cry | no camera |
| Johto | Suicune (the battle) | Route 25 and the Kanto stops | Missing (owed) | held back by plan 20 · J15 | built with J15's Kanto, recipe here |
| Johto | Ho-Oh | `bell_tower_roof.txt` | Light | the dance's lines, the bell's sound, a fade, cry | it arrives in the dark: no flight, no light, no camera |
| Johto | Lugia | `whirl_islands_b3f_lugia_cave.txt` | Bare | cry, line, battle | everything |
| Johto | Moltres | `mount_silver_cave_moltres_chamber.txt` | Bare | empty scene marker, cry, line, battle | everything |
| Johto | Articuno, Zapdos | Seafoam (locked), Route 10 | Missing (owed) | plan 20 · J15 | built with J15, recipe here |
| Johto | Kyogre, Groudon, Rayquaza | the Embedded Tower | Missing (owed) | plan 20 · J15 | built with J15, recipe here |
| Johto | Celebi | Ilex Forest's shrine | Missing (event) | left out by J10's overlays | decision 1 |
| Johto | Raikou, Entei | grass | Roams | — | none |
| Hoenn | Kyogre (it wakes) | `seafloor_cavern_room_9.txt` | Staged | sparkles, a burst, two shakes, Archie's walk | none |
| Hoenn | Kyogre, Groudon, Rayquaza | `sootopolis_city.txt` | Staged | nine camera moves, fades, walks | none |
| Hoenn | Rayquaza (it wakes) | `sky_pillar_top.txt` (`Awaken`) | Staged | camera pan, two shakes, walk on the spot, flight | none |
| Hoenn | Rayquaza (battle) | `sky_pillar_top.txt` (`Rayquaza`) | Bare | cry, line, battle | the awakening's care stops at the battle |
| Hoenn | Kyogre | `marine_cave_end.txt` | Light | scene, cry, `camera shake` | no camera to the pool, nothing rises |
| Hoenn | Groudon | `terra_cave_end.txt` | Light | the same | the same |
| Hoenn | Regirock, Regice, Registeel | `desert_ruins.txt`, `island_cave.txt`, `ancient_tomb.txt` | Bare | the wall's opening is shaken; the Regi itself: cry, line, battle | the statue never wakes |
| Hoenn | Latias, Latios | `southern_island_interior.txt` | Light | scene, cry | it comes down at once; no flight, no camera |
| Hoenn | Latias or Latios (roams) | `littleroot_town_brendans_house_1f.txt` | Light | the television's scene | none: roamer |
| Hoenn | Deoxys | `birth_island_exterior.txt` | Light | `camera shake`, `effect burst`, show, cry | no camera, it is simply there |
| Hoenn | Mew | `faraway_island_interior.txt` | Light | scene, emote, cry | Emerald's flicker in the grass left out ("ours") |
| Hoenn | Lugia | `navel_rock_bottom.txt` | Light | two shakes, cry | no camera, it doesn't rise |
| Hoenn | Ho-Oh | `navel_rock_top.txt` | Light | shake, cry | the file's own header names the camera pan it left out |
| Hoenn | Jirachi | — | Missing (event) | — | decision 1 |
| Unova | Kyurem and Zekrom (story) | `giant_chasm_6.txt` (`Ghetsis`) | Light | seven cries, thirty lines, ten fades | the fusion happens behind fades; no camera, no effect |
| Unova | Kyurem (at rest) | `giant_chasm_6.txt` (`KyuremRests`) | Bare | cry, line, battle | everything |
| Unova | Cobalion, Virizion, Terrakion | `unova_route_13.txt`, `_11.txt`, `_22.txt` | Bare | cry, line, battle | everything |
| Unova | Azelf, Mesprit | `unova_route_23.txt`, `celestial_tower.txt` | Bare | cry, line, battle | everything |
| Unova | Heatran | `reversal_mountain_5.txt` | Bare | the Magma Stone's cry, line, battle | everything |
| Unova | Regirock, Regice, Registeel | `rock_peak_chamber.txt`, `iceberg_chamber.txt`, `iron_chamber.txt` | Bare | cry, line, battle | the same as Hoenn's |
| Unova | Uxie, Cresselia, Latias, Latios, Regigigas, Reshiram, Zekrom, Tornadus, Thundurus, Landorus | Nacrene, the Marvelous Bridge, the Dreamyard, Twist Mountain, the Dragonspiral Tower, the Abundant Shrine | Missing (owed) | plan 22 · U10, part c on | built with U10, recipe here |
| Unova | Victini, Keldeo, Meloetta, Genesect | — | Missing (event) | — | decision 1 |

**Counts** (one row a meeting, the roamers' own grass meetings left out): **5 staged** (Spear Pillar's altar, the Distortion World, the Seafloor Cavern, Sootopolis, Sky Pillar's awakening; Charon's staged scene in Heatran's room is counted with Heatran's bare battle), **16 light**, **19 bare** (a trio such as the Regis or the Swords of Justice a row), and **missing**: 5 rows not built (FireRed's roaming beast, which LS9 builds, and 4 owed by plans 20 and 22, about 16 species) and 4 rows given only by events (about 14 species). The staged five are all story scenes; **no legendary battle of the post-game, in any region, is staged**.

## Design

### The legendary's beat (the recipe)

Every scene of this plan is the same five beats, about six to nine seconds, written in the place's own script file:

1. **Hush** (0.5 s): `scene "<Name> at <place>"`, `music stop`, `face player <the way>`, `letterbox on 0.4`.
2. **Look** (1 to 2 s): `camera pan X Y 1.2` to the Pokémon's tile (or `camera path` for one met high above or far off), then `camera zoom 1.4 0.8`. A room (`MapSetting.Indoors`) takes the zoom alone; a room is seen whole and the pan only moves within the zoom's slack.
3. **Sign** (1 to 3 s): one scene effect, by the Pokémon's nature, on its tile or between it and the player (style guide, "Life", "Scene effects"), and one set piece on the Pokémon itself where it fits:

   | Kind of Pokémon | Effect | Set piece and camera |
   |---|---|---|
   | Time, space, the other world (Dialga, Palkia, Giratina, Darkrai) | `rift` held, `ripple` | `slowmo 0.5 1.5` |
   | Fire, lava, the sun (Moltres, Ho-Oh, Heatran, Groudon, Reshiram) | `burst`, then `ripple` (heat) | `camera shake 0.6` |
   | Lightning (Zapdos, Raikou, Thundurus, Zekrom) | two `burst`s 0.4 s apart | `camera shake 0.4` on each |
   | Ice, sea, rain (Articuno, Lugia, Kyogre, Suicune, Kyurem, Regice) | `sparkles`, `ripple` | `camera shake 0.5` for the sea's ones |
   | Stone and steel giants (the Regis, Regigigas, Terrakion, Cobalion) | `beam` on the statue's tile (its eyes lighting) | `shake <who> 8` (L12), `camera shake 0.8` |
   | Mind, dreams, the lakes (Mewtwo, Mew, Uxie, Mesprit, Azelf, Cresselia, Latias, Latios, Deoxys, Jirachi) | `sparkles`, `ripple` | `flicker <who>` for one that is there and not there |
   | Flight overhead (Rayquaza, Lugia, Ho-Oh, the forces of nature) | `shadow` crossing the ground toward the player | `camera path` up the shadow's way |
   | Grass and woods (Virizion, Celebi, Shaymin) | `sparkles` | `camera follow <who>` |
   | Ghost in a machine (Rotom) | `sparkles` on the set's tile | the set's own face: `flicker` on the television prop's person, or a sound of the set |

4. **Voice** (1 to 1.5 s): `cry "<species>"` with `camera shake` where the Pokémon is loud, an `emote player exclaim` where the player would start, and **one line of our own**, two at most, that says what the player sees (never the games' line). Lines fit the text box (`tools/ContentCheck`).
5. **Hand over**: `camera release 0.4`, `letterbox off 0.3`, `endscene`, then `wildbattle` as it is today, unchanged (species, level, `nofleeing`, `holding`) and everything after it unchanged. The release comes before the battle because L14's way in holds its own lens over the rig's zoom and widens from it (`CameraRig.Lens`); a session may try keeping a zoom of up to 1.4 into the battle's opening and judge by the `l36`-style shots, and says which it chose.

Rules that hold the recipe to the rest of the game:

- **The scene ends before the battle**, as `marine_cave_end.txt` and `birth_island_exterior.txt` do: the hold skips the beat and never the battle (plan 12 · Q20), and `SceneSkipTests` plays and skips every new scene by itself.
- **What the scene leaves is what today's script leaves.** The flags, the variables, `hide`, `show` and the battle are the same lines in the same order; a session moves nothing but presentation, so the golden `scripts.txt` moves by the lines said and the steps of each touched block alone.
- **Only the Pokémon's own tile and the tiles between it and the player** get effects, so nothing is lit through a wall; a Pokémon that is no person of the map yet (Deoxys's stone, Mew in the grass) is `show`n before the camera looks.
- **Roamers stay as they are**: the scene that sets one roaming may be staged (its leaving: `shadow` and `flicker`), the grass meeting is L14's.
- **No new engine work by default.** The kit already has every beat; a session that finds a beat it can't write (a Pokémon seen flying in over the field, a light in a statue's eyes as a held face) stops at the shots, writes what it wants as a new effect kind with the style guide first, and leaves it to LS10.
- **A test that holds the plan**: LS1 adds `LegendaryScenesTests.EveryLegendaryBattleOfAPlaceHasItsBeat`: for every `wildbattle` of a species whose data says `Legendary` or `Mythical` in every script file, the script has a `scene` before it that holds a `camera` line and an `effect`, `shake`, `flicker` or `anim` line. Today's bare and light rows are its list of exceptions (`StillBare`, each with the session that owes it); a session takes its rows off, and an exception that has its beat fails as stale, as plan 24's oracle does. A new place built by plans 20 and 22 is held by it from its first line.

### Shots and sound

- Each session shoots its scenes through the harness mode that already plays the place (named in each session), at three moments of the beat (look, sign, voice: `<prefix>_<species>_1_look` to `_3_voice`) and the battle's opening after it, and looks at each with the Read tool before calling it done; before/after pairs go to the user.
- The sounds are the kit's own: each effect brings its sound (L11), cries are `cry`, a rumble is `sound "use_earthquake"` and far thunder `sound "thunder_rumble"` (names from [`docs/sound-effects.md`](../sound-effects.md)). A new sound is plan 05's to make (`docs/sound-effects.md` first), never this plan's.
- `dotnet run --project tools/VoiceTool -- catalogue --add-missing` and `tools/TextTool report` after the lines change: the Romanian of a rewritten line goes stale and is plan 17 · N6's next slice.

## Sessions

| # | Session | Delivers | Shots (harness mode) | Needs |
|---|---|---|---|---|
| LS1 | The recipe, the guard and Sinnoh's rifts | the style guide's "Legendary scenes" (the five beats, the table above, the time budget) first; `LegendaryScenesTests` with its `StillBare` list; Dialga's and Palkia's rifts (`spear_pillar_dialga.txt`, `_palkia.txt`: the rift effect held on the rift's tile, ripple, slow motion, the bars, a pan up to it, the stale L14 line of their headers rewritten) and Giratina in Turnback Cave (`turnback_cave_giratina_room.txt`: a shadow across the room, the ripple its line promises, `camera shake` with the roar) | `epilogue` (`EPILOGUE_FROM=summit`), `pillar` (`PILLAR_FROM=spring` and on to Turnback Cave) | — |
| LS2 | Sinnoh's lakes, Heatran, Regigigas, Cresselia, Rotom | the Distortion World's Giratina given its look before the battle (`distortion_world_giratina_room.txt` `Arrival` or the battle's scene: the camera up to it, found missing by LS1's test); Uxie brought up to its siblings and all three given the look and sign beats (`acuity_cavern.txt`, `valor_cavern.txt`, `verity_cavern.txt`: sparkles, a ripple, Mesprit's `flicker` as it goes); Heatran's own beat after Charon's scene (`stark_mountain_room_3.txt`: pan to the heart, burst, heat ripple, shake); Regigigas's eyes (`snowpoint_temple_b5f.txt`: beam on its tile with the shake it has); Cresselia's leaving (`fullmoon_island_forest.txt`: camera on it, shadow away); Rotom's television (`OldChateauBackMiddleWestRoom.txt`) | `sunyshore` (`SUNYSHORE_FROM=lakes`), `legends` (`LEGENDS_FROM=heatran`, `temple`, `island`), `epilogue` | LS1 |
| LS3 | Kanto's birds and Mewtwo | Articuno, Zapdos, Moltres and Mewtwo by the recipe's rows (`seafoam_islands_b4f.txt`, `power_plant.txt`, `mt_ember_summit.txt`, `cerulean_cave_b1f.txt`): each empty `scene` marker filled; Mewtwo's push-in zoom and ripple, its line kept as the will pressed on the player | `kanto` (K7's and K9's parts) | LS1 |
| LS4 | Johto's birds of the towers | Ho-Oh flying in over the Bell Tower instead of a fade (`bell_tower_roof.txt`: the bell's sound, a shadow over the roof, a `camera path` up and back, a burst as it lands, sparkles of its colours); Lugia rising in the Whirl Islands (`whirl_islands_b3f_lugia_cave.txt`: the wing's sparkles, a ripple, the cave shaken twice); Moltres on Mt. Silver (`mount_silver_cave_moltres_chamber.txt`) | `johto` (`JOHTO_FROM=partd`, `kanto`) | LS1 |
| LS5 | Johto's three beasts | the Burned Tower's waking (`burned_tower_b1f.txt`: the camera to each of the three in turn as it flickers, a burst for Entei, two for Raikou, sparkles for Suicune, a shadow as each leaps away, the bars round it); Suicune at Cianwood's shore (`cianwood.txt`: the camera follows it off) | `johto` (`JOHTO_FROM=well`, `sea`) | LS1 |
| LS6 | Hoenn's titans after the story | Rayquaza's battle on the pillar's top (`sky_pillar_top.txt` `Rayquaza`: a pan up, shadow, the cry with the shake the awakening has); Kyogre in the Marine Cave and Groudon in Terra Cave (`marine_cave_end.txt`, `terra_cave_end.txt`: camera to the pool or the magma, ripple, the Pokémon shown rising before the line) | `hoenn` (`HOENN_FROM=postgame`) | LS1 |
| LS7 | Hoenn's Regis and islands | the three Regis woken (`desert_ruins.txt`, `island_cave.txt`, `ancient_tomb.txt`: beam on the statue, `shake <who> 8`, camera); Southern Island's Lati (`southern_island_interior.txt`: a shadow, then it lands); Deoxys (`birth_island_exterior.txt`: camera to the stone first); Mew's flicker in the grass Emerald has (`faraway_island_interior.txt`); Lugia and Ho-Oh at Navel Rock with the pan the file's header names (`navel_rock_bottom.txt`, `navel_rock_top.txt`) | `hoenn` (`HOENN_FROM=postgame`) | LS1; shares the Regi rows with LS8 |
| LS8 | Unova's legends after the League | the Swords of Justice (`unova_route_13.txt`, `_11.txt`, `_22.txt`: each seen first at a distance and the camera follows it in), Azelf and Mesprit (`unova_route_23.txt`, `celestial_tower.txt`), Kyurem at rest (`giant_chasm_6.txt` `KyuremRests`: sparkles of cold, zoom), Heatran (`reversal_mountain_5.txt`), the three Regis (`rock_peak_chamber.txt`, `iceberg_chamber.txt`, `iron_chamber.txt`, LS7's recipe) | `unova` (`UNOVA_FROM=legends`, `oldroads`) | LS1; LS7 for the Regis' lines |
| LS9 | Kyurem's fusion and FireRed's roaming beast | the Giant Chasm's story scene given its camera (`giant_chasm_6.txt` `Ghetsis`: the bars, a pan to Kyurem, Zekrom's arrival as a shadow, the fusion as a burst and a ripple where today there is a fade; the fades that hide a walk stay); FireRed's roaming beast after the National Pokédex, by FireRed's rule of the starter chosen, with its scene at the Pokémon Center's television or the first sight (`roamer start`, `Roamers`' Kanto routes) | `unova` (`UNOVA_FROM=league`), `kanto` | LS1 |
| LS10 | What the kit lacked (optional) | only if LS1 to LS9 asked for it in their shots: a new scene effect kind (a flight across the picture, a light in a statue's eyes as a held face), style guide first, its look in `Graphics/SceneFx.cs` and its sound by plan 05; then the rows that waited for it rewritten | the `story` mode's `st60_effect_<kind>` | any of LS1 to LS9 |

**For the plans that still owe a legendary** (20 · J15: Suicune's battle, the Kanto birds of HeartGold, the Embedded Tower; 22 · U10 part c on: Uxie, Cresselia, the Lati, Regigigas, Reshiram and Zekrom, the forces of nature): the session that builds the place writes its meeting by this plan's recipe from the start, and `LegendaryScenesTests` holds it without an exception. Nothing of theirs is a session here.

Every session: `tools/ContentCheck` first, then the filtered tests of what it touches (`LegendaryScenesTests`, `SceneSkipTests`, `StoryTests`, the region's story tests, `ScriptTests`, `ContentTests`, `VoiceTests`, `ScriptTranslationTests`, `Golden` with `GOLDEN=accept` for `scripts.txt` and the blocks it names in its close-out); the full suite once on the merged wave. LS2 to LS9 touch different script files and can run side by side once LS1 has merged (the style guide's section and the test's list are LS1's); LS7 and LS8 agree the Regis' lines.

## Needs and gives

- **Needs**: plan 09 · L4, L11, L12, L14 (all done); plan 12 · Q20's scene skip (done); the places as built by plans 01, 02, 19, 20, 21 and 22.
- **Gives**: plan 26's voice catalogue new lines; plan 17 · N6 the Romanian of them; plans 20 and 22 a recipe and a test for the legendaries they still build; plan 23's regions the same from their first legendary.

## Risks

- **A scene that moves the clock moves every later shot** of its harness mode (the camera's and the field's time): each session runs its mode before and after and `diff`s, and says which later shots moved only for the clock.
- **A scene left open** (a battle before `endscene`, a release missed) leaves the bars or the zoom over the battle's way in. `ScriptLibrary.Problems` and `SceneSkipTests` catch the first; the recipe's last beat and the shots catch the second.
- **Light through walls**: an effect on a tile the camera shows through a cave's rock. The recipe keeps effects to the Pokémon's tile and the way between; the shots show the rest.
- **Lines that say too much**: our own words only, one or two lines, measured by `tools/ContentCheck`.

## Decisions for the user

1. **The event-only legendaries and mythicals** (Darkrai, Shaymin, Arceus, Manaphy and Phione, Celebi, Jirachi, Victini, Keldeo, Meloetta, Genesect): **DECIDED 2026-10-31 by the user: open them.** Session LS11 (added) gives each a way in by a post-game condition of our own, battled after a scene written by the recipe; its designs are proposed to the user one region at a time before building.
2. **A zoom into the battle**: recommended, the camera released before `wildbattle`, so L14's widening starts from the place's own lens as it does today. The other choice keeps the scene's zoom into the way in.

## Status

- [x] LS1 The recipe, the guard and Sinnoh's rifts (2026-11-01: the style guide's "Legendary scenes"; `LegendaryScenesTests` with 37 rows of `StillBare`; Dialga's and Palkia's rifts and Turnback Cave's Giratina staged. The test found the Distortion World's Giratina unstaged by its rule: its `Arrival` has the cry, shake and ripple but no look before the battle, so its row is LS2's. Lines changed: the two rifts' cries' lines ("Dialga: Gyugguh-gwaah!" became a line of what is seen; their Romanian is blank for N6), Giratina's kept. Decision 2 taken as recommended: the camera is released before `wildbattle`. Shots not taken in this session: `epilogue` and `pillar` are owed a look at `ep2*` and `sp18*`.)
- [ ] LS2 Sinnoh's lakes, Heatran, Regigigas, Cresselia, Rotom
- [ ] LS3 Kanto's birds and Mewtwo
- [x] LS4 Johto's birds of the towers (2026-11-01: Ho-Oh's arrival after the dance is a shadow from the north down to the roof with a `camera path` up its way and back, a burst and its sparkles where it lands and the roof shaken, in place of the fade; its battle script a beat of its own (camera, burst, heat ripple, shake). Lugia's cave: the wing's sparkles, a ripple and the cave shaken twice round its cry. Moltres's chamber: a burst, a ripple and a shake, its `scene` closed before the battle where it had wrapped it. Lines: Moltres's "Gyaoo!" dropped from its line (Romanian filled to match), the rest kept. Three rows off `StillBare`. Camera released before `wildbattle`, as LS1. Shots not taken in this session: `johto` (`JOHTO_FROM=partd`, `kanto`) is owed a look at the three beats.)
- [x] LS5 Johto's three beasts (2026-11-01: the Burned Tower's waking under the bars, the camera on the three and then to each in turn as it flickers: two bursts with a shake each for Raikou, a burst and a shake for Entei, sparkles for Suicune, and a shadow as each leaps away (west, north, east past the stairs Eusine comes down). Cianwood's Suicune under the bars with the camera on it, sparkles, a shadow north as it bounds off and a `camera path` out over the sea, released before Eusine walks up. No battle and no `StillBare` row: the beasts are roamers and a chase. No line changed. Shots not taken in this session: `johto` (`JOHTO_FROM=well`, `sea`) is owed a look.)
- [ ] LS6 Hoenn's titans after the story
- [ ] LS7 Hoenn's Regis and islands
- [ ] LS8 Unova's legends after the League
- [ ] LS9 Kyurem's fusion and FireRed's roaming beast
- [ ] LS10 What the kit lacked (optional)

### LS11 (added with decision 1): the event legendaries
Open each event-only species named in decision 1 by a post-game condition of our own (a region's Hall of Fame plus a place-specific step), with a staged scene by the five-beat recipe and the unchanged wildbattle; one proposal per region for the user's approval first. Tests: LegendaryScenesTests covers them.
