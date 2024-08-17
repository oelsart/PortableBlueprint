using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace PortableBlueprint.Tent

{
    public class Graphic_LinkedTent : Graphic_Linked
    {
        public override Material MatSingle => MaterialAtlasPool.SubMaterialFromAtlas(((this.subGraphic as Graphic_Tent)
            .SubGraphicFor(Graphic_Tent.suffixes[0]) as Graphic_Appearances).SubGraphicFor(PB_DefOf.Fabric).MatSingle, LinkDirections.None);

        public Graphic_LinkedTent()
        {
        }

        public Graphic_LinkedTent(Graphic subGraphic) : base(subGraphic)
        {
            if (!(subGraphic is Graphic_Tent))
            {
                Log.Error("[Portable Blueprint] Only Graphic_Tent is allowed as the graphic for this link type. path=" + subGraphic.path);
            }
        }

        public override void Print(SectionLayer layer, Thing thing, float extraRotation)
        {
            Material mat = this.LinkedDrawMatFrom(thing, thing.Position);
            var vector = thing.TrueCenter();
            if (typeof(Building_TentDoor).IsAssignableFrom((thing.Position + IntVec3.South).GetEdifice(thing.Map)?.def.thingClass))
            {
                vector.y = AltitudeLayer.Blueprint.AltitudeFor();
            }
            Printer_Plane.PrintPlane(layer, vector, new Vector2(1f, 1f), mat, extraRotation, false, null, null, 0.01f, 0f);
            if (base.ShadowGraphic != null && thing != null)
            {
                base.ShadowGraphic.Print(layer, thing, 0f);
            }
        }

        public void Print(SectionLayer layer, Thing thing, float extraRotation, float overrideAltitude, LinkDirections? linkSet = null)
        {
            if (linkSet == null)
            {
                linkSet = this.GetLinkSet(thing, thing.Position);
            }
            Material mat = this.MaterialFor(thing, thing.Position, (LinkDirections)linkSet);
            var vector = thing.TrueCenter();
            vector.y = overrideAltitude;
            Printer_Plane.PrintPlane(layer, vector, new Vector2(1f, 1f), mat, extraRotation, false, null, null, 0.01f, 0f);
            if (base.ShadowGraphic != null && thing != null)
            {
                base.ShadowGraphic.Print(layer, thing, 0f);
            }
        }

        public override Material MatSingleFor(Thing thing)
        {
            return MaterialAtlasPool.SubMaterialFromAtlas((this.subGraphic as Graphic_Tent).SubGraphicFor(Graphic_Tent.suffixes[0]).MatSingleFor(thing), LinkDirections.None);
        }

        public LinkDirections GetLinkSet(Thing parent, IntVec3 cell)
        {
            int num = 0;
            int num2 = 1;
            for (int i = 0; i < 4; i++)
            {
                IntVec3 c = cell + GenAdj.CardinalDirections[i];
                if (this.ShouldLinkWith(c, parent))
                {
                    num += num2;
                }
                num2 *= 2;
            }
            return (LinkDirections)num;
        }

        protected override Material LinkedDrawMatFrom(Thing parent, IntVec3 cell)
        {
            var linkSet = this.GetLinkSet(parent, cell);
            return this.MaterialFor(parent, cell, linkSet);
        }

        public Material MaterialFor(Thing parent, IntVec3 cell, LinkDirections linkSet)
        {
            int num1 = 0;
            int num2 = 0;
            var adjacentCells = CellRect.SingleCell(cell).ExpandedBy(1).EdgeCells.Where(c => !parent.Map.linkGrid.LinkFlagsAt(c).HasFlag(parent.def.graphicData.linkFlags));
            for (var i = 0; i < adjacentCells.Count(); i++)
            {
                var cell2 = adjacentCells.ElementAt(i);
                if (cell2.GetRoof(parent.Map) == PB_DefOf.PB_TentRoof)
                {
                    if (linkSet.HasFlag(LinkDirections.Up) || linkSet.HasFlag(LinkDirections.Down))
                    {
                        if (cell2.x > cell.x) num1++;
                        else if (cell2.x < cell.x) num2++;
                    }
                    else
                    {
                        if (cell2.z > cell.z) num1++;
                        else if (cell2.z < cell.z) num2++;
                    }
                }
            }

            var graphicTent = this.subGraphic as Graphic_Tent;
            if (num1 == 0 && num2 == 0)
            {
                return MaterialAtlasPool.SubMaterialFromAtlas(graphicTent.SubGraphicFor(Graphic_Tent.suffixes[0]).MatSingleFor(parent), LinkDirections.None);
            }
            if (num1 >= num2)
            {
                return MaterialAtlasPool.SubMaterialFromAtlas(graphicTent.SubGraphicFor(Graphic_Tent.suffixes[1]).MatSingleFor(parent), linkSet);
            }
            else
            {
                return MaterialAtlasPool.SubMaterialFromAtlas(graphicTent.SubGraphicFor(Graphic_Tent.suffixes[0]).MatSingleFor(parent), linkSet);
            }
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return new Graphic_LinkedTent(subGraphic.GetColoredVersion(newShader, newColor, newColorTwo));
        }
    }
}
