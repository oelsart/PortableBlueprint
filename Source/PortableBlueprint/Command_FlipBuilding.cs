using UnityEngine;
using Verse;

namespace PortableBlueprint
{
    public class Command_FlipBuilding : Command_Action
    {
        public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
        {
            base.DrawIcon(rect, buttonMat, parms);
            if (this.commandIcon != null)
            {
                rect.y -= 8f;
                Widgets.DrawTextureFitted(rect, this.commandIcon, 0.7f);
            }
        }

        public Texture2D commandIcon;
    }
}
