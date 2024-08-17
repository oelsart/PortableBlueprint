using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PortableBlueprint
{
    public class MinifiedThing_Stackable : MinifiedThing
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach(var gizmo in base.GetGizmos())
            {
                if (gizmo is Designator_Install)
                {
                    yield return new Designator_InstallStackable();
                }
                else
                {
                    yield return gizmo;
                }
            }
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            foreach (var blueprint in this.blueprintsForDrawLine)
            {
                GenDraw.DrawLineBetween(this.TrueCenter(), blueprint.TrueCenter());
            }
        }

        public override bool CanStackWith(Thing other)
        {
            Thing inner = this.InnerThing;
            MinifiedThing_Stackable other2 = other as MinifiedThing_Stackable;
            Thing inner2 = other2?.InnerThing;
            return other2 != null && !this.Destroyed && !other2.Destroyed && inner.def == inner2.def && inner.Stuff == inner2.Stuff &&
                this.HitPoints == other2.HitPoints && inner.HitPoints == inner2.HitPoints && inner.DrawColor == inner2.DrawColor;
        }

        public override bool TryAbsorbStack(Thing other, bool respectStackLimit)
        {
            var flag = base.TryAbsorbStack(other, respectStackLimit);
            if (flag)
            {
                var minifiedStackable = other as MinifiedThing_Stackable;
                foreach (var blueprint in minifiedStackable.blueprintsForDrawLine)
                {
                    MinifiedThing_Stackable.SetThingToInstallFromMinified(blueprint, this);
                }
                this.blueprintsForDrawLine.AddRange(minifiedStackable.blueprintsForDrawLine);
            }
            return flag;
        }

        public override Thing SplitOff(int count)
        {
            MinifiedThing_Stackable minifiedThing = (MinifiedThing_Stackable)base.SplitOff(count);
            if (minifiedThing != this)
            {
                foreach (var blueprint in this.blueprintsForDrawLine.ToArray())
                {
                    if (blueprint.MiniToInstallOrBuildingToReinstall != this)
                    {
                        minifiedThing.blueprintsForDrawLine.Add(blueprint);
                        this.blueprintsForDrawLine.Remove(blueprint);
                    }
                }
            }
            return minifiedThing;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref blueprintsForDrawLine, "blueprintsForDrawLine", LookMode.Reference);
        }

        public HashSet<Blueprint_Install> blueprintsForDrawLine = new HashSet<Blueprint_Install>();

        public static readonly FastInvokeHandler SetThingToInstallFromMinified = MethodInvoker.GetHandler(AccessTools.Method(typeof(Blueprint_Install), "SetThingToInstallFromMinified"));
    }
}