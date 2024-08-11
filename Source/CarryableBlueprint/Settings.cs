using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CarryableBlueprint
{
    public class Settings : ModSettings
    {
        public Vector2 windowPosition = new Vector2(0f, 0f);

        public Dictionary<string, bool> makeBlueprintSettings = new Dictionary<string, bool>
        {
            { "CB.IncludeFloors", false },
            { "CB.DeconstructOrUninstall", false }
        };

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.windowPosition, "windowPosition", new Vector2(0f, 0f));
            var settings = this.makeBlueprintSettings;
            Scribe_Collections.Look(ref settings, "makeBlueprintSettings", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars && settings != null)
            {
                this.makeBlueprintSettings = settings;
            }
        }
    }
}
