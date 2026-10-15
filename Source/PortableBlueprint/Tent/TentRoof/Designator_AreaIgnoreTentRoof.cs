using RimWorld;
using UnityEngine;
using Verse;

namespace PortableBlueprint.Tent.TentRoof;

public class Designator_AreaIgnoreTentRoof : Designator_AreaIgnoreRoof
{
	private readonly List<Building> tentPoles;

	public Designator_AreaIgnoreTentRoof()
	{
		defaultLabel = "DesignatorAreaIgnoreRoofExpand".Translate();
		defaultDesc = "DesignatorAreaIgnoreRoofExpandDesc".Translate();
		icon = ContentFinder<Texture2D>.Get("UI/Designators/IgnoreRoofArea");
		hotKey = KeyBindingDefOf.Misc11;
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

		return Map.areaManager.Get<Area_BuildTentRoof>()[c] || Map.areaManager.Get<Area_NoTentRoof>()[c];
	}

	public override void DesignateSingleCell(IntVec3 c)
	{
		Map.areaManager.Get<Area_BuildTentRoof>()[c] = false;
		Map.areaManager.Get<Area_NoTentRoof>()[c] = false;
	}

	public override void SelectedUpdate()
	{
		GenUI.RenderMouseoverBracket();
		foreach (var tentPole in tentPoles)
		{
			GenDraw.DrawRadiusRing(tentPole.Position, tentPole.def.specialDisplayRadius);
		}

		Map.areaManager.Get<Area_NoTentRoof>().MarkForDraw();
		Map.areaManager.Get<Area_BuildTentRoof>().MarkForDraw();
	}
}