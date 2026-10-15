using RimWorld;
using UnityEngine;
using Verse;

namespace PortableBlueprint;

public static class FlipBuildingCommandUtility
{
	public static Command FlipCommand(Building_DrawingTable building, BuildableDef buildable, ThingDef stuff = null,
		Precept_Building sourcePrecept = null, ThingStyleDef style = null, bool styleOverridden = false,
		string label = null, string description = null, bool allowHotKey = false, ColorInt? glowerColorOverride = null,
		Texture2D commandIcon = null, Designator_Build des = null)
	{
		label ??= buildable.label;
		description ??= buildable.description;

		Command_FlipBuilding command_FlipBuilding = new()
		{
			action = () =>
			{
				building.Flipped = !building.Flipped;
				if (building.InteractionCell.GetThingList(building.Map).Any(t =>
				    {
					    if (t.def.passability != Traversability.Standable || t.def == building.def)
					    {
						    Messages.Message("InteractionSpotWillBeBlocked".Translate(t.LabelNoCount, t).CapitalizeFirst(),
							    MessageTypeDefOf.RejectInput);
						    return true;
					    }

					    var entityDefToBuild = t.def.entityDefToBuild;
					    if (entityDefToBuild != null && (entityDefToBuild.passability != Traversability.Standable ||
					                                     entityDefToBuild == building.def))
					    {
						    Messages.Message("InteractionSpotWillBeBlocked".Translate(t.LabelNoCount, t).CapitalizeFirst(),
							    MessageTypeDefOf.RejectInput);
						    return true;
					    }

					    return false;
				    }))
				{
					building.Flipped = !building.Flipped;
				}
				else
				{
					building.Notify_BuildingFlipped();
				}
			},
			defaultLabel = label,
			defaultDesc = description,
			Order = 9f
		};
		if (des is not null)
		{
			command_FlipBuilding.icon = des.ResolvedIcon(style);
			command_FlipBuilding.iconProportions = des.iconProportions;
			command_FlipBuilding.iconDrawScale = des.iconDrawScale;
			command_FlipBuilding.iconTexCoords = des.iconTexCoords;
			command_FlipBuilding.iconAngle = des.iconAngle;
			command_FlipBuilding.iconOffset = des.iconOffset;
			command_FlipBuilding.SetColorOverride(des.IconDrawColor);
		}

		command_FlipBuilding.defaultIconColor =
			stuff is not null ? buildable.GetColorForStuff(stuff) : buildable.uiIconColor;

		command_FlipBuilding.commandIcon = commandIcon;
		return command_FlipBuilding;
	}
}