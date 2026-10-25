# Models of our own

Approved `.glb` sculpts of the new species, **tracked by git** (unlike `overrides/models`, which git ignores). The game
reads this folder when it is started from the repository root, exactly as it reads `overrides/models`, and a file of
the same name in `overrides/models` wins, so a draft can be tried without touching what is committed.

Names are the species' (`Fakemon.glb`, `Fakemon-Mega.glb`): `docs/model-files.md` has the rules.

Only models made for this game belong here: our own sculpts, or drafts from a generator whose terms allow it, each
with its line in `docs/art/CREDITS.md`. Nothing taken from the games. Drafts that are not approved yet stay in
`tools/TripoSculpt/out/` (git-ignored); `dotnet run --project tools/TripoSculpt -- install <name>` copies an approved one here.
Keep each file under a few megabytes (clean it up in Blender first), and export with compression off.
