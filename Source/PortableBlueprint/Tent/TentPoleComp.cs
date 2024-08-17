using PortableBlueprint.PB_HarmonyPatch;
using PortableBlueprint.Tent.TentRoof;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PortableBlueprint.Tent
{
    public class TentPoleComp : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Designator_AreaBuildTentRoof();
            yield return new Designator_AreaNoTentRoof();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (respawningAfterLoad) return;
            var map = this.parent.Map;
            this.roofOverTent = new Dictionary<IntVec3, RoofDef>();
            for (int i = 0; i < GenRadial.NumCellsInRadius(this.parent.def.specialDisplayRadius); i++)
            {
                IntVec3 intVec = this.parent.Position + GenRadial.RadialPattern[i];
                RoofDef roof;
                if (intVec.InBounds(map) && (roof = intVec.GetRoof(map)) != null)
                {
                    if (roof == PB_DefOf.PB_TentRoof)
                    {
                        RoofDef roof2;
                        if (TentRoofUtility.GetFirstRoofHolder(intVec, map, false, this.parent).GetComp<TentPoleComp>().roofOverTent.TryGetValue(intVec, out roof2))
                        {
                            this.roofOverTent[intVec] = roof2;
                        }
                    }
                    else
                    {
                        this.roofOverTent[intVec] = roof;
                    }
                }
            }

            var room = this.parent.Position.GetRoom(map);
            if (!room.TouchesMapEdge)
            {
                Patch_AutoBuildRoofAreaSetter_TryGenerateAreaNow.MakeTentRoofArea(room.Cells.ToHashSet(), map);
            }
        }

        public override void PostDeSpawn(Map map)
        {
            for (int i = 0; i < GenRadial.NumCellsInRadius(this.parent.def.specialDisplayRadius); i++)
            {
                IntVec3 intVec = this.parent.Position + GenRadial.RadialPattern[i];
                if (intVec.InBounds(map) && intVec.GetRoof(map) == PB_DefOf.PB_TentRoof && !TentRoofUtility.WithinRangeOfRoofHolder(intVec, map, false, this.parent))
                {
                    RoofDef roof = null;
                    this.roofOverTent.TryGetValue(intVec, out roof);
                    map.roofGrid.SetRoof(intVec, roof);
                }
            }

            var cellRect = CellRect.SingleCell(this.parent.Position).ExpandedBy((int)this.parent.def.specialDisplayRadius + 1);
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

        public override void PostExposeData()
        {
            Scribe_Collections.Look(ref this.roofOverTent, "roofOverTent", LookMode.Value, LookMode.Def);
        }

        public Dictionary<IntVec3, RoofDef> roofOverTent;
    }
}
