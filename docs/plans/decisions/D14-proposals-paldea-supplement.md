# D14 · Evolutions past level 100, batch 9 (Paldea): the supplement

Plan 03 · D14. The user answered [D14-proposals-paldea.md](D14-proposals-paldea.md) on 2026-10-26: all 39 designs yes and every question by its default, but three answered with their first alternative ("b"), each of which wants designs the main document doesn't have. This supplement is those designs, written in wave 53 by the session that built the 39 (their data is built: plan 03's D14 line). **Answered and built (wave 54)**: the user approved all 18 designs on 2026-10-26 with every question at its default (the Loyal Three to 600, the ancient paradox Pokémon at night and the future ones by day, the paradox Pokémon may enter the Frontier, Zerofin with Swift Swim at 248 knowing Wave Crash); rows in `species-past-hundred.json`, entries in `dex-entries.json`, tests in `SpeciesPastHundredPaldeaSupplementTests`. The format is the main document's, and so are its settled rules.

- **Question 3, the Loyal Three: "b"**: Okidogi, Munkidori and Fezandipiti as a set of three, each with a Pecharunt in the party, risen either only to **600**, their master's own total, or to **660**, the settled rule for a region's legendary set. The answer named both readings: which one is question 1 below.
- **Question 4, the paradox Pokémon: "b"**: the fourteen of the base game (Great Tusk to Iron Thorns, Roaring Moon and Iron Valiant), each risen to the 600 ceiling (+30 from 570, +10 for Roaring Moon and Iron Valiant at 590), the ancient ones at night and the future ones by day; the six that are a legendary's past or future (Walking Wake, Iron Leaves, Gouging Fire, Raging Bolt, Iron Boulder, Iron Crown) still no. How the hour falls is question 2.
- **Question 6, Palafin: "b"**: an evolution that keeps Palafin's Zero Form's look and takes another of `abilities.json`'s abilities in Zero to Hero's place, 457 → 537. Which ability is question 4.

**The settled rules, applied**: the number is **2000 + the forebear's** (here 2964, 2984 to 2995, 3005, 3006 and 3014 to 3016, meeting nothing: R31's moves numbered 3001 to 3072 are another table); a species of ours that isn't legendary stops at **600**; abilities come only from `abilities.json`, the forebear's slots kept; the evolution is **by level with a condition the engine already runs**; the level within **Paldea's climb, 244 to 256, a legendary set at 258** (the main document's question 1, accepted); names are one word of at most 12 letters, taken by no species or form of the data (which holds batches 1 to 4 and the 39 of this batch) and by no word of the nine proposals; ours have no forms; a legendary's evolution is banned from the Battle Frontier.

Every name, type, ability, item, move, species, stat and sum here was checked by a script that only reads `species.json`, `abilities.json`, `items.json` and `moves.json` (what it checks is listed at the end), and the entries and tables were written by it from the same values.

## The table: the eighteen

| # | Was | Ours | No. | Typing | Total | Evolves |
|---|---|---|---|---|---|---|
| 1 | Palafin (964) | Zerofin | 2964 | Water, kept | 457 → 537 | level 248, knowing Wave Crash |
| 2 | Great Tusk (984) | Tuskendon | 2984 | Ground/Fighting, kept | 570 → 600 | level 250, at night |
| 3 | Scream Tail (985) | Shriekpuff | 2985 | Fairy/Psychic, kept | 570 → 600 | level 250, at night |
| 4 | Brute Bonnet (986) | Fungoliath | 2986 | Grass/Dark, kept | 570 → 600 | level 250, at night |
| 5 | Flutter Mane (987) | Banshemane | 2987 | Ghost/Fairy, kept | 570 → 600 | level 250, at night |
| 6 | Slither Wing (988) | Saurmoth | 2988 | Bug/Fighting, kept | 570 → 600 | level 250, at night |
| 7 | Sandy Shocks (989) | Lodestatic | 2989 | Electric/Ground, kept | 570 → 600 | level 250, at night |
| 8 | Iron Treads (990) | Treadnought | 2990 | Ground/Steel, kept | 570 → 600 | level 250, by day |
| 9 | Iron Bundle (991) | Cryoparcel | 2991 | Ice/Water, kept | 570 → 600 | level 250, by day |
| 10 | Iron Hands (992) | Gauntletron | 2992 | Fighting/Electric, kept | 570 → 600 | level 250, by day |
| 11 | Iron Jugulis (993) | Hydrajet | 2993 | Dark/Flying, kept | 570 → 600 | level 250, by day |
| 12 | Iron Moth (994) | Fusionmoth | 2994 | Fire/Poison, kept | 570 → 600 | level 250, by day |
| 13 | Iron Thorns (995) | Thornframe | 2995 | Rock/Electric, kept | 570 → 600 | level 250, by day |
| 14 | Roaring Moon (1005) | Lunaroar | 3005 | Dragon/Dark, kept | 590 → 600 | level 254, at night |
| 15 | Iron Valiant (1006) | Paladroid | 3006 | Fairy/Fighting, kept | 590 → 600 | level 254, by day |
| 16 | Okidogi (1014) | Okidaimyo | 3014 | Poison/Fighting, kept | 555 → 600 (or 660) | level 258, with a Pecharunt in the party |
| 17 | Munkidori (1015) | Munkiregent | 3015 | Poison/Psychic, kept | 555 → 600 (or 660) | level 258, with a Pecharunt in the party |
| 18 | Fezandipiti (1016) | Fezantessa | 3016 | Poison/Fairy, kept | 555 → 600 (or 660) | level 258, with a Pecharunt in the party |

## The designs

In National Pokédex order. Each entry: the typing, the category word, number and size, the stats with where the rise goes, the abilities, the evolution with the row it would be in `species.json`, the look (from our own sculpt of the species, which the user is reworking) and a Pokédex line for the two written entries a later session writes.

### 1. Zerofin, from Palafin (964)

- **Typing**: Water, kept.
- **The Swift Dolphin Pokémon**, No. 2964. 1.6 m, 80.0 kg (Palafin 1.3 m, 60.2 kg).
- **Stats**: 100 / 70 / 72 / 53 / 62 / 100 = 457 → **100 / 90 / 92 / 58 / 82 / 115 = 537** (+20 Atk +20 Def +5 SpA +20 SpD +15 Spe).
- **Abilities**: Swift Swim (was Zero to Hero); *alternative* (question 4): Water Veil.
- **Evolution**: Palafin at level 248, knowing Wave Crash. Data: `{"targetSpecies":"Zerofin","method":"LevelKnowsMove","level":248,"move":"Wave Crash"}`.
- **Look**: from our blue Palafin in its Zero Form: larger, its fins longer and swept back, a pale streak down its back like a racing stripe and its little smile kept. **New feature**: the swept-back fins.
- **Pokédex**: It never puts on a hero's suit. It swims faster than any Palafin, rescuing swimmers before anyone sees it.

### 2. Tuskendon, from Great Tusk (984)

- **Typing**: Ground/Fighting, kept.
- **The Ancient Tusk Pokémon**, No. 2984. 2.6 m, 420.0 kg (Great Tusk 2.2 m, 320 kg).
- **Stats**: 115 / 131 / 131 / 53 / 53 / 87 = 570 → **125 / 131 / 131 / 53 / 63 / 97 = 600** (+10 HP +10 SpD +10 Spe): the rise is +30, to the 600 ceiling.
- **Abilities**: Protosynthesis (was Protosynthesis).
- **Evolution**: Great Tusk at level 250, at night. Data: `{"targetSpecies":"Tuskendon","method":"LevelNight","level":250}`.
- **Look**: from our purple Great Tusk with its tusks curled over its back: larger, a second, shorter pair of tusks under the first, a mane of coarse red hair down its spine and its hide cracked like dry clay over the shoulders. **New feature**: the second pair of tusks.
- **Pokédex**: It rolls through the night across the crater's floor, and by morning a boulder field lies flattened behind it.

### 3. Shriekpuff, from Scream Tail (985)

- **Typing**: Fairy/Psychic, kept.
- **The Ancient Howl Pokémon**, No. 2985. 1.5 m, 14.0 kg (Scream Tail 1.2 m, 8 kg).
- **Stats**: 115 / 65 / 99 / 65 / 115 / 111 = 570 → **115 / 70 / 114 / 75 / 115 / 111 = 600** (+5 Atk +15 Def +10 SpA): the rise is +30, to the 600 ceiling.
- **Abilities**: Protosynthesis (was Protosynthesis).
- **Evolution**: Scream Tail at level 250, at night. Data: `{"targetSpecies":"Shriekpuff","method":"LevelNight","level":250}`.
- **Look**: from our pink Scream Tail with its long tail and its ruff: rounder, the ruff grown into a cloud of fur round its whole head, the tail longer with a second tuft and its ears tipped in pale violet. **New feature**: the second tuft of its tail.
- **Pokédex**: Its scream at night carries over a whole valley, and every Jigglypuff that hears it falls silent until dawn.

### 4. Fungoliath, from Brute Bonnet (986)

- **Typing**: Grass/Dark, kept.
- **The Ancient Mushroom Pokémon**, No. 2986. 1.6 m, 45.0 kg (Brute Bonnet 1.2 m, 21 kg).
- **Stats**: 111 / 127 / 99 / 79 / 99 / 55 = 570 → **111 / 127 / 104 / 79 / 109 / 70 = 600** (+5 Def +10 SpD +15 Spe): the rise is +30, to the 600 ceiling.
- **Abilities**: Protosynthesis (was Protosynthesis).
- **Evolution**: Brute Bonnet at level 250, at night. Data: `{"targetSpecies":"Fungoliath","method":"LevelNight","level":250}`.
- **Look**: from our grey and red Brute Bonnet with its fangs and tendrils: taller, its cap wider and spotted with pale scales, a second row of fangs under it, and its tendrils thick as roots trailing behind it. **New feature**: the second row of fangs.
- **Pokédex**: It rests as a harmless toadstool by day. After dark its spores drift so thick that the trees round it fall asleep.

### 5. Banshemane, from Flutter Mane (987)

- **Typing**: Ghost/Fairy, kept.
- **The Ancient Wail Pokémon**, No. 2987. 1.7 m, 6.0 kg (Flutter Mane 1.4 m, 4 kg).
- **Stats**: 55 / 55 / 55 / 135 / 135 / 135 = 570 → **75 / 55 / 65 / 135 / 135 / 135 = 600** (+20 HP +10 Def): the rise is +30, to the 600 ceiling.
- **Abilities**: Protosynthesis (was Protosynthesis).
- **Evolution**: Flutter Mane at level 250, at night. Data: `{"targetSpecies":"Banshemane","method":"LevelNight","level":250}`.
- **Look**: from our grey and pink Flutter Mane with its hair of feathers: longer, its feathered hair trailing to the ground in a veil, three red gems now on its brow and its lower edge ragged like a torn gown. **New feature**: the veil of feathered hair.
- **Pokédex**: Its weeping is heard at night above the ruins. Those who hear it see the ancient sky, full of beasts that no longer live.

### 6. Saurmoth, from Slither Wing (988)

- **Typing**: Bug/Fighting, kept.
- **The Ancient Moth Pokémon**, No. 2988. 3.8 m, 130.0 kg (Slither Wing 3.2 m, 92 kg).
- **Stats**: 85 / 135 / 79 / 85 / 105 / 81 = 570 → **90 / 135 / 94 / 85 / 105 / 91 = 600** (+5 HP +15 Def +10 Spe): the rise is +30, to the 600 ceiling.
- **Abilities**: Protosynthesis (was Protosynthesis).
- **Evolution**: Slither Wing at level 250, at night. Data: `{"targetSpecies":"Saurmoth","method":"LevelNight","level":250}`.
- **Look**: from our white and red Slither Wing: longer, two more pairs of wings along its body like a centipede's, its six legs heavier and red flames along its wings' edges. **New feature**: the two more pairs of wings.
- **Pokédex**: It flies only on moonless nights, beating its many wings so hard that the grass beneath catches fire.

### 7. Lodestatic, from Sandy Shocks (989)

- **Typing**: Electric/Ground, kept.
- **The Ancient Magnet Pokémon**, No. 2989. 2.7 m, 90.0 kg (Sandy Shocks 2.3 m, 60 kg).
- **Stats**: 85 / 81 / 97 / 121 / 85 / 101 = 570 → **100 / 81 / 97 / 121 / 100 / 101 = 600** (+15 HP +15 SpD): the rise is +30, to the 600 ceiling.
- **Abilities**: Protosynthesis (was Protosynthesis).
- **Evolution**: Sandy Shocks at level 250, at night. Data: `{"targetSpecies":"Lodestatic","method":"LevelNight","level":250}`.
- **Look**: from our grey Sandy Shocks with its mane of iron sand: larger, the mane grown into a sweeping coat of iron filings that stand on end, and two horns of fused sand on its head crackling with charge. **New feature**: the two horns of fused sand.
- **Pokédex**: Compasses spin wildly within a mile of it at night. It drags iron sand into dunes shaped like the lines of a magnet.

### 8. Treadnought, from Iron Treads (990)

- **Typing**: Ground/Steel, kept.
- **The Future Tank Pokémon**, No. 2990. 1.2 m, 380.0 kg (Iron Treads 0.9 m, 240 kg).
- **Stats**: 90 / 112 / 120 / 72 / 70 / 106 = 570 → **100 / 112 / 120 / 72 / 90 / 106 = 600** (+10 HP +20 SpD): the rise is +30, to the 600 ceiling.
- **Abilities**: Quark Drive (was Quark Drive).
- **Evolution**: Iron Treads at level 250, by day. Data: `{"targetSpecies":"Treadnought","method":"LevelDay","level":250}`.
- **Look**: from our grey Iron Treads with its wheels and tusks: wider, its wheels doubled into a pair of treads, a ring of blue light round its body and its tusks shaped into blades. **New feature**: the treads in place of wheels.
- **Pokédex**: In daylight it races across the plains faster than any car, and the treads it leaves are perfectly straight.

### 9. Cryoparcel, from Iron Bundle (991)

- **Typing**: Ice/Water, kept.
- **The Future Courier Pokémon**, No. 2991. 0.9 m, 20.0 kg (Iron Bundle 0.6 m, 11 kg).
- **Stats**: 56 / 80 / 114 / 124 / 60 / 136 = 570 → **76 / 80 / 114 / 124 / 70 / 136 = 600** (+20 HP +10 SpD): the rise is +30, to the 600 ceiling.
- **Abilities**: Quark Drive (was Quark Drive).
- **Evolution**: Iron Bundle at level 250, by day. Data: `{"targetSpecies":"Cryoparcel","method":"LevelDay","level":250}`.
- **Look**: from our red and white Iron Bundle with its bag: taller, its bag a second pack of gleaming plates, a visor of ice over its eyes and four jets on its underside. **New feature**: the second pack on its back.
- **Pokédex**: It speeds above the sea by day, and anything it passes freezes solid. No one knows what it is carrying.

### 10. Gauntletron, from Iron Hands (992)

- **Typing**: Fighting/Electric, kept.
- **The Future Fist Pokémon**, No. 2992. 2.2 m, 560.0 kg (Iron Hands 1.8 m, 380.7 kg).
- **Stats**: 154 / 140 / 108 / 50 / 68 / 50 = 570 → **154 / 140 / 108 / 50 / 88 / 60 = 600** (+20 SpD +10 Spe): the rise is +30, to the 600 ceiling.
- **Abilities**: Quark Drive (was Quark Drive).
- **Evolution**: Iron Hands at level 250, by day. Data: `{"targetSpecies":"Gauntletron","method":"LevelDay","level":250}`.
- **Look**: from our grey and yellow Iron Hands with its great hands: larger, its hands grown into gauntlets of four plates each, coils lit along its arms and a crest on its head like a helmet. **New feature**: the plated gauntlets.
- **Pokédex**: Its punches crack bedrock at a single blow. It stands motionless by night and wakes the moment the sun touches it.

### 11. Hydrajet, from Iron Jugulis (993)

- **Typing**: Dark/Flying, kept.
- **The Future Hunter Pokémon**, No. 2993. 1.6 m, 150.0 kg (Iron Jugulis 1.3 m, 111 kg).
- **Stats**: 94 / 80 / 86 / 122 / 80 / 108 = 570 → **94 / 80 / 101 / 122 / 95 / 108 = 600** (+15 Def +15 SpD): the rise is +30, to the 600 ceiling.
- **Abilities**: Quark Drive (was Quark Drive).
- **Evolution**: Iron Jugulis at level 250, by day. Data: `{"targetSpecies":"Hydrajet","method":"LevelDay","level":250}`.
- **Look**: from our blue and white Iron Jugulis with its two arm heads: longer, its wings swept back with jets at their tips, a third head grown at its tail and a ring of lights round its neck. **New feature**: the third head at its tail.
- **Pokédex**: It circles high on clear days, its three heads turning in every direction. Nothing that moves below escapes its notice.

### 12. Fusionmoth, from Iron Moth (994)

- **Typing**: Fire/Poison, kept.
- **The Future Moth Pokémon**, No. 2994. 1.5 m, 60.0 kg (Iron Moth 1.2 m, 36 kg).
- **Stats**: 80 / 70 / 60 / 140 / 110 / 110 = 570 → **85 / 70 / 85 / 140 / 110 / 110 = 600** (+5 HP +25 Def): the rise is +30, to the 600 ceiling.
- **Abilities**: Quark Drive (was Quark Drive).
- **Evolution**: Iron Moth at level 250, by day. Data: `{"targetSpecies":"Fusionmoth","method":"LevelDay","level":250}`.
- **Look**: from our yellow Iron Moth with its glowing core: wider, a second ring of armour round its core, its wings longer and blazing at their tips and its antennae grown into two curved prongs. **New feature**: the second ring round its core.
- **Pokédex**: Its core burns as hot as the sun at noon. It drifts above the desert by day, and the sand beneath turns to glass.

### 13. Thornframe, from Iron Thorns (995)

- **Typing**: Rock/Electric, kept.
- **The Future Spike Pokémon**, No. 2995. 2.0 m, 420.0 kg (Iron Thorns 1.6 m, 303 kg).
- **Stats**: 100 / 134 / 110 / 70 / 84 / 72 = 570 → **100 / 134 / 110 / 70 / 99 / 87 = 600** (+15 SpD +15 Spe): the rise is +30, to the 600 ceiling.
- **Abilities**: Quark Drive (was Quark Drive).
- **Evolution**: Iron Thorns at level 250, by day. Data: `{"targetSpecies":"Thornframe","method":"LevelDay","level":250}`.
- **Look**: from our green Iron Thorns with its spines: taller, its spines grown into a cage of crystal rods over its back, sparks running between them and its jaw a heavier plate. **New feature**: the cage of crystal rods.
- **Pokédex**: It stands still as a statue in the sun, drinking in light through its spines. Then it charges, and nothing can stop it.

### 14. Lunaroar, from Roaring Moon (1005)

- **Typing**: Dragon/Dark, kept.
- **The Ancient Dragon Pokémon**, No. 3005. 2.4 m, 450.0 kg (Roaring Moon 2 m, 380 kg).
- **Stats**: 105 / 139 / 71 / 55 / 101 / 119 = 590 → **105 / 139 / 81 / 55 / 101 / 119 = 600** (+10 Def): the rise is +10, to the 600 ceiling.
- **Abilities**: Protosynthesis (was Protosynthesis).
- **Evolution**: Roaring Moon at level 254, at night. Data: `{"targetSpecies":"Lunaroar","method":"LevelNight","level":254}`.
- **Look**: from our green and red Roaring Moon: larger, its wings longer and ragged like torn sails, its crest of horns curling forward into a crown and the red of its belly brighter. **New feature**: the crown of curled horns.
- **Pokédex**: When the moon is full it soars over the crater's rim, and its roar shakes loose rocks from the walls for hours.

### 15. Paladroid, from Iron Valiant (1006)

- **Typing**: Fairy/Fighting, kept.
- **The Future Knight Pokémon**, No. 3006. 1.7 m, 50.0 kg (Iron Valiant 1.4 m, 35 kg).
- **Stats**: 74 / 130 / 90 / 120 / 60 / 116 = 590 → **74 / 130 / 90 / 120 / 70 / 116 = 600** (+10 SpD): the rise is +10, to the 600 ceiling.
- **Abilities**: Quark Drive (was Quark Drive).
- **Evolution**: Iron Valiant at level 254, by day. Data: `{"targetSpecies":"Paladroid","method":"LevelDay","level":254}`.
- **Look**: from our white and green Iron Valiant with its blades: taller, its blade arms joined to a long lance, a cape of light from its shoulders and its visor sharper. **New feature**: the lance its blades form.
- **Pokédex**: It moves faster than the eye can follow, and its lance leaves trails of light in the noon sky.

### 16. Okidaimyo, from Okidogi (1014)

- **Typing**: Poison/Fighting, kept.
- **The Warlord Pokémon**, No. 3014. 2.2 m, 140.0 kg (Okidogi 1.8 m, 92.2 kg).
- **Stats** (question 1's default, to 600): 88 / 128 / 115 / 58 / 86 / 80 = 555 → **103 / 128 / 115 / 63 / 101 / 90 = 600** (+15 HP +5 SpA +15 SpD +10 Spe).
- **Stats** (question 1's alternative, to 660): **103 / 138 / 130 / 63 / 111 / 115 = 660** (+15 HP +10 Atk +15 Def +5 SpA +25 SpD +35 Spe).
- **Abilities**: Toxic Chain, hidden Guard Dog (was Toxic Chain, hidden Guard Dog).
- **Evolution**: Okidogi at level 258, with a Pecharunt in the party. Data: `{"targetSpecies":"Okidaimyo","method":"LevelWithSpeciesInParty","level":258,"species":"Pecharunt"}`.
- **Look**: from our black and green Okidogi with its chain collar: larger, its arms thicker with plates of poisoned muscle, its chain collar grown into a heavy mantle of links over its shoulders and its fur standing up like a lion's mane. **New feature**: the mantle of chain links.
- **Pokédex**: It once guarded a cruel master. Now it bows only to Pecharunt and smashes the boulders of the mountain in its rage.

### 17. Munkiregent, from Munkidori (1015)

- **Typing**: Poison/Psychic, kept.
- **The Regent Pokémon**, No. 3015. 1.4 m, 20.0 kg (Munkidori 1 m, 12.2 kg).
- **Stats** (question 1's default, to 600): 88 / 75 / 66 / 130 / 90 / 106 = 555 → **103 / 75 / 91 / 130 / 95 / 106 = 600** (+15 HP +25 Def +5 SpD).
- **Stats** (question 1's alternative, to 660): **113 / 75 / 91 / 150 / 110 / 121 = 660** (+25 HP +25 Def +20 SpA +20 SpD +15 Spe).
- **Abilities**: Toxic Chain, hidden Frisk (was Toxic Chain, hidden Frisk).
- **Evolution**: Munkidori at level 258, with a Pecharunt in the party. Data: `{"targetSpecies":"Munkiregent","method":"LevelWithSpeciesInParty","level":258,"species":"Pecharunt"}`.
- **Look**: from our black and green Munkidori: taller, its tail grown long and forked, its fur tipped in poison-green light and the chain at its neck a collar of linked rings. **New feature**: the forked tail.
- **Pokédex**: It wanders through the forest by night, reading the minds of those it meets. It does as Pecharunt wishes and no more.

### 18. Fezantessa, from Fezandipiti (1016)

- **Typing**: Poison/Fairy, kept.
- **The Courtier Pokémon**, No. 3016. 1.8 m, 45.0 kg (Fezandipiti 1.4 m, 30.1 kg).
- **Stats** (question 1's default, to 600): 88 / 91 / 82 / 70 / 125 / 99 = 555 → **103 / 91 / 102 / 80 / 125 / 99 = 600** (+15 HP +20 Def +10 SpA).
- **Stats** (question 1's alternative, to 660): **113 / 106 / 102 / 80 / 145 / 114 = 660** (+25 HP +15 Atk +20 Def +10 SpA +20 SpD +15 Spe).
- **Abilities**: Toxic Chain, hidden Technician (was Toxic Chain, hidden Technician).
- **Evolution**: Fezandipiti at level 258, with a Pecharunt in the party. Data: `{"targetSpecies":"Fezantessa","method":"LevelWithSpeciesInParty","level":258,"species":"Pecharunt"}`.
- **Look**: from our black and green Fezandipiti: longer, its tail feathers grown into a sweeping train with poison-green eyes on them and a fan of dark feathers at its throat. **New feature**: the sweeping train of feathers.
- **Pokédex**: It dances in the moonlight with its train spread wide. All who watch are charmed and forget what they were doing.

## Balance

- **The paradox Pokémon** all stand at **600**, the ceiling, as batch 4's Tangrowth and the Eeveelutions did: twelve rise +30 from 570, Roaring Moon (Lunaroar) and Iron Valiant (Paladroid) +10 from 590. Each rise goes to the stats their designs lack, so none of them sharpens its best stat: Banshemane keeps Flutter Mane's 135s and gains bulk (75 HP, 65 Def), Gauntletron keeps Iron Hands' 154 HP and 140 Attack and gains Sp. Def and Speed, Lunaroar's 10 goes to Defense, Paladroid's to Sp. Def. Protosynthesis and Quark Drive are kept: they need the sun or Electric Terrain, or a Booster Energy, as the forebears' do.
- **The Loyal Three**: at the default 600 (+45 each) they stand level with Pecharunt (600), which binds them; at 660 (+105 each) they stand 60 over it. Toxic Chain is kept, with its hidden ability each. The 660 reading's rise of 105 is the largest of any species of ours (the Treasures of Ruin rose +90).
- **Zerofin**: 537, +80 like any other; Swift Swim in Zero to Hero's place, with Palafin's 100 Speed now 115. Palafin's Hero Form (650) stays the strongest of its line, as the main document's question 6 said.
- **Flagged as strong**: Banshemane (Flutter Mane's 135 Sp. Atk, Sp. Def and Speed, now with 75 HP; *to turn down*: the 20 to HP as 10 to HP and 10 to Attack); Gauntletron (154 HP, 140 Attack and now 88 Sp. Def; *to turn down*: the 20 to Sp. Def as 10 to Sp. Def and 10 to Sp. Atk).

## Open questions

1. **The Loyal Three's total.** *Default*: **600** (+45 each), Pecharunt's own total, so that they stand no higher than the master that binds them, as batch 3 kept the Regis under Regigigas; the first legendary set of ours under 660, which `SpeciesPastHundredTests` then names as its one exception. *Alternative*: **660** (+105 each), the settled rule for a region's legendary set, past their master; both readings' stats are in the entries above.
2. **The paradox Pokémon's hour.** *Default*: as the answer gave it: **the ancient ones at night**, as the ancient world's beasts stir in the dark of Area Zero's depths, and **the future ones by day**, as machines that drink the sun's light. *Alternatives*: (a) the other way round, Scarlet's past by day under Koraidon's sun and Violet's future at night under Miraidon's; (b) any hour, each holding a **Booster Energy** (used up), the item that wakes their abilities, as `LevelHoldingItem` already runs; (c) a pair at a time, each ancient one with its future counterpart in the party (Great Tusk with Iron Treads, Donphan's past and future), which leaves those without one (Scream Tail, Brute Bonnet, Flutter Mane, Slither Wing, Sandy Shocks, Roaring Moon and their partners) without a partner of the right kind.
3. **The paradox Pokémon at the Battle Frontier.** The data doesn't flag them as legendary, so by the settled rule their evolutions may be entered, as the forebears may. *Default*: they may. *Alternative*: banned beside the legendaries' evolutions, which would need `FrontierRules.IsBannedOfOurs` to name them (the one engine change of this supplement).
4. **Zerofin's ability.** *Default*: **Swift Swim**, a racer's ability, which no Palafin has. *Alternative*: **Water Veil** (a burn never takes), or another the user names from `abilities.json`.
5. **Zerofin's condition.** *Default*: level 248, knowing **Wave Crash** (Palafin's own move at 61), the middle of the region. *Alternative*: level 248 with a Finizen in the party, the young it rescues.

## The table to answer

| # | Ours | Typing | Abilities | HP/Atk/Def/SpA/SpD/Spe (total) | Δ | Evolves |
|---|---|---|---|---|---|---|
| 1 | Zerofin | Water | Swift Swim | 100/90/92/58/82/115 (537) | +20 Atk +20 Def +5 SpA +20 SpD +15 Spe | level 248, knowing Wave Crash |
| 2 | Tuskendon | Ground/Fighting | Protosynthesis | 125/131/131/53/63/97 (600) | +10 HP +10 SpD +10 Spe | level 250, at night |
| 3 | Shriekpuff | Fairy/Psychic | Protosynthesis | 115/70/114/75/115/111 (600) | +5 Atk +15 Def +10 SpA | level 250, at night |
| 4 | Fungoliath | Grass/Dark | Protosynthesis | 111/127/104/79/109/70 (600) | +5 Def +10 SpD +15 Spe | level 250, at night |
| 5 | Banshemane | Ghost/Fairy | Protosynthesis | 75/55/65/135/135/135 (600) | +20 HP +10 Def | level 250, at night |
| 6 | Saurmoth | Bug/Fighting | Protosynthesis | 90/135/94/85/105/91 (600) | +5 HP +15 Def +10 Spe | level 250, at night |
| 7 | Lodestatic | Electric/Ground | Protosynthesis | 100/81/97/121/100/101 (600) | +15 HP +15 SpD | level 250, at night |
| 8 | Treadnought | Ground/Steel | Quark Drive | 100/112/120/72/90/106 (600) | +10 HP +20 SpD | level 250, by day |
| 9 | Cryoparcel | Ice/Water | Quark Drive | 76/80/114/124/70/136 (600) | +20 HP +10 SpD | level 250, by day |
| 10 | Gauntletron | Fighting/Electric | Quark Drive | 154/140/108/50/88/60 (600) | +20 SpD +10 Spe | level 250, by day |
| 11 | Hydrajet | Dark/Flying | Quark Drive | 94/80/101/122/95/108 (600) | +15 Def +15 SpD | level 250, by day |
| 12 | Fusionmoth | Fire/Poison | Quark Drive | 85/70/85/140/110/110 (600) | +5 HP +25 Def | level 250, by day |
| 13 | Thornframe | Rock/Electric | Quark Drive | 100/134/110/70/99/87 (600) | +15 SpD +15 Spe | level 250, by day |
| 14 | Lunaroar | Dragon/Dark | Protosynthesis | 105/139/81/55/101/119 (600) | +10 Def | level 254, at night |
| 15 | Paladroid | Fairy/Fighting | Quark Drive | 74/130/90/120/70/116 (600) | +10 SpD | level 254, by day |
| 16 | Okidaimyo | Poison/Fighting | Toxic Chain, hidden Guard Dog | 103/128/115/63/101/90 (600), or 103/138/130/63/111/115 (660) | +15 HP +5 SpA +15 SpD +10 Spe | level 258, with a Pecharunt in the party |
| 17 | Munkiregent | Poison/Psychic | Toxic Chain, hidden Frisk | 103/75/91/130/95/106 (600), or 113/75/91/150/110/121 (660) | +15 HP +25 Def +5 SpD | level 258, with a Pecharunt in the party |
| 18 | Fezantessa | Poison/Fairy | Toxic Chain, hidden Technician | 103/91/102/80/125/99 (600), or 113/106/102/80/145/114 (660) | +15 HP +20 Def +10 SpA | level 258, with a Pecharunt in the party |

## The technical plan (for after the user's answers)

As the main document's: each design a row of `tools/DataImporter/Overrides/species-past-hundred.json` after batch 9's (a `LevelWithSpeciesInParty` row takes `species`, a `LevelKnowsMove` row `move`), two written Pokédex entries and a habitat each in `Data/dex-entries.json` (`--check-entries` clean), a run of the importer, and tests beside `SpeciesPastHundredPaldeaTests`. Every condition runs today; nothing in the engine is needed, but for question 3's alternative. What a test of `SpeciesPastHundredTests` asks that this changes: at the default 600 the Loyal Three are the first legendary set of ours under 660 (its rule "a region's legendary set to 660" names them as its exception); at 660 their rise of 105 passes the 90 it allows. Zerofin is a species of ours with no form, so Zero to Hero's rule (`BattleCore.Shapes.cs`, by the species Palafin) never reaches it whatever its ability. The sculpts wait for the user, who is reworking them: until then ours are generated by rule.

## How the numbers were checked

A Node.js script kept in the session's scratch folder, reading only `species.json`, `abilities.json`, `items.json` and `moves.json`, with every capitalised word of the nine proposals read from `docs/plans`. For each design it checked: that the forebear is of Paldea's generation and evolves into nothing (but a form's own); that the paradox Pokémon are the fourteen of the base game, none flagged legendary by the data, each at 600 by +30 or +10, the ancient ones (Protosynthesis) at night and the future ones (Quark Drive) by day; that the Loyal Three are legendary in the data and Pecharunt mythical at 600, and that both readings total 600 and 660 with no stat lower than the forebear's or than the 600 reading's; that Zerofin rises +80 and doesn't keep Zero to Hero; that no stat falls; that the name is one word of at most 12 letters, taken by no species or form of the data and by no word of the nine proposals, and by no other design here; that the number is 2000 + the forebear's; that the abilities exist and fill exactly the forebear's slots; that the level is within 244 to 258; that the move to know is in the forebear's level-up learnset at 1 to 100 and runs; that the species named in the party exists; that the Booster Energy of question 2 exists and is no key item; and that each is taller and heavier than what it comes from.
