using Verse;

namespace CarryableBlueprint.Tent
{
    public class PlaceWorker_TentFloor : PlaceWorker
    {
        public override bool ForceAllowPlaceOver(BuildableDef other)
        {
            return !other.IsEdifice() || other.HasModExtension<TentParts>();
        }
    }
}
