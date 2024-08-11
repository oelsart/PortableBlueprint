using CarryableBlueprint.Tent;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CarryableBlueprint.CB_HarmonyPatch
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.harmony.rimworld.carryableblueprint");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Patch_PlayDataLoader_HotReloadDefs.Postfix();
        }
    }

    [HarmonyPatch(typeof(GraphicUtility), "WrapLinked")]    
    public static class Patch_GraphicUtility_WrapLinked
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();
            int pos = codes.FindIndex(c => c.opcode == OpCodes.Switch);
            var switchLabels = codes[pos].operand as Label[];
            Patch_GraphicUtility_WrapLinked.linkType = (LinkDrawerType)Convert.ToByte(switchLabels.Count());
            var label = generator.DefineLabel();
            codes[pos].operand = switchLabels.AddToArray(label);
            pos = codes.FindIndex(c => c.opcode == OpCodes.Newobj && c.operand.Equals(AccessTools.Constructor(typeof(ArgumentException))));
            codes.InsertRange(pos, new CodeInstruction[]
            {
            CodeInstruction.LoadArgument(0).WithLabels(label),
            new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Graphic_LinkedTent), new Type[] { typeof(Graphic) })),
            new CodeInstruction(OpCodes.Ret)
            });

            return codes;
        }

        public static LinkDrawerType linkType;
    }

    /*[HarmonyPatch(typeof(GraphicData), "Init")]
    public static class Patch_GraphicData_Init
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();
            int pos = codes.FindIndex(c => c.opcode == OpCodes.Call && c.operand.Equals(AccessTools.PropertyGetter(typeof(GraphicData), "Linked")));
            var labelBr = codes[pos + 1].operand;
            var labelFalse = generator.DefineLabel();
            codes.InsertRange(pos, new CodeInstruction[]
            {
                CodeInstruction.LoadField(typeof(GraphicData), "cachedGraphic"),
                new CodeInstruction(OpCodes.Isinst, typeof(Graphic_Tent)),
                new CodeInstruction(OpCodes.Brfalse_S, labelFalse),
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadField(typeof(GraphicData), "cachedGraphic"),
                new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Graphic_LinkedTent), new Type[] { typeof(Graphic) })),
                CodeInstruction.StoreField(typeof(GraphicData), "cachedGraphic"),
                new CodeInstruction(OpCodes.Br_S, labelBr),
                CodeInstruction.LoadArgument(0).WithLabels(labelFalse)
            });

            return codes;
        }
    }*/

    [HarmonyPatch(typeof(PlayDataLoader), "HotReloadDefs")]
    public static class Patch_PlayDataLoader_HotReloadDefs
    {
        public static void Postfix()
        {
            LongEventHandler.QueueLongEvent(() =>
            {
                CB_DefOf.CB_TentWall.graphicData.CopyFrom(CB_DefOf.CB_TentWall.graphicData);
                CB_DefOf.CB_TentWall.graphicData.linkType = Patch_GraphicUtility_WrapLinked.linkType;
                CB_DefOf.CB_TentWall.PostLoad();
                var layersToRegenerate = new HashSet<SectionLayer>();
                Find.Maps?.ForEach(m =>
                {
                    m.listerThings.ThingsOfDef(CB_DefOf.CB_TentWall)?.ForEach(t =>
                    {
                        t.Notify_DefsHotReloaded();
                        layersToRegenerate.Add(m.mapDrawer.SectionAt(t.Position).GetLayer(typeof(SectionLayer_ThingsGeneral)));
                    });
                });
                foreach (var layer in layersToRegenerate)
                {
                    layer.Regenerate();
                }
            }, "", false, null, false, null);
        }
    }
}