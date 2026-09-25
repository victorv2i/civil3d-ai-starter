using System;
using System.IO;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MyTools;

// Use Out.Line instead of Editor.WriteMessage. It prints to the Civil 3D command
// line and also saves the text to logs\last-run.txt, so you can show your AI
// assistant what a command did without copying anything.
public static class Out
{
    public static void Line(string text)
    {
        AcApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\n" + text);

        if (AppDomain.CurrentDomain.GetData("MyTools.RunLog") is string logFile)
        {
            try { File.AppendAllText(logFile, text + Environment.NewLine); }
            catch (IOException) { }
        }
    }
}
