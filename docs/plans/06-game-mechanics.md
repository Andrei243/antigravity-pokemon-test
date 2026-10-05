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

- Since R2 the rules of a battle are `Battle/Sim/BattleCore` and nothing else is: it has no screen, sound, keys or clock, is asked and answered in plain data, and writes a log. `Battle/BattleEngine.cs` is the face the game shows it with: the menus, and the log played back line by line. It runs single and double battles: damage, accuracy, critical hits and stat stages by Platinum's own arithmetic (`Battle/Sim/Formulas.cs`), simple status and stat moves driven by fields of `MoveData`, burn and poison damage at the end of the turn, the turn's order, switching, running, catching, EXP, level-ups and level evolution.
- Since the battle-system branch: Generation 4 status rules, about 70 abilities, about 50 held items and berries, and double battles (see R9 below). There are no EV gains, tag battles, or trainer AI beyond "what looks best".
- Since R3 a battle has its field and its passing conditions: the weather, the screens and veils of each side, what lies in wait for whoever comes in, Trick Room, Gravity and Tailwind; protection, Substitute, the moves that take two turns or hold their user, Taunt, Encore, Disable and their like; Pursuit, U-turn, Baton Pass and being dragged out; and the end of a turn in the original's order. 354 of Platinum's 467 moves run in full.
- Data (since plan 03 · D1 and R1): every species, 934 moves, 313 abilities and 771 items in `Data/*.json`; `docs/mechanics/coverage.md` says how much of each the engine runs.
- Since R1: a game is played by Platinum's rules or the modern ones (`Data/Ruleset.cs`), chosen when it begins; a battle rolls with its own generator (`Battle/Sim/BattleRandom.cs`, Platinum's), whose rolls a test can fix by kind.
- Field: walking and running, one-way ledges, one encounter table per map, trainers that spot the player, healing, a shop, PC storage.

## Decisions

**Confirmed by the user in R1 (2026-10-04).** `docs/mechanics/rulings.md` has them in full, with the table of what the two sets of rules say; the text below is what was proposed.

1. *Whose rules:* Platinum's by default, **and the modern rules as a choice made when a new game begins**, kept in the save and never changed afterwards. This is more than the proposal, which left the modern preset for later: R1 built the choice (title screen), the `Ruleset` with both presets, and the rules the engine already ran onto it.
2. *When the later mechanics appear:* **during Sinnoh's story if they can be balanced there, otherwise after the Hall of Fame** (not the proposal, which was the Hall of Fame outright). The rulings say what balanced has to mean; R20–R23 each end with the check, and plan 02's chapters S8, S9, S12 and S14 have the beats where they would arrive.
3. *How they live together:* one of each, as proposed.
4. *Where Dynamax works:* anywhere, as proposed.
5. *"All items":* everything, some as collectables, as proposed.
6. *"All moves":* as proposed. Counted properly there are 847 ordinary moves, 35 Z-Moves, 19 Max Moves and 33 G-Max Moves: the games' 919 numbers each type's Z-Move twice.
7. *Online play:* superseded by plan 07.
8. *The Game Corner:* slot machines.

**As proposed (2026-10-01):**

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

  **Outcome (2026-10-04).**
  - **Decisions**: all eight confirmed (above). Two went beyond the proposals and added work here: the modern rules as a choice at the start of a game, and the later mechanics in the story if balance allows.
  - **The rules of a game** (`Data/Ruleset.cs`): two presets, `Platinum` and `Modern`, holding every number that differs between the generations among the rules the engine runs today (critical hits and their odds, a burn's damage, paralysis, sleep, confusion, Steel's resistances, the moves' own values). A battle is fought by `BattleSetup.Rules`, or by `Ruleset.Current`, the rules of the game in progress. The title screen asks "Which rules?" after NEW GAME (`TitlePhase.ChooseRules`), the save keeps the answer (`SaveData.Rules`; older saves are Platinum's), a modern save says so on its Continue panel, and no option changes it. **Every later session reads the `Ruleset` for a rule that differs and adds the difference there**; the rulings list the ones known to come.
  - **A battle's own random numbers** (`Battle/Sim/BattleRandom.cs`, the first file of the new core): Platinum's generator (`BattleSystem_RandNext`), so a seed replays the same on every machine; its state is one number. Rolls are asked for by kind (`rng.Roll(RollKind.Critical, 16)`), and a `BattleRandom` can have a kind fixed (`Force`), which is how scenario tests pin "no critical hit, the strongest roll". Today's engine asks for its rolls by kind too, drawing exactly what it drew before (a test holds that), and the game's own battles still roll with `Dice` until R2: switching them now would have changed every seeded battle picture under the graphics session's comparison.
  - **Scenario tests** (`PokemonPlatinumTests/Scenario.cs`, `BattleScenarioTests`, `RulesetTests`, `BattleRandomTests`): Pokémon with known stats, a battle with its own rolls and rules, one rule to a test, the expected numbers worked out in a comment from the decompilation's `BattleSystem_CalcMoveDamage`, `CalcDamageVariance`, `CalcCriticalMulti` and `ApplyTypeChart`. The first ones cover the damage formula and its order, stat stages, critical hits, the burn penalty, accuracy, side effects, paralysis, a seed replaying a battle, and each rule of the two presets.
  - **The importer**: Platinum's moves carry the newest games' values where they differ (`modern`, 107 moves); Platinum's item table is whole (the hold effect's number, Fling, Natural Gift, Pluck, what can be tossed or registered, how the bag and the battle use each item and with what parameters); and a third source, Pokémon Showdown at a pinned commit (`Showdown.cs`; MIT, see THIRD-PARTY-NOTICES.md), gives each damaging move its power as a Z-Move and as a Max Move, each status move its Z-Power bonus, the 35 Z-Moves, 19 Max Moves and 33 G-Max Moves as moves of a kind of their own, and the 89 Mega Stones and 35 Z-Crystals with whose they are. It now leaves a file alone when nothing in it changed.
  - **The coverage report** (`docs/mechanics/coverage.md`) is the catalogue: every move, ability and item with how much of it runs, Platinum's items judged job by job (what using it does, what holding it does). `CoverageTests` fails when the report is not the one the data and the engine give today (so a session that writes effects has to regenerate it), and when a number falls under its floor: 231 of Platinum's 467 moves fully run and 96 partly, 70 of 123 abilities, 71 of 445 items working and 42 partly, 52 of 158 hold effects.
  - **Counts corrected**: Platinum has 445 items (the decompilation's 446th file is "no item"); the moves are 847 + 35 + 19 + 33 = 934 (decision 6).
  - **Found on the way, for R2 and R3** (listed in the rulings): a sleep lasts a turn less than drawn, the catch formula isn't the original's integer one, abilities' and items' multipliers aren't applied in the original's places, EXP has none of its bonuses. R1 left them, because the default rules had to stay exactly as they were.
  - **Not done here**: the forms themselves (Mega, Gigantamax: stats, types, abilities) are species data and come with plan 03 · D11; move flags for Gravity and Heal Block come with R3, from the original's own lists; berries' growing data with R14; the items of later games beyond the stones and crystals with R28. The modern rules don't yet change species' types and stats (plan 03 holds `species.json`), EXP, catching or TMs: each joins with its session.
  - **Tests**: 63 new in four classes (`BattleRandomTests`, `BattleScenarioTests`, `RulesetTests`, `CoverageTests`) and the title screen's question in `TitleScreenTests`; the whole suite passes. The harness's `title` mode shows the question in both answers and a modern save's Continue panel (`title_12`–`title_14`).
- **R2 · The battle core.** `Battle/Sim`: state, action queue, event pipeline, log. Move today's behaviour onto it (single battles, damage, accuracy, critical hits, stat stages, switching, running, catching, EXP) with the exact Platinum formulas, and play the log through the existing presentation. **Done when** every current test passes on the new core and recorded battles replay identically from a seed.

  **Outcome (2026-10-05).**
  - **The core** (`Battle/Sim/BattleCore.cs`, with `.Moves.cs` and `.Context.cs`): the rules and nothing else. It is given the two sides, a generator and the rules; it says what it waits for (`Request`: a choice for each Pokémon nobody inside chooses for, or a replacement for a place whose Pokémon fainted) and is answered in plain data (`Submit` of `BattleChoice`s: a move by position with its target, a switch, an item by name, running). Each answer is worked out to the next question at once, on the Pokémon the core was handed. A battle's course is written as one method that reads from top to bottom (a C# iterator, `Course`), which is what replaces the nested callbacks.
  - **The log** (`BattleLog.cs`): what happened, as events. A line (`Said`) carries what is seen as it appears (`Shows`: who lunges, the move's effect and how it landed, a stat change, a condition) and what lands a moment later (`OnImpact`: the hit's damage and its sound); between lines stand the things nobody reads (a ball thrown and how often it shook, EXP gained, a level, the end). `Log` is all of it, `TakeLog` what is new.
  - **The same choosing for both sides** (`IBattleController`): whoever chooses from inside the core does it with the battle's own numbers, so the battle still replays. The opponents' chooser is `TrainerAi`, the old "what looks best" until R9 writes Platinum's; tests and the fuzz put one on the player's side too. A player on the other end of a link battle is not one: their choices come in through `Submit` like the local player's.
  - **A battle's record** (`BattleRecord`): the number its chance began from and every answer it was given. `BattleCore.Record` gives it at any moment and `Replay` plays one through on a new battle between the same teams, to the same log; it is plain data (it goes out as JSON and comes back the same battle). What a tool changes between turns from outside (`BattleEngine.TryInflictStatus`, `ChangeStat`) is not in it.
  - **The game's face on it** (`BattleEngine`, `BattleEngine.Menus.cs`, `BattleMirror`): the core works a whole turn out before the first line of it is read, so it is handed **copies** of every Pokémon and the game's own are what the screen shows. The menus build the choices; the log is played as the old queue of messages, each line starting what it describes (`BattleAnimator`, the sounds) and changing the game's own Pokémon when it is seen: a hit's damage when the hit lands, a level at its line. Whenever a menu is open the two sets are the same (`BattleMirror.Publish`), and anything changed from outside meanwhile is taken over before the rules are asked (`Adopt`), which is what lets tests and the harness set a Pokémon's HP mid-battle as they always have. `BattleEngine`'s public members are as they were, so nothing under `UI/` or `Graphics/` changed, and the harness changed in two lines (it threw its balls through a private method that no longer exists; `BattleEngine.UseItem` is there for it).
  - **Platinum's arithmetic** (`Battle/Sim/Formulas.cs`, each function named after the original's and held by `FormulaTests`): whole numbers rounded down after every step. Damage in the original's order (the heart of it, + 2, the critical hit, a Life Orb, the roll taking 0 to 15 hundredths off, the user's own type, each of the target's types, never rounded down to nothing); a stat by its stage and a move's hit rate by the original's two tables; accuracy changed by the user's ability, the target's ability, the target's item and the user's item, in that order; the turn's order (running first, then items and switches in the order of the places, then moves by priority, a Quick Claw on one of five of the numbers each place draws, Speed, and a coin between equals); getting away by Speed against Speed with thirty more for each try that failed; EXP as base × level / 7 shared among those who fought, with the Exp. Share's half, the Lucky Egg and a trainer's Pokémon each adding half; catching with the original's two whole square roots and each ball's own strength.
  - **What plays differently for it**: running from a faster wild Pokémon can fail (it always worked); damage, EXP and a ball's chances are the original's to the point, so a little different from before in most battles; Struggle can be a critical hit; Smoke Ball and Run Away always get away; Exp. Share and Lucky Egg do what they say; Net, Dive, Nest, Repeat, Timer, Dusk and Quick Balls have their strengths (the game tells the battle whether it is night, water or a cave, and which species have been caught: `BattleConditions`, `GameEngine.BattleConditionsHere`).
  - **The game's battles roll with Platinum's generator** now (a `BattleRandom` seeded from `Dice`), which R1 had held back. Some dice changed their faces to the original's: the damage roll's 0 is the strongest hit, paralysis holds on one face of four, a thaw is one of five, a confused Pokémon hurts itself on the die's first face. So every seeded single battle in the harness shows other numbers than before (who missed, how much HP); nothing in how a battle is drawn changed, and the double battles come out the same picture for picture but for one HP bar.
  - **Coverage**: 71 of 123 abilities, 81 of 445 items working and 116 at least partly, 55 of 158 hold effects (the floors in `CoverageTests` are raised to them).
  - **Tests**: 53 new cases. `FormulaTests` holds each formula against numbers worked out by hand; `BattleCoreTests` plays the core with no screen: what it asks and refuses, the turn's order, running, EXP, a ball's four rolls, a battle recorded through the game's menus and replayed on a bare core (the same log, the same Pokémon at the end), 150 random battles between random teams that all end with nothing out of range, and that the screen's Pokémon are behind the rules' while a turn is shown and level with them when the menu opens. Older tests changed only where they read a die's faces (the weakest hit is the roll's 15 now, paralysis holds on its 0) or had left running to luck. The whole suite passes (992).
  - **Seen in the harness**: `battle`, `demo`, `doubles`, `evolution` and `versus`, each beside the same mode run on the tree before the change.
  - **Left as it was, for the session whose rule it is**: a sleep's length (R3; listed in the rulings); abilities' and items' bonuses are applied one after another and rounded down each time, but which of the formula's places each stands in hasn't been checked one by one (Thick Fat halves the finished damage where the original halves the move's power: R7, R8); a knocked-out target still goes down before the attacker's recoil is told, which is today's order and wants checking against the effect scripts with the rest of what follows a hit (R3, R4); the line "Player defeated…" doesn't say the player's name; EXP has no bonus for a traded Pokémon, because nothing is traded yet (R12, plan 07 · O5).
  - **For plan 07**: its asks 1 to 3 of the battle rework are met (rules apart from presentation, both sides chosen the same way, one seedable source), and so is the replay its O3 names. Ask 4 is half met: a battle's state is plain objects with no screen attached and any battle can be brought back by replaying its record, but nothing writes the state itself out yet.
- **R3 · Status and the field.** All status conditions and temporary states; the five weathers; screens, hazards, Trick Room, Gravity, Tailwind; protection, Substitute, semi-invulnerable and multi-turn moves; forced switches, Pursuit, U-turn and Baton Pass; the end-of-turn order.

  **Outcome (2026-10-05).**
  - **Read first, then written**: the turn itself (`battle_controller_player.c`: what can stop a move, the hit check, what follows a hit, the three passes of a turn's end), the script commands (`battle_script.c`) and the effect scripts and subscripts of every move in this session (`res/battle/scripts`), at the pinned commit. Numbers, orders and conditions were taken; no text was.
  - **The field** (`Battle/Sim/BattleField.cs`): `FieldState` holds the weather (with its turns, or that it stays), Trick Room and Gravity; a `SideState` for each side (Reflect, Light Screen, Mist, Safeguard, Lucky Chant, Tailwind, the layers of Spikes and Toxic Spikes, Stealth Rock); and a `PlaceState` for each place (a Wish, a Future Sight on its way: they land on whoever stands there). A battle opens under the weather of the place it is fought in (`BattleConditions.Weather`, which the game fills from the area's sky), and that weather stays.
  - **Passing conditions** (`Battler.Volatile`, `Battler.Turn`): everything that lasts while a Pokémon stays in, as plain values (a place, a move's data, a count) so the screen's battler can hold a copy: love, Leech Seed, a curse, a nightmare, Torment, being identified, a perish count, Taunt, Heal Block, Embargo, Magnet Rise, Charge, Yawn, Encore, Disable, a binding move, Mean Look, Lock-On, a suppressed ability, the Substitute's HP, Focus Energy, Ingrain, Aqua Ring, Minimize, Defense Curl, Destiny Bond, Grudge, Rage, Imprison, the two Sports, the chain of Protects, the move it is held to (charging, flown up or dug down, recharging, rampaging, in an uproar, biding), its last move and who hit it last. `Volatiles.Passed` is what Baton Pass hands on.
  - **A move, in the original's order** (`BattleCore.Moves.cs`): what can stop it (sleep, a freeze, having to recharge, a flinch, Disable, Taunt, Imprison, Gravity, Heal Block, confusion, paralysis, love), its PP, its targets, a turn spent getting ready, then for each target whether it gets there (the roll; Protect; Lock-On and No Guard; the weather that makes a move sure; a target in the air, under the ground or the water), the damage (into a Substitute if there is one), the lines, the side effects, what the move does of its own, Rage, the target's ability, and then what the user gets out of it (a drain, recoil, its own stat changes) before anyone the move knocked out goes down.
  - **What a move does of its own** (`BattleCore.Effects.cs`): a table by the `effect` name in `moves.json`, each entry only the parts it has (a whole move of its own, a turn to charge and where it takes the user, when it fails, its power against this target, where it can follow a target, what happens on a hit, after the hits, on a miss). 99 effects: protection and Endure, Feint, Substitute; the four weather moves, Thunder, Blizzard, Solar Beam, the sun's three healers, Weather Ball; the screens and veils, Tailwind, Trick Room, Gravity, Brick Break, Defog, Rapid Spin; Spikes, Toxic Spikes, Stealth Rock; Wish, Future Sight, Healing Wish, Lunar Dance, Roost; Fly, Bounce, Dig, Dive, Shadow Force, Razor Wind, Sky Attack, Skull Bash and the moves that reach them there; the Jump Kicks' crash; recharging, rampages, Uproar, Bide, Rage; Roar and Whirlwind, Pursuit, U-turn, Baton Pass, Teleport; Attract, Leech Seed, Nightmare, Curse, Perish Song, Taunt, Torment, Encore, Disable, Yawn, Heal Block, Embargo, the binding moves, Mean Look, Lock-On, Foresight, Miracle Eye, Gastro Acid, Psycho Shift; Focus Energy, Ingrain, Aqua Ring, Magnet Rise, Destiny Bond, Grudge, Imprison, the Sports, Minimize and Stomp, Defense Curl, Charge; Rest, Refresh, Heal Bell, Haze, Smelling Salts, Wake-Up Slap, Snore, Dream Eater, Facade, Splash. The importer asks the engine which names it has code for (`BattleCore.HasMoveEffect`) and marks those moves fully run, keeping the name so the engine finds the code.
  - **Coming and going** (`BattleCore.Switch.cs`): a switch with Pursuit catching whoever leaves at twice the power; what lies in wait in the original's order (Toxic Spikes, which a Poison type takes away, Spikes, Stealth Rock; Magic Guard walks through); being dragged out by Roar; and leaving by one's own move, for which the replacement is asked in the middle of the turn (U-turn, Baton Pass, Healing Wish, Lunar Dance).
  - **The end of a turn** (`BattleCore.Turn.cs`): the three passes in the original's order (the rulings list them), everyone from the fastest, and whoever is brought to nothing fainting before the next thing happens.
  - **What may be chosen** (`BattleCore.Choices.cs`): `WhyNot` says why a choice can't be made (a disabled, tormented, taunted, sealed, grounded, heal-blocked or encored move, a Choice item, no PP; being held on the field by a binding move, Mean Look, roots, Shadow Tag, Arena Trap or Magnet Pull, with Shed Shell, Smoke Ball and Run Away as the ways out), `Submit` refuses what it would refuse, and the menus ask it before handing a choice over. A Pokémon in the middle of a move isn't asked at all, so a turn in which nobody outside has anything to choose is played straight on.
  - **Abilities and items for it**: Drizzle, Drought, Sand Stream, Snow Warning, Cloud Nine, Air Lock, Swift Swim, Chlorophyll, Sand Veil, Snow Cloak, Rain Dish, Ice Body, Dry Skin, Solar Power, Hydration, Leaf Guard, Shadow Tag, Arena Trap, Magnet Pull, Suction Cups, Oblivious, Bad Dreams; Light Clay, the four weather rocks, Grip Claw, Power Herb, Shed Shell, Big Root, Iron Ball. Levitate comes down under Gravity, with roots or an Iron Ball.
  - **The rules that differ** went into the `Ruleset` with their code (the rulings have the table): how long an ability's weather, a Taunt, an Encore, Disable, a Tailwind, an uproar and a binding move last, what a binding takes, how fast Protect wears out, Minimize's stages, Grass and powder, Ghosts and trapping, a Poison type's Toxic.
  - **What plays differently in the default rules**, each now Platinum's own: a sleep lasts 1 to 4 turns (it was 0 to 3); bad poison rounds its sixteenth down before the count; recoil and a drain are told before the target goes down; a Flame or Toxic Orb's harm starts the turn after; the turn's end has the original's order. And the 123 of Platinum's moves that hit without their effect or did nothing now do what they do.
  - **On the screen**: the menus say why a move, a switch or running is refused; a Pokémon that flew up or dug down is off its platform with its HP box left in place (`CombatantView.Away`, one check in `BattleRenderer`); the battle knows its weather (`BattleEngine.Weather`). A Substitute has no doll and the weather over a battle isn't drawn: both wait for the battle's art to take them up.
  - **The opponents' chooser** only picks what the rules allow, and has a sense of when each of these moves would still do something (a screen that isn't up, a foe not yet seeded); it is still not Platinum's AI (R9).
  - **Coverage**: Platinum's moves 354 run in full (231 before), 42 partly, 71 not yet; 112 of the later moves; 93 of 123 abilities; 91 items working and 126 at least partly, 65 of 158 hold effects. The floors are raised to them.
  - **Tests**: 78 new in `BattleFieldTests`, on the rules alone with both sides answered for, each number worked out from the original's code in a comment: what stops a move and for how long, Protect's chain, the Substitute, each weather's turns, damage and abilities, the screens, each hazard by its fractions, Trick Room, Tailwind, Gravity, the two-turn moves and what reaches them, recharging, rampages, Bide, Uproar, Pursuit, U-turn, Baton Pass, Roar and its level check, the ways of being held on the field, the conditions that work over time, the end of a turn's order, the modern rules' differences, the menus' refusals and a flight played through on the screen. One more plays 240 battles between random teams made of these moves, single and double, under every sky and by both sets of rules, which all end with nothing out of range and replay the same. The recorded battle of R2 gained a U-turn, so a replacement asked for in the middle of a turn is part of what replays. Older tests changed in three places, each to the original's number (a sleep's counter, bad poison's rounding, which ability is still to write). The whole suite passes (1,208).
  - **Seen in the harness**: `battle`, `demo` and `doubles` beside R2's runs (the same pictures; the lines differ only where the order of rolls or of recoil changed), and a new mode `conditions`, which shows a flight: the platform empty, then the Pokémon back for its strike.
  - **Left for the session whose rule it is**: the moves in these families that are their own (Magic Coat, Snatch, Follow Me, Fake Out, Sucker Punch, Counter, Rollout: R4, R5); abilities' and items' bonuses in the formula's exact places (R7, R8); the modern numbers R3 added, to be checked against Showdown's table of conditions, which wasn't among the files fetched (R19); the battle's drawing of a Substitute and of the weather (plan 04's follow-up, with R6's hooks).

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
- **R13 · Wild encounters.** Everything in the "Wild encounters" row above, on top of plan 01's tables. *Ready from plan 01 · M2–M3:* each open area's base land table and its water table with Platinum's rates, and the odds of a step meeting something (`EncounterSteps` in `Overworld/Map.cs`: the grace steps after a battle, four attempts in ten, then the rate). Still to come here: the slots the time of day swaps in (the area files carry only the morning table; `habitats.json`, which plan 03 · D10 added for the Pokédex, has every area's day and night species), the lead Pokémon's ability, Repels, and the rest of the row.
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

- **Scenario tests**: one or more per move, ability, item and rule, with a seeded generator and forced rolls ("a burned attacker's physical move does half damage"). `PokemonPlatinumTests/Scenario.cs` has the helpers.
- **Recorded battles**: a seed and a list of choices must always produce the same log (`BattleCoreTests.ARecordedBattleReplaysTheSame`, since R2; a session that adds a kind of choice or of request adds it to that battle).
- **Fuzzing**: thousands of random battles between random teams, checking invariants (no exceptions, HP and PP in range, every battle ends). `BattleCoreTests.RandomBattlesAlwaysEndAndLeaveNothingOutOfRange` plays 150 in every test run since R2, and since R3 `BattleFieldTests.BattlesFullOfTheseMovesAlwaysEndAndReplayTheSame` 240 more between teams made of the moves that have an effect of their own; R30 plays the thousands.
- **Numbers from the source**: damage and catch-rate examples computed by hand from the decomp's formulas become test vectors (`FormulaTests`, `BattleScenarioTests`).
- **In the game**: the harness's `battle` mode gains scripted fights showing each new mechanic, checked on screenshots.

## Risks

- **Size.** This is the largest plan: about 30 sessions. Platinum's battles (R1–R10) matter most, because the story needs them; the later generations can wait until after the Hall of Fame.
- **Rewriting the battle engine** while other plans change its presentation. R2 kept `BattleEngine`'s public members, so the interface, the tests and the harness carried on working (done 2026-10-05). From here a rule is written in `Battle/Sim` and what it looks like is a kind of event the engine shows: a session that needs something new on screen adds an event to `BattleLog.cs` and a case to `BattleEngine.Show`.
- **Rules that contradict each other** across generations. Decide once, in `rulings.md`, and keep the difference in the `Ruleset`.
- **Models for forms.** 97 Mega forms and 34 Gigantamax forms need models and transformation effects: plan 03's model generator (D5) must treat forms as models of their own, and plan 04 · G8 animates the transformations.

## Needs and gives

- **Needs** plan 03 · D1 (the data importer) before R1; plan 04's interface kit for the new battle controls and screens; plan 01's areas for encounters and battle environments; plan 02 · S1's scripting for the NPC services and the post-game quests.
- **Gives** plan 02 its battles (R9 replaces S3), plan 03 its move, ability, evolution and form mechanics (R4–R8 and R10 replace D2–D4; R24–R27 complete D11), and plan 05 the battle log that sounds hang on.

## Status

- [x] R1 Catalogue and rulings (2026-10-04: the eight decisions confirmed; Platinum's or the modern rules chosen at a new game and kept in the save; `Ruleset`; a battle's own generator with rolls by kind; scenario tests with numbers from the original's formulas; the importer's modern move values, whole item table and Z, Max and Mega data; the coverage report as a catalogue held by tests)
- [x] R2 The battle core (2026-10-05: `Battle/Sim/BattleCore`, the rules with no screen, asked and answered in plain data and writing a log; `BattleEngine` the game's face on it, playing the log through the presentation as it was; Platinum's exact arithmetic for damage, accuracy, the turn's order, running, EXP and catching; a battle's record of seed and answers that replays to the same log; the game's battles on Platinum's generator; 150 random battles in every test run)
- [x] R3 Status and the field (2026-10-05: the battle's field and every passing condition, written from the original's turn, script commands and effect scripts; 99 move effects by name, so 354 of Platinum's 467 moves run in full; the weather with its abilities and rocks, screens, hazards, Trick Room, Gravity, Tailwind; protection, Substitute, two-turn and held moves; Pursuit, U-turn, Baton Pass, Roar; what may be chosen and why not; the end of a turn in the original's order; 79 new tests)
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
