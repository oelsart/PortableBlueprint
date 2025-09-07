using RimWorld;
using System.Collections.Generic;
using System.Linq;
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
        icon = ContentFinder<Texture2D>.Get("UI/Designators/NoRoofArea", true);
        hotKey = KeyBindingDefOf.Misc5;
        soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
        soundDragChanged = null;
        soundSucceeded = SoundDefOf.Designate_ZoneAdd;
        useMouseIcon = true;
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
        return !base.Map.areaManager.Get<Area_NoTentRoof>()[c];
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        base.Map.areaManager.Get<Area_NoTentRoof>()[c] = true;
        Designator_AreaNoTentRoof.justAddedCells.Add(c);
    }

    protected override void FinalizeDesignationSucceeded()
    {
        base.FinalizeDesignationSucceeded();
        for (int i = 0; i < Designator_AreaNoTentRoof.justAddedCells.Count; i++)
        {
            base.Map.areaManager.Get<Area_BuildTentRoof>()[Designator_AreaNoTentRoof.justAddedCells[i]] = false;
        }
        Designator_AreaNoTentRoof.justAddedCells.Clear();
    }

    public override void SelectedUpdate()
    {
        GenUI.RenderMouseoverBracket();
        GenUI.RenderMouseoverBracket();
        foreach (var tentPole in tentPoles)
        {
            GenDraw.DrawRadiusRing(tentPole.Position, tentPole.def.specialDisplayRadius);
        }
        base.Map.areaManager.Get<Area_NoTentRoof>().MarkForDraw();
        base.Map.areaManager.Get<Area_BuildTentRoof>().MarkForDraw();
    }
}
