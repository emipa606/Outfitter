using System;
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
		public FloatMenuNested([NotNull] List<FloatMenuOption> options, [CanBeNull] string label) : base(options, label, false)
		{
			this.givesColonistOrders = true;
			this.vanishIfMouseDistant = true;
			this.closeOnClickedOutside = true;
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002070 File Offset: 0x00000270
		public override void DoWindowContents(Rect rect)
		{
			CollectionExtensions.Do<FloatMenuOption>(this.options, delegate(FloatMenuOption o)
			{
				o.SetSizeMode(FloatMenuSizeMode.Normal);
			});
			this.windowRect = new Rect(this.windowRect.x, this.windowRect.y, this.InitialSize.x, this.InitialSize.y);
			base.DoWindowContents(this.windowRect);
		}

		// Token: 0x06000003 RID: 3 RVA: 0x000020EB File Offset: 0x000002EB
		public override void PostClose()
		{
			base.PostClose();
			Tools.CloseLabelMenu(false);
		}
	}
}
