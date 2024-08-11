using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace CarryableBlueprint
{
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
            var flipCommand = FlipBuildingCommandUtility.FlipCommand(this, this.def, base.Stuff, base.StyleSourcePrecept as Precept_Building, this.StyleDef, true,
                "CB.CommandFlipBuilding".Translate(),
                string.Format("CB.CommandFlipBuildingDesc".Translate(), this.LabelNoCount),
                true, glowerColorOverride, ContentFinder<Texture2D>.Get("CarryableBlueprint/UI/FlipIcon"));
            yield return flipCommand;
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
            ReversePatch_Graphic_Print.Print(this.Graphic, layer, this, 0f);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.flippedInt, "flipped");
        }

        private bool flippedInt = false;

        private static readonly FastInvokeHandler DrawInteractionCell = MethodInvoker.GetHandler(AccessTools.Method(typeof(GenDraw), "DrawInteractionCell"));

        public Designator_MakeBlueprint makeBlueprintDes;
    }

    [HarmonyPatch(typeof(Graphic), "Print")]
    public static class ReversePatch_Graphic_Print
    {
        [HarmonyReversePatch]
        public static void Print(Graphic instance, SectionLayer layer, Thing thing, float extraRotation)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var codes = instructions.ToList();
                var label = generator.DefineLabel();
                var loc = generator.DeclareLocal(typeof(Rot4));
                var pos = codes.FindIndex(c => c.opcode == OpCodes.Callvirt && c.operand.Equals(AccessTools.Method(typeof(Graphic), nameof(Graphic.MatAt)))) - 1;
                codes[pos].labels.Add(label);

                codes.InsertRange(pos, new CodeInstruction[]
                {
                    CodeInstruction.LoadArgument(2),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Building_DrawingTable), "Flipped")),
                    new CodeInstruction(OpCodes.Brfalse_S, label),
                    new CodeInstruction(OpCodes.Stloc_S, loc),
                    new CodeInstruction(OpCodes.Ldloca_S, loc),
                    CodeInstruction.Call(typeof(Rot4), "get_Opposite")
                });
                return codes;
            }
            _ = Transpiler(null, null);
        }
    }
}
