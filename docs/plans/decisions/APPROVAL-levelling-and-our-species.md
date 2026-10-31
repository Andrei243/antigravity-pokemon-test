# For your approval: how the world is levelled, and how our 400 species enter it

*Written 2026-10-26 (wave 62). Two proposals, one sheet. Nothing is built yet. Answer by number ("L1 yes, L2 default, S5 b…"); anything you leave is taken at its **default**. The full documents are [W4b-levelling-the-world.md](W4b-levelling-the-world.md) (levelling) and [D15-species-of-ours-in-the-world.md](D15-species-of-ours-in-the-world.md) (species). Every number in them was counted by script against the repository's data.*

The two touch each other in one place, **trainers' teams**, so decide them together: Part 1 says what level a trainer's Pokémon is, Part 2 says whether it is also *evolved into one of ours*.

---

## What is wrong today (in four lines)

1. A later region is levelled by a straight line per region (Kanto 62→104, Johto 104→136, Hoenn 136→162, Unova 162→185), but only the **level** moves. A Youngster's Rattata becomes a level-105 **Rattata**. Unevolved Pokémon past an evolution: Kanto 766 of 1,650, Johto 773 of 1,776, Hoenn 864 of 1,828, Unova 564 of 1,808 (wild slots likewise).
2. The post-game after each Champion is only the line carried on, not designed (Johto's Red lands at 161, level with Hoenn's Champion).
3. Players drift apart: the typical one ends 5% under Kanto's Champion and 12% under Unova's; one who fights everything ends 14–18% over.
4. **None of our 400 species is met anywhere**: not in the wild, not on any trainer. Sinnoh's wild tops out at 55 and trainers at 85, so Sinnoh's own species of ours (evolving at 101–120) can never appear by level alone.

---

## Part 1 · Levelling everyone (W4b)

### The three designs

| | **A · finish the climb** | **B · refit to the typical player** | **C · keep the climb, change EXP in a crossed region** *(recommended)* |
|---|---|---|---|
| Idea | Add a post-game point per region, trainer stages, a level-cap table per region | Lower every Champion (Kanto 91, Johto 111, Hoenn 128, Unova 142) | Keep the climb; foes of a region you crossed into give **×1.5 EXP**, and EXP stops at **110% of the next milestone's level** (never lowers a level) |
| Spread between players | stays | shrinks | held to **−4% … +10%** of every Champion |
| Cost | 2 sessions | 2 + re-approving most of the 400 evolution levels and the 72 moves | 3 sessions |
| Moves goldens | `levels-crossed.txt` | many | `levels-crossed.txt` (new) |

### Questions

**L1 · Which design?** *Default: **C**.* (A is cheaper and leaves the spread; B lowers all Champions and re-opens the 400 evolutions and 72 moves you already approved.)

**L2 · How high is each post-game?** *Default:* `min(1.15 × Champion, next Champion − 2, 260)` → **Kanto 120, Johto 156, Hoenn 183, Unova 203**. (The original games put theirs 19–76% over their Champions.)

**L3 · Do trainers' Pokémon evolve as they are levelled up?** *Default:* every trainer's Pokémon takes the **games' own stages**; only ordinary trainers (not Leaders, Elite Four, Champions, rivals or bosses) go **on into species of ours**. Named trainers keep their games' last stage so their teams stay theirs. *(This is the same decision as S2/S3 below, answer once.)*

**L4 · Do wild Pokémon evolve with their level?** *Default: **no*** (so every regional Pokédex stays completable; a catch evolves as it levels).

**L5 · Ceiling and bonus: every difficulty, or Normal only?** *Default:* **every difficulty**, stacking with Easy's three halves; a Nuzlocke/level-cap run's own lower cap wins.

**L6 · Does Challenge grow by share past 100?** *Default: **yes**, in a crossed region only*: +6.5% Chiefs, +5% Bosses, +2% everyone (Sinnoh keeps +4/+3/+1).

**L7 · The later regions (Kalos…Paldea) and the cap of 260.** *Default:* keep the study's Champions and the post-game rule (Paldea's post-game is then only 2 levels long) and **settle it in plan 23**. Alternatives: lower Champions from Galar on, or a cap above 260.

**L8 · A later region's starter and gifts.** *Default:* given at the mapped level and evolving over their first level-ups, as today. Alternative: give the stage the level allows.

**L9 · Going back to an earlier region by harbour.** *Default:* its levels stay as first met, but its **League and phone/Vs. Seeker rematches** use the furthest Hall of Fame entered, mapped by that region's post-game pair.

**L10 · A game begun outside Sinnoh (`--region`, a developer switch).** *Default: leave it* (every game the title starts begins in Sinnoh).

---

## Part 2 · Putting our species in the world (D15)

352 of the 400 are placeable (48 legendary/mythical sets stay out): 205 from the built regions' batches, 147 from Kalos–Paldea (those regions aren't built).

### The designs

| | What | Sessions | Shows |
|---|---|---|---|
| **A · Grown as they are met** | A trainer's Pokémon whose mapped level reaches an evolution of ours *becomes it*; a wild forebear does so 1 time in 4, chosen by its personality (no new random draw). No data. | 1 | 203 of the 205, but almost nothing in Sinnoh or Kanto |
| **B · A rare slot of their own** | New overlay key `ours`: land slots 10 and 11 (1% each), surf slot 4, Super Rod slot 4, at their own levels, **after the region's Hall of Fame** (Sinnoh's after Kanto's). Room enough in every region. | 1 + ~3 | each in its own region |
| **C · The deep places** | One post-game place per region has its table swapped for ours behind a flag | 1–2 | five places |
| **T · Teams beyond the League** | 26 Elite Four/Champion teams (144 Pokémon) as `<id>_beyond` | 1 | the Leagues |

### Questions

**S1 · Which designs?** *Default: **A and B**, T later.* Alternatives: A alone; B alone; C instead of B; T too now.

**S2 · A (trainers): which evolutions count?** *Default: **every one**, the condition taken as met* (Johto 498, Hoenn 603, Unova 733 Pokémon). Alternative: by level alone (144 / 70 / 65), so an item/move/partner evolution keeps the forebear.

**S3 · A: do named trainers (Leaders, Elite Four, Champions, rivals, bosses) grow too?** *Default: **they grow like everyone** (Johto 106, Hoenn 171, Unova 146 of theirs).* Alternative: they keep their games' teams. ***Conflicts with L3's default** (L3 says named trainers keep their stage): pick one answer for both. My suggestion: take L3's default and answer **S3 = keep** if you want a Leader's team to stay theirs, or answer **L3 = everyone grows** if you want S3's default.*

**S4 · A: the wild share.** *Default: one in four.* Alternatives: every one past its level; one in ten; none (A for trainers only).

**S5 · B: how rare?** *Default: slots 10 & 11 on land, water slot 4, Super Rod slot 4 (1 in 100 each).* Alternative: slots 8 & 9 (4 in 100) outside Sinnoh, where nothing else uses them.

**S6 · B: when do they start to appear?** *Default: the region's own Hall of Fame; for Sinnoh, **Kanto's** Hall of Fame* (the team comes back by harbour at 100+). Alternatives: Sinnoh's own for Sinnoh; also require the National Pokédex.

**S7 · B: at what level?** *Default: the evolution level to four above, never mapped.* Alternative: the slot's own level mapped, raised to the evolution level where it is lower (a gentler Kanto).

**S8 · The Pokédex area page.** *Default: show where B places them.* Alternative: hide them, as the D12a `national` slots are.

**S9 · Kalos–Paldea's 147.** *Default: wait for their regions* (plan 23), each placing its batch by B's rule. Alternative: place them now, by habitat, in the five built regions' post-game (at 120–256, far above anything there).

**S10 · T, if chosen: when is a "beyond" team ready?** *Default: the region's Hall of Fame and a team Pokémon at or above the batch's lowest evolution level.* Alternatives: the Hall of Fame alone; every built region's.

---

## The shortest way to answer

Reply "**all defaults**" and this is what is built, in order:

1. **W4b-1** the ladder's data (post-game points, per-region level caps) → **W4b-2** stages (ordinary trainers evolve, named trainers keep their stage, Masters follow the player) → **W4b-3** crossed-region EXP ×1.5 with the 110% ceiling.
2. **D15-A** (grown as they are met; trainers every evolution, wild 1 in 4) → **D15-B** the `ours` slots, then three placement sessions (Sinnoh+Kanto, Johto+Hoenn, Unova).
3. T (the Leagues' "beyond" teams) later.

About eight sessions in all, none of which touches the sculpts or the battle goldens (`levels.txt` gets an `ours` line and a new `levels-crossed.txt`).

*Note for L3/S3: with all defaults taken literally the two disagree (L3 keeps named trainers' stages, S3 grows them). If you say "all defaults", I will read it as **named trainers keep their games' last stage** (L3), and S3 = keep.*

---

## Approved by the user on 2026-10-26

- **L1 = C** (the climb kept; ×1.5 EXP in a crossed region; soft ceiling at 110% of the next milestone's mapped level).
- **S1 = A + B + T, T now** (growth as they are met; the rare `ours` slots; the Leagues' 26 `<id>_beyond` teams).
- **L3 / S3 = named trainers grow too** (Leaders, Elite Four, Champions, rivals and bosses go on into species of ours like everyone). This overrides L3's default; W4b-2's stage rule therefore has no tier exception for species of ours (Masters excepted: they follow the player).
- **Everything else at its default**: L2 post-game `min(1.15 × Champion, next Champion − 2, 260)` (Kanto 120, Johto 156, Hoenn 183, Unova 203); L4 wild Pokémon do not evolve with their level; L5 ceiling and bonus in every difficulty; L6 Challenge by share in a crossed region; L7 later regions settled in plan 23; L8 gifts at the mapped level; L9 rematches use the furthest Hall of Fame entered; L10 leave `--region` games alone; S2 every evolution counts, the condition taken as met; S4 wild one in four; S5 slots 10 and 11 land, water slot 4, Super Rod slot 4; S6 the region's own Hall of Fame (Sinnoh: Kanto's); S7 evolution level to four above, never mapped; S8 shown on the Pokédex's area page; S9 Kalos–Paldea's 147 wait for their regions; S10 a "beyond" team is ready at the region's Hall of Fame with a team Pokémon at or above the batch's lowest evolution level.

**Build order** (both touch `Trainer.Relevelled`, so one after the other): W4b-1 the ladder's data, then W4b-2 stages together with D15-A (growth), then W4b-3 crossed-region EXP, D15-B's engine session, D15-T, then D15-B's three placement sessions.
