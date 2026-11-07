using System;

partial class Harness
{
    // ---------------------------------------------------------------- gifts (plan 40)

    // gifts (plan 40; not part of `all`; shots gf*): the rewards that come from outside a battle, a part to a session and
    // a file to a part (Modes/Gifts<Part>.cs), so later sessions add theirs without touching the others':
    //   items (plan 40 · GE11, Modes/GiftsItems.cs): what the finished regions owed of the items the rules run, the DNA
    //     Splicers in the Giant Chasm, a drive in Driftveil, Prof. Rowan's stones of our own Megas and Mom's eon stones in
    //     Hoenn's post-game (gf100 to gf103).
    // GIFTS_PART=items (a list, by commas) runs those parts alone; every part with none named, in this order.
    public void GiftsMode()
    {
        string only = Environment.GetEnvironmentVariable("GIFTS_PART") ?? "";
        bool Part(string name) => only.Length == 0 || only.Split(',', StringSplitOptions.TrimEntries).Contains(name);
        if (Part("items")) GiftsItems();
    }
}
