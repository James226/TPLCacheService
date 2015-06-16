using System;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Esendex.TokenBucket;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace TPLHLRC
{
    public class LookupService
    {
        private readonly IModel _rabbitModel;
        private readonly IRedisCacheService _redisCacheService;
        private readonly IDnsLookupService _dnsLookupService;
        private RabbitConsumerBlock _lookupRequestProvider;
        private Task _completionTask;

        public LookupService(IModel rabbitModel,
            IRedisCacheService redisCacheService,
            IDnsLookupService dnsLookupService)
        {
            _rabbitModel = rabbitModel;
            _redisCacheService = redisCacheService;
            _dnsLookupService = dnsLookupService;
        }

        public void Start()
        {
            _lookupRequestProvider = new RabbitConsumerBlock(_rabbitModel);
            var cacheLookupBlock = CacheLookupBlock.Create(_redisCacheService);

            var dnsLookupBlock = new TransformBlock<HLRLookupResult,HLRLookupResult>(r => _dnsLookupService.Lookup(r.Request));
            var cacheStoreBlock = new ActionBlock<HLRLookupResult>(r => _redisCacheService.Store(r));
            var logBlock = new ActionBlock<HLRLookupResult>(r => Console.WriteLine("Response to request MSISDN: {0}, MNC:{1}, MCC:{2}", r.Request.MSISDN, r.Properties["MNC"], r.Properties["MCC"]));
            var responseBlock = new ActionBlock<HLRLookupResult>(r => SendResponse(r));
            _completionTask = responseBlock.Completion;

            var bucket = TokenBuckets.Construct()
                .WithCapacity(1)
                .WithFixedIntervalRefillStrategy(1, TimeSpan.FromSeconds(20))
                .Build();

            var rateLimiterBlock = new TransformBlock<HLRLookupResult, HLRLookupResult>(r =>
            {
                bucket.Consume();
                return r;
            });

            _lookupRequestProvider.LinkTo(cacheLookupBlock, new DataflowLinkOptions { PropagateCompletion = true });

            rateLimiterBlock.LinkTo(dnsLookupBlock, new DataflowLinkOptions { PropagateCompletion = true });

            cacheLookupBlock.LinkTo(responseBlock, r => r.CacheResult == CacheResult.Hit);
            cacheLookupBlock.LinkTo(rateLimiterBlock, new DataflowLinkOptions { PropagateCompletion = true }, r => r.CacheResult == CacheResult.Miss);

            var forker = new BroadcastBlock<HLRLookupResult>(r => r);

            dnsLookupBlock.LinkTo(forker, new DataflowLinkOptions { PropagateCompletion = true });

            forker.LinkTo(responseBlock);
            forker.LinkTo(logBlock, new DataflowLinkOptions { PropagateCompletion = true });
            forker.LinkTo(cacheStoreBlock, new DataflowLinkOptions { PropagateCompletion = true });

            Task.WhenAll(forker.Completion, cacheLookupBlock.Completion)
                .ContinueWith(t => responseBlock.Complete());

            _lookupRequestProvider.Start();
        }

        private void SendResponse(HLRLookupResult result)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result.Properties));
            var properties = _rabbitModel.CreateBasicProperties();
            properties.CorrelationId = result.Request.MSISDN;
            _rabbitModel.BasicPublish(string.Empty, result.Request.ReplyTo, properties, bytes);
        }

        public void Stop()
        {
            _lookupRequestProvider.Stop();
            _completionTask.Wait();
        }
    }
}