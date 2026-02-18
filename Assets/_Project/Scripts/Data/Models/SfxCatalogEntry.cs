using System;
using System.Collections.Generic;

namespace MBHS.Data.Models
{
    [Serializable]
    public class SfxCatalogEntry
    {
        public string Id = "";
        public string Title = "";
        public string Category = "";
        public float DurationSeconds;
        public string AddressableKey = "";
        public bool IsLoopable;
    }

    [Serializable]
    public class SfxCatalog
    {
        public List<SfxCatalogEntry> Entries = new();
    }
}
