using UnityEngine;
using Verse;

namespace PortableBlueprint;

public class Settings : ModSettings
{
	public Vector2 windowPosition = new(0f, 0f);

	public Dictionary<string, bool> makeBlueprintSettings = new()
	{
		{ "PB.IncludeFloors", false },
		{ "PB.DeconstructOrUninstall", false }
	};

	public Dictionary<string, bool> placeBlueprintSettings = new()
	{
		{ "PB.BuildFromMap", true },
		{ "PB.BuildFromInventory", true }
	};

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref windowPosition, nameof(windowPosition), new Vector2(0f, 0f));
		Scribe_Collections.Look(ref makeBlueprintSettings, nameof(makeBlueprintSettings), LookMode.Value, LookMode.Value);
		Scribe_Collections.Look(ref placeBlueprintSettings, nameof(placeBlueprintSettings), LookMode.Value, LookMode.Value);
	}
}