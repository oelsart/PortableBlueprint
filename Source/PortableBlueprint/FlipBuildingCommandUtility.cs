using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace PortableBlueprint
{
    public static class FlipBuildingCommandUtility
    {
        public static Command FlipCommand(Building_DrawingTable building, BuildableDef buildable, ThingDef stuff = null, Precept_Building sourcePrecept = null, ThingStyleDef style = null, bool styleOverridden = false, string label = null, string description = null, bool allowHotKey = false, ColorInt? glowerColorOverride = null, Texture2D commandIcon = null, Designator_Build des = null)
        {
            if (label == null)
            {
                label = buildable.label;
            }
            if (description == null)
            {
                description = buildable.description;
            }
            Command_FlipBuilding command_FlipBuilding = new Command_FlipBuilding
            {
                action = delegate ()
            {
                building.Flipped = !building.Flipped;
                if (building.InteractionCell.GetThingList(building.Map).Any(t =>
                {
                    if (t.def.passability != Traversability.Standable || t.def == building.def)
                    {
                        Messages.Message("InteractionSpotWillBeBlocked".Translate(t.LabelNoCount, t).CapitalizeFirst(), MessageTypeDefOf.RejectInput);
                        return true;
                    }
                    var entityDefToBuild = t.def.entityDefToBuild;
                    if (entityDefToBuild != null && (entityDefToBuild.passability != Traversability.Standable || entityDefToBuild == building.def))
                    {
                        Messages.Message("InteractionSpotWillBeBlocked".Translate(t.LabelNoCount, t).CapitalizeFirst(), MessageTypeDefOf.RejectInput);
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
                icon = des.ResolvedIcon(style),
                iconProportions = des.iconProportions,
                iconDrawScale = des.iconDrawScale,
                iconTexCoords = des.iconTexCoords,
                iconAngle = des.iconAngle,
                iconOffset = des.iconOffset,
                Order = 9f
            };
            command_FlipBuilding.SetColorOverride(des.IconDrawColor);
            if (stuff != null)
            {
                command_FlipBuilding.defaultIconColor = buildable.GetColorForStuff(stuff);
            }
            else
            {
                command_FlipBuilding.defaultIconColor = buildable.uiIconColor;
            }
            command_FlipBuilding.commandIcon = commandIcon;
            return command_FlipBuilding;
        }
    }
}
