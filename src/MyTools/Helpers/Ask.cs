using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MyTools;

// Short ways to ask the person running a command for something.
public static class Ask
{
    static Editor Ed => AcApp.DocumentManager.MdiActiveDocument.Editor;

    // Yes/No question. Pressing Enter picks the default.
    public static bool YesNo(string question, bool defaultYes = false)
    {
        string byDefault = defaultYes ? "Yes" : "No";
        var options = new PromptKeywordOptions($"\n{question} [Yes/No] <{byDefault}>: ", "Yes No");
        options.Keywords.Default = byDefault;
        PromptResult result = Ed.GetKeywords(options);
        return result.Status == PromptStatus.OK && result.StringResult == "Yes";
    }

    // Click one object of type T (for example Alignment or TinSurface).
    // Returns ObjectId.Null if they press Esc.
    public static ObjectId PickOne<T>(string message) where T : Entity
    {
        var options = new PromptEntityOptions("\n" + message);
        options.SetRejectMessage($"\nThat isn't a {typeof(T).Name}. Try again.");
        options.AddAllowedClass(typeof(T), false);
        PromptEntityResult result = Ed.GetEntity(options);
        return result.Status == PromptStatus.OK ? result.ObjectId : ObjectId.Null;
    }

    // Select any number of objects; keeps only the ones of type T.
    // Returns an empty array if they press Esc or pick nothing useful.
    public static ObjectId[] PickMany<T>(string message) where T : Entity
    {
        var options = new PromptSelectionOptions { MessageForAdding = "\n" + message };
        PromptSelectionResult result = Ed.GetSelection(options);
        if (result.Status != PromptStatus.OK)
            return new ObjectId[0];

        RXClass wanted = RXObject.GetClass(typeof(T));
        return result.Value.GetObjectIds()
            .Where(id => id.ObjectClass.IsDerivedFrom(wanted))
            .ToArray();
    }
}
