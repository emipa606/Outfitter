using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace KillfaceTools.FMO
{
	// Token: 0x02000005 RID: 5
	public static class Tools
	{
		// Token: 0x06000007 RID: 7 RVA: 0x00002144 File Offset: 0x00000344
		public static void CloseLabelMenu(bool sound)
		{
			if (Tools.LabelMenu != null)
			{
				Find.WindowStack.TryRemove(Tools.LabelMenu, sound);
				Tools.LabelMenu = null;
			}
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002164 File Offset: 0x00000364
		public static FloatMenuOption MakeMenuItemForLabel([NotNull] string label, [NotNull] List<FloatMenuOption> fmo)
		{
			List<FloatMenuOption> options = fmo.ToList<FloatMenuOption>();
			bool isSingle = options.Count == 1 && !label.Contains(" ►");
			FloatMenuOptionNoClose floatMenuOptionNoClose = new FloatMenuOptionNoClose(label, delegate()
			{
				if (isSingle && !options[0].Disabled)
				{
					Action action = options[0].action;
					if (action != null)
					{
						Tools.CloseLabelMenu(true);
						action();
						return;
					}
				}
				else
				{
					int i = 0;
					List<FloatMenuOption> actions = new List<FloatMenuOption>();
					CollectionExtensions.Do<FloatMenuOption>(fmo, delegate(FloatMenuOption menuOption)
					{
						string label2 = menuOption.Label;
						Action action2 = delegate()
						{
							//FloatMenuOption menuOption = menuOption;
							Tools.actionMenu.Close(true);
							Tools.CloseLabelMenu(true);
							menuOption.action();
						};
						//int i = i;
						i++;
						FloatMenuOption item = new FloatMenuOption(label2, action2, (MenuOptionPriority)i, menuOption.mouseoverGuiAction, menuOption.revalidateClickTarget, menuOption.extraPartWidth, menuOption.extraPartOnGUI, null);
						actions.Add(item);
					});
					Tools.actionMenu = new FloatMenuNested(actions, null);
					Find.WindowStack.Add(Tools.actionMenu);
				}
			}, isSingle ? options[0].extraPartWidth : 0f, isSingle ? options[0].extraPartOnGUI : null);
			floatMenuOptionNoClose.Disabled = options.All((FloatMenuOption o) => o.Disabled);
			return floatMenuOptionNoClose;
		}

		// Token: 0x04000001 RID: 1
		public const string NestedString = " ►";

		// Token: 0x04000002 RID: 2
		public static FloatMenuLabels LabelMenu;

		// Token: 0x04000003 RID: 3
		private static FloatMenuNested actionMenu;
	}
}
