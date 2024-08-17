using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UseMinifiedBuildings.Patches;
using Verse;

namespace PortableBlueprint.UseMinifiedBuildingsPatch
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.harmony.rimworld.Portableblueprint.useminifiedbuildingspatch");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(Patch_PlaceBlueprintForBuild), "GetClosestCandidate")]
    public static class Patch_Patch_PlaceBlueprintForBuild_GetClosestCandidate
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Newobj && c.operand.Equals(AccessTools.Constructor(typeof(Func<MinifiedThing, bool>), new Type[] { typeof(object), typeof(IntPtr) }))) - 2;
            codes.RemoveRange(pos, 4);
            codes.InsertRange(pos, new[]
            {
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadArgument(2),
                CodeInstruction.LoadArgument(3),
                CodeInstruction.LoadArgument(4),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                CodeInstruction.Call(typeof(Patch_Patch_PlaceBlueprintForBuild_GetClosestCandidate), "ModifiedWhere")
            });
            
            return codes;
        }

        private static IEnumerable<MinifiedThing> ModifiedWhere(this IEnumerable<MinifiedThing> source, BuildableDef sourceDef, Map map, Faction faction, ThingDef stuff, bool inventory)
        {
            var settings = UseMinifiedBuildings.UseMinifiedBuildings.Settings;
            if (ModsConfig.IsActive("uuugggg.buildfrominventory") && !inventory)
            {
                source = source.ConcatIfNotNull(IncludePawnInventory(sourceDef, map, faction, stuff));
            }
            return source.Where(m =>
            {
                bool flag;
                if (m is MinifiedThing_Stackable ms) flag = ms.blueprintsForDrawLine.Count < ms.stackCount;
                else flag = InstallBlueprintUtility.ExistingBlueprintFor(m) == null;
                return m.InnerThing.def == sourceDef && m.InnerThing.Stuff == stuff && !m.IsForbidden(faction) &&
                flag && (!m.TryGetQuality(out var qc) || (settings.EnableForQualityBuildings &&
                qc >= settings.GetMinQuality(sourceDef.frameDef, map)));
            });
        }

        private static IEnumerable<MinifiedThing> IncludePawnInventory(BuildableDef sourceDef, Map map, Faction faction, ThingDef stuff)
        {
            return map.mapPawns.PawnsInFaction(faction)?.SelectMany(p => p.inventory.GetDirectlyHeldThings().Concat(p.carryTracker.GetDirectlyHeldThings())
                .OfType<MinifiedThing>().ModifiedWhere(sourceDef, map, faction, stuff, true));
        }
    }

    [HarmonyPatch(typeof(Patch_PlaceBlueprintForBuild), "Prefix")]
    public static class Patch_Patch_PlaceBlueprintForBuild_Prefix
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Call && c.operand.Equals(AccessTools.Method(typeof(GenConstruct), nameof(GenConstruct.PlaceBlueprintForInstall)))) + 1;
            var label = generator.DefineLabel();
            var blueprint = generator.DeclareLocal(typeof(Blueprint_Install));

            codes.Remove(codes[pos]);
            codes[pos].labels.Add(label);
            codes.InsertRange(pos, new[]
            {
                new CodeInstruction(OpCodes.Stloc_S, blueprint),
                CodeInstruction.LoadLocal(0),
                new CodeInstruction(OpCodes.Isinst, typeof(MinifiedThing_Stackable)),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                CodeInstruction.LoadLocal(0),
                CodeInstruction.LoadField(typeof(MinifiedThing_Stackable), nameof(MinifiedThing_Stackable.blueprintsForDrawLine)),
                new CodeInstruction(OpCodes.Ldloc_S, blueprint),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(HashSet<Blueprint_Install>), nameof(HashSet<Blueprint_Install>.Add))),
                new CodeInstruction(OpCodes.Pop)
            });

            return codes;
        }
    }
}
