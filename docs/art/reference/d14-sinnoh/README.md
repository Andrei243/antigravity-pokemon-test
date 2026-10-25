# D14 batch 4 (Sinnoh): reference drawings

Thirty-nine flat drawings of the species of our own that plan 03 · D14's fourth batch proposes (`docs/plans/D14-proposals-sinnoh.md`, Nos. 2400 to 2482, approved in full by the user), one SVG per creature named `<No>-<Name>.svg`, and `index.html`, a contact sheet of all of them with their names, numbers, typings and what each evolves from (open it in a browser; it loads nothing from outside).

**They are references only.** They exist so that the sessions that sculpt these species in 3D (`PokemonModels`, the SDF kit) have a picture of the proportions, the colours and the one new feature each design adds. The game never reads them: the engine's project copies only `Assets/` and `Data/` to its output, and nothing in `docs/` is built, copied or shipped. They are not ignored by git either; they are kept in the repository as design notes.

## How they were made

- Each is drawn by hand as SVG paths and shapes from its entry's written **Look**, typing, colours, height and weight in the proposal, and from nothing else: our own designs, not traced from or modelled on any existing artwork.
- The look is the clean "official artwork" one: one creature, roughly centred, a plain white background with a pale ground shadow, a bold outline (`#1b1b2b`, 5 units on a 512 × 512 view box), three to five flat body colours, one shade tone and a highlight. No text in the pictures.
- Dryadeon and Borealeon share one base drawing (pose, body, legs, head); the lake guardians Uxinous, Mespathos and Azelthymos share another (body, the three tails and their gems, the ring of light), each with its own head.
- Each was rendered with a headless browser and looked at, and the ones that read badly were drawn again.

## What a sculptor can take from them, and what not

- **Can**: the silhouette and its proportions, where each colour goes, the new feature each design adds (Maestrikket's blade and bow, Voltglider's membrane, Ossuarion's cairn, Lodestarch's star of five Mini-Noses, and the rest), and which parts glow.
- **Can't**: anything from behind or from the side not shown, exact volumes, or a final palette. Most are straight-on or nearly so, a few in profile; hand-written SVG is simple, and a drawing that disagrees with the proposal's written Look is wrong where it disagrees. The written Look is the design; these pictures only illustrate it.
- The base species' own looks are this game's sculpts as the proposal describes them, not the original games'.
