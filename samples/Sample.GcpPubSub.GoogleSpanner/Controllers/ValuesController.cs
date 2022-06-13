using System;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP;
using Google.Cloud.Spanner.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Sample.GcpPubSub.GoogleSpanner
{
    [Route("api/[controller]")]
    public class ValuesController : Controller, ICapSubscribe
    {
        public class MyObj
        {
            public string SingerId { get; set; }
            public string FirstName { get; set; }
        }

        private readonly string _connectionString;
        private readonly ICapPublisher _capBus;

        public ValuesController(ICapPublisher producer, IConfiguration configuration)
        {
            _capBus = producer;
            _connectionString = configuration.GetConnectionString("SpannerDB");
        }

        [Route("~/without/transaction")]
        public async Task<IActionResult> WithoutTransaction()
        {
            await _capBus.PublishAsync("sample.gcppubsub.googlespanner", DateTime.Now);

            return Ok();
        }

        [Route("~/adonet/transaction")]
        public IActionResult AdonetWithTransaction()
        {
            var random = new Random().Next(1, 10000);
            try
            {
                using (var connection = new SpannerConnection(_connectionString))
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        var id = Guid.NewGuid().ToString();
                        var sql = "INSERT INTO Singers (SingerId, FirstName, LastName, FullName) " +
                            " values ('" + id + "', 'User" + random + "', 'Doe', 'User" + random + " Doe')";
                        var cmd = connection.CreateDmlCommand(sql);
                        cmd.Transaction = transaction;
                        _ = cmd.ExecuteNonQuery();

                        var msg = new MyObj { SingerId = id, FirstName = "User" + random };

                        _capBus.Publish("sample.gcppubsub.googlespanner", msg);

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return Ok();
        }


        [CapSubscribe("sample.gcppubsub.googlespanner")]
        public void Test2(MyObj value)
        {
            Console.WriteLine("Subscriber output message: " + JsonSerializer.Serialize(value));
        }
    }
}