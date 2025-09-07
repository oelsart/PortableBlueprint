using RimWorld;
using System.Collections.Generic;
using System.Linq;
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
        icon = ContentFinder<Texture2D>.Get("UI/Designators/BuildRoofArea", true);
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
        if (!c.InBounds(base.Map))
        {
            return false;
        }
        if (c.Fogged(base.Map))
        {
            return false;
        }
        return !base.Map.areaManager.Get<Area_BuildTentRoof>()[c];
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        base.Map.areaManager.Get<Area_BuildTentRoof>()[c] = true;
        base.Map.areaManager.Get<Area_NoTentRoof>()[c] = false;
    }

    public override void SelectedUpdate()
    {
        GenUI.RenderMouseoverBracket();
        foreach (var tentPole in tentPoles)
        {
            GenDraw.DrawRadiusRing(tentPole.Position, tentPole.def.specialDisplayRadius);
        }
        base.Map.areaManager.Get<Area_NoTentRoof>().MarkForDraw();
        base.Map.areaManager.Get<Area_BuildTentRoof>().MarkForDraw();
    }
}
