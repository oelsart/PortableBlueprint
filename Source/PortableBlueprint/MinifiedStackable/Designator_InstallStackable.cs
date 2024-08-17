using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace PortableBlueprint
{
    public class Designator_InstallStackable : Designator_Install
    {
        public override int DraggableDimensions => this.PlacingDef.placingDraggableDimensions;

        public override bool DragDrawMeasurements => true;
        public override string Label => "CommandInstall".Translate();

        public override string Desc => "CommandInstallDesc".Translate();

        public override bool Visible
        {
            get
            {
                var selectedThings = Find.Selector.SelectedObjects.Select(o => o as Thing);
                return selectedThings.All(t => t.def == selectedThings.First().def);
            }
        }

        protected List<MinifiedThing_Stackable> MinifiedStackableList
        {
            get
            {
                if (this.minifiedStackableList == null)
                {
                    this.minifiedStackableList = Find.Selector.SelectedObjects.Select(o => o as MinifiedThing_Stackable).ToList();
                }
                return this.minifiedStackableList;
            }
        }

        new protected Thing ThingToInstall => MinifiedStackableList.First().InnerThing;

        public override BuildableDef PlacingDef => this.ThingToInstall.def;

        public override bool CanRemainSelected()
        {
            return true;
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            foreach (var minifiedStackable in this.MinifiedStackableList)
            {
                if (minifiedStackable != null)
                {
                    foreach (var b in minifiedStackable.blueprintsForDrawLine.ToArray())
                    {
                        if (!b.Destroyed)
                        {
                            b.Destroy(DestroyMode.Cancel);
                        }
                    }
                    minifiedStackable.blueprintsForDrawLine.Clear();
                }
            }
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            return GenConstruct.CanPlaceBlueprintAt_NewTemp(this.ThingToInstall.def, c, Rot4.North, this.Map);
        }

        public override void RenderHighlight(List<IntVec3> dragCells)
        {
            if (dragCells.Count == 0) return;
            dragCells = this.DeterminCells(dragCells).Where(c => this.CanDesignateCell(c)).ToList();
            DesignatorUtility.RenderHighlightOverSelectableCells(this, dragCells);
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            GenSpawn.WipeExistingThings(c, this.placingRot, this.MinifiedStackableList[thingCount].InnerThing.def.installBlueprintDef, base.Map, DestroyMode.Deconstruct);
            var blueprint = GenConstruct.PlaceBlueprintForInstall(this.MinifiedStackableList[thingCount], c, base.Map, this.placingRot, Faction.OfPlayer, true);
            FleckMaker.ThrowMetaPuffs(GenAdj.OccupiedRect(c, this.placingRot, this.PlacingDef.Size), base.Map);
            this.stackCount++;
            if (this.stackCount == this.MinifiedStackableList[thingCount].stackCount)
            {
                this.stackCount = 0;
                this.thingCount++;
                if (this.thingCount == this.MinifiedStackableList.Count)
                {
                    this.thingCount = 0;
                    Find.DesignatorManager.Deselect();
                }
            }
            this.MinifiedStackableList[thingCount].blueprintsForDrawLine.Add(blueprint);
        }

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            if (cells.Count() == 0) return;
            var sumStackCount = this.MinifiedStackableList.Sum(m => m.stackCount);
            cells = this.DeterminCells(cells.ToList()).Where(c => this.CanDesignateCell(c));

            foreach (var (c, i) in cells.Select((c, i) => (c, i)))
            {
                if (i >= sumStackCount) break;
                GenSpawn.WipeExistingThings(c, this.placingRot, this.MinifiedStackableList[thingCount].InnerThing.def.installBlueprintDef, base.Map, DestroyMode.Deconstruct);
                var blueprint = GenConstruct.PlaceBlueprintForInstall(this.MinifiedStackableList[thingCount], c, base.Map, this.placingRot, Faction.OfPlayer, true);
                FleckMaker.ThrowMetaPuffs(GenAdj.OccupiedRect(c, this.placingRot, this.PlacingDef.Size), base.Map);
                this.minifiedStackableList[thingCount].blueprintsForDrawLine.Add(blueprint);
                this.stackCount++;
                if (this.stackCount == this.MinifiedStackableList[thingCount].stackCount)
                {
                    this.stackCount = 0;
                    this.thingCount++;
                    if (this.thingCount == this.MinifiedStackableList.Count)
                    {
                        this.thingCount = 0;
                    }
                }
            }
            Find.DesignatorManager.Deselect();
        }

        public override void SelectedProcessInput(Event ev)
        {
            base.SelectedProcessInput(ev);
            if (Input.GetMouseButtonDown(0))
            {
                this.dragOrigin = UI.MouseCell();
            }
        }

        protected override void DrawGhost(Color ghostCol)
        {
            ThingDef def;
            if ((def = (this.PlacingDef as ThingDef)) != null)
            {
                MeditationUtility.DrawMeditationFociAffectedByBuildingOverlay(base.Map, def, Faction.OfPlayer, UI.MouseCell(), this.placingRot);
                GauranlenUtility.DrawConnectionsAffectedByBuildingOverlay(base.Map, def, Faction.OfPlayer, UI.MouseCell(), this.placingRot);
                PsychicRitualUtility.DrawPsychicRitualSpotsAffectedByThingOverlay(base.Map, def, UI.MouseCell(), this.placingRot);
            }
            Graphic baseGraphic = this.ThingToInstall.Graphic.ExtractInnerGraphicFor(this.ThingToInstall, null);
            GhostDrawer.DrawGhostThing(UI.MouseCell(), this.placingRot, (ThingDef)this.PlacingDef, baseGraphic, ghostCol, AltitudeLayer.Blueprint, this.ThingToInstall, true, this.StuffDef);
        }

        private IEnumerable<IntVec3> DeterminCells(List<IntVec3> dragCells)
        {
            var sumStackCount = this.MinifiedStackableList.Sum(m => m.stackCount);
            IEnumerable<IntVec3> cells;
            if (this.DraggableDimensions == 1)
            {
                if (dragCells[0] == this.dragOrigin)
                {
                    cells = dragCells.GetRange(0, Math.Min(dragCells.Count, sumStackCount));
                }
                else
                {
                    cells = dragCells.GetRange(Math.Max(0, dragCells.Count - sumStackCount), Math.Min(dragCells.Count, sumStackCount));
                }
            }
            else
            {
                var mouseCell = UI.MouseCell();
                var cellRect = CellRect.FromLimits(this.dragOrigin, mouseCell);
                var diff = this.dragOrigin - mouseCell;
                var direction = new IntVec2((int)Mathf.Clamp(diff.x, -1f, 1f), (int)Mathf.Clamp(diff.z, -1f, 1f));
                while (cellRect.Count() > sumStackCount)
                {
                    if (cellRect.Width > cellRect.Height)
                    {
                        mouseCell.x += direction.x;
                        cellRect = CellRect.FromLimits(this.dragOrigin, mouseCell);
                    }
                    else
                    {
                        mouseCell.z += direction.z;
                        cellRect = CellRect.FromLimits(this.dragOrigin, mouseCell);
                    }
                }
                cells = cellRect.Cells;
            }
            return cells;
        }

        private List<MinifiedThing_Stackable> minifiedStackableList;

        private int stackCount = 0;

        private int thingCount = 0;

        private IntVec3 dragOrigin;
    }
}
