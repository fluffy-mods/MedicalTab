// Karel Kroeze
// Controller.cs
// 2017-05-16

using System.Reflection;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy
{
    public class Controller : Mod
    {
        #region Constructors

        public Controller(ModContentPack content) : base(content)
        {
            // postfix implied def generation for capacity columns
            //HarmonyInstance.DEBUG = true;
            HarmonyInstance harmony = HarmonyInstance.Create("fluffy.medicaltab");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // get/create settings
            GetSettings<Settings>();
        }

        #region Overrides of Mod

        public override string SettingsCategory() { return "MedicalTab".Translate(); }
        public override void DoSettingsWindowContents( Rect inRect ) { Settings.DoSettingsWindowContents( inRect ); }

        #endregion

        #endregion Constructors
    }
}
