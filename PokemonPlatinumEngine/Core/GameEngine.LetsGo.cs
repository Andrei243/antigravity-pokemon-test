using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics.OpenWorld;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Let's Go in an open map and the raids on Team Star's gates (plan 34 · PD5). The Let's Go key (<see cref="GameAction.LetsGo"/>,
/// on foot only) sends the team's lead at the wild Pokémon of the field nearest the player within
/// <see cref="LetsGoReach"/>: one that may be fought so (<see cref="LetsGoBattle.MayFight"/>) is fought there and then by
/// the rules with nobody watching (<see cref="LetsGoBattle"/>), on a seed the field's chance draws for it and nothing
/// else; one higher than the lead, or shiny, or any under a Nuzlocke, is met in a real battle as walking into it would.
/// The field shows the lead running out to it, the target hit and fading or the lead coming back (<see cref="LetsGoShow"/>).
/// During a raid (<see cref="StarRaid"/>, begun by a script's <c>starraid begin</c>) the key sends the first three in
/// turn at the crew's Pokémon round the yard, whatever their level, and the raid's clock runs on the field's ticks; its
/// end, won or lost, starts the base's script. Nothing here draws or reads the GPU.
/// </summary>
public partial class GameEngine
{
    /// <summary>How near a wild Pokémon must stand for Let's Go to reach it, in millimetres (outside a raid).</summary>
    public const int LetsGoReach = 10_000;

    // The raid under way on a base's gate, or null
    private StarRaid? starRaid;

    // What the field shows of the last Let's Go, or null once it has played
    private LetsGoShow? letsGoShow;

    /// <summary>The last Let's Go battle fought (a tool's and a test's to read), or null.</summary>
    internal LetsGoBattle? LastLetsGo { get; private set; }

    /// <summary>
    /// The key pressed in an open map, on foot, with nothing else under way: the lead sent. True when a real battle
    /// begins instead (the frame's walking ends there).
    /// </summary>
    private bool LetsGo(OpenField field)
    {
        if (OpenMounted) return false;
        // One at a time: the lead comes back before another is sent
        if (letsGoShow is { } shown && !shown.Over(field.Wild.Tick)) return false;
        if (starRaid is { } raid) return LetsGoInTheRaid(field, raid);

        var lead = flow.Party.Members.FirstOrDefault(p => !p.IsEgg && !p.IsFainted);
        if (lead == null) return false;
        var seen = NearestWild(field);
        if (seen == null) return false;
        var wild = WildOfTheField(field, seen);
        // One higher than the lead, a shiny one, or any under the Nuzlocke (whose catches and burials are a real
        // battle's): met as walking into it would meet it
        if (!LetsGoBattle.MayFight(lead, wild) || flow.Run.Nuzlocke)
        {
            field.Wild.Met(seen);
            openWildMet = seen.Zone;
            flow.FieldEncounter = true;
            wildSurprised = false;
            string place = field.Map.AreaAt(field.Player.Pos)?.Key ?? field.Map.Name;
            MeetWildPokemon(wild, BattleKind.Normal, nuzlockeCatch: Nuzlocke.Meet(flow.Run, flow.RunState, place, wild, flow.Pokedex));
            return true;
        }
        var battle = FightLetsGo(lead, wild);
        if (battle.End == LetsGoEnd.Felled)
        {
            field.Wild.Met(seen);
            field.Wild.Ended(seen.Zone, OpenWildEnd.CaughtOrFelled);
        }
        letsGoShow = LetsGoShow.Of(lead, field.Player.Pos, seen.Pos, seen.Species, battle.End, field.Wild.Tick);
        return AfterLetsGo(lead, battle);
    }

    /// <summary>A raid's Let's Go: the next of the first three at the nearest of the crew's Pokémon within the raid's reach.</summary>
    private bool LetsGoInTheRaid(OpenField field, StarRaid raid)
    {
        if (raid.Over || raid.Nearest(field.Player.Pos) is not { } foe) return false;
        int index = raid.NextLead(flow.Party);
        if (index < 0) return false;
        var lead = flow.Party.Members[index];
        var wild = MakeWild(new WildEncounterEntry { SpeciesName = foe.Species, MinLevel = foe.Level, MaxLevel = foe.Level }, null);
        // The crew's Pokémon is never shiny, and is fought whatever its level
        wild.IsShiny = false;
        var battle = FightLetsGo(lead, wild);
        if (battle.End == LetsGoEnd.Felled) raid.Fell(foe);
        letsGoShow = LetsGoShow.Of(lead, field.Player.Pos, foe.Pos, foe.Species, battle.End, field.Wild.Tick);
        return AfterLetsGo(lead, battle);
    }

    /// <summary>The fight itself, on a seed the field's chance draws for it, under the place's conditions and the run's terms.</summary>
    private LetsGoBattle FightLetsGo(Pokemon lead, Pokemon wild)
    {
        uint seed = unchecked((uint)fieldRandom.Next());
        var battle = LetsGoBattle.Fight(lead, wild, new LetsGoTerms
        {
            Seed = seed,
            Rules = Ruleset.Current,
            Conditions = BattleConditionsHere(),
            PlayerName = PlayerIdentity.Name,
            Difficulty = flow.Run.Difficulty,
            RunLevelCap = CurrentLevelCap() ?? 0,
            CrossedExp = CrossedExpHere()
        });
        LastLetsGo = battle;
        Log.Info($"Let's Go: {lead.Species.Name} {lead.Level} at {wild.Species.Name} {wild.Level}: {battle.End} in {battle.Turns} turns, {battle.Exp} EXP.");
        return battle;
    }

    /// <summary>What a Let's Go leaves: the Pokédex told of what was seen, a level's evolution, the statistics. True when an evolution begins (the frame's walking ends there).</summary>
    private bool AfterLetsGo(Pokemon lead, LetsGoBattle battle)
    {
        flow.Pokedex.RegisterSeen(battle.Core.WildPokemon[0]);
        flow.Statistics.Count(Statistics.Levelled, battle.LeveledUp ? 1 : 0);
        AudioManager.PlaySound(battle.End == LetsGoEnd.Felled ? "hit_super" : "hit_normal");
        return battle.LeveledUp && !flow.Run.Nuzlocke
            && PlayEvolutions(FindEvolutions(new[] { lead }, EvolutionTrigger.LevelUp, cancellable: true), GameState.Overworld);
    }

    /// <summary>The wild Pokémon of the field nearest the player within <see cref="LetsGoReach"/>, on their level of ground.</summary>
    private static OpenWildPokemon? NearestWild(OpenField field)
    {
        OpenWildPokemon? best = null;
        long bestD = (long)LetsGoReach * LetsGoReach;
        foreach (var w in field.Wild.All)
        {
            if (Math.Abs(w.Pos.Y - field.Player.Pos.Y) > 3000) continue;
            long d = w.Pos.GroundDistanceSquared(field.Player.Pos);
            if (d > bestD) continue;
            bestD = d;
            best = w;
        }
        return best;
    }

    /// <summary>The Pokémon a wild one of the field is, made as the grass makes one (the lead's Synchronize and Cute Charm, the region's levels).</summary>
    private Pokemon WildOfTheField(OpenField field, OpenWildPokemon seen)
    {
        string name = seen.Species;
        var species = PokemonDatabase.Get(name);
        string? form = null;
        if (species == null && PokemonDatabase.SpeciesOfForm(name) is { } ofForm)
        {
            form = name;
            species = ofForm;
        }
        var lead = WildLead.Of(flow.Party);
        return MakeWild(new WildEncounterEntry
        {
            SpeciesName = species?.Name ?? name,
            FormName = form,
            MinLevel = seen.Level,
            MaxLevel = seen.Level,
            Nature = WildEncounterRules.NatureFor(lead, fieldRandom),
            Gender = WildEncounterRules.GenderFor(species, lead, fieldRandom)
        }, null);
    }

    // ------------------------------------------------------------------ the raid

    /// <summary>A script's <c>starraid begin</c>: the raid round the base's yard, from now. Nothing outside an open map that has the yard.</summary>
    internal void BeginStarRaid(string key)
    {
        if (StarBases.Get(key) is not { } @base || flow.Open is not { } field || field.Map.Marker(@base.Yard) is not { } yard)
        {
            Log.Warn($"A raid on {key} begun where there is no yard for it.");
            return;
        }
        starRaid = StarRaid.On(field.Map, @base, yard.Pos);
        letsGoShow = null;
        Log.Info($"The raid on {@base.Key}'s gate begins: {StarRaid.Goal} to fell in {StarRaid.Seconds} s.");
    }

    /// <summary>
    /// A tick of the raid, among the field's (after the player's step): its clock runs; the player gone from the street,
    /// or none of the team able to fight, gives it up. Won or lost, the base's script is started, and true is
    /// given back (the frame's walking ends there).
    /// </summary>
    private bool RaidTick(OpenField field)
    {
        if (starRaid is not { } raid) return false;
        raid.Tick();
        if (!raid.Over && (field.Map.AreaAt(field.Player.Pos)?.Key != StarBases.File
            || flow.Party.Members.All(p => p.IsEgg || p.IsFainted)))
            raid.GiveUp();
        if (!raid.Over) return false;
        starRaid = null;
        Log.Info($"The raid on {raid.Base.Key}'s gate ends {(raid.Won ? "won" : "lost")}: {raid.Felled} felled.");
        StartScript(raid.Won ? raid.Base.WonScript : raid.Base.LostScript, file: StarBases.File);
        return true;
    }

    /// <summary>The figures of a raid's crew and of a Let's Go under way (<see cref="GatherOpenFigures"/>).</summary>
    private void GatherLetsGoFigures(OpenField field)
    {
        if (starRaid is { } raid)
            foreach (var foe in raid.Standing)
            {
                if (foe == null) continue;
                openFigures.Add(new OpenFigure(OpenFigureKind.Pokemon, "raid:" + foe.Id, foe.Species, Feet(foe.Pos), (float)OpenYaw.Radians(foe.Yaw), Height: HeightOf(foe.Species)));
            }
        if (letsGoShow is not { } show) return;
        long tick = field.Wild.Tick;
        if (show.Over(tick))
        {
            letsGoShow = null;
            return;
        }
        if (show.LeadAt(tick) is { } lead)
            openFigures.Add(new OpenFigure(OpenFigureKind.Pokemon, "letsgo", show.Lead, Feet(lead), (float)OpenYaw.Radians(show.Yaw), Height: HeightOf(PokemonLook.Base(show.Lead))));
        if (show.TargetShown(tick) is { } scale)
            openFigures.Add(new OpenFigure(OpenFigureKind.Pokemon, "letsgo:target", show.Target, Feet(show.To), (float)OpenYaw.Radians(show.Yaw + OpenYaw.Turn / 2), Height: HeightOf(show.Target) * scale));
    }

    /// <summary>The raid's corner (its count and its clock), over the open field while one is under way.</summary>
    private void DrawStarRaidCorner()
    {
        if (flow.Open != null && starRaid is { } raid) ModernUi.StarRaidCount(VirtualWidth, raid.Felled, StarRaid.Goal, raid.SecondsLeft);
    }

    /// <summary>No raid and no Let's Go under way: a new game, a loaded one, the player put down in an open map.</summary>
    private void ForgetTheLetsGo()
    {
        starRaid = null;
        letsGoShow = null;
    }

    internal sealed partial class Driver
    {
        /// <summary>The raid under way (plan 34 · PD5), or null.</summary>
        public StarRaid? StarRaid => game.starRaid;

        /// <summary>The last Let's Go battle fought, or null.</summary>
        public LetsGoBattle? LastLetsGo => game.LastLetsGo;

        /// <summary>What the field shows of the last Let's Go, or null.</summary>
        public LetsGoShow? LetsGoShowing => game.letsGoShow;

        /// <summary>A raid begun as a script's <c>starraid begin</c> begins one.</summary>
        public void BeginStarRaid(string key) => game.BeginStarRaid(key);
    }
}

/// <summary>
/// What the field shows of a Let's Go (plan 34 · PD5; style guide, "Open world", "Let's Go"): the lead running out from
/// the player to its target, a moment at it while the target flickers with the hits, then the target fading (shrinking
/// into the ground) where it was felled, and the lead running back. All by the field's ticks; nothing of the rules reads it.
/// </summary>
public sealed record LetsGoShow(string Lead, OpenPos From, OpenPos To, string Target, LetsGoEnd End, long Began, int Yaw)
{
    /// <summary>The phases' lengths in ticks: out, at it (the hits), the fade, and back.</summary>
    public const int Out = OpenClock.TicksPerSecond * 5 / 10, Hits = OpenClock.TicksPerSecond * 4 / 10, Fade = OpenClock.TicksPerSecond * 5 / 10,
        Back = OpenClock.TicksPerSecond * 5 / 10;

    public static LetsGoShow Of(Pokemon lead, OpenPos from, OpenPos to, string target, LetsGoEnd end, long tick) =>
        new(lead.LookName, from, to, target, end, tick, OpenYaw.Toward(to.MetresX - from.MetresX, to.MetresZ - from.MetresZ));

    public bool Over(long tick) => tick - Began >= Out + Hits + Fade + Back;

    /// <summary>Where the lead stands at a tick (running out, at the target a metre short of it, running back), or null once home.</summary>
    public OpenPos? LeadAt(long tick)
    {
        long t = tick - Began;
        if (t < 0 || Over(tick)) return null;
        // A metre short of the target, on the way from the player
        double dx = To.MetresX - From.MetresX, dz = To.MetresZ - From.MetresZ, length = Math.Max(0.001, Math.Sqrt(dx * dx + dz * dz));
        double reach = Math.Max(0, length - 1.0) / length;
        double share = t < Out ? t / (double)Out : t < Out + Hits + Fade ? 1 : 1 - (t - Out - Hits - Fade) / (double)Back;
        share *= reach;
        return OpenPos.FromMetres(From.MetresX + dx * share, From.MetresY + (To.MetresY - From.MetresY) * share, From.MetresZ + dz * share);
    }

    /// <summary>
    /// The target's size at a tick as a share of its own (null while it is not drawn by the show). One felled is gone
    /// from the field at once, so the show stands it in its place: whole until the hits, flickering through them (drawn
    /// on alternate twentieths of a second), shrinking away through the fade. One that stands is drawn by the field as
    /// ever, and never by the show.
    /// </summary>
    public float? TargetShown(long tick)
    {
        long t = tick - Began;
        if (End != LetsGoEnd.Felled || t < 0 || t >= Out + Hits + Fade) return null;
        if (t < Out) return 1f;
        if (t < Out + Hits) return ((t - Out) / (OpenClock.TicksPerSecond / 20)) % 2 == 0 ? 1f : null;
        return 1f - (t - Out - Hits) / (float)Fade;
    }
}
