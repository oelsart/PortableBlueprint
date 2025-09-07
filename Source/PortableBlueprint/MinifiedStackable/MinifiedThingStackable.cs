using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PortableBlueprint;

public class MinifiedThingStackable : MinifiedThing
{
    public List<Blueprint_Install> blueprintsForDrawLine = [];

    public static readonly AccessTools.FieldRef<MinifiedThing, ThingOwner> innerContainer = AccessTools.FieldRefAccess<MinifiedThing, ThingOwner>("innerContainer");

    internal ThingOwner InnerContainer => innerContainer(this);

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
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

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        for (var i = blueprintsForDrawLine.Count - 1; i >= 0; i--)
        {
            if (blueprintsForDrawLine[i] is null || !blueprintsForDrawLine[i].Spawned)
            {
                blueprintsForDrawLine.RemoveAt(i);
            }
        }
    }

    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();
        foreach (var blueprint in blueprintsForDrawLine)
        {
            if (blueprint?.Spawned ?? false)
            {
                GenDraw.DrawLineBetween(DrawPosHeld ?? DrawPos, blueprint.TrueCenter());
            }
        }
    }

    public override bool CanStackWith(Thing other)
    {
        if (other is not MinifiedThingStackable minifiedStackable) return false;
        Thing inner = InnerThing;
        Thing inner2 = minifiedStackable.InnerThing;
        return inner != null && inner2 != null && !Destroyed && !minifiedStackable.Destroyed && inner.def == inner2.def && inner.Stuff == inner2.Stuff &&
            HitPoints == minifiedStackable.HitPoints && inner.HitPoints == inner2.HitPoints && inner.DrawColor == inner2.DrawColor;
    }

    public override bool TryAbsorbStack(Thing other, bool respectStackLimit)
    {
        if (!CanStackWith(other))
        {
            return false;
        }
        if (other is MinifiedThingStackable minifiedStackable)
        {
            int num = ThingUtility.TryAbsorbStackNumToTake(this, other, respectStackLimit);
            for (var i = 0; i < num; i++)
            {
                var blueprint = minifiedStackable.blueprintsForDrawLine.ElementAtOrDefault(minifiedStackable.blueprintsForDrawLine.Count - i - 1);
                if (blueprint?.MiniToInstallOrBuildingToReinstall == minifiedStackable)
                {
                    Blueprint_InstallEnroute.SetThingToInstallFromMinified(blueprint, this);
                    blueprintsForDrawLine.Add(blueprint);
                }
            }
            minifiedStackable.blueprintsForDrawLine.Clear();
        }
        return base.TryAbsorbStack(other, respectStackLimit);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref blueprintsForDrawLine, "blueprintsForDrawLine", LookMode.Reference);
    }
}