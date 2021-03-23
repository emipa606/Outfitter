using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace Outfitter
{
    internal class Controller : Mod
    {
        public static Settings Settings;

        public Controller(ModContentPack content)
            : base(content)
        {
            Settings = GetSettings<Settings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }

        [NotNull]
        public override string SettingsCategory()
        {
            return "Outfitter";
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            Settings.Write();

            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            foreach (var bodyDef in DefDatabase<BodyDef>.AllDefsListForReading)
            {
                if (bodyDef.defName != "Human")
                {
                    continue;
                }

                var neck = bodyDef.corePart.parts.FirstOrDefault(x => x.def == BodyPartDefOf.Neck);
                var head = neck?.parts.FirstOrDefault(x => x.def == BodyPartDefOf.Head);
                if (head == null)
                {
                    continue;
                }

                //    if (!head.groups.Contains(BodyPartGroupDefOf.Eyes))
                {
                    //     head.groups.Add(BodyPartGroupDefOf.Eyes);
                    //BodyPartRecord leftEye = head.parts.FirstOrDefault(x => x.def == BodyPartDefOf.LeftEye);
                    //BodyPartRecord rightEye = head.parts.FirstOrDefault(x => x.def == BodyPartDefOf.RightEye);
                    var jaw = head.parts.FirstOrDefault(x => x.def == BodyPartDefOf.Jaw);

                    if (Settings.UseEyes)
                    {
                        //leftEye?.groups.Remove(BodyPartGroupDefOf.FullHead);
                        //rightEye?.groups.Remove(BodyPartGroupDefOf.FullHead);
                        jaw?.groups.Remove(BodyPartGroupDefOf.FullHead);
                        //Log.Message("Outfitter removed FullHead from Human eyes.");
                    }
                    else
                    {
                        /*if (leftEye != null && !leftEye.groups.Contains(BodyPartGroupDefOf.FullHead))
                            {
                                leftEye?.groups.Add(BodyPartGroupDefOf.FullHead);
                            }
                            if (rightEye != null && !rightEye.groups.Contains(BodyPartGroupDefOf.FullHead))
                            {
                                rightEye?.groups.Add(BodyPartGroupDefOf.FullHead);
                            }*/
                        if (jaw != null && !jaw.groups.Contains(BodyPartGroupDefOf.FullHead))
                        {
                            jaw.groups.Add(BodyPartGroupDefOf.FullHead);
                        }

                        //Log.Message("Outfitter re-added FullHead to Human eyes.");
                    }

                    break;
                }
            }
        }
    }
}