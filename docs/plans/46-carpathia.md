# Plan 46 · Carpathia, a region of our own after Romania

Written 2026-11-09, before any session, at the user's request, from [the study](studies/romania-region-study.md) and the user's answers of the same day (decisions 1 to 12, below).

**Goal**: a tenth region, **Carpathia**, that has no original game at all. It is drawn from Romania: the ring of the Carpathians round the Transylvanian plateau, the plains outside it, the Danube to its delta and the Black Sea. It is built on [plan 32](32-open-world-engine.md)'s open-world engine and comes after Paldea in the chain. It has:
- **its own Pokédex of about 250 new species** (249 since 2026-11-12) with no forebear in any game, three starter lines, a trio of legendaries, the three Fates, eight mythicals woven into the story and 25 Carpathian forms of existing species;
- **a nineteenth type, Sound**, because music is at the heart of Romania;
- **a primary gimmick of our own, the Tales (*basme*)**: once a battle a trainer tells a tale learned in the world, which changes the field for both sides for five turns and wakes the Pokémon bound to it; the other trainer may answer with a tale of their own;
- **a secondary gimmick, the winter masks**: the bear, goat, horse and stag of the New Year's dances, crafted or found at the festivals, giving a Pokémon a role for a battle;
- a story of a team that wants **the sun to stand still**, with the Zmeu;
- gyms in **any order**, levels that **climb past Paldea** under a cap raised to about 300.

Everything in it is ours: places, people, species, words, art and music. Real places of Romania inspire its landmarks; the map's names are invented with Romanian roots (decision 4).

## Where we are

- **The engine.** Plan 32 · OW0 to OW9 are built (OW10 and the user's gates left); plans 33 and 34 will have proven it twice, with a mount (plan 34 · PD2's `MovementGrants` on the capsule) and raids, before this plan starts. Rooms stay grid rooms behind doors (plan 32 · decision 6).
- **Regions.** Nine regions are chained Sinnoh to Paldea (`RegionDatabase`), and plan 34 says the chain ends at Paldea. Each region has a file (`Data/regions/<id>.json`), a crossing (`GameEngine.CrossTo`), harbours and Fly between regions. Carpathia is a tenth `Region`, after Paldea, with a link from Paldea's harbour.
- **Species of ours** (plan 03 · D14) are 400 evolutions past level 100 of existing species, numbered from `PokemonDatabase.OursFrom` (2000) and in regional blocks of a thousand from 4000 to about 8025, made by the importer from a hand-written table (`SpeciesPastHundred`), each grown from a forebear's values. **Since CP1 (2026-11-12) a species can be made from nothing** (`SpeciesOfNothing`, numbers from 10001); the table waits for CP3's approved designs.
- **Types** are an enum of nineteen since CP2 (2026-11-12): the games' eighteen and **Sound**, ours, last (`Data/Enums.cs`, `TypeChart`).
- **The cap** is `Ruleset.HighestLevel` = 260 (plan 18 · W4), read by 26 places, nearly all as a clamp; Paldea's fair climb puts its Champion at about 258 and its post-game at 262 ([the W4 study](studies/W4-level-curve-study.md), section 4).
- **The later-mechanic pattern** (plan 06 · R20 to R23, R30) has been followed five times: a `Ruleset` flag, a key item, a flag on the choice, a `WhyNot`, an event, the AI's use, `RunTerms.NoLaterMechanics`, a field of the Vs. Recorder's file. Its looks (plan 25) and a layer of the battle songs (plan Y · YD5) have a pattern too.
- **Seasons** exist on the grid only (plan 22 · U3, U4); the open renderer has none.
- **Crafting** (plan 39 · CR1) has the kit, materials and drops; CR3 to CR5 add making.
- **The story** (docs, 2026-11-12): [the outline](decisions/CP-story-outline.md) with the user's beats, answers and approved names (Doina Lupescu, Ilie Sorescu, Aurelian Zenovie, Mara, Costel, Radu and Ilinca), and [the chapters](decisions/CP-story-chapters.md), scene by scene from the prologue to the post-game, with the Champion (Ruxandra Zorescu, proposed), Team Zenith's four executives, the eight leaders, the twelve tales' places and Tale Forms, the professors' creepy-voice moments, the roamers and the story's map onto the sessions below. The chapters wait for the user's ten open questions; CP18 to CP27 go ahead on their recommendations.

## Design

### The region's file and its place in the chain

`Data/regions/carpathia.json`: `field: open`, plan 30's `movement` (`Free`, `Analog`, `Grown`, the region's own traversal grants), its professor and rival, its three starters, eight badges, the `Arrival` marker a Champion crossing from Paldea lands on, `levels` and `caps` (below), the Carpathian Pokédex. A `Region` value after Paldea with `Generation` 0 (ours, as D14's species). Paldea's harbour gains the link (plan 34 is amended by this plan: the chain no longer ends there). A new game may also begin in Carpathia (`--region Carpathia`), as in every region, at its own levels.

### The map

Built as plan 32's data (`Data/world3d/carpathia/`), hand-written terrain, carves, water and roads, at about **3.5 km by 2.5 km**. The shape is Romania's: the Carpathian arc, the plateau inside it, plains to the south and east, the river along the south to a delta, the sea on the east. The study's table of places is the list to draw from; the region's first session (CP18) writes the map's sketch and names with the user. Landmarks to build:

- the **hairpin road** over the ridge with its glacial lake and its tunnel (the Transfăgărășan);
- the **plateau of stone faces** (Bucegi's Sphinx and Babele), a legendary's place;
- **castles on crags** (Bran, Corvin, Peleș): the Ghost gym, the villain's stronghold, the League's palace;
- **walled old towns** of painted houses whose roofs have eyes (Sibiu, Brașov, Sighișoara) and fortified churches;
- the **salt mine** as a cavern with a lake and a wheel (Turda), and the **ice cave** (Scărișoara);
- the **river's gorge** with a face carved in the cliff (the Iron Gates), and the **delta** of reed channels and floating islands;
- the **sea coast** with its resort and its old casino on the shore, the region's harbour;
- **the capital**: boulevards, an arch, park lakes and a palace too big for its square;
- the **wooden north**: needle-spired wooden churches, carved gates, painted monasteries, a cemetery of painted crosses (Maramureș, Bucovina), where many tales are found;
- the **mud volcanoes**' grey moonscape, and the **Dacian sanctuary** of stone circles in the forest (the post-game).

**Traversal**: the region's own way about, on `MovementGrants`: a pony for the plains and passes, a boat for the delta's channels, a glide from the heights; each granted by the story.

**Architecture**: new `Architecture`s and style-guide rows first: *Plateau* (painted walled towns, eyed roofs), *Castle* (on crags, built over carves), *Wooden* (Maramureș), *Monastery* (painted walls), *Capital* (belle-époque boulevards), *Delta* (reed and timber), *Coast* (resort blocks).

**Seasons**: the open renderer gains the grid's seasons (`SeasonPalette`, season swaps of chunks, tables by season) and Carpathia has them, a month each as Unova's: snow on the ridge in winter, the plateau gold in autumn.

### Gyms and the story

**Gyms in any order** (decision 8), as Paldea's: the levels of the first three fixed, the later ones scaled to the badges held. Suggested types and places, the region's session to settle them with the user: Water on the coast, Flying in the delta, Rock on the pass, Ice in the ice cave, Grass in the wooden north, Ghost in the castle, Psychic among the stone faces, **Sound in the capital's opera**. Each has a test before the leader, as a grid room's puzzle or a task in the open world: a *căluș* dance, a shepherd's round-up, a sledge run, a painted-egg hunt, a tale told to an old woman.

**The villain** (decision 11) is a team of ours that wants **the sun to stand still**: an endless day, no more winters or nights, by binding the **Zmeu**, who in the tales steals the sun. The tales are how the player undoes it, and the Vârcolac, who eats the moon, is the balance. The story stays with folklore and away from the country's real politics.

*Since 2026-11-12 the story is [the outline](decisions/CP-story-outline.md) and [the chapters](decisions/CP-story-chapters.md)*, which take the place of this paragraph's first sketch: Team Zenith binds **Furtunaur** on the ridge; the two professors, Doina Lupescu and Ilie Sorescu, come on a hunch after the player's lost `???` fight, bind it with the Zmeu's and the Vârcolac's tales and become **Solzmeu** and **Lunacolac**, who roam afterwards; the ridge opens at 7 badges (the plateau's and the castle's among them), the League asks all eight. The gyms' types and places are the chapters' section 4.

**The calendar**: Carpathia keeps Romania's days through plan 40's festivals: **Mărțișor** (1 March, a red-and-white charm, Baba Dochia's days to 9 March), **Dragobete** (24 February), **Sânziene** (24 June, the fairies' night), Easter's painted eggs, the Christmas carollers, and the **New Year's mask dances**, where the masks are won.

### The new species

About **250** (decision 2: 120 at first, 249 since 2026-11-12: Nos. 1 to 235 the region's species, 236 to 249 the Fates, the legendaries and the mythicals), numbered **from 10001** in the order of the Carpathian Pokédex, outside the National 1,025 (`GetAll()` stays the games' alone) and outside the Pokédex of our own (`Pokedex.Ours` stays D14's). A **Carpathian Pokédex** of its own, with a diploma (plan 40 · GE4's pattern) and the charm a regional Pokédex earns (decision 6).

- **Made from nothing**: a hand-written table `tools/DataImporter/Overrides/species-carpathia.json` with every value a species has (stats, types, abilities, catch rate, growth, egg groups, effort, base experience, height, weight, colour, shape, gender ratio, learnset, TMs, tutors, evolutions), which the importer makes whole species of (`SpeciesOfNothing.cs`, beside `SpeciesPastHundred.cs`); a test holds every field present and every move and ability existing and run.
- **Designed in batches** that the user approves, as D14's were (`decisions/CP-proposals-*.md`), the first being the starters and legendaries ([CP-proposals-starters-legendaries.md](decisions/CP-proposals-starters-legendaries.md)). **25 regional forms** of existing species (nine first, sixteen more on 2026-11-12: the salt-mine Sableye, the monastery painter Smeargle, the Sound lines of Noibat, Whismur and Jigglypuff, the goat dance's Skiddo, the lantern Drifloon, the giant Golett, the village balaur Dunsparce, the hora Spinda, the Dacian Bronzor, the linden-tea Sinistea, the wooden Chingling) in the region's own block of a thousand (10000 + the species' number would collide with the Pokédex's own numbers, so forms take **11000 + the species' number**).
- **Sculpted** from pictures, as D14's 400 were (`PokemonModels.Carpathia*.cs`), a batch of about 25 a session: about ten sessions for 249 species and 25 forms, where CP4 to CP8 were counted for half that.
- **The briefs** (2026-11-09, grown 2026-11-12) are `decisions/CP-dex-1-plateau.md` to `CP-dex-6-legends-and-mythicals.md`, read in that order with the `b` files after their area and the forms (`CP-dex-5-forms.md`) last; `tools/GeminiArt -- missing --set carpathia` lists what has no picture yet.
- **Named** in English and Romanian together; **the user reviews the Romanian** (decision 7): `Data/text/ro/names.json` gains the Carpathian species, written by hand.
- **Pokédex entries** written by hand (`dex-entries.json`), the check run as always.
- **About 20 signature moves and 15 abilities** of our own (moves from **3101**, after R31's 3001 to 3072), each with its effect, its look (`MoveFx`), its sound and scenario tests (Showdown's oracle knows none of them).

### The Sound type

A **nineteenth type** (decision 5), at the end of `PokemonType`. Its matchups are proposed below and approved by the user in CP2:

| Sound attacking | |
|---|---|
| Super effective against | Rock, Ice, Psychic (resonance shatters stone, ice and thought) |
| Not very effective against | Steel, Ground, Sound (metal rings it back, earth swallows it) |
| No effect on | — |

| Sound defending | |
|---|---|
| Weak to | Dark (silence), Electric (feedback), Ground (it swallows sound) |
| Resists | Normal, Flying, Sound |
| Immune to | — |

**The rules round it**:
- **Every Sound-type move is a sound move** (carries `MoveFlags.Sound`), so Soundproof is the type's immunity, Throat Spray and Punk Rock answer it, and the Throat Chop silence stops it. Liquid Voice turns it Water as it turns any sound move.
- **The games' existing sound moves keep their types** (Hyper Voice stays Normal, Boomburst stays Normal); the Sound type's own moves are new ones of ours.
- **Inverse battles** turn its row round as every row (`TypeChart.GetEffectiveness(..., inverse)`).
- **What is per type gains a Sound entry**: `Words`, `TypeSigns` (a note for its sign), its colour, the trainer AI's 40ths chart, Tera Sound (a Tera type and Tera Blast), a Sound gem, a type-boosting held item (a tuning fork), a resist berry, a Z-crystal and a Max Move of its own (each a row of the later mechanics), the Hidden Power table **not** (Platinum's sixteen), Arceus and Silvally **not** (no plate, no memory: decision 13 below), the Battle Hall's board **not** (Platinum's seventeen).
- **What must not move**: the golden battles under Platinum's rules (no Sound in them), the oracle (Sound left out of its scenarios: Showdown has no such type), the coverage report's floors.

### The Tales, the primary gimmick

A *basm* is a Romanian fairy tale (decision 1).

- **What a battle does**: once a battle, with the **Book of Tales** (a key item) in the bag and a tale learned, a trainer **tells a tale** with their move (`BattleChoice.Tale`, `WhyNotTale`). For **five turns** the tale's rule holds over the whole field for **both** sides (a `TaleState` in `FieldState`, with its turns, beside the weathers and terrains). Each Pokémon **bound to that tale** on the teller's side **wakes into its Tale Form** (`FormKind.Tale`) for as long as the tale is told, and goes back as it ends. The other trainer may **answer** with a tale of their own, once, which ends the first tale at once and begins theirs (`TaleAnswered`). A tale already told can't be told again in the same battle.
- **The tales**, about twelve, each a rule, a sky (`StageLight`), a layer of the battle song and a Tale Form for the species bound to it. A first list for CP11 to settle: **the Zmeu's** (the sun cannot set: Fire stronger, no night forms), **the Vârcolac's** (the eclipse: the field dark, Dark and Ghost stronger, accuracy down), **the Solomonar's** (the storm: a weather the teller owns), **Baba Dochia's** (the cold returns: each switch-in sheds a layer of its defence), **the Ielele's** (the night dance: Sound moves strike twice), **Miorița's** (the shepherd's lament: when a Pokémon faints, an ally takes its strength), **Muma Pădurii's** (the forest closes: Grass heals, the foe's switches are held), **Făt-Frumos'** (youth without age: no stat may fall), **the Uriași's** (the giants: the heaviest hit hardest), **the Spiriduș'** (the house sprite: items work twice), **the Sânziene's** (midsummer: healing doubled), **the Draco's** (the war banner: the team's attacks rise each turn), each to be balanced in CP11.
- **Found in the world**, never bought: from an elder in a village, a painted monastery wall, a carved gate, a stone of the sanctuary, a festival. The Book keeps each tale with a page of its story (a screen of its own, like the Journal). Trainers of the region know tales by their place in the story.
- **The trainer AI** tells its best tale when it helps its side more than the foe's (worked out on the battle's own numbers, like R30's `TeraNow`) and answers a tale that hurts it.
- **On screen**: the sky changes, the field shows the tale's look, a Tale Form wakes with a cue, the song gains its tale layer (folk instruments over the theme, as the giant layer does), a line names the tale.
- **The pattern's rows**: `Ruleset.Tales`, the key item, `RunTerms.NoLaterMechanics` asking it, `BattleRecord` and the Vs. Recorder's file keeping the tales told, `TaleTold` and `TaleAnswered` events on their lines.

### The winter masks, the secondary gimmick

At New Year Romanian villages fill with masked dancers.

- **What a battle does**: once a battle, a trainer **masks a Pokémon** (`BattleChoice.Mask`, `WhyNotMask`); a mask is a role for the rest of the battle, changing its ability and its second type to the mask's:
  - the **bear** (*ursul*): bulk, takes hits meant for an ally;
  - the **goat** (*capra*): speed and mischief, steals the foe's raised stats;
  - the **horse** (*căluțul*): carries an ally out, a free switch each turn;
  - the **stag** (*cerbul*): the leader, raises the team each turn.
  A battle may see **one tale and one mask** from each side.
- **Where masks come from**: crafted with plan 39's kit from materials (fur, horn, wood, felt) once the workshop's mask pattern is learned, and won at the New Year's dances. A mask is a held thing of the bag's own pocket, worn only in battle.
- **The look**: a mask is an accessory fitted on the head bone of any body plan (`PokemonModels`' role bones), sculpted once per mask and scaled to the head, never a sculpt per species.

### Levels and the cap

The cap is **raised** (decision 3): `Ruleset.HighestLevel` to **300**. Carpathia's world files keep levels of its own (1 to about 70), and its `levels` map them as every crossed region's are: its story from about **262 to 286**, its Champion about **286**, its post-game to about **296**, its `caps` by badge in its own levels. What the raise touches: the 26 readers of `HighestLevel` (mostly clamps, nothing to change), Erratic and Fluctuating past 260 (`GetExpForLevel`), an overflow check of the damage formula at 300, `LevelMapping`'s bounds, the online check, R31's moves past 100 extended to 300 (moves of our own learned from 261 to 300), the crossed region's soft ceiling (`LevelCaps.Ceiling`), the golden levels files (`levels.txt`, `levels-crossed.txt`) and the ladder's test (`LevelLadderTests`). A new game begun in Carpathia plays at its own levels as any region does.

### Sound, voice and words

- **Music** of the region on its folk instruments: the *nai*, *cobză*, *țambal*, *bucium*, *fluier*, *taragot*; the free *doină* for nights and laments, the *hora* for towns, uneven 7/16 for battles; new instruments for the synthesiser where plan 31's set lacks them. Each tale has its layer. A credits song through Lyria in the region's own style.
- **Voices**: Kokoro designs with a Romanian accent for the cast (plan 26).
- **Words**: English is the source as everywhere; the Romanian of this region is **reviewed by the user** (decision 7), names first.

## Decisions for the user

Answered on 2026-11-09:

1. **The gimmick**: the Tales as the primary, the winter masks as the secondary.
2. **New species**: about 120; raised by the user on 2026-11-12 to **about 240 to 250** (249 written), with more regional forms (25) and more mythicals that move the story (8).
3. **Levels**: the cap raised past 260 (to about 300), Carpathia climbing on from Paldea.
4. **Names**: invented with Romanian roots.
5. **A new type**: yes, **Sound**.
6. **The Pokédex**: a Carpathian Pokédex with its diploma, outside the National 1,025.
7. **Romanian text**: the user reviews.
8. **Gyms**: in any order.
9. **The species' work**: starts early, beside the waves.
10. **The region's name**: Carpathia.
11. **The villain's goal**: to make the sun stand still, with the Zmeu.
12. **The legendaries**: the Zmeu, the Vârcolac and the Solomonar's storm-dragon, with the Dacian draco as the mythical.

Open, each with its recommendation (the session named goes ahead on it unless the user says otherwise):

13. **Sound's matchups** (CP2). *Recommended:* the tables above. Arceus and Silvally get no Sound form.
14. **Existing sound moves** (CP2). *Recommended:* they keep their types; Sound-type moves are new. The alternative retypes a few (Boomburst, Hyper Voice), which moves the golden battles and the oracle.
15. **One tale and one mask a battle** (CP11, CP13). *Recommended:* each side may tell one tale (and answer once) and mask one Pokémon. The alternative lets a mask be worn only by a Pokémon bound to the tale being told.
16. **The map's sketch and the towns' names** (CP18). The user judges the sketch, the first town and the names before the rest is built.
17. **The professor, the rival and the villain team's name** (CP18). Proposed with the starters' file. *Answered 2026-11-12*: two professors, Doina Lupescu and Ilie Sorescu; the rival Radu or Ilinca by the look; Team Zenith under Aurelian Zenovie; the apprentices Mara and Costel ([the outline](decisions/CP-story-outline.md)). The Champion, the executives and the leaders are proposed in [the chapters](decisions/CP-story-chapters.md), with eight questions of their own.

Answered on 2026-11-12 (the user, recorded in [the outline](decisions/CP-story-outline.md), "Answers", and [the chapters](decisions/CP-story-chapters.md)):

18. **The climax** is the ridge (Act III of the chapters, III-0 to III-10, one evening into one night): the rival at the foot, the executives on the road, the Champion holding the tunnel, the lost `???` fight against the bound Furtunaur, the two professors arriving each on a hunch, healing the team and binding it with the Zmeu's and the Vârcolac's tales, and becoming **Solzmeu** and **Lunacolac**, who weaken it for the catch and then roam. A session of its own, **CP25b**, was added for it. Zenovie's fate stays unspoken but implied; the professors speak through their new throats at a few moments, never explained; both roamers can be caught, never together; the professors stay Pokémon.
19. **The ridge opens at 7 badges, the League asks all 8.** The rival is Radu or Ilinca by the player's look; the Champion is met before the ridge and helps at several moments. The names Doina Lupescu, Ilie Sorescu, Aurelian Zenovie, Mara, Costel, Radu and Ilinca are approved.
20. **The staged battle the climax needs exists** since that day (CP25b, above): the `???` fight is a `wildbattle … hidelevel canlose`. The chapters' eight open questions (section 8) are still open.

## Sessions

Three tracks. **Species and type** (CP1 to CP10) may start beside the waves now (decision 9); **the gimmicks and the engine** (CP11 to CP17) need no region and may run once their seams are free; **the region** (CP18 to CP27) starts after plan 34 and every other plan but plan 27.

### CP1 · Species made from nothing (1 session)
- The table and `SpeciesOfNothing.cs`, the block from 10001 and forms from 11000, the Carpathian Pokédex (`Pokedex.Carpathia`, its words, its diploma), `GetAll()` and `Ours()` untouched, `Everyone()` taking them. Tests for the table's completeness and the counts. A placeholder species for the tests until CP3's are approved.

### CP2 · The Sound type (1 to 2 sessions; lane 3)
- `PokemonType.Sound`, the chart and its inverse, every per-type row (words, sign, colour, AI chart, Tera, gem, booster, berry, Z-crystal, Max Move), Soundproof's immunity through the flag, the golden battles and the oracle held still. Decisions 13 and 14 carried out.

### CP1 and CP2 as built (2026-11-12)
- **CP1** (`tools/DataImporter/SpeciesOfNothing.cs`, `docs/mechanics/rulings.md`, "Species made from nothing"): the table `Overrides/species-carpathia.json` (empty; a row leaving any value out stops the import naming it), a species' number 10000 + its Carpathian number (`PokemonDatabase.CarpathiaFrom` 10001, `CarpathianFormsFrom` 11000 kept for the forms), `IsOurs` still "no game's" so every exclusion of ours holds, D14's own readers moved to `IsPastHundred`, `IsCarpathian` and `PokemonDatabase.Carpathia()`, `GetAll()` the 1,025 and `Ours()` D14's alone. `Pokedex.Carpathia` (`carpathia`) with its words and diploma in English and Romanian, read from `Everyone()`; **the National Pokédex leaves Carpathia's species out** (decided here: "outside the National 1,025"), so they are listed in the Carpathian Pokédex alone. Owed: the region's file naming the Pokédex (CP18), its charm (CP27), the forms' block in use (CP8). The tests hold a placeholder line of their own, never in the data.
- **CP2** (`docs/mechanics/rulings.md`, "The Sound type"; style guide, "Type signs" and the colours' row): the chart above; Words (*Sunet* in Romanian), a sign (a note), teal `64,168,152`, a cry timbre, the move, Z-Move and Max Move sounds and looks; Tera Sound and its shard, the Sound Gem, the Tuning Fork, the Taci Berry, the Sonorium Z (**items 9920 to 9924**), Resounding Finale and Max Chorus (**moves 3090 and 3091**, generation 0; 3073 to 3089 free, 3101 on CP10's), two crafting materials (Hollow Reed, Echo Crystal). The Z-Move and Max Move carry no flags (as none do), so Soundproof doesn't stop them. Left out by `TypeChart.GamesTypes`: the randomiser, the Friend Safari, the sparring room, Conversion 2 and the help's chart; offered by `TypesWithSpecies` (none yet): the Monotype run and the Pokédex's search; the battle corpus draws no Sound item. **Owed**: the help's type chart drawn nineteen wide (a shot), Sound moves of its own (CP10), Hidden Power, the plates, the memories and the Battle Hall's board stay without it by decision 13.

### CP15 as built (2026-11-12)
- **The cap to 300** (`docs/mechanics/rulings.md`, "The level cap past 100 and a region's levels", its last entry): `Ruleset.HighestLevel` and `Ruleset.Modern.LevelCap` 300, the old 260 kept as `Ruleset.FormerHighestLevel`. Every reader of `HighestLevel` was a clamp or a bound and follows it (the constructors, `LevelTo`, `Relevel`, `Trainer.Relevelled`, the Rare Candy, the EXP Candies and the Day Care through `Pokemon.Cap`, the staged `hidelevel` foe, the console's and the scenarios' `wild`, which said 260 by hand, the online check, the save tool, `OpenPlaceFiles`, the content check). Erratic and Fluctuating go on by plan 18 · W4's rule (44,280,000 EXP for Fluctuating at 300, the dearest). The damage formula's steps are held under `Formulas.MostDamage` (2^28), its overflow check at 300. Saves, `SavedPokemonData` and the Vs. Recorder's file take 1 to 300 with no change of format. **The regions before Carpathia keep 260** (decided here): `LevelMapping.Top` and `LevelCaps.TopOf` hold a mapping, and a crossed region's soft ceiling, under 260 wherever the region's levels end at or under it, so Galar's post-game ceiling stays 260 (not 266) and nothing of `levels.txt`, `levels-crossed.txt` or the ladder moved; Carpathia's own `levels` climbing past 260 reach 300. Tests: `LevelThreeHundredTests`, `LevelCapTests` brought to 300.
- **Owed**: moves of our own learned from 261 to 300 (R31's table ends below 260), Carpathia's own `levels` and `caps` (CP18), the study's simulation run again with Carpathia's climb.

### CP3 · The first proposals approved (beside; docs)
- The starters and legendaries ([the file](decisions/CP-proposals-starters-legendaries.md)) answered by the user; then the proposals of the rest in batches of 25 to 30 (the Delta and coast, the plateau, the ridge and caves, the north and the capital) and the regional forms.

### CP4 to CP8 · Sculpts (5 sessions, H)
- About 25 species a session from the approved pictures, with their eyes, decals and shiny palettes, held by the model tests; the regional forms in the last. Since the Pokédex grew to 249 species and 25 forms (2026-11-12) this is about ten sessions, not five: CP8 is split into CP8 to CP8e as the batches come.

### CP9 · Entries and names (1 session)
- Two entries and a habitat for each, the Romanian names for the user's review.

### CP10 · Signature moves and abilities (2 sessions; lane 3)
- About 20 moves (3101 on) and 15 abilities, their effects, looks and sounds, scenario tests.

### CP11 · The Tales: rules (1 to 2 sessions; lane 3)
- `Ruleset.Tales`, the Book, `BattleChoice.Tale`, `TaleState`, the dozen tales' rules where the field's rules bite, `TaleTold` and `TaleAnswered`, the AI, the Vs. Recorder, `NoLaterMechanics`; balance by scenario tests.

### CP12 · The Tales: forms, looks, songs and the Book (2 sessions; H)
- Tale Forms (`FormKind.Tale`) and their sculpts for the species bound to tales, each tale's sky and field look, the tale layer of the battle songs, the Book of Tales' screen, `tale learn` in scripts.

### CP13 · The masks: rules and making (1 session; lane 3)
- `BattleChoice.Mask`, the four roles, `WhyNotMask`, the AI, the mask pocket, the crafting pattern (plan 39) and the festival's gift.

### CP14 · The masks: on every body (1 session; H)
- The four masks sculpted, fitted to the head bone of every body plan, held by a test on one species of each plan.

### CP15 · The cap to 300 (1 session; lane 3)
- Everything the levels section lists, the golden files accepted with what moved and why.

### CP16 · Seasons in the open world (1 session; lane 7)
- `SeasonPalette` in the open renderer's cover, season swaps of chunks in the baker and the reader, tables by season in the zones.

### CP17 · Carpathian architecture (1 to 2 sessions; H)
- The style guide's rows, then the seven styles in the building kit, and the castle on a crag over a carve.

### CP18 · Carpathia's file and the first town (1 session; H, U)
- The map's sketch, names, professor, rival and villain team agreed with the user; the region's file, the link from Paldea, `--region Carpathia`, the player's home town, the lab, the starters, the first route. The user judges the first town before the rest.
- Story ([the chapters](decisions/CP-story-chapters.md), section 7): the prologue P1 to P7 (the *șezătoare*, the painted chest, the pony, Doina's lab with the Book of Tales, Ilie's observatory), the crossing's version, the region's phone; Doina's and Ilie's voices designed.

### CP19 to CP24 · The provinces (6 sessions; H)
- CP19 the coast and the delta (the boat), CP20 the plateau's towns, CP21 the ridge and its road (the pony, the glide), CP22 the caves (the salt mine, the ice cave), CP23 the wooden north, CP24 the capital; each with its gyms, rooms, people, wild zones, tales to find and its part of the story.
- Story by session (the chapters' section 7): CP19 Sabina's false dawn (I-6); CP20 the market and the Champion (I-1), the glide (I-3), the endless noon (II-1); CP21 Făt-Frumos' tale (I-2), the rival (I-4), the crest shut by the storm; CP22 Zamfira at the salt mine (II-3); CP23 Teodor's blank wall (I-5); CP24 the second sun (II-4).

### CP25 · The villain team and the castle (1 to 2 sessions)
- The castle on the crag: the Ghost gym's halls and the Vârcolac's tale, Team Zenith's keep and the stronghold's assault with the Champion (II-2), the professors' two calls (I-7), the Stillers' trainers and the four executives' teams.

### CP25b · The ridge (1 to 2 sessions; added 2026-11-12)
- The climax (the chapters' III-0 to III-10): the rival at the foot, the two executives on the road, the Champion at the tunnel, the `???` fight, the professors' arrival, the binding and the transformation, the weakened fight and the catch, the roamers' leaving, the Solomonar's tale, the labs. Needs the wild battle terms, the runs' exceptions, the beast voice and text, and the roamers seen and fleeing on the open map (the chapters' engine needs 1, 2, 5, 6, 8 and 9). **Since 2026-11-12 the staged battles are built** (commit 406da9604; `docs/mechanics/rulings.md`, "Staged battles"; `docs/scripts.md`): `hidelevel`, `untouchable`, `passive` and `endafter N` on a `battle` or `wildbattle` line, and `canlose` on a `wildbattle`; a staged battle gives no EXP, money or find, refuses balls, tells the Pokédex nothing and buries no Nuzlocke dead, which is need 2's ??? fight. **Still owed**: a starting HP (`hp <percent>`, III-7's second fight), `passive` for a number of turns rather than the whole battle (the roamers' first meetings, CP27), a `hidelevel` level above 260 (CP15 raises the cap that bounds it), the forced catch's ways under runs that bar catching (open question 4), and no roamer has ever been on an open map.

### CP26 · The League and the crossing (1 session)
- The League in the capital's palace, the Champion, the Hall of Fame, the credits, Fly and harbours both ways with Paldea. The Champion is Ruxandra Zorescu (proposed), the Elite Four Dark, Fighting, Fairy and Ground (the chapters' L-1 to L-5).

### CP27 · After the Hall of Fame (1 to 2 sessions)
- The Dacian sanctuary and the draco, the festivals' days in full (the New Year's mask dances, Sânziene, Mărțișor), rematches, the Carpathian diploma.
- Story (A-1 to A-6): the empty labs and the professors' notes, Solzmeu by day and Lunacolac by night as roamers of the open map (a new kind of roamer), the creepy-voice moments V2 to V6, the rival's verse, the Stillers' remnants.

## Needs and gives

- **Needs** plan 32 whole, plans 33 and 34 (the engine proven, Paldea's harbour), plan 30 · FM3 to FM9, plan 39 (crafting, for the masks), plan 40 (festivals, GE4's diplomas), plan 31's sound, plan 26's voices, plan 18 · W4b's ladder.
- **Gives** the chain a tenth region, the game a nineteenth type, and plan 27 one more region to carry over as data.

## Status

- [x] CP1 Species made from nothing (2026-11-12; the table empty until CP3; see "As built", below)
- [x] CP2 The Sound type (2026-11-12; decisions 13 and 14 carried out as recommended; see "As built", below)
- [ ] CP3 The first proposals approved (starters and legendaries written 2026-11-09, waiting for the user)
- [x] The story's outline (2026-11-12: the user's beats, answers and names approved) and its chapters (2026-11-12, docs; ten open questions (two added 2026-11-12 with the new mythicals) waiting for the user)
- [ ] CP4 Sculpts I
- [ ] CP5 Sculpts II
- [ ] CP6 Sculpts III
- [ ] CP7 Sculpts IV
- [ ] CP8 Sculpts V and the regional forms
- [ ] CP9 Entries and names
- [ ] CP10 Signature moves and abilities
- [ ] CP11 The Tales: rules
- [ ] CP12 The Tales: forms, looks, songs and the Book
- [ ] CP13 The masks: rules and making
- [ ] CP14 The masks: on every body
- [x] CP15 The cap to 300 (2026-11-12; see "CP15 as built", above)
- [ ] CP16 Seasons in the open world
- [ ] CP17 Carpathian architecture
- [ ] CP18 Carpathia's file and the first town
- [ ] CP19 The coast and the delta
- [ ] CP20 The plateau's towns
- [ ] CP21 The ridge and its road
- [ ] CP22 The caves
- [ ] CP23 The wooden north
- [ ] CP24 The capital
- [ ] CP25 The villain team and the castle
- [ ] CP25b The ridge
- [ ] CP26 The League and the crossing
- [ ] CP27 After the Hall of Fame
