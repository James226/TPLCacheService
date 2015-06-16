using System.Collections.Generic;
using System.Text;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace TPLHLRC.Tests
{
    [TestFixture]
    public class LookupServiceCacheHitTests
    {
        private Mock<IModel> _model;
        private Mock<IRedisCacheService> _cacheLookupService;
        private Mock<IDnsLookupService> _dnsLookupService;
        private HLRLookupRequest _request;
        private string _replyTo;

        [SetUp]
        public void SetUp()
        {
            IBasicConsumer consumer = null;
            _model = new Mock<IModel>();
            _model
                .Setup(m => m.CreateBasicProperties())
                .Returns(new BasicProperties());
            _model
                .Setup(m => m.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicConsumer>()))
                .Callback<string, bool, IBasicConsumer>((s, b, c) => consumer = c);

            _cacheLookupService = new Mock<IRedisCacheService>();
            _request = new HLRLookupRequest { MSISDN = "447987654321", ReplyTo = _replyTo };
            _cacheLookupService
                .Setup(s => s.Lookup(It.IsAny<HLRLookupRequest>()))
                .Returns(new HLRLookupResult
                {
                    Request = _request,
                    CacheResult = CacheResult.Hit,
                    Properties = new Dictionary<string, string>
                    {
                        {"MNC", "20"},
                        {"MCC", "234"}
                    }
                });
            _dnsLookupService = new Mock<IDnsLookupService>();
            var lookupCache = new LookupService(_model.Object, _cacheLookupService.Object, _dnsLookupService.Object);

            lookupCache.Start();

            var encodedBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_request));
            _replyTo = "112233";
            consumer.HandleBasicDeliver(string.Empty,
                0,
                false,
                string.Empty,
                string.Empty,
                new BasicProperties { ReplyTo = _replyTo },
                encodedBytes);

            lookupCache.Stop();
        }

        [Test]
        public void ThenTheCacheIsQueried()
        {
            _cacheLookupService
                .Verify(s => s.Lookup(It.Is<HLRLookupRequest>(r => r.MSISDN == _request.MSISDN)));
        }

        [Test]
        public void ThenTheDnsLookupIsNotInvoked()
        {
            _dnsLookupService
                .Verify(s => s.Lookup(It.IsAny<HLRLookupRequest>()), Times.Never);
        }

        [Test]
        public void ThenTheResponseIsSent()
        {
            _model
                .Verify(m => m.BasicPublish(string.Empty, _replyTo, It.IsAny<IBasicProperties>(), It.Is<byte[]>(b => VerifyResponsePayload(b))));
        }

        private bool VerifyResponsePayload(byte[] bytes)
        {
            var response = JObject.Parse(Encoding.UTF8.GetString(bytes));

            Assert.That(response["MNC"].Value<string>(), Is.EqualTo("20"));
            Assert.That(response["MCC"].Value<string>(), Is.EqualTo("234"));
            return true;
        }
    }

    [TestFixture]
    public class LookupServiceCacheMissTests
    {
        private Mock<IModel> _model;
        private Mock<IRedisCacheService> _cacheLookupService;
        private Mock<IDnsLookupService> _dnsLookupService;
        private HLRLookupRequest _request;
        private string _replyTo;
        private HLRLookupResult _result;

        [SetUp]
        public void SetUp()
        {
            IBasicConsumer consumer = null;
            _model = new Mock<IModel>();
            _model
                .Setup(m => m.CreateBasicProperties())
                .Returns(new BasicProperties());
            _model
                .Setup(m => m.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicConsumer>()))
                .Callback<string, bool, IBasicConsumer>((s, b, c) => consumer = c);

            _cacheLookupService = new Mock<IRedisCacheService>();
            _request = new HLRLookupRequest { MSISDN = "447987654321", ReplyTo = _replyTo };
            _cacheLookupService
                .Setup(s => s.Lookup(It.IsAny<HLRLookupRequest>()))
                .Returns(new HLRLookupResult
                {
                    Request = _request,
                    CacheResult = CacheResult.Miss
                });

            _dnsLookupService = new Mock<IDnsLookupService>();
            _result = new HLRLookupResult
            {
                Request = _request,
                CacheResult = CacheResult.Miss,
                Properties = new Dictionary<string, string>
                {
                    { "MNC", "11" },
                    { "MCC", "218" }
                }
            };
            _dnsLookupService
                .Setup(s => s.Lookup(It.IsAny<HLRLookupRequest>()))
                .Returns(_result);
            var lookupCache = new LookupService(_model.Object, _cacheLookupService.Object, _dnsLookupService.Object);

            lookupCache.Start();

            var encodedBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_request));
            _replyTo = "112233";
            consumer.HandleBasicDeliver(string.Empty,
                0,
                false,
                string.Empty,
                string.Empty,
                new BasicProperties { ReplyTo = _replyTo },
                encodedBytes);

            lookupCache.Stop();
        }

        [Test]
        public void ThenTheCacheIsQueried()
        {
            _cacheLookupService
                .Verify(s => s.Lookup(It.Is<HLRLookupRequest>(r => r.MSISDN == _request.MSISDN)));
        }

        [Test]
        public void ThenTheDnsServiceIsQueried()
        {
            _dnsLookupService
                .Verify(s => s.Lookup(It.Is<HLRLookupRequest>(r => r.MSISDN == _request.MSISDN)));
        }

        [Test]
        public void ThenTheResponseIsSent()
        {
            _model
                .Verify(m => m.BasicPublish(string.Empty, _replyTo, It.IsAny<IBasicProperties>(), It.Is<byte[]>(b => VerifyResponsePayload(b))));
        }

        [Test]
        public void ThenTheResponseIsCached()
        {
            _cacheLookupService
                .Verify(s => s.Store(_result));
        }

        private bool VerifyResponsePayload(byte[] bytes)
        {
            var response = JObject.Parse(Encoding.UTF8.GetString(bytes));

            Assert.That(response["MNC"].Value<string>(), Is.EqualTo("11"));
            Assert.That(response["MCC"].Value<string>(), Is.EqualTo("218"));
            return true;
        }
    }
}