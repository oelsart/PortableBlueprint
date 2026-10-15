using RimWorld;
using Verse;
using Verse.AI;

namespace PortableBlueprint.Tent;

public class JobDriver_BuildTentRoof : JobDriver_AffectRoof
{
	protected override PathEndMode PathEndMode => PathEndMode.Touch;

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOn(() => !Map.areaManager.Get<Area_BuildTentRoof>()[Cell]);
		this.FailOn(() => !TentRoofUtility.WithinRangeOfRoofHolder(Cell, Map));
		this.FailOn(() => !TentRoofUtility.ConnectedToRoofHolder(Cell, Map, true));
		foreach (var toil in base.MakeNewToils())
		{
			yield return toil;
		}
	}

	protected override void DoEffect()
	{
		for (var i = 0; i < 9; i++)
		{
			var intVec = Cell + GenAdj.AdjacentCellsAndInside[i];
			if (intVec.InBounds(Map) && Map.areaManager.Get<Area_BuildTentRoof>()[intVec] &&
			    Map.roofGrid.RoofAt(intVec) != PB_DefOf.PB_TentRoof &&
			    TentRoofUtility.GetFirstRoofHolder(intVec, Map) != null &&
			    RoofUtility.FirstBlockingThing(intVec, Map) == null)
			{
				Map.roofGrid.SetRoof(intVec, PB_DefOf.PB_TentRoof);
				MoteMaker.PlaceTempRoof(intVec, Map);
				var list = Map.thingGrid.ThingsListAt(intVec);
				for (var j = 0; j < list.Count; j++)
				{
					var thing = list[j];
					if (thing.def.building is { IsMortar: true } && thing.TryGetComp(out CompWakeUpDormant compWakeUpDormant))
					{
						compWakeUpDormant.Activate(pawn);
					}
				}
			}
		}

		HashSet<SectionLayer> regeneratedLayers = [];
		for (var i = 0; i < 4; i++)
		{
			var intVec = Cell + GenAdj.DiagonalDirections[i] * 2;
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
		return Cell.GetRoof(Map) == PB_DefOf.PB_TentRoof;
	}
}