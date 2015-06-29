using System;
using Microsoft.Owin.Hosting;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace TPLHLRC
{
    static class Program
    {
        public static LookupService LookupService;
        static void Main(string[] args)
        {
            var connectionMultiplexer = ConnectionMultiplexer.Connect("localhost:6379");
            var database = connectionMultiplexer.GetDatabase(1);
            var rabbitConnectionFactory = new ConnectionFactory();
            var connection = rabbitConnectionFactory.CreateConnection();

            
            var redisCacheLookup = new RedisCacheService(database);

            LookupService = new LookupService(connection.CreateModel(),
                redisCacheLookup,
                new DnsLookupService());

            const string baseAddress = "http://localhost:9000/"; 

            // Start OWIN host 
            using (WebApp.Start<Startup>(baseAddress))
            {
                LookupService.Start();
                Console.ReadLine();
            }

            LookupService.Stop();
            connection.Dispose();
            connectionMultiplexer.Dispose();
        }
    }
}
