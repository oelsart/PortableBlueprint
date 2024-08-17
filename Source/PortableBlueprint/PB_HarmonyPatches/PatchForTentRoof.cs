using HarmonyLib;
using PortableBlueprint.Tent;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace PortableBlueprint.PB_HarmonyPatch
{
    [HarmonyPatch(typeof(AutoBuildRoofAreaSetter), "TryGenerateAreaNow")]
    public static class Patch_AutoBuildRoofAreaSetter_TryGenerateAreaNow
    {
        delegate bool Any(IntVec3 c);

        static Any ContainsTentPole(Map map)
        {
            return c => c.GetEdifice(map)?.HasComp<TentPoleComp>() ?? false;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var flag2 = generator.DeclareLocal(typeof(bool));
            codes.InsertRange(0, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stloc_S, flag2)
            });

            var pos = codes.FindIndex(c => c.opcode == OpCodes.Ldfld && c.operand.Equals(AccessTools.Field(typeof(BuildingProperties), "allowAutoroof")));
            pos = codes.FindIndex(pos, c => c.opcode == OpCodes.Bne_Un_S);
            var label = (Label)codes[pos].operand;
            var label2 = generator.DefineLabel();
            codes[pos].operand = label2;
            pos = codes.FindIndex(pos, c => c.opcode == OpCodes.Ldloc_1);
            codes[pos].labels.Add(label);
            codes.InsertRange(pos, new CodeInstruction[] {
                CodeInstruction.LoadLocal(2).WithLabels(label2),
                CodeInstruction.LoadField(typeof(Thing), "def"),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ThingDef), nameof(ThingDef.HasModExtension)).MakeGenericMethod(typeof(TentParts))),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Stloc_S, flag2)
            });

            pos = codes.FindIndex(pos, c => c.opcode == OpCodes.Ldfld && c.operand.Equals(AccessTools.Field(typeof(AutoBuildRoofAreaSetter), "cellsToRoof"))) - 1;
            var pos2 = codes.FindLastIndex(pos, c => c.opcode == OpCodes.Leave_S);
            var label3 = (Label)codes[pos2].operand;
            var label4 = generator.DefineLabel();
            codes[pos].labels.Add(label4);
            codes.InsertRange(pos, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_S, flag2).WithLabels(label3),
                new CodeInstruction(OpCodes.Brfalse_S, label4),
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadField(typeof(AutoBuildRoofAreaSetter), "innerCells"),
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadField(typeof(AutoBuildRoofAreaSetter), "map"),
                CodeInstruction.Call(typeof(Patch_AutoBuildRoofAreaSetter_TryGenerateAreaNow), "MakeTentRoofArea"),
                new CodeInstruction(OpCodes.Ret)
            });
            return codes;
        }

        public static void MakeTentRoofArea(HashSet<IntVec3> innerCells, Map map)
        {
            var cellsToRoof = new HashSet<IntVec3>();
            foreach (IntVec3 a in innerCells)
            {
                for (int l = 0; l < 9; l++)
                {
                    IntVec3 intVec2 = a + GenAdj.AdjacentCellsAndInside[l];
                    if (intVec2.InBounds(map) && (l == 8 || intVec2.GetTentRoofHolder(map) != null))
                    {
                        cellsToRoof.Add(intVec2);
                    }
                }
            }

            var justRoofedCells = new List<IntVec3>();
            foreach (IntVec3 intVec3 in cellsToRoof)
            {
                if (map.roofGrid.RoofAt(intVec3) == null && !justRoofedCells.Contains(intVec3) && !map.areaManager.Get<Area_NoTentRoof>()[intVec3] && TentRoofUtility.WithinRangeOfRoofHolder(intVec3, map, true))
                {
                    map.areaManager.Get<Area_BuildTentRoof>()[intVec3] = true;
                    justRoofedCells.Add(intVec3);
                }
            }
        }
    }

    [HarmonyPatch(typeof(AreaManager), "AddStartingAreas")]
    public static class Patch_AreaManager_AddStartingAreas
    {
        public static void Postfix(List<Area> ___areas, AreaManager __instance)
        {
            ___areas.Add(new Area_BuildTentRoof(__instance));
            ___areas.Add(new Area_NoTentRoof(__instance));
        }
    }

    [HarmonyPatch(typeof(JobDriver_RemoveRoof), "MakeNewToils")]
    public static class Patch_JobDriver_RemoveRoof_MakeNewToils
    {
        public static void Prefix(JobDriver_RemoveRoof __instance)
        {
            __instance.FailOn(() => __instance.pawn.MapHeld.areaManager.Get<Area_NoTentRoof>()[__instance.job.targetA.Cell]);
        }
    }

    [HarmonyPatch(typeof(WorkGiver_RemoveRoof), "HasJobOnCell")]
    public static class Patch_JobDriver_WorkGiver_RemoveRoof_HasJobOnCell
    {
        public static void Postfix(ref bool __result, Pawn pawn, IntVec3 c)
        {
            __result = __result && c.GetRoof(pawn.Map) != PB_DefOf.PB_TentRoof;
        }
    }
}
