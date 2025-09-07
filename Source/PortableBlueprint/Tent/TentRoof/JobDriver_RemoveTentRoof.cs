using PortableBlueprint.Tent.TentRoof;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace PortableBlueprint.Tent;

public class JobDriver_RemoveTentRoof : JobDriver_AffectRoof
{
    private static readonly List<IntVec3> removedRoofs = [];

    protected override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() => !Map.areaManager.Get<Area_NoTentRoof>()[Cell]);
        foreach (Toil toil in base.MakeNewToils())
        {
            yield return toil;
        }
    }

    protected override void DoEffect()
    {
        removedRoofs.Clear();

        var tempRoofGrid = Map.GetComponent<TempRoofGrid>();
        Map.roofGrid.SetRoof(Cell, tempRoofGrid.RoofAt(Cell));
        tempRoofGrid.SetRoof(Cell, null);
        removedRoofs.Add(Cell);
        removedRoofs.Clear();

        HashSet<SectionLayer> regeneratedLayers = [];
        for (int i = 0; i < 4; i++)
        {
            IntVec3 intVec = Cell + GenAdj.DiagonalDirections[i];
            SectionLayer sectionLayer = Map.mapDrawer.SectionAt(intVec).GetLayer(typeof(SectionLayer_ThingsGeneral));
            if (!regeneratedLayers.Contains(sectionLayer))
            {
                sectionLayer.Regenerate();
                regeneratedLayers.Add(sectionLayer);
            }
        }
    }

    protected override bool DoWorkFailOn()
    {
        return Map.roofGrid.RoofAt(Cell) != PB_DefOf.PB_TentRoof;
    }
}
