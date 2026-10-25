# D14 · Evolutions past level 100, batch 1 (Kanto): the proposal

Plan 03 · D14, the design part of its first batch. The user asked on 2026-10-26 to start recommending the Pokémon that will evolve after level 100; drafted the same day, **approved by the user on 2026-10-25 ("I accept all the proposals for kanto": all 46 designs, the sets and the defaults of the ten open questions; the three "no" rows stay out)**. **Its data was built in wave 51**: the 46 designs are the rows of `tools/DataImporter/Overrides/species-past-hundred.json`, made whole species of `species.json` (Nos. 2020 to 2146) by `tools/DataImporter/SpeciesPastHundred.cs` with their evolution rows on their forebears, their moves (the forebear's to 100, R31's past it) and their written Pokédex entries, every open question at its default (plan 03's D14 line has what was built and how they are counted). Its sculpts were built in wave 52 (`PokemonModels.Ours.Kanto.cs`, all 46). Not built yet: the National Pokédex's heading of their own, Romanian names. The format is 06 · R20b's ([R20b-mega-proposals.md](R20b-mega-proposals.md)).

Every name, type, ability, item, move, stat and sum in this document was checked by a script that only reads `PokemonPlatinumEngine/Data/species.json`, `abilities.json` and `items.json` (kept outside the repository; what it checks is listed under "How the numbers were checked", at the end).

## The shortlist: the twelve strongest

If the user wants a small first batch, these are the twelve to build. Each one fills a real gap (a weak total, a design that stops early, or a line whose own lore asks for a later stage), and each has a look that reads at once.

| | Ours | From | Typing | Total | Evolves | Why it leads |
|---|---|---|---|---|---|---|
| 1 | **Porrigard** | Farfetch'd | Normal/Flying | 377 → 457 | level 101 holding a Leek | the lowest total of any species alone in Kanto; the Galarian line moved on and Kanto's never did |
| 2 | **Kitsunox** | Ninetales | Fire/Ghost | 505 → 585 | level 120, friendship, at night | the fox that lives a thousand years is made for a level past 100 |
| 3 | **Solarion** | Rapidash | Fire/Flying | 500 → 580 | level 115, by day | the fire horse taking wing |
| 4 | **Ferrisle** | Lapras | Water/Ice | 535 → 600 | level 125, friendship | the beloved ferry has no line at all |
| 5 | **Ossugard** | Marowak | Ground/Ghost | 425 → 505 | level 105 holding a Thick Club | the line of the lost mother, closed |
| 6 | **Najarok** | Arbok | Poison/Dark | 448 → 528 | level 106 | the face on the hood wakes |
| 7 | **Cordyhusk** | Parasect | Grass/Ghost | 405 → 485 | level 110, at night | the mushroom takes its host whole |
| 8 | **Ratchisel** | Raticate | Normal/Steel | 413 → 493 | level 105 | the first route's rat, with steel teeth |
| 9 | **Pincerex** | Kingler | Water/Steel | 475 → 555 | level 110 | a crab of 130 Attack and nothing else, given a steel claw |
| 10 | **Azhdarch** | Aerodactyl | Rock/Flying | 515 → 595 | level 125 holding an Old Amber | the giant flying reptiles were real |
| 11 | **Ammonarch** | Omastar | Rock/Water | 495 → 575 | level 120 holding a Helix Fossil | the giant ammonites were real |
| 12 | **Eurypterox** | Kabutops | Rock/Water | 495 → 575 | level 120 holding a Dome Fossil | the sea scorpions were real |

## What qualifies, and how many

The plan's rule (D14; W4's decision table): a new evolution for every species **with no evolution at all**, and for **the last stage of every line of two**. A line of three gains new moves only (06 · R31), so its last stage is not here.

The script reads every species' `evolutions` and builds the lines. One choice had to be made: an evolution that starts from a **regional form** (`fromForm` Galar, Hisui, Paldea or Alola) belongs to that form's own line, not to the species. So Kanto's Farfetch'd is a species alone (only the Galarian one becomes Sirfetch'd), Kanto's Mr. Mime is the last of a line of two (Mime Jr. into Mr. Mime; only the Galarian one goes on to Mr. Rime), and Meowth into Persian is a line of two (the Galarian Meowth's Perrserker is its own). This is also how the engine reads them (`Evolution.FitsForm`: "a regional form evolves by nothing else").

**Kanto, National Pokédex 1 to 151: 49 species qualify.**

- **12 alone**: Farfetch'd, Kangaskhan, Pinsir, Tauros, Lapras, Ditto, Aerodactyl, Articuno, Zapdos, Moltres, Mewtwo, Mew (five of them legendary or mythical).
- **37 last of a line of two**: Raticate, Fearow, Arbok, Sandslash, Ninetales, Parasect, Venomoth, Dugtrio, Persian, Golduck, Arcanine, Tentacruel, Rapidash, Slowbro, Dodrio, Dewgong, Muk, Cloyster, Hypno, Kingler, Electrode, Exeggutor, Marowak, Hitmonlee, Hitmonchan, Weezing, Seaking, Starmie, Mr. Mime, Jynx, Gyarados, Vaporeon, Jolteon, Flareon, Omastar, Kabutops, Snorlax. Five of them come from a first stage of a later game (Tyrogue twice, Mime Jr., Smoochum, Munchlax) and are still lines of two.
- **Not here, last of a line of three (19)**: Venusaur, Charizard, Blastoise, Butterfree, Beedrill, Pidgeot, Raichu, Nidoqueen, Nidoking, Clefable, Wigglytuff, Vileplume, Poliwrath, Alakazam, Machamp, Victreebel, Golem, Gengar, Dragonite. They take R31's moves.
- **Not here, a Kanto species whose line ends in a later region**: Onix (Steelix), Scyther (Scizor, Kleavor), Slowpoke's other end (Slowking), Tangela (Tangrowth), Lickitung (Lickilicky) and Eevee's other five ends. Each is designed in the batch of the region where its last stage is numbered (Johto for Steelix, Scizor and Slowking; Sinnoh for Tangrowth, Lickilicky, Leafeon and Glaceon; and so on).

**The whole data, for the record.** By the same rule the National Pokédex has 215 species alone (84 of them legendary or mythical) and 240 last stages of lines of two (from 223 first stages, since Eevee and Tyrogue branch). Counting a regional form's evolution as the species' own gives 201 and 246. The plan's "205" and "221 lines" were rough counts written before this script; the later batches use the script's.

## How to read an entry

- **Name**: our own, one word of at most 12 letters (Crabominable's length, the longest in the data). The script holds every name against every species and form name in `species.json`, without case or punctuation, and against the others of the batch: none is taken. None is a real Pokémon's name of any game (the data has all 1,025 and every form).
- **No.**: a number of our own, **2000 + the number of the species it evolves from** (Raticate 20 gives 2020). It keeps them in National Pokédex order, leaves room for every future game's species (the National Pokédex will not reach 2000 for decades), and says at a glance what each one comes from. See the open questions.
- **Stats**: HP / Attack / Defense / Sp. Atk / Sp. Def / Speed, from the stats the game reads (the newest games' where `species.json` has a `modern` change: Arbok, Dugtrio, Farfetch'd, Dodrio, Electrode, Exeggutor). **The rise is +80, capped so that no species of ours that is not legendary passes 600** (the pseudo-legendaries' total, the highest any non-legendary, non-Mega species reaches; Slaking's 670 aside). 80 is the median of the 29 times a later game gave a species that was final a new last stage (Golbat into Crobat +80, Magneton into Magnezone +70, Rhydon into Rhyperior +50, Lickitung into Lickilicky +130; 20 to 150, mean 84.7; Scizor and Kleavor, which add nothing, left out). The cap trims the rise of nine: Arcanine (+45), Exeggutor (+70), Gyarados and Snorlax (+60), Lapras (+65), Cloyster, Vaporeon, Jolteon and Flareon (+75). The three birds rise +80 to 660, under Mewtwo's 680. HP may rise, unlike a Mega's. Each entry says where the rise goes and why.
- **Abilities**: only abilities in `abilities.json`, **as many slots as the species had**, in the same order, the hidden one last, because an evolving Pokémon keeps its slot (`docs/mechanics/evolution.md`: "first stays first, second stays second"). A new ability is in bold, the old ones as they were. **All of them run today**: `docs/mechanics/coverage.md` counts every one of the 313 abilities in the data as having its effect (R27 and R27b finished the later ones).
- **Evolution**: by level between 101 and 160, often with a second condition. **Every condition is one the engine already runs**: `Evolution.Meets` checks a row's `level` before anything else, so a level can be added to any method (friendship, night, day, rain, a held item, a known move, a place, an item used), and the row is written as the data already writes one (each entry gives it). No new method and no new code. A held item is used up, as for every "level up holding" evolution.
- **Levels**: Kanto's Champion is met at about 104 and Johto's at 136 (W4's fair climb, `docs/mechanics/rulings.md`, "The level cap past 100"). So 40 of the 46 evolve between 101 and 125, within Kanto's post-game or Johto's first half; the three strongest of Kanto's own (Arcanine, Gyarados, Snorlax) at 130, in Johto; the three birds at 150, in Hoenn. None past 160.
- **Look**: a short brief for the sculpt in the style guide's terms ("Pokémon (version 2, G7)"), starting from our own sculpt of the species (the summary over its build in `PokemonModels.*.cs`): the silhouette's change, the colours and one new feature. A species whose female looks different (Raticate, Dodrio, Hypno, Seaking, Gyarados) keeps that difference where the feature survives.
- **Say**: strong yes (the shortlist), yes, maybe (a design that already ends its line well, or a choice the user may not want), no.

## The legendaries, the mythical and Ditto

The user said on 2026-10-21 that only some legendaries and mythicals should evolve, each batch proposing which. Kanto has five, and one more species alone whose whole design is to have no stats of its own.

- **Articuno, Zapdos and Moltres: maybe, as a trio or not at all.** They are Kanto's post-game reward, and a team that crosses into later regions keeps them longer than any other legendary of the region. Grown together they read as one idea (the aurora, the thunderhead, the phoenix), each by holding its own type's item at level 150, which the climb reaches in Hoenn. Each keeps Pressure and takes a weather ability as its hidden one. +80 to 660 keeps them under Mewtwo's 680. Their Galarian forms don't evolve (the engine's rule above). *Default if the user doesn't say*: build them, since they are the one legendary family of Kanto that a long journey keeps using.
- **Mewtwo: no.** Already 680, the highest of Kanto, with two Megas. Anything above it is a cover legendary's territory, and the story of Mewtwo is that nothing made it stronger.
- **Mew: no.** It is the ancestor of every species, the root and not a stage; an evolution would say the opposite of its whole lore.
- **Ditto: no.** Transform copies the target's stats (all but HP), types, ability and moves, so a Ditto of 368 fights as a Ditto of 288. The only stat that would matter is HP, and a Ditto with more HP is a better breeding tool and nothing else. *Alternative*: an evolution whose ability is Imposter made certain and whose HP is high (a "mimic colony"), if the user wants one.

## The table: every qualifying species

The user may strike rows here before reading the designs. "No." is our number; "Total" is the stats the game reads now, and ours.

| # | Was | Ours | No. | Typing (was → ours) | Total | Evolves | Say |
|---|---|---|---|---|---|---|---|
| 1 | Raticate (20) | Ratchisel | 2020 | Normal → **Normal/Steel** | 413 → 493 | level 105 | strong yes: the weakest final stage of Kanto's routes, and the rat that gnaws through concrete has room to grow |
| 2 | Fearow (22) | Lancerow | 2022 | Normal/Flying, kept | 442 → 522 | level 104, holding a Scope Lens (used up) | yes: a route bird that Pidgeot outclasses; a critical-hit striker gives it a job |
| 3 | Arbok (24) | Najarok | 2024 | Poison → **Poison/Dark** | 448 → 528 | level 106 | strong yes: a classic design that stops early; the hood's face becoming real is a strong idea |
| 4 | Sandslash (28) | Sandspire | 2028 | Ground → **Ground/Rock** | 450 → 530 | level 108 | yes: a plain physical attacker that a sandstorm team would use; the spines turned to stone suit it |
| 5 | Ninetales (38) | Kitsunox | 2038 | Fire → **Fire/Ghost** | 505 → 585 | level 120, with friendship 220 or more, at night | strong yes: the fox that lives a thousand years is made for an evolution past 100 |
| 6 | Parasect (47) | Cordyhusk | 2047 | Bug/Grass → **Grass/Ghost** | 405 → 485 | level 110, at night | strong yes: the lowest total of Kanto's last stages (405), and the mushroom taking its host whole is its own story |
| 7 | Venomoth (49) | Somnimoth | 2049 | Bug/Poison → **Bug/Psychic** | 450 → 530 | level 112, at night | yes: a dust moth whose Psychic moves become its own; Quiver Dance makes it a real special sweeper |
| 8 | Dugtrio (51) | Molemount | 2051 | Ground → **Ground/Rock** | 425 → 505 | level 104 | yes: the frailest qualifier (35 HP); a hill of moles that finally shows more of itself |
| 9 | Persian (53) | Regalynx | 2053 | Normal → **Normal/Fairy** | 440 → 520 | level 110, holding an Amulet Coin (used up) | yes: the cat of coins gets a coin's evolution; Technician and Fake Out with real stats |
| 10 | Golduck (55) | Psyduke | 2055 | Water → **Water/Psychic** | 500 → 580 | level 112, knowing Zen Headbutt | yes: Psyduck's headache made a power at last; Golduck's Psychic moves without the type |
| 11 | Arcanine (59) | Kirinine | 2059 | Fire → **Fire/Dragon** | 555 → 600 | level 130, with friendship 220 or more | maybe: already called the Legendary Pokémon (555); the line ends well, so only if Kanto should have a beast of legend |
| 12 | Tentacruel (73) | Medusarch | 2073 | Water/Poison, kept | 515 → 595 | level 115, holding a Poison Barb (used up) | yes: a sea that every surfer crosses is full of them; a colony jellyfish fits a level past 100 |
| 13 | Rapidash (78) | Solarion | 2078 | Fire → **Fire/Flying** | 500 → 580 | level 115, by day | strong yes: the fire horse taking wing is the most wanted evolution of the batch |
| 14 | Slowbro (80) | Slowmantle | 2080 | Water/Psychic, kept | 490 → 570 | level 120, holding a Big Pearl (used up) | maybe: the line already branches (Slowking) and has a Mega; this would be a third end |
| 15 | Farfetch'd (83) | Porrigard | 2083 | Normal/Flying, kept | 377 → 457 | level 101, holding a Leek (used up) | strong yes: the lowest total of any species alone in Kanto (377), and the one the Galarian line left behind |
| 16 | Dodrio (85) | Tripluma | 2085 | Normal/Flying, kept | 470 → 550 | level 108 | maybe: three heads is already the joke; a fourth would be a worse one. Proposed with crests instead |
| 17 | Dewgong (87) | Glacitusk | 2087 | Water/Ice, kept | 475 → 555 | level 112, holding a Never-Melt Ice (used up) | yes: a sea lion that grows into a walrus with a narwhal's tusk; physical where Lapras is special |
| 18 | Muk (89) | Sludgemire | 2089 | Poison → **Poison/Ground** | 500 → 580 | level 115 | yes: a heavy sludge thickens into a tar pit, and Poison/Ground gives Kanto's sludge the bulk and the Earthquake it never had |
| 19 | Cloyster (91) | Clamspire | 2091 | Water/Ice, kept | 525 → 600 | level 120, holding a Shell Bell (used up) | maybe: Shell Smash and Skill Link already make it strong (525); a rise to 600 would go mostly into what it lacks |
| 20 | Hypno (97) | Mesmeryx | 2097 | Psychic → **Psychic/Dark** | 483 → 563 | level 110, at night | yes: a hypnotist who works at night; a special attacker where Hypno had only bulk |
| 21 | Kingler (99) | Pincerex | 2099 | Water → **Water/Steel** | 475 → 555 | level 110 | strong yes: Kanto's crab with 130 Attack and nothing else; a siege claw of steel fits it |
| 22 | Electrode (101) | Dynamorb | 2101 | Electric → **Electric/Steel** | 490 → 570 | level 105, at a Magnetic Field (Mt. Coronet's) | maybe: a ball that already is very fast; a design-light line, so a maybe |
| 23 | Exeggutor (103) | Palmagus | 2103 | Grass/Psychic, kept | 530 → 600 | a Leaf Stone used on it at level 118 or above | maybe: 530 and two looks already (the Alolan one); a grove of palms is a fair idea, but the line ends well |
| 24 | Marowak (105) | Ossugard | 2105 | Ground → **Ground/Ghost** | 425 → 505 | level 105, holding a Thick Club (used up) | strong yes: the line whose story is a lost mother; a warden of the dead's bones closes it |
| 25 | Hitmonlee (106) | Savateur | 2106 | Fighting → **Fighting/Flying** | 455 → 535 | level 110, holding a Black Belt (used up) | yes: a kicker with 53 Defense; its flying kicks become its type |
| 26 | Hitmonchan (107) | Pugilord | 2107 | Fighting → **Fighting/Steel** | 455 → 535 | level 110, holding a Punching Glove (used up) | yes: the boxer becomes a champion; Iron Fist with steel gloves |
| 27 | Weezing (110) | Smoglom | 2110 | Poison → **Poison/Fire** | 490 → 570 | level 112 | maybe: the Galarian form already gave it a new look; Poison/Fire is new for it |
| 28 | Kangaskhan (115) | Matriroo | 2115 | Normal → **Normal/Fighting** | 490 → 570 | level 115, with friendship 220 or more | maybe: its Mega already shows the grown child; an evolution would repeat it |
| 29 | Seaking (119) | Sabrefin | 2119 | Water → **Water/Fairy** | 450 → 530 | level 106 | yes: a goldfish that every rod finds; a unicorn of the sea has a place |
| 30 | Starmie (121) | Stellamir | 2121 | Water/Psychic, kept | 520 → 600 | level 120, at night | maybe: already strong (520) and a Mega in the newest game; only if a third stage of stars is wanted |
| 31 | Mr. Mime (122) | Mimestro | 2122 | Psychic/Fairy, kept | 460 → 540 | level 108, knowing Reflect | maybe: the Galarian line already goes on to Mr. Rime; a maestro of walls is a fair end for Kanto's |
| 32 | Jynx (124) | Aurorelle | 2124 | Ice/Psychic, kept | 455 → 535 | level 112, at night | maybe: the design itself is the question; proposed as a singer of the northern lights, a new look rather than a bigger Jynx |
| 33 | Pinsir (127) | Mandibron | 2127 | Bug → **Bug/Dark** | 500 → 580 | level 115, at night | yes: a stag beetle alone in its line; Bug/Dark is a typing almost nobody has |
| 34 | Tauros (128) | Bovarch | 2128 | Normal → **Normal/Ground** | 490 → 570 | level 115 | yes: the bull that leads the herd; Kanto's only species of its kind |
| 35 | Gyarados (130) | Typhonarch | 2130 | Water/Flying → **Water/Dragon** | 540 → 600 | level 130, in the rain | maybe: its line ends in the most famous evolution there is, and it has a Mega; the dragon it looks like is the obvious step |
| 36 | Lapras (131) | Ferrisle | 2131 | Water/Ice, kept | 535 → 600 | level 125, with friendship 220 or more | strong yes: the beloved ferry has no line at all; a ship of the sea that carries a village fits it |
| 37 | Vaporeon (134) | Naiadeon | 2134 | Water, kept | 525 → 600 | level 120, holding a Water Stone, with friendship 220 or more (used up) | maybe: proposed with Jolteon and Flareon as a trio, or not at all; Eevee's other five wait for their batches |
| 38 | Jolteon (135) | Fulgureon | 2135 | Electric, kept | 525 → 600 | level 120, holding a Thunder Stone, with friendship 220 or more (used up) | maybe: with the trio |
| 39 | Flareon (136) | Pyreon | 2136 | Fire, kept | 525 → 600 | level 120, holding a Fire Stone, with friendship 220 or more (used up) | maybe: with the trio |
| 40 | Omastar (139) | Ammonarch | 2139 | Rock/Water, kept | 495 → 575 | level 120, holding a Helix Fossil (used up) | strong yes: the fossils are rare and the ammonite's giant cousins were real; its own Helix Fossil wakes it |
| 41 | Kabutops (141) | Eurypterox | 2141 | Rock/Water, kept | 495 → 575 | level 120, holding a Dome Fossil (used up) | strong yes: the sea scorpions of the old seas were real and huge; the scythes and a tail make one |
| 42 | Aerodactyl (142) | Azhdarch | 2142 | Rock/Flying, kept | 515 → 595 | level 125, holding an Old Amber (used up) | strong yes: the giant flying reptiles were real, and the Old Amber it came from wakes it again |
| 43 | Snorlax (143) | Dozemount | 2143 | Normal → **Normal/Grass** | 540 → 600 | level 130, with friendship 220 or more | maybe: Snorlax already ends well and has a Gigantamax form; a forest growing on a sleeper is a fair idea |
| 44 | Articuno (144) | Glacirune | 2144 | Ice/Flying, kept | 580 → 660 | level 150, holding a Never-Melt Ice (used up) | maybe: the three birds as a trio or not at all |
| 45 | Zapdos (145) | Fulguris | 2145 | Electric/Flying, kept | 580 → 660 | level 150, holding a Magnet (used up) | maybe: with the trio |
| 46 | Moltres (146) | Pyrevant | 2146 | Fire/Flying, kept | 580 → 660 | level 150, holding a Charcoal (used up) | maybe: with the trio |
| – | Ditto (132) | none | – | Normal | 288 | – | no: Transform copies everything but HP, so an evolution changes nothing in battle |
| – | Mewtwo (150) | none | – | Psychic | 680 | – | no: already Kanto's highest, with two Megas |
| – | Mew (151) | none | – | Psychic | 600 | – | no: the ancestor of every species is the root, not a stage |

In all: 12 strong yes, 15 yes, 19 maybe (the three birds and the three Eeveelutions among them, each as a set) and 3 no.

## The designs

In National Pokédex order. Each entry: the typing and why, the category word, number, size, the stats (the species' as the game reads them → ours), the abilities, the evolution with the row it would be in `species.json`, and the look.

### 1. Ratchisel, from Raticate (20): strong yes

- **Typing**: Normal → **Normal/Steel**. Its incisors turn to steel, as the Pokédex has always said they gnaw through anything; Steel gives the frail rat ten resistances and an immunity to Poison, at the cost of a four-times weakness to Fighting. *Alternative*: Normal/Ground, a burrower (*the Warren Pokémon*).
- **The Gnawer Pokémon**, No. 2020. 0.9 m, 38.0 kg (Raticate 0.7 m, 18.5 kg).
- **Stats**: 55 / 81 / 60 / 50 / 70 / 97 = 413 → **70 / 106 / 85 / 50 / 75 / 107 = 493** (+15 HP +25 Atk +25 Def +5 SpD +10 Spe).
- **Abilities**: **Strong Jaw**, Guts, hidden Hustle (was Run Away, Guts, hidden Hustle).
- **Evolution**: Raticate at level 105. Data: `{"targetSpecies":"Ratchisel","level":105}`.
- **Look**: from our heavy rat sitting up on its hind feet: lower and longer, on all fours, its brown fur grizzled grey along the back and its whiskers kept. The two buck teeth grow into broad chisels of `Metal`, and the scaly tail thickens with three steel-grey rings. **New feature**: the chisel teeth, square at the ends.

### 2. Lancerow, from Fearow (22): yes

- **Typing**: Normal/Flying, kept. A Kanto route bird stays one. *Alternative*: Flying/Fighting, a duellist (Hawlucha's typing).
- **The Lance Beak Pokémon**, No. 2022. 1.6 m, 52.0 kg (Fearow 1.2 m, 38.0 kg).
- **Stats**: 65 / 90 / 65 / 61 / 61 / 100 = 442 → **80 / 125 / 75 / 61 / 66 / 115 = 522** (+15 HP +35 Atk +10 Def +5 SpD +15 Spe).
- **Abilities**: **Super Luck**, hidden Sniper (was Keen Eye, hidden Sniper).
- **Evolution**: Fearow at level 104, holding a Scope Lens (used up). Data: `{"targetSpecies":"Lancerow","method":"LevelHoldingItem","level":104,"item":"Scope Lens"}`.
- **Look**: from our Fearow: the long beak lengthens into a lance with a ring of hard `Shell` where it meets the face, the red crest sweeps back into three long plumes, and the wings broaden with pale bars across the flight feathers. **New feature**: the lance beak, half again as long as the head.

### 3. Najarok, from Arbok (24): strong yes

- **Typing**: Poison → **Poison/Dark**. The face on its hood wakes and stares; Dark gives Sucker Punch and Crunch the same-type bonus, makes it immune to Psychic and leaves Ground its one weakness. *Alternative*: keep Poison alone.
- **The King Cobra Pokémon**, No. 2024. 5.0 m, 110.0 kg (Arbok 3.5 m, 65.0 kg).
- **Stats**: 60 / 95 / 69 / 65 / 79 / 80 = 448 → **80 / 120 / 84 / 65 / 89 / 90 = 528** (+20 HP +25 Atk +15 Def +10 SpD +10 Spe).
- **Abilities**: Intimidate, Shed Skin, hidden **Merciless** (was Intimidate, Shed Skin, hidden Unnerve).
- **Evolution**: Arbok at level 106. Data: `{"targetSpecies":"Najarok","level":106}`.
- **Look**: from our Arbok: longer and heavier, the purple deepened toward violet with thin gold bands down the body. The hood widens into a crown-shaped frill, and the pattern on it becomes a second face whose two eyes are decals that open and blink. **New feature**: the waking face on the hood.

### 4. Sandspire, from Sandslash (28): yes

- **Typing**: Ground → **Ground/Rock**. Its quills harden into spines of rock; Ground/Rock is Rhydon's and Golem's typing, which the desert pangolin fits, with four-times weaknesses to Water and Grass. *Alternative*: Ground/Steel (Excadrill's typing).
- **The Rampart Pokémon**, No. 2028. 1.4 m, 55.0 kg (Sandslash 1.0 m, 29.5 kg).
- **Stats**: 75 / 100 / 110 / 45 / 55 / 65 = 450 → **85 / 125 / 130 / 45 / 70 / 75 = 530** (+10 HP +25 Atk +20 Def +15 SpD +10 Spe).
- **Abilities**: **Rough Skin**, hidden Sand Rush (was Sand Veil, hidden Sand Rush).
- **Evolution**: Sandslash at level 108. Data: `{"targetSpecies":"Sandspire","level":108}`.
- **Look**: from our Sandslash, hunched forward with its two great claws: the quills on its back become fewer, taller spines of sandstone (`Shell`, banded in two browns), the claws thicken into picks, and a plate of rock covers its brow. **New feature**: the ridge of sandstone spines, tallest over the shoulders.

### 5. Kitsunox, from Ninetales (38): strong yes

- **Typing**: Fire → **Fire/Ghost**. The Pokédex's curse that lasts a thousand years; Ghost makes it immune to Normal and Fighting and gives Hex and Shadow Ball. *Alternative*: Fire/Psychic (a fox of divination, Delphox's typing).
- **The Millennium Pokémon**, No. 2038. 1.4 m, 26.0 kg (Ninetales 1.1 m, 19.9 kg).
- **Stats**: 73 / 76 / 75 / 81 / 100 / 100 = 505 → **88 / 76 / 75 / 121 / 110 / 115 = 585** (+15 HP +40 SpA +10 SpD +15 Spe).
- **Abilities**: **Cursed Body**, hidden Drought (was Flash Fire, hidden Drought).
- **Evolution**: Ninetales at level 120, with friendship 220 or more, at night. Data: `{"targetSpecies":"Kitsunox","method":"FriendshipNight","level":120}`.
- **Look**: from our slender fox standing tall: its gold coat pales to cream, and the nine tails grow longer and wispier, each ending in a pale violet flame (`Glow`). The ruff becomes a mantle, and a small pale flame floats at the brow on a thin strand. **New feature**: the violet flames at the nine tails' ends.

### 6. Cordyhusk, from Parasect (47): strong yes

- **Typing**: Bug/Grass → **Grass/Ghost**. The bug beneath has died and the mushroom walks it, as the Pokédex hints; Bug goes, Ghost comes, and its four-times weaknesses to Fire and Flying become twice. *Alternative*: Grass/Poison, a toxic fungus.
- **The Fungus Pokémon**, No. 2047. 1.6 m, 45.0 kg (Parasect 1.0 m, 29.5 kg).
- **Stats**: 60 / 95 / 80 / 60 / 80 / 30 = 405 → **90 / 105 / 95 / 65 / 100 / 30 = 485** (+30 HP +10 Atk +15 Def +5 SpA +20 SpD).
- **Abilities**: Effect Spore, Dry Skin, hidden **Perish Body** (was Effect Spore, Dry Skin, hidden Damp).
- **Evolution**: Parasect at level 110, at night. Data: `{"targetSpecies":"Cordyhusk","method":"LevelNight","level":110}`.
- **Look**: from our Parasect: the mushroom swells into a broad parasol that hangs over the body like a cloak, its red cap spotted with pale dots that glow at night (`Glow`); threads of white mycelium hang from the rim. The bug beneath is a pale, hollow husk with its eyes painted out. **New feature**: the threads hanging from the cap's rim.

### 7. Somnimoth, from Venomoth (49): yes

- **Typing**: Bug/Poison → **Bug/Psychic**. The moth's compound eyes already read minds in its moves; Psychic over Poison. *Alternative*: keep Bug/Poison.
- **The Dust Moth Pokémon**, No. 2049. 1.8 m, 18.0 kg (Venomoth 1.5 m, 12.5 kg).
- **Stats**: 70 / 65 / 60 / 90 / 75 / 90 = 450 → **80 / 65 / 65 / 120 / 90 / 110 = 530** (+10 HP +5 Def +30 SpA +15 SpD +20 Spe).
- **Abilities**: Shield Dust, Tinted Lens, hidden Wonder Skin (was Shield Dust, Tinted Lens, hidden Wonder Skin).
- **Evolution**: Venomoth at level 112, at night. Data: `{"targetSpecies":"Somnimoth","method":"LevelNight","level":112}`.
- **Look**: from our Venomoth: the four wings grow wider, the lower pair with tails like a luna moth's, their purple paled to lilac with an eyespot on each (a decal). Its fuzzy body thickens, and the antennae branch like ferns. **New feature**: the long tails of the lower wings.

### 8. Molemount, from Dugtrio (51): yes

- **Typing**: Ground → **Ground/Rock**. The hill they live in comes up with them; Rock for the boulders on its crown. *Alternative*: keep Ground alone.
- **The Mole Hill Pokémon**, No. 2051. 1.2 m, 120.0 kg (Dugtrio 0.7 m, 33.3 kg).
- **Stats**: 35 / 100 / 50 / 50 / 70 / 120 = 425 → **65 / 120 / 70 / 50 / 75 / 125 = 505** (+30 HP +20 Atk +20 Def +5 SpD +5 Spe).
- **Abilities**: Sand Veil, Arena Trap, hidden Sand Force (was Sand Veil, Arena Trap, hidden Sand Force).
- **Evolution**: Dugtrio at level 104. Data: `{"targetSpecies":"Molemount","level":104}`.
- **Look**: from our Dugtrio: five heads in a ring where there were three, the middle one the tallest, all standing out of one wide mound of earth that now shows (a flattened ellipsoid of brown, paint for the dirt) with boulders set in its slopes (`Shell`, grey). The pink noses are kept. **New feature**: the mound itself, which the three never showed.

### 9. Regalynx, from Persian (53): yes

- **Typing**: Normal → **Normal/Fairy**. Charm over menace (the Alolan Persian is already the Dark one); Fairy makes it immune to Dragon and hits Fighting and Dark. *Alternative*: Normal/Psychic, the gem's power.
- **The Royal Cat Pokémon**, No. 2053. 1.3 m, 45.0 kg (Persian 1.0 m, 32.0 kg).
- **Stats**: 65 / 70 / 60 / 65 / 65 / 115 = 440 → **80 / 95 / 70 / 65 / 80 / 130 = 520** (+15 HP +25 Atk +10 Def +15 SpD +15 Spe).
- **Abilities**: Limber, Technician, hidden Unnerve (was Limber, Technician, hidden Unnerve).
- **Evolution**: Persian at level 110, holding an Amulet Coin (used up). Data: `{"targetSpecies":"Regalynx","method":"LevelHoldingItem","level":110,"item":"Amulet Coin"}`.
- **Look**: from our sleek cat on four long legs: longer and leaner, the cream coat brushed with gold at the paws and tail, a mane of soft fur at the chest. The red gem on the brow grows into a crown of three gems (`Metal` setting), and the curled tail ends in a tuft. **New feature**: the crown of three gems.

### 10. Psyduke, from Golduck (55): yes

- **Typing**: Water → **Water/Psychic**. Golduck has always used Psychic moves without the type; the gem on its brow gives it. *Alternative*: keep Water alone.
- **The Diviner Pokémon**, No. 2055. 2.0 m, 95.0 kg (Golduck 1.7 m, 76.6 kg).
- **Stats**: 80 / 82 / 78 / 95 / 80 / 85 = 500 → **90 / 82 / 88 / 125 / 95 / 100 = 580** (+10 HP +10 Def +30 SpA +15 SpD +15 Spe).
- **Abilities**: Damp, Cloud Nine, hidden Swift Swim (was Damp, Cloud Nine, hidden Swift Swim).
- **Evolution**: Golduck at level 112, knowing Zen Headbutt. Data: `{"targetSpecies":"Psyduke","method":"LevelKnowsMove","level":112,"move":"Zen Headbutt"}`.
- **Look**: from our Golduck: taller and leaner, the blue deepened, the bill longer, and the red gem on the brow grown into a third eye that glows (`Glow`, pale red). Webbed fins spread from its forearms, and a pale ring of light floats round its head on thin strands. **New feature**: the glowing third eye.

### 11. Kirinine, from Arcanine (59): maybe

- **Typing**: Fire → **Fire/Dragon**. The beast of old stories it is named after; Dragon makes Water moves neutral and keeps its resistances to Fire, Grass and Electric; Ground, Rock and Dragon hit it hard. *Alternative*: keep Fire alone.
- **The Legend Pokémon**, No. 2059. 2.4 m, 220.0 kg (Arcanine 1.9 m, 155.0 kg).
- **Stats**: 90 / 110 / 80 / 100 / 80 / 95 = 555 → **90 / 125 / 90 / 100 / 90 / 105 = 600** (+15 Atk +10 Def +10 SpD +10 Spe).
- **Abilities**: Intimidate, Flash Fire, hidden Justified (was Intimidate, Flash Fire, hidden Justified).
- **Evolution**: Arcanine at level 130, with friendship 220 or more. Data: `{"targetSpecies":"Kirinine","method":"Friendship","level":130}`.
- **Look**: from our great striped dog under its heavy mane: larger, the mane flowing longer like a lion dance's, the black stripes curling into scrolls, two short horns of `Shell` swept back from the brow and a long beard of cream fur. **New feature**: the two horns.

### 12. Medusarch, from Tentacruel (73): yes

- **Typing**: Water/Poison, kept. The jellyfish's typing is its whole character. *Alternative*: Water/Electric, a jellyfish that stings with current.
- **The Man-o'-War Pokémon**, No. 2073. 3.0 m, 90.0 kg (Tentacruel 1.6 m, 55.0 kg).
- **Stats**: 80 / 70 / 65 / 80 / 120 / 100 = 515 → **100 / 70 / 85 / 100 / 130 / 110 = 595** (+20 HP +20 Def +20 SpA +10 SpD +10 Spe).
- **Abilities**: Clear Body, Liquid Ooze, hidden Rain Dish (was Clear Body, Liquid Ooze, hidden Rain Dish).
- **Evolution**: Tentacruel at level 115, holding a Poison Barb (used up). Data: `{"targetSpecies":"Medusarch","method":"LevelHoldingItem","level":115,"item":"Poison Barb"}`.
- **Look**: from our Tentacruel: the bell becomes a tall sail-like float (the Portuguese man-o'-war's), blue to pale violet, with the two red gems kept as three. The tentacles double in number and length, trailing in a cluster beneath. **New feature**: the sail rising from the float.

### 13. Solarion, from Rapidash (78): strong yes

- **Typing**: Fire → **Fire/Flying**. The horse that runs at the sun's pace takes to the sky; Flying gives Brave Bird beside Flare Blitz, with a four-times weakness to Rock. *Alternative*: Fire/Fairy.
- **The Sun Steed Pokémon**, No. 2078. 2.2 m, 130.0 kg (Rapidash 1.7 m, 95.0 kg).
- **Stats**: 65 / 100 / 70 / 80 / 80 / 105 = 500 → **80 / 120 / 80 / 80 / 95 / 125 = 580** (+15 HP +20 Atk +10 Def +15 SpD +20 Spe).
- **Abilities**: **Reckless**, Flash Fire, hidden Flame Body (was Run Away, Flash Fire, hidden Flame Body).
- **Evolution**: Rapidash at level 115, by day. Data: `{"targetSpecies":"Solarion","method":"LevelDay","level":115}`.
- **Look**: from our Rapidash: taller and more muscular, its single horn longer and spiralled, and from its shoulders spread two wings of flame (`Glow`, gold at the heart and orange at the edge). The mane and tail stream further, and the hooves leave flames. **New feature**: the two wings of flame.

### 14. Slowmantle, from Slowbro (80): maybe

- **Typing**: Water/Psychic, kept. *Alternative*: Water/Steel, the shell become armour.
- **The Hermit Pokémon**, No. 2080. 2.0 m, 140.0 kg (Slowbro 1.6 m, 78.5 kg).
- **Stats**: 95 / 75 / 110 / 100 / 80 / 30 = 490 → **115 / 75 / 130 / 115 / 105 / 30 = 570** (+20 HP +20 Def +15 SpA +25 SpD).
- **Abilities**: Oblivious, Own Tempo, hidden Regenerator (was Oblivious, Own Tempo, hidden Regenerator).
- **Evolution**: Slowbro at level 120, holding a Big Pearl (used up). Data: `{"targetSpecies":"Slowmantle","method":"LevelHoldingItem","level":120,"item":"Big Pearl"}`.
- **Look**: from our standing Slowbro: the Shellder on its tail grows into a great spiral shell that covers its back like a hermit crab's house, grey with pink lines, and the Slowbro sits more inside it. A pearl gleams in the shell's mouth (`Shell`, white). **New feature**: the spiral shell over its back.

### 15. Porrigard, from Farfetch'd (83): strong yes

- **Typing**: Normal/Flying, kept. The Galarian form already went to Fighting with Sirfetch'd. *Alternative*: Grass/Flying, the leek grown into a living staff.
- **The Leek Warden Pokémon**, No. 2083. 1.2 m, 32.0 kg (Farfetch'd 0.8 m, 15.0 kg).
- **Stats**: 52 / 90 / 55 / 58 / 62 / 60 = 377 → **67 / 115 / 65 / 58 / 67 / 85 = 457** (+15 HP +25 Atk +10 Def +5 SpD +25 Spe).
- **Abilities**: Keen Eye, Inner Focus, hidden Defiant (was Keen Eye, Inner Focus, hidden Defiant).
- **Evolution**: Farfetch'd at level 101, holding a Leek (used up). Data: `{"targetSpecies":"Porrigard","method":"LevelHoldingItem","level":101,"item":"Leek"}`.
- **Look**: from our plump duck with its flat yellow bill and dark brows: taller and leaner, a white feather cape at its shoulders, the leek grown into a long staff tipped with a bundle of green leaves (a thing carried, on the bone of the wing that holds it), and a few grey feathers in the brows. **New feature**: the leek staff.

### 16. Tripluma, from Dodrio (85): maybe

- **Typing**: Normal/Flying, kept. *Alternative*: Normal/Fighting, a runner that kicks.
- **The Triple Plume Pokémon**, No. 2085. 2.2 m, 110.0 kg (Dodrio 1.8 m, 85.2 kg).
- **Stats**: 60 / 110 / 70 / 60 / 60 / 110 = 470 → **80 / 130 / 80 / 60 / 70 / 130 = 550** (+20 HP +20 Atk +10 Def +10 SpD +20 Spe).
- **Abilities**: Run Away, Early Bird, hidden Tangled Feet (was Run Away, Early Bird, hidden Tangled Feet).
- **Evolution**: Dodrio at level 108. Data: `{"targetSpecies":"Tripluma","level":108}`.
- **Look**: from our Dodrio: each head grows a crest of plumes in its own colour (red, gold, white), the body grows a small fan of tail feathers that it raises like a peacock's, and the legs thicken. The female's beige necks are kept. **New feature**: the three crests.

### 17. Glacitusk, from Dewgong (87): yes

- **Typing**: Water/Ice, kept. *Alternative*: Water/Fairy.
- **The Ice Floe Pokémon**, No. 2087. 2.4 m, 220.0 kg (Dewgong 1.7 m, 120.0 kg).
- **Stats**: 90 / 70 / 80 / 70 / 95 / 70 = 475 → **110 / 90 / 95 / 70 / 105 / 85 = 555** (+20 HP +20 Atk +15 Def +10 SpD +15 Spe).
- **Abilities**: Thick Fat, Hydration, hidden Ice Body (was Thick Fat, Hydration, hidden Ice Body).
- **Evolution**: Dewgong at level 112, holding a Never-Melt Ice (used up). Data: `{"targetSpecies":"Glacitusk","method":"LevelHoldingItem","level":112,"item":"Never-Melt Ice"}`.
- **Look**: from our Dewgong: bigger and heavier, its white coat lightly blue at the flippers, the single horn grown into a long spiralled tusk (`Shell`, ivory) and two short tusks below the mouth, and a mantle of thick fur at the neck. **New feature**: the spiral tusk.

### 18. Sludgemire, from Muk (89): yes

- **Typing**: Poison → **Poison/Ground**. The sludge thickens into tar and swallows the ground under it; Ground makes it immune to Electric. *Alternative*: Poison/Steel, a heap of scrap.
- **The Tar Pit Pokémon**, No. 2089. 1.8 m, 180.0 kg (Muk 1.2 m, 30.0 kg).
- **Stats**: 105 / 105 / 75 / 65 / 100 / 50 = 500 → **125 / 125 / 100 / 65 / 110 / 55 = 580** (+20 HP +20 Atk +25 Def +10 SpD +5 Spe).
- **Abilities**: Stench, Sticky Hold, hidden Poison Touch (was Stench, Sticky Hold, hidden Poison Touch).
- **Evolution**: Muk at level 115. Data: `{"targetSpecies":"Sludgemire","level":115}`.
- **Look**: from our great low mound with a head leaning forward: darker, black purple with a gloss (`Scales`) and bubbles on its surface, bones and rocks half sunk in it. Its arms are thicker and drip. **New feature**: the old things half sunk in the tar.

### 19. Clamspire, from Cloyster (91): maybe

- **Typing**: Water/Ice, kept. *Alternative*: Water/Rock, a reef.
- **The Reef Pokémon**, No. 2091. 2.0 m, 220.0 kg (Cloyster 1.5 m, 132.5 kg).
- **Stats**: 50 / 95 / 180 / 85 / 45 / 70 = 525 → **75 / 105 / 180 / 85 / 75 / 80 = 600** (+25 HP +10 Atk +30 SpD +10 Spe).
- **Abilities**: Shell Armor, Skill Link, hidden Overcoat (was Shell Armor, Skill Link, hidden Overcoat).
- **Evolution**: Cloyster at level 120, holding a Shell Bell (used up). Data: `{"targetSpecies":"Clamspire","method":"LevelHoldingItem","level":120,"item":"Shell Bell"}`.
- **Look**: from our Cloyster: the spiked shell grows into a spire of shells grown together like a reef, with smaller shells on its slopes, pale violet with white spikes. The pearl face inside is kept, now with a second, smaller face in a side shell. **New feature**: the spire of shells.

### 20. Mesmeryx, from Hypno (97): yes

- **Typing**: Psychic → **Psychic/Dark**. It eats dreams by night; Dark makes it immune to Psychic and its Ghost and Dark weaknesses neutral, at the cost of a four-times weakness to Bug. *Alternative*: Psychic/Ghost.
- **The Trance Pokémon**, No. 2097. 1.9 m, 90.0 kg (Hypno 1.6 m, 75.6 kg).
- **Stats**: 85 / 73 / 70 / 73 / 115 / 67 = 483 → **95 / 73 / 80 / 113 / 125 / 77 = 563** (+10 HP +10 Def +40 SpA +10 SpD +10 Spe).
- **Abilities**: Insomnia, Forewarn, hidden Inner Focus (was Insomnia, Forewarn, hidden Inner Focus).
- **Evolution**: Hypno at level 110, at night. Data: `{"targetSpecies":"Mesmeryx","method":"LevelNight","level":110}`.
- **Look**: from our Hypno: taller and thinner, its yellow paled and its white collar grown into a high ruff (the female's fuller still). The pendulum becomes a ring of three coins circling on a thin chain round its hand. **New feature**: the circling coins.

### 21. Pincerex, from Kingler (99): strong yes

- **Typing**: Water → **Water/Steel**. The big claw tempers into steel; Steel gives Iron Head and Meteor Mash and many resistances. *Alternative*: Water/Fighting.
- **The Pincer King Pokémon**, No. 2099. 1.8 m, 110.0 kg (Kingler 1.3 m, 60.0 kg).
- **Stats**: 55 / 130 / 115 / 50 / 50 / 75 = 475 → **80 / 150 / 120 / 50 / 70 / 85 = 555** (+25 HP +20 Atk +5 Def +20 SpD +10 Spe).
- **Abilities**: Hyper Cutter, Shell Armor, hidden Sheer Force (was Hyper Cutter, Shell Armor, hidden Sheer Force).
- **Evolution**: Kingler at level 110. Data: `{"targetSpecies":"Pincerex","level":110}`.
- **Look**: from our Kingler: the big claw grows larger still and turns steel grey (`Metal`) with a red rim, the small claw a little bigger, the shell darker red with spikes along its edge. A crown of five short spikes on its head. **New feature**: the steel claw.

### 22. Dynamorb, from Electrode (101): maybe

- **Typing**: Electric → **Electric/Steel**. Its shell turns to metal where the magnetic field is, as Magnezone's does, and takes Magnezone's typing with its four-times weakness to Ground. *Alternative*: keep Electric alone.
- **The Ball Pokémon**, No. 2101. 1.6 m, 130.0 kg (Electrode 1.2 m, 66.6 kg).
- **Stats**: 60 / 50 / 70 / 80 / 80 / 150 = 490 → **80 / 50 / 80 / 115 / 95 / 150 = 570** (+20 HP +10 Def +35 SpA +15 SpD).
- **Abilities**: Soundproof, Static, hidden Aftermath (was Soundproof, Static, hidden Aftermath).
- **Evolution**: Electrode at level 105, at a Magnetic Field (Mt. Coronet's). Data: `{"targetSpecies":"Dynamorb","method":"LevelAtLocation","level":105,"location":"Magnetic Field"}`.
- **Look**: from our Electrode: larger, its red and white halves divided by a band of steel (`Metal`) with three studs, and two arcs of current (`Glow`, pale yellow) circling it on thin strands. **New feature**: the steel band.

### 23. Palmagus, from Exeggutor (103): maybe

- **Typing**: Grass/Psychic, kept. *Alternative*: Grass/Fairy.
- **The Grove Pokémon**, No. 2103. 3.0 m, 200.0 kg (Exeggutor 2.0 m, 120.0 kg).
- **Stats**: 95 / 95 / 85 / 125 / 75 / 55 = 530 → **110 / 95 / 100 / 140 / 100 / 55 = 600** (+15 HP +15 Def +15 SpA +25 SpD).
- **Abilities**: Chlorophyll, hidden Harvest (was Chlorophyll, hidden Harvest).
- **Evolution**: Exeggutor, with a Leaf Stone used on it at level 118 or above. Data: `{"targetSpecies":"Palmagus","method":"UseItem","level":118,"item":"Leaf Stone"}`.
- **Look**: from our palm tree on stubby legs: two trunks rise from one root, each with a crown of leaves and three coconut heads, and a fourth head grows low at the join. **New feature**: the second trunk.

### 24. Ossugard, from Marowak (105): strong yes

- **Typing**: Ground → **Ground/Ghost**. The Alolan Marowak's ghost fire made its own; Ground/Ghost hits hard and is immune to Normal, Fighting and Electric. *Alternative*: Ground/Fighting.
- **The Bone Warden Pokémon**, No. 2105. 1.4 m, 60.0 kg (Marowak 1.0 m, 45.0 kg).
- **Stats**: 60 / 80 / 110 / 50 / 80 / 45 = 425 → **80 / 110 / 115 / 50 / 90 / 60 = 505** (+20 HP +30 Atk +5 Def +10 SpD +15 Spe).
- **Abilities**: Rock Head, Lightning Rod, hidden Battle Armor (was Rock Head, Lightning Rod, hidden Battle Armor).
- **Evolution**: Marowak at level 105, holding a Thick Club (used up). Data: `{"targetSpecies":"Ossugard","method":"LevelHoldingItem","level":105,"item":"Thick Club"}`.
- **Look**: from our Marowak, taller with its bone raised over its shoulder: the skull helmet grows into a great horned skull, the club becomes a long bone staff with a skull at its head, and a pale blue flame (`Glow`) burns in the skull's eyes. **New feature**: the bone staff.

### 25. Savateur, from Hitmonlee (106): yes

- **Typing**: Fighting → **Fighting/Flying**. Its flying kicks; Flying gives Bounce and Brave Bird and takes Ground away. *Alternative*: keep Fighting alone.
- **The Savate Pokémon**, No. 2106. 1.8 m, 60.0 kg (Hitmonlee 1.5 m, 49.8 kg).
- **Stats**: 50 / 120 / 53 / 35 / 110 / 87 = 455 → **75 / 140 / 68 / 35 / 110 / 107 = 535** (+25 HP +20 Atk +15 Def +20 Spe).
- **Abilities**: Limber, Reckless, hidden Unburden (was Limber, Reckless, hidden Unburden).
- **Evolution**: Hitmonlee at level 110, holding a Black Belt (used up). Data: `{"targetSpecies":"Savateur","method":"LevelHoldingItem","level":110,"item":"Black Belt"}`.
- **Look**: from our Hitmonlee: taller still, its legs longer and springier, a short cape of brown hide at its shoulders, and wraps (`Fur`, cream) round its ankles. **New feature**: the cape.

### 26. Pugilord, from Hitmonchan (107): yes

- **Typing**: Fighting → **Fighting/Steel**. The gloves turn to steel; Steel adds Bullet Punch and Meteor Mash. *Alternative*: Fighting/Electric (Thunder Punch's).
- **The Prizefight Pokémon**, No. 2107. 1.6 m, 75.0 kg (Hitmonchan 1.4 m, 50.2 kg).
- **Stats**: 50 / 105 / 79 / 35 / 110 / 76 = 455 → **75 / 130 / 94 / 35 / 110 / 91 = 535** (+25 HP +25 Atk +15 Def +15 Spe).
- **Abilities**: Keen Eye, Iron Fist, hidden Inner Focus (was Keen Eye, Iron Fist, hidden Inner Focus).
- **Evolution**: Hitmonchan at level 110, holding a Punching Glove (used up). Data: `{"targetSpecies":"Pugilord","method":"LevelHoldingItem","level":110,"item":"Punching Glove"}`.
- **Look**: from our Hitmonchan: broader in the chest, its red gloves become steel gauntlets (`Metal`) with a red band, a gold champion's belt at its waist and a high collar. **New feature**: the gauntlets.

### 27. Smoglom, from Weezing (110): maybe

- **Typing**: Poison → **Poison/Fire**. Its gas burns; Fire gives it Flamethrower with the type and resists Bug and Fairy four times, with a four-times weakness to Ground. *Alternative*: keep Poison alone.
- **The Smog Cloud Pokémon**, No. 2110. 1.2 m, 12.0 kg (Weezing 1.2 m, 9.5 kg).
- **Stats**: 65 / 90 / 120 / 85 / 70 / 60 = 490 → **85 / 90 / 130 / 110 / 90 / 65 = 570** (+20 HP +10 Def +25 SpA +20 SpD +5 Spe).
- **Abilities**: Levitate, hidden **Neutralizing Gas** (was Levitate, hidden Stench).
- **Evolution**: Weezing at level 112. Data: `{"targetSpecies":"Smoglom","level":112}`.
- **Look**: from our two balls of gas grown together: a third, smallest ball joins them, all three ringed with a thin band of fire (`Glow`) at the seams, smoke rising from vents on their tops. **New feature**: the third ball.

### 28. Matriroo, from Kangaskhan (115): maybe

- **Typing**: Normal → **Normal/Fighting**. The parent that fights for its child; Fighting gives it Close Combat and Drain Punch. *Alternative*: keep Normal alone.
- **The Matriarch Pokémon**, No. 2115. 2.6 m, 120.0 kg (Kangaskhan 2.2 m, 80.0 kg).
- **Stats**: 105 / 95 / 80 / 40 / 80 / 90 = 490 → **105 / 120 / 100 / 40 / 100 / 105 = 570** (+25 Atk +20 Def +20 SpD +15 Spe).
- **Abilities**: Early Bird, Scrappy, hidden Inner Focus (was Early Bird, Scrappy, hidden Inner Focus).
- **Evolution**: Kangaskhan at level 115, with friendship 220 or more. Data: `{"targetSpecies":"Matriroo","method":"Friendship","level":115}`.
- **Look**: from our great brown parent with its helmet of hard skin: taller, the helmet grown into a crest, the baby in the pouch older and wearing a helmet of its own. **New feature**: the child's helmet.

### 29. Sabrefin, from Seaking (119): yes

- **Typing**: Water → **Water/Fairy**. The horn of a unicorn; Fairy gives Play Rough and Moonblast. *Alternative*: Water/Steel.
- **The Swordfish Pokémon**, No. 2119. 2.0 m, 70.0 kg (Seaking 1.3 m, 39.0 kg).
- **Stats**: 80 / 92 / 65 / 65 / 80 / 68 = 450 → **90 / 117 / 75 / 65 / 90 / 93 = 530** (+10 HP +25 Atk +10 Def +10 SpD +25 Spe).
- **Abilities**: Swift Swim, Water Veil, hidden Lightning Rod (was Swift Swim, Water Veil, hidden Lightning Rod).
- **Evolution**: Seaking at level 106. Data: `{"targetSpecies":"Sabrefin","level":106}`.
- **Look**: from our Seaking: longer and sleeker, the horn grown into a straight sword's blade (`Shell`, white), the orange and white patches more like a koi's, the fins flowing longer. The female's shorter horn is kept as a shorter blade. **New feature**: the sword horn.

### 30. Stellamir, from Starmie (121): maybe

- **Typing**: Water/Psychic, kept. *Alternative*: Psychic/Steel.
- **The Nebula Pokémon**, No. 2121. 1.6 m, 110.0 kg (Starmie 1.1 m, 80.0 kg).
- **Stats**: 60 / 75 / 85 / 100 / 85 / 115 = 520 → **80 / 75 / 95 / 125 / 100 / 125 = 600** (+20 HP +10 Def +25 SpA +15 SpD +10 Spe).
- **Abilities**: Illuminate, Natural Cure, hidden Analytic (was Illuminate, Natural Cure, hidden Analytic).
- **Evolution**: Starmie at level 120, at night. Data: `{"targetSpecies":"Stellamir","method":"LevelNight","level":120}`.
- **Look**: from our Starmie: a third, smaller star turns between its two, all three around the core, and the core's gem holds a swirl of light (`Glow`, pink and violet). **New feature**: the third star.

### 31. Mimestro, from Mr. Mime (122): maybe

- **Typing**: Psychic/Fairy, kept. *Alternative*: Psychic/Steel, walls that are real.
- **The Grand Mime Pokémon**, No. 2122. 1.6 m, 60.0 kg (Mr. Mime 1.3 m, 54.5 kg).
- **Stats**: 40 / 45 / 65 / 100 / 120 / 90 = 460 → **70 / 45 / 80 / 120 / 120 / 105 = 540** (+30 HP +15 Def +20 SpA +15 Spe).
- **Abilities**: Soundproof, Filter, hidden Technician (was Soundproof, Filter, hidden Technician).
- **Evolution**: Mr. Mime at level 108, knowing Reflect. Data: `{"targetSpecies":"Mimestro","method":"LevelKnowsMove","level":108,"move":"Reflect"}`.
- **Look**: from our Mr. Mime: taller, a tall hat, a conductor's tailcoat of pink and white, and the hands (which float as the style guide's hands do) holding a pane of light between them (`Glow`, pale blue). **New feature**: the pane of light.

### 32. Aurorelle, from Jynx (124): maybe

- **Typing**: Ice/Psychic, kept. *Alternative*: Ice/Fairy.
- **The Diva Pokémon**, No. 2124. 1.6 m, 45.0 kg (Jynx 1.4 m, 40.6 kg).
- **Stats**: 65 / 50 / 35 / 115 / 95 / 95 = 455 → **85 / 50 / 55 / 140 / 95 / 110 = 535** (+20 HP +20 Def +25 SpA +15 Spe).
- **Abilities**: Oblivious, Forewarn, hidden Dry Skin (was Oblivious, Forewarn, hidden Dry Skin).
- **Evolution**: Jynx at level 112, at night. Data: `{"targetSpecies":"Aurorelle","method":"LevelNight","level":112}`.
- **Look**: from our Jynx: a new figure rather than a bigger one, tall and slender in a gown of ice crystals, its long hair turned pale blue-white and lit along its length by the green and violet bands of an aurora (`Glow`). Its face is a pale lilac mask with large eyes. **New feature**: the aurora in its hair.

### 33. Mandibron, from Pinsir (127): yes

- **Typing**: Bug → **Bug/Dark**. Its horns close like a guillotine; Bug/Dark has only Lokix, and gives Knock Off and Sucker Punch. *Alternative*: Bug/Fighting (Heracross's, its rival).
- **The Guillotine Pokémon**, No. 2127. 1.8 m, 80.0 kg (Pinsir 1.5 m, 55.0 kg).
- **Stats**: 65 / 125 / 100 / 55 / 70 / 85 = 500 → **85 / 150 / 105 / 55 / 85 / 100 = 580** (+20 HP +25 Atk +5 Def +15 SpD +15 Spe).
- **Abilities**: Hyper Cutter, Mold Breaker, hidden Moxie (was Hyper Cutter, Mold Breaker, hidden Moxie).
- **Evolution**: Pinsir at level 115, at night. Data: `{"targetSpecies":"Mandibron","method":"LevelNight","level":115}`.
- **Look**: from our stag beetle, all head on short legs: the horns grow longer and darker (`Shell`, near black) with an edge of red teeth, a smooth carapace closes over its back, and its body darkens to brown-black. **New feature**: the red-edged horns.

### 34. Bovarch, from Tauros (128): yes

- **Typing**: Normal → **Normal/Ground**. The stampede shakes the ground; Ground gives Earthquake with the type and immunity to Electric. *Alternative*: Normal/Fighting (the Paldean Combat Breed's).
- **The Herd Bull Pokémon**, No. 2128. 1.8 m, 140.0 kg (Tauros 1.4 m, 88.4 kg).
- **Stats**: 75 / 100 / 95 / 40 / 70 / 110 = 490 → **100 / 125 / 100 / 40 / 85 / 120 = 570** (+25 HP +25 Atk +5 Def +15 SpD +10 Spe).
- **Abilities**: Intimidate, Anger Point, hidden Sheer Force (was Intimidate, Anger Point, hidden Sheer Force).
- **Evolution**: Tauros at level 115. Data: `{"targetSpecies":"Bovarch","level":115}`.
- **Look**: from our bull with its shaggy mane and three tails: heavier in the shoulders, its horns grown wider and ringed, the mane longer and darker, and a crest of rock-like hide (`Shell`, grey) on its brow where the three studs were. **New feature**: the ringed horns.

### 35. Typhonarch, from Gyarados (130): maybe

- **Typing**: Water/Flying → **Water/Dragon**. The dragon it has always looked like; Water/Dragon is Kingdra's, and loses the four-times Electric weakness. *Alternative*: keep Water/Flying.
- **The Tempest Pokémon**, No. 2130. 9.0 m, 400.0 kg (Gyarados 6.5 m, 235.0 kg).
- **Stats**: 95 / 125 / 79 / 60 / 100 / 81 = 540 → **100 / 145 / 99 / 60 / 100 / 96 = 600** (+5 HP +20 Atk +20 Def +15 Spe).
- **Abilities**: Intimidate, hidden Moxie (was Intimidate, hidden Moxie).
- **Evolution**: Gyarados at level 130, in the rain. Data: `{"targetSpecies":"Typhonarch","method":"LevelInRain","level":130}`.
- **Look**: from our Gyarados: longer, with two short forelegs and clawed hands, the crest grown into a great fan of fins, and long whiskers (the female's white) flowing like a dragon's. **New feature**: the forelegs.

### 36. Ferrisle, from Lapras (131): strong yes

- **Typing**: Water/Ice, kept. *Alternative*: Water/Fairy.
- **The Voyager Pokémon**, No. 2131. 4.0 m, 600.0 kg (Lapras 2.5 m, 220.0 kg).
- **Stats**: 130 / 85 / 80 / 85 / 95 / 60 = 535 → **130 / 85 / 100 / 105 / 110 / 70 = 600** (+20 Def +20 SpA +15 SpD +10 Spe).
- **Abilities**: Water Absorb, Shell Armor, hidden Hydration (was Water Absorb, Shell Armor, hidden Hydration).
- **Evolution**: Lapras at level 125, with friendship 220 or more. Data: `{"targetSpecies":"Ferrisle","method":"Friendship","level":125}`.
- **Look**: from our gentle plesiosaur: much larger, the knobbled grey shell grown into a wide deck with a low crest of ice along its middle and small pines of ice on it, the neck longer, the horn longer. **New feature**: the deck of ice.

### 37. Naiadeon, from Vaporeon (134): maybe

- **Typing**: Water, kept. The Eeveelutions are one type each. *Alternative*: none for this species.
- **The Tide Pokémon**, No. 2134. 1.3 m, 38.0 kg (Vaporeon 1.0 m, 29.0 kg).
- **Stats**: 130 / 65 / 60 / 110 / 95 / 65 = 525 → **130 / 65 / 80 / 130 / 110 / 85 = 600** (+20 Def +20 SpA +15 SpD +20 Spe).
- **Abilities**: Water Absorb, hidden Hydration (was Water Absorb, hidden Hydration).
- **Evolution**: Vaporeon at level 120, holding a Water Stone, with friendship 220 or more (used up). Data: `{"targetSpecies":"Naiadeon","method":"LevelHoldingItem","level":120,"item":"Water Stone","needsFriendship":true}`.
- **Look**: from our Vaporeon: longer, the ruff grown into a flowing mane of fins, the tail fin wider and translucent at its edge, and pale wave lines painted down its body. **New feature**: the mane of fins.

### 38. Fulgureon, from Jolteon (135): maybe

- **Typing**: Electric, kept. *Alternative*: none for this species.
- **The Storm Pokémon**, No. 2135. 1.0 m, 30.0 kg (Jolteon 0.8 m, 24.5 kg).
- **Stats**: 65 / 65 / 60 / 110 / 95 / 130 = 525 → **90 / 65 / 75 / 135 / 105 / 130 = 600** (+25 HP +15 Def +25 SpA +10 SpD).
- **Abilities**: Volt Absorb, hidden Quick Feet (was Volt Absorb, hidden Quick Feet).
- **Evolution**: Jolteon at level 120, holding a Thunder Stone, with friendship 220 or more (used up). Data: `{"targetSpecies":"Fulgureon","method":"LevelHoldingItem","level":120,"item":"Thunder Stone","needsFriendship":true}`.
- **Look**: from our Jolteon: its spikes grow longer and sweep back into lightning bolts, white at their tips (`Glow`), and its ruff stands up in a crest. **New feature**: the white-tipped bolts.

### 39. Pyreon, from Flareon (136): maybe

- **Typing**: Fire, kept. *Alternative*: none for this species.
- **The Blaze Pokémon**, No. 2136. 1.1 m, 32.0 kg (Flareon 0.9 m, 25.0 kg).
- **Stats**: 65 / 130 / 60 / 95 / 110 / 65 = 525 → **90 / 130 / 85 / 95 / 110 / 90 = 600** (+25 HP +25 Def +25 Spe).
- **Abilities**: Flash Fire, hidden Guts (was Flash Fire, hidden Guts).
- **Evolution**: Flareon at level 120, holding a Fire Stone, with friendship 220 or more (used up). Data: `{"targetSpecies":"Pyreon","method":"LevelHoldingItem","level":120,"item":"Fire Stone","needsFriendship":true}`.
- **Look**: from our Flareon: its ruff and tail grow fuller, and the ruff's tips burn with small flames (`Glow`); a tuft on its brow streams back. **New feature**: the burning ruff.

### 40. Ammonarch, from Omastar (139): strong yes

- **Typing**: Rock/Water, kept. *Alternative*: Rock/Psychic (the Helix).
- **The Spiral Pokémon**, No. 2139. 1.6 m, 70.0 kg (Omastar 1.0 m, 35.0 kg).
- **Stats**: 70 / 60 / 125 / 115 / 70 / 55 = 495 → **90 / 60 / 125 / 140 / 90 / 70 = 575** (+20 HP +25 SpA +20 SpD +15 Spe).
- **Abilities**: Swift Swim, Shell Armor, hidden Weak Armor (was Swift Swim, Shell Armor, hidden Weak Armor).
- **Evolution**: Omastar at level 120, holding a Helix Fossil (used up). Data: `{"targetSpecies":"Ammonarch","method":"LevelHoldingItem","level":120,"item":"Helix Fossil"}`.
- **Look**: from our Omastar: the spiral shell much larger and ridged, with spikes on its outer whorl, and the tentacles longer and more of them. **New feature**: the ridged whorl.

### 41. Eurypterox, from Kabutops (141): strong yes

- **Typing**: Rock/Water, kept. *Alternative*: Rock/Bug.
- **The Sea Scorpion Pokémon**, No. 2141. 1.8 m, 70.0 kg (Kabutops 1.3 m, 40.5 kg).
- **Stats**: 60 / 115 / 105 / 65 / 70 / 80 = 495 → **80 / 140 / 105 / 65 / 85 / 100 = 575** (+20 HP +25 Atk +15 SpD +20 Spe).
- **Abilities**: Swift Swim, Battle Armor, hidden Weak Armor (was Swift Swim, Battle Armor, hidden Weak Armor).
- **Evolution**: Kabutops at level 120, holding a Dome Fossil (used up). Data: `{"targetSpecies":"Eurypterox","method":"LevelHoldingItem","level":120,"item":"Dome Fossil"}`.
- **Look**: from our Kabutops: longer and lower, with a jointed tail ending in a flat paddle, the two scythes bigger, and a second, smaller pair of claws below them. **New feature**: the jointed tail.

### 42. Azhdarch, from Aerodactyl (142): strong yes

- **Typing**: Rock/Flying, kept. *Alternative*: Rock/Dragon.
- **The Sky Tyrant Pokémon**, No. 2142. 3.0 m, 150.0 kg (Aerodactyl 1.8 m, 59.0 kg).
- **Stats**: 80 / 105 / 65 / 60 / 75 / 130 = 515 → **100 / 130 / 85 / 60 / 85 / 135 = 595** (+20 HP +25 Atk +20 Def +10 SpD +5 Spe).
- **Abilities**: Rock Head, Pressure, hidden Unnerve (was Rock Head, Pressure, hidden Unnerve).
- **Evolution**: Aerodactyl at level 125, holding an Old Amber (used up). Data: `{"targetSpecies":"Azhdarch","method":"LevelHoldingItem","level":125,"item":"Old Amber"}`.
- **Look**: from our flying reptile: much larger, a long stiff neck and a great crest on its skull, the purple wings broader, and amber stones in a ridge down its back. **New feature**: the crest.

### 43. Dozemount, from Snorlax (143): maybe

- **Typing**: Normal → **Normal/Grass**. It has slept so long the woods grew on it; Grass adds Wood Hammer and Ground resistance. *Alternative*: keep Normal alone.
- **The Slumber Pokémon**, No. 2143. 2.6 m, 620.0 kg (Snorlax 2.1 m, 460.0 kg).
- **Stats**: 160 / 110 / 65 / 65 / 110 / 30 = 540 → **160 / 125 / 95 / 70 / 120 / 30 = 600** (+15 Atk +30 Def +5 SpA +10 SpD).
- **Abilities**: Immunity, Thick Fat, hidden Gluttony (was Immunity, Thick Fat, hidden Gluttony).
- **Evolution**: Snorlax at level 130, with friendship 220 or more. Data: `{"targetSpecies":"Dozemount","method":"Friendship","level":130}`.
- **Look**: from our Snorlax: larger, a mossy mantle on its back and shoulders with small trees and flowers, and a bird's nest on its head. **New feature**: the mossy mantle.

### 44. Glacirune, from Articuno (144): maybe

- **Typing**: Ice/Flying, kept. *Alternative*: none of the three.
- **The Aurora Pokémon**, No. 2144. 2.2 m, 70.0 kg (Articuno 1.7 m, 55.4 kg).
- **Stats**: 90 / 85 / 100 / 95 / 125 / 85 = 580 → **105 / 85 / 110 / 120 / 140 / 100 = 660** (+15 HP +10 Def +25 SpA +15 SpD +15 Spe).
- **Abilities**: Pressure, hidden **Snow Warning** (was Pressure, hidden Snow Cloak).
- **Evolution**: Articuno at level 150, holding a Never-Melt Ice (used up). Data: `{"targetSpecies":"Glacirune","method":"LevelHoldingItem","level":150,"item":"Never-Melt Ice"}`.
- **Look**: from our slender blue bird with its crest of three plumes: larger, the two tail streamers grown into a long veil of aurora (`Glow`, green and violet) and ice crystals at the wing tips. **New feature**: the aurora veil.

### 45. Fulguris, from Zapdos (145): maybe

- **Typing**: Electric/Flying, kept. *Alternative*: none of the three.
- **The Thunderhead Pokémon**, No. 2145. 2.0 m, 70.0 kg (Zapdos 1.6 m, 52.6 kg).
- **Stats**: 90 / 90 / 85 / 125 / 90 / 100 = 580 → **105 / 90 / 95 / 150 / 100 / 120 = 660** (+15 HP +10 Def +25 SpA +10 SpD +20 Spe).
- **Abilities**: Pressure, hidden **Drizzle** (was Pressure, hidden Static).
- **Evolution**: Zapdos at level 150, holding a Magnet (used up). Data: `{"targetSpecies":"Fulguris","method":"LevelHoldingItem","level":150,"item":"Magnet"}`.
- **Look**: from our Zapdos: larger, its jagged feathers grown longer and darker at the base, a crown of storm cloud (grey, soft) at its head. **New feature**: the cloud crown.

### 46. Pyrevant, from Moltres (146): maybe

- **Typing**: Fire/Flying, kept. *Alternative*: none of the three.
- **The Phoenix Pokémon**, No. 2146. 2.6 m, 80.0 kg (Moltres 2.0 m, 60.0 kg).
- **Stats**: 90 / 100 / 90 / 125 / 85 / 90 = 580 → **105 / 100 / 90 / 150 / 105 / 110 = 660** (+15 HP +25 SpA +20 SpD +20 Spe).
- **Abilities**: Pressure, hidden **Drought** (was Pressure, hidden Flame Body).
- **Evolution**: Moltres at level 150, holding a Charcoal (used up). Data: `{"targetSpecies":"Pyrevant","method":"LevelHoldingItem","level":150,"item":"Charcoal"}`.
- **Look**: from our long slender bird whose wings and tail are sheets of fire: the fire whitens at the core, a tail of three long flame streamers, and a halo of embers at its crest. **New feature**: the three-streamer tail.

## Balance

Measured by the script against the species in the data (the non-legendary ones; the Ultra Beasts count among them):

- **Totals**: 457 (Porrigard) to 600, the birds 660. Nothing that isn't legendary passes the pseudo-legendaries' 600; ten reach it.
- **Highest of each stat**: Attack 150 (Pincerex, Mandibron), under Rampardos's 165 and Kartana's 181. Sp. Atk 150 (Fulguris, Pyrevant), 140 for the rest (Palmagus, Aurorelle, Ammonarch), under Xurkitree's 173. Speed 150 (Dynamorb, which is Electrode's own 150 kept), then Azhdarch's 135. Defense 180 (Clamspire, Cloyster's own). HP 160 (Dozemount, Snorlax's own).
- **The weakest stay modest on purpose**: Porrigard 457, Cordyhusk 485, Ratchisel 493. A rise of 80 takes a weak line to where a middle line was, not to the top; the user may want a floor instead (open question 4).

**Flagged as strong** (each inside the official range; the user may want one turned down):
1. **Kitsunox**: Drought on a fast Fire/Ghost with 121 Sp. Atk and Nasty Plot in its line. *To turn down*: Flash Fire as its hidden ability in place of Drought.
2. **Somnimoth**: Quiver Dance with 120 Sp. Atk, 110 Speed and Tinted Lens. *To turn down*: Speed 100 and Sp. Def 100.
3. **Typhonarch**: 145 Attack, Dragon Dance, Moxie and a typing with two weaknesses. Only at level 130 in the rain, which keeps it late. *To turn down*: Attack 135 and Defense 109.
4. **Clamspire**: Shell Smash and Skill Link, now with 75 HP and 75 Sp. Def. *To turn down*: give the 75 to HP and Sp. Def alone and keep Attack 95.

**Weaker by design**: Tripluma, Molemount and Dynamorb are maybes for their designs more than their numbers.

## Open questions: what a species of our own touches

The plan asks the first batch to list what assumed 1,025 species and to settle it with the user. These are the places a search of the engine and the tests finds, each with the default a building session takes if the user doesn't say otherwise.

1. **The numbers.** *Default*: 2000 + the number of the species it evolves from (2020 for Raticate's), stored as `dexNumber` like any species'. `SavedPokemonData.DexNumber` then finds it by number as it finds the rest, and a save keeps it as it keeps any species. *Alternative*: one block from 1501 on, numbered in the order the user approves them (closer together, but a later batch's numbers then depend on the earlier ones' answers).
2. **The National Pokédex.** `Pokedex.Entries("national")` is every species in the data by number, so ours would appear after Pecharunt as they are added, and its completion and diploma (`Pokedex.Completion`, `AwardDiplomas`) would count them. *Default*: they are listed in the National Pokédex after the 1,025, under a heading of their own, and **don't count toward its completion or its diploma**, which stays the games' 1,025; a diploma of their own once the last batch is built. *Alternative*: they count, and the National diploma waits for them all. No regional Pokédex lists them (`dexNumbers` has only `national`).
3. **The PC.** `PcBoxes.NationalBoxCount` is 100 boxes (3,000 places): a living Pokédex of 1,025 and about 450 of ours fits. *Default*: nothing changes.
4. **The rise.** *Default*: +80 for every species, the 600 ceiling for those that aren't legendary. *Alternatives*: a floor (no species of ours under 480, so Porrigard and Ratchisel rise more), or a rise by the species' total (more for the weakest).
5. **What takes them and what doesn't.** *Defaults*: the Battle Frontier lets the player enter them (`FrontierRules` bans the eighteen it names; the three birds' evolutions join that list, the rest don't) but its trainers never draw them (`frontier.json` is Platinum's); Pal Park never shows them (`PalParkState` draws only from Platinum's 493); the Pokéathlon's rivals, when its own pool runs short, draw from every species that isn't legendary and would take them, which the building session holds to 1 to 1,025; Challenge's trainers (`DifficultyRules.StageAt`) only ever step a Pokémon back down its line, so they never become ours; migration can't bring them (no game has them) and the way out can't take them: a `.pk4` holds no species past 493 already (`DsPokemon.HighestSpecies`), and Showdown's text would name a species Showdown doesn't know, so *by default* the way out refuses them too.
6. **The golden files.** `BattleCorpus` draws its modern battles from every species with a number, so adding species would move `battles-modern.txt`. *Default*: the corpus is held to 1 to 1,025, and nothing golden moves.
7. **Their names in other languages.** PokeAPI has none. *Default*: English everywhere but Romanian, whose names the user's Romanian sessions write by hand (plan 17), as the plan says.
8. **A female look.** Five of the species have one (Raticate, Dodrio, Hypno, Seaking, Gyarados). *Default*: ours keep it where the feature survives (each look says so), as a `+f` look; the rest have none.
9. **Megas and Gigantamax.** The species with Megas (Slowbro, Kangaskhan, Starmie, Pinsir, Gyarados, Aerodactyl) and Gigantamax forms (Kingler, Lapras, Snorlax) keep them; ours have none, and a stone keyed to the species does nothing in ours' hand (Showdown's rule, R20b's).
10. **Regional forms.** Alolan, Galarian, Hisuian and Paldean forms of these species don't evolve into ours: the engine already refuses them every evolution that isn't their form's own (`Evolution.FitsForm`). *Default*: kept so; a regional form's own past-100 evolution would be a design of its own, in a later batch, only on the user's word.

## The table to answer

One row a species. The user answers each with yes, a change, or no; striking a row in the selection table above is a no.

| # | Ours | Typing | Abilities | HP/Atk/Def/SpA/SpD/Spe (total) | Δ | Evolves |
|---|---|---|---|---|---|---|
| 1 | Ratchisel | Normal/Steel | Strong Jaw, Guts, Hustle | 70/106/85/50/75/107 (493) | +15 HP +25 Atk +25 Def +5 SpD +10 Spe | level 105 |
| 2 | Lancerow | Normal/Flying | Super Luck, Sniper | 80/125/75/61/66/115 (522) | +15 HP +35 Atk +10 Def +5 SpD +15 Spe | level 104, holding a Scope Lens (used up) |
| 3 | Najarok | Poison/Dark | Intimidate, Shed Skin, Merciless | 80/120/84/65/89/90 (528) | +20 HP +25 Atk +15 Def +10 SpD +10 Spe | level 106 |
| 4 | Sandspire | Ground/Rock | Rough Skin, Sand Rush | 85/125/130/45/70/75 (530) | +10 HP +25 Atk +20 Def +15 SpD +10 Spe | level 108 |
| 5 | Kitsunox | Fire/Ghost | Cursed Body, Drought | 88/76/75/121/110/115 (585) | +15 HP +40 SpA +10 SpD +15 Spe | level 120, with friendship 220 or more, at night |
| 6 | Cordyhusk | Grass/Ghost | Effect Spore, Dry Skin, Perish Body | 90/105/95/65/100/30 (485) | +30 HP +10 Atk +15 Def +5 SpA +20 SpD | level 110, at night |
| 7 | Somnimoth | Bug/Psychic | Shield Dust, Tinted Lens, Wonder Skin | 80/65/65/120/90/110 (530) | +10 HP +5 Def +30 SpA +15 SpD +20 Spe | level 112, at night |
| 8 | Molemount | Ground/Rock | Sand Veil, Arena Trap, Sand Force | 65/120/70/50/75/125 (505) | +30 HP +20 Atk +20 Def +5 SpD +5 Spe | level 104 |
| 9 | Regalynx | Normal/Fairy | Limber, Technician, Unnerve | 80/95/70/65/80/130 (520) | +15 HP +25 Atk +10 Def +15 SpD +15 Spe | level 110, holding an Amulet Coin (used up) |
| 10 | Psyduke | Water/Psychic | Damp, Cloud Nine, Swift Swim | 90/82/88/125/95/100 (580) | +10 HP +10 Def +30 SpA +15 SpD +15 Spe | level 112, knowing Zen Headbutt |
| 11 | Kirinine | Fire/Dragon | Intimidate, Flash Fire, Justified | 90/125/90/100/90/105 (600) | +15 Atk +10 Def +10 SpD +10 Spe | level 130, with friendship 220 or more |
| 12 | Medusarch | Water/Poison | Clear Body, Liquid Ooze, Rain Dish | 100/70/85/100/130/110 (595) | +20 HP +20 Def +20 SpA +10 SpD +10 Spe | level 115, holding a Poison Barb (used up) |
| 13 | Solarion | Fire/Flying | Reckless, Flash Fire, Flame Body | 80/120/80/80/95/125 (580) | +15 HP +20 Atk +10 Def +15 SpD +20 Spe | level 115, by day |
| 14 | Slowmantle | Water/Psychic | Oblivious, Own Tempo, Regenerator | 115/75/130/115/105/30 (570) | +20 HP +20 Def +15 SpA +25 SpD | level 120, holding a Big Pearl (used up) |
| 15 | Porrigard | Normal/Flying | Keen Eye, Inner Focus, Defiant | 67/115/65/58/67/85 (457) | +15 HP +25 Atk +10 Def +5 SpD +25 Spe | level 101, holding a Leek (used up) |
| 16 | Tripluma | Normal/Flying | Run Away, Early Bird, Tangled Feet | 80/130/80/60/70/130 (550) | +20 HP +20 Atk +10 Def +10 SpD +20 Spe | level 108 |
| 17 | Glacitusk | Water/Ice | Thick Fat, Hydration, Ice Body | 110/90/95/70/105/85 (555) | +20 HP +20 Atk +15 Def +10 SpD +15 Spe | level 112, holding a Never-Melt Ice (used up) |
| 18 | Sludgemire | Poison/Ground | Stench, Sticky Hold, Poison Touch | 125/125/100/65/110/55 (580) | +20 HP +20 Atk +25 Def +10 SpD +5 Spe | level 115 |
| 19 | Clamspire | Water/Ice | Shell Armor, Skill Link, Overcoat | 75/105/180/85/75/80 (600) | +25 HP +10 Atk +30 SpD +10 Spe | level 120, holding a Shell Bell (used up) |
| 20 | Mesmeryx | Psychic/Dark | Insomnia, Forewarn, Inner Focus | 95/73/80/113/125/77 (563) | +10 HP +10 Def +40 SpA +10 SpD +10 Spe | level 110, at night |
| 21 | Pincerex | Water/Steel | Hyper Cutter, Shell Armor, Sheer Force | 80/150/120/50/70/85 (555) | +25 HP +20 Atk +5 Def +20 SpD +10 Spe | level 110 |
| 22 | Dynamorb | Electric/Steel | Soundproof, Static, Aftermath | 80/50/80/115/95/150 (570) | +20 HP +10 Def +35 SpA +15 SpD | level 105, at a Magnetic Field (Mt. Coronet's) |
| 23 | Palmagus | Grass/Psychic | Chlorophyll, Harvest | 110/95/100/140/100/55 (600) | +15 HP +15 Def +15 SpA +25 SpD | a Leaf Stone used on it at level 118 or above |
| 24 | Ossugard | Ground/Ghost | Rock Head, Lightning Rod, Battle Armor | 80/110/115/50/90/60 (505) | +20 HP +30 Atk +5 Def +10 SpD +15 Spe | level 105, holding a Thick Club (used up) |
| 25 | Savateur | Fighting/Flying | Limber, Reckless, Unburden | 75/140/68/35/110/107 (535) | +25 HP +20 Atk +15 Def +20 Spe | level 110, holding a Black Belt (used up) |
| 26 | Pugilord | Fighting/Steel | Keen Eye, Iron Fist, Inner Focus | 75/130/94/35/110/91 (535) | +25 HP +25 Atk +15 Def +15 Spe | level 110, holding a Punching Glove (used up) |
| 27 | Smoglom | Poison/Fire | Levitate, Neutralizing Gas | 85/90/130/110/90/65 (570) | +20 HP +10 Def +25 SpA +20 SpD +5 Spe | level 112 |
| 28 | Matriroo | Normal/Fighting | Early Bird, Scrappy, Inner Focus | 105/120/100/40/100/105 (570) | +25 Atk +20 Def +20 SpD +15 Spe | level 115, with friendship 220 or more |
| 29 | Sabrefin | Water/Fairy | Swift Swim, Water Veil, Lightning Rod | 90/117/75/65/90/93 (530) | +10 HP +25 Atk +10 Def +10 SpD +25 Spe | level 106 |
| 30 | Stellamir | Water/Psychic | Illuminate, Natural Cure, Analytic | 80/75/95/125/100/125 (600) | +20 HP +10 Def +25 SpA +15 SpD +10 Spe | level 120, at night |
| 31 | Mimestro | Psychic/Fairy | Soundproof, Filter, Technician | 70/45/80/120/120/105 (540) | +30 HP +15 Def +20 SpA +15 Spe | level 108, knowing Reflect |
| 32 | Aurorelle | Ice/Psychic | Oblivious, Forewarn, Dry Skin | 85/50/55/140/95/110 (535) | +20 HP +20 Def +25 SpA +15 Spe | level 112, at night |
| 33 | Mandibron | Bug/Dark | Hyper Cutter, Mold Breaker, Moxie | 85/150/105/55/85/100 (580) | +20 HP +25 Atk +5 Def +15 SpD +15 Spe | level 115, at night |
| 34 | Bovarch | Normal/Ground | Intimidate, Anger Point, Sheer Force | 100/125/100/40/85/120 (570) | +25 HP +25 Atk +5 Def +15 SpD +10 Spe | level 115 |
| 35 | Typhonarch | Water/Dragon | Intimidate, Moxie | 100/145/99/60/100/96 (600) | +5 HP +20 Atk +20 Def +15 Spe | level 130, in the rain |
| 36 | Ferrisle | Water/Ice | Water Absorb, Shell Armor, Hydration | 130/85/100/105/110/70 (600) | +20 Def +20 SpA +15 SpD +10 Spe | level 125, with friendship 220 or more |
| 37 | Naiadeon | Water | Water Absorb, Hydration | 130/65/80/130/110/85 (600) | +20 Def +20 SpA +15 SpD +20 Spe | level 120, holding a Water Stone, with friendship 220 or more (used up) |
| 38 | Fulgureon | Electric | Volt Absorb, Quick Feet | 90/65/75/135/105/130 (600) | +25 HP +15 Def +25 SpA +10 SpD | level 120, holding a Thunder Stone, with friendship 220 or more (used up) |
| 39 | Pyreon | Fire | Flash Fire, Guts | 90/130/85/95/110/90 (600) | +25 HP +25 Def +25 Spe | level 120, holding a Fire Stone, with friendship 220 or more (used up) |
| 40 | Ammonarch | Rock/Water | Swift Swim, Shell Armor, Weak Armor | 90/60/125/140/90/70 (575) | +20 HP +25 SpA +20 SpD +15 Spe | level 120, holding a Helix Fossil (used up) |
| 41 | Eurypterox | Rock/Water | Swift Swim, Battle Armor, Weak Armor | 80/140/105/65/85/100 (575) | +20 HP +25 Atk +15 SpD +20 Spe | level 120, holding a Dome Fossil (used up) |
| 42 | Azhdarch | Rock/Flying | Rock Head, Pressure, Unnerve | 100/130/85/60/85/135 (595) | +20 HP +25 Atk +20 Def +10 SpD +5 Spe | level 125, holding an Old Amber (used up) |
| 43 | Dozemount | Normal/Grass | Immunity, Thick Fat, Gluttony | 160/125/95/70/120/30 (600) | +15 Atk +30 Def +5 SpA +10 SpD | level 130, with friendship 220 or more |
| 44 | Glacirune | Ice/Flying | Pressure, Snow Warning | 105/85/110/120/140/100 (660) | +15 HP +10 Def +25 SpA +15 SpD +15 Spe | level 150, holding a Never-Melt Ice (used up) |
| 45 | Fulguris | Electric/Flying | Pressure, Drizzle | 105/90/95/150/100/120 (660) | +15 HP +10 Def +25 SpA +10 SpD +20 Spe | level 150, holding a Magnet (used up) |
| 46 | Pyrevant | Fire/Flying | Pressure, Drought | 105/100/90/150/105/110 (660) | +15 HP +25 SpA +20 SpD +20 Spe | level 150, holding a Charcoal (used up) |

## The technical plan (for after the user's answers)

### 1. The data (one sitting)

- **Species of our own in the importer.** `Overrides/species.json` replaces fields of a species that exists; a whole species needs a new file, **`tools/DataImporter/Overrides/species-ours.json`**, whose entries are appended after the 1,025 (sorted by number), with everything a species of the data has: the values above, `evYield` (the old species' plus one point in the stat that rose most), `catchRate` 45 (3 for the birds), `baseExpYield` the old species' plus 20%, `growthRate`, egg groups, gender ratio and hatch cycles as the old species', `color` and `shape` for the Pokédex's search and the generator, `generation` 0 (ours), the learnset (06 · R31's moves) and the machines and tutor moves of the old species, `dexEntry` composed by `Models/DexText.cs`. The evolution row goes on the old species through `Overrides/species.json`'s `evolutions` (the old list plus the new row, since an override replaces the field whole). A few lines of the importer and a test.
- **The engine**: nothing for the evolutions (every method and condition runs). The number range, the National Pokédex's heading and diploma, the corpus and the draws held to 1 to 1,025 (open questions 2, 5, 6) are a few lines each, with a test each.
- **The Pokédex text**: two entries and a habitat each in `Data/dex-entries.json` (08 · P13's way, with `--check-entries`; none of ours can share words with a game's, since no game has them).
- **Then**: run the importer (`coverage.md` and the species count), the golden files (nothing should move), `FullDexTests` (every species made, saved and read back, now with ours).

### 2. The sculpts (one or two sittings per dozen)

Each in `PokemonModels.Ours.Kanto.cs`, in our numbers' order, a sculpt of its own built from its species' build where it shares most of the body (a flag on the existing build, as the Megas of our own will be), distinct from every other sculpt, in one piece, eyes as decals; listed with the hand-built species, so `PokemonModelTests` holds them. Checked with the harness's `dex` and `pokemon` turntables and a `versus` each, and shown to the user before they are kept. Their cries come from their data by themselves (`Cries`).

### 3. What is not this batch's

- R31's moves past level 100 (the learnsets above wait for them).
- Johto's batch (D14, batch 2), which takes Steelix, Scizor, Slowking and the rest that end in Johto.
- Where in the game the evolutions are met: they need no place, only a level the climb reaches.

## How the numbers were checked

A script (Node.js, kept in the session's scratch folder, reading only `species.json`, `abilities.json` and `items.json`) built the lines and the counts above, and for each of the 46 designs checked: that it qualifies and that every qualifying species has a design or a reason for none; that the rise is +80 or what the ceiling leaves, that no stat falls and that the six stats add up to the total written; that the name is one word of at most 12 letters, taken by no species or form of the data and by no other design; that the types are one or two of the eighteen; that the abilities exist and fill exactly the old species' slots; that the level is between 101 and 160, and at most 130 for a species that isn't legendary; that an item held exists and is no key item; that a move to know is in the species' learnset. The tables and the entries' numbers were written by the same script from the same values, so a number can't differ between the selection table, an entry and the table to answer. It also measured the rise (29 official cases) and each stat's highest for "Balance".
