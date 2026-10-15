using UnityEngine;
using Verse;

namespace PortableBlueprint;

public class Settings : ModSettings
{
	public const string IncludeFloors = "PB.IncludeFloors";
	public const string DeconstructOrUninstall = "PB.DeconstructOrUninstall";
	public const string BuildFromMap = "PB.BuildFromMap";
	public const string BuildFromInventory = "PB.BuildFromInventory";
	
	public Vector2 windowPosition = new(0f, 0f);

	public Dictionary<string, bool> makeBlueprintSettings = new()
	{
		{ IncludeFloors, false },
		{ DeconstructOrUninstall, false }
	};

	public Dictionary<string, bool> placeBlueprintSettings = new()
	{
		{ BuildFromMap, true },
		{ BuildFromInventory, true }
	};

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref windowPosition, nameof(windowPosition), new Vector2(0f, 0f));
		Scribe_Collections.Look(ref makeBlueprintSettings, nameof(makeBlueprintSettings), LookMode.Value, LookMode.Value);
		Scribe_Collections.Look(ref placeBlueprintSettings, nameof(placeBlueprintSettings), LookMode.Value, LookMode.Value);
		if (Scribe.mode is LoadSaveMode.LoadingVars)
		{
			makeBlueprintSettings ??= [];
			makeBlueprintSettings.TryAdd(IncludeFloors, false);
			makeBlueprintSettings.TryAdd(DeconstructOrUninstall, false);
			placeBlueprintSettings ??= [];
			placeBlueprintSettings.TryAdd(BuildFromMap, true);
			placeBlueprintSettings.TryAdd(BuildFromInventory, true);
		}
	}
}