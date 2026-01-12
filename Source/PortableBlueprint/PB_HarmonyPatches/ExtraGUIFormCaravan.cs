using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace PortableBlueprint.PB_HarmonyPatches;

public class ExtraGUIFormCaravan
{
    private readonly Dictionary<TransferableOneWay, List<ThingDefCountClass>> totalCostList = [];

    private readonly Dictionary<TransferableOneWay, List<TransferableOneWay>> relatedThingList = [];

    private readonly Dictionary<TransferableOneWay, bool> collapse = [];

    private readonly Dictionary<TransferableOneWay, List<ThingDefCountClass>> cachedThresholds = [];

    private readonly Dictionary<TransferableOneWay, Dictionary<TransferableOneWay, int>> cachedMassThresholds = [];

    private readonly Dictionary<TransferableOneWay, Dictionary<TransferableOneWay, bool>> cachedReachedThreshold = [];

    private TransferableOneWayWidget itemsTransfer;

    private TransferableOneWayWidget travelSuppliesTransfer;

    private TransferableOneWayWidget currentWidget;

    private float availableMass;

    private float extraViewRectHeight;

    private bool recacheRequest;

    private static readonly FastInvokeHandler GetMass = MethodInvoker.GetHandler(AccessTools.Method(typeof(TransferableOneWayWidget), "GetMass"));

    private static readonly FastInvokeHandler GetTransferableCategory = MethodInvoker.GetHandler(AccessTools.Method(typeof(CaravanUIUtility), "GetTransferableCategory"));

    private static ExtraGUIFormCaravan Instance { get; set; }

    [HarmonyPatch(typeof(CaravanUIUtility), "CreateCaravanTransferableWidgets")]
    public static class Patch_CaravanUIUtility_CreateCaravanTransferableWidgets
    {
        public static void Postfix(List<TransferableOneWay> transferables, TransferableOneWayWidget itemsTransfer, TransferableOneWayWidget travelSuppliesTransfer, Func<float> availableMassGetter)
        {
            Instance = new ExtraGUIFormCaravan
            {
                itemsTransfer = itemsTransfer,
                travelSuppliesTransfer = travelSuppliesTransfer
            };
            if (itemsTransfer == null && travelSuppliesTransfer == null)
            {
                Instance.itemsTransfer = new TransferableOneWayWidget(transferables, null, null, null, false, IgnorePawnsInventoryMode.Ignore, false, availableMassGetter);
            }

            var _ = false;
            if (travelSuppliesTransfer != null && Find.WindowStack.TryGetWindow<Dialog_FormCaravan>(out var dialog))
            {
                dialog.DrawAutoSelectCheckbox(Rect.zero, ref _);
            }

            var blueprints = transferables.Where(t => t.AnyThing.HasComp<CompBlueprint>());
            foreach (var blueprint in blueprints)
            {
                var comp = blueprint.AnyThing.TryGetComp<CompBlueprint>();

                var buildingLayout = comp.BuildingLayoutList;
                var totalCost = comp.TotalCost;
                var stuffList = totalCost.Select(c => c.thingDef);
                var tradList = transferables.Where(t => stuffList.Contains(t.ThingDef));
                var minifiedList = transferables.Where(t => buildingLayout?.Any(b => t.AnyThing.GetInnerIfMinified()?.def == b.def) ?? false);
                minifiedList = minifiedList.OrderByDescending(m =>
                {
                    m.AnyThing.TryGetQuality(out var qc);
                    return qc;
                }).ThenBy(m => m.AnyThing.LabelNoParenthesis);
                Instance.relatedThingList[blueprint] = [.. minifiedList.ConcatIfNotNull(tradList)];
                Instance.totalCostList[blueprint] = totalCost;
                Instance.cachedThresholds[blueprint] = [.. totalCost];

                Instance.availableMass = availableMassGetter?.Invoke() ?? float.MaxValue;
                Instance.cachedReachedThreshold[blueprint] = [];
                Instance.cachedMassThresholds[blueprint] = [];
            }

            ReCacheThresholds();
        }
    }

    private static void ReCacheThresholds()
    {
        foreach (var blueprint in Instance.relatedThingList.Select(t => t.Key))
        {
            if (!Instance.relatedThingList.TryGetValue(blueprint, out var tradList) ||
                !Instance.cachedReachedThreshold.TryGetValue(blueprint, out var reachedThresholdDict) ||
                !Instance.cachedMassThresholds.TryGetValue(blueprint, out var massThreasholdDict))
                continue;
            foreach (var trad in tradList.Reverse<TransferableOneWay>())
            {
                var widget = WidgetSelect(trad);
                reachedThresholdDict[trad] = ReachedThreshold(blueprint, trad);
                var num = Instance.availableMass + (float)GetMass(widget, trad.AnyThing) * trad.CountToTransfer;
                var massThreshold = num <= 0f ? 0 : Mathf.FloorToInt(num / (float)GetMass(widget, trad.AnyThing));
                massThreasholdDict[trad] = massThreshold;
            }
            reachedThresholdDict[blueprint] = ReachedThreshold(null, blueprint);
        }
        Instance.recacheRequest = false;
    }

    private static bool ReachedThreshold(TransferableOneWay blueprint, TransferableOneWay trad)
    {
        if (trad.AnyThing == null) return false;

        if (trad.AnyThing.HasComp<CompBlueprint>())
        {
            return Instance.cachedThresholds[trad].All(t =>
            {
                var minified = Instance.relatedThingList[trad].Where(r =>
                    r.AnyThing.GetInnerIfMinified()?.CostListAdjusted().Any(c => c.thingDef == t.thingDef) ?? false).ToList();
                if (!minified.NullOrEmpty() && minified.All(m => ReachedThreshold(trad, m))) return true;
                var trad2 = Instance.relatedThingList[trad].FirstOrDefault(r => r.ThingDef == t.thingDef);
                return trad2 != null && ReachedThreshold(trad, trad2);
            });
        }

        if (trad.AnyThing is MinifiedThing minifiedThing)
        {
            return minifiedThing.InnerThing.CostListAdjusted().All(c =>
            {
                var trad2 = Instance.relatedThingList[blueprint].FirstOrDefault(t => c.thingDef == t.ThingDef);
                return trad2 != null && Instance.cachedReachedThreshold[blueprint][trad2];
            }) || trad.CountToTransfer >= blueprint.AnyThing.TryGetComp<CompBlueprint>().BuildingLayoutList.Count(b => b.def == minifiedThing.InnerThing.def);
        }
        var threshold = Instance.cachedThresholds[blueprint].FirstOrDefault(t => t.thingDef == trad.ThingDef);
        return threshold != null && trad.CountToTransfer >= threshold.count;
    }

    [HarmonyPatch(typeof(TransferableOneWayWidget), "FillMainRect")]
    public static class Patch_TransferableOneWayWidget_FillMainRect
    {
        [HarmonyReversePatch]
        public static void Original(TransferableOneWayWidget instance, Rect mainRect, out bool anythingChanged) => throw new NotImplementedException();

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var label0 = generator.DefineLabel();
            codes[0].labels.Add(label0);
            codes.InsertRange(0,
            [
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ExtraGUIFormCaravan), nameof(Instance))),
                new CodeInstruction(OpCodes.Brtrue_S, label0),
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadArgument(1),
                CodeInstruction.LoadArgument(2, true),
                CodeInstruction.Call(typeof(Patch_TransferableOneWayWidget_FillMainRect), nameof(Patch_TransferableOneWayWidget_FillMainRect.Original)),
                new CodeInstruction(OpCodes.Ret)
            ]);

            var pos = codes.FindIndex(c => c.opcode == OpCodes.Blt_S) + 1;
            codes.InsertRange(pos,
            [
                CodeInstruction.LoadLocal(0),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ExtraGUIFormCaravan), nameof(Instance))),
                CodeInstruction.LoadField(typeof(ExtraGUIFormCaravan), nameof(extraViewRectHeight)),
                new CodeInstruction(OpCodes.Add),
                CodeInstruction.StoreLocal(0),
            ]);

            var pos2 = codes.FindIndex(pos, c => c.opcode == OpCodes.Ldloc_S && ((LocalBuilder)c.operand).LocalIndex == 5);
            codes.InsertRange(pos2,
            [
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ExtraGUIFormCaravan), nameof(Instance))),
                CodeInstruction.LoadField(typeof(ExtraGUIFormCaravan), nameof(extraViewRectHeight)),
                new CodeInstruction(OpCodes.Add)
            ]);

            var DoRow = AccessTools.Method(typeof(TransferableOneWayWidget), "DoRow");
            var pos3 = codes.FindIndex(pos2, c => c.opcode == OpCodes.Call && c.operand.Equals(DoRow));
            var label = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            codes[pos3].labels.Add(label);
            codes[pos3 + 1].labels.Add(label2);
            codes.InsertRange(pos3,
            [
                CodeInstruction.LoadLocal(8),
                CodeInstruction.LoadLocal(9),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<TransferableOneWay>), "Item")),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(TransferableOneWay), nameof(TransferableOneWay.ThingDef))),
                CodeInstruction.LoadField(typeof(PB_DefOf), nameof(PB_DefOf.PB_BlueprintPaper)),
                CodeInstruction.Call(typeof(object), nameof(Equals), [typeof(object), typeof(object)]),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                CodeInstruction.LoadLocal(1, true),
                CodeInstruction.LoadArgument(2),
                CodeInstruction.Call(typeof(ExtraGUIFormCaravan), nameof(DoBlueprintGUI)),
                new CodeInstruction(OpCodes.Br_S, label2)
            ]);

            return codes;
        }
    }

    public static void DoBlueprintGUI(TransferableOneWayWidget instance, Rect rect, TransferableOneWay blueprint, int k, float availableMass, ref float curY, ref bool anythingChanged)
    {
        Instance.currentWidget = instance;
        Instance.availableMass = availableMass;
        if (Instance.recacheRequest) ReCacheThresholds();
        if (!Instance.relatedThingList.TryGetValue(blueprint, out var relatedThings) ||
            !Instance.cachedReachedThreshold.TryGetValue(blueprint, out var reachedThresholdDict) ||
            !Instance.cachedThresholds.TryGetValue(blueprint, out var threasholds) ||
            !Instance.cachedMassThresholds.TryGetValue(blueprint, out var massThreasholdDict))
            return;
        
        var collapse = Instance.collapse;
        collapse.TryAdd(blueprint, true);
        var tex = collapse[blueprint] ? TexButton.Reveal : TexButton.Collapse;
        if (Widgets.ButtonImageFitted(rect.LeftPartPixels(30f), tex))
        {
            collapse[blueprint] = !collapse[blueprint];
            var extraHeight = 30f * relatedThings.Count;
            Instance.extraViewRectHeight += collapse[blueprint] ? -extraHeight : extraHeight;
        }

        var indent = 30f;
        var blueprintRect = new Rect(indent, curY, rect.width - indent, 30f);

        indent += 15f;
        foreach (var trad in relatedThings)
        {
            var reached = reachedThresholdDict[trad];
            GUI.color = reached ? Color.green : Color.red;
            if (!collapse[blueprint])
            {
                curY += 30f;
                var widget = WidgetSelect(trad);
                var threshold = threasholds.FirstOrDefault(t => t.thingDef == trad.ThingDef)?.count ?? 0;
                if (trad.CountToTransfer == threshold) threshold = massThreasholdDict[trad];
                var countToTransfer = trad.CountToTransfer;
                ReversePatch_TransferableOneWayWidget_DoRow.DoRow(widget, new Rect(indent, curY, rect.width - indent, 30f), trad, 1, availableMass, threshold);
                if (countToTransfer != trad.CountToTransfer)
                {
                    anythingChanged = true;
                }
            }
        }
        GUI.color = Color.white;
        if (blueprint.CountToTransfer == 0)
        {
            ReversePatch_TransferableOneWayWidget_DoRow.DoRow(instance, blueprintRect, blueprint, k, availableMass, 0);
        }
        else
        {
            GUI.color = reachedThresholdDict[blueprint] ? Color.green : Color.red;
            ReversePatch_TransferableOneWayWidget_DoRow.DoRow(instance, blueprintRect, blueprint, 1, availableMass, 0);
        }
        GUI.color = Color.white;
        Instance.currentWidget = null;
    }

    private static TransferableOneWayWidget WidgetSelect(TransferableOneWay trad)
    {
        var itemTransfer = Instance.itemsTransfer;
        var travelSuppliesTransfer = Instance.travelSuppliesTransfer;
        if (travelSuppliesTransfer == null) return itemTransfer;
        if (itemTransfer == null) return travelSuppliesTransfer;
        if ((int)GetTransferableCategory(null, trad) == 1) return itemTransfer;
        return travelSuppliesTransfer;
    }

    [HarmonyPatch(typeof(TransferableOneWayWidget), "DoRow")]
    public static class ReversePatch_TransferableOneWayWidget_DoRow
    {
        [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
        public static void DoRow(TransferableOneWayWidget instance, Rect rect, TransferableOneWay trad, int index, float availableMass, int threshold)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                var pos = codes.FindIndex(c => c.opcode == OpCodes.Stloc_S && ((LocalBuilder)c.operand).LocalIndex == 4);
                var pos2 = codes.FindIndex(c => c.opcode == OpCodes.Stloc_S && ((LocalBuilder)c.operand).LocalIndex == 5);
                codes.RemoveRange(pos + 1, pos2 - pos - 1);

                codes.Insert(pos + 1, CodeInstruction.LoadArgument(5));
                return codes;
            }

            _ = Transpiler(null);
        }
    }

    [HarmonyPatch]
    public static class Patch_TransferableUIUtility_DrawTransferableInfo_TipSignal
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.FindIncludingInnerTypes<MethodBase>(typeof(TransferableUIUtility), t => t.GetDeclaredMethods().FirstOrDefault(m => m.Name.Contains("<DrawTransferableInfo>")));
        }

        public static void Postfix(ref string __result, Transferable ___localTrad)
        {
            CompBlueprint compBlueprint;
            if ((compBlueprint = ___localTrad.AnyThing.TryGetComp<CompBlueprint>()) != null)
            {
                var source = TransferableUIUtility.ContentSourceDescription(___localTrad.AnyThing);
                __result = __result.Insert(__result.IndexOf(source, StringComparison.Ordinal), compBlueprint.CompTipStringExtra());
            }
        }
    }

    [HarmonyPatch(typeof(Transferable), nameof(Transferable.AdjustTo))]
    public static class Patch_Transferable_AdjustTo
    {
        public static void Prefix(Transferable __instance, int destination, ref bool __state)
        {
            if (Instance == null) return;
            Instance.recacheRequest = true;
            if (Instance.currentWidget == null || __instance is not TransferableOneWay transferableOneWay) return;
            var adjustment = destination - transferableOneWay.CountToTransfer;
            if (adjustment == 0) return;

            if (transferableOneWay.AnyThing?.HasComp<CompBlueprint>() ?? false)
            {
                var relatedThing = Instance.relatedThingList[transferableOneWay];
                if (adjustment > 0)
                {
                    foreach (var transferable in relatedThing)
                    {
                        if (transferable.AnyThing is MinifiedThing minifiedThing)
                        {
                            var count = transferableOneWay.AnyThing.TryGetComp<CompBlueprint>().BuildingLayoutList.Count(b => b.def == minifiedThing.InnerThing.def);
                            if (count != 0)
                            {
                                transferable.AdjustTo(transferable.ClampAmount(count));
                            }
                        }
                        else
                        {
                            var threshold = Instance.cachedThresholds[transferableOneWay].FirstOrDefault(t => t.thingDef == transferable.ThingDef);
                            if (threshold != null)
                            {
                                transferable.AdjustTo(transferable.ClampAmount(threshold.count));
                            }
                        }
                    }
                    Instance.collapse[transferableOneWay] = false;
                }
                else
                {
                    var costList = Instance.totalCostList[transferableOneWay];
                    foreach (var transferable in relatedThing)
                    {
                        if (transferable.AnyThing is MinifiedThing minifiedThing)
                        {
                            var count = transferableOneWay.AnyThing.TryGetComp<CompBlueprint>().BuildingLayoutList.Count(b => b.def == minifiedThing.InnerThing.def);
                            if (count != 0)
                            {
                                transferable.AdjustTo(transferable.ClampAmount(transferable.CountToTransfer - count));
                            }
                        }
                        else
                        {
                            var count = costList.FirstOrDefault(c => c.thingDef == transferable.ThingDef);
                            if (count != null)
                            {
                                transferable.AdjustTo(transferable.ClampAmount(transferable.CountToTransfer - count.count));
                            }
                        }
                    }
                    Instance.collapse[transferableOneWay] = true;
                }
            }

            if (transferableOneWay.AnyThing is MinifiedThing minifiedThing2)
            {
                var minifiedCostList = minifiedThing2.InnerThing.CostListAdjusted();
                foreach (var thresholds in Instance.cachedThresholds)
                {
                    foreach (var cost in minifiedCostList)
                    {
                        var threshold = Instance.cachedThresholds[thresholds.Key].FirstOrDefault(t => t.thingDef == cost.thingDef);
                        threshold?.count -= cost.count * adjustment;
                    }
                }
            }
        }
    }
}
