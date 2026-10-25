using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// Rotation battles (plan 14 · B7; <c>docs/mechanics/rulings.md</c>, "Rotation battles"): Black and White's. Each side
/// has one place, the front of a turntable of three seats; the Pokémon on the other two seats wait behind it, each on a
/// battler of its own (<see cref="wheel"/>) that keeps whatever lasted on it at the front (its stages, which are its
/// own, a doll, a confusion, a Leech Seed's seed), since a rotation is no switch. A move may be chosen for the Pokémon of
/// either seat behind (<see cref="BattleChoice.Rotation"/>): the turntable turns as the turn begins, in order of Speed,
/// and that Pokémon uses it. Only the one in front is on the field, so the end of the turn, the entry checks and
/// everything aimed at a place reach it alone; a Pokémon first brought forward has its entry checks then.
/// </summary>
public sealed partial class BattleCore
{
    /// <summary>A rotation battle (<see cref="BattleFormat.Rotation"/>): one place a side, the front of a turntable of three.</summary>
    public bool IsRotation => Format == BattleFormat.Rotation;

    // The two seats behind each side's front: [side][0] the seat after the front (a turn of +1 brings it forward),
    // [side][1] the seat before it (−1). Null outside a rotation battle
    private Battler[][]? wheel;

    /// <summary>
    /// Lays out each side's turntable as the battle is made: the next two of the team able to fight, after the one in
    /// front, sit behind it. All three are out from the start, so all three count as entering at the opening (Fake Out
    /// works on the first turn alone, whichever seat it is used from), and the player's three as having taken part.
    /// </summary>
    private void SetUpTheTurntables()
    {
        if (!IsRotation) return;
        wheel = new Battler[2][];
        for (int s = 0; s < 2; s++)
        {
            var side = (BattleSide)s;
            var front = SlotsOf(side)[0];
            var behind = front.Roster?.Members.Where(p => !p.IsFainted && p != front.Pokemon).Take(2).ToList() ?? new List<Pokemon>();
            wheel[s] = new Battler[2];
            for (int seat = 0; seat < 2; seat++)
            {
                var b = new Battler(side, 0) { Roster = front.Roster, Trainer = front.Trainer, Field = Field, Pokemon = seat < behind.Count ? behind[seat] : null };
                if (b.Pokemon != null)
                {
                    b.Pokemon.ResetStatStages();
                    CountFrom(b, opening: true);
                    if (side == BattleSide.Player) sentOut.Add(b.Pokemon);
                }
                wheel[s][seat] = b;
            }
        }
    }

    /// <summary>The battlers of the seats behind both fronts (none outside a rotation battle).</summary>
    private IEnumerable<Battler> WheelBattlers => wheel == null ? Enumerable.Empty<Battler>() : wheel.SelectMany(w => w);

    /// <summary>The Pokémon on a side's seats behind its front: on the turntable, and so never sent in from the team.</summary>
    private IEnumerable<Pokemon> OnTheWheel(BattleSide side) =>
        wheel == null ? Enumerable.Empty<Pokemon>() : wheel[(int)side].Where(b => b.Pokemon != null).Select(b => b.Pokemon!);

    /// <summary>
    /// A side's turntable as it stands, front first: the Pokémon in front, the one a turn of +1 brings forward, the one
    /// a turn of −1 brings forward (null on a seat that has nobody). Empty outside a rotation battle.
    /// </summary>
    public IReadOnlyList<Pokemon?> Turntable(BattleSide side) =>
        wheel == null ? Array.Empty<Pokemon?>() : new[] { SlotsOf(side)[0].Pokemon, wheel[(int)side][0].Pokemon, wheel[(int)side][1].Pokemon };

    /// <summary>The battler a turn of <paramref name="way"/> brings forward to <paramref name="front"/>'s place: the front itself for 0.</summary>
    private Battler? Seat(Battler front, int way) =>
        wheel == null ? null : way == 0 ? front : way > 0 ? wheel[(int)front.Side][0] : wheel[(int)front.Side][1];

    /// <summary>
    /// Turns a side's turntable one seat (+1 brings the seat after the front forward, −1 the one before): the three
    /// battlers trade everything they hold as Ally Switch's <see cref="Exchange"/> trades two places, all on one place,
    /// so nothing aimed at the front is moved and nothing on the Pokémon is lost. Turned and turned back it is as it was.
    /// </summary>
    private void Spin(Battler front, int way)
    {
        var (after, before) = (wheel![(int)front.Side][0], wheel[(int)front.Side][1]);
        var none = Enumerable.Empty<Battler>();
        if (way > 0) Exchange(front, after, none);
        else Exchange(front, before, none);
        Exchange(after, before, none);
    }

    /// <summary>What <paramref name="ask"/> gives with the turntable turned <paramref name="way"/> for the asking, turned back after.</summary>
    internal T WithRotated<T>(Battler front, int way, Func<T> ask)
    {
        if (way == 0 || wheel == null) return ask();
        Spin(front, way);
        try { return ask(); }
        finally { Spin(front, -way); }
    }

    /// <summary>The Pokémon a turn of <paramref name="way"/> would bring forward to this place (the one in front for 0), or null.</summary>
    public Pokemon? RotatesIn(Battler b, int way) => Seat(At(b.Place), way)?.Pokemon;

    /// <summary>
    /// Why a move chosen with a turn of the turntable can't be had (null when it can): only in a rotation battle, by
    /// one seat either way, with a move (no Mega Evolution, Z-Move, Dynamax or Terastallization beside it), from a
    /// front not held to its move or giant, to a seat whose Pokémon can fight; and the move is held to what the Pokémon
    /// brought forward may use (a Disable, a Choice item's lock, its PP), as if it stood in front.
    /// </summary>
    public string? WhyNotRotation(BattleChoice choice)
    {
        var b = At(choice.Who);
        if (choice.Rotation == 0) return null;
        string cant = Text.Of("battle.cantRotate", ("pokemon", b.Name));
        if (!IsRotation || choice.Rotation is not (1 or -1) || choice.Kind != ChoiceKind.Fight || choice.Move == BattleChoice.HeldMove) return cant;
        if (choice.Mega || choice.ZMove || choice.UltraBurst || choice.Dynamax || choice.Terastallize) return cant;
        if (!b.IsActive || b.IsHeldToItsMove || b.Volatile.Dynamaxed) return cant;
        if (Seat(b, choice.Rotation)?.Pokemon is not { IsFainted: false }) return cant;
        return WithRotated(b, choice.Rotation, () => WhyNot(choice with { Rotation = 0 }));
    }

    /// <summary>
    /// The turntables turn as the turn begins, before anything else is done (Black and White's): each turn chosen, the
    /// fastest front first, said with "X rotated to the front!" and its <see cref="Rotated"/>, and what the Pokémon
    /// brings with it. A Pokémon brought forward for the first time has its entry checks then.
    /// </summary>
    private void TurnTheTurntables(IReadOnlyList<BattleChoice> choices)
    {
        if (!IsRotation) return;
        var turning = choices.Where(c => c.Rotation != 0 && c.Kind == ChoiceKind.Fight)
            .Select(c => (Choice: c, Front: At(c.Who))).Where(t => t.Front.IsActive)
            .OrderByDescending(t => EffectiveSpeed(t.Front)).ThenBy(t => t.Front.Place.Number).ToList();
        foreach (var (choice, front) in turning)
        {
            if (Seat(front, choice.Rotation)?.Pokemon is not { IsFainted: false }) continue;
            BringForward(front, choice.Rotation);
        }
        if (turning.Count > 0) SwitchInChecks();
    }

    /// <summary>Turns the turntable and says so, with what the Pokémon brought forward has on it.</summary>
    private void BringForward(Battler front, int way)
    {
        Spin(front, way);
        var p = front.Pokemon!;
        var line = Say(Text.Of("battle.rotatedToTheFront", ("pokemon", front.Name)))
            .With(new Rotated(front.Place, way, p, front.HasSubstitute))
            .With(new CameInWith(front.Place, p.Status == StatusCondition.Faint ? StatusCondition.None : p.Status))
            .With(Passing(front));
        if (!front.IsPlayerSide) line.With(Seen.Of(p));
    }

    /// <summary>
    /// A front that fainted with nobody left on the team to send in for it (<see cref="ReplaceFainted"/> sends from the
    /// team first, as Black and White do) is turned away: the seat after it if its Pokémon can fight, else the one before.
    /// </summary>
    private void TurnPastTheFallen()
    {
        if (!IsRotation || Result != BattleResult.None) return;
        bool turned = false;
        foreach (var front in PlayerSlots.Take(1).Concat(EnemySlots.Take(1)))
        {
            if (front.IsActive) continue;
            int way = Seat(front, 1)?.Pokemon is { IsFainted: false } ? 1 : Seat(front, -1)?.Pokemon is { IsFainted: false } ? -1 : 0;
            if (way == 0) continue;
            BringForward(front, way);
            turned = true;
        }
        if (turned) SwitchInChecks();
    }
}
