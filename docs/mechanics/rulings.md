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
| A shiny Pokémon | 1 in 8,192: the personality's and the trainer's halves exclusive-or'd under 8 | 1 in 4,096: under 16 |
| An Everstone in breeding (plan 06 · R15) | the mother's or Ditto's nature one time in two | the holder's nature, always, whichever parent |
| The IVs an Egg inherits | three picks that can fall on the same stat; no item changes them | three different stats, five with a Destiny Knot, a Power item's from its holder |
| The Masuda method | four more draws for a shiny personality | five more |

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
| The footprints | Dr. Footstep's reading follows the species' footprint, which the Pokédex shows | One voice, no footprints | Plan 09 |

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
| The lift's ride | The car's lights scroll and its door opens and shuts (`PlayElevatorAnimation`, the door's animation in the gallery) | The lift's sounds and a tremor of the picture; the car stands open in its shaft | Plan 09 · L12 |
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
- **The balls on the machine** are counted by a variable of our own, `VAR_HALL_OF_FAME_BALLS`, which the room's pieces draw.

**Stand-ins for what this game lacks**

| What | The original | Here | Why |
| --- | --- | --- | --- |
| The ceremony's and the credits' songs | Their own (`SEQ_BLD_EV_DENDO2`, the ending theme) | The Champion's victory theme for the ceremony, the title's theme for the credits | Plan 05 · A6 |
| The Hall of Fame's and the League's rooms' songs | Their own | The League's theme | Plan 05 · A6 (plan 01 · M9, part 2d) |
| The machine at work | The healing machine's animation: the balls flash in turn as the jingle plays | The balls stand in their sockets and the heal's fanfare plays | Plan 09 · L12 (set pieces) |
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
| The lift's ride | The car's lights scroll (`PlayElevatorAnimation`) | The lift's sounds and a tremor of the picture, as in the Vista Lighthouse | Plan 09 · L12 |
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
| The store's lift | The car's door is a dynamic warp back to where the rider came from, the floor is chosen in one window of six, and the lights scroll | Six ways on one tile behind flags; the two lowest floors share an answer; the lift's sounds and a tremor | Plan 09 · L12 |
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
| The tremor in the mountain's heart, Regigigas shaking as it wakes | An effect of their own (`ScrCmd_29F`, `ShakeObject`) | The picture's shake and a rumble | Plan 09 · L11, L14 |
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

Platinum's trainers speak before a battle and after it, never during it; from Black and White on, leaders, rivals and bosses speak at its turning points. This game has them speak only where the `TrainerLines` option holds (On, or Rules in a game played by the modern rules), so a Platinum game shows nothing new. The rules mark the moments (`TrainerReacts`, `BattleCore.Speaks.cs`) whatever the option says; the words are ours (`Data/trainer-lines.json`) and the face's alone.

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
| The two flashes | Dark for a weaker foe, white for a stronger one, sixteen frames or so | White for everyone, a quarter of a second (plan 04 · G9's) | The first five ways were built so, and a battle on grass must open as it did |
| The species with ways of their own | Dialga, Palkia, Uxie, Azelf, Arceus; Shaymin, Giratina, Regigigas, Heatran, Darkrai, Rotom, the three Regis | The wild Pokémon's way by place, for now | Their camera is plan 09 · L14's; their rows go into `BattleMeeting.Row` before its check for two at once, as the original's do |
| The Gym Leaders, the Elite Four, the Champion | A band with their portrait | The diamonds for a Leader, the trainer's way by place for the others, for now | The bands are plan 09 · L15's |
| A double battle's four pieces | Four balls fly out from the middle to the edges, then a wipe | Four quarters of the dark slide in from the corners | Plan 09's design: the dark itself is the four pieces |
| A grunt's six shards | Six copies of the team's logo fly in to the middle, turning and shrinking to nothing, then a wipe | Six facets of our own mark fly in from the rim, each with its sixth of the dark behind it, and meet as the mark | The logo is art; decision 6 of plan 09 gives the team a mark of our own (`GalacticMark`) |
| The boss's mark | The logo fades in, stands, breaks into a mosaic as the picture is wiped | Our mark grows out of the picture's middle as the picture dims, and its dark core swallows it | The same; a mosaic is a smear, which the field's art never is |
| The water's waver | The whole picture's rows on a sine twelve or fifteen pixels deep, two or three waves down the screen | The scene effects' ripple, four or six texels deep on its own wave of twenty-four rows | One ripple in the field's composite (plan 09 · L11), not a second |
| The camera | Pulls in or back during most ways | Stands still | The field's upright cards are straightened for one lens and one distance; the ways that are a camera move are L14's |

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
- **The levels after the Hall of Fame** are Platinum's own and are now held as text (`PokemonPlatinumTests/Golden/levels.txt`): in the Battle Zone the grass and the caves hold levels 47 to 55 and the trainers 51 to 61, the Champion's team is 58 to 62 the first time and 74 to 78 after Stark Mountain. Whatever this game places of its own in Sinnoh after the Hall of Fame (the other versions' exclusives, the gifts, plan 03 · D12a) is set against those: wild Pokémon between the high forties and the middle fifties, nothing above the Champion's rematch.
