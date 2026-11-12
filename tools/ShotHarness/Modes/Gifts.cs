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
    //   diplomas (plan 40 · GE4, Modes/GiftsDiplomas.cs): the Pokédex of our own's list and an entry of it, the OURS pill
    //     in the National list, the diplomas of Johto's and of our own Pokédex and Hoenn's game designer reading them
    //     (gf30 to gf34); last, each part beginning a game of its own, so nothing before it moves.
    //   giftbox (plan 40 · GE7, Modes/GiftsBox.cs): the questionnaire in Jubilife TV's lobby, a Gift Runner, the box
    //     waiting, a card, a gift taken and the album (gf60 to gf66).
    //   board (plan 40 · GE8, Modes/GiftsBoard.cs): the Global Terminal's trade board, a request met, a blind trade and
    //     the backdrop lady (gf70 to gf75).
    //   festivals (plan 40 · GE9, Modes/GiftsFestivals.cs): each festival's town dressed on its days, by day and after
    //     dark, a stall and the holidays (gf80 to gf8d), the harness's day moved and put back.
    // Later sessions of plan 40 add parts of their own, a line each above.
    // GIFTS_PART=charms,items,diplomas,giftbox,board,festivals (a list, by commas) runs those parts alone; every part with none named, in this order.
    public void GiftsMode()
    {
        string only = Environment.GetEnvironmentVariable("GIFTS_PART") ?? "";
        bool Part(string name) => only.Length == 0 || only.Split(',', StringSplitOptions.TrimEntries).Contains(name);
        if (Part("charms")) GiftsCharms();
        if (Part("items")) GiftsItems();
        if (Part("diplomas")) GiftsDiplomas();
        if (Part("giftbox")) GiftsBox();
        if (Part("board")) GiftsBoard();
        if (Part("festivals")) GiftsFestivals();
    }
}
