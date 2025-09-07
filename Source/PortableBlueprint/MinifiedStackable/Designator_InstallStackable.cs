using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PortableBlueprint;

public class Designator_InstallStackable : Designator_Install
{
    public override bool DragDrawMeasurements => true;

    public override string Label => "CommandInstall".Translate();

    public override string Desc => "CommandInstallDesc".Translate();

    public override bool Visible
    {
        get
        {
            return Find.Selector.SelectedObjects.OfType<MinifiedThingStackable>().Select(t => t.def).Distinct().Count() == 1;
        }
    }

    protected List<MinifiedThingStackable> MinifiedStackableList
    {
        get
        {
            minifiedStackableList ??= [.. Find.Selector.SelectedObjects.OfType<MinifiedThingStackable>()];
            return minifiedStackableList;
        }
    }

    new protected Thing ThingToInstall => MinifiedStackableList.FirstOrDefault()?.InnerThing;

    public override BuildableDef PlacingDef => ThingToInstall?.def;

    public override bool CanRemainSelected()
    {
        return true;
    }

    public override void ProcessInput(Event ev)
    {
        base.ProcessInput(ev);
        foreach (var minifiedStackable in MinifiedStackableList)
        {
            for (var i = minifiedStackable.blueprintsForDrawLine.Count - 1; i >= 0; i--)
            {
                var blueprint = minifiedStackable.blueprintsForDrawLine[i];
                if (!blueprint.Destroyed)
                {
                    blueprint.Destroy(DestroyMode.Cancel);
                }
            }
            minifiedStackable.blueprintsForDrawLine.Clear();
        }
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        if (ThingToInstall is null) return false;
        return GenConstruct.CanPlaceBlueprintAt(ThingToInstall.def, c, Rot4.North, Map);
    }

    public override void RenderHighlight(List<IntVec3> dragCells)
    {
        if (dragCells.Count == 0) return;
        dragCells = [.. DeterminCells(dragCells).Where(c => CanDesignateCell(c))];
        DesignatorUtility.RenderHighlightOverSelectableCells(this, dragCells);
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        GenSpawn.WipeExistingThings(c, placingRot, MinifiedStackableList[thingCount].InnerThing.def.installBlueprintDef, Map, DestroyMode.Deconstruct);
        var blueprint = GenConstructEx.PlaceBlueprintForInstallEnroute(MinifiedStackableList[thingCount], c, Map, placingRot, Faction.OfPlayer, true);
        FleckMaker.ThrowMetaPuffs(GenAdj.OccupiedRect(c, placingRot, PlacingDef.Size), Map);
        stackCount++;
        if (stackCount == MinifiedStackableList[thingCount].stackCount)
        {
            stackCount = 0;
            thingCount++;
            if (thingCount == MinifiedStackableList.Count)
            {
                thingCount = 0;
                Find.DesignatorManager.Deselect();
            }
        }
        MinifiedStackableList[thingCount].blueprintsForDrawLine.Add(blueprint);
    }

    public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
    {
        if (cells.EnumerableNullOrEmpty()) return;
        cells = DeterminCells(cells.Reverse()).Where(c => CanDesignateCell(c));

        foreach (var c in cells)
        {
            DesignateSingleCell(c);
        }
        Find.DesignatorManager.Deselect();
    }

    public override void SelectedProcessInput(Event ev)
    {
        base.SelectedProcessInput(ev);
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = UI.MouseCell();
        }
    }

    protected override void DrawGhost(Color ghostCol)
    {
        if (PlacingDef is ThingDef def)
        {
            MeditationUtility.DrawMeditationFociAffectedByBuildingOverlay(base.Map, def, Faction.OfPlayer, UI.MouseCell(), placingRot);
            GauranlenUtility.DrawConnectionsAffectedByBuildingOverlay(base.Map, def, Faction.OfPlayer, UI.MouseCell(), placingRot);
            PsychicRitualUtility.DrawPsychicRitualSpotsAffectedByThingOverlay(base.Map, def, UI.MouseCell(), placingRot);
        }

        if (ThingToInstall != null)
        {
            Graphic baseGraphic = ThingToInstall.Graphic.ExtractInnerGraphicFor(ThingToInstall, null);
            GhostDrawer.DrawGhostThing(UI.MouseCell(), placingRot, (ThingDef)PlacingDef, baseGraphic, ghostCol, AltitudeLayer.Blueprint, ThingToInstall, true, StuffDef);
        }
    }

    private IEnumerable<IntVec3> DeterminCells(IEnumerable<IntVec3> dragCells)
    {
        var sumStackCount = MinifiedStackableList.Sum(m => m.stackCount);
        return dragCells.Take(sumStackCount);
    }

    private List<MinifiedThingStackable> minifiedStackableList;

    private int stackCount = 0;

    private int thingCount = 0;

    private IntVec3 dragOrigin;
}
