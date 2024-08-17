using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace PortableBlueprint.Tent
{
    public class JobDriver_BuildTentRoof : JobDriver_AffectRoof
    {
        protected override PathEndMode PathEndMode => PathEndMode.Touch;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !base.Map.areaManager.Get<Area_BuildTentRoof>()[base.Cell]);
            this.FailOn(() => !TentRoofUtility.WithinRangeOfRoofHolder(base.Cell, base.Map, false));
            this.FailOn(() => !TentRoofUtility.ConnectedToRoofHolder(base.Cell, base.Map, true));
            foreach (Toil toil in base.MakeNewToils())
            {
                yield return toil;
            }
        }

        protected override void DoEffect()
        {
            for (int i = 0; i < 9; i++)
            {
                IntVec3 intVec = base.Cell + GenAdj.AdjacentCellsAndInside[i];
                if (intVec.InBounds(base.Map) && base.Map.areaManager.Get<Area_BuildTentRoof>()[intVec] && base.Map.roofGrid.RoofAt(intVec) != PB_DefOf.PB_TentRoof && TentRoofUtility.GetFirstRoofHolder(intVec, base.Map, false) != null && RoofUtility.FirstBlockingThing(intVec, base.Map) == null)
                {
                    base.Map.roofGrid.SetRoof(intVec, PB_DefOf.PB_TentRoof);
                    MoteMaker.PlaceTempRoof(intVec, base.Map);
                    List<Thing> list = base.Map.thingGrid.ThingsListAt(intVec);
                    for (int j = 0; j < list.Count; j++)
                    {
                        Thing thing = list[j];
                        CompWakeUpDormant compWakeUpDormant;
                        if (thing.def.building != null && thing.def.building.IsMortar && thing.TryGetComp(out compWakeUpDormant))
                        {
                            compWakeUpDormant.Activate(this.pawn, true, false, false);
                        }
                    }
                }
            }

            HashSet<SectionLayer> regeneratedLayers = new HashSet<SectionLayer>();
            for (int i = 0; i < 4; i++)
            {
                IntVec3 intVec = base.Cell + GenAdj.DiagonalDirections[i] * 2;
                SectionLayer sectionLayer = base.Map.mapDrawer.SectionAt(intVec).GetLayer(typeof(SectionLayer_ThingsGeneral));
                if (!regeneratedLayers.Contains(sectionLayer))
                {
                    sectionLayer.Regenerate();
                    regeneratedLayers.Add(sectionLayer);
                }
            }
        }

        protected override bool DoWorkFailOn()
        {
            return base.Cell.GetRoof(base.Map) == PB_DefOf.PB_TentRoof;
        }
    }
}
