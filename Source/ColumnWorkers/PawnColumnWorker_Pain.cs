// Karel Kroeze
// PawnColumnWorker_Pain.cs
// 2017-05-15

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static Fluffy.CapacityUtility;

namespace Fluffy {
    public class PawnColumnWorker_Pain: PawnColumnWorker {
        #region Methods

        public override int Compare(Pawn a, Pawn b) {
            return GetValueToCompareTo(a).CompareTo(GetValueToCompareTo(b));
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table) {
            Pair<string, Color> painLabel = HealthCardUtility.GetPainLabel(pawn);
            string painTip = HealthCardUtility.GetPainTip(pawn);

            painTip += "\n\n";
            painTip += "MedicalTab.XClickToY".Translate("", "MedicalTab.ShowSurgeryOptionsThat".Translate(
                                                                                                           "MedicalTab.Reduce"
                                                                                                               .Translate
                                                                                                               (),
                                                                                                           "Pain"
                                                                                                               .Translate
                                                                                                               ()))
                                             .Trim().CapitalizeFirst();

            // draw label (centered)
            GUI.color = painLabel.Second;
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(rect, pawn.health.hediffSet.PainTotal.ToStringPercent());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // tooltip
            TooltipHandler.TipRegion(rect, painTip);

            // click
            Widgets.DrawHighlightIfMouseover(rect);
            if (Widgets.ButtonInvisible(rect)) {
                IEnumerable<RecipeDef> recipes = pawn.def.AllRecipes
                                                     .Where(
                                                            r => r.AvailableNow &&
                                                                 (r.AddsHediffThatReducesPain() ||
                                                                   ( Settings.SuggestDrugs && r.AdministersDrugThatReducesPain() ) ) &&
                                                                 NotMissingVitalIngredient(pawn, r)
                                                           );
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (RecipeDef recipe in recipes) {
                    options.Add(GenerateSurgeryOption(pawn, pawn, recipe,
                                                        recipe.PotentiallyMissingIngredients(null, pawn.Map)));
                }

                if (options.Count == 0) {
                    options.Add(new FloatMenuOption("None".Translate(), null));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        public override int GetMinWidth(PawnTable table) {
            return Mathf.Max(base.GetMinWidth(table), Constants.StatColumnMinWidth);
        }

        public float GetValueToCompareTo(Pawn pawn) {
            return pawn.health.hediffSet.PainTotal;
        }

        #endregion Methods
    }
}
