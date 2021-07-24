using System.Collections.Generic;
using Verse;

namespace KillfaceTools.FMO
{
    // Token: 0x02000003 RID: 3
    public class FloatMenuLabels : FloatMenu
    {
        // Token: 0x06000004 RID: 4 RVA: 0x000020F9 File Offset: 0x000002F9
        public FloatMenuLabels(List<FloatMenuOption> options) : base(options, null)
        {
            givesColonistOrders = false;
            vanishIfMouseDistant = true;
            closeOnClickedOutside = false;
        }
    }
}