// Karel Kroeze
// Resources.cs
// 2017-05-14

using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy
{
    [StaticConstructorOnStartup]
    public static class Resources
    {
        #region Fields

        public static Texture2D BleedingIcon,
                                Circle,
                                DashCircle,
                                StatusIconProcedure;

        #endregion Fields

        #region Constructors

        static Resources()
        {
            BleedingIcon = ContentFinder<Texture2D>.Get("UI/Icons/Medical/Bleeding");
            Circle = ContentFinder<Texture2D>.Get("UI/Icons/Medical/FillCircle");
            DashCircle = ContentFinder<Texture2D>.Get("UI/Icons/Medical/DashedCircle");
            StatusIconProcedure = ContentFinder<Texture2D>.Get("UI/Icons/Medical/Plus");
        }

        #endregion Constructors
    }
}
