using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace CarryableBlueprint
{
    public class ITab_DrawingTable : ITab_Bills
    {
        protected Building_DrawingTable SelDrawingTable => (Building_DrawingTable)base.SelThing;

        protected override void FillTab()
        {
            ReversePatch_ITab_Bills_FillTab.FillTab(this, this.SelDrawingTable.makeBlueprintDes);
        }
    }

    [HarmonyPatch(typeof(ITab_Bills), "FillTab")]
    public static class ReversePatch_ITab_Bills_FillTab
    {
        [HarmonyReversePatch]
        public static void FillTab(ITab_DrawingTable instance, Designator_MakeBlueprint des)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                var pos = codes.FindIndex(c => c.opcode == OpCodes.Callvirt && c.operand.Equals(AccessTools.Method(typeof(BillStack), nameof(BillStack.DoListing))));
                codes.Replace(codes[pos], CodeInstruction.Call(typeof(ReversePatch_BillStack_DoListing), nameof(ReversePatch_BillStack_DoListing.DoListing)));
                codes.Insert(pos, CodeInstruction.LoadArgument(1));
                return codes;
            }
            _ = Transpiler(null);
            throw new NotImplementedException();
        }
    }

    [HarmonyPatch(typeof(BillStack), nameof(BillStack.DoListing))]
    public static class ReversePatch_BillStack_DoListing
    {
        [HarmonyReversePatch]
        public static Bill DoListing(BillStack instance, Rect rect, Func<List<FloatMenuOption>> recipeOptionsMaker, ref Vector2 scrollPosition, ref float viewHeight, Designator_MakeBlueprint des)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                var pos = codes.FindIndex(c => c.opcode == OpCodes.Ldstr && c.operand.Equals("AddBill"));
                codes[pos].operand = "CB.DesignatorMakeBlueprint";
                pos = codes.FindIndex(c => c.opcode == OpCodes.Call && c.operand.Equals(AccessTools.PropertyGetter(typeof(Find), nameof(Find.WindowStack))));
                var pos2 = codes.FindIndex(pos, c => c.opcode == OpCodes.Callvirt && c.operand.Equals(AccessTools.Method(typeof(WindowStack), nameof(WindowStack.Add))));
                codes.RemoveRange(pos, pos2 - pos + 1);

                codes.InsertRange(pos, new CodeInstruction[]
                {
                    new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Find), nameof(Find.DesignatorManager))),
                    CodeInstruction.LoadArgument(5),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(DesignatorManager), nameof(DesignatorManager.Select)))
                });
                return codes;
            }
            _ = Transpiler(null);
            throw new NotImplementedException();
        }
    }
}
