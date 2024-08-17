using Verse;

namespace PortableBlueprint.Tent
{
    public class PlaceWorker_TentFloor : PlaceWorker
    {
        public override bool ForceAllowPlaceOver(BuildableDef other)
        {
            return other.HasModExtension<TentParts>() || (other is ThingDef frame && (frame.entityDefToBuild?.HasModExtension<TentParts>() ?? false));
        }
    }
}
