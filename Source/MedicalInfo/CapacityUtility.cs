// Karel Kroeze
// CapacityUtility.cs
// 2017-05-17

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy
{
    // todo; consolidation and clean up of various helpers.
    // todo; lobby for xml implementation of capacity tags on bodyparts so we can get rid of the dictionary.
    [StaticConstructorOnStartup]
    public static class CapacityUtility
    {

        #region Structs

        public struct DiseaseProgress
        {
            #region Fields

            public float immunity;
            public string label;
            public float severity;

            #endregion Fields

            #region Methods

            public static implicit operator DiseaseProgress(Hediff hediff)
            {
                var comp = hediff.TryGetComp<HediffComp_Immunizable>();
                if (comp == null)
                    throw new NullReferenceException($"hediff does not have immunizable comp");

                return new DiseaseProgress
                {
                    label = hediff.Label,
                    immunity = comp.Immunity,
                    severity = hediff.Severity
                };
            }

            #endregion Methods
        }

        #endregion Structs

        #region Fields

        public static Dictionary<PawnCapacityDef, HashSet<string>> CapacityTags =
            new Dictionary<PawnCapacityDef, HashSet<string>>();

        private static MethodInfo _generateSurgeryOptionMethodInfo;

        #endregion Fields

        #region Constructors

        static CapacityUtility()
        {
            var filtrationTags = new HashSet<string>();
            filtrationTags.Add("BloodFiltrationKidney");
            filtrationTags.Add("BloodFiltrationSource");
            filtrationTags.Add("BloodFiltrationLiver");
            CapacityTags.Add(PawnCapacityDefOf.BloodFiltration, filtrationTags);

            var pumpingTags = new HashSet<string>();
            pumpingTags.Add("BloodPumpingSource");
            CapacityTags.Add(PawnCapacityDefOf.BloodPumping, pumpingTags);

            var breathingTags = new HashSet<string>();
            breathingTags.Add("BreathingSource");
            CapacityTags.Add(PawnCapacityDefOf.Breathing, breathingTags);

            var consciousnessTags = new HashSet<string>();
            consciousnessTags.Add("ConsciousnessSource");
            CapacityTags.Add(PawnCapacityDefOf.Consciousness, consciousnessTags);

            var eatingTags = new HashSet<string>();
            eatingTags.Add("EatingSource");
            CapacityTags.Add(PawnCapacityDefOf.Eating, eatingTags);

            var hearingTags = new HashSet<string>();
            hearingTags.Add("HearingSource");
            CapacityTags.Add(PawnCapacityDefOf.Hearing, hearingTags);

            var manipulationTags = new HashSet<string>();
            manipulationTags.Add("ManipulationLimbCore");
            manipulationTags.Add("ManipulationLimbSegment");
            manipulationTags.Add("ManipulationLimbDigit");
            CapacityTags.Add(PawnCapacityDefOf.Manipulation, manipulationTags);

            var metabolismTags = new HashSet<string>();
            metabolismTags.Add("MetabolismSource");
            CapacityTags.Add(PawnCapacityDefOf.Metabolism, metabolismTags);

            var movingTags = new HashSet<string>();
            movingTags.Add("MovingLimbCore");
            movingTags.Add("MovingLimbSegment");
            movingTags.Add("MovingLimbDigit");
            movingTags.Add("Pelvis");
            movingTags.Add("Spine");
            CapacityTags.Add(PawnCapacityDefOf.Moving, movingTags);

            var sightTags = new HashSet<string>();
            sightTags.Add("SightSource");
            CapacityTags.Add(PawnCapacityDefOf.Sight, sightTags);

            var talkingTags = new HashSet<string>();
            talkingTags.Add("TalkingSource");
            CapacityTags.Add(PawnCapacityDefOf.Talking, talkingTags);

            // try and make an educated guess for any other capacity added by mods
            foreach (PawnCapacityDef capacityDef in DefDatabase<PawnCapacityDef>.AllDefsListForReading)
            {
                if (CapacityTags.ContainsKey(capacityDef))
                    continue;

                var tags = new HashSet<string>();
                tags.Add(capacityDef.LabelCap + "Source");
                CapacityTags.Add(capacityDef, tags);
            }
        }

        #endregion Constructors

        #region Methods


        public static List<DiseaseProgress> GetDiseaseProgresses( this Pawn pawn)
        {
            return pawn.health.hediffSet.hediffs
                .Where(
                    h =>
                        h.Visible && h.def.lethalSeverity > 0 &&
                        h.def.PossibleToDevelopImmunityNaturally() &&
                        h.TryGetComp<HediffComp_Immunizable>() != null)
                .Select(h => (DiseaseProgress)h)
                .ToList();
        }

        public static bool IsHealthy( this Pawn pawn )
        {
            if ( pawn.health.State != PawnHealthState.Mobile ||
                pawn.health.summaryHealth.SummaryHealthPercent < 1f ||
                pawn.health.hediffSet.BleedRateTotal > 0f ||
                pawn.health.hediffSet.PainTotal > 0f ||
                pawn.GetDiseaseProgresses().Any() ||
                DefDatabase<PawnCapacityDef>.AllDefsListForReading
                    .Any( cap => pawn.health.capacities.GetLevel( cap ) < 1f ) )
                return false;
            return true;
        }

        public static List<FloatMenuOption> AddedPartOptionsThatAffect(this RecipeDef r, PawnCapacityDef capacity,
                                                                        Pawn pawn, bool negative = false)
        {
            var options = new List<FloatMenuOption>();

            if (!r?.addsHediff?.IsAddedPart() ?? true)
                return options;

            if (!NotMissingVitalIngredient(pawn, r))
                return options;

            float after = r.addsHediff.addedPartProps.partEfficiency;

            IEnumerable<BodyPartRecord> parts = r.Worker.GetPartsToApplyOn(pawn, r)
                                                 .Where(p => p.Affects(capacity) &&
                                                             !pawn.health.hediffSet.AncestorHasDirectlyAddedParts(p));

            foreach (BodyPartRecord part in parts)
            {
                float current = PawnCapacityUtility.CalculatePartEfficiency(pawn.health.hediffSet, part);
                if (after < current == negative)
                    options.Add(GenerateSurgeryOption(pawn, pawn, r, r.PotentiallyMissingIngredients(null, pawn.Map),
                                                        part));
            }

            return options;
        }

        public static bool AddsHediffThatAffects(this RecipeDef r, PawnCapacityDef capacity, float current,
                                                  bool negative = false)
        {
            return r.addsHediff.IsHediffThatAffects(capacity, current, negative);
        }

        public static bool Affects( this Bill_Medical bill, PawnCapacityDef capacity )
        {
            if ( bill?.recipe == null )
                return false;
            
            return bill.recipe.AddsHediffThatAffects( capacity, -1 ) ||
                   bill.recipe.AdministersDrugThatAffects( capacity, -1 ) ||
                   ( bill.recipe.addsHediff.IsAddedPart() && bill.Part.Affects( capacity ));
        }

        public static bool AddsHediffThatReducesPain(this RecipeDef r)
        {
            return r.addsHediff.IsHediffThatReducesPain();
        }

        public static bool AdministersDrugThatAffects(this RecipeDef r, PawnCapacityDef capacity, float current,
                                                       bool negative = false)
        {
            if ( r.ingredients.NullOrEmpty() )
                return false;
            return r.ingredients[0].filter.BestThingRequest.singleDef.AffectsCapacityOnIngestion(capacity, current,
                                                                                                  negative);
        }

        public static bool AdministersDrugThatReducesPain(this RecipeDef r)
        {
            if (r.ingredients.NullOrEmpty())
                return false;
            return r.ingredients[0].filter.BestThingRequest.singleDef.ReducesPainOnIngestion();
        }

        public static bool Affects(this BodyPartRecord part, PawnCapacityDef capacity)
        {
            return CapacityTags[capacity].Any(tag => part.ThisOrAnyChildHasTag(tag));
        }

        public static bool AffectsCapacityOnIngestion(this ThingDef def, PawnCapacityDef capacity, float current,
                                                       bool negative = false)
        {
            return
                def?.ingestible?.outcomeDoers?.OfType<IngestionOutcomeDoer_GiveHediff>()
                   .Any(od => od.hediffDef.IsHediffThatAffects(capacity, current, negative)) ?? false;
        }

        public static FloatMenuOption GenerateSurgeryOption(Pawn pawn, Thing thingForMedBills, RecipeDef recipe,
                                                             IEnumerable<ThingDef> missingIngredients,
                                                             BodyPartRecord part = null)
        {
            if (_generateSurgeryOptionMethodInfo == null)
            {
                _generateSurgeryOptionMethodInfo = typeof(HealthCardUtility).GetMethod("GenerateSurgeryOption",
                                                                                          BindingFlags.NonPublic |
                                                                                          BindingFlags.Static);
                if (_generateSurgeryOptionMethodInfo == null)
                    throw new NullReferenceException("GenerateSurgeryOption method info not found!");
            }

            return
                _generateSurgeryOptionMethodInfo.Invoke(null,
                                                         new object[]
                                                             {pawn, thingForMedBills, recipe, missingIngredients, part})
                    as FloatMenuOption;
        }

        public static bool IsAddedPart(this HediffDef hediff)
        {
            return hediff?.addedPartProps?.partEfficiency != null;
        }

        public static bool IsHediffThatAffects(this HediffDef hediffDef, PawnCapacityDef capacity, float current,
                                                bool negative = false)
        {
            if (hediffDef?.stages.NullOrEmpty() ?? true)
                return false;

            foreach (HediffStage stage in hediffDef.stages)
            {
                if (stage.capMods.NullOrEmpty())
                    continue;

                foreach (PawnCapacityModifier capMod in stage.capMods)
                    if (capMod.capacity == capacity)
                    {
                        float after = Mathf.Min((current + capMod.offset) * capMod.postFactor, capMod.setMax);
                        return after < current == negative;
                    }
            }

            return false;
        }

        public static bool IsHediffThatReducesPain(this HediffDef hediffDef)
        {
            if (hediffDef?.stages.NullOrEmpty() ?? true)
                return false;

            return hediffDef.stages?.Any(hs => hs.painFactor < 1f || hs.painOffset < 0f) ?? false;
        }

        public static bool NotMissingVitalIngredient(Pawn pawn, RecipeDef r)
        {
            return !r.PotentiallyMissingIngredients(null, pawn.Map)
                     .Any(td => td.IsDrug || td.isBodyPartOrImplant);
        }

        public static bool ReducesPainOnIngestion(this ThingDef def)
        {
            return
                def?.ingestible?.outcomeDoers?.OfType<IngestionOutcomeDoer_GiveHediff>()
                   .Any(od => od.hediffDef.IsHediffThatReducesPain()) ?? false;
        }

        public static bool ThisOrAnyChildHasTag( this BodyPartRecord part, string tag )
        {
            if ( part?.def?.tags == null )
                return false;

            if ( part.def.tags.Contains( tag ) )
                return true;

            if ( part.parts.NullOrEmpty() )
                return false;

            return part.parts.Any( p => p.ThisOrAnyChildHasTag( tag ) );
        }

        #endregion Methods
    }
}
