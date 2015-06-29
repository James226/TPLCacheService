using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Esendex.TokenBucket;
using Microsoft.AspNet.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.MessagePatterns;

namespace TPLHLRC.Web.Hubs
{
    public class Client
    {
        public Task Task { get; set; }
        public CancellationTokenSource Cancellation { get; set; }
        public ITokenBucket Bucket { get; set; }
    }

    public class NumberLookup : Hub
    {
        private static readonly Dictionary<string, Client> Connections = new Dictionary<string, Client>();

        public override Task OnConnected()
        {
            Connections[Context.ConnectionId] = new Client();
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            if (Connections.ContainsKey(Context.ConnectionId) && Connections[Context.ConnectionId].Task != null)
            {
                Connections[Context.ConnectionId].Cancellation.Cancel();
                Connections[Context.ConnectionId].Task.Wait();
            }
            Connections.Remove(Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }

        public void Start(int lookupsPerSecond)
        {
            var conn = Connections[Context.ConnectionId];
            conn.Cancellation = new CancellationTokenSource();
            conn.Task = Task.Run(() =>
            {
                var random = new Random();
                var factory = new ConnectionFactory {HostName = "localhost"};
                conn.Bucket = TokenBuckets.Construct()
                    .WithCapacity(lookupsPerSecond)
                    .WithFixedIntervalRefillStrategy(lookupsPerSecond, TimeSpan.FromSeconds(1))
                    .Build();
                var replyBucket = TokenBuckets.Construct()
                    .WithCapacity(1)
                    .WithFixedIntervalRefillStrategy(1, TimeSpan.FromSeconds(1))
                    .Build();

                var currentCount = 0;


                using (var connection = factory.CreateConnection())
                {
                    var input = new BroadcastBlock<string>(r => r);
                    var output = new ActionBlock<string>(msisdn =>
                    {
                        using (var channel = connection.CreateModel())
                        {
                            var simpleRpcClient = new SimpleRpcClient(channel, "HLRLookup");
                            var response =
                                simpleRpcClient.Call(
                                    Encoding.UTF8.GetBytes(string.Format("{{MSISDN: '{0}'}}", msisdn)));
                            Interlocked.Increment(ref currentCount);
                            if (!replyBucket.TryConsume()) return;

                            var count = Interlocked.Exchange(ref currentCount, 0);
                            Clients.All.hello(count);
                        }
                    },
                        new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = 5});
                    input.LinkTo(output, new DataflowLinkOptions {PropagateCompletion = true});
                    while (!conn.Cancellation.IsCancellationRequested)
                    {
                        conn.Bucket.Consume();
                        input.Post(string.Format("447{0}", random.Next(100000000, 999999999)));
                    }
                    input.Complete();
                    output.Completion.Wait();
                }
            });
        }

        public void Stop()
        {
            Connections[Context.ConnectionId].Cancellation.Cancel();
        }

        public void SetRate(int rate)
        {
            Connections[Context.ConnectionId].Bucket = TokenBuckets.Construct()
                .WithCapacity(rate)
                .WithFixedIntervalRefillStrategy(rate, TimeSpan.FromSeconds(1))
                .Build();
        }
    }
}