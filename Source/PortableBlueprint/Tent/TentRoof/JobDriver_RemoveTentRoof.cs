using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace PortableBlueprint.Tent
{
    public class JobDriver_RemoveTentRoof : JobDriver_AffectRoof
    {
        protected override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !base.Map.areaManager.Get<Area_NoTentRoof>()[base.Cell]);
            foreach (Toil toil in base.MakeNewToils())
            {
                yield return toil;
            }
        }

        protected override void DoEffect()
        {
            var tentPole = TentRoofUtility.GetFirstRoofHolder(base.Cell, base.Map, false);
            JobDriver_RemoveTentRoof.removedRoofs.Clear();
            RoofDef roof = null;
            tentPole?.GetComp<TentPoleComp>().roofOverTent.TryGetValue(base.Cell, out roof);
            base.Map.roofGrid.SetRoof(base.Cell, roof);
            JobDriver_RemoveTentRoof.removedRoofs.Add(base.Cell);
            JobDriver_RemoveTentRoof.removedRoofs.Clear();

            HashSet<SectionLayer> regeneratedLayers = new HashSet<SectionLayer>();
            for (int i = 0; i < 4; i++)
            {
                IntVec3 intVec = base.Cell + GenAdj.DiagonalDirections[i];
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
            return base.Map.roofGrid.RoofAt(base.Cell) != PB_DefOf.PB_TentRoof;
        }

        private static List<IntVec3> removedRoofs = new List<IntVec3>();
    }
}
