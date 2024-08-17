using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.Steam;

namespace PortableBlueprint
{
    public class Designator_PlaceBlueprint : Designator
    {
        private List<IntVec3> OffsetPos
        {
            get
            {
                return buildingLayoutList.Select(building =>
                {
                    var pos = building.pos;
                    if (!building.def.rotatable)
                    {
                        if (globalRot == Rot4.West || globalRot == Rot4.South) pos.x -= (building.def.Size.x - 1) % 2;
                        if (globalRot == Rot4.South || globalRot == Rot4.East) pos.z -= (building.def.Size.z - 1) % 2;
                    }

                    if (Event.current.shift)
                    {
                        pos.x = -pos.x;
                        if (!building.def.rotatable)
                        {
                            pos.x -= (building.def.Size.x - 1) % 2;
                        }
                        else
                        {
                            pos.x += ((globalRot.AsInt + building.rot.AsInt) % 4 - 1) % 2 * (building.def.Size.x - 1) % 2;
                            pos.z -= ((globalRot.AsInt + 1 + building.rot.AsInt) % 4 - 1) % 2 * (building.def.Size.x - 1) % 2;
                        }
                    }
                    return pos;
                }).ToList();
            }
        }

        private List<Rot4> FlipRot
        {
            get
            {
                return buildingLayoutList.Select(building =>
                {
                    if (Event.current.shift && building.rot.IsHorizontal && building.def.rotatable)
                    {
                        building.rot = building.rot.Opposite;
                    }
                    return building.rot;
                }).ToList();
            }
        }

        public Designator_PlaceBlueprint(CompBlueprint comp)
        {
            this.comp = comp;
            this.defaultLabel = "PB.DesignatorPlaceBlueprint".Translate();
            this.defaultDesc = "PB.DesignatorPlaceBlueprintDesc".Translate();
            this.icon = ContentFinder<Texture2D>.Get("PortableBlueprint/UI/PlaceBlueprint", true);
            this.useMouseIcon = true;
            this.Order = -9f;
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            var buildingLayoutList = this.comp.BuildingLayoutList;
            if (!buildingLayoutList.NullOrEmpty())
            {
                this.buildingLayoutList = buildingLayoutList.Select(b => new BuildingLayout(b.def, b.stuff, b.pos, b.rot, b.style, b.precept)).ToList();
            }
            var floorLayoutList = this.comp.FloorayoutList;
            if (!floorLayoutList.NullOrEmpty())
            {
                this.floorLayoutList = floorLayoutList.Select(f => new FloorLayout(f.def, f.pos)).ToList();
            }
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            return canDesignate;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            if (!this.buildingLayoutList.NullOrEmpty())
            {
                for (var i = 0; i < buildingLayoutList.Count; i++)
                {
                    GenConstruct.PlaceBlueprintForBuild(buildingLayoutList[i].def, c + offsetPositions[i], Map, flippedRotations[i], Faction.OfPlayer, buildingLayoutList[i].stuff, buildingLayoutList[i].precept, buildingLayoutList[i].style);
                }
            }
            if (!this.floorLayoutList.NullOrEmpty())
            {
                for (var i = 0; i < floorLayoutList.Count; i++)
                {
                    GenConstruct.PlaceBlueprintForBuild(floorLayoutList[i].def, c + new IntVec3(Event.current.shift ? -floorLayoutList[i].pos.x : floorLayoutList[i].pos.x, 0, floorLayoutList[i].pos.z), Map, Rot4.North, Faction.OfPlayer, null);
                }
            }
            Find.DesignatorManager.Deselect();
        }

        public override void SelectedProcessInput(Event ev)
        {
            this.HandleRotationShortcuts();
        }

        public override void SelectedUpdate()
        {
            IntVec3 center = UI.MouseCell();
            canDesignate = AcceptanceReport.WasAccepted;

            if (!this.buildingLayoutList.NullOrEmpty())
            {
                this.offsetPositions = this.OffsetPos;
                this.flippedRotations = this.FlipRot;
                for (var i = 0; i < this.buildingLayoutList.Count; i++)
                {
                    Color ghostCol = Designator_Place.CanPlaceColor;
                    AcceptanceReport result;
                    if ((result = GenConstruct.CanPlaceBlueprintAt(buildingLayoutList[i].def, this.offsetPositions[i] + center, flippedRotations[i], Map, DebugSettings.godMode)) == AcceptanceReport.WasRejected)
                    {
                        ghostCol = Designator_Place.CannotPlaceColor;
                        this.canDesignate = result;
                    }
                    GhostDrawer.DrawGhostThing(center + offsetPositions[i], flippedRotations[i], buildingLayoutList[i].def, null, ghostCol, AltitudeLayer.Blueprint);
                }
            }
            if (!this.floorLayoutList.NullOrEmpty())
            {
                foreach (var floor in this.floorLayoutList)
                {
                    Color ghostCol = Designator_Place.CanPlaceColor;
                    AcceptanceReport result;
                    if ((result = GenConstruct.CanPlaceBlueprintAt(floor.def, floor.pos + center, Rot4.North, Map, DebugSettings.godMode)) == AcceptanceReport.WasRejected)
                    {
                        ghostCol = Designator_Place.CannotPlaceColor;
                        this.canDesignate = result;
                    }
                    var blueprintGraphic = floor.def.blueprintDef.graphic;
                    var material = blueprintGraphic.GetColoredVersion(blueprintGraphic.Shader, ghostCol, Color.white).MatSingle;
                    Graphics.DrawMesh(MeshPool.plane10, (center + floor.pos).ToVector3ShiftedWithAltitude(AltitudeLayer.Blueprint), Quaternion.identity, material, 0);
                }
            }
        }

        public override void DoExtraGuiControls(float leftX, float bottomY)
        {
            Rect winRect = new Rect(leftX, bottomY - 120f, 200f, 120f);
            Find.WindowStack.ImmediateWindow(73095, winRect, WindowLayer.GameUI, delegate
            {
                RotationDirection rotationDirection = RotationDirection.None;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                Rect rect = new Rect(winRect.width / 2f - 64f - 5f, 15f, 64f, 64f);
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
                Rect rect2 = new Rect(winRect.width / 2f + 5f, 15f, 64f, 64f);
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
                    this.Rotate(rotationDirection);
                }
                Widgets.Label(new Rect(0f, winRect.height - 38f, winRect.width, 30f), "PB.HoldShiftToFlip".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }, true, false, 1f, null);
        }

        private void HandleRotationShortcuts()
        {
            RotationDirection rotationDirection = RotationDirection.None;
            if (Event.current.button == 2)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    Event.current.Use();
                    this.middleMouseDownTime = Time.realtimeSinceStartup;
                }
                if (Event.current.type == EventType.MouseUp && Time.realtimeSinceStartup - this.middleMouseDownTime < 0.15f)
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
                this.Rotate(rotationDirection);
            }
        }

        public override void Rotate(RotationDirection rotDir)
        {
            if (!this.buildingLayoutList.NullOrEmpty())
            {
                foreach (var building in this.buildingLayoutList)
                {
                    building.pos = building.pos.RotatedBy(rotDir);
                    building.rot = building.def.rotatable ? building.rot.Rotated(rotDir) : building.rot;
                }
            }
            if (!this.floorLayoutList.NullOrEmpty())
            {
                foreach (var floor in this.floorLayoutList)
                {
                    floor.pos = floor.pos.RotatedBy(rotDir);
                }
            }
            globalRot = globalRot.Rotated(rotDir);
        }

        private readonly CompBlueprint comp;

        private List<BuildingLayout> buildingLayoutList;

        private List<FloorLayout> floorLayoutList;

        private List<IntVec3> offsetPositions = new List<IntVec3>();

        private List<Rot4> flippedRotations = new List<Rot4>();

        private Rot4 globalRot = Rot4.North;

        private float middleMouseDownTime;

        private AcceptanceReport canDesignate;
    }
}
