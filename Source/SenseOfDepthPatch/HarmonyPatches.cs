using HarmonyLib;
using SenseOfDepthForTallBuilding;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace PortableBlueprint.SenseOfDepthPatch
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.harmony.rimworld.portableblueprint.senseofdepthpatch");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(Building_DrawingTable), "PrintWithOverrideRotFlip")]
    public static class Patch_Building_DrawingTable_PrintWithOverrideRotFlip
    {
        public static void Postfix(SectionLayer layer, Building_DrawingTable __instance, Rot4 overrideRot, bool overrideFlipFlag)
        {
            var comp = __instance.TryGetComp<CompBackSideLayer>();
            if (comp != null && comp.props != null && comp.BSLDrawNow)
            {
                ReversePatch_BackSideLayerGraphics_PrintBackSideLayer.PrintBackSideLayer(layer, __instance, comp.Altitude, null, overrideRot, overrideFlipFlag);
            }
        }
    }

    [HarmonyPatch(typeof(BackSideLayerGraphics), nameof(BackSideLayerGraphics.PrintBackSideLayer))]
    public static class ReversePatch_BackSideLayerGraphics_PrintBackSideLayer
    {
        [HarmonyReversePatch]
        public static void PrintBackSideLayer(SectionLayer layer, Thing thing, float altitude, Graphic subGraphic, Rot4 overrideRot, bool overrideFlipFlag)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var codes = instructions.ToList();
                var pos = codes.FindIndex(c => c.opcode == OpCodes.Callvirt && c.operand.Equals(AccessTools.Method(typeof(Graphic), nameof(Graphic.MatAt)))) - 1;

                codes.InsertRange(pos, new CodeInstruction[]
                {
                    CodeInstruction.LoadArgument(4),
                    CodeInstruction.LoadArgument(5),
                    CodeInstruction.StoreLocal(3)
                });
                codes.RemoveAt(pos - 1);
                return codes;
            }
            _ = Transpiler(null, null);
        }
    }
}
