using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace TPLHLRC
{
    public class RabbitConsumerBlock : ISourceBlock<HLRLookupRequest>
    {
        private readonly IPropagatorBlock<HLRLookupRequest, HLRLookupRequest> _sourceBlock;

        private readonly IModel _model;

        private EventingBasicConsumer _consumer;

        public RabbitConsumerBlock(IPropagatorBlock<HLRLookupRequest, HLRLookupRequest> sourceBlock, IModel model)
        {
            _sourceBlock = sourceBlock;
            _model = model;
        }

        public void Start()
        {
            _consumer = new EventingBasicConsumer(_model);
            _consumer.Received += (sender, args) =>
            {
                var request = JsonConvert.DeserializeObject<HLRLookupRequest>(Encoding.UTF8.GetString(args.Body));
                request.ReplyTo = args.BasicProperties.ReplyTo;
                _sourceBlock.Post(request);
            };
            _model.QueueDeclare("HLRLookup", false, false, true, new Dictionary<string, object>());
            _model.BasicConsume("HLRLookup", false, _consumer);
        }

        public void Stop()
        {
            _model.BasicCancel(_consumer.ConsumerTag);
        }

        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<HLRLookupRequest> target)
        {
            _sourceBlock.ReleaseReservation(messageHeader, target);
        }

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<HLRLookupRequest> target)
        {
            return _sourceBlock.ReserveMessage(messageHeader, target);
        }

        public IDisposable LinkTo(ITargetBlock<HLRLookupRequest> target, DataflowLinkOptions linkOptions)
        {
            return _sourceBlock.LinkTo(target, linkOptions);
        }

        public HLRLookupRequest ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<HLRLookupRequest> target, out bool messageConsumed)
        {
            return _sourceBlock.ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        public Task Completion
        {
            get { return _sourceBlock.Completion; }
        }

        public void Fault(Exception exception)
        {
            _sourceBlock.Fault(exception);
        }

        public void Complete()
        {
            _sourceBlock.Complete();
        }
    }
}