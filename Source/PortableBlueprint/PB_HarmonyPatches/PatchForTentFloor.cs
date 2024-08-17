using PortableBlueprint.Tent;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace PortableBlueprint.PB_HarmonyPatch
{
    [HarmonyPatch(typeof(FilthMaker), "CanMakeFilth")]
    public static class Patch_FilthMaker_CanMakeFilth
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var label = generator.DefineLabel();
            codes.InsertRange(0, new CodeInstruction[]
            {
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadArgument(1),
                CodeInstruction.Call(typeof(Patch_FilthMaker_CanMakeFilth), "GetTentFloor"),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadArgument(1),
                CodeInstruction.LoadArgument(2),
                CodeInstruction.LoadArgument(3),
                CodeInstruction.Call(typeof(Patch_FilthMaker_CanMakeFilth), "CanMakeFilthOnTentFloor"),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Pop).WithLabels(label)
            });
            return codes;
        }

        public static Thing GetTentFloor(this IntVec3 c, Map map)
        {
            return c.GetThingList(map).FirstOrDefault(t => t.def.GetModExtension<TentParts>()?.filthAcceptanceMask > FilthSourceFlags.None);
        }

        public static bool CanMakeFilthOnTentFloor(Thing tentFloor, IntVec3 c, Map map, ThingDef filthDef, FilthSourceFlags additionalFlags = FilthSourceFlags.None)
        {
            if (!filthDef.filth.ignoreFilthMultiplierStat && (filthDef.filth.placementMask & FilthSourceFlags.Natural) == FilthSourceFlags.None && Rand.Value > tentFloor.def.GetStatValueAbstract(StatDefOf.FilthMultiplier, null))
            {
                return false;
            }
            FilthSourceFlags filthSourceFlags = filthDef.filth.placementMask | additionalFlags;
            FilthSourceFlags filthAcceptanceMask = tentFloor.def.GetModExtension<TentParts>().filthAcceptanceMask;
            if (filthAcceptanceMask != FilthSourceFlags.None && filthSourceFlags.HasFlag(FilthSourceFlags.Pawn))
            {
                if (c.GetRoof(map) != null)
                {
                    return true;
                }
                Room room = c.GetRoom(map);
                if (room != null && !room.TouchesMapEdge && !room.UsesOutdoorTemperature)
                {
                    return true;
                }
            }
            if (filthAcceptanceMask == FilthSourceFlags.None)
            {
                return false;
            }
            return (filthAcceptanceMask & filthSourceFlags) == filthSourceFlags;
        }
    }

    [HarmonyPatch(typeof(Pawn_FilthTracker), "TryPickupFilth")]
    public static class Patch_Pawn_FilthTracker_TryPickupFilth
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var label = generator.DefineLabel();
            codes[0].labels.Add(label);
            codes.InsertRange(0, new CodeInstruction[]
            {
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadField(typeof(Pawn_FilthTracker), "pawn"),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.Position))),
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadField(typeof(Pawn_FilthTracker), "pawn"),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.Map))),
                CodeInstruction.Call(typeof(Patch_FilthMaker_CanMakeFilth), "GetTentFloor"),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ret)
            });
            return codes;
        }
    }
}