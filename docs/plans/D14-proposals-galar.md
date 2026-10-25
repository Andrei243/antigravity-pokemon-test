# D14 · Evolutions past level 100, batch 8 (Galar and Hisui): the proposal

Plan 03 · D14, the design part of its batch for Galar's generation, written in wave 53 after the user approved batches 1 to 4 (Kanto, Johto, Hoenn and Sinnoh: [D14-proposals-kanto.md](D14-proposals-kanto.md), [D14-proposals-johto.md](D14-proposals-johto.md), [D14-proposals-hoenn.md](D14-proposals-hoenn.md), [D14-proposals-sinnoh.md](D14-proposals-sinnoh.md)) with every default, on 2026-10-25. **Answered by the user on 2026-10-26 and built in wave 53**: all 42 designs yes, matched by number (Enamorata, 2905, among them, though question 2 built it only beside Unova's forces of nature, which Unova's answers don't build: the one design whose condition the user overrode), every question at its default but 7 and 8, answered "b", whose designs this document didn't have and which [D14-proposals-galar-supplement.md](D14-proposals-galar-supplement.md) proposes. The 42 are rows of `tools/DataImporter/Overrides/species-past-hundred.json` (Nos. 2820 to 2905, at 226 to 240 and the legendary sets and Enamorata at 244), with two written Pokédex entries and a habitat each, held by `SpeciesPastHundredGalarTests`; no sculpt. A later session writes each approved row into the data (as entries of `tools/DataImporter/Overrides/species-past-hundred.json`, batch 1's table). The format is batch 4's. Unova's, Kalos's, Alola's and Paldea's batches are written beside this one in the same wave; where one of their answers touches this batch (Enamorus, question 2), it is said.

**What batches 1 to 4 settled, applied here without asking again**: the rise is **+80, capped so that no species of ours that isn't legendary passes 600**, and a region's legendary set rises to **660** (the birds, the beasts, the Regis, the eon pair, the lake guardians); the number is **2000 + the number of the species it evolves from** (here 2820 to 2905, meeting none of the earlier batches' 2020 to 2482); abilities come **only from `abilities.json`**, as many slots as the species had, in its order, the hidden one last; the evolution is **by level with a condition the engine already runs**, a held item used up; our species are listed in the National Pokédex after the 1,025 and **count toward none of its totals or diplomas**; names are invented, one word of at most 12 letters, and held against every species and form of the data and the earlier batches' 170; **an evolution that starts from a regional form belongs to that form's own line** (`Evolution.FitsForm`), and a regional form's own past-100 evolution is a design only on the user's word (batch 1's question 10); a female look, a Mega or a Gigantamax form is kept where the species has one, and **ours have no forms of their own**; the Frontier lets them be entered (a legendary's evolution joins its ban list), Pal Park, migration, the way out, the Pokéathlon's rivals and the golden corpus are held to 1 to 1,025; the cover legendaries and the mythicals don't evolve; a region's pairs are built as sets or not at all; a species with two looks of two typings is left out (batch 4's Wormadam); **each species evolves within its own region's climb** (batch 3's question 1), which batch 3 wrote as "at most 160".

**Galar's climb, and so its levels** (question 1): Galar is not built and has no region file, so its levels are the ones plan 18 · W4's study set for it ([W4-level-curve-study.md](W4-level-curve-study.md), "The regions one after another", the fair climb under the modern EXP, which is what the built regions' `levels` follow: Hoenn's `[[2, 136], [58, 162]]`, Unova's `[[2, 162], [59, 185]]`). Each region begins at the Champion's level of the one before and ends at its own: Galar comes after Alola, whose Champion stands at about **224**, and its own Champion, Leon, at about **242** (Sword and Shield's 65). By the same mapping its region file would hold `[[2, 224], [65, 242]]`: its first Gym Leader at about 229, its last at about 237, the Crown Tundra's legendaries (Sword and Shield's about 70 to 75) at about 243 to 246. **That whole climb lies above 160**, the ceiling batch 3 wrote, and so do Unova's (162 to 185), Kalos's (185 to 205), Alola's (205 to 224) and Paldea's (242 to 258): under 160 every Galar species caught in Galar, at 224 or more, would evolve at its first level. By default this batch's species evolve **between 226 and 240**, inside Galar's own climb and before its Champion, the weakest of the first routes at 226 and Hisui's at 238 to 240; and its two legendary sets at **244**, after the Champion, where the Crown Tundra's are met. The rule batch 3 meant then reads **"within the region's own climb, under the cap of 260"**, which leaves every approved level as it is.

Every name, type, ability, item, move, location, species, stat and sum in this document was checked by a script that only reads `PokemonPlatinumEngine/Data/species.json`, `abilities.json` and `items.json`, with the four earlier batches' names and numbers read from their documents (kept outside the repository; what it checks is listed under "How the numbers were checked", at the end).

## The shortlist: the twelve strongest

If the user wants a small first build of this batch, these are the twelve. Each lifts one of Galar's weakest last stages or species alone, or is among the region's best-loved designs.

| | Ours | From | Typing | Total | Evolves | Why it leads |
|---|---|---|---|---|---|---|
| 1 | **Voltechinus** | Pincurchin | Electric/Water | 435 → 515 | level 229, knowing Zing Zap | the lowest total of Galar's species alone; the sea urchin's spines as lightning rods |
| 2 | **Gorgehog** | Morpeko | Electric/Dark | 436 → 516 | level 231, knowing Aura Wheel | the hungry hamster at 436, its two moods in one body; ours keeps an ability that works for it |
| 3 | **Jarlkatt** | Perrserker | Steel/Fighting | 440 → 520 | level 229, knowing Iron Head | the lowest total of Galar's last stages; the Viking cat becomes the chief |
| 4 | **Reynardine** | Thievul | Dark/Fairy | 455 → 535 | level 227, at night | Galar's fox thief at 455, with a glamour by night |
| 5 | **Larderous** | Greedent | Normal/Ground | 460 → 540 | level 226, holding a Sitrus Berry (used up) | the greedy squirrel at 460 grows a larder under every tree |
| 6 | **Thistlewisp** | Eldegoss | Grass/Flying | 460 → 540 | level 226, by day | the cotton ball at 460 rides the wind on a crown of thistledown |
| 7 | **Phalanxion** | Falinks | Fighting/Steel | 470 → 550 | level 233, with a Falinks in the party | a troop of six at 470; with a second troop beside it, a phalanx |
| 8 | **Henjourner** | Stonjourner | Rock/Psychic | 470 → 550 | level 235, at dusk | the standing stone at 470 becomes a henge at dusk |
| 9 | **Floeguin** | Eiscue | Ice/Water | 470 → 550 | level 235, at an Ice Rock | the ice-headed penguin at 470 rides its own floe; ours keeps an ability that works for it |
| 10 | **Gannetgulp** | Cramorant | Flying/Water | 475 → 555 | level 229, knowing Dive | the much-loved seabird that swallows anything, grown into a gannet |
| 11 | **Paladrake** | Sirfetch'd | Fighting/Steel | 507 → 587 | level 234, after three critical hits in one battle | among Galar's best loved; three critical hits again, and the knight is a paladin |
| 12 | **Urngeist** | Polteageist | Ghost/Fire | 508 → 588 | a Cracked Pot used on it at level 236 or above | among Galar's best loved; the teapot ghost moves into a tea urn |

## What qualifies, and how many

The earlier batches' rule and the same kind of script: a species **with no evolution at all**, or **the last stage of a line of two**; an evolution that starts from a regional form counts as that form's own.

**Galar and Hisui, National Pokédex 810 to 905: 48 species qualify.**

- **21 alone**: Cramorant, Falinks, Pincurchin, Stonjourner, Eiscue, Indeedee, Morpeko, Dracozolt, Arctozolt, Dracovish, Arctovish, Zacian, Zamazenta, Eternatus, Zarude, Regieleki, Regidrago, Glastrier, Spectrier, Calyrex, Enamorus (ten of them legendary or mythical). The four fossils are each alone: Galar's fossils are halves put together, and none becomes anything.
- **27 last of a line of two**: Greedent, Thievul, Eldegoss, Dubwool, Drednaw, Boltund, Flapple, Appletun, Sandaconda, Barraskewda, Toxtricity, Centiskorch, Grapploct, Polteageist, Perrserker, Cursola, Sirfetch'd, Runerigus, Alcremie, Frosmoth, Copperajah, Urshifu, Wyrdeer, Kleavor, Basculegion, Sneasler, Overqwil. Applin has three ends (Flapple and Appletun here; Dipplin goes on to Hydrapple, Paldea's), each judged alone. Perrserker, Cursola, Sirfetch'd and Runerigus end lines that start from Galarian forms (Meowth, Corsola, Farfetch'd, Yamask), and Basculegion, Sneasler and Overqwil from Basculin's white stripes and the Hisuian Sneasel and Qwilfish: by the regional rule each is its form's own line of two, and Kanto's Meowth, Johto's Corsola and Qwilfish, Kanto's Farfetch'd and Unova's Yamask and Basculin keep the evolutions their own batches gave or give them. Alcremie's and Polteageist's lines run through looks (the sweets and the creams, the antique pot), which are no regional forms, and Urshifu's through Kubfu's two scrolls.
- **Deferred to this batch by the earlier four: three, and all qualify.** From Kanto, Scyther's other end, Kleavor (batch 2 named it "Hisui's batch"); from Johto, Stantler's Wyrdeer and the Hisuian Sneasel's Sneasler. Each is designed below. Johto's Girafarig (Farigiraf) and Dunsparce (Dudunsparce) end in Paldea, and Ursaring's line runs on into Ursaluna, the third of its line.
- **The seven first met in Hisui** (Wyrdeer, Kleavor, Ursaluna, Basculegion, Sneasler, Overqwil and Enamorus) are numbered in Galar's generation, and `species.json` gives them generation 8; Hisui is plan 23's optional side region (its decision 9), so they are this batch's (question 2).
- **Not here, last of a line of three (12)**: Rillaboom, Cinderace, Inteleon, Corviknight, Orbeetle, Coalossal, Hatterene, Grimmsnarl, Dragapult, Obstagoon (through the Galarian Zigzagoon and Linoone), Mr. Rime (through Mime Jr. and the Galarian Mr. Mime) and Ursaluna (through Teddiursa and Ursaring). They take 06 · R31's moves.
- **Not here, a Galar species whose line ends in a later region**: Applin's third end (Dipplin into Hydrapple) and Duraludon (into Archaludon), both Paldea's.
- **Not here, regional forms of older species that end their lines in Galar or Hisui**: the Galarian Rapidash, Slowbro, Slowking, Weezing and Darmanitan, the Galarian Stunfisk and birds (alone), and the Hisuian Arcanine, Electrode, Typhlosion, Samurott, Lilligant, Zoroark, Braviary, Goodra, Avalugg and Decidueye. Each is a form of a species another batch judged, and batch 1's question 10 keeps a regional form's own evolution for the user's word; none is proposed (question 7).

42 designs follow; Urshifu, Zacian, Zamazenta, Eternatus, Zarude and Calyrex are left out with their reasons.

## How to read an entry

As batch 4's, with one more line, **Pokédex**, a line for its written entry (plan 08 · P13's two entries are written when the species is built, in our own words and checked by `--check-entries`). **Stats** are the stats the game reads (none of the 48 has a `modern` change in `species.json`). **Levels**: 226 to 240 for the region's own (above), the first routes' species at 226 to 229, the middle of the region at 230 to 236, the fossils and Hisui's at 238 to 240, and the two legendary sets at 244. **Looks** start from our own sculpt of the species. Indeedee's and Basculegion's females are forms of their own in the data (`Indeedee-Female`, `Basculegion-Female`, with numbers of their own), and both evolve into ours, which keeps the female's colours as a look of hers (question 3). The Gigantamax forms of Drednaw, Flapple, Appletun, Sandaconda, Toxtricity, Centiskorch, Alcremie and Copperajah and Falinks's Mega stay theirs; ours get none. **Say**: strong yes (the shortlist), yes, maybe, no.

**The sets.** Galar's are built as sets or not at all: Flapple and Appletun (Applin's two Galar ends, each by its own apple again), the four fossils (each by its own signature move: Bolt Beak or Fishious Rend), Regieleki and Regidrago (by Regigigas, as batch 3's Regis), and Glastrier and Spectrier (by Calyrex). Every condition below is one the engine already runs, five of them for the first time in this table: **`CriticalHits`** (Sirfetch'd's three in one battle), **`LevelAfterDamage`** under the **Stone Arch** (Runerigus's; one of the data's four evolution places), **`LevelAfterMoveUses`** (Wyrdeer's twenty Psyshield Bashes, as Annihilape's Rage Fists), **`LevelAfterRecoil`** (Basculegion's 294) and **`SpinHoldingItem`** (Alcremie's): each of the four forebears' own way again, so its rule needs no explaining in the game.

## The legendaries, the mythicals and Urshifu

Galar and Hisui have ten legendaries (Urshifu among them) and one mythical; the user asked each batch to say which of its region's evolve.

- **Regieleki and Regidrago: maybe, as a pair or not at all**, each at level 244 with a Regigigas in the party, as batch 3's Regirock, Regice and Registeel at 160: Regigigas made them all, and its 670 stays above what it made. +80 to 660. *Default if the user doesn't say*: build them, since batch 3's three are built.
- **Glastrier and Spectrier: maybe, as a pair or not at all**, each at level 244 with a Calyrex in the party: the king's two steeds go further only beside their king. +80 to 660. *Default*: build them.
- **Calyrex: no.** At 500 it is the weakest legendary of the region, but its riders (`Calyrex-Ice`, `Calyrex-Shadow`, 680) are its change, and an evolution of the king alone would leave its steeds a stage behind.
- **Zacian, Zamazenta and Eternatus: no, as a set.** The cover legendaries of Sword and Shield and the one behind their story, each with its further shape already (the Crowned Sword and Shield, Eternamax).
- **Urshifu: no.** A legendary of two styles that are two typings (Single Strike Fighting/Dark, Rapid Strike Fighting/Water), as batch 4's Wormadam: an evolution would lose the style it was trained in, or be two species; its two Gigantamax forms stay.
- **Zarude: no.** The region's one mythical stands outside any stage, as Mew, Celebi and the five of Sinnoh.
- **Enamorus: maybe, and only with the Unova batch's forces of nature** (question 2): the fourth of Tornadus, Thundurus and Landorus, which Unova's batch judges. If they are built, Enamorus is built beside them, at level 244 with a Landorus in the party (Landorus's land is what Enamorus's spring brings to life), +80 to 660. *Default*: as the Unova batch's answer.

## The table: every qualifying species

The user may strike rows here before reading the designs. "No." is our number; "Total" is the stats the game reads now, and ours.

| # | Was | Ours | No. | Typing (was → ours) | Total | Evolves | Say |
|---|---|---|---|---|---|---|---|
| 1 | Greedent (820) | Larderous | 2820 | Normal → **Normal/Ground** | 460 → 540 | level 226, holding a Sitrus Berry (used up) | strong yes: a total of 460 and the squirrel that stuffs its cheeks; past 200 it digs a larder under every tree it passes |
| 2 | Thievul (828) | Reynardine | 2828 | Dark → **Dark/Fairy** | 455 → 535 | level 227, at night | strong yes: a total of 455, Galar's fox thief; past 200 it steals by night with a glamour no one sees through |
| 3 | Eldegoss (830) | Thistlewisp | 2830 | Grass → **Grass/Flying** | 460 → 540 | level 226, by day | strong yes: a total of 460; the cotton ball grows a crown of thistledown and rides the wind |
| 4 | Dubwool (832) | Ramsworth | 2832 | Normal → **Normal/Fighting** | 490 → 570 | level 227, knowing Cotton Guard | yes: the sheep's horns grown into a ram's curl; past 200 it leads the flock and butts down stone walls |
| 5 | Drednaw (834) | Juggernaw | 2834 | Water/Rock, kept | 485 → 565 | level 228, knowing Jaw Lock | yes: the snapping turtle grown into a dreadnought: a shell like a warship's and a jaw that never lets go |
| 6 | Boltund (836) | Voltcourser | 2836 | Electric, kept | 490 → 570 | level 228, by day | yes: the dog that runs for joy grown into a courser that outruns lightning |
| 7 | Flapple (841) | Pippinwyrm | 2841 | Grass/Dragon, kept | 485 → 565 | a Tart Apple used on it at level 230 or above | yes: with Appletun's, Applin's two Galar ends as a set: the apple wyrm guards an orchard |
| 8 | Appletun (842) | Crumbledrake | 2842 | Grass/Dragon, kept | 485 → 565 | a Sweet Apple used on it at level 230 or above | yes: Flapple's pair: the apple pie grown into a crumble that bakes in the sun |
| 9 | Sandaconda (844) | Duneconda | 2844 | Ground, kept | 510 → 590 | level 231, knowing Coil | yes: the snake that stores sand in its body grows long enough to wrap a dune |
| 10 | Cramorant (845) | Gannetgulp | 2845 | Flying/Water, kept | 475 → 555 | level 229, knowing Dive | strong yes: the much-loved seabird that swallows whatever it meets; Gulp Missile works for Cramorant alone, so ours swallows and brings it up again |
| 11 | Barraskewda (847) | Rapierfin | 2847 | Water → **Water/Steel** | 490 → 570 | level 230, in the rain | yes: the skewer of a fish grown into a rapier of steel; it hunts fastest in the rain |
| 12 | Toxtricity (849) | Amplipunk | 2849 | Electric/Poison, kept | 502 → 582 | level 232, knowing Overdrive | yes: the punk lizard's chest is a guitar's strings; past 200 it carries the whole stage |
| 13 | Centiskorch (851) | Furnapede | 2851 | Fire/Bug, kept | 525 → 600 | level 232, holding a Heat Rock (used up) | maybe: a total of 525 already; the radiator centipede grown into a furnace (to 600, +75) |
| 14 | Grapploct (853) | Kraketsu | 2853 | Fighting → **Fighting/Water** | 480 → 560 | level 233, knowing Octolock | yes: Galar's octopus is all Fighting and no sea; ours takes the sea back, with its grips |
| 15 | Polteageist (855) | Urngeist | 2855 | Ghost → **Ghost/Fire** | 508 → 588 | a Cracked Pot used on it at level 236 or above | strong yes: the ghost in a teapot moves into a tea urn that never goes cold |
| 16 | Perrserker (863) | Jarlkatt | 2863 | Steel → **Steel/Fighting** | 440 → 520 | level 229, knowing Iron Head | strong yes: a total of 440, the lowest of Galar's last stages; the Viking cat becomes the chief of its war band |
| 17 | Cursola (864) | Wraithreef | 2864 | Ghost → **Ghost/Water** | 510 → 590 | level 234, at night | yes: the ghost of a bleached coral grows into the ghost of a whole reef |
| 18 | Sirfetch'd (865) | Paladrake | 2865 | Fighting → **Fighting/Steel** | 507 → 587 | level 234, after three critical hits in one battle | strong yes: among Galar's best loved; three critical hits in one battle, its forebear's own way, make the knight a paladin |
| 19 | Runerigus (867) | Runedolmen | 2867 | Ground/Ghost, kept | 483 → 563 | level 234, under the Stone Arch after losing 49 HP or more without fainting | yes: the cursed painting in stone grows into a dolmen, under the same Stone Arch, by its forebear's own way |
| 20 | Alcremie (869) | Patisserene | 2869 | Fairy, kept | 495 → 575 | level 232, spun round holding a Love Sweet (used up) | maybe: the cream Pokémon grown into a tiered cake; its sixty-three looks would become one (question 4) |
| 21 | Falinks (870) | Phalanxion | 2870 | Fighting → **Fighting/Steel** | 470 → 550 | level 233, with a Falinks in the party | strong yes: a total of 470 and a troop of six; with a second Falinks beside it the two troops become one phalanx |
| 22 | Pincurchin (871) | Voltechinus | 2871 | Electric → **Electric/Water** | 435 → 515 | level 229, knowing Zing Zap | strong yes: a total of 435, the lowest of Galar's species alone; the sea urchin grows spines like lightning rods |
| 23 | Frosmoth (873) | Auroramoth | 2873 | Ice/Bug, kept | 475 → 555 | level 235, with friendship 220 or more, at night | yes: Snom's own way again, friendship at night: the frost moth whose wings hold the northern lights |
| 24 | Stonjourner (874) | Henjourner | 2874 | Rock → **Rock/Psychic** | 470 → 550 | level 235, at dusk | strong yes: a total of 470; the standing stone becomes a whole henge, at dusk, when the sun sets between its stones |
| 25 | Eiscue (875) | Floeguin | 2875 | Ice → **Ice/Water** | 470 → 550 | level 235, at an Ice Rock | strong yes: a total of 470; the penguin with an ice cube for a head grows into one that rides a floe. Ice Face works for Eiscue alone, so ours slides |
| 26 | Indeedee (876) | Majordomee | 2876 | Psychic/Normal, kept | 475 → 555 | level 236, with friendship 220 or more, by day | yes: the Pokémon that serves grows into the head of a household; the female, whose own form it is, keeps her look (question 3) |
| 27 | Morpeko (877) | Gorgehog | 2877 | Electric/Dark, kept | 436 → 516 | level 231, knowing Aura Wheel | strong yes: a total of 436; the two-faced hamster that is always hungry grows bigger cheeks. Hunger Switch works for Morpeko alone, so ours stuffs its cheeks |
| 28 | Copperajah (879) | Verdigrand | 2879 | Steel → **Steel/Ground** | 500 → 580 | level 236, in the rain | yes: Copperajah's skin turns green with age; in the rain it grows into an ancient elephant of green copper |
| 29 | Dracozolt (880) | Fulgurex | 2880 | Electric/Dragon, kept | 505 → 585 | level 238, knowing Bolt Beak | yes: with the other three of the Galar fossils as a set: a chimera put together wrongly grows the body it was missing |
| 30 | Arctozolt (881) | Glacivolt | 2881 | Electric/Ice, kept | 505 → 585 | level 238, knowing Bolt Beak | yes: with the Galar fossils: the bird and the plesiosaur grow into one, the ice no longer making it shiver |
| 31 | Dracovish (882) | Abyssaurus | 2882 | Water/Dragon, kept | 505 → 585 | level 238, knowing Fishious Rend | yes: with the Galar fossils: the fish's head on the drake's legs grows a jaw and gills it can use on land |
| 32 | Arctovish (883) | Glacichthys | 2883 | Water/Ice, kept | 505 → 585 | level 238, knowing Fishious Rend | yes: with the Galar fossils: the fish's head the right way up on the icy body, at last able to eat |
| – | Zacian (888) | none | – | Fairy | 660 | – | no: a cover legendary with its Crowned Sword already its further shape |
| – | Zamazenta (889) | none | – | Fighting | 660 | – | no: a cover legendary with its Crowned Shield already its further shape |
| – | Eternatus (890) | none | – | Poison/Dragon | 690 | – | no: the region's story's legendary, with its Eternamax form |
| – | Urshifu (892) | none | – | Fighting/Dark | 550 | – | no: two styles of two typings (Single and Rapid Strike), as Wormadam's cloaks; ours have no forms |
| – | Zarude (893) | none | – | Dark/Grass | 600 | – | no: a mythical, as Mew and Celebi |
| 33 | Regieleki (894) | Regifulgur | 2894 | Electric, kept | 580 → 660 | level 244, with a Regigigas in the party | maybe: with Regidrago as a pair or not at all, by Regigigas beside them, as batch 3's Regis; +80 to 660 |
| 34 | Regidrago (895) | Regidraconis | 2895 | Dragon, kept | 580 → 660 | level 244, with a Regigigas in the party | maybe: with Regieleki: the dragon's head of crystal grown into the whole dragon it remembers |
| 35 | Glastrier (896) | Hoarsteed | 2896 | Ice, kept | 580 → 660 | level 244, with a Calyrex in the party | maybe: with Spectrier as a pair or not at all, each by its king Calyrex beside it; +80 to 660 |
| 36 | Spectrier (897) | Shadesteed | 2897 | Ghost, kept | 580 → 660 | level 244, with a Calyrex in the party | maybe: Glastrier's pair: the swift horse of shadow grown into a steed no light can catch |
| – | Calyrex (898) | none | – | Psychic/Grass | 500 | – | no: its riders are its change, and its steeds would be left a stage behind |
| 37 | Wyrdeer (899) | Wyrdstag | 2899 | Normal/Psychic, kept | 525 → 600 | level 238, after using Psyshield Bash 20 times | maybe: from Hisui: Stantler's own way again, twenty Psyshield Bashes; 525 already, so +75 to the ceiling |
| 38 | Kleavor (900) | Halberdon | 2900 | Bug/Rock, kept | 500 → 580 | a Black Augurite used on it at level 238 or above | yes: from Hisui: Scyther's other end, by the same stone again; its axes grow into halberds |
| 39 | Basculegion (902) | Shoalwraith | 2902 | Water/Ghost, kept | 530 → 600 | level 238, after losing 294 HP or more to recoil without fainting | maybe: from Hisui: its forebear's own way again, 294 HP lost to recoil; 530 already, so +70 to the ceiling, and its female's own form (question 3) |
| 40 | Sneasler (903) | Clifflasher | 2903 | Fighting/Poison, kept | 510 → 590 | level 240, holding a Razor Claw by day (used up) | yes: from Hisui: the Hisuian Sneasel's own claw by day once more; the climber of cliffs grows claws that lash |
| 41 | Overqwil (904) | Spinarmada | 2904 | Dark/Poison, kept | 510 → 590 | level 238, knowing Barb Barrage | yes: from Hisui: the spiny pufferfish grown into an armada of spines; Johto's Qwilfish had its own (Fugumace) |
| 42 | Enamorus (905) | Enamorata | 2905 | Fairy/Flying, kept | 580 → 660 | level 244, with a Landorus in the party | maybe: from Hisui, the fourth of the forces of nature: built only if the Unova batch builds the other three (question 2); +80 to 660 |

In all: 12 strong yes, 21 yes, 9 maybe (the two legendary pairs and Enamorus among them) and 6 no.

## The designs

In National Pokédex order. Each entry: the typing and why, the category word, number, size, the stats (the species' as the game reads them → ours) with where the rise goes, the abilities, the evolution with the row it would be in `species.json`, the look and a line for its Pokédex entry.

### 1. Larderous, from Greedent (820): strong yes

- **Typing**: Normal → **Normal/Ground**. *Alternative*: Normal/Grass.
- **The Larder Pokémon**, No. 2820. 0.9 m, 11.0 kg (Greedent 0.6 m, 6 kg).
- **Stats**: 120 / 95 / 95 / 55 / 75 / 20 = 460 → **130 / 115 / 110 / 55 / 90 / 40 = 540** (+10 HP +20 Atk +15 Def +15 SpD +20 Spe).
- **Abilities**: Cheek Pouch, hidden Gluttony (was Cheek Pouch, hidden Gluttony).
- **Evolution**: Greedent at level 226, holding a Sitrus Berry (used up). Data: `{"targetSpecies":"Larderous","method":"LevelHoldingItem","level":226,"item":"Sitrus Berry"}`.
- **Look**: from our brown Greedent with its huge tail and bulging cheeks: rounder and broader, the curled tail grown into a second, fuller tail that holds berries like a basket, two long front teeth, a burrower's claws on its forepaws and soil on its belly fur. Brown fur, cream belly, red berries (Glow, none). **New feature**: the basket of a second tail.
- **Pokédex**: It buries berries under every tree it passes and never forgets one of them; a wood where it lives is a wood of berry bushes a few years later.

### 2. Reynardine, from Thievul (828): strong yes

- **Typing**: Dark → **Dark/Fairy**. *Alternative*: Dark/Psychic.
- **The Trickster Pokémon**, No. 2828. 1.5 m, 26.0 kg (Thievul 1.2 m, 19.9 kg).
- **Stats**: 70 / 58 / 58 / 87 / 92 / 90 = 455 → **80 / 58 / 73 / 112 / 107 / 105 = 535** (+10 HP +15 Def +25 SpA +15 SpD +15 Spe).
- **Abilities**: Run Away, Unburden, hidden Stakeout (was Run Away, Unburden, hidden Stakeout).
- **Evolution**: Thievul at level 227, at night. Data: `{"targetSpecies":"Reynardine","method":"LevelNight","level":227}`.
- **Look**: from our red-brown Thievul with its thin moustache and long tail: longer in the leg, the moustache curled at both ends, the tail's tip split into three like a fox of the old stories, and a pale violet mask of fur round the eyes that glimmers (Glow, violet) when it casts its glamour. **New feature**: the three-tipped tail.
- **Pokédex**: It leaves a trail of false footprints of light behind it. People who follow them by night wake in the morning in a field miles from home.

### 3. Thistlewisp, from Eldegoss (830): strong yes

- **Typing**: Grass → **Grass/Flying**. *Alternative*: Grass/Fairy.
- **The Thistledown Pokémon**, No. 2830. 0.8 m, 3.5 kg (Eldegoss 0.5 m, 2.5 kg).
- **Stats**: 60 / 50 / 90 / 80 / 120 / 60 = 460 → **75 / 50 / 100 / 100 / 130 / 85 = 540** (+15 HP +10 Def +20 SpA +10 SpD +25 Spe).
- **Abilities**: Cotton Down, Regenerator, hidden Effect Spore (was Cotton Down, Regenerator, hidden Effect Spore).
- **Evolution**: Eldegoss at level 226, by day. Data: `{"targetSpecies":"Thistlewisp","method":"LevelDay","level":226}`.
- **Look**: from our green Eldegoss with its round white cotton head: its cotton grown into a crown of thistledown in long spokes, each tipped with a seed, its leaf-arms longer and spread like wings, its small body hanging under the crown. Green body, white down, brown seeds. **New feature**: the spokes of thistledown.
- **Pokédex**: On a dry summer day it lets go of its crown and drifts across whole counties; wherever a seed lands, a meadow of soft white flowers follows.

### 4. Ramsworth, from Dubwool (832): yes

- **Typing**: Normal → **Normal/Fighting**. *Alternative*: Normal/Rock.
- **The Ram Pokémon**, No. 2832. 1.6 m, 70.0 kg (Dubwool 1.3 m, 43 kg).
- **Stats**: 72 / 80 / 100 / 60 / 90 / 88 = 490 → **87 / 105 / 115 / 60 / 100 / 103 = 570** (+15 HP +25 Atk +15 Def +10 SpD +15 Spe).
- **Abilities**: Fluffy, Steadfast, hidden Bulletproof (was Fluffy, Steadfast, hidden Bulletproof).
- **Evolution**: Dubwool at level 227, knowing Cotton Guard. Data: `{"targetSpecies":"Ramsworth","method":"LevelKnowsMove","level":227,"move":"Cotton Guard"}`.
- **Look**: from our white Dubwool with its black face and curled horns: heavier in the shoulder, the horns grown into two full curls of grey-gold that meet over its brow like a helmet, its fleece longer and gathered in thick ropes, black hooves and legs. White fleece, black face, gold-grey horns. **New feature**: the double curl of horn.
- **Pokédex**: Its fleece is so thick that a charge at full speed leaves it unhurt. Shepherds of the moors say one is worth a whole team of sheepdogs.

### 5. Juggernaw, from Drednaw (834): yes

- **Typing**: Water/Rock, kept. *Alternative*: Water/Steel.
- **The Ironclad Pokémon**, No. 2834. 1.4 m, 210.0 kg (Drednaw 1 m, 115.5 kg).
- **Stats**: 90 / 115 / 90 / 48 / 68 / 74 = 485 → **105 / 135 / 105 / 48 / 88 / 84 = 565** (+15 HP +20 Atk +15 Def +20 SpD +10 Spe).
- **Abilities**: Strong Jaw, Shell Armor, hidden Swift Swim (was Strong Jaw, Shell Armor, hidden Swift Swim).
- **Evolution**: Drednaw at level 228, knowing Jaw Lock. Data: `{"targetSpecies":"Juggernaw","method":"LevelKnowsMove","level":228,"move":"Jaw Lock"}`.
- **Look**: from our green and brown Drednaw with its rocky shell and horned head: larger, the shell's plates grown into the armour of an ironclad with a ridge of rivets along its keel, the horn on its head a ram's prow, its neck longer and its jaw heavier. Green shell, brown plates, pale horn. **New feature**: the ram's prow of a horn.
- **Pokédex**: It lies in wait at the mouths of rivers with only its prow above the water. Old boatmen steer wide of any rock that has a tooth.

### 6. Voltcourser, from Boltund (836): yes

- **Typing**: Electric, kept. *Alternative*: Electric/Fighting.
- **The Courser Pokémon**, No. 2836. 1.3 m, 45.0 kg (Boltund 1 m, 34 kg).
- **Stats**: 69 / 90 / 60 / 90 / 60 / 121 = 490 → **79 / 110 / 80 / 90 / 80 / 131 = 570** (+10 HP +20 Atk +20 Def +20 SpD +10 Spe).
- **Abilities**: Strong Jaw, hidden Competitive (was Strong Jaw, hidden Competitive).
- **Evolution**: Boltund at level 228, by day. Data: `{"targetSpecies":"Voltcourser","method":"LevelDay","level":228}`.
- **Look**: from our yellow and black Boltund with its legs of electricity: longer and leaner, a greyhound's deep chest, the electric bands on its legs grown into three on each leg (Glow, yellow), its tail a lightning bolt in three steps and its ears swept back. **New feature**: the three bands of current on each leg.
- **Pokédex**: It runs to the horizon and back before the thunder of its first step has reached the ears of those who watched it start.

### 7. Pippinwyrm, from Flapple (841): yes

- **Typing**: Grass/Dragon, kept. *Alternative*: Grass/Flying.
- **The Orchard Wyrm Pokémon**, No. 2841. 0.6 m, 4.0 kg (Flapple 0.3 m, 1 kg).
- **Stats**: 70 / 110 / 80 / 95 / 60 / 70 = 485 → **80 / 130 / 85 / 95 / 80 / 95 = 565** (+10 HP +20 Atk +5 Def +20 SpD +25 Spe).
- **Abilities**: Ripen, Gluttony, hidden Hustle (was Ripen, Gluttony, hidden Hustle).
- **Evolution**: Flapple at a Tart Apple used on it at level 230 or above. Data: `{"targetSpecies":"Pippinwyrm","method":"UseItem","level":230,"item":"Tart Apple"}`.
- **Look**: from our Flapple with its red apple wings and green body: twice as long, a wyrm's body coiled round a red apple of its own that it carries like a shield, wings of apple skin cut into feathers, a stalk and one leaf on its head. Green body, red wings and apple, brown stalk. **New feature**: the apple it carries like a shield.
- **Pokédex**: It nests in old orchards and guards one apple for its whole life. A tree it chooses gives fruit so sour no Pokémon will touch it.

### 8. Crumbledrake, from Appletun (842): yes

- **Typing**: Grass/Dragon, kept. *Alternative*: Grass/Fairy.
- **The Crumble Pokémon**, No. 2842. 0.9 m, 30.0 kg (Appletun 0.4 m, 13 kg).
- **Stats**: 110 / 85 / 80 / 100 / 80 / 30 = 485 → **120 / 85 / 100 / 120 / 100 / 40 = 565** (+10 HP +20 Def +20 SpA +20 SpD +10 Spe).
- **Abilities**: Ripen, Gluttony, hidden Thick Fat (was Ripen, Gluttony, hidden Thick Fat).
- **Evolution**: Appletun at a Sweet Apple used on it at level 230 or above. Data: `{"targetSpecies":"Crumbledrake","method":"UseItem","level":230,"item":"Sweet Apple"}`.
- **Look**: from our Appletun with its apple shell and long neck: larger, the shell's top crusted into a crumble of golden lumps, the red apple shell cracked to show baked apple beneath (Glow, faint amber), a short tail of pastry and a sweeter, sleepier face. Red shell, gold crumble, cream neck. **New feature**: the crust of crumble on its back.
- **Pokédex**: Children of the dales leave a bowl of cream on the step when its smell drifts over the hills, so that it will stay the winter.

### 9. Duneconda, from Sandaconda (844): yes

- **Typing**: Ground, kept. *Alternative*: Ground/Rock.
- **The Dune Snake Pokémon**, No. 2844. 5.0 m, 95.0 kg (Sandaconda 3.8 m, 65.5 kg).
- **Stats**: 72 / 107 / 125 / 65 / 70 / 71 = 510 → **87 / 122 / 130 / 65 / 95 / 91 = 590** (+15 HP +15 Atk +5 Def +25 SpD +20 Spe).
- **Abilities**: Sand Spit, Shed Skin, hidden Sand Veil (was Sand Spit, Shed Skin, hidden Sand Veil).
- **Evolution**: Sandaconda at level 231, knowing Coil. Data: `{"targetSpecies":"Duneconda","method":"LevelKnowsMove","level":231,"move":"Coil"}`.
- **Look**: from our Sandaconda with its coiled body and sack of sand: longer, two sacks of sand along its body instead of one, the rim of its mouth a ring of orange scales, its tail's tip a rattle of hardened sand. Tan body, orange rim, brown diamond markings. **New feature**: the second sack of sand.
- **Pokédex**: A desert it lives in moves a little every night. It shifts whole dunes when it coils, and the paths of travellers go with them.

### 10. Gannetgulp, from Cramorant (845): strong yes

- **Typing**: Flying/Water, kept. *Alternative*: Water/Steel.
- **The Gannet Pokémon**, No. 2845. 1.2 m, 30.0 kg (Cramorant 0.8 m, 18 kg).
- **Stats**: 70 / 85 / 55 / 85 / 95 / 85 = 475 → **85 / 100 / 80 / 95 / 105 / 90 = 555** (+15 HP +15 Atk +25 Def +10 SpA +10 SpD +5 Spe).
- **Abilities**: **Cud Chew** (was Gulp Missile); the old one works for its own species alone (question 5).
- **Evolution**: Cramorant at level 229, knowing Dive. Data: `{"targetSpecies":"Gannetgulp","method":"LevelKnowsMove","level":229,"move":"Dive"}`.
- **Look**: from our dark blue Cramorant with its yellow throat pouch and hooked bill: larger, a gannet's sharp white head and a pouch twice as big that often has a tail sticking out of it, its wing tips black and long and its blank stare kept. Dark blue body, white head, yellow pouch. **New feature**: the bigger pouch with a tail in it.
- **Pokédex**: It dives from the clouds and comes up with whatever it hit first. It has been seen trying to swallow a rowing boat.

### 11. Rapierfin, from Barraskewda (847): yes

- **Typing**: Water → **Water/Steel**. *Alternative*: Water/Dark.
- **The Rapier Pokémon**, No. 2847. 1.8 m, 50.0 kg (Barraskewda 1.3 m, 30 kg).
- **Stats**: 61 / 123 / 60 / 60 / 50 / 136 = 490 → **76 / 138 / 80 / 60 / 70 / 146 = 570** (+15 HP +15 Atk +20 Def +20 SpD +10 Spe).
- **Abilities**: Swift Swim, hidden Propeller Tail (was Swift Swim, hidden Propeller Tail).
- **Evolution**: Barraskewda at level 230, in the rain. Data: `{"targetSpecies":"Rapierfin","method":"LevelInRain","level":230}`.
- **Look**: from our brown and yellow Barraskewda: longer and thinner, its jaw grown into a rapier's blade of bright steel (Metal), a basket-hilt of fins behind its head and its tail's propeller of four blades. Brown back, yellow belly, steel blade. **New feature**: the rapier of a jaw.
- **Pokédex**: It strikes faster than the eye can follow; fishermen find only a clean hole through their nets where a school of fish was.

### 12. Amplipunk, from Toxtricity (849): yes

- **Typing**: Electric/Poison, kept. *Alternative*: Electric/Fighting.
- **The Headliner Pokémon**, No. 2849. 1.9 m, 55.0 kg (Toxtricity 1.6 m, 40 kg).
- **Stats**: 75 / 98 / 70 / 114 / 70 / 75 = 502 → **85 / 103 / 85 / 129 / 85 / 95 = 582** (+10 HP +5 Atk +15 Def +15 SpA +15 SpD +20 Spe).
- **Abilities**: Punk Rock, Plus, hidden Technician (was Punk Rock, Plus, hidden Technician).
- **Evolution**: Toxtricity at level 232, knowing Overdrive. Data: `{"targetSpecies":"Amplipunk","method":"LevelKnowsMove","level":232,"move":"Overdrive"}`.
- **Look**: from our purple Toxtricity with its yellow spikes and the strings of its chest: taller, its crest grown into a mohawk of five spikes, the chest's organ a full row of glowing strings with a second row on its back (Glow, yellow or blue), a stand of three amplifier-like spines on its tail. Purple body, yellow spikes; the Low Key's blue on whichever came from one (question 4). **New feature**: the second row of strings on its back.
- **Pokédex**: When it plays, the air sparks and smells of chemicals. Its crowds are made of Pokémon that came from miles away to hear it.

### 13. Furnapede, from Centiskorch (851): maybe

- **Typing**: Fire/Bug, kept. *Alternative*: Fire/Steel.
- **The Furnace Pokémon**, No. 2851. 4.0 m, 170.0 kg (Centiskorch 3 m, 120 kg).
- **Stats**: 100 / 115 / 65 / 90 / 90 / 65 = 525 → **105 / 125 / 90 / 95 / 105 / 80 = 600** (+5 HP +10 Atk +25 Def +5 SpA +15 SpD +15 Spe): the rise is +75, to the 600 ceiling.
- **Abilities**: Flash Fire, White Smoke, hidden Flame Body (was Flash Fire, White Smoke, hidden Flame Body).
- **Evolution**: Centiskorch at level 232, holding a Heat Rock (used up). Data: `{"targetSpecies":"Furnapede","method":"LevelHoldingItem","level":232,"item":"Heat Rock"}`.
- **Look**: from our red and orange Centiskorch with its glowing segments: longer, each segment's grille a furnace's door with fire behind it (Glow, orange), two more pairs of whip-like antennae and its head's jaws heavier and black with soot. Red body, black jaws, orange fire. **New feature**: the furnace doors on each segment.
- **Pokédex**: Its body is hot enough to melt iron. Smiths of the north once kept one coiled round their forge through the whole winter.

### 14. Kraketsu, from Grapploct (853): yes

- **Typing**: Fighting → **Fighting/Water**. *Alternative*: Fighting/Dark.
- **The Grapple Master Pokémon**, No. 2853. 2.0 m, 60.0 kg (Grapploct 1.6 m, 39 kg).
- **Stats**: 80 / 118 / 90 / 70 / 80 / 42 = 480 → **95 / 133 / 105 / 70 / 95 / 62 = 560** (+15 HP +15 Atk +15 Def +15 SpD +20 Spe).
- **Abilities**: Limber, hidden Technician (was Limber, hidden Technician).
- **Evolution**: Grapploct at level 233, knowing Octolock. Data: `{"targetSpecies":"Kraketsu","method":"LevelKnowsMove","level":233,"move":"Octolock"}`.
- **Look**: from our blue Grapploct with its arms like a wrestler's: larger, its eight arms ending in fists with knuckles of pale suckers, a belt of dark blue round its middle and its head's eyes narrower. Blue body, cream suckers, dark belt. **New feature**: the belt.
- **Pokédex**: It trains on the sea floor, throwing boulders. A diver who sees it at practice should not stop to watch: it takes watching as a challenge.

### 15. Urngeist, from Polteageist (855): strong yes

- **Typing**: Ghost → **Ghost/Fire**. *Alternative*: Ghost/Water.
- **The Tea Urn Pokémon**, No. 2855. 0.5 m, 4.0 kg (Polteageist 0.2 m, 0.4 kg).
- **Stats**: 60 / 65 / 65 / 134 / 114 / 70 = 508 → **80 / 65 / 90 / 144 / 124 / 85 = 588** (+20 HP +25 Def +10 SpA +10 SpD +15 Spe).
- **Abilities**: Weak Armor, hidden Cursed Body (was Weak Armor, hidden Cursed Body).
- **Evolution**: Polteageist at a Cracked Pot used on it at level 236 or above. Data: `{"targetSpecies":"Urngeist","method":"UseItem","level":236,"item":"Cracked Pot"}`.
- **Look**: from our Polteageist in its purple teapot: a tall tea urn of pale china with a little brass tap, the tea's ghost rising out of its lid as a curl of steam with a face, two cups that float round it, and the chip on its spout kept. Pale china, purple ghost tea, brass tap; the antique's mark under its foot where it came from one (question 4). **New feature**: the tap.
- **Pokédex**: Its tea is always hot and always too strong. A house that pours a cup from it hears china clink in empty rooms for a week.

### 16. Jarlkatt, from Perrserker (863): strong yes

- **Typing**: Steel → **Steel/Fighting**. *Alternative*: Steel/Ice.
- **The Jarl Pokémon**, No. 2863. 1.1 m, 50.0 kg (Perrserker 0.8 m, 28 kg).
- **Stats**: 70 / 110 / 100 / 50 / 60 / 50 = 440 → **90 / 130 / 110 / 50 / 80 / 60 = 520** (+20 HP +20 Atk +10 Def +20 SpD +10 Spe).
- **Abilities**: Battle Armor, Tough Claws, hidden Steely Spirit (was Battle Armor, Tough Claws, hidden Steely Spirit).
- **Evolution**: Perrserker at level 229, knowing Iron Head. Data: `{"targetSpecies":"Jarlkatt","method":"LevelKnowsMove","level":229,"move":"Iron Head"}`.
- **Look**: from our brown Perrserker with its iron helmet-fur and beard: larger, the helmet grown into a horned iron helm, a round shield of iron on one forearm, its beard braided in two and a cloak of brown fur. Brown fur, dark iron helm and shield, cream claws. **New feature**: the round shield.
- **Pokédex**: It rules a band of Perrserker from a hall of driftwood. Its helm is so heavy that it never takes it off, even to sleep.

### 17. Wraithreef, from Cursola (864): yes

- **Typing**: Ghost → **Ghost/Water**. *Alternative*: Ghost/Rock.
- **The Dead Reef Pokémon**, No. 2864. 1.4 m, 2.0 kg (Cursola 1 m, 0.4 kg).
- **Stats**: 60 / 95 / 50 / 145 / 130 / 30 = 510 → **85 / 95 / 80 / 150 / 135 / 45 = 590** (+25 HP +30 Def +5 SpA +5 SpD +15 Spe).
- **Abilities**: Weak Armor, hidden Perish Body (was Weak Armor, hidden Perish Body).
- **Evolution**: Cursola at level 234, at night. Data: `{"targetSpecies":"Wraithreef","method":"LevelNight","level":234}`.
- **Look**: from our white Cursola with its ghostly core and branches: larger and wider, a crown of bleached branches like antlers spreading round a core of three ghostly spheres (Glow, pale violet), streamers of grey weed hanging from them and the base shattered. White branches, violet core, grey weed. **New feature**: the three spheres of its core.
- **Pokédex**: Where it settles, warm seas grow cold. Divers say a reef it haunts still whispers of the colours it lost.

### 18. Paladrake, from Sirfetch'd (865): strong yes

- **Typing**: Fighting → **Fighting/Steel**. *Alternative*: Fighting/Flying.
- **The Paladin Duck Pokémon**, No. 2865. 1.0 m, 140.0 kg (Sirfetch'd 0.8 m, 117 kg).
- **Stats**: 62 / 135 / 95 / 68 / 82 / 65 = 507 → **82 / 140 / 105 / 68 / 97 / 95 = 587** (+20 HP +5 Atk +10 Def +15 SpD +30 Spe).
- **Abilities**: Steadfast, hidden Scrappy (was Steadfast, hidden Scrappy).
- **Evolution**: Sirfetch'd at level 234, after three critical hits in one battle. Data: `{"targetSpecies":"Paladrake","method":"CriticalHits","level":234,"value":3}`.
- **Look**: from our white Sirfetch'd with its leek lance and leaf shield: a breastplate of pale steel, the leek grown into a lance with a steel point, the shield a heater shield of green leaves bound in steel, a crest of white feathers on a helmet and its eyebrows kept. White feathers, green leek and shield, pale steel. **New feature**: the steel point of its lance.
- **Pokédex**: It never runs from a battle and never strikes one who has fallen. Children of the south still play at jousting with leeks after it.

### 19. Runedolmen, from Runerigus (867): yes

- **Typing**: Ground/Ghost, kept. *Alternative*: Rock/Ghost.
- **The Dolmen Pokémon**, No. 2867. 2.2 m, 120.0 kg (Runerigus 1.6 m, 66.6 kg).
- **Stats**: 58 / 95 / 145 / 50 / 105 / 30 = 483 → **88 / 105 / 150 / 70 / 115 / 35 = 563** (+30 HP +10 Atk +5 Def +20 SpA +10 SpD +5 Spe).
- **Abilities**: Wandering Spirit (was Wandering Spirit).
- **Evolution**: Runerigus at level 234, under the Stone Arch after losing 49 HP or more without fainting. Data: `{"targetSpecies":"Runedolmen","method":"LevelAfterDamage","level":234,"location":"Stone Arch","value":49}`.
- **Look**: from our grey Runerigus with its slab of carved rock: two slabs leaning together like a dolmen, the shadow body under them with its yellow eyes, more runes cut along both slabs (Glow, red at the runes) and a capstone over them. Grey stone, black shadow, red runes. **New feature**: the capstone.
- **Pokédex**: Its runes are a curse no one has been able to read. People who try to trace them dream for nights of a long road in the dust.

### 20. Patisserene, from Alcremie (869): maybe

- **Typing**: Fairy, kept. *Alternative*: Fairy/Normal.
- **The Gateau Pokémon**, No. 2869. 0.6 m, 1.5 kg (Alcremie 0.3 m, 0.5 kg).
- **Stats**: 65 / 60 / 75 / 110 / 121 / 64 = 495 → **85 / 60 / 90 / 125 / 131 / 84 = 575** (+20 HP +15 Def +15 SpA +10 SpD +20 Spe).
- **Abilities**: Sweet Veil, hidden Aroma Veil (was Sweet Veil, hidden Aroma Veil).
- **Evolution**: Alcremie at level 232, spun round holding a Love Sweet (used up). Data: `{"targetSpecies":"Patisserene","method":"SpinHoldingItem","level":232,"item":"Love Sweet"}`.
- **Look**: from our white Alcremie with its cream body and berry on top: a cake of three tiers, the lowest its skirt of cream, the middle its body with its small arms, the top its head with a crown of cream swirls and a pink Love Sweet on top, piped hearts round each tier. White cream, pink hearts, red berry. **New feature**: the three tiers.
- **Pokédex**: Whoever tastes its cream is happy for a whole day. At weddings in Galar a cake shaped like it is said to bring good luck.

### 21. Phalanxion, from Falinks (870): strong yes

- **Typing**: Fighting → **Fighting/Steel**. *Alternative*: Fighting/Rock.
- **The Phalanx Pokémon**, No. 2870. 3.6 m, 90.0 kg (Falinks 3 m, 62 kg).
- **Stats**: 65 / 100 / 100 / 70 / 60 / 75 = 470 → **80 / 120 / 115 / 70 / 80 / 85 = 550** (+15 HP +20 Atk +15 Def +20 SpD +10 Spe).
- **Abilities**: Battle Armor, hidden Defiant (was Battle Armor, hidden Defiant).
- **Evolution**: Falinks at level 233, with a Falinks in the party. Data: `{"targetSpecies":"Phalanxion","method":"LevelWithSpeciesInParty","level":233,"species":"Falinks"}`.
- **Look**: from our yellow Falinks, its brass leader and five troopers in a line: seven troopers in two ranks, each with a small round shield of brass, the leader at the front with a tall brass helmet and a crest. Yellow bodies, brass shields and helmet, white eyes. **New feature**: the brass shields.
- **Pokédex**: Its troopers march as one. When it locks its shields, nothing short of a landslide breaks its line.

### 22. Voltechinus, from Pincurchin (871): strong yes

- **Typing**: Electric → **Electric/Water**. *Alternative*: Electric/Rock.
- **The Storm Urchin Pokémon**, No. 2871. 0.6 m, 6.0 kg (Pincurchin 0.3 m, 1 kg).
- **Stats**: 48 / 101 / 95 / 91 / 85 / 15 = 435 → **78 / 106 / 105 / 106 / 95 / 25 = 515** (+30 HP +5 Atk +10 Def +15 SpA +10 SpD +10 Spe).
- **Abilities**: Lightning Rod, hidden Electric Surge (was Lightning Rod, hidden Electric Surge).
- **Evolution**: Pincurchin at level 229, knowing Zing Zap. Data: `{"targetSpecies":"Voltechinus","method":"LevelKnowsMove","level":229,"move":"Zing Zap"}`.
- **Look**: from our purple Pincurchin with its spines and its teeth: rounder, its spines longer and tipped with yellow sparks (Glow, yellow), a ring of shorter spines round its mouth, its teeth kept and a little sea foam round its base. Purple body, black and yellow spines. **New feature**: the sparks at its spines' tips.
- **Pokédex**: In a storm it climbs to the top of the rocks and drinks the lightning. Tide pools near it fizz for days after.

### 23. Auroramoth, from Frosmoth (873): yes

- **Typing**: Ice/Bug, kept. *Alternative*: Ice/Fairy.
- **The Aurora Moth Pokémon**, No. 2873. 1.7 m, 55.0 kg (Frosmoth 1.3 m, 42 kg).
- **Stats**: 70 / 65 / 60 / 125 / 90 / 65 = 475 → **85 / 65 / 80 / 140 / 100 / 85 = 555** (+15 HP +20 Def +15 SpA +10 SpD +20 Spe).
- **Abilities**: Shield Dust, hidden Ice Scales (was Shield Dust, hidden Ice Scales).
- **Evolution**: Frosmoth at level 235, with friendship 220 or more, at night. Data: `{"targetSpecies":"Auroramoth","method":"FriendshipNight","level":235}`.
- **Look**: from our white Frosmoth with its icy wings: wider, the wings grown into two pairs of pale ice with bands of aurora green and violet across them (Glow, green and violet), its antennae feathered with frost and its body's fur longer. White body, pale blue wings, aurora bands. **New feature**: the aurora bands.
- **Pokédex**: On the coldest nights it dances over the snow, and the sky above it lights up green and violet until dawn.

### 24. Henjourner, from Stonjourner (874): strong yes

- **Typing**: Rock → **Rock/Psychic**. *Alternative*: Rock/Fairy.
- **The Henge Pokémon**, No. 2874. 3.0 m, 700.0 kg (Stonjourner 2.5 m, 520 kg).
- **Stats**: 100 / 125 / 135 / 20 / 20 / 70 = 470 → **110 / 135 / 135 / 40 / 60 / 70 = 550** (+10 HP +10 Atk +20 SpA +40 SpD).
- **Abilities**: Power Spot (was Power Spot).
- **Evolution**: Stonjourner at level 235, at dusk. Data: `{"targetSpecies":"Henjourner","method":"LevelDusk","level":235}`.
- **Look**: from our grey Stonjourner, a standing stone with a face: three standing stones in a ring under one lintel, its face on the lintel, the old marks of lichen on its sides, and a pale gold sun disc (Glow, gold) hanging in the gap between its stones. Grey stone, green lichen, gold disc. **New feature**: the sun disc between its stones.
- **Pokédex**: Once a year, at sunset on the longest day, the sun shines through the gap between its stones and it walks for a whole night.

### 25. Floeguin, from Eiscue (875): strong yes

- **Typing**: Ice → **Ice/Water**. *Alternative*: Ice/Steel.
- **The Ice Floe Pokémon**, No. 2875. 1.7 m, 140.0 kg (Eiscue 1.4 m, 89 kg).
- **Stats**: 75 / 80 / 110 / 65 / 90 / 50 = 470 → **90 / 95 / 115 / 75 / 100 / 75 = 550** (+15 HP +15 Atk +5 Def +10 SpA +10 SpD +25 Spe).
- **Abilities**: **Slush Rush** (was Ice Face); the old one works for its own species alone (question 5).
- **Evolution**: Eiscue at level 235, at an Ice Rock. Data: `{"targetSpecies":"Floeguin","method":"LevelAtLocation","level":235,"location":"Ice Rock"}`.
- **Look**: from our blue Eiscue with its block of ice for a head: taller, the ice block grown into a floe that covers its head and back like a sledge, its flippers longer, its yellow crest tufts trailing and a hanging icicle at its chin. Blue body, white belly, pale ice, yellow tufts. **New feature**: the floe on its back.
- **Pokédex**: It rides its own floe out to sea and back. In the far north it carries small Pokémon across the water on its back.

### 26. Majordomee, from Indeedee (876): yes

- **Typing**: Psychic/Normal, kept. *Alternative*: Psychic/Fairy.
- **The Majordomo Pokémon**, No. 2876. 1.2 m, 40.0 kg (Indeedee 0.9 m, 28 kg).
- **Stats**: 60 / 65 / 55 / 105 / 95 / 95 = 475 → **80 / 65 / 80 / 120 / 105 / 105 = 555** (+20 HP +25 Def +15 SpA +10 SpD +10 Spe).
- **Abilities**: Inner Focus, Synchronize, hidden Psychic Surge (was Inner Focus, Synchronize, hidden Psychic Surge).
- **Evolution**: Indeedee at level 236, with friendship 220 or more, by day. Data: `{"targetSpecies":"Majordomee","method":"FriendshipDay","level":236}`.
- **Look**: from our purple Indeedee with its horn-like feelers and coat-tail: taller, the coat-tail grown into a full tailcoat of purple and black, the feelers longer and curled at the tips, a white collar of fur and a small gold chain at its waist. The female's colours, white where his are black, are kept. **New feature**: the tailcoat.
- **Pokédex**: It knows what its trainer wants before they do. A house it serves never runs out of tea and never loses a key.

### 27. Gorgehog, from Morpeko (877): strong yes

- **Typing**: Electric/Dark, kept. *Alternative*: Electric/Normal.
- **The Gorging Pokémon**, No. 2877. 0.6 m, 8.0 kg (Morpeko 0.3 m, 3 kg).
- **Stats**: 58 / 95 / 58 / 70 / 58 / 97 = 436 → **78 / 110 / 73 / 80 / 73 / 102 = 516** (+20 HP +15 Atk +15 Def +10 SpA +15 SpD +5 Spe).
- **Abilities**: **Cheek Pouch** (was Hunger Switch); the old one works for its own species alone (question 5).
- **Evolution**: Morpeko at level 231, knowing Aura Wheel. Data: `{"targetSpecies":"Gorgehog","method":"LevelKnowsMove","level":231,"move":"Aura Wheel"}`.
- **Look**: from our yellow and black Morpeko: rounder, two great cheek pouches that spark when full, a striped tail like a spring, its fur half yellow and half dark purple down the middle (its two moods in one body) and the seed pouch at its belt kept. Yellow and purple fur, red cheeks. **New feature**: the fur split down the middle.
- **Pokédex**: It eats every few minutes. Half its body is calm, half is furious, and which half is in charge depends on when it last ate.

### 28. Verdigrand, from Copperajah (879): yes

- **Typing**: Steel → **Steel/Ground**. *Alternative*: Steel/Rock.
- **The Patina Pokémon**, No. 2879. 3.6 m, 900.0 kg (Copperajah 3 m, 650 kg).
- **Stats**: 122 / 130 / 69 / 80 / 69 / 30 = 500 → **132 / 140 / 89 / 80 / 89 / 50 = 580** (+10 HP +10 Atk +20 Def +20 SpD +20 Spe).
- **Abilities**: Sheer Force, hidden Heavy Metal (was Sheer Force, hidden Heavy Metal).
- **Evolution**: Copperajah at level 236, in the rain. Data: `{"targetSpecies":"Verdigrand","method":"LevelInRain","level":236}`.
- **Look**: from our green and orange Copperajah with its copper skin: larger and older, its skin turned to verdigris green with orange showing only at the edges, the trunk longer and ending in a broad shovel of copper, tusks of pale stone and a howdah-like ridge on its back. Verdigris green, worn orange, pale tusks. **New feature**: the shovel of a trunk.
- **Pokédex**: Its green skin took a hundred years of rain to grow. It moved the stones of more than one old castle in Galar.

### 29. Fulgurex, from Dracozolt (880): yes

- **Typing**: Electric/Dragon, kept. *Alternative*: Electric/Rock.
- **The Fossil Pokémon**, No. 2880. 2.3 m, 250.0 kg (Dracozolt 1.8 m, 190 kg).
- **Stats**: 90 / 100 / 90 / 80 / 70 / 75 = 505 → **105 / 115 / 100 / 80 / 90 / 95 = 585** (+15 HP +15 Atk +10 Def +20 SpD +20 Spe).
- **Abilities**: Volt Absorb, Hustle, hidden Sand Rush (was Volt Absorb, Hustle, hidden Sand Rush).
- **Evolution**: Dracozolt at level 238, knowing Bolt Beak. Data: `{"targetSpecies":"Fulgurex","method":"LevelKnowsMove","level":238,"move":"Bolt Beak"}`.
- **Look**: from our Dracozolt, the bird's top on the drake's tail: the two halves grown together, a long neck between them, the feathers of its top spreading into a ruff over the drake's shoulders, its arms longer and its tail's spikes sparking (Glow, yellow). Yellow and green, orange feathers. **New feature**: the neck that joins its two halves.
- **Pokédex**: Restored wrongly, it grew right of itself. Scholars still argue about which of its halves came first.

### 30. Glacivolt, from Arctozolt (881): yes

- **Typing**: Electric/Ice, kept. *Alternative*: Electric/Rock.
- **The Fossil Pokémon**, No. 2881. 2.8 m, 200.0 kg (Arctozolt 2.3 m, 150 kg).
- **Stats**: 90 / 100 / 90 / 90 / 80 / 55 = 505 → **105 / 115 / 100 / 100 / 90 / 75 = 585** (+15 HP +15 Atk +10 Def +10 SpA +10 SpD +20 Spe).
- **Abilities**: Volt Absorb, Static, hidden Slush Rush (was Volt Absorb, Static, hidden Slush Rush).
- **Evolution**: Arctozolt at level 238, knowing Bolt Beak. Data: `{"targetSpecies":"Glacivolt","method":"LevelKnowsMove","level":238,"move":"Bolt Beak"}`.
- **Look**: from our Arctozolt, the bird's top on the icy body: the halves grown together, the bird's top larger with arms that reach the ground, the frost on its body grown into a mantle of ice crystals and its body's shivering stilled. Pale blue, yellow feathers, white ice. **New feature**: the mantle of ice.
- **Pokédex**: It no longer shivers. The cold that once froze half its body is now a shield it wears without noticing.

### 31. Abyssaurus, from Dracovish (882): yes

- **Typing**: Water/Dragon, kept. *Alternative*: Water/Rock.
- **The Fossil Pokémon**, No. 2882. 2.8 m, 280.0 kg (Dracovish 2.3 m, 215 kg).
- **Stats**: 90 / 90 / 100 / 70 / 80 / 75 = 505 → **105 / 105 / 110 / 70 / 100 / 95 = 585** (+15 HP +15 Atk +10 Def +20 SpD +20 Spe).
- **Abilities**: Water Absorb, Strong Jaw, hidden Sand Rush (was Water Absorb, Strong Jaw, hidden Sand Rush).
- **Evolution**: Dracovish at level 238, knowing Fishious Rend. Data: `{"targetSpecies":"Abyssaurus","method":"LevelKnowsMove","level":238,"move":"Fishious Rend"}`.
- **Look**: from our Dracovish, the fish's head on the drake's body: the halves joined by a thick neck, the fish head turned the right way up with gills along its throat, the drake's tail grown long and finned. Blue-green fish, green and brown drake. **New feature**: the gills along its throat.
- **Pokédex**: It breathes on land and in the sea alike. Its jaws, once too strong for its own head, now bite with the force they were made for.

### 32. Glacichthys, from Arctovish (883): yes

- **Typing**: Water/Ice, kept. *Alternative*: Water/Rock.
- **The Fossil Pokémon**, No. 2883. 2.5 m, 230.0 kg (Arctovish 2 m, 175 kg).
- **Stats**: 90 / 90 / 100 / 80 / 90 / 55 = 505 → **105 / 105 / 110 / 90 / 100 / 75 = 585** (+15 HP +15 Atk +10 Def +10 SpA +10 SpD +20 Spe).
- **Abilities**: Water Absorb, Ice Body, hidden Slush Rush (was Water Absorb, Ice Body, hidden Slush Rush).
- **Evolution**: Arctovish at level 238, knowing Fishious Rend. Data: `{"targetSpecies":"Glacichthys","method":"LevelKnowsMove","level":238,"move":"Fishious Rend"}`.
- **Look**: from our Arctovish, the fish's head upside down on the icy body: the head turned the right way up, its jaw strong, the icy body's flippers longer and a frill of frost along its back. Blue fish, pale ice body, white frost. **New feature**: the frill of frost.
- **Pokédex**: It can finally eat with ease, and it eats a great deal. It swims under the ice of frozen lakes for weeks without coming up.

### 33. Regifulgur, from Regieleki (894): maybe

- **Typing**: Electric, kept. *Alternative*: Electric/Steel.
- **The Electron Pokémon**, No. 2894. 1.5 m, 180.0 kg (Regieleki 1.2 m, 145 kg).
- **Stats**: 80 / 100 / 50 / 100 / 50 / 200 = 580 → **100 / 110 / 75 / 110 / 65 / 200 = 660** (+20 HP +10 Atk +25 Def +10 SpA +15 SpD).
- **Abilities**: Transistor (was Transistor).
- **Evolution**: Regieleki at level 244, with a Regigigas in the party. Data: `{"targetSpecies":"Regifulgur","method":"LevelWithSpeciesInParty","level":244,"species":"Regigigas"}`.
- **Look**: from our yellow Regieleki with its ring of current and its dotted face: its ring doubled into two rings crossing like an orrery, the dots of its face in a cross of seven (Glow, yellow), its body's bands grown thicker. Yellow, black seams, white dots. **New feature**: the second ring.
- **Pokédex**: It runs round the world in a day and comes back to its maker. The power lines of a whole country hum when it passes.

### 34. Regidraconis, from Regidrago (895): maybe

- **Typing**: Dragon, kept. *Alternative*: Dragon/Rock.
- **The Dragon Orb Pokémon**, No. 2895. 2.6 m, 260.0 kg (Regidrago 2.1 m, 200 kg).
- **Stats**: 200 / 100 / 50 / 100 / 50 / 80 = 580 → **200 / 110 / 75 / 110 / 75 / 90 = 660** (+10 Atk +25 Def +10 SpA +25 SpD +10 Spe).
- **Abilities**: Dragon's Maw (was Dragon's Maw).
- **Evolution**: Regidrago at level 244, with a Regigigas in the party. Data: `{"targetSpecies":"Regidraconis","method":"LevelWithSpeciesInParty","level":244,"species":"Regigigas"}`.
- **Look**: from our green Regidrago with its arms as a dragon's jaws: a second dragon's head of crystal on its back, its body's orange crystals larger (Glow, orange), the dots of its face in a cross of seven. Green body, orange crystals, white dots. **New feature**: the second head of crystal.
- **Pokédex**: Its maker gave it the shape of a dragon it had seen long ago. Beside its maker again, it grows into the whole of that dragon.

### 35. Hoarsteed, from Glastrier (896): maybe

- **Typing**: Ice, kept. *Alternative*: Ice/Steel.
- **The Hoarfrost Pokémon**, No. 2896. 2.6 m, 1000.0 kg (Glastrier 2.2 m, 800 kg).
- **Stats**: 100 / 145 / 130 / 65 / 110 / 30 = 580 → **115 / 155 / 140 / 65 / 125 / 60 = 660** (+15 HP +10 Atk +10 Def +15 SpD +30 Spe).
- **Abilities**: Chilling Neigh (was Chilling Neigh).
- **Evolution**: Glastrier at level 244, with a Calyrex in the party. Data: `{"targetSpecies":"Hoarsteed","method":"LevelWithSpeciesInParty","level":244,"species":"Calyrex"}`.
- **Look**: from our white Glastrier with its mask of ice: larger, the ice mask grown into a helm of hoarfrost with two horns, its mane of ice longer, its hooves ringed with frost and a cloak of icicles over its back. White, pale blue ice, black eyes. **New feature**: the helm's two horns.
- **Pokédex**: Its breath frosts a forest in a night. Only its king can ride it, and it will carry no one else.

### 36. Shadesteed, from Spectrier (897): maybe

- **Typing**: Ghost, kept. *Alternative*: Ghost/Dark.
- **The Shade Pokémon**, No. 2897. 2.4 m, 50.0 kg (Spectrier 2 m, 44.5 kg).
- **Stats**: 100 / 65 / 60 / 145 / 80 / 130 = 580 → **115 / 65 / 85 / 155 / 100 / 140 = 660** (+15 HP +25 Def +10 SpA +20 SpD +10 Spe).
- **Abilities**: Grim Neigh (was Grim Neigh).
- **Evolution**: Spectrier at level 244, with a Calyrex in the party. Data: `{"targetSpecies":"Shadesteed","method":"LevelWithSpeciesInParty","level":244,"species":"Calyrex"}`.
- **Look**: from our black Spectrier with its pale mane: longer in the leg, the mane grown into a trailing flame of pale blue (Glow, pale blue), its hooves gone to smoke and a crown-like crest on its brow. Black body, pale blue mane. **New feature**: the crest on its brow.
- **Pokédex**: It runs through the night without touching the ground. Only its king can ride it, and those it passes forget it at once.

### 37. Wyrdstag, from Wyrdeer (899): maybe

- **Typing**: Normal/Psychic, kept. *Alternative*: Psychic/Fairy.
- **The Wyrd Stag Pokémon**, No. 2899. 2.2 m, 130.0 kg (Wyrdeer 1.8 m, 95.1 kg).
- **Stats**: 103 / 105 / 72 / 105 / 75 / 65 = 525 → **108 / 110 / 92 / 110 / 100 / 80 = 600** (+5 HP +5 Atk +20 Def +5 SpA +25 SpD +15 Spe): the rise is +75, to the 600 ceiling.
- **Abilities**: Intimidate, Frisk, hidden Sap Sipper (was Intimidate, Frisk, hidden Sap Sipper).
- **Evolution**: Wyrdeer at level 238, after using Psyshield Bash 20 times. Data: `{"targetSpecies":"Wyrdstag","method":"LevelAfterMoveUses","level":238,"move":"Psyshield Bash","value":20}`.
- **Look**: from our grey Wyrdeer with its beard of fur and its antlers' black orbs: larger, the antlers grown into a crown of many tines with six orbs that glow (Glow, pale blue), the beard longer and a mantle of fur over its shoulders. Grey fur, white beard, black and blue orbs. **New feature**: the six orbs.
- **Pokédex**: Its antlers show those who look at them the place they long for most. People who follow it into the hills come back calm.

### 38. Halberdon, from Kleavor (900): yes

- **Typing**: Bug/Rock, kept. *Alternative*: Bug/Steel.
- **The Halberd Pokémon**, No. 2900. 2.2 m, 130.0 kg (Kleavor 1.8 m, 89 kg).
- **Stats**: 70 / 135 / 95 / 45 / 70 / 85 = 500 → **90 / 145 / 110 / 45 / 90 / 100 = 580** (+20 HP +10 Atk +15 Def +20 SpD +15 Spe).
- **Abilities**: Swarm, Sheer Force, hidden Sharpness (was Swarm, Sheer Force, hidden Sharpness).
- **Evolution**: Kleavor at a Black Augurite used on it at level 238 or above. Data: `{"targetSpecies":"Halberdon","method":"UseItem","level":238,"item":"Black Augurite"}`.
- **Look**: from our brown Kleavor with its axes of black stone: taller, the axes grown into halberds, a long haft of brown chitin with a blade of black augurite and a spike, its wings short and stiff like a cape, and its head's horn longer. Brown body, black stone blades, grey wings. **New feature**: the halberd's spike.
- **Pokédex**: Its blades split stone as easily as wood. Old woodsmen in Hisui marked a tree it had touched and never felled it.

### 39. Shoalwraith, from Basculegion (902): maybe

- **Typing**: Water/Ghost, kept. *Alternative*: Water/Dark.
- **The Shoal Pokémon**, No. 2902. 3.8 m, 140.0 kg (Basculegion 3 m, 110 kg).
- **Stats**: 120 / 112 / 65 / 80 / 75 / 78 = 530 → **120 / 117 / 90 / 85 / 100 / 88 = 600** (+5 Atk +25 Def +5 SpA +25 SpD +10 Spe): the rise is +70, to the 600 ceiling.
- **Abilities**: Swift Swim, Adaptability, hidden Mold Breaker (was Swift Swim, Adaptability, hidden Mold Breaker).
- **Evolution**: Basculegion at level 238, after losing 294 HP or more to recoil without fainting. Data: `{"targetSpecies":"Shoalwraith","method":"LevelAfterRecoil","level":238,"value":294}`.
- **Look**: from our green Basculegion with its ghost-wreathed head: longer, the ghosts of its school grown from four to seven, trailing round it like a shoal (Glow, pale blue), its fins ragged and its eyes red. Green body, blue ghosts, red eyes; the female's red stripe where she was the one. **New feature**: the shoal of seven ghosts.
- **Pokédex**: The spirits of every school it swam with ride with it. Rivers it travels are said to run silent for a day after.

### 40. Clifflasher, from Sneasler (903): yes

- **Typing**: Fighting/Poison, kept. *Alternative*: Fighting/Ice.
- **The Cliff Climber Pokémon**, No. 2903. 1.6 m, 60.0 kg (Sneasler 1.3 m, 43 kg).
- **Stats**: 80 / 130 / 60 / 40 / 80 / 120 = 510 → **90 / 140 / 85 / 40 / 100 / 135 = 590** (+10 HP +10 Atk +25 Def +20 SpD +15 Spe).
- **Abilities**: Pressure, Unburden, hidden Poison Touch (was Pressure, Unburden, hidden Poison Touch).
- **Evolution**: Sneasler at level 240, holding a Razor Claw by day (used up). Data: `{"targetSpecies":"Clifflasher","method":"LevelHoldingItemDay","level":240,"item":"Razor Claw"}`.
- **Look**: from our grey Sneasler with its long claws and its cream crest: taller, the claws longer with a purple sheen of poison (Glow, faint purple), a second pair of claws on its feet for climbing and its crest tall and swept back. Grey fur, cream crest, purple claws. **New feature**: the climbing claws on its feet.
- **Pokédex**: It climbs sheer cliffs as fast as other Pokémon run on the flat, and its claws leave poison in the rock for years.

### 41. Spinarmada, from Overqwil (904): yes

- **Typing**: Dark/Poison, kept. *Alternative*: Dark/Steel.
- **The Armada Pokémon**, No. 2904. 3.0 m, 80.0 kg (Overqwil 2.5 m, 60.5 kg).
- **Stats**: 85 / 115 / 95 / 65 / 65 / 85 = 510 → **95 / 130 / 105 / 65 / 95 / 100 = 590** (+10 HP +15 Atk +10 Def +30 SpD +15 Spe).
- **Abilities**: Poison Point, Swift Swim, hidden Intimidate (was Poison Point, Swift Swim, hidden Intimidate).
- **Evolution**: Overqwil at level 238, knowing Barb Barrage. Data: `{"targetSpecies":"Spinarmada","method":"LevelKnowsMove","level":238,"move":"Barb Barrage"}`.
- **Look**: from our black Overqwil with its spines and its jaw: longer, its spines grown into a forest of black spears with purple tips, a fin along its back like a sail and its toothy jaw wider. Black body, purple tips, yellow eyes. **New feature**: the sail of a fin.
- **Pokédex**: It drifts with its sail up through the dark seas, and whole fleets of smaller Pokémon shelter in the shadow of its spines.

### 42. Enamorata, from Enamorus (905): maybe

- **Typing**: Fairy/Flying, kept. *Alternative*: Fairy/Psychic.
- **The Love-Hate Pokémon**, No. 2905. 2.0 m, 60.0 kg (Enamorus 1.6 m, 48 kg).
- **Stats**: 74 / 115 / 70 / 135 / 80 / 106 = 580 → **94 / 120 / 90 / 145 / 100 / 111 = 660** (+20 HP +5 Atk +20 Def +10 SpA +20 SpD +5 Spe).
- **Abilities**: Cute Charm, hidden Contrary (was Cute Charm, hidden Contrary).
- **Evolution**: Enamorus at level 244, with a Landorus in the party. Data: `{"targetSpecies":"Enamorata","method":"LevelWithSpeciesInParty","level":244,"species":"Landorus"}`.
- **Look**: from our pink Enamorus riding its cloud: larger, its cloud grown into a chariot of cloud with a heart-shaped prow, its tail's trailing spots grown into a ring of hearts round it and its horns longer. Pink, white cloud, dark spots. **New feature**: the chariot of cloud.
- **Pokédex**: Where it passes, flowers open out of season. It is gentle to those who are kind to the land, and hard on those who are not.

## Balance

Measured by the script, as the earlier batches':

- **Totals**: 515 (Voltechinus) to 600 (Furnapede (+75), Wyrdstag (+75), Shoalwraith (+70), each risen only to the ceiling), the legendary sets 660. Nothing that isn't legendary passes 600.
- **Highest of each stat**, not counting the legendaries: HP 132 (Verdigrand); Atk 145 (Halberdon); Def 150 (Runedolmen); SpA 150 (Wraithreef); SpD 135 (Wraithreef); Spe 146 (Rapierfin). Among the legendaries Regifulgur keeps Regieleki's Speed of 200 and Regidraconis Regidrago's HP of 200, both unrisen.
- **The weakest stay modest on purpose** (batch 1's answer: no floor): Voltechinus 515, Gorgehog 516, Jarlkatt 520, Reynardine 535, from the region's four lowest.
- **Abilities**: every species keeps its own but Cramorant, Eiscue and Morpeko, whose Gulp Missile, Ice Face and Hunger Switch the engine runs for their own species alone (`BattleCore.Shapes.cs`): on ours they would do nothing (question 5).

**Flagged as strong** (each inside the official range; the user may want one turned down):
1. **Rapierfin**: Barraskewda's 136 Speed rises to 146 with 138 Attack, Propeller Tail and Swift Swim. *To turn down*: Attack 130 and the 8 to HP; or Speed kept at 136.
2. **Shoalwraith**: Adaptability with 117 Attack and Swift Swim beside it. *To turn down*: Attack 112 kept, the 5 to Sp. Def.
3. **Clifflasher**: 140 Attack and 135 Speed with Unburden. *To turn down*: Speed 125 and the 10 to Defense.
4. **Urngeist**: Shell Smash with 144 Sp. Atk, and a Fire typing that hits Steel. *To turn down*: Sp. Atk 134 kept, the 10 to HP.
5. **Halberdon**: Kleavor's Sharpness with 145 Attack and Stone Axe. *To turn down*: Attack 135 kept, the 10 to Sp. Def.

**Weaker by design**: Henjourner (its rise goes to the Sp. Def of 20 behind Stonjourner's walls, not to its attacks) and Runedolmen (Runerigus's Speed of 30 risen by 5) are judged on their designs more than their numbers.

## Open questions: only what is new for Galar and Hisui

The earlier batches' questions are settled and applied (above). These are new.

1. **The level band.** Galar's climb (about 224 to 242, plan 18 · W4's fair climb) lies wholly above the 160 batch 3 wrote, as do Unova's, Kalos's, Alola's and Paldea's. *Default*: **each batch evolves within its own region's climb, under the cap of 260** (the 160 dropped, every approved level unchanged): Galar's species between 226 and 240, its legendary sets at 244. `SpeciesPastHundredTests.Climb` gains a row for generation 8 when the batch is built, and Galar's region file, when plan 23 · Z9 writes it, should keep `[[2, 224], [65, 242]]` or move these levels with it. *Alternatives*: keep 160 for every later region (each of its species then evolves at its first level once caught there, since none is met under 224); or one band of 226 to 240 for every later region's batch alike.
2. **Hisui's seven, and Enamorus.** Hisui is plan 23's optional side region (its decision 9), so the seven first met there have no climb of their own. *Default*: they are this batch's, at Galar's levels (238 to 240), by their own generation in the data; Enamorus is built only if the Unova batch builds Tornadus's, Thundurus's and Landorus's, as their fourth. *Alternatives*: Hisui's at Sinnoh's band (101 to 120, since Hisui would be reached from Sinnoh's post-game), or left for a Hisui batch of their own if plan 23 keeps Hisui.
3. **Indeedee's and Basculegion's females.** Both are forms in the data with their own stats and abilities (`Indeedee-Female`, `Basculegion-Female`), as Meowstic's will be in Kalos's batch; a form is not regional, so the female evolves by the species' row (`Evolution.FitsForm`). *Default*: one species of ours with the male's numbers and abilities, the female's colours kept as a look of hers (`PokemonLook`'s female, never a form of ours). *Alternatives*: the female's numbers for both; or the females left out (her form refused the row by a `fromForm`).
4. **Looks that aren't forms: Toxtricity, Polteageist and Alcremie.** Toxtricity's Low Key (other abilities), Polteageist's antique pot and Alcremie's sixty-three creams and sweets are forms of look in the data, and each evolves by the species' row. *Default*: one look each (Amplipunk Amped's abilities with the Low Key's blue where it came from one, Urngeist with the antique's mark where it came from one, Patisserene one white wedding cake), since ours have no forms; Patisserene a maybe for that. *Alternatives*: Alcremie left out as Rotom (its whole design is its decorations), or a Low Key Amplipunk with the Low Key's abilities (Punk Rock, Minus, Technician) as a form of ours, against the settled rule.
5. **Abilities that work for one species.** Gulp Missile, Ice Face and Hunger Switch change a form of Cramorant's, Eiscue's or Morpeko's, and the engine runs them for those species alone (`BattleCore.Shapes.cs`: `Is(b, "Eiscue", "Ice Face")`). *Default*: ours take an ability of `abilities.json` that keeps the idea, in the same one slot: Gannetgulp **Cud Chew** (it swallows and brings up again), Floeguin **Slush Rush** (it slides on its floe), Gorgehog **Cheek Pouch** (it stuffs its cheeks). *Alternative*: the old abilities kept, doing nothing on ours.
6. **The four fossils.** Galar's fossils are halves put together wrongly (Dracozolt, Arctozolt, Dracovish, Arctovish). *Default*: each grows the body it was missing, as a set of four, by its own signature move (Bolt Beak or Fishious Rend); none holds a fossil, since `items.json` has none of Galar's four. *Alternatives*: two only (one bird, one fish), or the four by holding a Rare Bone.
7. **Regional forms that end their lines in Galar or Hisui.** The Galarian Rapidash, Slowbro, Slowking, Weezing, Darmanitan, Stunfisk and birds and the Hisuian Arcanine, Electrode, Typhlosion, Samurott, Lilligant, Zoroark, Braviary, Goodra, Avalugg and Decidueye are forms of species other batches judge (batch 1's question 10). *Default*: none proposed here. *Alternative*: a list of their designs, numbered by a rule of their own, since 2000 + the species' number is the species' own design's.
8. **Urshifu.** *Default*: no (above), as Wormadam. *Alternative*: one Urshifu of ours whatever the style, keeping Fighting/Dark, so that the Rapid Strike one would lose its Water on evolving.

## The table to answer

One row a species. The user answers each with yes, a change, or no; striking a row in the selection table above is a no.

| # | Ours | Typing | Abilities | HP/Atk/Def/SpA/SpD/Spe (total) | Δ | Evolves |
|---|---|---|---|---|---|---|
| 1 | Larderous | Normal/Ground | Cheek Pouch, Gluttony | 130/115/110/55/90/40 (540) | +10 HP +20 Atk +15 Def +15 SpD +20 Spe | level 226, holding a Sitrus Berry (used up) |
| 2 | Reynardine | Dark/Fairy | Run Away, Unburden, Stakeout | 80/58/73/112/107/105 (535) | +10 HP +15 Def +25 SpA +15 SpD +15 Spe | level 227, at night |
| 3 | Thistlewisp | Grass/Flying | Cotton Down, Regenerator, Effect Spore | 75/50/100/100/130/85 (540) | +15 HP +10 Def +20 SpA +10 SpD +25 Spe | level 226, by day |
| 4 | Ramsworth | Normal/Fighting | Fluffy, Steadfast, Bulletproof | 87/105/115/60/100/103 (570) | +15 HP +25 Atk +15 Def +10 SpD +15 Spe | level 227, knowing Cotton Guard |
| 5 | Juggernaw | Water/Rock | Strong Jaw, Shell Armor, Swift Swim | 105/135/105/48/88/84 (565) | +15 HP +20 Atk +15 Def +20 SpD +10 Spe | level 228, knowing Jaw Lock |
| 6 | Voltcourser | Electric | Strong Jaw, Competitive | 79/110/80/90/80/131 (570) | +10 HP +20 Atk +20 Def +20 SpD +10 Spe | level 228, by day |
| 7 | Pippinwyrm | Grass/Dragon | Ripen, Gluttony, Hustle | 80/130/85/95/80/95 (565) | +10 HP +20 Atk +5 Def +20 SpD +25 Spe | a Tart Apple used on it at level 230 or above |
| 8 | Crumbledrake | Grass/Dragon | Ripen, Gluttony, Thick Fat | 120/85/100/120/100/40 (565) | +10 HP +20 Def +20 SpA +20 SpD +10 Spe | a Sweet Apple used on it at level 230 or above |
| 9 | Duneconda | Ground | Sand Spit, Shed Skin, Sand Veil | 87/122/130/65/95/91 (590) | +15 HP +15 Atk +5 Def +25 SpD +20 Spe | level 231, knowing Coil |
| 10 | Gannetgulp | Flying/Water | Cud Chew | 85/100/80/95/105/90 (555) | +15 HP +15 Atk +25 Def +10 SpA +10 SpD +5 Spe | level 229, knowing Dive |
| 11 | Rapierfin | Water/Steel | Swift Swim, Propeller Tail | 76/138/80/60/70/146 (570) | +15 HP +15 Atk +20 Def +20 SpD +10 Spe | level 230, in the rain |
| 12 | Amplipunk | Electric/Poison | Punk Rock, Plus, Technician | 85/103/85/129/85/95 (582) | +10 HP +5 Atk +15 Def +15 SpA +15 SpD +20 Spe | level 232, knowing Overdrive |
| 13 | Furnapede | Fire/Bug | Flash Fire, White Smoke, Flame Body | 105/125/90/95/105/80 (600) | +5 HP +10 Atk +25 Def +5 SpA +15 SpD +15 Spe | level 232, holding a Heat Rock (used up) |
| 14 | Kraketsu | Fighting/Water | Limber, Technician | 95/133/105/70/95/62 (560) | +15 HP +15 Atk +15 Def +15 SpD +20 Spe | level 233, knowing Octolock |
| 15 | Urngeist | Ghost/Fire | Weak Armor, Cursed Body | 80/65/90/144/124/85 (588) | +20 HP +25 Def +10 SpA +10 SpD +15 Spe | a Cracked Pot used on it at level 236 or above |
| 16 | Jarlkatt | Steel/Fighting | Battle Armor, Tough Claws, Steely Spirit | 90/130/110/50/80/60 (520) | +20 HP +20 Atk +10 Def +20 SpD +10 Spe | level 229, knowing Iron Head |
| 17 | Wraithreef | Ghost/Water | Weak Armor, Perish Body | 85/95/80/150/135/45 (590) | +25 HP +30 Def +5 SpA +5 SpD +15 Spe | level 234, at night |
| 18 | Paladrake | Fighting/Steel | Steadfast, Scrappy | 82/140/105/68/97/95 (587) | +20 HP +5 Atk +10 Def +15 SpD +30 Spe | level 234, after three critical hits in one battle |
| 19 | Runedolmen | Ground/Ghost | Wandering Spirit | 88/105/150/70/115/35 (563) | +30 HP +10 Atk +5 Def +20 SpA +10 SpD +5 Spe | level 234, under the Stone Arch after losing 49 HP or more without fainting |
| 20 | Patisserene | Fairy | Sweet Veil, Aroma Veil | 85/60/90/125/131/84 (575) | +20 HP +15 Def +15 SpA +10 SpD +20 Spe | level 232, spun round holding a Love Sweet (used up) |
| 21 | Phalanxion | Fighting/Steel | Battle Armor, Defiant | 80/120/115/70/80/85 (550) | +15 HP +20 Atk +15 Def +20 SpD +10 Spe | level 233, with a Falinks in the party |
| 22 | Voltechinus | Electric/Water | Lightning Rod, Electric Surge | 78/106/105/106/95/25 (515) | +30 HP +5 Atk +10 Def +15 SpA +10 SpD +10 Spe | level 229, knowing Zing Zap |
| 23 | Auroramoth | Ice/Bug | Shield Dust, Ice Scales | 85/65/80/140/100/85 (555) | +15 HP +20 Def +15 SpA +10 SpD +20 Spe | level 235, with friendship 220 or more, at night |
| 24 | Henjourner | Rock/Psychic | Power Spot | 110/135/135/40/60/70 (550) | +10 HP +10 Atk +20 SpA +40 SpD | level 235, at dusk |
| 25 | Floeguin | Ice/Water | Slush Rush | 90/95/115/75/100/75 (550) | +15 HP +15 Atk +5 Def +10 SpA +10 SpD +25 Spe | level 235, at an Ice Rock |
| 26 | Majordomee | Psychic/Normal | Inner Focus, Synchronize, Psychic Surge | 80/65/80/120/105/105 (555) | +20 HP +25 Def +15 SpA +10 SpD +10 Spe | level 236, with friendship 220 or more, by day |
| 27 | Gorgehog | Electric/Dark | Cheek Pouch | 78/110/73/80/73/102 (516) | +20 HP +15 Atk +15 Def +10 SpA +15 SpD +5 Spe | level 231, knowing Aura Wheel |
| 28 | Verdigrand | Steel/Ground | Sheer Force, Heavy Metal | 132/140/89/80/89/50 (580) | +10 HP +10 Atk +20 Def +20 SpD +20 Spe | level 236, in the rain |
| 29 | Fulgurex | Electric/Dragon | Volt Absorb, Hustle, Sand Rush | 105/115/100/80/90/95 (585) | +15 HP +15 Atk +10 Def +20 SpD +20 Spe | level 238, knowing Bolt Beak |
| 30 | Glacivolt | Electric/Ice | Volt Absorb, Static, Slush Rush | 105/115/100/100/90/75 (585) | +15 HP +15 Atk +10 Def +10 SpA +10 SpD +20 Spe | level 238, knowing Bolt Beak |
| 31 | Abyssaurus | Water/Dragon | Water Absorb, Strong Jaw, Sand Rush | 105/105/110/70/100/95 (585) | +15 HP +15 Atk +10 Def +20 SpD +20 Spe | level 238, knowing Fishious Rend |
| 32 | Glacichthys | Water/Ice | Water Absorb, Ice Body, Slush Rush | 105/105/110/90/100/75 (585) | +15 HP +15 Atk +10 Def +10 SpA +10 SpD +20 Spe | level 238, knowing Fishious Rend |
| 33 | Regifulgur | Electric | Transistor | 100/110/75/110/65/200 (660) | +20 HP +10 Atk +25 Def +10 SpA +15 SpD | level 244, with a Regigigas in the party |
| 34 | Regidraconis | Dragon | Dragon's Maw | 200/110/75/110/75/90 (660) | +10 Atk +25 Def +10 SpA +25 SpD +10 Spe | level 244, with a Regigigas in the party |
| 35 | Hoarsteed | Ice | Chilling Neigh | 115/155/140/65/125/60 (660) | +15 HP +10 Atk +10 Def +15 SpD +30 Spe | level 244, with a Calyrex in the party |
| 36 | Shadesteed | Ghost | Grim Neigh | 115/65/85/155/100/140 (660) | +15 HP +25 Def +10 SpA +20 SpD +10 Spe | level 244, with a Calyrex in the party |
| 37 | Wyrdstag | Normal/Psychic | Intimidate, Frisk, Sap Sipper | 108/110/92/110/100/80 (600) | +5 HP +5 Atk +20 Def +5 SpA +25 SpD +15 Spe | level 238, after using Psyshield Bash 20 times |
| 38 | Halberdon | Bug/Rock | Swarm, Sheer Force, Sharpness | 90/145/110/45/90/100 (580) | +20 HP +10 Atk +15 Def +20 SpD +15 Spe | a Black Augurite used on it at level 238 or above |
| 39 | Shoalwraith | Water/Ghost | Swift Swim, Adaptability, Mold Breaker | 120/117/90/85/100/88 (600) | +5 Atk +25 Def +5 SpA +25 SpD +10 Spe | level 238, after losing 294 HP or more to recoil without fainting |
| 40 | Clifflasher | Fighting/Poison | Pressure, Unburden, Poison Touch | 90/140/85/40/100/135 (590) | +10 HP +10 Atk +25 Def +20 SpD +15 Spe | level 240, holding a Razor Claw by day (used up) |
| 41 | Spinarmada | Dark/Poison | Poison Point, Swift Swim, Intimidate | 95/130/105/65/95/100 (590) | +10 HP +15 Atk +10 Def +30 SpD +15 Spe | level 238, knowing Barb Barrage |
| 42 | Enamorata | Fairy/Flying | Cute Charm, Contrary | 94/120/90/145/100/111 (660) | +20 HP +5 Atk +20 Def +10 SpA +20 SpD +5 Spe | level 244, with a Landorus in the party |

## The technical plan (for after the user's answers)

Batch 1's pipeline holds unchanged: each design an entry of `tools/DataImporter/Overrides/species-past-hundred.json` (a `LevelWithSpeciesInParty` row takes `species`, a `LevelAtLocation` row `location`, a `LevelAfterDamage` row `location` and `value`, `CriticalHits`, `LevelAfterRecoil` and `LevelAfterMoveUses` their `value` and the last its `move`), two written Pokédex entries and a habitat each in `Data/dex-entries.json` (`--check-entries` clean), a run of the importer, and `SpeciesPastHundredTests`' count of designs, its `Climb` given a row for generation 8 (226 to 240, the legendary sets 244, question 1) and a test to each condition new to the table (three critical hits, the Stone Arch after damage, twenty uses of a move, recoil, a spin). The sculpts wait for the user (wave 53: every species of ours stays generated by rule from its data until the user says otherwise). Nothing in the engine is needed: every method above runs today, and the counters `Pokemon.EvolutionProgress` keeps for Sirfetch'd's, Runerigus's, Wyrdeer's and Basculegion's forebears are kept for any species whose evolution asks for them. The legendary sets' evolutions join the Frontier's ban list (`FrontierRules.IsBannedOfOurs`). What is not this batch's: 06 · R31's moves past level 100 (the learnsets wait for them), the lines of three, Galar's and Hisui's maps (plan 23), and so where in the game the Stone Arch stands: until Galar is built a Runedolmen can only come from a Runerigus that can only come from a Galarian Yamask under the same arch, so both wait for Galar together.

## How the numbers were checked

The same kind of Node.js script as the earlier batches', kept in the session's scratch folder and reading only `species.json`, `abilities.json` and `items.json`, with the 170 names and numbers of batches 1 to 4 read from their four documents' tables. It built the lines (a regional form's evolution counted as the form's own, an evolution into a look counted as the species'), found the 48 that qualify among National Pokédex 810 to 905, and for each of the 42 designs checked: that it qualifies, and that every qualifying species has a design or a reason for none, and every species given a reason qualifies; that the rise is +80 or what the ceiling leaves (600, or 660 for the legendary sets), that no stat falls and that the six stats add up to the total written; that the name is one word of letters, at most 12, taken by no species or form of the data, by none of the earlier batches' 170 and by no other design of this batch; that the number is 2000 + the old species' and meets none of the earlier batches'; that the types are one or two of the eighteen and not the same twice; that the abilities exist and fill exactly the old species' slots, the hidden one where it had one; that the method is one the data already uses; that the level is between 226 and 240 for a species that isn't legendary and 244 for the legendary sets; that an item held or used exists and is no key item; that a move to know or use is in the species' level-up learnset; that a place is one of the data's four evolution places; that a species named in the party exists; and that each is taller and heavier than what it comes from. The shortlist, the tables and the entries were written by the same script from the same values, so a number can't differ between them. The names of Unova's, Kalos's, Alola's and Paldea's batches, written in the same wave, could not be read: the session that builds the second of two batches checks this batch's 42 names against the other's.
