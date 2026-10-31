using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The Gigantamax signatures, parts a and b (plan 25 · YD7; style guide, "The Gigantamax signatures"): ten G-Max Moves with
/// set pieces of their own, held as Y14 holds its batches (each lands with its damage, is over by its cue's end, stays
/// under <see cref="MoveFx.MaxQuads"/> with the Max Moves' shared beats at every frame from both sides, is gentle on
/// the eyes, pure and its own), each with a sound of its own; and every Gigantamax growth's clouds in its own G-Max
/// Move's colours, from the form the rules hand the screen.
/// </summary>
public class GMaxSignatureTests
{
    private const float Frame = 1f / 60f;
    private static readonly FxPlaces Places = new();

    private static readonly string[] PartA = { "G-Max Wildfire", "G-Max Volt Crash", "G-Max Terror", "G-Max Resonance", "G-Max Replenish" };
    private static readonly string[] PartB = { "G-Max Chi Strike", "G-Max Gold Rush", "G-Max Cuddle", "G-Max Befuddle", "G-Max Depletion" };
    private static readonly string[] Ten = PartA.Concat(PartB).ToArray();

    private static MoveData Move(string name) => MoveDatabase.Get(name) ?? throw new ArgumentException(name);

    private static EffectCue Cue(string name, BattleSide from = BattleSide.Player, bool critical = false, bool missed = false, bool onGuard = false)
    {
        var move = Move(name);
        var to = from == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;
        return new BattleAnimator().Cue(new EffectCue
        {
            Kind = CueKind.Move, Move = move.Name, Type = move.Type, Category = move.Category, MaxMove = true,
            FromSide = from, ToSide = to, Critical = critical, SuperEffective = critical, Missed = missed, Blocked = onGuard, OnGuard = onGuard
        });
    }

    private static FxList Draw(EffectCue cue, float age)
    {
        cue.Age = age;
        var fx = new FxList();
        MoveFx.Play(cue, Places, fx);
        return fx;
    }

    private static string Picture(FxList fx) =>
        string.Join(";", fx.Quads.Select(q => $"{q.Shape}{q.Center.X:0.000},{q.Center.Y:0.000},{q.Center.Z:0.000}/{q.Size:0.000}/{q.Color.R},{q.Color.G},{q.Color.B},{q.Color.A}"))
        + "|" + string.Join(";", fx.Ribbons.Select(r => $"{r.Width:0.000}/{r.Points.Length}/{r.Color.R},{r.Color.G}")) + $"|{fx.Flash:0.000}|{fx.Shake:0.000}";

    [Fact]
    public void TheTenSignaturesAreGMaxMovesAndNothingElseHasOne()
    {
        Assert.Equal(Ten.OrderBy(n => n), MoveFx.GMaxSignatureMoves.OrderBy(n => n));
        Assert.All(Ten, n => Assert.Equal(MoveKind.GMaxMove, Move(n).Kind));
    }

    [Fact]
    public void ResonancesCurtainsStandUpAsFoldsNotAlongTheirLength()
    {
        // Part a's first curtains were ribbons along the curtain's line, which read as streaks across the trees: each
        // curtain is upright folds now and nothing runs along its length (a thin seam there read as a streak too).
        var cue = Cue("G-Max Resonance");
        var fx = Draw(cue, 0.9f);
        var folds = fx.Quads.Where(q => q.Shape == FxShape.Glow && q.Stretch >= 2.5f && q.Along.Y > 0.99f).ToList();
        Assert.True(folds.Count >= 24, $"{folds.Count} upright folds");
        Assert.DoesNotContain(fx.Ribbons, r => r.Points.Length >= 8);
    }

    [Fact]
    public void EverySignatureLandsWithItsDamageAndIsOverByItsCuesEnd()
    {
        foreach (var name in Ten)
            foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
            {
                var fx = Draw(Cue(name, side), BattleAnimator.ImpactTime);
                Assert.True(Math.Abs(fx.Landed - BattleAnimator.ImpactTime) <= Frame, $"{name} lands at {fx.Landed:0.000}");
                foreach (var cue in new[] { Cue(name, side), Cue(name, side, critical: true), Cue(name, side, missed: true), Cue(name, side, onGuard: true) })
                {
                    Assert.Equal(BattleAnimator.MaxMoveTime, cue.Length);
                    var after = Draw(cue, BattleAnimator.MaxMoveTime + Frame);
                    Assert.True(after.Count == 0 && after.Flash == 0f && after.Shake == 0f, $"{name} still draws {after.Count} after its cue");
                }
            }
    }

    [Fact]
    public void NoSignatureDrawsMoreThanTheCapFlashesTooHardShakesTooMuchOrRisesTooHigh()
    {
        var over = new List<string>();
        foreach (var name in Ten)
            foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
                foreach (var cue in new[] { Cue(name, side, critical: true), Cue(name, side, onGuard: true) })
                {
                    var target = cue.ToSide;
                    float h = Places.Height(target, 0), top = Places.Feet(target, 0).Y + h * 1.3f;
                    float flashing = 0f;
                    // The Max Moves' shared landing flashes for as long as it does; a signature adds no longer flash
                    string typeMove = MoveDatabase.GetAll().First(m => m.Kind == MoveKind.MaxMove && m.Type == Move(name).Type && m.Name != "Max Guard").Name;
                    var typed = Cue(typeMove, side, critical: true);
                    float typedFlashing = 0f;
                    for (float age = 0f; age < BattleAnimator.MaxMoveTime; age += Frame) if (Draw(typed, age).Flash > 0f) typedFlashing += Frame;
                    for (float age = 0f; age < BattleAnimator.MaxMoveTime; age += Frame)
                    {
                        var fx = Draw(cue, age);
                        if (fx.Count > MoveFx.MaxQuads) over.Add($"{name} from {side} draws {fx.Count} at {age:0.00}");
                        if (fx.Flash > 0.35f) over.Add($"{name} flashes {fx.Flash:0.00} at {age:0.00}");
                        if (fx.Shake > 0.5f) over.Add($"{name} shakes {fx.Shake:0.00} at {age:0.00}");
                        if (fx.Flash > 0f) flashing += Frame;
                        var high = fx.Quads.FirstOrDefault(q => q.Size >= h * 0.25f && q.Center.Y > top + 0.01f && Math.Abs(q.Center.X - Places.Feet(target, 0).X) < 4f);
                        if (age > 0.6f && high.Size > 0f) over.Add($"{name} rises over 1.3 h ({high.Shape}) at {age:0.00}");
                    }
                    if (flashing > Math.Max(1f / 3f, typedFlashing) + Frame / 2f) over.Add($"{name} flashes for {flashing:0.00} s ({typeMove} {typedFlashing:0.00})");
                }
        Assert.True(over.Count == 0, string.Join(" | ", over.Distinct().Take(12)));
    }

    [Fact]
    public void EverySignatureIsPureAndItsOwnNotItsTypesMaxMoveNorItsOldMark()
    {
        var seen = new Dictionary<string, string>();
        foreach (var name in Ten)
        {
            var a = Cue(name);
            var b = Cue(name);
            a.Seed = b.Seed = 4242;
            foreach (float age in new[] { 0.1f, BattleAnimator.ImpactTime, 0.7f, 1.2f })
                Assert.Equal(Picture(Draw(a, age)), Picture(Draw(b, age)));
            string picture = Picture(Draw(a, 0.7f));
            Assert.False(seen.ContainsKey(picture), $"{name} looks like {seen.GetValueOrDefault(picture)}");
            seen[picture] = name;
            // Not its type's Max Move at the set piece
            string typeMove = MoveDatabase.GetAll().First(m => m.Kind == MoveKind.MaxMove && m.Type == Move(name).Type && m.Name != "Max Guard").Name;
            var typed = Cue(typeMove);
            typed.Seed = 4242;
            Assert.NotEqual(picture, Picture(Draw(typed, 0.7f)));
            Assert.True(Draw(a, 0.7f).Count > 0, $"{name} draws nothing at 0.7 s");
        }
    }

    [Fact]
    public void EachSignatureIsHeardByItsOwnSoundAndTheRestByTheirTypes()
    {
        foreach (var name in Ten)
        {
            string sound = SoundBank.GMaxSound(name);
            Assert.StartsWith("gmax_", sound);
            Assert.Equal(sound, SoundBank.MoveSound(name, Move(name).Type));
            Assert.True(SoundBank.Exists(sound), sound);
            Assert.True(SoundBank.IsSetPiece(sound));
            Assert.InRange(SoundBank.Get(sound)!.Duration, 1.0, 2.0);
        }
        foreach (var name in MoveFx.GMaxMoves.Except(Ten))
            Assert.Equal(SoundBank.MaxMoveSound(Move(name).Type), SoundBank.MoveSound(name, Move(name).Type));
        Assert.Equal("", SoundBank.GMaxSound("Max Flare"));
    }

    // ------------------------------------------------------------------ the growth's own colours

    [Fact]
    public void EveryGigantamaxFormNamesItsGMaxMoveAndNothingElseDoes()
    {
        var forms = PokemonDatabase.Everyone().SelectMany(s => s.Forms ?? new()).Where(f => f.Kind == FormKind.Gigantamax).Select(f => f.Name).ToList();
        Assert.True(forms.Count >= 34, $"{forms.Count} Gigantamax forms");
        foreach (var form in forms)
        {
            var move = BattleAnimator.GMaxOfForm(form);
            Assert.True(move != null, $"{form} names no G-Max Move");
            Assert.Equal(MoveKind.GMaxMove, Move(move!).Kind);
            Assert.NotNull(MoveFx.GMaxGrowthColours(move));
        }
        Assert.Equal("G-Max Wildfire", BattleAnimator.GMaxOfForm("Charizard-Gmax"));
        Assert.Equal("G-Max Stun Shock", BattleAnimator.GMaxOfForm("Toxtricity-Low-Key-Gmax"));
        Assert.Equal("G-Max One Blow", BattleAnimator.GMaxOfForm("Urshifu-Single-Strike-Gmax"));
        Assert.Equal("G-Max Rapid Flow", BattleAnimator.GMaxOfForm("Urshifu-Rapid-Strike-Gmax"));
        Assert.Null(BattleAnimator.GMaxOfForm("Charizard-Mega-X"));
        Assert.Null(BattleAnimator.GMaxOfForm(null));
        Assert.Null(MoveFx.GMaxGrowthColours(null));
    }

    [Fact]
    public void EveryGigantamaxGrowthIsCappedPureAndInItsOwnColours()
    {
        foreach (var gmax in MoveFx.GMaxMoves)
            foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
                foreach (var kind in new[] { CueKind.Dynamax, CueKind.DynamaxEnd })
                {
                    EffectCue Make() => new BattleAnimator().Cue(new EffectCue { Kind = kind, Gigantamax = true, GMax = gmax, FromSide = side, ToSide = side });
                    var a = Make();
                    var b = Make();
                    b.Seed = a.Seed;
                    for (float age = 0f; age < a.Length; age += Frame * 2f)
                    {
                        var fx = Draw(a, age);
                        Assert.True(fx.Count <= MoveFx.MaxQuads, $"{gmax}'s {kind} draws {fx.Count} at {age:0.00}");
                        Assert.InRange(fx.Flash, 0f, 0.35f);
                        Assert.Equal(0f, fx.Shake);
                        Assert.Equal(Picture(fx), Picture(Draw(b, age)));
                    }
                    Assert.Equal(0, Draw(a, a.Length + Frame).Count);
                }
        // A growth's clouds at the gathering are in its own colours, not the Dynamax's red, and differ by the move
        var pictures = new Dictionary<string, string>();
        foreach (var gmax in MoveFx.GMaxMoves)
        {
            var cue = new BattleAnimator().Cue(new EffectCue { Kind = CueKind.Dynamax, Gigantamax = true, GMax = gmax, FromSide = BattleSide.Enemy, ToSide = BattleSide.Enemy });
            cue.Seed = 77;
            var red = new BattleAnimator().Cue(new EffectCue { Kind = CueKind.Dynamax, Gigantamax = true, FromSide = BattleSide.Enemy, ToSide = BattleSide.Enemy });
            red.Seed = 77;
            string p = Picture(Draw(cue, 0.4f));
            Assert.NotEqual(Picture(Draw(red, 0.4f)), p);
            var (ca, cb, _) = MoveFx.GMaxGrowthColours(gmax)!.Value;
            Assert.Contains(Draw(cue, 0.4f).Quads, q => q.Shape == FxShape.Smoke && q.Color.R == ca.R && q.Color.G == ca.G && q.Color.B == ca.B);
            Assert.Contains(Draw(cue, 0.4f).Quads, q => q.Shape == FxShape.Smoke && q.Color.R == cb.R && q.Color.G == cb.G && q.Color.B == cb.B);
            Assert.False(pictures.ContainsKey(p), $"{gmax}'s growth looks like {pictures.GetValueOrDefault(p)}'s");
            pictures[p] = gmax;
        }
    }

    [Fact]
    public void AGigantamaxOnScreenCarriesItsMovesColoursFromItsGrowthToItsWreathAndItsEnd()
    {
        var charizard = Mon("Charizard", 50, "Flamethrower");
        charizard.DynamaxLevel = 10;
        charizard.GigantamaxFactor = true;
        var party = new Party();
        party.Add(charizard);
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get(BattleCore.DynamaxBandItem)!);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag, Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Mon("Magikarp", 50, "Splash") }, Random = Calm(), Rules = Ruleset.Modern
        });
        Settle(battle);
        battle.SelectMainMenuOption(0);
        Assert.True(battle.ToggleLaterMechanic());
        battle.SelectMove(0);
        for (int i = 0; i < 600 && !battle.Anim.Cues.Any(c => c.Kind == CueKind.Dynamax); i++)
        {
            if (battle.IsWaitingForConfirm) battle.ConfirmMessage();
            battle.Update(Frame);
        }
        var grow = Assert.Single(battle.Anim.Cues, c => c.Kind == CueKind.Dynamax);
        Assert.Equal("G-Max Wildfire", grow.GMax);
        Assert.Equal("G-Max Wildfire", battle.Anim.Player.GMax);
        // Settled at a menu from the rules' own form, it keeps them
        battle.Anim.SettleDynamax(BattleSide.Player, 0, true, 2, true, false, BattleAnimator.GMaxOfForm("Charizard-Gmax"));
        Assert.Equal("G-Max Wildfire", battle.Anim.Player.GMax);
        // ...and its end takes them with it
        var end = battle.Anim.Undynamax(BattleSide.Player, 0);
        Assert.Equal("G-Max Wildfire", end!.GMax);
        Assert.Null(battle.Anim.Player.GMax);
        // A plain Dynamax has none
        var anim = new BattleAnimator();
        anim.Dynamax(BattleSide.Enemy, 0, gigantamax: false, played: false, gmax: "G-Max Wildfire");
        Assert.Null(anim.Enemy.GMax);
    }
}
