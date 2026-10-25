# D14 · Evolutions past level 100, batch 6 (Kalos), supplement: Zygarde

Plan 03 · D14, written in wave 53 beside the build of batch 6 ([D14-proposals-kalos.md](D14-proposals-kalos.md), whose 24 designs the user approved on 2026-10-26). To that document's question 2 ("No legendary set") the user answered **b: Zygarde alone, as the trio's guardian**, which the document named as an alternative without a design. This is that design, in the same format, for the user to approve. **Not yet answered by the user.** Nothing here is built: no species, no evolution row, no entries, no test. Batch 6's other 24 are built.

**What is settled and applied without asking again**: the earlier batches' rules (a legendary's evolution rises a region's legendary set to **660**, by +80 at most; the number is 2000 + the species'; abilities only from `abilities.json`, as many slots as the species had; evolution by level with a condition the engine already runs, never a key item; ours have no forms of their own; a legendary's evolution joins the Battle Frontier's ban list), and batch 6's question 1 (each evolves within Kalos's own climb, about 185 to 205).

Every name, type, ability, move, stat and sum below was checked by a script that reads only `species.json` and `abilities.json`, with every species and form of the data and every name in the nine `D14-proposals-*.md` documents (1,921 names) held against the name.

## The design

### 1. Ecosentinel, from Zygarde (718): yes

- **Typing**: Dragon/Ground, kept. *Alternative*: Dragon/Grass (the order of the living land).
- **The Equilibrium Pokémon**, No. 2718. 6.2 m, 720.0 kg (Zygarde 5 m, 305 kg; its 10% Forme 1.2 m, 33.5 kg; its Complete Forme 4.5 m, 610 kg; its Mega is left out of the measure, as every batch's).
- **Stats**: 108 / 100 / 121 / 81 / 95 / 95 = 600 → **118 / 110 / 131 / 81 / 110 / 110 = 660** (+10 HP +10 Atk +10 Def +15 SpD +15 Spe): the rise is +60, to the 660 of a region's legendary set (question A), to its Sp. Def and Speed, then its HP, Attack and Defense; its Sp. Atk, which a physical guardian doesn't use, is kept. The Complete Forme's 708 stays above it, so the change Zygarde already has is still its greatest.
- **Abilities**: Aura Break (was Aura Break). A Power Construct Zygarde (a form of the data, `Zygarde-10-Power-Construct` and `Zygarde-50-Power-Construct`) takes Aura Break as it evolves, its form lost (question C).
- **Evolution**: Zygarde at level 205, knowing Core Enforcer. Data: `{"targetSpecies":"Ecosentinel","method":"LevelKnowsMove","level":205,"move":"Core Enforcer"}`. Core Enforcer is Zygarde's own move, learned at level 1 (`species.json`); the Zygarde Cube is a key item, which no evolution asks for.
- **Look**: from our green and black serpent-hound of the 50% Forme, its body a lattice of hexagonal cells: longer and broader, the green cells along its body lit from within (Glow, green), the collar of fins at its neck grown into a ring of five hexagonal plates that stand round its head like a crown, and a single red core showing at its chest, which the 50% Forme hides. **New feature**: the ring of five plates round its head.
- **Pokédex**: When life and death fall out of balance it gathers every cell scattered across the land, and the land itself grows quiet to watch.

## Balance

- **Total** 660, the most a region's legendary set rises to, and +60, under the +80 cap. Its highest stat, Defense 131, is Zygarde's own 121 and ten; Ho-Oh's beasts, the Regis and the lake guardians stand at 660 too.
- **Flagged**: Aura Break with 110 Attack and 110 Speed is a stronger Zygarde, but without Power Construct it can never reach the Complete Forme's 216 HP. *To turn down*: Speed 100 and the 10 to Sp. Atk.

## Open questions: only what is new for Zygarde

A. **The 660 reading.** The rule is that a region's legendary set rises to 660. Kalos's set is Zygarde alone (the user's answer to the main document's question 2), so 660 is a rise of 60 from its 50% Forme's 600. *Default*: **660** (+60), as every legendary set before it. *Alternative*: +80 to 680 (the cap of a rise, not of a set), which meets Xerneas's and Yveltal's 680 and makes the guardian their equal.

B. **The level.** Batch 6's band is 188 to 203, the first Gym Leader to the Elite Four; the earlier batches put a set's legendaries at the climb's top (Hoenn's at 160, Sinnoh's lake guardians at 150). *Default*: **205**, the level the study gives Kalos's Champion and the end of Kalos's climb, so it evolves only once the region's League is reached. *Alternative*: 203, the band's own top (with Noivern's end).

C. **Its formes.** Zygarde's 10% Forme and its two Power Construct formes are forms of the data; its Complete Forme is a battle's shape and its Mega a Mega. *Default*: **any forme evolves**, its forme lost, as batch 6's Gourgeist sizes and Furfrou trims (a form that is no regional one meets the engine's `Evolution.FitsForm`), and a Power Construct Zygarde takes Aura Break. *Alternative*: the 50% Forme alone (which needs a rule of the engine the earlier batches did without: an evolution that refuses every form of its own species).

D. **The condition.** *Default*: knowing **Core Enforcer**, its signature move. *Alternatives*: knowing Land's Wrath (learned at 48), or with a Xerneas or a Yveltal in the party (`LevelWithSpeciesInParty` names one species, so only one of the two could be asked for).

## The table to answer

| # | Ours | Typing | Abilities | HP/Atk/Def/SpA/SpD/Spe (total) | Δ | Evolves |
|---|---|---|---|---|---|---|
| 1 | Ecosentinel | Dragon/Ground | Aura Break | 118/110/131/81/110/110 (660) | +10 HP +10 Atk +10 Def +15 SpD +15 Spe | level 205, knowing Core Enforcer |

## The technical plan (for after the user's answer)

As batch 6's: an entry of `tools/DataImporter/Overrides/species-past-hundred.json` at the end of batch 6's block, two written Pokédex entries and a habitat (Rare) in `Data/dex-entries.json` (`--check-entries` clean), a run of the importer, and in `SpeciesPastHundredTests`' climb the Kalos line given a legendary's end (`legend ? 205` by question B). `SpeciesPastHundredKalosTests.KalossSpeciesEvolveWithinKalossOwnClimb`, which holds today that no legendary of Kalos goes further, is changed to hold that Zygarde alone does. A test: a Zygarde at 205 knowing Core Enforcer becomes Ecosentinel and not at 204 or without the move; a 10% and a Power Construct Zygarde evolve with no form left over and Aura Break; Ecosentinel totals 660 and may not enter the Battle Frontier. Nothing in the engine is needed. No sculpt: until the user says otherwise it is generated by rule from its data, as every species of ours.
