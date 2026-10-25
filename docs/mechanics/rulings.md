# Rulings

Choices made where the games disagree with each other, or where a rule leans on something this game doesn't have. Each says what was chosen and why, so it can be changed in one place. The general ones come first; the rest are in the order they were taken.

## The general rulings (plan 06 · R1, confirmed by the user on 2026-10-04)

1. **Whose rules: the modern ones, in every game** (the user's ruling of 2026-10-12, carried out by plan 06 · R19 on 2026-10-17; it replaces the ruling of 2026-10-04, under which a new game asked on the title screen whether to play by Platinum's rules or the modern ones). There is no question after NEW GAME any more: every new game is played by the modern rules, and a game begun under Platinum's is brought over to them as it loads (`GameEngine.ApplySaveData`: its Pokémon take the modern moves', species' and learnsets' values, effort past the modern 252 in a stat is cut to it and PP past a move's modern most to that, `SavedPokemonData.ToPokemon`; its next save says `Modern`). `SaveData.Rules` is read for that alone. The options that followed the rules (the move hints, the trainers' lines in battle) take what they gave under the modern ones: they are on unless turned off, and a settings file that says "Rules" reads as on. What stays Platinum's is the content: Sinnoh's story, maps, encounter tables and trainers. Whatever came after Platinum follows the game that introduced it, in its latest form.
   - Every difference lives in `Data/Ruleset.cs`. `Ruleset.Platinum` stays in the code as the baseline the modern rules are written as differences from: the tests of R1 to R18 hold it to the decompilation, `battles-platinum.txt` keeps it from moving, and a test or a battle handed no rules of its own is fought by it. No game is played by it. Whether it goes for good is R30's question at the sign-off. Code that runs a rule which differs between the generations asks the battle's `Rules` (or `Ruleset.Current` outside a battle) for the number; it never carries a constant of its own. The table below is the list.
   - A move's own numbers follow the rules too: `moves.json` holds Platinum's values, and `modern` beside them where the newest games differ (107 of Platinum's moves). A species' and a form's types, base stats and, since R19, the moves they learn by level do the same (`species.json`'s `modern`). `Ruleset.Use` swaps them in as a game begins or loads.
   - **The moves learned by level are the newest game's** (the user's "latest moveset for each Pokémon", R19): the importer gives each of species 1 to 493 the list of the newest game it is in (by `LearnsetVersionGroups`, Scarlet and Violet first, as species 494 on already had theirs): 486 species and 12 forms learn another list than Platinum's. **Until R24 to R26 give the later moves their effects, a Pokémon learns by level only the moves that do something in a battle** (`MoveDatabase.Runs`, read by `Pokemon.Learnset`): a move whose effect is still missing but that hits (`support: Partial`) is learned and hits; a move that does nothing yet (`support: None`) is passed over, and is learned by the Pokémon that reach its level once it runs. A trainer's Pokémon whose moves the original's data gives keep them.
2. **When Mega Evolution, Z-Moves, Dynamax and Terastallization appear: each in its own region** (the user's ruling of 2026-10-12, which replaces the ruling of 2026-10-04, "during Sinnoh's story if they can be balanced there, otherwise after the Hall of Fame"): Mega Evolution in Kalos, Z-Moves in Alola, Dynamax in Galar, Terastallization in Paldea. Sinnoh's story has none of them. See "The later mechanics in the story" below.
3. **How they live together: one of each.** Each mechanic once per trainer per battle, and a Pokémon can use only one of them in a battle. A Pokémon holding a Mega Stone or a Z-Crystal can't Terastallize; a Mega or Primal Pokémon can't Dynamax.
4. **Where Dynamax works: anywhere**, once the Dynamax Band is in hand.
5. **"Every item": all of them, some only as collectables.** Every item of the main games is in the data with its name, description, pocket and price. Items of systems this game has get their real effect. The rest (curry and sandwich ingredients, TM materials, other regions' story keys: about 700) exist without a function, and the coverage report lists them.
6. **"Every move": 847 ordinary moves, 35 Z-Moves, 19 Max Moves and 33 G-Max Moves**, each with its exact effect. The games number 919 because each of the eighteen type Z-Moves has a physical and a special id; here each is one move. The 18 Shadow moves of the GameCube games are left out.
7. **Online play** is no longer out of scope: plan 07 builds it. A battle between two players is fought by one set of rules that both agree to before it starts (plan 07 · O6 decides how the room offers it).
8. **The Game Corner has slot machines**, as in the Platinum the decompilation is built from.

### What the two sets of rules say

Every game is played by the modern column (R19). The Platinum column is the baseline the code writes it against. Before taking Platinum's column out of play, R19 laid the table before the user for any rule wanted as Platinum has it; with no answer to the contrary the whole modern column is kept (the overview's default). The one to look at twice is **EXP**: the modern formula gives the whole team a share, which feeds the levels plan 18 · W4 has to solve. A rule the user wants kept as Platinum has it becomes a value of the modern preset in `Ruleset.Modern`, and nothing else changes.

In the `Ruleset` today, because the engine already runs these rules:

| Rule | Platinum (the baseline) | Modern (every game) |
| --- | --- | --- |
| A critical hit | × 2 | × 1.5 |
| Chance of one, by stage | 1 in 16, 8, 4, 3, 2 | 1 in 24, 8, 2, then always |
| Sniper | half as much again: × 3 | half as much again: × 2.25 |
| A burn takes, each turn | 1/8 of HP | 1/16 of HP |
| Paralysis cuts Speed to | a quarter | a half |
| Electric types | can be paralysed | can't |
| A sleep lasts | 1 to 4 turns | 1 to 3 |
| A confused Pokémon hurts itself | 1 time in 2 | 1 time in 3 |
| Steel resists Ghost and Dark | yes | no |
| A move's power, accuracy, PP, priority, type | Platinum's (Tackle 35 and 95%) | the newest games' (Tackle 40 and 100%) |
| Weather an ability starts | lasts the battle | five turns, eight with the weather's rock |
| Powder and spore moves on a Grass type | work | do nothing |
| A Ghost type | can be held on the field | can always leave |
| Toxic from a Poison type | can miss | can't |
| A Taunt lasts | 3 to 5 turns | 3 |
| An Encore lasts | 4 to 8 turns | 3 |
| Disable lasts | 4 to 7 turns | 5 |
| A Tailwind lasts | 3 turns | 4 |
| An uproar lasts | 3 to 6 turns | 3 |
| A binding move (Wrap, Fire Spin) hurts for | 2 to 5 turns, 5 with a Grip Claw, a sixteenth of HP each | 4 or 5 turns, 7 with a Grip Claw, an eighth each |
| Protect, Detect or Endure again at once | half as likely each time, down to an eighth | a third as likely each time, down to 1 in 729 |
| Minimize raises evasion by | one stage | two |
| A move of two to five hits lands | 2 or 3 on half the draws, then any of the four (3/8, 3/8, 1/8, 1/8) | 2, 3, 4 or 5 at 35%, 35%, 15%, 15% |
| Explosion and Self-Destruct | against half the target's Defense | against the whole of it |
| Hidden Power's power | 30 to 70 by the IVs | 60 |
| Chatter confuses | 3 times in 10 at most, by the cry recorded (no microphone here: taken as the loudest) | always |
| Knock Off against a held item | its own 20 power | half as strong again |
| The opponents' Thief, Covet, Trick and Switcheroo | fail outside the Battle Frontier | work |
| Simple | the holder's stages change as anyone's and count double wherever they are read (damage, accuracy, the turn's order) | each change to the holder's stages is doubled |
| EXP | by the foe's level alone (base × level / 7), shared among those who fought, half to Exp. Share holders | scaled by the two levels (Generation 7's formula), the whole team gets some, half for those who didn't fight, a fifth more past the level it would evolve at |
| Effort in a stat | at most 255 | at most 252 |
| A vitamin raises effort | up to 100 | up to the stat's limit |
| The level cap (plan 18 · W4) | 100 | 260 (`Ruleset.LevelCap`; "The level cap past 100", below) |
| A Pokémon at the level cap | gains no effort from battles | still gains effort |
| A shiny Pokémon | 1 in 8,192: the personality's and the trainer's halves exclusive-or'd under 8 | 1 in 4,096: under 16 |
| An Everstone in breeding (plan 06 · R15) | the mother's or Ditto's nature one time in two | the holder's nature, always, whichever parent |
| The IVs an Egg inherits | three picks that can fall on the same stat; no item changes them | three different stats, five with a Destiny Knot, a Power item's from its holder |
| The Masuda method | four more draws for a shiny personality | five more |
| The moves learned by level | Platinum's lists | the newest game's list of each species (less the moves that do nothing yet: above) |

The modern column's numbers for the rules R3 to R5 added (from the weather's length down) were checked in R19 against Pokémon Showdown at the importer's pinned commit (`data/conditions.ts` and the conditions of `data/moves.ts`), and every one holds: a burn's sixteenth, paralysis's half, a sleep of one to three turns (`random(2, 5)`), confusion's one time in three, a binding move's five or six (`random(5, 7)`, eight with a Grip Claw) and its eighth, protecting again at a third as likely each time down to 1 in 729 (`counterMax: 729`), an ability's weather of five turns (eight with its rock), Taunt's 3, Encore's 3, Disable's 5, Tailwind's 4, Uproar's 3 and Minimize's two stages.

Known differences that join the `Ruleset` in the session that writes their rule, never as a constant beside it:

| Rule | Platinum | Modern | Session |
| --- | --- | --- | --- |
| Catching | Platinum's formula | critical captures, the later status bonuses | R9, R13 |
| TMs | used up | kept | R11 |
| Poison in the field | hurts every four steps, down to 1 HP | none | R13 |
| Species' types and base stats | Platinum's (Clefairy is Normal) | the newest games' (Fairy; the stat raises of Generations 6 and 7) | plan 03 · D11 (done: `Ruleset.ModernSpeciesValues`); in play since R19, every game being played by them, which is what puts the Fairy type on Sinnoh's own species |
| Abilities that were reworked (Sturdy holding on at 1 HP, Lightning Rod and Storm Drain taking the move in, Stench's flinch, Pickup in battle) | as in Platinum (written in R7) | as in the newest games: Lightning Rod and Storm Drain since R27 ("The later abilities"), the rest R27b | R27 |

### Where the engine stands against Platinum's own code

R1 found places where the engine differed from the decompilation and left them, because the default rules were not to change under the graphics session's comparison shots. R2 (2026-10-05) rewrote the battle's arithmetic from the original's functions. These are the only changes the default rules have had since R1, and each is what Platinum does:

- **Catching** is the original's: whole numbers, two whole square roots (`BattleScript_CalcCatchShakes`), four rolls against the result, and each ball's own strength (Net, Dive, Nest, Repeat, Timer, Dusk and Quick Balls work; the battle is told whether it is night, on water or in a cave and which species have been caught). A Master Ball and a ball that can't fail make no rolls.
- **EXP** is the original's (`BtlCmd_CalcExpGain`): base × level / 7, shared among the Pokémon that fought and still stand; with an Exp. Share in the party half goes to those who fought and half to the holders; a Lucky Egg and a trainer's Pokémon each add half, one after the other, rounded down each time. Since R10 a Pokémon from another trainer adds half again ("a boosted ... EXP."); one comes only by trading (R12, plan 07 · O5).
- **Bonuses are whole-number steps**: an ability's or an item's bonus is applied by itself, in hundredths, and rounded down before the next, in one of the formula's places (to the move's power, to the stat, before the roll, to the finished damage). A Life Orb's comes before the roll, as in the original. Since R7 every ability's bonus stands in the original's own place and order, and since R8 every held item's too (below).
- **Running** follows the original (`Battler_CanEscape`): a Pokémon no slower than the foe always gets away; a slower one by its Speed × 128 / the foe's Speed + 30 for each earlier try, kept in one byte, against a roll of 256. Before R2 running always worked. Smoke Ball and Run Away always get away.
- **The turn's order** is the original's (`BattleSystem_CompareBattlerSpeed`): running first, then items and switches in the order of the places, then moves by priority, Quick Claw, Speed and a coin. Every place draws a number each turn, and a Quick Claw works when its holder's leaves nothing over five.
- **Accuracy** is the original's table of stages, then the user's ability, the target's ability, the target's item and the user's item, each rounded down in turn.
- **Struggle** has no type and can be a critical hit (before R2 it couldn't).

R3 (2026-10-05) wrote the conditions and the field from the original's turn (`battle_controller_player.c`: what can stop a move, and the three passes of a turn's end), its script commands (`battle_script.c`) and the effect scripts themselves (`res/battle/scripts`). Where that changed what the default rules already did, it is again what Platinum does:

- **A sleep** is a counter of 2 to 5 (`subscript_fall_asleep`) that drops by one each time the sleeper's turn comes; when nothing is left the Pokémon wakes and moves on that same turn. So a sleep lasts 1 to 4 turns (it was 0 to 3), and Rest's is always two.
- **Bad poison** takes a sixteenth of HP, rounded down, times its count (it was the count times HP, over sixteen: a point more now and then).
- **What follows a hit** is in the original's order: the lines about the hit, what the move does to its target, then the attacker's recoil or what it drained, and only then does a target that was knocked out go down, with Destiny Bond and Grudge answering at that moment.
- **A Flame Orb or Toxic Orb** acts after everything else of its holder's turn end, so its harm starts the turn after.
- **The end of a turn** runs in three passes, each in the original's order: what the two sides and the sky have (screens, veils and a tailwind run down; a Wish comes true; the weather goes on or ends and passes over everyone from the fastest); then each Pokémon from the fastest with everything that is its own (roots and a ring of water, its ability, its item, Leech Seed, poison, a burn, a nightmare, a curse, a binding move, Bad Dreams, an uproar, a rampage, Disable, Encore, Lock-On, Charge, Taunt, Magnet Rise, Heal Block, Embargo, Yawn, its berries, an orb); then what was sent ahead (Future Sight, a perish count) and Trick Room. Whoever is brought to nothing faints before the next thing happens.

Our own choices in R3, where the original leaves room or this game is built differently:

- **Counters count as the original's do**, each kind in its own way (some end when they reach nothing, Encore and Disable a turn end after), so the lengths in the table above are what the scripts' numbers come to in play. An uproar's is as the script draws it (3 to 6 turns).
- **Heal Block** stops the moves on the original's own list, a draining move, Leech Seed, a Wish, Ingrain and Aqua Ring. It does not stop Leftovers or an ability that heals in the weather: Platinum's doesn't.
- **Rest** is stopped by Insomnia and Vital Spirit only; Leaf Guard doesn't stop it even in the sun, as in Platinum.
- **Brick Break** breaks the screens of a target its type can't hurt (a Ghost), as the original takes care to.
- **A Jump Kick that doesn't land** costs half of what it would have done to that target, at most half the target's HP; a Protect counts as not landing; against a Ghost the damage is nothing and so is the fall.
- **Roar and Whirlwind** in a wild battle end it as a getaway (`BattleResult.PlayerRan`), whoever used them, and so does Teleport.
- **The weather of the place** comes into the battle and stays (`Weathers.InBattle`): rain of any strength is rain, snow of any strength is hail, a sandstorm and fog are themselves. No area open so far has any.
- **Text**: every line a battle says about these is our own wording.
- **Not shown yet**: a Substitute has no doll (the Pokémon stays as it is; the lines say what the Substitute took), and the weather over a battle is told in its lines but not drawn. A Pokémon that flew up or dug down is taken off its platform, its HP box left in place. The battle's drawing of both is for plan 04's follow-up, with the hooks R6 gives it.

R4 (2026-10-05) wrote the families of moves whose rule is a formula or a condition on the battle's numbers, from their script commands (`battle_script.c`), their effect scripts and the original's tables (the weights, the HP bar's pixels, Trump Card's powers, the order of the types). Where that changed what the default rules already did, it is again what Platinum does:

- **A move of several hits** draws its count as the original does (two or three on half the draws, then any of the four; Skill Link makes it five), tells each hit's critical hit as it lands and the type's effect once after the last, runs its side effect for each hit, and stops when the target is down or its user has been put to sleep. Triple Kick alone rolls each kick, and a kick that misses ends them with no miss told.
- **A status move is told by its category**, never by a power of 0: the original's table gives every move of variable power (Counter, Seismic Toss, Return, Hidden Power…) the power 1, which the importer writes as 0. So a Taunt lets Counter and Seismic Toss through, and Sucker Punch works against a Pokémon about to use them, as in Platinum; before R4 both read the 0.
- **A power worked out as nothing** (Return at no friendship, Frustration at full) falls back on the table's 1, as `BattleSystem_CalcMoveDamage` does, so the move still deals its 2.
- **Explosion and Self-Destruct** halve the target's Defense inside the damage formula (not the finished damage), are stopped by Damp anywhere on the field (Mold Breaker ignores it) with their user's HP untouched, spend their user before the hit is worked out and, with a Protect in the way, for nothing.
- **Beat Up** is one hit for each member of the party that stands with no condition (the user always), worked out from base stats and the level alone, with a roll and a critical hit of its own each, and no type at all: a Ghost takes it.
- **The one-hit knockouts** go by a roll of 100 under the accuracy plus the levels' difference, never against a higher level ("is unaffected"), never through Sturdy, and are sure after Lock-On or with No Guard. The damage is the whole of the HP, so Endure and a Focus Sash hold against them.
- **Pay Day** counts five times the user's level a use, for the player's side only, and the coins are picked up with the winnings, at most 65,535.

Our own choices in R4, where the original leaves room or this game is built differently:

- **Fury Cutter's count** resets only when its user is unlocked from the move (a miss, a flinch, paralysis, a Protect, a switch), which is where the original's code resets it; using another move in between doesn't.
- **Tri Attack's die** has three faces: 0 burns, 1 freezes, 2 paralyses. The order of the original's three subscripts wasn't checked, which only a forced roll could tell apart.
- **Acupressure** raises the user's own stat until R9 gives a double battle an ally to choose; an ally behind a Substitute would be refused.
- **Stockpile at three** says "But it failed!" where the original says nothing; **Spit Up** tells that the stockpiled effect wore off after its hits, and the Defense and Sp. Def it raised go back with it.
- **Beat Up's lines** name each member's attack, with "Foe" before an opponent's.
- **Judgment** takes the type of the plate held; Arceus itself stays Normal until Multitype gives it the plate's form (R7), so a plated Judgment has no bonus for the user's own type yet.
- **Explosion's user** goes down after the Pokémon it took with it.
- **Text**: every line is our own wording again.

R5 (2026-10-05) wrote the moves of their own from their commands (`battle_script.c`), their effect scripts and subscripts, the lists they go by (`sCannotMetronomeMoves` and its like) and the controller's bouncing and snatching of a move. Where that changed what the default rules already did, it is again what Platinum does:

- **A move called by another** (Metronome, Sleep Talk, Assist, Copycat, Me First, Mirror Move) costs no PP and is told with its own line, and the move chosen is what Encore, Disable, Mimic and Copycat go by afterwards: a Copycat after a Metronome fails, as the original's `movePrev` holds the caller.
- **What a move changes for the battle alone** (a Transform, a Mimic, a copied or swapped ability, Power Trick's swap) is undone when the Pokémon leaves the field, and a knocked-off item is back in its holder's hands when the battle ends; what Thief, Covet, Trick and Switcheroo move is moved for good, and Sketch is for good.
- **The opponents' Pokémon can't take the player's items**: Thief, Covet, Trick and Switcheroo fail for them outside the Battle Frontier and link battles (`BtlCmd_TryStealItem`, `BtlCmd_TrySwapItems`); by the modern rules they work.
- **Sticky Hold** keeps its holder's item against all four and against Knock Off, Pluck and Bug Bite, with a line, unless the user's ability breaks through; Arceus's plate and Giratina's orb can't be taken, swapped, thrown or knocked off, and mail can't change hands.
- **Magic Coat and Snatch** fail for the last Pokémon to act in a turn; the fastest snatcher gets a snatched move and uses it on itself; a bounced move goes back at whoever used it, with no second "used" line.
- **Follow Me** draws every move of the other side aimed at one Pokémon, whoever it was aimed at; Helping Hand needs an ally that is still to act and nobody helping already.
- **Chatter** confuses on a roll of 100 at or under its chance, so 31 times in 100 at the loudest.
- **A thrown or plucked berry** does what the item table says for it, to whoever eats it: HP, a cure, PP, a stat.

Our own choices in R5, where the original leaves room or this game is built differently:

- **Chatter's chance** is the loudest recording's, 30: this game has no microphone. The original gives a Chatot with no recording nothing.
- **A transformed Pokémon keeps its name** in every line while it wears another's shape (a Ditto with no nickname is "Ditto" still); the original shows the party's nickname throughout for the same reason.
- **Recycle's memory** of an item used up or thrown goes with the Pokémon and is lost when it leaves the field; the original keeps it by place, so a replacement could find what its predecessor used.
- **Power Trick isn't passed by Baton Pass** (the original passes its flag, with the stats of the one that comes in unswapped).
- **Mirror Move** copies the last move aimed at its user; in a double battle the original draws among the last of each attacker.
- **Helping Hand in a single battle** says there is no target, where the original says it failed.
- **A plucked or thrown berry's disliked flavour** confuses whoever ate it, and a plucked Micle Berry sharpens its next move, since R8; so does a Custap Berry or Quick Claw that went off this turn keep its holder's item until its move begins.
- **Text**: every line is our own wording again.

R6 (2026-10-05) wrote the three moves of the battlefield from the original's tables of the ground (`include/data/terrain`) and the way it picks a battle's ground from the field (`CalcTerrain`):

- **The ground** is the tile underfoot first (ice; tall grass; sand; snow of any depth; the marsh's mud; a cave floor; water that can be surfed), then the area's battle background (a route is plain, a forest grass, a town a building, a mountain route a mountain, a cave a cave), and the League's rooms, the Distortion World and the Battle Frontier are one "special" ground. A puddle and a bridge are in the tables but nothing in Platinum picks them, and nothing does here.
- **Camouflage** fails for Arceus and for a Pokémon already of the ground's type; **Nature Power** says what it turned into and uses that move as one called by another; **Secret Power**'s effect is the ground's, three times in ten, held off by Shield Dust and a Substitute like any side effect.

Our own choices in R6:

- **A hand-made map with no battle background** goes by its stage: the hand-made Jubilife City is open land where the original's city is a building, until the city is imported with its header.
- **Secret Power's flinch** asks, as every flinch here does, that the target hasn't moved yet this turn.

R7 (2026-10-05) gave every one of Platinum's 123 abilities its code, from the original's damage function (`BattleSystem_CalcMoveDamage`), its check of what acts as a Pokémon comes in (`BattleSystem_TriggerEffectOnSwitch`), its comparison of Speed, its answers to a hit (`BattleSystem_TriggerAbilityOnHit`), its change of shapes (`BattleSystem_TriggerFormChange`), the end of a battle won (`BtlCmd_GenerateEndOfBattleItem`) and, for the field, `src/overlay006/wild_encounters.c`. Where that changed what the default rules already did, it is again what Platinum does:

- **Every ability's bonus is in the formula's own place**, in the original's order, each step rounded down: the move's own multiplier (with Reckless's 12 tenths for a move with recoil or a crash), Charge, an ally's Helping Hand, Technician (60 or less, never Struggle), Huge Power and Pure Power, Slow Start, the held items, Thick Fat (the power halved, not the damage), Hustle, Guts, Marvel Scale, Plus and Minus, Mud Sport and Water Sport, the four abilities of a pinch, Heatproof, Dry Skin's weakness to Fire (the power, a quarter more), Simple, Unaware, Rivalry, Iron Fist, Solar Power, a sandstorm's Rock types, Flower Gift for its whole side, Explosion's half Defense. A defender's ability counts for nothing against Mold Breaker. Before R7 Thick Fat, Heatproof and Dry Skin changed the finished damage, and Technician read the move's power before its own multiplier.
- **Helping Hand is a step of its own** after Charge and before Technician (it was folded into the move's multiplier, which rounds differently beside Reckless); on a Future Sight it counts twice, in the power and once more on the damage set aside, as the original does.
- **A confused Pokémon's hit on itself is worked out as Struggle** (`CALC_SELF_HIT`), whatever move was chosen: no Technician, no Iron Fist, no Life Orb, no weather and no screens; the holder's Huge Power, Hustle, Guts and a held item's boost for Normal moves do count, and Rivalry, finding its own gender across from it, makes the hit a quarter stronger.
- **Simple** leaves the stages as they are and counts them double where they are read: the damage formula, the accuracy check and the turn's order (it doubled each change, which is the newest games' rule and is now the modern one).
- **What acts as a Pokémon comes in goes phase by phase**, and only within a phase by Speed: the place's weather, Trace, the weather abilities, Intimidate, Download, Anticipation, Forewarn, Frisk, Slow Start, Mold Breaker, Pressure, the shapes, the held item. Each acts once per stay on the field. The check runs as Pokémon come in, after every action and as every turn begins, so an ability taken by Trace or by a Transform acts then (a Ditto that takes on Intimidate cuts the foe's Attack), and Slow Start's five turns end at a turn's start.
- **Speed** is worked out in the original's order: the stage (doubled for Simple), Swift Swim and Chlorophyll, the held item, Quick Feet with a condition or else paralysis, Slow Start, Unburden once the item it came in with is gone, a tailwind. **Stall** goes after everyone of its priority (two with Stall: the faster goes after).
- **Truant** acts on the turn after it comes in and loafs every second turn from then; a loafing Truant doesn't tighten its focus for Focus Punch or catch a leaving foe with Pursuit.
- **Normalize** makes every move its user makes Normal, Hidden Power, Judgment, Weather Ball and Natural Gift included, a Pursuit that catches its target and a move called by another too. What Mirror Move copies from it and what Magic Coat sends back are the move as it is known.
- **Lightning Rod and Storm Drain** draw an Electric or a Water move aimed at one Pokémon to the fastest holder that isn't its user, and the holder takes it like anyone: Platinum's give no immunity. Not on the turn a move gets ready, not from a user with Normalize or Mold Breaker, not past a Follow Me.
- **Anticipation** shudders at a damaging move that would be super effective, or a one-hit knockout from a foe of its level or more, never at a move that can't touch it, a counter or a move of fixed damage; a move counts by the type its data gives it. **Forewarn** names the foes' strongest move, a move of no fixed power counting as 80, a counter as 120 and a one-hit knockout as 150. **Frisk** names a foe's item. **Trace** never takes Forecast, Trace or Multitype.
- **Color Change** takes the type of the damaging move that hit it while it stands; **Aftermath** takes a quarter from whoever knocks its holder out by touch, unless anyone has Damp; **Cute Charm** infatuates whoever touches it three times in ten, by Attract's own rules.
- **Poison Heal** gives an eighth back in place of the poison's damage and does nothing at full HP; **Gluttony** eats a berry of a pinch at half its holder's HP; **Klutz** makes its holder's item nothing to it (no effect, nothing to throw, no gift, and nothing from a berry it plucks).
- **Shapes**: Castform with Forecast takes the sun's, the rain's or the hail's shape and type and its own back under any other sky or with Cloud Nine about; Cherrim blooms in the sun whatever its ability is (the original asks only the species), and Flower Gift's half again of Attack and Sp. Def is for whoever has the ability, in the sun; Arceus with Multitype is the type of the plate it holds, read from the item itself every time, so a Ditto in its shape goes by what Ditto holds.
- **An Embargo** doesn't take hold of Arceus or of the holder of Giratina's orb.
- **After a battle won**, each of the player's Pokémon with Pickup and no item finds one, one time in ten, drawn from the row of its level in Platinum's table; each with Honey Gather finds Honey five times in a hundred for every ten levels. Nothing is said of either, as in the original.
- **In the field** the ability of the Pokémon at the head of the party, fainted or not, shapes the wild Pokémon met: Arena Trap, No Guard and Illuminate double the place's rate; White Smoke, Quick Feet and Stench halve it, Sand Veil in a sandstorm and Snow Cloak in the snow; Magnet Pull and Static draw a Steel or an Electric type out of the table one time in two; Hustle, Vital Spirit and Pressure meet the highest level one time in two; Keen Eye and Intimidate keep away, one time in two, a Pokémon five levels or more below a lead above level 5; Synchronize gives its own nature one time in two; Cute Charm the other gender two times in three.

Our own choices in R7, where the original leaves room or this game is built differently:

- **Bugs of the original that are kept**, because they are how Platinum plays: Magnet Pull does nothing on the water (the code overwrites what it found), and Sticky Hold and Suction Cups do nothing for fishing (the doubling is computed and thrown away; there is no fishing here yet, R13).
- **Arceus's shape after a battle**: a plate held makes Arceus take its shape as a battle begins, with a line the first time, and it keeps the shape afterwards. In the original the shape is set the moment the plate is given, in the bag; giving items there is R11's, which will set it at once.
- **A Pokémon with no gender given Cute Charm** (which no species has) chooses nothing in the field, where the original stops with an error.
- **Left for the session whose rule it is**: Compound Eyes making wild Pokémon hold items more often (wild Pokémon hold nothing yet: R13); Flame Body and Magma Armor hatching eggs sooner (R15; *done, 2026-10-10*); Gluttony for the berries that aren't in the engine yet (Lansat, Starf, Micle, Custap), an Embargo failing against a Quick Claw or Custap Berry that already went off, and Giratina's Origin Forme going back without its orb (R8); the reworked abilities of later generations (R27).
- **Text**: every line is our own wording ("can't get going yet!", "finally got going!", "took the attack!", "shuddered!", "breaks the mold!").

One thing R7 found in the battle's face, not in the rules: a choice handed over while lines were still being shown copied the screen's stale state over the rules' (only a tool could do it; the game's menus open after the last line). The face now never takes the screen's state while it is behind.

Still different, each waiting for the session whose rule it is:

- Obedience is not in the turn yet (R10).

R8 (2026-10-05) gave every one of Platinum's 158 held items its code, by the item table's hold effect rather than by name, from the original's block of items in the damage function, its comparison of Speed, its three trigger functions for held items (`BattleSystem_TriggerHeldItem`, `_TriggerLeftovers`, `_TriggerDetrimentalHeldItem`), its answers to a hit (`_TriggerHeldItemOnHit`, `_OnPivotMove`), the accuracy check, the subscripts for the berries against a type and the Quick Claw, and the bag's command (`BtlCmd_UseBagItem`). Where that changed what the engine did, it is again what Platinum does:

- **Every held item's bonus is in the formula's own place**, in the original's block (after Slow Start, before Thick Fat) and order: a type's item and the plates (a fifth more), Choice Band and Specs (half more to the stat), Soul Dew (half more to a Latios's or Latias's Sp. Atk and Sp. Def), Deep Sea Tooth and Scale, Light Ball, Metal Powder, Thick Club (twice), the Adamant, Lustrous and Griseous Orbs (a fifth more to the holder's two types; Giratina's not when transformed), Muscle Band and Wise Glasses (a tenth more). The Life Orb and the Metronome come before the roll, the Expert Belt after Filter and Solid Rock. A species' item goes by the species the Pokémon is in battle, so a Ditto transformed into Pikachu gets a Light Ball's doubling, as in the original.
- **Speed's items** come after the weather abilities: the Macho Brace, the Iron Ball and the Power items halve it, read from the item itself (neither an Embargo nor Klutz undoes it, as the original reads them), the Choice Scarf adds half, a Quick Powder doubles a Ditto's. **The order** within a priority: a Quick Claw or Custap Berry that went off goes first (two: by Speed alone, Trick Room or not), then a Lagging Tail or Full Incense goes last (two: the faster goes after), then Stall, then Speed. A Quick Claw goes off when the number its place drew for the turn leaves nothing over five; a Custap Berry at a quarter of its holder's HP (half with Gluttony) and is eaten as the move begins.
- **A Quick Claw says nothing**: the original plays its animation and prints no line; only the Custap Berry says "let it move first!", and not when its holder is the last to act anyway. Until its move begins an item that went off is kept safe from an Embargo, Knock Off, Trick and Switcheroo (`Volatiles.ItemWentOff`, the original's `quickClaw` and `custapBerry` flags), which in a single battle only a move of higher priority could reach.
- **The berries and herbs** are eaten at the original's moments: as a Pokémon comes in, after a hit it took, after every action for everyone (a White Herb right after the Growl), and twice at a turn's end (a berry after the ability, Leftovers and Black Sludge after it). Oran and Berry Juice at half HP (10 and 20 back), Sitrus a quarter of its holder's HP, the five flavour berries an eighth (and a nature that dislikes the flavour, the one whose lowered stat it stands for, is confused by it), the pinch berries at a quarter (half with Gluttony) for one stage while the stat can still rise, Lansat the focus, Starf two stages of one of the five stats that can still rise, drawn by lot, Micle a fifth more accuracy for one move, Leppa ten PP to the first move with none, White Herb every lowered stage back, Mental Herb the end of a love.
- **The berries against a type** halve the finished damage as the hit lands, when it is super effective and of their type (the Chilan Berry any Normal hit, Normal under Normalize counting), never a one-hit knockout or a hit of a fixed amount, never a hit a Substitute took; the berry is eaten.
- **The items that answer a hit**, after the target's ability: a Sticky Barb moves to an attacker that touched its holder holding nothing (not Knock Off, not U-turn on its way out); a Jaboca Berry costs a physical attacker an eighth of its HP and a Rowap Berry a special one, unless Magic Guard; an Enigma Berry gives a quarter back after a super-effective hit. Shell Bell gives an eighth of the damage dealt; the Life Orb costs a tenth after a damaging move that hit, unless Magic Guard. King's Rock and Razor Fang flinch one time in ten with a move the table marks for it.
- **Destiny Knot**: whoever made its holder fall in love (Attract, Cute Charm) falls in love with it, by Attract's own rules. **Amulet Coin and Luck Incense** double a trainer battle's prize once anyone on the field has held one, read from the item itself as the original does. **Cleanse Tag and Pure Incense** on the lead cut the encounter rate to two thirds, after the ability's say.
- **The bag in battle** runs the item table's own parameters: HP (a Hyper Potion's 200, kept as a signed byte in the original's table and read right now), a condition, confusion (Yellow Flute too), infatuation (Red Flute), a Revive on the bench, Ether and Elixir, the X items, Dire Hit, Guard Spec. (five turns of Mist), a Poké Doll or Fluffy Tail out of a wild battle. What would do nothing is refused with "It won't have any effect!", as the bag's ABLE and NOT ABLE have it.

Our own choices in R8:

- **A Metronome's count** forgives a move that landed nowhere (the original decrements on its own flag for a failed move); and the count is kept for every move the holder uses, as the original's `metronomeMove` is.
- **The refusal for an item the battle can't use** ("There's a time and place for everything, but not now.") is our own line; the original's bag doesn't offer such an item in battle at all. The battle's BAG menu is still the four-item shortcut; R11 builds the bag's screen, with the berries used from it.
- **Left for the session whose rule it is**: the EV items' EVs and the use items' friendship (R10); the bag's screen in battle (R11); wild Pokémon holding items (R13); the later games' items (R28).

R9 (2026-10-06) wrote Platinum's trainer AI from the original's own (`src/battle/trainer_ai/trainer_ai.c` and the 8,108 lines of its script, `script.s`), the battles that play by rules of their own from the controller and the field's battle setups (`battle_controller_player.c`, `battle_display.c`, `field_battle_data_transfer.c`), and the trainers themselves from `res/trainers/data` and `TrainerData_BuildParty`. Where that changed what the engine did, it is again what Platinum does:

- **The AI scores every move** (`TrainerAI_Init`, `TrainerAI_EvalMoves`): 100 to start, 0 for a move that can't be chosen or has no PP, a damage roll of 100 minus 0 to 15 for each, then each routine its trainer's data names (Basic, EvalAttack, Expert, SetupFirstTurn, Risky, PrioritizeExtremes, BatonPass, CheckHp, Weather, Harassment; TagStrategy added in any double battle) adds or takes away, routine by routine in the order of their bits. The highest score is used, a tie broken by chance. In a double battle each move is scored against each of the other three, a move on the partner counting only at 100 or more, and the best of the four used (`TrainerAI_MainDoubles`).
- **What the AI knows** is what the original's does: the moves it has seen each Pokémon use, an ability or an item once a line has shown it, and otherwise a guess from the species (an even chance between two abilities). Its arithmetic is the original's, the types' multipliers as 40ths (an immunity 0, neutral 40, the user's own type 60 left as it is), and every roll it makes is the battle's (`RollKind.AiChoice`), so a battle with a trainer still replays.
- **Switching, items and the Pokémon sent in after a faint** are the original's (`TrainerAI_ShouldSwitch` and its six checks, `TrainerAI_ShouldUseItem`, `BattleAI_PostKOSwitchIn`). A trainer's items are the up to four its data gives; one further down the list is looked at only once few enough of the trainer's Pokémon are left (the original's count of the living against the items).
- **A trainer's team** is built as the original builds it: the personality from the generator seeded with the IV scale, the level, the species and the trainer's id, run on by its class, with the class's gender in its low byte; the IVs all the scale's share of 31; the nature, gender and ability from the personality; never shiny. The prize money is the last Pokémon's level × 4 × the class's multiplier, doubled in a double battle.
- **Wild Pokémon** use a move drawn at random from those they can use, as the original's do.

Platinum's quirks, kept because they are what the game does:

- **Basic asks for Levitate twice** where the second was meant to be Dry Skin, so Dry Skin never marks a Water move down. **EvaDown2 and AccDown2** are checked with each other's routine.
- **Thunder's own check in Expert** is never reached (its dispatch goes elsewhere first); **Magnitude** compares the ability loaded last by the check before it.
- **Weather** (the flag) gives its +5 to a move that sets weather the field hasn't got; any other move falls through to the sun's check, so on the first turn every move but a sunny day gets it alike.
- **The item check doesn't stop at the first item it would use**: every item after it that passes the count's gate is spent too, and the last of them is the one used (`TrainerAI_ShouldUseItem` sets `result` and never breaks).
- **The Pokémon sent in after a faint** is scored in a byte that wraps round, as the original's is; **Perish Song's switch** never fires (the original compares the wrong value).
- **The Great Marsh's bait and mud**: bait raises the catch stage and, nine times in ten, the escape stage; mud lowers the escape stage and, nine times in ten, the catch stage; the Pokémon runs when a roll of 255 is at or under its species' flee rate taken at the escape stage. The player acts first.

The battles of their own (`BattleKind`):

- **A roamer** runs at its first chance (`RoamingPokemon_Main`) unless bound, held by Mean Look, or held by Shadow Tag or by Arena Trap (Levitate floats above it).
- **The catching lesson** (`FieldBattleDTO_NewCatchingTutorial`): the assistant's own starter at level 5 against a level-2 Bidoof, twenty Poké Balls, nothing chosen by the player; no critical hit, no move misses, and a ball that can't fail. The original's screens choose the assistant's first move and then the ball; here the assistant uses one move and then throws (the AI's `CatchTutorial` routine would throw at a fifth of the HP). The player's Pokédex isn't told of the Bidoof, and the catch is the assistant's.
- **The first battle** (the rival's on Route 201, `BATTLE_STATUS_FIRST_BATTLE`) has no critical hits.
- **A battle that can't be run from** (the story's legendaries) refuses RUN.
- **Pal Park**: a Park Ball can't fail and the player acts first, so the Pokémon never acts. It is thrown as a Safari Ball and said as a Park Ball, since Platinum has no Park Ball item. The show's score is the original's (`catching_show.c`): the six species' points, 200 for each catch that shares no type with the one before, 50 for each type caught, two for each second under a thousand.
- **A tag battle** (a partner at the player's side, `BATTLE_TYPE_TRAINER_WITH_AI_PARTNER`): the partner chooses with its own data's flags and never uses items; the player loses when their own team is down, whatever the partner has left.

Our own choices in R9:

- **The lines** of the Great Marsh, of a wild Pokémon running and of the lesson are our own, on the original's beats.
- **What the AI reads from the log**: an ability or an item counts as shown once a line names it on the Pokémon, which is how the original's flags are set by its messages.
- **Left for the session whose rule it is**: Pal Park as a place, with its timer (R16), and the Great Marsh's daily Pokémon (R13; the marsh itself, its steps and its balls came with plan 01 · M7); roamers moving over the map (R13); two trainers in the field spotting the player at once (plan 02); the partner drawn as a trainer in battle and healing the team between battles (a script's `heal`, plan 02's chapters); the Battle Frontier's own AI uses (R18).

What matches, and is held by `FormulaTests`, `BattleScenarioTests` and `BattleCoreTests`: the damage formula and its order (stat × power × (2 × level / 5 + 2) / defence / 50, a burn's halving, + 2, the critical multiplier, a Life Orb, the roll taking 0 to 15 hundredths off, then × 1.5 for the user's own type, then each of the target's types, and never less than 1 for a hit that lands), stat stages, what a critical hit ignores, and everything in the list above.

**A battle's chance is Platinum's too.** Every battle rolls on Platinum's own generator (`Battle/Sim/BattleRandom.cs`), so a seed and the choices made are the whole battle: `BattleCore.Record` is that pair and `Replay` plays it again to the same log. Our own choice, where the original has nothing to follow: the seed of a battle in the game is drawn from the game's dice as the battle begins.

### The later mechanics in the story

Rewritten by plan 06 · R20 (2026-10-17) to the user's answer of 2026-10-12, which replaced the conditional ruling of 2026-10-04 ("in Sinnoh's story if that can be balanced, otherwise after the Hall of Fame") and the proposal R1 wrote under it (a boss showing each mechanic in S8, S9, S12 and S14, and a balance check of Sinnoh's bosses at the end of each of R20–R23).

- **Each is handed over in its own region**, as the games that introduced them did: Mega Evolution in Kalos (the Key Stone), Z-Moves in Alola (the Z-Ring), Dynamax in Galar (the Dynamax Band), Terastallization in Paldea (the Tera Orb). The region's chapter in plan 23 writes the beat that hands the item over and gives that region's trainers theirs.
- **Sinnoh's story has none of them.** No beat is written into S8, S9, S12 or S14, Sinnoh's trainers have no Key Stone (or ring, band, orb), and Sinnoh's story owes no balance check: there is nothing of the four in it to balance. A player who comes back to Sinnoh with an item from a later region uses it there like anywhere else (one of each, ruling 3).
- **What R20–R23 build is the rules and the AI's use of them**, behind the `Ruleset` (`Ruleset.MegaEvolution` for R20, `Ruleset.ZMoves` for R21, `Ruleset.Dynamax` for R22, `Ruleset.Terastallization` for R23; under `Ruleset.Platinum` none of it is reached, so `battles-platinum.txt` never moves for them), with the gating item in the data and nothing in the game giving it yet. A trainer who has the mechanic says so in its own data (`Trainer.KeyStone`, `keyStone` in a trainer's record; `Trainer.ZRing`, `zRing`; `Trainer.DynamaxBand`, `dynamaxBand`; `Trainer.TeraOrb`, `teraOrb`); the player has it while the item is in the bag.
- **The trainer AI uses each whenever it can**, as Pokémon Showdown's random player does with the option at 1; using them *sensibly* (keeping a Mega for the right Pokémon, a Z-Move for the right moment) is R30's.
- The switches the plan foresaw (each mechanic can be turned off in the options) are R30's, with the other options for the rules.

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
| Own Tempo Rockruff | Level 25 at dusk, 17:00 to 17:59 | Level 25 in Platinum's evening, 17:00 to 19:59 | Platinum's clock has no dusk of its own; its evening is the nearest of its five times of day |
| Galarian Farfetch'd | Three critical hits in one battle | The same, evolving as the battle ends (not after one that was lost); B can stop it, as it can a level-up | Evolutions wait for the battle's end here anyway |
| Galarian Yamask | 49 HP lost to moves without fainting, then walking under the Dusty Bowl's stone arch | The same damage, counted across battles until it faints, then a level-up on a map with the Stone Arch | No such arch in Sinnoh yet: plan 03 · D12's zone for the later generations can have one, named so in its map |
| White-Striped Basculin | 294 HP lost to its own recoil without fainting, then (Legends: Arceus) a level | The same, counted across battles until it faints | |

**Trading**

- Trade evolutions stay trade evolutions: the game will have trading, online (plan 07) and with characters in the game (decided 2026-10-02). This replaces the idea in plan 03, decision 4, of swapping them for another method.
- A Linking Cord used on a Pokémon counts as a trade, as in Legends: Arceus. It needs the held item where the trade does, and a Shelmet in the party for Karrablast (and the other way round). Whether the game hands Linking Cords out is still open; nothing does yet.

**Forms** (plan 03 · D11)

- Platinum's own forms (Rotom's appliances, Giratina's Origin Forme, Shaymin's Sky Forme, Deoxys's formes, Wormadam's cloaks) have the decompilation's values; where the newest games changed them (Rotom's appliances took a second type in Generation 5) those are their modern values, which a game played by the modern rules uses. Forms that came later have their newest game's values.
- A form evolves by its own evolutions; a regional form by nothing else, and any other form also by its species' evolutions into species its form has none into (a sandy Burmy that is male still becomes Mothim).
- An evolution into another region's form (Pikachu into an Alolan Raichu) happens only in that region, as in Sun and Moon. None of those regions is built yet, so in the game they become the species' own forms.
- A female of a species whose females are a form of their own (Meowstic, Indeedee, Oinkologne, Basculegion, Pyroar, Frillish, Jellicent) is in that form from the moment she is met or evolves.

**Not taken from later games**

- Leafeon, Glaceon, Magnezone and Probopass evolve at their places (Moss Rock, Ice Rock, a magnetic field), not with the stones Generation 8 switched to.
- Feebas evolves on Beauty, not by trade with a Prism Scale.
- A Rare Candy does nothing for a level-100 Pokémon.

## Friendship (2026-10-02)

Platinum's table and thresholds (`Pokemon_UpdateFriendship`). The +1 for being in the place the Pokémon was met is left out until Pokémon record where they were met. A traded Pokémon starts again at its species' base friendship.

## Pokédex (2026-10-04, plan 03 · D10)

**Regional Pokédexes (2026-10-17, plan 18 · W3).** A Pokédex is a PokeAPI identifier, not a mode: `Pokedex.Entries("kanto")` lists the species whose `dexNumbers` have it, in that Pokédex's own order, and `"national"` is the National one. Seen and caught stay by National number and are shared, so a species seen in Sinnoh counts in Kanto's listing if Kanto lists it, and each listing counts only its own species. The player has the regional Pokédex of every region they have entered (Sinnoh's from the start; `Pokedex.Enter`, called when the Pokédex is opened and when the game travels, saved as `SaveData.DexesEntered`), then the National one once it is open; the search panel's first row switches between them. The AREA page draws the map and the wild Pokémon of the listing's own region, and for the National listing of the region the player stands in. A region with no habitats file shows "Area unknown." for every species. What opens the National Pokédex is the region's own rule (`NationalUnlock`): Sinnoh's is Platinum's, Kanto's is sixty caught (FireRed and LeafGreen ask the same of the player at Oak's aide), and any other region's the Hall of Fame alone until its plan says otherwise. A regional Pokédex's diploma asks every species seen and the National one every species but the mythical ones caught, as before; a diploma is saved under `Sinnoh`, `National` or the Pokédex's identifier. The Trainer Card's regional score (`TrainerScore.CaughtRegional`) is still Platinum's own Sinnoh number, since the Card is Sinnoh's until plan 18 · W2.

**Platinum's rules, kept as they are**

- The Sinnoh Pokédex is Platinum's own 210 species in its order (`res/pokemon/sinnoh_pokedex.json` in the decompilation). Its SEEN and CAUGHT counts count those species only.
- The National Pokédex is opened after the Hall of Fame, once every species of the Sinnoh Pokédex has been seen (`Pokedex.CanUnlockNational`). The professor's scene that hands it over is the story's (plan 02, post-game).
- The Sinnoh diploma asks for all 210 species seen. The National one asks for every species caught except the mythical ones, which the games only ever gave out at events (23 of the 1025, by PokeAPI's flag).
- The search looks only among the species seen; ordering by weight or height looks only among those caught, whose size the Pokédex knows.
- The area page shows where a species lives in the grass at each time of day (morning is the table's own slots; the day and the night put their two species in slots 2 and 3; evening counts as day and late night as night), on the water and with each rod. Swarms, the Poké Radar and the species a second game in the console calls up are left out, as Platinum leaves them out.

- **The looks seen** (2026-10-21, plan 08 · P19, after `src/pokedex.c`'s `Pokedex_Encounter`): the Pokédex records every look of every species the player has seen, in the order first seen: a look is the Pokémon's model name (its form's Showdown name, or the species') and its gender. The original records the genders, Unown's letters, Shellos's and Gastrodon's seas, Burmy's cloaks, Deoxys's and Rotom's formes and the like for the nine species whose forms Platinum keeps; this game records them for every form, so the FORMS page has the same to show for a Meowth of Alola or a Mega Charizard. A look is recorded from the first sighting (a wild Pokémon or a trainer's as it is sent out, a catch, a gift, a hatching, an evolution and the Shedinja it leaves, a trade, a rival in a contest), whatever the Pokédex can show yet, never from the catching lesson or the Battle Frontier, which tell the Pokédex nothing. The first Spinda met gives the personality its spots are drawn from. A save made before looks were kept has them rebuilt as it loads: the Pokémon the player has (team, boxes, Day Care) give theirs, and every other species seen has its own form, no gender known.
- **The FORMS page** appears once the Pokédex has been upgraded (`FLAG_ENABLED_POKEDEX_FORM_DETECTION`, the original's `canDetectForms`), which Professor Rowan's assistant does in the gate between Route 218 and Canalave City (`Route218GateToCanalaveCity.txt`, its trigger or a word with him), as in the original.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| A female's look | A sprite of her own for the species that differ | The species' sculpt, with the gender sign beside it | Plan 09 · L8 brings the female looks; the page needs no change when it does |
| Who hands out a diploma | The game director in Jubilife City's Game Freak building | The Pokédex itself, the first time it is opened once complete; it can be seen again from the search panel | Jubilife City is still a hand-made map without that building; plan 01 · M5 can move the ceremony there |
| A place a script takes the player into | Shown on the town map where the place is | The Great Marsh is shown at Pastoria City, whose gate leads into it; Turnback Cave's inner rooms where the rest of the cave is | No warp leads there, so the import can't find their place by itself |
| The size page's trainer | The player's silhouette | The player's own field sprite as a silhouette, 1.4 m tall for both characters | Our own choice of height |

## Field moves and key items (2026-10-06, plan 02 · S2)

**Platinum's rules, kept as they are** (`src/field_move_tasks.c`, `src/overlay005/`, `src/item_use_functions.c`)

- The badge each move asks for outside battle is Platinum's: Coal for Rock Smash, Forest for Cut, Cobble for Fly, Fen for Surf, Relic for Defog, Mine for Strength, Icicle for Rock Climb, Beacon for Waterfall. Flash, Teleport, Dig, Sweet Scent, Soft-Boiled and Milk Drink ask for none.
- Where a move can be used: Cut, Rock Smash (not from the water) and Strength facing their obstacle; Surf facing water one can surf on, on foot; Waterfall facing a waterfall from the water; Rock Climb facing a rock face along its grain; Flash in a dark place not lit yet; Defog in fog; Fly where the place's header allows flying, and Teleport there too but not in a town; Dig where it allows an Escape Rope and a way out is known; Sweet Scent anywhere. The party menu says why a move can't be used, and lists a Pokémon's field moves in the order of its moves.
- Strength lasts until the player leaves the place; Flash and Defog until the player goes somewhere that isn't a cave. Leaving a place clears its local flags, so a tree that was cut and a rock that was smashed are back, and a boulder that was pushed stands where it stood.
- A boulder is pushed only where it could itself step: no water, ledge, cliff, warp or anyone in the way. It takes the original's slow walk (16 frames).
- Going down a waterfall needs a Pokémon that knows Waterfall and no badge, and asks nothing; going up asks.
- Soft-Boiled and Milk Drink give a fifth of the user's maximum HP (no more than the other is missing), and can't be used by a Pokémon with that or less left, on itself, or on one fainted or at full HP.
- Dig and an Escape Rope lead to where the player went into the caves (the tile outside the way in, one further south for someone who walked in northward); Teleport to the town of the Pokémon Center last gone into, Twinleaf Town before any.
- Fly goes to the original's twenty fly spots, each opened by arriving in its town for the first time (the Pokémon League's two and Route 221's by the story).
- Fishing: the cast takes 34 frames, whether anything will bite is rolled at the cast (the rod's rate, then a slot: 60, 30, 5, 4 and 1 in a hundred for the Old Rod, 40, 40, 15, 4 and 1 for the other two), something bites after one to four seconds, and the button must be pressed within 45, 30 or 15 frames by rod. Pressed early, the line comes up empty; nothing bites at all after four seconds.
- The Bicycle can't be ridden through very tall grass, mud or the marsh's grass, nor where the place's header forbids it, and can't be got off on the Cycling Road or a bike bridge. The Cycling Road's gate keepers let only riders through, and a rider stays on the Bicycle until the next warp. The run button changes gear.
- The Pokétch's apps keep the original's numbering.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| Chatter in the field | Records the player's voice as Chatot's cry | Not in the party menu | No microphone (as Chatter's confusion, above) |
| A partner travelling with the player | Fly, Teleport, Dig and an Escape Rope are refused while Cheryl, Mira, Riley or Buck walks along | The check is there (`FieldMoveError.Partner`) and never fires | Nobody walks along yet: their chapters (plan 02 · S6, S7, S10, S15) |
| The Pokétch | Twenty-five apps on the lower touch screen | The digital watch, the pedometer and the team's app, on a watch over the field (P shows it, O changes the app) | One screen and no touch; the other apps come with what they show (the Dowsing Machine with item hunting, the Day-Care Checker with the Day Care) |
| The Bicycle | The player drawn riding it | Its pace, rules, music and sounds; the player is drawn on foot | No riding sprite yet |
| Fly's map | A cursor over the town map | A cursor over the map since plan 01 · M12 ("The Town Map", below); it was a list of the towns beside the map | Done |

## After the battle (2026-10-06, plan 06 · R10)

**Platinum's rules, kept as they are** (`src/battle/battle_script.c`, `battle_controller_player.c`, `battle_system.c`, `src/pokemon.c`, `src/item_use_pokemon.c`, `src/overlay006/wild_encounters.c`)

- **Effort** (`BattleScript_CalcEffortValues`): each Pokémon that gains EXP for a foe also gains its yield, stat by stat in the order HP, Attack, Defense, Speed, Sp. Atk, Sp. Def: a Power item's number added to its own stat, then doubled by Pokérus (caught or cured), then by the Macho Brace, then cut to 255 in the stat and 510 in all; once the total is full the rest is skipped. The effort is counted before the levels the same EXP brings, so they work it into the stats; a Pokémon that didn't level shows it at its next level, as in the original. A level-100 Pokémon gains neither EXP nor effort.
- **Vitamins and berries** (`CalculateEVUpdate`, `Pokemon_ApplyItemEffects`): a vitamin adds 10 up to 100 and does nothing at 100 or with the total full; a berry takes 10 away and brings a stat over 100 down to 100 at once. A berry on a stat at 0 still pleases its Pokémon, and is refused only when friendship is full too. Shedinja's HP effort can't be raised. The stats are worked out again at once.
- **An item's friendship** (`UpdatePokemonFriendship`): by the three bands (below 100, below 200, from 200), a gain × 150 / 100 with a Soothe Bell, then + 1 in a Luxury Ball; never past 255 or under 0.
- **Pokérus** (`Pokemon_ApplyPokerus`, `Pokemon_ValidatePokerus`, `Party_UpdatePokerusStatus`): after every battle but the catching lesson, three draws in 65,536 give one Pokémon of the team a strain, unless it has had one; then, one time in three, each carrier gives its strain to the one before it and the one after it that never had it. Each day takes a day off; with none left, or after more than four days at once, it is cured and can't come back. Its draws are the field's chance, not the battle's. The nurse notices it (`FLAG_POKECENTER_IDENTIFIED_POKERUS`, once).
- **Obedience** (`BattleControllerPlayer_CheckObedience`): only a Pokémon whose original trainer is someone else can disobey, and only above the level the badges allow (10, then 30 from two badges, 50 from four, 70 from six, any with eight). Its three rolls of 256 are the original's: one against level and cap to obey, a second to use another move (or, asleep and told to Snore or Sleep Talk, to sleep on), and a third against the levels over the cap for a nap (not with a condition, Vital Spirit, Insomnia or an uproar), hurting itself (a typeless hit of 40) or doing nothing in one of four ways. A Rage it was in ends. Such a Pokémon gains half as much EXP again.
- **Losing** (`subscript_battle_lost`, `BattleSystem_CalcMoneyPenalty`): the team's highest level × 4 × the badges' step (2, 4, 6, 9, 12, 16, 20, 25, 30), never more than the player has, is dropped before a wild Pokémon or paid to a trainer, in a battle the story lets the player lose as much as any other. The player wakes up in the Pokémon Center last gone into, in front of the nurse, or at home beside Mom before any (`FieldTask_BlackOutFromBattle`, `Location_InitBlackOut`; the spot is the one Teleport also reads, `VAR_SPAWN_LOCATION`), and the team is healed.
- **Forms outside battle**: Burmy takes the cloak of the ground of every battle it was sent out in (`BattleSystem_SetBurmyForm`: grass the plant cloak, open ground, sand, mountains and caves the sandy one, buildings, bridges and the League's and Frontier's rooms the trash one; not in the Great Marsh or Pal Park). Shellos and Gastrodon are met in the east sea's colours where the area's encounter data says so, and Unown in its table's letters (the ruins' dead ends most letters, the rooms of the way through F, R, I, E, N, D, the room past Maniac Tunnel the two marks; `WildEncounters_UnownTables`). Giratina is in its Origin Forme while it holds the Griseous Orb, and Arceus with Multitype is the type of its plate, whenever what it holds changes (giving, taking, a battle that took or gave an item). The Gracidea sends Shaymin into its Sky Forme from 4:00 to 19:59, if it is standing and not frozen; night, being frozen (in battle too: "changed back into its Land Forme!") and being put in a box bring it back.
- **A move to learn** (`SEQ_GET_EXP_WANTS_TO_LEARN_MOVE`): a level whose move doesn't fit asks, in the battle, whether to forget one; no asks whether to give up on it, and not giving up asks again. A move the Pokémon already knows is passed over.

**Our own choices**

- The battle's lines for losing and for disobeying are our own words on the original's beats.
- A battle read on without an answer (the tests, the harness) keeps every move: no to forgetting, then yes to giving up.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The Gracidea on Shaymin | Only one met at an event (its fateful-encounter mark) | Any Shaymin | No events in this game |
| The +1 friendship for the place it was met | Given by items and events alike | Left out | Pokémon don't record where they were met yet |
| A cured Pokémon | A small face on its summary | Nothing | Our own interface: the PKRS tag shows only while it is carried |
| The Distortion World's ground | Gives Burmy the sandy cloak | The trash cloak, as the League's rooms do | `BattleTerrain.Special` stands for all of them; the Distortion World is plan 01's |

## The bag and shops (2026-10-06, plan 06 · R11)

**Platinum's rules, kept as they are** (`src/item_use_pokemon.c`, `src/pokemon.c`, `src/overlay006/wild_encounters.c`, `src/scrcmd_shop.c`, `include/data/mart_items.h`)

- **An item on a Pokémon** (`Pokemon_CheckItemEffects`, `Pokemon_ApplyItemEffects`): first whether it would do anything (the party's ABLE and NOT ABLE), then its parts in the original's order: the conditions it heals, HP or a revival, a level, PP Ups, PP, effort, and last the item's friendship, which is given only if something was done. HP comes back by the table's amount or its shares (−1 all, −2 half, −3 a quarter), and a Pokémon whose most is 1 is given 1. A revival brings a fainted Pokémon round and nothing else; a Rare Candy on a fainted Pokémon brings it round with the HP the level adds.
- **PP Ups** (`MoveTable_CalcMaxPP`): each adds a fifth of the move's base PP, three at most; a PP Max gives all three; a move with fewer than 5 base PP (Sketch) takes none. The PP the raise adds are given at once.
- **A Rare Candy's friendship**: only the table's (+5, +3, +2 by band); the +5, +3, +2 a level gives is the battle's own (`BattleScript`'s level-up), not the item's.
- **TMs and HMs**: taught to a species whose list names the machine; a TM is used up, an HM isn't, and an HM's move can't be forgotten to make room for another.
- **A Repel** (`RepelPreventsEncounter`): while its steps last, a wild Pokémon of a lower level than the team's first Pokémon that can fight is turned away; another can't be used while one lasts. **The flutes** (`ModifyEncounterRateWithFlute`): the Black Flute halves the encounter rate and the White Flute adds half, after the rate's other changes, until the player goes elsewhere.
- **The Marts** (`ScrCmd_PokeMartCommon`): the common counter's stock grows with the badges in six steps (none, one or two, three or four, five or six, seven, eight), each item from its own step on; a town's own counter (`ScrCmd_PokeMartSpecialties`) sells its list whatever the badges. A shop pays half an item's price, nothing for a key item.

**Our own choices**

- The shop's and the bag's lines are our own words.
- The second counter of each Mart is a second clerk standing beside the first, at the end of the counter; Platinum's rooms have the two clerks too, and ours are laid out by hand until plan 01 · M11.

| What | The original | Here | Why |
| --- | --- | --- | --- |
| Later species' TMs and HMs | Not in Platinum | PokeAPI's machine moves of the species that are Platinum's machines | So a species from a later game learns what its own games teach |
| Mail | Written on its own screen | Held and given, never written | The mail screen is plan 11's |
| The move tutors | In their houses (Route 212, Snowpoint, Survival Area) | Each species' list in the data (`tutorMoves`), nobody to teach yet | Their rooms are plan 01 · M11 |

## The east and the sea (2026-10-06, plan 01 · M7)

**Platinum's rules, kept as they are** (`src/field_overworld_weather.c`, `src/overlay005/field_control.c`, `src/scrcmd.c`, `res/field/scripts/scripts_pastoria_city_observatory_gate_1f.s`)

- **The weather calendar** (`FieldSystem_GetWeather`): the south of Route 212, Route 213, Route 216, Acuity Lakefront and Snowpoint City take the weather of today's row of `sYearlyWeather`, a table of the 366 days of a leap year; in any other year the days from March on read one row further. The date is the computer's, as the original's is the console's.
- **The Safari Game**: for 500, thirty Safari Balls and 500 steps (`*steps >= 500` in the field's step counter, `*safariBallCount = 30`). Every Pokémon met is met in a Safari battle (plan 06 · R9's bait, mud and balls), whose balls are the game's own; the game ends on the last step or with the last ball, and the attendant takes the player back to the gate in Pastoria City. Saying no, or not having the fee, takes the player back out. Walking out through the gate ends a game still on.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The calendar's penalty | Changing the console's clock puts the five places on the 2nd of January's weather for a day (`FieldSystem_HasPenalty`) | No penalty: the row is always today's | The game reads the computer's clock and keeps no record of it being changed |
| The Safari Game's gate | A counter in the observatory gate's room asks before the marsh | Asked as the player comes out into the marsh, which sends them back out on a no | The gate's rooms come with plan 01 · M11; until then it is walked through |
| The marsh's binoculars | The day's species shown through the coin viewers; the tram rides between the areas | The day's species are met (plan 06 · R13); the viewers and the tram stand there and do nothing yet | Plan 02's scripts |
| Retiring from the game | The menu offers to end it early | Walk out through the gate | No field menu entry for it yet |
| Iron Island's lifts | A platform rides between levels | Stepping onto it is a warp to the other level, through a fade | Moving platforms came with the Canalave Gym's lifts (plan 01 · M9 2a, `CanalaveLifts`); Iron Island's could ride them too, once its levels are one map |
| Sailor Eldritch's boat | Sails to Iron Island, and to Fullmoon and Newmoon Islands when the story sends the player there | Iron Island and back | The two islands are plan 01 · M10's |
| Maniac Tunnel | Dug once enough kinds of Unown are seen | Not open; Solaceon's Rare Candy beyond it is held back | Plan 08 · P19, with the count of Unown's forms seen (see "After the Hall of Fame, part c") |


**The Canalave Gym** (plan 01 · M9 2a)

| What | The original | Here | Why |
| --- | --- | --- | --- |
| An empty slot | A platform's place on a floor above the ground is open in that floor's collision map whether or not the platform is there; the floor's model covers it | A slot is walked only while its platform stands there; where it has gone, the floor has a hole | Nobody walks on the air, and the picture shows the hole |
| The floors' heights | Ten tiles apart, by the walker's height | The same; the floor someone is on is the nearest to their height (`CanalaveLifts.FloorOf`) | |
| A save made on an upper floor | The platforms are laid out by the room's own script as the map loads | The same: they come back at their first ends | |

**The Snowpoint Gym** (plan 01 · M9 2b)

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The ice's speed | Nought to three, one more for each tile sloping down the way the slider goes and one less for each sloping up, measured a quarter of a tile ahead of the tile's middle (`PlayerAvatar_CheckIceHeightChange`); stopped, it is gone | The same (`IceSlide`, `Player.IceSpeed`) | |
| No speed left on the way up | The slider walks a tile back down slowly, facing up the slope, and stands; so does a slider stopped by something on the way up | The same, at a quarter of the ice's pace (the original's sixteen frames against its four) | |
| The pace of a slide | A fast walk of four frames a tile at nought, three at one, two at two and three | Our run's eight tiles a second at nought, faster in the original's ratios (10.7, then 16) | The game's paces are its own; the ratios are the original's |
| A slide into a ledge, a ramp or a climb | Any collision stops the slide | The slide goes on into whatever the step would do, as this game's ice always did | No ice of the open world lies against one; left as it was so nothing outside the Gym moves |
| A snowball | Broken by a slide whose speed is one or more when it stands on the next tile (`ov5_021E06A8`); the slide goes on through its place; deleted until the map's objects are made again | The same (`IceSlide.Breaks`); back the next time the player comes in, as anything taken off a map is | |
| A save made with snowballs broken | The map's objects are saved, so a broken snowball stays broken | Every snowball is back after loading | The save keeps no map's things but its trainers and items; coming in again brings them back anyway |
| The room's own script | Hides Candice in Snowpoint City if `FLAG_DUMMY_0x00EB` is set | Left out | Nothing sets that flag |

**The Sunyshore Gym** (plan 01 · M9 2c)

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The gears' walkways | Closed by the room's table of regions for its state (`SunyshoreGym_DynamicMapFeaturesCheckCollision`), for the player's own steps only | The same (`SunyshoreGears.Refuses`, asked by `FieldMovement.Step`); a trainer's eyes and walk see the room's own grid, where every walkway is open, as the original's map objects do | |
| The way they turn | 5.625 degrees a frame at thirty frames a second; the state changes as the button is pressed and the field waits for the gears | The same (`SunyshoreGears.Turn`); the field holds while they turn | |
| A button | Every step onto it presses it (the room's coordinate events wait for a variable nothing changes) | The same | |
| Coming in | By a room's door from the room before, its first state; by any other way in (the door from the room beyond), the state that leads back: two, one and nought (`PersistedMapFeatures_InitForSunyshoreGym`) | The same, by the row the player comes in on (`SunyshoreGears.ArriveAt`) | |
| A save made in the Gym | The state is saved with the game's persisted features | It comes back as coming in from the room beyond lays it out | The save keeps no puzzle's state, as with the Canalave Gym's platforms |
| The third room's top row | Four columns (7, 8, 14 and 15) are open in the collision grid but have no height plate under them, so the walker's height check stops anyone stepping onto them from the platforms either side | Solid | The same in play: nobody walks from one platform to the next past the puzzle |
| The statues | Name the player once the Badge is won, and the rival too once `VAR_RIVAL_BEAT_SUNYSHORE_GYM` is set | The player only | The rival's scene in Sunyshore City, which sets the variable, is plan 02 · S13's; the statue's branch comes with it |
| The floor's behaviour | `DYNAMIC_HEIGHT_COLLISION` (0x59) on most of the floor | Left out | It acts only under a dynamic height plate, and the Gym has none |

**The League's rooms** (plan 01 · M9 2d)

**Platinum's rules, kept as they are** (`src/platform_lift.c`, `res/field/scripts/scripts_pokemon_league_*.s`, `res/field/events/events_pokemon_league_*.json`)

- **There is no way back.** Each room is come into on a tile that carries no warp, and leads on by one door: from the Pokémon Center's door to the Hall of Fame the only ways out are to win and to lose. A battle lost wakes the player in the League's own Pokémon Center.
- **The Pokémon Center makes the rooms ready**: every time the player comes into it, the four doors behind are open, the four doors on are shut and the Four have not been beaten (`FLAG_HIDE_POKEMON_LEAGUE_<WHO>_ROOM_ENTRANCE_DOOR` set, `…_EXIT_DOOR` and `FLAG_DEFEATED_<WHO>` cleared).
- **A room of the Four**: the challenger walks two steps in and the door comes down behind; the door on is gone once the room's master is beaten. Cynthia's room, the hallway and the Hall of Fame are each one scene, played as the player comes in.
- **A platform** is three tiles by two, rises two units of sixteen a frame at thirty frames a second (3.75 tiles a second), and never comes down: ten tiles to Aaron's room, five to Bertha's, Flint's, Lucian's and the Champion's, fifteen in the Champion's room. A passage's sets off when the player steps onto its middle tile, once.
- **The Four's teams, their Full Restores and their minds** are `trainers.json`'s (`elite_four_aaron`, `_bertha`, `_flint`, `_lucian`, `champion_cynthia`).

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The guard at the door | Looks the Badges over and steps aside, whoever asks | Counts them, and stays where he is for fewer than eight | Nobody reaches the original's League without all eight (Victory Road needs Waterfall, and Waterfall the Beacon Badge); a game brought there some other way is told so instead of let through |
| The way into a room of the Four | An open doorway in the room's south wall, which the door shuts before the player can act | Wall in the room's own grid; the door stands in it once it has shut | The same in play; the room's front wall is cut away to its kerb, as every room's is |
| When a room's arrival plays | While a local variable is nought (the frame table is asked every frame) | For the Four, while the door behind is still open (its own flag); for Cynthia's room, the hallway and the Hall of Fame, once as the player comes in | A place's script runs once on arrival here, and a local variable is lost to a save made in the room, which would walk the player in again |
| A platform's floor | The dynamic height plate takes the top's height once the platform has stopped; the player is held on it meanwhile | The floor is where the platform is (`LeagueLift.FloorAt`); the plate's own height (`Floor`) changes as the original's does | Nobody steps while it rises, and whoever stands on it is drawn at its height |
| Where a platform stands as the player comes in | By the warp's row: at the foot from the bottom warp, at the top from any other; saved with the game | By the height the player stands at, which the save keeps | The same for both of the original's ways in; a game saved on a platform at the top comes back with it there |
| `FLAG_ALT_MUSIC_CHAMPION_ROOM` | Set before Cynthia's battle, cleared when it is lost | Set once it is won | A script ends where a battle is lost; the room has no second song yet (plan 05 · A6) |
| The Four's and Cynthia's rematch teams | Once Charon is arrested at Stark Mountain (`FLAG_ARRESTED_CHARON_STARK_MOUNTAIN`) | Not asked | The flag's scene is plan 02 · S15's, and the suite allows no flag read that nothing sets |
| The Hall of Fame | The professor's and Cynthia's words, the Poké Balls on the machine, each Pokémon shown, the save, the credits, the game begun again at home, and the flags of `SetHallOfFameVictoryFlagsAndVars` | The walk to the machine, a welcome, then the team healed and entered (`halloffame`, `pc halloffame`), `FLAG_GAME_COMPLETED`, the region's story done, Giratina back in Turnback Cave unless caught, and a warp to the player's room | The scene is plan 02 · S14's: this is the smallest end that leaves the game playable (`scripts/PokemonLeagueHallOfFame.txt` lists what S14 adds) |
| The rival before the door | His last battle, by a trigger in front of the guard | Not there | Plan 02 · S14's |
| The Pokémon Center's PC | Set into the back wall's row at (8, 0), used from (8, 1) | A desk at (8, 1), used from (8, 2) | A room here has no floor in its back wall's row |
| The Pokémon Center's stairs | An alcove south of the hall with the ways to the upper floor and the basement | Left out | Those floors are plan 01 · M11's |
| The rooms' music | `SEQ_D_LEAGUE` in the passages and the rooms, another song in the Champion's room once she is beaten, `SEQ_BLD_DENDO` in the hallway and the Hall | The League's own theme (`sinnoh/pokemon_league`) everywhere | Plan 05 · A6 owes the three songs |
| Flint's greeting | He runs on the spot between its halves, faster and faster | A start (the "!" over his head) and a beat | A person has no run on the spot yet |

## The first chapter (2026-10-06, plan 02 · S4)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_twinleaf_town*.s`, `scripts_route_201.s`, `scripts_verity_lakefront.s`, `scripts_lake_verity_low_water.s`, `scripts_sandgem_town*.s`, `scripts_route_202.s`, `src/field/field_system.c`'s `InitNewGame`)

- **A new game** begins upstairs at home with no Pokémon, an empty bag and no Pokédex, and the start menu offers POKéDEX and POKéMON only once the player has them. Running needs the Running Shoes, which Mom gives once the player has come home with a Pokémon.
- **The first battle is the rival's**, not a wild Starly's (Diamond and Pearl's): his team is the one Platinum names after the player's starter (`TRAINER_RIVAL_ROUTE_201_<starter>`, the one strong against it), it has no critical hits, and it may be lost: the team is healed either way and the player wakes at home.
- **The professor and the Pokédex ask until they hear yes**, as the original's questions loop; the rival asks for his battle the same way.
- **The assistant's first Pokémon** is the one of the three neither child took.

**Our own choices** (2026-10-07, after a game left the briefcase unseen and went on with no Pokémon)

- **Nobody leaves the briefcase without a Pokémon.** The original turns the player back from two tiles at the grass's edge while the briefcase waits; here the whole of the grass's edge, the road west to the lake and the road home do so too (triggers of our own in `overlays/route_201.json`).
- **The rival steps aside** at the end of the professor's scene, and stands there again whenever the player comes back to the road while the briefcase waits. His own tile is the one in front of it, and from the field's steep camera his back hid it.
- **No battle is started without a Pokémon able to fight**, from the grass, a trainer's eyes or a script: a script's battle says why and ends the script there (`IScriptHost.CanBattle`). The original never meets the case; here it ended the game.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The rival on the way to the lake | Walks behind the player from Route 201 to the lakefront | Runs ahead and waits at the lakefront; the roads home and to Sandgem Town tell the player he went to the lake | Nobody follows the player yet |
| The way to the lab door | The assistant walks the player from the edge of Sandgem Town to the lab | A fade, and the two stand at the door | Two people walked side by side through a town isn't a script command yet |
| The television | A caption of the special before the field appears | The special's last lines under "TV" over the bedroom | No caption screen; the lines are the same beat |
| A nickname for the first Pokémon | The professor asks whether to give one | Not asked | Pokémon can't be named yet |
| The Journal | Records what the player does | Given, and does nothing yet | The Journal's screen isn't built |

## The second chapter (2026-10-06, plan 02 · S5)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_jubilife_city.s`, `scripts_trainers_school.s`, `scripts_route_203.s`, `scripts_oreburgh_gate_1f.s`, `scripts_oreburgh_city.s`, `scripts_oreburgh_mine_b2f.s`, `scripts_oreburgh_city_gym.s`, `scripts_init_new_game.s`)

- **Looker holds the road to Route 203** until the player has delivered the parcel and has a Pokétch, and the Pokétch is the three coupons' price; the third clown has nothing to give until the president has told the campaign.
- **The rival's teams** on Route 203 are the ones Platinum names after the player's starter, and so is the assistant's in the tag battle (`TRAINER_DAWN_JUBILIFE_CITY_<starter>` for a boy, Lucas's for a girl).
- **The Gym's door** is kept by the rival until Roark has come back from the mine; the Coal Badge brings Team Galactic to Jubilife's north gate and takes the professor out of his lab until they are beaten.
- **HM06** is given the first time the player passes the hiker in Oreburgh Gate, Badge or not; it is used in the field only with the Coal Badge.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The boy who takes the player to the Gym | Walks there with the player behind him | A fade, and the two stand before the Gym | Nobody follows the player yet |
| The collector after the tag battle | Gives the Fashion Case and accessories | Not there | Accessories and contests are plan 06 · R17's |
| Looker's Pal Pad, the Global Terminal's greeter | After the Coal Badge, by the Pokémon Center | Not there | Their rooms are plan 01 · M11's |
| People who wander | Walk about within their own range | Stand where they are placed (until S6, which made them move) | The importer didn't keep the ranges yet |


## The third chapter, first half (2026-10-07, plan 02 · S6)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_floaroma_town.s`, `scripts_route_205_south.s`, `scripts_floaroma_meadow.s`, `scripts_valley_windworks_outside.s`, `scripts_eterna_forest.s`, `scripts_follower_partners.s`, `src/overlay006/wild_encounters.c`, `src/encounter.c`, `src/unk_0206450C.c`)

- **People move about** by their movement type and range (`Overworld/Wandering.cs`): those who look about turn after a wait of 16, 32, 48 or 64 frames drawn at random; those who wander turn the same way and take a step that way only when it stays within their range of where they first stood and nothing is in it; those who walk a loop go round a corner of their box, out along two legs to its edge and back along two to where they began, marking time while something blocks them. Coming to a map again puts everyone back where they first stood.
- **Someone travelling with the player** (Cheryl, `Overworld/Follower.cs`) heals the team after every battle that isn't lost (fled from too: `CheckPlayerWonBattle` counts only a loss or a draw as not won), brings a second wild Pokémon into every battle of the grass (both drawn as the first is; the lead scaring either off leaves the grass quiet), battles beside the player with a fresh team of their own each time, stays behind when the player whites out, and keeps the Bicycle, the rods, an Escape Rope and the field moves that leave the place in the bag. The player gets off the Bicycle as she joins.
- **Two trainers who see the player at once come together** (`APPROACH_TYPE_VS2`): beside a partner always, as a tag battle; without one, two against the player's two Pokémon when the player has two able to fight, and otherwise one at a time.
- **The meadow's two grunts** battle one after the other with no healing between, and a loss in either leaves them there for both battles again.
- **Coming out of a warp** onto a trigger starts it, as the original's step off a door's mat does (Cheryl at the forest's edge); a trigger on a warp's own tile goes before the warp (she turns the player back from it).

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| Drifloon at the Valley Windworks | On Fridays, after Commander Mars, a Drifloon floats by the signboard to be battled | Not there | A Pokémon can't stand in the field yet |
| The Galactic lobby theme in the Windworks | `SEQ_D_GINLOBBY` until Mars is beaten | Route 205's theme throughout | No lobby theme yet (plan 05) |
| Cheryl joining | A jingle of its own (`SEQ_GONIN`) | The level-up fanfare | No partner jingle yet (plan 05) |
| A follower through a warp | Follows the player onto the next map | Stays on her own map, the battles still beside the player | Only Cheryl travels so far, and the forest's exits turn the player back while she does |

## Wild encounters (2026-10-07, plan 06 · R13)

**Platinum's rules, kept as they are** (`src/overlay006/wild_encounters.c`, `swarm.c`, `special_dates.c`, `great_marsh_daily_encounters.c`, `trophy_garden_daily_encounters.c`, `roamer_after_battle.c`, `feebas_fishing.c`, `src/overlay005/honey_tree.c`, `field_control.c`, `src/pokeradar.c`, `src/roaming_pokemon.c`, `src/special_encounter.c`, `res/field/encounters/`)

- **The grass's slots at a moment** (`WildEncounters_TryWildEncounter`), each keeping the level and weight of the slot it takes: the morning (4:00 to 9:59) keeps the table's own slots 2 and 3, the day and the evening (10:00 to 19:59) put the area's day species in them and the night and the late night (20:00 to 3:59) its night species; then a swarm's species in slots 0 and 1, the Trophy Garden's two in 6 and 7 once the player has the National Pokédex, and during a Safari Game the Great Marsh's daily species in 6 and 7. Water and rods keep their tables (but Feebas's tiles, below).
- **The day's number** (`RecordMixedRNG`, `SpecialEncounter_SetMixedRecordDailies`): each new day moves it on (× 1,812,433,253 + 1 in 32 bits, once a day passed), and the Great Marsh's and the swarm's numbers become it. Swarms are at one of 22 places, the number modulo 22 (`sSwarmMapIdTable`), from the day the assistant's sister first tells of them; the Great Marsh's six areas each read five bits of theirs (area n the bits from 5n) from a list of 32, the National Pokédex's once the player has it.
- **The Trophy Garden** (`TrophyGarden_AddNewMon`): each new Pokémon is drawn from the garden's sixteen until it is neither of the two there, takes the first place and moves the first to the second.
- **The days that change the odds** (`SpecialDates_ModifyEncounterRate`): on 39 days of the year (the original's list of 43, four of which change nothing) the flat chance that an attempt gets through (40 in a hundred, 70 in very tall grass or on a Bicycle) moves by five or ten either way, never under one; with the clock's penalty (which this game never gives, "The east and the sea") it wouldn't.
- **What wild Pokémon hold** (`Pokemon_GiveHeldItem`): a species whose two items are the same always holds it; otherwise 45 in a hundred hold nothing, 50 the common item and 5 the rare, or 20, 60 and 20 with Compound Eyes at the head of the team. Every wild Pokémon met in the field or put in the way by a script is given one, but a roamer, the catching lesson's and Pal Park's.
- **What keeps a Pokémon away**: Sweet Scent and Honey draw one out whatever the lead's Keen Eye or Intimidate and whatever Repel lasts (`WildEncounters_TrySweetScentEncounter`); a rod's catch can be kept away by Keen Eye and Intimidate but never by a Repel; a roamer is kept away by a Repel as a Pokémon of its level would be.
- **A Pokémon hooked on a rod is fought on the water** (`FieldBattleDTO_SetWaterTerrain`), wherever the player stands to fish: the Dive Ball, Camouflage, Nature Power and Secret Power read water.
- **Honey trees** (`HoneyTree_SlatherTree`, `HoneyTree_GetTreeSlatherStatus`): 21 trees, slathered from the south with a Honey. A day's honey (1,440 minutes) draws one group as it is slathered: nothing 10 in a hundred, the common table 70 and the uncommon 20, or at one of the player's four Munchlax trees nothing 9, common 20, uncommon 70 and Munchlax 1. Six hours later (1,080 minutes left) a slot of the group's six is waiting (40, 20, 20, 10, 5, 5 in a hundred) at a level from 5 to 15, and the tree shakes as the group's table says. The same tree slathered twice running keeps its group nine times in ten. The four Munchlax trees are four bytes of the trainer's 32-bit number, each modulo 21, later ones moved on past any repeat. Battling what came takes the honey with it.
- **The Poké Radar** (`PokeRadar_*`, `RadarSpawnPatches`, `SetupGrassPatches`): fifty steps charge its battery; used standing in tall grass, on foot and alone, it sets one patch of tall grass shaking on each of four rings round the player (9, 7, 5 and 3 tiles across), only at the player's height in the player's place. A patch walked into always meets a Pokémon. The first starts a chain of its species; after a battle won or a catch the patches are set again, each going on with the chain 88, 68, 48 and 28 times in a hundred from the outer ring in (98, 78, 58, 38 after a catch) and, if so, sparkling one time in 8,200 − 200 × the chain (never better than one in 200); a patch that doesn't go on shakes softly or hard on a coin, and a hard shake puts the area's four radar species in slots 4, 5, 10 and 11. Running, losing, another Pokémon met, a trainer, a warp, the Bicycle or every patch out of the screen ends the chain, which counts to 999.
- **Roaming Pokémon** (`RoamingPokemon_*`, `RoamerAfterBattle_UpdateRoamers`): six slots (Mesprit and Cresselia at 50, Darkrai at 40, the three birds at 60), each set loose by the story. Whenever the player walks into another place every roamer moves on, anywhere one time in sixteen and to a place nearby otherwise, never into the one the player has just left (a warp notes where the player is but moves nobody; not during a Safari Game); flying, Teleport and a game continued send each anywhere. Where one is, a Pokémon met in the field is it one time in two (not beside a partner or in a radar patch). It flees, keeps its HP and condition, and once knocked out or caught roams no more (`VAR_ROAMING_<SPECIES>_STATE`); after any battle with it, and three times in ten after another wild battle, every roamer where the player stands moves elsewhere.
- **Feebas** (`PlayerAvatar_IsFacingFeebasTile`): the tiles of Mt. Coronet's lake are cut into four groups in the original's order, and the day's number picks one of each by its four bytes; a cast at one hooks Feebas (levels 10 to 20, any rod) one time in two.
- **Poison in the field** (`Field_UpdatePoison`, `Pokemon_DoPoisonDamage`, `Pokemon_TrySurvivePoison`): every fourth step each poisoned Pokémon able to fight loses a hit point, never its last, and the field flashes; one left with one hit point comes through, cured, and loses a little friendship (5, 5 or 10 by its band).

**The dual slot** (plan 08 · P23; `WildEncounters_ReplaceDualSlotEncounters`, the table's `ruby`, `sapphire`, `emerald`, `firered` and `leafgreen`)

- Each area with grass lists two species for each of the five Game Boy Advance games (`WorldAreaFile.DualSlot`, `MapArea.DualSlot`, written by the importer's `--data` from the original's encounter files; the Pokédex's area page leaves them out as the original does). With a game chosen **they take slots 8 and 9**, each keeping the slot's level and weight, after the time of day, a swarm and the Pokégear's songs and before the Trophy Garden's and the Great Marsh's (slots 6 and 7, which they never share). Sweet Scent, Honey, the Poké Radar and every step in the grass go through the same table (`EncounterSlots.Grass`), so each meets them.
- **The stand-in for the cartridge**: there is none to read, so the game is the options' SLOT 2 GAME (`GameSettings.SlotTwo`, `SlotTwoGame`: None, Ruby, Sapphire, Emerald, FireRed, LeafGreen; None unless chosen, one value for the whole game, so it can be changed between steps). Not `Foreign.GbaGame`, which tells which game wrote a save file.
- **Only with the National Pokédex** (the plan's ruling; Platinum's own condition for the dual slot is the cartridge alone), so a new game's grass is Platinum's whatever the options say. With a game chosen its two take the place of our own species of plan 03 · D12a in the same two slots (the species Diamond and Pearl have, which nothing else gives): with None they are met as before.

**Our own choices**

- The honey tree's, the radar's, the poison's and the swarm's lines are our own words.
- The day's number starts at a draw of the field's chance in a new game, where the original starts it at nothing until a record-mixing group is founded; the trainer's hidden half (the 16 bits the card doesn't show) is drawn the same way and kept with the wild Pokémon's state (`SpecialEncounters.SecretId`).
- The radar's patches stir their grass (it parts and jumps from side to side, as round someone's feet) and shed a few blades, the more for a hard shake, a sparkle over a shiny one; a honey tree with Pokémon at it sheds leaves from its crown. The original animates the grass and the tree with sprites of its own.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The pair of species a second game calls up | Slots 8 and 9 take two of the game in the DS's other slot | The options' SLOT 2 GAME names the game (None unless chosen); with the National Pokédex its two take slots 8 and 9 | No cartridge to read (plan 08 · P23, "The dual slot" below) |
| Who tells of swarms and of the Trophy Garden | The assistant's sister in her house in Sandgem Town; Mr. Backlot in his mansion, once a day | The common scripts are ready (`common.SwarmNews`, the `trophygarden` command) and nobody runs them yet | Their rooms are plan 01 · M11's |
| The radar's music | The radar's own theme while patches shake | Not yet (the chain records are the Pokétch's Trainer Counter's, plan 06 · R14b) | The theme is plan 05's |
| Who is where | The Pokétch's marking map shows the roamers; the TV tells of swarms | The marking map (plan 06 · R14b); not the TV yet | The TV is plan 08's |
| Poison by the modern rules | Generation 5 on: poison does nothing outside battle | So (`Ruleset.PoisonInTheField`) | The modern preset's ruling |

## The third chapter, second half (2026-10-07, plan 02 · S6)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_eterna_city.s`, `scripts_team_galactic_eterna_building_1f.s` to `_4f.s`, `scripts_cycle_shop.s`, `scripts_eterna_city_underground_man_house.s`, `scripts_eterna_city_pokecenter_1f.s`, `scripts_eterna_forest.s`, `scripts_eterna_city_gym.s`, `src/map_object.c`)

- **HM01 comes before the Gym**: Cynthia gives it in front of Team Galactic's building as soon as the rival's scene at the statue is over (her trigger waits for `VAR_ETERNA_CITY_STATE` 1), and Cut clears the trees in the field only with the Forest Badge, so the building, behind a tree, comes after the Gym.
- **Gardenia stands at her Gym's door** from the start of a game and goes in once spoken to. Someone a script takes away is hidden by their own flag for good, as the original's `RemoveObject` sets it (`MapObject_SetFlagAndDeleteObject`): the rival, Cyrus, Cynthia and Gardenia all leave so.
- **The building's floors are the original's to the tile**, and so is Looker's warning: on each floor one way up comes out in a pocket with a grunt and an item whose only way on is back down, the other where the next way up is. Its grunts, Scientist Travon and Commander Jupiter fight with Platinum's teams; Jupiter's defeat takes every grunt out of the building and the town (`FLAG_HIDE_ETERNA_CITY_GALACTIC_GRUNTS`) and sends the manager home.
- **Eterna's ways out are watched** from the Bicycle (`VAR_ETERNA_CITY_BLOCK_EXITS_STATE` 1) until the player has the Explorer Kit as well, which the town's arrival script checks: the west way tells of the Cycling Road, the south one sends the player to the Underground Man.
- **Gardenia waits before the Old Chateau** once her Gym's script has cleared `FLAG_HIDE_ETERNA_FOREST_GARDENIA`, as the original's does after her battle.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The Pokémon Team Galactic held | A Clefairy and a Buneary on the top floor, then the Clefairy in the cycle shop and the Buneary in the Pokémon Center | Not there; the flags that move them are set as the original sets them | A Pokémon can't stand in the field yet (plan 10) |
| The Galactic lobby theme in the building | `SEQ_D_GINLOBBY` until Jupiter is beaten | Eterna's theme throughout | No lobby theme yet (plan 05) |
| The Underground Man's missions and his PC | Six missions below ground; the PC's pages on flags, spheres and traps | The Explorer Kit, his offer and the first mission; the PC's notes in a line | The Underground is plan 06 · R16's |
| The Friendship Checker's woman | Reads out the first Pokémon's friendship on later visits | One line of her own after giving the app | A script can't ask a Pokémon's friendship yet; the app is kept and shown once the Pokétch runs it |
| The Old Chateau | Its rooms, its ghosts and Rotom's television | Its door in the forest stays shut | Its rooms are plan 01 · M11's; nothing of the story happens inside |
| Rotom's room in the building | Behind a wall on the ground floor that the Secret Key opens | The wall | The Secret Key is the post-game's |

## The fourth chapter, first part (2026-10-10, plan 02 · S7)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_route_207.s`, `scripts_mt_coronet_1f_south.s`, `scripts_route_208.s`, `scripts_wayward_cave_1f.s`, `scripts_hearthome_city.s`, `scripts_contest_hall_lobby.s`, `scripts_route_209_gate_to_hearthome_city.s`, `scripts_hearthome_gym_leader_room.s`)

- **The assistant comes to the foot of Mt. Coronet** on Route 207 the first time the player steps onto the original's trigger, with the Vs. Seeker from the professor and the Dowsing Machine for the Pokétch, after a guess at which hand holds it (either answer gets both). Eterna's bug catcher goes back to his line about the wind once she has been (`VAR_ROUTE_207_COUNTERPART_TRIGGER_STATE` 1).
- **Cyrus waits inside Mt. Coronet** at the first trigger of its southern hall, speaks of the mountain where Sinnoh began and goes off west.
- **The black belt on Route 208 gives the Odd Keystone** once, as the original's does (`FLAG_RECEIVED_ROUTE_208_ODD_KEYSTONE`).
- **Mira is optional**: she waits in Wayward Cave's first room, joins when spoken to and walks with the player; at the way out she thanks them and leaves for good (`FLAG_TRAVELED_WITH_MIRA`). A player who leaves without her finds her waiting where she stood.
- **Keira's Buneary runs into the player** at the city's west edge, from whichever of the trigger's five tiles they stepped onto, and Keira comes after it. In the Contest Hall's lobby she meets the player's mother and goes off to rehearse.
- **The fisherman walks the player to the Contest Hall** if asked, from whichever side they spoke to him.
- **Fantina is at the Contest Hall**, not in her Gym, until she is spoken to: she twirls, says she will wait at the Gym, and leaves; the Gym's guide stops keeping its door (`FLAG_HIDE_HEARTHOME_CITY_GYM_GUIDE`).
- **The road east is shut until Fantina is beaten**: two men talking about eggs stand in the doorway of the gate to Route 209 (`FLAG_HIDE_HEARTHOME_CITY_ROUTE_209_BLOCKADE`, set by her Gym's script), and the rival waits inside the gate to battle with the team that has the upper hand on the player's starter.
- **Saves from before**: `common.ChapterFour` (story version 9) hides the chapter's people until their scenes; a save that already holds the Relic Badge also gets Fantina out of the lobby and the guide away from the Gym's door.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| Keira's present | Glitter Powder for the player | Not given | No accessories yet (plan 06 · R17) |
| The mother's gift | A dress or a tuxedo for contests | Not given | No Dress-Up yet (the contests, plan 06 · R17) |
| The receptionists | Enter the player in a Super Contest | Say no contest is being held today | The contests are plan 06 · R17's |
| The reporter in the lobby | Interviews the player | Hidden | No interviews yet (plan 08 · P9) |
| Mira in the depths | Follows the player down into Wayward Cave's lower floor | Walks with the player on its first floor only | A follower stays on her own map (plan 02 · S6) |
| Amity Square | Walking with a Pokémon, the gifts it finds | Its gates are passed through as before | Its rooms and walks are plan 01 · M11's |
| The lobby's and the gate's furniture | The original's own models | Counters, a computer, plants, tables and benches of our kit, where its models stand | The kit has no contest booths yet |

## The fourth chapter, second part (2026-10-10, plan 02 · S7 and plan 08 · P12)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_route_209.s`, `scripts_route_209_lost_tower_5f.s`, `scripts_solaceon_town.s`, `scripts_solaceon_ruins_room_1.s`, `scripts_solaceon_ruins_room_2.s`, `scripts_solaceon_ruins_room_7.s`, `scripts_route_210_south.s`)

- **The Hallowed Tower** is read from its four tiles. Without the Odd Keystone it is a broken tower; with it the player may set the stone in its gap (`VAR_HALLOWED_TOWER_STATE` 1). From then it stirs at eight, fifteen, twenty-two and twenty-nine people spoken to, and at thirty-two Spiritomb comes out of it (level 25). Once its battle is over, however it ended, the stone is spent and the count starts again (`ClearSpiritombCounter`).
- **The fisherman on Route 209 gives the Good Rod** once, and after that explains how to fish.
- **The Lost Tower** has five floors joined by stairs, wild Pokémon on every floor (the Old Chateau's floor behaviour, at the original's rate and slots), its trainers and items, and fog on the top floor that Defog lifts while the player stays. The two old women there give the Spell Tag and the Cleanse Tag once the fog has been cleared in front of them (`FLAG_USED_DEFOG_IN_ROUTE_209_LOST_TOWER_5F`), and keep giving them after the fog has come back.
- **The rival comes down Solaceon's street** the first time the player crosses the original's trigger from Route 209, says he found HM05 in the ruins and goes off north (`VAR_SOLACEON_TOWN_STATE` 1).
- **The Ruin Maniac gives the Pokémon History app** to a trainer who has seen fifty species of the Sinnoh Pokédex.
- **The hiker in the ruins** asks to borrow HM05 once the player has it and gives a Green Shard for it; the HM stays the player's. Turned down, he asks again at once the next time (`FLAG_DID_NOT_LOAN_HM_DEFOG`).
- **The Psyduck stand across Route 210** until the Secret Potion cures their headaches; then they go for good (`FLAG_HIDE_ROUTE_210_SOUTH_PSYDUCK`).

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| Spiritomb's count | People spoken to in the Underground (`VAR_SPIRITOMB_COUNTER`) | Different people spoken to anywhere, each counted once until Spiritomb appears (`StoryState.Greeted`, saved as `SaveData.GreetedPeople`) | No Underground yet (plan 06 · R16); plan 08's decision 2 |
| The Unown inscriptions | Written in the Unown alphabet | Read out in letters | No Unown typeface yet |
| The Lost Tower's music | A theme of its own | Route 209's | No song for it yet |
| The Lost Tower's look | The original's tower models | A room of our kit with headstones and tombs where its graves stand | The kit has no tower interior yet |
| Cynthia after the Psyduck | Comes up Route 210 and gives the Old Charm | Not yet | What follows the Psyduck is plan 02 · S9's (the Secret Potion came in S8, at Valor Lakefront) |

## The fifth chapter (2026-10-10, plan 02 · S8)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_route_215.s`, `scripts_veilstone_city.s`, `scripts_veilstone_city_gym.s`, `scripts_veilstone_city_galactic_warehouse.s`, `scripts_pastoria_city.s`, `scripts_pastoria_city_gym.s`, `scripts_route_213.s`, `scripts_valor_lakefront.s`, `scripts_route_212_gate_to_hearthome_city.s`, `scripts_init_new_game.s`)

- **The original's `RemoveObject` hides for good.** It sets the flag that hides whoever it takes off the map (`MapObject_SetFlagAndDeleteObject`), so everyone a scene of the original sends away is hidden by their flag here as well: the assistant after Crasher Wake's song (until Maylene's Badge clears it), the warehouse's three grunts together (`FLAG_HIDE_VEILSTONE_GALACTIC_GRUNTS`), the rival after his battle in Pastoria, the grunt who runs east to Route 213. Read without that, the original's scripts seem to leave the rival standing in the Pastoria Gym's doorway after his battle.
- **The rival keeps Pastoria's Gym door while Crasher Wake is away** (`FLAG_HIDE_PASTORIA_CITY_RIVAL`, clear in a new game), saying the Leader has gone to Veilstone; he steps out of it once Looker has found HM02, and comes back down the street to battle at the door (`VAR_PASTORIA_CITY_STATE` 1, the warehouse's). So Crasher Wake can't be challenged before the warehouse, as in the original.
- **The television crew keeps the gate between Hearthome City and Route 212 shut** until the player has come to Pastoria City from the other side, whose arrival lifts it (`FLAG_HIDE_ROUTE_212_BLOCKADE`, set by Pastoria's `OnEnter` every time, as the original's OnTransition does). Veilstone, and Route 214 and 213 after it, are the only way to Pastoria before.
- **HM02 is an item in its ball** in the warehouse, picked up by the player after Looker has said to keep it, as in the original: it isn't handed over. The Dusk Stone lies behind the rusty door, which is plan 02 · S11's.
- **The Secret Potion comes at Valor Lakefront, in this chapter**, from Cynthia once the grunt who carried the bomb is beaten there, as the original's order has it; plan 02's outline gave it to S9, which begins at the Psyduck.
- **The road east to Sunyshore City is closed** by a man at Valor Lakefront who steps across it and turns the player back (`VAR_VALOR_LAKEFRONT_BLOCK_SUNYSHORE_STATE` 0) until the professor's lab sees to it after the Distortion World (plan 02 · S13; the original's `scripts_sandgem_town_pokemon_research_lab.s`).
- **Saves from before**: `common.ChapterFive` (story version 10) hides the chapter's people until their scenes. A save that already holds Maylene's Badge has had Crasher Wake come out of her Gym before it (`VAR_VEILSTONE_CITY_CRASHER_WAKE_STATE` 1); one that holds Crasher Wake's, or has been to Pastoria City (`FLAG_FIRST_ARRIVAL_PASTORIA_CITY`), has seen the rival leave the Gym's doorway and the television crew leave the gate. The scenes the Badges' own scripts set going (the assistant's call for help, Crasher Wake coming out of his Gym) play as such a save next comes to the town.

**Our own choices**

- **The rival's battle in Pastoria is shown for the scene alone.** The original clears his flag for the battle and sets it again if the player loses; here his flag stays set and the scene shows him, so a battle lost leaves the Gym's doorway clear and the trigger waiting, whatever the field does with his place meanwhile.
- **Crasher Wake leaves his Gym** while he is after the bomber (`FLAG_HIDE_PASTORIA_CITY_GYM_CRASHER_WAKE`, set as he comes out after the Fen Badge and cleared when Cynthia has given the Secret Potion). The Gym's map gives its Leader no flag of his own, so the Gym's arrival script sends him away (`PastoriaGym.txt`, `hide`), until lane 2 gives his map entry the flag.
- **A scene played from where the player stands** follows the original's tile tables: the assistant from the warehouse's west, north or south (east of her is the gap between the guards, which nobody reaches before the scene), Crasher Wake to each of the four tiles before the Veilstone Gym, Looker on Route 213 and Cynthia and the rival at the lakefront to each side of the grunt, the man by the road to Sunyshore at each of its three tiles. The player's facing is read before anything turns them, since a scene's `face player` would change it.
- **Pastoria's arrival scene puts the player at the Gym's door** before it plays, as the original's OnFrame finds them there: coming out of the Gym is the only way to state 3, but a save loaded elsewhere in town would otherwise watch the scene from a distance.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The bomb's sound | Its own explosion | Thunder (`thunder`) with the camera shaken | No explosion sound yet (plan 05) |
| Crasher Wake's song | Lines of his own theme in the text box, with music notes | Our own verses, without the notes | The interface's font has no music note (Latin-1 only) |
| Looker's music | A theme of his own as he arrives | The place's own | No song for him yet (plan 05) |
| The television crew | In the gate's room, before its far door | In its doorway on Hearthome's side, our own people with the original's flag | The gate's room is plan 01 · M11's |
| Looker in the Game Corner | Talks of Team Galactic's logo on the reels | Not there | The Game Corner's room is plan 06 · R16b's; Maylene's Badge sets his flag all the same |
| Looker in the Hotel Grand Lake's lobby | Asks the player to search outside | His flag is set and cleared, nobody stands there | The hotel's lobby is plan 01 · M11's |
| The Croagunk by Pastoria's board | Hops past now and then (one time in ten) | Left out (its flags are kept) | A Pokémon's walk past is a flourish no beat needs; plan 10 can bring it |
| The beauty's Suite Key | Taken, and a reward in her cottage | She talks of the key she lost | Her cottage is plan 01 · M11's |
| The meteorites | Change a Deoxys's forme | Are read | No command changes a Pokémon's form yet |
| The warehouse's rusty door | A door in the wall between the hall and the back room | The wall itself, with the door's line read on its two tiles | The room renderer draws no door inside a room, only the way out |
| Maylene's Mega Evolution | Not in Platinum | Left out | The later mechanics' beats wait for plan 06 · R20 (decision 06 · 2's default): it goes into `VeilstoneGym.txt` after her battle |
| The cameramen at Lake Valor | Keep the lake shut until the Canalave Library's meeting | Not placed | Placed with the scene that sends them off (plan 02 · S10), or nobody could reach the lake meanwhile |
| Maniac Tunnel | The Ruin Maniac digs on as more Unown forms are seen | His short cave only | No count of the Unown forms seen yet: plan 08 · P19 brings it, and the tunnel with it |

## The Pokétch's apps (2026-10-10, plan 06 · R14b)

**The frame**

- **Who gives what.** Every app Platinum gives runs. The Stopwatch and the Alarm Clock are in the original's list but given by nobody, so they stay in the list and never show (`Poketch.Runs`). The Pokétch Company's president gives the Memo Pad, the Marking Map, the Link Searcher and the Move Tester for the first, third, fifth and seventh Badge, one a visit, as the original's does (`PoketchCompany.President`). The other givers stand in rooms plan 01 · M11 builds (the Route 208 house, the Day Care, the Veilstone Department Store, the Celestic Town house, the Route 213 house, the Pastoria observatory gate, the Pal Park lobby and the Sunyshore house); until then they give nothing, and `if poketchapp X` is what their scripts will ask.
- **The stylus is a cursor.** The original's lower screen is touched while the player walks; here the Pokétch is taken in hand (I): the player stands still, the arrows move a cursor from one of the app's buttons to the nearest that way, and confirm touches it. A drag, a held touch and a double tap are told in the apps' own rulings below.
- **An app is made afresh each time it comes up**, as the original's task is, and keeps only what the original's save or Pokétch memory keeps (`Poketch.Recall`, `Keep`). It runs on while the Pokétch is put away, as the original's lower screen does.
- **The Analog Watch's touch** lights the dial for half a second, our own length.
- **The Link Searcher always finds nobody**: there is no wireless play yet (plan 07).

**The toys** (`src/applications/poketch/calculator`, `memo_pad`, `counter`, `coin_toss`, `roulette`, `dot_art`, `color_changer`, `kitchen_timer`)

- **A stroke is a row of touches.** The original's memo pad and roulette are drawn on with the stylus, a line followed from point to point (`UpdatePixelsOnPath`), on a page of 78 by 75 dots of two pixels. Here the Pokétch is touched with a cursor that moves a block at a time, so the page is 36 by 35 blocks (a dot of the original is about half a block) and each touch fills one block; the arrow keys held down walk the cursor on, so a line is a few presses. The memo pad's eraser rubs out two blocks by two, the original's four dots by four at this size (`ERASER_SIZE`), up and left of the block touched as the original's reaches mostly up and left of its point; the original rubs out only the single dot where a stroke begins, which a single touch can't tell apart from a stroke, so here every touch rubs out the whole square.
- **The counter goes round.** At 9999 the next touch counts nought (`State_UpdateApp`: `if (++value > 9999) value = 0`), as the original's does; it doesn't stop at its most.
- **What lasts between apps.** The counter's count, the coin's face and the kitchen timer are kept in the original's Pokétch memory (`PoketchMemory_Write32`), which holds one app's data and is forgotten whenever the side button chooses another app (`PoketchMemory_ResetActiveAppID`); it only carries an app over the lower screen being taken for something else (a battle, a menu). Here an app's state lives until another app is chosen (`Poketch.State`), which comes to the same. Only the dot art's picture is saved, as in the original (`Poketch.dotArtData`); the screen's colour is the Pokétch's own.
- **The dot art is saved as it is drawn.** The original writes the picture back when the app closes and when the game is saved (`SaveDotArtGrid` from `Free` and `SaveCallback`); here each touch keeps it at once (`Poketch.Keep`), which gives the same picture at every save. It is kept as each dot's shade less one, row by row, where the original packs four dots to a byte.
- **The dot art's second shade.** The original's four shades are four colours of the Pokétch's palette (`UpdateTilemap`: 4, 15, 8 and 1); the LCD here has three tones, so the second shade is the middle tone drawn faintly, as the unlit segments of a figure are.
- **A frame is a thirtieth of a second.** The coin's flight, the roulette's arrow and the kitchen timer's rings are counted in the original's frames: its main loop waits two of the screen's refreshes a pass (`src/main.c`), so its tasks run thirty times a second. The coin rises to 85 pixels and lies still at its 74th frame (about 2.5 s), the arrow is up to speed in 37 frames and stops about 85 after STOP, and the timer rings every 8 frames.
- **The roulette's one roll.** On STOP the original draws `MTRNG_Next() % 8` for a delay, but as written (`Task_RunSpinner`, state 3: `if (stopDelay == 0) stopDelay--; else next state`) only a roll of nought changes anything, by one frame more at speed. It is kept so; the roll is the field's generator (`PoketchContext.Rng`). Where the arrow stops is otherwise the player's timing.
- **The kitchen timer's clock.** The original counts the time from the console's clock (`Timer_GetCurrentTimestamp`), so it runs on through anything; here it counts the frames' own time, which goes on while the Pokétch is put away and while the field is played (the engine updates the app on the screen every field frame), and the time set is still whole seconds counted down from START, pauses left out. In place of the original's character beating its hands as it rings, two bells of our own swing.
- **The calculator's display.** Ten places don't fit at the size of the watch's figures (7 blocks wide), so the calculator has figures of its own, 3 blocks by 5; the error, which the original shows as a sign of its own in every place, is an "E" here. Its keys are labelled in the interface's font.
- **The calculator's cry.** `PlayResultSpeciesCry` plays a cry for a whole part from 1 to 493 that is a Pokémon seen (by the Sinnoh Pokédex's numbers until the National Pokédex is had). The app hands `PoketchContext.Cry` a Pokémon of that species made for the purpose, on a generator of its own seeded with nought, so hearing a cry draws nothing from the field's chance.

**The map apps: Platinum's rules, kept as they are** (`src/applications/poketch/dowsing_machine`, `berry_searcher`, `marking_map`, `trainer_counter`, `poketch_map.c`, `src/script_manager.c`, `src/pokeradar.c`, `src/overlay006/radar_chain_records.c`, `src/poketch.c`, `include/inlines.h`)

- **The Dowsing Machine** (`FindNearbyHiddenItems`, `FieldSystem_GetNearbyHiddenItems`): the hidden items not yet found in the player's place within seven tiles either side, seven up and six down; tiles are 11 of its pixels apart, and an item is shown where it lies when the touch is within its own range of it (8, 24 or 48 pixels by the item's range 0, 1 or 2: its own tile only, within two tiles, within about four), at most eight; if none is, an item within 48 pixels makes the ring go on and on. A step stops the ring and the items.
- **The Berry Searcher** (`GetReadyBerryPatches`): a cell for each patch that is growing (seen once) and in fruit, by the original's table of 118 cells; the patches after it in the same cell are passed over; 64 cells at most. It looks as it comes up and when touched, not in between.
- **The Marking Map** (`State_Idle`, `UpdateMarkerPriorities`, `sDefaultMapMarkers`): six markers, starting in a row in the sea south of the mainland; a touch picks up the marker within eight pixels (here: the touched cell or one beside it), the one moved last first. The roamers on the loose are shown at the original's place for each of their 29 routes (`PoketchMap_GetPositionFromMapID`); the player's cell is their tile over 32 (`PoketchMap_GetPlayerLocation`).
- **The Trainer Counter's records** (`GetLowestChainRecordSlot`, `TryReplaceLowestChainRecord`, `SortChainRecords`): three records; a chain takes the first empty one or the lowest as it begins and writes itself into it each time it grows past it, and the three are sorted best first, ties as the original's comparisons leave them. `RadarChainRecords_GetNumFilledSlots` asks the first record three times, so the records count as all filled or none; kept, with no effect a player can see, since only a record with a chain plays its cry.

**The map apps: our own choices**

- **The stylus is a cursor.** The Dowsing Machine is touched tile by tile (the original reads the touch to the pixel); the Marking Map's markers are picked up by touching their cell and put down by touching another, rather than dragged, and only onto the map (the original's can be dropped anywhere on the screen); both apps' every cell is a button, the cursor starting on the player's tile and on the map's middle. Picking a marker up and putting it down play the Pokétch's usual touch (`poketch`), which the original doesn't, so the cursor's touch is heard.
- **The map is a block a chunk**, 30 by 25 blocks in the middle of the screen, read from the region's habitats file, as Fly and the Pokédex lay Sinnoh out; the original's own pixel places (a roamer's route, a hidden island, the default markers) are turned into cells by the inverse of its `mapPositionsX` and `mapPositionsY`, to the nearest cell. A few roamer routes land a cell off our chunks of the route (Route 204, 214, 217): the original's places are kept.
- **What moves and how long** are our own timings: the Dowsing Machine's ring spreads in 0.6 s and its items blink for 0.8 s; the Berry Searcher's focus takes the original's 18 frames (six mosaic sizes, three frames each) as three steps of cells; the Trainer Counter's hop keeps the original's 16 frames and 24 pixels (five blocks).
- **The Berry Searcher's refresh sound** is the Pokétch's usual touch (`poketch`) for the original's sound 1656.
- **The Trainer Counter shows the chain as it is.** The original redraws the chain's count only when its species changes, which on the DS is enough because a battle (the only thing that grows a chain) tears the Pokétch down and builds it again; here the app stays up through a battle, so it reads the chain every frame.
- **The Dowsing Machine looks in the player's own place** (`map.AreaAt`), as the original lists only its map header's events: on the map of Sinnoh, an item across an area's border isn't found.

**The map apps: stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The radar's chain records | Kept in the save's special encounters, by the radar as the chain grows | Kept in the Pokétch's memory (`TrainerCounterApp.Follow`), which the engine calls whenever the chain changes, whichever app is showing | The radar's code and the save are outside the app |
| The hidden places' variables | Set by the story (`SystemVars_SetHiddenLocationMagic`) | Read, nothing sets them yet | The scripts that reveal them are later chapters' |

**The Pokémon apps: Platinum's rules, kept as they are** (`src/applications/poketch/friendship_checker/`, `daycare_checker/`, `pokemon_history/`, `move_tester/`, `matchup_checker/`, `src/overlay005/daycare.c`, `src/poketch.c`)

- **The Friendship Checker** (`GetFriendshipLevel`, `Init`): friendship under 1, 35, 70, 150, 200 and 255 is level 0 to 5, and 255 level 6. Levels 0 to 2 dislike the player with an intensity of 3 to 1, level 3 neither, levels 4 to 6 like the player with 1 to 3. A Pokémon wanders at one pixel a frame in a direction drawn from the generator and never faster than three; touched, it cries, and one that doesn't dislike the player shows its intensity in hearts and stays under the stylus, one that does keeps still. The stylus on the ground within 48 pixels draws those that don't dislike the player and sends the others away, at 100, 150, 175 or 200 in a hundred of the walking pace by intensity, until it is 64 pixels off. Two that bump trade their speeds along the line between them and ignore the stylus for 20 frames; one bumping into a Pokémon showing its liking is turned back (or nudged off at a tenth of a pixel a step if it was standing). Two quick touches of the ground make everyone jump 20 pixels in 23 frames, with the Counter's sound.
- **The Day-Care Checker** (`LoadDaycareSummary`, `SetLevelSprites`): the two Pokémon left, each with its level (at most 100; no hundreds under 100 and no tens under 10) and its gender's sign (none for a Pokémon with no gender), and an egg between them when the Day Care has one. It reads the Day Care as it comes up and again whenever it is touched, the picture coming back through ten steps of four frames.
- **The Pokémon History** (`Poketch_PokemonHistoryEnqueue`): twelve Pokémon caught, hatched or given, added at the end and the first dropped when it is full; shown oldest first, four to a row; one touched cries.
- **The Move Tester** (`GetExclamationCount`, `GetTypeAfterShift`): the move's type and the target's first type go round Platinum's seventeen in the original's order (Normal, Fire, Water, Electric, Grass, Ice, Fighting, Poison, Ground, Flying, Psychic, Bug, Rock, Ghost, Dragon, Dark, Steel); the second goes to none past either end and from none to the first or last. The marks are none if either type takes nothing, else three, one more for each type weak to the move and one fewer for each that resists it; a second type the same as the first counts once. What was chosen is kept for next time.
- **The Matchup Checker** (`BoxMon_GetPairDaycareCompatibilityScore`, `DaycareCompatibilityScoreToLevel`, `UpdateLeftMon`): two of the team, the first two to begin with; each side goes on through the team past the other's, and only with more than two. The answer is the Day Care's: never if either's first egg group is Undiscovered or both are Ditto; with one Ditto, a little from the same trainer and well from two; never for the same gender or one with none, or no egg group shared; then the best for the same species from two trainers, well for the same species from one or two species from two, a little for two species from one. The egg groups are the species', whatever its form. With fewer than two Pokémon the check beeps and does nothing.

**The Pokémon apps: stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| Touching the Friendship Checker | The stylus held down as long as the player likes, anywhere; a double tap is two taps of six frames or less | A touch holds the stylus where it fell for 1.5 s; the ground is six spots (the corners and the middles of the long sides); a second touch of the ground within 0.4 s of the first is the double tap | One screen and no touch: a cursor and a button (plan 06 · R14b's frame) |
| The Friendship Checker's walk | Icons may wander partly past the screen's edges (−10 to 217, −22 to 183), collisions found at the moment within a frame | Icons stay on the screen with room for their hearts; collisions are found frame by frame | Our screen doesn't clip what is drawn on it; the difference can't be seen at sixty frames a second |
| The Pokémon History's cries | Cries by species and form | A Pokémon of the species and form, level 1, made from the field's generator the first time it is touched | The app's `Cry` takes a Pokémon and the history keeps names |
| Sounds | The Day-Care Checker's `DENSI12`; the Matchup Checker's `POKETCH_012` (a step), `013` (no match) and `014` (the best) | `poketch`; `poketch`, `poketch_beep` and `dowsing_ping` | The bank has none of them (`docs/sound-effects.md`) |
| The Move Tester's chart | A table of its own, Platinum's | The game's own chart by the rules it is played by (`TypeChart`) | The same in a Platinum game; a modern game's Pokétch tells the modern chart |
| The Matchup Checker's button | Checks on the stylus's release, nothing if it was dragged off | Checks as it is touched | No stylus to drag |

**The Pokémon apps: our own choices**

- The Move Tester's words for how well a move works are our own: IT HAS NO EFFECT, IT BARELY WORKS, NOT VERY EFFECTIVE, IT HITS NORMALLY, SUPER EFFECTIVE, EXTREMELY EFFECTIVE.
- The Day-Care Checker's mosaic is a dissolve from the paper in cells of two blocks, fewer at each of the original's ten steps.
- The fish, hearts, egg, gender signs, arrows and marks are block patterns of our own.

## Poffins and the contest condition (2026-10-10, plan 06 · R14c)

**Platinum's rules, kept as they are** (`src/poffin.c`, `src/overlay083/ov83_0223F7F4.c` and `ov83_0223B5A0.c`, `src/applications/poffin_case/main.c`, `src/applications/poffin_berry_selection/`, `src/applications/pokemon_summary_screen/`, `src/scrcmd.c`, `res/field/scripts/scripts_poffin_*.s`, `scripts_hearthome_city_pokemon_fan_club.s`, `scripts_contest_hall_lobby.s`)

- **The condition** is six bytes of a Pokémon: cool, beauty, cute, smart, tough and the sheen, each 0 to 255. Feebas evolves on its beauty (170) at a level up, as since R10; a person's trade gives the original's (Charap's Chatot comes with 20 of each of the five and no sheen).
- **A Poffin** (`Poffin_MakePoffin`) is named for its flavours: one alone is that flavour's, two the stronger first (the first of the five on a tie), three rich, four or five overripe; any flavour of 50 or more makes it mild; none at all, or a mistake in the pot, makes it foul, with three of the missing flavours set to 2 by the game's generator and its smoothness kept. Its level (`Poffin_CalcLevel`) is the flavour it is named for first, or the strongest for a rich, overripe, foul or mild one, never over 99. Flavours and smoothness are bytes.
- **The pot** (`ov83_0223FDD8` and the functions it calls), thirty frames to the second: the stylus's stroke between two frames is measured along the place it came from (`ApproximateArcLength`, × 160), counting in full where it passes the four points straight across from the middle and nothing half way between them; a stroke in the very middle (within 16 pixels) counts half, one past the batter's rim (72 pixels, out to 88 as the batter speeds up) nothing, and the cooks' strokes are averaged and pushed in at 8, 7 and 7 parts in 204 for the three stages; the batter slows by 64, 72 and 80 a frame by itself, never past standing still, and goes no faster than 3640. It turns by `CalcRadialAngle(68, speed / 160)`, and a turn counts when it goes the arrow's way. A stage ends after 600 frames or 16 such turns. The arrow is drawn as the cooking opens and again 150, 120 or 90 frames later and up to 59 more by the stage, counting down only while the batter goes its way: it keeps its way with odds that lean further from it each time it is kept, and turns back to even odds when it changes. A second at 3640 spills the batter over, again and again, but in the last stage; three seconds at 910 or under warn that it will burn and each three more burn it, the time slow adding up over the whole cooking until it is counted (the original's counter is not cleared by stirring well, only by reaching its three seconds). Cooks stir together (more than one, every one in the batter and stroking hard, the batter at a good speed the arrow's way, each within 32 pixels of the first) after four such frames, each frame counting.
- **What comes out** (`ov83_0223FFD4`): the berries' flavours added up, each less the next round (spicy less dry … sour less spicy), each less one for every one of those below nought; four or five below nought, or two cooks with the same berry, make it foul. The flavours are scaled by the time (× 1,800,000 ÷ the frames, rounded to a whole per cent, so a minute is × 1 and half a minute × 2), rounded, less one for each burn and each spill, never below nought. The smoothness is the berries' average less the number of cooks, less a sixth of the frames stirred together × 0, 1, 5 or 10 for one to four cooks ÷ 10 (at most 10 off), and at least 15. Each cook takes home as many Poffins as there were cooks.
- **The case** (`PoffinCase`) holds a hundred, filled from the first empty place; one eaten or thrown away leaves the rest to close up in order. It lists newest first, under a flavour's tab those with any of it (`PoffinManager_FilterPoffins`). The Poffin House's cook asks for the case and a berry first (`CheckCanCookPoffin`: no berry, or a full case), and "cook another?" asks the same.
- **Eating one** (`PoffinCase_UpdateMonContestStats`): each flavour is added to its quality and the smoothness to the sheen, held to 255; the flavour the nature likes counts a tenth more and the one it dislikes a tenth less, multiplied in single precision and cut to a byte as the original does (`(u8)(attr × 1.1f)`, so 7 stays 7 and 10 × 0.9 stays 9); the Pokémon likes its trainer one point more. One whose sheen is 255 won't eat (`TryFeedPoffin`), however much room its qualities have. How it takes the Poffin (`PoffinCase_GetPoffinPreference`): happily when there is more of the liked flavour than of the disliked, sourly when less, plainly when they are the same or its nature likes nothing.
- **The summary's condition page** is shown once the Contest Hall has been visited (`PokemonSummaryScreen_ShowContestData`: `FLAG_CONTEST_HALL_VISITED`, which the lobby's first scene sets), and always while feeding. The sheen shows as twelve sparkles at most, `(12 << 8) / 255 × sheen >> 8` of them, all twelve at 255.
- **Who gives what**: the Pokémon Fan Club's chairman gives the Poffin Case once; the Contest Hall's boy at the official counter gives a Mild Poffin (60, 30, 30, 30, 30, smoothness 40) to a player with a case and room in it, the first time he is spoken to or later (`GivePoffin`). A Poffin cooked alone adds 3 to the Trainer Card's score (`TRAINER_SCORE_EVENT_UNK_12`).

**Our own choices**

- **The spoon.** The original is stirred with the stylus. Here the left and right arrows turn a spoon round the pot, 48 of the pot screen's pixels from its middle, 32 pixels a frame while held (fast enough to keep the last stage's batter going, as `PoffinCookingScreen.SpoonStep` says), and it stands still when let go; so the batter's speed is kept in its band by holding and letting go. The rules read the spoon as the stylus, the frame late the original's game hears its own stylus (its touch goes out through the link and comes back with everyone's). A tool can touch any point instead (`PoffinCookingScreen.Touch`).
- **The unit vector** of the stroke's measure is worked out in double precision and rounded to the console's fixed point; the SDK's own square root may differ by one part in four thousand, which can move a stroke's measure by a pixel at a boundary.
- **A speed gauge** beside the pot shows the batter's speed against the band that neither burns nor spills, which the original leaves to the look of the batter and the flames; the stage, the time and the burns and spills are shown at the side too.
- **The lines** are our own words on the original's beats (the warnings, the results, "cook another?", the case's messages, the people's lines). The Poffin House's idol, old couple and girl, the fan club's woman (the first Pokémon's friendship in the original's seven steps) and its three Pokémon of the field stand on the original's tiles.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| Cooking in a group | Up to four by wireless, or at the Wi-Fi Club; the sparkles and the bonus of stirring together | Only alone: the cook offers COOK, INFO and EXIT. The rules of a group are written and tested (`PoffinPot` takes up to four cooks) | Wireless play is plan 07's |
| The TV's Poffin corner | A reporter in the Poffin House and in the fan club, a programme of the Poffins cooked | The reporters are left out; nothing is broadcast | The TV is plan 08's |
| The Poffin Case's look | Its own touch screen of flavour buttons and a list of names | The interface kit's tabs and rows | One screen and no touch |
| Feeding | The party menu, then the summary's condition page, then a scene of the Pokémon eating | All in the case: the team's cards, the condition before, the line as it eats, the condition after with the change | The summary here is a page of the party menu |

## Breeding and what a personality decides (2026-10-10, plan 06 · R15)

**Platinum's rules, kept as they are** (`src/pokemon.c`, `src/overlay005/daycare.c`, `src/egg_hatch.c`, `src/overlay006/wild_encounters.c`, `src/trainer_data.c`, `src/scrcmd_daycare.c`, `src/scrcmd_party.c`, `src/unk_02092494.c`, `res/field/scripts/scripts_day_care_common.s`, `scripts_pokemon_day_care.s`, `scripts_common.s`, `scripts_eterna_city.s`)

- **The personality decides** (`Pokemon_InitWith`, `sub_02073E18`): every Pokémon the game makes draws a personality first (two 16-bit draws, the first the low half); its nature is its remainder by 25, its gender its low byte against the species' share of females in 256ths (31, 63, 127, 191, 223; always one, always the other, or none), its ability the second of two when its bit 0 is set, and it is shiny when its two halves and its first trainer's two exclusive-or to under 8. Its IVs then come from two more draws, three five-bit stats each (HP, Attack, Defense; Speed, Sp. Atk, Sp. Def). Nature, gender and shininess are still kept beside the personality, so a save from before keeps what it had.
- **Whose shininess**: a Pokémon met in the field, given, hatched or set roaming is shiny for the player's 32-bit number (the card's ID and the hidden half, `TrainerInfo_ID`; `PlayerIdentity.Number`); a trainer's never is (`OTID_NOT_SHINY`); a trade's comes with its own trainer and personality.
- **Synchronize and Cute Charm** (`CreateWildMon`): a nature asked for is drawn for, personality after personality (`sub_02074044`); a gender asked for is built (`sub_02074128`): the nature itself for a female, and for a male the next multiple of 25 past the species' share plus the nature, which is why Platinum's Cute Charm Pokémon are shiny for some trainers far more often than for others. The Poké Radar's sparkling patch is a personality found shiny for the player (`Pokemon_FindShinyPersonality`), found again until it also gives Synchronize's nature or Cute Charm's gender (`CreateWildMonShinyWithGenderOrNature`).
- **The Day Care** (`daycare.c`): two places; each step of the player's walks each Pokémon left a step, which it gains as a point of EXP when taken back, growing into the levels it reaches with each level's moves pushed in (the first move out when four are known) and no friendship for it. Taking one back costs 100 and 100 for each level grown. The Lady takes no Egg, not the player's only Pokémon (`CountPartyNonEggs`), not one that would leave the team and the boxes with only two able to fight between them (`CountAliveMonsAndBoxMons`), and not the last of the team able to fight; a Shaymin left goes back to its Land Forme.
- **Getting on** (`BoxMon_GetPairDaycareCompatibilityScore`): never for the Undiscovered group, two Dittos, the same gender, no gender beside a Pokémon that isn't Ditto, or no shared egg group; with one Ditto 20 from one trainer and 50 from two; the same species 50 from one trainer and 70 from two; two species 20 and 50. Every 256th step of the second Pokémon, with no Egg waiting, a draw of a hundred against that number finds an Egg (`Daycare_Update`), whose personality is drawn there and then.
- **The Egg's nature** (`Daycare_GetParentToInheritNature`, `Daycare_SetInheritedNature`): only the female's (or Ditto's, a coin between two Dittos) holding an Everstone, and then one time in two; personalities are drawn until one has that nature and isn't nought, at most 2,401 times. A personality of nought is no Egg at all, as in the original.
- **The Egg's species** (`Egg_DetermineEggSpeciesAndParentSlots`, `Daycare_AlterEggSpeciesWithIncenseItem`): the first of the female's line (`offspring`), or of the one that isn't Ditto; Nidoran♀'s and Illumise's lines give Nidoran♂ and Volbeat when bit 15 of the personality is set; Manaphy gives Phione; and the nine incense babies (Wynaut, Azurill, Mime Jr., Bonsly, Munchlax, Mantyke, Budew, Happiny, Chingling) only when a parent holds their incense, the species they grow into otherwise. It takes the mother's form; beside a male, Ditto is the mother, so the male is the father.
- **The Egg's IVs** (`Egg_InheritIVs`): random, then three picked from the six and taken from either parent on a coin. The original removes from its list the place of the draw rather than the stat drawn, so HP can only be the first pick, Defense only the first or second, and one stat can be picked two or three times: kept, as it is how Platinum breeds. No held item changes them in Platinum (the Power items' rule came with HeartGold and SoulSilver).
- **The Egg's moves** (`Egg_BuildMoveset`): its species' moves at level 1, then the father's egg moves of its species, in his order; then his moves a TM or HM teaches that its species can learn by that machine; then the moves both parents know that its species learns by level. Each goes into a free place or pushes the first move out. A Pichu also knows Volt Tackle when a parent holds a Light Ball (`Egg_TryGiveVoltTackle`).
- **The Masuda method** (`Egg_SetInitialData`): parents from games of different languages (`Pokemon.Language`), and a personality not shiny for the player, draw it again with the other generator (× 1,812,433,253 + 1) up to four times until one is.
- **Eggs**: level 1, in a Poké Ball, met at level nought, their hatch cycles kept in their friendship (`MON_DATA_FRIENDSHIP`). An Egg is never sent into battle ("An Egg can't battle!"), takes no item, medicine or machine, gains no friendship from walking or EXP, uses no field move, eats no Poffin, enters no Hall of Fame and is wanted in no trade; at the head of the team it brings no ability to bear on the wild Pokémon met (`isFirstMonEgg`), the Pokétch's friendship and matchup checkers pass it by, and the lottery doesn't read its trainer's number.
- **Egg cycles** (`Daycare_Update`, `Daycare_GetEggCycleLength`, `Party_GetEggCyclesToSubtract`): a cycle is 255 steps, or 230 on twelve days of the year (`sEggCycleSpecialDates`; its comment says New Year's Day but its number is the twelfth of January, and the number runs), counted on every step whether or not anything is at the Day Care. At its end each Egg of the team loses a cycle, two when a Pokémon of the team that isn't an Egg has Flame Body or Magma Armor (one when it has only one left); the first Egg found with none left hatches instead, and those after it wait for the next cycle. So an Egg of n cycles hatches after at most (n + 1) × 255 steps.
- **Hatching** (`Egg_CreateHatchedMon`, `FieldTask_HatchEgg`): "Oh?", the scene, and the Pokémon its personality and IVs make at level 1, with the Egg's moves, friendship 120, its species' name, in a Poké Ball, met where it hatched at level nought; it goes into the Pokédex as caught and adds 7 to the Trainer Card's score (`TRAINER_SCORE_EVENT_UNK_15`).
- **The couple's beats** (`DayCareCommon_Man`, `DayCareCommon_Lady`): the Man outside offers an Egg (twice; turned down twice, the couple keep it and it is gone), says how the Pokémon are and how well two get on in four sentences; the Lady inside raises one and asks about another, says how many levels each has grown, and gives one back for its fee (asking which when there are two, and whether the other too). Once a Pokémon has been left, a fan of the couple's waits by the table with the Day-Care Checker app.
- **Cynthia's Egg** (`EternaCity_CoordEvent_CynthiaTryGiveEgg`): once Commander Jupiter is beaten, a step east at x 308 brings Cynthia up the street with a Togepi Egg (`GiveEgg`); turned down, or with six Pokémon, she waits by the cycle shop, asks again when spoken to, and stops the player stepping past her.

**Our own choices**

- The couple's, the fan's and Cynthia's lines, the Egg Watch's four sentences and the hatching scene are our own, on the original's beats. The Lady doesn't walk to the back of the Day Care to fetch a Pokémon.
- An Egg counts as fainted for everything that asks who can fight (`Pokemon.IsFainted`), so the battle's rules and the field's checks pass it by without a check of their own; what would revive or heal it asks first whether it is an Egg.
- A form carried down is found by name: the mother's form's suffix on the Egg's species (Wormadam-Sandy lays Burmy-Sandy), never a form taken for a battle.
- An Egg is received from someone (`Pokemon.MetLocation`: "the Day-Care Couple", "Cynthia") until it hatches, when it is met where it hatched; the original keeps both.
- The Egg's species of a later species is the first of its line by PokeAPI, and its egg moves the newest game's.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| Mail held by a Pokémon left | Kept with it at the Day Care, and the Man remarks on the Mail | Nothing is said of Mail | Mail is plan 11's |
| A Giratina holding the Griseous Orb | The Lady can't take the orb off it, and says so | Taken as it is | The PC's handling of the orb comes with plan 01 · M11's rooms |
| A nickname for the hatched Pokémon | The naming screen is offered | Not offered, as none is offered for a Pokémon caught | No naming in the field yet (plan 12) |
| Spinda's spots | Drawn from its personality | Every Spinda the same | Its model is one sculpt; spots by personality are plan 09's |
| The modern rules' breeding | Egg moves from either parent, no TMs, the mother's ball, hidden abilities, the Oval Charm | Platinum's moves and ball; the Everstone, the Destiny Knot, the Power items and Masuda's five draws are the `Ruleset`'s | R19 and R28 bring the later items and abilities |

## The sixth chapter (2026-10-10, plan 02 · S9)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_route_210_south.s`, `scripts_route_210_north.s`, `scripts_celestic_town.s`, `scripts_init_celestic_town.s`, `scripts_celestic_town_cave.s`, `scripts_celestic_town_north_house.s`, `scripts_fuego_ironworks_building.s`, `scripts_route_218.s`, `scripts_route_219.s`, `scripts_route_221.s`, `scripts_canalave_city.s`, `scripts_init_canalave_city.s`, `scripts_init_new_game.s`)

- **Cynthia comes up Route 210 behind the player** once the Psyduck have gone, from the tile below whichever of the two the player faced them from, and asks until the answer is yes that the Old Charm be taken to her grandmother (`FLAG_USED_SECRETPOTION`). The Secret Potion isn't taken from the bag, as the original's script takes nothing.
- **The trainer resting by the Café Cabin gives TM51** (Roost) once (`FLAG_RECEIVED_ROUTE_210_SOUTH_TM51`).
- **Celestic Town's grunt asks first** whether the player means to get in his way; a no leaves him there. Beaten, he runs off past the player by the way the original's tables give for each side, the elder comes up from the south of town to stand beside the player and takes the Old Charm (`FLAG_DELIVERED_OLD_CHARM`), and the ruins' door is open.
- **Cyrus is met in the ruins' cave**, as the painting is first read (`FLAG_EXAMINED_CELESTIC_TOWN_CAVE_PAINTING`): the elder comes in to tell its legend, Cyrus after her. Said no, he shoves the player aside and stands before the painting, and spoken to he asks again; beaten, he goes, the elder gives **HM03** (Surf; the Fen Badge lets it be used outside battle) and goes home (`FLAG_HIDE_CELESTIC_TOWN_NORTH_HOUSE_ELDER` cleared), and Cynthia waits outside (`VAR_CELESTIC_TOWN_STATE` 1).
- **Cynthia's word outside the ruins lifts Route 218's blockade** (`FLAG_HIDE_ROUTE_218_BLOCKADE`, state 2): a guitarist, a fisherman and their Clefairy and Pikachu, rehearsing a show before the gate to Canalave City, who take no notice of the player. Reaching Canalave City takes her away from Celestic Town (`FLAG_HIDE_CELESTIC_TOWN_CYNTHIA`, Canalave's `OnEnter`, as the original's OnTransition).
- **Arriving in Celestic Town unlocks the Vs. Seeker's second level of rematches** (`FLAG_UNLOCKED_VS_SEEKER_LVL_2`), as the original's OnTransition does; and the assistant's Vs. Seeker on Route 207 now sets the first (`FLAG_UNLOCKED_VS_SEEKER_LVL_1`), which plan 02 · S7's scene had left out.
- **The rival battles on Canalave City's bridge** as the player first crosses it (the original's trigger, a column of four tiles, `VAR_CANALAVE_CITY_STATE` 0), with the team that has the upper hand on the player's starter, tells the player to train on Iron Island and runs off (state 1). Byron's Badge brings him back outside the Gym (`CanalaveGym.txt` sets state 2 and clears his flag; the library's scene is plan 02 · S10's).
- **The Fuego Ironworks has no Team Galactic in Platinum** (Diamond and Pearl's Commander Mars isn't there): Mr. Fuego gives his first visitor a Star Piece and trades a Shard of each colour for each Star Piece, ten at once for ten or more (`FLAG_RECEIVED_FUEGO_IRONWORKS_BUILDING_STAR_PIECE`, `FLAG_MR_FUEGO_ASKED_FOR_TRADE`); its workers battle with Platinum's teams, its floors move the walker as the original's do.
- **Route 221's workers** stand at Pal Park's door until the National Pokédex, whose OnTransition hides them (plan 02 · S15, with Pal Park).
- **Saves from before**: `common.ChapterSix` (story version 11) hides the chapter's people until their scenes (the rival's flag is left as Byron's script left it in a save with the Mine Badge). A save whose Psyduck were cured before this chapter, when Cynthia didn't come after them, is given her Old Charm and her errand; one that has been to Canalave City (`FLAG_FIRST_ARRIVAL_CANALAVE_CITY`) has Route 218's blockade gone, so it isn't shut in; one that holds the Vs. Seeker has its first level of rematches.

**Our own choices**

- **The ruins' cave and the ironworks' floor are maps of the imported world** (`CelesticTownCave`, matrix 169; `FuegoIronworksBuilding`, matrix 8; `world.json`'s `Cave` setting), the original's own plans with their warps, items, triggers and moving floors, drawn as caves until their places have looks of their own. The elder's house is a room rebuilt to the original's plan (`CelesticTownNorthHouse`), its book and its scroll read as signboards.
- **A scene that waits for the player at a door** plays only where the original's OnFrame finds them: Cynthia's comes as the player steps out of the ruins' cave onto the tile before its door; anyone who came back to Celestic Town another way (Teleport out of the cave) finds her standing where she waits, with the blockade lifted all the same.
- **Cyrus and the elder stand before the painting again** whenever the player comes back while Cyrus is still there (the cave's `OnEnter`), as the original lays its objects out afresh: a battle lost or a challenge refused doesn't leave them wherever the scene had walked them.
- **The elder's warning on the road says nothing once the Old Charm is delivered**: the original's trigger would still tell of a spaceman long gone.
- **Without the Old Charm** (a player who reached Celestic Town by Mt. Coronet, never meeting Cynthia on Route 210), the elder thanks the player and sends them to the ruins all the same, as the original's script, which takes the charm without asking, would.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The elder's look | An old woman of her own (`expert_f`) | The lady (`Lady`) | The second half's named cast is plan 11 · C6's, and the old woman's look plan 11 · C2–C4's |
| The ruins' painting | A great painting on the cave's back wall | A slab of the cave's rock (`Outcrop`) where its model stands, read from the two tiles before it | No art for it yet |
| The ironworks' floor | A factory floor of steel, with furnaces | Cave floor and rock, its crates and drums, the furnace a steel tower (`Headframe`) | No look of its own yet; the original builds it as one of its dungeons |
| The Pokédex upgrade | The assistant's father in the gate between Route 218 and Canalave City shows male and female forms | Not there | The gate's room is plan 01 · M11's, and the Pokédex shows no genders yet |
| The Old Rod | A fisherman in the gate between Jubilife City and Route 218 | Not given | The gate's room is plan 01 · M11's: the gates are walked through |
| Cynthia before the painting | Her theory of the painting, after Charon's arrest on Stark Mountain | Not placed | Done since: plan 02 · S15, part c |
| The elder's research | The Adamant and Lustrous Orbs' passage, after the Hall of Fame | Not there | Done since: plan 02 · S15, part c |
| Grandma Wilma's house and Celestic's other houses | Draco Meteor, the glasses shop | Doors shut | Plan 01 · M11 |
| Z-Moves at Celestic Town | Not in Platinum | Left out | The later mechanics' beats wait for plan 06 · R21 (decision 06 · 2's default): it goes into `celestic_town_cave.txt` after Cyrus's battle |

## Super Contests (2026-10-10, plan 06 · R17a)

**Platinum's rules, kept as they are** (`src/contest.c`, `src/unk_02094EDC.c`, `src/unk_020298BC.c`, `src/applications/party_menu/main.c`, `src/overlay017/ov17_0223F118.c`, `ov17_02243AC4.c`, `ov17_02245F14.c`, `ov17_02246ECC.c`, `ov17_0223B140.c`, `ov17_0223DAD0.c`, `ov17_0224A0FC.c`, `ov17_0224CFB8.c`, `ov17_0224E930.c`, `ov17_02250744.c`, `src/overlay022/ov22_02255D44.c`, `res/prebuilt/contest/data/contest_data.narc`, `res/moves/*/data.json`, `res/field/scripts/scripts_contests.s`, `scripts_contest_hall_lobby.s`). All of it is decompiled to C; its functions and fields are mostly unnamed, and the names here are ours.

- **Who may enter** (`CheckContestEligibility`): not an Egg, not fainted, two moves at least, and as many of the contest's own ribbons as there are ranks under the one entered (the Normal Rank asks none). Four ranks (Normal, Great, Ultra, Master) of five contests (Cool, Beauty, Cute, Smart, Tough): twenty ribbons, named as the original names them (`RIBBON_COOL`, `RIBBON_CUTE_GREAT`).
- **Setting one up** (`Contest_Init`), every draw 16 bits of the game's generator, in this order: the theme (`sub_02095A74`: one of three at the Normal Rank, six at the Great, nine at the Ultra, any of the twelve at the Master), the judge in the middle (`sub_020954F0`: one of those the rank's table gives a 2 for the contest; the two with a 1 sit at the sides), then the three opponents (`sub_02094F04`: of the 96 contestants those of the rank who enter the contest and turn up at this time of the story, drawn one by one; one "guest" at most, drawn first and put in place of one of the three). Their Pokémon are level 10 with the table's moves, nickname and condition, the trainer's gender where the species has two, and never shiny (`sub_02095380`). "This time of the story" is the game completed and the National Pokédex obtained.
- **The Visual Competition** (`ov17_0223F374`, `ov17_0223F334`, `sub_02095928`, `sub_0209598C`): the condition scores the contest's quality plus half of its two neighbours and the sheen together (Cool's neighbours are Tough and Beauty, and so on round), a tenth more with the quality's own scarf and a twentieth with a neighbour's; a heart for each of the rank's eight steps reached (10 to 80 at the Normal Rank, 320 to 600 at the Master). The dressing-up scores the theme's points for each accessory worn (each theme's file gives every accessory 0 to 2), as many accessories as the rank allows (5, 10, 15, 20), in sixty seconds; one to four hearts by the rank's three steps (3, 5, 8 at the Normal Rank; 10, 20, 30 at the Master), none for none. The opponents wear the dressing their row keeps for the theme.
- **The Dance Competition** (`ov17_0224D710`, `ov17_0224DE54`, `ov17_0224B20C`): thirty frames to the second. Each contestant leads in turn, the fourth entry first and the player last, for two bars. A bar is sixteen beats (twelve for the two tunes in three time); in its first half the lead dances up to three steps (four from the Great Rank), in its second the others repeat them, the same step half a bar later. A step is measured against the nearest half-beat (`ov17_0224DDE4`): within two frames it is excellent (2 points), within three good (1), else a miss; one and two frames at the Ultra and Master Ranks but for the Beauty Contest's slow tune. An answer where the lead made no step, or with another step, scores nothing. A step is read again a beat less two frames after the last. Seven tunes by rank and contest (120 beats a minute at the Normal and Great Ranks; 100, 60, 150, 132 and 140 for the five contests above).
- **How the opponents dance** (`ov17_0224E990`, `ov17_0224EE90`): each has a generator of its own, seeded from the game's as each lead's turn begins. A lead picks its half-beats (whole beats only under the Ultra Rank; never two next to one another above it, and off-beats half as often), each a few frames off by its looseness (its way of dancing, 1 to 4, doubled at the Normal Rank, halved at the Ultra, a third at the Master), and its steps (the same again 90 times in 100 at the Normal Rank, 40 at the Great). An answer is planned as far off as the dancer is loose, and is the wrong step so many times in a hundred: 3 for the step that way of dancing fumbles, 8 for a change of step, 2 off the beat of the one before, 5 long after it, all doubled under the Ultra Rank.
- **The Acting Competition** (`ov17_0223BE58` to `ov17_0223C888`): four turns. Each turn everyone chooses a move and one of the three judges, then performs in the turn's order. A move's own points come from its effect (three hearts for a plain act, two for most, one or none for those that reckon their own; ten points to a heart); its effect goes off (below); a score doubled by the turn before is doubled; then the judge's voltage moves: up a step for a move of the contest's own quality, down a step for the two it clashes with (Cool against Cute and Smart, and so on round), nothing for its neighbours. A voltage filled (five steps) pays five hearts, eight with the judge in the middle, and starts again. Once all four have performed, each judge gives three hearts to a Pokémon alone with them, two each to two, one each to three, none to four. No move may be shown two turns running.
- **The 23 effects** (`ov17_02246578` to `ov17_02246E68`), each as the original reckons it: first or last next turn; +2 if the move raises the voltage; +3 with the judge to itself; may be shown twice running (not thrice); the judge's voltage added; +15 when all four chose one judge; every judge's voltage down a step; the next turn's score doubled; the bonus of the act before for filling a voltage taken too; no voltage up, or none down, for the rest of the turn; the next turn's order shuffled; double if performed last; more the lower the voltage (4, 3, 2, 1, 0 hearts); +2 first; +2 last; +3 when the act before raised the voltage too; more the later (1 to 4 hearts); +3 right after a voltage is filled; +3 for the least of the turn. `CONTEST_EFFECT_DOUBLED_JUDGE` (+2 for each rival with the same judge) is in the table and no move has it.
- **The next turn's order** (`ov17_02243B0C`): the lowest score of the turn goes first, and of two level the one who performed later; a move that asked to go first or last, or shuffled the order, overrides the score. The first turn's order is the best of the looks and the dance together first (`ov17_0223EEB8`).
- **How the opponents act** (`ov17_02246F9C`): the original's table of 165 rows, copied number for number (`ActingAiTable`): each row holds for one place in the order, asks one of 28 questions of the stage, and adds its weight (one of four, by the contestant's way of thinking) to the moves it names and the judges its question marked. The heaviest move wins and then its heaviest judge, ties by draws; a move with no judge preferred shies from the judge the player chose 230 times in 256 at the Normal Rank, 128 at the Great, 51 at the Ultra, never at the Master.
- **The results** (`ov17_02251930`, `ov17_02251A1C`): each round is worth 3,333 of 10,000. The round's best score divides its worth, in whole numbers, and each score takes that many hundredths, rounded; the shares become bars, a whole percent of the round's best share of 64 dots; the bars together place the four, and of two level a draw each decides.
- **What a contest leaves** (`Contest_EndContest`, `Contest_CalcFirstTimeVictoryAccessoryReward`): the winner's ribbon, the friendship of a win (+3, +2, +1), 7 points of the Trainer Card's score, the opponents' Pokémon seen in the Pokédex, a flag for a win at the Master Rank, and the rank's accessory (a barrette, balloons, a hat, a stage in the contest's colour) when the Fashion Case has room, which it has once. A practice round (one of the three, at the Normal Rank, against the practice rounds' own opponents) leaves nothing, and only says how many did better.
- **The Fashion Case** (`FashionCase`): nine of each of the first 61 accessories, one of each of the other 39. Keira gives the first, a Glitter Powder, in the Contest Hall's first scene (`Common_GiveAccessory`).

**Quirks kept**

- Two of the opponents' questions ask whether it is "turn four", which the turn counter (0 to 3 while a turn is chosen) never says: their rows never fire.
- A move an opponent may not show is an empty place whose quality reads as Cool (nought); the questions that ask whether it has a move of the contest's quality count it, so in a Cool Contest they always say yes.
- An opponent leading the dance at the Ultra or Master Rank reads one place past its list of half-beats when it tries the last one (the original's array ends there): read as free.
- "The least of the turn" compares a contestant's points once the judges have given theirs with the others' points as last looked at, which for those who perform after it is before their own judge's effect: kept as the original walks it.
- A round nobody scored in divides by nought in the original; here nobody gets anything for it.

**Our own choices**

- **One screen for the whole contest**, in the menus' look, with the Pokémon as their menu sprites: the original's stage, its judges, its crowd, the Pokémon's move animations and the photographs are not drawn (style guide, "Super Contests"). Every line is our own, on the original's beats (the introductions, the theme, the dance's and the acting's announcements, each effect's two lines, the judges' remarks, the winner).
- **The dressing-up is a list, not a canvas.** The original's accessories are dragged onto the Pokémon with the stylus; where they are put changes the picture, not the score. Here the case is a grid: confirm puts the accessory under the cursor on, cancel takes the last one off, Enter ends it. The player's accessories are drawn in a ring round the Pokémon, an opponent's where its dressing puts them.
- **The dance is stepped with the four arrow keys**, one for each of the original's four touch buttons, in their order: up for the jump, down for the step forward, left and right. The bar on screen (the lead's steps and where each is to be repeated) is ours; the original shows the steps as they come.
- **The dance has no tune yet.** The original's seven are music (plan 05); the Pokétch's tick sounds on each beat instead, at the tune's tempo.
- **The acting's judge is chosen after the move**, with the arrows, where the original has three buttons on the touch screen. The moves' cards say whether the move raises or lowers the voltage in this contest, which the original leaves to the player to know.
- **The receptionist's explanations end the talk**, where the original goes back to its menu: the tests play every way through a script, and menus that lead back to menus are hundreds of ways. A Pokémon that may not enter is told why and the player asked whether to cancel, where the original's party screen marks it NOT ABLE before it can be chosen.
- **The masks' names** (Turtwig Mask, Chimchar Mask, Piplup Mask) are written as this game writes species, not in the original's capitals.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The stage | The contest is held in a room of its own (the Contest Hall's stage, with the four contestants as people, the camera flashes and the applause by fame) between its rounds | The receptionist's script goes straight into the contest's screen and comes back to the counter | The stage's room is plan 01 · M11's; the applause is a sound of plan 05's (the Pokémon's fame is reckoned and kept, `Contestant.Fame`) |
| The contest clothes | The player changes into a tuxedo or a dress, given by their mother in the lobby's first scene | Not given, not worn | The player's clothes are plan 11's |
| Link Contests | The left counter: four players by wireless, with a rank and judges of their own | The receptionist says none are held | Plan 07 |
| The accessories' pictures | A sprite each | A sign of nine shapes in the accessory's colour | Their art would be a sheet of a hundred (plan 09) |
| Backdrops | The Fashion Case keeps backdrops too; the opponents' dressings name one | The dressings' backdrop is imported and nothing uses it | The dressing-up photographs are plan 06 · R17b's |
| The winner's photograph | The winner's dressed-up picture is kept and hung on the lobby's boards | The boards say what they said | With the photographs (R17b) |
| Where accessories come from | Amity Square, the Floaroma flower shop (for berries), the Veilstone massage girl, Jubilife TV's dress-up room | Only Keira's Glitter Powder, the prizes and `giveaccessory` in a script | Amity Square is plan 06 · R16's; the shops' and houses' rooms are plan 01 · M11's |
| The Fashion Case in the bag | Says how many accessories and backdrops it holds | The key item does nothing when used | R17b, with the Ball Capsules' screens (done: "Ball Capsules, Seals and ribbons", below) |
| The TV, the Journal and the records | The contest is a TV programme, a line of the Journal for a Link Contest, and counts in the game's records | Not kept | Plan 08's TV; the records are R17b's with the ribbons' screen |
| Chatot's cry | A recorded Chatter plays in the acting | Not there | No microphone |

## The seventh chapter (2026-10-10, plan 02 · S10)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_iron_island.s`, `scripts_iron_island_b2f_left_room.s`, `scripts_canalave_city.s`, `scripts_init_canalave_city.s`, `scripts_canalave_library_1f.s` to `3f.s`, `scripts_valor_lakefront.s`, `scripts_lake_valor_drained.s`, `scripts_valor_cavern.s`, `scripts_verity_lakefront.s`, `scripts_lake_verity_low_water.s`, `scripts_lake_verity.s`, `scripts_mt_coronet_1f_north_room_1.s`, `scripts_route_211_east.s`, `scripts_route_217.s`, `scripts_twinleaf_town_player_house_1f.s`, `scripts_init_new_game.s`)

- **Riley gives HM04 at the tunnels' door on Iron Island**, in Platinum, to whoever speaks to him, with or without Byron's Badge (what he says depends on it), and goes inside (`FLAG_HIDE_IRON_ISLAND_RILEY`). Strength outside battle waits for the Mine Badge (plan 02 · S2's rule).
- **Riley travels with the player on the left room of the island's second basement**, from the original's trigger just inside it to the two Team Galactic grunts before the lift (`VAR_IRON_ISLAND_B2F_LEFT_ROOM_FOLLOWER_RILEY_STATE` 0, 1, 2): as Platinum's `riley_iron_island` (a Lucario of level 41), the team healed after every battle, wild Pokémon two at a time, the room's eight trainers in four pairs for the two of them. Turning back toward the way out leaves him where he first stood. The grunts battle the pair (`galactic_grunt_iron_island_1` and `_2`); talking to either starts the same scene, as the original's objects run the trigger's script.
- **The Riolu Egg** is offered after that battle and asked again while the answer is no; with six in the team Riley keeps it (`FLAG_COULD_NOT_RECEIVE_RIOLU_EGG`) and waits by the lift, where the grunts stood, until there is room (`FLAG_RECEIVED_RIOLU_EGG_FROM_RILEY`).
- **Byron's Badge sends the rival to the library**: as the player comes out of the Gym he calls them there and runs (`VAR_CANALAVE_CITY_STATE` 2 to 3, `VAR_CANALAVE_LIBRARY_STATE` 1), and then keeps the library's door until the player has HM04 or a Pokémon that knows Strength, so Iron Island can't be skipped.
- **The meeting on the library's third floor** shares out the lakes (the assistant with the professor to Lake Verity, the rival to Lake Acuity, the player to Lake Valor) and ends in the explosion: `FLAG_LAKE_VALOR_EXPLODED`, the cameramen gone from Valor Lakefront, the professor, the assistant and the rival outside the library (state 4), where a sailor brings the news (state 5; the two who wait have gone to Lake Verity by the next visit, state 6).
- **Lake Valor is the drained lake** from the start of the game until the story fills it again: the lakefront's warps lead to `lake_valor_drained` while `FLAG_GALACTIC_LEFT_LAKE_VALOR` is unset (the cameramen are what keeps anyone from seeing it before the explosion). Commander Saturn waits in Valor Cavern; beaten, he and the grunts who only talk are gone (`FLAG_HIDE_LAKE_VALOR_GALACTIC`), the three grunts who battle stay, and `FLAG_DEFEATED_COMMANDER_SATURN_VALOR_CAVERN` turns Verity Lakefront's way in.
- **Lake Verity is two maps too**: the lake as the game begins (`lake_verity_low_water` in the decompilation's naming), where the professor and the assistant find nothing after the explosion, and the lake Team Galactic holds (`lake_verity`), which the lakefront leads to once Saturn is beaten. There the professor calls to the player as they come in (`VAR_LAKE_VERITY_PROF_ROWAN_STATE`), and Commander Mars, beaten, leaves with everyone of Team Galactic (`FLAG_HIDE_LAKE_VERITY_TEAM_GALACTIC`, `FLAG_TEAM_GALACTIC_LEFT_LAKE_VERITY`, `VAR_LAKE_ACUITY_STATE` 1, `FLAG_HIDE_LAKE_ACUITY_JUPITER` cleared).
- **What keeps the north shut is two grunts in Mt. Coronet**: they stand across the northern hall's way to the stairs down (and so to Route 216) until Mars is beaten (`FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNTS_M`), talking of the fog below. Rock Climb, for Acuity Lakefront's rock face, waits for the Icicle Badge.
- **HM08 lies in a ball on Route 217** in Platinum (the hiker who lost it gives an Icicle Plate to whoever found it, in his house: plan 01 · M11), **Maylene catches the player up** at the route's south end on her way to Snowpoint City (`VAR_ROUTE_217_STATE`), and **Route 211's trainer gives TM77** once (`FLAG_RECEIVED_ROUTE_211_EAST_TM77`).
- **Saves from before**: `common.ChapterSeven` (story version 12) hides the chapter's people until their scenes; the library's people are left as Byron's script left them in a save with the Mine Badge. A save that has been to Snowpoint City (`FLAG_FIRST_ARRIVAL_SNOWPOINT_CITY`) or holds the Icicle Badge has Mt. Coronet's grunts gone and Maylene past, so it isn't shut in the north.

**Our own choices**

- **A way the story switches** (`Warp.Until`, `Warp.From`, an overlay's `switches`): the original keeps two warps on the same tiles and moves one of them off the map by script each time the place is come to. Here both are kept, each with the flag it is there until or from, and `Map.ApplyDoors` shuts the one the story isn't at whenever a flag changes.
- **The drained lake, Valor Cavern and the lake Team Galactic holds are maps of the imported world** (`LakeValorDrained`, matrix 104; `ValorCavern`, matrix 106; `LakeVerityGalactic`, matrix 102), the original's own plans with their people, trainers, items and warps. The Canalave Library's three floors are rooms rebuilt to the original's plans.
- **A scene that waits for the player at a door** plays only there, as in the sixth chapter: the rival's call outside the Gym and the news outside the library. Anyone who comes to Canalave City another way in those states finds the story moved on without the words (the rival gone to the library; the news heard).
- **The sailor stops two tiles nearer the professor** than the original's, beside the rival: where the original stands him, the text box covers him.
- **Mt. Coronet's grunts are gone for a save that comes up the stairs behind them** (the hall's arrival script): nobody gets north of them in a game of today, so whoever arrives there is a save from before the chapter.
- **Riley is taken off the map behind a fade** once the Egg is given; the original leaves him standing until the room is come to again.
- **Six of the lake's twenty-three Magikarp have nothing to say**: they lie in the middle of a heap of eleven in the lake bed's north-west corner, where nobody can walk up to them.
- **Maylene's flag is set and nothing reads it**: the original takes her out of her own Gym from this meeting until the Icicle Badge (`FLAG_HIDE_VEILSTONE_CITY_GYM_MAYLENE`); the Gym's map (plan 01 · M9's) gives her no hiding flag, so she is in both places until it does.
- **The library's books are Sinnoh's myths retold**: seven books on the original's tiles, each in our own words on the original's subject (a myth of people and Pokémon in the tall grass, the three who dived into the lakes, the three unnamed, the young man of Veilstone and his sword, the Original One, a horrific myth, three folk stories). The folk stories are chosen from a menu, where the original draws one of the three at random.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The explosion | The library shakes, books and all, with a boom | `sound "thunder_rumble"`, `camera shake` and a line of the game's own voice | The story's effects are plan 09 · L11's, which has not landed |
| The television's news | A broadcast shown on the television's own screen (`StartLibraryTV`) | The report's lines under "TV" over a black screen, as the first chapter's special | Plan 09 · L11 |
| Riley's look | His own (`riley`) | The clerk (`Clerk`): a grown man in a blue jacket | The second half's named cast is plan 11 · C6's |
| Commander Saturn's look | His own (`saturn`) | A grunt (`Grunt`) | Plan 11 · C6 |
| The sailor's and the cameramen's looks | `sailor`, `cameraman` | The gentleman (`Gentleman`) | Plan 11 · C2 to C4 (left to the fallback, a sailor is the boy the player is) |
| Team Galactic's theme at the lakes | `D_GINLOBBY` plays at both lakes and in Valor Cavern until Team Galactic leaves (`FLAG_ALT_MUSIC_LAKE_VALOR`, `FLAG_ALT_MUSIC_LAKE_VERITY`, which are set all the same) | The lakes' own theme, and the cave's | No such song yet (plan 05 · A6) |
| A partner's jingle | `SEQ_GONIN` as Riley joins | The level-up fanfare, as for Cheryl and Mira | Plan 05 |
| The Bosses' words in battle | Saturn and Mars speak as their last Pokémon comes out | Nothing | Plan 11 · C12 |
| The drained lake's bed | Mud and rock, with the bomb's scorch marks | The import's ground for it (rock, puddles, the trees of its islets); the scorch marks (`bomb_mark`) draw nothing | No art for it yet (`Data/WorldModels.cs`) |
| Lucian in the library | He reads on the first floor after the Hall of Fame | Not placed | Done since: plan 02 · S15, part a |
| The library's hiker | He tells of the plates he found, once the player has Arceus from a distribution | Not placed | An event the original gave out by distribution |
| Verity Cavern, Acuity Cavern, Azelf | The guardians wait in their caverns after the Distortion World | Verity Cavern's mouth is shut; Valor Cavern is empty once Saturn has gone | Plan 02 · S13 |
| The houses of Routes 216 and 217 | A lodge to rest in; an Icicle Plate for whoever found HM08; a Spell Tag | Doors shut | Plan 01 · M11 |
| Byron in Iron Island's house | He gives a Metal Coat after the National Pokédex | Door shut | Done since: plan 01 · M11, part b (the cabin) and plan 02 · S15, part a (Byron) |

## Ball Capsules, Seals and ribbons (2026-10-10, plan 06 · R17b)

**Platinum's rules, kept as they are** (`src/ball_seal_info.c`, `src/applications/capsule_menu/main.c`, `manager.c`, `placement.c`, `graphics.c`, `src/pokemon.c`, `src/battle_anim/ov12_02235E94.c`, `ov12_02237E54.c`, `src/unk_0202CC64.c`, `src/ribbon.c`, `src/scrcmd_party.c`, `src/party.c`, `src/trainer_case.c`, `src/contest.c`, `src/box_app_manager.c`, `src/shop_menu.c`, `include/data/mart_items.h`, `res/trainers/ball_capsules.json`, and the scripts of Solaceon Town's east house, the Sunyshore Market, Sunyshore City's north-east house, Route 213's Footstep House, the Ribbon Syndicate, the Contest Hall's lobby and Jubilife City).

- **The Seal Case** (`SealCase`): eighty kinds of Seal and twelve Ball Capsules of up to eight Seals each. Ninety-nine of a kind at most, counted with those stuck on capsules (`GiveOrTakeSeal` refuses what would pass it); a Seal stuck on a capsule is out of the case until it is taken off again.
- **A capsule** is a list of Seals, each a kind and a place in the dots of the capsule's own picture, the ball's middle at (190, 70). A Seal dropped within sixty dots of the middle is on the ball; further out it goes back into the case. The first of the list is in front.
- **The capsules' menu** by what a capsule carries (`CapsuleGraphics_OpenCapsuleSelectionMenu`): one on a Pokémon's ball offers taking it off first; one with Seals and on nobody's ball putting it on first; an empty one only changing it, swapping and leaving. Over the twelve, left and right go round all of them and up and down stop at the first and last rows.
- **On a Pokémon** (`Pokemon_SetBallSeal`, `MON_DATA_BALL_CAPSULE_ID`, `MON_DATA_BALL_CAPSULE`): the Pokémon holds the capsule's number and its own copy of the Seals. Only a capsule with a Seal on it can be put on, never on an Egg, and only on a Pokémon of the team; a capsule already on another Pokémon comes off that one. Changing a capsule that is on a ball and keeping the change renews the Pokémon's copy; swapping two capsules takes their Pokémon with them. A Pokémon whose ball has a capsule doesn't go into a box (`box_app_manager.c`: the capsule has to come off first), and the Shedinja a Nincada leaves has none.
- **The burst** (`ov12_02237EA0`, `ov12_022365D4`): as the ball opens, each Seal goes off after a delay by how far from the ball's middle it is stuck: at once within twenty dots, then eight, fourteen and twenty frames (of thirty a second) for each twenty dots more, twenty for anything within four dots of the edge; a letter always after eight. It goes off as far right of the Pokémon's place and as far above it as it is stuck from the ball's middle, thirty dots higher.
- **Trainers' capsules** (`res/trainers/ball_capsules.json`, `ball_seal` in a trainer's data): 105 Pokémon of trainers' teams come out of one of the trainers' own 128 capsules: Volkner's Electivire, Flint's Magmortar, the rival's starter from Pastoria City on, and one Pokémon each of some two dozen trainers of the routes (Ace Trainers, Rich Boys, a Lady, an Idol, a Clown).
- **Where Seals come from**: the mother in Solaceon Town's east house gives the Seal Case with five Seals, each a draw (one of the six Heart Seals, two of the six Star Seals, one of the four Foamy Seals, one of the seven Song Seals). Her boy gives ten of a letter's Seal for the first Unown of the team in that letter's shape (as many as bring the kind to ninety-nine; "!" is the Shock Seal and "?" the Mystery Seal), and not twice running for one shape, which he forgets once the player leaves. The merchant of the Sunyshore Market sells seven Seals a day by the day of the week (`sSealShopStock`), at the prices of `sealTypeValues` (50 or 100), as many at a time as the money and the ninety-nine allow.
- **A capsule kept** counts two points of the Trainer Card's score (`TRAINER_SCORE_EVENT_UNK_06`).
- **The ribbons** (`sRibbonDataTable`): eighty, in the original's order (Hoenn's Champion Ribbon and its twenty contest ribbons, Winning, Victory, Artist and Effort, the seven of Hoenn's and Orre's gifts and events, Sinnoh's Champion Ribbon, the twenty of the Super Contests, the Battle Tower's six, the seven of the days, the Syndicate's three, Footprint, Record, History, Legend, and the seven of later events). The team's count is the different ribbons its Pokémon show between them (`CountPartyRibbons`), Eggs left out.
- **Who gives which**: every giver means the first of the team that is no Egg (`GetFirstNonEggInParty`). The Hall of Fame gives every Pokémon of the entering team that is no Egg the Sinnoh Champ Ribbon (`Party_GiveChampionRibbons`). Julia, in Sunyshore City's north-east house, gives the day's ribbon for a story: Alert on Monday, Shock, Downcast, Careless, Relax, Snooze, and Smile on Sunday; the first visit only makes the acquaintance, and she hears one story a day (two daily flags). The woman of the Sunyshore Market gives the Effort Ribbon to a Pokémon whose effort values come to 510. Dr. Footstep, on Route 213, reads its friendship in five steps (under 70, from 70, 150, 200, and 255) and gives the Footprint Ribbon at 255. The Ribbon Syndicate lets in a team that shows ten different ribbons, and its cashier sells the first of the team the Gorgeous Ribbon for 10,000, then the Royal Ribbon for 100,000, then the Gorgeous Royal Ribbon for 999,999.
- **The contests' records and photographs** (`Contest_EndContest`): the Super Contests entered, those won and the ribbons won are counted (to 9,999, practice rounds never); a win puts the winner's photograph in the frame of its contest, one of five on the Contest Hall's wall, over the one that hung there.
- **The Trainer Card's stars** (`TrainerCase_CalculateTrainerCardLevel`): a win at the Master Rank is one of the five, read from the flag R17a already set.
- **The Fashion Case** (`JubilifeCity_GiveFashionCaseAndAccessories`): the man of Jubilife TV gives it in Jubilife City once Team Galactic has gone from the north gate, with five draws among the six fluffs, five among the six feathers and moustaches after them, the first backdrop and two different ones of the eight after it. Eighteen backdrops, one of each at most.

**Quirks kept**

- A Seal whose middle is exactly sixty dots from the ball's is still on it; the delay's last step begins four dots short of the edge, so a Seal at fifty-six dots waits twenty frames where the steps of twenty would give it fourteen.
- The capsule a Pokémon carries is a copy: taking a Seal off the capsule in the case and backing out without keeping leaves both as they were, and a capsule swapped away keeps its Pokémon.
- The boy counts an Unown's Seals with those stuck on capsules, so a case with ninety-five of a letter gets four.
- Volkner's Electivire comes out of seven of one Fire Seal, all the trainers' capsule holds.

**Our own choices**

- **The cases open from the bag.** The original's bag only says how many Seals or accessories a case holds (`ITEM_USE_FUNC_BAG_MESSAGE`); the capsules are changed at a PC, and nothing shows the accessories outside a contest. Here OPEN on the Seal Case goes to the Ball Capsules (and a PC offers BALL CAPSULES too, as the original's does, once the player has the case), and OPEN on the Fashion Case to a viewer of its accessories, its backdrops and the contests' records.
- **The stylus is the arrows.** A Seal is picked from the case's list (eight kinds to a page, the page turning off its top and foot), carried over the ball four dots a press, and dropped with the button; the item button goes from the case's side to the Seals on the ball, which are picked up again the same way. Enter keeps the capsule; backing out asks first, as the original's QUIT does.
- **What a capsule gives is shown on its screen**, over and over on a little stage beside the ball, with the battle's own pieces drawn flat; the original shows it only in battle. The merchant's stall shows the Seal under the cursor the same way.
- **The Seals' looks are ours**: a sign and a colour for each kind (style guide, "Ball Capsules and Seals"), and bursts of our own for the fourteen families (hearts that float up, stars thrown out, rays, smoke, sparks, bubbles, flames, confetti, petals, notes, drops, a bang, twinkles), the A to G of a family differing in colour, size and number. A letter Seal bursts as its letter, written in dots, where it was stuck: several spell a word, read from the left on the player's side and on the opponent's alike.
- **The capsule's burst comes with the ball's own flash**, where the original plays it in place of the ball's particles.
- **The ribbons' page** is the summary's last, after the stats and the condition: the original has it among its summary's pages too, with the ribbons' pictures and descriptions, which are ours here (a sign by what a ribbon is given for, and a line of our own saying so).
- **Julia's story is a yes or a no.** The original has the player choose a word from the easy-chat system's lists; this game has no such lists, and the word decides nothing.
- **Dr. Footstep speaks in one voice.** The original words his reading five ways by the species' kind of footprint (and says nothing of a Pokémon that leaves none); here the five steps of friendship have a line each.
- **A photograph is a print**: the winner's menu sprite on the theatre's stage with the accessories it wore round it, its ribbon, its name, the contest and rank, the player's name and the day. The original keeps the dressed-up picture itself, with the backdrop chosen.
- **The Fashion Case comes to an older save** as it loads, once it is past Jubilife City's scene with Team Galactic and has none (`GameEngine.OweTheFashionCase`); no story version was raised for it.
- **Every line is our own**, on the original's beats.

**Which ribbons can be had, and which wait**

| Ribbons | How many | Given by | Waits for |
| --- | --- | --- | --- |
| The Super Contests' | 20 | A win (R17a) | |
| Alert, Shock, Downcast, Careless, Relax, Snooze, Smile | 7 | Julia, one a day | |
| Effort | 1 | The woman of the Sunyshore Market | |
| Footprint | 1 | Dr. Footstep | |
| Sinnoh Champ | 1 | The Hall of Fame | |
| Gorgeous, Royal, Gorgeous Royal | 3 | `common.RibbonSyndicateCashier`, behind `common.RibbonSyndicateEntry` | The Resort Area's Ribbon Syndicate (plan 01 · M10): its doorkeeper and its cashier run the two scripts |
| Ability, Great Ability, Double Ability, Multi Ability, Pair Ability, World Ability | 6 | Nobody yet | The Battle Tower (plan 06 · R18) |
| Hoenn's Champion, its twenty contest ribbons, Winning, Victory, Artist | 24 | Nobody | They come only on a Pokémon brought over from Hoenn (Pal Park, plan 06 · R16b; the migration of plan 08) |
| Marine, Land, Sky, Country, National, Earth, World | 7 | Nobody | The same: given in Hoenn's and Orre's games and by their events |
| Record, History, Legend, Red, Green, Blue, Festival, Carnival, Classic, Premier | 10 | Nobody | Nothing in Platinum gives them (the last seven were an event's); named and drawn |

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The Seals' pictures and particles | A sprite and a particle effect each | A sign on a sticker and a burst from the battle effects' kit | Nothing is taken from the games |
| The Seals' sounds | Some Seals have a sound of their own as they burst (the Song Seals' notes) | The ball's own sound | Plan 05 |
| The ribbons' pictures and words | A sprite and a description each | A rosette, a bow, a medal or a pennant in the ribbon's colour, and a line of our own | The same |
| The backdrops | A picture each | A scene of our own in two tones and a few plain shapes, by the backdrop's name | The same |
| The Ribbon Syndicate | A building in the Resort Area: a doorkeeper, a cashier, a spa upstairs | Stood in for no longer: the doorkeeper and the cashier of its ground floor start the two scripts (plan 01 · M10, part b), and its lift and spa are built (plan 01 · M11, part c) | Done |
| The Sunyshore Market's other stalls | Three more sellers of nothing, and the Seals on show | Its people say their lines | Plan 01 · M11 |
| The Seal Club's programme | A TV programme shows other trainers' capsules | Not there | Plan 08's TV |
| Seals and backdrops from elsewhere | Mystery gifts, the Global Terminal, a daily Seal in the Pokémon News Press, Amity Square's accessories | Only the givers above | Plans 07, 08 and 01 · M11; Amity Square is R16's |
| The footprints | Dr. Footstep's reading follows the species' footprint, which the Pokédex shows | The Pokédex shows a print of our own made from each walking species' model (plan 09 · L9), and Dr. Footstep reads his five steps with `if friendship`; he still speaks in one voice, for a Pokémon that leaves no print too | Done but the voices: a voice by the kind of print is a later session's |

## The eighth chapter, first half (2026-10-10, plan 02 · S11, part a)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_acuity_lakefront.s`, `scripts_lake_acuity.s`, `scripts_init_lake_acuity.s`, `scripts_lake_acuity_low_water.s`, `scripts_snowpoint_city.s`, `scripts_snowpoint_city_pokecenter_1f.s`, `scripts_snowpoint_city_mart.s`, `scripts_snowpoint_city_gym.s`, `scripts_init_new_game.s`, and the events files of those places)

- **What keeps Lake Acuity shut until the Icicle Badge is a rock face**, in Platinum: nobody stands in the way (Diamond and Pearl's grunts at the lakefront are gone; their lines are still in the text bank and nothing says them). The lake lies at the top of a face that needs Rock Climb, HM08 lies on Route 217, and Rock Climb outside battle waits for Snowpoint's Badge (plan 02 · S2's rule).
- **The rival stands at the head of that face from the start of the game**, and calls down to the player the first time they come up to its foot (the original's trigger, two tiles across the path, `VAR_ACUITY_LAKEFRONT_STATE` 0): the rocks need the Badge, and he is off to face Team Galactic. He walks on to the lake and is gone for good (`FLAG_HIDE_ACUITY_LAKEFRONT_RIVAL`, which the original's `RemoveObject` sets). The road from Route 217 to Snowpoint City crosses the trigger's two tiles, so the scene is always played before the Gym (`SnowpointAndLakeAcuityTests` walks the lakefront with those tiles shut).
- **Maylene waits in Snowpoint's Pokémon Center** from the start (her flag is never set by a new game), with one thing to say before the player has the Icicle Badge and another after. Coming to Acuity Lakefront with the Badge sends her home: `FLAG_HIDE_SNOWPOINT_CITY_POKECENTER_1F_MAYLENE` set and `FLAG_HIDE_VEILSTONE_CITY_GYM_MAYLENE` cleared (the lakefront's OnTransition), which undoes what Route 217 did.
- **Lake Acuity is two maps**, as the other lakes are: the lake as the game begins (`lake_acuity_low_water` in the decompilation's naming), which the lakefront leads to until `FLAG_TEAM_GALACTIC_LEFT_LAKE_VERITY` is set, and the lake Team Galactic has come to (`lake_acuity`) from then on, for the rest of the game. The rival stands on the first from the news of the explosion in Canalave City, with two things to say; nobody meets him there in a game played in order (Mt. Coronet's grunts keep the north shut until Commander Mars is beaten, which is also what turns the way in).
- **Nobody battles Commander Jupiter at Lake Acuity.** The scene plays as the player comes in (the map's OnFrame, `VAR_LAKE_ACUITY_STATE` 1): the rival has lost to her already; she speaks to him, walks down the shore to the player, speaks of Eterna City and of Team Galactic's headquarters in Veilstone City, and leaves; the player walks up to the rival, who speaks three times and leaves too. Both are removed (`FLAG_HIDE_LAKE_ACUITY_JUPITER`, `FLAG_HIDE_LAKE_ACUITY_RIVAL`).
- **What that scene sets**, all of it the original's: `VAR_LAKE_ACUITY_STATE` 2; `FLAG_HIDE_VEILSTONE_CITY_GRUNT_M_STORAGE_KEY` cleared (the grunt who drops the Storage Key appears in Veilstone City: that is what opens the way into Team Galactic's headquarters, plan 02 · S11, part b); `FLAG_HIDE_SANDGEM_TOWN_LAB_PROF_ROWAN` cleared; the professor and the assistant hidden at Lake Verity on both of its maps (four flags); `FLAG_LAKE_VALOR_EXPLODED` cleared, so Canalave City's people and its library's readers go back to their own lines; `FLAG_ALT_MUSIC_LAKE_ACUITY` set.
- **Candice's Badge** also sets `FLAG_HIDE_VEILSTONE_GALACTIC_GRUNTS` (her Gym's script, plan 01 · M9; the flag is set already by the warehouse's scene, as the original's `RemoveObject` sets it there).
- **The temple's guard turns the player back** from the west half of the temple's door every time (the original's trigger, `VAR_SNOWPOINT_CITY_STATE` 0), and says the same when spoken to. Candice comes to let the Champion in after the Hall of Fame with the National Pokédex (state 1), and the sailor of the ship to the Battle Zone sails only then (`FLAG_GAME_COMPLETED`): both are plan 02 · S15's.
- **Saves from before**: `common.ChapterEight` (story version 13) hides Uxie in Acuity Cavern and Candice and Cynthia in Snowpoint City, as the original's new game does. A save that has been to Snowpoint City (`FLAG_FIRST_ARRIVAL_SNOWPOINT_CITY`) or holds the Icicle Badge passed the rock face when nobody stood at its head: for it the rival has called down and gone (`FLAG_HIDE_ACUITY_LAKEFRONT_RIVAL`, `VAR_ACUITY_LAKEFRONT_STATE` 1).

**Our own choices**

- **The lake Team Galactic comes to is a map of the imported world** (`LakeAcuityGalactic`, matrix 108), the original's own plan with its people, its TM14 and its warps, named as Lake Verity's is; `LakeAcuity` (matrix 107) stays the lake as the game begins. The two warps the original leaves among its trees (to the lakefront's first pair, from the lake's other map) and Acuity Cavern's mouth are shut in its overlay.
- **The player comes into the lake a step inside its way out** (14 or 15, 49), as every warp of the imported world puts the player down, where the original stands them on the warp's own tile (y 50). Every walk of the scene is therefore a step shorter than the original's: Jupiter stops beside the player (from the west tile) or in front of them (from the east), and the player goes up four tiles, not five, to stand beside the rival.
- **The camera is sent to a fixed tile** in both scenes (310, 238 at the lakefront; 16, 45 at the lake), where the original moves it six or five tiles north of wherever the player stands, and at the lake it comes back once Jupiter has walked down, where the original brings it down with her.
- **The rival of the lake as it was is placed and scripted** although no game played in order reaches him: a save from before the seventh chapter that was already north of Mt. Coronet's grunts, with the Icicle Badge and Rock Climb, can. Whoever stands there has Snowpoint's Badge, so his second line sends the player to Sunyshore's Gym, as the original's does.
- **Snowpoint's Pokémon Center and Mart are the towns' common rooms**, so Maylene, the Center's lass, its other trainer and its Psyduck, and the Mart's two customers, stand where those rooms have room for them and can be seen, not on the original's tiles. Maylene turns to face whoever speaks to her; the original has her stamp on the spot twice as she turns (she is cold).
- **A person of Snowpoint the scenes show is given a look that exists**: the temple's guard and two townswomen the lady and the lass, the townsman the youngster, the two sailors the gentleman (their overlay's `npcType`). Left to the fallback each was the boy the player is, and the guard stands beside the player in her own scene.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| Team Galactic's theme at Lake Acuity | `D_GINLOBBY` plays on the lake until the scene has ended (`FLAG_ALT_MUSIC_LAKE_ACUITY`, which is set all the same) | The lake's own theme | No such song yet (plan 05 · A6) |
| Candice's look | Her own (`candice`) | The lass, in her Gym (plan 01 · M9) | The second half's named cast is plan 11 · C6's. Jupiter has her own |
| The looks of Snowpoint's people | `snowpoint_npc_m`, `snowpoint_npc_f`, `ace_trainer_snow_f`, `sailor` | The youngster, the lady, the lass, the gentleman | Plan 11 · C2 to C4 |
| The Bosses' words in battle | Candice speaks as her last Pokémon comes out | Nothing | Plan 11 · C12 |
| Maylene's stamping | `WalkOnSpotFaster` twice toward the player | She turns to the player | No such step in the script language; a scene's effects are plan 09 · L11's |
| Candice at the temple, Cynthia on the quay, the ship | After the Hall of Fame: Candice lets the Champion into the temple, Cynthia sees the player off, the sailor sails to the Fight Area | Not placed (their flags are set from the start); the sailor says only what he says before | Done since: plan 02 · S15, part a (the ship, Cynthia) and part b (Candice, the temple's door); the temple inside is plan 01 · M10b's |
| Acuity Cavern and Uxie | Uxie waits in the cavern after the Distortion World | The cavern's mouth is shut | Plan 02 · S13 |
| The professor in his lab | Hidden and shown by `FLAG_HIDE_SANDGEM_TOWN_LAB_PROF_ROWAN` | The flag is cleared as the original clears it; the lab's room keeps him at his desk whatever it says | The lab's later scenes are plan 02 · S12 and S13's |
| The move tutor's house, the Pokémon Center's other floors | A hiker teaches moves for shards; the upper and lower floors | Door shut; no such floors | Plan 01 · M11 |

## The eighth chapter, second half (2026-10-11, plan 02 · S11, part b)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_veilstone_city.s`, `scripts_veilstone_city_galactic_warehouse.s`, `scripts_galactic_hq_1f.s` to `_4f.s`, `_b1f.s`, `_b2f.s`, `_hall.s`, `_laboratory.s`, `_control_room.s`, their `scripts_init_` files, `scripts_init_new_game.s`, the events files of those places, and for the panels `src/overlay005/field_control.c` and `src/field_map_change.c`)

- **The player never holds the Storage Key.** The grunt on the terrace before the Galactic building lets it fall as he runs (`FLAG_VEILSTONE_CITY_GRUNT_DROPPED_STORAGE_KEY`), Looker picks it up and asks whether the player will come into the hideout with him, and it is Looker who unlocks the warehouse's door with it (`FLAG_USED_STORAGE_KEY`). The item exists in the table and nothing gives it: the branch of the warehouse door's script that asks to use it from the bag is Diamond and Pearl's.
- **Looker asks until the player says yes.** A no leaves him standing before the building (the city's OnTransition puts him at 717, 593 once the key has fallen), and spoken to he asks again; a yes sets `VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE` 3 and `FLAG_AGREED_WITH_LOOKER_TO_ENTER_HIDEOUT`, and he waits no longer in the street once the warehouse's scene has played (`FLAG_HIDE_VEILSTONE_CITY_LOOKER`, state 4).
- **The building's three doors are open from the start of the game.** Two lead into the lobby, whose stairs are behind a shutter; the third into a room with nothing in it but the pad the way out lands on. The headquarters is entered from the warehouse.
- **A warp panel** is a tile of the behaviour `TILE_BEHAVIOR_WARP_PANEL` with a warp event on it. Whoever ends a step on one is taken to the tile the warp names, which is the panel at the far end, and stands on it facing as they did when they stepped on: the map change is handed the player's own facing, where a door's or a flight of stairs' puts its traveller a step beyond and turns them. Arriving is no step, so the far panel does not take them back until they step off it and on again. The green pad on the ground floor has the panel's look and no behaviour: the fourth floor's green panel leads to it, and it leads nowhere.
- **Four shutters open to the Galactic Key**, each for good by a flag of its own (`FLAG_HIDE_GALACTIC_HQ_1F_DOOR`, `_B2F_DOOR`, `_3F_DOOR`, `_4F_DOOR`): the lobby's before the stairs, the lower basement's between the store rooms and the key's own side, the third floor's across its south corridor, and the fourth's across the corridor to Cyrus's room. The key lies behind the basement's, so it is reached the long way round: up from the warehouse's stairs, by panels across the ground and second floors, and down again.
- **Cyrus's speech** plays as the player comes down the hall's east passage to Looker (the hall's trigger, `VAR_GALACTIC_HQ_HALL_STATE` 0): Cyrus is added on the dais (`FLAG_HIDE_GALACTIC_HQ_HALL_CYRUS`, set by a new game), speaks, and is removed; the ranks of grunts are removed behind a fade (`FLAG_HIDE_GALACTIC_HQ_HALL_GRUNTS`); Looker speaks and is removed (`FLAG_HIDE_GALACTIC_HQ_HALL_LOOKER`). The hall's west panel, which the ranks stand across, is the way on from then.
- **Cyrus is battled in his own room** on the fourth floor (the trigger at the end of its corridor, `VAR_GALACTIC_HQ_4F_STATE` 0), gives the Master Ball, and leaves by the warp panel he points to (`FLAG_HIDE_GALACTIC_HQ_4F_CYRUS`, state 1).
- **Commander Saturn is battled when spoken to**, in the control room, and steps away from the button (`FLAG_DEFEATED_GALACTIC_HQ_CONTROL_ROOM_SATURN`, `VAR_GALACTIC_HQ_CONTROL_ROOM_STATE` 1); the room's OnTransition stands him aside on later visits, and a trigger on the exit mat calls the player back while the state is 1.
- **What the button sets**, all of it the original's: `VAR_GALACTIC_HQ_CONTROL_ROOM_STATE` 0, `FLAG_FREED_GALACTIC_HQ_POKEMON`, `FLAG_HIDE_GALACTIC_HQ_CONTROL_ROOM_LAKE_GUARDIANS`, and for Mt. Coronet `FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNT_F` cleared, `FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING` set, `FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING_SHARDS` cleared and `VAR_MT_CORONET_2F_STATE` 1. Saturn is removed, and the flag his removal sets, `FLAG_HIDE_GALACTIC_HQ_TEAM_GALACTIC`, is the one that hides every member on the building's floors and the two bollards across the fourth floor's way out. Charon stays (`FLAG_HIDE_GALACTIC_HQ_CONTROL_ROOM_CHARON` is the Distortion World's).
- **A bed of the second floor's nap room heals the team** (a fade, the morning jingle, `HealParty`), as often as it is asked.
- **Saves from before**: `common.Headquarters` (story version 14) sets what the original's new game sets for this half: `FLAG_HIDE_VEILSTONE_CITY_GRUNT_M_STORAGE_KEY`, `FLAG_HIDE_GALACTIC_HQ_HALL_CYRUS` and `FLAG_HIDE_GALACTIC_HQ_1F_SATURN`. A save that has already played Lake Acuity's scene (`VAR_LAKE_ACUITY_STATE` 2 or more) has the grunt's flag cleared again, as that scene clears it.

**Our own choices**

- **The nine floors are room files made from the original's plans** (`tools/MapImporter -- --room`), as the Eterna building's are: the original's grid, warps, triggers, people, items and signs on the original's tiles, with furniture of ours where its models stand. The hall's eighty-one grunts are eighty-one people of the map.
- **The player hears the speech from beside Looker** (20, 12), where the original stands them a tile south of him: that tile is under this game's text box.
- **The camera goes to the dais** for the speech and comes back behind the fade, where the original moves it up the hall and back behind fades of its own.
- **A bed is rested in from beside it or from its foot.** The original's bed is three tiles long and is walked onto; here its middle tile is blocked, so nobody stands inside the piece of furniture.
- **The fourth floor's machine gives one report to a reading**, where the original's menu comes round again after each.
- **The lobby's four people say their first lines only.** What they say once Team Galactic has left Lake Valor is asked by `FLAG_GALACTIC_LEFT_LAKE_VALOR`, which nothing sets before the Distortion World; the lines for afterwards are written beside each script (`GalacticHQ1F.txt`) for the session that sets the flag.
- **Riding a panel takes eight tenths of a second of turning**, half a second before the picture closes and three tenths after it opens, a quarter turn every fourteenth of a second (`WarpPanels.SpinUp`, `SpinDown`, `QuarterTurn`): the original's animation, to this game's fade.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| Saturn's look | His own (`saturn`) | A grunt, as at Lake Valor | Plan 11 · C6. Cyrus, Charon and Looker have theirs |
| Team Galactic's themes | `D_GINLOBBY` in the lobby's floor, `D_AGITO` on the other floors and in the hall, `THE_EVENT04` in the laboratory and the control room; the commander's and the boss's battle themes | Veilstone City's theme on every floor; the classes' battle themes | No such songs yet (plan 05 · A6) |
| The crowd in the hall | A fanfare of cheering at each part of the speech | The picture shakes, and the ranks roar in a line of their own | The story's effects are plan 09 · L11's |
| The containment units | Three machines with animations of their own, lit and darkened by the button | Racks either side of each Pokémon, which reads as a machine's sign once the Pokémon has gone | A prop of its own with L11's effects |
| The three Pokémon set free | Each flickers out as its machine goes dark | They come out two tiles, cry, flicker three times together and are gone | Plan 09 · L11 |
| The bollards of the fourth floor | Team Galactic's own barrier | The Hearthome Gym's post (`npcType` `Bollard`) | Art of its own with the building's later look (plan 01 · M11) |
| The Storage Key in the bag | Never given, in Platinum | Not given; the door's branch that asks for it is left out | Diamond and Pearl's leftover |
| The Bosses' words in battle | Cyrus and Saturn speak as their last Pokémon comes out | Nothing | Plan 11 · C12 |
| The lobby afterwards | Saturn in the lobby and other lines for its four people, after the Distortion World | Saturn's script is written and he stays hidden; the first lines are said | Plan 02 · S12 and S13 set the flags |

## The Underground (2026-10-11, plan 06 · R16a)

**Platinum's rules, kept as they are** (`src/underground.c`, `src/underground/mining.c`, `spheres.c`, `traps.c`, `secret_bases.c`, `base_decoration.c`, `vendors.c`, `pc.c`, `manager.c`, `menus.c`, `trap_prices.c`, `treasure_prices.c`, `src/goods.c`, `src/field_map_change.c`, `src/item_use_functions.c`, `res/field/scripts/scripts_underground.s` and the Underground Man's; the generators are the console kit's `MATH_Rand16` and `MATH_Rand32`, which the decompilation doesn't hold: their multipliers are the kit's published ones):

- **What the save holds** (`Underground`): forty Spheres, forty treasures, forty traps and forty goods in the bag below ground, two hundred goods in the base's PC, fifteen goods out in the base at most, sixteen boulders, a hundred buried Spheres, sixteen traps of the player's own setting, sixty-four spread by the day, two hundred and fifty walls that hold something; a Sphere's size is 99 at most.
- **The way down** (`CanUseExplorerKit`, `MapChangeUndergroundContext_New`): the Explorer Kit works on the region's own map alone, on foot, off the Cycling Road and the bridges' decks, out of a Safari Game, and not on ground that forbids it. The Underground is half the region's size: under four chunks of the overworld lies one of its own, and whoever digs down lands on one of four tiles of it by the quarter stood in. Coming up is where one went down.
- **Six systems that don't join.** The tunnels are six separate systems (and the base's room a seventh place), so where one comes out below hangs on where one dug down, as in the original.
- **The day's spread** (`MiningEnv_Init`, `Underground_HandleDailyEvents`): a new day moves the Underground's number on (`ARNG(seed + days)`), and the next time the tunnels are come to, a wall is put near each buried Sphere (a hundred tries within ten tiles) and fourteen clusters are thrown down, each of six to eleven walls and nought to five traps of the twenty-six that are spread. A wall can hold something where it is rock of the tunnels with open floor beside it.
- **The wall** (`mining.c`): thirteen squares by ten, 196 of strength; a blow of the pick costs it 4 and of the hammer 8. Dirt lies two layers thick, four in ten patches of eight by eight, six in fifteen patches of five by five where it already lay four. Two to four things are set in it (three in the first wall ever dug, and no dark rock), each drawn by weight from the table by whether the trainer's number is odd or even and whether the National Pokédex is had (the Skull Fossil for odd numbers, the Armor Fossil for even; the old fossils, the evolution stones' rarer weights and the plates' with the National Pokédex); a plate is found once in a game and never twice in a wall; dark rock fills what room is left, eight things in all at most. A blow takes two layers off its square; the pick one off the four beside it; the hammer two off those and one off the corners; a blow on bare dark rock takes nothing off anything. What lies wholly bare when the wall falls in or the last thing is bared is the player's. A Sphere dug out is of size 1 (Prism, Pale) or 1 to 4, ten more for a large one, one more for every 39 of strength the wall has left, and five more unless it fell in.
- **Spheres** (`spheres.c`): one is buried in the floor ahead, never in a base, a wall or under someone; one buried onto another of its kind joins it (the larger, a fifth of the smaller and one more). Each grows every day: a Prism or Pale one by 1 or 2, a Red or Blue one by 3 to 6, a Green one by 5 to 9, for ninety-nine days counted at most; dug up, it is its size and its growth, 99 at most.
- **Traps** (`traps.c`): a trap is set in the floor ahead and goes off under whoever steps on it, its setter too; one seen can be taken up. A Move Trap carries the player twenty-five tiles and a Hurl Trap forty-three, or to the first thing in the way; a Hole Trap is climbed out of in eight turns and a Pit Trap in eighteen; a Reverse Trap turns the steering round and a Confuse Trap deals the four ways out afresh, for thirty steps; smoke is wiped away a touch at a time (three off the tile, two off the four beside it, one off the corners, clear past five; fewer than thirteen tiles left is clear); a Rock Trap's boulder breaks after eleven blows and a Rockfall Trap's after seventeen; foam is seven bubbles and a Bubble Trap's twenty, one burst a tap; sixteen leaves or thirty-two flowers are blown off the screen; an Ember Trap's flame is two high and a Fire Trap's four, one off for every ten frames of a single breath and back to its height at a pause; an Alert Trap only calls out.
- **The radar** (`UndergroundMan_TouchRadarSearch`): what lies within six steps, eight of a kind at most.
- **The hikers** (`vendors.c`): twenty, on the tiles of the original's table: eight trade goods for Spheres, eight traps, four give Spheres for treasures. A hiker's five wares are drawn from the day's number and his own (a trap hiker's first is always the Digger Drill; nothing twice, nothing with no price, no good that waits for the National Pokédex until it is had), each for a Sphere of its kind (any kind for the Digger Drill) of a size drawn between the table's two. He gives a Sphere of half that size for a trap or a good of the player's, and the treasure hikers one of the whole size for a treasure.
- **The secret base** (`secret_bases.c`, `base_decoration.c`): the Digger Drill opens one in a wall of the tunnels three tiles wide with rock behind it, and is used up; a second base moves the first. The room is twelve tiles by twelve with the PC at the head of it and sixteen big boulders on a lattice that leaves the way from the mat to the PC free. Goods out of the PC stand anywhere on the floor that nothing else stands on, but the tiles before the PC and inside the mat; the PC and the boulders are never moved. Flags brought home earn a better flag at one, three, ten and fifty; with them fifteen, eleven, six and none of the boulders must stay, and ten, twelve and fifteen goods may stand. A trap's tool goes off under whoever steps on it and is still there after.
- **The Underground Man** (`scripts_eterna_city_underground_man_house.s`): his missions in their order, each rewarded on the next visit and the next set by speaking again: go down (a Move Trap, a Bubble Trap, a Leaf Trap), dig at a wall (a Prism, a Red and a Blue Sphere of size 1), bury a Sphere (a Digger Drill), make a base (a Plain Table, a Wooden Chair, a Small Bookshelf and a Buneary Doll, sent to the base's PC), decorate it (the doll of the player's own first Pokémon), bring one flag home (the Pretty Gem), then three, ten and fifty (the Shiny, Mystic and Glitter Gems). A reward the PC has no room for waits.
- **Roark** stands beside whoever comes down for the first time, says what the place is, and is gone.
- **The Trainer Card's score** (`GameRecords_IncrementTrainerScore`): one for going down, one for every hundred steps below, two for a wall dug clean, three for a new base, thirty for a flag brought home.

**Quirks kept**

- The ten patches of thicker dirt are laid five rows higher than their columns would have them (the original subtracts five from the row alone), and a patch of the thickest dirt is laid by reading the corner of the patch before it.
- The Damp Rock's shape is declared four squares wide and written three wide, so it is read as a block with no hole in it.
- The records count the Old Amber and, by a slip of the original's, the Rare Bone as fossils.
- A slide is set at 27 and 45 tiles and moves while more than one is left; a hole at ten and twenty turns of which the last two are never asked for.
- The last thing bared wins over the wall falling in on the same blow.
- A hiker's price is drawn from the least up to one short of the most.
- One's own trap goes off under one's own feet, and counts in the records as a trap stepped on.

**Our own choices**

- **The radar is the Run button, pressed while standing still.** The original's radar is a touch on the lower screen; here the button sends a pulse round where the player stands, and what answers glints for a moment in the field: gold for a wall, red for a trap, blue for a buried Sphere. Held on the move the button runs, as the original's B does below ground.
- **The wall is dug with a cursor**: the arrows move it a square at a time, the button strikes, the back button changes the tool. The original's is the stylus.
- **A trap is got out of with the keys.** Where the original has the stylus rub, tap or the microphone blown into: the arrows wipe smoke with a hand five tiles high (a touch at each tile of its height, standing in for a scribble), the button's taps break a boulder and burst bubbles one at a time, and holding the button is a breath, as strong as the microphone at its loudest.
- **The bag below ground opens from the Explorer Kit** (USE in the bag, or the item button): TRAPS, SPHERES, GOODS, TREASURES and SURFACE, the last with the way up and the records. The original's start menu changes into this below ground.
- **A base is walked into and out of**: stepping into its way in or onto its mat goes through, with no question.
- **The way into a base is a cave's mouth in the wall**, there while the base is.
- **The smoke's four patterns are ours** (the original's are pictures), and so is every line said below ground, on the original's beats.

**What needed other players, and what became of it**

Decision 06 · 7 leaves local wireless and the online play of the Underground out. Plainly:

- **Left out**: other players walking the tunnels, greeting them and the questions asked of them, the gifts handed from one to another, helping someone out of a trap, traps set for others (the records of traps that caught someone, of people helped, of gifts given and of flags lost stay at nought), a flag taken from whoever carries one, and the base's PC as others see it. Nothing here counts toward Spiritomb's tower: the people spoken to there are the field's (`GREETINGS`).
- **Given a stand-in**: capturing flags, on which the base's whole growth hangs (below).
- **Also not there, though it needs nobody else**: the three radars of the base's PC (its lists of the walls, traps and Spheres about the tunnels). The Run button's pulse is the only radar.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| Other players' flags | A flag is taken from the PC of another player's base, or from a player carrying one, and carried home | The day's diggers (`Diggers.Today`): up to seven a day, drawn from the day's number, each with a base of their own somewhere in the tunnels and a flag on its PC to be taken, one a day from each; a flag carried is lost on going up | Decision 06 · 7; without flags no boulder is ever cleared |
| The touch radar | A touch on the lower screen | The Run button pressed while standing still, round the player | No second screen |
| The stylus and the microphone | Tapping the wall; rubbing, tapping and blowing at a trap | A cursor and the button; the arrows, taps and a held button | The same |
| The smoke's patterns | Four pictures | Drifts of our own laying | Nothing is taken from the games |
| The Underground's music and sounds | `SEQ_TANKOU` and its own effects | The mine's theme, and effects the game already has (the Pokétch's beep for the radar, a rock breaking for the drill, the stairs for the way down) | Plan 05 · A6 |
| The goods | A model each | The room furniture's pieces where one fits, plain pieces of our own for the rest; one figure in its species' colour for every doll | Each would be a sculpt of its own |
| The hikers' looks | The hiker | The look that stands for it in `characters.json` | Plan 11 |
| Three hikers | All twenty stand in the tunnels | All twenty stand in the tunnels: the three of the north-west system, under Fullmoon and Newmoon Islands, waited in the overlay's `heldBack` until the islands opened | Done (plan 01 · M11, part a) |
| The base's PC radars | Lists of what is about | Not there | See above |

## The rest of the post-game (2026-10-11, plan 01 · M10, part b)

Stark Mountain, Snowpoint Temple, Fullmoon and Newmoon Island, the Seabreak Path, Flower Paradise, the Battle Frontier's grounds and the Ribbon Syndicate's ground floor. The places are the original's, tile for tile; what follows is where this game does something else for now.

- **The Seabreak Path is revealed by its variable alone.** The original's `MapMatrix_RevealSeabreakPath` writes four other chunks into Route 224's cells of the matrix while `VAR_HIDDEN_LOCATION_SEABREAK_PATH` holds 0x1028, and so does this game (`world.json`'s `swaps`, `Map.ApplySwappedPlaces`). Nothing sets the variable: the scene that does (Oak's Letter on Route 224) belongs to an event gift.
- **Arriving on an island puts it on the maps**, as the original's `EnableHiddenLocation` does: `VAR_HIDDEN_LOCATION_FULL_MOON_ISLAND` takes 0x0208 and `VAR_HIDDEN_LOCATION_NEW_MOON_ISLAND` 0x0229. Nothing reads them until the Town Map (plan 01 · M12).
- **The Ribbon Syndicate's door is kept inside**, as the original's is: the room's arrival script counts the team's ribbons, and whoever has fewer than ten is told so and put back on the tile before the door, facing south (the original's `Warp MAP_HEADER_RESORT_AREA, 824, 454, DIR_SOUTH`). A member finds the hostess one tile to the east. This supersedes the rows of "Ball Capsules, Seals and ribbons" that say the two scripts are started by nobody.
- **Snowpoint Temple's doors stay shut** until the story opens them, where the original keeps them open behind its guard and her trigger (`VAR_SNOWPOINT_CITY_STATE` 0): the eighth chapter has them so, and nobody is in the temple before the Hall of Fame either way.
- **What glows on Stark Mountain** glows at every hour, by the place's climate (style guide, "Climates"): this game's own look, which the original has none of.

| What | The original | Here, for now | Until |
|---|---|---|---|
| Buck, Looker, Commanders Mars and Jupiter, Charon and their grunts, Heatran, the Magma Stone | Stark Mountain's story from its foot to its last room | In the area files under their flags; not on the maps. The last room is empty | Plan 02 · S15a |
| Regigigas | Stands in the temple's lowest floor and wakes for whoever brings Regirock, Regice and Registeel | In the area file under its flag; not on the map | Plan 02 · S15 |
| Cresselia and the Lunar Wing, Darkrai, Shaymin | Each on its island or in its meadow, for the story or an event gift | In the area files under their flags; not on the maps. The Lunar Wing is held back | Plan 02 · S15b, the event gifts' session |
| The ships to the islands | Canalave's sailor takes the player to Fullmoon Island during the sailor's son's nightmare, and the Harbor Inn's bed to Newmoon Island | The islands' own sailors sail back; nobody sails out | Plan 02 · S15b |
| Newmoon Island's sailor | Away while the dream lasts (`FLAG_HIDE_NEWMOON_ISLAND_SAILOR`) | Always there | The event gifts' session |
| The Battle Frontier's five facilities | The Battle Tower, Factory, Hall, Castle and Arcade behind their doors | Buildings seen from outside; their fourteen ways in are shut | Plan 06 · R18 |
| The Exchange Service Corner | Two booths trade Battle Points for prizes, a third sells scratch-off cards for them | The three attendants say they wait for the halls to open | Plan 06 · R18a |
| The gate house to the Battle Frontier | A room with a receptionist | Walked through | Plan 01 · M11 |
| The Frontier's reporter | Moved about the grounds by the arrival script for the television's programme | Not placed | Plan 08 |
| The Ribbon Syndicate's upper floor | A spa: once a day the first of the team that is no Egg grows friendlier (+30 five times in a hundred, +10 twenty-five, +5 otherwise), reached by a lift | Stood in for no longer: the lift and the spa are built ("The rooms of the north and of the Battle Zone", below) | Done (plan 01 · M11, part c) |
| Stark Mountain's, the temple's, the islands' and the Frontier's themes | `TOWN06`, `D_06`, `D_02`, `D_03`, `CITY11` | Route 206's, Victory Road's, the forest's, Turnback Cave's and Veilstone's play | Plan 05 · A6 |
| The looks of the sailors, the Frontier's attendants and its visitors | Their own object looks | Looks of ours by `npcType` (a gentleman, a clerk, a lass…) | Plan 11 |
| The woods' weather | Header values 23 and 26 to 30, which the decompilation doesn't name | Clear | Whoever names them |

## The ninth chapter: Spear Pillar and the Distortion World (2026-10-11, plan 02 · S12)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_mt_coronet_1f_north_room_1.s`, `_1f_tunnel_room.s`, `_2f.s`, `_6f.s`, `scripts_spear_pillar.s`, `scripts_spear_pillar_distorted.s`, `scripts_distortion_world_1f.s` to `_b7f.s`, `_giratina_room.s`, `_turnback_cave_room.s`, `scripts_sendoff_spring.s`, `scripts_turnback_cave_giratina_room.s`, `scripts_init_new_game.s`, the events files of those places, and for everything the Distortion World does without a script `src/overlay009/ov9_02249960.c` with `include/constants/distortion_world.h`)

- **The cave painting on Mt. Coronet's second floor is an object on the warp it shuts**, behind `FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING`, and its shards are two more behind `FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING_SHARDS`; the button in Team Galactic's control room swaps them. The way down from the floor's upper half lands in front of the painting whether it stands or not.
- **Looker gives the Black Flute** the first time the player steps toward the broken painting (a trigger of three tiles, `VAR_MT_CORONET_2F_STATE` 1 to 2, `FLAG_RECEIVED_MT_CORONET_2F_BLACK_FLUTE`), and gives it when spoken to if the scene was somehow cut short. He stands by the hole until Sendoff Spring.
- **The summit is two maps on one way out.** Mt. Coronet's sixth floor leads to Spear Pillar until `FLAG_SPEAR_PILLAR_IS_DISTORTED` and to the torn Spear Pillar while it is set (the original moves the warp as the floor loads).
- **Two grunts stop the player at the head of Spear Pillar's steps and battle together**, a double battle with two trainers (`VAR_SPEAR_PILLAR_STATE` 0 to 1).
- **Mars and Jupiter are fought beside the rival**, whose team hangs on the player's starter, and he heals the player's team afterwards; Cyrus's scene follows in the same script. It sets `VAR_SPEAR_PILLAR_STATE` 3, `FLAG_UNLOCKED_VS_SEEKER_LVL_3`, `FLAG_SPEAR_PILLAR_IS_DISTORTED`, `VAR_SPEAR_PILLAR_DISTORTED_STATE` 1, marks Dialga and Palkia as seen (`SetSpeciesSeen`), and brings the player to the torn map.
- **Cynthia asks until the player says yes** at the torn pillar (`VAR_SPEAR_PILLAR_DISTORTED_STATE` 2 either way); Charon is gone from the control room from that scene on (`FLAG_HIDE_GALACTIC_HQ_CONTROL_ROOM_CHARON`).
- **How far the player has come through the Distortion World is one count**, `VAR_DISTORTION_WORLD_PROGRESS`: 1 entered the first floor, 2 stood on its slab, 3 entered B1F, 4 saw Mesprit there, 5 spoke to Cynthia on B2F, 6 spoke to Cyrus on B3F, 7 the boulders done, 8 entered B7F, 9 heard Cynthia and Cyrus, 10 beat Cyrus, 11 and 12 Giratina's two shadows in its room, 13 Giratina arrived, 14 Giratina met. Who stands on each floor is decided by it as the floor loads.
- **The boulder puzzle** (`sBoulderFallLocations`, `DistWorldFallingBoulder`): a boulder pushed onto a fall tile of B5F falls to B6F; a boulder pushed onto a fall tile of B6F rests there for good if the tile is the one its guardian keeps, and its guardian leaves; pushed onto any other it falls through and is back on B5F. With all three at rest the puzzle is finished, and Cynthia, spoken to, raises the stepping stones (`FLAG_DISTORTION_WORLD_PUZZLE_FINISHED`, `FLAG_DISTORTION_WORLD_STEPPING_STONES`). The fall tiles are the original's own: three on B5F, eleven on B6F.
- **Cyrus battles when spoken to on B7F** after the argument with Cynthia, and Cynthia heals the team once he is beaten.
- **Giratina is a wild Pokémon in its Origin Forme at level 47 that can be run from** (`StartGiratinaOriginBattle`). A loss or a draw is a white-out. Caught, `FLAG_CAUGHT_GIRATINA` and `FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA` are set and `FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_ITEM` cleared; beaten or run from, nothing is set and it waits in Turnback Cave. Either way the progress is 14, Cynthia and Cyrus have their scene, and the portal opens.
- **The portal leads out at Sendoff Spring** and reveals the Spring Path (`VAR_HIDDEN_LOCATION_SPRING_PATH` 0x0312, `VAR_EXITED_DISTORTION_WORLD_STATE` 1). The scene there sets the state 2, `VAR_SANDGEM_TOWN_LAB_STATE` 2, `FLAG_HIDE_SPEAR_PILLAR_DISTORTED_TEAM_GALACTIC`, `FLAG_HIDE_MT_CORONET_GALACTIC_GRUNTS`, `FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNT_F`, `FLAG_HIDE_MT_CORONET_2F_LOOKER`, `FLAG_ALT_MUSIC_GALACTIC_HQ_1F` and `FLAG_GALACTIC_LEFT_LAKE_VALOR`, and clears `FLAG_SPEAR_PILLAR_IS_DISTORTED` and `FLAG_HIDE_GALACTIC_HQ_1F_SATURN`.
- **Turnback Cave is shut by Cynthia, not by a flag of Giratina's.** She stands on the tile before the cave's mouth at Sendoff Spring until the Hall of Fame sets `FLAG_HIDE_SENDOFF_SPRING_CYNTHIA`; Giratina's own flag is clear in a new game. A game of ours had set it at the start and cleared it at the Hall of Fame: `common.ChapterNine` and story version 15 put Giratina back for a save that has not caught it, and take Cynthia away for one already past the Hall of Fame.
- **The Griseous Orb lies in the room of the Distortion World that Turnback Cave's portal leads to**, which hangs where Giratina stood once it is gone from there.
- **Saves from before**: `common.ChapterNine` (story version 15) sets what the original's new game sets for this chapter: `FLAG_HIDE_MT_CORONET_2F_LOOKER`, `FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING_SHARDS` (unless the button has been pressed), `FLAG_HIDE_SPEAR_PILLAR_DISTORTED_CYNTHIA` and `FLAG_HIDE_SPEAR_PILLAR_DISTORTED_MESPRIT`; and `FLAG_HIDE_SPEAR_PILLAR_RIVAL`, which the original sets each time Spear Pillar is come to while its state is under 4.

**Our own choices**

- **The Distortion World's scenes are scripts.** The original runs them from its own code, by tables of who stands where; here each floor has a script file, its people are its overlay's at the original's tiles, and each floor's arrival stands them by the count above.
- **The puzzle's state is flags of our own** beside the original's count, where the original keeps bits beside its flags: `FLAG_DISTORTION_WORLD_<WHO>_BOULDER_FELL` (gone from B5F), `FLAG_HIDE_DISTORTION_WORLD_B6F_<WHO>_BOULDER` (not yet on B6F), `FLAG_DISTORTION_WORLD_<WHO>_BOULDER_IN_PIT` (at rest, and its guardian gone), and `VAR_DISTORTION_WORLD_<WHO>_SHOWN` for a guardian's showing. So are `FLAG_HIDE_SPEAR_PILLAR_LEGENDS` (Dialga, Palkia and the shadow at the altar, shown by the scene alone) and `FLAG_HIDE_DISTORTION_WORLD_APPARITIONS` (whoever a scene of the Distortion World brings on by itself).
- **A pit is the tile the boulder falls through.** The import knows the original's fall tiles only as blocked; on a map of nothing each is made a tile of nothing, let into the island beside the path the boulder is pushed along, and a boulder pushed onto it sinks into its mouth. The boulder at rest in its guardian's pit is a boulder of its own, shown by the flag.
- **Giratina and the portal in its room hang most of a tile above the stones** (a `level` of 1.8), clear of the head of whoever stands before them; they are spoken to from the stone all the same.
- **The camera goes to the altar for Cyrus's scene** and stops where he stands whole above the text.
- **The cave painting is a drawing of our own** of what our own line says is on it: a ring with three marks round it, and two beasts face to face.
- **The cries written out** ("Kyauuun!", "Gshrrraaaoooh!") are our own.
- **Scripts may tell the Pokédex of a Pokémon they show** (`seen`), and meet a wild Pokémon in a form of its species (`wildbattle "Giratina-Origin" 47`).

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The summoning at Spear Pillar | A cutscene: the Red Chains, Dialga and Palkia drawn out of their rifts, the shadow rising from the floor and closing over Cyrus | Dialga and Palkia step out as Pokémon of the map with their cries and a shake of the picture; the shadow is Giratina's field sprite between two fades | Plan 09 · L11 (effects), L12 (set pieces), L14 (the legendaries' ways in) |
| Dynamax at Spear Pillar | Nothing: a later game's mechanic | Nothing is shown or handed over | Decision 06 · 2 is not confirmed; plan 06 · R22 writes its beat here if it is |
| The three over the tear | A cutscene of Uxie, Mesprit and Azelf | The three as Pokémon of the map (Uxie and Azelf people of our own behind Mesprit's flag), their cries and the camera | Plan 09 · L14 |
| The painting's breaking | Whole, then shards | Whole (a slab of our own), then rubble; nothing between | Plan 09 · L12 |
| Giratina's shadow in the Distortion World | A shadow crossing the sky, five times | Its cry, a shake of the picture, and whoever is there looking round | Plan 09 · L14 |
| Giratina's arrival | It descends as the sky darkens | The script's fade, and Giratina there | Plan 09 · L14 |
| Moving slabs | A slab carries whoever stands on it, and Cynthia rides the first floor's and B6F's with the player | A slab is a way down through a fade (plan 01 · M8); Cynthia is met at its foot | The platforms are M8's later part |
| Cyrus on B4F | Glimpsed walking off, by which slab the player rides | Left out | With the slabs |
| The stepping stones | They rise as the puzzle is done (B6F) and as the player walks (Giratina's room) | There all the time; Cynthia holds B6F's passage until the puzzle is done | With the slabs and plan 09 · L11 |
| The guardians' showing | Each pushes its boulder a little way; Mesprit walks the player through it | The guardian shows itself beyond the hole with its cry, and a line | Plan 09 · L11 |
| A boulder's fall | An animation down through the floor, and onto B6F | It sinks into the pit's mouth and is gone, with a sound and a shake | Plan 09 · L11 |
| A Giratina on the team | Put into its Origin Forme on entering the Distortion World and back on leaving (`SetPartyGiratinaForm`) | Its forme goes by the Griseous Orb alone, so one caught there is in its Altered Forme once the battle ends | Plan 06's form rules |
| The summit's battle stage | A mountain's | The cave's rock stage (the overlays' `battleArena`); the rules still go by the mountain's terrain | No mountain stage yet |
| Two Pokémon for the double battle | Two able to fight (`CheckHasTwoAliveMons`) | Two on the team (`if party < 2`) | The script language has no count of the able |
| `FLAG_FIRST_ARRIVAL_SPEAR_PILLAR` | Set on arriving | Not set | Nothing reads it: Spear Pillar is no place Fly goes |
| Looks and themes | Mars's, Jupiter's and Saturn's own looks; Team Galactic's, Looker's and Giratina's themes, the Distortion World's own | Commanders' looks as before; Spear Pillar's theme and the Distortion World's, the classes' battle themes | Plan 11 · C6; plan 05 · A6 |
| The Bosses' words in battle | Cyrus speaks as his last Pokémon comes out | Nothing | Plan 11 · C12 |
| Walls walked sideways | The Distortion World's walls and ceilings | Not kept; the floors are joined by ways of their overlays | Plan 01 · M8 |

## Fossils, the Game Corner, the Villa and Pal Park (2026-10-11, plan 06 · R16b)

**Platinum's rules, kept as they are** (`src/scrcmd_fossil.c`, `src/coins.c` and the script commands for coins and prizes, `src/overlay101/` whole (the slot machine: `ov101_021D0D80.c`, `ov101_021D1A28.c`, `ov101_021D59AC.c`, `ov101_021D7E48.c` and its tables), `src/villa_furniture.c`, `villa_visitors.h` and `SystemVars_UpdateVillaVisitor`, `src/catching_show.c`, and the scripts of the Mining Museum, the Game Corner, the prize exchange, Veilstone's south-east house, the Resort Area, the Villa, Pal Park and its lobby):

- **Fossils** (`sFossilItemToSpeciesMapping`): Old Amber is Aerodactyl, the Helix Fossil Omanyte, the Dome Fossil Kabuto, the Root Fossil Lileep, the Claw Fossil Anorith, the Armor Fossil Shieldon and the Skull Fossil Cranidos. The Mining Museum's scientist has nothing to offer until the player has the Explorer Kit; he takes one Fossil (asking which when the bag holds more than one kind), keeps its species in `VAR_REVIVED_POKEMON_SPECIES`, and has the Pokémon ready, at level 20, once the player has left the museum and come in again. A full team is told to make room.
- **The Coin Case** (`coins.c`): 50,000 coins at most, and coins are added all or none. The clown of Veilstone's south-east house gives the case to whoever wins his toss, which is a draw of two whichever hand is named. The Game Corner's clerk sells 50 coins for $1,000 and 500 for $10,000 while they fit; an old man gives 20 and a farmer 50, once each.
- **The slot machines** (decision 06 · 8: the international Platinum's, as the decompilation has them). Twelve of the hall's machines are played, each with a mood of 0 to 5 dealt anew every day (one machine of the best and one of the worst among them). The machine runs at the original's thirty frames a second:
  - three reels of twenty-one symbols (the original's strips), turning half a symbol a frame; a game costs three coins and plays all five lines;
  - as the lever is pulled the machine draws what the game may pay (by its mood: a prize on 25 to 35 games in a hundred; which prize, and whether the bonus comes with it, by the mood's weights), and most of the time lets on what it drew;
  - a reel stopped slips on up to four symbols to bring the drawn symbol onto a line, or to keep every line clear when nothing was drawn; a replay or the ball of the wide prizes is brought round however far it is while the reels are stopped left to right;
  - a cherry on the first reel pays 2 for each line it stands on, by itself; three balls pay 10, three moons 15, three sevens 100; three replays play the next game for nothing;
  - the bonus, once drawn, waits: the machine asks for three sevens of one colour, and until they are lined up most games are drawn as a chance at them (65 to 75 in a hundred by the mood) and the rest as a small prize;
  - the bonus game is rounds of fifteen games at one coin each, in which the mascot points out the order to stop the reels in; stopped in that order a replay on the middle line pays 15, and out of order nothing is brought round. Whether another round follows is drawn as each begins, by the kind of bonus (50, 70 or 90 in a hundred) less what tired the mascot; under a red moon a win tires it, under a bright one another round is certain;
  - the longest run of bonus rounds at a sitting is kept (`VAR_CONSECUTIVE_BONUS_ROUND_WINS`), and ten in a row earn TM64 at the counter.
- **The prize exchange**: nineteen prizes at the original's prices in coins (the Silk Scarf, Wide Lens, Zoom Lens and Metronome at 1,000; fifteen TMs from 2,000 to 20,000). The guitarist beside the counter tells the type a Pokémon's Hidden Power is (`CalcHiddenPowerType`), whether it knows the move or not.
- **The Villa** (`villa_furniture.c`, `scripts_villa.s`): a boy in the Resort Area gives it to whoever first walks up, and waits inside until the free table has been ordered. The catalogue on the table lists twenty pieces at the original's prices, each with its one place in the room; nine are listed only once something has been done: the Music Box with the Rack, the Tea Set with the Guest Set, the two busts for the Battle Frontier's victories and prints, the Piano after ten entries in the Hall of Fame, the Guest Set after fifty battles at the Battleground, the Wall Clock after fifty berries planted, the Masterpiece after thirty Eggs hatched, the Chandelier after 300,000 steps. A piece ordered is delivered at once and blocks the tiles the original's table gives it. The delivery of the last piece adds ten to the Trainer Card's score.
- **The Villa's visitors** (`CalcVillaVisitorIndex`): each new day, unless the player is at the Resort Area or in the Villa, a visitor is drawn: with fewer than eight pieces one of four (the rival, the assistant, the professor, Mom), with eight one of twelve (the eight Gym Leaders join them), with twelve one of fifteen (Cynthia, Flint with Volkner, Maylene with Candice). Each says one of five things, drawn with the visitor. Some are found inside, five wait at the door until asked in, and four walk in once the player has stepped up to the table. The table, the Small Sofa, the Bookshelf, the Houseplant and the Piano each bring someone round the moment they are delivered.
- **Pal Park** (`catching_show.c`, plan 06 · R9's `CatchingShow`): the attendant starts a show for whoever has six Pokémon waiting in the park and six free places in the boxes. In the park each of the six keeps to its corner of the grass or the water, a Park Ball never misses, and the sixth catch ends the show. The score is told in its three parts, a new record is kept (`VAR_CATCHING_SHOW_RECORD`), a berry is given by the score (under 3,000 one of eight, under 3,300 one of five, under 3,500 one of seven, one of six from there up), and the six go to the boxes as Pokémon met at Pal Park, or are left in the park to be caught again. Walking back to the gate, or speaking to the attendant beside it, asks whether to give the show up; given up, the six go on waiting.

**Quirks kept**

- The visitor's chance is compared so that the chance itself passes: a visitor comes on 26, 76 and 91 days in a hundred, not 25, 75 and 90.
- Every line that pays is paid what the last line found pays, and a cherry on the first reel is a line by itself.
- While the bonus waits for its sevens, the small prize's table is read with its second share asked twice and its last two never.
- A Catching Show's score of 10,000 or more would earn no berry: the lobby's script checks only under it.
- The clown's question makes no difference to his toss.

**Our own choices**

- **A machine is played with the game's keys**: the button bets and pulls the lever, Left, Down and Right stop the first, second and third reel, and the back button gets up. The original's are X, Down, and Y, B and A; a button kept down counts the coins out twice as fast, as there.
- **The symbols are ours**: the original's numbers with pictures of our own (a red and a blue seven, a replay mark, cherries, a ball, a moon), and so is everything on the machine's display. What the mascot does on the original's lower screen is a small display here: the order pointed out as three numbers over the reels, the moon, and what stamina is left.
- **The waits the original spends on its mascot's animations are lengths of our own** (the rules in between are frame for frame).
- **The day's deal of moods uses the game's own generator**, seeded with the day's number as the original seeds the console's: the moods are the original's twelve, in an order that isn't the cartridge's for the same day.
- **The exchange's counter ends the talk after an answer**; the original's goes back to its list.
- **A piece of furniture is delivered where the script stands**: the original walks the player out of the door and in again behind a black screen.
- **Every line** in the museum, the hall, the exchange, the Villa and the park is ours, on the original's beats: the visitors' five sayings each are our own too.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The six Pokémon of a Catching Show | Brought over from a Game Boy Advance cartridge in the lower slot | Nothing has been brought over: the attendant says so and no show begins. `PalParkState.Stock(six)` puts six in the park (saved as `SaveData.Pastimes.Waiting`), and the show, its score, its prize and the boxes run from there | Plan 08 · P21 writes the migration and calls it; the smallest stand-in that claims nothing |
| A show broken off | The six wait in the save | The same: the show itself isn't saved, and a game taken up again in the park is sent back to the lobby | As the original |
| Pal Park's lobby | Two floors: a counter below, a gallery above reached by an escalator, with four spectators | One floor on the original's plan, the stairs roped off | Plan 01 · M11 builds the gallery |
| Professor Oak in the lobby | Meets the player at (7, 15) the first time they come in after the National Pokédex | Not there | Plan 02 · S15 |
| Route 221's workers | Stand before the door until the National Pokédex | They stand there still | Plan 02 · S15 hides them |
| Looker at the Game Corner | Sits at a machine until the Cobble Badge, and again after Stark Mountain with other words | His first line; gone with the Badge | Plan 02 · S15 brings him back |
| The machines' sounds and the places' music | Their own | Effects the game already has (the coin, the gears, a beep, Clefairy's cry); Veilstone's theme in the hall, Oreburgh's in the museum, Sandgem's in the Villa, Route 206's in the lobby and the Great Marsh's in the park | Plan 05 · A6 |
| A name for a revived Pokémon | Asked for | Not asked | Plan 12 has no naming in the field yet |
| The two busts' conditions | A victory at each of the Battle Frontier's five facilities; a silver print or better at any of them | Never met: the busts aren't listed | Plan 06 · R18 fills `VillaRecordsNow` |
| The Guest Set's condition | Fifty battles at the Battleground (a game record, `RECORD_TIMES_BATTLED_AT_BATTLEGROUND`) | A story variable of our own naming, `VAR_TIMES_BATTLED_AT_BATTLEGROUND`, which nothing counts yet | Plan 02 · S15 builds the Battleground and counts into it |
| The Chandelier | Hangs over the room | Owned and counted, not drawn: it hangs out of the camera's sight | The doll's house has no ceiling |
| The Music Box and the TV | A tune; a programme | A line each | Plan 05, plan 08 |
| The catalogue as a book on the table | An object of its own | The table's own tiles open the order form | One thing fewer to stand on a table |
| Byron, Candice, Volkner and Flint | Their own looks | A gentleman, a lass and two youngsters, as their own Gyms have them | Plan 11 |

## The rooms of the south-west and the centre (2026-10-11, plan 01 · M11, part a)

The houses the areas of M5 and M6 still kept shut, on the original's plans: its tiles, its furniture by its models' names, its people on its events' tiles. The lines are our own, on the beats of each room's script in the decompilation.

- **A gift is given once, by the original's flag**: the Quick Claw (Jubilife's condominiums), the Dusk Ball, the Great Ball and the Heal Ball (Oreburgh's houses; the Heal Ball only to a team with a Geodude on it, as the original asks), the Sprayduck (the flower shop), TM88 (Floaroma's middle house, on a yes), TM67 (Eterna's condominiums), the Shell Bell (Hearthome's south-east house) and Bebe's Eevee, of level 20, which only a team with room takes.
- **A script rolls the original's die** (`random N`, the original's `GetRandom`, on the game's own chance). The flower shop's berry of the day is one of five (Cheri, Chesto, Pecha, Rawst, Aspear) and the Berry Master's one of twenty-six (Cheri to Tamato, the original's item number plus the roll); each is given once a day by its `FLAG_DAILY_...` flag, which the day's events clear.
- **The florist's swaps are the original's twenty-two**, at its prices (`sAccessoryShop_ItemLists`: one berry for a flower or a pair of specs, ten for the next five accessories, fifty or a hundred for the last nine). The berries are taken all or none, and come back when the Fashion Case has no room for the accessory.
- **The Berry Master's house**: the four mulches at 200 each; the Berry Searcher goes onto the Pokétch of whoever owns up to forgetting where their berries are planted; the book on the table is read where it lies.
- **A rest in Route 205's house heals the team**, behind a fade, as the original's does.
- **The assistant's sister tells of the day's swarm** once the Hall of Fame is behind the player, and her first word of it begins the swarms: `common.SwarmNews`, which plan 06 · R13 wrote and nobody started until her house was built.
- **The Old Chateau's wild Pokémon are met on its floor**: every tile of the original's behaviour `OldChateauFloor` (0x0B), by the area's own table and rate, as the Lost Tower's are. Its two ghosts are the original's (`CheckShouldShowGhost`): coming into the dining room rolls a die of a hundred and one and shows the old man at the head of the table on ten or less, and the room off the corridor shows the little girl on twenty-five or less; each goes its way and is gone, and its flag hides it the rest of the time.
- **The Old Chateau's back rooms are five files of one chunk.** The original draws the five rooms off the corridor in one piece of land and walks each from its own door. Here each room's file shows the rooms beside it over the partitions, cut to the wainscot as every inner wall is, and what lies on the floor lies only in the room it is reached from (the Dread Plate and TM90 are not shown a second time next door).
- **A lift has doors and no cabin**: the Hearthome houses' lift is a warp on the original's door tile in the back wall, which leads to the tile before the doors of the other floor.
- **A seat is walked onto**, as the original's stools and cushions are: whoever sits at a table stands on the seat's own tile.

| What | The original | Here, for now | Until |
|---|---|---|---|
| Rotom's television | The set in the Old Chateau's second room off the corridor: looked at after dark, a Rotom of level 20 comes out of it, once a day until it is caught (`FLAG_CAUGHT_OLD_CHATEAU_ROTOM`, `FLAG_DAILY_BATTLED_OLD_CHATEAU_ROTOM`) | A `Television` on (11, 3) and (12, 3) of `OldChateauBackMiddleWestRoom`, read from (11, 4) or (12, 4), that says nothing | Done since: plan 02 · S15, part c (the room's `TV` script) |
| Professor Oak in Eterna City's south house | Stands at (4, 5) once the National Pokédex is had (`FLAG_HIDE_ETERNA_CITY_SOUTH_HOUSE_PROF_OAK`), tells of the three birds that roam and sets them loose | Left out of the room | Plan 02 · S15 |
| The Pokémon News Press's assignment | The editor names a Pokémon the player has seen, wants one shown to him before the day is out, and pays three of one of twelve kinds of ball and a Heart Scale | He says who he is and what he is short of (`FLAG_TALKED_TO_POKEMON_NEWS_PRESS_GYM_GUIDE`) | A session that gives scripts a draw from the Pokédex and a deadline (plan 06) |
| "The National Pokédex" in these rooms' scripts | The grandfather's second line and the sister's swarms wait for the National Pokédex | `FLAG_GAME_COMPLETED` stands for it, as for Victory Road's collector | The National Pokédex being given (plan 02, after the Hall of Fame) |
| Bebe's nickname question | Asks whether the Eevee should have a nickname | Not asked | Plan 12 · Q19 |
| The florist's window | One window lists all twenty-two accessories with their prices | Question boxes of six answers at most, in three steps, and one swap to a talk | A shop screen that takes berries |
| The lifts' cabins | A cabin of its own (`hearthome_city_southeast_house_elevator`) with a menu of floors | The doors lead straight to the other floor | Plan 01 · M11, part d, with the Department Store's and the TV station's lifts |
| The baby in the pram | An object of Hearthome's north-east house | Left out: no look stands for one | Plan 11 |
| The looks of these rooms' people | Their own object looks (an expert, a scientist, a reporter, a roughneck…) | Looks of ours by `npcType` (a lady, a lass, a gentleman…), each with its own name | Plan 11 |
| The Old Chateau's two flights | Stairs with balustrades | Plank ramps between low walls | A staircase for rooms on two levels (plan 04's kit) |
| The herb shop's counter | A wooden counter | The Mart's own counter, which is what a clerk serves across | A counter of a house's own |
| The Old Chateau's theme | `SEQ_D_03` | Turnback Cave's plays | Plan 05 · A6 |
| The Trainers' School, the Pokétch Company and Jubilife TV | Rooms on the original's plans, the last two of several floors | The rooms made by hand (a classroom, one room for the company's three floors, the television station's lobby alone) | Plan 01 · M11, part d: the second chapter's scenes, the Lottery Corner and the Pokétch's gifts stand on their tiles, and the upper floors' people speak for plan 08's programmes |
| The player's and the rival's houses, the professor's lab | Rooms on the original's plans | The rooms made by hand | The story lane's: the first chapter's scenes, a new game's first tile and a white-out's bed stand on their tiles |

## The tenth chapter: Sunyshore (2026-10-11, plan 02 · S13)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_sandgem_town_pokemon_research_lab.s`, `scripts_counterpart_talk.s`, `scripts_twinleaf_town_player_house_1f.s`, `scripts_verity_cavern.s`, `scripts_valor_cavern.s`, `scripts_acuity_cavern.s`, `scripts_valor_lakefront.s`, `scripts_route_222.s`, `scripts_sunyshore_city.s`, `scripts_init_sunyshore_city.s`, `scripts_vista_lighthouse.s`, `scripts_vista_lighthouse_elevator.s`, `scripts_sunyshore_city_pokecenter_1f.s`, `scripts_sunyshore_city_mart.s`, `scripts_pokemon_league_hall_of_fame.s`, `scripts_pokemon_league_north_pokecenter_1f.s`, `scripts_init_new_game.s`, the events files of those places, and `src/binoculars_vista_lighthouse.c`)

- **It is the professor's lab that opens Sunyshore and brings the guardians home**, not the Distortion World's end. While `VAR_SANDGEM_TOWN_LAB_STATE` is 2 (Sendoff Spring sets it) the professor and the assistant stand inside the lab's door and the scene plays as the player comes in. It sets `VAR_EXITED_DISTORTION_WORLD_STATE` 3, `FLAG_HIDE_VALOR_LAKEFRONT_COLLECTOR` and `VAR_VALOR_LAKEFRONT_BLOCK_SUNYSHORE_STATE` 1 (the man who turns people back east of Valor Lakefront, and his trigger), clears `FLAG_HIDE_VERITY_CAVERN_MESPRIT`, `FLAG_HIDE_VALOR_CAVERN_AZELF` and `FLAG_HIDE_ACUITY_CAVERN_UXIE`, and leaves the state 3.
- **Between that scene and the player's arrival in Sunyshore City** the two stay by the door: the professor sends his regards to Cynthia, the assistant wonders about the Pokémon the player met. Arriving in Sunyshore sets `FLAG_HIDE_SANDGEM_TOWN_LAB_COUNTERPART` and the state 4.
- **Mom asks whether everything is all right** from the tremor (`VAR_CANALAVE_LIBRARY_STATE` 2) until `VAR_EXITED_DISTORTION_WORLD_STATE` is 2.
- **Mesprit is not battled in its cavern.** Spoken to, it cries, flickers three times and is set roaming (`ActivateRoamingPokemon ROAMING_SLOT_MESPRIT`: level 50). Professor Rowan then comes in, from a tile that hangs on the way the player faces, seven steps up the cave to their side; he speaks of the Marking Map, and has a line more for someone who lacks the app. If Mesprit's state was `ROAMER_STATE_RESET` (3) it is made `ROAMER_STATE_ROAMING` (0) instead and nobody comes.
- **Azelf and Uxie are wild Pokémon at level 50, and the battle may be run from** (`StartLegendaryBattle`). Lost: a white-out, and the Pokémon waits. Any other end takes it from its cavern (the original sets its flag as the cavern loads again behind the battle); caught, `FLAG_CAUGHT_AZELF` or `FLAG_CAUGHT_UXIE` is set; knocked out or run from, a line says it has gone into the cavern.
- **The Hall of Fame brings them back**: Azelf's and Uxie's flags are cleared while they haven't been caught, and a Mesprit knocked out as a roamer (`ROAMER_STATE_DEFEATED`, 2) is put back in its cavern with the state `RESET`. One caught stays caught.
- **Each cavern marks its first arrival** (`FLAG_FIRST_ARRIVAL_VERITY_CAVERN` and its fellows), and Valor Cavern's way out leads to whichever Lake Valor the story is at.
- **Route 222's rich boy gives TM56 once** (`FLAG_RECEIVED_ROUTE_222_TM56`).
- **Flint meets whoever comes into Sunyshore City** while `VAR_SUNYSHORE_CITY_STATE` is 0 (an on-frame script, by the gate's row the player stands in), and then stands before the Gym's door (state 1) until `FLAG_VOLKNER_RETURNED_TO_GYM`. Spoken to after it, he leaves for the Pokémon League.
- **Volkner stands at the top of the Vista Lighthouse from the start**, on the tile the binoculars are read from. Spoken to, he walks round the shaft to the lift and is gone (`FLAG_HIDE_VISTA_LIGHTHOUSE_VOLKNER`), which sets `FLAG_VOLKNER_RETURNED_TO_GYM`.
- **The lighthouse's lift** carries whoever comes into its car to the other end: two steps in, the ride, two steps out, to 6, 10 of the gallery or to 886, 791 of Sunyshore City, facing south either way.
- **The rival and Jasmine**: once Volkner's script has set the city's state to 2, the row of five tiles two south of Jasmine starts the scene. Nobody battles. Jasmine gives HM07 (`FLAG_RECEIVED_SUNYSHORE_CITY_HM07`), and the state is 3. Spoken to before the Badge, she says only that the sea will turn the player back.
- **`VAR_RIVAL_BEAT_SUNYSHORE_GYM` is set by the rival's battle in the League's Pokémon Center**, of whose trigger it is the variable, not by anything in Sunyshore: the Gym's statues name him from then on (plan 02 · S14).
- **A new game hides** the professor in Verity Cavern (`FLAG_HIDE_VERITY_CAVERN_PROF_OAK`, as the decompilation names his flag) and the rival in Sunyshore City.

**Our own choices**

- **A guardian is brought back only where it hasn't been met already.** The lab's scene clears Azelf's and Uxie's flags unless their `FLAG_CAUGHT_` is set, and Mesprit's only while its roaming state is 0. In the original nothing can have happened to them by then; a save from before the chapter may hold one, and story version 16 counts a guardian the player has as caught.
- **Sunyshore's arrival moves the lab's state on from 3 only.** The original does so from 2 as well, for someone who has not heard the professor out, which nobody there can be (the road opens with his scene); a save from before the chapter can, and for it the lab's scene still waits.
- **Someone who first comes to Sunyshore City any other way than by the gate** finds Flint before the Gym already, as a scene missed elsewhere has moved on (plan 02 · S9's Cynthia). The original's on-frame script has no branch for it.
- **Flint stops a step short of the original's tile** at the gate: here whoever comes through a gate stands a step outside it. He goes on east and north out of sight before he is put before the Gym.
- **The lift knows its way by a variable of our own**, `VAR_VISTA_LIGHTHOUSE_FLOOR`: 0 set by Sunyshore City's arrival script, 1 by the gallery's. The original asks the floor its dynamic warp remembers (`GetFloorsAbove`). The car's own door leads out to the city, for whoever is ever left standing in it.
- **The professor walks seven steps back down Verity Cavern**, not nine: the original's nine take him through the cavern's wall.
- **The professor and the assistant stand two rows inside the door** of our lab, which is smaller than the original's, and the player takes a step in as the scene begins, so that nobody stands under the text.
- **The lighthouse's two rooms are on the original's plans.** The shaft at the gallery's middle is an inner wall, cut away like any other, and the car stands open in its south face (style guide, "Rooms": a lift).
- **The caverns' battles are fought on the cave's stage** (their overlays' `battleArena`): their floor is wet stone that mirrors whoever stands on it, which is no cave floor to the field, so the stage would have been a meadow.
- **Sunyshore's Pokémon Center and Mart** are the towns' common rooms, so their people stand where those have room, not on the original's tiles.
- **Saves from before** (story version 16): `common.ChapterTen` hides the professor in Verity Cavern and the rival in Sunyshore City. A save that has already been to Sunyshore City, or holds its Beacon Badge, walked in when nobody stood at the gate and the Gym was open: for it Flint has gone, Volkner is in his Gym and the road from Valor Lakefront is open, and with the Badge the city's state is 2, so the beach's scene and HM07 wait.
- **The cries written out** ("Kyuoon!", "Kyuuoh...", "Kyuuuhn!") are our own.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The binoculars | The picture goes to the Pokémon League across the sea: the map itself is changed, with a camera of its own, until a button is pressed | Two lines over a black screen that say what is seen | Plan 09 · L12 (set pieces) |
| Mesprit's picture | Shown over the field before its cry (`DrawPokemonPreview`) | Its cry and a line | Plan 09 · L12 |
| The lift's ride | The car's lights scroll and its door opens and shuts (`PlayElevatorAnimation`, the door's animation in the gallery) | The lift's own set piece since plan 09 · L12 ("The original's set pieces", below); the car stands open in its shaft | A door that opens for a ride is not built |
| Looks | Flint's, Volkner's and Jasmine's own; sailors, a collector | Flint and Volkner as youngsters, as in their rooms at the League and the Gym; Jasmine a lass; the sailors gentlemen. The gallery's young visitor is a lass, or Volkner would stand beside his twin | Plan 11 · C6, C2 to C4 |
| The lake caverns' theme | Their own (`D_RYAYHY`) | The cave's | Plan 05 · A6 |
| Volkner's words in battle | A Leader speaks during the battle itself | Nothing | Plan 11 · C12 |
| The rival's branch of the Gym's statues | Named beside the player once he has battled before the League's door | Not written | Plan 02 · S14 sets the variable |
| The lab's other people | Two scientists and its shelves, with lines for this stretch of the story | Not there: the lab is still the room made by hand | Plan 01 · M11 |
| The gate house and the houses | Route 222's gate with its two people; Sunyshore's and Route 222's houses | Walked through; locked | Plan 01 · M11 |

## The Battle Frontier's frame and the Battle Tower (2026-10-11, plan 06 · R18a)

**Platinum's rules, kept as they are** (`src/battle_frontier_save.c` and `include/constants/battle_frontier.h`, `src/wifi_battle_tower_save.c`, `src/unk_02049D08.c`, `src/unk_0204AEE8.c`, `src/unk_0206B9D8.c`, `src/overlay104/frontier_opponents.c`, `ov104_0223A0C4.c` and `ov104_022394A4.c`, `src/frscrcmd_battle_tower.c`, `src/unk_020494DC.c`, the Frontier's branches of `src/battle/`, `sBattleFrontierBanlist` in `src/pokemon.c`, `res/trainers/frontier/`, and the scripts of the Battle Tower's seven rooms, its battle rooms and the Battle Frontier's grounds):

- **The records** (`BattleFrontierStatsIndex`): a hundred numbers in the original's order, the Tower's ten first, then the Factory's, the Hall's, the Castle's and the Arcade's, none past 9,999; a record is kept only when bettered; a streak "runs" for one way of taking one facility's challenge, and that is one bit of seven sets.
- **Battle Points**: 9,999 at most, given and spent whole. The Exchange Service Corner's west booth trades for 26 things (the six vitamins for 1 BP each, the six Power items and the two Orbs for 16, the two Herbs for 32, nine things to hold and the Rare Candy for 48), and its east booth for 15 Technical Machines (32 to 80 BP), at the original's prices in the original's order.
- **Who may go in**: no Egg and none of the eighteen banned species (Mewtwo, Mew, Lugia, Ho-Oh, Celebi, Kyogre, Groudon, Rayquaza, Jirachi, Deoxys, Dialga, Palkia, Giratina, Phione, Manaphy, Darkrai, Shaymin, Arceus); no two of one species; no two holding the same item (holding nothing is no item in common). A counter turns away a team that has too few such Pokémon before anyone is chosen, and names the banned species the Pokédex has seen.
- **A Frontier battle** (`BATTLE_TYPE_FRONTIER`): the team is a copy of the Pokémon entered, brought down to level 50 where they are above it and left at their own level below; nothing the battle does reaches the Pokémon themselves. Nothing comes out of the Bag. RUN asks whether to give the match up, and giving it up is a battle lost. No EXP, no effort, no prize money either way and none scattered by Pay Day, nothing picked up, no waking in a Pokémon Center. Every Pokémon obeys, the Pokédex is told of nothing it sees, a Burmy keeps its cloak, a Soul Dew does nothing, and a trainer's Pokémon may steal an item (which the battle's end gives back, the team being a copy). The trainers think with three routines (basic, attack evaluation, expert) and use no items.
- **The trainers and their Pokémon**: 300 trainers, five partners and ten Brains, each with a list of sets drawn from 950. A set is a species, four moves, a nature, an item and the stats its effort goes into (510 between them, 255 at most each). Every IV is the same and goes by the trainer's number: 3 under trainer 100, three more for each twenty after, 31 from trainer 220. A personality is drawn in pairs of sixteen bits until it gives the set's nature and isn't shiny; the ability is its lowest bit's. Friendship is full, or none with Frustration.
- **The Battle Tower**: a set is seven trainers, a Single Battle with three Pokémon, a Double Battle with four, a Multi Battle with two beside a partner's two. The set's trainers are drawn as the challenge goes up, from the row of the room the streak has reached (the seventh from the bosses' row), none twice; each one's Pokémon are drawn as they are come to, no two of a species or an item, a pair's kept apart from each other's. All of it is on the Tower's own generator (`state × 48828125 + 1`, the draw the state over 65,535), seeded from a number kept with the records: a new streak moves that number on (`× 1566083941 + 1`), a streak that goes on doesn't, and a new day starts it from the day's number. So a room is the same however often it is come back to.
- **Between battles** the team is healed, and the challenge can be gone on with, put down or given up. Seven won earn Battle Points by the room the streak is left in (3, 3, 4, 4, 5, 5, 7 and 7 from then on), add seven to the Trainer Card's score, and the next challenge starts in the next room. A battle lost or a challenge given up ends the streak where it stands.
- **The Tower Tycoon** is the seventh trainer of the third and of the seventh set of the Single Battle Room (battles 21 and 49). Beaten, he is worth 20 Battle Points, the lobby hands over a silver or a gold print (`VAR_BATTLE_TOWER_PRINT_STATE`: 1 beaten, 2 handed over, 3 beaten again, 4 handed over), and the Pokémon entered are given the Ability Ribbon or the Great Ability Ribbon. A streak of fifty in the Double or the Multi Battle Room gives them the Double or the Multi Ability Ribbon. The fifth gold print sets `FLAG_OBTAINED_ALL_BATTLE_FRONTIER_GOLD_PRINTS` and adds a thousand to the score.
- **Trophies**: a Single Battle Room record of 20, 50 and 100 earns the bronze, the silver and the gold trophy, goods sent to the secret base's PC one at a time; with no room there, one waits, and someone stands beside the middle counter until it is collected.
- **The Brains' frame** (`FrontierBrains`): every facility's Brain comes at its battles 21 and 49 (the Hall's at 50 and 170), each has a print variable of the same five values, and a round is seven battles (the Hall's ten).
- **The judge** in the lobby adds a Pokémon's six IVs up and puts the sum in one of four ranks (to 90, to 120, to 150, beyond), names the stat of the highest (the first of them in the order HP, Attack, Defense, Speed, Sp. Atk, Sp. Def) and puts that one in one of four as well (to 15, to 25, to 30, 31). What he calls each rank is ours.

**Quirks kept**

- A room's number is passed in a byte as the Tower's trainers are drawn: the 257th room draws from the first room's row again.
- Two sets that hold nothing count as holding the same item when a trainer's team is drawn; the player's own Pokémon may all hold nothing.
- After fifty refusals for an item's sake a trainer's team is drawn without looking at items, and is handed Bright Powder, a Lum Berry, Leftovers and a Quick Claw by its places instead.
- The Battle Points for a set go by the room the streak has just come into, so the first set and the second both earn 3.

**Our own choices**

- **The team is entered on the team's own screen** (`PartyScreen.OpenToEnter`): each Pokémon chosen is numbered in its order, one that may not go in says so, and the button confirms once enough are in. The original's is a mode of its party menu with an ENTER and NO ENTRY menu on each Pokémon.
- **RETIRE and REST are asked once**: answered no, the challenge goes on to the next battle, where the original goes back to its menu.
- **A challenge put down** is saved in the lobby, which asks at once whether to go on with it; left for later, either counter's attendant takes it up again. The original closes the game there. A challenge whose Pokémon are no longer where and what they were ends, since here one can walk away in between. A game closed in the middle of a challenge finds its streak ended, as the original's does.
- **A trainer of a battle room says nothing**: the original's lines are its own text, and we have 300 trainers. The room says who steps up; the Tycoon has lines of ours.
- **The Exchange Service Corner asks once**: a prize is chosen from a list and the talk ends (as the Game Corner's exchange does here); the original's counter stays open.
- **The lifts' doors are doorways in the lobby's back wall** behind each booth, the attendant walking in ahead; the corridor's door is one of four, drawn on the game's own chance.
- **The Tower's draws use a number seeded from the day's number of this game**, which isn't a cartridge's for the same day: the generator and every step of it are the original's, the rooms it gives are ours.
- **What the Bag is refused with and what RUN asks** are our words.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The Wi-Fi Battle Rooms and their two machines | Teams sent in over the Wi-Fi Connection, seven rooms, ranks, a leaders' board | The east counter's attendant says the rooms are closed; the machines have no data | Decision 06 · 7: nothing comes in over the air; the World Ability Ribbon waits with it |
| The Multi Battle Room with a friend | A link to another console from the west counter | Only the Battle Salon's five partners; the question isn't asked | Plan 07; the Pair Ability Ribbon waits with it |
| Marley and Buck in the Battle Salon | There once they have been travelled with | Their flags (`FLAG_TRAVELED_WITH_MARLEY`, `FLAG_BUCK_LEFT_BATTLEGROUND`) are nobody's to set yet | Plan 02 · S15 sets them |
| A battle kept as a Battle Video | Asked after each battle, and the Vs. Recorder's saved file | Not asked | Plan 08 · P10 writes the file; R12's Vs. Recorder is as it was |
| The reporter in the lobby and on the grounds | Interviews whoever has a streak to talk about | Not there | Plan 08's television |
| The scratch-off cards | A booth of the Exchange Service Corner, a Battle Point a card | The attendant says the cards haven't come | Not built: no session has it yet |
| The Tower's music, the lift's and the doors' sounds | Their own | Veilstone City's theme, and sounds the game has | Plan 05 · A6 |
| The Tower Tycoon, Riley, Marley, Buck, the attendants and the judge | Their own looks | A gentleman, a clerk, a lass, a youngster, lasses and a scientist | Plan 11 |
| The battle rooms' stage | The Tower's own background and platforms | A Gym's hall | Plan 04's arenas have none for the Frontier |
| The Frontier's other four facilities | The Battle Factory, Hall, Castle and Arcade | Their doors stay shut; their records, prints and rules are in the frame | Plan 06 · R18b to R18d |

## The rooms of the east and the sea (2026-10-11, plan 01 · M11, part b)

The houses the areas of M7 still kept shut, on the original's plans, made as part a's were. The lines are our own, on the beats of each room's script in the decompilation.

- **A gift is given once, by the original's flag**: the White Flute (the Hotel Grand Lake's east cottage at Valor Lakefront), TM92 (the clown in the north-west cottage on Route 213), the Soothe Bell (the maid of the Pokémon Mansion's middle room), TM48 (Canalave's south-east house), the Macho Brace, the Porygon and the five scarves below. The Pokétch's Coin Toss comes from the rich boy of the north-east cottage and its Matchup Checker from the observatory's cowgirl.
- **The Suite Key.** The woman before the east cottage's door at Valor Lakefront has dropped its key, which the import hides beside the hotel's reception on Route 213. Handed it, she lets herself in (`FLAG_HIDE_VALOR_LAKEFRONT_BEAUTY`), and from then on is in the cottage with the White Flute (`FLAG_HIDE_GRAND_LAKE_VALOR_LAKEFRONT_EAST_HOUSE_BEAUTY`, which plan 02 · S8 sets from the start). Until then nobody comes to that door: she stands on the tile before it, as the original has her.
- **The Massage Girl** (Veilstone's south-west house; `scripts_veilstone_city_southwest_house.s`): once a day one Pokémon of the team grows friendlier by three, as `ScrCmd_IncreasePartyMonFriendship` counts it (half as much again with a Soothe Bell, then one more out of a Luxury Ball, never past 255), and she finds one of sixteen accessories, Pretty Dewdrop to Glitter Powder, drawn among those the Fashion Case still has room for (`TryGetRandomMassageGirlAccessory`). An Egg is not massaged. The original's one more point for a Pokémon met in the place where the script runs is left out: a Pokémon here keeps no such place.
- **The Scarf Guy** (Pastoria's north-east house) looks at the first of the team that is no Egg and gives the scarf of each contest condition it has at 200 or more, in the original's order (Cool, Beauty, Cute, Smart, Tough), one scarf to a visit and each once.
- **The boy and his Combee** (Pastoria's middle house): a male and a female Combee on the team at once, neither an Egg, for the Macho Brace (`CheckPartyCombeeGenderCount`).
- **Pastoria's berry of the day** is one of the seventeen that weaken a hit of one type, Occa to Chilan in the order of the original's item numbers, by its die of seventeen.
- **The old man of Route 221** rewards whoever shows him a Pokémon of the team at the day's level (`VAR_DAILY_RANDOM_LEVEL`): a Black Belt, an Expert Belt, a Focus Sash and round again, once a day. The original draws the first level as a game begins; here the day's events draw it, so in a game's first day he makes do with a number of his own, 25.
- **The Safari Game is started at the observatory's counter** (`PastoriaCityObservatoryGate1F`), as in the original: the room's coordinate event before the attendant asks, takes the fee and lets the player through the back door into the marsh. Coming back through that door with a game on, the attendant asks whether to leave (yes ends the game, no leads back in); a game that ended in the marsh brings the player to the same tile (`VAR_SAFARI_GAME_STATE` 2, set by `common.SafariTimeUp` and `SafariOutOfBalls`). Either way the cowgirl gives the Matchup Checker for five Pokémon or more caught in that one game, which the Safari Game now counts (`SafariGame.Caught`, kept in the save). Whoever is found in the marsh with no game on is shown back to the counter.
- **The Seven Stars Restaurant**: nine couples, five of whom have a table each day, drawn by the original's die of nine as the room is come into; a couple asks for a double battle, two trainers against the player's two Pokémon, and may be battled again the next day with their teams afresh. A couple beaten says so for the rest of the day by a flag of our own (`FLAG_DAILY_BEAT_RESTAURANT_<ONE>_<OTHER>`), where the original clears their trainer flags each morning: the story's record of who has been beaten stays as it is.
- **The Pokémon Mansion's maids** hold the 5-Maid Knockout Exact-Turn Attack Challenge as the original does: five maids one after another, and for a sweep in exactly the day's number of turns (five to nine, a die of five, drawn once a day) a battle with the young master or the young lady of the house, by a die of two. The turns are the original's count (`VAR_TOTAL_TURNS_LAST_BATTLE`, added up over the five): a battle's last turn is not one, so a battle won at once counts none. The challenge is taken once a day, by a flag of our own (`FLAG_DAILY_TOOK_FIVE_MAID_KNOCKOUT_CHALLENGE`).
- **Mr. Backlot** adds a Pokémon to the Trophy Garden once a day (plan 06 · R13's `trophygarden`), after agreeing with him twice, and his butler is sent out to see to it.
- **The maids' three rooms are three maps.** The original keeps them side by side in one map, each behind its own door of the corridor; here each is a map of its own, so each is come into and left by its one door.
- **A trainer battled again brings a fresh team.** A script that battles someone already beaten, whose team has no Pokémon left able to fight, has it built afresh from Platinum's data, as the original builds a trainer's team for every battle.

| What | The original | Here, for now | Until |
|---|---|---|---|
| The restaurant's hours | Open from nine in the morning to eleven at night, by the hour | Shut late at night and in the morning (midnight to ten), by the time of day, which is what a script can ask | A script can ask the hour |
| The guard of Mr. Backlot's statue | Away from two to six in the morning | Away late at night (midnight to four) | The same |
| The restaurant's lines | Three sets for each couple, one drawn each day | One set | Plan 08, if ever |
| The binoculars of the observatory | The picture swings over the marsh and shows a Pokémon that lives where it stops | A line names the Pokémon the day has brought to one of the six areas, by the script's die, from the Sinnoh Pokédex's list | Plan 09's camera kit for the view; the National list once a script can ask for the National Pokédex (plan 02 · S15) |
| No room for a Pokémon at the counter | The attendant turns away whoever has a full team and full boxes | Not asked | A script can ask the boxes |
| The game director's diplomas | He shows the diploma on the spot, for Sinnoh's Pokédex and for the National one | He knows a complete Pokédex of Sinnoh and sends the player to open the Pokédex, which hands its diplomas out itself (plan 03 · D10's ruling) | The ceremony moved to him, if it is wanted |
| "The National Pokédex" in these rooms' scripts | Mr. Backlot's and his butler's second lines wait for it | `FLAG_GAME_COMPLETED` stands for it, as in part a's rooms | Plan 02 · S15 |
| The Harbor Inn | Opens for the night Darkrai calls the player, given by a distribution of its own (the Member Card) | Its door in Canalave City stays shut, and its room is not built | Plan 02 · S15b, if the event is given |
| Sailor Eldritch at home, Byron in Iron Island's cabin | The sailor comes in once his son is woken with the Lunar Wing; Byron waits with a Metal Coat once the National Pokédex is had | Left out of the rooms; the boy and his mother say what they say before the nightmare, and the cabin is empty | Plan 02 · S15 and S15b |
| The reporter in Canalave's west house | There while one of Jubilife TV's programmes wants an interview | Left out: the house is empty | Plan 08 |
| Pal Park's viewing gallery | An upper level reached by an escalator, with four spectators | Roped off, as plan 06 · R16b left it | Plan 01 · M11, part d, with the escalators of the Pokémon Centers and the Department Store |
| Naming the Porygon | Asked for | Not asked | Plan 12 · Q19 |
| The cafe's bar, the hotel's reception, the observatory's counter | Counters of each room's own | Wooden tables, spoken across by the original's own tile behaviour | A counter of a house's own |
| The looks of these rooms' people | Their own object looks (a maid, a waitress, a policeman, a cameraman, a game director…) | Looks of ours by `npcType` (a lass, a lady, a gentleman…) | Plan 11 |
| The moves' tutors and the two move men | Route 212's shard tutor, Route 210's Grandma Wilma, Pastoria's Move Relearner, Canalave's Move Deleter | Their houses stay shut | Plan 01 · M11, part d |

## The Battle Factory (2026-10-11, plan 06 · R18b)

**Platinum's rules, kept as they are** (`src/overlay104/battle_factory.c`, `battle_factory_helpers.c`, `frscrcmd_battle_factory.c`, `src/battle_factory_save.c`, `src/scrcmd_battle_factory.c`, `src/battle_frontier_stats.c`, `scripts_battle_factory.s`, `frontier_scripts_battle_factory.s`, and the rental application, `src/applications/frontier/battle_factory/main.c`):

- **Nothing of the player's own goes in.** A challenge is Single or Double, at Level 50 or at the Open Level, where every Pokémon is of level 100; each of the four has its own streak, record and count of trades.
- **A round** is seven battles. Its fourteen trainers are drawn as it begins, none twice (the seven a solo challenge meets are the first seven), from the round's row of the table: one span for the first six battles and another for the seventh, the eighth round's row for every round after. The remainder is taken by a span's width, so its last trainer is never drawn.
- **The six on offer** are drawn from the row of the round (at Level 50 sets 1 to 150 with IVs of 0 in the first round, then 151 to 250 with 4, 251 to 350 with 8, 351 to 486 with 12, 487 to 622 with 16, 623 to 758 with 20, 759 to 894 with 24, and from the eighth round any of 351 to 950 with 31; the Open Level's rows start at set 351), no two of one species and no two holding the same item. **Trades lift some of them** into the next row, with its IVs: none for a streak of up to 6 trades, the last one of the six from 7, two from 14, three from 21, four from 28 and five from 35; the last two places are then each swapped with one of the six, drawn, so a lifted one can stand anywhere. The eighth round's row can't be lifted.
- **A trainer's three** are drawn from the row the trainer's number falls in, never lifted, and share neither a species nor an item with the six on offer (the first trainer's) or with the three in hand and the three just beaten (every trainer after).
- **Three are chosen in the order they go into battle**, and the taking counts as one trade for the streak. Both teams go into every battle whole.
- **What the staff tell** of the trainer to come goes by the round: all three species in the first, two in the second, the first species and its first move in the third, that move alone in the fourth, and from the fifth the type most of the three have (each Pokémon counting once for each of its types, the first in the original's order of types winning a tie) or that no type is shared by two.
- **How the trainers think** goes by the round too: at random in the first two rounds, with the basic routine alone in the third and fourth, with all three routines from the fifth.
- **After a battle won** the challenge can be gone on with, put down or given up. Going on, the next trainer's three are drawn and told of before a trade is offered: one of the three in hand for one of the three just beaten, which comes over as it was (its set, IVs and personality). A trade counts for the streak and for the game's own record of trades.
- **Seven won** earn Battle Points by the round the streak goes on to (5, 5, 5, 5, 7, 7, 8, and 9 from the eighth), add seven to the Trainer Card's score, and the streak runs on: the next challenge of that way and level takes up its wins and its trades. A battle lost or a challenge given up ends the streak where it stands; a game closed in the middle of a challenge loses it, with its latest streak and trades set to nought.
- **The Factory Head** is the 21st and the 49th battle of a Single streak. His three are rentals like anyone's, from a row kept for him (at Level 50 sets 351 to 486, IVs of 12 the first time and 31 the second; at the Open Level 759 to 950), and he thinks with all three routines whatever the round. Beaten, the round is worth 20 Battle Points and the lobby hands over the silver or the gold print (`VAR_BATTLE_FACTORY_PRINT_STATE`).
- **The records**: the latest streak and its trades, and the record streak with the trades of the streak that set it (when a record is only equalled, the more trades of the two are kept).

**Quirks kept**

- The round's fourteenth trainer is worked out as though a solo challenge met fourteen: in the second and the sixth round of a Single challenge it is the Factory Head, whom nobody meets there.
- Two sets that hold nothing would count as holding the same item (every set of the tables holds something).
- The Battle Points go by the round the streak has just come into, so the first round earns the second row's 5.
- A streak taken up past its eighth round keeps the round's count it began with, which only the eighth row's tables read.

**Our own choices**

- **The lobby's floor is level.** The original comes in at the head of three flights of stairs, six tiles above the floor; a room's walls stand at one level here, and a floor sunk under its own door drew as a pit with earth for sides. Its tiles are the original's: where the rails of the three flights block, either side of the aisle from the door, benches stand.
- **The corridor and the battle room are rooms.** The original plays everything past the lobby as a scene over two pictures; here they are two small rooms laid out from where that scene stands its people (the staff before the door, the challenger at the west end of the battle room's floor and the trainer at the east), and the scene is their arrival scripts.
- **One screen for the rentals and the trade** (`FactoryScreen`), in the menus' look, for the original's conveyor of Poké Balls: the same steps and the same ways back (a Pokémon's menu of SUMMARY, RENT or REMOVE, CANCEL; Cancel takes the last one back; the third asks; a trade's BACK and CANCEL, each asked once more). Only one's own Pokémon are read in full, as in the original.
- **A rental's summary** is one page with its ability and item on it; the original opens its summary of four pages.
- **REST and RETIRE are asked once**, and a challenge put down is saved in the lobby and taken up at the counter, as the Battle Tower's is here. Nothing can be changed in between: the three rentals are kept with it.
- **The draws are on the game's own chance** in the original's order; what was drawn is kept, so a challenge put down comes back as it was.
- **A trainer of the battle room says nothing**, as in the Tower; the Factory Head has lines of ours, with the three rentals named and a number from 10 to 99 as the original has them.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The Multi challenge | Two players side by side over a link, two rentals each | The second counter's attendant says nobody can be linked up with; its stats and running flags are in the records | Decision 06 · 7; plan 07 |
| A battle kept as a Battle Video | RECORD in the menu between battles | Not offered | Plan 08 · P10 |
| The reporter in the lobby | Interviews whoever has a streak to talk about | Not there | Plan 08's television |
| The Factory Head's coming | Smoke, rising blocks and a sound of his own as he appears | The floor shakes and he stands there | Plan 09's effects, plan 05 · A6 |
| The Factory's music and its door's sound | Their own | Veilstone City's theme and a sliding door | Plan 05 · A6 |
| Thorton, the attendants and the staff | Their own looks | A gentleman, lasses and a scientist | Plan 11 |
| The battle room's stage | The Factory's own background and platforms | A Gym's hall | Plan 04's arenas have none for the Frontier |
| The lobby's stairs | Three flights down from the door | A level floor, benches where the rails stand | A room's shell stands at one level; whoever gives rooms a sunk floor (plan 04, plan 01 · M11) can put the original's plates back (they are in the importer's `--room battle_factory`) |

## The eleventh chapter: the Pokémon League (2026-10-11, plan 02 · S14)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_pokemon_league_north_pokecenter_1f.s`, `scripts_pokemon_league_hall_of_fame.s`, `scripts_sunyshore_city_gym_room_1.s`, `scripts_twinleaf_town_player_house_1f.s`, `scripts_counterpart_talk.s`, `scripts_victory_road_1f.s`, `scripts_pokemon_league.s`, `scripts_pokemon_day_care.s`, `scripts_init_new_game.s`, `src/clear_game.c`, `src/cutscenes/hall_of_fame.c`, `src/location.c`)

- **The way there asks for five field moves.** Route 223 is sea and a waterfall; Victory Road's three floors ask for Rock Climb, Rock Smash and Strength before they let anyone out at the League's end, and for Surf and Waterfall again inside. A test walks it with all five and with each of them missing.
- **The rival battles whoever first walks up to the League's guard**: the row before him, three tiles wide, while `VAR_RIVAL_BEAT_SUNYSHORE_GYM` is nought. He comes in from the mat inside the way out (his object's tile, 11, 11) to the tile below the player's, to his own theme, with the team that goes with the player's starter (`rival_pokemon_league_*`). Beaten, he leaves the way he came and the variable is 1; a battle lost is a white-out, he is hidden again, and the scene comes at the next step onto the row.
- **That variable is the Sunyshore Gym's statues' too**: once it is set they name the rival after the player. The player's name comes first there alone, the player's Badge having been the first.
- **The guard counts the eight Badges off**, town by town, before the Badge's fanfare.
- **The Hall of Fame's words go by whether the League has been won before** (`GetLeagueVictories`, which is nought exactly until the game is first completed): the professor on how long it has been and Cynthia on when she became Champion, or the professor on standing straighter and Cynthia on what the room holds. Then her welcome and what the machine keeps.
- **What a victory sets** (`SetHallOfFameVictoryFlagsAndVars`), each under its own name: `FLAG_UNLOCKED_VS_SEEKER_LVL_4` and, with the National Pokédex, the Spring Path on the map (both set before the machine's balls); `FLAG_HIDE_DAY_CARE_GYM_GUIDE` and `FLAG_ALT_MUSIC_CHAMPION_ROOM` cleared; Dialga's and Palkia's states at Spear Pillar put back to nought unless caught; Heatran shown, with Stark Mountain's third room's state 1, unless caught and only once Buck has been spoken to at the Battleground; Regigigas shown unless caught and only with the National Pokédex; Azelf, Uxie and Giratina shown unless caught; each of the five roamers that was knocked out (state 2) made ready to be set loose again (state 3), Mesprit back in its cavern and Cresselia on Fullmoon Island for it; `VAR_PLAYER_HOUSE_POSTGAME_STATE` 1 if it was nought; the assistant in Sandgem Town, the professor in the Veilstone Department Store's basement unless he has been spoken to there, and Lucian in Canalave's library shown; Cynthia's talk in Celestic's ruins made new, and Cynthia gone from Sendoff Spring.
- **Then, in `ClearGame`'s order**: the Champion Ribbons and the record, the game completed, the scene of the team's entry, the team healed and the game saved **in the player's room** (`SetPlayerStartLocation`: the save is written with the player already there), and the credits.
- **The scene of the team's entry** shows each Pokémon in turn (it comes in, cries, and its lines appear in three steps: the welcome and its name, what it is, whose it was and where it was met or hatched), then the Champion with the team coming in from both sides, the first to the right and the second to the left, the title, the Champion's name, ID number and time played, and paper falling until a button is pressed. Eggs are not shown.
- **Mom has news once**, the first time the player is downstairs at home with `VAR_PLAYER_HOUSE_POSTGAME_STATE` 1 (it becomes 2): the rival came looking for them, shouting about a ship at Snowpoint City; and without the National Pokédex she asks how the professor's Pokédex is coming along (`FLAG_TALKED_TO_MOM_ABOUT_NATIONAL_DEX_PROGRESS`; with it, `FLAG_TALKED_TO_MOM_ABOUT_RIVAL_SNOWPOINT_CITY`). The original asks both flags although the scene can never come twice; so does ours.
- **The assistant stands in Sandgem Town again** and speaks by how far the Pokédex has come: every species of the Sinnoh Pokédex seen, and no National Pokédex yet, and she wants it shown to the professor.
- **Victory Road's first floor marks its first arrival** (`FLAG_FIRST_ARRIVAL_VICTORY_ROAD`), and its collector has other words for a Champion he still keeps out.
- **The Day-Care couple's fan** is hidden by `FLAG_HIDE_DAY_CARE_GYM_GUIDE`, which a new game sets and both a Pokémon left with the couple and the Hall of Fame clear.
- **A new game hides** the rival in the League's Pokémon Center and the Day-Care couple's fan.

**Our own choices**

- **The game goes straight on at home after the credits.** The original resets the console to its title screen, and CONTINUE then wakes the player in their room from the save just made. Here the picture opens on the player's room as the credits end: the save has been written there, so loading it at any later time comes to the same place.
- **The rival's flag is never cleared.** The original clears it as his scene begins and sets it again as he leaves or as the battle is lost; here the scene shows him and takes him off by itself (a script's own `show` and `hide` hold until the room is come to again), which leaves the same flag in every case.
- **Six of the flags a victory asks about are set by scenes not yet written** (the post-game's: plan 02 · S15). They are asked already, as the original asks them, and a list in the tests (`StoryTests.AskedAheadOfTheirScenes`) names them until then; a flag on that list that something sets fails the test, so the list can't outlive its reason. *All six are set by their scenes since plan 02 · S15, part c, and the list is empty.*
- **The Day-Care couple's fan comes in as the first Pokémon is left**, in the Lady's own script, where the original shows him only as the room is next come to. The room's arrival script does the original's too.
- **The assistant congratulates the Champion** where the original first sends them to Celestic Town's elder (`FLAG_UNLOCKED_DIALGA_PALKIA_SPEAR_PILLAR` unset): the elder's scene is plan 02 · S15's, and until it is written a line that pointed to it would point nowhere.
- **Victory Road's collector still goes with the Hall of Fame alone** (plan 01 · M10, part a), where the original also asks for the National Pokédex: until that Pokédex can be had the back rooms would be shut to a Champion for no reason the game could give. The same holds for the three lines of plan 01 · M11's rooms that ask `FLAG_GAME_COMPLETED` in its place. Plan 02 · S15 moves them to `if nationaldex`.
- **A save from before** (story version 17): `common.ChapterEleven` hides the rival at the League and, unless a Pokémon has been left at the Day Care, the couple's fan. A save that entered the Hall of Fame while its scene ended in a stand-in is given what a victory sets by that scene's own script, run with no screen, with the Vs. Seeker's fourth level; and for it the rival has had his battle before the door, since nobody asks a Champion which of the two is ready for the Elite Four.
- **The ceremony and the credits are our own staging** (style guide, "The Hall of Fame's ceremony" and "The credits"). The credits are this remake's own, written in `Data/credits.json`: the project, its thanks to the original's makers in general words, and the sources `docs/art/CREDITS.md` and `THIRD-PARTY-NOTICES.md` list. Nothing of the original's credit roll, its scenes or its list of names was read or used.
- **The button** moves a card of the ceremony on once it is whole, and held down it hurries the credits' roll four times; the original's ceremony and credits go at their own pace.
- **The balls on the machine** are the machine's own set piece since plan 09 · L12 (`anim halloffame`; "The original's set pieces", below), which the room's pieces draw; until then a variable of our own counted them.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The ceremony's and the credits' songs | Their own (`SEQ_BLD_EV_DENDO2`, the ending theme) | The Champion's victory theme for the ceremony, the title's theme for the credits | Plan 05 · A6 |
| The Hall of Fame's and the League's rooms' songs | Their own | The League's theme | Plan 05 · A6 (plan 01 · M9, part 2d) |
| The machine at work | The healing machine's animation: the balls appear in turn and flash once, without a sound | The balls are set in their sockets in turn, each with a click, and lit on the beat of the heal's fanfare, which plays as the machine's chime | Built by plan 09 · L12 ("The original's set pieces", below); the click and the chime are this scene's own |
| The credits' scenes | The player on a Bicycle through morning, day and night, a stretch of memories, Twinleaf Town | The night hall, with the Champion's team standing beside the roll in turn | Our own; plan 09 · L12 may dress it further |
| Looks | The rival's, Cynthia's and the professor's are ours already; the guard an Ace Trainer | The guard a youngster | Plan 11 · C2 to C4 |
| The rival's words in battle | Spoken during the battle itself | Nothing | Plan 11 · C12 |
| Terastallization at the League's door | (not in Platinum: decision 06 · 2's beat for the later mechanic) | Nothing is shown or handed over | Plan 06 · R23 |
| The Four's and Cynthia's rematch teams | From `FLAG_ARRESTED_CHARON_STARK_MOUNTAIN` on | Their first teams every time | Plan 02 · S15 |
| Lucian in the library, the professor in the Department Store | Brought on by the victory's flags | The flags are cleared; neither is on a map yet | Plan 02 · S15 (Lucian); plan 01 · M11 (the store's basement) |
| The player's birthday | The assistant asks whether it is today | Not asked | The game keeps no birthday |

## The rooms of the north and of the Battle Zone (2026-10-11, plan 01 · M11, part c)

The houses the areas of M8 and M10 still kept shut, with Celestic Town's and Route 222's, on the original's plans, made as part a's and part b's were. The lines are our own, on the beats of each room's script in the decompilation.

- **A gift is given once, by the original's flag**: the Icicle Plate (Route 217's west house, for whoever comes in with HM08 in the bag: the hiker lost it in the snow outside, and lets its finder keep it), the Spell Tag (Route 217's north-east house), a Fresh Water (Route 225's house) and TM42 (the Survival Area's south house). The Pokétch's Analog Watch comes from the black belt of Celestic Town's south-west house.
- **The man in glasses** (Celestic Town's shop) gives a pair by the hour, each once, at the original's hours (`GetHour`): Choice Specs from four to ten in the morning, Black Glasses from ten to eight in the evening, Wise Glasses from eight to four. Those are the bounds of this game's times of day (morning; day and twilight; night and the small hours), so a script asks the time of day and gives what the original gives.
- **Celestic Town's shop** is its north-west house: the town has no Poké Mart, and the old couple behind this counter are its two clerks, she with the common counter by the badges (`PokeMartCommonWithGreeting`) and he with the town's own (`MART_SPECIALTIES_ID_CELESTIC`: Air Mail and the Dusk, Quick and Timer Balls). Both are the common clerk, served across the counter.
- **The woman of the snow** (Route 217's north-east house) sets the flag that hides her as she hands the Spell Tag over (`FLAG_HIDE_ROUTE_217_NORTHEAST_HOUSE_POKEMON_BREEDER_F`). The original's object stays until its map is left; here the script shows her by itself as it sets the flag, which holds until the house is come to again. From then on the house is empty.
- **The size of a Remoraid** (Route 222's east house; `Models/SizeContest.cs`, the original's `overlay005/size_contest.c`). A Pokémon's size is its species' height scaled by a factor of sixteen bits made of the low half of its personality and the low four bits of its IVs (`CalcSizeFactor`), bent by the original's table of sixteen intervals from an even spread into a bell between 29% and 172.5% of the Pokédex's height (`CalcMultFromSizeFactor`; the original's search of the table never lands on its last interval but one, which changes nothing: both lines climb one for one). The fisherman measures a Remoraid of the team against the record (`VAR_SIZE_CONTEST_RECORD`), gives a Net Ball for every one bigger than it, and keeps that one's factor as the new record. A new game of the original sets the record to 33,280 (`InitSizeContestRecord`: a Remoraid of 60.1 cm); here a record never kept reads as that number, so no game and no save needs it set.
- **Poké Kid Janet** (the Pikachu Fan Club, Route 222's west house) has a Pikachu's own look among the five Pikachu and no sight, as the original gives her: she is someone of the room drawn as the Pokémon and battled when spoken to, with Platinum's two Pikachu.
- **The inventor of Sunyshore City** (its east house) gives the Calendar, the Dot Artist and the Roulette, in that order, each for the sight of a Pokémon of the team of the nature he asks for (Serious, Naive, Quirky; `FindPartySlotWithNature`, which passes an Egg by). His first talk only introduces him; what he asks for next is told by the apps the Pokétch has.
- **The Item Expert** (the Fight Area's south house) tells of eight held items. The original lists the eight and EXIT in one window and comes back to the list after each; a question here takes six answers and a menu never leads back to itself (every way through a script is played), so the eight are asked for in two steps and one item is one talk.
- **The Meister** (Route 226's house) trades his Magikarp for a Finneon (`NpcTrades`' "foppa", plan 06 · R12's). On the first talk he also sets `FLAG_ENABLED_POKEDEX_LANGUAGE_DETECTION`, as the original does.
- **A rest** is given by the keeper of the Snowbound Lodge on Route 216 and by its bed, and by the old woman of Route 227's house. The original's old woman asks again at once after a rest, for as long as the answer is yes; here a rest is one talk.
- **The boy of the Resort Area's house** is glad for whoever has seen every Pokémon of Sinnoh. In the original nobody reaches the island without having seen them all; here the ship sails after the Hall of Fame alone (plan 01 · M10's decision), so he asks the Pokédex first (210 seen) and has another line for whoever hasn't.
- **The Ribbon Syndicate's lift** is a room of its own, on the original's plan: the doors in the back wall of each floor lead into the car, and its attendant asks for the floor. The original remembers where its rider came from and aims the car's door there (`SetSpecialLocation`, a dynamic warp); here the car's door is two ways on one tile, one to the ground floor until a flag of our own (`FLAG_RIBBON_SYNDICATE_LIFT_UPSTAIRS`) and one to the floor above from it, as the lakes' ways are (a room's map file may now give a way `until` and `from` too), and the ride sets or clears the flag. Each floor's arrival says the same, so the flag is never wrong for long.
- **The spa** (the Syndicate's upper floor): once a day (`FLAG_DAILY_GOT_SPA_TREATMENT_RESORT_AREA_RIBBON_SYNDICATE_2F`) the first of the team that is no Egg (`GetFirstNonEggInParty`) grows friendlier, by 30 five times in a hundred, by 10 twenty-five and by 5 the rest (`GetRandom` of a hundred), as `IncreasePartyMonFriendship` counts it (half as much again with a Soothe Bell, one more out of a Luxury Ball, never past 255). The attendant leads the guest to the chair of the west bay by the original's own steps from each of the four tiles she is spoken to from, and the three others close in.
- **A pile of books** (`PropType.BookPile`) is what the original's `book01` is where it lies on a blocked tile of the floor: the houses of the inventor, of the Survival Area's black belt and of the old gentleman of Route 228, whose four piles are read.
- **What the team is asked** grew by three words of the `team` command (`docs/scripts.md`): `first`, `nature` and `size`.

| What | The original | Here, for now | Until |
|---|---|---|---|
| The size of a Remoraid as it is told | Tenths of an inch in the English game, and it is those that are compared | Centimetres to the millimetre, which is what `CalcMillimeterSize` works out, and the millimetres are compared | For good: the Pokédex here is metric |
| News of a record Remoraid | A record is reported to Jubilife TV (`sub_0206DC6C`) | Not reported | Plan 08's programmes |
| The Pokédex in other languages | The Meister turns on the Pokédex's entries in other languages (`TurnOnPokedexLanguageDetection`), for the species of the countries he has been to | The flag is set and nothing reads it: the Pokédex has one language | Whichever plan gives the Pokédex entries in other languages |
| The lift's ride | The car's lights scroll (`PlayElevatorAnimation`) | The lift's own set piece since plan 09 · L12, as in the Vista Lighthouse ("The original's set pieces", below) | Built |
| The spa's equipment | A laboratory's machines stand in its two bays (`machine_l01` to `l03`) | Shelves of oils against the wall and two treatment beds on the big machine's tiles, so the floor reads as a spa | For good |
| The posters of the spa's lounge | Two, on the wall between the lounge and the lobby | Left out: an inner wall is cut down to the wainscot | An inner wall that carries pictures |
| The Item Expert's list | One window of nine that is come back to | Two questions, and one item a talk | A script's list of more than six that may loop |
| The old woman of Route 227 | Asks again after every rest | One rest a talk | The same |
| The looks of these rooms' people | Their own object looks (a hiker, a fisherman, a worker, a black belt, a receptionist, a roughneck, Snowpoint's people in their winter clothes…) | Looks of ours by `npcType` (a gentleman, a lady, a lass…) | Plan 11 |
| The three move tutors' houses | Snowpoint City's east house (with the old man who teaches a trendy saying a day), the Survival Area's north house, and Route 228's north house, where the three strongest moves of the starters are taught | Their doors stay shut | Plan 01 · M11, part d, with the tutors of Routes 210 and 212; the rule is plan 12 · Q19's |
| The Battleground, Snowpoint Temple's doors, the Rock Peak Ruins | Opened by the story | Shut, as plans 01 · M10 and 02 · S11 left them | Plan 02 · S15 |
| Two doors of Sunyshore City | Two houses the original's own files call unused | Shut for good | Never |

## After the Hall of Fame, part a: the National Pokédex and the Battle Zone (2026-10-11, plan 02 · S15, part a)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_sandgem_town_pokemon_research_lab.s`, `scripts_pokedex_ratings.s`, `scripts_counterpart_talk.s`, `scripts_route_202.s`, `scripts_sandgem_town_counterpart_house_1f.s`, `scripts_hearthome_city_northwest_house.s`, `scripts_pokemon_mansion_office.s`, `scripts_victory_road_1f.s`, `scripts_victory_road_1f_room_2.s`, `scripts_follower_partners.s`, `scripts_route_221.s`, `scripts_pal_park_lobby.s`, `scripts_eterna_city_south_house.s`, `scripts_iron_island_house.s`, `scripts_snowpoint_city.s`, `scripts_fight_area.s`, `scripts_survival_area.s`, `scripts_route_224.s`, `scripts_route_227.s`, `scripts_route_230.s`, `scripts_canalave_library_1f.s`, `scripts_game_corner.s`, `scripts_init_new_game.s`, and each map's events and `scripts_init_*.s`; `src/unk_0205DFC4.c` for the ratings' steps, `src/pokedex.c` for what a complete Sinnoh Pokédex is)

- **The National Pokédex is given in the professor's lab.** Spoken to with every Pokémon of the Sinnoh Pokédex seen (all 210: the original leaves none out, `sExcludedMonsLocal` is empty), the professor asks to be shown it; Professor Oak walks in from the door, upgrades it (`SetNationalDexEnabled`: our `nationaldex`), says Pal Park is open and leaves, and Professor Rowan gives the Poké Radar. From then on the professor speaks of the world's Pokémon.
- **Until then he rates the Pokédex** by how many of Sinnoh's Pokémon it has seen, once the assistant has shown the player round Sandgem Town: thirteen steps, at 15, 30, 45, 60, 80, 100, 120, 140, 160, 180, 200, all but the last, and all (`Pokedex_GetRatingMessageID_Local`).
- **Who asks for the National Pokédex and not for the Hall of Fame**: the assistant's grandfather, Bebe, Mr. Backlot and his butler's question, the workers before Pal Park, Byron on Iron Island. **Who asks for both**: Victory Road's collector, the assistant's little sister (the swarms) and Snowpoint Temple's guard. **Who asks for the Hall of Fame alone**: the ship's sailor in Snowpoint City, the butler's other words, the League's own rooms.
- **The assistant, once the Pokédex is the National one**, asks in this order whether the player has spoken to her sister (until the sister has told of the swarms), whether they have met Bebe (until her Eevee is given), then offers the Poké Radar's lesson and walks off to Route 202 (`FLAG_STARTED_COUNTERPART_POKE_RADAR_TUTORIAL`, `VAR_ROUTE_202_STATE` 2), where she stands at 186, 817, shows the grass to the east with the camera sent there, tells of the chain and goes back to stand before the lab (state 3). After that she has one of three things to say, by a roll of three.
- **Pal Park opens with the National Pokédex**: Route 221's two workers are gone as the route is come to (`Route221_OnTransition`). In the lobby Professor Oak meets whoever first comes in, puts the Trainer Counter on the Pokétch, makes the record to beat the rival's 2,000 points, says he will be in Eterna City and leaves by the door (`VAR_PAL_PARK_LOBBY_STATE` 1; `FLAG_HIDE_ETERNA_CITY_SOUTH_HOUSE_PROF_OAK` cleared).
- **In Eterna City's south house** he gives the Upgrade once and sets Moltres, Zapdos and Articuno roaming (`ActivateRoamingPokemon`, `FLAG_ACTIVATED_ROAMING_LEGENDARY_BIRDS`); afterwards he sets loose again whichever of them the Hall of Fame has brought back (state 3 made 0), and has a word, once, for whoever has caught all three (`FLAG_CAUGHT_ROAMING_LEGENDARY_BIRDS`).
- **Byron is in Iron Island's cabin** once the National Pokédex is had and until his Metal Coat is given (`IronIslandHouse_OnTransition`); he leaves by the door.
- **The ship sails for a Champion**, whatever their Pokédex (`FLAG_GAME_COMPLETED` alone), to the Fight Area's pier at 623, 434, facing east. The first time, Cynthia comes down from the town to the head of the gangway, behind the player, speaks and goes back up (`FLAG_SAILED_TO_BATTLE_ZONE`).
- **The Fight Area's first scene** begins as the player steps ashore while `VAR_FIGHT_AREA_STATE` is nought. The rival, who stands on the pier, has the player come up to him and takes them to the Battle Frontier's gate (state 1), where Flint and Volkner stand before the doors with five onlookers, and asks whether they will team up. Refused, he waits there and asks again whenever spoken to, bringing the player round to his left hand from whichever side they spoke. Agreed, the two step up and battle the two, the rival with his `rival_fight_area_*` team for the player's starter; a battle lost is a white-out. Won: Volkner turns to Flint, they speak; behind a dark screen they and the onlookers go; the rival says he is off to the Frontier and is cut short by Palmer coming out of the gate, who knocks him two tiles back; Palmer looks about, speaks to both, goes back in; the rival says who that was and runs off south; Buck, who watched from beside the Mart, comes over, asks the player to the Survival Area and leaves (state 2).
- **Two pairs keep the roads out of the town**, an Ace Trainer and a Black Belt across the way north to Route 225 and a fisherman and a rich boy at the water's edge on Route 230, all behind `FLAG_HIDE_FIGHT_AREA_BLOCKADE`. The flag is set when the tag battle is won **and** the National Pokédex is had, by whichever comes second: the battle's end asks for the Pokédex, and Professor Oak's upgrade asks whether `VAR_FIGHT_AREA_STATE` is 2. Neither pair lets anyone out by itself; a test walks the town with each pair alone, with both and with neither.
- **Route 227**: the rival and Crasher Wake stand at the foot of the road from the start, and are met by whoever steps onto either tile below them (`VAR_ROUTE_227_WAKE_RIVAL_STATE`); Crasher Wake walks off down the road and the rival runs after him for the Battle Frontier. Further up Buck steps down to whoever comes, asks them to look round Stark Mountain and asks again until the answer is yes (`VAR_ROUTE_227_BUCK_STATE` 1); he stays on the road until the mountain's first room is come into.
- **The Survival Area's Buck and rival are hidden from the start** and shown by Stark Mountain's last scene (part b). Buck says the Battleground is his place and goes in at its door. The rival battles on Saturdays and Sundays, once a day (`FLAG_DAILY_DEFEATED_SURVIVAL_AREA_RIVAL`), with `rival_survival_area_1_*` and, once the team has entered the Hall of Fame twenty times, `rival_survival_area_2_*`; on other days he has one of three things to say.
- **Marley waits in Victory Road's long back room.** Stepping into the column before her (rows 55 to 60) has her come up, ask to be taken through and follow as `marley_victory_road`; stepping back into the column before the way in sends her back to her place; the row at the room's far end has her thank the player and go on to the way out, which sets `FLAG_TRAVELED_WITH_MARLEY` (the Battle Tower's Battle Salon reads it). Spoken to on the way she has two things to say.
- **Route 224's people come only with an event gift.** Professor Oak stands by the white rock only for a Champion with the National Pokédex who carries Oak's Letter with the Shaymin event on the save, and Marley's walk up to the rock waits for `VAR_ROUTE_224_STATE` 1, which only that gift's `VAR_SHAYMIN_EVENT_STATE` sets. Without the gift the rock is unmarked and nobody is there.
- **Lucian reads on the library's first floor after every victory** (the Hall of Fame clears his flag each time): spoken to, he names one of five books by a roll of five, says he will see the player at the League and goes out by the door.
- **Looker is back at the Game Corner once Charon is caught** at Stark Mountain, with other words (`FLAG_ARRESTED_CHARON_STARK_MOUNTAIN`).
- **A new game hides** Palmer at the Fight Area, Buck and the rival in the Survival Area, Professor Oak and Marley on Route 224, Byron in Iron Island's cabin, and whoever Stark Mountain, the Battleground, Canalave's sailor's house and Rotom's room bring on later, with the Lunar Wing; everyone else of the Battle Zone's story stands there from the start.

**Our own choices**

- **The upgrade waits for the Hall of Fame.** The original's lab asks only that Eterna City has been reached (`FLAG_FIRST_ARRIVAL_ETERNA_CITY`) and that every Pokémon of Sinnoh has been seen, so its National Pokédex can be had before the League. This game's rule is plan 03's decision 5 (`Pokedex.CanUnlockNational`: the Hall of Fame entered, and all 210 seen), decided before the original's script was read; the lab's script asks `FLAG_GAME_COMPLETED` in the place of the original's flag, a test holds the script to `CanUnlockNational` in all four cases, and the professor's rating has a line of its own for whoever has seen all 210 before the League. *To be confirmed*: moving the rule to the original's is one line of the script and one of `CanUnlockNational`.
- **The rival's walk to the gate is behind a fade.** The original walks the two of them thirty-seven tiles across the town to the rival's tune; the town's people wander across that road here, and a walk a script sends the player on gives up at whoever stands in the way. The picture closes on the pier and opens before the gate, as the boy's walk to the Oreburgh Gym does.
- **Someone knocked back turns round.** Palmer's push sends the rival two tiles south still facing north in the original; our walks face the way they go, so he runs back two tiles and turns.
- **The Fight Area's scene begins on the landing itself** (the tile the ship puts the player on), not wherever the town is come to with the state at nought: a game that reached the town another way finds the rival on the pier, and speaking to him there starts the same scene.
- **In Pal Park's lobby the player goes up to Professor Oak**, three steps, where the original's professor comes three steps down to the door: down there the text box would cover the two of them. He walks out past the player by the door all the same.
- **Cynthia stops a tile short on Snowpoint's quay.** The original stands her on the tile at the player's back; from the field's camera the player's own head would hide her there, so she stops one tile further up and is seen as she speaks.
- **Professor Oak's own tile in the lab is its door** (5, 9), as the rival's is at the League (the eleventh chapter): he is on the map only for his scene, which puts him on the tile inside the door, and the tile whoever comes in stands on is nobody's.
- **Professor Oak's welcome in the lobby asks for the National Pokédex too**, which in the game is the same thing (nobody passes the workers without it), so that the Catching Show's own tests and tools, which come into the lobby without it, meet nobody.
- **The assistant comes onto Route 202 as the player does**, already in her place in the grass (the route's arrival clears her flag there), where the original clears it in Sandgem Town as she walks off: on one map of the whole region she would otherwise be seen for a moment where she stood in the first chapter.
- **The rival's weekday words are rolled as he is spoken to**, where the original rolls once as the town is come to (`VAR_RANDOM_SURVIVAL_AREA_RIVAL_MESSAGE`).
- **Buck's door.** He turns to the Battleground's door, it sounds, and he is gone: the door itself is shut until the room behind it is built (part c).
- **A flag set ahead**: `common.ChapterTwelve` sets every flag the original's new game sets for the time after the Hall of Fame, parts b and c's people among them, so that those sessions need no step of their own to hide them from a save in play.
- **A save from before** (story version 18): `common.ChapterTwelve` hides the people above. A save that has already been to the Battle Zone (it can have come only while nothing stood there: its first-arrival flags say so) has the Fight Area's scene and Route 227's first scene counted as played, with everyone they send away gone and the blockades with them, whatever its Pokédex, so that nobody is shut in or out and nobody greets it as newly landed; Buck stays on Route 227.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| Looks | Professor Oak's, Flint's, Volkner's, Palmer's, Buck's, Marley's, Lucian's, Byron's, the sailors' | A gentleman (Oak, Palmer, Lucian, Byron, the sailors), a youngster (Flint, Volkner, Buck), a lass (Marley) | Plan 11 · C5, C6 |
| The rival's walk across the Fight Area | Thirty-seven tiles to his own tune | A fade | Above; plan 09 · L11 may walk it once townspeople give way to a scene |
| The gate's door, the Battleground's door | Open and shut as Palmer and Buck go through | A sound | Plan 01 · M11, part d built the gate house; the Battleground is a room since plan 02 · S15, part c, and Buck still goes in with a sound (a door that opens for someone else: plan 09 · L12) |
| The Fight Area's and the Survival Area's songs, the rival's follow-me tune | `CITY11`, `SEQ_THE_RIV` as he leads | Veilstone's theme; the rival's own theme on the pier | Plan 05 · A6 |
| Professor Oak's rating of the National Pokédex | Twenty-one steps by the number caught, of 493 | A line about Pokédexes | Plan 03 says what this game's National Pokédex asks for; its steps go with that |
| The professors' PC in a Pokémon Center | The same ratings from any PC (`PokedexRatings_CheckPC`), and `FLAG_SHOWN_COMPLETE_LOCAL_DEX`, which spares the professor his question | Not offered; he always asks | Plan 08 (the PC's other entries) |
| The assistant's words for a full National Pokédex | `CheckNationalDexCompleted` | Not said | Nothing can fill it yet (plan 03 · D12, D13) |
| The lab's two scientists | Each with other words once the Pokédex is upgraded | Not in the room | Plan 01 · M11, part d (the lab rebuilt to the original's plan) |
| Route 224: Professor Oak, Marley, Shaymin, the name written on the rock | An event gift (Oak's Letter) | Nobody; the rock is unmarked | The event gifts (plan 08); plan 01 · M10, part b has the Seabreak Path's swap ready |
| Rivals' and Leaders' words in battle | Spoken during the battle | Nothing | Plan 11 · C12 |
| Later mechanics | (not in Platinum) | Flint and Volkner, the rival and Marley use none | Decision 06 · 2: after their own sessions (plan 06 · R20 to R23) |

## The Battle Hall and the Battle Castle (2026-10-11, plan 06 · R18c)

### The Battle Hall

**Platinum's rules, kept as they are** (`src/overlay104/battle_hall.c`, `battle_hall_helpers.c`, `frscrcmd_battle_hall.c`, `src/scrcmd_battle_hall.c`, `src/battle_hall_save.c`, `src/battle_hall_win_records.c`, `scripts_battle_hall.s`, `frontier_scripts_battle_hall.s`, and the board, `src/applications/frontier/battle_hall/main.c`):

- **One Pokémon goes in**, of level 30 or more, no Egg and none of the banned; two of one species for a Double challenge. It battles at its own level, with what it holds, and is whole again for every battle.
- **A round** is ten battles, each against one trainer with one Pokémon (in a Double challenge the same Pokémon twice, their personalities a step of 250,025 apart so that both have the set's nature). Before each the type of the Pokémon to be met is chosen on a board of the seventeen types.
- **A type's rank** goes up with every win over it, from the first to the tenth; a type beaten ten times can't be chosen until every type has been, and then all seventeen stand at their tenth rank again. The ranks last as long as the streak does and are kept in its stats, four bits to a type, two types to a stat, by the types' places on the board.
- **The opponent's level** is the challenger's level less three times its square root, plus the rank (from nought) times the level over five times that root, plus half a level for every other type already won over; rounded up, and never above the challenger's own. The root is the console's in fixed point (twelve bits after the point), and the rest is in single precision. A Double challenge goes by the higher of the two levels.
- **The trainer**: a class is drawn first, a roll of twelve: eight of them the eight classes of the type's rank, four the classes the type itself brings (the Water type's Tubers, Sailor and Fisherman). Then, from a place drawn among all the Frontier's trainers onward, the first of that class who hasn't stood in this round; when nobody is left, another of the rank's classes is drawn. A Double challenge draws two, though only the first stands.
- **The Pokémon** comes from the Hall's own table of 477, in four groups by strength (1 to 154, 155 to 270, 271 to 375, 376 to 477): the first group at a type's first two ranks, the first two at the next three, the second and third at the next three, the third and fourth at the last two. From a place drawn in that range onward, the first of the chosen type that is not the challenger's species and hasn't been met in this round; once the walk has come all the way round, only the one met last is kept out. Its IVs go by the rank (8, 10, 12 … 26), and its trainer thinks at random up to the third rank, with the basic routine up to the seventh and with all three from the eighth.
- **Ten won** earn Battle Points by the round the streak goes on to (1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 6, 6, 8, 8, 10, 10, and 12 from the eighteenth), add seven to the Trainer Card's score, and the streak runs on. A battle lost or a challenge given up ends it, and every type goes back to its first rank.
- **A streak belongs to the species it was begun with.** Entering another species asks first, and ends it. Each species keeps its own longest streak, for Single and for Double challenges.
- **The Hall Matron** is the 50th and the 170th battle of a Single streak, whatever type is chosen: the board announces her and leaves only her place to choose. Her Pokémon is of any type, from the challenger's species' own group the first time and from the strongest group the second, at the challenger's level with 31 in every stat. The win raises the chosen type's rank all the same; the round is worth 20 Battle Points, and the lobby hands over the silver or the gold print (`VAR_BATTLE_HALL_PRINT_STATE`).
- **The record keeper** adds up every species' Single record and hands over Battle Points as the total passes each of 47 marks, from 10 (1 BP) to 100,000 (1,000 BP); several passed at once are paid together (`VAR_BATTLE_HALL_NEXT_TOTAL_RECORD_MILESTONE`).
- **The fan** stands nearer the counter from 11 species with a record and nearer still from 101, and has ten things to say by their number (1, 11, 31, 51, 101, 151, 251, 351 and 475).
- **Well-known visitors** come to the lobby after a round won, three times in ten and once a day: one from a total of 500, one of two from 1,000, one of four from 10,000.

**Quirks kept**

- The walk through the table steps over the last number of its range: that Pokémon is met only when it is the place drawn.
- A Single round's second places are never filled and hold a nought, which the "not in this round" check reads as trainer 0 and Pokémon 0: after a round's first battle trainer 0, a Youngster, is never drawn.
- A species the Hall's table doesn't hold counts as of the strongest group for the Hall Matron's first Pokémon.
- The Battle Points go by the round the streak has just come into.

**Our own choices**

- **The square root's last bit.** The decompilation calls the console kit's `FX_Sqrt`, whose code it doesn't hold. Here it is the whole-number root of the value shifted up 32 more bits, brought back down ten bits and rounded to the nearest, which is what the console's divider is documented to hand over; a level whose root sits within a four-thousandth of a rounding edge could differ by one level from the console (`BattleHallTests.TheSquareRootIsTheConsolesInFixedPoint` holds the form).
- **A guard on the search for a Pokémon.** The original's walk has come round when it is back at the place it began from, which it can't be when that place is the range's last number (it is stepped over): with nothing left to find it would then walk for ever. Here the walk also counts as having come round after four times the range's length, and after eight takes whatever comes next. No table of the original's reaches either.
- **The board is one screen** (`HallBoardScreen`), in the menus' look, for the original's lower-screen grid: the same cells in the same order, the same cursor (the entered Pokémon's two cells as one, the way back up from them by the side they were come to from), the same steps (a yes or a no for a type; the Hall Matron's notice, then only her place). Nobody leaves it without a type, as in the original.
- **A round is one scene.** The original plays the stage as a scene of the Frontier's own over a picture; here the stage is a room laid out from where that scene stands its people, and its arrival script loops through the ten battles.
- **REST and RETIRE are asked once**, and a challenge put down is saved in the lobby and taken up at the counter, by the Pokémon it was entered with in its place.
- **The draws are on the game's own chance** in the original's order; what was drawn is kept (the trainers and the Pokémon by their numbers), so a challenge put down comes back as it was.

### The Battle Castle

**Platinum's rules, kept as they are** (`src/overlay104/battle_castle.c`, `battle_castle_helpers.c`, `frscrcmd_battle_castle.c`, `src/battle_castle_save.c`, the valets' two applications, `src/applications/frontier/battle_castle/self_app.c` and `opponent_app.c`, `scripts_battle_castle.s`, `frontier_scripts_battle_castle.s`):

- **Three Pokémon go in**, of different species, no Egg and none of the banned. What they hold is taken at the door and given back at the end, and whoever is above level 50 battles at 50.
- **A round** is seven battles of three against three (two out at a time in a Double challenge). Its fourteen trainers are drawn as it begins, none twice, from the round's row (one span for the first six, another for the seventh; the eighth round's row for every round after; the remainder is taken by a span's width, so its last trainer is never drawn), and each trainer's three by the draw every facility but the Tower shares.
- **Nothing is restored between battles.** HP, PP and what is held carry on; after a battle won whoever fainted is back with a single HP and every status is gone.
- **Castle Points** are what the challenge is run on. Every round begins with 10 more. A battle won earns, for each of the three still standing, 3, and 3 more at full HP, 2 at half or more, 1 under it, and 1 without a status; 8 for a battle that used 5 PP or fewer between the three, 6 for 10 or fewer, 4 for 15 or fewer; 7 for each of the trainer's Pokémon that was raised five levels; never less than 1. They stay with the streak from round to round.
- **The first counter** (one's own three): HP restored for 10, PP for 8 from its second rank, both for 12 from its third; a berry or an item rented to hold (the first 8 berries at its first two ranks and all 32 at the third; no items at the first rank, 12 at the second, all 27 at the third; one that holds something already is asked about, and what it held is given up). The healing counter's ranks cost 100 and 100, the rental counter's 100 and 150.
- **The second counter** (the trainer to come): a Pokémon identified for 1; made five levels higher for 1, or five lower for 15 (from 55 or 45 the other way takes it back to 50); its stats, ability, nature and item told for 2; its moves for 5, from the counter's second rank, which costs 50 and is its highest. What was bought is forgotten with the trainer.
- **A battle is passed** for 50 Castle Points from the fourth round on, though never the Castle Valet's: it counts for the round and the streak, not as a battle won, and earns nothing.
- **How the trainers think** goes by the round: at random in the first two, with the basic routine in the third and fourth, with all three from the fifth.
- **Seven through** earn Battle Points by the round the streak goes on to (3, 3, 4, 4, 5, 5, and 7 from the seventh), add seven to the Trainer Card's score, and the streak runs on with its Castle Points and its counters' ranks. A battle lost or a challenge given up ends the streak and puts every counter back at its first rank; a game closed in the middle of a challenge loses the streak's Castle Points too.
- **The Castle Valet** is the 21st and the 49th battle of a Single streak, thinking with all three routines whatever the round. Beaten, the round is worth 20 Battle Points and the lobby hands over the silver or the gold print (`VAR_BATTLE_CASTLE_PRINT_STATE`).
- **The records**: the latest streak and its Castle Points, the points spent, and the record streak with the Castle Points of the streak that set it (when a record is only equalled, the more points of the two are kept).

**Quirks kept**

- The first battle after a challenge is taken up again counts as having used no PP: the PP it began with isn't noted on that path.
- The fourteenth trainer of a Single round is worked out as though a solo challenge met fourteen.

**Our own choices**

- **The challenge begins once the save is agreed to**, as the original's does in the scene behind the door: a round's ten Castle Points are handed out as it begins, and a round not entered earns none.
- **A challenge taken up again draws its next trainer afresh.** The original keeps the sets of the trainer just beaten and makes his three once more as it loads (with IVs read from a place it never wrote); nothing reads them again, so they are left out.
- **The valet's room and the throne room are rooms.** The original plays everything past the lobby as a scene over pictures; here they are two rooms laid out from where that scene stands its people (the valet in the passage with the challenger before him; the lady at the head of the throne room, the challenger west of the carpet and the trainer east), and the scene is their arrival scripts. `VAR_BATTLE_CASTLE_SCENE`, a variable of ours, tells the valet's room what the throne room sent the player back with.
- **One screen for both counters** (`CastleScreen`), in the menus' look, on the original's two applications: the same menus, the same prices asked about before each is paid, the same refusals in the same order (the rank, the points, whether there is anything to restore).
- **A Pokémon's summary at a counter** is the rental's one page for one's own; one of the trainer's is a panel of the counter with its six stats, its nature, its ability and what it holds, and not its moves, which are another purchase.
- **How a battle was won is said in four ways** by the Castle Points it earned (40 or more, 25 or more, 15 or more, fewer), in lines of ours.
- **The valet's menu begins with BATTLE**, then one's own Pokémon, the opponent and the pass; the original's begins with one's own Pokémon and has the battle third. A menu backed out of gives its last answer here, and the pass asks before it is paid for.
- **The lobbies' floors are level**, as the Battle Factory's is: the Hall's comes down three flights from its door in the original and the Castle's has galleries four and six tiles up. The tiles are the original's; benches and counters stand where its rails and parapets block, and the Castle's galleries are a block of wall.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The Multi challenges | Two players side by side over a link | Each lobby's second attendant says nobody can be linked up with; their stats and running flags are in the records | Decision 06 · 7; plan 07 |
| A battle kept as a Battle Video | RECORD in the menu between battles | Not offered | Plan 08 · P10 |
| The reporters in the lobbies | Interview whoever has a streak to talk about | Not there | Plan 08's television |
| What each trainer says | The Frontier's own lines before and after a battle | They come on without a word; Argenta and Darach have lines of ours | The original's text is its own |
| The Hall's crowd | A full house drawn in the scene, louder with the streak, lights and confetti | Four spectators, eight from the third round and twelve from the sixth, a wave of note bubbles | Plan 09's effects, plan 05 · A6 |
| The Hall Matron's coming | The lights go down and the stage's own animation | She comes down the room a few steps at a time between the crowd's waves | Plan 09 |
| The Hall's well-known visitors | One person of the map in four looks (the other child of Twinleaf Town, Mom, the professor from Kanto, Olivine's Leader) | Four people of the map, each there by a flag of ours (`FLAG_BATTLE_HALL_VISITOR_0` to `3`), with the original's flag and variable kept beside them | A person's look isn't changed by a variable here |
| The fan | Serena or Winston, by the player's own look | One fan | Plan 11's cast |
| The Castle's upper floor and its lady | Caitlin watches from a balcony, and the valets come and go with trays | She stands at the head of the throne room; one valet walks up with the Castle Points | The rooms are laid out on one level |
| The counters' applications | Two lower-screen applications with their own art | `CastleScreen`, in the menus' look | The interface is our own |
| Argenta, Darach, Caitlin, the attendants, the announcer and the valets | Their own looks | Looks of the cast that stand for them until theirs are made | Plan 11 |
| The stage and the throne room in battle | Each facility's own background and platforms | A Gym's hall and the League's | Plan 04's arenas have none for the Frontier |
| The halls' music and their sounds | Their own | Veilstone City's theme, a sliding door, the item fanfare | Plan 05 · A6 |
| The lobbies' stairs and galleries | Flights of stairs and upper floors | Level floors | A room's shell stands at one level; the original's plates are in the importer's `--room battle_hall` and `--room battle_castle` |

## The gate houses, the tutors, the Department Store, the Pokémon Centers' floors and the Global Terminal (2026-10-11, plan 01 · M11, part d)

The last rooms of Sinnoh that had none: the fifteen gate houses that were walked through, the seven houses of the move tutors and the two move men, Veilstone's Department Store, the floor above and the floor below every Pokémon Center, the Global Terminal and the gallery over Pal Park's lobby, on the original's plans, made as parts a to c's were. The lines are our own, on the beats of each room's script in the decompilation.

- **A gate house is a room.** Fifteen more on the original's five plans (the gate to Route 209 was plan 02 · S7's): seven on that gate's own plan, a road running east and west (Jubilife's and Canalave's to Route 218, Hearthome's to Route 208, Veilstone's to Route 215, Pastoria's to Route 213, Sunyshore's to Route 222, Route 226's to Route 228), three on a road running north and south (Hearthome's to Route 212, Veilstone's to Route 214, the Fight Area's to Route 225), the Cycling Road's two, Hearthome's two to Amity Square, and the hall between the Fight Area and the Battle Frontier. Each is entered by its overlay's `doors` and left by warps of its own; no overlay passes a gate `through` any more, and the builder no longer reads the word.
- **The Old Rod** is given once in the gate between Jubilife City and Route 218 (`FLAG_RECEIVED_OLD_ROD`), to whoever agrees with the fisherman that it is a fine thing; afterwards he offers to tell how it is fished with.
- **The Pokédex's upgrade** in the gate to Canalave City: the assistant's father waits in the middle of the room (the original's trigger, a column five tiles deep, while `VAR_ROUTE_218_GATE_TO_CANALAVE_CITY_STATE` is 0), upgrades the Pokédex to tell males from females and one form from another, and leaves by the east door behind his flag.
- **The television crew** of the gate between Hearthome City and Route 212 (plan 02 · S8's blockade, `FLAG_HIDE_ROUTE_212_BLOCKADE`) keeps its lines. The original stands the two on the mat before the south door; here they stand a row north of it, so that the tile one comes in on from Route 212 is nobody's, and the row before them is a trigger that turns the player back.
- **The Cycling Road's gates** are the only rooms the Bicycle is ridden through (a map file's `bike`). As in the original, a row of each room's lane past the keepers' counters turns back anyone on foot (the rooms' own triggers while `VAR_MAP_LOCAL_0x02` is 0, asking the built-in variable `CYCLING`, the original's `CheckPlayerOnBike`); a rider is waved through and can't get off from there to the road (`FLAG_FORCE_BIKING_IN_GATE`, which `BicycleRules` asks and the next warp clears), the row before it lets a rider coming back off again, and whoever comes in from the road is riding and stays so until that row. The door onto the road is for riders only as well (`cyclistsOnly`). The professor's assistant in the north gate gives the Exp. Share once to whoever has seen thirty-five Pokémon, and is gone the next time; the Battle Girl of the south gate gives the Flag, an Accessory, once.
- **The Battle Frontier's gate**: the first time through, its six attendants turn to the door and the one at the near end of the west counter calls the player over, upgrades the Vs. Recorder and offers to explain (`VAR_BATTLE_FRONTIER_GATE_TO_FIGHT_AREA_STATE`); afterwards she answers the same questions when spoken to, and each of the other five tells of one hall. An answer ends the talk.
- **The three shard tutors** (Route 212's house, Snowpoint City's east house, the Survival Area's north house) teach (`Models/MoveTutors.cs`, the original's `res/pokemon/move_tutors.json` and `overlay005/move_tutor*.c`): thirteen, eight and seventeen moves, each at its price in Red, Blue, Yellow and Green Shards. A Pokémon is chosen, then one of the place's moves that its species' own tutor list has and that it doesn't know (`ShowMoveTutorMoveSelectionMenu`); the Shards are asked for before the move is taught (`CheckCanAffordMove`) and taken once it is learned (`PayShardCost`). A Pokémon that knows four gives one up, and an HM's move can't be the one, as with a TM.
- **Grandma Wilma** (Route 210's house) teaches Draco Meteor, for nothing, to a Dragon type that doesn't know it and whose friendship is 255, by the original's checks in their order: with nobody on the team who is a Dragon type and lacks the move she says so before anyone is chosen (`GetPartyMonType`, `CheckPartyMonHasMove`); then nobody chosen, an Egg, not a Dragon type, the move known already, the friendship short of 255.
- **The old man of Route 228's north house** teaches Blast Burn, Hydro Cannon or Frenzy Plant the same way to one of the twelve starters of Kanto to Sinnoh fully grown, whichever is its own, at friendship 255; his poster lists the twelve.
- **The Move Deleter** (Canalave City's east house) makes a Pokémon forget a move for good, an HM's too, which nothing else can: never an Egg's, and never a Pokémon's last move.
- **Veilstone's Department Store** is six floors and the car of its lift. Its counters sell `marts.json`'s specialties (two on the ground floor, two on the second, two on the third, the basement's Berries). The fourth floor's two clerks sell goods for a Secret Base for money, at the prices of the Underground's own table (`underground.json`'s `money`), sent to the base's PC. The roof's two vending machines sell Fresh Water for 200, Soda Pop for 300 and Lemonade for 350, and one time in sixty-four a second can drops for nothing (`GetRandom 64`). The basement sells Lava Cookies, one for 200 or ten for 2,000, and Poffins at 6,400 each, of two flavours at 32 apiece and a smoothness of 20 (the original's `GivePoffin` with those numbers), to whoever has a Poffin Case with room in it; its fourth counter has sold out. The socialite of the ground floor gives the mask of a starter the player didn't meet, once (Piplup's to whoever chose Turtwig, Turtwig's to whoever chose Chimchar, Chimchar's to whoever chose Piplup); the third clerk of the second floor gives the Pokétch's Counter; the collector on the roof gives a Sticky Barb once; and Professor Rowan stands in the basement once the Hall of Fame has been entered, says his piece once and is gone the next time (plan 02 · S14's flag).
- **The store's lift** is a room of its own, as the Ribbon Syndicate's is: the doors in each floor's back wall lead into the car, and the car's own door is six ways on one tile, each `from` a flag of our own (`FLAG_VEILSTONE_STORE_LIFT_AT_B1F` and `_1F` to `_5F`). Every floor's arrival script says where the car stands (`VAR_VEILSTONE_STORE_LIFT_FLOOR`, a variable of ours) and sets that floor's flag alone; the attendant asks for a floor, says which way the car goes and what the floor sells, and lets the rider out there.
- **Every Pokémon Center has a floor above and a floor below**, on the original's two plans (`pokecenter_2f` and `pokecenter_b1f`, each shared by every town through a header of its own: a map of each to a Center here, eighteen pairs). Their attendants say what their desks are for and that the link is down.
- **The Global Terminal** is three floors joined by warp panels, with its people on the original's tiles. The beauty of the ground floor guesses the player's favourite Pokémon, the first of the team, as the original's does.
- **Pal Park's viewing gallery** is the lobby's upper level in the original, three tiles up, reached by the escalator on (10, 13): three rows before the north windows the length of the room, and a walk two tiles wide down the east side to the escalator's head. Its four spectators stand on the original's tiles, looking out over the park.

| What | The original | Here, for now | Until |
|---|---|---|---|
| The crew in the gate to Route 212 | On the mat before the south door, two abreast | A row north, with a trigger's row before them, so the tile one arrives on is free | For good |
| The Cycling Road's north gate's trigger | Four tiles wide, of which one lies under the keeper's counter | The lane's three tiles | For good: nobody stands on the fourth |
| The Exp. Share's condition | Thirty-five Pokémon seen in all (`GetNationalDexSeenCount`) | The built-in `SEEN`, which is Sinnoh's count: the same number until Pokémon from elsewhere are met | A built-in for the whole Pokédex's count, if a script ever needs one |
| The Pokédex's forms and genders | `TurnOnPokedexFormDetection` lets the Pokédex show them | `FLAG_ENABLED_POKEDEX_FORM_DETECTION` is set and nothing reads it: the Pokédex shows what it shows | Plan 03, if the Pokédex's form page is to wait for it |
| The Vs. Recorder's upgrade | The Frontier's records can be looked at and sent | Words: the records are the facilities' own boards (plan 06 · R18a), and nothing is sent | Plan 13, if at all |
| Amity Square's gates | The receptionist lets in only who walks with one of the cute Pokémon | The receptionists and the people before them speak; the check is the square's own | Plan 06 · R16c |
| The gate to Route 214 | The man's second line, once the Ruin Maniac has dug through, and a television crew for an interview | His first line; no crew | Plan 08 · P19 (Maniac Tunnel, with the count of Unown's forms) and plan 08 · P9 (the interviews) |
| The Move Relearner (Pastoria City's east house) | Teaches a move the species learns by level at or below the Pokémon's own, for a Heart Scale | Says what he does, tells a Heart Scale when he sees one, and takes nothing | Plan 12 · Q19 (`Models/Relearning.cs`) |
| The Name Rater (Eterna's condominiums) | Renames a Pokémon the player first met | Asks whose name he is to judge, finds it a decent one, and has no better to give: his book of names is away | Plan 13 · V4's naming prompt (plan 12 · Q19, which brings nicknames) |
| The old man of Snowpoint's east house | Unlocks a "trendy saying" a day for the phrases players send each other | Offers, and the saying escapes him | With the wireless, if ever (decision 06 · 7) |
| A tutor's list | One window of every move the Pokémon can learn there, with its price beside it | A question whose answers are the moves and their Shards, scrolling past eight rows | For good |
| The store's attendants (2F to 4F) | Recommend one of four at random to a regular customer (`CheckIsDepartmentStoreRegular`, a count of purchases) and the first to anyone else | The first, to everyone: the count isn't kept | Plan 06, with the game records |
| The fourth floor's lists | The shop's own window with every good and its price | Questions of six answers: five furnishings, the six dolls in two | A shop that sells goods |
| A vending machine | Goes on asking until the player stops | A can a talk | For good: every way through a script ends |
| The Poffin seller's list | One window of ten kinds | Three questions | The same |
| The store's lift | The car's door is a dynamic warp back to where the rider came from, the floor is chosen in one window of six, and the lights scroll | Six ways on one tile behind flags; the two lowest floors share an answer; the ride is the lift's own set piece since plan 09 · L12 | The warp and the answers as they were; "The original's set pieces", below |
| Escalators | Moving stairs with handrails of their own (`ESCALATOR`, `ESCALATOR_FLIP_FACE`) and a ride that is played | The side stairs, in steel, taken as stairs are: a warp with its flight beside it | A session that draws moving stairs |
| The store's and the Centers' escalators | Both flights of a floor in one well | A warp with its flight on either side of the well's middle (the store), or where the hand-made ground floors had their benches (the Centers) | With the Centers rebuilt to the original's plan |
| The Centers' ground floors | The original's plan, with the escalators in an alcove of the south wall | Still the hand-made room; the League's north Center, which is on the original's plan, has its flight up on the two tiles the original blocks in the hall's south-west corner and its flight down on the south-east one | A session that rebuilds the Centers and Marts |
| The Centers' upper floors | The Union Room, the Colosseum, the record corner; the Wi-Fi Plaza and the Wi-Fi Club below; doors behind the desks | The desks and their attendants, who say the link is down; no doors | Decision 06 · 7: left out |
| The Global Terminal | The GTS, the rankings, Box Data, Dress-Up Data, Battle Videos | The rooms and their people; the receptionists say the link is down and every machine reads NO SIGNAL | Decision 06 · 7: left out; plan 13 if online trading reuses the building |
| The terminal's globe | A great globe turning in the middle of the ground floor | A statue on its tiles | A session that models it |
| The lady who gives a Backdrop (the terminal's 2F speaks of her) | Gives one for a first trade over the GTS | Gives nothing | With the GTS |
| Pal Park's gallery | The lobby's own upper level, three tiles up, the lobby in view below | A map of its own (`PalParkGallery`), with nothing drawn where the hall lies below | A session that gives a `Lab` room relief |
| The girl in the gallery | Three lines by a result the script never sets itself (whatever the last script left) | The first: she waits for the show | For good |
| The lady in the gallery | An Accessory or a Backdrop for the Game Boy Advance game in the console's slot | Her line with no game in the slot | Never: the hardware |

## After the Hall of Fame, part b: Stark Mountain, Snowpoint Temple and the legendary Pokémon (2026-10-11, plan 02 · S15, part b)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_stark_mountain_outside.s`, `scripts_stark_mountain_room_1.s`, `scripts_stark_mountain_room_2.s`, `scripts_stark_mountain_room_3.s`, `scripts_follower_partners.s`, `scripts_battleground.s` and `scripts_survival_area.s` for what they read of this part, `scripts_fight_area.s`, `scripts_snowpoint_city.s`, `scripts_snowpoint_temple_1f.s`, `scripts_snowpoint_temple_b5f.s`, `scripts_canalave_city.s`, `scripts_canalave_city_sailor_eldritch_house.s`, `scripts_fullmoon_island.s`, `scripts_fullmoon_island_forest.s`, `scripts_newmoon_island_forest.s`, `scripts_route_228.s`, `scripts_mt_coronet_1f_north_room_2.s`, `scripts_iron_island_b3f.s`, `scripts_rock_peak_ruins.s`, `scripts_pokemon_league_*_room.s`, `scripts_pokemon_league_hall_of_fame.s`, `scripts_init_new_game.s`, and each map's events and `scripts_init_*.s`; `src/roaming_pokemon.c` for the roamers' levels):

- **Stark Mountain is Charon's.** Nobody takes the Magma Stone but Charon, and only for a moment: Buck is the player's partner through the mountain, Looker's Croagunk snatches the stone back, and Buck carries it to its place again before the three part outside.
- **The grunts go in ahead.** Two grunts stand at the far corner of the way round the pool of lava. The first step onto the tile where the road reaches the pool (752, 235, while `VAR_STARK_MOUNTAIN_OUTSIDE_STATE` is 0) has them walk to the mouth, doubt aloud that Charon can stand in for Cyrus, and go in (state 1).
- **The first room** (the scene of `VAR_STARK_MOUNTAIN_ROOM_1_STATE` 0, played as the room is come into; the arrival also hides Buck on Route 227): Mars asks after Cyrus and battles (`commander_mars_stark_mountain`: Bronzong 58, Golbat 58, Purugly 60), steps back, Jupiter battles in her place (`commander_jupiter_stark_mountain`: Bronzong 58, Golbat 58, Skuntank 60), and both leave Team Galactic and the room by the way in, the player a step aside; Charon speaks his mind about them and about Cyrus, and goes on up the room with his two grunts for the Magma Stone (state 1). A battle lost is a white-out, and the scene plays again from its start.
- **Buck through the great room** (`VAR_STARK_MOUNTAIN_ROOM_2_FOLLOWER_BUCK_STATE`): the tile where the way in opens out (42, 69) has him come up the passage behind the player the first time, and stand a step further in every time after, and join as `buck_stark_mountain` (a Claydol at level 63): the team is healed after every battle, and the room's trainers, who stand in pairs, are fought two at a time beside him. A step back towards the way out (42, 70) leaves him waiting; the tile before the way into the heart (47, 3) has him say this is it and go in ahead (state 2). Away from the room without having seen him through, the state is 0 again. Spoken to on the way he has four things to say, the last again and again.
- **The mountain's heart** (the scene of `VAR_STARK_MOUNTAIN_ROOM_3_STATE` 0): Charon between two grunts before the stone, Buck inside the way in; the mountain trembles; Buck and the player go up the hall; Charon takes the stone and says what he will do with it; a voice, a Croagunk that snatches the stone and brings it back to the cracked rock in the corner, which is Looker; the grunts run at the words "International Police"; two policemen come in and lead Charon out; Looker takes everyone outside. Nobody battles. It sets `VAR_STARK_MOUNTAIN_OUTSIDE_STATE` 2, the state 1, `FLAG_ARRESTED_CHARON_STARK_MOUNTAIN`, clears `FLAG_HIDE_GAME_CORNER_LOOKER`, `FLAG_HIDE_SURVIVAL_AREA_RIVAL`, `FLAG_HIDE_SURVIVAL_AREA_BUCK`, `FLAG_HIDE_BATTLEGROUND_BUCK` and the two flags of Looker and Buck before the mouth, and puts the player down at 751, 233, facing north.
- **The parting** (the outside's state 2, as the place is come to): Looker has the Magma Stone and the tale of what moving it wakes; Buck takes it back into the mountain and comes out again; he asks the player to his place in the Survival Area and goes; Looker says goodbye and is gone behind a dark screen (state 3).
- **With Charon caught**: the Elite Four and the Champion bring their rematch teams (`elite_four_*_rematch`, `champion_cynthia_rematch`: sixteen levels higher, to Aaron 69, Bertha 71, Flint 73, Lucian 75, Cynthia 78); Buck and the rival stand before the Battleground; Looker is back at the Game Corner; the Fight Area's rival is hidden as that town is come to (`FightArea_HideRival`).
- **Heatran waits on Buck.** It stands at the head of the hall only for a Champion with the National Pokédex, once Buck has been spoken to **inside the Battleground** (`FLAG_TALKED_TO_BATTLEGROUND_BUCK`, which that room's script sets) and while the room's state is 1: the room's own arrival decides each time. It battles at level 50 and may be run from (`StartLegendaryBattle`). Lost: a white-out, and it waits. Otherwise the state is 2 and it is gone; caught, `FLAG_CAUGHT_HEATRAN`; any battle not lost sets `FLAG_UNLOCKED_VS_SEEKER_LVL_5`. `FLAG_BATTLED_STARK_MOUNTAIN_ROOM_3_HEATRAN`, set as it is first spoken to, gives the Survival Area's cameraman his other words. The Hall of Fame puts it back (the flag cleared, the state 1) unless it was caught, and only once Buck has been spoken to.
- **Snowpoint Temple opens for a Champion with the National Pokédex**, the Pokédex asked first. The guard says her piece as ever; then Candice comes up the street and tells her to let this one in (`VAR_SNOWPOINT_CITY_STATE` 1, for good): the trigger before the west door is dead, the guard tells of the temple's age, the man who longs to see inside has other words, and Candice is hidden the next time the town is come to. The same scene plays if the guard is spoken to before the trigger is stepped on. **The guard never moves**: she stands before the east door, which nobody ever walks into.
- **Regigigas** stands at the temple's foot from the start. Read, it is a statue with words on its dais; with Regirock, Regice and Registeel all in the team it wakes (`FLAG_AWAKENED_REGIGIGAS`, for good: from then on it battles whoever speaks to it, with or without the three) and battles at **level 1**, a battle that may be run from. Lost: a white-out, and it waits. Otherwise it is gone; caught, `FLAG_CAUGHT_REGIGIGAS`. The Hall of Fame brings it back unless caught, for whoever has the National Pokédex.
- **The sailor's son** (`VAR_LUNAR_WING_EVENT_STATE`): the first time a Champion with the National Pokédex comes into Sailor Eldritch's house the boy lies in his bed in a nightmare, his mother beside him (1). Looked at, he mutters of something dark that watches him (2, `FLAG_TALKED_TO_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_LITTLE_BOY`). At the quay his father then asks only whether the player will go to Fullmoon Island for him, and with the Lunar Wing in the bag sends them to his son; from the day the boy has been looked at, Fullmoon Island is one of the places he offers. Looked at again by someone who has met Cresselia and carries the Lunar Wing, the boy wakes (3, `FLAG_WOKE_UP_…_LITTLE_BOY`, the wing used up); his father comes in, thanks the player and goes back to his boat. The next visit it is 0 again for good.
- **Fullmoon Island**: the ship lands at 39, 277, facing west. The island's sailor asks before sailing back, and sails at once for whoever carries the Lunar Wing. **Cresselia is not battled there**: spoken to, it cries, flickers and is gone, set roaming at level 50 (`ActivateRoamingPokemon`); the Lunar Wing's ball, hidden from the start of the game, shows where it rested (`FLAG_MET_CRESSELIA`), a tile further off when the player stood south of it, for which Cresselia draws back a step first. A roaming Cresselia knocked out is put back on the island by the Hall of Fame and set loose again by speaking to it, that time with no wing.
- **The roamers** are the original's six at its levels: Mesprit 50, Cresselia 50, Darkrai 40, Moltres, Zapdos and Articuno 60. Mesprit is set loose in its cavern (the tenth chapter), the three birds by Professor Oak in Eterna City (part a), Cresselia here.

**Our own choices**

- **Heatran is written and waits for part c.** Its condition is the original's, and the flag it asks for is the Battleground's to set, a room that part c builds: until then Heatran's room is empty after the scene, as it is in Platinum before Buck has been visited. `StoryTests.AskedAheadOfTheirScenes` keeps `FLAG_TALKED_TO_BATTLEGROUND_BUCK` until that scene exists.
- **The three Regis' rooms stay shut.** The Rock Peak Ruins (Route 228), the Iceberg Ruins (Mt. Coronet's northern hall) and the Iron Ruins (Iron Island's lowest floor) are each two ways on one tile, of which the original keeps one: an empty chamber for everyone, or the one whose statue answers, for whoever travels with a Regigigas "met in a fateful encounter" (an event's gift). Neither chamber is built here and all six ways stay `locked` in their overlays; so the three are had by trade or from another region, and a team that has them wakes Regigigas all the same. What would open them: the two maps each (plan 01), and a way to the gift Regigigas or a quest in its place (plan 03 · D12c).
- **Newmoon Island, the Harbor Inn and Darkrai are left as they are.** The original opens the inn and sails to Newmoon Island only for a Champion who has woken the boy and carries the Member Card of an event. The inn's door stays `locked`, Canalave's sailor never offers the island, and its landing is the one place the walk of the whole game still sets out from on the story's word alone (`WorldWalk.StoryArrivals`). Shaymin's and Arceus's places are the same kind of gift (plan 03 · D12c; plan 08 for the gifts).
- **The Magma Stone and Looker's rock are things of the story's own.** The original draws them as a Strength boulder and a cracked rock that happen to do nothing; laid out as such here they would be pushed and smashed. Its two objects stay held back, and the story stands a `MagmaStone` and a `FalseRock` on their tiles behind their flags: things of the map that are in the way like a person and that no field move clears (style guide, "The Magma Stone").
- **The temple's east door stays a door painted on the wall.** The guard stands before it for good, so nothing could ever walk into it, and a way nobody can come to fails the walk of the whole game.
- **Where the picture would hide someone, they stand a tile aside.** From the field's steep camera whoever stands right behind the player is behind the player's own head, and the text box covers the lowest rows of the picture. So Looker, before the mountain's mouth, stands east of the tile the player is put down on (752, 233; the original's 751, 232 is north of it), and Buck comes back out to his own tile beside them, a step further than the original's; Mars steps back before her beaten line where the original has her speak first; and Candice comes eight tiles up Snowpoint's street where the original's seven leave her under the text box.
- **The camera looks where the scene is.** It looks across the lava while the grunts speak, and at the middle of the mountain's heart from Charon's first word until Looker turns to the two at the end: the original's screen is the size of each scene, ours is wider and shorter, and the stone stood past its top edge.
- **The boy in his bed is a second person of the room.** The original moves its one boy onto the bed's middle tile, which it leaves open; here the bed blocks its tiles, so a boy of the same looks is on that tile (`level`, the bed's height, so that nobody takes him for standing in a wall), on the map only while our own flag `FLAG_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_LITTLE_BOY_IN_BED` is set, and the boy who runs about is hidden by the same flag. The room's arrival sets and clears it from the original's variable. A room without relief draws him on its floor, inside the bed's box: the covers come to his shoulders.
- **Sailor Eldritch's own tile at home is a step inside the door**: the original's is the tile whoever comes in is put down on, and he is on the map only for his scene, which stands him there.
- **A save from before needs nothing.** `common.ChapterTwelve` (story version 18) hid this part's people; everything else goes by flags and variables that are nought until their scenes. A game saved further up Stark Mountain's first or last room while nobody stood there is brought down to the way in as the scene begins.
- **Buck's way off before the mountain** is the road round the lava to its south-west corner, where he is taken off the map: the original walks him the rest of the way round out of sight, through tiles it blocks.
- **The Pokédex's area page**: `special.json` lists Heatran at Stark Mountain, Regigigas at Snowpoint Temple and Cresselia at Fullmoon Island, where each is first met.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| Looks | Buck's, the policemen's, Candice's, the temple guard's, the sailor's and his family's | A youngster (Buck, the boy), gentlemen (the policemen, the sailor on both quays and at home), a lass (Candice), ladies (the guard, the mother) | Plan 11 · C2 to C6 |
| The boy in his bed | The boy's own sprite drawn over the bed | The boy within the bed's box, the covers to his shoulders, with the sleeper's bubble as he is looked at | Plan 11 (people who lie down) |
| The tremor in the mountain's heart, Regigigas shaking as it wakes | An effect of their own (`ScrCmd_29F`, which shakes the camera from side to side for two seconds; `ShakeObject`, eight times) | The picture's shake and a rumble in the mountain; the statue itself shaken eight times, with a rumble, since plan 09 · L12 | "The original's set pieces", below |
| Cresselia's picture before its cry | `DrawPokemonPreview` | Left out, as Mesprit's | Plan 09 · L12 |
| Looker's tune, Team Galactic's, Stark Mountain's and the temple's own themes | `Common_SetLookerBGM`, `D_06`, `D_02` | The places' stand-in themes play on | Plan 05 · A6 |
| The ship's sailing to the island | The ship's own scene (`TakeShipFromCanalave`) | A sound and a fade | Plan 09 · L12 |
| The Commanders', the Four's and Cynthia's words in battle | Spoken during the battle | Nothing | Plan 11 · C12 |
| The Journal's lines for the rematches | `LOCATION_EVENT_BEAT_ELITE_FOUR_MEMBER` with the rematch's trainer | As the first battles: none | Plan 08 |
| Team Galactic in the mountain's light | Their own sprites | Their pale tops take the volcano's light and read as bare skin in the rooms | Plan 09 (the climate's light on pale cloth) |
| Later mechanics | (not in Platinum) | Mars, Jupiter and the rematch teams use none; nothing is shown at Stark Mountain | Decision 06 · 2: after their own sessions (plan 06 · R20 to R23) |

## The Battle Arcade (2026-10-11, plan 06 · R18d)

**Platinum's rules, kept as they are** (`src/overlay104/battle_arcade.c`, `battle_arcade_helpers.c`, `frscrcmd_battle_arcade.c`, `src/scrcmd_battle_arcade.c`, `src/battle_arcade_save.c`, `scripts_battle_arcade.s`, `frontier_scripts_battle_arcade.s`, and the game board, `src/applications/frontier/battle_arcade/main.c`):

- **Three Pokémon go in**, of different species, no Egg and none of the banned. They go in holding nothing, whatever they held, and whoever is above level 50 battles at 50.
- **A round** is seven battles of three against three (two out at a time in a Double challenge). Its fourteen trainers are drawn as it begins, none twice, from the round's row (one span for the first six, another for the seventh; the eighth round's row for every round after; the remainder is taken by a span's width, so its last trainer is never drawn), and each trainer's three by the draw every facility but the Tower shares, with what their sets would have them hold taken away.
- **The three are whole for every battle.** Nothing of a battle carries into the next but what the board gave them to hold, as the battle left it.
- **The game board** is played before every battle: sixteen panels, four to a row, an event on each. As it opens a number is drawn for its events to go by, then the panel its cursor starts on, then for each panel its kind and its event, two draws a panel. The panels count down from three (a first third of a second, then four fifths of a second for each number), the events are shown, and the cursor runs over them a panel at a time until it is stopped or thirty seconds are up. The cursor moves before the stop is looked at, so a stop takes the panel it has just come to. Every panel then shows the event it stopped on, for a second.
- **Which events may come up** goes by the round, a row of seven for each of the thirty-two (the seventh for every round after, and in it every event may come up): poison, paralysis and a burn in the first and third; a berry in the first three and an item from the fourth; HP down and three levels from the second; sleep, ice, the swap and the battle passed from the fifth; sun, rain, a sandstorm and hail in the second and third; fog and a Trick Room in the fourth and sixth; a slower cursor in the first three, a faster and a jumping one in all; a Battle Point from the fourth, and three only from the seventh. Seven of them also go by the battle of the round: the swap and the three that change the cursor never before a round's seventh battle; a Battle Point before its first, third and fifth; three of them, or the battle passed, before its second, fourth and sixth.
- **What kind a panel is** is a roll of a hundred against one of five rows of weights, for the trainer's Pokémon, the challenger's, the battle and the board, or a bonus: 15, 15, 40 and 30 after a battle that scored 21 or more; 35, 20, 30 and 15 from 16; 30, 30, 35 and 5 from 10; 25, 40, 30 and 5 from 5; and 10, 75, 10 and 5 under that, which is also how a challenge's first board is laid out.
- **A battle's score** is by the three the challenger fought it with: 8 with none of them left with a status, 6 with one, 4 with two; 6 with none fainted, 4 with one, 2 with two; and 10 for a battle over in under three turns, 6 in under five, 4 in under seven, 2 in under nine.
- **The events.** HP down: each of a side's three begins a fifth short, rounded down (worked out as the original does: the most times 1.2, cut to a whole number, less the most). Poison, paralysis, a burn: all three of a side, but those a type or an ability keeps from it (Poison and Steel types and Immunity; Ground types and Limber; Fire types and Water Veil). Sleep and ice: one of the three, the first from a place the board's number names whom nothing keeps from it (Insomnia and Vital Spirit; Ice types and Magma Armor), sleep for two to five turns on a draw of its own. A berry or an item: the same one for all three, from one of three lists by the round (the first three rounds, the next three, and after), by the board's number. Three levels for a side. Sun, rain, a sandstorm, hail or fog that lasts the battle, or a Trick Room for its first five turns. The cursor a step faster, or a step slower, from the next board on (eight speeds: a panel every 20, 16, 8, 4, 3 or 2 frames, every frame, and every frame again), or jumping to a panel drawn afresh at every move on the next board only. The teams swapped for the battle. One Battle Point, or three, handed over on the spot. The battle passed. Or nothing.
- **The cursor's speed** begins a round at the fourth step, from the fifth round at the fifth, from the eighth at the sixth.
- **After a battle** both threes are whole again; what the challenger's own three still hold is theirs to keep, also where the trainer battled with them; three levels the board gave the challenger's three come off again.
- **A battle passed** counts for the round and for the streak, not as a battle won.
- **How the trainers think** goes by the round: at random in the first two, with the basic routine in the third and fourth, with all three from the fifth.
- **Seven through** earn Battle Points by the round the streak goes on to (2, 2, 2, 2, 4, 4, 5, and 6 from the eighth), add seven to the Trainer Card's score, and the streak runs on. A battle lost or a challenge given up ends it. A challenge put down between two battles keeps the game it had come to, the cursor's speed and whether it jumps, the round's trainers and what the three hold; only the latest streak is written then, and the record waits for the round's end.
- **The Arcade Star** is the 21st and the 49th battle of a Single streak, thinking with all three routines whatever the round; the board is played before her battle as before any other. Beaten, the round is worth 20 Battle Points and the lobby hands over the silver or the gold print (`VAR_BATTLE_ARCADE_PRINT_STATE`).

**Quirks kept**

- How the last battle went is not kept with a challenge put down: the first board after a rest is laid out as after a battle that scored nothing.
- A swapped battle is scored by the trainer's three, which the challenger fought it with.
- A status given by the board asks only the types and the abilities named above: nothing else keeps a Pokémon from it.

**Our own choices**

- **An event drawn among none.** Before the seventh battle of the first and of the fifth round the board has no event of the battle's kind to give, and the original then takes the remainder of a draw by nought. The console's own division routine hands a division by nought back with the remainder nought (it returns as soon as it sees the divisor), and the walk that follows goes on through the list to the first event of the next kind that has one, which is NO EVENT. That is what happens here; it has not been checked on a console, and `BattleArcadeTests.ThePanelsAreDrawnByHowTheLastBattleWent` holds the form.
- **The board is one screen** (`ArcadeBoardScreen`), in the menus' look, for the original's two: the panels at the middle, the challenger's three at the left and the trainer's at the right with what each holds, the cursor's speed, the battle of the round and the streak along the foot, and a bar that runs down with the cursor's thirty seconds. The confirm button stands for the stop button on the touch screen. A frame of the board is a thirtieth of a second, as the original's.
- **A panel's colour says whom its event falls on**: red for the trainer to come, blue for the challenger, green for both, gold for a bonus, with a key over the board. The signs on the panels and the events' names are our own.
- **The corridor and the game room are rooms.** The original plays everything past the lobby as a scene over pictures; here they are two rooms laid out from where that scene stands its people (the host before the great screen, the challenger west of the floor and the trainer east, the crowd at the foot of the room), and the scene is their arrival scripts. A whole round is one arrival script of the game room, which loops.
- **REST and RETIRE are asked once**, between two games, and a challenge put down is saved in the lobby and taken up at the counter, by the three it was entered with in their places.
- **A challenge taken up again draws its next trainer afresh**, as the Battle Castle's does.
- **The draws are on the game's own chance** in the original's order; a board's draws are made as it comes up, and the sleeper's turns as its event is carried out.
- **The lobby's floor is level**, as the other lobbies' are: the original's comes down three flights from its door and has a lounge sunk below it, reached by ramps. The tiles are the original's; where the floor steps down two inner walls stand, each open at one end as the ramps are.
- **A battle under the board's weather** is handed to the battle as a place's weather is (`BattleConditions.Weather`, the original's `fieldWeather`), so it lasts the battle, and a Trick Room as the place's own too (`BattleConditions.TrickRoom`, the original's `OVERWORLD_WEATHER_TRICK_ROOM`), for five turns. Nothing of the battle's rules was changed for it.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The Multi challenge | Two players side by side over a link, two Pokémon each | The second attendant says nobody can be linked up with; its stats and running flag are in the records | Decision 06 · 7; plan 07 |
| A battle kept as a Battle Video | RECORD in the menu between games | Not offered | Plan 08 · P10 |
| The reporter in the lobby | Interviews whoever has a streak to talk about | Not there | Plan 08's television |
| What each trainer says | The Frontier's own lines before and after a battle | They walk in without a word; Dahlia and the host have lines of ours | The original's text is its own |
| The game board's art | Its own panels, icons, cursor and the two teams' icons over two screens | `ArcadeBoardScreen`, in the menus' look, with signs drawn from the kit's shapes | The interface is our own |
| The stop button | A button on the touch screen | The confirm button | No touch screen |
| The crowd and the lights | A full house drawn in the scene, spotlights, confetti for the Arcade Star | Thirteen spectators standing at the foot of the room, a wave of note bubbles | Plan 09's effects |
| The Arcade Star's coming | She dances in under the lights | She comes up the runner from the door, turning on the spot twice as she goes | Plan 09 |
| The attendants of the game room | Several run up to see to the Pokémon after a battle | One, who runs up and back | The room has one |
| Dahlia, the host, the attendants and the crowd | Their own looks | Looks of the cast that stand for them until theirs are made | Plan 11 |
| The game room in battle | The Arcade's own background and platforms | A Gym's hall | Plan 04's arenas have none for the Frontier |
| The Arcade's music and its sounds | Its own theme, the board's jingles and the cursor's ticks | Veilstone City's theme, the menus' tick for the cursor, a sliding door, the item fanfare | Plan 05 · A6 |
| The lobby's stairs and its sunk lounge | Three flights of stairs and ramps | A level floor with two inner walls | A room's shell stands at one level; the original's plates are in the importer's `--room battle_arcade` |

## The Town Map (2026-10-11, plan 01 · M12)

The map of the region as one set of data and one screen, for the Town Map out of the bag, Fly's choice and a map on a wall. The grid, the blocks and their kinds, the flags a description waits for, the hidden places and the way come are the original's (`res/town_map/town_map_data.json`, `src/applications/town_map`, `src/overworld_map_history.c`); the picture is painted from our own world and every word is ours.

- **The grid** is the overworld's matrix, a cell to a chunk of 32 tiles by 32: thirty by thirty, of which the screen shows rows 5 to 28. The cursor keeps to the original's bounds (columns 1 to 28, rows 6 to 28; `HandleInput`) and starts on the player's cell, or on Twinleaf Town's (3, 27) when nothing says where the player is.
- **Where the player is** (`TownMapContext_Init`): on the overworld, the cell stood in. Anywhere else, the cell last gone in from (the original's exit location: `exitSpot`, which every warp out of the overworld notes), and where a game has none (it began indoors, or a script took the player there) the cell the place is come into from, found by walking back through the warps (`RegionMap.PlacesOf`). Where none of the three says, the screen reads "Somewhere off the map" and shows no pin.
- **What is told of a place** (`PrintLocationDescription`): a route's words from the start, a town's once its `FLAG_FIRST_ARRIVAL_…` is set, a landmark's name and words once its own flag is; a landmark with no flag of its own waits for what its area waits for (the original's `make_town_map_data.py` gives it the area's check).
- **A landmark's flag** is set in the original by the place's own map script as the player first comes there. Here a script of ours sets some; for twelve the map's file names the place whose first arrival does (`arrival`, by area or by map), and the engine sets the flag where it notes a town's first arrival (`GameEngine.NoteArrivalOnTheMap`). The flags are the original's, so a script written later that sets one changes nothing.
- **The hidden places** (`SystemVars_CheckHiddenLocation`): Fullmoon Island, Newmoon Island, the Spring Path, and the Seabreak Path with Flower Paradise are off the map until their variable holds the original's number: no block under the cursor, and nothing of them in the picture (open water, or for the Spring Path the land round it). The picture is painted again when one is revealed.
- **Fly's places** (`TownMap_GetFlyLocationAtPos`, `CanFlyToHoveredLocation`) are the original's twenty, on the cells of their towns. One is open when its flag is set and its area is open in the world. Two are not drawn at all until they are open, as the original leaves them off: the Pokémon Center before Victory Road and Pal Park. **Pal Park is flown to** once its gate has been come to (`FLAG_FIRST_ARRIVAL_POKE_PARK_FRONT_GATE`, which the original's `GetSpawnIdByMapAndCoords` answers with): plan 02 · S2 had left it out of Fly's list, and it is in it now.
- **The way come** (`OverworldMapHistory`): a ring of six cells, each with the way the player faced as they came into it, written as a step crosses into another cell and, with no way faced, as a door, a ship or a flight brings the player onto the overworld; the first step after that writes the cell again with its way, as the original's does. The map marks the five before the newest, each cell once, and steps through them from the oldest, 26 thirtieths of a second to each. It is saved (`SaveData.MapTrail`), and a save from before it has none until the player walks.
- **No roamer and no swarm is marked**, because the original's Town Map marks neither: a roaming Pokémon is shown by the Pokétch's Marking Map (plan 06 · R14b), which keeps its own cells, and a swarm is told by the assistant's sister. The two maps share the grid (a cell to a chunk of the overworld, `RegionMap.CellOf`), not their marks.
- **A wall's map** (`TOWN_MAP_MODE_WALL_MAP`) is the same screen with no way come. Nothing opens it yet: the maps on the walls of the Pokémon Centers are plan 08 · P8's, which calls `GameEngine.OpenTownMap(TownMapMode.Wall)`.

| What | The original | Here, for now | Until |
|---|---|---|---|
| The picture | Drawn art on the lower screen, a block of it lit for each place visited | Painted from the world's own tiles, a texel a tile; a visit changes the place's words and Fly's disc, not the picture | For good: our own art |
| Two screens | The map below, the place's name, picture and words above | One screen: the map on the left, the place on the right | For good |
| The place's picture | A drawing of each town and landmark on the upper screen | A window on the map from near, four units to a tile, round the cursor's cell | Drawings of the places, if a later plan makes them |
| Zooming | The X button or a touch of the lens doubles the map round the cursor | Confirm (the Menu key when choosing where to fly) shows a third of the map, three units to a tile, with the whole map small beside it | For good |
| The cursor | A hand moved by the pad or put down by the stylus | Four corners round a cell, moved by the arrows; no stylus | For good (the stylus is a cursor, as in the Pokétch) |
| Fly's places | Blocks of the map's own art that light up | A disc on the middle of each place, gold once open | For good |
| The way come | Five marks, each lit in turn with an arrow | Five dots, one in turn larger with its arrow | For good |
| Pal Park's flag | Set by the place's own script | Set by the arrival rule on coming into the lobby (`arrival`: `PalParkLobby`), whose scene is plan 06 · R16b's | A script that sets it there, which changes nothing |
| The landmarks' flags | Each set by its place's script | Set by the arrival rule where no script of ours sets them | The same |
| A wall's map | Read from the Pokémon Centers' walls | The mode is there and nobody calls it | Plan 08 · P8 |
| The Pokédex's area page | Its own map, in the Pokédex's colours | The Town Map's picture at 20 units to a chunk, the species' cells lit | For good; another region's Pokédex passes its own region (`ModernUi.RegionMap` asks Sinnoh's) |

## After the Hall of Fame, part c: the Battleground, Rotom, the ruins and the two pillars (2026-10-11, plan 02 · S15, part c)

**Platinum's rules, kept as they are** (`res/field/scripts/scripts_battleground.s` and `src/scrcmd.c`'s `ScrCmd_GetRandomBattlegroundTrainers`, `scripts_survival_area.s`, `scripts_old_chateau_back_middle_west_room.s`, `scripts_celestic_town_north_house.s`, `scripts_celestic_town_cave.s`, `scripts_counterpart_talk.s`, `scripts_mt_coronet_6f.s`, `scripts_spear_pillar_dialga.s`, `scripts_spear_pillar_palkia.s`, `scripts_pokemon_league_hall_of_fame.s` and `scripts_init_new_game.s` of the pinned decompilation, with their events)

- **The Battleground's door** in the Survival Area is shut until Charon has been caught on Stark Mountain (`FLAG_ARRESTED_CHARON_STARK_MOUNTAIN`: the original moves the door's warp away until then). Its plate says "members only" either way.
- **The first time in** (`VAR_BATTLEGROUND_STATE` 0) the old man behind the bar, Buck's grandfather, notices the player and has them come over: a private club for Sinnoh's Gym Leaders and whoever they vouch for (state 1). Spoken to afterwards he tells of Stark Mountain and the Magma Stone, and once Heatran has been met there (`FLAG_BATTLED_STARK_MOUNTAIN_ROOM_3_HEATRAN`) only bids the player welcome.
- **Buck** stands before the bar until he is spoken to: his place is for the toughest trainers, the player is a special case, and he is off to the Battle Tower. He walks out by the door round whoever is in his way (`FLAG_TALKED_TO_BATTLEGROUND_BUCK`, which is what lets Heatran wake; `FLAG_BUCK_LEFT_BATTLEGROUND`, by which the Battle Salon and this room's own fourth place take him; `FLAG_HIDE_BATTLEGROUND_BUCK`).
- **The day's guests** are drawn the first time the room is come into each day (`FLAG_DAILY_SET_BATTLEGROUND_TRAINERS`). The first place is one of the eight Gym Leaders. The second and the third are another Leader each or nobody: a die of nine whose ninth face is nobody, thrown again while it shows a Leader already drawn, and nobody at the eighth throw. The fourth is one of the five who battled beside the player, or nobody (a die of six), and comes only once the story has had the player travel with them (Cheryl: `FLAG_TRAVELED_WITH_CHERYL`; Riley: his Egg taken; Marley: `FLAG_TRAVELED_WITH_MARLEY`; Mira: `FLAG_TRAVELED_WITH_MIRA`; Buck: once he has left for the Tower); drawn before that, their place stays empty for the day (`FLAG_HIDE_BATTLEGROUND_TRAINER_4`). They stand on the original's four tiles: (2, 9) looking east, (6, 8) south, (9, 7) west, (11, 5) north.
- **Each guest asks for a battle**, in one of two sets of words by a coin thrown with the draw (`VAR_RANDOM_BATTLEGROUND_MESSAGE`), and in other words once refused, until the room is left. Agreed to, the battle is their team of Platinum's data (`leader_<name>_rematch`: five Pokémon of levels 61 to 65; `cheryl_battleground` and the other four), and it is counted as it begins, won or lost. Beaten, they are beaten for the day (`FLAG_DAILY_DEFEATED_BATTLEGROUND_<WHO>`) and say something else; Roark and Byron have words about each other while both are in. A battle lost is a white-out.
- **Rotom** is in the television of the Old Chateau's second room off the corridor. By day the set only has an unfriendly air. At night and late at night something stirs in it, and whoever agrees to thump it meets a Rotom of level 20 (`StartWildBattle`: it may be run from), once a day (`FLAG_DAILY_BATTLED_OLD_CHATEAU_ROTOM`, set before the battle) until caught (`FLAG_CAUGHT_OLD_CHATEAU_ROTOM`). Nothing else is asked: not the Hall of Fame, not the National Pokédex.
- **The elder of Celestic Town**, at home, reads to a Champion (`FLAG_GAME_COMPLETED`) from a book she found in the shrine: the bearer of the Adamant Orb calls Dialga to Spear Pillar, the bearer of the Lustrous Orb Palkia (`FLAG_UNLOCKED_DIALGA_PALKIA_SPEAR_PILLAR`, set every time she is asked). **The assistant**, before the lab, passes this on until it is heard, while the Sinnoh Pokédex is not yet seen in full; after it she has the professor's word that Dialga or Palkia may show itself at Spear Pillar, unless one of the two has been battled there since the last victory, when she congratulates.
- **Mt. Coronet's way out onto its summit** is decided as its sixth floor is come to, among four maps of Spear Pillar: the torn pillar while `FLAG_SPEAR_PILLAR_IS_DISTORTED` is set; otherwise, for a game that is out of the Distortion World (`VAR_EXITED_DISTORTION_WORLD_STATE` 2 or more) and has heard the elder, **Dialga's pillar** with the Adamant Orb in the bag or held by one of the team, unless Dialga is caught or `VAR_SPEAR_PILLAR_DIALGA_STATE` is 1; failing that **Palkia's** with the Lustrous Orb by the same rule; failing both, the pillar as it stands. So both orbs lead to Dialga first and to Palkia on the next climb. The orbs lie side by side in a room of Mt. Coronet's fourth floor, where the import put them.
- **Dialga and Palkia** are each behind a rift over the altar of a pillar with nobody on it. Read from below, the rift asks whether the Pokémon is to be challenged; a yes brings its cry and a battle at **level 70**, which may be run from. Whatever its end but a loss the rift is quiet (the state 1); caught, `FLAG_CAUGHT_DIALGA` or `FLAG_CAUGHT_PALKIA`. Every entry into the Hall of Fame puts the state of whichever isn't caught back to 0.
- **Cynthia before the painting**: once Charon is caught, the first look at the ruins' painting brings her in by the door to stand beside the player. She tells how it has always been read and asks three times whether she may go on; a no at any of them and she begs pardon and goes, a yes to all three and she tells her own reading, the line on one of Sinnoh's old Plates, where Giratina belongs, and what she believes. She is heard once (`FLAG_TALKED_TO_CELESTIC_TOWN_CAVE_CYNTHIA`), and again after every entry into the Hall of Fame, which clears the flag.
- **What a new game hides** was checked flag by flag against `scripts_init_new_game.s`: `common.NewGame` and the chapters it calls set every one of its 112 flags but ten, which are the original's own leftovers (its unused Battle Park's two, three dummies), the Battle Tower's two (its rooms lay their people out as each is come to), the Villa's delivery man and book, and the Poffin House's reporter (plan 08). A test holds the list.

**Our own choices**

- **The guests are thirteen people of the room**, each shown by a flag of the room's own (`FLAG_BATTLEGROUND_HAS_<WHO>`), where the original gives four objects the looks of whoever was drawn. The room's arrival clears all thirteen, sets those the day's draw names and puts each on their place. `VAR_BATTLEGROUND_TRAINER_1` to `_4` keep the draw as the original's do, by our own numbers: a Leader 0 to 7 in the original's list's order (Roark, Gardenia, Crasher Wake, Maylene, Fantina, Candice, Byron, Volkner) and nobody 8; the fourth 0 to 4 (Cheryl, Riley, Marley, Buck, Mira) and nobody 5. None of them is a trainer of the map: each is battled with `battle self as "…"`, since a Leader's class brings no eye theme.
- **Refusals and the day's wins are remembered by guest, not by place**: `FLAG_MAP_LOCAL_BATTLEGROUND_DECLINED_<WHO>` where the original has a local flag for each of the four places, and the daily flag alone where it also keeps a local one. A place holds one guest all day, so nothing differs.
- **`VAR_TIMES_BATTLED_AT_BATTLEGROUND`** is our name for the original's game record (`RECORD_TIMES_BATTLED_AT_BATTLEGROUND`), as plan 06 · R16b named it for the Villa's catalogue.
- **The welcome's walk stops a tile short.** The original walks the player to the tile under Buck's own (11, 9), where the player's head hides him from our camera; the walk ends at (10, 9), across the bar from the old man all the same. A game that is in the room without having come by the mat is spoken to where it stands, and one taken up on one of the guests' four places (saved there on a day the place was empty) is moved to the mat before anyone is put down on it.
- **Buck goes out with the door's sound**, on the mat: the original plays the sound of stairs there.
- **The room's look.** The original's furniture is part of the room's own model, which the importer doesn't read: the long counter the old man stands behind (its tiles are the original's `Table` and are spoken across), shelves where the original marks a shop's shelves behind it, a bin, a big table in the middle of the floor, three small ones along the west wall and a plant either side of the mat are laid on the tiles the original blocks. The screen on the back wall is the one prop the original lists. The two windows are ours.
- **Mt. Coronet's four ways out are four warps on one tile**, as the original's are: the torn pillar's and the plain one go by `FLAG_SPEAR_PILLAR_IS_DISTORTED` as before, and Dialga's and Palkia's each by a flag of our own that the floor's arrival script sets and clears (`FLAG_SPEAR_PILLAR_HOLDS_DIALGA`, `FLAG_SPEAR_PILLAR_HOLDS_PALKIA`). A way that is there from a flag takes the tile from one that is only there until another (`Map.GetWarpAt`), which is the one rule the engine learned for it.
- **A legendary battle may be run from**, as Heatran's and the lakes' guardians': the original's `StartLegendaryBattle` forbids nothing. A Dialga or Palkia run from is quiet until the next victory, as one knocked out is.
- **The pillars are maps of the imported world** (matrices 280 and 281, which differ from Spear Pillar's own only by the one rift each): the rift is the same prop the torn pillar shows (`PropType.Rift`), drawn by the model the world's chunk names.
- **The Plate's line and the book's passage are our own words** for what the original quotes, like every other line.
- **The elder reads to a Champion whatever else is true**, as the original's does: nothing asks that the Distortion World was left by Sendoff Spring, because nobody is a Champion otherwise.
- **Two of the game's scripts' words were added**: `setvar VAR_X VAR_Y` copies a variable (or one of the game's own, `RESULT`), as the original's `SetVar` does; and `team holds "Adamant Orb"` asks whether anyone of the team holds an item (`CheckPartyHasHeldItem`).
- **A Rock Climb no longer ends on a tile someone stands on** (plan 02 · S11, part a found it at Acuity Lakefront, where the rival stands at the head of the face): the climb is refused while they are there, as a ramp's jump is.
- **A save from before needs nothing**: no story version. `common.ChapterTwelve` (version 18) had hidden Buck in his room; Cynthia in the ruins is hidden since the first chapter's flags; the guests are shown by flags nothing has set; a save with Charon already caught finds the door open.
- **The Pokédex's area page**: `special.json` lists Rotom at the Old Chateau, Dialga and Palkia at Spear Pillar, and Giratina in Turnback Cave's last room, which the ninth chapter had left off it.

**Left shut, with what would open each**

- **Rotom's room** behind Team Galactic's building in Eterna City, with the five appliances and the professor (`FLAG_HIDE_ROTOMS_ROOM_PROF_ROWAN`, set for every game): the Secret Key of an event opens its wall. Rotom's forms exist and are had by `FormRules`; nothing changes one in play. Plan 03 · D12c for a way to the forms, plan 08 for the gift.
- **The Hall of Origin**: the step before each pillar's way down waits for the Azure Flute of an event (`VAR_HALL_OF_ORIGIN_STATE`). Plan 03 · D12c.
- **Maniac Tunnel**: the Ruin Maniac digs his cave on as more of Unown's forms are seen (ten for the long cave, twenty-six for the tunnel and the ruins' room beyond it), a count the Pokédex doesn't keep yet. Route 214's two further ways and Solaceon Town's stay `locked`; the gate man of Route 214 keeps his first line. Plan 08 · P19, which brings the forms seen, opens the three maps with it (Route 214's three ways on one tile are switched as Mt. Coronet's four are).

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| Looks | Buck's, his grandfather's, Candice's, Byron's, Volkner's, Riley's and Marley's | A youngster (Buck, Volkner), gentlemen (the old man, Byron, who are told apart only by where they stand), lasses (Candice, Marley), a clerk (Riley), as their other scenes have them | Plan 11 · C5, C6 |
| The room's own furniture | The clubhouse's model | The art kit's tables, shelves and plants on the tiles the original blocks | Plan 01 (a model reader, if ever) |
| Rotom in the set, and the thump | The television's flicker and the Pokémon seen in it | The script's words, its cry and the battle's own opening | Plan 09 · L11, L14 |
| Dialga's and Palkia's ways in | The rift's own scene before the battle | The cry, a shake of the picture and the battle's opening | Plan 09 · L14 |
| The legendaries' and the Leaders' battle themes | Their own | The legendary theme the game has; the Gym Leaders' theme | Plan 05 · A6 |
| The guests' words in battle | Spoken during the battle | Nothing | Plan 11 · C12 |
| The Survival Area's song in the room | `SEQ_CITY11_D` | Veilstone's, as outside | Plan 05 · A6 |
| The Journal's lines for the rematches and the legendaries | Written as each is met | None | Plan 08 |
| Hisui | (not in Platinum) | Nothing: the ruins' painting is where plan 23 · Z17 puts the way to its side region | Plan 23 · Z17 |
| Later mechanics | (not in Platinum) | The rematch teams use none | Decision 06 · 2: plan 06 · R20 to R23 |

## Amity Square's walk

Plan 06 · R16c, from the original's `scripts_amity_square.s`, `scrcmd_amity_square.c`, `events_amity_square.json`, the two gate houses' scripts and `system_vars.c`.

**The original's rules, kept**

- **Who may walk.** Sixteen species: Pikachu, Clefairy, Psyduck, Pachirisu, Drifloon, Buneary, Happiny, and the three starters with what they grow into; four more with the National Pokédex: Jigglypuff, Torchic, Skitty, Shroomish. The receptionist asks about each such Pokémon of the team in its order until the player says yes to one; with none she lists the species, and whoever says no to all is told to come back. An Egg is never asked about, but an Egg of one of the species keeps her from listing them, as the original's two checks read a party differently.
- **Nobody is in the square alone.** The tile north of each gate's lane asks whoever steps on it with no Pokémon out, and turns them back a step unless one comes out; the tile south of it says goodbye to whoever steps on it with one. The gate houses' own receptionists only talk, as the original's do. Coming out of its ball rests the whole team (`HealParty`).
- **What it turns up.** Speaking to it with two hundred steps walked since the walk began or the last find, it is holding something: decided ahead, a berry one time in five and an accessory otherwise, the accessory from its family's row of ten by a roll of a hundred (15, 15, 15, 15, 10, 10, 8, 5, 5, 2), the berry from nine (Magost 20, Cornn, Rabuta and Nomel 15 each, Spelon, Pamtre, Watmel, Durin and Belue 7 each). The kind decided is tried first and the other when it can't be put away. Otherwise it has a mood: one of nine at random, and the six starters grown up have nine of their own.
- **The man of the park** gives five of one of the nine berries or one of seven accessories once a day, drawn as the square is come into, and stands in one of five places by a die.
- **The ruins' huts.** Nine huts in two sets: the two by the west lawn's stairs lead only to one another, and the seven of the east lead about among themselves, which is the only way to the Spooky Plate, the woman with her Happiny and four of the man's five places. A step north, west or east of a hut's middle carries the player, in the dark, to the middle of the hut that tile leads to, from where they walk out of its door.

**Our own choices**

- **The step count is kept only on a walk.** The original counts every step of the game up to ten thousand and reads the count only here, after clearing it as a walk begins; here it is counted while a Pokémon is out, which comes to the same.
- **A tall Pokémon keeps two tiles behind.** The field's camera is steep: anything a tile south of the player that stands taller than about nine tenths of a tile is drawn over their feet, and Torterra, at nearly two tiles, hid the player to the shoulders whenever they walked north. A Pokémon drawn taller than 1.3 tiles (`GameEngine.TallStroller`: of the square's species, Torterra and Empoleon) follows two tiles behind (`Follower.Gap`), makes way as any follower does when the player walks back into it, and is spoken to from the tile between. Every other follower is a step behind, as the original's are.
- **The bag never fills**, so a berry found always fits; the original's order (the kind decided, then the other, then nothing) is kept and tested for when it would matter.
- **The man keeps his gift when the Fashion Case has no room for it**, and can be asked again that day; the original hands it over unseen and counts the day as done.
- **The walk ends however the square is left.** The original has only its two gates. Here whatever takes the player to another map with a Pokémon out puts it back in its ball (`GameEngine.LeaveStrollBehind`).
- **A game saved on a walk** is taken up again with the Pokémon out behind the player: the story's own variables are all a save keeps of it, and the square's arrival script leaves a walk under way alone.
- **The place of the team goes with the Pokémon.** The original keeps a place of the team and would speak of another Pokémon if the team were put in another order during a walk; here the place follows the one that is out.
- **The huts are stone huts, and the desks desks.** The world's models there had been taken for standing stones and flower beds; walked into, they are a hut with a doorway in its south face, under whose slab the player is not seen, and a counter round the tile each receptionist stands on (style guide, "Props").
- **Every line is our own**, on the original's beats: the receptionists' six things, the nine moods of each kind, the man's offer.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The Pokémon's own sprite | An overworld sprite drawn for each of the twenty species, with a walk | Its field sprite, baked from its model: two frames, shown in turn with a hop to each tile | Plan 10 · F2's walk cycles |
| Its shadow in the huts' dark, the objects raised and lowered as a hut carries the player | `ScrCmd_32D`, `32E`, `338`, `339` | Nothing: the player and the Pokémon are under the slab and unseen | The hut hides them |
| The huts' sound | `SEQ_SE_DP_GYURU` | The warp panels' `warp` | Plan 05 · A6 |
| The television's Amity Square Watch, and the count of finds it reads | `ScrCmd_27C`, `RECORD_UNK_051`, the reporters in the gates | Not kept | Plan 08's television |
| A follower outside the square | None in Platinum | None: plan 10 · F3 and F4 | The follower is one type already (`Overworld/Follower.cs`) |

## The hand-made rooms of the first chapters (2026-10-11, plan 01 · M11, the last rooms)

The rooms made by hand before the world was imported, laid out again on the original's plans: the ground floors of the seventeen Pokémon Centers and of the thirteen Poké Marts, the Trainers' School, the Pokétch Company, Jubilife TV's lobby and the rival's house. The beats of each person's lines were read from the original's scripts (`scripts_<town>_pokecenter_1f.s`, `scripts_<town>_mart.s`, `scripts_trainers_school.s`, `scripts_poketch_co_1f.s` to `3f`, `scripts_jubilife_tv_1f.s`, `scripts_twinleaf_town_rival_house_1f.s` and `2f`); the lines are our own.

- **One plan serves every Center and one every Mart**, as in the original, where seventeen headers share one land file and twelve another (Pastoria's Mart is the same room with a carving on its counter and two tiles of its corners blocked or open differently: here it is the common plan with the carving read on the counter). Each town's room is a file of its own with its own people.
- **People a chapter placed keep the chapter's lines, scripts and names** and moved to the original's tiles: Maylene stands before the counter in Snowpoint's Center as the original has her, not in the corner the hand-made room had for her. Snowpoint's woman in the Mart keeps the chapter's id (`snowpoint_lady`) for the original's `snowpoint_npc_f`.
- **The attendant before the basement** stands in Sandgem's and Jubilife's Centers alone, as in the original, behind `FLAG_HIDE_POKECENTER_BASEMENT_BLOCKADE`, which the Oreburgh Gym's script sets with the Coal Badge. She stands on the tile the way up from the basement comes out on; nobody can be below while she is there, but for a save made downstairs before she was placed, which comes up beside her.
- **A white-out wakes two tiles before the nurse**, across her counter, as it did: the engine finds the tile from her. At home it is still beside Mom.
- **The escalators are where the original has them**, in alcoves of the south wall, the way up at the west and the way down at the east; the floors above and below were not changed and lead back onto the mats beside them.
- **Celestic's Pokéfan** gives his Great Ball once a day (`FLAG_DAILY_RECEIVED_CELESTIC_TOWN_POKECENTER_1F_GREAT_BALL`) by the first Pokémon's friendship, at the original's thresholds: 150 and more the ball, 50 and more a kind word, under that a word of advice. The original's check of a full bag is not made: the bag here has room.
- **The rival leaves the Trainers' School and his own room by the original's movements**: round the player when spoken to from the south, straight down otherwise; and from his desk round to the stairs once the player has stepped clear of their head. In a script the player's step comes first and his run after, where the original moves both at once.
- **The Pokétch Company has two doors**, each leading back out before its own; its floors are joined by the stairs beside the original's warps.
- **A save made in a room that has been laid out again** wakes on the tile the room is come into by, looking in; so does any save that stands on a tile of a room where nobody can stand. This is a rule of the save's place (`WorldVersion` 3), not of the story: no story version was raised.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The Centers' daily trainers | Two people of each ground floor who are a reporter and a cameraman, an idol, a clown, two interviewers, a Poké Kid or a guitarist by the day of the week, battled once a day after the Vs. Seeker's first rematches | Nobody: their tiles, (6, 6) and (5, 6), are left free | Plan 08 · P9, with the reporters |
| Oreburgh's psychic | Asks the player's favourite kind of Trainer and makes it the look others see in the Union Room | Asks, and changes nothing | Plan 13 (online): decision 06 · 7 |
| The Pokétch Company's four PCs | A menu of the apps the player has, each with its description | A word on a handful of apps each, all twenty-five between them | The language's menus are fixed lists; plan 08 if ever |
| The Pokétch Company's reporter, the Marts' delivery man | A television interview; an event's gift | Not placed | Plan 08 · P9; plan 13 |
| Jubilife TV's upper floors | The Fitting Room, the studios, the records, by the stairs and the lift | A rope across the stairs, a card by the lift, and a guide who says the studios are shut | Plan 08 · P8 (the programmes), plan 13 (the rankings) |
| The Lottery Corner's helper | (none: the hand-made lobby had a gentleman who explained the lottery) | The clerk explains it herself; the original's four others are in | The original's lobby has no such person |
| The healing machine's monitor, the Centers' machine beside the counter | Models of their own | The emblem on the wall; a terminal on the nurse's side of the counter | Plan 01 (a model reader, if ever) |
| The rival's console | A model on the floor before his television | Read on its tile, with nothing drawn | The same |
| Escalators that move | Carry the player | The side stairs in steel, taken as stairs are | Style guide, "Rooms": an escalator |
| Looks | The classes' own, the basement's attendant's, the receptionist's | The classes' looks the table has; a nurse; a lady | Plan 11 · C2 to C4 |

## Trainers who speak in battle (2026-10-12, plan 11 · C12)

Platinum's trainers speak before a battle and after it, never during it; from Black and White on, leaders, rivals and bosses speak at its turning points. This game has them speak while the `TrainerLines` option is on, which it is unless turned off (since R19 every game is played by the modern rules, where "Rules" gave on). The rules mark the moments (`TrainerReacts`, `BattleCore.Speaks.cs`) whatever the option says; the words are ours (`Data/trainer-lines.json`) and the face's alone.

- **The three moments**, each once per trainer and battle: their last Pokémon able to fight comes out in the middle of the battle (a replacement, a switch, a move that brings it in; the opening's send-outs never count, so a trainer of one has no such moment); the first of theirs falls while others can still fight (the faint that leaves them nobody is the battle's end, which has its own words); their last one stands in the red, a fifth of its HP or under (`BattleCore.RedShare`, the HP bar's red).
- **The red is read after each action and after the end of a turn**, not at the hit's own impact as the plan first had it: the reaction goes with the last line the action said, so the trainer speaks once the move has been told whole ("It's super effective!" and a critical hit included). At the hit's impact the face's interlude would have put the line between the move and its effectiveness. It follows that whatever left the last one in the red counts (a hit, poison, recoil), that a Pokémon taken from above the red straight to nothing never stood in it and says nothing, and that one healed out of the red in the same action says nothing either.
- **Only the other side speaks.** A partner beside the player (the rival at Spear Pillar and in the Fight Area) has no moments, and a wild Pokémon none.
- **Who has lines**: the rival's battles against the player, the eight Gym Leaders' first battles, every battle of Mars, Jupiter, Saturn and Cyrus, and the Elite Four's and Cynthia's first battles. Rematches, the Battle Frontier's Brains and the Fight Area's tag battle have none yet; a trainer with nothing for a moment says nothing.

| What | Black and White on | Here | Why |
| --- | --- | --- | --- |
| The trainer on screen | Steps back onto the field as the line is read | The line in the message box under the trainer's name; nobody steps back | Plan 11 · C13 |
| The moments | Some games also a line for the last Pokémon's Mega Evolution or Z-Move | The three above | Those mechanics are not in the game |

## The level-up panel (2026-10-12, plan 08 · P5)

Platinum shows what a level did to a Pokémon's stats (`BattleScript_GetExpTask`, `src/battle/battle_script.c`): the line that tells of the level, then a window with the six stats and what each gained, a press, the six stats as they now stand, a press, and only then the moves the level brings. This game shows the same under both sets of rules, with no option: it is Platinum's own.

- **The order is the original's**: the level's line with its fanfare first (`SEQ_GET_EXP_WAIT_LEVEL_UP_MESSAGE_PRINT` waits for the text to be written, not for a key), the panel over it (`SEQ_GET_EXP_LEVEL_UP_SUMMARY_PRINT_DIFF`, then `_PRINT_TRUE`), and both pages before `SEQ_GET_EXP_CHECK_LEARN_MOVE`. Plan 08's design had the panel before the line; the original's order was kept, and the log's events stand where they stood (`LevelRose`, then the line, then `MoveWanted`).
- **The six stats** stand in the original's order (its `statParams`: the most HP, Attack, Defense, Sp. Atk, Sp. Def, Speed), which is not the order effort is counted in.
- **Before and after are read where the original reads them**: the beaten foe's effort is given first (`BattleScript_CalcEffortValues`, at the start of the task), the old stats are the ones the Pokémon had (effort moves no stat until a level reckons them again), and the new ones count that effort. So a level's gains can hold a point the last foe gave.
- **Either button turns the page**, as the original's A, B, X, Y or a touch do.

| What | Platinum | Here | Why |
| --- | --- | --- | --- |
| Several levels from one gain | A level at a time: the bar fills, the line, the window, the level's moves, and round again | One line and one panel for them all: the gains of every level reached, the stats at the last | The rules give a foe's EXP at once and write one `LevelRose` (plan 06 · R10); the shape of the log was not this session's to change |
| Whose icon | The party's icon beside the window only for a Pokémon that isn't on the field | The icon, the name and the level in the panel's head, for every Pokémon | The panel stands where the player's HP box is, so the name would be nowhere but in the line |
| A Rare Candy from the bag | The party menu's own window, the gains and then the stats, before the level's moves and its evolution | The battle's panel over the team's cards, on the half of the screen the Pokémon's card isn't on | One panel and one painter for both |
| The level's line in the bag | In the party menu's message box | In the prompt under the panel; the moves the level taught into a free place are told by a notice once the panel has been read | The bag tells what an item did by notices at the top of the screen, and a notice and the prompt would have said the same line at once |

## The ways into a battle by place and by whom (2026-10-12, plan 09 · L13)

Which way a battle opens is the original's choice (`EncEffects_GetEffectPair` and `sEncEffectsTable` of `src/enc_effects.c`, `CutInEffects_ForBattle` of `src/overlay005/encounter_effect.c`), ported as `BattleMeeting.Row` and `SceneTransition.ForBattle`, with `IntoBattleTests` naming the original's lines beside each row. What the ways look like is our own (style guide, "Life", "Into battle"): only the order of the choice, each way's beats and its length in frames were read.

- **The quirks kept.** The first opposing trainer's class decides for everyone met (`dto->trainer[1]`). At the Battle Frontier the check for a Brain comes before the one for two a side, and both before Team Galactic's. Team Galactic's ways are theirs in a double battle too. Any other double battle is met the double's way, a Gym Leader's or an Elite Four's as well, which the original's own comment calls a bug that no battle of Platinum's meets; Volkner's in the Fight Area is told apart only for its music. A link battle is met the Frontier's way, and a double one the double's. A Frontier Brain met outside the Frontier would be met the Frontier's way. "Stronger" is a first Pokémon of a higher level than the player's first that can fight, never an equal one.
- **Classes.** The original has a class to each Gym Leader, to each of the Elite Four, to each Commander and to the grunts of either sex; `trainers.json` names them by the class's word (Leader, Elite Four, Commander, Galactic) with the person's name beside it, and `BattleMeeting.ClassRow` reads those words.

| What | Platinum | Here | Why |
| --- | --- | --- | --- |
| The ground of a League room, the Distortion World or a Frontier hall | In no case of `CutInEffects_ForBattle`'s switch, which leaves its number unset | The grass's way, as for every ground but water and a cave | `BattleTerrain.Special` stands for all of them, and a way must be picked |
| The two flashes | Dark for a weaker foe, white for a stronger one, sixteen frames or so | White for everyone, a quarter of a second (plan 04 · G9's) | The ways of plan 04 · G9 were built so, and a battle on grass must open as it did |
| The species with ways of their own | Dialga, Palkia, Uxie, Azelf, Arceus; Shaymin, Giratina, Regigigas, Heatran, Darkrai, Rotom, the three Regis | The wild Pokémon's way by place, for now | Their camera is plan 09 · L14's; their rows go into `BattleMeeting.Row` before its check for two at once, as the original's do |
| The Gym Leaders, the Elite Four, the Champion | A band with their portrait | A band with their portrait, since plan 09 · L15 (the section below); the diamonds that stood in for a Leader's are gone | Plan 09's decision 5 |
| A double battle's four pieces | Four balls fly out from the middle to the edges, then a wipe | Four quarters of the dark slide in from the corners | Plan 09's design: the dark itself is the four pieces |
| A grunt's six shards | Six copies of the team's logo fly in to the middle, turning and shrinking to nothing, then a wipe | Six facets of our own mark fly in from the rim, each with its sixth of the dark behind it, and meet as the mark | The logo is art; decision 6 of plan 09 gives the team a mark of our own (`GalacticMark`) |
| The boss's mark | The logo fades in, stands, breaks into a mosaic as the picture is wiped | Our mark grows out of the picture's middle as the picture dims, and its dark core swallows it | The same; a mosaic is a smear, which the field's art never is |
| The water's waver | The whole picture's rows on a sine twelve or fifteen pixels deep, two or three waves down the screen | The scene effects' ripple, four or six texels deep on its own wave of twenty-four rows | One ripple in the field's composite (plan 09 · L11), not a second |
| The camera | Pulls in or back during most ways | Stands still | The field's upright cards are straightened for one lens and one distance; the ways that are a camera move are L14's |

## The bands of the Gym Leaders, the Elite Four and the Champion (2026-10-12, plan 09 · L15)

Who is met on a band is the original's choice: `EncEffects_TrainerClassEffect` gives each of the eight Gym Leaders, each of the Elite Four and the Champion a row of their own (`src/enc_effects.c` 215-253), and `sEncEffectsTable` gives each row its own way in (rows 75-87, `ENCEFF_CUTIN_LEADER_ROARK` to `ENCEFF_CUTIN_CHAMPION_CYNTHIA`). Here the row is the class's word (`BattleMeeting.ClassRow`: Leader, Elite Four, Champion) and the band's colours the name's (`SceneTransition.Bands`), so a rematch team of the Battleground or of the League is met as its person is, someone named Roark of another class is met as anyone is, and, the order of `EncEffects_GetEffectPair` being kept, a band is lost in a double battle, at the Battle Frontier and in a link battle.

What a band looks like is our own (style guide, "Life", "Into battle", "Bands"). Read of the original were its beats and their lengths in frames, thirty to the second: `EncounterEffect_GymLeader` and `EncounterEffect_EliteFourChampion` (`src/overlay005/encounter_effect_core.c` 2895-3126 and 3213-3593) and the banner's wipe (`ov5_021DED20`, `src/overlay005/encounter_effect.c` 1190-1223). Its banners, mugshots, mark, particles and palettes are art, named in that code only by their numbers in an archive, and none was opened.

- **A Gym Leader's, as the original plays it.** One flash. The banner is wiped in from the right over six frames behind an edge toothed every sixteen lines, and scrolls thirty pixels a frame from then on. Ten frames. Four sprites of the mark at the left, three frames apart, the first three shrinking from twice their size over six frames. The mugshot slides in from beyond the right edge over four frames, blended fourteen sixteenths of the way to black. Ten frames. The screen goes to white over three frames; at the white the mugshot is unblended, the field's brightness is set to minus fourteen of sixteen and the name's window is shown; back from white over three. Twenty-six frames. White over fifteen. A hundred frames or so after the flash.
- **The Elite Four's and the Champion's.** The field is captured as a still. One flash. The player's mugshot (the boy's or the girl's) and theirs slide in from the two sides over six frames, each with a piece of banner behind it, both darkened; the name's window under theirs and the mark at the middle from three frames in, with sparks behind it. The screen to white over three frames, where both are unblended, and back over six. Eight frames. Then the mugshots tremble, thrown two pixels the other way every two frames along the diagonal, for thirty-two frames (nine for the Champion); then they fly apart along it, fast at first, as the screen goes to white over eight.

| What | Platinum | Here | Why |
| --- | --- | --- | --- |
| The flash before a band | One, white, some eight frames | The two of every way, a quarter of a second | One `Flash` for every way in, as plan 09 · L13 left it |
| The banner | A picture to each of the thirteen, scrolling | A band in three colours of ours to each, with speed lines of dashes running in whole units | The banners are art; the colours follow each Gym's type or the room |
| The mark | A sprite, stamped in four times | "VS" in the interface's own type, in the same four beats | The sprite is art |
| The mugshot | A drawing | A bust of the person's own rig with the battle's angry face, rendered once into a picture of its own | The drawings are art; it is the look the battle then stands on its platform |
| The silhouette | The mugshot blended fourteen sixteenths to black | The picture's shape in one flat colour | A bust rendered in three dimensions and darkened shows a grey face over black clothes; a shape reads as a silhouette |
| What the portrait stands on | The banner, each drawn for its mugshot | A card in the band: a slanted panel of the band's light colour behind each portrait (a colour of their own for Volkner and Cynthia, whose hair is that colour) | Our Leaders are dressed in their Gym's colours, which are their band's: a bust laid straight on the band lost its coat in it |
| The name | The trainer's name in the system font, beside the mugshot | The name in 52 Black on a plate, with the class in capitals beside it | The kit's type; the battle names them "Leader Roark" |
| The dimming | The field's brightness to minus fourteen of sixteen; for the League a still of the field blended under the banners | The dark at alpha 200 over the picture round the band, 130 round the League's band until its portraits light | One veil of the transition's own dark |
| The Elite Four's banner | Two pieces, one behind each mugshot | One band that comes in as two halves and meets under the mark | Plan 09's design: one band for both kinds |
| The sparks behind the League's mark | Two particle effects | None | Particle data is art and was not opened; the wait they cause is taken as thirty frames or so |
| The mark's moment in the League's band | Three frames in, as the mugshots slide | Once the halves have met, a tenth of a second later | Ours stands on the band, which is not whole until then |
| The trembling | Along the diagonal, two pixels | Apart and at one another along the band, to 15 units | The portraits' feet stay on the band's foot |
| The Champion's trembling | Nine frames, to the Elite Four's thirty-two | The Elite Four's: one kind (`TransitionKind.EliteBand`) and one length for the five | The plan gives the League one band; her band is three quarters of a second longer than the original's |
| The white | The screen's brightness faded up: every pixel paler at once | A shape that closes (a bar from the band's middle row; a slash between the two portraits), in the original's fifteen and eight frames | Every way's cover is a shape a point is under or not (`Covers`), which the tests hold |
| The battle's first frames | Faded in from white | The same shape opening in reverse, out of the white; no dark frame between | As every way of L13 opens on its own shape |
| Length, with the flash | Some 3.6 s for a Leader, 3.2 s for the Elite Four, 2.5 s for the Champion | 3.45 s, 3.15 s, 3.15 s | Counted from the frames above |

## The legendary's and the mythical's ways in (2026-10-13, plan 09 · L14)

Who is met on them is the original's choice: `EncEffects_WildPokemonEffect` (`src/enc_effects.c` 283-336) gives ten species' rows of their own, by species whatever the form, and `EncEffects_GetEffectPair` returns any of them before its check for two at once (167-175); `sEncEffectsTable` (89-98) gives each row its way. Dialga and Palkia, Uxie and Azelf, and Arceus are met on `ENCEFF_CUTIN_LEGENDARY`; Shaymin, Giratina, the Regis and the four it calls minor legendaries (Regigigas, Heatran, Darkrai, Rotom) on `ENCEFF_CUTIN_MYTHICAL`; Mesprit, Cresselia and the three birds have rows only for their music and are met by place (`ENCEFF_CUTIN_USE_LOCAL`). The Regis and the birds have no row of their own in Pal Park (`mapHeaderID != ZONE_ID_PAL_PARK`), which here is asked of the battle's kind (`BattleKind.PalPark`), since a battle of the park is one.

What the camera does is read from `EncounterEffect_Legendary` and `EncounterEffect_Mythical` (`src/overlay005/encounter_effect_core.c` 2404-2549), the table of sixteen lenses `sLegendaryEncounterCameraParams` (2261-2374) and the interpolation tasks they start (`src/overlay005/encounter_effect.c` 280-366), thirty frames to the second. No picture, palette or sound was opened.

- **The legendary's, as the original plays it.** One flash. The camera's field of view is widened by 0x100 over forty frames; six frames later its distance is pulled in by 2350 units over eight frames along a quadratic whose rate opens at half a unit a frame the other way, with the screen's motion blur on; the screen's brightness then goes to white over sixty frames.
- **The mythical's.** One flash. The camera is set to each of sixteen sets of pitch, turn about the upright and field of view in turn, held for one more frame than its `frameDelay` each (the first is the field camera's own), with motion blur on; then white over ten frames.

| What | Platinum | Here | Why |
| --- | --- | --- | --- |
| The flash | One | The two of every way, a quarter of a second | One `Flash` for every way in, as plan 09 · L13 left it |
| The legendary's rush | The camera's distance, 2350 units nearer | The lens, from 0.85 to 4 on its ratio, along the same curve's share of the way | The field's upright things are straightened for one camera; the rig changes its field of view and nothing else. 4 is the tightest the kit's lens goes |
| The mythical's turns | The camera turned about the point it looks at, its pitch and its yaw | That point stepped as far as the turn would swing a point four tiles off, in whole texels (`JoltReach`), and its field of view as the table has it | As above: a turn would change the pitch the field is straightened for |
| The motion blur | On through both | None | A smear is not pixel art |
| The white | The screen's brightness faded up | A shape that closes: a disc from the middle for the legendary, with a wash of the whole picture to alpha 200 under it; a frame closing in from the edges for the mythical, in the original's sixty and ten frames | Every way's cover is a shape a point is under or not (`Covers`), as the bands' |
| The battle's first frames | Faded in from white | The same shape opening in reverse | As every way of L13 and L15 opens on its own shape |
| The camera afterwards | Restored from the copy kept as the effect began | Let go at the midpoint (`CameraRig.LetGo`), under the white: the lens is held over the rig's own zoom and never written into it | The rig's own state, a script's zoom included, is never touched |
| Pal Park's exception | By the map's header | By `BattleKind.PalPark` | The battle says where it is fought |
| Length, flashes included | Some 3.9 s and 2.2 s after the flash | 4.15 s and 2.483 s | Counted from the frames above, with our flashes |

## Formats of the later games (2026-10-12, plan 14)

The formats Platinum never had are extras of ours. Each is a change to the rules that a battle is handed as it is made, and none is met unless the player goes looking for it: under either ruleset Sinnoh's routes, trainers and facilities keep Platinum's formats (plan 14, decision 1), and the golden battles and teams (`PokemonPlatinumTests/Golden`) hold that a battle with no such flag is fought as it was. Where a later game has a rule of its own for a format, it is taken from Pokémon Showdown at the commit the data importer pins (`Sources.ShowdownCommit`), the project's source for the rules after Generation 4; a section below says what was read and where this game does otherwise.

### Inverse battles (plan 14 · B1)

An inverse battle is X and Y's: the type chart is turned round for the whole battle. The rule is Showdown's Inverse Mod (`data/rulesets.ts`, `inversemod`), which does two things. Its `onEffectiveness` answers for one of the target's types at a time: a type the move's type couldn't touch counts as weak to it, and anything else has its sign turned (twice becomes half, half becomes twice). Its `onNegateImmunity: false` means no *type* stops a move there (`Pokemon.runImmunity`, `sim/pokemon.ts`).

- **One place.** `TypeChart.GetEffectiveness(attack, defender1, defender2, rules, inverse)` turns each type against each round before the two are multiplied, as Showdown sums one type at a time (`runEffectiveness`): Ground against Steel and Flying is nothing in Platinum (2 × 0) and even in an inverse battle (½ × 2). Nothing else knows what "inverse" means.
- **The flag comes with the battle's conditions** (`BattleConditions.Inverse`, as the place's weather and the Battle Arcade's Trick Room do) and is never set on a battle that is running. The rules read it from the field (`FieldState.Inverse`, set as the battle is made), and a battle's record keeps it (`BattleRecord.Inverse`): the same seed and answers under the other chart are another battle, so `BattleCore.Replay` refuses a record of the wrong kind, both ways. A record written before the flag existed reads as a plain battle's.
- **Who asks the chart, and so follows** with no rule of their own: the damage and the lines about it ("It's super effective!", the hit's sound, the move's effect on screen), through `DamageCalculator.Effectiveness` and `Calculate`, which read the target's field; the check that a move does nothing to its target before it is used, so a move of a fixed amount or a one-hit knockout reaches a type that was immune (Seismic Toss on a Ghost); Thunder Wave, the one status move a type can be immune to, which paralyzes a Ground type; the berries against a type and an Expert Belt, which go by whether the hit is super effective; Filter, Solid Rock and Tinted Lens; Anticipation; Wonder Guard, which lets through what is super effective by the battle's chart (a Normal move reaches Shedinja, a Fire move no longer does); Stealth Rock (Showdown asks `runEffectiveness` for it too: Charizard takes a thirty-second); and the move cards' hints (plan 12 · Q10), which are the same `Effectiveness` on the rules' own battlers.
- **An ability's immunity stays; a type's becomes a weakness.** Levitate still keeps Ground moves off, and so does Magnet Rise: Showdown's `isGrounded` leaves out only the Flying type's own immunity when immunities are negated, and still answers for Levitate and Magnet Rise. Volt Absorb, Water Absorb, Flash Fire, Motor Drive and Soundproof ask nothing of the chart and are as they were. A Flying type takes a Ground move for twice.
- **What lifted an immunity has nothing left to lift.** Gravity, Ingrain and an Iron Ball (for a Flying type), Foresight, Odor Sleuth and Scrappy (for a Ghost) and Miracle Eye (for a Dark type) make a move that did nothing hit evenly in Platinum. In an inverse battle the type is weak to the move whether or not any of them is in force, as in Showdown, whose rule answers "weak" for such a type before anything else is asked. Gravity and an Iron Ball still bring down a Pokémon that Levitate or Magnet Rise holds up, and Roost still takes the Flying type away for the turn.
- **What is not the chart is not turned round**: a Poison or Steel type still can't be poisoned, a Rock, Ground or Steel type still walks through a sandstorm, Leech Seed still does nothing to a Grass type, and Spikes and Toxic Spikes still pass under a Flying type (Showdown asks `isGrounded()` plain for them).
- **The trainer AI thinks with the battle's chart.** The original's routines ask the battle's own `BattleSystem_ApplyTypeChart` for a move's matchup and its own damage calculation for a move's strength; here the second is `DamageCalculator.Calculate`, which reads the field, so an AI left on the plain chart would have called a move "immune" and counted its damage as double in the same breath. `AiThinking.TypeChart`, `TrainerAi`'s check of the bench and `BattleSystem_TypeMatchupMultiplier`'s sum are handed the flag: a trainer in an inverse battle keeps away from what is resisted *there*, and Levitate and Magnet Rise still make a Ground move worthless to it.
- **The helps outside a battle** (the Help screen's chart, the Pokétch's Move Tester) show the plain chart: they are no battle's.

| What | The later games | Here | Why |
| --- | --- | --- | --- |
| Conversion 2 | Showdown's move reads its own table of types and never asks the rule, so it would take a type that *resisted* in the plain chart, which is weak to the move in an inverse battle | The battle's chart: a type that resists the move there (against a Normal move there is none, and it fails) | The move's meaning is a type that resists the last hit. One argument (`BattleCore.Conversion2`) turns it back if the games are found to do as Showdown does |
| Freeze-Dry | Its "super effective against Water" is not turned round (the rule's one exception) | Not built: the move has no effect of its own yet | Whoever writes its effect keeps the exception |
| The opening | Nothing is said | A line of our own once everyone is out, before the weather: "Every type matchup is turned upside down!" (`BattleCore.InverseLine`) | The log, a replay and the screen should say what kind of battle it is; the move cards' hints say the rest |
| Who offers one | X and Y: someone in Kalos, once a day, with a prize by how the battle went | A scientist of our own at the Fight Area's east edge (`fight_area.InverseBattle`), there once the tag battle before the Battle Frontier's gate is over, one battle a day (`FLAG_DAILY_FIGHT_AREA_INVERSE_BATTLE`), won or lost | Plan 14, decision 1: the later formats are met after the Hall of Fame. The prizes are not built: he pays his team's prize money |
| His team, and the name the battle gives him | — | A trainer of our own since plan 14 · B14, `scientist_ingram` in `Data/our-trainers.json`: Scientist Ingram, with a Magneton at 55 and a Bronzong at 53, each knowing four moves of its own data, and Platinum's prize for a Scientist of his last level | `trainers.json` is Platinum's table and nothing else (the golden `trainers.txt` holds it), and a team written into the overlay would be drawn from the game's chance as the world loads. In B1 he borrowed `scientist_shaun_rematch_3` and was called Scientist Shaun, the name of a trainer of Route 212; B14's file of trainers of ours gave him his own. The Bronzong (Steel and Psychic, Levitate) is a second Pokémon the chart turns round for: what it feared in Platinum (Fire, Ghost and Dark) it resists there, and what it shrugged off (Grass, Psychic, Poison among them) finds it weak |
| A battle lost to him | — | No white-out: the script goes on (`canlose`) and the day's battle is had. The battle itself still says its usual last lines and takes a loss's money | Every battle a script may lose ends so; the lines after a loss are the battle's, not the script's |

### Hordes (plan 14 · B3)

A horde is X and Y's: one of the player's Pokémon against five wild ones at once. Showdown has no horde format, so the rules are the games' as Showdown's own pieces give them (the spread hit, a wild battle's) and our own where it says so below. B3 is the rules alone; the screen and the grass are B4's.

- **A format, behind the rules.** `BattleFormat.Horde`, handed to a battle as a single or a double is (`BattleSetup.Format`, `CoreSetup.Format`), with `BattleFormats.PlacesOf` saying one place for the player and five for the wild side, and `BattleFormats.FallBack` what a battle asked for becomes. A horde is fought only under rules that have it (`Ruleset.Hordes`: the modern rules), with at least two wild Pokémon, the player alone (a partner along makes it the double it always was), and an ordinary wild battle (not the Great Marsh, Pal Park, a roamer, the catching lesson or a trainer's); anything else falls back to a single against the first, so `battles-platinum.txt` can't move for it. A horde of two to four is fought with as many places as it has.
- **The places** are numbered as the original numbers its four (`Place.Number`, slot × 2, + 1 for the wild side), up to `Place.MaxNumber` (10): the field's places and what a Pokémon took from each place this turn are sized by it. Every place draws a number for the turn's order (a Quick Claw's), the original's four in a single or a double and ten in a horde, so no battle of a single or a double draws one number more than it did.
- **Who chooses.** Each wild Pokémon chooses for itself, as any wild Pokémon does (`TrainerAi`: a move at random, and where it faces several foes a foe drawn for it). The player's Pokémon may aim a move at any of the five.
- **A move that hits every foe hits all five**, and each takes the spread hit (three quarters: `Formulas.BaseDamage`'s `* 3 / 4`, the modern formula's 3072/4096), the same as in a double: nothing is added for five.
- **A ball can't be aimed until one is left** (`WhyNotItem`): "There are two Pokémon out!" with two, and a line of our own with more, "There are too many Pokémon out! The Ball can't be aimed!".
- **EXP comes as each foe faints**, from that foe, as in any wild battle with more than one: nothing is shared out at the end.
- **Running is measured against the fastest wild Pokémon standing** (the first of them where two are as fast), by the same formula; in a single or a wild double it is the first of them, as Platinum has it. The games don't say which foe they measure against: this is the plan's choice.
- **What a wild double battle refuses a horde refuses too**, since others stand on the field: Roar and Whirlwind, Teleport, Dragon Tail and Circle Throw driving a wild Pokémon off, and Emergency Exit's flight all fail or do nothing, and a miss is told of its target ("… avoided the attack!", where a single says the user's attack missed). After You and Quash work, as in a double. Dragon Darts aims both darts at its one target, since which foes stand beside which is reach (plan 14 · B6).
- **What it says.** "A horde of wild Zubat appeared!" as they come out (no names where the species differ: "A horde of wild Pokémon appeared!"), and "Lucas defeated the wild horde!" at the end: lines of our own, where a double names each Pokémon.
- **A record says its format** (`BattleRecord.Format`), and `Replay` refuses one under another: the same seed and answers in a single are another battle. A record that names none (one written before, or a Vs. Recorder file's, whose setup names the format itself) isn't held to one.
- **In the field** a table's slot may say how many come at once (`WildEncounterEntry.Count`, `count` in a file, left out for one): the others are the slot's species, each at a level drawn as the first's was (`WildEncounterRules.Meet`), and the game meets them as a horde (`GameEngine.StartWildBattle`), never beside a partner, on the water, in the Great Marsh's game or in the Bug-Catching Contest. No table of Platinum's has one (plan 14, decision 1; `HordeTests.EveryPlacesTablesMeetOnePokemonAtATime` holds every region's): the grass's hordes are a table of their own beside the place's (below).
- **Where hordes live** (plan 14 · B4; `Data/hordes.json`, `Overworld/Hordes.cs`). Plan 03 · D12's zone for the later generations, where the plan first put them, was dropped on 2026-10-12, so the places are our own choice under decision 1: in each built region the tall grass of a few routes the player has reason to walk after its Hall of Fame (Sinnoh's Battle Zone, Routes 224 to 230; Kanto's Routes 13, 17 and 23; Johto's Routes 26, 44 and 45; Hoenn's Routes 115, 118 and 120), each with one or two hordes. **They come only once that region's Hall of Fame is entered** (its `<Region>HallOfFame` flag) **and under rules that have hordes** (`Ruleset.Hordes`): before it, or under Platinum's rules, no draw is made and nothing moves. A horde's species is one of the region's own Pokédex whose family lives in that place's own grass (Machop where Machoke does), five at a time at levels drawn each from the row's range, a few below the place's weakest (X and Y's hordes are weaker than the grass's single Pokémon, which is the point of their EXP); `HordeScreenTests` holds all of it, and `levels.txt` doesn't list them, since they are no table of the place's.
- **How often.** A step in such grass that meets a Pokémon meets a horde one time in twenty (`Hordes.ChanceInHundred`), the share X and Y are said to give; the draw for it is made only where a horde could come. Sweet Scent there always draws one out, as in X and Y (`Map.DrawOutWild`). No horde comes on the water, in a cave, beside a partner, at a Poké Radar patch or as a roamer. X and Y show no sign of a horde in the grass before it comes, and nor does this game: the way into the battle is the one for two at once, and the line "A horde of wild … appeared!" is its sign.
- **On the screen** (B4; style guide, "Stage", "Places on a platform", and "Battle panels", "A horde's boxes", "The five and the one"): the five stand on an arc across the foe's platform at 0.45 of a single's size, appear together with their cries a third of a second apart (`BattleEngine.HordeCryStep`), and have small boxes stacked down the left; a single-target move asks which of the five on a row of five cards over the player's one; a spread move's shot frames all five. Every look kept for a place (the views, what lasts on each Pokémon, what waits for each place, the effects' places) has five a side (`BattleAnimator.MostSlots`), each side its own count (`SlotsOf`). The Pokédex is told of each of the five as it is met (they are one species but for a mixed horde of a test's making), and the one left to be caught is caught as any.

### SOS calls (plan 14 · B5)

Sun and Moon's SOS battles: a wild Pokémon may call for help, and an ally that answers joins it, so a single becomes one against two. Showdown has no SOS battles, so nothing is read from it; the numbers below are Sun and Moon's as they are commonly given (Bulbapedia's "SOS Battle"), and where a number of theirs isn't known, or the plan asked for something else, the rule is ours and says so.

- **A format, behind the rules.** `BattleFormat.Sos`, handed to a battle as a single, a double and a horde are, with `BattleFormats.PlacesOf` giving the player one place and the wild side two, the second empty as the battle opens. It is fought only under rules that have calls (`Ruleset.SosCalls`: the modern rules), with one wild Pokémon, the player alone and an ordinary wild battle (not the Great Marsh, Pal Park, a roamer, the catching lesson or a trainer's); anything else is the single it began as (`BattleFormats.FallBack`), so `battles-platinum.txt` can't move for it, and neither corpus did (no battle of either asks for one).
- **Where.** A place's wild Pokémon call only where its overlay says `callsForHelp` (`MapArea.CallsForHelp`; or a met `WildEncounterEntry.CallsForHelp`, which is how the harness asks): no area of Sinnoh says so (plan 14, decision 1), and plan 23 · Z6's totems are the first to. Not beside a partner, nor two at once, nor in the Great Marsh's game or the Bug-Catching Contest.
- **When.** *A rule of ours, from Sun and Moon:* the call is the wild Pokémon's action for its turn, made in place of the move it chose, as their callers spend their turn calling; the plan had put it in a step after the end of the turn, where the caller would have moved and called both. It is made when the wild Pokémon's action comes, its side has an empty place (none ever calls into a full one, so there are never more than two), it is free to act (not asleep or frozen, not flinching, not held to a move of several turns, not out of sight) and its species calls at all (`BattleCore.CallsForHelp`).
- **How readily** (`BattleCore.CallChance`, `RollKind.Call` out of 100): the species' call rate (`SosAllies.RateOf`), three times over at half its HP or less and five times at a fifth or less (Sun and Moon's; the plan had "doubled when its HP is low"), twice that when an ally of its fell this turn or the turn before (the plan's), and twice again when the player's Pokémon on the field has Intimidate, Unnerve or Pressure (Sun and Moon's), never past a hundred. **Answered** (`AnswerChance`, a second `RollKind.Call`) at four times the rate: "Foe Pikipek called for help!", then "Wild Pikipek appeared!" or "Its help didn't appear!". The call is the caller's action either way. *Not built:* the chance falling with each call (Sun and Moon's chains run to 255 at the same odds, and the plan names no such fall), and the answer's rise after a call that went unanswered.
- **The call rates** (plan 14, decision 6, its recommended answer: data of our own) are Sun and Moon's scale, 0, 3, 6, 9 or 15, given by a rule of ours (`Data/SosAllies.cs`): none for a legendary or a mythical species, otherwise by how easily the species is caught (a catch rate of 190 or more 15, 120 or more 9, 45 or more 6, below 3), and `Data/sos-allies.json`'s `rates` where the rule's isn't wanted (the Ultra Beasts, 0).
- **Who answers** (`SosAllies.Candidates`): the caller's own species, three times likelier than each of the others; every other species of its line (all that share its Egg) that the ally's level reaches by evolving by level alone, so a Pikipek at 10 calls Pikipeks and one at 14 a Trumbeak too, and a Pikachu never calls a Raichu, which a stone brings; and the file's `pairs` of species of other lines (Cubone and Kangaskhan, Corsola and Mareanie, one way), which plan 23 · Z6 widens with Alola's own. The ally's level is the caller's less nought to two (ours: Sun and Moon draw it from the place's table), it comes in the caller's regional form where its species has that form, and it holds nothing.
- **Made on the battle's own generator** (`RollKind.Ally`): its level, its species, then a number its personality and IVs are drawn from (a `BattleRandom` of its own), so a seed and the answers replay a battle with calls to the same log (`BattleRecord.Format` keeps the format, and a record of an SOS battle is refused as a single's). Its shininess goes by the player's number and the run's odds, as any Pokémon's.
- **The chain** (`FieldState.SosChain`, the calls answered in the battle; a chain of its own beside the Poké Radar's and the DexNav's, and nothing outside the battle keeps it) gives each ally the bonus of its place in it, Sun and Moon's (`Overworld/WildEncounterRules.cs`, `SosChain.Of`): IVs made whole, one from the fifth ally, two from the tenth, three from the twentieth, four from the thirtieth, each a different stat; its hidden ability, 5 in a hundred from the tenth, 10 from the twentieth, 15 from the thirtieth (under the modern rules alone, which are the only ones with calls); and more rolls to be shiny, 4 from the eleventh, 8 from the twenty-first, 12 from the thirty-first, against the odds the run gives (plan 13 · V5's `ShinyDenominator`, Platinum's 1 in 8,192 or the modern 1 in 4,096 otherwise). The chain goes on while either stands: an ally whose caller fell calls in its turn.
- **Two standing**: a ball is refused as in a wild double ("There are two Pokémon out!"), so a Pokémon is caught only once it is alone; each faint gives its own EXP as it falls; what a wild double refuses because others stand (Roar, Whirlwind, Teleport, a drive, Emergency Exit's flight) an SOS battle refuses from its first turn, its second place being there even while empty; running is measured against the first standing, as a wild double's is. The battle is won once both places are down and nobody is calling.
- **The Pokédex** is told of each ally as its line is read, as of any Pokémon met (the line carries its `Seen`), and of an ally caught as of any catch.
- **The Adrenaline Orb** is its hold effect alone (plan 06 · R28a). *Not built:* using it from the bag in an SOS battle to make the wild Pokémon call more readily, as in Sun and Moon, which waits for the item's battle pocket in the data.
- **On the screen** (style guide, "Stage", "An ally answers", and "Battle panels", "An SOS battle's boxes"): the caller stands alone in the middle until the first ally comes, hops where it stands and cries as it calls; the ally hops in from behind its place over 0.7 s as the side opens out to a double's two places; the foes' boxes are a double's, the player's a single's. The log carries `CalledForHelp(Caller, Answered)` on the call's line and `AllyJoined(Place, Pokemon, AsItCame, Chain)` just before the ally's line, whose `Entered` is not out of a ball; the screen takes the ally in as it came (`BattleMirror.Adopt`), since the rules are a turn ahead. Every ally the caller may call has its model made behind the battle's fade (`GameEngine.SosAllyLooks`).

### Triple battles (plan 14 · B6)

Black and White's triple battles, three places a side, with Pokémon Showdown's rules (its `sim/` at the pinned commit isn't in the importer's cache, so what follows is Showdown's as its documentation and data give it, and where a rule is ours it says so).

- **A format, behind the rules.** `BattleFormat.Triple` at the enum's end, three places a side (`BattleFormats.PlacesOf`), fought only under rules that have triples (`Ruleset.Triples`: the modern rules), the player alone with three able to fight, against one trainer's three or three wild Pokémon, in an ordinary battle. Anything else asked for as a triple is asked for as a double, and falls back from there as a double does (`BattleFormats.FallBack`), so `battles-platinum.txt` can't move for it, and neither corpus did (no battle of either asks for one). A record keeps the format (`BattleRecord.Format`) and a triple's is refused as a double's.
- **Where.** A trainer whose `format` is `"triple"` (`Trainer.Format` and both `TrainerRecord`s, `docs/data-files.md`; `DoubleBattle` stays and means a double) asks for one, alone and with no partner beside the player (`GameEngine.StartTrainerBattle`). No trainer of Platinum's table, of ours, of the masters' or of any region's world has one (plan 14, decision 1; `TripleTests`), so nothing in the game is a triple yet: the harness's `formats` mode stages its own (ft30 to ft32), and plan 22's Unova and plan 13 · V12's Versus are where they come.
- **Reach** (`BattleCore.InReach`, the one place it is decided): a place of the other side is in reach when it is across from the user's (slot 2 − the user's) or beside that one, a place of the user's own side when it is beside the user's; so from the centre everyone is, and from an end the far foe and the far ally are not. A move that reaches anywhere (`MoveFlags.Distance`, Showdown's `distance` flag, read by the importer for every move: Aerial Ace, Aura Sphere, Water Pulse, the Flying moves of `any` target, and Aromatherapy, Heal Bell, Perish Song and the like, whose own targets reach everyone anyway) ignores it. In a single, a double, a horde and an SOS battle everything is in reach, so nothing there changed.
- **What reach filters** (`ResolveTargets`): a move aimed at one Pokémon goes to its target only within reach, and a target out of reach is never handed over (`WhyNot` says "X is out of reach!", and the target cards offer only those in reach); where the one aimed at has gone, or Follow Me or Lightning Rod would draw the move, only a Pokémon in reach is taken; a random foe's move (Outrage) and a spread move (Rock Slide on the foes, Earthquake on everyone) take only those in reach, and a move with none in reach fails with "But there was no target...". Helping Hand reaches the ally beside; Coaching and the moves of the whole side, as in Showdown, reach every ally. *Not reach-filtered* (a stand-in of ours): what abilities do to "the foes" (Intimidate, Frisk, Anticipation, the trapping abilities, Bad Dreams) acts on every foe, where Showdown limits Intimidate and the trapping ones to the foes beside.
- **The shift** (plan 14, decision 4, its recommended answer: Black and White's): a Pokémon at an end may spend its turn moving to the centre (`ChoiceKind.Shift`, `BattleChoice.Shift`; refused from the centre and in any other format), trading places with whoever stands there, or moving into it empty, as Ally Switch trades them (`BattleCore.Exchange`: what lasts on each goes with it, and a move still to come aimed at a place finds whoever stands there now). It is made with the turn's switches and items, before any move, in the order of the places and by Speed among shifts (a rule of ours where Showdown's queue order isn't to hand), and said as "X moved to the center!" with a `PlacesSwapped` on the line. X and Y kept the rule; their change was to the automatic move, below, which this game takes as Black and White and Showdown have it.
- **The move to the centre** (`MoveToTheCentre`, at the turn's end once the fainted are replaced): when each side has one Pokémon left able to fight and the two can't reach each other, both move to the centre with "The Pokémon moved to the center!", as Showdown's triples do; a side down to one with others in reach stays where it stands. So no triple can stall out of reach (the fuzz of a hundred holds it).
- **Ally Switch** in a triple trades the two ends, and fails from the centre (Showdown's `allyswitch`).
- **The trainer AI** thinks of a triple as of a double, over every place in its reach while any foe is (`TrainerAi.Doubles`); a wild Pokémon aims at a foe it reaches; the switches and replacements decided for one place of a side are kept from the others; nobody of the AI's ever shifts (Showdown's random player doesn't either), which the move to the centre makes safe. Battle Style Shift's offer is a single battle's alone, as before.
- **On the screen** (style guide, "Stage", "A triple's places", and "Battle panels", "A triple's boxes", "The six cards", "SHIFT"): the foe's three on a horde's arc, the player's three in a row, all at 0.6; small boxes down each side; six target cards with only those in reach lit; SHIFT in place of RUN for a Pokémon at an end. *A decision of this session:* the plan's "three platforms a side" is three places on each side's one platform, as a horde's five are, rather than three platforms of their own; the overview frames both as it is.

### Master Trainers (plan 14 · B14)

Let's Go's Master Trainers: a master for every species, battled one-on-one by one of the player's Pokémon of that species, with the bag closed. Nothing of Let's Go's is read; the masters' moves are Pokémon Showdown's random-battle sets at the pinned commit (MIT), and their names are our own.

- **The masters are data** (`Data/master-trainers.json`, `docs/data-files.md`): `master_<species>` for all 1025, each one Pokémon at level 75 with every IV whole and nothing held, thinking as Platinum's Elite Four do (`Basic`, `EvalAttack`, `Expert`), with no items and no prize money. `TrainerDatabase.Get` finds them beside Platinum's table, and `All` stays Platinum's alone, so the golden teams and battles don't move.
- **Their moves.** The newest generation's set comes first; a set fits when four of its moves (all of them, for a set of fewer) are moves the species can learn and the engine runs in full, and the four are drawn from those on Platinum's generator seeded by the national number. Where no set fits whole the best of them gives what it can and the highest moves its level taught it make up the four (122 masters at the pinned commit); a species no set lists (351, most that still evolve, Bidoof among them) knows what its level taught it, as any trainer's Pokémon with no moves chosen. The plan's rule had every move of the set learnable; Showdown's movepools are five to eight moves of which some are a later game's, so held to that rule two generations' sets fitted a few hundred species fewer. A set written by hand for a species would go in `tools/DataImporter/Overrides/master-trainers.json`, which is not built.
- **What may be learnt** (`Models/Legality.cs`, the check plan 14 · B11 and plan 13 · V13 share): the species' own level-up moves at any level, its Egg moves, Platinum's tutors' lists and the TMs and HMs it takes, and all of the same of whatever it evolved from, since a Pokémon keeps what it knew; Grandma Wilma's Draco Meteor for a Dragon type and a fully grown starter's strongest move. Nothing else: an event's move, a later game's tutor the data doesn't list, a Sketch.
- **One-on-one is the parties.** `choosepokemon "Bidoof"` lets only a Bidoof be chosen (the rest greyed, an Egg too); `master self` fights the master of the chosen Pokémon's species with a `Party` of its own holding that Pokémon, the team's own object (plan 14, decision 9): the EXP and levels it gains, the PP it spends and the hurt and condition it ends with stay with it, as they would in any battle, and the rest of the team takes no part and gains nothing. No master stands in the world (decision 8): plan 14 · B15's hall calls them.
- **The bag is closed to both sides** (`CoreSetup.NoBattleItems`, `BattleTerms.NoBattleItems`): `WhyNotItem` refuses every item to either side with "No items may be used in this battle!", the trainer AI never reaches for its items, and the BAG command is greyed and says so when chosen. Held items are not the bag's and still work; a master's Pokémon holds none.
- **Won, the master is beaten** (`StoryState.Defeat("master_bidoof")`), for good, which is the title plan 14 · B15 shows; no prize is paid. **Lost under `canlose`**, the team is healed on the spot and the master may be battled again, but the battle still says its usual last lines and takes a loss's money, as B1's scientist's does: a battle that only the script may lose has no rule of its own for the money yet.

## The original's set pieces (2026-10-12, plan 09 · L12)

The original's scripts call seven set pieces by name (`src/scrcmd.c`: `PlayPokecenterHealingAnimation`, `PlayPCBootUpAnimation`, `PlayPCShutDownAnimation`, `PlayElevatorAnimation`, `PlayHallOfFameHealingAnimation`, `ShakeObject`, `FlickerObject`); ours call ours (`anim`, `shake`, `flicker`; `docs/scripts.md`, "The original's set pieces"). What was read from the original is its counts and its order: who is counted, how many times, and where in a script the piece stands. The timing, the art and the sounds are ours (style guide, "Rooms", "Machines at work", and "Life", "Set pieces").

- **A ball for each Pokémon that is no Egg** on the nurse's tray (`CountPartyNonEggs`), and **one for each of the team**, an Egg's too, in the Hall of Fame's sockets (`GetPartyCount`).
- **The nurse turns to her machine** (west of her in every Pokémon Center, as in the original) and back before the team is well: the original's order (`CommonScript_NurseHealPokemon`: turn, the animation, turn, `HealParty`), and the same when she looks after someone who came round at her counter after a lost battle.
- **The lifts that have a car** are four of the original's seven: the Vista Lighthouse's, the Ribbon Syndicate's and Veilstone's Department Store's with four rounds of the car's lights, and the Battle Tower's with three. Jubilife TV's lift is roped off until plan 08 · P8 builds its floors, and the lifts of Hearthome's two houses lead straight to their floors (plan 01 · M11, part a); a car for any of them calls `anim elevator` as these do.
- **Regigigas is shaken eight times and Cresselia flickers six** (`ShakeObject … 8`, `FlickerObject … 6`): the two uses plan 09's table names. The original's other scripts were not searched for more; of those read for this session, Team Galactic's control room has neither, and Mesprit's flicker in Verity Cavern is its script's own in the original too (it is taken off the map and put back by its flag three times over), so ours stays the script's.

| What | Platinum | Here | Why |
| --- | --- | --- | --- |
| The healing machine | The balls appear on the tray one every sixteen frames (two columns, row by row from the top left), each with a sound; then they and the machine's screen play one animation while the fanfare sounds; then they are gone | One every 0.35 s on a tray of two rows of three, from the back row's left, each with a sound of our own; lit on the fanfare's beat for the 3.6 s it sounds; then the tray is empty | Our machine's tray is wider than it is deep; our fanfare is our own, and longer |
| The Hall of Fame's machine | The same balls in the same places as the nurse's, no sound, one flash, and gone before the picture closes | The balls in the machine's own six sockets, a click for each, lit to the heal's fanfare, and left in their sockets as the picture closes | The scene of plan 02 · S14 had the clicks and the chime, and a machine that empties itself a frame before the dark reads as a fault |
| A PC's sounds | One as it boots (`SEQ_SE_DP_PC_ON`), another as a part of it is chosen (`PC_LOGIN`), a third as it is switched off | One as its screen comes on in the field and one as it goes off; the storage screen itself is silent | The bank has two PC sounds; they belong to the screen in the room |
| Which screens come on | The Pokémon Centers' PC and the three desks with a laptop (the four models `FieldSystem_LoadPCAnimation` looks for), whichever the room has | A PC that is used (one a `PCTerminal` stands on) is dark until it is switched on; a computer nobody uses keeps its lit screen | A room full of dead screens reads as a power cut; the ones behind a counter are someone's at work |
| A lift's sound | One sound played until the lights' animation has gone round its count, and a chime | A hum of half a second played once to each round, and a chime | The bank plays a sound through; a ride is a whole number of rounds |
| A lift's lights | The car's lights (`elevator_lights`) play the animation of the way it goes, so many times over | The indicator's arrow of the way the car goes, running a texel at a time, once to each round | The car's indicator is plan 02 · S13's, and its arrows say which way |
| A shake | The object moved by the speed and the offsets its script gives (Regigigas: 90, and 3 across) | Three texels to each side by turns, a thirtieth of a second each | One shake for every script; whole texels |
| A flicker | Hidden and shown by turns with the delay its script gives (Cresselia: 8 frames) | A fifth of a second each | The scene of plan 02 · S15, part b had it so |

**Not set pieces of this kind, and whose they are.** Rows above this section name plan 09 · L12 for things the plan never gave it; each is its chapter's own to write with `effect` and the camera kit, or waits for a screen that doesn't exist: the cave painting's breaking (nobody sees it: the control room's button swaps the painting for its shards by their flags, in the original as here), the Vista Lighthouse's binoculars (a look at another map: plan 09 · L4's outcome), Mesprit's and Cresselia's picture before their cry (`DrawPokemonPreview`: a picture over the field, no chapter has one yet), the ship's sailing (`TakeShipFromCanalave`, a scene of its own), a door that opens for someone else (the Battleground's, a lift's in the gallery), and the credits' scenes. Stark Mountain's tremor is the picture's own shake in the original too (`ScrCmd_29F` shakes the camera), so nobody there is shaken.

## The game with every species in it (2026-10-12, plan 03 · D13)

Platinum was made for 493 species and this game has 1,025. Where that number alone changes what Platinum's own rules come to, the choice is here.

| What | Platinum | Here | Why |
| --- | --- | --- | --- |
| The PC's boxes | Eighteen of thirty: 540 places for 493 species, so one of each could be kept | Eighteen until the Pokédex is the National one, a hundred from then on (`PcBoxes.NationalBoxCount`: 3,000 places for 1,025 species) | Eighteen hold barely half of the National Pokédex, and with a full team and full boxes nothing more can be caught. The story is played with Platinum's eighteen: the boxes come with Professor Oak's upgrade (the script's `nationaldex`, and a line of his saying so), after the Hall of Fame |
| A Pokémon met in a form | A form's level-up moves are read with the form (`Pokemon_SetDefaultMoves`), but a trainer's Pokémon is given its form after its moves, so it knows its species' own | `ChangeForm` leaves a Pokémon the moves it had, which is the trainers' way and the way of one that changes form; `Pokemon.InForm` makes one that is met in a form, with the form's own moves | 86 of the 541 forms have a learnset of their own (the regional forms, Wormadam's cloaks, Deoxys's formes). Nothing of Platinum's is made with `InForm`; what brings a form with moves of its own into the game (plan 03 · D12a's sources of forms, and the later regions' plans) is |
| The species in a save | A number | The name, and the National Pokédex's number beside it (`SavedPokemonData.DexNumber`) | A species is found by its name; if a later import of the data spells the name another way (an apostrophe, an accent, a gender sign), it is found by its number, and a Pokémon with no name of its own is called what its species is called then. A save from before the number was kept goes by the name alone |

- **The boxes that come later** stand after the eighteen, empty, named "Box 19" to "Box 100", with the sixteen wallpapers round again. The eighteen keep their names, their wallpapers and everything in them. A save keeps as many boxes as its PC has, and a save that has the National Pokédex from before the boxes grew is given them as it loads. The number is the user's (2026-10-12): the session's own first number was thirty-six, enough for one of each species, and the user asked for a hundred, room for a lot more of them (their forms, more than one of a kind). With so many, the screen shows which box this is as a track and a number where eighteen are a row of dots, and a key kept down at the box's name runs on through the boxes.
- **What the game keeps of the species it has shown** is bounded, where it once grew with every species met: the menu sprites of 240 species (`PokemonSprites.Kept`, eight boxes' worth), those longest unasked for freed first and read from the cache again when next shown, never one asked for in the last two seconds; no model for good but the stand-in's (a model built only to bake a sprite is let go once it is baked, a battle's with the battle); 48 cries.
- **The species whose data began at level 5**: PokeAPI's table gives Crabrawler nothing before level 5 in its newest game, so one hatched at level 1 knew a Tackle it never learns. Its learnset is corrected by hand (`tools/DataImporter/Overrides/species.json`), with Vise Grip at level 1 as in its own game.
- **The levels after the Hall of Fame** are Platinum's own and are now held as text (`PokemonPlatinumTests/Golden/levels.txt`): in the Battle Zone the grass and the caves hold levels 47 to 55 and the trainers 51 to 61, the Champion's team is 58 to 62 the first time and 74 to 78 after Stark Mountain. Whatever this game places of its own in Sinnoh after the Hall of Fame (plan 03 · D12a) is set against those: wild Pokémon between the high forties and the middle fifties, nothing above the Champion's rematch. Pal Park's visitors are; the other versions' exclusives stand in the places Diamond and Pearl have them at those slots' own levels, and the other two starters come at level 5 (the user's choices of 2026-10-17).

## The player's house and Professor Rowan's lab (2026-10-12, plan 01 · M11, the last step)

The last two hand-made places, laid out again on the original's plans (`twinleaf_town_player_house_1f` and `2f`, `sandgem_town_pokemon_research_lab`), with every scene's walks ported from the original's movements (`scripts_twinleaf_town_player_house_1f.s` and `2f`, `scripts_sandgem_town_pokemon_research_lab.s`). The lines are our own.

- **The adventure begins on the original's tile**: 4, 6 of the bedroom, before the television (`sPlayerStartLocation`), with the original's four coordinate events round it. The Hall of Fame's script wakes the Champion there too.
- **The rival's scene in the bedroom goes the original's four ways**, by the tile the first step took the player onto. He calls from the head of the stairs, runs up to the player's east side, is distracted by the computer (he stands on its seat, looking at it), comes back to the player's west side and runs round them out again. His one line about the computer is two, with the run between them.
- **Mom sits on her cushion before the television**, looking up at it, and is spoken to there from any side. Her three scenes are the original's. *The first morning* she comes round the room to the foot of the stairs and goes back, a tile further east than in the original, which leaves the player on the stairs' own tile where this game puts whoever comes down them on the tile beside it. *The Running Shoes*: the player is brought in before the kitchen counter with her at the sink behind it, and follows her round to the cushions, setting off six steps after her as the original's movement has it. *The Journal*, with the rival's mother's visit.
- **The rival's mother's visit goes the original's four ways**, by the side of Mom the player spoke to her from: she comes a step inside the door, then over to the player to hand the parcel over (two steps up, two steps east, or none), and out by the way she came.
- **A white-out at home wakes on the tile east of Mom**, turned to her: in the player's house the cushion beside hers, 8, 8, which is the original's spawn tile (`sSpawnLocations`). It was the tile west of her, which in the hand-made house was the table's.
- **The television downstairs shows the contest of the hour**, as the original's does: the Cute Contest's round-up in the morning, the Smart Contest's by day and at dusk, the Tough Contest's at night.
- **The lab's scenes are on the original's tiles.** The assistant, a step ahead of the player at the door, leads the way up the lab and stands aside at the professor's left hand. Back from the Distortion World the professor and the assistant wait on 7, 11 and 6, 11, three tiles further up the lab than the original's 7, 14 and 6, 14, and the player walks up to them: this game's camera shows a room's front rows under the text box, and on the original's tiles the scene would be read with the player hidden behind it. Professor Oak comes in on 7, 14 and walks up to stand below Professor Rowan; the player, first brought round to that tile from whichever side the professor was spoken to, has stepped aside to the west.
- **The professor is spoken to from every side**: he stands in the open at the head of his hall. The hand-made lab had him behind a desk, spoken to across it.
- **The professor's two assistants** are new people, each with the original's three things to say: one for the days between the professor's news and the player's arrival in Sunyshore City, one once the National Pokédex is the player's, one for every other day. The man is the father of the child who helps the professor, as in the original. The woman's own name is not given.
- **A save made in the bedroom before it was laid out again wakes on the tile the adventure begins on**, whatever tile it stood on: the room a region's game begins in is come into by that tile (`SaveData.PlaceOn`). Beside the head of the stairs, where the rule of the other rooms would put it, a game saved before its first step could walk downstairs round the rival's scene and leave its four tiles armed for good. The house's ground floor and the lab follow the one rule, the mat inside the door (`WorldVersion` 4). No story version was raised.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The wardrobe | (none: it is plan 11 · C10's) | On the tile where the original's game console lies, against the bedroom's back wall; the console's own line is not said | The tile is blocked already, so the room is walked exactly as the original's is, the rival's four ways among them |
| The bedroom's window and poster | The window over the console's tile and the next; the poster beside it, with a scrolling sign | The window a tile further east, clear of the wardrobe; the poster over the computer's desk, read on the desk's east half | A wardrobe stood over half the window |
| The "Adventure Rules" and the poster's tip | The X Button opens the menu; record your progress with SAVE | Our own words, with this game's keys | The handheld's buttons are not this game's |
| The rival on the stairs | Stands two tiles down the flight and walks up | Appears at the stairs' head | Nobody stands on a flight of ours |
| The rival's mother at the door | Stands on the mat | Stands on the door's own tile and comes in two steps | The mat is where whoever comes in stands |
| The kitchen's counter | Part of the room's own model | A shop's counter, with the original's behaviour (spoken across) | Plan 01 (a model reader, if ever) |
| The lab's two great machines | One model two tiles wide and nearly three high, each side of the door | Two of our lab machines side by side | The same |
| The lab's office chairs | Two seats, before the PC and in the east of the hall | Nothing: the tiles are open floor | Our seat is a floor cushion, which is no laboratory's |
| The lab's plan | Black beyond the walls of a room that narrows | Void outside the plan, as a house has (style guide, "Rooms"); Pal Park's gallery, whose void is a well inside the room, is drawn as it was | — |
| Looks | The professor's assistants' own | The scientist's coat for the man, a lady for the woman | Plan 11 · C2 to C4 |
| Naming the first Pokémon | The professor offers a nickname before the Pokédex | Not asked | Plan 12 or 08 (the naming screen from a script) |
| Mom's small talk | One of four lines at random once the story is under way | Her rest, and the tremor's line | Plan 08, if ever |

## Battle Scene and Battle Style (2026-10-13, plan 08 · P1)

Platinum's options screen has BATTLE SCENE (on, off) and BATTLE STYLE (Shift, Set) beside SOUND (stereo, mono). Here they are three rows of the options (`OptionRow.BattleScene`, `BattleStyle`, and a third value of `Speakers`), on, Shift and stereo unless chosen, as Platinum has them, under both sets of rules.

- **Shift's offer, in the original's order** (`BattleControllerPlayer_ReplaceFainted`, `res/battle/scripts/subscripts/subscript_replace_fainted.s` of the pinned commit): the trainer's next Pokémon is chosen first; then, where the style is Shift, the battle is no double, link or Frontier battle (`BATTLE_TYPE_FORCED_SET_MODE`), the player's Pokémon is still standing and the player has another that could come in (`BattleSystem_AnyReplacementMons`), the line names the trainer's next Pokémon and asks, with a Yes and No menu; Yes opens the team (`BtlCmd_ChoosePokemonMenu`, which can be backed out of, and stays in then), and a Pokémon chosen there comes in as `subscript_switch_pokemon` plays it: the recall, the newcomer, what lies in wait for it, a faint; only then is the trainer's Pokémon sent out, with what lies in wait for it. The rules make the same stops (`BattleCore.OfferSwitch`, a `SwitchOfferRequest` answered by `BattleChoice.Stay` or `Switch`), and what acts as the Pokémon come in acts once both are out, as it does after any replacement.
- **Nothing holds the player's Pokémon at the offer**: the original's menu bars nobody (no check of Mean Look, a bind or Shadow Tag), and whatever bound it fell with the foe that did. `BattleCore.WhyNot` passes a switch at the offer over the trap check.
- **Where it is offered**: a single battle against one trainer, answered from outside (never a partner's or an AI's side), never at the Battle Frontier or in a double or tag battle, and only where the battle was made with `CoreSetup.OffersSwitch`, which the game sets from the option for the trainer battles it starts. A battle a tool or a test makes for itself is Set unless it asks, so the golden battles are as they were. A record keeps the flag (`BattleRecord.OffersSwitch`), since its answers include each offer's.
- **The Battle Scene off** is the face's alone (`BattleEngine.Scene`): the original's `BattleSystem_AreAnimationsOn` skips every move's animation but Transform's (`BtlCmd_PlayMoveAnimation`) and every battle animation but the Substitute's swaps (`BtlCmd_PlayBattleAnimation`), and so do we: no lunge, no move's effect, no attack sent ahead landing, no stat's, heal's or condition's cue, none of the turn's moments, no weather's cue, and none of their sounds. A move's own sound and a hit's flicker, shake and sound stay, as the original's damage and its blink do; so do the send-outs, the recalls, a faint, the ball, the doll, the weather's layer, what lies on the field and what lasts on a Pokémon, which are ours and not animations the original's switch turns off. What a line carries lands as the line appears, through the same pending effects in the same order. The rules never read it.
- **Mono** folds the two sides of the mixer's output into their middle (`AudioMixer.Mono`), after the handheld's speakers if those are chosen too, though the row offers one or the other.

| What | Platinum | Here | Why |
| --- | --- | --- | --- |
| The offer's words | The original's line | Our own: "Ace Trainer Vera has Starly coming out next. Will you change Pokémon?" | Nothing of the original's text is taken |
| Answering the offer by moving on | No answer but Yes or No | A confirm, B or No stays in | Whatever reads a battle on as messages (the tests' `Settle`, the harness's `Confirm`) gets past it as it does a move to learn |
| The team after Yes | The party screen of the original | The battle's switching cards, their question naming the Pokémon coming | One switching panel for every switch |

## The field of the later games (2026-10-17, plan 06 · R19b)

The rules are Pokémon Showdown's (`data/conditions.ts`, `data/moves.ts`, `data/abilities.ts`), as for every rule past Platinum's. None of these is in Platinum, so nothing here changes a battle of Platinum's rules (`battles-platinum.txt` held still); the moves and abilities that bring them are R24 to R27's and call the core's entry points.

- **Snow in hail's place.** Under the modern rules whatever would bring hail brings snow (Generation 9): the move Hail, Snow Warning (whose line is then "brought the snow!") and a snowy place's sky. Snow hurts nobody and raises an Ice type's Defense by half against physical moves; everything else hail does, snow does (Blizzard never misses, Weather Ball turns Ice, Ice Body and Snow Cloak, the healing moves' quarter, Castform's snowy shape, Aurora Veil). The trainer AI reads snow as hail. *Ruling*: the move Hail, which the newest games no longer have, is kept and brings snow, since Platinum's trainers and learnsets still know it.
- **The strong weathers** (Desolate Land, Primordial Sea, Delta Stream): no end of their own; no other weather can take their place (a weather move fails, a weather ability says nothing); another strong weather can. They go when the last Pokémon on the field with the ability that brought them leaves (a switch, a faint), with their own line. The extremely harsh sun is the sun to every other rule and burns a damaging Water move away before it reaches anyone (its PP spent); the heavy rain is the rain and puts a damaging Fire move out; the strong winds are no weather to any other rule and make a move that would be super effective on the Flying type neutral against it (Stealth Rock is not a move's hit, and is untouched).
- **The terrains**, one at a time, five turns and eight from a Terrain Extender in the setter's hands, running down after the weather at a turn's end. They reach a Pokémon on the ground (as Spikes ask: not a Flying type, not floating by Levitate or Magnet Rise, unless Gravity, roots or an Iron Ball hold it down) and not up in the air, under the ground or the water. *Electric*: Electric moves of a Pokémon on it 5325/4096 as strong; no sleep. *Grassy*: Grass moves 5325/4096; a sixteenth of its HP back at each turn's end (before the roots and Leftovers); Earthquake, Bulldoze and Magnitude half as strong against it. *Misty*: no major condition and no confusion (a rampage's too); Dragon moves half as strong against it. *Psychic*: Psychic moves 5325/4096; a move that goes first (priority above nought) fails against a foe on it. Nature Power, Camouflage and Secret Power take the terrain's row before the ground's: Thunderbolt, Energy Ball, Moonblast, Psychic; Electric, Grass, Fairy, Psychic; paralysis, sleep, Special Attack down, Speed down. *Ruling*: the power's bonus is applied to the move's power in the formula's place for the field's bonuses (beside the sports), where Showdown's onBasePower has it.
- **Aurora Veil**: Reflect and Light Screen at once over its side for five turns (eight with a Light Clay), only while hail or snow is in effect; it fails while up; it counts once beside either screen; a critical hit goes past it; Brick Break and Defog take it away.
- **Sticky Web**: one layer; whoever comes in on the ground loses a stage of Speed, as if a foe had lowered it (so Clear Body keeps it off), after the other hazards, and Magic Guard does not walk through it. Rapid Spin and Defog take it away. G-Max Steelsurge's sharp steel is R22's.

| What | Showdown | Here | Why |
| --- | --- | --- | --- |
| Defog's reach | Clears the user's side's hazards and the terrain too (Generation 8) | Platinum's Defog with the veil and the web added to what it clears from the target's side | Defog's later rule is R24's, with the moves |
| Who hears of a terrain changing first | Speed order | The places' order | Sorting by Speed may roll for a tie, and nothing answers yet (Mimicry is R27's, which sorts) |
| Lines | The games' | Our own words on the same beats | Nothing of the original's text is taken |

## Shapes that change in battle (2026-10-17, plan 06 · R19b)

What `CheckShapes` grew into: each shape goes by its ability's name in force (none under Gastro Acid), never on a transformed Pokémon, and takes the form's own stats (no such form changes HP). A *passing* shape is given back as the Pokémon leaves the field or the battle ends; a *lasting* one stays through switching and is given back as the battle ends. Each is told by its line with `Reshaped`.

- **Stance Change** (Aegislash, passing): the Blade Forme before any move that hits, the Shield Forme before King's Shield, ahead of the move's line.
- **Zen Mode** (Darmanitan and its Galarian form, passing): at a turn's end, the Zen Mode at half its HP or under, back above.
- **Relic Song** (Meloetta, passing): having landed, it turns Meloetta between its Aria and Pirouette Formes.
- **Schooling** (Wishiwashi, level 20 or more, passing): as it comes in and at each turn's end, the School Form above a quarter of its HP, the Solo Form at a quarter or under.
- **Shields Down** (Minior, passing): as it comes in and at each turn's end, its core above half its HP is under its shell, and at half or under the shell falls away (`Minior` and `Minior-Red`, `Minior-Blue-Meteor` and `Minior-Blue`). Its shell's keeping off conditions is R27's.
- **Disguise** (Mimikyu, lasting): the first hit of a move that would harm it is taken by the disguise (nothing said of how the type did), which is busted, and it loses an eighth of its HP. Mold Breaker goes past it.
- **Ice Face** (Eiscue, lasting): the first physical hit is taken by the icy face, which breaks; it forms again as hail or snow comes in, or as it comes in under them.
- **Gulp Missile** (Cramorant, passing): Surf, or Dive's plunge, brings up an Arrokuda (above half its HP) or a Pikachu (at half or under); hit by a move while it holds one, it spits it at the attacker for a quarter of the attacker's HP and its Defense down a stage, or paralysis.
- **Hunger Switch** (Morpeko, passing): it turns between Full Belly and Hangry at every turn's end, with nothing said.
- **Zero to Hero** (Palafin, lasting): leaving the field in its Zero Form, it is the Hero from then on, told once a battle as it comes in again.

## Oracle disagreements (2026-10-17, plan 24 · X6)

The modern rules' damage formula (`DamageCalculator.Calculate` under `Ruleset.Modern`) is held against Pokémon Showdown's damage calculator (`@smogon/calc` 0.12.0, generation 9; `tools/CalcOracle`) on 3,000 scenarios drawn by `PokemonPlatinumTests/Oracle/OracleScenarios.cs`: 1,122 species and forms of all 1025 species built `Under(Ruleset.Modern)`, 310 moves of one hit that the engine runs fully, the abilities and Platinum's held items the engine runs, IVs, effort, natures, stat stages, statuses, the seven weathers, the four terrains, the three screens, critical hits, single and double battles. Every roll is compared, and both Pokémon's stats first. **2,244 scenarios agreed to the point on every roll and every stat** when X6 was written; the 756 that didn't were each named by an entry of `PokemonPlatinumTests/Oracle/known-gen9.json` (`OracleTests.TheModernRulesAreGeneration9s` fails on any other), below with how many scenarios each took and what was recommended. Nothing was changed in the rules for them then: none is a plain bug of Platinum's baseline, and each is a modern rule the engine doesn't have yet. The decompilation has no say here (it is Platinum's); Showdown is the reference for the later generations' rules (plan 06).

| Disagreement | Scenarios | Owed by | Recommendation |
| --- | --- | --- | --- |
| **The screens are the last modifier.** Platinum halves a hit behind Reflect or Light Screen inside the base damage, before the roll, the same-type bonus and the type chart (two thirds for two behind one screen); from generation 5 a screen is a final modifier (2048/4096, 2732/4096 in a double battle), applied after all of them, and Aurora Veil with them. | 503 (56% of the scenarios with a screen) | the modern damage formula | A session of its own before R24 (proposed, "the modern damage formula"): `DamageCalculator` takes the generation 5+ order behind a value of the `Ruleset` (`Ruleset.ModernDamageFormula`), Platinum's order kept for the baseline. `battles-modern.txt` moves with it, `battles-platinum.txt` must not. |
| **The burn halves after the type chart.** Platinum halves a burned Pokémon's physical hit inside the base damage, before the + 2; from generation 5 the halving comes after the roll, the same-type bonus and the type chart. | 153 | the modern damage formula | The same session. |
| **The weather comes after the + 2.** Platinum strengthens or weakens a Fire or Water move in the sun or the rain inside the base damage; from generation 5 the weather is a modifier of its own after the + 2, beside a spread hit's. | 36 | the modern damage formula | The same session. |
| **The modifiers in 4096ths.** Platinum rounds down after every bonus; from generation 5 the bonuses to the power, the stats and the damage are chained in 4096ths and rounded half down (Technician 6144/4096, Life Orb 5324/4096, a type-boosting item 4915/4096), and the same-type bonus is rounded half down. A point or two. | 52 (of 661 scenarios with no screen, burn, weather, terrain, critical hit or second Pokémon, 10 disagree) | the modern damage formula | The same session. Until it lands, the four entries excuse only a difference of at most 8 points or 3% of the hit on every roll (2 points for this one), so a real difference hiding among them still fails. |
| **Lightning Rod and Storm Drain take the move in.** From generation 5 they make their holder immune to the type they draw and raise its Sp. Atk; the engine runs Platinum's abilities, which only draw a move aimed at a partner. | 6 | plan 06 · R27 | Behind the `Ruleset` with the modern abilities (R27), or with the modern formula's session if it comes first: the immunity is the core's (`AbsorbsMove`), not the formula's. |
| **Photon Geyser's physical side.** The move is physical when its user's Attack is higher than its Sp. Atk, and ignores abilities. The data gives it no `effect`, so the engine runs it as a plain special move and `coverage.md` counts it as fully run. | 6 | plan 06 · R24 to R26 | Give it an effect name in `tools/DataImporter/Overrides` so that it is counted as partly run until the batch of moves that writes it. The other moves of its kind (Psyshock, Body Press, Foul Play, Shell Side Arm, Tera Blast) have their effects and are partly run, so the oracle doesn't draw them. |

**Since plan 06 · R19c (2026-10-17) 2,993 scenarios agree.** The modern damage formula (`Ruleset.ModernDamageFormula`, `DamageCalculator.Modern.cs`) settled the four disagreements of the formula: the screens, the burn, the weather and the 4096ths are no longer on the known list, and Normalize's fifth more power (generation 7) came with them. What is left is Lightning Rod and Storm Drain (1 scenario, R27): **since plan 06 · R24 (2026-10-17) 2,999 agree**, Photon Geyser having its effect (`PhotonGeyser`: physical when its user's Attack at its stage is the higher, abilities ignored, `DamageCalculator.PhysicalByStats` and `IgnoresAbilities`) and its entry taken off; **since plan 06 · R27 (2026-10-18) all 3,000**, Lightning Rod and Storm Drain taking their type in under the modern rules (the core's, `AbsorbsMove`: a scenario the core keeps from the formula agrees with the calculator's nothing) and `known-gen9.json` empty. Where the formula puts each of Platinum's bonuses under the modern rules is plan 06's R19c.

What the oracle leaves out, and why (each is the core's before the formula is asked, or a battle's past): moves of several hits, of a power the battle's course decides (Facade, Brine, Payback, Eruption below full HP), that change their type (Weather Ball, Judgment), Knock Off (its power is the core's), Brick Break, Dream Eater and Fake Out (they fail in the core); an ability that takes a move in (Levitate, Water Absorb, Volt Absorb, Motor Drive, Flash Fire, Dry Skin, Wonder Guard, Soundproof) against a move it would take; Water moves in the harsh sun and Fire moves in the heavy rain; a move of priority on Psychic Terrain; Download, Forecast and Multitype, whose part is played as their holder comes in; the berries that weaken a hit as it lands; Mega Evolution, Z-Moves, Dynamax and Terastallization, which R20 to R23 add with their scenarios; Let's Go's partner moves and forms, which generation 9 doesn't have.

Found on the way, in the data's names rather than the rules: the data names a few forms otherwise than Showdown does, where `CLAUDE.md` says they keep Showdown's spelling. A female's own form is `Pyroar-Female`, `Meowstic-Female`, `Indeedee-Female`, `Basculegion-Female`, `Frillish-Female`, `Jellicent-Female` (Showdown: `-F`, or a look of the species alone); `Aegislash` is Showdown's `Aegislash-Shield` and `Minior` its `Minior-Meteor` (Showdown's `Minior` is the Core); `Necrozma-Dusk`, `Tauros-Paldea-Blaze-Breed`, `Ogerpon-Cornerstone-Mask`, `Darmanitan-Galar-Standard`, `Zygarde-10-Power-Construct` and `Squawkabilly-White-Plumage` are Showdown's `Necrozma-Dusk-Mane`, `Tauros-Paldea-Blaze`, `Ogerpon-Cornerstone`, `Darmanitan-Galar`, `Zygarde-10%` and `Squawkabilly-White`. The oracle takes each as Showdown's (its answers say so in their notes) and the stats agree. Recommended: nothing until a session reads a form by Showdown's name (plan 06 · R29, plan 07's links), which renames them or keeps a table of the two. The calculator doesn't know nine held items with no part in damage (Exp. Share, Smoke Ball, Soothe Bell, Everstone, Lucky Egg, Pure Incense, Luck Incense, Cleanse Tag, Amulet Coin), and works those scenarios out with no item.

## Every species in Sinnoh with nobody else (2026-10-17, plan 03 · D12a)

The user's decision 4 (2026-10-12): a region holds every species of its own games, and the whole Pokédex can be completed with no link. In Sinnoh that is the 493 of Diamond, Pearl and Platinum. `SpeciesSources` (GPU-free) is every way the data and the scripts give, and `SinnohSpeciesTests` holds the 493 to it. The user answered the session's questions on 2026-10-17 (wave 19); the table is as they decided.

| What the original asks | What it was | Here |
| --- | --- | --- |
| Diamond's and Pearl's species Platinum's tables leave out | The other version, or a trade | Ours, in the places Diamond and Pearl have them, with the National Pokédex, in the grass's slots 8 and 9 at those slots' own levels (an overlay's `national`): Murkrow and Misdreavus in Eterna Forest (13), Stunky and Skuntank on Route 221 (31 and 30), Glameow and Purugly on Route 222 (40 and 41), Trapinch and Vibrava on Route 228 (49 and 50; the two versions' Poké Radar there); and Clamperl on the Super Rod's slot 3 on Routes 219 and 221 (40 to 55). Diamond and Pearl have the first two at night only and the Trapinch line by the radar only: here they take the slots a second game in the console filled, at any hour, so that Platinum's own night and radar species stay as they were |
| A trade evolution | A trade with another console | A Linking Cord used on the Pokémon counts as a trade; the GTS's receptionist hands one over whenever the bag holds none, as many as are wanted (the user kept it unlimited) |
| The other two starters | Not in Diamond, Pearl or Platinum | The professor's gift with the National Pokédex, at level 5, so they are raised and evolved as the player's own (the user's choice) |
| Cranidos or Shieldon by the trainer's number | The Underground digs one kind of fossil for an odd number and the other for an even one | The museum's collector gives one of each, once, with the National Pokédex |
| Leafeon at the Moss Rock | Eterna Forest | Eterna Forest's overlay names the Moss Rock (it was missing) |
| A second game in the console's species (the dual slots), the other regions' first partners, the older regions' legendaries | Migrated from the GBA games to Pal Park, or an event | Pal Park, where the original brought the GBA games' species over (the user kept it): its Catching Show brings six visitors from the species nothing else gives, by a fixed rotation by the date (`PalParkState.VisitorSpecies`: the pool in National order read as a ring, each day the next six, so one date always brings the same six and the whole pool comes round in turn; no chance decides who comes), at levels 47 to 55. Another day's six that still wait make way for today's whenever no show is under way (`PalParkState.Welcome`) |
| Rotom's appliances, Shaymin's Sky Forme, the event legendaries' places | The Secret Key, the Gracidea, the Member Card, the Azure Flute, the Oak's Letter | Plan 03 · D12c; until then the legendaries come to Pal Park |
| Unown's ! and ? | The room past Maniac Tunnel | Plan 08 · P19 |

## Mega Evolution and Primal Reversion (2026-10-17, plan 06 · R20)

The rules are Pokémon Showdown's at the pinned commit (`sim/battle-actions.ts`' `canMegaEvo` and `runMegaEvo`, `sim/battle-queue.ts`' order of actions, `sim/side.ts`' once a battle, `sim/battle.ts`' queue sorted again, `data/items.ts`' stones and orbs), behind `Ruleset.MegaEvolution` (the modern rules have it, Platinum's don't). Code: `Battle/Sim/BattleCore.Mega.cs`; tests: `MegaEvolutionTests`.

- **Who may**: a trainer with a Key Stone (the player while one is in the bag, `CoreSetup.KeyStone`; a trainer by `Trainer.KeyStone`), for a Pokémon holding its own species' Mega Stone (a stone for one form of its species, the Eternal Flower's, only in that form; `MegaStone.HeldByForm`) or a Rayquaza that knows Dragon Ascent and holds no Z-Crystal. Not while transformed, not in a Mega or primal form already. A wild Pokémon never.
- **Once a battle for each trainer**: the player's own Pokémon count as one trainer, a partner and each opposing trainer as theirs. Two in one turn's answer are refused, as is one after the trainer has Mega Evolved (`WhyNotMega`, `megaOnlyOnce`).
- **When**: chosen with the move (`BattleChoice.Mega`), it happens once the turn's switches and items are done and before its first move (Showdown's `megaEvo`, order 104, after `switch`'s 103), the fastest first. The moves still to come are then put in order again by the Speeds it gave, as Showdown sorts its queue before each move from Generation 8 (Generation 7 moved only the Mega's own move; Generation 6 not even that). Focus Punch's tightening is still said before the turn's switches, Platinum's order, so it comes before the Mega's line.
- **What it changes**: the form (`Pokemon.ChangeForm`), and with it the stats, the types (whatever a move had changed of them goes, as Showdown's `setSpecies` sets them afresh), the weight and the ability, the form's first, which then acts as one does on entry (Intimidate, Drought, Delta Stream). It lasts through switches and is given back, with the ability, as the battle ends. Its line names the stone and the trainer whose Key Stone answers it (Rayquaza's says its trainer's wish reached it), then "has Mega Evolved into Mega …", which carries `MegaEvolved`.
- **Primal Reversion**: a Kyogre holding the Blue Orb or a Groudon holding the Red Orb takes its primal form as it comes in, needing no Key Stone and spending no Mega Evolution, and given back as the battle ends. **Ruling**: it happens before any ability acts on entry, as in Omega Ruby and Alpha Sapphire, so its new ability is the one that does and Drizzle or Drought is never heard; Showdown runs the orb's `onSwitchIn` at priority −1, after the base ability's start.
- **The strong weathers' abilities run** with it: Desolate Land, Primordial Sea and Delta Stream bring the harsh sun, the heavy rain and the strong winds as their holder comes in, through R19b's `SetStrongWeather`, which ends them once nobody on the field has the ability. They were R27's; R20 brings them because the primal forms and Mega Rayquaza do nothing without them.
- **Stones and orbs stay in their hands**: a Mega Stone held by its own species, the Blue Orb by a Kyogre and the Red Orb by a Groudon can't be knocked off (nor does Knock Off gain its power on them), stolen, swapped by Trick, given by Bestow to the one it belongs to, or thrown (Showdown's `onTakeItem`, asked of both hands). A stone in another species' hands is an item like any other.
- **The trainer AI** Mega Evolves whenever it can, with whatever move it chose, drawing nothing for it (Showdown's random player with its `mega` option at 1). Under Platinum's rules, or with no Key Stone, it chooses exactly what it chose before.
- **The data**: the Key Stone (a key item), the Red Orb and the Blue Orb (held items) are imported from PokeAPI by name (`Importer.LaterItemsByName`); the 89 Mega Stones and the 97 Mega and 2 primal forms were already there (R1, plan 03 · D11). **Where the stones are found is Kalos's**: nothing in the game gives a Key Stone, a Mega Stone or an orb, and plan 23's Kalos chapter places them with the beat that hands the Key Stone over.
- **Approximations**, each Showdown's rule cut down: a Mega Evolution doesn't count as Truant's action for the turn (Showdown's `moveThisTurnResult`); a Pokémon that leaves under Power Trick or with an ability a move gave it comes back with its Mega form's own; the Megas whose abilities are of later generations (Tough Claws, Aerilate, Parental Bond, Mega Launcher, Magic Bounce…) have them in name only until R27; Illusion and Sky Drop, which Showdown checks, aren't in the game yet.
- **The screen** (`BattleEngine`): the item button in the move menu arms the Mega Evolution for the move to be chosen (`ToggleMega`, `MegaArmed`, `CanMegaEvolveNow`); the screen's Pokémon takes the form from `MegaEvolved` and plays Transform's cue on itself as a stand-in. The forms' models, which plan 03 built, are asked for before the battle with the Pokémon's own (`GameEngine.BattleShapesOf`). The button's picture and the change's own look are plan 25 · Y14's.

## Z-Moves and Ultra Burst (2026-10-17, plan 06 · R21)

The rules are Pokémon Showdown's at the pinned commit (`sim/battle-actions.ts`' `getZMove`, `getActiveZMove`, `canZMove`, `runZPower`, `canUltraBurst` and `runMegaEvo`, `modifyDamage`'s `zBrokeProtect`, `sim/side.ts`' once a battle, `data/items.ts`' crystals and `data/moves.ts`' signature Z-Moves and `healreplacement`), behind `Ruleset.ZMoves` (the modern rules have them, Platinum's don't). Code: `Battle/Sim/BattleCore.ZMoves.cs` (and `Battle/BattleEngine.ZMoves.cs` for the screen); tests: `ZMoveTests`.

- **Who may**: a trainer with a Z-Ring (the player while one is in the bag, `CoreSetup.ZRing`; a trainer by `Trainer.ZRing`), for a Pokémon holding a Z-Crystal that makes a Z-Move of the move chosen (`BattleCore.ZMoveOf`, Showdown's `getZMove`): a type's crystal turns a damaging move of its type into the type's Z-Move and a status move of its type into its Z-Power version; a species' crystal turns its one move into its own Z-Move, in the hands of the species and forms it names (a Pikachu in a cap for Pikashunium Z; Showdown's `Necrozma-Dusk-Mane` and `Necrozma-Dawn-Wings` are the data's `Necrozma-Dusk` and `Necrozma-Dawn`; the totem forms aren't in the game). Not with a move out of PP; not a Mega, primal or Ultra form that has transformed. A wild Pokémon never.
- **Once a battle for each trainer** (`WhyNotZMove`, `zOnlyOnce`), counted as Mega Evolution is; two in one turn's answer are refused. A Z-Move that the Pokémon never got to use (asleep, flinching, fully paralysed) isn't spent: the Z-Power is spent once the move is paid for, as Showdown's `runMove` spends it after `BeforeMove` and the PP.
- **What it is**: the move chosen pays its own PP and then becomes its Z-Move (`BattleChoice.ZMove`; `BecomeZMove` in `ExecuteMove`). "X surrounds itself with its Z-Power!" carries `ZPowerUsed`. A type's Z-Move takes the move's power as a Z-Move (`moves.json`'s `zPower`, the newest games' values), the move's category and the move's priority, hits one target (a spread move's Z-Move is aimed at one) and never misses; it is said to be unleashed before "X used …!". A status move is used as "Z-" and its name, its Z-Power first (`runZPower`, by `zBonus`): its stats raised, its HP restored whole, its lowered stats put back, a critical-hit boost, the centre of attention, Curse's two ways (a Ghost healed, anyone else a stage of Attack), or Memento's and Parting Shot's heal waiting for whoever next comes into the place; then the move itself.
- **Through a protection**: Protect and its kin, Mat Block, a Quick Guard against a move that goes first and a Wide Guard against a move at more than one foe stop no Z-Move, status ones included; a damaging one does a quarter of its damage, the last step of the damage (`modify(damage, 0.25)`, at least 1), with "couldn't fully protect itself and got hurt!". A shield's bite (King's Shield, Spiky Shield, Baneful Bunker) answers no Z-Move.
- **The signature Z-Moves** are data of their own (`moves.json`'s 17, written by the importer from Showdown's entries): plain hits of their power (Catastropika, Sinister Arrow Raid, Oceanic Operetta, Pulverizing Pancake, Let's Snuggle Forever, Soul-Stealing 7-Star Strike), 10,000,000 Volt Thunderbolt's high critical-hit ratio, Stoked Sparksurfer's sure paralysis, Malicious Moonsault twice as strong on a Minimized target, Searing Sunraze Smash and Menacing Moonraze Maelstrom through abilities (Sunsteel Strike's rule), Light That Burns the Sky physical by the higher Attack and through abilities (Photon Geyser's), Genesis Supernova's Psychic Terrain, Splintered Stormshards' end of a terrain, Clangorous Soulblaze at both foes with a stage of every stat after, Guardian of Alola's three quarters of the target's HP (a quarter of that through a protection), Extreme Evoboost's two stages of every stat. The six whose Showdown entries say so make contact; Clangorous Soulblaze is a sound move.
- **Z-Crystals stay in their hands**: no crystal is ever knocked off, stolen, swapped, given or thrown (Showdown's `onTakeItem: false` for every one), whoever holds it.
- **Ultra Burst**: a Necrozma fused with Solgaleo or Lunala (`Necrozma-Dusk`, `Necrozma-Dawn`) holding Ultranecrozium Z, its trainer having a Z-Ring, takes `Necrozma-Ultra` with the turn's Mega Evolutions (`BattleChoice.UltraBurst`, `WhyNotUltraBurst`), the fastest first, once a battle for each trainer and apart from the Mega Evolution, as Showdown's `canUltraBurst`; its lines carry `UltraBursted`, the form lasts through switches and is given back as the battle ends, as a Mega's is. Ultranecrozium Z makes Light That Burns the Sky in Necrozma-Ultra's hands alone, so not in the turn of the burst (Showdown checks the crystal's user as the move is chosen).
- **The trainer AI** uses a Z-Move whenever the move it chose has one, and Ultra Bursts whenever it can, drawing nothing for either; under Platinum's rules, or with no Z-Ring, it chooses exactly what it chose before.
- **The data**: the Z-Ring (a key item) is imported from PokeAPI by name with our own words (`Importer.LaterItemsByName`, `Overrides/items.json`) and named in Romanian; the 35 crystals, the 35 Z-Moves, each move's `zPower` and `zBonus` were already there (R1). **Where the ring and the crystals are found is Alola's** (plan 23): nothing in the game gives them yet.
- **Approximations**, each Showdown's rule cut down: an -ate ability (Normalize here) still changes a Z-Move's type, where Showdown leaves a damaging Z-Move's alone; Hidden Power, Weather Ball and the like make the Z-Move of their own type as the data has it; a status Z-Move can be bounced by Magic Coat and Crafty Shield stops one; what Copycat and Mirror Move see after a Z-Move is the move chosen; an Encore that changes the move turns the Z-Power off where the new move has none. Max Moves and Dynamax are R22's.
- **The screen** (`BattleEngine`): the move menu's item button arms a Mega Evolution where one can be had, and the Z-Power otherwise (`ToggleLaterMechanic`, `ZArmed`, `CanZMoveNow`, `ZMoveAt` for the name the menu may show); the armed move goes as its Z-Move, with an Ultra Burst where one can be had. An Ultra Burst's form is taken from `UltraBursted` with Transform's cue as a stand-in, and its model asked for before the battle (`GameEngine.BattleShapesOf`). The button's picture, the Z-Power's look and each Z-Move's own effect are plan 25 · Y14's.

## Dynamax and Gigantamax (2026-10-18, plan 06 · R22)

The rules are Pokémon Showdown's at the pinned commit (`data/conditions.ts`' `dynamax` condition, `sim/battle-actions.ts`' `getMaxMove` and `getActiveMaxMove`, `sim/pokemon.ts`' `getDynamaxRequest`, `maxMoveDisabled` and `getUndynamaxedHP`, `sim/battle.ts`' `endTurn` and `getActionSpeed`, `sim/battle-queue.ts`' `runDynamax` and `data/moves.ts`' Max Moves, G-Max Moves, Max Guard and what fails against a Dynamaxed Pokémon), behind `Ruleset.Dynamax` (the modern rules have it, Platinum's don't). Code: `Battle/Sim/BattleCore.Dynamax.cs` (and `Battle/BattleEngine.Dynamax.cs` for the screen); tests: `DynamaxTests`.

- **Who may**: a trainer with a Dynamax Band (the player while one is in the bag, `CoreSetup.DynamaxBand`; a trainer by `Trainer.DynamaxBand` and a record's `dynamaxBand`), once a battle for each trainer (`WhyNotDynamax`, `dynamaxOnlyOnce`, counted as Mega Evolution is); two in one turn's answer are refused. Not a Pokémon in a Mega, primal or Ultra form, nor one that could Mega Evolve (holding its stone), nor one holding a Z-Crystal, nor Zacian, Zamazenta or Eternatus, nor one with no Max Move it may use; a choice that Dynamaxes goes with a move of the Pokémon's own and with no other later mechanic. A wild Pokémon never.
- **When**: after the turn's Mega Evolutions and before its first move, the fastest first (`runDynamax`, order 105, after `megaEvo`'s 104). "X swelled to a giant size with Dynamax energy!" (or "…into its Gigantamax form!") carries `Dynamaxed`, with the HP and form from that line on.
- **The HP**: its HP and its most are each raised by 1.5 and a twentieth for every Dynamax Level (`Pokemon.DynamaxLevel`, 0 to 10, saved): twice at 10, worked out in twentieths and rounded down; a Shedinja keeps its one. As the Dynamax ends its HP goes back by the same share, rounded up (`getUndynamaxedHP`), and its most to what it was. **Approximation**: whatever the core works out as a share of the HP (poison, the weather, Leftovers, Super Fang, Pain Split and the rest) reads the Dynamaxed HP, where Showdown reads the HP before it for most of them (`baseMaxhp`); the G-Max Moves' four fields read the HP before it, as Showdown's do. A level gained while Dynamaxed keeps the new most and cuts the HP to it.
- **How long**: three turns: each turn's end counts one (the condition's residual, last of all) and the third ends it, the fastest first, with "X shrank back to its usual size!" carrying `DynamaxEnded`. It ends too, with its event and no line, as its Pokémon is called back or made to leave (before it goes), as it faints, and as the battle ends.
- **What it does to the Pokémon**: Minimize, a Substitute and a Torment end as it begins; a Cramorant lets its catch go. While it lasts it can't flinch, Destiny Bond doesn't take it down, it can't be dragged out by Roar, Whirlwind, Dragon Tail, Circle Throw or a Red Card ("X is far too huge to be moved!"), an Encore turns no Max Move aside, a Choice item neither locks it nor raises its stats, and against it Encore, Torment, Skill Swap, Entrainment, Instruct, Low Kick, Grass Knot, Heavy Slam and Heat Crash fail and a one-hit knockout doesn't affect it.
- **Its moves**: every move it uses becomes its Max Move once its PP is paid (`BecomeMaxMove`, Showdown's `getActiveMaxMove`): a status move Max Guard, a damaging move its type's Max Move of the move's power as a Max Move (`moves.json`'s `maxPower`, the newest games' values), category and priority; a Gigantamax form's damaging move of its type its G-Max Move. "X used Max …!" carries `MaxMoveUsed`. What it may choose is held back only by what holds a Max Move back (`WhyNotMaxMove`: Disable, a taunt or an Assault Vest against a status move, Belch before a berry, no PP), not by Torment, Imprison, Gravity, Heal Block, Throat Chop or a Choice item. Its turn's order goes by the Max Move's own priority (Max Guard's +4, every other 0, `getActionSpeed`), while Psychic Terrain and Quick Guard read the move's own. A Max Move never misses, goes through Protect and its kin at a quarter of its damage as a Z-Move does (`zBrokeProtect`), and does what its `self.onHit` does once it has hit: the five that lower a stage of each foe's stat, the five that raise one of each of its side's, the four weathers (hail is snow under the modern rules) and the four terrains.
- **Max Guard** is Protect's kin, with Protect's chance and chain, at +4, and stops every move but the list Showdown names (Feint, Roar, Perish Song, Mean Look, the G-Max Moves One Blow and Rapid Flow and the rest), Max Moves too.
- **Gigantamax**: a Pokémon with the Gigantamax Factor (`Pokemon.GigantamaxFactor`, saved) whose species or form has a Gigantamax form takes it as it Dynamaxes (`GigantamaxFormOf`: one in a form takes its form's own, Toxtricity's Low Key and Urshifu's Rapid Strike Style; one in a regional form or a cap's Pikachu none; one in no form its species' or the first of them), only its look and its Max Moves changing, and is given its form back as the Dynamax ends. Its G-Max Move is that of the form or species it was in (`gigantamaxOf`). The 33 run by their Showdown entries: the conditions (Befuddle, Stun Shock, Volt Crash, Malodor), confusion (Gold Rush, Smite), love (Cuddle), Torment of foes not Dynamaxed (Meltdown), no escape (Terror), a binding of its own (Centiferno, Sandblast), stats (Foam Burst, Tartness), two PP off the foe's last move (Depletion), a critical-hit stage to its side for each use up to three (Chi Strike), a sixth healed (Finale), every condition of its party cured (Sweetness), a berry eaten found again one time in two (Replenish), Gravity (Gravitas), Aurora Veil whatever the weather (Resonance), Stealth Rock (Stonesurge), the foes' screens and veils and both sides' hazards and the terrain blown away (Wind Rage), drowsiness one time in two (Snooze), 160 power through abilities (Drum Solo, Fireball, Hydrosnipe), every protection gone through whole (One Blow, Rapid Flow), and four fields and a hazard of their own below.
- **What G-Max Moves leave** (`SideCondition` `GMaxSteelsurge`, `GMaxWildfire`, `GMaxCannonade`, `GMaxVineLash`, `GMaxVolcalith`, each with its event on its line and its look in `BattleFieldFx`): Steelsurge's steel on the foes' side until Defog, Rapid Spin or Wind Rage takes it, biting whoever comes in as Stealth Rock would if it were of the Steel type; Wildfire's flames, Cannonade's torrents, Vine Lash's vines and Volcalith's rocks round the foes' side for four turns, a sixth of the HP before any Dynamax off everyone there not of the move's type at each turn's end (after a Pledge's sea of fire). **Approximation**: Court Change doesn't carry them over.
- **The trainer AI** Dynamaxes the first of its Pokémon that can, with whatever move it chose and where it chose no other later mechanic, drawing nothing; under Platinum's rules, or with no Dynamax Band, it chooses exactly what it chose before.
- **The data**: the Dynamax Band (a key item) is imported from PokeAPI by name with our own words (`Importer.LaterItemsByName`, `Overrides/items.json`) and named in Romanian; Max Guard's effect is `MaxGuard`, the other Max Moves' `MaxMove` and the G-Max Moves' `GMaxMove`, all run. The Dynamax Candy is in the data and does nothing yet, and nothing gives the Gigantamax Factor (Max Soup): **where the band, the candy and the factor are found is Galar's** (plan 23).
- **Left**: Dynamax Cannon, Behemoth Blade and Behemoth Bash doubling against a Dynamaxed target (R26's moves: `BattleCore.IsDynamaxed` is there for them); an -ate ability changing a Max Move's type; Hidden Power and Weather Ball making the Max Move of the type they become; Taunt at the move's own time against Max Guard; Max Raid dens (plan 23 · Z10's).
- **Mega Dynamax**, a session of our own that plan 06 lists for later, is a Mega form that Dynamaxes: `WhyNotDynamax` refuses a Mega and `MegaFormOf` a Mega Dynamaxing, and those two are where it would change; everything else (the HP, the Max Moves, the end) holds for any form.
- **The screen** (`BattleEngine`): the move menu's item button arms a Mega Evolution where one can be had, the Z-Power where that can, and the Dynamax otherwise (`ToggleLaterMechanic`, `DynamaxArmed`, `CanDynamaxNow`, `MaxMoveAt` for the name the menu may show, `MenuDynamaxed`); the screen's Pokémon takes the HP and the form from `Dynamaxed` and `DynamaxEnded`, with Transform's cue as a stand-in, and the Gigantamax form's model is asked for before the battle (`GameEngine.BattleShapesOf`). The growth, the red clouds, the button and each Max Move's own effect are plan 25 · Y15's.

## Terastallization (2026-10-18, plan 06 · R23)

The rules are Pokémon Showdown's at the pinned commit (`sim/battle-actions.ts`' `canTerastallize`, `terastallize`, the power of 60 in `getDamage` and the same-type bonus in `modifyDamage`; `sim/pokemon.ts`' `getTypes` and `runEffectiveness`; `sim/battle-queue.ts`' `terastallize` order; `data/moves.ts`' Tera Blast and Tera Starstorm; `data/abilities.ts`' Tera Shift, Tera Shell, Teraform Zero and Embody Aspect; `data/pokedex.ts`' `forceTeraType`), behind `Ruleset.Terastallization` (the modern rules have it, Platinum's don't). Code: `Battle/Sim/BattleCore.Tera.cs`, `Battle/DamageCalculator.Tera.cs`, `Battle/BattleEngine.Tera.cs` for the screen and `Models/TeraType.cs`; tests: `TerastallizationTests`.

- **Who may**: a trainer with a Tera Orb (the player while one is in the bag, `CoreSetup.TeraOrb`; a trainer by `Trainer.TeraOrb` and a record's `teraOrb`), once a battle for each trainer (`WhyNotTera`, `teraOnlyOnce`, counted as Mega Evolution is); two in one turn's answer are refused. Not a Pokémon holding a Mega Stone it could Mega Evolve with or a Z-Crystal (Showdown's `canTerastallize`), nor one in a Mega, primal or Ultra form, nor a Dynamaxed one (decision 06 · 3: one mechanic a Pokémon), and a Terastallized Pokémon can't Dynamax. A choice that Terastallizes goes with any move, the move it is held to included, and with no other later mechanic. A wild Pokémon never. **The orb needs no recharging here**: the games' "recharged at a Pokémon Center" is one battle's use, which is what once a battle already is.
- **When**: after the turn's Mega Evolutions and Dynamaxes and before its first move, the fastest first (`terastallize`, order 106, after `runDynamax`'s 105), and the moves still to come are put in order again. "X Terastallized into the Y type!" carries `Terastallized(Place, TeraType, Form, MaxHp, Hp)`.
- **How long**: to the battle's end, through switches and faints alike: it is kept on the Pokémon (`Pokemon.Terastallized`, never saved), not on its place, and taken off in `End`.
- **The Tera Type** (`Pokemon.TeraType`, a `TeraType`: one of the eighteen types or Stellar, which is kept beside `PokemonType` because it is no type of the chart): **our default is the Pokémon's own first type** (the type of its form), with no draw, so no battle's dice moved for it; Showdown's random battles choose one from a set, the games one of the Pokémon's own types at random. One given to it (a Tera Shard's in Paldea, a trainer's) is kept and saved (`SavedPokemonData.TeraType`, left out when none was given). Ogerpon's is its mask's whatever is given (Grass with the Teal Mask, Water, Fire, Rock with the others) and Terapagos's Stellar (Showdown's `forceTeraType`).
- **Defence**: a Terastallized Pokémon is its Tera Type alone (`Battler.Types`, `HasType`: before a plate, Conversion's or Soak's types), roosting takes no Flying type from it, and whatever a move had added to its types goes. A Stellar one keeps its own types.
- **The same-type bonus** (`DamageCalculator.TeraStab`): half again for a move of its Tera Type or of one of its own types (`Battler.HasTypeBeforeTera`), twice where the Tera Type is one of its own; Adaptability makes the bonus twice, or 2.25 times where it was twice, but only for a move of its type now, the Tera Type (Showdown's `onModifySTAB` asks `hasType`). **Stellar**: its own types keep their half, and the first use of each type is twice for one of its own types and 4915/4096 for another (`stellarBoostedTypes`; every use for a Terapagos in its Stellar Form); no Adaptability. The boost is decided once a use, so every hit and every target of the use has it (`move.stellarBoosted`). **Approximation**: the core decides it at the use's first hit, where Showdown pushes the type as each hit's damage is worked out, so a Stellar spread move's later targets lose the power of 60 there and keep it here.
- **The power of 60** (`BattleCore.TeraPower`, `getDamage`): a move of its Tera Type (for a Stellar one, a move whose boost it still has) weaker than 60 hits as 60, unless the move has priority in the data, strikes several times, or is of a power its effect decides from nothing (its data's 0, or 150).
- **Stellar moves** (`MoveData.Stellar`, a copy of the move for one use, its `Type` Normal and read by nothing): no type stands in their way and none resists them, and they are super effective against a Terastallized target (`runEffectiveness`).
- **Tera Blast**: a Terastallized user's is of its Tera Type, whatever Normalize says, and physical where its Attack at its stage is higher than its Sp. Atk at its stage; a Stellar one's is of the Stellar type at 100 power and lowers its user's Attack and Sp. Atk a stage once it has hit. **Tera Starstorm**: from a Terapagos in its Stellar Form it is a Stellar move at every foe, physical where its Attack is the higher and it has Terastallized; from anyone else a Normal move at one target (the data's "all foes" is narrowed by the effect's `Aim`).
- **Ogerpon**: Terastallizing, its ability is Embody Aspect for the battle and raises a stat by its mask (Speed with the Teal Mask, Sp. Def with the Wellspring, Attack with the Hearthflame, Defense with the Cornerstone), and again at each entry while Terastallized. **Approximation**: the data has no "-Tera" forms of Ogerpon, so it keeps its form; the ability is given back as the battle ends.
- **Terapagos**: Tera Shift takes it into its Terastal Form as it comes in, its HP raised with its most (`ChangeForm`, as Showdown's own sum does), under any rules (it is the ability's, not Terastallization's); Terastallizing in its Terastal Form it takes its Stellar Form, and Teraform Zero ends the weather and the terrain, whatever brought them. Both forms are given back as the battle ends. **Tera Shell** at full HP makes a damaging move not very effective unless its type can't touch it at all, and Mold Breaker's kin go through it. **Approximation**: it says no line of its own, and each hit asks it afresh, where Showdown keeps a move of several hits resisted.
- **The trainer AI** Terastallizes the first of its Pokémon that can, with whatever move it chose and where it chose no other later mechanic, drawing nothing; under Platinum's rules, or with no Tera Orb, it chooses exactly what it chose before.
- **The data**: the Tera Orb (a key item) is imported from PokeAPI by name with our own words (`Importer.LaterItemsByName`, `Overrides/items.json`) and named in Romanian; Tera Blast and Tera Starstorm run (`TeraBlast`, `TeraStarstorm` in `BattleCore.Tera.cs`). **Where the orb and the Tera Shards are found is Paldea's** (plan 23): the eighteen shards and the Stellar one are not in the data yet, and nothing changes a Tera Type in the game.
- **Left**: the moves that change a type (Soak, Conversion, Reflect Type, Magic Powder, Forest's Curse, Trick-or-Treat, Burn Up and Double Shock) say their line against a Terastallized Pokémon and change nothing it is shown as, where Showdown makes them fail; Illusion's end as an Ogerpon or a Terapagos Terastallizes; Morpeko's form; Tera Raid crystals (plan 23 · Z15's).
- **The screen** (`BattleEngine`): the move menu's item button arms a Mega Evolution, then a Z-Move, then a Dynamax, then a Terastallization, whichever can be had first (`ToggleLaterMechanic`, `TeraArmed`, `CanTerastallizeNow`, `MenuTeraType`); the screen's Pokémon takes the Tera Type, and a Terapagos its Stellar Form and HP, from `Terastallized`, with Transform's cue as a stand-in, and loses it as the battle's `Ended` is shown. The crystal, the crown and the button are plan 25's.

## The later abilities (2026-10-18, plan 06 · R27)

167 of the 190 abilities of Generations 5 to 9 run, each by Pokémon Showdown's rule (`data/abilities.ts` at the pinned commit; the data's own description for the seven of Legends: Z-A, which the pinned commit doesn't have). Where each is written:

- **In the damage formula**, by name, in the chain Showdown puts it in (`Battle/DamageCalculator.Abilities.cs`: `LaterPower`, `LaterAttack`, `LaterDefense`, `LaterFinal`, one line each in `DamageCalculator.Modern.cs`). The modern formula alone reads them: no Pokémon of Platinum's rules has one, and Platinum's order has no place for them. The power: Sheer Force 5325, Tough Claws 5325 for a touch, Strong Jaw, Mega Launcher and Sharpness 6144 for a bite, a pulse and Showdown's slicing moves, Punk Rock 5325 for a sound move, Toxic Boost and Flare Boost 6144, Analytic 5325 against a target that has moved, Sand Force 5325 in the sand, the -ate abilities' 4915 (Aerilate, Pixilate, Refrigerate, Galvanize, Dragonize), Parental Bond's second hit 1024, Supreme Overlord 4096 + 410 for each of its party fainted (five at most), Steely Spirit, Battery and Power Spot for an ally, the auras' 5448 (3072 under Aura Break), Fire Mane 6144 for Fire. The stats: Defeatist 2048, Water Bubble 8192 (and 2048 against Fire), Purifying Salt 2048 against Ghost, Steelworker, Dragon's Maw and Rocky Payload 6144, Transistor 5325, Gorilla Tactics 6144, Orichalcum Pulse and Hadron Engine 5461, Protosynthesis and Quark Drive 5325 on the best stat by its stage, Stakeout 8192 against a Pokémon that came in this turn, the four Ruin abilities 3072 on everyone else's stat (never two of a kind), Fur Coat 8192, Grass Pelt 6144. The final modifiers: Multiscale and Shadow Shield 2048 at full HP, Ice Scales 2048 against a special move, Fluffy 2048 for a touch and 8192 for Fire, Punk Rock's 2048 against sound, Aura Guard 2048 against a touch, Neuroforce 5120, Friend Guard 3072 for an ally, Prism Armor as Filter. Infiltrator passes the screens; Merciless is critical against a poisoned target.
- **In the core**, by name (`Battle/Sim/BattleCore.Abilities.cs`, a line each where the course reads it): Prankster's, Gale Wings' (at full HP) and Triage's priority (`PriorityOf`); Quick Draw (three in ten, as a Quick Claw) and Mycelium Might (last, as a Lagging Tail) in the turn's order; the move as its user makes it (`ShapedByAbility`, after Normalize: the -ate abilities, Liquid Voice, Long Reach, Sheer Force taking the move's side effects away, Protean and Libero once a stay, generation 9); Parental Bond's two hits; a move stopped before it lands (`StoppedByAbility`: a Prankster's status move against a Dark foe, Dazzling, Queenly Majesty and Armor Tail against priority on their side, Good as Gold, Telepathy); Magic Bounce before Magic Coat; Unseen Fist and Piercing Drill past Protect and the shields; Infiltrator past a Substitute; Mirror Armor and Guard Dog against a stat lowered by another, Defiant and Competitive after one; Corrosion poisoning a Steel or a Poison type; Poison Puppeteer confusing whoever it poisons; Soul-Heart at every faint; Heavy Metal and Light Metal in `WeightOf`; Propeller Tail and Stalwart past Follow Me and the redirecting abilities.
- **As effects** (`Battle/Effects/LaterAbilityEffects.cs`, listed in `Data/AbilityEffectTable.Later.cs`): the Surges, Orichalcum Pulse and Hadron Engine on entry; Intrepid Sword, Dauntless Shield and Supersweet Syrup once a battle; Screen Cleaner (with each screen's `SideConditionChanged`), Curious Medicine, Costar, Hospitality, the auras' and the Ruin abilities' lines, Turboblaze and Teravolt as Mold Breaker; Sap Sipper, Well-Baked Body, Wind Rider, Earth Eater, Bulletproof, Overcoat and **Lightning Rod and Storm Drain, which take their type in and raise Sp. Atk under the modern rules** (they only draw it under Platinum's, so the last oracle disagreement is gone and `known-gen9.json` is empty: 3,000 of 3,000 agree); Justified, Rattled, Stamina, Water Compaction, Steam Engine, Thermal Exchange, Weak Armor, Berserk, Anger Shell, Electromorphosis, Wind Power, Gooey, Tangling Hair, Iron Barbs, Cotton Down, Sand Spit, Seed Sower, Spicy Spray, Cursed Body, Mummy, Lingering Aroma, Wandering Spirit, Perish Body, Pickpocket, Innards Out, Toxic Debris (a layer of Toxic Spikes with its event); Poison Touch, Toxic Chain, Magician, Moxie, Chilling Neigh, Grim Neigh, Beast Boost, Eelevate; Contrary, Big Pecks, Full Metal Body, Mind's Eye, Sweet Veil, Pastel Veil, Shields Down's shell, Water Bubble, Purifying Salt, Comatose; Moody, Harvest, Healer, Regenerator; Sand Rush, Slush Rush, Surge Surfer; Mimicry through `OnTerrainChange`; and the shapes of R19b and R23 that the core ran by name already (Zen Mode, Stance Change, Schooling, Disguise, Ice Face, Gulp Missile, Hunger Switch, Zero to Hero, Embody Aspect, Tera Shift, Tera Shell, Teraform Zero), given their entries.

**Cut down**, each Showdown's rule made smaller: Sheer Force takes away what the move's data says (a condition, a flinch, confusion, a stat change that isn't sure, its user's rise, and since R27b the chance its own code rolls by) but no other part of a move's own code, and leaves a Life Orb's recoil; Analytic asks whether its target has moved, not every target; Protosynthesis and Quark Drive have no Booster Energy and say their line only as their holder comes in under the sun or the terrain; Quick Draw says nothing; Mycelium Might doesn't pass the target's ability; Comatose keeps every condition off but isn't counted as asleep by Sleep Talk, Snore, Dream Eater or Wake-Up Slap (Hex counts it, R26a); Wonder Skin scales the roll to 50 of the move's accuracy instead of setting it before the stages; Guard Dog answers Intimidate alone; Full Metal Body and Prism Armor are passed by Mold Breaker; a Prankster's move called by another isn't stopped by a Dark type, and Psychic Terrain and Quick Guard read a move's own priority, not the one an ability gives it; Regenerator's third is said nothing of (as Showdown); Fire Mane, Aura Guard and Piercing Drill (Legends: Z-A) go by the data's description (half as strong again, half the damage of a touch, a touch past Protect at full damage).

**The last 23** (plan 06 · R27b, 2026-10-18), by `data/abilities.ts` and `data/conditions.ts` at the pinned commit, which the importer now fetches into its cache with the rest of Showdown's files (`Sources.ShowdownFiles`): every one of the 190 runs. The classes are `Battle/Effects/LaterAbilityEffects2.cs`, the table `Data/AbilityEffectTable.Later2.cs`, and what the core reads by name `Battle/Sim/BattleCore.Abilities2.cs`:

- **Unnerve and As One** keep every berry from the foes while they stand (`BattleEffects.ItemOf`, the resist berries and the berries that answer a hit; `LaterAbilities.Unnerved`), each said as it comes in; As One is one ability in the data, Chilling Neigh for the Ice Rider and Grim Neigh for the Shadow Rider by Calyrex's form.
- **Illusion** sends its holder out under the name of the last of its party behind it that can still fight (`IllusionFor`, `Volatiles.Illusion`, which `Battler.Name` says), and a damaging hit, or Neutralizing Gas coming in, breaks it. **Imposter** transforms into the foe across from it as it comes in (the diagonal one in a double battle), not into one behind a Substitute, out of reach, transformed or disguised.
- **Neutralizing Gas** (`Battler.Ability` asks `LaterAbilities.Neutralized`): while a holder stands (not transformed, its own not suppressed) every other ability is none but Showdown's `cantsuppress` ones, an Ability Shield's holder's and a Tatsugiri in command; as it comes in the others' entry checks are cleared, so once the last gas goes (said first at the next entry check) whatever they do on entry they do again.
- **Aroma Veil** stops Attract, Disable, Encore, Heal Block, Taunt and Torment aimed at its side before they land; **Flower Veil** keeps another's stat drops and conditions (Yawn too) off a Grass type of its side; and, as the audit below found, **Sweet Veil** and **Pastel Veil** keep their condition off the whole side (`VeiledFrom`). Mold Breaker passes all four.
- **A berry's own course**: whatever is used up is noted (`ConsumeItem` → `Ate`) and answered once its own line is said (`Digest`, after each action, each turn's end and each check of a Pokémon's condition): **Cheek Pouch** gives a third of its HP back for a berry, **Cud Chew** chews it again at the end of the next turn (by the berry's eaten-out-of-turn effect, as Bug Bite gives it), **Symbiosis** hands an ally that used its item up its own; **Ripen** says so before the berry's line and doubles what the berry heals and raises (`Ripened`), and a resist berry takes three quarters of the hit away.
- **Receiver and Power of Alchemy** take on a fainted ally's ability (not one of Showdown's `noreceiver`), which acts at the next entry check.
- **Wimp Out and Emergency Exit**: a hit that takes the holder from over half its HP to half or under (not one Sheer Force made stronger) sends it back once the move is done, if someone can come in, whatever traps it; the user's U-turn of that move leaves nobody; a wild one flees a single battle.
- **Dancer** dances a dance that did something again at once, everyone else's with it, the fastest first, at the dance's target where its own ally aimed it at a foe and at the dancer otherwise.
- **Commander** (doubles only): a Tatsugiri beside a Dondozo goes into its mouth (`Elsewhere.Vanished` with its `Vanished` event: out of every move's reach, doing nothing), the Dondozo's five stats rise two stages, neither can leave or be dragged out, and the Tatsugiri comes out once the Dondozo is gone; **Order Up** then raises the Dondozo's Defense for a Droopy Tatsugiri, its Speed for a Stretchy one and its Attack otherwise.
- **Opportunist** copies a foe's stat rise; **Battle Bond** (generation 9) raises a bonded Greninja's Attack, Sp. Atk and Speed for a knockout once a battle with foes still to come; **Power Construct** makes a Zygarde of the cells at half its HP or less its Complete Forme at a turn's end, for the battle, its most HP raised and the difference given (`FormHpChanged`, a new event at `BattleLog.cs`'s end, which the screen takes as it takes a Dynamax's HP); **RKS System** makes Silvally the type of the memory it holds, Normal with none; **Ball Fetch** picks up the first ball of the battle that failed, for a Pokémon of the player's holding nothing (its "field side" is nothing more: neither the games nor Showdown give it a part in wild encounters, so `Overworld/WildEncounterRules.cs` is unchanged); **Mega Sol**'s holder hits as under the sun whatever the sky (`DamageCalculator.SkyFor`).

**Cut down**: Illusion disguises the battle's lines alone (the HP box, the model and the Pokédex show the holder, a wild Pokémon has no party to look like, and the party's order is the roster's, not Showdown's order of switching); Neutralizing Gas doesn't end the strong weathers or Slow Start; Aroma Veil's stop is told before the move lands, where Showdown blocks the condition itself (Psychic Noise's Heal Block isn't covered); Cheek Pouch doesn't answer a berry eaten by Bug Bite or Pluck, and Ripen doesn't double an Enigma Berry's heal (given before it is eaten) or a Jaboca or Rowap Berry's harm (the item's, R28); Wimp Out and Emergency Exit answer a move's hits alone, not poison, the weather, hazards or confusion; Opportunist copies each rise as it is made, where Showdown gathers them to the action's end; a Tatsugiri in command is still asked for its move, which comes to nothing; RKS System's memory doesn't make Multi-Attack its type (R28's items); Mega Sol changes the formula's weather alone, not Solar Beam's charge or what else the sun does for its holder.

**The audit** (R27b): each of R27's 167 against its entry in `abilities.ts`. Set right, each with a test in `LaterAbilities2Tests`: **Supreme Overlord** counts the fallen of its party as it comes in, by Showdown's table of powers (4506, 4915, 5325, 5734, 6144), where R27 counted at each hit and added 410 a faint; **Protosynthesis and Quark Drive** keep the stat they took hold with (`Volatiles.ParadoxBest`), where R27 worked it out afresh at each read; **Sweet Veil and Pastel Veil** reach the allies, and Pastel Veil cures the side's poison as it comes in; **Wind Rider** raises its Attack, and **Wind Power** charges, when a Tailwind starts behind them and (Wind Rider) as it comes in under one; **Rattled** raises its Speed when Intimidated; **Mirror Armor** turns nothing back from a stat already at its lowest; **Poison Puppeteer** is Pecharunt's alone, and only by a move; **Poison Touch** doesn't get past Shield Dust; **Mind's Eye** takes no notice of the target's evasion (R27's cut-down, gone); **Light Metal** halves the weight in whole hectograms; **Sheer Force** counts its user's rise (Flame Charge, Power-Up Punch: a secondary of Showdown's, where a drop is its `self`) and a chance its own code rolls (Snore's flinch) among what it takes away, which the oracle found; "As One" and "Embody Aspect", the data's names, are among the abilities Simple Beam and Entrainment can't replace or hand on. **Disagreements left**, each Showdown's rule still cut down: Analytic asks whether its target has moved, not whether everyone has; Water Bubble and Thermal Exchange keep a burn off but don't cure one their holder already has; Wonder Skin scales the roll; Dazzling and its kin let Perish Song, Flower Shield and Rototiller made faster through; Berserk and Anger Shell look at each hit of a move of several, and don't hold a healing berry back until they have; Cursed Body's Disable lasts the engine's Disable's turns; Magician takes from a Fling and doesn't wait for a Gem; Mycelium Might doesn't pass the target's ability; Guard Dog answers Intimidate alone.

**The oracle**, drawn afresh (`ORACLE=write`, `node oracle.mjs`): the scenarios now meet every later ability; the oracle hands the formula the move as the core shapes it (`BattleCore.ShapedBy`: a -ate ability's type and fifth, Sheer Force) and tells the calculator that Protosynthesis and Quark Drive take hold (`boostedStat: 'auto'`); all 3,000 agree, and `known-gen9.json` stays empty.
**Left for R27b** (23): Unnerve and As One (berries kept from the foes), Illusion, Imposter (a Transform on entry with its cue), Aroma Veil and Flower Veil (their allies), Cheek Pouch and Ripen and Cud Chew (a berry's own course), Symbiosis, Receiver and Power of Alchemy (an ally's item or ability), Wimp Out and Emergency Exit (a switch in the middle of a turn), Battle Bond and Power Construct (forms in battle, R29), Dancer, RKS System (Silvally's memories, R28), Ball Fetch, Neutralizing Gas, Commander (and Order Up's boost), Opportunist, Mega Sol. The oracle's scenarios were drawn before these abilities ran: drawing them afresh (`ORACLE=write`, then `node oracle.mjs` in `tools/CalcOracle`) holds the new bonuses to the calculator.

## The later games' held items, medicine and balls (2026-10-18, plan 06 · R28a)

The held items go by Pokémon Showdown's `data/items.ts` at the pinned commit; Showdown has no catching and no bag outside a battle, so the balls go by the newest main games' own bonuses as they are published, and the medicine by what Platinum's medicine of the same kind does.

- **Hold effects of our own.** PokeAPI says nothing of what a later item does, so `tools/DataImporter/Importer.LaterItems.cs` gives each later item that does something held a hold effect named for its Showdown id (`WeaknessPolicy`, `RockyHelmet`, `GemFire`, `MemoryFairy`, `DriveWater`), with a number where one is needed (a Rocky Helmet's 6, a Gem's 30, a Pixie Plate's and a Fairy Feather's 20), and Fling's power from Showdown. The engine runs them by that name as it runs Platinum's (`Battle/Effects/HeldItemEffects.Later.cs`), so the Pixie Plate is `ArceusFairy` and the Roseli Berry `WeakenSeFairy`, which Platinum's own lines for plates and berries against a type take with no line of their own.
- **In the modern damage formula alone** (`DamageCalculator.Items.cs`): an Eviolite's and an Assault Vest's 6144 on the defence (the vest against special moves; a species that evolves into anything counts as not fully evolved), a Punching Glove's 4506 on a punch, a Gem's 5325, and a Utility Umbrella that keeps the sun's and the rain's bonus off the hits its holder takes (Showdown's `onWeatherModifyDamage` asks the defender only). The Platinum formula has no place for them, and no Pokémon of Platinum's rules holds one.
- **As the move is made**: a Punching Glove takes the touch out of a punch, Protective Pads out of every move of its holder's (the move made without its contact flag, as Long Reach does), so a Rocky Helmet, Rough Skin and the rest don't answer it. *Cut down*: Tough Claws and Unseen Fist, which Showdown reads off the flag itself, lose their touch under the pads too.
- **A Gem** goes off as the move's first hit on a target it reached is worked out (so not on a miss or into Protect), once for every target, and not for a status move, Struggle, a Z-Move, a Max Move or a move of a fixed amount.
- **What answers a hit** (`onDamagingHit`, from `ItemsOnHit` after the target's ability): an Air Balloon pops (but not under a Substitute, which Showdown pops it behind too: cut down); a Rocky Helmet costs a toucher a sixth; a Weakness Policy two stages of Attack and Sp. Atk after a super-effective hit that isn't of a fixed amount, used up whether or not the stages can rise (Showdown's `useItem` doesn't ask); an Absorb Bulb, a Luminous Moss, a Cell Battery and a Snowball a stage after a hit of their type; a Kee Berry after a physical hit and a Maranga Berry after a special one.
- **Once the move is done** (`ItemsAfterTheMove`, before a U-turn's user leaves): Throat Spray after a sound move that didn't fail; Blunder Policy after a miss by the accuracy roll (not a one-hit knockout's, nor a status move's, which never reaches `use.Hits`: cut down); a Mirror Herb takes on the stages its foes gained during the move, all at once; then one Pokémon leaves: an Eject Button's holder (and the attacker's own U-turn is called off, as Showdown's `switchFlag`), else a Red Card's attacker, dragged out at random as by Roar (the card is used up against Ingrain and Suction Cups too, and does nothing when the attacker has nobody to come in, so never against a wild Pokémon), else an Eject Pack's holder after a stage of its fell. *Cut down*: an Eject Pack and a Mirror Herb take note only while a move is under way, so an Intimidate or a Sticky Web as a Pokémon comes in sets off neither.
- **In their rules**: a Clear Amulet keeps every stat drop by another Pokémon off, and says so only when the move was for that alone; Heavy-Duty Boots walk over every hazard and a Poison type on the ground in them still takes the Toxic Spikes up; Safety Goggles keep powders and the sandstorm's and hail's harm off; a Covert Cloak keeps side effects off as Shield Dust does, past Mold Breaker; Room Service lowers Speed once Trick Room is twisted up, or as its holder comes in under it; the Seeds raise Defense or Sp. Def as their terrain comes or their holder comes in on it; an Adrenaline Orb answers an Intimidate, even one an ability stopped, unless its holder's Attack was as low already or its Speed as high (`LaterItems.Intimidated`, from Intimidate's own code); Loaded Dice make a move of two to five hits four or five; a Binding Band makes a bind take a sixth (the binder's band, kept as the bind begins, `Volatiles.BindDivisor`); a Float Stone halves its holder's weight; a Ring Target takes its holder's type immunities away (not its ability's); Light Clay lengthens Aurora Veil (it did since R19b), and a Terrain Extender a terrain (R19b, by name). A Drive gives Techno Blast its type and a Memory Multi-Attack's.
- **Booster Energy** sets Protosynthesis or Quark Drive going as its holder comes in without the sun or the terrain, for as long as it stays in (`Volatiles.BoosterEnergy`). *Cut down*: Showdown also uses it the moment the sun or the terrain ends with its holder on the field; here only as it comes in.
- **Done in R28b** (below, "The last held items"): the Ability Shield, the Rusted Sword and Shield, Booster Energy's later moment, Ogerpon's masks, the Legend Plate and the Blank Plate, an Eject Pack and a Mirror Herb outside a move, Blunder Policy after a status move, an Air Balloon behind a Substitute, Tough Claws and Unseen Fist under Protective Pads, and the Strange Ball. Silvally's form from its Memory is R27b's RKS System's.
- **Medicine**: the later medicine the bag uses in a battle has the use parameters of Platinum's of the same kind, and so works wherever Platinum's does: a Sweet Heart is a Potion's 20 HP; the Casteliacone, Lumiose Galette, Shalour Sable, Big Malasada and Pewter Crunchies a Full Heal; Max Honey a Max Revive; Max Mushrooms the five X items at once. The mints, candies, feathers, mochi, Exp. Candies, Ability Capsule and Patch, nectars and the Dynamax Candy change a Pokémon outside a battle and are R28b's.
- **The balls** (`CatchCalculator.Later.cs`, behind `Ruleset.LaterBalls`, which the modern rules have): Platinum's balls take the newest games' bonuses (a Net Ball 3.5 times, a Dive Ball 3.5, a Repeat Ball 3.5, a Dusk Ball 3, a Quick Ball 5 on the first turn, a Timer Ball 1229/4096 more each turn to 4, which in tenths is 3 a turn, a Nest Ball (41 less the level) tenths below level 30); Johto's are brought in from HeartGold and SoulSilver by name (the importer's `JohtoBalls`; Johto's Heavy Ball takes the name from Legends: Arceus's) and go by Generation 7 on: a Fast Ball 4 times on a base Speed of 100 or more, a Level Ball 2, 4 or 8 times as the player's Pokémon's level is above, twice and four times the target's, a Lure Ball 4 times on a Pokémon hooked on a rod (`BattleConditions.Fishing`), a Moon Ball 4 times on a species that evolves by a Moon Stone, a Love Ball 8 times on its own Pokémon's species and the other gender, a Heavy Ball 20 less on the catch rate under 100 kg, nothing to 200, 20 more to 300 and 30 from there, a Friend Ball's friendship of 200 and a Sport Ball's 1.5; the Dream Ball 4 times on a Pokémon asleep or with Comatose (Generation 8 on); the Beast Ball 5 times on the eleven Ultra Beasts and a tenth on anything else, where every other ball catches an Ultra Beast at a tenth; Hisui's Leaden and Wing Balls 1.5 and Gigaton and Jet Balls 2 (their own game's distance is no battle's), its Origin Ball and the Park Ball always hold. A Heal Ball makes what it caught whole (its HP, condition and PP) and a Luxury Ball's friendship is the field's (both were Platinum's and only now counted). *Cut down*: the catch is still Platinum's own arithmetic (four shakes against a rate of tenths, no critical capture, no Generation 5 rate), and the Strange Ball, which only Legends: Arceus's way of bringing a Pokémon over makes, catches as a Poké Ball.
- **Text**: ten battle lines of our own (`battle.usedItsItem`, `floatsWithItsBalloon`, `balloonPopped`, `gemStrengthened`, `ejectButton`, `ejectPack`, `heldUpRedCard`, `clearAmulet`, `boosterEnergy`, `mirroredStats`), with Romanian beside them; Johto's eight new balls have names and descriptions of our own in both.

## Items used outside a battle (2026-10-18, plan 06 · R28b)

Pokémon Showdown has no bag, so these go by the newest main games' own rules as they are published; where those games disagree the newest wins, as the modern rules do everywhere (R19a). Each is an item of the bag used on one Pokémon of the team, through the bag's ABLE and NOT ABLE (`BagScreen.WouldWorkOn`), and is used up. `Models/LaterItemUse.cs` has the mints, Exp. Candies, Ability Capsule and Patch, Dynamax Candy and nectars, by a field use the importer gives each (`Importer.LaterItems.cs`, `LaterShaping`); the effort items are Platinum's vitamins' kind (`EffortRules`, by use parameters).

- **A mint** (Sword and Shield on) makes the Pokémon's stats grow as the mint's nature's would (`Pokemon.MintNature`, the games' stat nature; `Pokemon.StatNature` is what `RecalculateStats` reads). Its own nature stays what it was for everything else: a Poffin's or a berry's taste, Synchronize, breeding, and the summary, which names the mint's beside it ("Hardy (Adamant mint)"). A mint of the nature its stats already have does nothing; a mint of its own nature takes the last mint away. The Serious Mint is the one neutral mint the games sell. Saved (`SavedPokemonData.MintNature`) and copied by `CopyStateFrom`.
- **An Exp. Candy** (Sword and Shield on) gives its Experience Points: XS 100, S 800, M 3,000, L 10,000, XL 30,000, to level 100 at most, through `GainExp`; the levels it reaches bring their moves and their evolution and show the level's panel, as a Rare Candy does, and like a Rare Candy it gives no friendship of its own (the levels' friendship is a battle's) and a fainted Pokémon comes round with the HP the levels added. Nothing at level 100.
- **An Ability Capsule** swaps a Pokémon's ability for the other of its species' (or form's) two; nothing for a species with one, nor for a Pokémon with its hidden ability (Sword and Shield's and Scarlet and Violet's rule). **An Ability Patch** gives the hidden ability, and on a Pokémon that has it gives back the first of its two (Scarlet and Violet's way back; Sword and Shield's patch had none); nothing for a species with no hidden ability of its own. A hidden ability now stays the hidden one through an evolution and a change of form (`Pokemon.EvolveInto`, `ChangeForm`), where before it fell to the first of the two.
- **Feathers** (Black and White's wings, named feathers since Sword and Shield) give a point of effort, **mochi** (Scarlet and Violet's) ten, each up to the most a stat may hold (252, the rules' `EvStatCap`, not the vitamins' limit: `EffortRules.LimitFor`) and the 510 in all; the **Fresh Start Mochi** takes every point away, and says so (`itemUse.effortReset`). *Our ruling*: neither gives friendship (the games' feathers did a point in Generations 5 to 7; Showdown has none to read).
- **Legends: Arceus's candies** (Health, Mighty, Tough, Smart, Courage and Quick, with L and XL) raise that game's effort levels, which this game hasn't: *our ruling*, an effort level is 25 points of effort, so a candy gives 25, an L 50 and an XL 75, by the same limits as a feather. Smart is Sp. Atk and Courage Sp. Def, as in that game.
- **A Dynamax Candy** raises the Dynamax Level a level to 10 (`Pokemon.DynamaxLevel`, R22's); nothing for Zacian, Zamazenta and Eternatus, who never Dynamax.
- **Oricorio's nectars**: the Red Nectar puts it in its Baile Style (its species' own), the Yellow in Pom-Pom, the Pink in Pa'u and the Purple in Sensu (`ChangeForm`); nothing for any other species, nor for the style it is in. The games have each nectar found in its own island's meadow; it is used up.
- **Hyper training** (`Models/HyperTraining.cs`): a Bottle Cap makes one stat grow as if its IV were 31 and a Gold Bottle Cap all six (`Pokemon.HyperTrained`, a bit to each stat), while the IV itself is left as it was, which is what an Egg inherits and Hidden Power reads. A stat whose IV is 31 already, or that has been trained, can't be. *Ruling*: Scarlet and Violet's level 50 or more (Sun, Moon and Sword and Shield asked for 100), since the modern rules are the newest games'. Nobody in Sinnoh trains: the caps are traded to Mr. Hyper in the later regions, whose chapters (plans 21 to 23) place him and call `HyperTraining.Train`. The two caps are brought in by name (`LaterItemsByName`), with names and descriptions of our own in Romanian.
- **TRs** (Sword and Shield's hundred technical records, `TR00` to `TR99`, brought in to the TMs pocket by name): a TR teaches its move to a species that learned that move by a machine in Sword and Shield, the one game that had them (PokeAPI's `pokemon_moves`, version group 20; `AddTechnicalRecords` puts each species' TRs after its own machines in `TmMoves`, so `MoveTeaching` teaches them as it teaches a TM). A species Sword and Shield left out learns none. *Ruling*: a TR is used up as it teaches, as Sword and Shield's are (the brief that sent this session thought them reusable; it was Sword and Shield's TMs that were). The father's moves a TR teaches go to an Egg as a TM's do (Platinum's rule, which Generation 6 dropped).
- **Evolutions**: Feebas also evolves when traded holding a Prism Scale (Generation 5 on; an importer override of `species.json`, since PokeAPI keeps the row with the later evolutions, which it reads only for species after Platinum's); its beauty is still Sinnoh's way. The Gimmighoul Coins and Meltan Candies that `LevelWithItemsInBag` counts were already run; the coverage counts them now (`Evolution.IsGatheredForEvolution`).
- **The Strange Ball** catches as a Poké Ball (*our ruling*: the games make it only for a Pokémon brought over from Legends: Arceus and never let it be thrown). **Pass Orbs** have nothing to run: they bought the Entralink's powers, of a link this game hasn't.
- **Not yet**: Meowsticite, Magearnite and Tatsugirinite, the Mega Stones of Legends: Z-A, whose Mega forms the data hasn't (plan 03's forms); the catch's own Generation 5 arithmetic and critical captures (the catch is still Platinum's four shakes); a screen to choose the stat a Bottle Cap trains (Mr. Hyper's own, with his chapter).
- **Text**: five lines of the bag of our own (`itemUse.effortReset`, `minted`, `gainedExp`, `abilityChanged`, `dynamaxLevel`), the summary's `summary.minted` and a battle line, `battle.abilityShield`, each with Romanian beside it; the Romanian names and descriptions of the two caps and the hundred TRs are in `names.items.json`.

## The last held items (2026-10-18, plan 06 · R28b)

By Pokémon Showdown's `data/items.ts`, `data/moves.ts` and `sim/battle.ts` at the pinned commit, each through a hold effect of our own (`Importer.LaterItems.cs`); `Battle/Sim/BattleCore.LaterItems2.cs` has most of them.

- **The Ability Shield** (`LaterItems.AbilityShielded`: in force under Klutz, Showdown's `ignoreKlutz`; out of use under an Embargo or in Magic Room): nothing changes its holder's ability, with the line `battle.abilityShield` (Worry Seed, Simple Beam, Entrainment, Role Play on its user, Skill Swap either way, Doodle on each of its user's side, Gastro Acid, Mummy, Lingering Aroma and Wandering Spirit, Receiver and Power of Alchemy), and Trace finds nothing to do, silently (Showdown's `onSetAbility` says nothing for Trace). No breaker of abilities goes past it: Mold Breaker and its kin, and the moves that ignore abilities, meet the shielded Pokémon's ability as if they broke nothing (`LaterItems.Breaks`, and `BattleEffects.Of`, which keeps a shielded holder's ability where a breaker leaves it out). R27b gave it its first half, against Neutralizing Gas. *Cut down*: a Transform still copies a shielded Pokémon's ability (Showdown's `setAbility` there is the transform's own).
- **The Rusted Sword and Shield**: Zacian holding the sword is Zacian-Crowned in a battle and Zamazenta holding the shield Zamazenta-Crowned (Showdown's battle-only formes with their required items), from the moment it comes in (`Crown`, in the shapes taken on entry), and its Iron Head is Behemoth Blade or Behemoth Bash with the PP it had; both are given back as it leaves, as any shape is (`Revert`). Behemoth Blade and Bash were R26b's (twice against a Dynamaxed target).
- **Ogerpon's masks**: Ogerpon wears the mask it holds (`FormRules.ByHeldItem`; its Teal Mask with none), and a mask makes the moves of the Ogerpon whose mask it is a fifth stronger (4915/4096, on the power, in the modern formula alone: `DamageCalculator.Items.cs`). The Tera Types the masks give were R23's.
- **The Legend Plate** (Legends: Arceus's; Showdown has no battle of it): Judgment takes the type that does the most to its first target, the first in the types' order where two do as much (`LegendPlateType`); Arceus keeps its form. *Our ruling*: Arceus's own type is not changed by it, as that game's own battles have no Multitype to change. **The Blank Plate** is Normal's plate (`ArceusNormal`): Arceus stays Normal, its Judgment Normal and its Normal moves a fifth stronger, as the other plates do for theirs.
- **Booster Energy** goes off the moment the sun or the Electric Terrain ends with its holder on the field (`BoostersWake`, as the weather runs out, a strong weather goes, another weather takes the sun's place; `BoosterEnergy.OnTerrainChange`), as Showdown's `onUpdate`, where R28a used it only as its holder came in.
- **An Eject Pack and a Mirror Herb outside a move**: what a Pokémon's coming in does to the stages (an Intimidate, a Sticky Web, a foe's Download or Intrepid Sword) is taken up once everyone has come in and the switch-in checks are done (`ItemsAfterTheEntry`, Showdown's `onAnySwitchIn`): after the battle's opening, after every action and after the fainted are replaced. A Mirror Herb takes on the stages its foe gained; an Eject Pack's holder whose stage fell leaves, its side choosing who comes in. What was noted before an action and taken up by nothing is forgotten as the next action begins. *Cut down*: a drop at a turn's end (Moody, Octolock) is taken up only once the fainted are replaced, and only if someone fainted (Showdown's `onResidual` takes it up at once).
- **Blunder Policy** goes off after a status move missed by its accuracy too (`MoveUse.StatusMissed`), where R28a counted only the damaging moves'.
- **An Air Balloon** pops when its holder's Substitute takes the hit (Showdown's `onAfterSubDamage`).
- **Protective Pads** take the touch out of a move for what answers a touch, but Tough Claws and Unseen Fist, which Showdown reads off the move's own flag, still count it (`TurnFlags.PaddedTouch`). A Punching Glove takes the flag from the punch itself, so Tough Claws doesn't count a gloved punch.
- **Golden battles**: `battles-platinum.txt` held still. `battles-modern.txt` moved in 96 of its 101 blocks, because the modern corpus draws its held items by hold effect from every holdable item and the eight hold effects above joined the draw (an Ability Shield, a Rusted Sword, a Rusted Shield, the three masks and the two plates), which moves every later draw of the corpus.

## Forms changed outside a battle (2026-10-18, plan 06 · R29)

What R19b, R20 to R23, R27b and R28b left of the forms, outside a battle, and the forms an evolution chooses. Pokémon Showdown has no bag, so the rules are the newest games' as published (Serebii for Alcremie's creams; the decompilation for Platinum's meteorites). In a battle every shape was R19b's (Stance Change to Zero to Hero), R27b's (Battle Bond, Power Construct), R23's (Ogerpon, Terapagos) and R28b's (the Crowned swords and shields, Ogerpon's masks); nothing of it changed.

- **The key items** (`Models/FormChanges.cs`, field use `FormChange`, given by the importer to eight items brought in by name: the DNA Splicers, the Reveal Glass, the Prison Bottle, the Zygarde Cube, the N-Solarizer and N-Lunarizer, the Rotom Catalog, the Reins of Unity). Used from the bag's Key Items on a Pokémon (ABLE or NOT ABLE on its card), never used up.
- **Fusions** (Kyurem with Zekrom or Reshiram, Necrozma with Solgaleo or Lunala, Calyrex with Glastrier or Spectrier): the partner must be on the team; it leaves the team and is kept whole inside the other (`Pokemon.FusedWith`, saved as `SavedPokemonData.FusedWith`, left out for one fused with nobody, so no save version moved). Kyurem's Scary Face and Glaciate become Fusion Bolt and Freeze Shock (black) or Fusion Flare and Ice Burn (white) where known, and back on splitting (Black 2 and White 2's). Necrozma learns Sunsteel Strike or Moongeist Beam, Calyrex Glacial Lance or Astral Barrage; with four moves the bag asks which to forget, as for a level's move (**our ruling**: the games make one go, here a no keeps the four). Splitting forgets the move, and one left with none knows Confusion (Sword and Shield's). Splitting needs a free place on the team; the partner comes back beside it.
- **The Reveal Glass** turns Tornadus, Thundurus, Landorus and Enamorus between Incarnate and Therian; **the Prison Bottle** binds and unbinds Hoopa, Hyperspace Hole and Hyperspace Fury turning into each other (no three days' limit: Sword and Shield's); **the Zygarde Cube** turns Zygarde between its 10% and 50% Formes, each keeping its kind of ability (Aura Break or Power Construct), and teaches no moves (**left**: the Cube's moves, R29b).
- **The Rotom Catalog** is turned a page each use, in the order own form, Heat, Wash, Frost, Fan, Mow, back (**stand-in**: the games show the list to choose from; a choice screen is R29b's). The appliance's move (Overheat, Hydro Pump, Blizzard, Air Slash, Leaf Storm) takes the place of the last appliance's, is learned into a free place, or is asked about; back to its own form it forgets it, and one left with no move knows Thunder Shock. It stands in for Platinum's own way, the Secret Key of the Old Chateau's room, which an event gave (plan 02's "What the story still owes").
- **By a held item** (`FormRules.ByHeldItem`, after every battle, on giving an item and at a white-out): Legends: Arceus's Adamant Crystal, Lustrous Globe and Griseous Core hold Dialga, Palkia and Giratina in their Origin Formes, and power them as the orbs do (the orbs' hold effects, `DialgaBoost`, `PalkiaBoost`, `GiratinaBoost`); the Griseous Orb still gives Giratina's Origin Forme too, as Platinum's (**our ruling**: Scarlet and Violet took that from the orb; the story of Platinum is played with it). The Adamant and Lustrous Orbs change nothing. Genesect looks like its drive, and Silvally with RKS System like its memory (its type in battle was R27b's).
- **By a move** (`FormRules.ByMoves`, after `MoveTeaching.Learn` and every battle): Keldeo is in its Resolute Form while it knows Secret Sword.
- **Veilstone City's meteorites** (Platinum's own: `VeilstoneCity_DeoxysMeteoriteSpeed`, `Defense`, `Attack`, `Normal`, the original's scripts 25 to 28): each puts every Deoxys of the team in its forme (`team deoxys`, `ChangeDeoxysForm`) with Deoxys's cry; with none it is only a stone, as before. The lines are our own.
- **The forms an evolution chooses** (`Evolution.Find` hands back the evolution with the form decided, so the scene shows it): Toxtricity is Low Key for the twelve natures Sword and Shield name (its own nature, not a mint's); a Maushold of three and a three-segment Dudunsparce come once in a hundred, when the personality is a multiple of a hundred (Scarlet and Violet's encryption constant, which is the personality here); Alcremie's cream is the spin's way, length and hour, its decoration the sweet held. **A spin is told once it is over** (`SpinTracker`: the player stops turning for 1.2 s, or turns the other way), so Milcery evolves a moment after the player stops, where it evolved on the fourth quarter turn before; a spin is long past five seconds from its first quarter turn to its last, and the Rainbow Swirl wants ten anticlockwise at dusk (Platinum's evening, 17:00 to 19:59, **our ruling**: Sword and Shield's is 19:00 to 19:59). Dusk is day for the other creams.
- **Golden files**: `battles-platinum.txt` held still. `battles-modern.txt` moved in block 151 alone: its Scrafty draws the Griseous Core where it drew the Griseous Orb, since the corpus draws held items by hold effect and the core shares the orb's, and the rest of the foe's team is drawn after it. `scripts.txt` moved in Veilstone City's meteorites (four scripts in place of one).
- **Left for R29b**: Furfrou's trims (a groomer of a later region), the Zygarde Cube's moves and Zygarde's cells, a choice screen for the Rotom Catalog, Hoopa's and Shaymin's going back in a box (Shaymin's is Platinum's and done; Hoopa's is the old games' and not Sword and Shield's), Necrozma-Ultra outside Ultra Burst (none), Magearna's and the Pokémon whose forms are only looks given by events, Zarude's Dada (an event), the names the data gives some forms otherwise than Showdown does (above, "Found on the way"); and the oracle, which knows none of these forms by Showdown's names.
## Unova as Black 2's maps make it (2026-10-20, plan 22 · U2)

Unova is read from [fuddlesworth/pokebw2](https://github.com/fuddlesworth/pokebw2) at the pinned commit
(`tools/MapImporter/Bw2/`, `--game black2 --data`), as Johto is read from HeartGold's decompilation. Only numbers and
names come into the repository. Aspertia City with its rooms and gate, Route 19, Floccesy Town with its rooms, Route 20
and Floccesy Ranch are open; nothing of the story is (U5). What follows is what this game decides where the source
leaves a choice.

- **Black 2's version.** Wherever a table differs between the two versions (an encounter table's `black2` and `white2`
  halves), Black 2's is read (plan 22's decision 4). White 2's species come by trade or plan 03 · D12's zone.
- **The flags and variables carry the region's name.** The decompilation names most of its flags only by value
  (`EVENT_FLAG_0x02e9`). Ours are `FLAG_UNOVA_0x02e9` and `VAR_UNOVA_0x40a1`, and a flag the decompilation has named keeps
  its name after the prefix (`Bw2Events.Flag`, `Var`). So no flag of Unova's is ever another region's, and none takes a
  placeholder's name as if it were a meaning. Black 2 gives an item ball with no flag of its own one by its script's
  number (`FLAG_UNOVA_` and 0x4000 plus the script), so a ball once taken stays taken. A hidden item takes the flag its
  global script sets (`0x8002`), or 0x0b04 plus its number where it sets none.
- **The keys.** An area's key is the zone's own name in lower case (`aspertia_city`, `floccesy_ranch`). A route, Victory
  Road and the League take the region's name (`unova_route_19`), since Kanto's routes have the same numbers and a script
  file is found by its key alone. A trainer's id is the region's prefix and the constant (`unova_lass_molly`),
  found by `TrainerDatabase.Get(constant, "unova")`.
- **Movements.** Black 2's movement codes are read as the nearest of Platinum's words (`Bw2Events.Movement`: look about,
  wander, wander along one line, look two ways). A code that walks a path of its own, or that a script drives, stands
  still until the session that opens it. People and things on a rail (a bridge's or a cave's path that the camera
  follows) have no tile and are left out.
- **Trainers.** A trainer's team, items and AI routines are Black 2's. Each Pokémon's personality is worked out as
  HeartGold's `TrainerData_BuildParty` works it, since the source does not port Black 2's own. The prize money is the
  last Pokémon's level times four times the class's rate, twice that for a double battle. A triple battle and a
  rotation battle are fought as single ones until plan 14 gives them their formats. The trainers' lines (`messages`)
  are never read. Every trainer's two lines are their overlay's, in our own words. A Pokémon's form in a trainer's team
  is not read yet. A class the source names only by value falls back to its constant's words ("Worker 44"); none stands
  in an open area.
- **Encounter rates.** A zone gives its grass, dark grass and water a rate setting, a number into twelve rows of how the
  chance climbs (`ENCOUNT_RATE_CONFIGS`). Until plan 22 · U4 ports the climb, an area's rate is the row's largest number
  (`Bw2Encounters.Rate`: 13 for the first row, 19 for the second). The rods' slots are met 60, 30, 5, 4 and 1 times in a
  hundred, as the water's are.
- **Seasons.** An area whose Pokémon change with the season keeps spring's tables as its own and each other season that
  differs, whole, in `WorldAreaFile.Seasons`. Nothing reads them until U4's calendar of seasons: until then every game
  meets spring's. The dark grass, the shaking grass and the rippling water are read and kept beside them, and U4 gives
  them their rules. Unova alone has seasons (`RegionFile.HasSeasons`).
- **The seasons' chunks** (plan 22 · U2, part b). Black 2 lays 26 chunks of its own in place of others in a season
  (Route 20's four in autumn, six cells of the north-west in winter, two cells of the east coast in each of summer,
  autumn and winter, and Twist Mountain's and the Moor of Icirrus's floors in winter and Pledge Grove's in autumn). Its table of the
  swaps (`map_replace.c`'s archive) is not extracted by the decompilation, so the reader takes the swap from the names,
  which say it: `winter05_04` lies over the overworld's `overworld_2_map05_04` (column 5, row 4) and `winter0701_01_00`
  over the cave's `…_m_dun0701_01_00`. The matrix's own chunks are spring's, and `WorldMatrixFile.Seasons` names the
  other seasons' where they differ; a season's chunk keeps its spring chunk's warps. Nothing reads them until U4.
- **Trainers' teams** (part b). A Pokémon of a form (`form` above 0: two Basculin, a Wormadam and Colress's Rotom) is
  that form by Generation 5's numbering (`Bw2World.FormName`). A class number the decompilation gives only to tell two
  classes of one name apart (`TRAINER_CLASS_WORKER_44`) is not part of the class's name. A team's `gender` and
  `ability` are not read yet: the personality is worked out as HeartGold's, which gives each its own.
- **Where the textures lie.** By the user's decision of 2026-10-20, each chunk's `model.glb` is read past its JSON chunk
  into its binary one, as plain numbers, only to find which texture covers each tile (`Bw2Geometry`). Only a
  `TerrainCover` code a tile is kept. `Bw2Cover` sorts the names, buildings take the tiles under their props' boxes, and
  the report lists the textures no rule sorts by the tiles they lie over. 82.3% of the overworld's tiles are sorted. No
  vertex, texture, palette, image, sound or text of the game is written into the repository.
- **The rooms** are `Indoors` matrices, several to a matrix as Johto's are, each a map of its own told apart by its
  area. A room's props are numbered apart from the open country's, since the two are two tables of the source.
- **The new game.** A game begun with `--region Unova` wakes in the player's room in Aspertia City (14, 5). Unova has
  no story yet (`HasStory` false), so the game starts as a region without one does: with Snivy, the record's first
  starter, at level 5, and a few items, until U5's professor hands the starters out. The player's mother is ours: she stands at the table and
  sees to the team as a nurse does, until U5 writes her scene. A Champion who comes over steps out before the house
  (47, 762).
- **The people left out.** Whoever a flag hides at the start of the original's story is left out of the overlays until
  their scene is written, since every flag is clear in a new game and they would stand there. That includes the rival,
  the professor's assistant, Team Plasma, and the Pokémon Rangers of Route 20, who wait for a later chapter at level 32.
  Professor Juniper's and Hugh's looks are in `characters.json` for U5.
- **The music** is Sinnoh's and the common songs standing in until plan 05 gives Unova its own: Twinleaf Town's for
  Aspertia City, Floaroma Town's for Floccesy Town, Route 201's for the routes and the gate, Route 205's for the ranch's
  field, and the common Pokémon Center's and Gym's.
- **The buildings** are stand-ins in Sinnoh's ways of building (`WorldModels.Unova`, each with its `StandInUntil`),
  until U5 gives each town a way of its own.
- **Wave 48's fixes** (2026-10-25, from the wave 47 shots). Black 2's `yuk` after a number (`out7yuk01`, `out36yuk1`) is
  a floor's, Nimbasa's and its park's paving: read as snow (`yuki`), it had made the city snow country (blue light, a
  snowfield for every battle on it). A zone's `battle_bg` is read by the zones that name each: 1 a route's plain, 2 the
  big cities' street (Castelia, Nimbasa, Driftveil and its drawbridge, Route 4), 3 a town's, 4 and 5 caves, 6 woods,
  7 mountains, 8 the shore and the ruins under the sea, 9 indoors, 10 the desert; 2 and 3 are both `City`, as
  Platinum's towns are. Black 2 fights a city's battles on a street, so **paving where the area's header names a city's
  background is a stage of ours, `City`** (flagstones under a row of buildings), in every region; a city's earth or grass
  stays the meadow (Driftveil's sandy streets, where Rood is battled). The Castelia sewers' battles are fought on the
  cave's stage (their overlay's `battleArena`) until a sewer has a stage of its own. Join Avenue, a covered street of
  shops under a glass roof, is a room of its own look (`InteriorStyle.Street`): its street is paving, its shopfronts
  the blocks between the lanes; its shops are still part c's. The Nimbasa Gym's runway stands over nothing, as Black
  2's does in its dark hall (the overlay's `overTheDark`); Black 2's spotlights and the models' walk are still left out.

## Seasons in Unova (2026-10-24, plan 22 · U4)

What a season does beside the field's look (U3). Only a region whose record has `hasSeasons` has a season
(`GameEngine.SeasonHere`), so nothing of this reaches Sinnoh, Kanto, Johto or Hoenn: their matrices name no season's
chunks, their areas no season's tables, and a coat is never changed on their maps.

- **The layouts.** Black 2 swaps a season's chunks into the matrix (`map_replace.c`); here each map of a matrix with
  `seasons` is built once more round each season's cells (`World.SeasonalSwaps`, `WorldMapBuilder.BuildSwapped`) and
  keeps both grounds, as a swap of the story's does (`Map.AddSeasonalPlace`, a `SwappedPlace` with a `Season`). The
  engine lays the field's season on every map whenever it takes or turns one (`GameEngine.LayTheSeason`,
  `Map.ApplySeason`): as a game is begun or taken up, when the options fix a season, and behind the next fade once the
  calendar has turned, so the ground never changes under the player's eyes. Spring's chunks are the matrix's own. A
  cell whose season's chunk the importer hasn't written (an area not open yet) keeps spring's; a cell whose buildings
  differ between the two is kept too, since a swap carries no building, while one whose buildings are the same (the
  gate of Floccesy Ranch on Route 20) changes its ground alone. The story's own swaps never move a season's chunks.
  Today that is Route 20's four autumn cells, which open some two hundred tiles spring's trees shut; Icirrus City's frozen marsh and
  the drifts of Route 7 come with their areas (U10).
- **The wild Pokémon.** An area's own tables are spring's, and a season after it that differs has its tables whole
  (`MapArea.SeasonTables`, `TablesFor`). The moment carries the season (`EncounterMoment.Season`, null outside Unova),
  and the grass, the water and the Super Rod are met from the season's tables. The Old and Good Rods are the area's all
  year (Black 2 has one rod).
- **The dark grass** (`DarkGrass`, `VeryTallDarkGrass`) is grass with its own table and rate (`WorldAreaFile.DarkGrass`),
  met in each season; the very tall kind is thicker, as tall grass is, and stops a Bicycle. A Pokémon met there comes
  with a second one time in ten (`Map.DarkGrassPairChance`) to a player with two able to fight, as a wild double battle
  (the core's, as a partner's grass brings); a second the lead scares off leaves the first alone. **The one in ten is
  our own number**, until a session reads Black 2's from the decompilation's field code. Nothing is drawn off the dark
  grass, so no other place's chance moves. The dark grass is drawn as tall grass until its look is written (the style
  guide's "Grass, flowers, trees, ledges and rocks").
- **Not yet**: the shaking grass's, the rippling water's and its rod's tables are read into each area and met by
  nothing, until plan 10 · F12's patches; the habitats' seasons and the Pokédex's AREA page line for them.
- **Deerling and Sawsbuck** (`Models/Seasons.cs`, `SeasonalForms`) wear the season: spring is the species itself, the
  other three its coats (`Deerling-Summer`…). The team's are put in the season's coat as a game is begun or taken up,
  as the season turns or is fixed, and as the PC is closed (one taken out of a box), and a wild one is met in it; only
  where `SeasonHere` is a season, so a coat taken out of Unova stays what it is, as the later games keep it. An
  evolution carries the coat over (the data's `fromForm` and `targetForm`), and the save keeps it as any form.

## Unova's first chapter (2026-10-24, plan 22 · U5, part a)

Black 2's story from the player's room to Route 20's end, in our own words on the beats of the decompilation's
scripts (`res/field/scripts/aspertia_city*.s`, `route_19.s`, `floccesy_*.s`, `route_20.s`), read for the order of the
scenes, who stands where, the battles and the gifts, and nothing of their text.

- **A new game in Unova begins in the player's room with nothing** (`unova.json`'s `hasStory`): Mom asks her
  questions at the foot of the stairs, Hugh and his sister meet the player at their door, Bianca waits at the outlook
  with the three Pokémon and the Pokédex, Hugh's first battle follows (`canlose`, as Black 2's), then the Pokémon
  Center's tour and ten Poké Balls, Mom's Running Shoes and the sister's Town Map outside it, the gate's Potion, Bianca's
  catching lesson on Route 19 (her Munna and a Patrat), Alder on Route 19 and in Floccesy Town, Hugh's battle at the
  ranch and the Town Map handed over, the owners' lost Herdier found in the grove beside Team Plasma's first grunt, who
  runs and leaves TM21 behind, Alder's lesson at home (two of the school's pupils, Seymour's monkey weak to the starter
  and Cassie's strong, then five Oran Berries), Cheren's Gym (Pedro and Serena first, then `unova_leader_cheren`, the
  Basic Badge and TM83), Bianca's TM27 and Cheren taking Hugh's challenge outside the school, the hiker on Route 20 who
  wants a Badge, and Cheren's dark-grass advice and three Pecha Berries. Hugh's teams are `unova_rival`, `_2` and `_3`
  (then `_4` to `_6`) by the starter: his is the one strong against it.
- **Our tellings where Black 2's are another's.** Hugh doesn't follow the player round town (Black 2's follower walk):
  he looks on his own side and catches up at the outlook's steps. The Town Map is one item; the second is a flag of
  ours (`FLAG_UNOVA_HUGHS_TOWN_MAP`) until it is handed over. The C-Gear, the Xtransceiver's numbers, the nickname
  question and Bianca's Habitat List are left out (no such device or screen yet). Cheren's TM83 is our data's TM83
  (Platinum's table, Natural Gift), not Black 2's Work Up: the item table is Platinum's. The gate's guard turns back a
  player with no Pokémon in the gate house as well as the town's own row (`VAR_UNOVA_ASPERTIA` 1).
- **Flags and variables.** The decompilation's are by value (`EVENT_FLAG_0x02e9`), which the importer writes as
  `FLAG_UNOVA_0x02e9` and no script may name (a flag's name is capitals). So each story person's overlay gives a flag
  of ours in place of the import's, and the scripts name variables of ours on triggers of the overlay's own tiles:
  `VAR_UNOVA_HOUSE` is Black 2's `0x40a0`, `_ASPERTIA` `0x40a1`, `_OUTLOOK` `0x40a4`, `_ROUTE_19` `0x40a3`, `_FLOCCESY`
  `0x40a5`, `_RANCH` `0x40a7`, `_ROUTE_20` `0x40ab`, `_GATE` `0x40e0`, each counted as Black 2 counts it where the
  beats are the same; `FLAG_UNOVA_HIDE_MOM_HOME` is `0x02e4`, `_HUGH_ASPERTIA` `0x02e9`, `_MOM_ASPERTIA` `0x02e7`,
  `_BIANCA_ASPERTIA` `0x02e6`, `_HUGHS_SISTER` `0x02e8`, `_CHEREN_ASPERTIA` `0x02e5`, `_BIANCA_CENTER` `0x02dc`,
  `_BIANCA_ROUTE_19` `0x03db`, `_ALDER_ROUTE_19` `0x02e2`, `_ALDER_FLOCCESY` `0x02db`, `_HUGH_RANCH` `0x02d9`,
  `_RANCH_OWNERS` `0x02d7`, `_RANCH_GRUNT` `0x02d8`, `_HUGH_ROUTE_20` `0x02bd`, `_CHEREN_ROUTE_20` `0x02bf`.
- **What a game starts with** is `common.UnovaNewGame` (run for every new game); `FLAG_UNOVA_STORY_BEGUN` says it has
  run. A save made before the story was written is brought up to date by the chapter's arrival scripts
  (`common.UnovaCatchUp`), with no story version raised: one whose game began in Unova with a team (plan 22 · U2's) is
  taken past the opening (`common.UnovaOpeningDone`), so nobody hands it a second starter; a Champion who crossed in
  plays the opening all the same, naming Hugh at the door (`namerival` where `CROSSED_IN`).
- **Hugh is the rival**: Black 2's class "Pokémon Trainer Rival" counts as the rival's (`TrainerDatabase.IsRival`), so
  he is called by the name the player gave him and fights to the rival's song.
- **The places' looks**: Aspertia's houses and school are `Architecture.Hillside`, Floccesy's houses and bell tower
  `Bellfield` (the style guide's rows); the Pokémon Center and the gate house are every region's. Virbank's two, in view
  from Route 20's end, stay stand-ins for part b.
- **Songs** of our own in `Data/music/unova/`: `aspertia`, `route19` (also Route 20, the ranch and the gate), `floccesy`,
  `gym`, the four battles, two victories and two eye themes; the rest of the roles fall back to `common`.

## Unova's first chapter, part b (2026-10-24, plan 22 · U5, part b)

Black 2's Virbank chapter, from Route 20's end to the ship terminal, in our own words on the beats of the
decompilation's scripts (`res/field/scripts/virbank_*.s`, `route_20.s`, `pokestar_studios.s`), read for the order of
the scenes, who stands where, the battles and the gifts, and nothing of their text.

- **The chapter**: the gate's guard gives two Great Balls; Roxie and her father, the captain of Virbank's ship, argue
  in the street as the player comes in, and he leaves for Pokéstar Studios; the studios' man turns the player back at
  the north gate until the Toxic Badge; the Gym's guide gives Fresh Water; Roxie can't hear a challenger over her band
  until Nicky and Billy Jo are beaten, then battles (`unova_leader_roxie`) for the Toxic Badge and TM09; on the way out
  the studios' scout stops the player and Roxie runs to fetch her father; Team Plasma's three grunts face Roxie and Hugh
  on the dock, the player beats the one in charge (`unova_team_plasma_grunt_44`), the grunts flee and Roxie gives HM01;
  Hugh searches Route 20, where the grunt who ran that way is beaten in the dark grass (`unova_team_plasma_grunt_48`)
  and leaves for the sea, and Hugh guesses Team Plasma moves by boat; the captain, back from his screen test, takes up
  his ship again, and he and Hugh wait in the ship terminal.
- **Our tellings where Black 2's are another's.** Pokéstar Studios is a door (plan 22's decision 7): Black 2 plays the
  studios' first visit and the captain's audition between the Badge and the dock, ours tells of it in the scout's and
  Roxie's words. The Xtransceiver's call as the player comes in is left out. The grunts on the dock flee east along the
  quay, where Black 2 runs them west through the town past the player. The terminal's captain stands at the top of the
  hall: Black 2's tile is cut away with the berth behind it. The voyage to Castelia City is part c's (Castelia's streets
  stand on rails the importer doesn't read yet): until then the captain is still warming his engines. TM09 is our data's
  TM09 (Platinum's table). The Poké Toy's item ball is held back: our item table has no such item. The complex's three
  workers battle when spoken to, without waiting for the foreman to ask.
- **Flags and variables**, ours where Black 2's are by value: `VAR_UNOVA_VIRBANK` is `0x40ac` (0, 2 to 8 as Black 2
  counts them, its 1 being the call), `_VIRBANK_GYM` `0x40aa`, `_VIRBANK_GATE` `0x4151`, `_COMPLEX_TALK` `0x40dd`, and
  `VAR_UNOVA_ROUTE_20` goes on to 5 and 6 (Black 2's 4 unused); `FLAG_UNOVA_HIDE_ROXIE_AND_CAPTAIN` is `0x02d6`,
  `_POKESTAR_GUARD` `0x02d5`, `_VIRBANK_DOCK` `0x02d4`, `_ROXIE_GYM` Roxie's stage self once she has gone,
  `_ROXIE_SCENE` `0x02d1`, `_VIRBANK_SCOUT` `0x02d0`, `_ROUTE_20_GRUNT` `0x02be`, `_CAPTAIN_TERMINAL` `0x02d3`,
  `_HUGH_TERMINAL` `0x02d2`; `FLAG_DAILY_UNOVA_BALL_SWAP` is the complex's daily swap. `common.UnovaVirbank` sets what
  a game starts with for the chapter (`FLAG_UNOVA_VIRBANK_BEGUN` says it has run), from `UnovaNewGame` and, for a save
  of part a's, from `UnovaCatchUp`, with no story version raised.
- **Team Plasma** brings the Galactic eye and battle roles by class (`sound-map.json`) and, in Unova, a song of its
  own as its eye song (`unova/eye_plasma`); the dock's speech plays it too. Roxie's battle is the Gym Leader's.
- **The places' looks**: Virbank's houses, its long house by the water and the club's way down are
  `Architecture.Dockyard`; the club is a Gym in Poison's colour; the ship at the quay is a boat, the complex's tanks,
  stacks and towers headframes. An opening walked into in Unova (a gate's on an east-west road, a door in a room's back
  row) is walked into whatever its tile's cover says (`World.PlaceEvents`).
- **Songs** of our own: `unova/virbank` (the town, its rooms, gates and the complex) and `unova/eye_plasma`.

## Unova's first chapter, part c (2026-10-24, plan 22 · U5, part c)

Black 2's Castelia chapter, from the voyage out of Virbank to Colress on Route 4, in our own words on the beats of the
decompilation's scripts (`res/field/scripts/virbank_city_5.s`, `castelia_*.s`, `route_4.s`), read for the order of the
scenes, who stands where, the battles and the gifts, and nothing of their text.

- **The chapter**: Hugh in the ship terminal asks whether the player is coming, and the captain takes them both across
  the bay (a line over the dark and the ship's horn, as the crossing's journey is told); ashore at Prime Pier Hugh sees a
  ship with black sails leave the bay and runs off; on the avenue a Harlequin welcomes a first-time visitor with a
  Bicycle; at the Gym's door Clyde, its guide, says Burgh has gone off after some trouble, and Iris, looking for Burgh,
  hears the player is after Team Plasma and runs to help; on the avenue she calls the player on to Thumb Pier, where
  Hugh comes running and goes down into the sewers; inside them Hugh heals the team and comes along, and the two beat
  Team Plasma's grunts side by side (`unova_team_plasma_grunt` and `_3`, Hugh as `unova_rival_13`, `_14` or `_15` by the
  player's starter); the grunts flee, Burgh comes along the passage, Hugh hands over HM04 and goes after the rest of
  Team Plasma, a stranger in a white coat praises the battle and slips away, and Burgh goes back to his Gym; Iris, back on
  Thumb Pier, sends the player to the Gym, whose door Clyde leaves; Burgh's battle (`unova_leader_burgh`) gives the
  Insect Badge and TM76; on the plaza the stranger looks over the team, names himself Colress and goes to wait on Route
  4, where he wakes the row of Crustle everyone took for boulders, battles the player (`unova_colress`), gives a Protein
  and goes on north.
- **Castelia's avenue and the street across its north stand on Black 2's rails.** Black 2 lays both out on rails, whose
  data the decompilation keeps outside its text, and puts every event there by a place along a rail; the reader leaves
  them out (as it did before). So the avenue along the bay is a map of ours, `CasteliaCity` (`Data/maps/CasteliaCity.json`,
  92 by 14): a road between a row of towers and the bay, with the four streets' mouths, the Pokémon Center's door and
  the two open piers' mouths in the order Black 2's rails have them (Thumb Pier, Castelia Street over the closed Cruise
  Dock, the Pokémon Center, Mode Street over Prime Pier, Narrow Street over the closed Unity Pier, Gym Street over the
  closed Liberty Pier); its west end, where the gate to the Skyarrow Bridge stands, is shut. The street across the north
  is left out: each street's north end opens straight onto the plaza's rim (exits of ours on its west, south-west,
  south-east and east edges). Every tile of a way that leads onto a rail is a door of ours (an overlay's `doors`) to the
  map of ours or the plaza's rim. The avenue's people, signs and the scenes there (the Harlequin, Iris) are ours, on the
  beats of `castelia_city.s`.
- **Castelia's towers** are drawn into each street's own model with no prop of their own: the reader writes a prop of
  ours, `c3_tower`, for each rectangle of the blocked tiles their textures lie over (`Bw2Cover.Towers`, at most ten
  tiles across, its height the tallest roof's), and on the five streets for every blocked tile of plain ground, since
  Black 2 draws only the faces the camera sees; a street's two chunks each model part of the other's, so a Castelia
  chunk is read with its matrix's other chunks moved into its frame (`WithNeighbours`). The streets lie half a tile
  under the walking surface their terrain grids give, so a tile there with nothing on or over it takes the look of
  what lies under it (judged on Castelia's chunks alone: the areas opened before keep the look their sessions saw). The
  towers are `Architecture.Metropolis`'s, a skyscraper each (the style guide's row).
- **The Gym**: Black 2 hides its three Harlequins in cocoons that burst as the player pushes through the honey, and
  reaches Burgh's own room through the honey by a script's warp; the room is cut to what its ways in reach, so Burgh's
  room has nothing. The honey's puzzle is left for later: the Harlequins stand in the hall (two moved off the walls
  their cocoons hang over), and Burgh waits at the head of it (a person of ours, `FLAG_UNOVA_HIDE_BURGH_GYM`, shown once
  he is back from the sewers). The hall is Bug's (`arenaType`), the Gym's building Bug's colour. TM76 is our data's TM76
  (Platinum's table), as Roxie's TM09.
- **Left out, for later sessions**: the Castelia Harlequin Hunt (its medals are plan 13 · V9's; its Bicycle is the beat
  kept, and a Champion who brought one of their own is told so); Mode Street's ice-cream stand and its daily sale (its
  queue stands); the towers' rooms, the Cruise Dock, Unity and Liberty Piers, the Skyarrow Bridge's gate, the sewers'
  other rooms and the Relic Passage; Black 2's girl in the sewers who drops something; Route 4's trainers, houses, works,
  Join Avenue and the desert (plan 22 · U6's: the route is open for Colress's scene with only its listed people on it).
  Castelia's Pokémon Center is no spawn of the region yet (Virbank's isn't either), so a white-out in Castelia wakes in
  the last one the record names.
- **Flags and variables**, ours where Black 2's are by value: `VAR_UNOVA_CASTELIA` is the city's one count (Black 2's
  `0x40ae` 1 and 2, `0x40e2` 1 and 2, `0x40b1` 1 to 3, `0x40b2` 1 to 4, `0x40b3`, `0x40b4` 1 and 2, `0x40b6` 1 to 3 in
  one line: 1 on the voyage, 2 ashore, 3 the Bicycle, 4 Iris on Gym Street, 5 Iris on the avenue, 6 Hugh down the
  sewers, 7 Hugh along, 8 the sewers' scene, 9 Iris's word, 10 the Insect Badge, 11 Colress on the plaza, 12 his
  battle); `FLAG_UNOVA_HIDE_HUGH_PIER` is `0x02ec`, `_HUGH_THUMB` `0x02f2`, `_HUGH_SEWERS` `0x02f3`, `_IRIS_AVENUE`
  `0x02ef`, `_IRIS_PIER` `0x02f1`, `_IRIS_GYM_STREET` and `_BURGH_GYM` ours, `_SEWER_GRUNTS`, `_SEWERS_BURGH` and
  `_SEWERS_COLRESS` `0x02f7`, `_CLYDE_STREET` `0x02f0`, `_COLRESS_PLAZA` `0x02f4`, `_COLRESS_ROUTE_4` `0x02f6`,
  `_CRUSTLE_ROUTE_4` `0x02f5`; `FLAG_UNOVA_GOT_WATER_CASTELIA` and `_GOT_TM76` are the gifts'. `common.UnovaCastelia`
  hides the chapter's people (`FLAG_UNOVA_CASTELIA_BEGUN`), run by `UnovaVirbank` and, for a save of part b's, by
  `UnovaCatchUp`, with no story version raised.
- **Songs**: `unova/castelia` (the city, its gate and Prime Pier's and Thumb Pier's waits); the sewers keep Sinnoh's
  `cave` and Route 4 `unova/route19` until their own. Burgh's and Colress's battles are the Gym Leader's.

## Unova's second chapter, part a: the desert to Nimbasa (2026-10-25, plan 22 · U6)

Black 2's road from Route 4 to the Bolt Badge, in our own words on the beats of the decompilation's scripts
(`res/field/scripts/route_4*.s`, `desert_resort*.s`, `route_gate.s`, `join_avenue.s`, `nimbasa_city.s`,
`nimbasa_city_gym.s`), read for the order of the scenes, who stands where, the battles and the gifts, and nothing of
their text.

- **The chapter**: Route 4's trainers (its two fishermen, the Hooligans, three policemen, a biker, a youngster, two
  roughnecks and a Breeder) stand on the road, and its five houses of flats are open, the rest house's woman seeing to the
  team; the Desert Resort's edge, its gate house and the desert beyond lie in their sandstorm, the desert's archaeologist
  giving Soft Sand; Join Avenue is a street of empty shopfronts between Route 4 and Nimbasa City; in Nimbasa, before the
  Gear Station, the Subway Bosses Ingo and Emmet battle two on two (`unova_subway_boss_ingo` and `_emmet`) beside the
  hero the player isn't, Rosa for a boy and Nate for a girl, whose team is the starter neither the player nor Hugh took
  (`unova_rosa`, `_2`, `_3` and `unova_nate`, `_2`, `_3` for Snivy, Tepig and Oshawott, Black 2's `0x4030`), and who
  gives a Vs. Recorder; in the amusement park the Nimbasa Gym's guide gives Fresh Water, three models step down the
  runway to battle in turn (`unova_beauty_nikola`, `_fleming`, `_amp_re`), and Elesa (`unova_leader_elesa`) gives the
  Bolt Badge and TM72; back in the city Hugh faces four of Team Plasma on the street west to Route 5, the player beats
  two (`unova_team_plasma_grunt_4` and `_47`), they flee, and Hugh tells of his sister's Purrloin, heals the team and
  goes on.
- **Our tellings where Black 2's are another's.** The Gym's runway is three triggers of ours, each model stepping down
  three tiles to the challenger and back, where Black 2 walks each down the whole catwalk and plays the lights; the
  models' walk out with the player afterwards is left out. Black 2's guide stops the challenger at the door; ours is
  spoken to. Hugh's scene is one scene: Black 2 has the player speak to each grunt after Hugh's "Cover me!"; ours plays
  both battles straight after it, the grunts flee west along the street and Hugh east. The young Trainer's battle is
  begun by speaking to them, as Black 2's. Join Avenue's owner, who asks the first visitor to manage the avenue and fill
  it with shops, only talks: the avenue's shops are left. The daily gifts and games of Route 4's houses (the fast
  Pokémon's reward, the Cottonee for Petilil trade, the waitress's cry quiz) only talk of themselves. The Braviary that
  lands on Route 4 on Thursdays, the Anville delivery man and his Pokémon, the Battle Institute's girl and the Gym
  guide at the park's door are left out. TM72 is our data's TM72 (Platinum's table), as Burgh's TM76.
- **The places**: Nimbasa builds in a way of its own (`Architecture.Funfair`, the style guide's row); its stadiums,
  theater and Ferris wheel are halls (the wheel a hall until it is drawn as one, part b), and the park's two archways
  (`c4_gate`) are ground walked under, since a gate house there shut the Gym away. Route 4's houses of flats are
  Castelia's panel blocks. A sign of Black 2's second condition (1) is read outdoors (Nimbasa's, the desert's, Narrow
  Street's café); indoors it is a gate house's news board, which the reader leaves out. Black 2's weathers 3 and 12 are
  the sandstorm (the desert's edge and the desert).
- **Left for part b**: Routes 5 and 16 beyond the city's gates, the Lostlorn Forest, Anville Town and the Gear Station
  with the Battle Subway's desks, the Musical Theater, the stadiums, Nimbasa's houses, the Ferris wheel's scene, the
  Relic Castle and the Relic Passage, the Drawbridge, Driftveil City with Clay and the Quake Badge, the World Tournament
  and the Plasma Frigate.
- **Flags and variables**, ours where Black 2's are by value: `VAR_UNOVA_NIMBASA` is the city's count (Black 2's
  `0x40c0` 1 and 4 in one line: 1 once the Bolt Badge is won, 2 once Hugh's scene is played), `VAR_UNOVA_NIMBASA_GYM`
  the runway's (`0x409b`, 0 to 3); `FLAG_UNOVA_HIDE_HUGH_NIMBASA` is `0x02f9`, `_NIMBASA_PLASMA` `0x02fa`,
  `_SUBWAY_BOSSES` `0x02fc`, `_ROSA_NIMBASA` and `_NATE_NIMBASA` ours (Black 2 has one object, dressed by the player's
  look); `FLAG_UNOVA_GOT_WATER_NIMBASA`, `_GOT_TM72` and `_GOT_SOFT_SAND` are the gifts'. `common.UnovaNimbasa` hides
  the chapter's people (`FLAG_UNOVA_NIMBASA_BEGUN`), run by `UnovaCastelia` and, for a save of part c's, by
  `UnovaCatchUp`, with no story version raised; Nimbasa's arrival shows Rosa or Nate.
- **Songs** of our own: `unova/route4` (the route, its houses, the desert's edge and its gate house), `unova/desert_resort`
  and `unova/nimbasa` (the city, the park and Join Avenue). Elesa's and the Subway Bosses'
  battles are the Gym Leader's.

## Unova's second chapter, part b: Nimbasa's rooms to the Quake Badge (2026-10-25, plan 22 · U6, part b)

Black 2's road from Nimbasa's rooms over Route 5 and the Driftveil Drawbridge to Clay's Quake Badge, in our own words on
the beats of the decompilation's scripts (`res/field/scripts/nimbasa_city*.s`, `route_5.s`, `driftveil_*.s`), read for
the order of the scenes, who stands where, the battles and the gifts, and nothing of their text.

- **Nimbasa's rooms**: the Big Stadium's and the Small Court's lobbies and their halls upstairs, two houses of two floors,
  the Battle Institute, the Musical Theater's lobby and the old roller coaster in the park (Elesa's Gym before she moved
  it to the runway) are open, their people only talking. Left out: the stadiums' fields and the games played at set times
  (Black 2's `0x400f`) with their crowds, the Battle Institute's tests (the link's: the receptionist says the link is
  closed, decision 06 · 7), the Pokémon Musical itself, the roller coaster's switches (`0x400b` to `0x400e`) and the
  Gear Station (the Battle Subway's desks, plan 22 · U6's next part). Nimbasa's and Driftveil's Pokémon Centers are spawns.
- **The Ferris wheel** is a ride of ours: its attendant (ours, before the wheel's queue lane) asks, and the ride is a scene
  of the fade with lines over the dark, the city by day or its lights after dark. Black 2 rides it beside a Trainer of
  the day after a battle; that is left out, and the wheel is still a hall until it is drawn as one.
- **Route 5**: its trainers stand on the road (Preston, the Musician, battles when spoken to), and the street's performers
  and their audience (Black 2's `0x02f8`) are always out. As the player first comes in from Nimbasa's gate, Bianca runs
  out after them with HM02 (our data's, Platinum's table: Fly) and tells of the Hidden Grottoes; the grotto Black 2 leads
  the player to is plan 22 · U11's. Cheren on Route 5 is the post-game's. The route and the drawbridge play
  `unova/route4` until songs of their own.
- **The Driftveil Drawbridge** is a map of its own, its import the deck and nothing round it: the sea lies five tiles under
  it (`seaBelow`; style guide, "Relief", "A bridge over the sea"), its ends are abutments of ground at the deck's height,
  and it is seen with a camera of ours (`FieldCamera.Drawbridge`), lower and further off than the overworld's, where
  Black 2 names its camera 9. The original's props there (named as other places' by number) are left out, and the
  bridge's raising for ships and the Ducklett shadows over it are left for later.
- **Driftveil City's chapter**: as the player comes off the drawbridge a grunt presses a former member of Team Plasma to
  come back; Hugh runs up, the grunt flees with Hugh after him, and the former member asks the player to the old church
  by the Gym (Black 2's `0x40c3` 0 to 1). Rood waits before its door and tests the player in battle (`unova_rood`),
  Hugh comes back empty-handed, and inside Rood tells how Team Plasma broke in two (`0x40c3` 2 to 3; Black 2 has Rood ask
  the player in and tells the rest inside over several talks, ours is one scene as the house is come to). A worker
  keeps the Gym's door while Clay is busy until then. Clay's Gym is Black 2's mine of lifts on rails, which the reader
  leaves out: ours is a hall of the mine (`Data/maps/DriftveilCity_Gym.json`) with rock walls zigzagging up it and the
  seven workers of the original's Gym on its shifts, reached from the lobby by the lift's car at the head of its path (an
  overlay exit; Black 2's switch beside it is read for its words). Clay (`unova_leader_clay`) gives the Quake Badge and
  TM78 (our data's TM78, Platinum's table). Outside, Clay and Hugh: Hugh asks why Clay let the former members stay, and
  Clay tells them both of the Pokémon World Tournament south of the city (`0x40c3` 4 to 5). The motorcyclist who
  explains Triple and Rotation Battles only talks until plan 14 builds them; the market's goods, the flats' upper floors
  and the World Tournament beyond its gate (whose south door stays shut) are left.
- **Flags and variables**, ours where Black 2's are by value: `VAR_UNOVA_ROUTE_5` is Route 5's count (`0x4139`),
  `VAR_UNOVA_DRIFTVEIL` the city's (`0x40c3`, 0 to 5, `driftveil_city.txt`'s header); `FLAG_UNOVA_HIDE_DRIFTVEIL_GRUNT`
  is `0x02c3`, `_EXPLASMA_BRIDGE` `0x02c2`, `_ROOD_DOOR` `0x02cb`, `_DRIFTVEIL_GYM_WORKER` `0x02cf`, `_CLAY_DRIFTVEIL`
  `0x02cd`, `_HUGH_DRIFTVEIL` `0x02cc`; `_BIANCA_ROUTE_5`, `_HUGH_CHURCH`, `_HUGH_GYM`, `_HUGH_PLASMA_HOUSE`, `_ROOD_HOME`
  and `_EXPLASMA_HOME` are ours (Black 2 moves one object where we hide one person and show another);
  `FLAG_UNOVA_GOT_WATER_DRIFTVEIL` and `_GOT_TM78` are the gifts'. `common.UnovaDriftveil` hides the chapter's people
  (`FLAG_UNOVA_DRIFTVEIL_BEGUN`), run by `UnovaNimbasa` and, for a save of part a's, by `UnovaCatchUp`, with no story
  version raised.
- **Songs** of our own: `unova/driftveil` (the city, its rooms and the market). Clay's battle is the Gym Leader's, Rood's
  the trainer's.

## Unova's second chapter, part c: Route 16 to the Plasma Frigate (2026-10-25, plan 22 · U6, part c)

Black 2's side roads of the chapter and its first boarding of the Plasma Frigate, in our own words on the beats of the
decompilation's scripts (`res/field/scripts/route_16.s`, `lostlorn_forest.s`, `gear_station.s`, `anville_town*.s`,
`relic_passage*.s`, `relic_castle*.s`, `pwt.s`, `plasma_frigate.s`), read for the order of the scenes, who stands where,
the battles and the gifts, and nothing of their text.

- **Route 16 and the Lostlorn Forest**: Nimbasa's east gate opens onto Route 16, its trainers on the road, its small tree
  and boulder Black 2's (whose objects are told by the global scripts they run, 10000 a Strength boulder and 10004 a tree
  that Cut fells, since Black 2 draws them as models of many names); the gate house at its east end leads to the
  Marvelous Bridge, the post-game's, and stays shut. North of it the Lostlorn Forest: its trainers, and a traveller who
  asks after Zoroark, gives a TM and turns out to be the Zoroark, which cries and bounds off (Black 2's `0x03c7`). Black
  2's TM95 (Snarl) is no TM of our data's: the traveller gives TM46, Thief, Dark as Snarl is. Black and White's trailer
  stands in the forest with no way in, as in Black 2 (its room is left out).
- **The Gear Station and Anville Town**: Black 2's hall stands on rails, which the reader leaves out, so it is a map of
  ours (`GearStation`): the desks of the Battle Subway's three lines and of the Anville Town Line along its north wall.
  The Subway's desks say the cars are in the depot (its lines are plan 22 · U6's next part and U11, on plan 06 · R18's
  rules); the Anville Town Line's agent takes the player out to Anville Town, a ride of the fade and lines over the dark
  with the engine's sound, as the voyage to Castelia is, and Anville's station agent brings them back. Anville builds in
  a way of its own (`Architecture.Railyard`); its cars are trams on their rails and its turntable ground. On a Saturday
  or a Sunday its train fans and collectors come (Black 2's `0x02a8` and `0x02a9`, ours `FLAG_UNOVA_HIDE_ANVILLE_WEEKEND`,
  set or cleared by the town's arrival); the collectors' trades of items only talk of themselves, and the girl's lost
  Pansage (`0x0362`) is left. A Depot Agent in a house hands on lost property: a Rare Candy the first time (`0x00ef`),
  then one of Black 2's eight items a day, drawn on the game's die.
- **The Relic Passage**: three halls under the sea from the Castelia Sewers (whose way north opens) to the World
  Tournament's grounds, with a tunnel up into the Relic Castle's buried halls. Its four holes (the behaviour
  `StrengthHole`, 2 by 2) are filled by a boulder pushed with Strength: ours are the overlays' `holes`, a wall until the
  hole's flag (`FLAG_UNOVA_RELIC_PASSAGE_HOLE_EAST`, `_MIDDLE`, `_NORTH`, `_WEST`), and `pits` on their tiles whose
  script sets it as the boulder comes to rest, the boulder gone with the hole it filled. Black 2's boulder fills its
  2-by-2 hole whole; ours is one tile and fills it as well.
- **The Relic Castle**: the Desert Resort's ruin opens. Its entrance hall's two sand pits (Black 2's `MapChangeQuicksand`
  triggers) drop whoever steps in to the floor below, and stairs lead down and up; the buried halls the Relic Passage
  leads into run on to the deepest room, where Volcarona battles at level 35 (beaten or caught it is gone for good, run
  from it stays; Black 2's level 65 after the Hall of Fame is the post-game's). Black 2's zones 3 to 15, 18, 20 and 24 to
  31 of the castle are Black and White's rooms on the Black City matrix with nothing in them, and are not opened.
- **The Pokémon World Tournament's grounds and the Plasma Frigate**: the gate south of Driftveil opens onto the grounds;
  the tournament's hall stays shut (plan 14 · B11 and B12 with plan 22's next part), and Black 2's Clay, Hugh, Cheren and
  Colress at its door are held back for it. Black 2 plays the frigate's scene as the player comes out of the hall after
  its first tournament; ours plays at the first step onto the tournament's pier once Clay has told of the tournament
  (`VAR_UNOVA_DRIFTVEIL` 5): Cheren and Hugh come up behind the player, a grunt runs down the pier, Hugh goes after him and
  Cheren after Hugh, and Colress warns the player off and then bids them go. The gangway at the pier's end is the way
  aboard only while the frigate is moored (`FLAG_UNOVA_FRIGATE_AT_PIER` to `FLAG_UNOVA_FRIGATE_SAILED`); the frigate is
  drawn as a ship along the pier (a `Boat` of the overlay's), and stays drawn after it has sailed until it has a model of
  its own that can come and go. Aboard, one scene: Hugh and Cheren on deck, two grunts battled in turn (Black 2's
  `TRAINER_TEAM_PLASMA_GRUNT_5` and `_6`), Zinzolin from the bow, and the Shadow Triad putting all three back ashore;
  Black 2 has Cheren and the player clear a second band of grunts before Zinzolin, which is left. On the pier Hugh rages
  at the departing ship and Cheren goes on to Route 6. The ship's rooms, its later moorings and its later boardings'
  people and items are plan 22 · U8's; come aboard at any other time, the deck's grunts see the player back down.
- **Join Avenue's shops**: Black 2's avenue fills with shops as its visitors come and the player manages it; ours opens
  three of its bays as shops of our own, each a shopkeeper behind a counter whose end reads as its sign: a market of
  balls and medicine, a flower shop of mulches and Honey, and a café of drinks and a Casteliacone (`marts.json`'s
  `join_avenue_market`, `_flowers`, `_cafe`). The visitors, the ranks and the manager's work are plan 22 · U11's.
- **Flags and variables**, ours where Black 2's are by value: `VAR_UNOVA_PWT` is the grounds' count (Black 2's `0x40c6`
  from 2: 0 until the pier's scene, 1 aboard, 2 put ashore, 3 done); `FLAG_UNOVA_HIDE_HUGH_PWT`, `_CHEREN_PWT`,
  `_GRUNT_PWT`, `_COLRESS_PWT`, `_HUGH_PWT_PIER`, `_CHEREN_PWT_PIER`, `_HUGH_FRIGATE` (`0x033e`), `_CHEREN_FRIGATE`
  (`0x033d`), `_ZINZOLIN_FRIGATE` (`0x0341`), `_TRIAD_FRIGATE` (`0x0342`), `_FRIGATE_GRUNTS`, `_LOSTLORN_TRAVELLER`
  (`0x03c7`), `_LOSTLORN_ZOROARK`, `_VOLCARONA` (`0x03a0`); `FLAG_UNOVA_GOT_RARE_CANDY_ANVILLE` and
  `FLAG_DAILY_UNOVA_ANVILLE_LOST_ITEM` are the lost property's. `common.UnovaFrigate` hides the chapter's people
  (`FLAG_UNOVA_FRIGATE_BEGUN`), run by `UnovaDriftveil` and, for a save of part b's, by `UnovaCatchUp`, with no story
  version raised.
- **The look of the import**: the frigate's deck plates and Anville's ground lie under their walking surface, as
  Castelia's streets do, and are judged from what lies under a tile with nothing on or over it (`Bw2Cover`, their chunks
  alone); the sea round the frigate's hull is water, its deck plates wherever nothing names a tile and its walls
  (`out70_kabe`) its bulwarks, the Relic Castle's sand
  (`in10_suna`) sand and its walls of stone (`in10_kabe`) cliffs, not railings, and the Relic Passage's `d0yuka` a cave's
  floor, not a street's. A Unova way out at the edge of a cave's floor that the cave stood up as rock while
  it led nowhere is cut down to the floor it is walked into from (the Castelia Sewers' way into the Relic Passage).
- **Songs** of our own: `unova/lostlorn_forest`, `unova/anville`, `unova/subway` (the Gear Station),
  `unova/relic_passage`, `unova/relic_castle` and `unova/plasma_frigate`; Route 16 plays Route 4's, as Black 2 plays the
  same route theme on both, and the grounds Driftveil's.

## Unova's second chapter, part d: the Battle Subway, the World Tournament's hall and Driftveil's market (2026-10-25, plan 22 · U6, part d)

What part c left, in our own words and our own teams on the beats of Black 2's story (none of its scripts was read for
this part: the places, the people and the data of the import are part b's and c's, the rest is written from the plan).

- **The Battle Subway** is the smallest honest version of plan 06 · R18's machinery that the plan allows (`Models/Frontier/
  BattleSubway.cs`, GPU-free; the script command `subway`, `docs/scripts.md`). It borrows the Frontier's rules whole: who
  may go in (`FrontierRules`: no Egg, nothing on the banlist, no two of a species or holding the same item), copies of
  the team at level 50 or under, a Frontier battle (no EXP, money or Bag), the Frontier's trainers (`FrontierTrainers`)
  drawn by the Battle Tower's rooms (`FrontierDatabase.TowerRooms`, seven battles to a room), and the Battle Points
  of the Frontier's own count. What it does **not** borrow is a facility's own state: a run is the story's own
  variables (`VAR_UNOVA_SUBWAY_STATE`, `_TRAIN`, `_WINS`, `_SEED`, `_SLOT_0` to `_3`, `_BEST_SINGLE`, `_DOUBLE`,
  `_MULTI`), so there is no new field of a save, no host property and no migration, and a game saved in a car comes back
  to the same car (the arrival script goes on from the battle the streak has come to). The Single Train takes three
  Pokémon, the Double Train four (two trainers' worth of Pokémon on the field at once, one trainer with four) and the
  Multi Train two beside a partner, a Frontier trainer of the first room drawn on the run's own seed so that the
  same two Pokémon stand beside the player every car; the foes are a pair of trainers with two each. Black 2's Subway
  trainers aren't in the import, so the Frontier's stand in. After seven cars the conductor asks whether to get off or go
  on; getting off, or a battle lost, ends the run and pays the Battle Points of the sets cleared (3 for the first, one
  more for each set after it up to 10), and the best streak of each train is kept. **Ingo** waits at the 21st battle and
  **Emmet** at the 49th, in every train (together in the Multi Train): teams of our own at level 50
  (`our-trainers.json`'s `subway_ingo` and `subway_emmet`), since the data's Subway Bosses are the tag-battle ones of the
  story. The car is a map of ours (`BattleSubwayTrain`, entered by the Gear Station's three desks and left by a door or by
  the run's end), and the Gear Station's desks, which only talked, run it. **Left**: a run put down and taken up again
  at the desk, the Super trains (Black 2's second set of three, the Super Single's level cap of 100), a friend over a link
  for the Multi Train, the Subway's own board of records and its Battle Point exchange (the Frontier's Exchange Service
  Corner is the Tower's, plan 06 · R18a), prints and the Depot Agents' own battles; all of it is plan 22 · U11's and the
  plan 14 formats' (the Wi-Fi Subway needs other players: plan 06 · R18's decision 7).
- **The World Tournament's hall** (`PokemonWorldTournament`, a map of ours: Black 2's `pwt_2` rooms are not read, so the
  lobby, desk and stage are one hall) opens once Clay has told of the tournament (`FLAG_UNOVA_PWT_OPEN`, set by his
  script and by the grounds' arrival for a save past it). The first visit is a story event: Clay's welcome, the
  offer, the team of three (the Subway's rules and `subway begin single`), and the **Driftveil Tournament**, a knockout of
  three rounds in the Subway's way (level 50 copies, nothing from the Bag, nothing won but the match): Cheren
  (`unova_cheren`), Hugh (`unova_rival_10`, `_11` or `_12`, the team the player's starter meets) and Clay
  (`unova_leader_clay`), each their own team of the data at level 50 (`subway foe`). A win earns Clay's five Battle Points;
  a defeat is played out all the same, and either sets `FLAG_UNOVA_PWT_FIRST_TOURNAMENT`, which **lets the pier's scene
  of the Plasma Frigate play** (Black 2 plays it as the player comes out of the hall after its first tournament; ours
  is the first step onto the pier after it, as before). A save past the frigate is counted past the tournament too. The
  desk afterwards says the tournament has been played and that the others open as their rules are drawn up: **the
  formats of later tournaments (triples, rotations, the Leaders' and Champions' brackets) are plan 14 · B11 and B12's,
  and no other tournament is held**. Black 2's Hugh, Clay, Cheren and Colress at the hall's door (objects 6 to 9) stay
  held back, since the hall's scene is theirs.
- **Driftveil Market's three stalls** (`driftveil_city_7.txt`): the seller sells herbs (`marts.json`'s
  `driftveil_market_herbs`: Energy Powder, Energy Root, Heal Powder, Revival Herb), the sailor his imports
  (`_goods`: a Poké Doll, a Fluffy Tail, the Repels, the X items, Guard Spec. and Dire Hit) and the nursery aide sweets
  (`_sweets`). Black 2's market changes its wares with the ships that come in; ours is a fixed stock of our own.
  **Join Avenue's growth** (its visitors, ranks and the manager's work) stays plan 22 · U11's.
- **Anville Town's trades and its lost Pansage**: at the weekend the railway collector (14) swaps a Timburr for his
  Klink, Cogs, and the girl who tried swapping once (9) a Tympole for her Dwebble, Shelly, each once (`NpcTrades`'
  `cogs` and `shelly`, ours: a trade of Black 2's own isn't in the import). The girl (16) tells of her lost Pansage, which
  then sits in the plaza (`FLAG_UNOVA_ANVILLE_PANSAGE_LOST`, Black 2's `0x0362` object), hops into the player's arms
  when spoken to and is taken back to her for a Miracle Seed, after which it sits by her.
- **Songs** of our own: `unova/route5` (Route 5 and its gate), `unova/drawbridge` (the Driftveil Drawbridge) and
  `unova/pwt` (the World Tournament's grounds and hall), each a loop of 29 to 40 seconds with no clashes by day and at
  night.
- **Left for the next part**: the frigate's model coming and going, the Marvelous Bridge and Route 15 (plan 22 · U10), the
  Ferris wheel drawn as a wheel, the drawbridge's raising and its Ducklett shadows, the 2-by-2 boulder of the Relic
  Passage and the desert's storm timed within budget.

## Unova's third chapter, part a: Route 6 to Lentimas Town (2026-10-25, plan 22 · U7)

Black 2's beats from the Season Research Lab to the plane that lands in Lentimas Town, in our own words (the decompilation's
zone scripts, read for the order of beats alone) on flags of ours, `FLAG_UNOVA_…` and `VAR_UNOVA_…` (the original's by
value can't be named in a script).

- **The order of the chapter.** After the Plasma Frigate has sailed Cheren stands on Route 6 and walks the player to the
  Season Research Lab (`VAR_UNOVA_ROUTE_6` 1 to 2): he tells of the cold snap near Lacunosa Town and the odd readings at
  Castelia and Driftveil and gives HM03, Surf, **which is what takes the player over Route 6's east bank to Mistralton Cave**.
  On the west bridge Cobalion shows itself across the pond to Rood and an old
  man (3). Chargestone Cave comes out in Mistralton City, as the original's does: Bianca at its mouth (her errand is for
  the professor), N alone in its long corridor (he is gone after one word; Colress's grunts and N's later scenes are part
  b's). In Mistralton City Professor Juniper and Skyla meet the player on the road (the Master Ball for the Pokédex, once;
  `VAR_UNOVA_MISTRALTON` 1): the professor goes on to the Celestial Tower, Skyla to her Gym. Skyla's Gym (2, the Jet
  Badge, TM62) is a maze of wind with five pilots; the professor in the tower gives a Lucky Egg once and, once the Badge is
  won, goes back to the cargo hall (3); there Skyla asks whether the player is ready, Bianca dashes in for the ride, and the
  plane flies to Lentimas Town (4: a scene of the fade and lines over the dark, as the voyage to Castelia is). In Lentimas
  Town Juniper tells of the two stones, the dragons in them and Drayden of Opelucid City, and Bianca goes on to Reversal
  Mountain (`VAR_UNOVA_LENTIMAS` 1); an attendant on the strip flies anyone back to Mistralton City.
- **The Jet Badge** is the region's sixth (`givebadge jet`, the order of `unova.json`'s badges); Skyla's team is Black 2's
  (`unova_leader_skyla`), TM62 is the Gym's TM in our data's table (Platinum's, which the importer fills).
- **The Celestial Tower** has five floors on the original's plans; its spiral ramp is a rail Black 2 rides and the importer
  leaves out, so each floor has a way up at a tile of its own and a way down at the strip the original gives (overlay
  `exits` and `doors`), as Castelia's avenue has. The great bell at the summit may be rung by anyone; Black 2's Mesprit,
  hung over the summit until it is met, and the bell's quest (a waitress, a clerk and a maid of the lower floors) are part
  b's.
- **The water's edge** (`TileBehavior.SurfEdge`, Black 2's 0xB7): a blocked tile whose neighbour is open water. It was a
  wall; it is now **a tile a walker meets as water and a surfer rides over** (`FieldMovement.Step` answers `Obstacle.Water`
  to a walker, `CanStartSurf` lets Surf begin onto it), which is how the original's shore works, and which opens Route 6's
  east bank. A blocked tile of any other behaviour in water is still a wall.
- **Mistralton Cave** has one floor here. The original's own data joins its chambers by one-tile walls this import marks
  blocked, so only the first chamber is walked; the rest, its second floor (to the Guidance Chamber), Clay Tunnel and the
  two hikers beyond belong to U10, and the way on is shut meanwhile (a warp that leads nowhere is blocked).
- **Looks and sounds.** `Architecture.Airfield` (Mistralton and Lentimas: orange plaster, a hip roof, flower boxes), the
  plane (`PropType.Airplane`, white with an orange stripe) and the crystals (`PropType.ChargeStone`, three shards with a
  spark in the tallest, glowing with the lamps' light) are ours; the Gym is a Flying one, `GymTheme`
  `Flying`; looks `skyla`, `pilot` (and the eye theme `ace`, the class's battle theme the Gym's) and `n`. Songs of our
  own, none heard by the user yet: `unova/route6`, `chargestone`, `mistralton` (with a night arrangement) and
  `celestial_tower`, each over 25 s and clean in `MusicRender`.
- **What a save needs.** A save of the frigate's chapter is given the chapter's hidden people by `UnovaCatchUp`
  (`FLAG_UNOVA_MISTRALTON_BEGUN`), with no story version raised.

## Unova's third chapter, part b: the Strange House to Lacunosa Town (2026-10-25, plan 22 · U7)

Black 2's beats from the plane's landing to Lacunosa Town, in our own words (the decompilation's zone scripts, read for the
order alone) on flags of ours.

- **The order of the chapter.** From Lentimas Town the Strange House stands at the slopes' end and Reversal Mountain's cave
  leads on to Undella Town; Route 13 runs from Undella's gate to Lacunosa Town. Bianca, run ahead from Lentimas Town, waits at
  the west end of the great cave (`VAR_UNOVA_REVERSAL` 0): she asks the player along, heals the team and travels with them as
  their partner (1), stops at the cave's middle passage (`VAR_UNOVA_REVERSAL_LAIR` 1), wonders at Heatran's lair (2), and the
  player parts from her where they like (2 for REVERSAL: she then stands in the east tunnel talking about the Magma Stone).
  At Undella Town's east end Hugh runs up for a battle (`VAR_UNOVA_UNDELLA` 1). At Lacunosa Town Professor Juniper and Bianca
  (flown ahead) take the player to the old woman's house for the tale of the meteorite (`VAR_UNOVA_LACUNOSA` 1, then 2),
  Hugh finds Zinzolin and a grunt at the west end (3) and the two are beaten in a tag battle with Hugh and run (4).
- **Bianca is the player's partner**, the one `partner` of the cave: her trainer is Black 2's own `unova_bianca` (her
  Musharna's team of level 40), so wild Pokémon in the grass come in pairs, the team is healed after every battle that isn't
  lost and two trainers who see the player come together. **A partner follows through a place of several maps**
  (`Follower.Rehome`, `GameEngine.KeepPartnerAlong`; until this session she followed only on the map where she was a person):
  on a map that has no person for her she becomes the person of that map with the same key, who the flags keep off it until
  she comes (`bianca_along`, hidden by `FLAG_UNOVA_HIDE_BIANCA_ALONG`, one in each floor of the cave), and is shown there; a game
  saved on a walk finds her on the map it wakes on. She **leaves at every way out of the cave** (a trigger across each asks
  whether to part, and the player is walked back a step on no), since there is no person of hers outside it; a game lost
  with her along leaves nobody travelling, and the next floor come to puts her back at the west end (`common.UnovaBiancaWaits`).
- **What is left out, and why.** Black 2's White 2 floors of Reversal Mountain (twins of Black 2's six) are not built. The Strange
  House's four rooms behind its wrong doors (the original's rooms that lead back into the house) are plain wall, so the house is
  one way through: hall, library, rooms, library, cellar, attic; the attic's feather is a Lunar Wing. The researcher's Habitat List
  is a Pokémon of the mountain on the team (the game has no Habitat List). Cynthia's villa (`undella_town_5`) is a scene of
  Elesa and Skyla in Black 2's data, entered by no warp and no script of the story, and `undella_town_4` is an empty room with
  no way in; neither is built. Black 2's Cobalion on Route 13, Heatran in its lair, the later Hugh and Zinzolin of Undella Town and
  the worker on Undella Bay's jetty are overlay people held hidden for plan 22 · U10. Chargestone Cave has no later scene of N and
  no grunts of Colress in Black 2's data (N is met once, in part a), and Route 7 has no winter drifts in it: the winter chunks
  are Icirrus City's, Route 8's and the Dragonspiral Tower's cells and Twist Mountain's and the Moor of Icirrus's floors.
- **Looks and sounds.** Undella Town's houses are stand-ins in the Resort's way of building and Lacunosa Town's in the Stone's,
  Reversal Mountain's machine hall is Mistralton's hangar style; the beach's parasols are pavilions and its chairs benches (`Architecture`
  rows of their own are left for a later session, with the style guide's). `Bw2Cover` sorts `in65_light` (a shaft of light over a pool,
  no lamp) as nothing and `tg_` (Lacunosa's ground) as a packed track. Songs of our own, none heard by the user yet:
  `unova/strange_house`, `reversal_mountain` (with a night arrangement), `reversal_cave`, `undella` (with a night arrangement), `route13`
  (with a night arrangement) and `lacunosa`, each over 25 s and clean in `MusicRender`. Team Plasma's Zinzolin has the grunts' eye
  theme and the boss's battle theme in the sound map.
- **What a save needs.** A save past part a is given the chapter's hidden people by `UnovaCatchUpB` (`FLAG_UNOVA_LACUNOSA_BEGUN`),
  which the places of this part call at their arrival, with no story version raised; Bianca at the cave's mouth is shown by that
  floor's own arrival once the scene at Lentimas Town has been played.

## Unova's fourth chapter, part a: Route 12 to Opelucid City (2026-10-25, plan 22 · U8)

Black 2's beats from the road north of Lacunosa Town to the Legend Badge and the Plasma Frigate's ice, in our own words (the
decompilation's zone scripts, read for the order alone) on flags of ours. Part b (the Marine Tube, Humilau City, Route 22 and 21, the
Seaside Cave and the Frigate's rooms) is the next session's.

- **The order of the chapter.** From Lacunosa Town Route 12 runs west to the Village Bridge's east gate, the bridge to its west gate and
  Route 11 on to Opelucid City's east end (the gate houses `VillageBridgeGate_East`, `_West` and `OpelucidGate_East`); Route 9 and its
  mall lie west of the city behind `OpelucidGate_West`. `VAR_UNOVA_OPELUCID` is the chapter's count: Iris meets the player at the east
  end (our trigger, 440, 173 to 177; 1) and tells them to see Route 9 first; the Legend Badge is won from Drayden at the top of the Gym
  (2: he asks the player to his house, and `FLAG_UNOVA_DRAYDEN_AT_HOME` puts him there); his house's arrival plays the tale of the two
  dragons (Reshiram and Zekrom, the third dragon Kyurem, the Splicers guarded) and a sound from the sky (3) and puts the player at the
  door, where the city's arrival plays the ice; Zinzolin demands the Splicers and the grunts take the streets (4), Zinzolin waits at the
  Gym's door and is beaten there (5), Drayden shows the Splicers and the Shadow Triad takes them (6), the Triad's one waits at the east
  end and is beaten, Cheren comes by the Xtransceiver's call with the way to Humilau City, and the ice melts (7).
- **The ice is a swapped layout, as a season's chunks are.** Black 2 lays Opelucid's frozen look by a layout of its own (`MapReplace`,
  whose table the decompilation doesn't extract), and the data has no chunk of it, so the importer writes a twin of each of the city's five
  chunks (`Bw2World.IceChunk`: the same tiles, behaviours, heights and props, with made ground ice and bare ground snow, numbered the
  original's plus 2000: 2074 to 2077 and 2104) and `world.json`'s `swaps` lays them in their cells while `FLAG_UNOVA_OPELUCID_ICE` is set;
  the city's overlay lays snow over the air by a zone with the same flag, and the street's people (12 to 18) are indoors while it holds.
  The flag is set behind a black screen at 3 and cleared at 7 behind another. A swap may lie on chunks a building of the world's models
  stands on only where the same building stands in both (`Map.AddSwappedPlace`; it had refused any), since the ice changes the ground
  round the buildings and not the buildings. The Plasma Frigate itself is never drawn, as the first one's model isn't.
- **The Dragon Gym is five platforms joined by exits.** Black 2's is a tower the statues' heads carry the challenger up, which the data keeps
  as platforms over a void: ours joins them with six exits of the overlay (the entrance hall's corridor to the first platform, that to
  the second, the third and the fourth, and the last back to the first), with the five veterans standing on the platforms where the
  overlay puts them (`at`) and Drayden in the strip at the top. Black 2's Triple and Rotation battles (the veterans', the Gym's own, the
  Battle House's) are fought as single ones until plan 14 · B6 and B7. The Legend Badge is the region's seventh; Drayden gives TM82 as the
  data's table, Platinum's, fills it.
- **The Village Bridge's rooms are not built.** Its houses stand on Black 2's rails, which give a warp no tile (as Castelia's avenue's
  do): the bridge, its gates and its trainers and balls are open and its seven rooms (the stone house, the restaurant, the Hip Waders'
  hut, the nursery, the cottage, the rest house, the studio) and the five people who stand or walk on its rails (the musician Russo, the Gentleman with
  the 999-win streak and his battle, a backpacker, the runner who asks for Fresh Water and a girl) wait for part b. Route 11's Virizion, who crosses the road for the first to
  come, is plan 22 · U10's (the Swords of Justice) and is held hidden, as Route 13's Cobalion is; two balls of Route 11 with nothing in them
  are held back. Opelucid City's old building (`opelucid_city_12`, the memory of Iris and Drayden after the League) is the post-game's.
- **Shopping Mall Nine** has five counters of our own stock on Goldenrod's lists (marts.json's `mall_medicine`, `_balls`, `_vitamins`,
  `_battle`, `_machines`); its evening sale and its Lady's errand are left out. Opelucid's five houses of two floors, its Pokémon Center
  (a spawn) and Route 9's trainers are built as the data has them; the woman upstairs in Drayden's house gives a Ring Target once.
- **Looks and sounds.** Opelucid's buildings are stand-ins in Blackthorn's way of building (dragon-blue gables) and Jubilife's, the
  bridge's in the harbour's, until the towns have ways of their own (`Architecture` rows for Opelucid and Humilau are part b's, with the
  style guide's). Drayden has the dragon elder's look until plan 22 · U12. Songs of our own, none heard by the user yet:
  `unova/opelucid` (with a night arrangement), `route12` (the family of Routes 9, 11 and 12, with a night arrangement),
  `village_bridge` (with a night arrangement) and `shopping_mall`, each over 25 s and clean in `MusicRender`. Smashers and Hoopsters
  have the sporting eye theme.
- **What a save needs.** A save past Lacunosa Town is given the chapter's hidden people by `UnovaCatchUpC` (`FLAG_UNOVA_OPELUCID_BEGUN`),
  which the arrivals of Route 12, Route 11, Opelucid City, its Gym and Drayden's house call, with no story version raised.

## Kanto's first chapter (2026-10-18, plan 19 · K3)

FireRed's first chapter, from the bedroom in Pallet Town to the Boulder Badge in Pewter City, is scripts of ours on its
beats (`pallet_town.txt`, `pallet_town_professor_oaks_lab.txt`, `viridian_city_mart.txt`, `viridian_city.txt`,
`pallet_town_rivals_house.txt`, `route_22.txt`, `route_2_east_building.txt`, `pewter_city.txt`, `pewter_city_gym.txt`,
`pewter_city_museum_1f.txt`), under FireRed's own flags and variables (`VAR_MAP_SCENE_PALLET_TOWN_OAK`,
`VAR_MAP_SCENE_PALLET_TOWN_PROFESSOR_OAKS_LAB`, `VAR_MAP_SCENE_VIRIDIAN_CITY_MART`, `FLAG_HIDE_OAK_IN_HIS_LAB`…). What
differs from FireRed, and why:

- **Whose rules.** Kanto is fought by the rules the game is played by (since plan 06 · R19, the modern ones for every
  game: decision of 2026-10-12), with abilities, held items and the physical-special split, on FireRed's teams and
  levels (`Data/world/kanto/trainers.json`). FireRed's own Generation 3 rules are no preset. The levels are FireRed's;
  whether a Champion who crosses from Sinnoh meets them scaled is the user's choice, still to be made, and nothing scales
  them yet.
- **A new game** begins on FireRed's tile of the bedroom (6, 6 of `PalletTown_PlayersHouse_2F`) with no Pokémon and an
  empty bag (`kanto.json`'s `hasStory`). Whoever crosses from Sinnoh arrives before the house instead (`Region.Arrival`).
  What a Kanto game starts with is `common.KantoNewGame`, which `StoryMigration.BeginNewGame` runs after `NewGame` for
  every new game (each region's `<Region>NewGame`), since Sinnoh's and Kanto's flags never meet.
- **Oak in the grass.** FireRed's Oak walks the player round to his lab and opens its door; here he catches up with the
  player, says his piece and the picture goes dark to come up inside the lab, where its own arrival walks him to his desk
  and the player after him. So does the Gym's guide in Pewter City, who in FireRed leads the player round to the Gym's
  door: the player is put down before it.
- **The three Poké Balls** each open the choice of three, as Sinnoh's briefcase does (`starter`), whichever is looked at;
  the rival takes the one strong against the player's (`Region.RivalStarterFor`), and both balls leave the table.
  `StoryState.RivalStarterFor` reads the briefcase the species is in, so a Kanto starter is answered by a Kanto one. A
  game that crosses from Sinnoh and plays Kanto's opening keeps one choice of starter, the last made.
- **The rival** is FireRed's two classes (`Rival Early`, `Rival Late`), which are the rival's class here
  (`TrainerDatabase.IsRival`): his name is the player's choice, his battle music the rival's role, which Kanto plays as
  its trainers' theme (`MusicDirector.StandIns`), as FireRed does.
- **Viridian's old man** shows no catch: FireRed's lesson is a battle he fights against a Weedle while the player
  watches; here he tells it, since the watched lesson of this game is the professor's assistant's (`catchinglesson`).
- **Flash** is FireRed's HM05; Platinum's HM05 is Defog and its Flash TM70, which Oak's aide on Route 2 gives for ten
  species caught (the new built-in variable `CAUGHT`). Using it outside battle asks for the Boulder Badge (Kanto's gates:
  Flash Boulder, Cut Cascade, Fly Thunder, Strength Rainbow, Surf Soul, Rock Smash Marsh, Waterfall Volcano).
- **The Running Shoes** come from Oak's aide in Pewter City once the Boulder Badge is won, as FireRed's do; until then
  nobody in Kanto runs.
- **A save of Kanto from before the chapter** (K1 and K2's stand-in start, which owned one of Oak's three and recorded no
  starter) is taken past the opening (`common.KantoOpeningDone`, story version 20), its starter the one it owns.

## Johto's first chapter (2026-10-18, plan 20 · J4)

HeartGold's first chapter, from the bedroom in New Bark Town to the Zephyr Badge in Violet City, is scripts of ours on
its beats (`new_bark_player_house_1f.txt`, `new_bark.txt`, `new_bark_elms_lab_1f.txt`, `cherrygrove.txt`,
`route_30_mr_pokemon_house.txt`, `route_29.txt`, `sprout_tower_3f.txt`, `violet_gym.txt`, `violet_pokecenter_1f.txt`,
`violet.txt`, `route_32.txt`, `route_32_pokecenter_1f.txt`), under HeartGold's own flags and variables where it has them
(`VAR_SCENE_ELMS_LAB`, `VAR_SCENE_PLAYERS_HOUSE_1F`, `VAR_SCENE_CHERRYGROVE_CITY_OW`, `FLAG_HIDE_NEW_BARK_RIVAL`,
`FLAG_ELMS_LAB_PREVENT_PLAYER_ESCAPE`, the unnamed `VAR_UNK_408B` and `VAR_UNK_40A4` of its triggers) and ours where it
hasn't (`FLAG_HIDE_SPROUT_TOWER_RIVAL`, `FLAG_HIDE_VIOLET_POKECENTER_AIDE`, `FLAG_GOT_MYSTERY_EGG`). What differs from
HeartGold, and why:

- **Whose rules and levels.** Johto is fought by the rules the game is played by, on HeartGold's teams and levels
  (`Data/world/johto/trainers.json`), as Kanto's chapter is. Whether a Champion who crosses from Kanto meets them scaled
  is the user's choice, still to be made (plan 18 · W4's study), and nothing scales them yet.
- **A new game** wakes in the bedroom upstairs (4, 9 of `NewBark_PlayersHouse_2F`) with no Pokémon and an empty bag
  (`johto.json`'s `hasStory`); whoever crosses from Kanto arrives before the house (`Region.Arrival`). What a Johto game
  starts with is `common.JohtoNewGame`, run after `NewGame` for every new game.
- **The Pokégear, the Map Card and the Mystery Egg are flags** (`FLAG_GOT_POKEGEAR`, `FLAG_GOT_MAP_CARD`,
  `FLAG_GOT_MYSTERY_EGG`), with their fanfares and words, and an Egg in the bag would want a key item of Johto's own,
  which no other scene asks for. Since plan 20 · J3 the Pokégear is a screen of the start menu, and Mom's and Elm's
  numbers are registered as their scenes say ("The Pokégear", below).
- **Elm's machine** opens the choice of three, as Sinnoh's briefcase does (`starter`), from the machine's tile or from
  Elm himself once he has asked; the boy at the window takes the one strong against the player's
  (`Region.RivalStarterFor`), and his three teams (`johto_rival_silver_2`, `_3`, `_6`) are HeartGold's for each.
- **The rival's name** is asked in the introduction, as Sinnoh's and Kanto's are (plan 02 · S4), so the policeman's
  question only has the name the player already gave confirmed; HeartGold asks it there with a keyboard.
- **The Running Shoes** come from Mom on the lab's step, as soon as the player has a Pokémon; HeartGold's Mom runs out
  to the town's edge.
- **The Guide Gent's tour** is told over a dark screen and ends before his house; HeartGold walks it.
- **The catching lesson** on Route 29 is the friend's, with the Pokémon Elm had left (`catchinglesson`), as Sinnoh's
  assistant's is; HeartGold's friend catches with their Marill.
- **The Egg** is Togepi's from the start (`giveegg "Togepi"`), handed over by Elm's aide in Violet City's Pokémon Center
  once the Zephyr Badge is won; HeartGold's Mystery Egg is the same Egg, shown to Elm first.
- **Violet's Gym** is open from the start, as HeartGold's is; Sprout Tower's Elder gives TM70 (Flash), and the Zephyr
  Badge lets Rock Smash be used outside battle (Johto's gates).
- **Route 32's head** turns back whoever has no Badge and gives a Miracle Seed to whoever has the Zephyr Badge.
- **The Ruins of Alph** stay shut behind Route 32's gate house: their chambers and the Unown are a later session's.
- **A save of Johto from before the chapter** (J2's stand-in start, which owned one of Elm's three and recorded no
  starter) is taken past the opening (`common.JohtoOpeningDone`, story version 21), its starter the one it owns.
## Kanto's second chapter, first part (2026-10-18, plan 19 · K4)

From Route 3 over Mt. Moon to Cerulean City, Nugget Bridge and Bill's Sea Cottage, scripts of ours on FireRed's beats
(`mt_moon_b2f.txt`, `cerulean_city.txt`, `cerulean_city_gym.txt`, `route_24.txt`, `route_25_sea_cottage.txt`,
`route_4_pokemon_center_1f.txt`), under FireRed's flags and variables (`VAR_MAP_SCENE_MT_MOON_B2F`,
`VAR_MAP_SCENE_CERULEAN_CITY_RIVAL`, `VAR_MAP_SCENE_ROUTE24`, `FLAG_HELPED_BILL_IN_SEA_COTTAGE`, `FLAG_GOT_SS_TICKET`…).
What differs from FireRed, and why:

- **The rival at Nugget Bridge** gives no Fame Checker (decision 2 of plan 19 leaves it out): after the battle he walks
  round the player and away south, by the side of the bridge his trigger leaves free.
- **Bill's teleporter** has no doors that open: the Clefairy steps into it behind a dark screen, and the cell separator
  brings Bill out of it the same way, with the level-up fanfare. FireRed's temporary flag for "Bill is in the
  teleporter" is a flag of the place here (`FLAG_MAP_LOCAL_BILL_IN_TELEPORTER`), which is forgotten as the player leaves,
  as FireRed's is.
- **Bill's list of favourite Pokémon** (since the chapter's second part) is a question of the four: the one picked is
  told in a line of words in place of FireRed's picture, and marked seen in the Pokédex as FireRed marks it; one
  picture a reading, where FireRed's list stays open.
- **The thief in Cerulean** hands TM28 back once beaten, wherever the bag has room (this game's bag has no limit), and
  leaves behind a dark screen; the policeman keeps the burgled house's door, and the Slowbro with its trainer the road
  south, until the S.S. Ticket, as FireRed's `CeruleanCity_EventScript_BlockExits` keeps them (`place` on every arrival).
- **The people FireRed's new game hides** that this chapter brings on (the rival at the bridge, Bill as himself, the
  Pokémon Center's two who only talk after the Hall of Fame) are hidden by `common.KantoNewGame` for a new game, and by
  the places' own arrival scripts for a save from before the chapter, which never came to them: no story version is
  raised.
- **Route 4's two Black Belts** talk: FireRed's Mega Punch and Mega Kick tutors wait for Kanto's tutors.
- **The Bike Shop** names its price and sells nothing: the Bike Voucher is Vermilion City's (the chapter's second part).
- **Kanto's badges** have colours and marks of their own (style guide, "Badges").
- **Route 22's gate** keeps FireRed's guard for the Boulder Badge (its Route 23 badge check, by the Badge itself): the
  step beside him turns back whoever hasn't it, and once he has seen it nobody is stopped there. Route 23 beyond, with
  its guards for the other seven, is plan 19 · K7's.

## Kanto's second chapter, second part (2026-10-18, plan 19 · K4)

From Cerulean City south over Routes 5 and 6 and the Underground Path to Vermilion City, the S.S. Anne, Lt. Surge, Route
11 and Diglett's Cave, scripts of ours on FireRed's beats (`vermilion_city.txt`, `vermilion_city_gym.txt`,
`vermilion_city_pokemon_fan_club.txt`, `vermilion_city_house_1.txt`, `cerulean_city_bike_shop.txt`,
`ss_anne_exterior.txt`, `ss_anne_2f_corridor.txt`, `ss_anne_captains_office.txt`), under FireRed's flags and variables
(`FLAG_GOT_BIKE_VOUCHER`, `FLAG_GOT_BICYCLE`, `VAR_VERMILION_CITY_TICKET_CHECK_TRIGGER`, `VAR_MAP_SCENE_VERMILION_CITY`,
`VAR_MAP_SCENE_S_S_ANNE_2F_CORRIDOR`, `FLAG_GOT_HM01`, `FLAG_FOUND_BOTH_VERMILION_GYM_SWITCHES`…). What differs from
FireRed, and why:

- **The Bike Voucher** is an item of ours (`Bike Voucher`, id 9010 in `tools/DataImporter/Overrides/items.json`), since
  no game the importer reads has it; the Bike Shop takes it for Platinum's Bicycle, which rides by plan 02 · S2's rules
  wherever FireRed's map headers allow cycling.
- **The S.S. Anne's departure** is FireRed's beats without its cut-in of the ship pulling away: coming off her with
  HM01 (`VAR_MAP_SCENE_VERMILION_CITY` 1), the player sees her from the gangway, her horn, a shake, and is back on the
  quay with the variable at 3 at once (FireRed's 2 and its two steps up the pier are one warp here). From then on the
  pier's sailor turns everyone back. The ship stands at the gangway as a ship of ours (an overlay's `Boat` prop), where
  FireRed draws one large object.
- **Lt. Surge's bins** are FireRed's rule (`SetVermilionTrashCans`): the first switch on any of the fifteen bins drawn on
  the game's own chance each time the Gym is come into, the second on a bin beside it, north, south, east or west; a
  wrong bin after the first resets both and draws them again. The switches' bins are story variables of ours
  (`VAR_VERMILION_GYM_SWITCH_1`, `_2`), the first switch found a flag of the place (`FLAG_MAP_LOCAL_…`, FireRed's
  `FLAG_TEMP_1`). FireRed's half-lit beams after the first switch are not drawn: the gates stand whole until both are
  found, and go then. The gates are things of the map of ours (`ElectricGate`) on the six tiles of the gap, which the
  world builder opens under them, where FireRed changes the tiles themselves with `setmetatile`.
- **Oak's aide in Vermilion City** comes after Lt. Surge only for a trainer without `FLAG_GOT_HM05` (Flash, which this
  game gives as TM70 from Route 2's aide), and points to Route 2 in our words.
- **Route 11's gate**: the aide's Itemfinder waits for an item this game doesn't have (Platinum has the Pokétch's
  dowsing app instead), and the boy's Nidorino trade, as Vermilion's Spearow trade, for plan 07's trades; both only talk.
- **Fossils, the Old Amber, the Pokédexes on Oak's desk and the Town Map on the rival's sister's table** have looks of
  their own (style guide, "Things laid out to be taken"); the Old Amber is given by the scientist beside it, as in
  FireRed, and waits for Cinnabar Island's lab (plan 19 · K7).
- **The people FireRed's new game hides** that this part brings on (the rival on the S.S. Anne, Oak's aide in Vermilion)
  are hidden by `common.KantoNewGame` for a new game and by the places' own arrival scripts for an older save, as in
  the first part: no story version is raised.

## Kanto's third chapter, first part (2026-10-18, plan 19 · K5)

From Cerulean City east over Routes 9 and 10 and through Rock Tunnel to Lavender Town and the Pokémon Tower, and west
along Route 8 and under Saffron City to Route 7, scripts of ours on FireRed's beats (`pokemon_tower_2f.txt`,
`pokemon_tower_5f.txt`, `pokemon_tower_6f.txt`, `lavender_town_volunteer_pokemon_house.txt`,
`route_7_east_entrance.txt`, `route_8_west_entrance.txt`, `route_10_pokemon_center_1f.txt`, `route_4.txt`,
`rock_tunnel_b1f.txt`), under FireRed's flags and variables. What differs from FireRed, and why:

- **Rock Tunnel is dark** as FireRed's map says (`requires_flash`, which the GBA reader now reads as the weather
  Sinnoh's dark caves have), until Flash is used, which Kanto's gates give to the Boulder Badge.
- **The Pokémon Tower's wild ghosts are met as themselves**: FireRed shows them as an unknown "GHOST" that can't be
  hit until the Silph Scope; this game has no such battle, so the Gastly and Haunter of the tower's grass of graves are
  battled and caught as any wild Pokémon. The spirit before the stairs of the sixth floor, whose battle without the
  Scope can only be run from, says its piece and pushes the player back instead: the tower is barred above the sixth
  floor until the Silph Scope, which the Rocket Hideout under Celadon City gives (part b, with the battle with Marowak,
  Mr. Fuji's rescue and the Poké Flute). Mr. Fuji's place at home stays empty until then.
- **The purified ground of the fifth floor** heals the team every time it is stepped into, as FireRed's; FireRed's
  `VAR_TEMP_1`, which the GBA forgets on every arrival, is forgotten by the floor's own arrival script.
- **The gates to Saffron City** turn everyone back: their guards are thirsty, and FireRed lets through whoever brings
  them the Tea from Celadon City's old lady, which is part b's. Their steps' variable
  (`VAR_MAP_SCENE_ROUTE5_ROUTE6_ROUTE7_ROUTE8_GATES`) is FireRed's.
- **Kanto's tutors** (Route 4's Black Belts for Mega Punch and Mega Kick, Rock Tunnel's youngster for Rock Slide) teach
  their move free, to one Pokémon, once in a game, as FireRed's do, each under FireRed's flag (`FLAG_TUTOR_MEGA_PUNCH`).
  FireRed's own table of who may learn a tutor's move is not read (it lies outside what the importer fetches), so the
  table is ours (`MoveTutors.Kanto`): a Pokémon may learn the move if its species could learn it by Platinum's data, or
  could learn the Platinum machine move named beside it (Focus Punch for the punch and the kick, TM80 for Rock Slide).
  The other tutors of FireRed (Dream Eater, Seismic Toss and the rest) come with their chapters.
- **Oak's aide on Route 10** gives an Everstone for twenty kinds caught, as FireRed's does.
- **A flight of stairs of Kanto's whose side is shut** (the tower's fifth floor, with a gravestone beside its west
  flight) is walked off to the north first: FireRed's stairs are taken only when walked onto the way they go, and the
  way on lies north; the engine takes a warp from any side.
- **A Gym of Kanto's is fought in on its Leader's stage** (`BattleArena.Gym` with the room's `arenaType`): Lt. Surge's
  battle had been fought indoors on a plain floor.
## The Pokégear (2026-10-18, plan 20 · J3)

Johto's Pokégear, from HeartGold's `src/application/pokegear`, `src/field/overlay_2_gear_phone.c`,
`src/unk_02092BE8.c` and `src/field/encounter_check.c` (read at the pinned commit), in our own words and music.

- **What it remembers is the story's own flags and variables**: the cards by HeartGold's names (`FLAG_GOT_POKEGEAR`,
  `FLAG_GOT_MAP_CARD`, `FLAG_GOT_RADIO_CARD`), a number by a flag of ours (`FLAG_PHONE_CONTACT_<ID>`), a rematch asked
  for by another (`FLAG_PHONE_REMATCH_<ID>`), so a save keeps it with no field of its own (as Amity Square's walk is
  kept), where HeartGold's save has a phone book of its own. Story version 22 registers Mom's number for a game of
  Johto that has the Pokégear and Elm's for one with his Pokémon (`common.JohtoPokegear`). It is Johto's alone: the
  start menu shows it only there.
- **The cards are tabs**, where HeartGold's are pages of a touch screen behind buttons; the clock is on every card. The
  map card opens the region's map (plan 01 · M12's screen, in a mode of its own, `TownMapMode.Card`, which goes back to
  the Pokégear), with Johto's `townmap.json` written for it: a block for each of Johto's areas on the cells its chunks
  take in HeartGold's overworld, and Kanto's half of that grid out of view. Fly over Johto is J9's.
- **A contact rings on their own** as HeartGold's ring manager has it: once ten minutes of the field's own time have
  passed since the last try (HeartGold counts the console's minutes), a step tries once, and 51 times in a hundred one
  of the registered numbers of the kinds that ring (trainers, Gym Leaders, the friend, Buena, the juggler) not met where
  the player is rings, drawn evenly; a miss waits ten minutes more. HeartGold also sorts its callers in three groups
  by a weight and remembers who has rung this round, and has scripted calls (Elm's about the Egg, the Bike Shop, Bill's
  boxes…); ours draws among all of them alike and has no scripted calls yet. No call comes underground or in a cave,
  where HeartGold's headers forbid them, nor while a script runs.
- **A rematch over the phone** (plan 06 · R12's rule, `VsSeeker.RematchTeam`): a registered trainer who has been beaten
  asks for one when a call with them falls on their day of the week and time of day (the phone book's
  `rematchWeekday` and `rematchTimeOfDay`; HeartGold's morning is 4 to 10, its day 10 to 20, its night the rest), and
  then waits on their map, spinning, until fought, through any number of doors (the Vs. Seeker's give up after a
  hundred steps). The teams are HeartGold's numbered after the first (`TRAINER_YOUNGSTER_JOEY_2` to `_4`), written as
  the trainer's row by the importer; HeartGold moves each caller on by flags of the story in their own scripts, and
  ours opens the row's levels by the Johto Badges, two for the second team, four, six and eight for the next, and
  Johto's Hall of Fame for a fifth (`PhoneRematch.BadgesForLevel`). Gifts over the phone (Joey's HP Up) are not given yet.
- **The radio** is HeartGold's dial: a round pad with a cursor the arrows move, four stations in Johto at HeartGold's
  spots, clear within the inner ring and heard through static within the outer, the Unown's signal alone in the Ruins
  of Alph's halls and nothing in a cave. The slots are HeartGold's (`RadioShow_TranslateStationID`): the music; Professor
  Oak's talk; the trainers' profiles at even hours and the towns' people at odd; the search party, the drama and Buena's
  password by the hour's remainder by three. Their words are ours (`Data/world/johto/radio.json`), drawn with the game's
  dice every six seconds. Team Rocket's broadcast and Mahogany's signal wait for their chapters (J11), the Poké Flute's
  for Kanto (J15), GB Sounds for decision 8.
- **The music channel** plays HeartGold's song of the day (`RadioShow_PokemonMusic_Setup`): the March on Sundays,
  Mondays and Fridays, the Lullaby on Tuesdays and Saturdays, and with the National Pokédex the Hoenn Sound on Wednesdays
  and the Sinnoh Sound on Thursdays. The songs are ours (`johto/radio_march`, `radio_lullaby`, `radio_hoenn_sound`, and
  Route 201's own theme for the Sinnoh Sound, which is what HeartGold's is). A song plays over the field until the radio
  is tuned away, a door is gone through or a battle begins, where HeartGold's keeps playing until another song is asked for.
- **What the songs do**: HeartGold adds 25 to its share of steps that may meet something with the March and takes 25
  away with the Lullaby (`FieldSystem_EncounterRateRoll`); ours adds them to Platinum's own share (40 on foot, 70 in tall
  grass or on a Bicycle), never under nought: HeartGold's share is an unsigned byte that can wrap round under the
  Lullaby, a bug not kept. The Hoenn and Sinnoh Sounds put the area's two species of each in the grass's slots 2 and 3,
  and 4 and 5, after a swarm's (`EncSlotArray_Update_HoennSinnohSounds`). The Unown's signal, which HeartGold reads to
  draw the Unown not yet caught, does nothing yet (the Ruins of Alph's chambers are still to come).

## The centre of Johto, first part (2026-10-18, plan 20 · J5)

The Ruins of Alph, Union Cave, Route 33 and Azalea Town, from HeartGold's maps, events and tables (read at the pinned
commit), in our own words and music.

- **The Ruins of Alph's panels**: each of the four chambers has its stone panel in the floor, and each is a puzzle of
  its own as HeartGold's are: sixteen pieces of a picture that begin on the ring of squares round a frame of four by
  four, each turned some quarter turns, to be picked up, turned and put down until every piece lies in its own place
  upright (`Models/AlphPuzzle.cs`; the screen is ours, `UI/AlphPuzzleScreen.cs`, the picture the species' own menu
  sprite cut into the stone, never the original's). The four are Kabuto (the north-east chamber, with a scientist in
  it), Aerodactyl (south-east), Omanyte (south-west) and a great bird, Ho-Oh (north-west). How each begins is fixed
  for the panel, as the original's are, from a draw of its own seeded by its name; a panel left half done begins again
  as it began the next time it is read. Solved, the panel is kept whole (`FLAG_ALPH_<NAME>_PANEL_SOLVED`, ours) and
  something is heard to rumble under the Ruins; what HeartGold does next (the floor giving way into the room below,
  the Unown appearing, the hall's way in opening, the rooms behind the chambers' back walls and their items) is the
  chambers' later session, so the hall's door and the back walls stay shut.
- **What waits for the next chapter (plan 20 · J7) stands as the chapter finds it**: the Slowpoke are gone from
  Azalea Town's streets and from Kurt's house, a grunt of Team Rocket stands about by the well, whose way down is shut
  with the Gym's, Kurt talks of the well and makes no balls yet (his Apricorn balls are J6's), the charcoal kiln's
  apprentice and his Farfetch'd are away in Ilex Forest, whose gate house keeps its west door shut. HeartGold's Rocket
  who bothers a passer-by, the red-haired boy and the Apricorn trees are left out until their sessions; Route 36's
  gate house at the Ruins' north end keeps its north door shut until that route opens.
- **Union Cave's Lapras**, which HeartGold brings to its lowest floor on Fridays, is left out until a later session;
  the cave's three floors are open, the lowest reached as in HeartGold.
- **Azalea Town is built in logs** under dark-brown roofs (`Architecture.Woodland`, the style guide's row), where
  HeartGold's houses are plain timber; its signs say what they say in our own words.
## The centre of Johto, second part (2026-10-18, plan 20 · J5)

Ilex Forest, Route 34 and the Day Care, Goldenrod City, Routes 35 to 37, the National Park and Ecruteak City with the
Burned Tower, from HeartGold's maps, events and tables, in our own words and music.

- **The road north goes as HeartGold's does, but for the strange tree**: Ilex Forest's small tree waits for Cut (the
  field's own obstacle), and the Sudowoodo HeartGold stands on Route 36 is the next chapter's (plan 20 · J7, with the
  SquirtBottle) and is left out until then, so no flag is set. Its ledges are what keep the National Park's side of
  Route 36 from Route 37 in HeartGold; with it gone, Ecruteak City is reached from Violet City as well as by Goldenrod
  and the park. Whoever spoke of the tree says something else meanwhile.
- **The Pokémon Day Care on Route 34 is Solaceon's**: its man and lady run the common Day Care scripts on the one Day
  Care the save holds, so a Pokémon left in either region is the same Pokémon, growing as the player walks anywhere.
- **Officers Keith and Dirk**, who battle in HeartGold only after dark (a talking double stands there by day), battle at
  every hour for now; their doubles are left out until the day's people are swapped as Sinnoh's joggers are.
- **Whitney** battles, cries and gives the Plain Badge and TM45 only when spoken to again, as in HeartGold (her lass's
  nudge is folded into the Gym Guide's line); the Badge counts her four trainers beaten. The Plain Badge's Strength and
  obedience follow the region's record.
- **What is shut until its session**: the Department Store's lift and basement, the Radio Tower above its ground floor
  (the quiz and the Radio Card are J7's, the takeover J11's), the Underground's lower floors, the Magnet Train, the
  Global Terminal, the Pokéathlon Dome, the Bell Tower beyond its gate, Morty's Gym and the roads to Routes 38 and 42.
  The Game Corner has no machines to play, as HeartGold's has none, and its prize counter is shut; every counter of
  the Department Store is the common counter of every Mart until the store's own stock is written. The Bug-Catching
  Contest, the Kimono Girls' first meeting, the Burned Tower's beasts, Eusine and the rival, the Farfetch'd and the
  Cut master, Bill and the Name Rater's renaming are left out or only talk.
- **Goldenrod is built in brick under gold roofs** (`Architecture.Boulevard`) and **Ecruteak in dark planks under deep
  red tile** (`Architecture.Heritage`), the style guide's rows; Ilex Forest plays the dungeons' theme as HeartGold's
  header gives it, and Routes 34 to 37 one theme of ours for HeartGold's two arrangements.

## Hoenn's first chapter (2026-10-18, plan 21 · H2, its first part)

Emerald's first chapter, from the moving van to Wally's catch in Petalburg City, is scripts of ours on its beats
(`littleroot_town.txt`, `littleroot_town_brendans_house_1f.txt`, `_2f.txt`, `littleroot_town_mays_house_1f.txt`,
`_2f.txt`, `route_101.txt`, `littleroot_town_professor_birchs_lab.txt`, `route_103.txt`, `oldale_town.txt`,
`petalburg_city.txt`, `petalburg_city_gym.txt`), under Emerald's own flags and variables (`VAR_LITTLEROOT_INTRO_STATE`,
`VAR_LITTLEROOT_TOWN_STATE`, `VAR_LITTLEROOT_RIVAL_STATE`, `VAR_ROUTE101_STATE`, `VAR_BIRCH_LAB_STATE`,
`VAR_OLDALE_TOWN_STATE`, `VAR_OLDALE_RIVAL_STATE`, `VAR_PETALBURG_CITY_STATE`, `VAR_PETALBURG_GYM_STATE`, the map
objects' `FLAG_HIDE_…`, `FLAG_RESCUED_BIRCH`, `FLAG_DEFEATED_RIVAL_ROUTE103`, `FLAG_ADVENTURE_STARTED`), none of which
meets a name of Platinum's, Kanto's or Johto's, so none needed `HOENN_`. What differs from Emerald, and why:

- **Whose rules and levels.** Hoenn is fought by the rules the game is played by, on Emerald's teams and levels
  (`Data/world/hoenn/trainers.json`), as Kanto's and Johto's chapters are; Emerald's own Generation 3 rules are not a
  third preset. Whether a Champion who crosses from Johto meets them scaled is the user's choice, still to be made, and
  nothing scales them yet.
- **A new game** steps out of the moving van beside the west house of Littleroot Town (123, 292, Emerald's dynamic warp
  out of the truck) with no Pokémon and an empty bag (`hoenn.json`'s `hasStory`). The van's ride is told over the dark,
  as the town's arrival, and only for a game standing on that tile with the introduction not begun: a Champion who
  crosses from Johto arrives before the house's door (`Region.Arrival`) and is never put in the van. What a Hoenn game
  starts with is `common.HoennNewGame`, run after `NewGame` for every new game.
- **The family lives in the west house** whatever the player's look; Emerald moves a girl into the east house and makes
  the west one the rival's. The east house is the rival's family's here for both, and its people of the player's family
  are left out of its overlay.
- **The rival is the hero the player isn't**: May for the boy, Brendan for the girl (`hoenn.json`'s `rivals`,
  `Region.RivalFor`), named by the look, so the introduction asks no rival's name. The player keeps their own figure
  (plan 18's decision 7); the rival is drawn as May or Brendan, looks of ours, written in an overlay as
  `Rival:MAY:BRENDAN` (`PlayerIdentity.CharacterFor`). Emerald's two encounter themes are one song here
  (`hoenn/rival`).
- **The clock** is the computer's own (`GameClock`), so setting it is a line and no screen; Mom comes up to see it as
  Emerald's does.
- **The neighbour's child comes in** when the player looks at the Poké Ball on their bedroom's floor, as Emerald's
  does; Emerald's second way in, the child walking in downstairs if the player leaves the room first, is left out.
- **The professor's bag** is a Poké Ball on the grass where Emerald draws a bag; looking at it opens the choice of
  three (`starter`), the first battle (a Zigzagoon of level 2) follows at once, and the professor's thanks are the
  lab's arrival. The rival takes the one strong against the player's (`Region.RivalStarterFor`), and Emerald's team
  for that pair is the battle on Route 103 (`hoenn_may_route_103_<the player's starter>`, `hoenn_brendan_…`).
- **The Mart's clerk in Oldale Town** gives the Potion where she stands; Emerald's clerk walks the player to the Mart
  first.
- **Petalburg's Gym** is open from the first visit; only its hall is walked until Norman's Balance Badge (plan 21 · H4),
  every way on into its rooms `locked` in its overlay. Norman stands in the hall (Emerald's `MoveNormanToEntrance`).
- **Wally's catch** is the catching lesson (`catchinglesson "Ralts" 5 by "Wally" "Zigzagoon" 7`, a lesson shown by
  someone other than the assistant, with their own Pokémon), in the tall grass east of the city, come to through the
  dark; Emerald walks the two there.
- **A save of Hoenn from before the chapter** (H1's stand-in start, which owned one of Birch's three and recorded no
  starter) is taken past the opening (`common.HoennOpeningDone`, story version 23), its starter the one it owns;
  Petalburg's Gym and Wally's catch are left for it to play.
- **Wally on the platform.** In the lesson, the figure on the player's platform is Wally's own look (`BattleSetup.PlayerLook`,
  the catcher's name read as a look); Sinnoh's lesson keeps the player's, as it had.

## The road to Rustboro (2026-10-18, plan 21 · H2, its second part)

Route 104 to Rusturf Tunnel is scripts of ours on Emerald's beats (`petalburg_woods.txt`, `rustboro_city.txt`,
`rustboro_city_gym.txt`, `rustboro_city_devon_corp_1f.txt`, `_3f.txt`, `rustboro_city_cutters_house.txt`,
`rustboro_city_pokemon_school.txt`, `route_116.txt`, `rusturf_tunnel.txt`, `route_104_mr_brineys_house.txt`,
`route_104_pretty_petal_flower_shop.txt`), under Emerald's flags and variables (`VAR_PETALBURG_WOODS_STATE`,
`VAR_RUSTBORO_CITY_STATE`, `VAR_RUSTURF_TUNNEL_STATE`, `VAR_ROUTE116_STATE`, `VAR_DEVON_CORP_3F_STATE`,
`VAR_BRINEY_HOUSE_STATE`, `FLAG_DEVON_GOODS_STOLEN`, `FLAG_RECOVERED_DEVON_GOODS`, `FLAG_RETURNED_DEVON_GOODS`,
`FLAG_SYS_POKENAV_GET`, `FLAG_HAS_MATCH_CALL`, `FLAG_BADGE01_GET` through `givebadge stone`). What differs, and why:

- **The Devon researcher** stays where he stands as the grunt comes down the path, rather than hiding behind the player:
  the path through the woods is one tile wide there.
- **Mr. Stone's office**: the employee's walk to the desk is a fade, and the player stands before the president's desk.
  The PokéNav is no item in the bag: it is the start menu's entry once Mr. Stone has given it, as Emerald's is.
- **Match Call**: the scientist who adds it at Devon's door registers Mom's and Mr. Stone's numbers, the rival registers
  in Rustboro, and the calls are ours (`phone.txt`); its tutorial, which opens the PokéNav on its own in Emerald, is a
  line. Hoenn's trainers who swap numbers, and their rematches, are a later session's.
- **The rival in Rustboro** is met at the city's south edge only; Emerald's other place, by Mr. Briney's cottage for a
  player who left by another way, is plan 21 · H3's, with the boat.
- **The PokéNav's pages**: the map (the region's picture, a cell to each chunk), the contest condition (round a
  pentagon, each quality's number beside it), Match Call and the ribbons. Emerald's search of Pokémon by condition and
  its ribbon list per Pokémon are left out for now.
- **Rusturf Tunnel**: the grunt cornered with Peeko at the dead end, his battle and Mr. Briney's thanks as Emerald plays
  them; the rocks that keep Wanda from her young man are Rock Smash's, and his breaking through (with HM04's gift) is
  plan 21 · H3's. Wanda speaks of her cousin Wally coming to stay, which Emerald tells in Verdanturf.
- **Mr. Briney** is home with Peeko once the goods are back, and his offer to sail is a line: the sailing to Dewford is
  plan 21 · H3's.
- **Hoenn's berry trees** are patches of their own past Sinnoh's 128 (`BerryPatches.HoennFirst`), numbered in the
  order of Emerald's tree names, each starting with the berry its name gives; they grow by Platinum's rules until
  Emerald's own come (the Wailmer Pail waters nothing yet).
- **Petalburg's Gym** is as long as the hall the camera follows: a GBA room longer than a chunk is as wide as its
  floor, not as its column of chunks (`World.cs`).
- **Still to come in H2**: Route 104, Petalburg Woods and its grunt, Rustboro City, Roxanne and the Stone Badge, Rusturf
  Tunnel and the Devon Goods, and Mr. Stone's PokéNav.

## Mr. Briney's waters, Dewford and Slateport (2026-10-18, plan 21 · H3, its first part)

Mr. Briney's boat to Slateport's museum is scripts of ours on Emerald's beats (`route_104_mr_brineys_house.txt`,
`dewford_town.txt`, `dewford_town_gym.txt`, `dewford_town_hall.txt`, `dewford_town_house_1.txt`, `_2.txt`,
`granite_cave_1f.txt`, `granite_cave_stevens_room.txt`, `route_109.txt`, `route_109_seashore_house.txt`,
`slateport_city.txt`, `slateport_city_sterns_shipyard_1f.txt`, `slateport_city_oceanic_museum_1f.txt`, `_2f.txt`,
`slateport_city_pokemon_fan_club.txt`), under Emerald's flags and variables (`VAR_BRINEY_LOCATION` with Emerald's
numbers, 1 the cottage, 2 Dewford, 3 Route 109; `FLAG_MR_BRINEY_SAILING_INTRO`, `FLAG_DELIVERED_STEVEN_LETTER`,
`FLAG_BADGE02_GET` through `givebadge knuckle`, `FLAG_DEFEATED_DEWFORD_GYM`, `FLAG_DOCK_REJECTED_DEVON_GOODS`,
`FLAG_DELIVERED_DEVON_GOODS`, `VAR_SLATEPORT_MUSEUM_1F_STATE`, `VAR_SLATEPORT_OUTSIDE_MUSEUM_STATE`). What differs, and why:

- **The boat** doesn't move: Emerald slides the boat and the player across the sea tile by tile, which the field can't
  do for a boat (only a surfer crosses water). The voyage is a scene: out of the cottage to the jetty, the sailing song,
  the cinema bars and the camera travelling over the sea of Routes 105 and 106 (or 107 and 108), then the far jetty.
  A boat of ours (`PropType.Boat`, an open hull) lies at each of Mr. Briney's three moorings, whether he is there or not.
- **Where he sails**: from the cottage only to Dewford; from Dewford back to the cottage, and to Slateport's beach once
  Steven has his letter (Emerald's own rule); from Route 109 back to Dewford. Emerald's call from Dad on the first
  voyage is left out: Norman has no number yet.
- **Brawly's Gym** is dark by its own light, not a cave's: every trainer of the six beaten widens the circle the player
  sees by Emerald's flash levels (`Overworld/DewfordLights.cs`, `sFlashLevelToRadius`), and Brawly's defeat lights the
  hall. Flash does nothing there, as in Emerald.
- **Flash** is TM70 from the hiker at Granite Cave's mouth, as Kanto's aide gives it: Emerald's HM05 is Platinum's Defog.
- **Steven** asks for the Letter only from someone who carries it, and leaves the way the player came in.
- **Match Call** gains Steven, Brawly and Scott with their scenes, and Ricky and Lola, the two tubers of Route 109's
  beach, who ask once beaten. Emerald's other trainers who register, and every rematch, wait for Emerald's table of
  rematches, which the reader doesn't read yet.
- **Dewford Hall's** trendy phrase is a word of the week nobody can change (Emerald's Easy Chat has no place here),
  and the man with TM36 stands there from the start.
- **The Oceanic Museum**: fifty Poké Dollars at the counter (waved in short of money while carrying the goods, as
  Emerald does); Team Aqua's grunt from the tunnel gives TM46 and is gone in a fade; upstairs the two grunts' battles
  one after the other, Archie's warning and the captain's thanks, the team healed. The visitors come back once Team
  Aqua has gone. Scott meets the player at the door and swaps numbers.
- **Slateport's market** has the Energy Guru's vitamins; the decoration and doll stalls say their stock hasn't come in
  (secret bases are plan 21 · H10), and the Battle Tent's lobby is open with its bouts to come.
- **Granite Cave's cracked floor**, which only the Mach Bike crosses, is a floor like any other until the bikes come.
- **Still to come in H3**: Route 110 and its Trick House, the Cycling Road, Mauville City, Wattson and the Dynamo Badge,
  Rydel's two bikes, Route 117, Verdanturf and the three Battle Tents' bouts, Rock Smash for Rusturf Tunnel and the young
  man's HM04, the rival on Route 110.

## Route 110 to Verdanturf (2026-10-19, plan 21 · H3, its second part)

Route 110, Mauville City, Route 117 and Verdanturf Town are scripts of ours on Emerald's beats (`route_110.txt`, the
Cycling Road's two gates, `route_110_trick_house_entrance.txt`, `_puzzle_1.txt`, `_end.txt`, `mauville_city.txt`,
`mauville_city_gym.txt`, `mauville_city_bike_shop.txt`, `mauville_city_house_1.txt`, `route_117.txt`, `rusturf_tunnel.txt`,
`verdanturf_town.txt`, `verdanturf_town_wandas_house.txt`, `verdanturf_town_friendship_raters_house.txt`), under Emerald's
flags and variables (`VAR_ROUTE110_STATE`, `VAR_TRICK_HOUSE_LEVEL`, `VAR_TRICK_HOUSE_ENTRANCE_STATE`,
`VAR_TRICK_HOUSE_PUZZLE_1_STATE`, `FLAG_DEFEATED_WALLY_MAUVILLE`, `FLAG_DECLINED_WALLY_BATTLE_MAUVILLE`, `VAR_SCOTT_STATE`,
`FLAG_RECEIVED_BIKE`, `FLAG_RECEIVED_HM_ROCK_SMASH`, `VAR_MAUVILLE_GYM_STATE`, `FLAG_MAUVILLE_GYM_BARRIERS_STATE`,
`FLAG_BADGE03_GET` through `givebadge dynamo`, `FLAG_RECEIVED_HM_STRENGTH`, `FLAG_RUSTURF_TUNNEL_OPENED`). What differs, and why:

- **Rydel's two bicycles** are `BikeKind` (`Overworld/FieldMovement.cs`), carried by `Walker.Bike` and `Player.Bike`, set by
  the item the player gets on with: the Mach Bike is always at full speed (Platinum's top gear) and is what rides up a
  muddy slope; the Acro Bike keeps a bicycle's easy pace, hops along a rail (`MB_*_RAIL`, only along its length, never
  across it, never on foot) and up a bumpy slope. The run button changes gear on Platinum's Bicycle alone. Emerald's Mach
  Bike gathers speed over a few steps and the Acro Bike's wheelie is a button of its own: neither is played; the bicycle
  is at its speed at once and the hop is taken by stepping onto the rail. A save made riding one rides the bike in the bag.
  Granite Cave's and Sky Pillar's cracked floors (`MB_CRACKED_FLOOR`), which only the Mach Bike crosses without falling, are
  still floors like any other: falling through to the floor below is left for plan 21 · H12, with Sky Pillar.
- **The Itemfinder**, the rival's gift on Route 110, is Emerald's `ItemfinderCheckForHiddenItems`: hidden things not yet
  found within the GBA's screen (seven tiles across, five up and down), the nearest first, the player turned toward it
  along the farther axis (`Overworld/Itemfinder.cs`). Emerald's spin of the player is a plain turn.
- **The rival** battles at the first step past the road's north end with Emerald's team for the player's starter, gives
  the Itemfinder and rides north on foot (Emerald's rival gets on a bike: our rival has no bicycle figure).
- **The Trick House** is its first puzzle: the Trick Master is found under the desk, the scroll's code opens the door at the
  top of the hedge maze (Emerald swaps the door's tile; here a way the story opens, `FLAG_HOENN_TRICK_HOUSE_PUZZLE_1_UNLOCKED`),
  and his reward is a Rare Candy. Emerald's corridor back is a room of its own; here the last room's back door leads
  to the front room. The next puzzles wait for their badges and their rooms (plan 21 · H4 and after).
- **Wally** battles at the Gym's door when spoken to, as Emerald's does; his class is the rival's, drawn as Wally. Scott
  comes up afterwards, and Wally and his uncle wait in Wanda's house in Verdanturf.
- **Mauville's Gym**: Emerald draws the beams into its tiles and flips all of them whenever a switch other than the last
  one stepped on is pressed. Here the beams that block are electric gates of the map (`npcType` `ElectricGate`, as
  Vermilion's), two sets shown by flags of ours that the Gym's script keeps to Emerald's barrier flag; Wattson's defeat
  turns them all off. The beams' ends that block in both states are the room's walls. Wattson gives TM34 (Shock Wave, the
  same number as Emerald's) and his number.
- **Rusturf Tunnel**: the two rocks are broken with Rock Smash (the Dynamo Badge), and the young man's scene follows the
  second at once (Emerald's `OnFrame`), or the next step by the rocks for one broken from the party menu: HM04, then he and
  Wanda go home, where they wait.
- **The GBA's temporary flags** (`FLAG_TEMP_1` to `1F`) are cleared as a place is left, as the original clears them as
  every map loads, and an obstacle's is made its area's own: a tree cut on Route 104 had hidden one on Route 117 and one in
  the Trick House that shared its number.
- **Match Call** gains Wattson and eight trainers of Routes 110 and 117 who ask once beaten (Edwin, Benjamin, Abigail,
  Isabel, Isaac, Lydia, Dylan, Maria). Their rematches still wait for Emerald's table of rematches and its rule (a call's
  chance and steps), which is no small matter: plan 21's status records it.
- **The Day Care** on Route 117 is Sinnoh's (`common.DayCareMan`, `DayCareLady`): one Day Care for the whole game.
- **What waits**: the Battle Tents' bouts (Slateport's on the Battle Factory's rules could come first; Verdanturf's and
  Fallarbor's need the Battle Palace's and the Battle Arena's, plan 14 · B9); Mauville's Game Corner (shut); New Mauville
  (shut until Wattson's Basement Key, after Norman); the move tutors of Mauville and Verdanturf (they talk); the Cycling
  Road's timed challenge; Professor Birch's number on Route 110.

## The desert, Fallarbor and Mauville's Game Corner (2026-10-20, plan 21 · H4, its first part, and H3, its third)

Routes 111 to 114, Fiery Path, Fallarbor Town, Meteor Falls' first room and Mauville's Game Corner are scripts of ours on
Emerald's beats (`route_111.txt`, `route_111_winstrate_familys_house.txt`, `route_111_old_ladys_rest_stop.txt`,
`route_112.txt`, `route_112_cable_car_station.txt`, `route_113_glass_workshop.txt`, `route_114.txt`, its three rooms,
`fallarbor_town.txt`, `fallarbor_town_pokemon_center_1f.txt`, `fallarbor_town_cozmos_house.txt`, `meteor_falls_1f_1r.txt`,
`mauville_city_game_corner.txt`, `mauville_city_house_2.txt`, `slateport_city_mart.txt`), under Emerald's flags and
variables (`FLAG_HIDE_ROUTE_111_*_WINSTRATE`, `FLAG_RECEIVED_MACHO_BRACE`, `FLAG_DAILY_ROUTE_111_RECEIVED_BERRY`,
`FLAG_HIDE_ROUTE_112_TEAM_MAGMA`, `VAR_GLASS_WORKSHOP_STATE`, `VAR_ASH_GATHER_COUNT`, `FLAG_HIDE_FALLARBOR_POKEMON_CENTER_LANETTE`,
`FLAG_HIDE_LANETTES_HOUSE_LANETTE`, `FLAG_SYS_PC_LANETTE`, `FLAG_RECEIVED_TM_ROAR`, `FLAG_RECEIVED_TM_DIG`,
`FLAG_DAILY_ROUTE_114_RECEIVED_BERRY`, `VAR_METEOR_FALLS_STATE`, `FLAG_MET_ARCHIE_METEOR_FALLS`, `FLAG_MET_PROF_COZMO`,
`FLAG_RECEIVED_COIN_CASE`, `FLAG_RECEIVED_20_COINS`). What differs, and why:

- **The desert's sandstorm and Route 113's ash** are stretches of their areas with weather and music of their own (an
  overlay's `zones`, `MapArea.ZoneAt`, read by `Map.WeatherAt` and `BgmTrackAt`; the music changes as a step crosses a
  zone's edge as it does at an area's). Emerald sets both by script: Route 111's sandstorm and sun triggers along the
  desert's edges and its `CheckSetSandstorm` by the player's row and column, Route 113's `CheckSetAshWeather` by its column
  (19 to 84) and its weather triggers. Our zones are rectangles round the same edges, so the storm begins a tile or two
  apart from where Emerald's trigger would start it on the desert's diagonal north-west side. The plan's wording (the
  desert's tiles an area of their own through the reader) is answered this way, which needs nothing of the importer.
- **The Go-Goggles' gate** is Emerald's `ViciousSandstormTrigger`: a step onto the desert's south or west edge without
  Go-Goggles is told so and pushed back a step. The goggles are the rival's gift in Lavaridge Town, plan 21 · H4's next
  part, so until then the desert, Mirage Tower and the Desert Ruins are closed (their doors are shut as well).
- **Rock Smash** finds a Pokémon under a Hoenn rock as Emerald's `RockSmashWildEncounter` does: after the rock breaks, the
  area's `rock_smash_mons` (five slots weighted as the water's) at its rate in sixteenths against a draw of 2,880, with a
  Cleanse Tag's and the flutes' say and no ability's (`Map.SmashOut`, `WildEncounterRules.RockSmashMeets`; the script
  command `smashencounter`, which the common Rock and UseRockSmash scripts end with). Platinum's areas have no such table,
  so its rocks still hide nothing.
- **Emerald's stairs join any two levels**: a GBA tile of elevation 0 can be stepped onto from any elevation, and some
  of Emerald's climb two levels in one tile (Route 114's two ways down to Meteor Falls). Drawn as ramps, those are steeper
  than a step, so on a GBA game's map a step along a stair's or a ramp's slope is never a cliff (`FieldMovement.Step`,
  `SlopedAlong`).
- **The Winstrates** come out of their door one after another and each battles (`battle … as "hoenn_victor"` and on); a
  family lost to part way is put back as it began when Route 111 is come to again (Emerald clears their trainer flags; a
  trainer battled `as` a team is battled again whether beaten or not, so only who stands outside is put back).
  Victoria's Macho Brace waits for Vicky's defeat. Gabby and Ty's interview on Route 111 is left for later (all three of
  their places hidden, where Emerald shows the first).
- **The glass workshop's ash** is gathered by stepping through the grass of Route 113's ash zone with the Soot Sack in the
  bag, each tile once until the place is come to again (Emerald's `STEP_CB_ASH` turns the grass bare until the map
  loads). The master makes the five flutes at Emerald's prices in ash and hands one over at once, where Emerald sends the
  player out and back first; the Pretty Chair and Desk are decorations, plan 21 · H10's.
- **Meteor Falls** plays Emerald's scene at its trigger: the two grunts of Team Magma with the meteorite, Archie and his two
  grunts come in after them, the player pushed aside as Team Magma runs, Archie's words. Route 112's two grunts are gone
  afterwards (the cable car's ride and Mt. Chimney are the next part, so its attendant says the car is being looked over).
  Team Magma's eye theme is Team Aqua's song (both teams share Emerald's battle theme); a Hoenn `eye_magma` is left.
- **Professor Cozmo** waits in Meteor Falls until Mt. Chimney; his wife's lines ask `FLAG_DEFEATED_EVIL_TEAM_MT_CHIMNEY`,
  which the next part sets (`StoryTests.AskedAheadOfTheirScenes` names it until then). His scene at home, the Meteorite
  (an item of ours, 9110) for TM27, is written and waits for him to come home.
- **Lanette** introduces herself in Fallarbor's Pokémon Center and goes home, where she calls the storage system hers
  (`FLAG_SYS_PC_LANETTE`). Her Lotad doll is a decoration (plan 21 · H10).
- **Mauville's Game Corner** sits the player at Platinum's slot machine (plan 06 · R16b) at each of its twelve: Emerald's
  own machine (its reels, its Pikachu bonus) is not built, and its two roulette tables are covered over. The coin counter
  is Emerald's prices; the TM counter is Emerald's five at its prices (`gamecorner prize mauville`); the doll counter's prizes
  are decorations (H10). The Coin Case comes from the woman next door for a Harbor Mail (an item of ours, 9109), which
  Slateport's Mart sells as Emerald's does, on a counter of its own (`marts.json`'s `slateport_mail`).
- **The move relearner** of Fallarbor says what he does and does nothing yet (plan 12 · Q19), and the Metronome tutor in the
  Mart only talks. Fallarbor's Battle Tent is its lobby (the Battle Arena's rules, plan 14 · B9).
- **What waits**: the rest of H4 (the cable car, Mt. Chimney and Maxie, Jagged Pass, Lavaridge and Flannery, the Go-Goggles,
  the desert, Mirage Tower and its fossils, Petalburg's Gym and Norman), Route 115 and Meteor Falls' rooms behind the falls,
  the Battle Tents' bouts, Gabby and Ty, the Secret Power man on Route 111. (Part b, below, wrote all of H4 but the Desert
  Ruins, Gabby and Ty and the Secret Power man.)

## Mt. Chimney, Lavaridge and Petalburg's Gym (2026-10-20, plan 21 · H4, its second part)

The cable car, Mt. Chimney's summit, Jagged Pass, Lavaridge Town and its Gym, Mirage Tower, Route 115, Meteor Falls'
inner rooms and Petalburg's Gym are scripts of ours on Emerald's beats (`route_112_cable_car_station.txt`,
`mt_chimney_cable_car_station.txt`, `mt_chimney.txt`, `lavaridge_town.txt`, `lavaridge_town_gym_1f.txt`,
`lavaridge_town_herb_shop.txt`, `lavaridge_town_house.txt`, `mirage_tower_4f.txt`, `fallarbor_town_cozmos_house.txt`,
`petalburg_city_gym.txt`, `petalburg_city_wallys_house.txt`), under Emerald's flags and variables
(`VAR_CABLE_CAR_STATION_STATE`, `FLAG_HIDE_MT_CHIMNEY_TEAM_MAGMA`, `FLAG_HIDE_MT_CHIMNEY_TEAM_AQUA`,
`FLAG_DEFEATED_EVIL_TEAM_MT_CHIMNEY`, `FLAG_HIDE_MT_CHIMNEY_TRAINERS`, `FLAG_HIDE_MT_CHIMNEY_LAVA_COOKIE_LADY`,
`FLAG_RECEIVED_TM_RETURN`, `FLAG_RECEIVED_LAVARIDGE_EGG`, `FLAG_RECEIVED_CHARCOAL`, `FLAG_RECEIVED_TM_OVERHEAT`,
`VAR_LAVARIDGE_TOWN_STATE`, `FLAG_RECEIVED_GO_GOGGLES`, `FLAG_HIDE_MIRAGE_TOWER_ROOT_FOSSIL`, `_CLAW_FOSSIL`,
`FLAG_CHOSE_ROOT_FOSSIL`, `FLAG_CHOSE_CLAW_FOSSIL`, `VAR_MIRAGE_TOWER_STATE`, `VAR_PETALBURG_GYM_STATE`,
`FLAG_DEFEATED_PETALBURG_GYM`, `FLAG_RECEIVED_TM_FACADE`, `VAR_PETALBURG_CITY_STATE`, `FLAG_RECEIVED_HM_SURF`). Story
version 29: `common.HoennChapterSix`, which `HoennNewGame` calls, hides the summit's trainers and the Lava Cookie lady
until Team Magma has gone, the rival in Lavaridge until the Go-Goggles and Wally's father in the Gym until Norman is
beaten. What differs, and why:

- **The cable car** is a scene at each station: the attendant steps up onto the platform before the car, the player
  follows her up the station's corridor (one tile wide, so she can't walk beside them as Emerald's does), the picture goes
  dark over the ride and the other station's arrival script puts the player down (`VAR_CABLE_CAR_STATION_STATE` 1 going
  up, 2 coming down). Emerald's view of the car over the mountain is not drawn. Route 112's attendant waits for Team
  Magma's grunts to leave the road (`FLAG_HIDE_ROUTE_112_TEAM_MAGMA`): Emerald's stand in her door's way, ours beside it.
- **Mt. Chimney's summit**: Tabitha and two grunts are trainers of the map in the way; Team Aqua and Team Magma scuffle
  in the middle with their Poochyena, each of whom only talks. Maxie battles at the machine, his people are taken off
  behind a fade, and Archie walks round to thank the player from whichever side they spoke to Maxie from. The Meteorite
  is read out of the machine (Emerald's `MeteoriteMachine`) and Cozmo, home in Fallarbor from then on, gives TM27 for it.
  The mountain's trainers come out once Lavaridge Town is come to after the summit (Emerald clears their flag in
  Lavaridge's own script), and the Lava Cookie lady sells at 200 money from the summit's quiet.
- **Team Magma's eye song** is a song of Hoenn's own folder (`eye_magma`), which the sound map's `eyeSongs` names for
  `Team Magma`, `Magma Admin` and `Magma Leader` (`MusicDirector.EyeSong`): a region without the file plays the class's
  eye theme. Team Aqua keeps the eye theme part a gave both teams. The two leaders' battles and victories are Hoenn's own
  `battle_galactic_boss` and `victory_galactic_boss` by their classes, the admins' Team Galactic's grunt theme.
- **Lavaridge's Gym** is Emerald's two floors: the patches of sand that drop their walker to the floor of steam and the
  vents that blow them back up are warp panels (`Overworld/WarpPanels.cs`), each to the tile over or under it. Where a
  vent throws its rider onto plain sand that leads nowhere, the reader now counts that tile as a landing (`GbaWorld`), so
  the builder lays no warp there. The vent under Eli, the hiker, would throw its rider into the hall's wall, and Emerald
  keeps him standing on it for good: it is `locked`. Emerald buries its trainers in the sand and has them spring out as
  the player comes near; here they stand on their patches in plain sight.
- **The Go-Goggles** are Emerald's scene, but it waits for the doors it is written for: Emerald plays it on whatever tile
  the town is next come to by, though its state is set only inside the Gym; ours plays as the player comes out of the
  Gym or the Pokémon Center (`PLAYER_X` and `PLAYER_Y`). Emerald's rival rides off on a bicycle, a second object of the
  town; ours runs.
- **Mirage Tower** stands until one of its fossils is taken, where Emerald shows it on some days only. Taking either
  hides both, shakes the floor and puts the player down in the sand before where it stood (`VAR_MIRAGE_TOWER_STATE` 1);
  `FLAG_HOENN_MIRAGE_TOWER_FALLEN`, a flag of ours, shuts Route 111's way in (an overlay's `switches`), since Emerald hides
  the tower's whole map object, which this world draws as its ground.
- **Petalburg's Gym**: Emerald's doors between its rooms are warps that lead nowhere until a script moves the player; ours
  are read, each asking "Go in?" before it lets the player through, open in the hall with four Badges
  (`VAR_PETALBURG_GYM_STATE` 6) and in a room once its trainer is beaten. Whoever goes through comes in a tile north of
  the room's mat facing into the room, so they don't stand on the way back out; each mat leads back to the floor before
  the door it was come in by (the overlay's `doors`). Norman stands in the hall until the fourth Badge (Emerald's
  `MoveNormanToEntrance`) and waits in his room from then on.
- **HM03**: after Norman's battle Wally's father comes in, and the dark that closes in the Gym opens in Wally's house, where
  he gives Surf (`VAR_PETALBURG_CITY_STATE` 4 to 5). Emerald walks the two of them across the city to his door. Norman's
  Match Call is Emerald's on Route 104's boat, which H2 wrote without it, so it is not registered here.
- **What waits**: the Desert Ruins' door (until the Regis), Gabby and Ty, the Secret Power man, Norman's rematch, the
  harness's shots of this part.

## Dive and the sea floor (2026-10-20, plan 21 · H5, its first part)

Platinum has no Dive in the field, so the field move follows Emerald (`TrySetupDiveDownScript`,
`TrySetupDiveEmergeScript`, `TrySetDiveWarp`, `FldEff_UseDive`) where the plan doesn't say otherwise. What differs, and why:

- **The Badge is the region's**: Dive is offered only in a region whose file's `gates` name it (Hoenn's `"Dive": "Mind"`,
  Emerald's `FLAG_BADGE07_GET`), both in the field and on the team's menu (`PartyScreen.ActionsFor(pokemon, region)`):
  a Pokémon that knows Dive in Sinnoh or Johto shows the menu it always showed, and their dark water stays plain sea.
- **The tiles** are Emerald's four behaviours under names of ours, at the end of the GBA block (`DiveWater` for
  `MB_DEEP_WATER`, `NoSurfacing`, `Seaweed`, `SeaweedNoSurfacing`). Dark water is surfable like any sea; `CanDive` says yes
  on it to a surfer whose team knows Dive, `CanSurface` under any floor tile but the two that forbid it, and coming up
  also needs open sea on the same tile above (`SurfacesOnto`). Wild Pokémon of the floor are met only in its seaweed, from
  the floor's own water table (Emerald's underwater `water_mons`: Clamperl, Chinchou, Relicanth).
- **One button both ways**: Emerald dives and comes up with A on the tile underfoot, and B is our Run, so A is both:
  `TryInteract` reads the tile under a surfer or a diver before what lies ahead, and runs `common.Dive` or
  `common.Surface` (the question, the move's cut-in, then the script's `dive`, which fades, plays the bubbles' sound
  `dive` and lands the player on the same tile of the other map, diving or surfing).
- **A diver walks**: the plan had surfing's pace under the sea; Emerald's diver moves at walking pace and can't run, and
  so does ours (`FieldMovement`'s pace for `TravelMode.Diving` is `Walk`), with no bike, no ledges and no footfalls.
- **The floors are maps by matrix, not one map**: the reader lays each of Emerald's underwater maps under the route it
  lies under (`GbaWorld.LayUnderItsRoutes`); the floors under Routes 124 to 128 are matrix 2, `HoennUnderwater`, at the
  overworld's own coordinates, so a dive lands on the same tile (188 tiles each way, none astray). The floors under the
  Seafloor Cavern, Sootopolis and the Marine Cave are matrices of their own for H8 and H12. Only `underwater_route_124`
  is open; the rest is rock until its route opens.
- **Under the sea battles are fought on the water's ground and the sea floor's own stage**: `BattleTerrain.Water` (so the
  Dive Ball, Nature Power, Camouflage and Secret Power read as they do on the water, which is where Platinum's rules
  would put Emerald's underwater terrain) and, since part b, `BattleArena.Underwater` (`Map.ArenaAt` on an underwater
  map, whatever the seabed). Its look is ours: Emerald's underwater battle background has no counterpart in Platinum.
  Dive the move in battle is unchanged.
- **Route 124 waits for Lilycove**: it is reached by sea from Lilycove City (H7), so a walk of the game finds it only
  through `WorldWalk.OpenedAhead` (the sand before the treasure hunter's house), which H7 takes off. Its song is
  `hoenn/route104` until Emerald's Route 120 song is written; the treasure hunter's house is `locked` until H8.
- **Saves**: a game saved under the sea keeps `TravelMode.Diving` as one saved surfing keeps surfing, and a player on an
  underwater map always settles diving (and off one never does). No version was raised: no older save can stand on a map
  that didn't exist, so none is touched.
- **The sea floor's light, bubbles and sound are ours** (part b): Emerald draws the light of the waves and a diver's
  bubbles as a field effect and plays its own underwater song; ours are the style guide's caustic layers, a breath
  every 2.8 seconds of the field's clock heard as `bubbles`, and `AmbienceBed.Underwater` alone under whatever weather
  is above. Still to come: the floors under Routes 125 to 128 with their routes (H8), and the frame weighed with
  `profile` on the user's machine.

## Route 118 to Fortree City (2026-10-20, plan 21 · H6, its first part)

Route 118, Route 119 with the Weather Institute, Fortree City with Winona's Gym, and Route 120 are scripts of ours on
Emerald's beats (`route_118.txt`, `route_119.txt`, `route_119_weather_institute_1f.txt`,
`route_119_weather_institute_2f.txt`, `fortree_city.txt`, `fortree_city_gym.txt`, `route_120.txt`, and in `common.txt`
`HoennChapterSeven`, `HoennKecleonLook` and `HoennKecleonFound`), under Emerald's flags and variables
(`VAR_ROUTE118_STATE`, `FLAG_HIDE_ROUTE_118_STEVEN`, `FLAG_RECEIVED_GOOD_ROD`, `VAR_WEATHER_INSTITUTE_STATE`,
`FLAG_HIDE_ROUTE_119_TEAM_AQUA`, `FLAG_HIDE_WEATHER_INSTITUTE_1F_WORKERS`, `_2F_WORKERS`, `_2F_AQUA_GRUNT_M`,
`FLAG_RECEIVED_CASTFORM`, `VAR_ROUTE119_STATE`, `FLAG_HIDE_ROUTE_119_RIVAL`, `FLAG_HIDE_ROUTE_119_SCOTT`,
`FLAG_RECEIVED_HM_FLY`, `VAR_SCOTT_STATE`, `FLAG_KECLEON_FLED_FORTREE`, `FLAG_HIDE_FORTREE_CITY_KECLEON`,
`FLAG_NOT_READY_FOR_BATTLE_ROUTE_120`, `FLAG_RECEIVED_DEVON_SCOPE`, `FLAG_HIDE_ROUTE_120_STEVEN`,
`FLAG_HIDE_ROUTE_120_KECLEON_BRIDGE`, `FLAG_HIDE_ROUTE_120_KECLEON_1` to `_5`, `FLAG_DAILY_ROUTE_120_RECEIVED_BERRY`,
`FLAG_DEFEATED_FORTREE_GYM`, `FLAG_RECEIVED_TM_AERIAL_ACE`, `FLAG_ENABLE_WINONA_MATCH_CALL`). **No story version is
raised**: `common.HoennChapterSeven`, which `HoennNewGame` calls, hides the stretch's people (the rival and Scott on
Route 119, the institute's workers and the grunt who brings the news, the Wingull of Fortree's fourth house, Gabby and
Ty) and sets `FLAG_HOENN_CHAPTER_SEVEN`, a flag of ours; Route 118's and Route 119's arrival scripts call it for a save
that has never run it, which is every save from before this part, since nobody could reach these roads before it. What
differs, and why:

- **The weather is the area's zones**: Emerald's coordinate events set the weather as the player crosses rows of
  Routes 119 and 120 (`SetRoute119Weather`, `SetRoute120Weather`). Here the overlays give a stretch its weather
  (`zones`): Route 118 is clear, Route 119 rains from the institute's south to Fortree's edge, and Route 120 runs clear
  by the city, rain over the bridge and the long grass, and cloud in the south.
- **Steven on Route 118** comes to the ledge's edge before whichever of the three tiles below it the player steps on,
  hops down and walks off east, as Emerald's does; Emerald's line about the player's team is ours.
- **The Weather Institute**: Shelly battles only when spoken to, as in Emerald; once beaten, a grunt runs up from the
  stairs with the news of Mt. Pyre and Team Aqua leaves behind a fade. The scientist gives Castform at level 25 holding a
  Mystic Water, which needed `givepokemon … holding` (`docs/scripts.md`). The ground floor's beds heal the team, read on
  any of their tiles. The workers come back downstairs as the player comes out (state 1 to 2), where Emerald moves them
  on the institute's own map.
- **The rival on Route 119** runs up from the west end of the bridge, battles with the starter's team and gives HM02;
  Emerald's rides off on a bicycle (a second object of the rival), ours runs, as in Lavaridge. Scott then comes down the
  path to talk (`VAR_SCOTT_STATE`).
- **Kecleon nobody can see** are things of the map (`NPC.UnseenType`, "Unseen" in an overlay): in the way and spoken to,
  drawn as nothing, with no shadow; Emerald's `invisible` movement. Without the Devon Scope speaking to one says only that
  something unseeable is in the way. With it, the player is asked whether to use the Scope, the Kecleon's tile sparkles
  (`effect sparkles`), it cries and is battled at level 30 (Route 119's two and Route 120's five), and it is gone whatever
  came of the battle but a loss. Emerald plays the Scope's own reveal animation; the sparkles are ours.
- **Fortree's Kecleon** on the bridge to the Gym only flees, as in Emerald, and the bridge is free
  (`FLAG_KECLEON_FLED_FORTREE`). Emerald then swaps the bridge's metatiles; here the bridge is already drawn whole.
- **Steven on Route 120** asks whether the player is ready; refused, he waits (`FLAG_NOT_READY_FOR_BATTLE_ROUTE_120`).
  Agreed, he shows the bridge's Kecleon through the Scope, it is battled, he gives the Devon Scope and is gone behind no
  fade (Emerald's Steven walks off the bridge).
- **The berry beauty of Route 120** gives one berry a day. Emerald chooses it by the last digit of the trainer number,
  which no script here reads; the day's die (`random 5`) chooses among the same five: Figy, Wiki, Mago, Aguav, Iapapa.
- **Winona's Gym**: the rotating gates are the room's puzzle (`FortreeGates`, Emerald's `rotating_gate.c` with its
  Fortree layout, shapes and sweeps): a step into an arm turns the gate a quarter round unless an arm would sweep into a
  wall, and coming in again lays them out afresh. Her Badge counts the Gym's six trainers beaten, as Emerald's does; she
  gives TM40, and with the PokéNav her number (`addcontact winona`).
- **Match Call on the routes**: six trainers (Rose and Dalton on Route 118, Jackson and Catherine on 119, Robert and
  Jeffrey on 120) ask, once beaten and spoken to, to be registered, as Emerald's do; their calls are `phone.txt`'s.
- **What waits** (part b): Route 121, the Safari Zone, Fortree's decoration shop (H10), Gabby and Ty's interviews, the
  Ancient Tomb's and the Scorched Slab's doors (`locked`), Fortree's house trades, the Hidden Power quiz and the Sleep
  Talk tutor, the Wingull's errand, and the bridges' bounce underfoot.

## Route 121 and the Safari Zone (2026-10-20, plan 21 · H6, its second part)

Route 121, the Safari Zone's gate and the zone, and Fortree's first and second houses are scripts of ours on Emerald's
beats (`route_121.txt`, `route_121_safari_zone_entrance.txt`, `hoenn_safari_zone_south.txt`,
`fortree_city_house_1.txt`, `fortree_city_house_2.txt`, and in `common.txt` `HoennChapterSevenB` and Hoenn's way out
of `SafariTimeUp` and `SafariOutOfBalls`), under Emerald's flags and variables (`VAR_ROUTE121_STATE`,
`FLAG_HIDE_ROUTE_121_TEAM_AQUA_GRUNTS`, `VAR_SAFARI_ZONE_STATE`, `FLAG_GOOD_LUCK_SAFARI_ZONE`,
`FLAG_HIDE_SAFARI_ZONE_SOUTH_EAST_EXPANSION`, `FLAG_HIDE_SAFARI_ZONE_SOUTH_CONSTRUCTION_WORKERS`,
`FLAG_FORTREE_NPC_TRADE_COMPLETED`, `FLAG_MET_HIDDEN_POWER_GIVER`, `FLAG_RECEIVED_TM_HIDDEN_POWER`,
`FLAG_MOVE_TUTOR_TAUGHT_SLEEP_TALK`). **No story version is raised**: `common.HoennChapterSevenB`, which `HoennNewGame`
calls, sets what Emerald's new game sets for the zone (its expansion's attendant away) and `FLAG_HOENN_CHAPTER_SEVEN_B`,
a flag of ours; Route 121's arrival calls it for a save that never ran it. What differs, and why:

- **One Safari Game for every region**: Hoenn's zone is plan 01 · M7's `SafariGame`, as Kanto's and Johto's are:
  `safari start 500` gives thirty Safari Balls and Emerald's five hundred steps for $500, every wild Pokémon met while
  it runs is a Safari battle, and the last step or ball runs `common.SafariTimeUp` or `SafariOutOfBalls`, which bring a
  player of Hoenn back to the gate's door (`VAR_SAFARI_ZONE_STATE` 1); the gate's arrival walks them out past the
  counter, as Emerald's `ExitSafariZone` does.
- **The counter asks nothing first**: Emerald's attendant wants a Pokéblock Case before taking the fee, and turns away a
  player whose boxes are full. Pokéblocks are plan 21 · H11's, and no Safari gate of this game asks after the boxes
  (the Great Marsh's, Kanto's and Johto's don't), so the counter only asks and takes the money. The feeders in the zone are left for H11 too.
- **The zone is one map**: Emerald's six parts are separate maps joined at their edges; the importer lays them side by
  side as one (`HoennSafariZone`), each part its own area with its own table, so walking from one to the next is a step
  and not a warp. Their keys take the region's prefix (`hoenn_safari_zone_*`), since FireRed's are `safari_zone_*`.
- **The attendant inside the door** stands before it as Emerald's does, so the zone is only left through him: the first
  time he wishes the player luck, and after that he asks whether to end the game early. Spoken to with no game on (a
  save loaded in the zone after a game ended) he shows the player out. Emerald moves him aside as the player comes in;
  the counter here brings the player in a step south of him.
- **The expansion** (the north-east and south-east parts) stays behind its two builders until Hoenn's Hall of Fame,
  which sets `FLAG_HIDE_SAFARI_ZONE_SOUTH_CONSTRUCTION_WORKERS` (plan 21 · H9).
- **Team Aqua on Route 121**: the three grunts at its west end are met at any of the area's four coordinate events,
  as Emerald's are, and walk off south onto the pier toward Mt. Pyre. The order they were given is ours.
- **Fortree's trade**: the boy's Plusle, Pluses, comes for a Volbeat, holding Grass Mail (Emerald's is Wood Mail, which
  the item table hasn't got) and with Emerald's own contest condition, five numbers and a sheen, where Platinum's trades give one
  number to all five: the trade record (`NpcTrades`) carries them.
- **The Hidden Power quiz**: the old woman's three hands are right, right and left, as Emerald's are; a wrong guess
  ends the round and she may be asked again.
- **The Sleep Talk tutor** teaches once, for nothing, to any Pokémon that could learn Sleep Talk from Platinum's TM82;
  Emerald's own list of who may learn a tutor's move is not read, as with Kanto's tutors (`MoveTutors.Kanto`, which is
  every region's table of tutors of one move).
- **The Ancient Tomb** is open, as Emerald's is; its raised dots are read as words of ours, since Braille is not
  drawn. Flash in its chamber opens nothing and Registeel is not on the map until the Sealed Chamber is read (plan 21 ·
  H8 and after). The Scorched Slab plays Petalburg Woods' song and holds TM11, as Emerald's does.
- **Lavaridge's buried trainers**: Emerald's Gym hides its trainers in its sand on some of its warp tiles. A buried
  trainer jumps out at whoever comes near and stays on the patch once beaten, so Emerald's player never takes those
  warps; the reader writes a warp under a `TRAINER_TYPE_BURIED` object as a landing, which leads nowhere (Gerald's at
  2, 15 was one nobody could reach).
- **What waits** (part c, below): the bridges' metatiles once the Kecleon has fled.

## Gabby and Ty, the Wingull and the planks (2026-10-20, plan 21 · H6, its third part)

Gabby and Ty's battles and interviews, Fortree's fourth house and the plank bridges are scripts and rules of ours on
Emerald's beats (`common.txt`: `HoennChapterSevenC`, `GabbyAndTyUpdateLocation`, `GabbyAndTyGabby`, `GabbyAndTyTy`,
`GabbyAndTyFight`, `GabbyAndTyAfterTheBattle`; `fortree_city_house_4.txt`; `Graphics/FieldLife.Planks.cs`), under
Emerald's flags and variables (`VAR_GABBY_AND_TY_BATTLE_NUM`, the eight `FLAG_HIDE_ROUTE_1xx_GABBY_AND_TY_n`,
`FLAG_GABBY_AND_TY_ON_AIR`, `FLAG_WINGULL_SENT_ON_ERRAND`, `FLAG_HIDE_FORTREE_CITY_HOUSE_4_WINGULL`,
`FLAG_HIDE_MOSSDEEP_CITY_HOUSE_2_WINGULL`). **No story or save version is raised**: `HoennChapterSevenC` hides the
pair and the Mossdeep Wingull and shows the Fortree one, `HoennNewGame` calls it, and the arrival scripts of Routes 111,
118, 119 and 120 and of Fortree City call it for a save that never ran it (`FLAG_HOENN_CHAPTER_SEVEN_C`, a flag of
ours). What differs, and why:

- **One script for every route**: Emerald has a script to each of the pair's nine places; the three routes share
  `GabbyAndTyGabby` and `GabbyAndTyTy`, which pick the party by the count of battles. The count goes 0 to 8 and, past
  the ninth battle, back to 6 (Emerald's remainder), so from the sixth party on the pair go round Routes 111, 118 and
  120 with the sixth party for good. The six parties are Emerald's (`hoenn_gabby_and_ty_1` to `6`, class "Interviewer",
  a double battle).
- **Temporary flags**: Emerald's trainer flag and `FLAG_TEMP_SKIP_GABBY_INTERVIEW` are one local flag here
  (`FLAG_MAP_LOCAL_GABBY_AND_TY_BEATEN`), cleared when the player leaves the route: the pair fought on this visit only
  say they are watching, as the beaten trainer's second talk does. The player needs two Pokémon able to be sent out
  together; one is told to come back with two.
- **Two words for one**: Emerald asks for one easy-chat word. This game's phrase picker (plan 08 · P9) takes two, so
  the answer is two words (`phrase GABBY_AND_TY`), kept until the next battle, quoted back at it, and read in the
  segment. Backing out of the picker refuses the interview, as saying no does.
- **What the battle is remembered for**: Emerald notes whether the player threw a ball, used a healing item, lost a
  Pokémon, took fewer than the usual number of turns or won in a single turn (its trivia, `GabbyAndTyTrivia`). This game
  counts two of those, as the host can tell them: a battle over at once (`TURNS == 0`, Emerald's "short battle") and a
  Pokémon of the team fallen in it (`FAINTED` rose). A thrown ball or a healing item in a double battle against the
  trainers of the pair is not counted, so those two bits of trivia are left.
- **The segment on the air**: an interview given sets `FLAG_GABBY_AND_TY_ON_AIR` (Hoenn's televisions only, among the
  programmes `Broadcasts.On` offers) and `FLAG_GABBY_AND_TY_SHOW_NEW`, so the first television looked at afterwards
  shows it, as Jubilife's interview is. The next battle with the pair takes it off the air until it is interviewed
  about again, as Emerald's `gabbyAndTyData.onAir` is cleared when the pair are fought.
- **The Wingull**: the boy sends it out the first time he is spoken to, and it runs off through the door. Emerald shows
  Mossdeep's Wingull then (the other half of the errand); here that bird stays hidden in a town that is not built yet,
  and the boy says the Wingull is away on an errand ever after. The letter delivered, the Wingull home again and the
  boy's Mental Herb are plan 21 · H8's, written with Mossdeep, so nothing here asks for those flags yet.
- **The planks**: Emerald's `FortreeBridgePerStepCallback` lowers the plank's metatile under the player for a few
  frames and springs the one they left. The plank's picture here is baked into its chunk, so the walker's sprite does
  the dipping, for Hoenn's `Bridge` tiles alone (Sinnoh's decks are stone and iron, and share the behaviour). The
  numbers are the style guide's ("Planks that give"); the sound is `bridge_walk`, ours.
- **What waits**: the metatile swap of the bridge once Fortree's Kecleon has fled (an alternate chunk of the importer's);
  the Wingull's return and Mental Herb (H8).

## Lilycove City and Mt. Pyre (2026-10-24, plan 21 · H7, its first part)

Lilycove City, Routes 122 and 123 and Mt. Pyre are scripts of ours on Emerald's beats (`lilycove_city.txt`, the
store's floors, the motel, the Move Deleter's, two houses, `route_122.txt`, `route_123.txt`, the Berry Master's house,
`mt_pyre_1f.txt`, `mt_pyre_summit.txt`), under Emerald's flags and variables (`FLAG_VISITED_LILYCOVE_CITY`,
`FLAG_MET_RIVAL_LILYCOVE`, `FLAG_DECLINED_RIVAL_BATTLE_LILYCOVE`, `VAR_MT_PYRE_STATE`, `FLAG_RECEIVED_RED_OR_BLUE_ORB`,
`FLAG_HIDE_MT_PYRE_SUMMIT_ARCHIE`, `FLAG_HIDE_MT_PYRE_SUMMIT_TEAM_AQUA`, `FLAG_HIDE_JAGGED_PASS_MAGMA_GUARD`, the daily
`FLAG_DAILY_*` of the berries and the lottery). **No story or save version is raised**: `common.HoennChapterEight` hides
what Emerald's new game hides here (Maxie on the summit, the museum's patrons and tourists, the Fan Club's interviewer,
the motel's game designers, the roof's sale woman), `HoennNewGame` calls it, and Lilycove City's, Route 122's and Route
123's arrivals call it for a save that never ran it (`FLAG_HOENN_CHAPTER_EIGHT`, a flag of ours). What differs, and why:

- **The rival leaves on foot**: Emerald's rival flies off on a bird (`FLDEFF_NPCFLY_OUT`); here the picture goes dark
  and the rival is gone, as on Route 119. Emerald also puts the rival back in their bedroom at home afterwards
  (`FLAG_HIDE_LITTLEROOT_TOWN_MAYS_HOUSE_RIVAL_BEDROOM` cleared); that bedroom's lines are part b's to write, so the flag
  is left.
- **The Wailmer are in the way only on their own tiles**: Emerald draws Team Aqua's three Wailmer into Lilycove's
  metatiles east of the city (`LilycoveCity_EventScript_SetWailmerMetatiles`, local 76 to 78, 12 to 17). The reader's
  map leaves the sea round those tiles open, wide enough to sail east to Route 124 past them, so the three stand as
  Pokémon of the map on the tiles of their heads and the sea to Route 124 is open from Lilycove (Route 124 is no longer
  `WorldWalk.OpenedAhead`). Mossdeep and the east sea are shut anyway until plan 21 · H8; part b decides whether the
  channel needs a gate of ours, and takes the Wailmer away with `FLAG_TEAM_AQUA_ESCAPED_IN_SUBMARINE`.
- **The store's lift is shut**: Emerald's lift is a menu of floors that the stairs reach as well; its doors are
  `locked` on every floor. The fifth floor's clerks, who sell decorations for secret bases, only talk until plan 21 · H10.
  The roof's clearance sale (a woman shown on some days) stays hidden.
- **The Lottery Corner is Jubilife TV's draw** (`lottery draw`, `Models/Lottery.cs`), with Emerald's prizes: two digits a
  PP Up, three an Exp. Share, four a Max Revive, five a Master Ball; one digit wins nothing, as in Emerald.
- **The Giga Drain girl asks the team's species**: Emerald asks whether a Grass type is on the team
  (`IsGrassTypeInParty`); a script here asks species by name, so she looks for the Grass Pokémon of Hoenn's routes so far
  (the Treecko, Lotad, Seedot, Shroomish, Oddish, Cacnea and Lileep lines, Roselia and Tropius).
- **The Berry Master's wife takes a phrase of two words** (`phrase BERRY_MASTERS_WIFE`), where Emerald takes one of its
  easy-chat sayings; Emerald's five sayings that bring a rare berry once (Spelon, Pamtre, Watmel, Durin, Belue) are words
  the picker does not have, and wait for part b. Her ordinary berry is one of Cheri to Sitrus, by a die of ten.
- **Mt. Pyre's summit plays once**: the old woman gives the **Magma Emblem** (a key item of ours, 9112, in the
  importer's overrides), Team Aqua is gone and `VAR_MT_PYRE_STATE` is 1. Emerald's second scene there (state 2, the two
  leaders bringing the orbs back after Sootopolis) is plan 21 · H8's; its triggers are left without scripts, and the old
  couple's later lines with them. The mountainside's fog stands above its middle as a zone (Emerald's fog and sun
  triggers), and the summit is in its header's fog.
- **Lilycove's ground**: every metatile of its tileset that is open more often than blocked is paving (`GbaCover.HoennRanges`'
  `gTileset_Lilycove`), as Lavaridge's rows are read; the cove's open ground in the General tileset is left to the
  builder's guess, since those rows are shared by all of Hoenn.

## Team Magma's and Team Aqua's hideouts (2026-10-24, plan 21 · H7, its second part)

Jagged Pass's way in, Team Magma's hideout, Slateport's harbour scene and Team Aqua's hideout are scripts of ours on
Emerald's beats (`jagged_pass.txt`, `magma_hideout_4f.txt`, `slateport_city.txt`, `slateport_city_harbor.txt`,
`aqua_hideout_1f.txt`, `aqua_hideout_b1f.txt`, `aqua_hideout_b2f.txt`), under Emerald's flags and variables
(`VAR_JAGGED_PASS_STATE`, `FLAG_BEAT_MAGMA_GRUNT_JAGGED_PASS`, `FLAG_GROUDON_AWAKENED_MAGMA_HIDEOUT`,
`FLAG_HIDE_MAGMA_HIDEOUT_GRUNTS`, `VAR_SLATEPORT_CITY_STATE`, `VAR_SLATEPORT_HARBOR_STATE`, `FLAG_MET_TEAM_AQUA_HARBOR`,
`FLAG_DEFEATED_ELECTRODE_*_AQUA_HIDEOUT`, `FLAG_TEAM_AQUA_ESCAPED_IN_SUBMARINE`, `FLAG_HIDE_LILYCOVE_CITY_AQUA_GRUNTS`).
**No story or save version is raised**: `common.HoennChapterEightB` hides what Emerald's new game hides for them
(Groudon awake, Captain Stern and Gabby and Ty in Slateport's street, Stern, Archie and his grunt in its harbour),
`HoennNewGame` calls it, and Jagged Pass's, Slateport City's and Lilycove City's arrivals call it for a save that never
ran it (`FLAG_HOENN_CHAPTER_EIGHT_B`). What differs, and why:

- **The rock on Jagged Pass is a way that the story opens**: Emerald draws rock over the hideout's mouth until
  `VAR_JAGGED_PASS_STATE` is 2 (`setmetatile`). Here the warp is an overlay's `switches` entry there `from`
  `FLAG_HOENN_MAGMA_HIDEOUT_OPEN`, a flag of ours the shaking scene sets with the variable, so its tile is a wall until
  then; the mouth is drawn as the import has it, open, in both states.
- **Groudon wakes and is gone with no battle**, as Emerald has it: the orb's light is the scene effects' sparkles and a
  burst, its rising and going two walks and the camera shaken twice, and both of Emerald's Groudon objects are hidden
  for good. `hoenn/awaken_legend` (ours) plays once under it through `music`, since `fanfare` knows only the item kinds.
  Maxie battles once spoken to (`hoenn_maxie_magma_hideout`) and sets what Emerald sets: Slateport's two scenes ready.
- **Captain Stern's interview is short**: Emerald moves Slateport's crowd to stand round the camera and gives each a line
  of it; here Stern, Gabby and Ty stand where Emerald's objects stand, the crowd keeps its places and lines, and the
  street's scene warps the player into the harbour as Emerald's does.
- **The submarine is not drawn**: Emerald draws only its shadow in the water (`submarine_shadow`), at Slateport's berth
  and in the hideout's pen; this game has no such picture, so Archie and his grunt leave the quay and a line says the
  submarine slid away, and so does Matt's. The harbour's patrons are hidden from Groudon's waking on
  (`FLAG_HIDE_SLATEPORT_CITY_HARBOR_PATRONS`), and Emerald brings them back after Mossdeep's Gym (plan 21 · H8).
- **The two Electrode look like Poké Balls**, as Emerald's do: they are item balls of the overlay with a script of their
  own, which cries, says what the ball was and battles (level 30); won, run from or caught, each is gone for good.
- **The channel east of Lilycove is shut on Emerald's twelve tiles**: the three Wailmer stand on the tiles of their
  heads and the other nine tiles of their two-by-two bodies are things in the way that are not drawn (`Unseen`), all
  hidden by `FLAG_TEAM_AQUA_ESCAPED_IN_SUBMARINE`. No gate of ours is added beyond them: the reader's sea is open further
  from the city, and whatever lies past it (Mossdeep, the east sea) is shut until plan 21 · H8 anyway.
- **Matt notices the player** on the area's two triggers by a temporary variable (`VAR_TEMP_1`), which the room's arrival
  sets back to 0, or to 1 once the submarine has gone, as Emerald's temporary variables are cleared on a map's load.
- **The rival is home after Lilycove**: the battle before the Department Store clears
  `FLAG_HIDE_LITTLEROOT_TOWN_MAYS_HOUSE_RIVAL_BEDROOM` (the rival's family is always the east house's here), and the rival
  has a line of their own there. Emerald's Pichu doll in the room is left.
- **The hideouts' music** is one song of ours for both, `hoenn/aqua_magma_hideout`, as Emerald plays one. Team Magma's
  halls take the volcanic climate's light (`climate: Volcanic`); Team Aqua's are rooms (`Indoors`), their warp panels
  the reader's `WarpPanel` tiles.

## Mossdeep City (2026-10-24, plan 21 · H8, its first part)

Routes 125 to 127 above water and their sea floors, Mossdeep City and its rooms and the treasure hunter's house on
Route 124 are open, and the stretch from Mossdeep's Gym to Dive is scripts of ours on Emerald's beats
(`mossdeep_city*.txt`, `route_124_diving_treasure_hunters_house.txt`, `route_125.txt` to `route_127.txt`), under
Emerald's flags and variables (`VAR_MOSSDEEP_CITY_STATE`, `VAR_MOSSDEEP_SPACE_CENTER_STATE`,
`VAR_MOSSDEEP_SPACE_CENTER_STAIR_GUARD_STATE`, `VAR_STEVENS_HOUSE_STATE`, `FLAG_DEFEATED_MAGMA_SPACE_CENTER`,
`FLAG_RECEIVED_HM_DIVE`, `FLAG_WINGULL_DELIVERED_MAIL`). **No story or save version is raised**:
`common.HoennChapterNine` hides what Emerald's new game hides for them (Team Magma before the Space Center and on its
two floors, Steven upstairs there and at home, the Beldum's ball and his letter, and Mr. Briney at Stern's shipyard
until the Mind Badge), `HoennNewGame` calls it, and Mossdeep City's arrival calls it for a save that never ran it
(`FLAG_HOENN_CHAPTER_NINE`). What differs, and why:

- **Dive is HM09**: Emerald's HM08 is Dive, but Platinum's eight HMs keep their numbers (HM08 is Rock Climb), so
  Steven gives a ninth, an item of the importer's overrides (`HM09`, id 9113). Whoever learns Dive by a machine in any
  game PokeAPI lists may learn it (`Importer.AddHoennMachines`), Platinum's own species too, since Platinum taught Dive
  by none; like every HM it can't be forgotten to make room.
- **The Gym's floor of arrows** is the room's puzzle (`MossdeepTiles`, Emerald's `rotating_tile_puzzle.c`): eleven loops
  of four arrows in five colours, their tiles hand-written from the layout (numbers), and a switch of a colour (the
  room's coordinate events, a new script command, `rotatetiles`) moves everyone standing on that colour's arrows one step
  the way the arrow points, keeping their facing, and turns each a quarter anticlockwise, all at once. Emerald turns them
  after the step; here they turn as they set off. Where they stand lasts until the room is come to again, as Emerald's
  copies of its objects do. The stone statues are things of the map (`GymStatue`), drawn as cards, and the pad by the
  twins sends the player back to the door. Emerald's leftover switches of Ruby and Sapphire (`FLAG_MOSSDEEP_GYM_SWITCH_*`)
  are not followed.
- **Tate and Liza are one trainer of two** (`hoenn_tate_and_liza_1`, a double battle), as Emerald's are, their name in
  the table "Tate & Liza" (the reader now keeps a pair's names apart at an ampersand with no spaces). Emerald refuses the
  battle to a team with one Pokémon able to fight; here a team of one fights it, as every double battle of this game does.
  Their Badge sets what Emerald's does: the twelve trainers beaten, Team Magma before the Space Center and on both its
  floors, Steven upstairs, Slateport's harbour full again and Mr. Briney helping Dock at the shipyard.
- **Team Magma files into the Space Center** at the first step onto Emerald's six triggers once the Badge is won, with a
  line of Maxie's that Emerald's scene doesn't have.
- **The grunt on the stairs steps aside** west of himself when the player comes at him from the east and east otherwise,
  as Emerald's script means to (`VAR_MOSSDEEP_SPACE_CENTER_STAIR_GUARD_STATE` 1 or 3); by a slip Emerald sends him east
  whatever the player's side, and a game returning finds him at 13,3, which is not followed.
- **The battle beside Steven** is the game's tag battle (`battle maxie as … and tabitha with "hoenn_steven_partner"`):
  Emerald asks the player to choose three Pokémon and fights with those; here the whole team fights, as in every battle
  beside a partner of this game. Steven's own three are Emerald's `sStevenMons` (Metang, Skarmory and Aggron at 42 to
  44, every IV at its most, Brave, Impish and Adamant), which `trainers.h` doesn't hold: the reader writes them as
  `hoenn_steven_partner` from the numbers of `src/battle_tower.c`. Steven and Wally are of the rival's class in Emerald's
  table and keep their own names (`TrainerDatabase.OthersOfTheRivalsClass`), where the class alone had named them
  `{rival}` (Wally's battle in Mauville City was named so).
- **The three grunts upstairs** ask whether the player will take them on, as Emerald's do, and send a player who says no
  back down to the foot of the stairs (Emerald puts them on the stairs' own tile; here the tile before them).
- **The launch counter** says only that the next launch is soon: Emerald counts the launches by the weeks of the game,
  which this game doesn't count.
- **Mossdeep's people** who talk of Pokéblocks and secret bases only talk (plan 21 · H10 and H11), the Super Rod has a
  flag of Hoenn's own (`FLAG_HOENN_RECEIVED_SUPER_ROD`, since Sinnoh's Fight Area gives one under Emerald's name), the
  King's Rock and the Sun Stone are given once, and the black belt by the shore teaches Dynamic Punch once (`tutor once`,
  its nearest kin among Platinum's machines Focus Punch). The Game Corner, Emerald's room of link games, stays shut.
- **The Wingull's errand** is whole: the Wingull of the house in Mossdeep, there once Fortree's boy has sent it off, cries
  and flies home when spoken to (`FLAG_WINGULL_DELIVERED_MAIL`), and the boy gives a Mental Herb once.
- **The treasure hunter** offers all four shards in one question and turns down one not in the bag, where Emerald's menu
  lists only those the bag holds.
- **The eastern sea** plays Route 120's song, Route 124 too now that it is written. Route 125 has no dark water
  until the post-game's Marine Cave (Emerald's abnormal weather lays it), so its sea floor is the post-game's (plan 21
  · H12). Emerald's shallows (`MB_SHALLOW_WATER`) are water to look at over all of Hoenn (`GbaCover.Of`), where they had
  taken the nearest ground.
- **Mossdeep is built as Starward** (white stucco under sky-blue hip roofs, shutters and flower boxes; the style guide's
  row), and its open metatiles are its sandy paths (`GbaCover.HoennRanges`).

## The Seafloor Cavern and Sootopolis (2026-10-24, plan 21 · H8, its second part)

Route 128 and its sea floor, the Seafloor Cavern (the pool under the sea before it, its mouth and nine rooms),
Sootopolis City with the pool under its harbour, its Pokémon Center, Mart and eight houses, and the Cave of Origin's
three rooms are open, and the stretch from Kyogre's waking to Wallace's leaving for Sky Pillar is scripts of ours on
Emerald's beats (`route_128.txt`, `underwater_seafloor_cavern.txt`, `seafloor_cavern_*.txt`, `sootopolis_city*.txt`,
`cave_of_origin_b1f.txt`), under Emerald's flags and variables (`VAR_SEAFLOOR_CAVERN_STATE`, `VAR_ROUTE128_STATE`,
`VAR_SOOTOPOLIS_CITY_STATE` 0 to 3, `FLAG_KYOGRE_ESCAPED_SEAFLOOR_CAVERN`, `FLAG_LEGENDARIES_IN_SOOTOPOLIS`,
`FLAG_SYS_WEATHER_CTRL`, `FLAG_STEVEN_GUIDES_TO_CAVE_OF_ORIGIN`, `FLAG_WALLACE_GOES_TO_SKY_PILLAR`). **No story or save
version is raised**: `common.HoennChapterTen` hides what Emerald's new game hides for them (Archie, Maxie and Steven on
Route 128, the cavern's last hall's people and the waking Kyogre, Sootopolis's Steven, Archie, Maxie, titans, Rayquaza,
Wallace and watching townsfolk, and Wallace at Sky Pillar), `HoennNewGame` calls it, and the arrival scripts of Route 128,
the cavern's last hall and Sootopolis call it for a save that never ran it (`FLAG_HOENN_CHAPTER_TEN`). What differs, and
why:

- **A floor that is no map of the sea's** is dived to and come up from at one place, as Emerald's `setdivewarp` in a
  map's own script sends it: the overlay's `diveTo` and `emergeTo`. Coming up anywhere in the pool before the cavern comes
  up in the cavern's mouth at 10, 17; its dark water goes down to the pool at 6, 5; Sootopolis's harbour dives to the pool
  under it at 9, 6, which comes up in the harbour at 29, 53. The passages along the floor (Route 128's into the cavern's
  pool, Route 126's under the crater's wall) are warps a diver swims onto, as Emerald's are.
- **The abnormal weather** is Emerald's `WEATHER_ABNORMAL`, the downpour and the drought by turns: a new field weather,
  `Drought` (a hard white sun, the battle's harsh sun laid over the hour's light at seven tenths; a battle under it opens in
  the sun, as Emerald carries its drought over), and `Abnormal`, which turns between the two every ten seconds of the
  field's clock, the downpour first (our measure; Emerald's own count is in code this import doesn't read). It lies over
  Routes 124 to 128, Lilycove City and Mossdeep City while `FLAG_SYS_WEATHER_CTRL` is set (Emerald's
  `Common_EventScript_SetAbnormalWeather` on every map of the eastern sea; Routes 129 to 131, Ever Grande City and Sky
  Pillar's outside join with them, part c), as overlay zones with `while` a flag; the routes and Sootopolis play
  `hoenn/abnormal_weather` under it, the two towns their own songs. In Sootopolis the downpour alone falls from the
  titans' coming until their clash is seen (`FLAG_HOENN_SOOTOPOLIS_CLASH_SEEN`, a flag of ours, Emerald's
  `SootopolisCity_EventScript_SetWeather` reading its state), and the two by turns after it; Rayquaza's coming must clear
  that flag with Emerald's own (part c).
- **Kyogre wakes as Groudon did**, with no battle: the Red Orb's light, Kyogre rising and gone, the cavern shaking, steps
  on the spot where it lies in its pool. Archie's call from outside is told by him alone, with the PC's sounds.
- **Route 128's scene** is the route's own arrival, which Emerald plays as the frame's first map script; Steven comes and
  goes on his Skarmory's cry, where Emerald draws the flying Pokémon.
- **The clash over Sootopolis** is a camera sent to the harbour, the weather breaking, and the two titans striking at each
  other three times (steps on the spot, cries, the camera shaken), from wherever the player has come into the city.
  Emerald lays the city out again for the clash (`LAYOUT_SOOTOPOLIS_CITY_LEGENDS_BATTLE`, rough water under the two),
  which is not followed: they stand in the harbour as it is.
- **Steven's guide** to the Cave of Origin is said and then put by a fade outside the cave's mouth, where Emerald walks
  both across the city's terraces. Afterwards the old man before the cave steps aside when spoken to from the south (and
  stays aside until the city is come to again); Emerald puts him aside whenever the city is come to.
- **Wallace's question** lists Sky Pillar first: Emerald's four answers come back to the question but one, and the tests
  that play every way through a script answer a question asked again as it was first answered.
- **The houses' doors** are shut while the titans are in the city (switches until `FLAG_LEGENDARIES_IN_SOOTOPOLIS`), as
  Emerald locks them until Archie and Maxie leave; reading one says it is shut. Juan's Gym stays shut for part c, and so
  does the Mystery Events house. The townsfolk who come out to watch stand where Emerald stands them for states 1 to 5.
- **Sootopolis's people**: Kiri gives two berries a day, one of six and a Figy or an Iapapa Berry (Emerald's first is one
  of a range of berries; here the six of Emerald's range this game has the commonest); the black belt gives TM31, Brick
  Break, once; the woman of the Pokémon Center teaches Double-Edge once (`FLAG_MOVE_TUTOR_TAUGHT_DOUBLE_EDGE`); the two
  brothers measure Seedot and Lotad against records of their own (`VAR_SEEDOT_SIZE_RECORD`, `VAR_LOTAD_SIZE_RECORD`) and
  give an Elixir for a bigger one; the woman with the Wailmer Doll keeps it until there is a Secret Base to put it in
  (plan 21 · H10); the Mart sells Emerald's stock but its mail.
- **Sootopolis is built as Crater** (pale stone under deep sea-blue hip roofs with shutters; the style guide's row), its
  white stone floor paving (`GbaCover.HoennRanges`). Its spawn for Fly and a white-out (Emerald's heal location 10) stands
  on a map of its own, which the region's spawns can't yet say: it waits for part c (or plan 18 · W9's maps for Fly).

## Sky Pillar, Rayquaza and the Rain Badge (2026-10-24, plan 21 · H8, its third part)

Routes 129 to 131 and Pacifidlog Town with its Pokémon Center and five houses, Sky Pillar from the cave on Route 131's
shore to its top, and Juan's Gym with its basement are open, and the stretch from Wallace at Sky Pillar's door to the
Rain Badge is scripts of ours on Emerald's beats (`sky_pillar_outside.txt`, `sky_pillar_top.txt`, `sootopolis_city.txt`,
`sootopolis_city_gym_1f.txt`), under Emerald's flags and variables (`VAR_SOOTOPOLIS_CITY_STATE` 3 to 6,
`VAR_SKY_PILLAR_STATE`, `VAR_SKY_PILLAR_RAYQUAZA_CRY_DONE`, `VAR_SOOTOPOLIS_WALLACE_STATE`, `FLAG_MET_MAXIE_SOOTOPOLIS`,
`FLAG_MET_ARCHIE_SOOTOPOLIS`, `FLAG_SOOTOPOLIS_ARCHIE_MAXIE_LEAVE`, `FLAG_RECEIVED_HM_WATERFALL`,
`FLAG_RECEIVED_TM_WATER_PULSE`). **No story version is raised**: `common.HoennChapterEleven` (`FLAG_HOENN_CHAPTER_ELEVEN`)
hides Rayquaza come back to the top, `HoennNewGame` calls it, and Sootopolis's and Sky Pillar's top's arrivals call it for
a save that never ran it. What differs, and why:

- **Sky Pillar is laid clean**, as Emerald's floors' own scripts lay them until Rayquaza has come down
  (`GbaMaps.FirstLayouts`: each floor's `LAYOUT_SKY_PILLAR_*_CLEAN`, and Route 131 always with the island, as its
  `ON_TRANSITION` lays it). The clean fourth floor keeps five cracked tiles, and they are the way on: a cracked floor whose
  area names the floor below (the overlay's `holeTo`, Emerald's `setholewarp`) drops whoever walks onto it to the same
  tile there (`Map.HoleTargetAt`), into the third floor's middle, whose stairs lead to the fifth. On the Bicycle it is
  ridden over (Emerald asks the Mach Bike's speed; here any bicycle). The floors cracked all over after the descent, the
  Mach Bike's climb and Rayquaza's battle are the post-game's (plan 21 · H12).
- **Wallace at the door** walks over and speaks where he stands; Emerald walks both a few steps toward the door before the
  second quake. The door is the overlay's (`doors`, opened by `FLAG_WALLACE_GOES_TO_SKY_PILLAR`), since the import leaves
  Emerald's closed door (a blocked tile no behaviour names) leading nowhere.
- **Rayquaza wakes** with no battle, as Emerald's does: the camera on it, its roars, the tower shaking, and it gone. Its
  coming over Sootopolis is the city's own arrival the first time after (`VAR_SKY_PILLAR_STATE` 1): the camera to the
  harbour, the titans gone behind a fade, Rayquaza over the water to `hoenn/rayquaza_appears` (ours, played once), its
  roars, and the weather clearing with Emerald's three flags and ours (`FLAG_SYS_WEATHER_CTRL`,
  `FLAG_LEGENDARIES_IN_SOOTOPOLIS`, `FLAG_HOENN_SOOTOPOLIS_CLASH_SEEN` cleared; `VAR_SKY_PILLAR_STATE` 2). Emerald lays rough
  water under it and warps the player out of a white flash to a fixed tile; here the player stays where they came in, and
  a line tells of the storm breaking.
- **Archie and Maxie** leave once both have been spoken to (Emerald's `MaxieArchieLeave`) behind a fade; Emerald then sends
  them to Mt. Pyre's summit to give back the orbs (`VAR_MT_PYRE_STATE` 2), which is left for the session that writes that
  scene, since the summit stands them both on one tile. Their leaving opens the Gym (the overlay's door, opened by
  `FLAG_SOOTOPOLIS_ARCHIE_MAXIE_LEAVE`, Emerald's `LockGymDoor`); the houses open as the titans go, where Emerald waits for
  the leaders too.
- **Juan's ice** is Emerald's step callback and the room's scripts (`SootopolisIce`): thin ice cracks under a first step
  and counts, a second step drops the player to the basement onto the same tile, the count is one on every arrival, and
  the glazed stairs (Emerald's `MB_SLIDE_SOUTH`, which slides a climber back down) become plain stairs at 8, 28 and 67. The
  cracks and the glaze are drawn by the room's pieces (style guide, "The Sootopolis Gym's ice"). Juan's Badge counts the
  basement's ten trainers beaten, gives TM03 and registers him in Match Call, and calms the city for good (state 6).
- **Sootopolis's spawn** names its own map (`RegionSpawn.Map`, written by the importer for a heal location off the
  overworld; Littleroot's two houses, the Southern Island and the Battle Frontier gain theirs the same way), and the town map
  puts the city on the middle of Route 126's cells with a Fly spot. A white-out in Hoenn still wakes at home (plan 18 · W9).
- **Pacifidlog** is built as Raft (logs under straw-gold gable roofs; the style guide's row), its log walkways a deck of
  logs (`GbaCover.HoennRanges`). Its people talk; the Fan Club's younger brother's daily Return or Frustration (a reading of
  the lead's friendship by a script), the trader's trade, the Explosion tutor and Mirage Island wait for their sessions.
  Routes 129 to 131 lie under the titans' weather while it lasts, as the rest of the eastern sea.
- **The import's bridges and shallows** (found by the wave 44 run): water that a bridge crosses now lies at the lowest level
  of its shores, and a bridge's run is read to its ends however long (Route 120's pond took its west half's level from
  the bridge's raised ends, and the middle of its sixteen tiles no deck), so Route 120's bridge is a deck over its pond,
  with Steven standing on it (an object of a bridge's elevation over a deck stands on the deck), and Route 119's and
  Route 104's decks over their rivers with it. Emerald's shallows out at sea that touch no shore are a sandbar to look at
  (Route 128's islet, where Archie, Maxie and Steven stand), where part a made every shallow water to look at.

## Shoal Cave and its tide, Routes 132 to 134 and Pacifidlog's three (2026-10-24, plan 21 · H8, its fourth part)

Shoal Cave's five rooms (the entrance, inner, stairs, lower and ice rooms) and Routes 132 to 134 west of Pacifidlog back
to Slateport City are open; Route 125's way into the cave is no longer locked. What differs from Emerald, and why:

- **The tide** is Emerald's `UpdateShoalTideFlag` (`src/time_events.c`; `Overworld/ShoalTide.cs`): high from midnight to
  three, nine to three and nine to midnight, low in the six hours between, and read only as the player comes in from
  outdoors (`GetLastUsedWarpMapType`), so it never turns under them inside. It is the script command `tide`, run by the
  entrance room's `OnTransition`, a place's script the game now runs to its end behind the warp's fade before anything of
  the place is drawn (Emerald's `MAP_SCRIPT_ON_TRANSITION`; `GameEngine.RunTransitionScript`, on a host that shows
  nothing), and `FLAG_SYS_SHOAL_TIDE` lays Emerald's high-tide layouts over the entrance and inner rooms through
  `world.json`'s `swaps` (the reader's `GbaWorld.Alternates`), where Emerald's `setmaplayoutindex` swaps the whole layout.
  The game's own "came from outdoors" is the place left being open country and no cave; a loaded save comes from nowhere
  and keeps the tide it was saved with, where Emerald reads the saved last warp's map type.
- **The Shoal Salt and Shells** lie where Emerald has them (two salt piles and four shells in the inner room, a pile in the
  stairs room and one in the lower room, read in the ground), each found once a day under a flag of ours,
  `FLAG_DAILY_RECEIVED_SHOAL_SALT_1` to `_4` and `_SHELL_1` to `_4`, which the new day clears: Emerald's
  `FLAG_RECEIVED_SHOAL_*` are cleared only when the Shell Bell's old man is next spoken to after the day changed
  (`FLAG_SYS_SHOAL_ITEM`). The two inner piles are under the water at high tide (Emerald leaves their large piles off
  then). The piles and stones are not drawn larger or smaller as Emerald's `setmetatile` draws them. The old man makes a
  Shell Bell of four and four as often as they are brought; the lower room's Black Belt gives the Focus Band once.
- **The arrow warp at the lower room's west end**, whose tile says "out to the north" with rock there, puts whoever comes
  down it on the open tile beside it, as Johto's ways that face a wall do (`World.Join`, now for Hoenn too).
- **Routes 132 to 134** are Emerald's currents, carrying a surfer west (`TileBehavior.Current*`, plan 19 · K6), with their
  trainers and items; Route 134's dark water and the Sealed Chamber under it stay the post-game's (plan 21 · H12): the area
  names no floor to dive to.
- **Pacifidlog's Fan Club brother** reads the lead's friendship (`LEAD_FRIENDSHIP`, Emerald's `GetLeadMonFriendshipScore`:
  150 or more gives TM27 Return, under 50 TM21 Frustration, between nothing), **once a day** (`FLAG_DAILY_PACIFIDLOG_RECEIVED_TM`)
  where Emerald gives one a week (`VAR_PACIFIDLOG_TM_RECEIVED_DAY`, seven days): the plan's own word, and the game has no
  count of days a script can read. **The trader's** Horsea is Emerald's `INGAME_TRADE_HORSEA` (Seasor, Skylar's, 46285,
  personality 0x7F: a Brave male with Swift Swim; tough 30 and the rest 5, sheen 10) holding Bubble Mail for Emerald's Wave
  Mail, which the item table hasn't got (NpcTrades `seasor`). **The Explosion tutor** in the Pokémon Center teaches once in
  a game, by `MoveTutors.Kanto`'s table, Explosion being TM64's move (`FLAG_MOVE_TUTOR_TAUGHT_EXPLOSION`).

## Ever Grande City and the League (2026-10-25, plan 21 · H9, its first part)

Ever Grande City (`Architecture.Bloomstone`, its walks, steps and flower beds in `GbaCover.HoennRanges`), its Pokémon
Center on both floors, Victory Road's three floors and the Pokémon League's hall, halls, four rooms, Champion's room and
Hall of Fame are open, with scripts of ours on Emerald's beats (`ever_grande_city*.txt`, `hoenn_victory_road_1f.txt`). What
differs from Emerald, and why:

- **Victory Road is named apart.** Kanto's maps are called `VictoryRoad_1F` and the rest, so Hoenn's are
  `HoennVictoryRoad_1F`, `_B1F` and `_B2F` (their keys `hoenn_victory_road_*`, as the importer already prefixed them), and
  its items' flags take the region's name (`FLAG_ITEM_HOENN_VICTORY_ROAD_1F_PP_UP`, `FLAG_HIDDEN_ITEM_HOENN_…`;
  `GbaEvents.ItemFlag`): Emerald's `FLAG_HIDDEN_ITEM_VICTORY_ROAD_1F_ULTRA_BALL` is also FireRed's, and an Ultra Ball found
  in one region would have been gone from the other. FireRed's flags keep their names, which saves hold.
- **Wally** runs in along the passage below the west passage's two trigger tiles and battles (`hoenn_wally_vr_1`), and
  waits where he stopped (`VAR_VICTORY_ROAD_1F_STATE` 1 or 2, which the floor's arrival reads, Emerald's
  `setobjectxyperm`). A battle lost leaves the variable at nought and him below to try again. From the Hall of Fame he
  waits by the way out instead and battles once (`hoenn_wally_vr_2`); his Match Call rematches are not read.
- **The guards** stand shoulder to shoulder before the door (the hall's arrival puts them there until
  `FLAG_ENTERED_ELITE_FOUR`) and look for the Feather Badge alone, as Emerald's do (the only Badge a challenger can come
  without). The hall's arrival sets `FLAG_LANDMARK_POKEMON_LEAGUE`, by which Fly lands at the League's door from then on
  (the town map's Ever Grande cells split: the top two the League's, Emerald's `sMapFlyDestinations`), and makes the hall
  the place a battle lost wakes the player in (spawn 20, Emerald's `HEAL_LOCATION_EVER_GRANDE_CITY_POKEMON_LEAGUE`).
- **The League's rooms** are walked as Kanto's are: the way back out of each room and each hall is shut once the player is
  through (Emerald closes the room's door behind the challenger and swaps its tiles), the way ahead opens with the room's
  master beaten (`FLAG_DEFEATED_ELITE_4_SIDNEY` to `_DRAKE`, Emerald's; the Champion's door by `FLAG_HOENN_DEFEATED_WALLACE`,
  ours, where Emerald swaps the door's tiles in the scene), and the hall's arrival puts all of it back, the rival and the
  professor out of the Champion's room too, as Emerald's white-out and Hall of Fame do. Each way ahead puts the player on
  the next floor's first open row facing it, where Emerald stands them on the door and walks them in (`VAR_ELITE_4_STATE`'s
  scenes are not played). The four battle when spoken to, with Emerald's teams; there is no rematch team, as in Emerald.
- **The Champion's room** is one scene as the challenger comes in: up the room, Wallace and his battle, the rival running in
  with advice too late (May's lines for a boy, Brendan's for a girl), Professor Birch after, and Wallace leading on. Wallace
  goes through the door ahead and the scene warps the player into the Hall of Fame, where Emerald walks both through.
  Birch's rating of the Pokédex is a line, not Emerald's `ProfBirch_EventScript_RatePokedex` count.
- **The Hall of Fame**'s floor is Emerald's polished one (`MB_ICE`), so nobody walks there: Wallace is put beside the
  player and turns to them. The team is recorded, `HoennHallOfFame` and Emerald's `FLAG_IS_CHAMPION` set, the scene of the
  team's entry played, the team healed, the player sent home to the bedroom (Emerald's `setrespawn` to the 2F of the house)
  and the game saved before the credits, as Kanto's and Johto's are. Of Emerald's `SetGameClearFlags` the game keeps what it
  has places for: Wally to the way out, the Safari Zone's expansion opened (its builders gone, its attendant back), Steven
  gone travelling from his house with his letter and his Beldum's ball on the desk (the ball only for a player who has none
  of it yet), and the League's rooms put back. `FLAG_SYS_GAME_CLEAR` is FireRed's name too and is not set here.
- **The National Pokédex** is Birch's in his lab once Hoenn's Hall of Fame is entered, and asks nothing else, as Emerald's
  `LittlerootTown_ProfessorBirchsLab_EventScript_UpgradeToNationalDex` (the region file's `nationalUnlock` is `HallOfFame`).
- **Left for the second part**: home after the credits (Dad back at home and his S.S. Ticket, Mom's news), the television's
  report and the choice of Latias or Latios (a roamer of Hoenn's routes, which `Overworld/Roamers.cs` does not have yet), the
  harbour's ship on to the next region (`RegionDatabase`'s Hoenn link is still undecided), Scott's Battle Frontier pass,
  Wally's and the Four's rematches through Match Call, and the abnormal weather's other places of the post-game.

## Home as Hoenn's Champion, the eon roamer and the way on (2026-10-25, plan 21 · H9, its second part)

What the first part left of the League's end, with scripts of ours on Emerald's beats (`littleroot_town_brendans_house_1f.txt`,
`common.HoennChapterThirteen`). What differs from Emerald, and why:

- **Home.** The first way down the stairs once `HoennHallOfFame` is set plays one scene in the family's house: Mom's
  welcome, the television's report of a Pokémon nobody can name flying over Hoenn, her question of its colour, and Dad
  (Norman) home from Petalburg with the S.S. Ticket, who then goes back to his Gym. Emerald plays the report and Dad's
  visit as two beats of the same arrival; here they are one scene, played once (`VAR_HOENN_CHAMPION_HOME`, ours).
  `FLAG_HIDE_PLAYERS_HOUSE_DAD` is Emerald's; a new game sets it (`HoennChapterThirteen`), and the house's arrival gives
  it to a save made before it was written, which then plays the scene on its next way down.
- **The colour named roams.** RED sets Latias roaming and BLUE Latios, at level 40 (Emerald's `VAR_ROAMER_POKEMON` 0 and
  1, its `ROAMER_SPECIES`). The other of the two is Emerald's Southern Island's, reached with the Eon Ticket, which no
  scene gives here. The roamer goes over Emerald's twenty places (`sRoamerLocations`: Routes 110, 111 and 117 to 134,
  `Roamers.HoennRoutes`) by Platinum's rules of moving, which differ from Emerald's only in the odds: one time in
  sixteen anywhere, nearby otherwise, never to the place the player has just left. It is met on land and on the water
  where its place has a table, one time in two, as every roamer of this game is.
- **The S.S. Ticket** is Emerald's present, given for the S.S. Tidal and the Battle Frontier, both plan 21 · H12's: until
  then Lilycove's ferry desk says the Tidal takes no passengers today.
- **The way on to Unova is by ship from Lilycove City's harbour.** Emerald has no such crossing, so the captain is ours:
  he stands on the gangway beside the ferry desk (7, 10) once Hoenn's Hall of Fame is entered, as Sinnoh's captain does
  on Canalave's pier, and `common.Attendant` speaks for him. A Champion of Hoenn lands before the player's house in
  Aspertia City (Unova's `Arrival`). Plan 18's table proposed a ship from Slateport; Lilycove is where Emerald's ships
  for far places leave (the ferries to the islands of its events), and it has a room for the quay where Slateport's
  harbour is Captain Stern's and Team Aqua's.
- **Hoenn's bands.** The eight Leaders, the Elite Four and Wallace wear bands after their Gym's type or their room (the
  style guide's third table), none the base colour of a Sinnoh or Kanto band; Tate and Liza's double battle loses its
  band, as every double battle does.
- **Left**: Scott's Battle Frontier pass, Wally's and the Four's rematches through Match Call, Hoenn's table for plan 14 ·
  B11's tournaments, and the post-game's places (H12).

## Kanto's third chapter, second part (2026-10-18, plan 19 · K5)

From Celadon City to the Marsh Badge, scripts of ours on FireRed's beats (`celadon_city_*.txt`, `rocket_hideout_*.txt`,
`pokemon_tower_6f.txt`, `pokemon_tower_7f.txt`, `lavender_town_volunteer_pokemon_house.txt`, `route_12.txt`,
`route_16.txt`, `route_16_house.txt`, `route_5_south_entrance.txt` and the other three gates, `saffron_city*.txt`,
`silph_co_*.txt`), under FireRed's flags and variables. What differs from FireRed, and why:

- **Saffron City is laid into the overworld.** FireRed's routes join a 48 by 40 copy of the city's middle
  (`SaffronCity_Connection`), and the real city (66 by 55, its houses, people and the far doors of its four gates) is a
  map no connection reaches. The importer lays the real city in where the copy would be, ten tiles west and seven north
  of it (its middle is the copy, metatile for metatile), and lays it last, so its frame, which repeats the ends of
  Routes 5 to 8, gives way to the routes themselves (`GbaWorld.StandIns`). The overworld stays 416 tiles square and the
  copy is a matrix of its own with nothing joined to it, so the matrices keep their numbers; the chunks after it were
  renumbered once.
- **The Rocket Hideout's arrows spin** as FireRed's do: an arrow is a slide of Platinum's, and on a map that has
  FireRed's stop tiles a spin goes on over plain floor in the last arrow's way until a stop tile or a wall
  (`FieldMovement.Spins`; Platinum's own slides, on maps with no stop tiles, end on plain floor as before).
- **The lifts** (the department store's, the hideout's, Silph Co.'s): FireRed's car leads back to the floor it last
  stopped at (`MAP_DYNAMIC`). Here the panel rides the car (`anim elevator`) and puts the player out before the lift's
  door of the floor chosen, and the car's own doorway leads to the ground floor (the first basement in the hideout),
  so nobody is shut in a car. The hideout's panel does nothing without the Lift Key.
- **Barriers and Silph Co.'s locked doors** are things of the map on their tiles (FireRed changes metatiles): the
  hideout's barrier (`ElectricGate`) goes once both its grunts are beaten (FireRed counts them as the map loads; here
  each one's battle asks), Silph Co.'s twenty doors (`GalacticDoor`, four to a door) open to the Card Key for good under
  FireRed's flags (`FLAG_SILPH_5F_DOOR_1`).
- **The ghost of the sixth floor is Marowak** once the Silph Scope shows it: a wild battle at level 30, after which
  the spirit rests and the way up opens; run from, it pushes the player back as before. FireRed's ghost can't be
  caught; a wild battle here has no such rule yet, so a Marowak caught is taken for a spirit at rest too.
- **The Snorlax of Routes 12 and 16** wake to the Poké Flute (its tune, `kanto/poke_flute`, played over the field) and
  battle at level 30; once the battle is over, however it ended, the Snorlax is gone.
- **The Tea** is taken by the first thirsty guard given it, who shares it with the other three: one variable for the
  four gates (`VAR_MAP_SCENE_ROUTE5_ROUTE6_ROUTE7_ROUTE8_GATES`), as FireRed's.
- **Saffron's grunts on doormats.** FireRed stands grunts on the mats of the Gym, a house and Copycat's house until
  Team Rocket leaves; a way out of a room must land on open ground here, so those three are left away and the Gym's door
  is shut until Giovanni is beaten at Silph Co. (`FLAG_HIDE_SAFFRON_ROCKETS`), which keeps FireRed's order of Silph Co.
  before Sabrina. The grunt at Silph Co.'s door keeps it until Mr. Fuji is rescued, as FireRed's.
- **Prizes and slots.** The Game Corner's machines and the prize exchange are plan 06 · R16b's (the same machines and
  list as Veilstone's); FireRed's Pokémon prizes, its Soft-Boiled and Thunder Wave tutors, Copycat's trade and the
  other gifts of the chapter not written here are left for a later session.
- **Fly over Kanto** is the Town Map's Fly mode on Kanto's own `townmap.json`: a cell for each chunk of the overworld,
  the area whose chunk it is, a town on the cell of its Pokémon Center's spawn; Fly lands at the region's spawns,
  each once come to (FireRed's `FLAG_WORLD_MAP_*` are its first arrivals). HM02 is given in Route 16's house.

## Kanto's fourth chapter, first part (2026-10-18, plan 19 · K6)

From Route 12's foot and the Cycling Road to the Soul Badge, scripts of ours on FireRed's beats
(`route_16_north_entrance_1f.txt`, `route_18_east_entrance_1f.txt`, `route_16.txt`, `route_17.txt`, `route_18.txt`,
`route_15_west_entrance_2f.txt`, `route_12_fishing_house.txt`, `fuchsia_city*.txt`, `safari_zone_secret_house.txt`,
and `common.SafariTimeUp` and `SafariOutOfBalls`), under FireRed's flags and variables. What differs from FireRed, and
why:

- **The Cycling Road's gates ask the bag, not the saddle**, as FireRed's do: the guard calls back whoever crosses the
  lower lane of Route 16's gate, or Route 18's gate westward, without a Bicycle (the trigger's script asks
  `if item "Bicycle"`, where FireRed sets `VAR_TEMP_1` as the gate loads to end the triggers). The steps by the doors note going onto the
  road and coming off it in FireRed's `VAR_MAP_SCENE_ROUTE16`, and while it is 1 Routes 16, 17 and 18 put the player on
  the Bicycle as they are come to (`bicycle on`, a new command: FireRed's `ForcePlayerOntoBike`) and keep them on it
  until the next door (`FLAG_ON_CYCLING_ROAD`, which every warp clears). Like FireRed, the variable is only put back by
  the gates: a player who flies off the road rides again the next time they come to Route 16.
- **The slope pulls a rider south** (`TileBehavior.CyclingRoadPull`, FireRed's `MB_CYCLING_ROAD_PULL_DOWN`): a rider
  who steers nowhere is taken a step south at the Bicycle's pace, again and again; one who steers goes where they steer,
  uphill too. Someone on foot stands (`FieldMovement.Pulls`).
- **The Safari Zone is the Great Marsh's Safari Game with FireRed's numbers**: $500, thirty Safari Balls and six hundred
  steps (`safari start 600`), every wild Pokémon met in a Safari battle of Platinum's (bait and mud, not FireRed's bait
  and rocks). The counter asks from FireRed's three steps before the zone's door; coming back through the door with a
  game on asks whether to leave early; a game that runs out sends a player of Kanto back inside the gate
  (`if region "Kanto"`, a new condition, in the two common scripts), whose arrival sees them off.
- **The Warden** mumbles until the Gold Teeth (an item of ours, id 9025, through the data importer's overrides, found
  in the zone's west part where FireRed's item ball is) are in the bag: he takes them and gives HM04 (FireRed asks
  whether the teeth's item ball has been taken; here the bag is asked, which is the same unless the teeth could leave
  it, which they can't). The secret house gives HM03.
- **Koga** battles for the Soul Badge and TM06 and counts his six trainers beaten (FireRed's `set_gym_trainers 5`);
  his Gym's walls nobody can see are FireRed's blocked tiles, which a room of this game stands up as low partitions:
  here they can be seen (a wall drawn as floor is a look still to make).
- **FireRed's temporary variables** (`VAR_TEMP_0` to `F`) are forgotten as a place is left, as FireRed clears them as
  every map loads; a trigger of FireRed's that waits on one being 0 fires again in the next place.
- **The pens of Fuchsia's zoo** hold FireRed's Pokémon, the western one Kabuto, or Omanyte for whoever took the Dome
  Fossil (FireRed's `VAR_OBJ_GFX_ID_0`; here two of the map, each behind `FLAG_GOT_DOME_FOSSIL` one way). Their signs
  tell the Pokédex of the Pokémon as seen.
- **Gifts**: the Good Rod in Fuchsia and the Super Rod on Route 12 for a yes, Route 15's aide's Exp. Share for fifty
  kinds caught, Substitute taught once by the boy by Fuchsia's Mart (`MoveTutors.Kanto`). The Move Deleter is
  Canalave's. Route 18's trade, FireRed's Magikarp measure on Route 12 and its Fame Checker are left away.

## Kanto's fourth chapter, second part (2026-10-19, plan 19 · K6, part b)

From Fuchsia City's shore over the sea to the Seafoam Islands and Cinnabar Island, with the Power Plant on Route 10's
river and, from plan 19 · K7's list, Cinnabar's Gym, Blaine and the Volcano Badge and the Pokémon Mansion, scripts of ours
on FireRed's beats (`route_20.txt`, `seafoam_islands_*.txt`, `power_plant.txt`, `cinnabar_island*.txt`,
`common.PokemonMansionSwitch`), under FireRed's flags and variables. What differs from FireRed, and why:

- **Holes drop whoever steps on them to the floor below**, as warps (`TileBehavior.Hole`, FireRed's `MB_FALL_WARP`).
  Each floor keeps a warp under each hole above, on a tile that is no way anywhere, as the place its faller comes
  down: the GBA reader writes such a warp as a landing (`AreaWarp.Landing`), which leads nowhere and shuts nothing, so
  stepping back onto the landing takes nobody up again (FireRed takes a warp only on a tile whose behaviour is a way).
  Emerald's Granite Cave holes will take the same rule at Hoenn's next `--game emerald --data`.
- **A boulder pushed onto a hole falls to the floor below**: each hole is a pit (`Map.BoulderPits`, the Distortion
  World's rule), whose script hides the boulder for good by its own flag and shows its counterpart on the floor below.
  FireRed keeps the counterpart's flag in the boulder object's trainer field; here the hole's script asks which boulder
  fell (`if subject "pushable_boulder_2"`, a new condition) and clears that one's flag. Route 20 puts every boulder
  back where it starts until both of a floor's lie across its current, as FireRed's map does on every arrival.
- **Currents carry a surfer** (`TileBehavior.CurrentNorth` and its kin, FireRed's `MB_*_CURRENT`; `FieldMovement.Carries`)
  step after step until calm water or something in the way; whoever comes down a hole onto water rides out on it there
  (`Player.ComeDownOn`, FireRed's `ForcePlayerToStartSurfing`). FireRed's scripts that walk a faller down B3F's current
  and up B4F's are the currents themselves here, with two ways out at the foot of B3F's (the overlay's `exits`) where
  FireRed's script warps the player to B4F. Once two boulders lie across a floor's current FireRed lays the floor out
  again in calm water (`setmaplayoutindex`): the GBA reader writes that layout's chunks after every matrix's
  (`GbaWorld.Alternates`), and `world.json` swaps them in by the floor's flag (`FLAG_STOPPED_SEAFOAM_B3F_CURRENT`,
  `_B4F_`; a swap may now go by a flag as well as a variable). B4F's three steps that turn a surfer back north are the
  current's own pull, and have no script.
- **Water is one level to a surfer on a GBA game's map** (`Map.WaterIsOneLevel`, set for worlds found at their doors):
  the GBA's water is "any level", and drawn at its banks' levels the Seafoam Islands' rapids came out as steps no
  surfer could cross. Waterfalls and decks keep their rule.
- **Articuno and Zapdos** are FireRed's static encounters at level 50: run from, each flies off and is back on the next
  visit; beaten or caught, gone for good (`FLAG_FOUGHT_ARTICUNO`, `FLAG_FOUGHT_ZAPDOS`, as FireRed's arrival scripts
  show them while unfought). The Power Plant's two Electrode lie as Poké Balls and are gone however their battle ends.
  The Power Plant's doors on Route 10's river are open.
- **Cinnabar's Gym** is locked until the Secret Key's ball in the Mansion's basement has been picked up (FireRed asks its
  flag, `FLAG_HIDE_POKEMON_MANSION_B1F_SECRET_KEY`, and so does the island's arrival). Its six doors, which FireRed
  draws shut and opens with `setmetatile`, are shutters of ours on the doorways' tiles (opened under them), each gone
  once its quiz machine is answered right or its trainer, called out by a wrong answer, is beaten
  (`FLAG_CINNABAR_GYM_QUIZ_1` to `_6`). The six questions are our own, true or false in FireRed's order of answers.
  Blaine gives the Volcano Badge and TM38, counts his seven trainers beaten and opens every door.
- **The Pokémon Mansion's switch**, behind a statue's eyes on every floor, turns every barrier of the house over at once
  (`FLAG_POKEMON_MANSION_SWITCH_STATE`). The barriers FireRed changes with `setmetatile` are electric gates of ours on
  their tiles (the lab's security beams), one set shown until the switch is pressed and the other from then on; the
  vertical barriers' top tiles, which are walls either way, have none. The diaries are pages of our own.
- **The lab** brings a Fossil (the Helix or Dome Fossil, the Old Amber) back to life at FireRed's level 5 (`fossil give
  5`) once the player has gone out and come back, on the game's own Fossils; its researcher teaches Metronome once
  (Platinum has no machine of it: `MoveTutors.Kanto` counts Psych Up as its kin). Clifton's, Norma's and Garett's trades
  are plan 07's.
- **Left for plan 19 · K7**: Bill waiting on Cinnabar once Blaine is beaten and the ferry to One Island (FireRed sets
  `VAR_MAP_SCENE_CINNABAR_ISLAND` 1, as here; Bill, the ferry and the Center's Bill stay hidden, `common.KantoNewGame`
  and the places' arrivals), the Fame Checker, the quiz trainers' walk up to the machine.
- **Koga's walls** (K6a's left-over) are a ruling now: they stay the low partitions a room of this game stands blocked
  tiles up as. A wall nobody sees would be a look of its own and leave a 3D field with no hint of where the maze runs;
  the puzzle of finding the way stays, by sight rather than by bumping.

## Kanto's fifth chapter (2026-10-20, plan 19 · K7)

From Cinnabar Island over the sea to One Island, and from Viridian City's Gym up Route 23 and Victory Road to the
Pokémon League and Kanto's Hall of Fame, scripts of ours on FireRed's beats (`cinnabar_island*.txt`, `one_island*.txt`,
`viridian_city_gym.txt`, `route_22.txt`, `route_23.txt`, `kanto_victory_road_*.txt`, `indigo_plateau_*.txt`,
`pokemon_league_*.txt`, `common.SailWithBill`, `common.SeagallopTo*`, `common.VictoryRoadSwitch`), under FireRed's flags
and variables. What differs from FireRed, and why:

- **FireRed's Victory Road is `kanto_victory_road_*`**: Platinum's keys are `victory_road_1f` and the rest, so a GBA
  game's Victory Road takes its region's prefix as the importer reads it (`GbaMap.KeyPrefix`, the plan's rule for keys
  that would meet Sinnoh's; Emerald's will be `hoenn_victory_road_*`). Its maps keep FireRed's names (`VictoryRoad_1F`).
- **Bill and the Seagallop**: once Blaine is beaten (`VAR_MAP_SCENE_CINNABAR_ISLAND` 1) Bill waits by the dock and asks
  the player to sail with him; refused, he waits in the Pokémon Center and asks there. The Seagallop comes and goes
  while the screen is dark and the ship is never drawn (`FLAG_HIDE_CINNABAR_SEAGALLOP`, and a flag of ours for One
  Island's harbour): the trip is a line over the dark and the two come ashore outside One Island's harbour, where Bill
  takes the player straight into the Pokémon Center (FireRed walks them up the road). Celio gives the **Tri-Pass**
  (an item of ours, id 9026) at the first meeting, and Bill stays to help him. The harbour's sailor sails a holder of
  the pass to Vermilion City or Cinnabar Island, and Vermilion's ferry sailor to One Island. Not here: the Meteorite,
  Two and Three Island, Lostelle and the Berry Forest, Bill's return to Cinnabar (`FLAG_SEVII_DETOUR_FINISHED`), the
  Ruby and the Sapphire: plan 19 · K9.
- **One Island's town owns no chunk of its own**: it is a corner of the chunks Treasure Beach owns. The importer now
  writes the chunks such an open area lies in, and the area is open by its tiles (`GbaImport.WriteWorld`'s lodged
  areas). Its houses build in a way of their own (`Architecture.Isle`).
- **Giovanni** battles for the Earth Badge and gives TM26; FireRed's set_gym_trainers 8 counts his eight trainers beaten,
  `FLAG_HIDE_MISC_KANTO_ROCKETS` is set and the rival goes to wait on Route 22 (`VAR_MAP_SCENE_ROUTE22` 3). Spoken to
  again, he gives up Team Rocket and is gone (`FLAG_HIDE_VIRIDIAN_GIOVANNI`, which FireRed's `removeobject` sets). The
  Gym's spin arrows are FireRed's (plan 19 · K5, part b), now painted on the floor (style guide, "Spin arrows").
- **The rival before the League's gate** is FireRed's late battle, by his starter, and can't be lost and the game go on.
- **Route 23's guards** each check the next Badge on the tiles of their line, as FireRed's coordinate events do, counting
  `VAR_MAP_SCENE_ROUTE23` up from Route 22's gate's 1 to 8. FireRed's lines run on over rock and the forest at the road's
  west edge, which nobody can stand on here, and those tiles are left out; a trainer without the Badge is turned back a
  step south (beside the last guard, where rock lies south, a step west and south).
- **Victory Road's floor switches** are FireRed's triggers only a boulder sets off (their value, 99, is one the
  variable never holds): here each is a pit (`Map.BoulderPits`), whose script lifts its rock barrier. The barriers,
  which FireRed draws with `setmetatile`, are heaps of rock of ours (`Rubble`) on their tiles, hidden by flags of ours
  (`FLAG_VICTORY_ROAD_1F_SWITCH_PRESSED` and the rest, for FireRed's variables at 100); Route 23 puts them back, and the
  two boulders of the hole on 3F (`FLAG_HIDE_VICTORY_ROAD_3F_BOULDER`, `_2F_`), as FireRed's Route23_OnTransition does.
  A boulder left on a switch goes back where it started when the floor is left, as every pushed boulder does; the
  barrier stays open until Route 23. The Cooltrainer on 2F teaches Double-Edge once (`MoveTutors.Kanto`, Return its kin
  among Platinum's machines).
- **The League's rooms** are FireRed's, each leading on only: the way back to the hall is shut behind the player (FireRed
  closes it with `setmetatile`), and the door ahead, drawn shut in FireRed's layout, is a door of ours opened by its
  master's flag (`FLAG_DEFEATED_LORELEI` to `FLAG_DEFEATED_CHAMP`; an overlay's `doors` with `openedBy`). The arrivals
  are on the last row of each room's floor, since FireRed's thresholds lie past it. Coming into the Indigo Plateau's
  Pokémon Center puts all four back (FireRed clears the same as its whiteout and its Hall of Fame leave the League).
  The four are spoken to for their battles (FireRed walks the player in first; here nobody walks). The rematch teams,
  Lorelei's absence and the door guard's word about it are plan 19 · K9's.
- **The Champion** is the rival, battled with FireRed's first teams; his records are of the Champion's class (its band
  and song) under the rival's own name (`TrainerDatabase.IsRivalAsChampion`). Oak comes in after the battle
  (`FLAG_HIDE_OAK_IN_CHAMP_ROOM`) and takes the player to the Hall of Fame.
- **Kanto's Hall of Fame** is Sinnoh's scene on FireRed's beats: Oak's words, the team recorded (`halloffame`, which
  still gives Sinnoh's Champ Ribbon), `KantoHallOfFame` (the region's own flag, `Region.StoryCompleteFlag`) and FireRed's
  `FLAG_SYS_GAME_CLEAR`, the team's entry, the team healed, the game saved in the player's room in Pallet Town, and
  the credits (this game's roll). The room's floor is FireRed's `MB_ICE`, which slides a walker, so nobody walks in it.
  `FLAG_GAME_COMPLETED`, Sinnoh's, is not set. What a Champion of Kanto finds afterwards (the sailor moved to
  Vermilion's harbour and the way on to Johto, the National Pokédex) is plan 19 · K8's and K9's.

## Kanto's sixth chapter (2026-10-20, plan 19 · K8)

What K7 left of the League's end: the way on to Johto, the songs and the bands. Nothing of Sinnoh's moved.

- **The way on to Johto is by boat from Vermilion City.** FireRed has no such crossing, so the sailor is ours: a
  captain on the quay east of the pier (301, 230), an overlay person who is `isTransportAttendant`, so
  `common.Attendant` and `RegionDatabase.AttendantLines` speak for him as they do for Sinnoh's. He says only who may
  come aboard until `KantoHallOfFame` is set (the region's `StoryCompleteFlag`, which the Hall of Fame's script
  sets), and then takes the player to Johto's arrival before the house in New Bark Town. Pallet Town's shore has no
  sailor now. The ruling of plan 18 · W10 (a region's attendant at its departure map; the next region's story is not
  started until the previous one's Hall of Fame) is unchanged.
- **Kanto's own songs.** The Champion's battle (`kanto/battle_champion`), the quiet room the Hall of Fame is entered
  by (`kanto/slow_pallet`, the room's map theme: FireRed's room plays its Pallet Town theme slowed) and the credits
  (`kanto/credits`) are compositions of ours; the ceremony itself plays `kanto/hall_of_fame` as before. A scene's song
  that only some regions have is asked of the region's folder first (`MusicDirector.SceneSong`,
  `AudioManager.PlaySceneMusic`) and falls back to the common role's (Title for the credits, Victory Champion for the
  ceremony), so a new region's songs are a file and no new `MusicRole`.
- **Kanto's bands.** The eight Leaders, the Elite Four and the Champion wear bands after their Gym's type or their
  room (the style guide's second table), none the colour of a Sinnoh band of the same type. The Champion is the rival,
  so his band is found by the class and the rival's name (the data's `{rival}` or whatever the player called him),
  and the plate fills the placeholder when it is drawn. A trainer no table names still wears the plain band.

## The first Sevii Islands (2026-10-20, plan 19 · K9, part a)

Kindle Road, Mt. Ember, Two Island and Three Island, and the errand that runs through them, as scripts of ours on
FireRed's beats and flags (`OneIsland_PokemonCenter_1F`, `TwoIsland_JoyfulGameCorner`, `ThreeIsland`,
`ThreeIsland_BerryForest`, `CinnabarIsland`, `MtEmber_Exterior`, `MtEmber_RubyPath_B5F`, `MtEmber_Summit`,
`PalletTown_ProfessorOaksLab`), in our own words. No story version was raised: each place's own arrival hides whoever
its part of the errand hasn't reached, so a save from before K9 finds the islands as a new game does.

- **The Meteorite is Bill's.** FireRed's Celio gives it in the meeting; here Bill hands it over as the two set to
  work (`one_island_pokemon_center_1f.GiveMeteorite`, `FLAG_GOT_METEORITE_FROM_BILL`, a flag of ours), and a game
  that met Celio under K7, before there was a Meteorite, is given it the next time Bill is spoken to.
- **The detour ends as FireRed's does.** Lostelle's father takes the Meteorite for a Moon Stone once she is home
  (`VAR_MAP_SCENE_ONE_ISLAND_POKEMON_CENTER_1F` 2), the first step towards Bill sends the two of them to Cinnabar by
  the Seagallop (3), and Bill says goodbye on the dock (`VAR_MAP_SCENE_CINNABAR_ISLAND` 4), after which Lostelle is
  in her own house on Three Island. The game corner's link games are not played: its machines say that nobody is
  linking.
- **The bikers.** Three of the gang and then Paxton are battled one after another, as FireRed's
  `ThreeIsland_EventScript_BattleBikers`, as teams of FireRed's data (`kanto_biker_goon` to `_3`,
  `kanto_cue_ball_paxton`); refusing their question sends the player back a step.
- **The National Pokédex is Oak's, at his desk.** FireRed's Oak stops a Champion outside his house; here he gives it
  when spoken to in the lab, the rival coming in for his. The rule is FireRed's (`Pokedex.CanUnlockNational` with
  Kanto's `Caught60`): the Hall of Fame (`FLAG_SYS_GAME_CLEAR`), One Island reached (Celio met,
  `VAR_MAP_SCENE_ONE_ISLAND_POKEMON_CENTER_1F` 1 or more) and sixty species caught.
- **Celio's Ruby waits for the detour and the National Pokédex**, as FireRed's `OneIsland_PokemonCenter_1F` asks: his
  request (4) opens the Ruby Path's mouth (`FLAG_MT_EMBER_RUBY_PATH_OPEN`, a flag of ours on the warp's `openedBy`,
  where FireRed has the mouth walkable and empty) and brings two Team Rocket grunts to it; their passwords are ours,
  and the warehouse they open is part b's. Given the Ruby he asks a second favour; agreed to, he takes the Ruby and
  the Tri-Pass for the Rainbow Pass (an item of ours, id 9036, as the Ruby, 9035). Refused, he waits.
- **The Seagallop's sailors** at One, Two and Three Island and at Vermilion's pier take either pass, and sail to the
  first three islands; Four to Seven are part b's. Two Island's Poké Maniac, FireRed's link trader, is not there.
- **Moltres** waits on Mt. Ember's summit at level 50 until it has been fought, as Zapdos and Articuno.

## The last Sevii Islands (2026-10-20, plan 19 · K9, part b)

Four to Seven Island, their caves and rooms, Cerulean Cave and the end of Celio's errand, as scripts of ours on
FireRed's beats and flags (`FourIsland`, `FourIsland_IcefallCave_Back`, `FourIsland_LoreleisHouse`,
`SixIsland_PokemonCenter_1F`, `SixIsland_RuinValley`, `SixIsland_DottedHole_SapphireRoom`, `FiveIsland_Meadow`,
`FiveIsland_RocketWarehouse`, `OneIsland_PokemonCenter_1F`, `SevenIsland_SevaultCanyon_TanobyKey`,
`FiveIsland_LostCave_Room10`, `FiveIsland_ResortGorgeous`, `CeruleanCave_B1F`), in our own words. No story version
was raised: each place's arrival hides whoever its part of the errand hasn't reached.

- **The Rainbow Pass sails anywhere.** A holder of it is asked first which waters (Kanto, One to Three Island, Four to
  Seven Island) and then which port (`common.SeagallopRainbow`, every harbour's `VAR_SEAGALLOP_HERE` 0 to 8, FireRed's
  `SEAGALLOP_*` numbers); the Tri-Pass alone still sails as in part a. The ships at the four new harbours stay out of
  sight, as One Island's does.
- **The rival meets the player once**, on Four Island or in Six Island's Pokémon Center, whichever comes first
  (FireRed's two scenes and their shared `VAR_MAP_SCENE_FOUR_ISLAND`); he only talks, as FireRed's. Both play on
  arrival, with a fade where FireRed walks him off.
- **Icefall Cave's poachers.** The scene on the back room's three triggers is FireRed's: Lorelei, the grunts, one
  battle (`kanto_team_rocket_grunt_45`), her Lapras, the warehouse named. It also takes the scientist from before the
  Dotted Hole's door in Ruin Valley (`FLAG_HIDE_RUIN_VALLEY_SCIENTIST`), so the door is come to only after the cave,
  and puts Lorelei at home among her dolls (all of which are shown, where FireRed shows each by a draw of its own).
  HM07 is the cave's own item ball, as FireRed's.
- **The Dotted Hole's door is Cut on its dots** (FireRed's braille, which spells CUT): read, it asks, and a team that
  knows Cut with the Cascade Badge opens it for good (`FLAG_USED_CUT_ON_RUIN_VALLEY_BRAILLE` on the door's
  `openedBy`). The Sapphire is snatched in its room by the scientist who followed the player, who gives away the
  warehouse's second password as he goes; the room's tremor is a `camera shake`.
- **The passwords are ours.** Mt. Ember's grunts let slip the first ('Seel the deal') and the Dotted Hole's thief the
  second ('Ekans ate the cake'); FireRed's two are its own words and are not used. The keypad by the warehouse's door
  is read, and with both (`FLAG_LEARNED_YES_NAH_CHANSEY`) opens the door (`FLAG_UNLOCKED_ROCKET_WAREHOUSE`).
- **The warehouse.** The admin on the trigger row calls the player over; beaten, the whole of Team Rocket leaves with
  a fade (`FLAG_HIDE_FIVE_ISLAND_ROCKETS`, `FLAG_DEFEATED_ROCKETS_IN_WAREHOUSE`). FireRed's spin arrows are not laid
  on the floor. Gideon battles for the Sapphire and gives it up (`FLAG_RECOVERED_SAPPHIRE`); the Sapphire is an item
  of ours, id 9037, as the Ruby.
- **Celio's last scene.** Given the Sapphire he finishes the machine and it links the islands with Hoenn (6,
  `FLAG_SYS_CAN_LINK_WITH_RS`): Cerulean Cave's guard goes (`FLAG_HIDE_CERULEAN_CAVE_GUARD`), Lorelei goes back to
  the League (`FLAG_HIDE_LORELEI_IN_HER_HOUSE`), and the Elite Four and the Champion battle with FireRed's second
  teams (`kanto_elite_four_<name>_2`, `kanto_champion_rematch_<starter>`). FireRed's roaming beast is not let loose.
- **The Tanoby Key.** Its seven floor switches are pits (`pits` in the overlay): a boulder come to rest on one sets a
  local flag of ours (`FLAG_MAP_LOCAL_TANOBY_KEY_SWITCH_1` to `_7`), and the seventh opens the seven chambers for good
  (`FLAG_SYS_UNLOCKED_TANOBY_RUINS` on their doors' `openedBy`). Leaving the room forgets a half-done puzzle, as the
  boulders are put back.
- **Selphy** battles the player as they come into the Lost Cave's tenth room and is led home at once (FireRed has her
  walk behind the player); she thanks them before her door and is at home from then on. Her wishes are not written.
- **The Memorial Pillar** takes one Lemonade for TM42; FireRed's flag for a full bag is not needed, since the TM
  always fits.
- **The Water Labyrinth's Egg** is Togepi's, for a lead at full friendship (`LEAD_FRIENDSHIP` 255), and waits for room
  on the team.
- **The tutors.** Four Island's big man teaches Body Slam and Seven Island's cooltrainer Swords Dance, once each
  (`Models/MoveTutors.cs`'s Kanto table: Body Slam's kin among Platinum's machines is Return, Swords Dance is TM75);
  Cape Brink's old woman teaches the strongest move of a starter's last form as Route 228's master does, to the twelve
  starters of Kanto to Sinnoh fully grown, where FireRed's knows only Kanto's three.
- **Mewtwo** waits at the bottom of Cerulean Cave at level 70 until fought, as the birds; a run from it hides it until
  the cave is come to again.

## The last Sevii Islands, part c (2026-10-20, plan 19 · K9, part c)

The little the islands still owed, as scripts of ours on FireRed's beats and flags
(`data/maps/ThreeIsland_DunsparceTunnel/scripts.inc`, `SixIsland_WaterPath_House1`, `Route12_FishingHouse`,
`FiveIsland_ResortGorgeous_House`, `FourIsland_LoreleisHouse`, and `src/field_specials.c`'s
`SampleResortGorgeousMonAndReward` and `UpdateLoreleiDollCollection`).

- **The Dunsparce Tunnel is dug through with the National Pokédex.** FireRed lays the tunnel out again with
  `setmaplayoutindex` once the Pokédex is the National one; here the dug-out layout is an alternate the GBA reader
  writes as a chunk of its own (`GbaWorld.Alternates`, chunk 655), swapped in by `world.json`'s `swaps` on
  `FLAG_THREE_ISLAND_TUNNEL_DUG_OUT`, which both arrivals set (`three_island_dunsparce_tunnel.OnEnter` and
  `three_island_port.OnEnter`, each `if nationaldex setflag`), so the dig is through whichever end is come to first.
  Until then the prospector is a second person of the overlay at the wall he is working (`prospector_digging`, hidden
  by the flag) and the port's east steps are shut, a way `from` the flag in the overlay's `switches`; after it he
  stands at his own end with a Nugget to give, once (`FLAG_GOT_NUGGET_FROM_DUNSPARCE_TUNNEL`).
- **The size measurers keep records of their own.** The Water Path's beauty measures Heracross and Route 12's fishing
  brother Magikarp, each against a record of their own rather than the one size record of Platinum's Route 221
  fisherman: `team size "<species>" <variable>` measures against the named variable and `team size keep <variable>`
  writes it, and `team record <species> [<variable>]` tells what is written up. So a Heracross never spoils the
  Magikarp board, and Sinnoh's `VAR_SIZE_CONTEST_RECORD` is untouched by either. The first one shown always earns its
  ball, as in FireRed, where a record of nothing is beaten by anything: a Nest Ball
  (`FLAG_GOT_NEST_BALL_FROM_WATER_PATH_HOUSE_1`) and a Net Ball (`FLAG_GOT_RECORD_SETTING_MAGIKARP`), each once, and
  the brother measures nothing until his Super Rod has been given. The sizes are Platinum's own arithmetic
  (`Models/SizeContest.cs`), not FireRed's, which has no bell curve: the same Pokémon is the same size wherever it is
  measured in this game.
- **Selphy's wishes.** Her wish is a species the Pokédex has seen, drawn on the game's own chance (`random seen`, the
  new `IScriptHost.SpeciesSeenAtRandom`, FireRed's `SampleResortGorgeousMon`) and kept as its National Pokédex number
  in `VAR_RESORT_GORGEOUS_REQUESTED_MON`, which `if has var VAR_…` asks the team about and `{species:VAR_…}` says in a
  line; the gift is drawn with it (`VAR_RESORT_GORGEOUS_REWARD`: 1 a Luxury Ball, seven draws in ten, and 2 to 7 one
  of FireRed's six deluxe rewards, in its own order), and the butler, who is off the map until she calls him, hands it
  over. She wishes again the next time the house is come into (`FLAG_MAP_LOCAL_SELPHY_THANKED`, FireRed's
  `FLAG_TEMP_2`). **FireRed forgets a wish 250 steps after it is made; here a wish stands until it is granted**, since
  nothing else in this game counts steps against a flag and a forgotten wish only annoys.
- **Lorelei's dolls** come one by one with the times the team has entered the Hall of Fame, as FireRed's
  `UpdateLoreleiDollCollection` gives them (25 entries a Meowth, then 50, 75, 100, 125, 150, 175 and 200 a Chansey, a
  female Nidoran, a Jigglypuff, a male Nidoran, a Fearow, a Pidgeot and a Lapras): her room's `OnEnter` sets all eight
  flags and clears each the count has earned, reading the count as the built-in `LEAGUE_VICTORIES`, where FireRed
  reads its own game record. Her other six dolls always stand.
- **The Rocket Warehouse's arrows need nothing of their own.** Part b's "left" list was mistaken: the warehouse is
  laid out from FireRed's own tiles, whose slides and spinner are the behaviours plan 19 · K5, part b taught the field
  for the Rocket Hideout, so its floor already runs and stops as the hideout's does
  (`KantoTests.TheRocketWarehousesArrowsSpinAsTheHideoutsDo` holds it).

What the islands still owe, and why:

- **The roaming beast.** FireRed sets Raikou, Entei or Suicune roaming once Celio's network is finished, chosen by the
  starter. `Models/Roamers.cs` keeps a roamer by species and the three of them already have Johto's routes to roam
  (plan 20), so letting Kanto loose one would need a field of its own in the save to say which region's roam a beast
  is on. Left to the session that gives the roamers a region.
- **Icefall Cave's cracked ice** (FireRed's `MB_CRACKED_FLOOR`) needs a tile that remembers how often it has been
  stepped on and a fall to the floor below; the cave is walked without it, as plan 19 · K9, part b left it.
- **The Seagallop's Rainbow menu** stays two tiers of questions, since `choose` takes at most six answers and the
  Rainbow Pass opens seven islands.
- **The Trainer Tower and Altering Cave** want the e-Reader; **Two Island's Poké Maniac** wants a trade, which is plan
  07's.

## Johto's second chapter (2026-10-18, plan 20 · J7)

From the Slowpoke Well to the Fog Badge, as scripts of ours on HeartGold's beats and flags (`scr_seq_T23`, `D26R0102`,
`T23GYM0102`, `D36R0101`, `R36`, `T25R0201`, `T25R0401`, `T25R0601`, `D23R0101`, `T27PC0101`, `D18R0101`, `D18R0102`,
`T27R0501`, `T27GYM0101`, `T27`), in our own words. Flags keep HeartGold's names, `FLAG_UNK_*` included; where
HeartGold's object has no flag, or moves its own object instead of hiding one, the flag is ours
(`FLAG_HIDE_AZALEA_RIVAL`, `FLAG_HIDE_AZALEA_GYM_BUGSY`, `FLAG_HIDE_SLOWPOKE_WELL_B1F_KURT`). `common.JohtoChapterTwo`
hides the chapter's people for a new game and for a save from before it (story version 25).

- **The strange tree on Route 36 stands where HeartGold stands it** (415, 246), in the one-tile way between Violet
  City's side of the route and the turning north to Route 37; the ledges of the way from the National Park drop a
  walker east of it. So Ecruteak City is come to only once the tree has been watered with the SquirtBottle and battled
  (a wild Sudowoodo at level 20, won or caught; run from, it stands again), as in HeartGold, and a walk of the whole
  game in which the tree stays reaches neither Route 37 nor Ecruteak City (`JohtoTests`). A save made west of the tree
  before it stood is never shut in: Goldenrod City, Ilex Forest and Union Cave lead round to Violet City. The florist
  lends the SquirtBottle (a key item of ours, `Overrides/items.json`) to a trainer with the Plain Badge; her sister,
  who waits by the tree, gives three Oran and three Pecha Berries and goes home, where HeartGold's Berry Pots, a key
  item with no use in this game yet, are left out. The florist sells the four mulches afterwards.
- **Bugsy is away until the Slowpoke Well is cleared** (a flag of ours): HeartGold's Gym is open all along, but the
  Hive Badge is what lets Cut be used outside of battle, and Ilex Forest's apprentice and his birds only come once the
  well is cleared, so the chapter is played in HeartGold's order either way.
- **The Azalea Gym's Spinarak carts run straight across**: each of HeartGold's twelve docks is one of its triggers, and
  stepping onto one rides the player (a sound and the dark) to the dock facing it on the next island. HeartGold's carts
  run on tracks its two levers turn; the levers are read and left as they are. The top island's north-east dock, which
  has none below it, runs to the middle dock. A room of a DS game longer than a chunk is walled where neither its ways
  in nor its triggers reach (`WorldMapBuilder`), so the islands are part of the room and the open floor round the hall
  isn't.
- **Ilex Forest's two Farfetch'd each run twice and are caught the third time**: HeartGold's birds flee from sticks the
  player steps on and are caught from behind; here each hops to a new spot (two of HeartGold's) as it is spoken to and
  gives in at the third (`VAR_FARFETCHD1_PLACE`, `VAR_FARFETCHD2_PLACE`, ours, which the forest's own script puts back
  as it is come to). With both, the charcoal maker comes for his apprentice and gives HM01; the apprentice's Charcoal
  waits at the kiln.
- **Kurt gives a Lure Ball** for the well, as HeartGold's Kurt does; making balls of Apricorns is plan 20 · J6's.
- **The Radio Card** is won at the Radio Tower's quiz: five questions of ours, yes or no in HeartGold's pattern of
  answers, all right in a row. The Pokégear's radio (plan 20 · J3) reads its flag.
- **Raikou and Entei roam Johto**, set loose by the Burned Tower at level 40 (HeartGold's `CreateRoamer 0` and `1`):
  two slots after Platinum's six in `Overworld/Roamers.cs`, each roaming HeartGold's sixteen routes of Johto by its
  own table of neighbours (`field_roamer.c`), never Sinnoh's; a save of the six slots is given the two as it is read.
  Suicune doesn't roam, as in HeartGold (the plan's "three roamers" is the Gold and Silver of 2000): it runs on ahead
  of Eusine, and where it is next seen is a later chapter's.
- **The Ecruteak Gym's floor hides its holes** as HeartGold's does: each hole off the safe path is one of its triggers,
  and a step onto one drops the player back by the door. Nothing on the floor tells the path; the Gym Guide warns of it.
  Until the Burned Tower's three have fled, an old man sees visitors out and Morty is away.
- **Surf is the Dance Theater's gift**: the gentleman of the audience gives HM03 once the grunt bothering a Kimono Girl
  is beaten, walked up to on the stage's steps or spoken to. The red-haired boy's scene at the theater's door, after
  which the five Kimono Girls take the stage, waits for both the Fog Badge and HM03 (`VAR_UNK_4079` 3, set by whichever
  comes second): HeartGold's variable is set by a script of its own this game hasn't, and the scene sets
  `VAR_UNK_410C` past the gift.
- **Officers Keith and Dirk battle after dark only**: at night and late at night, by their routes' own scripts as the
  place is come to; by day their doubles stand on their tiles and talk.
- **Bill** meets the player on the first visit to Ecruteak City's Pokémon Center and is home in Goldenrod City
  afterwards with an Eevee for a team with room for it; his number isn't given (the Pokégear's PC calls are later).
- **The Goldenrod Department Store's counters sell HeartGold's stock** (`src/scrcmd_mart.c`'s lists 3 to 7, laid over
  the imported marts by `tools/DataImporter/Overrides/marts.json`), its roof's machines sell drinks, and its lift is
  ridden as the Celadon store's is (plan 19 · K5): the car's door leads to the ground floor, and the attendant lets
  the player out on the floor chosen. Its basement stays shut.
- **Left for later**: the Bug-Catching Contest; Headbutt from the forest's man (he only talks); the Game Corner's
  prizes; the phone numbers of Kurt, Bill and the Day-Care couple; Cameron and the photo album; Suicune's sightings;
  the Kimono Girls' battles (J11).

## The west and the sea of Johto (2026-10-19, plan 20 · J8)

Routes 38 to 41, the Moomoo Farm, Olivine City with its lighthouse and port hall, Cianwood City and the Whirl Islands'
upper floors, from HeartGold's maps, events and tables, in our own words and music.

- **Whirlpool** is HeartGold's field move, added after Platinum's own (`FieldMove.Whirlpool`, `FieldMoves.Whirlpool`).
  A whirlpool is a block of tiles of HeartGold's whirlpool behaviour (three by three on Route 41), each block drawn as
  a ring turning on the water (`PropType.Whirlpool`, placed by the model `uzushio`). It is water to anyone on foot,
  nobody sets out onto it from the shore, and it stops a surfer from every side. Faced from the water it asks, as a
  waterfall does (`common.Whirlpool`): with a Pokémon that knows the move and Johto's Glacier Badge (the region's
  `gates`), a yes carries the surfer across the whole block to the open water beyond, any way it is crossed; without
  either it says the water is too strong. The party menu's Whirlpool does the same where one lies ahead. HeartGold
  pulls the player through over several frames; here the crossing is one climb, as Waterfall's is.
- **The Glitter Lighthouse's floors take the rooms' models**: their header calls them a cave and their props read from
  the field's set come out as Ecruteak's Mart and New Bark's houses, so the importer reads them from the rooms' set
  (`HgWorld.RoomsByTheirModels`); the light room, another bank of area data, reads right from the field's. **The gap in
  the fourth floor** (the original's trigger, whose script drops the player through it) is an overlay's exit down to
  the ledge round the third floor, from which the tower's west half and the way up are come to, as HeartGold's
  climb goes. The lift waits for the chapter that brings the medicine up (J9).
- **What waits for its chapter**: Jasmine (in the light room by the sick Ampharos, Amphy, and in her Gym), Chuck, the
  red-haired boy at Olivine's Gym door, Suicune and Eusine at Cianwood, the pharmacy's medicine and the Moomoo Farm's
  sick Miltank (they say what they are for), the photographer, the weekday siblings, Professor Oak at the port, the
  S.S. Aqua's quay (J15), the Battle Frontier beyond Route 40's gate house (J13; since part d its guide takes a Johto
  Champion there by a crossing: "Route 40's way to the Battle Frontier", below), the cliff path to Route 47 and the
  Safari Zone, and below the Whirl Islands' rock step on their second basement the third basement and Lugia's cave
  (J11). The original hides Route 39's herd, the lighthouse's fourth-floor trainers and the barn's Miltank behind
  flags nothing in this game sets, so they are always there.
- **The whirling water round the Whirl Islands' lowest cave** (`d_uzushio`) is no whirlpool: its tiles are plain cave
  water, so it is left to the cave's own water to draw.

## Johto's third chapter (2026-10-20, plan 20 · J9)

Olivine's lighthouse to the Mineral Badge, as scripts of ours on HeartGold's beats and flags (`scr_seq_T26`,
`T26GYM0101`, `D27R0107`, `D27R0108`, `T24`, `T24GYM0101`, `T24R0501`, `R39R0101`, `R39R0201`), in our own words.

- **Who waits is kept off the map by the place's own arrival**, not by the new game: the red-haired boy at Olivine's
  Gym door (`FLAG_HIDE_OLIVINE_RIVAL` while `VAR_UNK_4078` is 0), Jasmine and the two who watch her in the Gym (while
  `VAR_SCENE_LIGHTHOUSE_JASMINE` is below 2), Eusine and Suicune in Cianwood. HeartGold's new game sets those flags;
  setting them as each place is come to does the same for a new game and for every save made before, so no story
  version is raised.
- **The lighthouse's lift** opens once Jasmine has first spoken and stepped aside (`FLAG_UNK_1D8`, the flag HeartGold's
  own object at the lift's door is hidden by): both its doors are walls until then. The car carries whoever comes in
  to the other end at once, as the Vista Lighthouse's does, the two ends each saying which they are as they are come
  to (`VAR_OLIVINE_LIGHTHOUSE_LIFT_FLOOR`, ours); HeartGold's car remembers its last floor instead.
- **The Secret Potion** is Platinum's own key item, given by Cianwood's pharmacist once Jasmine has asked
  (`FLAG_GOT_SECRETPOTION`) and taken by Jasmine for Amphy. Every other time the pharmacist sells HeartGold's five
  medicines (`src/scrcmd_mart.c`'s list 11). A save of Sinnoh's Route 210 is not touched: the item is taken in Johto.
- **Chuck's waterfall**: HeartGold turns the Gym's waterfall off with a winch beside it (`CianwoodGymTurnWinch`, a
  temporary variable, so it runs again on every visit); here the winch sets a flag local to the room, and Chuck hears
  nothing until it is set. The waterfall itself isn't drawn. Chuck counts his four black belts beaten (HeartGold's
  `SetTrainerFlag`), and his wife meets the player as they come out of the Gym with HM02 (`VAR_UNK_4116`).
- **Suicune in Cianwood**: HeartGold's tower counts the sightings (`VAR_UNK_4076`); here Cianwood's arrival counts its
  stop once the three of the Burned Tower have fled. Suicune bounds off and Eusine battles at once; a battle lost
  ends the scene as HeartGold's whites out, and Suicune is gone either way (`FLAG_HIDE_ROUTE_42_SUICUNE` is cleared
  for its next stop, plan 20 · J10).
- **Fly over Johto** (plan 18 · W9's half within one region): `SpawnLocations.In(region)` gives a region's spawns
  (its file's, and those its world folder's `spawns.json` adds, which is how Kanto's FireRed heal locations reach
  Fly), a first arrival opens the town of the region the player is in, the region's map resolves its fly spots
  against its own spawns, and a flight lands on the town's own overworld (`SpawnLocation.Map`). Johto's eight towns
  are its spawns, each before its Pokémon Center (New Bark Town's before the player's house). Teleport, a white-out
  and the place to come back to stay Sinnoh's until W9: before this, a Kanto flight landed on Sinnoh's map.
- **The Moomoo Farm's Miltank** is fed only once the farmer has told what ails her (`FLAG_UNK_0A8`): HeartGold counts
  six berries with six flags and cures her with the seventh; here one variable of ours counts them
  (`VAR_MOOMOO_FARM_BERRIES_FED`). HeartGold takes any of its Oran Berries one at a time, as here. The girls' Seals
  are left out (Johto's game has no Seal Case of its own).
- **A smashed rock** (J7's leftover) lets out a wild Pokémon of its place's `rockSmash` table at the table's rate in a
  hundred (`rocksmash` after the common `Rock` script): HeartGold's two slots are met 90 and 10 times in a hundred, the
  GBA games' five by their water's weights, the lead's ability having its say and a Repel none, as for a rod. Sinnoh's
  rocks hide nothing, as in Platinum.
- **Left for later sessions**: Baoba on Route 40; Jasmine's and Chuck's numbers and rematches; Elm's call after the
  third Badge and `VAR_SCENE_ROCKET_TAKEOVER` (J11); the photographer and the weekday siblings of Routes 40 and 41.
  (Routes 47 and 48, the cliff gate, Cliff Cave and the Safari Zone came with J8's and J9's parts b: "The Johto Safari
  Zone", below.)

## The Johto Safari Zone (2026-10-20, plan 20 · J8, part b, and J9, part b)

Routes 47 and 48, the Cliff Edge Gate behind Cianwood City, Cliff Cave, the Safari Zone's gate with its Pokémon Center,
the gate hall and the zone itself, from HeartGold's maps, events and tables (`scr_seq_D47R0101`, `D48R0101`,
`D50R0101`, `R47`, `T26`), in our own words; Warden Baoba's call and his Owner Aptitude Test up to the Owner.

- **Twelve areas side by side, no Area Customizer.** HeartGold's zone is a ring of trees round a hole of three chunks
  by two, which the game fills as the zone loads with six of its twelve areas in the order the player has set
  (`MapMatrix_Load`, `SafariZone_InitAreaSet`); the Area Customizer swaps the others in. Here the importer widens the
  zone's matrix (212) to eight chunks and fills both sixes at once (`HgMaps.SafariCells`): the opening arrangement
  (Plains, Meadow and Savannah north; Peak, Rocky Beach and Wetland south) where HeartGold puts it, and the other six
  (Forest, Swamp, Marshland; Wasteland, Mountain, Desert) east of them in the same order. Every area is open from the
  first visit. The machine in the hall says it can change nothing yet, and its attendant says why: arranging the
  areas is a later session's, if ever.
- **Each area keeps its own wild Pokémon**, read from HeartGold's `safari_enc.json` (`FieldSystem_GenerateSafariEncounter`):
  ten slots met as often as one another, each at its level, the grass's species by the time of day as the rest of
  Johto's are. The slots HeartGold adds for the objects placed in an area (and for days spent laid out) are left out
  with the Customizer. Every area is named "Safari Zone", which is the place a Pokémon caught there was met.
- **The Safari Game is HeartGold's: thirty balls and no steps** (`safari start endless`, `SafariGame.NoStepLimit`). The
  counter takes $500 and gives thirty Safari Balls; the game ends when the last is thrown (`common.SafariOutOfBalls`,
  which brings a Johto player back before the hall's counter) or when the player walks back out through the zone's
  door and says so (the hall's arrival asks, as `D47R0101_006` does; no sends them back in). The step counter over the
  field shows the balls alone for an endless game (style guide, "Field menus and notices"). The battles are Platinum's
  Safari battles (`BattleKind.Safari`: Bait and Mud, the flight after each throw), not HeartGold's, which add a
  Pokémon's mood and leave out Mud for Rocks; the Great Marsh's daily Pokémon never reach the zone (their table is
  asked by the Great Marsh's own area keys).
- **The counter's gate** moves aside in HeartGold for whoever has paid. This hall has no moving gate: its tile is
  solid, and the player is taken through behind a fade (out: a warp into the zone; back: `place` before the counter).
- **The zone's door leads back into the hall**, behind the counter. HeartGold's leads to "the last place" the player
  came from; here both of its warps lead to the hall's back tile, whose arrival asks whether the game is over.
- **Warden Baoba's call** (`VAR_UNK_4057` 1, `FLAG_UNK_0F5`, `FLAG_UNK_249`, HeartGold's): HeartGold rings as the
  player leaves the Glitter Lighthouse with Amphy cured; here Olivine City's own arrival rings the first time it is
  come to after the cure (`VAR_SCENE_LIGHTHOUSE_JASMINE` 2) and sets the variable to 3, a value of ours, so the call
  comes once. The call opens the Safari Zone (the workmen leave the Cliff Edge Gate) and puts his number in the
  Pokégear (`addcontact baoba`; his own talk on the phone is a line of `phone.txt`).
- **The two tests are asked by where a Pokémon was met** (`if has "Geodude" met "Safari Zone"`): HeartGold asks for a
  Geodude, and later a Sandshrew, caught in the zone by checking the met location of the team's members, which is what
  the condition does. Geodude lives in the Peak Area, Sandshrew in the Desert Area.
- **The second test waits a day**: HeartGold's Baoba rings once a timer of its own has run out (`VAR_UNK_4057` 3 to 4);
  here passing the first test sets a daily flag of ours (`FLAG_DAILY_BAOBA_THINKING`), and the Safari Zone's gate on
  Route 48 plays his call as the player comes to it on a later day. The workmen on the gate's plaza come out with the
  second test (`FLAG_HIDE_SAFARI_ZONE_WORKERS`, set by the gate's own arrival until then), as HeartGold's do.
- **The Owner** (`VAR_UNK_4057` 6) is greeted as one at the counter and still pays, as in HeartGold. What HeartGold asks
  of an Owner after that (placing objects, `VAR_UNK_4057` 7 and on, `FLAG_UNK_183`) is a later session's.
- **The Safari Zone Link** (swapping zones with a friend) is closed: its counters say it isn't ready, as the wireless
  link is down elsewhere (decision 06 · 7).
- **Cliff Cave's cracked rocks** stand only for a player with the National Pokédex: the cave's arrival takes them off
  the map otherwise, as `D50R0101`'s does.
- **Music**: no song of ours has been written for these places yet, so they borrow Johto's others (Route 38's
  for the routes, Route 34's for the gate in the cliff, Union Cave's for Cliff Cave, the National Park's for the
  Safari Zone, its gate and its hall).
- **Left for later sessions**: the Embedded Tower behind Route 47's hiker (warps 4 to 6 locked), Crasher Wake by
  Route 47's waterfall and Chuck there after the Kanto Badges, Cameron on Route 48 (the Kanto Safari Zone's link),
  the Area Customizer and objects, and the Safari Zone's Pokémon called by the objects.

## The north of Johto, first part (2026-10-20, plan 20 · J10, part a)

Route 42 and Mt. Mortar, Mahogany Town, Route 43 and the Lake of Rage, Route 44, the Ice Path, Blackthorn City and the
Dragon's Den, Routes 45 and 46 and the Dark Cave, from HeartGold's maps, events and tables, in our own words and music.
Routes 26 and 27, Tohjo Falls, Victory Road and the Indigo Plateau are part b's.

- **The way east is open as the stretch is**: Ecruteak's gate to Route 42 (its warps 10 and 11), the Dark Cave behind
  Route 31 and Route 46 above Route 29's gate house. Mahogany Town's east exit, which HeartGold's Rage Candy seller
  stands in until the Team Rocket Hideout is cleared, is open: he is the fourth chapter's (J11), who puts him there
  behind the hideout's flag, so that the Ice Path and Blackthorn City can be built and walked to now.
- **What waits for the fourth chapter (J11)** is left out of the overlays, so no story version is raised: Pryce in his
  Gym, Clair in hers and in the Dragon's Den, Lance, the rival in the Den, the red Gyarados and Wesley at the Lake of
  Rage, the Team Rocket grunts on Route 43 and in Mahogany, the man by the Gym, Suicune and Eusine on Route 42, the
  hiker before one of Mt. Mortar's mouths, the Ice Path's HM07 man, the Kimono Girl and the guards before the Gym and
  the Den. The Team Rocket Hideout's floors are not opened: their way down is the souvenir shop's stairs, on which its
  salesman stands, as HeartGold has him until the hideout's scene (the stairs lead nowhere until J11 opens them). The
  Mahogany Gym's ice blocks are HeartGold's statues of the scene after Pryce, and wait with him.
- **The original hides some people behind flags nothing in this game sets**: the Lake of Rage's four fishermen and
  campers, the souvenir shop's two and the guard in Route 43's gate are always there. The gifts of the stretch (the Lake
  of Rage's TM10 and the fishing guru's record, the Mt. Mortar karate master's Tyrogue, the Ice Path's HM07) wait for
  their chapter's scripts; their givers only talk.
- **Ice and lava**: the Ice Path's and the Mahogany Gym's ice is HeartGold's behaviour 0x20, which is Platinum's own
  ice, so they slide by `IceSlide` already. A walk of the whole game steps tile by tile and is not carried by ice, so it
  comes into every floor; whether every slide comes to rest where HeartGold's does is the Mahogany Gym's puzzle, which
  comes with Pryce (J11). The Blackthorn Gym's lava (HeartGold's `Magma`, about a thousand tiles, none of them blocked
  in the area's own grid) and the Ice Path's ladders stay `Waiting` behaviours: until the Gym's boulders are pushed into
  the lava to bridge it, with Clair (J11), its lava is plain ground and can be walked over.
- **The towns' looks** (the style guide's "Buildings"): Mahogany is `Architecture.Upland` (dark logs under a mossy hip
  roof, a chimney; the Lake of Rage's two houses are Mahogany's), Blackthorn `Dragonstone` (grey cut stone under dragon
  blue gables, shutters and a chimney). Mahogany's lookout posts and Blackthorn's stone gate are columns, the boulders
  of Blackthorn's streets outcrops, the Dragon Shrine a shrine of Blackthorn's stone on its island.
- **Music**: `johto/route42` (Routes 42 to 44, the Lake of Rage and the gate houses, as HeartGold's headers share
  `SEQ_GS_R_7_42`), `mahogany`, `ice_path` (the Ice Path and the Dark Cave, which share `SEQ_GS_D_KOORINONUKE`),
  `blackthorn` and `dragons_den`, our own; Mt. Mortar plays `union_cave` and Routes 45 and 46 `route34`, as HeartGold's
  headers share theirs, and the Gyms `gym`.
- **Fly** lands before Mahogany's and Blackthorn's Pokémon Centers (spawns 9 and 10). The Indigo Plateau's is part b's.

## The way to the Indigo Plateau (2026-10-20, plan 20 · J10, part b)

Route 27 and Tohjo Falls, Route 26, the League's reception gate with Kanto's Route 22 beside it, Victory Road and the
Indigo Plateau with the League's hall and rooms, from HeartGold's maps, events and tables, in our own words and music.

- **Three keys take Johto's prefix**: HeartGold names its Victory Road's floors, Route 22 and the Hall of Fame as
  Platinum and FireRed name theirs, and a place's scripts are found by its area's key alone, whatever the region. Those
  keys are written `johto_…` by the importer (`HgWorld.Clashes`: `johto_victory_road_1f` to `3f`, `johto_route_22`,
  `johto_pokemon_league_hall_of_fame`); every other key of the stretch is HeartGold's own. The rest of HeartGold's
  Kanto meets FireRed's keys the same way, which J15 settles when it opens it.
- **The League's rooms are places only**: Will's, Koga's, Bruno's, Karen's and Lance's rooms and the Hall of Fame are
  open with nobody in them, and every door between them stands open, since the battles, the doors that shut behind a
  challenger and open once the master is beaten, the guard who counts the Badges at the hall's door, Lance's, Professor
  Oak's and Mary's scene and the Hall of Fame's are the fifth chapter's (J12). The rival who waits in Victory Road is
  J12's too.
- **The reception gate**: HeartGold's guard asks for the eight Johto Badges on the tile before Victory Road's door
  (`VAR_UNK_4110`, its own variable, set once he lets the player by); ours does, in our words, and steps whoever is
  short of them back. The door east to Kanto's Route 22 is held by the same gate's other trigger, which turns everyone
  back for now: HeartGold opens it once the player has been to the Hall of Fame, and asking `JohtoHallOfFame` before
  J12 sets it would be a flag nothing sets. The door west to Route 28 is locked until J15 opens Mt. Silver's side.
- **Left out**: Giovanni in Tohjo Falls' hidden room, who belongs to a distributed event of HeartGold's (Celebi), not to
  its story; the week siblings in Route 26's house, who come with the days (J13); the trainer on Route 26 that the
  original holds back for its story.
- **The League's look** (the style guide's "Buildings"): one building on the Plateau with its Pokémon Center and Mart
  inside, pale stone under a red hip roof, arched windows and glass doors, `LEAGUE` over its entrance; not Sinnoh's
  tiers. Its rooms keep a room's look for now. Routes 26 and 27's houses are Viridian's (`Architecture.Evergreen`).
- **Music**: `johto/route26` (Routes 26, 27 and 22, the gate and the houses; HeartGold's Route 22 plays a theme of
  Kanto's own, which comes with J15), `victory_road`, `indigo_plateau`, `battle_elite_four` and `battle_champion`, our
  own; Tohjo Falls plays `union_cave`.
- **Fly** lands before the League's door on the Plateau (spawn 11), once it has been come to.

## Johto's fourth chapter, part a (2026-10-20, plan 20 · J11, part a)

The Lake of Rage to the Glacier Badge: the toll on Route 43, the red Gyarados, Lance, the souvenir shop's stairs, the
Team Rocket Hideout's three floors with the tag battle beside Lance, and Pryce, played by scripts of ours on HeartGold's
beats (`scr_seq_R43R0201`, `T29`, `D35R0101` to `D35R0104`, `T28GYM0101`) and its flags and variables. The Radio Tower,
the Ice Path's scenes, Clair, the Dragon's Den, the Kimono Girls, Ho-Oh and Lugia are part b's.

- **The toll is asked on arrival**: HeartGold's gate house in the middle of Route 43 takes ¥1,000 (or all that is left
  when that is less) as its room script runs, until the hideout is cleared. Ours does the same in the room's `OnEnter`,
  a hundred, ten and one at a time where the player has less, and it also sets `FLAG_HIDE_ROUTE_43_GATE_GUARD`, which
  HeartGold's new game sets: the guard is back once the hideout is cleared, with the grunts gone
  (`FLAG_HIDE_ROUTE_43_GATE_ROCKETS`), and gives TM36. Setting a flag HeartGold's new game sets as the place is come
  to, rather than in `common.JohtoNewGame`, is how no story version is raised (a save begun before would never have
  run it): so do the lake (Lance hidden until the Gyarados is gone) and the hideout's floors (their scene people
  hidden until their scenes).
- **The red Gyarados is met shiny** (`wildbattle "Gyarados" 30 shiny`, a shiny personality drawn for the player's
  trainer number as HeartGold's flag on its `WildBattle` makes one). Its model and sprites are the species' own colours:
  a shiny palette is no part of this game's models yet, so it looks like any Gyarados and is told apart by its line and
  its sparkle in battle. Won or caught it leaves the Red Scale, which **is a flag only** (`FLAG_GOT_RED_SCALE`): the item
  isn't in this game's data and the importer can't be run here, so Mr. Pokémon's trade for the Exp. Share waits with
  it (part b, or the next run of the importer). Run from, it is gone all the same and nothing is left.
- **The shop's stairs**: HeartGold slides a shelf aside; this room's shelf is its wall, so the stairs' warp is a lone
  switched way (`switches`, `from` `FLAG_MAHOGANY_SHOP_STAIRS_FOUND`, a flag of ours), a wall until Lance's scene sets
  it. The big man before the Mahogany Gym's door is HeartGold's, behind its `FLAG_UNK_1F9`, which the hideout's last
  scene sets: Pryce can't be reached before the hideout is cleared, as in HeartGold.
- **The hideout** keeps HeartGold's own count (`VAR_UNK_40AC`, 0 to 10, its steps listed at the head of
  `team_rocket_headquarters_b2f.txt`) and B2F's (`VAR_UNK_40A9`). The two grunts on B3F who each tell half the password
  move the count on with their own scripts once both are beaten, as HeartGold's do. The passwords are our own words
  ("Golden" and "Persian" for B3F's shutters; the Murkrow's "The boss is back!" for B2F's), HeartGold's not being ours
  to use. The shutters are things of the map (`GalacticDoor` cards) behind our door flags
  (`FLAG_ROCKET_HIDEOUT_B3F_DOOR_OPEN`, `_B2F_DOOR_OPEN`); B1F's statues are the original's five variables
  (`VAR_UNK_40AD` to `_40B1`), its grunt battles as HeartGold's two trainers, and its floor traps throw their wild
  Pokémon once each (`VAR_ROCKET_TRAP_*`), all turned off once the hideout is cleared. Ariana's battle is a tag battle
  beside Lance (`battle ariana and grunt with "johto_pkmn_trainer_lance_lance"`), and the three Electrode at the
  generator are `wildbattle`s that may be fled and tried again.
- **The hideout's music** is `johto/union_cave`: HeartGold's own hideout theme is no song of ours yet.
- **The Mahogany Gym's ice blocks** (`npcType` `IceBlock`, `FieldMovement.IceBlockRun`): walked or slid into, a block
  slides away over the ice until a wall, a person, another block or a way out stops it, and is back where it stood when
  the room is come to again, as a boulder is. HeartGold asks for no field move to push them and neither do we.
- **The takeover's start**: HeartGold sets `VAR_SCENE_ROCKET_TAKEOVER` as its count of the Storm, Mineral and Glacier
  Badges reaches three. Here the Leader's script that gives the last of the three sets it; Pryce's does now, and part b
  gives Chuck's and Jasmine's the same lines, for a game that wins the Glacier Badge before either.
- **Left out**: Whirlpool's HM, which HeartGold's Lance gives after the hideout: this game's `HM05` is Platinum's
  Defog, and HeartGold's HM of Whirlpool is no item of its data yet (the importer must give it a name of its own), so
  the Whirl Islands' lower floors stay shut with it; the Rage Candy seller at Mahogany's east exit and the
  saleswoman (the Rage Candy Bar isn't in the data either); Wesley, the fishing guru's record and TM10 at the lake.

## Johto's fourth chapter, part b (2026-10-20, plan 20 · J11, part b)

Goldenrod's takeover, from the third of the Storm, Mineral and Glacier Badges to the Rainbow Wing: Professor Elm's call,
the city taken, the uniform under the city, the Radio Tower's five floors, its lift and its observation deck, the
basement of gates and the warehouse, played by scripts of ours on HeartGold's beats (`scr_seq_T25`, `D23R0101` to
`D23R0107`, `D37R0101` to `D37R0105`) and its one count, `VAR_SCENE_ROCKET_TAKEOVER` (0 to 5, `goldenrod.txt`'s header).
The Ice Path's scenes, Clair, the Dragon's Den, the Kimono Girls, Ho-Oh and Lugia are part c's.

- **Elm's call comes on the next arrival**: HeartGold rings the Pokégear on the first map change after the third
  Badge. Here Cianwood City's, Olivine City's and Mahogany Town's arrivals call `common.JohtoTakeoverCall` while the
  count is 1 (the Leader's script that gives the last of the three sets it, Chuck's and Jasmine's since this part). The
  call is a scene of the field's text box, as the Pokégear's scripted calls are not yet a screen of their own.
- **The uniform is told in words**: the grunt by the photo studio under the city lends the player one, and the line
  says so over a black screen; the player is drawn as before, since a Rocket uniform is J14's to dress. The count is 3
  while it is worn, and the grunt on the tower's stairs goes by the count, not by a look.
- **The rival at the tower follows the grunt's words**: HeartGold plays the red-haired boy's arrival from a trigger on
  the stairs once the grunt has stepped aside. Here it follows straight on from the grunt's own script, so the one
  scene plays however the player stands; it ends in the grunt's battle, which moves the count to 4.
- **Petrel and Archer may be lost to** (`canlose`): a battle lost with Petrel puts him back in the Director's clothes
  with no key given, as HeartGold's white-out does, and Archer stays at the rail.
- **The Card Key is asked by a flag**: the slot on the tower's third floor asks `FLAG_UNK_1C1` (the Director has handed
  his key over), not the bag, because Silph Co.'s Card Key in Kanto has the same name and a player who has been there
  carries it already. The Director gives the item only when the bag has none.
- **The basement's gates** are things of the map (`GalacticDoor` cards, three to a gate across a passage, one to a gate
  in a doorway) behind flags of ours (`FLAG_GOLDENROD_B2F_GATE_0_SHUT` to `_9_SHUT`, `FLAG_OPENED_GOLDENROD_PURPLE_GATE`).
  The three switches each turn their own set of gates over (HeartGold's table), the fourth opens the way to the
  warehouse for good, and the floor's arrival sets the gates back as HeartGold's does.
- **People of our own**: the rival under the city comes in on the stairs' own tile (HeartGold's tile beside it is a
  wall here), and the Director who comes up the lift to the deck stands on the deck's floor, where HeartGold keeps him
  beside it; each is behind a flag of ours that its place's arrival sets.
- **The store's lift goes down**: the Department Store's basement is the lift's seventh stop
  (`VAR_GOLDENROD_STORE_LIFT_FLOOR` 7); a question holds six answers, so the lower floors are a second question.
- **The Basement Key and the Rainbow Wing** are key items of ours (9201, 9202) in the importer's overrides, written into
  `items.json` by hand to match since the importer wasn't run; the Rainbow Wing is HeartGold's (SoulSilver's Silver
  Wing is another game's).
- **What the deck's scene moves on**: HeartGold's flags for Mahogany's saleswoman and `VAR_UNK_407A`, and the guard
  before the Blackthorn Gym standing aside, are set as its scene sets them, though the saleswoman and Clair wait for
  part c.
- **Left out**: the Kimono Girl the Basement Key sends under the city (`VAR_UNK_40E7`), the tunnel's shops, salons and
  photo studio, Buena and her Blue Card, Whitney and the friend at the Gym, Team Rocket's broadcast on the radio, and the
  Department Store basement's crates, which its Machoke shift.

## Johto's fourth chapter, part c (2026-10-20, plan 20 · J11, part c)

The Ice Path, Blackthorn City's two doors, Clair and her Gym, the Dragon's Den and the Dragon Shrine's questions to the
Rising Badge, played by scripts of ours on HeartGold's beats (`scr_seq_D39R0101` to `D39R0103`, `T30`, `T30GYM0101`,
`D44R0102`, `D44R0103`) and flags. Whirlpool from Lance, the Whirl Islands' lower floors and Lugia, the Kimono Girls'
battles and Ho-Oh, and the small gifts are left (plan 20's J11 record says why).

- **The Blackthorn Gym is HeartGold's, not Gold's and Silver's**: its floor is lava crossed on three slabs of stone that
  turn and slide (`BlackthornPlatforms`, from HeartGold's `src/field/gymmick/blackthorn.c`: the three slabs' places and
  sizes, the buttons on each, a slide its own width with a bump and a return where its leading edge meets anything but
  lava, a turn refused where a side touches something and stopped part way by the original's tables of how far each
  corner gets). The plan's "lava bridged by its boulders" was Gold's and Silver's puzzle and is not built. The slabs are
  laid out afresh whenever the room is come to, as HeartGold's `BlackthornGymInit` does, so nothing of them is saved.
  The three panels at the ends of the ways across carry the player back to the door by a warp into the same room, which
  lays the slabs out again. Their frames are the original's (two a tile, sixteen a quarter); their look is our own
  (style guide, "The slabs on the lava").
- **The Ice Path's boulders**: HeartGold's boulders are named `rock` in its field objects, which the import had read as
  nothing; they are Strength boulders now wherever they stand in Johto (eleven areas: the Burned Tower, the Dark Cave,
  the Ice Path, Victory Road, Mt. Mortar, the Slowpoke Well, Union Cave and the Whirl Islands). A boulder pushed onto a
  hole of B1F (a pit of the overlay's) falls through: it is gone for good by its own object's flag, and a boulder of the
  map stands on the ice below under its hole from then on (`FLAG_HIDE_ICE_PATH_BOULDER_<N>_FALLEN` cleared, and a flag
  of ours, `FLAG_ICE_PATH_HOLE_<N>_FILLED`, so B2F's arrival can hide the others: HeartGold sets the four `_FALLEN`
  flags as a new game begins). A fallen boulder is a thing of the map of its own (`npcType` `FallenBoulder`, drawn as a
  boulder), which stops a slide and can't be pushed: HeartGold's fallen boulders are scenery. Which boulder falls
  through which hole doesn't matter, as in HeartGold, whose holes each have the fallen boulder of their own number
  under them.
- **A fall through a hole** is the hole's own trigger (HeartGold's `D39R0102_000` to `_003`): the player spins, drops
  to the tile below that the hole names, and B2F's arrival gives the landing its jolt (`VAR_UNK_40AB`).
- **The Kimono Girl on the ice** is pushed only from the tile behind her, as in HeartGold. Refused twice, she gives up
  for now; HeartGold asks again at once as long as the answer is no, which here would be a question leading back to
  itself.
- **Blackthorn's guards stand by the arrivals**: HeartGold hides the man beside the Gym's door and the old man beside the
  Dragon's Den's as a new game begins; here the city's own arrival hides each until what moves them aside has happened
  (Team Rocket out of the Radio Tower; Clair beaten), so no save needs a story version.
- **Clair is always home**: HeartGold hides her in her Gym only around her rematches after the Hall of Fame, which this
  game has not yet. She battles once, sends the player to the shrine, and gives TM59 (Dragon Pulse) in the Den on the
  way out (`VAR_UNK_40C4` 1) or, if the player leaves another way, in her Gym; coming back to the city without meeting
  her sends her home (HeartGold's `T30_005`).
- **The elder's questions are taken up where they stopped**: HeartGold asks a question again at once after a wrong
  answer. Here the talk ends there ("think it over") and goes on from the same question when the elder is spoken to or
  the shrine come to again (`VAR_DRAGON_SHRINE_QUESTION`, a variable of ours), so no question leads back to itself. A
  wrong answer is remembered all the same (`FLAG_FAILED_DRAGONS_DEN_QUIZ`), and the Dratini given once TM59 has been
  had knows Extreme Speed only if no answer was ever wrong (`givepokemon … knowing`, in its first move's place, as
  HeartGold's `SetMonMove` puts it). The right answers are HeartGold's (the second of each question's three is wrong in
  the first and fourth, the third in the second, the first in the third and fifth); the words are ours.
- **What the shrine's end sets for New Bark Town is J12's**: HeartGold sets the rival's room, `VAR_SCENE_ELMS_LAB` 8 and
  the way east as the Badge is given; here only the shrine's own state (`FLAG_UNK_998`, `FLAG_UNK_0EA`, `VAR_UNK_40C3`)
  and Clair's in the Den are set, and J12 writes the scenes those start.

## Johto's fifth chapter: Victory Road, the League and the Hall of Fame (2026-10-24, plan 20 · J12, part a)

Elm's Master Ball, the red-haired boy's last challenge in Victory Road, the Elite Four, Lance and Johto's Hall of Fame,
played by scripts of ours on HeartGold's beats (`scr_seq_T20R0101`, `D43R0103`, `T10R0101` to `T10R0701`) and flags.

- **Elm's Master Ball asks the Badge, not the shrine**: HeartGold sets `VAR_SCENE_ELMS_LAB` 8 as the Rising Badge is
  given and Elm hands the Master Ball over on the next visit. Here the lab's arrival asks the Badge itself and a flag of
  ours (`FLAG_GOT_MASTER_BALL_FROM_ELM`) and sets the variable as it gives the ball, so a save past the shrine from
  before this session needs no story version. HeartGold's rival's-room scene in New Bark Town is not played.
- **The rival in Victory Road is brought in by his scene**: HeartGold's new game hides him (`FLAG_HIDE_VICTORY_ROAD_RIVAL`);
  here the third floor's arrival sets the flag, and the scene (the first trigger, on `VAR_UNK_40C5` 0) places him two
  tiles behind the player, whichever of the corridor's three tiles they stepped on. His team beats the starter, as every
  earlier battle of his (`johto_rival_silver_5`, `_9`, `_13`).
- **The Elite Four's doors are their exits' flags**: HeartGold hides each room's door ahead by `FLAG_HIDE_<WHO>S_ROOM_EXIT`
  as its master is beaten; the same flags open the overlay's doors here (`openedBy`), and the way back is locked. The
  master is spoken to, as in Kanto's League (plan 19 · K7); HeartGold walks the challenger up to them. Lance's door is a
  flag of ours (`FLAG_HIDE_LANCES_ROOM_EXIT`), and Mary and Oak stand in his room only from his defeat
  (`FLAG_LANCES_ROOM_VISITORS`, ours). The hall's arrival puts the League back whenever it is come into, as HeartGold's
  white-out and its Hall of Fame leave it. The rematch teams (`_2`) are J15's.
- **The rooms take their master's type** (`arenaType`: Psychic, Poison, Fighting, Dark and Dragon), as Kanto's League's
  do: their halls' own looks are J14's.
- **Home after the Hall of Fame**: the scene warps to the player's room in New Bark Town, saves and rolls the credits,
  as the other regions' do; Johto's own ceremony and credits songs are not written (the common ones play). Elm rings the
  first time the Champion comes downstairs (`common.JohtoChampionCall`, `FLAG_ELM_CALLED_THE_CHAMPION`, ours). HeartGold's
  call is about the S.S. Aqua to Kanto and the S.S. Ticket; here the journey goes on to Hoenn (plan 18 · W10), so Elm tells
  of the ship that sails there from Olivine's port, and no ticket is given.
- **The ship to Hoenn sails from Olivine's port hall**: the sailor before the quay is the region's attendant
  (`RegionDatabase`'s link from Johto, `Transport.Boat`, `DepartureMap` `Olivine_PortHall`), where HeartGold's S.S. Aqua
  leaves from. The quay itself and the voyage to Kanto stay J15's.
- **The gate's door to Route 22** lets a Champion of Johto (`JohtoHallOfFame`) through and puts its trigger to rest
  (`VAR_UNK_4121` 1). Kanto beyond it is only the stretch of Route 22 J10 opened.
- **Left**: the reunion with a Kanto team (plan 18 · W4 and W5's crossing is not built, so there is no `Crossing.CanTake`
  to ask), the rival's room in New Bark Town, the post-game rival on the Plateau (`FLAG_HIDE_INDIGO_PLATEAU_RIVAL`), and
  the rematches.

## Reading the player's own files (2026-10-21, plan 08 · P20)

The migration of Pal Park (P21) needs the player's Pokémon out of files the player owns. P20 is only the readers: they take bytes,
give plain data and write nothing, and nothing in the game calls them yet. No save or Pokémon file of the games is in the repository
(`MigrationTests` builds every file it reads, byte by byte), and the readers are written from the decompilation's C
(`src/main_menu/ov97_02235D18.c`, `gba_pokemon.c`, `src/pokemon.c`, `src/savedata.c`), never from a tool's source.

- **The door** is `ForeignFile.Open(path)` (and `Read(bytes)` for bytes already in hand): a file is told by its size alone (80 or 100 bytes a
  GBA Pokémon, 136 or 236 a DS Pokémon, 128 KiB and a few bytes more a GBA save, 512 KiB a Platinum save), opened read-only
  with its size asked first (nothing over 512 KiB is ever loaded), and **refused whole** with a reason (`ForeignRefusal`:
  `UnknownSize`, `DamagedSave`, `DamagedPokemon`, `Unreadable`) or read whole. A test holds the bytes, the modification time and the
  folder's contents unchanged after a read, and the architecture test holds `Foreign` to no `File`, `Directory` or writing stream and
  no raylib type.
- **A GBA save** (`GbaSave`): two slots of fourteen 4 KiB sectors, each with its place among the fourteen, a checksum (the 32-bit
  words added and the two halves of the sum added), the signature `0x08012025` and a counter in its last twelve bytes; a slot is whole
  only if all fourteen are, and the newer whole slot is read (the counters compared as `ov97_02235FFC` compares them, the one that
  wrapped from the top to nought the newer); a sector may lie in any turn of the fourteen. Of the save only the boxes (sectors 5 to 13:
  the current box, fourteen boxes of thirty 80-byte Pokémon, their names) and the trainer (name and ID, in sector 0) are read, not
  the party. **The game is told by the first sector's size** (Ruby and Sapphire 2192, which the decompilation gives; Emerald 3884 and
  FireRed and LeafGreen 3876 are the sizes those games are known to use, not in it), and FireRed cannot be told from LeafGreen by a
  save, so a Deoxys's forme goes by the Pokémon's own game of origin there. **Owed to a real file**: the decompilation has Ruby's
  and Sapphire's sector sizes alone, so Emerald's and FireRed's first-sector sizes and the fifth sector's (3136 or 3816) are
  candidates tried in order, with any size the checksum accepts as the last resort; the first real file of either decides them.
- **A Platinum save** (`PlatinumSave`): a primary copy and a backup (0 and 0x40000), each a normal block and a boxes block that end in a
  20-byte footer (two counters, the block's own size, the signature `0x20060623`, its number and a CRC-16). The blocks are found by
  their footers (their sizes are in the footer, so the thirty-odd tables of the normal block need not be added up), and the pair is
  chosen as `SaveData_LoadCheck` chooses (the newer copy of each block, the older normal block where the two disagree, a block with no
  whole copy refusing the save). Of the normal block only the trainer is read (name at 0x68, ID at 0x78), of the boxes block the
  `PCBoxes` at its start. **The CRC is the SDK's CRC-16/CCITT, whose code the decompilation leaves out**: both readings of it (the
  polynomial taken from the high bit and from the low) are accepted, since a real file is the check neither can be given offline.
- **A Pokémon** is a `ForeignPokemon` (plain data: nothing points back at the file). GBA (`GbaPokemon`): the 48 bytes of four
  substructures are xored word by word with the trainer's ID and the personality, lie in the order the personality's remainder of 24
  chooses and add up to the header's checksum; a failing checksum is a **bad Egg**, which is what the game itself shows (species 412),
  not a refusal, so a damaged slot of a save costs its Pokémon alone; a slot that holds no species is empty. DS (`DsPokemon`): four
  blocks of 32 bytes in the order bits 13 to 17 of the personality choose, encrypted by the linear congruential generator the checksum seeds;
  an **empty slot is not zeroes** (`BoxPokemon_Init` clears it and then encrypts it, which with a checksum of nought is a pattern of its
  own that decrypts to nothing). A `.pk3` or `.pk4` is the same bytes decrypted and in the fixed order, and is told from a `.ek3` or `.ek4` by
  whether its blocks add up as they lie.
- **The conversion** (`ForeignPokemon.ToSaved`, `BoxMonGBAToBoxMon`): a GBA species by the table (`Treecko` 277 is 252), friendship back to
  70, the ability by the GBA's bit unless the species gained one in Generation 4 (then the first), Unown's letter from the
  personality's bits (`(P & 0x3000000) >> 18 | (P & 0x30000) >> 12 | (P & 0x300) >> 6 | P & 3`, modulo 28), Deoxys's forme by the game,
  the nature, gender and shininess from the personality as Platinum's follow them, the item through the table of GBA numbers, and the
  texts through the language's character table (a code the table lacks makes the whole text question marks, as the original). The ball and
  the original trainer's name, ID and gender, the place's level and the day are kept; the ribbons, the contest ranks and the game of origin
  have no field here and are dropped. The checks that refuse a Pokémon (an HM move, a key item, an impossible species) are asked of the
  read Pokémon (`KnowsAnHm`, `HoldsAnInvalidItem`, `HasARealSpecies`) and applied by P21.
- **Left out**: a party Pokémon's extra bytes (HP, stats, status: nothing of them is read), the party itself, Pal Park's and a cartridge's
  other tables, Ruby against Sapphire and FireRed against LeafGreen, and every file of another kind (a `.dsv`, a Showdown text, a Pokémon
  Bank file) — a file of a size none of the four kinds has is refused at once.

## Pal Park's migration (2026-10-21, plan 08 · P21)

Platinum's own migration brings six Pokémon out of a Game Boy Advance cartridge into Pal Park, where the Catching Show lets the player
catch them again (`src/main_menu/gba_migrator.c`, `pal_park_transfers.c`). Here the cartridge is the player's own files in a `migrate`
folder beside the saves (`SavePaths.Migrate`), read by plan 08 · P20's readers; the rules are `Models/Migration.cs` and the ledger
`PalParkLedger`, and the screen is the title's MIGRATE.

- **What is Platinum's** (`Migration.WhyNotFrom`, in the original's order where it has one): only once the save has the National
  Pokédex; a source that gave in the last day waits for the rest of it (`CANNOT_MIGRATE_WAIT_FOR_FULL_DAY`: a source is told by its
  trainer's ID, twenty are remembered and the oldest forgotten, `PalParkLedger.Remember`); fewer than six Pokémon in its boxes
  (`CANNOT_MIGRATE_LESS_THAN_6_IN_GBA_BOXES`, counted as the original counts them: every Pokémon with a species, an Egg too); six
  that came before and still wait in the park (`CANNOT_MIGRATE_ALREADY_STOCKED`). Of a Pokémon: no Egg, none that knows an HM of the
  GBA's (Cut, Fly, Surf, Strength, Flash, Rock Smash, Waterfall, Dive), none that holds one of the GBA's key items or machines,
  and only a species the two games share (`IsGBASpeciesInvalid`). **Six exactly** are chosen, as the original's screen asks.
- **What is ours**: a Pokémon that has come over before doesn't come twice (its personality and its original trainer's ID, kept in the
  save as `PastimesSave.Came`), and none that **the legality check** refuses (`Migration.Impossible`: a species in the data, EXP no higher
  than a level 100's on its curve, effort of at most 255 a stat and 510 in all, IVs 0 to 31, an item this game has, and moves its data can
  teach it — the level-up learnset, Egg moves, the tutors', the machines and the starters' last move: `Legality.Learnable`). Nature,
  gender and ability follow from the personality, so they can't be wrong. **A Pokémon of the DS games** (a Platinum save, a `.pk4`) is
  refused for none of the GBA's three checks (an HM move, a key item and a species number are the GBA's concern), but for an Egg, for
  having come before and for the legality check. **A folder of loose Pokémon files counts as one source** (`ForeignKind.Files`,
  `Migration.Combine`, remembered under `Migration.LooseFilesId`).
- **What is left out**: `CANNOT_MIGRATE_DIFFERENT_CONSOLE` and `CANNOT_MIGRATE_INTERNAL_CLOCK_ALTERED` (the console's MAC address and
  its clock's offset: a computer has nothing to stand for them, and a clock a player could set back was never a guard that kept
  anything), and the cartridge read error (a file the readers refuse is told by its own notice). **The time** is an argument
  (`WallClock.UtcNow` in the game, a fixed time in a test or a tool).
- **The legality check lets through what a data list can't say**: a Pokémon with a move only an event gave it (a Celebi's, a Jirachi's) is
  refused, since the data has no list of event moves; a player with such a Pokémon can carry it over as a `.pk4` from a tool that
  makes it legal. The check is no stricter than that: it doesn't read the Pokémon's origin, ribbons or the PP it was kept with.
- **What is kept of a Pokémon** is `SavedPokemonData` (`ForeignPokemon.ToSaved`: P20's conversion), so the original trainer's name, ID
  and gender, the nickname, EXP, effort, IVs, moves with their PP Ups, the held item, the ball (until it goes to the boxes as a Park
  Ball, `PalParkState.TakeSix`), Pokérus, markings, the met level and the date (a DS Pokémon's) and the condition. What this game has no
  field for (ribbons, the game of origin, the GBA's contest ranks but beauty) is dropped, as P20's rulings say.
- **Where they wait**: the six go into `PalParkState.Waiting` of the receiving save (`PastimesSave.Waiting`), where the Catching Show,
  the lobby's worker and the daily visitors (plan 03 · D12a, who yield to them) already look. **The title doesn't load a game**: the
  save is read afresh from its slot, the six brought into it (`Migration.Bring(SaveData…)`) and the slot written (`SaveSlots.Write`),
  and the title's own copy takes the new park only once the write succeeded (`GameEngine.CarryOutMigration`); a save that can't be
  read or written leaves everything as it was and says so (`MigrationBlock.CouldNotSave`).
- **Reading is all that is done to the folder**: `MigrationFolder.Read` lists it (never makes it), `ForeignFile.Open` opens each file read-only,
  and a test and the harness (`title` mode, "the migrate folder is as it was") hold the files' bytes and times unchanged after a whole
  migration. MIGRATE is on the title's menu only while the folder holds a file the readers know and a save can be read, so a
  game without the folder has the title it has always had.

## Between this game's saves, and the way out (2026-10-21, plan 08 · P22)

The migration's screen also moves Pokémon from one of this game's saves into another's Pal Park, and writes one out of a save as a
file or as text. The rules are `Core/SlotMoves.cs` (a move and its recovery), `Models/ShowdownText.cs` and `DsPokemon.Write`; the
files written by `Core/MigrationOut.cs`.

- **Another slot is a source like a file** (`ForeignKind.OwnSave`, `Migration.FromSave`): its boxes only, not its team, each
  Pokémon held whole (`ForeignPokemon.Own`, so what is moved is the Pokémon itself and not a conversion of it). A Pokémon of the
  game's own has nothing the GBA's checks ask, so it is refused for an Egg, for having no id yet (`Pokemon.Id`, which a save is given
  as it loads: the save has to have been opened once), for being already in the receiving save (team, boxes, park, Day Care: the
  `CameBefore` of a file's Pokémon) and for the legality check, as a file's is. **The once-a-day wait is not asked of a slot**:
  the wait guards against one cartridge giving its six again and again, and these six leave the source for good.
- **Six exactly, as the park takes them** (the National Pokédex of the receiving save, an empty park, six chosen from the source's boxes),
  and they wait in the park's `Waiting` as the six of a file do, but are **not** entered in the ledger (`Came`, `Sources`): they are
  not copies, so nothing can come twice.
- **A move is four writes with a receipt** (`MoveReceipt`, `SaveData.Receipts`, the full copies of the six and the source's checksum):
  the receiving save with the six and the receipt; the source without them and with the receipt; the receiving save with the receipt
  closed; the source with it closed. `SaveSlots.Write` makes each whole or not at all (plan 12 · Q1), and `SlotMoves.Resolve` runs as the
  game starts, **before the slots are read for the title**: for a receipt open anywhere, the source lets go of the Pokémon if it still
  holds them, the receipts are closed (the receiving save's first), and the move is done, so after a crash at any of the four writes
  each Pokémon is in exactly one save. **With the receiving save gone** (its slot deleted between the writes) the six go back into the
  source's first free places, and a source with no room for one says so in the log. A receiving slot that now holds another game (it has neither the receipt nor a
  single one of the six) is treated as gone. A save that can't be read keeps the receipt open until it can. A save of this game copied into
  migrate/ is not a source: the readers know no JSON save, so it is not listed.
- **The way out** (`MigrationOut`): a Pokémon of a save (the team, then the boxes) written, as a copy, into `migrate/out/`: as
  **a `.pk4`** (`DsPokemon.Write`: Platinum's 136 bytes in a box, plain and in the order A, B, C, D, with the checksum, as the tools
  that read a `.pk4` read them; `DsPokemon.ReadPlain` reads one back) or as **Pokémon Showdown's text** (`ShowdownText.Of`: the name
  with the nickname, the gender and the item; the ability, level, shininess, happiness, effort, nature, IVs and moves; English,
  whatever language the game is in). A `.pk4` holds only the species of the fourth generation's Pokédex (to 493) and a later game's
  item, ability or move is written as none: the screen offers a later species as text only and says why. The original trainer is
  the Pokémon's own, or the save's player where it has none. A name is never written over (`FileMode.CreateNew`; the next free
  number is taken); the folder is made when the first file is written, and nothing else in `migrate/` is written, renamed or moved.
- **Offered** when the folder holds a file the readers know, or when two saves can be read (one to move from, one to move to). With
  one save and no folder there is nothing to offer, and the player makes the folder to write out.

## The level cap past 100 and a region's levels (2026-10-24, plan 18 · W4)

The user chose on 2026-10-21 that the team crosses from region to region at full strength, with the level cap raised
well above 100 ([the study](../plans/W4-level-curve-study.md), option B). What W4 built:

- **The cap is 260 by the modern rules** (`Ruleset.LevelCap`; `Ruleset.HighestLevel` is the highest any rules allow) and
  stays 100 by Platinum's, so the baseline (`battles-platinum.txt`) can't move. 260 is the study's fair climb under the
  modern EXP: Paldea's Champion at about 258 with the team a step ahead. The simulation was not run again in W4: the
  new moves (plan 06 · R31) and evolutions (plan 03 · D14) that fill the levels past 100 are not built, and the number
  is to be checked once they are.
- **What stopped at 100 stops at the cap**: a battle's EXP and effort (by the battle's own rules, `Rules.LevelCap`), the
  EXP bar and the summary's TOP LEVEL, the Rare Candy and the EXP Candies, the Day Care's growing (the game's cap,
  `Pokemon.Cap`). A Pokémon is made or loaded at any level from 1 to 260 whatever the game's cap, so **a save holding a
  level past 100 loads as it was**; the save's format didn't change.
- **The curves past 100.** Fast, Medium Fast, Medium Slow and Slow go on as their formulas do. *Our ruling*: Erratic's
  last branch, `level³ × (160 − level) / 100`, falls after 100 and turns negative at 160, and Fluctuating means nothing
  past 100, so past 100 each keeps the share of Medium Fast's cube it has at 100: Erratic 600,000 × (level / 100)³
  and Fluctuating 1,640,000 × (level / 100)³, rounded down. Both rise level by level to 260, and Platinum's values up to
  100 are untouched. At 260 the dearest curve, Fluctuating, is about 28.8 million EXP, well inside its number.
- **The damage formula at the cap**: Platinum's base damage is worked out in a long (the modern one already was), so a
  stat of a few thousand at level 260 can't overflow it; nothing under 100 moves.
- **The Day-Care Checker** shows a level to 260, still three figures. **A `.pk4` written out** (plan 08 · P22) of a
  Pokémon past 100 is written at 100's EXP, since Platinum's level is its EXP's. A save's own Pokémon moved between
  slots may be past 100. A foreign file's Pokémon are still held to 100.
- **A region's levels.** A region file's `levels` (`LevelMapping`) is pairs of its own game's level and the level met
  here: a straight line between pairs, on past either end, rounded half up and held to 1 to 260. Each region's first
  route is met at the Champion's level of the region before it and its own Champion at the study's fair climb:
  Kanto `[[2, 62], [63, 104]]`, Johto `[[2, 104], [50, 136]]`, Hoenn `[[2, 136], [58, 162]]`, Unova
  `[[2, 162], [59, 185]]`. The regions to come take theirs from the same table: Kalos's Champion at 205, Alola's at
  224, Galar's at 242 and Paldea's at 258, each region beginning at the last one's. Sinnoh has none.
- *Our ruling*: **the region files keep their own game's levels**, and the mapping is applied as a Pokémon is met
  (`GameEngine.LevelHere`): a wild Pokémon's level as drawn, every trainer's team (the battle fights copies,
  `Trainer.Relevelled`, so the trainer of the map keeps their team and a rematch is never mapped twice), a partner's, a
  script's `wildbattle` and `givepokemon` (`Pokemon.Relevel`: moves chosen for it kept, moves that are only its level's
  those of the new level, HP keeping its share). Scripts go on writing their game's levels (1 to 100). Not mapped: an
  Egg, a roamer, the Battle Frontier.
- *Our ruling*: **only in a region the game didn't begin in.** A game remembers where it began (`SaveData.BeganIn`; a save
  from before began where it stands, since no crossing was built then): every game from the title begins in Sinnoh,
  and a game begun elsewhere by `--region` or a tool plays that region at its own game's levels, which is how the
  harness's `kanto`, `johto`, `hoenn` and `unova` modes stay as they were.
- **Not yet**: the post-games of the later regions climb on past their Champions on the same line (Johto's Red, 88 in
  HeartGold, comes to 161), which a region's plan may bend with a third pair. The Battle Hall takes a Pokémon at its own
  level and draws its foes to 100 at most: a Pokémon past 100 entered there is plan 06's to rule on.

## Regions (2026-10-24, plan 18 · W4, part b)

What a crossing from one region to the next does, under the user's option B (2026-10-21). The rules are
`Models/Crossing.cs`; the journey on screen (the vessel's dark screen and its line) is W5's, the way back W8's.

- **What crosses**: everything. The whole team at full strength (`Crossing.CanTake` allows every Pokémon,
  `Crossing.Depart` leaves the party as it is), the boxes, money, the bag, every region's badges, the Pokédex, the
  Trainer Card, the Hall of Fame's records and the story's flags. *Our ruling*: under option B **nothing waits in the
  boxes**, so the greyed rows of decision 2's fresh journey and the shot `30e_pc_resting` are not built; `CanTake` stays
  the one place a later choice would ask (W8's harbours sort the party by it).
- **What changes is the region the player is in**: its Pokédex is entered (W3), `{rival}` names its rival
  (`PlayerIdentity.InRegion`, on every arrival), and its Pokémon are met at its mapped levels (`GameEngine.LevelHere`,
  part a): a crossing sets nothing of the levels itself.
- **A Pokémon belongs to a region** (`Pokemon.Region`, saved as `SavedPokemonData.Region`). *Our ruling*: it is given
  the region the player stands in by the one sweep that gives ids (`GameEngine.GiveIds`: as a game begins, loads and
  saves, as a Pokémon is received and before a crossing), so a Pokémon caught, given or hatched belongs to the region it
  came in, never the one it crosses into; a Pokémon received by a trade joins the receiver's region whatever it says.
  An older save has none, and its Pokémon read as the save's own region (the region it began in, which is where it
  stands, since no crossing existed). Nothing reads it in play yet: it is the met data plan 07 · O2 and W8 build on.
- **Each region has its own rival.** The introduction names the rival of the region the game begins in; a region
  crossed into has its own (Kanto's Blue, Johto's Silver, the hero the player isn't in Hoenn) until its opening asks
  with `namerival`, the introduction's keyboard over the field (OK with nothing typed keeps the region's own; B only
  deletes a letter, since a rival must be called something). The save keeps the introduction's name as `RivalName` and
  the others as `RivalNames` (a region to a name, written only once a second region's rival has one; no version
  raised); an older save's one name is the rival of the region it began in.
- **The links' vessels** (plan 18's table, our own words): Sinnoh to Kanto by **ship** from Canalave's pier, its captain
  at the end of the pier beside Sailor Eldritch, shown only by `SinnohHallOfFame` so Platinum's Canalave is as it was
  until the post-game (a person of our own, `overlays/canalave_city.json`'s `npcs`); Kanto to Johto by ship from
  Vermilion's quay; Johto to Hoenn by **ferry** from Olivine's port hall; Hoenn to Unova undecided until plan 21 writes
  Hoenn's Hall of Fame and its harbour; Unova on to Paldea by **plane** (`Transport.Plane`), their departures plan 23's.
  The attendant, ready to sail, says the whole team is welcome aboard before the player agrees.
- **A game in one region never meets any of it**: nobody crosses before the region's Hall of Fame, and a game begun
  elsewhere by `--region` keeps its own game's levels (part a).

## The journey on screen (2026-10-24, plan 18 · W5)

How a crossing is seen and heard. The rules of what crosses are "Regions", above.

- **A script, not a screen**: `common.Attendant` asks `travel`, which says the attendant's lines and answers 0 (no way
  on from here: the person's own lines), 1 (not yet) or 2 (ready); on 2 it calls `common.Crossing`, which asks "Shall
  we set off, then?", closes the script's black screen over the field and crosses with `travel go`. *Our ruling*: the
  question is plain Yes and No, the destination having been named by the attendant's lines; W8's harbours put their
  choice of regions in its place. A no leaves the player on the quay with a line, and the attendant asks again next time.
- **The dark** is the script's own black screen with one line over it (`crossing.voyage.<vessel>`, our own words: a
  ship sails through the night, a ferry rocks over the waves, a plane climbs above the clouds) and the vessel's sound
  (`crossing_horn` for whatever sails, `crossing_engine` for a plane; both our own, `SoundDesign`). The field's music
  stops under it; the region arrived in plays its own as the line is put away. Nothing is drawn of the vessel or the
  way: *our ruling*, so a crossing looks the same between any two regions and no region needs art for it. A region
  that wants a deck to walk (HeartGold's S.S. Aqua) builds it as a map of its own; the link's `Scene` that would point
  at it is not built, since no region asks for one yet.
- **The arrival** is `TravelTo`'s, unchanged but for the field's life cleared as on any warp: the region's arrival
  tile, its music and its name sign, under the dark that `common.Crossing` then lifts.
- **The rival of a region crossed into is named in its opening**: the built-in variable `CROSSED_IN` is 1 where the
  script plays in a region the game didn't begin in (`GameFlow.BeganIn`), and a region's opening asks it before
  `namerival`. Kanto's is Oak's lab (`pallet_town_professor_oaks_lab.NameTheRival`, as the player first walks in: Oak
  asks his grandson's name, as FireRed's introduction does); Johto's is the officer's question in Elm's lab, where the
  player gives the thief's name (HeartGold asks it there too). A game begun in the region never meets either.

## Fly between regions (2026-10-24, plan 18 · W9)

The one way back to a region left: the README's "Regions" (the user's answer of 2026-10-12) has one way on by vessel and
nothing back by harbour (W8 left out), and Fly back to any region already entered.

- **Which regions** (`Crossing.CanFly`, `FlyRegions`): every other region the player has been in, as the Trainer Card
  counts it (`Crossing.HasEntered`: its Pokédex entered, a badge of it, or its Hall of Fame), built and with a map of its
  own to choose a town on. *Our ruling*: nothing is asked of either region's Hall of Fame, though plan 18's first draft
  of W9 asked both to be finished. A region is left by its vessel only once its story is over, so whatever lies behind
  the player is finished anyway; the README's words ask only that the region was entered, so a Champion back in Sinnoh
  may fly on to a Kanto still in the middle of its story, which the vessel would take them to as well.
- **Where Fly is used** is asked as ever, in the region the player stands in: its badge for Fly (the region's `gates`),
  no partner along, outdoors. A town of another region is open to Fly by its own flag, as in its own region.
- **The choice** is the Town Map's Fly mode with a tab per region (style guide, "The Town Map", "Fly between regions");
  Q and E change the region, the arrows the cursor. The original has one region and no such row: it is ours.
- **A flight to another region is a crossing** (`GameEngine.CrossTo`, which the vessel's `TravelTo` also goes through):
  every Pokémon given the region it leaves, the party sorted by `Crossing.Depart` (whole, under option B), the region's
  Pokédex entered and its rival the one `{rival}` names, the music and name sign of the town landed in, the region's
  levels by themselves. It is seen as Fly's own flight and fade: no vessel, no dark screen, no line.
- **Each region's place to come back to** (`SpawnLocations.VariableOf`): Sinnoh's is the original's `VAR_SPAWN_LOCATION`;
  every other region keeps its own (`VAR_SPAWN_LOCATION_KANTO`), set on going into one of its Pokémon Centers, so a
  flight away and back leaves each region's as it was. Teleport goes to the region's own (its first spawn before any);
  a white-out outside Sinnoh still wakes at the region's home, as plan 19 · K1 ruled. Nothing new is saved: the
  regions entered, the towns' flags and the variables were already in the save, so no save version was raised.

## The Bug-Catching Contest (2026-10-24, plan 15 · E5)

HeartGold's own, read from pret/pokeheartgold's `src/overlay_bug_contest.c` (and its header), with its two tables,
`files/data/mushi/mushi_encount.csv` and `mushi_trainer.csv`, copied as numbers into `Data/world/johto/bug-contest.json`.
Plan 15's decision 5 recommended Serebii's numbers and a judging of our own; plan 20 · J1's pin of pret/pokeheartgold
came first, so the judging is HeartGold's and the decision's alternative is what was done.

- **Where and when**: the National Park, entered at the counter of either gate house into it (Route 35's and Route
  36's), on Tuesdays, Thursdays and Saturdays, once a day (`FLAG_DAILY_BUG_CONTEST_ENTERED`). The park is the one map
  (`NationalPark`): HeartGold's second park of the contest (`D22R0102`) is not imported; its five contestants are the
  overlay's `npcs` on that map's tiles, each shown by a flag of ours, and the park's own people and Trainers are hidden
  by `FLAG_BUG_CONTEST_UNDER_WAY` (HeartGold's `FLAG_UNK_24E`), both set and cleared by `bugcontest` alone.
- **Who goes in** (`BugContest_BackUpParty`): the first Pokémon of the team able to fight, alone; the rest wait at the
  counter and come back in their places with it as the park left it (`BugContest_RestoreParty_RetrieveCaughtPokemon`).
  HeartGold asks no question of which: neither does this game. A team with nobody able is turned away.
- **The stay**: twenty Sport Balls and twenty minutes of the game's own time, counted while the field (a script over
  it too) or a contest battle is on screen, never in a menu. The time running out in the park, or the last Sport Ball
  missed, ends it: an announcement and the player is brought to the gate they came in by for the judging. Walking out
  of the park into a gate asks whether the player is done (no sends them back in).
- **The battles**: ordinary wild battles with the player's own Pokémon, the field's step rate as the park's grass has
  it, the species and level from the day's table (`BugContest_GetEncounterSlot`: a roll of a hundred, the first row
  whose rate it reaches; the table is the first before the National Pokédex and after it the weekday halved:
  Tuesday's, Thursday's or Saturday's). BAG is the Sport Ball (`BattleConditions.OnlyBall`, the balls counted by
  `SpecialBalls`); nothing else of the bag may be used, and a Sport Ball is refused in any other battle. A Pokémon
  caught goes to neither team nor PC: the contest keeps it, and a second catch asks which to keep (the other is set
  free). *Cut down*: HeartGold shows the Sport Balls on the
  touch screen's button in the bag's place; ours is the small button where BAG stands, labelled with the count.
- **The others** (`BugContest_InitOpponents`): five of the ten (Don, Ed, Abby, William, Benny, Barry, Cindy, Josh,
  Samuel, Kipp), none twice, each a row of their eight fit for the day and the Pokédex, its score moved by
  `rand % (2 × spread) − spread`.
- **The judging** (`BugContest_JudgePlayerMon`): the species' worth from the table, plus the level × 100 ÷ the table's
  highest level for the species, plus the six IVs' sum × 100 ÷ 186, plus the HP left × 100 ÷ its most, each rounded
  down; nothing caught scores nothing. The six are put in order by HeartGold's selection sort, in which a later
  entrant takes a tie: the player is the last, so the player wins any tie. A level of a region crossed into is judged
  against the table's highest as that region maps it (plan 18 · W4).
- **The prizes** (`BugContest_Judge`): first a Sun Stone, or after the National Pokédex one drawn of ten (the Sun,
  Moon, Fire, Thunder, Water and Leaf Stones, then the Shiny, Dusk and Dawn Stones and an Oval Stone); second an
  Everstone; third a Sitrus Berry; anyone else a Shed Shell. *Cut down*: the results are a board of the six, told
  third, second and first, where HeartGold's announcer reads each out in the park with the contestants lined up.
- **Afterwards** the catch joins the team, or the PC when the team was full; the others go home. A battle lost in the
  park gives the contest up: the team is put back and the catch let go before the player wakes up.

## Field moves without a knower, the screens (2026-10-24, plan 12 · Q18, part b)

- **Who does the move.** A granted move (`FieldHelp.Granted`: the region's Badge held and the HM in the bag, the options' row on for the rules in force) is done by a helper of the game's own, made by `FieldHelp.HelperFor` from a generator seeded by the move, so it is the same Pokémon each time and `Dice` is not touched: a Staravia for Fly, a Bibarel for Surf and Waterfall, a Bidoof for the rest, level 20. It is never added to the team, the Pokédex or the save. A move a Pokémon of the team knows is still done by that Pokémon first.
- **The party menu** lists a strip of the granted moves that nobody on the team knows, under the cards; choosing one hands the game the move with place -1 (`PartyScreen.HelperUser`), and the game checks it where the player stands (`FieldMoveRules.Check`) exactly as it checks a team member's. The script that runs says that a wild Pokémon lent a hand (`usemove`) and the cut-in shows the helper.
- **Fly** keeps its flier on the map screen as a `Pokemon` (`TownMapScreen.Flier`), the team's member or the helper, and `common.UseFly` is started with it. Where Fly may go is the Fly screen's own business (plan 18 · W9).
- **The Pokétch's Hidden Moves app** (`PoketchApp.HiddenMoves`, last in the enum) shows while the row is on and is refused by `Poketch.Register`, so no save holds it and nobody gives it. It lists the granted moves.

## The Pokéathlon (2026-10-24, plan 15 · E6)

HeartGold's venue, kept by the user on 2026-10-12. pret/pokeheartgold has the Pokéathlon's save (`PokeathlonSave`,
`include/pokeathlon/pokeathlon_save.h`) in C but its events still in assembly (overlay 96), so the events' numbers are
our own on HeartGold's shapes, and the stats are plan 15's decision 6 (our own formula, no table from the games).

- **The stats** (`AthleteStats.Of`): Speed, Power, Skill, Stamina and Jump, one to five each, from a species' (or its
  form's) data. Speed is its base Speed in bands at 45, 65, 85 and 105; Power (twice its stronger attacking stat and
  once the other, over three) at 55, 75, 95 and 115; Stamina (HP and both defences, over three) at 55, 70, 85 and 100;
  Skill (Special Attack, Special Defence and Speed, over three) at 50, 65, 80 and 95, one more for a Humanoid or Arms
  shape and one less for a Ball, Fish, Blob, Squiggle or Heads; Jump starts at 3, one more under 10 kg, one less from
  100 kg and again from 300 kg, one less at 3 m or more, one more for a flier (Flying type, wings, Levitate), one less
  for a Fish, Squiggle or Blob that doesn't fly, one more for fast (base Speed 90 or more) legs. *Cut down*: HeartGold's
  stats have a range each that Aprijuice moves the Pokémon within; ours are one number, and Aprijuice waits for plan 20's
  Apricorns.
- **A course**: the player's three (no Egg; the team needs three that may go in) against three rival teams drawn on the
  course's own generator: a name each of eight of ours, three different species of Johto's Pokédex (no legendary or
  mythical Pokémon) and a knack between 0.45 and 0.85 that says how well they play. Three events, thirty frames a
  second as the original's, each a pure function of its seed (drawn from the course's generator as it begins) and the
  player's presses. The points earned are the player's total; the most a save holds is HeartGold's 99,999. *Stand-in*:
  the Practice Course of the three events built so far is the only course until plan 15 · E8 brings HeartGold's five.
- **Hurdle Dash** (Speed): each of the three runs a leg of 1,200 units over seven hurdles 150 apart; a runner gathers
  speed at 0.1 + 0.04 × Stamina a frame to 3 + 0.6 × Speed, a jump keeps it up 10 + 3 × Jump frames, and a hurdle met
  on the ground stops it dead for 24 − 3 × Skill frames. A minute at most; a team through scores a point for every
  twelve frames left of the minute, one still running 30 × the share of the way it came.
- **Block Smash** (Power): forty seconds at a stack of blocks of 40 each. A press hits once the last swing is over
  (8 − Speed frames) for 2 × Power, one more if pressed within Skill frames of the swing's end; every hit tires the
  hitter by 12 − 2 × Stamina out of 100, and at 100 it hits for its Power alone; the two resting get a point back a
  frame; a switch (the cancel button) sends the next in and takes twelve frames. Two points a block.
- **Ring Drop** (Power): a minute on a platform of radius 100, one of each team's three on it at a time; the arrows push
  at 0.12 + 0.05 × Speed a frame (a tenth lost a frame), the button lunges at 1.5 + 0.5 × Power and rests
  30 − 3 × Skill frames, and two that meet part as heavy things do, each as heavy as 2 + Stamina + Power ÷ 2 (harder
  while either lunges). Whoever leaves the platform is out for a second and a half and the next of the team comes on at
  its own edge; the knock-off is the last toucher's if it touched within three seconds. Fifteen points a knock-off, five
  off a fall, never below nought.
- **The rivals** play by the same rules, each frame's input their own mind's on the event's generator: a hurdler aims
  its jump at the middle of the leap, off by more the less its knack, and now and then forgets; a smasher presses on
  most frames and changes a tired hitter for a rested one; a pusher goes for the nearest and back from the edge.
- **The scoreboard**: the four by their totals, a tie to the player and then to the team named first; a course's best
  score is kept with the runs and the wins (`SaveData.Athlon`). *Cut down*: HeartGold's medals, its bonuses for what a
  Pokémon did in an event and its records board are plan 15 · E8's; the events are played on a board of their own, not
  in the dome's arena, and the player's team is drawn as menu icons.
- **Where** (plan 20 · J13): Route 35's gate house's west door (warp 2) leads onto HeartGold's own square
  (`MAP_POKEATHLON_DOME`, the map `PokeathlonDome`), a map of its own with the dome at its head, drawn as a `Hall` by
  Johto's catalogue (`poke_c_dome`; plan 15's `BuildingKind.Dome` is not made, and nothing of the dome stands on the
  overworld). Its three doors lead into the lobby (`PokeathlonDome_1F`), where the clerk in the middle of the long desk
  runs `common.AthlonDesk`. *Cut down*: the lobby's ways down to the event rooms and up to the stands stay shut, since
  the events are played on their own board; the owner's welcome on a first visit and Whitney's visits are left out.

## Ways to play (2026-10-24, plan 13 · V1)

NEW GAME asks one more thing, after "start a new game?" when there are saves: how the game is to be played. The answer is
a `RunRules` record (`Models/RunRules.cs`) kept whole in the save (`SaveData.Run`) and never changed. Normal, which is
every choice off, is Platinum's game exactly and is what the panel opens on, so the usual NEW GAME is one more press of A.
A save from before the choice has no `Run` and reads as Normal; a Normal game writes no field, so no save version was raised.

What each choice will change, and which session gives it effect (V1 only asks and remembers; **nothing is enforced yet**):

| Choice | Meaning | Session |
|---|---|---|
| Difficulty | Easy / Normal / Challenge: trainers' levels, held items, AI, EXP and prices | V2 (done, below) |
| Level cap, Trainers scale | EXP stops at the next leader's level; trainers follow the lead | V3 (done, below) |
| Nuzlocke, Dupes / Shiny / Gifts clauses | a fainted Pokémon is lost; one catch for each area | V4 (done, below; V5 the Gifts clause) |
| Monotype, Party size | every Pokémon of a type; fewer than six carried | V5 (done, below) |
| No items in battle, No Center, No catching, No running, Set | refused by the rules where each is enforced | V5 (done, below) |
| Shiny odds | 0 is the rules' own (what Normal keeps); 4096, 1024 or 512 otherwise | V5 (done, below) |
| Randomise Pokémon, Randomise items, Seed | other species and other items, the same ones for a seed | V6 (done, below); abilities, moves and types are V7's |

Presets (NORMAL, NUZLOCKE: the Nuzlocke with the dupes and shiny clauses; HARDCORE: that plus Challenge, the level cap, no
items in battle and Set) are records with some fields set; a run that matches none is CUSTOM. The clauses show only while
the Nuzlocke is on and are cleared when it is turned off. `RunRules.Current` is the run in force, set by the engine as a
game begins or loads, for the one rule with no caller to hand it to (the shiny roll). No region is chosen here: every game
begins in Sinnoh (plan 18 · W10).

### Challenge and Easy (plan 13 · V2)

Our own numbers: Platinum has one difficulty. `Models/DifficultyRules.cs` (GPU-free) is all of it, and **Normal changes
nothing**: `Prepare` hands back the very trainer it was given, the EXP and the prices are the rules' own.

- **Trainers** are changed on a copy, in `GameEngine.StartTrainerBattle` after a crossed region's mapping, so the maps, the
  seeded `Dice` and every shot are as before. The first battle of a game (the rival's lesson beside the player's first
  Pokémon) and a Master Trainer's are left as they are.
- **Levels** move by the trainer's class: a Gym Leader, one of the Elite Four and the Champion by 4, the rival, a Commander
  and a Galactic Boss by 3, everyone else by 1. Challenge raises (never past the rules' level cap), Easy lowers (never
  below 2, or 1 where a Pokémon was).
- **Challenge** also: gives a Leader, one of the Elite Four and the Champion with fewer than six one more Pokémon, the
  first species of their own `_rematch` team (Platinum's, `trainers.json`) whose line isn't on the team, at the level of
  their best and at the stage that level allows (a Golem is a Geodude below 25, a Graveler below 30: a level evolution
  needs its level, friendship 16 and any other 30), knowing what that level taught it, before the team's last; gives the
  best Pokémon of a Leader, the League, the Champion, the rival, a Commander and a Galactic Boss a Sitrus Berry where it
  holds nothing, and the extra Pokémon Leftovers; thinks with every routine of the AI (Basic to Harassment; not the tag
  battle's, which the battle adds); and pays half as much again.
- **Easy** also: three halves of the EXP a battle gives (`BattleCore.Give`, from `CoreSetup.Difficulty`; a recorded battle
  keeps it and refuses to be replayed under another) and a quarter off what a counter asks (`ShopScreen.PriceOf`; selling
  is unchanged, so buying and selling back never earns).

### The level cap and trainers scaled to the lead (plan 13 · V3)

Both are off under Normal, and nothing moves: a battle's `RunLevelCap` is 0, a trainer is the very object it was.

- **The cap** (`Models/LevelCaps.cs`, GPU-free) is the next Gym Leader's best Pokémon by the Badges held in the region the
  player stands in: Sinnoh's 14, 22, 26, 32, 37, 41, 44 and 50 (plan 02's pacing targets, held to `trainers.json` by a
  test), then the Champion's 62 once all eight are held, and none once the region's Hall of Fame is entered. A region with
  no table (every other one until its own plan writes it) is uncapped.
- **EXP** (`BattleCore.Give`, from `CoreSetup.RunLevelCap`, which the engine fills from `GameEngine.CurrentLevelCap()`):
  after Easy's three halves, only what takes the Pokémon to the start of the cap's level goes in (`LevelCaps.Allowed`), so a
  capped Turtwig stops at exactly 14 with no progress towards 15; one on or past the cap gains nothing and no gain is told.
  Each capped Pokémon that fought hears "can't grow past Lv. N yet." once a battle, after its level's line. Effort still
  comes (a capped Pokémon is not a level-100 one). A recorded battle keeps the cap and refuses to be replayed under another.
- **Candies**: a Rare Candy or an Exp. Candy on a Pokémon on the cap's level is NOT ABLE in the bag and says the same line;
  the Exp. Candy gives no more than the way to the cap. The Day Care's levels are not held (plan 13 asks for the battle and
  the candy only).
- **Scaling** (`DifficultyRules.ScaleTo`, from `Prepare(trainer, rules, leadLevel)`): the team's best Pokémon becomes the
  level of the player's lead and every other moves the same number of levels, so the spread is kept; up or down, as the
  panel's help says. It is laid first, so Challenge's and Easy's levels sit on the scaled team. The first battle and a Master
  Trainer's are left alone, as for the difficulty. A Pokémon is not evolved to suit its new level.

### The Nuzlocke (plan 13 · V4)

The community's rules, which Platinum doesn't have; `Models/Nuzlocke.cs` and `Models/RunState.cs` (GPU-free) are all of
them, and **a run without the Nuzlocke writes and refuses nothing** (`Meet` returns Free and keeps nothing, a battle is
handed no terms, `BattleCore.Nuzlocke` is null, so the golden battles are as they were).

- **One catch in each place.** A place is the area's key on a streamed map (each of the Great Marsh's six areas its own,
  `great_marsh_1` to `_6`), a room's or a hand-made map's name otherwise. The first wild Pokémon met there in the field
  (`StartWildBattle`, a roamer included) spends it, caught or not: `RunState.AreasMet`. A later one gets
  `NuzlockeTerms(AreaSpent)` and every ball is refused in its battle, a Safari Ball too (`BattleCore.WhyNotItem`, for the
  player's side only). The Bug-Catching Contest's catch is the contest's and counts nothing; a Pokémon a script puts in the
  way (`wildbattle`: the legendaries, the lakes' guardians) is free to catch and spends nothing, as is a gift; under the
  Gifts clause (plan 13 · V5) a gift and such a static encounter are the place's one chance (below).
- **The dupes clause**: a wild Pokémon whose line (`PokemonSpecies.Offspring`, the first of the line, or the species itself)
  has a species in the Pokédex's caught list passes: no ball, and the place stays unspent for the next. **The shiny
  clause**: a shiny is free and spends nothing.
- **The fallen**: after every battle but the assistant's lesson and the first battle (the tutorial), the team's fainted
  Pokémon (not an Egg, which counts as fainted) go into the graveyard (`RunState.Graveyard`, each as `SavedPokemonData`
  with the place and the play time), **out of the team for good**, before anything heals them (a partner's heal after a
  battle, a script's lost battle). The graveyard is neither the team nor a box: nothing can withdraw, heal or use what is
  in it, and the box rule is simply that a Pokémon never reaches a box fainted (a box holds the living only). A Pokémon
  that faints and is brought back within one battle is not buried: only those down when it ends.
- **No revive**: a Revive, a Max Revive, a Revival Herb and Sacred Ash (`item.Use` with `revive` or `reviveAll`) are refused
  in battle (`battle.nuzlockeNoRevive`) and in the bag (`BagScreen.NoRevives`); other medicine is the same as ever.
- **The end** (decision 2's recommendation): a whiteout, a battle lost that no script lets the story go on from, ends the
  run (`RunState.Ended`, `EndedBy` "whiteout"), even with Pokémon in the boxes. The whole team is in the graveyard by then,
  the player comes round at the Pokémon Center as ever with nobody, and the game is written once, as it ended
  (`WriteSave(runsLastWord)`). From then on nothing writes the slot (the save menu, SAVE AND QUIT, a script's `save`): the
  save loads, to look at the card, the graveyard and the boxes, and plays on with no battle to fight. A script's lost battle
  that lets the story go on buries the fallen and ends nothing, even if it leaves the team empty.
- **A name for every catch**: a Pokémon caught under the Nuzlocke in the field (or the Great Marsh) is named as the field
  comes back, on plan 12 · Q19's keyboard (`GameState.Nickname`, `GameFlow.NameAfterBattle`), which closes back to the field;
  B on an empty name keeps the species' own. Platinum asks after every catch, in the battle: plan 06 still owns that.
- **Recorded battles** don't keep the terms: they only refuse choices, so the answers a battle was given replay the same.

### Restrictions and shiny odds (plan 13 · V5)

Community rules and our own, all off under Normal (`RunRules.BattleTerms()` is null, `Party.Limit` is 6, `Has` is false, the
shiny roll reads the rules' own odds), so nothing a Normal game does moved. Each is read at the one place it is enforced.

- **The team's size** is `Party.Limit` (6, or the run's `PartyLimit`, set by the engine as a game begins or loads); every
  caller already asked `IsFull`, so a catch (`Caught.ToBox`), a gift, a withdrawal and an Egg follow it. The battle's copy of
  the team carries the limit. A gift into a full team goes to the PC, as ever.
- **Monotype** (`RunRules.MayCarry`): a Pokémon may be on the team only if it has the type; **the starter's whole line is
  exempt** (it is given before any type could be chosen for). A ball is refused in a battle against a wild Pokémon without the
  type (`battle.runMonotype`); a gift of another type goes to the PC with a line saying so (`IScriptHost.GivePokemon`, both
  hosts) and the PC refuses to withdraw it or put it on the team (`PCScreen.WhyNotCarry`). Left: a trade, a hatched Egg, an
  evolution into another type: the rule is not enforced on those.
- **No items in battle** (`RunTerms.NoItems`) is the player's side only (the trainers keep their items), **balls excepted**:
  a wild battle's BAG still opens for a ball, since a Nuzlocke with no ball is no Nuzlocke, and everything else is refused with
  the closed bag's line. Where no ball could be thrown either (a trainer's battle, or catching forbidden) BAG is greyed as in a
  Master Trainer's battle (`BattleCore.BagShutByTheRun`, `BattleEngine.BagClosed`). Not in the Great Marsh, Pal Park, the
  assistant's lesson or the Bug-Catching Contest, whose bags are their own. (`BattleSetup.NoBattleItems` stays the Master
  Trainers' rule for both sides.)
- **No catching** refuses every ball for the player's side in a wild battle (`battle.runNoCatching`), the Safari's included;
  not Pal Park's or the contest's, which are those places' own. **No running** refuses RUN from a wild Pokémon
  (`battle.runNoRunning`); a trainer's battle never offered it, and the Great Marsh's RUN is the marsh's own.
- **No Center**: `common.Nurse` asks `if rule nocenter` and declines in a line of our own, healing nothing. Mom's rest, the
  Hall of Fame and the other regions' own healers are not asked (left).
- **Set**: the Battle Style row reads Set and cannot be changed while the run's `SetStyle` is on (`OptionsScreen.StyleLocked`,
  set from the game, never from the title), and `OffersSwitch` is off in every trainer battle.
- **Shiny odds**: `RunRules.ShinyOdds(rulesOdds)` is the run's denominator, else the rules' (8,192 Platinum's, 4,096 the
  modern ones); `Pokemon`'s constructor reads it through `RunRules.Current`. **The Shiny Charm** is a key item of the data (the
  importer brings it in; nothing in the game gives it yet, plan 06 · R28's): with it in the bag a wild Pokémon met in the field
  and a gift get two more rolls (`ShinyCharm.Roll`, `Pokemon.RerollForShiny`: a fresh personality drawn, the first that is
  shiny becomes the Pokémon's, with the nature and gender a lead's ability chose and the ability, form and stats that follow).
  Nothing is drawn without the charm, so no seeded draw moves.
- **The Nuzlocke's Gifts clause**: with it on, a gift (`GivePokemon`) spends the place's catch and a Pokémon a script puts in
  the player's way (`wildbattle`) is the place's one chance, caught or not (`Nuzlocke.Gift`, `MeetStatic`); the starter is a
  gift of the briefcase, not of `GivePokemon`, and spends nothing. A run that has ended shows RUN OVER in red on the CONTINUE
  card and the save panel (`ModernUi.SaveMark`).

### The randomiser: species and items (plan 13 · V6)

Our own, off under Normal (`RunRules.Random` is null, `Randomizer.For` answers null, nothing asks it, the golden battles are as they were). `Models/Randomizer.cs` (GPU-free) is a map decided by hashing the seed with a number or a place (`Core/SplitMix.cs`: SplitMix32, FNV-1a for a place's text, never `Dice`, `System.Random` or a string's `GetHashCode`), so one seed is one game on every machine and no order of asking changes it. It edits no data: the map is asked where the species or item is used. A seed is 0 to 999,999,999, typed on the title or rolled once from `Dice.Shared` as the randomiser is turned on; `RandomizerOptions` (seed, Species, Items) is kept whole in `SaveData.Run`.

- **A species** goes to another of the games' (never one of ours) in the same place in its line (a lone species, a first, a middle or a last stage), legendaries and mythicals among themselves, within a tenth of its base-stat total at Platinum's values (so the map does not move with the rules in force), the margin widening a tenth at a time until four are found. **A line stays a line**: the evolution of a species goes to one of the evolutions of what its pre-evolution went to, where that has one; otherwise it is chosen alone. It need not be one-to-one, never maps to itself unless nothing else fits, and a name that is no species of the games is left alone.
- **Where it is asked**: a wild Pokémon in `StartWildBattle` (the grass, the water, rods, rocks, the Great Marsh, the contest, a horde and a second of a pair; **not a roamer**, which the story names), a trainer's team in `StartTrainerBattle` on a copy (`Trainer.WithTeamOf`; the same personality, IVs, level, held item and capsule, never shiny, knowing what its level teaches; a Master Trainer's one species and an empty team stay; the first battle is mapped too, so the rival's team is the mapped one), the partner's team, a script's `givepokemon` and `wildbattle` (`ScriptRunner`, so both hosts), the assistant's Pokémon in the catching lesson, and the briefcase. Order on the copy: the species, then the difficulty's levels. **Left as they are**: a gift with a move of its own (the Dragon Shrine's Dratini), a form named by a script, Eggs, fossils, trades, Pal Park's six, roamers and the lesson's wild Pokémon; and **any species a script asks the team for** (`if has "Regirock"`: `ScriptLibrary.SpeciesAsked`), which would otherwise never come.
- **The starters**: each briefcase's three slots go to three first stages (that have a line to grow into) of three different lines, decided once in the regions' order (`Randomizer.Starters`, and `Species` says the same of a slot). The briefcase shows the mapped three and gives the mapped Pokémon, **while the story keeps the slot** (`StarterSelectScreen.Slots`, `ChosenSlot`; `StoryState.ChooseStarter(slot)`): `if starter "Turtwig"`, the rival's team ids (`rival_route_201_turtwig`, whose Chimchar is mapped like any other) and the assistant's still choose by the slots, and `{starter}`, `{rivalstarter}` and `{assistantstarter}` name the mapped species. A Monotype run's exempt starter is the mapped one's line (`RunRules.MayCarry`).
- **An item** found, hidden or given (`find`, `give`; not `additem`, shops, fossils or prizes) goes to another of its pocket and about its price (a half to twice, widening until four are found; a price of nothing only to another of nothing; a TM to another TM, one at a time), among Platinum's own (`Id` up to 467), decided by the seed and the place: **the ball's or hidden item's own flag**, else the script and its line, so a ball is one item every time and another ball another. **Never swapped, in or out**: a key item, an HM, anything that can't be tossed, a Mega Stone or Z-Crystal, and every item a script takes, asks for or asks a Pokémon to hold (`ScriptLibrary.ItemsAsked`: the Coin Case, the Card Key, the Orbs, and Poké Balls and Oran Berries too, which one script or another asks for).
- **Completable**: a Sinnoh Pokédex of other species can never be seen whole, so a run that randomises species counts it all seen once Sinnoh's Hall of Fame is entered (`Randomizer.SinnohSeen`, the professor's `SEEN`), which is what opens the National Pokédex there. The field moves need a Pokémon that can learn them, which a randomised party may lack: the options' Field helpers row (on unless turned off) lends a wild helper once the Badge and the HM are held, and the HMs are never swapped. The area page of the Pokédex shows where the species that stand for a species live (`Randomizer.Sources`, `Habitats.Of(sources)`).
- **Shown**: the card's mark reads the preset and the seed (`ModernUi.RunMark`), the Trainer Card's Run page lists the three rows.
- **Left**: abilities, moves and types are V7's; random evolutions (the plan's switch to loosen lines) are not built; trades, Eggs, fossils, the roamers and the story's fixed Pokémon stay, and a randomised Nuzlocke counts the species met.

## The Dragon's Den after the League, Johto's swarms and the Move Relearner (2026-10-25, plan 20 · J13, part c)

| Subject | HeartGold | This game |
|---|---|---|
| What begins the Den's post-game | The rival's battle in Mt. Moon (`D02R0101`) sets `VAR_UNK_40FC` 1 | The first arrival at the Dragon's Den once `JohtoHallOfFame` is set does (Kanto's part of Johto is J15's, which puts the Mt. Moon battle in front of it) |
| The scene | The rival, Lance and Clair; a multi battle, the player with the rival against Lance and Clair (`TRAINER_PARTNER_RIVAL_1` to `_3` by the starter, `CHAMPION_LANCE_3`, `LEADER_CLAIR_CLAIR_3`) | The same, in our own words, as a `battle a and b with partner` of two overlay trainers; it ends at `VAR_UNK_40FC` 2 |
| The boy and Clair afterwards | The boy on Tuesdays and Thursdays; Clair by the hour (6 to 9 until her number is registered, 16 to 18 after), her number for the asking, a photograph with her | The boy on Tuesdays and Thursdays; Clair on the morning, then the afternoon once registered (the time of day, not the hour); no photograph |
| Swarms | One place of twenty a day (`sSwarmMapLUT`), met in the grass, on the water or on a rod, turned on in Oak's lab and told by Pokémon Talk | The same twenty by the day's number modulo 20; they begin at the Johto Hall of Fame (`swarms on`); the radio tells the day's one time in two; the nine Kanto places are in the table and open nowhere yet |
| The Move Relearner | A Heart Scale for a move the Pokémon could have learned by its level and doesn't know | The same (`Relearning.MovesFor`, the party menu's REMEMBER list) in Pastoria and in Blackthorn; the scale goes only once the move is learned; Fallarbor's relearner still only talks |

## Route 40's way to the Battle Frontier (2026-10-25, plan 20 · J13, part d)

HeartGold's gate on Route 40 leads north to a Battle Frontier of its own (`battle_frontier_frontier_access` and the
facilities behind it). This game has one Battle Frontier, Sinnoh's (plan 06 · R18), and the user decided on 2026-10-25
that **Route 40's gate is a second door to it, with one set of records**: a streak, the Battle Points and the prints are
the save's own (`FrontierRecords`, one field of the save, `SaveData.Frontier`), the same whichever door the player came
by. Plan 18's rules stand in the way of a plain door: every map belongs to exactly one region, and a warp never crosses
regions.

- **The way chosen: a crossing, not a warp.** The gate's north door (warp 1) stays shut, and the guide HeartGold stands
  before it (`w40r0101_gsmiddleman1`, shown always: `FLAG_UNK_25B`, which hid him, is HeartGold's and nothing here sets
  it) walks the player along the road: a question, the script's black screen, one line over it, and `travel to
  "BattleFrontier" 47 51 up`, which goes through `GameEngine.CrossTo`, the one way every region is crossed into (the
  vessels' `TravelTo` and Fly's flight to another region are its other callers). So the team goes along whole and each
  Pokémon is given the region it left (`Crossing.Depart`, `GiveIds`, option B), Sinnoh's Pokédex is entered and its
  rival is the one `{rival}` names, the grounds' music and name sign come with the arrival, and nothing of the levels
  is set by the crossing. The player comes out on the grounds on the tile the gate from the Fight Area lets out beside
  (47, 51, facing the plaza). *Why*: of the two the user's brief named, this is the one that adds no map and copies
  nothing. Johto-side copies of the grounds would have to be a second `BattleFrontier` in Johto's list leading into
  the facilities' rooms, which are Sinnoh's maps, so its warps would cross regions all the same, or the five
  facilities' lobbies, corridors and battle rooms (twenty-odd maps, each with its arrival script) copied as Johto's,
  whose scripts name their own maps by name: two of everything to keep alike for no gain, since the records are one set
  anyway.
- **The way back is a guide, not a harbour.** The README's "Regions" forbids a vessel back to a region left; the road is
  no vessel and both ends are the same Frontier, so the way back is a guide of our own on the grounds (an overlay
  `npcs` person of `battle_frontier.json`, `battle_frontier.JohtoGuide`, on 46, 51 beside the arrival tile), who asks
  and crosses back with `travel to "Route40_FrontierGate" 5 4 down`, setting the player down before Route 40's guide.
  *Our ruling*: he stands there for **anyone who has entered Johto's Hall of Fame** (`shownBy` `JohtoHallOfFame`), not
  only for someone who came by Route 40, so the Frontier is one place with two doors: a Sinnoh Champion who has finished
  Johto may leave by either. Nothing new is saved (no flag of "came from Johto"), and a Champion who never went to
  Johto never sees him.
- **When it opens: Johto's Hall of Fame.** HeartGold's own timing couldn't be read (the decompilation's cache isn't on
  this machine); Platinum's Frontier is post-game, and the facilities are Platinum's, so Route 40's guide offers the road
  once `JohtoHallOfFame` is set and says before that that only Trainers of the Hall of Fame may go. In a game begun in
  Sinnoh, which is every game but one begun by `--region`, a Johto Champion is a Sinnoh Champion too and has had the
  Frontier's Sinnoh door since the Fight Area's ship.
- **What a visitor from Johto finds.** The facilities ask nothing of the region: a Frontier battle copies the team at
  level 50 at most, fights trainers of `frontier.json` (never relevelled by a region's mapping), tells the Pokédex
  nothing, makes every Pokémon obey and takes no money or EXP, so Johto's levels, Pokédex and badges don't enter into
  it. The grounds are Sinnoh's: Fly from them asks Sinnoh's Fly badge, as it does anywhere in Sinnoh, and the guide is
  the way back for whoever has none. Each region's place to come back to (`SpawnLocations.VariableOf`) is untouched:
  neither road has a Pokémon Center, and a Frontier battle never wakes the player in one.
- **Known edge**: a game begun by `--region Johto` that crosses here may walk on through the gate house into the Fight
  Area, Sinnoh's post-game, without Sinnoh's story; no game begun in Sinnoh can tell the difference, so the gate house is
  left as it is.

| What | HeartGold | Here |
| --- | --- | --- |
| The Frontier | Its own, north of Route 40's gate, with five facilities of its own | Sinnoh's, reached from Route 40 by a crossing; one set of records |
| The gate's north door | A warp onto the Frontier's road | Shut; the guide before it takes the player along the road |
| The way back | The same door | The Johto guide on the grounds, a crossing back to Route 40 |
| When it opens | Not read (no cache) | Johto's Hall of Fame |

## Secret bases (2026-10-25, plan 21 · H10)

Emerald's `src/secret_base.c`, `src/decoration.c`, `src/data/decoration/header.h` and `data/scripts/secret_base.inc`;
the lines are our own.

- **Kept as Emerald has them**: a spot's id names its room by its tens (`SECRET_BASE_TREE2_1` is 171, room `Tree2`);
  the four rooms of each of the six kinds and the tile before each PC; one base at a time, a second spot emptying the
  first with everything in it back in the decoration PC; Secret Power known by a Pokémon of the team, and no Badge for
  it; sixteen decorations standing at most; the PC's pockets (ten desks, chairs, plants, posters and cushions, thirty
  ornaments and mats, forty dolls); each decoration's shape, kind, placement and price; where each placement may stand
  (on floor, a poster only on the wall over the floor, a tall piece's top row before the wall); the sellers' lists and
  prices (Slateport's market, Lilycove's fifth floor); the Secret Power man on Route 111 and TM43.
- **Rulings**:
  - The question whether to make a base at a spot is asked outside it, before Secret Power, where Emerald goes in first
    and asks there; answered no, nothing is made. Facing the player's own base goes in, with no question.
  - A spot's kind is told by its id, not by its tile: the reader keeps all of Emerald's spot behaviours as one
    `SecretBaseSpot`.
  - The wall a poster hangs on is every blocked tile with floor right under it that is no PC (Emerald's
    `MB_SECRET_BASE_NORTH_WALL`, which the reader doesn't keep); the floor every open tile but the door's.
  - Dolls and cushions stand on the floor, one to a tile, as every decoration does here; Emerald stands them on desks
    and mats too. A big doll stands on one tile, two tiles tall.
  - Kept clear: the tile inside the door and the tile before the PC (Emerald keeps clear the tile the player stood on
    when they turned the PC on, which is that one), and the visitor's spot is no matter, as no friend's base comes.
  - The Solid Board isn't laid over a hole, the breakable door and the mud ball don't break, and the mats make no sound.
  - The base's door leads back to the tile the player opened it from, facing away from the spot.
  - REGISTRY says that no friend's base is registered: friends' bases come by record mixing, and nothing is linked.
  - PACK UP takes everything back into the PC and gives the base up, and the player is put out at its spot (Emerald's
    `MoveOutOfSecretBase`).
  - A base's room wears a house's interior until the six kinds have their own looks.

| What | Emerald | Here |
| --- | --- | --- |
| Making a base | Secret Power at a spot, in, then "make it here?" | "Use Secret Power?" at the spot, then in |
| A friend's base | Record mixing, its trainer and the registry | None; REGISTRY is empty |
| Dolls and cushions | On the floor, a desk or a mat | On the floor |
| The decoration PC | Eight pockets by kind | One list, the same rooms by kind |

## Scott's way to the Battle Frontier and the League's rematches (2026-10-26, plan 21 · H9, part c)

**The Frontier.** Emerald's Scott gives a Frontier Pass and the ferry to an island of its own; this game has one Battle
Frontier, Sinnoh's (plan 06 · R18), so Hoenn's door is the third of the pattern of "Route 40's way to the Battle
Frontier", with one set of records. Scott stands on Lilycove's quay beside the harbour's desk (`lilycove_city_harbor.Scott`,
13, 12, `shownBy` `HoennHallOfFame`) and gives **the pass as a flag** (`FLAG_RECEIVED_FRONTIER_PASS`: there is no item, so
no table, no Romanian name and no bag line to add), then asks and crosses with `travel to "BattleFrontier" 47 51 up`
(`GameEngine.CrossTo`: the whole team, each Pokémon given the region it left, Sinnoh's Pokédex entered). On the grounds
a second guide, `battle_frontier.HoennGuide` (48, 51, facing the arrival tile beside Johto's at 46, 51, shown by
`HoennHallOfFame`), crosses back to the harbour on 12, 12 beside Scott. Both are there for anyone who has entered Hoenn's
Hall of Fame; nothing new is saved. Emerald's own facilities (the S.S. Tidal, the island's seven halls) stay plan 21 ·
H12's and plan 14's: a Hoenn Champion plays Sinnoh's five.

**The rematches.** Emerald has no rematch for the Elite Four, the Champion or Wally; the user's brief gives them the
PokéNav's Match Call, as Johto's trainers have the Pokégear's. Each of Sidney, Phoebe, Glacia, Drake, Wallace and Wally
(the one who waits by Victory Road's way out) is **registered by `addcontact` the first time they are beaten** (in their
room's script, and Wally's), and asks a **Champion of Hoenn** for a rematch when rung, or ringing, on their day and hour
(`phone.json`: Monday day, Wednesday night, Friday morning, Saturday day, Sunday day and Tuesday morning), by the rule
of `PhoneRematch` (`Wants` reads the contact's region and asks Hoenn's Hall of Fame for Hoenn's). The call sets the
waiting flag (`FLAG_PHONE_REMATCH_<ID>`; the call's script sets it too, since the story test wants every flag a script
reads written by a script). **The waiting person is in their room, not on the road**: a League room's script (Wallace's is
the scene of the Champion's room) battles the rematch team when its flag is set and clears it, so no one spins in a room
the player walks through once by its doors. **One level each**: a rematch is the same team at +8 levels (+10 for Wally),
the same species, moves, items and AI, with prize money one and a half times, written by hand as
`hoenn_<who>_rematch` in `Data/our-trainers.json` and named by `Data/world/hoenn/rematches.json` (the importer rewrites the
region's `trainers.json`, so the rows are read from the file beside it by `TrainerDatabase`); the rows hold, and every
rematch battle is the room's own, never the Vs. Seeker's.

## Apricorns, the Box and Kurt's balls (2026-10-25, plan 20 · J6)

HeartGold's Apricorn trees, its Apricorn Box and Kurt's workshop, as ours (`Models/Apricorns.cs`, `Data/apricorns.json`).

- **The trees.** The 24 `bonguri` objects of Johto's area files are trees (`NPC.ApricornTreeType`, a thing of the map in the way like a person, spoken to through `common.ApricornTree`). **Their colours are ours**: HeartGold gives each tree a colour by its map, but nothing the pinned sources carry says which, so `apricorns.json` gives each tree one by the route it stands on (Route 37's three are Red, the colour of a Level Ball; all seven colours grow somewhere). A tree **bears one Apricorn a calendar day**: it has its fruit on until it is picked and again from the next calendar day (`GameClock.Today`; the hour does not matter), the day of its last picking kept in the save by its area and id. Picking asks, and fills the Box or leaves the fruit on the tree if the Box already holds 99 of the colour.
- **The Box.** A key item (`Apricorn Box`, `fieldUse` `ApricornBox`, from PokeAPI's `apricorn-box`) that the man in Route 30's Apricorn house gives (our placement; a tree met without it says the player has nothing to keep the fruit in). The Apricorns are not bag items but counts in the Box (99 of a colour at most), so no pocket fills with them; the Box is looked into from the bag (back to the bag), the item button, or a script's `apricorn box`.
- **Kurt.** He is given **every Apricorn of one colour** the Box holds, takes **one order at a time** (refused while the balls are in the works, and while they wait to be collected) and has **one ball for each Apricorn on the next calendar day**: Red a Level Ball, Blue a Lure Ball, Yellow a Moon Ball, Green a Friend Ball, Pink a Love Ball, White a Fast Ball, Black a Heavy Ball (the games' own pairing). The count is our own choice: one for each Apricorn, so that the one a tree gives a day is worth a ball. He asks, once the Pokégear is in the bag, to be registered in it (`addcontact kurt`; `phone.json` already lists him as type 4, a number that does not ring on its own), **and rings once, the first step after the day has passed**, to say the balls are ready (`GameEngine.KurtRingsOnAStep`, before the other contacts' chance). A save from before has an empty Box.
- **The balls' rules** were plan 06 · R28a's and are unchanged: the later games' bonuses of `CatchCalculator.Later.cs` (the Fast Ball 4× from a base Speed of 100, the Level Ball 8×/4×/2×, the Lure Ball 4× on a battle begun by a rod, the Moon Ball 4× for a Moon Stone evolver, the Love Ball 8× for the same species of the other gender, the Heavy Ball by weight on the species' rate, the Friend Ball a friend of 200), under the modern rules. Under Platinum's rules they keep the plain Poké Ball rate, as they did. J6 gives them looks (`BattleBall.LookOf`, the style guide's "Poké Ball (G8)") and releases of their own.

## Hoenn's contests and Pokéblocks (2026-10-26, plan 21 · H11)

Emerald's `src/contest.c`, `contest_effect.c`, `contest_ai.c`, `contest_util.c`, `src/data/contest_moves.h`,
`contest_opponents.h`, `berry_blender.c`, `pokeblock.c` and `data/scripts/contest_hall.inc`; the lines are our own.

- **Two contests, by region.** Sinnoh plays the Super Contest (plan 06 · R17a: visual, dance, acting) and **Hoenn
  plays Emerald's contest**, a different game: four Pokémon, five rounds of four appeals, one move each. The two share
  the quality (`ContestStat`), the ribbon record (`Pokemon.Ribbons`, the ribbon table) and the Poffin/Pokéblock
  condition (`Cool`… `Sheen`), and nothing else: `MoveData.Contest` is Platinum's, `MoveData.HoennContest` Emerald's
  (kept in `Data/hoenn-contest.json`, written by `tools/DataImporter -- --hoenn-contest` from the pinned pokeemerald,
  so that the importer's own files need no merge).
- **The data**: 48 effects (an appeal and a jam, each in tens of points), 354 moves of the first three generations
  (an effect, a quality, up to five combos, the starter it is) and 96 contestants (species, moves, quality pools, rank,
  and whether they appear only before or only after the game's end). A move Emerald never gave a number is a plain
  appeal of 40 in the quality of its type. Stomp's line in the source has no field name; the importer reads it.
- **The first round** orders the turns: each contestant's condition is its stat of the contest's quality plus half of its
  two neighbours' and its sheen (`CalculateContestantRound1Points`), the best going first; ties go to the order they
  were drawn. The score of the whole contest is that condition plus **twice** the points of the five appeals.
- **An appeal** is the move's effect: the hearts it gives (ten points each, to eight), the **jam** it takes off the
  contestant just before, a **combo** (a starter, then the move that follows it, which doubles the base appeal and takes
  the judge's eye), a **repeat** (minus twenty, then thirty… for the same move again), the **crowd** (a meter of five
  lights, stirred by moves of the contest's quality and cooled by clashing ones; the fifth light bursts for sixty), a
  **condition** raised in tens (each ten is a star and adds to every appeal after it) and **nerves**, which the
  startling move gives and the calm one takes away. A move that asks to go **earlier or later** bends the next round's
  order; one that gives everything leaves **no more turns**.
- **The opponents' minds** (`AppealRound.Ai`) are Emerald's `ContestAI_ChooseMove`: the scripts' scoring of each move
  from what has happened so far, a draw on the round's generator among the best, so a seed and the player's choices
  are the whole contest. The three are drawn from the table by the contest's quality and rank (`Candidates`), before or
  after the game's end by the region's story flag.
- **Ranks and ribbons**: Normal, Super, Hyper and Master; a rank is entered only with the ribbon of the rank below in
  that quality (`GetContestEntryEligibility`), a win gives that rank's ribbon (`RIBBON_HOENN_COOL`, `_SUPER`, `_HYPER`,
  `_MASTER`), a win of a rank already won gives nothing but, at the Master Rank, a **Luxury Ball**. A Master Rank win of
  **800 points or more** brings the painter, who hangs the picture in the Lilycove Museum (one per quality) and gives the
  **Artist Ribbon** once to each Pokémon. The Contest Hall's wall keeps the last six winners, newest first. The ribbons
  are no longer "waiting for Hoenn" on a Pokémon's ribbon page.
- **Ours, not Emerald's**: the contest is a screen (`AppealScreen`) and not the stage's rooms (`ContestHall*` maps
  with the judge walking); the hall behind the lobby is a room of people who only talk. Contests with friends (the link
  receptionist) wait for plan 07 and say so.
- **Pokéblocks** (`Models/Pokeblocks.cs`): the Berry Blender's `CalculatePokeblock` as written. The berries' flavours and
  smoothness are added, **each flavour is taken from the next one's** (spicy minus dry, dry minus sweet… sour minus
  spicy), every negative is counted and taken from the rest, the whole is scaled by the speed the arm reached
  (`maxRpm / 333 + 100`, in tenths), and the colour and feel are read from what is left. Two of one berry, or nothing
  left, or four negatives, make a **black** Pokéblock whose flavours are three of the five at 2. Feeding adds each
  flavour's level to the quality it names and the feel to the sheen; the nature's likes and dislikes
  (`gPokeblockFlavorCompatibilityTable`) add or take the same gain from every quality. A Pokémon whose sheen is full
  eats no more; an Egg none. The case holds forty and is a key item.
- **The blender** (`Models/BlenderGame.cs`): the arm turns at a speed that the player's presses raise and a miss
  lowers, 24 seconds at thirty frames a second; the others at the drum press by the generator they are handed, so a
  blend is the same given its berries and seed. Their berries are drawn from Cheri to Pomeg. Blending with friends
  waits for plan 07.

## The museum's gallery, the chairman's scarves and the rest of the Pokéblock people (2026-10-26, plan 21 · H11, part b)

Emerald's `LilycoveCity_LilycoveMuseum_2F`, `SlateportCity_PokemonFanClub`, `MossdeepCity_House1`,
`Route121_SafariZoneEntrance` and the painter's half of `LilycoveCity_ContestLobby`; the lines are our own.

- **A painting is looked at, not set into the wall.** Emerald changes two metatiles of the gallery's wall for each quality
  once its flag (`FLAG_COOL_PAINTING_MADE` and its fellows) is set, a picture drawn from the Pokémon it was painted of
  that is not in the repository. The frames here are read from before (the area's reads, `CoolPainting` to
  `ToughPainting` of `lilycove_city_lilycove_museum_2f.txt`): a painting that hangs (`HoennContestRecords.Museum`, which
  keeps the winner's species, name, trainer, contest, rank and points) opens in the Fashion Case's viewer (`appeals view`,
  `ScriptScreen.ContestPainting`, the style guide's "A painting"); an empty frame says whose it is, as before.
- **The museum fills as the paintings hang** (Emerald's `UpdateMuseumPatrons`, the lobby's `MuseumPatrons`): the first
  four bring a patron each (`FLAG_HIDE_LILYCOVE_MUSEUM_PATRON_1` to `_4` cleared), the fifth the tourists, once the painter
  has been agreed to; a quality's painting hung again replaces the old one and brings nobody. The gallery's curator
  (`Curator`) thanks for each painting and, once all five quality's frames are filled, gives the **Glass Ornament**
  (`FLAG_RECEIVED_GLASS_ORNAMENT`), a decoration into the secret base's PC (`openbase give`), once; with no room in the PC
  he keeps it for the player.
- **The chairman of Slateport's Fan Club** asks first for `FLAG_ENTERED_CONTEST` (the receptionist sets it with every
  contest entered; before it he only wishes to see such a Pokémon), then gives the scarf of the first quality in which the
  team's first Pokémon that is no Egg has a condition of **200 or more**, each scarf once, looked for in the order Cool, Beauty,
  Cute, Smart, Tough (Red, Blue, Pink, Green, Yellow; Emerald's `CheckLeadMonCool` and fellows: of its checks the last to run
  set the answer, and the Cool check runs last). With all five he has nothing more to give (`VAR_SLATEPORT_FAN_CLUB_STATE`
  2). A Pokémon that holds a scarf adds twenty to its quality in a contest, as before.
- **Mossdeep's black belt** tells the colour of Pokéblock the first Pokémon of the team likes (`pokeblock taste`): the
  colour of the one flavour its nature likes (spicy red, dry blue, sweet pink, bitter green, sour yellow), or that it
  neither likes nor dislikes any, for the five natures without a taste. The colour is read from the compatibility table
  the feeding already uses, so what he says is what a Pokéblock of it would do.
- **The Safari Zone's counter asks for the Pokéblock Case** (Emerald's `Route121_SafariZoneEntrance_EventScript_TryEnterSafariZone`),
  before the fee and before anything else; a player without one is sent back a step and told where the Case is given
  (the Contest Lobby's receptionist).
- **What is not here.** The stage rooms (`ContestHall`, `ContestHallBeauty`, `ContestHallCool`, `ContestHallCute`,
  `ContestHallSmart`, `ContestHallTough`): Emerald never lets the player walk in them (its player is hidden and the
  contest's graphics replace the map; they have no warps), so they are a backdrop for the contest and not a place;
  the contest here is the screen alone. **Emerald's Pokéblock feeders** are not built: the pinned data names their tile
  behaviour (`MB_POKEBLOCK_FEEDER`) and no script or text uses them, and their rules (what a Pokéblock set down does to
  the wild Pokémon round it, for how many steps) are in source (`src/`) that the importer's cache does not hold, so there is
  nothing to build them from honestly; the reader's chunks carry no such tile yet either. **The Lilycove Lady's contest**
  is the same (her rules are in source the import does not read); both are left.

## The Regis, the Sealed Chamber and Southern Island (2026-10-26, plan 21 · H12, part a)

Emerald's post-game places that need no new facility, as ours: the three Regis' riddles and caves, the sea floor under
Route 134 with the Sealed Chamber at its end, and Southern Island with the eon Pokémon that isn't roaming. The other
places the plan lists for H12 are left (the plan's status line says which).

- **The way down** is Route 134's dark water: any dive there goes to the one floor under it (Emerald's `setdivewarp` in
  Route134's `OnTransition`, 8, 6), a pool of dark water that a diver swims (`FieldMovement.Step` lets a Diving walker
  onto `DiveWater` of an underwater map; before it a floor laid as a pool of water could only be arrived on) and a
  passage at 8, 8 into the Sealed Chamber's corridor. Coming up anywhere in either comes up in Route 134 at 300, 291;
  from the one tile at the corridor's end, 12, 44 beside the dots that point up, it comes up in the chamber's pool at 10,
  19 (Emerald's `Underwater_SealedChamber_OnDive`: the overlay's `emergeAt`), and Dive from that pool goes down to it again.
- **The three braille puzzles are field moves used on a tile**, as Emerald's are (its `ShouldDoBraille…Effect` ask the
  player's position): Dig at 10, 3 before the outer room's wall, Rock Smash at 8, 21 before the Desert Ruins' wall and
  Flash at 8, 21 before the Ancient Tomb's. The overlay's `braille` names the tile, the move and the script; chosen in the
  party menu there the move can be used (the region's badge still asked: Dig none, Rock Smash the Dynamo Badge, Flash the
  Knuckle Badge) and runs that script. Rock Smash and Flash do nothing, with a line, until the Sealed Chamber's doors have
  opened; Dig works from the first.
- **The riddle** is Emerald's: with a Wailord first on the team and a Relicanth last, the back wall of the inner room
  (10, 4) shakes the cave, three doors open far off and `FLAG_REGI_DOORS_OPENED` is set (`team ends`). The six walls of
  dots that tell the story and the ten of the alphabet are read as lines of our own; the original's are braille of its
  own, which nobody here reads.
- **Regice's lap is ours.** Emerald counts steps along a path of tiles (`ShouldDoBrailleRegicePuzzle`) that its braille
  describes only as a lap; the Island Cave's is a lap of the hall's four corners one after another, from the wall, and
  back to it (`VAR_ISLAND_CAVE_LAP` 1 to 5, the overlay's triggers), forgotten as the cave is left and come to again.
- **A wall of dots is three slabs of rubble** (the `Rubble` thing of Kanto's Victory Road, hidden by the puzzle's flag) over
  the doorway, whose warp (`doors` and `openedBy`) is shut until then. The original draws a wall with a script's
  `setmetatile`; the slabs do the same with what the field already draws.
- **The Regis** are battled at level 40 (Emerald's `StartRegiBattle`), with their own battle songs, and may be run from:
  one run from is back with the next arrival, one beaten or caught is gone (`FLAG_DEFEATED_<REGI>`); each cave's arrival
  shows it while it isn't. `common.HoennChapterFourteen` hides all five of the places' people from a new game and an
  older save gets it as it comes in. No story version was raised.
- **Southern Island is by ferry from Lilycove's harbour**, and the pass is ours: Emerald gives the Eon Ticket by an event
  nobody has here, so the attendant at the ferry desk gives a Champion of Hoenn a pass the first time they speak to her
  (`FLAG_ENABLE_SHIP_SOUTHERN_ISLAND`, a flag as Scott's Frontier Pass is) and sails them to the island's beach whenever
  they ask; a sailor there sails them back. The S.S. Tidal's cabins and the other ferries (Navel Rock, Birth Island,
  Faraway Island) are not built.
- **The eon Pokémon of Southern Island is the one that doesn't roam Hoenn** (Emerald's `VAR_ROAMER_POKEMON`: 0 is Latias
  roaming and Latios here, 1 the other way), at level 50 holding a Soul Dew (`wildbattle … holding`), met by reading
  the stone on the island of the garden's pond from 13, 12 once per visit until it is beaten or caught
  (`FLAG_DEFEATED_` and `FLAG_CAUGHT_LATIAS_OR_LATIOS`). Emerald walks it down from the north over a pan of the camera;
  here it comes down at once to the tile before the stone.
- **Altering Cave stays unbuilt**: its warp on Route 103 is a plain rock tile that nobody can walk into in Emerald either
  (the reader leaves such a warp as a way no one can take), and its other wild tables are the Mystery Gift's.
