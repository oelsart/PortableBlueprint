using Verse;

namespace PortableBlueprint;

public class PortableBlueprint : Mod
{
	public static Settings settings;

	public static UI_MakeBlueprint UI;
	
	public PortableBlueprint(ModContentPack content) : base(content)
	{
		settings = GetSettings<Settings>();
		UI = new UI_MakeBlueprint();
	}
}