using RimWorld;
using Verse;

namespace PortableBlueprint.Tent
{
    public class PlaceWorker_TentFloor : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            Thing thing2 = map.thingGrid.ThingAt(loc, ThingDefOf.SteamGeyser);
            if (thing2 != null)
            {
                return "SpaceAlreadyOccupied".Translate();
            }
            return true;
        }

        public override bool ForceAllowPlaceOver(BuildableDef other)
        {
            return (other is ThingDef thingDef) && thingDef.CoexistsWithFloors && !thingDef.holdsRoof;
        }
    }
}
