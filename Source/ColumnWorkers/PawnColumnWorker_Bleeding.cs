// Karel Kroeze
// PawnColumnWorker_Bleeding.cs
// 2017-05-14

using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy {
    public class PawnColumnWorker_Bleeding: PawnColumnWorker_Icon {
        #region Methods

        public override int Compare(Pawn a, Pawn b) {
            return ValueToCompareTo(a).CompareTo(ValueToCompareTo(b));
        }

        public override void DoHeader(Rect rect, PawnTable table) {
            def.headerIconSize = new Vector2(Constants.HeaderIconSize, Constants.HeaderIconSize);
            base.DoHeader(rect, table);
        }

        public float ValueToCompareTo(Pawn pawn) {
            return pawn.health.hediffSet.BleedRateTotal;
        }

        protected override Texture2D GetIconFor(Pawn pawn) {
            if (pawn.health.hediffSet.BleedRateTotal > .01f) {
                return Resources.BleedingIcon;
            }

            return null;
        }

        protected override Vector2 GetIconSize(Pawn pawn) {
            return Mathf.Lerp(.3f, 1, Mathf.Min(pawn.health.hediffSet.BleedRateTotal / Constants.BleedingMax, 1f)) *
                   new Vector2(Constants.IconSize, Constants.IconSize);
        }

        protected override string GetIconTip(Pawn pawn) {
            string text = pawn.health.hediffSet.BleedRateTotal.ToStringPercent() + "/" + "LetterDay".Translate();
            int ticksToDeath = HealthUtility.TicksUntilDeathDueToBloodLoss(pawn);
            if (ticksToDeath < 60000) {
                text += " (" + "TimeToDeath".Translate(ticksToDeath.ToStringTicksToPeriod()) + ")";
            } else {
                text = text + " (" + "WontBleedOutSoon".Translate() + ")";
            }

            return text;
        }

        #endregion Methods
    }
}
