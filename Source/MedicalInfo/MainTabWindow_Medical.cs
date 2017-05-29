// Karel Kroeze
// MainTabWindow_Medical.cs
// 2017-05-14

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy
{
    public enum SourceType
    {
        Colonists,
        Animals,
        Prisoners,
        Visitors,
        Hostiles
    }

    public class MainTabWindow_Medical : MainTabWindow_PawnTable
    {
        #region Fields

        private static MainTabWindow_Medical _instance;
        private static FieldInfo _tableFieldInfo;
        private SourceType _source = SourceType.Colonists;

        #endregion Fields

        #region Constructors

        static MainTabWindow_Medical()
        {
            _tableFieldInfo = typeof(MainTabWindow_PawnTable).GetField("table",
                                                                          BindingFlags.Instance | BindingFlags.NonPublic);
            if (_tableFieldInfo == null)
                throw new NullReferenceException("table field not found!");
        }

        public MainTabWindow_Medical()
        {
            _instance = this;
        }

        #endregion Constructors

        #region Properties

        public static MainTabWindow_Medical Instance => _instance;

        public SourceType Source
        {
            get { return _source; }
            private set
            {
                _source = value;
                RebuildTable();
            }
        }

        public PawnTable Table
        {
            get { return _tableFieldInfo.GetValue(this) as PawnTable; }
            private set { _tableFieldInfo.SetValue(this, value); }
        }

        protected override IEnumerable<Pawn> Pawns
        {
            get
            {
                switch (Source)
                {
                    case SourceType.Colonists:
                        return Find.VisibleMap.mapPawns.FreeColonists;

                    case SourceType.Animals:
                        return Find.VisibleMap.mapPawns
                                   .PawnsInFaction(Faction.OfPlayer)
                                   .Where(p => p.RaceProps.Animal)
                                   .OrderByDescending(p => p.RaceProps.petness)
                                   .ThenBy(p => p.RaceProps.baseBodySize)
                                   .ThenBy(p => p.def.label);

                    case SourceType.Prisoners:
                        return Find.VisibleMap.mapPawns.PrisonersOfColony;

                    case SourceType.Hostiles:
                        return Find.VisibleMap.mapPawns
                                   .AllPawnsSpawned
                                   .Where(p => p.RaceProps.Humanlike &&
                                               p.Faction.HostileTo(Faction.OfPlayer) &&
                                               ( Settings.ShowAllHostiles || p.health.Downed ) &&
                                               !Find.VisibleMap.fogGrid.IsFogged( p.PositionHeld ) );

                    case SourceType.Visitors:
                        return Find.VisibleMap.mapPawns
                                   .AllPawnsSpawned
                                   .Where(p => p.RaceProps.Humanlike &&
                                               p.Faction != Faction.OfPlayer &&
                                               !p.Faction.HostileTo(Faction.OfPlayer) &&
                                               !Find.VisibleMap.fogGrid.IsFogged(p.PositionHeld));

                    default:
                        return base.Pawns;
                }
            }
        }

        protected override PawnTableDef PawnTableDef => PawnTableDefOf.Medical;

        #endregion Properties

        #region Methods

        public void DoSourceSelectionButton(Rect rect)
        {
            // apparently, font size going to tiny on fully zooming in is working as designed...
            Text.Font = GameFont.Small;
            if (Widgets.ButtonText(rect, Source.ToString().Translate()))
            {
                var options = new List<FloatMenuOption>();

                foreach (SourceType sourceOption in Enum.GetValues(typeof(SourceType)).OfType<SourceType>())
                    if (sourceOption != Source)
                        options.Add(new FloatMenuOption(sourceOption.ToString().Translate(), delegate { Source = sourceOption; }));

                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        public override void DoWindowContents(Rect rect)
        {
            DoSourceSelectionButton(new Rect(rect.xMin, rect.yMin, 150f, 30f));
            base.DoWindowContents(rect);
        }

        private void RebuildTable()
        {
            IEnumerable<PawnColumnDef> columns =
                PawnTableDef.columns.Where(c => (c.Worker as OptionalColumn)?.ShowFor(Source) ?? true);
            Table = new PawnTable(columns, () => Pawns, 998, UI.screenWidth - (int)(Margin * 2f), 0,
                                   (int)(UI.screenHeight - 35 - ExtraBottomSpace - ExtraTopSpace - Margin * 2f));
        }

        #endregion Methods
    }
}
