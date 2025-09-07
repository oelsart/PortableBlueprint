using System;
using System.Linq;
using Verse;

namespace PortableBlueprint.Tent;

public class TentFloorComp : ThingComp
{
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        foreach (var c in parent.OccupiedRect())
        {
            var thingList = c.GetThingList(parent.Map);
            foreach (var thing in thingList)
            {
                if (thing.def.category == ThingCategory.Plant && thing.Map.dynamicDrawManager.DrawThings.Contains(thing))
                {
                    thing.Map.dynamicDrawManager.DeRegisterDrawable(thing);
                }
            }
        }
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map);
        foreach (var c in parent.OccupiedRect())
        {
            var thingList = c.GetThingList(map);
            foreach (var thing in thingList)
            {
                if (thing.def.category == ThingCategory.Plant && thing.def.drawerType != DrawerType.MapMeshOnly && !thing.Map.dynamicDrawManager.DrawThings.Contains(thing))
                {
                    thing.Map.dynamicDrawManager.RegisterDrawable(thing);
                }
            }
        }
    }
}
