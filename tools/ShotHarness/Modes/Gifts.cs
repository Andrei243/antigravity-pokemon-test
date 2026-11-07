using System;

partial class Harness
{
    // ---------------------------------------------------------------- gifts (plan 40)

    // gifts (plan 40; not part of `all`; shots gf*): the rewards that come from outside a battle, a part to a session and
    // a file to a part (Modes/Gifts<Part>.cs), so later sessions add theirs without touching the others':
    //   charms (plan 40 · GE5, Modes/GiftsCharms.cs): a designer handing over the charms two Pokédexes seen whole have
    //     earned, the bag's Key Items with them, a critical capture rattling in the air and holding, and the EXP the catch
    //     gives with the Exp. Charm's share on its line (gf40 to gf44).
    //   items (plan 40 · GE11, Modes/GiftsItems.cs): what the finished regions owed of the items the rules run, the DNA
    //     Splicers in the Giant Chasm, a drive in Driftveil, Prof. Rowan's stones of our own Megas and Mom's eon stones in
    //     Hoenn's post-game (gf100 to gf103).
    // Later sessions of plan 40 add parts of their own, a line each above.
    // GIFTS_PART=charms,items (a list, by commas) runs those parts alone; every part with none named, in this order.
    public void GiftsMode()
    {
        string only = Environment.GetEnvironmentVariable("GIFTS_PART") ?? "";
        bool Part(string name) => only.Length == 0 || only.Split(',', StringSplitOptions.TrimEntries).Contains(name);
        if (Part("charms")) GiftsCharms();
        if (Part("items")) GiftsItems();
    }
}
