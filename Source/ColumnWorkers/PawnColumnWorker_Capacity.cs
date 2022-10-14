// Karel Kroeze
// PawnColumnWorker_Capacity.cs
// 2017-05-16

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static Fluffy.CapacityUtility;
using static Fluffy.Constants;

namespace Fluffy {
    public class PawnColumnWorker_Capacity: PawnColumnWorker {
        #region Fields

        private Vector2 cachedLabelSize = Vector2.zero;

        #endregion Fields

        #region Properties

        public PawnCapacityDef Capacity => (def as PawnColumnDef_Capacity).capacity;
        public bool MoveDown => (def as PawnColumnDef_Capacity).moveLabelDown;

        #endregion Properties

        #region Methods

        public override int Compare(Pawn a, Pawn b) {
            return Efficiency(a).CompareTo(Efficiency(b));
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table) {
            // get values
            float level = Efficiency(pawn);
            string label = level.ToStringPercent();
            Color color = HealthCardUtility.GetEfficiencyLabel(pawn, Capacity).Second;
            string tip = HealthCardUtility.GetPawnCapacityTip(pawn, Capacity);


            // draw label
            GUI.color = color;
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // pending bills
            DoPendingBillsDrawExtra(rect, pawn);
            if (Mouse.IsOver(rect)) {
                // getting that tooltip can be quite expensive, let's not do it until we have to.
                tip += GetPendingBillsTip(pawn);
            }

            // tooltip
            Widgets.DrawHighlightIfMouseover(rect);
            tip += GetInteractionTip(pawn);
            TooltipHandler.TipRegion(rect, tip);

            // done for hostile pawns
            if (MainTabWindow_Medical.Instance.Source == SourceType.Hostiles) {
                return;
            }

            // click
            DoInteractions(rect, pawn, level);
        }

        private void DoInteractions(Rect rect, Pawn pawn, float level) {
            if (Widgets.ButtonInvisible(rect)) {
                bool negative = Event.current.button == 1;
                IEnumerable<RecipeDef> recipes = pawn.def.AllRecipes
                                                     .Where(
                                                            r => r.AvailableNow &&
                                                                 ( r.AddsHediffThatAffects( Capacity, level, negative ) ||
                                                                   ( Settings.SuggestDrugs && r.AdministersDrugThatAffects( Capacity, level, negative ) ) ) &&
                                                                 NotMissingVitalIngredient( pawn, r )
                                                           );
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (RecipeDef recipe in recipes) {
                    options.Add(GenerateSurgeryOption(pawn, pawn, recipe,
                                                        recipe.PotentiallyMissingIngredients(null, pawn.Map)));
                }

                foreach (RecipeDef recipe in pawn.def.AllRecipes) {
                    options.AddRange(recipe.AddedPartOptionsThatAffect(Capacity, pawn, negative));
                }

                if (options.Count == 0) {
                    options.Add(new FloatMenuOption("None".Translate(), null));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private string GetInteractionTip(Pawn pawn) {
            string tip = "\n";
            tip += "MedicalTab.XClickToY".Translate("MedicalTab.Left".Translate(),
                                                     "MedicalTab.ShowSurgeryOptionsThat".Translate(
                                                                                                   "MedicalTab.Increase"
                                                                                                       .Translate(),
                                                                                                   Capacity.GetLabelFor(
                                                                                                                        pawn)))
                                         .CapitalizeFirst();

            tip += "\n";
            tip += "MedicalTab.XClickToY".Translate("MedicalTab.Right".Translate(),
                                                     "MedicalTab.ShowSurgeryOptionsThat".Translate(
                                                                                                   "MedicalTab.Reduce"
                                                                                                       .Translate(),
                                                                                                   Capacity.GetLabelFor(
                                                                                                                        pawn)))
                                         .CapitalizeFirst();
            return tip;
        }

        public override void DoHeader(Rect rect, PawnTable table) {
            // todo; cache labelRect instead of size?
            Rect labelRect = GetHeaderLabelRect(rect);
            base.DoHeader(labelRect, table);

            // vertical line
            if (!MoveDown) {
                Vector2 lineStart = new Vector2(Mathf.FloorToInt(rect.center.x), labelRect.yMax);
                // note that two 1px lines give a much crisper line than one 2px line. Obv.
                GUI.color = new Color(1f, 1f, 1f, .3f);
                Widgets.DrawLineVertical(lineStart.x, lineStart.y, 20f);
                Widgets.DrawLineVertical(lineStart.x + 1, lineStart.y, 20f);
                GUI.color = Color.white;
            }
        }

        public float Efficiency(Pawn pawn) {
            return pawn.health.capacities.GetLevel(Capacity);
        }

        public Rect GetHeaderLabelRect(Rect rect) {
            if (cachedLabelSize == Vector2.zero) {
                cachedLabelSize = Text.CalcSize(Capacity.LabelCap);
            }

            float x = rect.center.x;
            Rect result = new Rect(x - ((cachedLabelSize.x + ExtraHeaderLabelWidth) / 2f), rect.y,
                                   cachedLabelSize.x + ExtraHeaderLabelWidth,
                                   HeaderHeight - AlternatingHeaderLabelOffset);
            if (MoveDown) {
                result.y += AlternatingHeaderLabelOffset;
            }

            return result;
        }

        public override int GetMinHeaderHeight(PawnTable table) {
            return HeaderHeight;
        }

        public override int GetMinWidth(PawnTable table) {
            return StatColumnMinWidth;
        }

        private void DoPendingBillsDrawExtra(Rect rect, Pawn pawn) {
            if (pawn.BillStack.Bills.Any(b => (b as Bill_Medical).Affects(Capacity))) {
                Rect iconRect = new Rect(rect.xMax - StatusIconSize - 3f, rect.yMax - StatusIconSize - 3f,
                                         StatusIconSize,
                                         StatusIconSize);
                GUI.color = new Color(1f, 1f, 1f, .3f);
                GUI.DrawTexture(iconRect, Resources.StatusIconProcedure);
                GUI.color = Color.white;
            }
        }

        private string GetPendingBillsTip(Pawn pawn) {
            string tip = "";

            IEnumerable<Bill_Medical> bills = pawn.BillStack.Bills
                                          .OfType<Bill_Medical>()
                                          .Where( b => b.Affects( Capacity ) );

#if DEBUG
            Log.Message(pawn.LabelCap + " :: " + pawn.BillStack.Bills.Count + " :: " + bills.Count() + " :: " + string.Join(", ", pawn.BillStack.Bills.Select(b => b.recipe.defName).ToArray()));
#endif

            if (bills.Any()) {
                tip += "\n";
                tip += "MedicalTab.ScheduledProcedures".Translate();
                tip += "\n";

                foreach (Bill_Medical bill in bills) {
                    tip += bill.LabelCap.Indented() + "\n";
                }
            }

            return tip;
        }

        #endregion Methods
    }
}
