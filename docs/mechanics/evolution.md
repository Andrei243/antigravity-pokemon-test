# Evolution

Every way a Pokémon evolves in the main games, and how this game runs it. The rules live in `Models/Evolution.cs` (no drawing, no input), the scene in `UI/EvolutionScreen.cs`, friendship in `Models/Friendship.cs`. `docs/mechanics/coverage.md` (generated) counts the evolutions by method; `docs/mechanics/rulings.md` records the choices made where games disagree or where the original needs something this game lacks.

Sources: Platinum's own code for everything up to Generation 4 (pret/pokeplatinum: `Pokemon_GetEvolutionTargetSpecies` and `Pokemon_UpdateFriendship` in `src/pokemon.c`, the scene and Shedinja in `src/evolution.c`, night in `src/rtc.c`, the walking bonus in `src/overlay005/field_control.c`, the faint penalty in `src/battle/battle_script.c`); PokeAPI's `pokemon_evolution.csv` for the methods of later games.

## How it works

Four things set an evolution off (`EvolutionTrigger`). Each method answers to exactly one of them.

| Trigger | When the game asks | Can B stop it? |
| --- | --- | --- |
| `LevelUp` | After a battle, for each of the player's Pokémon that gained a level in it (`BattleEngine.LeveledUp`); after a Rare Candy. Never during the battle, and not after a battle that was lost. | Yes |
| `UseItem` | An item used on a Pokémon from the bag (the bag shows ABLE or NOT ABLE on each Pokémon). | No |
| `Trade` | A Pokémon has just arrived by trade (`GameEngine.ReceiveTradedPokemon`). | No |
| `Spin` | The player turns a full circle in the field (`SpinTracker`). | No |

`Evolution.Find(pokemon, trigger, context)` returns the evolution that happens now, or null. As in Platinum, a species' evolutions are tried in the order they are listed and the first whose condition holds wins. `EvolutionContext` carries what the rules can't read off the Pokémon: the party, the bag, whether it is night, whether it rains, the special places of the map, the item used, the species traded for.

`Evolution.Evolve(pokemon, evolution, context)` carries it out: it uses up what the method uses up, changes the species, leaves Shedinja behind for Nincada, and returns the moves the new species offers.

The scene (`GameState.Evolution`) plays the queued evolutions one after the other: the notice, light gathering, the two shapes swapping faster and faster, the flash in which the species changes, the congratulations, then the new moves. `GameEngine` puts the new species in the Pokédex and fades back to wherever the evolution came from (the field, or the bag).

## What stops an evolution

- **The Everstone** (hold effect `NoEvolve`) stops level-up, trade and spin evolutions, but not an item used on the Pokémon. Kadabra evolves whatever it holds: that is how Platinum's code reads.
- **B** during the scene, for level-up evolutions only. The Pokémon tries again the next time it levels up.
- **Level 100**: a Pokémon that can't gain a level can't evolve by level-up (Platinum; later games let a Rare Candy do it).

## What evolving does

- The ability keeps its slot (first stays first, second stays second).
- A Pokémon without a nickname takes the new species' name.
- Hit points gained are added to the current ones; a fainted Pokémon stays fainted.
- The item is used up for "trade holding", "level up holding" and "spin holding" methods. The coins and candies of `LevelWithItemsInBag` leave the bag.
- What was counted toward the evolution (steps, uses of a move, knock-outs) is cleared.
- **Moves**: the new species' moves for the level the Pokémon is at (Platinum), and its "evolution moves" (level 0 in the learnsets of Generation 7 on). With a free place the move is learned; with four moves the scene asks which to forget, or to keep all four.
- **Shedinja**: when Nincada becomes Ninjask with a free place in the party and a Poké Ball in the bag, a Shedinja joins with the same level, nature, IVs, EVs and moves, and one Poké Ball is used up. Shedinja always has 1 HP.

## Every method

"First seen" is the generation that introduced the method. "Waits for" names what must exist before a player can meet the condition; the rule itself is written and tested.

### On a level-up

| Method (`EvolutionMethod`) | Needs | Examples | First seen | Waits for |
| --- | --- | --- | --- | --- |
| `Level` | The level | Turtwig 18, Starly 14 | 1 | |
| `Friendship` | Friendship 220 or more | Golbat, Chansey, Togepi, Munchlax, Buneary | 2 | |
| `FriendshipDay` | Friendship 220, by day | Eevee → Espeon, Budew, Riolu | 2 | |
| `FriendshipNight` | Friendship 220, at night | Eevee → Umbreon, Chingling, Snom | 2 | |
| `LevelAttackHigher` / `LevelAttackEqual` / `LevelDefenseHigher` | Level 20, Attack compared with Defense | Tyrogue → Hitmonlee / Hitmontop / Hitmonchan | 2 | |
| `LevelPersonalityLow` / `LevelPersonalityHigh` | Level 7; the upper half of the personality value, modulo 10, below 5 or not | Wurmple → Silcoon / Cascoon | 3 | |
| `LevelNinjask` + `LevelShedinja` | Level 20; Shedinja as described above | Nincada | 3 | |
| `Beauty` | Beauty 170 or more | Feebas | 3 | Poffins (plan 06 · R14) |
| `LevelMale` / `LevelFemale` | The level and the gender | Burmy, Combee, Salandit, Espurr | 4 | |
| `LevelHoldingItemDay` / `LevelHoldingItemNight` | Holding the item at that time | Happiny + Oval Stone; Gligar + Razor Fang, Sneasel + Razor Claw | 4 | |
| `LevelHoldingItem` | Holding the item | (none in the data yet) | 4 | |
| `LevelKnowsMove` | Knowing the move | Piloswine, Yanma, Tangela (Ancient Power), Lickitung (Rollout), Aipom (Double Hit), Bonsly, Mime Jr. (Mimic), Steenee, Clobbopus, Girafarig, Dunsparce | 4 | |
| `LevelWithSpeciesInParty` | That species elsewhere in the party | Mantyke with Remoraid | 4 | |
| `LevelAtLocation` | The map has the place: `Moss Rock`, `Ice Rock`, `Magnetic Field` | Eevee → Leafeon / Glaceon, Magneton, Nosepass | 4 | Eterna Forest, Route 217, Mt. Coronet (plan 01) |
| `LevelDay` / `LevelNight` | The level, at that time | Tyrunt, Amaura, Rockruff, Fomantis, Yungoos, Greavard, Cosmoem | 6 | |
| `LevelKnowsMoveType` (+ `needsFriendship`) | A move of that type, and friendship 220 | Eevee → Sylveon | 6 | |
| `LevelWithTypeInParty` | The level, with that type elsewhere in the party | Pancham with a Dark type | 6 | |
| `LevelInRain` | The level, while it rains in the field | Sliggoo | 6 | Weather in the field (plan 01) |
| `LevelUpsideDown` | Originally: the console held upside down. Here: the level | Inkay | 6 | |
| `Affection` | Originally Pokémon-Amie hearts; friendship from Generation 8 on. Here: friendship 220 | (none in the data: Sylveon's row is the newer one) | 6 | |
| `LevelAfterSteps` | That many steps at the head of the party | Pawmo, Bramblin, Rellor (1000) | 9 | |
| `LevelAfterMoveUses` | The move used that many times in battle | Primeape (Rage Fist ×20), Stantler (Psyshield Bash ×20) | 8 (Legends: Arceus) | |
| `LevelAfterDefeating` | That many of the species knocked out while it was on the field | Bisharp (3 Bisharp) | 9 | |
| `LevelWithItemsInBag` | That many of the item in the bag (they are used up) | Gimmighoul (999 Gimmighoul Coins), Meltan (400 Meltan Candies) | 9, GO | A way to collect them (plan 03 · D12) |

### With an item used on it

| Method | Needs | Examples | First seen |
| --- | --- | --- | --- |
| `UseItem` | The item | The stones (Fire, Water, Thunder, Leaf, Moon, Sun, Shiny, Dusk, Dawn, Ice), the apples, the armours, Cracked Pot, the scrolls, Black Augurite, Metal Alloy | 1 |
| `UseItemMale` / `UseItemFemale` | The item and the gender | Kirlia → Gallade, Snorunt → Froslass (Dawn Stone) | 4 |
| `UseItemNight` | The item, at night (a full moon in Legends: Arceus) | Ursaring + Peat Block | 8 |

### By trade

| Method | Needs | Examples | First seen | Waits for |
| --- | --- | --- | --- | --- |
| `Trade` | Being traded | Kadabra, Machoke, Graveler, Haunter, Boldore, Gurdurr, Phantump, Pumpkaboo | 1 | Trading |
| `TradeHoldingItem` | Being traded while holding the item | Onix and Scyther (Metal Coat), Seadra, Poliwhirl and Slowpoke (King's Rock), Porygon, Porygon2, Rhydon, Electabuzz, Magmar, Dusclops, Clamperl, Spritzee, Swirlix | 2 | Trading |
| `TradeWithSpecies` | Being traded for that species | Karrablast for Shelmet, and the other way round | 5 | Trading |

A **Linking Cord** used on a Pokémon counts as a trade (Legends: Arceus). It is in `items.json` and works today; nothing in the game hands one out yet. For Karrablast and Shelmet the partner has to be in the party.

### By spinning

| Method | Needs | Examples | First seen |
| --- | --- | --- | --- |
| `SpinHoldingItem` | The player turns four quarter turns the same way, each within 1.2 s of the last, while the Pokémon holds the item | Milcery with any of the seven Sweets | 8 |

## Friendship

0 to 255, starting at the species' base value (70 for most). Platinum's table, by band of current friendship:

| Event | Below 100 | 100–199 | 200 and up | Hooked up |
| --- | --- | --- | --- | --- |
| Level up (battle or Rare Candy) | +5 | +3 | +2 | Yes |
| Every 128 steps, half the time, for each Pokémon in the party | +1 | +1 | +1 | Yes |
| Fainting in battle | −1 | −1 | −1 | Yes |
| Fainting to a foe 30 or more levels above | −5 | −5 | −10 | Yes |
| Beating a Gym Leader, the Elite Four or the Champion | +3 | +2 | +1 | With the gyms (plan 02) |
| Learning a TM or HM | +1 | +1 | 0 | With TMs (plan 06 · R11) |
| Surviving poison in the field | −5 | −5 | −10 | With field poison (plan 06 · R13) |
| Winning a contest | +3 | +2 | +1 | With contests (plan 06 · R17) |

Gains are one higher for a Pokémon caught in a Luxury Ball and ×1.5 for one holding a Soothe Bell. Items with their own friendship numbers (vitamins, herbs, EV berries) go through `FriendshipRules.Change`; the bonus for being where the Pokémon was met waits for met locations (plan 07 · O2). A traded Pokémon starts over at its species' base friendship.

## Not in the data yet: regional forms

The importer leaves out evolutions that start from a regional form, because forms don't exist yet (plan 03 · D11). When they do, most use methods above (Galarian Meowth at level 28, Galarian Linoone at level 35 at night, Hisuian Sneasel holding a Razor Claw by day, Hisuian Qwilfish knowing or using Barb Barrage, Galarian Slowpoke with a Galarica Cuff or Wreath, Paldean Wooper at level 20, Alolan forms by region). Three need rules of their own:

| Species | Original rule | Planned here |
| --- | --- | --- |
| Galarian Farfetch'd → Sirfetch'd | Three critical hits in one battle | The same, counted by the battle (a new trigger at the battle's end) |
| Galarian Yamask → Runerigus | Take 49 or more damage, then walk under a stone arch | Level up at a named place after taking the damage |
| White-Striped Basculin → Basculegion | Lose 294 HP to recoil without fainting | A counter like the move-use one, then level up |

Also tied to forms rather than to the method: which Alcremie, Lycanroc, Toxtricity, Urshifu, Maushold or Dudunsparce comes out. Each of those is one evolution here.

Mega Evolution, Primal Reversion, Gigantamax and form changes are not evolutions; plan 06 · R19–R23 and R29 cover them.

## For trading

`GameEngine.ReceiveTradedPokemon(received, given)` is the one call a trade makes once the exchange is agreed, whether with a friend online (plan 07 · O5) or with a character in the game (plan 06 · R12): the received Pokémon takes the place of the one given, starts at base friendship, goes into the Pokédex, and evolves if trading is what it was waiting for. Underneath, `Evolution.Find(pokemon, EvolutionTrigger.Trade, context)` with `context.TradedFor` set is all the rule there is, so a server can ask the same question without the game.

Still to come with trading itself: the original trainer and the 1.5× EXP and obedience that hang on it (plan 07 · O2), and telling the other side that its Karrablast or Shelmet evolved.

## Checking it

- `PokemonPlatinumTests/EvolutionTests.cs`: every method, the Everstone, Shedinja, friendship, the scene, the bag, saves, and a test that every evolution in the data has a rule.
- `dotnet run --project tools/ShotHarness -- <out dir> evolution`: the scene from notice to congratulations, the move choice, a stopped evolution, the bag's choice of Pokémon, and a stone and a trade played through the game's own states (`e*`).
