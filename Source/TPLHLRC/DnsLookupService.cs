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
        private static readonly Random Random = new Random();

        public HLRLookupResult Lookup(HLRLookupRequest request)
        {
            return new HLRLookupResult
            {
                Request = request,
                CacheResult = CacheResult.Miss,
                Properties = new Dictionary<string, string> {  
                    { "MNC", Random.Next(0, 99).ToString("D2") },
                    { "MCC", Random.Next(0, 999).ToString("D3") }
                }
            };
        }
    }
}
