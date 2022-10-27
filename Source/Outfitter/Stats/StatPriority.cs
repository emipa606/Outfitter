// Outfitter/StatCache.cs
//
// Copyright Karel Kroeze, 2016.
//
// Created 2016-01-02 13:58

using System.Collections.Generic;
using JetBrains.Annotations;
using Outfitter.Enums;
using RimWorld;
using Verse;

namespace Outfitter;

public class StatPriority
{
    public StatPriority(StatDef stat, float priority, StatAssignment assignment = StatAssignment.Automatic)
    {
        Stat = stat;
        Weight = priority;
        Assignment = assignment;
    }

    public StatPriority(
        KeyValuePair<StatDef, float> statDefWeightPair,
        StatAssignment assignment = StatAssignment.Automatic)
    {
        Stat = statDefWeightPair.Key;
        Weight = statDefWeightPair.Value;
        Assignment = assignment;
    }

    public StatAssignment Assignment { get; set; }

    public StatDef Stat { get; }

    public float Weight { get; set; }

    public void Delete([NotNull] Pawn pawn)
    {
        pawn.GetApparelStatCache().Cache.Remove(this);

        pawn.GetSaveablePawn().Stats.RemoveAll(i => i.Stat == Stat);
    }

    public void Reset(Pawn pawn)
    {
        var stats = pawn.GetWeightedApparelStats();
        var indiStats = pawn.GetWeightedApparelIndividualStats();

        if (stats.ContainsKey(Stat))
        {
            Weight = stats[Stat];
            Assignment = StatAssignment.Automatic;
        }

        if (indiStats.ContainsKey(Stat))
        {
            Weight = indiStats[Stat];
            Assignment = StatAssignment.Individual;
        }

        var pawnSave = pawn.GetSaveablePawn();
        pawnSave.Stats.RemoveAll(i => i.Stat == Stat);
    }
}

// ReSharper disable once CollectionNeverUpdated.Global