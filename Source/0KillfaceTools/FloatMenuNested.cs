using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace KillfaceTools.FMO
{
    // Token: 0x02000002 RID: 2
    public class FloatMenuNested : FloatMenu
    {
        // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
        public FloatMenuNested([NotNull] List<FloatMenuOption> options, [CanBeNull] string label) : base(options, label)
        {
            givesColonistOrders = true;
            vanishIfMouseDistant = true;
            closeOnClickedOutside = true;
        }

        // Token: 0x06000002 RID: 2 RVA: 0x00002070 File Offset: 0x00000270
        public override void DoWindowContents(Rect rect)
        {
            options.Do(delegate(FloatMenuOption o) { o.SetSizeMode(FloatMenuSizeMode.Normal); });
            windowRect = new Rect(windowRect.x, windowRect.y, InitialSize.x, InitialSize.y);
            base.DoWindowContents(windowRect);
        }

        // Token: 0x06000003 RID: 3 RVA: 0x000020EB File Offset: 0x000002EB
        public override void PostClose()
        {
            base.PostClose();
            Tools.CloseLabelMenu(false);
        }
    }
}