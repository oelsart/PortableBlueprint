using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace PortableBlueprint.Tent;


public class Building_TentDoor : Building_Door
{
    private Graphic_LinkedTent topGraphic;

    private Graphic_LinkedTent[] moverGraphic;

    private LinkDirections? linkSet;

    private Graphic_LinkedTent TopGraphic
    {
        get
        {
            if (topGraphic == null)
            {
                if (def.GetModExtension<TentDoorGraphics>().graphicDataTop == null)
                {
                    Log.Error("Tent flap must have modExtensions: TentDoorGraphics.");
                    return null;
                }
                topGraphic = def.GetModExtension<TentDoorGraphics>().graphicDataTop.GraphicColoredFor(this) as Graphic_LinkedTent;
                if (topGraphic == null)
                {
                    Log.Error("Tent flap must have graphicClass: Graphic_Tent.");
                }
            }
            return topGraphic;
        }
    }

    private Graphic_LinkedTent[] MoverGraphic
    {
        get
        {
            if (moverGraphic == null)
            {
                if (def.GetModExtension<TentDoorGraphics>().graphicDataMover == null)
                {
                    Log.Error("Tent flap must have modExtensions: TentDoorGraphics.");
                    return null;
                }
                moverGraphic = [
                    def.GetModExtension<TentDoorGraphics>().graphicDataMover[0].GraphicColoredFor(this) as Graphic_LinkedTent,
                    def.GetModExtension<TentDoorGraphics>().graphicDataMover[1].GraphicColoredFor(this) as Graphic_LinkedTent
                ];
                if (moverGraphic.Any(g => g == null))
                {
                    Log.Error("Tent flap must have graphicClass: Graphic_Tent.");
                }
            }
            return moverGraphic;
        }
    }

    public override void Print(SectionLayer layer)
    {
        linkSet = TopGraphic.GetLinkSet(this, Position);
        TopGraphic.Print(layer, this, 0f, AltitudeLayer.Blueprint.AltitudeFor(), linkSet);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        linkSet ??= TopGraphic.GetLinkSet(this, Position);
        float offsetDist = 0f + (0.45f * OpenPct);
        DrawMovers(drawLoc, offsetDist, AltitudeLayer.DoorMoveable.AltitudeFor(), Vector3.one, Graphic.ShadowGraphic);
    }

    protected void DrawMovers(Vector3 drawPos, float offsetDist, float altitude, Vector3 drawScaleFactor, Graphic_Shadow shadowGraphic)
    {
        for (int i = 0; i < 2; i++)
        {
            Vector3 vector;
            Mesh mesh;
            LinkDirections linkSet = (LinkDirections)this.linkSet;
            if (linkSet.HasFlag(LinkDirections.Up) || linkSet.HasFlag(LinkDirections.Down))
            {
                vector = new Vector3(0f, 0f, i == 0 ? -def.size.x : def.size.x);
            }
            else
            {
                vector = new Vector3(i == 0 ? -def.size.z : def.size.z, 0f, 0f);
            }
            mesh = MeshPool.plane10;
            Vector3 vector2 = drawPos;
            vector2.y = altitude;
            vector2 += vector * offsetDist;
            Graphics.DrawMesh(mesh, Matrix4x4.TRS(vector2, Quaternion.identity, new Vector3(def.size.x * drawScaleFactor.x, drawScaleFactor.y, def.size.z * drawScaleFactor.z)), MoverGraphic[i].MaterialFor(this, Position, linkSet), 0);
            shadowGraphic?.DrawWorker(vector2, Rotation, def, this, 0f);
        }
    }

    protected override void Tick()
    {
        if (Spawned)
        {
            base.Tick();
        }
    }
}
