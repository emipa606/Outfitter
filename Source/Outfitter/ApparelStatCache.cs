// Outfitter/StatCache.cs
//
// Copyright Karel Kroeze, 2016.
//
// Created 2016-01-02 13:58

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Outfitter.Enums;
using RimWorld;
using UnityEngine;
using Verse;

namespace Outfitter
{
    public class ApparelStatCache
    {
        public delegate void ApparelScoreRawIgnored_WtHandlers(ref List<StatDef> statDef);

        public delegate void ApparelScoreRawInfusionHandlers(
            [NotNull] Apparel apparel,
            [NotNull] StatDef parentStat,
            ref HashSet<StatDef> infusedOffsets);

        public delegate void ApparelScoreRawStatsHandler(Apparel apparel, StatDef statDef, out float num);

        public const float MaxValue = 2.5f;

        public static readonly List<StatDef> SpecialStats =
            new List<StatDef>
            {
                StatDefOf.MentalBreakThreshold,
                StatDefOf.PsychicSensitivity,
                StatDefOf.ToxicSensitivity
            };

        private static readonly SimpleCurve Curve =
            new SimpleCurve {new CurvePoint(-5f, 0.1f), new CurvePoint(0f, 1f), new CurvePoint(100f, 4f)};

        private readonly Pawn _pawn;

        private readonly SaveablePawn _pawnSave;

        // public List<Apparel> recentApparel = new List<Apparel>();
        public readonly List<StatPriority> Cache;

        public readonly Dictionary<Apparel, float> RawScoreDict = new Dictionary<Apparel, float>();

        public readonly Dictionary<Apparel, Pawn> ToDropList = new Dictionary<Apparel, Pawn>();

        private int _lastStatUpdate;

        private int _lastTempUpdate;

        private int _lastWeightUpdate;

        public ApparelStatCache(Pawn pawn)
            : this(pawn.GetSaveablePawn())
        {
        }

        // public NeededWarmth neededWarmth;
        private ApparelStatCache([NotNull] SaveablePawn saveablePawn)
        {
            _pawn = saveablePawn.Pawn;
            _pawnSave = _pawn.GetSaveablePawn();
            Cache = new List<StatPriority>();
            _lastStatUpdate = -5000;
            _lastTempUpdate = -5000;
            _lastWeightUpdate = -5000;
        }

        [NotNull]
        public List<StatPriority> StatCache
        {
            get
            {
                // update auto stat priorities roughly between every vanilla gear check cycle
                if (Find.TickManager.TicksGame - _lastStatUpdate <=
                    JobGiver_OutfitterOptimizeApparel.ApparelStatCheck && !_pawnSave.ForceStatUpdate)
                {
                    return Cache;
                }

                // list of auto stats
                if (Cache.Count < 1 && _pawnSave.Stats.Count > 0)
                {
                    foreach (var statDef in _pawnSave.Stats)
                    {
                        Cache.Add(new StatPriority(statDef.Stat, statDef.Weight, statDef.Assignment));
                    }
                }

                RawScoreDict.Clear();
                _pawnSave.Stats.Clear();

                // clear auto priorities
                Cache.RemoveAll(stat => stat.Assignment == StatAssignment.Automatic);
                Cache.RemoveAll(stat => stat.Assignment == StatAssignment.Individual);

                // loop over each (new) stat
                // Armor only used by the Battle beacon, no relevance to jobs etc.
                if (_pawnSave.ArmorOnly)
                {
                    var updateArmorStats = _pawn.GetWeightedApparelArmorStats();
                    foreach (var pair in updateArmorStats)
                    {
                        // find index of existing priority for this stat
                        var i = Cache.FindIndex(stat => stat.Stat == pair.Key);

                        // if index -1 it doesnt exist yet, add it
                        if (i < 0)
                        {
                            var armorStats = new StatPriority(pair.Key, pair.Value);
                            Cache.Add(armorStats);
                        }
                        else
                        {
                            // it exists, make sure existing is (now) of type override.
                            Cache[i].Weight += pair.Value;
                        }
                    }
                }
                else
                {
                    var updateAutoPriorities = _pawn.GetWeightedApparelStats();
                    var updateIndividualPriorities =
                        _pawn.GetWeightedApparelIndividualStats();

                    // updateAutoPriorities = updateAutoPriorities.OrderBy(x => x.Key.label).ToDictionary(x => x.Key, x => x.Value);
                    updateAutoPriorities = updateAutoPriorities.OrderByDescending(x => Mathf.Abs(x.Value))
                        .ToDictionary(x => x.Key, x => x.Value);
                    updateIndividualPriorities = updateIndividualPriorities.OrderBy(x => x.Key.label)
                        .ToDictionary(x => x.Key, x => x.Value);

                    foreach (var pair in updateIndividualPriorities)
                    {
                        // find index of existing priority for this stat
                        var i = Cache.FindIndex(stat => stat.Stat == pair.Key);

                        // if index -1 it doesnt exist yet, add it
                        if (i < 0)
                        {
                            var individual =
                                new StatPriority(pair.Key, pair.Value, StatAssignment.Individual);
                            Cache.Add(individual);
                        }
                        else
                        {
                            // if exists, make sure existing is (now) of type override.
                            Cache[i].Assignment = StatAssignment.Override;
                        }
                    }

                    foreach (var pair in updateAutoPriorities)
                    {
                        // find index of existing priority for this stat
                        var i = Cache.FindIndex(stat => stat.Stat == pair.Key);

                        // if index -1 it doesnt exist yet, add it
                        if (i < 0)
                        {
                            Cache.Add(new StatPriority(pair));
                        }
                        else
                        {
                            // if exists, make sure existing is (now) of type override.
                            Cache[i].Assignment = StatAssignment.Override;
                        }
                    }
                }

                // update our time check.
                _lastStatUpdate = Find.TickManager.TicksGame;
                _pawnSave.ForceStatUpdate = false;
                _pawnSave.ArmorOnly = false;

                foreach (var statPriority in Cache.Where(
                    statPriority => statPriority.Assignment != StatAssignment.Automatic
                                    && statPriority.Assignment != StatAssignment.Individual))
                {
                    var exists = false;
                    foreach (var stat in _pawnSave.Stats.Where(
                        stat => stat.Stat.Equals(statPriority.Stat)))
                    {
                        stat.Weight = statPriority.Weight;
                        stat.Assignment = statPriority.Assignment;
                        exists = true;
                    }

                    if (exists)
                    {
                        continue;
                    }

                    var stats =
                        new Saveable_Pawn_StatDef
                        {
                            Stat = statPriority.Stat,
                            Assignment = statPriority.Assignment,
                            Weight = statPriority.Weight
                        };
                    _pawnSave.Stats.Add(stats);
                }

                return Cache;
            }
        }

        public FloatRange TargetTemperatures
        {
            get
            {
                UpdateTemperatureIfNecessary();
                return _pawnSave.TargetTemperatures;
            }

            set
            {
                _pawnSave.TargetTemperatures = value;
                _pawnSave.TargetTemperaturesOverride = true;
            }
        }

        private FloatRange TemperatureWeight
        {
            get
            {
                UpdateTemperatureIfNecessary(false, true);
                return _pawnSave.Temperatureweight;
            }
        }

        public static event ApparelScoreRawInfusionHandlers ApparelScoreRawFillInfusedStat;

        public static event ApparelScoreRawStatsHandler ApparelScoreRawPawnStatsHandlers;

        public static event ApparelScoreRawIgnored_WtHandlers IgnoredWtHandlers;

        public static float ApparelScoreRaw_ProtectionBaseStat(Apparel ap)
        {
            var num = ap.GetStatValue(StatDefOf.ArmorRating_Sharp)
                      + (ap.GetStatValue(StatDefOf.ArmorRating_Blunt) * 0.5f);

            return num * 0.1f;
        }

        public static void DoApparelScoreRaw_PawnStatsHandlers(
            [NotNull] Apparel apparel,
            [NotNull] StatDef statDef,
            out float num)
        {
            num = 0f;
            ApparelScoreRawPawnStatsHandlers?.Invoke(apparel, statDef, out num);
        }


        public static void FillIgnoredInfused_PawnStatsHandlers(ref List<StatDef> allApparelStats)
        {
            IgnoredWtHandlers?.Invoke(ref allApparelStats);
        }

        public float ApparelScoreRaw([NotNull] Apparel ap)
        {
            if (RawScoreDict.ContainsKey(ap))
            {
                return RawScoreDict[ap];
            }

            // only allow shields to be considered if a primary weapon is equipped and is melee

            if (ap.def.thingClass == typeof(ShieldBelt) && _pawn.equipment.Primary?.def.IsRangedWeapon == true)
            {
                return -1f;
            }

            // Fail safe to prevent pawns get out of the regular temperature.
            // Might help making pawn drop equipped apparel if it's too cold/warm.
            // this.GetInsulationStats(ap, out float insulationCold, out float insulationHeat);
            // FloatRange temperatureRange = this.pawn.ComfortableTemperatureRange();
            // if (ap.Wearer != thisPawn)
            // {
            // temperatureRange.min += insulationCold;
            // temperatureRange.max += insulationHeat;
            // }
            // if (temperatureRange.min > 12 && insulationCold > 0 || temperatureRange.max < 32 && insulationHeat < 0)
            // {
            // return -3f;
            // }

            // relevant apparel stats
            var entry = GetAllOffsets(ap);

            var statBases = entry.StatBases;
            var equippedOffsets = entry.EquippedOffsets;
            var infusedOffsets = entry.InfusedOffsets;

            // start score at 1
            float score = 1;

            // add values for each statdef modified by the apparel
            var stats = _pawn.GetApparelStatCache().StatCache;

            foreach (var statPriority in stats.Where(statPriority => statPriority != null))
            {
                var stat = statPriority.Stat;

                if (statBases.Contains(stat))
                {
                    var apStat = ap.GetStatValue(stat);

                    if (SpecialStats.Contains(stat))
                    {
                        CalculateScoreForSpecialStats(ap, statPriority, _pawn, apStat, ref score);
                    }
                    else
                    {
                        // add stat to base score before offsets are handled 
                        // (the pawn's apparel stat cache always has armors first as it is initialized with it).
                        score += apStat * statPriority.Weight;
                    }
                }

                // equipped offsets, e.g. movement speeds
                if (equippedOffsets.Contains(stat))
                {
                    var apStat = ap.GetEquippedStatValue(_pawn, stat);

                    if (SpecialStats.Contains(stat))
                    {
                        CalculateScoreForSpecialStats(ap, statPriority, _pawn, apStat, ref score);
                    }
                    else
                    {
                        score += apStat * statPriority.Weight;
                    }

                    // multiply score to favour items with multiple offsets
                    // score *= adjusted;

                    // debug.AppendLine( statWeightPair.Key.LabelCap + ": " + score );
                }

                // infusions
                if (!infusedOffsets.Contains(stat))
                {
                    continue;
                }

                // float statInfused = StatInfused(infusionSet, statPriority, ref dontcare);
                DoApparelScoreRaw_PawnStatsHandlers(ap, stat, out var statInfused);

                if (SpecialStats.Contains(stat))
                {
                    CalculateScoreForSpecialStats(ap, statPriority, _pawn, statInfused, ref score);
                }
                else
                {
                    // Bug with Infused and "Ancient", it completely kills the pawn's armor
                    if (statInfused < 0 && (stat == StatDefOf.ArmorRating_Blunt
                                            || stat == StatDefOf.ArmorRating_Sharp))
                    {
                        score = -2f;
                        return score;
                    }

                    score += statInfused * statPriority.Weight;
                }
            }

            score += ap.GetSpecialApparelScoreOffset();

            score += ApparelScoreRaw_ProtectionBaseStat(ap);

            // offset for apparel hitpoints
            if (ap.def.useHitPoints)
            {
                var x = ap.HitPoints / (float) ap.MaxHitPoints;
                score *= ApparelStatsHelper.HitPointsPercentScoreFactorCurve.Evaluate(x);
            }

            if (ap.WornByCorpse && ThoughtUtility.CanGetThought(_pawn, ThoughtDefOf.DeadMansApparel))
            {
                score -= 0.5f;
                if (score > 0f)
                {
                    score *= 0.1f;
                }
            }

            if (ap.Stuff == ThingDefOf.Human.race.leatherDef)
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
            }

            score *= ApparelScoreRaw_Temperature(ap);

            RawScoreDict.Add(ap, score);

            return score;
        }

        public static void CalculateScoreForSpecialStats(Apparel ap, StatPriority statPriority, Pawn thisPawn,
            float apStat, ref float score)
        {
            var current = thisPawn.GetStatValue(statPriority.Stat);
            var goal = statPriority.Weight;
            var defaultStat = thisPawn.def.GetStatValueAbstract(statPriority.Stat);

            if (thisPawn.story != null)
            {
                foreach (var trait in thisPawn.story.traits.allTraits)
                {
                    defaultStat += trait.OffsetOfStat(statPriority.Stat);
                }

                foreach (var trait in thisPawn.story.traits.allTraits)
                {
                    defaultStat *= trait.MultiplierOfStat(statPriority.Stat);
                }
            }

            if (ap.Wearer == thisPawn)
            {
                current -= apStat;
            }
            else
            {
                foreach (var worn in thisPawn.apparel.WornApparel)
                {
                    if (ApparelUtility.CanWearTogether(worn.def, ap.def, thisPawn.RaceProps.body))
                    {
                        continue;
                    }

                    var stat1 = worn.GetStatValue(statPriority.Stat);
                    var stat2 = worn.GetEquippedStatValue(thisPawn, statPriority.Stat);
                    DoApparelScoreRaw_PawnStatsHandlers(worn, statPriority.Stat, out var stat3);
                    current -= stat1 + stat2 + stat3;
                }
            }

            if (!(Math.Abs(current - goal) > 0.01f))
            {
                return;
            }

            var need = 1f - Mathf.InverseLerp(defaultStat, goal, current);
            score += Mathf.InverseLerp(current, goal, current + apStat) * need;
        }

        public float ApparelScoreRaw_Temperature([NotNull] Apparel apparel)
        {
            // float minComfyTemperature = pawnSave.RealComfyTemperatures.min;
            // float maxComfyTemperature = pawnSave.RealComfyTemperatures.max;
            var minComfyTemperature = _pawn.ComfortableTemperatureRange().min;
            var maxComfyTemperature = _pawn.ComfortableTemperatureRange().max;

            // temperature
            var targetTemperatures = TargetTemperatures;

            GetInsulationStats(apparel, out var insulationCold, out var insulationHeat);

            var log = apparel.LabelCap + " - InsCold: " + insulationCold + " - InsHeat: " + insulationHeat
                      + " - TargTemp: " + targetTemperatures + "\nMinComfy: " + minComfyTemperature + " - MaxComfy: "
                      + maxComfyTemperature;

            // if this gear is currently worn, we need to make sure the contribution to the pawn's comfy temps is removed so the gear is properly scored
            var wornApparel = _pawn.apparel.WornApparel;
            if (!wornApparel.NullOrEmpty())
            {
                if (wornApparel.Contains(apparel))
                {
                    // log += "\nPawn is wearer of this apparel.";
                    minComfyTemperature -= insulationCold;
                    maxComfyTemperature -= insulationHeat;
                }
                else
                {
                    // check if the candidate will replace existing gear
                    foreach (var wornAp in wornApparel)
                    {
                        if (ApparelUtility.CanWearTogether(wornAp.def, apparel.def, _pawn.RaceProps.body))
                        {
                            continue;
                        }

                        GetInsulationStats(wornAp, out var insulationColdWorn, out var insulationHeatWorn);

                        minComfyTemperature -= insulationColdWorn;
                        maxComfyTemperature -= insulationHeatWorn;

                        // Log.Message(apparel +"-"+ insulationColdWorn + "-" + insulationHeatWorn + "-" + minComfyTemperature + "-" + maxComfyTemperature);
                    }
                }
            }

            log += "\nBasic stat not worn - MinComfy: " + minComfyTemperature + " - MaxComfy: " + maxComfyTemperature;

            // now for the interesting bit.
            var temperatureScoreOffset = new FloatRange(0f, 0f);

            // isolation_cold is given as negative numbers < 0 means we're underdressed
            var neededInsulationCold = targetTemperatures.min - minComfyTemperature;

            // isolation_warm is given as positive numbers.
            var neededInsulationWarmth = targetTemperatures.max - maxComfyTemperature;

            var tempWeight = TemperatureWeight;
            log += "\nWeight: " + tempWeight + " - NeedInsCold: " + neededInsulationCold + " - NeedInsWarmth: "
                   + neededInsulationWarmth;

            if (neededInsulationCold < 0)
            {
                // currently too cold
                // caps ap to only consider the needed temp and don't give extra points
                if (neededInsulationCold > insulationCold)
                {
                    temperatureScoreOffset.min += neededInsulationCold;
                }
                else
                {
                    temperatureScoreOffset.min += insulationCold;
                }
            }
            else
            {
                // currently warm enough
                if (insulationCold > neededInsulationCold)
                {
                    // this gear would make us too cold
                    temperatureScoreOffset.min += insulationCold - neededInsulationCold;
                }
            }

            // invert for scoring
            temperatureScoreOffset.min *= -1;

            if (neededInsulationWarmth > 0)
            {
                // currently too warm
                // caps ap to only consider the needed temp and don't give extra points
                if (neededInsulationWarmth < insulationHeat)
                {
                    temperatureScoreOffset.max += neededInsulationWarmth;
                }
                else
                {
                    temperatureScoreOffset.max += insulationHeat;
                }
            }
            else
            {
                // currently cool enough
                if (insulationHeat < neededInsulationWarmth)
                {
                    // this gear would make us too warm
                    temperatureScoreOffset.max += insulationHeat - neededInsulationWarmth;
                }
            }

            // Punish bad apparel
            // temperatureScoreOffset.min *= temperatureScoreOffset.min < 0 ? 2f : 1f;
            // temperatureScoreOffset.max *= temperatureScoreOffset.max < 0 ? 2f : 1f;

            // New
            log += "\nPre-Evaluate: " + temperatureScoreOffset.min + " / " + temperatureScoreOffset.max;

            temperatureScoreOffset.min = Curve.Evaluate(temperatureScoreOffset.min * tempWeight.min);
            temperatureScoreOffset.max = Curve.Evaluate(temperatureScoreOffset.max * tempWeight.max);

            log += "\nScoreOffsetMin: " + temperatureScoreOffset.min + " - ScoreOffsetMax: "
                   + temperatureScoreOffset.max + " *= " + (temperatureScoreOffset.min * temperatureScoreOffset.max);

            if (Prefs.DevMode)
            {
                //Log.Message(log);
            }

            return temperatureScoreOffset.min * temperatureScoreOffset.max;

            // return 1 + (temperatureScoreOffset.min + temperatureScoreOffset.max) / 15;
        }

        private void GetInsulationStats(Apparel apparel, out float insulationCold, out float insulationHeat)
        {
            if (Outfitter.Cache.InsulationDict.TryGetValue(apparel, out var range))
            {
                insulationCold = range.min;
                insulationHeat = range.max;
                return;
            }

            // offsets on apparel
            insulationCold = apparel.GetStatValue(StatDefOf.Insulation_Cold) * -1;
            insulationHeat = apparel.GetStatValue(StatDefOf.Insulation_Heat);

            insulationCold -= apparel.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Cold);
            insulationHeat += apparel.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Heat);

            // offsets on apparel infusions
            DoApparelScoreRaw_PawnStatsHandlers(apparel, StatDefOf.ComfyTemperatureMin, out var infInsulationCold);
            DoApparelScoreRaw_PawnStatsHandlers(apparel, StatDefOf.ComfyTemperatureMax, out var infInsulationHeat);
            insulationCold += infInsulationCold;
            insulationHeat += infInsulationHeat;

            Outfitter.Cache.InsulationDict.Add(apparel, new FloatRange(insulationCold, insulationHeat));
        }

        public ApparelEntry GetAllOffsets([NotNull] Apparel ap)
        {
            if (Outfitter.Cache.ApparelEntries.ContainsKey(ap))
            {
                return Outfitter.Cache.ApparelEntries[ap];
            }

            var entry = new ApparelEntry();
            GetStatsOfApparel(ap, ref entry.EquippedOffsets, ref entry.StatBases);
            GetStatsOfApparelInfused(ap, ref entry.InfusedOffsets);

            Outfitter.Cache.ApparelEntries.Add(ap, entry);
            return entry;
        }

        public void UpdateTemperatureIfNecessary(bool force = false, bool forceweight = false)
        {
            if (Find.TickManager.TicksGame - _lastTempUpdate > JobGiver_OutfitterOptimizeApparel.ApparelStatCheck
                || force)
            {
                // get desired temperatures
                if (!_pawnSave.TargetTemperaturesOverride)
                {
                    // float temp = GenTemperature.GetTemperatureAtTile(thisPawn.Map.Tile);
                    var lowest = LowestTemperatureComing(_pawn.Map);
                    var highest = HighestTemperatureComing(_pawn.Map);

                    // float minTemp = Mathf.Min(lowest - 5f, temp - 15f);
                    _pawnSave.TargetTemperatures =
                        new FloatRange(Mathf.Min(12, lowest - 10f), Mathf.Max(32, highest + 10f));

                    var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
                    if (_pawn.workSettings.WorkIsActive(cooking) && _pawn.workSettings.GetPriority(cooking) < 3)
                    {
                        _pawnSave.TargetTemperatures.min = Mathf.Min(_pawnSave.TargetTemperatures.min, -3);
                    }

                    _lastTempUpdate = Find.TickManager.TicksGame;
                }
            }

            // FloatRange RealComfyTemperatures = thisPawn.ComfortableTemperatureRange();
            var min = _pawn.def.statBases.GetStatValueFromList(StatDefOf.ComfyTemperatureMin,
                StatDefOf.ComfyTemperatureMin.defaultBaseValue);
            var max = _pawn.def.statBases.GetStatValueFromList(StatDefOf.ComfyTemperatureMax,
                StatDefOf.ComfyTemperatureMax.defaultBaseValue);

            if (Find.TickManager.TicksGame - _lastWeightUpdate <= JobGiver_OutfitterOptimizeApparel.ApparelStatCheck &&
                !forceweight)
            {
                return;
            }

            var weight = new FloatRange(1f, 1f);

            if (_pawnSave.TargetTemperatures.min < min)
            {
                weight.min += Math.Abs((_pawnSave.TargetTemperatures.min - min) / 10);
            }

            if (_pawnSave.TargetTemperatures.max > max)
            {
                weight.max += Math.Abs((_pawnSave.TargetTemperatures.max - max) / 10);
            }

            _pawnSave.Temperatureweight = weight;
            _lastWeightUpdate = Find.TickManager.TicksGame;
        }

        private static void FillInfusionHashset_PawnStatsHandlers(
            Apparel apparel,
            StatDef parentStat,
            ref HashSet<StatDef> infusedOffsets)
        {
            ApparelScoreRawFillInfusedStat?.Invoke(apparel, parentStat, ref infusedOffsets);
        }

        private void GetStatsOfApparel(
            [NotNull] Apparel ap,
            ref HashSet<StatDef> equippedOffsets,
            ref HashSet<StatDef> statBases)
        {
            if (ap.def.equippedStatOffsets != null)
            {
                foreach (var equippedStatOffset in ap.def.equippedStatOffsets)
                {
                    equippedOffsets.Add(equippedStatOffset.stat);
                }
            }

            if (ap.def.statBases == null)
            {
                return;
            }

            foreach (var statBase in ap.def.statBases)
            {
                statBases.Add(statBase.stat);
            }
        }

        private void GetStatsOfApparelInfused(Apparel ap, ref HashSet<StatDef> infusedOffsets)
        {
            foreach (var statPriority in _pawn.GetApparelStatCache().StatCache)
            {
                FillInfusionHashset_PawnStatsHandlers(ap, statPriority.Stat, ref infusedOffsets);
            }
        }

        private float GetTemperature(Twelfth twelfth, [NotNull] Map map)
        {
            return GenTemperature.AverageTemperatureAtTileForTwelfth(map.Tile, twelfth);
        }

        private float LowestTemperatureComing([NotNull] Map map)
        {
            var twelfth = GenLocalDate.Twelfth(map);
            var a = GetTemperature(twelfth, map);
            for (var i = 0; i < 3; i++)
            {
                twelfth = twelfth.NextTwelfth();
                a = Mathf.Min(a, GetTemperature(twelfth, map));
            }

            return Mathf.Min(a, map.mapTemperature.OutdoorTemp);
        }

        private float HighestTemperatureComing([NotNull] Map map)
        {
            var twelfth = GenLocalDate.Twelfth(map);
            var a = GetTemperature(twelfth, map);
            for (var i = 0; i < 3; i++)
            {
                twelfth = twelfth.NextTwelfth();
                a = Mathf.Max(a, GetTemperature(twelfth, map));
            }

            return Mathf.Max(a, map.mapTemperature.OutdoorTemp);
        }

        // ReSharper disable once CollectionNeverUpdated.Global
    }
}