//// duduluu
//// MainTabWindow_Medical.cs
//// 2017-06-22
//
//using Harmony;
//using RimWorld;
//using Verse;
//
//
// TODO: RimWorld 1.0 adds PostImpliedDefGeneration injection step, check if this is still required.
//namespace Fluffy
//{
//    [HarmonyPatch(typeof(LoadedLanguage), "InjectIntoData")]
//    public class LoadedLanguage_InjectIntoData
//    {
//        #region Methods
//
//        // Invoke after loading language, so that capacity column headers can use translated label.
//        public static void Postfix(LoadedLanguage __instance)
//        {
//            foreach (PawnColumnDef column in DefDatabase<PawnColumnDef>.AllDefsListForReading)
//            {
//                var column_Capacity = column as PawnColumnDef_Capacity;
//                if (column_Capacity != null)
//                {
//                    var capacity = column_Capacity.capacity;
//                    column_Capacity.label = capacity.label;
//                    column_Capacity.description = capacity.description;
//                }
//            }
//        }
//
//        #endregion
//    }
//}
