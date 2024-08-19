using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PortableBlueprint
{
    public class Command_RenameBlueprint : Command
    {
        public Command_RenameBlueprint(CompBlueprint comp)
        {
            this.comp = comp;
            this.defaultLabel = "PB.RenameBlueprint".Translate();
            this.icon = TexButton.Rename;
        }

        public override void ProcessInput(Event ev)
        {
            Find.WindowStack.Add(new Dialog_GiveBlueprintName(comp, s =>
            {
                comp.BlueprintName = s;
            }));
        }

        CompBlueprint comp;
    }
}
