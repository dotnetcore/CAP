using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Sample.RabbitMQ.Oracle;

namespace sample.rabbitmq.oracle.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly ICapPublisher _capBus;

        public ValuesController(ICapPublisher capPublisher)
        {
            _capBus = capPublisher;
        }

        [Route("~/without/transaction")]
        public async Task<IActionResult> WithoutTransaction()
        {
            await _capBus.PublishAsync("sample.rabbitmq.oracle", DateTime.Now);

            return Ok();
        }

        [Route("~/adonet/transaction")]
        public IActionResult AdonetWithTransaction()
        {
            using (var connection = new OracleConnection(AppDbContext.ConnectionString))
            {
                using (var transaction = connection.BeginTransaction(_capBus, true))
                {
                    //your business code
                    var p = new OracleParameter { ParameterName = ":Id", Value = 4001 };
                    var p2 = new OracleParameter { ParameterName = ":Name", Value = Guid.NewGuid().ToString() };
                    var p3 = new OracleParameter { ParameterName = ":Birthday", Value = DateTime.Now };
                    //connection.Execute($"insert into \"Persons\" (\"Id\",\"Name\") values(:Id,'{Guid.NewGuid().ToString()}')", param: p, transaction: (IDbTransaction)transaction.DbTransaction);
                    
                    var cmd = connection.CreateCommand();
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = $"insert into \"Persons2\" (\"Id\",\"Name\",\"Birthday\") values(:Id,:Name,:Birthday)";
                    cmd.Parameters.Add(p);
                    cmd.Parameters.Add(p2);
                    cmd.Parameters.Add(p3);
                    cmd.ExecuteNonQuery();

                    //for (int i = 0; i < 5; i++)
                    //{
                    _capBus.Publish("sample.rabbitmq.oracle", DateTime.Now);
                    //}
                }
            }

            return Ok();
        }

        [Route("~/ef/transaction")]
        public IActionResult EntityFrameworkWithTransaction([FromServices] AppDbContext dbContext)
        {
            using (var trans = dbContext.Database.BeginTransaction(_capBus, autoCommit: false))
            {
                dbContext.Persons.Add(new Person() { Name = "ef.transaction" });

                for (int i = 0; i < 1; i++)
                {
                    _capBus.Publish("sample.rabbitmq.oracle", DateTime.Now);
                }

                dbContext.SaveChanges();

                trans.Commit();
            }
            return Ok();
        }

        [NonAction]
        [CapSubscribe("sample.rabbitmq.oracle")]
        public void Subscriber(DateTime p)
        {
            Console.WriteLine($@"{DateTime.Now} Subscriber invoked, Info: {p}");
        }

        [NonAction]
        [CapSubscribe("sample.rabbitmq.oracle", Group = "group.test2")]
        public void Subscriber2(DateTime p, [FromCap] CapHeader header)
        {
            Console.WriteLine($@"{DateTime.Now} Subscriber invoked, Info: {p}");
        }
    }
}
