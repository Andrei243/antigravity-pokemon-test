using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The card the player designs (plan 43 · LF1; style guide, "Trainer Card" and "The card designer"): the item button on
/// the card opens the designer, the design is kept in the save (<see cref="SaveData.CardDesign"/>), the greeting is a
/// phrase of the story (<see cref="CardDesign.GreetingPhrase"/>), the posed figure is rendered offscreen only with a
/// window, and PRINT writes the two sides as one picture into the screenshots folder. A card never designed draws as
/// it always did.
/// </summary>
public partial class GameEngine
{
    private static readonly TrainerCardFace EmptyCardFace = new(null, null, null, null, Array.Empty<Pokemon>(), "", "", null);

    // The posed figure's picture, made while the card or the designer is up and let go after
    private RenderTexture2D cardFigure;
    private bool cardFigureLoaded, cardFigureDrawn;

    // The face as last resolved, and what it was resolved from (the six's ids, the signature)
    private TrainerCardFace? cardFace;
    private string? cardFaceKey;
    private Dictionary<Guid, Pokemon>? cardOwned;

    // PRINT (plan 43 · LF1): 1 draws the front this frame, 2 the back, each taken once drawn
    private int cardPrintStage;
    private Image cardPrintFront;

    /// <summary>The last card PRINT wrote this run; null before any.</summary>
    public string? LastCardPrint { get; private set; }

    /// <summary>A game's card as its save had it, or none.</summary>
    private void LoadTheLife(SaveData? save)
    {
        flow.CardDesign = save?.CardDesign?.Copy();
        flow.PhotoBook.Load(save?.PhotoBook);
        cardFace = null;
        cardFaceKey = null;
        cardOwned = null;
    }

    /// <summary>The design as the save keeps it: the six's ids of Pokémon no longer the player's dropped (released, traded).</summary>
    private CardDesign? CardDesignToSave()
    {
        if (flow.CardDesign is not { } design) return null;
        var owned = OwnedPokemon().Select(p => p.Id).ToHashSet();
        design.Forget(owned.Contains);
        return design.Copy();
    }

    /// <summary>The design the card shows now: the designer's while it is open, else the game's.</summary>
    private CardDesign? DesignNow => flow.CardDesignScreen.IsActive ? flow.CardDesignScreen.Design : flow.CardDesign;

    /// <summary>What the card's line under the name says: the design's title, or (never designed) the latest master's.</summary>
    private string CardTitleNow()
    {
        string latest = MasterTitles.Latest(flow.Story) is { } m ? MasterTitles.Title(m) : "";
        if (DesignNow is not { } design) return latest;
        return design.Title switch
        {
            CardTitleKind.Master => latest,
            CardTitleKind.Champion when RegionDatabase.Get(design.TitleRegion ?? "") is { } region => ChampionTitle(region),
            _ => ""
        };
    }

    private static string ChampionTitle(Region region) =>
        Text.Of("cardDesign.title.champion", ("region", Text.Of($"dexText.region.{region.Generation}")));

    /// <summary>What the card's looks wait for, as the game stands.</summary>
    internal CardFacts CardFactsNow()
    {
        var whole = RegionDatabase.All.Where(r => r.BadgeNames.Count > 0 && flow.Story.BadgeCount(r.Id) >= r.BadgeNames.Count).Select(r => r.Id);
        var champions = RegionDatabase.All.Where(r => flow.Story.IsRegionComplete(r.Id)).Select(r => r.Id);
        var medals = Enum.GetValues<MedalCategory>().Where(c => Medals.In(c).All(flow.Medals.Has)).Select(c => c.ToString());
        return new CardFacts
        {
            BadgesWhole = new HashSet<string>(whole, StringComparer.OrdinalIgnoreCase),
            Champions = new HashSet<string>(champions, StringComparer.OrdinalIgnoreCase),
            MedalsWhole = new HashSet<string>(medals, StringComparer.OrdinalIgnoreCase),
            Diplomas = new HashSet<string>(flow.Pokedex.Diplomas.Select(Pokedex.DiplomaKey), StringComparer.OrdinalIgnoreCase),
            SpotsTaken = new HashSet<string>(PhotoSpotsWhole(), StringComparer.OrdinalIgnoreCase)
        };
    }

    /// <summary>The titles the card may carry: the latest master's once one is won, and each Hall of Fame entered.</summary>
    private List<CardTitleChoice> CardTitlesNow()
    {
        var titles = new List<CardTitleChoice>();
        if (MasterTitles.Latest(flow.Story) is { } m) titles.Add(new CardTitleChoice(CardTitleKind.Master, null, MasterTitles.Title(m)));
        foreach (var r in RegionDatabase.All.Where(r => !r.BorrowsRules && flow.Story.IsRegionComplete(r.Id)))
            titles.Add(new CardTitleChoice(CardTitleKind.Champion, r.Id, ChampionTitle(r)));
        return titles;
    }

    /// <summary>The designer, opened from the card (the item button), on the design as it stands.</summary>
    internal void OpenCardDesigner()
    {
        // A Pokémon caught since the last save is given its id first, so the six can name it
        GiveIds();
        cardOwned = null;
        var candidates = flow.Party.Members.Concat(flow.Boxes.All)
            .Select(p => new CardCandidate(p.Id, p.DisplayName, p.Level, p)).ToList();
        var facts = CardFactsNow();
        flow.CardDesignScreen.Open(flow.CardDesign, CardLooks.BackgroundsOpen(facts, flow.FashionCase.Backdrops), CardLooks.FramesOpen(facts),
            CardTitlesNow(), candidates, PhraseGroups.Shown(flow.Story.PhraseOf(CardDesign.GreetingPhrase) ?? Array.Empty<string>()));
        flow.State = GameState.CardDesign;
    }

    /// <summary>The designer's frame: its own buttons, then what it asks of the game (the greeting's picker, a print), and back to the card once it is left.</summary>
    private void UpdateCardDesigner(float dt)
    {
        // Nothing moves while the card is being printed (two frames)
        if (cardPrintStage > 0) return;
        var screen = flow.CardDesignScreen;
        screen.Update(dt);
        if (screen.TakeGreetingRequest())
        {
            flow.PhrasesReturn = GameState.CardDesign;
            OpenPhrases(CardDesign.GreetingPhrase);
            return;
        }
        if (screen.TakePrintRequest())
        {
            flow.CardDesign = screen.Design.Copy();
            if (!IsHeadless) cardPrintStage = 1;
        }
        if (!screen.IsActive)
        {
            flow.CardDesign = screen.Design.Copy();
            flow.State = GameState.TrainerCard;
            cardFaceKey = null;
        }
    }

    /// <summary>The greeting's picker closed: a phrase given is kept as the card's greeting, and the designer comes back.</summary>
    private void CloseGreetingPicker()
    {
        if (flow.PhraseScreen.Outcome is IReadOnlyList<string> phrase && PhraseGroups.IsWhole(phrase))
        {
            flow.Story.SetPhrase(CardDesign.GreetingPhrase, phrase);
            flow.CardDesignScreen.Greeting = PhraseGroups.Shown(phrase);
        }
        flow.State = flow.CardDesignScreen.IsActive ? GameState.CardDesign : GameState.TrainerCard;
    }

    /// <summary>
    /// The designed card's face as it stands (null for a card never designed): resolved again only when what it shows
    /// has changed, since the card asks for it every frame it is drawn.
    /// </summary>
    internal TrainerCardFace? CardFaceNow()
    {
        var design = DesignNow;
        if (design == null) return null;
        bool designing = flow.CardDesignScreen.IsActive;
        var pad = designing ? flow.CardDesignScreen.Pad : null;
        string greeting = PhraseGroups.Shown(flow.Story.PhraseOf(CardDesign.GreetingPhrase) ?? Array.Empty<string>());
        string key = string.Join("|", design.Background, design.Frame, design.Shown == null ? "team" : string.Join(",", design.Shown),
            greeting, CardTitleNow(), designing ? "pad" : design.Signature ?? "", cardFigureDrawn, flow.Party.Count);
        if (cardFace != null && key == cardFaceKey)
            return cardFace with { Figure = FigureNow(), Signature = pad ?? cardFace.Signature };

        cardOwned ??= OwnedPokemon().Where(p => p.Id != Guid.Empty).GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
        var shown = design.ShownOf(flow.Party.Members, cardOwned);
        var signature = pad ?? (design.Signature is { } s ? SignaturePad.FromSaved(s) : null);
        cardFace = new TrainerCardFace(CardLooks.Background(design.Background), CardLooks.BackdropOf(design.Background), CardLooks.Frame(design.Frame),
            FigureNow(), shown, greeting, CardTitleNow(), signature);
        cardFaceKey = key;
        return cardFace;
    }

    /// <summary>The regions whose five photo spots are all taken (plan 43 · LF4).</summary>
    private IEnumerable<string> PhotoSpotsWhole() => flow.PhotoBook.RegionsWhole();

    private Texture2D? FigureNow() => cardFigureLoaded && cardFigureDrawn ? cardFigure.Texture : null;

    /// <summary>
    /// The posed figure, rendered offscreen (before the virtual screen's texture mode) while the card or the designer
    /// is up and there is a design: the player as the field draws them, in the pose and the face chosen, held still. A
    /// look not made yet is asked for and never waited for.
    /// </summary>
    private void RenderCardFigure()
    {
        if (IsHeadless || DesignNow is not { } design) return;
        if (!cardFigureLoaded)
        {
            cardFigure = Gpu.Target(ModernUi.CardFigureWidth, ModernUi.CardFigureHeight, "ui");
            Raylib.SetTextureFilter(cardFigure.Texture, TextureFilter.Bilinear);
            cardFigureLoaded = true;
        }
        string look = InTheKitHere(PlayerIdentity.Character);
        if (CharacterModels.TryGet(look, renderContext.Shaders) == null) return;
        var pose = new CharacterPose { Time = 0.5f, Expression = CharacterAnimation.ExpressionOf(design.Mood) };
        if (design.Pose == CardPoseKind.Sitting) pose.Stance = Overworld.Stance.SitGround;
        else if (design.Pose == CardPoseKind.Gesture && design.Gesture is { } gesture)
        {
            pose.Emote = CharacterAnimation.EmoteOf(gesture);
            pose.EmoteTime = CardDesign.HeldAt * CharacterAnimation.Duration(pose.Emote);
        }
        CharacterStudio.Render(renderContext, look, cardFigure, pose, yaw: 0.3f, viewHeight: 1.9f, lookY: 0.78f);
        cardFigureDrawn = true;
    }

    /// <summary>The figure's picture let go, and the face forgotten, once the card is put away.</summary>
    private void ForgetCardFigure()
    {
        if (cardFigureLoaded)
        {
            Gpu.Unload(cardFigure);
            cardFigureLoaded = cardFigureDrawn = false;
        }
        cardFace = null;
        cardFaceKey = null;
        cardOwned = null;
    }

    /// <summary>While the card is printed, the card's side that frame (instead of the designer).</summary>
    internal bool PrintingCard => cardPrintStage > 0;

    /// <summary>
    /// PRINT, once each frame is drawn: the front is taken first, then the back, and the two halved and stood one over
    /// the other are written as one picture into the screenshots folder, named as the screenshot key names.
    /// </summary>
    private void TakeCardPrint()
    {
        if (cardPrintStage == 0) return;
        var image = Raylib.LoadImageFromTexture(virtualScreen.Texture);
        Raylib.ImageFlipVertical(ref image);
        Raylib.ImageResize(ref image, VirtualWidth, VirtualHeight);
        if (cardPrintStage == 1)
        {
            cardPrintFront = image;
            cardPrintStage = 2;
            return;
        }
        cardPrintStage = 0;
        var sheet = Raylib.GenImageColor(VirtualWidth, VirtualHeight * 2, Color.Black);
        Raylib.ImageDraw(ref sheet, cardPrintFront, new Rectangle(0, 0, VirtualWidth, VirtualHeight), new Rectangle(0, 0, VirtualWidth, VirtualHeight), Color.White);
        Raylib.ImageDraw(ref sheet, image, new Rectangle(0, 0, VirtualWidth, VirtualHeight), new Rectangle(0, VirtualHeight, VirtualWidth, VirtualHeight), Color.White);
        Raylib.UnloadImage(cardPrintFront);
        Raylib.UnloadImage(image);
        try
        {
            string folder = SavePaths.Current.Screenshots;
            Directory.CreateDirectory(folder);
            string path = Screenshots.PathFor(folder, WallClock.Local, File.Exists);
            Raylib.ExportImage(sheet, path);
            LastCardPrint = path;
            AudioManager.PlaySound("shutter");
            flow.Toast.Show(Text.Of("cardDesign.print.done", ("file", Path.GetFileName(path))));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"The card couldn't be printed: {e.Message}");
            flow.Toast.Show(Text.Of("cardDesign.print.notDone", ("why", e.Message)));
        }
        Raylib.UnloadImage(sheet);
    }

    /// <summary>The card drawn as PRINT takes it: its front, then its back.</summary>
    private void DrawCardForPrint() =>
        ModernUi.DrawTrainerCard(VirtualWidth, VirtualHeight, TrainerCardNow(), null, 1f, back: cardPrintStage == 2);

    internal sealed partial class Driver
    {
        /// <summary>The card designer (plan 43 · LF1).</summary>
        public CardDesignScreen CardDesigner => game.flow.CardDesignScreen;

        /// <summary>The card's design as the game keeps it; null for a card never designed.</summary>
        public CardDesign? CardDesign { get => game.flow.CardDesign; set { game.flow.CardDesign = value; game.cardFaceKey = null; } }

        /// <summary>The item button pressed on the card: the designer opens.</summary>
        public void OpenCardDesigner() => game.OpenCardDesigner();

        /// <summary>What the Trainer Card shows now, its design's face among it.</summary>
        public TrainerCardInfo CardNow => game.TrainerCardNow();

        /// <summary>What the card's looks wait for, as the game stands.</summary>
        public CardFacts CardFacts => game.CardFactsNow();

        /// <summary>The last card PRINT wrote; null before any.</summary>
        public string? LastCardPrint => game.LastCardPrint;
    }
}

