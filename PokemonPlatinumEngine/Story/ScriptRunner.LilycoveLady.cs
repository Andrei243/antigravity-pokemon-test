using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 21 · H11, part c: <c>favorlady</c>, the Lilycove Lady's favour (<see cref="LilycoveLady"/>). What she
/// remembers is the story's own variables, so a game saved in the middle of a favour keeps it.
/// </summary>
public sealed partial class ScriptRunner
{
    /// <summary>
    /// <c>which</c>: the favour she asks for, 0 to 5 (slippery, roundish, whamish, shiny, sticky, pointy), drawn the first time
    /// she is asked. <c>give</c>: the item last chosen (<c>chooseitem gift</c>) handed over and taken from the bag; RESULT 0 when
    /// it wasn't of her kind, 1 when it was, 2 when it was the fifth of her kind or the one she wanted most, which earns the
    /// prize. <c>new</c>: a new favour drawn, the count back at nought.
    /// </summary>
    private void FavorLadyCommand(Instruction i)
    {
        var story = host.Story;
        int Draw() => host.Chance.Next(0x10000);
        void Begin()
        {
            var (favor, best) = LilycoveLady.Pick(Draw);
            story.SetVar(LilycoveLady.FavorVariable, favor + 1);
            story.SetVar(LilycoveLady.BestVariable, best);
            story.SetVar(LilycoveLady.GivenVariable, 0);
        }
        switch (i.Name)
        {
            case "which":
                if (story.Var(LilycoveLady.FavorVariable) == 0) Begin();
                Result = story.Var(LilycoveLady.FavorVariable) - 1;
                break;
            case "give":
            {
                if (story.Var(LilycoveLady.FavorVariable) == 0) Begin();
                int favor = story.Var(LilycoveLady.FavorVariable) - 1;
                if (lastItem is not { } name || ItemDatabase.Get(name) is not { } item || !host.Bag.RemoveItem(item, 1))
                    throw Wrong(i, "'favorlady give' needs an item chosen from the bag ('chooseitem gift')");
                int before = story.Var(LilycoveLady.GivenVariable);
                int after = LilycoveLady.CountAfter(favor, story.Var(LilycoveLady.BestVariable), before, name);
                story.SetVar(LilycoveLady.GivenVariable, after);
                Result = after >= LilycoveLady.Threshold ? 2 : after > before ? 1 : 0;
                break;
            }
            case "new":
                Begin();
                Result = story.Var(LilycoveLady.FavorVariable) - 1;
                break;
        }
    }
}
