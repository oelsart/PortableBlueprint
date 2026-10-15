using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;

namespace PortableBlueprint;

public class UI_MakeBlueprint
{
	public Dictionary<string, bool> curSettings;

	public Rect windowRect;

	protected int ContentCount => curSettings.Count;

	protected float WindowWidth => curSettings.Max(s => Text.CalcSize(s.Key.Translate()).x) + 30f + Margin + 2f;

	protected Vector2 WindowSize => new(WindowWidth,
		Text.LineHeightOf(GameFont.Small) + Margin * 2f + 4f + Text.LineHeightOf(GameFont.Small) * ContentCount);

	protected const float Margin = 6f;

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
			if (Event.current is { type: EventType.MouseDrag, button: 0 })
			{
				if (Find.WindowStack.Windows.FirstOrDefault(w => w.ID == -15254158) is { } window)
					windowRect.position = window.windowRect.position;
			}

			if (Event.current is { type: EventType.MouseUp, button: 0 })
			{
				PortableBlueprint.settings.windowPosition = windowRect.position;
				PortableBlueprint.settings.Write();
			}
		}

		var contentRect = new Rect(inRect.x, labelRect.yMax + 4f, inRect.width, inRect.height - labelRect.height);
		Listing_Standard listing_Standard = new();
		listing_Standard.Begin(contentRect);
		var settings = curSettings;
		foreach (var (key, _) in settings)
		{
			var rect = listing_Standard.GetRect(Text.CalcHeight(key, listing_Standard.ColumnWidth));
			if (Widgets.ButtonInvisible(rect))
			{
				PortableBlueprint.settings.Write();
			}

			ref var setting = ref CollectionsMarshal.GetValueRefOrNullRef(settings, key);
			if (!Unsafe.IsNullRef(ref setting))
			{
				Widgets.CheckboxLabeled(rect, key.Translate(), ref setting);
			}
		}

		listing_Standard.End();
	}
}