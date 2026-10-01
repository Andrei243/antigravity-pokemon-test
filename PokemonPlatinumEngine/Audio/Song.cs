using System.Collections.Generic;

namespace PokemonPlatinumEngine.Audio;

/// <summary>One note of a track, in ticks (<see cref="Song.TicksPerWhole"/> to a whole note).</summary>
public readonly record struct NoteEvent(int Tick, int Length, int Gate, int Key, float Velocity);

public sealed class SongTrack
{
    public required string Name { get; init; }
    public required Instrument Instrument { get; init; }

    /// <summary>The instrument the night arrangement uses instead; the same one when null.</summary>
    public Instrument? NightInstrument { get; init; }

    public float Volume { get; init; } = 0.8f;
    public float Pan { get; init; }
    public float ReverbSend { get; init; } = 0.3f;

    /// <summary>Notes in the order they start.</summary>
    public List<NoteEvent> Notes { get; } = new();

    public bool IsDrums => Instrument.IsKit;
}

/// <summary>
/// A piece of music: tracks of notes, a tempo, and where it loops back to. Fanfares and jingles have no loop and
/// play once. Read from the <c>.mml</c> files in <c>Data/music</c> by <see cref="Mml"/>.
/// </summary>
public sealed class Song
{
    public const int TicksPerWhole = 192;
    public const int TicksPerQuarter = TicksPerWhole / 4;

    /// <summary>The path under Data/music without the extension, for example <c>sinnoh/twinleaf</c>.</summary>
    public required string Id { get; init; }

    public string Title { get; init; } = "";

    /// <summary>Quarter notes per minute.</summary>
    public double Tempo { get; init; } = 120;

    /// <summary>The night arrangement plays this much slower or faster.</summary>
    public double NightTempo { get; init; } = 0.95;

    public int TicksPerBar { get; init; } = TicksPerWhole;

    /// <summary>Tick the song jumps back to when it reaches the end; -1 to play once.</summary>
    public int LoopTick { get; init; } = -1;

    /// <summary>The tick where the song ends (and loops from).</summary>
    public int EndTick { get; init; }

    public float Reverb { get; init; } = 0.3f;

    public List<SongTrack> Tracks { get; } = new();

    public bool Loops => LoopTick >= 0;

    public double SecondsPerTick(bool night) => 60.0 / ((night ? Tempo * NightTempo : Tempo) * TicksPerQuarter);

    /// <summary>Length of one pass through the song, in seconds.</summary>
    public double Duration(bool night = false) => EndTick * SecondsPerTick(night);

    /// <summary>Length of the looping part, in seconds.</summary>
    public double LoopDuration(bool night = false) => Loops ? (EndTick - LoopTick) * SecondsPerTick(night) : 0;
}
