using RimWorld;
using Verse;

namespace CarryableBlueprint
{
    public class BuildingLayout : IExposable
    {
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
            
            Scribe_Defs.Look(ref this.def, "def");
            Scribe_Defs.Look(ref this.stuff, "stuff");
            Scribe_Values.Look(ref this.pos, "position");
            Scribe_Values.Look(ref this.rot, "rotation");
            Scribe_Defs.Look(ref this.style, "style");
            Scribe_Deep.Look(ref this.precept, "precept");
        }

        public ThingDef def;

        public ThingDef stuff;

        public IntVec3 pos;

        public Rot4 rot;

        public ThingStyleDef style;

        public Precept_ThingStyle precept;
    }
}
