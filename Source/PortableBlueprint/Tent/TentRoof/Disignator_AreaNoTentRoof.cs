using RimWorld;
using UnityEngine;
using Verse;

namespace PortableBlueprint.Tent.TentRoof;

public class Designator_AreaNoTentRoof : Designator_AreaNoRoof
{
	private readonly List<Building> tentPoles;

	private static readonly List<IntVec3> justAddedCells = [];

	public Designator_AreaNoTentRoof()
	{
		defaultLabel = "PB.DesignatorAreaNoTentRoofExpand".Translate();
		defaultDesc = "PB.DesignatorAreaNoTetRoofExpandDesc".Translate();
		icon = ContentFinder<Texture2D>.Get("UI/Designators/NoRoofArea");
		hotKey = KeyBindingDefOf.Misc5;
		soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
		soundDragChanged = null;
		soundSucceeded = SoundDefOf.Designate_ZoneAdd;
		useMouseIcon = true;
		tentPoles = Map.listerBuildings.allBuildingsColonist.Where(b => b.HasComp<TentPoleComp>()).ToList();
	}

	public override AcceptanceReport CanDesignateCell(IntVec3 c)
	{
		if (!c.InBounds(Map))
		{
			return false;
		}

		if (c.Fogged(Map))
		{
			return false;
		}

		return !Map.areaManager.Get<Area_NoTentRoof>()[c];
	}

	public override void DesignateSingleCell(IntVec3 c)
	{
		Map.areaManager.Get<Area_NoTentRoof>()[c] = true;
		justAddedCells.Add(c);
	}

	protected override void FinalizeDesignationSucceeded()
	{
		base.FinalizeDesignationSucceeded();
		for (var i = 0; i < justAddedCells.Count; i++)
		{
			Map.areaManager.Get<Area_BuildTentRoof>()[justAddedCells[i]] = false;
		}

		justAddedCells.Clear();
	}

	public override void SelectedUpdate()
	{
		GenUI.RenderMouseoverBracket();
		GenUI.RenderMouseoverBracket();
		foreach (var tentPole in tentPoles)
		{
			GenDraw.DrawRadiusRing(tentPole.Position, tentPole.def.specialDisplayRadius);
		}

		Map.areaManager.Get<Area_NoTentRoof>().MarkForDraw();
		Map.areaManager.Get<Area_BuildTentRoof>().MarkForDraw();
	}
}