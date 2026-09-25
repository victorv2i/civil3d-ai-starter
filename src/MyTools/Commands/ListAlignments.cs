using System.ComponentModel;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MyTools.Commands;

// Example of a read-only command: it looks at the drawing and changes nothing.
public class ListAlignments
{
    [CommandMethod("MYALIGNMENTS")]
    [Description("Lists every alignment with its length and station range.")]
    public void Run()
    {
        Database db = AcApp.DocumentManager.MdiActiveDocument.Database;
        CivilDocument civil = CivilApplication.ActiveDocument;

        ObjectIdCollection ids = civil.GetAlignmentIds();
        if (ids.Count == 0)
        {
            Out.Line("No alignments in this drawing.");
            return;
        }

        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
            Out.Line($"{ids.Count} alignment(s):");
            foreach (ObjectId id in ids)
            {
                var alignment = (Alignment)tr.GetObject(id, OpenMode.ForRead);
                Out.Line($"  {alignment.Name}: length {alignment.Length:F2}, " +
                         $"stations {alignment.StartingStation:F2} to {alignment.EndingStation:F2}");
            }
            tr.Commit();
        }
    }
}
