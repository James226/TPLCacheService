using NUnit.Framework;

namespace TPLHLRC.Tests
{
    [TestFixture]
    public class DnsLookupServiceTests
    {
        private HLRLookupResult _result;
        private HLRLookupRequest _request;

        [SetUp]
        public void SetUp()
        {
            var dnsLookupService = new DnsLookupService();

            _request = new HLRLookupRequest {MSISDN = "447987654321", ReplyTo = "Reply"};
            _result = dnsLookupService.Lookup(_request);
        }

        [Test]
        public void ThenTheRequestIsContainedInTheResult()
        {
            Assert.That(_result.Request, Is.SameAs(_request));
        }
    }
}