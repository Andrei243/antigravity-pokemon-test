using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The cards of the people beaten (plan 43 · LF2; <see cref="CardAlbum"/>): a battle of the field won gives the card
/// of each Leader, Elite Four member or Champion across it that the album lacks, the Trainer Card's last page shows
/// them, and their busts are rendered offscreen only for the cards in view and only with a window. Nothing of this
/// draws from the game's chance or reaches a battle's rules.
/// </summary>
public partial class GameEngine
{
    // The cards as the page shows them, resolved once (the card asks every frame it is drawn)
    private IReadOnlyList<AlbumEntry>? albumShown;

    private readonly CardPortraits albumBusts = new();

    /// <summary>A game's cards as its save had them, or none.</summary>
    private void LoadTheAlbum(SaveData? save)
    {
        flow.CardAlbum.Load(save?.CardAlbum);
        albumShown = null;
    }

    /// <summary>
    /// A battle of the field is over: won against trainers, each of them across the field who is a Leader, one of an
    /// Elite Four or a Champion gives their card, the first time. The look kept is the one the battle drew them in.
    /// (A battle of the Frontier, a tournament's, a link battle's, a film's and a recording watched end elsewhere.)
    /// </summary>
    private void CardsAfterBattle(BattleEngine battle)
    {
        if (battle.Result != BattleResult.PlayerVictory || !battle.IsTrainerBattle) return;
        foreach (var trainer in battle.Trainers)
            if (flow.CardAlbum.Give(trainer.Id, PlayerIdentity.Fill(trainer.Name), CharacterStyles.Default.InBattle(trainer)) != null)
                albumShown = null;
    }

    /// <summary>The album as the Trainer Card's page shows it; empty while no card has been won.</summary>
    internal IReadOnlyList<AlbumEntry> AlbumNow() =>
        albumShown ??= flow.CardAlbum.Cards.Count == 0 ? Array.Empty<AlbumEntry>() : flow.CardAlbum.Shown().Select(EntryOf).ToList();

    /// <summary>The album read again, in the language chosen now: as the Trainer Card is opened.</summary>
    private int ReadTheAlbum()
    {
        albumShown = null;
        return AlbumNow().Count;
    }

    /// <summary>
    /// A card made from the data: the rank and region of its team, the team's species as the table has them (never a
    /// copy a run or a crossed region changed), the colours of their band (the plain one where a region has none) and
    /// the first of their lines in battle, where they have any.
    /// </summary>
    private AlbumEntry EntryOf(AlbumCard card)
    {
        var (regionId, rank, dataName) = CardAlbum.PersonOf(card.Id)!.Value;
        var record = TrainerDatabase.Get(card.Id)!;
        var region = RegionDatabase.Get(regionId);
        // The band's table knows a person by their class and name; Kanto's Champion by the rival's placeholder
        string bandClass = CardAlbum.ClassOf(rank);
        string bandName = TrainerDatabase.IsRivalAsChampion(record) ? SceneTransition.RivalPlaceholder : dataName;
        string name = card.Name.Length > 0 ? card.Name : dataName;
        var band = SceneTransition.BandOf(bandClass, bandName) with { Name = name };
        string look = CardAlbumLook(card.Look, region);
        // (as the data builds each, so a female with a look of her own is drawn in it; built from her personality alone)
        var team = record.Party.Select(m => new AlbumMon(TrainerDatabase.Build(m).LookName, LocalNames.Species(m.Species))).ToList();
        int generation = region?.Generation ?? 4;
        string line = TrainerLines.SetOf(card.Id)?.Lines.FirstOrDefault() is { } said
            ? RichText.Plain(PlayerIdentity.Fill(Text.OfData(said.Text)))
            : "";
        return new AlbumEntry(name, Text.Of($"cardAlbum.rank.{rank}"), Text.Of($"dexText.region.{generation}").ToUpperInvariant(),
            look, team, band, line);
    }

    // A card's person is of their own region's kit, wherever the card is looked at (plan 30)
    private static string CardAlbumLook(string look, Region? region) =>
        region?.Kit == CharacterKit.Grown && CharacterModels.KindOf(look) == RigKind.Humanoid ? CharacterStyle.InKit(look, CharacterKit.Grown) : look;

    /// <summary>The busts the page has in view: the sheet's nine, and the opened card's, large, over them.</summary>
    internal IReadOnlyList<(string Look, bool Large)> AlbumBustsInView()
    {
        var screen = flow.TrainerCardScreen;
        if (!screen.ShowsCards) return Array.Empty<(string, bool)>();
        var album = AlbumNow();
        if (album.Count == 0) return Array.Empty<(string, bool)>();
        var views = album.Skip(screen.Sheet * TrainerCardScreen.CardsPerSheet).Take(TrainerCardScreen.CardsPerSheet)
            .Select(c => (c.Look, Large: false)).Distinct().ToList();
        // (the nine are kept while one is opened, so that putting it back shows them at once)
        if (screen.CardOpened) views.Add((album[Math.Min(screen.CardCursor, album.Count - 1)].Look, true));
        return views;
    }

    /// <summary>The offscreen phase of a frame: the busts of the cards in view, and none once the page is left.</summary>
    private void RenderTheAlbumsBusts(GameState scene)
    {
        if (IsHeadless) return;
        if (scene != GameState.TrainerCard || !flow.TrainerCardScreen.ShowsCards) albumBusts.End();
        else albumBusts.Show(renderContext, AlbumBustsInView());
    }

    private Texture2D? AlbumBustNow(string look, bool large) => albumBusts.Picture(look, large);

    internal sealed partial class Driver
    {
        /// <summary>The cards of the people beaten, as the game keeps them (plan 43 · LF2).</summary>
        public CardAlbum CardAlbum => game.flow.CardAlbum;

        /// <summary>The cards as the Trainer Card's page shows them.</summary>
        public IReadOnlyList<AlbumEntry> AlbumShown => game.AlbumNow();

        /// <summary>The Trainer Card opened as the start menu's entry opens it.</summary>
        public void OpenTrainerCard() => game.HandleStartMenuChoice(StartMenuChoice.Trainer);

        /// <summary>The busts in view, and how many of those asked for are rendered (nought with no window).</summary>
        public IReadOnlyList<(string Look, bool Large)> AlbumBustsInView => game.AlbumBustsInView();
        public int AlbumBustsRendered => game.albumBusts.Rendered;
    }
}
