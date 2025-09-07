using RimWorld;
using System.Collections.Generic;
using Verse;

namespace PortableBlueprint;

public class Bill_Blueprint : Bill_ProductionWithUft
{
    public override string Label => base.Label + ": " + BlueprintName;

    public List<BuildingLayout> BuildingLayoutList => buildingLayoutInt;

    public List<FloorLayout> FloorLayoutList => floorLayoutInt;

    public string BlueprintName
    {
        get => blueprintName;
        set => blueprintName = value;
    }

    public Bill_Blueprint()
    {
    }

    public Bill_Blueprint(RecipeDef recipe, List<BuildingLayout> buildingLayouts, List<FloorLayout> floorLayouts) : base(recipe)
    {
        buildingLayoutInt = buildingLayouts;
        floorLayoutInt = floorLayouts;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref buildingLayoutInt, "buildingLayout", LookMode.Deep);
        Scribe_Collections.Look(ref floorLayoutInt, "floorLayout", LookMode.Deep);
        Scribe_Values.Look(ref blueprintName, "blueprintName");
    }

    private List<BuildingLayout> buildingLayoutInt;

    private List<FloorLayout> floorLayoutInt;

    private string blueprintName;
}
