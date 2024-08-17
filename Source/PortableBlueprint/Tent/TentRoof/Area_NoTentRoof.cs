using UnityEngine;
using Verse;

namespace PortableBlueprint
{
    public class Area_NoTentRoof : Area
    {
        public override string Label => "PB.NoTentRoof".Translate();

        public override Color Color => new Color(0.859f, 0.369f, 0.8f);

        public override int ListPriority => 9100;

        public Area_NoTentRoof() { }

        public Area_NoTentRoof(AreaManager areaManager) : base(areaManager) { }

        public override string GetUniqueLoadID()
        {
            return "Area_" + this.ID + "_NoTentRoof";
        }
    }
}
