using RimWorld;
using UnityEngine;
using Verse;

namespace PortableBlueprint;

public sealed class Dialog_GiveBlueprintName : Window
{
	private const int FirstCharLimit = 64;

	public override Vector2 InitialSize => new(500f, 150f);

	public Dialog_GiveBlueprintName(CompBlueprint comp, Action<string> Named)
	{
		forcePause = true;
		closeOnAccept = false;
		closeOnCancel = false;
		absorbInputAroundWindow = true;

		this.Named = Named;
		nameMessageKey = "PB.NameBlueprint".Translate();
		invalidNameMessageKey = "PB.BlueprintMustBeNamed".Translate();
		curName = comp.BlueprintName;
	}

	public Dialog_GiveBlueprintName(Building_DrawingTable drawingTable, IntVec3 pos, Action<string> Named)
	{
		forcePause = true;
		closeOnAccept = false;
		closeOnCancel = false;
		absorbInputAroundWindow = true;

		this.Named = Named;
		nameMessageKey = "PB.NameBlueprint".Translate();
		invalidNameMessageKey = "PB.BlueprintMustBeNamed".Translate();
		var roomLabel = pos.GetRoom(drawingTable.Map)?.GetRoomRoleLabel();
		var blueprintNames = Find.Maps.SelectMany(m => m.listerThings.ThingsOfDef(PB_DefOf.PB_BlueprintPaper))
			.Select(b => b.TryGetComp<CompBlueprint>()?.BlueprintName ?? "")
			.Concat(drawingTable.billStack.Bills.Select(b => (b as Bill_Blueprint)?.BlueprintName ?? "")).ToList();
		var maxCount = 0;
		if (roomLabel != null)
		{
			if (!blueprintNames.EnumerableNullOrEmpty())
			{
				maxCount = blueprintNames.Max(n =>
				{
					var index = Math.Max(0, n.IndexOf(roomLabel, StringComparison.Ordinal));
					int.TryParse(n[Math.Min(n.Length - 1, index + roomLabel.Length)..], out var count);
					return count;
				});
			}
		}
		else
		{
			if (!blueprintNames.EnumerableNullOrEmpty())
			{
				maxCount = blueprintNames.Max(n =>
				{
					int.TryParse(n[..Math.Min(n.Length - 1, 3)], out var count);
					return count;
				});
			}
		}

		curName = roomLabel + (maxCount + 1);
	}

	public override void DoWindowContents(Rect rect)
	{
		Text.Font = GameFont.Small;
		var enter = false;
		var escape = false;
		if (Event.current.type == EventType.KeyDown)
		{
			if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
			{
				enter = true;
				Event.current.Use();
			}

			if (Event.current.keyCode == KeyCode.Escape)
			{
				escape = true;
				Event.current.Use();
			}
		}

		var num = 0f;
		using (new TextBlock(TextAnchor.UpperCenter))
		{
			Widgets.Label(rect, nameMessageKey);
			num += Text.CalcHeight(nameMessageKey, rect.width) + 10f;
		}

		curName = Widgets.TextField(new Rect(0f, num, rect.width, 35f), curName, FirstCharLimit);
		Rect rect2 = new(0f, rect.height - 35f, rect.width / 2f - 5f, 35f);
		if (Widgets.ButtonText(rect2, "OK".Translate()) || enter)
		{
			if (IsValidName(curName))
			{
				Named(curName);
				Find.WindowStack.TryRemove(this);
			}
			else
			{
				Messages.Message(invalidNameMessageKey.Translate(), MessageTypeDefOf.RejectInput, false);
			}

			Event.current.Use();
		}

		Rect rect3 = new(rect.width / 2f + 5f, rect.height - 35f, rect.width / 2f - 5f, 35f);

		if (Widgets.ButtonText(rect3, "Cancel".Translate()) || escape)
		{
			Find.WindowStack.TryRemove(this);
		}
	}

	private static bool IsValidName(string s) => s.Length > 0;

	private string curName;

	private readonly string nameMessageKey;

	private readonly string invalidNameMessageKey;

	private readonly Action<string> Named;
}