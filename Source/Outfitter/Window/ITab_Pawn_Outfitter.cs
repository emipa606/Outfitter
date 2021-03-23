using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Outfitter.Enums;
using Outfitter.Textures;
using Outfitter.Window;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Outfitter
{
    public class Tab_Pawn_Outfitter : ITab
    {
        #region Public Constructors

        public Tab_Pawn_Outfitter()
        {
            size = new Vector2(770f, 550f);
            labelKey = "OutfitterTab";
        }

        #endregion Public Constructors

        #region Public Properties

        public override bool IsVisible
        {
            get
            {
                var selectedPawn = SelPawn;

                // thing selected is a pawn
                if (selectedPawn == null)
                {
                    Find.WindowStack.TryRemove(typeof(Window_Pawn_ApparelDetail), false);

                    // Find.WindowStack.TryRemove(typeof(Window_Pawn_ApparelList), false);
                    return false;
                }

                // of this colony
                if (selectedPawn.Faction != Faction.OfPlayer)
                {
                    return false;
                }

                // and has apparel (that should block everything without apparel, animals, bots, that sort of thing)
                if (selectedPawn.apparel == null)
                {
                    return false;
                }

                return true;
            }
        }

        #endregion Public Properties

        #region Protected Methods

        protected override void FillTab()
        {
            var pawnSave = SelPawnForGear.GetSaveablePawn();

            // Outfit + Status button
            var rectStatus = new Rect(20f, 15f, 380f, ButtonHeight);

            var outfitRect = new Rect(rectStatus.x, rectStatus.y, (392f / 3) - Margin, ButtonHeight);

            var outfitEditRect = new Rect(outfitRect.xMax + Margin, outfitRect.y, outfitRect.width, ButtonHeight);

            var outfitJobRect = new Rect(outfitEditRect.xMax + Margin, outfitRect.y, outfitRect.width, ButtonHeight);

            // select outfit
            if (Widgets.ButtonText(outfitRect, SelPawnForGear.outfits.CurrentOutfit.label))
            {
                var options = new List<FloatMenuOption>();

                foreach (var current in Current.Game.outfitDatabase.AllOutfits)
                {
                    var localOut = current;
                    options.Add(
                        new FloatMenuOption(
                            localOut.label,
                            delegate { SelPawnForGear.outfits.CurrentOutfit = localOut; }));
                }

                var window = new FloatMenu(options, "SelectOutfit".Translate());

                Find.WindowStack.Add(window);
            }

            // edit outfit
            if (Widgets.ButtonText(
                    outfitEditRect,
                    "OutfitterEditOutfit".Translate() + " ..."))
                //"OutfitterEditOutfit".Translate() + " " + SelPawnForGear.outfits.CurrentOutfit.label + " ..."))
            {
                Find.WindowStack.Add(new Dialog_ManageOutfits(SelPawnForGear.outfits.CurrentOutfit));
            }

            // job outfit
            if (Widgets.ButtonText(
                outfitJobRect,
                pawnSave.MainJob == MainJob.Anything
                    ? "MainJob".Translate()
                    : "PreferedGear".Translate() + " " + pawnSave.MainJob.ToString()
                        .Replace("00", " - ")
                        .Replace("_", " ")))
            {
                var options = new List<FloatMenuOption>();
                foreach (MainJob mainJob in Enum.GetValues(typeof(MainJob)))
                {
                    options.Add(
                        new FloatMenuOption(
                            mainJob.ToString().Replace("00", " - ").Replace("_", " "),
                            delegate
                            {
                                pawnSave.MainJob = mainJob;
                                pawnSave.ForceStatUpdate = true;

                                SelPawnForGear.mindState.Notify_OutfitChanged();

                                if (SelPawnForGear.jobs.curJob != null
                                    && SelPawnForGear.jobs.IsCurrentJobPlayerInterruptible())
                                {
                                    SelPawnForGear.jobs.EndCurrentJob(JobCondition
                                        .InterruptForced);
                                }
                            }));
                }

                var window = new FloatMenu(options, "MainJob".Translate());

                Find.WindowStack.Add(window);
            }

            // Status checkboxes
            var rectCheckboxes = new Rect(rectStatus.x, rectStatus.yMax + Margin, rectStatus.width, 72f);
            var check1 = new Rect(rectCheckboxes.x, rectCheckboxes.y, rectCheckboxes.width, 24f);
            var check2 = new Rect(rectCheckboxes.x, check1.yMax, rectCheckboxes.width, 24f);
            var check3 = new Rect(rectCheckboxes.x, check2.yMax, rectCheckboxes.width, 24f);

            var pawnSaveAddWorkStats = pawnSave.AddWorkStats;
            var pawnSaveAddIndividualStats = pawnSave.AddIndividualStats;
            var pawnSaveAddPersonalStats = pawnSave.AddPersonalStats;

            Widgets.CheckboxLabeled(check1, "AddWorkStats".Translate(), ref pawnSaveAddWorkStats);
            Widgets.CheckboxLabeled(check2, "AddIndividualStats".Translate(), ref pawnSaveAddIndividualStats);
            Widgets.CheckboxLabeled(check3, "AddPersonalStats".Translate(), ref pawnSaveAddPersonalStats);

            if (GUI.changed)
            {
                pawnSave.AddWorkStats = pawnSaveAddWorkStats;
                pawnSave.AddIndividualStats = pawnSaveAddIndividualStats;
                pawnSave.AddPersonalStats = pawnSaveAddPersonalStats;
                pawnSave.ForceStatUpdate = true;
            }

            // main canvas
            var canvas = new Rect(20f, rectCheckboxes.yMax, 392f, size.y - rectCheckboxes.yMax - 20f);
            GUI.BeginGroup(canvas);
            var cur = Vector2.zero;

            DrawTemperatureStats(pawnSave, ref cur, canvas);
            cur.y += Margin;
            DrawApparelStats(cur, canvas);

            GUI.EndGroup();

            DrawApparelList();

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        #endregion Protected Methods

        #region Private Fields

        private const float ButtonHeight = 30f;
        private const float Margin = 10f;
        private const float ThingIconSize = 30f;
        private const float ThingLeftX = 40f;
        private const float ThingRowHeight = 64f;

        private static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        private Vector2 _scrollPosition = Vector2.zero;
        private Vector2 _scrollPosition1 = Vector2.zero;

        private float _scrollViewHeight;
        private float _scrollViewHeight1;

        #endregion Private Fields

        #region Private Properties

        private bool CanControl => SelPawn.IsColonistPlayerControlled;

        private Pawn SelPawnForGear
        {
            get
            {
                if (SelPawn != null)
                {
                    return SelPawn;
                }

                if (SelThing is Corpse corpse)
                {
                    return corpse.InnerPawn;
                }

                throw new InvalidOperationException("Gear tab on non-pawn non-corpse " + SelThing);
            }
        }

        #endregion Private Properties

        #region Private Methods

        private void DrawApparelList()
        {
            // main canvas
            var rect = new Rect(432, 20, 318, 530);

            Text.Font = GameFont.Small;

            // Rect rect2 = rect.ContractedBy(10f);
            var calcScore = new Rect(rect.x, rect.y, rect.width, rect.height);
            GUI.BeginGroup(calcScore);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            var outRect = new Rect(0f, 0f, calcScore.width, calcScore.height);
            var viewRect1 = outRect;
            viewRect1.height = _scrollViewHeight1;

            if (viewRect1.height > outRect.height)
            {
                viewRect1.width -= 20f;
            }

            Widgets.BeginScrollView(outRect, ref _scrollPosition1, viewRect1);
            var num = 0f;

            if (SelPawn.apparel != null)
            {
                Widgets.ListSeparator(ref num, viewRect1.width, "Apparel".Translate());
                foreach (var current2 in from ap in SelPawn.apparel.WornApparel
                    orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                    select ap)
                {
                    var bp = string.Empty;
                    var layer = string.Empty;
                    foreach (var apparelLayer in current2.def.apparel.layers)
                    {
                        foreach (var bodyPartGroupDef in current2.def.apparel.bodyPartGroups)
                        {
                            bp += bodyPartGroupDef.LabelCap + " - ";
                        }

                        layer = apparelLayer.ToString();
                    }

                    Widgets.ListSeparator(ref num, viewRect1.width, bp + layer);
                    DrawThingRowModded(ref num, viewRect1.width, current2);
                }
            }

            if (Event.current.type == EventType.Layout)
            {
                _scrollViewHeight1 = num + 30f;
            }

            Widgets.EndScrollView();

            GUI.EndGroup();
        }

        private void DrawApparelStats(Vector2 cur, Rect canvas)
        {
            // header
            var statsHeaderRect = new Rect(cur.x, cur.y, canvas.width, 30f);
            cur.y += 30f;
            Text.Anchor = TextAnchor.LowerLeft;
            Text.Font = GameFont.Small;
            Widgets.Label(statsHeaderRect, "PreferedStats".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            // add button
            var addStatRect = new Rect(statsHeaderRect.xMax - 16f, statsHeaderRect.yMin + Margin, 16f, 16f);
            if (Widgets.ButtonImage(addStatRect, OutfitterTextures.AddButton))
            {
                var options = new List<FloatMenuOption>();
                foreach (var def in SelPawnForGear.NotYetAssignedStatDefs().OrderBy(i => i.label.ToString()))
                {
                    var option = new FloatMenuOption(
                        def.LabelCap,
                        delegate
                        {
                            SelPawnForGear.GetApparelStatCache().StatCache
                                .Insert(
                                    0,
                                    new
                                        StatPriority(def,
                                            0f,
                                            StatAssignment
                                                .Manual));
                        });
                    options.Add(option);
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            TooltipHandler.TipRegion(addStatRect, "StatPriorityAdd".Translate());

            // line
            GUI.color = Color.grey;
            Widgets.DrawLineHorizontal(cur.x, cur.y, canvas.width);
            GUI.color = Color.white;

            // some padding
            cur.y += Margin;

            // main content in scrolling view
            var contentRect = new Rect(cur.x, cur.y, canvas.width, canvas.height - cur.y);
            var viewRect = contentRect;
            viewRect.height = _scrollViewHeight;
            if (viewRect.height > contentRect.height)
            {
                viewRect.width -= 20f;
            }

            Widgets.BeginScrollView(contentRect, ref _scrollPosition, viewRect);

            GUI.BeginGroup(viewRect);
            cur = Vector2.zero;

            // none label
            if (!SelPawnForGear.GetApparelStatCache().StatCache.Any())
            {
                var noneLabel = new Rect(cur.x, cur.y, viewRect.width, 30f);
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(noneLabel, "None".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                cur.y += 30f;
            }
            else
            {
                // legend kind of thingy.
                var legendRect = new Rect(cur.x + ((viewRect.width - 24) / 2), cur.y, (viewRect.width - 24) / 2, 20f);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.LowerLeft;
                Widgets.Label(legendRect, "-" + ApparelStatCache.MaxValue.ToString("N1"));
                Text.Anchor = TextAnchor.LowerRight;
                Widgets.Label(legendRect, ApparelStatCache.MaxValue.ToString("N1"));
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                cur.y += 15f;

                // statPriority weight sliders
                foreach (var stat in SelPawnForGear.GetApparelStatCache().StatCache)
                {
                    DrawStatRow(ref cur, viewRect.width, stat, SelPawnForGear, out var stopUI);
                    if (!stopUI)
                    {
                        continue;
                    }

                    // DrawWApparelStatRow can change the StatCache, invalidating the loop.
                    // So if it does that, stop looping - we'll redraw on the next tick.
                    // + force a statPriority update
                    SelPawnForGear.GetApparelStatCache().RawScoreDict.Clear();
                    break;
                }
            }

            if (Event.current.type == EventType.Layout)
            {
                _scrollViewHeight = cur.y + 10f;
            }

            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        private void DrawStatRow(
            ref Vector2 cur,
            float width,
            [NotNull] StatPriority statPriority,
            Pawn pawn,
            out bool stopUI)
        {
            // sent a signal if the statlist has changed
            stopUI = false;

            // set up rects
            var labelRect = new Rect(cur.x, cur.y, (width - 24) / 2f, 30f);
            var sliderRect = new Rect(labelRect.xMax + 4f, cur.y + 5f, labelRect.width, 25f);
            var buttonRect = new Rect(sliderRect.xMax + 4f, cur.y + 3f, 16f, 16f);

            // draw label
            Text.Font = Text.CalcHeight(statPriority.Stat.LabelCap, labelRect.width) > labelRect.height
                ? GameFont.Tiny
                : GameFont.Small;
            switch (statPriority.Assignment)
            {
                case StatAssignment.Automatic:
                    GUI.color = Color.grey;
                    break;

                case StatAssignment.Individual:
                    GUI.color = Color.cyan;
                    break;

                case StatAssignment.Manual:
                    GUI.color = Color.white;
                    break;

                case StatAssignment.Override:
                    GUI.color = new Color(0.75f, 0.69f, 0.33f);
                    break;

                default:
                    GUI.color = Color.white;
                    break;
            }
            // if (!ApparelStatsHelper.AllStatDefsModifiedByAnyApparel.Contains(statPriority.Stat))
            // {
            //     GUI.color *= new Color(0.8f, 0.8f, 0.8f);
            // }

            Widgets.Label(labelRect, statPriority.Stat.LabelCap);
            Text.Font = GameFont.Small;

            // draw button
            // if manually added, delete the priority
            var buttonTooltip = string.Empty;
            if (statPriority.Assignment == StatAssignment.Manual)
            {
                buttonTooltip = "StatPriorityDelete".Translate(statPriority.Stat.LabelCap);
                if (Widgets.ButtonImage(buttonRect, OutfitterTextures.DeleteButton))
                {
                    statPriority.Delete(pawn);
                    stopUI = true;
                }
            }

            // if overridden auto assignment, reset to auto
            if (statPriority.Assignment == StatAssignment.Override)
            {
                buttonTooltip = "StatPriorityReset".Translate(statPriority.Stat.LabelCap);
                if (Widgets.ButtonImage(buttonRect, OutfitterTextures.ResetButton))
                {
                    statPriority.Reset(pawn);
                    stopUI = true;
                }
            }

            // draw line behind slider
            GUI.color = new Color(.3f, .3f, .3f);
            for (var y = (int) cur.y; y < cur.y + 30; y += 5)
            {
                Widgets.DrawLineVertical((sliderRect.xMin + sliderRect.xMax) / 2f, y, 3f);
            }

            // draw slider
            switch (statPriority.Assignment)
            {
                case StatAssignment.Automatic:
                    GUI.color = Color.grey;
                    break;

                case StatAssignment.Individual:
                    GUI.color = Color.cyan;
                    break;

                case StatAssignment.Manual:
                    GUI.color = Color.white;
                    break;

                case StatAssignment.Override:
                    GUI.color = new Color(0.75f, 0.69f, 0.33f);
                    break;

                default:
                    GUI.color = Color.white;
                    break;
            }

            var weight = GUI.HorizontalSlider(
                sliderRect,
                statPriority.Weight,
                ApparelStatCache.SpecialStats.Contains(statPriority.Stat)
                    ? 0.01f
                    : -ApparelStatCache.MaxValue,
                ApparelStatCache.MaxValue);

            if (Mathf.Abs(weight - statPriority.Weight) > 1e-4)
            {
                statPriority.Weight = weight;
                if (statPriority.Assignment == StatAssignment.Automatic ||
                    statPriority.Assignment == StatAssignment.Individual)
                {
                    statPriority.Assignment = StatAssignment.Override;
                }
            }

            if (GUI.changed)
            {
                pawn.GetApparelStatCache().RawScoreDict.Clear();
            }

            GUI.color = Color.white;

            // tooltips
            TooltipHandler.TipRegion(labelRect, statPriority.Stat.LabelCap + "\n\n" + statPriority.Stat.description);
            if (buttonTooltip != string.Empty)
            {
                TooltipHandler.TipRegion(buttonRect, buttonTooltip);
            }

            TooltipHandler.TipRegion(sliderRect, statPriority.Weight.ToStringByStyle(ToStringStyle.FloatTwo));

            // advance row
            cur.y += 30f;
        }

        private void DrawTemperatureStats([NotNull] SaveablePawn pawnSave, ref Vector2 cur, Rect canvas)
        {
            // header
            var tempHeaderRect = new Rect(cur.x, cur.y, canvas.width, 30f);
            cur.y += 30f;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(tempHeaderRect, "PreferedTemperature".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            // line
            GUI.color = Color.grey;
            Widgets.DrawLineHorizontal(cur.x, cur.y, canvas.width);
            GUI.color = Color.white;

            // some padding
            cur.y += Margin;

            // temperature slider
            // SaveablePawn pawnStatCache = MapComponent_Outfitter.Get.GetSaveablePawn(SelPawn);
            var pawnStatCache = SelPawnForGear.GetApparelStatCache();
            var targetTemps = pawnStatCache.TargetTemperatures;
            var minMaxTemps = ApparelStatsHelper.MinMaxTemperatureRange;
            var sliderRect = new Rect(cur.x, cur.y, canvas.width - 20f, 40f);
            var tempResetRect = new Rect(sliderRect.xMax + 4f, cur.y + Margin, 16f, 16f);
            cur.y += 40f; // includes padding

            // current temperature settings
            GUI.color = pawnSave.TargetTemperaturesOverride ? Color.white : Color.grey;
            Widgets_FloatRange.FloatRange(
                sliderRect,
                123123123,
                ref targetTemps,
                minMaxTemps,
                ToStringStyle.Temperature);
            GUI.color = Color.white;

            if (Math.Abs(targetTemps.min - pawnStatCache.TargetTemperatures.min) > 1e-4
                || Math.Abs(targetTemps.max - pawnStatCache.TargetTemperatures.max) > 1e-4)
            {
                pawnStatCache.TargetTemperatures = targetTemps;
            }

            if (pawnSave.TargetTemperaturesOverride)
            {
                if (Widgets.ButtonImage(tempResetRect, OutfitterTextures.ResetButton))
                {
                    pawnSave.TargetTemperaturesOverride = false;

                    // var saveablePawn = MapComponent_Outfitter.Get.GetSaveablePawn(SelPawn);
                    // saveablePawn.targetTemperaturesOverride = false;
                    pawnStatCache.UpdateTemperatureIfNecessary(true);
                }

                TooltipHandler.TipRegion(tempResetRect, "TemperatureRangeReset".Translate());
            }

            Text.Font = GameFont.Small;
            TryDrawComfyTemperatureRange(ref cur.y, canvas.width);
        }

        private void DrawThingRowModded(ref float y, float width, Apparel apparel)
        {
            if (apparel == null)
            {
                DrawThingRowVanilla(ref y, width, null);
                return;
            }

            var rect = new Rect(0f, y, width, ThingRowHeight);

            if (Mouse.IsOver(rect))
            {
                GUI.color = HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }

            GUI.color = ThingLabelColor;

            // LMB doubleclick
            if (Widgets.ButtonInvisible(rect))
            {
                // Left Mouse Button Menu
                if (Event.current.button == 0)
                {
                    Find.WindowStack.Add(new Window_Pawn_ApparelDetail(SelPawn, apparel));
                }

                // RMB menu
                if (Event.current.button == 1)
                {
                    var floatOptionList =
                        new List<FloatMenuOption>
                        {
                            new FloatMenuOption(
                                "ThingInfo".Translate(),
                                delegate { Find.WindowStack.Add(new Dialog_InfoCard(apparel)); })
                        };

                    if (CanControl)
                    {
                        floatOptionList.Add(
                            new FloatMenuOption(
                                "OutfitterComparer".Translate(),
                                delegate
                                {
                                    Find.WindowStack.Add(
                                        new
                                            Dialog_PawnApparelComparer(SelPawnForGear,
                                                apparel));
                                }));

                        void DropApparel()
                        {
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                            InterfaceDrop(apparel);
                        }

                        void DropApparelHaul()
                        {
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                            InterfaceDropHaul(apparel);
                        }

                        floatOptionList.Add(new FloatMenuOption("DropThing".Translate(), DropApparel));
                        floatOptionList.Add(new FloatMenuOption("DropThingHaul".Translate(), DropApparelHaul));
                    }

                    var window = new FloatMenu(floatOptionList, string.Empty);
                    Find.WindowStack.Add(window);
                }
            }

            if (apparel.def.DrawMatSingle != null && apparel.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y + 5f, ThingIconSize, ThingIconSize), apparel);
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ThingLabelColor;
            var textRect = new Rect(ThingLeftX, y, width - ThingLeftX, ThingRowHeight - Text.LineHeight);
            var scoreRect = new Rect(ThingLeftX, textRect.yMax, width - ThingLeftX, Text.LineHeight);

            var conf = SelPawn.GetApparelStatCache();
            var text = apparel.LabelCap;
            var textScore = Math.Round(conf.ApparelScoreRaw(apparel), 2).ToString("N2");

            if (SelPawn.outfits != null
                && SelPawn.outfits.forcedHandler.IsForced(apparel))
            {
                text = text + ", " + "ApparelForcedLower".Translate();
                Widgets.Label(textRect, text);
            }
            else
            {
                GUI.color = new Color(0.75f, 0.75f, 0.75f);
                if (apparel.def.useHitPoints)
                {
                    var x = apparel.HitPoints / (float) apparel.MaxHitPoints;
                    if (x < 0.5f)
                    {
                        GUI.color = Color.yellow;
                    }

                    if (x < 0.2f)
                    {
                        GUI.color = Color.red;
                    }
                }

                Widgets.Label(textRect, text);
                GUI.color = Color.white;
                Widgets.Label(scoreRect, textScore);
            }

            y += ThingRowHeight;
        }

        private void DrawThingRowVanilla(ref float y, float width, Thing thing)
        {
            var rect = new Rect(0f, y, width, 28f);
            if (Mouse.IsOver(rect))
            {
                GUI.color = HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }

            GUI.color = ThingLabelColor;
            var rect2A = new Rect(rect.width - 24f, y, 24f, 24f);
            UIHighlighter.HighlightOpportunity(rect, "InfoCard");
            TooltipHandler.TipRegion(rect2A, "DefInfoTip".Translate());
            if (Widgets.ButtonImage(rect2A, OutfitterTextures.Info))
            {
                Find.WindowStack.Add(new Dialog_InfoCard(thing));
            }

            if (CanControl)
            {
                var rect2 = new Rect(rect.width - 24f, y, 24f, 24f);
                TooltipHandler.TipRegion(rect2, "DropThing".Translate());
                if (Widgets.ButtonImage(rect2, OutfitterTextures.Drop))
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    InterfaceDrop(thing);
                }

                rect.width -= 24f;
            }

            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), thing);
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ThingLabelColor;
            var rect3 = new Rect(ThingLeftX, y, width - ThingLeftX, 28f);
            var text = thing.LabelCap;
            if (thing is Apparel apparel && SelPawn.outfits != null
                                         && SelPawn.outfits.forcedHandler.IsForced(apparel))
            {
                text = text + ", " + "ApparelForcedLower".Translate();
            }

            Widgets.Label(rect3, text);
            y += ThingRowHeight;
        }

        private void InterfaceDrop([NotNull] Thing t)
        {
            if (t is Apparel apparel)
            {
                var selPawnForGear = SelPawn;
                if (!selPawnForGear.jobs.IsCurrentJobPlayerInterruptible())
                {
                    return;
                }

                var job = new Job(JobDefOf.RemoveApparel, apparel) {playerForced = true};
                selPawnForGear.jobs.TryTakeOrderedJob(job);
            }
            else if (t is ThingWithComps thingWithComps
                     && SelPawn.equipment.AllEquipmentListForReading.Contains(thingWithComps))
            {
                SelPawn.equipment.TryDropEquipment(
                    thingWithComps,
                    out _, SelPawn.Position);
            }
            else if (!t.def.destroyOnDrop)
            {
                SelPawn.inventory.innerContainer.TryDrop(t, ThingPlaceMode.Near, out _);
            }
        }

        private void InterfaceDropHaul(Thing t)
        {
            if (t is Apparel apparel)
            {
                var selPawnForGear = SelPawn;
                if (!selPawnForGear.jobs.IsCurrentJobPlayerInterruptible())
                {
                    return;
                }

                var job =
                    new Job(JobDefOf.RemoveApparel, apparel) {playerForced = true, haulDroppedApparel = true};
                selPawnForGear.jobs.TryTakeOrderedJob(job);
            }
            else if (t is ThingWithComps thingWithComps
                     && SelPawn.equipment.AllEquipmentListForReading.Contains(thingWithComps))
            {
                SelPawn.equipment.TryDropEquipment(
                    thingWithComps,
                    out _, SelPawn.Position);
            }
            else if (!t.def.destroyOnDrop)
            {
                SelPawn.inventory.innerContainer.TryDrop(t, ThingPlaceMode.Near, out _);
            }
        }

        private void TryDrawComfyTemperatureRange(ref float curY, float width)
        {
            if (SelPawnForGear.Dead)
            {
                return;
            }

            var rect = new Rect(0f, curY, width, 22f);
            var statValue = SelPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMin);
            var statValue2 = SelPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMax);
            Widgets.Label(
                rect,
                string.Concat(
                    "ComfyTemperatureRange".Translate(),
                    ": ",
                    statValue.ToStringTemperature("F0"),
                    " ~ ",
                    statValue2.ToStringTemperature("F0")));
            curY += 22f;
        }

        #endregion Private Methods
    }
}