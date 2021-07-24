using System;
using System.Collections.Generic;
using System.Linq;
using KillfaceTools.FMO;
using RimWorld;
using UnityEngine;
using Verse;

namespace Outfitter.TabPatch
{
    public static class Tab_Bills_Patch
    {
        public delegate void Postfix(ref Rect rect);

        private const string Separator = "   ";

        private const string NewLine = "\n-------------------------------";

        private static float _viewHeight = 1000f;

        private static Vector2 _scrollPosition;

        private static readonly Vector2 WinSize = new Vector2(420f, 480f);

        private static Bill _mouseoverBill;

        // RimWorld.ITab_Bills
        public static bool FillTab_Prefix()
        {
            var selTable = (Building_WorkTable) Find.Selector.SingleSelectedThing;
            if (!Controller.Settings.UseCustomTailorWorkbench
                || selTable.def != ThingDef.Named("HandTailoringBench")
                && selTable.def != ThingDef.Named("ElectricTailoringBench"))
            {
                return true;
            }

            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.BillsTab, KnowledgeAmount.FrameDisplayed);
            var x = WinSize.x;
            var winSize2 = WinSize;
            var rect2 = new Rect(0f, 0f, x, winSize2.y).ContractedBy(10f);

            Dictionary<string, List<FloatMenuOption>> LabeledSortingActions()
            {
                var dictionary = new Dictionary<string, List<FloatMenuOption>>();

                // Dictionary<string, List<FloatMenuOption>> dictionary2 = new Dictionary<string, List<FloatMenuOption>>();
                var recipesWithoutPart = selTable.def.AllRecipes.Where(bam =>
                    bam.products?.FirstOrDefault()?.thingDef?.apparel?.bodyPartGroups.NullOrEmpty() ?? true).ToList();
                var recipesWithPart = selTable.def.AllRecipes.Where(bam =>
                    !bam.products?.FirstOrDefault()?.thingDef?.apparel?.bodyPartGroups.NullOrEmpty() ?? false).ToList();
                recipesWithPart.SortByDescending(blum => blum.label);

                foreach (var recipeDef in recipesWithoutPart)
                {
                    if (!recipeDef.AvailableNow)
                    {
                        continue;
                    }

                    var recipe = recipeDef;

                    void Action()
                    {
                        var any = false;
                        foreach (var col in selTable.Map.mapPawns.FreeColonists)
                        {
                            if (!recipe.PawnSatisfiesSkillRequirements(col))
                            {
                                continue;
                            }

                            any = true;
                            break;
                        }

                        if (!any)
                        {
                            Bill.CreateNoPawnsWithSkillDialog(recipe);
                        }

                        var bill = recipe.MakeNewBill();
                        selTable.billStack.AddBill(bill);
                        if (recipe.conceptLearned != null)
                        {
                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(recipe.conceptLearned, KnowledgeAmount.Total);
                        }

                        if (TutorSystem.TutorialMode)
                        {
                            TutorSystem.Notify_Event(new EventPack("AddBill-" + recipe.LabelCap));
                        }
                    }

                    var floatMenuOption = new FloatMenuOption(recipe.LabelCap, Action, MenuOptionPriority.Default, null,
                        null, 29f,
                        rect => Widgets.InfoCardButton((float) (rect.x + 5.0),
                            (float) (rect.y + ((rect.height - 24.0) / 2.0)), recipe));

                    dictionary.Add(recipe.LabelCap, new List<FloatMenuOption> {floatMenuOption});
                }

                foreach (var recipeDef in recipesWithPart)
                {
                    if (!recipeDef.AvailableNow)
                    {
                        continue;
                    }

                    var recipe = recipeDef;

                    var recipeProduct = recipe.products.FirstOrDefault();

                    var colonistsWithThing = new List<Pawn>();
                    if (recipeProduct != null && recipeProduct.thingDef.IsApparel)
                    {
                        colonistsWithThing = selTable.Map.mapPawns.FreeColonistsSpawned.Where(p =>
                                p.apparel.WornApparel.Any(ap => ap.def == recipeProduct.thingDef))
                            .ToList();
                    }

                    void MouseoverGuiAction()
                    {
                        var tooltip = string.Empty;

                        for (var index = 0; index < recipe.ingredients.Count; index++)
                        {
                            var ingredient = recipe.ingredients[index];
                            if (index > 0)
                            {
                                tooltip += ", ";
                            }

                            tooltip += ingredient.Summary;
                        }

                        tooltip += "\n";

                        if (recipeProduct != null)
                        {
                            var thingDef = recipeProduct.thingDef;
                            for (var index = 0; index < thingDef.apparel.bodyPartGroups.Count; index++)
                            {
                                var bpg = thingDef.apparel.bodyPartGroups[index];
                                if (index > 0)
                                {
                                    tooltip += ", ";
                                }

                                tooltip += bpg.LabelCap;
                            }

                            tooltip += "\n";
                            for (var index = 0; index < thingDef.apparel.layers.Count; index++)
                            {
                                var layer = thingDef.apparel.layers[index];
                                if (index > 0)
                                {
                                    tooltip += ", ";
                                }

                                tooltip += layer.ToString();
                            }

                            var statBases = thingDef.statBases
                                .Where(bing => bing.stat.category == StatCategoryDefOf.Apparel)
                                .ToList();
                            if (!statBases.NullOrEmpty())
                            {
                                // tooltip = StatCategoryDefOf.Apparel.LabelCap;
                                // tooltip += "\n-------------------------------";
                                tooltip += "\n";
                                foreach (var statOffset in statBases)
                                {
                                    {
                                        // if (index > 0)
                                        tooltip += "\n";
                                    }

                                    tooltip += statOffset.stat.LabelCap + Separator + statOffset.ValueToStringAsOffset;
                                }
                            }

                            if (!thingDef.equippedStatOffsets.NullOrEmpty())
                            {
                                // if (tooltip == string.Empty)
                                // {
                                // tooltip = StatCategoryDefOf.EquippedStatOffsets.LabelCap;
                                // }
                                {
                                    // else
                                    tooltip += "\n\n" + StatCategoryDefOf.EquippedStatOffsets.LabelCap;
                                }

                                tooltip += NewLine;
                                foreach (var statOffset in thingDef.equippedStatOffsets)
                                {
                                    tooltip += "\n";
                                    tooltip += statOffset.stat.LabelCap + Separator + statOffset.ValueToStringAsOffset;
                                }
                            }
                        }

                        if (colonistsWithThing.Count > 0)
                        {
                            tooltip += "\n\nWorn by: ";
                            for (var j = 0; j < colonistsWithThing.Count; j++)
                            {
                                var p = colonistsWithThing[j];
                                if (j > 0)
                                {
                                    tooltip += j != colonistsWithThing.Count - 1 ? ", " : " and ";
                                }

                                tooltip += p.LabelShort;
                            }
                        }

                        TooltipHandler.TipRegion(
                            new Rect(Event.current.mousePosition.x - 5f, Event.current.mousePosition.y - 5f, 10f, 10f),
                            tooltip);
                    }

                    void Action()
                    {
                        var any = false;
                        foreach (var col in selTable.Map.mapPawns.FreeColonists)
                        {
                            if (!recipe.PawnSatisfiesSkillRequirements(col))
                            {
                                continue;
                            }

                            any = true;
                            break;
                        }

                        if (!any)
                        {
                            Bill.CreateNoPawnsWithSkillDialog(recipe);
                        }

                        var bill = recipe.MakeNewBill();
                        selTable.billStack.AddBill(bill);
                        if (recipe.conceptLearned != null)
                        {
                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(recipe.conceptLearned, KnowledgeAmount.Total);
                        }

                        if (TutorSystem.TutorialMode)
                        {
                            TutorSystem.Notify_Event(new EventPack("AddBill-" + recipe.LabelCap));
                        }
                    }

                    var floatMenuOption = new FloatMenuOption(recipe.LabelCap, Action, MenuOptionPriority.Default,
                        delegate { MouseoverGuiAction(); }, null, 29f,
                        rect => Widgets.InfoCardButton((float) (rect.x + 5.0),
                            (float) (rect.y + ((rect.height - 24.0) / 2.0)), recipe));

                    // recipe.products?.FirstOrDefault()?.thingDef));

                    // list.Add(new FloatMenuOption("LoL", null));
                    // Outfitter jump in here

                    // for (int j = 0; j < recipe.products.Count; j++)
                    // {
                    if (recipeProduct == null)
                    {
                        continue;
                    }

                    var count = selTable.Map.listerThings.ThingsOfDef(recipeProduct.thingDef).Count;

                    var wornCount = colonistsWithThing.Count;

                    for (var k = 0; k < recipeProduct.thingDef?.apparel?.bodyPartGroups?.Count; k++)
                    {
                        var bPart = recipeProduct.thingDef.apparel.bodyPartGroups[k];

                        string key = bPart.LabelCap + Tools.NestedString;

                        if (!dictionary.ContainsKey(key))
                        {
                            dictionary.Add(key, new List<FloatMenuOption>());
                        }

                        if (k == 0)
                        {
                            floatMenuOption.Label += " (" + count + "/" + wornCount + ")";

                            // + "\n"
                            // + recipeProduct.thingDef.equippedStatOffsets.ToStringSafeEnumerable();
                        }

                        dictionary[key].Add(floatMenuOption);
                    }
                }

                // Dictionary<string, List<FloatMenuOption>> list2 = new Dictionary<string, List<FloatMenuOption>>();
                // dictionary2 = dictionary2.OrderByDescending(c => c.Key).ToDictionary(KeyValuePair<string, List<FloatMenuOption>>);
                if (!dictionary.Any())
                {
                    dictionary.Add("NoneBrackets".Translate(), new List<FloatMenuOption> {null});
                }

                // else
                // {
                // foreach (KeyValuePair<string, List<FloatMenuOption>> pair in list)
                // {
                // string label = pair.Key;
                // if (pair.Value.Count == 1)
                // {
                // label = pair.Value.FirstOrDefault().Label;
                // }
                // list2.Add(label, pair.Value);
                // }
                // }
                return dictionary;
            }

            _mouseoverBill = DoListing(selTable.BillStack, rect2, LabeledSortingActions, ref _scrollPosition,
                ref _viewHeight);

            return false;
        }

        public static bool TabUpdate_Prefix()
        {
            var selTable = (Building_WorkTable) Find.Selector.SingleSelectedThing;
            if (selTable.def != ThingDef.Named("HandTailoringBench")
                && selTable.def != ThingDef.Named("ElectricTailoringBench"))
            {
                return true;
            }

            if (_mouseoverBill == null)
            {
                return false;
            }

            _mouseoverBill.TryDrawIngredientSearchRadiusOnMap(Find.Selector.SingleSelectedThing.Position);
            _mouseoverBill = null;

            return false;
        }

        private static Bill DoListing(BillStack __instance, Rect rect,
            Func<Dictionary<string, List<FloatMenuOption>>> labeledSortingActions, ref Vector2 scrollPosition,
            ref float viewHeight)
        {
            Bill result = null;
            GUI.BeginGroup(rect);
            Text.Font = GameFont.Small;
            if (__instance.Count < 15)
            {
                var rect2 = new Rect(0f, 0f, 150f, 29f);
                if (Widgets.ButtonText(rect2, "AddBill".Translate()))
                {
                    // Outfitter Code
                    var items = labeledSortingActions.Invoke().Keys.Select(
                        label =>
                        {
                            var fmo = labeledSortingActions.Invoke()[label];
                            return Tools.MakeMenuItemForLabel(label, fmo);
                        }).ToList();

                    Tools.LabelMenu = new FloatMenuLabels(items);
                    Find.WindowStack.Add(Tools.LabelMenu);
                }

                UIHighlighter.HighlightOpportunity(rect2, "AddBill");
            }

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            var outRect = new Rect(0f, 35f, rect.width, (float) (rect.height - 35.0));
            var viewRect = new Rect(0f, 0f, (float) (outRect.width - 16.0), viewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            var num = 0f;
            for (var i = 0; i < __instance.Count; i++)
            {
                var bill = __instance.Bills[i];

                var rect3 = bill.DoInterface(0f, num, viewRect.width, i);
                if (!bill.DeletedOrDereferenced && Mouse.IsOver(rect3))
                {
                    result = bill;
                }

                num = (float) (num + (rect3.height + 6.0));
            }

            if (Event.current.type == EventType.Layout)
            {
                viewHeight = (float) (num + 60.0);
            }

            Widgets.EndScrollView();
            GUI.EndGroup();
            DoBwmPostfix?.Invoke(ref rect);
            return result;
        }

        public static event Postfix DoBwmPostfix;
    }
}