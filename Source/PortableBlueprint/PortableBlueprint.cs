using Verse;

namespace PortableBlueprint
{
    public class PortableBlueprint : Mod
    {
        public PortableBlueprint(ModContentPack content) : base(content)
        {
            PortableBlueprint.settings = GetSettings<Settings>();
            PortableBlueprint.content = content;
            PortableBlueprint.UI = new UI_MakeBlueprint();
        }

        public static ModContentPack content;

        public static Settings settings;

        public static UI_MakeBlueprint UI;
    }
}
