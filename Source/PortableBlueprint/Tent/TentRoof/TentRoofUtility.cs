using PortableBlueprint.Tent;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PortableBlueprint
{
    public static class TentRoofUtility
    {
        public static bool WithinRangeOfRoofHolder(IntVec3 c, Map map, bool assumeNonNoRoofCellsAreRoofed = false, Thing ignoreThing = null)
        {
            return TentRoofUtility.GetFirstRoofHolder(c, map, assumeNonNoRoofCellsAreRoofed, ignoreThing) != null;
        }

        public static Building GetFirstRoofHolder(IntVec3 c, Map map, bool assumeNonNoRoofCellsAreRoofed = false, Thing ignoreThing = null)
        {
            Building building = null;
            map.floodFiller.FloodFill(c, (IntVec3 x) => (x.GetRoof(map) == PB_DefOf.PB_TentRoof || x == c || (assumeNonNoRoofCellsAreRoofed && !map.areaManager.Get<Area_NoTentRoof>()[x])) && x.InHorDistOf(c, RoofMaxSupportDistance), delegate (IntVec3 x)
            {
                for (int i = 0; i < 5; i++)
                {
                    IntVec3 c2 = x + GenAdj.CardinalDirectionsAndInside[i];
                    if (c2.InBounds(map) && c2.InHorDistOf(c, RoofMaxSupportDistance))
                    {
                        Building edifice = c2.GetEdifice(map);
                        if ((edifice?.HasComp<TentPoleComp>() ?? false) && edifice != ignoreThing)
                        {
                            building = edifice;
                            return true;
                        }
                    }
                }
                return false;
            }, int.MaxValue, false, null);
            return building;
        }

        public static bool ConnectedToRoofHolder(IntVec3 c, Map map, bool assumeRoofAtRoot)
        {
            bool connected = false;
            map.floodFiller.FloodFill(c, (IntVec3 x) => (x.GetRoof(map) == PB_DefOf.PB_TentRoof || (x == c & assumeRoofAtRoot)) && !connected, delegate (IntVec3 x)
            {
                for (int i = 0; i < 5; i++)
                {
                    IntVec3 c2 = x + GenAdj.CardinalDirectionsAndInside[i];
                    if (c2.InBounds(map))
                    {
                        Building edifice = c2.GetEdifice(map);
                        if (edifice?.HasComp<TentPoleComp>() ?? false)
                        {
                            connected = true;
                            return;
                        }
                    }
                }
            }, int.MaxValue, false, null);
            return connected;
        }

        public static Thing GetTentRoofHolder(this IntVec3 c, Map map)
        {
            List<Thing> thingList = c.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i].HasComp<TentPoleComp>())
                {
                    return thingList[i];
                }
            }
            return null;
        }

        private static readonly float RoofMaxSupportDistance = DefDatabase<ThingDef>.AllDefs.Where(d => d.HasComp<TentPoleComp>()).Select(d => d.specialDisplayRadius).Max();
    }
}
