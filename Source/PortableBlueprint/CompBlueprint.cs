using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PortableBlueprint
{
    public class CompBlueprint : ThingComp
    {
        public string BlueprintName
        {
            get
            {
                return this.blueprintName;
            }
            set
            {
                this.blueprintName = value;
            }
        }

        public List<BuildingLayout> BuildingLayoutList => this.buildingLayoutList;

        public List<FloorLayout> FloorayoutList => this.floorLayoutList;

        private string Quotation
        {
            get
            {
                var buildingList = this.BuildingLayoutList?.GroupBy(b => b.def);
                var floorList = this.FloorayoutList?.GroupBy(b => b.def);
                var text = "";
                if (!buildingList.EnumerableNullOrEmpty())
                {
                    text += "PB.Quotation.Buildings".Translate() + ":\n";
                    foreach (var building in buildingList)
                    {
                        text += $" -{building.Key.LabelCap} {building.Count()}\n";
                    }
                    text += "\n";
                }

                if (!floorList.EnumerableNullOrEmpty())
                {
                    text += "PB.Quotation.Floors".Translate() + ":\n";
                    foreach (var floor in floorList)
                    {
                        text += $" -{floor.Key.LabelCap} {floor.Count()}\n";
                    }
                    text += "\n";
                }

                text += "PB.Quotation.TotalCost".Translate() + ":\n";
                foreach (var cost in this.TotalCost)
                {
                    text += $" -{cost.thingDef.LabelCap} {cost.count}\n";
                }
                text = text.TrimEnd();
                return text;
            }
        }

        public IEnumerable<ThingDefCountClass> TotalCost
        {
            get
            {
                var buildingLayout = new List<BuildingLayout>().ConcatIfNotNull(this.BuildingLayoutList);
                var floorLayout = new List<FloorLayout>().ConcatIfNotNull(this.FloorayoutList);
                var costList = buildingLayout.Select(b => b.def.CostListAdjusted(b.stuff)).ConcatIfNotNull(floorLayout.Select(f => f.def.CostList)).SelectMany(c => c);
                foreach (var cost in costList.GroupBy(c => c.thingDef))
                {
                    yield return new ThingDefCountClass(cost.Key, cost.Sum(c => c.count));
                }
            }
        }
        public override string TransformLabel(string label)
        {
            return label + ": " + this.BlueprintName;
        }
        public override string CompTipStringExtra()
        {
            return "\n\n" + this.Quotation;
        }

        public override string GetDescriptionPart()
        {
            return this.Quotation;
        }

        public override void Notify_RecipeProduced(Pawn pawn)
        {
            var bill = pawn.CurJob.bill;
            var blueprintBill = bill as Bill_Blueprint;
            if (blueprintBill == null) Log.Error("[Portable Blueprint] Blueprint is produced in an invalid way.");

            this.buildingLayoutList = blueprintBill.BuildingLayoutList;
            this.floorLayoutList = blueprintBill.FloorLayoutList;
            this.blueprintName = blueprintBill.BlueprintName;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Designator_PlaceBlueprint(this);
            yield return new Command_RenameBlueprint(this);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref this.buildingLayoutList, "buildingLayout", LookMode.Deep);
            Scribe_Collections.Look(ref this.floorLayoutList, "floorLayout", LookMode.Deep);
            Scribe_Values.Look(ref this.blueprintName, "blueprintName");
        }

        private string blueprintName;

        private List<BuildingLayout> buildingLayoutList;

        private List<FloorLayout> floorLayoutList;
    }
}
