using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace CarryableBlueprint.CB_HarmonyPatch
{
    [HarmonyPatch]
    public static class Patch_Toils_Haul_StartCarryThing
    {
        static MethodInfo TargetMethod()
        {
            Type type = AccessTools.FirstInner(typeof(Toils_Haul), t => t.GetFields().Select(f => f.FieldType)
            .SequenceEqual(new Type[] { typeof(Toil), typeof(TargetIndex), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) }));
            return AccessTools.FirstMethod(type, m => m.Name.Contains("StartCarryThing"));
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Ldloc_S && (c.operand as LocalBuilder).LocalIndex == 4);
            var label = generator.DefineLabel();
            var minifiedStackable = generator.DeclareLocal(typeof(MinifiedThing_Stackable));
            var target = generator.DeclareLocal(typeof(LocalTargetInfo));
            var blueprint = generator.DeclareLocal(typeof(Blueprint_Install));
            codes.InsertRange(pos, new CodeInstruction[]
            {
                CodeInstruction.LoadLocal(2),
                new CodeInstruction(OpCodes.Isinst, typeof(MinifiedThing_Stackable)),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Stloc_S, minifiedStackable),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                CodeInstruction.StoreLocal(4),
                new CodeInstruction(OpCodes.Ldloc_S, minifiedStackable),
                CodeInstruction.LoadField(typeof(MinifiedThing_Stackable), "blueprintsForDrawLine"),
                CodeInstruction.LoadLocal(1),
                new CodeInstruction(OpCodes.Ldc_I4_2),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Job), "GetTarget")),
                new CodeInstruction(OpCodes.Stloc_S, target),
                new CodeInstruction(OpCodes.Ldloca_S, target),
                CodeInstruction.Call(typeof(LocalTargetInfo), "get_Thing"),
                new CodeInstruction(OpCodes.Isinst, typeof(Blueprint_Install)),
                new CodeInstruction(OpCodes.Stloc_S, blueprint),
                new CodeInstruction(OpCodes.Ldloc_S, blueprint),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ldloc_S, blueprint),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(HashSet<Blueprint_Install>), "Remove")),
                new CodeInstruction(OpCodes.Pop).WithLabels(label)
            });

            return codes;
        }
    }

    [HarmonyPatch(typeof(JobDriver_HaulToContainer), "MakeNewToils")]
    public static class Patch_JobDriver_HaulToContainer_MakeNewToils
    {
         public static void Postfix(ref IEnumerable<Toil> __result)
        {
            var pos = __result.FirstIndexOf(t => t.debugName == "JumpIfAlsoCollectingNextTargetInQueue");
            var list = __result.ToList();
            list.Insert(pos, SetSplittedThingToInstall(TargetIndex.A, TargetIndex.B));
            __result = list;
        }

        public static Toil SetSplittedThingToInstall(TargetIndex haulableInd, TargetIndex containerInt)
        {
            Toil toil = ToilMaker.MakeToil("SetSplittedThingToMiniToInstall");
            toil.initAction = () =>
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Thing thing = curJob.GetTarget(containerInt).Thing;
                Blueprint_Install blueprint = thing as Blueprint_Install;
                if (blueprint == null) return;
                Thing carriedThing = actor.carryTracker.CarriedThing;

                MinifiedThing_Stackable minifiedThing = carriedThing as MinifiedThing_Stackable;
                if (minifiedThing == null) return;

                SetThingToInstallFromMinified(blueprint, minifiedThing);
                minifiedThing.blueprintsForDrawLine.Add(blueprint);
            };
            return toil;
        }

        private static readonly FastInvokeHandler SetThingToInstallFromMinified = MethodInvoker.GetHandler(AccessTools.Method(typeof(Blueprint_Install), "SetThingToInstallFromMinified"));
    }

    [HarmonyPatch(typeof(ListerBuildings), "RegisterInstallBlueprint")]
    public static class Patch_ListerBuildings_RegisterInstallBlueprint
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var label = generator.DefineLabel();
            codes[0].labels.Add(label);
            codes.InsertRange(0, new CodeInstruction[]
            {
                CodeInstruction.LoadArgument(1),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Blueprint_Install), "MiniToInstallOrBuildingToReinstall")),
                new CodeInstruction(OpCodes.Isinst, typeof(MinifiedThing_Stackable)),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ret)
            });

            return codes;
        }
    }

    [HarmonyPatch(typeof(ListerBuildings), "DeregisterInstallBlueprint")]
    public static class Patch_ListerBuildings_DeregisterInstallBlueprint
    {
        public static void Postfix(Blueprint_Install blueprint)
        {
            var minifiedStackable = blueprint.MiniToInstallOrBuildingToReinstall as MinifiedThing_Stackable;
            if (minifiedStackable != null)
            {
                minifiedStackable.blueprintsForDrawLine.Remove(blueprint);
            }
        }
    }

    [HarmonyPatch(typeof(MinifyUtility), "Uninstall")]
    public static class Patch_MinifyUtility_Uninstall
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var pos = codes.FindLastIndex(c => c.opcode == OpCodes.Brfalse_S);
            var label = codes[pos].operand;
            codes.InsertRange(pos + 1, new CodeInstruction[] {
                CodeInstruction.LoadLocal(1),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.Destroyed))),
                new CodeInstruction(OpCodes.Brtrue_S, label),
            });
            return codes;
        }
    }
}