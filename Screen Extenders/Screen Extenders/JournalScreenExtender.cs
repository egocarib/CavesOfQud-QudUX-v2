using System.Collections.Generic;
using QudUX.Utilities;
using Qud.API;
using XRL.Core;
using XRL.UI;

namespace QudUX.ScreenExtenders
{
    public static class JournalScreenExtender
    {
        public static long CacheTurn = -1L;
        public static Dictionary<IBaseJournalEntry, JournalLocation> CachedLocations = new Dictionary<IBaseJournalEntry, JournalLocation>();

        public static bool IsALocation(this JournalScreen.JournalEntry journalEntry)
        {
            JournalLocation location = GetJournalLocationForEntry(journalEntry);
            return location.IsValid;
        }

        public static bool HasBeenVisited(this JournalScreen.JournalEntry journalEntry)
        {
            JournalLocation location = GetJournalLocationForEntry(journalEntry);
            return location.HasBeenVisited;
        }

        public static JournalLocation GetJournalLocationForEntry(JournalScreen.JournalEntry journalEntry)
        {
            FlushLocationCache();
            JournalLocation location;
            if (!CachedLocations.TryGetValue(journalEntry.baseEntry, out location))
            {
                location = new JournalLocation(journalEntry.baseEntry);
                CachedLocations.Add(journalEntry.baseEntry, location);
            }
            return location;
        }

        public static void FlushLocationCache()
        {
            if (XRLCore.Core.Game.Turns != CacheTurn)
            {
                CacheTurn = XRLCore.Core.Game.Turns;
                CachedLocations = new Dictionary<IBaseJournalEntry, JournalLocation>();
            }
        }

        public struct JournalLocation
        {
            private readonly JournalMapNote _Entry;
            private bool? _HasBeenVisited;

            public JournalLocation(IBaseJournalEntry journalEntry)
            {
                this._Entry = (journalEntry is JournalMapNote journalMapNote) ? journalMapNote : null;
                this._HasBeenVisited = null;
            }

            public bool IsValid
            {
                get
                {
                    return _Entry != null;
                }
            }

            public bool HasBeenVisited
            {
                get
                {
                    if (this._HasBeenVisited == null)
                    {
                        this._HasBeenVisited = this.IsValid
                            && (XRLCore.Core.Game.ZoneManager.CachedZones.ContainsKey(this._Entry.zoneid)
                            || JournalUtilities.FrozenZoneDataExists(this._Entry.zoneid));
                    }
                    return (bool)this._HasBeenVisited;
                }
            }
        }
    }
}
