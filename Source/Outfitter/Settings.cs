using UnityEngine;
using Verse;

namespace Outfitter
{
    public class Settings : ModSettings
    {
        private bool _useCustomTailorWorkbench;

        private bool _useEyes;
        public bool UseEyes => _useEyes;

        public bool UseCustomTailorWorkbench => _useCustomTailorWorkbench;

        public void DoWindowContents(Rect inRect)
        {
            var list = new Listing_Standard {ColumnWidth = inRect.width / 2};
            list.Begin(inRect);

            list.Gap();

            list.CheckboxLabeled(
                "Settings.UseEyes".Translate(),
                ref _useEyes,
                "Settings.UseEyesTooltip".Translate());

            list.CheckboxLabeled(
                "Settings.UseTailorWorkbenchUI".Translate(),
                ref _useCustomTailorWorkbench,
                "Settings.UseTailorWorkbenchUITooltip".Translate());

            list.End();

            if (GUI.changed)
            {
                Mod.WriteSettings();
            }

            // FlexibleSpace();
            // BeginVertical();
            // if (Button("Settings.Apply".Translate()))
            // {
            // foreach (Pawn pawn in PawnsFinder.AllMapsAndWorld_Alive)
            // {
            // if (pawn.RaceProps.Humanlike)
            // {
            // CompFace faceComp = pawn.TryGetComp<CompFace>();
            // if (faceComp != null)
            // {
            // this.WriteSettings();
            // faceComp.sessionOptimized = false;
            // pawn.Drawer.renderer.graphics.ResolveAllGraphics();
            // }
            // }
            // }
            // }
            // EndVertical();
            // FlexibleSpace();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _useEyes, "useEyes", false, true);
            Scribe_Values.Look(ref _useCustomTailorWorkbench, "useCustomTailorWorkbench", false, true);
        }
    }
}