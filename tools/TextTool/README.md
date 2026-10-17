# TextTool

Writes the skeleton of a language's translation files for the game's scripts, overlays and hand-made rooms, and reports what each language still lacks. Plan 17 · N4. No window; the files are described in `docs/data-files.md`, "Translating the scripts, the overlays and the rooms".

```bash
dotnet run --project tools/TextTool -- extract <tag>              # scripts/, world/ and maps/ files of the language, every English line, an empty text; files that exist are left alone
dotnet run --project tools/TextTool -- extract <tag> --update     # bring the existing files up to date: new lines added, rewritten ones stale (the old text kept in "was"), lines that are gone dropped
dotnet run --project tools/TextTool -- report                     # every language that has files: per file, what is untranslated, stale or unknown, and what is wrong; rewrites docs/localisation.md
dotnet run --project tools/TextTool -- report <tag> --lines       # one language, naming each line
dotnet run --project tools/TextTool -- report --check             # ends with 1 if docs/localisation.md is not what the files give
```

`<tag>` is a language of `Data/text/languages.json` other than English and the pseudo-language. It reads the game's own files from the build output (`dotnet run` copies them) and writes under `PokemonPlatinumEngine/Data/text/<tag>/`, found by walking up to `PokemonPlatinum.sln`.

**What the report calls things.** A line is *translated* when its entry has text and its `en` is the English the game has now; *stale* when the English was rewritten after (the game shows the English); *unknown* when the entry matches nothing the game has (a script, a person or a sign that is gone, or a file nobody's). *Wrong* is a translation that uses a placeholder or a markup code its English line lacks, or has a different number of lines of text. `ScriptTranslationTests` holds `docs/localisation.md` to the files and every language's files to the game, so a language that goes wrong fails the build.
