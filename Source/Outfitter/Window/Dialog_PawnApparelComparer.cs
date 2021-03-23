using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace Outfitter.Window
{
    public class Dialog_PawnApparelComparer : Verse.Window
    {
        private const float ScoreWidth = 100f;

        [NotNull] private readonly Apparel _apparel;

        [NotNull] private readonly Pawn _pawn;

        private Dictionary<Apparel, float> _dict;

        private Vector2 _scrollPosition;

        public Dialog_PawnApparelComparer(Pawn p, Apparel apparel)
        {
            doCloseX = true;
            //this.closeOnEscapeKey = true;
            doCloseButton = true;

            _pawn = p;
            _apparel = apparel;
        }

        public override Vector2 InitialSize => new Vector2(500f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            var apparelStatCache = _pawn.GetApparelStatCache();
            var currentOutfit = _pawn.outfits.CurrentOutfit;

            if (_dict == null || Find.TickManager.TicksGame % 60 == 0 || GUI.changed)
            {
                var ap = new List<Apparel>(_pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel)
                    .OfType<Apparel>().Where(
                        x => x.Map.haulDestinationManager.SlotGroupAt(x.Position) != null));

                foreach (var otherPawn in PawnsFinder.AllMaps_FreeColonists.Where(x => x.Map == _pawn.Map))
                {
                    foreach (var pawnApparel in otherPawn.apparel.WornApparel)
                    {
                        if (otherPawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(pawnApparel))
                        {
                            ap.Add(pawnApparel);
                        }
                    }
                }

                ap = ap.Where(
                    i => !ApparelUtility.CanWearTogether(_apparel.def, i.def, _pawn.RaceProps.body)
                         && currentOutfit.filter.Allows(i)).ToList();


                ap = ap.OrderByDescending(
                    i =>
                    {
                        var g = _pawn.ApparelScoreGain(i);
                        return g;
                    }).ToList();

                _dict = new Dictionary<Apparel, float>();
                foreach (var currentAppel in ap)
                {
                    var gain = _pawn.ApparelScoreGain(currentAppel);
                    _dict.Add(currentAppel, gain);
                }
            }

            var groupRect = inRect.ContractedBy(10f);
            groupRect.height -= 100;
            GUI.BeginGroup(groupRect);


            var apparelLabelWidth = ((groupRect.width - (2 * ScoreWidth)) / 3) - 8f - 8f;
            var apparelEquippedWidth = apparelLabelWidth;
            var apparelOwnerWidth = apparelLabelWidth;

            var itemRect = new Rect(groupRect.xMin + 4f, groupRect.yMin, groupRect.width - 8f, 28f);

            DrawLine(
                ref itemRect,
                null,
                "Apparel",
                apparelLabelWidth,
                null,
                "Equiped",
                apparelEquippedWidth,
                null,
                "Target",
                apparelOwnerWidth,
                "Score",
                "Gain");

            groupRect.yMin += itemRect.height;
            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMin, groupRect.width);
            groupRect.yMin += 4f;
            groupRect.height -= 4f;
            groupRect.height -= Text.LineHeight * 1.2f * 3f;

            var viewRect = new Rect(
                groupRect.xMin,
                groupRect.yMin,
                groupRect.width - 16f, (_dict.Count * 28f) + 16f);
            if (viewRect.height < groupRect.height)
            {
                groupRect.height = viewRect.height;
            }

            var listRect = viewRect.ContractedBy(4f);

            Widgets.BeginScrollView(groupRect, ref _scrollPosition, viewRect);


            foreach (var kvp in _dict)
            {
                var currentAppel = kvp.Key;
                var gain = kvp.Value;

                itemRect = new Rect(listRect.xMin, listRect.yMin, listRect.width, 28f);
                if (Mouse.IsOver(itemRect))
                {
                    GUI.DrawTexture(itemRect, TexUI.HighlightTex);
                    GUI.color = Color.white;
                }

                var equipped = currentAppel.Wearer;

                var gainString = _pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(currentAppel)
                    ? gain.ToString("N3")
                    : "No Allow";

                DrawLine(
                    ref itemRect,
                    currentAppel,
                    currentAppel.LabelCap,
                    apparelLabelWidth,
                    equipped,
                    equipped?.LabelCap,
                    apparelEquippedWidth,
                    null,
                    null,
                    apparelOwnerWidth,
                    apparelStatCache.ApparelScoreRaw(currentAppel).ToString("N3"),
                    gainString
                );

                listRect.yMin = itemRect.yMax;
            }

            Widgets.EndScrollView();

            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMax, groupRect.width);

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }

        private void DrawLine(
            ref Rect itemRect,
            [CanBeNull] Apparel apparelThing,
            string apparelText,
            float textureWidth,
            [CanBeNull] Pawn equippedPawn,
            string apparelEquippedText,
            float apparelEquippedWidth,
            [CanBeNull] Pawn apparelOwnerThing,
            string apparelOwnerText,
            float apparelOwnerWidth,
            string apparelScoreText,
            string apparelGainText)
        {
            Rect fieldRect;
            var isCurrentlyWorn = equippedPawn != null;

            if (apparelThing != null)
            {
                fieldRect = new Rect(itemRect.xMin, itemRect.yMin, itemRect.height, itemRect.height);
                if (!apparelText.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(fieldRect, apparelText);
                }

                if (apparelThing.def.DrawMatSingle != null && apparelThing.def.DrawMatSingle.mainTexture != null)
                {
                    Widgets.ThingIcon(fieldRect, apparelThing);
                }

                if (Widgets.ButtonInvisible(fieldRect))
                {
                    Close();
                    Find.MainTabsRoot.EscapeCurrentTab();
                    if (isCurrentlyWorn)
                    {
                        //FIXME: Find.CameraDriver.JumpToVisibleMapLoc(equippedPawn.PositionHeld);
                        Find.Selector.ClearSelection();
                        if (equippedPawn.Spawned)
                        {
                            Find.Selector.Select(equippedPawn);
                        }
                    }
                    else
                    {
                        //FIXME: Find.CameraDriver.JumpToVisibleMapLoc(apparelThing.PositionHeld);
                        Find.Selector.ClearSelection();
                        if (apparelThing.Spawned)
                        {
                            Find.Selector.Select(apparelThing);
                        }
                    }

                    return;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(apparelText))
                {
                    fieldRect = new Rect(itemRect.xMin, itemRect.yMin, textureWidth, itemRect.height);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(fieldRect, apparelText);
                }
            }

            itemRect.xMin += textureWidth;

            if (isCurrentlyWorn)
            {
                fieldRect = new Rect(itemRect.xMin, itemRect.yMin, itemRect.height, itemRect.height);
                if (!apparelEquippedText.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(fieldRect, apparelEquippedText);
                }

                if (equippedPawn.def.DrawMatSingle != null
                    && equippedPawn.def.DrawMatSingle.mainTexture != null)
                {
                    Widgets.ThingIcon(fieldRect, equippedPawn);
                }

                if (Widgets.ButtonInvisible(fieldRect))
                {
                    Close();
                    Find.MainTabsRoot.EscapeCurrentTab();
                    Find.CameraDriver.JumpToCurrentMapLoc(equippedPawn.PositionHeld);
                    Find.Selector.ClearSelection();
                    if (equippedPawn.Spawned)
                    {
                        Find.Selector.Select(equippedPawn);
                    }

                    return;
                }
            }
            else
            {
                if (!apparelEquippedText.NullOrEmpty())
                {
                    fieldRect = new Rect(itemRect.xMin, itemRect.yMin, apparelEquippedWidth, itemRect.height);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(fieldRect, apparelText);
                }
            }

            itemRect.xMin += apparelEquippedWidth;

            if (apparelOwnerThing != null)
            {
                fieldRect = new Rect(itemRect.xMin, itemRect.yMin, itemRect.height, itemRect.height);
                if (!string.IsNullOrEmpty(apparelOwnerText))
                {
                    TooltipHandler.TipRegion(fieldRect, apparelOwnerText);
                }

                if (apparelOwnerThing.def.DrawMatSingle != null
                    && apparelOwnerThing.def.DrawMatSingle.mainTexture != null)
                {
                    Widgets.ThingIcon(fieldRect, apparelOwnerThing);
                }
            }
            else
            {
                if (!apparelOwnerText.NullOrEmpty())
                {
                    fieldRect = new Rect(itemRect.xMin, itemRect.yMin, apparelOwnerWidth, itemRect.height);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(fieldRect, apparelOwnerText);
                }
            }

            itemRect.xMin += apparelOwnerWidth;

            fieldRect = new Rect(itemRect.xMin, itemRect.yMin, ScoreWidth, itemRect.height);
            if (isCurrentlyWorn)
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
            }

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(fieldRect, apparelScoreText);

            itemRect.xMin += ScoreWidth;

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, ScoreWidth, itemRect.height), apparelGainText);
            GUI.color = Color.white;
            if (apparelThing == null)
            {
                return;
            }

            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonInvisible(fieldRect))
            {
                Find.WindowStack.Add(new Window_Pawn_ApparelDetail(_pawn, apparelThing));
            }
        }
    }
}