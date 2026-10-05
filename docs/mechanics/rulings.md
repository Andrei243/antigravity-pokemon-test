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

The modern column's numbers for the rules R3 to R5 added (from the weather's length down) are the newest games' as we know them: the moves' own durations were read from Pokémon Showdown's move table, but its table of conditions (binding, the chance of protecting again) was not among the files fetched in R1. R19 checks every one of them against the source before the modern rules are signed off.

Known differences that join the `Ruleset` in the session that writes their rule, never as a constant beside it:

| Rule | Platinum | Modern | Session |
| --- | --- | --- | --- |
| EXP | by the foe's level alone, shared among those who fought | scaled by the difference in level; the whole party gets some | R10 |
| Catching | Platinum's formula | critical captures, the later status bonuses | R9, R13 |
| TMs | used up | kept | R11 |
| Poison in the field | hurts every four steps, down to 1 HP | none | R13 |
| Species' types and base stats | Platinum's (Clefairy is Normal) | the newest games' (Fairy; the stat raises of Generations 6 and 7) | plan 03 · D11 (done: `Ruleset.ModernSpeciesValues`), then R19 |
| Abilities that were reworked (Sturdy holding on at 1 HP, Lightning Rod and Storm Drain taking the move in, Stench's flinch, Pickup in battle) | as in Platinum (written in R7) | as in the newest games | R27 |

### Where the engine stands against Platinum's own code

R1 found places where the engine differed from the decompilation and left them, because the default rules were not to change under the graphics session's comparison shots. R2 (2026-10-05) rewrote the battle's arithmetic from the original's functions. These are the only changes the default rules have had since R1, and each is what Platinum does:

- **Catching** is the original's: whole numbers, two whole square roots (`BattleScript_CalcCatchShakes`), four rolls against the result, and each ball's own strength (Net, Dive, Nest, Repeat, Timer, Dusk and Quick Balls work; the battle is told whether it is night, on water or in a cave and which species have been caught). A Master Ball and a ball that can't fail make no rolls.
- **EXP** is the original's (`BtlCmd_CalcExpGain`): base × level / 7, shared among the Pokémon that fought and still stand; with an Exp. Share in the party half goes to those who fought and half to the holders; a Lucky Egg and a trainer's Pokémon each add half, one after the other, rounded down each time. The bonus for a traded Pokémon joins with trading (R12, plan 07 · O5).
- **Bonuses are whole-number steps**: an ability's or an item's bonus is applied by itself, in hundredths, and rounded down before the next, in one of the formula's places (to the move's power, to the stat, before the roll, to the finished damage). A Life Orb's comes before the roll, as in the original. Since R7 every ability's bonus stands in the original's own place and order (below); the held items' are applied together at the place the original's block of items has, and R8 gives each of them its own.
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
- **A plucked or thrown berry's disliked flavour** (which confuses in the original) and Micle's accuracy wait for the berries' own session (R8), as does a Custap Berry or Quick Claw that went off this turn keeping its holder's item.
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
- **Left for the session whose rule it is**: Compound Eyes making wild Pokémon hold items more often (wild Pokémon hold nothing yet: R13); Flame Body and Magma Armor hatching eggs sooner (R15); Gluttony for the berries that aren't in the engine yet (Lansat, Starf, Micle, Custap), an Embargo failing against a Quick Claw or Custap Berry that already went off, and Giratina's Origin Forme going back without its orb (R8); the reworked abilities of later generations (R27).
- **Text**: every line is our own wording ("can't get going yet!", "finally got going!", "took the attack!", "shuddered!", "breaks the mold!").

One thing R7 found in the battle's face, not in the rules: a choice handed over while lines were still being shown copied the screen's stale state over the rules' (only a tool could do it; the game's menus open after the last line). The face now never takes the screen's state while it is behind.

Still different, each waiting for the session whose rule it is:

- A held item's bonus is applied with the others at the place the original's block of items has; each one's own place and order are R8's.
- Obedience and the Quick Claw's own line are not in the turn yet (R8, R10).
- The opponents choose "what looks best" (`TrainerAi`), with a little chance and some sense of when a condition or a screen would still do something. R9 writes Platinum's own AI.

What matches, and is held by `FormulaTests`, `BattleScenarioTests` and `BattleCoreTests`: the damage formula and its order (stat × power × (2 × level / 5 + 2) / defence / 50, a burn's halving, + 2, the critical multiplier, a Life Orb, the roll taking 0 to 15 hundredths off, then × 1.5 for the user's own type, then each of the target's types, and never less than 1 for a hit that lands), stat stages, what a critical hit ignores, and everything in the list above.

**A battle's chance is Platinum's too.** Every battle rolls on Platinum's own generator (`Battle/Sim/BattleRandom.cs`), so a seed and the choices made are the whole battle: `BattleCore.Record` is that pair and `Replay` plays it again to the same log. Our own choice, where the original has nothing to follow: the seed of a battle in the game is drawn from the game's dice as the battle begins.

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
