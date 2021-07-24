using System;
using System.Linq;
using JetBrains.Annotations;
using Outfitter.Textures;
using RimWorld;
using UnityEngine;
using Verse;
using static UnityEngine.GUILayout;

namespace Outfitter
{
    public class Window_Pawn_ApparelDetail : Verse.Window
    {
        private const float BaseValue = 85f;

        private readonly Apparel _apparel;

        private readonly GUIStyle _fontBold =
            new GUIStyle
            {
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                },
                padding = new RectOffset(0, 0, 12, 6)
            };

        private readonly GUIStyle _headline =
            new GUIStyle
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                normal =
                {
                    textColor = Color.white
                },
                padding = new RectOffset(0, 0, 12, 6)
            };

        private readonly GUIStyle _hoverBox = new GUIStyle {hover = {background = OutfitterTextures.BgColor}};

        private readonly Pawn _pawn;

        private readonly GUIStyle _whiteLine = new GUIStyle {normal = {background = OutfitterTextures.White}};

        private Def _def;

        private Vector2 _scrollPosition;

        private ThingDef _stuff;

        public Window_Pawn_ApparelDetail(Pawn pawn, Apparel apparel)
        {
            doCloseX = true;
            closeOnClickedOutside = true;
            //this.closeOnEscapeKey = true;
            doCloseButton = true;
            preventCameraMotion = false;

            _pawn = pawn;
            _apparel = apparel;
        }

        public override Vector2 InitialSize => new Vector2(510f, 550f);

        [CanBeNull]
        private Def Def
        {
            get
            {
                if (_apparel != null)
                {
                    return _apparel.def;
                }

                return _def;
            }
        }

        private bool IsVisible
        {
            get
            {
                // thing selected is a pawn
                if (SelPawn == null)
                {
                    return false;
                }

                // of this colony
                if (SelPawn.Faction != Faction.OfPlayer)
                {
                    return false;
                }

                // and has apparel (that should block everything without apparel, animals, bots, that sort of thing)
                if (SelPawn.apparel == null)
                {
                    return false;
                }

                return true;
            }
        }

        private Pawn SelPawn => Find.Selector.SingleSelectedThing as Pawn;

        public override void DoWindowContents(Rect inRect)
        {
            var conf = _pawn.GetApparelStatCache();

            var conRect = new Rect(inRect);

            conRect.height -= 50f;

            BeginArea(conRect);

            // begin main group
            BeginVertical();

            Label(GetTitle(), _headline);
            Text.Font = GameFont.Small;

            // GUI.BeginGroup(contentRect);
            var labelWidth = conRect.width - BaseValue - BaseValue - BaseValue - 48f;

            DrawLine("Status", labelWidth, "BaseMod", "Strength", "Score", _fontBold);

            Space(6f);
            Label(string.Empty, _whiteLine, Height(1));
            Space(6f);

            var apparelEntry = conf.GetAllOffsets(_apparel);

            var equippedOffsets = apparelEntry.EquippedOffsets;
            var statBases = apparelEntry.StatBases;
            var infusedOffsets = apparelEntry.InfusedOffsets;

            _scrollPosition = BeginScrollView(_scrollPosition, Width(conRect.width));

            // relevant apparel stats

            // start score at 1
            float score = 1;

            // add values for each statdef modified by the apparel
            foreach (var statPriority in _pawn.GetApparelStatCache().StatCache
                .OrderBy(i => i.Stat.LabelCap))
            {
                var stat = statPriority.Stat;
                string statLabel = stat.LabelCap;

                // statbases, e.g. armor

                // StatCache.DoApparelScoreRaw_PawnStatsHandlers(_pawn, _apparel, statPriority.Stat, ref currentStat);
                if (statBases.Contains(stat))
                {
                    var statValue = _apparel.GetStatValue(stat);
                    var statScore = 0f;
                    if (ApparelStatCache.SpecialStats.Contains(stat))
                    {
                        ApparelStatCache.CalculateScoreForSpecialStats(_apparel, statPriority, _pawn, statValue,
                            ref statScore);
                    }
                    else
                    {
                        // statValue += StatCache.StatInfused(infusionSet, statPriority, ref baseInfused);
                        statScore = statValue * statPriority.Weight;
                    }

                    score += statScore;

                    DrawLine(
                        statLabel,
                        labelWidth,
                        statValue.ToStringPercent("N1"),
                        statPriority.Weight.ToString("N2"),
                        statScore.ToString("N2"));
                }

                if (equippedOffsets.Contains(stat))
                {
                    var statValue = _apparel.GetEquippedStatValue(_pawn, stat);

                    // statValue += StatCache.StatInfused(infusionSet, statPriority, ref equippedInfused);
                    var statScore = 0f;
                    if (ApparelStatCache.SpecialStats.Contains(stat))
                    {
                        ApparelStatCache.CalculateScoreForSpecialStats(_apparel, statPriority, _pawn, statValue,
                            ref statScore);
                    }
                    else
                    {
                        statScore = statValue * statPriority.Weight;
                    }

                    score += statScore;

                    DrawLine(
                        statLabel,
                        labelWidth,
                        statValue.ToStringPercent("N1"),
                        statPriority.Weight.ToString("N2"),
                        statScore.ToString("N2"));
                }

                if (!infusedOffsets.Contains(stat))
                {
                    continue;
                }

                {
                    GUI.color = Color.green; // new Color(0.5f, 1f, 1f, 1f);

                    // float statInfused = StatCache.StatInfused(infusionSet, statPriority, ref dontcare);
                    ApparelStatCache.DoApparelScoreRaw_PawnStatsHandlers(_apparel, stat, out var statValue);

                    var badArmor = true;

                    var statScore = 0f;
                    if (ApparelStatCache.SpecialStats.Contains(stat))
                    {
                        ApparelStatCache.CalculateScoreForSpecialStats(_apparel,
                            statPriority, _pawn,
                            statValue,
                            ref statScore);
                    }
                    else
                    {
                        // Bug with Infused and "Ancient", it completely kills the pawn's armor
                        if (statValue < 0
                            && (stat == StatDefOf.ArmorRating_Blunt || stat == StatDefOf.ArmorRating_Sharp))
                        {
                            score = -2f;
                            badArmor = false;
                        }

                        statScore = statValue * statPriority.Weight;
                    }

                    DrawLine(
                        statLabel,
                        labelWidth,
                        statValue.ToStringPercent("N1"),
                        statPriority.Weight.ToString("N2"),
                        statScore.ToString("N2"));

                    GUI.color = Color.white;

                    if (badArmor)
                    {
                        score += statScore;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            GUI.color = Color.white;

            // end upper group
            EndScrollView();

            // begin lower group
            FlexibleSpace();
            Space(6f);
            Label(string.Empty, _whiteLine, Height(1));
            Space(6f);
            DrawLine(string.Empty, labelWidth, "Modifier", string.Empty, "Subtotal");

            DrawLine("BasicStatusOfApparel".Translate(), labelWidth, "1.00", "+", score.ToString("N2"));

            var special = _apparel.GetSpecialApparelScoreOffset();
            if (Math.Abs(special) > 0f)
            {
                score += special;

                DrawLine(
                    "OutfitterSpecialScore".Translate(),
                    labelWidth,
                    special.ToString("N2"),
                    "+",
                    score.ToString("N2"));
            }

            var armor = ApparelStatCache.ApparelScoreRaw_ProtectionBaseStat(_apparel);

            if (Math.Abs(armor) > 0.01f)
            {
                score += armor;

                DrawLine("OutfitterArmor".Translate(), labelWidth, armor.ToString("N2"), "+", score.ToString("N2"));
            }

            if (_apparel.def.useHitPoints)
            {
                // durability on 0-1 scale
                var x = _apparel.HitPoints / (float) _apparel.MaxHitPoints;
                score *= ApparelStatsHelper.HitPointsPercentScoreFactorCurve.Evaluate(x);

                DrawLine(
                    "OutfitterHitPoints".Translate(),
                    labelWidth,
                    x.ToString("N2"),
                    "weighted",
                    score.ToString("N2"));

                GUI.color = Color.white;
            }

            if (_apparel.WornByCorpse && ThoughtUtility.CanGetThought(_pawn, ThoughtDefOf.DeadMansApparel))
            {
                score -= 0.5f;
                if (score > 0f)
                {
                    score *= 0.1f;
                }

                DrawLine(
                    "OutfitterWornByCorpse".Translate(),
                    labelWidth,
                    "modified",
                    "weighted",
                    score.ToString("N2"));
            }

            if (_apparel.Stuff == ThingDefOf.Human.race.leatherDef)
            {
                if (ThoughtUtility.CanGetThought(_pawn, ThoughtDefOf.HumanLeatherApparelSad))
                {
                    score -= 0.5f;
                    if (score > 0f)
                    {
                        score *= 0.1f;
                    }
                }

                if (ThoughtUtility.CanGetThought(_pawn, ThoughtDefOf.HumanLeatherApparelHappy))
                {
                    score *= 2f;
                }

                DrawLine(
                    "OutfitterHumanLeather".Translate(),
                    labelWidth,
                    "modified",
                    "weighted",
                    score.ToString("N2"));
            }

            var temperature = conf.ApparelScoreRaw_Temperature(_apparel);

            if (Math.Abs(temperature - 1f) > 0)
            {
                score *= temperature;

                DrawLine(
                    "OutfitterTemperature".Translate(),
                    labelWidth,
                    temperature.ToString("N2"),
                    "*",
                    score.ToString("N2"));
            }

            DrawLine(
                "OutfitterTotal".Translate(),
                labelWidth,
                string.Empty,
                "=",
                conf.ApparelScoreRaw(_apparel).ToString("N2"));

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // end main group
            EndVertical();
            EndArea();
        }

        public override void WindowUpdate()
        {
            if (!IsVisible)
            {
                Close(false);
            }
        }

        protected override void SetInitialSizeAndPosition()
        {
            var inspectWorker = (MainTabWindow_Inspect) MainButtonDefOf.Inspect.TabWindow;
            windowRect = new Rect(
                770f,
                inspectWorker.PaneTopY - 30f - InitialSize.y, InitialSize.x, InitialSize.y).Rounded();
        }

        private void DrawLine(
            string statDefLabelText,
            float statDefLabelWidth,
            string statDefValueText,
            string multiplierText,
            string finalValueText,
            GUIStyle style = null)
        {
            if (style != null)
            {
                BeginHorizontal(style);
            }
            else
            {
                BeginHorizontal(_hoverBox);
            }

            Label(statDefLabelText, Width(statDefLabelWidth));
            Label(statDefValueText, Width(BaseValue));
            Label(multiplierText, Width(BaseValue));
            Label(finalValueText, Width(BaseValue));
            EndHorizontal();

            // Text.Anchor = TextAnchor.UpperLeft;
            // Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, statDefLabelWidth, itemRect.height), statDefLabelText);
            // itemRect.xMin += statDefLabelWidth;
            // Text.Anchor = TextAnchor.UpperRight;
            // Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, statDefValueWidth, itemRect.height), statDefValueText);
            // itemRect.xMin += statDefValueWidth;
            // Text.Anchor = TextAnchor.UpperRight;
            // Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, multiplierWidth, itemRect.height), multiplierText);
            // itemRect.xMin += multiplierWidth;
            // Text.Anchor = TextAnchor.UpperRight;
            // Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, finalValueWidth, itemRect.height), finalValueText);
            // itemRect.xMin += finalValueWidth;
        }

        private string GetTitle()
        {
            if (_apparel != null)
            {
                return _apparel.LabelCap;
            }

            if (Def is ThingDef thingDef)
            {
                return GenLabel.ThingLabel(thingDef, _stuff).CapitalizeFirst();
            }

            return Def?.LabelCap;
        }

#pragma warning disable 649
#pragma warning restore 649
#pragma warning disable 649
#pragma warning restore 649
    }
}