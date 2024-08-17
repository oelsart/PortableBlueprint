using HarmonyLib;
using PortableBlueprint.Tent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace PortableBlueprint.PB_HarmonyPatches
{
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
}
