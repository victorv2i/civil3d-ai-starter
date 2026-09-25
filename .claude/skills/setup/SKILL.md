---
name: setup
description: Use when the user asks to set up this repo, says MyTools isn't showing up in Civil 3D, or the build can't find .NET.
---

# Set up or repair MyTools

1. Run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/setup.ps1 -Yes`.
   It finds Civil 3D, installs the .NET SDK in the user's folder if needed (no admin),
   builds, and installs the loader into Civil 3D's plug-in folder.

2. If it stops, the last line says why. Common ones:
   - Download failed: the company network may block it. Ask them to get IT to install
     the .NET SDK version it names, then run setup again.
   - "Civil 3D is using the old loader": ask them to close Civil 3D, then run it again.
   - Build failed: fix the errors, then run it again.
   - "running scripts is disabled on this system": company policy blocks PowerShell
     scripts. Don't try to get around it. Walk them through the manual steps in the
     README's "If something goes wrong" section.

3. When it finishes, tell them the three "Next" steps it printed, in your own words.

If MyTools still doesn't appear in Civil 3D after a restart:
- Check `%APPDATA%\Autodesk\ApplicationPlugins\MyTools.bundle\PackageContents.xml`
  names the right release (`R24.3` = 2024, `R25.0` = 2025, `R25.1` = 2026, `R26.0` = 2027).
- In Civil 3D, `APPAUTOLOAD` should include startup loading (the default is 14).
- If a security prompt appeared and they chose "Do Not Load", have them restart
  Civil 3D and choose "Always Load" this time.
