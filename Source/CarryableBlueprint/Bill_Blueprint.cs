using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CarryableBlueprint
{
    public class Bill_Blueprint : Bill_ProductionWithUft
    {
        public override string Label => base.Label + ": " + this.BlueprintName;

        public List<BuildingLayout> BuildingLayoutList
        {
            get => this.buildingLayoutInt;
            set => this.buildingLayoutInt = value;
        }

        public List<FloorLayout> FloorLayoutList
        {
            get => this.floorLayoutInt;
            set => this.floorLayoutInt = value;
        }

        public string BlueprintName
        {
            get => this.blueprintName;
            set => this.blueprintName = value;
        }

        public Bill_Blueprint()
        {
        }

        public Bill_Blueprint(RecipeDef recipe) : base(recipe)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref buildingLayoutInt, "buildingLayout", LookMode.Deep);
            Scribe_Collections.Look(ref floorLayoutInt, "floorLayout", LookMode.Deep);
            Scribe_Values.Look(ref this.blueprintName, "blueprintName");
        }

        private List<BuildingLayout> buildingLayoutInt;

        private List<FloorLayout> floorLayoutInt;

        private string blueprintName;
    }
}
    