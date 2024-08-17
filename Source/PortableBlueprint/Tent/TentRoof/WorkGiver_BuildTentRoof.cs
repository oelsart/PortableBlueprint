using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace PortableBlueprint.Tent
{
    public class WorkGiver_BuildTentRoof: WorkGiver_BuildRoof
    {
        public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
        {
            return pawn.Map.areaManager.Get<Area_BuildTentRoof>().ActiveCells;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return pawn.Map.areaManager.Get<Area_BuildTentRoof>().TrueCount == 0;
        }

        public override bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            if (!pawn.Map.areaManager.Get<Area_BuildTentRoof>()[c])
            {
                return false;
            }
            if (c.GetRoof(pawn.Map) == PB_DefOf.PB_TentRoof)
            {
                return false;
            }
            if (c.IsForbidden(pawn))
            {
                return false;
            }
            if (!pawn.CanReserve(c, 1, -1, ReservationLayerDefOf.Ceiling, forced))
            {
                return false;
            }
            if (!pawn.CanReach(c, PathEndMode.Touch, pawn.NormalMaxDanger(), false, false, TraverseMode.ByPawn) && this.BuildingToTouchToBeAbleToBuildRoof(c, pawn) == null)
            {
                return false;
            }
            if (!TentRoofUtility.WithinRangeOfRoofHolder(c, pawn.Map, false))
            {
                return false;
            }
            if (!TentRoofUtility.ConnectedToRoofHolder(c, pawn.Map, true))
            {
                return false;
            }
            Thing thing = RoofUtility.FirstBlockingThing(c, pawn.Map);
            return thing == null || RoofUtility.CanHandleBlockingThing(thing, pawn, forced);
        }

        private Building BuildingToTouchToBeAbleToBuildRoof(IntVec3 c, Pawn pawn)
        {
            if (c.Standable(pawn.Map))
            {
                return null;
            }
            Building edifice = c.GetEdifice(pawn.Map);
            if (edifice == null)
            {
                return null;
            }
            if (!pawn.CanReach(edifice, PathEndMode.Touch, pawn.NormalMaxDanger(), false, false, TraverseMode.ByPawn))
            {
                return null;
            }
            return edifice;
        }
            
        public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            LocalTargetInfo targetB = c;
            Thing thing = RoofUtility.FirstBlockingThing(c, pawn.Map);
            if (thing != null)
            {
                return RoofUtility.HandleBlockingThingJob(thing, pawn, forced);
            }
            if (!pawn.CanReach(c, PathEndMode.Touch, pawn.NormalMaxDanger(), false, false, TraverseMode.ByPawn))
            {
                targetB = this.BuildingToTouchToBeAbleToBuildRoof(c, pawn);
            }
            return JobMaker.MakeJob(PB_DefOf.PB_BuildTentRoof, c, targetB);
        }
    }
}
