using System;
using System.Threading.Tasks.Dataflow;
using Moq;
using NUnit.Framework;

namespace TPLHLRC.Tests
{
    //[TestFixture]
    //public class LookupServiceTests
    //{
    //    private Mock<ISourceBlock<HLRLookupRequest>> _lookupRequestProvider;
    //    private Mock<IPropagatorBlock<HLRLookupRequest, HLRLookupResult>> _cacheLookupService;
    //    private Mock<IPropagatorBlock<HLRLookupResult, HLRLookupResult>> _dnsLookupService;
    //    private Mock<ITargetBlock<HLRLookupResult>> _responseProvider;

    //    [SetUp]
    //    public void SetUp()
    //    {
    //        _lookupRequestProvider = new Mock<ISourceBlock<HLRLookupRequest>>();
    //        _cacheLookupService = new Mock<IPropagatorBlock<HLRLookupRequest, HLRLookupResult>>();
    //        _dnsLookupService = new Mock<IPropagatorBlock<HLRLookupResult, HLRLookupResult>>();
    //        _responseProvider = new Mock<ITargetBlock<HLRLookupResult>>();
    //        var lookupCache = new LookupService(_lookupRequestProvider.Object, _cacheLookupService.Object, _dnsLookupService.Object, _responseProvider.Object);

    //        lookupCache.Start();
    //    }

    //    [Test]
    //    public void ThenTheCacheIsLinked()
    //    {
    //        _lookupRequestProvider
    //            .Verify(p => p.LinkTo(_cacheLookupService.Object, It.IsAny<DataflowLinkOptions>()));
    //    }

    //    [Test]
    //    public void ThenTheDnsLookupServiceIsLinked()
    //    {
    //        _cacheLookupService
    //            .Verify(s => s.LinkTo(It.IsAny<ITargetBlock<HLRLookupResult>>(), It.IsAny<DataflowLinkOptions>()));
    //    }
    //}
}