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

    /// <summary>The item <c>raid wish</c> throws into a den (plan 33 · GL3's second session).</summary>
    public const string WishingPiece = "Wishing Piece";

    /// <summary>The variables a Tera crystal looked into writes (plan 34 · PD7): its stars, its species' number and its level.</summary>
    public const string CrystalStarsVariable = "VAR_PALDEA_RAID_STARS", CrystalSpeciesVariable = "VAR_PALDEA_RAID_SPECIES", CrystalLevelVariable = "VAR_PALDEA_RAID_LEVEL";

    private RaidDenFile? raidDen;
    private RaidToday raidToday;
    // Whether the den looked into is one of Paldea's Tera crystals (plan 34 · PD7), and whether its shards were taken
    private bool raidCrystal, raidShardsTaken;

    /// <summary>The daily flag a den's own word stands in for: its Watts taken today, or a Wishing Piece thrown in today.</summary>
    public static string DenFlag(string den, string what) => $"FLAG_DAILY_GALAR_DEN_{den.ToUpperInvariant()}_{what}";

    /// <summary>The daily flag a Tera crystal felled today sets (plan 34 · PD7): quiet until tomorrow.</summary>
    public static string CrystalFlag(string crystal) => $"FLAG_DAILY_PALDEA_CRYSTAL_{crystal.ToUpperInvariant()}_DONE";

    private void RaidCommand(Instruction i)
    {
        switch (i.Name)
        {
            case "crystal":
            {
                // A Tera crystal of Paldea's open map looked into (plan 34 · PD7): what it holds today written into
                // VAR_PALDEA_RAID_*, {type} its Tera Type in the language chosen, RESULT 1 where its light stands and its
                // boss wasn't felled today
                raidDen = Raids.Crystal(i.Other) ?? throw Wrong(i, $"there is no crystal '{i.Other}' (Data/world/paldea/crystals.json)");
                raidCrystal = true;
                raidShardsTaken = false;
                int version = Math.Max(1, host.Story.Var("VAR_PALDEA_VERSION"));
                raidToday = Raids.CrystalTodayUpTo(raidDen, host.DayNumber, version, Raids.TeraMostStarsOf(host.Story));
                var species = PokemonDatabase.Get(raidToday.Species) ?? PokemonDatabase.SpeciesOfForm(raidToday.Species);
                host.Story.SetVar(CrystalStarsVariable, raidToday.Stars);
                host.Story.SetVar(CrystalSpeciesVariable, species?.DexNumber ?? 0);
                host.Story.SetVar(CrystalLevelVariable, raidToday.Level);
                lastType = TeraWord(raidToday.Tera!.Value);
                Result = raidToday.Lit && !host.Story.Has(CrystalFlag(raidDen.Id)) ? 1 : 0;
                break;
            }
            case "shards":
            {
                // The felled crystal's Tera Shards of its Tera Type into the bag, once (plan 34 · PD7): RESULT how many,
                // {item} the shard; 0 for a den's raid or a second asking
                Result = 0;
                if (!raidCrystal || raidShardsTaken || raidToday.Tera is not { } tera) break;
                var shard = ItemDatabase.Get(TeraChange.ShardOf(tera));
                if (shard == null) break;
                int count = Raids.ShardsOf(raidToday.Stars);
                host.Bag.AddItem(shard, count);
                raidShardsTaken = true;
                lastItem = shard.Name;
                Result = count;
                break;
            }
            case "den":
            {
                raidCrystal = false;
                raidDen = Raids.Den(i.Other) ?? throw Wrong(i, $"there is no den '{i.Other}' (Data/world/galar/dens.json)");
                int version = Math.Max(1, host.Story.Var("VAR_GALAR_VERSION"));
                bool wished = host.Story.Has(DenFlag(raidDen.Id, "WISHED"));
                raidToday = Raids.Today(raidDen, host.DayNumber, version, host.Story.BadgeCount(host.Region), wished);
                var species = PokemonDatabase.Get(raidToday.Species) ?? PokemonDatabase.SpeciesOfForm(raidToday.Species);
                host.Story.SetVar(RaidStarsVariable, raidToday.Stars);
                host.Story.SetVar(RaidSpeciesVariable, species?.DexNumber ?? 0);
                host.Story.SetVar(RaidLevelVariable, raidToday.Level);
                // A den whose boss was felled today is quiet until tomorrow, and so is a den the story hasn't opened
                // (plan 33 · GL7, part b: the Mega Max den waits for Galar's Hall of Fame)
                Result = raidToday.Lit && Raids.IsOpen(raidDen, host.Story.Has) && !host.Story.Has(DenFlag(raidDen.Id, "DONE")) ? 1 : 0;
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
                if (raidCrystal)
                {
                    // A Tera Raid (plan 34 · PD7): the boss in the crystal's Tera Type, three of the academy's students beside the player
                    int paldeaVersion = Math.Max(1, host.Story.Var("VAR_PALDEA_VERSION"));
                    var teraBoss = Make(raidToday.Species, raidToday.Level, i);
                    teraBoss.TeraType = raidToday.Tera!.Value;
                    var students = Raids.Allies(den, host.DayNumber, paldeaVersion, raidToday.Level, Raids.Paldea);
                    host.RaidBattle(teraBoss, new RaidTerms(raidToday.Stars, students, Tera: raidToday.Tera));
                    afterBusy = () =>
                    {
                        AfterBattle(mayLose: true);
                        if (lastOutcome == BattleOutcome.Won) host.Story.Set(CrystalFlag(den.Id));
                    };
                    break;
                }
                int version = Math.Max(1, host.Story.Var("VAR_GALAR_VERSION"));
                var boss = Make(raidToday.Species, raidToday.Level, i);
                // A Mega Max den's boss holds its stone and Mega Dynamaxes by itself (plan 33 · GL7, part b)
                if (raidToday.Item is { } held) boss.HeldItem = ItemDatabase.Get(held) ?? throw Wrong(i, $"there is no item '{held}'");
                var allies = Raids.Allies(den, host.DayNumber, version, raidToday.Level);
                host.RaidBattle(boss, new RaidTerms(raidToday.Stars, allies, MegaMaxDen: den.MegaMax));
                afterBusy = () =>
                {
                    AfterBattle(mayLose: true);
                    if (lastOutcome == BattleOutcome.Won) host.Story.Set(DenFlag(den.Id, "DONE"));
                };
                break;
            }
            case "boss":
            {
                // A boss the story brings (plan 33 · GL6): the form named at its level beside three trainers of the data,
                // a raid that can't be lost where the line says so. What `raid catch` throws at afterwards is the
                // species itself, at the same level and stars, as a den's felled boss is
                if (NoPokemonToBattle()) break;
                var boss = Make(i.Other, i.Number, i);
                var allies = new List<Trainer>();
                foreach (string id in i.Trainers)
                {
                    var record = TrainerDatabase.Get(id, host.Region) ?? throw Wrong(i, $"there is no trainer '{id}'");
                    var ally = new Trainer { Id = record.Id };
                    TrainerDatabase.Fill(ally, record);
                    allies.Add(ally);
                }
                raidDen = null;
                raidCrystal = false;
                raidToday = new RaidToday(boss.Species.Name, i.Numbers[0], i.Number, true);
                host.RaidBattle(boss, new RaidTerms(i.Numbers[0], allies, CannotFail: !i.CanLose));
                afterBusy = () => AfterBattle(mayLose: true);
                break;
            }
            case "wish":
            {
                // A Wishing Piece thrown into the den looked into: RESULT 1 its beam lit for the day (the piece taken from
                // the bag, the den looked into again so its variables say what now waits), 0 no piece in the bag, 2 its
                // beam lit already. The piece's daily flag is what `raid den` reads as wished
                var den = raidDen ?? throw Wrong(i, "no den has been looked into");
                // (a Tera crystal takes no Wishing Piece: plan 34 · PD7)
                if (raidCrystal || !Raids.IsOpen(den, host.Story.Has)) { Result = 0; break; }
                if (raidToday.Lit) { Result = 2; break; }
                var piece = ItemDatabase.Get(WishingPiece);
                if (piece == null || host.Bag.GetQuantity(piece) <= 0) { Result = 0; break; }
                host.Bag.RemoveItem(piece, 1);
                host.Story.Set(DenFlag(den.Id, "WISHED"));
                int version = Math.Max(1, host.Story.Var("VAR_GALAR_VERSION"));
                raidToday = Raids.Today(den, host.DayNumber, version, host.Story.BadgeCount(host.Region), wished: true);
                var species = PokemonDatabase.Get(raidToday.Species) ?? PokemonDatabase.SpeciesOfForm(raidToday.Species);
                host.Story.SetVar(RaidStarsVariable, raidToday.Stars);
                host.Story.SetVar(RaidSpeciesVariable, species?.DexNumber ?? 0);
                host.Story.SetVar(RaidLevelVariable, raidToday.Level);
                Result = 1;
                break;
            }
            case "catch":
            {
                // The felled boss, a ball of the bag thrown at it: RESULT 1 caught, 0 got away, 2 no such ball
                var ball = ItemDatabase.Get(i.Other);
                if (ball == null || host.Bag.GetQuantity(ball) <= 0) { Result = 2; break; }
                host.Bag.RemoveItem(ball, 1);
                // A felled Tera boss is always taken, and draws nothing (plan 34 · PD7)
                bool tera = raidToday.Tera != null;
                bool caught = tera || host.Random(100) < Raids.CatchChance(raidToday.Stars, ball.Name);
                if (caught)
                {
                    var pokemon = Make(raidToday.Species, raidToday.Level, i);
                    if (raidToday.Tera is { } teraType) pokemon.TeraType = teraType;
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
