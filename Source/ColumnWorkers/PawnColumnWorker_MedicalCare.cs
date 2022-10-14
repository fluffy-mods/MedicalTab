// Karel Kroeze
// PawnColumnWorker_MedicalCare.cs
// 2017-05-14

using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy {
    public class PawnColumnWorker_MedicalCare: PawnColumnWorker, IOptionalColumn {
        public MedicalCareCategory OverallCare {
            get => MainTabWindow_Medical.Instance?.Table?.PawnsListForReading?.Max(p => p.playerSettings?.medCare) ?? MedicalCareCategory.Best;
            set {
                foreach (Pawn pawn in MainTabWindow_Medical.Instance.Table.PawnsListForReading) {
                    if (pawn?.playerSettings?.medCare != null) {
                        pawn.playerSettings.medCare = value;
                    }
                }
            }
        }

        public bool ShowFor(SourceType source) {
            switch (source) {
                case SourceType.Hostiles:
                    return false;
                case SourceType.Colonists:
                case SourceType.Animals:
                case SourceType.Prisoners:
                case SourceType.Visitors:
                default:
                    return true;
            }
        }

        public override int Compare(Pawn a, Pawn b) {
            return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table) {
            if (pawn?.playerSettings?.medCare != null) {
                MedicalCareUtility.MedicalCareSetter(rect, ref pawn.playerSettings.medCare);
            }
        }

        public void DoDefaultMedCareHeader(Rect rect, PawnTable table) {
            switch (MainTabWindow_Medical.Instance.Source) {
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
                default:
                    break;
            }
        }

        public override void DoHeader(Rect rect, PawnTable table) {
            // decrease height of rect (base does this already, but MedCareSetter does not.
            rect.yMin = rect.yMax - Constants.DesiredHeaderHeight;

            if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Mouse.IsOver(rect) && table.PawnsListForReading.Any()) {
                MedicalCareCategory current    = table.PawnsListForReading.Max( p => p.playerSettings.medCare );
                MedicalCareUtility.MedicalCareSetter(rect, ref current);
                if (OverallCare != current) {
                    OverallCare = current;
                }

                TooltipHandler.TipRegion(rect, GetHeaderTip(table));
            } else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) && Mouse.IsOver(rect)) {
                // defaults
                DoDefaultMedCareHeader(rect, table);
                TooltipHandler.TipRegion(rect, GetHeaderTip(table));
            } else {
                // text
                base.DoHeader(rect, table);
            }
        }

        public override int GetMinWidth(PawnTable table) {
            return Constants.MedicalCareSetterWidth;
        }

        internal int GetValueToCompare(Pawn pawn) {
            return (int) pawn.playerSettings.medCare;
        }

        protected override string GetHeaderTip(PawnTable table) {
            string tip = base.GetHeaderTip( table );
            tip += "\n\n";

            if (table.PawnsListForReading.Any()) {
                tip += "MedicalTab.XClickToY".Translate("MedicalTab.Shift".Translate(),
                                                         "MedicalTab.MassAssignMedicalCare".Translate())
                                             .CapitalizeFirst();
                tip += "\n";
            }

            tip += "MedicalTab.XClickToY".Translate("MedicalTab.Ctrl".Translate(),
                                                     "MedicalTab.SetDefaultMedicalCare".Translate()).CapitalizeFirst();
            return tip;
        }

        // todo; try to get rid of the EventType == Layout bail for DoColumn() to see if that improves responsiveness
    }
}
