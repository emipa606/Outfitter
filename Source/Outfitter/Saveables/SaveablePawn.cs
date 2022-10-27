using System.Collections.Generic;
using Outfitter.Enums;
using RimWorld;
using Verse;

namespace Outfitter;

public class SaveablePawn : IExposable
{
    private bool _addIndividualStats = true;
    private bool _addPersonalStats = true;

    private bool _addWorkStats = true;
    private List<Saveable_Pawn_StatDef> ApparelStats = new List<Saveable_Pawn_StatDef>();

    public bool ArmorOnly;

    public bool AutoEquipWeapon;

    public MainJob MainJob;

    // Exposed members
    public Pawn Pawn;

    public List<Saveable_Pawn_StatDef> Stats = new List<Saveable_Pawn_StatDef>();

    public FloatRange TargetTemperatures;

    public bool TargetTemperaturesOverride;

    public FloatRange Temperatureweight;

    public List<Apparel> ToDrop = new List<Apparel>();

    public List<Apparel> ToWear = new List<Apparel>();

    // public FloatRange RealComfyTemperatures;

    public bool ForceStatUpdate { get; set; }

    public bool AddIndividualStats
    {
        get => _addIndividualStats;
        set => _addIndividualStats = value;
    }

    public bool AddPersonalStats
    {
        get => _addPersonalStats;
        set => _addPersonalStats = value;
    }

    public bool AddWorkStats
    {
        get => _addWorkStats;

        set => _addWorkStats = value;
    }

    // public SaveablePawn(Pawn pawn)
    // {
    // Pawn = pawn;
    // Stats = new List<Saveable_Pawn_StatDef>();
    // _lastStatUpdate = -5000;
    // _lastTempUpdate = -5000;
    // }
    public void ExposeData()
    {
        Scribe_References.Look(ref Pawn, "Pawn");
        Scribe_Values.Look(ref TargetTemperaturesOverride, "targetTemperaturesOverride");
        Scribe_Values.Look(ref TargetTemperatures, "TargetTemperatures");

        // bug: stats are not saved
        Scribe_Collections.Look(ref Stats, "Stats", LookMode.Deep);

        // todo: rename with next big version
        Scribe_Collections.Look(ref ApparelStats, "WeaponStats", LookMode.Deep);
        Scribe_Values.Look(ref _addWorkStats, "AddWorkStats", true);
        Scribe_Values.Look(ref _addIndividualStats, "AddIndividualStats", true);
        Scribe_Values.Look(ref _addPersonalStats, "addPersonalStats", true);
        Scribe_Values.Look(ref MainJob, "mainJob");
    }
}