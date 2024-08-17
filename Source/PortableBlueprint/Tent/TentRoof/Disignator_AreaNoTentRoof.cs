using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PortableBlueprint.Tent.TentRoof
{
    public class Designator_AreaNoTentRoof : Designator_AreaNoRoof
    {
        public Designator_AreaNoTentRoof()
        {
            this.defaultLabel = "PB.DesignatorAreaNoTentRoofExpand".Translate();
            this.defaultDesc = "PB.DesignatorAreaNoTetRoofExpandDesc".Translate();
            this.icon = ContentFinder<Texture2D>.Get("UI/Designators/NoRoofArea", true);
            this.hotKey = KeyBindingDefOf.Misc5;
            this.soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
            this.soundDragChanged = null;
            this.soundSucceeded = SoundDefOf.Designate_ZoneAdd;
            this.useMouseIcon = true;
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
            return !base.Map.areaManager.Get<Area_NoTentRoof>()[c];
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            base.Map.areaManager.Get<Area_NoTentRoof>()[c] = true;
            Designator_AreaNoTentRoof.justAddedCells.Add(c);
        }

        protected override void FinalizeDesignationSucceeded()
        {
            base.FinalizeDesignationSucceeded();
            for (int i = 0; i < Designator_AreaNoTentRoof.justAddedCells.Count; i++)
            {
                base.Map.areaManager.Get<Area_BuildTentRoof>()[Designator_AreaNoTentRoof.justAddedCells[i]] = false;
            }
            Designator_AreaNoTentRoof.justAddedCells.Clear();
        }

        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
            GenUI.RenderMouseoverBracket();
            foreach (var tentPole in this.tentPoles)
            {
                GenDraw.DrawRadiusRing(tentPole.Position, tentPole.def.specialDisplayRadius);
            }
            base.Map.areaManager.Get<Area_NoTentRoof>().MarkForDraw();
            base.Map.areaManager.Get<Area_BuildTentRoof>().MarkForDraw();
        }

        private readonly IEnumerable<Building> tentPoles;

        private static List<IntVec3> justAddedCells = new List<IntVec3>();

    }
}
