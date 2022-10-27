using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace KillfaceTools.FMO;

public static class Tools
{
    public const string NestedString = " ►";

    public static FloatMenuLabels LabelMenu;

    private static FloatMenuNested actionMenu;

    public static void CloseLabelMenu(bool sound)
    {
        if (LabelMenu == null)
        {
            return;
        }

        Find.WindowStack.TryRemove(LabelMenu, sound);
        LabelMenu = null;
    }

    public static FloatMenuOption MakeMenuItemForLabel([NotNull] string label, [NotNull] List<FloatMenuOption> fmo)
    {
        var options = fmo.ToList();
        var isSingle = options.Count == 1 && !label.Contains(" ►");
        var floatMenuOptionNoClose = new FloatMenuOptionNoClose(label, delegate
        {
            if (isSingle && !options[0].Disabled)
            {
                var action = options[0].action;
                if (action == null)
                {
                    return;
                }

                CloseLabelMenu(true);
                action();
            }
            else
            {
                var i = 0;
                var actions = new List<FloatMenuOption>();
                fmo.Do(delegate(FloatMenuOption menuOption)
                {
                    var label2 = menuOption.Label;

                    void Action2()
                    {
                        //FloatMenuOption menuOption = menuOption;
                        actionMenu.Close();
                        CloseLabelMenu(true);
                        menuOption.action();
                    }

                    //int i = i;
                    i++;
                    var item = new FloatMenuOption(label2, Action2, (MenuOptionPriority)i,
                        menuOption.mouseoverGuiAction, menuOption.revalidateClickTarget,
                        menuOption.extraPartWidth,
                        menuOption.extraPartOnGUI);
                    actions.Add(item);
                });
                actionMenu = new FloatMenuNested(actions, null);
                Find.WindowStack.Add(actionMenu);
            }
        }, isSingle ? options[0].extraPartWidth : 0f, isSingle ? options[0].extraPartOnGUI : null)
        {
            Disabled = options.All(o => o.Disabled)
        };
        return floatMenuOptionNoClose;
    }
}