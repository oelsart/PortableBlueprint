using RimWorld;
using UnityEngine;
using Verse;

namespace PortableBlueprint;

public class Command_RenameBlueprint : Command
{
    public Command_RenameBlueprint(CompBlueprint comp)
    {
        this.comp = comp;
        defaultLabel = "PB.RenameBlueprint".Translate();
        icon = TexButton.Rename;
    }

    public override void ProcessInput(Event ev)
    {
        Find.WindowStack.Add(new Dialog_GiveBlueprintName(comp, s =>
        {
            comp.BlueprintName = s;
        }));
    }

    readonly CompBlueprint comp;
}
