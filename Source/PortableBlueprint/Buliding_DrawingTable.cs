using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace PortableBlueprint
{
    [StaticConstructorOnStartup]
    public class Building_DrawingTable : Building_WorkTable, IBillGiver, IBillGiverWithTickAction
    {
        public bool Flipped {
            get
            {
                return this.flippedInt;
            }
            set
            {
                this.flippedInt = value;
            }
        }

        public override List<IntVec3> InteractionCells => new List<IntVec3> { this.InteractionCell };

        public override IntVec3 InteractionCell
        {
            get
            {
                return ThingUtility.InteractionCell(this.InteractionOffset, this.Position, this.Rotation);
            }
        }

        protected IntVec3 InteractionOffset
        {
            get
            {
                if (this.Flipped)
                {
                    var offsetX = -def.interactionCellOffset.x;
                    if (this.def.Size.x % 2 == 0)
                    {
                        offsetX++;
                    }
                    return new IntVec3(offsetX, this.def.interactionCellOffset.y, this.def.interactionCellOffset.z);
                }
                else
                {
                    return this.def.interactionCellOffset;
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            ColorInt? glowerColorOverride = null;
            CompGlower comp;
            if ((comp = base.GetComp<CompGlower>()) != null && comp.HasGlowColorOverride)
            {
                glowerColorOverride = new ColorInt?(comp.GlowColor);
            }
            Designator_Build des = BuildCopyCommandUtility.FindAllowedDesignator(this.def, true);
            if (des != null)
            {
                var flipCommand = FlipBuildingCommandUtility.FlipCommand(this, this.def, base.Stuff, base.StyleSourcePrecept as Precept_Building, this.StyleDef, true,
                    "PB.CommandFlipBuilding".Translate(),
                    string.Format("PB.CommandFlipBuildingDesc".Translate(), this.LabelNoCount),
                    true, glowerColorOverride, ContentFinder<Texture2D>.Get("PortableBlueprint/UI/FlipIcon"), des);
                yield return flipCommand;
            }
            this.makeBlueprintDes = new Designator_MakeBlueprint(this);
            yield return this.makeBlueprintDes;
        }

        public void Notify_BuildingFlipped()
        {
            this.Map.mapDrawer.SectionAt(this.Position).GetLayer(typeof(SectionLayer_ThingsGeneral)).Regenerate();
        }

        public override void DrawExtraSelectionOverlays()
        {
            if (this.def.specialDisplayRadius > 0.1f)
            {
                GenDraw.DrawRadiusRing(this.Position, this.def.specialDisplayRadius);
            }
            if (this.def.drawPlaceWorkersWhileSelected && this.def.PlaceWorkers != null)
            {
                for (int i = 0; i < this.def.PlaceWorkers.Count; i++)
                {
                    this.def.PlaceWorkers[i].DrawGhost(this.def, this.Position, this.Rotation, Color.white, this);
                }
            }
            Building_DrawingTable.DrawInteractionCell(null, this.def, this.InteractionOffset, this.Position, this.Rotation);
        }

        public override void Print(SectionLayer layer)
        {
            base.Print(layer);
            var rot = this.Rotation;
            if (this.Flipped && rot.IsHorizontal && (this.def.GetModExtension<FlippableBuildingExtension>()?.hasFlatSurface ?? false))
            {
                rot = rot.Opposite;
            }
            ReversePatch_Graphic_Print.Print(this.Graphic, layer, this, 0f, rot, this.Flipped);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.flippedInt, "flipped");
        }

        private bool flippedInt = false;

        private static readonly FastInvokeHandler DrawInteractionCell = MethodInvoker.GetHandler(AccessTools.Method(typeof(GenDraw), "DrawInteractionCell"));

        public Designator_MakeBlueprint makeBlueprintDes;

        private static FastInvokeHandler PrintCompFXGraphic;
    }

    [HarmonyPatch(typeof(Graphic), "Print")]
    public static class ReversePatch_Graphic_Print
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        public static void Print(Graphic instance, SectionLayer layer, Thing thing, float extraRotation, Rot4 overrideRot, bool overrideFlipFlag)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var codes = instructions.ToList();
                var pos = codes.FindIndex(c => c.opcode == OpCodes.Callvirt && c.operand.Equals(AccessTools.Method(typeof(Graphic), nameof(Graphic.MatAt)))) - 1;

                codes.InsertRange(pos, new CodeInstruction[]
                {
                    CodeInstruction.LoadArgument(4),
                    CodeInstruction.LoadArgument(5),
                    CodeInstruction.StoreLocal(0)
                });
                codes.RemoveRange(pos - 2, 2);
                return codes;
            }
            _ = Transpiler(null, null);
        }
    }
}
