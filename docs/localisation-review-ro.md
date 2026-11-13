# Romanian text to review

Written by a translation session (plan 17 · N6); for the user's review. Each row is something where the Romanian is a choice, not a rendering. The files are `PokemonPlatinumEngine/Data/text/ro/`.

## Terms chosen

| English | Romanian chosen | Where | Why unsure |
|---|---|---|---|
| Pennant Hall, pennants | Sala Fanioanelor, fanioane | `scripts/Hub_Plaza.json`, `world/hub_town.json` | `ui.json` already says FANIOANE and Sala Fanioanelor; kept |
| Laurel Gallery / Laurel Court / Laurel Circuit | Galeria Laurilor / Curtea Laurilor / Circuitul Laurilor | `scripts/Hub_*.json` | `ui.json` has Galeria Laurilor; the Court and the Circuit are my own match |
| Open Door Agency | Agenția Ușa Deschisă | `scripts/Hub_Office.json` | already in the files |
| Gift Runner | Curier de Cadouri | `scripts/common.json`, 90+ overlays | an older overlay line has "Curier de daruri"; I unified on `ui.json`'s "Curier de Cadouri" |
| the four Delve hollows: Brinecave, The Rootwood, Heartpeak, The Old Tower | Peștera Sărată, Pădurea Rădăcinilor, Vârful Inimii, Turnul cel Vechi | `scripts/survival_area.json` (DelveKeeper, 3) | invented place names, translated by meaning; maybe keep the English names |
| Delve camp / "hollow" | tabără de explorare / scobitură | `scripts/survival_area.json` | "scobitură" is stiff; a better word for the game's "hollow"? |
| The cups: Crest, Sprout, Spark, Pebble, Summit | Cupa Creastei, Mugurului, Scânteii, Pietricelei, Culmii | `scripts/BattleFrontierStadium.json` | invented cup names, rendered by meaning |
| Battle Points | Puncte de Luptă (the abbreviation BP kept in "{player} a primit {number} BP!") | various | matches the existing files |
| Props (Pokémon Musical) | Accesorii | `scripts/musical_theater.json` | the item "Prop Trunk" is shown through `{item}` elsewhere; check `names.items.json` has it |
| take (a film take, Jubilife studio) | duplă | `scripts/JubilifeTV1F.json` | film jargon; "duplă" is the usual word but check the register |
| the meals: Mushroom Bake, Spiced Flatbread, Berry Pilaf, Quiet Broth, Bean Hotpot, Potato Hash, Honey Oat Cakes | Ciuperci la cuptor, Lipie condimentată, Pilaf cu boabe, Supă liniștită, Tocană de fasole, Cartofi sotați, Turte de ovăz cu miere | five `MealStand` scripts | invented dishes; the menu line carries the price in the choice, so they must stay short |
| Ismene's play on "season", "act", "bays" | sezon, act, golfuri | `scripts/Hub_Keep.json` | the theatre metaphor ("golful scenei" for stage bay) may read oddly |
| Looker's deduction lines ("Then we deduce!") | "Atunci deducem!" | `scripts/Hub_Office.json` | fine, but a more idiomatic detective phrase may exist |
| Berry / Berries | Boabă / Boabe | everywhere | taken from `names.items.json` ("Boabă Oran") |
| Tera Shard, Tera Orb | Ciob Tera (plural: cioburi Tera), Globul Tera | `scripts/common.json` | from `names.items.json`; the menu line "CIOBURI TERA" is capitalised by me |

## Lines I was least sure of

| File | Key | English | Romanian | Why unsure |
|---|---|---|---|---|
| `scripts/Hub_Keep.json` | Ghetsis 4 | Leave me. I am composing a very long complaint. | Lasă-mă. Compun o reclamație foarte lungă. | the joke is dry; register is formal on purpose |
| `scripts/Hub_Keep.json` | Ismene 9 | They didn't disagree. | Nu au zis nu. | idiom |
| `scripts/Hub_Arena.json` | Herald 20 | the Laurel Champion | Campionul Laurilor | a title of the game's own |
| `scripts/common.json` | Courier 14 | a sparrow and clockwork, of all things | o vrabie și un mecanism de ceasornic, dintre toate lucrurile! | slightly freer than the English |
| `scripts/common.json` | TrainingExchange 4 | A Bottle Cap goes to the Head Coach of any training hall | Antrenorul-șef | "Head Coach" rendered as Antrenor-șef |
| `scripts/musical_theater.json` | Desk 0 | Welcome, darling. | Bun venit, dragule. | the owner's gender is not told; may need "dragă" |
| `scripts/dewford_town.json` | CaseHoennSailor 1 | the sea song "Salt on the label, light in the hand, lamps for the dark of the land!" | Sare pe etichetă, lumină în mână, lămpi pentru bezna din țară! | rhyme lost; the English rhymes |
| `scripts/ruins_of_alph.json` | CaseJohtoMirelle 3 | Top marks. | Notă maximă. | |
| Various | Looker / Lockwork lines | "Efficient. And beaten. Wonderful." | Eficient. Și învins. Minunat. | |
| `ui.json` | basme.learned (plan 46 · CP12) | {player} learned the tale of {tale}! | {player} a învățat basmul despre {tale}! | Carpathia's words, for the user's review (decision 7); "basmul despre" or "basmul lui"? `{tale}` is the tale's name without an article (Zmeu, Vârcolac, Baba Dochia) |
| `ui.json` | basme.writtenDown (plan 46 · CP12) | It was written into the Book of Tales. | A fost scris în Cartea Basmelor. | Carpathia's words, for the user's review |

Place names and people's names (Looker, Quincy Rowan-Pike, Ione Calder, towns of the games) are kept in English, as the existing files do.
