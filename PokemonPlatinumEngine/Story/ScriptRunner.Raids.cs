using System;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The script's <c>raid</c> (plan 33 · GL3; <c>docs/scripts.md</c>, "Max Raid dens"): a den looked into (what it holds
/// today, written into <c>VAR_GALAR_RAID_STARS</c>, <c>_SPECIES</c> and <c>_LEVEL</c>, RESULT 1 where its beam is lit), the
/// Watts its light gives once a day, its raid fought, and its felled boss caught with a ball of the bag on the host's own
/// die. What a den holds is <see cref="Raids.Today"/>'s, drawn by the day's number and never the dice.
/// </summary>
public sealed partial class ScriptRunner
{
    /// <summary>The variables a den looked into writes, what its lines say by <c>{var:…}</c> and <c>{species:…}</c>.</summary>
    public const string RaidStarsVariable = "VAR_GALAR_RAID_STARS", RaidSpeciesVariable = "VAR_GALAR_RAID_SPECIES", RaidLevelVariable = "VAR_GALAR_RAID_LEVEL";

    /// <summary>The Watts the player holds (plan 33 · GL2).</summary>
    public const string WattsVariable = "VAR_GALAR_WATTS";

    private RaidDenFile? raidDen;
    private RaidToday raidToday;

    /// <summary>The daily flag a den's own word stands in for: its Watts taken today, or a Wishing Piece thrown in today.</summary>
    public static string DenFlag(string den, string what) => $"FLAG_DAILY_GALAR_DEN_{den.ToUpperInvariant()}_{what}";

    private void RaidCommand(Instruction i)
    {
        switch (i.Name)
        {
            case "den":
            {
                raidDen = Raids.Den(i.Other) ?? throw Wrong(i, $"there is no den '{i.Other}' (Data/world/galar/dens.json)");
                int version = Math.Max(1, host.Story.Var("VAR_GALAR_VERSION"));
                bool wished = host.Story.Has(DenFlag(raidDen.Id, "WISHED"));
                raidToday = Raids.Today(raidDen, host.DayNumber, version, host.Story.BadgeCount(host.Region), wished);
                var species = PokemonDatabase.Get(raidToday.Species) ?? PokemonDatabase.SpeciesOfForm(raidToday.Species);
                host.Story.SetVar(RaidStarsVariable, raidToday.Stars);
                host.Story.SetVar(RaidSpeciesVariable, species?.DexNumber ?? 0);
                host.Story.SetVar(RaidLevelVariable, raidToday.Level);
                // A den whose boss was felled today is quiet until tomorrow
                Result = raidToday.Lit && !host.Story.Has(DenFlag(raidDen.Id, "DONE")) ? 1 : 0;
                break;
            }
            case "watts":
            {
                var den = raidDen ?? throw Wrong(i, "no den has been looked into");
                string flag = DenFlag(den.Id, "WATTS");
                if (!raidToday.Lit || host.Story.Has(flag)) { Result = 0; break; }
                int watts = Raids.WattsOf(raidToday.Stars);
                host.Story.Set(flag);
                host.Story.SetVar(WattsVariable, host.Story.Var(WattsVariable) + watts);
                Result = watts;
                break;
            }
            case "fight":
            {
                var den = raidDen ?? throw Wrong(i, "no den has been looked into");
                if (NoPokemonToBattle()) break;
                int version = Math.Max(1, host.Story.Var("VAR_GALAR_VERSION"));
                var boss = Make(raidToday.Species, raidToday.Level, i);
                var allies = Raids.Allies(den, host.DayNumber, version, raidToday.Level);
                host.RaidBattle(boss, new RaidTerms(raidToday.Stars, allies));
                afterBusy = () =>
                {
                    AfterBattle(mayLose: true);
                    if (lastOutcome == BattleOutcome.Won) host.Story.Set(DenFlag(den.Id, "DONE"));
                };
                break;
            }
            case "catch":
            {
                // The felled boss, a ball of the bag thrown at it: RESULT 1 caught, 0 got away, 2 no such ball
                var ball = ItemDatabase.Get(i.Other);
                if (ball == null || host.Bag.GetQuantity(ball) <= 0) { Result = 2; break; }
                host.Bag.RemoveItem(ball, 1);
                bool caught = host.Random(100) < Raids.CatchChance(raidToday.Stars, ball.Name);
                if (caught)
                {
                    var pokemon = Make(raidToday.Species, raidToday.Level, i);
                    pokemon.Ball = ball.Name;
                    host.GivePokemon(pokemon);
                }
                Result = caught ? 1 : 0;
                break;
            }
        }
    }

    /// <summary>A Pokémon of a species or a form at a level, as a den holds it.</summary>
    private static Pokemon Make(string name, int level, Instruction at)
    {
        if (PokemonDatabase.Get(name) is { } plain) return new Pokemon(plain, level);
        var species = PokemonDatabase.SpeciesOfForm(name) ?? throw new ScriptException($"line {at.Line}: there is no species or form '{name}'");
        return Pokemon.InForm(species, name, level);
    }
}
