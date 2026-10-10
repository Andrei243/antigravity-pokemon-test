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
| EXP | by the foe's level alone (base × level / 7), shared among those who fought, half to Exp. Share holders | scaled by the two levels (Generation 7's formula), the whole team gets some, half for those who didn't fight, a fifth more past the level it would evolve at |
| Effort in a stat | at most 255 | at most 252 |
| A vitamin raises effort | up to 100 | up to the stat's limit |
| A level-100 Pokémon | gains no effort from battles | still gains effort |
| A shiny Pokémon | 1 in 8,192 | 1 in 4,096 |

The modern column's numbers for the rules R3 to R5 added (from the weather's length down) are the newest games' as we know them: the moves' own durations were read from Pokémon Showdown's move table, but its table of conditions (binding, the chance of protecting again) was not among the files fetched in R1. R19 checks every one of them against the source before the modern rules are signed off.

Known differences that join the `Ruleset` in the session that writes their rule, never as a constant beside it:

| Rule | Platinum | Modern | Session |
| --- | --- | --- | --- |
| Catching | Platinum's formula | critical captures, the later status bonuses | R9, R13 |
| TMs | used up | kept | R11 |
| Poison in the field | hurts every four steps, down to 1 HP | none | R13 |
| Species' types and base stats | Platinum's (Clefairy is Normal) | the newest games' (Fairy; the stat raises of Generations 6 and 7) | plan 03 · D11 (done: `Ruleset.ModernSpeciesValues`), then R19 |
| Abilities that were reworked (Sturdy holding on at 1 HP, Lightning Rod and Storm Drain taking the move in, Stench's flinch, Pickup in battle) | as in Platinum (written in R7) | as in the newest games | R27 |

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
- **Left for the session whose rule it is**: Compound Eyes making wild Pokémon hold items more often (wild Pokémon hold nothing yet: R13); Flame Body and Magma Armor hatching eggs sooner (R15); Gluttony for the berries that aren't in the engine yet (Lansat, Starf, Micle, Custap), an Embargo failing against a Quick Claw or Custap Berry that already went off, and Giratina's Origin Forme going back without its orb (R8); the reworked abilities of later generations (R27).
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
| Fly's map | A cursor over the town map | A list of the towns beside the map, the town ringed | Our own interface (style guide, "Fly") |

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
| Shininess | Drawn from the personality and the trainer's ID (1 in 8,192) | A draw of its own at the same odds (1 in 4,096 by the modern rules), and nothing shows it yet | Personalities don't decide everything they do in the original yet (R15, breeding, is where they matter) |
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
| Maniac Tunnel | Dug once enough kinds of Unown are seen | Not open; Solaceon's Rare Candy beyond it is held back | The Ruin Maniac's digging is the story's (plan 02 · S8) |


**The Canalave Gym** (plan 01 · M9 2a)

| What | The original | Here | Why |
| --- | --- | --- | --- |
| An empty slot | A platform's place on a floor above the ground is open in that floor's collision map whether or not the platform is there; the floor's model covers it | A slot is walked only while its platform stands there; where it has gone, the floor has a hole | Nobody walks on the air, and the picture shows the hole |
| The floors' heights | Ten tiles apart, by the walker's height | The same; the floor someone is on is the nearest to their height (`CanalaveLifts.FloorOf`) | |
| A save made on an upper floor | The platforms are laid out by the room's own script as the map loads | The same: they come back at their first ends | |

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

**Our own choices**

- The honey tree's, the radar's, the poison's and the swarm's lines are our own words.
- The day's number starts at a draw of the field's chance in a new game, where the original starts it at nothing until a record-mixing group is founded; the trainer's hidden half (the 16 bits the card doesn't show) is drawn the same way and kept with the wild Pokémon's state (`SpecialEncounters.SecretId`).
- The radar's patches stir their grass (it parts and jumps from side to side, as round someone's feet) and shed a few blades, the more for a hard shake, a sparkle over a shiny one; a honey tree with Pokémon at it sheds leaves from its crown. The original animates the grass and the tree with sprites of its own.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The pair of species a second game calls up | Slots 8 and 9 take two of the game in the DS's other slot | The table's own | No second game to read (plan 08's dual-slot stand-in) |
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
| Cynthia's Egg | After Commander Jupiter, by the cycle shop: a Togepi Egg, and she waits there until the team has room or the player takes it | Not given; her two triggers (states 3 and 4) wait | There are no eggs yet (plan 06 · R15) |
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
| Cynthia after the Psyduck | Comes up Route 210 and gives the Old Charm | Not yet | The Secret Potion and what follows are plan 02 · S9's |

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
| The Day-Care Checker | The Day Care's two Pokémon and its egg; a level with the steps walked there added (`DaycareMon_GiveExperience`) | Whatever the context's `DayCare` holds, the Pokémon's own level, and never an egg | The Day Care and breeding are plan 06 · R15; the context has no egg to read yet |
| The Pokémon History's cries | Cries by species and form | A Pokémon of the species and form, level 1, made from the field's generator the first time it is touched | The app's `Cry` takes a Pokémon and the history keeps names |
| Sounds | The Day-Care Checker's `DENSI12`; the Matchup Checker's `POKETCH_012` (a step), `013` (no match) and `014` (the best) | `poketch`; `poketch`, `poketch_beep` and `dowsing_ping` | The bank has none of them (`docs/sound-effects.md`) |
| The Move Tester's chart | A table of its own, Platinum's | The game's own chart by the rules it is played by (`TypeChart`) | The same in a Platinum game; a modern game's Pokétch tells the modern chart |
| The Matchup Checker's button | Checks on the stylus's release, nothing if it was dragged off | Checks as it is touched | No stylus to drag |

**The Pokémon apps: our own choices**

- The Move Tester's words for how well a move works are our own: IT HAS NO EFFECT, IT BARELY WORKS, NOT VERY EFFECTIVE, IT HITS NORMALLY, SUPER EFFECTIVE, EXTREMELY EFFECTIVE.
- The Day-Care Checker's mosaic is a dissolve from the paper in cells of two blocks, fewer at each of the original's ten steps.
- The fish, hearts, egg, gender signs, arrows and marks are block patterns of our own.
