using Verse;

namespace CarryableBlueprint
{
    public class CarryableBlueprint : Mod
    {
        public CarryableBlueprint(ModContentPack content) : base(content)
        {
            CarryableBlueprint.settings = GetSettings<Settings>();
            CarryableBlueprint.content = content;
            CarryableBlueprint.UI = new UI_MakeBlueprint();
        }

        public static ModContentPack content;

        public static Settings settings;

        public static UI_MakeBlueprint UI;
    }
}
