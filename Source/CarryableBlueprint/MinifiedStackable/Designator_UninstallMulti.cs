using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CarryableBlueprint
{
    public class Designator_UninstallMulti : Designator_Uninstall
    {
        public override int DraggableDimensions => 2;

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            foreach (var cell in cells)
            {
                foreach(var thing in cell.GetThingList(base.Map).Where(t => this.CanDesignateThing(t).Accepted).ToArray())
                {
                    base.DesignateThing(thing);
                }
            }
        }
    }
}
