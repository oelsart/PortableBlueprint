using RimWorld;
using System.Linq;
using Verse;
using static PortableBlueprint.ModCompat;

namespace PortableBlueprint;

public class GenConstructEx
{
    public static Blueprint_InstallEnroute PlaceBlueprintForInstallEnroute(MinifiedThing itemToInstall, IntVec3 center, Map map, Rot4 rotation, Faction faction, bool sendBPSpawnedSignal = true)
    {
        var blueprint_Install = MakeBlueprintInstallEnroute(itemToInstall.InnerThing.def.installBlueprintDef);
        Blueprint_InstallEnroute.SetThingToInstallFromMinified(blueprint_Install, itemToInstall);
        blueprint_Install.SetFactionDirect(faction);
        GenSpawn.Spawn(blueprint_Install, center, map, rotation);
        if (faction != null && sendBPSpawnedSignal)
        {
            QuestUtility.SendQuestTargetSignals(faction.questTags, "PlacedBlueprint", blueprint_Install.Named("SUBJECT"));
        }
        return blueprint_Install;
    }

    internal static Blueprint_InstallEnroute MakeBlueprintInstallEnroute(ThingDef def, ThingDef stuff = null)
    {
        if (stuff != null && !stuff.IsStuff)
        {
            Log.Error("MakeThing error: Tried to make " + def?.ToString() + " from " + stuff?.ToString() + " which is not a stuff. Assigning default.");
            stuff = GenStuff.DefaultStuffFor(def);
        }
        if (def.MadeFromStuff && stuff == null)
        {
            Log.Error("MakeThing error: " + def?.ToString() + " is madeFromStuff but stuff=null. Assigning default.");
            stuff = GenStuff.DefaultStuffFor(def);
        }
        if (!def.MadeFromStuff && stuff != null)
        {
            Log.Error("MakeThing error: " + def?.ToString() + " is not madeFromStuff but stuff=" + stuff?.ToString() + ". Setting to null.");
            stuff = null;
        }
        if (!typeof(Blueprint_Install).IsAssignableFrom(def.thingClass))
        {
            Log.Error("MakeThing error: " + "Invalid item for MakeBlueprintInstallEnroute()");
            return null;
        }
        Blueprint_InstallEnroute obj = new()
        {
            def = def
        };
        obj.SetStuffDirect(stuff);
        obj.PostMake();
        obj.PostPostMake();
        return obj;
    }

    public static MinifiedThing FindMinifiedThing(Map map, ThingDef innerDef, ThingDef stuff, ThingStyleDef style = null, Precept_ThingStyle precept = null)
    {
        bool MinifiedThingMatch(MinifiedThing minifiedThing)
        {
            var innerThing = minifiedThing?.InnerThing;
            return innerThing?.def == innerDef && innerThing.Stuff == stuff && innerThing.StyleDef == style && innerThing.StyleSourcePrecept == precept &&
                MinifiedThingAvailable(minifiedThing);
        }

        MinifiedThing MinifiedThingOnMap(Map map2)
        {
            foreach (var minifiedThing in map2.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.MinifiedThing)).OfType<MinifiedThing>())
            {
                if (MinifiedThingMatch(minifiedThing))
                {
                    return minifiedThing;
                }
            }
            return null;
        }

        MinifiedThing MinifiedThingInInventory(Map map2)
        {
            foreach (var pawn in map2.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
            {
                if (pawn.inventory is null) continue;
                foreach (var minifiedThing in pawn.inventory.innerContainer.OfType<MinifiedThing>())
                {
                    if (MinifiedThingMatch(minifiedThing))
                    {
                        return minifiedThing;
                    }
                }
            }
            return null;
        }

        if (VehicleMapFramework.Active)
        {
            if (PortableBlueprint.settings.placeBlueprintSettings["PB.BuildFromMap"])
            {
                foreach (var map2 in VehicleMapFramework.BaseMapAndVehicleMaps(map))
                {
                    var minifiedThing = MinifiedThingOnMap(map2);
                    if (minifiedThing != null)
                    {
                        return minifiedThing;
                    }
                }
            }
            if (PortableBlueprint.settings.placeBlueprintSettings["PB.BuildFromInventory"])
            {
                return MinifiedThingInInventory(VehicleMapFramework.BaseMap(map));
            }
            return null;
        }
        else
        {
            if (PortableBlueprint.settings.placeBlueprintSettings["PB.BuildFromMap"])
            {
                var minifiedThing = MinifiedThingOnMap(map);
                if (minifiedThing != null)
                {
                    return minifiedThing;
                }
            }
            if (PortableBlueprint.settings.placeBlueprintSettings["PB.BuildFromInventory"])
            {
                return MinifiedThingInInventory(map);
            }
            return null;
        }
    }

    private static bool MinifiedThingAvailable(MinifiedThing minifiedThing)
    {
        if (minifiedThing.IsForbidden(Faction.OfPlayer)) return false;
        if (minifiedThing is MinifiedThingStackable minifiedThingStackable)
        {
            return minifiedThingStackable.stackCount > minifiedThingStackable.blueprintsForDrawLine.Count;
        }
        return InstallBlueprintUtility.ExistingBlueprintFor(minifiedThing) is null;
    }
}