using PortableBlueprint.PB_HarmonyPatch;
using PortableBlueprint.Tent.TentRoof;
using System.Collections.Generic;
using Verse;

namespace PortableBlueprint.Tent;

public class TentPoleComp : ThingComp
{
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        yield return new Designator_AreaBuildTentRoof();
        yield return new Designator_AreaNoTentRoof();
        yield return new Designator_AreaIgnoreTentRoof();
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        if (respawningAfterLoad) return;
        var map = parent.Map;
        var tempRoofGrid = map.GetComponent<TempRoofGrid>();
        for (int i = 0; i < GenRadial.NumCellsInRadius(parent.def.specialDisplayRadius); i++)
        {
            IntVec3 intVec = parent.Position + GenRadial.RadialPattern[i];
            RoofDef roof;
            if (intVec.InBounds(map) && (roof = intVec.GetRoof(map)) != null && roof != PB_DefOf.PB_TentRoof)
            {
                tempRoofGrid.SetRoof(intVec, roof);
            }
        }

        var room = parent.Position.GetRoom(map);
        if (!room.TouchesMapEdge)
        {
            Patch_AutoBuildRoofAreaSetter_TryGenerateAreaNow.MakeTentRoofArea([.. room.Cells], map);
        }
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        var tempRoofGrid = map.GetComponent<TempRoofGrid>();
        for (int i = 0; i < GenRadial.NumCellsInRadius(parent.def.specialDisplayRadius); i++)
        {
            IntVec3 intVec = parent.Position + GenRadial.RadialPattern[i];
            if (intVec.InBounds(map) && intVec.GetRoof(map) == PB_DefOf.PB_TentRoof && !TentRoofUtility.WithinRangeOfRoofHolder(intVec, map, false, parent))
            {
                map.roofGrid.SetRoof(intVec, tempRoofGrid.RoofAt(intVec));
                tempRoofGrid.SetRoof(intVec, null);
                map.areaManager.Get<Area_BuildTentRoof>()[intVec] = false;
                map.areaManager.Get<Area_NoTentRoof>()[intVec] = false;
            }
        }

        var cellRect = CellRect.SingleCell(parent.Position).ExpandedBy((int)parent.def.specialDisplayRadius + 1);
        var layersToRegenerate = new HashSet<SectionLayer>();
        for (int i = 0; i < cellRect.Height + 16; i += 17)
        {
            for (int j = 0; j < cellRect.Width + 16; j += 17)
            {
                layersToRegenerate.Add(map.mapDrawer.SectionAt(cellRect.Min + new IntVec3(j, 0, i)).GetLayer(typeof(SectionLayer_ThingsGeneral)));
            }
        }
        foreach (var layer in layersToRegenerate) layer.Regenerate();
    }
}
