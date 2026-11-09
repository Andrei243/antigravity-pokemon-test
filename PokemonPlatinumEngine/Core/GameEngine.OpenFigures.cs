using System;
using System.Collections.Generic;
using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Graphics.OpenWorld;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// What stands in an open map beside the player, as the open renderer draws it (plan 32 · OW9): the map's people who are
/// there within <see cref="OpenFigures.DrawReach"/> of the player, each in their look in the grown kit (their own, else
/// their trainer class's as a battle draws them, else the plain townsperson), the "!" over a trainer walking up while it
/// shows, whoever follows the player (a Pokémon of the team at its height, or a partner), and plan 32 · OW7's wild
/// Pokémon of the field at their species' heights. <b>Whatever else stands in an open map and is drawn is an
/// <see cref="OpenFigure"/> added in <see cref="GatherOpenFigures"/></b>. Nothing of the rules reads any of it, and
/// nothing of a region of today reaches it.
/// </summary>
public partial class GameEngine
{
    private readonly List<OpenFigure> openFigures = new();
    private readonly List<OpenPerson> openFigurePeople = new();
    private readonly Dictionary<OpenPerson, string> openLooks = new();

    /// <summary>The walkers the open grass parts round: the player first, then the figures nearest them (eight in all).</summary>
    private readonly List<Vector3> openWalkers = new();

    /// <summary>
    /// The figures of the frame (plan 32 · OW9): the people present near the player, whoever follows them, and (OW7)
    /// the wild Pokémon of the field, in that order, each at its feet and facing its way.
    /// </summary>
    internal IReadOnlyList<OpenFigure> GatherOpenFigures(OpenField field)
    {
        openFigures.Clear();
        var map = field.Map;
        foreach (var p in map.PeopleNear(field.Player.Pos, (int)(OpenFigures.DrawReach * OpenPos.PerMetre), openFigurePeople))
        {
            if (!p.Present) continue;
            bool exclaiming = openApproach is { } a && a.Trainer == p && a.Exclaiming || openPairApproach is { } b && b.Trainer == p && b.Exclaiming;
            openFigures.Add(new OpenFigure(OpenFigureKind.Person, "person:" + p.Key, OpenLookOf(map, p), Feet(p.Pos), (float)OpenYaw.Radians(p.Yaw), exclaiming));
        }
        if (field.Follower is { } follower)
        {
            if (follower.TrainerId == null)
            {
                var look = follower.Who;
                float height = flow.Along?.Pokemon?.Height ?? PokemonDatabase.Get(Models.PokemonLook.Base(look))?.Height ?? 0.6f;
                openFigures.Add(new OpenFigure(OpenFigureKind.Pokemon, "follower", look, Feet(follower.Pos), (float)OpenYaw.Radians(follower.Yaw), Height: height));
            }
            else
            {
                string look = CharacterStyle.InKit(CharacterStyles.Default.CharacterFor(flow.Along?.Who.NpcType ?? ""), CharacterKit.Grown);
                openFigures.Add(new OpenFigure(OpenFigureKind.Person, "follower", look, Feet(follower.Pos), (float)OpenYaw.Radians(follower.Yaw)));
            }
        }
        // Plan 32 · OW7: the field's wild Pokémon, each at its feet, facing its way, at its species' or form's height
        var player = Feet(field.Player.Pos);
        foreach (var wild in field.Wild.All)
        {
            var feet = Feet(wild.Pos);
            if (Vector3.DistanceSquared(feet, player) > OpenFigures.DrawReach * OpenFigures.DrawReach) continue;
            openFigures.Add(new OpenFigure(OpenFigureKind.Pokemon, "wild:" + wild.Id, wild.Species, feet, (float)OpenYaw.Radians(wild.Yaw), Height: HeightOf(wild.Species)));
        }
        // A tool's figures put in by hand (the harness's crowd, standing about the village)
        if (OpenFiguresByHand is { } byHand) openFigures.AddRange(byHand);
        return openFigures;
    }

    /// <summary>A species' or a form's height in metres (a form's own where it has one), by the name a zone's row gives it.</summary>
    private static float HeightOf(string name)
    {
        if (PokemonDatabase.Get(name) is { } species) return species.Height;
        if (PokemonDatabase.SpeciesOfForm(name) is { } of) return of.Form(name)?.Height ?? of.Height;
        return 0.6f;
    }

    /// <summary>Figures a tool stands in the open field by hand (the harness's crowd and field Pokémon), drawn as the game's own are; null for none.</summary>
    internal List<OpenFigure>? OpenFiguresByHand { get; set; }

    /// <summary>The grass parts round the player and the figures nearest them (eight at most, the shader's count).</summary>
    internal IReadOnlyList<Vector3> OpenWalkersOf(OpenField field, IReadOnlyList<OpenFigure> figures)
    {
        openWalkers.Clear();
        var feet = Feet(field.Player.Pos);
        openWalkers.Add(feet);
        var near = new List<(float D, Vector3 At)>();
        foreach (var f in figures)
        {
            float d = Vector3.Distance(f.Feet, feet);
            if (d < OpenGrass.Reach) near.Add((d, f.Feet));
        }
        near.Sort((x, y) => x.D.CompareTo(y.D));
        for (int i = 0; i < near.Count && openWalkers.Count < 8; i++) openWalkers.Add(near[i].At);
        return openWalkers;
    }

    private static Vector3 Feet(OpenPos pos) => new(pos.X / (float)OpenPos.PerMetre, pos.Y / (float)OpenPos.PerMetre, pos.Z / (float)OpenPos.PerMetre);

    /// <summary>The look someone of an open map is drawn in, in the grown kit: their own, their trainer class's (as a battle draws them), or the plain townsperson's.</summary>
    internal string OpenLookOf(OpenMap map, OpenPerson p)
    {
        if (openLooks.TryGetValue(p, out var known)) return known;
        string look = p.Look is { Length: > 0 } own
            ? CharacterStyles.Default.CharacterFor(own)
            : NpcOf(map, p).TrainerData is { } trainer ? CharacterStyles.Default.InBattle(trainer) : CharacterStyles.Default.CharacterFor("");
        look = CharacterStyle.InKit(look, CharacterKit.Grown);
        openLooks[p] = look;
        return look;
    }

    /// <summary>The looks of an open map's people and its follower, asked for ahead behind the fade of coming there (never waited for).</summary>
    private void PreloadTheOpenLooks(OpenField field)
    {
        if (IsHeadless) return;
        var looks = new List<string>();
        foreach (var p in field.Map.People) looks.Add(OpenLookOf(field.Map, p));
        CharacterModels.Preload(looks);
        if (field.Follower is { TrainerId: null } f) PokemonModels.Request(f.Who);
        // The species the map's zones bring out (plan 32 · OW7), so a wild Pokémon coming into view has its model
        foreach (var zone in field.Map.Zones)
            foreach (var slot in zone.Slots) PokemonModels.Request(slot.Species);
    }

    internal sealed partial class Driver
    {
        /// <summary>The figures the open renderer drew in the last frame (plan 32 · OW9).</summary>
        public IReadOnlyList<OpenFigure> OpenFigures => game.openFigures;

        /// <summary>Figures stood in the open field by hand, drawn as the game's own (the harness's crowd), or null.</summary>
        public List<OpenFigure>? OpenFiguresByHand
        {
            get => game.OpenFiguresByHand;
            set => game.OpenFiguresByHand = value;
        }

        /// <summary>The look someone of the open map is drawn in.</summary>
        public string OpenLookOf(OpenPerson p) => game.OpenLookOf(game.flow.Open?.Map ?? throw new InvalidOperationException("the player is in no open map"), p);
    }
}
