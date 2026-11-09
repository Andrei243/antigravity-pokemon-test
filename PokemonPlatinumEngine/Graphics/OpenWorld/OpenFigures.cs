using System;
using System.Collections.Generic;
using System.Numerics;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>What a figure of an open map is drawn as: a person (a rig of the grown kit) or a Pokémon (its model).</summary>
internal enum OpenFigureKind
{
    Person,
    Pokemon
}

/// <summary>
/// Someone or something standing in an open map that the renderer draws (plan 32 · OW9; style guide, "Open world",
/// "People and Pokémon"), beside the player: one of the map's people, whoever follows the player, and the wild Pokémon
/// in the field (plan 32 · OW7, which adds its own to the same list, <c>GameEngine.GatherOpenFigures</c>). A key the
/// renderer knows it by from frame to frame (its walk's phase is worked out from how far it moved), its look (a look of
/// <c>characters.json</c> in the grown kit, or a Pokémon's look name), where its feet are and the way it faces (radians,
/// nought south), whether the "!" is over it, and for a Pokémon its height in metres. GPU-free.
/// </summary>
internal readonly record struct OpenFigure(OpenFigureKind Kind, string Key, string Look, Vector3 Feet, float Yaw, bool Exclaiming = false, float Height = 0f);

/// <summary>
/// Which of the figures in view are drawn as themselves and which as their impostors (plan 32 · OW9's levels of detail,
/// the plan's "twelve live, the rest impostors"): the <see cref="MostLive"/> nearest the eye within
/// <see cref="LiveReach"/> are live, and of those the ones within <see cref="OutlineReach"/> keep their outline's shell
/// (a Pokémon's middle level is its model without the shell); the rest within <see cref="DrawReach"/> are impostors, eight
/// views baked once a look. Ties of distance go to the figure first in the list, so the choice is the same every frame
/// for the same places. GPU-free.
/// </summary>
internal static class OpenFigures
{
    /// <summary>How many figures are drawn live at most.</summary>
    public const int MostLive = 12;

    /// <summary>How near the eye a figure is drawn live, and with its outline's shell, in metres.</summary>
    public const float LiveReach = 45f, OutlineReach = 26f;

    /// <summary>How near the eye a figure is drawn at all, in metres: past the fog's near edge, where an impostor is a few pixels.</summary>
    public const float DrawReach = 240f;

    /// <summary>
    /// The figures' indices into <paramref name="live"/> (nearest first) and <paramref name="far"/> (in the list's order),
    /// each cleared first; figures further than <see cref="DrawReach"/> are in neither.
    /// </summary>
    public static void Choose(IReadOnlyList<OpenFigure> figures, Vector3 eye, List<int> live, List<int> far)
    {
        live.Clear();
        far.Clear();
        var near = new List<(float D, int I)>();
        for (int i = 0; i < figures.Count; i++)
        {
            float d = Vector3.Distance(figures[i].Feet, eye);
            if (d > DrawReach) continue;
            if (d <= LiveReach) near.Add((d, i));
            else far.Add(i);
        }
        near.Sort((a, b) => a.D != b.D ? a.D.CompareTo(b.D) : a.I.CompareTo(b.I));
        for (int k = 0; k < near.Count; k++)
        {
            if (k < MostLive) live.Add(near[k].I);
            else far.Add(near[k].I);
        }
        far.Sort();
    }

    /// <summary>The height a Pokémon of the field is drawn at, in metres: its own, kept between a hand's breadth and a house's storey so neither vanishes nor fills the view.</summary>
    public static float PokemonHeight(float metres) => Math.Clamp(metres, 0.35f, 3.5f);
}
