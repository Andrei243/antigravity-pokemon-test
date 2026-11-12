using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models.Studio;

namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// A film shot on the battle (plan 42 · PS4; style guide, "The film studio"): as each menu opens, the film's director
/// is asked for the beats now due, whose lines go on the interlude a trainer's own line takes, named for their
/// speaker; a beat with lines to choose opens <see cref="LearnStep.Line"/> with them beside the box. The battle's
/// first menu is preceded by the title card, and the take's end by its ending's lines and its stars
/// (<see cref="WrapUpStudio"/>). The rules never know a film is shot: the director reads the log by an index of its
/// own and nothing here changes a choice, a roll or an event.
/// </summary>
public partial class BattleEngine
{
    private StudioDirector? studio;
    private int studioTakeNumber;
    private bool studioTitled;
    private StudioBeat? studioAsking;

    /// <summary>The film being shot, or null for any other battle.</summary>
    public StudioDirector? Studio => studio;

    /// <summary>The take as it ended, once <see cref="WrapUpStudio"/> has run; null until then.</summary>
    public StudioTake? StudioTake { get; private set; }

    /// <summary>The take is over and every line of its ending has been read.</summary>
    public bool StudioDone => StudioTake != null && steps.Count == 0 && interlude.Count == 0 && !waitingForMessageConfirm;

    /// <summary>The answers beside the open question: a film's lines, or Yes and No.</summary>
    public IReadOnlyList<string> LearnAnswers => LearnStep == LearnStep.Line && studioAsking != null
        ? studioAsking.Choices.Select(c => PlayerIdentity.Fill(Text.OfData(c.Text))).ToList()
        : new[] { Text.Of("battleMenu.yes"), Text.Of("battleMenu.no") };

    /// <summary>Where the script's own line stands among the open beat's.</summary>
    private int ScriptLineIndex => studioAsking == null ? 0 : Math.Max(0, studioAsking.Choices.FindIndex(c => c.Script));

    private void StartStudio(BattleSetup setup)
    {
        studio = setup.StudioFilm;
        studioTakeNumber = Math.Max(1, setup.StudioTakeNumber);
    }

    /// <summary>The menu is about to open on a film: its title card first, then each beat due, then the menu itself.</summary>
    private void StudioCue()
    {
        var film = studio!.Film;
        if (!studioTitled)
        {
            studioTitled = true;
            interlude.Enqueue(new Line(Text.Of("studio.titleCard", ("title", Text.Of(film.TitleKey)), ("genre", Text.Of(film.GenreKey)), ("take", studioTakeNumber)), null));
        }
        // A take out of turns asks no more beats: the game wraps it up at this menu
        var beat = core.Turn >= film.Battle.Turns ? null : studio.Next(core);
        if (beat == null)
        {
            if (interlude.Count > 0) interlude.Enqueue(new Happening(BeginChoosing));
            else BeginChoosing();
            return;
        }
        string speaker = PlayerIdentity.Fill(film.NameOf(beat.Speaker));
        foreach (string line in beat.Lines) interlude.Enqueue(new Line(PlayerIdentity.Fill(Text.OfData(line)), null, speaker));
        interlude.Enqueue(new Happening(beat.Choices.Count > 0 ? () => AskTheLine(beat) : StudioCue));
    }

    /// <summary>A beat's lines to choose come up beside the box, the last line read still in it.</summary>
    private void AskTheLine(StudioBeat beat)
    {
        studioAsking = beat;
        LearnStep = LearnStep.Line;
        LearnCursor = 0;
        CurrentSpeaker = null;
        HUD.MenuState = BattleMenuState.LearnMove;
    }

    /// <summary>The player says the line chosen, and the film goes on to the next beat due.</summary>
    private void SayTheLine(int answer)
    {
        var beat = studioAsking!;
        studioAsking = null;
        var choice = studio!.Choose(beat, beat.Choices[Math.Clamp(answer, 0, beat.Choices.Count - 1)].Id);
        AudioManager.PlaySound("select");
        HUD.MenuState = BattleMenuState.Message;
        interlude.Enqueue(new Line(PlayerIdentity.Fill(Text.OfData(choice.Text)), null, PlayerIdentity.Name));
        interlude.Enqueue(new Happening(StudioCue));
        Pump();
    }

    /// <summary>
    /// The take is over (the battle ended, or its turns ran out at a menu): its ending's lines and its stars are said,
    /// once. The game waits for <see cref="StudioDone"/> before it lets the battle go.
    /// </summary>
    public void WrapUpStudio()
    {
        if (studio == null || StudioTake != null) return;
        var take = studio.End(core.Result, core.Turn);
        StudioTake = take;
        var ending = studio.Film.Endings.First(e => e.Id == take.Ending);
        string speaker = PlayerIdentity.Fill(studio.Film.NameOf(ending.Speaker));
        foreach (string line in ending.Lines) interlude.Enqueue(new Line(PlayerIdentity.Fill(Text.OfData(line)), null, speaker));
        interlude.Enqueue(new Line(Text.Of("studio.stars", ("stars", take.Stars)), null));
        HUD.MenuState = BattleMenuState.Message;
        Pump();
    }
}
