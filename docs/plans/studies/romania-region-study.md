# Study · A region of our own, after Romania

Written 2026-11-09, at the user's request, before any plan. **Answered the same day**: the user chose the Tales with the winter masks beside them, a Sound type, about 120 species, the cap raised, Carpathia as the name and the rest as [plan 46](../46-carpathia.md) records; that plan now carries the design. The user asked what can be done with a new region that comes **after every plan but plan 27 (Unity)**, has **entirely new Pokémon** and **an entirely new primary gimmick**, is **based on Romania**, and is built on **plan 32's open-world engine**. This is an analysis and a menu, not a plan: it says what the engine already gives, what the region would need that nothing has today, what fits Romania, and which questions the user decides before a plan (call it plan 46) is written.

## In one paragraph

Everything the region needs to *exist* is built or planned by the time it starts: the open-world engine (plan 32), grown characters and free movement (plan 30), the region chain and crossing (plan 18), species of ours (plan 03 · D14), the later-mechanic pattern (plan 06 · R20 to R23), Romanian as a full language (plan 17 · N6), voices (plan 26) and songs (plan 31). What is genuinely new is three things: **(1) a Pokédex of species with no forebear in any game** (D14's species all grow from an existing one, so the data, sculpts, entries and moves have no source but us), **(2) a fifth battle mechanic of our own design**, and **(3) a region whose map has no original at all**, so every place is designed from Romania itself rather than "inspired by the original game". It is the largest piece of original content the project has attempted: about **40 to 55 sittings**, roughly Paldea's size plus a Pokédex's worth of design.

## What the engine already gives (and will have, by then)

| Need | Where it comes from | State today |
|---|---|---|
| A continuous 3D world with caves under hills, rivers, roads, weather, day and night | plan 32 · OW0 to OW10 (`Data/world3d/<region>/`, the baker, `OpenRenderer`) | OW0 to OW9 built; OW10 and the user's gates (the floors on the Windows machine, decision 17 on steering) left |
| Rooms, gyms' puzzles, Pokémon Centers | grid rooms behind doors (plan 32 · decision 6), plan 30's free movement | built |
| Seen wild Pokémon with temperaments, by hour, zones and density | plan 32 · OW7 (`OpenWild`, `open-wild.json`) | built |
| Trainers' cones, people, followers on the mesh | plan 32 · OW6 | built |
| Grown characters, their clothes, animation | plan 30 · FM3 to FM6, FM10 | FM4 built, the rest in waves 94 to 97 |
| Movement powers (swim, climb, glide, jump) | plan 34 · PD2's mount rules on the capsule (`MovementGrants`) | designed, built with Paldea |
| Joining the chain, the crossing, Fly between regions, harbours | plan 18 · W4 to W10, `GameEngine.CrossTo` | built |
| Region files: starters, professor, rival, badges, levels, caps, Pokédexes | `Data/regions/<id>.json` | built |
| Species of ours: a table, numbers outside the National Pokédex, their own Pokédex, sculpts | plan 03 · D14, `Pokedex.Ours`, `PokemonModels.Ours.*` | built for 400 species past level 100 |
| Hand-written Pokédex entries with the overlap check | plan 08 · P13 (`dex-entries.json`, `--check-entries`) | built |
| A later mechanic as a once-a-battle choice | plan 06 · R20's pattern (a choice flag, a `WhyNot`, a key item, an event, the AI's use, `Ruleset`, `NoLaterMechanics`, the Vs. Recorder) | built four times (Mega, Z, Dynamax, Tera) plus Mega Dynamax |
| The gimmick on screen: cues, the stage's light, a song layer | plan 25 (`StageLight`, `BodyMotion`, moments), plan Y's giant layer in the battle songs | built |
| Seasons that change the land and the tables | plan 22 · U3, U4 (`SeasonPalette`, season swaps) | built for the grid; the open renderer has none yet |
| Romanian on screen | plan 17 · N6 | rolls with every wave |
| Voices with accents, songs, a Lyria credits song | plans 26, 31, the 2026-11-09 credits songs | built (voices wait for the user's ears) |
| Crafting, cooking, festivals, bonding, training | plans 35, 36, 39, 40 | in waves 91 to 110 |

**What the region does not get for free**:
- **The open renderer has no seasons.** Romania without four seasons would waste its best feature (below); the open world needs the grid's `SeasonPalette` and season swaps carried over (one session, rules and look).
- **The building kit has no Romanian architecture.** Gothic castles on crags, fortified Saxon churches, Maramureș wooden churches with needle spires, Bucovina's painted monasteries, Bucharest's belle-époque boulevards and its one enormous palace, Black Sea resort blocks, Danube Delta reed houses. Each is an `Architecture` and a style-guide row, and a castle on a crag wants the open world's carves under it.
- **Species with no forebear**: nothing in `SpeciesPastHundred` makes a species from nothing. A new table and importer step is needed (below).
- **The level ladder ends.** Paldea's band is projected at 244 to 256 under the modern cap of 260 (`Ruleset.HighestLevel`). A region after it has nowhere to climb (decision 3).

## The new Pokémon

### How many, and how numbered

Paldea brought about 120 new species, Galar about 90, Alola about 90. **Recommended: about 120**, of which 3 starter lines (9), roughly 10 legendaries and mythicals, about 15 regional forms of existing species (the cheapest kind, and it ties Romania to the rest of the game), and the rest new lines.

Numbers: species of ours start at `PokemonDatabase.OursFrom` (2000, D14's past-hundred species, up to about 3025), regional forms of ours use blocks of a thousand per region (4000 Alola to 7000 Paldea, up to about 8025), and moves of ours are 3001 to 3072. A new block **from 10001** keeps clear of all of them and reads cleanly ("Romanian No. 1 is 10001"). They would be counted in a **Romanian Pokédex** of the region's own (plan 18 · W3's ids) and, unlike D14's species, could also count toward a Pokédex goal of their own and a diploma (plan 40 · GE4's pattern) — but **never** toward the National 1,025 (`GetAll()` stays the games' alone; decision 6).

### What each species costs

| Part | Today's tool | New work |
|---|---|---|
| Base stats, types, abilities, catch rate, growth, egg groups, effort | `species.json` from `tools/DataImporter`; D14's hand-written table | a new hand-written table (`Overrides/species-romania.json`) and an importer step that makes whole species from it with no forebear |
| Learnsets, TMs, tutors | the importer; `Legality` | written by hand per species, held by a test to "every move exists and runs" |
| Evolutions | `EvolutionData` | ordinary rows; new conditions only for the gimmick (below) |
| Sculpt | `PokemonModels.*.cs`, the SDF kit; D14 sculpted 400 from the user's pictures | **the largest cost**: ~120 sculpts, best done as D14 was, from the user's (or Gemini-made) concept pictures, a batch of 20 to 30 a session |
| Eyes, decals, shiny palette, female looks | `PokemonDecals`, `ShinyPalettes` rule, `CreateFemale` | the palette is by rule; hand-picked shinies optional |
| Cry | `Cries`, from size, name and types | free |
| Field sprite, menu sprite, footprint, 3D in battle | baked from the model | free |
| Pokédex entries (two each) and habitat | `dex-entries.json`, `--check-entries` | written by hand, as P13 to P17; no games' text to collide with, so the check is a formality |
| Names | `LocalNames` | English **and Romanian names designed together** (the user is the best judge of the Romanian; decision 7) |
| Signature moves and abilities | `BattleCore.Effects.Later*.cs`, `AbilityEffectTable.Later.cs`; `MoveFx` recipes | each new move or ability is code, a recipe and a sound; budget about 20 moves and 15 abilities |
| Golden files, the oracle | `battles-modern.txt`, `OracleTests` | the corpus draws from `GetAll()`, so nothing moves unless a session chooses; the oracle can't check species Showdown doesn't know, so new moves get scenario tests instead |

### What Romania gives the Pokédex

A pool to design from, grouped so each group is a line or two (none of these are proposals yet; the D14 way is a proposals file the user approves batch by batch):

- **Folklore, the richest source**: the *zmeu* (a man-shaped dragon who steals the sun), the *balaur* (the many-headed dragon), the *solomonari* (weather-wizards who ride balauri through storms), the *strigoi* and *moroi* (the restless dead), the *vârcolac* (a wolf that eats the moon in an eclipse), the *pricolici*, *Muma Pădurii* (the old mother of the forest), the *Ielele* (dancing fairies of the night meadows), the *Sânziene* (midsummer fairies), *Baba Dochia* (the old woman of March, the cold's last days), *Făt-Frumos* and *Ileana Cosânzeana*, the *spiriduș* (a house sprite), the *vâlva băilor* (the spirit of the mines), the *uriași* (giants), the Dacian *draco* (the wolf-headed serpent banner).
- **Animals**: the Carpathian brown bear, the lynx, the wolf, the chamois, the European bison (*zimbru*), the Hucul pony, the Carpathian and Mioritic shepherd dogs, the Danube Delta's pelicans, black storks and herons, the beluga sturgeon, the golden eagle, the horned viper, the fire salamander, the stag beetle.
- **Places and things**: the salt mines (Turda's halls), the Scărișoara ice cave, the Berca mud volcanoes, the Bucegi stones (the Sphinx, Babele), the Iron Gates, Brâncuși's Endless Column, the painted eggs of Bucovina, the *ie* blouse's embroidered motifs, the Mărțișor's red-and-white cord, the Merry Cemetery's painted crosses, the *nai* and the *bucium*.
- **Food** (for plan 39's cooking and for a playful line or two): *sarmale*, *mămăligă*, *cozonac*, *papanași*, *mici*.

**Legendaries, a natural trio and a pair**: the **Zmeu** (sun, Fire/Dragon), the **Vârcolac** (moon and eclipse, Dark/…), and a third who keeps the balance, the **Solomonar**'s storm-dragon (Dragon/Electric or Flying), with the **Draco** banner-serpent as the region's box legendary or its mythical, and the **Ielele** or **Muma Pădurii** as mythicals.

**Starters**, three suggestions to start the conversation: a lynx kit (Grass, growing into a forest spirit after Muma Pădurii's tales), a fire-salamander (Fire, growing toward the zmeu), and a Danube pelican or sturgeon (Water).

**Regional forms**: Romanian forms of existing species are cheap (a form row, a sculpt from its species' builder) and very good for the region's identity: a Carpathian Ursaring line, a Delta Pelipper, a salt-mine Sableye, a Bucovina Smeargle that paints monasteries, a Mărțișor Snom, a Transylvanian Zubat/Noibat line, a shepherd's Lycanroc.

### Should there be a new type?

Possible, and the most expensive single choice in the region. `TypeChart` is code, `PokemonType` is read by every switch from Tera to Arceus's plates, the AI's 40ths chart, `Words`, the oracle, the golden battles and every type-coloured look. **Recommended: no new type**; Romania's folklore is well served by Ghost, Dark, Dragon, Fairy and the rest, and the new gimmick carries the novelty instead (decision 5).

## The new primary gimmick

Each later region's gimmick follows the same shape in this codebase (plan 06 · R20): a `Ruleset` flag, a key item, a flag on the move's choice, a `WhyNot…`, an event on its line, the AI's use, `RunTerms.NoLaterMechanics`, a field of the Vs. Recorder's file, its look (plan 25) and a layer of the battle songs (plan Y · YD5). A new one costs **about 4 to 6 sessions** done this way. Three candidates, each built from Romania:

### A · Basme, the tales (recommended)

A *basm* is a Romanian fairy tale. **Once a battle, a trainer tells a tale** they have learned: for the next five turns the tale's rule holds over the whole field for **both** sides (the zmeu's tale: the sun cannot set, fire burns hotter; Baba Dochia's: the cold returns, every switch-in sheds a layer; the Ielele's: the night's dance, sound moves strike twice; Miorița's: the shepherd's lament, a fainted Pokémon's ally takes its strength…), and **Pokémon bound to that tale awaken** into a *Tale Form* for as long as it is told. The other trainer may **answer with a tale of their own**, which ends the first and starts theirs (once each), so it is a duel of tales, not a button pressed first.

- **Why it fits Romania**: the region's identity is its folklore, and tales are how it is passed on (old people in villages, a *șezătoare* evening by the fire).
- **Why it fits the open world**: tales are **found, not bought**. Each is learned from an elder in a village, a painted wall, a carved gate in Maramureș, a page in a monastery, a ruin's inscription — a reason to explore every corner of the map, which no earlier gimmick gave. A tale learned also teaches the player something true about the place (an entry in a book of tales, like the Journal).
- **Why it is new**: every earlier gimmick changes one Pokémon. This one changes **the field and the team together**, and invites an answer. It sits beside the weathers and terrains (plan 06 · R19b's `FieldState`) rather than beside Mega Evolution, and the existing field look (`BattleFieldFx`, `StageLight`) gives it a stage to stand on.
- **How it would be built**: `Ruleset.Tales`; a key item (a *carte de basme*, a book of tales); `BattleChoice.Tale` with `WhyNotTale`; a `FieldCondition`-like `TaleState` in `FieldState` with its turns; `TaleTold` and `TaleAnswered` events; a tale's rules a table and its hooks where the original's would be (the same places weathers and terrains bite); Tale Forms as `FormKind.Tale` rows; the AI tells its best tale and answers one that hurts it; the battle's song gains a **tale layer** (as the giant layer: folk instruments over the theme); `StageLight` gives each tale its sky. About **6 sessions** with ~12 tales.

### B · Eclipse (the Vârcolac's bite)

The vârcolac eats the moon. Once a battle, a trainer **calls the eclipse**: the field goes dark, and each Pokémon of the region shows its *eclipsed* side (a second form, often another type), for the rest of the battle. Simple, striking on screen, and it ties to the region's strong day and night (strigoi at night, the Ielele's meadows). Closer to Mega Evolution in shape, so cheaper (**about 4 sessions**), but less new.

### C · Motive (the embroidered signs)

The *ie* blouse carries motifs (the tree of life, the ram's horn, the star, the wave) each with a meaning. The player **stitches** motifs, crafted from materials of the field (plan 39), onto a Pokémon's *altiță*; in battle, once, the stitched motif **wakes** and gives the Pokémon a second type and a boon by its motif. A builder's gimmick, tied to crafting, very Romanian, but it overlaps Terastallization (a type of choice) and leans on a plan the user may still drop.

### D · Hora, the round dance

The *hora* is the circle dance every village wedding ends in. Once a battle, a trainer **calls the hora**: for the next four turns the whole team is **one circle**. A switch costs no turn (the one going out hands over its hand), a Pokémon coming in keeps the stat changes of the one it took the hand from, and the benched Pokémon **clap the rhythm**: each one still standing on the bench adds a small boon to whoever is out. The answer is to break the circle (a fainted dancer ends it at once).

- **Why Romania**: the hora is the country's emblem of togetherness, and its music (7/16, the *geamparale*) gives the battle songs a layer of their own.
- **Why new**: every other gimmick is about the Pokémon on the field; this one makes **the bench** matter and turns the team into the unit.
- **Cost**: about 4 sessions. The rules touch switching (`BattleCore.Switch.cs`) and the turn's order, which plan 14's formats and the Vs. Recorder must keep; the look is the bench's balls in a circle under the platform.

### E · Mărțișor, the red-and-white cord

On 1 March Romanians give a *mărțișor*, a charm on a twisted red-and-white cord. Once a battle, a trainer **ties the cord between two of their Pokémon** (red to one, white to the other): from then on **they share what happens to them**. Damage one takes is split between the two, a stat change on one is half on the other, and when one faints the other **takes its strength** (its highest stat added in part) for the rest of the battle. In a double battle both stand on the field; in a single the white one waits on the bench and still shares.

- **Why Romania**: the cord is the most loved Romanian custom, and its meaning (two strands, winter and spring, together) is the rule.
- **Why new**: a bond between two Pokémon of one team; nothing in the series links two of a team's Pokémon in battle.
- **Cost**: about 4 to 5 sessions. Every place HP is lost must know about the pair, so it is a seam in `LoseHp` and `Faint`, held by the golden battles as R20's choice flags were. A natural tie to plan 35's bonding.

### F · Solomonar, riding the storm

The *solomonari* are wizards who ride a *balaur* through the clouds and command hail and rain. Once a battle, a trainer **calls a storm and a Pokémon rides it**: the weather becomes the trainer's (it can't be replaced by the foe's for five turns), and the rider becomes a **Storm Rider**, airborne (ground moves miss it, its moves take on the weather's type: rain gives Water, hail Ice, sand Rock, sun Fire, wind Flying) and grows stronger each turn the storm holds.

- **Why Romania**: a folk legend almost nobody outside Romania knows, and it gives the region's weather-dragon legendary its reason to exist.
- **Why new**: weather today is a side effect of an ability or a move; here it is **owned** and ridden.
- **Cost**: about 4 sessions, small, since weathers, their looks (plan 25 · Y1) and "out of the ground's reach" (Magnet Rise, flight) already exist.

### G · Strigoi, the restless

A *strigoi* is a dead soul that will not stay in its grave. Once a battle, when one of the trainer's Pokémon faints, the trainer may **let it rise**: it comes back as a strigoi for **three turns**, at a third of its HP, part Ghost, unable to be healed, its moves draining the foe, and then it falls for good (and can't be revived this battle).

- **Why Romania**: the strigoi is the region's most famous creature, and the castles, night and fog of the map are built for it.
- **Why new**: the only gimmick that acts **after** a faint, so it changes how a losing battle is played.
- **Cost**: about 4 sessions. **Its risk**: it touches what a faint means (the Nuzlocke's burial, `EndBattle`, the Frontier's rules, a whiteout); each needs a ruling. Darker in tone than the rest, which may be just what the region wants or not.

### H · Doina, the sung lament

The *doină* is a free, improvised song of longing. Once a battle, a trainer **sings a doina** with their Pokémon: a short line of timed presses appears over the move menu (the rhythm of the battle song), and the better the presses, the stronger the move that follows and the longer its after-effect (a doina's echo: three turns of the move's secondary effect repeated at the end of each turn).

- **Why Romania**: its music is the soul of the region, and the synthesiser plays it.
- **Why new**: the only gimmick that is **played with the hands**, not chosen from a list.
- **Cost**: about 4 sessions. Uses `Core/TimingAssist.cs` (the timing helper for people who need it), and the AI sings with a set skill per trainer. **Its risk**: a battle stops being a pure function of its choices; the Vs. Recorder must store the presses' result, which it can.

### I · Măști, the winter masks

At New Year, Romanian villages fill with masked dancers: the **bear**, the **goat** (*capra*), the **horse**, the **stag**, the old men. Once a battle, a trainer **puts a mask on a Pokémon**: each mask is a role (the bear: bulk and taking hits for the team; the goat: speed and mischief, swapping the foe's boosts; the horse: carrying an ally to switch for free; the stag: the leader, raising the team), changing its ability and its typing to the mask's for the rest of the battle. Masks are **made** by the player (plan 39's crafting) and **found** at the winter festivals.

- **Why Romania**: the masked dances are some of the oldest living customs in Europe, and they look wonderful.
- **Why new**: roles, not power; close in shape to Ogerpon's masks, but for any Pokémon and chosen per battle.
- **Cost**: about 5 sessions; each mask a sculpted accessory on every body plan (`PokemonModels`), the costliest part.

### J · Anotimp, the turning season

Romania's four seasons are sharp: deep snow, wet spring, hot summer, golden autumn. Once a battle, a trainer **turns the season**: the battlefield becomes spring, summer, autumn or winter for five turns (a field state like a terrain: spring heals a little, summer strengthens Fire and Grass, autumn makes leaves fall that hide the field's hazards, winter slows), and **every Pokémon of the region shows its seasonal form** (as Deerling and Sawsbuck do today in Unova), changing type and stats with it.

- **Why Romania**: the seasons are the land; the region's map changes with them (once the open renderer has seasons, above) and so would its battles.
- **Why new**: ties the battle to the calendar and the map in one mechanic, and gives every species of the region four looks without four species.
- **Cost**: about 5 sessions, plus the sculpting of seasonal forms (four looks a species is too many for 120; one or two "turning" looks a species is reasonable).

### Side by side

| | Acts on | New because | Sessions | Biggest risk |
|---|---|---|---|---|
| A Tales | field + team, both sides | a duel of tales, found by exploring | 6 | tales need a lot of design and balance |
| B Eclipse | each region Pokémon | darkness for the whole field | 4 | too close to Mega Evolution |
| C Motifs | one Pokémon, chosen type | crafted, a builder's gimmick | 4 to 5 | overlaps Tera; leans on plan 39 |
| D Hora | the whole team, the bench | the bench matters | 4 | switching rules are delicate |
| E Mărțișor | two of a team, linked | a bond inside the team | 4 to 5 | every HP change knows the pair |
| F Solomonar | weather + one rider | weather owned and ridden | 4 | small; may feel like an add-on |
| G Strigoi | a fainted Pokémon | acts after a faint | 4 | Nuzlocke, the Frontier, tone |
| H Doina | one move | played with the hands | 4 | not a pure function of choices |
| I Masks | one Pokémon's role | roles for any Pokémon | 5 | an accessory sculpt for every body |
| J Seasons | field + region Pokémon | battle tied to the calendar and map | 5 + forms | four looks a species |

Several pair well as a primary and a secondary, as Paldea pairs Tera with picnics: **A with J** (a tale for each season), **A with E** (the cord given on Mărțișor's day is a tale's gift), **A with I** (masks found at the winter festivals, each with its tale), or **F** as the weather-dragon legendary's own signature rather than a region-wide mechanic, as B could be the Vârcolac's.

**Recommended: A, the tales**, with C's motifs as a smaller **secondary** gimmick if the user wants crafting in the region (Paldea pairs Tera with picnics the same way). B could become the legendary Vârcolac's own signature (its eclipse its tale) rather than a region-wide mechanic.

## The region: Romania as a map

Romania's geography is almost a ready-made region shape: **a ring of mountains (the Carpathians) round a high plateau (Transylvania)**, plains outside it (Wallachia, Moldavia), the **Danube** along the south ending in a **delta** at the **Black Sea**. The open world suits it better than any earlier region: real roads that climb mountains, caves under them, a river to follow from gorge to delta.

Built as plan 32 says (hand-written terrain, carves, water, roads, places), at **a reduced scale of our own** (Paldea's plan uses a third of the original's distances, about 3 km by 3 km; Romania could be about 3.5 km by 2.5 km). Names invented with Romanian roots, as Pokémon regions do (Lumiose from *lumière*), never the real towns' names on the map (decision 4): the region itself perhaps **Dacira**, **Carpathia** or **Valahra**.

### Places worth building (each a landmark of the open world)

| Real place | What it becomes | What it shows off |
|---|---|---|
| The Transfăgărășan and the Transalpina | a road of hairpins up the ridge, a high pass with a glacial lake and a tunnel through the top | `roads.json` splines, carves, the orbit camera, snow on the heights |
| The Carpathian ridge, the Bucegi plateau, the Sphinx and Babele | the region's spine; stone faces on a plateau; a legendary's place | the land at its largest; impostors to the horizon |
| Bran, Corvin and Peleș castles | the Ghost or Dark gym in a castle on a crag; the villain's stronghold; the Champion's palace | a carve under a building; a new `Architecture` |
| Sibiu, Brașov, Sighișoara, Transylvania's fortified churches | old walled towns of the plateau, painted houses with roofs that have "eyes" (the dormers) | towns in the open world, a style of their own |
| Salina Turda and Praid | a salt mine as a vast cavern with a lake, a wheel and a boat on it | the engine's caves at full size |
| The Scărișoara ice cave, the Apuseni karst, Bigăr's waterfall | ice cave, sinkholes, a falls over moss | ice under the land; water |
| The Iron Gates of the Danube | the river in a gorge, a face carved in the cliff, a dam | water volumes, a boat or swim grant |
| The Danube Delta | channels in reeds, pelicans, floating islands, fishermen's villages | water as the land; a boat-ride travel mode |
| The Black Sea coast | resort beaches, a port city with an old casino on the shore | the region's harbour and its link to the chain |
| Bucharest | the capital: boulevards, an arch, parks with lakes, and one palace too big for its square | the region's largest city; the League |
| Maramureș and Bucovina | wooden churches with needle spires, carved gates, painted monasteries, the Merry Cemetery | tales found (gimmick A); a gentle, folk north |
| The Berca mud volcanoes | a grey moonscape that bubbles | a Ground/Poison area |
| Sarmizegetusa Regia | the Dacian sanctuary of stone circles in the forest | the draco's mythical; the post-game |

### Gyms, story and rhythm

- **Eight gyms** across the ring, in any order (Paldea's freedom) or in an order (classic): e.g. Water on the coast, Bug or Flying in the delta, Rock on the pass, Ice in the ice cave, Grass in Maramureș, Ghost in the castle, Psychic among the Bucegi stones, Steel or Electric in the capital. Gym tests as Paldea's: a puzzle in a grid room or a task in the open world (a *căluș* dance, a shepherd's round-up with the shepherd dogs, a sledge run, a painted-egg hunt).
- **A villain team of our own**: the folklore suggests one that wants **the sun to stand still** (with the zmeu) or **an endless night** (with the vârcolac), and the tales are how the player undoes it. Stay with folklore and avoid the country's real politics (Ceaușescu, 1989, Roșia Montană's mine): keep the region a place of tales.
- **The calendar**: Romania's year is marked by days the game can keep — **Mărțișor** (1 March, a red-and-white charm given; Baba Dochia's days 1 to 9 March), **Dragobete** (24 February), **Sânziene** (24 June, the fairies' night), Easter's painted eggs, Christmas's carollers (*colindători*) and the New Year's bear and goat dancers. Plan 40's festivals and the region's seasons carry them; each day can teach a tale.
- **Traversal**: the region's own way about. Suggestions: on horseback (a Hucul pony Pokémon) for the plains and passes, a boat for the delta, and a glide from the heights; reuse plan 34's `MovementGrants` and its edge flags.

## Sound, voice and words

- **Music**: Romanian folk is a gift to the synthesiser: the *nai* (pan flute), *cobză*, *țambal* (cimbalom), *bucium* (alphorn), *fluier*, *taragot*, the free *doină* for night and sad scenes, the *hora* for towns, the uneven 7/16 *geamparale* for battles. The synthesiser needs a few new instruments (plan 31's fuller sound by then). The battle themes carry the tale layer.
- **The credits song** via Lyria in the region's own style, as every region has had since 2026-11-09.
- **Voices**: Kokoro designs with a Romanian accent for the region's cast.
- **Words**: written in English as the source, with **the Romanian written by the user or reviewed by them** first in this one region, since it is theirs; species and place names designed in both languages together.

## The levels after Paldea (needs a decision)

The chain's levels climb region by region (plan 18 · W4b): Paldea's band is projected at 244 to 256, and the modern cap is 260. Three ways for a region after it:

1. **Raise the cap** (to about 300) and climb on. Every rule that asks the cap already asks `Ruleset.LevelCap`, but the growth curves past 260, R31's moves past 100, the golden level files and the D14 climbs would all need extending.
2. **A region that takes the player's level** (plan 41 · EG1's `RulesHere` idea): Romania's world files keep a 1 to 100 climb, and for a player crossing in they are mapped to **their furthest region's band** at the time. Nothing past 260 is needed.
3. **Romania as a second start**: a new game may begin there (`--region`, as Kanto, Johto, Hoenn and Unova can) with its own climb to 100, and a Champion crossing in from Paldea meets it mapped onto the top of the ladder.

**Recommended: 2 and 3 together**, which is what plan 18 · W4's mapping already does for a region the game didn't begin in: no new cap, and the region stands on its own as a new game for someone who wants only it.

## Size, order and gates

| Part | Sittings |
|---|---|
| The region's design document: map, towns, gyms, story beats, names (with the user) | 1 to 2 |
| The species: table and importer step for species with no forebear, the Pokédex and its tests | 1 |
| The species: proposals in batches the user approves (as D14) | 2 to 3 |
| The species: sculpts, ~120 in batches of 25, from pictures | 5 to 6 |
| Their entries, signature moves (~20), abilities (~15) and move looks | 4 to 5 |
| The gimmick (tales): rules, AI, forms, looks, song layer, the book of tales | 5 to 6 |
| Seasons in the open world | 1 |
| Romanian architecture and the castle on a crag | 2 |
| The region: file, start, first town, the user's look | 1 |
| The region: provinces, gyms, caves, the delta, the capital, the League | 8 to 10 |
| The story: villain, legendaries, the Hall of Fame, the crossing | 3 to 4 |
| After the Hall of Fame: the mythicals, Sarmizegetusa, festivals, rematches | 2 to 3 |
| Music, voices, words | rolls beside, about 3 |
| **Total** | **about 40 to 55** |

**Order**: after plans 33 and 34 (so the open world, the mount and the raids are proven twice) and every other plan but 27; before 27, which then carries Romania to Unity as data like every other region. The species' design and sculpting (no engine needed) could start **earlier, beside** the waves, as D14's batches did, so that when the region's sessions begin its Pokédex is already standing.

**Gates the user owns**: plan 32's floors and decision 17 (steering by the camera); approval of each batch of species; the look of the first town; the gimmick's choice; the map's first sketch; ears on the songs.

## Decisions for the user

1. **The gimmick** (the user liked A, the tales, on 2026-11-09): A alone, or A as the primary with a secondary from B to J (recommended: J, the seasons, or E, the cord), or another of B to J as the primary.
2. **How many new species**: about 120 (recommended), fewer (about 80: a smaller region), or more (about 150, Paldea plus its DLC's size).
3. **Levels**: the region maps to the player's furthest band and is also a new-game start (recommended), or the cap is raised past 260.
4. **Names**: invented names with Romanian roots for the region, towns and people (recommended), or the real names.
5. **A new type**: none (recommended), or one (a 19th type, the largest single cost of the region).
6. **Counting the species**: a Romanian Pokédex and diploma of its own, outside the National 1,025 (recommended), or folded into the Pokédex of our own.
7. **Romanian text**: the user writes or reviews the Romanian names and lines of this region first (recommended), or the usual N6 pass.
8. **Order or freedom**: gyms in any order like Paldea (recommended for an open world), or in a fixed order.
9. **When the species start**: early and beside the waves, as D14 did (recommended), or only once the region's plan starts.

Once these are answered, the next step is plan 46, written in the shape of plans 33 and 34 (goal, where we are, design, decisions, sessions with what each delivers), and a first proposals file for the starters and legendaries in the D14 way.
