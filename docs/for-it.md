# For IT

A short summary for whoever approves software.

## What setup installs

Setup runs as the user. It doesn't need admin rights and doesn't change Program Files,
the registry or PATH.

- **.NET SDK** (Microsoft), only if a suitable one isn't already there. It goes in
  `%LOCALAPPDATA%\Microsoft\dotnet`, downloaded with Microsoft's official
  `dotnet-install.ps1` from `dot.net`. Civil 3D 2024 to 2026 need SDK 8 or newer, 2027
  needs 10. If you install it machine-wide yourself, setup uses yours. Where AppLocker
  or similar blocks programs in `%LOCALAPPDATA%`, a machine-wide install is the way to go.
- **PowerShell**: `Setup.cmd` runs `scripts\setup.ps1` with `-ExecutionPolicy Bypass`.
  An execution policy set by Group Policy still wins; the README has manual steps for
  that case.
- **A Civil 3D plug-in** in `%APPDATA%\Autodesk\ApplicationPlugins\MyTools.bundle`: one
  small unsigned DLL and two text files. Civil 3D shows its usual security prompt the
  first time, because that folder isn't a trusted location by default.
- **Autodesk API packages** from nuget.org (`AutoCAD.NET`, `Civil3D.NET`, published by
  Autodesk) during the first build, cached in `%USERPROFILE%\.nuget\packages`.

Network access during setup: `dot.net`, `aka.ms`, `builds.dotnet.microsoft.com` and
`ci.dot.net` for the SDK, and `api.nuget.org` for the Autodesk packages.

## What the plug-in does

It loads the DLL the user builds in their MyTools folder and registers its commands.
It doesn't connect to the network, run in the background outside Civil 3D, or read
drawings on its own. Commands only run when the user types them.

Two things to know:

- The loader reads `build\Debug\MyTools.dll` from memory, so that DLL doesn't go through
  SECURELOAD or TRUSTEDPATHS. Only the loader does. Anyone who can write to the user's
  MyTools folder can put code into their Civil 3D, the same as any folder a user
  NETLOADs from.
- With `SECURELOAD` set to 2, Civil 3D won't load the bundle at all until you add
  `%APPDATA%\Autodesk\ApplicationPlugins\MyTools.bundle\Contents` to `TRUSTEDPATHS`.

Shared copies (`MyTools-Shared.bundle`) are plain Civil 3D plug-ins: an unsigned
`MyTools.dll` plus a `PackageContents.xml`. They go through SECURELOAD like any other.

## What goes to the AI

Claude Code reads and writes files in the MyTools folder and sends that context to
Anthropic under your company's Claude agreement. That includes the code, what the user
types, and `logs\last-run.txt`, which holds what the last command printed (for example
alignment names or point descriptions). Drawings aren't opened or sent unless the user
does that on purpose.

## Removing it

Delete `%APPDATA%\Autodesk\ApplicationPlugins\MyTools.bundle` (and
`MyTools-Shared.bundle` on coworkers' computers). Optionally delete
`%LOCALAPPDATA%\Microsoft\dotnet` if setup installed it and nothing else uses it.
