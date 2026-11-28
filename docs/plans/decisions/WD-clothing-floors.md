# Plan 47 · Clothing floors in every region's big store (proposal, 2026-11-22)

Written 2026-11-22, docs only, from [the user's answers of the same day](ANSWERS-2026-11-22.md) ("New work from the
user's note": *"I also want to add a lot of clothing and we need to add stores for that in some of the key cities, in
each generation there are malls from what I remember, you can add in them a new floor for clothing specific to that
region."*). The answer asks a session of [plan 47](../47-wardrobe-of-pieces.md) to write **which region has which mall
or department store, and how many garments each new clothing floor sells**, and to show it to the user before anything
is built. This is that list. Nothing is built; every number below is a proposal.

**Waiting on the user**: the per-region table (section 3), the totals (section 4) and the eleven questions (section 6).
Anything left unanswered goes ahead on its recommendation, as the answer sheet's rule has it.

---

## 1. What the wardrobe has today (read from the code and data)

- **`Data/clothes.json`: 39 garments** in five slots (`Hat`, `Top`, `Bottoms`, `Shoes`, `Bag`), of which two are
  "nothing" (`no_hat`, `no_bag`). Each is a colour, an accent and a few shape switches (`Garment` in
  `Data/ClothingDatabase.cs`): a hat's kind (`hat`), a top's `coat`, `shortSleeves`, `stripes`, `scarf`, the bottoms'
  `skirt` or `shorts`. Shoes and bags have no shape switch: every shoe is the same shoe, every bag the same backpack.
  By where they are sold: 26 of Sinnoh's (Jubilife), 5 of Lumiose's, 6 of Alola's.
- **Five shops sell them** (`boutiques` in `clothes.json`, opened by `wardrobe "<key>"`):
  | Key | Room | Region | Rows sold |
  |---|---|---|---|
  | `jubilife` | `Data/maps/JubilifeBoutique.json` (+ the salon's chair, the dyer and the photo studio, plan 43 · LF3, LF5) | Sinnoh | 26 |
  | `lumiose` | `Data/maps/LumioseCity_Boutique.json` (+ a dyer) | Kalos | 14 |
  | `lumiose_hats` | `Data/maps/LumioseCity_HatShop.json` | Kalos | 8 |
  | `hauoli` | `Data/maps/HauoliCity_ApparelShop.json` | Alola | 12 |
  | `royal_avenue` | `Data/maps/RoyalAvenue_Shop.json` | Alola | 10 |
  Kanto, Johto, Hoenn, Unova, Galar, Paldea and Hisui sell no clothes at all; Galar's and Paldea's player wardrobes
  (`Postwick_PlayersHouse_2F.txt`, `CaboPoco_PlayersHouse_2F.txt`) only change what is owned.
- **The salon and the dye house** (plan 43 · LF3) are built: `wardrobe salon` (cut, hair colour, eyes, skin) at
  Jubilife's boutique, `wardrobe dye "<key>"` (16 dyes, each with its berry; a dyed garment is `garment~dye`) at
  Jubilife's and Lumiose's. The dye counter ignores the key it is given (`GameEngine.Salon.cs`), so a dyer can stand at
  any counter.
- **The wardrobe screen** (`UI/WardrobeScreen.cs`) lists a slot's rows eight at a time (`VisibleRows = 8`), scrolling,
  in the file's order; at home it lists **every garment owned in that slot**, each dyed copy after its plain one. With
  300 more rows a home list of tops would run to a hundred or more: see question 8.
- **Shapes the kits can already draw but `clothes.json` cannot yet sell** (read from `Graphics/CharacterStyle.cs` and
  `tools/BlenderAnim/blender/sculpts/people_chibi.py` / `people_grown.py`):
  - **Hats**: `Headwear` has 14 kinds and both Blender files build all 14 (`HATS = [...]`). `CharacterStyle.Dressed`
    already reads any kind from a garment's `hat` field, so **a Fedora, Beanie, Bandana, Headband, Cowboy, Visor,
    WideHat or Bow can be sold today as a row of data alone**, no code.
  - **Tops**: the kits draw a zipped `Jacket`, a `Buttoned` coat, a `Robe` closed to the ankles, a `Vest` over the
    shirt and `Overalls` (people's looks use them), but `Garment` has no field for them; each is **a field and a line
    of `Dressed`**, no modelling.
- **Plan 47 as built** (2026-11-13, the user's route b): people are read from Blender files built at release
  (`Graphics/CharacterFiles.cs`, `CharacterModels.Files.cs`; 89 pieces a chibi build, nine grown files), with "no file,
  no change"; `--safe-mode` still draws everyone from the C# sculpting kits, **so a new shape must exist in both: the
  C# kits (chibi and grown) and the Blender scripts (`people_chibi.py`, `people_grown.py`)**. That is what makes a new
  shape dear and a new colour cheap.
- **Decision 5, answered 2026-11-22**: every new garment it listed (a hoodie with its hood, a skirt, a coat, boots, a
  second and a third bag), **no new slots**, and "a lot more clothing". So everything below fits the five slots.
- **Words**: garment names and descriptions are English only today (no Romanian file reads them; `ro/ui.json`'s
  `outfits` section translates the screen, not the garments). See question 10.

## 2. The big stores the game has today

| Region | Store in the game today | Floors today | Lift today | Files |
|---|---|---|---|---|
| **Sinnoh** | Veilstone Dept. Store (plan 01 · M11d, hand-made rooms) | B1F, 1F, 2F, 3F, 4F, 5F (the Rooftop Plaza): six, with stairs between each and a lift | `VeilstoneStoreElevator.txt`: six floors on a flag each (`FLAG_VEILSTONE_STORE_LIFT_AT_*`) and `VAR_VEILSTONE_STORE_LIFT_FLOOR` 1–6; the floor question is already at `choose`'s cap of six answers ("1F OR B1F" shares one) | `Data/maps/VeilstoneStore{B1F,1F..5F,Elevator}.json`, `Data/scripts/VeilstoneStore*.txt`, `RegionDatabase.cs` (Sinnoh list) |
| **Kanto** | Celadon Dept. Store (FireRed's, plan 19 · K5b) | 1F–5F and the roof | `celadon_city_department_store_elevator.txt`: `VAR_ELEVATOR_FLOOR` 1–5; question at the cap (5F..1F + Stay) | `Data/world/kanto/areas|overlays/celadon_city_department_store_*.json` |
| (Johto's Kanto) | HeartGold's own Celadon store, in Johto's world | 1F–5F, roof, lift | `celadon_department_store_elevator.txt` | `Data/world/johto/.../celadon_department_store_*.json` (see question 4) |
| **Johto** | Goldenrod Dept. Store (HeartGold's, plan 20 · J5b, J7) | B1F, 1F–6F (6F "Tranquil Square", its rooftop "closed for now") | `goldenrod_department_store_elevator.txt`: `VAR_GOLDENROD_STORE_LIFT_FLOOR` 1–6, the basement kept as 7 "since it is no number between the others"; first question at the cap ("LOWER FLOORS" leads to a second) | `Data/world/johto/.../goldenrod_department_store_*.json` |
| **Hoenn** | Lilycove Dept. Store (Emerald's, plan 21 · H7, H12k) | 1F–5F and the rooftop | `lilycove_city_department_store_elevator.txt`: `VAR_DEPT_STORE_FLOOR` 1–5; question at the cap | `Data/world/hoenn/.../lilycove_city_department_store_*.json` |
| **Unova** | Shopping Mall Nine on Route 9 (Black 2's, plan 22 · U8a) | one shop floor (`ShoppingMallNine_1F`, area `shopping_mall`: five counters and Unova's training hall, plan 36 · TR4) and its back room (`ShoppingMallNine_2F`, `shopping_mall_2`, up the north-east escalator) | none | `Data/world/unova/world.json`, `overlays/shopping_mall*.json`, `scripts/shopping_mall.txt` |
| **Kalos** | no department store in the originals; Lumiose's boutique, hat shop and Coiffure Clips | the boutique is one room (11 × 10) | none | `Data/maps/LumioseCity_Boutique.json`, `LumioseCity_HatShop.json` |
| **Alola** | Royal Avenue's Megamart (the islands' big store) | one room (15 × 12); plus the two apparel shops above | none | `Data/maps/RoyalAvenue_Megamart.json` |
| **Galar** | no store room; Motostoke and Wyndon each have a **building already standing with no door**: Motostoke's "the boutique" (`places/motostoke.json`, solid 70..94 × -702..-690, sign BOUTIQUE) and Wyndon's "a shop on the avenue" (`places/wyndon.json`, sign BOUTIQUE) | — | — | `Data/world3d/galar/places/{motostoke,wyndon}.json`, `RegionDatabase.cs` (Galar list) |
| **Paldea** | no store room; Mesagoza's **"the boutique by the plaza"** stands with no door (`places/mesagoza.json`, solid -44..-30 × -342..-328) | — | — | `Data/world3d/paldea/places/mesagoza.json` |
| **Hisui** | Jubilife Village's **clothier**: its door is in the area (`areas/jubilife_village.json` warp 5 → `JubilifeVillage_Clothier`) and **locked** (`overlays/jubilife_village.json`, `"locked": [4, 5, 6]`, "shut until a later session builds their rooms") | — | — | `Data/world/hisui/...`, `RegionDatabase.cs` (Hisui list) |
| **Carpathia** | not built (plan 46 · CP24, "the capital: the boulevards, the arch, the opera, the palace's outside") | — | — | plan 46 |

Not proposed: the hub of ours, Wayhaven (plan 41), and Kanto's and Hoenn's other shops (Slateport's market stalls,
the Sevii Islands); question 11 asks whether any is wanted.

**How a floor is reached, by kind of region** (what the later sessions edit):
- **Hand-made stores** (Veilstone): a new map file, stairs re-pointed (the 4F's up-stairs and the roof's down-stairs),
  a new stop on the lift, the floor directories' signs.
- **Imported stores** (Celadon, Goldenrod, Lilycove): a new hand-made room in `Data/maps`, named in the region's
  `Maps` list in `RegionDatabase.cs`, **reached by the lift alone**: a new `warp` line in the lift's script and a new
  answer in its question, whose first question is already at `choose`'s cap of six, so it splits as Goldenrod's
  already does ("UPPER FLOORS" / "LOWER FLOORS"). The new room's own lift door warps into the car, as every floor's does.
  No imported chunk is touched.
- **Imported rooms without a lift** (Mall Nine): an `exits` tile in the area's overlay (a warp of ours, docs/data-files
  .md, "World files"; Sunyshore's workshop is the precedent) onto a hand-made room.
- **Hand-drawn regions** (Kalos, Alola, Hisui): a new map file and a stairs warp in the room (Kalos, Alola) or the area
  door unlocked (Hisui's warp 5: take 5 out of `locked` and add its `doors` row).
- **Open regions** (Galar, Paldea): a `doors` entry on the standing building in the town's place file (a box, the
  room, `at`, `facing`, an `out` marker, as Wyndon's Pokémon Center has), a room in `Data/maps` named in the region's
  `Maps` list; `OpenWorldBake <region> --check` to see whether the bake moves.

## 3. The proposal, region by region

Counts are **rows of `clothes.json`**: a colourway is a row of its own (an id, a name, a description, a price), as
`navy_beret`, `mint_beret` and `plum_beret` are today. Under plan 47's zones a colourway is the same piece with other
colours, so a row is data alone; what costs is a **shape**. Each region's rows are split four ways:

- **N**: decision 5's new shapes (WD4): hoodie with hood, long skirt, short coat (a parka), tall boots, shoulder bag,
  waist bag / tote. Modelled in both kits, both routes.
- **K**: kit shapes not yet sold: the hats by data alone (Fedora, Beanie, Bandana, Headband, Cowboy, Visor, WideHat),
  the tops by a field (`Jacket`, `Buttoned`, `Vest`, `Robe`). No modelling.
- **R**: recolours of today's shapes (tee, long-sleeved top, coat with or without scarf, striped, trousers, shorts,
  skirt, shoes, backpack, beret, cap).
- **X**: a region's own new shape, beyond decision 5 (four in all, each shown to the user from a board first).

Prices follow today's (800 to 4,200) and rise with the point of the story a store is met at. Every floor has **one
clothes clerk** (`wardrobe "<key>"`) and, by question 6's default, a dyer (`wardrobe dye "<key>"`); a shopper or two
with a line each. Every floor opens **when its store does** (question 7); no flag of its own.

| # | Region | Floor and where | Theme of the stock | Rows | N | K | R | X | Price | Met at |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | **Sinnoh** | Veilstone Dept. Store, a new floor between 4F and the rooftop: **"5F · Sinnoh Wear"** (the roof becomes "Rooftop" on the signs; its file keeps its name) | the four corners of Sinnoh: Snowpoint's winter (parkas, beanies, boots), Pastoria's marsh rain, Hearthome's contest best (buttoned coats), Mt. Coronet's hiking (vests, shoulder bags) | **36** | 12 | 8 | 16 | — | 800–4,800 | 3 badges (Veilstone) |
| 2 | **Kanto** | Celadon Dept. Store, **"6F · Kanto Classics"**, above 5F, by the lift | the old Kanto look: caps and visors, sporty tees in the towns' colours (Cerulean, Vermilion, Lavender, Fuchsia, Cinnabar), a wide sun hat, a waist bag | **30** | 8 | 6 | 16 | — | 1,000–5,000 | 3 badges (Celadon) |
| 3 | **Johto** | Goldenrod Dept. Store, **"7F · Johto Wear"**, above Tranquil Square, by the lift (the rooftop stays closed) | old and new: yukata-like robes for Ecruteak's festivals, bandanas and fedoras of the radio's crowd, Goldenrod's city pop | **30** | 6 | 10 | 14 | — | 1,200–6,000 | 2 badges (Goldenrod) |
| 4 | **Hoenn** | Lilycove Dept. Store, **"6F · Hoenn Wear"**, by the lift (the rooftop stays reached from 5F) | sea and sun: bandanas in Brendan's and May's way, visors, sailors' stripes, light skirts, Mt. Chimney's ash-grey | **30** | 6 | 8 | 16 | — | 1,500–7,000 | 6 badges (Lilycove) |
| 5 | **Unova** | Shopping Mall Nine, **"3F · Unova Style"**, up a second escalator from the shop floor (an `exits` tile) | Castelia's business street and Nimbasa's stage: visors, fedoras, buttoned coats, hoodies, boots | **30** | 8 | 8 | 14 | — | 2,000–9,000 | 7 badges (Route 9) |
| 6 | **Kalos** | Lumiose's boutique, **an upper floor "Couture"** up stairs from the shop (`LumioseCity_Boutique_2F`) | high fashion: long skirts, short tailored coats, fedoras and wide hats, the boulevards' cream, navy and rouge | **30** | 8 | 8 | 14 | — | 3,000–15,000 | 1 badge (Lumiose) |
| 7 | **Alola** | Royal Avenue's Megamart, **"2F · Island Wear"** up stairs | resort and ring: sun hats, visors, bandanas, totes and waist bags, lagoon and hibiscus colours | **24** | 4 | 8 | 12 | — | 900–4,000 | Akala, after Lana's trial (Royal Avenue) |
| 8 | **Galar** | Motostoke's boutique, the standing building given its door: **"Motostoke Outfitters"**, one floor | knitwear and the stadiums: beanies, buttoned coats, striped "kit" tees in each gym's colours, and a **flat cap** (X) | **30** | 6 | 6 | 12 | 6 | 1,500–8,000 | Motostoke (GL4) |
| 9 | **Paldea** | Mesagoza's "boutique by the plaza" given its door: **"Mesagoza Boutique"**, one floor | the academy and the plaza: the **uniform shirt with its tie** in four seasons and two more (X), knit vests, wide hats, shoulder bags | **30** | 6 | 6 | 12 | 6 | 2,000–8,000 | the academy's welcome (PD1) |
| 10 | **Hisui** | Jubilife Village's **clothier**, its locked door opened (`JubilifeVillage_Clothier`) | the village's own: robes, headbands, straw wide hats, bandanas, plain trousers; nothing modern | **18** | — | 12 | 6 | — | 500–3,000 | Z17's welcome |
| 11 | **Carpathia** | the capital's department store on its boulevard, **built with plan 46 · CP24** (a floor of its own in the region's grand store) | the country's dress: the embroidered blouse (*ie*, X), the fur hat (*căciulă*, X), sheepskin vests (*cojoc*, the kit's `Vest`), beanies, and winter coats | **36** | 6 | 6 | 12 | 12 | 2,000–10,000 | the capital (CP24) |
| | | | **Total** | **324** | **70** | **86** | **144** | **24** | | |

**What the four X shapes are** (each a new piece in both kits and both routes, approved from a board first, as WD4's):
the **flat cap** (Galar; a hat kind, `Headwear.FlatCap`), the **uniform tie** (Paldea; a top's field that adds a tie
piece, as `extra.Scarf` hangs a scarf), the **embroidered blouse** (Carpathia; a pattern of the top's accent zone in
bands at the collar, cuffs and shoulders, beside today's `stripes`), the **fur hat** (Carpathia; a hat kind,
`Headwear.FurHat`). With decision 5's six that makes **ten new modelled shapes**; plus **four new fields** (`jacket`,
`buttoned`, `vest`, `robe`) for shapes the kits already have.

**Sample rows**, to show the scale of one floor (Sinnoh's 36, by slot; N 12, K 8, R 16):
- Hats (7): beanie ×3 (Snowpoint white, Coronet grey, cobalt) and a visor (marsh green), K; cap ×3 in colours
  Jubilife doesn't sell, R. No berets: Jubilife has them.
- Tops (12): hoodie with hood ×3 and parka ×3, N; buttoned contest coat ×2 and vest ×2, K; tee ×2 (a meteorite print's
  colours, Floaroma's yellow), R.
- Bottoms (7): trousers ×3, skirt ×2, shorts ×2, R (the long skirt is Kalos's and Hoenn's).
- Shoes (6): tall boots ×3, N; sneakers ×3, R.
- Bags (4): shoulder bag ×3, N; backpack ×1, R.
The names, descriptions and colours of every row are the building session's, written in the manner of today's rows.

## 4. Totals and sessions

- **324 new rows** (288 for the ten floors of the regions built; 36 for Carpathia's, with CP24), on top of today's 39:
  about **363 garments**, nine times today's. **About 110 designs** (a shape in a region's look), each in 2 to 6
  colourways.
- **Ten new modelled shapes** (six of decision 5, four regional), **four new fields** for kit shapes, the rest data.
- **Eleven new shop rooms** (ten built by plan 47, Carpathia's by CP24), **four lifts' questions regrouped**
  (Veilstone's and Goldenrod's already ask twice; Celadon's and Lilycove's start to), **two open-world doors** (Motostoke, Mesagoza),
  **one locked door opened** (Hisui).

| Session | What | Size | Marks |
|---|---|---|---|
| **WD4** (as planned, grown by this list) | decision 5's six shapes in both C# kits and both Blender scripts; the four fields (`jacket`, `buttoned`, `vest`, `robe`) and `Dressed`'s lines; the first rows that use them | 2 sessions | H |
| **WD7a** · the grid stores | Veilstone, Celadon, Goldenrod, Lilycove: four rooms, the lifts' questions split, the signs, `clothes.json`'s four stocks (126 rows); **the home wardrobe sorted** (question 8) | 1–2 sessions | H (no new script command: `wardrobe "<key>"` is enough) |
| **WD7b** · the drawn and the mall | Mall Nine's `exits` and room, Lumiose's upper floor, Royal Avenue's 2F, Hisui's clothier (102 rows) | 1 session | H |
| **WD7c** · the open regions and the regional shapes | Motostoke's and Mesagoza's doors and rooms (60 rows), and the flat cap and the uniform tie modelled (board first) | 1–2 sessions | H, U |
| *(plan 46 · CP24)* | Carpathia's store and its 36 rows, the embroidered blouse and the fur hat modelled (board first) | inside CP24 | H, U |

**About 5 to 7 sessions** in plan 47 (WD4's two and WD7's three to five), and Carpathia's part inside CP24. Beside
them, 17 · N6 for the rows' Romanian once question 10 is answered.

**Where it slots in.** The **R** and **K-hat** rows draw with today's code in both kits, so WD7a and WD7b **need
nothing of WD2 or WD3** and can come first; their **N** and **K-top** rows wait for WD4 (each floor can open with the
rows that draw and gain the rest when WD4 lands, a line of `clothes.json` each). WD5 (the grown player) must cover the
new shapes before Kalos's, Alola's, Galar's, Paldea's and Carpathia's floors sell them (every region from Kalos on
dresses the player in the grown kit, plan 30); the K hats and fields are in the grown file already. **One of plan 30,
plan 43 and plan 47 touches `CharacterModels` at a time** (plan 47, "Needs and gives").

## 5. Things to keep

- **Ids and saves**: new rows only; no garment id changes, no `SaveData` version raised (a wardrobe owns ids it reads
  back; an id the data doesn't know is left off, `ClothingDatabase.Get`).
- **Veilstone's roof keeps its file name** (`VeilstoneStore5F`) so a save standing on it loads; the new floor is a new
  file. Its lift stop is **value 7**, kept apart from the order as Goldenrod's basement is, so no save's
  `VAR_VEILSTONE_STORE_LIFT_FLOOR` changes meaning. Celadon's (6), Lilycove's (6) and Goldenrod's (8) new stops sit
  above the others.
- **Imported chunks untouched**; every new floor is a room of ours, named so (`Data/maps`, "a room of ours" in
  `RegionDatabase.cs`'s comment, as the training halls and Lockwork's cells are).
- **Our own clothes and words**: no garment of a particular game copied (no "Boutique Couture", no Thrifty Megamart
  stock, no academy crest); the floors' names above are placeholders for the user.

## 6. Questions for the user (each with its default)

1. **The list itself.** Ten floors (eleven with Carpathia) in the stores of section 3? *Default: yes, as listed.*
2. **How many.** About 30 rows a floor, 324 in all (about 110 designs)? Or fewer and dearer (about 20 a floor, 220 in
   all) or more (about 45 a floor, 480)? *Default: about 30 a floor, 324.*
3. **Unova's floor.** Shopping Mall Nine (the region's own mall, but met only after the seventh badge) or a store of
   ours on Castelia's avenue (the third badge's city, `Data/maps/CasteliaCity.json` is ours, as Lockwork's tower off
   it is)? *Default: Mall Nine, as the user's note says "malls".*
4. **Kanto twice.** Johto's world holds HeartGold's own Celadon store as well as Kanto's FireRed one. Give the clothing
   floor to Kanto's alone, or to both (the same stock, a second room)? *Default: Kanto's alone; HeartGold's lift stays
   as it is.*
5. **Galar's store.** Motostoke's boutique (met early, the first gym city) or Wyndon's (the capital, met at the Cup)?
   *Default: Motostoke; Wyndon's door stays shut, a possible later floor.*
6. **A dyer on every floor**, so any garment can be dyed in every region? *Default: yes (one script line each; the
   counter needs no boutique of its own). The salon's chair stays at Jubilife (and its room owed by plan 43 · LF3).*
7. **When each floor opens.** With its store (on the first arrival), or behind a later point of the story? *Default:
   with its store; no new flag.*
8. **The home wardrobe with 360 garments.** Today it lists every owned garment of a slot in the file's order, eight
   rows at a time. Sort it by where it was bought (the region's floor a heading) and let a shoulder button jump
   between regions? *Default: yes, in WD7a; nothing of a save moves.*
9. **The four regional shapes** (flat cap, uniform tie, embroidered blouse, fur hat): build them, each from a board
   first, or keep to decision 5's six and recolours alone? *Default: build them, board first.*
10. **Words.** Garment names and descriptions are English only today. Give them Romanian with the rest (a
    `names.clothes.json` per language, read as `names.moves.json` is, plan 17 · N6), 363 names and descriptions?
    *Default: yes, by a 17 · N6 session after WD7a, the floors' people's lines in Romanian as each floor is built.*
11. **Anywhere else?** The hub (Wayhaven, plan 41), Slateport's market, Wyndon's door, a folk-wear stall in
    Carpathia's plateau-town market (CP20), or the Sevii Islands? *Default: none for now.*
