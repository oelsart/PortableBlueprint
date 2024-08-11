using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CarryableBlueprint.Tent
{
    public class WorkGiver_RemoveTentRoof : WorkGiver_RemoveRoof
    {
        public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
        {
            return pawn.Map.areaManager.Get<Area_NoTentRoof>().ActiveCells;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return pawn.Map.areaManager.Get<Area_NoTentRoof>().TrueCount == 0;
        }

        public override bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            return pawn.Map.areaManager.Get<Area_NoTentRoof>()[c] && (c.GetRoof(pawn.Map) == CB_DefOf.CB_TentRoof) && !c.IsForbidden(pawn) && pawn.CanReserve(c, 1, -1, ReservationLayerDefOf.Ceiling, forced);
        }

        public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            return JobMaker.MakeJob(CB_DefOf.CB_RemoveTentRoof, c, c);
        }

        public override float GetPriority(Pawn pawn, TargetInfo t)
        {
            IntVec3 cell = t.Cell;
            int num = 0;
            for (int i = 0; i < 8; i++)
            {
                IntVec3 c = cell + GenAdj.AdjacentCells[i];
                if (c.InBounds(t.Map))
                {
                    if (c.GetEdifice(t.Map)?.HasComp<TentPoleComp>() ?? false)
                    {
                        return -60f;
                    }
                    if (c.GetRoof(pawn.Map) == CB_DefOf.CB_TentRoof)
                    {
                        num++;
                    }
                }
            }
            return (float)(-(float)Mathf.Min(num, 3));
        }
    }
}
