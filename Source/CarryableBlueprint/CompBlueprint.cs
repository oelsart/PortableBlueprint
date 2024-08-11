using System.Collections.Generic;
using Verse;

namespace CarryableBlueprint
{
    public class CompBlueprint : ThingComp
    {
        public override string TransformLabel(string label)
        {
            return label + " " + this.BlueprintName;
        }

        public string BlueprintName => this.blueprintName;

        public List<BuildingLayout> BuildingLayoutList => this.buildingLayoutList;

        public List<FloorLayout> FloorayoutList => this.floorLayoutList;

        public override void Notify_RecipeProduced(Pawn pawn)
        {
            var bill = pawn.CurJob.bill;
            var blueprintBill = bill as Bill_Blueprint;
            if (blueprintBill == null) Log.Message("[Carryable Blueprint] Blueprint is produced in an invalid way.");

            this.buildingLayoutList = blueprintBill.BuildingLayoutList;
            this.floorLayoutList = blueprintBill.FloorLayoutList;
            this.blueprintName = blueprintBill.BlueprintName;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Designator_PlaceBlueprint(this);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref this.buildingLayoutList, "buildingLayout", LookMode.Deep);
            Scribe_Collections.Look(ref this.floorLayoutList, "floorLayout", LookMode.Deep);
            Scribe_Values.Look(ref this.blueprintName, "blueprintName");
        }

        public void DrawInLayout(List<BuildingLayout> layoutList) => this.buildingLayoutList = layoutList;

        private string blueprintName;

        private List<BuildingLayout> buildingLayoutList;

        private List<FloorLayout> floorLayoutList;
    }
}
