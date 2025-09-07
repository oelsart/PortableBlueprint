using RimWorld;
using UnityEngine;
using Verse;

namespace PortableBlueprint;

public struct BuildingLayout : IExposable
{
    public ThingDef def;

    public ThingDef stuff;

    public IntVec3 pos;

    public Rot4 rot;

    public ThingStyleDef style;

    public Precept_ThingStyle precept;

    public IntVec3 PositionForRot(Rot4 globalRot)
    {
        var position = pos.RotatedBy(globalRot);
        if (!def.rotatable)
        {
            if (globalRot == Rot4.West || globalRot == Rot4.South) position.x -= (def.Size.x - 1) % 2;
            if (globalRot == Rot4.South || globalRot == Rot4.East) position.z -= (def.Size.z - 1) % 2;
        }

        if (Event.current.shift)
        {
            position.x = -position.x;
            if (!def.rotatable)
            {
                position.x -= (def.Size.x - 1) % 2;
            }
            else
            {
                position.x += (((globalRot.AsInt + rot.AsInt) % 4) - 1) % 2 * (def.Size.x - 1) % 2;
                position.z -= (((globalRot.AsInt + 1 + rot.AsInt) % 4) - 1) % 2 * (def.Size.x - 1) % 2;
            }
        }
        return position;
    }

    public Rot4 RotatedRot(Rot4 globalRot)
    {
        var rot2 = def.rotatable ? new Rot4(rot.AsInt + globalRot.AsInt) : rot;
        if (Event.current.shift && rot2.IsHorizontal && def.rotatable)
        {
            return rot2.Opposite;
        }
        return rot2;
    }

    public BuildingLayout() { }

    public BuildingLayout(ThingDef def, ThingDef stuff, IntVec3 pos, Rot4 rot, ThingStyleDef style, Precept_ThingStyle precept)
    {
        this.def = def;
        this.stuff = stuff;
        this.pos = pos;
        this.rot = rot;
        this.style = style;
        this.precept = precept;
    }

    public void ExposeData()
    {

        Scribe_Defs.Look(ref def, "def");
        Scribe_Defs.Look(ref stuff, "stuff");
        Scribe_Values.Look(ref pos, "position");
        Scribe_Values.Look(ref rot, "rotation");
        Scribe_Defs.Look(ref style, "style");
        Scribe_Deep.Look(ref precept, "precept");
    }
}
