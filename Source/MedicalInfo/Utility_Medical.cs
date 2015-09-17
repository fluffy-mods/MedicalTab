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
    public static class Utility_Medical
    {

        public static Texture2D[] careTextures = new Texture2D[]
        {
            ContentFinder<Texture2D>.Get("UI/Icons/Medical/NoCare", true),
            ContentFinder<Texture2D>.Get("UI/Icons/Medical/NoMeds", true),
            ThingDefOf.HerbalMedicine.uiIcon,
            ThingDefOf.Medicine.uiIcon,
            ThingDefOf.GlitterworldMedicine.uiIcon
        };

        public static Texture2D bloodTexture = ContentFinder<Texture2D>.Get("UI/Icons/Medical/Bleeding", true);

        public static Texture2D bloodTextureWhite = ContentFinder<Texture2D>.Get("UI/Buttons/blood", true);

        public static Texture2D opTexture = ContentFinder<Texture2D>.Get("UI/Buttons/medical", true);

        public static void MedicalCareSetter(Rect rect, ref MedicalCareCategory medCare)
        {
            float iconSize = rect.width / 5f;
            float iconHeightOffset = (rect.height - iconSize) / 2;
            Rect rect2 = new Rect(rect.x, rect.y + iconHeightOffset, iconSize, iconSize);
            for (int i = 0; i < 5; i++)
            {
                MedicalCareCategory mc = (MedicalCareCategory)i;
                Widgets.DrawHighlightIfMouseover(rect2);
                GUI.DrawTexture(rect2, Utility_Medical.careTextures[i]);
                if (Widgets.InvisibleButton(rect2))
                {
                    medCare = mc;
                    SoundDefOf.TickHigh.PlayOneShotOnCamera();
                }
                if (medCare == mc)
                {
                    GUI.DrawTexture(rect2, Widgets.CheckboxOnTex);
                }
                TooltipHandler.TipRegion(rect2, () => mc.GetLabel(), 632165 + i * 17);
                rect2.x += rect2.width;
            }
        }

        public static void DoHediffTooltip(Rect rect, Pawn p, PawnCapacityDef capDef)
        {
            StringBuilder tooltip = new StringBuilder();
            bool tip = false;
            try
            {
                // easy bit, hediffs that have a direct effect
                // diseases and such (cataract, sickness)
                IEnumerable<Hediff> hediffs = p.health.hediffSet.GetHediffs<Hediff>().Where(h => h.Visible && h.CapMods != null);
                foreach (Hediff diff in hediffs)
                {
                    if (diff.CapMods.Count > 0 && diff.CapMods.Any(cm => cm.capacity == capDef))
                    {
                        tip = true;
                        tooltip.AppendLine((diff.Part == null ? "Whole body" : diff.Part.def.LabelCap) + ": " + diff.LabelCap);
                    }
                }

                // hard bit, hediffs that do not directly list capacity changes.
                // get parts that matter for this capDef
                List<string> activityGroups = p.RaceProps.body.GetActivityGroups(capDef);
                List<BodyPartRecord> relevantParts = new List<BodyPartRecord>();
                for (int i = 0; i < activityGroups.Count; i++)
                {
                    relevantParts.AddRange(p.RaceProps.body.GetParts(capDef, activityGroups[i]));
                }
                relevantParts.Distinct();

                // missing parts, subset to relevant
                IEnumerable<Hediff_MissingPart> missings = p.health.hediffSet.GetMissingPartsCommonAncestors().Where(h => relevantParts.Contains(h.Part));
                foreach (Hediff_MissingPart missing in missings)
                {
                    tip = true;
                    tooltip.AppendLine((missing.Part == null ? "Whole body" : missing.Part.def.LabelCap) + ": " + missing.LabelCap);
                }

                // injuries, subset to relevant
                IEnumerable<Hediff_Injury> injuries = p.health.hediffSet.GetHediffs<Hediff_Injury>().Where(h => relevantParts.Contains(h.Part));
                foreach (Hediff_Injury injury in injuries)
                {
                    tip = true;
                    tooltip.AppendLine((injury.Part == null ? "Whole body" : injury.Part.def.LabelCap) + ": " + injury.LabelCap);
                }
            }
            catch (Exception)
            {
                Log.Message("Error getting tooltip for medical info.");
            }

            if (!tip) tooltip.AppendLine("OK");

            TooltipHandler.TipRegion(rect, tooltip.ToString());
        }

        public static void medicalCareSetterAll(List<Pawn> pawns)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            for (int i = 0; i < 5; i++)
            {
                MedicalCareCategory mc = (MedicalCareCategory)i;
                FloatMenuOption option = new FloatMenuOption(mc.GetLabel(), delegate
                {
                    for (int j = 0; j < pawns.Count; j++)
                    {
                        pawns[j].playerSettings.medCare = mc;
                    }
                    SoundDefOf.TickHigh.PlayOneShotOnCamera();
                    MainTabWindow_Medical.isDirty = true;
                });
                list.Add(option);
            }
            Find.WindowStack.Add(new FloatMenu(list, true));
        }

        public static void  recipeOptionsMaker(Pawn pawn)
        {
            Thing thingForMedBills = pawn as Thing;
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (RecipeDef current in thingForMedBills.def.AllRecipes)
            {
                if (current.AvailableNow)
                {
                    IEnumerable<ThingDef> enumerable = current.PotentiallyMissingIngredients(null);
                    if (!enumerable.Any((ThingDef x) => x.isBodyPartOrImplant))
                    {
                        IEnumerable<BodyPartRecord> partsToApplyOn = current.Worker.GetPartsToApplyOn(pawn, current);
                        if (partsToApplyOn.Any<BodyPartRecord>())
                        {
                            foreach (BodyPartRecord current2 in partsToApplyOn)
                            {
                                RecipeDef localRecipe = current;
                                BodyPartRecord localPart = current2;
                                string text;
                                if (localRecipe == RecipeDefOf.RemoveBodyPart)
                                {
                                    text = HealthCardUtility.RemoveBodyPartSpecialLabel(pawn, current2);
                                }
                                else
                                {
                                    text = localRecipe.LabelCap;
                                }
                                if (!current.hideBodyPartNames)
                                {
                                    text = text + " (" + current2.def.label + ")";
                                }
                                Action action = null;
                                if (enumerable.Any<ThingDef>())
                                {
                                    text += " (";
                                    bool flag = true;
                                    foreach (ThingDef current3 in enumerable)
                                    {
                                        if (!flag)
                                        {
                                            text += ", ";
                                        }
                                        flag = false;
                                        text += "MissingMedicalBillIngredient".Translate(new object[]
                                        {
                                        current3.label
                                        });
                                    }
                                    text += ")";
                                }
                                else
                                {
                                    action = delegate
                                    {
                                        if (!Find.ListerPawns.FreeColonists.Any((Pawn col) => localRecipe.PawnSatisfiesSkillRequirements(col)))
                                        {
                                            Bill.CreateNoPawnsWithSkillDialog(localRecipe);
                                        }
                                        Pawn pawn2 = thingForMedBills as Pawn;
                                        if (pawn2 != null && !pawn.InBed() && pawn.RaceProps.Humanlike)
                                        {
                                            if (!Find.ListerBuildings.allBuildingsColonist.Any((Building x) => x is Building_Bed && (x as Building_Bed).Medical))
                                            {
                                                Messages.Message("MessageNoMedicalBeds".Translate(), MessageSound.Negative);
                                            }
                                        }
                                        Bill_Medical bill_Medical = new Bill_Medical(localRecipe);
                                        pawn2.BillStack.AddBill(bill_Medical);
                                        bill_Medical.Part = localPart;
                                        if (pawn2.Faction != null && !pawn2.Faction.def.hidden && !pawn2.Faction.HostileTo(Faction.OfColony) && localRecipe.Worker.IsViolationOnPawn(pawn2, localPart, Faction.OfColony))
                                        {
                                            Messages.Message("MessageMedicalOperationWillAngerFaction".Translate(new object[]
                                            {
                                            pawn2.Faction
                                            }), MessageSound.Negative);
                                        }
                                    };
                                }
                                list.Add(new FloatMenuOption(text, action, MenuOptionPriority.Medium, null, null));
                            }
                        }
                    }
                }
            }
            Find.WindowStack.Add(new FloatMenu(list, false));
        }
    }
}
