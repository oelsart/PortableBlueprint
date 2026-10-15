using PortableBlueprint.Tent;
using Verse;

namespace PortableBlueprint;

public static class TentRoofUtility
{
	public static bool WithinRangeOfRoofHolder(IntVec3 c, Map map, bool assumeNonNoRoofCellsAreRoofed = false,
		Thing ignoreThing = null)
	{
		return GetFirstRoofHolder(c, map, assumeNonNoRoofCellsAreRoofed, ignoreThing) != null;
	}

	public static Building GetFirstRoofHolder(IntVec3 c, Map map, bool assumeNonNoRoofCellsAreRoofed = false,
		Thing ignoreThing = null)
	{
		Building building = null;
		map.floodFiller.FloodFill(c,
			x => (x.GetRoof(map) == PB_DefOf.PB_TentRoof || x == c ||
			      (assumeNonNoRoofCellsAreRoofed && !map.areaManager.Get<Area_NoTentRoof>()[x])) &&
			     x.InHorDistOf(c, RoofMaxSupportDistance), delegate(IntVec3 x)
			{
				for (var i = 0; i < 5; i++)
				{
					var c2 = x + GenAdj.CardinalDirectionsAndInside[i];
					if (c2.InBounds(map) && c2.InHorDistOf(c, RoofMaxSupportDistance))
					{
						var edifice = c2.GetEdifice(map);
						if ((edifice?.HasComp<TentPoleComp>() ?? false) && edifice != ignoreThing)
						{
							building = edifice;
							return true;
						}
					}
				}

				return false;
			});
		return building;
	}

	public static bool ConnectedToRoofHolder(IntVec3 c, Map map, bool assumeRoofAtRoot)
	{
		var connected = false;
		map.floodFiller.FloodFill(c,
			x => (x.GetRoof(map) == PB_DefOf.PB_TentRoof || x == c & assumeRoofAtRoot) && !connected, delegate(IntVec3 x)
			{
				for (var i = 0; i < 5; i++)
				{
					var c2 = x + GenAdj.CardinalDirectionsAndInside[i];
					if (c2.InBounds(map))
					{
						var edifice = c2.GetEdifice(map);
						if (edifice?.HasComp<TentPoleComp>() ?? false)
						{
							connected = true;
							return;
						}
					}
				}
			});
		return connected;
	}

	public static Thing GetTentRoofHolder(this IntVec3 c, Map map)
	{
		var thingList = c.GetThingList(map);
		return thingList.FirstOrDefault(t => t.HasComp<TentPoleComp>());
	}

	private static readonly float RoofMaxSupportDistance = DefDatabase<ThingDef>.AllDefs
		.Where(d => d.HasComp<TentPoleComp>()).Select(d => d.specialDisplayRadius).Max();
}