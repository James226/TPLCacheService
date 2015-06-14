using System;
using System.Threading.Tasks.Dataflow;
using Esendex.TokenBucket;
using RabbitMQ.Client;

namespace TPLHLRC
{
    public class LookupService
    {
        private readonly IModel _rabbitModel;
        private readonly RedisCacheService _redisCacheService;
        private readonly IDnsLookupService _dnsLookupService;
        private RabbitConsumerBlock _lookupRequestProvider;

        public LookupService(IModel rabbitModel,
            RedisCacheService redisCacheService,
            IDnsLookupService dnsLookupService)
        {
            _rabbitModel = rabbitModel;
            _redisCacheService = redisCacheService;
            _dnsLookupService = dnsLookupService;
        }

        public void Start()
        {
            _lookupRequestProvider = new RabbitConsumerBlock(new BroadcastBlock<HLRLookupRequest>(r => r), _rabbitModel);
            var cacheLookupBlock =
                new TransformBlock<HLRLookupRequest, HLRLookupResult>(input => _redisCacheService.Lookup(input),
                    new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = 20});

            var dnsLookupBlock = new TransformBlock<HLRLookupResult,HLRLookupResult>(r => _dnsLookupService.Lookup(r.Request));
            var cacheStoreBlock = new ActionBlock<HLRLookupResult>(r => _redisCacheService.Store(r));
            var responseBlock = new ActionBlock<HLRLookupResult>(r => Console.WriteLine("Response to request MSISDN: {0}, MNC:{1}, MCC:{2}", r.Request.MSISDN, r.Properties["MNC"], r.Properties["MCC"]));

            _lookupRequestProvider.LinkTo(cacheLookupBlock);

            var bucket = TokenBuckets.Construct()
                .WithCapacity(1)
                .WithFixedIntervalRefillStrategy(1, TimeSpan.FromSeconds(20))
                .Build();

            var hookBucket = new TransformBlock<HLRLookupResult, HLRLookupResult>(r =>
            {
                bucket.Consume();
                return r;
            });

            hookBucket.LinkTo(dnsLookupBlock);
            
            cacheLookupBlock.LinkTo(responseBlock, r => r.CacheResult == CacheResult.Hit);
            cacheLookupBlock.LinkTo(hookBucket, r => r.CacheResult == CacheResult.Miss);

            var forker = new BroadcastBlock<HLRLookupResult>(r => r);

            dnsLookupBlock.LinkTo(forker);

            forker.LinkTo(responseBlock);
            forker.LinkTo(cacheStoreBlock);

            _lookupRequestProvider.Start();
        }

        public void Stop()
        {
            _lookupRequestProvider.Stop();
        }
    }
}