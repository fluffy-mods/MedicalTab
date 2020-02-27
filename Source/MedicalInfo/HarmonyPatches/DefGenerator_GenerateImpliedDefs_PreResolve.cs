// Karel Kroeze
// DefGenerator_GenerateImpliedDefs_PreResolve.cs
// 2017-05-16

using DynamicPawnTable;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Fluffy
{
    [HarmonyPatch(typeof(DefGenerator), nameof( DefGenerator.GenerateImpliedDefs_PreResolve ))]
    public class DefGenerator_GenerateImpliedDefs_PreResolve
    {
        #region Methods

        public static void Postfix()
        {
            var moveLabelDown = false;
            DynamicPawnTableDef medicalTable = DynamicPawnTableDefOf.Medical;

            foreach (PawnCapacityDef capacity in DefDatabase<PawnCapacityDef>.AllDefsListForReading)
            {
                var column = new PawnColumnDef_Capacity();
                column.defName = "PawnColumnDef_" + capacity.defName;
                column.capacity = capacity;
                column.description = capacity.description;
                column.label = capacity.label;
                column.moveLabelDown = moveLabelDown;
                column.sortable = true;
                column.workerClass = typeof(PawnColumnWorker_Capacity);

                column.PostLoad();
                DefDatabase<PawnColumnDef>.Add(column);
                medicalTable.columns.Insert(medicalTable.columns.Count - 1, column);

                moveLabelDown = !moveLabelDown;
            }
        }

        #endregion Methods
    }
}
