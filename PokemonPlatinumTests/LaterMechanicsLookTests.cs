using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The looks of Mega Evolution, Ultra Burst and the Z-Moves (plan 25 · Y14; style guide, "Mega Evolution, Ultra Burst
/// and Z-Moves"): every Z-Move lands with its damage, is over by its cue's end, stays under <see cref="MoveFx.MaxQuads"/>
/// and is its own; the changes of shape and the Z-Power are pure, capped and gentle on the eyes; the screen plays them
/// on R20's and R21's events, takes a new shape inside its light, and arms the item button.
/// </summary>
public class LaterMechanicsLookTests
{
    private const float Frame = 1f / 60f;
    private static readonly FxPlaces Places = new();

    private static IEnumerable<MoveData> ZMoves => MoveDatabase.GetAll().Where(m => m.Kind == MoveKind.ZMove);

    private static EffectCue ZCue(MoveData move, bool critical = false, bool superEffective = false, bool missed = false)
    {
        bool self = move.Category == MoveCategory.Status;
        return new BattleAnimator().Cue(new EffectCue
        {
            Kind = CueKind.Move, Move = move.Name, Type = move.Type, Category = move.Category, ZMove = true,
            FromSide = BattleSide.Player, ToSide = self ? BattleSide.Player : BattleSide.Enemy,
            Critical = critical, SuperEffective = superEffective, Missed = missed
        });
    }

    private static EffectCue Change(CueKind kind, bool primal = false, PokemonType type = PokemonType.Normal, BattleSide side = BattleSide.Enemy) =>
        new BattleAnimator().Cue(new EffectCue { Kind = kind, Primal = primal, Type = type, FromSide = side, ToSide = side });

    private static FxList Draw(EffectCue cue, float age)
    {
        cue.Age = age;
        var fx = new FxList();
        MoveFx.Play(cue, Places, fx);
        return fx;
    }

    private static string Picture(FxList fx) =>
        string.Join(";", fx.Quads.Select(q => $"{q.Shape}{q.Center.X:0.000},{q.Center.Y:0.000},{q.Center.Z:0.000}/{q.Size:0.000}/{q.Color.R},{q.Color.A}"))
        + "|" + string.Join(";", fx.Ribbons.Select(r => $"{r.Width:0.000}/{r.Points.Length}")) + $"|{fx.Flash:0.000}|{fx.Shake:0.000}";

    // ------------------------------------------------------------------ the Z-Moves

    [Fact]
    public void ThereAreThirtyFiveZMovesAndEachHasALookOfItsOwn()
    {
        Assert.Equal(35, ZMoves.Count());
        Assert.All(ZMoves, m => Assert.True(MoveFx.HasZLook(m.Name), $"{m.Name} has no look"));
        Assert.Equal(17, MoveFx.ZSignatureMoves.Count());
    }

    [Fact]
    public void EveryZMoveLandsWithItsDamageWithinAFrame()
    {
        foreach (var move in ZMoves)
        {
            var fx = Draw(ZCue(move), BattleAnimator.ImpactTime);
            Assert.True(Math.Abs(fx.Landed - BattleAnimator.ImpactTime) <= Frame, $"{move.Name} lands at {fx.Landed:0.000}");
        }
    }

    [Fact]
    public void EveryZMoveIsOverByTheEndOfItsCueAndPlaysLongerThanAMove()
    {
        Assert.True(BattleAnimator.ZMoveTime > BattleAnimator.EffectTime);
        foreach (var move in ZMoves)
        {
            Assert.Equal(BattleAnimator.ZMoveTime, ZCue(move).Length);
            foreach (var cue in new[] { ZCue(move), ZCue(move, critical: true, superEffective: true), ZCue(move, missed: true) })
            {
                var fx = Draw(cue, BattleAnimator.ZMoveTime + Frame);
                Assert.True(fx.Count == 0 && fx.Flash == 0f && fx.Shake == 0f, $"{move.Name} still draws {fx.Count} after its cue");
            }
        }
    }

    [Fact]
    public void NoZMoveDrawsMoreThanTheCapFlashesTooHardOrShakesTooMuch()
    {
        var over = new List<string>();
        foreach (var move in ZMoves)
        {
            var cue = ZCue(move, critical: true, superEffective: true);
            for (float age = 0f; age < BattleAnimator.ZMoveTime; age += Frame)
            {
                var fx = Draw(cue, age);
                if (fx.Count > MoveFx.MaxQuads) over.Add($"{move.Name} draws {fx.Count} at {age:0.00}");
                if (fx.Flash > 0.35f) over.Add($"{move.Name} flashes {fx.Flash:0.00} at {age:0.00}");
                if (fx.Shake > 0.5f) over.Add($"{move.Name} shakes {fx.Shake:0.00} at {age:0.00}");
            }
        }
        Assert.Empty(over);
    }

    [Fact]
    public void EveryZMoveIsTheSameForTheSameAgeAndSeed()
    {
        foreach (var move in ZMoves)
        {
            var a = ZCue(move);
            var b = ZCue(move);
            b.Seed = a.Seed;
            foreach (float age in new[] { 0.1f, BattleAnimator.ImpactTime, 0.6f, 1.0f, 1.3f })
                Assert.Equal(Picture(Draw(a, age)), Picture(Draw(b, age)));
        }
    }

    [Fact]
    public void EveryZMoveIsItsOwnAndBiggerThanTheMoveItWasMadeOf()
    {
        // At its set piece's height every Z-Move draws something different from every other, and more than its type's
        // template does at its own landing
        var seen = new Dictionary<string, string>();
        foreach (var move in ZMoves)
        {
            var cue = ZCue(move);
            cue.Seed = 4242;
            var fx = Draw(cue, 0.7f);
            Assert.True(fx.Count > 0, $"{move.Name} draws nothing at 0.7 s");
            string picture = Picture(fx);
            Assert.False(seen.ContainsKey(picture), $"{move.Name} looks like {seen.GetValueOrDefault(picture)}");
            seen[picture] = move.Name;

            if (move.Category == MoveCategory.Status) continue;
            var plain = new EffectCue { Kind = CueKind.Move, Move = "", Type = move.Type, Category = move.Category, FromSide = BattleSide.Player, ToSide = BattleSide.Enemy, Seed = 4242 };
            int template = Enumerable.Range(0, 60).Max(i => Draw(plain, i * Frame).Count);
            int z = Enumerable.Range(0, 90).Max(i => Draw(cue, i * Frame).Count);
            Assert.True(z > template, $"{move.Name} draws at most {z}, its type's template {template}");
        }
    }

    [Fact]
    public void AZMoveIsHeardAsItsTypesZMove()
    {
        Assert.Equal("z_move_normal", SoundBank.MoveSound("Breakneck Blitz", PokemonType.Normal));
        Assert.Equal("z_move_electric", SoundBank.MoveSound("Catastropika", PokemonType.Electric));
        // A move of its own is heard as before
        Assert.Equal("use_tackle", SoundBank.MoveSound("Tackle", PokemonType.Normal));
        foreach (var type in Enum.GetValues<PokemonType>()) Assert.True(SoundBank.Exists(SoundBank.ZMoveSound(type)));
        foreach (var name in new[] { "mega_evolve", "primal_reversion", "ultra_burst", "z_power" })
        {
            Assert.True(SoundBank.Exists(name));
            Assert.True(SoundBank.IsSetPiece(name));
            Assert.InRange(SoundBank.Get(name)!.Duration, 1.0, 2.0);
        }
    }

    // ------------------------------------------------------------------ the changes of shape and the Z-Power

    public static IEnumerable<object[]> Changes =>
    [
        [CueKind.Mega, false, PokemonType.Normal],
        [CueKind.Mega, true, PokemonType.Water],
        [CueKind.Mega, true, PokemonType.Ground],
        [CueKind.UltraBurst, false, PokemonType.Psychic],
        [CueKind.ZPower, false, PokemonType.Fire]
    ];

    [Theory]
    [MemberData(nameof(Changes))]
    public void AChangeIsPureCappedGentleAndOverByItsEnd(CueKind kind, bool primal, PokemonType type)
    {
        foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
        {
            var a = Change(kind, primal, type, side);
            var b = Change(kind, primal, type, side);
            b.Seed = a.Seed;
            bool drew = false;
            for (float age = 0f; age < a.Length; age += Frame)
            {
                var fx = Draw(a, age);
                Assert.True(fx.Count <= MoveFx.MaxQuads, $"{kind} draws {fx.Count} at {age:0.00}");
                Assert.InRange(fx.Flash, 0f, 0.35f);
                Assert.Equal(0f, fx.Shake);
                Assert.Equal(Picture(fx), Picture(Draw(b, age)));
                drew |= fx.Count > 0;
            }
            Assert.True(drew);
            var after = Draw(a, a.Length + Frame);
            Assert.Equal(0, after.Count);
            Assert.Equal(0f, after.Flash);
            Assert.Equal(0f, MoveFx.BodyCast(a).Amount);
        }
    }

    [Fact]
    public void TheBodyTurnsToLightAsItsShapeChanges()
    {
        var mega = Change(CueKind.Mega);
        mega.Age = BattleAnimator.MegaSwapTime;
        Assert.True(MoveFx.BodyCast(mega).Amount > 0.9f);
        mega.Age = 0.05f;
        Assert.Equal(0f, MoveFx.BodyCast(mega).Amount);

        var burst = Change(CueKind.UltraBurst);
        burst.Age = 0.6f;
        var dark = MoveFx.BodyCast(burst);
        Assert.True(dark.Amount > 0.7f && dark.Color.R < 80, "it darkens first");
        burst.Age = BattleAnimator.UltraBurstSwapTime + 0.05f;
        var light = MoveFx.BodyCast(burst);
        Assert.True(light.Amount > 0.9f && light.Color.R > 240, "and turns to light as its shape changes");

        // The Z-Power's cast is no more than a tint
        var z = Change(CueKind.ZPower, type: PokemonType.Water);
        for (float age = 0f; age < BattleAnimator.ZPowerTime; age += Frame)
        {
            z.Age = age;
            Assert.InRange(MoveFx.BodyCast(z).Amount, 0f, 0.31f);
        }
    }

    [Fact]
    public void APrimalReversionLooksOtherThanAMegaEvolution()
    {
        var mega = Change(CueKind.Mega);
        var kyogre = Change(CueKind.Mega, true, PokemonType.Water);
        var groudon = Change(CueKind.Mega, true, PokemonType.Ground);
        kyogre.Seed = groudon.Seed = mega.Seed;
        foreach (float age in new[] { 0.4f, BattleAnimator.MegaSwapTime + 0.1f })
        {
            Assert.NotEqual(Picture(Draw(mega, age)), Picture(Draw(kyogre, age)));
            Assert.NotEqual(Picture(Draw(kyogre, age)), Picture(Draw(groudon, age)));
        }
    }

    // ------------------------------------------------------------------ on screen

    private static Pokemon Holding(Pokemon p, string item)
    {
        p.HeldItem = ItemDatabase.Get(item) ?? throw new ArgumentException(item);
        return p;
    }

    private static BattleEngine Battle(Pokemon mine, string key, string foe = "Snorlax")
    {
        var party = new Party();
        party.Add(mine);
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get(key)!);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag, Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Mon(foe, 50, "Splash") }, Random = Calm(), Rules = Ruleset.Modern
        });
        Settle(battle);
        battle.SelectMainMenuOption(0);
        return battle;
    }

    /// <summary>Reads the battle on to the line that says <paramref name="text"/>, with the line just shown.</summary>
    private static void ReadTo(BattleEngine battle, string text)
    {
        for (int i = 0; i < 600; i++)
        {
            if (battle.CurrentMessage.Contains(text)) return;
            if (battle.IsWaitingForConfirm) battle.ConfirmMessage();
            battle.Update(Frame);
        }
        Assert.Fail($"never said \"{text}\"");
    }

    [Fact]
    public void AMegaEvolutionPlaysItsOwnCueAndTakesItsShapeInsideItsLight()
    {
        var mine = Holding(Mon("Charizard", 50, "Splash"), "Charizardite X");
        var battle = Battle(mine, BattleCore.KeyStoneItem, "Magikarp");
        Assert.Equal((LaterMark.Mega, false), ModernUi.LaterButtonOf(battle));
        Assert.True(battle.ToggleMega());
        Assert.Equal((LaterMark.Mega, true), ModernUi.LaterButtonOf(battle));
        battle.SelectMove(0);
        ReadTo(battle, "Mega Evolved");
        Assert.Contains(battle.Anim.Cues, c => c.Kind == CueKind.Mega && !c.Primal);
        Assert.DoesNotContain(battle.Anim.Cues, c => c.Kind == CueKind.Move && c.Move == "Transform");
        // Not yet: the shape changes inside the light
        Assert.Null(mine.Form);
        for (float t = 0f; t < BattleAnimator.MegaSwapTime + 0.1f; t += Frame) battle.Update(Frame);
        Assert.Equal("Charizard-Mega-X", mine.Form);
    }

    [Fact]
    public void TheZPowerGathersInItsTypesColourAndTheZMovePlaysItsOwnSetPiece()
    {
        var mine = Holding(Mon("Bidoof", 50, "Tackle"), "Normalium Z");
        var battle = Battle(mine, BattleCore.ZRingItem);
        Assert.Equal((LaterMark.ZPower, false), ModernUi.LaterButtonOf(battle));
        // The cards name their moves until the Z-Power is armed, and then their Z-Moves
        Assert.Equal("Tackle", ModernUi.CardName(battle, mine.Moves[0], 0));
        Assert.True(battle.ToggleLaterMechanic());
        Assert.Equal((LaterMark.ZPower, true), ModernUi.LaterButtonOf(battle));
        Assert.Equal("Breakneck Blitz", ModernUi.CardName(battle, mine.Moves[0], 0));
        battle.SelectMove(0);
        ReadTo(battle, "Z-Power");
        var power = Assert.Single(battle.Anim.Cues, c => c.Kind == CueKind.ZPower);
        Assert.Equal(PokemonType.Normal, power.Type);
        Assert.True(battle.Anim.Player.ChargeAge >= 0f, "the Pokémon gathers itself");
        ReadTo(battle, "Breakneck Blitz");
        var z = Assert.Single(battle.Anim.Cues, c => c.Kind == CueKind.Move && c.Move == "Breakneck Blitz");
        Assert.True(z.ZMove);
        Assert.Equal(BattleAnimator.ZMoveTime, z.Length);
    }

    [Fact]
    public void AnUltraBurstPlaysItsOwnCue()
    {
        var necrozma = Holding(Mon("Necrozma", 50, "Tackle"), BattleCore.UltranecroziumZ);
        necrozma.ChangeForm("Necrozma-Dusk");
        var battle = Battle(necrozma, BattleCore.ZRingItem, "Magikarp");
        Assert.Equal((LaterMark.UltraBurst, false), ModernUi.LaterButtonOf(battle));
        Assert.True(battle.ToggleLaterMechanic());
        battle.SelectMove(0);
        ReadTo(battle, "Ultra Burst");
        Assert.Contains(battle.Anim.Cues, c => c.Kind == CueKind.UltraBurst);
        Assert.Equal("Necrozma-Dusk", necrozma.Form);
        for (float t = 0f; t < BattleAnimator.UltraBurstSwapTime + 0.1f; t += Frame) battle.Update(Frame);
        Assert.Equal("Necrozma-Ultra", necrozma.Form);
    }
}
