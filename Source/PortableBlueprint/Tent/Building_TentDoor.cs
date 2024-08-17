using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace PortableBlueprint.Tent
{

    public class Building_TentDoor : Building_Door
    {
        private Graphic_LinkedTent TopGraphic
        {
            get
            {
                if (this.topGraphic == null)
                {
                    if (this.def.GetModExtension<TentDoorGraphics>().graphicDataTop == null)
                    {
                        Log.Error("Tent door must have modExtensions: TentDoorGraphics.");
                        return null;
                    }
                    this.topGraphic = this.def.GetModExtension<TentDoorGraphics>().graphicDataTop.GraphicColoredFor(this) as Graphic_LinkedTent;
                    if (this.topGraphic == null) 
                    {
                        Log.Error("Tent door must have graphicClass: Graphic_Tent.");
                    }
                }
                return this.topGraphic;
            }
        }

        private Graphic_LinkedTent[] MoverGraphic
        {
            get
            {
                if (this.moverGraphic == null)
                {
                    if (this.def.GetModExtension<TentDoorGraphics>().graphicDataMover == null)
                    {
                        Log.Error("Tent door must have modExtensions: TentDoorGraphics.");
                        return null;
                    }
                    this.moverGraphic = new Graphic_LinkedTent[] {
                        this.def.GetModExtension<TentDoorGraphics>().graphicDataMover[0].GraphicColoredFor(this) as Graphic_LinkedTent,
                        this.def.GetModExtension<TentDoorGraphics>().graphicDataMover[1].GraphicColoredFor(this) as Graphic_LinkedTent
                    };
                    if (this.moverGraphic.Any(g => g == null))
                    {
                        Log.Error("Tent door must have graphicClass: Graphic_Tent.");
                    }
                }
                return this.moverGraphic;
            }
        }

        public override void Print(SectionLayer layer)
        {
            this.linkSet = this.TopGraphic.GetLinkSet(this, this.Position);
            this.TopGraphic.Print(layer, this, 0f, AltitudeLayer.Blueprint.AltitudeFor(), linkSet);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (this.linkSet == null)
            {
                this.TopGraphic.GetLinkSet(this, this.Position);
            }
            float offsetDist = 0f + 0.45f * this.OpenPct;
            this.DrawMovers(drawLoc, offsetDist, AltitudeLayer.DoorMoveable.AltitudeFor(), Vector3.one, this.Graphic.ShadowGraphic);
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
                    vector = new Vector3(0f, 0f, i == 0 ? -this.def.size.x : this.def.size.x);
                }
                else
                {
                    vector = new Vector3(i == 0 ? -this.def.size.z : this.def.size.z, 0f, 0f);
                }
                mesh = MeshPool.plane10;
                Vector3 vector2 = drawPos;
                vector2.y = altitude;
                vector2 += vector * offsetDist;
                Graphics.DrawMesh(mesh, Matrix4x4.TRS(vector2, Quaternion.identity, new Vector3((float)this.def.size.x * drawScaleFactor.x, drawScaleFactor.y, (float)this.def.size.z * drawScaleFactor.z)), this.MoverGraphic[i].MaterialFor(this, this.Position, linkSet), 0);
                shadowGraphic?.DrawWorker(vector2, base.Rotation, this.def, this, 0f);
            }
        }

        public override void Tick()
        {
            if (this.Spawned)
            {
                base.Tick();
            }
        }

        private Graphic_LinkedTent topGraphic;

        private Graphic_LinkedTent[] moverGraphic;

        private LinkDirections? linkSet;
    }
}
 