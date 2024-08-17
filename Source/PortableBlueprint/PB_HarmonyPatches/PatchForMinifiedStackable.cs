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

namespace PortableBlueprint.PB_HarmonyPatch
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

    [HarmonyAfter("Uuugggg.rimworld.Build_From_Inventory.main")]
    [HarmonyPatch(typeof(JobDriver_HaulToContainer), "MakeNewToils")]
    public static class Patch_JobDriver_HaulToContainer_MakeNewToils
    {
         public static void Postfix(ref IEnumerable<Toil> __result)
        {
            var pos = __result.FirstIndexOf(t => t.debugName == "JumpIfAlsoCollectingNextTargetInQueue");
            var list = __result.ToList();
            list.Insert(pos, SetSplittedThingToInstall(TargetIndex.B));

            if (ModsConfig.IsActive("uuugggg.buildfrominventory"))
            {
                list.Insert(1, SetSplittedThingToInstall(TargetIndex.A, TargetIndex.B));
            }
            __result = list;
        }

        public static Toil SetSplittedThingToInstall(TargetIndex haulableInd)
        {
            Toil toil = ToilMaker.MakeToil("SetSplittedThingToInstallA");
            toil.initAction = () =>
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Thing thing = curJob.GetTarget(haulableInd).Thing;
                if (!(thing is Blueprint_Install blueprint)) return;
                Thing carriedThing = actor.carryTracker.CarriedThing;

                if (!(carriedThing is MinifiedThing_Stackable minifiedThing)) return;
                MinifiedThing_Stackable.SetThingToInstallFromMinified(blueprint, minifiedThing);
            };
            return toil;
        }

        public static Toil SetSplittedThingToInstall(TargetIndex haulableInd, TargetIndex blueprintInd)
        {
            Toil toil = ToilMaker.MakeToil("SetSplittedThingToInstallB");
            toil.initAction = () =>
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Thing thing = curJob.GetTarget(blueprintInd).Thing;
                if (!(thing is Blueprint_Install blueprint)) return;
                Thing thingToInstall = curJob.GetTarget(haulableInd).Thing;

                if (!(thingToInstall is MinifiedThing_Stackable minifiedThing)) return;
                MinifiedThing_Stackable.SetThingToInstallFromMinified(blueprint, minifiedThing);
            };
            return toil;
        }
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
            codes.InsertRange(pos + 1, new [] {
                CodeInstruction.LoadLocal(1),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.Destroyed))),
                new CodeInstruction(OpCodes.Brtrue_S, label),
            });
            return codes;
        }
    }

    /*[HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "InstallJob")]
    public static class Patch_WorkGiver_ConstructDeliverResources_InstallJob
    {
        public static void Postfix(WorkGiver_ConstructDeliverResources __instance, Pawn pawn, Blueprint_Install install, ref Job __result)
        {
            if (!(install.MiniToInstallOrBuildingToReinstall is MinifiedThing_Stackable minifiedStack)) return;
            if (!ThingsAvailableAnywhere(minifiedStack, pawn))
            {
                missingResources.Add(minifiedStack);
                if (FloatMenuMakerMap.makingFor != pawn)
                {
                    MissingMaterialsMessage(pawn);
                }
            }
            else
            {
                Thing foundRes;
                if (CanUseCarriedResource(pawn, minifiedStack))
                {
                    foundRes = pawn.carryTracker.CarriedThing;
                }
                else
                {
                    foundRes = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(minifiedStack.def), PathEndMode.ClosestTouch,
                        TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false), 9999f,
                        (Thing r) => ResourceValidator(pawn, minifiedStack, r), null, 0, -1,
                        false, RegionType.Set_Passable, false);
                }
                if (foundRes == null)
                {
                    missingResources.Add(minifiedStack);
                    if (FloatMenuMakerMap.makingFor != pawn)
                    {
                        MissingMaterialsMessage(pawn);
                    }
                }
                else
                {
                    FindAvailableNearbyResources(foundRes, pawn, out int num2);
                    var num3 = 1;
                    HashSet<Thing> hashSet = new HashSet<Thing>();//FindNearbyNeeders(pawn, install, num2, out int num3, out _);
                    hashSet.Add(install);
                    Thing thing;
                    if (hashSet.Count > 0)
                    {
                        thing = hashSet.MinBy((Thing needer) => IntVec3Utility.ManhattanDistanceFlat(foundRes.Position, needer.Position));
                        hashSet.Remove(thing);
                    }
                    else
                    {
                        thing = install;
                    }
                    int num4 = 0;
                    int i = 0;
                    do
                    {
                        num4 += resourcesAvailable[i].stackCount;
                        num4 = Mathf.Min(num4, Mathf.Min(num2, num3));
                        i++;
                    }
                    while (num4 < num3 && num4 < num2 && i < resourcesAvailable.Count);
                    resourcesAvailable.RemoveRange(i, resourcesAvailable.Count - i);
                    resourcesAvailable.Remove(foundRes);
                    Job job2 = JobMaker.MakeJob(JobDefOf.HaulToContainer);
                    job2.targetA = foundRes;
                    job2.targetQueueA = new List<LocalTargetInfo>();
                    for (i = 0; i < resourcesAvailable.Count; i++)
                    {
                        job2.targetQueueA.Add(resourcesAvailable[i]);
                    }
                    job2.targetC = install;
                    job2.targetB = thing;
                    if (hashSet.Count > 0)
                    {
                        job2.targetQueueB = new List<LocalTargetInfo>();
                        foreach (Thing t in hashSet)
                        {
                            job2.targetQueueB.Add(t);
                        }
                    }
                    job2.count = num4;
                    job2.haulMode = HaulMode.ToContainer;
                    Log.Message(job2.count);
                    __result = job2;
                    return;
                }
            }
        }

        private static void MissingMaterialsMessage(Pawn pawn)
        {
            if (missingResources.Count > 0 && FloatMenuMakerMap.makingFor == pawn)
            {
                JobFailReason.Is("MissingMaterials".Translate((from kvp in missingResources
                                                               select string.Format("{0}", kvp.Label)).ToCommaList(false, false)), null);
            }
        }

        private static bool ThingsAvailableAnywhere(MinifiedThing_Stackable minifiedStack, Pawn pawn)
        {
            int key = Gen.HashCombine<Faction>(minifiedStack.GetHashCode(), pawn.Faction);
            if (!cachedResults.TryGetValue(key, out bool flag))
            {
                IEnumerable<MinifiedThing_Stackable> list = pawn.Map.listerThings.GetThingsOfType<MinifiedThing_Stackable>().Where(m => m.CanStackWith(minifiedStack));
                foreach (var foundMinified in list)
                {
                    if (!foundMinified.IsForbidden(pawn))
                    {
                        if (foundMinified.stackCount >= 1)
                        {
                            cachedResults.Add(key, flag);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static bool CanUseCarriedResource(Pawn pawn, MinifiedThing_Stackable m)
        {
            Thing carriedThing = pawn.carryTracker.CarriedThing;
            if (((carriedThing != null) ? carriedThing.GetInnerIfMinified().def : null) != m.InnerThing.def)
            {
                return false;
            }
            if (!KeyBindingDefOf.QueueOrder.IsDownEvent)
            {
                return true;
            }
            if (pawn.CurJob != null && !IsValidJob(pawn.CurJob, pawn))
			{
                return false;
            }
            foreach(var jobQueue in pawn.jobs.jobQueue)
			{
                if (!IsValidJob(jobQueue.job, pawn))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsValidJob(Job job, Pawn pawn)
        {
            return job.def != JobDefOf.HaulToContainer || job.targetA != pawn.carryTracker.CarriedThing;
        }

        private static bool ResourceValidator(Pawn pawn, MinifiedThing_Stackable minified, Thing th)
        {
            return (th is MinifiedThing_Stackable minified2) && minified2.InnerThing.def == minified.InnerThing.def && !th.IsForbidden(pawn) && pawn.CanReserve(th, 1, -1, null, false);
        }

        private static void FindAvailableNearbyResources(Thing firstFoundResource, Pawn pawn, out int resTotalAvailable)
        {
            resTotalAvailable = 0;
            if (!(firstFoundResource is MinifiedThing_Stackable minified)) return;

            int num = pawn.carryTracker.MaxStackSpaceEver(firstFoundResource.def);
            resourcesAvailable.Clear();
            resourcesAvailable.Add(firstFoundResource);
            resTotalAvailable += firstFoundResource.stackCount;
            if (resTotalAvailable < num)
            {
                foreach (Thing thing in GenRadial.RadialDistinctThingsAround(firstFoundResource.PositionHeld, firstFoundResource.MapHeld, 5f, false))
                {
                    if (!(thing is MinifiedThing_Stackable minified2)) continue;
                    if (resTotalAvailable >= num)
                    {
                        resTotalAvailable = num;
                        break;
                    }
                    if (minified2.InnerThing.def == minified.InnerThing.def && GenAI.CanUseItemForWork(pawn, thing))
                    {
                        resourcesAvailable.Add(thing);
                        resTotalAvailable += thing.stackCount;
                    }
                }
            }
            resTotalAvailable = Mathf.Min(resTotalAvailable, num);
        }

        private static HashSet<Thing> FindNearbyNeeders(Pawn pawn, Blueprint_Install install, int resTotalAvailable, out int neededTotal, out Job jobToMakeNeederAvailable)
        {
            neededTotal = 1;
            HashSet<Thing> hashSet = new HashSet<Thing>();
            foreach (Thing thing2 in GenRadial.RadialDistinctThingsAround(install.Position, install.Map, 8f, true))
            {
                if (neededTotal >= resTotalAvailable)
                {
                    break;
                }
                if (IsNewValidNearbyNeeder(thing2, hashSet, pawn))
                {
                    hashSet.Add(thing2);
                    neededTotal += 1;
                }
            }
            jobToMakeNeederAvailable = null;
            return hashSet;
        }

        private static bool IsNewValidNearbyNeeder(Thing t, HashSet<Thing> nearbyNeeders, Pawn pawn)
        {
            return t.Faction == pawn.Faction && !nearbyNeeders.Contains(t) && !t.IsForbidden(pawn) && GenConstruct.CanConstruct(t, pawn, false, false, JobDefOf.HaulToContainer);
        }

        private static readonly List<Thing> resourcesAvailable = new List<Thing>();

        private static readonly List<MinifiedThing_Stackable> missingResources = new List<MinifiedThing_Stackable>();

        private static readonly Dictionary<int, bool> cachedResults = new Dictionary<int, bool>();
    }

    [HarmonyPatch(typeof(WorkGiver_ConstructDeliverResourcesToBlueprints), "JobOnThing")]
    public static class Patch_WorkGiver_ConstructDeliverResourcesToBlueprints_NoCostFrameMakeJobFor
    {
        public static void Postfix(ref Job __result)
        {
        }
    }*/
}   