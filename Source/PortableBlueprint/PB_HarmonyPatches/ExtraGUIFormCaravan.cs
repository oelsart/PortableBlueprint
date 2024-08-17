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

namespace PortableBlueprint.PB_HarmonyPatches
{
    public class ExtraGUIFormCaravan
    {
        private Dictionary<TransferableOneWay, IEnumerable<ThingDefCountClass>> totalCostList = new Dictionary<TransferableOneWay, IEnumerable<ThingDefCountClass>>();

        private Dictionary<TransferableOneWay, List<TransferableOneWay>> relatedThingList = new Dictionary<TransferableOneWay, List<TransferableOneWay>>();

        private Dictionary<TransferableOneWay, bool> collapse = new Dictionary<TransferableOneWay, bool>();

        private Dictionary<TransferableOneWay, Dictionary<TransferableOneWay, int>> cachedThresholds = new Dictionary<TransferableOneWay, Dictionary<TransferableOneWay, int>>();

        private Dictionary<TransferableOneWay, Dictionary<TransferableOneWay, int>> cachedMassThresholds = new Dictionary<TransferableOneWay, Dictionary<TransferableOneWay, int>>();

        private Dictionary<TransferableOneWay, Dictionary<TransferableOneWay, bool>> cachedReachedThreshold = new Dictionary<TransferableOneWay, Dictionary<TransferableOneWay, bool>>();

        private List<TransferableOneWay> transferables;

        private TransferableOneWayWidget itemsTransfer;

        private TransferableOneWayWidget travelSuppliesTransfer;

        private TransferableOneWayWidget currentWidget;

        private float availableMass;

        private float extraViewRectHeight = 0f;

        private static ExtraGUIFormCaravan instance;

        private static FastInvokeHandler GetMass = MethodInvoker.GetHandler(AccessTools.Method(typeof(TransferableOneWayWidget), "GetMass"));

        private static FastInvokeHandler GetTransferableCategory = MethodInvoker.GetHandler(AccessTools.Method(typeof(CaravanUIUtility), "GetTransferableCategory"));

        [HarmonyPatch(typeof(CaravanUIUtility), "CreateCaravanTransferableWidgets")]
        public static class Patch_CaravanUIUtility_CreateCaravanTransferableWidgets
        {
            public static void Postfix(List<TransferableOneWay> transferables, TransferableOneWayWidget itemsTransfer, TransferableOneWayWidget travelSuppliesTransfer, Func<float> availableMassGetter)
            {
                ExtraGUIFormCaravan.instance = new ExtraGUIFormCaravan
                {
                    transferables = transferables,
                    itemsTransfer = itemsTransfer,
                    travelSuppliesTransfer = travelSuppliesTransfer
                };
                if (itemsTransfer == null && travelSuppliesTransfer == null)
                {
                    ExtraGUIFormCaravan.instance.itemsTransfer = new TransferableOneWayWidget(transferables, null, null, null, false, IgnorePawnsInventoryMode.Ignore, false, availableMassGetter);
                }
                Log.Message($"{itemsTransfer}, {travelSuppliesTransfer}, {transferables.Count}");

                bool _ = false;
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
                    }).OrderBy(m => m.AnyThing.LabelNoParenthesis);
                    ExtraGUIFormCaravan.instance.relatedThingList[blueprint] = minifiedList.ConcatIfNotNull(tradList).ToList();
                    ExtraGUIFormCaravan.instance.totalCostList[blueprint] = totalCost;

                    ExtraGUIFormCaravan.instance.availableMass = (availableMassGetter != null) ? availableMassGetter() : float.MaxValue;
                    ExtraGUIFormCaravan.instance.cachedReachedThreshold[blueprint] = new Dictionary<TransferableOneWay, bool>();
                    ExtraGUIFormCaravan.instance.cachedThresholds[blueprint] = new Dictionary<TransferableOneWay, int>();
                    ExtraGUIFormCaravan.instance.cachedMassThresholds[blueprint] = new Dictionary<TransferableOneWay, int>();
                    foreach (var trad in ExtraGUIFormCaravan.instance.relatedThingList[blueprint])
                    {
                        ExtraGUIFormCaravan.instance.cachedThresholds[blueprint][trad] = ExtraGUIFormCaravan.GetThreshold(blueprint, trad);
                    }
                }

                ExtraGUIFormCaravan.ReCacheThresholds();
            }
        }

        private static void ReCacheThresholds()
        {
            foreach (var blueprint in ExtraGUIFormCaravan.instance.relatedThingList.Select(t => t.Key))
            {
                ExtraGUIFormCaravan.instance.cachedReachedThreshold[blueprint][blueprint] = ReachedThreshold(null, blueprint);
                foreach (var trad in ExtraGUIFormCaravan.instance.relatedThingList[blueprint])
                {
                    var widget = ExtraGUIFormCaravan.WidgetSelect(trad);
                    ExtraGUIFormCaravan.instance.cachedReachedThreshold[blueprint][trad] = ReachedThreshold(blueprint, trad);
                    float num = ExtraGUIFormCaravan.instance.availableMass + (float)GetMass(widget, trad.AnyThing) * (float)trad.CountToTransfer;
                    var massThreshold = (num <= 0f) ? 0 : Mathf.FloorToInt(num / (float)GetMass(widget, trad.AnyThing));
                    ExtraGUIFormCaravan.instance.cachedMassThresholds[blueprint][trad] = massThreshold;
                }
            }
        }

        [HarmonyPatch(typeof(Dialog_LoadTransporters), "CalculateAndRecacheTransferables")]
        public static class Patch_Dialog_LoadTransporters_CalculateAndRecacheTransferables
        {
            public static void Postfix(List<TransferableOneWay> ___transferables, TransferableOneWayWidget ___itemsTransfer) => Patch_CaravanUIUtility_CreateCaravanTransferableWidgets.Postfix(___transferables, ___itemsTransfer, null, null);
        }

        [HarmonyPatch(typeof(TransferableOneWayWidget), "FillMainRect")]
        public static class Patch_TransferableOneWayWidget_FillMainRect
        {
            [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
            public static void Original(TransferableOneWayWidget instance, Rect mainRect, out bool anythingChanged) => throw new NotImplementedException();

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var codes = instructions.ToList();
                var label0 = generator.DefineLabel();
                codes[0].labels.Add(label0);
                codes.InsertRange(0, new[]
                {
                    CodeInstruction.LoadField(typeof(ExtraGUIFormCaravan), nameof(ExtraGUIFormCaravan.instance)),
                    new CodeInstruction(OpCodes.Brtrue_S, label0),
                    CodeInstruction.LoadArgument(0),
                    CodeInstruction.LoadArgument(1),
                    CodeInstruction.LoadArgument(2, true),
                    CodeInstruction.Call(typeof(Patch_TransferableOneWayWidget_FillMainRect), "Original"),
                    new CodeInstruction(OpCodes.Ret)
                });

                var pos = codes.FindIndex(c => c.opcode == OpCodes.Blt_S) + 1;
                codes.InsertRange(pos, new []
                {
                    CodeInstruction.LoadLocal(0),
                    CodeInstruction.LoadField(typeof(ExtraGUIFormCaravan), nameof(ExtraGUIFormCaravan.instance)),
                    CodeInstruction.LoadField(typeof(ExtraGUIFormCaravan), nameof(ExtraGUIFormCaravan.extraViewRectHeight)),
                    new CodeInstruction(OpCodes.Add),
                    CodeInstruction.StoreLocal(0),
                });

                var pos2 = codes.FindIndex(pos, c => c.opcode == OpCodes.Ldloc_S && (c.operand as LocalBuilder).LocalIndex == 5);
                codes.InsertRange(pos2, new []
                {
                    CodeInstruction.LoadField(typeof(ExtraGUIFormCaravan), nameof(ExtraGUIFormCaravan.instance)),
                    CodeInstruction.LoadField(typeof(ExtraGUIFormCaravan), nameof(ExtraGUIFormCaravan.extraViewRectHeight)),
                    new CodeInstruction(OpCodes.Add)
                });

                var DoRow = AccessTools.Method(typeof(TransferableOneWayWidget), "DoRow");
                var pos3 = codes.FindIndex(pos2, c => c.opcode == OpCodes.Call && c.operand.Equals(DoRow));
                var label = generator.DefineLabel();
                var label2 = generator.DefineLabel();
                codes[pos3].labels.Add(label);
                codes[pos3 + 1].labels.Add(label2);
                codes.InsertRange(pos3, new []
                {
                    CodeInstruction.LoadLocal(8),
                    CodeInstruction.LoadLocal(9),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<TransferableOneWay>), "Item")),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(TransferableOneWay), nameof(TransferableOneWay.ThingDef))),
                    CodeInstruction.LoadField(typeof(PB_DefOf), nameof(PB_DefOf.PB_Blueprint)),
                    CodeInstruction.Call(typeof(object), nameof(Equals), new Type[] { typeof(object), typeof(object) }),
                    new CodeInstruction(OpCodes.Brfalse_S, label),
                    CodeInstruction.LoadLocal(1, true),
                    CodeInstruction.LoadArgument(2),
                    CodeInstruction.Call(typeof(ExtraGUIFormCaravan), nameof(ExtraGUIFormCaravan.DoBlueprintGUI)),
                    new CodeInstruction(OpCodes.Br_S, label2)
                });

                return codes;
            }
        }

        public static void DoBlueprintGUI(TransferableOneWayWidget instance, Rect rect, TransferableOneWay blueprint, int k, float availableMass, ref float curY, ref bool anythingChanged)
        {
            ExtraGUIFormCaravan.instance.currentWidget = instance;
            ExtraGUIFormCaravan.instance.availableMass = availableMass;
            var collapse = ExtraGUIFormCaravan.instance.collapse;
            if (!collapse.ContainsKey(blueprint))
            {
                collapse[blueprint] = true;
            }
            Texture2D tex = collapse[blueprint] ? TexButton.Reveal : TexButton.Collapse;
            if (Widgets.ButtonImageFitted(rect.LeftPartPixels(30f), tex))
            {
                collapse[blueprint] = !collapse[blueprint];
                var extraHeight = 30f * ExtraGUIFormCaravan.instance.relatedThingList[blueprint].Count;
                ExtraGUIFormCaravan.instance.extraViewRectHeight += collapse[blueprint] ? -extraHeight : extraHeight;
            }

            var indent = 30f;
            var blueprintRect = new Rect(indent, curY, rect.width - indent, 30f);

            indent += 15f;
            var relatedThings = ExtraGUIFormCaravan.instance.relatedThingList[blueprint];
            var allReached = true;
            foreach (var trad in relatedThings)
            {
                var reached = ExtraGUIFormCaravan.instance.cachedReachedThreshold[blueprint][trad];
                if (reached) {
                    GUI.color = Color.green;
                }
                else
                {
                    GUI.color = Color.red;
                    allReached = false;
                }
                if (!collapse[blueprint])
                {
                    curY += 30f;
                    var widget = ExtraGUIFormCaravan.WidgetSelect(trad);
                    var threshold = ExtraGUIFormCaravan.instance.cachedThresholds[blueprint][trad];
                    if (trad.CountToTransfer == threshold) threshold = ExtraGUIFormCaravan.instance.cachedMassThresholds[blueprint][trad];
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
                GUI.color = allReached ? Color.green : Color.red;
                ReversePatch_TransferableOneWayWidget_DoRow.DoRow(instance, blueprintRect, blueprint, 1, availableMass, 0);
            }
            GUI.color = Color.white;
            ExtraGUIFormCaravan.instance.currentWidget = null;
        }

        public static int GetThreshold(TransferableOneWay blueprint, TransferableOneWay trad)
        {
            MinifiedThing minifiedThing;
            var costList = ExtraGUIFormCaravan.instance.totalCostList[blueprint];
            if ((minifiedThing = trad.AnyThing as MinifiedThing) != null)
            {
                return blueprint.AnyThing.TryGetComp<CompBlueprint>().BuildingLayoutList.Where(b => b.def == minifiedThing.InnerThing.def).Count();
            }
            return costList.First(c => c.thingDef == trad.ThingDef).count;
        }

        private static bool ReachedThreshold(TransferableOneWay blueprint, TransferableOneWay trad)
        {
            if (trad.AnyThing == null) return false;

            if (trad.AnyThing.HasComp<CompBlueprint>())
            {
                return ExtraGUIFormCaravan.instance.relatedThingList[trad].All(t => ReachedThreshold(trad, t));
            }

            var minifiedThing = trad.AnyThing as MinifiedThing;
            if (minifiedThing != null)
            {
                return minifiedThing.InnerThing.CostListAdjusted().All(c =>
                {
                    var trad2 = ExtraGUIFormCaravan.instance.transferables.FirstOrDefault(t => c.thingDef == t.ThingDef);
                    return trad2 != null && ReachedThreshold(blueprint, trad2);
                }) || trad.CountToTransfer >= ExtraGUIFormCaravan.instance.cachedThresholds[blueprint][trad];
            }
            return trad.CountToTransfer >= ExtraGUIFormCaravan.instance.cachedThresholds[blueprint][trad];
        }

        private static TransferableOneWayWidget WidgetSelect(TransferableOneWay trad)
        {
            var itemTransfer = ExtraGUIFormCaravan.instance.itemsTransfer;
            var travelSuppliesTransfer = ExtraGUIFormCaravan.instance.travelSuppliesTransfer;
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
                    var pos = codes.FindIndex(c => c.opcode == OpCodes.Stloc_S && (c.operand as LocalBuilder).LocalIndex == 4);
                    var pos2 = codes.FindIndex(c => c.opcode == OpCodes.Stloc_S && (c.operand as LocalBuilder).LocalIndex == 5);
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
            static MethodInfo TargetMethod()
            {
                var type = AccessTools.Inner(typeof(TransferableUIUtility), "<>c__DisplayClass26_0");
                return AccessTools.Method(type, "<DrawTransferableInfo>b__0");
            }

            public static void Postfix(ref string __result, Transferable ___localTrad)
            {
                CompBlueprint compBlueprint;
                if ((compBlueprint = ___localTrad.AnyThing.TryGetComp<CompBlueprint>()) != null)
                {
                    var source = TransferableUIUtility.ContentSourceDescription(___localTrad.AnyThing);
                    __result = __result.Insert(__result.IndexOf(source), compBlueprint.CompTipStringExtra());
                }
            }
        }

        [HarmonyPatch(typeof(Transferable), nameof(Transferable.AdjustTo))]
        public static class Patch_Transferable_AdjustTo
        {
            public static void Prefix(Transferable __instance, int destination)
            {
                var transferableOneWay = __instance as TransferableOneWay;
                if (ExtraGUIFormCaravan.instance == null || ExtraGUIFormCaravan.instance.currentWidget == null || transferableOneWay == null) return;
                var adjustment = destination - transferableOneWay.CountToTransfer;
                if (adjustment == 0) return;

                if (transferableOneWay.AnyThing?.HasComp<CompBlueprint>() ?? false)
                {
                    var relatedThing = ExtraGUIFormCaravan.instance.relatedThingList[transferableOneWay];
                    if (adjustment > 0)
                    {
                        foreach (var transferable in relatedThing)
                        {
                            var threshold = ExtraGUIFormCaravan.instance.cachedThresholds[transferableOneWay][transferable];
                            transferable.AdjustTo(transferable.ClampAmount(threshold));
                        }
                        ExtraGUIFormCaravan.instance.collapse[transferableOneWay] = false;
                    }
                    else
                    {
                        var costList = ExtraGUIFormCaravan.instance.totalCostList[transferableOneWay];
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
                        ExtraGUIFormCaravan.instance.collapse[transferableOneWay] = true;
                    }
                }

                if (transferableOneWay.AnyThing is MinifiedThing minifiedThing2)
                {
                    var minifiedCostList = minifiedThing2.InnerThing.CostListAdjusted();
                    foreach (var thresholds in ExtraGUIFormCaravan.instance.cachedThresholds)
                    {
                        foreach (var cost in minifiedCostList)
                        {
                            var trad = thresholds.Value.Select(t => t.Key).FirstOrDefault(t => t.ThingDef == cost.thingDef);
                            if (trad != null)
                            {
                                ExtraGUIFormCaravan.instance.cachedThresholds[thresholds.Key][trad] -= cost.count * adjustment;
                            }
                        }
                    }
                }
            }

            public static void Postfix(Transferable __instance)
            {
                var transferableOneWay = __instance as TransferableOneWay;
                if (ExtraGUIFormCaravan.instance == null || ExtraGUIFormCaravan.instance.currentWidget == null || transferableOneWay == null) return;
                ExtraGUIFormCaravan.ReCacheThresholds();
            }
        }

        [HarmonyPatch(typeof(Window), nameof(Window.PostClose))]
        public static class Patch_Window_PostClose
        {
            public static void Postfix() => ExtraGUIFormCaravan.instance = null;
        }
    }
}
