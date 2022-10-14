// Copyright Karel Kroeze, 2020-2021.
// MedicalInfo/MedicalInfo/PawnColumnWorker_SelfTend.cs

using System;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Fluffy {
    public class PawnColumnWorker_SelfTend: PawnColumnWorker_Checkbox, IOptionalColumn {
        #region Fields

        // todo; override the horrible vanilla checkboxes
        private static MethodInfo _drawWorkBoxBackgroundMethodInfo;

        #endregion Fields

        #region Methods

        public static void DrawCellBackground(Rect cell, Pawn pawn) {
            if (_drawWorkBoxBackgroundMethodInfo == null) {
                _drawWorkBoxBackgroundMethodInfo = typeof(WidgetsWork).GetMethod("DrawWorkBoxBackground",
                    BindingFlags.Static |
                    BindingFlags.NonPublic);
            }

            if (_drawWorkBoxBackgroundMethodInfo == null) {
                throw new NullReferenceException("WidgetsWork.DrawWorkBoxBackground not found");
            }

            _drawWorkBoxBackgroundMethodInfo.Invoke(null, new object[] { cell, pawn, WorkTypeDefOf.Doctor });
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table) {
            TooltipHandler.TipRegion(rect, GetTip(pawn));
            if (!HasCheckbox(pawn)) {
                return;
            }

            Rect cell = new Rect(0f, 0f, Constants.IconSize, Constants.IconSize)
                      .CenteredOnXIn(rect)
                      .CenteredOnYIn(rect);
            DrawCellBackground(cell, pawn);

            if (pawn.playerSettings.selfTend) {
                GUI.DrawTexture(cell, WidgetsWork.WorkBoxCheckTex);
            }

            if (Widgets.ButtonInvisible(rect)) {
                SetValue(pawn, !GetValue(pawn), table);
                if (GetValue(pawn)) {
                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                } else {
                    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                }
            }
        }

        public override void DoHeader(Rect rect, PawnTable table) {
            def.headerIconSize = new Vector2(Constants.HeaderIconSize, Constants.HeaderIconSize);
            base.DoHeader(rect, table);
        }

        public bool ShowFor(SourceType source) {
            if (source == SourceType.Colonists) {
                return true;
            }

            return false;
        }

        protected override string GetTip(Pawn pawn) {
            if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor)) {
                return
                    "MedicalTab.PawnIsIncapableOfX".Translate(pawn.LabelShort, WorkTypeDefOf.Doctor.gerundLabel)
                                                   .CapitalizeFirst();
            }

            if (!pawn.workSettings.WorkIsActive(WorkTypeDefOf.Doctor)) {
                return
                    "MedicalTab.PawnIsNotAX".Translate(pawn.LabelShort, WorkTypeDefOf.Doctor.pawnLabel)
                                            .CapitalizeFirst();
            }

            return
                "MedicalTab.ToggleSelfTend".Translate(GetValue(pawn) ? "Off".Translate() : "On".Translate())
                                           .CapitalizeFirst();
        }

        protected override bool GetValue(Pawn pawn) {
            return pawn.playerSettings.selfTend;
        }

        protected override bool HasCheckbox(Pawn pawn) {
            return !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor) &&
                   pawn.workSettings.WorkIsActive(WorkTypeDefOf.Doctor);
        }

        protected override void SetValue(Pawn pawn, bool value, PawnTable table) {
            pawn.playerSettings.selfTend = value;
        }

        #endregion Methods
    }
}
