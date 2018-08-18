using System;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace Sample.Kafka.MySql.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller, ICapSubscribe
    {
        private readonly ICapPublisher _capBus;

        public ValuesController(ICapPublisher producer)
        {
            _capBus = producer;
        }

        [Route("~/publish")]
        public async Task<IActionResult> PublishMessage()
        {
            using (var connection = new MySqlConnection("Server=192.168.10.110;Database=testcap;UserId=root;Password=123123;"))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                //your business code here

                await _capBus.PublishAsync("xxx.xxx.test2", 123456);

                transaction.Commit();
            }

            return Ok("publish successful!");
        }

        [CapSubscribe("#.test2")]
        public void Test2(int value)
        {
            Console.WriteLine("Subscriber output message: " + value);
        }
    }
}