# R20c · Mega Dynamax: the proposal

Plan 06 · R20c, the design part. Drafted 2026-10-26 (wave 56), **awaiting the user's approval**. Nothing here is built: no data, no item, no engine change, no test, no sculpt, no picture. Every rule, row and brief below waits for the user's yes, or the user's change, before a later session writes it.

The user's words (2026-10-26): Gigantamax is only Dynamax with another form, but **Mega Dynamax is different: it is the Dynamax of a Mega-Evolved Pokémon**. The overview has had the session since 2026-10-12 ("Mega Dynamax, a mechanic of our own that joins Mega Evolution and Dynamax for some species", plan 00's "Work no plan owned"), and R22 left the two places it would change (`docs/mechanics/rulings.md`, "Dynamax and Gigantamax": `WhyNotDynamax` refuses a Mega, and the turn's choice refuses a Mega Evolution and a Dynamax together). No official game has it; Pokémon Showdown has no rule for it. Everything here is our own, built on the two mechanics as R20 and R22 have them (Showdown's at the pinned commit).

It answers four things: **the rules** (part 1), **which species get a Mega Dynamax form and what it looks like** (part 2), **where it is handed over and how a battle offers it** (part 3), and **the open questions** (part 4), with the table to answer at the end.

## How to read this

- **What changes, in one line**: decision 06 · 3 says "a Mega or Primal Pokémon can't Dynamax". Mega Dynamax lets a **Mega** (not a primal form) Dynamax, for a trainer who has the new key item. Everything else of decision 3 stands: one Mega Evolution and one Dynamax a trainer a battle, no Z-Move or Terastallization for the same Pokémon.
- **A form's name**: a Mega Dynamax form is named after the Mega form it grows from, with `-Mmax` after it (as a Gigantamax form has `-Gmax`): `Charizard-Mega-X-Mmax`, `Venusaur-Mega-Mmax`. Its kind is a new `FormKind.MegaDynamax` (at the enum's end). On screen it is "Mega Dynamax Charizard X". No name of the data ends in `-Mmax` (checked).
- **Recommendation**: **yes** (a form and a brief), **maybe** (a brief is written so that the user can say yes; default no), **no** (no form; no brief, one is written if the user turns the row to yes).
- **Size**: the height a Pokédex would give the form, by one rule: twelve times the Mega's height (the form's own where the data gives one, the species' otherwise), rounded, at least **20 m** and at most **60 m**, and 2 m over the species' Gigantamax form where it has one (so Mega Dynamax Charizard stands over Gigantamax Charizard's 28 m). Eternamax Eternatus's 100 m stays the largest thing in the game. In battle every giant is drawn at R22's giant scale (`BattleAnimator.DynamaxScaleFoe` 1.25, `DynamaxScaleOwn` 1.3), whatever its height. Weight: 1000 kg as the data writes for every Gigantamax form (the weight moves fail against a Dynamaxed Pokémon anyway).
- Every name, type, ability, item, move, stat, total, height and trainer's team below was checked by a script that only reads the data and this document ("How the numbers were checked", at the end).

## 1. The rules

### 1.1 Who may

- **A trainer with the Mega Max Band** (part 3), a new key item. The player has it while it is in the bag (`CoreSetup.MegaMaxBand`, set as `KeyStone` and `DynamaxBand` are set in `BattleEngine`); a trainer by `Trainer.MegaMaxBand`, a record's `megaMaxBand`. The trainer still needs the **Key Stone** to Mega Evolve at all (R20, unchanged); the Mega Max Band is what lets the Mega then Dynamax. A trainer with the band and no Key Stone can Mega Dynamax only a Pokémon that is already a Mega, which never happens (a Mega form is only ever taken in battle), so in practice both are needed.
- **A Pokémon in a Mega form** (`FormKind.Mega`), or one that is choosing to Mega Evolve this turn (1.2). Not a primal form (Kyogre, Groudon), not Ultra Necrozma, not a Terastallized Pokémon, not one holding a Z-Crystal (it couldn't Mega Evolve anyway), not Zacian, Zamazenta or Eternatus (R22's `NeverDynamax`).
- **Not a legendary's or a mythical's Mega** (default; question 4): Mega Mewtwo X and Y, Mega Latias and Latios, Mega Rayquaza, Mega Heatran, Mega Darkrai, Mega Zygarde, Mega Diancie, both Mega Magearna and Mega Zeraora. Their Megas total 700 to 780 already.
- **Any other Mega may Mega Dynamax** (default; question 3), whether or not its row below gives it a form of its own: one with a Mega Dynamax form takes it as it grows (as a Gigantamax does); one without grows in its Mega form, as a Pokémon with no Gigantamax form grows in its own. So the rows of part 2 decide **looks**, never who may.
- **A wild Pokémon never** (R20 and R22's rule).
- **Where**: wherever both a Mega Evolution and a Dynamax may happen today (decision 06 · 4: Dynamax anywhere). Nothing new is decided for the Battle Frontier: whatever its rules let of the two, they let of this.
- **Under the modern rules alone**: a new `Ruleset.MegaDynamax`, true in `Ruleset.Modern`, false in `Ruleset.Platinum`, so `battles-platinum.txt` can't move for it. Every game is modern (R19a), so this only keeps Platinum's own battles out of reach. No options row (question 11).

### 1.2 When, and what it spends

- **It spends the trainer's Mega Evolution and the trainer's Dynamax**, both once a battle (R20's `megaUsed`, R22's `dynamaxUsed`, counted per trainer as now). No third count: a trainer who Mega Dynamaxes has used both, and can neither Mega Evolve nor Dynamax another Pokémon in that battle. A trainer who has already Dynamaxed another Pokémon can't Mega Dynamax, and one who has Mega Evolved one Pokémon can Mega Dynamax only that one.
- **In one choice** (default; question 2): a move's choice may carry both flags, `BattleChoice.Mega` and `BattleChoice.Dynamax`, which `WhyNotDynamaxChoice` refuses today. The Mega Evolution happens at its moment (after the switches, Showdown's order 104), then the Dynamax at its own (order 105), the fastest first in each, and the moves still to come are put in order again as after a Mega Evolution. The player sees two lines: "X has Mega Evolved into Mega X!", then the Mega Dynamax line (1.6).
- **On a later turn**: a Pokémon that Mega Evolved on an earlier turn and is still in its Mega form may choose the Dynamax flag alone, with the band, if its trainer hasn't Dynamaxed.
- The choice still goes with a move of the Pokémon's own (no `HeldMove`, as any Dynamax) and with no Z-Move, Ultra Burst or Terastallization.

### 1.3 What it changes, and what it doesn't

Everything is R22's Dynamax, on a Mega:

| | While Mega Dynamaxed |
|---|---|
| **HP** | Its HP and its most raised by 1.5 and a twentieth for every Dynamax Level (`Pokemon.DynamaxLevel`, 0 to 10): twice at level 10, rounded down; given back by the same share, rounded up, as it ends (`getUndynamaxedHP`). The Mega's base HP is its species' (no Mega changes HP), so the number is the one a Dynamax of the same Pokémon would have. |
| **How long** | Three turns, each turn's end counting one, the third ending it ("X shrank back to its usual size!"). It also ends, with its event and no line, as it is called back or made to leave, as it faints and as the battle ends. |
| **Stats, typing, ability** | **The Mega's**, unchanged. The Mega Dynamax form copies the Mega form's base stats, types and abilities (the importer copies them, and a test holds them equal), so `ChangeForm` into it changes nothing but the look and the size. Mega Charizard Y keeps Drought, Mega Salamence Aerilate, Mega Tyranitar Sand Stream, Mega Gengar Shadow Tag. An ability that acts on entry (Intimidate, Drought) acts as the Mega Evolution happens, as R20 has it, and not again as it grows. |
| **Weight** | Not weighed: Low Kick, Grass Knot, Heavy Slam and Heat Crash fail against it (R22). |
| **Its moves** | Every move it uses becomes its Max Move once its PP is paid (`BecomeMaxMove`): a status move **Max Guard**, a damaging move **its type's Max Move** at the move's `maxPower` (the data's own, the newest games' values: 631 moves have one, from 70 to 150; Flamethrower's 95 is 130 as Max Flare, Close Combat's 120 is 95 as Max Knuckle, Fighting and Poison moves being cut down as the games cut them), of the move's category. Each Max Move does what R22 runs (table below). No new moves (question 5). |
| **What it can't** | As any Dynamax: no flinch, no Destiny Bond, not dragged out ("X is far too huge to be moved!"), no Encore, Torment, Skill Swap, Entrainment or Instruct against it, no one-hit knockout, no Choice lock or boost; Max Guard at +4 with Protect's chain. |
| **When it ends** | It shrinks back **into its Mega form**, not its species: the Mega lasts to the battle's end, as R20 has it. `DynamaxEnded` carries the Mega form's name. A Mega Dynamaxed Pokémon called back comes in again later as its Mega, and can't Dynamax again (the trainer's Dynamax is spent). |

The Max Moves it can use, by the Max Move's type, and what each does once it has hit (R22, Showdown's `self.onHit`; the data's 19 Max Moves, all run):

| Type | Max Move | Does | Type | Max Move | Does |
|---|---|---|---|---|---|
| Normal | Max Strike | each foe's Speed a stage down | Fire | Max Flare | the sun |
| Fighting | Max Knuckle | its side's Attack a stage up | Water | Max Geyser | the rain |
| Flying | Max Airstream | its side's Speed a stage up | Grass | Max Overgrowth | Grassy Terrain |
| Poison | Max Ooze | its side's Sp. Atk a stage up | Electric | Max Lightning | Electric Terrain |
| Ground | Max Quake | its side's Sp. Def a stage up | Psychic | Max Mindstorm | Psychic Terrain |
| Rock | Max Rockfall | a sandstorm | Ice | Max Hailstorm | hail (snow under the modern rules) |
| Bug | Max Flutterby | each foe's Sp. Atk a stage down | Dragon | Max Wyrmwind | each foe's Attack a stage down |
| Ghost | Max Phantasm | each foe's Defense a stage down | Dark | Max Darkness | each foe's Sp. Def a stage down |
| Steel | Max Steelspike | its side's Defense a stage up | Fairy | Max Starfall | Misty Terrain |
| status | Max Guard | protects (+4), Protect's chain | | | |

**Abilities that meet a Max Move for the first time.** R22 never had a Mega Dynamaxed, so a few Mega abilities meet Max Moves here for the first time. The build follows Showdown's rule for each, read at the pinned commit, and adds a test of each kind:

- **Parental Bond** (Mega Kangaskhan): Showdown's `parentalbond` leaves a Max Move (and a Z-Move) alone (its `onPrepareHit` returns for `move.isMax`, `move.isZ`); our `BattleCore.Abilities.cs` asks neither yet (line 151), since nobody with Parental Bond could Dynamax before. The build adds the check.
- **The -ate abilities** (Mega Pinsir's and Mega Salamence's Aerilate, Mega Gardevoir's and Mega Altaria's Pixilate, Mega Glalie's Refrigerate, Mega Feraligatr's Dragonize) on a Normal move made a Max Move: R22 listed "an -ate ability changing a Max Move's type" as left; this session settles it by Showdown's rule.
- **Abilities that read a move's flags** (Tough Claws for contact, Mega Launcher for pulse moves, Strong Jaw for biting, Iron Fist for punches, Technician for low power, Skill Link, Sheer Force, Adaptability): they read the Max Move's own flags and power as the core has them today. Nothing new, but the build's tests take one of each through a Max Move.

**Gigantamax and Mega Dynamax** (question 5). Five Mega forms belong to species with a Gigantamax form: Mega Venusaur, Mega Charizard X and Y, Mega Blastoise and Mega Gengar (and three of R20b's fifteen, Rillaboom, Cinderace and Inteleon). **Default: both stay, by what the Pokémon holds.** A Pokémon with the Gigantamax Factor that holds no Mega Stone Gigantamaxes as today (its G-Max Move, its Gigantamax look). The same Pokémon holding its stone, with a Key Stone and the Mega Max Band, Mega Dynamaxes: its Mega Dynamax look, its type's plain Max Moves, and its Gigantamax Factor unused for that battle. Without the band it can't Dynamax at all while it holds the stone (R22's rule, kept). The R22 code already decides it in that order: `GigantamaxFormOf` is never asked for a Pokémon in a Mega form.

**Z-Moves and Ultra Burst** (R21): nothing changes. A Z-Crystal's holder holds no Mega Stone, so it can't Mega Evolve; Ultra Necrozma is no Mega. A choice still carries at most one of {Mega Evolution with or without the Dynamax, Z-Move, Ultra Burst, Terastallization}.

**Terastallization** (R23): nothing changes. A Mega can't Terastallize, and a Terastallized Pokémon can't Dynamax.

### 1.4 The trainer AI

Today (`TrainerAi.WithTheLaterMechanics`) a trainer Mega Evolves whenever it can and Dynamaxes the first of its Pokémon that can, drawing nothing for either. With the Mega Max Band the second rule would Dynamax its lead before its Mega ever came out. **Default** (question 10): a trainer with the band who still has a Pokémon able to Mega Evolve (on the field or in the party, not fainted) keeps its Dynamax for that one, and Mega Dynamaxes it the first turn it can, in the same choice as its Mega Evolution; a trainer with the band and no such Pokémon left Dynamaxes as today. Still no draw, so no battle's dice move for it. Under Platinum's rules, or without the band, it chooses exactly what it chose before.

### 1.5 The log and the screen

- **No new event** (default; question 12): the Mega Evolution's line carries `MegaEvolved` as now; the Mega Dynamax's line carries R22's `Dynamaxed(Place, Form, Gigantamax: false, MaxHp, Hp)` with `Form` the `-Mmax` form (or the Mega form itself where none was made), and its end `DynamaxEnded(Place, Form, …)` with the Mega form's name. Whoever shows the battle tells a Mega Dynamax by the form's kind (`FormKind.MegaDynamax`), or by `Form` being a Mega where no form was made. Alternative: an event of its own at `BattleLog.cs`'s end (`MegaDynamaxed`), which a replay file and the golden text would write as one more kind of line.
- **The lines** (keys of `ui.json`, Romanian at the build): `battle.megaDynamaxed` "{pokemon} swelled into its Mega Dynamax form!" (for every Mega Dynamax, with a form of its own or not), `battle.cantMegaDynamax` "{pokemon} can't Mega Dynamax!", and `battleMenu.later.megaDynamax` "Mega Dynamax". The first fits the message box's two lines with the longest name (the build's `EveryEnglishBattleLineFitsTheMessageBoxsTwoLines` holds it).
- **The hooks, on R20's pattern** (each in the file the mechanic already has):
  - `Ruleset.MegaDynamax`; `CoreSetup.MegaMaxBand`; `Trainer.MegaMaxBand` and the record's `megaMaxBand`; the Vs. Recorder's file (`BattleRecordFile`'s `Opening` and `ToSetup`) carries the band as it carries the Key Stone and the Dynamax Band, or a recorded battle plays otherwise.
  - `BattleCore.Dynamax.cs`: `WhyNotDynamax` lets a Mega through where `Rules.MegaDynamax`, the band and not a legendary's or mythical's Mega (`WhyNotMegaDynamax`, one method); `WhyNotDynamaxChoice` lets a choice carry `Mega` with `Dynamax` where the Mega Evolution is allowed and the Pokémon then could Mega Dynamax; `MegaDynamaxFormOf(p)` is the `-Mmax` form of a Mega form, or null (as `GigantamaxFormOf`); the Dynamax's own moment takes it.
  - `BattleCore.Mega.cs`: nothing changes but the order check (a Mega Evolution chosen with a Dynamax happens first, which order 104 before 105 already gives).
  - `TrainerAi.WithTheLaterMechanics`: 1.4's one rule.
  - `BattleEngine.Dynamax.cs` and the menu (part 3): `MegaDynamaxArmed`, `CanMegaDynamaxNow`, and `ToggleLaterMechanic`'s new step.
  - `BattleAnimator`: the Mega Dynamax look (plan 25's, like Y15's): R22's red wreath with the Mega's four colours in it (style guide, "Mega Evolution": violet `186,96,255`, pink `255,120,200`, orange `255,176,70`, cyan `110,220,255`), where a Gigantamax adds gold. The style guide is changed first, by the session that draws it.
  - `GameEngine.BattleShapesOf` asks for the `-Mmax` form's model before the battle, as it asks for a Gigantamax form's.

## 2. Which species

### 2.1 What the data has

The data has **97 Mega forms of 87 species**: **48 of X, Y, Omega Ruby and Alpha Sapphire** (46 species, Charizard and Mewtwo two each: the "48 Megas" of the user's count) and **49 of Legends: Z-A** (Mega Raichu X and Y, Mega Absol Z, Mega Garchomp Z, Mega Lucario Z, the two Mega Meowstic, both Mega Magearna and three Mega Tatsugiri among them). Beside them stand **R20b's fifteen of our own**, still unanswered (plan 06's R20b entry): their rows here are "yes, if R20b's row is".

Five of the 97 belong to species with a Gigantamax form (Venusaur, Charizard twice, Blastoise, Gengar), and three of R20b's fifteen (Rillaboom, Cinderace, Inteleon): **both are kept** (1.3).

### 2.2 How the recommendation was made

- **No for every legendary's or mythical's Mega** (12 forms), by 1.1's rule.
- **Yes for every starter's Mega** (13 forms in the data: Venusaur, Charizard X and Y, Blastoise, Sceptile, Blaziken, Swampert, Meganium, Feraligatr, Emboar, Chesnaught, Delphox, Greninja; and R20b's fifteen), the line the user asked Megas for first.
- **Yes for every pseudo-legendary's Mega** (6: Dragonite, Tyranitar, Salamence, Metagross, Garchomp, Baxcalibur).
- **Yes for a Mega that is the ace of a Champion, an Elite Four member or a Gym Leader** of a region of the chain (17: Pidgeot, Gyarados, Gengar, Alakazam, Steelix, Scizor, Ampharos, Houndoom, Gardevoir, Aggron, Altaria, Lucario, Abomasnow, Chandelure, Excadrill, Dragalge, Glimmora), since they are who would use it (part 3).
- **Maybe for the other 18 of X, Y, Omega Ruby and Alpha Sapphire** and for nine of Legends: Z-A's that a known trainer uses (Starmie, Victreebel, Raichu X and Y, Skarmory, Froslass, Scolipede, Golisopod, Pyroar).
- **No for the rest of Legends: Z-A's** (22), among them the second Megas of a species already chosen (Absol Z, Garchomp Z, Lucario Z), the Eternal Floette's (one Pokémon in the world), the two Meowstic's and Tatsugiri's three.

**In all: 36 yes, 27 maybe, 34 no** of the data's 97, and R20b's 15 yes if R20b is: **51 forms at the default**, each a sculpt (the user's, who is reworking the sculpts; until a sculpt exists a Mega Dynamax shows its Mega's model grown, as a form with no model of its own shows its species').

### 2.3 The table of the 97

| # | Mega form | From | Typing | Ability | Total | Recommendation | Mega Dynamax form | Height |
|---|---|---|---|---|---|---|---|---|
| 1 | Venusaur-Mega | X, Y, ORAS | Grass/Poison | Thick Fat | 625 | **yes** | `Venusaur-Mega-Mmax` | 29 m |
| 2 | Charizard-Mega-X | X, Y, ORAS | Fire/Dragon | Tough Claws | 634 | **yes** | `Charizard-Mega-X-Mmax` | 30 m |
| 3 | Charizard-Mega-Y | X, Y, ORAS | Fire/Flying | Drought | 634 | **yes** | `Charizard-Mega-Y-Mmax` | 30 m |
| 4 | Blastoise-Mega | X, Y, ORAS | Water | Mega Launcher | 630 | **yes** | `Blastoise-Mega-Mmax` | 27 m |
| 5 | Beedrill-Mega | X, Y, ORAS | Bug/Poison | Adaptability | 495 | **maybe** | `Beedrill-Mega-Mmax` | 20 m |
| 6 | Pidgeot-Mega | X, Y, ORAS | Normal/Flying | No Guard | 579 | **yes** | `Pidgeot-Mega-Mmax` | 26 m |
| 7 | Raichu-Mega-X | Legends: Z-A | Electric | Electric Surge | 585 | **maybe** | `Raichu-Mega-X-Mmax` | 20 m |
| 8 | Raichu-Mega-Y | Legends: Z-A | Electric | No Guard | 585 | **maybe** | `Raichu-Mega-Y-Mmax` | 20 m |
| 9 | Clefable-Mega | Legends: Z-A | Fairy/Flying | Magic Bounce | 583 | **no** | – | – |
| 10 | Alakazam-Mega | X, Y, ORAS | Psychic | Trace | 600 | **yes** | `Alakazam-Mega-Mmax` | 20 m |
| 11 | Victreebel-Mega | Legends: Z-A | Grass/Poison | Innards Out | 590 | **maybe** | `Victreebel-Mega-Mmax` | 54 m |
| 12 | Slowbro-Mega | X, Y, ORAS | Water/Psychic | Shell Armor | 590 | **maybe** | `Slowbro-Mega-Mmax` | 24 m |
| 13 | Gengar-Mega | X, Y, ORAS | Ghost/Poison | Shadow Tag | 600 | **yes** | `Gengar-Mega-Mmax` | 22 m |
| 14 | Kangaskhan-Mega | X, Y, ORAS | Normal | Parental Bond | 590 | **maybe** | `Kangaskhan-Mega-Mmax` | 26 m |
| 15 | Starmie-Mega | Legends: Z-A | Water/Psychic | Huge Power | 620 | **maybe** | `Starmie-Mega-Mmax` | 28 m |
| 16 | Pinsir-Mega | X, Y, ORAS | Bug/Flying | Aerilate | 600 | **maybe** | `Pinsir-Mega-Mmax` | 20 m |
| 17 | Gyarados-Mega | X, Y, ORAS | Water/Dark | Mold Breaker | 640 | **yes** | `Gyarados-Mega-Mmax` | 60 m |
| 18 | Aerodactyl-Mega | X, Y, ORAS | Rock/Flying | Tough Claws | 615 | **maybe** | `Aerodactyl-Mega-Mmax` | 25 m |
| 19 | Dragonite-Mega | Legends: Z-A | Dragon/Flying | Multiscale | 700 | **yes** | `Dragonite-Mega-Mmax` | 26 m |
| 20 | Mewtwo-Mega-X | X, Y, ORAS | Psychic/Fighting | Steadfast | 780 | **no** (legendary or mythical) | – | – |
| 21 | Mewtwo-Mega-Y | X, Y, ORAS | Psychic | Insomnia | 780 | **no** (legendary or mythical) | – | – |
| 22 | Meganium-Mega | Legends: Z-A | Grass/Fairy | Mega Sol | 625 | **yes** | `Meganium-Mega-Mmax` | 29 m |
| 23 | Feraligatr-Mega | Legends: Z-A | Water/Dragon | Dragonize | 630 | **yes** | `Feraligatr-Mega-Mmax` | 28 m |
| 24 | Ampharos-Mega | X, Y, ORAS | Electric/Dragon | Mold Breaker | 610 | **yes** | `Ampharos-Mega-Mmax` | 20 m |
| 25 | Steelix-Mega | X, Y, ORAS | Steel/Ground | Sand Force | 610 | **yes** | `Steelix-Mega-Mmax` | 60 m |
| 26 | Scizor-Mega | X, Y, ORAS | Bug/Steel | Technician | 600 | **yes** | `Scizor-Mega-Mmax` | 24 m |
| 27 | Heracross-Mega | X, Y, ORAS | Bug/Fighting | Skill Link | 600 | **maybe** | `Heracross-Mega-Mmax` | 20 m |
| 28 | Skarmory-Mega | Legends: Z-A | Steel/Flying | Stalwart | 565 | **maybe** | `Skarmory-Mega-Mmax` | 20 m |
| 29 | Houndoom-Mega | X, Y, ORAS | Dark/Fire | Solar Power | 600 | **yes** | `Houndoom-Mega-Mmax` | 23 m |
| 30 | Tyranitar-Mega | X, Y, ORAS | Rock/Dark | Sand Stream | 700 | **yes** | `Tyranitar-Mega-Mmax` | 30 m |
| 31 | Sceptile-Mega | X, Y, ORAS | Grass/Dragon | Lightning Rod | 630 | **yes** | `Sceptile-Mega-Mmax` | 23 m |
| 32 | Blaziken-Mega | X, Y, ORAS | Fire/Fighting | Speed Boost | 630 | **yes** | `Blaziken-Mega-Mmax` | 23 m |
| 33 | Swampert-Mega | X, Y, ORAS | Water/Ground | Swift Swim | 635 | **yes** | `Swampert-Mega-Mmax` | 23 m |
| 34 | Gardevoir-Mega | X, Y, ORAS | Psychic | Pixilate | 618 | **yes** | `Gardevoir-Mega-Mmax` | 20 m |
| 35 | Sableye-Mega | X, Y, ORAS | Dark/Ghost | Magic Bounce | 480 | **maybe** | `Sableye-Mega-Mmax` | 20 m |
| 36 | Mawile-Mega | X, Y, ORAS | Steel | Huge Power | 480 | **maybe** | `Mawile-Mega-Mmax` | 20 m |
| 37 | Aggron-Mega | X, Y, ORAS | Steel | Filter | 630 | **yes** | `Aggron-Mega-Mmax` | 26 m |
| 38 | Medicham-Mega | X, Y, ORAS | Fighting/Psychic | Pure Power | 510 | **maybe** | `Medicham-Mega-Mmax` | 20 m |
| 39 | Manectric-Mega | X, Y, ORAS | Electric | Intimidate | 575 | **maybe** | `Manectric-Mega-Mmax` | 22 m |
| 40 | Sharpedo-Mega | X, Y, ORAS | Water/Dark | Strong Jaw | 560 | **maybe** | `Sharpedo-Mega-Mmax` | 30 m |
| 41 | Camerupt-Mega | X, Y, ORAS | Fire/Ground | Sheer Force | 560 | **maybe** | `Camerupt-Mega-Mmax` | 30 m |
| 42 | Altaria-Mega | X, Y, ORAS | Dragon/Fairy | Pixilate | 590 | **yes** | `Altaria-Mega-Mmax` | 20 m |
| 43 | Banette-Mega | X, Y, ORAS | Ghost | Prankster | 555 | **maybe** | `Banette-Mega-Mmax` | 20 m |
| 44 | Chimecho-Mega | Legends: Z-A | Psychic/Steel | Levitate | 555 | **no** | – | – |
| 45 | Absol-Mega | X, Y, ORAS | Dark | Magic Bounce | 565 | **maybe** | `Absol-Mega-Mmax` | 20 m |
| 46 | Absol-Mega-Z | Legends: Z-A | Dark/Ghost | Sharpness | 565 | **no** | – | – |
| 47 | Glalie-Mega | X, Y, ORAS | Ice | Refrigerate | 580 | **maybe** | `Glalie-Mega-Mmax` | 25 m |
| 48 | Salamence-Mega | X, Y, ORAS | Dragon/Flying | Aerilate | 700 | **yes** | `Salamence-Mega-Mmax` | 22 m |
| 49 | Metagross-Mega | X, Y, ORAS | Steel/Psychic | Tough Claws | 700 | **yes** | `Metagross-Mega-Mmax` | 30 m |
| 50 | Latias-Mega | X, Y, ORAS | Dragon/Psychic | Levitate | 700 | **no** (legendary or mythical) | – | – |
| 51 | Latios-Mega | X, Y, ORAS | Dragon/Psychic | Levitate | 700 | **no** (legendary or mythical) | – | – |
| 52 | Rayquaza-Mega | X, Y, ORAS | Dragon/Flying | Delta Stream | 780 | **no** (legendary or mythical) | – | – |
| 53 | Staraptor-Mega | Legends: Z-A | Fighting/Flying | Contrary | 585 | **no** | – | – |
| 54 | Lopunny-Mega | X, Y, ORAS | Normal/Fighting | Scrappy | 580 | **maybe** | `Lopunny-Mega-Mmax` | 20 m |
| 55 | Garchomp-Mega | X, Y, ORAS | Dragon/Ground | Sand Force | 700 | **yes** | `Garchomp-Mega-Mmax` | 23 m |
| 56 | Garchomp-Mega-Z | Legends: Z-A | Dragon | Levitate | 700 | **no** | – | – |
| 57 | Lucario-Mega | X, Y, ORAS | Fighting/Steel | Adaptability | 625 | **yes** | `Lucario-Mega-Mmax` | 20 m |
| 58 | Lucario-Mega-Z | Legends: Z-A | Fighting/Steel | Aura Guard | 625 | **no** | – | – |
| 59 | Abomasnow-Mega | X, Y, ORAS | Grass/Ice | Snow Warning | 594 | **yes** | `Abomasnow-Mega-Mmax` | 32 m |
| 60 | Gallade-Mega | X, Y, ORAS | Psychic/Fighting | Inner Focus | 618 | **maybe** | `Gallade-Mega-Mmax` | 20 m |
| 61 | Froslass-Mega | Legends: Z-A | Ice/Ghost | Snow Warning | 580 | **maybe** | `Froslass-Mega-Mmax` | 31 m |
| 62 | Heatran-Mega | Legends: Z-A | Fire/Steel | Flash Fire | 700 | **no** (legendary or mythical) | – | – |
| 63 | Darkrai-Mega | Legends: Z-A | Dark | Bad Dreams | 700 | **no** (legendary or mythical) | – | – |
| 64 | Emboar-Mega | Legends: Z-A | Fire/Fighting | Mold Breaker | 628 | **yes** | `Emboar-Mega-Mmax` | 22 m |
| 65 | Excadrill-Mega | Legends: Z-A | Ground/Steel | Piercing Drill | 608 | **yes** | `Excadrill-Mega-Mmax` | 20 m |
| 66 | Audino-Mega | X, Y, ORAS | Normal/Fairy | Healer | 545 | **maybe** | `Audino-Mega-Mmax` | 20 m |
| 67 | Scolipede-Mega | Legends: Z-A | Bug/Poison | Shell Armor | 585 | **maybe** | `Scolipede-Mega-Mmax` | 38 m |
| 68 | Scrafty-Mega | Legends: Z-A | Dark/Fighting | Intimidate | 588 | **no** | – | – |
| 69 | Eelektross-Mega | Legends: Z-A | Electric | Eelevate | 615 | **no** | – | – |
| 70 | Chandelure-Mega | Legends: Z-A | Ghost/Fire | Infiltrator | 620 | **yes** | `Chandelure-Mega-Mmax` | 30 m |
| 71 | Golurk-Mega | Legends: Z-A | Ground/Ghost | Unseen Fist | 583 | **no** | – | – |
| 72 | Chesnaught-Mega | Legends: Z-A | Grass/Fighting | Bulletproof | 630 | **yes** | `Chesnaught-Mega-Mmax` | 20 m |
| 73 | Delphox-Mega | Legends: Z-A | Fire/Psychic | Levitate | 634 | **yes** | `Delphox-Mega-Mmax` | 20 m |
| 74 | Greninja-Mega | Legends: Z-A | Water/Dark | Protean | 630 | **yes** | `Greninja-Mega-Mmax` | 20 m |
| 75 | Pyroar-Mega | Legends: Z-A | Fire/Normal | Fire Mane | 607 | **maybe** | `Pyroar-Mega-Mmax` | 20 m |
| 76 | Floette-Mega | Legends: Z-A | Fairy | Fairy Aura | 651 | **no** | – | – |
| 77 | Meowstic-Male-Mega | Legends: Z-A | Psychic | Trace | 566 | **no** | – | – |
| 78 | Meowstic-Female-Mega | Legends: Z-A | Psychic | Trace | 566 | **no** | – | – |
| 79 | Malamar-Mega | Legends: Z-A | Dark/Psychic | Contrary | 582 | **no** | – | – |
| 80 | Barbaracle-Mega | Legends: Z-A | Rock/Fighting | Tough Claws | 600 | **no** | – | – |
| 81 | Dragalge-Mega | Legends: Z-A | Poison/Dragon | Regenerator | 594 | **yes** | `Dragalge-Mega-Mmax` | 25 m |
| 82 | Hawlucha-Mega | Legends: Z-A | Fighting/Flying | No Guard | 600 | **no** | – | – |
| 83 | Zygarde-Mega | Legends: Z-A | Dragon/Ground | Aura Break | 778 | **no** (legendary or mythical) | – | – |
| 84 | Diancie-Mega | X, Y, ORAS | Rock/Fairy | Magic Bounce | 700 | **no** (legendary or mythical) | – | – |
| 85 | Crabominable-Mega | Legends: Z-A | Fighting/Ice | Iron Fist | 578 | **no** | – | – |
| 86 | Golisopod-Mega | Legends: Z-A | Bug/Steel | Tough Claws | 630 | **maybe** | `Golisopod-Mega-Mmax` | 28 m |
| 87 | Drampa-Mega | Legends: Z-A | Normal/Dragon | Berserk | 585 | **no** | – | – |
| 88 | Magearna-Mega | Legends: Z-A | Steel/Fairy | Soul-Heart | 700 | **no** (legendary or mythical) | – | – |
| 89 | Magearna-Original-Mega | Legends: Z-A | Steel/Fairy | Soul-Heart | 700 | **no** (legendary or mythical) | – | – |
| 90 | Zeraora-Mega | Legends: Z-A | Electric | Volt Absorb | 700 | **no** (legendary or mythical) | – | – |
| 91 | Falinks-Mega | Legends: Z-A | Fighting | Defiant | 570 | **no** | – | – |
| 92 | Scovillain-Mega | Legends: Z-A | Grass/Fire | Spicy Spray | 586 | **no** | – | – |
| 93 | Glimmora-Mega | Legends: Z-A | Rock/Poison | Adaptability | 625 | **yes** | `Glimmora-Mega-Mmax` | 34 m |
| 94 | Tatsugiri-Curly-Mega | Legends: Z-A | Dragon/Water | Commander | 575 | **no** | – | – |
| 95 | Tatsugiri-Droopy-Mega | Legends: Z-A | Dragon/Water | Commander | 575 | **no** | – | – |
| 96 | Tatsugiri-Stretchy-Mega | Legends: Z-A | Dragon/Water | Commander | 575 | **no** | – | – |
| 97 | Baxcalibur-Mega | Legends: Z-A | Dragon/Ice | Thermal Exchange | 700 | **yes** | `Baxcalibur-Mega-Mmax` | 25 m |

The ability is the Mega form's first in the data (the species' first where the form names none). The typing, ability and total are the Mega's, which the Mega Dynamax form keeps (1.3).

### 2.4 R20b's fifteen

**Yes, if R20b's row is approved** (each with R20b's typing, ability and stats as the user answers them; a row R20b turns down has no Mega Dynamax either):

| # | Mega of our own | Mega Dynamax form | Height | Gigantamax kept |
|---|---|---|---|---|
| 1 | Mega Typhlosion | `Typhlosion-Mega-Mmax` | 20 m | – |
| 2 | Mega Torterra | `Torterra-Mega-Mmax` | 26 m | – |
| 3 | Mega Infernape | `Infernape-Mega-Mmax` | 20 m | – |
| 4 | Mega Empoleon | `Empoleon-Mega-Mmax` | 20 m | – |
| 5 | Mega Serperior | `Serperior-Mega-Mmax` | 40 m | – |
| 6 | Mega Samurott | `Samurott-Mega-Mmax` | 20 m | – |
| 7 | Mega Decidueye | `Decidueye-Mega-Mmax` | 20 m | – |
| 8 | Mega Incineroar | `Incineroar-Mega-Mmax` | 22 m | – |
| 9 | Mega Primarina | `Primarina-Mega-Mmax` | 22 m | – |
| 10 | Mega Rillaboom | `Rillaboom-Mega-Mmax` | 30 m | Rillaboom-Gmax (28 m) |
| 11 | Mega Cinderace | `Cinderace-Mega-Mmax` | 29 m | Cinderace-Gmax (27 m) |
| 12 | Mega Inteleon | `Inteleon-Mega-Mmax` | 42 m | Inteleon-Gmax (40 m) |
| 13 | Mega Meowscarada | `Meowscarada-Mega-Mmax` | 20 m | – |
| 14 | Mega Skeledirge | `Skeledirge-Mega-Mmax` | 20 m | – |
| 15 | Mega Quaquaval | `Quaquaval-Mega-Mmax` | 22 m | – |

Heights by the same rule, from the species' height (R20b changes none).

### 2.5 The look every Mega Dynamax shares

Gigantamax forms each look different, and only their red clouds say what they are. A Mega Dynamax should say both of its halves at a glance, so every one shares three things (our own, on the style guide's colours), and then has **one new feature** of its own:

1. **The Mega's parts made of light.** Whatever its Mega Evolution added to the species (Charizard X's blue flames, Mega Gardevoir's gown, Mega Steelix's crystals) is grown and turned to faceted crystal lit from inside, in the Mega Evolution's four colours: violet, pink, orange and cyan. The rest of the body keeps the Mega's colours, a little deeper.
2. **The Mega mark of our own** (the style guide's: a ring round a four-pointed star) set on the body as a glowing crystal medallion, on the chest or the brow, where a Gigantamax has nothing.
3. **Dynamax clouds round its feet**: R22's dark red storm clouds (`232,40,84` and the cloud's dark red), with violet and cyan light inside them where a Gigantamax's have gold.

**The briefs are written to be image prompts.** Each "Look" line is self-contained (the size, the base Mega, the silhouette, the colours, the new feature and the shared look), and the headings and bullets keep the shape `tools/GeminiArt` reads (`### N. Name: …`, `- **Typing**:`, `- **Look**:`), so once its list of files names this document (one line, `("megadynamax", "R20c-mega-dynamax-proposals.md")`, for the user or the build session to add) `dotnet run --project tools/GeminiArt -- generate --set megadynamax` draws them into `docs/art/reference/gemini/megadynamax/`. Gemini has never seen our sculpts, so each Look describes the Mega in plain words; where the picture and the written brief differ, the brief wins (the folder's own rule). Briefs are written for every **yes** and **maybe**; a **no** turned to yes gets one at the build.

### 2.6 The briefs: the data's Megas

In the table's order. Heights from 2.3.

### 1. Mega Dynamax Venusaur: `Venusaur-Mega-Mmax`, about 29 m (yes)

- **Typing**: Grass/Poison (its Mega's).
- **Look**: A titan about 29 m tall, grown from Mega Venusaur, a broad four-legged blue-green reptile with a huge pink flower and a palm of leaves on its back. Silhouette: low and wide like a hill, the flower on its back opened into a lotus as wide as the beast is long, its petals turned to faceted pink and violet crystal, its leaves fanned out flat around it like a skirt. Colours: deep teal hide, pink petals glowing from inside, yellow pollen dust. New feature: a ring of tall crystal stamens standing up from the flower's heart like spires, each tipped with orange light. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its brow, and dark red storm clouds with violet and cyan light curling round its feet.

### 2. Mega Dynamax Charizard X: `Charizard-Mega-X-Mmax`, about 30 m (yes)

- **Typing**: Fire/Dragon (its Mega's).
- **Look**: A titan about 30 m tall, grown from Mega Charizard X, a black-scaled winged dragon with blue flames pouring from the corners of its mouth, blue wing membranes and a blue flame on its tail. Silhouette: upright, wings spread wide and high like black sails, the shoulders broader. Colours: charcoal body, electric blue and cyan flames, violet glints. New feature: its blue mouth-flames rise as two long horns of blue crystal fire sweeping back over its head into a three-pronged crown. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light curling round its feet.

### 3. Mega Dynamax Charizard Y: `Charizard-Mega-Y-Mmax`, about 30 m (yes)

- **Typing**: Fire/Flying (its Mega's).
- **Look**: A titan about 30 m tall, grown from Mega Charizard Y, an orange winged dragon with large jagged wings, small extra wings at its wrists and a long horn crest swept back from its head. Silhouette: wings raised in a great V, the crest longer. Colours: bright orange body, cream belly, teal wing membranes edged in orange crystal. New feature: a small sun burning inside a cage of crystal ribs at its chest, white-gold at the heart, throwing heat haze. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its brow, and dark red storm clouds with violet and cyan light curling round its feet.

### 4. Mega Dynamax Blastoise: `Blastoise-Mega-Mmax`, about 27 m (yes)

- **Typing**: Water (its Mega's).
- **Look**: A titan about 27 m tall, grown from Mega Blastoise, a blue upright turtle with a brown shell, one great cannon on top of its shell and a smaller cannon on each forearm. Silhouette: a walking fortress, the shell domed higher. Colours: blue skin, brown and cream shell, cannon barrels turned to cyan crystal. New feature: the back cannon has become a turret of three crystal barrels on a turning ring, water spiralling inside them. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light curling round its feet.

### 5. Mega Dynamax Beedrill: `Beedrill-Mega-Mmax`, about 20 m (maybe)

- **Typing**: Bug/Poison (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Beedrill, a lean yellow and black wasp with two long lance-like forearm stingers, a third stinger at the end of a long abdomen and four narrow wings. Silhouette: hovering, needles forward. Colours: yellow and black bands, the stingers turned to violet crystal tipped with toxic pink light, wings like stained glass. New feature: a halo of a dozen small crystal needles hovering behind it, pointed forward like a volley about to fly. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its thorax, and dark red storm clouds with violet and cyan light curling beneath it.

### 6. Mega Dynamax Pidgeot: `Pidgeot-Mega-Mmax`, about 26 m (yes)

- **Typing**: Normal/Flying (its Mega's).
- **Look**: A titan about 26 m tall, grown from Mega Pidgeot, a large brown bird with a cream breast, blue-tipped wings and very long head plumes in red and yellow. Silhouette: wings spread from horizon to horizon, the plumes streaming far behind. Colours: warm brown, cream, the plumes turned to ribbons of crystal shading from orange to pink. New feature: its tail spread into a fan of long crystal quills like a comet's tail. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its breast, and dark red storm clouds with violet and cyan light swirling under it.

### 7. Mega Dynamax Raichu X: `Raichu-Mega-X-Mmax`, about 20 m (maybe)

- **Typing**: Electric (its Mega's).
- **Look**: A titan about 20 m tall, grown from our Mega Raichu X, an orange mouse with long ears, cream belly, a thin tail ending in a lightning-bolt tip and a wild crest of electricity. Silhouette: standing on its hind legs, ears up, tail arched high over its back. Colours: orange fur, cream belly, ear tips and tail bolt turned to yellow and cyan crystal crackling with sparks. New feature: its tail bolt has become a giant lightning-shaped crystal blade it holds over its head. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its belly, and dark red storm clouds with violet and cyan light curling round its feet.

### 8. Mega Dynamax Raichu Y: `Raichu-Mega-Y-Mmax`, about 20 m (maybe)

- **Typing**: Electric (its Mega's).
- **Look**: A titan about 20 m tall, grown from our Mega Raichu Y, a sleek orange mouse with long ears and a long thin tail, faster-looking and leaner than Raichu X. Silhouette: crouched to spring, ears swept back. Colours: orange fur, cream belly, yellow cheek pouches turned to glowing crystal. New feature: a ring of floating crystal orbs round its body, each holding a ball of lightning, linked by thin arcs. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light curling round its feet.

### 9. Mega Dynamax Alakazam: `Alakazam-Mega-Mmax`, about 20 m (yes)

- **Typing**: Psychic (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Alakazam, a gaunt yellow humanoid fox with a very long white beard, a red gem on its brow and five spoons floating around it. Silhouette: floating seated in the air, legs crossed, arms open. Colours: golden yellow, brown armour-like plates, white beard. New feature: the five spoons have become five tall crystal blades turning slowly round it in a wheel, each a different one of violet, pink, orange, cyan and white. Shared look: the Mega mark (a ring round a four-pointed star) glowing as a crystal medallion in place of the red gem on its brow, and dark red storm clouds with violet and cyan light swirling beneath it.

### 10. Mega Dynamax Victreebel: `Victreebel-Mega-Mmax`, about 54 m (maybe)

- **Typing**: Grass/Poison (its Mega's).
- **Look**: A titan about 54 m tall, grown from our Mega Victreebel, a huge yellow pitcher-plant with a wide mouth, a long curled vine on its head and two leaves for arms. Silhouette: a towering bell rising out of the clouds, its vine curling high above. Colours: yellow-green pitcher with brown spots, leaves deep green, the inside of its mouth glowing acid pink. New feature: a ring of smaller crystal pitcher-plants sprouting round its rim like a crown, each dripping glowing nectar. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its front, and dark red storm clouds with violet and cyan light round its base.

### 11. Mega Dynamax Slowbro: `Slowbro-Mega-Mmax`, about 24 m (maybe)

- **Typing**: Water/Psychic (its Mega's).
- **Look**: A titan about 24 m tall, grown from Mega Slowbro, a pink sleepy upright creature almost swallowed by the huge grey spiral shell clamped on its tail. Silhouette: the spiral shell towers like a lighthouse, the pink body a small calm figure inside it. Colours: soft pink skin, grey and violet shell. New feature: the shell's spiral turned to violet crystal with cyan light running round its whorls like a staircase. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its forehead, and dark red storm clouds with violet and cyan light round its feet.

### 12. Mega Dynamax Gengar: `Gengar-Mega-Mmax`, about 22 m (yes)

- **Typing**: Ghost/Poison (its Mega's).
- **Look**: A titan about 22 m tall, grown from Mega Gengar, a round purple ghost with a wide grin, spiky back, a third eye on its forehead and its legs sunk into a pool of shadow. Silhouette: a dark dome rising from a shadow pool, arms wide. Colours: deep purple, red eyes, the third eye a huge cyan-violet crystal. New feature: six long shadow hands rising from the pool round it, fingers tipped with violet crystal claws. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its belly, and dark red storm clouds with violet and cyan light boiling out of its shadow.

### 13. Mega Dynamax Kangaskhan: `Kangaskhan-Mega-Mmax`, about 26 m (maybe)

- **Typing**: Normal (its Mega's).
- **Look**: A titan about 26 m tall, grown from Mega Kangaskhan, a brown armoured mother with a belly pouch, whose young has climbed out to fight beside her on the ground. Silhouette: the mother standing tall, the young at her feet in a boxer's stance. Colours: brown hide, cream belly, the young's markings turned to pink crystal. New feature: the young wears glowing crystal boxing gloves of orange and pink light. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on the mother's brow, and dark red storm clouds with violet and cyan light round their feet.

### 14. Mega Dynamax Starmie: `Starmie-Mega-Mmax`, about 28 m (maybe)

- **Typing**: Water/Psychic (its Mega's).
- **Look**: A titan about 28 m tall, grown from our Mega Starmie, a violet double starfish with a red gem core at its centre. Silhouette: a great spinning star standing on one point. Colours: violet body, gold edging, the core turned to a ruby-and-cyan crystal. New feature: a second, larger star of crystal light turning behind it like a halo, its points lit in the four colours. Shared look: the Mega mark (a ring round a four-pointed star) inside its core, and dark red storm clouds with violet and cyan light round its lowest point.

### 15. Mega Dynamax Pinsir: `Pinsir-Mega-Mmax`, about 20 m (maybe)

- **Typing**: Bug/Flying (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Pinsir, a brown stag-beetle-like bug with huge horned pincers on its head, spikier with orange wings. Silhouette: hovering, pincers wide open. Colours: brown shell, orange translucent wings. New feature: the pincers lined along their inner edges with rows of violet crystal teeth that glow brighter toward the tips. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light swirling beneath it.

### 16. Mega Dynamax Gyarados: `Gyarados-Mega-Mmax`, about 60 m (yes)

- **Typing**: Water/Dark (its Mega's).
- **Look**: A titan about 60 m tall, grown from Mega Gyarados, a long blue sea serpent with a fierce open mouth, black-edged fins and a crest of red spines. Silhouette: coils rising out of a whirlpool, head reared high and roaring. Colours: deep blue, cream belly, black and red fins. New feature: a row of crystal sail-fins along its whole spine, glowing violet at the base and cyan at the tips, like a storm front on its back. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its brow, and dark red storm clouds with violet and cyan light spinning in the whirlpool round it.

### 17. Mega Dynamax Aerodactyl: `Aerodactyl-Mega-Mmax`, about 25 m (maybe)

- **Typing**: Rock/Flying (its Mega's).
- **Look**: A titan about 25 m tall, grown from Mega Aerodactyl, a grey-violet pterosaur with jagged rock spikes along its body and wings, a fanged jaw. Silhouette: wings flung wide, jaws open. Colours: grey-lavender hide, the rock spikes turned to amber crystal. New feature: amber crystal shards bursting from its back like an erupting fossil bed. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light swirling under it.

### 18. Mega Dynamax Dragonite: `Dragonite-Mega-Mmax`, about 26 m (yes)

- **Typing**: Dragon/Flying (its Mega's).
- **Look**: A titan about 26 m tall, grown from our Mega Dragonite, a big friendly orange dragon with a cream belly, small teal wings and two thin antennae. Silhouette: round and kindly but colossal, arms open as if to catch the sky. Colours: warm orange, cream, teal wing membranes turned to cyan crystal. New feature: its antennae have grown into two long ribbons of light trailing behind it like a sea dragon's streamers. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its belly, and dark red storm clouds with violet and cyan light swirling round it.

### 19. Mega Dynamax Meganium: `Meganium-Mega-Mmax`, about 29 m (yes)

- **Typing**: Grass/Fairy (its Mega's).
- **Look**: A titan about 29 m tall, grown from our Mega Meganium, a long-necked pale green dinosaur with a ring of pink-and-yellow petals round its neck and two antennae. Silhouette: neck raised high like a brachiosaur, petals spread. Colours: pale green, pink and yellow petals turned to glowing crystal. New feature: the petal ring has opened into a great sunflower corona behind its head, a disc of gold light. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 20. Mega Dynamax Feraligatr: `Feraligatr-Mega-Mmax`, about 28 m (yes)

- **Typing**: Water/Dragon (its Mega's).
- **Look**: A titan about 28 m tall, grown from our Mega Feraligatr, a blue upright crocodile with a huge jaw, red spikes down its back and a yellow belly. Silhouette: upright, jaws gaping, tail curling forward. Colours: deep blue, the red back spikes turned to violet and orange crystal like a dragon's fins. New feature: two long dragon horns of cyan crystal sweeping back from its jaw. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 21. Mega Dynamax Ampharos: `Ampharos-Mega-Mmax`, about 20 m (yes)

- **Typing**: Electric/Dragon (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Ampharos, a yellow upright sheep-dragon with a long flowing white mane and a glowing red orb at its tail's end. Silhouette: standing tall, mane streaming like a cloak. Colours: yellow, white mane shot with crystal threads, the orb on the brow and tail glowing. New feature: its tail orb has become a crystal lighthouse lantern, sweeping a beam of light. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 22. Mega Dynamax Steelix: `Steelix-Mega-Mmax`, about 60 m (yes)

- **Typing**: Steel/Ground (its Mega's).
- **Look**: A titan about 60 m tall, grown from Mega Steelix, a vast grey iron snake of joined boulders with crystal growths on its head and body. Silhouette: rearing up in a spiral like a tower, its head a great iron jaw. Colours: dark iron grey, the crystal growths turned to violet and cyan prisms glowing from inside. New feature: a crown of six tall crystal pillars round its head like a stone circle. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its brow, and dark red storm clouds with violet and cyan light round its coils.

### 23. Mega Dynamax Scizor: `Scizor-Mega-Mmax`, about 24 m (yes)

- **Typing**: Bug/Steel (its Mega's).
- **Look**: A titan about 24 m tall, grown from Mega Scizor, a red armoured mantis-like warrior with huge pincer claws marked like eyes, black stripes and small wings. Silhouette: upright, claws raised. Colours: glossy crimson, black. New feature: four large translucent wings spread behind it like stained-glass windows of violet and orange crystal. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 24. Mega Dynamax Heracross: `Heracross-Mega-Mmax`, about 20 m (maybe)

- **Typing**: Bug/Fighting (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Heracross, a blue beetle warrior with a huge forked horn and heavy arms. Silhouette: horn raised like a battering ram. Colours: deep blue shell, the horn turned to violet crystal with orange edges. New feature: the forked horn has split into a broad crown of crystal points. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 25. Mega Dynamax Skarmory: `Skarmory-Mega-Mmax`, about 20 m (maybe)

- **Typing**: Steel/Flying (its Mega's).
- **Look**: A titan about 20 m tall, grown from our Mega Skarmory, a silver steel bird with bladed red-lined wings and a long beak. Silhouette: diving, wings swept back like blades. Colours: polished steel, red linings. New feature: every feather of its wings is a separate crystal blade, fanned out with gaps of light between. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its breast, and dark red storm clouds with violet and cyan light swirling under it.

### 26. Mega Dynamax Houndoom: `Houndoom-Mega-Mmax`, about 23 m (yes)

- **Typing**: Dark/Fire (its Mega's).
- **Look**: A titan about 23 m tall, grown from Mega Houndoom, a black hound with curved horns, a bony ribcage-like armour on its chest and back and a forked tail. Silhouette: crouched, head low, horns forward. Colours: black, the bone armour white, red mouth. New feature: a row of crystal torches standing up along its spine, each burning with orange and violet flame. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its brow, and dark red storm clouds with violet and cyan light round its feet.

### 27. Mega Dynamax Tyranitar: `Tyranitar-Mega-Mmax`, about 30 m (yes)

- **Typing**: Rock/Dark (its Mega's).
- **Look**: A titan about 30 m tall, grown from Mega Tyranitar, a green armoured monster with many spikes and glowing cracks running across its plates. Silhouette: a living mountain, tail sweeping behind. Colours: olive green armour, the cracks glowing cyan and violet. New feature: a ring of crystal-veined boulders floating round its body, caught in a slow orbit. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 28. Mega Dynamax Sceptile: `Sceptile-Mega-Mmax`, about 23 m (yes)

- **Typing**: Grass/Dragon (its Mega's).
- **Look**: A titan about 23 m tall, grown from Mega Sceptile, a green gecko warrior with leaf blades on its arms, a red-tipped tail like a seed bundle. Silhouette: crouched to spring, tail arched like a tree. Colours: green, yellow, red tail tips turned to crystal. New feature: its tail has grown into a branching crystal tree with leaves of green light and lightning running through the branches. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 29. Mega Dynamax Blaziken: `Blaziken-Mega-Mmax`, about 23 m (yes)

- **Typing**: Fire/Fighting (its Mega's).
- **Look**: A titan about 23 m tall, grown from Mega Blaziken, a red bird-like fighter with a cream crest, flames streaming from its wrists and a long cream headpiece. Silhouette: a martial-arts stance, one leg raised. Colours: red, cream, gold. New feature: its wrist flames have grown into two long pillars of crystal flame that wrap its forearms like gauntlets. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 30. Mega Dynamax Swampert: `Swampert-Mega-Mmax`, about 23 m (yes)

- **Typing**: Water/Ground (its Mega's).
- **Look**: A titan about 23 m tall, grown from Mega Swampert, a blue mudfish wrestler with massive arms, orange gills and a black fin on its head. Silhouette: hunched forward on its huge fists. Colours: blue, orange gills, the fins turned to cyan crystal. New feature: a frozen tidal wave of crystal water rising from its back like a crest. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 31. Mega Dynamax Gardevoir: `Gardevoir-Mega-Mmax`, about 20 m (yes)

- **Typing**: Psychic (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Gardevoir, a slender graceful psychic figure in a flowing white ballgown, green hair and a red crest on its chest. Silhouette: arms open, gown spread wide like a dome. Colours: white gown, green, the crest red. New feature: the ballgown spread into a great crystal skirt patterned like a cathedral's round window of violet, pink and cyan glass. Shared look: the Mega mark (a ring round a four-pointed star) glowing inside the red chest crest, and dark red storm clouds with violet and cyan light round the gown's hem.

### 32. Mega Dynamax Sableye: `Sableye-Mega-Mmax`, about 20 m (maybe)

- **Typing**: Dark/Ghost (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Sableye, a small dark imp with gem eyes holding a huge red gem shield in front of it. Silhouette: hunched behind its towering gem. Colours: dark purple body, the red gem shield turned to a ruby crystal glowing violet. New feature: smaller crystals sprouting from the shield's rim like a crown of gemstones. Shared look: the Mega mark (a ring round a four-pointed star) inside the shield's gem, and dark red storm clouds with violet and cyan light round its feet.

### 33. Mega Dynamax Mawile: `Mawile-Mega-Mmax`, about 20 m (maybe)

- **Typing**: Steel (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Mawile, a small yellow creature with two huge black steel jaws growing from the back of its head like pigtails. Silhouette: the two great jaws looming over the small body. Colours: cream, black jaws with violet-pink crystal teeth. New feature: the jaws' teeth lit with violet light and a chain of crystal links between the jaws. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 34. Mega Dynamax Aggron: `Aggron-Mega-Mmax`, about 26 m (yes)

- **Typing**: Steel (its Mega's).
- **Look**: A titan about 26 m tall, grown from Mega Aggron, a grey armoured dinosaur covered in white steel plates with a ridge of horns. Silhouette: a walking fortress. Colours: steel grey, white plates turned to faceted crystal. New feature: a wall of crystal shield-plates rising from its back like towers on a castle wall. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 35. Mega Dynamax Medicham: `Medicham-Mega-Mmax`, about 20 m (maybe)

- **Typing**: Fighting/Psychic (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Medicham, a slender meditating humanoid with flowing sash-like appendages. Silhouette: seated cross-legged in the air, sashes fanned like many arms. Colours: red, white, cream. New feature: the sashes turned to ribbons of crystal light that circle it like a lotus. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its brow, and dark red storm clouds with violet and cyan light swirling beneath it.

### 36. Mega Dynamax Manectric: `Manectric-Mega-Mmax`, about 22 m (maybe)

- **Typing**: Electric (its Mega's).
- **Look**: A titan about 22 m tall, grown from Mega Manectric, a blue wolf with a wild yellow mane swept back like lightning. Silhouette: leaping, mane streaming. Colours: blue, yellow. New feature: the mane turned to a storm of yellow crystal spikes crackling with cyan arcs. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its brow, and dark red storm clouds with violet and cyan light round its feet.

### 37. Mega Dynamax Sharpedo: `Sharpedo-Mega-Mmax`, about 30 m (maybe)

- **Typing**: Water/Dark (its Mega's).
- **Look**: A titan about 30 m tall, grown from Mega Sharpedo, a navy-and-red shark with a scarred face and a jagged yellow crest on its snout. Silhouette: bursting up through the clouds, jaws open. Colours: navy blue, red, yellow crest turned to amber crystal. New feature: its fins turned to crystal blades that trail spiralling water. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its brow, and dark red storm clouds with violet and cyan light round it.

### 38. Mega Dynamax Camerupt: `Camerupt-Mega-Mmax`, about 30 m (maybe)

- **Typing**: Fire/Ground (its Mega's).
- **Look**: A titan about 30 m tall, grown from Mega Camerupt, a heavy camel-like beast with one great volcano on its back. Silhouette: low and wide, the volcano rising like a mountain. Colours: dark red-brown, the volcano's mouth glowing orange. New feature: the volcano's cone ringed with violet crystal vents spitting orange and pink light. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its brow, and dark red storm clouds with violet and cyan light round its feet.

### 39. Mega Dynamax Altaria: `Altaria-Mega-Mmax`, about 20 m (yes)

- **Typing**: Dragon/Fairy (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Altaria, a blue bird almost hidden in a huge cotton cloud of white fluff. Silhouette: a cloud bank with a bird's head and tail. Colours: sky blue, white fluff glowing pink at the edges. New feature: the fluff has grown into a whole cumulus cloud sparkling with crystal snowflakes. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its breast, and dark red storm clouds with violet and cyan light under it.

### 40. Mega Dynamax Banette: `Banette-Mega-Mmax`, about 20 m (maybe)

- **Typing**: Ghost (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Banette, a dark grey cursed puppet with a zipper mouth and spiky limbs. Silhouette: hanging in the air like a marionette. Colours: dark grey, gold zipper, red eyes. New feature: glowing violet crystal strings rising from its limbs to the sky as if something huge works it. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light beneath it.

### 41. Mega Dynamax Absol: `Absol-Mega-Mmax`, about 20 m (maybe)

- **Typing**: Dark (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Absol, a white-furred feline with a dark face, a scythe-like horn and wing-like fur on its back. Silhouette: poised on a peak, fur streaming. Colours: white, dark blue-grey. New feature: its back's fur wings turned to large feathered crystal wings. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its brow, and dark red storm clouds with violet and cyan light round its feet.

### 42. Mega Dynamax Glalie: `Glalie-Mega-Mmax`, about 25 m (maybe)

- **Typing**: Ice (its Mega's).
- **Look**: A titan about 25 m tall, grown from Mega Glalie, a round head of ice with a huge cracked-open jaw and horns. Silhouette: a floating glacier with a face. Colours: black, white ice. New feature: icicles turned to giant crystal fangs glowing cyan inside. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its brow, and dark red storm clouds with violet and cyan light swirling beneath it.

### 43. Mega Dynamax Salamence: `Salamence-Mega-Mmax`, about 22 m (yes)

- **Typing**: Dragon/Flying (its Mega's).
- **Look**: A titan about 22 m tall, grown from Mega Salamence, a blue dragon whose wings have fused into one great red crescent. Silhouette: the crescent wing like a blade across the sky. Colours: blue, red crescent turned to crystal. New feature: a comet tail of light trailing from the crescent's points. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light swirling under it.

### 44. Mega Dynamax Metagross: `Metagross-Mega-Mmax`, about 30 m (yes)

- **Typing**: Steel/Psychic (its Mega's).
- **Look**: A titan about 30 m tall, grown from Mega Metagross, a blue steel body with four great legs folded upward like arms and a silver X on its face. Silhouette: hovering, legs raised. Colours: steel blue, silver. New feature: an orbit of hexagonal crystal plates circling it like a brain's cells. Shared look: the Mega mark (a ring round a four-pointed star) set in the centre of the X on its face, and dark red storm clouds with violet and cyan light beneath it.

### 45. Mega Dynamax Lopunny: `Lopunny-Mega-Mmax`, about 20 m (maybe)

- **Typing**: Normal/Fighting (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Lopunny, a slender brown rabbit with long ears and fluffy cream fur at its wrists and legs. Silhouette: kicking high. Colours: brown, cream, the fluff glowing pink. New feature: its ears turned into long ribbons of light streaming behind it. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 46. Mega Dynamax Garchomp: `Garchomp-Mega-Mmax`, about 23 m (yes)

- **Typing**: Dragon/Ground (its Mega's).
- **Look**: A titan about 23 m tall, grown from Mega Garchomp, a navy blue shark-dragon with sharp scythe-like arm blades and a red belly. Silhouette: a land shark mid-charge. Colours: navy blue, red belly, the scythes turned to violet crystal. New feature: a tall crystal dorsal fin cutting through a whirling sandstorm. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 47. Mega Dynamax Lucario: `Lucario-Mega-Mmax`, about 20 m (yes)

- **Typing**: Fighting/Steel (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Lucario, a blue jackal warrior with black markings, spikes on its paws and chest, and four tassels behind its head. Silhouette: aura sphere ready. Colours: blue, black, tan. New feature: its head tassels turned into long ribbons of crystal aura, and an aura sphere between its paws. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 48. Mega Dynamax Abomasnow: `Abomasnow-Mega-Mmax`, about 32 m (yes)

- **Typing**: Grass/Ice (its Mega's).
- **Look**: A titan about 32 m tall, grown from Mega Abomasnow, a white furry snow-yeti with an evergreen back and huge icicles. Silhouette: a moving forest under snow. Colours: white, deep green, cyan ice. New feature: a forest of crystal ice-trees growing from its back. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.


### 49. Mega Dynamax Gallade: `Gallade-Mega-Mmax`, about 20 m (maybe)

- **Typing**: Psychic/Fighting (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Gallade, a slender white knight-like psychic with a green helmet crest, long blades on its elbows and a white cape-like lower body. Silhouette: blades crossed before it, cape billowing. Colours: white, green, a red fin on its chest. New feature: its elbow blades grown into two long crystal swords, edges lit cyan, the cape turned to a sheet of pale violet light. Shared look: the Mega mark (a ring round a four-pointed star) glowing inside the red fin on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 50. Mega Dynamax Froslass: `Froslass-Mega-Mmax`, about 31 m (maybe)

- **Typing**: Ice/Ghost (its Mega's).
- **Look**: A titan about 31 m tall, grown from our Mega Froslass, a white ghostly snow maiden with a hollow body, an ice-blue face, horn-like ornaments on her head and a long red sash round her waist. Silhouette: floating tall, the sash spiralling round her like a ribbon in a blizzard. Colours: snow white, ice blue, a deep red sash. New feature: a tall crown of crystal ice shards rising from her head, lit cyan from inside. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on her chest, and dark red storm clouds with violet and cyan light swirling beneath her.

### 51. Mega Dynamax Emboar: `Emboar-Mega-Mmax`, about 22 m (yes)

- **Typing**: Fire/Fighting (its Mega's).
- **Look**: A titan about 22 m tall, grown from our Mega Emboar, a huge upright fire pig wrestler, black and red, with a thick beard of flame round its jaw and gold bands on its wrists. Silhouette: arms raised over its head in a champion's pose, belly forward. Colours: red, black, gold, orange flame. New feature: the flame beard grown into a high collar of crystal flames standing round its head like a mane, orange at the root and pink at the tips. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its belly, and dark red storm clouds with violet and cyan light round its feet.

### 52. Mega Dynamax Excadrill: `Excadrill-Mega-Mmax`, about 20 m (yes)

- **Typing**: Ground/Steel (its Mega's).
- **Look**: A titan about 20 m tall, grown from our Mega Excadrill, a dark grey mole with steel claws and a steel blade on its head striped red and white. Silhouette: claws and head blade joined into one great drill pointed at the sky. Colours: gunmetal, red and white stripes. New feature: two more crystal drills grown on its forearms, spinning, throwing violet sparks. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 53. Mega Dynamax Audino: `Audino-Mega-Mmax`, about 20 m (maybe)

- **Typing**: Normal/Fairy (its Mega's).
- **Look**: A titan about 20 m tall, grown from Mega Audino, a round cream and pink rabbit-like nurse with very long frilled ears that reach the ground. Silhouette: standing gently, ears flowing like a cloak. Colours: cream, soft pink, blue eyes. New feature: the frilled ears' edges turned to rings of crystal that glow like a heartbeat, pink and cyan. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 54. Mega Dynamax Scolipede: `Scolipede-Mega-Mmax`, about 38 m (maybe)

- **Typing**: Bug/Poison (its Mega's).
- **Look**: A titan about 38 m tall, grown from our Mega Scolipede, a great magenta centipede with long horns on its head and tail and armoured segments. Silhouette: reared up like a cobra, its body curling down into the clouds. Colours: magenta, violet, yellow eyes. New feature: a row of crystal spines standing from every segment, dripping toxic pink light. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its head, and dark red storm clouds with violet and cyan light round its coils.

### 55. Mega Dynamax Chandelure: `Chandelure-Mega-Mmax`, about 30 m (yes)

- **Typing**: Ghost/Fire (its Mega's).
- **Look**: A titan about 30 m tall, grown from our Mega Chandelure, a black ghostly chandelier with a round glass head and arms holding violet flames. Silhouette: a chandelier hanging from nothing, arms spread. Colours: black iron, glass, violet and blue flame. New feature: its arms branched into a cathedral chandelier of dozens of crystal lamps, each burning violet. Shared look: the Mega mark (a ring round a four-pointed star) glowing inside its glass head, and dark red storm clouds with violet and cyan light boiling beneath it.

### 56. Mega Dynamax Chesnaught: `Chesnaught-Mega-Mmax`, about 20 m (yes)

- **Typing**: Grass/Fighting (its Mega's).
- **Look**: A titan about 20 m tall, grown from our Mega Chesnaught, a heavy armoured chestnut knight with a green spiked shell on its back and great shield-like forearms. Silhouette: braced behind its raised forearms like a castle gate. Colours: green shell, brown and cream body, orange accents. New feature: its back shell grown into a domed fortress of crystal spikes, green at the base and violet at the points. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 57. Mega Dynamax Delphox: `Delphox-Mega-Mmax`, about 20 m (yes)

- **Typing**: Fire/Psychic (its Mega's).
- **Look**: A titan about 20 m tall, grown from our Mega Delphox, an upright fox sorceress in a long robe of red fur, with tall ears and a burning wand. Silhouette: standing tall, wand raised over its head. Colours: red, yellow, black, orange flame. New feature: the wand grown into a tall crystal staff topped with a burning orb, orange at the heart and violet at the rim. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its robe's hem.

### 58. Mega Dynamax Greninja: `Greninja-Mega-Mmax`, about 20 m (yes)

- **Typing**: Water/Dark (its Mega's).
- **Look**: A titan about 20 m tall, grown from our Mega Greninja, a blue ninja frog with a long pink tongue wrapped round its neck like a scarf. Silhouette: crouched low on one knee, ready to spring. Colours: navy and light blue, pink tongue scarf, white markings. New feature: a giant water shuriken of cyan crystal strapped to its back, as wide as it is tall. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 59. Mega Dynamax Pyroar: `Pyroar-Mega-Mmax`, about 20 m (maybe)

- **Typing**: Fire/Normal (its Mega's).
- **Look**: A titan about 20 m tall, grown from our Mega Pyroar, a proud tan lion with a great mane of red and gold flame. Silhouette: standing on a crag, roaring. Colours: tan, red, gold. New feature: the mane turned to a sunburst of crystal flames fanning out round its head. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its brow, and dark red storm clouds with violet and cyan light round its feet.

### 60. Mega Dynamax Dragalge: `Dragalge-Mega-Mmax`, about 25 m (yes)

- **Typing**: Poison/Dragon (its Mega's).
- **Look**: A titan about 25 m tall, grown from our Mega Dragalge, a brown seaweed dragon like a leafy sea dragon, with frilly fins and a fin crest. Silhouette: rising out of the clouds as if from deep water, fronds floating round it. Colours: muddy brown, violet, a yellow crest. New feature: its fronds grown into a kelp forest of glowing poison crystal round its body. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light beneath it.

### 61. Mega Dynamax Golisopod: `Golisopod-Mega-Mmax`, about 28 m (maybe)

- **Typing**: Bug/Steel (its Mega's).
- **Look**: A titan about 28 m tall, grown from our Mega Golisopod, a grey armoured isopod knight with four arms, two of them great clawed ones. Silhouette: upright, claws raised like a samurai's. Colours: grey-violet armour, pale belly. New feature: a tall crest of crystal plates on its head like a helmet's. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 62. Mega Dynamax Glimmora: `Glimmora-Mega-Mmax`, about 34 m (yes)

- **Typing**: Rock/Poison (its Mega's).
- **Look**: A titan about 34 m tall, grown from our Mega Glimmora, a flower of crystal petals round a dark rocky core. Silhouette: petals spread like a giant blossom tilted to the sky. Colours: dark blue rock, pink and violet crystal petals. New feature: a slow rain of glowing poison crystals falling from its petals' tips round it. Shared look: the Mega mark (a ring round a four-pointed star) glowing in its core, and dark red storm clouds with violet and cyan light beneath it.

### 63. Mega Dynamax Baxcalibur: `Baxcalibur-Mega-Mmax`, about 25 m (yes)

- **Typing**: Dragon/Ice (its Mega's).
- **Look**: A titan about 25 m tall, grown from our Mega Baxcalibur, a heavy grey dragon of ice with a jagged blade-like fin down its back. Silhouette: crouched like a crocodile, back fin like a raised sword. Colours: steel grey, ice blue, yellow eyes. New feature: the back fin grown into one crystal greatsword along its whole spine, glowing cyan. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 2.7 The briefs: R20b's fifteen

Each from R20b's own Look (its reference drawing is `docs/art/reference/r20b-megas/<Species>-Mega.svg`, and Gemini's `docs/art/reference/gemini/megas/`); a brief R20b changes is written again.

### 64. Mega Dynamax Typhlosion: `Typhlosion-Mega-Mmax`, about 20 m (yes, if R20b's row is)

- **Typing**: Fire/Ground (R20b's proposal).
- **Look**: A titan about 20 m tall, grown from Mega Typhlosion, a broad cream beast hunched low, with a ring of dark rock round its shoulders like a crater's rim with fire welling inside, an ash-grey back cracked with lava, and rock bracers on its forearms. Silhouette: a living volcano on four legs. Colours: cream, charcoal, ash grey, glowing orange lava. New feature: the crater's three tongues of flame grown into three pillars of crystal fire standing from its shoulders. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its brow, and dark red storm clouds with violet and cyan light round its feet.

### 65. Mega Dynamax Torterra: `Torterra-Mega-Mmax`, about 26 m (yes, if R20b's row is)

- **Typing**: Grass/Ground (R20b's proposal).
- **Look**: A titan about 26 m tall, grown from Mega Torterra, a great green tortoise whose shell carries a ridge of three snowcapped peaks, moss on the rim and a small grove of trees, with long tusks. Silhouette: a mountain range walking. Colours: olive green, brown, snow white, moss. New feature: the peaks turned to crystal mountains, violet in their shadows and pink at their snowy tips. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its brow, and dark red storm clouds with violet and cyan light round its feet like low mist round a range.

### 66. Mega Dynamax Infernape: `Infernape-Mega-Mmax`, about 20 m (yes, if R20b's row is)

- **Typing**: Fire/Fighting (R20b's proposal).
- **Look**: A titan about 20 m tall, grown from Mega Infernape, a tall orange-and-cream fire monkey with a mane of flame streaming from its head down its back, gold armour plates on shoulders and knees and rings of flame round its wrists. Silhouette: a fighter's stance, one fist forward. Colours: orange, cream, gold, flame. New feature: the wrist rings grown into great hoops of crystal fire round each fist. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 67. Mega Dynamax Empoleon: `Empoleon-Mega-Mmax`, about 20 m (yes, if R20b's row is)

- **Typing**: Water/Steel (R20b's proposal).
- **Look**: A titan about 20 m tall, grown from Mega Empoleon, a regal navy penguin emperor with a tall gold-edged three-pronged crown, long steel flipper blades, a navy cape of plumage and gold epaulettes. Silhouette: standing straight as a throne, cape falling behind. Colours: navy, white, gold, steel. New feature: the crown grown into a tall trident of cyan crystal. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round the cape's hem.

### 68. Mega Dynamax Serperior: `Serperior-Mega-Mmax`, about 40 m (yes, if R20b's row is)

- **Typing**: Grass/Dragon (R20b's proposal).
- **Look**: A titan about 40 m tall, grown from Mega Serperior, a regal green serpent rising high from its coils, a hood of leaves round its head like a cobra's, two swept-back horns of leaf and two leaf-wings by its neck. Silhouette: a coiled tower with a hooded head. Colours: green, cream belly, yellow leaf edges. New feature: the leaf hood turned to a fan of crystal leaves, green at the root and violet at the tips. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its brow, and dark red storm clouds with violet and cyan light round its coils.

### 69. Mega Dynamax Samurott: `Samurott-Mega-Mmax`, about 20 m (yes, if R20b's row is)

- **Typing**: Water/Steel (R20b's proposal).
- **Look**: A titan about 20 m tall, grown from Mega Samurott, a blue sea lion samurai on four legs with a full kabuto helmet crested with a gold crescent, shell armour over its shoulders and two seamitar blades half drawn from sheaths on its forelegs. Silhouette: a warlord ready to charge. Colours: blue, steel, gold, cream edges. New feature: both seamitars fully drawn as long blades of cyan crystal. Shared look: the Mega mark (a ring round a four-pointed star) on the helmet's brow plate, and dark red storm clouds with violet and cyan light round its feet.

### 70. Mega Dynamax Decidueye: `Decidueye-Mega-Mmax`, about 20 m (yes, if R20b's row is)

- **Typing**: Grass/Ghost (R20b's proposal).
- **Look**: A titan about 20 m tall, grown from Mega Decidueye, a tall hooded owl archer whose cowl shades its face to the eyes, with a cape of quills like arrows in a quiver, teal glow at their tips and a bow of vine across its chest. Silhouette: drawing the bow. Colours: brown, green, cream, teal glow. New feature: an arrow of crystal light nocked on the bow, as long as the owl is tall. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 71. Mega Dynamax Incineroar: `Incineroar-Mega-Mmax`, about 22 m (yes, if R20b's row is)

- **Typing**: Fire/Dark (R20b's proposal).
- **Look**: A titan about 22 m tall, grown from Mega Incineroar, a broad black-and-red tiger wrestler with a champion's belt of fire with a gold plate bearing a paw print. Silhouette: arms flexed, chest out. Colours: black, red, gold, flame. New feature: the belt's flames grown into a ring of crystal fire round its waist, the gold plate glowing. Shared look: the Mega mark (a ring round a four-pointed star) set in the belt plate beside the paw print, and dark red storm clouds with violet and cyan light round its feet.

### 72. Mega Dynamax Primarina: `Primarina-Mega-Mmax`, about 22 m (yes, if R20b's row is)

- **Typing**: Water/Fairy (R20b's proposal).
- **Look**: A titan about 22 m tall, grown from Mega Primarina, a sea-lion singer with a long train of hair falling like a gown strung with pearls, a coral tiara and a wide fan tail, floating spheres of water round her. Silhouette: singing with head raised. Colours: white, sky blue, pink coral, pearls. New feature: the spheres of water turned to a ring of crystal spheres lit violet and cyan like stage lights. Shared look: the Mega mark (a ring round a four-pointed star) set in the tiara, and dark red storm clouds with violet and cyan light round her tail.

### 73. Mega Dynamax Rillaboom: `Rillaboom-Mega-Mmax`, about 30 m (yes, if R20b's row is)

- **Typing**: Grass (R20b's proposal).
- **Look**: A titan about 30 m tall, grown from Mega Rillaboom, a big green gorilla drummer with a mane of leaves over its shoulders, a stump drum strapped to its back on a harness of vines and two thick branch drumsticks. Silhouette: drumsticks raised, the drum behind it. Colours: green, brown wood, orange accents. New feature: the drum's face turned to a disc of crystal that ripples with rings of light at each beat. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet. (It is not Gigantamax Rillaboom, whose drum is a great stump kit; keep the two apart.)

### 74. Mega Dynamax Cinderace: `Cinderace-Mega-Mmax`, about 29 m (yes, if R20b's row is)

- **Typing**: Fire (R20b's proposal).
- **Look**: A titan about 29 m tall, grown from Mega Cinderace, a slim white rabbit striker with long ears swept back into a crest of flame, a white scarf trailing behind and a ball of fire under one foot. Silhouette: about to kick. Colours: white, red, yellow, flame. New feature: the fireball under its foot turned to a ball of crystal fire, orange inside and violet at its facets. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet. (Not Gigantamax Cinderace's giant fireball: keep it the size of the rabbit's body.)

### 75. Mega Dynamax Inteleon: `Inteleon-Mega-Mmax`, about 42 m (yes, if R20b's row is)

- **Typing**: Water (R20b's proposal).
- **Look**: A titan about 42 m tall, grown from Mega Inteleon, a tall slender blue lizard agent with a yellow crest swept back like a hood, a tail falling like the tails of a coat and a scope over one eye. Silhouette: standing tall, one finger pointed like a pistol. Colours: navy, light blue, yellow crest. New feature: the scope's lens turned to a large cyan crystal lens that projects a crosshair of light. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 76. Mega Dynamax Meowscarada: `Meowscarada-Mega-Mmax`, about 20 m (yes, if R20b's row is)

- **Typing**: Grass/Dark (R20b's proposal).
- **Look**: A titan about 20 m tall, grown from Mega Meowscarada, a slender green cat magician in a great cape of leaves lined pink, a full bloom of petals at its throat and a sharp mask, spinning a flower bomb on one fingertip. Silhouette: a bow to the audience, cape sweeping. Colours: green, pink, magenta. New feature: the flower bomb turned to a crystal rose spinning above its fingertip. Shared look: the Mega mark (a ring round a four-pointed star) set in the bloom at its throat, and dark red storm clouds with violet and cyan light round its feet.

### 77. Mega Dynamax Skeledirge: `Skeledirge-Mega-Mmax`, about 20 m (yes, if R20b's row is)

- **Typing**: Fire/Ghost (R20b's proposal).
- **Look**: A titan about 20 m tall, grown from Mega Skeledirge, a heavy red crocodile singer with a skull painted down its back, a great wide-brimmed crown of fire on its head with a bird's face in front, and a tail flame shaped like a guitar's head. Silhouette: jaws open, singing. Colours: red, white skull paint, orange and yellow flame. New feature: the crown of fire turned to crystal flames like a ring of stage spotlights. Shared look: the Mega mark (a ring round a four-pointed star) as a glowing crystal medallion on its chest, and dark red storm clouds with violet and cyan light round its feet.

### 78. Mega Dynamax Quaquaval: `Quaquaval-Mega-Mmax`, about 22 m (yes, if R20b's row is)

- **Typing**: Water/Fighting (R20b's proposal).
- **Look**: A titan about 22 m tall, grown from Mega Quaquaval, a tall blue dancing bird with a crescent headdress of plumes tipped orange, streamers of water beads trailing from its wrists and a skirt of blue feathers. Silhouette: mid-dance on one leg, streamers swirling. Colours: blue, white, orange, yellow. New feature: the water streamers turned to long ribbons of cyan crystal beads circling it like a dance's path. Shared look: the Mega mark (a ring round a four-pointed star) set in the headdress, and dark red storm clouds with violet and cyan light round its feet.

## 3. Where it is handed over, and how a battle offers it

### 3.1 The item

**The Mega Max Band** (default; question 1): a new key item, our own, "A Dynamax Band with a Key Stone set into it. It lets a Mega-Evolved Pokémon Dynamax." (words for the build to tune; Romanian at the build). It sits beside the Key Stone and the Dynamax Band in the key items' pocket and does nothing outside battle. Not in the data under that name or any other (checked; nor is "Wishing Star" or "Max Soup").

### 3.2 Where, region by region

Each later mechanic is handed over in its own region (decision 06 · 2): the Key Stone in Kalos, the Dynamax Band in Galar. The chain goes Sinnoh, Kanto, Johto, Hoenn, Unova, **Kalos**, Alola, **Galar**, Paldea, so a player reaches Galar with the Key Stone already in the bag.

- **Default: Galar, after its Hall of Fame.** Sonia, at the research lab in Wedgehurst, has studied the Wishing Stars that power the Dynamax Band, and the Key Stone's light from Kalos; once the player has entered Galar's Hall of Fame and holds both the Key Stone and the Dynamax Band, she asks for the two, sets the stone into the band, and hands back the Mega Max Band (the player keeps the Key Stone and the Dynamax Band: the new band is a third item, so nothing else that asks for either breaks). A scene of our own on plan 23 · Galar's beats, with a flag of ours (`FLAG_GOT_MEGA_MAX_BAND`). *Alternatives*: (b) Kalos, from Professor Sycamore, the first time the player comes back to Lumiose City with the Dynamax Band (Kalos is crossed again by Fly between regions, plan 18 · W9); (c) no new item: from the moment the bag holds both the Key Stone and the Dynamax Band (question 1's alternative).
- **Who uses it, once the player has it** (default; question 8): one Champion-level trainer of each built region gets a rematch team of ours with a Mega Dynamax ace and the band, fought only after the player holds the band (so nobody meets it first from the other side), on the League's or the post-game's rematch beat that region already has:

| Region | Who | Their ace (its stone) | The team in the data today |
|---|---|---|---|
| Sinnoh | Cynthia, at the League | Garchomp (Garchompite) | `champion_cynthia` has Garchomp |
| Kanto | the rival, as Champion | Pidgeot (Pidgeotite) | `kanto_champion_first_squirtle` has Pidgeot (the other two starters' teams too) |
| Johto | Lance, at the League | Dragonite (Dragoninite) | `johto_champion_lance` has Dragonite (and Gyarados) |
| Hoenn | Steven, after the Hall of Fame | Metagross (Metagrossite) | `hoenn_steven` has Metagross |
| Unova | Iris, at the League | Aggron (Aggronite) | `unova_champion_iris` has Aggron |
| Kalos | Diantha | Gardevoir (Gardevoirite) | not built (plan 23) |
| Alola | Professor Kukui | Lucario (Lucarionite) | not built (plan 23) |
| Galar | Leon | Charizard (Charizardite Y) | not built (plan 23) |
| Paldea | Geeta | Glimmora (Glimmoranite) | not built (plan 23) |

  Each is a team of `Data/our-trainers.json` (`<id>_megamax`, with `keyStone`, `dynamaxBand` and `megaMaxBand`), never a change to the imported table, so `trainers.txt` and the golden battles don't move. *Alternatives*: only Galar's Leon (the one place it is born); or nobody but the player.
- **Where the stones are found** is R20's and R20b's (Kalos, and each of ours in its starter's home region); nothing here changes it.

### 3.3 In the battle's menu

The move menu's item button (`ToggleLaterMechanic`) arms, in turn, whatever the Pokémon can have today (a Mega Evolution, then a Z-Move, then a Dynamax, then a Terastallization). Mega Dynamax adds one step:

- **A Pokémon holding its stone** (with the Key Stone and the Mega Max Band, neither spent): the first press arms **Mega Evolution**, the second **Mega Dynamax** (both flags: it Mega Evolves and grows this turn), the third disarms. Without the band, as today.
- **A Pokémon already in its Mega form** (the band, the Dynamax unspent): the first press arms **Mega Dynamax** (the Dynamax flag alone), the second disarms.
- While Mega Dynamax is armed the move names show their Max Moves (`MaxMoveAt`, as an armed Dynamax does), and the button's caption reads "Mega Dynamax" (`battleMenu.later.megaDynamax`). Its picture is plan 25's (Y15's Dynamax button with the Mega's four colours).
- On the HP box the turns left show as a Dynamax's do (`DynamaxTurnsLeft`).

## 4. Open questions

1. **The key item.** *Default*: a new key item, the **Mega Max Band**, needed beside the Key Stone (3.1). *Alternative*: no new item; the Key Stone and the Dynamax Band together let a Mega Dynamax.
2. **The same turn.** *Default*: the Mega Evolution and the Dynamax **may be chosen together** in one turn (1.2), and a Mega of an earlier turn may Dynamax later. *Alternative*: only on a later turn than the Mega Evolution, so it is a two-turn plan the foe can see coming.
3. **Who may.** *Default*: **any non-legendary Mega**; a row of part 2 decides only whether it has a look of its own. *Alternative*: only the Megas with a Mega Dynamax form.
4. **Legendary and mythical Megas.** *Default*: **no** (12 forms: Mewtwo X and Y, Latias, Latios, Rayquaza, Heatran, Darkrai, Zygarde, Diancie, both Magearna, Zeraora). *Alternative*: yes, rules and forms, with briefs written at the build.
5. **Max Moves, and the Gigantamax species.** *Default*: the **type's Max Moves only**; a Pokémon with the Gigantamax Factor holding its stone Mega Dynamaxes with its factor unused, and Gigantamaxes as today when it holds no stone (both kept). *Alternatives*: (a) a Gigantamax species keeps its **G-Max Move** while Mega Dynamaxed, from a move of the G-Max Move's type (Venusaur's Vine Lash, Charizard's Wildfire, Blastoise's Cannonade, Gengar's Terror; and Rillaboom's Drum Solo, Cinderace's Fireball, Inteleon's Hydrosnipe); (b) every Mega Dynamax form gets a signature Max Move of its own, as a G-Max Move, written in a later proposal of its own (about fifty moves).
6. **The numbers.** *Default*: **the games' Dynamax**, unchanged: HP ×1.5 to ×2 by Dynamax Level, three turns. *Alternatives*: (a) two turns, since a Mega's stats are already 100 over its species'; (b) HP ×1.5 whatever the Dynamax Level.
7. **Where it is handed over.** *Default*: **Galar, after its Hall of Fame, from Sonia** in Wedgehurst (3.2). *Alternatives*: (b) Kalos, from Professor Sycamore, on the first return with the Dynamax Band; (c) nowhere, if question 1's alternative is chosen.
8. **Who uses it.** *Default*: **one Champion-level trainer of each region**, in a rematch team of ours fought only after the player has the band (3.2's table). *Alternatives*: only Leon in Galar; nobody but the player.
9. **The "maybe" rows.** *Default*: a **maybe is a no** unless the user says yes to the row (27 rows, briefs written). *Alternative*: every maybe is a yes (78 forms in all with R20b's).
10. **The trainer AI.** *Default*: a trainer with the band **keeps its Dynamax for its Mega** and Mega Dynamaxes it the first turn it can (1.4). *Alternative*: the rule of today, the first that can Dynamax does, which with a band would rarely reach the Mega.
11. **An option to turn it off.** *Default*: **none**: `Ruleset.MegaDynamax` in the modern rules, and nobody but the player's choice and the trainers with the band ever use it. *Alternative*: an options row before `OptionRow.Help`, "MEGA DYNAMAX: ON/OFF", which hides the menu's step and the trainers' bands.
12. **The battle log.** *Default*: **R22's events** (`Dynamaxed` with the `-Mmax` form, `DynamaxEnded` back to the Mega), no event of its own (1.5). *Alternative*: a `MegaDynamaxed` event at `BattleLog.cs`'s end.
13. **Sizes.** *Default*: the **rule** of "How to read this" (12 × the Mega's height, 20 to 60 m, over a Gigantamax form by 2 m). *Alternative*: heights set by hand per form.
14. **Max Raid dens** (plan 23 · Z10). *Default*: **no Mega Dynamax raid boss**. *Alternative*: a post-game den in Galar where a Mega Dynamax boss is met (a wild Pokémon may then do it there alone).

## The table to answer

**The rules** (answer each with its default, its alternative, or a change):

| Q | Question | Default |
|---|---|---|
| 1 | The key item | the Mega Max Band, beside the Key Stone |
| 2 | The same turn | Mega Evolution and Dynamax may be chosen together |
| 3 | Who may | any non-legendary Mega; rows decide looks only |
| 4 | Legendary and mythical Megas | no |
| 5 | Max Moves, Gigantamax | the type's Max Moves; Gigantamax kept beside it |
| 6 | The numbers | the games' Dynamax (×1.5 to ×2 HP, three turns) |
| 7 | Where | Galar, after its Hall of Fame, from Sonia |
| 8 | Who uses it | one Champion-level rematch per region (3.2) |
| 9 | Maybe rows | no unless said yes |
| 10 | The AI | keeps its Dynamax for its Mega |
| 11 | An option | none |
| 12 | The log | R22's events |
| 13 | Sizes | the rule |
| 14 | Raid dens | none |

**The forms** (answer each row yes, no or a change; the 34 rows of "no" are in 2.3 and need an answer only to turn one to yes):

| # | Mega Dynamax form | Rec. | Height | New feature |
|---|---|---|---|---|
| 1 | `Venusaur-Mega-Mmax` | yes | 29 m | crystal stamen spires in a lotus |
| 2 | `Charizard-Mega-X-Mmax` | yes | 30 m | crown of blue crystal fire |
| 3 | `Charizard-Mega-Y-Mmax` | yes | 30 m | a small sun caged at its chest |
| 4 | `Blastoise-Mega-Mmax` | yes | 27 m | a turret of three crystal barrels |
| 5 | `Beedrill-Mega-Mmax` | maybe | 20 m | a halo of crystal needles |
| 6 | `Pidgeot-Mega-Mmax` | yes | 26 m | a comet-tail fan of quills |
| 7 | `Raichu-Mega-X-Mmax` | maybe | 20 m | a lightning-blade tail |
| 8 | `Raichu-Mega-Y-Mmax` | maybe | 20 m | a ring of lightning orbs |
| 9 | `Alakazam-Mega-Mmax` | yes | 20 m | a wheel of five crystal blades |
| 10 | `Victreebel-Mega-Mmax` | maybe | 54 m | a crown of crystal pitchers |
| 11 | `Slowbro-Mega-Mmax` | maybe | 24 m | a crystal spiral shell |
| 12 | `Gengar-Mega-Mmax` | yes | 22 m | six shadow hands |
| 13 | `Kangaskhan-Mega-Mmax` | maybe | 26 m | the young's crystal gloves |
| 14 | `Starmie-Mega-Mmax` | maybe | 28 m | a halo star of light |
| 15 | `Pinsir-Mega-Mmax` | maybe | 20 m | crystal teeth on its pincers |
| 16 | `Gyarados-Mega-Mmax` | yes | 60 m | crystal sail-fins down its spine |
| 17 | `Aerodactyl-Mega-Mmax` | maybe | 25 m | amber crystal shards |
| 18 | `Dragonite-Mega-Mmax` | yes | 26 m | antennae as ribbons of light |
| 19 | `Meganium-Mega-Mmax` | yes | 29 m | a sunflower corona |
| 20 | `Feraligatr-Mega-Mmax` | yes | 28 m | two dragon horns of crystal |
| 21 | `Ampharos-Mega-Mmax` | yes | 20 m | a lighthouse lantern tail |
| 22 | `Steelix-Mega-Mmax` | yes | 60 m | a crown of crystal pillars |
| 23 | `Scizor-Mega-Mmax` | yes | 24 m | stained-glass crystal wings |
| 24 | `Heracross-Mega-Mmax` | maybe | 20 m | a crown-horn of crystal points |
| 25 | `Skarmory-Mega-Mmax` | maybe | 20 m | wings of separate crystal blades |
| 26 | `Houndoom-Mega-Mmax` | yes | 23 m | crystal torches down its spine |
| 27 | `Tyranitar-Mega-Mmax` | yes | 30 m | orbiting crystal-veined boulders |
| 28 | `Sceptile-Mega-Mmax` | yes | 23 m | a crystal tree for a tail |
| 29 | `Blaziken-Mega-Mmax` | yes | 23 m | crystal flame gauntlets |
| 30 | `Swampert-Mega-Mmax` | yes | 23 m | a frozen tidal wave crest |
| 31 | `Gardevoir-Mega-Mmax` | yes | 20 m | a rose-window crystal gown |
| 32 | `Sableye-Mega-Mmax` | maybe | 20 m | a crown of gems on its shield |
| 33 | `Mawile-Mega-Mmax` | maybe | 20 m | crystal teeth and a chain |
| 34 | `Aggron-Mega-Mmax` | yes | 26 m | a castle wall of crystal plates |
| 35 | `Medicham-Mega-Mmax` | maybe | 20 m | a lotus of crystal ribbons |
| 36 | `Manectric-Mega-Mmax` | maybe | 22 m | a storm mane of crystal spikes |
| 37 | `Sharpedo-Mega-Mmax` | maybe | 30 m | crystal fin blades |
| 38 | `Camerupt-Mega-Mmax` | maybe | 30 m | crystal vents round the cone |
| 39 | `Altaria-Mega-Mmax` | yes | 20 m | a whole cumulus of crystal snow |
| 40 | `Banette-Mega-Mmax` | maybe | 20 m | crystal puppet strings |
| 41 | `Absol-Mega-Mmax` | maybe | 20 m | feathered crystal wings |
| 42 | `Glalie-Mega-Mmax` | maybe | 25 m | crystal fangs |
| 43 | `Salamence-Mega-Mmax` | yes | 22 m | a comet tail from the crescent |
| 44 | `Metagross-Mega-Mmax` | yes | 30 m | an orbit of hexagon plates |
| 45 | `Lopunny-Mega-Mmax` | maybe | 20 m | ears as ribbons of light |
| 46 | `Garchomp-Mega-Mmax` | yes | 23 m | a crystal dorsal fin in a sandstorm |
| 47 | `Lucario-Mega-Mmax` | yes | 20 m | tassels as crystal aura ribbons |
| 48 | `Abomasnow-Mega-Mmax` | yes | 32 m | a forest of crystal ice-trees |
| 49 | `Gallade-Mega-Mmax` | maybe | 20 m | crystal swords and a cape of light |
| 50 | `Froslass-Mega-Mmax` | maybe | 31 m | a crown of ice shards |
| 51 | `Emboar-Mega-Mmax` | yes | 22 m | a collar of crystal flames |
| 52 | `Excadrill-Mega-Mmax` | yes | 20 m | two more crystal drills |
| 53 | `Audino-Mega-Mmax` | maybe | 20 m | crystal rings on its ears |
| 54 | `Scolipede-Mega-Mmax` | maybe | 38 m | crystal spines on every segment |
| 55 | `Chandelure-Mega-Mmax` | yes | 30 m | a cathedral chandelier |
| 56 | `Chesnaught-Mega-Mmax` | yes | 20 m | a dome fortress of crystal spikes |
| 57 | `Delphox-Mega-Mmax` | yes | 20 m | a crystal staff |
| 58 | `Greninja-Mega-Mmax` | yes | 20 m | a crystal water shuriken |
| 59 | `Pyroar-Mega-Mmax` | maybe | 20 m | a sunburst mane |
| 60 | `Dragalge-Mega-Mmax` | yes | 25 m | a kelp forest of crystal |
| 61 | `Golisopod-Mega-Mmax` | maybe | 28 m | a crest of crystal plates |
| 62 | `Glimmora-Mega-Mmax` | yes | 34 m | a rain of poison crystals |
| 63 | `Baxcalibur-Mega-Mmax` | yes | 25 m | a crystal greatsword spine |
| 64 | `Typhlosion-Mega-Mmax` | yes if R20b | 20 m | pillars of crystal fire |
| 65 | `Torterra-Mega-Mmax` | yes if R20b | 26 m | crystal mountain peaks |
| 66 | `Infernape-Mega-Mmax` | yes if R20b | 20 m | hoops of crystal fire on its fists |
| 67 | `Empoleon-Mega-Mmax` | yes if R20b | 20 m | a crystal trident crown |
| 68 | `Serperior-Mega-Mmax` | yes if R20b | 40 m | a hood of crystal leaves |
| 69 | `Samurott-Mega-Mmax` | yes if R20b | 20 m | both blades drawn in crystal |
| 70 | `Decidueye-Mega-Mmax` | yes if R20b | 20 m | an arrow of light on the bow |
| 71 | `Incineroar-Mega-Mmax` | yes if R20b | 22 m | a ring of crystal fire round the belt |
| 72 | `Primarina-Mega-Mmax` | yes if R20b | 22 m | crystal spheres like stage lights |
| 73 | `Rillaboom-Mega-Mmax` | yes if R20b | 30 m | a crystal drum face |
| 74 | `Cinderace-Mega-Mmax` | yes if R20b | 29 m | a ball of crystal fire |
| 75 | `Inteleon-Mega-Mmax` | yes if R20b | 42 m | a crystal scope lens |
| 76 | `Meowscarada-Mega-Mmax` | yes if R20b | 20 m | a spinning crystal rose |
| 77 | `Skeledirge-Mega-Mmax` | yes if R20b | 20 m | a crown of crystal spotlights |
| 78 | `Quaquaval-Mega-Mmax` | yes if R20b | 22 m | ribbons of crystal beads |

At the defaults: 36 forms of the data and R20b's fifteen, **51 forms**; every one also needs the user's sculpt (2.2).

## The technical plan (for after the user's answers)

All of it under the modern rules alone (`Ruleset.MegaDynamax`), so `battles-platinum.txt` can't move. One session for the rules and the data, about 3 to 4 hours; the sculpts are the user's, whenever the user says.

1. **The rules** (1.1 to 1.5): `Ruleset.MegaDynamax`; `CoreSetup.MegaMaxBand`, `Trainer.MegaMaxBand` and the record's `megaMaxBand` (`TrainerDatabase`, `BattleMirror`), the Vs. Recorder's file; `BattleCore.Dynamax.cs`'s `WhyNotMegaDynamax`, the choice that carries both flags, `MegaDynamaxFormOf`; Parental Bond's and the -ate abilities' rules for Max Moves (1.3); `TrainerAi.WithTheLaterMechanics`'s one rule; `BattleEngine.Dynamax.cs`'s menu step, `MegaDynamaxArmed`, `CanMegaDynamaxNow`; the three lines and the button's caption in `ui.json` with their Romanian. `docs/mechanics/rulings.md` gains "Mega Dynamax" after "Dynamax and Gigantamax", and plan 06's decision 3 is rewritten in one sentence.
2. **The data**: `FormKind.MegaDynamax` at the enum's end; the importer writes each approved form after its Mega (a hand table, `tools/DataImporter/Overrides/mega-dynamax.json`, a form name and its height each, and `MegaDynamaxForms.cs` copying the Mega form's types, stats and abilities into it; weight 1000), never by hand in `species.json`; the Mega Max Band in `Overrides/items.json` at the next free id, with its Romanian name and description in `Data/text/ro/names.items.json` and the French counts of `LocalNamesTests` left as they are (an item of ours has no French). Coverage's item count up by one.
3. **The tests** (`MegaDynamaxTests`, under the modern rules, numbers worked by hand): the HP at Dynamax Level 0 and 10 and back; Mega Evolution and Dynamax in one choice, in Showdown's order, with the order put again; on a later turn; the trainer's two counts spent; each refusal (no band, a legendary's Mega, a primal, a Terastallized Pokémon, a Dynamax spent); it ends as a Mega and stays one; Parental Bond leaves a Max Move alone; an -ate ability on a Max Move; a Gigantamax Charizard holding Charizardite Y (Mega Dynamax, no G-Max Move) and holding nothing (Gigantamax); the AI keeping its Dynamax for its Mega; a recorded battle replayed; and under Platinum's rules nothing reached. Golden files: none should move (no trainer of the corpus has the band); the session runs `GOLDEN=accept … --filter Golden` and reads the diff to be sure.
4. **The story** (plan 23, per region, when each chapter is built): Sonia's scene in Galar (or the alternative's), and the nine rematch teams of 3.2 in `our-trainers.json` with the scripts that pick them once the player holds the band. Not in this session's build: Kalos, Alola, Galar and Paldea aren't built.
5. **The look** (plan 25, a Y session of its own after Y15): the wreath in the Mega's four colours, the growth from a Mega, the button; the style guide first.
6. **The sculpts**: the user's (the user is reworking every sculpt). Until then a Mega Dynamax shows its Mega's model grown, as a form without a model of its own shows its species'. When the user wants them, `tools/GeminiArt`'s list of files gains this document (2.5) and the reference pictures are drawn from the briefs.

## How the numbers were checked

A Node.js script kept in the session's scratch folder, reading only `species.json`, `items.json`, `moves.json`, `abilities.json`, `trainers.json`, the regions' `world/<region>/trainers.json` and this document. It checked: that the data has 97 Mega forms of 87 species, 48 of X, Y, Omega Ruby and Alpha Sapphire (the list of 48 checked against the data, each a Mega form of it) and 49 others; that every legendary's or mythical's Mega (12, by the data's flags) is a "no"; that every yes and maybe is a Mega form of the data; the counts 36, 27 and 34; each form's typing, ability (the form's first, or its species' where it names none; each one in `abilities.json`), total and height in the table, and that the table's every row carries the recommendation the script holds; that no name of the data ends in `-Mmax`, and that every `-Mmax` name in this document is a row of the table or one of R20b's fifteen; that every yes and maybe has a brief whose heading carries its height, and that every height is the rule's (12 × the Mega's height, 20 to 60 m, 2 m over a Gigantamax form: Venusaur 24 m, Charizard 28 m, Blastoise 25 m, Gengar 20 m, Rillaboom 28 m, Cinderace 27 m, Inteleon 40 m in the data); that R20b's fifteen are species of the data with no Mega yet; that the five Megas of species with a Gigantamax form are the five named; that the Key Stone, the Dynamax Band, the Dynamax Candy and the nine stones of 3.2 are items of the data and that "Mega Max Band", "Wishing Star" and "Max Soup" are not; that the data has 19 Max Moves and 33 G-Max Moves, Max Guard and the four G-Max Moves of question 5's species, and 631 moves with a `maxPower` from 70 to 150 (Flamethrower 130, Close Combat 95); that every ability named in 1.3 exists; and that each trainer of 3.2's table that is in the data today has the ace it names.
