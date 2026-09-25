# Civil 3D AI Starter

Describe a Civil 3D command in plain English. Claude writes it in C#, builds it, and
a few seconds later you can run it in Civil 3D. No programming needed, and no
restarting Civil 3D between tries.

You need Windows, Civil 3D 2024 to 2027, and Claude Code (the **Code** tab in the
Claude desktop app; company Team and Enterprise plans include it).

## Easiest: let your AI set it up

Open Claude Code and paste this:

```
Set up https://github.com/victorv2i/civil3d-ai-starter on this computer. Put it in my user folder, run its setup, and tell me what to do next.
```

You can also give it the downloaded ZIP instead of the link. Other AI coding
assistants that can run commands work the same way.

## Or set it up yourself

1. Click **Code > Download ZIP**. Right-click the ZIP, choose **Properties**, tick
   **Unblock**, click OK. Then **Extract All** into your user folder (`C:\Users\<you>`).
2. Open the extracted folder and double-click **Setup.cmd**. Press Enter for yes.
3. Open Civil 3D. If it asks about MyTools, choose **Always Load**. Type `MYTOOLS`.
4. In Claude, go to **Code** and open the folder that has `Setup.cmd` in it.

## Use it

1. Tell Claude what you want, for example: *"Make a command that lists every pipe
   with its size, length and slope, and saves it as a CSV."*
2. Claude writes and builds it. A few seconds later Civil 3D shows
   **MyTools: N command(s) ready**.
3. Type the command name in Civil 3D. Try it on a copy of a drawing first.
4. Not right? Tell Claude what happened; it reads the log. Right? Say "save this".

Say which objects (all, ones you pick, ones on a layer), what should change, and
what to do with odd cases like locked points or missing data. One change at a time.
If you have scripts that already work, put them in `samples/` and Claude will match them.

To give your tools to coworkers, ask Claude for "a copy to share". They copy one
folder and restart Civil 3D. They don't need Claude or any setup.

## Tools you start with

- `MYTOOLS` lists your tools. `MYRELOAD` loads the newest build right away.
- `MYALIGNMENTS` lists alignments with their lengths and stations.
- `MYUPPERDESC` makes COGO point descriptions uppercase. It asks first; `U` undoes it.
- `MYELEVATION` reads a surface's elevation wherever you click.

## If something goes wrong

- **MyTools doesn't show up.** Run `Setup.cmd` again, then restart Civil 3D.
- **Setup can't download .NET.** Ask IT to install the .NET SDK (8 for Civil 3D 2024
  to 2026, 10 for 2027), then run setup again.
- **"Running scripts is disabled on this system."** Your company blocks PowerShell
  scripts. Ask IT for the .NET SDK, double-click `scripts\build.cmd`, then copy the
  `build\MyTools.bundle` folder into `%APPDATA%\Autodesk\ApplicationPlugins`.
- **"Unknown command."** Wait for the "ready" message, or type `MYRELOAD`.
- **Civil 3D gets slow in a long session.** Restart it now and then.

To remove it, delete `%APPDATA%\Autodesk\ApplicationPlugins\MyTools.bundle` (paste
that path into File Explorer's address bar).

## More

- [docs/recipes.md](docs/recipes.md): code for common Civil 3D jobs
- [docs/advanced.md](docs/advanced.md): sharing, picking a Civil 3D version, how reloading works
- [docs/for-it.md](docs/for-it.md): what gets installed and what data goes where

## For AI assistants setting this up

1. Put the repo in the user's profile folder: `git clone
   https://github.com/victorv2i/civil3d-ai-starter` if git is installed, otherwise
   download and extract
   `https://github.com/victorv2i/civil3d-ai-starter/archive/refs/heads/main.zip`
   (it contains a `civil3d-ai-starter-main` folder).
2. Tell the user setup may install Microsoft's .NET SDK in their user folder. Then,
   from the repo folder, run
   `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/setup.ps1 -Yes`.
3. If setup stops, pass on what it says. Don't work around company policy.
4. Then tell the user to open Civil 3D (choose **Always Load**, type `MYTOOLS`) and to
   open the repo folder in Claude Code, so the rules in `AGENTS.md` apply.

MIT license. Not affiliated with Autodesk or Anthropic.
