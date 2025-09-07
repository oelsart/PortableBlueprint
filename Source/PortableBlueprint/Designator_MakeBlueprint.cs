using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PortableBlueprint;

public class Designator_MakeBlueprint : Designator
{
    private readonly Building_DrawingTable drawingTable;

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Orders;

    public Designator_MakeBlueprint(Building_DrawingTable building)
    {
        defaultLabel = "PB.DesignatorMakeBlueprint".Translate();
        defaultDesc = "PB.DesignatorMakeBlueprintDesc".Translate();
        icon = ContentFinder<Texture2D>.Get("PortableBlueprint/UI/MakeBlueprint", true);
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        useMouseIcon = false;
        Order = 50f;
        isOrder = true;
        drawingTable = building;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 loc)
    {
        return AcceptanceReport.WasAccepted;
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        return (t as Building) != null && t.def.BuildableByPlayer;
    }

    public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
    {
        var buildings = cells.SelectMany(c => c.GetThingList(Map).Where(t => CanDesignateThing(t))).Distinct().ToList();
        List<IntVec3> designateCells = null;

        IntVec3 min;
        IntVec3 max;
        if (PortableBlueprint.settings.makeBlueprintSettings["PB.IncludeFloors"])
        {
            designateCells = [.. cells.Where(c => c.GetTerrain(Map).BuildableByPlayer)];
        }

        var existBuildings = !buildings.NullOrEmpty();
        var existFloors = !designateCells.NullOrEmpty();

        if (existBuildings)
        {
            min = new IntVec3(buildings.Min(b => b.Position.x), 0, buildings.Min(b => b.Position.z));
            max = new IntVec3(buildings.Max(b => b.Position.x), 0, buildings.Max(b => b.Position.z));

            if (existFloors)
            {
                min = new IntVec3(Math.Min(min.x, designateCells.Min(c => c.x)), 0, Math.Min(min.z, designateCells.Min(c => c.z)));
                max = new IntVec3(Math.Max(min.x, designateCells.Max(c => c.x)), 0, Math.Max(min.z, designateCells.Max(c => c.z)));
            }
        }
        else if (existFloors)
        {
            min = new IntVec3(designateCells.Min(c => c.x), 0, designateCells.Min(c => c.z));
            max = new IntVec3(designateCells.Max(c => c.x), 0, designateCells.Max(c => c.z));
        }
        else return;
        IntVec3 center = new((min.x + max.x) / 2, 0, (min.z + max.z) / 2);
        var terrainGrid = Map.terrainGrid;

        var bill_Blueprint = new Bill_Blueprint(PB_DefOf.PB_DrawBlueprint,
            existBuildings ? [.. buildings.Select(b => new BuildingLayout(b.def, b.Stuff, b.Position - center, b.Rotation, b.StyleDef, b.StyleSourcePrecept))] : null,
            existFloors ? [.. designateCells.Select(c => new FloorLayout(terrainGrid.TopTerrainAt(c), terrainGrid.FoundationAt(c), c - center))] : null);

        Find.WindowStack.Add(new Dialog_GiveBlueprintName(drawingTable, center, s =>
        {
            bill_Blueprint.BlueprintName = s;
            drawingTable.billStack.AddBill(bill_Blueprint);
            if (PortableBlueprint.settings.makeBlueprintSettings["PB.DeconstructOrUninstall"])
            {
                if (existBuildings)
                {
                    foreach (var building in buildings)
                    {
                        var designation = building.def.Minifiable ? DesignationDefOf.Uninstall : DesignationDefOf.Deconstruct;
                        if (!Map.designationManager.HasMapDesignationOn(building))
                        {
                            Map.designationManager.AddDesignation(new Designation(building, designation));
                        }
                    }
                }
                if (existFloors)
                {
                    foreach (var c in designateCells)
                    {
                        if (!Map.designationManager.HasMapDesignationAt(c))
                        {
                            Map.designationManager.AddDesignation(new Designation(c, DesignationDefOf.RemoveFloor));
                        }
                    }
                }
            }
        }));
    }

    public override void DoExtraGuiControls(float leftX, float bottomY)
    {
        PortableBlueprint.UI.curSettings = PortableBlueprint.settings.makeBlueprintSettings;
        Find.WindowStack.ImmediateWindow(15254158, PortableBlueprint.UI.windowRect, WindowLayer.GameUI, PortableBlueprint.UI.DoWindowContents);
    }

    public override void RenderHighlight(List<IntVec3> dragCells)
    {
        base.RenderHighlight(dragCells);
        if (PortableBlueprint.settings.makeBlueprintSettings["PB.IncludeFloors"])
        {
            foreach (var c in Find.DesignatorManager.Dragger.DragCells)
            {
                if (c.GetTerrain(Map).BuildableByPlayer)
                {
                    Graphics.DrawMesh(MeshPool.plane10, c.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays), Quaternion.identity, DesignatorUtility.DragHighlightThingMat, 0);
                }
            }

        }
    }

    public override void SelectedUpdate()
    {
        GenUI.RenderMouseoverBracket();
    }
}
