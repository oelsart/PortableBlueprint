using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PortableBlueprint;

public class UI_MakeBlueprint
{
    public Dictionary<string, bool> curSettings;

    public Rect windowRect;

    protected int ContentCount => curSettings.Count;

    protected float WindowWidth => curSettings.Max(s => Text.CalcSize(s.Key.Translate()).x) + 30f + Margin + 2f;

    protected Vector2 WindowSize => new(WindowWidth, Text.LineHeightOf(GameFont.Small) + (Margin * 2f) + 4f + (Text.LineHeightOf(GameFont.Small) * ContentCount));

    protected float Margin => 6f;

    public UI_MakeBlueprint()
    {
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            curSettings = PortableBlueprint.settings.makeBlueprintSettings;
            windowRect = new Rect(PortableBlueprint.settings.windowPosition, WindowSize);
        });
    }

    public void DoWindowContents()
    {
        if (curSettings is null) return;
        windowRect.size = WindowSize;
        Rect labelRect;
        var inRect = windowRect.AtZero().ContractedBy(Margin);
        using (new TextBlock(GameFont.Small))
        {
            labelRect = new Rect(inRect.x, inRect.y, inRect.width, Text.LineHeight);
            Widgets.Label(labelRect, "PB.DesignatorMakeBlueprint".Translate());
            Widgets.DrawLineHorizontal(labelRect.x, labelRect.yMax, labelRect.width);
        }

        GUI.DragWindow(labelRect);
        if (Mouse.IsOver(labelRect))
        {
            if (Input.GetMouseButton(0))
            {
                Window window = Find.WindowStack.Windows.FirstOrDefault(w => w.ID == -15254158);
                windowRect.position = window.windowRect.position;
            }
            if (Input.GetMouseButtonUp(0))
            {
                PortableBlueprint.settings.windowPosition = windowRect.position;
                PortableBlueprint.settings.Write();
            }
        }

        var contentRect = new Rect(inRect.x, labelRect.yMax + 4f, inRect.width, inRect.height - labelRect.height);
        Listing_Standard listing_Standard = new();
        listing_Standard.Begin(contentRect);
        var settings = curSettings;
        for (var i = 0; i < settings.Count; i++)
        {
            var rect = listing_Standard.GetRect(Text.CalcHeight(settings.ElementAt(i).Key, listing_Standard.ColumnWidth));
            var rect2 = rect.RightPartPixels(24f);
            if (Widgets.ButtonInvisible(rect))
            {
                PortableBlueprint.settings.Write();
            }
            var setting = settings.ElementAt(i).Value;
            Widgets.CheckboxLabeled(rect, settings.ElementAt(i).Key.Translate(), ref setting);
            settings[settings.ElementAt(i).Key] = setting;
        }
        listing_Standard.End();
    }
}
