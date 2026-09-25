# Working in this repo

This repo builds Civil 3D commands in C#. The person you're working with is
probably a civil engineer, not a programmer. They describe what they want, you
write and build the command, they run it in Civil 3D.

Talk to them in plain words. Don't ask them to edit code or run developer
commands; do that yourself. When they need to do something in Civil 3D, give
numbered steps with the exact command names to type.

## How it fits together

- `src/MyTools/Commands/` holds the commands, one class per file. This is where you work.
- `src/MyTools/Helpers/` has `Out` (print and log) and `Ask` (Yes/No, pick one, pick many).
- `src/Loader/` is the loader Civil 3D starts with. It loads `build\Debug\MyTools.dll`
  from memory and swaps in each new build a couple of seconds after it lands, so the
  user never restarts Civil 3D or runs NETLOAD. Leave it alone unless asked.
- `docs/recipes.md` has snippets for common Civil 3D jobs that are known to compile.
- `api-docs/<year>/` has Autodesk's own API reference (XML), copied there by the build.
  It's gitignored, so search it by path.
- `samples/` may hold scripts the user wrote before. Match their style where it makes sense.
- `logs/last-run.txt` shows what the last command printed and any error with a stack trace.

## Build

```
scripts\build.cmd
```

From Git Bash use `cmd //c scripts\\build.cmd`. From PowerShell use `.\scripts\build.cmd`.
If the build says no .NET SDK is installed, run
`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/setup.ps1 -Yes`.

A change isn't finished until the build has no errors and no new warnings. The
Civil 3D year comes from `Directory.Build.props` (newest installed unless pinned).
Only use API members that exist in the API docs for that year.

## After a build, tell the user

1. What the command does, in one sentence.
2. How to try it: switch to Civil 3D, wait for "MyTools: N command(s) ready" on the
   command line (or type `MYRELOAD`), then type the command name.
3. What they should see, and anything only a real run will show.
4. To try it on a copy of a drawing when it changes things.

## When they say it didn't work

Read `logs/last-run.txt` first. It has the command's output and the full error.
Then fix, build, and tell them to run it again. If the log is missing or old, the
command didn't run: check that they saw the "ready" message and typed the right name.

If a build fails with "being used by another process", Civil 3D has the DLL loaded
through NETLOAD. Ask them to close Civil 3D and never NETLOAD `build\Debug\MyTools.dll`;
the loader handles it.

## Writing commands

- Name commands `MY` plus a short word (`MYPIPEREPORT`). `MYTOOLS` and `MYRELOAD` are taken.
- Put `[Description("...")]` on every command. `MYTOOLS` shows it as the menu.
- Print with `Out.Line`, not `Editor.WriteMessage`, so the output lands in the log.
- Get the drawing with `AcApp.DocumentManager.MdiActiveDocument`
  (`using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;`) and Civil 3D
  data with `CivilApplication.ActiveDocument`.
- Do all reads and writes in one `Transaction`. Open objects `ForRead` and call
  `UpgradeOpen()` only on the ones you change. One transaction means one U undoes it all.
- Before changing many objects, say how many and ask (`Ask.YesNo`). Skip objects that
  can't be edited instead of stopping, count them, and report both numbers at the end.
  Objects on a locked layer throw `Autodesk.AutoCAD.Runtime.Exception` with
  `ErrorStatus.OnLockedLayer` from `UpgradeOpen()`; checked-in project points throw
  `InvalidOperationException`. `UppercaseDescriptions.cs` shows both.
- When a command shouldn't touch the whole drawing, let them pick (`Ask.PickOne<T>`,
  `Ask.PickMany<T>`).
- `Surface`, `Entity`, `DBObject`, `Section`, `Shape`, `Table`, `Graph` and `PointCloud`
  exist in both the AutoCAD and Civil 3D namespaces. Use an alias when a file needs both.
- Look up anything you aren't sure of in `docs/recipes.md`, then `api-docs/<year>/`
  (`AeccDbMgd.xml` for Civil 3D, `AcDbMgd.xml`, `AcCoreMgd.xml` and `AcMgd.xml` for
  AutoCAD). Member names look like `P:Autodesk.Civil.DatabaseServices.CogoPoint.RawDescription`.
  Never guess an API member. If there's no way to do it, say so and offer options.
- Don't add `IExtensionApplication` or `[assembly: CommandClass]` to MyTools, and don't
  touch `Helpers/LoaderMarker.cs`. They break the reload.
- Each run creates a new instance of the command's class. Anything that should last
  between runs goes in a static field, and static fields reset when a new build loads.
- `Assembly.Location` is empty while the loader runs a command, so don't use it to
  find files.
- Prefer CSV for spreadsheets. Ask before adding a NuGet package.
- Never save, close or open drawings, delete files, or reach the network from a command
  unless the request says to.

## Git

If the folder isn't a git repo yet, offer to make one so every working command is a save
point. After the user says a command works, commit its files with a short message about
what it does. Don't commit `build/`, `logs/`, `api-docs/` or drawings.

## Sharing with coworkers

`scripts\build.cmd -c Release` builds `build\Share\MyTools-Shared.bundle`. A coworker
copies that folder into `%APPDATA%\Autodesk\ApplicationPlugins` and restarts Civil 3D;
it loads every time after that (they'll get a security prompt the first time: Always
Load). They need the same Civil 3D year and nothing else. Don't install it on a computer
that also runs this repo's setup, or the commands get registered twice. To update them,
send a new copy.
