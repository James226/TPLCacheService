﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace TPLHLRC.Tests
{
    [TestFixture]
    public class RabbitConsumerBlockStartTests
    {
        private Mock<IModel> _model;

        [SetUp]
        public void SetUp()
        {
            _model = new Mock<IModel>();
            var consumerBlock = new RabbitConsumerBlock(_model.Object);
            consumerBlock.Start();
        }

        [Test]
        public void ThenTheQueueIsDeclared()
        {
            _model
                .Verify(m => m.QueueDeclare("HLRLookup", false, false, true, It.IsAny<IDictionary<string, object>>()));
        }

        [Test]
        public void ThenBasicConsumeIsCalled()
        {
            _model
                .Verify(m => m.BasicConsume("HLRLookup", false, It.IsAny<IBasicConsumer>()));
        }
    }

    [TestFixture]
    public class RabbitConsumerBlockStopTests
    {
        private Mock<IModel> _model;

        [SetUp]
        public void SetUp()
        {
            _model = new Mock<IModel>();
            var consumerBlock = new RabbitConsumerBlock(_model.Object);
            consumerBlock.Start();
            consumerBlock.Stop();
        }

        [Test]
        public void ThenBasicConsumeIsCalled()
        {
            _model
                .Verify(m => m.BasicCancel(It.IsAny<string>()));
        }
    }

    [TestFixture]
    public class RabbitConsumerBlockReceiveTests
    {
        private Mock<IModel> _model;
        private IBasicConsumer _consumer;
        private HLRLookupRequest _request;
        private string _replyTo;
        private RabbitConsumerBlock _consumerBlock;

        [SetUp]
        public void SetUp()
        {
            _replyTo = "1245";
            _model = new Mock<IModel>();
            _consumerBlock = new RabbitConsumerBlock(_model.Object);

            _model
                .Setup(m => m.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicConsumer>()))
                .Callback<string, bool, IBasicConsumer>((s, b, consumer) => _consumer = consumer);
            _consumerBlock.Start();

            _request = new HLRLookupRequest { MSISDN = "447987654321" };
            var encodedBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_request));
            _consumer.HandleBasicDeliver(string.Empty,
                0,
                false,
                string.Empty,
                string.Empty,
                new BasicProperties {ReplyTo = _replyTo},
                encodedBytes);
        }

        [Test]
        public void ThenOfferMessageIsCalled()
        {
            var result = _consumerBlock.Receive(TimeSpan.FromMilliseconds(500));

            Assert.That(result, Is.Not.Null);
            Assert.That(result.MSISDN, Is.EqualTo(_request.MSISDN));
            Assert.That(result.ReplyTo, Is.EqualTo(_replyTo));
        }
    }
}
