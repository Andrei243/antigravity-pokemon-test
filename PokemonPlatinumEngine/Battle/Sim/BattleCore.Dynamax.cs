using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// Dynamax and Gigantamax (plan 06 · R22; docs/mechanics/rulings.md, "Dynamax and Gigantamax"), the third of the four
// later mechanics, on R20's pattern: a flag on the move's choice (BattleChoice.Dynamax), refused by WhyNot where it
// can't be; the change carried out at its moment of the turn (after the Mega Evolutions), with its line and an event of
// its own (Dynamaxed, DynamaxEnded, MaxMoveUsed); the trainer AI's use of it (TrainerAi.WithTheLaterMechanics); all of
// it behind the Ruleset (Ruleset.Dynamax), so a battle of Platinum's rules never comes here. The rules are Pokémon
// Showdown's at the pinned commit: data/conditions.ts (the dynamax condition: three turns, the HP by the Dynamax Level,
// no flinch, no Torment, no dragging out), sim/battle-actions.ts (getMaxMove, getActiveMaxMove), sim/pokemon.ts
// (getDynamaxRequest, maxMoveDisabled, getUndynamaxedHP), sim/battle.ts (endTurn ends it at its third turn),
// sim/battle-queue.ts (runDynamax, order 105) and data/moves.ts (each Max Move's and G-Max Move's own effect, Max
// Guard, and what fails against a Dynamaxed Pokémon). A Mega Dynamax of our own, which plan 06 lists for later, is a
// Mega form that Dynamaxes: MegaFormOf and WhyNotDynamax's refusal of a Mega are the two places it would change.
public sealed partial class BattleCore
{
    /// <summary>The item in the bag that lets the player's Pokémon Dynamax, by name.</summary>
    public const string DynamaxBandItem = "Dynamax Band";

    /// <summary>The player has a Dynamax Band (<see cref="CoreSetup.DynamaxBand"/>).</summary>
    public bool DynamaxBand { get; }

    /// <summary>Whoever has Dynamaxed this battle, and whoever has chosen to this turn: a trainer, or the player for their own (<see cref="MegaKey"/>).</summary>
    private readonly HashSet<object> dynamaxUsed = new(), dynamaxChosen = new();

    /// <summary>The uses of Max Moves, and the move each was made of.</summary>
    private readonly Dictionary<MoveUse, MoveData> maxUses = new();

    /// <summary>The species that can't Dynamax (Showdown's <c>cannotDynamax</c>).</summary>
    private static readonly HashSet<string> NeverDynamax = new() { "Zacian", "Zamazenta", "Eternatus" };

    /// <summary>Each type's Max Move (Showdown's <c>MAX_MOVES</c>); a status move's is Max Guard.</summary>
    private static readonly Dictionary<PokemonType, string> TypeMaxMoves = new()
    {
        [PokemonType.Normal] = "Max Strike", [PokemonType.Fighting] = "Max Knuckle", [PokemonType.Flying] = "Max Airstream",
        [PokemonType.Poison] = "Max Ooze", [PokemonType.Ground] = "Max Quake", [PokemonType.Rock] = "Max Rockfall",
        [PokemonType.Bug] = "Max Flutterby", [PokemonType.Ghost] = "Max Phantasm", [PokemonType.Steel] = "Max Steelspike",
        [PokemonType.Fire] = "Max Flare", [PokemonType.Water] = "Max Geyser", [PokemonType.Grass] = "Max Overgrowth",
        [PokemonType.Electric] = "Max Lightning", [PokemonType.Psychic] = "Max Mindstorm", [PokemonType.Ice] = "Max Hailstorm",
        [PokemonType.Dragon] = "Max Wyrmwind", [PokemonType.Dark] = "Max Darkness", [PokemonType.Fairy] = "Max Starfall"
    };

    /// <summary>What every status move becomes under Dynamax.</summary>
    public const string MaxGuardName = "Max Guard";

    /// <summary>The three G-Max Moves of a power of their own, whatever the move they are made of, that ignore their target's ability.</summary>
    private static readonly HashSet<string> FixedGMaxMoves = new() { "G-Max Drum Solo", "G-Max Fireball", "G-Max Hydrosnipe" };

    /// <summary>The two G-Max Moves that go through every protection, Max Guard's too, and whole.</summary>
    private static readonly HashSet<string> UnstoppableGMaxMoves = new() { "G-Max One Blow", "G-Max Rapid Flow" };

    /// <summary>The moves Max Guard lets through (its condition's <c>bypassesMaxGuard</c>).</summary>
    private static readonly HashSet<string> MaxGuardBypass = new()
    {
        "Acupressure", "After You", "Ally Switch", "Aromatherapy", "Aromatic Mist", "Coaching", "Confide", "Copycat", "Curse", "Decorate",
        "Doom Desire", "Feint", "Future Sight", "G-Max One Blow", "G-Max Rapid Flow", "Heal Bell", "Hold Hands", "Howl", "Jungle Healing",
        "Life Dew", "Mean Look", "Perish Song", "Play Nice", "Power Trick", "Roar", "Role Play", "Tearful Look"
    };

    /// <summary>The move effects that fail against a Dynamaxed Pokémon: Encore, Torment, Skill Swap, Entrainment, Instruct, and the moves that go by its weight.</summary>
    private static readonly HashSet<string> FailOnDynamaxed = new()
    {
        "Encore", "Torment", "SwitchAbilities", "Entrainment", "Instruct", "IncreasePowerWithWeight", "HeavySlam"
    };

    /// <summary>The G-Max Moves, by the form or species whose they are.</summary>
    private static Dictionary<string, MoveData>? gMaxMoves;

    private static Dictionary<string, MoveData> GMaxMoves => gMaxMoves ??= MoveDatabase.GetAll()
        .Where(m => m.Kind == MoveKind.GMaxMove && m.GigantamaxOf != null)
        .GroupBy(m => m.GigantamaxOf!).ToDictionary(g => g.Key, g => g.First());

    /// <summary>The trainer of the place has a Dynamax Band: the player's own (<see cref="DynamaxBand"/>), a trainer's or a partner's.</summary>
    private bool HasDynamaxBand(Battler b) => b.Trainer != null ? b.Trainer.DynamaxBand : b.IsPlayerSide && DynamaxBand;

    /// <summary>The Pokémon in a place is Dynamaxed (what R26's Dynamax Cannon, Behemoth Blade and Behemoth Bash ask).</summary>
    public static bool IsDynamaxed(Battler b) => b.Volatile.Dynamaxed;

    /// <summary>
    /// The Gigantamax form a Pokémon with the Gigantamax Factor takes as it Dynamaxes; null for none. One in a form
    /// takes its form's own (Toxtricity's Low Key Form, Urshifu's Rapid Strike Style) or none (a regional form, a
    /// cap's Pikachu); one in none takes its species' (or the first of them, the Amped Form's and the Single Strike
    /// Style's).
    /// </summary>
    public static string? GigantamaxFormOf(Pokemon p)
    {
        if (!p.GigantamaxFactor || p.IsEgg) return null;
        if (p.Form != null) return p.Species.Form(p.Form + "-Gmax")?.Name;
        return p.Species.Form(p.Species.Name + "-Gmax")?.Name
            ?? p.Species.Forms?.FirstOrDefault(f => f.Kind == FormKind.Gigantamax)?.Name;
    }

    /// <summary>The G-Max Move of the form or species a Pokémon was in before its Gigantamax form (Showdown's <c>canGigantamax</c>).</summary>
    private static MoveData? GMaxMoveOf(PokemonSpecies species, string? formBefore) =>
        formBefore != null && GMaxMoves.TryGetValue(formBefore, out var own) ? own
        : GMaxMoves.GetValueOrDefault(species.Name);

    /// <summary>
    /// The Max Move a move becomes under Dynamax (Showdown's <c>getMaxMove</c>): Max Guard for a status move, the
    /// G-Max Move of a Gigantamax form for a damaging move of its type, the type's Max Move for the rest; null for
    /// Struggle, which stays Struggle.
    /// </summary>
    public static MoveData? MaxMoveOf(PokemonSpecies species, MoveData move, bool gigantamax, string? formBefore)
    {
        if (move == StruggleData) return null;
        if (move.Category == MoveCategory.Status) return MoveDatabase.Get(MaxGuardName);
        if (gigantamax && GMaxMoveOf(species, formBefore) is { } g && g.Type == move.Type) return g;
        return TypeMaxMoves.TryGetValue(move.Type, out var name) ? MoveDatabase.Get(name) : null;
    }

    /// <summary>The Max Move the move in this position would be for the Pokémon in a place, Dynamaxed or about to be; null where it has none.</summary>
    public MoveData? MaxMoveFor(Battler b, int move)
    {
        var mine = At(b.Place);
        if (mine.Pokemon is not { } p || move < 0 || move >= p.Moves.Count) return null;
        var v = mine.Volatile;
        bool gigantamax = v.Dynamaxed ? v.Gigantamaxed : GigantamaxFormOf(p) != null;
        return MaxMoveOf(p.Species, p.Moves[move].Data, gigantamax, v.Dynamaxed ? v.FormBeforeGigantamax : p.Form);
    }

    // ---------------------------------------------------------------- what may be chosen

    /// <summary>
    /// Why the Pokémon in a place can't Dynamax with its move this turn; null when it can (Showdown's
    /// <c>getDynamaxRequest</c>). Under rules without it, with no Dynamax Band, Dynamaxed already, once its trainer
    /// has Dynamaxed, in a Mega, primal or Ultra form or able to Mega Evolve, holding a Z-Crystal, of a species that
    /// can't, or with no Max Move it may use.
    /// </summary>
    public string? WhyNotDynamax(Battler b)
    {
        var mine = At(b.Place);
        string no = Text.Of("battle.cantDynamax", ("pokemon", mine.Name));
        if (!Rules.Dynamax || mine.Pokemon is not { } p || !HasDynamaxBand(mine) || mine.Volatile.Dynamaxed) return no;
        if (dynamaxUsed.Contains(MegaKey(mine))) return Text.Of("battle.dynamaxOnlyOnce");
        if (p.FormData?.Kind is FormKind.Mega or FormKind.Primal || p.Form == UltraForm || p.HeldItem?.ZCrystal != null || MegaFormOf(p) != null) return no;
        if (NeverDynamax.Contains(p.Species.Name) || p.IsEgg) return no;
        return p.Moves.Any(m => WhyNotMaxMove(mine, m) == null) ? null : no;
    }

    /// <summary>Whether the Pokémon in a place may Dynamax with the move it chooses now (and nobody of its trainer's has chosen to this turn).</summary>
    public bool CanDynamax(Battler b) => WhyNotDynamax(b) == null && !dynamaxChosen.Contains(MegaKey(At(b.Place)));

    /// <summary>A choice that Dynamaxes: with a move of its own, alone of the later mechanics, and where <see cref="WhyNotDynamax"/> lets it.</summary>
    private string? WhyNotDynamaxChoice(BattleChoice choice, Battler b)
    {
        if (choice.Kind != ChoiceKind.Fight || choice.Move == BattleChoice.HeldMove || choice.Mega || choice.ZMove || choice.UltraBurst)
            return Text.Of("battle.cantDynamax", ("pokemon", b.Name));
        return WhyNotDynamax(b);
    }

    /// <summary>What a chooser inside may not have: a Dynamax the rules don't let it make.</summary>
    private BattleChoice AsTheDynamaxRulesLet(Battler b, BattleChoice choice) =>
        choice.Dynamax && (WhyNotDynamaxChoice(choice, b) != null || !CanDynamax(b)) ? choice with { Dynamax = false } : choice;

    /// <summary>Two Dynamaxes of one trainer in one answer are refused.</summary>
    private void RefuseTwoDynamax(IReadOnlyList<BattleChoice> choices)
    {
        if (choices.Where(c => c.Dynamax).GroupBy(c => MegaKey(At(c.Who))).Any(g => g.Count() > 1)) throw new ArgumentException(Text.Of("battle.dynamaxOnlyOnce"));
    }

    /// <summary>Notes the Dynamaxes an answer chooses, refusing a second one of the same trainer's in a turn.</summary>
    private void ChooseDynamax(IEnumerable<BattleChoice> chosen)
    {
        foreach (var choice in chosen.Where(c => c.Dynamax))
            if (!dynamaxChosen.Add(MegaKey(At(choice.Who)))) throw new ArgumentException(Text.Of("battle.dynamaxOnlyOnce"));
    }

    /// <summary>
    /// Why a move can't be picked as a Max Move (Showdown's <c>maxMoveDisabled</c>, and what still disables a
    /// Dynamaxed Pokémon's moves): Disable, a taunt or an Assault Vest against a status move, Belch before a berry, no
    /// PP. Torment, Imprison, Gravity, Heal Block, Throat Chop and a Choice item hold nothing back.
    /// </summary>
    private string? WhyNotMaxMove(Battler b, Move move, bool ignorePp = false)
    {
        var mine = At(b.Place);
        var v = mine.Volatile;
        var data = move.Data;
        if (v.Disabled == data) return Text.Of("battle.isDisabled", ("pokemon", mine.Name), ("move", move.DisplayName));
        bool status = data.Category == MoveCategory.Status;
        if (status && v.TauntTurns > 0) return Text.Of("battle.cantUseAfterThe", ("pokemon", mine.Name), ("move", move.DisplayName));
        if (status && BattleEffects.ItemInHand(mine)?.Name == "Assault Vest") return Text.Of("battle.isDisabled", ("pokemon", mine.Name), ("move", move.DisplayName));
        if (data.Effect == "Belch" && !ateBerry.Contains(mine.Pokemon!)) return Text.Of("battle.isDisabled", ("pokemon", mine.Name), ("move", move.DisplayName));
        if (!ignorePp && move.CurrentPP <= 0) return Text.Of("battle.thereNoPpLeft");
        return null;
    }

    /// <summary>An action's priority: a Max Move goes by its own, Max Guard's +4 and the rest's 0 (Showdown's <c>getActionSpeed</c>, which reads the Max Move from the dex).</summary>
    private int ActPriority(Act a)
    {
        if (a.Move is not { } m) return 0;
        bool max = Rules.Dynamax && a.Choice.Kind == ChoiceKind.Fight && a.Choice.Move != BattleChoice.HeldMove && m.Data != StruggleData
            && (a.Choice.Dynamax || a.User.Volatile.Dynamaxed);
        if (!max) return m.Priority;
        return m.Data.Category == MoveCategory.Status ? MoveDatabase.Get(MaxGuardName)?.Priority ?? 4 : 0;
    }

    // ---------------------------------------------------------------- the Dynamax and its end

    /// <summary>
    /// The turn's Dynamaxes, after its Mega Evolutions (Showdown's <c>runDynamax</c>, order 105), the fastest first:
    /// every Pokémon still on the field whose move came with one. True when anyone did.
    /// </summary>
    private bool Dynamaxes(IReadOnlyList<Act> acts)
    {
        dynamaxChosen.Clear();
        var those = acts.Where(a => a.Choice is { Kind: ChoiceKind.Fight, Dynamax: true } && a.User.IsActive && a.User.Pokemon == a.Actor).ToList();
        if (those.Count == 0) return false;
        bool any = false;
        foreach (var b in BySpeed().Where(b => those.Any(a => a.User == b)))
        {
            if (Result != BattleResult.None) break;
            if (!b.IsActive || WhyNotDynamax(b) != null) continue;
            Dynamax(b);
            any = true;
        }
        return any;
    }

    /// <summary>
    /// The dynamax condition's <c>onStart</c>: Minimize, a Substitute and a Torment go; a Cramorant with its catch
    /// lets it go; one with the Gigantamax Factor takes its Gigantamax form; and its HP, its most and what it has, are
    /// raised by half and a twentieth for each Dynamax Level (twice at 10), all but a Shedinja's.
    /// </summary>
    private void Dynamax(Battler b)
    {
        var p = b.Pokemon!;
        var v = b.Volatile;
        dynamaxUsed.Add(MegaKey(b));
        v.Minimized = false;
        v.Tormented = false;
        bool subGone = b.HasSubstitute;
        v.SubstituteHp = 0;
        if (p.Form is "Cramorant-Gulping" or "Cramorant-Gorging" && !v.Transformed) p.Form = null;

        string? gmax = v.Transformed ? null : GigantamaxFormOf(p);
        v.FormBeforeGigantamax = p.Form;
        v.AbilityBeforeGigantamax = p.AbilityName;
        if (gmax != null)
        {
            // Only its look and its Max Moves change: a Gigantamax form has its species' stats, types and ability
            p.Form = gmax;
            v.Gigantamaxed = true;
        }
        v.Dynamaxed = true;
        v.DynamaxTurns = 0;
        v.MaxHpBeforeDynamax = p.MaxHP;
        if (p.Species.Name != "Shedinja")
        {
            // ratio = 1.5 + level × 0.05, in twentieths
            int twentieths = 30 + Math.Clamp(p.DynamaxLevel, 0, 10);
            p.TakeHp(p.MaxHP * twentieths / 20, p.CurrentHP * twentieths / 20);
        }
        v.MaxHpDynamaxed = p.MaxHP;
        var line = Say(gmax != null ? Text.Of("battle.gigantamaxed", ("pokemon", b.Name)) : Text.Of("battle.dynamaxed", ("pokemon", b.Name)))
            .With(new Dynamaxed(b.Place, p.Form, gmax != null, p.MaxHP, p.CurrentHP));
        if (subGone) line.With(new SubstituteChanged(b.Place, Up: false));
    }

    /// <summary>
    /// The dynamax condition's <c>onEnd</c>: its HP falls back by the same share, rounded up (<c>getUndynamaxedHP</c>),
    /// its most to what it was, and a Gigantamax form back to the form it was in. Said at its third turn's end; as its
    /// Pokémon leaves the field, faints or the battle ends, only its event is told.
    /// </summary>
    private void Undynamax(Battler b, bool said)
    {
        var v = b.Volatile;
        if (!v.Dynamaxed || b.Pokemon is not { } p) return;
        v.Dynamaxed = false;
        v.DynamaxTurns = 0;
        if (v.Gigantamaxed)
        {
            p.Form = v.FormBeforeGigantamax;
            v.Gigantamaxed = false;
        }
        if (p.Species.Name != "Shedinja")
        {
            if (p.MaxHP == v.MaxHpDynamaxed && v.MaxHpDynamaxed > 0)
                p.TakeHp(v.MaxHpBeforeDynamax, (int)(((long)p.CurrentHP * v.MaxHpBeforeDynamax + p.MaxHP - 1) / p.MaxHP));
            else
                // Its stats were worked out again meanwhile (a level gained): its most is already its own
                p.TakeHp(p.MaxHP, Math.Min(p.CurrentHP, p.MaxHP));
        }
        var ended = new DynamaxEnded(b.Place, p.Form, p.MaxHP, p.CurrentHP);
        if (said) Say(Text.Of("battle.dynamaxEnded", ("pokemon", b.Name))).With(ended);
        else Emit(ended);
    }

    /// <summary>Each Dynamax counts the turn's end, and ends at its third (Showdown's residual <c>turns++</c> and <c>endTurn</c>), the fastest first.</summary>
    private void DynamaxRunsDown()
    {
        if (Result != BattleResult.None) return;
        var ending = new List<Battler>();
        foreach (var b in AllBattlers.Where(b => b.IsActive && b.Volatile.Dynamaxed))
            if (++b.Volatile.DynamaxTurns >= 3) ending.Add(b);
        // (BySpeed may toss a coin between equals: asked only when someone's Dynamax ends, so nothing else draws for it)
        if (ending.Count == 0) return;
        foreach (var b in BySpeed().Where(ending.Contains)) Undynamax(b, said: true);
    }

    /// <summary>A Dynamaxed Pokémon can't be dragged out by Roar, Whirlwind, Dragon Tail, Circle Throw or a Red Card (the condition's <c>onDragOut</c>).</summary>
    private bool HoldsItsGroundDynamaxed(Battler b)
    {
        if (!b.Volatile.Dynamaxed) return false;
        Say(Text.Of("battle.dynamaxHoldsGround", ("pokemon", b.Name)));
        return true;
    }

    /// <summary>
    /// Max Guard, and what a Dynamaxed target can't be made to do (<c>Guarded</c> asks it first): Max Guard stops every
    /// move but its own list, which go through every protection; a one-hit knockout doesn't affect a Dynamaxed target,
    /// and Encore, Torment, Skill Swap, Entrainment, Instruct and the moves that go by its weight fail. Null where
    /// none of it has a say.
    /// </summary>
    private bool? DynamaxGuards(MoveUse use, Battler target, MoveHit hit)
    {
        if (target == use.User) return null;
        if (target.Turn.Shield == Shield.MaxGuard) return !MaxGuardBypass.Contains(use.Data.Name);
        if (!target.Volatile.Dynamaxed) return null;
        if (use.Data.Effect == "OneHitKo")
        {
            hit.FailLine = Text.Of("battle.itDoesntAffect", ("target", target.Name));
            return true;
        }
        if (use.Data.Effect is { } effect && FailOnDynamaxed.Contains(effect))
        {
            hit.FailLine = Text.Of("battle.butItFailed");
            return true;
        }
        return null;
    }

    // ---------------------------------------------------------------- the Max Moves

    /// <summary>
    /// A Dynamaxed Pokémon's move becomes its Max Move once it has paid its PP (Showdown's <c>getActiveMaxMove</c>): of
    /// the move's power as a Max Move (the three G-Max Moves of their own power keep theirs), its category and its
    /// priority, where Psychic Terrain and Quick Guard read it. Its line is said here, with the event that says what
    /// it was made of; a Max Move goes through Protect and its kin at a quarter, as a Z-Move does.
    /// </summary>
    private void BecomeMaxMove(MoveUse use, Battler? target)
    {
        var user = use.User;
        var p = user.Pokemon!;
        var own = use.Move.Data;
        if (MaxMoveOf(p.Species, own, user.Volatile.Gigantamaxed, user.Volatile.FormBeforeGigantamax) is not { } max) return;
        var data = max.CopyWith(d =>
        {
            if (own.Category != MoveCategory.Status)
            {
                if (!FixedGMaxMoves.Contains(max.Name)) d.Power = own.MaxPower > 0 ? own.MaxPower : 100;
                d.Category = own.Category;
                // The three that ignore their target's ability, as Sunsteel Strike does (DamageCalculator.IgnoresAbilities)
                if (FixedGMaxMoves.Contains(max.Name)) d.Effect = "SunsteelStrike";
            }
            d.Priority = own.Priority;
        });
        use.Move = new Move(data, use.Move.CurrentPP);
        use.Own = data;
        use.Effect = EffectOf(data);
        maxUses[use] = own;
        if (data.Category != MoveCategory.Status && !UnstoppableGMaxMoves.Contains(data.Name)) zUses.Add(use);
        // What it used last is the Max Move: Max Guard's chain counts by it, and Disable and Encore find nothing
        user.Volatile.LastMove = data;
        use.Line = Say(Text.Of("battle.used", ("user", user.Name), ("move", data.DisplayName))).With(new MaxMoveUsed(user.Place, data.Name, own.Name));
        if (!user.IsActive) return;
        use.Targets = ResolveTargets(user, use.Move, target);
    }

    /// <summary>The Max Moves' and the G-Max Moves' effects, by the names the data gives them (the importer marks them run by these).</summary>
    private static readonly Dictionary<string, MoveEffect> MaxEffects = new()
    {
        ["MaxMove"] = new() { Then = (b, use) => b.MaxMoveFollows(use) },
        ["GMaxMove"] = new() { Then = (b, use) => b.MaxMoveFollows(use), Hit = (b, use, hit) => b.GMaxOnTarget(use, hit) },
        // Max Guard: Protect's chance and chain (it counts toward Protect's), against everything but its own list
        ["MaxGuard"] = new() { Does = (b, use) => b.Brace(use, endure: false, Shield.MaxGuard) }
    };

    /// <summary>The user and its allies on the field (Showdown's <c>alliesAndSelf</c>), and its foes (<c>foes</c>).</summary>
    private List<Battler> AlliesAndSelf(Battler user) => SlotsOf(user.Side).Where(b => b.IsActive).ToList();

    private List<Battler> FoesOf(Battler user) => ActiveFoes(user).ToList();

    /// <summary>
    /// A Max Move's or a G-Max Move's <c>self.onHit</c>, once it has hit: the stats of its side or of the foes, the
    /// weather or the terrain for a Max Move; for a G-Max Move what is its own.
    /// </summary>
    private void MaxMoveFollows(MoveUse use)
    {
        if (!maxUses.ContainsKey(use) || !use.Hits.Any(h => h.Landed) || Result != BattleResult.None) return;
        var user = use.User;
        void Mine(StatType stat, int stages)
        {
            foreach (var b in AlliesAndSelf(user)) ChangeStat(b, stat, stages, user, false, By.Move);
        }
        void Theirs(StatType stat, int stages)
        {
            foreach (var b in FoesOf(user)) ChangeStat(b, stat, stages, user, false, By.Move);
        }
        void Sky(BattleWeather weather, string rock)
        {
            int turns = 5 + BattleEffects.Of(user).Sum(e => e.ExtraTurns(rock));
            SetWeather(Rules.Sky(weather), turns, Starts(Rules.Sky(weather)));
        }
        switch (use.Data.Name)
        {
            case "Max Strike": Theirs(StatType.Speed, -1); break;
            case "Max Flutterby": Theirs(StatType.SpAttack, -1); break;
            case "Max Phantasm": Theirs(StatType.Defense, -1); break;
            case "Max Wyrmwind": Theirs(StatType.Attack, -1); break;
            case "Max Darkness": Theirs(StatType.SpDefense, -1); break;
            case "Max Knuckle": Mine(StatType.Attack, 1); break;
            case "Max Airstream": Mine(StatType.Speed, 1); break;
            case "Max Ooze": Mine(StatType.SpAttack, 1); break;
            case "Max Quake": Mine(StatType.SpDefense, 1); break;
            case "Max Steelspike": Mine(StatType.Defense, 1); break;
            case "Max Flare": Sky(BattleWeather.Sun, "Sun"); break;
            case "Max Geyser": Sky(BattleWeather.Rain, "Rain"); break;
            case "Max Rockfall": Sky(BattleWeather.Sandstorm, "Sandstorm"); break;
            case "Max Hailstorm": Sky(BattleWeather.Hail, "Hail"); break;
            case "Max Lightning": SetTerrain(FieldCondition.ElectricTerrain, user); break;
            case "Max Overgrowth": SetTerrain(FieldCondition.GrassyTerrain, user); break;
            case "Max Mindstorm": SetTerrain(FieldCondition.PsychicTerrain, user); break;
            case "Max Starfall": SetTerrain(FieldCondition.MistyTerrain, user); break;
            default: GMaxFollows(use); break;
        }
    }

    /// <summary>The G-Max Moves' own <c>self.onHit</c> (data/moves.ts), on the user's side, the foes or the field.</summary>
    private void GMaxFollows(MoveUse use)
    {
        var user = use.User;
        var theirs = Other(user.Side);
        switch (use.Data.Name)
        {
            case "G-Max Befuddle":
                foreach (var t in FoesOf(user))
                    TryInflictStatus(t, rng.Roll(RollKind.Pick, 3) switch { 0 => StatusCondition.Sleep, 1 => StatusCondition.Paralyze, _ => StatusCondition.Poison }, user, false, By.Move);
                break;
            case "G-Max Stun Shock":
                foreach (var t in FoesOf(user))
                    TryInflictStatus(t, rng.Roll(RollKind.Pick, 2) == 0 ? StatusCondition.Paralyze : StatusCondition.Poison, user, false, By.Move);
                break;
            case "G-Max Volt Crash":
                foreach (var t in FoesOf(user)) TryInflictStatus(t, StatusCondition.Paralyze, user, false, By.Move);
                break;
            case "G-Max Malodor":
                foreach (var t in FoesOf(user)) TryInflictStatus(t, StatusCondition.Poison, user, false, By.Move);
                break;
            case "G-Max Gold Rush":
            case "G-Max Smite":
                foreach (var t in FoesOf(user)) Confuse(t, user, false, By.Move);
                break;
            case "G-Max Cuddle":
                foreach (var t in FoesOf(user)) Charm(user, t);
                break;
            case "G-Max Meltdown":
                foreach (var t in FoesOf(user).Where(t => !t.Volatile.Dynamaxed && !t.Volatile.Tormented))
                {
                    t.Volatile.Tormented = true;
                    Say(Text.Of("battle.wasSubjectedToTorment", ("target", t.Name)));
                }
                break;
            case "G-Max Terror":
                foreach (var t in FoesOf(user).Where(t => t.Volatile.TrappedBy == null))
                {
                    t.Volatile.TrappedBy = user.Place;
                    Say(Text.Of("battle.canNoLongerEscape", ("target", t.Name)));
                }
                break;
            case "G-Max Centiferno":
            case "G-Max Sandblast":
                foreach (var t in FoesOf(user).Where(t => t.Volatile.BindTurns == 0))
                {
                    t.Volatile.BindTurns = BattleEffects.Of(user).Any(e => e.BindsToTheEnd) ? Rules.GripClawBindTurns : Lasting(Rules.BindTurns);
                    t.Volatile.BoundBy = user.Place;
                    t.Volatile.BindingMove = use.Data.Name;
                    Say(Text.Of("battle.wasCaughtIn", ("target", t.Name), ("user", user.Name), ("move", use.Data.DisplayName)));
                }
                break;
            case "G-Max Foam Burst":
                foreach (var t in FoesOf(user)) ChangeStat(t, StatType.Speed, -2, user, false, By.Move);
                break;
            case "G-Max Tartness":
                foreach (var t in FoesOf(user)) ChangeStat(t, StatType.Evasion, -1, user, false, By.Move);
                break;
            case "G-Max Depletion":
                foreach (var t in FoesOf(user))
                {
                    if (t.Volatile.LastMove is not { } last || last.Kind == MoveKind.ZMove || t.Pokemon!.Moves.FirstOrDefault(m => m.Data == last) is not { } slot || slot.CurrentPP <= 0) continue;
                    int taken = Math.Min(2, slot.CurrentPP);
                    slot.CurrentPP -= taken;
                    Say(Text.Of("battle.itReducedThePp", ("target", t.Name), ("move", slot.DisplayName), ("taken", taken)));
                }
                break;
            case "G-Max Chi Strike":
                foreach (var b in AlliesAndSelf(user).Where(b => b.Volatile.ChiStrike < 3))
                {
                    b.Volatile.ChiStrike++;
                    Say(Text.Of("battle.isGettingPumped", ("user", b.Name)));
                }
                break;
            case "G-Max Finale":
                foreach (var b in AlliesAndSelf(user).Where(b => b.Pokemon!.CurrentHP < b.Pokemon.MaxHP))
                    RestoreHp(b, Math.Max(1, b.Pokemon!.MaxHP / 6), Text.Of("battle.regainedHealth", ("user", b.Name)));
                break;
            case "G-Max Sweetness":
                // Every Pokémon of the side, those on the bench too
                foreach (var b in AlliesAndSelf(user).Where(b => b.Pokemon!.Status != StatusCondition.None))
                    CureStatus(b, Text.Of("battle.statusReturnedToNormal", ("user", b.Name)));
                foreach (var p in user.Roster?.Members ?? new List<Pokemon>())
                {
                    if (p.IsFainted || p.Status == StatusCondition.None || AllBattlers.Any(b => b.IsActive && b.Pokemon == p)) continue;
                    p.Status = StatusCondition.None;
                    p.SleepTurns = 0;
                    p.ToxicCounter = 0;
                }
                break;
            case "G-Max Replenish":
                // One time in two, each of the side holding nothing finds the berry it ate
                if (rng.Roll(RollKind.SideEffect, 2) == 0) break;
                foreach (var b in AlliesAndSelf(user).Where(b => b.Pokemon!.HeldItem == null && b.Volatile.ConsumedItem is { Berry: not null }))
                {
                    b.Pokemon!.HeldItem = b.Volatile.ConsumedItem;
                    b.Volatile.ConsumedItem = null;
                    Say(Text.Of("battle.gmaxReplenish", ("pokemon", b.Name), ("item", ItemOf(b))));
                }
                break;
            case "G-Max Gravitas":
                if (!Field.Gravity) Intensify(use);
                break;
            case "G-Max Resonance":
            {
                var side = Field.Side(user.Side);
                if (side.AuroraVeil) break;
                side.AuroraVeilTurns = 5 + BattleEffects.Of(user).Sum(e => e.ExtraTurns("Screens"));
                Say(Text.Of("battle.auroraVeilMadeStronger", ("team", TeamOf(user.Side)))).With(Now(user.Side, SideCondition.AuroraVeil));
                break;
            }
            case "G-Max Stonesurge":
            {
                var side = Field.Side(theirs);
                if (side.StealthRock) break;
                side.StealthRock = true;
                Say(string.Format(Text.Of("battle.pointedStonesFloatIn"), TeamOf(theirs))).With(Now(theirs, SideCondition.StealthRock));
                break;
            }
            case "G-Max Steelsurge":
            {
                var side = Field.Side(theirs);
                if (side.GMaxSteelsurge) break;
                side.GMaxSteelsurge = true;
                Say(Text.Of("battle.gmaxSteelsurgeStart", ("team", TeamOf(theirs)))).With(Now(theirs, SideCondition.GMaxSteelsurge));
                break;
            }
            case "G-Max Wildfire": FourTurnsOf(theirs, SideCondition.GMaxWildfire); break;
            case "G-Max Cannonade": FourTurnsOf(theirs, SideCondition.GMaxCannonade); break;
            case "G-Max Vine Lash": FourTurnsOf(theirs, SideCondition.GMaxVineLash); break;
            case "G-Max Volcalith": FourTurnsOf(theirs, SideCondition.GMaxVolcalith); break;
            case "G-Max Wind Rage": WindRage(user); break;
        }
    }

    /// <summary>G-Max Snooze's own <c>onHit</c>, on each target it hit: one time in two, one with no condition grows drowsy.</summary>
    private void GMaxOnTarget(MoveUse use, MoveHit hit)
    {
        if (use.Data.Name != "G-Max Snooze" || !hit.Landed || !hit.Target.IsActive) return;
        var t = hit.Target;
        if (t.Pokemon!.Status != StatusCondition.None || t.Volatile.YawnTurns > 0 || Field.Side(t.Side).Safeguard) return;
        if (rng.Roll(RollKind.SideEffect, 2) == 0) return;
        t.Volatile.YawnTurns = 2;
        Say(Text.Of("battle.madeDrowsy", ("user", use.User.Name), ("target", t.Name)));
    }

    /// <summary>G-Max Cuddle's love (the attract condition's <c>onStart</c>): of the other gender, not Oblivious, and not in love already.</summary>
    private void Charm(Battler user, Battler t)
    {
        if (t.Ability?.Effect is { BlocksInfatuation: true } || t.Volatile.InLoveWith != null) return;
        var mine = user.Pokemon!.Gender;
        var theirs = t.Pokemon!.Gender;
        if (mine == theirs || mine == Gender.Genderless || theirs == Gender.Genderless) return;
        t.Volatile.InLoveWith = user.Place;
        Say(Text.Of("battle.fellInLove", ("target", t.Name))).With(Passing(t));
        Reciprocate(t, user);
    }

    /// <summary>The four turns of G-Max Wildfire, Cannonade, Vine Lash or Volcalith round a side; one already there stays as it is.</summary>
    private void FourTurnsOf(BattleSide side, SideCondition what)
    {
        var state = Field.Side(side);
        ref int turns = ref GMaxTurns(state, what);
        if (turns > 0) return;
        turns = 4;
        Say(Text.Of(GMaxLines(what).Start, ("team", TeamOf(side)))).With(Now(side, what));
    }

    private static ref int GMaxTurns(SideState state, SideCondition what)
    {
        switch (what)
        {
            case SideCondition.GMaxWildfire: return ref state.GMaxWildfireTurns;
            case SideCondition.GMaxCannonade: return ref state.GMaxCannonadeTurns;
            case SideCondition.GMaxVineLash: return ref state.GMaxVineLashTurns;
            default: return ref state.GMaxVolcalithTurns;
        }
    }

    /// <summary>The G-Max Move whose a side condition is (what the information panel names it by).</summary>
    public static MoveData? GMaxMoveOf(SideCondition what) => MoveDatabase.Get(what switch
    {
        SideCondition.GMaxSteelsurge => "G-Max Steelsurge",
        SideCondition.GMaxWildfire => "G-Max Wildfire",
        SideCondition.GMaxCannonade => "G-Max Cannonade",
        SideCondition.GMaxVineLash => "G-Max Vine Lash",
        SideCondition.GMaxVolcalith => "G-Max Volcalith",
        _ => ""
    });

    /// <summary>The lines of the four: as it comes, as it hurts, as it goes.</summary>
    private static (string Start, string Hurt, string End) GMaxLines(SideCondition what) => what switch
    {
        SideCondition.GMaxWildfire => ("battle.gmaxWildfireStart", "battle.gmaxWildfireHurt", "battle.gmaxWildfireEnd"),
        SideCondition.GMaxCannonade => ("battle.gmaxCannonadeStart", "battle.gmaxCannonadeHurt", "battle.gmaxCannonadeEnd"),
        SideCondition.GMaxVineLash => ("battle.gmaxVineLashStart", "battle.gmaxVineLashHurt", "battle.gmaxVineLashEnd"),
        _ => ("battle.gmaxVolcalithStart", "battle.gmaxVolcalithHurt", "battle.gmaxVolcalithEnd")
    };

    /// <summary>The four and the type each spares.</summary>
    private static readonly (SideCondition What, PokemonType Spares)[] GMaxFields =
    {
        (SideCondition.GMaxWildfire, PokemonType.Fire), (SideCondition.GMaxCannonade, PokemonType.Water),
        (SideCondition.GMaxVineLash, PokemonType.Grass), (SideCondition.GMaxVolcalith, PokemonType.Rock)
    };

    /// <summary>At a turn's end the four hurt whoever on their side isn't of their type, a sixth of its HP before any Dynamax (their <c>onResidual</c>, <c>baseMaxhp / 6</c>).</summary>
    private void GMaxBurns(Battler b)
    {
        var state = Field.Side(b.Side);
        foreach (var (what, spares) in GMaxFields)
        {
            if (!b.IsActive || GMaxTurns(state, what) == 0 || b.HasType(spares) || BattleEffects.Of(b).Any(e => e.PreventsIndirectDamage)) continue;
            int baseMax = b.Volatile.Dynamaxed ? b.Volatile.MaxHpBeforeDynamax : b.Pokemon!.MaxHP;
            LoseHp(b, Math.Max(1, baseMax / 6), Text.Of(GMaxLines(what).Hurt, ("pokemon", b.Name)));
        }
    }

    /// <summary>The four run four turn ends each, and say so as they go.</summary>
    private void GMaxRunDown()
    {
        foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
        {
            var state = Field.Side(side);
            foreach (var (what, _) in GMaxFields)
                if (RunsOut(ref GMaxTurns(state, what))) Say(Text.Of(GMaxLines(what).End, ("team", TeamOf(side)))).With(Now(side, what));
        }
    }

    /// <summary>G-Max Steelsurge's steel bites whoever comes in, as Stealth Rock does by the Steel type (its condition's <c>onSwitchIn</c>).</summary>
    private void SteelsurgeBites(Battler b)
    {
        if (!b.IsActive || !Field.Side(b.Side).GMaxSteelsurge) return;
        var mon = b.Pokemon!;
        float matchup = TypeChart.GetEffectiveness(PokemonType.Steel, mon.PrimaryType, mon.SecondaryType, Rules, Field.Inverse);
        int divisor = matchup switch { >= 4f => 2, >= 2f => 4, >= 1f => 8, >= 0.5f => 16, > 0f => 32, _ => 0 };
        if (divisor > 0) LoseHp(b, Math.Max(1, mon.MaxHP / divisor), Text.Of("battle.gmaxSteelsurgeHurt", ("pokemon", b.Name)));
    }

    /// <summary>
    /// G-Max Wind Rage (its <c>self.onHit</c>): the foes' screens and veils go, and what lies on either side; the
    /// terrain lifts. Each line takes its own look off the field.
    /// </summary>
    private void WindRage(Battler user)
    {
        var theirs = Other(user.Side);
        var foe = Field.Side(theirs);
        string team = TeamOf(theirs, capital: true);
        SideConditionChanged Gone(BattleSide side, SideCondition what) => new(side, what, 0);
        if (foe.Reflect)
        {
            foe.ReflectTurns = 0;
            Say(Text.Of("battle.reflectWasBlownAway", ("team", team))).With(Gone(theirs, SideCondition.Reflect));
        }
        if (foe.LightScreen)
        {
            foe.LightScreenTurns = 0;
            Say(Text.Of("battle.lightScreenWasBlown", ("team", team))).With(Gone(theirs, SideCondition.LightScreen));
        }
        if (foe.AuroraVeil)
        {
            foe.AuroraVeilTurns = 0;
            Say(Text.Of("battle.auroraVeilWoreOff", ("team", team))).With(Gone(theirs, SideCondition.AuroraVeil));
        }
        if (foe.Safeguard)
        {
            foe.SafeguardTurns = 0;
            Say(Text.Of("battle.safeguardWasBlownAway", ("team", team))).With(Gone(theirs, SideCondition.Safeguard));
        }
        if (foe.Mist)
        {
            foe.MistTurns = 0;
            Say(Text.Of("battle.mistWasBlownAway", ("team", team))).With(Gone(theirs, SideCondition.Mist));
        }
        foreach (var side in new[] { theirs, user.Side })
        {
            var s = Field.Side(side);
            string who = TeamOf(side);
            if (s.Spikes > 0)
            {
                s.Spikes = 0;
                Say(Text.Of("battle.theSpikesAroundWere", ("team", who))).With(Gone(side, SideCondition.Spikes));
            }
            if (s.ToxicSpikes > 0)
            {
                s.ToxicSpikes = 0;
                Say(Text.Of("battle.thePoisonSpikesAround", ("team", who))).With(Gone(side, SideCondition.ToxicSpikes));
            }
            if (s.StealthRock)
            {
                s.StealthRock = false;
                Say(Text.Of("battle.thePointedStonesAround", ("team", who))).With(Gone(side, SideCondition.StealthRock));
            }
            if (s.StickyWeb)
            {
                s.StickyWeb = false;
                Say(Text.Of("battle.theStickyWebAroundWas", ("team", who))).With(Gone(side, SideCondition.StickyWeb));
            }
            if (s.GMaxSteelsurge)
            {
                s.GMaxSteelsurge = false;
                Say(Text.Of("battle.gmaxSteelsurgeEnd", ("team", who))).With(Gone(side, SideCondition.GMaxSteelsurge));
            }
        }
        if (Field.Terrain is { } terrain) EndTerrain(terrain);
    }
}
