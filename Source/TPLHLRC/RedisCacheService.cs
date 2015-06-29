using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Threading.Tasks.Dataflow;

namespace TPLHLRC
{
    public interface IRedisCacheService
    {
        Task<HLRLookupResult> Lookup(HLRLookupRequest request);
        void Store(HLRLookupResult result);
    }

    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDatabase _database;

        public RedisCacheService(IDatabase database)
        {
            _database = database;
        }

        public async Task<HLRLookupResult> Lookup(HLRLookupRequest request)
        {
            RedisValue result;
            try
            {
                result = await _database.StringGetAsync(request.MSISDN);
            }
            catch (Exception e)
            {
                return new HLRLookupResult {Request = request, CacheResult = CacheResult.Miss};
            }
            if (!result.HasValue)
                return new HLRLookupResult { Request = request, CacheResult = CacheResult.Miss };

            var properties = JObject.Parse(result).ToObject<Dictionary<string, string>>();
            return new HLRLookupResult { Request = request, CacheResult = CacheResult.Hit, Properties = properties };
        }

        public void Store(HLRLookupResult result)
        {
            try
            {
                _database.StringSet(result.Request.MSISDN, JsonConvert.SerializeObject(result.Properties));
            }
            catch
            {
                // ignored
            }
        }
    }

    public static class CacheLookupBlock
    {
        public static IPropagatorBlock<HLRLookupRequest, HLRLookupResult> Create(IRedisCacheService redisCacheService, int maxDegreeOfParallelism)
        {
            return new TransformBlock<HLRLookupRequest, HLRLookupResult>(r => redisCacheService.Lookup(r), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism });
        }
    }
}