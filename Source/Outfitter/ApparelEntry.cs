// ReSharper disable StyleCop.SA1307

using System.Collections.Generic;
using RimWorld;

namespace Outfitter
{
    public class ApparelEntry
    {
        public HashSet<StatDef> EquippedOffsets;
        public HashSet<StatDef> InfusedOffsets;
        public HashSet<StatDef> StatBases;

        public ApparelEntry()
        {
            EquippedOffsets = new HashSet<StatDef>();
            InfusedOffsets = new HashSet<StatDef>();
            StatBases = new HashSet<StatDef>();
        }
    }
}