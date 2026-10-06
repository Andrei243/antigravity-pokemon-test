using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using static PokemonPlatinumEngine.Battle.Sim.Ai.MoveEffectId;

namespace PokemonPlatinumEngine.Battle.Sim.Ai;

/// <summary>
/// The tag strategy of the AI script (<c>TagStrategy_Main</c> and the routines it reaches, in
/// <c>src/battle/trainer_ai/script.s</c>): how a trainer's Pokémon in a double battle weighs its moves with its partner
/// in mind, both those aimed at the foes and those aimed at the partner itself. The same questions in the same order,
/// the same rolls, the same scores.
/// </summary>
internal sealed partial class AiThinking
{
    /// <summary>The moves of a fixed or level-based amount (and the one-hit knockouts), which skip the matchup bonuses.</summary>
    private static readonly HashSet<MoveEffectId> TagFixedDamage = new()
    {
        OneHitKo, N40DamageFlat, LevelDamageFlat, RandomDamage1To150Level, N20DamageFlat
    };

    /// <summary>
    /// <c>TagStrategy_Main</c>: a move aimed at the partner goes to <see cref="TagStrategyPartner"/>. A damaging move
    /// that is resisted mostly loses 1 (2 against a double resistance) unless it would knock out or the target's partner
    /// is gone; then it is weighed against its partner's moves, and the moves with their own handling follow.
    /// </summary>
    private void TagStrategy()
    {
        if (TargetIsPartner)
        {
            TagStrategyPartner();
            return;
        }

        // A move whose damage isn't worked out goes straight to the moves with their own handling
        if (DamageScore(roll: false) == DamageRank.NoComparison)
        {
            TagStrategyCheckSpecialScoring();
            return;
        }

        if (!TagFixedDamage.Contains(Effect))
        {
            // TagStrategy_TryScoreMinus1 and _TryScoreMinus2
            if (Effectiveness() == Eff.Half) Resisted(-1);
            else if (Effectiveness() == Eff.Quarter) Resisted(-2);
        }
        TagStrategyScoreMove();

        void Resisted(int change)
        {
            if (Kills(roll: false) == true) return;
            // The target's partner at 0% (gone, or under 1%) means the target stands alone
            if (HpPercent(Who.DefenderPartner) == 0) return;
            if (RandomBelow(64)) return;
            Score(change);
        }
    }

    /// <summary>
    /// <c>TagStrategy_ScoreMove</c>: a move that hits at least as hard as any of the user's and its partner's
    /// (Explosion's effect aside) gets +1 half the time, or about four times in five for a move of priority's effect.
    /// </summary>
    private void TagStrategyScoreMove()
    {
        if (HighestWithPartner(roll: false) != DamageRank.Highest)
        {
            TagStrategyCheckBeforeScoring();
            return;
        }
        if (Effect == HalveDefense)
        {
            TagStrategyCheckSpecialScoring();
            return;
        }

        // TagStrategy_TryScorePlus1 is the same with the odds of a priority move. A move that misses out on its +1 goes on
        // to the super-effective bonus; one that gets it skips that bonus (the original's flow)
        if (RandomBelow(Effect == Priority1 ? 50 : 128))
        {
            TagStrategyCheckBeforeScoring();
            return;
        }
        Score(1);
        TagStrategyCheckSpecialScoring();
    }

    /// <summary>
    /// <c>TagStrategy_CheckBeforeScoring</c>: a super-effective hit (not one of a fixed amount) gets +1 about three
    /// times in five, a four-times hit three times in four.
    /// </summary>
    private void TagStrategyCheckBeforeScoring()
    {
        if (!TagFixedDamage.Contains(Effect))
        {
            // TagStrategy_TryPrioritizingDoubleEffective and _TryPrioritizingQuadEffective
            if (Effectiveness() == Eff.Double)
            {
                if (!RandomBelow(100)) Score(1);
            }
            else if (Effectiveness() == Eff.Quadruple)
            {
                if (!RandomBelow(64)) Score(1);
            }
        }
        TagStrategyCheckSpecialScoring();
    }

    /// <summary>
    /// <c>TagStrategy_CheckSpecialScoring</c>: the moves with a routine of their own, then the Electric, Fire and Water
    /// moves by their type, then any move while the partner knows Helping Hand.
    /// </summary>
    private void TagStrategyCheckSpecialScoring()
    {
        switch (Move.Name)
        {
            case "Skill Swap": TagStrategySkillSwap(); return;
            case "Earthquake" or "Magnitude": TagStrategyEarthquake(); return;
            case "Future Sight" or "Doom Desire": TagStrategyFutureSight(); return;
            case "Rain Dance": TagStrategyRainDance(); return;
            case "Sunny Day": TagStrategySunnyDay(); return;
            case "Hail": TagStrategyHail(); return;
            case "Sandstorm": TagStrategySandstorm(); return;
            case "Gravity": TagStrategyGravity(); return;
            case "Trick Room": TagStrategyTrickRoom(); return;
            case "Follow Me": TagStrategyFollowMe(); return;
        }

        var type = MoveType;
        if (type == PokemonType.Electric)
        {
            TagStrategyCheckElectricMove();
            return;
        }
        if (type == PokemonType.Fire)
        {
            TagStrategyCheckFireMove();
            return;
        }
        if (type == PokemonType.Water)
        {
            TagStrategyCheckWaterMove();
            return;
        }
        if (Knows(Who.AttackerPartner, "Helping Hand") == true) TagStrategyPartnerKnowsHelpingHand();
    }

    /// <summary>
    /// <c>TagStrategy_RainDance</c>: +2 for the user and +2 for its partner each, if it has Dry Skin, or Hydration with
    /// a condition to wash off.
    /// </summary>
    private void TagStrategyRainDance()
    {
        Rain(Who.Attacker, AbilityOf(Who.Attacker));

        string? theirs = CheckAbility(Who.AttackerPartner, "Hydration") == Knowledge.Have ? "Hydration"
            : CheckAbility(Who.AttackerPartner, "Dry Skin") == Knowledge.Have ? "Dry Skin"
            : null;
        Rain(Who.AttackerPartner, theirs);

        void Rain(Who who, string? ability)
        {
            if ((ability == "Hydration" && HasStatus(who, Cond.Any)) || ability == "Dry Skin") Score(2);
        }
    }

    /// <summary>
    /// <c>TagStrategy_SunnyDay</c>: for the user and for its partner each, +2 for Flower Gift or a healthy Leaf Guard
    /// (no condition, 30% HP or more), −2 for Dry Skin, and for Solar Power +1 at half HP or more and then a half
    /// chance of −2.
    /// </summary>
    private void TagStrategySunnyDay()
    {
        Sun(Who.Attacker, AbilityOf(Who.Attacker));

        string? theirs = CheckAbility(Who.AttackerPartner, "Leaf Guard") == Knowledge.Have ? "Leaf Guard"
            : CheckAbility(Who.AttackerPartner, "Flower Gift") == Knowledge.Have ? "Flower Gift"
            : CheckAbility(Who.AttackerPartner, "Dry Skin") == Knowledge.Have ? "Dry Skin"
            : CheckAbility(Who.AttackerPartner, "Solar Power") == Knowledge.Have ? "Solar Power"
            : null;
        Sun(Who.AttackerPartner, theirs);

        void Sun(Who who, string? ability)
        {
            switch (ability)
            {
                case "Leaf Guard":
                    if (!HasStatus(who, Cond.Any) && HpPercent(who) >= 30) Score(2);
                    break;
                case "Flower Gift":
                    Score(2);
                    break;
                case "Dry Skin":
                    Score(-2);
                    break;
                case "Solar Power":
                    // The +1 falls through into the half chance of −2 all the same
                    if (HpPercent(who) >= 50) Score(1);
                    if (!RandomBelow(128)) Score(-2);
                    break;
            }
        }
    }

    /// <summary><c>TagStrategy_Hail</c>: +2 for the user and +2 for its partner each, if it has Ice Body or Snow Cloak or knows Blizzard.</summary>
    private void TagStrategyHail()
    {
        if ((AbilityOf(Who.Attacker) is "Ice Body" or "Snow Cloak") || Knows(Who.Attacker, "Blizzard") == true) Score(2);

        if (CheckAbility(Who.AttackerPartner, "Ice Body") == Knowledge.Have
            || CheckAbility(Who.AttackerPartner, "Snow Cloak") == Knowledge.Have
            || Knows(Who.AttackerPartner, "Blizzard") == true)
            Score(2);
    }

    /// <summary><c>TagStrategy_Sandstorm</c>: +2 for the user and +2 for its partner each, if it has Sand Veil or is part Rock.</summary>
    private void TagStrategySandstorm()
    {
        if (AbilityOf(Who.Attacker) == "Sand Veil" || Type1(Who.Attacker) == PokemonType.Rock || Type2(Who.Attacker) == PokemonType.Rock)
            Score(2);

        if (CheckAbility(Who.AttackerPartner, "Sand Veil") == Knowledge.Have
            || Type1(Who.AttackerPartner) == PokemonType.Rock
            || Type2(Who.AttackerPartner) == PokemonType.Rock)
            Score(2);
    }

    /// <summary>
    /// <c>TagStrategy_Gravity</c>: −30 while Gravity is up already; otherwise −5 for each of its own side that floats
    /// (Levitate, Flying, Magnet Rise) and, three times in four, +3 for each foe that does.
    /// </summary>
    private void TagStrategyGravity()
    {
        if (Gravity)
        {
            // TagStrategy_PartnerScoreMinus30
            Score(-30);
            return;
        }

        if (Floats(Who.Attacker)) Score(-5);
        if (Floats(Who.AttackerPartner)) Score(-5);
        if (Floats(Who.Defender) && !RandomBelow(64)) Score(3);
        if (Floats(Who.DefenderPartner) && !RandomBelow(64)) Score(3);

        bool Floats(Who who) =>
            CheckAbility(who, "Levitate") == Knowledge.Have || HasType(who, PokemonType.Flying) || Has(who, MonFx.MagnetRise);
    }

    /// <summary>
    /// <c>TagStrategy_TrickRoom</c>: −30 once either side is down to one Pokémon, or when the user and its partner are
    /// the two fastest; +5 three times in four when they are the two slowest; −5 otherwise.
    /// </summary>
    private void TagStrategyTrickRoom()
    {
        if (HpPercent(Who.AttackerPartner) == 0 || HpPercent(Who.DefenderPartner) == 0 || HpPercent(Who.Defender) == 0)
        {
            Score(-30);
            return;
        }

        switch (SpeedRank(Who.Attacker))
        {
            case 0:
                // The partner can't be first as well, but the original asks
                if (SpeedRank(Who.AttackerPartner) is 1 or 0)
                {
                    Score(-30);
                    return;
                }
                Score(-5);
                return;
            case 1:
                if (SpeedRank(Who.AttackerPartner) == 0)
                {
                    Score(-30);
                    return;
                }
                Score(-5);
                return;
            case 2:
                if (SpeedRank(Who.AttackerPartner) != 3 || RandomBelow(64))
                {
                    Score(-5);
                    return;
                }
                Score(5);
                return;
            case 3:
                if (SpeedRank(Who.AttackerPartner) != 2 || RandomBelow(64))
                {
                    Score(-5);
                    return;
                }
                Score(5);
                return;
        }
    }

    /// <summary>
    /// <c>TagStrategy_FollowMe</c>: three times in four, a change by the HP of the user and of its partner: the
    /// healthier the user and the weaker the partner, the better (from −2 to +3); under 30% HP itself, −5.
    /// </summary>
    private void TagStrategyFollowMe()
    {
        int change;
        if (HpPercent(Who.Attacker) > 90) change = ByPartner(-1, 1, 2, 3);
        else if (HpPercent(Who.Attacker) > 50) change = ByPartner(-2, -1, 1, 2);
        else if (HpPercent(Who.Attacker) > 30) change = ByPartner(-2, -2, 1, 2);
        else change = -5;

        if (RandomBelow(64)) return;
        Score(change);

        // The partner above 90%, above 50%, above 30%, or lower
        int ByPartner(int high, int medium, int low, int lowest) =>
            HpPercent(Who.AttackerPartner) > 90 ? high
            : HpPercent(Who.AttackerPartner) > 50 ? medium
            : HpPercent(Who.AttackerPartner) > 30 ? low
            : lowest;
    }

    /// <summary>
    /// <c>TagStrategy_PartnerKnowsHelpingHand</c>: with a partner that knows Helping Hand, a move whose damage is
    /// worked out (not one of a fixed amount) gets +1.
    /// </summary>
    private void TagStrategyPartnerKnowsHelpingHand()
    {
        if (TagFixedDamage.Contains(Effect)) return;
        if (DamageScore(roll: false) != DamageRank.NoComparison) Score(1);
    }

    /// <summary>
    /// <c>TagStrategy_Unused_1</c> and <c>_Unused_2</c>, which nothing in the script reaches: for a user with a
    /// condition, −5 for a move whose damage isn't worked out, else +1, and +2 more for its strongest.
    /// </summary>
    private void TagStrategyUnused()
    {
        if (!HasStatus(Who.Attacker, Cond.Any)) return;
        var rank = DamageScore(roll: false);
        if (rank == DamageRank.NoComparison)
        {
            Score(-5);
            return;
        }
        Score(1);
        if (rank == DamageRank.Highest) Score(2);
    }

    /// <summary>
    /// <c>TagStrategy_Earthquake</c>: Earthquake and Magnitude get +2 when the partner floats above them, −10 when it is
    /// Fire, Electric, Poison or Rock, and −3 otherwise.
    /// </summary>
    private void TagStrategyEarthquake()
    {
        // Nothing asks whether the partner is there: a lone user's Earthquake gets the −3 too
        if (Has(Who.AttackerPartner, MonFx.MagnetRise)
            || CheckAbility(Who.AttackerPartner, "Levitate") == Knowledge.Have
            || HasType(Who.AttackerPartner, PokemonType.Flying))
        {
            Score(2);
            return;
        }
        if (HasType(Who.AttackerPartner, PokemonType.Fire)
            || HasType(Who.AttackerPartner, PokemonType.Electric)
            || HasType(Who.AttackerPartner, PokemonType.Poison)
            || HasType(Who.AttackerPartner, PokemonType.Rock))
        {
            Score(-10);
            return;
        }
        Score(-3);
    }

    /// <summary>
    /// <c>TagStrategy_FutureSight</c>: Future Sight and Doom Desire get −3 when the partner knows one of them too and
    /// moves before the user; with the user ahead, half the time the order is looked at again, and a speed tie
    /// decided the other way this time costs 3 all the same.
    /// </summary>
    private void TagStrategyFutureSight()
    {
        if (HpPercent(Who.AttackerPartner) == 0) return;
        if (Knows(Who.AttackerPartner, "Future Sight") != true && Knows(Who.AttackerPartner, "Doom Desire") != true) return;

        // The partner's place in the order is asked again after the roll, so a speed tie is decided afresh
        switch (SpeedRank(Who.Attacker))
        {
            case 3:
                Score(-3);
                return;
            case 2:
                if (SpeedRank(Who.AttackerPartner) is 0 or 1)
                {
                    Score(-3);
                    return;
                }
                if (RandomBelow(128)) return;
                if (SpeedRank(Who.AttackerPartner) == 2) Score(-3);
                return;
            case 1:
                if (SpeedRank(Who.AttackerPartner) == 0)
                {
                    Score(-3);
                    return;
                }
                if (RandomBelow(128)) return;
                if (SpeedRank(Who.AttackerPartner) == 1) Score(-3);
                return;
            case 0:
                if (RandomBelow(128)) return;
                if (SpeedRank(Who.AttackerPartner) == 0) Score(-3);
                return;
        }
    }

    /// <summary>
    /// <c>TagStrategy_SkillSwap</c>: aimed at a foe, +5 to be rid of the user's own Truant, Slow Start, Stall or Klutz,
    /// else +2 to take the foe's Shadow Tag, Pure Power, Huge Power, Mold Breaker, Solid Rock, Filter or Flower Gift.
    /// </summary>
    private void TagStrategySkillSwap()
    {
        if (AbilityOf(Who.Attacker) is "Truant" or "Slow Start" or "Stall" or "Klutz")
        {
            Score(5);
            return;
        }
        if (AbilityOf(Who.Defender) is "Shadow Tag" or "Pure Power" or "Huge Power" or "Mold Breaker" or "Solid Rock" or "Filter" or "Flower Gift")
            Score(2);
    }

    /// <summary>
    /// <c>TagStrategy_CheckElectricMove</c>: Discharge is weighed by what it does to the partner; any other Electric
    /// move loses 1 when the target's partner has Lightning Rod (8 more if that partner is Ground) and 10 when the
    /// user's own partner has it.
    /// </summary>
    private void TagStrategyCheckElectricMove()
    {
        if (MoveIs("Discharge"))
        {
            TagStrategySpreadElectricMove();
            return;
        }

        // TagStrategy_TargetProtectedByLightningRod
        if (CheckAbility(Who.DefenderPartner, "Lightning Rod") == Knowledge.Have)
        {
            Score(-1);
            if (HasType(Who.DefenderPartner, PokemonType.Ground)) Score(-8);
        }

        // TagStrategy_PartnerHasLightningRod (its second look for Discharge can't find it by now)
        if (CheckAbility(Who.AttackerPartner, "Lightning Rod") == Knowledge.Have) Score(-10);
    }

    /// <summary>
    /// <c>TagStrategy_SpreadElectricMove</c>: Discharge gets +3 when the partner has Motor Drive or Volt Absorb, −10 when
    /// it is Water or Flying, +3 when it is Ground, and −3 otherwise.
    /// </summary>
    private void TagStrategySpreadElectricMove()
    {
        if (CheckAbility(Who.AttackerPartner, "Motor Drive") == Knowledge.Have
            || CheckAbility(Who.AttackerPartner, "Volt Absorb") == Knowledge.Have)
        {
            Score(3);
            return;
        }
        if (HasType(Who.AttackerPartner, PokemonType.Water) || HasType(Who.AttackerPartner, PokemonType.Flying))
        {
            Score(-10);
            return;
        }
        // The original's slip: Ground is asked after Water and Flying, so a Water/Ground or Flying/Ground partner,
        // which Discharge can't touch, still costs 10
        if (HasType(Who.AttackerPartner, PokemonType.Ground))
        {
            Score(3);
            return;
        }
        Score(-3);
    }

    /// <summary>
    /// <c>TagStrategy_CheckWaterMove</c>: Surf is weighed by what it does to the partner; any other Water move loses 1
    /// unless the target's partner surely lacks Storm Drain, and 10 when the user's own partner has it.
    /// </summary>
    private void TagStrategyCheckWaterMove()
    {
        if (MoveIs("Surf"))
        {
            TagStrategySpreadWaterMove();
            return;
        }

        // Only a sure "doesn't have" spares the −1: an ability the AI can't tell counts as Storm Drain
        if (CheckAbility(Who.DefenderPartner, "Storm Drain") != Knowledge.NotHave) Score(-1);

        // TagStrategy_CheckPartnerStormDrain (its second look for Surf can't find it by now)
        if (CheckAbility(Who.AttackerPartner, "Storm Drain") == Knowledge.Have) Score(-10);
    }

    /// <summary>
    /// <c>TagStrategy_SpreadWaterMove</c>: Surf gets +3 when the partner has Dry Skin or Water Absorb, −10 when it is
    /// Ground or Fire, and −3 otherwise.
    /// </summary>
    private void TagStrategySpreadWaterMove()
    {
        if (CheckAbility(Who.AttackerPartner, "Dry Skin") == Knowledge.Have
            || CheckAbility(Who.AttackerPartner, "Water Absorb") == Knowledge.Have)
        {
            Score(3);
            return;
        }
        // The original leaves Rock out of the partners Surf hurts most
        if (HasType(Who.AttackerPartner, PokemonType.Ground) || HasType(Who.AttackerPartner, PokemonType.Fire))
        {
            Score(-10);
            return;
        }
        Score(-3);
    }

    /// <summary>
    /// <c>TagStrategy_CheckFireMove</c>: a Fire move gets +1 while the user's Flash Fire is lit; Lava Plume is then
    /// weighed by what it does to the partner.
    /// </summary>
    private void TagStrategyCheckFireMove()
    {
        // TagStrategy_FlashFireScorePlus1
        if (FlashFire(Who.Attacker)) Score(1);

        // TagStrategy_CheckLavaPlume
        if (MoveIs("Lava Plume")) TagStrategySpreadFireMove();
    }

    /// <summary>
    /// <c>TagStrategy_SpreadFireMove</c>: Lava Plume gets −3 when the partner has Dry Skin, +3 when it has Flash Fire,
    /// −10 when it is Grass, Steel, Ice or Bug, and −3 otherwise.
    /// </summary>
    private void TagStrategySpreadFireMove()
    {
        if (CheckAbility(Who.AttackerPartner, "Dry Skin") == Knowledge.Have)
        {
            Score(-3);
            return;
        }
        if (CheckAbility(Who.AttackerPartner, "Flash Fire") == Knowledge.Have)
        {
            Score(3);
            return;
        }
        if (HasType(Who.AttackerPartner, PokemonType.Grass)
            || HasType(Who.AttackerPartner, PokemonType.Steel)
            || HasType(Who.AttackerPartner, PokemonType.Ice)
            || HasType(Who.AttackerPartner, PokemonType.Bug))
        {
            Score(-10);
            return;
        }
        Score(-3);
    }

    /// <summary>
    /// <c>TagStrategy_Partner</c>: a move aimed at the user's own partner. −30 if the partner is down; a damaging Fire,
    /// Electric or Water move only for a partner that soaks it up; a damaging Fling left as it is; any other damaging
    /// move −30; a status move by its own routine.
    /// </summary>
    private void TagStrategyPartner()
    {
        if (Fainted(Who.AttackerPartner))
        {
            // TagStrategy_PartnerScoreMinus30
            Score(-30);
            return;
        }
        if (DamageScore(roll: false) == DamageRank.NoComparison)
        {
            TagStrategyPartnerStatusMove();
            return;
        }

        var type = MoveType;
        if (type == PokemonType.Fire)
        {
            TagStrategyCheckPartnerFireAbsorption();
            return;
        }
        if (type == PokemonType.Electric)
        {
            TagStrategyCheckPartnerElectricAbsorption();
            return;
        }
        if (type == PokemonType.Water)
        {
            TagStrategyCheckPartnerWaterAbsorption();
            return;
        }
        // TagStrategy_PartnerTrick: nothing
        if (MoveIs("Fling")) return;

        // TagStrategy_ScoreMinus30
        Score(-30);
    }

    /// <summary><c>TagStrategy_CheckPartnerFireAbsorption</c>: +3 for a partner whose Flash Fire isn't lit yet; −30 for any other.</summary>
    private void TagStrategyCheckPartnerFireAbsorption()
    {
        // TagStrategy_CheckPartnerFlashFireActive
        if (CheckAbility(Who.AttackerPartner, "Flash Fire") == Knowledge.Have && !FlashFire(Who.AttackerPartner))
        {
            Score(3);
            return;
        }
        Score(-30);
    }

    /// <summary>
    /// <c>TagStrategy_CheckPartnerElectricAbsorption</c>: for a partner with Motor Drive, +3 three times in eight (−30
    /// instead if its Speed is as high as it goes); for one with Volt Absorb, by its HP; −30 for any other.
    /// </summary>
    private void TagStrategyCheckPartnerElectricAbsorption()
    {
        if (CheckAbility(Who.AttackerPartner, "Motor Drive") == Knowledge.Have)
        {
            // TagStrategy_CheckPartnerMotorDrive
            if (RandomBelow(160)) return;
            Score(Stage(Who.AttackerPartner, StatType.Speed) == 12 ? -30 : 3);
            return;
        }
        if (CheckAbility(Who.AttackerPartner, "Volt Absorb") == Knowledge.Have)
        {
            // TagStrategy_CheckPartnerVoltAbsorb
            TagPartnerHealedByMove();
            return;
        }
        Score(-30);
    }

    /// <summary><c>TagStrategy_CheckPartnerWaterAbsorption</c>: for a partner with Water Absorb or Dry Skin, by its HP; −30 for any other.</summary>
    private void TagStrategyCheckPartnerWaterAbsorption()
    {
        if (CheckAbility(Who.AttackerPartner, "Water Absorb") == Knowledge.Have
            || CheckAbility(Who.AttackerPartner, "Dry Skin") == Knowledge.Have)
        {
            // TagStrategy_PartnerWaterAbsorb
            TagPartnerHealedByMove();
            return;
        }
        Score(-30);
    }

    /// <summary>
    /// The two absorbing partners' scoring, which the original writes out twice (<c>TagStrategy_CheckPartnerVoltAbsorb</c>
    /// and <c>TagStrategy_PartnerWaterAbsorb</c>): −10 at full HP, nothing above 90%, and below that +3 a quarter, half
    /// or three quarters of the time the lower it is (above 75%, above 50%, or less).
    /// </summary>
    private void TagPartnerHealedByMove()
    {
        int hp = HpPercent(Who.AttackerPartner);
        if (hp == 100)
        {
            Score(-10);
            return;
        }
        if (hp > 90) return;
        if (RandomBelow(hp > 75 ? 64 : hp > 50 ? 128 : 192)) Score(3);
    }

    /// <summary><c>TagStrategy_PartnerStatusMove</c>: the status moves worth aiming at the partner, each by its own routine; −30 for any other.</summary>
    private void TagStrategyPartnerStatusMove()
    {
        if (MoveIs("Skill Swap"))
        {
            TagStrategyPartnerSkillSwap();
            return;
        }
        if (MoveIs("Will-O-Wisp"))
        {
            TagStrategyPartnerWillOWisp();
            return;
        }
        if (MoveIs("Thunder Wave"))
        {
            TagStrategyPartnerThunderWave();
            return;
        }
        if (Effect == StatusBadlyPoison || Effect == StatusPoison)
        {
            TagStrategyPartnerPoisonStatus();
            return;
        }
        if (MoveIs("Helping Hand"))
        {
            TagStrategyPartnerUsingHelpingHand();
            return;
        }
        if (MoveIs("Swagger"))
        {
            TagStrategyPartnerSwagger();
            return;
        }
        // TagStrategy_PartnerTrick: nothing
        if (MoveIs("Trick") || MoveIs("Switcheroo")) return;
        if (MoveIs("Gastro Acid"))
        {
            TagStrategyPartnerGastroAcid();
            return;
        }
        if (MoveIs("Acupressure"))
        {
            TagStrategyPartnerAcupressure();
            return;
        }
        Score(-30);
    }

    /// <summary>The partner's moves that miss often enough for Compound Eyes or No Guard to be worth giving it.</summary>
    private static readonly string[] TagInaccurateMoves =
    {
        "Fire Blast", "Thunder", "Cross Chop", "Hydro Pump", "Dynamic Punch", "Blizzard", "Zap Cannon", "Megahorn",
        "Focus Blast", "Gunk Shot", "Magma Storm", "Power Whip", "Seed Flare", "Head Smash"
    };

    /// <summary>
    /// <c>TagStrategy_PartnerSkillSwap</c>: +10 to take the partner's Truant or Slow Start; a user with Levitate gives it
    /// to an Electric partner without it (+1, +2 if pure Electric); a user with Compound Eyes or No Guard gives it to a
    /// partner with a move that misses often (+3); −30 otherwise.
    /// </summary>
    private void TagStrategyPartnerSkillSwap()
    {
        if (AbilityOf(Who.Defender) is "Truant" or "Slow Start")
        {
            Score(10);
            return;
        }

        if (AbilityOf(Who.Attacker) == "Levitate")
        {
            if (AbilityOf(Who.Defender) == "Levitate")
            {
                Score(-30);
                return;
            }
            if (Type1(Who.Defender) == PokemonType.Electric)
            {
                Score(1);
                if (Type2(Who.Defender) == PokemonType.Electric)
                {
                    Score(1);
                    return;
                }
            }
            // A partner Electric only in part has its +1 and goes on to the accuracy check below, which a user with
            // Levitate can't pass: −30 after all
        }

        // TagStrategy_PartnerSkillSwap_GiveAccuracyIncrease and _PartnerHasInaccurateMove
        if ((AbilityOf(Who.Attacker) is "Compound Eyes" or "No Guard")
            && TagInaccurateMoves.Any(name => Knows(Who.AttackerPartner, name) == true))
        {
            Score(3);
            return;
        }
        Score(-30);
    }

    /// <summary>
    /// <c>TagStrategy_PartnerWillOWisp</c>: a partner with Flash Fire is weighed as for a Fire move; one with Guts gets
    /// +5 if it has no condition, isn't Fire, holds no Flame Orb or Toxic Orb and has 81% HP or more; −30 otherwise.
    /// </summary>
    private void TagStrategyPartnerWillOWisp()
    {
        if (CheckAbility(Who.AttackerPartner, "Flash Fire") == Knowledge.Have)
        {
            TagStrategyCheckPartnerFireAbsorption();
            return;
        }

        if (CheckAbility(Who.AttackerPartner, "Guts") != Knowledge.Have
            || HasStatus(Who.AttackerPartner, Cond.Any)
            || Type1(Who.Defender) == PokemonType.Fire
            || Type2(Who.Defender) == PokemonType.Fire
            || Holds(Who.AttackerPartner, "Flame Orb")
            || Holds(Who.AttackerPartner, "Toxic Orb")
            || HpPercent(Who.AttackerPartner) < 81)
        {
            Score(-30);
            return;
        }
        Score(5);
    }

    /// <summary>
    /// <c>TagStrategy_PartnerThunderWave</c>: a partner that isn't Ground and has Motor Drive or Volt Absorb is weighed
    /// as for an Electric move; −30 for any other.
    /// </summary>
    private void TagStrategyPartnerThunderWave()
    {
        if (Type1(Who.Defender) == PokemonType.Ground || Type2(Who.Defender) == PokemonType.Ground)
        {
            Score(-30);
            return;
        }
        if (CheckAbility(Who.AttackerPartner, "Motor Drive") == Knowledge.Have
            || CheckAbility(Who.AttackerPartner, "Volt Absorb") == Knowledge.Have)
        {
            TagStrategyCheckPartnerElectricAbsorption();
            return;
        }
        Score(-30);
    }

    /// <summary>
    /// <c>TagStrategy_PartnerPoisonStatus</c>: a move that poisons gets +5 for a partner with Poison Heal that has no
    /// condition, holds no Toxic Orb and has 91% HP or less; −30 otherwise.
    /// </summary>
    private void TagStrategyPartnerPoisonStatus()
    {
        // The original's slips: it turns away a partner above 91% (where Will-O-Wisp's check wants the healthy ones),
        // and never turns away a Poison or Steel partner, which can't be poisoned
        if (CheckAbility(Who.AttackerPartner, "Poison Heal") != Knowledge.Have
            || HasStatus(Who.Defender, Cond.Any)
            || Holds(Who.AttackerPartner, "Toxic Orb")
            || HpPercent(Who.AttackerPartner) > 91)
        {
            Score(-30);
            return;
        }
        Score(5);
    }

    /// <summary>
    /// <c>TagStrategy_PartnerUsingHelpingHand</c>: −30 with no partner; with one above half HP or the fastest on the
    /// field, +2 three times in four and −1 otherwise.
    /// </summary>
    private void TagStrategyPartnerUsingHelpingHand()
    {
        if (HpPercent(Who.AttackerPartner) == 0)
        {
            Score(-30);
            return;
        }
        if (HpPercent(Who.AttackerPartner) > 50 || SpeedRank(Who.AttackerPartner) < 1)
        {
            // TagStrategy_PartnerUsingHelpingHand_TryScorePlus2
            if (RandomBelow(64))
            {
                Score(-1);
                return;
            }
            Score(2);
        }
    }

    /// <summary>
    /// <c>TagStrategy_PartnerSwagger</c>: for a partner holding a Persim Berry or a Lum Berry, +3 unless its Attack is
    /// up two stages already; −30 for any other. (Own Tempo isn't asked about.)
    /// </summary>
    private void TagStrategyPartnerSwagger()
    {
        if (!Holds(Who.Defender, "Persim Berry") && !Holds(Who.Defender, "Lum Berry"))
        {
            Score(-30);
            return;
        }
        // TagStrategy_PartnerSwagger_TryScorePlus3
        if (Stage(Who.Defender, StatType.Attack) > 7) return;
        Score(3);
    }

    /// <summary>
    /// <c>TagStrategy_PartnerGastroAcid</c>: −30 if the partner's ability is suppressed already; +5 to switch off its
    /// Truant or Slow Start.
    /// </summary>
    private void TagStrategyPartnerGastroAcid()
    {
        if (Has(Who.AttackerPartner, MonFx.AbilitySuppressed))
        {
            Score(-30);
            return;
        }
        if (CheckAbility(Who.AttackerPartner, "Truant") == Knowledge.Have
            || CheckAbility(Who.AttackerPartner, "Slow Start") == Knowledge.Have)
            Score(5);
    }

    /// <summary>The stats Acupressure's checks look at, in the original's order.</summary>
    private static readonly StatType[] TagAcupressureStats =
    {
        StatType.Attack, StatType.Defense, StatType.Speed, StatType.SpAttack, StatType.SpDefense, StatType.Evasion,
        StatType.Accuracy
    };

    /// <summary>
    /// <c>TagStrategy_PartnerAcupressure</c>: −10 for a partner with Simple that has a stat up three stages, −30 for one
    /// without that has a stat up six; then −1 at half HP or less, and otherwise +2 about two times in three above 90%
    /// HP, about one time in three below.
    /// </summary>
    private void TagStrategyPartnerAcupressure()
    {
        if (CheckAbility(Who.AttackerPartner, "Simple") == Knowledge.Have)
        {
            // TagStrategy_PartnerAcupressureSimple
            if (TagAcupressureStats.Any(stat => Stage(Who.AttackerPartner, stat) > 8))
            {
                Score(-10);
                return;
            }
        }
        else if (TagAcupressureStats.Any(stat => Stage(Who.AttackerPartner, stat) == 12))
        {
            Score(-30);
            return;
        }

        // TagStrategy_PartnerAcupressure_CheckHP
        if (HpPercent(Who.AttackerPartner) < 51)
        {
            Score(-1);
            return;
        }
        if (HpPercent(Who.AttackerPartner) <= 90 && RandomBelow(128)) return;
        // TagStrategy_PartnerAcupressure_TryScorePlus2
        if (RandomBelow(80)) return;
        Score(2);
    }
}
