# R20b · Megas of our own: the proposal

Plan 06 · R20b, the design part. Drafted 2026-10-18, **awaiting the user's approval**. Nothing here is built: no data, no item, no sculpt. Every row below waits for the user's yes, or for the user's change, before a later session writes it into the data.

The list is the user's (final since 2026-10-17): the fifteen final forms of every region's starters whose line has no Mega yet. The other twelve already have one in the data: Venusaur, Charizard (X and Y), Blastoise, Sceptile, Blaziken and Swampert from X, Y and Omega Ruby and Alpha Sapphire; Meganium, Feraligatr, Emboar, Chesnaught, Delphox and Greninja from Legends: Z-A.

## How to read this

- **Name**: "Mega <Name>", as the games say it. The form is named the way Showdown names a Mega, `<Name>-Mega`, so `MegaStone.Form` and `PokemonModels.Forms` find it. Nobody gets an X and a Y.
- **Stats**: HP / Attack / Defense / Sp. Atk / Sp. Def / Speed. The base stats are the data's own (`species.json`, which are the newest games' too: none of the fifteen has a `modern` stat change). Every Mega's total is its species' plus 100. HP never changes, as no official Mega's does. The 100 goes into three to five stats, the way the games share it out (Mega Charizard X: three stats; Mega Venusaur: four; Mega Blaziken: five). Where the role is clear, most of it goes into the stats the species already lives by. Every sum below was worked out by arithmetic and checked by a script against the data.
- **Ability**: an ability that is already in `abilities.json`. Nothing new is invented. The games' Mega abilities follow three patterns, and each row names its own:
  - (H) the species' hidden ability made certain: Mega Blaziken's Speed Boost, Mega Chesnaught's Bulletproof, Mega Greninja's Protean.
  - (N) a new ability that fits the new shape: Mega Charizard X's Tough Claws, Mega Venusaur's Thick Fat, Mega Sceptile's Lightning Rod.
  - (W) a weather setter: Mega Charizard Y's Drought.
  - **Runs today?** says whether the engine runs the ability now. An ability of Generation 5 or later has its name only until R27 (`docs/mechanics/rulings.md`, "Mega Evolution and Primal Reversion": "have them in name only until R27"), which is true today of every later Mega's ability.
- **Typing**: a starter Mega that has one type often gains a second (Mega Sceptile and Mega Feraligatr gain Dragon, Mega Meganium Fairy; only Mega Blastoise stays a single type). A starter Mega that already has two keeps them (Mega Venusaur, Mega Blaziken, Mega Swampert, Mega Chesnaught, Mega Delphox, Mega Greninja; Charizard X's Dragon is the one exception). The proposal does the same. Of the six single-typed species in the list, three gain a type. The other three would only copy a typing a starter Mega already has. Each row gives the alternative of keeping the species' own typing.
- **Look**: a short brief for the sculpt in the style guide's terms ("Pokémon (version 2, G7)"; a Mega is "its species grown into its Mega Evolution's shape"). It names the silhouette's change, the colours and one new feature. It starts from our own sculpt of the species (the summary over each build in `PokemonModels.*.cs`), and the Mega is built from that sculpt.
- **Stone**: `<Name>ite` or a shortened form, as the games shorten some (Blastoisinite, Legends: Z-A's Feraligite). None of the fifteen names is in `items.json` (checked), and none is longer than 13 letters (Chesnaughtite, already in the bag, is 13).

## The fifteen

### 1. Mega Typhlosion: `Typhlosion-Mega`, Typhlosionite

- **Typing**: Fire → **Fire / Ground**. Its category is the Volcano Pokémon, and a Mega is where it becomes one. Ground gives it Earth Power to go with its Fire moves, Electric can't touch it, and Rock is no longer a weakness. *Alternative*: keep Fire alone, as Mega Blastoise keeps Water.
- **Ability**: **Drought** (W; runs today). A volcano clears the sky. The sun also halves the Water moves that hit the new typing four times over. This is Mega Charizard Y's pattern, and it gives the Mega a reason to exist beside its Hisuian form. *Alternative*: Flash Fire made certain (H), which is much weaker.
- **Stats**: 78 / 84 / 78 / 109 / 85 / 100 = 534 → **78 / 84 / 98 / 149 / 105 / 120 = 634** (+0 / +0 / +20 / +40 / +20 / +20). It stays a fast special attacker that has to last the turns its Eruption needs.
- **Look**: the cream beast hunches lower and broader. Its collar of flame climbs into a ring of dark rock round its shoulders, like a crater's rim (blocks with little rounding, charcoal), with fire welling inside it (`Glow`, pale at the heart). The dark blue of its back turns to ash grey, with lava painted in thin cracks running down to its tail. Its forearms carry bracers of the same rock. **New feature**: the crater on its shoulders, with three tongues of flame standing out of it.

### 2. Mega Torterra: `Torterra-Mega`, Torterrite

- **Typing**: Grass / Ground, kept. The Continent Pokémon already is what its Mega would be.
- **Ability**: **Solid Rock** (N; runs today). The shell turns to bedrock. A super-effective hit does a quarter less, which softens its four-times Ice weakness and its Fire, Flying and Bug weaknesses. *Alternative*: Thick Fat (Mega Venusaur's: Ice and Fire alone, halved).
- **Stats**: 95 / 109 / 105 / 75 / 85 / 56 = 525 → **95 / 129 / 145 / 75 / 115 / 66 = 625** (+0 / +20 / +40 / +0 / +30 / +10). A physical wall that hits back, in Mega Venusaur's and Mega Chesnaught's mould.
- **Look**: the shell's spikes rise into a ridge of three peaks, their tips painted snow white, with moss (`Leaf`, darker green) along the shell's rim. The one tree grows into a small grove: the old tree in the middle, two saplings on the slopes. The tusks are longer, the face plates heavier. **New feature**: the snowcapped ridge, so the Mega reads as a mountain range on four legs.

### 3. Mega Infernape: `Infernape-Mega`, Infernapite

- **Typing**: Fire / Fighting, kept.
- **Ability**: **Iron Fist** (H; runs today). Its hidden ability made certain, as Mega Blaziken's is. Mach Punch, Drain Punch, Fire Punch, Thunder Punch and Ice Punch all gain from it. *Alternative*: Tough Claws (N), which is Mega Charizard X's.
- **Stats**: 76 / 104 / 71 / 104 / 71 / 108 = 534 → **76 / 129 / 81 / 129 / 81 / 138 = 634** (+0 / +25 / +10 / +25 / +10 / +30). It keeps its mixed attack, even on both sides, and becomes faster still, which is the monkey's whole character.
- **Look**: taller on its legs, its arms longer. The crown of flame on its head becomes a mane of flame streaming down its back to the tail (a row of tongues, `Glow`). The gold markings on its shoulders and knees grow into plates like armour (`Metal`, gold). **New feature**: a ring of flame round each wrist, like a boxer's wraps set alight.

### 4. Mega Empoleon: `Empoleon-Mega`, Empoleonite

- **Typing**: Water / Steel, kept.
- **Ability**: **Competitive** (H; R27). It is the Emperor Pokémon, and a lowered stat raises its Special Attack by two stages. That punishes Intimidate and the moves that lower stats, which are aimed at it often. *Alternative*: Filter (N).
- **Stats**: 84 / 86 / 88 / 111 / 101 / 60 = 530 → **84 / 86 / 113 / 146 / 126 / 75 = 630** (+0 / +0 / +25 / +35 / +25 / +15). A special tank that hits hard; it stays slow, as an emperor may.
- **Look**: its trident crest rises into a tall crown of three prongs, edged gold. The blades of its flippers grow longer and turn steel (`Metal`). A cape of navy plumage falls from its shoulders to the ground, its edge feathered like the spikes of its coat. **New feature**: gold epaulettes on its shoulders, where the cape hangs from.

### 5. Mega Serperior: `Serperior-Mega`, Serperiorite

- **Typing**: Grass → **Grass / Dragon**. The Regal Pokémon is a great serpent, and dragons in its look are a short step away. It gains Dragon moves and resists Water, Electric, Grass and Ground twice. *Cost*: Ice becomes a four-times weakness. *Alternative*: keep Grass alone. Grass / Dragon is also Mega Sceptile's typing.
- **Ability**: **Contrary** (H; R27). Its hidden ability made certain. A Leaf Storm raises its Special Attack instead of lowering it, which is what the line is known for.
- **Stats**: 75 / 75 / 95 / 75 / 95 / 113 = 528 → **75 / 75 / 110 / 115 / 110 / 143 = 628** (+0 / +0 / +15 / +40 / +15 / +30). Speed and Special Attack first, as the Leaf Storm sweeper it is.
- **Look**: it rises higher from its coils. Its yellow leaf collar spreads into a hood of leaves round its head, like a cobra's, the blades flat to the front. Two curved horns of leaf sweep back from its crown, and a pair of leaf-wings stands from the coils behind the neck, flat to the side they face. **New feature**: the leafy cobra's hood, which frames the face.

### 6. Mega Samurott: `Samurott-Mega`, Samurottite

- **Typing**: Water → **Water / Steel**. The shells on its forelegs become armour, and the seamitars become blades of steel. Steel gives it Iron Head and Smart Strike, and resistances to every type but four. *Duplicate*: Mega Empoleon's typing too. *Alternatives*: Water / Fighting (Quaquaval's) or keep Water alone.
- **Ability**: **Sharpness** (N; R27). Slicing moves are 1.5 times as strong: Razor Shell, Aqua Cutter, Sacred Sword, Night Slash. This is the Hisuian Samurott's hidden ability, given to the regional one with the blades it draws.
- **Stats**: 95 / 100 / 85 / 108 / 70 / 70 = 528 → **95 / 140 / 105 / 108 / 85 / 95 = 628** (+0 / +40 / +20 / +0 / +15 / +25). The Mega makes up its mind to be a physical swordsman.
- **Look**: the helmet grows into a full kabuto with a tall crescent crest, gold on the steel (`Metal`). Plates of shell armour lie over its shoulders and flanks, edged cream. Its moustache is longer. **New feature**: both seamitars half drawn from their sheaths on the forelegs, longer than the leg.

### 7. Mega Decidueye: `Decidueye-Mega`, Decidueyite

- **Typing**: Grass / Ghost, kept.
- **Ability**: **Infiltrator** (N; R27). A ghost's arrow passes a Substitute, a Reflect, a Light Screen and Safeguard, as the Arrow Quill Pokémon's shot passes everything. It goes with Spirit Shackle, which already reaches past the target's guard. *Alternative*: Long Reach made certain (H), which does little.
- **Stats**: 78 / 107 / 75 / 100 / 100 / 70 = 530 → **78 / 142 / 90 / 100 / 110 / 110 = 630** (+0 / +35 / +15 / +0 / +10 / +40). A physical archer, at last fast enough to shoot first.
- **Look**: its hood deepens into a cowl that shades its face to the eyes. Its wing-cloak lengthens into a cape whose edge is a fan of quills, like arrows in a quiver. A ghostly green glow hangs at the quills' tips (`Glow`, a soft teal). **New feature**: a bow of vine held across its chest, the string a thin tube from wing tip to wing tip.

### 8. Mega Incineroar: `Incineroar-Mega`, Incineroarite

- **Typing**: Fire / Dark, kept.
- **Ability**: **Intimidate** (H; runs today). The heel's glare made certain. Like Mega Manectric's, it acts when the Mega Evolution happens, as on entry (R20's rule).
- **Stats**: 95 / 115 / 90 / 80 / 90 / 60 = 530 → **95 / 145 / 110 / 80 / 110 / 90 = 630** (+0 / +30 / +20 / +0 / +20 / +30). A tougher wrestler with real speed.
- **Look**: broader in the chest and shoulders. Its belt of fire becomes a champion's belt: a wide band with a great gold plate in front (`Metal`), with flames rising from the band behind. The red stripes on its arms broaden into bands, and the black mask spreads to the brow. **New feature**: the gold belt plate, with a paw print in it.

### 9. Mega Primarina: `Primarina-Mega`, Primarinite

- **Typing**: Water / Fairy, kept.
- **Ability**: **Liquid Voice** (H; R27). Its hidden ability made certain: its songs are Water moves (Hyper Voice with the same-type bonus), which is the Soloist Pokémon's whole design.
- **Stats**: 80 / 74 / 74 / 126 / 116 / 60 = 530 → **80 / 74 / 94 / 156 / 136 / 90 = 630** (+0 / +0 / +20 / +30 / +20 / +30). A special attacker that also walls special attacks, with enough speed to sing first.
- **Look**: her hair grows into a long train that falls to the ground behind her like a gown, strung with more pearls. A tiara of pink coral sits where the star was. Her tail fin spreads into a wide fan, laced blue. **New feature**: a ring of floating spheres of water round her, held to her on thin strands as a hand that floats free is (style guide), each lit pale like a stage's light.

### 10. Mega Rillaboom: `Rillaboom-Mega`, Rillaboomite

- **Typing**: Grass, kept. A gorilla drummer would go to Fighting, but that is Mega Chesnaught's typing, and Ground is Mega Torterra's. *Alternative*: Grass / Fighting.
- **Ability**: **Grassy Surge** (H; R27). Its hidden ability made certain: the drum's beat raises Grassy Terrain, which makes Grassy Glide strike first.
- **Stats**: 100 / 125 / 90 / 60 / 70 / 85 = 530 → **100 / 160 / 110 / 60 / 90 / 110 = 630** (+0 / +35 / +20 / +0 / +20 / +25). The heaviest hitter of the fifteen. See "Balance".
- **Look**: bigger and more upright. Its mane of leaves spreads over its shoulders like pads, and the stump drum it beats is strapped to its back on a harness of vines, like a taiko's frame. The two drumsticks become thick branches with leaves at their ends. **New feature**: the drum on its back, its face a disc laid on a flattened ellipsoid as the Gigantamax drum's is.

### 11. Mega Cinderace: `Cinderace-Mega`, Cinderacite

- **Typing**: Fire, kept. Fighting for the kicks is already Infernape's, Blaziken's and Emboar's. *Alternative*: Fire / Fighting.
- **Ability**: **Libero** (H; R27). Its hidden ability made certain, as Mega Greninja keeps Protean. In the newest rules it changes type once per entry. *Alternative*: Reckless (N), for Flare Blitz and High Jump Kick.
- **Stats**: 80 / 116 / 75 / 65 / 75 / 119 = 530 → **80 / 151 / 95 / 65 / 95 / 144 = 630** (+0 / +35 / +20 / +0 / +20 / +25). The striker, faster and harder.
- **Look**: its long ears lengthen and sweep back into a crest of flame (`Glow` at their tips). The white ruff becomes a scarf trailing behind it, and a captain's band rings its arm. **New feature**: a ball of fire held under one foot, ready to kick (a thing carried, on the bone of the foot that holds it).

### 12. Mega Inteleon: `Inteleon-Mega`, Inteleonite

- **Typing**: Water, kept. Water / Dark would be Mega Greninja's. *Alternative*: Water / Ice, for a cold-blooded agent.
- **Ability**: **Sniper** (H; runs today). Its hidden ability made certain. Snipe Shot and its high critical-hit ratio hit three times as hard on a critical hit.
- **Stats**: 70 / 85 / 65 / 125 / 65 / 120 = 530 → **70 / 95 / 80 / 160 / 80 / 145 = 630** (+0 / +10 / +15 / +35 / +15 / +25). The sniper sharpened: Special Attack and Speed first.
- **Look**: the yellow crest sweeps back longer, like a hood. Its tail lengthens into the tails of a coat that falls to its knees (flat fins, dark blue). The membrane over one eye becomes a scope: a ring with a pale lens. **New feature**: the scope over one eye, its lens lit pale.

### 13. Mega Meowscarada: `Meowscarada-Mega`, Meowscaradite

- **Typing**: Grass / Dark, kept.
- **Ability**: **Magician** (N; R27). Its category is the Magician Pokémon. A damaging move steals the target's item, as a stage magician makes things vanish. Magician is also Delphox's hidden ability, but Mega Delphox's is Levitate, so no starter Mega has it.
- **Stats**: 76 / 110 / 70 / 81 / 70 / 123 = 530 → **76 / 150 / 85 / 81 / 90 / 148 = 630** (+0 / +40 / +15 / +0 / +20 / +25). A fast physical attacker, Flower Trick first.
- **Look**: its cape of leaves grows into a great cape that falls to the ground, lined pink. The ruff of petals at its throat opens into a full bloom pinned there. Its mask is drawn sharper. **New feature**: a flower bomb spinning on one fingertip, its petals flat round a disc (a thing carried, on the bone of the hand).

### 14. Mega Skeledirge: `Skeledirge-Mega`, Skeledirgite

- **Typing**: Fire / Ghost, kept.
- **Ability**: **Punk Rock** (N; R27). It is the Singer Pokémon, and its Torch Song is a sound move. Punk Rock makes its sound moves 1.3 times as strong and halves those that hit it. *Alternative*: Unaware made certain (H).
- **Stats**: 104 / 75 / 100 / 110 / 75 / 66 = 530 → **104 / 75 / 115 / 145 / 105 / 86 = 630** (+0 / +0 / +15 / +35 / +30 / +20). A bulky special singer.
- **Look**: the little bird of flame on its head (its Loud Bonnet) grows into a great crown of fire with a wide brim. The painted skull spreads from its head down its back as bones painted on the red. The wisp at its tail's end becomes a flame shaped like the head of a guitar. **New feature**: the crown of fire, with the bird's face in the middle of its front.

### 15. Mega Quaquaval: `Quaquaval-Mega`, Quaquavalite

- **Typing**: Water / Fighting, kept.
- **Ability**: **Dancer** (N; R27). It is the Dancer Pokémon, and Aqua Step is a dance: whenever any Pokémon dances (Aqua Step, Swords Dance, Dragon Dance, Quiver Dance, Teeter Dance, Feather Dance, Petal Dance, Fiery Dance, Revelation Dance, Lunar Dance, Victory Dance), it dances the same move after it. *Alternative*: Moxie made certain (H), which is the stronger choice in battle.
- **Stats**: 85 / 120 / 80 / 85 / 75 / 85 = 530 → **85 / 155 / 100 / 85 / 90 / 115 = 630** (+0 / +35 / +20 / +0 / +15 / +30). Aqua Step's sweeper, with its speed gained.
- **Look**: its crest of plumes fans into a crescent headdress, blue with an orange flash at each plume's end. The boa of water becomes two long streamers trailing from its wrists, each a string of beads close enough to touch, as the boa is (style guide). A skirt of blue feathers fans round its hips. **New feature**: the crescent headdress.

## The table to answer

One row a species. The user answers each row with yes, a change, or no.

| # | Mega | Typing (was → proposed) | Ability (pattern; runs) | Stats HP/Atk/Def/SpA/SpD/Spe (total) | Δ | Stone |
|---|---|---|---|---|---|---|
| 1 | Mega Typhlosion | Fire → **Fire/Ground** | Drought (W; now) | 78/84/98/149/105/120 (634) | +20 Def +40 SpA +20 SpD +20 Spe | Typhlosionite |
| 2 | Mega Torterra | Grass/Ground | Solid Rock (N; now) | 95/129/145/75/115/66 (625) | +20 Atk +40 Def +30 SpD +10 Spe | Torterrite |
| 3 | Mega Infernape | Fire/Fighting | Iron Fist (H; now) | 76/129/81/129/81/138 (634) | +25 Atk +10 Def +25 SpA +10 SpD +30 Spe | Infernapite |
| 4 | Mega Empoleon | Water/Steel | Competitive (H; R27) | 84/86/113/146/126/75 (630) | +25 Def +35 SpA +25 SpD +15 Spe | Empoleonite |
| 5 | Mega Serperior | Grass → **Grass/Dragon** | Contrary (H; R27) | 75/75/110/115/110/143 (628) | +15 Def +40 SpA +15 SpD +30 Spe | Serperiorite |
| 6 | Mega Samurott | Water → **Water/Steel** | Sharpness (N; R27) | 95/140/105/108/85/95 (628) | +40 Atk +20 Def +15 SpD +25 Spe | Samurottite |
| 7 | Mega Decidueye | Grass/Ghost | Infiltrator (N; R27) | 78/142/90/100/110/110 (630) | +35 Atk +15 Def +10 SpD +40 Spe | Decidueyite |
| 8 | Mega Incineroar | Fire/Dark | Intimidate (H; now) | 95/145/110/80/110/90 (630) | +30 Atk +20 Def +20 SpD +30 Spe | Incineroarite |
| 9 | Mega Primarina | Water/Fairy | Liquid Voice (H; R27) | 80/74/94/156/136/90 (630) | +20 Def +30 SpA +20 SpD +30 Spe | Primarinite |
| 10 | Mega Rillaboom | Grass | Grassy Surge (H; R27) | 100/160/110/60/90/110 (630) | +35 Atk +20 Def +20 SpD +25 Spe | Rillaboomite |
| 11 | Mega Cinderace | Fire | Libero (H; R27) | 80/151/95/65/95/144 (630) | +35 Atk +20 Def +20 SpD +25 Spe | Cinderacite |
| 12 | Mega Inteleon | Water | Sniper (H; now) | 70/95/80/160/80/145 (630) | +10 Atk +15 Def +35 SpA +15 SpD +25 Spe | Inteleonite |
| 13 | Mega Meowscarada | Grass/Dark | Magician (N; R27) | 76/150/85/81/90/148 (630) | +40 Atk +15 Def +20 SpD +25 Spe | Meowscaradite |
| 14 | Mega Skeledirge | Fire/Ghost | Punk Rock (N; R27) | 104/75/115/145/105/86 (630) | +15 Def +35 SpA +30 SpD +20 Spe | Skeledirgite |
| 15 | Mega Quaquaval | Water/Fighting | Dancer (N; R27) | 85/155/100/85/90/115 (630) | +35 Atk +20 Def +15 SpD +30 Spe | Quaquavalite |

Patterns: eight make the hidden ability certain (H), six take a new one (N) and one sets the weather (W). Five run in the engine today (Drought, Solid Rock, Iron Fist, Intimidate, Sniper); the other ten have their names only until R27, as every later Mega's ability does now. No two of the fifteen share an ability. Only Typhlosion's Drought is shared with an official starter Mega (Mega Charizard Y).

## The Hisuian forms of Typhlosion, Samurott and Decidueye

**Proposal: none. A Hisuian form has no Mega, and the stone does nothing in its hand.**

- **The games give no regional form a Mega.** A Galarian Slowbro holding Slowbronite doesn't Mega Evolve. In Showdown a stone is keyed by the species' own name (`megaStone: {"Slowbro": "Slowbro-Mega"}`), so `Slowbro-Galar` never matches. Mega Raichu X and Y come from Kanto's Raichu, not the Alolan one (Raichunite's `megaStone` names `Raichu`). Typhlosionite, Samurottite and Decidueyite would follow the same rule.
- **The user's list is of the starters' final forms**, and the Hisuian forms are a different line. Quilava, Dewott and Dartrix evolve into them only in Hisui (`evolutions` with a `region`). They already have their own typing (Fire/Ghost, Water/Dark, Grass/Fighting), their own signature moves (Infernal Parade, Ceaseless Edge, Triple Arrows) and their own place in the game.
- **Three more designs would be three more sculpts** of a different build, from bodies we sculpted apart (`Typhlosion-Hisui`, `Samurott-Hisui`, `Decidueye-Hisui`). The fifteen's batch would grow by a fifth for forms nobody asked for.
- **Fifteen distinct Megas need it.** Mega Typhlosion's Drought and Fire/Ground, Mega Samurott's Steel and Sharpness, and Mega Decidueye's archer's bow would not suit the Hisuian builds. Each would need a design of its own, and the Hisuian Samurott's hidden ability is the very Sharpness the regional Mega takes.

**What it needs in code**: `BattleCore.MegaFormOf` lets a Pokémon in any form use a stone whose `HeldByForm` is null. So a Hisuian Typhlosion holding Typhlosionite would today take `Typhlosion-Mega`, and, of the official stones, a Galarian Slowbro holding Slowbronite would take `Slowbro-Mega` and an Alolan Raichu holding Raichunite X would take `Raichu-Mega-X`. **This is a gap in R20 itself**, found while writing this proposal. The build session closes it: a stone with no `HeldByForm` works only for a Pokémon that is in no form of `FormKind.Regional` (Showdown's rule, since the regional form's name is never a key of the stone). It is held by one test of `MegaEvolutionTests` with a Galarian Slowbro, and by one with a Hisuian Typhlosion once the stone exists.

**If the user wants them anyway** (a `-Hisui-Mega` each, with stones of their own: the shared stone can't tell the forms apart without `HeldByForm`, which names one form), a sketch for a later session to propose in full:
- Mega Hisuian Typhlosion: Fire/Ghost, Cursed Body or Infiltrator, Special Attack and Speed.
- Mega Hisuian Samurott: Water/Dark, Sharpness (its own hidden ability), Attack and Speed. The regional Mega would then take another ability (Tough Claws), so the two don't share one.
- Mega Hisuian Decidueye: Grass/Fighting, Scrappy, Attack and Defense.

Each would be one more row of this table, one more stone and one more sculpt.

## Balance

Measured against the official Megas in the data (97 Mega forms; the Legends: Z-A Megas among them, whose totals run 565 to 700 for non-legendaries):

- **Totals**: 625 to 634, the starter Megas' own range (Mega Venusaur 625, Charizard 634, Blaziken 630, Swampert 635, Meganium 625, Feraligatr 630, Emboar 628, Chesnaught 630, Delphox 634, Greninja 630). None is out of line.
- **Speed**: the fastest are Meowscarada at 148, Inteleon at 145, Cinderace at 144 and Serperior at 143. The official Megas reach 150 to 153 (Mega Alakazam 150, Mega Aerodactyl 150, Mega Absol Z 151, Mega Zeraora 153). None is above that line.
- **Highest attacking stat**: Rillaboom's 160 Attack and Inteleon's 160 Sp. Atk, the same as Mega Blaziken's and Mega Feraligatr's 160 and below Mega Heracross's 185 or Mega Alakazam's 175.

**Flagged as strong** (each still inside the official Megas' range; the user may want one turned down):
1. **Mega Typhlosion**: Drought with 149 Sp. Atk and Eruption at full HP. This is Mega Charizard Y's pattern (159 Sp. Atk, Drought), one of the strongest Megas. Fire/Ground also takes Water moves at two times, not four, under its own sun. *To turn down*: Sp. Atk 139 and Defense 108.
2. **Mega Rillaboom**: 160 Attack with Grassy Surge, which makes Grassy Glide a priority move every turn and heals it, while Grassy Terrain halves Earthquake against it. *To turn down*: Tough Claws (N) in place of Grassy Surge, or Attack 150 and Speed 120.
3. **Mega Serperior**: Contrary with 143 Speed. Each Leaf Storm raises it two stages, and Dragon adds coverage. The base species is already a known sweeper. *To turn down*: Speed 133 and Sp. Def 120.
4. **Mega Cinderace**: Libero with 151 Attack and 144 Speed. Under the newest rules Libero acts once per entry, which keeps it below the old Protean Megas. Flagged for the pair of numbers alone.

**Weaker by design**: Mega Quaquaval's Dancer and Mega Decidueye's Infiltrator are chosen for the Pokémon's character over its power. Moxie and Long Reach are the safe alternatives named in their rows.

## The technical plan (for after the user's answers)

All of it is under the modern rules alone, as R20's Megas are: `Ruleset.MegaEvolution` gates the Mega Evolution itself. Every game is modern now (R19a), so this only keeps Platinum's own battles (`battles-platinum.txt`) out of reach.

### 1. The data (one sitting, about 3 hours)

- **The forms.** `Overrides/species.json` replaces a field whole (`Overrides.Apply` in `tools/DataImporter/Program.cs`), so an override giving Typhlosion `forms` would wipe out the imported Hisuian form, and Rillaboom's, Cinderace's and Inteleon's Gigantamax forms. The importer gains **`Overrides/forms.json`**: a species' name mapped to a list of forms appended to its imported `forms` (a form of the same name replaced), applied after `species.json`'s overrides, with a comment over each form. About 30 lines and one test. Each form is written as the importer writes Meganium's (`name`, `kind: "Mega"`, `types` only where they change, `baseStats`, `abilities`, `height`, `weight`; no learnset, as a battle-only form has none). A Mega's `color` follows Showdown's for the others; ours is the colour the look names.
- **The stones.** Fifteen new items in `Overrides/items.json`, ids 9002 to 9016 after the Distortion Orb's 9001. Each is in the Items pocket, priced 0, `canUseInBattle: false`, with a `megaStone` of `{ "species": …, "form": "<Name>-Mega" }` and our own description in the form the data already uses ("Held by Typhlosion, it lets it Mega Evolve in battle."). The importer's own check (`Program.cs`: "brings out unknown form") holds each stone to its form.
- **The regional gap** in `BattleCore.MegaFormOf`, above, closed with its two tests.
- **Romanian**: thirty entries in the hand-written `Data/text/ro/names.items.json`, a name (`Typhlosionit`, as `Meganiumite` is `Meganiumit`) and a description for each stone, held by `RomanianAbilityItemNamesTests` (whole, unique, drawable, no cedilla, fits the bag). The battle's line "has Mega Evolved into Mega {species}" needs nothing new. Romanian has no form names of its own today; plan 17 · P17 gives the forms their Pokédex text.
- **Where the stones are found**: R20 left every stone to Kalos (plan 23), with the Key Stone. Proposal: each of our stones is given in its starter's home region once that region's chapter stands and the player holds the Key Stone. The region's professor gives it after its Hall of Fame (Rowan the Torterrite, Infernapite and Empoleonite; Kalos's the Kalos ones, and so on). Until then, nothing gives them, as nothing gives the official stones. It is a line in plan 23 for each region, written when this proposal is approved.
- **Then**: run the importer, which rewrites `coverage.md` (Mega Stones 89 → 104; the items' count up by fifteen). Run `GOLDEN=accept … --filter Golden` and read the diff. Nothing should move: no trainer of the corpus has a Key Stone, `BattleCorpus` never draws a stone (no hold effect), and a species' forms aren't drawn as battlers. `MegaEvolutionTests` gains one test that runs a Mega of our own, chosen with the button and by the AI, under the modern rules.

### 2. The sculpts (one or two sittings, about 6 to 10 hours)

- Fifteen forms, each a sculpt of its own in plan 03's way, in **`PokemonModels.MegasOfOurOwn.cs`** in National Pokédex order (Typhlosion 157 to Quaquaval 914). Each is listed in `PokemonModels.Forms`. As `Meganium-Mega` is built (`MeganiumBuild(true)` in `PokemonModels.Johto1.cs`), each species' existing build takes a `mega` flag where the Mega shares most of the body, and the new pieces (the crater, the cape, the drum, the scope) are helpers in the new file.
- The rules that file lists: distinct from every other sculpt, in one piece (the floating spheres of water round Mega Primarina are joined by strands; none goes in `PiecesOf`), eyes as decals on one smooth curve, paint for patches, no `LooksLike`.
- Checked with the harness: `dex Typhlosion-Mega Torterra-Mega …` front and `--back`, `pokemon` turntables for each, and `versus` for a few of them in battle (Mega Evolved through the button, Transform's cue standing in until plan 25 · Y14). Shown to the user before it is kept, as the designs are.
- The `dex` boards meshing fifteen new models take a few minutes with a shared `POKEMON_CACHE`.

### 3. What is not this session's

- The Mega Evolution's own cue and the button's picture: plan 25 · Y14.
- The ten abilities in name only: R27 runs them.
- The trainers who carry these Megas (a region's Champion, a rematch): plan 23's chapters. The trainer AI already Mega Evolves whenever it can.
- More species: only on the user's word (plan 06 · R20b, "More species follow only on the user's word").

**In all**: this proposal, then about 3 hours of data and 6 to 10 of sculpts once the user has answered, as one session or two (the data first, so the sculpts can be checked in battle).
