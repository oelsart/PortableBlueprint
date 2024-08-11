using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CarryableBlueprint
{
    public class Designator_MakeBlueprint : Designator
    {
        public override int DraggableDimensions => 2;

        public Designator_MakeBlueprint(Building_DrawingTable building)
        {
            this.defaultLabel = "CB.DesignatorMakeBlueprint".Translate();
            this.defaultDesc = "CB.DesignatorMakeBlueprintDesc".Translate();
            this.icon = ContentFinder<Texture2D>.Get("CarryableBlueprint/UI/MakeBlueprint", true);
            this.soundDragSustain = SoundDefOf.Designate_DragStandard;
            this.soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            this.useMouseIcon = false;
            this.Order = 50f;
            this.isOrder = true;
            this.drawingTable = building;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return AcceptanceReport.WasAccepted;
        }

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            return t as Building != null && t.def.BuildableByPlayer;
        }

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            IEnumerable<Thing> buildings = cells.SelectMany(c => c.GetThingList(this.Map).Where(t => this.CanDesignateThing(t)));
            buildings = buildings.Distinct();
            IEnumerable<IntVec3> designateCells = Enumerable.Empty<IntVec3>();

            IntVec3 min;
            IntVec3 max;
            if (CarryableBlueprint.settings.makeBlueprintSettings["CB.IncludeFloors"])
            {
                designateCells = cells.Where(c => c.GetTerrain(this.Map).BuildableByPlayer);
            }

            var existBuildings = !buildings.EnumerableNullOrEmpty();
            var existFloors = !designateCells.EnumerableNullOrEmpty();

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
            IntVec3 center = new IntVec3((min.x + max.x) / 2, 0, (min.z + max.z) / 2);

            var bill_Blueprint = new Bill_Blueprint(CB_DefOf.CB_DrawBlueprint);
            if (existBuildings)
            {
                bill_Blueprint.BuildingLayoutList = buildings.Select(b => new BuildingLayout(b.def, b.Stuff, b.Position - center, b.Rotation, b.StyleDef, b.StyleSourcePrecept)).ToList();
            }
            if (existFloors)
            {
                bill_Blueprint.FloorLayoutList = designateCells.Select(c => new FloorLayout(c.GetTerrain(this.Map), c - center)).ToList();
            }

            Find.WindowStack.Add(new Dialog_GiveBlueprintName(this.drawingTable, center, s =>
            {
                bill_Blueprint.BlueprintName = s;
                drawingTable.billStack.AddBill(bill_Blueprint);
                if (CarryableBlueprint.settings.makeBlueprintSettings["CB.DeconstructOrUninstall"])
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
            Find.WindowStack.ImmediateWindow(15254158, CarryableBlueprint.UI.windowRect, WindowLayer.GameUI, () => CarryableBlueprint.UI.DoWindowContents());
        }

        public override void RenderHighlight(List<IntVec3> dragCells)
        {
            base.RenderHighlight(dragCells);
            if (CarryableBlueprint.settings.makeBlueprintSettings["CB.IncludeFloors"])
            {
                foreach (var c in Find.DesignatorManager.Dragger.DragCells)
                {
                    if (c.GetTerrain(this.Map).BuildableByPlayer)
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

        private readonly Building_DrawingTable drawingTable;
    }
}
