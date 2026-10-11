using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Whoever walks behind the player (the original's <c>MOVEMENT_TYPE_FOLLOW_PLAYER</c>), a step behind, onto each
/// tile the player has just left, at the player's own pace, and never in the player's way: walking back into them
/// swaps the two of them round. There is one kind of follower and the game has one at a time. It is either
/// someone travelling with the player (plan 02 · S6; the original's partner trainer, <c>VAR_PARTNER_TRAINER_ID</c>):
/// Cheryl through Eterna Forest, later Mira, Riley, Marley and Buck, whose <see cref="TrainerId"/> battles beside the
/// player, which is the engine's to see to; or a Pokémon of the team out of its ball (plan 06 · R16c: in Amity
/// Square; plan 10 · F3: wherever the lead may follow), which <see cref="Pokemon"/> names and which changes no
/// battle. Either way <see cref="Who"/> is a thing of the map, drawn and spoken to as one. No drawing or input.
/// </summary>
public sealed class Follower
{
    private readonly Queue<(int X, int Y)> trail = new();
    private NpcWalk? step;

    /// <summary>Who walks behind the player on the map: a person, or the thing of the map a Pokémon of the team is dressed as.</summary>
    public NPC Who { get; }

    /// <summary>The trainer of Platinum's data who battles beside the player while they travel together; null for a Pokémon, who only walks.</summary>
    public string? TrainerId { get; }

    /// <summary>The Pokémon of the team that walks behind the player as <see cref="Who"/>; null for a person.</summary>
    public Pokemon? Pokemon { get; }

    public Follower(NPC who, string trainerId)
    {
        Who = who;
        TrainerId = trainerId;
    }

    /// <summary>A Pokémon of the team out of its ball, as a thing of the map that <see cref="Dress"/> makes it.</summary>
    public Follower(NPC body, Pokemon pokemon)
    {
        Who = body;
        Pokemon = pokemon;
        Dress(body, pokemon);
    }

    /// <summary>
    /// Makes a thing of the map a Pokémon of the team: its species or form to look at (<see cref="NPC.Species"/>, drawn
    /// as a field sprite like any Pokémon of the map) and its name to be spoken of by. Where the follower may be
    /// anywhere (plan 10 · F3) the body is made for the map it is on; in Amity Square it is the square's own object.
    /// </summary>
    public static void Dress(NPC body, Pokemon pokemon)
    {
        body.NpcType = NPC.PokemonType;
        body.Species = pokemon.ModelName;
        body.Name = pokemon.DisplayName;
    }

    /// <summary>
    /// How many tiles behind the player they keep: one, as the original's followers do, or two for a Pokémon so tall
    /// that a tile behind the player it would hide them from the field's steep camera whenever they walked north
    /// (plan 06 · R16c: the game says which, by the height it is drawn at).
    /// </summary>
    public int Gap { get; set; } = 1;

    // The player is walking onto the follower's own tile: it makes way at once, whatever its gap. And where the player
    // is heading, as far as the follower has been told: it never sets off for that tile (a player who turns back toward
    // a follower two tiles behind steps onto the tile between them)
    private bool crowded;
    private (int X, int Y)? playerHeading;

    /// <summary>Whether they are on their way somewhere.</summary>
    public bool Moving => step != null || CanSetOff;

    // A tile to go to, and leave to go: the gap behind the player is kept, and the tile isn't the one the player is on
    // their way to
    private bool CanSetOff => trail.Count > 0 && (crowded || (trail.Count >= Gap && trail.Peek() != playerHeading));

    /// <summary>
    /// The player has set off from a tile for <paramref name="heading"/>: the follower goes there next, or after the
    /// tiles before it where it keeps a wider gap.
    /// </summary>
    public void PlayerLeft(int x, int y, (int X, int Y)? heading = null)
    {
        if (heading == (Who.GridX, Who.GridY)) crowded = true;
        playerHeading = heading;
        if ((x, y) == (Who.GridX, Who.GridY)) return;
        trail.Enqueue((x, y));
        // Someone who falls far behind (a long slide, a ride) is brought up to the last tiles but two
        while (trail.Count > Gap + 1)
        {
            var skipped = trail.Dequeue();
            step?.Finish();
            step = null;
            (Who.GridX, Who.GridY) = skipped;
            Who.StepOffsetX = Who.StepOffsetY = 0f;
        }
    }

    /// <summary>
    /// A frame: the step under way goes on at <paramref name="tilesPerSecond"/>, and the next one begins toward the
    /// tile the player left, straight along each axis in turn.
    /// </summary>
    public void Update(float dt, float tilesPerSecond)
    {
        if (step != null)
        {
            step.Update(dt);
            if (!step.IsDone && (Who.StepOffsetX != 0f || Who.StepOffsetY != 0f)) return;
            if (!step.IsDone && !CanSetOff) return;
            step.Finish();
            step = null;
        }
        if (!CanSetOff) return;
        crowded = false;

        var (tx, ty) = trail.Dequeue();
        var path = new List<Direction>();
        for (int x = Who.GridX; x != tx; x += Math.Sign(tx - x)) path.Add(tx > x ? Direction.Right : Direction.Left);
        for (int y = Who.GridY; y != ty; y += Math.Sign(ty - y)) path.Add(ty > y ? Direction.Down : Direction.Up);
        if (path.Count == 0) return;
        step = new NpcWalk(Who, path, Math.Max(NpcWalk.WalkPace, tilesPerSecond));
    }

    /// <summary>
    /// Puts them behind the player at once, facing the way the player faces: a door gone through, a save loaded.
    /// <paramref name="tiles"/> behind, which is their gap unless whoever places them knows there is no room for it.
    /// </summary>
    public void Behind(int playerX, int playerY, Direction facing, int? tiles = null)
    {
        trail.Clear();
        crowded = false;
        playerHeading = null;
        step?.Finish();
        step = null;
        var (dx, dy) = FieldMovement.Delta(facing);
        int back = Math.Clamp(tiles ?? Gap, 1, Gap);
        (Who.GridX, Who.GridY) = (playerX - dx * back, playerY - dy * back);
        // The tiles between are the ones the player is taken to have left: the follower comes on over them
        for (int k = back - 1; k >= 1; k--) trail.Enqueue((playerX - dx * k, playerY - dy * k));
        Who.Facing = facing;
        Who.StepOffsetX = Who.StepOffsetY = 0f;
        Who.WalkBlend = 0f;
    }

    /// <summary>Ends a step under way where it was going: a script takes them over.</summary>
    public void Settle()
    {
        trail.Clear();
        crowded = false;
        step?.Finish();
        step = null;
    }
}
