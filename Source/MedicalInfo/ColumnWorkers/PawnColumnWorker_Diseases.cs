// Karel Kroeze
// PawnColumnWorker_Diseases.cs
// 2017-05-15

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy {
    public class PawnColumnWorker_Diseases: PawnColumnWorker {

        public override int Compare(Pawn a, Pawn b) {
            return GetValueToCompareTo(a).CompareTo(GetValueToCompareTo(b));
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table) {
            IEnumerable<Hediff> diseases = pawn.GetPotentiallyLethalHediffs();
            Rect diseaseRect = new Rect(rect.xMin - (Constants.IconSize / 2f),
                                        rect.yMin + ((rect.height - Constants.IconSize) / 2f),
                                        Constants.IconSize, Constants.IconSize);
            int n = diseases.Count();
            foreach (Hediff disease in diseases) {
                diseaseRect.x += Constants.StatColumnMinWidth / (n + 1);
                DrawDiseaseIndicator(diseaseRect, (CapacityUtility.DiseaseProgress) disease);
            }

            TooltipHandler.TipRegion(rect, () => GetTooltip(pawn, diseases), pawn.GetHashCode());
        }

        private string GetTooltip(Pawn pawn, IEnumerable<Hediff> diseases) {
            string tip = "";
            foreach (IGrouping<BodyPartRecord, Hediff> set in diseases.GroupBy(k => k.Part)) {
                tip += GetDiseaseTooltip(pawn, set, set.Key) + "\n\n";
            }

            return tip;
        }

        public override void DoHeader(Rect rect, PawnTable table) {
            def.headerIconSize = new Vector2(Constants.HeaderIconSize, Constants.HeaderIconSize);
            base.DoHeader(rect, table);
        }

        public void DrawDiseaseIndicator(Rect rect, CapacityUtility.DiseaseProgress disease) {
            // draw immunity
            if (disease.immunity > 0) {
                Rect immunityRect = rect.ContractedBy(Mathf.Lerp(rect.width / 2f, 0f, disease.immunity));
                GUI.color = new Color(1f, 1f, 1f, Mathf.Lerp(.5f, 1f, disease.immunity));
                GUI.DrawTexture(immunityRect, Resources.Circle);
            }

            // draw disease progress
            Rect diseaseProgressRect = rect.ContractedBy(Mathf.Lerp(rect.width / 2f, 0f, disease.severity));
            GUI.color = disease.color;
            GUI.DrawTexture(diseaseProgressRect, Resources.Circle);
            GUI.color = Color.white;

            // draw indicator
            GUI.color = disease.tended ? Color.white : Color.gray;
            GUI.DrawTexture(rect, Resources.DashCircle);
        }

        public string GetDiseaseTooltip(Pawn pawn, IEnumerable<Hediff> diffs, BodyPartRecord part) {
            IEnumerable<string> tips = diffs.Select(diff => diff.GetTooltip(pawn, false));
            if (part != null) {
                return $"{part.LabelCap}:\n{string.Join("\n", tips)}";
            } else {
                return string.Join("\n", tips);
            }
        }

        public override int GetMinWidth(PawnTable table) {
            return Constants.StatColumnMinWidth;
        }

        public float GetValueToCompareTo(Pawn pawn) {
            List<CapacityUtility.DiseaseProgress> diseases = pawn.GetDiseases()
                               .Select(d => (CapacityUtility.DiseaseProgress)d)
                               .ToList();
            if (!diseases.Any()) {
                return -1;
            }

            return diseases.Max(d => d.severity - d.immunity);
        }
    }
}
