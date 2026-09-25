using System.ComponentModel;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil;
using Autodesk.Civil.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
// Surface exists in both AutoCAD and Civil 3D; this file means the Civil 3D one.
using Surface = Autodesk.Civil.DatabaseServices.Surface;

namespace MyTools.Commands;

// Example of an interactive command: pick a surface, then click points and
// get the surface elevation at each one until you press Enter or Esc.
public class SurfaceElevation
{
    [CommandMethod("MYELEVATION")]
    [Description("Pick a surface, then click points to read its elevation. Enter to finish.")]
    public void Run()
    {
        var doc = AcApp.DocumentManager.MdiActiveDocument;

        ObjectId surfaceId = Ask.PickOne<Surface>("Select a surface:");
        if (surfaceId.IsNull)
            return;

        using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
        {
            var surface = (Surface)tr.GetObject(surfaceId, OpenMode.ForRead);
            Out.Line($"Surface: {surface.Name}");

            while (true)
            {
                var pick = new PromptPointOptions("\nClick a point (Enter to finish):") { AllowNone = true };
                PromptPointResult result = doc.Editor.GetPoint(pick);
                if (result.Status != PromptStatus.OK)
                    break;

                try
                {
                    double z = surface.FindElevationAtXY(result.Value.X, result.Value.Y);
                    Out.Line($"  E {result.Value.X:F2}, N {result.Value.Y:F2}: elevation {z:F2}");
                }
                catch (PointNotOnEntityException)
                {
                    Out.Line("  That point is outside the surface.");
                }
            }
            tr.Commit();
        }
    }
}
