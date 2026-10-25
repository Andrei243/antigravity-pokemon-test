# D14 · Evolutions past level 100, batch 9 (Paldea): the proposal

Plan 03 · D14, the design part of its ninth and last batch, written in wave 53 after the user approved batch 1 ([D14-proposals-kanto.md](D14-proposals-kanto.md)), batch 2 ([D14-proposals-johto.md](D14-proposals-johto.md)), batch 3 ([D14-proposals-hoenn.md](D14-proposals-hoenn.md)) and batch 4 ([D14-proposals-sinnoh.md](D14-proposals-sinnoh.md)) with every default, on 2026-10-25. Batches 5 to 8 (Unova, Kalos, Alola and Galar) are written in the same wave by other sessions, side by side with this one; their drafts were read at the end of this session (in their own worktrees, unmerged): no name of this batch is in any of them, and their first questions default the level band as this one does (question 1). The main session holds the nine documents against one another once more at the merge. Nothing here is built yet: no species, no evolution row, no learnset, no sculpt, no test. A later session writes each row into the data (as entries of `tools/DataImporter/Overrides/species-past-hundred.json`, batch 1's table). The format is batch 4's, with one line more to each design: a Pokédex line, as the brief for this batch asked.

**What batches 1 to 4 settled, applied here without asking again**: the rise is **+80, capped so that no species of ours that isn't legendary passes 600**, and a region's legendary set rises to **660** (batch 1's birds, batch 2's beasts, batch 3's Regis and eon pair, batch 4's lake guardians); the number is **2000 + the number of the species it evolves from**; abilities come **only from `abilities.json`**, as many slots as the species had, in its order, the hidden one last; the evolution is **by level with a condition the engine already runs**, a held item used up; our species are listed in the National Pokédex after the 1,025 and **count toward none of its totals or diplomas**; names are invented, one word of at most 12 letters, and held against every species and form of the data and the earlier batches' names; **an evolution that starts from a regional form belongs to that form's own line** (`Evolution.FitsForm`); a female look, a Mega or a Gigantamax form is kept where the species has one, and **ours have no forms of their own**; the Frontier lets them be entered (a legendary's evolution joins its ban list), Pal Park, migration, the way out, the Pokéathlon's rivals and the golden corpus are held to 1 to 1,025; the cover legendaries and the mythicals don't evolve; a region's sets are built as sets or not at all; a single legendary with no set doesn't evolve (batch 1's Mewtwo, batch 4's Heatran); **each species evolves within its own region's climb**, with batch 4's rule for a region whose climb never passes 100.

**Paldea's climb, and so its levels** (question 1). Paldea is not built: plan 23 · Z13 gives it its region file, and nothing has written its `levels` yet. The level curve study ([W4-level-curve-study.md](W4-level-curve-study.md), section 4, the fair climb under the modern EXP, which plan 18 · W4 adopted and which Kanto's, Johto's, Hoenn's and Unova's region files already follow) sets Galar's Champion at about **242** and Paldea's at about **258**, so Paldea would be met from about 242 on its first route (Scarlet and Violet's 2) to 258 at Geeta (their 62), two levels under the cap of 260. That whole climb lies past the ceiling of 160 that batch 3's first answer wrote beside the climb ("within the region's own climb, at most 160"), so the two halves of the settled rule can't both hold here, as batch 4's Sinnoh couldn't meet the climb from the other side. By default **the climb wins**, since it is the half batch 3 gave its reason for (a species caught in its own region must not evolve at its first level there): this batch's species evolve **between 244 and 256**, the weakest of the first areas at 244 and Gholdengo, the strongest of the region's own, at 256, and its one legendary set at **258**, Paldea's Champion's level, in the region's post-game. The rule becomes "within the region's own climb, at most 160 for a region whose climb reaches 160, and within the climb wherever it starts past it" (question 1 has the alternatives, and says what it means for batches 5 to 8).

**The numbers.** By the settled rule this batch's numbers are 2916 to 3013; Gholdengo's is **3000**, the Treasures of Ruin's **3001 to 3004** and Sinistcha's **3013**, the first species of ours past 3000. Nothing in the engine bounds them (`PokemonDatabase.OursFrom` is a floor and nothing more), and the 72 moves of our own that 06 · R31 numbered 3001 to 3072 are rows of `moves.json`, a table of their own, so the numbers meet nothing (question 8).

Every name, type, ability, item, move, species, stat and sum in this document was checked by a script that only reads `PokemonPlatinumEngine/Data/species.json`, `abilities.json` and `items.json` (and `moves.json`'s `support`, to know a move to know can be learned), with the earlier batches' names and numbers read from their documents (kept outside the repository; what it checks is listed under "How the numbers were checked", near the end).

## The shortlist: the twelve strongest

If the user wants a small first build of this batch, these are the twelve. Each lifts one of Paldea's weakest last stages or is among the region's best-loved designs.

| | Ours | From | Typing | Total | Evolves | Why it leads |
|---|---|---|---|---|---|---|
| 1 | **Fortarach** | Spidops | Bug/Steel | 404 → 484 | level 244, knowing Sticky Web | the lowest total of Paldea's last stages; the trap spider grown into a gatehouse |
| 2 | **Bossquawk** | Squawkabilly | Normal/Flying | 417 → 497 | level 244, knowing Taunt | the lowest of a species alone in Paldea; the cities' parrot gangs need a boss |
| 3 | **Wugsextet** | Wugtrio | Water/Ground | 425 → 505 | level 244, knowing Triple Dive | 425 and an HP of 35; three garden eels become a colony of six |
| 4 | **Clodkaiser** | Clodsire | Poison/Ground | 430 → 510 | level 244, holding a Poison Barb (used up) | 430, the Paldean Wooper's own end, the bog's emperor |
| 5 | **Locustorm** | Lokix | Bug/Dark | 450 → 530 | level 246, at night | 450, and the grasshopper whose kicks hint at a swarm; it leads one |
| 6 | **Klawlossus** | Klawf | Rock/Steel | 450 → 530 | level 246, knowing Guillotine | 450, the first Titan; the crab that hangs from cliffs holds them up |
| 7 | **Squeakshire** | Maushold | Normal/Fairy | 470 → 550 | level 246, with friendship 220 or more, by day | the family of mice grown into a village, by friendship by day |
| 8 | **Khepristar** | Rabsca | Bug/Psychic | 470 → 550 | level 246, by day | 470, the beetle that rolls its young in a ball; the ball becomes a small sun |
| 9 | **Tatsushogun** | Tatsugiri | Dragon/Water | 475 → 555 | level 250, with a Dondozo in the party | the little dragon that commands a catfish, grown into an admiral beside one |
| 10 | **Sashimaru** | Veluza | Water/Psychic | 478 → 558 | level 246, knowing Fillet Away | 478, the fish that sheds its own flesh; grown, its bones are blades |
| 11 | **Habanerogue** | Scovillain | Grass/Fire | 486 → 566 | a Fire Stone used on it at level 248 or above | the two-headed pepper, ripened by its Fire Stone again |
| 12 | **Duskenblade** | Ceruledge | Fire/Ghost | 525 → 600 | level 252, holding a Malicious Armor (used up) | among Paldea's best loved; a second cursed armour takes the swordsman on |

## What qualifies, and how many

The earlier batches' rule and the same script: a species **with no evolution at all**, or **the last stage of a line of two**; an evolution that starts from a regional form counts as that form's own; a line belongs to the batch of the generation its last stage is from.

**Paldea, National Pokédex 906 to 1025: 69 species qualify.**

- **41 alone**: Squawkabilly, Klawf, Bombirdier, Cyclizar, Orthworm, Flamigo, Veluza, Dondozo, Tatsugiri; the 20 paradox Pokémon (Great Tusk, Scream Tail, Brute Bonnet, Flutter Mane, Slither Wing, Sandy Shocks, Iron Treads, Iron Bundle, Iron Hands, Iron Jugulis, Iron Moth, Iron Thorns, Roaring Moon, Iron Valiant, Walking Wake, Iron Leaves, Gouging Fire, Raging Bolt, Iron Boulder, Iron Crown), which the data doesn't flag as legendary; and 12 legendary or mythical (Wo-Chien, Chien-Pao, Ting-Lu, Chi-Yu, Koraidon, Miraidon, Okidogi, Munkidori, Fezandipiti, Ogerpon, Terapagos, Pecharunt).
- **28 last of a line of two**: Oinkologne, Spidops, Lokix, Maushold, Dachsbun, Armarouge, Ceruledge, Bellibolt, Kilowattrel, Mabosstiff, Grafaiai, Brambleghast, Toedscruel, Scovillain, Rabsca, Espathra, Wugtrio, Palafin, Revavroom, Glimmora, Houndstone, Cetitan, Clodsire, Farigiraf, Dudunsparce, Gholdengo, Sinistcha, Archaludon. Charcadet has two ends (Armarouge and Ceruledge), each judged alone. Clodsire is the Paldean Wooper's own end (only the Paldean Wooper becomes Clodsire; Quagsire was batch 2's), as batch 1 read the Galarian Farfetch'd's Sirfetch'd.
- **Deferred to this batch by the earlier ones**: batch 2 named Girafarig (Farigiraf) and Dunsparce (Dudunsparce) as Johto species whose lines end in a later region, and both qualify and are designed below. Duraludon's line (Galar) ends in Paldea's Archaludon, which qualifies and is refused only for its 600. No other earlier line ends here as a line of two.
- **Not here, last of a line of three (11)**: Meowscarada, Skeledirge, Quaquaval, Pawmot, Arboliva, Garganacl, Tinkaton, Annihilape, Kingambit, Baxcalibur, Hydrapple. They take 06 · R31's moves. Three of them end older lines in Paldea (Annihilape after Mankey and Primeape, Kingambit after Pawniard and Bisharp, Hydrapple after Applin and Dipplin); the other eight are Paldea's own lines of three.
- **Not here, the seven first met in Hisui** (Wyrdeer, Kleavor, Ursaluna, Basculegion, Sneasler, Overqwil and Enamorus): they are of Galar's generation in the data (899 to 905), so they are batch 8's, whatever Galar's document decides for them.

39 designs follow; 30 are left out with their reasons (Palafin, Archaludon, the twenty paradox Pokémon, Koraidon, Miraidon, the Loyal Three, Ogerpon, Terapagos and Pecharunt).

## How to read an entry

As batch 4's. **Stats** are the stats the game reads (the newest games' where `species.json` has a `modern` change; among the 69 none has one). **Levels**: 244 to 256 for the region's own (above), the weakest of the first areas at 244 to 246, the middle of the region at 248 to 250, the strongest at 252 and Gholdengo, the strongest of all, at 256; the Treasures of Ruin at 258. **Looks** start from our own sculpt of the species; Oinkologne is the one species of the batch whose female looks different, and keeps that difference as her look; Scovillain, Glimmora and Tatsugiri keep their Megas, which ours don't get. **Pokédex** is one line in our own words, for the two written entries of 08 · P13 that a later session writes. **Say**: strong yes (the shortlist), yes, maybe, no.

**The sets.** Paldea has few pairs, and each is built as a set or not at all: **Tatsugiri and Dondozo**, each with the other in the party (the commander and its catfish, as batch 3's Latias and Latios), and **the four Treasures of Ruin**, a ring, each with the next in the party (as batch 4's lake guardians). Charcadet's two ends, Armarouge and Ceruledge, are designed as a matching pair, each by a second suit of its own armour, but each stands alone (question 10). Every condition below is one the engine already runs; none needs a place, since Paldea has none in the data (`LevelAtLocation` knows only Sinnoh's three and Galar's Stone Arch).

## The legendaries, the mythical, the paradox Pokémon, Palafin and Archaludon

Paldea has eleven legendaries and one mythical in the data, and twenty paradox Pokémon that the data doesn't flag but that are legendary in every other way; the user asked each batch to say which of its region's evolve.

- **Wo-Chien, Chien-Pao, Ting-Lu and Chi-Yu: maybe, as a set of four or not at all**, each at level 258 with the next in the party: the tablets with the sword, the sword with the vessel, the vessel with the beads, the beads with the tablets, which closes the ring as the four were sealed together. +90 to 660, the region's legendary set, as every region's before it. *Default if the user doesn't say*: build them.
- **Koraidon and Miraidon: no, as a pair.** The cover legendaries, 670 each, and their builds and modes are their changes.
- **Okidogi, Munkidori and Fezandipiti: no** (question 3). The Loyal Three are a set, but their whole story is Pecharunt's chains on them; risen to 660 they would stand 60 over the master that binds them, where batch 3 kept the Regis under Regigigas.
- **Ogerpon: no.** A legendary alone, and its four masks are forms of four typings; an evolution would lose the mask it was made from (batch 4's Wormadam).
- **Terapagos: no.** The region's last legendary, and its Terastal and Stellar Forms are its changes.
- **Pecharunt: no.** The mythicals stand outside any stage.
- **The twenty paradox Pokémon: no** (question 4). Each is the far past or the far future of a living species (Great Tusk of Donphan, Iron Valiant of Gardevoir and Gallade, Walking Wake of Suicune), already 570 or 590, so the settled ceiling would leave them +30 or +10; and a further stage of a Pokémon out of its time is a third time, not a growth.
- **Palafin: no** (question 6). Finizen becomes Palafin, and Palafin becomes its Hero Form in battle (650): that is its change, and Zero to Hero on a species of ours, which has no Hero Form, would do nothing.
- **Archaludon: no.** Duraludon's end through Paldea, and already 600: the ceiling leaves it nothing (question 7).

## The table: every qualifying species

The user may strike rows here before reading the designs. "No." is our number; "Total" is the stats the game reads now, and ours.

| # | Was | Ours | No. | Typing (was → ours) | Total | Evolves | Say |
|---|---|---|---|---|---|---|---|
| 1 | Oinkologne (916) | Trufflord | 2916 | Normal → **Normal/Ground** | 489 → 569 | level 248, knowing Earth Power | yes: the hog that roots for truffles; past its own level it roots up whole groves |
| 2 | Spidops (918) | Fortarach | 2918 | Bug → **Bug/Steel** | 404 → 484 | level 244, knowing Sticky Web | strong yes: a total of 404, the lowest of Paldea's last stages; the spider that hides behind its legs becomes a gatehouse |
| 3 | Lokix (920) | Locustorm | 2920 | Bug/Dark, kept | 450 → 530 | level 246, at night | strong yes: a total of 450, and the grasshopper whose design already hints at a swarm; grown, it leads one |
| 4 | Maushold (925) | Squeakshire | 2925 | Normal → **Normal/Fairy** | 470 → 550 | level 246, with friendship 220 or more, by day | strong yes: 470, and the family of mice the whole region knows; the family grows into a village |
| 5 | Dachsbun (927) | Brioschund | 2927 | Fairy → **Fairy/Fire** | 477 → 557 | level 246, holding a Charcoal (used up) | yes: 477, and the dog that is a loaf of bread; held to the coals once more it bakes into a whole bakery |
| 6 | Squawkabilly (931) | Bossquawk | 2931 | Normal/Flying, kept | 417 → 497 | level 244, knowing Taunt | strong yes: a total of 417, the lowest of a species alone in Paldea; the parrot gangs of the cities need a boss |
| 7 | Armarouge (936) | Pyropaladin | 2936 | Fire/Psychic, kept | 525 → 600 | level 252, holding an Auspicious Armor (used up) | yes: Charcadet's end by the lucky armour; a second suit of it makes a paladin. Built beside Ceruledge's as a matching pair |
| 8 | Ceruledge (937) | Duskenblade | 2937 | Fire/Ghost, kept | 525 → 600 | level 252, holding a Malicious Armor (used up) | strong yes: among the best loved of Paldea; a second suit of cursed armour takes the swordsman further |
| 9 | Bellibolt (939) | Thunderbelly | 2939 | Electric → **Electric/Water** | 495 → 575 | level 248, in the rain | yes: the frog whose belly is a generator; in the rain it becomes a thundercloud on legs |
| 10 | Kilowattrel (941) | Terawattrel | 2941 | Electric/Flying, kept | 490 → 570 | level 248, knowing Hurricane | yes: the frigatebird that rides the storm; grown, it carries one |
| 11 | Mabosstiff (943) | Mastiffdon | 2943 | Dark, kept | 505 → 585 | level 250, with a Maschiff in the party | yes: the boss dog; with a Maschiff at its side it becomes the boss of a whole street |
| 12 | Grafaiai (945) | Graffitape | 2945 | Poison/Normal, kept | 485 → 565 | level 248, knowing Knock Off | yes: the monkey that paints with its poison; grown, its tags are murals |
| 13 | Brambleghast (947) | Thornwraith | 2947 | Grass/Ghost, kept | 480 → 560 | level 248, by day | yes: the tumbleweed that rolls through the deserts; grown, it is a whirlwind of thorns |
| 14 | Toedscruel (949) | Myceliarch | 2949 | Ground/Grass, kept | 515 → 595 | level 252, at night | yes: the mushroom that walks on its threads; at night the whole forest floor is its body |
| 15 | Klawf (950) | Klawlossus | 2950 | Rock → **Rock/Steel** | 450 → 530 | level 246, knowing Guillotine | strong yes: 450, and the first Titan of the path of legends; it hangs from cliffs, and grown, it holds the cliff up |
| 16 | Scovillain (952) | Habanerogue | 2952 | Grass/Fire, kept | 486 → 566 | a Fire Stone used on it at level 248 or above | strong yes: the two-headed pepper the whole region loves; the stone that ripened it ripens it again |
| 17 | Rabsca (954) | Khepristar | 2954 | Bug/Psychic, kept | 470 → 550 | level 246, by day | strong yes: 470, the beetle that rolls a ball holding its young; grown, the ball is a small sun |
| 18 | Espathra (956) | Mirastrich | 2956 | Psychic → **Psychic/Fairy** | 481 → 561 | level 248, knowing Lumina Crash | yes: the ostrich with eyes on its feathers; grown, its feathers show mirages |
| 19 | Wugtrio (961) | Wugsextet | 2961 | Water → **Water/Ground** | 425 → 505 | level 244, knowing Triple Dive | strong yes: a total of 425 and an HP of 35; the three eels in the sand become a colony of six |
| 20 | Bombirdier (962) | Bomberon | 2962 | Flying/Dark, kept | 485 → 565 | level 248, holding a Hard Stone (used up) | yes: the stork that drops rocks, and a Titan; grown, it carries a whole boulder |
| – | Palafin (964) | none | – | Water | 457 | – | no: its Hero Form is its change (650), and Zero to Hero does nothing on a species with no Hero Form of its own; ours have no forms (question 6) |
| 21 | Revavroom (966) | Turbovroom | 2966 | Steel/Poison, kept | 500 → 580 | level 250, knowing Spin Out | yes: the engine Pokémon of Team Star's bases; grown, it is a racing machine |
| 22 | Cyclizar (967) | Velocidrake | 2967 | Dragon/Normal, kept | 501 → 581 | level 250, with friendship 220 or more, by day | yes: the mount every rider in Paldea knows; with its rider's friendship it becomes a war steed |
| 23 | Orthworm (968) | Orthobore | 2968 | Steel → **Steel/Ground** | 480 → 560 | level 248, knowing Earthquake | yes: the Titan of the desert that eats iron; grown, it bores tunnels through mountains |
| 24 | Glimmora (970) | Geodiadem | 2970 | Rock/Poison, kept | 525 → 600 | level 252, holding a Star Piece (used up) | yes: the crystal flower of the caves; holding a Star Piece it blooms a diadem |
| 25 | Houndstone (972) | Barrowhound | 2972 | Ghost, kept | 488 → 568 | level 248, at night | yes: the ghost dog that sleeps in a grave; grown, it guards a whole barrow |
| 26 | Flamigo (973) | Flamenkick | 2973 | Flying/Fighting, kept | 500 → 580 | level 250, knowing Brave Bird | yes: the flamingo whose flock moves as one; grown, it dances |
| 27 | Cetitan (975) | Cetiberg | 2975 | Ice, kept | 521 → 600 | an Ice Stone used on it at level 252 or above | yes: the whale that walks on land; the stone that made it makes it an iceberg |
| 28 | Veluza (976) | Sashimaru | 2976 | Water/Psychic, kept | 478 → 558 | level 246, knowing Fillet Away | strong yes: 478, the fish that sheds its flesh to swim faster; grown, its bones are blades |
| 29 | Dondozo (977) | Grandozo | 2977 | Water, kept | 530 → 600 | level 250, with a Tatsugiri in the party | yes: with Tatsugiri's, as a set: the catfish that a Tatsugiri commands, grown only with one beside it |
| 30 | Tatsugiri (978) | Tatsushogun | 2978 | Dragon/Water, kept | 475 → 555 | level 250, with a Dondozo in the party | strong yes: with Dondozo's, as a set: the little dragon that commands a giant catfish from inside its mouth; grown, it is an admiral |
| 31 | Clodsire (980) | Clodkaiser | 2980 | Poison/Ground, kept | 430 → 510 | level 244, holding a Poison Barb (used up) | strong yes: 430, the Paldean Wooper's own end; grown, the bog's emperor |
| 32 | Farigiraf (981) | Farigigiraf | 2981 | Normal/Psychic, kept | 520 → 600 | level 252, knowing Twin Beam | maybe: Girafarig's line only now goes on in Paldea, and it is 520; a name that reads the same both ways, as both ends of it think |
| 33 | Dudunsparce (982) | Dunsprawl | 2982 | Normal, kept | 520 → 600 | level 252, knowing Hyper Drill | maybe: Dunsparce's line went on only in Paldea, and it is 520; grown, it sprawls through its own tunnels |
| – | Great Tusk (984) | none | – | Ground/Fighting | 570 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Scream Tail (985) | none | – | Fairy/Psychic | 570 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Brute Bonnet (986) | none | – | Grass/Dark | 570 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Flutter Mane (987) | none | – | Ghost/Fairy | 570 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Slither Wing (988) | none | – | Bug/Fighting | 570 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Sandy Shocks (989) | none | – | Electric/Ground | 570 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Iron Treads (990) | none | – | Ground/Steel | 570 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Iron Bundle (991) | none | – | Ice/Water | 570 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Iron Hands (992) | none | – | Fighting/Electric | 570 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Iron Jugulis (993) | none | – | Dark/Flying | 570 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Iron Moth (994) | none | – | Fire/Poison | 570 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Iron Thorns (995) | none | – | Rock/Electric | 570 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| 34 | Gholdengo (1000) | Bullionaire | 3000 | Steel/Ghost, kept | 550 → 600 | level 256, with 999 Gimmighoul Coins in the bag (used up) | maybe: the coin Pokémon is 550 and Good as Gold already; a second hoard of 999 coins makes it a treasury |
| 35 | Wo-Chien (1001) | Tabletruin | 3001 | Dark/Grass, kept | 570 → 660 | level 258, with a Chien-Pao in the party | maybe: the four Treasures of Ruin as a set or not at all, each by the next in the party: the tablets with the sword beside them |
| 36 | Chien-Pao (1002) | Swordruin | 3002 | Dark/Ice, kept | 570 → 660 | level 258, with a Ting-Lu in the party | maybe: with the Treasures of Ruin: the sword with the vessel beside it |
| 37 | Ting-Lu (1003) | Vesselruin | 3003 | Dark/Ground, kept | 570 → 660 | level 258, with a Chi-Yu in the party | maybe: with the Treasures of Ruin: the vessel with the beads beside it |
| 38 | Chi-Yu (1004) | Beadruin | 3004 | Dark/Fire, kept | 570 → 660 | level 258, with a Wo-Chien in the party | maybe: with the Treasures of Ruin: the beads with the tablets beside them, which closes the ring |
| – | Roaring Moon (1005) | none | – | Dragon/Dark | 590 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Iron Valiant (1006) | none | – | Fairy/Fighting | 590 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Koraidon (1007) | none | – | Fighting/Dragon | 670 | – | no: 670, the cover legendary of Scarlet, and its builds are its changes |
| – | Miraidon (1008) | none | – | Electric/Dragon | 670 | – | no: 670, the cover legendary of Violet, and its modes are its changes |
| – | Walking Wake (1009) | none | – | Water/Dragon | 590 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Iron Leaves (1010) | none | – | Grass/Psychic | 590 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| 39 | Sinistcha (1013) | Chagamage | 3013 | Grass/Ghost, kept | 508 → 588 | a Masterpiece Teacup used on it at level 250 or above | yes: the matcha that lives in a bowl; poured into a masterpiece it becomes the whole ceremony |
| – | Okidogi (1014) | none | – | Poison/Fighting | 555 | – | no: one of the Loyal Three, whose story is Pecharunt's hold on them; raised past 600 they would pass the master that binds them (question 3) |
| – | Munkidori (1015) | none | – | Poison/Psychic | 555 | – | no: with Okidogi, as the Loyal Three |
| – | Fezandipiti (1016) | none | – | Poison/Fairy | 555 | – | no: with Okidogi, as the Loyal Three |
| – | Ogerpon (1017) | none | – | Grass | 550 | – | no: a legendary alone, and its four masks are forms of four typings; an evolution would lose the mask it was made from, as Wormadam's cloak |
| – | Archaludon (1018) | none | – | Steel/Dragon | 600 | – | no: already 600, the ceiling for a species that isn't legendary; there is nothing to rise by |
| – | Gouging Fire (1020) | none | – | Fire/Dragon | 590 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Raging Bolt (1021) | none | – | Electric/Dragon | 590 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Iron Boulder (1022) | none | – | Rock/Psychic | 590 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Iron Crown (1023) | none | – | Steel/Psychic | 590 | – | no: a paradox Pokémon, the far past or far future of a living species, at 570 or 590 already (question 4) |
| – | Terapagos (1024) | none | – | Normal | 450 | – | no: the region's last legendary, and its Terastal and Stellar Forms are its changes |
| – | Pecharunt (1025) | none | – | Poison/Ghost | 600 | – | no: a mythical, outside any stage, as Mew, Celebi and Manaphy |

In all: 12 strong yes, 20 yes, 7 maybe (the Treasures of Ruin as a set among them) and 30 no.

## The designs

In National Pokédex order. Each entry: the typing and why, the category word, number, size, the stats (the species' as the game reads them → ours) with where the rise goes, the abilities, the evolution with the row it would be in `species.json`, the look and a Pokédex line.

### 1. Trufflord, from Oinkologne (916): yes

- **Typing**: Normal → **Normal/Ground**. *Alternative*: Normal alone.
- **The Truffle Lord Pokémon**, No. 2916. 1.4 m, 170.0 kg (Oinkologne 1 m, 120 kg).
- **Stats**: 110 / 100 / 75 / 59 / 80 / 65 = 489 → **120 / 120 / 95 / 59 / 100 / 75 = 569** (+10 HP +20 Atk +20 Def +20 SpD +10 Spe): most to its Attack, Defense and Sp. Def.
- **Abilities**: Lingering Aroma, Gluttony, hidden Thick Fat (was Lingering Aroma, Gluttony, hidden Thick Fat).
- **Evolution**: Oinkologne at level 248, knowing Earth Power. Data: `{"targetSpecies":"Trufflord","method":"LevelKnowsMove","level":248,"move":"Earth Power"}`.
- **Look**: from our grey Oinkologne with its curled crest: heavier, its snout broadened into a rooting spade, the crest grown into a wig of tight grey curls over its shoulders, two short tusks of pale ochre and a band of dark bristle round its middle. The female's own look, paler with her long lashes and a rosier snout (Oinkologne's female form), is kept as her look. **New feature**: the rooting spade of a snout.
- **Pokédex**: It snuffles out truffles a metre underground, and the earthy scent it leaves on the soil makes every hog for a valley follow its tracks.

### 2. Fortarach, from Spidops (918): strong yes

- **Typing**: Bug → **Bug/Steel**. *Alternative*: Bug/Fighting.
- **The Fortress Pokémon**, No. 2918. 1.4 m, 40.0 kg (Spidops 1 m, 16.5 kg).
- **Stats**: 60 / 79 / 92 / 52 / 86 / 35 = 404 → **85 / 94 / 112 / 52 / 101 / 40 = 484** (+25 HP +15 Atk +20 Def +15 SpD +5 Spe): most to its HP.
- **Abilities**: Insomnia, hidden Stakeout (was Insomnia, hidden Stakeout).
- **Evolution**: Spidops at level 244, knowing Sticky Web. Data: `{"targetSpecies":"Fortarach","method":"LevelKnowsMove","level":244,"move":"Sticky Web"}`.
- **Look**: from our green Spidops with the legs it folds over its face: larger, the folded legs grown into a portcullis of grey steel bars over its face, the tips of its legs hooked like grappling irons, and strands of silver silk strung between the spines on its back. **New feature**: the portcullis over its face.
- **Pokédex**: It shuts its barred face in a doorway and waits for days. Whatever pushes at the bars sticks to the silk threaded through them.

### 3. Locustorm, from Lokix (920): strong yes

- **Typing**: Bug/Dark, kept. *Alternative*: Bug/Flying.
- **The Locust Storm Pokémon**, No. 2920. 1.3 m, 26.0 kg (Lokix 1 m, 17.5 kg).
- **Stats**: 71 / 102 / 78 / 52 / 55 / 92 = 450 → **86 / 122 / 88 / 52 / 80 / 102 = 530** (+15 HP +20 Atk +10 Def +25 SpD +10 Spe): most to its Sp. Def.
- **Abilities**: Swarm, hidden Tinted Lens (was Swarm, hidden Tinted Lens).
- **Evolution**: Lokix at level 246, at night. Data: `{"targetSpecies":"Locustorm","method":"LevelNight","level":246}`.
- **Look**: from our grey Lokix with its black visor and blade-like hind legs: taller, its wings grown long enough to fly far, the visor sharper with a red band across it like a mask, its hind legs banded in red and black and a ragged cloak of wing-cases at its back. **New feature**: the long wings for flying far.
- **Pokédex**: One leaping at night is followed by thousands. Farmers in Paldea read the dusk sky in summer to see whether it has risen.

### 4. Squeakshire, from Maushold (925): strong yes

- **Typing**: Normal → **Normal/Fairy**. *Alternative*: Normal alone.
- **The Hamlet Pokémon**, No. 2925. 0.5 m, 5.0 kg (Maushold 0.3 m, 2.8 kg).
- **Stats**: 74 / 75 / 70 / 65 / 75 / 111 = 470 → **94 / 90 / 90 / 70 / 90 / 116 = 550** (+20 HP +15 Atk +20 Def +5 SpA +15 SpD +5 Spe): most to its HP and Defense.
- **Abilities**: Friend Guard, Cheek Pouch, hidden Technician (was Friend Guard, Cheek Pouch, hidden Technician).
- **Evolution**: Maushold at level 246, with friendship 220 or more, by day. Data: `{"targetSpecies":"Squeakshire","method":"FriendshipDay","level":246}`.
- **Look**: from our family of four white mice: six of them now, the two parents larger with round ears and a white scarf of fur each, two little ones in the middle and the smallest riding on a parent's back, all six with the same pink cheeks. **New feature**: the two more mice.
- **Pokédex**: A family that has stayed together long enough calls its relatives in. They share every berry six ways and never quarrel over the last one.

### 5. Brioschund, from Dachsbun (927): yes

- **Typing**: Fairy → **Fairy/Fire**. *Alternative*: Fairy alone.
- **The Bakery Pokémon**, No. 2927. 0.8 m, 25.0 kg (Dachsbun 0.5 m, 14.9 kg).
- **Stats**: 57 / 80 / 115 / 50 / 80 / 95 = 477 → **87 / 95 / 120 / 60 / 95 / 100 = 557** (+30 HP +15 Atk +5 Def +10 SpA +15 SpD +5 Spe): most to its HP.
- **Abilities**: Well-Baked Body, hidden Aroma Veil (was Well-Baked Body, hidden Aroma Veil).
- **Evolution**: Dachsbun at level 246, holding a Charcoal (used up). Data: `{"targetSpecies":"Brioschund","method":"LevelHoldingItem","level":246,"item":"Charcoal"}`.
- **Look**: from our brown Dachsbun: longer, its body a braided brioche glazed a deep gold, the crust scored along its back, a scatter of pale seeds on its ears and a thin curl of steam rising from its back (decals). **New feature**: the braided crust.
- **Pokédex**: Its body is warm as a fresh loaf all day. A child who falls asleep against it wakes up smelling of baking, and its trainer never buys bread.

### 6. Bossquawk, from Squawkabilly (931): strong yes

- **Typing**: Normal/Flying, kept. *Alternative*: Dark/Flying.
- **The Gang Boss Pokémon**, No. 2931. 0.9 m, 5.0 kg (Squawkabilly 0.6 m, 2.4 kg).
- **Stats**: 82 / 96 / 51 / 45 / 51 / 92 = 417 → **92 / 116 / 76 / 45 / 71 / 97 = 497** (+10 HP +20 Atk +25 Def +20 SpD +5 Spe): most to its Defense.
- **Abilities**: Intimidate, Hustle, hidden Guts (was Intimidate, Hustle, hidden Guts).
- **Evolution**: Squawkabilly at level 244, knowing Taunt. Data: `{"targetSpecies":"Bossquawk","method":"LevelKnowsMove","level":244,"move":"Taunt"}`.
- **Look**: from our green parrot with its crest: larger, the crest grown into a tall swept pompadour, a ruff of bright feathers at its neck like a collar, a notch in one wing and its beak heavier and hooked. **New feature**: the pompadour of a crest.
- **Pokédex**: It perches above the plaza and gives orders with one squawk. The flock steals for it, and it never lifts a feather to carry the loot.

### 7. Pyropaladin, from Armarouge (936): yes

- **Typing**: Fire/Psychic, kept. *Alternative*: Fire/Steel.
- **The Fire Paladin Pokémon**, No. 2936. 1.8 m, 120.0 kg (Armarouge 1.5 m, 85 kg).
- **Stats**: 85 / 60 / 100 / 125 / 80 / 75 = 525 → **100 / 60 / 100 / 140 / 110 / 90 = 600** (+15 HP +15 SpA +30 SpD +15 Spe): the rise is +75, to the 600 ceiling: most to its Sp. Def.
- **Abilities**: Flash Fire, hidden Weak Armor (was Flash Fire, hidden Weak Armor).
- **Evolution**: Armarouge at level 252, holding an Auspicious Armor (used up). Data: `{"targetSpecies":"Pyropaladin","method":"LevelHoldingItem","level":252,"item":"Auspicious Armor"}`.
- **Look**: from our red and gold Armarouge with its arm cannons: taller, its helm grown into a crested great helm, a shield of gold plates on one forearm, a mantle of flame at its shoulders (Glow, orange) and the cannons longer. **New feature**: the shield on its forearm.
- **Pokédex**: It is said to wear the armour of a knight who guarded travellers. It still walks the old roads at dusk and lights the way with its cannons.

### 8. Duskenblade, from Ceruledge (937): strong yes

- **Typing**: Fire/Ghost, kept. *Alternative*: Fire/Steel.
- **The Grave Blade Pokémon**, No. 2937. 1.9 m, 85.0 kg (Ceruledge 1.6 m, 62 kg).
- **Stats**: 75 / 125 / 80 / 60 / 100 / 85 = 525 → **95 / 140 / 100 / 60 / 105 / 100 = 600** (+20 HP +15 Atk +20 Def +5 SpD +15 Spe): the rise is +75, to the 600 ceiling: most to its HP and Defense.
- **Abilities**: Flash Fire, hidden Weak Armor (was Flash Fire, hidden Weak Armor).
- **Evolution**: Ceruledge at level 252, holding a Malicious Armor (used up). Data: `{"targetSpecies":"Duskenblade","method":"LevelHoldingItem","level":252,"item":"Malicious Armor"}`.
- **Look**: from our purple and blue Ceruledge with its flame blades: taller, a long tattered cloak of blue flame from its shoulders (Glow, blue), its two blades joined at the hilt into one long glaive and its helm crowned with a ring of dark spikes. **New feature**: the glaive of two blades.
- **Pokédex**: The grudges in its armour have burned for a thousand years. It hunts only those who break an oath, and its blue flame never warms anything.

### 9. Thunderbelly, from Bellibolt (939): yes

- **Typing**: Electric → **Electric/Water**. *Alternative*: Electric/Ground.
- **The Storm Frog Pokémon**, No. 2939. 1.5 m, 160.0 kg (Bellibolt 1.2 m, 113 kg).
- **Stats**: 109 / 64 / 91 / 103 / 83 / 45 = 495 → **119 / 74 / 106 / 128 / 98 / 50 = 575** (+10 HP +10 Atk +15 Def +25 SpA +15 SpD +5 Spe): most to its Sp. Atk.
- **Abilities**: Electromorphosis, Static, hidden Damp (was Electromorphosis, Static, hidden Damp).
- **Evolution**: Bellibolt at level 248, in the rain. Data: `{"targetSpecies":"Thunderbelly","method":"LevelInRain","level":248}`.
- **Look**: from our green Bellibolt with its round belly and bulb eyes: larger, the belly a drum with ridges like a coil, the eyes' bulbs bigger and lit (Glow, yellow) and a cloud of grey froth round its feet. **New feature**: the ridged drum of a belly.
- **Pokédex**: When it slaps its belly in a storm, the lightning answers. Fishermen of the bay go home the moment they hear it drumming.

### 10. Terawattrel, from Kilowattrel (941): yes

- **Typing**: Electric/Flying, kept. *Alternative*: Electric/Water.
- **The Storm Glider Pokémon**, No. 2941. 1.8 m, 50.0 kg (Kilowattrel 1.4 m, 38.6 kg).
- **Stats**: 70 / 70 / 60 / 105 / 60 / 125 = 490 → **90 / 70 / 85 / 120 / 80 / 125 = 570** (+20 HP +25 Def +15 SpA +20 SpD): most to its Defense.
- **Abilities**: Wind Power, Volt Absorb, hidden Competitive (was Wind Power, Volt Absorb, hidden Competitive).
- **Evolution**: Kilowattrel at level 248, knowing Hurricane. Data: `{"targetSpecies":"Terawattrel","method":"LevelKnowsMove","level":248,"move":"Hurricane"}`.
- **Look**: from our yellow and black Kilowattrel: wider, its wings grown long and forked, their edges crackling (Glow, yellow), its throat pouch larger and its tail a pair of long streamers. **New feature**: the forked streamers of a tail.
- **Pokédex**: It glides on storm winds for weeks without landing. The charge in its throat pouch is enough to light a lighthouse.

### 11. Mastiffdon, from Mabosstiff (943): yes

- **Typing**: Dark, kept. *Alternative*: Dark/Fighting.
- **The Don Pokémon**, No. 2943. 1.4 m, 90.0 kg (Mabosstiff 1.1 m, 61 kg).
- **Stats**: 80 / 120 / 90 / 60 / 70 / 85 = 505 → **95 / 135 / 100 / 60 / 100 / 95 = 585** (+15 HP +15 Atk +10 Def +30 SpD +10 Spe): most to its Sp. Def.
- **Abilities**: Intimidate, Guard Dog, hidden Stakeout (was Intimidate, Guard Dog, hidden Stakeout).
- **Evolution**: Mabosstiff at level 250, with a Maschiff in the party. Data: `{"targetSpecies":"Mastiffdon","method":"LevelWithSpeciesInParty","level":250,"species":"Maschiff"}`.
- **Look**: from our grey Mabosstiff with its jowls and spiked collar: larger, its jowls heavier, the collar a heavy chain with a gold plate, a scar across its brow and its fur darker at the back like a coat. **New feature**: the chain with its gold plate.
- **Pokédex**: Every dog of the town keeps out of its way, and every pup is fed. It settles quarrels between packs with a single look.

### 12. Graffitape, from Grafaiai (945): yes

- **Typing**: Poison/Normal, kept. *Alternative*: Poison/Fairy.
- **The Toxic Artist Pokémon**, No. 2945. 1.0 m, 40.0 kg (Grafaiai 0.7 m, 27.2 kg).
- **Stats**: 63 / 95 / 65 / 80 / 72 / 110 = 485 → **83 / 105 / 85 / 90 / 87 / 115 = 565** (+20 HP +10 Atk +20 Def +10 SpA +15 SpD +5 Spe): most to its HP and Defense.
- **Abilities**: Unburden, Poison Touch, hidden Prankster (was Unburden, Poison Touch, hidden Prankster).
- **Evolution**: Grafaiai at level 248, knowing Knock Off. Data: `{"targetSpecies":"Graffitape","method":"LevelKnowsMove","level":248,"move":"Knock Off"}`.
- **Look**: from our grey Grafaiai with its painted fingers: taller, all ten fingers dyed a colour each, a swirl of paint down its tail and over its chest (decals), and a long tail it paints with like a brush. **New feature**: the tail it paints with.
- **Pokédex**: It covers whole walls overnight in poison paint that dries harmless. Each mural is its own, and no two of its kind paint alike.

### 13. Thornwraith, from Brambleghast (947): yes

- **Typing**: Grass/Ghost, kept. *Alternative*: Grass/Ground.
- **The Dust Devil Pokémon**, No. 2947. 1.6 m, 10.0 kg (Brambleghast 1.2 m, 6 kg).
- **Stats**: 55 / 115 / 70 / 80 / 70 / 90 = 480 → **85 / 125 / 90 / 80 / 85 / 95 = 560** (+30 HP +10 Atk +20 Def +15 SpD +5 Spe): most to its HP.
- **Abilities**: Wind Rider, hidden Infiltrator (was Wind Rider, hidden Infiltrator).
- **Evolution**: Brambleghast at level 248, by day. Data: `{"targetSpecies":"Thornwraith","method":"LevelDay","level":248}`.
- **Look**: from our brown Brambleghast with its pale face of vines: larger, its ball of brambles wound into a spinning ring round a ghostly core (Glow, pale green), dust trailing from its edge and two long thorned vines like arms. **New feature**: the ring of brambles round its core.
- **Pokédex**: It spins across the dunes faster than the wind that carries it. What it catches in its thorns it carries away for good.

### 14. Myceliarch, from Toedscruel (949): yes

- **Typing**: Ground/Grass, kept. *Alternative*: Grass/Poison.
- **The Mycelium Pokémon**, No. 2949. 2.3 m, 80.0 kg (Toedscruel 1.9 m, 58 kg).
- **Stats**: 80 / 70 / 65 / 80 / 120 / 100 = 515 → **100 / 80 / 90 / 95 / 120 / 110 = 595** (+20 HP +10 Atk +25 Def +15 SpA +10 Spe): most to its Defense.
- **Abilities**: Mycelium Might (was Mycelium Might).
- **Evolution**: Toedscruel at level 252, at night. Data: `{"targetSpecies":"Myceliarch","method":"LevelNight","level":252}`.
- **Look**: from our dark Toedscruel with its pale cap and tentacles: larger, its cap broader with a ring of smaller caps round it, its tentacles longer with pale threads between them and spots that glow at night (Glow, pale blue). **New feature**: the ring of small caps.
- **Pokédex**: Its threads run under a whole forest. When one tree is cut down, it knows it from a mountain away.

### 15. Klawlossus, from Klawf (950): strong yes

- **Typing**: Rock → **Rock/Steel**. *Alternative*: Rock/Fighting.
- **The Cliff Ambush Pokémon**, No. 2950. 1.8 m, 140.0 kg (Klawf 1.3 m, 79 kg).
- **Stats**: 70 / 100 / 115 / 35 / 55 / 75 = 450 → **90 / 115 / 125 / 35 / 85 / 80 = 530** (+20 HP +15 Atk +10 Def +30 SpD +5 Spe): most to its Sp. Def.
- **Abilities**: Anger Shell, Shell Armor, hidden Regenerator (was Anger Shell, Shell Armor, hidden Regenerator).
- **Evolution**: Klawf at level 246, knowing Guillotine. Data: `{"targetSpecies":"Klawlossus","method":"LevelKnowsMove","level":246,"move":"Guillotine"}`.
- **Look**: from our red and grey Klawf with its great claws: larger, its shell plated in grey steel over the red, one claw much larger than the other (a fiddler's), and its eyestalks longer with a heavy brow. **New feature**: the one great claw.
- **Pokédex**: It hangs head down from the cliffs for days. A single snap of its great claw cuts a boulder in two.

### 16. Habanerogue, from Scovillain (952): strong yes

- **Typing**: Grass/Fire, kept. *Alternative*: Grass/Dark.
- **The Ghost Pepper Pokémon**, No. 2952. 1.2 m, 25.0 kg (Scovillain 0.9 m, 15 kg).
- **Stats**: 65 / 108 / 65 / 108 / 65 / 75 = 486 → **85 / 118 / 85 / 118 / 80 / 80 = 566** (+20 HP +10 Atk +20 Def +10 SpA +15 SpD +5 Spe): most to its HP and Defense.
- **Abilities**: Chlorophyll, Insomnia, hidden Moody (was Chlorophyll, Insomnia, hidden Moody).
- **Evolution**: Scovillain with a Fire Stone used on it at level 248 or above. Data: `{"targetSpecies":"Habanerogue","method":"UseItem","level":248,"item":"Fire Stone"}`.
- **Look**: from our green and red Scovillain with its two heads: larger, a third small head between the two, still green, the red head's crown of leaves a jester's cap and its body ribbed like a pepper's skin. **New feature**: the third, green head.
- **Pokédex**: The green head ripens only when both others agree. Its spice is so hot that the air round it shimmers on a cold day.

### 17. Khepristar, from Rabsca (954): strong yes

- **Typing**: Bug/Psychic, kept. *Alternative*: Bug/Fire.
- **The Sun Roller Pokémon**, No. 2954. 0.6 m, 8.0 kg (Rabsca 0.3 m, 3.5 kg).
- **Stats**: 75 / 50 / 85 / 115 / 100 / 45 = 470 → **95 / 50 / 100 / 135 / 115 / 55 = 550** (+20 HP +15 Def +20 SpA +15 SpD +10 Spe): most to its HP and Sp. Atk.
- **Abilities**: Synchronize, hidden Telepathy (was Synchronize, hidden Telepathy).
- **Evolution**: Rabsca at level 246, by day. Data: `{"targetSpecies":"Khepristar","method":"LevelDay","level":246}`.
- **Look**: from our green beetle lying on its back under its ball: larger, the ball of sand turned into a glowing gold sphere with rings round it (Glow, gold), its legs longer and its wing-cases marked with a pale sun. **New feature**: the rings round its sphere.
- **Pokédex**: It rolls its glowing sphere from dawn to dusk. Ancient people thought it pushed the sun across the sky.

### 18. Mirastrich, from Espathra (956): yes

- **Typing**: Psychic → **Psychic/Fairy**. *Alternative*: Psychic alone.
- **The Mirage Pokémon**, No. 2956. 2.3 m, 120.0 kg (Espathra 1.9 m, 90 kg).
- **Stats**: 95 / 60 / 60 / 101 / 60 / 105 = 481 → **105 / 60 / 85 / 121 / 85 / 105 = 561** (+10 HP +25 Def +20 SpA +25 SpD): most to its Defense and Sp. Def.
- **Abilities**: Opportunist, Frisk, hidden Speed Boost (was Opportunist, Frisk, hidden Speed Boost).
- **Evolution**: Espathra at level 248, knowing Lumina Crash. Data: `{"targetSpecies":"Mirastrich","method":"LevelKnowsMove","level":248,"move":"Lumina Crash"}`.
- **Look**: from our yellow and pink Espathra with its fan of eye feathers: taller, the fan grown into a full ring of eye feathers round its head, the eyes on them changing colour (Glow, pink and blue) and its legs longer. **New feature**: the full ring of eye feathers.
- **Pokédex**: It turns the ring of eyes on whatever chases it, and the pursuer sees a mirage of an empty plain while it runs off.

### 19. Wugsextet, from Wugtrio (961): strong yes

- **Typing**: Water → **Water/Ground**. *Alternative*: Water alone.
- **The Eel Colony Pokémon**, No. 2961. 1.5 m, 12.0 kg (Wugtrio 1.2 m, 5.4 kg).
- **Stats**: 35 / 100 / 50 / 50 / 70 / 120 = 425 → **75 / 110 / 70 / 50 / 80 / 120 = 505** (+40 HP +10 Atk +20 Def +10 SpD): most to its HP.
- **Abilities**: Gooey, Rattled, hidden Sand Veil (was Gooey, Rattled, hidden Sand Veil).
- **Evolution**: Wugtrio at level 244, knowing Triple Dive. Data: `{"targetSpecies":"Wugsextet","method":"LevelKnowsMove","level":244,"move":"Triple Dive"}`.
- **Look**: from our three red garden eels with their yellow noses: six eels in two rings of three, the middle ones taller, each with its yellow lips and a stripe of pale spots down its body. **New feature**: the second ring of three.
- **Pokédex**: Six heads share one body under the sand. Divers have dug for hours to find its end and never reached it.

### 20. Bomberon, from Bombirdier (962): yes

- **Typing**: Flying/Dark, kept. *Alternative*: Flying/Rock.
- **The Air Raid Pokémon**, No. 2962. 1.9 m, 60.0 kg (Bombirdier 1.5 m, 42.9 kg).
- **Stats**: 70 / 103 / 85 / 60 / 85 / 82 = 485 → **90 / 123 / 100 / 60 / 100 / 92 = 565** (+20 HP +20 Atk +15 Def +15 SpD +10 Spe): most to its HP and Attack.
- **Abilities**: Big Pecks, Keen Eye, hidden Rocky Payload (was Big Pecks, Keen Eye, hidden Rocky Payload).
- **Evolution**: Bombirdier at level 248, holding a Hard Stone (used up). Data: `{"targetSpecies":"Bomberon","method":"LevelHoldingItem","level":248,"item":"Hard Stone"}`.
- **Look**: from our white and black Bombirdier with its apron: larger, the apron a satchel slung across its chest with a boulder in it, its beak heavier, black flight feathers longer and a band of dark feathers over its eyes like goggles. **New feature**: the satchel with its boulder.
- **Pokédex**: It flies over rival nests and drops a boulder on them. Its satchel can hold a rock heavier than itself.

### 21. Turbovroom, from Revavroom (966): yes

- **Typing**: Steel/Poison, kept. *Alternative*: Steel/Fire.
- **The Turbo Pokémon**, No. 2966. 2.2 m, 180.0 kg (Revavroom 1.8 m, 120 kg).
- **Stats**: 80 / 119 / 90 / 54 / 67 / 90 = 500 → **95 / 134 / 105 / 54 / 92 / 100 = 580** (+15 HP +15 Atk +15 Def +25 SpD +10 Spe): most to its Sp. Def.
- **Abilities**: Overcoat, hidden Filter (was Overcoat, hidden Filter).
- **Evolution**: Revavroom at level 250, knowing Spin Out. Data: `{"targetSpecies":"Turbovroom","method":"LevelKnowsMove","level":250,"move":"Spin Out"}`.
- **Look**: from our grey Revavroom with its pipes: longer and lower, its body a racing machine with two more pipes at its back, a spoiler of steel over its tail and lights at its front (Glow, yellow). **New feature**: the steel spoiler.
- **Pokédex**: Its roar rattles windows a kilometre away. Racers who hear it behind them pull aside at once.

### 22. Velocidrake, from Cyclizar (967): yes

- **Typing**: Dragon/Normal, kept. *Alternative*: Dragon/Flying.
- **The Steed Pokémon**, No. 2967. 2.0 m, 90.0 kg (Cyclizar 1.6 m, 63 kg).
- **Stats**: 70 / 95 / 65 / 85 / 65 / 121 = 501 → **90 / 105 / 90 / 85 / 90 / 121 = 581** (+20 HP +10 Atk +25 Def +25 SpD): most to its Defense and Sp. Def.
- **Abilities**: Shed Skin, hidden Regenerator (was Shed Skin, hidden Regenerator).
- **Evolution**: Cyclizar at level 250, with friendship 220 or more, by day. Data: `{"targetSpecies":"Velocidrake","method":"FriendshipDay","level":250}`.
- **Look**: from our green Cyclizar with its frills: larger and longer, its frills grown into a saddle of hard plates, two short horns, and its tail a long whip of green with an orange tip. **New feature**: the saddle of plates.
- **Pokédex**: A rider it trusts can sit on it from one end of Paldea to the other in a day. It lets no one else near the saddle.

### 23. Orthobore, from Orthworm (968): yes

- **Typing**: Steel → **Steel/Ground**. *Alternative*: Steel alone.
- **The Tunnel Bore Pokémon**, No. 2968. 3.2 m, 450.0 kg (Orthworm 2.5 m, 310 kg).
- **Stats**: 70 / 85 / 145 / 60 / 55 / 65 = 480 → **100 / 105 / 145 / 60 / 80 / 70 = 560** (+30 HP +20 Atk +25 SpD +5 Spe): most to its HP.
- **Abilities**: Earth Eater, hidden Sand Veil (was Earth Eater, hidden Sand Veil).
- **Evolution**: Orthworm at level 248, knowing Earthquake. Data: `{"targetSpecies":"Orthobore","method":"LevelKnowsMove","level":248,"move":"Earthquake"}`.
- **Look**: from our pink and grey Orthworm with its steel segments and head cones: longer, its head a drill of three ridged cones, more segments with steel plates, and dust trailing from its rear. **New feature**: the drill of three cones.
- **Pokédex**: It eats the iron in the rock as it goes. The tunnels it leaves are so round that people use them as roads.

### 24. Geodiadem, from Glimmora (970): yes

- **Typing**: Rock/Poison, kept. *Alternative*: Rock/Fairy.
- **The Gem Bloom Pokémon**, No. 2970. 1.9 m, 70.0 kg (Glimmora 1.5 m, 45 kg).
- **Stats**: 83 / 55 / 90 / 130 / 81 / 86 = 525 → **98 / 55 / 100 / 140 / 111 / 96 = 600** (+15 HP +10 Def +10 SpA +30 SpD +10 Spe): the rise is +75, to the 600 ceiling: most to its Sp. Def.
- **Abilities**: Toxic Debris, hidden Corrosion (was Toxic Debris, hidden Corrosion).
- **Evolution**: Glimmora at level 252, holding a Star Piece (used up). Data: `{"targetSpecies":"Geodiadem","method":"LevelHoldingItem","level":252,"item":"Star Piece"}`.
- **Look**: from our blue and pink Glimmora with its crystal petals: larger, a second ring of crystal petals, their tips lit (Glow, pink) and a crown of small crystals over its face. **New feature**: the second ring of crystal petals.
- **Pokédex**: Its petals ring like glass in a cave's draught. The poison crystals it sheds are worth more than gold to anyone who can handle them.

### 25. Barrowhound, from Houndstone (972): yes

- **Typing**: Ghost, kept. *Alternative*: Ghost/Ground.
- **The Barrow Guard Pokémon**, No. 2972. 2.4 m, 25.0 kg (Houndstone 2 m, 15 kg).
- **Stats**: 72 / 101 / 100 / 50 / 97 / 68 = 488 → **92 / 116 / 110 / 60 / 107 / 83 = 568** (+20 HP +15 Atk +10 Def +10 SpA +10 SpD +15 Spe): most to its HP.
- **Abilities**: Sand Rush, hidden Fluffy (was Sand Rush, hidden Fluffy).
- **Evolution**: Houndstone at level 248, at night. Data: `{"targetSpecies":"Barrowhound","method":"LevelNight","level":248}`.
- **Look**: from our white Houndstone with its gravestone: longer, the gravestone a whole cairn of three stones on its back, a candle flame floating at its brow (Glow, white) and its ears longer and ragged. **New feature**: the cairn of three stones.
- **Pokédex**: It keeps watch over old graves through the night. Lost travellers who follow its candle always find the road again.

### 26. Flamenkick, from Flamigo (973): yes

- **Typing**: Flying/Fighting, kept. *Alternative*: Flying/Fire.
- **The Flamenco Pokémon**, No. 2973. 2.0 m, 50.0 kg (Flamigo 1.6 m, 37 kg).
- **Stats**: 82 / 115 / 74 / 75 / 64 / 90 = 500 → **92 / 125 / 94 / 75 / 94 / 100 = 580** (+10 HP +10 Atk +20 Def +30 SpD +10 Spe): most to its Sp. Def.
- **Abilities**: Scrappy, Tangled Feet, hidden Costar (was Scrappy, Tangled Feet, hidden Costar).
- **Evolution**: Flamigo at level 250, knowing Brave Bird. Data: `{"targetSpecies":"Flamenkick","method":"LevelKnowsMove","level":250,"move":"Brave Bird"}`.
- **Look**: from our pink Flamigo standing on one leg: taller, its neck feathers grown into a ruffled collar like a dancer's, a long train of tail feathers that sweep the ground and red bands at its knees. **New feature**: the ruffled collar.
- **Pokédex**: When it dances on one leg, every Flamigo near it dances too. Its kicks land on the beat of its stamping.

### 27. Cetiberg, from Cetitan (975): yes

- **Typing**: Ice, kept. *Alternative*: Ice/Ground.
- **The Iceberg Whale Pokémon**, No. 2975. 5.4 m, 950.0 kg (Cetitan 4.5 m, 700 kg).
- **Stats**: 170 / 113 / 65 / 45 / 55 / 73 = 521 → **170 / 125 / 95 / 45 / 85 / 80 = 600** (+12 Atk +30 Def +30 SpD +7 Spe): the rise is +79, to the 600 ceiling: most to its Defense and Sp. Def.
- **Abilities**: Thick Fat, Slush Rush, hidden Sheer Force (was Thick Fat, Slush Rush, hidden Sheer Force).
- **Evolution**: Cetitan with an Ice Stone used on it at level 252 or above. Data: `{"targetSpecies":"Cetiberg","method":"UseItem","level":252,"item":"Ice Stone"}`.
- **Look**: from our white Cetitan on all fours with its horn: larger, its back crusted with ice like an iceberg, its horn grown longer and clear, and its tusks of ice heavier. **New feature**: the iceberg on its back.
- **Pokédex**: It walks slowly through the snowfields with an iceberg on its back. Young Cetoddle ride on it from one mountain to the next.

### 28. Sashimaru, from Veluza (976): strong yes

- **Typing**: Water/Psychic, kept. *Alternative*: Water/Steel.
- **The Bladefin Pokémon**, No. 2976. 3.2 m, 120.0 kg (Veluza 2.5 m, 90 kg).
- **Stats**: 90 / 102 / 73 / 78 / 65 / 70 = 478 → **100 / 122 / 88 / 88 / 85 / 75 = 558** (+10 HP +20 Atk +15 Def +10 SpA +20 SpD +5 Spe): most to its Attack and Sp. Def.
- **Abilities**: Mold Breaker, hidden Sharpness (was Mold Breaker, hidden Sharpness).
- **Evolution**: Veluza at level 246, knowing Fillet Away. Data: `{"targetSpecies":"Sashimaru","method":"LevelKnowsMove","level":246,"move":"Fillet Away"}`.
- **Look**: from our grey and pink Veluza: longer, its fins grown into blades along its back, its head finned like a helm and the pink stripe down its side wider. **New feature**: the blades along its back.
- **Pokédex**: Its fins cut through a wave without a splash. It thinks clearly only when it has shed what it does not need.

### 29. Grandozo, from Dondozo (977): yes

- **Typing**: Water, kept. *Alternative*: Water/Dragon.
- **The Great Catfish Pokémon**, No. 2977. 15.0 m, 330.0 kg (Dondozo 12 m, 220 kg).
- **Stats**: 150 / 100 / 115 / 65 / 65 / 35 = 530 → **150 / 115 / 125 / 65 / 100 / 45 = 600** (+15 Atk +10 Def +35 SpD +10 Spe): the rise is +70, to the 600 ceiling: most to its Sp. Def.
- **Abilities**: Unaware, Oblivious, hidden Water Veil (was Unaware, Oblivious, hidden Water Veil).
- **Evolution**: Dondozo at level 250, with a Tatsugiri in the party. Data: `{"targetSpecies":"Grandozo","method":"LevelWithSpeciesInParty","level":250,"species":"Tatsugiri"}`.
- **Look**: from our blue and pink Dondozo with its open mouth: larger, its whiskers grown long and trailing, a pale ridge along its back and its lips a deep pink. **New feature**: the trailing whiskers.
- **Pokédex**: It swallows a ship's wake and the fish in it. It goes where the Tatsugiri in its mouth points, and nowhere else.

### 30. Tatsushogun, from Tatsugiri (978): strong yes

- **Typing**: Dragon/Water, kept. *Alternative*: Dragon/Fairy.
- **The Admiral Pokémon**, No. 2978. 0.5 m, 14.0 kg (Tatsugiri 0.3 m, 8 kg).
- **Stats**: 68 / 50 / 60 / 120 / 95 / 82 = 475 → **88 / 50 / 85 / 135 / 105 / 92 = 555** (+20 HP +25 Def +15 SpA +10 SpD +10 Spe): most to its Defense.
- **Abilities**: Commander, hidden Storm Drain (was Commander, hidden Storm Drain).
- **Evolution**: Tatsugiri at level 250, with a Dondozo in the party. Data: `{"targetSpecies":"Tatsushogun","method":"LevelWithSpeciesInParty","level":250,"species":"Dondozo"}`.
- **Look**: from our Tatsugiri in its Curly Form: larger, its long upper fin a curled admiral's hat, a cape of fin down its back and a pale baton of a fin under its chin. **New feature**: the admiral's hat of a fin.
- **Pokédex**: It calls the moves from inside its Dondozo's mouth. The other Tatsugiri of the bay follow its orders without a sound.

### 31. Clodkaiser, from Clodsire (980): strong yes

- **Typing**: Poison/Ground, kept. *Alternative*: Poison/Water.
- **The Bog Emperor Pokémon**, No. 2980. 2.3 m, 330.0 kg (Clodsire 1.8 m, 223 kg).
- **Stats**: 130 / 75 / 60 / 45 / 100 / 20 = 430 → **140 / 95 / 90 / 50 / 110 / 25 = 510** (+10 HP +20 Atk +30 Def +5 SpA +10 SpD +5 Spe): most to its Defense.
- **Abilities**: Poison Point, Water Absorb, hidden Unaware (was Poison Point, Water Absorb, hidden Unaware).
- **Evolution**: Clodsire at level 244, holding a Poison Barb (used up). Data: `{"targetSpecies":"Clodkaiser","method":"LevelHoldingItem","level":244,"item":"Poison Barb"}`.
- **Look**: from our brown Clodsire with its fin spines: larger, its spines grown into a crown of poison quills down its back, its face rounder and placid, and its tail broader. **New feature**: the crown of quills.
- **Pokédex**: It lies in the bog and never moves, so the bog moves round it. Its quills are tipped with a poison that numbs for a whole day.

### 32. Farigigiraf, from Farigiraf (981): maybe

- **Typing**: Normal/Psychic, kept. *Alternative*: Psychic/Dark.
- **The Twin Mind Pokémon**, No. 2981. 4.0 m, 220.0 kg (Farigiraf 3.2 m, 160 kg).
- **Stats**: 120 / 90 / 70 / 110 / 70 / 60 = 520 → **120 / 100 / 95 / 120 / 95 / 70 = 600** (+10 Atk +25 Def +10 SpA +25 SpD +10 Spe): most to its Defense and Sp. Def.
- **Abilities**: Cud Chew, Armor Tail, hidden Sap Sipper (was Cud Chew, Armor Tail, hidden Sap Sipper).
- **Evolution**: Farigiraf at level 252, knowing Twin Beam. Data: `{"targetSpecies":"Farigigiraf","method":"LevelKnowsMove","level":252,"move":"Twin Beam"}`.
- **Look**: from our Farigiraf with its tail head: taller, the tail's head grown a neck of its own so the two face each other across its back, the stripes of its body matching and both heads' horns longer. **New feature**: the tail head's own neck.
- **Pokédex**: Its two heads have grown equal. Each thinks half of every thought, and neither can be fooled while the other is awake.

### 33. Dunsprawl, from Dudunsparce (982): maybe

- **Typing**: Normal, kept. *Alternative*: Normal/Dragon.
- **The Land Serpent Pokémon**, No. 2982. 5.0 m, 60.0 kg (Dudunsparce 3.6 m, 39.2 kg).
- **Stats**: 125 / 100 / 80 / 85 / 75 / 55 = 520 → **125 / 110 / 100 / 95 / 100 / 70 = 600** (+10 Atk +20 Def +10 SpA +25 SpD +15 Spe): most to its Sp. Def.
- **Abilities**: Serene Grace, Run Away, hidden Rattled (was Serene Grace, Run Away, hidden Rattled).
- **Evolution**: Dudunsparce at level 252, knowing Hyper Drill. Data: `{"targetSpecies":"Dunsprawl","method":"LevelKnowsMove","level":252,"move":"Hyper Drill"}`.
- **Look**: from our Dudunsparce with two segments: longer, four segments, each with its pale stripes, a drill tail of three ridges and its little wings larger. **New feature**: the four segments.
- **Pokédex**: Its tunnels under the hills run so far that no one has mapped them. It forgets where its tail is.

### 34. Bullionaire, from Gholdengo (1000): maybe

- **Typing**: Steel/Ghost, kept. *Alternative*: Steel/Psychic.
- **The Treasure Hoard Pokémon**, No. 3000. 1.5 m, 45.0 kg (Gholdengo 1.2 m, 30 kg).
- **Stats**: 87 / 60 / 95 / 133 / 91 / 84 = 550 → **102 / 60 / 100 / 138 / 106 / 94 = 600** (+15 HP +5 Def +5 SpA +15 SpD +10 Spe): the rise is +50, to the 600 ceiling: most to its HP and Sp. Def.
- **Abilities**: Good as Gold (was Good as Gold).
- **Evolution**: Gholdengo at level 256, with 999 Gimmighoul Coins in the bag (used up). Data: `{"targetSpecies":"Bullionaire","method":"LevelWithItemsInBag","level":256,"item":"Gimmighoul Coin","value":999}`.
- **Look**: from our gold Gholdengo riding its surfboard of coins: taller, the board a tall stack of coins it stands on, a crown of coins over its head and a gown of linked coins down its body (Metal). **New feature**: the crown of coins.
- **Pokédex**: It counts every coin it owns every night. Those who try to take one find their own pockets empty in the morning.

### 35. Tabletruin, from Wo-Chien (1001): maybe

- **Typing**: Dark/Grass, kept. *Alternative*: Dark/Ground.
- **The Ruinous Pokémon**, No. 3001. 2.0 m, 110.0 kg (Wo-Chien 1.5 m, 74.2 kg).
- **Stats**: 85 / 85 / 100 / 95 / 135 / 70 = 570 → **105 / 95 / 115 / 115 / 145 / 85 = 660** (+20 HP +10 Atk +15 Def +20 SpA +10 SpD +15 Spe): most to its HP and Sp. Atk.
- **Abilities**: Tablets of Ruin (was Tablets of Ruin).
- **Evolution**: Wo-Chien at level 258, with a Chien-Pao in the party. Data: `{"targetSpecies":"Tabletruin","method":"LevelWithSpeciesInParty","level":258,"species":"Chien-Pao"}`.
- **Look**: from our Wo-Chien, the snail under its wooden tablets: larger, eight tablets now in a fan, each carved with a dark seal (Glow, red), and its shell of leaves a mound with roots trailing from it. **New feature**: the fan of eight tablets.
- **Pokédex**: Its tablets are carved with the names of a king's servants. Each one it takes back leaves the land round it a little greener.

### 36. Swordruin, from Chien-Pao (1002): maybe

- **Typing**: Dark/Ice, kept. *Alternative*: Ice/Steel.
- **The Ruinous Pokémon**, No. 3002. 2.4 m, 200.0 kg (Chien-Pao 1.9 m, 152.2 kg).
- **Stats**: 80 / 120 / 80 / 90 / 65 / 135 = 570 → **100 / 135 / 100 / 95 / 90 / 140 = 660** (+20 HP +15 Atk +20 Def +5 SpA +25 SpD +5 Spe): most to its Sp. Def.
- **Abilities**: Sword of Ruin (was Sword of Ruin).
- **Evolution**: Chien-Pao at level 258, with a Ting-Lu in the party. Data: `{"targetSpecies":"Swordruin","method":"LevelWithSpeciesInParty","level":258,"species":"Ting-Lu"}`.
- **Look**: from our white Chien-Pao with its two sword fangs: larger, four blades of ice now, two at its jaw and two from its shoulders, a seal on its brow (Glow, red) and its fur longer and frosted. **New feature**: the shoulder blades of ice.
- **Pokédex**: Its four blades were cut from the ice of a king's tomb. A cold wind follows its footprints for days.

### 37. Vesselruin, from Ting-Lu (1003): maybe

- **Typing**: Dark/Ground, kept. *Alternative*: Ground/Rock.
- **The Ruinous Pokémon**, No. 3003. 3.2 m, 900.0 kg (Ting-Lu 2.7 m, 699.7 kg).
- **Stats**: 155 / 110 / 125 / 55 / 80 / 45 = 570 → **160 / 130 / 135 / 60 / 120 / 55 = 660** (+5 HP +20 Atk +10 Def +5 SpA +40 SpD +10 Spe): most to its Sp. Def.
- **Abilities**: Vessel of Ruin (was Vessel of Ruin).
- **Evolution**: Ting-Lu at level 258, with a Chi-Yu in the party. Data: `{"targetSpecies":"Vesselruin","method":"LevelWithSpeciesInParty","level":258,"species":"Chi-Yu"}`.
- **Look**: from our brown Ting-Lu with the bronze vessel for a head: larger, the vessel a great bronze bell of a cauldron with a seal on its side (Glow, red), its antlers branched further and its legs heavier. **New feature**: the cauldron of a head.
- **Pokédex**: The vessel on its head is said to hold the land's grudge. Wherever it lies down, nothing grows for a year.

### 38. Beadruin, from Chi-Yu (1004): maybe

- **Typing**: Dark/Fire, kept. *Alternative*: Fire/Water.
- **The Ruinous Pokémon**, No. 3004. 0.8 m, 10.0 kg (Chi-Yu 0.4 m, 4.9 kg).
- **Stats**: 55 / 80 / 80 / 135 / 120 / 100 = 570 → **90 / 85 / 105 / 150 / 125 / 105 = 660** (+35 HP +5 Atk +25 Def +15 SpA +5 SpD +5 Spe): most to its HP.
- **Abilities**: Beads of Ruin (was Beads of Ruin).
- **Evolution**: Chi-Yu at level 258, with a Wo-Chien in the party. Data: `{"targetSpecies":"Beadruin","method":"LevelWithSpeciesInParty","level":258,"species":"Wo-Chien"}`.
- **Look**: from our red Chi-Yu, the flame fish with its beads: larger, its flame body longer with fins of fire, six beads now in a ring round it (Glow, red) and a seal on its brow. **New feature**: the ring of six beads.
- **Pokédex**: Its six beads hold the fire of a king's jealousy. It swims through lava as other fish swim through water.

### 39. Chagamage, from Sinistcha (1013): yes

- **Typing**: Grass/Ghost, kept. *Alternative*: Grass/Fire.
- **The Tea Ceremony Pokémon**, No. 3013. 0.5 m, 8.0 kg (Sinistcha 0.2 m, 2.2 kg).
- **Stats**: 71 / 60 / 106 / 121 / 80 / 70 = 508 → **91 / 60 / 106 / 141 / 105 / 85 = 588** (+20 HP +20 SpA +25 SpD +15 Spe): most to its Sp. Def.
- **Abilities**: Hospitality, hidden Heatproof (was Hospitality, hidden Heatproof).
- **Evolution**: Sinistcha with a Masterpiece Teacup used on it at level 250 or above. Data: `{"targetSpecies":"Chagamage","method":"UseItem","level":250,"item":"Masterpiece Teacup"}`.
- **Look**: from our green Sinistcha in its bowl: larger, its bowl now an iron kettle with a ghostly whisk floating over it, steam of matcha rising (decals) and the green tea within lit (Glow, green). **New feature**: the iron kettle.
- **Pokédex**: It holds a tea ceremony for anyone who sits still. Drinking the tea it pours drains a guest's strength, and the cups are lovely.

## Balance

Measured by the script, as the earlier batches':

- **Totals**: 484 (Fortarach) to 600 (Pyropaladin from 525, Duskenblade from 525, Geodiadem from 525, Cetiberg from 521, Grandozo from 530, Farigigiraf from 520, Dunsprawl from 520, Bullionaire from 550, each risen only to the ceiling), the Treasures of Ruin 660. Nothing that isn't legendary passes 600.
- **Highest of each stat** among those that aren't legendary: HP 170 (Cetiberg), Attack 140 (Duskenblade), Defense 145 (Orthobore), Sp. Atk 141 (Chagamage), Sp. Def 120 (Myceliarch), Speed 125 (Terawattrel). Among the Treasures: HP 160 (Vesselruin), Attack 135 (Swordruin), Sp. Atk 150 (Beadruin), Speed 140 (Swordruin). Grandozo's and Cetiberg's HP are Dondozo's 150 and Cetitan's 170 kept, unrisen; their rise goes to the Sp. Def they lacked.
- **The weakest stay modest on purpose** (batch 1's answer: no floor): Fortarach 484, Bossquawk 497, Wugsextet 505, Clodkaiser 510, from the four lowest totals of the region's last stages and species alone.
- **Abilities**: every species keeps its own; none is changed.

**Flagged as strong** (each inside the official range; the user may want one turned down):
1. **Bullionaire**: Good as Gold, which no status move touches, with 138 Sp. Atk and 106 Sp. Def. *To turn down*: Sp. Atk kept at 133 and the 5 to HP; or keep it a maybe and leave Gholdengo as it is.
2. **Velocidrake**: Regenerator (hidden) and Shed Tail in its line, with Cyclizar's 121 Speed kept and its defences risen to 90. *To turn down*: Defense and Sp. Def +20 each and the 10 to HP.
3. **Cetiberg**: Cetitan's 170 HP with Thick Fat and now 95 Defense and 85 Sp. Def; a wall that hits back with 125 Attack and Sheer Force. *To turn down*: Defense +20 and the 10 to Speed.
4. **Habanerogue**: Scovillain's hidden Moody, kept as batch 4 kept Bibarel's for Dambarel (question 9).
5. **Klawlossus**: Regenerator (hidden) behind 125 Defense, and Anger Shell's rises at half HP with 115 Attack. *To turn down*: Sp. Def +20 and the 10 to HP.

**Weaker by design**: Clodkaiser keeps Clodsire's Speed of 20 (now 25), the slowest of the batch, as the bog it lies in; Squeakshire's rise is spread as its family is; half of Wugsextet's rise goes to the HP of 35 it lacked.

## Open questions: only what is new for Paldea

The earlier batches' answers are settled and applied (above). These are new.

1. **The level band.** Batch 3's rule is "within the region's own climb, at most 160", and Paldea's climb (the study's fair climb, which no region file has written yet) runs from about 242 to 258, all of it past 160. *Default*: **the climb wins wherever it starts past 160**: here 244 to 256, the Treasures at 258, two under the cap of 260. The same question meets every batch whose region is climbed past 160, which is Unova's on (Black 2's region file climbs 162 to 185, the study sets Kalos at about 205, Alola 224, Galar 242); the drafts of batches 5 to 8, written beside this one, default it the same way (Unova 165 to 183, Kalos 188 to 203, Alola 209 to 221, Galar 226 to 240, each set of legendaries just past its Champion, where Paldea's Champion at 258 leaves only the cap past him, so the Treasures stand at his level), and one answer is best given for all five. *Alternatives*: (a) keep 160 as the ceiling for every batch, so that every Paldea species evolves at its first level up in Paldea, which batch 3 refused for Hoenn; (b) one band for every batch past Hoenn, 161 to 200, whatever its region's climb, which reads more like the games' own levels but lets the later regions' species evolve as soon as they are caught; (c) wait for Paldea's region file (plan 23 · Z13) and set the band from its `levels`, leaving the numbers here as placeholders.
2. **The Treasures of Ruin.** *Default*: build them as a set of four, each at level 258 with the next in the party (a ring), +90 to 660. *Alternatives*: each holding an item of its own (the data has nothing of theirs: the nearest would be a Spell Tag for all four), each with all three others in the party (the engine names one species to a row), or not at all.
3. **The Loyal Three.** *Default*: no (above). *Alternatives*: a set of three, each with a Pecharunt in the party, risen only to **600**, the master's own total (+45 each), as batch 3 kept the Regis under Regigigas; or to 660 by the settled rule for a legendary set, past their master.
4. **The paradox Pokémon.** *Default*: no, all twenty (above). *Alternatives*: the fourteen of the base game (Great Tusk to Iron Thorns, Roaring Moon and Iron Valiant) at +30 to 600 by the settled ceiling, each at night (the ancient ones) or by day (the future ones), with the six that are the past or future of a legendary (Walking Wake, Iron Leaves, Gouging Fire, Raging Bolt, Iron Boulder, Iron Crown) still no; or Great Tusk and Iron Treads, Donphan's past and future, as a pair by each other in the party, and no others.
5. **The looks that are forms.** Squawkabilly's four plumages, Maushold's family of three, Dudunsparce's Three-Segment Form, Tatsugiri's three looks and Oinkologne's female form are forms in the data. *Default*: ours have one look each, from the species' first (the green plumage, the family of four, two segments, the Curly Form), whatever form evolves; and Oinkologne's female evolves into Trufflord with **her own female look** (a `+f` look of ours, as the earlier batches' females, `PokemonLook.Females`), with the male's stats and abilities, since ours have no forms. *Alternative*: a look of ours for each (colours only: the plumages, the Tatsugiri), which would be forms of ours, against the settled rule.
6. **Palafin.** *Default*: no (above). *Alternative*: an evolution that keeps Palafin's Zero form's look and takes another of `abilities.json`'s abilities in Zero to Hero's place (Swift Swim, or Water Veil), at 457 → 537; Palafin's Hero Form would then be the only Pokémon of its line that is stronger than ours.
7. **Archaludon at 600.** *Default*: no, by the ceiling, as batch 4 rose Tangrowth and the Eeveelutions only to the ceiling. *Alternative*: an exception for a species already at 600 (none of the earlier batches had one), which would be the first of ours past 600 that isn't legendary.
8. **The numbers past 3000.** *Default*: the settled rule, 2000 + the forebear's (Gholdengo 3000, the Treasures 3001 to 3004, Sinistcha 3013); `moves.json`'s 3001 to 3072 are moves, a table of their own. *Alternative*: none needed; a block of its own would break the rule every earlier batch kept.
9. **Moody, again.** Habanerogue keeps Scovillain's hidden Moody, as Dambarel kept Bibarel's. *Default*: kept, as the settled rule keeps a species' abilities and this game bans none. *Alternative*: Flash Fire in Moody's place (in `abilities.json`, not Scovillain's own), as batch 4 offered Swift Swim for Dambarel.
10. **Armarouge and Ceruledge.** Charcadet's two ends are designed as a matching pair, each by a second suit of its own armour at 252. *Default*: built as two species standing alone (either may be struck). *Alternative*: built as a set or not at all, as the fossils were.
11. **Commander.** Tatsushogun keeps Commander and Grandozo is a Dondozo's end, but the ability asks for Dondozo by name. *Default*: as the rules have it (Tatsushogun commands a Dondozo, and nothing commands Grandozo); nothing in the engine changes. *Alternative*: Commander also takes Grandozo and Tatsushogun as their forebears, a line of `BattleCore.Abilities2.cs` (06 · R27's, which asks for the ally's species by the name Dondozo) and the one engine change of this batch.

## The table to answer

One row a species. The user answers each with yes, a change, or no; striking a row in the selection table above is a no.

| # | Ours | Typing | Abilities | HP/Atk/Def/SpA/SpD/Spe (total) | Δ | Evolves |
|---|---|---|---|---|---|---|
| 1 | Trufflord | Normal/Ground | Lingering Aroma, Gluttony, Thick Fat | 120/120/95/59/100/75 (569) | +10 HP +20 Atk +20 Def +20 SpD +10 Spe | level 248, knowing Earth Power |
| 2 | Fortarach | Bug/Steel | Insomnia, Stakeout | 85/94/112/52/101/40 (484) | +25 HP +15 Atk +20 Def +15 SpD +5 Spe | level 244, knowing Sticky Web |
| 3 | Locustorm | Bug/Dark | Swarm, Tinted Lens | 86/122/88/52/80/102 (530) | +15 HP +20 Atk +10 Def +25 SpD +10 Spe | level 246, at night |
| 4 | Squeakshire | Normal/Fairy | Friend Guard, Cheek Pouch, Technician | 94/90/90/70/90/116 (550) | +20 HP +15 Atk +20 Def +5 SpA +15 SpD +5 Spe | level 246, with friendship 220 or more, by day |
| 5 | Brioschund | Fairy/Fire | Well-Baked Body, Aroma Veil | 87/95/120/60/95/100 (557) | +30 HP +15 Atk +5 Def +10 SpA +15 SpD +5 Spe | level 246, holding a Charcoal (used up) |
| 6 | Bossquawk | Normal/Flying | Intimidate, Hustle, Guts | 92/116/76/45/71/97 (497) | +10 HP +20 Atk +25 Def +20 SpD +5 Spe | level 244, knowing Taunt |
| 7 | Pyropaladin | Fire/Psychic | Flash Fire, Weak Armor | 100/60/100/140/110/90 (600) | +15 HP +15 SpA +30 SpD +15 Spe | level 252, holding an Auspicious Armor (used up) |
| 8 | Duskenblade | Fire/Ghost | Flash Fire, Weak Armor | 95/140/100/60/105/100 (600) | +20 HP +15 Atk +20 Def +5 SpD +15 Spe | level 252, holding a Malicious Armor (used up) |
| 9 | Thunderbelly | Electric/Water | Electromorphosis, Static, Damp | 119/74/106/128/98/50 (575) | +10 HP +10 Atk +15 Def +25 SpA +15 SpD +5 Spe | level 248, in the rain |
| 10 | Terawattrel | Electric/Flying | Wind Power, Volt Absorb, Competitive | 90/70/85/120/80/125 (570) | +20 HP +25 Def +15 SpA +20 SpD | level 248, knowing Hurricane |
| 11 | Mastiffdon | Dark | Intimidate, Guard Dog, Stakeout | 95/135/100/60/100/95 (585) | +15 HP +15 Atk +10 Def +30 SpD +10 Spe | level 250, with a Maschiff in the party |
| 12 | Graffitape | Poison/Normal | Unburden, Poison Touch, Prankster | 83/105/85/90/87/115 (565) | +20 HP +10 Atk +20 Def +10 SpA +15 SpD +5 Spe | level 248, knowing Knock Off |
| 13 | Thornwraith | Grass/Ghost | Wind Rider, Infiltrator | 85/125/90/80/85/95 (560) | +30 HP +10 Atk +20 Def +15 SpD +5 Spe | level 248, by day |
| 14 | Myceliarch | Ground/Grass | Mycelium Might | 100/80/90/95/120/110 (595) | +20 HP +10 Atk +25 Def +15 SpA +10 Spe | level 252, at night |
| 15 | Klawlossus | Rock/Steel | Anger Shell, Shell Armor, Regenerator | 90/115/125/35/85/80 (530) | +20 HP +15 Atk +10 Def +30 SpD +5 Spe | level 246, knowing Guillotine |
| 16 | Habanerogue | Grass/Fire | Chlorophyll, Insomnia, Moody | 85/118/85/118/80/80 (566) | +20 HP +10 Atk +20 Def +10 SpA +15 SpD +5 Spe | a Fire Stone used on it at level 248 or above |
| 17 | Khepristar | Bug/Psychic | Synchronize, Telepathy | 95/50/100/135/115/55 (550) | +20 HP +15 Def +20 SpA +15 SpD +10 Spe | level 246, by day |
| 18 | Mirastrich | Psychic/Fairy | Opportunist, Frisk, Speed Boost | 105/60/85/121/85/105 (561) | +10 HP +25 Def +20 SpA +25 SpD | level 248, knowing Lumina Crash |
| 19 | Wugsextet | Water/Ground | Gooey, Rattled, Sand Veil | 75/110/70/50/80/120 (505) | +40 HP +10 Atk +20 Def +10 SpD | level 244, knowing Triple Dive |
| 20 | Bomberon | Flying/Dark | Big Pecks, Keen Eye, Rocky Payload | 90/123/100/60/100/92 (565) | +20 HP +20 Atk +15 Def +15 SpD +10 Spe | level 248, holding a Hard Stone (used up) |
| 21 | Turbovroom | Steel/Poison | Overcoat, Filter | 95/134/105/54/92/100 (580) | +15 HP +15 Atk +15 Def +25 SpD +10 Spe | level 250, knowing Spin Out |
| 22 | Velocidrake | Dragon/Normal | Shed Skin, Regenerator | 90/105/90/85/90/121 (581) | +20 HP +10 Atk +25 Def +25 SpD | level 250, with friendship 220 or more, by day |
| 23 | Orthobore | Steel/Ground | Earth Eater, Sand Veil | 100/105/145/60/80/70 (560) | +30 HP +20 Atk +25 SpD +5 Spe | level 248, knowing Earthquake |
| 24 | Geodiadem | Rock/Poison | Toxic Debris, Corrosion | 98/55/100/140/111/96 (600) | +15 HP +10 Def +10 SpA +30 SpD +10 Spe | level 252, holding a Star Piece (used up) |
| 25 | Barrowhound | Ghost | Sand Rush, Fluffy | 92/116/110/60/107/83 (568) | +20 HP +15 Atk +10 Def +10 SpA +10 SpD +15 Spe | level 248, at night |
| 26 | Flamenkick | Flying/Fighting | Scrappy, Tangled Feet, Costar | 92/125/94/75/94/100 (580) | +10 HP +10 Atk +20 Def +30 SpD +10 Spe | level 250, knowing Brave Bird |
| 27 | Cetiberg | Ice | Thick Fat, Slush Rush, Sheer Force | 170/125/95/45/85/80 (600) | +12 Atk +30 Def +30 SpD +7 Spe | an Ice Stone used on it at level 252 or above |
| 28 | Sashimaru | Water/Psychic | Mold Breaker, Sharpness | 100/122/88/88/85/75 (558) | +10 HP +20 Atk +15 Def +10 SpA +20 SpD +5 Spe | level 246, knowing Fillet Away |
| 29 | Grandozo | Water | Unaware, Oblivious, Water Veil | 150/115/125/65/100/45 (600) | +15 Atk +10 Def +35 SpD +10 Spe | level 250, with a Tatsugiri in the party |
| 30 | Tatsushogun | Dragon/Water | Commander, Storm Drain | 88/50/85/135/105/92 (555) | +20 HP +25 Def +15 SpA +10 SpD +10 Spe | level 250, with a Dondozo in the party |
| 31 | Clodkaiser | Poison/Ground | Poison Point, Water Absorb, Unaware | 140/95/90/50/110/25 (510) | +10 HP +20 Atk +30 Def +5 SpA +10 SpD +5 Spe | level 244, holding a Poison Barb (used up) |
| 32 | Farigigiraf | Normal/Psychic | Cud Chew, Armor Tail, Sap Sipper | 120/100/95/120/95/70 (600) | +10 Atk +25 Def +10 SpA +25 SpD +10 Spe | level 252, knowing Twin Beam |
| 33 | Dunsprawl | Normal | Serene Grace, Run Away, Rattled | 125/110/100/95/100/70 (600) | +10 Atk +20 Def +10 SpA +25 SpD +15 Spe | level 252, knowing Hyper Drill |
| 34 | Bullionaire | Steel/Ghost | Good as Gold | 102/60/100/138/106/94 (600) | +15 HP +5 Def +5 SpA +15 SpD +10 Spe | level 256, with 999 Gimmighoul Coins in the bag (used up) |
| 35 | Tabletruin | Dark/Grass | Tablets of Ruin | 105/95/115/115/145/85 (660) | +20 HP +10 Atk +15 Def +20 SpA +10 SpD +15 Spe | level 258, with a Chien-Pao in the party |
| 36 | Swordruin | Dark/Ice | Sword of Ruin | 100/135/100/95/90/140 (660) | +20 HP +15 Atk +20 Def +5 SpA +25 SpD +5 Spe | level 258, with a Ting-Lu in the party |
| 37 | Vesselruin | Dark/Ground | Vessel of Ruin | 160/130/135/60/120/55 (660) | +5 HP +20 Atk +10 Def +5 SpA +40 SpD +10 Spe | level 258, with a Chi-Yu in the party |
| 38 | Beadruin | Dark/Fire | Beads of Ruin | 90/85/105/150/125/105 (660) | +35 HP +5 Atk +25 Def +15 SpA +5 SpD +5 Spe | level 258, with a Wo-Chien in the party |
| 39 | Chagamage | Grass/Ghost | Hospitality, Heatproof | 91/60/106/141/105/85 (588) | +20 HP +20 SpA +25 SpD +15 Spe | a Masterpiece Teacup used on it at level 250 or above |

## The technical plan (for after the user's answers)

Batch 1's pipeline holds unchanged: each design an entry of `tools/DataImporter/Overrides/species-past-hundred.json` (a `LevelWithSpeciesInParty` row takes `species`, a `LevelWithItemsInBag` row `item` and `value`), two written Pokédex entries and a habitat each in `Data/dex-entries.json` (`--check-entries` clean; the Pokédex lines above are where they start), a run of the importer, `SpeciesPastHundredTests`' count of designs and its `Climb` given Paldea's generation (244 to 256, the Treasures 258) by question 1's answer, and the Treasures' evolutions on the Frontier's ban list (`FrontierRules.IsBannedOfOurs`). The sculpts wait for the user, who is reworking them: until then ours are generated by rule from their data. Nothing in the engine is needed for this batch (but question 11's alternative): every method above runs today, `LevelWithItemsInBag` with a level (Gimmighoul's coins) among them, and `Evolution` checks a row's level before its condition for every method. What is not this batch's: 06 · R31's moves past level 100 (the learnsets take them by type and stats, as batch 1's), the lines of three, and Paldea itself (plan 23 · Z13 to Z16), without which no species of this batch is met at the levels it evolves at.

## The nine batches, and what is left to answer

| Batch | Region | Forebears (National) | Our numbers | Document | Where it stands |
|---|---|---|---|---|---|
| 1 | Kanto | 1 to 151 | 2020 to 2146 (46) | [D14-proposals-kanto.md](D14-proposals-kanto.md) | approved 2026-10-25; data built (wave 51), sculpts built (wave 52) |
| 2 | Johto | 152 to 251 | 2162 to 2245 (34) | [D14-proposals-johto.md](D14-proposals-johto.md) | approved 2026-10-25; data built (wave 52) |
| 3 | Hoenn | 252 to 386 | 2262 to 2381 (51) | [D14-proposals-hoenn.md](D14-proposals-hoenn.md) | approved 2026-10-25; data built (wave 52) |
| 4 | Sinnoh | 387 to 493 | 2400 to 2482 (39) | [D14-proposals-sinnoh.md](D14-proposals-sinnoh.md) | approved 2026-10-25; data built in wave 53 |
| 5 | Unova | 494 to 649 | within 2494 to 2649 (52 in its draft) | D14-proposals-unova.md | proposal written in wave 53, beside this one; to answer |
| 6 | Kalos | 650 to 721 | within 2650 to 2721 (24 in its draft) | D14-proposals-kalos.md | proposal written in wave 53; to answer |
| 7 | Alola | 722 to 809 | within 2722 to 2809 (28 in its draft) | D14-proposals-alola.md | proposal written in wave 53; to answer |
| 8 | Galar (and the seven first met in Hisui) | 810 to 905 | within 2810 to 2905 (42 in its draft) | D14-proposals-galar.md | proposal written in wave 53; to answer |
| 9 | Paldea | 906 to 1025 | 2916 to 3013 (39) | this document | proposal written in wave 53; to answer |

Batches 1 to 4 are 170 species of our own. **What is left for the user to answer**: batches 5 to 9 (each document's designs and its own questions; question 1 here, the band for a region climbed past 160, is shared by all five and is best answered once); the sculpts of every batch, which the user is reworking; plan 06 · R20b's fifteen Megas ([R20b-mega-proposals.md](R20b-mega-proposals.md)), not yet answered; and the Gigantamax designs, which no proposal has yet and the user has not asked for. Once batches 5 to 9 are answered, D14 is the data of every batch, the Pokédex entries, the Romanian names and the sculpts.

## How the numbers were checked

The same kind of Node.js script as the earlier batches', kept in the session's scratch folder and reading only `species.json`, `abilities.json`, `items.json` and `moves.json`'s `support`, with the earlier batches' names and numbers read from the four documents' tables (and, at the end, the names in the four drafts of batches 5 to 8 read from their own worktrees, every capitalised word of them, with no clash; the main session holds the nine against one another again at the merge). It built the lines (a regional form's evolution counted as the form's own, an evolution into a species of ours left out) and the list of the 69 that qualify, and for each of the 39 designs checked: that it qualifies and is of Paldea's generation, and that every qualifying species has a design or a reason for none, and every species given a reason qualifies; that the rise is +80 or what the ceiling leaves (600, or 660 for the legendary set), that no stat falls and that the six stats add up to the total written (the stats are written as the species' own plus the rise, so the two can't differ); that the name is one word of at most 12 letters, taken by no species or form of the data (which holds batches 1 to 3's 131 already), by no name of the earlier documents' tables and by no other design of this batch; that the number is 2000 + the old species' and meets none of the earlier batches'; that the types are one or two of the eighteen; that the abilities exist and fill exactly the old species' slots; that the method is one the data already uses; that the level is between 244 and 256 for a species that isn't legendary and 258 for the Treasures of Ruin; that an item held, used or counted exists and is no key item; that a move to know is in the species' level-up learnset and runs (`support` not None); that a species named in the party exists and isn't the species itself; that each is taller and heavier than what it comes from; that the look names the female's difference exactly where `species.json` says the species has one; and that the shortlist is the twelve strong yeses. The tables and the entries were written by the same script from the same values, so a number can't differ between the selection table, an entry and the table to answer.
