using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PortableBlueprint;

public class CompBlueprint : ThingComp
{
    public string BlueprintName
    {
        get => blueprintName;
        set => blueprintName = value;
    }

    public List<BuildingLayout> BuildingLayoutList => buildingLayoutList;

    public List<FloorLayout> FloorLayoutList => floorLayoutList;

    private string Quotation
    {
        get
        {
            var buildingList = buildingLayoutList?.GroupBy(b => b.def).ToList();
            var floorList = floorLayoutList?.GroupBy(b => b.topDef).ToList();
            var foundationList = floorLayoutList?.GroupBy(b => b.foundationDef).ToList();
            var text = "";
            if (!buildingList.NullOrEmpty())
            {
                text += "PB.Quotation.Buildings".Translate() + ":\n";
                foreach (var building in buildingList!)
                {
                    if (building.Key is null) continue;
                    text += $" -{building.Key.LabelCap} {building.Count()}\n";
                }
                text += "\n";
            }

            if (!floorList.NullOrEmpty())
            {
                text += "PB.Quotation.Floors".Translate() + ":\n";
                foreach (var floor in floorList!)
                {
                    if (floor.Key is null) continue;
                    text += $" -{floor.Key.LabelCap} {floor.Count()}\n";
                }
            }
            if (!foundationList.NullOrEmpty())
            {
                foreach (var foundation in foundationList!)
                {
                    if (foundation.Key is null) continue;
                    text += $" -{foundation.Key.LabelCap} {foundation.Count()}\n";
                }
                text += "\n";
            }

            text += "PB.Quotation.TotalCost".Translate() + ":\n";
            foreach (var cost in TotalCost)
            {
                text += $" -{cost.thingDef.LabelCap} {cost.count}\n";
            }
            text = text.TrimEnd();
            return text;
        }
    }

    public List<ThingDefCountClass> TotalCost
    {
        get
        {
            if (field is null)
            {
                field = [];
                foreach (var cost in CostEnumerable().SelectMany(c => c)
                             .Where(c => c?.thingDef != null).GroupBy(c => c.thingDef))
                {
                    field.Add(new ThingDefCountClass(cost.Key, cost.Sum(c => c.count)));
                }
            }
            return field;

            IEnumerable<List<ThingDefCountClass>> CostEnumerable()
            {
                if (!buildingLayoutList.NullOrEmpty())
                {
                    foreach (var building in buildingLayoutList.Where(b => b.def != null))
                    {
                        yield return building.def.CostListAdjusted(building.stuff);
                    }
                }
                if (!floorLayoutList.NullOrEmpty())
                {
                    foreach (var floor in floorLayoutList.Where(f => f.topDef != null))
                    {
                        yield return floor.topDef.CostList;
                    }
                    foreach (var floor in floorLayoutList.Where(f => f.foundationDef != null))
                    {
                        yield return floor.foundationDef.CostList;
                    }
                }
            }
        }
    }
    public override string TransformLabel(string label)
    {
        return label + ": " + BlueprintName;
    }
    public override string CompTipStringExtra()
    {
        return "\n\n" + Quotation;
    }

    public override string GetDescriptionPart()
    {
        return Quotation;
    }

    public override void Notify_RecipeProduced(Pawn pawn)
    {
        var bill = pawn.CurJob.bill;
        if (bill is not Bill_Blueprint blueprintBill)
        {
            Log.Error("[Portable Blueprint] Blueprint is produced in an invalid way.");
            return;
        }

        buildingLayoutList = blueprintBill.BuildingLayoutList;
        floorLayoutList = blueprintBill.FloorLayoutList;
        blueprintName = blueprintBill.BlueprintName;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        yield return new Designator_PlaceBlueprint(this);
        yield return new Command_RenameBlueprint(this);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref buildingLayoutList, "buildingLayout", LookMode.Deep);
        Scribe_Collections.Look(ref floorLayoutList, "floorLayout", LookMode.Deep);
        Scribe_Values.Look(ref blueprintName, "blueprintName");
    }

    private string blueprintName;

    private List<BuildingLayout> buildingLayoutList;

    private List<FloorLayout> floorLayoutList;
}
