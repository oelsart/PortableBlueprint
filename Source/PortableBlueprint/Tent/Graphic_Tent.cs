using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PortableBlueprint.Tent
{
    public class Graphic_Tent : Graphic_Appearances
    {
        public override Material MatSingle => MaterialAtlasPool.SubMaterialFromAtlas((this.subGraphics[0] as Graphic_Appearances).SubGraphicFor(PB_DefOf.Fabric).MatSingle, LinkDirections.None);

        public override void Init(GraphicRequest req)
        {
            this.subGraphics = new Graphic[Graphic_Tent.suffixes.Count];
            this.data = req.graphicData;
            this.path = req.path;
            this.color = req.color;
            this.drawSize = req.drawSize;
            for (var i = 0; i < Graphic_Tent.suffixes.Count; i++)
            {
                this.subGraphics[i] = GraphicDatabase.Get<Graphic_Appearances>(req.path + Graphic_Tent.suffixes[i], req.shader, req.drawSize, req.color, Color.white, req.graphicData);
            }
        }

        public override void Print(SectionLayer layer, Thing thing, float extraRotation)
        {
            Log.WarningOnce("[PortableBlueprint] Graphic_Tent must be wrapped. This usually occurs after defs hotreload.", 54651343);
        }

        public Graphic SubGraphicFor(string suffix)
        {
            return subGraphics[suffixes.IndexOf(suffix)];
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return GraphicDatabase.Get<Graphic_Tent>(this.path, newShader, this.drawSize, newColor, Color.white, this.data, null);
        }

        public readonly static List<string> suffixes = new List<string>{
            "_NorthEast",
            "_SouthWest",
        };
    }
}
