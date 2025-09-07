using UnityEngine;
using Verse;

namespace PortableBlueprint;

public struct FloorLayout : IExposable
{
    public TerrainDef topDef;

    public TerrainDef foundationDef;

    public IntVec3 pos;

    public readonly IntVec3 PositionForRot(Rot4 globalRot)
    {
        var pos2 = pos.RotatedBy(globalRot);
        if (Event.current.shift)
        {
            pos2.x = -pos2.x;
        }
        return pos2;
    }

    public FloorLayout() { }

    public FloorLayout(TerrainDef topDef, TerrainDef foundationDef, IntVec3 pos)
    {
        this.topDef = topDef;
        this.foundationDef = foundationDef;
        this.pos = pos;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref topDef, "topDef");
        Scribe_Defs.Look(ref foundationDef, "foundationDef");
        Scribe_Values.Look(ref pos, "position");
    }
}
