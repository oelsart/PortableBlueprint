using Verse;

namespace CarryableBlueprint
{
    public class FloorLayout : IExposable
    {
        public FloorLayout() { }

        public FloorLayout(TerrainDef def, IntVec3 pos)
        {
            this.def = def;
            this.pos = pos;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref pos, "position");
        }

        public TerrainDef def;

        public IntVec3 pos;
    }
}
