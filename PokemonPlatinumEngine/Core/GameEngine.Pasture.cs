using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>What a pasture act answered (plan 35 · BN5): the script's RESULT, and the words and item it names.</summary>
public readonly record struct PastureAnswer(int Result, string Thing = "", string? Item = null);

/// <summary>
/// The pasture in the game (plan 35 · BN5; <see cref="Pasture"/>; docs/mechanics/rulings.md, "The pasture"): every PC of a
/// region with one offers it (<c>common.PC</c>'s <c>if pasture</c>), the script's <c>pasture enter</c> warps the player to
/// its gate with the way back noted, and the box shown walks the meadow as Pokémon of the map, laid as the place is come to
/// (<see cref="KeepThePasture"/>) and laid again whenever the box changes under it. Spoken to, one runs
/// <c>common.PastureMember</c>, whose <c>pasture</c> acts are answered here.
/// </summary>
public partial class GameEngine
{
    // The bodies of the herd, and the map they stand on
    private readonly List<NPC> herdBodies = new();
    private Map? herdMap;

    // The close-up was opened from the pasture: it goes back to the field, not to the camp
    private bool careFromPasture;

    /// <summary>Whether the PC offers the pasture here: the region the player stands in has one, and the game is played by the modern rules.</summary>
    internal bool PastureHere => Ruleset.Current.Preset == RulesPreset.Modern && Pasture.MapOf(Here.Id) is { } map && MapDatabase.MapNames.Contains(map);

    /// <summary>
    /// The pasture as the place is come to: in a pasture, the visit's herd is laid (a visit no PC began, a tool's, comes
    /// back to the region's harbour); anywhere else, the visit is over and its herd is taken off.
    /// </summary>
    private void KeepThePasture()
    {
        if (!Pasture.IsPasture(flow.Map.Name))
        {
            ClearHerd();
            flow.Pasture = null;
            return;
        }
        if (flow.Pasture == null)
        {
            var back = Here.HarbourSpot ?? new MapSpot(flow.Map.Name, Pasture.GateX, Pasture.GateY, Direction.Up);
            flow.Pasture = new PastureSave { Box = flow.Boxes.CurrentBox, Map = back.Map, X = back.X, Y = back.Y, Facing = back.Facing };
        }
        LayHerd();
    }

    /// <summary>Takes the herd's bodies off the meadow they stand on.</summary>
    private void ClearHerd()
    {
        if (herdMap != null)
            foreach (var body in herdBodies) herdMap.NPCs.Remove(body);
        herdBodies.Clear();
        flow.Herd.Clear();
        workers.Clear();
        herdMap = null;
    }

    /// <summary>
    /// Lays the box shown on the meadow: one body to each Pokémon that isn't an Egg, where <see cref="Pasture.Place"/> puts
    /// it, wandering a little round that tile. Nothing is drawn from the dice. The field's renderer
    /// asks for their field sprites as they come into view, a Poké Ball standing in until they are baked.
    /// </summary>
    internal void LayHerd()
    {
        ClearHerd();
        if (flow.Pasture is not { } visit || !Pasture.IsPasture(flow.Map.Name)) return;
        var map = flow.Map;
        visit.Box = Math.Clamp(visit.Box, 0, flow.Boxes.Boxes.Count - 1);
        var slots = flow.Boxes.Boxes[visit.Box].Slots;
        foreach (var (slot, pokemon, x, y) in Pasture.Place(Pasture.OpenTiles(map), visit.Box, slots))
        {
            var body = new NPC
            {
                Id = Pasture.HerdPrefix + slot,
                GridX = x,
                GridY = y,
                Facing = Direction.Down,
                Script = "PastureMember",
                Movement = PersonMovement.Parse("wander_around", Pasture.Roam, Pasture.Roam, x, y, Direction.Down)
            };
            Follower.Dress(body, pokemon);
            map.Add(body);
            herdBodies.Add(body);
            flow.Herd[body.Id] = pokemon;
            // (its field sprite is baked from its model, made in the background: a Poké Ball stands in until then)
            if (!IsHeadless && !FieldSprites.Ready(body.Species!)) flow.WantModel(body.Species!);
        }
        LayWorkers(map);
        herdMap = map;
    }

    // Those away on a job the pasture shows, by their body's id (plan 35 · BN6; not saved)
    private readonly Dictionary<string, Job> workers = new();

    /// <summary>
    /// Those away on a job of the pasture's fields, seen at their work (plan 35 · BN6): the orchard's pickers under its
    /// trees, the hot spring's bathers in its pool to the chest. They stand still and are no part of the box shown.
    /// </summary>
    private void LayWorkers(Map map)
    {
        workers.Clear();
        var count = new Dictionary<JobKind, int>();
        for (int i = 0; i < flow.Jobs.Jobs.Count; i++)
        {
            var job = flow.Jobs.Jobs[i];
            int nth = count.GetValueOrDefault(job.Kind);
            count[job.Kind] = nth + 1;
            if (Pasture.WorkerPlace(job.Kind, nth) is not { } at) continue;
            var pokemon = job.Pokemon.ToPokemon();
            var body = new NPC
            {
                Id = Pasture.WorkerPrefix + i,
                GridX = at.X,
                GridY = at.Y,
                Facing = job.Kind == JobKind.Orchard ? Direction.Up : Direction.Down,
                Script = "PastureWorker",
                // (a bather stands in the pool to the chest, as the Resort Area's swimmers do)
                Wading = job.Kind == JobKind.HotSpring ? 10 : 0
            };
            Follower.Dress(body, pokemon);
            map.Add(body);
            herdBodies.Add(body);
            workers[body.Id] = job;
            if (!IsHeadless && !FieldSprites.Ready(body.Species!)) flow.WantModel(body.Species!);
        }
    }

    /// <summary>The Pokémon of the herd a body stands for; null for anyone else.</summary>
    private Pokemon? HerdOf(NPC? body) => body != null && flow.Herd.TryGetValue(body.Id, out var p) ? p : null;

    /// <summary>The box and slot a Pokémon of the boxes is in, or null.</summary>
    private (int Box, int Slot)? PlaceInTheBoxes(Pokemon pokemon)
    {
        for (int b = 0; b < flow.Boxes.Boxes.Count; b++)
        {
            var slots = flow.Boxes.Boxes[b].Slots;
            for (int s = 0; s < slots.Length; s++)
                if (ReferenceEquals(slots[s], pokemon)) return (b, s);
        }
        return null;
    }

    /// <summary>
    /// A pasture act of a script (the script command <c>pasture</c>): <c>enter</c>, <c>leave</c>, <c>box</c> (−1, 0 or +1),
    /// <c>count</c>, and of the Pokémon the script belongs to <c>mood</c>, <c>find</c>, <c>pet</c>, <c>feed</c> (the item
    /// last chosen), <c>care</c> and <c>take</c>. Each answer is in the rulings, "The pasture".
    /// </summary>
    internal PastureAnswer PastureAct(string act, int number, NPC? subject, string? item, Action<string, int, int, Direction?> warp)
    {
        switch (act)
        {
            case "enter":
            {
                if (!PastureHere) return new(0);
                var player = flow.Player;
                flow.Pasture = new PastureSave
                {
                    Box = flow.Boxes.CurrentBox, Map = flow.Map.Name, X = player.GridX, Y = player.GridY, Facing = player.Facing
                };
                warp(Pasture.MapOf(Here.Id)!, Pasture.GateX, Pasture.GateY, Direction.Up);
                return new(1);
            }
            case "leave":
            {
                if (flow.Pasture is not { } visit) return new(0);
                warp(visit.Map, visit.X, visit.Y, visit.Facing);
                return new(1);
            }
            case "box":
            {
                if (flow.Pasture is not { } visit) return new(0);
                if (number != 0)
                {
                    visit.Box = Pasture.Turn(visit.Box, Math.Sign(number), flow.Boxes.Boxes.Count);
                    flow.Boxes.CurrentBox = visit.Box;
                    LayHerd();
                }
                return new(visit.Box + 1, flow.Boxes.ShownName(visit.Box));
            }
            case "count":
                return new(flow.Herd.Count);
            // One away on a job, spoken to at its work (plan 35 · BN6): the whole hours it has left (0 once it is due back), its job's word
            case "job":
            {
                if (subject == null || !workers.TryGetValue(subject.Id, out var job)) return new(0);
                int left = (int)Math.Ceiling(job.Left(WallClock.UtcNow).TotalHours);
                return new(left, Text.Of($"pasture.job.{job.Kind}"));
            }
        }

        if (HerdOf(subject) is not { } p) return new(0);
        var page = flow.Care.Of(p);
        int day = CareDay;
        switch (act)
        {
            case "mood":
            {
                var mood = CareRules.MoodOf(page.Mood);
                subject!.ShowBubble(LeadFollower.BubbleOf(mood, LeadFollower.BandOf(p.Friendship)), 1.2f);
                return new((int)mood, Text.Of($"pasture.mood.{mood}"));
            }
            case "find":
            {
                if (page.FoundDay == day || Pasture.FindOf(day, p.Id, Here.Id) is not { } found || ItemDatabase.Get(found) is not { } data) return new(0);
                page.FoundDay = day;
                flow.Bag.AddItem(data, 1);
                return new(1, Item: found);
            }
            case "pet":
                return new(CareRules.Pat(page, p, day) ? 1 : 2);
            case "feed":
            {
                if (item == null || ItemDatabase.Get(item) is not { } food || flow.Bag.GetQuantity(food) <= 0) return new(2);
                bool ate = food.Treat != null ? Treats.Feed(page, p, day, food) : food.Berry != null && CareRules.FeedBerry(page, p, day, food);
                if (!ate) return new(0);
                flow.Bag.RemoveItem(food, 1);
                return new(1, Item: food.Name);
            }
            case "care":
            {
                if (!flow.CareScreen.Open(p, flow.Care, day, GameClock.IsNight)) return new(0);
                if (!IsHeadless)
                {
                    PokemonModels.Request(p.LookName);
                    PokemonSprites.Request(p.LookName);
                }
                careFromPasture = true;
                flow.State = GameState.Care;
                return new(1);
            }
            case "take":
            {
                var refusal = Pasture.WhyNotTake(p, flow.Party, flow.Run, flow.Story.PlayerStarter);
                if (refusal != PastureRefusal.None)
                    return new(refusal switch { PastureRefusal.TeamFull => 2, PastureRefusal.Run => 3, _ => 4 });
                if (PlaceInTheBoxes(p) is not { } at || !flow.Boxes.Withdraw(flow.Party, at.Box, at.Slot)) return new(0);
                subject!.Movement = null;
                herdMap?.NPCs.Remove(subject);
                herdBodies.Remove(subject);
                flow.Herd.Remove(subject.Id);
                return new(1);
            }
        }
        return new(0);
    }

    private sealed partial class FieldHost
    {
        public bool PastureHere => game.PastureHere;

        public PastureAnswer Pasture(string act, int number, NPC? subject, string? item) =>
            game.PastureAct(act, number, subject, item, Warp);
    }
}
