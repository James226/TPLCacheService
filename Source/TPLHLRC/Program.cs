using System;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace TPLHLRC
{
    static class Program
    {
        static void Main(string[] args)
        {
            var connectionMultiplexer = ConnectionMultiplexer.Connect("localhost:6379");
            var database = connectionMultiplexer.GetDatabase(1);
            var rabbitConnectionFactory = new ConnectionFactory();
            var connection = rabbitConnectionFactory.CreateConnection();

            
            var redisCacheLookup = new RedisCacheService(database);

            var lookupService = new LookupService(connection.CreateModel(),
                redisCacheLookup,
                new DnsLookupService());

            lookupService.Start();
            Console.ReadLine();

            lookupService.Stop();

            // TODO: Stop lookup service
            connection.Dispose();
            connectionMultiplexer.Dispose();
        }
    }
}
