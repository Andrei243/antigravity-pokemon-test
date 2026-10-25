# D14 · Evolutions past level 100, batch 5 (Unova): the proposal

Plan 03 · D14, the design part of its fifth batch, written in wave 53 after the user approved batch 1 ([D14-proposals-kanto.md](D14-proposals-kanto.md)), batch 2 ([D14-proposals-johto.md](D14-proposals-johto.md)), batch 3 ([D14-proposals-hoenn.md](D14-proposals-hoenn.md)) and batch 4 ([D14-proposals-sinnoh.md](D14-proposals-sinnoh.md)) with every default, on 2026-10-25. **Waiting for the user's answers.** Nothing here is built: no species, no evolution row, no learnset, no sculpt, no test. Once approved, a later session writes each row into the data (as entries of `tools/DataImporter/Overrides/species-past-hundred.json`, batch 1's table). The format is batch 4's, with a Pokédex line added to each design.

**What batches 1 to 4 settled, applied here without asking again**: the rise is **+80, capped so that no species of ours that isn't legendary passes 600**, and a region's legendary set rises to **660** (batch 1's birds, batch 2's beasts, batch 3's Regis and eon pair, batch 4's lake guardians); the number is **2000 + the number of the species it evolves from** (here 2505 to 2640, meeting none of the earlier batches' 2020 to 2482); abilities come **only from `abilities.json`**, as many slots as the species had, in its order, the hidden one last; the evolution is **by level with a condition the engine already runs**, a held item used up; our species are listed in the National Pokédex after the 1,025 and **count toward none of its totals or diplomas**; names are invented, one word of at most 12 letters, and held against every species and form of the data and the earlier batches' 170; **an evolution that starts from a regional form belongs to that form's own line** (`Evolution.FitsForm`), so the Hisuian Lilligant, Zoroark and Braviary and the Galarian Darmanitan and Stunfisk evolve into none of ours; a female look, a Mega or a Gigantamax form is kept where the species has one, and **ours have no forms of their own**; the Frontier lets them be entered (a legendary's evolution joins its ban list), Pal Park, migration, the way out, the Pokéathlon's rivals and the golden corpus are held to 1 to 1,025; the cover legendaries and the mythicals don't evolve; a region's pairs are built as sets or not at all; **each species evolves within its own region's climb** (batch 3's first question).

**Unova's climb, and so its levels** (question 1): the region file `regions/unova.json` maps Black 2's levels onto the chain with `"levels": [[2, 162], [59, 185]]` (plan 18 · W4's fair climb): its first route at 162, its Gym Leaders at about 166 to 182, Iris at 185, and the post-game on past her on the same line. So Unova is the first region whose whole climb lies above batch 3's ceiling of 160 ("within the region's own climb, at most 160"), and that ceiling can't be met inside it. By default this batch's own species evolve **between 165 and 183**, within Unova's climb and before its Champion: the weakest of the first routes at 165 to 168, the region's middle at 170 to 178, the strongest and the last places' at 180 to 183; and its one legendary set at **190**, after Iris, as Hoenn's set came at 160 after its Champion's 162. The ceiling becomes the cap's own (260, plan 18 · W4), with "within the region's own climb" as the rule.

Every name, type, ability, item, move, location, species, stat and sum in this document was checked by a script that only reads `PokemonPlatinumEngine/Data/species.json`, `abilities.json` and `items.json`, with the four earlier batches' 170 names read from their documents (kept outside the repository; what it checks is listed under "How the numbers were checked", at the end).

## The shortlist: the twelve strongest

If the user wants a small first build of this batch, these are the twelve. Each lifts one of Unova's weakest last stages or is among the region's best-loved designs.

| | Ours | From | Typing | Total | Evolves | Why it leads |
|---|---|---|---|---|---|---|
| 1 | **Beaconwatch** | Watchog | Normal/Electric | 420 → 500 | level 165, at night | the lowest total of Unova's last stages; its glowing stripes as a lighthouse's lamp |
| 2 | **Amorabat** | Swoobat | Psychic/Flying | 425 → 505 | level 166, friendship 220 or more, at night | 425, by Woobat's own friendship |
| 3 | **Nimbolga** | Emolga | Electric/Flying | 428 → 508 | level 167, holding a Cell Battery | the lowest total of a species alone in Unova; the sky squirrel grown into a storm kite |
| 4 | **Remedino** | Audino | Normal/Fairy | 445 → 525 | level 168, friendship 220 or more, by day | the Pokémon of every Pokémon Center, 445 |
| 5 | **Umbrapard** | Liepard | Dark | 446 → 526 | level 166, knowing Sucker Punch | 446, the cruel cat grown into its own shadow |
| 6 | **Mariachactus** | Maractus | Grass/Ground | 461 → 541 | level 172, knowing Petal Dance | 461, the dancing cactus as a band of one |
| 7 | **Chinchillux** | Cinccino | Normal/Fairy | 470 → 550 | a Shiny Stone at level 170 | 470, by its own stone again |
| 8 | **Seasonstag** | Sawsbuck | Normal/Grass | 475 → 555 | level 174 | the stag of the seasons, all four on its antlers at once |
| 9 | **Pharaurum** | Cofagrigus | Ghost/Steel | 483 → 563 | level 175, holding a Reaper Cloth | 483, the golden coffin grown into a pharaoh's tomb |
| 10 | **Colossurk** | Golurk | Ground/Ghost | 483 → 563 | level 178, knowing Dynamic Punch | 483, the ancient automaton grown into a colossus |
| 11 | **Zoromirage** | Zoroark | Dark | 510 → 590 | level 178, at night | among the best loved of Unova: the fox of illusions |
| 12 | **Helioptera** | Volcarona | Bug/Fire | 550 → 600 | level 183, by day | among the best loved of Unova: the sun moth grown into its corona |

## What qualifies, and how many

The earlier batches' rule and the same script: a species **with no evolution at all**, or **the last stage of a line of two**; an evolution that starts from a regional form counts as that form's own.

**Unova, National Pokédex 494 to 649: 62 species qualify.**

- **27 alone**: Victini, Audino, Throh, Sawk, Basculin, Maractus, Sigilyph, Emolga, Alomomola, Cryogonal, Stunfisk, Druddigon, Bouffalant, Heatmor, Durant, Cobalion, Terrakion, Virizion, Tornadus, Thundurus, Reshiram, Zekrom, Landorus, Kyurem, Keldeo, Meloetta and Genesect (thirteen of them legendary or mythical). Basculin and Stunfisk are alone by the regional rule: only the white-striped Basculin becomes Basculegion (question 5), and the Galarian Stunfisk is its own.
- **35 last of a line of two**: Watchog, Liepard, Simisage, Simisear, Simipour, Musharna, Zebstrika, Swoobat, Excadrill, Whimsicott, Lilligant, Darmanitan, Crustle, Scrafty, Cofagrigus, Carracosta, Archeops, Garbodor, Zoroark, Cinccino, Swanna, Sawsbuck, Escavalier, Amoonguss, Jellicent, Galvantula, Ferrothorn, Beheeyem, Beartic, Accelgor, Mienshao, Golurk, Braviary, Mandibuzz and Volcarona. Petilil has two ends (Lilligant and the Hisuian Lilligant, a form) and Rufflet two (Braviary and the Hisuian Braviary), each judged as the species' own; Yamask's Galarian end, Runerigus, is Galar's batch's.
- **Deferred to this batch by the earlier four: none.** No line of an older region ends in Unova, and Unova brought no new stage to an older line.
- **Not here, last of a line of three (19)**: Serperior, Emboar, Samurott, Stoutland, Unfezant, Gigalith, Conkeldurr, Seismitoad, Leavanny, Scolipede, Krookodile, Gothitelle, Reuniclus, Vanilluxe, Klinklang, Eelektross, Chandelure, Haxorus and Hydreigon. They take 06 · R31's moves.
- **Not here, an Unova line that goes on in a later region**: none but by its regional forms (the white-striped Basculin's Basculegion, the Galarian Yamask's Runerigus, the Hisuian Zorua's and Rufflet's ends), each its own form's line and its region's batch's.

52 designs follow; Victini, Tornadus, Thundurus, Reshiram, Zekrom, Landorus, Kyurem, Keldeo, Meloetta and Genesect are left out with their reasons.

## How to read an entry

As batch 4's. **Stats** are the stats the game reads (the newest games' where `species.json` has a `modern` change; none of the 62 has one, since the data already holds their newest values). **Levels**: 165 to 183 for the region's own (above), the weakest of the first routes at 165 to 168, the strongest at 180 to 183, and the Swords of Justice at 190. **Looks** start from our own sculpt of the species; Jellicent's female, which the data keeps as a form (`Jellicent-Female`), is a female look of ours (question 6), and Excadrill, Audino, Scrafty and Golurk keep their Megas and Garbodor its Gigantamax form, which ours don't get. **Pokédex**: one line of our own words, the kind of line 08 · P13's written entries are made from; the two written entries and a habitat come when the batch is built, `--check-entries` clean. **Say**: strong yes (the shortlist), yes, maybe, no.

**The sets.** Unova has more pairs than any batch before it, and each is built as a set or not at all: **the three monkeys** (Simisage, Simisear and Simipour, each by its own stone again, as batch 3's Sunflora and Delcatty); **Throh and Sawk** (each with the other in the party); **Whimsicott and Lilligant** (Cottonee and Petilil were each other's in Black and White; each by its Sun Stone again); **the fossils** (Carracosta and Archeops, each holding a Rare Bone, since Unova's Cover and Plume Fossils are not in `items.json`: question 7); **Escavalier and Accelgor** (each made by trading for the other, each with the other in the party); **Braviary and Mandibuzz** (Rufflet and Vullaby were each other's version, the eagle by day and the vulture by night); **Heatmor and Durant** (predator and prey, each after knocking out three of the other, batch 3's Zangoose and Seviper); and the **Swords of Justice** (each with Keldeo in the party). Every condition below is one the engine already runs; one uses a place of Sinnoh's own, as batch 4's Glaciarbor: **`LevelAtLocation`** at the **Ice Rock** (Route 217) for Cryogonal.

## The legendaries and the mythicals

Unova has nine legendaries and four mythicals; the user asked each batch to say which of its region's evolve.

- **Cobalion, Terrakion and Virizion: maybe, as a trio or not at all**, each at level 190 with a Keldeo in the party. The three taught the young Keldeo to be a Sword of Justice; beside it, the teachers go further themselves. +80 to 660, as the birds, the beasts, the Regis and the lake guardians. *Default if the user doesn't say*: build them.
- **Reshiram, Zekrom and Kyurem: no, as a set.** The Tao trio: the two covers of Black and White at 680, and Kyurem at 660 whose fusions with them (`Kyurem-White`, `Kyurem-Black`) are its further shapes.
- **Tornadus, Thundurus and Landorus: no, as a set** (question 2). Each already has its further shape, its Therian Forme, as Dialga, Palkia and Giratina have their Origin Formes; and Enamorus, the fourth of the Forces of Nature, is Hisui's.
- **Victini, Keldeo, Meloetta and Genesect: no.** The mythicals stand outside any stage, as Mew, Celebi, Jirachi and Manaphy; Keldeo's Resolute Form, Meloetta's Pirouette Forme and Genesect's drives are their changes, and Keldeo is what the Swords of Justice evolve beside.

## The table: every qualifying species

The user may strike rows here before reading the designs. "No." is our number; "Total" is the stats the game reads now, and ours.

| # | Was | Ours | No. | Typing (was → ours) | Total | Evolves | Say |
|---|---|---|---|---|---|---|---|
| – | Victini (494) | none | – | Psychic/Fire | 600 | – | no: a mythical, outside any stage, as Mew, Celebi, Jirachi and Manaphy |
| 1 | Watchog (505) | Beaconwatch | 2505 | Normal → **Normal/Electric** | 420 → 500 | level 165, at night | strong yes: 420, the lowest total of Unova's last stages; the lookout whose stripes glow in the dark, grown into a lighthouse of its burrows |
| 2 | Liepard (510) | Umbrapard | 2510 | Dark, kept | 446 → 526 | level 166, knowing Sucker Punch | strong yes: 446; the cat whose beauty hides a cruel streak, grown into a shadow that walks beside its own |
| 3 | Simisage (512) | Simisylvan | 2512 | Grass, kept | 498 → 578 | a Leaf Stone used on it at level 170 or above | yes: with Simisear and Simipour, as a set or not at all: the three monkeys stop at 498, and the stone that made each makes it again |
| 4 | Simisear (514) | Simiscorch | 2514 | Fire, kept | 498 → 578 | a Fire Stone used on it at level 170 or above | yes: with the other two monkeys: the same stone again |
| 5 | Simipour (516) | Simitorrent | 2516 | Water, kept | 498 → 578 | a Water Stone used on it at level 170 or above | yes: with the other two monkeys: the same stone again |
| 6 | Musharna (518) | Somnarna | 2518 | Psychic → **Psychic/Fairy** | 487 → 567 | a Moon Stone used on it at level 170 or above | yes: the stone that woke it from Munna wakes it again; the dream smoke from its brow grown into a whole sleeping world |
| 7 | Zebstrika (523) | Stormstrika | 2523 | Electric, kept | 497 → 577 | level 170, knowing Wild Charge | yes: the thunderbolt horse grown into the storm that runs before it |
| 8 | Swoobat (528) | Amorabat | 2528 | Psychic/Flying, kept | 425 → 505 | level 166, with friendship 220 or more, at night | strong yes: 425; the bat whose heart-shaped nose sends waves that cheer whoever meets them, by Woobat's own friendship and at night |
| 9 | Excadrill (530) | Excavarch | 2530 | Ground/Steel, kept | 508 → 588 | level 176, holding a Soft Sand (used up) | yes: the drill mole of Unova's mines; past 100 its claws and crest are a boring machine that cuts whole tunnels |
| 10 | Audino (531) | Remedino | 2531 | Normal → **Normal/Fairy** | 445 → 525 | level 168, with friendship 220 or more, by day | strong yes: 445, and the Pokémon of every Pokémon Center; its ear-feelers that hear a heartbeat grown into a physician's |
| 11 | Throh (538) | Throjutsu | 2538 | Fighting, kept | 465 → 545 | level 172, with a Sawk in the party | yes: with Sawk's, as a pair or not at all: the judo master goes further only with its rival beside it |
| 12 | Sawk (539) | Sawkendo | 2539 | Fighting, kept | 465 → 545 | level 172, with a Throh in the party | yes: Throh's pair: the karate master with its rival beside it |
| 13 | Whimsicott (547) | Cottonimbus | 2547 | Grass/Fairy, kept | 480 → 560 | a Sun Stone used on it at level 172 or above | yes: with Lilligant's, as a pair or not at all (Cottonee and Petilil were each other's version): the stone that made each makes it again |
| 14 | Lilligant (549) | Ballerilly | 2549 | Grass → **Grass/Fairy** | 480 → 560 | a Sun Stone used on it at level 172 or above | yes: Whimsicott's pair: the flower whose crown is its pride, grown into a prima ballerina |
| 15 | Basculin (550) | Bascurage | 2550 | Water → **Water/Dark** | 460 → 540 | level 172, knowing Head Smash | maybe: the hostile fish of Unova's rivers; but its three stripes are forms, and the white-striped one already goes on to Basculegion (question 5) |
| 16 | Darmanitan (555) | Darumandala | 2555 | Fire → **Fire/Psychic** | 480 → 560 | level 174, by day | yes: the blazing ape whose Zen Mode stills it into a statue; past 100 the stillness is its own and the fire within it never cools |
| 17 | Maractus (556) | Mariachactus | 2556 | Grass → **Grass/Ground** | 461 → 541 | level 172, knowing Petal Dance | strong yes: 461, the dancing cactus of the Desert Resort; past 100 it is a band on its own, maracas in every hand |
| 18 | Crustle (558) | Crustadel | 2558 | Bug/Rock, kept | 485 → 565 | level 174, holding a Hard Stone (used up) | yes: the crab that carries a slab of rock; past 100 it carries a fortress |
| 19 | Scrafty (560) | Brawlhood | 2560 | Dark/Fighting, kept | 488 → 568 | level 174, knowing High Jump Kick | yes: the hoodlum that pulls its shed skin up like trousers; past 100 it leads the gang |
| 20 | Sigilyph (561) | Glyphoracle | 2561 | Psychic/Flying, kept | 490 → 570 | level 174, knowing Cosmic Power | yes: the guardian of an ancient city that still flies its old route; past 100 its patterns become a whole script |
| 21 | Cofagrigus (563) | Pharaurum | 2563 | Ghost → **Ghost/Steel** | 483 → 563 | level 175, holding a Reaper Cloth (used up) | strong yes: 483, the coffin that swallows grave robbers; past 100 it is the tomb of a whole dynasty |
| 22 | Carracosta (565) | Tortugarch | 2565 | Water/Rock, kept | 495 → 575 | level 178, holding a Rare Bone (used up) | yes: with Archeops's, as the fossils of every batch: the prototurtle grown into the ancient sea's king |
| 23 | Archeops (567) | Primavian | 2567 | Rock/Flying, kept | 567 → 600 | level 178, holding a Rare Bone (used up) | yes: Carracosta's pair: the first bird finally learns to fly well, though Defeatist still holds it back |
| 24 | Garbodor (569) | Scrapodor | 2569 | Poison → **Poison/Steel** | 474 → 554 | level 172, knowing Gunk Shot | yes: the trash heap of Unova's cities; past 100 the metal it eats becomes its armour |
| 25 | Zoroark (571) | Zoromirage | 2571 | Dark, kept | 510 → 590 | level 178, at night | strong yes: among the best loved of Unova; the fox of illusions grown into a mirage that fools a whole town |
| 26 | Cinccino (573) | Chinchillux | 2573 | Normal → **Normal/Fairy** | 470 → 550 | a Shiny Stone used on it at level 170 or above | strong yes: 470; the chinchilla whose fur repels dust, grown into a living silk scarf, by its own stone again |
| 27 | Swanna (581) | Cygnelle | 2581 | Water/Flying, kept | 473 → 553 | level 172, knowing Hurricane | yes: the white swan that dances at dawn; past 100 it leads the flock's dance |
| 28 | Sawsbuck (586) | Seasonstag | 2586 | Normal/Grass, kept | 475 → 555 | level 174 | strong yes: the stag of Unova's seasons, whose coats are four forms; ours carries all four seasons at once on its antlers, so no coat is lost |
| 29 | Emolga (587) | Nimbolga | 2587 | Electric/Flying, kept | 428 → 508 | level 167, holding a Cell Battery (used up) | strong yes: 428; the sky squirrel that glides on its membrane, grown into a kite of storm cloud |
| 30 | Escavalier (589) | Lancelier | 2589 | Bug/Steel, kept | 495 → 575 | level 176, with an Accelgor in the party | yes: with Accelgor's, as a pair or not at all: each was made by the other in a trade, and each goes further only beside the other |
| 31 | Amoonguss (591) | Sporemonarch | 2591 | Grass/Poison, kept | 464 → 544 | level 174, in the rain | yes: the mushroom that looks like a Poké Ball; past 100 a ring of toadstools grows round it |
| 32 | Jellicent (593) | Jellicrown | 2593 | Water/Ghost, kept | 480 → 560 | level 176, at night | yes: the jellyfish that sinks ships to their halls in the deep; past 100 it rules the halls |
| 33 | Alomomola (594) | Alomatron | 2594 | Water → **Water/Fairy** | 470 → 550 | level 175, knowing Healing Wish | yes: the fish that heals any wounded Pokémon in the sea; past 100 its membrane heals whole shoals |
| 34 | Galvantula (596) | Voltarantula | 2596 | Bug/Electric, kept | 472 → 552 | level 175, knowing Sticky Web | yes: the electric spider; past 100 its web is a net of lightning |
| 35 | Ferrothorn (598) | Ferrospire | 2598 | Grass/Steel, kept | 489 → 569 | level 176, holding a Rocky Helmet (used up) | yes: the thorn pod that hangs from cave ceilings; past 100 its spikes are the cave |
| 36 | Beheeyem (606) | Cosmogrey | 2606 | Psychic → **Psychic/Electric** | 485 → 565 | level 178, at night | yes: the cerebral Pokémon said to come from beyond the sky; past 100 its fingers' lights are a ring of lamps round it |
| 37 | Beartic (614) | Beartundra | 2614 | Ice → **Ice/Fighting** | 505 → 585 | level 178, holding a Never-Melt Ice (used up) | yes: the polar bear whose breath freezes its beard into fangs; past 100 a glacier rides on its back |
| 38 | Cryogonal (615) | Cryohedron | 2615 | Ice → **Ice/Psychic** | 515 → 595 | level 178, at an Ice Rock | yes: the snowflake that floats on the cold; at an Ice Rock its crystal becomes a lattice of many |
| 39 | Accelgor (617) | Velocigor | 2617 | Bug, kept | 495 → 575 | level 176, with an Escavalier in the party | yes: Escavalier's pair: the snail that shed its shell for speed, now a ninja of the wind |
| 40 | Stunfisk (618) | Fluxflounder | 2618 | Ground/Electric, kept | 471 → 551 | level 172, in the rain | yes: the flatfish that hides in the mud and shocks whoever steps on it; past 100 the mud round it is a field of sparks |
| 41 | Mienshao (620) | Mienshifu | 2620 | Fighting, kept | 510 → 590 | level 180, knowing Aura Sphere | yes: the martial arts master with whip-like sleeves; past 100 a grand master of its school |
| 42 | Druddigon (621) | Drudrakon | 2621 | Dragon → **Dragon/Rock** | 485 → 565 | level 180, holding a Dragon Fang (used up) | yes: the cave dragon that warms itself in the sun at the cave's mouth; past 100 its wings open |
| 43 | Golurk (623) | Colossurk | 2623 | Ground/Ghost, kept | 483 → 563 | level 178, knowing Dynamic Punch | strong yes: 483; the automaton made long ago to guard people, grown into the colossus a whole city once built |
| 44 | Bouffalant (626) | Bouffalanx | 2626 | Normal → **Normal/Ground** | 490 → 570 | level 176, knowing Head Charge | yes: the bash buffalo whose afro takes any blow; past 100 a whole stampede in one body |
| 45 | Braviary (628) | Imperavia | 2628 | Normal/Flying, kept | 510 → 590 | level 182, by day | yes: with Mandibuzz's, as a pair or not at all (Rufflet and Vullaby were each other's version): the valiant eagle by day |
| 46 | Mandibuzz (630) | Carrionqueen | 2630 | Dark/Flying, kept | 510 → 590 | level 182, at night | yes: Braviary's pair: the bone vulture by night |
| 47 | Heatmor (631) | Pyrovore | 2631 | Fire, kept | 484 → 564 | level 176, after knocking out 3 Durant | yes: with Durant's, as a pair or not at all: the anteater and the iron ant are each other's prey and predator, and each goes further by beating the other |
| 48 | Durant (632) | Formicarch | 2632 | Bug/Steel, kept | 484 → 564 | level 176, after knocking out 3 Heatmor | yes: Heatmor's pair: the iron ant grown into the queen of its colony |
| 49 | Volcarona (637) | Helioptera | 2637 | Bug/Fire, kept | 550 → 600 | level 183, by day | strong yes: among the best loved of Unova; the moth said to be the sun itself, grown into its corona |
| 50 | Cobalion (638) | Cobaltarch | 2638 | Steel/Fighting, kept | 580 → 660 | level 190, with a Keldeo in the party | maybe: the Swords of Justice as a trio or not at all, each by Keldeo in the party: they taught the young one, and beside it they go further |
| 51 | Terrakion (639) | Terrakhan | 2639 | Rock/Fighting, kept | 580 → 660 | level 190, with a Keldeo in the party | maybe: with the Swords of Justice |
| 52 | Virizion (640) | Virizenith | 2640 | Grass/Fighting, kept | 580 → 660 | level 190, with a Keldeo in the party | maybe: with the Swords of Justice; Keldeo beside the three closes the circle |
| – | Tornadus (641) | none | – | Flying | 580 | – | no: the Forces of Nature as a set, and each already has its further shape, its Therian Forme |
| – | Thundurus (642) | none | – | Electric/Flying | 580 | – | no: with the Forces of Nature; its Therian Forme is its change |
| – | Reshiram (643) | none | – | Dragon/Fire | 680 | – | no: 680, a cover legendary of the region, the white half of the Tao trio |
| – | Zekrom (644) | none | – | Dragon/Electric | 680 | – | no: 680, the black half of the Tao trio, as Reshiram |
| – | Landorus (645) | none | – | Ground/Flying | 600 | – | no: with the Forces of Nature; 600 already, and its Therian Forme is its change |
| – | Kyurem (646) | none | – | Dragon/Ice | 660 | – | no: 660 already, and its fusions with Reshiram and Zekrom (Kyurem-White and Kyurem-Black) are its further shapes |
| – | Keldeo (647) | none | – | Water/Fighting | 580 | – | no: a mythical, and its Resolute Form is its change; it is what the Swords of Justice evolve beside |
| – | Meloetta (648) | none | – | Normal/Psychic | 600 | – | no: a mythical, and its Pirouette Forme is its change |
| – | Genesect (649) | none | – | Bug/Steel | 600 | – | no: a mythical, and its four drives are its changes |

In all: 12 strong yes, 36 yes, 4 maybe (the Swords of Justice as a set among them) and 10 no.

## The designs

In National Pokédex order. Each entry: the typing and why, the category word, number, size, the stats (the species' as the game reads them → ours) with where the rise goes, the abilities, the evolution with the row it would be in `species.json`, the look and a Pokédex line.

### 1. Beaconwatch, from Watchog (505): strong yes

- **Typing**: Normal → **Normal/Electric**. *Alternative*: Normal/Psychic, its hypnotic stare.
- **The Beacon Pokémon**, No. 2505. 1.4 m, 38.0 kg (Watchog 1.1 m, 27 kg).
- **Stats**: 60 / 85 / 69 / 60 / 69 / 77 = 420 → **75 / 100 / 84 / 70 / 84 / 87 = 500** (+15 HP +15 Atk +15 Def +10 SpA +15 SpD +10 Spe): the rise goes to its HP, Attack, Defense and Sp. Def, then its Sp. Atk and Speed.
- **Abilities**: Illuminate, Keen Eye, hidden Analytic (was Illuminate, Keen Eye, hidden Analytic).
- **Evolution**: Watchog at level 165, at night. Data: `{"targetSpecies":"Beaconwatch","method":"LevelNight","level":165}`.
- **Look**: from our brown and cream Watchog with its yellow stripes and red eyes: taller and leaner, the stripes along its back and tail lit (Glow, yellow) in rings like a lighthouse's lamp, its tail longer and held up as a mast, a ruff of cream fur at its neck. **New feature**: the ringed lamp of its tail.
- **Pokédex**: It stands on its tail all night at the edge of its colony's burrows, and its stripes pulse in a code that only its own kind can read.

### 2. Umbrapard, from Liepard (510): strong yes

- **Typing**: Dark, kept. *Alternative*: Dark/Fairy, its beauty a lure.
- **The Shadow Cat Pokémon**, No. 2510. 1.4 m, 48.0 kg (Liepard 1.1 m, 37.5 kg).
- **Stats**: 64 / 88 / 50 / 88 / 50 / 106 = 446 → **79 / 103 / 70 / 93 / 70 / 111 = 526** (+15 HP +15 Atk +20 Def +5 SpA +20 SpD +5 Spe): the rise goes to its Defense and Sp. Def, then its HP, Attack, Sp. Atk and Speed.
- **Abilities**: Limber, Unburden, hidden Prankster (was Limber, Unburden, hidden Prankster).
- **Evolution**: Liepard at level 166, knowing Sucker Punch. Data: `{"targetSpecies":"Umbrapard","method":"LevelKnowsMove","level":166,"move":"Sucker Punch"}`.
- **Look**: from our purple Liepard with its yellow rosettes and pink mask: longer, the rosettes grown into crescents that run in a line down its back, a second shadow-tail (Glow, violet at its tip) and its mask a sharper red. **New feature**: the shadow-tail.
- **Pokédex**: Its shadow moves a moment before it does. Prey watching the cat is caught by the shadow it was not watching.

### 3. Simisylvan, from Simisage (512): yes

- **Typing**: Grass, kept. *Alternative*: Grass/Fighting, for all three.
- **The Grove Monkey Pokémon**, No. 2512. 1.3 m, 40.0 kg (Simisage 1.1 m, 30.5 kg).
- **Stats**: 75 / 98 / 63 / 98 / 63 / 101 = 498 → **85 / 113 / 78 / 113 / 78 / 111 = 578** (+10 HP +15 Atk +15 Def +15 SpA +15 SpD +10 Spe): the rise goes to its Attack, Defense, Sp. Atk and Sp. Def, then its HP and Speed.
- **Abilities**: Gluttony, hidden Overgrow (was Gluttony, hidden Overgrow).
- **Evolution**: Simisage by a Leaf Stone used on it at level 170 or above. Data: `{"targetSpecies":"Simisylvan","method":"UseItem","level":170,"item":"Leaf Stone"}`.
- **Look**: from our green Simisage with its leafy crest and its tail of leaves: taller, the crest grown into a crown of branches, a mantle of leaves at its shoulders and the tail a whole bough with small fruit. **New feature**: the crown of branches.
- **Pokédex**: It tends the trees of its forest as a gardener would. Berries grow twice as large wherever it has slept.

### 4. Simiscorch, from Simisear (514): yes

- **Typing**: Fire, kept. *Alternative*: Fire/Fighting, for all three.
- **The Pyre Monkey Pokémon**, No. 2514. 1.2 m, 38.0 kg (Simisear 1 m, 28 kg).
- **Stats**: 75 / 98 / 63 / 98 / 63 / 101 = 498 → **85 / 113 / 78 / 113 / 78 / 111 = 578** (+10 HP +15 Atk +15 Def +15 SpA +15 SpD +10 Spe): the rise goes to its Attack, Defense, Sp. Atk and Sp. Def, then its HP and Speed.
- **Abilities**: Gluttony, hidden Blaze (was Gluttony, hidden Blaze).
- **Evolution**: Simisear by a Fire Stone used on it at level 170 or above. Data: `{"targetSpecies":"Simiscorch","method":"UseItem","level":170,"item":"Fire Stone"}`.
- **Look**: from our red Simisear with its flame-shaped crest and tail: taller, the crest a crown of three flames, embers along its arms (Glow, orange) and the tail a torch with a cup of fire at its end. **New feature**: the torch of a tail.
- **Pokédex**: It keeps the fire at the heart of its troop alight through every winter. Its tail's flame is said to have never gone out.

### 5. Simitorrent, from Simipour (516): yes

- **Typing**: Water, kept. *Alternative*: Water/Fighting, for all three.
- **The Torrent Monkey Pokémon**, No. 2516. 1.2 m, 39.0 kg (Simipour 1 m, 29 kg).
- **Stats**: 75 / 98 / 63 / 98 / 63 / 101 = 498 → **85 / 113 / 78 / 113 / 78 / 111 = 578** (+10 HP +15 Atk +15 Def +15 SpA +15 SpD +10 Spe): the rise goes to its Attack, Defense, Sp. Atk and Sp. Def, then its HP and Speed.
- **Abilities**: Gluttony, hidden Torrent (was Gluttony, hidden Torrent).
- **Evolution**: Simipour by a Water Stone used on it at level 170 or above. Data: `{"targetSpecies":"Simitorrent","method":"UseItem","level":170,"item":"Water Stone"}`.
- **Look**: from our blue Simipour with its fountain crest: taller, the crest a crown of three jets, a waterfall of a tail that pours and refills and a pale band of foam round its wrists. **New feature**: the waterfall of a tail.
- **Pokédex**: The water in its tail never runs dry. Farmers in a drought leave fruit out in the hope that one will visit their fields.

### 6. Somnarna, from Musharna (518): yes

- **Typing**: Psychic → **Psychic/Fairy**. *Alternative*: Psychic/Ghost, the nightmares it eats.
- **The Dreamweaver Pokémon**, No. 2518. 1.5 m, 85.0 kg (Musharna 1.1 m, 60.5 kg).
- **Stats**: 116 / 55 / 85 / 107 / 95 / 29 = 487 → **116 / 60 / 105 / 132 / 110 / 44 = 567** (+5 Atk +20 Def +25 SpA +15 SpD +15 Spe): the rise goes to its Sp. Atk, then its Defense, Sp. Def, Speed and Attack.
- **Abilities**: Forewarn, Synchronize, hidden Telepathy (was Forewarn, Synchronize, hidden Telepathy).
- **Evolution**: Musharna by a Moon Stone used on it at level 170 or above. Data: `{"targetSpecies":"Somnarna","method":"UseItem","level":170,"item":"Moon Stone"}`.
- **Look**: from our pink Musharna with its flower patterns and the smoke from its brow: larger and rounder, its eyes half open now, the smoke grown into a ring of cloud round its body with shapes of dreams in it (decals, Glow, pink) and a band of stars across its back. **New feature**: the ring of dream-cloud.
- **Pokédex**: It sleeps above a sleeping town and weaves the town's dreams into its cloud. Nobody it watches over has a bad dream.

### 7. Stormstrika, from Zebstrika (523): yes

- **Typing**: Electric, kept. *Alternative*: Electric/Ground, a stampede.
- **The Storm Steed Pokémon**, No. 2523. 2.0 m, 110.0 kg (Zebstrika 1.6 m, 79.5 kg).
- **Stats**: 75 / 100 / 63 / 80 / 63 / 116 = 497 → **85 / 120 / 83 / 85 / 83 / 121 = 577** (+10 HP +20 Atk +20 Def +5 SpA +20 SpD +5 Spe): the rise goes to its Attack, Defense and Sp. Def, then its HP, Sp. Atk and Speed.
- **Abilities**: Lightning Rod, Motor Drive, hidden Sap Sipper (was Lightning Rod, Motor Drive, hidden Sap Sipper).
- **Evolution**: Zebstrika at level 170, knowing Wild Charge. Data: `{"targetSpecies":"Stormstrika","method":"LevelKnowsMove","level":170,"move":"Wild Charge"}`.
- **Look**: from our black and white Zebstrika with its lightning mane: larger, the mane grown long and forked like a bolt in two branches, its stripes lit at the edges (Glow, white-blue) and its hooves ringed with sparks. **New feature**: the forked mane.
- **Pokédex**: Its gallop gathers a thundercloud above it. A herd of them crossing the plains is mistaken for a storm on the horizon.

### 8. Amorabat, from Swoobat (528): strong yes

- **Typing**: Psychic/Flying, kept. *Alternative*: Psychic/Fairy, its heart-shaped nose.
- **The Heartstruck Pokémon**, No. 2528. 1.2 m, 16.0 kg (Swoobat 0.9 m, 10.5 kg).
- **Stats**: 67 / 57 / 55 / 77 / 55 / 114 = 425 → **82 / 57 / 75 / 102 / 70 / 119 = 505** (+15 HP +20 Def +25 SpA +15 SpD +5 Spe): the rise goes to its Sp. Atk, then its Defense, HP, Sp. Def and Speed.
- **Abilities**: Unaware, Klutz, hidden Simple (was Unaware, Klutz, hidden Simple).
- **Evolution**: Swoobat at level 166, with friendship 220 or more, at night. Data: `{"targetSpecies":"Amorabat","method":"FriendshipNight","level":166}`.
- **Look**: from our blue Swoobat with its heart-shaped nose: wider wings with a scalloped edge, the nose's heart doubled into a pair (Glow, pink) and a long tail of fur like a ribbon. **New feature**: the pair of hearts.
- **Pokédex**: It flies to anyone who is crying and beats its wings in time with their heart until they are calm.

### 9. Excavarch, from Excadrill (530): yes

- **Typing**: Ground/Steel, kept. *Alternative*: Ground/Rock.
- **The Tunnel King Pokémon**, No. 2530. 1.0 m, 62.0 kg (Excadrill 0.7 m, 40.4 kg).
- **Stats**: 110 / 135 / 60 / 50 / 65 / 88 = 508 → **110 / 145 / 90 / 50 / 95 / 98 = 588** (+10 Atk +30 Def +30 SpD +10 Spe): the rise goes to its Defense and Sp. Def, then its Attack and Speed.
- **Abilities**: Sand Rush, Sand Force, hidden Mold Breaker (was Sand Rush, Sand Force, hidden Mold Breaker).
- **Evolution**: Excadrill at level 176, holding a Soft Sand (used up). Data: `{"targetSpecies":"Excavarch","method":"LevelHoldingItem","level":176,"item":"Soft Sand"}`.
- **Look**: from our grey and red Excadrill with its drill crest and steel claws: broader, its crest a drill of two spirals turning against each other, its claws grown into three blades each, plates on its back like a miner's helmet. **New feature**: the double-spiral crest.
- **Pokédex**: Cities lay their subways along the tunnels it leaves. It can bore straight through a mountain in a single night.

### 10. Remedino, from Audino (531): strong yes

- **Typing**: Normal → **Normal/Fairy**. *Alternative*: Normal alone.
- **The Physician Pokémon**, No. 2531. 1.4 m, 45.0 kg (Audino 1.1 m, 31 kg).
- **Stats**: 103 / 60 / 86 / 60 / 86 / 50 = 445 → **108 / 60 / 106 / 90 / 106 / 55 = 525** (+5 HP +20 Def +30 SpA +20 SpD +5 Spe): the rise goes to its Sp. Atk, then its Defense, Sp. Def, HP and Speed.
- **Abilities**: Healer, Regenerator, hidden Klutz (was Healer, Regenerator, hidden Klutz).
- **Evolution**: Audino at level 168, with friendship 220 or more, by day. Data: `{"targetSpecies":"Remedino","method":"FriendshipDay","level":168}`.
- **Look**: from our pink and cream Audino with its long ear-feelers: taller, the ear-feelers grown into coiled feelers that end in rings like a stethoscope's, a white apron of fur at its front and a red cross of fur on its forehead. **New feature**: the coiled ear-feelers.
- **Pokédex**: It listens to a patient's heartbeat and knows at once what ails them. Hospitals in Unova keep one on every ward.

### 11. Throjutsu, from Throh (538): yes

- **Typing**: Fighting, kept. *Alternative*: Fighting/Ground.
- **The Black Belt Pokémon**, No. 2538. 1.6 m, 80.0 kg (Throh 1.3 m, 55.5 kg).
- **Stats**: 120 / 100 / 85 / 30 / 85 / 45 = 465 → **120 / 120 / 105 / 30 / 105 / 65 = 545** (+20 Atk +20 Def +20 SpD +20 Spe): the rise goes to its Attack, Defense, Sp. Def and Speed.
- **Abilities**: Guts, Inner Focus, hidden Mold Breaker (was Guts, Inner Focus, hidden Mold Breaker).
- **Evolution**: Throh at level 172, with a Sawk in the party. Data: `{"targetSpecies":"Throjutsu","method":"LevelWithSpeciesInParty","level":172,"species":"Sawk"}`.
- **Look**: from our red Throh with its white belt: taller and heavier, its belt black with a gold edge, a second belt tied across its chest and its brow set in a heavy frown. **New feature**: the black belt edged in gold.
- **Pokédex**: It ties a new belt each time it beats a rival worth the name. One was seen wearing seven at once.

### 12. Sawkendo, from Sawk (539): yes

- **Typing**: Fighting, kept. *Alternative*: Fighting/Steel.
- **The Karate Master Pokémon**, No. 2539. 1.6 m, 68.0 kg (Sawk 1.4 m, 51 kg).
- **Stats**: 75 / 125 / 75 / 30 / 75 / 85 = 465 → **90 / 140 / 95 / 30 / 95 / 95 = 545** (+15 HP +15 Atk +20 Def +20 SpD +10 Spe): the rise goes to its Defense and Sp. Def, then its HP, Attack and Speed.
- **Abilities**: Sturdy, Inner Focus, hidden Mold Breaker (was Sturdy, Inner Focus, hidden Mold Breaker).
- **Evolution**: Sawk at level 172, with a Throh in the party. Data: `{"targetSpecies":"Sawkendo","method":"LevelWithSpeciesInParty","level":172,"species":"Throh"}`.
- **Look**: from our blue Sawk with its black belt: taller, the belt tied with long ends, guards of hardened skin on its forearms like armour, and a white band round its brow. **New feature**: the forearm guards.
- **Pokédex**: Its strikes are so fast that a log it has struck falls apart only after it has bowed and walked away.

### 13. Cottonimbus, from Whimsicott (547): yes

- **Typing**: Grass/Fairy, kept. *Alternative*: Grass/Flying.
- **The Cloud Puff Pokémon**, No. 2547. 1.0 m, 9.0 kg (Whimsicott 0.7 m, 6.6 kg).
- **Stats**: 60 / 67 / 85 / 77 / 75 / 116 = 480 → **80 / 67 / 100 / 102 / 95 / 116 = 560** (+20 HP +15 Def +25 SpA +20 SpD): the rise goes to its Sp. Atk, then its HP, Sp. Def and Defense.
- **Abilities**: Prankster, Infiltrator, hidden Chlorophyll (was Prankster, Infiltrator, hidden Chlorophyll).
- **Evolution**: Whimsicott by a Sun Stone used on it at level 172 or above. Data: `{"targetSpecies":"Cottonimbus","method":"UseItem","level":172,"item":"Sun Stone"}`.
- **Look**: from our green and cream Whimsicott with its cotton mane: larger, the cotton grown into a cloud it rides on, small puffs of cotton drifting off it (decals) and its horns longer and curled. **New feature**: the cloud it rides.
- **Pokédex**: It drifts on the wind from one end of the region to the other. A town it passes over wakes to find its streets white with cotton.

### 14. Ballerilly, from Lilligant (549): yes

- **Typing**: Grass → **Grass/Fairy**. *Alternative*: Grass alone.
- **The Prima Flower Pokémon**, No. 2549. 1.4 m, 22.0 kg (Lilligant 1.1 m, 16.3 kg).
- **Stats**: 70 / 60 / 75 / 110 / 75 / 90 = 480 → **85 / 60 / 95 / 130 / 95 / 95 = 560** (+15 HP +20 Def +20 SpA +20 SpD +5 Spe): the rise goes to its Defense, Sp. Atk and Sp. Def, then its HP and Speed.
- **Abilities**: Chlorophyll, Own Tempo, hidden Leaf Guard (was Chlorophyll, Own Tempo, hidden Leaf Guard).
- **Evolution**: Lilligant by a Sun Stone used on it at level 172 or above. Data: `{"targetSpecies":"Ballerilly","method":"UseItem","level":172,"item":"Sun Stone"}`.
- **Look**: from our green Lilligant with its red flower crown: taller, the crown a ring of three flowers, its leaf-skirt in two tiers like a tutu and long ribbons of leaf from its arms. **New feature**: the tutu of two tiers.
- **Pokédex**: Its dance makes the flowers of a meadow turn to follow it. Only those who live long with it ever see it dance its full piece.

### 15. Bascurage, from Basculin (550): maybe

- **Typing**: Water → **Water/Dark**. *Alternative*: Water alone.
- **The Fury Fish Pokémon**, No. 2550. 1.4 m, 30.0 kg (Basculin 1 m, 18 kg).
- **Stats**: 70 / 92 / 65 / 80 / 55 / 98 = 460 → **85 / 112 / 80 / 85 / 75 / 103 = 540** (+15 HP +20 Atk +15 Def +5 SpA +20 SpD +5 Spe): the rise goes to its Attack and Sp. Def, then its HP, Defense, Sp. Atk and Speed.
- **Abilities**: Reckless, Adaptability, hidden Mold Breaker (was Reckless, Adaptability, hidden Mold Breaker).
- **Evolution**: Basculin at level 172, knowing Head Smash. Data: `{"targetSpecies":"Bascurage","method":"LevelKnowsMove","level":172,"move":"Head Smash"}`.
- **Look**: from our green Basculin with its stripe and its jaw: longer, the jaw heavier with a row of teeth that show, a fin of jagged rays along its back and the stripe split into two scars. **New feature**: the jagged fin.
- **Pokédex**: It leads the shoals that fight over a river. Where it swims no other fish, of any kind, will stay.

### 16. Darumandala, from Darmanitan (555): yes

- **Typing**: Fire → **Fire/Psychic**. *Alternative*: Fire alone.
- **The Meditation Pokémon**, No. 2555. 1.7 m, 140.0 kg (Darmanitan 1.3 m, 92.9 kg).
- **Stats**: 105 / 140 / 55 / 30 / 55 / 95 = 480 → **105 / 150 / 85 / 30 / 85 / 105 = 560** (+10 Atk +30 Def +30 SpD +10 Spe): the rise goes to its Defense and Sp. Def, then its Attack and Speed.
- **Abilities**: Sheer Force, hidden **Inner Focus** (was Sheer Force, hidden Zen Mode).
- **Evolution**: Darmanitan at level 174, by day. Data: `{"targetSpecies":"Darumandala","method":"LevelDay","level":174}`.
- **Look**: from our red Darmanitan with its brows of flame: larger, seated, its fire burning in a ring behind its back like a halo (Glow, orange), its brows longer and its arms crossed over a round belly marked like a daruma's. **New feature**: the ring of fire at its back.
- **Pokédex**: It sits in meditation for days at a time, and the fire within it grows hotter the stiller it sits. A blow from it melts stone.

### 17. Mariachactus, from Maractus (556): strong yes

- **Typing**: Grass → **Grass/Ground**. *Alternative*: Grass/Fairy.
- **The Desert Band Pokémon**, No. 2556. 1.5 m, 45.0 kg (Maractus 1 m, 28 kg).
- **Stats**: 75 / 86 / 67 / 106 / 67 / 60 = 461 → **90 / 96 / 87 / 121 / 87 / 60 = 541** (+15 HP +10 Atk +20 Def +15 SpA +20 SpD): the rise goes to its Defense and Sp. Def, then its HP, Sp. Atk and Attack.
- **Abilities**: Water Absorb, Chlorophyll, hidden Storm Drain (was Water Absorb, Chlorophyll, hidden Storm Drain).
- **Evolution**: Maractus at level 172, knowing Petal Dance. Data: `{"targetSpecies":"Mariachactus","method":"LevelKnowsMove","level":172,"move":"Petal Dance"}`.
- **Look**: from our green Maractus with its maraca arms and pink flowers: taller, a second pair of arms that are maracas too, a sombrero of flowers on its head and spines along its body like the strings of a guitar. **New feature**: the second pair of maracas.
- **Pokédex**: Its rhythm carries for miles over the sand. Travellers lost in the desert follow it to an oasis.

### 18. Crustadel, from Crustle (558): yes

- **Typing**: Bug/Rock, kept. *Alternative*: Bug/Steel.
- **The Citadel Pokémon**, No. 2558. 1.9 m, 300.0 kg (Crustle 1.4 m, 200 kg).
- **Stats**: 70 / 105 / 125 / 65 / 75 / 45 = 485 → **90 / 120 / 125 / 70 / 100 / 60 = 565** (+20 HP +15 Atk +5 SpA +25 SpD +15 Spe): the rise goes to its Sp. Def, then its HP, Attack, Speed and Sp. Atk.
- **Abilities**: Sturdy, Shell Armor, hidden Weak Armor (was Sturdy, Shell Armor, hidden Weak Armor).
- **Evolution**: Crustle at level 174, holding a Hard Stone (used up). Data: `{"targetSpecies":"Crustadel","method":"LevelHoldingItem","level":174,"item":"Hard Stone"}`.
- **Look**: from our orange Crustle with its stone slab: larger, the slab grown into a citadel of three tiers with towers at its corners and windows, its claws heavier and its legs stouter. **New feature**: the towered slab.
- **Pokédex**: Small Pokémon make their homes in the windows of its shell. A whole colony moves wherever it goes.

### 19. Brawlhood, from Scrafty (560): yes

- **Typing**: Dark/Fighting, kept. *Alternative*: Dark/Poison.
- **The Gang Boss Pokémon**, No. 2560. 1.4 m, 42.0 kg (Scrafty 1.1 m, 30 kg).
- **Stats**: 65 / 90 / 115 / 45 / 115 / 58 = 488 → **85 / 115 / 120 / 45 / 120 / 83 = 568** (+20 HP +25 Atk +5 Def +5 SpD +25 Spe): the rise goes to its Attack and Speed, then its HP, Defense and Sp. Def.
- **Abilities**: Shed Skin, Moxie, hidden Intimidate (was Shed Skin, Moxie, hidden Intimidate).
- **Evolution**: Scrafty at level 174, knowing High Jump Kick. Data: `{"targetSpecies":"Brawlhood","method":"LevelKnowsMove","level":174,"move":"High Jump Kick"}`.
- **Look**: from our orange Scrafty with its yellow skin pulled up and its red crest: taller, the crest grown into a pompadour, the shed skin a long coat with a collar and its arms heavier. **New feature**: the long coat of shed skin.
- **Pokédex**: Every Scraggy in a territory answers to it. It settles their quarrels with one headbutt to each side.

### 20. Glyphoracle, from Sigilyph (561): yes

- **Typing**: Psychic/Flying, kept. *Alternative*: Psychic/Rock, its ancient city.
- **The Oracle Pokémon**, No. 2561. 1.8 m, 22.0 kg (Sigilyph 1.4 m, 14 kg).
- **Stats**: 72 / 58 / 80 / 103 / 80 / 97 = 490 → **87 / 58 / 95 / 128 / 95 / 107 = 570** (+15 HP +15 Def +25 SpA +15 SpD +10 Spe): the rise goes to its Sp. Atk, then its HP, Defense, Sp. Def and Speed.
- **Abilities**: Wonder Skin, Magic Guard, hidden Tinted Lens (was Wonder Skin, Magic Guard, hidden Tinted Lens).
- **Evolution**: Sigilyph at level 174, knowing Cosmic Power. Data: `{"targetSpecies":"Glyphoracle","method":"LevelKnowsMove","level":174,"move":"Cosmic Power"}`.
- **Look**: from our black and colourful Sigilyph with its single eye: larger, two more wings, its body's patterns grown into rows of glyphs (Glow, gold) and a second, closed eye above the first. **New feature**: the second eye.
- **Pokédex**: The glyphs on its body change as it flies. Scholars have spent lives copying them and believe they foretell what is to come.

### 21. Pharaurum, from Cofagrigus (563): strong yes

- **Typing**: Ghost → **Ghost/Steel**. *Alternative*: Ghost/Ground, a pyramid.
- **The Pharaoh Pokémon**, No. 2563. 2.2 m, 150.0 kg (Cofagrigus 1.7 m, 76.5 kg).
- **Stats**: 58 / 50 / 145 / 95 / 105 / 30 = 483 → **88 / 50 / 145 / 120 / 120 / 40 = 563** (+30 HP +25 SpA +15 SpD +10 Spe): the rise goes to its HP, then its Sp. Atk, Sp. Def and Speed.
- **Abilities**: Mummy (was Mummy).
- **Evolution**: Cofagrigus at level 175, holding a Reaper Cloth (used up). Data: `{"targetSpecies":"Pharaurum","method":"LevelHoldingItem","level":175,"item":"Reaper Cloth"}`.
- **Look**: from our gold Cofagrigus with its shadow hands: taller, the coffin's lid a gold mask with a headdress of blue and gold stripes, six shadow hands, and its body a sarcophagus inside a second case. **New feature**: the striped headdress.
- **Pokédex**: It lies still in ruins for centuries. Those who try to take the gold from its body are found years later sealed inside it.

### 22. Tortugarch, from Carracosta (565): yes

- **Typing**: Water/Rock, kept. *Alternative*: Water/Dragon.
- **The Ancient Shell Pokémon**, No. 2565. 1.6 m, 150.0 kg (Carracosta 1.2 m, 81 kg).
- **Stats**: 74 / 108 / 133 / 83 / 65 / 32 = 495 → **89 / 123 / 133 / 93 / 95 / 42 = 575** (+15 HP +15 Atk +10 SpA +30 SpD +10 Spe): the rise goes to its Sp. Def, then its HP, Attack, Sp. Atk and Speed.
- **Abilities**: Solid Rock, Sturdy, hidden Swift Swim (was Solid Rock, Sturdy, hidden Swift Swim).
- **Evolution**: Carracosta at level 178, holding a Rare Bone (used up). Data: `{"targetSpecies":"Tortugarch","method":"LevelHoldingItem","level":178,"item":"Rare Bone"}`.
- **Look**: from our blue and black Carracosta with its shell: larger, its shell ridged with three keels and spikes along its rim, its flippers grown into broad paddles and its jaw a hooked beak. **New feature**: the three keels.
- **Pokédex**: It lived in the seas before the land was dry. Its shell is so old that coral grew and died on it while it slept.

### 23. Primavian, from Archeops (567): yes

- **Typing**: Rock/Flying, kept. *Alternative*: Rock/Dragon.
- **The First Flight Pokémon**, No. 2567. 1.7 m, 42.0 kg (Archeops 1.4 m, 32 kg).
- **Stats**: 75 / 140 / 65 / 112 / 65 / 110 = 567 → **85 / 140 / 75 / 112 / 78 / 110 = 600** (+10 HP +10 Def +13 SpD): the rise is +33, to the 600 ceiling: it goes to its Sp. Def, then its HP and Defense.
- **Abilities**: Defeatist (was Defeatist).
- **Evolution**: Archeops at level 178, holding a Rare Bone (used up). Data: `{"targetSpecies":"Primavian","method":"LevelHoldingItem","level":178,"item":"Rare Bone"}`.
- **Look**: from our yellow, red and blue Archeops: larger, its feathered wings grown full with long blue flight feathers, a crest of red quills and a tail of long feathers like a fan. **New feature**: the full flight feathers.
- **Pokédex**: Its wings at last carry it as high as it always wanted to fly. Seen from below its tail feathers spread like a rainbow.

### 24. Scrapodor, from Garbodor (569): yes

- **Typing**: Poison → **Poison/Steel**. *Alternative*: Poison/Dark.
- **The Scrap Heap Pokémon**, No. 2569. 2.3 m, 160.0 kg (Garbodor 1.9 m, 107.3 kg).
- **Stats**: 80 / 95 / 82 / 60 / 82 / 75 = 474 → **95 / 115 / 102 / 60 / 102 / 80 = 554** (+15 HP +20 Atk +20 Def +20 SpD +5 Spe): the rise goes to its Attack, Defense and Sp. Def, then its HP and Speed.
- **Abilities**: Stench, Weak Armor, hidden Aftermath (was Stench, Weak Armor, hidden Aftermath).
- **Evolution**: Garbodor at level 172, knowing Gunk Shot. Data: `{"targetSpecies":"Scrapodor","method":"LevelKnowsMove","level":172,"move":"Gunk Shot"}`.
- **Look**: from our green Garbodor with its trash-bag arms: larger, its body plated with bent metal and bottle caps, its arm-ends grown into the scoops of a digger and a crown of pipes letting out fumes (decals). **New feature**: the crown of pipes.
- **Pokédex**: It lives in scrapyards and eats what the crushers leave. A city where one lives has no rubbish, and no clean air.

### 25. Zoromirage, from Zoroark (571): strong yes

- **Typing**: Dark, kept. *Alternative*: Dark/Ghost.
- **The Mirage Fox Pokémon**, No. 2571. 1.8 m, 95.0 kg (Zoroark 1.6 m, 81.1 kg).
- **Stats**: 60 / 105 / 60 / 120 / 60 / 105 = 510 → **80 / 110 / 85 / 125 / 85 / 105 = 590** (+20 HP +5 Atk +25 Def +5 SpA +25 SpD): the rise goes to its Defense and Sp. Def, then its HP, Attack and Sp. Atk.
- **Abilities**: Illusion (was Illusion).
- **Evolution**: Zoroark at level 178, at night. Data: `{"targetSpecies":"Zoromirage","method":"LevelNight","level":178}`.
- **Look**: from our grey and red Zoroark with its long mane: taller, the mane longer and split into nine locks each with a teal bead, a haze round its body (decals) where it is never quite there. **New feature**: the nine locks of its mane.
- **Pokédex**: It can show a whole town an illusion at once. Travellers have spent a night in an inn that was gone by morning.

### 26. Chinchillux, from Cinccino (573): strong yes

- **Typing**: Normal → **Normal/Fairy**. *Alternative*: Normal alone.
- **The Silk Scarf Pokémon**, No. 2573. 0.8 m, 12.0 kg (Cinccino 0.5 m, 7.5 kg).
- **Stats**: 75 / 95 / 60 / 65 / 60 / 115 = 470 → **90 / 110 / 80 / 70 / 80 / 120 = 550** (+15 HP +15 Atk +20 Def +5 SpA +20 SpD +5 Spe): the rise goes to its Defense and Sp. Def, then its HP, Attack, Sp. Atk and Speed.
- **Abilities**: Cute Charm, Technician, hidden Skill Link (was Cute Charm, Technician, hidden Skill Link).
- **Evolution**: Cinccino by a Shiny Stone used on it at level 170 or above. Data: `{"targetSpecies":"Chinchillux","method":"UseItem","level":170,"item":"Shiny Stone"}`.
- **Look**: from our grey Cinccino with its white scarf of fur: longer, the scarf grown into a long stole that trails behind it, its ears tufted and its tail a plume with a shine (Glow, pale). **New feature**: the trailing stole.
- **Pokédex**: Its fur shines so that dust cannot settle on it. Weavers have tried for centuries to make cloth as fine as its stole.

### 27. Cygnelle, from Swanna (581): yes

- **Typing**: Water/Flying, kept. *Alternative*: Water/Fairy.
- **The Swan Queen Pokémon**, No. 2581. 1.7 m, 34.0 kg (Swanna 1.3 m, 24.2 kg).
- **Stats**: 75 / 87 / 63 / 87 / 63 / 98 = 473 → **90 / 97 / 83 / 97 / 83 / 103 = 553** (+15 HP +10 Atk +20 Def +10 SpA +20 SpD +5 Spe): the rise goes to its Defense and Sp. Def, then its HP, Attack, Sp. Atk and Speed.
- **Abilities**: Keen Eye, Big Pecks, hidden Hydration (was Keen Eye, Big Pecks, hidden Hydration).
- **Evolution**: Swanna at level 172, knowing Hurricane. Data: `{"targetSpecies":"Cygnelle","method":"LevelKnowsMove","level":172,"move":"Hurricane"}`.
- **Look**: from our white Swanna with its blue crest: larger, the crest grown into a crown of feathers, its neck longer and its wings with long trailing feathers like a gown. **New feature**: the crown of feathers.
- **Pokédex**: At dawn it dances on the lake and every swan of the lake dances with it. The ripples of the dance can be seen from the mountains.

### 28. Seasonstag, from Sawsbuck (586): strong yes

- **Typing**: Normal/Grass, kept. *Alternative*: Grass/Fairy.
- **The Four Seasons Pokémon**, No. 2586. 2.3 m, 130.0 kg (Sawsbuck 1.9 m, 92.5 kg).
- **Stats**: 80 / 100 / 70 / 60 / 70 / 95 = 475 → **90 / 120 / 90 / 60 / 90 / 105 = 555** (+10 HP +20 Atk +20 Def +20 SpD +10 Spe): the rise goes to its Attack, Defense and Sp. Def, then its HP and Speed.
- **Abilities**: Chlorophyll, Sap Sipper, hidden Serene Grace (was Chlorophyll, Sap Sipper, hidden Serene Grace).
- **Evolution**: Sawsbuck at level 174. Data: `{"targetSpecies":"Seasonstag","method":"Level","level":174}`.
- **Look**: from our brown Sawsbuck with its antlers: larger, its antlers grown into four branches, each in a season of its own: blossom, green leaves, red leaves and snow; its coat a single deep brown and a white chest. **New feature**: the antlers of four seasons.
- **Pokédex**: Its antlers bear all four seasons at once. Where it rests, spring and winter meet in the same clearing.

### 29. Nimbolga, from Emolga (587): strong yes

- **Typing**: Electric/Flying, kept. *Alternative*: Electric/Fairy.
- **The Storm Kite Pokémon**, No. 2587. 0.7 m, 9.0 kg (Emolga 0.4 m, 5 kg).
- **Stats**: 55 / 75 / 60 / 75 / 60 / 103 = 428 → **75 / 85 / 75 / 90 / 75 / 108 = 508** (+20 HP +10 Atk +15 Def +15 SpA +15 SpD +5 Spe): the rise goes to its HP, then its Defense, Sp. Atk, Sp. Def, Attack and Speed.
- **Abilities**: Static, hidden Motor Drive (was Static, hidden Motor Drive).
- **Evolution**: Emolga at level 167, holding a Cell Battery (used up). Data: `{"targetSpecies":"Nimbolga","method":"LevelHoldingItem","level":167,"item":"Cell Battery"}`.
- **Look**: from our white and black Emolga with its yellow membrane: larger, the membrane grown into a diamond like a kite's with a frame of black, a tail of small clouds on a string of sparks (Glow, yellow) and its cheeks larger. **New feature**: the kite's diamond.
- **Pokédex**: It flies up into thunderclouds and comes down charged for a week. Children fly kites beside it on stormy days.

### 30. Lancelier, from Escavalier (589): yes

- **Typing**: Bug/Steel, kept. *Alternative*: Bug/Fighting.
- **The Paladin Pokémon**, No. 2589. 1.4 m, 55.0 kg (Escavalier 1 m, 33 kg).
- **Stats**: 70 / 135 / 105 / 60 / 105 / 20 = 495 → **95 / 145 / 115 / 60 / 115 / 45 = 575** (+25 HP +10 Atk +10 Def +10 SpD +25 Spe): the rise goes to its HP and Speed, then its Attack, Defense and Sp. Def.
- **Abilities**: Swarm, Shell Armor, hidden Overcoat (was Swarm, Shell Armor, hidden Overcoat).
- **Evolution**: Escavalier at level 176, with an Accelgor in the party. Data: `{"targetSpecies":"Lancelier","method":"LevelWithSpeciesInParty","level":176,"species":"Accelgor"}`.
- **Look**: from our grey and red Escavalier with its lances: taller, its lances longer and fluted, a plumed crest on its helmet, a shield of shell on one arm and a cape of red. **New feature**: the shield of shell.
- **Pokédex**: It rides out against anything that threatens its forest. Its lances strike before its enemy sees it move.

### 31. Sporemonarch, from Amoonguss (591): yes

- **Typing**: Grass/Poison, kept. *Alternative*: Grass/Ghost.
- **The Toadstool Pokémon**, No. 2591. 1.2 m, 30.0 kg (Amoonguss 0.6 m, 10.5 kg).
- **Stats**: 114 / 85 / 70 / 85 / 80 / 30 = 464 → **114 / 95 / 95 / 100 / 100 / 40 = 544** (+10 Atk +25 Def +15 SpA +20 SpD +10 Spe): the rise goes to its Defense, then its Sp. Def, Sp. Atk, Attack and Speed.
- **Abilities**: Effect Spore, hidden Regenerator (was Effect Spore, hidden Regenerator).
- **Evolution**: Amoonguss at level 174, in the rain. Data: `{"targetSpecies":"Sporemonarch","method":"LevelInRain","level":174}`.
- **Look**: from our white and red Amoonguss with its Poké Ball cap: larger, its cap broader with a scalloped rim, small toadstools growing round its foot in a ring and spores drifting (decals, Glow, pink). **New feature**: the ring of toadstools.
- **Pokédex**: It grows a fairy ring wherever it settles after rain. Anything that steps inside the ring falls asleep at once.

### 32. Jellicrown, from Jellicent (593): yes

- **Typing**: Water/Ghost, kept. *Alternative*: Water/Poison.
- **The Abyss Monarch Pokémon**, No. 2593. 2.8 m, 200.0 kg (Jellicent 2.2 m, 135 kg).
- **Stats**: 100 / 60 / 70 / 85 / 105 / 60 = 480 → **110 / 60 / 95 / 110 / 115 / 70 = 560** (+10 HP +25 Def +25 SpA +10 SpD +10 Spe): the rise goes to its Defense and Sp. Atk, then its HP, Sp. Def and Speed.
- **Abilities**: Water Absorb, Cursed Body, hidden Damp (was Water Absorb, Cursed Body, hidden Damp).
- **Evolution**: Jellicent at level 176, at night. Data: `{"targetSpecies":"Jellicrown","method":"LevelNight","level":176}`.
- **Look**: from our blue (or pink) Jellicent with its crown: larger, its crown grown taller with five points, its tentacles longer and frilled and a cape of membrane trailing from its back. **New feature**: the cape of membrane.
- **Pokédex**: It is said to rule a palace built of sunken ships far below the sea. Sailors bow when one surfaces beside them.

### 33. Alomatron, from Alomomola (594): yes

- **Typing**: Water → **Water/Fairy**. *Alternative*: Water alone.
- **The Guardian Pokémon**, No. 2594. 1.6 m, 50.0 kg (Alomomola 1.2 m, 31.6 kg).
- **Stats**: 165 / 75 / 80 / 40 / 45 / 65 = 470 → **165 / 85 / 105 / 55 / 75 / 65 = 550** (+10 Atk +25 Def +15 SpA +30 SpD): the rise goes to its Sp. Def, then its Defense, Sp. Atk and Attack.
- **Abilities**: Healer, Hydration, hidden Regenerator (was Healer, Hydration, hidden Regenerator).
- **Evolution**: Alomomola at level 175, knowing Healing Wish. Data: `{"targetSpecies":"Alomatron","method":"LevelKnowsMove","level":175,"move":"Healing Wish"}`.
- **Look**: from our pink Alomomola with its heart shape: larger, its membrane grown into a wide veil round its body, fins at its sides like open hands and a ring of light (Glow, pink) round it. **New feature**: the veil of membrane.
- **Pokédex**: Wounded Pokémon follow it across the open sea. It wraps the weakest in its veil and carries them to safe water.

### 34. Voltarantula, from Galvantula (596): yes

- **Typing**: Bug/Electric, kept. *Alternative*: Bug/Steel.
- **The Storm Spider Pokémon**, No. 2596. 1.2 m, 30.0 kg (Galvantula 0.8 m, 14.3 kg).
- **Stats**: 70 / 77 / 60 / 97 / 60 / 108 = 472 → **85 / 77 / 80 / 122 / 80 / 108 = 552** (+15 HP +20 Def +25 SpA +20 SpD): the rise goes to its Sp. Atk, then its Defense, Sp. Def and HP.
- **Abilities**: Compound Eyes, Unnerve, hidden Swarm (was Compound Eyes, Unnerve, hidden Swarm).
- **Evolution**: Galvantula at level 175, knowing Sticky Web. Data: `{"targetSpecies":"Voltarantula","method":"LevelKnowsMove","level":175,"move":"Sticky Web"}`.
- **Look**: from our yellow and blue Galvantula with its four eyes: larger, its legs longer and banded, a web of sparks strung between its legs (decals, Glow, yellow) and two more eyes. **New feature**: the web between its legs.
- **Pokédex**: It spins a web of electrified thread across a whole valley. Birds fly round the valley rather than over it.

### 35. Ferrospire, from Ferrothorn (598): yes

- **Typing**: Grass/Steel, kept. *Alternative*: Steel/Rock.
- **The Iron Thorn Pokémon**, No. 2598. 1.4 m, 180.0 kg (Ferrothorn 1 m, 110 kg).
- **Stats**: 74 / 94 / 131 / 54 / 116 / 20 = 489 → **99 / 114 / 131 / 69 / 126 / 30 = 569** (+25 HP +20 Atk +15 SpA +10 SpD +10 Spe): the rise goes to its HP, then its Attack, Sp. Atk, Sp. Def and Speed.
- **Abilities**: Iron Barbs, hidden Anticipation (was Iron Barbs, hidden Anticipation).
- **Evolution**: Ferrothorn at level 176, holding a Rocky Helmet (used up). Data: `{"targetSpecies":"Ferrospire","method":"LevelHoldingItem","level":176,"item":"Rocky Helmet"}`.
- **Look**: from our grey-green Ferrothorn with its three spiked tendrils: larger, five tendrils, each spike grown into a spire, green leaves at the base of its body and roots along its tendrils. **New feature**: the five tendrils.
- **Pokédex**: It roots itself to a cave's ceiling and grows downward for years. Miners find caves hung with its spires.

### 36. Cosmogrey, from Beheeyem (606): yes

- **Typing**: Psychic → **Psychic/Electric**. *Alternative*: Psychic/Steel.
- **The Overmind Pokémon**, No. 2606. 1.4 m, 50.0 kg (Beheeyem 1 m, 34.5 kg).
- **Stats**: 75 / 75 / 75 / 125 / 95 / 40 = 485 → **90 / 75 / 95 / 145 / 105 / 55 = 565** (+15 HP +20 Def +20 SpA +10 SpD +15 Spe): the rise goes to its Defense and Sp. Atk, then its HP, Speed and Sp. Def.
- **Abilities**: Telepathy, Synchronize, hidden Analytic (was Telepathy, Synchronize, hidden Analytic).
- **Evolution**: Beheeyem at level 178, at night. Data: `{"targetSpecies":"Cosmogrey","method":"LevelNight","level":178}`.
- **Look**: from our brown Beheeyem with its three-coloured fingers: taller, its head larger with a ridge across it, its three lights grown into a ring of lamps round it (Glow, red, yellow and green) and a cape of brown. **New feature**: the ring of lamps.
- **Pokédex**: Lights in the night sky are said to be its kind calling to one another. It rewrites what it has seen in anyone's memory.

### 37. Beartundra, from Beartic (614): yes

- **Typing**: Ice → **Ice/Fighting**. *Alternative*: Ice/Ground.
- **The Glacier Bear Pokémon**, No. 2614. 3.2 m, 380.0 kg (Beartic 2.6 m, 260 kg).
- **Stats**: 95 / 130 / 80 / 70 / 80 / 50 = 505 → **105 / 140 / 105 / 70 / 105 / 60 = 585** (+10 HP +10 Atk +25 Def +25 SpD +10 Spe): the rise goes to its Defense and Sp. Def, then its HP, Attack and Speed.
- **Abilities**: Snow Cloak, Slush Rush, hidden Swift Swim (was Snow Cloak, Slush Rush, hidden Swift Swim).
- **Evolution**: Beartic at level 178, holding a Never-Melt Ice (used up). Data: `{"targetSpecies":"Beartundra","method":"LevelHoldingItem","level":178,"item":"Never-Melt Ice"}`.
- **Look**: from our white Beartic with its icy beard: larger, its beard grown into a row of icicles, a ridge of glacier ice along its back and its claws icy blue. **New feature**: the ridge of glacier ice.
- **Pokédex**: It walks the frozen sea alone. The ice on its back is said to be older than the oldest town of the north.

### 38. Cryohedron, from Cryogonal (615): yes

- **Typing**: Ice → **Ice/Psychic**. *Alternative*: Ice alone.
- **The Crystal Lattice Pokémon**, No. 2615. 1.5 m, 220.0 kg (Cryogonal 1.1 m, 148 kg).
- **Stats**: 80 / 50 / 50 / 95 / 135 / 105 = 515 → **95 / 50 / 90 / 110 / 135 / 115 = 595** (+15 HP +40 Def +15 SpA +10 Spe): the rise goes to its Defense, then its HP, Sp. Atk and Speed.
- **Abilities**: Levitate (was Levitate).
- **Evolution**: Cryogonal at level 178, at an Ice Rock. Data: `{"targetSpecies":"Cryohedron","method":"LevelAtLocation","level":178,"location":"Ice Rock"}`.
- **Look**: from our blue Cryogonal with its six-pointed crystal and its chains: larger, the crystal grown into a lattice of three snowflakes at angles, its chains longer and its face in the middle of them all. **New feature**: the lattice of three snowflakes.
- **Pokédex**: It is born in clouds of snow and grows each winter by another flake. One was found large enough to stand inside.

### 39. Velocigor, from Accelgor (617): yes

- **Typing**: Bug, kept. *Alternative*: Bug/Water.
- **The Swift Shinobi Pokémon**, No. 2617. 1.1 m, 30.0 kg (Accelgor 0.8 m, 25.3 kg).
- **Stats**: 80 / 70 / 40 / 100 / 60 / 145 = 495 → **90 / 70 / 70 / 115 / 85 / 145 = 575** (+10 HP +30 Def +15 SpA +25 SpD): the rise goes to its Defense, then its Sp. Def, Sp. Atk and HP.
- **Abilities**: Hydration, Sticky Hold, hidden Unburden (was Hydration, Sticky Hold, hidden Unburden).
- **Evolution**: Accelgor at level 176, with an Escavalier in the party. Data: `{"targetSpecies":"Velocigor","method":"LevelWithSpeciesInParty","level":176,"species":"Escavalier"}`.
- **Look**: from our red Accelgor wrapped in its membrane: taller, the membrane grown into long trailing scarves, a mask with a pale band over its eyes and a stance like a dancer's on one foot. **New feature**: the trailing scarves.
- **Pokédex**: It wraps itself in layers of membrane against drying out. It moves so fast that only its scarves are seen.

### 40. Fluxflounder, from Stunfisk (618): yes

- **Typing**: Ground/Electric, kept. *Alternative*: Ground/Water.
- **The Mud Trap Pokémon**, No. 2618. 1.0 m, 22.0 kg (Stunfisk 0.7 m, 11 kg).
- **Stats**: 109 / 66 / 84 / 81 / 99 / 32 = 471 → **109 / 81 / 99 / 106 / 114 / 42 = 551** (+15 Atk +15 Def +25 SpA +15 SpD +10 Spe): the rise goes to its Sp. Atk, then its Attack, Defense, Sp. Def and Speed.
- **Abilities**: Static, Limber, hidden Sand Veil (was Static, Limber, hidden Sand Veil).
- **Evolution**: Stunfisk at level 172, in the rain. Data: `{"targetSpecies":"Fluxflounder","method":"LevelInRain","level":172}`.
- **Look**: from our brown Stunfisk with its yellow lips: larger and wider, a fringe of fins round its body like a flounder's, spots on its back that light up (Glow, yellow) and its grin broader. **New feature**: the fringe of fins.
- **Pokédex**: It lies in the mud for weeks and smiles. The spots on its back light up as a warning, but only a moment before it strikes.

### 41. Mienshifu, from Mienshao (620): yes

- **Typing**: Fighting, kept. *Alternative*: Fighting/Psychic.
- **The Grand Master Pokémon**, No. 2620. 1.7 m, 45.0 kg (Mienshao 1.4 m, 35.5 kg).
- **Stats**: 65 / 125 / 60 / 95 / 60 / 105 = 510 → **80 / 135 / 80 / 100 / 80 / 115 = 590** (+15 HP +10 Atk +20 Def +5 SpA +20 SpD +10 Spe): the rise goes to its Defense and Sp. Def, then its HP, Attack, Speed and Sp. Atk.
- **Abilities**: Inner Focus, Regenerator, hidden Reckless (was Inner Focus, Regenerator, hidden Reckless).
- **Evolution**: Mienshao at level 180, knowing Aura Sphere. Data: `{"targetSpecies":"Mienshifu","method":"LevelKnowsMove","level":180,"move":"Aura Sphere"}`.
- **Look**: from our purple and cream Mienshao with its sleeves: taller, the sleeves longer and ending in tassels, a long white beard of fur and a sash with tassels at its waist. **New feature**: the white beard.
- **Pokédex**: It teaches the Mienfoo of its mountain until they surpass it. It is said never to have struck first.

### 42. Drudrakon, from Druddigon (621): yes

- **Typing**: Dragon → **Dragon/Rock**. *Alternative*: Dragon/Steel.
- **The Cave Dragon Pokémon**, No. 2621. 2.0 m, 200.0 kg (Druddigon 1.6 m, 139 kg).
- **Stats**: 77 / 120 / 90 / 60 / 90 / 48 = 485 → **92 / 135 / 105 / 65 / 105 / 63 = 565** (+15 HP +15 Atk +15 Def +5 SpA +15 SpD +15 Spe): the rise goes to its HP, Attack, Defense, Sp. Def and Speed, then its Sp. Atk.
- **Abilities**: Rough Skin, Sheer Force, hidden Mold Breaker (was Rough Skin, Sheer Force, hidden Mold Breaker).
- **Evolution**: Druddigon at level 180, holding a Dragon Fang (used up). Data: `{"targetSpecies":"Drudrakon","method":"LevelHoldingItem","level":180,"item":"Dragon Fang"}`.
- **Look**: from our blue and red Druddigon with its rough red face: larger, its stubby wings grown out and able to spread, the red of its face a plated mask and ridges of rock down its back. **New feature**: the spread wings.
- **Pokédex**: For centuries its kind could not fly. This one rose from its cave once the sun had warmed it, and the cave was never empty again.

### 43. Colossurk, from Golurk (623): strong yes

- **Typing**: Ground/Ghost, kept. *Alternative*: Ground/Steel.
- **The Colossus Pokémon**, No. 2623. 3.6 m, 500.0 kg (Golurk 2.8 m, 330 kg).
- **Stats**: 89 / 124 / 80 / 55 / 80 / 55 = 483 → **104 / 139 / 95 / 60 / 95 / 70 = 563** (+15 HP +15 Atk +15 Def +5 SpA +15 SpD +15 Spe): the rise goes to its HP, Attack, Defense, Sp. Def and Speed, then its Sp. Atk.
- **Abilities**: Iron Fist, Klutz, hidden No Guard (was Iron Fist, Klutz, hidden No Guard).
- **Evolution**: Golurk at level 178, knowing Dynamic Punch. Data: `{"targetSpecies":"Colossurk","method":"LevelKnowsMove","level":178,"move":"Dynamic Punch"}`.
- **Look**: from our green Golurk with its seal across its chest: larger, a second seal over its head like a crown, plates of stone on its shoulders, its rocket feet grown wider and its eyes lit (Glow, yellow). **New feature**: the second seal.
- **Pokédex**: Its maker sealed a second band of power round its head. If both seals came off at once no one knows what it would do.

### 44. Bouffalanx, from Bouffalant (626): yes

- **Typing**: Normal → **Normal/Ground**. *Alternative*: Normal/Fighting.
- **The Stampede Pokémon**, No. 2626. 1.9 m, 150.0 kg (Bouffalant 1.6 m, 94.6 kg).
- **Stats**: 95 / 110 / 95 / 40 / 95 / 55 = 490 → **105 / 135 / 105 / 45 / 105 / 75 = 570** (+10 HP +25 Atk +10 Def +5 SpA +10 SpD +20 Spe): the rise goes to its Attack, then its Speed, HP, Defense, Sp. Def and Sp. Atk.
- **Abilities**: Reckless, Sap Sipper, hidden Soundproof (was Reckless, Sap Sipper, hidden Soundproof).
- **Evolution**: Bouffalant at level 176, knowing Head Charge. Data: `{"targetSpecies":"Bouffalanx","method":"LevelKnowsMove","level":176,"move":"Head Charge"}`.
- **Look**: from our brown Bouffalant with its afro: larger, the afro grown into a great mane that covers its shoulders, its horns longer and curved forward and its hooves heavier. **New feature**: the mane of afro.
- **Pokédex**: It charges headfirst through anything in its way. The ground shakes for a mile when it runs.

### 45. Imperavia, from Braviary (628): yes

- **Typing**: Normal/Flying, kept. *Alternative*: Fighting/Flying.
- **The Sky Emperor Pokémon**, No. 2628. 1.9 m, 60.0 kg (Braviary 1.5 m, 41 kg).
- **Stats**: 100 / 123 / 75 / 57 / 75 / 80 = 510 → **100 / 133 / 95 / 57 / 100 / 105 = 590** (+10 Atk +20 Def +25 SpD +25 Spe): the rise goes to its Sp. Def and Speed, then its Defense and Attack.
- **Abilities**: Keen Eye, Sheer Force, hidden Defiant (was Keen Eye, Sheer Force, hidden Defiant).
- **Evolution**: Braviary at level 182, by day. Data: `{"targetSpecies":"Imperavia","method":"LevelDay","level":182}`.
- **Look**: from our red, white and blue Braviary with its crest: larger, the crest grown into a plume of five feathers, its wings longer with blue tips and scars of battle on its chest feathers. **New feature**: the plume of five feathers.
- **Pokédex**: The more scars it carries, the more the flock respects it. It flies at the head of every battle.

### 46. Carrionqueen, from Mandibuzz (630): yes

- **Typing**: Dark/Flying, kept. *Alternative*: Dark/Ghost.
- **The Bone Queen Pokémon**, No. 2630. 1.5 m, 55.0 kg (Mandibuzz 1.2 m, 39.5 kg).
- **Stats**: 110 / 65 / 105 / 55 / 95 / 80 = 510 → **110 / 75 / 115 / 80 / 105 / 105 = 590** (+10 Atk +10 Def +25 SpA +10 SpD +25 Spe): the rise goes to its Sp. Atk and Speed, then its Attack, Defense and Sp. Def.
- **Abilities**: Big Pecks, Overcoat, hidden Weak Armor (was Big Pecks, Overcoat, hidden Weak Armor).
- **Evolution**: Mandibuzz at level 182, at night. Data: `{"targetSpecies":"Carrionqueen","method":"LevelNight","level":182}`.
- **Look**: from our brown and black Mandibuzz with its bone skirt: larger, the bones grown into a skirt and a crown of bones, its feathered ruff longer and its wings tattered at the tips. **New feature**: the crown of bones.
- **Pokédex**: It decorates its nest with the bones of what it has found. The larger the crown it wears, the more feared it is.

### 47. Pyrovore, from Heatmor (631): yes

- **Typing**: Fire, kept. *Alternative*: Fire/Ground.
- **The Furnace Pokémon**, No. 2631. 1.7 m, 80.0 kg (Heatmor 1.4 m, 58 kg).
- **Stats**: 85 / 97 / 66 / 105 / 66 / 65 = 484 → **95 / 112 / 86 / 120 / 86 / 65 = 564** (+10 HP +15 Atk +20 Def +15 SpA +20 SpD): the rise goes to its Defense and Sp. Def, then its Attack, Sp. Atk and HP.
- **Abilities**: Gluttony, Flash Fire, hidden White Smoke (was Gluttony, Flash Fire, hidden White Smoke).
- **Evolution**: Heatmor at level 176, after knocking out 3 Durant. Data: `{"targetSpecies":"Pyrovore","method":"LevelAfterDefeating","level":176,"species":"Durant","value":3}`.
- **Look**: from our red Heatmor with its tube snout and the vent in its tail: larger, its snout longer, the vent grown into a chimney with flames (Glow, orange) and its fur banded in grey like soot. **New feature**: the chimney of a tail.
- **Pokédex**: It breathes fire into the Durant's nests and melts their armour. Its tail burns like a forge.

### 48. Formicarch, from Durant (632): yes

- **Typing**: Bug/Steel, kept. *Alternative*: Bug/Rock.
- **The Iron Queen Pokémon**, No. 2632. 0.8 m, 70.0 kg (Durant 0.3 m, 33 kg).
- **Stats**: 58 / 109 / 112 / 48 / 48 / 109 = 484 → **88 / 119 / 112 / 48 / 78 / 119 = 564** (+30 HP +10 Atk +30 SpD +10 Spe): the rise goes to its HP and Sp. Def, then its Attack and Speed.
- **Abilities**: Swarm, Hustle, hidden Truant (was Swarm, Hustle, hidden Truant).
- **Evolution**: Durant at level 176, after knocking out 3 Heatmor. Data: `{"targetSpecies":"Formicarch","method":"LevelAfterDefeating","level":176,"species":"Heatmor","value":3}`.
- **Look**: from our grey Durant with its jaws: larger, its abdomen grown long and banded, wings folded on its back and a crown of steel horns. **New feature**: the folded wings.
- **Pokédex**: It rules a nest of a thousand Durant. Its armour does not melt even in Heatmor's fire.

### 49. Helioptera, from Volcarona (637): strong yes

- **Typing**: Bug/Fire, kept. *Alternative*: Fire/Fairy.
- **The Solar Flare Pokémon**, No. 2637. 2.0 m, 65.0 kg (Volcarona 1.6 m, 46 kg).
- **Stats**: 85 / 60 / 65 / 135 / 105 / 100 = 550 → **95 / 60 / 90 / 135 / 105 / 115 = 600** (+10 HP +25 Def +15 Spe): the rise is +50, to the 600 ceiling: it goes to its Defense, then its Speed and HP.
- **Abilities**: Flame Body, hidden Swarm (was Flame Body, hidden Swarm).
- **Evolution**: Volcarona at level 183, by day. Data: `{"targetSpecies":"Helioptera","method":"LevelDay","level":183}`.
- **Look**: from our white and red Volcarona with its six orange wings: larger, the wings grown into a ring of eight round its body like the sun's rays (Glow, orange), a corona of white fur and its blue eyes brighter. **New feature**: the ring of eight wings.
- **Pokédex**: It is said that when the sun was lost in a long winter, one of these took its place. Its wings give off light and warmth alike.

### 50. Cobaltarch, from Cobalion (638): maybe

- **Typing**: Steel/Fighting, kept. *Alternative*: Steel/Fairy, for all three Fairy in place of Fighting.
- **The Iron Resolve Pokémon**, No. 2638. 2.4 m, 300.0 kg (Cobalion 2.1 m, 250 kg).
- **Stats**: 91 / 90 / 129 / 90 / 72 / 108 = 580 → **106 / 110 / 129 / 105 / 97 / 113 = 660** (+15 HP +20 Atk +15 SpA +25 SpD +5 Spe): the rise goes to its Sp. Def, then its Attack, HP, Sp. Atk and Speed.
- **Abilities**: Justified (was Justified).
- **Evolution**: Cobalion at level 190, with a Keldeo in the party. Data: `{"targetSpecies":"Cobaltarch","method":"LevelWithSpeciesInParty","level":190,"species":"Keldeo"}`.
- **Look**: from our blue Cobalion with its horned crest: larger, its crest grown into a blade of steel that sweeps back, plates on its shoulders and a cape of blue fur at its neck. **New feature**: the blade of its crest.
- **Pokédex**: It is the one the others follow. Its resolve has never bent, and steel bends round it.

### 51. Terrakhan, from Terrakion (639): maybe

- **Typing**: Rock/Fighting, kept. *Alternative*: Rock/Fairy, for all three.
- **The Cavern Lord Pokémon**, No. 2639. 2.2 m, 310.0 kg (Terrakion 1.9 m, 260 kg).
- **Stats**: 91 / 129 / 90 / 72 / 90 / 108 = 580 → **106 / 139 / 110 / 77 / 115 / 113 = 660** (+15 HP +10 Atk +20 Def +5 SpA +25 SpD +5 Spe): the rise goes to its Sp. Def, then its Defense, HP, Attack, Sp. Atk and Speed.
- **Abilities**: Justified (was Justified).
- **Evolution**: Terrakion at level 190, with a Keldeo in the party. Data: `{"targetSpecies":"Terrakhan","method":"LevelWithSpeciesInParty","level":190,"species":"Keldeo"}`.
- **Look**: from our grey Terrakion with its great horns: larger, its horns grown into a crown of four, plates of stone on its back like a fortress and its face fur darker. **New feature**: the crown of four horns.
- **Pokédex**: It once broke a castle to free the Pokémon held there. It can carry the weight of a mountain on its back.

### 52. Virizenith, from Virizion (640): maybe

- **Typing**: Grass/Fighting, kept. *Alternative*: Grass/Fairy, for all three.
- **The Grassland Blade Pokémon**, No. 2640. 2.4 m, 220.0 kg (Virizion 2 m, 200 kg).
- **Stats**: 91 / 90 / 72 / 90 / 129 / 108 = 580 → **106 / 105 / 97 / 110 / 129 / 113 = 660** (+15 HP +15 Atk +25 Def +20 SpA +5 Spe): the rise goes to its Defense, then its Sp. Atk, HP, Attack and Speed.
- **Abilities**: Justified (was Justified).
- **Evolution**: Virizion at level 190, with a Keldeo in the party. Data: `{"targetSpecies":"Virizenith","method":"LevelWithSpeciesInParty","level":190,"species":"Keldeo"}`.
- **Look**: from our green Virizion with its leaf-blade horns: larger, the horns grown into long blades of leaf, a mane of leaves down its neck and petals in its tail. **New feature**: the mane of leaves.
- **Pokédex**: It moves through a forest without stirring a leaf. Its blades cut only those who have harmed the weak.

## Balance

Measured by the script, as the earlier batches':

- **Totals**: 500 (Beaconwatch) to 600 (Primavian and Helioptera, each risen only to the ceiling: +33 from Archeops's 567 and +50 from Volcarona's 550), the Swords of Justice 660. Nothing that isn't legendary passes 600.
- **Highest of each stat**: Attack 150 (Darumandala, from Darmanitan's 140, as high as the highest risen Attack of the earlier batches), then 145 (Excavarch and Lancelier). Sp. Atk 145 (Cosmogrey, from Beheeyem's 125), then Helioptera's 135, which is Volcarona's own kept. Speed 145 (Velocigor, Accelgor's own kept), then Stormstrika's 121. Defense 145 (Pharaurum, Cofagrigus's own) and Sp. Def 135 (Cryohedron, Cryogonal's own). HP 165 (Alomatron, Alomomola's own; its rise goes to the defences it lacked).
- **The weakest stay modest on purpose** (batch 1's answer: no floor): Beaconwatch 500, Amorabat 505, Nimbolga 508, Remedino 525 and Umbrapard 526, from the five lowest totals of the region's last stages and species alone.
- **Abilities**: every species keeps its own but Darmanitan, whose hidden Zen Mode, which changes Darmanitan's own shape and does nothing for any other species, becomes **Inner Focus** on Darumandala (question 3).

**Flagged as strong** (each inside the official range; the user may want one turned down):
1. **Helioptera**: Volcarona's Quiver Dance on 135 Sp. Atk and now 115 Speed and 90 Defense, which was already among the strongest set-up Pokémon of any game. *To turn down*: Speed 105 and the 10 to HP; or keep it a strong yes for its design and leave the rise at Defense.
2. **Excavarch**: Sand Rush and Mold Breaker with 145 Attack, and now 90 Defense and 95 Sp. Def. *To turn down*: Attack 135 and the 10 to HP.
3. **Alomatron**: Regenerator on 165 HP, and now 105 Defense and 75 Sp. Def, with Healing Wish and Wish to give it away. *To turn down*: Sp. Def 65 and the 10 to Attack.
4. **Darumandala**: Sheer Force on 150 Attack, as high as any batch's highest risen Attack. *To turn down*: Attack 140 (Darmanitan's own) and the 10 to Sp. Def.
5. **Amorabat**: Swoobat's hidden Simple, which doubles every stat it raises, with Calm Mind in its line and 119 Speed. Kept as Swoobat has it, by the settled rule.

**Weaker by design**: Primavian (Defeatist halves its Attack and Sp. Atk below half its HP, as Archeops's; its rise of 33 goes to its HP and defences, which is what keeps it above half) and Formicarch (Truant, Durant's hidden ability, is kept) are judged on their designs more than their numbers.

## Open questions: only what is new for Unova

The earlier batches' thirty-two are settled and applied (above). These are new.

1. **The level band.** Batch 3's rule is "within the region's own climb, at most 160", and Unova's whole climb lies above it: 162 on its first route, 185 at its Champion. *Default*: **the ceiling becomes the level cap's own (260) and the rule "within the region's own climb"**: 165 to 183 here, the Swords of Justice at 190 after the Champion, which leaves the four earlier batches' approved levels as they are. `SpeciesPastHundredTests`' climb by the forebear's generation gains a row for the fifth (165 to 183, legendaries 190). *Alternatives*: keep 160, and every Unova species of ours evolves as soon as it is caught in Unova (as batch 3 refused for Hoenn's); or one level for all, 165, so that each can evolve on Unova's first routes.
2. **The legendaries.** *Default*: the Swords of Justice as a trio, each at level 190 with a Keldeo in the party, to 660. *Alternatives*: a ring, each with the next of the three in the party (batch 4's lake guardians); each holding its type's plate (the Iron, Stone and Meadow Plates are in the data); or not at all. The Forces of Nature: no, since their Therian Formes are their further shapes (*alternative*: the three as a set to 660, though Landorus is already 600 and would rise only 60, by batch 3's eon pair). The Tao trio and the four mythicals: no.
3. **Darmanitan's Zen Mode.** The ability changes Darmanitan's own shape (`Darmanitan-Zen`) and does nothing for another species. *Default*: Darumandala's hidden ability is **Inner Focus**, its meditation kept as a mind no flinch can break. *Alternatives*: Zen Mode kept, doing nothing (batch 4 kept Flower Gift on Sakurabloom, but Flower Gift still works for any holder); or Flash Fire.
4. **Sawsbuck's seasons.** Deerling and Sawsbuck wear the season (`Sawsbuck-Summer` and the rest, `Models/Seasons.cs`), and ours have no forms. *Default*: Seasonstag carries all four seasons at once on four branches of its antlers, one look in every season, and every coat of Sawsbuck evolves into it. *Alternative*: four forms of ours by the season, against the settled rule.
5. **Basculin's stripes.** Basculin's red, blue and white stripes are forms of one species (`Basculin-Blue-Striped`, `Basculin-White-Striped`), and only the white-striped one already goes on, to Basculegion (Hisui's). *Default*: **maybe**; a Basculin of any stripe evolves into Bascurage by level, and the white-striped one keeps its own way to Basculegion as well (the engine lets a form take an evolution of the species whose target its own row doesn't name, `Evolution.FitsForm`). *Alternatives*: no, as batch 4's Wormadam; or only the red and blue ones (the white one refused by a row of its own), which would be the first evolution of ours that names a form.
6. **Jellicent's female.** The data keeps the pink female Jellicent as a form (`Jellicent-Female`), not as a female look. *Default*: Jellicrown has one species and a female look of its own (pink, the crown with a frill), as the female looks of batches 1 to 4; either Jellicent evolves into it. *Alternative*: one look, blue.
7. **Unova's fossils.** The Cover Fossil and the Plume Fossil are not in `items.json` (the data has the item table of Platinum and the later items the games share). *Default*: both hold a **Rare Bone** (used up), the old bone that both are said to have come from, so the pair stays one idea. *Alternatives*: each a type's item (Mystic Water for Carracosta, Sharp Beak for Archeops); or the two fossils added to the data as items of ours first.
8. **Throh's and Sawk's pairing.** *Default*: each with the other in the party, so a trainer raises both; Throh was White's and Sawk Black's, each the other's opposite, as Plusle and Minun were each other's partners. *Alternatives*: each holding a Black Belt; or each after knocking out three of the other (Heatmor and Durant's way).

## The table to answer

One row a species. The user answers each with yes, a change, or no; striking a row in the selection table above is a no.

| # | Ours | Typing | Abilities | HP/Atk/Def/SpA/SpD/Spe (total) | Δ | Evolves |
|---|---|---|---|---|---|---|
| 1 | Beaconwatch | Normal/Electric | Illuminate, Keen Eye, Analytic | 75/100/84/70/84/87 (500) | +15 HP +15 Atk +15 Def +10 SpA +15 SpD +10 Spe | level 165, at night |
| 2 | Umbrapard | Dark | Limber, Unburden, Prankster | 79/103/70/93/70/111 (526) | +15 HP +15 Atk +20 Def +5 SpA +20 SpD +5 Spe | level 166, knowing Sucker Punch |
| 3 | Simisylvan | Grass | Gluttony, Overgrow | 85/113/78/113/78/111 (578) | +10 HP +15 Atk +15 Def +15 SpA +15 SpD +10 Spe | a Leaf Stone used on it at level 170 or above |
| 4 | Simiscorch | Fire | Gluttony, Blaze | 85/113/78/113/78/111 (578) | +10 HP +15 Atk +15 Def +15 SpA +15 SpD +10 Spe | a Fire Stone used on it at level 170 or above |
| 5 | Simitorrent | Water | Gluttony, Torrent | 85/113/78/113/78/111 (578) | +10 HP +15 Atk +15 Def +15 SpA +15 SpD +10 Spe | a Water Stone used on it at level 170 or above |
| 6 | Somnarna | Psychic/Fairy | Forewarn, Synchronize, Telepathy | 116/60/105/132/110/44 (567) | +5 Atk +20 Def +25 SpA +15 SpD +15 Spe | a Moon Stone used on it at level 170 or above |
| 7 | Stormstrika | Electric | Lightning Rod, Motor Drive, Sap Sipper | 85/120/83/85/83/121 (577) | +10 HP +20 Atk +20 Def +5 SpA +20 SpD +5 Spe | level 170, knowing Wild Charge |
| 8 | Amorabat | Psychic/Flying | Unaware, Klutz, Simple | 82/57/75/102/70/119 (505) | +15 HP +20 Def +25 SpA +15 SpD +5 Spe | level 166, with friendship 220 or more, at night |
| 9 | Excavarch | Ground/Steel | Sand Rush, Sand Force, Mold Breaker | 110/145/90/50/95/98 (588) | +10 Atk +30 Def +30 SpD +10 Spe | level 176, holding a Soft Sand (used up) |
| 10 | Remedino | Normal/Fairy | Healer, Regenerator, Klutz | 108/60/106/90/106/55 (525) | +5 HP +20 Def +30 SpA +20 SpD +5 Spe | level 168, with friendship 220 or more, by day |
| 11 | Throjutsu | Fighting | Guts, Inner Focus, Mold Breaker | 120/120/105/30/105/65 (545) | +20 Atk +20 Def +20 SpD +20 Spe | level 172, with a Sawk in the party |
| 12 | Sawkendo | Fighting | Sturdy, Inner Focus, Mold Breaker | 90/140/95/30/95/95 (545) | +15 HP +15 Atk +20 Def +20 SpD +10 Spe | level 172, with a Throh in the party |
| 13 | Cottonimbus | Grass/Fairy | Prankster, Infiltrator, Chlorophyll | 80/67/100/102/95/116 (560) | +20 HP +15 Def +25 SpA +20 SpD | a Sun Stone used on it at level 172 or above |
| 14 | Ballerilly | Grass/Fairy | Chlorophyll, Own Tempo, Leaf Guard | 85/60/95/130/95/95 (560) | +15 HP +20 Def +20 SpA +20 SpD +5 Spe | a Sun Stone used on it at level 172 or above |
| 15 | Bascurage | Water/Dark | Reckless, Adaptability, Mold Breaker | 85/112/80/85/75/103 (540) | +15 HP +20 Atk +15 Def +5 SpA +20 SpD +5 Spe | level 172, knowing Head Smash |
| 16 | Darumandala | Fire/Psychic | Sheer Force, Inner Focus | 105/150/85/30/85/105 (560) | +10 Atk +30 Def +30 SpD +10 Spe | level 174, by day |
| 17 | Mariachactus | Grass/Ground | Water Absorb, Chlorophyll, Storm Drain | 90/96/87/121/87/60 (541) | +15 HP +10 Atk +20 Def +15 SpA +20 SpD | level 172, knowing Petal Dance |
| 18 | Crustadel | Bug/Rock | Sturdy, Shell Armor, Weak Armor | 90/120/125/70/100/60 (565) | +20 HP +15 Atk +5 SpA +25 SpD +15 Spe | level 174, holding a Hard Stone (used up) |
| 19 | Brawlhood | Dark/Fighting | Shed Skin, Moxie, Intimidate | 85/115/120/45/120/83 (568) | +20 HP +25 Atk +5 Def +5 SpD +25 Spe | level 174, knowing High Jump Kick |
| 20 | Glyphoracle | Psychic/Flying | Wonder Skin, Magic Guard, Tinted Lens | 87/58/95/128/95/107 (570) | +15 HP +15 Def +25 SpA +15 SpD +10 Spe | level 174, knowing Cosmic Power |
| 21 | Pharaurum | Ghost/Steel | Mummy | 88/50/145/120/120/40 (563) | +30 HP +25 SpA +15 SpD +10 Spe | level 175, holding a Reaper Cloth (used up) |
| 22 | Tortugarch | Water/Rock | Solid Rock, Sturdy, Swift Swim | 89/123/133/93/95/42 (575) | +15 HP +15 Atk +10 SpA +30 SpD +10 Spe | level 178, holding a Rare Bone (used up) |
| 23 | Primavian | Rock/Flying | Defeatist | 85/140/75/112/78/110 (600) | +10 HP +10 Def +13 SpD | level 178, holding a Rare Bone (used up) |
| 24 | Scrapodor | Poison/Steel | Stench, Weak Armor, Aftermath | 95/115/102/60/102/80 (554) | +15 HP +20 Atk +20 Def +20 SpD +5 Spe | level 172, knowing Gunk Shot |
| 25 | Zoromirage | Dark | Illusion | 80/110/85/125/85/105 (590) | +20 HP +5 Atk +25 Def +5 SpA +25 SpD | level 178, at night |
| 26 | Chinchillux | Normal/Fairy | Cute Charm, Technician, Skill Link | 90/110/80/70/80/120 (550) | +15 HP +15 Atk +20 Def +5 SpA +20 SpD +5 Spe | a Shiny Stone used on it at level 170 or above |
| 27 | Cygnelle | Water/Flying | Keen Eye, Big Pecks, Hydration | 90/97/83/97/83/103 (553) | +15 HP +10 Atk +20 Def +10 SpA +20 SpD +5 Spe | level 172, knowing Hurricane |
| 28 | Seasonstag | Normal/Grass | Chlorophyll, Sap Sipper, Serene Grace | 90/120/90/60/90/105 (555) | +10 HP +20 Atk +20 Def +20 SpD +10 Spe | level 174 |
| 29 | Nimbolga | Electric/Flying | Static, Motor Drive | 75/85/75/90/75/108 (508) | +20 HP +10 Atk +15 Def +15 SpA +15 SpD +5 Spe | level 167, holding a Cell Battery (used up) |
| 30 | Lancelier | Bug/Steel | Swarm, Shell Armor, Overcoat | 95/145/115/60/115/45 (575) | +25 HP +10 Atk +10 Def +10 SpD +25 Spe | level 176, with an Accelgor in the party |
| 31 | Sporemonarch | Grass/Poison | Effect Spore, Regenerator | 114/95/95/100/100/40 (544) | +10 Atk +25 Def +15 SpA +20 SpD +10 Spe | level 174, in the rain |
| 32 | Jellicrown | Water/Ghost | Water Absorb, Cursed Body, Damp | 110/60/95/110/115/70 (560) | +10 HP +25 Def +25 SpA +10 SpD +10 Spe | level 176, at night |
| 33 | Alomatron | Water/Fairy | Healer, Hydration, Regenerator | 165/85/105/55/75/65 (550) | +10 Atk +25 Def +15 SpA +30 SpD | level 175, knowing Healing Wish |
| 34 | Voltarantula | Bug/Electric | Compound Eyes, Unnerve, Swarm | 85/77/80/122/80/108 (552) | +15 HP +20 Def +25 SpA +20 SpD | level 175, knowing Sticky Web |
| 35 | Ferrospire | Grass/Steel | Iron Barbs, Anticipation | 99/114/131/69/126/30 (569) | +25 HP +20 Atk +15 SpA +10 SpD +10 Spe | level 176, holding a Rocky Helmet (used up) |
| 36 | Cosmogrey | Psychic/Electric | Telepathy, Synchronize, Analytic | 90/75/95/145/105/55 (565) | +15 HP +20 Def +20 SpA +10 SpD +15 Spe | level 178, at night |
| 37 | Beartundra | Ice/Fighting | Snow Cloak, Slush Rush, Swift Swim | 105/140/105/70/105/60 (585) | +10 HP +10 Atk +25 Def +25 SpD +10 Spe | level 178, holding a Never-Melt Ice (used up) |
| 38 | Cryohedron | Ice/Psychic | Levitate | 95/50/90/110/135/115 (595) | +15 HP +40 Def +15 SpA +10 Spe | level 178, at an Ice Rock |
| 39 | Velocigor | Bug | Hydration, Sticky Hold, Unburden | 90/70/70/115/85/145 (575) | +10 HP +30 Def +15 SpA +25 SpD | level 176, with an Escavalier in the party |
| 40 | Fluxflounder | Ground/Electric | Static, Limber, Sand Veil | 109/81/99/106/114/42 (551) | +15 Atk +15 Def +25 SpA +15 SpD +10 Spe | level 172, in the rain |
| 41 | Mienshifu | Fighting | Inner Focus, Regenerator, Reckless | 80/135/80/100/80/115 (590) | +15 HP +10 Atk +20 Def +5 SpA +20 SpD +10 Spe | level 180, knowing Aura Sphere |
| 42 | Drudrakon | Dragon/Rock | Rough Skin, Sheer Force, Mold Breaker | 92/135/105/65/105/63 (565) | +15 HP +15 Atk +15 Def +5 SpA +15 SpD +15 Spe | level 180, holding a Dragon Fang (used up) |
| 43 | Colossurk | Ground/Ghost | Iron Fist, Klutz, No Guard | 104/139/95/60/95/70 (563) | +15 HP +15 Atk +15 Def +5 SpA +15 SpD +15 Spe | level 178, knowing Dynamic Punch |
| 44 | Bouffalanx | Normal/Ground | Reckless, Sap Sipper, Soundproof | 105/135/105/45/105/75 (570) | +10 HP +25 Atk +10 Def +5 SpA +10 SpD +20 Spe | level 176, knowing Head Charge |
| 45 | Imperavia | Normal/Flying | Keen Eye, Sheer Force, Defiant | 100/133/95/57/100/105 (590) | +10 Atk +20 Def +25 SpD +25 Spe | level 182, by day |
| 46 | Carrionqueen | Dark/Flying | Big Pecks, Overcoat, Weak Armor | 110/75/115/80/105/105 (590) | +10 Atk +10 Def +25 SpA +10 SpD +25 Spe | level 182, at night |
| 47 | Pyrovore | Fire | Gluttony, Flash Fire, White Smoke | 95/112/86/120/86/65 (564) | +10 HP +15 Atk +20 Def +15 SpA +20 SpD | level 176, after knocking out 3 Durant |
| 48 | Formicarch | Bug/Steel | Swarm, Hustle, Truant | 88/119/112/48/78/119 (564) | +30 HP +10 Atk +30 SpD +10 Spe | level 176, after knocking out 3 Heatmor |
| 49 | Helioptera | Bug/Fire | Flame Body, Swarm | 95/60/90/135/105/115 (600) | +10 HP +25 Def +15 Spe | level 183, by day |
| 50 | Cobaltarch | Steel/Fighting | Justified | 106/110/129/105/97/113 (660) | +15 HP +20 Atk +15 SpA +25 SpD +5 Spe | level 190, with a Keldeo in the party |
| 51 | Terrakhan | Rock/Fighting | Justified | 106/139/110/77/115/113 (660) | +15 HP +10 Atk +20 Def +5 SpA +25 SpD +5 Spe | level 190, with a Keldeo in the party |
| 52 | Virizenith | Grass/Fighting | Justified | 106/105/97/110/129/113 (660) | +15 HP +15 Atk +25 Def +20 SpA +5 Spe | level 190, with a Keldeo in the party |

## The technical plan (for after the user's answers)

Batch 1's pipeline holds unchanged: each design an entry of `tools/DataImporter/Overrides/species-past-hundred.json` (a `LevelWithSpeciesInParty` row takes `species`, a `LevelAfterDefeating` row `species` and `value`, a `LevelAtLocation` row `location`, a `UseItem` row `item` and `level`), two written Pokédex entries and a habitat each in `Data/dex-entries.json` (`--check-entries` clean), a run of the importer, and `SpeciesPastHundredTests`' count of designs with a row of the fifth generation in its climb (question 1). The sculpts wait: the user is reworking the Pokémon sculpts, so ours stay generated by rule from their data until the user says otherwise, and no `PokemonModels.Ours.Unova.cs` is written. Nothing in the engine is needed for this batch: every method above runs today, `UseItem` with a level, `FriendshipNight`, `FriendshipDay`, `LevelWithSpeciesInParty`, `LevelAfterDefeating`, `LevelInRain` and `LevelAtLocation` at the Ice Rock among them. The Swords of Justice's evolutions join the Frontier's ban list (`FrontierRules.IsBannedOfOurs`). What is not this batch's: 06 · R31's moves past level 100 (the learnsets take them as batch 1's do), the lines of three, the regional forms' own lines (Hisui's and Galar's batches), and where in the game the evolutions are met (they need no place but the Ice Rock, only a level the climb reaches).

## How the numbers were checked

The same kind of Node.js script as the earlier batches', kept in the session's scratch folder and reading only `species.json`, `abilities.json` and `items.json`, with the 170 names of batches 1 to 4 read from their four documents' designs. It built the lines (a regional form's evolution counted as the form's own) and the list of the 62 that qualify, found no line of an older region that ends in Unova, and for each of the 52 designs checked: that it qualifies and is Unova's, that every qualifying species has a design or a reason for none, and every species given a reason qualifies; that a legendary is designed only in the set; that the rise is +80 or what the ceiling leaves (600, or 660 for the Swords of Justice), that no stat falls and that the six stats add up to the total written (the stats are written as the species' own plus the rise, so the two can't differ); that the name is one word of at most 12 letters, taken by no species or form of the data (the 131 of ours already built among them), by none of the earlier batches' 170 and by no other design of this batch; that the number is 2000 + the old species'; that the types are one or two of the eighteen; that the abilities exist and fill exactly the old species' slots; that the method is one the data already uses; that the level is between 165 and 183 for a species that isn't legendary and 190 for the Swords of Justice; that an item held or used exists and is no key item; that a move to know is in the species' level-up learnset at level 100 or below; that a place is one of the data's evolution places; that a species named in the party or to be knocked out exists; and that each is taller and heavier than what it comes from. The tables and the entries were written by the same script from the same values, so a number can't differ between the selection table, an entry and the table to answer. The documents of the batches written beside this one in wave 53 (Kalos, Alola, Galar and Paldea) were read as their sessions' drafts stood, and none of the 52 names appears in any of them; a name one of them takes after that is for the merge to settle.
