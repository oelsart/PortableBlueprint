using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace PortableBlueprint;

[StaticConstructorOnStartup]
public class Building_DrawingTable : Building_WorkTable, IBillGiver, IBillGiverWithTickAction
{
    public bool Flipped
    {
        get
        {
            return flippedInt;
        }
        set
        {
            flippedInt = value;
        }
    }

    public override List<IntVec3> InteractionCells => [InteractionCell];

    public override IntVec3 InteractionCell
    {
        get
        {
            return ThingUtility.InteractionCell(InteractionOffset, Position, Rotation);
        }
    }

    protected IntVec3 InteractionOffset
    {
        get
        {
            if (Flipped)
            {
                var offsetX = -def.interactionCellOffset.x;
                if (def.Size.x % 2 == 0)
                {
                    offsetX++;
                }
                return new IntVec3(offsetX, def.interactionCellOffset.y, def.interactionCellOffset.z);
            }
            else
            {
                return def.interactionCellOffset;
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
        Designator_Build des = BuildCopyCommandUtility.FindAllowedDesignator(def, true);
        if (des != null)
        {
            var flipCommand = FlipBuildingCommandUtility.FlipCommand(this, def, base.Stuff, base.StyleSourcePrecept as Precept_Building, StyleDef, true,
                "PB.CommandFlipBuilding".Translate(),
                string.Format("PB.CommandFlipBuildingDesc".Translate(), LabelNoCount),
                true, glowerColorOverride, ContentFinder<Texture2D>.Get("PortableBlueprint/UI/FlipIcon"), des);
            yield return flipCommand;
        }
        makeBlueprintDes = new Designator_MakeBlueprint(this);
        yield return makeBlueprintDes;
    }

    public void Notify_BuildingFlipped()
    {
        Map.mapDrawer.SectionAt(Position).GetLayer(typeof(SectionLayer_ThingsGeneral)).Regenerate();
    }

    public override void DrawExtraSelectionOverlays()
    {
        if (def.specialDisplayRadius > 0.1f)
        {
            GenDraw.DrawRadiusRing(Position, def.specialDisplayRadius);
        }
        if (def.drawPlaceWorkersWhileSelected && def.PlaceWorkers != null)
        {
            for (int i = 0; i < def.PlaceWorkers.Count; i++)
            {
                def.PlaceWorkers[i].DrawGhost(def, Position, Rotation, Color.white, this);
            }
        }
        Building_DrawingTable.DrawInteractionCell(null, def, InteractionOffset, Position, Rotation);
    }

    public override void Print(SectionLayer layer)
    {
        var rot = Rotation;
        if (Flipped && rot.IsHorizontal && (def.GetModExtension<FlippableBuildingExtension>()?.hasFlatSurface ?? false))
        {
            rot = rot.Opposite;
        }
        PrintWithOverrideRotFlip(layer, rot, Flipped);

        foreach (var comp in AllComps)
        {
            if (comp.GetType().Name != "CompBackSideLayer")
            {
                comp.PostPrintOnto(layer);
            }
        }
    }

    private void PrintWithOverrideRotFlip(SectionLayer layer, Rot4 overrideRot, bool overrideFlipFlag)
    {
        ReversePatch_Graphic_Print.Print(Graphic, layer, this, 0f, overrideRot, Flipped);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref flippedInt, "flipped");
    }

    private bool flippedInt = false;

    private static readonly FastInvokeHandler DrawInteractionCell = MethodInvoker.GetHandler(AccessTools.Method(typeof(GenDraw), "DrawInteractionCell"));

    public Designator_MakeBlueprint makeBlueprintDes;
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

            codes.InsertRange(pos,
            [
                CodeInstruction.LoadArgument(4),
                CodeInstruction.LoadArgument(5),
                CodeInstruction.StoreLocal(0)
            ]);
            codes.RemoveRange(pos - 2, 2);
            return codes;
        }
        _ = Transpiler(null, null);
    }
}
