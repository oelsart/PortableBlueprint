using RimWorld;
using Verse;

namespace PortableBlueprint
{
    [DefOf]
    public static class PB_DefOf
    {
        static PB_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(PB_DefOf));
        }

        public static ThingDef PB_TentWall;

        public static ThingDef PB_TentPole;

        public static ThingDef PB_TentFloor;

        public static ThingDef PB_Blueprint;

        public static StuffAppearanceDef Fabric;

        public static StuffAppearanceDef Leathery;

        public static RoofDef PB_TentRoof;

        public static JobDef PB_BuildTentRoof;

        public static JobDef PB_RemoveTentRoof;

        public static RecipeDef PB_DrawBlueprint;
    }
}
