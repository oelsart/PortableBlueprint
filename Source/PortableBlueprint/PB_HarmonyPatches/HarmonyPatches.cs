using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace PortableBlueprint.PB_HarmonyPatches;

[StaticConstructorOnStartup]
class HarmonyPatches
{
    static HarmonyPatches()
    {
        var harmony = new Harmony("OELS.PortableBlueprint");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        Patch_PlayDataLoader_HotReloadDefs.Postfix();
    }
}

[HarmonyPatch(typeof(PlayDataLoader), "HotReloadDefs")]
public static class Patch_PlayDataLoader_HotReloadDefs
{
    public static void Postfix()
    {
        LongEventHandler.QueueLongEvent(() =>
        {
            PB_DefOf.PB_TentWall.graphicData.CopyFrom(PB_DefOf.PB_TentWall.graphicData);
            PB_DefOf.PB_TentWall.graphicData.linkType = Patch_GraphicUtility_WrapLinked.linkType;
            PB_DefOf.PB_TentWall.PostLoad();
            Find.Maps?.ForEach(m =>
            {
                m.listerThings.ThingsOfDef(PB_DefOf.PB_TentWall)?.ForEach(t =>
                {
                    t.Notify_DefsHotReloaded();
                    t.DirtyMapMesh(m);
                });
            });
        }, "", false, null, false);
    }
}