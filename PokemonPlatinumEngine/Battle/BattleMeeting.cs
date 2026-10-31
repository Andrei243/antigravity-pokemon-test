using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

/// <summary>A trainer who is met: the class and the name <c>trainers.json</c> gives them (Leader, Roark).</summary>
public readonly record struct MetTrainer(string Class, string Name, string Id = "");

/// <summary>
/// Whom a meeting is with, as the original sorts them before it picks the way into the battle and its music
/// (<c>enum EncEffectsPairID</c> of <c>src/enc_effects.c</c>, without the rows that tell one Gym Leader or one
/// legendary Pokémon from another: a Leader's and an Elite Four's name is beside their class here, and the wild
/// species' rows are the original's groups, as its table gives each group its way in and its music; plan 09 · L14).
/// </summary>
public enum MeetingRow
{
    /// <summary>A Gym Leader alone (<c>ENCEFF_LEADER_…</c>).</summary>
    Leader,
    /// <summary>One of the Elite Four (<c>ENCEFF_ELITE_FOUR_…</c>).</summary>
    EliteFour,
    /// <summary>The Champion (<c>ENCEFF_CHAMPION_CYNTHIA</c>).</summary>
    Champion,
    /// <summary>The rival (<c>ENCEFF_RIVAL</c>): met as anyone is, to music of his own.</summary>
    Rival,
    /// <summary>A Team Galactic grunt (<c>ENCEFF_GALACTIC_GRUNT</c>).</summary>
    GalacticGrunt,
    /// <summary>A Commander (<c>ENCEFF_GALACTIC_CMDR</c>).</summary>
    GalacticCommander,
    /// <summary>The boss (<c>ENCEFF_GALACTIC_CYRUS</c>).</summary>
    GalacticBoss,
    /// <summary>A battle of the Battle Frontier (<c>ENCEFF_FRONTIER</c>).</summary>
    Frontier,
    /// <summary>A link battle (<c>ENCEFF_LINK_BATTLE</c>).</summary>
    Link,
    /// <summary>Two Pokémon a side against trainers (<c>ENCEFF_DOUBLE_BATTLE</c>).</summary>
    DoubleBattle,
    /// <summary>Two wild Pokémon at once (<c>ENCEFF_DOUBLE_WILD</c>).</summary>
    DoubleWild,
    /// <summary>A Frontier Brain, in the Frontier or out of it (<c>ENCEFF_FRONTIER_BRAIN</c>).</summary>
    FrontierBrain,
    /// <summary>Volkner in a double battle (<c>ENCEFF_DOUBLE_LEADER</c>): the double's way, to a Leader's music.</summary>
    DoubleLeader,
    /// <summary>Any other trainer (<c>ENCEFF_NORMAL_TRAINER</c>).</summary>
    Trainer,
    /// <summary>A wild Pokémon (<c>ENCEFF_NORMAL_WILD</c>).</summary>
    Wild,

    // The wild species with rows of their own (plan 09 · L14; EncEffects_WildPokemonEffect)

    /// <summary>Shaymin (<c>ENCEFF_SHAYMIN</c>): the mythical's way.</summary>
    Shaymin,
    /// <summary>Dialga or Palkia (<c>ENCEFF_DIALGA_PALKIA</c>): the legendary's way.</summary>
    DialgaPalkia,
    /// <summary>Uxie or Azelf (<c>ENCEFF_UXIE_AZELF</c>): the legendary's way.</summary>
    UxieAzelf,
    /// <summary>Mesprit (<c>ENCEFF_MESPRIT</c>): met as anyone is, to the lakes' guardians' music.</summary>
    Mesprit,
    /// <summary>Arceus (<c>ENCEFF_ARCEUS</c>): the legendary's way.</summary>
    Arceus,
    /// <summary>Regigigas, Heatran, Darkrai or Rotom (<c>ENCEFF_MINOR_LEGENDARIES</c>): the mythical's way.</summary>
    MinorLegendary,
    /// <summary>Cresselia (<c>ENCEFF_CRESSELIA</c>): met as anyone is.</summary>
    Cresselia,
    /// <summary>Articuno, Zapdos or Moltres outside Pal Park (<c>ENCEFF_KANTO_BIRDS</c>): met as anyone is, to the legendary music.</summary>
    KantoBirds,
    /// <summary>Giratina (<c>ENCEFF_GIRATINA</c>): the mythical's way.</summary>
    Giratina,
    /// <summary>Regirock, Regice or Registeel outside Pal Park (<c>ENCEFF_REGI_TRIO</c>): the mythical's way.</summary>
    RegiTrio
}

/// <summary>The ground a way into a battle goes by (<c>CutInEffects_ForBattle</c>): water, a cave's floor, and everything else.</summary>
public enum MeetingGround { Grass, Water, Cave }

/// <summary>
/// What is known of a battle as it begins in the field, which is what the way into it is picked from (plan 09 ·
/// L13; the original's <c>FieldBattleDTO</c>, for the fields <c>src/enc_effects.c</c> and
/// <c>CutInEffects_ForBattle</c> read): the trainers met or the wild Pokémon's species, whether two fight a side,
/// the kind of battle, whether it is the Battle Frontier's or a link battle, the ground it is fought on, and the
/// level of the first Pokémon each side sends out. No screen, sound or clock: <c>SceneTransition.ForBattle</c>
/// turns it into a way in.
/// </summary>
public sealed record BattleMeeting
{
    /// <summary>The trainers met, the first first; none in a battle with wild Pokémon. The original reads the first's class alone (<c>dto->trainer[1]</c>).</summary>
    public IReadOnlyList<MetTrainer> Trainers { get; init; } = Array.Empty<MetTrainer>();

    /// <summary>The species of the first wild Pokémon, whatever its form; null in a trainer's battle.</summary>
    public string? WildSpecies { get; init; }

    /// <summary>Two Pokémon fight a side (<c>BATTLE_TYPE_DOUBLES</c>): a double battle, a tag battle, two wild Pokémon beside a partner.</summary>
    public bool Double { get; init; }

    /// <summary>The battles with rules of their own; Pal Park's is what the species' rows ask (plan 09 · L14).</summary>
    public BattleKind Kind { get; init; } = BattleKind.Normal;

    /// <summary>A battle of the Battle Frontier (<c>BATTLE_TYPE_FRONTIER</c>).</summary>
    public bool Frontier { get; init; }

    /// <summary>A link battle (<c>BATTLE_TYPE_LINK</c>): none is fought yet (plan 07 · O6).</summary>
    public bool Link { get; init; }

    /// <summary>The ground the battle is fought on, as <c>GameEngine.TerrainAt</c> gives it.</summary>
    public BattleTerrain Terrain { get; init; }

    /// <summary>The level of the first Pokémon they send out that can fight, and of the player's (<c>Party_FindFirstEligibleBattler</c>).</summary>
    public int TheirLevel { get; init; }
    public int OwnLevel { get; init; }

    /// <summary>A battle with trainers (<c>BATTLE_TYPE_TRAINER</c>).</summary>
    public bool WithTrainers => Trainers.Count > 0;

    /// <summary>Their first Pokémon is of a higher level than the player's first (<c>CutInEffects_ForBattle</c>: the difference above nought).</summary>
    public bool Stronger => TheirLevel - OwnLevel > 0;

    /// <summary>
    /// The ground as the way in tells it (<c>CutInEffects_ForBattle</c>, the switch on <c>dto->terrain</c>): water,
    /// a cave, and the grass's way for every other, sand, snow and a building included. The grounds of their own
    /// (a League room, the Distortion World, a Frontier hall) are in no case of the original's switch, which leaves
    /// its number unset; here they go the grass's way too (<c>docs/mechanics/rulings.md</c>).
    /// </summary>
    public MeetingGround Ground => Terrain switch
    {
        BattleTerrain.Water => MeetingGround.Water,
        BattleTerrain.Cave => MeetingGround.Cave,
        _ => MeetingGround.Grass
    };

    /// <summary>The meeting with trainers of the game's data: their classes and names, the first first.</summary>
    public static IReadOnlyList<MetTrainer> Of(IEnumerable<Trainer> trainers) =>
        trainers.Select(t => new MetTrainer(t.TrainerClass, t.Name, t.Id)).ToList();

    /// <summary>
    /// How the first trainer's class is told (<c>EncEffects_TrainerClassEffect</c>): the classes with a way or a
    /// music of their own, and everyone else. The original has a class to each Gym Leader, to each of the Elite
    /// Four, to each Commander and to the grunts of either sex; <c>trainers.json</c> names them by the class's word
    /// and keeps the person's name beside it.
    /// </summary>
    public static MeetingRow ClassRow(string trainerClass) => trainerClass switch
    {
        "Leader" => MeetingRow.Leader,
        "Elite Four" => MeetingRow.EliteFour,
        "Champion" => MeetingRow.Champion,
        "Rival" => MeetingRow.Rival,
        "Galactic Boss" => MeetingRow.GalacticBoss,
        "Commander" => MeetingRow.GalacticCommander,
        "Galactic" => MeetingRow.GalacticGrunt,
        "Tower Tycoon" or "Hall Matron" or "Factory Head" or "Arcade Star" or "Castle Valet" => MeetingRow.FrontierBrain,
        _ => MeetingRow.Trainer
    };

    /// <summary>
    /// The row of a wild Pokémon's species (<c>EncEffects_WildPokemonEffect</c> of <c>src/enc_effects.c</c>): by the
    /// species alone, whatever its form, so a Giratina in its Origin Forme and a Rotom in an appliance are theirs; the
    /// Regis and the birds met in Pal Park (the original asks its map, <c>ZONE_ID_PAL_PARK</c>; here the battle's
    /// kind) are met as any wild Pokémon is. <see cref="MeetingRow.Wild"/> for every species with no row.
    /// </summary>
    public static MeetingRow SpeciesRow(string? species, BattleKind kind) => species switch
    {
        "Shaymin" => MeetingRow.Shaymin,
        "Cresselia" => MeetingRow.Cresselia,
        "Giratina" => MeetingRow.Giratina,
        "Registeel" or "Regice" or "Regirock" => kind == BattleKind.PalPark ? MeetingRow.Wild : MeetingRow.RegiTrio,
        "Regigigas" or "Heatran" or "Darkrai" or "Rotom" => MeetingRow.MinorLegendary,
        "Mesprit" => MeetingRow.Mesprit,
        "Uxie" or "Azelf" => MeetingRow.UxieAzelf,
        "Dialga" or "Palkia" => MeetingRow.DialgaPalkia,
        "Arceus" => MeetingRow.Arceus,
        "Moltres" or "Articuno" or "Zapdos" => kind == BattleKind.PalPark ? MeetingRow.Wild : MeetingRow.KantoBirds,
        _ => MeetingRow.Wild
    };

    /// <summary>
    /// Whom the meeting is with (<c>EncEffects_GetEffectPair</c>, in its order and with its quirks). Among trainers:
    /// at the Frontier a Brain is a Brain even in a double, anyone else a double battle in a double and the
    /// Frontier's alone; then Team Galactic's own, even in a double; then any other double, a Gym Leader's or the
    /// Elite Four's too (the slip the original's own comment owns), Volkner's kept apart for its music; then a link
    /// battle; then the class. Among wild Pokémon the species with a row of its own first (<see cref="SpeciesRow"/>:
    /// the legendary and mythical Pokémon, and those met as anyone is to music of their own), even two at once, as
    /// the original returns any row below <c>ENCEFF_NORMAL_WILD</c> before its check for doubles (plan 09 · L14);
    /// then two at once, or one.
    /// </summary>
    public MeetingRow Row
    {
        get
        {
            if (WithTrainers)
            {
                var first = ClassRow(Trainers[0].Class);
                // A Champion fought with a Mega Max team is met as the Champion whatever their class (Steven's is the
                // Rival's): a team of our own, which the original's rows never see (plan 25 · YD6)
                if (!Frontier && !Double && !Link && Data.TrainerDatabase.IsMegaMaxTeam(Trainers[0].Id)) return MeetingRow.Champion;
                if (Frontier)
                {
                    if (first == MeetingRow.FrontierBrain) return first;
                    return Double ? MeetingRow.DoubleBattle : MeetingRow.Frontier;
                }
                if (first is MeetingRow.GalacticGrunt or MeetingRow.GalacticCommander or MeetingRow.GalacticBoss) return first;
                if (Double) return first == MeetingRow.Leader && Trainers[0].Name == "Volkner" ? MeetingRow.DoubleLeader : MeetingRow.DoubleBattle;
                if (Link) return MeetingRow.Link;
                return first;
            }
            var species = SpeciesRow(WildSpecies, Kind);
            if (species != MeetingRow.Wild) return species;
            return Double ? MeetingRow.DoubleWild : MeetingRow.Wild;
        }
    }
}
