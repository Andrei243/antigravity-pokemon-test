using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim.Link;

/// <summary>
/// A link client's view made into what a battle screen plays (plan 28 · I3, the screen's half). Every battle screen
/// stands on the player's side, so side B's view is turned round (<see cref="BattleWire.Turned(IReadOnlyList{BattleEvent}, BattleRoster)"/>):
/// its own places are the player's on its screen, the HUD seen from whoever is choosing. A foe's HP comes in the bar's
/// 48ths (<see cref="SideView.BarPixels"/>); the screen draws a bar from a Pokémon's HP and its most, so a foe's is put
/// on its stand-in's own scale, the bar's share kept (nothing finer than the 48ths can be read off it). Questions and
/// answers are turned the same way. GPU-free: the tests play it with no screen.
/// </summary>
public sealed class LinkScreenFeed
{
    public LinkClient Client { get; }

    /// <summary>Whether this screen's view is turned round: the player is side B, the core's other side.</summary>
    public bool Turned => Client.Side == BattleSide.Enemy;

    private int read;
    private readonly Dictionary<Place, Pokemon> standing = new();
    private int askedRead = -1;

    public LinkScreenFeed(LinkClient client) => Client = client;

    /// <summary>The events that came since the last call, as the screen plays them.</summary>
    public List<BattleEvent> TakeEvents()
    {
        var events = Client.Events;
        if (read >= events.Count || Client.Roster == null) return new List<BattleEvent>();
        var fresh = events.Skip(read).ToList();
        read = events.Count;
        var screen = Turned ? BattleWire.Turned(fresh, Client.Roster) : fresh;
        return screen.Select(Scaled).ToList();
    }

    /// <summary>The question standing, as the screen asks it (its places on the player's side), once each: null when none is new.</summary>
    public LinkAsk? TakeAsk()
    {
        if (Client.Asked is not { } ask || ask.Number == askedRead || Client.Roster == null) return null;
        askedRead = ask.Number;
        return Screen(ask);
    }

    /// <summary>The question standing now as the screen asks it, whether or not it was taken.</summary>
    public LinkAsk? Standing => Client.Asked is { } ask && Client.Roster != null ? Screen(ask) : null;

    private LinkAsk Screen(LinkAsk ask) => Turned
        ? ask with { Request = BattleWire.Turned(ask.Request, Client.Roster!), Options = ask.Options.Select(BattleWire.Turned).ToList() }
        : ask;

    /// <summary>Answers the question with the screen's choices, turned back to the battle's own places.</summary>
    public void Answer(IReadOnlyList<BattleChoice> choices) =>
        Client.Answer(Turned ? choices.Select(BattleWire.Turned).ToList() : choices.ToList());

    /// <summary>A foe's HP in the bar's 48ths, on its stand-in's scale: a share of none stays none, any other at least 1.</summary>
    public static int OnItsScale(int bar, int most) =>
        bar <= 0 || most <= 0 ? 0 : System.Math.Min(most, System.Math.Max(1, (bar * most + SideView.BarPixels - 1) / SideView.BarPixels));

    private BattleEvent Scaled(BattleEvent e)
    {
        switch (e)
        {
            case Said said:
                var line = new Said(said.Text);
                foreach (var inner in said.Shows) line.Shows.Add(Scaled(inner));
                foreach (var inner in said.OnImpact) line.OnImpact.Add(Scaled(inner));
                return line;
            case Entered entered:
                standing[entered.Place] = entered.Pokemon;
                return e;
            case Rotated rotated:
                standing[rotated.Place] = rotated.Pokemon;
                return e;
            case PlacesSwapped swapped:
                bool hasA = standing.Remove(swapped.First, out var a), hasB = standing.Remove(swapped.Second, out var b);
                if (hasA) standing[swapped.Second] = a!;
                if (hasB) standing[swapped.First] = b!;
                return e;
            case Struck struck when IsFoe(struck.Place):
                return struck with { Hp = OnItsScale(struck.Hp, MostAt(struck.Place)) };
            case HpChanged changed when IsFoe(changed.Place):
                return changed with { Hp = OnItsScale(changed.Hp, MostAt(changed.Place)) };
            case Dynamaxed big when IsFoe(big.Place):
                return big with { MaxHp = MostAt(big.Place), Hp = OnItsScale(big.Hp, MostAt(big.Place)) };
            case DynamaxEnded small when IsFoe(small.Place):
                return small with { MaxHp = MostAt(small.Place), Hp = OnItsScale(small.Hp, MostAt(small.Place)) };
            case FormHpChanged form when IsFoe(form.Place):
                return form with { MaxHp = MostAt(form.Place), Hp = OnItsScale(form.Hp, MostAt(form.Place)) };
            case Terastallized tera when tera.MaxHp > 0 && IsFoe(tera.Place):
                return tera with { MaxHp = MostAt(tera.Place), Hp = OnItsScale(tera.Hp, MostAt(tera.Place)) };
        }
        return e;
    }

    // On the screen the player's side is the player's own; every place of the other side is a foe's
    private static bool IsFoe(Place place) => place.Side == BattleSide.Enemy;

    private int MostAt(Place place) => standing.TryGetValue(place, out var p) ? p.MaxHP : SideView.BarPixels;
}
