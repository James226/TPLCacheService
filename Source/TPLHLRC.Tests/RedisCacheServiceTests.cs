using System.Collections.Generic;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using StackExchange.Redis;

namespace TPLHLRC.Tests
{
    [TestFixture]
    public class RedisCacheLookupCacheMissTests
    {
        private Mock<IDatabase> _redis;
        private HLRLookupRequest _request;
        private HLRLookupResult _result;

        [SetUp]
        public void SetUp()
        {
            _redis = new Mock<IDatabase>();
            var redisCacheLookup = new RedisCacheService(_redis.Object);

            _request = new HLRLookupRequest {MSISDN = "447987654321"};
            _redis
                .Setup(r => r.StringGet(_request.MSISDN, CommandFlags.None))
                .Returns(RedisValue.Null);

            _result = redisCacheLookup.Lookup(_request);
        }

        [Test]
        public void ThenACacheMissIsReturned()
        {
            Assert.That(_result.CacheResult, Is.EqualTo(CacheResult.Miss));
        }
    }

    [TestFixture]
    public class RedisCacheLookupCacheHitTests
    {
        private Mock<IDatabase> _redis;
        private HLRLookupRequest _request;
        private HLRLookupResult _result;

        [SetUp]
        public void SetUp()
        {
            _redis = new Mock<IDatabase>();
            var redisCacheLookup = new RedisCacheService(_redis.Object);

            _request = new HLRLookupRequest {MSISDN = "447987654321"};
            _redis
                .Setup(r => r.StringGet(_request.MSISDN, CommandFlags.None))
                .Returns(JsonConvert.SerializeObject(new { MNC = 30, MCC = 234 }));

            _result = redisCacheLookup.Lookup(_request);
        }

        [Test]
        public void ThenACacheHitIsReturned()
        {
            Assert.That(_result.CacheResult, Is.EqualTo(CacheResult.Hit));
        }

        [Test]
        public void ThenThePropertiesAreSet()
        {
            Assert.That(_result.Properties, Is.EquivalentTo(new Dictionary<string, string> { { "MNC", "30"}, { "MCC", "234"}}));
        }
    }

    [TestFixture]
    public class RedisCacheStoreTests
    {
        private Mock<IDatabase> _redis;
        private HLRLookupResult _result;

        [SetUp]
        public void SetUp()
        {
            _redis = new Mock<IDatabase>();
            var redisCacheLookup = new RedisCacheService(_redis.Object);

            _result = new HLRLookupResult
            {
                Request = new HLRLookupRequest
                {
                    MSISDN = "447987654321"
                },
                Properties = new Dictionary<string, string>
                {
                    { "MNC", "20"},
                    { "MCC", "234" }
                }
            };

            redisCacheLookup.Store(_result);
        }

        [Test]
        public void ThenTheResultIsStored()
        {
            _redis
                .Verify(r => r.StringSet(
                    _result.Request.MSISDN,
                    "{\"MNC\":\"20\",\"MCC\":\"234\"}",
                    null,
                    When.Always,
                    CommandFlags.None));
        }
    }
}