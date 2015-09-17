using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace Fluffy
{
    public class MainTabWindow_Medical : MainTabWindow_PawnList
    {
        private const float TopAreaHeight = 40f;

        protected const float LabelRowHeight = 50f;

        public override Vector2 RequestedTabSize
        {
            get
            {
                return new Vector2(1050f, 90f + (float)base.PawnsCount * 30f + 65f);
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            MainTabWindow_Work.Reinit();
        }

        public enum order {
            Name,
            Care,
            BleedRate,
            Operations,
            Efficiency
        }

        public enum sourceOptions
        {
            Colonists,
            Prisoners
        }

        public sourceOptions source = sourceOptions.Colonists;

        public order orderBy = order.Name;

        public PawnCapacityDef orderByCapDef = PawnCapacityDefOf.Consciousness;

        public bool asc = true;

        public static bool isDirty = false;

        protected override void BuildPawnList()
        {
            this.pawns.Clear();

            IEnumerable<Pawn> tempPawns = new List<Pawn>();

            switch (source)
            {
                case sourceOptions.Prisoners:
                    tempPawns = Find.ListerPawns.PrisonersOfColony;
                    break;
                case sourceOptions.Colonists:
                default:
                    tempPawns = Find.ListerPawns.FreeColonists;
                    break;
            }

            switch (orderBy)
            {
                case order.Care:
                    this.pawns = (from p in tempPawns
                                  orderby p.playerSettings.medCare ascending
                                  select p).ToList();
                    break;
                case order.BleedRate:
                    this.pawns = (from p in tempPawns
                                  orderby p.health.hediffSet.BleedingRate
                                  select p).ToList();
                    break;
                case order.Operations:
                    this.pawns = (from p in tempPawns
                                  orderby p.BillStack.Count
                                  select p).ToList();
                    break;
                case order.Efficiency:
                    this.pawns = (from p in tempPawns
                                  orderby p.health.capacities.GetEfficiency(orderByCapDef) descending
                                  select p).ToList();
                    break;
                case order.Name:
                default:
                    this.pawns = (from p in tempPawns
                                  orderby p.LabelCap ascending
                                  select p).ToList();
                    break;
            }

            if (!asc)
            {
                this.pawns.Reverse();
            }

            isDirty = false;
        }

        public List<PawnCapacityDef> capDefs = DefDatabase<PawnCapacityDef>.AllDefsListForReading.Where(x => x.showOnHumanlikes).ToList();

        public override void DoWindowContents(Rect rect)
        {
            base.DoWindowContents(rect);

            if (isDirty)
            {
                BuildPawnList();
            }
            Rect position = new Rect(0f, 0f, rect.width, 80f);
            GUI.BeginGroup(position);

            float x = 0f;
            Text.Font = GameFont.Small;

            // prisoner / colonist toggle
            Rect sourceButton = new Rect(0f, 0f, 200f, 35f);
            if (Widgets.TextButton(sourceButton, source.ToString()))
            {
                if (source == sourceOptions.Colonists)
                {
                    source = sourceOptions.Prisoners;
                } else
                {
                    source = sourceOptions.Colonists;
                }
                isDirty = true;
            }

            // name
            Rect nameLabel = new Rect(x, 50f, 175f, 30f);
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(nameLabel, "FluffyMedical.Name".Translate());
            if (Widgets.InvisibleButton(nameLabel))
            {
                if (orderBy == order.Name)
                {
                    asc = !asc;
                } else
                {
                    orderBy = order.Name;
                    asc = true;
                }
                isDirty = true;
            }
            TooltipHandler.TipRegion(nameLabel, "FluffyMedical.ClickToSortBy".Translate("FluffyMedical.Name".Translate()));
            Widgets.DrawHighlightIfMouseover(nameLabel);
            x += 175f;

            // care
            Rect careLabel = new Rect(x, 50f, 100f, 30f);
            Widgets.Label(careLabel, "FluffyMedical.Care".Translate());
            if (Widgets.InvisibleButton(careLabel))
            {
                if (Event.current.shift)
                {
                    Utility_Medical.medicalCareSetterAll(pawns);
                } else
                {
                    if (orderBy == order.Care)
                    {
                        asc = !asc;
                    }
                    else
                    {
                        orderBy = order.Care;
                        asc = true;
                    }
                    isDirty = true;
                }
            }
            TooltipHandler.TipRegion(careLabel, "FluffyMedical.ClickToSortBy".Translate("FluffyMedical.Care".Translate()) + "\n" +
                                                "FluffyMedical.ShiftClickTo".Translate("FluffyMedical.SetCare".Translate()));
            Widgets.DrawHighlightIfMouseover(careLabel);
            x += 100f;

            // bloodloss
            Rect bloodLabel = new Rect(x, 50f, 50f, 30f);
            Rect bloodIcon = new Rect(x + 17f, 60f, 16f, 16f);
            GUI.DrawTexture(bloodIcon, Utility_Medical.bloodTextureWhite);
            if (Widgets.InvisibleButton(bloodLabel))
            {
                if (orderBy == order.BleedRate)
                {
                    asc = !asc;
                }
                else
                {
                    orderBy = order.BleedRate;
                    asc = true;
                }
                isDirty = true;
            }
            TooltipHandler.TipRegion(bloodLabel, "FluffyMedical.ClickToSortBy".Translate("BleedingRate".Translate()));
            Widgets.DrawHighlightIfMouseover(bloodLabel);
            x += 50f;

            // Operations
            Rect opLabel = new Rect(x, 50f, 50f, 30f);
            Rect opIcon = new Rect(x + 17f, 60f, 16f, 16f);
            GUI.DrawTexture(opIcon, Utility_Medical.opTexture);
            if (Widgets.InvisibleButton(opLabel))
            {
                if (orderBy == order.Operations)
                {
                    asc = !asc;
                }
                else
                {
                    orderBy = order.Operations;
                    asc = true;
                }
                isDirty = true;
            }
            TooltipHandler.TipRegion(opLabel, "FluffyMedical.ClickToSortBy".Translate("FluffyMedical.CurrentOperations".Translate()));
            Widgets.DrawHighlightIfMouseover(opLabel);
            x += 50f;

            bool offset = true;
            // extra 15f offset for... what? makes labels roughly align.
            float colWidth = (rect.width - x - 15f) / capDefs.Count;
            for (int i = 0; i < capDefs.Count; i++)
            {
                Rect defLabel = new Rect(x + colWidth * i - colWidth / 2, 10f + (offset ? 10f : 40f), colWidth * 2, 30f);
                Widgets.DrawLine(new Vector2(x + colWidth * (i + 1) - colWidth / 2, 40f + (offset ? 5f : 35f)), new Vector2(x + colWidth * (i + 1) - colWidth / 2, 80f), Color.gray, 1);
                Widgets.Label(defLabel, capDefs[i].LabelCap);
                if (Widgets.InvisibleButton(defLabel))
                {
                    if (orderBy == order.Efficiency && orderByCapDef == capDefs[i])
                    {
                        asc = !asc;
                    }
                    else
                    {
                        orderBy = order.Efficiency;
                        orderByCapDef = capDefs[i];
                        asc = true;
                    }
                    isDirty = true;
                }
                TooltipHandler.TipRegion(defLabel, "FluffyMedical.ClickToSortBy".Translate(capDefs[i].LabelCap));
                Widgets.DrawHighlightIfMouseover(defLabel);

                offset = !offset;
            }

            GUI.EndGroup();

            Rect content = new Rect(0f, position.yMax, rect.width, rect.height - position.yMax);
            GUI.BeginGroup(content);
            base.DrawRows(new Rect(0f, 0f, content.width, content.height));
            GUI.EndGroup();
        }

        public static Rect inner(Rect rect, float size)
        {
            return new Rect(rect.xMin + (rect.width - size) / 2, rect.yMin + (rect.height - size) / 2, size, size);
        }

        protected override void DrawPawnRow(Rect rect, Pawn p)
        {
            // name is handled in PreDrawRow, start at 175
            float x = 175f;
            float y = rect.yMin;

            // care
            Rect careRect = new Rect(x, y, 100f, 30f);
            Utility_Medical.MedicalCareSetter(careRect, ref p.playerSettings.medCare);
            x += 100f;

            // blood
            Rect bloodRect = new Rect(x, y, 50f, 30f);
            float bleedRate = p.health.hediffSet.BleedingRate; // float in range 0 - 1
            float iconSize;
            if (bleedRate < 0.01f)
            {
                iconSize = 0f;
            }
            else if (bleedRate < .1f)
            {
                iconSize = 8f;
            }
            else if (bleedRate < .3f)
            {
                iconSize = 16f;
            }
            else
            {
                iconSize = 24f;
            }
            Rect iconRect = inner(bloodRect, iconSize);
            GUI.DrawTexture(iconRect, Utility_Medical.bloodTexture);
            TooltipHandler.TipRegion(bloodRect, "BleedingRate".Translate() + ": " + bleedRate.ToStringPercent() + "/" + "LetterDay".Translate());
            Widgets.DrawHighlightIfMouseover(bloodRect);
            x += 50f;

            // Operations
            Rect opLabel = new Rect(x, y, 50f, 30f);
            Rect opIcon = new Rect(x + 17f, 60f, 16f, 16f);
            if (Widgets.InvisibleButton(opLabel))
            {
                if(Event.current.button == 0)
                {
                    Utility_Medical.recipeOptionsMaker(p);
                }
                else if (Event.current.button == 1)
                {
                    p.BillStack.Clear();
                }
            }
            StringBuilder opLabelString = new StringBuilder();
            opLabelString.AppendLine("FluffyMedical.ClickTo".Translate("FluffyMedical.ScheduleOperation".Translate()));
            opLabelString.AppendLine("FluffyMedical.RightClickTo".Translate("FluffyMedical.UnScheduleOperations".Translate()));
            opLabelString.AppendLine();
            opLabelString.AppendLine("FluffyMedical.ScheduledOperations".Translate());

            bool opScheduled = false;
            foreach (Bill op in p.BillStack)
            {
                opLabelString.AppendLine(op.LabelCap);
                opScheduled = true;
            }
            
            if (opScheduled)
            {
                GUI.DrawTexture(inner(opLabel, 16f), Widgets.CheckboxOnTex);
            }
            else
            {
                opLabelString.AppendLine("FluffyMedical.NumCurrentOperations".Translate("No"));
            }

            TooltipHandler.TipRegion(opLabel, opLabelString.ToString());
            Widgets.DrawHighlightIfMouseover(opLabel);
            x += 50f;

            // main window
            Text.Anchor = TextAnchor.MiddleCenter;
            float colWidth = (rect.width - x) / capDefs.Count;
            for (int i = 0; i < capDefs.Count; i++)
            {
                Rect capDefCell = new Rect(x, y, colWidth, 30f);
                Pair<string, Color> colorPair = HealthCardUtility.GetEfficiencyLabel(p, capDefs[i]);
                string label = (p.health.capacities.GetEfficiency(capDefs[i]) * 100f).ToString("F0") + "%";
                GUI.color = colorPair.Second;
                Widgets.Label(capDefCell, label);
                if (Mouse.IsOver(capDefCell))
                {
                    GUI.DrawTexture(capDefCell, TexUI.HighlightTex);
                }
                Utility_Medical.DoHediffTooltip(capDefCell, p, capDefs[i]);
                x += colWidth;
            }
        }
    }
}

