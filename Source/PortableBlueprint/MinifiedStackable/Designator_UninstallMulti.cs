using RimWorld;
using Verse;

namespace PortableBlueprint;

public class Designator_UninstallMulti : Designator_Uninstall
{
	public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
	{
		foreach (var cell in cells)
		{
			foreach (var thing in cell.GetThingList(Map).Where(t => CanDesignateThing(t).Accepted).ToArray())
			{
				base.DesignateThing(thing);
			}
		}
	}
}