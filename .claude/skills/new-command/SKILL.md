---
name: new-command
description: Use when the user wants a new Civil 3D command or wants to change what an existing one does. Turns a plain-English request into a command that builds, then tells them how to try it.
---

# New Civil 3D command

1. **Pin down the request.** Restate it in two or three short lines: which objects,
   all of them or ones they pick, what changes, what it prints at the end. Ask only
   about gaps that change the result (units, rounding, what to do with locked or
   missing data). One question at a time, in plain words.

2. **Find the API.** Check `docs/recipes.md` first, then search `api-docs/<year>/`
   for the classes and members you need. Read an existing command in
   `src/MyTools/Commands/` so the new one looks the same.

3. **Write it** in `src/MyTools/Commands/<Name>.cs` following the rules in AGENTS.md:
   `MY` name, `[Description]`, `Out.Line`, one transaction, ask before changing many
   objects, skip what can't be edited, summary at the end.

4. **Build** with `scripts\build.cmd` until there are no errors or new warnings. On a
   compile error about an API member, look it up again instead of guessing.

5. **Hand it over** using "After a build, tell the user" in AGENTS.md.

6. **When they report back,** read `logs/last-run.txt` before anything else. Once it
   works, offer to commit.
