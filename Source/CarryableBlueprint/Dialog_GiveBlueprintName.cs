using RimWorld;
using RimWorld.BaseGen;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace CarryableBlueprint
{
    public class Dialog_GiveBlueprintName : Window
    {
        protected virtual int FirstCharLimit => 64;

        public override Vector2 InitialSize => new Vector2(500f, 150f);

        public Dialog_GiveBlueprintName(Building_DrawingTable drawingTable, IntVec3 pos, Action<string> Named) : base()
        {
            this.forcePause = true;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
            this.absorbInputAroundWindow = true;

            this.Named = Named;
            this.nameMessageKey = "CB.NameBlueprint".Translate();
            this.invalidNameMessageKey = "CB.BlueprintMustBeNamed".Translate();
            var roomLabel = pos.GetRoom(drawingTable.Map)?.GetRoomRoleLabel();
            var blueprintNames = Find.Maps.SelectMany(m => m.listerThings.ThingsOfDef(CB_DefOf.CB_Blueprint)).Select(b => b.TryGetComp<CompBlueprint>()?.BlueprintName ?? "")
                .Concat(drawingTable.billStack.Bills.Select(b => (b as Bill_Blueprint)?.BlueprintName ?? ""));
            int maxCount = 0;
            if (roomLabel != null)
            {
                blueprintNames.Where(n => n.StartsWith(roomLabel));
                if (!blueprintNames.EnumerableNullOrEmpty())
                {
                    maxCount = blueprintNames.Max(n =>
                    {
                        int index = n.IndexOf(roomLabel);
                        int.TryParse(n.Substring(index + roomLabel.Length), out int count);
                        return count;
                    });
                }
            }
            else
            {
                blueprintNames.Where(n => int.TryParse(n.Substring(0, 1), out _));
                if (!blueprintNames.EnumerableNullOrEmpty())
                {
                    maxCount = blueprintNames.Max(n =>
                    {
                        int.TryParse(n.Substring(0, 3), out int count);
                        return count;
                    });
                }
            }
            this.curName = roomLabel + (maxCount + 1);
        }

        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Small;
            bool enter = false;
            bool escape = false;
            if (Event.current.type == EventType.KeyDown) {
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    enter = true;
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    escape = true;
                    Event.current.Use();
                }
            }

            float num = 0f;
            using (new TextBlock(TextAnchor.UpperCenter))
            {
                Widgets.Label(rect, this.nameMessageKey);
                num += Text.CalcHeight(this.nameMessageKey, rect.width) + 10f;
            }
            Widgets.TextField(new Rect(0f, num, rect.width, 35f), this.curName, this.FirstCharLimit);
            Rect rect2 = new Rect(0f, rect.height - 35f, rect.width / 2f - 5f, 35f);
            if (Widgets.ButtonText(rect2, "OK".Translate()) || enter)
            {
                string text2 = this.curName;
                string text3 = (text2 != null) ? text2.Trim() : null;
                if (this.IsValidName(text3))
                {
                    this.Named(text3);
                    Find.WindowStack.TryRemove(this, true);
                }
                else
                {
                    Messages.Message(this.invalidNameMessageKey.Translate(), MessageTypeDefOf.RejectInput, false);
                }
                Event.current.Use();
            }
            Rect rect3 = new Rect(rect.width / 2f + 5f, rect.height - 35f, rect.width / 2f - 5f, 35f);

            if (Widgets.ButtonText(rect3, "Cancel".Translate()) || escape){
                Find.WindowStack.TryRemove(this, true);
            }
        }

        protected virtual bool IsValidName(string s) => s.Length > 0;

        private readonly string curName;

        private readonly string nameMessageKey;

        private readonly string invalidNameMessageKey;

        private readonly Action<string> Named;
    }
}
