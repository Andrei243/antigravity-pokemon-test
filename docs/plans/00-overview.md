# 00 · Overview: what is left, in lanes that run side by side

Written on 2026-10-07, after every one of the 26 plans was read in full and checked against the code. The plans keep what each session delivers, needs and leaves for the user to decide; this file says **when** each session runs and **what runs beside it**, so that several sessions (one agent each, in a worktree of its own) can be done at once and merged together. It replaces the README's "Order", which was written before any session.

**On 2026-10-12 the user answered the decisions that gate sessions** ("Decisions that gate sessions", below, has every answer and what is still open). They changed the lanes after wave 11, the regions' section and the work no plan owned: every game begins in Sinnoh, the later regions without a decompilation are drawn by hand, Generations 5 to 9 live in their own regions and not in a zone of Sinnoh's, the four later mechanics are handed over in their own regions, Romanian is the first translation, and migration from the player's files is dropped. The plans' own decision lists still carry what they recommended before: "Stale lines found in the plans" names each, and a plan's next session rewrites its own.

The idea in one paragraph: the work is split into six **lanes** whose sessions touch different parts of the code, plus work that can go **beside** any of them. A **wave** is one round of sessions, at most one from each lane, started from the same commit and merged together at its end. Lanes 1 (the story) and 2 (the map and its rooms) are the spine: lane 2 stays a wave ahead of the chapter that needs its Gym or room. The other lanes have only soft gates: when one is behind, the chapter leaves the beat out and writes where it goes, as plan 02 already does.

## Where we are

About 350 sittings are left, a dozen or so of them marked optional (Hisui and the Pokéathlon no longer are, since 2026-10-12), and plan 27's seven come after them all.

| Plan | Done | Left (sittings) | Notes |
|---|---|---|---|
| 01 · Sinnoh map | M1–M12 | ~1 | done 2026-10-11 but for two hand-made rooms still to rebuild to the original's plans (the player's house and the professor's lab) and M12's timings, owed on a quiet machine |
| 02 · Story | S1, S2, S4–S15 | 0 | done 2026-10-11: played from the bedroom to the post-game; the plan ends with what the story still owes other plans |
| 03 · National Pokédex | D1, D5–D11, every model batch to Pecharunt (all 1,025 hand-built; Paldea 3a–3c) | 3 | D12 (two parts: its zone for Generations 5–9 was dropped on 2026-10-12), D13 |
| 04 · Graphics | everything | 0 | what G11 owes (the M6–M9 cities in `profile`) goes to 01 · M12 |
| 05 · Sound and music | A1–A5, A7 | ~3 | A6 rolls on inside the sessions that open places and scenes |
| 06 · Game mechanics | R1–R18 | ~14 | every one of Platinum's mechanics is in (2026-10-11); R19 to R30 are the later generations', and two sessions of our own were asked for on 2026-10-12 (Megas of our own, Mega Dynamax) |
| 07 · Online | — | 8 | most of O2 and O3 came with plan 06; O8 optional |
| 08 · Platinum's details | P6, P7, P12 | 16 | P3's met data and markings came with R12; P20–P23 (migration, the second slot) were dropped on 2026-10-12 |
| 09 · Looks | L11 | 14 | |
| 10 · Companions | F1 | 11 | F8 optional |
| 11 · People | C1, C5, C6, C11, C12 (C9, C10 in part) | 8 | the looks are a table, `characters.json` |
| 12 · Quality of life | Q1, Q10, Q11 | 27 | |
| 13 · Ways to play | — | 26 | V11, V14, V25 optional |
| 14 · Battle formats | — | 15 | B2, B13 optional |
| 15 · Side activities | — | 8 | E6–E8 (the Pokéathlon) kept on 2026-10-12, just before 20 · J13 |
| 16 · Tooling and shipping | T5, T16 (and a CI build-and-test workflow) | 22 | T9, the editor, is now what Unova and the later regions are drawn with |
| 17 · Localisation | — | 6 | wanted since 2026-10-12: Romanian first, changed in the options at any time |
| 18 · Other regions | — | 10 | |
| 19 · Kanto | — | 9 | |
| 20 · Johto | — | 15 | J15 optional |
| 21 · Hoenn | — | 12 | |
| 22 · Unova | — | 12 | |
| 23 · Later regions | — | 24 | drawn by hand; Hisui (Z17–Z20) and Lumiose's era (Z21–Z24) are wanted since 2026-10-12, through a rift after the last region |
| 24 · Guards | X1, X2 | 4 | X3 is overdue: story versions 3 and 4 shipped with no saves kept to test against |
| 25 · Battle presentation | Y1 | 14 | |
| 26 · Voice | — (only the plan is written) | 8 | NVorbis allowed on 2026-10-12 |
| 27 · Moving to Unity | — (only the plan is written, 2026-10-08) | 7 | deferred by the user to the end of the project, after every other plan; its numbers are measured again then |

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
| 03 · D12 | **a** the test that every species of Diamond, Pearl and Platinum can be had in Sinnoh with no link (the two other versions' exclusives, the trade evolutions, what the second slot gave), the post-game's tables, the other two starters after the Hall of Fame, gifts and the forms' sources; **c** the legendaries' quests. Part **b**, the zone for Generations 5–9, was dropped on 2026-10-12: those species live in their own regions (decision 03 · 4). |
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
| 11 | **16 · T11, then 16 · T12, then 17 · N1 and N2**: a freeze, nothing else runs | | | | | | 08 · P15; 05 · A6 |

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
    *Done 2026-10-10.* X2's golden files don't exist yet (X2 is still open in wave 3), so there was nothing to regenerate: every Pokémon now takes two draws for its personality before its IVs, so every seeded draw after a Pokémon is made has moved (wild Pokémon's and gifts' natures, genders, abilities and stats, and with them the harness's battles and every later shot of `all`). X2 records the draws as R15 left them; a shot from before R15 is compared with one after by `baseline.sh`, mode by mode.
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
- **Wave 11 · the freeze.** With Sinnoh playable to the Hall of Fame, 16 · T11 takes the game's states out of `GameEngine` into a `GameFlow` without renderers, and T12 makes a windowless game of it. Both touch nearly every part of the engine. After them, a chapter can be played through by a test, and the playthrough test (T13) and recorded play (T19) become possible. A second language is wanted (Romanian first, decided 2026-10-12), so 17 · N1 and N2 go into the same freeze, after T12, because N2 is cheapest before lane 3 adds the later generations' lines.

## After the Hall of Fame: the lanes go on

From wave 12, each lane takes its next session whose gates are met. The waves are no longer written out, but the rules above still hold. The list for each lane is in its order; "after X" is a hard gate.

**Lane 1 · Story**
- 02 · S15a: the National Pokédex, the Poké Radar given, the ship, and the Fight, Survival and Resort Areas with Routes 224–230.
- S15b: Stark Mountain, Buck and Heatran, Snowpoint Temple and the Regis, the roaming legendaries, Cresselia.
- S15c: Rotom and the Old Chateau, the Gym Leaders' rematches, the Celestic ruins scene plan 23 asks for.
- Then the Pokédex's completion, by decision 03 · 4 (2026-10-12): a region holds every species of its own games, so that the whole Pokédex can be completed with no link.
  - 03 · D12a: the test that every species of Diamond, Pearl and Platinum can be had in Sinnoh, on the map or by a quest. It places the two other versions' exclusives, gives the trade evolutions a way that needs nobody else, gives the other two starters after the Hall of Fame, and brings the post-game's tables, the gifts and the forms' sources. It also proposes to the user where the species the second slot gave go, and what Pal Park's Catching Show draws from now that nothing is migrated.
  - D12c (the legendaries' quests, with the Azure Flute and the Hall of Origin), then D13.
  - D12b (a zone for Generations 5–9) is dropped. The same test is run for each later region by the last session of its plan.
- 15 · E5 no longer follows here: with no zone, the Bug-Catching Contest's park is Johto's own, and E5 runs just before 20 · J7.
- Then the lane becomes a region lane (below).

**Lane 2 · Map and rooms**
- 01 · M11d: the gate houses made rooms. `WorldWalk` stops passing through them. It also brings the Pokémon Centers' upper floors, the Global Terminal, the Department Store and the move tutors' rooms.
- 01 · M12: the Town Map, built as the one region map that 18 · W3 and W9, 20 · J3 and 12 · Q22 reuse. Also the performance pass (the M6–M9 cities and Gyms added to `profile`, `GetNpcAt`/`GetWarpAt`/`IsWalkable` indexed by chunk) and the held-back items.
- Then plan 18's readers: W6, then W7. The sources are agreed (2026-10-12): pinned commits of pret/pokefirered and pret/pokeemerald, and pret/pokeheartgold for Johto. Plan 01 is done, so the lane is free for them now.

**Lane 3 · Rules**
- 06 · R19 (the modern groundwork), first. On 2026-10-12 the user asked for the later generations' rules, the Fairy type among them, to be playable from Sinnoh on. They are the modern preset's, chosen at NEW GAME; Platinum's preset keeps giving what it gave (whether a game under Platinum's rules should have them too is asked back, in the decisions' table).
- R24, R25 and R26, each with its effects in a partial file of its own. The data importer and the coverage are re-run after the merge, so two of them can share a wave. Then R27, R28a, R28b, R29. These go before R20 to R23 since 2026-10-12: they are what a game under the modern rules can already use in Sinnoh, and nothing in play can use the four mechanics until their regions stand.
- R20 to R23, one after another: R20 sets the pattern the other three follow. The four are handed over in their own regions (06 · decision 2 as answered on 2026-10-12: Mega Evolution in Kalos, Z-Moves in Alola, Dynamax in Galar, Terastallization in Paldea). So no beat is written into S8, S9, S12 or S14, and Sinnoh's story owes no balance check. Each session builds its rules and the AI's use of them; the region's chapter in plan 23 hands the item over.
- Then the two sessions of our own ("Work no plan owned", below): Megas of our own after R20, and Mega Dynamax after R20 and R22, each once the user has approved its list of species.
- 14 · B3, B4, B5, B6, B7, each one in a wave with no other core session. B4's horde slots lose the zone they were kept to (plan 03 · D12b is dropped): they are Kalos's own tables'.
- B8 to B12 (R18 is done); B15, after B14.
- 24 · X5, then X6 after R19. Node is allowed in CI (decided 2026-10-12): the oracle's answers are committed and a workflow regenerates them.
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
- 10 · F5, F6, F7, F9 (its marks under a name that isn't R12's `Pokemon.Marks`), F11, F12.
- 10 · F10 (Alphas) waits: its decision 5 kept them to the post-game zone, which is dropped. They are Hisui's (plan 23 · Z18), unless the user takes that decision's alternative, a rare slot in every area under the modern rules.
- 09 · L9; 08 · P8, then P9; 10 · F8 (optional).

**Lane 6 · Comfort, tools and guards**
- 12 · Q14, which backfills the objectives of S4 to S14.
- 26 · V1, so the remaining chapters write who speaks.
- 12 · Q18, 07 · O2 (the Pokémon's own id and the save's version, which three other plans would otherwise each write), 24 · X4.
- 12 · Q4, Q5, Q8, Q9, Q6, Q7.
- 08 · P3, P4, P18, P19; 09 · L5; 15 · E1, E2.
- 12 · Q13, Q15, Q16, Q19, Q21, Q22, Q23, Q24, Q25 (after 08 · P10), Q26 to Q30.
- 16 · T6, T7, T13 (after T7 and T12), T10, T17 to T22, T4 (Linux only: decision 16 · 4), T1 and T2 (decisions 16 · 1–3 and 5 are taken; the first public release is published only on the user's word), T23, T24, T14, T15 (optional).
- 16 · T9, the editor. M11d went without it. Since 2026-10-12 it is a hard gate of plans 22 and 23, whose regions are drawn with it (decision 18 · 5): it grows from rooms and overlays to outdoor areas (chunks, heights, behaviours, the models that stand on them) and lands before 22 · U1.
- 17 · N3 and N4, then N5's Latin half, then N6 in Romanian (the section below says what each needs).
- 15 · E3, E4.

**Beside**: 08 · P15, P16 and P17 (after P19), 09 · L6, L7 and L8 (after L1), and 05 · A6 as scenes ask for songs.

## Then: the other regions and the rest

These are the plans the README put after Sinnoh's Hall of Fame, which has been in the game since 02 · S14: the groundwork can start whenever lanes 2 and 6 take it. What the user decided on 2026-10-12 (the decisions' table has each answer):

- **Every game begins in Sinnoh.** NEW GAME asks nothing about regions, and the stand-in Kanto is no longer where a game starts.
- **One way on, and Fly back.** A region's harbour opens with its Hall of Fame and sails one way, to the next region. From then on Fly leads back to any region already entered. No harbour sails back.
- **A rift after the last region** leads to Hisui and to Legends: Z-A's Lumiose. Both are built, and neither hangs off Sinnoh or Kalos.
- **The team comes along.** The user's worry is the levels: the later regions have to be levelled for a team that has already won a League, as Gold and Silver levelled Kanto after Red and Blue, and the cap of 100 may have to rise a long way. 18 · W4 studies that first and brings it to the user before anything is built.
- **A decompilation wherever one exists** (Kanto, Hoenn, Johto). Unova and everything after are drawn by hand with 16 · T9's editor and polished afterwards. No dump is read.
- **Each later mechanic is handed over in its own region**: Mega Evolution in Kalos, Z-Moves in Alola, Dynamax in Galar, Terastallization in Paldea.
- **Still open: the order after Sinnoh.** Until the user says otherwise the plans go by generation order with Sinnoh pulled to the front (Kanto, Johto, Hoenn, Unova, Kalos, Alola, Galar, Paldea, then the rift). That builds the three regions with a decompilation first, and brings the four mechanics in the order the games did.

- **The groundwork (plan 18).**
  - W1, then W10, which is small now: every NEW GAME begins in Sinnoh, the chain is ordered with Sinnoh first, and no question is asked. It rewrites the README's "Regions" and plan 13's decision 4. Then W3.
  - Then W2, in a wave where no other session gives a badge: it removes the `Badge` enum and raises the story version.
  - Then W4, which starts with the level curve for a carried team (above); W5, the one-way crossing; W9, Fly between regions, which is the only way back. W8, the harbours that sail back, is left out.
  - W6, then W7, in parallel with W1 to W5 (they share only `Sources.cs`).
- **Up to three regions at once**, each in its own `Data/world/<region>` and music folder:
  - **Kanto** (19 · K1 to K9, in order) after W1, W6 and W7. Plan 19's rule that no Kanto chapter comes before the Sinnoh chapter of the same number is met: Sinnoh's story is whole.
  - **Johto**: 20 · J1 right after W6 (the same importer); J2 on after W1 to W4. J3 and J5 can share a wave; J14 can go any time after J2.
  - **Hoenn** (21) after K1 and K2. H5 (Dive) comes after H7, because the sea floor's way in is through Lilycove.
  - **Unova** (22): U1 after 16 · T9. The region is drawn by hand, so U1 and U2 are no longer readers of a dump: they lay out its shape, towns and routes, then its events, encounters and trainers from public numbers, and the user polishes afterwards.
  - **The later regions** (23): Z1 (Kalos) and Z9 (Galar) once U1 has shown how a drawn region goes, side by side, drawn the same way. Hisui (Z17–Z20) and Lumiose's era (Z21–Z24) come after Paldea, behind the rift.
  - A region chapter that adds a Pokétch, a phone, a bicycle or a field power keeps to its region's files where it can. The hot files in the table below are merged by its rules.
- **Ways to play (13).**
  - V1, after 18 · W10. No region is chosen any more, so V1 needs only the title's phases as W10 leaves them. Then V2, V3 (its cap and scaling are what W4's level curve is built on).
  - V4, V5; V8, then V9; V10, then V24.
  - V6, then V7; V12, then V13, V20 and V21; V22, then V23.
  - V15, then V16 to V19 one after another (one pair of data files). V26.
  - Optional: V11, V14, V25.
- **Online (07).**
  - O3, in a wave when lane 3 is away from the battle's log.
  - Then O1 (the core library split), alone like T11.
  - Then O4; O5 and O6; O7. O8 is optional.
- **Voice (26).**
  - V2 (NVorbis is allowed, 2026-10-12).
  - V3 writes the sample script (`docs/voice/samples.md`), the recording guide and the booth first. Then the user records, about twenty passages, and V3's trial chooses the cloning engine by ear. Then V4, V5, V6.
  - V7, chapter by chapter: first the chapters already written, then each region's.
  - V8, after 17 · N6. With Romanian the first language, whether the voices speak it too is asked before V3's trial, because it decides which engines are tried and which language the samples are in.
- **Localisation (17)**, wanted since 2026-10-12: Romanian first, and the language an options row the player changes at any time (so nothing of it is kept in the save, plan 17's decision 5).
  - N1 and N2 in wave 11's freeze; then N3. PokeAPI has no Romanian and the games were never sold in it, so species, moves, abilities and items keep their English names; whether their descriptions are written in Romanian is N6's question for the user.
  - N4, never in the same wave as 26 · V1, because both number the lines.
  - N5's Latin half before N6: Romanian's ă, ș and ț are outside the Latin-1 that `UiFonts` loads. Nunito has them (its `cmap` was read on 2026-10-12), so no face is downloaded. Its CJK half waits for a language that needs it.
  - N6, in Romanian.
- **Side activities (15)**: E6 to E8 (the Pokéathlon) are kept (decided 2026-10-12) and run just before 20 · J13, which stands the dome in Johto; E5 runs just before 20 · J7.
- **Unity (27)**: deferred by the user to the end, after everything above. Its spike (S0) is where its own six decisions begin.

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
| `Models/Legality.cs` | the first of 13 · V13, 14 · B11, 07 · O5 (08 · P21 is dropped) | later | never two of them in one wave |
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
| The later mechanics' beats in S8, S9, S12 and S14 | not written: each mechanic is handed over in its own region (2026-10-12), by that region's chapter in plan 23 |
| Megas of our own: Mega forms the games never had (asked for on 2026-10-12) | a new session of plan 06 after R20: their data through `tools/DataImporter/Overrides`, behind the `Ruleset`; their sculpts as forms, in plan 03's way; their looks with 25 · Y14. The list of species is the user's to approve first |
| Mega Dynamax, a mechanic of our own that joins Mega Evolution and Dynamax for some species (asked for on 2026-10-12) | a new session of plan 06 after R20 and R22. It changes 06 · decision 3 for those species ("a Mega Pokémon can't Dynamax"). Which species, what it does and where it is handed over are the user's to approve first |
| Every species of Diamond, Pearl and Platinum to be had in Sinnoh with no link, and the other two starters after the Hall of Fame | 03 · D12a |
| Every later region holding every species of its own games | the last session of each region's plan, by D12a's test |
| A level curve for a team carried from region to region, and a cap that may rise above 100 | 18 · W4 with 13 · V3, studied from how Gold and Silver levelled Kanto, and brought to the user first |
| Pal Park's Catching Show, with nothing migrated to stock it | 03 · D12a proposes what it draws from |
| Outdoor areas drawn by hand, for Unova and after | 16 · T9, grown from rooms and overlays to areas |
| Romanian's letters beyond Latin-1 | 17 · N5's Latin half, before N6 |
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

Each plan keeps its own list. These are the ones that hold a session in this order.

### Answered

The user answered these on 2026-10-12, but for the first. Where an answer goes against what a plan recommended, the answer holds, and "Stale lines found in the plans" names the lines to rewrite.

| Decision | Answer | What it changes |
|---|---|---|
| 11 · 1: the looks in a file or in code | a file, `characters.json` (taken with 11 · C1) | nothing left |
| 18 · 5 and 6, and the pins: where the other regions' layouts come from | a decompilation wherever one exists: pinned commits of pret/pokefirered (Kanto), pret/pokeemerald (Hoenn) and pret/pokeheartgold (Johto). Where none exists, Generation 5 on, the region is drawn by hand and polished afterwards | 18 · W6 and W7 can start. No dump is read, so 22 · U1 and U2 and plan 23's readers become drawing, and 16 · T9's editor is their hard gate |
| 18 · 10: where a new game begins | always in Sinnoh; NEW GAME asks nothing about regions | 18 · W10 shrinks; the README's "Regions" and 13 · decision 4 are rewritten by it; 13 · V1 chooses no region |
| 18 · 9 and 11: the ways between regions | a one-way harbour on to the next region, then Fly back to any region entered | 18 · W8 is left out; W9 is the way back |
| 23 · 3, 9 and 10: Hisui and Legends: Z-A's Lumiose | both wanted, through a rift that opens after the last region | Z17–Z24 are no longer optional and come after Paldea; neither hangs off Celestic's ruins or Kalos's Hall of Fame |
| 18 · 2: what crosses with the player | the team comes along (the user's worry is the levels that follow from it) | 18 · W4 studies the level curve first; see "Still open" |
| 03 · 4: where the species from outside Sinnoh live | each region holds every species of its own games; in Sinnoh, every species of Diamond, Pearl and Platinum, on the map or by a quest, with the other two starters after the Hall of Fame; the whole Pokédex can be completed with no link | 03 · D12b's zone is dropped; D12a grows; 15 · E5, 14 · B4 and 10 · F10 lose the zone (each is placed above) |
| 17 · 1: a second language | yes: Romanian first, changed in the options at any time | 17 · N1 and N2 join wave 11's freeze; N5's Latin half goes before N6 |
| 06 · 2: the later mechanics in Platinum's story | not in Sinnoh's story: each is handed over in its own region. This replaces the ruling of 2026-10-04 ("in the story if it can be balanced") | R20–R23 write no beats into S8, S9, S12 or S14; `docs/mechanics/rulings.md`'s "The later mechanics in the story" is rewritten by R20 |
| 16 · 4 and 24 · 4: a Windows runner, node in CI | whatever is easier: CI stays on Linux, and node runs in a CI workflow with the oracle's answers committed, so nothing is installed on the user's machine | 16 · T4 has no Windows job; 24 · X5 can run, X6 after R19 |
| 16 · 1–3, 5: licence, posture, where releases go, the mesh cache | the plan's recommendations, taken for the least legal exposure: MIT for the code; the fan-game notice on the title and in the README with a contact for takedown, no donations, no store; GitHub Releases; the mesh cache shipped | 16 · T1 and T2 can run. T2 builds the release and stops there: the first public one is published on the user's word, since a public build is what draws a takedown |
| 25 · 4: how far the move effects go | every move | 25 · Y11–Y15 stay |
| 26: NVorbis | allowed | 26 · V2 can run. The samples and the engine are V3's: it writes the script the user reads, and its trial chooses the engine by ear |
| 08 · 7 and 9: migration from the player's files, the second slot | neither | 08 · P20–P23 are dropped; Pal Park's show and the second slot's species go to 03 · D12a |
| 15 · 7: keep the Pokéathlon | keep it | 15 · E6–E8 run just before 20 · J13 |

### Still open

Where a default is given, the session goes ahead on it unless the user says otherwise.

| Decision | Gates | Default |
|---|---|---|
| The order of the regions after Sinnoh, and so which region is last | plans 18 to 23's order, 18 · W10's chain, where the rift opens | generation order with Sinnoh first: Kanto, Johto, Hoenn, Unova, Kalos, Alola, Galar, Paldea, then the rift |
| The later generations' rules from Sinnoh on: under the modern rules only, or in a game under Platinum's rules too | 06 · R19 | the modern rules only, as the user ruled on 2026-10-04; Platinum's preset doesn't move |
| The level curve for a carried team, and whether the cap rises above 100 | 18 · W4, 13 · V3, every region's levels | wait: W4 brings the study |
| Which species get Megas of our own, and which Mega Dynamax, what it does and where it is handed over | the two new sessions of plan 06 | wait: a list is proposed to the user |
| Whether the voices speak Romanian too | 26 · V3's trial and samples, V8 | English voices under Romanian text, until the user says |
| Where the second slot's species go in Sinnoh, and what Pal Park's show draws from | 03 · D12a | D12a proposes both |
| 07 · 1–4: who hosts, for whom | plan 07 from O1 | wait |
| 27 · 1–6: the move to Unity | plan 27 | deferred to the end by the user (2026-10-08) |

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

### Made stale by the answers of 2026-10-12

The plans were not edited when the user answered; this file holds the answers. Each line below is rewritten by its plan's next session, to the answer in "Decisions that gate sessions".

- **README**
  - "Regions" says a new game starts in Kanto and the chain runs in generation order with nothing leading back (18 · W10 rewrites it).
  - It counts twenty-six plans; plan 27 is the twenty-seventh.
- **03**: decision 4's Pal Park-style area and zone for Generations 5–9, and D12's line with "the new zone for later generations".
- **06**: decision 2 and R20–R23's "check" of Sinnoh's bosses; decision 3, once Mega Dynamax is agreed; `docs/mechanics/rulings.md`, "The later mechanics in the story".
- **08**: P20–P23 and decisions 7, 8 and 9; and plan 06 · R16b's "until plan 08's migration" for Pal Park's show.
- **10**: F10 and decision 5 ("the post-game zone's tables only").
- **13**: decision 4 (the chain's default, Kanto onward) and V1's place after a choice of region; V11's `StartRegion` and `OneRegion`, which W10 no longer writes.
- **14**: B4's horde slot "in the new zone's tables" and decision 1's zone.
- **15**: decision 3 (the venues in plan 03 · D12's zone), E5's park, and decision 7, which is answered: the Pokéathlon stays.
- **17**: N6's "N5 for a CJK language" (Romanian needs N5's Latin half), and decisions 1 and 5, which are answered.
- **18**: decisions 2, 5, 6, 9, 10 and 11; W8 (left out); W10's questions after NEW GAME, its `Offered`, `ChooseRegion`, `ChooseOneRegion`, `RegionStart` and the chain going round; "Generation 5 and later", which recommends the dump.
- **19**: the rule that no Kanto chapter comes before the Sinnoh chapter of the same number is met and can go.
- **22**: decision 2, U1 and U2 as readers of a dump, decision 4's "the dump's version only" and its "D12's zone", decision 6's "since the dump has them whole".
- **23**: decision 1 (sampling a dump), decisions 3, 9 and 10 (Hisui off Celestic's ruins and optional, the Lumiose era after Kalos's Hall of Fame), decision 11's alternative, the "optional" on Z17–Z20, and where each of the four mechanics is handed over, which its chapters now own.
- **24**: decision 4, which is answered.
- **25**: decision 4, which is answered.
- **26**: V2's wait for NVorbis; V8, which Romanian brings forward as a question.

## Starting a wave

```text
Do wave 2 of docs/plans/00-overview.md.
```

The main session starts one agent per cell of the wave, each in its own worktree, and merges as "How a wave runs" says. A single session of the wave can also be asked for alone ("Do 02 · S7 from wave 2 of the overview"); it then merges by itself.

## Status

- [ ] Wave 0 · 16 · T5 (beside: 03 · Paldea 3a, done 2026-10-10)
- [ ] Wave 1 · 08 · P7, 01 · M9 1b, 06 · R14a, 11 · C1, 10 · F1, 16 · T16 (beside: Paldea 3b, done 2026-10-10)
- [ ] Wave 2 · 02 · S7 with 08 · P12 (done 2026-10-10), 01 · M9 2a (done 2026-10-10), 06 · R14b (done 2026-10-10), 12 · Q10 (done 2026-10-10), 11 · C5 (done 2026-10-10), 24 · X1 (done 2026-10-10) (beside: Paldea 3c, done 2026-10-10)
- [x] Wave 3 · 02 · S8 (done 2026-10-10: Veilstone and Pastoria to the Secret Potion; it took the Secret Potion from S9, as the original orders it), 01 · M9 2b (done 2026-10-10), 06 · R14c (done 2026-10-10), 12 · Q11 (done 2026-10-12: the abilities and items a battle names are log events, `AiMemory` reads them, and the information panel opens from the commands), 09 · L11 (done 2026-10-12: six scene effects and the `effect` command, played in the scenes of Spear Pillar, the Distortion World, the headquarters' control room and Canalave's library), 24 · X2 (done 2026-10-12: 200 golden battles and every trainer's team, generated on the merged wave's tree, after Q11's events)
- [x] Wave 4 · 02 · S9 (done 2026-10-10: Route 210's Psyduck and Cynthia's Old Charm to the rival on Canalave City's bridge; Celestic's ruins and the Fuego Ironworks' floor opened as maps of the imported world, two entries of `world.json` to merge by hand), 01 · M9 2c (done 2026-10-10), 06 · R15 (done 2026-10-10), 11 · C12 (done 2026-10-12: `TrainerReacts` at three moments, `trainer-lines.json` with the rival's, the Leaders', Team Galactic's bosses', the Elite Four's and Cynthia's lines, and the "Trainers speak" option, on under the modern rules only), 11 · C6 (done 2026-10-12: twenty looks of the second half's named cast with the Frontier Brains, four hair cuts, a fedora, a beard and glasses in the kit; the fallbacks stay until C4), 24 · X3 (done 2026-10-12: `scripts.txt` and `saves.txt` golden files, a save of every world and story version, the harness's `saves` mode)
- [x] Wave 5 · 02 · S10 (done 2026-10-10: Riley's HM04 on Iron Island to Maylene and HM08 on Route 217; the drained Lake Valor, Valor Cavern and the lake Team Galactic holds opened as maps of the imported world, three entries of `world.json`; story version 12), 01 · M9 2d (done 2026-10-10: the League's rooms and lifts to the Hall of Fame, whose scene, credits and the rival's battle before the door are S14's), 06 · R17a (done 2026-10-10: the Super Contests and `MoveData.Contest`), 25 · Y1 (done 2026-10-12: `WeatherContinues` on the turn's line, the five weathers drawn on the stage with their light and sounds, the Substitute's doll with its swaps, flinch and fade; a doll passed by Baton Pass is told on the send-out's line; the rain battle's timing is owed on the Windows machine), 11 · C11 (done 2026-10-12: `act` and `mood`, `onspot` and `back` in a walk, the Bow, Shrug, Point and Laugh emotes with their strips), 12 · Q1 (done 2026-10-12: a save written through a temp file over a backup with a checksum, `SaveLoad` and the title's notice, the close button's question)
- [ ] Wave 6 · 02 · S11a (done 2026-10-10: the rival at Acuity Lakefront's rock face, Snowpoint City and Maylene, the scene at Lake Acuity, where Platinum has no battle with Jupiter; the lake Team Galactic leaves opened as a map of the imported world, one entry of `world.json`; story version 13), 01 · M10a (done 2026-10-10: the Fight, Survival and Resort Areas, Routes 224 to 230 and Victory Road's back rooms open, five of the fourteen stand-ins built; the ship from Snowpoint City is S15a's, and until it sails nobody comes there), 06 · R17b (done 2026-10-10: Ball Capsules, Seals and the ribbons, which finishes R17; the Ribbon Syndicate's two scripts wait for its room in the Resort Area, 01 · M10b (done 2026-10-11: Stark Mountain, Snowpoint Temple's floors, Fullmoon and Newmoon Islands, the Seabreak Path behind a chunk swap, Flower Paradise, the Battle Frontier's grounds, the Ribbon Syndicate's room; no stand-in model is left; M10 is done) or M11), 08 · P5, 09 · L4, 16 · T8
- [ ] Wave 7 · 02 · S11b (done 2026-10-11: the Storage Key, the warehouse's door, the headquarters' nine rooms with their warp panels, Cyrus's speech and battle, the Master Ball, Saturn and the three set free; story version 14), 01 · M10b, 06 · R16a (done 2026-10-11: the Underground under the whole region, mining, Spheres, traps, the hikers, the secret base and `DecorationScreen`, the Underground Man's missions; other players stood in for by the day's diggers), 25 · Y2, 09 · L13, 16 · T3
- [ ] Wave 8 · 02 · S12 (done 2026-10-11: the climb through Mt. Coronet, Spear Pillar and the tag battle beside the rival, Cyrus taken by Giratina's shadow, the Distortion World's floors and boulders, Cyrus's battle, Giratina in its Origin Forme, Sendoff Spring; the torn Spear Pillar opened as a map of the imported world; story version 15), 01 · M11a (done 2026-10-11: 47 rooms of the south-west and the centre on the original's plans, the Old Chateau's ten among them, the flower shop, the Berry Master, Bebe; the hand-made rooms of the first chapters are left as they are), 14 · B1, 25 · Y3, 09 · L12, 12 · Q2
- [ ] Wave 9 · 02 · S13 (done 2026-10-11: the professor's lab after the Distortion World, Azelf and Uxie in their caverns and Mesprit roaming, Flint, Volkner at the Vista Lighthouse, the Beacon Badge's story, Jasmine and HM07; story version 16), 01 · M11b (done 2026-10-11: 27 rooms of the east and the sea: the Pokémon Mansion, the Seven Stars Restaurant, the Hotel Grand Lake, the Café Cabin, Pastoria's observatory gate where the Safari Game now starts, Veilstone's, Pastoria's and Canalave's houses), 06 · R16b (done 2026-10-11: the Mining Museum's fossils, the Game Corner's slot machines and prizes, the Villa and its furniture and visitors, Pal Park's lobby and grounds; nothing is brought over to the Catching Show until plan 08's migration), 09 · L2, 09 · L15, 12 · Q20
- [ ] Wave 10 · 02 · S14 (done 2026-10-11: the rival before the League's door, the Hall of Fame's scene in full, the ceremony and the credits as screens of their own, waking at home; story version 17. Sinnoh's story is played to the Hall of Fame), 01 · M11c (done 2026-10-11: 21 rooms of the north and the Battle Zone: Celestic's shop and houses, the Snowbound Lodge, Routes 217 and 222's houses with the fisherman's size contest, Sunyshore's and the Battle Zone's houses, the Ribbon Syndicate's spa and lift), 14 · B14, 08 · P1, 09 · L14, 08 · P13 (beside: 08 · P14)
- [ ] Wave 11 · the freeze: 16 · T11, then T12, then 17 · N1 and N2 (Romanian is wanted: 2026-10-12)
- [ ] The post-game: 02 · S15a (done 2026-10-11: the professor's rating, Professor Oak and the National Pokédex, the Poké Radar, the ship from Snowpoint, the Fight Area's tag battle and blockades, Route 227, Marley, Oak at Pal Park and in Eterna; story version 18), 02 · S15b (done 2026-10-11: Stark Mountain with Buck, Mars, Jupiter and Charon's arrest, Heatran, the Elite Four's and Cynthia's rematch teams, Candice and Snowpoint Temple with Regigigas, the sailor's son and Cresselia; the Regis' ruins, the Harbor Inn and Darkrai stay shut as events), 02 · S15c (done 2026-10-11: the Battleground and the Gym Leaders' rematches, Rotom in the Old Chateau's television, the Celestic elder's book, Dialga and Palkia at Spear Pillar; S15 and plan 02 are done, with what the story still owes listed at the plan's end), 01 · M11d (done 2026-10-11: 69 rooms: fifteen gate houses in place of every way through, the tutors' and the Move Deleter's houses, Veilstone's Department Store, the Centers' upper and lower floors, the Global Terminal, Pal Park's gallery; M11 is done but for the hand-made rooms of the first chapters), 01 · M12 (done 2026-10-11: the Town Map as the one region map (the bag's, Fly's and the Pokédex's area page), people and warps indexed by tile, forty new profile scenes, the held-back items walked and none yet reachable; plan 01 is done but for the hand-made rooms of the first chapters and the timings, which are owed on a quiet machine), 03 · D12a, D12c and D13 (D12b, the zone, was dropped on 2026-10-12)
- [ ] Platinum's last mechanics, taken out of their waves on 2026-10-11: 06 · R18a (done 2026-10-11: Battle Points, the Frontier's records, rules, trainers and sets, the Brains' frame, and the Battle Tower's seven rooms with Palmer; Wi-Fi rooms left out), 06 · R18b (done 2026-10-11: the Battle Factory: rentals by round, hints, trades, Thorton, its lobby, corridor and battle room; Multi left out), 06 · R18c (done 2026-10-11: the Battle Hall (one Pokémon, seventeen types by ten ranks, Argenta) and the Battle Castle (Castle Points, the valets' counters, Darach), their six rooms; Multi left out), 06 · R18d (done 2026-10-11: the Battle Arcade: the board's sixteen panels and thirty-two events, Dahlia; all five facilities of the Battle Frontier stand, and R18 is done), 06 · R16c (done 2026-10-11: Amity Square's walk: the species that may stroll, the follower a step behind, its finds, the ruins' huts, the man of the park; one follower type for people and Pokémon. R16 is done, and with it every one of Platinum's mechanics, R1 to R18)
- [ ] The other regions' groundwork: 18 · W1–W10
