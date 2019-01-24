// Karel Kroeze
// PawnColumnWorker_MedicalCare.cs
// 2017-05-14

using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy
{
    public class PawnColumnWorker_MedicalCare : PawnColumnWorker, OptionalColumn
    {
        // todo; try to get rid of the EventType == Layout bail for DoColumn() to see if that improves responsiveness

        #region Properties

        public MedicalCareCategory OverallCare
        {
            get
            {
                return MainTabWindow_Medical.Instance.Table.PawnsListForReading.Max(p => p.playerSettings.medCare);
            }
            set
            {
                MainTabWindow_Medical.Instance.Table.PawnsListForReading.ForEach(p => p.playerSettings.medCare = value);
            }
        }

        #endregion Properties

        #region Methods

        public override int Compare(Pawn a, Pawn b)
        {
            return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if ( pawn?.playerSettings?.medCare != null )
                MedicalCareUtility.MedicalCareSetter(rect, ref pawn.playerSettings.medCare );
        }

        public void DoDefaultMedCareHeader(Rect rect)
        {
            switch (MainTabWindow_Medical.Instance.Source)
            {
                case SourceType.Animals:
                    MedicalCareUtility.MedicalCareSetter(rect, ref Find.PlaySettings.defaultCareForColonyAnimal);
                    break;

                case SourceType.Colonists:
                    MedicalCareUtility.MedicalCareSetter(rect, ref Find.PlaySettings.defaultCareForColonyHumanlike);
                    break;

                case SourceType.Hostiles:
                    MedicalCareUtility.MedicalCareSetter(rect, ref Find.PlaySettings.defaultCareForHostileFaction);
                    break;

                case SourceType.Prisoners:
                    MedicalCareUtility.MedicalCareSetter(rect, ref Find.PlaySettings.defaultCareForColonyPrisoner);
                    break;

                case SourceType.Visitors:
                    MedicalCareUtility.MedicalCareSetter(rect, ref Find.PlaySettings.defaultCareForNeutralFaction);
                    break;
            }
        }

        public override void DoHeader(Rect rect, PawnTable table)
        {
            // decrease height of rect (base does this already, but MedCareSetter does not.
            rect.yMin = rect.yMax - Constants.DesiredHeaderHeight;

            if (Event.current.shift && Mouse.IsOver(rect) && table.PawnsListForReading.Any())
            {
                // mass assign
                // note; weird as fuck sentinel/token approach, because somehow an intercepted click event and the medCareSetter do not fire in the same GUI phase?
                MedicalCareCategory sentinel = OverallCare;
                MedicalCareCategory token = table.PawnsListForReading.Max(p => p.playerSettings.medCare);
                MedicalCareUtility.MedicalCareSetter(rect, ref token);
                if (sentinel != token)
                    OverallCare = token;

                TooltipHandler.TipRegion(rect, GetHeaderTip(table));
            }
            else if (Event.current.control && Mouse.IsOver(rect))
            {
                // defaults
                DoDefaultMedCareHeader(rect);
                TooltipHandler.TipRegion(rect, GetHeaderTip(table));
            }
            else
            {
                // text
                base.DoHeader(rect, table);
            }
        }

        public override int GetMinWidth(PawnTable table) => Constants.MedicalCareSetterWidth;

        public bool ShowFor(SourceType source)
        {
            switch (source)
            {
                case SourceType.Hostiles:
                    return false;

                default:
                    return true;
            }
        }

        internal int GetValueToCompare(Pawn pawn)
        {
            return (int)pawn.playerSettings.medCare;
        }

        protected override string GetHeaderTip(PawnTable table)
        {
            string tip = base.GetHeaderTip(table);
            tip += "\n\n";

            if (table.PawnsListForReading.Any())
            {
                tip += "MedicalTab.XClickToY".Translate("MedicalTab.Shift".Translate(),
                                                         "MedicalTab.MassAssignMedicalCare".Translate())
                                             .CapitalizeFirst();
                tip += "\n";
            }
            tip += "MedicalTab.XClickToY".Translate("MedicalTab.Ctrl".Translate(),
                                                     "MedicalTab.SetDefaultMedicalCare".Translate()).CapitalizeFirst();
            return tip;
        }

        #endregion Methods
    }
}
