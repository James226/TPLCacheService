using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace TPLHLRC
{
    public class RedisCacheService
    {
        private readonly IDatabase _database;

        public RedisCacheService(IDatabase database)
        {
            _database = database;
        }

        public HLRLookupResult Lookup(HLRLookupRequest request)
        {
            var result = _database.StringGet(request.MSISDN);
            if (!result.HasValue)
                return new HLRLookupResult { Request = request, CacheResult = CacheResult.Miss };

            var properties = JObject.Parse(result).ToObject<Dictionary<string, string>>();
            return new HLRLookupResult { Request = request, CacheResult = CacheResult.Hit, Properties = properties };
        }

        public void Store(HLRLookupResult result)
        {
            _database.StringSet(result.Request.MSISDN, JsonConvert.SerializeObject(result.Properties));
        }
    }
}