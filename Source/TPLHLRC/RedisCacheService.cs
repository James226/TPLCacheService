using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Threading.Tasks.Dataflow;

namespace TPLHLRC
{
    public interface IRedisCacheService
    {
        HLRLookupResult Lookup(HLRLookupRequest request);
        void Store(HLRLookupResult result);
    }

    public class RedisCacheService : IRedisCacheService
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

    public static class CacheLookupBlock
    {
        public static IPropagatorBlock<HLRLookupRequest, HLRLookupResult> Create(IRedisCacheService redisCacheService)
        {
            return new TransformBlock<HLRLookupRequest, HLRLookupResult>(r => redisCacheService.Lookup(r), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 20 });
        }
    }
}