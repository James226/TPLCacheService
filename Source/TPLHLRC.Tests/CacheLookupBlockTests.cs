using System;
using System.Threading.Tasks.Dataflow;
using Moq;
using NUnit.Framework;

namespace TPLHLRC.Tests
{
    [TestFixture]
    public class CacheLookupBlockTests
    {
        private HLRLookupResult _result;
        private HLRLookupResult _expectedResult;

        [SetUp]
        public void SetUp()
        {
            var request = new HLRLookupRequest();
            var redisCacheService = new Mock<IRedisCacheService>();
            _expectedResult = new HLRLookupResult();
            redisCacheService
                .Setup(s => s.Lookup(request))
                .Returns(_expectedResult);

            var cacheBlock = CacheLookupBlock.Create(redisCacheService.Object);

            cacheBlock.Post(request);

            _result = cacheBlock.Receive(TimeSpan.FromMilliseconds(500));
        }

        [Test]
        public void ThenTheResultIsReturned()
        {
            Assert.That(_result, Is.SameAs(_expectedResult));
        }
    }
}