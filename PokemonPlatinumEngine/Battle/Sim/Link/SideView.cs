using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim.Link;

/// <summary>
/// The team preview (plan 28 · I2, I4): a team as the other player sees it before the first turn, in an order of its
/// own that says nothing of the order the team was brought or will go in (by National Pokédex number, then form, level,
/// gender and shininess; the species clause leaves no two alike, and two alike are told apart by the order they were
/// brought, which is all that could leak). GPU-free.
/// </summary>
public static class TeamPreview
{
    /// <summary>A team's preview, and for each of its Pokémon (in the team's order) its place in the preview.</summary>
    public static (List<PreviewEntry> Preview, int[] PlaceOf) Of(IReadOnlyList<Pokemon> team)
    {
        var entries = team.Select((p, i) => (Entry: EntryOf(p), Index: i))
            .OrderBy(e => e.Entry.Dex).ThenBy(e => e.Entry.Form ?? "", StringComparer.Ordinal).ThenBy(e => e.Entry.Level)
            .ThenBy(e => e.Entry.Gender).ThenBy(e => e.Entry.Shiny).ThenBy(e => e.Index)
            .ToList();
        var placeOf = new int[team.Count];
        for (int j = 0; j < entries.Count; j++) placeOf[entries[j].Index] = j;
        return (entries.Select(e => e.Entry).ToList(), placeOf);
    }

    /// <summary>What the preview shows of one Pokémon.</summary>
    public static PreviewEntry EntryOf(Pokemon p) => new(p.Species.DexNumber, p.Form, p.Level, p.Gender, p.IsShiny);

    /// <summary>
    /// The stand-in a machine holds for a foe it knows only from the preview: its species, form, level, gender and
    /// shininess, and nothing else (no moves, no item, no ability). Its stats are none of the foe's and are never read:
    /// a foe's HP comes as the bar's share (<see cref="SideView.BarPixels"/>). Made the same on every machine.
    /// </summary>
    public static Pokemon StandIn(PreviewEntry entry)
    {
        var species = PokemonDatabase.GetByDex(entry.Dex) ?? throw new BattleWireException($"No species is number {entry.Dex}");
        var p = new Pokemon(species, entry.Level, 0u, new BattleRandom(0))
        {
            Gender = entry.Gender,
            IsShiny = entry.Shiny,
            HeldItem = null,
            AbilityName = null
        };
        p.Form = entry.Form;
        p.Moves.Clear();
        p.CurrentHP = SideView.BarPixels;
        return p;
    }
}

/// <summary>
/// What one side's screen may be shown of a battle (plan 28 · I2), made on the server before anything is sent, following
/// Platinum's link battles: the side's own Pokémon exactly; the foe's HP only as the bar's share in 48ths
/// (<see cref="BarPixels"/>), as Platinum's bar draws it, and its Pokémon as stand-ins numbered by the team preview,
/// which says nothing of the order they were brought in; a foe's move once it has been used, and its item or ability
/// only where an <see cref="ItemShown"/> or <see cref="AbilityShown"/> names it, which the log says no sooner; no EXP,
/// level or move learned of the other side's. The view is a filter over the same events, so whatever plays a log plays
/// a view. Nothing of the battle's chance is in it: the log carries none.
/// <para>
/// It follows the battle as it goes (<see cref="Filter"/> is handed each part of the log once, in order), keeping who
/// stands in each place and their most HP, which a foe's bar is worked out from. GPU-free.
/// </para>
/// </summary>
public sealed class SideView
{
    /// <summary>Platinum's HP bar, in pixels: a foe's HP is told in these.</summary>
    public const int BarPixels = 48;

    public BattleSide Side { get; }

    /// <summary>The foe's team as the preview shows it, a place for each of its Pokémon (and any that came later, a wild ally).</summary>
    public IReadOnlyList<PreviewEntry> FoePreview => foePreview;

    /// <summary>The stand-ins this view's events name the foe's Pokémon by, in the preview's order.</summary>
    public IReadOnlyList<Pokemon> FoeStandIns => standIns;

    /// <summary>The names this view's events go on the wire by: the side's own by team and position, the foe's by their place in the preview.</summary>
    public BattleRoster Roster { get; }

    private readonly List<PreviewEntry> foePreview;
    private readonly List<Pokemon> standIns;
    private readonly Dictionary<Pokemon, Pokemon> standInOf = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Pokemon, int> mostHp = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<Pokemon> own = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Place, Pokemon> standing = new();
    private readonly Dictionary<Place, int> placeMostHp = new();

    /// <param name="side">Whose screen.</param>
    /// <param name="ownTeams">The side's own teams (the player's and a partner's), as the battle holds them.</param>
    /// <param name="foes">Every Pokémon of the other side, as the battle holds them; the preview is made of them.</param>
    /// <param name="startingHp">The most HP each Pokémon went in with, where it is not its <see cref="Pokemon.MaxHP"/> now (one that has levelled since); left out, now's.</param>
    public SideView(BattleSide side, IReadOnlyList<IReadOnlyList<Pokemon>> ownTeams, IReadOnlyList<Pokemon> foes,
        IReadOnlyDictionary<Pokemon, int>? startingHp = null)
    {
        Side = side;
        var (preview, placeOf) = TeamPreview.Of(foes);
        foePreview = preview;
        standIns = preview.Select(TeamPreview.StandIn).ToList();
        for (int i = 0; i < foes.Count; i++) standInOf[foes[i]] = standIns[placeOf[i]];
        var entries = new List<(PokemonRef, Pokemon)>();
        for (int t = 0; t < ownTeams.Count; t++)
            foreach (var (name, p) in BattleRoster.Team(side, t, ownTeams[t]))
            {
                entries.Add((name, p));
                own.Add(p);
            }
        entries.AddRange(BattleRoster.Team(Other, 0, standIns));
        Roster = new BattleRoster(entries);
        foreach (var p in ownTeams.SelectMany(t => t).Concat(foes))
            mostHp[p] = startingHp != null && startingHp.TryGetValue(p, out int hp) ? hp : p.MaxHP;
    }

    private BattleSide Other => Side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;

    /// <summary>A side's view of a whole battle's log, after the fact (the golden battles' tests).</summary>
    public static List<BattleEvent> For(BattleCore core, BattleSide side) => Of(core, side).Filter(core.Log);

    /// <summary>A side's view of a battle, ready to follow it: its own teams and the other side's as the core holds them.</summary>
    public static SideView Of(BattleCore core, BattleSide side)
    {
        var player = new List<IReadOnlyList<Pokemon>> { core.PlayerParty.Members };
        if (core.Partner != null) player.Add(core.Partner.Party.Members);
        var enemy = core.Trainers.Select(t => (IReadOnlyList<Pokemon>)t.Party.Members).ToList();
        if (core.WildPokemon.Count > 0) enemy.Add(core.WildPokemon.ToList());
        // A Pokémon's most HP as it went in: the first level it rose to says what it had before
        var starting = new Dictionary<Pokemon, int>(ReferenceEqualityComparer.Instance);
        foreach (var rose in Flatten(core.Log).OfType<LevelRose>()) starting.TryAdd(rose.Pokemon, rose.Before.HP);
        return side == BattleSide.Player
            ? new SideView(side, player, enemy.SelectMany(t => t).ToList(), starting)
            : new SideView(side, enemy, player.SelectMany(t => t).ToList(), starting);
    }

    private static IEnumerable<BattleEvent> Flatten(IEnumerable<BattleEvent> log)
    {
        foreach (var e in log)
        {
            yield return e;
            if (e is Said said)
                foreach (var inner in said.Shows.Concat(said.OnImpact)) yield return inner;
        }
    }

    /// <summary>The bar's share of a foe's HP, as Platinum's bar draws it: none at none, a pixel at the least while it stands.</summary>
    public static int Bar(int hp, int most)
    {
        if (hp <= 0 || most <= 0) return 0;
        return Math.Clamp(hp * BarPixels / most, 1, BarPixels);
    }

    /// <summary>Whether the Pokémon is one of this side's own.</summary>
    public bool IsOwn(Pokemon p) => own.Contains(p);

    /// <summary>What of these events, the next of the battle's log, this side's screen is shown.</summary>
    public List<BattleEvent> Filter(IEnumerable<BattleEvent> events)
    {
        var shown = new List<BattleEvent>();
        foreach (var e in events)
            if (Shown(e) is { } kept) shown.Add(kept);
        return shown;
    }

    /// <summary>A question as this side is asked it; a Pokémon it names is named as this side knows it.</summary>
    public BattleRequest Request(BattleRequest request, IReadOnlyList<Place> places) => request switch
    {
        ActionRequest => new ActionRequest(places),
        SwitchOfferRequest offer => new SwitchOfferRequest(offer.Place, Seen(offer.Upcoming)),
        _ => request
    };

    private bool Foe(Place place) => place.Side != Side;

    /// <summary>A Pokémon as this side knows it: its own as it is, a foe as its stand-in (one the preview didn't have gets one now).</summary>
    private Pokemon Seen(Pokemon p)
    {
        if (own.Contains(p)) return p;
        if (standInOf.TryGetValue(p, out var known)) return known;
        var entry = TeamPreview.EntryOf(p);
        var standIn = TeamPreview.StandIn(entry);
        foePreview.Add(entry);
        standIns.Add(standIn);
        standInOf[p] = standIn;
        Roster.Add(new PokemonRef(Other, 0, standIns.Count - 1), standIn);
        mostHp.TryAdd(p, p.MaxHP);
        return standIn;
    }

    private int MostAt(Place place) => placeMostHp.TryGetValue(place, out int most) ? most : 0;

    private int HpFor(Place place, int hp) => Foe(place) ? Bar(hp, MostAt(place)) : hp;

    private void Stand(Place place, Pokemon p)
    {
        standing[place] = p;
        placeMostHp[place] = mostHp.TryGetValue(p, out int most) ? most : p.MaxHP;
    }

    /// <summary>One event as this side is shown it; null for one it isn't.</summary>
    private BattleEvent? Shown(BattleEvent e)
    {
        switch (e)
        {
            case Said said:
                // A link battle's lines are written for both players: this side reads its own (plan 28 · I3)
                var line = new Said(Narration.For(said.Text, Side));
                foreach (var inner in said.Shows)
                    if (Shown(inner) is { } kept) line.Shows.Add(kept);
                foreach (var inner in said.OnImpact)
                    if (Shown(inner) is { } kept) line.OnImpact.Add(kept);
                return line;

            case Entered entered:
                Stand(entered.Place, entered.Pokemon);
                return entered with { Pokemon = Seen(entered.Pokemon) };
            case Rotated rotated:
                Stand(rotated.Place, rotated.Pokemon);
                return rotated with { Pokemon = Seen(rotated.Pokemon) };
            case AllyJoined ally:
                Stand(ally.Place, ally.Pokemon);
                var seenAlly = Seen(ally.Pokemon);
                return ally with { Pokemon = seenAlly, AsItCame = seenAlly };
            case PlacesSwapped swapped:
                Swap(swapped.First, swapped.Second);
                return swapped;
            case Caught caught:
                return caught with { Pokemon = Seen(caught.Pokemon) };

            // The foe's HP as its bar
            case Struck struck:
                return struck with { Hp = HpFor(struck.Place, struck.Hp) };
            case HpChanged changed:
                return changed with { Hp = HpFor(changed.Place, changed.Hp) };
            case Dynamaxed big:
                placeMostHp[big.Place] = big.MaxHp;
                return Foe(big.Place) ? big with { MaxHp = BarPixels, Hp = Bar(big.Hp, big.MaxHp) } : big;
            case DynamaxEnded small:
                placeMostHp[small.Place] = small.MaxHp;
                return Foe(small.Place) ? small with { MaxHp = BarPixels, Hp = Bar(small.Hp, small.MaxHp) } : small;
            case FormHpChanged form:
                placeMostHp[form.Place] = form.MaxHp;
                return Foe(form.Place) ? form with { MaxHp = BarPixels, Hp = Bar(form.Hp, form.MaxHp) } : form;
            case Terastallized tera when tera.MaxHp > 0:
                placeMostHp[tera.Place] = tera.MaxHp;
                return Foe(tera.Place) ? tera with { MaxHp = BarPixels, Hp = Bar(tera.Hp, tera.MaxHp) } : tera;

            // What only the Pokémon's own trainer is told
            case ExpGained exp:
                return own.Contains(exp.Pokemon) ? exp : null;
            case LevelRose rose:
                mostHp[rose.Pokemon] = rose.After.HP;
                foreach (var (place, p) in standing.Where(s => ReferenceEquals(s.Value, rose.Pokemon)).ToList()) placeMostHp[place] = rose.After.HP;
                return own.Contains(rose.Pokemon) ? rose : null;
            case MoveWanted wanted:
                return own.Contains(wanted.Pokemon) ? wanted : null;
        }
        return e;
    }

    private void Swap(Place a, Place b)
    {
        bool hasA = standing.TryGetValue(a, out var pa), hasB = standing.TryGetValue(b, out var pb);
        int ma = MostAt(a), mb = MostAt(b);
        standing.Remove(a);
        standing.Remove(b);
        if (hasA) standing[b] = pa!;
        if (hasB) standing[a] = pb!;
        placeMostHp[a] = mb;
        placeMostHp[b] = ma;
    }
}
