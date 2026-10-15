using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace PortableBlueprint.PB_HarmonyPatch;

[HarmonyPatch(typeof(JobDriver_HaulToContainer), "MakeNewToils")]
public static class Patch_JobDriver_HaulToContainer_MakeNewToils
{
	public static IEnumerable<Toil> Postfix(IEnumerable<Toil> values)
	{
		foreach (var toil in values)
		{
			if (toil.debugName == "MoveOffTargetBlueprint")
			{
				yield return SetSplitThingToInstall(TargetIndex.B);
			}

			yield return toil;
		}
	}

	public static Toil SetSplitThingToInstall(TargetIndex containerInd)
	{
		var toil = ToilMaker.MakeToil();
		toil.initAction = () =>
		{
			var actor = toil.actor;
			var curJob = actor.jobs.curJob;
			var thing = curJob.GetTarget(containerInd).Thing;
			if (thing is not Blueprint_Install blueprint) return;
			var carriedThing = actor.carryTracker.CarriedThing;

			if (carriedThing is not MinifiedThingStackable minifiedThing) return;
			Blueprint_InstallEnroute.SetThingToInstallFromMinified(blueprint, minifiedThing);
		};
		return toil;
	}
}

[HarmonyPatch(typeof(ListerBuildings), nameof(ListerBuildings.RegisterInstallBlueprint))]
public static class Patch_ListerBuildings_RegisterInstallBlueprint
{
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
		ILGenerator generator)
	{
		var codes = instructions.ToList();
		var label = generator.DefineLabel();
		codes[0].labels.Add(label);
		codes.InsertRange(0,
		[
			CodeInstruction.LoadArgument(1),
			new CodeInstruction(OpCodes.Callvirt,
				AccessTools.PropertyGetter(typeof(Blueprint_Install), "MiniToInstallOrBuildingToReinstall")),
			new CodeInstruction(OpCodes.Isinst, typeof(MinifiedThingStackable)),
			new CodeInstruction(OpCodes.Brfalse_S, label),
			new CodeInstruction(OpCodes.Ret)
		]);

		return codes;
	}
}

[HarmonyPatch(typeof(ListerBuildings), nameof(ListerBuildings.DeregisterInstallBlueprint))]
public static class Patch_ListerBuildings_DeregisterInstallBlueprint
{
	public static void Postfix(Blueprint_Install blueprint)
	{
		if (blueprint.MiniToInstallOrBuildingToReinstall is MinifiedThingStackable minifiedStackable)
		{
			minifiedStackable.blueprintsForDrawLine.Remove(blueprint);
		}
	}
}

[HarmonyPatch(typeof(MinifyUtility), nameof(MinifyUtility.Uninstall))]
public static class Patch_MinifyUtility_Uninstall
{
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var codes = instructions.ToList();
		var pos = codes.FindLastIndex(c => c.opcode == OpCodes.Brfalse_S);
		var label = codes[pos].operand;
		codes.InsertRange(pos + 1, [
			CodeInstruction.LoadLocal(1),
			new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.Destroyed))),
			new CodeInstruction(OpCodes.Brtrue_S, label),
		]);
		return codes;
	}
}

[HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "ResourceDeliverJobFor")]
public static class Patch_WorkGiver_ConstructDeliverResources_ResourceDeliverJobFor
{
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var codes = new CodeMatcher(instructions);
		codes.MatchStartForward(new CodeMatch(c => c.opcode == OpCodes.Isinst && c.OperandIs(typeof(Blueprint_Install))));
		codes.InsertAfterAndAdvance(CodeInstruction.Call(
			typeof(Patch_WorkGiver_ConstructDeliverResources_ResourceDeliverJobFor), nameof(CheckInstallEnroute)));
		return codes.Instructions();
	}

	private static Blueprint_Install CheckInstallEnroute(Blueprint_Install blueprint_Install)
	{
		if (blueprint_Install is Blueprint_InstallEnroute)
		{
			return null;
		}

		return blueprint_Install;
	}
}

[HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "IsNewValidNearbyNeeder")]
public static class Patch_WorkGiver_ConstructDeliverResources_IsNewValidNearbyNeeder
{
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var codes = instructions.ToList();
		var m_Isnt = AccessTools.Method(typeof(GenTypes), nameof(GenTypes.Isnt), [typeof(object)]);
		var m_IsntOr = AccessTools.Method(typeof(Patch_WorkGiver_ConstructDeliverResources_IsNewValidNearbyNeeder),
			nameof(IsntBlueprintInstallOrBlueprintInstallEnroute));
		var pos = codes.FindIndex(c =>
		{
			if (c.opcode != OpCodes.Call || c.operand is not MethodInfo method || !method.IsGenericMethod) return false;
			return method.GetGenericMethodDefinition() == m_Isnt;
		});
		if (pos != -1)
		{
			codes[pos].operand = m_IsntOr;
			codes.Insert(pos, CodeInstruction.LoadArgument(3));
		}

		return codes;
	}

	private static bool IsntBlueprintInstallOrBlueprintInstallEnroute(Thing t, IConstructible constructible)
	{
		return t.Isnt<Blueprint_Install>() || (t is Blueprint_InstallEnroute blueprint &&
		                                       constructible is Blueprint_InstallEnroute blueprint2 &&
		                                       blueprint.def == blueprint2.def);
	}
}

[HarmonyPatch(typeof(Toils_Haul), nameof(Toils_Haul.DepositHauledThingInContainer))]
public static class Patch_Toils_Haul_DepositHauledThingInContainer
{
	public static void Postfix(TargetIndex containerInd, Toil __result)
	{
		__result.AddPreInitAction(() =>
		{
			var actor = __result.actor;
			var curJob = actor.jobs.curJob;
			var thing = curJob.GetTarget(containerInd).Thing;
			if (thing.def.Minifiable && thing.def.minifiedDef.thingClass.SameOrSubclassOf<MinifiedThingStackable>())
			{
				actor.jobs.curDriver.ReadyForNextToil();
			}
		});
	}
}