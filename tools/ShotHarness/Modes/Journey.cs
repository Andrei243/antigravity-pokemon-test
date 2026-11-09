partial class Harness
{
    /// <summary>
    /// The journey that ties the regions together (plan 41; not part of <c>all</c>; shots <c>eg</c> with a decade to each
    /// session). A part to a file of its own, so later sessions add theirs here without touching another's:
    /// <c>JOURNEY_FROM=rematches</c> runs EG5's alone (<c>eg50</c> to <c>eg52</c>); with nothing named every part runs.
    /// </summary>
    public void JourneyMode()
    {
        string? from = Environment.GetEnvironmentVariable("JOURNEY_FROM");
        bool Wants(string part) => string.IsNullOrEmpty(from) || string.Equals(from, part, StringComparison.OrdinalIgnoreCase);
        if (Wants("hub")) JourneyHub();
        if (Wants("rematches")) JourneyRematches();
    }
}
