// Karel Kroeze
// PawnColumnWorker_Diseases.cs
// 2017-05-15

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy
{
    public class PawnColumnWorker_Diseases : PawnColumnWorker
    {
        private static readonly MethodInfo _getTooltipMethodInfo;

        static PawnColumnWorker_Diseases()
        {
            _getTooltipMethodInfo =
                typeof( HealthCardUtility ).GetMethod( "GetTooltip", BindingFlags.NonPublic | BindingFlags.Static );
            if ( _getTooltipMethodInfo == null )
                throw new MissingMethodException( "HealthCardUtility.GetTooltip not found" );
        }

        public override int Compare( Pawn a, Pawn b )
        {
            return GetValueToCompareTo( a ).CompareTo( GetValueToCompareTo( b ) );
        }

        public override void DoCell( Rect rect, Pawn pawn, PawnTable table )
        {
            var diseases = pawn.GetPotentiallyLethalHediffs();
            var diseaseRect = new Rect( rect.xMin - Constants.IconSize                   / 2f,
                                        rect.yMin + ( rect.height - Constants.IconSize ) / 2f,
                                        Constants.IconSize, Constants.IconSize );
            var n = diseases.Count();
            foreach ( var disease in diseases )
            {
                diseaseRect.x += Constants.StatColumnMinWidth / ( n + 1 );
                DrawDiseaseIndicator( diseaseRect, (CapacityUtility.DiseaseProgress) disease );
            }

            TooltipHandler.TipRegion( rect, () => GetTooltip( pawn, diseases ), pawn.GetHashCode() );
        }

        private string GetTooltip( Pawn pawn, IEnumerable<Hediff> diseases )
        {
            var tip = "";
            foreach ( var set in diseases.GroupBy( k => k.Part ) )
                tip += GetDiseaseTooltip( pawn, set, set.Key ) + "\n\n";
            return tip;
        }

        public override void DoHeader( Rect rect, PawnTable table )
        {
            def.headerIconSize = new Vector2( Constants.HeaderIconSize, Constants.HeaderIconSize );
            base.DoHeader( rect, table );
        }

        public void DrawDiseaseIndicator( Rect rect, CapacityUtility.DiseaseProgress disease )
        {
            // draw immunity
            if ( disease.immunity > 0 )
            {
                var immunityRect = rect.ContractedBy( Mathf.Lerp( rect.width / 2f, 0f, disease.immunity ) );
                GUI.color = new Color( 1f, 1f, 1f, Mathf.Lerp( .5f, 1f, disease.immunity ) );
                GUI.DrawTexture( immunityRect, Resources.Circle );
            }

            // draw disease progress
            var diseaseProgressRect = rect.ContractedBy( Mathf.Lerp( rect.width / 2f, 0f, disease.severity ) );
            GUI.color = disease.color;
            GUI.DrawTexture( diseaseProgressRect, Resources.Circle );
            GUI.color = Color.white;

            // draw indicator
            GUI.color = disease.tended ? Color.white : Color.gray;
            GUI.DrawTexture( rect, Resources.DashCircle );
        }

        public string GetDiseaseTooltip( Pawn pawn, IEnumerable<Hediff> diffs, BodyPartRecord part )
        {
            return _getTooltipMethodInfo.Invoke( null, new object[] {diffs, pawn, part} ) as string;
        }

        public override int GetMinWidth( PawnTable table )
        {
            return Constants.StatColumnMinWidth;
        }

        public float GetValueToCompareTo( Pawn pawn )
        {
            var diseases = pawn.GetDiseases()
                               .Select( d => (CapacityUtility.DiseaseProgress) d )
                               .ToList();
            if ( !diseases.Any() )
                return -1;

            return diseases.Max( d => d.severity - d.immunity );
        }
    }
}