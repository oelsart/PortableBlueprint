using System.Reflection.Emit;
using HarmonyLib;
using PortableBlueprint.Tent;
using Verse;

namespace PortableBlueprint.PB_HarmonyPatches;

[HarmonyPatch(typeof(GraphicUtility), "WrapLinked")]
public static class Patch_GraphicUtility_WrapLinked
{
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
		ILGenerator generator)
	{
		List<CodeInstruction> codes = [.. instructions];
		var pos = codes.FindIndex(c => c.opcode == OpCodes.Switch);
		var switchLabels = (Label[])codes[pos].operand;
		linkType = (LinkDrawerType)Convert.ToByte(switchLabels.Length);
		var label = generator.DefineLabel();
		codes[pos].operand = switchLabels.AddToArray(label);
		pos = codes.FindIndex(c =>
			c.opcode == OpCodes.Newobj && c.operand.Equals(AccessTools.Constructor(typeof(ArgumentException))));
		codes.InsertRange(pos,
		[
			CodeInstruction.LoadArgument(0).WithLabels(label),
			new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Graphic_LinkedTent), [typeof(Graphic)])),
			new CodeInstruction(OpCodes.Ret)
		]);

		return codes;
	}

	public static LinkDrawerType linkType;
}