# D14 · Evolutions past level 100, batch 7 (Alola), supplement: the Ultra Beasts

Plan 03 · D14, a supplement to [D14-proposals-alola.md](D14-proposals-alola.md), written in wave 53 when that batch's 28 designs were built. The user answered Alola's question 6 with its first alternative on 2026-10-26: **the Ultra Beasts as a set, each at level 225 with another Ultra Beast in the party, to 660 (Naganadel +120)**. The main document had no designs for them, since its default was that none evolves; these are the ten. **Not answered yet.** Nothing here is built: no species, no evolution row, no learnset, no sculpt, no test. The format is the main document's, and every rule it applied holds here unless a question below asks about it.

**What is settled already** (batches 1 to 4 and the main document, applied without asking again): the number is **2000 + the number of the species it evolves from** (here 2793 to 2806, meeting none of the main document's 2735 to 2788 and nothing earlier); abilities come only from `abilities.json`, as many slots as the species had, in its order; names are invented, one word of at most 12 letters, held against every species and form of the data, every ability and item, every row of the table and every name of every D14 proposal document; ours have no forms of their own; a region's legendary set rises to **660**; Alola's set evolves at **225**, the Champion + 1 (Alola's question 1, accepted); the mythicals (Magearna, Marshadow, Zeraora, Melmetal) and Necrozma stay as the main document left them.

Every name, type, ability, stat and sum here was checked by a script that only reads `PokemonPlatinumEngine/Data/species.json`, `abilities.json` and `items.json`, the table `tools/DataImporter/Overrides/species-past-hundred.json` and the names of every `docs/plans/D14-proposals-*.md` (what it checks is listed at the end).

## What is new about this set, and needs the user's word

Three things the main document's rules don't cover, each a question below with its default:

1. **"Another Ultra Beast" is any of ten, which no condition of the engine says yet.** `LevelWithSpeciesInParty` names one species. The user's words ask for any of the others; that is a small change (a row naming several species, any one of which is enough) and its test, made by the session that builds these rows (question 1). The set of a ring, as the island guardians', runs today and is the alternative.
2. **The Ultra Beasts aren't legendary in the data.** `species.json` (PokeAPI) marks none of the eleven legendary or mythical, so by the batches' rules their evolutions would be caught at 45, allowed into the Battle Frontier, and capped at 600, which 660 passes. Making them a region's legendary set (question 2) is a field the table's row doesn't have yet (`SpeciesPastHundred.cs` copies `legendary` from the forebear): one optional field of the row, its test, and nothing else.
3. **Celesteela already weighs 999.9 kg**, the data's heaviest (Cosmoem's too) and the batches' own ceiling, so Celestrata can't be heavier than what it comes from (question 4).

## The table

| # | Was | Ours | No. | Typing | Total | Evolves |
|---|---|---|---|---|---|---|
| 1 | Nihilego (793) | Nihiliad | 2793 | Rock/Poison, kept | 570 → 660 | level 225, with another Ultra Beast in the party |
| 2 | Buzzwole (794) | Buzzcolossus | 2794 | Bug/Fighting, kept | 570 → 660 | level 225, with another Ultra Beast in the party |
| 3 | Pheromosa (795) | Pheromuse | 2795 | Bug/Fighting, kept | 570 → 660 | level 225, with another Ultra Beast in the party |
| 4 | Xurkitree (796) | Xurkigrove | 2796 | Electric, kept | 570 → 660 | level 225, with another Ultra Beast in the party |
| 5 | Celesteela (797) | Celestrata | 2797 | Steel/Flying, kept | 570 → 660 | level 225, with another Ultra Beast in the party |
| 6 | Kartana (798) | Origamurai | 2798 | Grass/Steel, kept | 570 → 660 | level 225, with another Ultra Beast in the party |
| 7 | Guzzlord (799) | Devourmaw | 2799 | Dark/Dragon, kept | 570 → 660 | level 225, with another Ultra Beast in the party |
| 8 | Naganadel (804) | Naganapex | 2804 | Poison/Dragon, kept | 540 → 660 | level 225, with another Ultra Beast in the party (Poipole counts) |
| 9 | Stakataka (805) | Ziggurak | 2805 | Rock/Steel, kept | 570 → 660 | level 225, with another Ultra Beast in the party |
| 10 | Blacephalon (806) | Pyrocephalon | 2806 | Fire/Ghost, kept | 570 → 660 | level 225, with another Ultra Beast in the party |

"Another Ultra Beast" is any of the eleven (the ten above and Poipole) but itself. Every typing is kept: the Ultra Beasts' typings are among the strangest of any region and are what they are.

## The designs

In National Pokédex order. Each entry: the category word, number, size, the stats (the species' as the game reads them → ours) with where the rise goes, the abilities, the evolution, the look and a Pokédex line. Every one keeps **Beast Boost**, its only ability, which already works for any species that has it (`AbilityEffectTable.Later.cs`).

### 1. Nihiliad, from Nihilego (793)

- **Typing**: Rock/Poison, kept.
- **The Sovereign Parasite Pokémon**, No. 2793. 1.6 m, 70.0 kg (Nihilego 1.2 m, 55.5 kg).
- **Stats**: 109 / 53 / 47 / 127 / 131 / 103 = 570 → **129 / 53 / 82 / 142 / 131 / 123 = 660** (+20 HP +35 Def +15 SpA +20 Spe): the rise goes to its Defense, which was 47, then its HP and Speed.
- **Abilities**: Beast Boost (was Beast Boost).
- **Evolution**: Nihilego at level 225 with another Ultra Beast in the party.
- **Look**: from our glassy hooded jelly with its trailing tentacles: taller, the hood folded into a crown of three glass lobes, eight tentacles where it had four, and the glass shot with a sheen of other colours. **New feature**: the crown of glass lobes.
- **Pokédex**: It drifts through the gaps between worlds and gathers the strength of every creature it has clung to. Its crown rings faintly when another of its kind is near.

### 2. Buzzcolossus, from Buzzwole (794)

- **Typing**: Bug/Fighting, kept.
- **The Colossus Pokémon**, No. 2794. 3.0 m, 480.0 kg (Buzzwole 2.4 m, 333.6 kg).
- **Stats**: 107 / 139 / 139 / 53 / 53 / 79 = 570 → **117 / 149 / 139 / 68 / 93 / 94 = 660** (+10 HP +10 Atk +15 SpA +40 SpD +15 Spe): the rise goes to its Sp. Def, which was 53, then its Sp. Atk and Speed.
- **Abilities**: Beast Boost (was Beast Boost).
- **Evolution**: Buzzwole at level 225 with another Ultra Beast in the party.
- **Look**: from our red, swollen mosquito with its great arms: larger, the arms grown to the ground like a gorilla's, its proboscis longer and banded, and a second pair of translucent wings. **New feature**: the second pair of wings.
- **Pokédex**: It lifts boulders bigger than a house to show off, then flexes for anyone watching. It drinks only nectar now, and in great quantities.

### 3. Pheromuse, from Pheromosa (795)

- **Typing**: Bug/Fighting, kept.
- **The Graceful Pokémon**, No. 2795. 2.2 m, 30.0 kg (Pheromosa 1.8 m, 25 kg).
- **Stats**: 71 / 137 / 37 / 137 / 37 / 151 = 570 → **101 / 137 / 67 / 137 / 67 / 151 = 660** (+30 HP +30 Def +30 SpD): the rise goes only to its HP and defences, which were 71, 37 and 37; its attacks and Speed were already its strength.
- **Abilities**: Beast Boost (was Beast Boost).
- **Evolution**: Pheromosa at level 225 with another Ultra Beast in the party.
- **Look**: from our slender white insect with its long legs and its antennae: taller, a veil of white down from its shoulders like a train, its antennae long and curling, and a shell of pale gold over its back. **New feature**: the veil of down.
- **Pokédex**: Its every step is a dance. It never lets a speck of this world's dust settle on its veil, and it flees anyone who reaches to touch it.

### 4. Xurkigrove, from Xurkitree (796)

- **Typing**: Electric, kept. *Alternative*: Electric/Grass.
- **The Power Grove Pokémon**, No. 2796. 4.6 m, 140.0 kg (Xurkitree 3.8 m, 100 kg).
- **Stats**: 83 / 89 / 71 / 173 / 71 / 83 = 570 → **103 / 89 / 96 / 173 / 96 / 103 = 660** (+20 HP +25 Def +25 SpD +20 Spe): the rise goes to its defences, which were 71, then its HP and Speed; its Sp. Atk was already 173.
- **Abilities**: Beast Boost (was Beast Boost).
- **Evolution**: Xurkitree at level 225 with another Ultra Beast in the party.
- **Look**: from our tangle of black wires with its star of a head: taller, three trunks of wire twisted together, branches of wire at its crown lit at their tips (Glow, white), and roots of cable that spread round its feet. **New feature**: the lit branches.
- **Pokédex**: It roots its cables into a hill and draws the lightning down to it. Where it has stood for a night, the ground hums for a year.

### 5. Celestrata, from Celesteela (797)

- **Typing**: Steel/Flying, kept.
- **The Orbiter Pokémon**, No. 2797. 11.0 m, 999.9 kg (Celesteela 9.2 m, 999.9 kg; question 4).
- **Stats**: 97 / 101 / 103 / 107 / 101 / 61 = 570 → **107 / 111 / 113 / 117 / 111 / 101 = 660** (+10 HP +10 Atk +10 Def +10 SpA +10 SpD +40 Spe): the rise goes to its Speed, which was 61, and a little to every other.
- **Abilities**: Beast Boost (was Beast Boost).
- **Evolution**: Celesteela at level 225 with another Ultra Beast in the party.
- **Look**: from our tall green and white figure with its arms of rocket stages: taller, a third stage on each arm, a ring of white panels round its waist like a station's, and its flames longer and blue. **New feature**: the ring of panels.
- **Pokédex**: It rises above the clouds and circles the world before coming down again. Those who see its flames cross the night sky make a wish.

### 6. Origamurai, from Kartana (798)

- **Typing**: Grass/Steel, kept.
- **The Folded Blade Pokémon**, No. 2798. 0.5 m, 0.3 kg (Kartana 0.3 m, 0.1 kg).
- **Stats**: 59 / 181 / 131 / 59 / 31 / 109 = 570 → **99 / 181 / 131 / 69 / 71 / 109 = 660** (+40 HP +10 SpA +40 SpD): the rise goes to its HP and Sp. Def, which were 59 and 31; its Attack was already 181.
- **Abilities**: Beast Boost (was Beast Boost).
- **Evolution**: Kartana at level 225 with another Ultra Beast in the party.
- **Look**: from our little figure of folded white paper with its blade of a head: larger, folded into a warrior's armour with a crest, a second blade on each arm and a long folded scarf. **New feature**: the folded scarf.
- **Pokédex**: It folds itself into a new shape after every duel it wins. A sheet of paper it has touched can cut through steel.

### 7. Devourmaw, from Guzzlord (799)

- **Typing**: Dark/Dragon, kept.
- **The Devourer Pokémon**, No. 2799. 6.5 m, 950.0 kg (Guzzlord 5.5 m, 888 kg).
- **Stats**: 223 / 101 / 53 / 97 / 53 / 43 = 570 → **223 / 111 / 83 / 107 / 83 / 53 = 660** (+10 Atk +30 Def +10 SpA +30 SpD +10 Spe): the rise goes to its defences, which were 53; its HP was already 223.
- **Abilities**: Beast Boost (was Beast Boost).
- **Evolution**: Guzzlord at level 225 with another Ultra Beast in the party.
- **Look**: from our black mouth on legs with its toothed hands: larger, a second mouth opening in its chest, its hands wider and a crest of spines along its back. **New feature**: the second mouth.
- **Pokédex**: It has eaten mountains and is still hungry. It swallows whatever it sees, and nobody knows where it goes.

### 8. Naganapex, from Naganadel (804)

- **Typing**: Poison/Dragon, kept.
- **The Poison Lance Pokémon**, No. 2804. 4.2 m, 190.0 kg (Naganadel 3.6 m, 150 kg).
- **Stats**: 73 / 73 / 73 / 127 / 73 / 121 = 540 → **103 / 93 / 103 / 137 / 103 / 121 = 660** (+30 HP +20 Atk +30 Def +10 SpA +30 SpD): +120, to the set's 660 (question 3): to its HP and defences, which were 73, then its Attack.
- **Abilities**: Beast Boost (was Beast Boost).
- **Evolution**: Naganadel at level 225 with another Ultra Beast in the party; a Poipole counts.
- **Look**: from our purple dragon of wings and its three stingers: longer, the stingers on its head grown into a crown of five, its wings in two pairs and a lance of a tail lit at its tip (Glow, white). **New feature**: the crown of five stingers.
- **Pokédex**: Its poison glows in the dark, and it fires it from its tail like a falling star. It once was the smallest of its kind.

### 9. Ziggurak, from Stakataka (805)

- **Typing**: Rock/Steel, kept.
- **The Ziggurat Pokémon**, No. 2805. 6.5 m, 960.0 kg (Stakataka 5.5 m, 820 kg).
- **Stats**: 61 / 131 / 211 / 53 / 101 / 13 = 570 → **101 / 141 / 211 / 63 / 111 / 33 = 660** (+40 HP +10 Atk +10 SpA +10 SpD +20 Spe): the rise goes to its HP, which was 61, then its Speed; its Defense was already 211.
- **Abilities**: Beast Boost (was Beast Boost).
- **Evolution**: Stakataka at level 225 with another Ultra Beast in the party.
- **Look**: from our wall of grey blocks with its eyes between them: taller, the blocks stepped back in tiers like a ziggurat, a gate at its foot, and the eyes on every tier. **New feature**: the tiers.
- **Pokédex**: More of its kind join it with every battle, until it stands as tall as a temple. Each block keeps watch in its own direction.

### 10. Pyrocephalon, from Blacephalon (806)

- **Typing**: Fire/Ghost, kept.
- **The Grand Finale Pokémon**, No. 2806. 2.3 m, 20.0 kg (Blacephalon 1.8 m, 13 kg).
- **Stats**: 53 / 127 / 53 / 151 / 79 / 107 = 570 → **93 / 127 / 83 / 151 / 99 / 107 = 660** (+40 HP +30 Def +20 SpD): the rise goes to its HP and defences, which were 53; its attacks and Speed were already its strength.
- **Abilities**: Beast Boost (was Beast Boost).
- **Evolution**: Blacephalon at level 225 with another Ultra Beast in the party.
- **Look**: from our clown in black and white with its head of fireworks: taller, three heads of fireworks juggled over its shoulders, a ruff of sparks round its neck and a cape striped black and white. **New feature**: the three heads.
- **Pokédex**: It juggles three heads of fireworks, and each one it throws ends in a bigger blast. It never bows until its audience is asleep.

## Balance

- **Totals**: every one 660, as every region's legendary set (the main document's island guardians among them). The rise is +90, Naganapex's +120.
- **Highest of each stat**: HP 223 (Devourmaw, Guzzlord's own); Atk 181 (Origamurai, Kartana's own); Def 211 (Ziggurak, Stakataka's own); SpA 173 (Xurkigrove, Xurkitree's own); SpD 131 (Nihiliad, Nihilego's own); Spe 151 (Pheromuse, Pheromosa's own). No stat of the set passes its forebear's highest: the rise goes only to the stats each lacked, so the Ultra Beasts keep their lopsided shapes.
- **Flagged as strong**: Origamurai (181 Attack with Beast Boost, now on 99 HP and 71 Sp. Def) and Pheromuse (137 in both attacks and 151 Speed, now with 101 HP). *To turn either down*: half the rise to HP.

## Open questions

1. **What "another Ultra Beast" means.** *Default*: the user's words, **any of the eleven but itself**, which needs one small change: an evolution row may name several species, any one of which in the party is enough (`EvolutionData.Species` as today, and a list beside it read by `LevelWithSpeciesInParty`), with its test. *Alternatives*: a ring of ten, each with the next in National order (Nihilego with Buzzwole, …, Blacephalon with Nihilego), as the island guardians', which runs today; or each with Poipole, the one that grows.
2. **Legendary or not.** The data marks none of them legendary. *Default*: **made a legendary set by the row** (one optional field of the table's row, `legendary`, which `SpeciesPastHundred.cs` reads before the forebear's): caught at 3, banned from the Battle Frontier as every legendary's evolution (`FrontierRules.IsBannedOfOurs`), and 660 under the tests' rule for a set. *Alternative*: left as the data has them, which caps them at 600 (+30, Naganadel +60) and lets them into the Frontier.
3. **Naganadel.** *Default*: one of the set like the others, +120 to 660, with any Ultra Beast in the party, a Poipole among them. *Alternatives*: Naganadel alone is built and the other nine aren't (the main document's other alternative, since it is the one that already grew); or Naganadel alone needs no partner, since it was a Poipole once.
4. **Celesteela's weight.** It already weighs 999.9 kg, the data's heaviest. *Default*: Celestrata keeps **999.9 kg**, the only design of any batch not heavier than what it comes from. *Alternative*: a heavier weight, which the Pokédex's pages and the weight-based moves (Heavy Slam, Low Kick) take, but which no species of the data has.
5. **The level.** *Default*: **225**, with the island guardians, after Alola's Champion. *Alternative*: 228, Alola's post-game's highest, since the Ultra Beasts are met only there.

## The table to answer

One row a species. The user answers each with yes, a change, or no.

| # | Ours | Typing | Abilities | HP/Atk/Def/SpA/SpD/Spe (total) | Δ | Evolves |
|---|---|---|---|---|---|---|
| 1 | Nihiliad | Rock/Poison | Beast Boost | 129/53/82/142/131/123 (660) | +20 HP +35 Def +15 SpA +20 Spe | level 225, with another Ultra Beast in the party |
| 2 | Buzzcolossus | Bug/Fighting | Beast Boost | 117/149/139/68/93/94 (660) | +10 HP +10 Atk +15 SpA +40 SpD +15 Spe | level 225, with another Ultra Beast in the party |
| 3 | Pheromuse | Bug/Fighting | Beast Boost | 101/137/67/137/67/151 (660) | +30 HP +30 Def +30 SpD | level 225, with another Ultra Beast in the party |
| 4 | Xurkigrove | Electric | Beast Boost | 103/89/96/173/96/103 (660) | +20 HP +25 Def +25 SpD +20 Spe | level 225, with another Ultra Beast in the party |
| 5 | Celestrata | Steel/Flying | Beast Boost | 107/111/113/117/111/101 (660) | +10 HP +10 Atk +10 Def +10 SpA +10 SpD +40 Spe | level 225, with another Ultra Beast in the party |
| 6 | Origamurai | Grass/Steel | Beast Boost | 99/181/131/69/71/109 (660) | +40 HP +10 SpA +40 SpD | level 225, with another Ultra Beast in the party |
| 7 | Devourmaw | Dark/Dragon | Beast Boost | 223/111/83/107/83/53 (660) | +10 Atk +30 Def +10 SpA +30 SpD +10 Spe | level 225, with another Ultra Beast in the party |
| 8 | Naganapex | Poison/Dragon | Beast Boost | 103/93/103/137/103/121 (660) | +30 HP +20 Atk +30 Def +10 SpA +30 SpD | level 225, with another Ultra Beast in the party |
| 9 | Ziggurak | Rock/Steel | Beast Boost | 101/141/211/63/111/33 (660) | +40 HP +10 Atk +10 SpA +10 SpD +20 Spe | level 225, with another Ultra Beast in the party |
| 10 | Pyrocephalon | Fire/Ghost | Beast Boost | 93/127/83/151/99/107 (660) | +40 HP +30 Def +20 SpD | level 225, with another Ultra Beast in the party |

## The technical plan (for after the user's answers)

The main document's pipeline: each design a row of `tools/DataImporter/Overrides/species-past-hundred.json` under "Batch 7, Alola", two written Pokédex entries and a habitat (Rare) each in `Data/dex-entries.json` (`--check-entries` clean), a run of the importer, and the tests (`SpeciesPastHundredTests`' rule for a legendary set, `SpeciesPastHundredAlolaTests` for the set's condition). With the defaults, two small changes come first, each with its test: a row's list of species for `LevelWithSpeciesInParty` (question 1) and a row's `legendary` (question 2). The sculpts wait for the user (wave 53's rule: ours stay generated by rule from their data).

## How the numbers were checked

A Node.js script in the session's scratch folder, reading only `species.json`, `abilities.json`, `items.json`, the table and the D14 proposal documents, checked for each of the ten: that the forebear is Alola's, alone or the last of its line (Naganadel, Poipole's end), with Beast Boost as its only ability and no newer stats; that the typing is the forebear's; that every rise is a multiple of five, no stat falls and the six add up to 660; that the name is one word of at most 12 letters taken by no species, form, ability or item of the data, no row of the table, no name of any D14 proposal document and no other design of this set; that the number is 2000 + the forebear's; that each is taller and heavier than what it comes from (Celestrata's 999.9 kg the one exception, question 4) and no heavier than 999.9 kg; and that the eleven species the condition names (the ten and Poipole) exist.
