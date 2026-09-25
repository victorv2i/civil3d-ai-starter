using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
#if !NETFRAMEWORK
using System.Runtime.Loader;
#endif

namespace MyTools.Loader;

// Loads build\Debug\MyTools.dll from memory, so the file is never locked and you
// can rebuild while Civil 3D is open. It registers the [CommandMethod]s it finds
// and swaps them for the new ones whenever a newer build appears.
//
// Civil 3D can't unload a DLL, so each reload adds a small copy to memory.
// Restart Civil 3D now and then during a long session.
internal static class ToolsHost
{
    const string CommandGroup = "MYTOOLS_COMMANDS";
    static readonly string[] LoaderCommandNames = { "MYTOOLS", "MYRELOAD" };

    static string repoRoot;
    static string dllPath;
    static string logPath;
    static DateTime loadedBuildTime;
    static readonly List<string> waitingMessages = new List<string>();
    static readonly List<Tool> tools = new List<Tool>();
    static readonly Stopwatch sinceLastCheck = Stopwatch.StartNew();

    sealed class Tool
    {
        public string Name;
        public string Description;
        public CommandFlags Flags;
        public Type Type;
        public MethodInfo Method;
        public CommandCallback Callback; // kept here so the garbage collector leaves it alone
    }

    public static void Start()
    {
        AcApp.Idle += OnIdle;

        string contents = Path.GetDirectoryName(typeof(ToolsHost).Assembly.Location);
        string pathFile = Path.Combine(contents, "repo-path.txt");
        if (!File.Exists(pathFile))
        {
            Say("MyTools: run Setup.cmd in your MyTools folder.");
            return;
        }

        repoRoot = File.ReadAllText(pathFile).Trim();
        dllPath = Path.Combine(repoRoot, "build", "Debug", "MyTools.dll");
        logPath = Path.Combine(repoRoot, "logs", "last-run.txt");

#if NETFRAMEWORK
        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromBuildFolder;
#endif
        Reload(force: true);
    }

    public static void Reload(bool force)
    {
        if (dllPath == null)
        {
            Say("MyTools: run Setup.cmd in your MyTools folder.");
            return;
        }
        if (!File.Exists(dllPath))
        {
            Say("MyTools: there's no build yet. Build the project, then type MYRELOAD.");
            return;
        }

        DateTime buildTime = File.GetLastWriteTimeUtc(dllPath);
        if (!force && buildTime == loadedBuildTime)
            return;
        loadedBuildTime = buildTime;

        List<Tool> found;
        try
        {
            found = FindTools(LoadFromBytes(dllPath));
        }
        catch (System.Exception ex)
        {
            Say($"MyTools: couldn't load the latest build ({ex.Message}). Type MYRELOAD to try again.");
            return;
        }

        foreach (Tool old in tools)
        {
            try { Utils.RemoveCommand(CommandGroup, old.Name); }
            catch (System.Exception) { }
        }
        tools.Clear();

        foreach (Tool tool in found)
        {
            if (LoaderCommandNames.Contains(tool.Name))
            {
                Say($"MyTools: skipped {tool.Name}, the loader already uses that name.");
                continue;
            }
            Tool current = tool;
            current.Callback = () => Run(current);
            try
            {
                Utils.AddCommand(CommandGroup, current.Name, current.Name, current.Flags, current.Callback);
                tools.Add(current);
            }
            catch (System.Exception ex)
            {
                Say($"MyTools: couldn't add {current.Name} ({ex.Message}). Another plug-in may use that name.");
            }
        }

        Say($"MyTools: {tools.Count} command(s) ready from the build at " +
            $"{buildTime.ToLocalTime():HH:mm:ss}. Type MYTOOLS to list them.");
    }

    public static void PrintList()
    {
        if (tools.Count == 0)
        {
            Say(dllPath == null
                ? "MyTools: nothing loaded. Run Setup.cmd in your MyTools folder."
                : "MyTools: nothing loaded yet. Build the project, then type MYRELOAD.");
            return;
        }

        Say($"Your tools (build from {loadedBuildTime.ToLocalTime():MMM d, HH:mm}):");
        foreach (Tool tool in tools)
            Say($"  {tool.Name,-22}{tool.Description}");
        Say("New builds load on their own. MYRELOAD forces it.");
        Say("Folder: " + repoRoot);
    }

    // Checks about once a second, while Civil 3D is idle, for a newer build.
    static void OnIdle(object sender, EventArgs e)
    {
        Document doc = AcApp.DocumentManager.MdiActiveDocument;
        if (doc != null && waitingMessages.Count > 0)
        {
            foreach (string message in waitingMessages)
                doc.Editor.WriteMessage("\n" + message);
            doc.Editor.WriteMessage("\n");
            waitingMessages.Clear();
        }

        if (sinceLastCheck.ElapsedMilliseconds < 1000)
            return;
        sinceLastCheck.Restart();

        try
        {
            if (dllPath == null || !File.Exists(dllPath))
                return;
            DateTime buildTime = File.GetLastWriteTimeUtc(dllPath);
            if (buildTime == loadedBuildTime)
                return;

            // Give the build a moment to finish writing its files.
            TimeSpan age = DateTime.UtcNow - buildTime;
            if (age >= TimeSpan.Zero && age < TimeSpan.FromSeconds(2))
                return;

            if (doc != null && !string.IsNullOrEmpty(doc.CommandInProgress))
                return;

            Reload(force: false);
        }
        catch (System.Exception ex)
        {
            Say("MyTools: " + ex.Message);
        }
    }

    static void Run(Tool tool)
    {
        var timer = Stopwatch.StartNew();
        StartLog(tool.Name);
        try
        {
            object instance = tool.Method.IsStatic ? null : Activator.CreateInstance(tool.Type);
            tool.Method.Invoke(instance, null);
            EndLog($"Finished OK in {timer.Elapsed.TotalSeconds:F1} s.");
        }
        catch (System.Exception ex)
        {
            System.Exception error = ex is TargetInvocationException && ex.InnerException != null
                ? ex.InnerException
                : ex;
            EndLog("FAILED" + Environment.NewLine + error);
            Say($"{tool.Name} stopped with an error: {error.Message}");
            Say($"Details are in {logPath}");
        }
    }

    static List<Tool> FindTools(Assembly assembly)
    {
        Type[] types;
        try { types = assembly.GetExportedTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }

        var found = new Dictionary<string, Tool>(StringComparer.OrdinalIgnoreCase);
        foreach (Type type in types)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                foreach (CommandMethodAttribute attr in method.GetCustomAttributes(typeof(CommandMethodAttribute), false))
                {
                    string name = attr.GlobalName.ToUpperInvariant();
                    if (found.ContainsKey(name))
                    {
                        Say($"MyTools: {name} is defined twice. Using the first one.");
                        continue;
                    }
                    var description = (DescriptionAttribute)method
                        .GetCustomAttributes(typeof(DescriptionAttribute), false)
                        .FirstOrDefault();
                    found[name] = new Tool
                    {
                        Name = name,
                        Description = description?.Description ?? "",
                        Flags = attr.Flags,
                        Type = type,
                        Method = method,
                    };
                }
            }
        }
        return found.Values.OrderBy(t => t.Name).ToList();
    }

    static Assembly LoadFromBytes(string path)
    {
        byte[] dll = File.ReadAllBytes(path);
        string pdbPath = Path.ChangeExtension(path, ".pdb");
        byte[] pdb = File.Exists(pdbPath) ? File.ReadAllBytes(pdbPath) : null;

#if NETFRAMEWORK
        return pdb == null ? Assembly.Load(dll) : Assembly.Load(dll, pdb);
#else
        var context = new BuildFolderContext(Path.GetDirectoryName(path));
        using var dllStream = new MemoryStream(dll);
        if (pdb == null)
            return context.LoadFromStream(dllStream);
        using var pdbStream = new MemoryStream(pdb);
        return context.LoadFromStream(dllStream, pdbStream);
#endif
    }

    static void StartLog(string command)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            File.WriteAllText(logPath,
                $"{command}  run {DateTime.Now:yyyy-MM-dd HH:mm:ss}, build {loadedBuildTime.ToLocalTime():HH:mm:ss}" +
                Environment.NewLine + Environment.NewLine);
            AppDomain.CurrentDomain.SetData("MyTools.RunLog", logPath);
        }
        catch (System.Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            AppDomain.CurrentDomain.SetData("MyTools.RunLog", null);
        }
    }

    static void EndLog(string result)
    {
        try { File.AppendAllText(logPath, Environment.NewLine + result + Environment.NewLine); }
        catch (System.Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { }
    }

    static void Say(string text)
    {
        Document doc = AcApp.DocumentManager.MdiActiveDocument;
        if (doc != null)
            doc.Editor.WriteMessage("\n" + text);
        else
            waitingMessages.Add(text); // shown once a drawing is open
    }

#if NETFRAMEWORK
    static readonly Dictionary<string, Assembly> resolved = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

    // Extra packages your commands use are loaded from the build folder.
    static Assembly ResolveFromBuildFolder(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name;
        if (resolved.TryGetValue(name, out Assembly already))
            return already;

        string path = Path.Combine(Path.GetDirectoryName(dllPath), name + ".dll");
        if (!File.Exists(path) || path.Equals(dllPath, StringComparison.OrdinalIgnoreCase))
            return null;

        Assembly assembly = Assembly.Load(File.ReadAllBytes(path));
        resolved[name] = assembly;
        return assembly;
    }
#else
    // Extra packages your commands use are loaded from the build folder.
    // Autodesk and .NET assemblies aren't there, so they come from Civil 3D.
    sealed class BuildFolderContext : AssemblyLoadContext
    {
        readonly string folder;

        public BuildFolderContext(string folder) : base("MyTools " + DateTime.Now.ToString("HH:mm:ss"))
        {
            this.folder = folder;
        }

        protected override Assembly Load(AssemblyName name)
        {
            string path = Path.Combine(folder, name.Name + ".dll");
            if (!File.Exists(path))
                return null;
            using var stream = new MemoryStream(File.ReadAllBytes(path));
            return LoadFromStream(stream);
        }
    }
#endif
}
