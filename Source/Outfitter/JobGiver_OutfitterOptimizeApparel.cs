using System.Linq;
using System.Text;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Outfitter
{
    public static class JobGiver_OutfitterOptimizeApparel
    {
        public const int ApparelStatCheck = 3750;

        // private const int ApparelOptimizeCheckIntervalMin = 9000;
        // private const int ApparelOptimizeCheckIntervalMax = 12000;
        private const float MinScoreGainToCare = 0.09f;

        private const int ApparelOptimizeCheckIntervalMax = 9000;
        private const int ApparelOptimizeCheckIntervalMin = 6000;

        // private const float MinScoreGainToCare = 0.15f;
        private static StringBuilder _debugSb;

        // private static Apparel lastItem;
        private static void SetNextOptimizeTick([NotNull] Pawn pawn)
        {
            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame
                                                     + Random.Range(
                                                         ApparelOptimizeCheckIntervalMin,
                                                         ApparelOptimizeCheckIntervalMax);

            pawn.GetApparelStatCache().RawScoreDict.Clear();

            // pawn.GetApparelStatCache().recentApparel.Clear();
        }

        private static bool CanWearApparel(Pawn pawn, Apparel apparel)
        {
            if (!EquipmentUtility.IsBondedTo(apparel, pawn))
            {
                return false;
            }

            if (pawn.apparel.WouldReplaceLockedApparel(apparel))
            {
                return false;
            }

            if (apparel.IsForbidden(pawn))
            {
                return false;
            }

            return true;
        }

        // private static NeededWarmth neededWarmth;
        // ReSharper disable once InconsistentNaming
        public static bool TryGiveJob_Prefix([CanBeNull] ref Job __result, Pawn pawn)
        {
            __result = null;
            if (pawn.outfits == null)
            {
                Log.ErrorOnce(
                    pawn + " tried to run JobGiver_OutfitterOptimizeApparel without an OutfitTracker",
                    5643897);
                return false;
            }

            if (pawn.Faction != Faction.OfPlayer)
            {
                Log.ErrorOnce("Non-colonist " + pawn + " tried to optimize apparel.", 764323);
                return false;
            }

            if (!DebugViewSettings.debugApparelOptimize)
            {
                if (Find.TickManager.TicksGame < pawn.mindState.nextApparelOptimizeTick)
                {
                    return false;
                }
            }
            else
            {
                _debugSb = new StringBuilder();
                _debugSb.AppendLine(string.Concat("Outfiter scanning for ", pawn, " at ", pawn.Position));
            }

            var currentOutfit = pawn.outfits.CurrentOutfit;
            var wornApparel = pawn.apparel.WornApparel;

            foreach (var ap in wornApparel)
            {
                var conf = pawn.GetApparelStatCache();

                var notAllowed = !currentOutfit.filter.Allows(ap)
                                 && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(ap);

                var shouldDrop = conf.ApparelScoreRaw(ap) < 0f
                                 && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(ap);

                var someoneWantsIt = pawn.GetApparelStatCache().ToDropList.ContainsKey(ap);

                if (!notAllowed && !shouldDrop && !someoneWantsIt)
                {
                    continue;
                }

                __result = new Job(JobDefOf.RemoveApparel, ap) {haulDroppedApparel = true};
                if (!someoneWantsIt)
                {
                    return false;
                }

                pawn.GetApparelStatCache().ToDropList[ap].mindState.nextApparelOptimizeTick = -5000;
                pawn.GetApparelStatCache().ToDropList[ap].mindState.Notify_OutfitChanged();
                pawn.GetApparelStatCache().ToDropList.Remove(ap);

                return false;
            }

            Thing thing = null;
            var score = 0f;
            var list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);

            if (list.Count == 0)
            {
                SetNextOptimizeTick(pawn);
                return false;
            }

            foreach (var t in list)
            {
                var apparel = (Apparel) t;

                // Not allowed
                if (!currentOutfit.filter.Allows(apparel))
                {
                    continue;
                }

                // Not in store
                if (apparel.Map.haulDestinationManager.SlotGroupAt(apparel.Position) == null)
                {
                    continue;
                }

                if (!CanWearApparel(pawn, apparel))
                {
                    continue;
                }

                var gain = pawn.ApparelScoreGain(apparel);

                // this blocks pawns constantly switching between the recent apparel, due to shifting calculations
                // not very elegant but working
                // if (pawn.GetApparelStatCache().recentApparel.Contains(apparel))
                // {
                // gain *= 0.01f;
                // }
                if (DebugViewSettings.debugApparelOptimize)
                {
                    _debugSb.AppendLine(apparel.LabelCap + ": " + gain.ToString("F2"));
                }

                //  float otherGain = 0f;
                //  Pawn otherPawn = null;
                //  foreach (Pawn otherP in pawn.Map.mapPawns.FreeColonistsSpawned.ToList())
                //  {
                //      if (otherP == pawn)
                //      {
                //          continue;
                //      }
                //      if (otherP.ApparelScoreGain(apparel) >= MinScoreGainToCare)
                //      {
                //          if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
                //          {
                //              if (pawn.CanReserveAndReach(apparel, PathEndMode.OnCell, pawn.NormalMaxDanger(), 1))
                //              {
                //                  thing = apparel;
                //                  score = gain;
                //              }
                //          }
                //          otherPawn = otherP;
                //          otherGain = Mathf.Max(otherGain, otherP.ApparelScoreGain(apparel));
                //      }
                //  }

                if (!(gain >= MinScoreGainToCare) || !(gain >= score))
                {
                    continue;
                }

                if (!ApparelUtility.HasPartsToWear(pawn, apparel.def))
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(apparel, PathEndMode.OnCell, pawn.NormalMaxDanger()))
                {
                    continue;
                }

                thing = apparel;
                score = gain;
            }

            if (DebugViewSettings.debugApparelOptimize)
            {
                _debugSb.AppendLine("BEST: " + thing);
                //Log.Message(_debugSb.ToString());
                _debugSb = null;
            }

            // New stuff
            if (false)
            {
                var list2 =
                    pawn.Map.mapPawns.FreeColonistsSpawned.Where(x => x.IsColonistPlayerControlled);
                foreach (var ap in wornApparel)
                {
                    foreach (var otherPawn in list2)
                    {
                        foreach (var otherAp in otherPawn.apparel.WornApparel.Where(
                            x => !ApparelUtility.CanWearTogether(ap.def, x.def, pawn.RaceProps.body)))
                        {
                            var gain = pawn.ApparelScoreGain(otherAp);
                            var otherGain = otherPawn.ApparelScoreGain(ap);
                            if (!(gain > MinScoreGainToCare) || !(gain >= score) || !(otherGain > MinScoreGainToCare))
                            {
                                continue;
                            }

                            score = gain;
                            if (Prefs.DevMode)
                            {
                                Log.Message(
                                    "OUTFITTER: " + pawn + " wants " + otherAp + " currently worn by " + otherPawn
                                    + ", scores: " + gain + " - " + otherGain + " - " + score);
                            }

                            if (otherPawn.GetApparelStatCache().ToDropList.ContainsKey(ap))
                            {
                                continue;
                            }

                            otherPawn.GetApparelStatCache().ToDropList.Add(otherAp, otherPawn);
                            otherPawn.mindState.nextApparelOptimizeTick = -5000;
                            otherPawn.mindState.Notify_OutfitChanged();
                        }
                    }
                }
            }

            if (thing == null)
            {
                SetNextOptimizeTick(pawn);
                return false;
            }

            // foreach (Apparel apparel in wornApparel)
            // {
            // pawn.GetApparelStatCache().recentApparel.Add(apparel);
            // }
            __result = new Job(JobDefOf.Wear, thing);
            pawn.Reserve(thing, __result, 1, 1);
            return false;
        }
    }
}