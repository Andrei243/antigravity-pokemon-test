# The sign-off of plan 06's mechanics

Plan 06 · R30, the audit part (wave 62, 2026-10-26). What the plan asked R30 for is "AI that uses the four mechanics sensibly, random-battle fuzzing, balance, the options for rules and mechanics, a final coverage report with every exception explained". This page is the evidence for what is done, every exception with its reason and owner, and the one decision the user still had to make: what becomes of Platinum's rules as a baseline. Nothing here removes or changes Platinum's rules or `battles-platinum.txt`. The user's answers of 2026-10-31 and what they built finish R30: "The user's decisions of 2026-10-31", below.

## What the audit found

### Coverage (`docs/mechanics/coverage.md`)

| What the plan counts | In the data | Runs in full | Exceptions |
| --- | --- | --- | --- |
| Platinum's moves | 467 | 467 | none |
| Moves of Generations 5–9 | 380 | 380 | none |
| Moves of our own past level 100 (R31) | 72 | 72 | none (their looks are plan 25's) |
| Z-Moves, Max Moves, G-Max Moves | 35 + 19 + 33 | 87 | none |
| Platinum's abilities | 123 | 123 | none |
| Abilities of Generations 5–9 | 190 | 190 | none (the cut-downs below) |
| Platinum's hold effects | 158 | 158 | none |
| Platinum's items | 445 | 379 | 1 partly, 16 not yet, 49 with nothing to run (below) |
| Items of later games | 455 | 451 | 3 not yet, 1 with nothing to run (below) |

Every move of the data, every ability and every hold effect runs. The report's "Move effects still to write" and "Abilities without an effect" sections are empty.

**Fixed in this audit** (the report judged by rules written before what they wait for was built): `tools/DataImporter/Coverage.cs` now counts a berry as working when it is planted (R14a's `BerryPatches`), cooked (R14c) and blended (plan 21 · H11), the four mulches and the Sprayduck as working at the berry soil (`common.BerryPatch`), and the Oval Stone as a stone held to evolve; Platinum's items that work rose 309 → 379 and the floor in `CoverageTests` with it. The "later mechanics' data" section no longer says nothing of it runs, and the evolution table's "waits for" column no longer names what was built (the weather, the evolution sites, Poffins, the game's own trades).

**Platinum's items that don't work, each with its reason:**

- *Partly*: the Safari Ball, which catches only in the Great Marsh and Hoenn's Safari Zone, where it is used (by design: it belongs to a place of its own).
- *Not yet*: the twelve Mail (written, held and kept in the PC: plan 12 · Q19 and plan 11's people), the Point Card and the Pal Pad (the wireless link: plan 07, by decision 06 · 7), the Azure Flute (an event item: what it opens, Hall of Origin, was never given by the game), and the Coin Case, which the Game Corners ask for and which keeps the coins, but whose USE in the bag (Platinum's "Your coins: n") isn't built (a small screen line; nobody owns it yet: plan 12 is the natural home).
- *Nothing to run* (49): items that are only sold, shown or handed over (28 of the Items pocket, such as Nuggets, shards and fossils, which a script or a shop takes, and 21 key items whose work is a script's asking whether the bag holds them).

**Later items that don't work:** Meowsticite, Magearnite and Tatsugirinite (Legends: Z-A's Mega Stones, whose Mega forms the data doesn't have yet: plan 03), and the Pass Orb (nothing to run: a Pokémon Black 2 and White 2 link item).

**Evolutions that can't be met yet:** online trades (plan 07 · O5; the game's own trades and a Linking Cord set every trade evolution off today), the Meltan Candies and Gimmighoul Coins to gather (plan 23's Paldea; Pokémon GO's candies have no home), and a Stone Arch for Runerigus and Runedolmen (plan 23's Galar).

### The damage oracle (`PokemonPlatinumTests/Oracle/known-gen9.json`)

Empty since R27: all 3,000 scenarios agree with Pokémon Showdown's calculator under the modern rules, and `OracleTests` passes with no known difference. Nothing to explain.

### Fuzzing

`BattleFuzzTests` (new, wave 62) plays random battles under the modern rules between random teams of every species the game has, its own 400 included, at levels 5 to 250, holding any holdable item, their own Mega Stone or a Z-Crystal, a quarter with the Gigantamax Factor, in single, double and triple battles, with both trainers carrying the Key Stone, the Z-Ring, the Dynamax Band, the Mega Max Band and the Tera Orb, both sides chosen by the trainer AI. Each battle must end, leave HP, PP and stages in range, end with the side the result names fainted, and play the same twice from its seed; and across forty battles each of the four mechanics must have happened. Forty battles run in every test run (about a second); `BATTLE_FUZZ=n` plays n and `BATTLE_FUZZ_FROM=n` starts elsewhere.

The sign-off played **6,000 battles** (seeds 1 to 6,000, about 50 s for each 3,000) with **no failure**: no exception, no battle unfinished, nothing out of range, every battle the same twice. In them: 425 Mega Evolutions, 241 Z-Moves, 11,688 Dynamaxes (27,356 Max Moves) and 11,308 Terastallizations. No Ultra Burst happened (a Necrozma with its fusion form and Ultranecrozium Z is rare in a random draw; `ZMoveTests` holds it). Beside it the older fuzzes still run in every test run: 150 Platinum battles (`BattleCoreTests`), 240 of moves with effects of their own (`BattleFieldTests`) and the 200 golden battles.

### R19 to R29b: what each left

Every one of R19 to R29b is ticked. What each left is listed under its entry in plan 06 and in `docs/mechanics/rulings.md`; gathered here by owner:

- **Plan 25 (looks)**: the change's look and the button's picture for Mega Evolution, Z-Moves, Dynamax and Terastallization (Y14, Y15); the looks of the moves of our own past level 100.
- **Plan 23 (later regions)**: where the Key Stone, Z-Ring, crystals, Dynamax Band, candy, Gigantamax Factor, Tera Orb and shards are found; Max Raid dens and Tera Raid crystals; Zygarde's cells, Furfrou's groomer; the Stone Arch and the gathered coins.
- **Plan 03**: the three Legends: Z-A Mega forms the data lacks; Ogerpon's "-Tera" forms.
- **Cut-downs of Showdown's rules** (each named in the rulings, none of which a battle of Platinum's rules can reach): Truant counting a Mega Evolution as its action; an -ate ability and a Z-Move's or Max Move's type; Magic Coat and Crafty Shield on a status Z-Move; HP shares read from the Dynamaxed HP; the moves that change a type against a Terastallized Pokémon; Tera Shell over a move of several hits; Illusion and Morpeko at a Terastallization; Round, Instruct, the Pledges' waiting line, the rainbow's chances, Dragon Darts against a protected first foe, Grassy Glide's priority elsewhere, Body Press's Attack bonuses, Revival Blessing's choice, Make It Rain's money, the Torques' flags, Chilly Reception's line order, Salt Cure and Syrup Bomb in the info panel; nine of the later abilities' finer points (rulings, "The later abilities").

None of these breaks a battle; each is a line said a little differently or a rare case decided more simply than Showdown decides it.

## The user's decisions of 2026-10-31, and what was built (R30 finished)

The four questions below were put to the user and answered on 2026-10-31: **1A, 2 yes, 3B, 4A**. With them R30, and plan 06, is done.

1. **The AI's use of the four mechanics: A, the sensible use proposed.** `TrainerAi.WithTheLaterMechanics` now: Mega Evolves (and Ultra Bursts) at once, as before; uses a Z-Move only where it is worth it (`ZMoveWorthIt`: a damaging move at least neutral against its target by the battle's chart, or a status move whose `zBonus` helps now: a stat raised that stands under +2, a heal below half HP, a fallen stat put back, the critical-hit boost, a redirection with others on the field, a fainted teammate restored, Curse's Ghost heal or Attack); Dynamaxes on the trainer's last able Pokémon or on its ace (none of the team of a higher level) on its first turn in (`DynamaxNow`), while a Mega Dynamax with the Mega Max Band keeps R20c's first-turn-it-can rule; Terastallizes where its Tera Type turns a super-effective hit of the foe's last move the AI has seen (`AiMemory.MovesSeen`) into a neutral or resisted one, or on the trainer's last able Pokémon (`TeraNow`; a Stellar Tera Type turns no weakness). Nothing is drawn: `battles-platinum.txt` and `battles-modern.txt` held still without being accepted (no trainer of the corpus carries any of the items). `LaterMechanicsDecisionsTests` holds each decision; `BattleFuzzTests` now asks forty battles for Mega Evolution, Dynamax and Terastallization and a run of 200 for a Z-Move, which is rare there under the new rule (none in the first 80 battles, 5 in the first 200).
2. **An option to turn them off: yes.** `RunRules.NoLaterMechanics`, a row of the run panel at NEW GAME ("No later mechanics", `RunRow.NoLaterMechanics`, with its words in English and Romanian), saved with the run and false in every older save (no version raised), `if rule nolatermechanics` in a script. It reaches a battle as `RunTerms.NoLaterMechanics` (`RunRules.BattleTerms`), which, unlike the run's other terms, holds for **both** sides: `BattleCore.LaterMechanicsBarred` is asked at each mechanic's one place of enforcement (`WhyNotMega`, `WhyNotZMove`, `WhyNotUltraBurst`, `WhyNotDynamax`, `WhyNotMegaDynamax`, `WhyNotTera`) and by Primal Reversion, so the player's buttons are refused and the trainer AI, which only asks those, tries none of them. Since it changes what the AI chooses, a `BattleRecord` keeps it (`NoLaterMechanics`, checked by `Replay`) and so does the Vs. Recorder's file (`BattleRecordFile.NoLaterMechanics`, through `Opening` and `ToSetup`).
3. **Balance: B, a judgement to make, not a number to hold.** Nothing is built to measure it. Balance is looked at by playing and by the fuzz's counts when a question comes up, and a measure is added only if the user asks for one.
4. **Platinum's rules as a baseline: A, kept as the test baseline, never offered.** Nothing to build: `Ruleset.Platinum` and `battles-platinum.txt` stay as they are, as described below.

## What R30 still owed, and was not the audit's (settled on 2026-10-31, above)

1. **The AI's use of the four mechanics.** Today `TrainerAi.WithTheLaterMechanics` uses each whenever it can, as Showdown's random player does: a Mega Evolution at once, a Z-Move on the first move that has one, a Dynamax on the first Pokémon that can (kept for a Mega with the Mega Max Band), a Terastallization on the first that can. Nothing is drawn for it, so no golden file depends on it. In the game only seven trainers carry any of the items (the Champions' Mega Dynamax teams of `our-trainers.json`), so it touches few battles yet; it will touch every later region's important trainers. A sensible use, proposed for the user: a Z-Move only on a damaging move that is at least neutral against the target, or on a status move whose Z-Power helps; Dynamax on the trainer's last Pokémon, or on the ace (the highest level) when it comes in; Terastallize when the Tera Type turns a weakness to the foe's last move into a resistance, or on the ace's last Pokémon; Mega Evolve at once, as now. This needs the user's yes before it is written, since it changes how every later Champion fights.
2. **Options for rules and mechanics.** Every game is played by the modern rules (the user's ruling of 2026-10-12) and the four mechanics are open to whoever holds the item. There is no option to turn a mechanic off. Proposal: a field of `RunRules` ("No later mechanics": Mega, Z, Dynamax and Tera refused for both sides through `RunTerms`, at the one place of enforcement each has, `WhyNotMega`, `WhyNotZMove`, `WhyNotDynamax`, `WhyNotTera`), so it is chosen with a run and saved with it, not an options row that could change in the middle of a game.
3. **Balance.** Nothing measures it yet. The fuzz above shows the battles hold together, not that they are fair. What could be measured cheaply: the win rate of the side that uses a mechanic first, per mechanic, over thousands of fuzz battles, and of the moves of our own past 100 (R31) against Showdown's moves of like power. The user decides whether balance is a number to hold or a judgement to make.

## The decision the user made: Platinum's rules as a baseline (A, 2026-10-31)

**Where it stands.** Since 2026-10-12 every game is played by the modern rules. `Ruleset.Platinum` is no longer offered in the game; it stays as the baseline the modern rules are written as differences from, `Ruleset.Current` is Platinum's until a game begins (so every test that builds a battle without rules is fought by it), and `battles-platinum.txt` (100 battles, 810 kB) holds that nothing changes it by accident. The plan says R30 decides whether it goes.

**What removing it would cost.** About 200 places in 59 test files name `Ruleset.Platinum`, and many more tests use it without naming it (the scenario helpers hand no rules). Their expected numbers are worked out by hand from the decompilation's code: they would all have to be worked out again from Showdown's, a session or more of work with no new behaviour, and the tests of R1 to R18 would stop proving that the engine is Platinum's arithmetic. The Battle Frontier, the trainer AI (ported routine by routine from Platinum's script) and every Sinnoh trainer's team were checked against the baseline.

**What keeping it costs.** Every rule that differs between the generations has two paths (`Ruleset` flags, the two damage formulas), and a session that changes one must keep the other still. `battles-platinum.txt` has caught nothing moving by accident since R19, which is the point of it, and is accepted in seconds.

**The choices.**

- **A. Keep it as the test baseline, never offered** (today's state, made permanent). Nothing to build.
- **B. Keep it and offer it**: a run of `RunRules` ("Platinum's rules") for players who want the original's battles. Small to build (the presets exist; a row of the run panel and its words), but it brings back the question of older saves and of which helps follow the rules.
- **C. Remove it**: one rule set, the modern one; the Platinum tests rewritten to the modern numbers, `battles-platinum.txt` deleted.

**Recommendation: A**, and the user chose A on 2026-10-31. The baseline is what proves the engine is Platinum's at heart, it costs little to keep, and removing it costs a great deal for no player-visible gain. B is worth doing later only if a player asks for the original's battles.

## Ticked in this audit

- **R20b, the Megas of our own**: the fifteen are forms of the data (`tools/DataImporter/Overrides/forms.json`, stones 9400 to 9414), run in battle by R20's button, stone and AI use (`MegaOfOurOwnTests`: every stone brings out its Mega as the document gives it, one Mega Evolves in battle, the trainer AI does it, a species' own stone does nothing for its regional form), are sculpted (`PokemonModels.Ours.Megas.cs`, held distinct, whole and with every decal covered by `OurSculptTests`), are listed in `PokemonModels.Forms` and so on the `dex` boards, and the user approved every design on 2026-10-26.
