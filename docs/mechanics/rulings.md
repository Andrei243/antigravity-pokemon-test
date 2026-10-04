# Rulings

Choices made where the games disagree with each other, or where a rule leans on something this game doesn't have. Plan 06 · R1 confirms the general ones (whose rules are the baseline, when the later mechanics appear); until then this file holds the rulings taken along the way. Each says what was chosen and why, so it can be changed in one place.

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
