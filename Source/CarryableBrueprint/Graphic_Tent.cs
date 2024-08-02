using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CarryableBrueprint
{
    public class Graphic_Tent : Graphic_LinkedCornerFiller
    {
        public Graphic_Tent(Graphic subGraphic) : base(subGraphic) { }

        public override bool ShouldLinkWith(IntVec3 c, Thing parent)
        {
            if (!parent.Spawned)
            {
                return false;
            }
            if (!c.InBounds(parent.Map))
            {
                return (parent.def.graphicData.linkFlags & LinkFlags.MapEdge) > LinkFlags.None;
            }
            if ((parent.Map.linkGrid.LinkFlagsAt(c) & parent.def.graphicData.linkFlags) > LinkFlags.None)
            {
                return true;
            }
            var offset = parent.Position - c;
            return (parent.Map.linkGrid.LinkFlagsAt(c + offset.RotatedBy(RotationDirection.Clockwise)) & parent.def.graphicData.linkFlags) > LinkFlags.None ||
                (parent.Map.linkGrid.LinkFlagsAt(c + offset.RotatedBy(RotationDirection.Counterclockwise)) & parent.def.graphicData.linkFlags) > LinkFlags.None;
        }
    }
}
