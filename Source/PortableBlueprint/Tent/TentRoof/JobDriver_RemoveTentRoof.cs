using PortableBlueprint.Tent.TentRoof;
using RimWorld;
using Verse;
using Verse.AI;

namespace PortableBlueprint.Tent;

public class JobDriver_RemoveTentRoof : JobDriver_AffectRoof
{
	protected override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOn(() => !Map.areaManager.Get<Area_NoTentRoof>()[Cell]);
		foreach (var toil in base.MakeNewToils())
		{
			yield return toil;
		}
	}

	protected override void DoEffect()
	{
		var tempRoofGrid = Map.GetComponent<TempRoofGrid>();
		Map.roofGrid.SetRoof(Cell, tempRoofGrid.RoofAt(Cell));
		tempRoofGrid.SetRoof(Cell, null);

		HashSet<SectionLayer> regeneratedLayers = [];
		for (var i = 0; i < 4; i++)
		{
			var intVec = Cell + GenAdj.DiagonalDirections[i];
			var sectionLayer = Map.mapDrawer.SectionAt(intVec).GetLayer(typeof(SectionLayer_ThingsGeneral));
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