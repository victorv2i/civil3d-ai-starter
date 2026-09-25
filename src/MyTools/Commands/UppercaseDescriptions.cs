using System;
using System.ComponentModel;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MyTools.Commands;

// Example of changing every object of one kind. It asks first, makes all the
// changes in one transaction (so a single U undoes it) and reports what it did.
public class UppercaseDescriptions
{
    [CommandMethod("MYUPPERDESC")]
    [Description("Makes every COGO point description UPPERCASE. Asks first; U undoes it.")]
    public void Run()
    {
        Database db = AcApp.DocumentManager.MdiActiveDocument.Database;
        CivilDocument civil = CivilApplication.ActiveDocument;

        uint total = civil.CogoPoints.Count;
        if (total == 0)
        {
            Out.Line("No COGO points in this drawing.");
            return;
        }

        if (!Ask.YesNo($"Uppercase the descriptions on {total} point(s)?"))
        {
            Out.Line("Cancelled. Nothing changed.");
            return;
        }

        int changed = 0;
        int locked = 0;
        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
            foreach (ObjectId id in civil.CogoPoints)
            {
                var point = (CogoPoint)tr.GetObject(id, OpenMode.ForRead);
                string current = point.RawDescription ?? "";
                string upper = current.ToUpperInvariant();
                if (upper == current)
                    continue;

                try
                {
                    point.UpgradeOpen();
                    point.RawDescription = upper;
                    changed++;
                }
                catch (InvalidOperationException)
                {
                    // Project points that are checked in can't be edited.
                    locked++;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == ErrorStatus.OnLockedLayer)
                {
                    locked++;
                }
            }
            tr.Commit();
        }

        Out.Line($"Updated {changed} of {total} point(s).");
        if (locked > 0)
            Out.Line($"Skipped {locked} locked point(s) (locked layer or checked-in project point).");
    }
}
