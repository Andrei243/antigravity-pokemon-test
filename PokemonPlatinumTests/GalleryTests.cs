using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q30: the gallery lists only the species seen with every form of each, plays the clips in the studio's order,
/// turns round and round, asks for a model once while its still sprite stands in, lets go of what it was the first to ask
/// for, and frames a model so that no turn takes it out of the picture, while every bake stays as it was.
/// </summary>
[Collection("Shared clocks and dice")]
public class GalleryTests
{
    /// <summary>Models of a test's own: which are held by others, which are built, and every ask and letting go.</summary>
    private sealed class Models : IGalleryModels
    {
        public readonly HashSet<string> Elsewhere = new(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> Built = new(StringComparer.OrdinalIgnoreCase);
        public readonly List<string> Asks = new(), Gone = new();
        public bool HeldElsewhere(string look) => Elsewhere.Contains(look);
        public void Ask(string look) => Asks.Add(look);
        public bool Ready(string look) => Built.Contains(look);
        public void LetGo(string look) => Gone.Add(look);
    }

    private static readonly int Bulbasaur = 1, Pikachu = 25, Rotom = 479;

    private static List<GalleryPerson> People => GalleryScreen.PeopleFor("PLAYER", "Lucas", "DAWN", "Dawn", RegionDatabase.First.Professor);

    private static (GalleryScreen Screen, Models Models) Opened(params int[] seen)
    {
        var models = new Models();
        var screen = new GalleryScreen(models);
        screen.Open(seen, People);
        return (screen, models);
    }

    [Fact]
    public void OnlyTheSpeciesSeenAreListedEachWithEveryFormInNationalOrder()
    {
        var (screen, _) = Opened(Rotom, Pikachu, Bulbasaur, Pikachu);
        var species = screen.Pokemon.Where(e => !e.IsForm).Select(e => e.Species.DexNumber).ToList();
        Assert.Equal(new[] { Bulbasaur, Pikachu, Rotom }, species);
        foreach (int number in species)
        {
            var s = PokemonDatabase.GetByDex(number)!;
            int at = screen.Pokemon.ToList().FindIndex(e => e.Look == s.Name && !e.IsForm);
            var forms = (s.Forms ?? new()).Select(f => f.Name).ToList();
            // Each species is followed by its own forms, every one of them, in the data's order
            Assert.Equal(forms, screen.Pokemon.Skip(at + 1).Take(forms.Count).Select(e => e.Look));
            Assert.All(screen.Pokemon.Skip(at + 1).Take(forms.Count), e => Assert.Same(s, e.Species));
        }
        Assert.Contains(screen.Pokemon, e => e.Look == "Rotom-Wash");
        Assert.Equal(GalleryTab.Pokemon, screen.Tab);
        Assert.Empty(Opened().Screen.Pokemon);
    }

    [Fact]
    public void TheClipsAreTheStudiosInOrderAndGoRound()
    {
        Assert.Equal(new[] { "idle", "physical", "special", "status", "hit", "faint", "entry", "dodge", "tired", "cheer", "charge", "walk" }, PokemonAnimation.Clips);
        var (screen, _) = Opened(Pikachu);
        var seen = new List<string> { screen.Clip };
        for (int i = 0; i < PokemonAnimation.Clips.Length; i++)
        {
            screen.NextClip(1);
            seen.Add(screen.Clip);
        }
        Assert.Equal(PokemonAnimation.Clips.Append("idle"), seen);
        screen.NextClip(-1);
        Assert.Equal("walk", screen.Clip);
        // A move's clip plays to its end and is held there before it plays again
        screen.NextClip(-10);
        Assert.Equal("physical", screen.Clip);
        screen.Advance(GalleryScreen.ClipSeconds + 0.1f);
        Assert.Equal(1f, screen.Pose().Attack);
        screen.Advance(GalleryScreen.HoldSeconds);
        Assert.True(screen.Pose().Attack < 0.2f);
        // The people's clips are standing and every emote
        screen.ChangeTab();
        Assert.Equal(GalleryTab.People, screen.Tab);
        Assert.Equal(Emote.None, screen.Emote);
        Assert.All(PokemonAnimation.Clips, c => Assert.False(string.IsNullOrEmpty(GalleryScreen.ClipName(c))));
    }

    [Fact]
    public void TheTurnWrapsRoundAndFrontAndBackAreAHalfTurnApart()
    {
        var (screen, _) = Opened(Pikachu);
        Assert.True(screen.Spinning);
        screen.Advance(1f);
        Assert.Equal(GalleryScreen.SpinSpeed, screen.Turn, 4);
        screen.Spin(7f);
        Assert.False(screen.Spinning);
        Assert.InRange(screen.Turn, 0f, MathF.Tau);
        Assert.Equal((GalleryScreen.SpinSpeed + 7f) % MathF.Tau, screen.Turn, 4);
        screen.Spin(-screen.Turn - 1f);
        Assert.Equal(MathF.Tau - 1f, screen.Turn, 4);
        // Held by hand, it no longer turns by itself
        float held = screen.Turn;
        screen.Advance(2f);
        Assert.Equal(held, screen.Turn);
        // Facing nearly front (a radian short of a whole turn), P shows the back, and again the front
        Assert.False(screen.Back);
        screen.ToggleBack();
        Assert.True(screen.Back);
        Assert.Equal(MathF.PI, screen.Turn);
        screen.ToggleBack();
        Assert.False(screen.Back);
        Assert.Equal(0f, screen.Turn);
    }

    [Fact]
    public void AModelNotBuiltIsAskedForOnceAndItsSpriteStandsInUntilItIs()
    {
        var (screen, models) = Opened(Bulbasaur, Pikachu);
        for (int i = 0; i < 120; i++) screen.Advance(1f / 60f);
        Assert.Equal(new[] { "Bulbasaur" }, models.Asks);
        Assert.False(screen.ShowsModel);
        models.Built.Add("Bulbasaur");
        Assert.True(screen.ShowsModel);

        // Moving on lets go of the model the gallery was the first to ask for, and asks for the next once
        int pikachu = screen.Pokemon.ToList().FindIndex(e => e.Look == "Pikachu");
        screen.Move(pikachu);
        for (int i = 0; i < 30; i++) screen.Advance(1f / 60f);
        Assert.Equal(new[] { "Bulbasaur", "Pikachu" }, models.Asks);
        Assert.Equal(new[] { "Bulbasaur" }, models.Gone);
        Assert.Equal(new[] { "Pikachu" }, screen.Owned);

        // A model someone else holds is asked for but never let go by the gallery
        models.Elsewhere.Add("Bulbasaur");
        screen.Move(-pikachu);
        screen.Close();
        Assert.Equal(new[] { "Bulbasaur", "Pikachu" }, models.Gone);
        Assert.Empty(screen.Owned);
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void AnEntrysSpeciesOpensTheGalleryOnItsRow()
    {
        var models = new Models();
        var screen = new GalleryScreen(models);
        screen.Open(new[] { Bulbasaur, Pikachu, Rotom }, People, "Pikachu");
        Assert.Equal("Pikachu", screen.Selected!.Look);
        Assert.Equal(new[] { "Pikachu" }, models.Asks);

        // The Pokédex's A on an entry asks for the gallery on its species, once
        var pokedex = new Pokedex();
        pokedex.RegisterSeen(396);   // Starly
        var dex = new PokedexScreen();
        dex.Open(pokedex);
        int starly = dex.Rows.ToList().FindIndex(r => r.Number == 10);   // Starly, No. 10 in Sinnoh
        dex.SelectedIndex = starly;
        dex.Confirm();
        Assert.Equal(PokedexFocus.Entry, dex.Focus);
        Assert.Null(dex.TakeGalleryRequest());
        dex.Confirm();
        Assert.Equal("Starly", dex.TakeGalleryRequest());
        Assert.Null(dex.TakeGalleryRequest());
    }

    [Fact]
    public void TheTitleOffersTheGalleryOnlyWithASaveThatHasSeenSomething()
    {
        var blind = new TitleScreen(new SaveData { PlayerName = "Lucas" });
        Assert.DoesNotContain(TitleChoice.Gallery, blind.Entries);
        var seeing = new TitleScreen(new SaveData { PlayerName = "Lucas", SeenSpecies = new() { 387 } });
        Assert.Equal(new[] { TitleChoice.Continue, TitleChoice.NewGame, TitleChoice.Delete, TitleChoice.Home, TitleChoice.Jukebox, TitleChoice.Gallery, TitleChoice.Options, TitleChoice.Credits, TitleChoice.Quit }, seeing.Entries);
        // The column still clears the disclaimer with its eighth row
        var rects = seeing.EntryRects();
        Assert.True(rects[^1].Y + rects[^1].Height <= 1040);
    }

    [Fact]
    public void TheTurntablesFrameHoldsTheModelAtEveryTurnAndTheBakesAreAsTheyWere()
    {
        Assert.Equal(1, PokemonSprites.BakeVersion);
        foreach (var plan in new[] { BodyPlan.Quadruped, BodyPlan.Serpent, BodyPlan.Bird })
        {
            var model = PokemonModels.Sample(plan);
            var front = PokemonSprites.Framing(model, SpriteView.Front, PokemonSprites.Size);
            var f = PokemonSprites.Framing(model, SpriteView.Turntable, GalleryScreen.PictureSize);
            // The front's own framing is untouched by the turntable's
            Assert.Same(front, PokemonSprites.Framing(model, SpriteView.Front, PokemonSprites.Size));
            var cam = f.Camera;
            var up = Vector3.Normalize(cam.Up);
            var right = Vector3.Normalize(Vector3.Cross(cam.Target - cam.Position, up));
            float half = cam.FovY / 2f;
            for (int k = 0; k < 16; k++)
            {
                var turn = Matrix4x4.CreateRotationY(f.Yaw + k * MathF.Tau / 16f);
                foreach (var p in model.Mesh.Positions)
                {
                    var q = Vector3.Transform(p, turn) - cam.Target;
                    Assert.InRange(MathF.Abs(Vector3.Dot(q, right)), 0f, half);
                    Assert.InRange(MathF.Abs(Vector3.Dot(q, up)), 0f, half);
                }
            }
        }
    }
}
