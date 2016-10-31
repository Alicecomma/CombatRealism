using System;
using System.Collections.Generic;
using CommunityCoreLibrary;
using RimWorld;
using Verse;

namespace Combat_Realism
{
    [SpecialInjectorSequencer(InjectionSequence.MainLoad, InjectionTiming.SpecialInjectors)]
    public class TabInjector : SpecialInjector
    {
        #region Methods

        public override bool Inject()
        {
            // get reference to lists of itabs
            List<Type> itabs = ThingDefOf.Human.inspectorTabs;
            List<ITab> itabsResolved = ThingDefOf.Human.inspectorTabsResolved;

            /*

#if DEBUG
            Log.Message( "Inspector tab types on humans:" );
            foreach ( var tab in itabs )
            {
                Log.Message( "\t" + tab.Name );
            }
            Log.Message( "Resolved tab instances on humans:" );
            foreach ( var tab in itabsResolved )
            {
                Log.Message( "\t" + tab.labelKey.Translate() );
            }
#endif
            */

            // replace ITab in the unresolved list
            int index = itabs.IndexOf(typeof(ITab_Pawn_Gear));
            if (index != -1)
            {
                itabs.Remove(typeof(ITab_Pawn_Gear));
                itabs.Insert(index, typeof(ITab_Inventory));
            }

            // replace resolved ITab, if needed.
            ITab oldGearTab = ITabManager.GetSharedInstance(typeof(ITab_Pawn_Gear));
            ITab newGearTab = ITabManager.GetSharedInstance(typeof(ITab_Inventory));
            if (!itabsResolved.NullOrEmpty() && itabsResolved.Contains(oldGearTab))
            {
                int resolvedIndex = itabsResolved.IndexOf(oldGearTab);
                itabsResolved.Insert(resolvedIndex, newGearTab);
                itabsResolved.Remove(oldGearTab);
            }

            return true;
        }

        #endregion Methods
    }
}