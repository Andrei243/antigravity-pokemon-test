using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The team's app (<c>party_status/main.c</c>): the team in two rows of three, each over a bar of its HP. A
/// Pokémon touched hops (<c>Task_HandleMonIconBounce</c>).
/// </summary>
public sealed class PartyStatusApp : PoketchAppState
{
    public const float HopSeconds = 0.4f;

    private readonly float[] hops = new float[Party.MaxSize];

    public override PoketchApp App => PoketchApp.PartyStatus;

    /// <summary>Where each slot's Pokémon stands, in blocks: a cell of 14 by 17, three to a row.</summary>
    public static PoketchButton Slot(int i) => new(i, 3 + (i % 3) * 14, 3 + (i / 3) * 17, 12, 15);

    /// <summary>How far through its hop a slot's Pokémon is, 0 standing to 1 landed.</summary>
    public float Hop(int slot) => hops[slot] <= 0f ? 0f : 1f - hops[slot] / HopSeconds;

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) =>
        Enumerable.Range(0, context.Party.Count).Select(Slot).ToList();

    public override void Press(int button, PoketchContext context)
    {
        if (button < 0 || button >= context.Party.Count) return;
        hops[button] = HopSeconds;
        context.Sound("poketch");
    }

    public override void Update(float dt, PoketchContext context)
    {
        for (int i = 0; i < hops.Length; i++) hops[i] = System.Math.Max(0f, hops[i] - dt);
    }
}
