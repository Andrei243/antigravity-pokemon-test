namespace PokemonPlatinumEngine.Battle;

// A tale's sky on screen (plan 46 · CP11; style guide, "Move effects (G8)", "The stage's light"): the tale told over the
// field, set by the log's TaleTold, TaleAnswered and TaleEnded as their lines are read and never read from core.Field,
// which is a turn ahead; SyncFromCore only brings it level once a menu opens (SettleTale), with nothing played. The
// sky eases in over TaleSkyIn seconds of the animator's own clock (StageLights.TaleLight).
public sealed partial class BattleAnimator
{
    /// <summary>How long a tale's sky takes to come in, in seconds.</summary>
    public const float TaleSkyIn = 0.6f;

    /// <summary>The tale told over the field as the screen has it, or null.</summary>
    public string? Tale { get; private set; }

    /// <summary>The animator's time the tale shown began to be told.</summary>
    public float TaleSince { get; private set; }

    /// <summary>A tale is told (or answers another): its sky comes in from now.</summary>
    public void TellTale(string? tale)
    {
        if (tale == Tale) return;
        Tale = tale;
        TaleSince = Time;
    }

    /// <summary>The rules' tale once a menu opens: taken at once, its sky whole.</summary>
    public void SettleTale(string? tale)
    {
        if (tale == Tale) return;
        Tale = tale;
        TaleSince = Time - TaleSkyIn;
    }

    /// <summary>How far the tale's sky has come in, 0..1.</summary>
    public float TaleStrength => Tale == null ? 0f : System.Math.Clamp((Time - TaleSince) / TaleSkyIn, 0f, 1f);
}
