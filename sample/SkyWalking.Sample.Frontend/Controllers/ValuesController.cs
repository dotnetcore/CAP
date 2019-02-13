using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SkyWalking.Sample.Frontend.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet]
        public async Task<IEnumerable<string>> Get()
        {
            await new HttpClient().GetAsync("http://localhost:5002/api/values");
            return new string[] {"value1", "value2"};
        }

        [HttpGet("{id}")]
        public async Task<string> Get(int id)
        {
            var client = new HttpClient();
            Task.WhenAll(client.GetAsync("http://localhost:5002/api/delay/2000"),
                client.GetAsync("http://localhost:5002/api/values"),
                client.GetAsync("http://localhost:5002/api/delay/200"));
            return await client.GetStringAsync("http://localhost:5002/api/delay/100");
        }
    }
}