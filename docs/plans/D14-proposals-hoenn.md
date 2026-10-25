# D14 · Evolutions past level 100, batch 3 (Hoenn): the proposal

Plan 03 · D14, the design part of its third batch, written in wave 51 after the user approved batch 1 ([D14-proposals-kanto.md](D14-proposals-kanto.md)) and batch 2 ([D14-proposals-johto.md](D14-proposals-johto.md)) with every default, on 2026-10-25. **Approved by the user on 2026-10-25: "I accept all the evolutions", read as the whole list and every one of the seven questions at its default (the level ceiling among them: each species evolves within its own region's climb).** Nothing here is built: no species, no evolution row, no learnset, no sculpt, no test. Every row below waits for the user's yes, a change, or a no, before a later session writes it into the data. The format is batch 2's.

**What batches 1 and 2 settled, applied here without asking again**: the rise is **+80, capped so that no species of ours that isn't legendary passes 600**, and a region's legendary set rises to **660** (batch 1's birds, batch 2's beasts); the number is **2000 + the number of the species it evolves from** (here 2262 to 2381, meeting none of the earlier batches' 2020 to 2245); abilities come **only from `abilities.json`**, as many slots as the species had, in its order, the hidden one last; the evolution is **by level with a condition the engine already runs**, a held item used up; our species are listed in the National Pokédex after the 1,025 and **count toward none of its totals or diplomas**; names are invented, one word of at most 12 letters, and held against every species and form of the data and the earlier batches' 80; **an evolution that starts from a regional form belongs to that form's own line** (`Evolution.FitsForm`); a female look, a Mega or a Gigantamax form is kept where the species has one, and ours have none of their own; the Frontier lets them be entered (a legendary's evolution joins its ban list), Pal Park, migration, the way out, the Pokéathlon's rivals and the golden corpus are held to 1 to 1,025; the eight Eeveelutions go alike (none is Hoenn's); the cover legendaries and the mythicals don't evolve.

**One thing is new and changes the levels** (question 1): Hoenn is met at levels 136 to 162 (`regions/hoenn.json`'s `levels`, Emerald's 2 to 58), so batch 1's ceiling of 130 for a species that isn't legendary would make every Hoenn Pokémon caught in Hoenn evolve at its first level. By default this batch's own species evolve **between 140 and 158**, within Hoenn's own climb and before its Champion, and its legendaries at 160.

Every name, type, ability, item, move, species, stat and sum in this document was checked by a script that only reads `PokemonPlatinumEngine/Data/species.json`, `abilities.json` and `items.json`, with the two earlier batches' names and numbers read from their documents (kept outside the repository; what it checks is listed under "How the numbers were checked", at the end).

## The shortlist: the twelve strongest

If the user wants a small first build of this batch, these are the twelve (the sixth is a pair). Each lifts one of the weakest last stages of any region, or is a design the region's players asked about for twenty years.

| | Ours | From | Typing | Total | Evolves | Why it leads |
|---|---|---|---|---|---|---|
| 1 | **Heartreef** | Luvdisc | Water/Fairy | 330 → 410 | level 140 holding a Heart Scale | the lowest total of Hoenn, and the Heart Scale is its own |
| 2 | **Whirlpanda** | Spinda | Normal/Fighting | 360 → 440 | level 140, knowing Teeter Dance | the second lowest; the staggering dance as the drunken master's style |
| 3 | **Gemwraith** | Sableye | Dark/Ghost | 380 → 460 | level 142 holding a Star Piece | 380, the gem eater grown through with gems |
| 4 | **Deceivore** | Mawile | Steel/Fairy | 380 → 460 | level 142 holding a Razor Fang | 380, the deceiver's jaw grown as a creature of its own |
| 5 | **Felinelle** | Delcatty | Normal/Fairy | 400 → 480 | a Moon Stone at level 140 | 400, among the weakest last stages anywhere; the same stone again |
| 6 | **Anodion** and **Cathodion** | Plusle and Minun | Electric/Fairy | 405 → 485 | level 142, each with the other in the party | the pair of 405, grown only together |
| 7 | **Streakoon** | Linoone | Normal/Ground | 420 → 500 | level 140, knowing Belly Drum | Hoenn's Linoone never went on; only the Galarian one became Obstagoon |
| 8 | **Hyaenarch** | Mightyena | Dark | 420 → 500 | level 142, at night | the first route's hunter, 420 |
| 9 | **Pelicaptain** | Pelipper | Water/Flying | 440 → 520 | level 145, in the rain | the coast's bird, given the Drizzle the newer games gave Pelipper |
| 10 | **Ocellwing** | Masquerain | Bug/Flying | 454 → 534 | level 142 holding a Silver Powder | the moth of eyespots, a wing more of them |
| 11 | **Sporepugil** | Breloom | Grass/Fighting | 460 → 540 | level 148 holding a Toxic Orb | the mushroom boxer by the item its Poison Heal is played with |
| 12 | **Omenblade** | Absol | Dark | 465 → 545 | level 150 holding a Scope Lens | the beloved Pokémon of disasters, with Super Luck's item |

## What qualifies, and how many

The earlier batches' rule and the same script: a species **with no evolution at all**, or **the last stage of a line of two**; an evolution that starts from a regional form counts as that form's own.

**Hoenn, National Pokédex 252 to 386: 58 species qualify.**

- **28 alone**: Sableye, Mawile, Plusle, Minun, Volbeat, Illumise, Torkoal, Spinda, Zangoose, Seviper, Lunatone, Solrock, Castform, Kecleon, Tropius, Absol, Relicanth, Luvdisc, Regirock, Regice, Registeel, Latias, Latios, Kyogre, Groudon, Rayquaza, Jirachi, Deoxys (ten of them legendary or mythical).
- **30 last of a line of two**: Mightyena, Linoone, Swellow, Pelipper, Masquerain, Breloom, Ninjask, Shedinja, Hariyama, Delcatty, Medicham, Manectric, Swalot, Sharpedo, Wailord, Camerupt, Grumpig, Cacturne, Altaria, Whiscash, Crawdaunt, Claydol, Cradily, Armaldo, Milotic, Banette, Chimecho, Glalie, Huntail, Gorebyss. Linoone's line is one of two by the regional rule (only the Galarian Linoone becomes Obstagoon); Chimecho comes from a first stage of a later game (Chingling), as batch 2's Sudowoodo and Mantine did; Nincada has two ends (Ninjask and Shedinja) and so has Clamperl (Huntail and Gorebyss), each a line of two; Glalie's line is one of two beside Snorunt's other end, Froslass, which is Sinnoh's.
- **Deferred to this batch by the earlier two: none.** Neither batch 1's nor batch 2's "not here" list names a line that ends in Hoenn: Wynaut's Wobbuffet was batch 2's, and Azurill's line ends in Johto's Azumarill, the last of three.
- **Not here, last of a line of three (15)**: Sceptile, Blaziken, Swampert, Beautifly, Dustox, Ludicolo, Shiftry, Gardevoir, Slaking, Exploud, Aggron, Flygon, Walrein, Salamence, Metagross (the two moths each end a line of three through Silcoon or Cascoon). They take 06 · R31's moves.
- **Not here, a Hoenn species whose line ends in a later region**: Kirlia's other end (Gallade), Nosepass (Probopass), Roselia (Roserade), Dusclops (Dusknoir) and Snorunt's other end (Froslass). Each is Sinnoh's batch's, if it qualifies there.

51 designs follow; Shedinja, Castform, Kyogre, Groudon, Rayquaza, Jirachi and Deoxys are left out with their reasons.

## How to read an entry

As batch 2's. **Stats** are the stats the game reads (the newest games' where `species.json` has a `modern` change: Swellow, Pelipper, Masquerain, Delcatty, Volbeat, Illumise, Lunatone, Solrock, Chimecho; Mawile is Steel/Fairy by the same). **Levels**: Hoenn is met at 136 to 162, its Gym Leaders at about 142 to 156 and its Champion at 162, so the region's own species evolve between 140 (the weakest, met on its first routes) and 158 (Relicanth, the living fossil), and its legendaries at 160, after the Champion. **Looks** start from our own sculpt of the species, and a species whose female looks different (Medicham, Swalot, Camerupt, Cacturne, Milotic, Relicanth) keeps that difference where the feature survives. **Say**: strong yes (the shortlist), yes, maybe, no.

**The sets.** Hoenn pairs its species more than any region, and each pair here is built as a set or not at all: Plusle and Minun, Volbeat and Illumise, Zangoose and Seviper, Lunatone and Solrock, the two fossils (Cradily and Armaldo), Clamperl's two ends (Huntail and Gorebyss), the three Regis and the eon pair. The weather trio is a set too, and a no. Every condition below is one the engine already runs; two are new to this batch's designs: **`LevelWithSpeciesInParty`** (Mantyke's own way into Mantine, batch 2's Raysail and beasts) for the pairs that go further together, and **`LevelAfterDefeating`** (Bisharp's own way into Kingambit, which counts knock-outs of a named species: `Evolution.cs`) for the two rivals, each grown by beating three of the other.

## The legendaries, the mythicals, Shedinja and Castform

Hoenn has eight legendaries and two mythicals; the user asked each batch to say which of its region's evolve.

- **Regirock, Regice and Registeel: maybe, as a trio or not at all.** Regigigas made them; with a Regigigas in the party at level 160 each goes further. +80 to 660, as batch 1's birds and batch 2's beasts, and still under Regigigas's own 670: the master stays above what it made. *Default if the user doesn't say*: build them.
- **Latias and Latios: maybe, as a pair or not at all**, each at level 160 with the other in the party. They are 600 already, so the rise is **+60, to the same 660** as every region's legendary set, under Rayquaza's 680 and their own Megas' 700. *Default*: build them.
- **Kyogre, Groudon and Rayquaza: no, as a set.** The cover legendaries of the region, 670 and 680, and each already has its further stage: the two Primal Reversions and Rayquaza's Mega.
- **Jirachi and Deoxys: no.** The two mythicals stand outside any stage, as Mew and Celebi: the one that wakes for a week in a thousand years, and the one whose four formes are its changes.
- **Shedinja: no.** Its whole design is 1 HP behind Wonder Guard; it is the husk Nincada leaves, not a creature that grows, and a rise anywhere but HP only makes the same Shedinja.
- **Castform: no**, as batch 2's Unown: an evolution would be four more weather forms of a Pokémon whose design is to change with the sky.

## The table: every qualifying species

The user may strike rows here before reading the designs. "No." is our number; "Total" is the stats the game reads now, and ours.

| # | Was | Ours | No. | Typing (was → ours) | Total | Evolves | Say |
|---|---|---|---|---|---|---|---|
| 1 | Mightyena (262) | Hyaenarch | 2262 | Dark, kept | 420 → 500 | level 142, at night | strong yes: the first route's hunter stops at 420; the leader of the pack it hunts in is the next step |
| 2 | Linoone (264) | Streakoon | 2264 | Normal → **Normal/Ground** | 420 → 500 | level 140, knowing Belly Drum | strong yes: Hoenn's own Linoone never went on (only the Galarian one became Obstagoon); it runs in straight lines, and a straight line under the earth is a burrow |
| 3 | Swellow (277) | Swiftarrow | 2277 | Normal/Flying, kept | 455 → 535 | level 145, by day | yes: the swallow that dives at its prey; past 100 it is all arrow |
| 4 | Pelipper (279) | Pelicaptain | 2279 | Water/Flying, kept | 440 → 520 | level 145, in the rain | strong yes: a total of 440 on the coast's bird; it carries things in its beak, and past 100 it carries a harbour's worth |
| 5 | Masquerain (284) | Ocellwing | 2284 | Bug/Flying, kept | 454 → 534 | level 142, holding a Silver Powder (used up) | strong yes: the moth whose eyes are on its antennae; four eyes become a whole wing of them |
| 6 | Breloom (286) | Sporepugil | 2286 | Grass/Fighting, kept | 460 → 540 | level 148, holding a Toxic Orb (used up) | strong yes: the mushroom boxer; Poison Heal is its best-known ability, and a Toxic Orb is how it is used |
| 7 | Ninjask (291) | Shinobuzz | 2291 | Bug/Flying, kept | 456 → 536 | level 145, at night | maybe: the ninja that moves too fast to see; but Speed Boost on the fastest Pokémon of Hoenn is already all it needs (see Balance) |
| – | Shedinja (292) | none | – | Bug/Ghost | 236 | – | no: its 1 HP is the whole of it (Wonder Guard on a husk), and a husk is what was left behind, not a creature that grows; more Attack would only make a better Shedinja of the same kind |
| 8 | Hariyama (297) | Yokozan | 2297 | Fighting, kept | 474 → 554 | level 150, holding a Black Belt (used up) | yes: the sumo wrestler grown into the grand champion who wears the rope |
| 9 | Delcatty (301) | Felinelle | 2301 | Normal → **Normal/Fairy** | 400 → 480 | a Moon Stone used on it at level 140 or above | strong yes: a total of 400, among the weakest last stages of any region; the moon's stone again, as Helianthra's sun stone |
| 10 | Sableye (302) | Gemwraith | 2302 | Dark/Ghost, kept | 380 → 460 | level 142, holding a Star Piece (used up) | strong yes: a total of 380 on the gem eater; one gem too many, and the gems grow through it |
| 11 | Mawile (303) | Deceivore | 2303 | Steel/Fairy, kept | 380 → 460 | level 142, holding a Razor Fang (used up) | strong yes: a total of 380; the Mega gave it two jaws, and the evolution keeps it a creature of its own with one larger |
| 12 | Medicham (308) | Ascetrance | 2308 | Fighting/Psychic, kept | 410 → 490 | level 145, by day | maybe: the meditator that floats in its trance; but Pure Power doubles whatever Attack it gains (see Balance) |
| 13 | Manectric (310) | Fulmenhound | 2310 | Electric, kept | 475 → 555 | level 150, in the rain | yes: the hound that makes thunderclouds in its mane; past 100 it carries its own storm |
| 14 | Plusle (311) | Anodion | 2311 | Electric → **Electric/Fairy** | 405 → 485 | level 142, with a Minun in the party | strong yes: with Minun's: two of 405, built and cheered as a pair; each goes further only with the other beside it |
| 15 | Minun (312) | Cathodion | 2312 | Electric → **Electric/Fairy** | 405 → 485 | level 142, with a Plusle in the party | strong yes: Plusle's pair |
| 16 | Volbeat (313) | Photurion | 2313 | Bug → **Bug/Fairy** | 430 → 510 | level 145, with an Illumise in the party | yes: with Illumise's: the fireflies that draw their figures in the night sky together |
| 17 | Illumise (314) | Lampyria | 2314 | Bug → **Bug/Fairy** | 430 → 510 | level 145, with a Volbeat in the party | yes: Volbeat's pair |
| 18 | Swalot (317) | Gulpgorge | 2317 | Poison, kept | 467 → 547 | level 146, holding a Black Sludge (used up) | yes: the stomach that swallows anything whole; it has room |
| 19 | Sharpedo (319) | Megalodax | 2319 | Water/Dark, kept | 460 → 540 | level 152 | yes: the ship-wrecker shark grown into the great shark of the old seas |
| 20 | Wailord (321) | Leviawhale | 2321 | Water, kept | 500 → 580 | level 155 | yes: the largest Pokémon of its day; past 100 it grows larger still, and its defences of 45 have room |
| 21 | Camerupt (323) | Volcamel | 2323 | Fire/Ground, kept | 460 → 540 | level 150, holding a Heat Rock (used up) | yes: the camel with volcanoes on its back; two humps grow into a range |
| 22 | Torkoal (324) | Smeltortoise | 2324 | Fire → **Fire/Steel** | 470 → 550 | level 148, by day | yes: the tortoise that burns coal in its shell; a forge grows round the fire |
| 23 | Grumpig (326) | Boarcane | 2326 | Psychic, kept | 470 → 550 | level 148, holding a Big Pearl (used up) | yes: the pig whose black pearls amplify its power; a greater pearl, a greater power |
| 24 | Spinda (327) | Whirlpanda | 2327 | Normal → **Normal/Fighting** | 360 → 440 | level 140, knowing Teeter Dance | strong yes: a total of 360, the lowest of Hoenn but Luvdisc's; the staggering dance is a fighting style, the drunken master's |
| 25 | Cacturne (332) | Saguarox | 2332 | Grass/Dark, kept | 475 → 555 | level 148, at night | yes: the scarecrow of the desert that hunts at night; it grows into the tall cactus of the desert's nights |
| 26 | Altaria (334) | Nimbusong | 2334 | Dragon/Flying, kept | 490 → 570 | level 152, with friendship 220 or more | yes: the bird that sings from the clouds; a choir of cloud |
| 27 | Zangoose (335) | Ravagoose | 2335 | Normal → **Normal/Fighting** | 458 → 538 | level 148, after knocking out three Seviper | yes: with Seviper's: the two rivals of Route 114, each grown by beating the other |
| 28 | Seviper (336) | Scimitail | 2336 | Poison → **Poison/Dark** | 458 → 538 | level 148, after knocking out three Zangoose | yes: Zangoose's pair |
| 29 | Lunatone (337) | Selenolith | 2337 | Rock/Psychic, kept | 460 → 540 | level 150, holding a Moon Stone at night (used up) | yes: with Solrock's: the meteorites of the moon and the sun, each whole at last |
| 30 | Solrock (338) | Heliolith | 2338 | Rock/Psychic, kept | 460 → 540 | level 150, holding a Sun Stone by day (used up) | yes: Lunatone's pair |
| 31 | Whiscash (340) | Namazuro | 2340 | Water/Ground, kept | 468 → 548 | level 148, knowing Earthquake | yes: the catfish of the old stories that shakes the earth; a Whiscash that knows Earthquake becomes it |
| 32 | Crawdaunt (342) | Crayronin | 2342 | Water/Dark, kept | 468 → 548 | level 148, knowing Crabhammer | yes: the rogue crayfish grown into a masterless swordsman |
| 33 | Claydol (344) | Dogunarch | 2344 | Ground/Psychic, kept | 500 → 580 | level 152 | yes: the ancient clay doll grown into the idol it was made after |
| 34 | Cradily (346) | Crinolith | 2346 | Rock/Grass, kept | 495 → 575 | level 150, holding a Root Fossil (used up) | yes: with Armaldo's, as batch 1's two fossils: the ancient sea lily rooted again |
| 35 | Armaldo (348) | Anomalorex | 2348 | Rock/Bug, kept | 495 → 575 | level 150, holding a Claw Fossil (used up) | yes: Cradily's pair |
| 36 | Milotic (350) | Prismaria | 2350 | Water → **Water/Fairy** | 540 → 600 | level 155, holding a Prism Scale (used up) | yes: the most beautiful Pokémon of its day; at 540 it rises only to the 600 ceiling |
| – | Castform (351) | none | – | Normal | 420 | – | no: four weather forms and Forecast; an evolution would be four more forms of a Pokémon whose design is to change with the sky, not to grow |
| 37 | Kecleon (352) | Chromeleon | 2352 | Normal → **Normal/Ghost** | 440 → 520 | level 145, knowing Shadow Sneak | yes: the chameleon that can turn invisible; past 100 it vanishes for good |
| 38 | Banette (354) | Effigrim | 2354 | Ghost, kept | 455 → 535 | level 146, holding a Spell Tag at night (used up) | yes: the thrown-away doll's grudge grows past 100 |
| 39 | Tropius (357) | Musadon | 2357 | Grass/Flying → **Grass/Dragon** | 460 → 540 | level 148, by day | yes: the fruit-bearing flyer, grown into the great beast of the orchards |
| 40 | Chimecho (358) | Carillone | 2358 | Psychic, kept | 455 → 535 | level 145, with friendship 220 or more, at night | yes: the wind chime grows into a carillon of bells; Chingling's own friendship at night takes it on |
| 41 | Absol (359) | Omenblade | 2359 | Dark, kept | 465 → 545 | level 150, holding a Scope Lens (used up) | strong yes: the beloved Pokémon that foresees disasters; at 465 it has room, and Super Luck has its item |
| 42 | Glalie (362) | Bergmaw | 2362 | Ice, kept | 480 → 560 | level 145, holding a Never-Melt Ice (used up) | yes: the round face of ice; its sister line got Froslass, and Glalie itself never went on |
| 43 | Huntail (367) | Gulpereel | 2367 | Water → **Water/Dark** | 485 → 565 | level 150, holding a Deep Sea Tooth (used up) | yes: with Gorebyss: Clamperl's two ends, each by the item that made it |
| 44 | Gorebyss (368) | Sirenyss | 2368 | Water → **Water/Fairy** | 485 → 565 | level 150, holding a Deep Sea Scale (used up) | yes: Huntail's pair |
| 45 | Relicanth (369) | Aeonfin | 2369 | Water/Rock, kept | 485 → 565 | level 158 | yes: the fish that hasn't changed in a hundred million years changes at last, late |
| 46 | Luvdisc (370) | Heartreef | 2370 | Water → **Water/Fairy** | 330 → 410 | level 140, holding a Heart Scale (used up) | strong yes: a total of 330, the lowest of Hoenn; its scale is the Heart Scale, which grows into a reef of its own |
| 47 | Regirock (377) | Regimonolith | 2377 | Rock, kept | 580 → 660 | level 160, with a Regigigas in the party | maybe: the three Regis as a trio or not at all: Regigigas made them, so Regigigas beside them takes them further |
| 48 | Regice (378) | Regiglacier | 2378 | Ice, kept | 580 → 660 | level 160, with a Regigigas in the party | maybe: with the Regis |
| 49 | Registeel (379) | Regiferrum | 2379 | Steel, kept | 580 → 660 | level 160, with a Regigigas in the party | maybe: with the Regis |
| 50 | Latias (380) | Aurelias | 2380 | Dragon/Psychic, kept | 600 → 660 | level 160, with a Latios in the party | maybe: the eon pair as a pair or not at all; each goes further only beside the other |
| 51 | Latios (381) | Aurelios | 2381 | Dragon/Psychic, kept | 600 → 660 | level 160, with a Latias in the party | maybe: Latias's pair |
| – | Kyogre (382) | none | – | Water | 670 | – | no: 670 and its Primal Reversion, a cover legendary of the region; its further stage is the Primal one |
| – | Groudon (383) | none | – | Ground | 670 | – | no: 670 and its Primal Reversion, as Kyogre |
| – | Rayquaza (384) | none | – | Dragon/Flying | 680 | – | no: 680 and its Mega Evolution, the third of the weather trio and the highest of the region |
| – | Jirachi (385) | none | – | Steel/Psychic | 600 | – | no: the mythical that wakes for a week in a thousand years is outside any stage, as Mew and Celebi |
| – | Deoxys (386) | none | – | Psychic | 600 | – | no: a virus from space in four formes, 600 in each; its formes are its changes, and a mythical |

In all: 13 strong yes (Plusle's and Minun's two among them), 31 yes, 7 maybe (the three Regis and the eon pair as sets among them) and 7 no.

## The designs

In National Pokédex order. Each entry: the typing and why, the category word, number, size, the stats (the species' as the game reads them → ours) with where the rise goes, the abilities, the evolution with the row it would be in `species.json`, and the look.

### 1. Hyaenarch, from Mightyena (262): strong yes

- **Typing**: Dark, kept. *Alternative*: Dark/Fighting.
- **The Packleader Pokémon**, No. 2262. 1.4 m, 60.0 kg (Mightyena 1 m, 37 kg).
- **Stats**: 70 / 90 / 70 / 60 / 60 / 70 = 420 → **85 / 115 / 85 / 60 / 70 / 85 = 500** (+15 HP +25 Atk +15 Def +10 SpD +15 Spe): the rise goes to its bite, its bulk and its speed, a pack leader's.
- **Abilities**: Intimidate, Quick Feet, hidden Moxie (was Intimidate, Quick Feet, hidden Moxie).
- **Evolution**: Mightyena at level 142, at night. Data: `{"targetSpecies":"Hyaenarch","method":"LevelNight","level":142}`.
- **Look**: from our grey and black hyena: bigger in the shoulders, the black mane grown into a long crest down its back, a pale scar over one eye, the red eyes kept and the tail heavier. **New feature**: the crest down its back.

### 2. Streakoon, from Linoone (264): strong yes

- **Typing**: Normal → **Normal/Ground**. *Alternative*: Normal/Dark (the way the Galarian line went).
- **The Bolt Pokémon**, No. 2264. 0.8 m, 45.0 kg (Linoone 0.5 m, 32.5 kg).
- **Stats**: 78 / 70 / 61 / 50 / 61 / 100 = 420 → **93 / 95 / 71 / 50 / 71 / 120 = 500** (+15 HP +25 Atk +10 Def +10 SpD +20 Spe): the rise goes to its speed and its Attack, which Belly Drum doubles.
- **Abilities**: Pickup, Gluttony, hidden Quick Feet (was Pickup, Gluttony, hidden Quick Feet).
- **Evolution**: Linoone at level 140, knowing Belly Drum. Data: `{"targetSpecies":"Streakoon","method":"LevelKnowsMove","level":140,"move":"Belly Drum"}`.
- **Look**: from our long brown and cream raccoon dog: lower and longer, its zigzag stripes pulled into straight bands along the body, broad digging claws on the forefeet and a blunt snout. **New feature**: the digging claws.

### 3. Swiftarrow, from Swellow (277): yes

- **Typing**: Normal/Flying, kept. *Alternative*: Fighting/Flying.
- **The Arrow Pokémon**, No. 2277. 1.0 m, 28.0 kg (Swellow 0.7 m, 19.8 kg).
- **Stats**: 60 / 85 / 60 / 75 / 50 / 125 = 455 → **75 / 110 / 75 / 75 / 65 / 135 = 535** (+15 HP +25 Atk +15 Def +15 SpD +10 Spe): the rise goes to its Attack, then its HP, Defense and Sp. Def.
- **Abilities**: Guts, hidden Scrappy (was Guts, hidden Scrappy).
- **Evolution**: Swellow at level 145, by day. Data: `{"targetSpecies":"Swiftarrow","method":"LevelDay","level":145}`.
- **Look**: from our blue swallow with its red breast and forked tail: longer and narrower, the tail's forks twice as long and swept like fletching, the red of the breast running into a point at the beak. **New feature**: the long fletched tail.

### 4. Pelicaptain, from Pelipper (279): strong yes

- **Typing**: Water/Flying, kept. *Alternative*: Water/Normal.
- **The Harbour Pokémon**, No. 2279. 1.6 m, 45.0 kg (Pelipper 1.2 m, 28 kg).
- **Stats**: 60 / 50 / 100 / 95 / 70 / 65 = 440 → **80 / 50 / 110 / 120 / 90 / 70 = 520** (+20 HP +10 Def +25 SpA +20 SpD +5 Spe): the rise goes to its Sp. Atk, its HP and its Sp. Def.
- **Abilities**: **Drizzle**, hidden Rain Dish (was Keen Eye, hidden Rain Dish).
- **Evolution**: Pelipper at level 145, in the rain. Data: `{"targetSpecies":"Pelicaptain","method":"LevelInRain","level":145}`.
- **Look**: from our white pelican with its great yellow bill: larger, the bill deeper with a band of blue round its rim, a sailor's cap of darker feathers on its head and wings as wide again. **New feature**: the cap of dark feathers.

### 5. Ocellwing, from Masquerain (284): strong yes

- **Typing**: Bug/Flying, kept. *Alternative*: Bug/Water.
- **The Eyespot Pokémon**, No. 2284. 1.2 m, 8.0 kg (Masquerain 0.8 m, 3.6 kg).
- **Stats**: 70 / 60 / 62 / 100 / 82 / 80 = 454 → **85 / 60 / 77 / 125 / 97 / 90 = 534** (+15 HP +15 Def +25 SpA +15 SpD +10 Spe): the rise goes to its Sp. Atk, then its Defense and Sp. Def.
- **Abilities**: Intimidate, hidden Unnerve (was Intimidate, hidden Unnerve).
- **Evolution**: Masquerain at level 142, holding a Silver Powder (used up). Data: `{"targetSpecies":"Ocellwing","method":"LevelHoldingItem","level":142,"item":"Silver Powder"}`.
- **Look**: from our pale moth with its four blue wings and false eyes: two more wings, each with a false eye (decals), the antennae's eyes larger and the body a deeper blue. **New feature**: the third pair of wings.

### 6. Sporepugil, from Breloom (286): strong yes

- **Typing**: Grass/Fighting, kept. *Alternative*: Grass/Poison.
- **The Spore Boxer Pokémon**, No. 2286. 1.5 m, 55.0 kg (Breloom 1.2 m, 39.2 kg).
- **Stats**: 60 / 130 / 80 / 60 / 60 / 70 = 460 → **80 / 145 / 95 / 60 / 75 / 85 = 540** (+20 HP +15 Atk +15 Def +15 SpD +15 Spe): the rise goes to its HP, Defense, Sp. Def and Speed; Attack was already 130.
- **Abilities**: Effect Spore, Poison Heal, hidden Technician (was Effect Spore, Poison Heal, hidden Technician).
- **Evolution**: Breloom at level 148, holding a Toxic Orb (used up). Data: `{"targetSpecies":"Sporepugil","method":"LevelHoldingItem","level":148,"item":"Toxic Orb"}`.
- **Look**: from our green kangaroo-like mushroom with its cap and stretching arms: taller, the cap broader with a rim of spore pockets (Glow, pale green), the arms' ends grown into gloves of fungus and the tail ending in a spore pod. **New feature**: the gloves of fungus.

### 7. Shinobuzz, from Ninjask (291): maybe

- **Typing**: Bug/Flying, kept. *Alternative*: Bug/Dark.
- **The Shadow Ninja Pokémon**, No. 2291. 1.0 m, 15.0 kg (Ninjask 0.8 m, 12 kg).
- **Stats**: 61 / 90 / 45 / 50 / 50 / 160 = 456 → **76 / 120 / 65 / 50 / 65 / 160 = 536** (+15 HP +30 Atk +20 Def +15 SpD): the rise goes to its Attack, HP and defences; Speed stays 160.
- **Abilities**: Speed Boost, hidden Infiltrator (was Speed Boost, hidden Infiltrator).
- **Evolution**: Ninjask at level 145, at night. Data: `{"targetSpecies":"Shinobuzz","method":"LevelNight","level":145}`.
- **Look**: from our yellow and black cicada with its two pairs of wings: darker, nearly black with the yellow kept as a mask across its eyes, a third, smaller pair of wings, and two curved blades at its forelegs. **New feature**: the blades at its forelegs.

### 8. Yokozan, from Hariyama (297): yes

- **Typing**: Fighting, kept. *Alternative*: Fighting/Ground.
- **The Grand Champion Pokémon**, No. 2297. 2.6 m, 330.0 kg (Hariyama 2.3 m, 253.8 kg).
- **Stats**: 144 / 120 / 60 / 40 / 60 / 50 = 474 → **154 / 135 / 80 / 40 / 80 / 65 = 554** (+10 HP +15 Atk +20 Def +20 SpD +15 Spe): the rise goes to its Defense and Sp. Def, then its Attack and Speed.
- **Abilities**: Thick Fat, Guts, hidden Sheer Force (was Thick Fat, Guts, hidden Sheer Force).
- **Evolution**: Hariyama at level 150, holding a Black Belt (used up). Data: `{"targetSpecies":"Yokozan","method":"LevelHoldingItem","level":150,"item":"Black Belt"}`.
- **Look**: from our heavy Hariyama with its great palms: larger, a thick white rope belt with paper streamers at the front (the grand champion's), its hair in a top knot and the palms broader. **New feature**: the rope belt.

### 9. Felinelle, from Delcatty (301): strong yes

- **Typing**: Normal → **Normal/Fairy**. *Alternative*: Normal/Psychic.
- **The Elegance Pokémon**, No. 2301. 1.3 m, 40.0 kg (Delcatty 1.1 m, 32.6 kg).
- **Stats**: 70 / 65 / 65 / 55 / 55 / 90 = 400 → **85 / 80 / 75 / 70 / 70 / 100 = 480** (+15 HP +15 Atk +10 Def +15 SpA +15 SpD +10 Spe): the rise is spread evenly over every stat.
- **Abilities**: Cute Charm, Normalize, hidden Wonder Skin (was Cute Charm, Normalize, hidden Wonder Skin).
- **Evolution**: Delcatty at a Moon Stone used on it at level 140 or above. Data: `{"targetSpecies":"Felinelle","method":"UseItem","level":140,"item":"Moon Stone"}`.
- **Look**: from our lilac cat with its cream collar of fur: longer and sleeker, the collar grown into a ruff, its tail ending in two plumes, a small crescent on its brow (Glow, pale). **New feature**: the crescent on its brow.

### 10. Gemwraith, from Sableye (302): strong yes

- **Typing**: Dark/Ghost, kept. *Alternative*: Ghost/Rock.
- **The Gem Hoard Pokémon**, No. 2302. 0.8 m, 20.0 kg (Sableye 0.5 m, 11 kg).
- **Stats**: 50 / 75 / 75 / 65 / 65 / 50 = 380 → **70 / 90 / 90 / 80 / 80 / 50 = 460** (+20 HP +15 Atk +15 Def +15 SpA +15 SpD): the rise goes to its HP and both defences, then its attacks.
- **Abilities**: Keen Eye, Stall, hidden Prankster (was Keen Eye, Stall, hidden Prankster).
- **Evolution**: Sableye at level 142, holding a Star Piece (used up). Data: `{"targetSpecies":"Gemwraith","method":"LevelHoldingItem","level":142,"item":"Star Piece"}`.
- **Look**: from our crouching purple imp with gem eyes: larger, crystals grown out of its back and shoulders (Glow, blue and red), the gem on its chest bigger and its claws longer. **New feature**: the crystals on its back.

### 11. Deceivore, from Mawile (303): strong yes

- **Typing**: Steel/Fairy, kept. *Alternative*: Steel/Dark.
- **The Twin Maw Pokémon**, No. 2303. 0.9 m, 30.0 kg (Mawile 0.6 m, 11.5 kg).
- **Stats**: 50 / 85 / 85 / 55 / 55 / 50 = 380 → **70 / 105 / 100 / 55 / 70 / 60 = 460** (+20 HP +20 Atk +15 Def +15 SpD +10 Spe): the rise goes to its Attack, HP and Defense.
- **Abilities**: Hyper Cutter, Intimidate, hidden Sheer Force (was Hyper Cutter, Intimidate, hidden Sheer Force).
- **Evolution**: Mawile at level 142, holding a Razor Fang (used up). Data: `{"targetSpecies":"Deceivore","method":"LevelHoldingItem","level":142,"item":"Razor Fang"}`.
- **Look**: from our small yellow Mawile with the great steel jaw behind its head: the jaw longer with a row of steel teeth along its whole edge and two horns at its root, the yellow dress-like body longer. **New feature**: the horns at the jaw's root.

### 12. Ascetrance, from Medicham (308): maybe

- **Typing**: Fighting/Psychic, kept. *Alternative*: Psychic/Fairy.
- **The Trance Pokémon**, No. 2308. 1.5 m, 38.0 kg (Medicham 1.3 m, 31.5 kg).
- **Stats**: 60 / 60 / 75 / 60 / 75 / 80 = 410 → **75 / 80 / 85 / 70 / 90 / 90 = 490** (+15 HP +20 Atk +10 Def +10 SpA +15 SpD +10 Spe): the rise goes to its Attack, HP, Sp. Def and Speed.
- **Abilities**: Pure Power, hidden Telepathy (was Pure Power, hidden Telepathy).
- **Evolution**: Medicham at level 145, by day. Data: `{"targetSpecies":"Ascetrance","method":"LevelDay","level":145}`.
- **Look**: from our grey and red meditating Medicham: taller and leaner, floating cross-legged, a third ball on its brow, its red trousers grown into a sash that drifts, and a ring of light behind its head (Glow, pale gold). The female's larger ear-balls are kept. **New feature**: the ring of light.

### 13. Fulmenhound, from Manectric (310): yes

- **Typing**: Electric, kept. *Alternative*: Electric/Dark.
- **The Thunderhead Pokémon**, No. 2310. 1.8 m, 60.0 kg (Manectric 1.5 m, 40.2 kg).
- **Stats**: 70 / 75 / 60 / 105 / 60 / 105 = 475 → **80 / 75 / 80 / 125 / 80 / 115 = 555** (+10 HP +20 Def +20 SpA +20 SpD +10 Spe): the rise goes to its Sp. Atk and both defences.
- **Abilities**: Static, Lightning Rod, hidden Minus (was Static, Lightning Rod, hidden Minus).
- **Evolution**: Manectric at level 150, in the rain. Data: `{"targetSpecies":"Fulmenhound","method":"LevelInRain","level":150}`.
- **Look**: from our blue and yellow hound with its swept mane: larger, the mane grown into a cloud that sparks (Glow, yellow), yellow bolts along its legs and the tail split in three points. **New feature**: the cloud of its mane.

### 14. Anodion, from Plusle (311): strong yes

- **Typing**: Electric → **Electric/Fairy**. *Alternative*: Electric alone, for both.
- **The Anode Pokémon**, No. 2311. 0.6 m, 7.0 kg (Plusle 0.4 m, 4.2 kg).
- **Stats**: 60 / 50 / 40 / 85 / 75 / 95 = 405 → **75 / 50 / 55 / 110 / 90 / 105 = 485** (+15 HP +15 Def +25 SpA +15 SpD +10 Spe): the rise goes to its Sp. Atk, then its HP, Defense and Sp. Def.
- **Abilities**: Plus, hidden Lightning Rod (was Plus, hidden Lightning Rod).
- **Evolution**: Plusle at level 142, with a Minun in the party. Data: `{"targetSpecies":"Anodion","method":"LevelWithSpeciesInParty","level":142,"species":"Minun"}`.
- **Look**: from our cream rabbit with red cheeks and a plus on its tail: taller, its ears longer with red tips, the plus on its tail and cheeks grown into small pompoms (Glow, red). **New feature**: the pompoms.

### 15. Cathodion, from Minun (312): strong yes

- **Typing**: Electric → **Electric/Fairy**. *Alternative*: Electric alone, for both.
- **The Cathode Pokémon**, No. 2312. 0.6 m, 7.0 kg (Minun 0.4 m, 4.2 kg).
- **Stats**: 60 / 40 / 50 / 75 / 85 / 95 = 405 → **75 / 40 / 65 / 100 / 100 / 105 = 485** (+15 HP +15 Def +25 SpA +15 SpD +10 Spe): the rise goes to its Sp. Atk, then its HP, Defense and Sp. Def.
- **Abilities**: Minus, hidden Volt Absorb (was Minus, hidden Volt Absorb).
- **Evolution**: Minun at level 142, with a Plusle in the party. Data: `{"targetSpecies":"Cathodion","method":"LevelWithSpeciesInParty","level":142,"species":"Plusle"}`.
- **Look**: from our cream rabbit with blue cheeks and a minus on its tail: Anodion's build in blue, the minus grown into pompoms (Glow, blue). **New feature**: the pompoms.

### 16. Photurion, from Volbeat (313): yes

- **Typing**: Bug → **Bug/Fairy**. *Alternative*: Bug/Electric, for both.
- **The Signal Light Pokémon**, No. 2313. 1.0 m, 24.0 kg (Volbeat 0.7 m, 17.7 kg).
- **Stats**: 65 / 73 / 75 / 47 / 85 / 85 = 430 → **80 / 93 / 85 / 47 / 100 / 105 = 510** (+15 HP +20 Atk +10 Def +15 SpD +20 Spe): the rise goes to its Attack, Speed and Sp. Def.
- **Abilities**: Illuminate, Swarm, hidden Prankster (was Illuminate, Swarm, hidden Prankster).
- **Evolution**: Volbeat at level 145, with an Illumise in the party. Data: `{"targetSpecies":"Photurion","method":"LevelWithSpeciesInParty","level":145,"species":"Illumise"}`.
- **Look**: from our upright firefly with its round blue tail light: taller, the light in its tail brighter and ringed (Glow, yellow), its antennae lit at the tips and its wings longer. **New feature**: the ringed tail light.

### 17. Lampyria, from Illumise (314): yes

- **Typing**: Bug → **Bug/Fairy**. *Alternative*: Bug/Electric, for both.
- **The Signal Light Pokémon**, No. 2314. 0.9 m, 24.0 kg (Illumise 0.6 m, 17.7 kg).
- **Stats**: 65 / 47 / 75 / 73 / 85 / 85 = 430 → **80 / 47 / 85 / 93 / 100 / 105 = 510** (+15 HP +10 Def +20 SpA +15 SpD +20 Spe): the rise goes to its Sp. Atk, Speed and Sp. Def.
- **Abilities**: Oblivious, Tinted Lens, hidden Prankster (was Oblivious, Tinted Lens, hidden Prankster).
- **Evolution**: Illumise at level 145, with a Volbeat in the party. Data: `{"targetSpecies":"Lampyria","method":"LevelWithSpeciesInParty","level":145,"species":"Volbeat"}`.
- **Look**: from our purple firefly with its antennae curled: a dress of wing-cases grown longer, a scent-like trail of light motes behind it (Glow, pale yellow) and the antennae's curls lit. **New feature**: the trail of light.

### 18. Gulpgorge, from Swalot (317): yes

- **Typing**: Poison, kept. *Alternative*: Poison/Normal.
- **The Bottomless Pokémon**, No. 2317. 2.0 m, 120.0 kg (Swalot 1.7 m, 80 kg).
- **Stats**: 100 / 73 / 83 / 73 / 83 / 55 = 467 → **120 / 83 / 98 / 83 / 98 / 65 = 547** (+20 HP +10 Atk +15 Def +10 SpA +15 SpD +10 Spe): the rise spreads over everything, most to HP.
- **Abilities**: Liquid Ooze, Sticky Hold, hidden Gluttony (was Liquid Ooze, Sticky Hold, hidden Gluttony).
- **Evolution**: Swalot at level 146, holding a Black Sludge (used up). Data: `{"targetSpecies":"Gulpgorge","method":"LevelHoldingItem","level":146,"item":"Black Sludge"}`.
- **Look**: from our purple bag of a body with its whiskers and black diamonds: larger and heavier, the diamonds grown into bands, its lips fuller and the whiskers longer. The female's shorter whiskers are kept. **New feature**: the bands where the diamonds were.

### 19. Megalodax, from Sharpedo (319): yes

- **Typing**: Water/Dark, kept. *Alternative*: Water/Steel.
- **The Abyss Shark Pokémon**, No. 2319. 2.6 m, 180.0 kg (Sharpedo 1.8 m, 88.8 kg).
- **Stats**: 70 / 120 / 40 / 95 / 40 / 95 = 460 → **90 / 140 / 60 / 95 / 60 / 95 = 540** (+20 HP +20 Atk +20 Def +20 SpD): the rise goes to its Attack, HP and both defences, which were 40.
- **Abilities**: Rough Skin, hidden Speed Boost (was Rough Skin, hidden Speed Boost).
- **Evolution**: Sharpedo at level 152. Data: `{"targetSpecies":"Megalodax","level":152}`.
- **Look**: from our red and navy torpedo with its jaw of teeth: longer and heavier, a second row of teeth, the yellow star on its nose a scar, and its fins larger. **New feature**: the second row of teeth.

### 20. Leviawhale, from Wailord (321): yes

- **Typing**: Water, kept. *Alternative*: Water/Ice.
- **The Leviathan Pokémon**, No. 2321. 20.0 m, 500.0 kg (Wailord 14.5 m, 398 kg).
- **Stats**: 170 / 90 / 45 / 90 / 45 / 60 = 500 → **180 / 100 / 70 / 100 / 70 / 60 = 580** (+10 HP +10 Atk +25 Def +10 SpA +25 SpD): the rise goes to its Defense and Sp. Def, then its attacks; HP was 170.
- **Abilities**: Water Veil, Oblivious, hidden Pressure (was Water Veil, Oblivious, hidden Pressure).
- **Evolution**: Wailord at level 155. Data: `{"targetSpecies":"Leviawhale","level":155}`.
- **Look**: from our blue whale: longer, barnacles and pale scars on its back, a ridge of fins along the spine and the blowhole's spout kept as a mist (Glow, white). **New feature**: the ridge of fins.

### 21. Volcamel, from Camerupt (323): yes

- **Typing**: Fire/Ground, kept. *Alternative*: Fire/Rock.
- **The Caldera Pokémon**, No. 2323. 2.4 m, 320.0 kg (Camerupt 1.9 m, 220 kg).
- **Stats**: 70 / 100 / 70 / 105 / 75 / 40 = 460 → **90 / 110 / 90 / 125 / 85 / 40 = 540** (+20 HP +10 Atk +20 Def +20 SpA +10 SpD): the rise goes to its HP, Defense and Sp. Atk.
- **Abilities**: Magma Armor, Solid Rock, hidden Anger Point (was Magma Armor, Solid Rock, hidden Anger Point).
- **Evolution**: Camerupt at level 150, holding a Heat Rock (used up). Data: `{"targetSpecies":"Volcamel","method":"LevelHoldingItem","level":150,"item":"Heat Rock"}`.
- **Look**: from our red camel with its two volcano humps: a third, taller cone between the two, lava glowing in their craters (Glow, orange), and rock plates on its legs. The female's larger humps are kept. **New feature**: the third cone.

### 22. Smeltortoise, from Torkoal (324): yes

- **Typing**: Fire → **Fire/Steel**. *Alternative*: Fire/Ground.
- **The Smelter Pokémon**, No. 2324. 0.9 m, 150.0 kg (Torkoal 0.5 m, 80.4 kg).
- **Stats**: 70 / 85 / 140 / 85 / 70 / 20 = 470 → **90 / 95 / 150 / 105 / 90 / 20 = 550** (+20 HP +10 Atk +10 Def +20 SpA +20 SpD): the rise goes to its HP, Sp. Atk and Sp. Def.
- **Abilities**: White Smoke, hidden **Drought** (was White Smoke, hidden Shell Armor).
- **Evolution**: Torkoal at level 148, by day. Data: `{"targetSpecies":"Smeltortoise","method":"LevelDay","level":148}`.
- **Look**: from our orange tortoise with its coal shell and chimney holes: the shell banded in iron (Metal), the holes grown into short chimneys that smoke, the coal glowing between the bands (Glow, orange). **New feature**: the chimneys.

### 23. Boarcane, from Grumpig (326): yes

- **Typing**: Psychic, kept. *Alternative*: Psychic/Fairy.
- **The Pearl Seer Pokémon**, No. 2326. 1.3 m, 110.0 kg (Grumpig 0.9 m, 71.5 kg).
- **Stats**: 80 / 45 / 65 / 90 / 110 / 80 = 470 → **95 / 45 / 80 / 115 / 125 / 90 = 550** (+15 HP +15 Def +25 SpA +15 SpD +10 Spe): the rise goes to its Sp. Atk, then its HP, Defense and Sp. Def.
- **Abilities**: Thick Fat, Own Tempo, hidden Gluttony (was Thick Fat, Own Tempo, hidden Gluttony).
- **Evolution**: Grumpig at level 148, holding a Big Pearl (used up). Data: `{"targetSpecies":"Boarcane","method":"LevelHoldingItem","level":148,"item":"Big Pearl"}`.
- **Look**: from our black and purple pig standing on two legs with its pearls: taller, a third pearl on its brow, the two in its hands larger and glowing (Glow, violet) and tusks. **New feature**: the third pearl.

### 24. Whirlpanda, from Spinda (327): strong yes

- **Typing**: Normal → **Normal/Fighting**. *Alternative*: Normal/Psychic.
- **The Drunken Master Pokémon**, No. 2327. 1.4 m, 20.0 kg (Spinda 1.1 m, 5 kg).
- **Stats**: 60 / 60 / 60 / 60 / 60 / 60 = 360 → **75 / 80 / 75 / 60 / 75 / 75 = 440** (+15 HP +20 Atk +15 Def +15 SpD +15 Spe): the rise goes to its Attack, then its HP, Defense, Sp. Def and Speed.
- **Abilities**: Own Tempo, Tangled Feet, hidden Contrary (was Own Tempo, Tangled Feet, hidden Contrary).
- **Evolution**: Spinda at level 140, knowing Teeter Dance. Data: `{"targetSpecies":"Whirlpanda","method":"LevelKnowsMove","level":140,"move":"Teeter Dance"}`.
- **Look**: from our cream panda with its spiral eyes and red spots: taller, the spots kept by its personality as Spinda's are, a sash at its waist and a gourd at its hip. **New feature**: the gourd at its hip.

### 25. Saguarox, from Cacturne (332): yes

- **Typing**: Grass/Dark, kept. *Alternative*: Grass/Ground.
- **The Desert Stalker Pokémon**, No. 2332. 1.8 m, 110.0 kg (Cacturne 1.3 m, 77.4 kg).
- **Stats**: 70 / 115 / 60 / 115 / 60 / 55 = 475 → **85 / 130 / 80 / 125 / 75 / 60 = 555** (+15 HP +15 Atk +20 Def +10 SpA +15 SpD +5 Spe): the rise goes to its Defense, HP and Attack.
- **Abilities**: Sand Veil, hidden Water Absorb (was Sand Veil, hidden Water Absorb).
- **Evolution**: Cacturne at level 148, at night. Data: `{"targetSpecies":"Saguarox","method":"LevelNight","level":148}`.
- **Look**: from our green scarecrow cactus with its hat-like head: taller, its arms branching like a saguaro's, the hat broader with spines on its brim, and white flowers on its arms at night. The female's paler diamond is kept. **New feature**: the branching arms.

### 26. Nimbusong, from Altaria (334): yes

- **Typing**: Dragon/Flying, kept. *Alternative*: Dragon/Fairy (the Mega's).
- **The Choir Pokémon**, No. 2334. 1.5 m, 35.0 kg (Altaria 1.1 m, 20.6 kg).
- **Stats**: 75 / 70 / 90 / 70 / 105 / 80 = 490 → **90 / 80 / 105 / 85 / 120 / 90 = 570** (+15 HP +10 Atk +15 Def +15 SpA +15 SpD +10 Spe): the rise is spread over every stat.
- **Abilities**: Natural Cure, hidden Cloud Nine (was Natural Cure, hidden Cloud Nine).
- **Evolution**: Altaria at level 152, with friendship 220 or more. Data: `{"targetSpecies":"Nimbusong","method":"Friendship","level":152}`.
- **Look**: from our blue bird with its wings of cloud: larger, the cloud wings grown into a mantle of cloud round its whole body, two long tail plumes and a crest of three feathers. **New feature**: the mantle of cloud.

### 27. Ravagoose, from Zangoose (335): yes

- **Typing**: Normal → **Normal/Fighting**. *Alternative*: Normal alone.
- **The Viper Bane Pokémon**, No. 2335. 1.5 m, 55.0 kg (Zangoose 1.3 m, 40.3 kg).
- **Stats**: 73 / 115 / 60 / 60 / 60 / 90 = 458 → **88 / 135 / 75 / 60 / 75 / 105 = 538** (+15 HP +20 Atk +15 Def +15 SpD +15 Spe): the rise goes to its Attack, Speed, HP and both defences.
- **Abilities**: Immunity, hidden Toxic Boost (was Immunity, hidden Toxic Boost).
- **Evolution**: Zangoose at level 148, after knocking out three Seviper. Data: `{"targetSpecies":"Ravagoose","method":"LevelAfterDefeating","level":148,"species":"Seviper","value":3}`.
- **Look**: from our white mongoose with its red scars: larger, the scars spread into stripes along its back, the claws longer, and its ears tufted. **New feature**: the stripes of scars.

### 28. Scimitail, from Seviper (336): yes

- **Typing**: Poison → **Poison/Dark**. *Alternative*: Poison alone.
- **The Blade Viper Pokémon**, No. 2336. 3.4 m, 80.0 kg (Seviper 2.7 m, 52.5 kg).
- **Stats**: 73 / 100 / 60 / 100 / 60 / 65 = 458 → **88 / 115 / 75 / 115 / 75 / 70 = 538** (+15 HP +15 Atk +15 Def +15 SpA +15 SpD +5 Spe): the rise is spread over every stat.
- **Abilities**: Shed Skin, hidden Infiltrator (was Shed Skin, hidden Infiltrator).
- **Evolution**: Seviper at level 148, after knocking out three Zangoose. Data: `{"targetSpecies":"Scimitail","method":"LevelAfterDefeating","level":148,"species":"Zangoose","value":3}`.
- **Look**: from our black snake with its gold bands and bladed tail: longer, the tail's blade grown into a curved scimitar, the gold bands edged in red and the fangs longer. **New feature**: the scimitar tail.

### 29. Selenolith, from Lunatone (337): yes

- **Typing**: Rock/Psychic, kept. *Alternative*: Rock/Fairy, and Rock/Fire for Solrock.
- **The Full Moon Pokémon**, No. 2337. 1.4 m, 250.0 kg (Lunatone 1 m, 168 kg).
- **Stats**: 90 / 55 / 65 / 95 / 85 / 70 = 460 → **100 / 55 / 80 / 120 / 100 / 85 = 540** (+10 HP +15 Def +25 SpA +15 SpD +15 Spe): the rise goes to its Sp. Atk, Defense, Sp. Def and Speed.
- **Abilities**: Levitate (was Levitate).
- **Evolution**: Lunatone at level 150, holding a Moon Stone at night (used up). Data: `{"targetSpecies":"Selenolith","method":"LevelHoldingItemNight","level":150,"item":"Moon Stone"}`.
- **Look**: from our pale crescent of rock: the crescent closed into a full disc with the crescent's shape kept as a raised edge, its red eyes kept, craters across its face. **New feature**: the full disc.

### 30. Heliolith, from Solrock (338): yes

- **Typing**: Rock/Psychic, kept. *Alternative*: Rock/Fire, and Rock/Fairy for Lunatone.
- **The Corona Pokémon**, No. 2338. 1.6 m, 240.0 kg (Solrock 1.2 m, 154 kg).
- **Stats**: 90 / 95 / 85 / 55 / 65 / 70 = 460 → **100 / 120 / 100 / 55 / 80 / 85 = 540** (+10 HP +25 Atk +15 Def +15 SpD +15 Spe): the rise goes to its Attack, Defense, Sp. Def and Speed.
- **Abilities**: Levitate (was Levitate).
- **Evolution**: Solrock at level 150, holding a Sun Stone by day (used up). Data: `{"targetSpecies":"Heliolith","method":"LevelHoldingItemDay","level":150,"item":"Sun Stone"}`.
- **Look**: from our orange sun of rock with its points: twice as many points, a corona of light round them (Glow, orange) and a band of darker rock across its face. **New feature**: the corona.

### 31. Namazuro, from Whiscash (340): yes

- **Typing**: Water/Ground, kept. *Alternative*: Ground alone.
- **The Quake Catfish Pokémon**, No. 2340. 1.6 m, 90.0 kg (Whiscash 0.9 m, 23.6 kg).
- **Stats**: 110 / 78 / 73 / 76 / 71 / 60 = 468 → **125 / 98 / 88 / 86 / 86 / 65 = 548** (+15 HP +20 Atk +15 Def +10 SpA +15 SpD +5 Spe): the rise goes to its Attack, HP and Sp. Def.
- **Abilities**: Oblivious, Anticipation, hidden Hydration (was Oblivious, Anticipation, hidden Hydration).
- **Evolution**: Whiscash at level 148, knowing Earthquake. Data: `{"targetSpecies":"Namazuro","method":"LevelKnowsMove","level":148,"move":"Earthquake"}`.
- **Look**: from our blue catfish with its long whiskers: larger, the whiskers grown into four long barbels, a rocky plate on its head and cracks of light along its flanks (Glow, pale yellow). **New feature**: the rocky plate on its head.

### 32. Crayronin, from Crawdaunt (342): yes

- **Typing**: Water/Dark, kept. *Alternative*: Water/Fighting.
- **The Rogue Blade Pokémon**, No. 2342. 1.4 m, 50.0 kg (Crawdaunt 1.1 m, 32.8 kg).
- **Stats**: 63 / 120 / 85 / 90 / 55 / 55 = 468 → **83 / 140 / 95 / 90 / 75 / 65 = 548** (+20 HP +20 Atk +10 Def +20 SpD +10 Spe): the rise goes to its Attack, HP and Sp. Def.
- **Abilities**: Hyper Cutter, Shell Armor, hidden Adaptability (was Hyper Cutter, Shell Armor, hidden Adaptability).
- **Evolution**: Crawdaunt at level 148, knowing Crabhammer. Data: `{"targetSpecies":"Crayronin","method":"LevelKnowsMove","level":148,"move":"Crabhammer"}`.
- **Look**: from our red crayfish with its great pincers and star on its head: its pincers longer and flattened like blades, the star grown into a crest, a shell like a straw hat on its back. **New feature**: the blades of its pincers.

### 33. Dogunarch, from Claydol (344): yes

- **Typing**: Ground/Psychic, kept. *Alternative*: Ground/Ghost.
- **The Clay Idol Pokémon**, No. 2344. 2.0 m, 180.0 kg (Claydol 1.5 m, 108 kg).
- **Stats**: 60 / 70 / 105 / 70 / 120 / 75 = 500 → **80 / 70 / 120 / 90 / 130 / 90 = 580** (+20 HP +15 Def +20 SpA +10 SpD +15 Spe): the rise goes to its HP, Sp. Atk and Speed.
- **Abilities**: Levitate (was Levitate).
- **Evolution**: Claydol at level 152. Data: `{"targetSpecies":"Dogunarch","level":152}`.
- **Look**: from our black clay doll with its many red eyes and floating hands: taller, a ring of eight eyes, a crown of clay on its head and four floating hands in place of two. **New feature**: the four hands.

### 34. Crinolith, from Cradily (346): yes

- **Typing**: Rock/Grass, kept. *Alternative*: Rock/Water.
- **The Sea Lily Pokémon**, No. 2346. 2.0 m, 90.0 kg (Cradily 1.5 m, 60.4 kg).
- **Stats**: 86 / 81 / 97 / 81 / 107 / 43 = 495 → **106 / 86 / 112 / 96 / 122 / 53 = 575** (+20 HP +5 Atk +15 Def +15 SpA +15 SpD +10 Spe): the rise goes to its HP, then its Defense, Sp. Atk and Sp. Def.
- **Abilities**: Suction Cups, hidden Storm Drain (was Suction Cups, hidden Storm Drain).
- **Evolution**: Cradily at level 150, holding a Root Fossil (used up). Data: `{"targetSpecies":"Crinolith","method":"LevelHoldingItem","level":150,"item":"Root Fossil"}`.
- **Look**: from our green sea lily with its tentacle head: taller, the stalk's tentacles grown into a crown of eight fronds (Leaf), the yellow eye-spots brighter and roots at its base. **New feature**: the crown of fronds.

### 35. Anomalorex, from Armaldo (348): yes

- **Typing**: Rock/Bug, kept. *Alternative*: Rock/Water.
- **The Ancient Hunter Pokémon**, No. 2348. 1.9 m, 100.0 kg (Armaldo 1.5 m, 68.2 kg).
- **Stats**: 75 / 125 / 100 / 70 / 80 / 45 = 495 → **90 / 140 / 115 / 70 / 95 / 65 = 575** (+15 HP +15 Atk +15 Def +15 SpD +20 Spe): the rise goes to its Speed, then its Attack, HP, Defense and Sp. Def.
- **Abilities**: Battle Armor, hidden Swift Swim (was Battle Armor, hidden Swift Swim).
- **Evolution**: Armaldo at level 150, holding a Claw Fossil (used up). Data: `{"targetSpecies":"Anomalorex","method":"LevelHoldingItem","level":150,"item":"Claw Fossil"}`.
- **Look**: from our armoured shrimp standing on two legs: its two claws grown into the curled grasping limbs of the anomalocaris, a fan of plates at its tail and its armour edged in red. **New feature**: the curled grasping limbs.

### 36. Prismaria, from Milotic (350): yes

- **Typing**: Water → **Water/Fairy**. *Alternative*: Water alone.
- **The Rainbow Pokémon**, No. 2350. 7.0 m, 200.0 kg (Milotic 6.2 m, 162 kg).
- **Stats**: 95 / 60 / 79 / 100 / 125 / 81 = 540 → **105 / 60 / 89 / 115 / 140 / 91 = 600** (+10 HP +10 Def +15 SpA +15 SpD +10 Spe): the rise is +60, to the ceiling: Sp. Atk and Sp. Def, then HP, Defense and Speed.
- **Abilities**: Marvel Scale, hidden Cute Charm (was Marvel Scale, hidden Cute Charm).
- **Evolution**: Milotic at level 155, holding a Prism Scale (used up). Data: `{"targetSpecies":"Prismaria","method":"LevelHoldingItem","level":155,"item":"Prism Scale"}`.
- **Look**: from our long cream serpent with its red brows and fanned tail: longer, the tail's fan doubled with scales that shift colour in the light, two longer ribbons from its head and a fin along the spine. The female's longer brows are kept. **New feature**: the doubled tail fan.

### 37. Chromeleon, from Kecleon (352): yes

- **Typing**: Normal → **Normal/Ghost**. *Alternative*: Normal alone.
- **The Vanish Pokémon**, No. 2352. 1.3 m, 35.0 kg (Kecleon 1 m, 22 kg).
- **Stats**: 60 / 90 / 70 / 60 / 120 / 40 = 440 → **75 / 110 / 80 / 60 / 130 / 65 = 520** (+15 HP +20 Atk +10 Def +10 SpD +25 Spe): the rise goes to its Speed, Attack, HP and Sp. Def.
- **Abilities**: Color Change, hidden Protean (was Color Change, hidden Protean).
- **Evolution**: Kecleon at level 145, knowing Shadow Sneak. Data: `{"targetSpecies":"Chromeleon","method":"LevelKnowsMove","level":145,"move":"Shadow Sneak"}`.
- **Look**: from our green chameleon with its red band: taller, the band grown into a frill, its colours shown in shifting stripes, its curled tail longer and its outline faint at its edges (a lighter outline). **New feature**: the frill.

### 38. Effigrim, from Banette (354): yes

- **Typing**: Ghost, kept. *Alternative*: Ghost/Dark.
- **The Grudge Doll Pokémon**, No. 2354. 1.4 m, 20.0 kg (Banette 1.1 m, 12.5 kg).
- **Stats**: 64 / 115 / 65 / 83 / 63 / 65 = 455 → **79 / 135 / 75 / 93 / 73 / 80 = 535** (+15 HP +20 Atk +10 Def +10 SpA +10 SpD +15 Spe): the rise goes to its Attack, then the rest.
- **Abilities**: Insomnia, Frisk, hidden Cursed Body (was Insomnia, Frisk, hidden Cursed Body).
- **Evolution**: Banette at level 146, holding a Spell Tag at night (used up). Data: `{"targetSpecies":"Effigrim","method":"LevelHoldingItemNight","level":146,"item":"Spell Tag"}`.
- **Look**: from our grey doll with its zipper mouth: taller, patched in pieces of other cloth, a second zipper down its chest half open with light inside (Glow, yellow) and long ribbons of cloth from its head. **New feature**: the zipper down its chest.

### 39. Musadon, from Tropius (357): yes

- **Typing**: Grass/Flying → **Grass/Dragon**. *Alternative*: Grass/Flying.
- **The Orchard Pokémon**, No. 2357. 2.6 m, 180.0 kg (Tropius 2 m, 100 kg).
- **Stats**: 99 / 68 / 83 / 72 / 87 / 51 = 460 → **114 / 83 / 98 / 87 / 102 / 56 = 540** (+15 HP +15 Atk +15 Def +15 SpA +15 SpD +5 Spe): the rise is spread over every stat.
- **Abilities**: Chlorophyll, Solar Power, hidden Harvest (was Chlorophyll, Solar Power, hidden Harvest).
- **Evolution**: Tropius at level 148, by day. Data: `{"targetSpecies":"Musadon","method":"LevelDay","level":148}`.
- **Look**: from our long-necked green Tropius with its leaf wings and its bunch of fruit: larger, two bunches of fruit, its leaf wings grown into a canopy, and a crest of young leaves. **New feature**: the second bunch of fruit.

### 40. Carillone, from Chimecho (358): yes

- **Typing**: Psychic, kept. *Alternative*: Psychic/Steel.
- **The Carillon Pokémon**, No. 2358. 1.0 m, 3.0 kg (Chimecho 0.6 m, 1 kg).
- **Stats**: 75 / 50 / 80 / 95 / 90 / 65 = 455 → **90 / 50 / 95 / 120 / 105 / 75 = 535** (+15 HP +15 Def +25 SpA +15 SpD +10 Spe): the rise goes to its Sp. Atk, then its HP, Defense and Sp. Def.
- **Abilities**: Levitate (was Levitate).
- **Evolution**: Chimecho at level 145, with friendship 220 or more, at night. Data: `{"targetSpecies":"Carillone","method":"FriendshipNight","level":145}`.
- **Look**: from our blue and white wind chime with its red cord: three bells hung one below another from its cord, the cord grown into two streamers and the yellow band on its head a crown. **New feature**: the three bells.

### 41. Omenblade, from Absol (359): strong yes

- **Typing**: Dark, kept. *Alternative*: Dark/Psychic.
- **The Calamity Pokémon**, No. 2359. 1.5 m, 60.0 kg (Absol 1.2 m, 47 kg).
- **Stats**: 65 / 130 / 60 / 75 / 60 / 75 = 465 → **80 / 145 / 75 / 85 / 75 / 85 = 545** (+15 HP +15 Atk +15 Def +10 SpA +15 SpD +10 Spe): the rise is spread over every stat, Attack among them.
- **Abilities**: Pressure, Super Luck, hidden Justified (was Pressure, Super Luck, hidden Justified).
- **Evolution**: Absol at level 150, holding a Scope Lens (used up). Data: `{"targetSpecies":"Omenblade","method":"LevelHoldingItem","level":150,"item":"Scope Lens"}`.
- **Look**: from our white Absol with its black scythe horn: larger, a second horn the shape of a crescent on the other side, its white mane grown into wings it can't fly with (as the Mega's) and its tail sharper. **New feature**: the second horn.

### 42. Bergmaw, from Glalie (362): yes

- **Typing**: Ice, kept. *Alternative*: Ice/Rock.
- **The Iceberg Pokémon**, No. 2362. 2.0 m, 400.0 kg (Glalie 1.5 m, 256.5 kg).
- **Stats**: 80 / 80 / 80 / 80 / 80 / 80 = 480 → **95 / 95 / 95 / 95 / 90 / 90 = 560** (+15 HP +15 Atk +15 Def +15 SpA +10 SpD +10 Spe): the rise is spread over every stat.
- **Abilities**: Inner Focus, Ice Body, hidden **Refrigerate** (was Inner Focus, Ice Body, hidden Moody).
- **Evolution**: Glalie at level 145, holding a Never-Melt Ice (used up). Data: `{"targetSpecies":"Bergmaw","method":"LevelHoldingItem","level":145,"item":"Never-Melt Ice"}`.
- **Look**: from our round black-faced head of ice with its horns: larger, the ice grown into a jagged berg round the face, four horns, and the jaw broader. **New feature**: the jagged berg round its face.

### 43. Gulpereel, from Huntail (367): yes

- **Typing**: Water → **Water/Dark**. *Alternative*: Water alone, for both.
- **The Abyssal Lure Pokémon**, No. 2367. 2.4 m, 45.0 kg (Huntail 1.7 m, 27 kg).
- **Stats**: 55 / 104 / 105 / 94 / 75 / 52 = 485 → **75 / 129 / 115 / 94 / 85 / 67 = 565** (+20 HP +25 Atk +10 Def +10 SpD +15 Spe): the rise goes to its Attack, HP and the rest.
- **Abilities**: Swift Swim, hidden **Strong Jaw** (was Swift Swim, hidden Water Veil).
- **Evolution**: Huntail at level 150, holding a Deep Sea Tooth (used up). Data: `{"targetSpecies":"Gulpereel","method":"LevelHoldingItem","level":150,"item":"Deep Sea Tooth"}`.
- **Look**: from our blue eel with its tail lure: longer, the lure on its tail a second, glowing fish (Glow, pale blue), the jaw wider and its spots lit. **New feature**: the second lure.

### 44. Sirenyss, from Gorebyss (368): yes

- **Typing**: Water → **Water/Fairy**. *Alternative*: Water alone, for both.
- **The Siren Pokémon**, No. 2368. 2.4 m, 35.0 kg (Gorebyss 1.8 m, 22.6 kg).
- **Stats**: 55 / 84 / 105 / 114 / 75 / 52 = 485 → **75 / 84 / 115 / 139 / 85 / 67 = 565** (+20 HP +10 Def +25 SpA +10 SpD +15 Spe): the rise goes to its Sp. Atk, HP and the rest.
- **Abilities**: Swift Swim, hidden **Liquid Voice** (was Swift Swim, hidden Hydration).
- **Evolution**: Gorebyss at level 150, holding a Deep Sea Scale (used up). Data: `{"targetSpecies":"Sirenyss","method":"LevelHoldingItem","level":150,"item":"Deep Sea Scale"}`.
- **Look**: from our pink sea serpent with its thin snout: longer, ribbon fins along its whole length, the snout finer and the tail's fan doubled. **New feature**: the ribbon fins.

### 45. Aeonfin, from Relicanth (369): yes

- **Typing**: Water/Rock, kept. *Alternative*: Water/Dragon.
- **The Living Fossil Pokémon**, No. 2369. 1.6 m, 60.0 kg (Relicanth 1 m, 23.4 kg).
- **Stats**: 100 / 90 / 130 / 45 / 65 / 55 = 485 → **115 / 110 / 140 / 45 / 85 / 70 = 565** (+15 HP +20 Atk +10 Def +20 SpD +15 Spe): the rise goes to its Attack, Sp. Def and Speed.
- **Abilities**: Swift Swim, Rock Head, hidden Sturdy (was Swift Swim, Rock Head, hidden Sturdy).
- **Evolution**: Relicanth at level 158. Data: `{"targetSpecies":"Aeonfin","level":158}`.
- **Look**: from our grey-brown rocky fish with its red mouth: larger, its rocky scales grown into plates, the lobed fins longer like limbs and a mark of age on its flank. The female's smaller mark is kept. **New feature**: the lobed fins like limbs.

### 46. Heartreef, from Luvdisc (370): strong yes

- **Typing**: Water → **Water/Fairy**. *Alternative*: Water alone.
- **The Devotion Pokémon**, No. 2370. 1.0 m, 20.0 kg (Luvdisc 0.6 m, 8.7 kg).
- **Stats**: 43 / 30 / 55 / 40 / 65 / 97 = 330 → **63 / 40 / 70 / 60 / 80 / 97 = 410** (+20 HP +10 Atk +15 Def +20 SpA +15 SpD): the rise goes to its HP, Sp. Atk, Defense and Sp. Def; Speed stays.
- **Abilities**: Swift Swim, hidden Hydration (was Swift Swim, hidden Hydration).
- **Evolution**: Luvdisc at level 140, holding a Heart Scale (used up). Data: `{"targetSpecies":"Heartreef","method":"LevelHoldingItem","level":140,"item":"Heart Scale"}`.
- **Look**: from our pink heart-shaped fish: a reef of small pink coral hearts grown along its back, its lips fuller and its tail fin larger. **New feature**: the reef of hearts on its back.

### 47. Regimonolith, from Regirock (377): maybe

- **Typing**: Rock, kept. *Alternative*: none of the three.
- **The Monolith Pokémon**, No. 2377. 2.2 m, 330.0 kg (Regirock 1.7 m, 230 kg).
- **Stats**: 80 / 100 / 200 / 50 / 100 / 50 = 580 → **100 / 130 / 200 / 50 / 120 / 60 = 660** (+20 HP +30 Atk +20 SpD +10 Spe): the rise goes to its Attack, Sp. Def, HP and Speed; Defense stays 200.
- **Abilities**: Clear Body, hidden Sturdy (was Clear Body, hidden Sturdy).
- **Evolution**: Regirock at level 160, with a Regigigas in the party. Data: `{"targetSpecies":"Regimonolith","method":"LevelWithSpeciesInParty","level":160,"species":"Regigigas"}`.
- **Look**: from our brown boulder giant with its braille dots: taller, more boulders in its arms, the seven dots grown to a ring and a lintel-like crown of stone. **New feature**: the ring of dots.

### 48. Regiglacier, from Regice (378): maybe

- **Typing**: Ice, kept. *Alternative*: none of the three.
- **The Glacier Pokémon**, No. 2378. 2.2 m, 260.0 kg (Regice 1.8 m, 175 kg).
- **Stats**: 80 / 50 / 100 / 100 / 200 / 50 = 580 → **100 / 50 / 120 / 130 / 200 / 60 = 660** (+20 HP +20 Def +30 SpA +10 Spe): the rise goes to its Sp. Atk, HP, Defense and Speed; Sp. Def stays 200.
- **Abilities**: Clear Body, hidden Ice Body (was Clear Body, hidden Ice Body).
- **Evolution**: Regice at level 160, with a Regigigas in the party. Data: `{"targetSpecies":"Regiglacier","method":"LevelWithSpeciesInParty","level":160,"species":"Regigigas"}`.
- **Look**: from our blue figure of ice: larger, its body faceted into a crystal of many faces, the dots a ring and spires of ice at its shoulders. **New feature**: the spires at its shoulders.

### 49. Regiferrum, from Registeel (379): maybe

- **Typing**: Steel, kept. *Alternative*: none of the three.
- **The Iron Vault Pokémon**, No. 2379. 2.3 m, 310.0 kg (Registeel 1.9 m, 205 kg).
- **Stats**: 80 / 75 / 150 / 75 / 150 / 50 = 580 → **100 / 95 / 165 / 80 / 160 / 60 = 660** (+20 HP +20 Atk +15 Def +5 SpA +10 SpD +10 Spe): the rise goes to its HP and Attack, then its defences.
- **Abilities**: Clear Body, hidden Light Metal (was Clear Body, hidden Light Metal).
- **Evolution**: Registeel at level 160, with a Regigigas in the party. Data: `{"targetSpecies":"Regiferrum","method":"LevelWithSpeciesInParty","level":160,"species":"Regigigas"}`.
- **Look**: from our dark steel figure: heavier, rivets along its seams, the dots a ring of lit bolts (Glow, red) and a vault-like plate on its chest. **New feature**: the plate on its chest.

### 50. Aurelias, from Latias (380): maybe

- **Typing**: Dragon/Psychic, kept. *Alternative*: Dragon/Fairy, and Dragon/Flying for Latios's.
- **The Dawn Eon Pokémon**, No. 2380. 1.7 m, 55.0 kg (Latias 1.4 m, 40 kg).
- **Stats**: 80 / 80 / 90 / 110 / 130 / 110 = 600 → **90 / 80 / 100 / 125 / 145 / 120 = 660** (+10 HP +10 Def +15 SpA +15 SpD +10 Spe): the rise is +60, to the 660 of every other region's legendary set: Sp. Atk and Sp. Def, then HP, Defense and Speed.
- **Abilities**: Levitate (was Levitate).
- **Evolution**: Latias at level 160, with a Latios in the party. Data: `{"targetSpecies":"Aurelias","method":"LevelWithSpeciesInParty","level":160,"species":"Latios"}`.
- **Look**: from our red and white jet-like Latias: longer, its wings swept back further with gold edges, the triangle on its chest grown into a star (Glow, pale gold) and a long tail fin. **New feature**: the gold-edged wings.

### 51. Aurelios, from Latios (381): maybe

- **Typing**: Dragon/Psychic, kept. *Alternative*: Dragon/Flying, and Dragon/Fairy for Latias's.
- **The Dawn Eon Pokémon**, No. 2381. 2.3 m, 75.0 kg (Latios 2 m, 60 kg).
- **Stats**: 80 / 90 / 80 / 130 / 110 / 110 = 600 → **90 / 90 / 90 / 145 / 125 / 120 = 660** (+10 HP +10 Def +15 SpA +15 SpD +10 Spe): the rise is +60, as Aurelias's.
- **Abilities**: Levitate (was Levitate).
- **Evolution**: Latios at level 160, with a Latias in the party. Data: `{"targetSpecies":"Aurelios","method":"LevelWithSpeciesInParty","level":160,"species":"Latias"}`.
- **Look**: from our blue and white Latios: Aurelias's changes in blue, its star pale blue. **New feature**: the gold-edged wings.

## Balance

Measured by the script, as the earlier batches':

- **Totals**: 410 (Heartreef) to 600 (Prismaria alone, Milotic's 540 risen only +60 to the ceiling), the Regis and the eon pair 660. Nothing that isn't legendary passes 600.
- **Highest of each stat**: Attack 145 (Sporepugil and Omenblade), under the earlier batches' 150. Sp. Atk 145 (Aurelios), and 139 for one that isn't legendary (Sirenyss). Speed 160 (Shinobuzz, which is Ninjask's own 160 kept), then Swiftarrow's 135. Defense 200 (Regimonolith, Regirock's own) and 150 for one that isn't legendary (Smeltortoise). Sp. Def 200 (Regiglacier, Regice's own). HP 180 (Leviawhale, from Wailord's 170).
- **The weakest stay modest on purpose** (batch 1's answer: no floor): Heartreef 410 and Whirlpanda 440 are the lowest of the batch, beside batch 2's Sleighwing (410) and Muralist (330).
- **Abilities the newer games gave the species**: `species.json` keeps Pelipper as Keen Eye and Rain Dish and Torkoal as White Smoke and Shell Armor, though the seventh generation gave them Drizzle and Drought. Pelicaptain takes Drizzle in its first slot and Smeltortoise Drought as its hidden one (question 6).

**Flagged as strong** (each inside the official range; the user may want one turned down):
1. **Shinobuzz**: Speed Boost on the fastest of the batch (160, Ninjask's own) with 120 Attack and Baton Pass in its line. *To turn down*: Attack 110 and the 10 to Defense, or keep it a maybe and leave Ninjask as it is.
2. **Ascetrance**: Pure Power doubles its Attack, so its 80 hits as 160. Medicham's own 60 hits as 120 already. *To turn down*: Attack 70, Sp. Def 100.
3. **Pelicaptain**: Drizzle with Rain Dish and 120 Sp. Atk, a rain setter that also hits hard. *To turn down*: Sp. Atk 110, HP 90.
4. **Sirenyss**: Liquid Voice makes Hyper Voice a Water move with Swift Swim's speed and 139 Sp. Atk. *To turn down*: Hydration kept as the hidden ability.

**Weaker by design**: Leviawhale (its rise goes to the defences Wailord lacks, not to its attacks) and Bergmaw (Glalie's even 80s risen evenly) are judged on their designs more than their numbers.

## Open questions: only what is new for Hoenn

The earlier batches' seventeen are settled and applied (above). These are new.

1. **The level ceiling.** Batch 1 put a species that isn't legendary at level 130 at most, which suited Kanto (met at about 2 to 104) and Johto (to 136). Hoenn is met at 136 to 162, so under that ceiling every Hoenn species caught in Hoenn evolves at its first level. *Default*: **each batch's species evolve within their own region's climb**, here 140 to 158 (and its legendaries at 160, after the Champion at 162). The rule becomes "within the region's own climb, at most 160", which leaves Kanto's and Johto's approved levels as they are. *Alternative*: keep 130, and Hoenn's species evolve as soon as they are caught.
2. **The Regis.** *Default*: build them as a trio, each at level 160 with a Regigigas in the party, to 660 (under Regigigas's 670). *Alternatives*: by holding an item each (a Hard Stone, a Never-Melt Ice, a Metal Coat), or not at all.
3. **Latias and Latios.** *Default*: build them as a pair, each at level 160 with the other in the party, +60 to 660. *Alternatives*: +80 to 680 (Rayquaza's), or not at all. Kyogre, Groudon and Rayquaza: no, as the cover legendaries before them; Jirachi and Deoxys: no, as Mew and Celebi.
4. **Shedinja and Castform.** *Default*: no (above). *Alternatives*: a Shedinja that keeps its 1 HP and Wonder Guard and puts +80 into the rest; a Castform evolution with its own four weather forms.
5. **The rivals.** Zangoose and Seviper each evolve after knocking out three of the other (`LevelAfterDefeating`, Kingambit's condition). *Default*: kept, since the rivalry is their whole lore. *Alternative*: each with the other in the party, as the region's other pairs.
6. **Abilities from a later generation that the data lacks.** Drizzle for Pelicaptain and Drought for Smeltortoise (above) are in `abilities.json` but not on Pelipper and Torkoal in `species.json`. *Default*: kept on ours. *Alternative*: Keen Eye and Shell Armor, the data's own.
7. **Two ends of one first stage.** Ninjask and Shedinja are both Nincada's, Huntail and Gorebyss both Clamperl's, Glalie and Froslass both Snorunt's. *Default*: each end is judged alone (Ninjask a maybe, Shedinja a no; Huntail and Gorebyss a set; Glalie here and Froslass in Sinnoh's batch), as batch 1 did Tyrogue's and batch 2 Slowpoke's.

## The table to answer

One row a species. The user answers each with yes, a change, or no; striking a row in the selection table above is a no.

| # | Ours | Typing | Abilities | HP/Atk/Def/SpA/SpD/Spe (total) | Δ | Evolves |
|---|---|---|---|---|---|---|
| 1 | Hyaenarch | Dark | Intimidate, Quick Feet, Moxie | 85/115/85/60/70/85 (500) | +15 HP +25 Atk +15 Def +10 SpD +15 Spe | level 142, at night |
| 2 | Streakoon | Normal/Ground | Pickup, Gluttony, Quick Feet | 93/95/71/50/71/120 (500) | +15 HP +25 Atk +10 Def +10 SpD +20 Spe | level 140, knowing Belly Drum |
| 3 | Swiftarrow | Normal/Flying | Guts, Scrappy | 75/110/75/75/65/135 (535) | +15 HP +25 Atk +15 Def +15 SpD +10 Spe | level 145, by day |
| 4 | Pelicaptain | Water/Flying | Drizzle, Rain Dish | 80/50/110/120/90/70 (520) | +20 HP +10 Def +25 SpA +20 SpD +5 Spe | level 145, in the rain |
| 5 | Ocellwing | Bug/Flying | Intimidate, Unnerve | 85/60/77/125/97/90 (534) | +15 HP +15 Def +25 SpA +15 SpD +10 Spe | level 142, holding a Silver Powder (used up) |
| 6 | Sporepugil | Grass/Fighting | Effect Spore, Poison Heal, Technician | 80/145/95/60/75/85 (540) | +20 HP +15 Atk +15 Def +15 SpD +15 Spe | level 148, holding a Toxic Orb (used up) |
| 7 | Shinobuzz | Bug/Flying | Speed Boost, Infiltrator | 76/120/65/50/65/160 (536) | +15 HP +30 Atk +20 Def +15 SpD | level 145, at night |
| 8 | Yokozan | Fighting | Thick Fat, Guts, Sheer Force | 154/135/80/40/80/65 (554) | +10 HP +15 Atk +20 Def +20 SpD +15 Spe | level 150, holding a Black Belt (used up) |
| 9 | Felinelle | Normal/Fairy | Cute Charm, Normalize, Wonder Skin | 85/80/75/70/70/100 (480) | +15 HP +15 Atk +10 Def +15 SpA +15 SpD +10 Spe | a Moon Stone used on it at level 140 or above |
| 10 | Gemwraith | Dark/Ghost | Keen Eye, Stall, Prankster | 70/90/90/80/80/50 (460) | +20 HP +15 Atk +15 Def +15 SpA +15 SpD | level 142, holding a Star Piece (used up) |
| 11 | Deceivore | Steel/Fairy | Hyper Cutter, Intimidate, Sheer Force | 70/105/100/55/70/60 (460) | +20 HP +20 Atk +15 Def +15 SpD +10 Spe | level 142, holding a Razor Fang (used up) |
| 12 | Ascetrance | Fighting/Psychic | Pure Power, Telepathy | 75/80/85/70/90/90 (490) | +15 HP +20 Atk +10 Def +10 SpA +15 SpD +10 Spe | level 145, by day |
| 13 | Fulmenhound | Electric | Static, Lightning Rod, Minus | 80/75/80/125/80/115 (555) | +10 HP +20 Def +20 SpA +20 SpD +10 Spe | level 150, in the rain |
| 14 | Anodion | Electric/Fairy | Plus, Lightning Rod | 75/50/55/110/90/105 (485) | +15 HP +15 Def +25 SpA +15 SpD +10 Spe | level 142, with a Minun in the party |
| 15 | Cathodion | Electric/Fairy | Minus, Volt Absorb | 75/40/65/100/100/105 (485) | +15 HP +15 Def +25 SpA +15 SpD +10 Spe | level 142, with a Plusle in the party |
| 16 | Photurion | Bug/Fairy | Illuminate, Swarm, Prankster | 80/93/85/47/100/105 (510) | +15 HP +20 Atk +10 Def +15 SpD +20 Spe | level 145, with an Illumise in the party |
| 17 | Lampyria | Bug/Fairy | Oblivious, Tinted Lens, Prankster | 80/47/85/93/100/105 (510) | +15 HP +10 Def +20 SpA +15 SpD +20 Spe | level 145, with a Volbeat in the party |
| 18 | Gulpgorge | Poison | Liquid Ooze, Sticky Hold, Gluttony | 120/83/98/83/98/65 (547) | +20 HP +10 Atk +15 Def +10 SpA +15 SpD +10 Spe | level 146, holding a Black Sludge (used up) |
| 19 | Megalodax | Water/Dark | Rough Skin, Speed Boost | 90/140/60/95/60/95 (540) | +20 HP +20 Atk +20 Def +20 SpD | level 152 |
| 20 | Leviawhale | Water | Water Veil, Oblivious, Pressure | 180/100/70/100/70/60 (580) | +10 HP +10 Atk +25 Def +10 SpA +25 SpD | level 155 |
| 21 | Volcamel | Fire/Ground | Magma Armor, Solid Rock, Anger Point | 90/110/90/125/85/40 (540) | +20 HP +10 Atk +20 Def +20 SpA +10 SpD | level 150, holding a Heat Rock (used up) |
| 22 | Smeltortoise | Fire/Steel | White Smoke, Drought | 90/95/150/105/90/20 (550) | +20 HP +10 Atk +10 Def +20 SpA +20 SpD | level 148, by day |
| 23 | Boarcane | Psychic | Thick Fat, Own Tempo, Gluttony | 95/45/80/115/125/90 (550) | +15 HP +15 Def +25 SpA +15 SpD +10 Spe | level 148, holding a Big Pearl (used up) |
| 24 | Whirlpanda | Normal/Fighting | Own Tempo, Tangled Feet, Contrary | 75/80/75/60/75/75 (440) | +15 HP +20 Atk +15 Def +15 SpD +15 Spe | level 140, knowing Teeter Dance |
| 25 | Saguarox | Grass/Dark | Sand Veil, Water Absorb | 85/130/80/125/75/60 (555) | +15 HP +15 Atk +20 Def +10 SpA +15 SpD +5 Spe | level 148, at night |
| 26 | Nimbusong | Dragon/Flying | Natural Cure, Cloud Nine | 90/80/105/85/120/90 (570) | +15 HP +10 Atk +15 Def +15 SpA +15 SpD +10 Spe | level 152, with friendship 220 or more |
| 27 | Ravagoose | Normal/Fighting | Immunity, Toxic Boost | 88/135/75/60/75/105 (538) | +15 HP +20 Atk +15 Def +15 SpD +15 Spe | level 148, after knocking out three Seviper |
| 28 | Scimitail | Poison/Dark | Shed Skin, Infiltrator | 88/115/75/115/75/70 (538) | +15 HP +15 Atk +15 Def +15 SpA +15 SpD +5 Spe | level 148, after knocking out three Zangoose |
| 29 | Selenolith | Rock/Psychic | Levitate | 100/55/80/120/100/85 (540) | +10 HP +15 Def +25 SpA +15 SpD +15 Spe | level 150, holding a Moon Stone at night (used up) |
| 30 | Heliolith | Rock/Psychic | Levitate | 100/120/100/55/80/85 (540) | +10 HP +25 Atk +15 Def +15 SpD +15 Spe | level 150, holding a Sun Stone by day (used up) |
| 31 | Namazuro | Water/Ground | Oblivious, Anticipation, Hydration | 125/98/88/86/86/65 (548) | +15 HP +20 Atk +15 Def +10 SpA +15 SpD +5 Spe | level 148, knowing Earthquake |
| 32 | Crayronin | Water/Dark | Hyper Cutter, Shell Armor, Adaptability | 83/140/95/90/75/65 (548) | +20 HP +20 Atk +10 Def +20 SpD +10 Spe | level 148, knowing Crabhammer |
| 33 | Dogunarch | Ground/Psychic | Levitate | 80/70/120/90/130/90 (580) | +20 HP +15 Def +20 SpA +10 SpD +15 Spe | level 152 |
| 34 | Crinolith | Rock/Grass | Suction Cups, Storm Drain | 106/86/112/96/122/53 (575) | +20 HP +5 Atk +15 Def +15 SpA +15 SpD +10 Spe | level 150, holding a Root Fossil (used up) |
| 35 | Anomalorex | Rock/Bug | Battle Armor, Swift Swim | 90/140/115/70/95/65 (575) | +15 HP +15 Atk +15 Def +15 SpD +20 Spe | level 150, holding a Claw Fossil (used up) |
| 36 | Prismaria | Water/Fairy | Marvel Scale, Cute Charm | 105/60/89/115/140/91 (600) | +10 HP +10 Def +15 SpA +15 SpD +10 Spe | level 155, holding a Prism Scale (used up) |
| 37 | Chromeleon | Normal/Ghost | Color Change, Protean | 75/110/80/60/130/65 (520) | +15 HP +20 Atk +10 Def +10 SpD +25 Spe | level 145, knowing Shadow Sneak |
| 38 | Effigrim | Ghost | Insomnia, Frisk, Cursed Body | 79/135/75/93/73/80 (535) | +15 HP +20 Atk +10 Def +10 SpA +10 SpD +15 Spe | level 146, holding a Spell Tag at night (used up) |
| 39 | Musadon | Grass/Dragon | Chlorophyll, Solar Power, Harvest | 114/83/98/87/102/56 (540) | +15 HP +15 Atk +15 Def +15 SpA +15 SpD +5 Spe | level 148, by day |
| 40 | Carillone | Psychic | Levitate | 90/50/95/120/105/75 (535) | +15 HP +15 Def +25 SpA +15 SpD +10 Spe | level 145, with friendship 220 or more, at night |
| 41 | Omenblade | Dark | Pressure, Super Luck, Justified | 80/145/75/85/75/85 (545) | +15 HP +15 Atk +15 Def +10 SpA +15 SpD +10 Spe | level 150, holding a Scope Lens (used up) |
| 42 | Bergmaw | Ice | Inner Focus, Ice Body, Refrigerate | 95/95/95/95/90/90 (560) | +15 HP +15 Atk +15 Def +15 SpA +10 SpD +10 Spe | level 145, holding a Never-Melt Ice (used up) |
| 43 | Gulpereel | Water/Dark | Swift Swim, Strong Jaw | 75/129/115/94/85/67 (565) | +20 HP +25 Atk +10 Def +10 SpD +15 Spe | level 150, holding a Deep Sea Tooth (used up) |
| 44 | Sirenyss | Water/Fairy | Swift Swim, Liquid Voice | 75/84/115/139/85/67 (565) | +20 HP +10 Def +25 SpA +10 SpD +15 Spe | level 150, holding a Deep Sea Scale (used up) |
| 45 | Aeonfin | Water/Rock | Swift Swim, Rock Head, Sturdy | 115/110/140/45/85/70 (565) | +15 HP +20 Atk +10 Def +20 SpD +15 Spe | level 158 |
| 46 | Heartreef | Water/Fairy | Swift Swim, Hydration | 63/40/70/60/80/97 (410) | +20 HP +10 Atk +15 Def +20 SpA +15 SpD | level 140, holding a Heart Scale (used up) |
| 47 | Regimonolith | Rock | Clear Body, Sturdy | 100/130/200/50/120/60 (660) | +20 HP +30 Atk +20 SpD +10 Spe | level 160, with a Regigigas in the party |
| 48 | Regiglacier | Ice | Clear Body, Ice Body | 100/50/120/130/200/60 (660) | +20 HP +20 Def +30 SpA +10 Spe | level 160, with a Regigigas in the party |
| 49 | Regiferrum | Steel | Clear Body, Light Metal | 100/95/165/80/160/60 (660) | +20 HP +20 Atk +15 Def +5 SpA +10 SpD +10 Spe | level 160, with a Regigigas in the party |
| 50 | Aurelias | Dragon/Psychic | Levitate | 90/80/100/125/145/120 (660) | +10 HP +10 Def +15 SpA +15 SpD +10 Spe | level 160, with a Latios in the party |
| 51 | Aurelios | Dragon/Psychic | Levitate | 90/90/90/145/125/120 (660) | +10 HP +10 Def +15 SpA +15 SpD +10 Spe | level 160, with a Latias in the party |

## The technical plan (for after the user's answers)

Batch 1's plan holds unchanged: the species in `tools/DataImporter/Overrides/species-ours.json`, the evolution rows through `Overrides/species.json`, the Pokédex text in `Data/dex-entries.json`, and the sculpts in `PokemonModels.Ours.Hoenn.cs`, in our numbers' order, each built from its species' build where it shares most of the body. Nothing in the engine is needed for this batch: every method above runs today, `LevelWithSpeciesInParty`, `LevelAfterDefeating`, `LevelHoldingItemNight` and `LevelHoldingItemDay` among them. What is not this batch's: 06 · R31's moves past level 100 (the learnsets wait for them), the species whose lines end in Sinnoh and later (their batches), and where in the game the evolutions are met (they need no place, only a level the climb reaches).

## How the numbers were checked

The same kind of Node.js script as the earlier batches', kept in the session's scratch folder and reading only `species.json`, `abilities.json` and `items.json`, with the 80 names and numbers of batches 1 and 2 read from their two documents' tables. It built the lines (a regional form's evolution counted as the form's own) and the list of the 58 that qualify, and for each of the 51 designs checked: that it qualifies and is Hoenn's, and that every qualifying species has a design or a reason for none; that the rise is +80 or what the ceiling leaves (600, or 660 for a legendary set), that no stat falls and that the six stats add up to the total written; that the name is one word of at most 12 letters, taken by no species or form of the data, by none of the earlier batches' 80 and by no other design of this batch; that the number is 2000 + the old species' and meets none of the earlier batches'; that the types are one or two of the eighteen; that the abilities exist and fill exactly the old species' slots; that the level is between 101 and 160, between 136 and 158 for a species that isn't legendary; that an item held or used exists and is no key item; that a move to know is in the species' level-up learnset; that a species named in the party or to be knocked out exists; and that each is taller and heavier than what it comes from. The tables and the entries were written by the same script from the same values, so a number can't differ between the selection table, an entry and the table to answer.
