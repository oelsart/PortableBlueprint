using HarmonyLib;
using PortableBlueprint.PB_HarmonyPatches;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using CaravanItemSelectionEnhanced;
using System.Runtime;

namespace PortableBlueprint.CaravanItemSelectionEnhancedPatch
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.harmony.rimworld.Portableblueprint.caravanitemselectionenhancedpatch");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            ExtraGUIFormCaravan.caravanUIActive = true;
            var modSettings = AccessTools.TypeByName("CaravanItemSelectionEnhanced.ModSettings");
            var instance = AccessTools.Field(modSettings, "settings").GetValue(null);
            Patch_CaravanUIUtility_Prefix_CreateCaravanTransferableWidgetsPrefix.config = AccessTools.FieldRefAccess<List<CaravanUiEntry>>(modSettings, "config")(instance);
        }
    }

    [HarmonyPatch]
    public static class Patch_CaravanUIUtility_Prefix_CreateCaravanTransferableWidgetsPrefix
    {
        static MethodInfo TargetMethod()
        {
            var type = AccessTools.TypeByName("CaravanItemSelectionEnhanced.CaravanUIUtility_Prefix");
            return AccessTools.Method(type, "CreateCaravanTransferableWidgetsPrefix");
        }

       /*public static void Postfix(List<TransferableOneWayWidget> ___transfers)
        {
            ExtraGUIFormCaravan.caravanUItransfers = ___transfers;
            foreach (var entry in config)
            {
                if (!entry.isPawnsTab && !entry.isUnassignedTab)
                {
                    entry.
                }
            }
        }*/

        public static List<CaravanUiEntry> config;
    }
}
