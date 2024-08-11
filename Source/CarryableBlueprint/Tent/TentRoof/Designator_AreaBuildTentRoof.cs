using RimWorld;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CarryableBlueprint.Tent.TentRoof
{
    public class Designator_AreaBuildTentRoof : Designator_AreaBuildRoof
    {
        public Designator_AreaBuildTentRoof()
        {
            this.defaultLabel = "CB.DesignatorAreaBuildRoofExpand".Translate();
            this.defaultDesc = "CB.DesignatorAreaBuildRoofExpandDesc".Translate();
            this.icon = ContentFinder<Texture2D>.Get("UI/Designators/BuildRoofArea", true);
            this.hotKey = KeyBindingDefOf.Misc9;
            this.soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
            this.soundDragChanged = SoundDefOf.Designate_DragZone_Changed;
            this.soundSucceeded = SoundDefOf.Designate_ZoneAdd_Roof;
            this.useMouseIcon = true;
            this.tutorTag = "AreaBuildTentRoofExpand";
            this.tentPoles = this.Map.listerBuildings.allBuildingsColonist.Where(b => b.HasComp<TentPoleComp>());
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(base.Map))
            {
                return false;
            }
            if (c.Fogged(base.Map))
            {
                return false;
            }
            return !base.Map.areaManager.Get<Area_BuildTentRoof>()[c];
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            base.Map.areaManager.Get<Area_BuildTentRoof>()[c] = true;
            base.Map.areaManager.Get<Area_NoTentRoof>()[c] = false;
        }

        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
            foreach (var tentPole in this.tentPoles)
            {
                GenDraw.DrawRadiusRing(tentPole.Position, tentPole.def.specialDisplayRadius);
            }
            base.Map.areaManager.Get<Area_NoTentRoof>().MarkForDraw();
            base.Map.areaManager.Get<Area_BuildTentRoof>().MarkForDraw();
        }

        private readonly IEnumerable<Building> tentPoles;
    }
}
