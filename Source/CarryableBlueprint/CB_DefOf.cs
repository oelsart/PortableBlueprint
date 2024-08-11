using RimWorld;
using Verse;

namespace CarryableBlueprint
{
    [DefOf]
    public static class CB_DefOf
    {
        static CB_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CB_DefOf));
        }

        public static ThingDef CB_TentWall;

        public static ThingDef CB_TentPole;

        public static ThingDef CB_Blueprint;

        public static StuffAppearanceDef Fabric;

        public static StuffAppearanceDef Leathery;

        public static RoofDef CB_TentRoof;

        public static JobDef CB_BuildTentRoof;

        public static JobDef CB_RemoveTentRoof;

        public static RecipeDef CB_DrawBlueprint;
    }
}
