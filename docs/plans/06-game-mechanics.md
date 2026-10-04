# Plan 06 · Game mechanics

**Goal**: every rule of Pokémon Platinum works the way it does in the original: battles, catching, growth, breeding, items, the field systems and the side activities. On top of that, the game gains the four battle mechanics of later generations (Mega Evolution, Z-Moves, Dynamax and Terastallization), every move and every item of the main-series games.

## Scope in numbers

Counted on 2026-10-01 from the decompilation and from PokeAPI's data:

| | In Platinum | In all main games |
|---|---|---|
| Moves | 467 | 919 (among them 35 Z-Moves and 19 Max Moves), plus 33 G-Max Moves |
| Abilities | 123 | 314 |
| Items | 446 (92 TMs, 8 HMs and 64 berries among them) | about 2,200 |
| Move-effect scripts | 277 | – |
| Mega forms | – | 97 (48 from Generations 6–7, 49 from Legends: Z-A and its expansion), 92 Mega Stones, 2 Primal forms |
| Z-Crystals | – | 35 (18 for types, 17 for single species) |
| Gigantamax forms | – | 32 species (34 form entries) |
| Tera Types | – | 18 and Stellar |

## Where we are

- `Battle/BattleEngine.cs` (about 1,000 lines) holds the rules, the message queue and the timing in one class. It runs single battles: damage (Generation 4's formula with critical hits, STAB, the type chart and the burn penalty), accuracy, stat stages, simple status and stat moves driven by fields of `MoveData`, burn and poison damage at the end of the turn, switching, running, catching (`CatchCalculator`), a flat EXP formula, level-ups and level evolution.
- Since the battle-system branch: Generation 4 status rules, about 70 abilities, about 50 held items and berries, and double battles (see R9 below). There are no EV gains, friendship, weather, protection, multi-turn moves, hazards, tag battles, or trainer AI beyond "strongest move".
- Data: 45 moves, 18 items in 5 bag pockets, 23 species.
- Field: walking and running, one-way ledges, one encounter table per map, trainers that spot the player, healing, a shop, PC storage.

## Decisions to confirm in R1

1. **Whose rules.** *Recommended:* Platinum's rules are the baseline, so the story plays exactly like Platinum. Anything that didn't exist in Platinum follows the game that introduced it, in its latest form. All differences between generations live in one `Ruleset` object, so a "Modern rules" preset (1.5× critical hits, 5-turn weather from abilities, reusable TMs, party-wide Exp. Share and so on) can be offered later as an option.
2. **When the later mechanics appear.** *Recommended:* after the Hall of Fame, like the National Pokédex: four short post-game quests give the Key Stone, the Z-Ring, the Dynamax Band and the Tera Orb, and post-game opponents (League rematches, the Battle Frontier, special trainers) use them. The story itself stays free of them. Each can be switched off in the options.
3. **How they live together.** No official game has all four. *Recommended:* each mechanic once per battle per trainer, and a Pokémon can use only one of them in a battle. Follow the conventions of formats where they coexist: a Pokémon holding a Mega Stone or a Z-Crystal can't Terastallize; a Mega or Primal Pokémon can't Dynamax. The stricter alternative is one mechanic in total per battle.
4. **Where Dynamax works.** In Sword and Shield only at Power Spots. *Recommended:* anywhere once unlocked; alternative: only in gyms, the League, the Battle Frontier and raid dens.
5. **What "all items" means.** *Recommended:* every item exists in the data with its name, description, pocket and price. Items whose effect belongs to a system this game has get their real effect. Items of systems this game lacks (curry and sandwich ingredients, TM materials, other regions' story keys, about 700 entries) exist as collectables without a function, and a report lists them.
6. **What "all moves" means.** All 919 main-series moves with their exact effects, the Z-Moves, the Max Moves and the 33 G-Max Moves. The 18 Shadow moves of the GameCube spin-offs are left out.
7. **Out of scope.** Online play (Wi-Fi battles and trades, the GTS, the Union Room, the Underground's multiplayer, Mystery Gift). Rewards that needed them get an in-game source (plan 03's decision 4 covers trades and event Pokémon).
8. **The Game Corner.** Slot machines as in the international Platinum, or the later European variant (a machine that simply hands out coins).

Record every ruling, including small ones met along the way (for example hail and Generation 9's snow both existing), in `docs/mechanics/rulings.md`.

## Sources of truth

- **Platinum's rules**: the decompilation. `src/battle/` (`battle_lib.c`, `battle_script.c`, `battle_controller.c`), the 277 move-effect scripts in `res/battle/scripts/effects/` and their `subscripts`, the trainer AI in `src/battle/trainer_ai/`, constants in `include/constants/battle/`, items in `res/items/data/<item>.json`, the Pokétch, the Underground and the other systems under `src/applications/`, `src/underground/` and neighbours. Take every number from there, not from memory.
- **Later generations**: Pokémon Showdown (<https://github.com/smogon/pokemon-showdown>, MIT). `data/moves.ts`, `abilities.ts`, `items.ts` and `conditions.ts` describe every effect; each move carries its Z-Move and Max Move power (`zMove`, `maxMove`); `sim/battle-actions.ts` has the Mega, Z, Dynamax and Tera rules; `data/mods/gen4pt/` holds Platinum's differences. Its test suite (`test/sim/`) is a ready list of scenarios. If logic or tests are ported, keep its licence notice in `THIRD-PARTY-NOTICES.md`.
- **Data**: PokeAPI's CSV files: `moves.csv`, `move_meta.csv`, `move_meta_stat_changes.csv`, `move_flag_map.csv`, `move_changelog.csv` (values per generation), `abilities.csv`, `items.csv` with `item_categories.csv`, `item_flag_map.csv`, `item_fling_effects.csv` and `item_prose.csv`, `berries.csv`, `machines.csv` (TM lists per game), `pokemon_forms.csv` (Mega and Gigantamax forms), `type_efficacy_past.csv`, the `*_past.csv` files for old stats, types and abilities, `super_contest_effects.csv`.

## Architecture

- **A battle core without presentation**, in `Battle/Sim/`: `BattleState` (two sides, one or two active Pokémon each, the field), an action queue (switches, the activation of Mega/Z/Dynamax/Tera, items, then moves by priority and Speed), and an event pipeline that moves, abilities, items and conditions hook into (before a move, on trying to hit, modifying damage, after damage, end of turn, on entry and so on), in a defined order. A seeded random generator makes every battle reproducible.
- **A battle log**: the core emits events (messages, damage, status, animations to play). `BattleAnimator`, the HUD and the sound director play the log; this replaces today's nested `QueueMessage` callbacks. `BattleEngine` stays as the façade that the interface, the tests and the harness use (`SelectMove`, `ConfirmMessage`, `Update`).
  *Ready from plan 04 · G8:* the animations to play are `EffectCue`s on `BattleAnimator` (a move from one place to another and how it landed, a stat change, a heal, a status condition; `Cue`, `StatChange`, `Heal`, `StatusGiven`) and the thrown ball (`ThrowBall`); the log's events map onto them one to one, and the effects land `BattleAnimator.ImpactTime` after their cue, when the damage does.
- **Effects as handlers**: one small class per unique move effect, ability, item or condition, registered by id. A generic handler driven by the move's data (damage class, ailment and chance, stat changes, hit count, drain, recoil, healing, critical rate, flinch chance, flags) covers the majority of moves without code; only the unique ones are written by hand.
- **`Ruleset`**: every number or rule that differs between generations is read from it.
- **Data**: generated by plan 03's `tools/DataImporter`, extended with move metadata and flags, per-generation values, abilities, the full item table, and the Z-Move, Max Move, Mega Stone and Z-Crystal data.
- **Coverage report**: a tool lists every move, ability and item as fully implemented, approximated or missing, into `docs/mechanics/coverage.md`; a test fails when the report gets worse.
- **Field systems** as separate services under `Systems/` (encounters, daily events, berries, breeding, storage), each testable without a window, saved through `SaveData` with migrations.

## What "all of Platinum's mechanics" covers

| Area | Contents |
|---|---|
| Battle core | Turn order (priority, Speed, Quick Claw, Trick Room), the exact damage and accuracy formulas, critical-hit stages, all seven stat stages, running, obedience, Struggle, PP |
| Status | Burn, freeze, paralysis, poison, bad poison, sleep; confusion, infatuation, flinching, Leech Seed, Curse, Nightmare, Perish Song, Taunt, Torment, Encore, Disable, Yawn, Heal Block, Embargo, trapping, Substitute and the rest of the temporary states |
| The field | Sun, rain, sandstorm, hail and fog; Reflect, Light Screen, Safeguard, Mist, Lucky Chant, Tailwind, Trick Room, Gravity; Spikes, Toxic Spikes, Stealth Rock; Wish, Future Sight, Healing Wish |
| Moves | All 467: multi-hit, two-turn and semi-invulnerable moves, recharge, rampages, fixed and level damage, one-hit KOs, counters, draining, recoil, moves that call or copy others, item moves, type and ability changes, switching moves, moves that depend on the battlefield (Secret Power, Nature Power) |
| Abilities | All 123, including their effects outside battle (encounter rates, eggs, items picked up) |
| Held items | Every hold item and all 64 berries, Fling and Natural Gift, the plates, the incenses |
| Formats | Single, double and tag battles, wild double battles with a partner, the Great Marsh's Safari Game, the Pal Park catching show, battles that can't be fled |
| Trainer AI | Platinum's AI flags, switching and item use |
| Catching | The catch formula with every ball of Generation 4, shakes, nicknames, sending to the PC |
| After battle | EXP with its bonuses, EVs, friendship, Pokérus, learning moves (with the prompt to forget one), evolution and cancelling it, prize money, whiting out, Pickup and Honey Gather |
| Growth | Natures, characteristics, Hidden Power, shininess, vitamins and EV berries, Macho Brace and the Power items, TMs and HMs, the move tutors, the Move Relearner and Deleter, the Name Rater |
| Evolution and forms | Every evolution method; Giratina, Rotom, Shaymin, Deoxys, Burmy and Wormadam, Shellos, Unown, Castform, Cherrim, Arceus |
| Breeding | The Day Care, egg groups, inheritance of IVs, nature and moves, incense babies, hatching, the Masuda method |
| Bag and shops | The eight pockets, every item's use, registering a key item, mail; Poké Marts whose stock grows with badges, the Veilstone Department Store, the special shops, hidden items and the Dowsing Machine |
| Wild encounters | Rates and slots by terrain and time of day, the lead Pokémon's ability, Repels, swarms, fishing, surfing, honey trees, the Poké Radar and its chains, the Great Marsh and Trophy Garden dailies, roaming Pokémon, Feebas's tiles, poison in the field |
| Time | The clock's five periods, daily and weekly events, berry growing with mulch and watering, Poffin cooking and contest condition, the lottery |
| Tools | The Pokétch and its 25 apps, the Vs. Seeker and rematches, the Journal, the Trainer Card and its stars, PC storage (18 boxes of 30) with wallpapers and markings, the Hall of Fame records |
| Side activities | The Underground on your own (mining, spheres, secret base), fossils, Amity Square, the Game Corner, the Villa, Super Contests (Visual, Dance, Acting), Ball Capsules and Seals, ribbons |
| Battle Frontier | The Tower, Factory, Hall, Castle and Arcade, Battle Points, the five Frontier Brains |

Movement (bike, surfing, ice, mud, snow) is plan 01's; HM obstacles and story gates are plan 02's.

## The four later mechanics

Rules checked against Pokémon Showdown's source on 2026-10-01.

**Mega Evolution** (Generations 6–7, Legends: Z-A)
- Needs the Key Stone and the Pokémon holding its own Mega Stone; Rayquaza instead needs to know Dragon Ascent.
- Chosen together with a move and happens before the moves of that turn. One Mega Evolution per trainer per battle. It lasts for the rest of the battle, also through switches.
- Changes the base stats, the ability and sometimes the type. A Mega Stone can't be knocked off, swapped or thrown.
- Primal Reversion: Kyogre and Groudon holding their orbs transform when they enter, without using up the Mega Evolution, and bring their own weather.

**Z-Moves** (Generation 7)
- Needs the Z-Ring and a held Z-Crystal. A type crystal turns a damaging move of its type into that type's Z-Move, with a power set per move; a status move of its type keeps its effect and gains a bonus first (a stat boost, a full heal, a reset of lowered stats). A species crystal turns one signature move into an exclusive Z-Move.
- One Z-Move per trainer per battle. It can't miss, and it does a quarter of its damage through Protect. The crystal is not used up and can't be removed.
- Ultra Burst (Necrozma) belongs here.

**Dynamax and Gigantamax** (Generation 8)
- Needs the Dynamax Band. Once per trainer per battle; lasts three turns and ends on switching out.
- HP is multiplied by 1.5 at Dynamax Level 0 up to 2.0 at Level 10 (raised with Dynamax Candy).
- Every move becomes the Max Move of its type, with a power set per move and a side effect (weather, terrain, or a stat change for a whole side); status moves become Max Guard.
- A Dynamax Pokémon can't flinch or be forced out; weight-based moves and one-hit KOs fail against it; Choice items don't lock it.
- Pokémon with the Gigantamax Factor take their Gigantamax form and use their G-Max Move. Zacian, Zamazenta and Eternatus can't Dynamax.

**Terastallization** (Generation 9)
- Needs the Tera Orb, recharged at a Pokémon Center. Once per trainer per battle; lasts until the Pokémon faints or the battle ends, also through switches.
- The Pokémon takes its Tera Type for defence. Moves of the Tera Type get the 1.5× same-type bonus, or 2× if the Pokémon already had that type; its original types keep their bonus. Tera-type moves weaker than 60 power count as 60 (not multi-hit or priority moves). Tera Blast takes the Tera Type.
- Stellar keeps the defensive types and boosts each type once: 2× for the Pokémon's own types, 1.2× for the others.
- Every Pokémon has a Tera Type, one of its own types by default; 50 Tera Shards change it.

They also bring what their moves and items rely on: the Fairy type, the four terrains, Aurora Veil, the Primal weathers and snow, new entry hazards, and the later abilities.

## Sessions

Each session ends with its tests green, the coverage report updated and the session ticked below.

### Foundations
- **R1 · Catalogue and rulings** (needs plan 03 · D1, done 2026-10-01: `tools/DataImporter` already brings in move metadata and flags, abilities and Platinum's items, flags each move's effect as fully run, partly run or not run, and writes a first `docs/mechanics/coverage.md`). Extend the importer (move metadata and flags, per-generation values, abilities, Platinum's 446 items, the Z, Max and Mega data). Build the coverage report. Confirm the decisions above and write `docs/mechanics/rulings.md`. Add the seeded random generator and the first scenario tests.
- **R2 · The battle core.** `Battle/Sim`: state, action queue, event pipeline, log. Move today's behaviour onto it (single battles, damage, accuracy, critical hits, stat stages, switching, running, catching, EXP) with the exact Platinum formulas, and play the log through the existing presentation. **Done when** every current test passes on the new core and recorded battles replay identically from a seed.
- **R3 · Status and the field.** All status conditions and temporary states; the five weathers; screens, hazards, Trick Room, Gravity, Tailwind; protection, Substitute, semi-invulnerable and multi-turn moves; forced switches, Pursuit, U-turn and Baton Pass; the end-of-turn order.

### Platinum's battles
- **R4 · Moves I: the data-driven families.** The generic handler; about 300 moves need no code of their own.
- **R5 · Moves II: unique effects.** Calling and copying moves, counters, item moves, type and ability changes, stat swaps, the remaining one-of-a-kind effects.
- **R6 · Moves III: the rest of the 467**, the battlefield-dependent moves, and the hooks plan 04 · G8 animates. **Done when** the report shows 467 of 467.
- **R7 · Abilities.** All 123, in battle and in the field.
- **R8 · Held items and berries**, and the items used during battle.
- **R9 · Double battles and AI.** Doubles and tag battles, targets, the partner, wild doubles, Platinum's trainer AI, the Safari Game, the Pal Park show, scripted battles (the catching lesson, legendaries, roamers that flee). This replaces plan 02 · S3.

*Started ahead of R1–R2 (battle-system branch), on today's `BattleEngine` rather than `Battle/Sim`:* battles are slot-based (`Battler`, `BattleSetup`, `BattleFormat`), with double battles against one trainer (`Trainer.DoubleBattle`), two trainers at once and wild pairs, target choice, spread moves at ×0.75 and replacement of fainted Pokémon. Sleep, freeze, paralysis, confusion and flinching follow Generation 4; Struggle and EXP split among participants are in. Abilities and held items share one hook class (`Battle/Effects/BattleEffect.cs`, the start of the event pipeline): about 70 abilities work and `AbilityDatabase` marks the rest `IsImplemented = false`; about 50 held items and berries work through `HeldItemEffects`. Species abilities, move targets and flags, and the held items are in the JSON files. Still to do for R7–R9: the remaining abilities, weather, tag battles with a partner, trainer AI that switches and uses items, and two trainers spotting the player together in the field. R2 should carry these hooks onto the new core rather than rewrite them.
- **R10 · After the battle.** EXP, EVs, friendship, Pokérus, the move-learning prompt, every evolution method and Platinum's forms, money and whiting out, obedience, shininess, natures and Hidden Power. This replaces plan 03 · D4.
  *Done ahead of R10 (2026-10-02, at the user's request):* evolution and friendship. Every evolution method of every generation in the data has a rule (`Models/Evolution.cs`, `docs/mechanics/evolution.md`); evolution happens after the battle in a scene of its own (`UI/EvolutionScreen.cs`, `GameState.Evolution`) that B can stop and that asks which move to forget; stones, Rare Candies and hold items go to a Pokémon the player picks from the bag; friendship follows Platinum's table for levels, walking and fainting (`Models/Friendship.cs`); Pokémon carry friendship, beauty, a personality value and the ball they were caught in, and saves keep them. The rulings are in `docs/mechanics/rulings.md`, which this started. R10 still owes: the move-learning prompt inside battles (the scene's `ModernUi.MoveChoice` is ready to reuse; `GainExp` still says "learned" for a fifth move it couldn't take), EVs, Pokérus, money, obedience, forms. Evolutions that wait on other sessions: Feebas on Poffins (R14), Sliggoo on field weather and the Moss Rock, Ice Rock and magnetic field on their maps (plan 01: set `evolutionSites` in the map files), trade evolutions on trading (R12 for in-game trades, plan 07 · O5 online; both call `GameEngine.ReceiveTradedPokemon`), and the three evolutions from regional forms that need a rule of their own (R29).

### Platinum's systems
- **R11 · The bag, items and shops.** The eight pockets, every Platinum item's effect, TMs, HMs and tutors, mail, the shops and their stock, hidden items.
  *Ready from plan 04 · G10 (2026-10-04):* the screens (`UI/BagScreen.cs`, `UI/ShopScreen.cs`; style guide, "Menu screens"). The bag has the eight pockets as tabs, each remembering its cursor, and an action menu per item (`BagScreen.ActionsFor`: USE, GIVE, CANCEL); medicine is used on a party Pokémon through `Models/FieldItems.cs` (HP, status, Revive, Full Restore) and anything but a Key Item or a TM can be given to hold. The shop asks how many (`ShopScreen.MostAffordable`, 99 at most). Still to come here: what every other item does (`BagScreen.CanUse` knows only medicine and evolution items), tossing and registering, teaching a TM from the bag, each shop's own stock (every clerk sells the same six things, `ShopScreen.Open`), and selling.
- **R12 · Storage and trainer tools.** PC boxes, the Hall of Fame records, the Trainer Card, the Journal, in-game trades, the Vs. Seeker and rematches.
  *Ready from plan 04 · G10 (2026-10-04):* the screens (`UI/PCScreen.cs`, `UI/TrainerCardScreen.cs`). The PC shows eighteen boxes of thirty over the flat list of stored Pokémon, in the order they were stored (`PCScreen.StoredIndex`); a slot of its own for each Pokémon, moving them, box names and wallpapers are still to come here. The Trainer Card shows a trainer ID (rolled at a new game, 0 to 65535) and the day the adventure began, both in the save since G10; its back, the stars and the signature are still to come.
- **R13 · Wild encounters.** Everything in the "Wild encounters" row above, on top of plan 01's tables. *Ready from plan 01 · M2–M3:* each open area's base land table and its water table with Platinum's rates, and the odds of a step meeting something (`EncounterSteps` in `Overworld/Map.cs`: the grace steps after a battle, four attempts in ten, then the rate). Still to come here: the slots the time of day swaps in, the lead Pokémon's ability, Repels, and the rest of the row.
- **R14 · Time.** Daily and weekly events, berries, Poffins and condition, the lottery, the Pokétch and its apps.
- **R15 · Breeding.**
- **R16 · Side activities I.** The Underground, fossils, Amity Square, the Game Corner, the Villa.
- **R17 · Side activities II.** Super Contests, Ball Capsules and Seals, ribbons.
- **R18 · The Battle Frontier.**

### Later generations
- **R19 · Modern groundwork.** The Fairy type, terrains, the new weathers, Aurora Veil, new hazards, form changes during battle, and the "Modern rules" preset of the `Ruleset`.
- **R20 · Mega Evolution**: the Key Stone, 92 Mega Stones, 97 forms, Primal Reversion, Mega Rayquaza, the button in the move menu, the AI's use of it, where the stones are found.
- **R21 · Z-Moves**: the Z-Ring, 35 crystals, the type and exclusive Z-Moves, Z-Power on status moves, Ultra Burst.
- **R22 · Dynamax and Gigantamax**: the band, Dynamax Levels and candy, 19 Max Moves, 33 G-Max Moves, the Gigantamax Factor. Optional: Max Raid dens.
- **R23 · Terastallization**: the orb, Tera Types and shards, Tera Blast, Stellar, Ogerpon and Terapagos. Optional: Tera Raid crystals.
- **R24–R26 · Moves of Generations 5–9**: about 380 regular moves in three batches, each with its exact effect. **Done when** the report shows 919 of 919.
- **R27 · Abilities of Generations 5–9**: the remaining 191.
- **R28 · Items of all games**: the hold items, medicines, balls (Apricorn, Beast, Hisuian), evolution and form-change items, mints, bottle caps, ability capsules and patches, EXP candies, TRs, gems, seeds, memories and drives with their effects; the collectable-only items as data; where each is obtained (with plan 03 · D12).
- **R29 · Later forms and evolutions**: fusions (Kyurem, Necrozma, Calyrex), Zygarde, Hoopa, Oricorio, Aegislash and the other in-battle form changes; the evolution methods of later generations, with a replacement where the original needs hardware this game lacks.
- **R30 · Sign-off**: AI that uses the four mechanics sensibly, random-battle fuzzing, balance, the options for rules and mechanics, a final coverage report with every exception explained.

## Verification

- **Scenario tests**: one or more per move, ability, item and rule, with a seeded generator and forced rolls ("a burned attacker's physical move does half damage").
- **Recorded battles**: a seed and a list of choices must always produce the same log.
- **Fuzzing**: thousands of random battles between random teams, checking invariants (no exceptions, HP and PP in range, every battle ends).
- **Numbers from the source**: damage and catch-rate examples computed by hand from the decomp's formulas become test vectors.
- **In the game**: the harness's `battle` mode gains scripted fights showing each new mechanic, checked on screenshots.

## Risks

- **Size.** This is the largest plan: about 30 sessions. Platinum's battles (R1–R10) matter most, because the story needs them; the later generations can wait until after the Hall of Fame.
- **Rewriting the battle engine** while other plans change its presentation. R2 keeps `BattleEngine`'s public methods so the interface, tests and harness carry on working.
- **Rules that contradict each other** across generations. Decide once, in `rulings.md`, and keep the difference in the `Ruleset`.
- **Models for forms.** 97 Mega forms and 34 Gigantamax forms need models and transformation effects: plan 03's model generator (D5) must treat forms as models of their own, and plan 04 · G8 animates the transformations.

## Needs and gives

- **Needs** plan 03 · D1 (the data importer) before R1; plan 04's interface kit for the new battle controls and screens; plan 01's areas for encounters and battle environments; plan 02 · S1's scripting for the NPC services and the post-game quests.
- **Gives** plan 02 its battles (R9 replaces S3), plan 03 its move, ability, evolution and form mechanics (R4–R8 and R10 replace D2–D4; R24–R27 complete D11), and plan 05 the battle log that sounds hang on.

## Status

- [ ] R1 Catalogue and rulings
- [ ] R2 The battle core
- [ ] R3 Status and the field
- [ ] R4 Moves I: data-driven families
- [ ] R5 Moves II: unique effects
- [ ] R6 Moves III: the rest of the 467
- [ ] R7 Abilities
- [ ] R8 Held items and berries
- [ ] R9 Double battles and AI
- [ ] R10 After the battle
- [ ] R11 The bag, items and shops
- [ ] R12 Storage and trainer tools
- [ ] R13 Wild encounters
- [ ] R14 Time
- [ ] R15 Breeding
- [ ] R16 Side activities I
- [ ] R17 Side activities II
- [ ] R18 The Battle Frontier
- [ ] R19 Modern groundwork
- [ ] R20 Mega Evolution
- [ ] R21 Z-Moves
- [ ] R22 Dynamax and Gigantamax
- [ ] R23 Terastallization
- [ ] R24 Moves of Generations 5–9, batch 1
- [ ] R25 Moves of Generations 5–9, batch 2
- [ ] R26 Moves of Generations 5–9, batch 3
- [ ] R27 Abilities of Generations 5–9
- [ ] R28 Items of all games
- [ ] R29 Later forms and evolutions
- [ ] R30 Sign-off
