using UnityEngine;
using Verse;

namespace CarryableBlueprint
{
    public class Area_BuildTentRoof : Area
    {
        public override string Label => "CB.BuildTentRoof".Translate();

        public override Color Color => new Color(0.235f, 0.784f, 0.627f);

        public override int ListPriority => 9100;

        public Area_BuildTentRoof() { }

        public Area_BuildTentRoof(AreaManager areaManager) : base(areaManager) { }

        public override string GetUniqueLoadID()
        {
            return "Area_" + this.ID + "_BuildTentRoof";
        }
    }
}
