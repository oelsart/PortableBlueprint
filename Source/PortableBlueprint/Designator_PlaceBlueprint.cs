using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.Steam;

namespace PortableBlueprint;

public class Designator_PlaceBlueprint : Designator
{
    private readonly CompBlueprint comp;

    private List<BuildingLayout> buildingLayoutList;

    private List<FloorLayout> floorLayoutList;

    private Rot4 globalRot = Rot4.North;

    private float middleMouseDownTime;

    private AcceptanceReport canDesignate;

    public Designator_PlaceBlueprint(CompBlueprint comp)
    {
        this.comp = comp;
        defaultLabel = "PB.DesignatorPlaceBlueprint".Translate();
        defaultDesc = "PB.DesignatorPlaceBlueprintDesc".Translate();
        icon = ContentFinder<Texture2D>.Get("PortableBlueprint/UI/PlaceBlueprint", true);
        useMouseIcon = true;
        Order = -9f;
    }

    public override void ProcessInput(Event ev)
    {
        base.ProcessInput(ev);
        var buildingLayoutList = comp.BuildingLayoutList;
        if (!buildingLayoutList.NullOrEmpty())
        {
            this.buildingLayoutList = [.. buildingLayoutList];
        }
        var floorLayoutList = comp.FloorLayoutList;
        if (!floorLayoutList.NullOrEmpty())
        {
            this.floorLayoutList = [.. floorLayoutList];
        }
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        return canDesignate;
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        if (!buildingLayoutList.NullOrEmpty())
        {
            for (var i = 0; i < buildingLayoutList.Count; i++)
            {
                var buildingLayout = buildingLayoutList[i];
                var pos = c + buildingLayout.PositionForRot(globalRot);
                var rot = buildingLayout.RotatedRot(globalRot);
                GenSpawn.WipeExistingThings(pos, rot, buildingLayout.def.blueprintDef, Map, DestroyMode.Deconstruct);
                MinifiedThing minifiedThing;
                if (buildingLayout.def.Minifiable &&
                    (minifiedThing = GenConstructEx.FindMinifiedThing(Map, buildingLayout.def, buildingLayout.stuff, buildingLayout.style, buildingLayout.precept)) != null)
                {
                    if (minifiedThing is MinifiedThingStackable minifiedThingStackable)
                    {
                        minifiedThingStackable.blueprintsForDrawLine.Add(GenConstructEx.PlaceBlueprintForInstallEnroute(minifiedThingStackable, pos, Map, rot, Faction.OfPlayer));
                        Delay.AfterNTicks(0, () =>
                        {
                            if (!minifiedThingStackable.Spawned && minifiedThingStackable.holdingOwner != null)
                            {
                                minifiedThingStackable.holdingOwner.TryDrop(minifiedThingStackable, ThingPlaceMode.Near, out _);
                            }
                        });
                    }
                    else
                    {
                        GenConstruct.PlaceBlueprintForInstall(minifiedThing, pos, Map, rot, Faction.OfPlayer);
                    }
                }
                else
                {
                    GenConstruct.PlaceBlueprintForBuild(buildingLayout.def, pos, Map, rot, Faction.OfPlayer, buildingLayout.stuff, buildingLayout.precept, buildingLayout.style);
                }
                FleckMaker.ThrowMetaPuffs(GenAdj.OccupiedRect(pos, rot, buildingLayout.def.Size), Map);
            }
        }
        if (!floorLayoutList.NullOrEmpty())
        {
            for (var i = 0; i < floorLayoutList.Count; i++)
            {
                var floorLayout = floorLayoutList[i];
                var pos = c + floorLayout.PositionForRot(globalRot);
                if (floorLayout.foundationDef != null)
                {
                    GenSpawn.WipeExistingThings(pos, Rot4.North, floorLayout.foundationDef.blueprintDef, Map, DestroyMode.Deconstruct);
                    GenConstruct.PlaceBlueprintForBuild(floorLayout.foundationDef, pos, Map, Rot4.North, Faction.OfPlayer, null);
                }
                if (floorLayout.topDef != null)
                {
                    GenSpawn.WipeExistingThings(pos, Rot4.North, floorLayout.topDef.blueprintDef, Map, DestroyMode.Deconstruct);
                    GenConstruct.PlaceBlueprintForBuild(floorLayout.topDef, pos, Map, Rot4.North, Faction.OfPlayer, null);
                }
            }
        }
        Find.DesignatorManager.Deselect();
    }

    public override void SelectedProcessInput(Event ev)
    {
        HandleRotationShortcuts();
    }

    public override void SelectedUpdate()
    {
        IntVec3 center = UI.MouseCell();
        canDesignate = AcceptanceReport.WasAccepted;

        if (!buildingLayoutList.NullOrEmpty())
        {
            foreach (var layout in buildingLayoutList)
            {
                Color ghostCol = Designator_Place.CanPlaceColor;
                AcceptanceReport result;
                var pos = layout.PositionForRot(globalRot);
                var rot = layout.RotatedRot(globalRot);
                if ((result = GenConstruct.CanPlaceBlueprintAt(layout.def, center + pos, rot, Map, DebugSettings.godMode)) == AcceptanceReport.WasRejected)
                {
                    ghostCol = Designator_Place.CannotPlaceColor;
                    canDesignate = result;
                }
                GhostDrawer.DrawGhostThing(center + pos, rot, layout.def, null, ghostCol, AltitudeLayer.Blueprint);
            }
        }
        if (!floorLayoutList.NullOrEmpty())
        {
            foreach (var layout in floorLayoutList)
            {
                var pos = layout.PositionForRot(globalRot);
                if (layout.foundationDef != null)
                {
                    Color ghostCol = Designator_Place.CanPlaceColor;
                    AcceptanceReport result;
                    if ((result = GenConstruct.CanPlaceBlueprintAt(layout.foundationDef, center + pos, Rot4.North, Map, DebugSettings.godMode)) == AcceptanceReport.WasRejected)
                    {
                        ghostCol = Designator_Place.CannotPlaceColor;
                        canDesignate = result;
                    }
                    var blueprintGraphic = layout.foundationDef.blueprintDef.graphic;
                    var material = blueprintGraphic.GetColoredVersion(blueprintGraphic.Shader, ghostCol, Color.white).MatSingle;
                    Graphics.DrawMesh(MeshPool.plane10, (center + pos).ToVector3ShiftedWithAltitude(AltitudeLayer.Blueprint), Quaternion.identity, material, 0);
                }
                if (layout.topDef != null)
                {
                    Color ghostCol = Designator_Place.CanPlaceColor;
                    AcceptanceReport result;
                    if ((result = GenConstruct.CanPlaceBlueprintAt(layout.topDef, center + pos, Rot4.North, Map, DebugSettings.godMode)) == AcceptanceReport.WasRejected)
                    {
                        ghostCol = Designator_Place.CannotPlaceColor;
                        canDesignate = result;
                    }
                    var blueprintGraphic = layout.topDef.blueprintDef.graphic;
                    var material = blueprintGraphic.GetColoredVersion(blueprintGraphic.Shader, ghostCol, Color.white).MatSingle;
                    Graphics.DrawMesh(MeshPool.plane10, (center + pos).ToVector3ShiftedWithAltitude(AltitudeLayer.Blueprint), Quaternion.identity, material, 0);
                }
            }
        }
    }

    public override void DoExtraGuiControls(float leftX, float bottomY)
    {
        Rect winRect = new(leftX, bottomY - 120f, 200f, 120f);
        Find.WindowStack.ImmediateWindow(73095, winRect, WindowLayer.GameUI, delegate
        {
            RotationDirection rotationDirection = RotationDirection.None;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Medium;
            Rect rect = new((winRect.width / 2f) - 64f - 5f, 15f, 64f, 64f);
            if (Widgets.ButtonImage(rect, TexUI.RotLeftTex, true, null))
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
                rotationDirection = RotationDirection.Counterclockwise;
                Event.current.Use();
            }
            if (!SteamDeck.IsSteamDeck)
            {
                Widgets.Label(rect, KeyBindingDefOf.Designator_RotateLeft.MainKeyLabel);
            }
            Rect rect2 = new((winRect.width / 2f) + 5f, 15f, 64f, 64f);
            if (Widgets.ButtonImage(rect2, TexUI.RotRightTex, true, null))
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
                rotationDirection = RotationDirection.Clockwise;
                Event.current.Use();
            }
            if (!SteamDeck.IsSteamDeck)
            {
                Widgets.Label(rect2, KeyBindingDefOf.Designator_RotateRight.MainKeyLabel);
            }
            if (rotationDirection != RotationDirection.None)
            {
                Rotate(rotationDirection);
            }
            Widgets.Label(new Rect(0f, winRect.height - 38f, winRect.width, 30f), "PB.HoldShiftToFlip".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }, true, false, 1f, null);

        PortableBlueprint.UI.curSettings = PortableBlueprint.settings.placeBlueprintSettings;
        Find.WindowStack.ImmediateWindow(15254158, PortableBlueprint.UI.windowRect, WindowLayer.GameUI, PortableBlueprint.UI.DoWindowContents);
    }

    private void HandleRotationShortcuts()
    {
        RotationDirection rotationDirection = RotationDirection.None;
        if (Event.current.button == 2)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                Event.current.Use();
                middleMouseDownTime = Time.realtimeSinceStartup;
            }
            if (Event.current.type == EventType.MouseUp && Time.realtimeSinceStartup - middleMouseDownTime < 0.15f)
            {
                rotationDirection = RotationDirection.Clockwise;
            }
        }
        if (KeyBindingDefOf.Designator_RotateRight.KeyDownEvent)
        {
            rotationDirection = RotationDirection.Clockwise;
        }
        if (KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent)
        {
            rotationDirection = RotationDirection.Counterclockwise;
        }
        if (rotationDirection != RotationDirection.None)
        {
            Rotate(rotationDirection);
        }
    }

    public void Rotate(RotationDirection rotDir)
    {
        globalRot = globalRot.Rotated(rotDir);
    }
}
