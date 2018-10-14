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
            public bool tended;
            public int tillTendTicks;

            #endregion Fields

            #region Methods

            public static explicit operator DiseaseProgress(Hediff hediff)
            {
                var immunizable = hediff.TryGetComp<HediffComp_Immunizable>();
                if (immunizable == null)
                    throw new NullReferenceException($"hediff does not have immunizable comp");
                var tendable = hediff.TryGetComp<HediffComp_TendDuration>();
                
                //int tillTendTicks = -1;
                //var tendComp = hediff.TryGetComp<HediffComp_TendDuration>();
                //if (tendComp != null)
                //    tillTendTicks = tendComp.tendTick + tendComp.TProps.tendDuration - Find.TickManager.TicksGame;

                return new DiseaseProgress
                {
                    label = hediff.Label,
                    immunity = immunizable.Immunity,
                    severity = hediff.Severity,
                    tended = !hediff.TendableNow(),
                    tillTendTicks = tendable?.tendTicksLeft ?? 0
                };
            }

            #endregion Methods
        }

        #endregion Structs

        #region Fields

        public static Dictionary<PawnCapacityDef, HashSet<BodyPartTagDef>> CapacityTags =
            new Dictionary<PawnCapacityDef, HashSet<BodyPartTagDef>>();

        private static MethodInfo _generateSurgeryOptionMethodInfo;

        #endregion Fields

        #region Constructors

        static CapacityUtility()
        {
            var filtrationTags = new HashSet<BodyPartTagDef>();
            filtrationTags.Add(BodyPartTagDefOf.BloodFiltrationKidney);
            filtrationTags.Add(BodyPartTagDefOf.BloodFiltrationLiver);
            filtrationTags.Add(BodyPartTagDefOf.BloodFiltrationSource);
            CapacityTags.Add(PawnCapacityDefOf.BloodFiltration, filtrationTags);

            var pumpingTags = new HashSet<BodyPartTagDef>();
            pumpingTags.Add(BodyPartTagDefOf.BloodPumpingSource);
            CapacityTags.Add(PawnCapacityDefOf.BloodPumping, pumpingTags);

            var breathingTags = new HashSet<BodyPartTagDef>();
            breathingTags.Add(BodyPartTagDefOf.BreathingPathway);
            breathingTags.Add(BodyPartTagDefOf.BreathingSource);
            breathingTags.Add(BodyPartTagDefOf.BreathingSourceCage);
            CapacityTags.Add(PawnCapacityDefOf.Breathing, breathingTags);

            var consciousnessTags = new HashSet<BodyPartTagDef>();
            consciousnessTags.Add(BodyPartTagDefOf.ConsciousnessSource);
            CapacityTags.Add(PawnCapacityDefOf.Consciousness, consciousnessTags);

            var eatingTags = new HashSet<BodyPartTagDef>();
            eatingTags.Add(BodyPartTagDefOf.EatingPathway);
            eatingTags.Add(BodyPartTagDefOf.EatingSource);
            CapacityTags.Add(PawnCapacityDefOf.Eating, eatingTags);

            var hearingTags = new HashSet<BodyPartTagDef>();
            hearingTags.Add(BodyPartTagDefOf.HearingSource);
            CapacityTags.Add(PawnCapacityDefOf.Hearing, hearingTags);

            var manipulationTags = new HashSet<BodyPartTagDef>();
            manipulationTags.Add(BodyPartTagDefOf.ManipulationLimbCore);
            manipulationTags.Add(BodyPartTagDefOf.ManipulationLimbDigit);
            manipulationTags.Add(BodyPartTagDefOf.ManipulationLimbSegment);
            CapacityTags.Add(PawnCapacityDefOf.Manipulation, manipulationTags);

            var metabolismTags = new HashSet<BodyPartTagDef>();
            metabolismTags.Add( BodyPartTagDefOf.MetabolismSource);
            CapacityTags.Add(PawnCapacityDefOf.Metabolism, metabolismTags);

            var movingTags = new HashSet<BodyPartTagDef>();
            movingTags.Add(BodyPartTagDefOf.MovingLimbCore);
            movingTags.Add(BodyPartTagDefOf.MovingLimbDigit);
            movingTags.Add(BodyPartTagDefOf.MovingLimbSegment);
            movingTags.Add(BodyPartTagDefOf.Pelvis);
            movingTags.Add(BodyPartTagDefOf.Spine);
            CapacityTags.Add(PawnCapacityDefOf.Moving, movingTags);

            var sightTags = new HashSet<BodyPartTagDef>();
            sightTags.Add(BodyPartTagDefOf.SightSource);
            CapacityTags.Add(PawnCapacityDefOf.Sight, sightTags);

            var talkingTags = new HashSet<BodyPartTagDef>();
            talkingTags.Add(BodyPartTagDefOf.TalkingPathway);
            talkingTags.Add(BodyPartTagDefOf.TalkingSource);
            CapacityTags.Add(PawnCapacityDefOf.Talking, talkingTags);

            // try and make an educated guess for any other capacity added by mods
            foreach (PawnCapacityDef capacityDef in DefDatabase<PawnCapacityDef>.AllDefsListForReading)
            {
                if (CapacityTags.ContainsKey(capacityDef))
                    continue;

                var tags = new HashSet<BodyPartTagDef>();
                foreach ( var tag in DefDatabase<BodyPartTagDef>.AllDefsListForReading.Where(
                    td => td.defName.Contains( capacityDef.defName )
                ) )
                {
                    Log.Message( $"Medical Tab :: Adding {tag.defName} to the list of required capacities for {capacityDef.defName}." );
                    tags.Add( tag );
                }

                if ( tags.Count == 0 )
                {
                    Log.Warning( $"Medical Tab :: Capacity {capacityDef.defName} does not have any bodyPartTags associated with it. This may be intentional." );
                }
                CapacityTags.Add(capacityDef, tags);
            }
            
            // spawn a message about orphan tags
            foreach ( var tag in DefDatabase<BodyPartTagDef>.AllDefsListForReading )
            {
                var used = false;
                foreach ( var tagset in CapacityTags.Values )
                {
                    if ( tagset.Contains( tag ) )
                    {
                        used = true;
                        break;
                    }
                }

                if ( !used )
                {
                    Log.Warning( $"Medical Tab :: Tag {tag.defName} is not associated with any pawnCapacity. This may be intentional." );
                }
            }
        }

        #endregion Constructors

        #region Methods


        public static List<Hediff> GetDiseases( this Pawn pawn)
        {
            return pawn.health.hediffSet.hediffs
                .Where(
                    h =>
                        h.Visible && h.def.lethalSeverity > 0 &&
                        h.def.PossibleToDevelopImmunityNaturally() &&
                        h.TryGetComp<HediffComp_Immunizable>() != null)
                .ToList();
        }

        public static bool IsHealthy( this Pawn pawn )
        {
            if ( pawn.health.State != PawnHealthState.Mobile ||
                pawn.health.summaryHealth.SummaryHealthPercent < 1f ||
                pawn.health.hediffSet.BleedRateTotal > 0f ||
                pawn.health.hediffSet.PainTotal > 0f ||
                pawn.GetDiseases().Any() ||
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
            return !r.PotentiallyMissingIngredients( null, pawn.Map ).Any();
        }

        public static bool ReducesPainOnIngestion(this ThingDef def)
        {
            return
                def?.ingestible?.outcomeDoers?.OfType<IngestionOutcomeDoer_GiveHediff>()
                   .Any(od => od.hediffDef.IsHediffThatReducesPain()) ?? false;
        }

        public static bool ThisOrAnyChildHasTag( this BodyPartRecord part, BodyPartTagDef tag )
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
