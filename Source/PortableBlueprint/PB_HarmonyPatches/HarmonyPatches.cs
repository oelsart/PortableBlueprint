using HarmonyLib;
using PortableBlueprint.PB_HarmonyPatches;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using static PortableBlueprint.PB_HarmonyPatches.ExtraGUIFormCaravan;

namespace PortableBlueprint.PB_HarmonyPatch
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.harmony.rimworld.portableblueprint");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Patch_PlayDataLoader_HotReloadDefs.Postfix();
            if (ModsConfig.IsActive("dhultgren.useminifiedbuildings"))
            {
                var type = AccessTools.TypeByName("UseMinifiedBuildings.Patches.Patch_PlaceBlueprintForBuild");
                var transpiler = AccessTools.Method(typeof(Patch_UseMinifiedBuildings_GetClosestCandidate), "Transpiler");
                harmony.Patch(AccessTools.Method(type, "GetClosestCandidate"), null, null, transpiler);
            }
            if (ModsConfig.IsActive("smashphil.vehicleframework"))
            {
                var type = AccessTools.TypeByName("Vehicles.UIHelper");
                var postfix = AccessTools.Method(typeof(Patch_UIHelper_CreateVehicleCaravanTransferableWidgets), "Postfix");
                harmony.Patch(AccessTools.Method(type, "CreateVehicleCaravanTransferableWidgets"), null, postfix);
                var type2 = AccessTools.TypeByName("Vehicles.Dialog_LoadCargo");
                var postfix2 = AccessTools.Method(typeof(Patch_Dialog_LoadTransporters_CalculateAndRecacheTransferables), "Postfix");
                harmony.Patch(AccessTools.Method(type2, "CalculateAndRecacheTransferables"), null, postfix2);
            }
        }
    }

    public static class Patch_UseMinifiedBuildings_GetClosestCandidate
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var minifiedThingDef = AccessTools.Field(typeof(ThingDefOf), nameof(ThingDefOf.MinifiedThing));
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Ldsfld && c.operand.Equals(minifiedThingDef));

            var ofType = AccessTools.Method(typeof(Enumerable), nameof(Enumerable.OfType)).MakeGenericMethod(typeof(MinifiedThing));
            var pos2 = codes.FindIndex(pos, c => c.opcode == OpCodes.Call && c.operand.Equals(ofType));

            codes.RemoveRange(pos, pos2 - pos + 1);

            var GetThingsOfType = AccessTools.Method(typeof(ListerThings), nameof(ListerThings.GetThingsOfType));
            GetThingsOfType = GetThingsOfType.MakeGenericMethod(typeof(MinifiedThing));
            codes.Insert(pos, new CodeInstruction(OpCodes.Call, GetThingsOfType));

            return codes;
        }
    }

    public static class Patch_UIHelper_CreateVehicleCaravanTransferableWidgets
    {
        public static void Postfix(List<TransferableOneWay> transferables, TransferableOneWayWidget itemsTransfer, Func<float> availableMassGetter)
        {
            Patch_CaravanUIUtility_CreateCaravanTransferableWidgets.Postfix(transferables, itemsTransfer, null, availableMassGetter);
        }
    }

    [HarmonyPatch(typeof(PlayDataLoader), "HotReloadDefs")]
    public static class Patch_PlayDataLoader_HotReloadDefs
    {
        public static void Postfix()
        {
            LongEventHandler.QueueLongEvent(() =>
            {
                PB_DefOf.PB_TentWall.graphicData.CopyFrom(PB_DefOf.PB_TentWall.graphicData);
                PB_DefOf.PB_TentWall.graphicData.linkType = Patch_GraphicUtility_WrapLinked.linkType;
                PB_DefOf.PB_TentWall.PostLoad();
                var layersToRegenerate = new HashSet<SectionLayer>();
                Find.Maps?.ForEach(m =>
                {
                    m.listerThings.ThingsOfDef(PB_DefOf.PB_TentWall)?.ForEach(t =>
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