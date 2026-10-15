using RimWorld;
using UnityEngine;
using Verse;

namespace PortableBlueprint.Tent.TentRoof;

public class Designator_AreaBuildTentRoof : Designator_AreaBuildRoof
{
	private readonly List<Building> tentPoles;

	public Designator_AreaBuildTentRoof()
	{
		defaultLabel = "PB.DesignatorAreaBuildRoofExpand".Translate();
		defaultDesc = "PB.DesignatorAreaBuildRoofExpandDesc".Translate();
		icon = ContentFinder<Texture2D>.Get("UI/Designators/BuildRoofArea");
		hotKey = KeyBindingDefOf.Misc9;
		soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
		soundDragChanged = SoundDefOf.Designate_DragZone_Changed;
		soundSucceeded = SoundDefOf.Designate_ZoneAdd_Roof;
		useMouseIcon = true;
		tutorTag = "AreaBuildTentRoofExpand";
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

		return !Map.areaManager.Get<Area_BuildTentRoof>()[c];
	}

	public override void DesignateSingleCell(IntVec3 c)
	{
		Map.areaManager.Get<Area_BuildTentRoof>()[c] = true;
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