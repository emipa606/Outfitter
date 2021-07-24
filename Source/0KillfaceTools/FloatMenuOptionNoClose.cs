using System;
using UnityEngine;
using Verse;

namespace KillfaceTools.FMO
{
    // Token: 0x02000004 RID: 4
    public class FloatMenuOptionNoClose : FloatMenuOption
    {
        // Token: 0x06000005 RID: 5 RVA: 0x0000211C File Offset: 0x0000031C
        public FloatMenuOptionNoClose(string label, Action action, float extraPartWidth,
            Func<Rect, bool> extraPartOnGUI = null) : base(label, action, MenuOptionPriority.Default, null, null,
            extraPartWidth, extraPartOnGUI)
        {
        }

        // Token: 0x06000006 RID: 6 RVA: 0x00002138 File Offset: 0x00000338
        public override bool DoGUI(Rect rect, bool colonistOrdering, FloatMenu floatMenu)
        {
            base.DoGUI(rect, colonistOrdering, floatMenu);
            return false;
        }
    }
}