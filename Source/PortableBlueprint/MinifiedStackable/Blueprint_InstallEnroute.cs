using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PortableBlueprint;

public class Blueprint_InstallEnroute : Blueprint_Install, IHaulEnroute, IThingHolder
{
    public ThingOwner resourceContainer;

    public static readonly FastInvokeHandler SetThingToInstallFromMinified = MethodInvoker.GetHandler(AccessTools.Method(typeof(Blueprint_Install), "SetThingToInstallFromMinified"));

    public Blueprint_InstallEnroute()
    {
        resourceContainer = new ThingOwner<Thing>(this, true);
    }

    Map IHaulEnroute.Map => Map;

    int IHaulEnroute.SpaceRemainingFor(ThingDef stuff)
    {
        
        return 1;
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return resourceContainer;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public override List<ThingDefCountClass> TotalMaterialCost()
    {
        return [new ThingDefCountClass(MiniToInstallOrBuildingToReinstall.def, 1)];
    }

    //本来DepositHauledThingInContainerで行われる処理を代わりにやっている
    public override bool TryReplaceWithSolidThing(Pawn workerPawn, out Thing createdThing, out bool jobEnded)
    {
        Thing carriedThing = workerPawn.carryTracker.CarriedThing;
        int num = carriedThing.stackCount;
        ThingDef def = carriedThing.def;
        num = Mathf.Min(this.GetSpaceRemainingWithEnroute(def, workerPawn), num);
        Map.enrouteManager.ReleaseFor(this, workerPawn);

        if (carriedThing is MinifiedThingStackable miniToInstall && miniToInstall != MiniToInstallOrBuildingToReinstall)
        {
            SetThingToInstallFromMinified(this, miniToInstall);
        }

        if (base.TryReplaceWithSolidThing(workerPawn, out createdThing, out jobEnded))
        {
            if (MiniToInstallOrBuildingToReinstall is MinifiedThingStackable miniToInstall2)
            {
                miniToInstall2.blueprintsForDrawLine.Remove(this);
            }
            carriedThing.stackCount -= num;
            if (carriedThing.stackCount <= 0)
            {
                workerPawn.carryTracker.innerContainer.ClearAndDestroyContents();
            }
            return true;
        }
        return false;
    }

    protected override Thing MakeSolidThing(out bool shouldSelect)
    {
        Thing thingToInstall = ThingToInstall;
        shouldSelect = false;
        if (MiniToInstallOrBuildingToReinstall is MinifiedThingStackable miniToInstall)
        {
            foreach (Designation designation in Map.designationManager.AllDesignationsOn(miniToInstall))
            {
                Designation designation2 = new(thingToInstall, designation.def, designation.colorDef);
                Map.designationManager.AddDesignation(designation2);
            }
            shouldSelect = Find.Selector.IsSelected(miniToInstall);
            miniToInstall.blueprintsForDrawLine.Remove(this);
            var innerThing = miniToInstall.InnerThing;
            var newThing = ThingMaker.MakeThing(innerThing.def, innerThing.Stuff);
            newThing.HitPoints = innerThing.HitPoints;
            newThing.SetColor(innerThing.DrawColor, false);
            return newThing;
        }
        return thingToInstall;
    }

    public override void DrawExtraSelectionOverlays()
    {
        if (ThingToInstall != null)
        {
            base.DrawExtraSelectionOverlays();
        }
    }
}