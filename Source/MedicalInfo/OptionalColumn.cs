// Karel Kroeze
// OptionalColumn.cs
// 2017-05-18

using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy
{
    public interface OptionalColumn
    {
        #region Methods

        bool ShowFor(SourceType source);

        #endregion Methods
    }
}
