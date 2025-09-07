using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PortableBlueprint.Tent;

public class Graphic_Tent : Graphic_Appearances
{
    public readonly static List<string> suffixes =
    [
        "_NorthEast",
        "_SouthWest",
    ];

    public override Material MatSingle => MaterialAtlasPool.SubMaterialFromAtlas((subGraphics[0] as Graphic_Appearances).SubGraphicFor(PB_DefOf.Fabric).MatSingle, LinkDirections.None);

    public override void Init(GraphicRequest req)
    {
        subGraphics = new Graphic[suffixes.Count];
        data = req.graphicData;
        path = req.path;
        color = req.color;
        drawSize = req.drawSize;
        for (var i = 0; i < suffixes.Count; i++)
        {
            subGraphics[i] = GraphicDatabase.Get<Graphic_Appearances>(req.path + suffixes[i], req.shader, req.drawSize, req.color, Color.white, req.graphicData);
        }
    }

    public override Material MatSingleFor(Thing thing)
    {
        var subGraphic = SubGraphicFor(thing);
        if (subGraphic == null)
        {
            subGraphic = SubGraphicFor(PB_DefOf.Fabric);
        }
        return subGraphic.MatSingleFor(thing);
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
        return GraphicDatabase.Get<Graphic_Tent>(path, newShader, drawSize, newColor, Color.white, data, null);
    }
}
