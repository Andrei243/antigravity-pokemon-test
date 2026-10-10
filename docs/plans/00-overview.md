# 00 · Overview: what is left, in lanes that run side by side

Written on 2026-10-07, after every one of the 26 plans was read in full and checked against the code. The plans keep what each session delivers, needs and leaves for the user to decide; this file says **when** each session runs and **what runs beside it**, so that several sessions (one agent each, in a worktree of its own) can be done at once and merged together. It replaces the README's "Order", which was written before any session.

The idea in one paragraph: the work is split into six **lanes** whose sessions touch different parts of the code, plus work that can go **beside** any of them. A **wave** is one round of sessions, at most one from each lane, started from the same commit and merged together at its end. Lanes 1 (the story) and 2 (the map and its rooms) are the spine: lane 2 stays a wave ahead of the chapter that needs its Gym or room. The other lanes have only soft gates: when one is behind, the chapter leaves the beat out and writes where it goes, as plan 02 already does.

## Where we are

About 350 sittings are left, some twenty of them marked optional.

| Plan | Done | Left (sittings) | Notes |
|---|---|---|---|
| 01 · Sinnoh map | M1–M8, M9 parts 1, 1b, 2a and 2b | ~10 | M9 2c and 2d, M10, M11, M12 |
| 02 · Story | S1, S2, S4–S8 | ~11 | S9 to S15; S11 and S15 are more than one sitting |
| 03 · National Pokédex | D1, D5–D11, every model batch to Pecharunt (all 1,025 hand-built; Paldea 3a–3c) | 4 | D12 (three parts), D13 |
| 04 · Graphics | everything | 0 | what G11 owes (the M6–M9 cities in `profile`) goes to 01 · M12 |
| 05 · Sound and music | A1–A5, A7 | ~3 | A6 rolls on inside the sessions that open places and scenes |
| 06 · Game mechanics | R1–R14 | ~23 | R16, R17 and R18 are several sittings each |
| 07 · Online | — | 8 | most of O2 and O3 came with plan 06; O8 optional |
| 08 · Platinum's details | P6 | 22 | P3's met data and markings came with R12 |
| 09 · Looks | — | 15 | |
| 10 · Companions | F1 | 11 | F8 optional |
| 11 · People | C1, C5 (C9, C10 in part) | 11 | the looks are a table, `characters.json` |
| 12 · Quality of life | — | 30 | |
| 13 · Ways to play | — | 26 | V11, V14, V25 optional |
| 14 · Battle formats | — | 15 | B2, B13 optional |
| 15 · Side activities | — | 8 | E6–E8 optional |
| 16 · Tooling and shipping | T5 (and a CI build-and-test workflow) | 23 | |
| 17 · Localisation | — | 6 | only if a second language is wanted |
| 18 · Other regions | — | 10 | |
| 19 · Kanto | — | 9 | |
| 20 · Johto | — | 15 | J15 optional |
| 21 · Hoenn | — | 12 | |
| 22 · Unova | — | 12 | |
| 23 · Later regions | — | 24 | Z17–Z20 (Hisui) optional |
| 24 · Guards | — | 6 | X3 is overdue: story versions 3 and 4 shipped with no saves kept to test against |
| 25 · Battle presentation | — | 15 | |
| 26 · Voice | — (only the plan is written) | 8 | |

Plan 13 and plan 26 both number their sessions V1, V2…; this file always writes the plan with the ID (13 · V1, 26 · V1), and so should anyone citing them.

## How a wave runs

1. **Start together.** Every session of a wave starts from the same commit of the branch, in a worktree of its own (`git worktree add`), on a branch named for its session.
2. **Each session tests only itself.** It builds, runs the tests of what it touches (`dotnet test --filter`), runs its harness mode before and after in its own worktree (the copy method in CLAUDE.md), records its outcome and ticks its plan, and commits in its worktree. It never runs the full suite (CLAUDE.md, "The full suite runs once").
3. **Merge in this order**, each with `git merge --no-ff`: lane 6 (tools and guards: what the others build on), then 3 (rules), 2 (map and rooms), 1 (story), 4, 5, then the work beside. Resolve by the table "Hot files" below. Then build the solution and the tools outside it (`tools/ShotHarness`, `tools/MusicRender`), regenerate what is generated, and run **the full suite once**. Fix what fails there, on the merged tree, then commit, push and open the pull request.
4. **How many at once.** A wave holds up to six sessions and the work beside. This machine has carried four agents with a full test run beside them (the load reached about 50, and every run slowed). With fewer hands, keep lanes 1 and 2 and take the other lanes in turn. A lane that waits a wave only slips its soft gates.
5. **Time nothing while a wave runs.** `profile`'s milliseconds mean something only on a quiet machine. A session whose proof is a timing (G11's rule) runs its timing after the merge, alone.
6. **No two "whoever lands first" sessions for the same seam in one wave.** The seams and their owners are in the table below.
7. **At most two sessions adding script commands in a wave, besides the chapter.** They append at the end of the `Op` enum, the parser and runner cases, both hosts, `ScriptTests.EveryCommand` and `docs/scripts.md`, and the merge keeps both.
8. **While lane 3 changes the battle's rules (R19 to R29, B1, B3 to B7), lane 4 takes only sessions that don't change `BattleCore`.** Hooking one event onto a named line is allowed; changing what a turn does is not.

## The lanes

| Lane | What it holds | Where it works |
|---|---|---|
| 1 · Story | plan 02's chapters and the groundwork they write on (08 · P7, the overlays' trainers from the table), then plan 03 · D12 and the region chapters | `Data/scripts`, overlays, the chapter's own rooms, its harness mode |
| 2 · Map and rooms | plan 01 (M9's rest, M9 part 2, M10, M11, M12), then the region importers | `Data/maps`, `world.json` and `--data`, the room renderer, `Gym*`, `RegionDatabase` |
| 3 · Rules | plan 06 R14–R30, plan 14, plan 24 · X5 and X6 | `Battle/Sim`, `Models`, the data importer, `coverage.md` |
| 4 · Battle on screen | plan 25, plan 08's battle sessions (P1, P2, P5, P10, P11), 09 · L1 and L2, 12 · Q10–Q12 and Q17, 11 · C1, C12 and C13 | `BattleEngine.Show`, `BattleAnimator`, `MoveFx`, `ModernUi.Battle` |
| 5 · Field and people | plan 09's field sessions, plan 10, plan 11's people, 08 · P8, P9 | `WorldRenderer`, `FieldLife`, `CharacterModels`, `characters.json` |
| 6 · Comfort, tools and guards | plan 12's rest, plan 08's menus and Pokédex (P3, P4, P13, P18, P19), plan 15, plan 16, plan 24 · X1–X4, 26 · V1, 07 · O2 | menus, saves, the title, the test project, `tools/`, CI |
| Beside | 03 · Paldea 3a–3c, 08 · P14–P17 (the Pokédex's written entries), 09 · L6–L8 (female looks), 05 · A6 songs written ahead | model files, one data file, song files: nothing anyone else edits |

### Sessions this file splits

Some sessions are too big for one sitting, or hold parts with different gates. They are split here. Each part ticks its plan's line only when the last part is done, and its outcome names the part.

| Session | Parts |
|---|---|
| 01 · M9's rest | **1b**: rooms drawn with relief, the Pastoria Gym, the Oreburgh Gym to the original's plan |
| 01 · M9 part 2 | **2a** the Canalave Gym (its lifts); **2b** the Snowpoint Gym (ice, snowballs); **2c** the Sunyshore Gym (gear walkways); **2d** the Elite Four's rooms, Cynthia's, the Hall of Fame's, the door's guard |
| 01 · M10 | **a** the Battle Zone's Fight, Survival and Resort Areas, Routes 224–230; **b** Stark Mountain, Snowpoint Temple inside, Fullmoon and Newmoon Islands, Flower Paradise, the Frontier's buildings, the last fourteen stand-ins |
| 01 · M11 | **a** the south-west's and the centre's remaining rooms, the Old Chateau among them; **b** the east and the sea; **c** the north; **d** the gate houses, the Pokémon Centers' upper floors, the Global Terminal, the Department Store, the tutors' rooms. A room a chapter needs is the chapter's, and M11 leaves it. |
| 02 · S11 | **a** Lake Acuity, Jupiter, Snowpoint City and the Icicle Badge; **b** the Galactic HQ |
| 02 · S15 | **a**, **b**, **c**: "After the Hall of Fame", lane 1 |
| 03 · the last species | **Paldea 3a** species 951–975 (Capsakid on); **3b** 976–1000; **3c** 1001–1025 (to Pecharunt). Each part brings its species' forms, 26 in all. |
| 03 · D12 | **a** the test that every species can be had, the post-game's tables, gifts and the forms' sources; **b** the zone for Generations 5–9; **c** the legendaries' quests |
| 06 · R14 | **a** the day's events, berries and the lottery; **b** the Pokétch's other apps; **c** Poffins and the contest condition |
| 06 · R16 | **a** the Underground (mining, spheres, traps, the secret base, flags); **b** fossils, the Game Corner, the Villa, Pal Park as a place (it builds the Game Corner's room, which M11 leaves); **c** Amity Square's walk |
| 06 · R17 | **a** the Super Contests; **b** Ball Capsules, Seals and ribbons |
| 06 · R18 | **a–d**: the Tower with Battle Points and the Brains' frame, then the Factory, the Hall and Castle, the Arcade |
| 06 · R28 | **a** held items, medicine and balls; **b** the rest |

## Waves 0 to 11: to Sinnoh's Hall of Fame

| Wave | 1 · Story | 2 · Map and rooms | 3 · Rules | 4 · Battle on screen | 5 · Field and people | 6 · Comfort, tools and guards | Beside |
|---|---|---|---|---|---|---|---|
| 0 | — | — | — | — | — | **16 · T5**, alone | 03 · Paldea 3a; 05 · A6 songs ahead |
| 1 | 08 · P7 | 01 · M9 1b | 06 · R14a | 11 · C1 | 10 · F1 | 16 · T16 | 03 · Paldea 3b |
| 2 | 02 · S7 with 08 · P12 | 01 · M9 2a Canalave | 06 · R14b | 12 · Q10 | 11 · C5 | 24 · X1 | 03 · Paldea 3c |
| 3 | 02 · S8 | 01 · M9 2b Snowpoint | 06 · R14c | 12 · Q11 | 09 · L11 | 24 · X2 | 05 · A6 |
| 4 | 02 · S9 | 01 · M9 2c Sunyshore | 06 · R15 | 11 · C12 | 11 · C6 | 24 · X3 | |
| 5 | 02 · S10 | 01 · M9 2d the League | 06 · R17a | 25 · Y1 | 11 · C11 | 12 · Q1 | |
| 6 | 02 · S11a | 01 · M10a | 06 · R17b | 08 · P5 | 09 · L4 | 16 · T8 | |
| 7 | 02 · S11b | 01 · M10b | 06 · R16a | 25 · Y2 | 09 · L13 | 16 · T3 | |
| 8 | 02 · S12 | 01 · M11a | 14 · B1 | 25 · Y3 | 09 · L12 | 12 · Q2 | |
| 9 | 02 · S13 | 01 · M11b | 06 · R16b | 09 · L2 | 09 · L15 | 12 · Q20 | |
| 10 | 02 · S14 | 01 · M11c | 14 · B14 | 08 · P1 | 09 · L14 | 08 · P13 | 08 · P14 |
| 11 | **16 · T11, then 16 · T12**: a freeze, nothing else runs | | | | | | 08 · P15; 05 · A6 |

What each cell is, and why it stands where it does:

- **Wave 0 · 16 · T5, the harness in files.** The harness is one file of over 5,000 lines that every session adds shots to, and it reaches the engine by reflection. T5 splits it into a file per mode behind a `Driver`, and moves the battle starts into `GameEngine.Battles.cs`. After it, sessions add their own mode files instead of colliding in one. It runs alone because it would collide with every other session; only the work beside, which never touches the harness, runs with it.
- **Wave 1 is the groundwork the chapters write on.**
  - 08 · P7 makes the overlays' trainers lines only, read from the trainer table, before more chapters add trainers. It raises the story version.
  - 11 · C1 moves the 25 looks out of `CharacterStyle`'s switch into `characters.json` before more chapters add to the switch.
  - 10 · F1 puts Platinum's Pokémon on the map. The Psyduck on Route 210 that ends S7 and blocks S9 is one.
  - 01 · M9 1b is M9's leftover: rooms drawn with relief, then the Pastoria Gym (its water floor at three heights, Crasher Wake, the Fen Badge) and the Oreburgh Gym rebuilt to the original's plan, with the flags Roark's script also sets. S8 needs the Fen Badge.
  - 06 · R14a is the first of R14's three parts: the day's events, berries and the lottery.
  - 16 · T16 sets up the machine for parallel worktrees: a model cache shared between them, `baseline.sh`, and `diff` exiting non-zero. Beyond its plan it also turns the tests' hard-coded counts into floors or computed numbers. The last merge failed on exactly that: two steps each added six rooms to the 130 maps and each wrote 136, where the merged tree had 142.
- **Wave 2.**
  - S7 starts on that groundwork and takes 08 · P12 (Spiritomb's tower on Route 209, in its area) with it.
  - 12 · Q10 lands `RulesDefault`, which C12, Q14, Q18 and Q19 build on.
  - 11 · C5 makes the named cast of the first half (Mira, Bebe, Crasher Wake, Cheryl, the Pokétch's president).
  - 24 · X1 makes CLAUDE.md's bans build errors. It also adds the one reader of real time, `WallClock`, which the bans need and which R14 and later sessions use. It also fixes the Hall of Fame's `DateTime.Now` (`GameEngine.Scripts.cs`), which its plan missed.
  - R14b is the Pokétch's other apps. It is not R14's Poffins, because their stand-in would stand in Hearthome, where S7 is writing.
- **Wave 3.**
  - S8 can run because M9 1b built Pastoria's Gym in wave 1.
  - 12 · Q11 turns the abilities and items a battle shows into log events. Then the AI no longer reads them by matching English text, which would break any translation and which 25 · Y3 needs.
  - 09 · L11 adds the story's effects (bursts, beams, rifts), which S10's explosion at Lake Valor needs first.
  - 24 · X2 holds Platinum's battles and teams in golden files before the rules lane changes them.
- **Wave 4.**
  - R15 (breeding) takes nature and shininess from the personality, which changes every Pokémon made. X2's golden files are regenerated as it merges, and its outcome must say so.
  - 11 · C6 makes the second half's named cast ahead of S10 (Riley, Byron, Saturn), S11 (Candice) and S13 (Volkner). C6's test that every class has a look waits for C4.
  - 24 · X3 keeps a save of every version and the scripts' golden walk. It is the most overdue of the guards.
  - 11 · C12 lets trainers speak in battle, so that from S10 on each chapter writes its bosses' lines; C12 also writes those S5 and S6 owe.
- **Waves 5 to 10** keep lane 2 a wave or more ahead of the Gym each chapter needs: Canalave by S10, Snowpoint by S11, Sunyshore by S13, the League's rooms by S14. Beside them:
  - lane 4 does the battle sessions that touch the core's lines before lane 3 starts changing the core in wave 12;
  - lane 5 brings the camera kit (L4) before S12, and the bosses' ways in and the bands (L13, L15), the set pieces (L12) and the legendaries' ways in (L14) before S14;
  - lane 6 makes saving safe (Q1, Q2), adds the crash handler and the command line (T3), and lands the content check (T8) before M11's rooms.
  - R16 is split: its first part (the Underground) and its second (fossils, the Game Corner, the Villa, Pal Park as a place) need nothing from F3 and F4. Its third, Amity Square's walk, waits for them.
  - 14 · B1 (inverse battles) and B14 (the masters' file) fill lane 3 while it must keep off the core.
  - B14 extends R12's `choosepokemon` instead of adding a second command for choosing a party member.
- **Wave 11 · the freeze.** With Sinnoh playable to the Hall of Fame, 16 · T11 takes the game's states out of `GameEngine` into a `GameFlow` without renderers, and T12 makes a windowless game of it. Both touch nearly every part of the engine. After them, a chapter can be played through by a test, and the playthrough test (T13) and recorded play (T19) become possible. If a second language is wanted, 17 · N1 and N2 go into the same freeze, after T12, because N2 is cheapest before lane 3 adds the later generations' lines.

## After the Hall of Fame: the lanes go on

From wave 12, each lane takes its next session whose gates are met. The waves are no longer written out, but the rules above still hold. The list for each lane is in its order; "after X" is a hard gate.

**Lane 1 · Story**
- 02 · S15a: the National Pokédex, the Poké Radar given, the ship, and the Fight, Survival and Resort Areas with Routes 224–230.
- S15b: Stark Mountain, Buck and Heatran, Snowpoint Temple and the Regis, the roaming legendaries, Cresselia.
- S15c: Rotom and the Old Chateau, the Gym Leaders' rematches, the Celestic ruins scene plan 23 asks for.
- Then the Pokédex's completion: 03 · D12a (the test that every species can be had, the post-game's tables, gifts and the forms' sources), D12b (the zone for Generations 5–9, after decision 03·4), D12c (the legendaries' quests, with the Azure Flute and the Hall of Origin), D13.
- Then 15 · E5 (the Bug-Catching Contest's park, in D12's zone).
- Then the lane becomes a region lane (below).

**Lane 2 · Map and rooms**
- 01 · M11d: the gate houses made rooms. `WorldWalk` stops passing through them. It also brings the Pokémon Centers' upper floors, the Global Terminal, the Department Store and the move tutors' rooms.
- 01 · M12: the Town Map, built as the one region map that 18 · W3 and W9, 20 · J3 and 12 · Q22 reuse. Also the performance pass (the M6–M9 cities and Gyms added to `profile`, `GetNpcAt`/`GetWarpAt`/`IsWalkable` indexed by chunk) and the held-back items.
- Then plan 18's readers: W6, then W7, once the pins are agreed.

**Lane 3 · Rules**
- 06 · R19 (the modern groundwork).
- R20 to R23, one after another: R20 sets the pattern the other three follow. If the user confirms the later mechanics' beats in the story (06 · decision 2), each of these sessions writes its own beat into the chapter that has it: Maylene's Mega Evolution in S8, Z-Moves at Celestic in S9, Dynamax at Spear Pillar in S12, Terastallization at the League in S14.
- R24, R25 and R26, each with its effects in a partial file of its own. The data importer and the coverage are re-run after the merge, so two of them can share a wave.
- R27, R28a, R28b, R29.
- 06 · R16c Amity Square, after 10 · F3 and F4.
- R18a–d, the Battle Frontier, after S15.
- 14 · B3, B4, B5, B6, B7, each one in a wave with no other core session.
- B8 to B12, after R18; B15, after B14 and S15.
- 24 · X5, then X6 after R19, once node is allowed in CI.
- R30, last.

**Lane 4 · Battle on screen.** While lane 3 changes the core, only these:
- 25 · Y4, 09 · L1, 08 · P2, 25 · Y6, then Y7 to Y10 in the story's order;
- 12 · Q12, 08 · P10, then P11, 11 · C13;
- 25 · Y5 (its one event, in a wave whose rules session keeps out of `BattleCore.Moves`), 12 · Q17 (after Q16);
- Y14 after R21, Y15 after R22, Y11 to Y13 after R24 to R26.

**Lane 5 · Field and people**
- 11 · C2, C3, C7, C8, then C4, which also brings C6's test.
- 09 · L3 (the rich text box), L10 (reflections).
- 10 · F2, F3 (which generalises S6's `Follower.cs` rather than making a second one), F4.
- 11 · C9, C10.
- 10 · F5, F6, F7, F9 (its marks under a name that isn't R12's `Pokemon.Marks`), F10, F11, F12.
- 09 · L9; 08 · P8, then P9; 10 · F8 (optional).

**Lane 6 · Comfort, tools and guards**
- 12 · Q14, which backfills the objectives of S4 to S14.
- 26 · V1, so the remaining chapters write who speaks.
- 12 · Q18, 07 · O2 (the Pokémon's own id and the save's version, which three other plans would otherwise each write), 24 · X4.
- 12 · Q4, Q5, Q8, Q9, Q6, Q7.
- 08 · P3, P4, P18, P19; 09 · L5; 15 · E1, E2.
- 12 · Q13, Q15, Q16, Q19, Q21, Q22, Q23, Q24, Q25 (after 08 · P10), Q26 to Q30.
- 16 · T6, T7, T13 (after T7 and T12), T9 (with M11d), T10, T17 to T22, T4 (after decision 16·4), T1 and T2 (after decisions 16·1–3 and 5), T23, T24, T14, T15 (optional).
- 15 · E3, E4.

**Beside**: 08 · P15, P16 and P17 (after P19), 09 · L6, L7 and L8 (after L1), and 05 · A6 as scenes ask for songs.

## Then: the other regions and the rest

These are the plans the README put after Sinnoh's Hall of Fame. Plan 19 allows Kanto sooner: no Kanto chapter before the Sinnoh chapter of the same number. If the user wants the regions started earlier, 18 · W1, W10 and W6 can take lane 6's and lane 2's places in waves 12 and on.

- **The groundwork (plan 18).**
  - W1, then W10 (after decisions 18·10 and 18·11: a NEW GAME that can start in Sinnoh), then W3.
  - Then W2, in a wave where no other session gives a badge: it removes the `Badge` enum and raises the story version.
  - Then W4, W5, W8, W9.
  - W6, then W7, in parallel with W1 to W5 (they share only `Sources.cs`).
- **Up to three regions at once**, each in its own `Data/world/<region>` and music folder:
  - **Kanto** (19 · K1 to K9, in order) after W1, W6 and W7.
  - **Johto**: 20 · J1 right after W6 (the same importer); J2 on after W1 to W4. J3 and J5 can share a wave; J14 can go any time after J2.
  - **Hoenn** (21) after K1 and K2. H5 (Dive) comes after H7, because the sea floor's way in is through Lilycove.
  - **Unova** (22): U1 after W6, decision 18·5 and the user's dump. Its reader shares nothing with the Game Boy Advance one.
  - **The later regions** (23): Z1 (Kalos) and Z9 (Galar) after U1, side by side. Hisui (Z17–Z20) is optional.
  - A region chapter that adds a Pokétch, a phone, a bicycle or a field power keeps to its region's files where it can. The hot files in the table below are merged by its rules.
- **Ways to play (13).**
  - V1, after 18 · W10, so that choosing the region comes before choosing the run. Then V2, V3.
  - V4, V5; V8, then V9; V10, then V24.
  - V6, then V7; V12, then V13, V20 and V21; V22, then V23.
  - V15, then V16 to V19 one after another (one pair of data files). V26.
  - Optional: V11, V14, V25.
- **Online (07).**
  - O3, in a wave when lane 3 is away from the battle's log.
  - Then O1 (the core library split), alone like T11.
  - Then O4; O5 and O6; O7. O8 is optional.
- **Voice (26).**
  - V2, after the user allows NVorbis; V3, once the user has recorded the samples; then V4, V5, V6.
  - V7, chapter by chapter: first the chapters already written, then each region's.
  - V8, after 17 · N6.
- **Localisation (17)**, if a second language is wanted:
  - N1, in a quiet wave; N2; N3.
  - N4, never in the same wave as 26 · V1, because both number the lines.
  - N5, after the user agrees to the font download.
  - N6.
- **Side activities (15)**: E6 to E8 (the Pokéathlon) only if decision 15·7 keeps it, just before 20 · J13.

## Shared seams and their owners

A seam is a piece several plans offer to write "if it hasn't been". Each has one owner here. Until it lands, the others use a stand-in and say so.

| Seam | Lands it | Wave | Also wanted by |
|---|---|---|---|
| The table of looks, `characters.json` | 11 · C1 | 1 | every session that adds a person |
| `WallClock`, the one reader of real time | 24 · X1 | 2 | 06 · R14, 15 · E3, 13 · V10, 12 · Q3 and Q16, 16 · T19 |
| `RulesDefault` (Off, Rules, On) | 12 · Q10 | 2 | 11 · C12, 12 · Q13, Q14, Q18, Q19, Q21, Q23, Q26 |
| `AbilityShown` and `ItemShown` events | 12 · Q11 | 3 | 25 · Y3, `AiMemory`, 17 · N2 |
| `MoveData.Contest` | 06 · R17a | 5 | 21 · H11 |
| The Galactic mark | 09 · L13 | 7 | 11 · C2 |
| `DecorationScreen` | 06 · R16a | 7 | 21 · H10 |
| `CommandLine` | 16 · T3 | 7 | 12 · Q2, 13 · V24, 16 · T20 |
| `SavePaths` | 12 · Q2 | 8 | 16 · T2, 12 · Q16, Q25, Q28 |
| The order of `PokePose` and `EyesOf` (style guide first) | 09 · L2 | 9 | 25 · Y5, 10 · F2, F6, F10 |
| One region map | 01 · M12 | after 11 | 18 · W3, W9, 20 · J3, 12 · Q22 |
| `Pokemon.Id` and the save's version | 07 · O2 | after 11 | 12 · Q27, Q3, 08 · P3, 24 · X3 |
| `GameDataFiles.Version` | 08 · P10 | after 11 | 13 · V14, V22, 16 · T19, 07 · O4 |
| The naming prompt | 12 · Q19 | after 11 | 13 · V4, a nickname after each catch, the starter's |
| The pad index and `LastDevice` | 12 · Q4 | after 11 | 13 · V12, V20, V21, 12 · Q24 |
| `CaptureScreen` | 12 · Q16 | after 11 | 13 · V8, 16 · T6 |
| `CardPage` | 13 · V1 | later | 18 · W2, 14 · B15 |
| `Models/Legality.cs` | the first of 13 · V13, 14 · B11, 07 · O5, 08 · P21 | later | never two of them in one wave |
| `LevelScaling` | the first of 13 · V12, 14 · B11, 07 · O6 | later | the same |
| A hidden ability on `Pokemon` | the first of 14 · B5, 10 · F12, 22 · U11 | later | the same |
| The Game Boy Advance's behaviour values | 18 · W6 | later | 20 · J1, 22 · U1 |
| A layout swapped behind a fade | 21 · H8 | later | 22 · U4, U8, plan 23 |
| Rock Smash's roll | the first region chapter that opens such a rock, in the chain's order | later | 19, 20 · J7, 21 · H4 |

## Work no plan owned, now placed

| Work | Placed in |
|---|---|
| Rooms drawn with relief, the Pastoria Gym, the Oreburgh Gym to the original's plan | 01 · M9 1b, wave 1 |
| The last 75 species hand-built (Capsakid to Pecharunt) and their 26 forms | 03 · Paldea 3a–3c, beside waves 0–2 |
| The M6–M9 cities and the Gyms timed by `profile` (owed since 04 · G11) | 01 · M12 |
| The move tutors, the Move Relearner, the Move Deleter, the Name Rater | their rooms and people in 01 · M11; the relearning rule in 12 · Q19 |
| Pal Park as a place | 06 · R16b |
| A nickname after a catch, and the starter's | 12 · Q19 |
| A mouth that talks, for 26 · V6 | 11 · C11 (faces on cue) |
| The later mechanics' beats in S8, S9, S12 and S14, if confirmed | R20, R21, R22 and R23, each writing its own |
| The objectives and the skippable scenes of S4 to S14 | 12 · Q14 and Q20, backfilling |
| S5's and S6's bosses' lines in battle | 11 · C12 |
| The Azure Flute and the Hall of Origin | 03 · D12c |
| Tests that count what every new room or area changes | 16 · T16 makes them floors |

## Hot files, and how to merge them

| File | Rule |
|---|---|
| CLAUDE.md, `docs/mechanics/rulings.md`, `docs/scripts.md`, `docs/data-files.md`, the style guide | Each session writes its own paragraph, section or rows. At the merge keep both sides, in merge order. |
| `StoryState.CurrentVersion` and `StoryMigration` | A session's step takes the next number. The second merged in a wave renumbers its step. A step is named for its session. |
| The script language (`Op`, parser, runner, both hosts, `EveryCommand`) | Append at the end; keep both. |
| `world.json` and the generated world files | Merge `world.json` by hand, then regenerate with `tools/MapImporter -- --data`. Never merge the generated files line by line. |
| `species.json`, `moves.json`, `items.json`, `coverage.md`, the coverage floors | Re-run `tools/DataImporter` after the merge; the floor takes the higher number. |
| The golden files (after 24 · X2 and X3) | Regenerate after the merge (`GOLDEN=accept`) and read the difference: what moved must be what the wave meant to move. |
| `RegionDatabase`, `characters.json`, `sound-map.json`, the `Architecture` enum, `WorldModels.cs`, `OptionRow` | Append; keep both. |
| The harness (after 16 · T5) | A file per mode. Its shared part keeps both sides. |
| Overlays | Keep both people's entries. On the same person, the chapter's lines and scripts win, and the people lane's look id is kept. |

## Decisions that gate sessions

Each plan keeps its own list. These are the ones that hold a session in this order. Where a default is given, the session goes ahead on it unless the user says otherwise.

| Decision | Gates | Default |
|---|---|---|
| 11 · 1: the looks in a file or in code | 11 · C1, wave 1 | a file, `characters.json` |
| 06 · 2: the later mechanics in Platinum's story | R20–R23's beats in S8, S9, S12, S14 | left out; each chapter notes where |
| 16 · 4 and 24 · 4: a Windows runner, node in CI | 16 · T4's Windows job; 24 · X5, X6 | Linux only; X5 waits |
| 16 · 1–3, 5: licence, posture, where releases go, the mesh cache | 16 · T1, T2 | wait |
| 03 · 4: the shape of the zone for Generations 5–9 | 03 · D12b, 15 · E5, 14 · B4, 10 · F10 | wait |
| 25 · 4: stop at Platinum's 467 moves | 25 · Y11–Y15 | do them all |
| 18 · 10–11: where a journey begins | 18 · W10, 13 · V1 | wait |
| 18 · 5 and 6, and the pins: where Generation 5 on, and Johto, come from | plans 19–23 | the plans' recommendations: pokefirered, pokeemerald, pokeheartgold, the user's own dump |
| 26: NVorbis, the samples, the engine | 26 · V2, V3, V4 | wait |
| 17 · 1: a second language at all | plan 17 | no |
| 08 · 7 and 9: migration from the player's files, the second slot | 08 · P20–P23 | wait |
| 15 · 7: keep the Pokéathlon | 15 · E6–E8, 20 · J13's dome | drop, as the second check judged |
| 07 · 1–4: who hosts, for whom | plan 07 from O1 | wait |
| Regions before Sinnoh's Hall of Fame | 18 · W1, W6, W10 sooner | after |

## Stale lines found in the plans

Each plan's next session fixes its own.

- **01**
  - "Where we are" says only the south-west is open.
  - M12's Fly and its off-thread chunks were done by S2 and M2.
  - Its swarms and Trophy Garden came with 06 · R13.
  - M11 doesn't list the rooms the chapters and Gyms have built since.
- **02**
  - "Where we are" describes the old battles.
  - The Vs. Seeker (R12) and the Poké Radar (R13) exist.
  - Use the decompilation's scripts, not Bulbapedia, which blocks fetching.
- **03**
  - No checkbox for the last Paldea batch.
  - "No EVs or forms yet".
  - The `Data/pokemon/` folders and the save's species "by number" are both wrong.
  - D12's "trade-evolution replacement" goes against decision 4.
- **05**
  - The architecture section still names MeltySynth and SoundFonts.
  - A5's loop lengths are wrong, and A2 still waits for A4.
  - A6 has no end. Its end is the Hall of Fame and credits (S14) and the post-game's songs (M10).
- **06**
  - "Where we are" stops at R10.
  - R20 counts 92 Mega Stones (the data has 89).
  - R27 says 191 abilities (190).
  - R24–R26's "919 of 919".
  - The optional raids in R22 and R23 are also 23 · Z10's and Z15's.
- **07**: "Where we are" is wholly stale, and O2 and O3 are mostly done.
- **08**
  - P3's fields exist (R12).
  - P7 counts 30 overlays (now 40).
  - P8 and P12 propose `Map.TileScripts`, which exists (an overlay's `read`).
  - P11's wait for the Vs. Recorder is over (S5).
  - P23 must build on R13's `EncounterSlots`.
- **09**: L8's INFO toggle should give way to P19's FORMS page.
- **10**
  - F3's `Follower.cs` and F9's `Marks` are names that exist already.
  - F5 should spawn through `EncounterSlots.Grass`.
- **11**
  - 25 looks, not 15.
  - The look-around is S6's.
  - `Stance` is a name in use.
- **12**: Q14 says the story's version is 2 (it is 4), and Q14 and Q20 cover only S4.
- **13**: V1's `CardPage` must take in R12's front and back.
- **14**: B14's `choosemember` duplicates R12's `choosepokemon`.
- **15**: E4 and E5's contest names will collide with R17's and Hoenn's; prefix them `BugContest`.
- **16**: its counts (harness lines, map files, overlays) are old, and T11 is more than one sitting.
- **17**: its count of scripts is old.
- **18, 19, 20, 21, 22, 23**
  - "`CurrentVersion` is 2".
  - R11–R13 listed as still to come.
  - 20 · J2 and J3 must build on R13's slots and R12's rematches.
  - 23 still calls the species from Inkay on generated.
- **24**: "Where we are" is old, and X1 misses the Hall of Fame's `DateTime.Now`.
- **26**: its counts of script files and Barry's lines are old.
- **Shot names claimed twice**:
  - 13's `28b`–`28d` and 08's `20d`/`20e` against shots that exist;
  - 08 · P13's and 10 · F11's `26n`;
  - 16 · T1's `29d` and 12 · Q15's `29c`;
  - 15 · E3's `j01`–`j05` against the `jubilife` mode;
  - plan 23's `wa`, `wg` and `wh` against the `world` mode.

  Each session checks its mode's existing names before choosing.

## Starting a wave

```text
Do wave 2 of docs/plans/00-overview.md.
```

The main session starts one agent per cell of the wave, each in its own worktree, and merges as "How a wave runs" says. A single session of the wave can also be asked for alone ("Do 02 · S7 from wave 2 of the overview"); it then merges by itself.

## Status

- [ ] Wave 0 · 16 · T5 (beside: 03 · Paldea 3a, done 2026-10-10)
- [ ] Wave 1 · 08 · P7, 01 · M9 1b, 06 · R14a, 11 · C1, 10 · F1, 16 · T16 (beside: Paldea 3b, done 2026-10-10)
- [ ] Wave 2 · 02 · S7 with 08 · P12 (done 2026-10-10), 01 · M9 2a (done 2026-10-10), 06 · R14b (done 2026-10-10), 12 · Q10 (done 2026-10-10), 11 · C5 (done 2026-10-10), 24 · X1 (done 2026-10-10) (beside: Paldea 3c, done 2026-10-10)
- [ ] Wave 3 · 02 · S8 (done 2026-10-10: Veilstone and Pastoria to the Secret Potion; it took the Secret Potion from S9, as the original orders it), 01 · M9 2b (done 2026-10-10), 06 · R14c (done 2026-10-10), 12 · Q11, 09 · L11, 24 · X2
- [ ] Wave 4 · 02 · S9, 01 · M9 2c, 06 · R15, 11 · C12, 11 · C6, 24 · X3
- [ ] Wave 5 · 02 · S10, 01 · M9 2d, 06 · R17a, 25 · Y1, 11 · C11, 12 · Q1
- [ ] Wave 6 · 02 · S11a, 01 · M10a, 06 · R17b, 08 · P5, 09 · L4, 16 · T8
- [ ] Wave 7 · 02 · S11b, 01 · M10b, 06 · R16a, 25 · Y2, 09 · L13, 16 · T3
- [ ] Wave 8 · 02 · S12, 01 · M11a, 14 · B1, 25 · Y3, 09 · L12, 12 · Q2
- [ ] Wave 9 · 02 · S13, 01 · M11b, 06 · R16b, 09 · L2, 09 · L15, 12 · Q20
- [ ] Wave 10 · 02 · S14, 01 · M11c, 14 · B14, 08 · P1, 09 · L14, 08 · P13 (beside: 08 · P14)
- [ ] Wave 11 · the freeze: 16 · T11, then T12
- [ ] The post-game: 02 · S15a–c, 01 · M11d and M12, 03 · D12a–c and D13
- [ ] The other regions' groundwork: 18 · W1–W10
