# Recipes

Snippets for common Civil 3D jobs. Every one of them compiles against the official
Autodesk packages for Civil 3D 2024, 2025, 2026 and 2027. They assume the usual
setup inside a command:

```csharp
var doc = AcApp.DocumentManager.MdiActiveDocument;   // AcApp = Autodesk.AutoCAD.ApplicationServices.Application
Database db = doc.Database;
CivilDocument civil = CivilApplication.ActiveDocument;
using (Transaction tr = db.TransactionManager.StartTransaction())
{
    // ...recipe here...
    tr.Commit();
}
```

Name clashes: `Surface`, `Entity`, `DBObject`, `Section`, `Shape`, `Table`, `Graph` and
`PointCloud` exist in both `Autodesk.AutoCAD.DatabaseServices` and
`Autodesk.Civil.DatabaseServices`. If a file uses both namespaces, pick one with an alias:

```csharp
using Surface = Autodesk.Civil.DatabaseServices.Surface;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
```

## Alignments and their profiles

```csharp
foreach (ObjectId alignmentId in civil.GetAlignmentIds())
{
    var alignment = (Alignment)tr.GetObject(alignmentId, OpenMode.ForRead);
    foreach (ObjectId profileId in alignment.GetProfileIds())
    {
        var profile = (Profile)tr.GetObject(profileId, OpenMode.ForRead);
        Out.Line($"{alignment.Name} / {profile.Name}");
    }
}
```

## Station and offset of a point

Throws `PointNotOnEntityException` (namespace `Autodesk.Civil`) past the ends of the alignment.

```csharp
double station = 0, offset = 0;
try
{
    alignment.StationOffset(point.X, point.Y, ref station, ref offset);
    Out.Line($"Station {station:F2}, offset {offset:F2}");
}
catch (PointNotOnEntityException)
{
    Out.Line("That point is beyond the ends of the alignment.");
}
```

## Point at a station and offset

Throws `PointNotOnEntityException` if the station is outside the alignment.

```csharp
double easting = 0, northing = 0;
alignment.PointLocation(station, offset, ref easting, ref northing);
var point = new Point3d(easting, northing, 0);
```

## Profile elevation at a station

```csharp
double elevation = profile.ElevationAt(station);
```

## Surfaces

`FindElevationAtXY` throws `PointNotOnEntityException` outside the surface.

```csharp
foreach (ObjectId id in civil.GetSurfaceIds())
{
    var surface = (Surface)tr.GetObject(id, OpenMode.ForRead);
    Out.Line($"{surface.Name} ({surface.GetType().Name})");
    if (surface is TinSurface tin)
    {
        var props = tin.GetGeneralProperties();
        Out.Line($"  elevation {props.MinimumElevation:F2} to {props.MaximumElevation:F2}");
    }
}

double z = surface.FindElevationAtXY(x, y);
```

## COGO points and point groups

```csharp
foreach (ObjectId id in civil.CogoPoints)
{
    var p = (CogoPoint)tr.GetObject(id, OpenMode.ForRead);
    Out.Line($"{p.PointNumber}: N {p.Northing:F3} E {p.Easting:F3} Z {p.Elevation:F3} {p.RawDescription}");
}

// Add a point. The last argument says whether to apply description keys.
ObjectId newId = civil.CogoPoints.Add(new Point3d(1000, 2000, 10), "CHECK", true);

// Find or create a point group and set what it includes.
ObjectId groupId = civil.PointGroups.Contains("Checks")
    ? civil.PointGroups["Checks"]
    : civil.PointGroups.Add("Checks");
var group = (PointGroup)tr.GetObject(groupId, OpenMode.ForWrite);
group.SetQuery(new StandardPointGroupQuery { IncludeRawDescriptions = "CHECK*" });
group.Update();
```

Checked-in project points throw `InvalidOperationException` when you edit them, and
anything on a locked layer throws `Autodesk.AutoCAD.Runtime.Exception` with
`ErrorStatus.OnLockedLayer` from `UpgradeOpen()`. Catch both, skip and count.

## Pipe networks

`Slope` is a ratio (0.02 means 2%) and always positive, so it doesn't tell you which
way the pipe runs. Compare the start and end points for that.

```csharp
foreach (ObjectId networkId in civil.GetPipeNetworkIds())
{
    var network = (Network)tr.GetObject(networkId, OpenMode.ForRead);
    foreach (ObjectId pipeId in network.GetPipeIds())
    {
        var pipe = (Pipe)tr.GetObject(pipeId, OpenMode.ForRead);
        Out.Line($"{network.Name} / {pipe.Name}: {pipe.PartSizeName}, length {pipe.Length2D:F2}, slope {pipe.Slope * 100:F2}%");
    }
    foreach (ObjectId structureId in network.GetStructureIds())
    {
        var structure = (Structure)tr.GetObject(structureId, OpenMode.ForRead);
        Out.Line($"{structure.Name}: rim {structure.RimElevation:F2}, sump {structure.SumpElevation:F2}");
    }
}
```

## Apply a style by name

Every kind of style has its own collection under `civil.Styles` (`SurfaceStyles`,
`ProfileStyles`, `PipeStyles`, `PointStyles` and so on). Check the name exists first.

```csharp
const string styleName = "Proposed";
if (!civil.Styles.AlignmentStyles.Contains(styleName))
{
    Out.Line($"No alignment style named \"{styleName}\" in this drawing.");
    return;
}
var alignment = (Alignment)tr.GetObject(alignmentId, OpenMode.ForWrite);
alignment.StyleId = civil.Styles.AlignmentStyles[styleName];
```

## Make sure a layer exists, then use it

```csharp
var layers = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
if (!layers.Has("C-ROAD"))
{
    layers.UpgradeOpen();
    var layer = new LayerTableRecord { Name = "C-ROAD" };
    layers.Add(layer);
    tr.AddNewlyCreatedDBObject(layer, true);
}

var entity = (Entity)tr.GetObject(someId, OpenMode.ForWrite);
entity.Layer = "C-ROAD";
```

## Draw lines and text in model space

```csharp
var blocks = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
var modelSpace = (BlockTableRecord)tr.GetObject(blocks[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 0, 0));
modelSpace.AppendEntity(line);
tr.AddNewlyCreatedDBObject(line, true);

var text = new DBText { Position = new Point3d(0, 5, 0), Height = 2.5, TextString = "Hello" };
modelSpace.AppendEntity(text);
tr.AddNewlyCreatedDBObject(text, true);
```

## Ask where to save a CSV, then write it

Plain CSV opens in Excel and needs no extra packages. Quote any field that might
contain a comma.

```csharp
var options = new PromptSaveFileOptions("\nSave CSV as") { Filter = "CSV file (*.csv)|*.csv" };
PromptFileNameResult result = doc.Editor.GetFileNameForSave(options);
if (result.Status != PromptStatus.OK)
    return;
File.WriteAllLines(result.StringResult, rows);
Out.Line($"Saved {result.StringResult}");

static string CsvField(string value) =>
    value.Contains(",") || value.Contains("\"") || value.Contains("\n")
        ? "\"" + value.Replace("\"", "\"\"") + "\""
        : value;
```

## Rebuild every corridor

Rebuilding is slow on big corridors. Make all your changes first, then rebuild once.

```csharp
foreach (ObjectId id in civil.CorridorCollection)
{
    var corridor = (Corridor)tr.GetObject(id, OpenMode.ForWrite);
    corridor.Rebuild();
}
```

## Run a built-in command after yours finishes

The trailing space presses Enter. `_` uses the English command name in any language
version, and `.` uses the built-in command even if someone redefined it.

```csharp
doc.SendStringToExecute("_.REGEN ", true, false, false);
```

## Let the user pick

`Ask` is in `src/MyTools/Helpers/Ask.cs`.

```csharp
ObjectId alignmentId = Ask.PickOne<Alignment>("Select an alignment:");
if (alignmentId.IsNull) return;                         // they pressed Esc

ObjectId[] pipeIds = Ask.PickMany<Pipe>("Select pipes:");   // other objects are ignored
if (!Ask.YesNo($"Change {pipeIds.Length} pipe(s)?")) return;
```
