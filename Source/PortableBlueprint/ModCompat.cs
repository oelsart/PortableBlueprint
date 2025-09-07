using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;

namespace PortableBlueprint;
public static class ModCompat
{
    public static class VehicleMapFramework
    {
        public static readonly bool Active = ModsConfig.IsActive("OELS.VehicleMapFramework") || ModsConfig.IsActive("OELS.VehicleMapFramework_steam") || ModsConfig.IsActive("OELS.VehicleMapFramework.dev");

        public static readonly Func<Map, IEnumerable<Map>> BaseMapAndVehicleMaps;

        public static readonly Func<Map, Map> BaseMap;

        static VehicleMapFramework()
        {
            if (!Active) return;
            try
            {
                BaseMapAndVehicleMaps = AccessTools.MethodDelegate<Func<Map, IEnumerable<Map>>>("VehicleMapFramework.VehicleMapUtility:BaseMapAndVehicleMaps");
                BaseMap = AccessTools.MethodDelegate<Func<Map, Map>>(AccessTools.Method("VehicleMapFramework.VehicleMapUtility:BaseMap", [typeof(Map)]));
            }
            catch
            {
                if (BaseMapAndVehicleMaps is null || BaseMap is null) Active = false;
            }
        }
    }
}
