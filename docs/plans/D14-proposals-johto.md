# D14 · Evolutions past level 100, batch 2 (Johto): the proposal

Plan 03 · D14, the design part of its second batch, written in wave 50 after the user approved batch 1 ([D14-proposals-kanto.md](D14-proposals-kanto.md)) on 2026-10-25. **Awaiting the user's approval.** Nothing here is built: no species, no evolution row, no learnset, no sculpt, no test. Every row below waits for the user's yes, a change, or a no, before a later session writes it into the data. The format is batch 1's.

**What batch 1 settled, applied here without asking again** (the defaults of its ten open questions, all accepted): the rise is **+80, capped so that no species of ours that isn't legendary passes 600**; the number is **2000 + the number of the species it evolves from**; abilities come **only from `abilities.json`**, as many slots as the species had, in its order, the hidden one last; the evolution is **by level between 101 and 160** (at most 130 for a species that isn't legendary) with a condition the engine already runs, a held item used up; our species are listed in the National Pokédex after the 1,025 and **count toward none of its totals or diplomas**; names are invented, one word of at most 12 letters, and held against every species and form of the data; **an evolution that starts from a regional form belongs to that form's own line** (`Evolution.FitsForm`), so a species whose regional form evolves is a species alone; a female look, a Mega or a Gigantamax form is kept where the species has one, and ours have none of their own; the Frontier lets them be entered (a legendary's evolution joins its ban list), Pal Park, migration, the way out, the Pokéathlon's rivals and the golden corpus are held to 1 to 1,025.

Every name, type, ability, item, move, stat and sum in this document was checked by a script that only reads `PokemonPlatinumEngine/Data/species.json`, `abilities.json` and `items.json`, with batch 1's names and numbers beside them (kept outside the repository; what it checks is listed under "How the numbers were checked", at the end).

## The shortlist: the twelve strongest

If the user wants a small first build of this batch, these are the twelve. Each fills a real gap (a weak total, a line the regional forms left behind, a design whose own lore asks for a later stage) or completes a set batch 1 began.

| | Ours | From | Typing | Total | Evolves | Why it leads |
|---|---|---|---|---|---|---|
| 1 | **Sleighwing** | Delibird | Ice/Flying | 330 → 410 | level 104, at an Ice Rock | the lowest total of any species alone in Johto |
| 2 | **Coccinova** | Ledian | Bug/Fighting | 390 → 470 | level 106, at night | the lowest total of Johto's last stages, and a puncher with 35 Attack |
| 3 | **Orbweavix** | Ariados | Bug/Poison | 400 → 480 | level 108, at night | the spider of 400 that waits all night in its web |
| 4 | **Reefcrown** | Corsola | Water/Rock | 410 → 490 | level 106 holding a Pearl | the Galarian Corsola moved on and Johto's never did |
| 5 | **Fugumace** | Qwilfish | Water/Poison | 440 → 520 | level 106, knowing Spikes | the Hisuian Qwilfish moved on and Johto's never did |
| 6 | **Mimicedar** | Sudowoodo | Rock/Grass | 410 → 490 | level 108, in the rain | the false tree that hates water takes root in the rain |
| 7 | **Ermilong** | Furret | Normal/Ice | 415 → 495 | level 106, at night | the first route's long body, in an ermine's winter coat |
| 8 | **Helianthra** | Sunflora | Grass/Fire | 425 → 505 | a Sun Stone at level 110 | the sun's flower, by the same stone again |
| 9 | **Ironwyrm** | Steelix | Steel/Ground | 510 → 590 | level 115 holding a Metal Coat | deferred from Kanto: the iron snake that hardens with age |
| 10 | **Atlascross** | Heracross | Bug/Fighting | 500 → 580 | level 115 | the Hercules beetle into the Atlas beetle; its rival Pinsir has Mandibron |
| 11 | **Cerberhound** | Houndoom | Dark/Fire | 500 → 580 | level 120, at night | the hellhound's three heads |
| 12 | **Gyrodrill** | Hitmontop | Fighting/Ground | 455 → 535 | level 110 holding a Muscle Band | the third of Tyrogue's ends, beside batch 1's Savateur and Pugilord |

## What qualifies, and how many

Batch 1's rule and the same script: a species **with no evolution at all**, or **the last stage of a line of two**; an evolution that starts from a regional form counts as that form's own.

**Johto, National Pokédex 152 to 251: 38 species qualify.**

- **15 alone**: Unown, Qwilfish, Shuckle, Heracross, Corsola, Delibird, Skarmory, Smeargle, Miltank, Raikou, Entei, Suicune, Lugia, Ho-Oh, Celebi (six of them legendary or mythical). Qwilfish and Corsola are alone by the regional rule: only the Hisuian Qwilfish becomes Overqwil and only the Galarian Corsola becomes Cursola, as only the Galarian Farfetch'd becomes Sirfetch'd.
- **23 last of a line of two**: Furret, Noctowl, Ledian, Ariados, Lanturn, Xatu, Sudowoodo, Sunflora, Quagsire, Espeon, Umbreon, Slowking, Wobbuffet, Forretress, Steelix, Granbull, Scizor, Magcargo, Octillery, Mantine, Houndoom, Donphan, Hitmontop. Sudowoodo, Wobbuffet and Mantine come from a first stage of a later game (Bonsly, Wynaut, Mantyke) and Hitmontop from Tyrogue, as batch 1's five did. Quagsire's line is a line of two: only the Paldean Wooper becomes Clodsire.
- **The five batch 1 deferred to this one**: Steelix (Onix's line), Scizor (Scyther's other end is Kleavor, Hisui's batch), Slowking (Slowpoke's other end, Slowbro, has batch 1's Slowmantle), Espeon and Umbreon (Eevee's; batch 1 built Vaporeon's, Jolteon's and Flareon's).
- **Kingdra is not here**: the data has Horsea into Seadra into Kingdra, a line of three, so Kingdra takes 06 · R31's moves. Nor are the other last stages of lines of three: Meganium, Typhlosion, Feraligatr, Crobat, Ampharos, Bellossom, Azumarill, Politoed, Jumpluff, Blissey and Tyranitar.
- **Not here, a Johto species whose line ends in a later region**: Aipom (Ambipom), Misdreavus (Mismagius), Murkrow (Honchkrow), Sneasel (Weavile; the Hisuian one's Sneasler), Yanma (Yanmega), Gligar (Gliscor), Stantler (Wyrdeer), Girafarig (Farigiraf), Dunsparce (Dudunsparce), and the lines of three that run through Ursaring (into Ursaluna), Piloswine (into Mamoswine) and Porygon2 (into Porygon-Z). Each is the batch of the region where its last stage is numbered, if it qualifies there. Tyrogue's and Smoochum's other ends were batch 1's.

34 designs follow; Unown, Lugia, Ho-Oh and Celebi are left out with their reasons.

## How to read an entry

As batch 1's. **Name** and **No.** follow its rules (the numbers here run 2162 to 2245 and meet none of batch 1's 2020 to 2146). **Stats** are the stats the game reads (the newest games' where `species.json` has a `modern` change: Noctowl, Ariados, Qwilfish, Magcargo, Corsola, Mantine; Granbull is Fairy by the same). **Levels**: Kanto's Champion is met at about 104 and Johto's at 136 (W4's fair climb), so Johto's own species evolve between 104 and 120, within Kanto's post-game or Johto itself, and the three beasts at 150, in Hoenn. **Looks** start from our own sculpt of the species, and a species whose female looks different (Ledian, Xatu, Sudowoodo, Quagsire, Wobbuffet, Steelix, Scizor, Heracross, Octillery, Houndoom, Donphan) keeps that difference where the feature survives. **Say**: strong yes (the shortlist), yes, maybe, no.

One condition is new to this batch and already in the data: **a species in the party** (`LevelWithSpeciesInParty`, Mantyke's own way into Mantine), which Raysail and the three beasts use. Delibird's Ice Rock is Route 217's (Glaceon's), so Sleighwing is a reason to fly back to Sinnoh.

## The legendaries, the mythical, Unown and Smeargle

Johto has six legendaries and one mythical; the user asked each batch to say which of its region's evolve.

- **Raikou, Entei and Suicune: maybe, as a trio or not at all.** Ho-Oh revived the three beasts in the Burned Tower; with a Ho-Oh in the party at level 150, which the climb reaches in Hoenn, each goes further. Each keeps Pressure and takes a hidden ability of its element (Electric Surge, Drought, Misty Surge: the north wind's purity). +80 to 660 matches batch 1's birds and stays under Lugia's and Ho-Oh's 680. *Default if the user doesn't say*: build them, as batch 1's birds were.
- **Lugia and Ho-Oh: no.** 680 each, the two cover legendaries of the region, as Mewtwo was Kanto's. Ho-Oh's part is to take the beasts further instead.
- **Celebi: no.** The mythical that travels through time stands outside any stage, as Mew does.
- **Unown: no.** Twenty-eight letters, each a form of its own; an evolution would be twenty-eight more forms of a Pokémon whose one move is Hidden Power, and its lore is that it is a letter, not a creature that grows.
- **Smeargle: yes, unlike Ditto.** Ditto was a no in batch 1 because Transform copies everything but HP. Sketch copies only moves: Smeargle fights with its own stats, so a rise is felt. Its total stays the lowest of ours (330), see Balance.

## The table: every qualifying species

The user may strike rows here before reading the designs. "No." is our number; "Total" is the stats the game reads now, and ours.

| # | Was | Ours | No. | Typing (was → ours) | Total | Evolves | Say |
|---|---|---|---|---|---|---|---|
| 1 | Furret (162) | Ermilong | 2162 | Normal → **Normal/Ice** | 415 → 495 | level 106, at night | strong yes: the weakest final stage of Johto's first route (415), and a stoat that turns white in winter is a real animal's change |
| 2 | Noctowl (164) | Noctaugur | 2164 | Normal/Flying → **Psychic/Flying** | 452 → 532 | level 110, at night | yes: an owl whose Psychic moves come without the type; a seer of the night gives them one |
| 3 | Ledian (166) | Coccinova | 2166 | Bug/Flying → **Bug/Fighting** | 390 → 470 | level 106, at night | strong yes: the lowest total of any last stage of Johto's (390); it has always punched, and Iron Fist is already its hidden ability |
| 4 | Ariados (168) | Orbweavix | 2168 | Bug/Poison, kept | 400 → 480 | level 108, at night | strong yes: a total of 400 on a design that stops early; the spider that waits in its web all night has room to grow |
| 5 | Lanturn (171) | Abyssolux | 2171 | Water/Electric, kept | 460 → 540 | level 112, at night | yes: its light shines from the deepest sea; a lanternfish of the abyss is the next step down |
| 6 | Xatu (178) | Totemsight | 2178 | Psychic/Flying, kept | 470 → 550 | level 115, by day | yes: the bird that stands all day facing the sun and seeing past and future; a totem of the days to come |
| 7 | Sudowoodo (185) | Mimicedar | 2185 | Rock → **Rock/Grass** | 410 → 490 | level 108, in the rain | strong yes: the rock that pretends to be a tree and hates the rain; in the rain, at last, it takes root and becomes one |
| 8 | Sunflora (192) | Helianthra | 2192 | Grass → **Grass/Fire** | 425 → 505 | a Sun Stone used on it at level 110 or above | strong yes: a total of 425 on a sun stone's species; the same stone again, past 100, turns its face into the sun's |
| 9 | Quagsire (195) | Mirelord | 2195 | Water/Ground, kept | 430 → 510 | level 106 | yes: a total of 430; the cheerful marsh fish growing old and great at the bottom of its marsh |
| 10 | Espeon (196) | Zenitheon | 2196 | Psychic, kept | 525 → 600 | level 120, holding a Sun Stone, with friendship 220 or more (used up) | yes: with Umbreon, the Eeveelutions of Johto, built as Kanto's three were approved |
| 11 | Umbreon (197) | Eclipseon | 2197 | Dark, kept | 525 → 600 | level 120, holding a Moon Stone, with friendship 220 or more (used up) | yes: Espeon's pair |
| 12 | Slowking (199) | Slowmonarch | 2199 | Water/Psychic, kept | 490 → 570 | level 120, holding a King's Rock (used up) | maybe: the other end of Slowpoke's line, beside Kanto's approved Slowmantle; the crowned king becoming a sovereign is the obvious step, but the line would then have two evolutions past 100 |
| 13 | Wobbuffet (202) | Wobbulwark | 2202 | Psychic → **Psychic/Ghost** | 405 → 485 | level 110 | maybe: its black tail is said to be its true self; here the tail's shadow takes over. But Shadow Tag with more bulk is a strong combination (see Balance) |
| 14 | Forretress (205) | Pinecitadel | 2205 | Bug/Steel, kept | 465 → 545 | level 112 | yes: the bagworm's fortress becoming a citadel; a hazard setter that today has no reason to stay in past 100 |
| 15 | Steelix (208) | Ironwyrm | 2208 | Steel/Ground, kept | 510 → 590 | level 115, holding a Metal Coat (used up) | strong yes: deferred from Kanto (Onix's line ends in Johto); the Pokédex says it grows harder with age, and an iron snake of 510 has room |
| 16 | Granbull (210) | Mastiffang | 2210 | Fairy → **Fairy/Fighting** | 450 → 530 | level 108 | yes: the bulldog that frightens others but is timid inside, grown into the guard dog it looks like |
| 17 | Qwilfish (211) | Fugumace | 2211 | Water/Poison, kept | 440 → 520 | level 106, knowing Spikes | strong yes: the Hisuian Qwilfish went on to Overqwil and Johto's never did (Porrigard's case in Kanto) |
| 18 | Scizor (212) | Guillotyne | 2212 | Bug/Steel, kept | 500 → 580 | level 120, holding a Razor Claw (used up) | yes: deferred from Kanto (Scyther's line ends here); already strong (500) and with a Mega, but one of the best-loved Johto designs |
| 19 | Shuckle (213) | Fermentoise | 2213 | Bug/Rock, kept | 505 → 585 | level 110, holding a Leftovers (used up) | maybe: the berry keeper of the rocks; its 230 and 230 are already the highest of their kind, so a rise only goes to the stats that don't matter to it |
| 20 | Heracross (214) | Atlascross | 2214 | Bug/Fighting, kept | 500 → 580 | level 115 | strong yes: the Hercules beetle grows into the Atlas beetle, the one with three horns; its rival Pinsir was given Mandibron in Kanto |
| 21 | Magcargo (219) | Calderon | 2219 | Fire/Rock, kept | 430 → 510 | level 112 | yes: the lava snail whose shell is cooled magma; at 430 it has room, and a volcano on its back is the next step |
| 22 | Corsola (222) | Reefcrown | 2222 | Water/Rock, kept | 410 → 490 | level 106, holding a Pearl (used up) | strong yes: the Galarian Corsola went on to Cursola and Johto's never did; a living reef at a total of 410 |
| 23 | Octillery (224) | Krakannon | 2224 | Water → **Water/Fire** | 480 → 560 | level 112 | yes: the octopus that fires ink from its mouth like a cannon, and Flamethrower among its moves; a cannon that burns |
| 24 | Delibird (225) | Sleighwing | 2225 | Ice/Flying, kept | 330 → 410 | level 104, at an Ice Rock (Route 217's) | strong yes: the lowest total of any species alone in Johto (330); a bird that carries presents should, past 100, carry a sleigh's worth |
| 25 | Mantine (226) | Raysail | 2226 | Water/Flying, kept | 485 → 565 | level 112, with an Octillery in the party | yes: the manta that flies over the waves with a Remoraid on its fin; past 100 it flies higher with its grown partner |
| 26 | Skarmory (227) | Rapierwing | 2227 | Steel/Flying, kept | 465 → 545 | level 115 | yes: its feathers are blades, and people make swords of the ones it drops; a bird all blade |
| 27 | Houndoom (229) | Cerberhound | 2229 | Dark/Fire, kept | 500 → 580 | level 120, at night | strong yes: the hound whose howl is the call of the underworld; the three-headed hound of the old stories is its obvious end |
| 28 | Donphan (232) | Siegephant | 2232 | Ground → **Ground/Steel** | 500 → 580 | level 115 | yes: the armoured elephant that rolls into its foes; a war elephant in plates is the next step |
| 29 | Smeargle (235) | Muralist | 2235 | Normal, kept | 250 → 330 | level 110 | yes: unlike Ditto (no in Kanto), Smeargle copies moves and fights with its own stats, so a rise is felt; the painter becomes a master |
| 30 | Hitmontop (237) | Gyrodrill | 2237 | Fighting → **Fighting/Ground** | 455 → 535 | level 110, holding a Muscle Band (used up) | strong yes: the third of Tyrogue's ends, beside Kanto's approved Savateur and Pugilord, with the same total (535) and the same kind of condition |
| 31 | Miltank (241) | Pastorine | 2241 | Normal → **Normal/Fairy** | 490 → 570 | level 115, with friendship 220 or more | yes: Tauros grew into Bovarch in Kanto; Miltank, its counterpart and Whitney's famous one, is its pair |
| 32 | Raikou (243) | Raitenko | 2243 | Electric, kept | 580 → 660 | level 150, with a Ho-Oh in the party | maybe: the three beasts as a trio or not at all; Ho-Oh gave them life, so Ho-Oh beside them takes them further |
| 33 | Entei (244) | Enkaiou | 2244 | Fire, kept | 580 → 660 | level 150, with a Ho-Oh in the party | maybe: with the trio |
| 34 | Suicune (245) | Suiseiren | 2245 | Water, kept | 580 → 660 | level 150, with a Ho-Oh in the party | maybe: with the trio |
| – | Unown (201) | none | – | Psychic | 336 | – | – | no: twenty-eight letters, each a form; an evolution would be twenty-eight forms of a Pokémon of 336 whose one move is Hidden Power, and its lore is that it is a letter, not a stage |
| – | Lugia (249) | none | – | Psychic/Flying | 680 | – | – | no: 680, a cover legendary, as Mewtwo was |
| – | Ho-Oh (250) | none | – | Fire/Flying | 680 | – | – | no: 680, a cover legendary; it is what takes the beasts further instead |
| – | Celebi (251) | none | – | Psychic/Grass | 600 | – | – | no: the mythical that travels in time is outside any stage, as Mew was |

In all: 12 strong yes, 16 yes, 6 maybe (the three beasts as a set among them) and 4 no.

## The designs

In National Pokédex order. Each entry: the typing and why, the category word, number, size, the stats (the species' as the game reads them → ours) with where the rise goes, the abilities, the evolution with the row it would be in `species.json`, and the look.

### 1. Ermilong, from Furret (162): strong yes

- **Typing**: Normal → **Normal/Ice**. *Alternative*: Normal/Ground, a burrower.
- **The Ermine Pokémon**, No. 2162. 2.2 m, 45.0 kg (Furret 1.8 m, 32.5 kg).
- **Stats**: 85 / 76 / 64 / 45 / 55 / 90 = 415 → **100 / 101 / 74 / 45 / 65 / 110 = 495** (+15 HP +25 Atk +10 Def +10 SpD +20 Spe): the rise goes to its speed and its bite, where a stoat's are.
- **Abilities**: **Slush Rush**, Keen Eye, hidden Frisk (was Run Away, Keen Eye, hidden Frisk).
- **Evolution**: Furret at level 106, at night. Data: `{"targetSpecies":"Ermilong","method":"LevelNight","level":106}`.
- **Look**: from our long striped Furret: longer and slimmer still, its brown and cream stripes gone to a winter coat of white, the tip of its tail black (an ermine's), a darker mask round the eyes and a fringe of longer fur along the spine. **New feature**: the black-tipped tail on a white coat.

### 2. Noctaugur, from Noctowl (164): yes

- **Typing**: Normal/Flying → **Psychic/Flying**. *Alternative*: keep Normal/Flying.
- **The Augur Pokémon**, No. 2164. 2.0 m, 60.0 kg (Noctowl 1.6 m, 40.8 kg).
- **Stats**: 100 / 50 / 50 / 86 / 96 / 70 = 452 → **110 / 50 / 60 / 121 / 111 / 80 = 532** (+10 HP +10 Def +35 SpA +15 SpD +10 Spe): the rise goes to its Sp. Atk, the stat its Psychic moves use.
- **Abilities**: Insomnia, Keen Eye, hidden Tinted Lens (was Insomnia, Keen Eye, hidden Tinted Lens).
- **Evolution**: Noctowl at level 110, at night. Data: `{"targetSpecies":"Noctaugur","method":"LevelNight","level":110}`.
- **Look**: from our round brown owl on one leg: taller, its brown deepened to the blue-black of the night sky with pale bars across the wings, the two rings round its eyes grown into rings of small gold marks (decals) and a third, closed eye painted on its brow between the tufts. **New feature**: the rings of gold marks round the eyes.

### 3. Coccinova, from Ledian (166): strong yes

- **Typing**: Bug/Flying → **Bug/Fighting**. *Alternative*: Bug/Fairy, the starlight it flies by.
- **The Star Boxer Pokémon**, No. 2166. 1.6 m, 48.0 kg (Ledian 1.4 m, 35.6 kg).
- **Stats**: 55 / 35 / 50 / 55 / 110 / 85 = 390 → **75 / 75 / 60 / 55 / 110 / 95 = 470** (+20 HP +40 Atk +10 Def +10 Spe): the rise goes to its Attack, which was 35, so its punches are worth throwing.
- **Abilities**: Swarm, Early Bird, hidden Iron Fist (was Swarm, Early Bird, hidden Iron Fist).
- **Evolution**: Ledian at level 106, at night. Data: `{"targetSpecies":"Coccinova","method":"LevelNight","level":106}`.
- **Look**: from our flying ladybird with its great blue eyes and four arms in white gloves: the gloves become heavier mitts with a red cuff, the red shell broadens and its five spots glow pale yellow at night (`Glow`), and the antennae (the female's shorter) end in small stars. **New feature**: the five glowing spots on its shell.

### 4. Orbweavix, from Ariados (168): strong yes

- **Typing**: Bug/Poison, kept. *Alternative*: Bug/Dark, a hunter of the night.
- **The Orb Weaver Pokémon**, No. 2168. 1.6 m, 60.0 kg (Ariados 1.1 m, 33.5 kg).
- **Stats**: 70 / 90 / 70 / 60 / 70 / 40 = 400 → **90 / 115 / 85 / 60 / 80 / 50 = 480** (+20 HP +25 Atk +15 Def +10 SpD +10 Spe): the rise goes to its Attack and its bulk, where a trap-layer needs them.
- **Abilities**: **Stakeout**, Insomnia, hidden Sniper (was Swarm, Insomnia, hidden Sniper).
- **Evolution**: Ariados at level 108, at night. Data: `{"targetSpecies":"Orbweavix","method":"LevelNight","level":108}`.
- **Look**: from our red spider on its long banded legs: larger and heavier in the abdomen, which bears a spiral web pattern in purple and yellow, the white horn longer, and a small round web strung between its hind legs that it carries (thin strands, white). **New feature**: the web it carries.

### 5. Abyssolux, from Lanturn (171): yes

- **Typing**: Water/Electric, kept. *Alternative*: Water/Ghost, the drowned light.
- **The Abyss Light Pokémon**, No. 2171. 1.8 m, 45.0 kg (Lanturn 1.2 m, 22.5 kg).
- **Stats**: 125 / 58 / 58 / 76 / 76 / 67 = 460 → **135 / 58 / 73 / 106 / 96 / 72 = 540** (+10 HP +15 Def +30 SpA +20 SpD +5 Spe): the rise goes to its Sp. Atk and its Defense; HP was already 125.
- **Abilities**: Volt Absorb, Illuminate, hidden Water Absorb (was Volt Absorb, Illuminate, hidden Water Absorb).
- **Evolution**: Lanturn at level 112, at night. Data: `{"targetSpecies":"Abyssolux","method":"LevelNight","level":112}`.
- **Look**: from our blue anglerfish with its two lights on stalks: longer and darker, deep navy with a pale belly, a jaw of thin teeth, the two lights joined by a third on its brow (`Glow`, yellow and pale blue), and rows of small light points along its flanks (decals). **New feature**: the rows of lights along its flanks.

### 6. Totemsight, from Xatu (178): yes

- **Typing**: Psychic/Flying, kept. *Alternative*: Psychic/Fairy.
- **The Totem Pokémon**, No. 2178. 2.0 m, 30.0 kg (Xatu 1.5 m, 15.0 kg).
- **Stats**: 65 / 75 / 70 / 95 / 70 / 95 = 470 → **80 / 75 / 80 / 125 / 90 / 100 = 550** (+15 HP +10 Def +30 SpA +20 SpD +5 Spe): the rise goes to its Sp. Atk and its Sp. Def, the stats a seer lives by.
- **Abilities**: Synchronize, Early Bird, hidden Magic Bounce (was Synchronize, Early Bird, hidden Magic Bounce).
- **Evolution**: Xatu at level 115, by day. Data: `{"targetSpecies":"Totemsight","method":"LevelDay","level":115}`.
- **Look**: from our standing green bird with its wings folded like a cloak: taller and straighter, the patterns on its folded wings grown into a second, carved face below its own, as on a totem pole, and two long red and white tail feathers. The female's green stripes are kept. **New feature**: the second face on its folded wings.

### 7. Mimicedar, from Sudowoodo (185): strong yes

- **Typing**: Rock → **Rock/Grass**. *Alternative*: keep Rock alone.
- **The True Tree Pokémon**, No. 2185. 1.8 m, 95.0 kg (Sudowoodo 1.2 m, 38.0 kg).
- **Stats**: 70 / 100 / 115 / 30 / 65 / 30 = 410 → **90 / 125 / 135 / 30 / 80 / 30 = 490** (+20 HP +25 Atk +20 Def +15 SpD): the rise goes to its Attack, Defense and HP, a trunk's bulk.
- **Abilities**: Sturdy, Rock Head, hidden **Sap Sipper** (was Sturdy, Rock Head, hidden Rattled).
- **Evolution**: Sudowoodo at level 108, in the rain. Data: `{"targetSpecies":"Mimicedar","method":"LevelInRain","level":108}`.
- **Look**: from our Sudowoodo with its arms out like branches: taller, its brown body cracked like bark over stone, the green balls on its arms and head grown into real leafy crowns (`Leaf`), and roots spreading at its feet. **New feature**: the leafy crowns where the green balls were.

### 8. Helianthra, from Sunflora (192): strong yes

- **Typing**: Grass → **Grass/Fire**. *Alternative*: Grass/Fairy.
- **The Sun Disc Pokémon**, No. 2192. 1.4 m, 25.0 kg (Sunflora 0.8 m, 8.5 kg).
- **Stats**: 75 / 75 / 55 / 105 / 85 / 30 = 425 → **85 / 75 / 70 / 135 / 100 / 40 = 505** (+10 HP +15 Def +30 SpA +15 SpD +10 Spe): the rise goes to its Sp. Atk, which Solar Power multiplies.
- **Abilities**: Chlorophyll, Solar Power, hidden Early Bird (was Chlorophyll, Solar Power, hidden Early Bird).
- **Evolution**: Sunflora at a Sun Stone used on it at level 110 or above. Data: `{"targetSpecies":"Helianthra","method":"UseItem","level":110,"item":"Sun Stone"}`.
- **Look**: from our Sunflora: a ring of petals twice as wide and gold to orange, its face in a disc that glows like the sun (`Glow`, warm yellow), two leaves for arms and a crown of small rays round the petals. **New feature**: the glowing disc.

### 9. Mirelord, from Quagsire (195): yes

- **Typing**: Water/Ground, kept. *Alternative*: Water/Poison (the Paldean line's Clodsire took that way).
- **The Bog Lord Pokémon**, No. 2195. 1.8 m, 120.0 kg (Quagsire 1.4 m, 75.0 kg).
- **Stats**: 95 / 85 / 85 / 65 / 65 / 35 = 430 → **120 / 105 / 100 / 65 / 85 / 35 = 510** (+25 HP +20 Atk +15 Def +20 SpD): the rise goes to its HP, Attack and Sp. Def.
- **Abilities**: Damp, Water Absorb, hidden Unaware (was Damp, Water Absorb, hidden Unaware).
- **Evolution**: Quagsire at level 106. Data: `{"targetSpecies":"Mirelord","level":106}`.
- **Look**: from our blue upright Quagsire with its round face: broader and heavier, moss and reeds growing on its head and back (`Leaf`, soft), the fins on its head grown into long frilled gills like an axolotl's, and the female's smaller back fins kept. **New feature**: the frilled gills.

### 10. Zenitheon, from Espeon (196): yes

- **Typing**: Psychic, kept. *Alternative*: none for this species.
- **The Zenith Pokémon**, No. 2196. 1.1 m, 32.0 kg (Espeon 0.9 m, 26.5 kg).
- **Stats**: 65 / 65 / 60 / 130 / 95 / 110 = 525 → **85 / 65 / 75 / 140 / 110 / 125 = 600** (+20 HP +15 Def +10 SpA +15 SpD +15 Spe): the rise goes to its HP and bulk; its Sp. Atk was already 130.
- **Abilities**: Synchronize, hidden Magic Bounce (was Synchronize, hidden Magic Bounce).
- **Evolution**: Espeon at level 120, holding a Sun Stone, with friendship 220 or more (used up). Data: `{"targetSpecies":"Zenitheon","method":"LevelHoldingItem","level":120,"item":"Sun Stone","needsFriendship":true}`.
- **Look**: from our Espeon: longer and finer, its forked tail grown into three tails, the red gem on its brow ringed with a small halo of light (`Glow`, pale gold), and the fur at its cheeks swept back. **New feature**: the three tails.

### 11. Eclipseon, from Umbreon (197): yes

- **Typing**: Dark, kept. *Alternative*: none for this species.
- **The Eclipse Pokémon**, No. 2197. 1.2 m, 33.0 kg (Umbreon 1.0 m, 27.0 kg).
- **Stats**: 95 / 65 / 110 / 60 / 130 / 65 = 525 → **115 / 65 / 125 / 60 / 140 / 95 = 600** (+20 HP +15 Def +10 SpD +30 Spe): the rise goes to its Speed and HP; it was already the bulkiest Eeveelution.
- **Abilities**: Synchronize, hidden Inner Focus (was Synchronize, hidden Inner Focus).
- **Evolution**: Umbreon at level 120, holding a Moon Stone, with friendship 220 or more (used up). Data: `{"targetSpecies":"Eclipseon","method":"LevelHoldingItem","level":120,"item":"Moon Stone","needsFriendship":true}`.
- **Look**: from our Umbreon: larger, its gold rings turned to thin crescents of an eclipse (`Glow`, pale gold rims round dark discs), a mane of black fur at the neck, and the ears longer. **New feature**: the eclipse rings.

### 12. Slowmonarch, from Slowking (199): maybe

- **Typing**: Water/Psychic, kept. *Alternative*: Psychic/Steel.
- **The Sovereign Pokémon**, No. 2199. 2.4 m, 110.0 kg (Slowking 2.0 m, 79.5 kg).
- **Stats**: 95 / 75 / 80 / 100 / 110 / 30 = 490 → **110 / 75 / 100 / 125 / 130 / 30 = 570** (+15 HP +20 Def +25 SpA +20 SpD): the rise goes to its Sp. Atk and Sp. Def, a ruler's mind.
- **Abilities**: Oblivious, Own Tempo, hidden Regenerator (was Oblivious, Own Tempo, hidden Regenerator).
- **Evolution**: Slowking at level 120, holding a King's Rock (used up). Data: `{"targetSpecies":"Slowmonarch","method":"LevelHoldingItem","level":120,"item":"King's Rock"}`.
- **Look**: from our Slowking standing tall with its white ruff and its Shellder crown: taller, the ruff grown into a royal cape of pink and white, the Shellder on its head grown into a second, larger shell with three jewels, and a staff of coral in its hand. **New feature**: the cape.

### 13. Wobbulwark, from Wobbuffet (202): maybe

- **Typing**: Psychic → **Psychic/Ghost**. *Alternative*: keep Psychic alone.
- **The Mirror Pokémon**, No. 2202. 1.6 m, 40.0 kg (Wobbuffet 1.3 m, 28.5 kg).
- **Stats**: 190 / 33 / 58 / 33 / 58 / 33 = 405 → **200 / 33 / 88 / 33 / 88 / 43 = 485** (+10 HP +30 Def +30 SpD +10 Spe): the rise goes to its Defense and Sp. Def, which Counter and Mirror Coat live by.
- **Abilities**: Shadow Tag, hidden Telepathy (was Shadow Tag, hidden Telepathy).
- **Evolution**: Wobbuffet at level 110. Data: `{"targetSpecies":"Wobbulwark","level":110}`.
- **Look**: from our blue Wobbuffet: larger, the black tail with its two eyes grown into a dark twin of the body that leans over it (`Glow` in its eyes), and a mirror-like sheen on its front. The female's lips are kept. **New feature**: the dark twin of the tail.

### 14. Pinecitadel, from Forretress (205): yes

- **Typing**: Bug/Steel, kept. *Alternative*: Steel/Rock.
- **The Citadel Pokémon**, No. 2205. 1.6 m, 250.0 kg (Forretress 1.2 m, 125.8 kg).
- **Stats**: 75 / 90 / 140 / 60 / 60 / 40 = 465 → **90 / 100 / 150 / 60 / 85 / 60 = 545** (+15 HP +10 Atk +10 Def +25 SpD +20 Spe): the rise goes to its HP and Sp. Def, its weak side.
- **Abilities**: Sturdy, hidden **Iron Barbs** (was Sturdy, hidden Overcoat).
- **Evolution**: Forretress at level 112. Data: `{"targetSpecies":"Pinecitadel","level":112}`.
- **Look**: from our round shelled Forretress: larger, its shell plates layered into towers with battlements on top, the gaps between them showing a glowing core (`Glow`, orange), and steel spikes at the plates' corners. **New feature**: the battlements.

### 15. Ironwyrm, from Steelix (208): strong yes

- **Typing**: Steel/Ground, kept. *Alternative*: Steel/Dragon.
- **The Iron Wyrm Pokémon**, No. 2208. 11.0 m, 600.0 kg (Steelix 9.2 m, 400.0 kg).
- **Stats**: 75 / 85 / 200 / 55 / 65 / 30 = 510 → **95 / 115 / 200 / 55 / 90 / 35 = 590** (+20 HP +30 Atk +25 SpD +5 Spe): the rise goes to its Attack, HP and Sp. Def; Defense stays 200.
- **Abilities**: Rock Head, Sturdy, hidden Sheer Force (was Rock Head, Sturdy, hidden Sheer Force).
- **Evolution**: Steelix at level 115, holding a Metal Coat (used up). Data: `{"targetSpecies":"Ironwyrm","method":"LevelHoldingItem","level":115,"item":"Metal Coat"}`.
- **Look**: from our grey iron snake of boulders: longer, each boulder faceted and grown with clear crystals at its joints (`Glow`, faint white), the jaw broader with the female's teeth kept, and a crest of crystal on the head. **New feature**: the crystals at its joints.

### 16. Mastiffang, from Granbull (210): yes

- **Typing**: Fairy → **Fairy/Fighting**. *Alternative*: Fairy/Dark.
- **The Bulldog Pokémon**, No. 2210. 1.8 m, 80.0 kg (Granbull 1.4 m, 48.7 kg).
- **Stats**: 90 / 120 / 75 / 60 / 60 / 45 = 450 → **110 / 140 / 90 / 60 / 75 / 55 = 530** (+20 HP +20 Atk +15 Def +15 SpD +10 Spe): the rise goes to its Attack and HP.
- **Abilities**: Intimidate, Quick Feet, hidden **Strong Jaw** (was Intimidate, Quick Feet, hidden Rattled).
- **Evolution**: Granbull at level 108. Data: `{"targetSpecies":"Mastiffang","level":108}`.
- **Look**: from our purple Granbull with its great underbite: heavier in the chest, a studded collar (`Metal`), the fangs longer and the jowls fuller, and a short cape of fur on its shoulders. **New feature**: the studded collar.

### 17. Fugumace, from Qwilfish (211): strong yes

- **Typing**: Water/Poison, kept. *Alternative*: Water/Steel, the spikes become iron.
- **The Spike Ball Pokémon**, No. 2211. 1.0 m, 25.0 kg (Qwilfish 0.5 m, 3.9 kg).
- **Stats**: 65 / 95 / 85 / 55 / 55 / 85 = 440 → **85 / 120 / 100 / 55 / 65 / 95 = 520** (+20 HP +25 Atk +15 Def +10 SpD +10 Spe): the rise goes to its Attack, HP and Defense.
- **Abilities**: Poison Point, Swift Swim, hidden Intimidate (was Poison Point, Swift Swim, hidden Intimidate).
- **Evolution**: Qwilfish at level 106, knowing Spikes. Data: `{"targetSpecies":"Fugumace","method":"LevelKnowsMove","level":106,"move":"Spikes"}`.
- **Look**: from our round puffer bristling with spikes: larger and always half puffed, the spikes longer and tipped purple, a band of thicker spikes round its middle like a mace's flanges, and its round tail fin wider. **New feature**: the band of flange spikes.

### 18. Guillotyne, from Scizor (212): yes

- **Typing**: Bug/Steel, kept. *Alternative*: Steel/Fighting.
- **The Iron Pincer Pokémon**, No. 2212. 2.0 m, 140.0 kg (Scizor 1.8 m, 118.0 kg).
- **Stats**: 70 / 130 / 100 / 55 / 80 / 65 = 500 → **85 / 150 / 115 / 55 / 90 / 85 = 580** (+15 HP +20 Atk +15 Def +10 SpD +20 Spe): the rise goes to its Attack, HP and Speed.
- **Abilities**: Swarm, Technician, hidden **Tough Claws** (was Swarm, Technician, hidden Light Metal).
- **Evolution**: Scizor at level 120, holding a Razor Claw (used up). Data: `{"targetSpecies":"Guillotyne","method":"LevelHoldingItem","level":120,"item":"Razor Claw"}`.
- **Look**: from our red Scizor with its two pincers: taller and leaner, the pincers grown longer with a blade along each jaw (`Metal`, silver edge), the wings longer, and a crest of three points on its head. The female's thicker abdomen is kept. **New feature**: the bladed pincers.

### 19. Fermentoise, from Shuckle (213): maybe

- **Typing**: Bug/Rock, kept. *Alternative*: Bug/Grass.
- **The Ferment Pokémon**, No. 2213. 1.0 m, 40.0 kg (Shuckle 0.6 m, 20.5 kg).
- **Stats**: 20 / 10 / 230 / 10 / 230 / 5 = 505 → **50 / 30 / 230 / 30 / 230 / 15 = 585** (+30 HP +20 Atk +20 SpA +10 Spe): the rise goes to its HP and Attack, the stats it lacks.
- **Abilities**: Sturdy, Gluttony, hidden Contrary (was Sturdy, Gluttony, hidden Contrary).
- **Evolution**: Shuckle at level 110, holding a Leftovers (used up). Data: `{"targetSpecies":"Fermentoise","method":"LevelHoldingItem","level":110,"item":"Leftovers"}`.
- **Look**: from our yellow Shuckle in its red shell: the shell grown into a cluster of shells like a hive, berries stored and fermenting in their openings (`Glow`, faint red and purple), and its yellow body longer. **New feature**: the berries in the shell's openings.

### 20. Atlascross, from Heracross (214): strong yes

- **Typing**: Bug/Fighting, kept. *Alternative*: Bug/Steel.
- **The Atlas Pokémon**, No. 2214. 1.9 m, 90.0 kg (Heracross 1.5 m, 54.0 kg).
- **Stats**: 80 / 125 / 75 / 40 / 95 / 85 = 500 → **95 / 145 / 95 / 40 / 105 / 100 = 580** (+15 HP +20 Atk +20 Def +10 SpD +15 Spe): the rise goes to its Attack, Defense and Speed.
- **Abilities**: Swarm, Guts, hidden Moxie (was Swarm, Guts, hidden Moxie).
- **Evolution**: Heracross at level 115. Data: `{"targetSpecies":"Atlascross","level":115}`.
- **Look**: from our blue beetle standing on two legs: larger, the great horn joined by two shorter horns from the shoulders (the Atlas beetle's three), the female's heart-shaped tip kept on its middle horn, and a darker blue shell with a metallic sheen (`Shell`). **New feature**: the two shoulder horns.

### 21. Calderon, from Magcargo (219): yes

- **Typing**: Fire/Rock, kept. *Alternative*: Fire/Ground.
- **The Caldera Pokémon**, No. 2219. 1.4 m, 150.0 kg (Magcargo 0.8 m, 55.0 kg).
- **Stats**: 60 / 50 / 120 / 90 / 80 / 30 = 430 → **80 / 55 / 135 / 105 / 95 / 40 = 510** (+20 HP +5 Atk +15 Def +15 SpA +15 SpD +10 Spe): the rise goes to its HP, Sp. Atk and Defense.
- **Abilities**: Magma Armor, Flame Body, hidden Weak Armor (was Magma Armor, Flame Body, hidden Weak Armor).
- **Evolution**: Magcargo at level 112. Data: `{"targetSpecies":"Calderon","level":112}`.
- **Look**: from our lava snail with its rocky shell: the shell grown into a small volcano cone with a crater at its top glowing and smoking (`Glow`, orange), and the snail's body brighter with cooling crust on its back. **New feature**: the crater on its shell.

### 22. Reefcrown, from Corsola (222): strong yes

- **Typing**: Water/Rock, kept. *Alternative*: Water/Fairy.
- **The Coral Reef Pokémon**, No. 2222. 1.2 m, 40.0 kg (Corsola 0.6 m, 5.0 kg).
- **Stats**: 65 / 55 / 95 / 65 / 95 / 35 = 410 → **85 / 55 / 115 / 80 / 115 / 40 = 490** (+20 HP +20 Def +15 SpA +20 SpD +5 Spe): the rise goes to its Defense, Sp. Def and Sp. Atk.
- **Abilities**: Hustle, Natural Cure, hidden Regenerator (was Hustle, Natural Cure, hidden Regenerator).
- **Evolution**: Corsola at level 106, holding a Pearl (used up). Data: `{"targetSpecies":"Reefcrown","method":"LevelHoldingItem","level":106,"item":"Pearl"}`.
- **Look**: from our pink coral Corsola: larger, its branches grown into a crown of coral in pink, white and orange with small fish-like knobs on it, a pearl set at the crown's middle, and its body broader. **New feature**: the pearl in the crown.

### 23. Krakannon, from Octillery (224): yes

- **Typing**: Water → **Water/Fire**. *Alternative*: Water/Steel.
- **The Artillery Pokémon**, No. 2224. 1.6 m, 70.0 kg (Octillery 0.9 m, 28.5 kg).
- **Stats**: 75 / 105 / 75 / 105 / 75 / 45 = 480 → **90 / 125 / 85 / 125 / 85 / 50 = 560** (+15 HP +20 Atk +10 Def +20 SpA +10 SpD +5 Spe): the rise goes to both attacking stats, as its Octazooka and Flamethrower share them.
- **Abilities**: Suction Cups, Sniper, hidden **Mega Launcher** (was Suction Cups, Sniper, hidden Moody).
- **Evolution**: Octillery at level 112. Data: `{"targetSpecies":"Krakannon","level":112}`.
- **Look**: from our red octopus with its tube of a mouth: larger, its mouth grown into a longer barrel ringed with darker bands, smoke at its rim, and its eight arms thicker with suckers glowing on their undersides (`Glow`, orange). The female's shorter fringe is kept. **New feature**: the ringed barrel.

### 24. Sleighwing, from Delibird (225): strong yes

- **Typing**: Ice/Flying, kept. *Alternative*: Ice/Fairy.
- **The Courier Pokémon**, No. 2225. 1.4 m, 35.0 kg (Delibird 0.9 m, 16.0 kg).
- **Stats**: 45 / 55 / 45 / 65 / 45 / 75 = 330 → **60 / 70 / 55 / 80 / 55 / 90 = 410** (+15 HP +15 Atk +10 Def +15 SpA +10 SpD +15 Spe): the rise goes to spread over every stat, since it has no strong one; it stays the weakest of ours (see Balance).
- **Abilities**: Vital Spirit, Hustle, hidden **Pickup** (was Vital Spirit, Hustle, hidden Insomnia).
- **Evolution**: Delibird at level 104, at an Ice Rock (Route 217's). Data: `{"targetSpecies":"Sleighwing","method":"LevelAtLocation","level":104,"location":"Ice Rock"}`.
- **Look**: from our red and white Delibird carrying its tail as a sack: taller, its wings broader and able to fly, the sack grown into a larger bag with a strap over its shoulder, a pair of goggles on its brow (`Metal` rims), and a scarf. **New feature**: the goggles.

### 25. Raysail, from Mantine (226): yes

- **Typing**: Water/Flying, kept. *Alternative*: Water/Psychic.
- **The Sky Kite Pokémon**, No. 2226. 3.0 m, 350.0 kg (Mantine 2.1 m, 220.0 kg).
- **Stats**: 85 / 40 / 70 / 80 / 140 / 70 = 485 → **105 / 50 / 85 / 100 / 145 / 80 = 565** (+20 HP +10 Atk +15 Def +20 SpA +5 SpD +10 Spe): the rise goes to its HP, Sp. Atk and Defense.
- **Abilities**: Swift Swim, Water Absorb, hidden Water Veil (was Swift Swim, Water Absorb, hidden Water Veil).
- **Evolution**: Mantine at level 112, with an Octillery in the party. Data: `{"targetSpecies":"Raysail","method":"LevelWithSpeciesInParty","level":112,"species":"Octillery"}`.
- **Look**: from our wide purple manta with its two horn-like fins: wider, the wing tips trailing long pale streamers, the horns longer, and a pattern of pale stars on its back (decals). **New feature**: the streamers at its wing tips.

### 26. Rapierwing, from Skarmory (227): yes

- **Typing**: Steel/Flying, kept. *Alternative*: Steel/Dragon.
- **The Sword Bird Pokémon**, No. 2227. 2.2 m, 80.0 kg (Skarmory 1.7 m, 50.5 kg).
- **Stats**: 65 / 80 / 140 / 40 / 70 / 70 = 465 → **80 / 105 / 145 / 40 / 85 / 90 = 545** (+15 HP +25 Atk +5 Def +15 SpD +20 Spe): the rise goes to its Attack, Speed and HP.
- **Abilities**: Keen Eye, Sturdy, hidden Weak Armor (was Keen Eye, Sturdy, hidden Weak Armor).
- **Evolution**: Skarmory at level 115. Data: `{"targetSpecies":"Rapierwing","level":115}`.
- **Look**: from our lean steel bird with red blades for flight feathers: larger, the red blades longer and narrower like rapiers, its crest a long blade swept back, and its tail feathers in a fan of blades. **New feature**: the fan of tail blades.

### 27. Cerberhound, from Houndoom (229): strong yes

- **Typing**: Dark/Fire, kept. *Alternative*: Dark/Ghost.
- **The Hellhound Pokémon**, No. 2229. 1.8 m, 70.0 kg (Houndoom 1.4 m, 35.0 kg).
- **Stats**: 75 / 90 / 50 / 110 / 80 / 95 = 500 → **85 / 100 / 70 / 135 / 90 / 100 = 580** (+10 HP +10 Atk +20 Def +25 SpA +10 SpD +5 Spe): the rise goes to its Sp. Atk and its Defense.
- **Abilities**: Early Bird, Flash Fire, hidden Unnerve (was Early Bird, Flash Fire, hidden Unnerve).
- **Evolution**: Houndoom at level 120, at night. Data: `{"targetSpecies":"Cerberhound","method":"LevelNight","level":120}`.
- **Look**: from our black hound with its curved horns and bone bands: two smaller heads from its shoulders beside its own, each with short horns (the female's shorter), flames at their mouths (`Glow`), and its tail ending in a longer arrowhead. **New feature**: the two heads at its shoulders.

### 28. Siegephant, from Donphan (232): yes

- **Typing**: Ground → **Ground/Steel**. *Alternative*: Ground/Rock.
- **The War Elephant Pokémon**, No. 2232. 1.8 m, 320.0 kg (Donphan 1.1 m, 120.0 kg).
- **Stats**: 90 / 120 / 120 / 60 / 60 / 50 = 500 → **110 / 140 / 135 / 60 / 75 / 60 = 580** (+20 HP +20 Atk +15 Def +15 SpD +10 Spe): the rise goes to its Attack and HP.
- **Abilities**: Sturdy, hidden **Stamina** (was Sturdy, hidden Sand Veil).
- **Evolution**: Donphan at level 115. Data: `{"targetSpecies":"Siegephant","level":115}`.
- **Look**: from our grey armoured elephant: its plates grown thicker and edged with steel (`Metal`), a howdah-like shell on its back, its tusks (the female's shorter) capped in steel and its trunk longer. **New feature**: the shell on its back.

### 29. Muralist, from Smeargle (235): yes

- **Typing**: Normal, kept. *Alternative*: Normal/Fairy.
- **The Fresco Pokémon**, No. 2235. 1.5 m, 70.0 kg (Smeargle 1.2 m, 58.0 kg).
- **Stats**: 55 / 20 / 35 / 20 / 45 / 75 = 250 → **70 / 35 / 50 / 35 / 60 / 80 = 330** (+15 HP +15 Atk +15 Def +15 SpA +15 SpD +5 Spe): the rise goes to spread evenly, since it can learn anything and so needs everything.
- **Abilities**: Own Tempo, Technician, hidden Moody (was Own Tempo, Technician, hidden Moody).
- **Evolution**: Smeargle at level 110. Data: `{"targetSpecies":"Muralist","level":110}`.
- **Look**: from our Smeargle with its paint-tipped tail: taller, a beret, a painter's smock of paint-flecked fur, and its tail grown into a longer brush whose tip changes colour by the trainer's number (like Smeargle's own). **New feature**: the beret.

### 30. Gyrodrill, from Hitmontop (237): strong yes

- **Typing**: Fighting → **Fighting/Ground**. *Alternative*: Fighting/Steel.
- **The Gyro Pokémon**, No. 2237. 1.6 m, 60.0 kg (Hitmontop 1.4 m, 48.0 kg).
- **Stats**: 50 / 95 / 95 / 35 / 110 / 70 = 455 → **75 / 115 / 110 / 35 / 115 / 85 = 535** (+25 HP +20 Atk +15 Def +5 SpD +15 Spe): the rise goes to its HP, Attack and Defense, as the other two.
- **Abilities**: Intimidate, Technician, hidden Steadfast (was Intimidate, Technician, hidden Steadfast).
- **Evolution**: Hitmontop at level 110, holding a Muscle Band (used up). Data: `{"targetSpecies":"Gyrodrill","method":"LevelHoldingItem","level":110,"item":"Muscle Band"}`.
- **Look**: from our Hitmontop spinning on its head: the horn grown into a drill with a spiral (`Shell`, brown), a broader ring of armour at its waist, and its tail longer and banded. **New feature**: the drill horn.

### 31. Pastorine, from Miltank (241): yes

- **Typing**: Normal → **Normal/Fairy**. *Alternative*: Normal/Ground, as Bovarch.
- **The Pasture Pokémon**, No. 2241. 1.4 m, 110.0 kg (Miltank 1.2 m, 75.5 kg).
- **Stats**: 95 / 80 / 105 / 40 / 70 / 100 = 490 → **115 / 95 / 120 / 40 / 90 / 110 = 570** (+20 HP +15 Atk +15 Def +20 SpD +10 Spe): the rise goes to its HP, Sp. Def and Speed.
- **Abilities**: Thick Fat, Scrappy, hidden Sap Sipper (was Thick Fat, Scrappy, hidden Sap Sipper).
- **Evolution**: Miltank at level 115, with friendship 220 or more. Data: `{"targetSpecies":"Pastorine","method":"Friendship","level":115}`.
- **Look**: from our pink Miltank on two legs: larger, a bell on a collar (`Metal`, gold), its horns longer and curled, its black and pink markings kept, and a garland of flowers round its neck. **New feature**: the bell.

### 32. Raitenko, from Raikou (243): maybe

- **Typing**: Electric, kept. *Alternative*: none of the three.
- **The Thunderclap Pokémon**, No. 2243. 2.2 m, 220.0 kg (Raikou 1.9 m, 178.0 kg).
- **Stats**: 90 / 85 / 75 / 115 / 100 / 115 = 580 → **105 / 85 / 85 / 145 / 110 / 130 = 660** (+15 HP +10 Def +30 SpA +10 SpD +15 Spe): the rise goes to its Sp. Atk and Speed.
- **Abilities**: Pressure, hidden **Electric Surge** (was Pressure, hidden Inner Focus).
- **Evolution**: Raikou at level 150, with a Ho-Oh in the party. Data: `{"targetSpecies":"Raitenko","method":"LevelWithSpeciesInParty","level":150,"species":"Ho-Oh"}`.
- **Look**: from our yellow tiger with its storm-cloud mane: larger, the mane grown into a towering cloud with lightning in it (`Glow`, white), its fangs longer and its stripes jagged as bolts. **New feature**: the lightning in its mane.

### 33. Enkaiou, from Entei (244): maybe

- **Typing**: Fire, kept. *Alternative*: none of the three.
- **The Eruption Pokémon**, No. 2244. 2.4 m, 240.0 kg (Entei 2.1 m, 198.0 kg).
- **Stats**: 115 / 115 / 85 / 90 / 75 / 100 = 580 → **130 / 145 / 95 / 90 / 85 / 115 = 660** (+15 HP +30 Atk +10 Def +10 SpD +15 Spe): the rise goes to its Attack, HP and Speed.
- **Abilities**: Pressure, hidden **Drought** (was Pressure, hidden Inner Focus).
- **Evolution**: Entei at level 150, with a Ho-Oh in the party. Data: `{"targetSpecies":"Enkaiou","method":"LevelWithSpeciesInParty","level":150,"species":"Ho-Oh"}`.
- **Look**: from our brown lion with its grey mane and bone-like plates: larger, the mane grown into a mantle of smoke over a ridge of glowing rock (`Glow`, orange), the plate on its face a crown, and its legs heavier. **New feature**: the ridge of glowing rock.

### 34. Suiseiren, from Suicune (245): maybe

- **Typing**: Water, kept. *Alternative*: none of the three.
- **The Purity Pokémon**, No. 2245. 2.2 m, 200.0 kg (Suicune 2.0 m, 187.0 kg).
- **Stats**: 100 / 75 / 115 / 90 / 115 / 85 = 580 → **115 / 75 / 130 / 110 / 130 / 100 = 660** (+15 HP +15 Def +20 SpA +15 SpD +15 Spe): the rise goes to its Sp. Atk, Defense and Sp. Def.
- **Abilities**: Pressure, hidden **Misty Surge** (was Pressure, hidden Inner Focus).
- **Evolution**: Suicune at level 150, with a Ho-Oh in the party. Data: `{"targetSpecies":"Suiseiren","method":"LevelWithSpeciesInParty","level":150,"species":"Ho-Oh"}`.
- **Look**: from our blue beast with its white streamers and crystal crest: larger, the two streamers grown into four that ripple like water, the crest a clear crown, and a mist at its feet (`Glow`, soft blue). **New feature**: the four streamers.

## Balance

Measured by the script, as batch 1's:

- **Totals**: 330 (Muralist) to 600 (Zenitheon and Eclipseon, the two Eeveelutions, as batch 1's three), the beasts 660. Nothing that isn't legendary passes 600, and only the two Eeveelutions reach it.
- **Highest of each stat**: Attack 150 (Guillotyne), the same as batch 1's Pincerex and Mandibron, then Atlascross and Enkaiou at 145. Sp. Atk 145 (Raitenko), then Zenitheon's 140, under batch 1's birds' 150. Speed 130 (Raitenko), then Zenitheon's 125. Defense and Sp. Def 230 (Fermentoise: Shuckle's own, unchanged). HP 200 (Wobbulwark, from Wobbuffet's 190).
- **The weakest stay modest on purpose** (batch 1's question 4 was answered with no floor): Muralist 330 and Sleighwing 410 are the two lowest of either batch, under batch 1's Porrigard (457). Smeargle's worth is Sketch, and Delibird's 330 leaves even +80 low; if the user wants either higher, that is a change to its row, not to the rule.

**Flagged as strong** (each inside the official range; the user may want one turned down):
1. **Wobbulwark**: Shadow Tag traps whatever it meets, and with 200 HP and 88 in both defences its Counter and Mirror Coat return more. Wobbuffet itself is allowed at the Battle Frontier (`FrontierRules` bans eighteen, not it). *To turn down*: keep Psychic alone, or give the 30s of its defences to HP.
2. **Guillotyne**: Technician with 150 Attack makes Bullet Punch the strongest priority move of either batch. *To turn down*: Attack 140, Defense 125.
3. **Helianthra**: Solar Power with 135 Sp. Atk under batch 1's Kitsunox's Drought or Enkaiou's. Slow (40) and weak to Rock, Flying and Poison, so it is kept in check. *To turn down*: Sp. Atk 125, Sp. Def 110.
4. **Krakannon**: Mega Launcher with 125 Sp. Atk on Water/Fire, a typing with three weaknesses. *To turn down*: Moody kept as the hidden ability.

**Weaker by design**: Fermentoise (its rise goes to stats Shuckle doesn't use) and Muralist (its rise is spread over every stat) are judged on their designs more than their numbers.

## Open questions: only what is new for Johto

Batch 1's ten are settled and applied (above). These are new.

1. **Espeon and Umbreon.** *Default*: they join batch 1's three Eeveelutions (each level 120 holding its stone, friendship 220 or more, 600): a Sun Stone for Espeon, a Moon Stone for Umbreon. Leafeon and Glaceon wait for Sinnoh's batch and Sylveon for Kalos's, by the same pattern, so all eight are alike in the end. *Alternative*: none of Eevee's ends evolve, and batch 1's three are taken back.
2. **The beasts.** *Default*: build them as a trio, by a Ho-Oh in the party at level 150. *Alternatives*: by holding an item each (as batch 1's birds), or not at all. Lugia, Ho-Oh and Celebi: no, as Mewtwo and Mew.
3. **Unown.** *Default*: no (above). *Alternative*: one evolution, the same for every letter (a "word" of three Unown), with no forms of its own.
4. **Both of Slowpoke's ends.** Batch 1 approved Slowmantle from Slowbro. *Default*: Slowking evolves too (Slowmonarch, a maybe), so the line has two evolutions past 100, one each. *Alternative*: only Slowbro's.
5. **Wobbulwark at the Frontier.** *Default*: it may be entered, as Wobbuffet may. *Alternative*: it joins the Frontier's ban list beside the beasts' evolutions.
6. **A Sinnoh place for a Johto species.** Sleighwing evolves at Route 217's Ice Rock, which means flying back to Sinnoh from Johto (plan 18 · W9). *Default*: kept, since the Ice Rock is the one place of snow the data names. *Alternative*: level 104 holding a Never-Melt Ice, as batch 1's Glacitusk.
7. **Two designs that need another species.** Raysail needs an Octillery in the party (Mantine's own condition, Mantyke's Remoraid, grown up) and the beasts need a Ho-Oh. *Default*: kept, as the data's own Mantine row does.

## The table to answer

One row a species. The user answers each with yes, a change, or no; striking a row in the selection table above is a no.

| # | Ours | Typing | Abilities | HP/Atk/Def/SpA/SpD/Spe (total) | Δ | Evolves |
|---|---|---|---|---|---|---|
| 1 | Ermilong | Normal/Ice | Slush Rush, Keen Eye, Frisk | 100/101/74/45/65/110 (495) | +15 HP +25 Atk +10 Def +10 SpD +20 Spe | level 106, at night |
| 2 | Noctaugur | Psychic/Flying | Insomnia, Keen Eye, Tinted Lens | 110/50/60/121/111/80 (532) | +10 HP +10 Def +35 SpA +15 SpD +10 Spe | level 110, at night |
| 3 | Coccinova | Bug/Fighting | Swarm, Early Bird, Iron Fist | 75/75/60/55/110/95 (470) | +20 HP +40 Atk +10 Def +10 Spe | level 106, at night |
| 4 | Orbweavix | Bug/Poison | Stakeout, Insomnia, Sniper | 90/115/85/60/80/50 (480) | +20 HP +25 Atk +15 Def +10 SpD +10 Spe | level 108, at night |
| 5 | Abyssolux | Water/Electric | Volt Absorb, Illuminate, Water Absorb | 135/58/73/106/96/72 (540) | +10 HP +15 Def +30 SpA +20 SpD +5 Spe | level 112, at night |
| 6 | Totemsight | Psychic/Flying | Synchronize, Early Bird, Magic Bounce | 80/75/80/125/90/100 (550) | +15 HP +10 Def +30 SpA +20 SpD +5 Spe | level 115, by day |
| 7 | Mimicedar | Rock/Grass | Sturdy, Rock Head, Sap Sipper | 90/125/135/30/80/30 (490) | +20 HP +25 Atk +20 Def +15 SpD | level 108, in the rain |
| 8 | Helianthra | Grass/Fire | Chlorophyll, Solar Power, Early Bird | 85/75/70/135/100/40 (505) | +10 HP +15 Def +30 SpA +15 SpD +10 Spe | a Sun Stone used on it at level 110 or above |
| 9 | Mirelord | Water/Ground | Damp, Water Absorb, Unaware | 120/105/100/65/85/35 (510) | +25 HP +20 Atk +15 Def +20 SpD | level 106 |
| 10 | Zenitheon | Psychic | Synchronize, Magic Bounce | 85/65/75/140/110/125 (600) | +20 HP +15 Def +10 SpA +15 SpD +15 Spe | level 120, holding a Sun Stone, with friendship 220 or more (used up) |
| 11 | Eclipseon | Dark | Synchronize, Inner Focus | 115/65/125/60/140/95 (600) | +20 HP +15 Def +10 SpD +30 Spe | level 120, holding a Moon Stone, with friendship 220 or more (used up) |
| 12 | Slowmonarch | Water/Psychic | Oblivious, Own Tempo, Regenerator | 110/75/100/125/130/30 (570) | +15 HP +20 Def +25 SpA +20 SpD | level 120, holding a King's Rock (used up) |
| 13 | Wobbulwark | Psychic/Ghost | Shadow Tag, Telepathy | 200/33/88/33/88/43 (485) | +10 HP +30 Def +30 SpD +10 Spe | level 110 |
| 14 | Pinecitadel | Bug/Steel | Sturdy, Iron Barbs | 90/100/150/60/85/60 (545) | +15 HP +10 Atk +10 Def +25 SpD +20 Spe | level 112 |
| 15 | Ironwyrm | Steel/Ground | Rock Head, Sturdy, Sheer Force | 95/115/200/55/90/35 (590) | +20 HP +30 Atk +25 SpD +5 Spe | level 115, holding a Metal Coat (used up) |
| 16 | Mastiffang | Fairy/Fighting | Intimidate, Quick Feet, Strong Jaw | 110/140/90/60/75/55 (530) | +20 HP +20 Atk +15 Def +15 SpD +10 Spe | level 108 |
| 17 | Fugumace | Water/Poison | Poison Point, Swift Swim, Intimidate | 85/120/100/55/65/95 (520) | +20 HP +25 Atk +15 Def +10 SpD +10 Spe | level 106, knowing Spikes |
| 18 | Guillotyne | Bug/Steel | Swarm, Technician, Tough Claws | 85/150/115/55/90/85 (580) | +15 HP +20 Atk +15 Def +10 SpD +20 Spe | level 120, holding a Razor Claw (used up) |
| 19 | Fermentoise | Bug/Rock | Sturdy, Gluttony, Contrary | 50/30/230/30/230/15 (585) | +30 HP +20 Atk +20 SpA +10 Spe | level 110, holding a Leftovers (used up) |
| 20 | Atlascross | Bug/Fighting | Swarm, Guts, Moxie | 95/145/95/40/105/100 (580) | +15 HP +20 Atk +20 Def +10 SpD +15 Spe | level 115 |
| 21 | Calderon | Fire/Rock | Magma Armor, Flame Body, Weak Armor | 80/55/135/105/95/40 (510) | +20 HP +5 Atk +15 Def +15 SpA +15 SpD +10 Spe | level 112 |
| 22 | Reefcrown | Water/Rock | Hustle, Natural Cure, Regenerator | 85/55/115/80/115/40 (490) | +20 HP +20 Def +15 SpA +20 SpD +5 Spe | level 106, holding a Pearl (used up) |
| 23 | Krakannon | Water/Fire | Suction Cups, Sniper, Mega Launcher | 90/125/85/125/85/50 (560) | +15 HP +20 Atk +10 Def +20 SpA +10 SpD +5 Spe | level 112 |
| 24 | Sleighwing | Ice/Flying | Vital Spirit, Hustle, Pickup | 60/70/55/80/55/90 (410) | +15 HP +15 Atk +10 Def +15 SpA +10 SpD +15 Spe | level 104, at an Ice Rock (Route 217's) |
| 25 | Raysail | Water/Flying | Swift Swim, Water Absorb, Water Veil | 105/50/85/100/145/80 (565) | +20 HP +10 Atk +15 Def +20 SpA +5 SpD +10 Spe | level 112, with an Octillery in the party |
| 26 | Rapierwing | Steel/Flying | Keen Eye, Sturdy, Weak Armor | 80/105/145/40/85/90 (545) | +15 HP +25 Atk +5 Def +15 SpD +20 Spe | level 115 |
| 27 | Cerberhound | Dark/Fire | Early Bird, Flash Fire, Unnerve | 85/100/70/135/90/100 (580) | +10 HP +10 Atk +20 Def +25 SpA +10 SpD +5 Spe | level 120, at night |
| 28 | Siegephant | Ground/Steel | Sturdy, Stamina | 110/140/135/60/75/60 (580) | +20 HP +20 Atk +15 Def +15 SpD +10 Spe | level 115 |
| 29 | Muralist | Normal | Own Tempo, Technician, Moody | 70/35/50/35/60/80 (330) | +15 HP +15 Atk +15 Def +15 SpA +15 SpD +5 Spe | level 110 |
| 30 | Gyrodrill | Fighting/Ground | Intimidate, Technician, Steadfast | 75/115/110/35/115/85 (535) | +25 HP +20 Atk +15 Def +5 SpD +15 Spe | level 110, holding a Muscle Band (used up) |
| 31 | Pastorine | Normal/Fairy | Thick Fat, Scrappy, Sap Sipper | 115/95/120/40/90/110 (570) | +20 HP +15 Atk +15 Def +20 SpD +10 Spe | level 115, with friendship 220 or more |
| 32 | Raitenko | Electric | Pressure, Electric Surge | 105/85/85/145/110/130 (660) | +15 HP +10 Def +30 SpA +10 SpD +15 Spe | level 150, with a Ho-Oh in the party |
| 33 | Enkaiou | Fire | Pressure, Drought | 130/145/95/90/85/115 (660) | +15 HP +30 Atk +10 Def +10 SpD +15 Spe | level 150, with a Ho-Oh in the party |
| 34 | Suiseiren | Water | Pressure, Misty Surge | 115/75/130/110/130/100 (660) | +15 HP +15 Def +20 SpA +15 SpD +15 Spe | level 150, with a Ho-Oh in the party |

## The technical plan (for after the user's answers)

Batch 1's plan holds unchanged: the species in `tools/DataImporter/Overrides/species-ours.json` (written for batch 1 by the session that builds it), the evolution rows through `Overrides/species.json`, the Pokédex text in `Data/dex-entries.json`, and the sculpts in `PokemonModels.Ours.Johto.cs`, in our numbers' order, each built from its species' build where it shares most of the body. Nothing in the engine is needed for this batch: every method above runs today, `LevelWithSpeciesInParty` among them. What is not this batch's: 06 · R31's moves past level 100 (the learnsets wait for them), the species whose lines end in Sinnoh and later (their batches), and where in the game the evolutions are met (they need no place but the Ice Rock, only a level the climb reaches).

## How the numbers were checked

The same Node.js script as batch 1's, kept in the session's scratch folder and reading only `species.json`, `abilities.json` and `items.json`. It built the lines (a regional form's evolution counted as the form's own) and the list of the 38 that qualify, and for each of the 34 designs checked: that it qualifies and that every qualifying species has a design or a reason for none; that the rise is +80 or what the ceiling leaves, that no stat falls and that the six stats add up to the total written; that the name is one word of at most 12 letters, taken by no species or form of the data, by none of batch 1's 46 and by no other design of this batch; that the number is 2000 + the old species' and meets none of batch 1's; that the types are one or two of the eighteen; that the abilities exist and fill exactly the old species' slots; that the level is between 101 and 160, and at most 130 for a species that isn't legendary; that an item held or used exists and is no key item; that a move to know is in the species' learnset; that a species named in the party exists; that a place is one the data already names; and that each is taller and heavier than what it comes from. The tables and the entries were written by the same script from the same values, so a number can't differ between the selection table, an entry and the table to answer.
