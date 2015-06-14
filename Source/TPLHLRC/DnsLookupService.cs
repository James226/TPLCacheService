using System;
using System.Collections.Generic;

namespace TPLHLRC
{
    public interface IDnsLookupService
    {
        HLRLookupResult Lookup(HLRLookupRequest request);
    }

    public class DnsLookupService : IDnsLookupService
    {
        public HLRLookupResult Lookup(HLRLookupRequest request)
        {
            var random = new Random();
            return new HLRLookupResult
            {
                Request = request,
                CacheResult = CacheResult.Miss,
                Properties = new Dictionary<string, string> {  
                    { "MNC", random.Next(0, 99).ToString("D2") },
                    { "MCC", random.Next(0, 999).ToString("D3") }
                }
            };
        }
    }
}
