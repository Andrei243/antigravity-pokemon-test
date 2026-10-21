using System;

namespace PokemonPlatinumEngine.Audio;

/// <summary>What the cry page's first mode switch is on: a second voice beside the cry, or where in the room it comes from.</summary>
public enum CryStereo { Chorus, Pan }

/// <summary>What the cry page's second mode switch is on: the cry ringing in a hall, or heard through a wall.</summary>
public enum CryRoom { Reverb, Filter }

/// <summary>
/// How the Pokédex's cry page bends a cry (plan 08 · P18), after the original's page (<c>crysub.c</c>): a pitch (down
/// to two octaves, which is the original's whole range, and a little way up), a switch between a chorus and a pan, and a
/// dial between a reverb and a filter. A shape of nothing is the cry as the Pokédex's entry plays it
/// (<see cref="CryMode.Pokedex"/>). Plain values: the page keeps one, and <see cref="Cries.Shaped"/> makes the cry from it.
/// </summary>
/// <param name="Pitch">Semitones, <see cref="LowestPitch"/> to <see cref="HighestPitch"/>: lower is slower and longer, as the DS pitches a sample.</param>
/// <param name="Stereo">Whether <paramref name="StereoAmount"/> is a chorus (0 to <see cref="MostChorus"/>) or a pan (<see cref="FurthestPan"/> to the left, as a negative, to as far to the right).</param>
/// <param name="Room">Whether <paramref name="RoomAmount"/> is a reverb or a filter (0, off, to <see cref="MostRoom"/>).</param>
public readonly record struct CryShape(int Pitch = 0, CryStereo Stereo = CryStereo.Chorus, int StereoAmount = 0, CryRoom Room = CryRoom.Reverb, int RoomAmount = 0)
{
    /// <summary>The original's pitch runs from nothing down to two octaves (<c>-(64 * 12) * 2</c> in sixty-fourths of a semitone); ours may go a octave up as well.</summary>
    public const int LowestPitch = -24, HighestPitch = 12;

    /// <summary>A chorus's most (the original's echo volume runs 0 to 127; ours is in eighths), and how far a pan goes each way.</summary>
    public const int MostChorus = 8, FurthestPan = 8;

    /// <summary>The most a reverb or a filter takes: the original's dial, a half turn each way, is eleven steps of a filter and twenty-two of a reverb.</summary>
    public const int MostRoom = 11;

    public static CryShape None => default;

    /// <summary>Whether the cry is as the Pokédex's entry plays it, with nothing to make: no pitch, no chorus, no reverb, no filter, no pan.</summary>
    public bool IsPlain => Pitch == 0 && StereoAmount == 0 && RoomAmount == 0;

    /// <summary>The cry's place in the stereo field, -1 (left) to 1 (right): the pan, or the middle for a chorus.</summary>
    public float PanPosition => Stereo == CryStereo.Pan ? Math.Clamp(StereoAmount / (float)FurthestPan, -1f, 1f) : 0f;

    /// <summary>The shape with its values brought into their ranges (a settings file or a test may give others).</summary>
    public CryShape Clamped() => this with
    {
        Pitch = Math.Clamp(Pitch, LowestPitch, HighestPitch),
        StereoAmount = Stereo == CryStereo.Pan ? Math.Clamp(StereoAmount, -FurthestPan, FurthestPan) : Math.Clamp(StereoAmount, 0, MostChorus),
        RoomAmount = Math.Clamp(RoomAmount, 0, MostRoom)
    };

    /// <summary>A shape's name for a cache and for what a test hears: <c>p-3 chorus4 reverb2</c>.</summary>
    public string Key => $"p{Pitch} {(Stereo == CryStereo.Pan ? "pan" : "chorus")}{StereoAmount} {(Room == CryRoom.Filter ? "filter" : "reverb")}{RoomAmount}";
}

/// <summary>One column of the cry page's scope: how far the wave goes down and up in it, and whether the playhead has been past it.</summary>
public readonly record struct ScopeColumn(float Low, float High, bool Played);
