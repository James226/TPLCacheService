using System.Collections.Generic;

namespace TPLHLRC
{
    public class HLRLookupResult
    {
        public HLRLookupRequest Request { get; set; }
        public CacheResult CacheResult { get; set; }
        public Dictionary<string, string> Properties { get; set; }
    }
}