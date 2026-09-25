// Leave this file alone.
//
// In Debug builds the MyTools loader registers your commands itself so it can
// swap in a new build without restarting Civil 3D. This attribute tells Civil 3D
// not to register them a second time. Release builds (the ones you share with
// coworkers) don't have it, so they work with a plain NETLOAD.
#if DEBUG
[assembly: Autodesk.AutoCAD.Runtime.CommandClass(typeof(MyTools.LoaderMarker))]

namespace MyTools
{
    public class LoaderMarker
    {
    }
}
#endif
