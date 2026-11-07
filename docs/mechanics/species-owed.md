# Every species, region by region: what is owed

Written by `NationalSpeciesTests.TheReportOfWhatEachRegionOwesIsWhatTheChecksGive` from `SpeciesSources` and the checks' lists of what is owed (plan 40 · GE1); run `GOLDEN=accept dotnet test PokemonPlatinumTests --filter NationalSpeciesTests` to write it again. A species is had in a region when its own game gives it there with nobody else: its open areas, rooms and scripts, its professor's three, its own counters, and whatever evolves or hatches from those with the items and places the region gives (a trade's evolution by a Linking Cord). A session that gives a way takes the species off its check's list, and the checks fail until it does.

| Region | Pokédex | Species | Had | Owed |
|---|---|---:|---:|---:|
| Sinnoh | `extended-sinnoh` | 210 | 210 | 0 |
| Kanto | `kanto` | 151 | 128 | 23 |
| Johto | `updated-johto` | 256 | 218 | 38 |
| Hoenn | `hoenn` | 202 | 187 | 15 |
| Unova | `updated-unova` | 301 | 248 | 53 |

Sinnoh's Pokédex is had whole. What of Diamond's, Pearl's and Platinum's National Pokédex nothing else in Sinnoh gives (70 species, `SinnohSpeciesTests.OnlyInPalPark`) comes by Pal Park's Catching Show (plan 03 · D12a).

## Kanto

- **GE2: FireRed's own trades**: Farfetch'd, Lickitung, Mr. Mime, Jynx
- **GE3: FireRed's Game Corner prizes**: Porygon
- **GE3: LeafGreen's, in its own places**: Sandshrew, Sandslash, Vulpix, Ninetales, Bellsprout, Weepinbell, Victreebel, Slowpoke, Slowbro, Staryu, Starmie, Magmar, Pinsir
- **GE3: a Linking Cord in Kanto**: Alakazam, Machamp, Golem, Gengar
- **had in another region: Hoenn's Faraway Island**: Mew

Items and places its evolutions ask for and nothing gives: Linking Cord (GE3: a Linking Cord in Kanto).

## Johto

- **GE3: SoulSilver's, in its own places**: Ledyba, Ledian, Vulpix, Ninetales, Meowth, Persian, Teddiursa, Ursaring, Delibird, Skarmory
- **GE3: a Fire Stone in Johto**: Arcanine, Flareon
- **GE3: a Linking Cord and an Upgrade in Johto**: Porygon2
- **GE3: a Linking Cord in Johto**: Alakazam, Machamp, Golem, Gengar, Politoed, Slowking, Scizor, Kingdra
- **GE3: a Sun Stone in Johto**: Bellossom, Sunflora
- **GE3: a Thunder Stone in Johto**: Raichu, Jolteon
- **GE3: fossils at Pewter's museum**: Omanyte, Omastar, Kabuto, Kabutops, Aerodactyl
- **had in another region: Hoenn's Faraway Island**: Mew
- **plan 38 · EX4: headbutt trees**: Pineco, Forretress, Exeggcute, Exeggutor, Heracross, Aipom, Ambipom

Items and places its evolutions ask for and nothing gives: Linking Cord (GE3: a Linking Cord in Johto); Thunder Stone (GE3: a Thunder Stone in Johto); Fire Stone (GE3: a Fire Stone in Johto); Sun Stone (GE3: a Sun Stone in Johto); Upgrade (GE3: an Upgrade in Johto).

## Hoenn

- **GE3: Ruby's and Sapphire's, in their own places**: Meditite, Medicham, Roselia, Zangoose, Lunatone
- **GE3: a Linking Cord in Hoenn**: Alakazam, Golem, Machamp, Kingdra, Huntail, Gorebyss
- **plan 38 · EX1: Hoenn's television outbreak**: Surskit, Masquerain
- **plan 38 · EX4: Route 119's Feebas**: Feebas, Milotic

Items and places its evolutions ask for and nothing gives: Linking Cord (GE3: a Linking Cord in Hoenn).

## Unova

- **GE2: Black 2's own trades**: Petilil, Lilligant
- **GE3: Black 2's gift**: Zorua, Zoroark
- **GE3: Black 2's gift and grass**: Eevee, Vaporeon, Jolteon, Flareon, Espeon, Umbreon, Leafeon, Glaceon
- **GE3: Black 2's grass, which the import has no place for**: Buneary, Lopunny
- **GE3: Chargestone Cave's Magnetic Field**: Magnezone, Probopass
- **GE3: White 2's legendary, in its own place**: Reshiram
- **GE3: White 2's, in its own places**: Elekid, Electabuzz, Skitty, Delcatty, Numel, Camerupt
- **GE3: a Linking Cord in Unova**: Magmortar, Electivire, Conkeldurr, Escavalier, Accelgor
- **GE3: a way of ours, for Black 2's Dream Radar**: Tornadus, Thundurus
- **GE3: fossils at Nacrene's museum**: Tirtouga, Carracosta, Archen, Archeops
- **plan 38 · EX11: dust clouds**: Steelix, Drilbur, Excadrill
- **plan 38 · EX11: rippling water**: Alomomola, Wailmer, Wailord, Lapras
- **plan 38 · EX11: shaking grass**: Dunsparce, Audino, Pansage, Simisage, Pansear, Simisear, Panpour, Simipour, Emolga, Castform
- **plan 38 · EX11: the bridges' shadows**: Ducklett, Swanna

Items and places its evolutions ask for and nothing gives: Linking Cord (GE3: a Linking Cord in Unova); Magnetic Field (GE3: Chargestone Cave's Magnetic Field); Moss Rock (GE3: Pinwheel Forest's Moss Rock); Ice Rock (GE3: Twist Mountain's Ice Rock); Electirizer (GE3: an Electirizer in Unova).

## The National Pokédex

Generations 1 to 5, had nowhere in the game yet:

- **GE3: White 2's legendary, in its own place**: Reshiram
- **GE3: a way of ours, for Black 2's Dream Radar**: Tornadus, Thundurus
- **GE3: fossils at Nacrene's museum**: Tirtouga, Carracosta, Archen, Archeops
- **plan 38 · EX11: Unova's dust clouds**: Drilbur, Excadrill
- **plan 38 · EX11: Unova's rippling water**: Alomomola

The later generations, owed by their regions' plans until each plan's last session runs `SpeciesSources.Of` for its region:

| Owed by | Species | Had somewhere | Not yet |
|---|---:|---:|---:|
| plan 23: Kalos | 72 | 59 | 13 |
| plan 23: Alola | 88 | 49 | 39 |
| plan 33: Galar | 89 | 0 | 89 |
| plan 23: Hisui | 7 | 1 | 6 |
| plan 34: Paldea | 120 | 4 | 116 |

**plan 23: Kalos**, not yet had: Aromatisse, Slurpuff, Binacle, Barbaracle, Tyrunt, Tyrantrum, Amaura, Aurorus, Yveltal, Zygarde, Diancie, Hoopa, Volcanion.

**plan 23: Alola**, not yet had: Crabominable, Mareanie, Toxapex, Stufful, Bewear, Oranguru, Wimpod, Golisopod, Sandygast, Palossand, Type: Null, Silvally, Minior, Turtonator, Drampa, Dhelmise, Tapu Koko, Tapu Lele, Tapu Bulu, Cosmog, Cosmoem, Lunala, Nihilego, Buzzwole, Pheromosa, Xurkitree, Celesteela, Kartana, Guzzlord, Necrozma, Magearna, Marshadow, Poipole, Naganadel, Stakataka, Blacephalon, Zeraora, Meltan, Melmetal.

**plan 33: Galar**, not yet had: Grookey, Thwackey, Rillaboom, Scorbunny, Raboot, Cinderace, Sobble, Drizzile, Inteleon, Skwovet, Greedent, Rookidee, Corvisquire, Corviknight, Blipbug, Dottler, Orbeetle, Nickit, Thievul, Gossifleur, Eldegoss, Wooloo, Dubwool, Chewtle, Drednaw, Yamper, Boltund, Rolycoly, Carkol, Coalossal, Applin, Flapple, Appletun, Silicobra, Sandaconda, Cramorant, Arrokuda, Barraskewda, Toxel, Toxtricity, Sizzlipede, Centiskorch, Clobbopus, Grapploct, Sinistea, Polteageist, Hatenna, Hattrem, Hatterene, Impidimp, Morgrem, Grimmsnarl, Obstagoon, Perrserker, Cursola, Sirfetch'd, Mr. Rime, Runerigus, Milcery, Alcremie, Falinks, Pincurchin, Snom, Frosmoth, Stonjourner, Eiscue, Indeedee, Morpeko, Cufant, Copperajah, Dracozolt, Arctozolt, Dracovish, Arctovish, Duraludon, Dreepy, Drakloak, Dragapult, Zacian, Zamazenta, Eternatus, Kubfu, Urshifu, Zarude, Regieleki, Regidrago, Glastrier, Spectrier, Calyrex.

**plan 23: Hisui**, not yet had: Kleavor, Ursaluna, Basculegion, Sneasler, Overqwil, Enamorus.

**plan 34: Paldea**, not yet had: Sprigatito, Floragato, Meowscarada, Fuecoco, Crocalor, Skeledirge, Quaxly, Quaxwell, Quaquaval, Lechonk, Oinkologne, Tarountula, Spidops, Nymble, Lokix, Pawmi, Pawmo, Pawmot, Tandemaus, Maushold, Fidough, Dachsbun, Smoliv, Dolliv, Arboliva, Squawkabilly, Nacli, Naclstack, Garganacl, Charcadet, Armarouge, Ceruledge, Tadbulb, Bellibolt, Wattrel, Kilowattrel, Maschiff, Mabosstiff, Shroodle, Grafaiai, Bramblin, Brambleghast, Toedscool, Toedscruel, Klawf, Capsakid, Scovillain, Rellor, Rabsca, Flittle, Espathra, Tinkatink, Tinkatuff, Tinkaton, Wiglett, Wugtrio, Bombirdier, Finizen, Palafin, Varoom, Revavroom, Cyclizar, Orthworm, Glimmet, Glimmora, Greavard, Houndstone, Flamigo, Cetoddle, Cetitan, Veluza, Dondozo, Tatsugiri, Clodsire, Great Tusk, Scream Tail, Brute Bonnet, Flutter Mane, Slither Wing, Sandy Shocks, Iron Treads, Iron Bundle, Iron Hands, Iron Jugulis, Iron Moth, Iron Thorns, Frigibax, Arctibax, Baxcalibur, Gimmighoul, Gholdengo, Wo-Chien, Chien-Pao, Ting-Lu, Chi-Yu, Roaring Moon, Iron Valiant, Koraidon, Miraidon, Walking Wake, Iron Leaves, Dipplin, Poltchageist, Sinistcha, Okidogi, Munkidori, Fezandipiti, Ogerpon, Archaludon, Hydrapple, Gouging Fire, Raging Bolt, Iron Boulder, Iron Crown, Terapagos, Pecharunt.
