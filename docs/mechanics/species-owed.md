# Every species, region by region: what is owed

Written by `NationalSpeciesTests.TheReportOfWhatEachRegionOwesIsWhatTheChecksGive` from `SpeciesSources` and the checks' lists of what is owed (plan 40 · GE1); run `GOLDEN=accept dotnet test PokemonPlatinumTests --filter NationalSpeciesTests` to write it again. A species is had in a region when its own game gives it there with nobody else: its open areas, rooms and scripts, its professor's three, its own counters, and whatever evolves or hatches from those with the items and places the region gives (a trade's evolution by a Linking Cord). A session that gives a way takes the species off its check's list, and the checks fail until it does.

| Region | Pokédex | Species | Had | Owed |
|---|---|---:|---:|---:|
| Sinnoh | `extended-sinnoh` | 210 | 210 | 0 |
| Kanto | `kanto` | 151 | 150 | 1 |
| Johto | `updated-johto` | 256 | 248 | 8 |
| Hoenn | `hoenn` | 202 | 200 | 2 |
| Unova | `updated-unova` | 301 | 283 | 18 |
| Kalos | `kalos-central`, `kalos-coastal`, `kalos-mountain` | 457 | 457 | 0 |

Sinnoh's Pokédex is had whole. What of Diamond's, Pearl's and Platinum's National Pokédex nothing else in Sinnoh gives (76 species, `SinnohSpeciesTests.OnlyInPalPark`) comes by Pal Park's Catching Show (plan 03 · D12a).

## Kanto

- **had in another region: Hoenn's Faraway Island**: Mew

## Johto

- **had in another region: Hoenn's Faraway Island**: Mew
- **plan 38 · EX4: headbutt trees**: Pineco, Forretress, Exeggcute, Exeggutor, Heracross, Aipom, Ambipom

## Hoenn

- **plan 38 · EX4: Route 119's Feebas**: Feebas, Milotic

## Unova

- **plan 38 · EX11: dust clouds**: Drilbur, Excadrill
- **plan 38 · EX11: rippling water**: Alomomola, Wailmer, Wailord, Lapras
- **plan 38 · EX11: shaking grass**: Dunsparce, Audino, Pansage, Simisage, Pansear, Simisear, Panpour, Simipour, Emolga, Castform
- **plan 38 · EX11: the bridges' shadows**: Ducklett, Swanna

## Kalos


## The National Pokédex

Generations 1 to 5, had nowhere in the game yet:

- **plan 38 · EX11: Unova's dust clouds**: Drilbur, Excadrill

The later generations, owed by their regions' plans until each plan's last session runs `SpeciesSources.Of` for its region:

| Owed by | Species | Had somewhere | Not yet |
|---|---:|---:|---:|
| plan 23: Kalos | 72 | 72 | 0 |
| plan 23: Alola | 88 | 83 | 5 |
| plan 33: Galar | 89 | 25 | 64 |
| plan 23: Hisui | 7 | 1 | 6 |
| plan 34: Paldea | 120 | 4 | 116 |

**plan 23: Alola**, not yet had: Magearna, Marshadow, Zeraora, Meltan, Melmetal.

**plan 33: Galar**, not yet had: Gossifleur, Eldegoss, Rolycoly, Carkol, Coalossal, Applin, Flapple, Appletun, Silicobra, Sandaconda, Cramorant, Arrokuda, Barraskewda, Toxel, Toxtricity, Sizzlipede, Centiskorch, Clobbopus, Grapploct, Sinistea, Polteageist, Hatenna, Hattrem, Hatterene, Impidimp, Morgrem, Grimmsnarl, Obstagoon, Perrserker, Cursola, Sirfetch'd, Mr. Rime, Runerigus, Milcery, Alcremie, Falinks, Pincurchin, Snom, Frosmoth, Stonjourner, Eiscue, Indeedee, Morpeko, Cufant, Copperajah, Dracozolt, Arctozolt, Dracovish, Arctovish, Duraludon, Dreepy, Drakloak, Dragapult, Zacian, Zamazenta, Eternatus, Kubfu, Urshifu, Zarude, Regieleki, Regidrago, Glastrier, Spectrier, Calyrex.

**plan 23: Hisui**, not yet had: Kleavor, Ursaluna, Basculegion, Sneasler, Overqwil, Enamorus.

**plan 34: Paldea**, not yet had: Sprigatito, Floragato, Meowscarada, Fuecoco, Crocalor, Skeledirge, Quaxly, Quaxwell, Quaquaval, Lechonk, Oinkologne, Tarountula, Spidops, Nymble, Lokix, Pawmi, Pawmo, Pawmot, Tandemaus, Maushold, Fidough, Dachsbun, Smoliv, Dolliv, Arboliva, Squawkabilly, Nacli, Naclstack, Garganacl, Charcadet, Armarouge, Ceruledge, Tadbulb, Bellibolt, Wattrel, Kilowattrel, Maschiff, Mabosstiff, Shroodle, Grafaiai, Bramblin, Brambleghast, Toedscool, Toedscruel, Klawf, Capsakid, Scovillain, Rellor, Rabsca, Flittle, Espathra, Tinkatink, Tinkatuff, Tinkaton, Wiglett, Wugtrio, Bombirdier, Finizen, Palafin, Varoom, Revavroom, Cyclizar, Orthworm, Glimmet, Glimmora, Greavard, Houndstone, Flamigo, Cetoddle, Cetitan, Veluza, Dondozo, Tatsugiri, Clodsire, Great Tusk, Scream Tail, Brute Bonnet, Flutter Mane, Slither Wing, Sandy Shocks, Iron Treads, Iron Bundle, Iron Hands, Iron Jugulis, Iron Moth, Iron Thorns, Frigibax, Arctibax, Baxcalibur, Gimmighoul, Gholdengo, Wo-Chien, Chien-Pao, Ting-Lu, Chi-Yu, Roaring Moon, Iron Valiant, Koraidon, Miraidon, Walking Wake, Iron Leaves, Dipplin, Poltchageist, Sinistcha, Okidogi, Munkidori, Fezandipiti, Ogerpon, Archaludon, Hydrapple, Gouging Fire, Raging Bolt, Iron Boulder, Iron Crown, Terapagos, Pecharunt.
