# Rulings

Choices made where the games disagree with each other, or where a rule leans on something this game doesn't have. Each says what was chosen and why, so it can be changed in one place. The general ones come first; the rest are in the order they were taken.

## The general rulings (plan 06 · R1, confirmed by the user on 2026-10-04)

1. **Whose rules: Platinum's, or the modern ones, chosen once.** A new game asks on the title screen, after NEW GAME: *Platinum rules* (offered first) or *Modern rules*. The answer is kept in the save (`SaveData.Rules`) and nothing in the game changes it afterwards: there is no option for it. Under Platinum's rules everything Platinum has works as in Platinum. Under the modern rules the same things follow the newest games. Either way, whatever came after Platinum follows the game that introduced it, in its latest form.
   - Every difference lives in `Data/Ruleset.cs`. Code that runs a rule which differs between the generations asks the battle's `Rules` (or `Ruleset.Current` outside a battle) for the number; it never carries a constant of its own. The table below is the list.
   - A move's own numbers follow the rules too: `moves.json` holds Platinum's values, and `modern` beside them where the newest games differ (107 of Platinum's moves). `Ruleset.Use` swaps them in as a game begins or loads.
2. **When Mega Evolution, Z-Moves, Dynamax and Terastallization appear: during Sinnoh's story, if they can be balanced there; otherwise after the Hall of Fame.** See "The later mechanics in the story" below for what balanced has to mean and when it is checked.
3. **How they live together: one of each.** Each mechanic once per trainer per battle, and a Pokémon can use only one of them in a battle. A Pokémon holding a Mega Stone or a Z-Crystal can't Terastallize; a Mega or Primal Pokémon can't Dynamax.
4. **Where Dynamax works: anywhere**, once the Dynamax Band is in hand.
5. **"Every item": all of them, some only as collectables.** Every item of the main games is in the data with its name, description, pocket and price. Items of systems this game has get their real effect. The rest (curry and sandwich ingredients, TM materials, other regions' story keys: about 700) exist without a function, and the coverage report lists them.
6. **"Every move": 847 ordinary moves, 35 Z-Moves, 19 Max Moves and 33 G-Max Moves**, each with its exact effect. The games number 919 because each of the eighteen type Z-Moves has a physical and a special id; here each is one move. The 18 Shadow moves of the GameCube games are left out.
7. **Online play** is no longer out of scope: plan 07 builds it. A battle between two players is fought by one set of rules that both agree to before it starts (plan 07 · O6 decides how the room offers it).
8. **The Game Corner has slot machines**, as in the Platinum the decompilation is built from.

### What the two sets of rules say

In the `Ruleset` today, because the engine already runs these rules:

| Rule | Platinum | Modern |
| --- | --- | --- |
| A critical hit | × 2 | × 1.5 |
| Chance of one, by stage | 1 in 16, 8, 4, 3, 2 | 1 in 24, 8, 2, then always |
| Sniper | half as much again: × 3 | half as much again: × 2.25 |
| A burn takes, each turn | 1/8 of HP | 1/16 of HP |
| Paralysis cuts Speed to | a quarter | a half |
| Electric types | can be paralysed | can't |
| A sleep has | four lengths | three |
| A confused Pokémon hurts itself | 1 time in 2 | 1 time in 3 |
| Steel resists Ghost and Dark | yes | no |
| A move's power, accuracy, PP, priority, type | Platinum's (Tackle 35 and 95%) | the newest games' (Tackle 40 and 100%) |

Known differences that join the `Ruleset` in the session that writes their rule, never as a constant beside it:

| Rule | Platinum | Modern | Session |
| --- | --- | --- | --- |
| Weather an ability starts | lasts the battle | five turns | R3, R7 |
| Grass types and powder moves, Ghost types and trapping, Poison types and Toxic's aim | no special case | immune, free to leave, never misses | R3 |
| EXP | by the foe's level alone, shared among those who fought | scaled by the difference in level; the whole party gets some | R10 |
| Catching | Platinum's formula | critical captures, the later status bonuses | R9, R13 |
| TMs | used up | kept | R11 |
| Poison in the field | hurts every four steps, down to 1 HP | none | R13 |
| Species' types and base stats | Platinum's (Clefairy is Normal) | the newest games' (Fairy; the stat raises of Generations 6 and 7) | plan 03 · D11, then R19 |
| Abilities that were reworked (Sturdy and the like) | as in Platinum | as in the newest games | R7, R27 |

Where today's engine already differs from Platinum's own code, found while writing R1's test vectors against the decompilation (for R2 and R3 to put right; the default rules must not change under the graphics session's comparison shots, so R1 left them):

- A sleep is drawn as 1 to 4 and counted down before it is checked, so it lasts 0 to 3 turns. R3 takes the original's number from its battle scripts.
- The catch formula works in floating point with a fourth root; the original works in whole numbers with two integer square roots.
- Abilities' and items' multipliers are multiplied together as fractions and applied once; the original applies each in its own place (to the power, to the stat, to the damage) and rounds down each time.
- EXP is base × level / 7 with the trainer bonus and nothing else (no Lucky Egg, traded or international bonuses).

What matches, and is held by `BattleScenarioTests`: the base damage formula and its order (stat × power × (2 × level / 5 + 2) / defence / 50, a burn's halving, + 2, the critical multiplier, the roll in sixteen steps of 85 to 100 hundredths, then × 1.5 for the user's own type, then the target's types), stat stages, and what a critical hit ignores.

### The later mechanics in the story

The user's ruling is conditional: in Sinnoh's story if that can be balanced, otherwise after the Hall of Fame. Nothing of the four exists yet, so balance can't be measured today. What R1 records is what "balanced" has to mean, so R20–R23 and the story's chapters build toward it and R30 measures it:

- **Each arrives late, one at a time, with a boss who uses it first.** A leader or commander shows the mechanic against the player and it is handed over after that battle, as the games that introduced them did. A proposal for plan 02 to confirm chapter by chapter: Mega Evolution with Maylene's Lucario (fourth badge, S8); Z-Moves at Celestic Town's ruins, with Cynthia's grandmother (S9); Dynamax with the Distortion World (S12); Terastallization at the Pokémon League's door (S14).
- **From then on every boss has it too.** Gym Leaders after that point, Team Galactic's commanders and Cyrus, the rival, the Elite Four and Cynthia use what the player has by then, on the Pokémon it suits (Candice's Abomasnow, Lucian's Gallade, Cynthia's Garchomp have Mega forms of their own).
- **Ordinary trainers stay Platinum's.** The cost is that the player outguns them more than in Platinum. If playtests show routes turning trivial, the lever is the player's side, not theirs: the stones and crystals the story hands out stay few, and "one of each" can be tightened to "one in total" outside boss battles.
- **The check.** Each of R20–R23 ends with its mechanic fought through the story's boss teams by the AI on both sides, with and without it (the fuzzing harness of plan 06, "Verification"). A mechanic that can't be made fair to both sides moves to after the Hall of Fame, which is the ruling's own fallback and needs no new decision.
- The switches the plan foresaw (each mechanic can be turned off in the options) stay, whichever way this falls.

## Evolution (2026-10-02)

The baseline is Platinum's own code (`Pokemon_GetEvolutionTargetSpecies`, `Evolution_ProcessEvolutionEffects`); methods that came later follow the game that introduced them. `docs/mechanics/evolution.md` lists every method.

**Platinum's rules, kept as they are**

- Friendship evolutions happen at 220 (Generation 8 lowered it to 160 on a different scale). Sylveon, which asks for friendship and a Fairy move, uses the same 220.
- Night is 20:00 to 03:59; everything else is day.
- Evolution is checked after the battle, for the Pokémon that gained a level in it, never during it. Nothing evolves after a battle the player lost.
- The Everstone stops level-up and trade evolutions, not items used on the Pokémon. Kadabra evolves even holding one.
- B stops a level-up evolution; an item's or a trade's can't be stopped.
- A level-100 Pokémon can't evolve by level-up, because it can't gain a level.
- Shedinja appears only with a free place in the party and a Poké Ball in the bag, which is used up.
- The first of a species' evolutions whose condition holds is the one that happens. So a friendly Eevee beside the Moss Rock becomes Leafeon, and (from later games) one that also knows a Fairy move becomes Sylveon before Espeon or Umbreon.

**Stand-ins for what this game lacks**

| Species | The original asks for | Here | Why |
| --- | --- | --- | --- |
| Inkay | Level 30 with the console upside down | Level 30 | No console to turn |
| Finizen | Level 38 while playing with another player | Level 38 | No shared field; can be tightened once plan 07 exists |
| Tandemaus | Level 25, gained inside a battle | Level 25 | Evolution is checked after battles anyway |
| Pawmo, Bramblin, Rellor | 1000 steps walking beside the player (Let's Go), then a level | 1000 steps at the head of the party, then a level | No Pokémon walk beside the player |
| Stantler, Primeape | A move used 20 times (in Legends: Arceus, in the agile style) | The move used 20 times in battle, then a level | No battle styles |
| Bisharp | Knock out three Bisharp that hold a Leader's Crest | Be on the field when three Bisharp are knocked out, then a level | No leader Bisharp with their followers; the battle doesn't record who struck the last blow |
| Gimmighoul | 999 Gimmighoul Coins, then a level | The same: 999 in the bag, used up | |
| Meltan | 400 Meltan Candies in Pokémon GO | 400 Meltan Candies in the bag at a level-up, used up | The only method there has ever been |
| Milcery | The player spins while it holds a Sweet | The same: four quarter turns the same way | Turning takes a step here, so a tight circle counts |
| Ursaring | Peat Block under a full moon | Peat Block at night | No phases of the moon |
| Cosmoem | Solgaleo or Lunala by game version | Solgaleo by day, Lunala at night | No versions |
| (affection) | Pokémon-Amie hearts | Friendship 220 | Generation 8 folded affection into friendship |
| Sliggoo | Rain in the field | The same; battles' rain doesn't count | Done: the field's weather where the player stands (plan 04 · G9) |

**Trading**

- Trade evolutions stay trade evolutions: the game will have trading, online (plan 07) and with characters in the game (decided 2026-10-02). This replaces the idea in plan 03, decision 4, of swapping them for another method.
- A Linking Cord used on a Pokémon counts as a trade, as in Legends: Arceus. It needs the held item where the trade does, and a Shelmet in the party for Karrablast (and the other way round). Whether the game hands Linking Cords out is still open; nothing does yet.

**Not taken from later games**

- Leafeon, Glaceon, Magnezone and Probopass evolve at their places (Moss Rock, Ice Rock, a magnetic field), not with the stones Generation 8 switched to.
- Feebas evolves on Beauty, not by trade with a Prism Scale.
- A Rare Candy does nothing for a level-100 Pokémon.

## Friendship (2026-10-02)

Platinum's table and thresholds (`Pokemon_UpdateFriendship`). The +1 for being in the place the Pokémon was met is left out until Pokémon record where they were met. A traded Pokémon starts again at its species' base friendship.

## Pokédex (2026-10-04, plan 03 · D10)

**Platinum's rules, kept as they are**

- The Sinnoh Pokédex is Platinum's own 210 species in its order (`res/pokemon/sinnoh_pokedex.json` in the decompilation). Its SEEN and CAUGHT counts count those species only.
- The National Pokédex is opened after the Hall of Fame, once every species of the Sinnoh Pokédex has been seen (`Pokedex.CanUnlockNational`). The professor's scene that hands it over is the story's (plan 02, post-game).
- The Sinnoh diploma asks for all 210 species seen. The National one asks for every species caught except the mythical ones, which the games only ever gave out at events (23 of the 1025, by PokeAPI's flag).
- The search looks only among the species seen; ordering by weight or height looks only among those caught, whose size the Pokédex knows.
- The area page shows where a species lives in the grass at each time of day (morning is the table's own slots; the day and the night put their two species in slots 2 and 3; evening counts as day and late night as night), on the water and with each rod. Swarms, the Poké Radar and the species a second game in the console calls up are left out, as Platinum leaves them out.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| Who hands out a diploma | The game director in Jubilife City's Game Freak building | The Pokédex itself, the first time it is opened once complete; it can be seen again from the search panel | Jubilife City is still a hand-made map without that building; plan 01 · M5 can move the ceremony there |
| A place a script takes the player into | Shown on the town map where the place is | The Great Marsh is shown at Pastoria City, whose gate leads into it; Turnback Cave's inner rooms where the rest of the cave is | No warp leads there, so the import can't find their place by itself |
| The size page's trainer | The player's silhouette | The player's own field sprite as a silhouette, 1.4 m tall for both characters | Our own choice of height |
