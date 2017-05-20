// Karel Kroeze
// PawnColumnWorker_Diseases.cs
// 2017-05-15

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy
{
    public class PawnColumnWorker_Diseases : PawnColumnWorker
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

            public static implicit operator DiseaseProgress( Hediff hediff )
            {
                var comp = hediff.TryGetComp<HediffComp_Immunizable>();
                if ( comp == null )
                    throw new NullReferenceException( $"hediff does not have immunizable comp" );

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

        #region Methods

        public override int Compare( Pawn a, Pawn b )
        {
            return GetValueToCompareTo( a ).CompareTo( GetValueToCompareTo( b ) );
        }

        public override void DoCell( Rect rect, Pawn pawn, PawnTable table )
        {
            List<DiseaseProgress> diseases = GetDiseaseProgresses( pawn );
            var diseaseRect = new Rect( rect.xMin - Constants.IconSize / 2f,
                                        rect.yMin + ( rect.height - Constants.IconSize ) / 2f,
                                        Constants.IconSize, Constants.IconSize );
            int n = diseases.Count;

            if ( diseases.Any() )
                for ( var i = 0; i < n; i++ )
                {
                    diseaseRect.x += Constants.StatColumnMinWidth / ( n + 1 );
                    DrawDiseaseIndicator( diseaseRect, diseases[i] );
                }
        }

        public override void DoHeader( Rect rect, PawnTable table )
        {
            def.headerIconSize = new Vector2( Constants.HeaderIconSize, Constants.HeaderIconSize );
            base.DoHeader( rect, table );
        }

        public void DrawDiseaseIndicator( Rect rect, DiseaseProgress disease )
        {
            // draw indicator
            GUI.DrawTexture( rect, Resources.DashCircle );

            // draw immunity
            Rect immunityRect = rect.ContractedBy( Mathf.Lerp( rect.width / 2f, 0f, disease.immunity ) );
            GUI.color = new Color( 1f, 1f, 1f, Mathf.Lerp( .5f, 1f, disease.immunity ) );
            GUI.DrawTexture( immunityRect, Resources.Circle );

            // draw disease progress
            Rect diseaseProgressRect = rect.ContractedBy( Mathf.Lerp( rect.width / 2f, 0f, disease.severity ) );
            GUI.color = new Color( 1f, .2f, .2f, Mathf.Lerp( .5f, 1f, disease.severity ) );
            GUI.DrawTexture( diseaseProgressRect, Resources.Circle );

            GUI.color = Color.white;
            TooltipHandler.TipRegion( rect,
                                      () =>
                                          $"{disease.label}: severity; {disease.severity.ToStringPercent()}, immunity; {disease.immunity.ToStringPercent()}",
                                      rect.GetHashCode() );
        }

        public List<DiseaseProgress> GetDiseaseProgresses( Pawn pawn )
        {
            return pawn.health.hediffSet.hediffs
                       .Where(
                              h =>
                                  h.Visible && h.def.lethalSeverity > 0 &&
                                  h.def.PossibleToDevelopImmunityNaturally() &&
                                  h.TryGetComp<HediffComp_Immunizable>() != null )
                       .Select( h => (DiseaseProgress) h )
                       .ToList();
        }

        public override int GetMinWidth( PawnTable table ) { return Constants.StatColumnMinWidth; }

        public float GetValueToCompareTo( Pawn pawn )
        {
            List<DiseaseProgress> diseases = GetDiseaseProgresses( pawn );
            if ( !diseases.Any() )
                return -1;

            return diseases.Max( d => d.severity - d.immunity );
        }

        #endregion Methods
    }
}
