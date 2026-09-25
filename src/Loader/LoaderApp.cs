using System.ComponentModel;
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(MyTools.Loader.LoaderApp))]
[assembly: CommandClass(typeof(MyTools.Loader.LoaderCommands))]

namespace MyTools.Loader;

public class LoaderApp : IExtensionApplication
{
    public void Initialize() => ToolsHost.Start();

    public void Terminate() { }
}

public class LoaderCommands
{
    [CommandMethod("MYTOOLS")]
    [Description("Lists your tools.")]
    public void ListTools() => ToolsHost.PrintList();

    [CommandMethod("MYRELOAD")]
    [Description("Loads the latest build right now.")]
    public void Reload() => ToolsHost.Reload(force: true);
}
