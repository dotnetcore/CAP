using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Sample.RabbitMQ.SqlServer.DispatcherPerGroup.Messages;
using System;
using System.Threading.Tasks;

namespace Sample.RabbitMQ.SqlServer.DispatcherPerGroup.Controllers
{
    public class HomeController : Controller
    {
        private readonly ICapPublisher _capPublisher;

        public HomeController(ICapPublisher capPublisher)
        {
            _capPublisher = capPublisher;
        }

        public async Task<IActionResult> Index()
        {
            await using (var connection = new SqlConnection("Server=(local);Database=CAP-Test;Trusted_Connection=True;"))
            {
                using var transaction = connection.BeginTransaction(_capPublisher);
                // This is where you would do other work that is going to persist data to your database

                var message = TestMessage.Create($"This is message text created at {DateTime.Now:O}.");

                await _capPublisher.PublishAsync(typeof(TestMessage).FullName, message);
                transaction.Commit();
            }

            return Content("ok");
        }
    }
}