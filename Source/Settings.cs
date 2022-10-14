// Karel Kroeze
// Settings.cs
// 2017-05-20

using UnityEngine;
using Verse;

namespace Fluffy {
    public class Settings: ModSettings {
        #region Overrides of ModSettings

        public static bool SuggestDrugs = true;
        public static bool ShowAllHostiles = false;

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref SuggestDrugs, "SuggestDrugs", true);
            Scribe_Values.Look(ref ShowAllHostiles, "ShowAllHostiles", false);
        }

        #endregion

        public static void DoSettingsWindowContents(Rect rect) {
            Listing_Standard list = new Listing_Standard(GameFont.Small) {
                ColumnWidth = rect.width
            };
            list.Begin(rect);

            list.CheckboxLabeled("MedicalTab.SuggestDrugs".Translate(), ref SuggestDrugs,
                                  "MedicalTab.SuggestDrugsTip".Translate());
            list.CheckboxLabeled("MedicalTab.ShowAllHostiles".Translate(), ref ShowAllHostiles,
                                  "MedicalTab.ShowAllHostilesTip".Translate());

            list.End();
        }
    }
}
