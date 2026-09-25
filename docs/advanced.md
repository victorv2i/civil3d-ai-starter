# Advanced

## Sharing your tools with coworkers

Build a release copy:

```
scripts\build.cmd -c Release
```

That makes `build\Share\MyTools-Shared.bundle`. A coworker copies that whole folder
into `%APPDATA%\Autodesk\ApplicationPlugins` (paste the path into File Explorer's
address bar) and restarts Civil 3D. From then on the tools load every time. The first
time, Civil 3D asks whether to load the unsigned plug-in; they choose Always Load.

They need the same Civil 3D year you built for, and they don't need this repo, Claude
or .NET. The shared copy doesn't reload itself, so send a new folder when you change
something. Don't install it on a computer that also has this repo set up, or every
command gets registered twice.

`build\Release\MyTools.dll` on its own also works with `NETLOAD`, once per session.
The Startup Suite in `APPLOAD` doesn't take .NET DLLs.

## Picking the Civil 3D version

The build uses the newest Civil 3D (2024 to 2027) installed on the computer. To pick
one, put the year in `Directory.Build.props`:

```xml
<Civil3DYear>2025</Civil3DYear>
```

Then run `Setup.cmd` again so Civil 3D gets the matching loader. 2024 builds on
.NET Framework 4.8, 2025 and 2026 on .NET 8, 2027 on .NET 10. Civil 3D 2023 and older
aren't supported: Autodesk doesn't publish their API as NuGet packages.

## How reloading works

Civil 3D can't unload a DLL, so normally every change means closing Civil 3D,
starting it again and running NETLOAD. This repo avoids that:

1. `Setup.cmd` puts a small loader (`src/Loader`) in
   `%APPDATA%\Autodesk\ApplicationPlugins\MyTools.bundle`. Civil 3D loads it at startup.
2. The loader reads `build\Debug\MyTools.dll` into memory, so the file is never locked
   and builds always succeed.
3. It finds every `[CommandMethod]` and registers it under its own command group.
4. While Civil 3D is idle it checks once a second for a newer build. When one lands
   and no command is running, it removes the old commands and registers the new ones.

Debug builds include `[assembly: CommandClass(typeof(LoaderMarker))]`, which stops
Civil 3D from registering the commands a second time when the loader reads the DLL.
Release builds leave it out so NETLOAD works as usual.

Each reload leaves the previous copy in memory. It's small, but restart Civil 3D
once in a while during long sessions.

The loader also wraps every command so an error shows up as a short message instead
of a crash dialog, with the details in `logs\last-run.txt`.

The approach follows what the [DevReload](https://github.com/shtirlitsDva/DevReload)
project worked out and tested in Civil 3D.

## Visual Studio

Open `MyTools.sln`. To stop on breakpoints, start Civil 3D normally, then in Visual
Studio use **Debug > Attach to Process** and pick `acad.exe`. Build with Ctrl+Shift+B;
the loader picks up the new build as usual.

## Extra packages

`CopyLocalLockFileAssemblies` is on, so NuGet packages you add end up next to
`MyTools.dll` and the loader finds them. Stick to CSV when you can; it needs nothing.

## Removing it

```
Setup.cmd -Remove
```

or delete `%APPDATA%\Autodesk\ApplicationPlugins\MyTools.bundle`. Your repo folder stays
as it is.
