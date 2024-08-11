using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace CarryableBlueprint
{
    public class UI_MakeBlueprint
    {
        protected int ContentCount => CarryableBlueprint.settings.makeBlueprintSettings.Count;

        protected Vector2 WindowSize => new Vector2(300f, Text.LineHeightOf(GameFont.Small) + this.Margin * 2f + 4f + Text.LineHeightOf(GameFont.Small) * this.ContentCount);

        protected float Margin => 6f;

        public UI_MakeBlueprint()
        {
            this.windowRect = new Rect(CarryableBlueprint.settings.windowPosition, this.WindowSize);
        }

        public void DoWindowContents()
        {
            Rect labelRect;
            var inRect = this.windowRect.AtZero().ContractedBy(this.Margin);
            using (new TextBlock(GameFont.Small))
            {
                labelRect = new Rect(inRect.x, inRect.y, inRect.width, Text.LineHeight);
                Widgets.Label(labelRect, "CB.DesignatorMakeBlueprint".Translate());
                Widgets.DrawLineHorizontal(labelRect.x, labelRect.yMax, labelRect.width);
            }
                
            GUI.DragWindow(labelRect);
            if (Mouse.IsOver(labelRect))
            {
                if (Input.GetMouseButton(0))
                {
                    Window window = Find.WindowStack.Windows.FirstOrDefault(w => w.ID == -15254158);
                    this.windowRect.position = window.windowRect.position;
                }
                if (Input.GetMouseButtonUp(0))
                {
                    CarryableBlueprint.settings.windowPosition = this.windowRect.position;
                    CarryableBlueprint.settings.Write();
                }
            }

            var contentRect = new Rect(inRect.x, labelRect.yMax + 4f, inRect.width, inRect.height - labelRect.height);
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(contentRect);
            var settings = CarryableBlueprint.settings.makeBlueprintSettings;
            for (var i = 0; i < settings.Count; i++)
            {
                var rect = listing_Standard.GetRect(Text.CalcHeight(settings.ElementAt(i).Key, listing_Standard.ColumnWidth));
                var rect2 = rect.RightPartPixels(24f);
                if (Widgets.ButtonInvisible(rect))
                {
                    CarryableBlueprint.settings.Write();
                }
                var setting = settings.ElementAt(i).Value;
                Widgets.CheckboxLabeled(rect, settings.ElementAt(i).Key.Translate(), ref setting);
                settings[settings.ElementAt(i).Key] = setting;
            }
            listing_Standard.End();
        }

        public Rect windowRect;
    }
}
